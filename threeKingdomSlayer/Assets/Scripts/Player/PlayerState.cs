using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 关卡状态枚举
/// </summary>
public enum StageState
{
    None,
    Starting,
    InProgress,
    Victory,
    Defeat
}

/// <summary>
/// 玩家状态 - 单例
/// 管理玩家属性（生命值、复活次数、6种攻击属性等）
/// 处理玩家受伤和复活逻辑
/// </summary>
public class PlayerState : MonoBehaviour
{
    public static PlayerState Instance { get; private set; }

    [Header("武将配置")]
    public HeroConfig heroConfig;

    [Header("升级系统")]
    public ExpCurveConfig expCurveConfig;

    [System.NonSerialized] public float currentHealth;
    [System.NonSerialized] public int currentRevives;
    [System.NonSerialized] public int killCount;
    [System.NonSerialized] public int coinCount;
    [System.NonSerialized] public int currentWave;
    [System.NonSerialized] public StageState stageState = StageState.None;

    // 升级系统运行时
    [System.NonSerialized] public int currentLevel;
    [System.NonSerialized] public float currentExp;
    [System.NonSerialized] public List<UpgradeAcquired> acquiredUpgrades = new List<UpgradeAcquired>();

    // 冷却计时器（按攻击类型索引）
    private Dictionary<AttackType, float> cooldownTimers = new Dictionary<AttackType, float>();

    // Buff 系统
    private Dictionary<BuffType, float> buffTimers = new Dictionary<BuffType, float>();

    // GC 优化：复用容器，避免每帧 new
    private readonly List<AttackType> _cooldownKeysCache = new List<AttackType>();
    private readonly List<BuffType> _expiredBuffsCache = new List<BuffType>();

    // 减伤Buff
    private float damageReductionPercent;
    private float damageReductionTimer;

    // 蓄力减伤（由 UpgradeEffectManager 提供数值，InputManager.OnChargeBegan/Ended 控制开关）
    private bool _isCharging;
    /// <summary>玩家当前是否处于蓄力状态（供 TimedPassiveModule / AttackSystem 查询）</summary>
    public bool IsCharging => _isCharging;

    // 无敌标记（狂怒大招等）
    [System.NonSerialized] public bool isInvincible;

    // 事件
    public System.Action<float, float> OnHealthChanged; // current, max
    public System.Action<int> OnReviveCountChanged;
    public System.Action<int> OnKillCountChanged;
    public System.Action<int> OnCoinChanged;
    public System.Action<int, int> OnCoinGained; // (amount gained, total coins)
    public System.Action<int> OnWaveChanged;
    public System.Action<StageState> OnStageStateChanged;
    public System.Action OnPlayerDied;
    public System.Action<int> OnComboChanged;
    public System.Action<string> OnComboTrigger;
    public System.Action<float, float> OnExpChanged;     // (currentExp, requiredExp)
    public System.Action<int> OnLevelUp;                  // (newLevel)

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        // 蓄力减伤：取消订阅
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnChargeBegan -= OnChargeBegan;
            InputManager.Instance.OnChargeEnded -= OnChargeEnded;
        }
    }

    private void Start()
    {
        if (heroConfig == null)
        {
            Debug.LogError("[PlayerState] heroConfig 未赋值！将使用默认值运行，部分功能可能受限");
            // 不 return，允许游戏在无配置时继续运行（使用默认值）
        }

        // 蓄力减伤：订阅 InputManager 的蓄力事件
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnChargeBegan += OnChargeBegan;
            InputManager.Instance.OnChargeEnded += OnChargeEnded;
        }

        ResetPlayer();
    }

    private void Update()
    {
        // 更新冷却计时器（复用缓存列表，避免每帧 new List 分配）
        _cooldownKeysCache.Clear();
        _cooldownKeysCache.AddRange(cooldownTimers.Keys);
        for (int i = 0; i < _cooldownKeysCache.Count; i++)
        {
            var type = _cooldownKeysCache[i];
            if (cooldownTimers[type] > 0)
                cooldownTimers[type] -= Time.deltaTime;
        }

        if (damageReductionTimer > 0) damageReductionTimer -= Time.deltaTime;

        // 更新 Buff 计时器（复用缓存列表）
        _expiredBuffsCache.Clear();
        foreach (var kv in buffTimers)
        {
            if (kv.Value > 0f)
            {
                buffTimers[kv.Key] -= Time.deltaTime;
                if (buffTimers[kv.Key] <= 0f)
                    _expiredBuffsCache.Add(kv.Key);
            }
        }
        foreach (var t in _expiredBuffsCache)
            buffTimers.Remove(t);
    }

    /// <summary>
    /// 重置玩家状态（关卡开始时调用）
    /// heroConfig 为 null 时使用默认值，确保不报错
    /// </summary>
    public void ResetPlayer()
    {
        // heroConfig 为 null 时使用安全默认值
        currentHealth = heroConfig != null ? heroConfig.maxHealth : 100f;
        currentRevives = heroConfig != null ? heroConfig.reviveCount : 0;
        killCount = 0;
        coinCount = 0;
        currentWave = 0;
        stageState = StageState.Starting;

        currentLevel = 0;
        currentExp = 0f;
        acquiredUpgrades.Clear();
        UpgradeEffectManager.Instance?.ResetAll();

        _isCharging = false;

        cooldownTimers.Clear();
        damageReductionPercent = 0f;
        damageReductionTimer = 0f;
        buffTimers.Clear();

        float maxHp = heroConfig != null ? heroConfig.maxHealth : 100f;
        OnHealthChanged?.Invoke(currentHealth, maxHp);
        OnReviveCountChanged?.Invoke(currentRevives);
        OnKillCountChanged?.Invoke(0);
        OnCoinChanged?.Invoke(0);
        OnWaveChanged?.Invoke(0);
    }

    #region 伤害系统

    /// <summary>
    /// 玩家受到伤害
    /// </summary>
    public void TakeDamage(float damage, Enemy source = null)
    {
        if (stageState == StageState.Defeat || stageState == StageState.Victory) return;
        if (isInvincible) return;

        float originalDamage = damage;

        // 反伤盾：蓄力时受到伤害且来源有效 → 基于原始伤害反弹，先于减伤
        if (_isCharging && source != null && UpgradeEffectManager.Instance != null)
        {
            float reflectPercent = UpgradeEffectManager.Instance.TryConsumeReflectShield();
            if (reflectPercent > 0f)
            {
                float reflectDamage = originalDamage * reflectPercent;
                source.TakeDamage(reflectDamage, damageNumberColor: new Color(0.7f, 0.2f, 1f));
                DebugLog.Info($"[PlayerState] 反伤盾触发: 反弹 {reflectDamage:F0} 伤害 (原始 {originalDamage:F0} × {reflectPercent:P0})");
            }
        }

        float finalDamage = originalDamage;
        if (damageReductionTimer > 0f)
        {
            finalDamage = originalDamage * (1f - damageReductionPercent);
        }

        // 蓄力减伤（与招架减伤乘法叠加）
        if (_isCharging && UpgradeEffectManager.Instance != null)
        {
            float chargeReduction = UpgradeEffectManager.Instance.GetChargeDamageReduction();
            if (chargeReduction > 0f)
                finalDamage *= (1f - chargeReduction);
        }

        currentHealth -= finalDamage;
        OnHealthChanged?.Invoke(currentHealth, heroConfig != null ? heroConfig.maxHealth : 500f);
        // 受击震动已暂时关闭（安卓端不合适），后续在其他功能情景中重新启用
        // Handheld.Vibrate();

        Debug.Log($"[PlayerState] 受到伤害: {originalDamage}(原始) -> {finalDamage}(最终), 剩余生命: {currentHealth}");

        if (currentHealth <= 0f)
        {
            HandleDeath();
        }
    }

    /// <summary>
    /// 回复生命值（不超过最大值）
    /// </summary>
    public void Heal(float amount)
    {
        if (stageState == StageState.Defeat || stageState == StageState.Victory) return;
        float maxHp = heroConfig != null ? heroConfig.maxHealth : 100f;
        currentHealth = Mathf.Min(currentHealth + amount, maxHp);
        OnHealthChanged?.Invoke(currentHealth, maxHp);
    }

    /// <summary>
    /// 处理玩家死亡
    /// </summary>
    private void HandleDeath()
    {
        if (currentRevives > 0)
        {
            // 复活
            currentRevives--;
            // BUG FIX: heroConfig null 保护，避免 Scene 重载时序问题或配置缺失导致 NPE。
            // 如果 heroConfig 为 null，使用硬编码默认值确保复活逻辑不崩溃。
            if (heroConfig == null)
            {
                Debug.LogError("[PlayerState] HandleDeath: heroConfig 为 null，使用默认值");
                currentHealth = 100f * 0.5f; // 默认 maxHealth=100, reviveHealthPercent=50%
            }
            else
            {
                currentHealth = heroConfig.maxHealth * heroConfig.reviveHealthPercent;
            }
            OnHealthChanged?.Invoke(currentHealth, heroConfig != null ? heroConfig.maxHealth : 100f);
            OnReviveCountChanged?.Invoke(currentRevives);
            Debug.Log($"[PlayerState] 复活！剩余复活次数: {currentRevives}, 生命值: {currentHealth}");
        }
        else
        {
            // 真正死亡
            stageState = StageState.Defeat;
            OnStageStateChanged?.Invoke(StageState.Defeat);
            OnPlayerDied?.Invoke();
            Debug.Log("[PlayerState] 玩家阵亡，游戏结束");
        }
    }

    #endregion

    #region 攻击冷却

    /// <summary>
    /// 检查攻击是否可用
    /// </summary>
    public bool IsAttackReady(AttackType attackType)
    {
        return !cooldownTimers.ContainsKey(attackType) || cooldownTimers[attackType] <= 0f;
    }

    /// <summary>
    /// 触发攻击冷却
    /// </summary>
    public void StartCooldown(AttackType attackType)
    {
        float cooldown = GetCooldownDuration(attackType);
        cooldownTimers[attackType] = cooldown;
    }

    /// <summary>
    /// 获取攻击冷却进度（0~1, 0=可用）
    /// </summary>
    public float GetCooldownProgress(AttackType attackType)
    {
        float timer = cooldownTimers.ContainsKey(attackType) ? cooldownTimers[attackType] : 0f;
        float duration = GetCooldownDuration(attackType);
        if (duration <= 0f) return 0f;
        return Mathf.Clamp01(timer / duration);
    }

    private float GetCooldownDuration(AttackType type)
    {
        if (heroConfig == null) return 1f;
        if (type == AttackType.Ultimate)
        {
            return heroConfig.ultimateSkillConfig != null ? heroConfig.ultimateSkillConfig.cooldown : 5f;
        }
        var cfg = heroConfig.GetSkillConfig(type);
        return cfg != null ? cfg.cooldown : 1f;
    }

    #endregion

    #region 升级系统

    /// <summary>
    /// 获取指定升级的当前等级（0=未获得）
    /// </summary>
    public int GetUpgradeLevel(string upgradeId)
    {
        for (int i = 0; i < acquiredUpgrades.Count; i++)
        {
            if (acquiredUpgrades[i].definition.upgradeId == upgradeId)
                return acquiredUpgrades[i].currentLevel;
        }
        return 0;
    }

    /// <summary>
    /// 获取升级到下一级所需经验，返回-1表示已满级
    /// </summary>
    public int GetExpRequiredForNextLevel()
    {
        if (expCurveConfig == null || expCurveConfig.expRequiredPerLevel == null) return -1;
        if (currentLevel >= expCurveConfig.expRequiredPerLevel.Count) return -1;
        return expCurveConfig.expRequiredPerLevel[currentLevel];
    }

    /// <summary>
    /// 增加经验值，返回实际触发的升级次数
    /// </summary>
    public int AddExp(float amount)
    {
        if (stageState != StageState.InProgress) return 0;
        if (expCurveConfig == null) return 0;

        currentExp += amount;
        int levelUps = 0;

        while (true)
        {
            int required = GetExpRequiredForNextLevel();
            if (required < 0) break;
            if (currentExp < required) break;

            currentExp -= required;
            currentLevel++;
            levelUps++;
            OnLevelUp?.Invoke(currentLevel);
        }

        int nextReq = GetExpRequiredForNextLevel();
        float displayReq = nextReq > 0 ? nextReq : currentExp;
        OnExpChanged?.Invoke(currentExp, displayReq);

        return levelUps;
    }

    #endregion

    #region 统计

    /// <summary>
    /// 增加击杀数
    /// </summary>
    public void AddKill()
    {
        killCount++;
        OnKillCountChanged?.Invoke(killCount);
    }

    /// <summary>
    /// 增加铜钱
    /// </summary>
    public void AddCoins(int amount)
    {
        coinCount += amount;
        OnCoinChanged?.Invoke(coinCount);
        if (amount > 0) OnCoinGained?.Invoke(amount, coinCount);
    }

    /// <summary>
    /// 获取本局获得指定道具的数量（当前仅支持 Coin）
    /// </summary>
    public int GetSessionProp(PropType type)
    {
        if (type == PropType.Coin) return coinCount;
        return 0;
    }

    /// <summary>
    /// 设置当前波次
    /// </summary>
    public void SetCurrentWave(int wave)
    {
        currentWave = wave;
        OnWaveChanged?.Invoke(wave);
    }

    /// <summary>
    /// 设置关卡状态
    /// </summary>
    public void SetStageState(StageState state)
    {
        stageState = state;
        OnStageStateChanged?.Invoke(state);
    }

    #endregion

    #region Buff 系统

    /// <summary>
    /// 检查是否拥有指定 Buff
    /// </summary>
    public bool HasBuff(BuffType type)
    {
        return buffTimers.ContainsKey(type);
    }

    /// <summary>
    /// 应用 Buff（duration=0 表示永久）
    /// </summary>
    public void ApplyBuff(BuffType type, float duration = 0f)
    {
        buffTimers[type] = duration;
    }

    /// <summary>
    /// 移除 Buff
    /// </summary>
    public void RemoveBuff(BuffType type)
    {
        buffTimers.Remove(type);
    }

    #endregion

    #region 减伤Buff

    /// <summary>
    /// 应用减伤Buff（招架成功后调用）
    /// </summary>
    public void ApplyDamageReduction(float percent, float duration)
    {
        damageReductionPercent = percent;
        damageReductionTimer = duration;
    }

    #endregion

    #region 蓄力减伤事件

    private void OnChargeBegan(Vector2 pos)
    {
        _isCharging = true;
    }

    private void OnChargeEnded()
    {
        _isCharging = false;
    }

    #endregion
}

/// <summary>
/// Buff 类型枚举
/// </summary>
public enum BuffType
{
    ForceLaunch,       // 强制击飞：CanBeLaunched 始终返回 true
    ProbabilityLaunch  // 概率击飞：攻击时按概率强制击飞
}

/// <summary>
/// 已获得的升级记录
/// </summary>
[System.Serializable]
public class UpgradeAcquired
{
    public UpgradeDefinition definition;
    public int currentLevel;
}

/// <summary>
/// 攻击类型枚举
/// </summary>
public enum AttackType
{
    Stab,     // 戳击
    Slash,    // 斩击
    Pierce,   // 穿刺
    Sweep,    // 横扫
    Launch,   // 挑飞
    Parry,    // 招架
    Ultimate  // 大招
}
