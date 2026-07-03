using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// QTE 状态机（由 Enemy 的 Idle 调度触发，不再自行管理冷却）
/// Idle → PerformingQTEAttack → QTEJudging → QTEEnding → QTECompleted → Idle
///
/// QTE 阶段概念：
///   - PerformingQTEAttack: 等待 QTE 阶段开始（飞行物到达 / 动画前摇结束）
///   - QTEJudging: 判定阶段，按 slot 时序持续到 max(slot.judgeEndTime)
///   - QTEEnding: 播放结束动画，等待动画播完后真正完成
///   - 全部 QTE 成功 → 销毁飞行物；任一失败 → 飞行物穿过摄像机
/// </summary>
public enum QTEState
{
    Idle,
    PerformingQTEAttack,
    QTEJudging,
    QTEEnding,
    QTECompleted
}

public class QTEInstance
{
    public QTEConfig config;
    public float spawnTime;       // QTE 阶段开始后的生成时间
    public float warningEndTime;  // 预警结束时间（判定窗口开始）
    public float judgeEndTime;    // 判定窗口结束时间
    public GameObject indicator;
    public bool resolved;
    public bool success;
    public bool judgmentStarted;  // 判定窗口已开始（用于通知 QTEDisplay）

    public bool IsInJudgeWindow(float phaseElapsed) => phaseElapsed >= warningEndTime && phaseElapsed <= judgeEndTime;
    public bool IsExpired(float phaseElapsed) => phaseElapsed > judgeEndTime && !resolved;
}

/// <summary>
/// QTE 控制器 — 挂载到 BOSS GameObject
/// 管理 QTE 队列状态机、冷却计时、触发时机、输入判定、成功/失败分发
/// </summary>
public class QTEController : MonoBehaviour
{
    [Header("配置")]
    public BossQTEData qteData;
    public ArrowGlobalConfig arrowConfig;

    [Header("格挡表现")]
    [Tooltip("格挡成功时生成的矛 prefab（stab_prefab）")]
    public GameObject stabBlockEffectPrefab;
    [Tooltip("格挡精灵帧：stab")]
    public Sprite stabBlockSprite;
    [Tooltip("格挡精灵帧：stab_rotate1")]
    public Sprite stabBlockRotateSprite1;
    [Tooltip("格挡精灵帧：stab_rotate2")]
    public Sprite stabBlockRotateSprite2;
    [Tooltip("格挡动画总时长（秒）")]
    public float stabBlockDuration = 0.5f;
    [Tooltip("格挡效果在摄像机前方距离")]
    public float stabBlockDistance = 3f;
    [Tooltip("格挡效果缩放")]
    public Vector3 stabBlockScale = new Vector3(0.15f, 0.15f, 0.15f);

    [Header("组件引用")]
    public Enemy enemy;
    public QTEDisplay qteDisplay;

    [Header("运行时状态")]
    [SerializeField] private QTEState _state = QTEState.Idle;

    private int _currentAttackIndex;
    private float _performingTimer;       // Performing 阶段的累计时间（用于动画前摇）
    private bool _qtePhaseStarted;        // QTE 阶段是否已开始（飞行物到达 / 动画前摇结束）
    private float _qtePhaseTimer;         // QTE 阶段开始后的计时器（用于 QTE spawn/judge）
    private float _fixedEndTimer;         // 固定时长结束倒计时（fixedQteDuration > 0 时使用）
    private float _effectiveJudgeDuration; // 计算得出的实际判定阶段时长 = max(slot.judgeEndTime)
    private float _endAnimTimer;           // 结束动画播放计时器
    private QTEAttackConfig _currentAttack;
    private List<QTEInstance> _activeQTEs = new List<QTEInstance>();
    private GameObject _activeProjectile;

    // 箭矢波追踪（多段防御型 QTE）：slotIndex → 该波所有箭矢
    private Dictionary<int, List<EnemyProjectile>> _arrowWaves = new Dictionary<int, List<EnemyProjectile>>();
    private bool _arrowWavesSpawned;

    // QTE 动画
    private Animator _animator;
    private bool _judgingSpeedApplied;   // Branched 模式下是否已对 Happen 应用慢放速度

    // 事件
    public System.Action OnQTETriggered;       // QTE 攻击触发
    public System.Action OnQTESuccess;         // QTE 判定成功
    public System.Action OnQTEFailure;         // QTE 判定失败
    public System.Action OnQTECompleted;       // 一轮 QTE 攻击结束
    public System.Action OnQTEAttackFinished;  // QTE 攻击完全结束，通知 Enemy 回到 Idle

    public QTEState State => _state;
    public QTEAttackConfig CurrentAttackConfig => _currentAttack;

    private void Awake()
    {
        if (enemy == null) enemy = GetComponent<Enemy>();
    }

    private void Start()
    {
        DebugLog.Info($"[QTEController] Start: enemy={enemy?.name}, isBoss={enemy?.isBoss}, qteData={qteData?.name}, qteAttacksCount={qteData?.qteAttacks?.Count}");
        if (enemy != null && enemy.isBoss)
        {
            // 初始化 QTE 攻击索引（由 Enemy 的 Idle 调度决定何时触发 QTE）
            if (qteData != null && qteData.qteAttacks.Count > 0)
                _currentAttackIndex = 0;
        }
    }

    private void OnDestroy()
    {
        KillProjectileSequence();
    }

    private void Update()
    {
        if (qteData == null || enemy == null) return;

        // 敌人脱离 QTE 状态（如被打入 Stun），中止 QTE
        if ((_state == QTEState.PerformingQTEAttack || _state == QTEState.QTEJudging || _state == QTEState.QTEEnding)
            && enemy.state != EnemyState.QTEAttacking)
        {
            DebugLog.Info($"[QTEController] 敌人脱离QTE状态({enemy.state})，中止QTE");
            AbortQTE();
            return;
        }

        switch (_state)
        {
            case QTEState.PerformingQTEAttack:
                UpdatePerforming();
                break;
            case QTEState.QTEJudging:
                UpdateJudging();
                break;
            case QTEState.QTEEnding:
                UpdateEnding();
                break;
        }
    }

    #region 状态切换

    /// <summary>
    /// 切换 QTE 数据（用于 BOSS 转阶段时更换 QTE 配置）
    /// 重置攻击索引，回到 Idle 状态
    /// </summary>
    public void SwitchQteData(BossQTEData newData)
    {
        qteData = newData;
        _currentAttackIndex = 0;
        _state = QTEState.Idle;
        _activeQTEs.Clear();
        DebugLog.Info($"[QTEController] 切换QTE数据: {newData?.name}, state={_state}");
    }

    /// <summary>
    /// 触发下一轮 QTE 攻击（由 Enemy 的 Idle 调度调用）
    /// 返回 false 表示当前无法触发（QTE 序列已耗尽、敌人在眩晕/击飞中等）
    /// </summary>
    public bool TriggerQTEAttack()
    {
        DebugLog.Info($"[QTEController] TriggerQTEAttack: attackIndex={_currentAttackIndex}, totalAttacks={qteData?.qteAttacks?.Count}");
        if (qteData == null || qteData.qteAttacks.Count == 0) return false;

        // 眩晕/击飞期间禁止触发QTE
        if (enemy.state == EnemyState.Stunned || enemy.state == EnemyState.Launched)
        {
            DebugLog.Info($"[QTEController] 敌人在眩晕/击飞状态({enemy.state})，跳过QTE");
            return false;
        }

        // QTE 序列已耗尽（非循环模式）
        if (_currentAttackIndex >= qteData.qteAttacks.Count)
        {
            DebugLog.Info("[QTEController] QTE序列已耗尽");
            return false;
        }

        _currentAttack = qteData.qteAttacks[_currentAttackIndex];
        DebugLog.Info($"[QTEController] 当前攻击: {_currentAttack?.name}, slots={_currentAttack?.qteSlots?.Count}, useMultiPhase={_currentAttack?.UseMultiPhaseAnimation}, useBranched={_currentAttack?.UseBranchedAnimation}");
        _state = QTEState.PerformingQTEAttack;
        _performingTimer = 0f;
        _qtePhaseStarted = false;
        _qtePhaseTimer = 0f;

        // 清除上一轮可能残留的指示器（防御性清理，防止重叠）
        DebugLog.Info($"[QTE_DIAG] TriggerQTEAttack: clearing indicators before new attack, attackIndex={_currentAttackIndex}");
        if (qteDisplay != null)
            qteDisplay.ClearAllIndicators();

        _activeQTEs.Clear();

        // 创建 QTE 实例（spawnTime/warningEndTime/judgeEndTime 相对于 QTE 阶段开始）
        foreach (var slot in _currentAttack.qteSlots)
        {
            if (slot.config == null) continue;
            _activeQTEs.Add(new QTEInstance
            {
                config = slot.config,
                spawnTime = slot.delay,
                warningEndTime = slot.delay + slot.config.warningDuration,
                judgeEndTime = slot.delay + slot.config.warningDuration + slot.config.judgeWindow,
                resolved = false,
                success = false
            });
        }

        // 初始化箭矢波追踪
        _arrowWaves.Clear();
        _arrowWavesSpawned = false;
        _fixedEndTimer = -1f;
        _judgingSpeedApplied = false;  // Branched 慢放标记重置

        // 计算实际判定阶段时长 = max(slot.judgeEndTime)，fixedQteDuration 作为保底下限
        _effectiveJudgeDuration = 0f;
        foreach (var qte in _activeQTEs)
        {
            if (qte.judgeEndTime > _effectiveJudgeDuration)
                _effectiveJudgeDuration = qte.judgeEndTime;
        }
        if (_currentAttack.fixedQteDuration > _effectiveJudgeDuration)
            _effectiveJudgeDuration = _currentAttack.fixedQteDuration;
        _endAnimTimer = -1f;

        enemy.EnterQTEAttack();
        StartQTEAnimation();

        // 多段防御型 QTE：不生成传统飞行物，改用箭矢波
        if (!(_currentAttack.UseMultiPhaseAnimation && _currentAttack.isDefensiveQTE && _currentAttack.arrowPrefab != null))
            SpawnProjectile();

        OnQTETriggered?.Invoke();
        return true;
    }

    private void SpawnProjectile()
    {
        if (_currentAttack.projectilePrefab == null) return;

        _activeProjectile = Instantiate(_currentAttack.projectilePrefab);
        _activeProjectile.transform.position = enemy.transform.position;

        var projectile = _activeProjectile.GetComponent<QTEProjectile>();
        if (projectile != null)
        {
            Vector3 targetPos = GetProjectileTargetPosition();
            projectile.Initialize(_currentAttack.projectileFlightTime, targetPos, OnProjectileReachedTarget);
        }
        else
        {
            // 无 QTEProjectile 组件的普通 prefab：直接飞过去
            Vector3 targetPos = GetProjectileTargetPosition();
            _activeProjectile.transform.DOMove(targetPos, _currentAttack.projectileFlightTime)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    OnProjectileReachedTarget();
                    if (_activeProjectile != null)
                    {
                        Destroy(_activeProjectile);
                        _activeProjectile = null;
                    }
                });
        }
    }

    private Vector3 GetProjectileTargetPosition()
    {
        float offsetZ = StageController.Instance != null ? StageController.Instance.GetFormationOffsetZ() : 0f;
        float zPos = offsetZ + _currentAttack.projectileTargetZ;
        return new Vector3(0, 1.5f, zPos);
    }

    private void OnProjectileReachedTarget()
    {
        StartQTEPhase();
    }

    private void StartQTEPhase()
    {
        if (_qtePhaseStarted) return;
        _qtePhaseStarted = true;
        _qtePhaseTimer = 0f;
    }

    private void KillProjectileSequence()
    {
        if (_activeProjectile != null)
            DOTween.Kill(_activeProjectile);
    }

    #region 箭矢波（多段防御型 QTE）

    private void SpawnArrowWaveForSlot(int slotIndex)
    {
        if (_currentAttack == null || _currentAttack.arrowPrefab == null) return;
        if (_arrowWaves.ContainsKey(slotIndex)) return;
        if (slotIndex < 0 || slotIndex >= _activeQTEs.Count) return;

        var qte = _activeQTEs[slotIndex];
        if (qte.config == null) return;

        float row5Z = GetRow5ZPosition();
        float spawnZ = row5Z + _currentAttack.arrowSpawnOffsetZ;
        float offsetZ = StageController.Instance != null ? StageController.Instance.GetFormationOffsetZ() : 0f;
        float targetZ = offsetZ + _currentAttack.projectileTargetZ;
        float playerX = PlayerState.Instance != null ? PlayerState.Instance.transform.position.x : 0f;

        var wave = new List<EnemyProjectile>();
        int count = _currentAttack.arrowsPerWave;
        float baseDmg = qte.config.failureDamage > 0f ? qte.config.failureDamage : qte.config.poiseDamage;
        float dmgPerArrow = baseDmg / count;

        // 从全局配置读取参数，未配置则用默认值
        float jitter = arrowConfig != null ? arrowConfig.randomPositionJitter : 0.3f;
        float flightVar = arrowConfig != null ? arrowConfig.randomFlightVariation : 0.1f;
        float arcVar = arrowConfig != null ? arrowConfig.randomArcVariation : 0.15f;
        float staggerMax = arrowConfig != null ? arrowConfig.staggerMax : 0.12f;
        float pitchAngle = arrowConfig != null ? arrowConfig.GetPitchAngleForRow(5) : 20f;
        float descentRatio = arrowConfig != null ? arrowConfig.descentPitchRatio : 0.75f;

        for (int i = 0; i < count; i++)
        {
            float xOffset = count > 1 ? Mathf.Lerp(-_currentAttack.arrowSpreadX * 0.5f, _currentAttack.arrowSpreadX * 0.5f, (float)i / (count - 1)) : 0f;
            float spawnX = playerX + xOffset + Random.Range(-jitter, jitter);
            float spawnY = 1.5f + Random.Range(-jitter * 0.67f, jitter);
            float spawnZJitter = Random.Range(-jitter * 0.67f, jitter * 0.67f);
            Vector3 spawnPos = new Vector3(spawnX, spawnY, spawnZ + spawnZJitter);

            float flightTime = (qte.config.warningDuration + qte.config.judgeWindow) * Random.Range(1f - flightVar, 1f + flightVar);
            float arcH = _currentAttack.arrowArcHeight * Random.Range(1f - arcVar, 1f + arcVar);
            float stagger = Random.Range(0f, staggerMax);

            var arrowObj = Instantiate(_currentAttack.arrowPrefab, spawnPos, Quaternion.identity);
            var projectile = arrowObj.GetComponent<EnemyProjectile>();
            if (projectile != null)
            {
                projectile.isQTEProjectile = true;
                if (stagger > 0.001f)
                {
                    float d = dmgPerArrow;
                    float tz = targetZ;
                    float sx = spawnX;
                    EnemyProjectile p = projectile;
                    float dr = descentRatio;
                    DOVirtual.DelayedCall(stagger, () =>
                    {
                        if (p != null) p.Launch(spawnPos, tz, sx, d, arcH, flightTime, null, pitchAngle, dr, _currentAttack.arrowTargetY);
                    });
                }
                else
                {
                    projectile.Launch(spawnPos, targetZ, spawnX, dmgPerArrow, arcH, flightTime, null, pitchAngle, descentRatio, _currentAttack.arrowTargetY);
                }
            }
            wave.Add(projectile);
        }

        _arrowWaves[slotIndex] = wave;
        _arrowWavesSpawned = true;
    }

    private void DeflectArrowWave(int slotIndex)
    {
        if (!_arrowWaves.TryGetValue(slotIndex, out var wave)) return;
        foreach (var p in wave)
        {
            if (p != null) p.Deflect();
        }
        _arrowWaves.Remove(slotIndex);
    }

    private void ClearAllArrowWaves()
    {
        foreach (var kv in _arrowWaves)
        {
            foreach (var p in kv.Value)
            {
                if (p != null) Destroy(p.gameObject);
            }
        }
        _arrowWaves.Clear();
    }

    private float GetRow5ZPosition()
    {
        float offsetZ = StageController.Instance != null ? StageController.Instance.GetFormationOffsetZ() : 0f;
        float rowSpacing = StageController.Instance != null ? StageController.Instance.GetRowSpacing() : 2.5f;
        int maxRow = StageController.Instance != null ? StageController.Instance.GetMaxVisibleRows() - 1 : 4;
        // 使用与 Enemy.GetRowZ 一致的公式: (maxRow - row) * (-spacing) + offset
        return (maxRow - 5) * (-rowSpacing) + offsetZ;
    }

    #endregion

    #region QTE 动画

    private void StartQTEAnimation()
    {
        if (_currentAttack == null)
        {
            DebugLog.Info("[QTEController] StartQTEAnimation: _currentAttack 为 null, 跳过");
            return;
        }
        if (_animator == null)
            _animator = GetComponent<Animator>();
        if (_animator == null)
        {
            DebugLog.Info("[QTEController] StartQTEAnimation: Animator 组件缺失, 跳过");
            return;
        }

        // 强制启用 Animator（防止上一轮 QTEEnd 后 Animator 被意外禁用）
        if (!_animator.enabled)
        {
            DebugLog.Info("[QTEController] StartQTEAnimation: Animator 被禁用，重新启用");
            _animator.enabled = true;
        }

        DebugLog.Info($"[QTEController] StartQTEAnimation: attackIndex={_currentAttackIndex}, enabled={_animator.enabled}, goActive={_animator.gameObject.activeInHierarchy}, currentState={_animator.GetCurrentAnimatorStateInfo(0).shortNameHash}");

        // 清除所有 QTE 相关 trigger 防止状态机残留
        _animator.ResetTrigger("QTETripleStab");
        _animator.ResetTrigger("QTESweep");
        _animator.ResetTrigger("QTEAttack");
        _animator.ResetTrigger("QTEEnd");
        _animator.ResetTrigger("QTEBlocked");
        _animator.ResetTrigger("QTEHit");

        // 强制回到Idle确保Animator从干净状态开始
        _animator.Play("Idle", 0, 0f);
        // BUG FIX: 某些Unity版本 Play+Update(0) 不完全重置，额外做一次
        _animator.Update(0f);
        _animator.Play("Idle", 0, 0f);

        var curState = _animator.GetCurrentAnimatorStateInfo(0);
        DebugLog.Info($"[QTEController] StartQTEAnimation: 重置后状态 IsName(Idle)={curState.IsName("Idle")}, shortNameHash={curState.shortNameHash}");

        if (_currentAttack.UseMultiPhaseAnimation)
        {
            _animator.SetTrigger("QTETripleStab");
            DebugLog.Info("[QTEController] 设置 QTETripleStab trigger");
        }
        else if (_currentAttack.UseBranchedAnimation)
        {
            _animator.SetTrigger("QTESweep");
            DebugLog.Info("[QTEController] 设置 QTESweep trigger");
            // 验证 trigger 是否被设置
            var nextState = _animator.GetCurrentAnimatorStateInfo(0);
            DebugLog.Info($"[QTEController] SetTrigger后状态: IsName(Idle)={nextState.IsName("Idle")}, shortNameHash={nextState.shortNameHash}");
        }
        else if (_currentAttack.qteAnimationClip != null)
        {
            _animator.SetTrigger("QTEAttack");
            DebugLog.Info("[QTEController] 设置 QTEAttack trigger");
        }
        else
        {
            DebugLog.Info("[QTEController] StartQTEAnimation: 无匹配动画类型");
        }
    }

    private void StopQTEAnimation()
    {
        if (_animator == null) return;
        _animator.speed = 1f;  // 恢复默认速度

        if (_currentAttack != null && (_currentAttack.UseMultiPhaseAnimation || _currentAttack.UseBranchedAnimation))
        {
            _animator.SetTrigger("QTEEnd");
        }
        else
        {
            _animator.Play("Idle");
        }
    }

    #endregion

    #endregion

    #region QTE 判定

    private void UpdatePerforming()
    {
        _performingTimer += Time.deltaTime;

        if (!_qtePhaseStarted)
        {
            // 无飞行物时，等待动画前摇结束后开始 QTE 阶段
            bool hasProjectile = _activeProjectile != null;
            // 多段动画模式：等待 Start 动画播完（动画前摇）
            bool multiPhaseReady = _currentAttack.UseMultiPhaseAnimation && _performingTimer >= _currentAttack.EffectiveLeadTime;
            // 单段动画模式：无飞行物 + 前摇结束
            bool singlePhaseReady = !_currentAttack.UseMultiPhaseAnimation && !hasProjectile && _performingTimer >= _currentAttack.EffectiveLeadTime;

            if (multiPhaseReady || singlePhaseReady)
                StartQTEPhase();
            // 有飞行物：等待 OnProjectileReachedTarget 回调
            return;
        }

        _qtePhaseTimer += Time.deltaTime;

        // 按 phase-relative 时间生成指示器 + 箭矢波
        foreach (var qte in _activeQTEs)
        {
            if (qte.indicator == null && _qtePhaseTimer >= qte.spawnTime)
            {
                SpawnQTEIndicator(qte);
                // 生成该 slot 对应的箭矢波
                SpawnArrowWaveForSlot(_activeQTEs.IndexOf(qte));
            }
        }

        // 检查判定窗口开始（通知 QTEDisplay 触发放大闪白）
        CheckJudgmentStart();

        // 所有 QTE 已到生成时间 → 进入判定阶段
        bool allReady = true;
        foreach (var qte in _activeQTEs)
        {
            if (qte.indicator == null && _qtePhaseTimer < qte.spawnTime)
            { allReady = false; break; }
        }
        if (allReady)
        {
            _state = QTEState.QTEJudging;
        }
    }

    private void UpdateJudging()
    {
        _qtePhaseTimer += Time.deltaTime;

        // Branched 模式：首个判定帧将 Happen 动画慢放以覆盖整个 QTE 窗口
        if (!_judgingSpeedApplied && _currentAttack != null && _currentAttack.UseBranchedAnimation && _animator != null)
        {
            _judgingSpeedApplied = true;
            float happenLength = _currentAttack.animationLoopClip != null ? _currentAttack.animationLoopClip.length : 0.5f;
            float window = _effectiveJudgeDuration > 0f ? _effectiveJudgeDuration : happenLength;
            float slowSpeed = happenLength / window;
            _animator.speed = Mathf.Clamp(slowSpeed, 0.05f, 1f);
        }

        // 所有 slot 已 resolved → 立即进入结束阶段，不等 judgeDuration 到期
        bool allResolved = true;
        foreach (var qte in _activeQTEs)
        {
            if (!qte.resolved) { allResolved = false; break; }
        }
        if (allResolved)
        {
            StartQTEEndingPhase();
            return;
        }

        // 判定阶段到期：强制结束，未 resolve 的 slot 视为失败，进入结束动画阶段
        float judgeDuration = _effectiveJudgeDuration;
        if (_qtePhaseTimer >= judgeDuration)
        {
            foreach (var qte in _activeQTEs)
            {
                if (!qte.resolved)
                    ResolveQTE(qte, false);
            }
            StartQTEEndingPhase();
            return;
        }

        // 检查判定窗口开始（通知 QTEDisplay 触发放大闪白）
        CheckJudgmentStart();

        // 检查到期的 slot 并自动失败（Branched 模式跳过 per-slot 过期）
        if (!(_currentAttack != null && _currentAttack.UseBranchedAnimation))
        {
            foreach (var qte in _activeQTEs)
            {
                if (!qte.resolved && qte.IsExpired(_qtePhaseTimer))
                    ResolveQTE(qte, false);
            }
        }
    }

    private void StartQTEEndingPhase()
    {
        _state = QTEState.QTEEnding;
        _endAnimTimer = 0f;

        // 判断玩家是否格挡成功（所有 slot 成功 = 格挡成功）
        bool playerBlocked = true;
        foreach (var qte in _activeQTEs)
        {
            if (!qte.resolved || !qte.success) { playerBlocked = false; break; }
        }

        float endClipLength = 0f;

        if (_currentAttack != null && _currentAttack.UseBranchedAnimation)
        {
            if (_animator == null) _animator = GetComponent<Animator>();
            if (_animator != null)
            {
                if (playerBlocked)
                {
                    // 加速播完剩余 Happen 帧，然后切到 Blocked
                    _animator.speed = 3f;
                    float accelerateWindow = 0.12f; // ~3-4 帧的加速时间
                    StartCoroutine(TriggerBlockedAfterAcceleration(accelerateWindow));
                }
                else
                {
                    // 失败：立即切到 Hit
                    _animator.speed = 1f;
                    _animator.SetTrigger("QTEHit");
                }
                endClipLength = _currentAttack.branchedResultDuration > 0f ? _currentAttack.branchedResultDuration : float.MaxValue;
            }
        }
        else
        {
            // 非 Branched 模式恢复速度
            if (_animator != null) _animator.speed = 1f;
            StopQTEAnimation();
            endClipLength = _currentAttack != null && _currentAttack.animationEndClip != null
                ? _currentAttack.animationEndClip.length : 0f;
        }

        if (endClipLength <= 0f)
        {
            CompleteQTEAttack();
            return;
        }
    }

    private System.Collections.IEnumerator TriggerBlockedAfterAcceleration(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (_animator != null)
        {
            _animator.SetTrigger("QTEBlocked");
            _animator.speed = 1f;
        }
    }

    private void UpdateEnding()
    {
        _endAnimTimer += Time.deltaTime;

        float endClipLength;
        if (_currentAttack != null && _currentAttack.UseBranchedAnimation)
        {
            // 分支动画模式：动画链由 AnimationEvent 主导，branchedResultDuration 兜底
            endClipLength = _currentAttack.branchedResultDuration > 0f
                ? _currentAttack.branchedResultDuration
                : float.MaxValue;
        }
        else
        {
            endClipLength = _currentAttack != null && _currentAttack.animationEndClip != null
                ? _currentAttack.animationEndClip.length : 0f;
        }

        if (_endAnimTimer >= endClipLength)
        {
            CompleteQTEAttack();
        }
    }

    public bool IsQTEActive
    {
        get
        {
            // QTE 阶段一旦开始，所有输入均被 QTE 系统拦截，防止误触普通攻击
            return _state == QTEState.QTEJudging || _state == QTEState.PerformingQTEAttack || _state == QTEState.QTEEnding;
        }
    }

    public bool TryQTEClick(Vector2 screenPos)
    {
        if (_state != QTEState.QTEJudging && _state != QTEState.PerformingQTEAttack) return false;
        if (!_qtePhaseStarted)
        {
            DebugLog.Info($"[QTEController] TryQTEClick 拒绝: _qtePhaseStarted=false");
            return false;
        }

        foreach (var qte in _activeQTEs)
        {
            if (qte.resolved || qte.config.qteType != QTEType.Click) continue;

            if (!qte.IsInJudgeWindow(_qtePhaseTimer)) continue;

            if (IsClickInQTEArea(screenPos, qte))
            {
                DebugLog.Info($"[QTEController] 点击QTE成功 idx={_activeQTEs.IndexOf(qte)}");
                ResolveQTE(qte, true);
                return true;
            }
        }
        DebugLog.Info($"[QTEController] TryQTEClick 未命中任何指示器 screenPos={screenPos}");
        return false;
    }

    public bool TryQTESwipe(Vector2 startScreenPos, Vector2 swipeDirection, float swipeSpeed, Vector2 releaseScreenPos)
    {
        if (_state != QTEState.QTEJudging && _state != QTEState.PerformingQTEAttack) return false;
        if (!_qtePhaseStarted) return false;

        foreach (var qte in _activeQTEs)
        {
            if (qte.resolved || qte.config.qteType != QTEType.Swipe) continue;

            if (!qte.IsInJudgeWindow(_qtePhaseTimer)) continue;

            if (swipeSpeed < qte.config.swipeMinSpeed)
            {
                DebugLog.Info($"[QTEController] 划动速度不足: {swipeSpeed:F0} < {qte.config.swipeMinSpeed}");
                continue;
            }

            // 检查划动是否经过指示器区域
            Rect? indicatorRect = GetIndicatorScreenRect(qte);
            if (indicatorRect == null) continue;

            if (!LineIntersectsRect(startScreenPos, releaseScreenPos, indicatorRect.Value))
                continue;

            float targetAngle = qte.config.swipeDirection;
            float swipeAngle = Mathf.Atan2(swipeDirection.y, swipeDirection.x) * Mathf.Rad2Deg;
            if (swipeAngle < 0f) swipeAngle += 360f;

            // 双向匹配：接受目标方向及其反方向（用户可能从指示器两侧划入）
            float diff = Mathf.Abs(Mathf.DeltaAngle(swipeAngle, targetAngle));
            float diffOpposite = Mathf.Abs(Mathf.DeltaAngle(swipeAngle, targetAngle + 180f));
            float bestDiff = Mathf.Min(diff, diffOpposite);
            if (bestDiff <= qte.config.swipeAngleTolerance)
            {
                DebugLog.Info($"[QTEController] 划动匹配成功: angle={swipeAngle:F1}° target={targetAngle}° diff={bestDiff:F1}° tol={qte.config.swipeAngleTolerance}°");
                ResolveQTE(qte, true);
                return true;
            }
            else
            {
                DebugLog.Info($"[QTEController] 划动角度偏差过大: bestDiff={bestDiff:F1}° > tol={qte.config.swipeAngleTolerance}° (swipe={swipeAngle:F1}° target={targetAngle}°)");
            }
        }
        return false;
    }

    private Camera _qteCanvasCamera;
    private bool _qteCanvasCameraChecked;

    private Camera GetQTECanvasCamera()
    {
        if (!_qteCanvasCameraChecked)
        {
            _qteCanvasCameraChecked = true;
            if (qteDisplay == null) qteDisplay = FindObjectOfType<QTEDisplay>();
            if (qteDisplay != null)
            {
                var canvas = qteDisplay.GetComponent<Canvas>();
                if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
                    _qteCanvasCamera = canvas.worldCamera;
            }
        }
        return _qteCanvasCamera;
    }

    /// <summary>
    /// 获取 QTE 指示器在屏幕空间中的矩形
    /// </summary>
    private Rect? GetIndicatorScreenRect(QTEInstance qte)
    {
        if (qte.indicator == null) return null;
        var rt = qte.indicator.GetComponent<RectTransform>();
        if (rt == null) return null;

        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        var cam = GetQTECanvasCamera();
        if (cam != null)
        {
            for (int i = 0; i < 4; i++)
                corners[i] = cam.WorldToScreenPoint(corners[i]);
        }
        return new Rect(corners[0].x, corners[0].y,
            corners[2].x - corners[0].x,
            corners[2].y - corners[0].y);
    }

    /// <summary>
    /// 线段与矩形相交检测
    /// </summary>
    private bool LineIntersectsRect(Vector2 p1, Vector2 p2, Rect rect)
    {
        if (rect.Contains(p1) || rect.Contains(p2))
            return true;

        Vector2 bl = new Vector2(rect.xMin, rect.yMin);
        Vector2 tl = new Vector2(rect.xMin, rect.yMax);
        Vector2 tr = new Vector2(rect.xMax, rect.yMax);
        Vector2 br = new Vector2(rect.xMax, rect.yMin);

        return LinesIntersect(p1, p2, bl, tl)
            || LinesIntersect(p1, p2, tl, tr)
            || LinesIntersect(p1, p2, tr, br)
            || LinesIntersect(p1, p2, br, bl);
    }

    private bool LinesIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
    {
        Vector2 d1 = a2 - a1;
        Vector2 d2 = b2 - b1;
        float cross = d1.x * d2.y - d1.y * d2.x;
        if (Mathf.Approximately(cross, 0f)) return false;

        float t = ((b1.x - a1.x) * d2.y - (b1.y - a1.y) * d2.x) / cross;
        float u = ((b1.x - a1.x) * d1.y - (b1.y - a1.y) * d1.x) / cross;

        return t >= 0f && t <= 1f && u >= 0f && u <= 1f;
    }

    /// <summary>
    /// 检查 QTE 判定窗口开始（通知 QTEDisplay 触发放大闪白）
    /// </summary>
    private void CheckJudgmentStart()
    {
        foreach (var qte in _activeQTEs)
        {
            if (qte.resolved || qte.judgmentStarted) continue;
            if (_qtePhaseTimer >= qte.warningEndTime && qte.indicator != null)
            {
                qte.judgmentStarted = true;
                if (qteDisplay != null)
                    qteDisplay.OnJudgmentStart(qte.indicator);
            }
        }
    }

    private bool IsClickInQTEArea(Vector2 screenPos, QTEInstance qte)
    {
        if (qte.indicator == null) return false;

        // 使用指示器的 RectTransform 判断
        var rt = qte.indicator.GetComponent<RectTransform>();
        if (rt == null) return false;

        var cam = GetQTECanvasCamera();
        return RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, cam);
    }

    private void SpawnQTEIndicator(QTEInstance qte)
    {
        if (qteDisplay == null)
        {
            qteDisplay = FindObjectOfType<QTEDisplay>();
            if (qteDisplay == null)
            {
                Debug.LogWarning("[QTEController] 未找到 QTEDisplay");
                return;
            }
            DebugLog.Info($"[QTEController] 找到 QTEDisplay: {qteDisplay.gameObject.name}");
        }
        DebugLog.Info($"[QTEController] 生成指示器: type={qte.config.qteType}, prefab={qte.config.qteIndicatorPrefab?.name}, pos={qte.config.screenPosition}");
        qte.indicator = qteDisplay.SpawnIndicator(qte.config);
        if (qte.indicator != null)
            DebugLog.Info($"[QTEController] 指示器已生成: {qte.indicator.name}, active={qte.indicator.activeSelf}, parent={qte.indicator.transform.parent?.name}");
        else
            Debug.LogWarning("[QTEController] SpawnIndicator返回null!");
    }

    private void ResolveQTE(QTEInstance qte, bool success, bool earlyFail = false)
    {
        DebugLog.Info($"[QTE_DIAG] ResolveQTE: success={success}, earlyFail={earlyFail}, indicatorId={qte.indicator?.GetInstanceID()}, remainingUnresolved={_activeQTEs.FindAll(q => !q.resolved).Count}");
        qte.resolved = true;
        qte.success = success;

        if (success)
        {
            OnQTESuccessSingle(qte);
        }
        else
        {
            OnQTEFailureSingle(qte);
        }

        // 通知 QTEDisplay 播放结果特效
        if (qteDisplay != null && qte.indicator != null)
        {
            if (earlyFail)
                qteDisplay.CancelIndicatorEarly(qte.indicator);
            else
                qteDisplay.ShowQTEResult(qte.indicator, success);
        }
    }

    private void OnQTESuccessSingle(QTEInstance qte)
    {
        // 防御型 QTE：弹反该 slot 对应的箭矢波，不造成 poise 伤害
        if (_currentAttack != null && _currentAttack.isDefensiveQTE)
        {
            DeflectArrowWave(_activeQTEs.IndexOf(qte));
            PlayBlockVisual();
        }
        else
        {
            if (enemy != null && _currentAttack != null)
                enemy.TakeQTEPoiseDamage(qte.config.poiseDamage, _currentAttack.interruptibleOnStun);
        }

        if (UltimateSystem.Instance != null)
            UltimateSystem.Instance.AddEnergy(qte.config.ultimateEnergyGain);

        OnQTESuccess?.Invoke();

        // QTE 格挡成功音效
        if (AudioManager.Instance != null)
            AudioManager.Instance.PostEvent("QTE_Block");
        else
            DebugLog.Info("[QTEController] AudioManager.Instance 为 null，跳过 QTE_Block 音效");

        // Handheld.Vibrate(); // 安卓端攻击震动暂关闭
    }

    private void OnQTEFailureSingle(QTEInstance qte)
    {
        // 防御型 QTE：箭矢波继续飞行（已在飞行中，无需额外操作）
        // 非防御型：失败伤害延迟到 CompleteQTEAttack 时应用
        OnQTEFailure?.Invoke();
    }

    #endregion

    #region Sweep 动画回调

    /// <summary>
    /// 由 End2 AnimationEvent 回调，通知 QTE 结果动画播放完毕
    /// </summary>
    public void OnSweepResultAnimationEnd()
    {
        if (_state == QTEState.QTEEnding)
        {
            DebugLog.Info("[QTEController] OnSweepResultAnimationEnd → CompleteQTEAttack");
            CompleteQTEAttack();
        }
    }

    #endregion

    #region 格挡表现

    /// <summary>
    /// 播放 QTE 格挡成功表现：矛从下方举起并旋转格挡箭矢
    /// 以摄像机为父节点：X 轴 90°→0°（举起）+ Z 轴 0°→360°（自转）+ 三帧精灵切换
    /// </summary>
    private void PlayBlockVisual()
    {
        if (stabBlockEffectPrefab == null) return;

        var cam = Camera.main;
        if (cam == null) return;

        // 父节点：负责 X 轴旋转（举起动作）
        var parent = new GameObject("QTE_BlockVFX");
        parent.transform.SetParent(cam.transform, false);
        parent.transform.localPosition = new Vector3(0f, 0f, stabBlockDistance);
        parent.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        // 子节点：实例化矛 prefab，负责 Z 轴自转
        var visual = Instantiate(stabBlockEffectPrefab, parent.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = stabBlockScale;
        visual.transform.localRotation = Quaternion.identity;

        var sr = visual.GetComponentInChildren<SpriteRenderer>();
        if (sr == null) sr = visual.GetComponent<SpriteRenderer>();
        if (sr != null && stabBlockSprite != null)
            sr.sprite = stabBlockSprite;

        float duration = stabBlockDuration;

        // X 轴：90° → 0°
        parent.transform.DOLocalRotate(new Vector3(0f, 0f, 0f), duration).SetEase(Ease.InOutQuad);

        // Z 轴：0° → 360° 自转
        visual.transform.DOLocalRotate(new Vector3(0f, 0f, 360f), duration, RotateMode.FastBeyond360).SetEase(Ease.Linear);

        // 三帧精灵均匀切换
        if (sr != null && stabBlockRotateSprite1 != null && stabBlockRotateSprite2 != null)
        {
            float third = duration / 3f;
            var spriteSeq = DOTween.Sequence();
            spriteSeq.SetTarget(visual);
            spriteSeq.AppendInterval(third);
            spriteSeq.AppendCallback(() => { if (sr != null) sr.sprite = stabBlockRotateSprite1; });
            spriteSeq.AppendInterval(third);
            spriteSeq.AppendCallback(() => { if (sr != null) sr.sprite = stabBlockRotateSprite2; });
        }

        // 动画结束后销毁
        DOVirtual.DelayedCall(duration + 0.05f, () =>
        {
            if (parent != null) Destroy(parent);
        });
    }

    #endregion

    #region QTE 攻击收尾

    /// <summary>
    /// 中止 QTE（敌人被打入 Stun/Launch/Dead 等非 QTEAttacking 状态）
    /// </summary>
    public void AbortQTE()
    {
        // 清理飞行物
        if (_activeProjectile != null)
        {
            var proj = _activeProjectile.GetComponent<QTEProjectile>();
            if (proj != null) proj.ContinuePassThrough(0.5f, null);
            else Destroy(_activeProjectile);
            _activeProjectile = null;
        }

        // 清理箭矢波
        ClearAllArrowWaves();

        StopQTEAnimation();

        DebugLog.Info("[QTE_DIAG] AbortQTE: clearing indicators");
        if (qteDisplay != null)
            qteDisplay.ClearAllIndicators();

        _activeQTEs.Clear();
        _state = QTEState.Idle;

        // BUG FIX: AbortQTE 需要确保 BOSS 设置冷却并回到 Idle，否则下一帧立即重新触发 QTE
        enemy.ExitQTEAttack();

        OnQTEAttackFinished?.Invoke();
        DebugLog.Info("[QTEController] QTE已中止");
    }

    private void CompleteQTEAttack()
    {
        _state = QTEState.QTECompleted;

        bool isDefensive = _currentAttack != null && _currentAttack.isDefensiveQTE;

        // 收集 QTE 失败伤害（非防御型才在此汇总，防御型由箭矢 OnArrival 处理）
        if (!isDefensive)
        {
            float totalFailureDamage = 0f;
            bool anyFailed = false;
            foreach (var qte in _activeQTEs)
            {
                if (qte.resolved && !qte.success)
                {
                    anyFailed = true;
                    totalFailureDamage += qte.config.failureDamage;
                }
            }
            if (totalFailureDamage > 0f && PlayerState.Instance != null)
            {
                PlayerState.Instance.TakeDamage(totalFailureDamage);
                DebugLog.Info($"[QTEController] QTE失败伤害: {totalFailureDamage:F0}");
            }

            // 飞行物处理
            if (_activeProjectile != null)
            {
                var proj = _activeProjectile.GetComponent<QTEProjectile>();
                if (anyFailed && proj != null)
                {
                    proj.ContinuePassThrough(0.8f, null);
                }
                else if (proj != null)
                {
                    proj.DestroyOnSuccess();
                }
                else
                {
                    Destroy(_activeProjectile);
                }
                _activeProjectile = null;
            }
        }

        // 通知敌人恢复
        enemy.ExitQTEAttack();

        // 停止 QTE 精灵动画
        StopQTEAnimation();

        // 清理指示器
        DebugLog.Info($"[QTE_DIAG] CompleteQTEAttack: clearing indicators, attackIndex={_currentAttackIndex}");
        if (qteDisplay != null)
            qteDisplay.ClearAllIndicators();

        // 前进到下一个 QTE 攻击
        _currentAttackIndex++;
        if (_currentAttackIndex >= qteData.qteAttacks.Count)
        {
            if (qteData.loopAttacks)
                _currentAttackIndex = 0;
            else
            {
                _state = QTEState.Idle;
                OnQTECompleted?.Invoke();
                OnQTEAttackFinished?.Invoke();
                return;
            }
        }

        _state = QTEState.Idle;
        OnQTECompleted?.Invoke();
        OnQTEAttackFinished?.Invoke();
    }

    #endregion
}
