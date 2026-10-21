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
public partial class QTEController : MonoBehaviour
{

    /// <summary>
    /// 关卡/阵型信息（只读）。历史上直接摸 StageController 门面，现只依赖角色接口
    /// （2026-10-20 批次 1）。延迟解析：本组件可能早于 StageController Awake。
    /// </summary>
    private IStageFormation _stageFormation;

    private IStageFormation StageFormation => _stageFormation ??= StageController.Instance;
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
            Debug.Log($"[QTE_FLIP_DIAG] Abort triggered: _state={_state} enemy.state={enemy.state} frame={Time.frameCount}");
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

}
