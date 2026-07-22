using UnityEngine;

/// <summary>
/// 大招系统 - 单例
/// 管理大招充能、触发、效果执行
/// 能量获取数值从 HeroConfig 的技能配置中读取
/// </summary>
public class UltimateSystem : MonoBehaviour
{
    public static UltimateSystem Instance { get; private set; }

    [Header("充能配置")]
    [Tooltip("大招充能上限")]
    public int maxUltimateEnergy = 100;

    [Header("大招效果")]
    [Tooltip("大招效果预制体（需挂载 UltimateEffect 子类组件）")]
    public GameObject ultimateEffectPrefab;

    [System.NonSerialized] private int currentEnergy;

    // 事件
    public System.Action<float> OnEnergyChanged;
    public System.Action OnUltimateReady;
    public System.Action OnUltimateActivated;

    public float EnergyPercent => maxUltimateEnergy > 0 ? (float)currentEnergy / maxUltimateEnergy : 0f;
    public bool IsReady => currentEnergy >= EnergyCost;
    public int CurrentEnergy => currentEnergy;

    private int EnergyCost
    {
        get
        {
            var cfg = PlayerState.Instance?.heroConfig?.ultimateSkillConfig;
            return cfg != null ? cfg.energyCost : maxUltimateEnergy;
        }
    }

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
    }

    private void Start()
    {
        ResetEnergy();
    }

    /// <summary>
    /// 根据攻击类型增加能量（由 AttackSystem 在命中时调用）
    /// 能量值从当前武将的技能配置中读取
    /// </summary>
    public void AddEnergyForAttack(AttackType attackType)
    {
        var cfg = PlayerState.Instance?.heroConfig?.GetSkillConfig(attackType);
        if (cfg == null) return;
        AddEnergy(cfg.ultimateEnergyGain);
    }

    public void AddEnergyForKill(bool isBoss)
    {
        var hero = PlayerState.Instance?.heroConfig;
        if (hero == null) return;
        AddEnergy(isBoss ? hero.ultimateEnergyPerBossKill : hero.ultimateEnergyPerEnemyKill);
    }

    /// <summary>
    /// 直接增加指定能量值
    /// </summary>
    public void AddEnergy(int amount)
    {
        if (amount <= 0) return;
        if (IsReady) return;

        currentEnergy = Mathf.Min(currentEnergy + amount, maxUltimateEnergy);

        float percent = EnergyPercent;
        OnEnergyChanged?.Invoke(percent);

        if (!IsReady) return;

        OnUltimateReady?.Invoke();
        Debug.Log($"[UltimateSystem] 大招充能完毕！");
    }

    public void ResetEnergy()
    {
        currentEnergy = 0;
        OnEnergyChanged?.Invoke(0f);
    }

    public void ActivateUltimate()
    {
        var qte = FindObjectOfType<QTEController>();
        if (qte != null && qte.IsStrictInputActive)
            return;

        if (!IsReady)
        {
            Debug.LogWarning("[UltimateSystem] 大招未充能完毕，无法激活");
            return;
        }

        var playerState = PlayerState.Instance;
        if (playerState != null && !playerState.IsAttackReady(AttackType.Ultimate))
        {
            Debug.LogWarning("[UltimateSystem] 大招冷却中");
            return;
        }

        currentEnergy = Mathf.Max(0, currentEnergy - EnergyCost);
        OnEnergyChanged?.Invoke(EnergyPercent);
        OnUltimateActivated?.Invoke();

        if (playerState != null)
            playerState.StartCooldown(AttackType.Ultimate);

        Debug.Log("[UltimateSystem] 大招激活！");

        if (ultimateEffectPrefab != null)
        {
            var effectInstance = Instantiate(ultimateEffectPrefab);
            var effect = effectInstance.GetComponent<UltimateEffect>();
            if (effect != null)
            {
                effect.Execute();
                // Handheld.Vibrate(); // 安卓端攻击震动暂关闭
            }
            else
            {
                Debug.LogWarning("[UltimateSystem] ultimateEffectPrefab 未挂载 UltimateEffect 组件");
            }

            float lifetime = effect != null ? effect.GetLifetime() : 2f;
            Destroy(effectInstance, lifetime);
        }
    }
}
