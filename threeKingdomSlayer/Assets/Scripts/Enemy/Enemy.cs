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
    QTEAttacking  // BOSS QTE 攻击演出中
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
/// 敌人实体 - MonoBehaviour
/// 管理敌人的生命值、架势值、状态机、前进移动、透明度渐变
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("基础属性")]
    public string enemyName = "骷髅兵";
    public int enemyId;

    /// <summary>
    /// 实例唯一 ID（运行时分配，用于调试日志区分同一 prefab 的不同实例）
    /// </summary>
    [System.NonSerialized] public int instanceId;
    private static int _nextInstanceId = 1;

    /// <summary>
    /// 调试标签：instanceId(enemyId)，如 #3(101)
    /// </summary>
    public string DebugTag => $"#{instanceId}({enemyId})";

    [Header("战斗属性")]
    public float maxHealth = 100f;
    public int occupySlots = 1;
    public float attackSpeed = 1f;
    public float attackDamage = 10f;
    public float attackRange = 1f;
    public float attackSpawnDuration = 0.3f;
    public float attackDrawDuration = 0.3f;
    public bool useAttackFlip = true;
    public float moveSpeed = 1f;

    [Header("架势系统")]
    public float maxPoise = 50f;
    public float stunDuration = 1.5f;

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

    [Header("弱点系统")]
    public float stabDamageMultiplier = 1f;
    public float slashDamageMultiplier = 1f;
    public float pierceDamageMultiplier = 1f;
    public float sweepDamageMultiplier = 1f;
    public float launchDamageMultiplier = 1f;
    public float poiseDamageMultiplier = 1f;

    [Header("招架血量眩晕阈值")]
    public ParryStunThreshold[] parryStunThresholds;

    [System.NonSerialized] public EnemyState state = EnemyState.Idle;
    [System.NonSerialized] public float currentHealth;
    [System.NonSerialized] public float currentPoise;
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
    public int columnIndex;
    public int rowIndex; // 0 = 最前排

    // 内部状态
    private float stunTimer;
    private float _appliedStunDuration; // 实际应用的眩晕时长（用于 UI 恢复进度）
    private float _poiseRecoveryEndTime; // 架势恢复完成的时间点（Time.time + duration）
    private float launchTimer;
    private float launchVelocityY;     // 当前Y轴速度
    private Vector3 launchStartLocalPos; // 挑飞起始位置
    private float currentLaunchYHeight;   // 本次挑飞的随机 Y 高度（仅用于初速度计算）
    private float _remainingStunOnLaunch;  // 挑飞时被中断的眩晕剩余时间（落地后恢复）
    private float attackTimer;      // 攻击冷却计时器（攻击动画结束后开始冷却）
    private float attackAnimTimer;  // 攻击动画计时器（攻击动作执行时间）
    public bool isAttackAnimating; // 是否正在播放攻击动画（AttackSpawn 或 AttackDraw）
    public bool isAttackDrawPhase;  // 是否处于攻击收招阶段（不可被招架打断）

    // 补齐移动链式触发
    // pendingRushMove = true 表示该敌人已标记为需要向前补齐，
    // TryStartRushMove() 会根据当前状态决定何时开始移动。
    // 链式触发：补齐移动完全完成（moveProgress >= 1.0）时，
    // OnRushMoveComplete 事件通知列管理器启动下一个敌人。
    // 必须等待前一敌人完全补齐完毕，后一敌人才能开始补齐。
    public bool pendingRushMove; // 标记需要向前补齐
    public System.Action<Enemy> OnRushMoveComplete; // 补齐移动完成事件（移动完全结束时触发）
    private float moveProgress; // 0~1, 当前排内移动进度
    private bool isMovingToNextRow;

    // 标记当前移动是否为"补齐移动"（由 RemoveEnemy/UpdateEnemyRow 触发）。
    // 补齐移动完成后不触发 UpdateEnemyRow()，避免无限循环。
    private bool isRushMove;

    // BUG FIX: 新增补齐延迟计时器（Problem 3）
    // 补齐移动完成后，若判定需继续补齐（rowIndex >= attackRange），
    // 先等待 rushMoveDelay 秒再开始下一次补齐移动。
    // 这样可以将 rushMoveSpeed 设置得更快（单次移动更迅速），
    // 而整体补齐速度通过 rushMoveDelay 来调节，达到"快移动+停顿"的效果。
    private float rushMoveDelayTimer;
    // OnRushMoveComplete 是否已触发（避免在同一个移动过程中重复触发）
    private bool rushMoveChainTriggered;
    // 敌人此次补齐的目标排位置（即列表位置）
    // 由 Column.RemoveEnemy() / ColumnManager.UpdateEnemyRow() 设置：
    // SetRowIndex(i+1) 后设置 targetRow = i
    // 在补齐移动完成的延迟循环中，检查 rowIndex <= targetRow 时停止补齐，
    // 防止多个敌人在 delay 循环中全部汇聚到 row=0。
    // targetRow = -1 表示未设置（非补齐移动），此时沿用旧行为。
    public int targetRow = -1;
    private Renderer[] renderers;
    // 材质实例数组（用于闪白效果）
    // 通过 renderer.material 创建实例，避免 MaterialPropertyBlock 在对象禁用时不生效的问题。
    // 在 Initialize() 中创建，在 ResetEnemy() 中销毁。
    private Material[] flashMaterials;
    private bool initialized;

    // 受伤闪白相关
    private float hitFlashTimer; // 闪白剩余时间
    private Color originalColor = Color.white; // 精灵原始颜色（白色）
    private const float HIT_FLASH_DURATION = 0.15f; // 闪白持续时间

    // DOTween: 前进补齐时 Y 轴弹跳偏移（由 DOTween 驱动，在 UpdateWorldPosition 中应用）
    private float bounceYOffset;

    // DOTween: 当前攻击动画序列（用于在 Die() 中取消正在执行的攻击动作）
    private Sequence attackSequence;

    // QTE 控制器缓存
    private QTEController _qteController;

    // 敌人物体原始缩放值（从预制体读取，用于攻击动画/死亡后还原）
    // 不能硬编码为 Vector3.one，因为不同敌人可能有不同默认缩放（如 0.5）
    private Vector3 originalScale;

    // 血条UI缓存
    private EnemyHealthBar cachedHealthBar;
    private EnemySpriteController cachedSpriteController;

    // 事件
    public System.Action<Enemy> OnDeath;
    public System.Action<Enemy> OnDamageTaken;
    public System.Action<Enemy, float, float> OnHealthChanged; // enemy, current, max
    public System.Action<Enemy, float, float> OnPoiseChanged;  // enemy, current, max
    public System.Action<Enemy> OnBossEngaged;                  // Boss 到达应战排

    // Boss 分阶段推进
    private System.Action _onColumnsModifiedHandler;
    private float _bossEngageTimer; // Boss 到达应战排后的无敌缓冲时间

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
    }

    /// <summary>
    /// 对象从池中重新激活时重建 Renderer 缓存
    /// Awake 只在首次创建时执行，池复用不会重新 Awake
    /// </summary>
    private void OnEnable()
    {
        renderers = GetComponentsInChildren<Renderer>();
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
        columnIndex = col;
        rowIndex = row;

        currentHealth = maxHealth;
        currentPoise = maxPoise;
        state = EnemyState.Idle;
        bossState = BossState.None;
        stunTimer = 0f;
        launchTimer = 0f;
        _remainingStunOnLaunch = 0f;
        // BUG FIX: attackTimer 初始化为一个正数，避免第一次进入 UpdateAttack() 时
        // 立即触发冷却结束（attackTimer <= 0f），导致攻击动画被跳过
        attackTimer = 1f;
        attackAnimTimer = 0f;
        isAttackAnimating = false;
        isAttackDrawPhase = false;
        moveProgress = 0f;
        isMovingToNextRow = false;
        isRushMove = false;
        pendingRushMove = false;
        // BUG FIX: 初始化重置新字段
        rushMoveDelayTimer = 0f;
        rushMoveChainTriggered = false;
        targetRow = -1;
        bounceYOffset = 0f;
        attackSequence = null;
        // 创建材质实例（用于闪白效果）
        // 通过 renderer.material 创建实例，避免 MaterialPropertyBlock 在对象禁用时不生效
        CreateFlashMaterials();

        initialized = true;

        gameObject.SetActive(true);

        // 保存原始缩放值，用于攻击动画/死亡后的还原
        originalScale = transform.localScale;

        UpdateWorldPosition();

        // 如果敌人已在攻击范围内（由 EnemyConfig.attackRange 决定），直接进入攻击状态
        int atkRange = (int)Mathf.Max(1, attackRange);
        if (rowIndex < atkRange)
        {
            StartAttacking();
        }
    }

    /// <summary>
    /// 每帧更新
    /// </summary>
    private void Update()
    {
        if (!initialized || state == EnemyState.Dead) return;

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
            case EnemyState.Moving:
                UpdateMovement();
                break;
            case EnemyState.Attacking:
                UpdateAttack();
                break;
            case EnemyState.QTEAttacking:
                // QTE 攻击演出中，由 QTEController 驱动，此处不做任何事
                break;
            case EnemyState.Idle:
            default:
                // Idle状态下什么都不做，等待外部调用 StartMoving()
                // 注意：不要在这里自动调用 StartMoving()，否则会导致无限循环
                // 因为 UpdateMovement() 完成后会设置 state = Idle
                break;
        }

        // BUG FIX: 补齐移动延迟计时器（Problem 3）
        // 当敌人完成一次补齐移动后，如果还需要继续补齐（rowIndex >= attackRange），
        // 会启动延迟计时器，等待 rushMoveDelay 秒后再开始下一次补齐移动。
        if (rushMoveDelayTimer > 0f)
        {
            rushMoveDelayTimer -= Time.deltaTime;
            if (rushMoveDelayTimer <= 0f)
            {
                // 延迟结束，尝试再次开始补齐移动
                Debug.Log($"[Enemy] 补齐延迟结束，尝试继续补齐: {DebugTag}, col={columnIndex}, row={rowIndex}");
                TryStartRushMove();
            }
        }

        // Boss 应战缓冲计时器：到达应战排后短暂无敌（给出场动画留时间）
        if (_bossEngageTimer > 0f)
        {
            _bossEngageTimer -= Time.deltaTime;
            if (_bossEngageTimer <= 0f)
            {
                bossState = BossState.InCombat;
                Debug.Log($"[Enemy] Boss进入战斗（缓冲结束）: {DebugTag}, col={columnIndex}, row={rowIndex}");
                OnBossEngaged?.Invoke(this);
                StartAttacking();
            }
        }

        // 更新受伤闪白计时器
        if (hitFlashTimer > 0f)
        {
            hitFlashTimer -= Time.deltaTime;
            if (hitFlashTimer <= 0f)
            {
                Debug.Log($"[Enemy] 闪白结束: {DebugTag}");
            }
        }

        // 每帧更新透明度（含闪白效果）
        UpdateAlpha();
    }

    #region 状态切换

     /// <summary>
    /// 开始向前移动（常规移动，基于 moveSpeed 秒数）
    /// BUG FIX: 当 rowIndex == 0 时，敌人已经到达最前排，不应再向前移动。
    /// 如果此时调用 StartMoving()，UpdateWorldPosition() 中 isMovingToNextRow == true 时，
    /// targetRowZ = -((0 - 1) * rowSpacing) = +rowSpacing（正数），
    /// 敌人会向远离玩家的方向（正 Z 方向）移动，表现为"后退"。
    /// 修复：rowIndex == 0 时直接进入 Attacking 状态。
    ///
    /// BUG FIX: 如果已经在 Moving 状态，不重置 moveProgress。
    /// 否则当 Column.RemoveEnemy() 或 ColumnManager.UpdateEnemyRow() 中调用 StartMoving()
    /// 时，会重置 moveProgress = 0f，导致正在移动中的敌人回到移动起点，看起来像是后退。
    ///
    /// 注意：保护检查只基于 state，不检查 isMovingToNextRow。
    /// 因为 ResetMovementState() 会重置 isMovingToNextRow = false，
    /// 如果保护检查依赖 isMovingToNextRow，会导致保护失效，
    /// moveProgress 被重置为 0，造成"无限补齐"的无限循环。
    /// </summary>
    /// <summary>
    /// 开始向前移动
    /// </summary>
    /// <param name="isRush">是否为补齐移动（由死亡/重排列触发）。补齐移动完成后不触发 UpdateEnemyRow()。</param>
    public void StartMoving(bool isRush = false)
    {
        if (state == EnemyState.Dead) return;

        // 逐排补齐模式：非补齐移动时，检查前方整排是否已完全清空
        if (!isRush && StageController.Instance?.GetFillUpRule() == FillUpRule.PerRow && !IsFrontRowClear())
        {
            Debug.Log($"[Enemy] PerRow 模式等待前排清空: {DebugTag}, col={columnIndex}, row={rowIndex}");
            return;
        }

        // 如果敌人已在攻击范围内，直接进入攻击状态而非移动
        // 补齐移动（isRush=true）优先级高于攻击：即使已在攻击范围内，也先向前补齐空位
        int atkRange = (int)Mathf.Max(1, attackRange);
        if (!isRush && rowIndex < atkRange)
        {
            StartAttacking();
            return;
        }

        // 补齐移动：如果已在目标位置或之前，不需要移动，恢复攻击
        if (isRush && targetRow >= 0 && rowIndex <= targetRow)
        {
            StartAttacking();
            return;
        }

        // BUG FIX: 如果已经在 Moving 状态，不重置 moveProgress
        // 避免被 Column.RemoveEnemy() / ColumnManager.UpdateEnemyRow() 重复调用时
        // 重置移动进度，导致敌人回到移动起点（看起来像后退）
        // 注意：不检查 isMovingToNextRow，因为 ResetMovementState() 会重置它
        if (state == EnemyState.Moving)
        {
            return;
        }

        isRushMove = isRush;
        Debug.Log($"[Enemy] StartMoving: {DebugTag}, col={columnIndex}, row={rowIndex}, targetRow={rowIndex - 1}, isRush={isRush}");
        state = EnemyState.Moving;
        isMovingToNextRow = true;
        moveProgress = 0f;

        // DOTween: 前进补齐时 Y 轴弹跳动画（一边前进一边沿 Y 轴跳动）
        if (isRush)
        {
            DOTween.Kill(transform, false); // 终止之前可能残留的弹跳动画
            transform.localScale = originalScale; // 重置被 DOPunchScale 打断后的残留 scale
            float moveDuration = moveSpeed;
            float bounceHeight = 0.5f; // 弹跳峰值高度
            // 通过 DOTween.To 驱动 bounceYOffset，形成抛物线弹跳轨迹
            DOTween.To(() => bounceYOffset, x => bounceYOffset = x, bounceHeight, moveDuration * 0.5f)
                .SetEase(Ease.OutQuad)
                .SetTarget(transform)
                .SetId("rushBounce");
            DOTween.To(() => bounceYOffset, x => bounceYOffset = x, 0f, moveDuration * 0.5f)
                .SetEase(Ease.InQuad)
                .SetDelay(moveDuration * 0.5f)
                .SetTarget(transform)
                .SetId("rushBounce");
        }
    }

    /// <summary>
    /// 开始攻击
    /// BUG FIX: 攻击分为两个阶段：
    ///   阶段1：攻击冷却（attackTimer）— 先进入冷却再攻击
    ///   阶段2：攻击动画（attackAnimTimer）— 冷却结束后播放攻击动画并造成伤害
    /// 这样敌人进入攻击范围后不会立即攻击，而是先进入冷却，符合"先冷却后攻击"的规则。
    ///
    /// 攻击优先级规则（Problem 1 修复）：
    ///   1. 进入攻击范围 → 先进入攻击冷却，再执行攻击
    ///   2. 攻击冷却期间需要向前补齐 → 先补齐再攻击
    ///   3. 正在攻击动画中 → 完成当前攻击后再判断是否需要向前补齐
    /// </summary>
    public void StartAttacking()
    {
        if (state == EnemyState.Dead) return;

        // 若 QTEController 正在等待攻击结束以触发 QTE，不要开始新攻击
        if (isBoss)
        {
            var qte = GetQTEController();
            if (qte != null && qte.State == QTEState.WaitingForAttackFinish)
            {
                // QTE 即将接手，等待 QTE 触发
                // 但需要检查：如果 QTE 还在等待且当前没有攻击动画，立即通知
                if (!isAttackAnimating)
                {
                    qte.OnEnemyAttackComplete();
                }
                return;
            }
        }

        // Boss 首次进入攻击状态：若尚未进入 InCombat，立即进入并创建血条
        if (isBoss && bossState == BossState.None)
        {
            bossState = BossState.InCombat;
            Debug.Log($"[Enemy] Boss直接进入战斗(rowIndex={rowIndex}): {DebugTag}, col={columnIndex}");
            OnBossEngaged?.Invoke(this);
        }

        state = EnemyState.Attacking;
        isAttackAnimating = false;
        isAttackDrawPhase = false;
        // 使用与攻击动画 OnComplete 中相同的冷却计算，保证补齐后首次攻击也遵循攻击 CD
        float totalInterval = (1f / attackSpeed);
        float cooldown = totalInterval * 0.4f;
        if (cooldown < 0.1f) cooldown = 0.1f;
        attackTimer = cooldown;
    }

    public void Stun(float duration)
    {
        if (state == EnemyState.Dead) return;
        // 击飞状态下不允许进入眩晕，否则敌人会冻结在半空
        // 但需要重置 Poise（相当于击飞中破防不触发眩晕，只重置架势）
        if (state == EnemyState.Launched)
        {
            currentPoise = maxPoise;
            OnPoiseChanged?.Invoke(this, currentPoise, maxPoise);
            return;
        }

        // 眩晕时清理移动状态，避免 DOTween 残留和状态不一致
        // 若正在补齐移动，恢复 pendingRushMove 标记，确保眩晕结束后链式补齐能继续
        if (state == EnemyState.Moving)
        {
            if (isRushMove)
                pendingRushMove = true;
            DOTween.Kill(transform, false);
            isMovingToNextRow = false;
            isRushMove = false;
            moveProgress = 0f;
            UpdateWorldPosition();
        }

        state = EnemyState.Stunned;
        stunTimer = duration;
        _appliedStunDuration = duration;
        _poiseRecoveryEndTime = Time.time + duration;
    }

    /// <summary>
    /// 挑飞：打断当前攻击动作（类似招架），将敌人击飞
    /// 若已处于击飞状态则忽略（延长由 TakeDamage 处理）
    /// </summary>
    public void Launch()
    {
        if (state == EnemyState.Dead) return;
        if (state == EnemyState.Launched) return;

        // 保存被中断的眩晕剩余时间，落地后恢复
        _remainingStunOnLaunch = (state == EnemyState.Stunned && stunTimer > 0f) ? stunTimer : 0f;

        // 清理所有 DOTween 动效（攻击动画、受击抖动等）
        // BUG FIX: 移除 DOTween.Kill("punchScale") 和 DOTween.Kill("rushBounce")，
        // 它们是全局 Kill，会误杀其他敌人的 tween。transform.DOKill(false) 已足够清理本对象。
        transform.DOKill(false);
        DOTween.Kill(transform, false);
        if (attackSequence != null && attackSequence.IsActive())
        {
            attackSequence.Kill();
            attackSequence = null;
        }
        UpdateWorldPosition();
        transform.localScale = originalScale;
        isAttackAnimating = false;
        isAttackDrawPhase = false;
        isMovingToNextRow = false;
        pendingRushMove = false;
        // BUG FIX: 不重置 targetRow。当 Launch 攻击错开命中时，
        // RemoveEnemy() 可能在 Launch() 之前设置 targetRow（前方敌人先死），
        // 若此处重置为 -1，落地后 UpdateLaunch() 不会触发补齐，导致链式前移中断。

        state = EnemyState.Launched;
        launchTimer = launchDuration;
        launchStartLocalPos = transform.localPosition;
        currentLaunchYHeight = Random.Range(launchYHeightMin, launchYHeightMax);
        launchVelocityY = Mathf.Sqrt(2f * launchGravity * currentLaunchYHeight);

        Debug.Log($"[Enemy] 挑飞: {DebugTag}, duration={launchDuration:F2}s, v0={launchVelocityY:F2}");
    }

    /// <summary>
    /// 延长浮空时间（被攻击命中时调用）
    /// 从当前位置叠加反弹速度，产生"颠球"效果
    /// </summary>
    public void ExtendLaunch(float extendTime)
    {
        if (state != EnemyState.Launched) return;
        launchTimer += extendTime;
        launchVelocityY = launchReboundVelocity;
        Debug.Log($"[Enemy] 延长浮空: {DebugTag}, +{extendTime:F2}s, 剩余={launchTimer:F2}s, 反弹速度={launchReboundVelocity:F2}");
    }

    /// <summary>
    /// 检查敌人当前是否可被击飞（挑飞）
    /// 基础条件：处于 Stunned 状态
    /// ForceLaunch Buff：始终可击飞
    /// </summary>
    public bool CanBeLaunched()
    {
        if (PlayerState.Instance != null && PlayerState.Instance.HasBuff(BuffType.ForceLaunch))
            return true;
        return state == EnemyState.Stunned;
    }

    /// <summary>
    /// 打断当前攻击动作（招架触发）
    /// 仅在 AttackSpawn 阶段可打断；AttackDraw 收招阶段不可打断，返回 false
    /// 打断后返回攻击冷却阶段（非眩晕），等待 attackTimer 后重新攻击
    /// </summary>
    public bool CancelAttack()
    {
        if (state == EnemyState.Dead) return false;
        // 只在 AttackSpawn 阶段可打断；AttackDraw 阶段不可打断
        if (!isAttackAnimating || isAttackDrawPhase) return false;

        // 清理 DOTween 残留（修复形变不恢复问题）
        transform.DOKill(false);

        if (attackSequence != null && attackSequence.IsActive())
        {
            attackSequence.Kill();
            attackSequence = null;
        }
        UpdateWorldPosition();
        transform.localScale = originalScale;
        isAttackAnimating = false;
        isAttackDrawPhase = false;

        // 重置攻击冷却，回到 AttackSpeed 等待状态（非眩晕）
        float totalInterval = (1f / attackSpeed);
        attackTimer = totalInterval * 0.4f;
        if (attackTimer < 0.1f) attackTimer = 0.1f;

        Debug.Log($"[Enemy] 招架打断攻击成功: {DebugTag}, col={columnIndex}, 返回冷却 {attackTimer:F2}s");
        return true;
    }

    /// <summary>
    /// 进入 QTE 攻击状态（由 QTEController 调用）
    /// 中断当前攻击，切换到 QTEAttacking 状态
    /// </summary>
    public void EnterQTEAttack()
    {
        if (state == EnemyState.Dead) return;

        // 中断当前攻击动画
        if (attackSequence != null && attackSequence.IsActive())
        {
            attackSequence.Kill();
            attackSequence = null;
        }
        transform.DOKill(false);
        UpdateWorldPosition();
        transform.localScale = originalScale;
        isAttackAnimating = false;
        isAttackDrawPhase = false;
        isMovingToNextRow = false;

        state = EnemyState.QTEAttacking;
        Debug.Log($"[Enemy] 进入QTE攻击: {DebugTag}");
    }

    /// <summary>
    /// 退出 QTE 攻击状态（由 QTEController 调用）
    /// </summary>
    public void ExitQTEAttack()
    {
        if (state != EnemyState.QTEAttacking) return;
        state = EnemyState.Attacking;
        // 重置攻击冷却
        float totalInterval = (1f / attackSpeed);
        attackTimer = totalInterval * 0.4f;
        if (attackTimer < 0.1f) attackTimer = 0.1f;
        isAttackAnimating = false;
        isAttackDrawPhase = false;
        Debug.Log($"[Enemy] 退出QTE攻击: {DebugTag}");
    }

    /// <summary>
    /// 获取 QTEController（懒加载缓存）
    /// </summary>
    public QTEController GetQTEController()
    {
        if (_qteController == null)
            _qteController = GetComponent<QTEController>();
        return _qteController;
    }

    #endregion

    #region 状态更新

    private void UpdateStun()
    {
        stunTimer -= Time.deltaTime;
        if (stunTimer <= 0f)
        {
            // STUN 结束后重置 Poise，开始新一周期的削韧循环
            currentPoise = maxPoise;
            _poiseRecoveryEndTime = 0f;
            OnPoiseChanged?.Invoke(this, currentPoise, maxPoise);

            // BUG FIX: 必须先从 Stunned 状态退出，否则 TryStartRushMove 检测到
            // state==Stunned 会直接返回 false，而 StartMoving 也可能因 PerRow 前排检查
            // 等原因提前返回不改变状态，导致敌人永久卡在 Stunned 状态
            state = EnemyState.Idle;

            // Boss 就位后锁定位置，不参与补齐前移
            if (isBoss && (bossState == BossState.InCombat || _bossEngageTimer > 0f))
            {
                StartAttacking();
            }
            else if (pendingRushMove)
            {
                // 眩晕前被标记了补齐移动，恢复后继续 Rush 链
                TryStartRushMove();
            }
            else
            {
                StartMoving();
            }
        }
    }

    /// <summary>
    /// 更新击飞状态：恒定重力加速度驱动
    /// 初始击飞给予上升初速度 sqrt(2*g*H)，空中受击叠加反弹速度
    /// </summary>
    private void UpdateLaunch()
    {
        launchTimer -= Time.deltaTime;

        // 恒定重力（计时器到期后加速下落，避免悬浮）
        float gravity = launchTimer > 0f ? launchGravity : launchGravity * 3f;
        launchVelocityY -= gravity * Time.deltaTime;

        // 挑飞期间眩晕计时继续跑，避免落地后恢复已过期的眩晕
        if (_remainingStunOnLaunch > 0f)
            _remainingStunOnLaunch = Mathf.Max(0f, _remainingStunOnLaunch - Time.deltaTime);

        // 计算新Y偏移
        float currentY = transform.localPosition.y - launchStartLocalPos.y;
        float newY = currentY + launchVelocityY * Time.deltaTime;

        // 着陆条件：自然落地（低于地面且正在下落）
        bool landed = newY <= 0f && launchVelocityY <= 0f;

        if (landed)
        {
            // 浮空结束，回到地面
            transform.localPosition = new Vector3(
                transform.localPosition.x,
                launchStartLocalPos.y,
                transform.localPosition.z);

            // 先退出击飞状态，再根据情况决定后续行为
            state = EnemyState.Idle;

            // BUG FIX: 挑飞打断了眩晕，落地后恢复剩余的眩晕时间
            // 避免 BOSS 在眩晕未结束时落地立即攻击
            if (_remainingStunOnLaunch > 0f)
            {
                float remaining = _remainingStunOnLaunch;
                _remainingStunOnLaunch = 0f;
                Stun(remaining);
                return;
            }

            // 逐排补齐：落地后触发 RowBasedFillUp（Launched→Idle 可能改变清空行状态）
            if (StageController.Instance?.GetFillUpRule() == FillUpRule.PerRow)
            {
                var cm = EnemyManager.Instance?.columnManager;
                if (cm != null) cm.RowBasedFillUp();
            }

            // Boss 就位后（战斗中或缓冲中）锁定位置，不参与补齐前移
            if (isBoss && (bossState == BossState.InCombat || _bossEngageTimer > 0f))
            {
                Debug.Log($"[Enemy] Boss落地锁定位置: {DebugTag}, col={columnIndex}, row={rowIndex}");
                StartAttacking();
                return;
            }

            // 如果 targetRow 被 Column 设置（前方死敌产生空位），则补齐前移
            if (targetRow >= 0 && rowIndex > targetRow)
            {
                pendingRushMove = true;
                var col = EnemyManager.Instance?.columnManager?.GetColumn(columnIndex);
                if (col != null)
                    col.StartRushFromLaunched(this);
                else
                    TryStartRushMove();
            }
            else
            {
                int atkRange = (int)Mathf.Max(1, attackRange);
                if (rowIndex < atkRange)
                {
                    StartAttacking();
                }
                else
                {
                    int frontRow = rowIndex - 1;
                    bool frontOccupied = false;
                    var col = EnemyManager.Instance?.columnManager?.GetColumn(columnIndex);
                    if (col != null && frontRow >= 0 && frontRow < col.enemies.Count)
                    {
                        Enemy front = col.enemies[frontRow];
                        if (front != null && front != this && front.state != EnemyState.Dead)
                            frontOccupied = true;
                    }
                    if (frontOccupied)
                    {
                        Debug.Log($"[Enemy] 落地后前方被占据，等待补齐: {DebugTag}, col={columnIndex}, row={rowIndex}, frontRow={frontRow}");
                    }
                    else
                    {
                        StartMoving();
                    }
                }
            }
            return;
        }

        // 应用位置
        transform.localPosition = new Vector3(
            transform.localPosition.x,
            launchStartLocalPos.y + newY,
            transform.localPosition.z);
    }

    /// <summary>
    /// 更新移动状态
    /// 常规移动：基于 moveSpeed（秒/排），从当前排(rowIndex)移动到前一排(rowIndex-1)
    /// 移动完成后更新 rowIndex，检查是否到达攻击距离
    ///
    /// 链式补齐：
    ///   - 补齐移动（isRush=true）在移动完全完成（moveProgress >= 1.0）时，
    ///     触发 OnRushMoveComplete 事件，通知列管理器启动下一个需要补齐的敌人。
    ///     必须等待前一敌人完全补齐完毕，后一敌人才能开始补齐。
    ///   - 每个敌人每次补齐只前进一排，不再连续多排补齐。
    ///     后续补齐通过延迟计时器（rushMoveDelayTimer）在移动完成后触发。
    ///     这样各敌人最终停在各自正确的排位置（rowIndex = listPosition），不会发生重合。
    ///
    /// 补齐延迟：
    ///   - 补齐移动完成后，如果还需要继续前进（rowIndex >= attackRange），
    ///     启动延迟计时器，等待 rushMoveDelay 秒后再开始下一次补齐移动。
    ///   - 这样可以将单次补齐移动速度（moveSpeed）加快，
    ///     而整体补齐节奏通过 rushMoveDelay 来调节，达到"快移动+停顿"的效果。
    /// </summary>
    private void UpdateMovement()
    {
        if (!isMovingToNextRow) return;

        // 常规移动：基于 moveSpeed（秒/排）
        float speed = moveSpeed;
        moveProgress += Time.deltaTime / speed;

        if (moveProgress >= 1f)
        {
            bool wasRush = isRushMove;
            Debug.Log($"[Enemy] 移动完成: {DebugTag}, col={columnIndex}, oldRow={rowIndex}, newRow={rowIndex - 1}, isRush={wasRush}");
            moveProgress = 0f;
            isMovingToNextRow = false;
            isRushMove = false;
            rushMoveChainTriggered = false; // 重置链式触发标记

            // 移动完成：rowIndex 前进一排
            rowIndex--;

            // BUG FIX: 防止 rowIndex 变为负数
            if (rowIndex < 0) rowIndex = 0;

            // Boss 分阶段推进：到达第3排(rowIndex=2)，暂停等待前两排清空
            if (isBoss && rowIndex == 2 && bossState == BossState.None)
            {
                BossPause();
                OnColumnsModifiedForBoss(); // 立即检查前两排是否已空，防止漏检
                if (!rushMoveChainTriggered)
                {
                    rushMoveChainTriggered = true;
                    OnRushMoveComplete?.Invoke(this);
                }
                return;
            }

            // 使用 attackRange 决定攻击距离
            int atkRange = (int)Mathf.Max(1, attackRange);
            bool reachedAttackRange = rowIndex < atkRange;

            if (wasRush)
            {
                // BUG FIX: 补齐移动优先级高于攻击。即使当前行已在攻击范围内，
                // 只要尚未到达目标位置（targetRow），就必须继续向前补齐。
                // 先检查是否需要继续补齐，然后再判断是否开始攻击。
                if (targetRow >= 0 && rowIndex > targetRow)
                {
                    // 尚未到达目标位置（列表位置），继续补齐
                    float delay = 0f;
                    if (StageController.Instance != null)
                    {
                        delay = StageController.Instance.GetRushMoveDelay();
                    }
                    if (delay > 0f)
                    {
                        Debug.Log($"[Enemy] 补齐移动完成（等待延迟继续补齐）: {DebugTag}, col={columnIndex}, row={rowIndex}, targetRow={targetRow}, delay={delay:F2}s");
                        state = EnemyState.Idle;
                        pendingRushMove = true;
                        rushMoveDelayTimer = delay;
                    }
                    else
                    {
                        // 无延迟，立即继续补齐
                        Debug.Log($"[Enemy] 补齐移动完成（立即继续补齐）: {DebugTag}, col={columnIndex}, row={rowIndex}, targetRow={targetRow}");
                        state = EnemyState.Idle;
                        pendingRushMove = true;
                        TryStartRushMove();
                    }
                }
                else if (reachedAttackRange)
                {
                    // 已到达目标位置且在攻击范围内，开始攻击
                    Debug.Log($"[Enemy] 补齐移动完成（到达目标位置+攻击范围）: {DebugTag}, col={columnIndex}, row={rowIndex}");
                    pendingRushMove = false;
                    targetRow = -1;
                    StartAttacking();
                }
                else
                {
                    // 已到达目标位置但不在攻击范围内，停止补齐
                    Debug.Log($"[Enemy] 补齐移动完成（到达目标位置，停止补齐）: {DebugTag}, col={columnIndex}, row={rowIndex}, targetRow={targetRow}");
                    state = EnemyState.Idle;
                    pendingRushMove = false;
                    targetRow = -1;
                }

                // BUG FIX: 链式触发移至移动完全完成后，而非移动中期。
                // 必须等待前一敌人完全补齐完毕（moveProgress >= 1.0），
                // 后一敌人才能开始补齐。这样符合"逐个向前补齐"的行为。
                // 使用 rushMoveChainTriggered 避免在同一个移动过程中重复触发。
                if (!rushMoveChainTriggered)
                {
                    rushMoveChainTriggered = true;
                    Debug.Log($"[Enemy] 补齐移动完全完成（触发链式）: {DebugTag}, col={columnIndex}, row={rowIndex}");
                    OnRushMoveComplete?.Invoke(this);

                    // Boss 分阶段推进：到达第2排(rowIndex=1)，启动缓冲计时器
                    if (isBoss && rowIndex <= 1 && (bossState == BossState.Approaching || bossState == BossState.None))
                    {
                        // 取消列监听，防止 OnColumnsModifiedForBoss 误触发 BossResume
                        var cm = EnemyManager.Instance?.columnManager;
                        if (cm != null && _onColumnsModifiedHandler != null)
                        {
                            cm.OnColumnsModified -= _onColumnsModifiedHandler;
                            _onColumnsModifiedHandler = null;
                        }
                        // 1秒无敌缓冲，为出场动画预留时间
                        _bossEngageTimer = 1f;
                        Debug.Log($"[Enemy] Boss到达应战排，启动缓冲计时器: {DebugTag}, col={columnIndex}, row={rowIndex}");
                    }
                }
            }
            else
            {
                // 自然移动完成后，通知ColumnManager更新列内排序
                if (reachedAttackRange)
                {
                    StartAttacking();
                }
                EnemyManager.Instance?.OnEnemyMovedForward(this);
                Debug.Log($"[Enemy] 自然移动完成，触发 UpdateEnemyRow: {DebugTag}, col={columnIndex}");
            }
        }

        UpdateWorldPosition();
    }

    /// <summary>
    /// 更新攻击状态（三阶段攻击循环）
    ///   阶段1：攻击冷却（attackTimer > 0）→ 等待冷却
    ///   阶段2：AttackSpawn 前摇（DOTween，可被招架打断）→ 向前翻面，完成时造成伤害
    ///   阶段3：AttackDraw 收招（DOTween，不可打断）→ 返回原位
    ///
    /// 攻击优先级规则：
    ///   1. 冷却期间如果 pendingRushMove == true → 先执行补齐再攻击
    ///   2. AttackSpawn 动画期间 → 可被招架打断（CancelAttack）
    ///   3. AttackDraw 动画期间 → 不可中断，完成当前攻击后检查 pendingRushMove
    /// </summary>
    private void UpdateAttack()
    {
        if (isAttackAnimating)
        {
            // 阶段2：DOTween 攻击动画播放中
            // 动画完成后通过 OnComplete 回调处理（PerformAttack + 设置冷却 + TryStartRushMove）
            return;
        }
        else
        {
            // 阶段1：攻击冷却
            // BUG FIX: 冷却期间如果标记了需要补齐，先执行补齐再攻击
            if (pendingRushMove)
            {
                TryStartRushMove();
                return; // 如果成功开始移动，直接返回，不再处理冷却
            }

            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                // 冷却结束，使用 DOTween 播放攻击动画
                PlayAttackAnimationTween();
            }
        }
    }

    private void PerformAttack()
    {
        // 通知玩家受到伤害
        // 由EnemyManager转发给PlayerState
        EnemyManager.Instance?.OnEnemyAttackPlayer(this);
    }

    /// <summary>
    /// 使用 DOTween 播放攻击动效（三阶段攻击循环）
    ///   AttackSpawn（前摇翻面）：向前移动 + 镜像翻转，完成时造成伤害，可被招架打断
    ///   AttackDraw（收招返回）：后退到原位 + 翻转回正，不可被招架打断
    ///   冷却阶段（AttackSpeed）：等待 attackTimer 倒计时，冷却结束后再次进入 AttackSpawn
    /// </summary>
    private void PlayAttackAnimationTween()
    {
        isAttackAnimating = true;
        isAttackDrawPhase = false;

        Vector3 startPos = transform.localPosition;
        Vector3 startScale = transform.localScale;

        float totalInterval = (1f / attackSpeed);
        float spawnDuration = attackSpawnDuration;
        float drawDuration = attackDrawDuration;
        float forwardDistance = 0.5f;

        attackSequence = DOTween.Sequence();
        attackSequence.SetTarget(transform);
        attackSequence.SetId("attackAnim");

        // AttackSpawn：向前移动 + 左右镜像翻转（可被招架打断）
        attackSequence.Append(transform.DOLocalMoveZ(startPos.z - forwardDistance, spawnDuration).SetEase(Ease.OutQuad));
        if (useAttackFlip)
            attackSequence.Join(transform.DOScaleX(-startScale.x, spawnDuration).SetEase(Ease.OutQuad));

        // AttackSpawn 完成 → 造成伤害，进入 AttackDraw 收招阶段
        attackSequence.AppendCallback(() =>
        {
            PerformAttack();
            isAttackDrawPhase = true; // 进入收招阶段，此后不可被招架打断
        });

        // AttackDraw：后退到原位 + 翻转回正（不可被招架打断）
        attackSequence.Append(transform.DOLocalMoveZ(startPos.z, drawDuration).SetEase(Ease.InQuad));
        if (useAttackFlip)
            attackSequence.Join(transform.DOScaleX(startScale.x, drawDuration).SetEase(Ease.InQuad));

        // 收招完成 → 进入冷却阶段
        attackSequence.OnComplete(() =>
        {
            attackSequence = null;
            isAttackAnimating = false;
            isAttackDrawPhase = false;

            float cooldown = totalInterval * 0.4f;
            if (cooldown < 0.1f) cooldown = 0.1f;
            attackTimer = cooldown;

            // 通知 QTEController 攻击完成（若 QTE 正在等待攻击结束）
            if (isBoss)
            {
                var qte = GetQTEController();
                if (qte != null) qte.OnEnemyAttackComplete();
            }

            TryStartRushMove();
        });
    }

    #endregion

    #region 伤害系统

    /// <summary>
    /// 受到伤害
    /// </summary>
    public void TakeDamage(float damage, DamageType damageType = DamageType.Stab)
    {
        if (state == EnemyState.Dead) return;
        if (isBoss && bossState != BossState.InCombat) return;

        // 击飞状态下受到伤害倍率
        if (state == EnemyState.Launched)
            damage *= launchedDamageTakenMultiplier;

        // 应用弱点倍率
        float multiplier = GetDamageMultiplier(damageType);
        float finalDamage = damage * multiplier;

        Debug.Log($"[Enemy] TakeDamage: {DebugTag}, col={columnIndex}, raw={damage:F1}, mult={multiplier:F2}, final={finalDamage:F1}, hp={currentHealth:F1}→{currentHealth - finalDamage:F1}");

        currentHealth -= finalDamage;
        OnDamageTaken?.Invoke(this);
        OnHealthChanged?.Invoke(this, currentHealth, maxHealth);

        // 受伤跳字：在敌人右侧显示红色带黑描边的伤害数字
        if (DamageNumberManager.Instance != null)
        {
            DamageNumberManager.Instance.Spawn(transform.position, finalDamage);
        }

        // 血条显示（非BOSS、未死亡时显示）
        if (!isBoss && currentHealth > 0f)
        {
            if (cachedHealthBar == null)
                cachedHealthBar = GetComponent<EnemyHealthBar>();
            if (cachedHealthBar == null)
                cachedHealthBar = gameObject.AddComponent<EnemyHealthBar>();
            cachedHealthBar.Show(currentHealth / maxHealth);
        }

        // BUG FIX: 同步应用闪白（立即设置颜色，不依赖 Update 循环）
        // 即使敌人秒杀死亡，闪白效果也在死亡前被渲染
        // 否则 Die() 设置 state = Dead 后，Update() 提前返回，UpdateAlpha() 不被调用
        ApplyHitFlashImmediate();

        // 触发受伤精灵闪烁（仅 Idle/Moving 状态，持续 0.3 秒）
        if (cachedSpriteController == null)
            cachedSpriteController = GetComponent<EnemySpriteController>();
        if (cachedSpriteController != null)
            cachedSpriteController.TriggerHitFlash();

        // 触发受伤闪白效果（非致命伤通过 Update 循环过渡恢复）
        hitFlashTimer = HIT_FLASH_DURATION;
        Debug.Log($"[Enemy] 触发闪白: {DebugTag}, duration={HIT_FLASH_DURATION}");

        // DOTween: 受击大小抖动效果（与闪白同步触发）
        // BUG FIX: 使用 per-instance ID（基于 GetInstanceID），避免全局 DOTween.Kill("punchScale")
        // 误杀其他敌人的 punch tween，导致其他敌人缩放停在中间值无法恢复。
        string punchId = $"punch_{GetInstanceID()}";
        DOTween.Kill(punchId);
        transform.localScale = originalScale;
        transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0.2f), 0.15f, 8, 0.5f)
            .SetTarget(transform)
            .SetId(punchId);

        // 击飞状态下被攻击延长浮空时间
        if (state == EnemyState.Launched && launchedHitExtendDuration > 0f)
        {
            ExtendLaunch(launchedHitExtendDuration);
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    /// <summary>
    /// 同步应用闪白效果（在 TakeDamage 中调用）
    /// 直接通过材质实例设置 color = white，确保即使敌人立即死亡，
    /// 闪白也在协程等待期间可见（协程中 state=Dead 但 UpdateAlpha 不执行，
    /// 材质颜色在协程等待期间保持白色）。
    /// </summary>
    private void ApplyHitFlashImmediate()
    {
        if (flashMaterials == null) return;
        foreach (var mat in flashMaterials)
        {
            if (mat != null) mat.color = Color.white;
        }
    }

    /// <summary>
    /// 创建材质实例（用于闪白效果）
    /// 通过 renderer.material 创建实例，确保修改 color 不会影响其他敌人。
    /// 在 Initialize() 中调用，在 ResetEnemy() 中销毁。
    /// </summary>
    private void CreateFlashMaterials()
    {
        // 先销毁旧的材质实例
        DestroyFlashMaterials();

        if (renderers == null || renderers.Length == 0) return;

        flashMaterials = new Material[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            // renderer.material 会创建材质实例（自动实例化 sharedMaterial）
            flashMaterials[i] = renderers[i].material;
            // 初始颜色设为白色（精灵原始颜色）
            flashMaterials[i].color = Color.white;
        }
    }

    /// <summary>
    /// 销毁材质实例
    /// </summary>
    private void DestroyFlashMaterials()
    {
        if (flashMaterials != null)
        {
            foreach (var mat in flashMaterials)
            {
                if (mat != null)
                {
                    Object.Destroy(mat);
                }
            }
            flashMaterials = null;
        }
    }

    /// <summary>
    /// 受到架势（Poise）伤害
    /// Poise 不会自动回复；归零时触发 STUN，STUN 结束后重置 Poise 到满值
    /// </summary>
    public bool TakePoiseDamage(float poiseDamage)
    {
        if (state == EnemyState.Dead) return false;
        if (state == EnemyState.Stunned) return false; // 眩晕期间免疫架势伤害
        if (isBoss && bossState != BossState.InCombat) return false;

        currentPoise -= poiseDamage;
        OnPoiseChanged?.Invoke(this, currentPoise, maxPoise);

        if (currentPoise <= 0f)
        {
            currentPoise = 0f;
            Stun(stunDuration);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 招架后检查血量百分比阈值，若低于阈值则眩晕
    /// 取 healthPercent 最小（最严格）的匹配阈值，避免重复眩晕
    /// </summary>
    public void CheckParryStunThresholds()
    {
        if (state == EnemyState.Dead || state == EnemyState.Stunned || state == EnemyState.Launched) return;
        // 仅 Boss 敌人才会因血量百分比触发招架眩晕
        if (!isBoss) return;
        if (parryStunThresholds == null || parryStunThresholds.Length == 0) return;

        float healthPercent = currentHealth / maxHealth;

        float bestHealthPercent = float.MaxValue;
        float bestStunDuration = 0f;
        foreach (var t in parryStunThresholds)
        {
            if (healthPercent < t.healthPercent && t.healthPercent < bestHealthPercent)
            {
                bestHealthPercent = t.healthPercent;
                bestStunDuration = t.stunDuration;
            }
        }

        if (bestStunDuration > 0f)
        {
            Stun(bestStunDuration);
        }
    }

    /// <summary>
    /// 根据伤害类型获取倍率
    /// </summary>
    private float GetDamageMultiplier(DamageType damageType)
    {
        switch (damageType)
        {
            case DamageType.Stab:   return stabDamageMultiplier;
            case DamageType.Slash:  return slashDamageMultiplier;
            case DamageType.Pierce: return pierceDamageMultiplier;
            case DamageType.Sweep:  return sweepDamageMultiplier;
            case DamageType.Launch: return launchDamageMultiplier;
            case DamageType.Poise:  return poiseDamageMultiplier;
            default: return 1f;
        }
    }

    /// <summary>
    /// 死亡
    /// 流程：
    ///   1. state = Dead（Update 提前返回，不再处理攻击/移动逻辑）
    ///   2. 取消正在执行的攻击 DOTween 动画
    ///   3. 立即触发 OnDeath 事件 → EnemyManager 计入击杀、判断通关
    ///   4. 启动死亡动效协程 DeathBounceAndFall()（弹起+旋转+掉落，纯视觉）
    ///   5. 协程结束后回收到对象池
    /// </summary>
    public void Die()
    {
        if (state == EnemyState.Dead) return;
        bool wasLaunched = (state == EnemyState.Launched);
        state = EnemyState.Dead;

        // BUG FIX: 取消正在执行的攻击 DOTween 动画
        // 若敌人在攻击动画中被秒杀，立即中断攻击动作（前移+翻转），直接进入死亡状态
        // Kill() 后立即重置位置和缩放：Kill 会留下中断时的中间值（如翻转-0.2），
        // 若不重置，死亡动效期间敌人位置/缩放会是错误的
        if (attackSequence != null && attackSequence.IsActive())
        {
            attackSequence.Kill();
            attackSequence = null;
            UpdateWorldPosition();
            transform.localScale = originalScale;
        }
        isAttackAnimating = false;
        isAttackDrawPhase = false;
        isMovingToNextRow = false;

        // 立即隐藏血条（非BOSS），避免血条随死亡动画飘落
        if (!isBoss && cachedHealthBar != null)
        {
            cachedHealthBar.Hide();
        }

        // 立即触发死亡事件：计入击杀数、判断通关（不等死亡动画播完）
        OnDeath?.Invoke(this);

        // 启动死亡动效协程（弹起 + 旋转 + 重力掉落，纯视觉表现）
        if (wasLaunched)
            StartCoroutine(LaunchDeathEffect());
        else
            StartCoroutine(DeathBounceAndFall());
    }

    /// <summary>
    /// 死亡动效协程 — 弹起 + 随机旋转 + 重力掉落
    /// 使用 DOTween 实现：
    ///   1. 立即闪白（ApplyHitFlashImmediate）
    ///   2. 弹起（Y 轴向上 OutQuad 缓动）
    ///   3. 随机旋转（在 X 和 Z 轴上随机角度，贯穿整个动画）
    ///   4. 受重力掉落离开屏幕（Y 轴向下 InQuad 缓动）
    ///   5. 恢复缩放和旋转（供对象池复用），回收到对象池
    /// </summary>
    private System.Collections.IEnumerator DeathBounceAndFall()
    {
        // 立即将材质颜色设为白色（闪白）
        ApplyHitFlashImmediate();

        // 保存起始位置
        Vector3 startPos = transform.localPosition;

        // 构建 DOTween 序列
        Sequence deathSeq = DOTween.Sequence();
        deathSeq.SetTarget(transform);
        deathSeq.SetId("deathAnim");

        float jumpHeight = Random.Range(1.5f, 3.0f);   // 弹起高度
        float fallDistance = 20f;                        // 掉落距离（确保离开屏幕）
        float jumpDuration = 0.3f;                       // 弹起时长
        float fallDuration = 0.8f;                       // 掉落时长

        // 阶段1：弹起（OutQuad 缓动，模拟起跳的加速度）
        deathSeq.Append(transform.DOLocalMoveY(startPos.y + jumpHeight, jumpDuration).SetEase(Ease.OutQuad));

        // 阶段2：受重力掉落离开屏幕（InQuad 缓动，模拟自由落体加速）
        deathSeq.Append(transform.DOLocalMoveY(startPos.y - fallDistance, fallDuration).SetEase(Ease.InQuad));

        // 随机旋转（贯穿整个动画：弹起阶段 + 掉落阶段）
        // 在 X 轴和 Z 轴上随机旋转，模拟被击飞后的翻滚
        Vector3 randomRotation = new Vector3(
            Random.Range(-60f, 60f),   // X 轴轻微翻转
            0f,                          // Y 轴不旋转（2D 精灵）
            Random.Range(-360f, 360f)   // Z 轴大角度旋转
        );
        float totalAnimDuration = jumpDuration + fallDuration;
        transform.DORotate(randomRotation, totalAnimDuration, RotateMode.LocalAxisAdd)
            .SetEase(Ease.OutQuad)
            .SetTarget(transform)
            .SetId("deathAnim");

        // 等待序列完成
        yield return deathSeq.WaitForCompletion();

        // 恢复缩放和旋转（供对象池复用）
        transform.localScale = originalScale;
        transform.localRotation = Quaternion.identity;

        // 死亡动效结束后回收到对象池
        EnemyPool.Instance?.ReturnEnemy(this);
    }

    /// <summary>
    /// 击飞死亡动效 — 从当前击飞位置旋转坠落
    /// 不弹起，直接从当前位置（可能在空中）旋转并掉出屏幕
    /// </summary>
    private System.Collections.IEnumerator LaunchDeathEffect()
    {
        ApplyHitFlashImmediate();

        Vector3 startPos = transform.localPosition;

        Sequence deathSeq = DOTween.Sequence();
        deathSeq.SetTarget(transform);
        deathSeq.SetId("deathAnim");

        float fallDistance = 20f;
        float fallDuration = 0.8f;

        // 从当前位置（可能在击飞空中）直接坠落
        deathSeq.Append(transform.DOLocalMoveY(startPos.y - fallDistance, fallDuration).SetEase(Ease.InQuad));

        // 随机旋转（比普通死亡更大角度，更有击飞感）
        Vector3 randomRotation = new Vector3(
            Random.Range(-90f, 90f),
            0f,
            Random.Range(-540f, -360f)
        );
        deathSeq.Join(transform.DORotate(randomRotation, fallDuration, RotateMode.LocalAxisAdd).SetEase(Ease.OutQuad));

        yield return deathSeq.WaitForCompletion();

        transform.localScale = originalScale;
        transform.localRotation = Quaternion.identity;

        EnemyPool.Instance?.ReturnEnemy(this);
    }

    #endregion

    #region 位置与透明度

    /// <summary>
    /// 重置移动状态（列内补齐时调用）
    /// 重置 state=Idle、isMovingToNextRow=false、moveProgress=0，
    /// 以便 StartMoving() 能通过 state==Moving 保护检查，重新开始移动。
    ///
    /// BUG FIX: 不再重置 pendingRushMove 标记，
    /// 由 Column.RemoveEnemy() 单独控制 pendingRushMove 的设置。
    /// </summary>
    public void ResetMovementState()
    {
        // 若敌人正在播放攻击动画，先清理 DOTween 序列并重置位置/缩放
        // 否则后续 StartMoving() 中的 DOTween.Kill(transform, false) 会直接中断攻击 tween，
        // 留下中间值（如 ScaleX=-0.2）导致视觉错误，且攻击动画与补齐移动并发
        if (attackSequence != null && attackSequence.IsActive())
        {
            attackSequence.Kill();
            attackSequence = null;
            UpdateWorldPosition();
            transform.localScale = originalScale;
        }
        isAttackAnimating = false;
        isAttackDrawPhase = false;

        state = EnemyState.Idle;
        isMovingToNextRow = false;
        moveProgress = 0f;
    }

    /// <summary>
    /// 击飞中的敌人静默补齐：直接更新 rowIndex 和 X/Z 位置，保留当前 Y（击飞高度）。
    /// 不播放 DOTween 弹跳动画，不触发补齐链。
    /// 由 Column 在 RemoveEnemy / CompactByClearRows / TriggerFillForward 中调用。
    /// </summary>
    public void SilentFillToTargetRow()
    {
        if (targetRow < 0 || rowIndex <= targetRow) return;

        int oldRow = rowIndex;
        rowIndex = targetRow;
        targetRow = -1;

        float xPos;
        float rowSpacing = 2.5f;
        float offsetZ = 0f;
        if (StageController.Instance != null)
        {
            xPos = StageController.Instance.GetFormationOffset(columnIndex, rowIndex);
            rowSpacing = StageController.Instance.GetRowSpacing();
            offsetZ = StageController.Instance.GetFormationOffsetZ();
        }
        else
        {
            xPos = (columnIndex - 2) * 2.0f;
        }

        float zPos = GetRowZ(rowIndex, rowSpacing, offsetZ);

        // 保留当前 Y：击飞高度由 UpdateLaunch() 管理，不重置
        float currentY = transform.localPosition.y;
        transform.localPosition = new Vector3(xPos, currentY, zPos);

        Debug.Log($"[Enemy] 击飞静默补齐: {DebugTag}, col={columnIndex}, row={oldRow}→{rowIndex}, pos=({xPos:F2},{currentY:F2},{zPos:F2})");
    }

    /// <summary>
    /// 尝试开始补齐移动（链式触发）
    /// 根据当前状态决定是否立即开始移动：
    ///   - Idle：直接开始移动（或攻击，若已到最前排）
    ///   - Attacking（冷却阶段）：中断冷却，优先补齐
    ///   - Attacking（动画阶段）：等待动画完成，由 UpdateAttack() 调用
    ///   - Stunned/Launched：等待恢复
    ///
    /// 返回值：true 表示已处理（开始移动或立即触发链式完成），false 表示等待下次尝试
    /// </summary>
    public bool TryStartRushMove()
    {
        if (!pendingRushMove || state == EnemyState.Dead)
            return false;

        switch (state)
        {
            case EnemyState.Idle:
                // BOSS 补齐规则：前方一整排（所有列）必须清空才前进，而非仅看本列
                if (isBoss && bossState == BossState.None && rowIndex > 2)
                {
                    if (!IsRowClearForBoss(rowIndex - 1))
                    {
                        // 前方排仍有存活敌人，订阅列修改事件等待重试
                        var cm = EnemyManager.Instance?.columnManager;
                        if (cm != null && _onColumnsModifiedHandler == null)
                        {
                            _onColumnsModifiedHandler = OnColumnsModifiedForBoss;
                            cm.OnColumnsModified += _onColumnsModifiedHandler;
                            Debug.Log($"[Enemy] Boss等待前方排清空(row={rowIndex - 1}): {DebugTag}, col={columnIndex}");
                        }
                        return false; // 不清除 pendingRushMove，等待重试
                    }
                    // 前方排已清空，取消等待订阅（如有）
                    if (_onColumnsModifiedHandler != null)
                    {
                        var cm2 = EnemyManager.Instance?.columnManager;
                        if (cm2 != null)
                            cm2.OnColumnsModified -= _onColumnsModifiedHandler;
                        _onColumnsModifiedHandler = null;
                    }
                }
                pendingRushMove = false;
                StartMoving(true);
                // 如果 state 未变为 Moving（例如 rowIndex=0 直接进入攻击），
                // 立即触发链式完成，让下一个敌人开始补齐
                if (state != EnemyState.Moving)
                {
                    OnRushMoveComplete?.Invoke(this);
                }
                return true;

            case EnemyState.Attacking:
                if (!isAttackAnimating)
                {
                    // 冷却阶段：中断攻击，但等待 rushMoveDelay 后再开始补齐
                    // 不立即 StartMoving，避免跳过补齐延迟计时器
                    ResetMovementState();
                    pendingRushMove = true;
                    float delay = StageController.Instance?.GetRushMoveDelay() ?? 0f;
                    if (delay > 0f)
                        rushMoveDelayTimer = delay;
                    else
                        StartMoving(true);
                    return true;
                }
                // 动画阶段：等待动画完成，由 UpdateAttack() 调用 TryStartRushMove
                return false;

            case EnemyState.Stunned:
            case EnemyState.Launched:
                // 普通敌人没有眩晕设计，不应被眩晕/击飞阻塞补齐移动
                if (!isBoss)
                {
                    state = EnemyState.Idle;
                    pendingRushMove = false;
                    StartMoving(true);
                    if (state != EnemyState.Moving)
                    {
                        OnRushMoveComplete?.Invoke(this);
                    }
                    return true;
                }
                // Boss 等待恢复
                return false;

            default:
                return false;
        }
    }

    /// <summary>
    /// 设置列和排索引
    /// </summary>
    public void SetColumnPosition(int col, int row)
    {
        columnIndex = col;
        rowIndex = row;
        UpdateWorldPosition();
    }

    /// <summary>
    /// 仅设置排索引（列内前移时调用）
    /// </summary>
    public void SetRowIndex(int row)
    {
        rowIndex = row;
        UpdateWorldPosition();
    }

    /// <summary>
    /// 根据排索引计算Z轴位置
    /// 在Unity中，-Z轴是前进方向（朝向玩家），+Z轴是后退方向（远离玩家）。
    /// rowIndex=0在最前排（最靠近玩家），rowIndex越大越远离玩家。
    ///
    /// 公式：zPos = (maxVisibleRows - 1 - rowIndex) * (-rowSpacing) + offsetZ
    /// rowIndex=0 → Z = (5-1-0) * (-2.5) = -10（最靠近玩家）
    /// rowIndex=4 → Z = (5-1-4) * (-2.5) = 0（最远离玩家）
    ///
    /// 前进方向：rowIndex从4→3→2→1→0，Z值从0→-2.5→-5.0→-7.5→-10
    /// 即向-Z方向移动。-Z方向是前进方向（朝向玩家），所以这是前进。
    /// </summary>
    private float GetRowZ(int row, float spacing, float offset)
    {
        int maxRow = 4; // 默认值
        if (StageController.Instance != null)
        {
            maxRow = StageController.Instance.GetMaxVisibleRows() - 1;
        }
        return (maxRow - row) * (-spacing) + offset;
    }

    /// <summary>
    /// 更新世界坐标
    /// 列：X轴使用梯形/扇形阵型偏移，排：Z轴偏移
    /// 移动过程中：从当前排(rowIndex)位置平滑移动到前一排(rowIndex-1)位置
    /// 非移动中：固定在当前排(rowIndex)位置
    ///
    /// BUG FIX: 移动过程中使用 rowIndex（当前排位置）计算阵型偏移，
    /// 因为 rowIndex 在移动过程中尚未更新（移动完成后才 rowIndex--）。
    /// 这样 X 轴和 Z 轴同步，避免"梯形向内聚拢"问题。
    /// </summary>
    private void UpdateWorldPosition()
    {
        float xPos;
        float rowSpacing = 2.5f;
        if (StageController.Instance != null)
        {
            // BUG FIX: 移动过程中使用 rowIndex（旧排位置）计算 X 轴偏移
            // 因为 rowIndex 在移动过程中尚未更新（移动完成后才 rowIndex--）
            // 这样 X 轴和 Z 轴同步，避免"梯形向内聚拢"问题
            xPos = StageController.Instance.GetFormationOffset(columnIndex, rowIndex);
            rowSpacing = StageController.Instance.GetRowSpacing();
        }
        else
        {
            // 回退到原始直线排列
            xPos = (columnIndex - 2) * 2.0f;
        }

        // 获取阵型整体Z轴偏移（用于调整敌人生成位置远离/靠近摄像机）
        float offsetZ = 0f;
        if (StageController.Instance != null)
        {
            offsetZ = StageController.Instance.GetFormationOffsetZ();
        }

        float zPos;
        if (isMovingToNextRow)
        {
            // BUG FIX: 移动过程中同时平滑过渡 X 轴和 Z 轴。
            // 之前 X 轴仅使用 rowIndex（当前排），导致移动完成时 X 瞬间跳跃到目标排的偏移量。
            // 现在 X 和 Z 都从当前排(rowIndex)平滑过渡到目标排(rowIndex-1)，
            // 使阵型的梯形/扇形展开在移动过程中就正确显示，而非抵达后才调整。
            //
            // rowIndex 尚未更新，使用 rowIndex 作为起点，rowIndex-1 作为终点
            float currentX = xPos; // 当前排的 X 偏移（已在上面通过 GetFormationOffset 计算）
            float targetX = StageController.Instance.GetFormationOffset(columnIndex, rowIndex - 1);
            xPos = Mathf.Lerp(currentX, targetX, moveProgress);

            // Z 轴：从当前排向目标排平滑过渡
            float currentRowZ = GetRowZ(rowIndex, rowSpacing, offsetZ);
            float targetRowZ = GetRowZ(rowIndex - 1, rowSpacing, offsetZ);
            zPos = Mathf.Lerp(currentRowZ, targetRowZ, moveProgress);
        }
        else
        {
            // 非移动中：固定在当前排位置
            zPos = GetRowZ(rowIndex, rowSpacing, offsetZ);
        }

        // 使用 localPosition 而非 position，这样敌人会相对于父节点定位
        // 你可以在场景中创建一个空的 Enemies GameObject 作为父节点，
        // 然后调整父节点的 Transform Position 来整体移动所有敌人的位置
        // DOTween: 前进补齐时 Y 轴弹跳由 bounceYOffset 驱动（非补齐移动时 bounceYOffset = 0）
        transform.localPosition = new Vector3(xPos, bounceYOffset, zPos);
    }

    /// <summary>
    /// 更新透明度（基于排索引）+ 受伤闪白效果
    /// 使用材质实例直接修改 color，确保闪白在对象禁用前始终可见。
    /// </summary>
    private void UpdateAlpha()
    {
        if (flashMaterials == null || flashMaterials.Length == 0) return;

        float alpha = GetAlphaForRow(rowIndex);

        foreach (var mat in flashMaterials)
        {
            if (mat == null) continue;

            // 使用白色作为基础色，仅通过透明度控制显示
            Color color = Color.white;
            color.a = alpha;

            // 受伤闪白效果：hitFlashTimer > 0 时设置全白全透明（alpha=1），无视排索引透明度
            if (hitFlashTimer > 0f)
            {
                color = Color.white; // (1, 1, 1, 1) — 全白全透明
            }

            mat.color = color;
        }
    }

    /// <summary>
    /// 根据排索引获取透明度
    /// </summary>
    private float GetAlphaForRow(int row)
    {
        // 从StageController获取透明度配置
        // 默认：第0排=1.0, 第1排=0.8, 第2排=0.6, 第3排=0.4, 第4排=0.2, 第5排+=0
        float[] factors = null;
        if (StageController.Instance != null)
        {
            factors = StageController.Instance.GetRowAlphaFactors();
        }

        if (factors == null)
        {
            factors = new float[] { 1.0f, 0.8f, 0.6f, 0.4f, 0.2f };
        }

        if (row < factors.Length)
            return factors[row];
        return 0f;
    }

    #endregion

    #region Boss 分阶段推进

    /// <summary>
    /// Boss 分阶段推进入口：根据当前 rowIndex 决定行为
    ///   rowIndex >= 2: 暂停等待前两排清空
    ///   rowIndex <= 1: 直接进入战斗
    /// 由 EnemyManager.RegisterEnemy 在 Boss 注册后调用
    /// </summary>
    public void StartBossPhaseAdvance()
    {
        if (!isBoss || bossState != BossState.None || state == EnemyState.Dead) return;

        // BOSS 已在最前排：直接应战
        if (rowIndex <= 1)
        {
            bossState = BossState.InCombat;
            Debug.Log($"[Enemy] Boss直接进入战斗(rowIndex={rowIndex}): {DebugTag}, col={columnIndex}");
            OnBossEngaged?.Invoke(this);
            return;
        }

        // BOSS 在后方：参与正常补齐链，TryStartRushMove 中的跨列检查保证
        // BOSS 仅在整排前方清空时才前进。到达 rowIndex=2 时触发 BossPause。
        Debug.Log($"[Enemy] Boss加入补齐链(rowIndex={rowIndex}): {DebugTag}, col={columnIndex}");
    }

    /// <summary>
    /// BOSS 补齐检查：指定排（跨所有列）是否已无存活非BOSS敌人。
    /// 使用 enemy.rowIndex 而非列表位置判断排归属（PerRow 模式下列表位置不再反映真实排号）。
    /// </summary>
    private bool IsRowClearForBoss(int row)
    {
        var cm = EnemyManager.Instance?.columnManager;
        if (cm == null) return true;

        for (int c = 0; c < cm.columnCount; c++)
        {
            var col = cm.GetColumn(c);
            if (col == null) continue;
            foreach (var e in col.enemies)
            {
                if (e == null) continue;
                if (e == this) continue;
                if (e.rowIndex != row) continue;
                if (e.state != EnemyState.Dead)
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 逐排补齐检查：前方一整排（跨所有列）是否已无存活敌人。
    /// 用于 PerRow 模式下非补齐移动的前置检查。
    /// </summary>
    private bool IsFrontRowClear()
    {
        if (rowIndex <= 0) return true;
        return IsRowClearForBoss(rowIndex - 1);
    }

    /// <summary>
    /// Boss 在第3排暂停：停止移动，等待前两排清空
    /// </summary>
    private void BossPause()
    {
        bossState = BossState.Approaching;
        state = EnemyState.Idle;
        pendingRushMove = false;
        targetRow = -1;

        // 停止 DOTween
        transform.DOKill(false);
        UpdateWorldPosition();
        transform.localScale = originalScale;

        // BUG FIX: 确保 Boss 在 Approaching 阶段可见
        // 强制启用 SpriteRenderer（某些路径可能导致 renderer 被禁用）
        if (renderers != null)
        {
            foreach (var r in renderers)
            {
                if (r != null) r.enabled = true;
            }
        }
        gameObject.SetActive(true);

        // 立即触发一次 UpdateAlpha 确保透明度正确
        // 防御：若 flashMaterials 意外为 null，直接通过 renderer 设置 alpha
        if (flashMaterials != null && flashMaterials.Length > 0)
        {
            UpdateAlpha();
        }
        else if (renderers != null)
        {
            float rowAlpha = GetAlphaForRow(rowIndex);
            foreach (var r in renderers)
            {
                if (r != null) r.material.color = new Color(1f, 1f, 1f, rowAlpha);
            }
        }

        // 订阅列修改事件，检测前两排是否清空
        var cm = EnemyManager.Instance?.columnManager;
        if (cm != null)
        {
            _onColumnsModifiedHandler = OnColumnsModifiedForBoss;
            cm.OnColumnsModified += _onColumnsModifiedHandler;
        }

        float alpha = GetAlphaForRow(rowIndex);
        Debug.Log($"[Enemy] Boss暂停在第3排: {DebugTag}, col={columnIndex}, rowIndex={rowIndex}, pos={transform.localPosition}, alpha={alpha}, active={gameObject.activeSelf}, rendererEnabled={GetComponent<SpriteRenderer>()?.enabled}, 等待前两排清空");
    }

    /// <summary>
    /// 列修改回调：Boss 分阶段推进的两种等待模式
    ///   1. BossState.Approaching（BossPause）：检测前两排(rowIndex 0,1)是否全部清空
    ///   2. BossState.None + pendingRushMove（补齐等待）：检测前方排是否已清空，重试 TryStartRushMove
    /// </summary>
    private void OnColumnsModifiedForBoss()
    {
        if (state == EnemyState.Dead) return;

        if (bossState == BossState.Approaching)
        {
            // 模式1：BossPause — 等待前两排清空
            var cm = EnemyManager.Instance?.columnManager;
            if (cm == null) return;

            for (int c = 0; c < cm.columnCount; c++)
            {
                var col = cm.GetColumn(c);
                if (col == null) continue;
                for (int r = 0; r <= 1 && r < col.enemies.Count; r++)
                {
                    var e = col.enemies[r];
                    if (e != null && e != this && e.state != EnemyState.Dead)
                        return;
                }
            }

            BossResume();
            return;
        }

        if (bossState == BossState.None && pendingRushMove && rowIndex > 2)
        {
            // 模式2：补齐等待 — 前方排已清空，重试 TryStartRushMove
            if (IsRowClearForBoss(rowIndex - 1))
            {
                var cm = EnemyManager.Instance?.columnManager;
                if (cm != null && _onColumnsModifiedHandler != null)
                {
                    cm.OnColumnsModified -= _onColumnsModifiedHandler;
                    _onColumnsModifiedHandler = null;
                }
                Debug.Log($"[Enemy] Boss前方排已清空，重试补齐: {DebugTag}, col={columnIndex}, row={rowIndex}");
                TryStartRushMove();
            }
        }
    }

    /// <summary>
    /// Boss 恢复推进：从第3排(rowIndex=2)补齐移动到第2排(rowIndex=1)
    /// </summary>
    private void BossResume()
    {
        // 取消订阅
        var cm = EnemyManager.Instance?.columnManager;
        if (cm != null && _onColumnsModifiedHandler != null)
        {
            cm.OnColumnsModified -= _onColumnsModifiedHandler;
            _onColumnsModifiedHandler = null;
        }

        Debug.Log($"[Enemy] Boss恢复推进: {DebugTag}, col={columnIndex}, 从第3排→第2排");

        targetRow = 1;
        pendingRushMove = true;
        TryStartRushMove();
    }

    #endregion

    #region 对象池

    private void OnDisable()
    {
        initialized = false;
    }

    /// <summary>
    /// 重置状态（回收到对象池时调用）
    /// </summary>
    public void ResetEnemy()
    {
        // 终止所有活跃的 DOTween 动画（完成当前值后跳转到最终值）
        transform.DOKill(true);

        state = EnemyState.Dead;
        currentHealth = 0f;
        currentPoise = 0f;
        initialized = false;
        // 清理 Boss 分阶段推进订阅
        if (_onColumnsModifiedHandler != null)
        {
            var cm = EnemyManager.Instance?.columnManager;
            if (cm != null)
                cm.OnColumnsModified -= _onColumnsModifiedHandler;
            _onColumnsModifiedHandler = null;
        }
        bossState = BossState.None;

        OnDeath = null;
        OnDamageTaken = null;
        OnHealthChanged = null;
        OnPoiseChanged = null;
        OnBossEngaged = null;
        // 销毁材质实例，避免内存泄漏
        DestroyFlashMaterials();
        gameObject.SetActive(false);
    }

    #endregion
}

/// <summary>
/// 伤害类型枚举
/// </summary>
public enum DamageType
{
    Stab,   // 戳击
    Slash,  // 斩击
    Pierce, // 穿刺
    Sweep,  // 横扫
    Launch, // 挑飞
    Poise   // 架势伤害
}

/// <summary>
/// 招架眩晕阈值：招架造成伤害后，若敌人血量低于 healthPercent，则眩晕 stunDuration 秒
/// </summary>
[System.Serializable]
public struct ParryStunThreshold
{
    [UnityEngine.Range(0f, 1f)] public float healthPercent;
    public float stunDuration;
}
