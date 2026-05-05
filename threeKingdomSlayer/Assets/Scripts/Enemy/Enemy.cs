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
    Dead
}

/// <summary>
/// 敌人实体 - MonoBehaviour
/// 管理敌人的生命值、架势值、状态机、前进移动、透明度渐变
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("配置")]
    public EnemyConfig config;

    [Header("运行时状态")]
    public EnemyState state = EnemyState.Idle;
    public float currentHealth;
    public float currentPoise;
    public int columnIndex;
    public int rowIndex; // 0 = 最前排

    // 内部状态
    private float stunTimer;
    private float launchTimer;
    private float attackTimer;      // 攻击冷却计时器（攻击动画结束后开始冷却）
    private float attackAnimTimer;  // 攻击动画计时器（攻击动作执行时间）
    private bool isAttackAnimating; // 是否正在播放攻击动画

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

    // 事件
    public System.Action<Enemy> OnDeath;
    public System.Action<Enemy> OnDamageTaken;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
    }

    /// <summary>
    /// 初始化敌人（从对象池取出时调用）
    /// 敌人生成时直接站在对应排位置，不自动开始移动。
    /// 只有当前排出现空位时（通过 Column.RemoveEnemy()），
    /// 后方敌人才会向前补齐。
    /// </summary>
    public void Initialize(EnemyConfig cfg, int col, int row)
    {
        config = cfg;
        columnIndex = col;
        rowIndex = row;

        currentHealth = cfg.maxHealth;
        currentPoise = cfg.maxPoise;
        state = EnemyState.Idle;
        stunTimer = 0f;
        launchTimer = 0f;
        // BUG FIX: attackTimer 初始化为一个正数，避免第一次进入 UpdateAttack() 时
        // 立即触发冷却结束（attackTimer <= 0f），导致攻击动画被跳过
        attackTimer = 1f;
        attackAnimTimer = 0f;
        isAttackAnimating = false;
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
        UpdateWorldPosition();

        // 如果敌人已在攻击范围内（由 EnemyConfig.attackRange 决定），直接进入攻击状态
        int attackRange = config != null ? (int)Mathf.Max(1, config.attackRange) : 1;
        if (rowIndex < attackRange)
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
                Debug.Log($"[Enemy] 补齐延迟结束，尝试继续补齐: enemyId={config?.enemyId}, col={columnIndex}, row={rowIndex}");
                TryStartRushMove();
            }
        }

        // 更新受伤闪白计时器
        if (hitFlashTimer > 0f)
        {
            hitFlashTimer -= Time.deltaTime;
            if (hitFlashTimer <= 0f)
            {
                Debug.Log($"[Enemy] 闪白结束: enemyId={config?.enemyId}");
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

        // 如果敌人已在攻击范围内（由 EnemyConfig.attackRange 决定），直接进入攻击状态而非移动
        int attackRange = config != null ? (int)Mathf.Max(1, config.attackRange) : 1;
        if (rowIndex < attackRange)
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
        Debug.Log($"[Enemy] StartMoving: enemyId={config?.enemyId}, col={columnIndex}, row={rowIndex}, targetRow={rowIndex - 1}, isRush={isRush}");
        state = EnemyState.Moving;
        isMovingToNextRow = true;
        moveProgress = 0f;

        // DOTween: 前进补齐时 Y 轴弹跳动画（一边前进一边沿 Y 轴跳动）
        if (isRush)
        {
            DOTween.Kill(transform, false); // 终止之前可能残留的弹跳动画
            float moveDuration = config != null ? config.moveSpeed : 1f;
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
        state = EnemyState.Attacking;
        // BUG FIX: 先进入攻击冷却阶段，冷却结束后才播放攻击动画
        // 不再立即播放攻击动画，而是从冷却阶段开始
        isAttackAnimating = false;
        // 初始冷却：attackTimer 已在 Initialize() 中设为 1f，保留该值
        // 后续冷却在 UpdateAttack() 的动画结束后设置
    }

    public void Stun(float duration)
    {
        if (state == EnemyState.Dead) return;
        state = EnemyState.Stunned;
        stunTimer = duration;
    }

    public void Launch(float duration)
    {
        if (state == EnemyState.Dead) return;
        state = EnemyState.Launched;
        launchTimer = duration;
    }

    #endregion

    #region 状态更新

    private void UpdateStun()
    {
        stunTimer -= Time.deltaTime;
        if (stunTimer <= 0f)
        {
            StartMoving();
        }
    }

    private void UpdateLaunch()
    {
        launchTimer -= Time.deltaTime;
        if (launchTimer <= 0f)
        {
            StartMoving();
        }
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
        float speed = config != null ? config.moveSpeed : 1f;
        moveProgress += Time.deltaTime / speed;

        if (moveProgress >= 1f)
        {
            bool wasRush = isRushMove;
            Debug.Log($"[Enemy] 移动完成: enemyId={config?.enemyId}, col={columnIndex}, oldRow={rowIndex}, newRow={rowIndex - 1}, isRush={wasRush}");
            moveProgress = 0f;
            isMovingToNextRow = false;
            isRushMove = false;
            rushMoveChainTriggered = false; // 重置链式触发标记

            // 移动完成：rowIndex 前进一排
            rowIndex--;

            // BUG FIX: 防止 rowIndex 变为负数
            if (rowIndex < 0) rowIndex = 0;

            // 使用 EnemyConfig.attackRange 决定攻击距离
            int attackRange = config != null ? (int)Mathf.Max(1, config.attackRange) : 1;
            bool reachedAttackRange = rowIndex < attackRange;

            if (wasRush)
            {
                if (reachedAttackRange)
                {
                    // 到达攻击范围，开始攻击
                    Debug.Log($"[Enemy] 补齐移动完成（到达攻击范围）: enemyId={config?.enemyId}, col={columnIndex}, row={rowIndex}");
                    StartAttacking();
                }
                else if (targetRow >= 0 && rowIndex <= targetRow)
                {
                    // BUG FIX: Problem 4 - 已到达目标位置（列表位置），停止补齐
                    // targetRow 由 Column.RemoveEnemy() / ColumnManager.UpdateEnemyRow() 设置，
                    // 值为列表位置 i（SetRowIndex(i+1) 后的目标排）。
                    // 当 rowIndex <= targetRow 时，该敌人已到达正确位置，禁止继续向前补齐，
                    // 防止多个敌人在 delay 循环中全部汇聚到 row=0 导致重叠。
                    Debug.Log($"[Enemy] 补齐移动完成（到达目标位置，停止补齐）: enemyId={config?.enemyId}, col={columnIndex}, row={rowIndex}, targetRow={targetRow}");
                    state = EnemyState.Idle;
                    pendingRushMove = false;
                    targetRow = -1;
                }
                else
                {
                    // 尚未到达攻击范围，启动补齐延迟
                    // 不再连续多排补齐，而是等待 rushMoveDelay 秒后再开始下一次补齐移动。
                    float delay = 0f;
                    if (StageController.Instance != null)
                    {
                        delay = StageController.Instance.GetRushMoveDelay();
                    }
                    if (delay > 0f)
                    {
                        Debug.Log($"[Enemy] 补齐移动完成（等待延迟继续补齐）: enemyId={config?.enemyId}, col={columnIndex}, row={rowIndex}, delay={delay:F2}s");
                        state = EnemyState.Idle;
                        pendingRushMove = true;
                        rushMoveDelayTimer = delay;
                    }
                    else
                    {
                        // 无延迟，立即继续补齐
                        Debug.Log($"[Enemy] 补齐移动完成（立即继续补齐）: enemyId={config?.enemyId}, col={columnIndex}, row={rowIndex}");
                        state = EnemyState.Idle;
                        pendingRushMove = true;
                        TryStartRushMove();
                    }
                }

                // BUG FIX: 链式触发移至移动完全完成后，而非移动中期。
                // 必须等待前一敌人完全补齐完毕（moveProgress >= 1.0），
                // 后一敌人才能开始补齐。这样符合"逐个向前补齐"的行为。
                // 使用 rushMoveChainTriggered 避免在同一个移动过程中重复触发。
                if (!rushMoveChainTriggered)
                {
                    rushMoveChainTriggered = true;
                    Debug.Log($"[Enemy] 补齐移动完全完成（触发链式）: enemyId={config?.enemyId}, col={columnIndex}, row={rowIndex}");
                    OnRushMoveComplete?.Invoke(this);
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
                Debug.Log($"[Enemy] 自然移动完成，触发 UpdateEnemyRow: enemyId={config?.enemyId}, col={columnIndex}");
            }
        }

        UpdateWorldPosition();
    }

    /// <summary>
    /// 更新攻击状态
    /// BUG FIX: 攻击分为两个阶段：
    ///   阶段1：攻击冷却（attackTimer > 0）— 先进入冷却再攻击
    ///   阶段2：攻击动画（DOTween）— 使用 DOTween Sequence 播放前移+翻转+后退动效，动画结束时造成伤害
    ///
    /// 攻击优先级规则（Problem 1 修复）：
    ///   1. 冷却期间如果 pendingRushMove == true → 先执行补齐再攻击
    ///   2. 动画期间不中断 → 完成当前攻击后检查 pendingRushMove
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
    /// 使用 DOTween 播放攻击动效
    /// 效果：向前（-Z方向）移动 + 左右镜像翻转（scale.X * -1）→ 后退到原位 + 翻转回正
    /// 动画时长基于 attackSpeed（攻击速度越快动画越快）
    /// 动画完成后：PerformAttack() → 进入冷却 → 检查是否需要补齐
    /// </summary>
    private void PlayAttackAnimationTween()
    {
        isAttackAnimating = true;

        // 保存起始位置和缩放（动画完成后恢复）
        Vector3 startPos = transform.localPosition;
        Vector3 startScale = transform.localScale;

        // 根据 attackSpeed 计算动画时长
        // 攻击间隔 = 1/attackSpeed，动画占 60%，冷却占 40%
        float totalInterval = config != null ? (1f / config.attackSpeed) : 1f;
        float animDuration = totalInterval * 0.6f;
        // 限制最小/最大动画时长
        animDuration = Mathf.Clamp(animDuration, 0.15f, 0.6f);
        float halfAnim = animDuration * 0.5f;
        float forwardDistance = 0.5f; // 向前移动距离

        attackSequence = DOTween.Sequence();
        attackSequence.SetTarget(transform);
        attackSequence.SetId("attackAnim");

        // 阶段1：向前移动 + 左右镜像翻转
        // 使用 Join 让两个动画同时进行
        attackSequence.Append(transform.DOLocalMoveZ(startPos.z - forwardDistance, halfAnim).SetEase(Ease.OutQuad));
        attackSequence.Join(transform.DOScaleX(-startScale.x, halfAnim).SetEase(Ease.OutQuad));

        // 阶段2：后退到原位 + 翻转回正常
        attackSequence.Append(transform.DOLocalMoveZ(startPos.z, halfAnim).SetEase(Ease.InQuad));
        attackSequence.Join(transform.DOScaleX(startScale.x, halfAnim).SetEase(Ease.InQuad));

        // 动画完成回调
        attackSequence.OnComplete(() =>
        {
            attackSequence = null;
            isAttackAnimating = false;
            PerformAttack();

            // 进入冷却阶段（动画占 60%，冷却占 40%）
            float cooldown = totalInterval * 0.4f;
            if (cooldown < 0.1f) cooldown = 0.1f; // 最小冷却时间
            attackTimer = cooldown;

            // 完成当前攻击后，检查是否需要向前补齐
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

        // 应用弱点倍率
        float multiplier = GetDamageMultiplier(damageType);
        float finalDamage = damage * multiplier;

        Debug.Log($"[Enemy] TakeDamage: enemyId={config?.enemyId}, col={columnIndex}, raw={damage:F1}, mult={multiplier:F2}, final={finalDamage:F1}, hp={currentHealth:F1}→{currentHealth - finalDamage:F1}");

        currentHealth -= finalDamage;
        OnDamageTaken?.Invoke(this);

        // 受伤跳字：在敌人右侧显示红色带黑描边的伤害数字
        if (DamageNumberManager.Instance != null)
        {
            DamageNumberManager.Instance.Spawn(transform.position, finalDamage);
        }

        // BUG FIX: 同步应用闪白（立即设置颜色，不依赖 Update 循环）
        // 即使敌人秒杀死亡，闪白效果也在死亡前被渲染
        // 否则 Die() 设置 state = Dead 后，Update() 提前返回，UpdateAlpha() 不被调用
        ApplyHitFlashImmediate();

        // 触发受伤闪白效果（非致命伤通过 Update 循环过渡恢复）
        hitFlashTimer = HIT_FLASH_DURATION;
        Debug.Log($"[Enemy] 触发闪白: enemyId={config?.enemyId}, duration={HIT_FLASH_DURATION}");

        // DOTween: 受击大小抖动效果（与闪白同步触发）
        transform.DOKill(true); // 完成当前正在播放的任何缩放动画，避免与新抖动冲突
        transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0.2f), 0.15f, 8, 0.5f);

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
    /// 受到架势伤害
    /// </summary>
    public void TakePoiseDamage(float poiseDamage)
    {
        if (state == EnemyState.Dead) return;

        currentPoise -= poiseDamage;
        if (currentPoise <= 0f)
        {
            // 架势破碎 -> 眩晕
            float stunDuration = config != null ? config.stunDuration : 1.5f;
            Stun(stunDuration);
            currentPoise = config != null ? config.maxPoise : 50f; // 重置架势
        }
    }

    /// <summary>
    /// 根据伤害类型获取倍率
    /// </summary>
    private float GetDamageMultiplier(DamageType damageType)
    {
        if (config == null) return 1f;

        switch (damageType)
        {
            case DamageType.Stab:   return config.stabDamageMultiplier;
            case DamageType.Slash:  return config.slashDamageMultiplier;
            case DamageType.Pierce: return config.pierceDamageMultiplier;
            case DamageType.Sweep:  return config.sweepDamageMultiplier;
            case DamageType.Launch: return config.launchDamageMultiplier;
            case DamageType.Poise:  return config.poiseDamageMultiplier;
            default: return 1f;
        }
    }

    /// <summary>
    /// 死亡
    /// BUG FIX: 改为使用协程处理闪白效果，而非立即触发死亡事件。
    /// 因为 EnemyManager.OnEnemyDied() → EnemyPool.ReturnEnemy() → ResetEnemy() → SetActive(false)
    /// 会在同一帧禁用 GameObject，导致材质颜色修改无法被渲染（渲染管线跳过禁用对象）。
    ///
    /// 新流程：
    ///   1. state = Dead（Update 提前返回，不再处理攻击/移动逻辑）
    ///   2. 取消正在执行的攻击 DOTween 动画
    ///   3. 启动死亡动效协程 DeathBounceAndFall()
    ///   4. 协程结束后触发 OnDeath 事件（池回收，禁用 GameObject）
    /// </summary>
    public void Die()
    {
        if (state == EnemyState.Dead) return;
        state = EnemyState.Dead;

        // BUG FIX: 取消正在执行的攻击 DOTween 动画
        // 若敌人在攻击动画中被秒杀，立即中断攻击动作（前移+翻转），直接进入死亡状态
        if (attackSequence != null && attackSequence.IsActive())
        {
            attackSequence.Kill();
            attackSequence = null;
        }
        isAttackAnimating = false;
        isMovingToNextRow = false;

        // 启动死亡动效协程（弹起 + 旋转 + 重力掉落）
        StartCoroutine(DeathBounceAndFall());
    }

    /// <summary>
    /// 死亡动效协程 — 弹起 + 随机旋转 + 重力掉落
    /// 使用 DOTween 实现：
    ///   1. 立即闪白（ApplyHitFlashImmediate）
    ///   2. 弹起（Y 轴向上 OutQuad 缓动）
    ///   3. 随机旋转（在 X 和 Z 轴上随机角度，贯穿整个动画）
    ///   4. 受重力掉落离开屏幕（Y 轴向下 InQuad 缓动）
    ///   5. 恢复缩放和旋转（供对象池复用），触发 OnDeath 事件
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
        transform.localScale = Vector3.one;
        transform.localRotation = Quaternion.identity;

        // 死亡动效结束后触发死亡事件（EnemyManager.OnEnemyDied → ReturnEnemy → SetActive(false)）
        OnDeath?.Invoke(this);
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
        state = EnemyState.Idle;
        isMovingToNextRow = false;
        moveProgress = 0f;
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
                    // 冷却阶段：先执行补齐再攻击
                    pendingRushMove = false;
                    ResetMovementState();
                    StartMoving(true);
                    return true;
                }
                // 动画阶段：等待动画完成，由 UpdateAttack() 调用 TryStartRushMove
                return false;

            case EnemyState.Stunned:
            case EnemyState.Launched:
                // 等待恢复，恢复后 StartMoving() 会检查 pendingRushMove
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
        float[] factors;
        if (StageController.Instance != null)
        {
            factors = StageController.Instance.rowAlphaFactors;
        }
        else
        {
            factors = new float[] { 1.0f, 0.8f, 0.6f, 0.4f, 0.2f };
            Debug.LogWarning("[Enemy] StageController.Instance 为 null，使用默认透明度配置");
        }

        if (row < factors.Length)
            return factors[row];
        return 0f;
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
        config = null;
        initialized = false;
        OnDeath = null;
        OnDamageTaken = null;
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
