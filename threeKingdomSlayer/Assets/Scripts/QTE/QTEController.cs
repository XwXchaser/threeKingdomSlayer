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

public enum QTEInputRule
{
    LegacyPassThrough,
    Strict
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
    public bool earlyInputFailed; // Strict 输入已锁定失败，但等待该段攻击演出完成后再结算
    public bool failureFeedbackShown; // 失败图案和指示器退场已即时播放

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

    [Header("输入规则")]
    [SerializeField] private QTEInputRule _inputRule = QTEInputRule.LegacyPassThrough;

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
    [Tooltip("格挡效果摄像机空间 X 偏移")]
    public float stabBlockOffsetX = 0f;
    [Tooltip("格挡效果起始位置（负值=从下方扫入，正值=从上方扫入）")]
    public float stabBlockOffsetY = -1.5f;
    [Tooltip("格挡效果缩放")]
    public Vector3 stabBlockScale = new Vector3(0.05f, 0.05f, 0.05f);

    [Header("组件引用")]
    public Enemy enemy;
    public QTEDisplay qteDisplay;

    [Header("运行时状态")]
    [SerializeField] private QTEState _state = QTEState.Idle;

    private int _currentQTEIndex;
    private float _nextQTEStartTime;
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
    private bool _completionStarted;
    private int _qteGeneration;

    // 箭矢波追踪（多段防御型 QTE）：slotIndex → 该波所有箭矢
    private readonly Dictionary<int, List<EnemyProjectile>> _arrowWaves = new Dictionary<int, List<EnemyProjectile>>();
    private readonly List<Tween> _arrowLaunchDelays = new List<Tween>();
    private bool _arrowWavesSpawned;

    // QTE 动画
    private Animator _animator;
    private bool _judgingSpeedApplied;   // Branched 模式下是否已对 Happen 应用慢放速度
    private bool _strictInputLockApplied;
    private bool _failureDamagePending;
    private bool _activityRegistered;

    // 事件
    public System.Action OnQTETriggered;       // QTE 攻击触发
    public System.Action OnQTESuccess;         // QTE 判定成功
    public System.Action OnQTEFailure;         // QTE 判定失败
    public System.Action OnQTECompleted;       // 一轮 QTE 攻击结束
    public System.Action OnQTEAttackFinished;  // QTE 攻击完全结束，通知 Enemy 回到 Idle

    public QTEState State => _state;
    public QTEAttackConfig CurrentAttackConfig => _currentAttack;
    public QTEInputRule InputRule => _inputRule;
    public bool UsesStrictInputRule => _inputRule == QTEInputRule.Strict;
    public bool IsStrictInputActive => UsesStrictInputRule && IsQTEActive;

    public void SetInputRule(QTEInputRule inputRule)
    {
        if (_inputRule == inputRule) return;
        SetStrictInputLock(false);
        _inputRule = inputRule;
        if (UsesStrictInputRule && IsQTEActive)
            SetStrictInputLock(true);
    }

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

    private void RegisterQTEActivity()
    {
        Debug.Log($"[QTE_FLIP_DIAG] Controller Register request name={name}#{GetInstanceID()} registered={_activityRegistered} state={_state}");
        if (_activityRegistered) return;
        _activityRegistered = true;
        QTEActivityHub.Begin(this);
    }

    private void UnregisterQTEActivity()
    {
        Debug.Log($"[QTE_FLIP_DIAG] Controller Unregister request name={name}#{GetInstanceID()} registered={_activityRegistered} state={_state}");
        if (!_activityRegistered) return;
        _activityRegistered = false;
        QTEActivityHub.End(this);
    }

    private void OnDisable()
    {
        if (_state == QTEState.Idle && !_strictInputLockApplied) return;
        CleanupDisabledQTE();
    }

    private void OnDestroy()
    {
        CleanupDisabledQTE();
    }

    private void CleanupDisabledQTE()
    {
        UnregisterQTEActivity();
        _qteGeneration++;
        SetStrictInputLock(false);
        KillProjectileSequence();
        if (_activeProjectile != null)
        {
            Destroy(_activeProjectile);
            _activeProjectile = null;
        }
        ClearAllArrowWaves();
        if (_animator != null) _animator.speed = 1f;
        if (qteDisplay != null) qteDisplay.ClearAllIndicators();
        _activeQTEs.Clear();
        _currentAttack = null;
        _qtePhaseStarted = false;
        _state = QTEState.Idle;
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
        if (IsQTEActive && enemy != null && enemy.state == EnemyState.QTEAttacking)
            AbortQTE();
        else
            CleanupInactiveQTE();

        qteData = newData;
        _currentAttackIndex = 0;
        DebugLog.Info($"[QTEController] 切换QTE数据: {newData?.name}, state={_state}");
    }

    private void CleanupInactiveQTE()
    {
        UnregisterQTEActivity();
        _qteGeneration++;
        SetStrictInputLock(false);
        KillProjectileSequence();
        if (_activeProjectile != null)
        {
            Destroy(_activeProjectile);
            _activeProjectile = null;
        }
        ClearAllArrowWaves();
        if (_animator != null) _animator.speed = 1f;
        if (qteDisplay != null) qteDisplay.ClearAllIndicators();
        _activeQTEs.Clear();
        _currentAttack = null;
        _currentQTEIndex = 0;
        _qtePhaseStarted = false;
        _state = QTEState.Idle;
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
        _completionStarted = false;
        _qteGeneration++;
        DebugLog.Info($"[QTEController] 当前攻击: {_currentAttack?.name}, slots={_currentAttack?.qteSlots?.Count}, useMultiPhase={_currentAttack?.UseMultiPhaseAnimation}, useBranched={_currentAttack?.UseBranchedAnimation}");
        _state = QTEState.PerformingQTEAttack;
        RegisterQTEActivity();
        _performingTimer = 0f;
        _qtePhaseStarted = false;
        _qtePhaseTimer = 0f;

        // 清除上一轮可能残留的指示器（防御性清理，防止重叠）
        DebugLog.Info($"[QTE_DIAG] TriggerQTEAttack: clearing indicators before new attack, attackIndex={_currentAttackIndex}");
        if (qteDisplay != null)
            qteDisplay.ClearAllIndicators();

        _activeQTEs.Clear();
        _currentQTEIndex = 0;
        _nextQTEStartTime = 0f;

        // 创建 QTE 实例（顺序队列；delay 表示上一段结束后到下一段出现的间隔）
        foreach (var slot in _currentAttack.qteSlots)
        {
            if (slot.config == null) continue;
            _activeQTEs.Add(new QTEInstance
            {
                config = slot.config,
                spawnTime = slot.delay,
                warningEndTime = 0f,
                judgeEndTime = 0f,
                resolved = false,
                success = false
            });
        }

        // 清理上一轮残余箭矢波
        ClearAllArrowWaves();
        _arrowWavesSpawned = false;
        _fixedEndTimer = -1f;
        _failureDamagePending = false;
        _judgingSpeedApplied = false;  // Branched 慢放标记重置

        // 顺序单槽模式不再预先计算三个并列slot的总时长；每段按自身警示/判定结束。
        _effectiveJudgeDuration = 0f;
        _endAnimTimer = -1f;

        enemy.EnterQTEAttack();
        StartQTEAnimation();

        // 防御型 QTE：在每个指示器生成时发射对应箭矢波。
        // 箭矢飞行覆盖“指示器入场 + judgeWindow”，避免在前摇期间提前飞完。
        if (!(_currentAttack.UseMultiPhaseAnimation && _currentAttack.isDefensiveQTE && _currentAttack.arrowPrefab != null))
            SpawnProjectile();

        OnQTETriggered?.Invoke();
        if (UsesStrictInputRule)
        {
            if (qteDisplay == null)
                qteDisplay = UnityEngine.Object.FindObjectOfType<QTEDisplay>();
            qteDisplay?.ShowStrictModePrompt();
        }
        SetStrictInputLock(true);
        return true;
    }

    private void SetStrictInputLock(bool locked)
    {
        if (_inputRule != QTEInputRule.Strict || _strictInputLockApplied == locked) return;
        _strictInputLockApplied = locked;
        if (locked)
            ComboManager.Instance?.Freeze();
        else
            ComboManager.Instance?.Resume();
        SetBuffDisplayLocked(locked);
    }

    private static void SetBuffDisplayLocked(bool locked)
    {
        var panels = FindObjectsOfType<BuffDisplayPanel>();
        for (int i = 0; i < panels.Length; i++)
            panels[i].SetQTEInputLocked(locked);
    }

    private void SpawnProjectile()
    {
        if (_currentAttack.projectilePrefab == null) return;

        _activeProjectile = Instantiate(_currentAttack.projectilePrefab);
        EnemyProjectileVisualPriority.Apply(_activeProjectile);
        _activeProjectile.transform.position = enemy.transform.position;

        int generation = _qteGeneration;
        var spawnedProjectile = _activeProjectile;
        var projectile = spawnedProjectile.GetComponent<QTEProjectile>();
        if (projectile != null)
        {
            Vector3 targetPos = GetProjectileTargetPosition();
            projectile.Initialize(_currentAttack.projectileFlightTime, targetPos, () => OnProjectileReachedTarget(generation));
        }
        else
        {
            // 无 QTEProjectile 组件的普通 prefab：直接飞过去
            Vector3 targetPos = GetProjectileTargetPosition();
            spawnedProjectile.transform.DOMove(targetPos, _currentAttack.projectileFlightTime)
                .SetEase(Ease.Linear)
                .SetUpdate(UpdateType.Normal, false)
                .OnComplete(() =>
                {
                    OnProjectileReachedTarget(generation);
                    if (spawnedProjectile != null)
                    {
                        Destroy(spawnedProjectile);
                        if (_activeProjectile == spawnedProjectile)
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

    private void OnProjectileReachedTarget(int generation)
    {
        if (generation != _qteGeneration || !IsQTEActive) return;
        StartQTEPhase();
    }

    private void StartQTEPhase()
    {
        if (_qtePhaseStarted) return;
        _qtePhaseStarted = true;
        _qtePhaseTimer = 0f;
        _fixedEndTimer = _currentAttack != null && _currentAttack.fixedQteDuration > 0f
            ? _currentAttack.fixedQteDuration
            : -1f;
        _nextQTEStartTime = _currentQTEIndex < _activeQTEs.Count
            ? _activeQTEs[_currentQTEIndex].spawnTime
            : 0f;
    }

    private bool CanEndQTEPhase()
    {
        if (_fixedEndTimer >= 0f && _qtePhaseTimer < _fixedEndTimer)
            return false;

        if (_currentAttack == null || !_currentAttack.isDefensiveQTE)
            return true;

        foreach (var wave in _arrowWaves.Values)
        {
            for (int i = 0; i < wave.Count; i++)
            {
                if (wave[i] != null)
                    return false;
            }
        }
        return _arrowLaunchDelays.Count == 0;
    }

    private void KillProjectileSequence()
    {
        if (_activeProjectile != null)
            DOTween.Kill(_activeProjectile);
    }

    #region 箭矢波（多段防御型 QTE）

    private void SpawnArrowWaveForSlot(int slotIndex, float overrideFlightTime = -1f)
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

        float jitter = arrowConfig != null ? arrowConfig.randomPositionJitter : 0.3f;
        float arcVar = arrowConfig != null ? arrowConfig.randomArcVariation : 0.15f;
        float staggerMax = arrowConfig != null ? arrowConfig.staggerMax : 0.12f;
        float maxDescentPitch = arrowConfig != null ? arrowConfig.maxDescentPitch : 89f;

        // 计算飞行时间：优先使用显式传入值，否则从 warningEndTime 反推
        float baseFlightTime;
        if (overrideFlightTime >= 0f)
        {
            baseFlightTime = overrideFlightTime;
        }
        else
        {
            float timeUntilWarningEnd = qte.warningEndTime - _qtePhaseTimer;
            baseFlightTime = Mathf.Max(timeUntilWarningEnd, 0.25f);
        }

        // 弧高钳制到可见范围（配置值可能过大，此处做视觉补偿）
        float arcHeightCap = 3.5f;
        float rawArcH = _currentAttack.arrowArcHeight;
        float arcH = Mathf.Min(rawArcH, arcHeightCap) * Random.Range(1f - arcVar, 1f + arcVar);

        for (int i = 0; i < count; i++)
        {
            float xOffset = count > 1 ? Mathf.Lerp(-_currentAttack.arrowSpreadX * 0.5f, _currentAttack.arrowSpreadX * 0.5f, (float)i / (count - 1)) : 0f;
            float spawnX = playerX + xOffset + Random.Range(-jitter, jitter);
            float spawnY = 1.5f + Random.Range(-jitter * 0.67f, jitter);
            float spawnZJitter = Random.Range(-jitter * 0.67f, jitter * 0.67f);
            Vector3 spawnPos = new Vector3(spawnX, spawnY, spawnZ + spawnZJitter);

            // 仅保留发射错峰；所有箭矢按同一绝对到达时刻计算飞行时间。
            float stagger = Random.Range(0f, staggerMax);
            float flightTime = Mathf.Max(baseFlightTime - stagger, 0.2f);

            var arrowObj = Instantiate(_currentAttack.arrowPrefab, spawnPos, Quaternion.identity);
            EnemyProjectileVisualPriority.Apply(arrowObj);
            var projectile = arrowObj.GetComponent<EnemyProjectile>();
            if (projectile != null)
            {
                projectile.isQTEProjectile = true;
                if (stagger > 0.001f)
                {
                    arrowObj.SetActive(false);
                    float d = dmgPerArrow;
                    float tz = targetZ;
                    float sx = spawnX;
                    float ah = arcH;
                    float ft = flightTime;
                    EnemyProjectile p = projectile;
                    Tween launchDelay = null;
                    launchDelay = DOVirtual.DelayedCall(stagger, () =>
                    {
                        _arrowLaunchDelays.Remove(launchDelay);
                        if (p != null)
                        {
                            p.gameObject.SetActive(true);
                            p.Launch(spawnPos, tz, sx, d, ah, ft, null,
                                _currentAttack.arrowTargetY, maxDescentPitch);
                        }
                    });
                    _arrowLaunchDelays.Add(launchDelay);
                }
                else
                {
                    projectile.Launch(spawnPos, targetZ, spawnX, dmgPerArrow, arcH, flightTime, null,
                        _currentAttack.arrowTargetY, maxDescentPitch);
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
    }

    private void ClearAllArrowWaves()
    {
        for (int i = 0; i < _arrowLaunchDelays.Count; i++)
        {
            if (_arrowLaunchDelays[i] != null && _arrowLaunchDelays[i].IsActive())
                _arrowLaunchDelays[i].Kill();
        }
        _arrowLaunchDelays.Clear();

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
            bool hasProjectile = _activeProjectile != null;
            bool multiPhaseReady = _currentAttack.UseMultiPhaseAnimation && _performingTimer >= _currentAttack.EffectiveLeadTime;
            bool singlePhaseReady = !_currentAttack.UseMultiPhaseAnimation && !hasProjectile && _performingTimer >= _currentAttack.EffectiveLeadTime;
            if (multiPhaseReady || singlePhaseReady)
                StartQTEPhase();
            return;
        }

        _qtePhaseTimer += Time.deltaTime;
        if (_currentQTEIndex >= _activeQTEs.Count)
        {
            if (CanEndQTEPhase())
                StartQTEEndingPhase();
            return;
        }

        var qte = _activeQTEs[_currentQTEIndex];
        if (qte.indicator == null && _qtePhaseTimer >= _nextQTEStartTime)
        {
            if (!qte.earlyInputFailed)
                SpawnQTEIndicator(qte);

            if (UsesStrictInputRule && qteDisplay != null)
            {
                // 落位前仅用于阶段2失败的预计结束时刻；实际落位时会以真实时刻重设完整窗口。
                qte.warningEndTime = _qtePhaseTimer + qteDisplay.slideInDuration;
                qte.judgeEndTime = qte.warningEndTime + qte.config.judgeWindow;
                qte.judgmentStarted = false;
            }
            else
            {
                // V1 保持图案生成即进入判定。
                qte.warningEndTime = _qtePhaseTimer;
                qte.judgeEndTime = qte.warningEndTime + qte.config.judgeWindow;
                qte.judgmentStarted = true;
            }

            // 严格模式的箭矢飞行覆盖图案入场和完整判定窗口。
            SpawnArrowWaveForSlot(_currentQTEIndex, qte.judgeEndTime - _qtePhaseTimer);
            _state = QTEState.QTEJudging;

            if (qte.earlyInputFailed)
            {
                DebugLog.Info($"[QTEController] 提前失败段按原时序演出 idx={_currentQTEIndex}, end={qte.judgeEndTime:F2}");
            }
        }
    }

    private void UpdateJudging()
    {
        _qtePhaseTimer += Time.deltaTime;
        if (_currentQTEIndex >= _activeQTEs.Count)
        {
            if (CanEndQTEPhase())
                StartQTEEndingPhase();
            return;
        }

        var qte = _activeQTEs[_currentQTEIndex];
        TryOpenStrictJudgmentWindow(qte);

        if (!_judgingSpeedApplied && _currentAttack != null && _currentAttack.UseBranchedAnimation && _animator != null && qte.judgmentStarted)
        {
            _judgingSpeedApplied = true;
            float happenLength = _currentAttack.animationLoopClip != null ? _currentAttack.animationLoopClip.length : 0.5f;
            _animator.speed = Mathf.Clamp(happenLength / qte.config.judgeWindow, 0.05f, 1f);
        }

        CheckJudgmentStart();
        if (qte.earlyInputFailed)
        {
            if (_qtePhaseTimer > qte.judgeEndTime)
                ResolveQTE(qte, false);
            return;
        }
        if (!qte.resolved && qte.IsExpired(_qtePhaseTimer))
            ResolveQTE(qte, false);
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
            if (_animator == null)
            {
                CompleteQTEAttack();
                return;
            }

            if (playerBlocked)
            {
                // 加速播完剩余 Happen 帧，然后切到 Blocked
                _animator.speed = 3f;
                float accelerateWindow = 0.12f; // ~3-4 帧的加速时间
                StartCoroutine(TriggerBlockedAfterAcceleration(accelerateWindow, _qteGeneration));
            }
            else
            {
                // 失败：立即切到 Hit
                _animator.speed = 1f;
                _animator.SetTrigger("QTEHit");
            }
            endClipLength = _currentAttack.branchedResultDuration > 0f ? _currentAttack.branchedResultDuration : float.MaxValue;
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

        DebugLog.Info($"[QTEController] 进入QTE收尾: resultDuration={endClipLength:F2}");
    }

    private System.Collections.IEnumerator TriggerBlockedAfterAcceleration(float delay, int generation)
    {
        yield return new WaitForSeconds(delay);
        if (generation != _qteGeneration || _state != QTEState.QTEEnding) yield break;
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
            DebugLog.Info($"[QTEController] QTE收尾兜底完成: elapsed={_endAnimTimer:F2}");
            CompleteQTEAttack();
        }
    }

    private bool TryOpenStrictJudgmentWindow(QTEInstance qte)
    {
        if (!UsesStrictInputRule || qte == null || qte.judgmentStarted || qte.indicator == null || qteDisplay == null)
            return qte != null && qte.judgmentStarted;
        if (!qteDisplay.HasLanded(qte.indicator))
            return false;

        qte.warningEndTime = _qtePhaseTimer;
        qte.judgeEndTime = _qtePhaseTimer + qte.config.judgeWindow;
        qte.judgmentStarted = true;
        DebugLog.Info($"[QTEController] 严格模式图案落位，开始判定 idx={_currentQTEIndex}, end={qte.judgeEndTime:F2}");
        return true;
    }

    public bool IsQTEActive
    {
        get
        {
            // QTE 阶段一旦开始，所有输入均被 QTE 系统拦截，防止误触普通攻击
            return _state == QTEState.QTEJudging || _state == QTEState.PerformingQTEAttack || _state == QTEState.QTEEnding;
        }
    }

    public bool TryStrictInput(Vector2 startScreenPos, Vector2 releaseScreenPos, bool isSwiped, float swipeDistance, float pressDuration)
    {
        // Strict 模式在整个 QTE 生命周期内吞掉战斗手势；收尾或已结算时不再回退为普通攻击。
        if (!IsStrictInputActive)
            return false;
        if (_currentQTEIndex >= _activeQTEs.Count)
            return true;

        var qte = _activeQTEs[_currentQTEIndex];
        if (qte.resolved || qte.earlyInputFailed) return true;

        if (!_qtePhaseStarted || qte.indicator == null)
            return true;

        if (!qte.judgmentStarted && !TryOpenStrictJudgmentWindow(qte))
        {
            qte.earlyInputFailed = true;
            DebugLog.Info($"[QTEController] 严格模式图案入场期间输入失败 idx={_currentQTEIndex}");
            ShowImmediateFailureFeedback(qte);
            return true;
        }

        if (!qte.IsInJudgeWindow(_qtePhaseTimer))
            return true;

        bool success = false;
        if (isSwiped)
        {
            Vector2 direction = releaseScreenPos - startScreenPos;
            float swipeSpeed = swipeDistance / Mathf.Max(pressDuration, 0.001f);
            success = TryQTESwipe(startScreenPos, direction, swipeSpeed, releaseScreenPos);
        }
        else
        {
            success = TryQTEClick(releaseScreenPos);
        }

        if (!success && !qte.resolved)
        {
            DebugLog.Info($"[QTEController] 严格模式错误输入锁定失败 idx={_currentQTEIndex}");
            qte.earlyInputFailed = true;
            ShowImmediateFailureFeedback(qte);
        }
        return true;
    }

    private static bool MatchesQTEGestureType(QTEConfig config, bool isSwiped)
    {
        return config != null && (config.qteType == QTEType.Swipe) == isSwiped;
    }

    private void ShowImmediateFailureFeedback(QTEInstance qte)
    {
        if (qte == null || qte.failureFeedbackShown) return;
        qte.failureFeedbackShown = true;

        if (qteDisplay == null)
            qteDisplay = UnityEngine.Object.FindObjectOfType<QTEDisplay>();
        if (qteDisplay == null) return;

        if (qte.indicator != null)
        {
            qteDisplay.FlashIndicatorFailure(qte.indicator);
            qteDisplay.ShowQTEResult(qte.indicator, false);
            qte.indicator = null;
        }
        else
        {
            qteDisplay.ShowResultFeedback(false);
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

        var qte = _activeQTEs[_currentQTEIndex];
        if (qte.resolved || qte.config.qteType != QTEType.Click) return false;
        if (!qte.IsInJudgeWindow(_qtePhaseTimer)) return false;

        if (IsClickInQTEArea(screenPos, qte))
        {
            DebugLog.Info($"[QTEController] 点击QTE成功 idx={_currentQTEIndex}");
            ResolveQTE(qte, true);
            return true;
        }
        DebugLog.Info($"[QTEController] TryQTEClick 未命中当前指示器 screenPos={screenPos}");
        return false;
    }

    public bool TryQTESwipe(Vector2 startScreenPos, Vector2 swipeDirection, float swipeSpeed, Vector2 releaseScreenPos)
    {
        if (_state != QTEState.QTEJudging && _state != QTEState.PerformingQTEAttack) return false;
        if (!_qtePhaseStarted) return false;

        var qte = _activeQTEs[_currentQTEIndex];
        if (qte.resolved || qte.config.qteType != QTEType.Swipe) return false;
        if (!qte.IsInJudgeWindow(_qtePhaseTimer)) return false;
        Rect? indicatorRect = GetIndicatorScreenRect(qte);
        if (indicatorRect == null || !LineIntersectsRect(startScreenPos, releaseScreenPos, indicatorRect.Value))
            return false;

        if (swipeSpeed < qte.config.swipeMinSpeed)
        {
            DebugLog.Info($"[QTEController] 划动速度不足: {swipeSpeed:F0} < {qte.config.swipeMinSpeed}");
            return false;
        }

        float targetAngle = qte.config.swipeDirection;
        float swipeAngle = Mathf.Atan2(swipeDirection.y, swipeDirection.x) * Mathf.Rad2Deg;
        if (swipeAngle < 0f) swipeAngle += 360f;
        float bestDiff = Mathf.Min(Mathf.Abs(Mathf.DeltaAngle(swipeAngle, targetAngle)), Mathf.Abs(Mathf.DeltaAngle(swipeAngle, targetAngle + 180f)));
        if (bestDiff > qte.config.swipeAngleTolerance)
        {
            DebugLog.Info($"[QTEController] 划动角度偏差过大: bestDiff={bestDiff:F1}° > tol={qte.config.swipeAngleTolerance}°");
            return false;
        }

        DebugLog.Info($"[QTEController] 划动匹配成功: idx={_currentQTEIndex}");
        PlaySwipeInputVisual(qte);
        ResolveQTE(qte, true);
        return true;
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
    /// 检查 QTE 判定窗口开始（标记 judgmentStarted，指示器落位后自动进入判定）
    /// </summary>
    private void CheckJudgmentStart()
    {
        if (!UsesStrictInputRule)
        {
            foreach (var qte in _activeQTEs)
            {
                if (qte.resolved || qte.judgmentStarted) continue;
                if (_qtePhaseTimer >= qte.warningEndTime && qte.indicator != null)
                    qte.judgmentStarted = true;
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
            qteDisplay = UnityEngine.Object.FindObjectOfType<QTEDisplay>();
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

    private void ResolveQTE(QTEInstance qte, bool success)
    {
        DebugLog.Info($"[QTE_DIAG] ResolveQTE: success={success}, indicatorId={qte.indicator?.GetInstanceID()}, remainingUnresolved={_activeQTEs.FindAll(q => !q.resolved).Count}");
        qte.resolved = true;
        qte.success = success;

        if (success)
        {
            OnQTESuccessSingle(qte);
        }
        else
        {
            OnQTEFailureSingle(qte);
            if (_currentAttack != null && !_currentAttack.isDefensiveQTE)
                _failureDamagePending = true;
            if (qteDisplay != null && qte.indicator != null)
                qteDisplay.FlashIndicatorFailure(qte.indicator);
        }

        // 通知 QTEDisplay 播放结果特效；提前/错误输入已即时播放失败反馈时不重复。
        if (qteDisplay != null && qte.indicator != null && !qte.failureFeedbackShown)
            qteDisplay.ShowQTEResult(qte.indicator, success);

        if (_currentQTEIndex < _activeQTEs.Count && _activeQTEs[_currentQTEIndex] == qte)
        {
            _currentQTEIndex++;
            if (_currentQTEIndex < _activeQTEs.Count)
            {
                _nextQTEStartTime = _qtePhaseTimer + _activeQTEs[_currentQTEIndex].spawnTime;
                _state = QTEState.PerformingQTEAttack;
            }
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

    public void OnSweepFailureHit()
    {
        if (_state != QTEState.QTEEnding) return;
        ApplyPendingFailureDamage();
    }

    private void ApplyPendingFailureDamage()
    {
        if (!_failureDamagePending) return;

        _failureDamagePending = false;
        float damage = _activeQTEs.Find(qte => qte.resolved && !qte.success)?.config.failureDamage ?? 0f;
        if (damage > 0f && PlayerState.Instance != null)
        {
            PlayerState.Instance.TakeDamage(damage);
            DebugLog.Info($"[QTEController] QTE失败伤害结算: {damage:F0}");
        }
    }

    /// <summary>
    /// 由 End2 AnimationEvent 回调，通知 QTE 结果动画播放完毕
    /// </summary>
    public void OnSweepResultAnimationEnd()
    {
        if (_state == QTEState.QTEEnding)
        {
            ApplyPendingFailureDamage();
            DebugLog.Info("[QTEController] OnSweepResultAnimationEnd → CompleteQTEAttack");
            CompleteQTEAttack();
        }
    }

    #endregion

    #region 格挡表现

    /// <summary>
    /// 玩家 Swipe QTE 输入识别后的固定倾角挥动反馈。
    /// </summary>
    private static readonly List<Enemy> EmptySwipeVisualTargets = new List<Enemy>(0);

    private void PlaySwipeInputVisual(QTEInstance qte)
    {
        if (qte == null || qte.config == null) return;

        var player = PlayerState.Instance;
        var slashConfig = player != null && player.heroConfig != null
            ? player.heroConfig.GetSkillConfig(AttackType.Slash)
            : null;
        if (player == null || slashConfig == null || slashConfig.attackWavePrefab == null) return;

        // SweepEffect 对 leftToRight 的视觉朝向与 QTE 图案的填充方向相反，故取反。
        bool leftToRight = Mathf.Abs(Mathf.DeltaAngle(qte.config.swipeDirection, 0f)) > 90f;
        const float indicatorTilt = 32.65f;
        float angleOffset = indicatorTilt;

        Vector3 playerPos = player.transform.position;
        Vector3 center = new Vector3(0f, playerPos.y + slashConfig.slashSpawnYOffset, playerPos.z + slashConfig.slashSpawnZOffset);

        // 完全走正式 Slash 的 SweepEffect 管线；空目标列表确保它只播放视觉。
        SweepEffect.Create(center, slashConfig.damageType, 0f, EmptySwipeVisualTargets, leftToRight,
            slashConfig.slashSweepHalfWidth, slashConfig.slashSweepAngle, slashConfig.slashSweepDuration,
            prefab: slashConfig.attackWavePrefab,
            rotateSprite1: stabBlockRotateSprite1,
            rotateSprite2: stabBlockRotateSprite2,
            angleOffset: angleOffset,
            movementTilt: indicatorTilt,
            additionalWeaponRotation: 20f);
    }

    /// <summary>
    /// 播放 QTE 格挡成功表现：矛从下方弧形扫入屏幕中央，同时自转。
    /// Pivot 在摄像机中心负责公转，Spear 偏移在 Pivot 下负责自转。
    /// </summary>
    private void PlayBlockVisual()
    {
        if (stabBlockEffectPrefab == null) return;

        var cam = Camera.main;
        if (cam == null) return;

        // Pivot：挂在摄像机中心，负责公转（绕摄像机 X 轴从下方扫入）
        var pivot = new GameObject("QTE_BlockVFX_Pivot");
        pivot.transform.SetParent(cam.transform, false);
        pivot.transform.localPosition = new Vector3(0f, 0f, stabBlockDistance);
        pivot.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        // Spear：偏移在 Pivot 下，stabBlockOffsetY 映射到 localZ（Pivot 旋转后将变为摄像机 Y 偏移）
        var visual = Instantiate(stabBlockEffectPrefab, pivot.transform);
        visual.transform.localPosition = new Vector3(stabBlockOffsetX, 0f, -stabBlockOffsetY);
        visual.transform.localScale = stabBlockScale;
        visual.transform.localRotation = Quaternion.identity;

        var sr = visual.GetComponentInChildren<SpriteRenderer>();
        if (sr == null) sr = visual.GetComponent<SpriteRenderer>();
        if (sr != null && stabBlockSprite != null)
            sr.sprite = stabBlockSprite;

        float duration = stabBlockDuration;

        // 公转：Pivot X 90° → 0°，Spear 从下方弧形扫入摄像机中心
        pivot.transform.DOLocalRotate(new Vector3(0f, 0f, 0f), duration).SetEase(Ease.InOutQuad).SetUpdate(UpdateType.Normal, false);

        // 自转：Spear Z 0° → 900°
        visual.transform.DOLocalRotate(new Vector3(0f, 0f, 900f), duration, RotateMode.FastBeyond360).SetEase(Ease.Linear).SetUpdate(UpdateType.Normal, false);

        // 三帧精灵均匀切换
        if (sr != null && stabBlockRotateSprite1 != null && stabBlockRotateSprite2 != null)
        {
            float third = duration / 3f;
            var spriteSeq = DOTween.Sequence().SetUpdate(UpdateType.Normal, false);
            spriteSeq.SetTarget(visual);
            spriteSeq.AppendInterval(third);
            spriteSeq.AppendCallback(() => { if (sr != null) sr.sprite = stabBlockRotateSprite1; });
            spriteSeq.AppendInterval(third);
            spriteSeq.AppendCallback(() => { if (sr != null) sr.sprite = stabBlockRotateSprite2; });
        }

        // 末尾20% fadeout
        if (sr != null)
        {
            DOVirtual.DelayedCall(duration * 0.8f, () =>
            {
                if (sr != null) sr.DOFade(0f, duration * 0.2f).SetUpdate(UpdateType.Normal, false);
            }).SetUpdate(UpdateType.Normal, false);
        }

        // 动画结束后销毁
        DOVirtual.DelayedCall(duration + 0.05f, () =>
        {
            if (pivot != null) Destroy(pivot);
        }).SetUpdate(UpdateType.Normal, false);
    }

    #endregion

    #region QTE 攻击收尾

    /// <summary>
    /// 中止 QTE（敌人被打入 Stun/Launch/Dead 等非 QTEAttacking 状态）
    /// </summary>
    public void AbortQTE()
    {
        UnregisterQTEActivity();
        _qteGeneration++;
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
        SetStrictInputLock(false);

        // BUG FIX: AbortQTE 需要确保 BOSS 设置冷却并回到 Idle，否则下一帧立即重新触发 QTE
        enemy.ExitQTEAttack();

        OnQTEAttackFinished?.Invoke();
        DebugLog.Info("[QTEController] QTE已中止");
    }

    private void CompleteQTEAttack()
    {
        if (_completionStarted)
        {
            Debug.Log($"[QTE_FLIP_DIAG] CompleteQTEAttack ignored duplicate state={_state}");
            return;
        }

        _completionStarted = true;
        UnregisterQTEActivity();
        ApplyPendingFailureDamage();
        SetStrictInputLock(false);
        _state = QTEState.QTECompleted;

        bool isDefensive = _currentAttack != null && _currentAttack.isDefensiveQTE;

        // 非防御型 QTE 的失败伤害已在 ResolveQTE 失败瞬间结算；收尾仅处理飞行物。
        if (!isDefensive && _activeProjectile != null)
        {
            bool anyFailed = _activeQTEs.Exists(qte => qte.resolved && !qte.success);
            var proj = _activeProjectile.GetComponent<QTEProjectile>();
            if (anyFailed && proj != null)
                proj.ContinuePassThrough(0.8f, null);
            else if (proj != null)
                proj.DestroyOnSuccess();
            else
                Destroy(_activeProjectile);
            _activeProjectile = null;
        }

        // 清理残余箭矢波（防御型 QTE：已 Deflect 的已完成，未到达的需强制销毁）
        ClearAllArrowWaves();

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
