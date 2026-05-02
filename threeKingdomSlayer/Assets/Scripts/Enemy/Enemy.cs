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
    private float attackTimer;
    private float moveProgress; // 0~1, 当前排内移动进度
    private bool isMovingToNextRow;
    private MaterialPropertyBlock mpb;
    private Renderer[] renderers;
    private bool initialized;

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
        attackTimer = 0f;
        moveProgress = 0f;
        isMovingToNextRow = false;
        initialized = true;

        gameObject.SetActive(true);
        UpdateWorldPosition();
        UpdateAlpha();
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
                // Idle状态下自动开始前进
                StartMoving();
                break;
        }

        // 每帧更新透明度
        UpdateAlpha();
    }

    #region 状态切换

    public void StartMoving()
    {
        if (state == EnemyState.Dead) return;
        state = EnemyState.Moving;
        isMovingToNextRow = true;
        moveProgress = 0f;
    }

    public void StartAttacking()
    {
        if (state == EnemyState.Dead) return;
        state = EnemyState.Attacking;
        attackTimer = 1f / (config != null ? config.attackSpeed : 1f);
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

    private void UpdateMovement()
    {
        if (!isMovingToNextRow) return;

        float speed = config != null ? config.moveSpeed : 1f;
        moveProgress += Time.deltaTime / speed;

        if (moveProgress >= 1f)
        {
            moveProgress = 0f;
            isMovingToNextRow = false;
            // 前进一排后，通知ColumnManager更新排索引
            EnemyManager.Instance?.OnEnemyMovedForward(this);
            // 到达最前排后开始攻击
            if (rowIndex == 0)
            {
                StartAttacking();
            }
            else
            {
                state = EnemyState.Idle;
            }
        }

        UpdateWorldPosition();
    }

    private void UpdateAttack()
    {
        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            // 攻击玩家
            PerformAttack();
            attackTimer = 1f / (config != null ? config.attackSpeed : 1f);
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
    /// 更新世界坐标
    /// 列：X轴使用梯形/扇形阵型偏移，排：Z轴偏移（负Z为前进方向）
    /// </summary>
    private void UpdateWorldPosition()
    {
        float xPos;
        float rowSpacing = 2.5f;
        if (StageController.Instance != null)
        {
            xPos = StageController.Instance.GetFormationOffset(columnIndex, rowIndex);
            rowSpacing = StageController.Instance.GetRowSpacing();
        }
        else
        {
            // 回退到原始直线排列
            xPos = (columnIndex - 2) * 2.0f;
        }
        float zPos = -(rowIndex * rowSpacing + moveProgress * rowSpacing); // 负Z为前进方向
        transform.position = new Vector3(xPos, 0f, zPos);
    }

    /// <summary>
    /// 更新透明度（基于排索引）
    /// </summary>
    private void UpdateAlpha()
    {
        if (renderers == null || renderers.Length == 0) return;

        float alpha = GetAlphaForRow(rowIndex);

        foreach (var renderer in renderers)
        {
            renderer.GetPropertyBlock(mpb);
            Color color = renderer.sharedMaterial.color;
            color.a = alpha;
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
