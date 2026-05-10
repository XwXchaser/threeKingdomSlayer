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

    [Header("运行时状态")]
    [SerializeField] private int currentEnergy;

    // 事件
    public System.Action<float> OnEnergyChanged;
    public System.Action OnUltimateReady;
    public System.Action OnUltimateActivated;

    public float EnergyPercent => maxUltimateEnergy > 0 ? (float)currentEnergy / maxUltimateEnergy : 0f;
    public bool IsReady => currentEnergy >= maxUltimateEnergy;
    public int CurrentEnergy => currentEnergy;

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
        if (!IsReady)
        {
            Debug.LogWarning("[UltimateSystem] 大招未充能完毕，无法激活");
            return;
        }

        currentEnergy = 0;
        OnEnergyChanged?.Invoke(0f);
        OnUltimateActivated?.Invoke();

        Debug.Log("[UltimateSystem] 大招激活！");

        if (ultimateEffectPrefab != null)
        {
            var effectInstance = Instantiate(ultimateEffectPrefab);
            var effect = effectInstance.GetComponent<UltimateEffect>();
            if (effect != null)
            {
                effect.Execute();
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
