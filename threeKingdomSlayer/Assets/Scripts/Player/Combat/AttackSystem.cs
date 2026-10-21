using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using DG.Tweening;

/// <summary>
/// 攻击系统
/// 实现6种攻击类型：戳击、斩击、穿刺、横扫、挑飞、招架
/// 所有攻击参数从 HeroConfig 的技能配置中读取
/// </summary>
public partial class AttackSystem : MonoBehaviour
{
    public static AttackSystem Instance { get; private set; }

    /// <summary>
    /// 关卡/阵型信息（只读）。攻击落点、瞄准指示、位移都依赖排偏移与排间距，
    /// 历史上直接摸 StageController 门面（22 处），现只依赖这份只读契约
    /// （2026-10-20 批次 1）。延迟解析：AttackSystem 可能早于 StageController Awake。
    /// </summary>
    private IStageFormation _stageFormation;

    private IStageFormation StageFormation => _stageFormation ??= StageRoles.Formation;

    /// <summary>攻击执行事件（仅 Slash/Sweep/Stab/Pierce/Launch 五种有效攻击触发）</summary>
    public System.Action<AttackType, int, bool> OnAttackPerformed; // attackType, targetColumn, slashLeftToRight

    /// <summary>Stab攻击最后一个命中的敌人（供被动效果查询目标）</summary>
    public Enemy LastStabTargetEnemy { get; private set; }

    [Header("组件引用")]
    public ColumnManager columnManager;
    public PlayerState playerState;

    [Header("招架远程飞行物")]
    [Tooltip("招架时扫描飞行物的范围半径")]
    public float parryProjectileRange = 4f;

    [Header("终结技·释放格挡（局外解锁项）")]
    [Tooltip("临时解锁开关：勾选 = 该升级已解锁 —— 终结技释放时附带 Parry 式格挡（反弹飞行物 + 打断正在攻击的敌人），仅机制、无 Parry 视觉。\n" +
             "正式版本应由局外解锁（天赋树/存档）写入，这里先用编辑器开关代替，避免阻塞内容验证。")]
    [SerializeField] private bool finisherParryUnlocked = false;

    [Header("幻影攻击")]
    [Tooltip("幻影攻击使用的紫色透明材质")]
    [SerializeField] private Material _phantomMaterial;

    [Header("被动效果Prefab")]
    [Tooltip("折返波使用的视觉Prefab")]
    [SerializeField] private GameObject _returnWavePrefab;
    [Tooltip("连锁弹射闪电连线使用的视觉Prefab")]
    [SerializeField] private GameObject _chainBounceVisualPrefab;
    [Tooltip("弹射连线用的Chain预制体（回退方案）")]
    [SerializeField] private GameObject _chainBouncePrefab;

    [Header("Stab动画帧")]
    [Tooltip("普通 Stab 高速中间帧（仅用于 Stab，不影响 Slash）")]
    [SerializeField] private Sprite _stabSpeedSprite;
    [Tooltip("戳击旋转帧1（stab_rotate1，从左往右朝向）")]
    [SerializeField] private Sprite _stabRotate1Sprite;
    [Tooltip("戳击旋转帧2（stab_rotate2，从左往右朝向）")]
    [SerializeField] private Sprite _stabRotate2Sprite;

    [Header("Launch动画帧")]
    [Tooltip("Launch帧1：stab_charge2")]
    [SerializeField] private Sprite _launchSprite1;
    [Tooltip("Launch帧2：stab_charge1")]
    [SerializeField] private Sprite _launchSprite2;
    [Tooltip("Launch帧3：stab")]
    [SerializeField] private Sprite _launchSprite3;

    [Header("攻击冷却模式")]
    [Tooltip("勾选→动作锁定模式（攻击动画期间锁定所有攻击输入）。\n取消→独立CD模式（每招独立冷却，可交替连打）。\n独立CD模式保留作为未来可能的奖励效果（如技能移除动作硬直）。")]
    public bool useActionBasedCooldown = false;

    [Header("Stab 射出内收角")]
    [Tooltip("启用时使用下方五列内收角覆盖；关闭时根据当前前排阵型自动计算。数值直接作为 StabRay 容器的 Yaw：当前镜头下增大绝对值会让视觉更内收。子 Prefab 不会再叠加此角度。")]
    [SerializeField] private bool useStabRayAngleOverrides;
    [FormerlySerializedAs("stabRayYawOverrides")]
    [SerializeField] private float[] stabRayInwardAngleOverrides = { -30f, -15f, 0f, 15f, 30f };

    [Header("Stab 视觉起点偏移")]
    [Tooltip("仅改变五列 Stab 视觉的世界 X 起点；按左到右排列，不影响命中射线、伤害、范围或击退。")]
    [SerializeField] private float[] stabVisualStartXOffsets = { -1f, -0.5f, 0f, 0.5f, 1f };

    [Header("Pierce 观察慢放")]
    [Tooltip("观察用：1=原速，3=1/3速。确认后调回 1。")]
    [SerializeField] private float pierceTimeScale = 3f;

    private float _actionLockTimer;
    private float _stabVisualTimer;
    private StabSweepEffect _lastStabEffect;
    private StabStartPose _lastStabPose = StabStartPose.None;
    private float _lastStabPoseTime;
    /// <summary>交接姿态的有效期：超过这个间隔就不再用旧姿态起步（避免长时间后再出招时跳到旧位置）</summary>
    private const float StabPoseHandoffWindow = 0.6f;
    private ChargeStabVisual _chargeStabVisual;
    /// <summary>最近一次戳击的目标列：连段蓄力时用它算「当前朝向 → 指向列」的偏摆增量</summary>
    private int _lastStabColumn = -1;
    /// <summary>招式自带击退的复用缓冲，避免每次命中都 new List</summary>
    private readonly List<Enemy> _movePushBuffer = new List<Enemy>(1);

    /// <summary>
    /// 当前是否处于攻击动作播放中（动作锁定计时器未结束）
    /// </summary>
    public bool IsActionPlaying => _actionLockTimer > 0f;

    /// <summary>最近一次成功执行招式的攻击类型（供招式状态机同步当前节点）</summary>
    public AttackType LastMoveAttackType { get; private set; }

    /// <summary>
    /// 最近一次成功执行招式的参考时长。
    /// 动作锁模式下等于实际动作锁（含 Launch 观察延长）；独立 CD 模式下为 actionDuration / 攻速。
    /// </summary>
    public float LastMoveDuration { get; private set; }

    /// <summary>动作锁剩余时间（0 表示可自由行动）</summary>
    public float ActionLockRemaining => _actionLockTimer > 0f ? _actionLockTimer : 0f;

    /// <summary>本次招式的蓄力等级（0 = 未蓄力；>=1 为蓄力招式等级）</summary>
    public int CurrentChargeLevel => _currentChargeLevel;

    /// <summary>
    /// 「终结技·释放格挡」是否已解锁。
    /// 当前由 Inspector 开关代替（局外解锁系统尚未实现）；将来由局外天赋/存档写入这个属性。
    /// </summary>
    public bool FinisherParryUnlocked
    {
        get => finisherParryUnlocked;
        set => finisherParryUnlocked = value;
    }

    /// <summary>最近一次成功执行招式所用的配置资产（招式表节点或武将默认配置）</summary>
    public AttackSkillConfig LastMoveConfig { get; private set; }

    /// <summary>
    /// 尝试取消最近一次戳击的起手：仅在其首个命中结算前有效。
    /// 供「按下即出轻攻击」模型下，滑动在起手阶段改写本次输入使用。
    /// 取消成功后清除动作锁与戳击视觉门；不触发能量、被动计数与击退波。
    /// 独立 CD 模式下不提供取消（避免破坏冷却一致性）。
    /// </summary>
    public bool TryCancelLightAttackStartup()
    {
        if (!useActionBasedCooldown) return false;

        StabSweepEffect effect = _lastStabEffect;
        _lastStabEffect = null;
        if (effect == null) return false;
        if (!effect.TryCancelBeforeHit()) return false;

        _actionLockTimer = 0f;
        _stabVisualTimer = 0f;
        LastMoveDuration = 0f;
        return true;
    }

    /// <summary>
    /// 连段蓄力：让当前戳击的枪体停在回收段中途等玩家蓄力（就地在枪体上换蓄力素材帧，
    /// 不启用蓄力视觉的进出场实例）。仅在招式节点存在「需要蓄力的后续边」时由状态机调用。
    /// </summary>
    public void BeginComboChargeHold()
    {
        if (_lastStabEffect == null) return;
        if (_chargeStabVisual == null) _chargeStabVisual = FindObjectOfType<ChargeStabVisual>();

        _lastStabEffect.SetChargeVisualSource(_chargeStabVisual);
        _lastStabEffect.BeginChargeHold();
    }

    /// <summary>按住时长（秒）透传给保持中的枪体，驱动「拉回蓄势位」的进度；aimColumn = 当前手指指向的列（供指向偏摆）</summary>
    public void UpdateComboChargeHold(float heldSeconds, int aimColumn = -1)
    {
        if (_lastStabEffect == null) return;
        _lastStabEffect.SetChargeHoldHeldSeconds(heldSeconds);

        // 指向偏摆：把「枪身当前朝向」转到「目标列第 1 排的位置」，差值交给枪体，
        // 上限由招式资产的 chargeHoldYawDegrees 夹住（0 = 不偏摆）。
        // 注意：不能用 GetStabRayYaw——它会被场景里的 per-column 角度覆盖影响，算出的角偏小，
        // 会让枪身“转到一半就停”、和玩家看到的目标点对不上。这里直接用世界坐标算。
        if (aimColumn >= 0 && aimColumn != _lastStabColumn)
        {
            float spacing = StageFormation != null ? StageFormation.GetRowSpacing() : 2.5f;
            int visibleRows = StageFormation != null ? StageFormation.GetMaxVisibleRows() : 5;
            float formationOffsetZ = StageFormation != null ? StageFormation.GetFormationOffsetZ() : 0f;
            Vector3 origin = _lastStabEffect.transform.position;
            float aimX = StageFormation != null
                ? StageFormation.GetFormationOffset(aimColumn, 0)
                : (aimColumn - 2) * 2f;
            float aimZ = GetEnemyRootWorldZ() + (visibleRows - 1) * (-spacing) + formationOffsetZ;
            float targetYaw = Mathf.Atan2(aimX - origin.x, aimZ - origin.z) * Mathf.Rad2Deg;
            float currentYaw = _lastStabEffect.transform.eulerAngles.y;
            _lastStabEffect.SetChargeAimYawDelta(Mathf.DeltaAngle(currentYaw, targetYaw));
        }
        else
        {
            _lastStabEffect.SetChargeAimYawDelta(0f);
        }
    }

    /// <summary>松开/节点切换：没有蓄力招式接手时让枪体恢复回收</summary>
    public void EndComboChargeHold()
    {
        if (_lastStabEffect != null) _lastStabEffect.EndChargeHold();
    }

    /// <summary>
    /// 让出枪体：非戳击、非挑飞招式接手时，把当前戳击的枪体立刻收掉
    /// （无论在蓄力保持中、还是正在自己续完回收），避免同时出现两把枪。
    /// </summary>
    private void ReleaseHeldStabVisual()
    {
        StabSweepEffect stab = _lastStabEffect;
        if (stab == null) return;

        stab.AbortAndDestroy();
        _lastStabEffect = null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        RegisterBuiltinAttackExecutors();
    }

    // ───────── 招式执行器注册表（扩展点）─────────
    // 新增一种招式 = 注册一个执行器（AttackType + 执行委托），不用再改 TryExecuteAttack 的分派。
    private readonly Dictionary<AttackType, System.Func<AttackExecutionContext, bool>> _attackExecutors =
        new Dictionary<AttackType, System.Func<AttackExecutionContext, bool>>();

    /// <summary>
    /// 注册**招式执行器**（扩展点）。
    /// 同一招式类型后注册者覆盖先注册者；未注册的招式不执行任何东西（与旧 switch 的默认分支一致）。
    /// </summary>
    public void RegisterAttackExecutor(AttackType type, System.Func<AttackExecutionContext, bool> executor)
    {
        if (executor == null) return;
        _attackExecutors[type] = executor;
    }

    /// <summary>该招式是否已注册执行器（调试 / 工具用）。</summary>
    public bool HasAttackExecutor(AttackType type) => _attackExecutors.ContainsKey(type);

    /// <summary>内置的六个招式执行器（保持改造前的行为）。</summary>
    private void RegisterBuiltinAttackExecutors()
    {
        RegisterAttackExecutor(AttackType.Stab, ctx => ExecuteStab(ctx.targetColumn));
        RegisterAttackExecutor(AttackType.Slash, ctx => ExecuteSlash(ctx.slashLeftToRight, ctx.slashVisualTilt));
        RegisterAttackExecutor(AttackType.Pierce, ctx => ExecutePierce(ctx.targetColumn));
        RegisterAttackExecutor(AttackType.Sweep, ctx => ExecuteSweep());
        RegisterAttackExecutor(AttackType.Launch, ctx => ExecuteLaunch());
        RegisterAttackExecutor(AttackType.Parry, ctx => ExecuteParry());
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (_actionLockTimer > 0f)
            _actionLockTimer -= Time.deltaTime;
        if (_stabVisualTimer > 0f)
            _stabVisualTimer -= Time.deltaTime;

        // 持续记录当前戳击的姿态：即使特效被外部清理（如路线转场），下一段也能接着走
        if (_lastStabEffect != null)
        {
            _lastStabPose = _lastStabEffect.CapturePose();
            _lastStabPoseTime = Time.time;
        }
    }

    private void Start()
    {
        if (columnManager == null)
            columnManager = FindObjectOfType<ColumnManager>();
        if (playerState == null)
            playerState = FindObjectOfType<PlayerState>();
        if (_chargeStabVisual == null)
            _chargeStabVisual = FindObjectOfType<ChargeStabVisual>();
    }

    /// <summary>
    /// 尝试执行攻击
    /// BUG FIX: 只有实际命中至少一个敌人时，才触发冷却和消耗
    /// cancelCurrentMove：由招式状态机在接续窗口内解析出后继招式时传入，
    /// 表现为「取消当前招式的收尾」直接接下一段；默认 false，行为与改造前一致。
    /// </summary>
    public bool TryExecuteAttack(AttackType attackType, int targetColumn = -1, bool slashLeftToRight = true,
        float slashVisualTilt = 0f, bool chargedAttack = false, bool cancelCurrentMove = false, int chargeLevel = 0,
        AttackSkillConfig moveConfig = null)
    {
        _currentChargeLevel = chargeLevel;
        if (playerState == null) return false;
        if (playerState.stageState != StageState.InProgress) return false;

        // 招架是局外解锁项：天赋树里配了节点且还没点时，竖滑不蓄力不再出招
        if (attackType == AttackType.Parry)
        {
            var hero = playerState.heroConfig;
            var tree = hero != null ? hero.talentTree : null;
            var parryCfg = hero != null ? hero.GetSkillConfig(AttackType.Parry) : null;
            if (TalentTreeState.IsMoveGatedAndLocked(tree, parryCfg))
            {
                Debug.Log("[Talent] 招架尚未解锁，本次输入不生效");
                return false;
            }
        }

        if (!cancelCurrentMove && attackType == AttackType.Stab && _stabVisualTimer > 0f)
            return false;

        // 冷却检查：新模式（动作锁定）→ 全局锁；旧模式 → 独立技能CD
        if (useActionBasedCooldown)
        {
            if (!cancelCurrentMove && _actionLockTimer > 0f) return false;
        }
        else
        {
            if (!playerState.IsAttackReady(attackType))
            {
                Debug.Log($"[AttackSystem] {attackType} 冷却中");
                return false;
            }
        }

        // 招式表解析出的招式资产从这里开始生效（守卫早退时不留下残留）
        _currentConfigOverride = moveConfig;

        // 蓄力保持中的枪体：非戳击、非挑飞招式接手时先收掉
        // （戳击的连段交接在 ExecuteStab 内处理；挑飞会先取用蓄势位姿态再自己收掉）
        if (attackType != AttackType.Stab && attackType != AttackType.Launch) ReleaseHeldStabVisual();

        bool hitAny = false;
        _attackIsCharged = chargedAttack;
        // 破冰会话：本次招式的合规判定在这里开，命中时由玩家侧会话 IceBreakSession 校验作用域；
        // 会话寿命跟本次招式实际动作时长走（设计 §7：招式结束即关闭）
        // 命中反应注册表（扩展点）：广播给所有已注册的处理器（内置冰系 + 自定义）。
        DamageReactionRegistry.BeginAttack(GetConfig(attackType), GetAttackDuration(GetConfig(attackType)));

        // 招式分派：走执行器注册表（内置六个 + 可注册的扩展实现）。
        if (_attackExecutors.TryGetValue(attackType, out var attackExecutor))
        {
            hitAny = attackExecutor(new AttackExecutionContext
            {
                system = this,
                targetColumn = targetColumn,
                slashLeftToRight = slashLeftToRight,
                slashVisualTilt = slashVisualTilt,
                charged = chargedAttack,
            });
        }
        _attackIsCharged = false;

        // 可破坏补给：戳击命中目标列时，连带破坏该列的道具。
        // 不参与 hitAny 判定（ExecuteStab 恒返回 true），只在道具确实存在时生效。
        if (attackType == AttackType.Stab && targetColumn >= 0)
            BreakableProp.TryHitColumn(targetColumn, attackType, 1f);

        if (hitAny)
        {
            // 动作锁模式只覆盖玩家自身的出手与收招；离手飞行物拥有独立生命周期。
            float moveDuration;
            if (useActionBasedCooldown)
            {
                var cfg = GetConfig(attackType);
                _actionLockTimer = GetAttackDuration(cfg);
                if (attackType == AttackType.Launch)
                    _actionLockTimer = Mathf.Max(_actionLockTimer, LaunchVisualEffect.GetObservationDuration(cfg));
                moveDuration = _actionLockTimer;
            }
            else
            {
                playerState.StartCooldown(attackType);
                moveDuration = GetAttackDuration(GetConfig(attackType));
            }

            // 供招式状态机同步节点时钟：参考时长与实际生效的动作锁一致
            LastMoveAttackType = attackType;
            LastMoveDuration = moveDuration;
            LastMoveConfig = GetConfig(attackType);

            if (attackType == AttackType.Stab)
            {
                var stabConfig = GetConfig(AttackType.Stab);
                float stabDuration = GetAttackDuration(stabConfig);
                _stabVisualTimer = stabDuration;
            }

            // Stab 与 Slash 的资源结算由实际首次命中时触发；空挥只消耗动作锁定/冷却。
            if (attackType != AttackType.Stab && attackType != AttackType.Slash)
            {
                UltimateSystem.Instance?.AddEnergyForAttack(attackType);
                if (attackType != AttackType.Parry && attackType != AttackType.Ultimate)
                    OnAttackPerformed?.Invoke(attackType, targetColumn, slashLeftToRight);
            }

            // 注意：Stab 的击退是异步的（hit 回调在射线穿过时才触发）。
            // 因此 _currentConfigOverride 不能在这里清，否则命中回调读到 null 会 fall back 到默认 Zhangfei_Stab（无击退）。
            // 真正的清在下方 StabSweepEffect 的 onComplete lambda 里（参见 Bug #2 修复注释）。
            if (attackType != AttackType.Stab)
                _currentConfigOverride = null;
            return true;
        }

        if (attackType != AttackType.Stab)
            _currentConfigOverride = null;
        Debug.Log($"[AttackSystem] {attackType} 未命中任何敌人，不消耗冷却");
        return false;
    }

}
