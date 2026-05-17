using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// QTE 状态机
/// Idle → CoolingDown → WaitingForAttackFinish → PerformingQTEAttack → QTEJudging → QTECompleted → CoolingDown
///
/// QTE 阶段概念：
///   - PerformingQTEAttack: 等待 QTE 阶段开始（飞行物到达 / 动画前摇结束）
///   - QTE 阶段开始后，slot delay 相对于阶段开始计时
///   - 全部 QTE 成功 → 销毁飞行物；任一失败 → 飞行物穿过摄像机
/// </summary>
public enum QTEState
{
    Idle,
    CoolingDown,
    WaitingForAttackFinish,
    PerformingQTEAttack,
    QTEJudging,
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

    [Header("组件引用")]
    public Enemy enemy;
    public QTEDisplay qteDisplay;

    [Header("运行时状态")]
    [SerializeField] private QTEState _state = QTEState.Idle;

    private int _currentAttackIndex;
    private float _qteTimer;              // 攻击开始后的通用计时器
    private bool _qtePhaseStarted;        // QTE 阶段是否已开始（飞行物到达 / 动画前摇结束）
    private float _qtePhaseTimer;         // QTE 阶段开始后的计时器（用于 QTE spawn/judge）
    private QTEAttackConfig _currentAttack;
    private List<QTEInstance> _activeQTEs = new List<QTEInstance>();
    private GameObject _activeProjectile;

    // 事件
    public System.Action OnQTETriggered;       // QTE 攻击触发
    public System.Action OnQTESuccess;         // QTE 判定成功
    public System.Action OnQTEFailure;         // QTE 判定失败
    public System.Action OnQTECompleted;       // 一轮 QTE 攻击结束

    public QTEState State => _state;

    private void Awake()
    {
        if (enemy == null) enemy = GetComponent<Enemy>();
    }

    private void Start()
    {
        if (enemy != null && enemy.isBoss)
        {
            enemy.OnBossEngaged += OnBossEngaged;
        }
    }

    private void OnDestroy()
    {
        if (enemy != null) enemy.OnBossEngaged -= OnBossEngaged;
        KillProjectileSequence();
    }

    private void Update()
    {
        if (qteData == null || enemy == null) return;

        switch (_state)
        {
            case QTEState.CoolingDown:
                UpdateCooldown();
                break;
            case QTEState.PerformingQTEAttack:
                UpdatePerforming();
                break;
            case QTEState.QTEJudging:
                UpdateJudging();
                break;
        }
    }

    #region 状态切换

    private void OnBossEngaged(Enemy boss)
    {
        if (qteData == null || qteData.qteAttacks.Count == 0) return;
        _currentAttackIndex = 0;
        StartCooldown(qteData.firstQTECooldown);
    }

    private void StartCooldown(float duration)
    {
        _state = QTEState.CoolingDown;
        _qteTimer = duration;
    }

    private void UpdateCooldown()
    {
        _qteTimer -= Time.deltaTime;
        if (_qteTimer <= 0f)
        {
            if (enemy.isAttackAnimating)
            {
                _state = QTEState.WaitingForAttackFinish;
            }
            else
            {
                TriggerQTEAttack();
            }
        }
    }

    public void OnEnemyAttackComplete()
    {
        if (_state == QTEState.WaitingForAttackFinish)
            TriggerQTEAttack();
    }

    private void TriggerQTEAttack()
    {
        if (qteData == null || qteData.qteAttacks.Count == 0) return;
        _currentAttack = qteData.qteAttacks[_currentAttackIndex];
        _state = QTEState.PerformingQTEAttack;
        _qteTimer = 0f;
        _qtePhaseStarted = false;
        _qtePhaseTimer = 0f;
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

        enemy.EnterQTEAttack();
        SpawnProjectile();
        OnQTETriggered?.Invoke();
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

    #endregion

    #region QTE 判定

    private void UpdatePerforming()
    {
        _qteTimer += Time.deltaTime;

        if (!_qtePhaseStarted)
        {
            // 无飞行物时，等待动画前摇结束后开始 QTE 阶段
            if (_activeProjectile == null && _qteTimer >= _currentAttack.animationLeadTime)
                StartQTEPhase();
            // 有飞行物：等待 OnProjectileReachedTarget 回调
            return;
        }

        _qtePhaseTimer += Time.deltaTime;

        // 按 phase-relative 时间生成指示器
        foreach (var qte in _activeQTEs)
        {
            if (qte.indicator == null && _qtePhaseTimer >= qte.spawnTime)
                SpawnQTEIndicator(qte);
        }

        // 所有 QTE 已到生成时间 → 进入判定阶段
        bool allReady = true;
        foreach (var qte in _activeQTEs)
        {
            if (qte.indicator == null && _qtePhaseTimer < qte.spawnTime)
            { allReady = false; break; }
        }
        if (allReady)
            _state = QTEState.QTEJudging;
    }

    private void UpdateJudging()
    {
        _qtePhaseTimer += Time.deltaTime;

        bool allResolved = true;
        foreach (var qte in _activeQTEs)
        {
            if (!qte.resolved)
            {
                if (qte.IsExpired(_qtePhaseTimer))
                    ResolveQTE(qte, false);
                else
                    allResolved = false;
            }
        }

        if (allResolved)
            CompleteQTEAttack();
    }

    public bool IsQTEActive
    {
        get
        {
            if (_state != QTEState.QTEJudging && _state != QTEState.PerformingQTEAttack) return false;
            if (!_qtePhaseStarted) return false;
            foreach (var qte in _activeQTEs)
            {
                if (!qte.resolved && qte.IsInJudgeWindow(_qtePhaseTimer))
                    return true;
            }
            return false;
        }
    }

    public bool TryQTEClick(Vector2 screenPos)
    {
        if (_state != QTEState.QTEJudging && _state != QTEState.PerformingQTEAttack) return false;
        if (!_qtePhaseStarted) return false;

        foreach (var qte in _activeQTEs)
        {
            if (qte.resolved || qte.config.qteType != QTEType.Click) continue;
            if (!qte.IsInJudgeWindow(_qtePhaseTimer)) continue;

            if (IsClickInQTEArea(screenPos, qte))
            {
                ResolveQTE(qte, true);
                return true;
            }
        }
        return false;
    }

    public bool TryQTESwipe(Vector2 swipeDirection, float swipeSpeed, Vector2 screenPos)
    {
        if (_state != QTEState.QTEJudging && _state != QTEState.PerformingQTEAttack) return false;
        if (!_qtePhaseStarted) return false;

        foreach (var qte in _activeQTEs)
        {
            if (qte.resolved || qte.config.qteType != QTEType.Swipe) continue;
            if (!qte.IsInJudgeWindow(_qtePhaseTimer)) continue;

            if (swipeSpeed < qte.config.swipeMinSpeed) continue;

            float targetAngle = qte.config.swipeDirection;
            float swipeAngle = Mathf.Atan2(swipeDirection.y, swipeDirection.x) * Mathf.Rad2Deg;
            if (swipeAngle < 0f) swipeAngle += 360f;

            float diff = Mathf.Abs(Mathf.DeltaAngle(swipeAngle, targetAngle));
            if (diff <= qte.config.swipeAngleTolerance)
            {
                ResolveQTE(qte, true);
                return true;
            }
        }
        return false;
    }

    private bool IsClickInQTEArea(Vector2 screenPos, QTEInstance qte)
    {
        if (qte.indicator == null) return false;

        // 使用指示器的 RectTransform 判断
        var rt = qte.indicator.GetComponent<RectTransform>();
        if (rt == null) return false;

        return RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos);
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
        }
        qte.indicator = qteDisplay.SpawnIndicator(qte.config);
    }

    private void ResolveQTE(QTEInstance qte, bool success)
    {
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
            qteDisplay.ShowQTEResult(qte.indicator, success);
        }
    }

    private void OnQTESuccessSingle(QTEInstance qte)
    {
        if (enemy != null)
            enemy.TakePoiseDamage(qte.config.poiseDamage);

        if (UltimateSystem.Instance != null)
            UltimateSystem.Instance.AddEnergy(qte.config.ultimateEnergyGain);

        OnQTESuccess?.Invoke();
    }

    private void OnQTEFailureSingle(QTEInstance qte)
    {
        if (PlayerState.Instance != null)
            PlayerState.Instance.TakeDamage(qte.config.failureDamage);

        OnQTEFailure?.Invoke();
    }

    #endregion

    #region QTE 攻击收尾

    private void CompleteQTEAttack()
    {
        _state = QTEState.QTECompleted;

        // 检查是否有 QTE 失败
        bool anyFailed = false;
        foreach (var qte in _activeQTEs)
        {
            if (qte.resolved && !qte.success)
            { anyFailed = true; break; }
        }

        // 飞行物处理
        if (_activeProjectile != null)
        {
            var proj = _activeProjectile.GetComponent<QTEProjectile>();
            if (anyFailed && proj != null)
            {
                // 任一失败 → 飞行物穿过摄像机
                proj.ContinuePassThrough(0.8f, null);
            }
            else if (proj != null)
            {
                // 全部成功 → 销毁飞行物
                proj.DestroyOnSuccess();
            }
            else
            {
                Destroy(_activeProjectile);
            }
            _activeProjectile = null;
        }

        // 通知敌人恢复
        enemy.ExitQTEAttack();

        // 清理指示器
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
                return;
            }
        }

        float cooldown = _currentAttack != null ? _currentAttack.cooldownAfterQTE : qteData.baseQTECooldown;
        StartCooldown(cooldown);
        OnQTECompleted?.Invoke();
    }

    #endregion
}
