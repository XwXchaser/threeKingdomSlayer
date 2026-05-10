using UnityEngine;

/// <summary>
/// 大招系统 - 单例
/// 管理大招充能、触发、效果执行
/// </summary>
public class UltimateSystem : MonoBehaviour
{
    public static UltimateSystem Instance { get; private set; }

    [Header("充能配置")]
    [Tooltip("大招充能上限")]
    public int maxUltimateEnergy = 100;
    [Tooltip("各攻击类型命中时获得的能量（顺序: Stab, Slash, Pierce, Sweep, Launch, Parry）")]
    public int[] energyGainPerHit = new int[] { 10, 10, 15, 12, 8, 5 };

    [Header("大招效果")]
    [Tooltip("大招效果预制体（需挂载 UltimateEffect 子类组件）")]
    public GameObject ultimateEffectPrefab;

    [Header("运行时状态")]
    [SerializeField] private int currentEnergy;

    // 事件
    public System.Action<float> OnEnergyChanged;   // percent: 0~1
    public System.Action OnUltimateReady;           // 充能满
    public System.Action OnUltimateActivated;       // 大招已释放

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
    /// </summary>
    public void AddEnergyForAttack(AttackType attackType)
    {
        int index = (int)attackType;
        int gain = 0;
        if (energyGainPerHit != null && index < energyGainPerHit.Length)
            gain = energyGainPerHit[index];

        if (gain <= 0) return;
        AddEnergy(gain);
    }

    /// <summary>
    /// 直接增加指定能量值
    /// </summary>
    public void AddEnergy(int amount)
    {
        if (amount <= 0) return;
        if (IsReady) return; // 已充满，不再增加

        int prevEnergy = currentEnergy;
        currentEnergy = Mathf.Min(currentEnergy + amount, maxUltimateEnergy);

        float percent = EnergyPercent;
        OnEnergyChanged?.Invoke(percent);

        if (!IsReady) return;

        OnUltimateReady?.Invoke();
        Debug.Log($"[UltimateSystem] 大招充能完毕！");
    }

    /// <summary>
    /// 重置充能（关卡开始时调用）
    /// </summary>
    public void ResetEnergy()
    {
        currentEnergy = 0;
        OnEnergyChanged?.Invoke(0f);
    }

    /// <summary>
    /// 激活大招（由 UI 按钮点击调用）
    /// </summary>
    public void ActivateUltimate()
    {
        if (!IsReady)
        {
            Debug.LogWarning("[UltimateSystem] 大招未充能完毕，无法激活");
            return;
        }

        // 消耗全部充能
        currentEnergy = 0;
        OnEnergyChanged?.Invoke(0f);
        OnUltimateActivated?.Invoke();

        Debug.Log("[UltimateSystem] 大招激活！");

        // 执行效果
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

            // 播放完成后销毁
            float lifetime = effect != null ? effect.GetLifetime() : 2f;
            Destroy(effectInstance, lifetime);
        }
    }
}
