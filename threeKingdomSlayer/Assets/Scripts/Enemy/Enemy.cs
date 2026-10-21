using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 敌人状态枚举
/// </summary>
public enum EnemyState
{
    Idle,
    Moving,
    Attacking,
    Stunned,
    Launched,
    Dead,
    QTEAttacking,  // BOSS QTE 攻击演出中
    GettingUp,
    Frozen,        // 冻结：暂停而非取消一切行动
    IceBreaking    // 破冰动画播放中：位置静止、不可再冻，动画结束才解除冻结
}

public enum EnemyFormState
{
    Normal,
    /// <summary>盾兵破盾表现期：无盾、减伤已失效、增伤未生效（见 design/enemy-1021-shield-guard.md）</summary>
    ShieldBroken,
    Coward
}

public enum RushMoveStartResult
{
    Started,
    AlreadyRushing,
    Deferred,
    Rejected
}

public enum RushMoveMode
{
    Step,
    ContinuousEmptyRun
}

public enum RushMoveOrderOwner
{
    None,
    SpawnEntry,
    WaveMarch,
    PushReturn,
    Boss
}

/// <summary>
/// Boss 推进状态枚举
/// </summary>
public enum BossState
{
    None,        // 非Boss或未进入分阶段推进
    Approaching, // Boss暂停在第3排(rowIndex=2)，等待前两排清空
    InCombat     // Boss已到达应战排(rowIndex=1)，进入战斗
}

/// <summary>
/// 攻击命中帧声明：出伤（近战）/ 击发（远程）必须与这一帧在屏幕上的时刻一致。
/// 运行时按 spriteName 匹配「屏幕上真实显示的精灵」，因此对动画重定时、卡肉、变速都成立；
/// keyIndex 只在编辑器校验器里用于校验/回填（0 基，attack8 = 8）。
/// </summary>
[System.Serializable]
public struct AttackHitFrame
{
    [Tooltip("命中帧的精灵名（如 Enemy_1011_attack8）")]
    public string spriteName;
    [Tooltip("命中帧在 clip 关键帧中的序号（0 基；仅供编辑器校验/回填）")]
    public int keyIndex;
    [Tooltip("该命中帧是否结算一次伤害/发射（多段连击可配多条；false = 只作对齐标记）")]
    public bool resolve;
    [Tooltip("该命中帧在攻击 clip 中的时间（秒）；由编辑器校验器自动回填")]
    public float hitTime;
}

/// <summary>
/// 攻击序列中的一步，定义该步的攻击类型和参数
/// </summary>
[System.Serializable]
public struct AttackStep
{
    [Tooltip("是否C技（霸体窗口 + 红色描边）")]
    public bool isCAttack;
    [Tooltip("动画Trigger名（空=默认Attack）")]
    public string animationTrigger;
    [Tooltip("兼容字段：未声明命中帧时作为前冲/出伤时长；已声明命中帧时由首个 resolve 命中帧时间替代")]
    public float spawnDuration;
    [Tooltip("收招时长（秒）：声明命中帧时自动改为「clip 剩余时间」，此值仅在未声明命中帧时生效")]
    public float drawDuration;
    [Tooltip("该步攻击后的额外冷却（秒）")]
    public float extraCooldown;
    [Tooltip("攻击时是否左右翻转")]
    public bool useFlip;
    [Tooltip("命中帧列表（空 = 沿用旧的 spawnDuration 出伤；非空时由 hitFrames 驱动时序）")]
    public List<AttackHitFrame> hitFrames;
}

/// <summary>
/// 敌人实体 - MonoBehaviour
/// 管理敌人的生命值、架势值、状态机、前进移动、透明度渐变
/// </summary>
public partial class Enemy : MonoBehaviour
{
    [Header("基础属性")]
    public string enemyName = "骷髅兵";
    public int enemyId;

    /// <summary>
    /// 实例唯一 ID（运行时分配，用于调试日志区分同一 prefab 的不同实例）
    /// </summary>
    [System.NonSerialized] public int instanceId;
    private static int _nextInstanceId = 1;
    private EnemyState _lastLoggedState = EnemyState.Idle;

    /// <summary>
    /// 调试标签：instanceId(enemyId)，如 #3(101)
    /// </summary>
    public string DebugTag => $"#{instanceId}({enemyId})";

    // GC 优化：默认排透明度系数，避免每帧 new float[]
    private static readonly float[] DefaultRowAlphaFactors = { 1.0f, 0.8f, 0.6f, 0.4f, 0.2f };

    [Header("战斗属性")]
    public float maxHealth = 100f;
    public int occupySlots = 1;
    public float attackSpeed = 1f;
    public float attackDamage = 10f;
    [Range(0f, 1f)]
    [Tooltip("普通形态下的伤害减免比例。0.5 = 减伤50%；胆怯形态下不生效。")]
    public float normalFormDamageReductionPercent = 0f;
    public float attackRange = 1f;
    public float moveSpeed = 1f;

    [Header("视觉偏移")]
    [Tooltip("美术基准偏移（Inspector 手工值）。精灵锚点在画布中心，此值把脚下移到地面基准线；对齐 -1.8 时取值见 EnemyGroundAligner。")]
    public float visualYOffset = 0f;

    /// <summary>
    /// 运行时按真实精灵剪影推算出的偏移（由 EnemyGroundAligner 写入，不序列化）。
    /// 与 visualYOffset 相加，使二者互不覆盖：美术基准可继续手调，自动对齐单独生效。
    /// </summary>
    [System.NonSerialized] public float runtimeVisualYOffset;

    [Header("攻击序列")]
    [Tooltip("攻击序列（按顺序循环执行每步攻击）")]
    public List<AttackStep> attackSequence;

    [Header("远程攻击")]
    [Tooltip("是否为远程单位（攻击时发射飞行物而非贴身造成伤害）")]
    public bool isRanged;
    [Tooltip("飞行物 Prefab（需挂载 EnemyProjectile 组件）")]
    public GameObject projectilePrefab;
    [Tooltip("飞行物抛物线最高点高度")]
    public float projectileArcHeight = 3f;
    [Tooltip("飞行物飞行时长")]
    public float projectileFlyDuration = 1f;
    [Tooltip("飞行物目标 Z 偏移（相对主摄像机 Z，越大越过摄像机越远）")]
    public float projectileZTargetOffset = 5f;
    [Tooltip("飞行物目标 X 中心（世界坐标）")]
    public float projectileLandingXCenter = 0f;
    [Tooltip("飞行物落点相对目标中心的随机半宽")]
    [Min(0f)] public float projectileLandingXSpread = 0f;
    [Tooltip("飞行物目标 X 偏移（相对敌人当前位置 X）；仅在落点随机范围为 0 时使用")]
    public float projectileXOffset = 0f;
    [Tooltip("启用时按敌人所在列使用固定落点 X，不使用随机散布。")]
    public bool useFixedProjectileLandingX;
    [Tooltip("固定落点 X：索引 0~4 对应敌人列 0~4。")]
    public float[] fixedProjectileLandingXByColumn = { -2f, -1f, 0f, 1f, 2f };
    [Tooltip("全局箭矢飞行参数配置，为空则用默认值")]
    public ArrowGlobalConfig arrowConfig;

    [Header("共享血量")]
    [Tooltip("与同行相邻同ID敌人共享血量")]
    public bool shareHealthWithAdjacent = false;

    [Header("架势系统")]
    public float maxPoise = 50f;
    public float stunDuration = 1.5f;

    [Header("冻结（冰系）")]
    [Tooltip("冻结时长（秒）。重复冻结只重置为满值，不叠加")]
    public float freezeDuration = 4f;
    [Tooltip("冻结概率上限，防必冻")]
    [Range(0f, 1f)] public float freezeChanceMax = 0.75f;
    [Tooltip("最后一次冰伤害后多久开始衰退（秒）")]
    public float freezeChanceDecayDelay = 0.6f;
    [Tooltip("冻结概率每秒衰退量")]
    public float freezeChanceDecayPerSecond = 0.5f;
    [Tooltip("处于攻击前摇时，冰伤害叠加概率的倍率")]
    public float freezeWindupChanceMultiplier = 2f;
    [Tooltip("冻结中被破冰推动时不产生位移")]
    public bool freezePushImmune = false;
    [Tooltip("冻结抗性：减算到冻结概率，减到 0 即免疫")]
    [Range(0f, 1f)] public float freezeResist = 0f;
    [Tooltip("破冰动画时长（秒）")]
    public float iceBreakAnimDuration = 0.4f;
[Tooltip("破冰后的回位延时（秒），从破冰动画结束起算")]
public float breakIceReturnDelay = 1.2f;
[Tooltip("冻结表现：挂在敌人身上的覆盖层 Prefab（可为空，为空时用程序化冰块）")]
public GameObject freezeOverlayPrefab;
[Tooltip("冰块高度 / 角色可见高度（等比缩放，保持素材本身的长宽比）")]
[Range(0.3f, 2.5f)] public float freezeOverlayScale = 0.825f;
[Tooltip("冰块中心垂直偏移（相对角色可见高度，正=向上）")]
[Range(-0.5f, 0.5f)] public float freezeOverlayYOffset = 0.1f;
[Tooltip("自然解冻后的停顿（秒）：此期间不接补齐/行军订单")]
[Min(0f)] public float freezeRecoveryDelay = 0.5f;
[Tooltip("破冰一次性特效 Prefab（可为空）。时长以 iceBreakAnimDuration 为准")]
public GameObject iceBreakEffectPrefab;
[Tooltip("自然解冻一次性特效 Prefab（可为空）。与破冰碎裂是两套不同表现")]
public GameObject freezeMeltEffectPrefab;

    [Tooltip("冰系表现共享配置（所有敌人共用一份；为空时才回落到本预制体上的以下字段）")]
    public FreezePresentationConfig freezePresentation;

    private GameObject ResolveFreezeOverlayPrefab() =>
        freezePresentation != null && freezePresentation.freezeOverlayPrefab != null ? freezePresentation.freezeOverlayPrefab : freezeOverlayPrefab;
    internal GameObject ResolveIceBreakPrefab() =>
        freezePresentation != null && freezePresentation.iceBreakEffectPrefab != null ? freezePresentation.iceBreakEffectPrefab : iceBreakEffectPrefab;
    internal GameObject ResolveMeltPrefab() =>
        freezePresentation != null && freezePresentation.freezeMeltEffectPrefab != null ? freezePresentation.freezeMeltEffectPrefab : freezeMeltEffectPrefab;
    private float ResolveOverlayScale() => freezePresentation != null ? freezePresentation.overlayScale : freezeOverlayScale;
    private float ResolveOverlayYOffset() => freezePresentation != null ? freezePresentation.overlayYOffset : freezeOverlayYOffset;

    [Header("击飞系统")]
    public float launchDuration = 2f;
    [Tooltip("下落重力加速度（真实重力=9.8，游戏建议15~25）")]
    public float launchGravity = 20f;
    [Tooltip("空中被击中时的向上反弹速度")]
    public float launchReboundVelocity = 8f;
    [Tooltip("空中Y轴随机高度范围（仅初始击飞时随机一次）")]
    public float launchYHeightMin = 1.5f;
    public float launchYHeightMax = 4.5f;
    [Range(1f, 5f)] public float launchedDamageTakenMultiplier = 1.5f;

    /// <summary>击飞落地回调（参数：落地敌人）</summary>
    [System.NonSerialized] public System.Action<Enemy> OnLaunchedLanded;
    [Tooltip("空中被击中时延长浮空的时间（秒）")]
    public float launchedHitExtendDuration = 0.5f;

    [Header("奖励")]
    public int coinReward = 10;
    public float expReward = 10f;
    [Tooltip("死亡掉落的经验宝石精灵（为空时使用 ExpGemManager 默认 prefab）")]
    public Sprite gemSprite;

    [Header("BOSS")]
    public bool isBoss;
    [Tooltip("Boss 血条 Prefab（可选，为 null 时使用 BattleHUD 默认模板）")]
    public GameObject bossHealthBarPrefab;
    [System.NonSerialized] public BossState bossState = BossState.None;
    [System.NonSerialized] public SharedHealthGroup sharedHealthGroup;
    [System.NonSerialized] public bool isSuperArmor;

    [Header("BOSS多阶段")]
    [Tooltip("各阶段配置（ScriptableObject引用），按 phaseIndex 排序")]
    public List<BossPhaseData> bossPhases;
    [System.NonSerialized] public int currentBossPhase = 0;
    [System.NonSerialized] public bool isPhaseTransitioning;
    internal bool _healthLocked;
    [System.NonSerialized] private BossPhaseData _deferredPhaseTransition; // QTE 中血量跌至转阶段阈值时挂起，待 QTE 结束再转阶段

    [Header("描边覆盖（0或Color.clear=使用全局默认）")]
    [SerializeField] private Color outlineColorCOverride = Color.clear;
    [SerializeField] private Color outlineColorSuperArmorOverride = Color.clear;
    [SerializeField] private Color outlineColorQTEOverride = Color.clear;
    [SerializeField] private float outlineWidthCOverride = 0f;
    [SerializeField] private float outlineWidthSuperArmorOverride = 0f;
    [SerializeField] private float outlineWidthQTEOverride = 0f;

    [Header("调试")]
    [SerializeField] private OutlineDebugMode debugForceOutline = OutlineDebugMode.None;

    [Header("弱点系统")]
    public float stabDamageMultiplier = 1f;
    public float slashDamageMultiplier = 1f;
    public float pierceDamageMultiplier = 1f;
    public float sweepDamageMultiplier = 1f;
    public float launchDamageMultiplier = 1f;
    public float poiseDamageMultiplier = 1f;

    // 原始值快照（池复用 / 多波次重置时恢复 prefab 默认值）
    private List<AttackStep> _originalAttackSequence;
    private float _originalStabMult, _originalSlashMult, _originalPierceMult;
    private float _originalSweepMult, _originalLaunchMult, _originalPoiseMult;
    private bool _originalIsSuperArmor;
    private BossQTEData _originalQteData;

    // 波次强化用的原始值缓存（prefab 出厂值，Awake 时捕获一次）
    private float _prefabMaxHealth;
    private float _prefabAttackSpeed;
    private float _prefabAttackDamage;
    public float BaseAttackDamage => _prefabAttackDamage;
    internal Color _prefabColor = Color.white;

    [Header("招架血量眩晕阈值")]
    public ParryStunThreshold[] parryStunThresholds;

    [System.NonSerialized] public EnemyState state = EnemyState.Idle;
    private bool _cowardTransitionPlaying;
    [System.NonSerialized] public EnemyFormState formState = EnemyFormState.Normal;
    public bool IsCoward => formState == EnemyFormState.Coward;
    [System.NonSerialized] public float currentHealth;
    [System.NonSerialized] public float currentPoise;
    /// <summary>QTE成功破防后延迟进入Stun，等QTE动画播完</summary>
    [System.NonSerialized] public bool deferredStun;
    /// <summary> 眩晕恢复进度 (0→1)，基于实际时间，不受状态切换影响 </summary>
    public float stunRecoveryProgress
    {
        get
        {
            if (_poiseRecoveryEndTime <= 0f || _appliedStunDuration <= 0f) return 1f;
            float remaining = _poiseRecoveryEndTime - Time.time;
            if (remaining <= 0f) return 1f;
            return 1f - (remaining / _appliedStunDuration);
        }
    }
    /// <summary>
    /// 当前攻击是否处于 Parry 判定窗口（含虚位提前量）。
    /// 动画顺序：蓄力帧 → 停顿（Parry 窗口）→ 发生帧（PerformAttack）→ 收招。
    /// 窗口起点 = 蓄力完成时刻（停顿开始），判定区间 = [起点 - grace, 起点 + duration]。
    /// </summary>
    public bool IsParryWindowActive
    {
        get
        {
            if (_parryWindowStartTime < 0f) return false;
            float t = Time.time;
            return t >= _parryWindowStartTime - _currentParryWindowGrace
                && t <= _parryWindowStartTime + _currentParryWindowDuration;
        }
    }
    public int columnIndex;
    public int rowIndex; // 0 = 最前排

    // 内部状态
    private float stunTimer;
    private float _appliedStunDuration; // 实际应用的眩晕时长（用于 UI 恢复进度）
    private float _poiseRecoveryEndTime; // 架势恢复完成的时间点（Time.time + duration）
    internal bool _isStunExiting;
    private bool _resumeStunAfterLaunch;
    private Coroutine _stunExitRoutine;
    private const float BOSS_STUN_END_DURATION = 0.9f;
    private float launchTimer;
    private float launchVelocityY;     // 当前Y轴速度
    private Vector3 launchStartLocalPos; // 挑飞起始位置
    private float currentLaunchYHeight;   // 本次挑飞的随机 Y 高度（仅用于初速度计算）
    private float _remainingStunOnLaunch;  // 挑飞时被中断的眩晕剩余时间（落地后恢复）
    private int _launchExtendCount;         // 当前浮空内 ExtendLaunch 次数（落地/重Launch 重置）
    private float attackTimer;      // 攻击冷却计时器（攻击动画结束后开始冷却）
    private float attackAnimTimer;  // 攻击动画计时器（攻击动作执行时间）
    public bool isAttackAnimating; // 是否正在播放攻击动画（AttackSpawn 或 AttackDraw）
    /// <summary>教学手势拍：敌人不进攻、不移动（只影响行动调度，受击/闪白/动画照常）。</summary>
    [System.NonSerialized] public bool tutorialHold;
    /// <summary>
    /// 「豁免补齐」：该敌人**不参与波次行军**（整排清空后的前移补齐），
    /// 原地不动、不填补前方空排；但它仍然**算占位**（它所在的排不会因为它而判空，
    /// 因此它身后的排也不会前移）——与「击退留下的空位不传给未受击敌人」同一口径。
    ///
    /// 用途：让某些敌人「无视空排补齐」（守卫/锚点类），用来做不前移的阻挡型效果。
    /// 生效位置：<c>ColumnManager</c> 的补齐**规划/派单**层（不派单、不预留目标槽），
    /// 不影响 SpawnEntry 入场、击退回位、横向位移与 Boss 推进（它们各有独立语义）。
    ///
    /// 可在 Inspector 预设（按 prefab），也可由效果在运行时置位。
    /// </summary>
    public bool ignoreFillUpOrders;
    public bool isAttackDrawPhase;  // 是否处于攻击收招阶段（不可被招架打断）
    public bool isCFrame;           // 是否处于C技起始帧（霸体窗口），仅 Parry/Launch 可打断
    internal int _currentAttackStep;  // 攻击序列当前位置
    internal float actionCooldownTimer; // BOSS Idle 调度冷却计时器
    /// <summary> 当前攻击步骤的前摇时长（供 EnemySpriteController 读取） </summary>
    [System.NonSerialized] public float currentStepSpawnDuration;
    /// <summary> 当前攻击步骤的 Parry 窗口起点时刻（Time.time），-1=无窗口 </summary>
    [System.NonSerialized] private float _parryWindowStartTime = -1f;
    /// <summary> 当前攻击步骤的 Parry 窗口时长（秒） </summary>
    [System.NonSerialized] private float _currentParryWindowDuration;
    /// <summary> 当前攻击步骤的 Parry 虚位提前量（秒） </summary>
    [System.NonSerialized] private float _currentParryWindowGrace;
    /// <summary> 当前攻击步骤的窗口时长（供 AnimationEvent 读取） </summary>
    [System.NonSerialized] private float _activeParryWindowDuration;
    /// <summary> 当前攻击步骤的虚位（供 AnimationEvent 读取） </summary>
    [System.NonSerialized] private float _activeParryWindowGrace;

    // Rush movement is scheduler-owned. pendingRushMove/targetRow remain public for
    // serialization/API compatibility, but normal-enemy orders are assigned through
    // AssignRushMoveOrder so callbacks can be validated by owner and generation.
    public bool pendingRushMove;
    public int targetRow = -1;
    public System.Action<Enemy, RushMoveOrderOwner, int> OnRushMoveComplete;
    internal RushMoveOrderOwner _rushMoveOrderOwner = RushMoveOrderOwner.None;
    internal int _rushMoveOrderGeneration;
    internal RushMoveMode _rushMoveMode = RushMoveMode.Step;
    internal float moveProgress; // 0~1, 当前排内移动进度
    internal bool isMovingToNextRow;

    internal bool isRushMove;
    private const float continuousRushMoveDuration = 0.2f;
    internal float rushMoveDelayTimer;
    internal bool rushMoveChainTriggered;
    internal Renderer[] renderers;
    // 材质实例数组（用于闪白效果）
    // 通过 renderer.material 创建实例，避免 MaterialPropertyBlock 在对象禁用时不生效的问题。
    // 在 Initialize() 中创建，在 ResetEnemy() 中销毁。
    internal Material[] flashMaterials;
    private bool initialized;

    // 受伤闪白相关
    internal float hitFlashTimer; // 闪白剩余时间
    private Color originalColor = Color.white; // 精灵原始颜色（白色）
    private const float HIT_FLASH_DURATION = 0.15f; // 闪白持续时间
    internal bool _allowHitFlashOnce; // Parry 打断霸体攻击时临时放行一次 HitFlash 动画

    // DOTween: 前进补齐时 Y 轴弹跳偏移（由 DOTween 驱动，在 UpdateWorldPosition 中应用）
    internal float bounceYOffset;

    /// <summary>当前生效的脚底视觉偏移（美术基准 + 运行时自动对齐）。</summary>
    public float EffectiveVisualYOffset => visualYOffset + runtimeVisualYOffset;

    /// <summary>
    /// 立即按当前生效偏移重算世界位置。
    /// 供 EnemyGroundAligner 在运行时写入 runtimeVisualYOffset 后调用，避免等到下一帧。
    /// 未初始化、击飞、死亡时不干预：这些状态下位置由 Enemy 自身时序或外部接管，
    /// 此时 runtimeVisualYOffset 已写入，会在下一次正常更新时生效。
    /// </summary>
    public void ApplyVisualOffsetNow()
    {
        if (!initialized) return;
        if (state == EnemyState.Launched || state == EnemyState.Dead) return;
        UpdateWorldPosition();
    }

    // DOTween: 当前攻击动画序列（用于在 Die() 中取消正在执行的攻击动作）
    internal Sequence _attackTween;
    // 命中帧：声明后出伤时刻由屏幕上的命中帧精灵决定（见 UpdateAttackHitFrames）
    private readonly List<string> _hitFrameSprites = new List<string>();
    private readonly List<bool> _hitFrameResolve = new List<bool>();
    private readonly List<bool> _hitFrameFired = new List<bool>();
    private bool _hitFrameResolvedOnce;
    private bool _hitFrameFiredAny;
    private bool _hitFrameInterrupted;
    internal bool _suspendTweenPaused;
    private EnemyAttackTelegraph _attackTelegraph;
    internal Tween _hitScaleTween;
    internal float _hitStopRemaining;
    public bool IsHitStopped => (_suspendSources & SuspendSource.HitStop) != 0;
    /// <summary>任何来源（卡肉 / 冻结）造成的挂起。挂起期间动画与攻击位移都停住。</summary>
    public bool IsSuspended => _suspendSources != SuspendSource.None;
    /// <summary>挂起来源：可叠加；只有全部解除才真正恢复动画与位移。</summary>
    [System.Flags]
    public enum SuspendSource
    {
        None = 0,
        HitStop = 1,
        Frozen = 2
    }
    internal SuspendSource _suspendSources;
    private bool _suspendAnimatorWasEnabled;

    // 冻结运行时状态（不序列化）
    /// <summary>顶开位移时长（秒）：这段期间保持冰冻形态，到位后才碎冰</summary>
    public const float BreakPushMoveSeconds = 0.12f;
    internal float _hitScaleMultiplier = 1f;

    // Animator HitFlash 协程引用
    internal Coroutine _hitFlashRoutine;
    internal Coroutine _getupRoutine;
    internal bool _getupInterrupted;

    // QTE 控制器缓存
    private QTEController _qteController;

    // 描边效果
    internal SpriteRenderer _spriteRenderer;
    private MaterialPropertyBlock _tintPropBlock; // 波次染色（描边属性已移到 EnemyOutline，不再共用 block）

    /// <summary>
    /// 关卡/阵型信息（只读）。敌人只依赖这份 5 成员的契约，不再直接摸 StageController 门面
    /// （2026-10-20 批次 1）。延迟解析：Enemy 可能早于 StageController Awake。
    /// </summary>
    private IStageFormation _stageFormation;

    internal IStageFormation StageFormation => _stageFormation ??= StageController.Instance;

    /// <summary>冰系视觉层（冰壳覆盖层 / 着色 / 碎冰特效）。视觉逻辑已从本类拆出，见 EnemyFreezeVisual。</summary>
    private EnemyFreezeVisual _freezeVisual;

    internal EnemyFreezeVisual FreezeVisual => _freezeVisual ??= new EnemyFreezeVisual();

    /// <summary>冰系视觉层需要的上下文：敌人节点 + 渲染器 + 冻结表现配置。</summary>
    /// <summary>当前挂起令牌集合（EnemyFreeze 判断「Frozen 令牌是否残留」用）。</summary>
    internal SuspendSource SuspendSources => _suspendSources;

    /// <summary>对象池回收：清掉残留挂起并恢复动画器（原本写在 ClearFreezeOnTeardown 里）。</summary>
    internal void ResetSuspendForTeardown()
    {
        _suspendSources = SuspendSource.None;
        _suspendTweenPaused = false;
        if (_animator != null) _animator.enabled = true;
    }

    internal EnemyFreezeVisual.Context FreezeVisualContext() => new EnemyFreezeVisual.Context
    {
        transform = transform,
        spriteRenderer = _spriteRenderer,
        overlayPrefab = ResolveFreezeOverlayPrefab(),
        overlayScale = ResolveOverlayScale(),
        overlayYOffset = ResolveOverlayYOffset(),
        tintColor = EnemyFreezeVisual.TintColor
    };

    // 敌人物体原始缩放与旋转值（从预制体读取，用于对象池复用还原）
    // 不能硬编码为 Vector3.one / Quaternion.identity，因为预制体可能有自定义初始姿态。
    internal Vector3 originalScale;
    private Quaternion originalRotation;

    // 血条UI缓存
    private EnemyHealthBar cachedHealthBar;
    internal Animator _animator;
    private EnemySpriteController _spriteCtrl;
    internal CavalryEnemy _cavalry;
    internal CavalryVisualController _cavalryVisual;
    private ShieldGuardEnemy _shieldGuard;
    private AnimationClip _attackClip; // 缓存的 Attack clip（用于同步 DOTween 与 Animator 时长）

    // 事件
    public System.Action<Enemy> OnDying;
    public System.Action<Enemy> OnDeath;
    public System.Action<Enemy> OnDeathAnimComplete; // 死亡动画播放完毕（Boss锦囊触发时机）

    /// <summary>当前排位对应的视觉不透明度（供独立死亡视觉等外部演出对齐，与 UpdateAlpha 同一套策略）。</summary>
    public float CurrentVisualAlpha => GetAlphaForRow(rowIndex);

    /// <summary>
    /// 自定义死亡视觉接管点（可选）。同 Prefab 上的视觉组件在 OnEnable 注册、OnDisable 注销；
    /// 返回 true 表示由它接管非击飞死亡演出，Enemy 不再启动 DeathBounceAndFall（根弹起＋随机旋转＋掉落）。
    /// 接管方在演出结束时必须调用 <see cref="CompleteCustomDeathVisualSequence"/> 完成收尾与回收。
    /// </summary>
    [System.NonSerialized] public System.Func<Enemy, bool> CustomDeathVisualHandler;
    public System.Action<Enemy> OnDamageTaken;
    public System.Action<Enemy, float, float> OnHealthChanged; // enemy, current, max
    public System.Action<Enemy, float, float> OnPoiseChanged;  // enemy, current, max
    public System.Action<Enemy> OnBossEngaged;                  // Boss 到达应战排
    public System.Action<Enemy, int> OnBossPhaseChanged;

    // Boss 分阶段推进
    private System.Action _onColumnsModifiedHandler;
    private float _bossEngageTimer; // Boss 到达应战排后的无敌缓冲时间

    private void Awake()
    {
        RefreshRendererCache();

        // 快照 prefab 原始值，供池复用 / 多波次重置
        SnapshotOriginalValues();
        originalScale = transform.localScale;
        originalRotation = transform.localRotation;

        // 缓存 prefab 原始值，供波次强化（ApplyWaveScaling）使用
        _prefabMaxHealth = maxHealth;
        _prefabAttackSpeed = attackSpeed;
        _prefabAttackDamage = attackDamage;
        if (_spriteRenderer != null)
            _prefabColor = _spriteRenderer.color;
        else if (renderers != null && renderers.Length > 0)
            _prefabColor = renderers[0].sharedMaterial.color;
    }

    private void SnapshotOriginalValues()
    {
        _originalAttackSequence = attackSequence != null ? new List<AttackStep>(attackSequence) : null;
        _originalStabMult = stabDamageMultiplier;
        _originalSlashMult = slashDamageMultiplier;
        _originalPierceMult = pierceDamageMultiplier;
        _originalSweepMult = sweepDamageMultiplier;
        _originalLaunchMult = launchDamageMultiplier;
        _originalPoiseMult = poiseDamageMultiplier;
        _originalIsSuperArmor = isSuperArmor;
        var qte = GetComponent<QTEController>();
        _originalQteData = qte != null ? qte.qteData : null;
    }

    private void RestoreOriginalValues()
    {
        if (_originalAttackSequence != null)
            attackSequence = new List<AttackStep>(_originalAttackSequence);
        stabDamageMultiplier = _originalStabMult;
        slashDamageMultiplier = _originalSlashMult;
        pierceDamageMultiplier = _originalPierceMult;
        sweepDamageMultiplier = _originalSweepMult;
        launchDamageMultiplier = _originalLaunchMult;
        poiseDamageMultiplier = _originalPoiseMult;
        isSuperArmor = _originalIsSuperArmor;
    }

    /// <summary>
    /// 对象从池中重新激活时重建 Renderer 缓存
    /// Awake 只在首次创建时执行，池复用不会重新 Awake
    /// </summary>
    private void OnEnable()
    {
        RefreshRendererCache();
    }

    /// <summary>
    /// 缓存敌人主体 Renderer。
    /// AttackTelegraph 的 SpriteRenderer 独立管理透明度和材质，不参与敌人主体闪白/波次染色。
    /// </summary>
    private void RefreshRendererCache()
    {
        var allRenderers = GetComponentsInChildren<Renderer>(true);
        var filtered = new List<Renderer>(allRenderers.Length);
        for (int i = 0; i < allRenderers.Length; i++)
        {
            var renderer = allRenderers[i];
            if (renderer == null || renderer.GetComponent<EnemyAttackTelegraph>() != null)
                continue;
            filtered.Add(renderer);
        }

        renderers = filtered.ToArray();
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// 初始化敌人（从对象池取出时调用）
    /// 敌人生成时直接站在对应排位置，不自动开始移动。
    /// 只有当前排出现空位时（通过 Column.RemoveEnemy()），
    /// 后方敌人才会向前补齐。
    /// </summary>
    public void Initialize(int col, int row)
    {
        instanceId = _nextInstanceId++;
        _cavalry = GetComponent<CavalryEnemy>();
        _cavalryVisual = GetComponent<CavalryVisualController>();
        _cavalry?.ResetCavalry();
        _shieldGuard = GetComponent<ShieldGuardEnemy>();
        _shieldGuard?.ResetShieldGuard();
        columnIndex = col;
        rowIndex = row;

        currentHealth = maxHealth;
        currentPoise = maxPoise;
        state = EnemyState.Idle;
        formState = EnemyFormState.Normal;
        bossState = BossState.None;
        isSuperArmor = false;
        isPhaseTransitioning = false;
        deferredStun = false;
        _healthLocked = false;
        _deferredPhaseTransition = null;
        currentBossPhase = 0;
        if (_phaseTransitionRoutine != null) { StopCoroutine(_phaseTransitionRoutine); _phaseTransitionRoutine = null; }
        stunTimer = 0f;
        _isStunExiting = false;
        _resumeStunAfterLaunch = false;
        _getupInterrupted = false;
        if (_getupRoutine != null)
        {
            StopCoroutine(_getupRoutine);
            _getupRoutine = null;
        }
        if (_stunExitRoutine != null)
        {
            StopCoroutine(_stunExitRoutine);
            _stunExitRoutine = null;
        }
        launchTimer = 0f;
        _remainingStunOnLaunch = 0f;
        // BUG FIX: attackTimer 初始化为一个正数，避免第一次进入 UpdateAttack() 时
        // 立即触发冷却结束（attackTimer <= 0f），导致攻击动画被跳过
        attackTimer = 1f;
        attackAnimTimer = 0f;
        isAttackAnimating = false;
        isAttackDrawPhase = false;
        isCFrame = false;
        _pierceHighlighted = false;
        UpdateOutlineState();
        _currentAttackStep = 0;
        ClearHitFrames();

        // 恢复转阶段覆写的字段为 prefab 原始值
        RestoreOriginalValues();
        moveProgress = 0f;
        isMovingToNextRow = false;
        isRushMove = false;
        pendingRushMove = false;
        _rushMoveOrderOwner = RushMoveOrderOwner.None;
        _rushMoveOrderGeneration = 0;
        _rushMoveMode = RushMoveMode.Step;
        rushMoveDelayTimer = 0f;
        rushMoveChainTriggered = false;
        targetRow = -1;
        actionCooldownTimer = 0f;
        bounceYOffset = 0f;
        _attackTween = null;
        _attackTelegraph = GetComponentInChildren<EnemyAttackTelegraph>(true);
        _attackTelegraph?.StopWarning();
        transform.localScale = originalScale;
        transform.localRotation = originalRotation;
        // 创建或恢复每个 Renderer 的专属材质实例，并恢复对象池残留的视觉状态。
        CreateFlashMaterials();
        ResetWaveTint();

        initialized = true;

        gameObject.SetActive(true);

        // 缓存 Animator 和 SpriteController 引用
        _animator = GetComponent<Animator>();
        _spriteCtrl = GetComponent<EnemySpriteController>();
        _attackTelegraph = GetComponentInChildren<EnemyAttackTelegraph>(true);
        _cowardTransitionPlaying = false;
        PlayIdleVisual();

        // 订阅 QTE 完成事件（BOSS 由 Idle 调度，QTE 完成后不依赖 QTEController 自管理冷却）
        if (isBoss)
        {
            var qte = GetQTEController();
            if (qte != null) qte.OnQTEAttackFinished += OnQTEAttackFinished;
        }

        // 恢复 QTE 数据（QTEController 的 qteData 可能在转阶段时被 SwitchQteData 覆写）
        // 始终调用 SwitchQteData，即使 _originalQteData 为 null（prefab 无 QTE 但转阶段获得了）
        var qteCtrl = GetQTEController();
        if (qteCtrl != null)
            qteCtrl.SwitchQteData(_originalQteData);

        // 应用当前阶段的 QTE 数据（覆盖 prefab 默认值，确保初始阶段走正确的 QTE 配置）
        if (isBoss)
        {
            var currentPhase = GetCurrentPhaseData();
            if (currentPhase?.qteData != null)
                qteCtrl.SwitchQteData(currentPhase.qteData);
        }

        // 缓存 Attack AnimationClip（用于远程 DOTween 时长同步）
        _attackClip = null;
        if (_animator != null && _animator.runtimeAnimatorController != null)
        {
            foreach (var clip in _animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name.Contains("_Attack") && !clip.name.Contains("_CAttack"))
                {
                    _attackClip = clip;
                    break;
                }
            }
        }

        // 原始姿态仅在 Awake 从预制体快照；不能在池复用时用残留动画状态覆盖。

        UpdateWorldPosition();

        // 如果敌人已在攻击范围内（由 EnemyConfig.attackRange 决定），直接进入攻击状态
        int atkRange = (int)Mathf.Max(1, attackRange);
        if (rowIndex < atkRange)
        {
            StartAttacking();
        }
    }

    /// <summary>
    /// 应用波次敌人强化（血量 / 攻速 / 伤害倍率 + 颜色叠加）
    /// 在 Initialize() 之后、共享血量组创建之前调用
    /// 始终基于 _prefab* 原始值计算，不累积
    /// </summary>
    public void ApplyWaveScaling(float hpMult, Color tint, float atkSpdMult = 1f, float dmgMult = 1f, float bossHpMult = 0f)
    {
        if (_prefabMaxHealth <= 0f) return;

        float effectiveHpMult = (isBoss && bossHpMult > 0f) ? bossHpMult : hpMult;
        maxHealth = _prefabMaxHealth * effectiveHpMult;
        currentHealth = maxHealth;

        attackSpeed = _prefabAttackSpeed * atkSpdMult;
        attackDamage = _prefabAttackDamage * dmgMult;

        Color finalColor = _prefabColor * tint;
        finalColor.a = _prefabColor.a;

        // 每波都显式写入（包括白色），避免对象池复用保留上一波染色。
        if (_spriteRenderer != null)
            _spriteRenderer.color = finalColor;

        // MeshRenderer: 使用独立 MaterialPropertyBlock 设 _Color，避免与描边 _propBlock 冲突
        if (renderers == null || renderers.Length == 0) return;

        if (_tintPropBlock == null)
            _tintPropBlock = new MaterialPropertyBlock();
        _tintPropBlock.SetColor("_Color", finalColor);

        foreach (var r in renderers)
        {
            if (r is SpriteRenderer) continue; // 已处理
            r.SetPropertyBlock(_tintPropBlock);
        }
    }

    /// <summary>
    /// 每帧更新
    /// </summary>
    private void Update()
    {
        if (!initialized || state == EnemyState.Dead) return;

        UpdateHitStop();
        UpdateFreezeChanceDecay();
        FreezeSys.TickPostFreezeRecovery();
        if (_pierceHighlighted)
            UpdateOutlineState();

        // 架势时间恢复到期（独立于状态，击飞中也会继续计时）
        if (_poiseRecoveryEndTime > 0f && Time.time >= _poiseRecoveryEndTime)
        {
            currentPoise = maxPoise;
            _poiseRecoveryEndTime = 0f;
            OnPoiseChanged?.Invoke(this, currentPoise, maxPoise);
        }

        switch (state)
        {
            case EnemyState.Stunned:
                UpdateStun();
                break;
            case EnemyState.Launched:
                UpdateLaunch();
                break;
            case EnemyState.GettingUp:
                // 起身由 Animator 状态和 _getupRoutine 驱动；期间不调度攻击或补齐。
                break;
            case EnemyState.Moving:
                UpdateMovement();
                break;
            case EnemyState.Attacking:
                UpdateAttack();
                break;
            case EnemyState.QTEAttacking:
                // QTE 攻击演出中，由 QTEController 驱动，此处不做任何事
                break;
            case EnemyState.Frozen:
                UpdateFrozen();
                break;
            case EnemyState.IceBreaking:
                UpdateIceBreaking();
                break;
            case EnemyState.Idle:
            default:
                if (tutorialHold) break;   // 教学：不调度任何新行动
                if (_cavalry != null && _cavalry.Tick())
                    break;
                // BOSS Idle 调度：冷却计时结束后随机选择行动
                if (isBoss && bossState == BossState.InCombat)
                {
                    actionCooldownTimer -= Time.deltaTime;
                    if (actionCooldownTimer <= 0f)
                        SelectBossAction();
                }
                break;
        }

        // 命中帧结算：声明命中帧时，出伤时刻由动画帧（屏幕上真实显示的精灵）驱动，与位移 tween 解耦
        if (state == EnemyState.Attacking)
            UpdateAttackHitFrames();

        // Only an already assigned scheduler order may resume after its delay.
        // 冻结期间整段计时暂停（冻结语义是挂起，不是取消）。
        if (!IsActionPaused && rushMoveDelayTimer > 0f)
        {
            rushMoveDelayTimer -= Time.deltaTime;
            if (rushMoveDelayTimer <= 0f && HasRushMoveOrder)
                TryStartRushMove();
        }

        // Boss 应战缓冲计时器：到达应战排后短暂无敌（给出场动画留时间）
        if (!IsActionPaused && _bossEngageTimer > 0f)
        {
            _bossEngageTimer -= Time.deltaTime;
            if (_bossEngageTimer <= 0f)
            {
                bossState = BossState.InCombat;
                DebugLog.Info($"[BOSS_ADVANCE] Boss进入战斗（缓冲结束）: {DebugTag}, col={columnIndex}, row={rowIndex}");
                OnBossEngaged?.Invoke(this);
                // 进入 Idle 调度：等待 actionCooldownTimer 后开始行动
                SetBossActionCooldown();
            }
        }

        // 更新受伤闪白计时器
        if (hitFlashTimer > 0f)
        {
            hitFlashTimer -= Time.deltaTime;
            if (hitFlashTimer <= 0f)
            {
                DebugLog.Info($"[Enemy] 闪白结束: {DebugTag}");
            }
        }

        // 每帧更新透明度（含闪白效果）
        UpdateAlpha();
    }


    // ── BOSS 多阶段：实现见 EnemyBossPhases（批次 3 刀 B），此处保留同名薄转发 ──

    private BossPhaseData GetCurrentPhaseData() => BossPhases.GetCurrentPhaseData();

    private void SetBossActionCooldown() => BossPhases.SetBossActionCooldown();

    private void SelectBossAction() => BossPhases.SelectBossAction();

    private void EnterPhaseTransition(BossPhaseData nextPhase) => BossPhases.EnterPhaseTransition(nextPhase);

    private void CompletePhaseTransition(BossPhaseData nextPhase) => BossPhases.CompletePhaseTransition(nextPhase);

}

/// <summary>
/// 伤害类型枚举——已移到 Assets/Scripts/Data/CombatEnums.cs（数据层，2026-10-20 批次 1）。
/// </summary>

/// <summary>
/// 招架眩晕阈值：招架造成伤害后，若敌人血量低于 healthPercent，则眩晕 stunDuration 秒
/// </summary>
[System.Serializable]
public struct ParryStunThreshold
{
    [UnityEngine.Range(0f, 1f)] public float healthPercent;
    public float stunDuration;
}
