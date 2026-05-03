using UnityEngine;

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
    private float moveProgress; // 0~1, 当前排内移动进度
    private bool isMovingToNextRow;
    private MaterialPropertyBlock mpb;
    private Renderer[] renderers;
    private bool initialized;

    // 受伤闪白相关
    private float hitFlashTimer; // 闪白剩余时间
    private Color originalColor = Color.white; // 精灵原始颜色（白色）
    private const float HIT_FLASH_DURATION = 0.15f; // 闪白持续时间

    // 事件
    public System.Action<Enemy> OnDeath;
    public System.Action<Enemy> OnDamageTaken;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        mpb = new MaterialPropertyBlock();
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
        initialized = true;

        gameObject.SetActive(true);
        UpdateWorldPosition();
        UpdateAlpha();

        // BUG FIX: 敌人生成后不自动开始移动。
        // 敌人开局时直接站在对应排位置，不需要移动。
        // 只有当前排被击杀出现空位时，Column.RemoveEnemy() 才会触发后方敌人前进。
        // 如果 rowIndex == 0（最前排），直接进入攻击状态。
        if (rowIndex <= 0)
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

        // 更新受伤闪白计时器
        if (hitFlashTimer > 0f)
        {
            hitFlashTimer -= Time.deltaTime;
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
    public void StartMoving()
    {
        if (state == EnemyState.Dead) return;

        // BUG FIX: 如果已经到达最前排，直接进入攻击状态而非移动
        // 否则 UpdateWorldPosition() 中 targetRowZ 计算为正值，敌人会向后退
        if (rowIndex <= 0)
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

        state = EnemyState.Moving;
        isMovingToNextRow = true;
        moveProgress = 0f;
    }

    /// <summary>
    /// 开始攻击
    /// BUG FIX: 攻击分为两个阶段：
    ///   阶段1：攻击动画（attackAnimTimer）— 播放攻击动作，不造成伤害
    ///   阶段2：攻击冷却（attackTimer）— 动画结束后造成伤害，然后进入冷却
    /// 这样攻击动画执行期间敌人不会立即再次攻击，配合动画效果
    ///
    /// BUG FIX: 不再重置 attackTimer 为 0f。
    /// attackTimer 在 Initialize() 中被初始化为 1f（正数），
    /// 在冷却阶段结束后才被设为 cooldown 值。
    /// 如果在 StartAttacking() 中重置 attackTimer = 0f，
    /// 会导致 UpdateAttack() 中 isAttackAnimating == false 时
    /// 立即触发 attackTimer <= 0f 条件，跳过冷却直接开始下一次攻击。
    /// </summary>
    public void StartAttacking()
    {
        if (state == EnemyState.Dead) return;
        state = EnemyState.Attacking;
        // 先播放攻击动画（固定0.5秒），动画结束后才造成伤害
        attackAnimTimer = 0.5f;
        isAttackAnimating = true;
        // BUG FIX: 不重置 attackTimer，避免跳过冷却
        // attackTimer 在 Initialize() 中初始化为 1f
        // 在冷却阶段结束后才被设为 cooldown 值
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
    /// BUG FIX: 移动完成后不调用 StartMoving() 继续移动，
    /// 而是由 Column.RemoveEnemy() / ColumnManager.UpdateEnemyRow() 触发补齐移动。
    /// 避免"无限补齐"的无限循环。
    /// </summary>
    private void UpdateMovement()
    {
        if (!isMovingToNextRow) return;

        // 常规移动：基于 moveSpeed（秒/排）
        float speed = config != null ? config.moveSpeed : 1f;
        moveProgress += Time.deltaTime / speed;

        if (moveProgress >= 1f)
        {
            moveProgress = 0f;
            isMovingToNextRow = false;

            // 常规移动：rowIndex 尚未更新，需要前进一排
            rowIndex--;

            // BUG FIX: 防止 rowIndex 变为负数
            if (rowIndex < 0) rowIndex = 0;

            // 前进一排后，通知ColumnManager更新列内排序
            EnemyManager.Instance?.OnEnemyMovedForward(this);

            // BUG FIX: 使用 EnemyConfig.attackRange 决定攻击距离
            // attackRange=1 表示需要到最前排（rowIndex=0）才能攻击
            // attackRange=2 表示距离玩家还有1排时（rowIndex=1）就能攻击
            // 以此类推
            int attackRange = config != null ? (int)Mathf.Max(1, config.attackRange) : 1;
            if (rowIndex < attackRange)
            {
                StartAttacking();
            }
            // BUG FIX: 不再调用 StartMoving() 继续移动。
            // 移动完成后，由 ColumnManager.UpdateEnemyRow() 触发后方敌人的补齐移动。
            // 如果这里调用 StartMoving()，会导致当前敌人继续向前移动，
            // 而 UpdateEnemyRow() 又会触发后方敌人补齐移动，造成无限循环。
        }

        UpdateWorldPosition();
    }

    /// <summary>
    /// 更新攻击状态
    /// BUG FIX: 攻击分为两个阶段：
    ///   阶段1：攻击动画（attackAnimTimer > 0）— 播放攻击动作，不造成伤害
    ///   阶段2：攻击冷却（attackTimer > 0）— 动画结束后造成伤害，然后进入冷却
    /// 这样攻击动画执行期间敌人不会立即再次攻击，配合动画效果
    /// </summary>
    private void UpdateAttack()
    {
        if (isAttackAnimating)
        {
            // 阶段1：播放攻击动画
            attackAnimTimer -= Time.deltaTime;
            if (attackAnimTimer <= 0f)
            {
                // 动画结束，造成伤害
                isAttackAnimating = false;
                PerformAttack();
                // 进入冷却阶段
                float cooldown = config != null ? (1f / config.attackSpeed) : 1f;
                attackTimer = cooldown;
            }
        }
        else
        {
            // 阶段2：攻击冷却
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                // 冷却结束，开始下一次攻击动画
                attackAnimTimer = 0.5f;
                isAttackAnimating = true;
            }
        }
    }

    private void PerformAttack()
    {
        // 通知玩家受到伤害
        // 由EnemyManager转发给PlayerState
        EnemyManager.Instance?.OnEnemyAttackPlayer(this);
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

        currentHealth -= finalDamage;
        OnDamageTaken?.Invoke(this);

        // 触发受伤闪白效果
        hitFlashTimer = HIT_FLASH_DURATION;

        if (currentHealth <= 0f)
        {
            Die();
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
    /// </summary>
    public void Die()
    {
        if (state == EnemyState.Dead) return;
        state = EnemyState.Dead;
        // 触发死亡事件，EnemyManager 通过 RegisterEnemy 订阅了此事件
        OnDeath?.Invoke(this);
    }

    #endregion

    #region 位置与透明度

    /// <summary>
    /// 重置移动状态（列内补齐时调用）
    /// 重置 state=Idle、isMovingToNextRow=false、moveProgress=0，
    /// 以便 StartMoving() 能通过 state==Moving 保护检查，重新开始移动。
    ///
    /// 注意：ResetMovementState() + StartMoving() 的组合用于列内补齐移动。
    /// Column.RemoveEnemy() 和 ColumnManager.UpdateEnemyRow() 中，
    /// 先调用 ResetMovementState() 重置状态，
    /// 再调用 SetRowIndex() 更新 rowIndex（内部调用 UpdateWorldPosition() 更新位置），
    /// 最后调用 StartMoving() 开始向更前一排移动。
    /// </summary>
    public void ResetMovementState()
    {
        state = EnemyState.Idle;
        isMovingToNextRow = false;
        moveProgress = 0f;
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
    /// BUG FIX: 移动过程中 X 轴使用 rowIndex（旧排位置）计算阵型偏移，
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
            // 移动中：从当前排向目标排平滑过渡
            // moveProgress: 0→1 表示从起点排移动到终点排
            //
            // rowIndex 尚未更新，使用 rowIndex 作为起点，rowIndex-1 作为终点
            // currentRowZ = GetRowZ(rowIndex, ...) — 当前排位置
            // targetRowZ  = GetRowZ(rowIndex - 1, ...) — 前一排位置
            // rowIndex从4→3→2→1→0，Z值从0→-2.5→-5.0→-7.5→-10，向-Z方向移动（前进）
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
        transform.localPosition = new Vector3(xPos, 0f, zPos);
    }

    /// <summary>
    /// 更新透明度（基于排索引）+ 受伤闪白效果
    /// 使用精灵图片的原始颜色（白色），仅修改透明度
    /// 受伤时短暂变为白色（闪白），然后恢复
    /// </summary>
    private void UpdateAlpha()
    {
        if (renderers == null || renderers.Length == 0) return;

        float alpha = GetAlphaForRow(rowIndex);

        foreach (var renderer in renderers)
        {
            renderer.GetPropertyBlock(mpb);

            // 使用白色作为基础色，仅通过透明度控制显示
            // 这样精灵图片的上色不会被覆盖
            Color color = Color.white;
            color.a = alpha;

            // 受伤闪白效果：hitFlashTimer > 0 时显示纯白色
            // 闪白结束后恢复原始颜色
            if (hitFlashTimer > 0f)
            {
                // 闪白期间：颜色为白色，透明度不变
                // 这样精灵图片会短暂显示为白色（闪白）
            }

            mpb.SetColor("_Color", color);
            renderer.SetPropertyBlock(mpb);
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
        state = EnemyState.Dead;
        currentHealth = 0f;
        currentPoise = 0f;
        config = null;
        initialized = false;
        OnDeath = null;
        OnDamageTaken = null;
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
