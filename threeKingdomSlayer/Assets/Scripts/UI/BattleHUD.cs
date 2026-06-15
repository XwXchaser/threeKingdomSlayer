using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 战斗HUD — 管理全局UI（波次、铜钱、击杀、Boss血条、胜负面板）。
/// 英雄专属UI已提取到 HeroHUD Prefab，通过 HeroConfig.heroHUDPrefab 实例化。
/// </summary>
public class BattleHUD : MonoBehaviour
{
    [Header("英雄 HUD（运行时实例化）")]
    [Tooltip("英雄 HUD 的父容器 Transform（通常为 BattleHUD Canvas 下的空节点）")]
    public Transform heroHUDParent;

    [Header("连杀计数")]
    public TMP_Text killCountText;

    [Header("铜钱")]
    public TMP_Text coinText;

    // GC 优化：缓存所有攻击类型数组，避免每帧 new[]
    private static readonly AttackType[] AllAttackTypes = { AttackType.Stab, AttackType.Slash, AttackType.Pierce, AttackType.Sweep, AttackType.Launch, AttackType.Parry };

    [Header("Boss 血条")]
    [Tooltip("BossHealthBar 模板 Prefab（BattleHUD 动态实例化）")]
    public GameObject bossHealthBarPrefab;
    [Tooltip("Boss 血条的父容器 Transform")]
    public Transform bossBarsParent;
    [Tooltip("同时显示 Boss 血条的最大数量")]
    public int maxBossBars = 5;

    [Header("连击UI")]
    public ComboUI comboUI;

    [Header("关卡状态")]
    public GameObject victoryPanel;
    public GameObject defeatPanel;
    public TMP_Text resultCoinText;
    [Tooltip("通关印章 Prefab（victory.prefab），世界空间 SpriteRenderer")]
    public GameObject victoryStampPrefab;

    // 运行时实例化的英雄 HUD
    private HeroHUD _heroHUD;

    // Boss 血条池
    private List<BossHealthUI> _activeBossBars = new List<BossHealthUI>();
    private bool _bossEventSubscribed;

    private void Awake()
    {
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.OnBossEngaged += OnBossEngaged;
            _bossEventSubscribed = true;
        }
    }

    private void Start()
    {
        // 实例化英雄 HUD
        InstantiateHeroHUD();

        // 注册 PlayerState 事件
        if (PlayerState.Instance != null)
        {
            PlayerState.Instance.OnHealthChanged += UpdateHealth;
            PlayerState.Instance.OnReviveCountChanged += UpdateRevives;
            PlayerState.Instance.OnKillCountChanged += UpdateKillCount;
            PlayerState.Instance.OnCoinChanged += UpdateCoins;
            PlayerState.Instance.OnStageStateChanged += OnStageStateChanged;
            PlayerState.Instance.OnExpChanged += UpdateExpBar;
            PlayerState.Instance.OnLevelUp += UpdateExpLevel;
        }

        if (!_bossEventSubscribed && EnemyManager.Instance != null)
        {
            EnemyManager.Instance.OnBossEngaged += OnBossEngaged;
            _bossEventSubscribed = true;
        }

        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);
    }

    private void InstantiateHeroHUD()
    {
        if (PlayerState.Instance == null || PlayerState.Instance.heroConfig == null)
        {
            Debug.LogError("[BattleHUD] 无法实例化 HeroHUD：PlayerState 或 heroConfig 为空");
            return;
        }

        var prefab = PlayerState.Instance.heroConfig.heroHUDPrefab;
        if (prefab == null)
        {
            Debug.LogWarning("[BattleHUD] heroHUDPrefab 未配置，跳过 HeroHUD 实例化");
            return;
        }

        var parent = heroHUDParent != null ? heroHUDParent : transform;
        var go = Instantiate(prefab, parent);
        _heroHUD = go.GetComponent<HeroHUD>();

        if (_heroHUD == null)
            Debug.LogError("[BattleHUD] heroHUDPrefab 上未找到 HeroHUD 组件");

        // 将 QTE frame 注入 QTEDisplay（老虎机动画区域）
        if (_heroHUD.qteIndicatorArea != null)
        {
            var qteDisplay = FindObjectOfType<QTEDisplay>();
            if (qteDisplay != null)
            {
                qteDisplay.qteFrameRect = _heroHUD.qteFrameRect;
                qteDisplay.qteIndicatorArea = _heroHUD.qteIndicatorArea;
            }
        }

        // 将 ExpBar Slider 和 Canvas 引用传给 ExpGemManager 和 HealthPotionManager
        if (ExpGemManager.Instance != null)
        {
            ExpGemManager.Instance.expSlider = _heroHUD.expSlider;
            ExpGemManager.Instance.gemParent = (RectTransform)transform;
        }
        if (HealthPotionManager.Instance != null)
        {
            HealthPotionManager.Instance.gemParent = (RectTransform)transform;
        }

        // 注入依赖到 StageProgressBar（避免时序问题）
        if (_heroHUD.stageProgressBar != null)
        {
            var sc = StageController.Instance?.stageConfig;
            var ws = WaveSpawner.Instance;
            Debug.Log("[BattleHUD] StageProgressBar 注入: stageConfig=" + (sc != null ? sc.name : "NULL") + " waveSpawner=" + (ws != null ? ws.name : "NULL"));
            if (sc != null && ws != null)
                _heroHUD.stageProgressBar.Initialize(sc, ws);
            else
                Debug.LogWarning("[BattleHUD] StageProgressBar 注入失败：sc或ws为null");
        }
        else
        {
            Debug.LogWarning("[BattleHUD] _heroHUD.stageProgressBar 为 null！");
        }
    }

    private void Update()
    {
        if (!_bossEventSubscribed && EnemyManager.Instance != null)
        {
            EnemyManager.Instance.OnBossEngaged += OnBossEngaged;
            _bossEventSubscribed = true;
        }

        // 冷却 UI 委托给 HeroHUD
        if (_heroHUD != null && PlayerState.Instance != null && PlayerState.Instance.heroConfig != null)
        {
            UpdateCooldownUI();
            UpdateCooldownFillUI();
        }
    }

    private void OnDestroy()
    {
        if (PlayerState.Instance != null)
        {
            PlayerState.Instance.OnHealthChanged -= UpdateHealth;
            PlayerState.Instance.OnReviveCountChanged -= UpdateRevives;
            PlayerState.Instance.OnKillCountChanged -= UpdateKillCount;
            PlayerState.Instance.OnCoinChanged -= UpdateCoins;
            PlayerState.Instance.OnStageStateChanged -= OnStageStateChanged;
            PlayerState.Instance.OnExpChanged -= UpdateExpBar;
            PlayerState.Instance.OnLevelUp -= UpdateExpLevel;
        }

        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.OnBossEngaged -= OnBossEngaged;
        }
    }

    #region UI更新

    private void UpdateHealth(float current, float max)
    {
        _heroHUD?.SetHealth(current, max);
    }

    private void UpdateRevives(int count)
    {
        _heroHUD?.SetReviveCount(count);
    }

    private void UpdateKillCount(int count)
    {
        if (killCountText != null)
            killCountText.text = $"击杀: {count}";
    }

    private void UpdateCoins(int count)
    {
        if (coinText != null)
            coinText.text = $"铜钱: {count}";
    }

    private void UpdateExpBar(float currentExp, float requiredExp)
    {
        if (_heroHUD != null && _heroHUD.expSlider != null)
        {
            _heroHUD.expSlider.maxValue = requiredExp > 0 ? requiredExp : 1f;
            _heroHUD.expSlider.value = Mathf.Min(currentExp, _heroHUD.expSlider.maxValue);
        }
    }

    private void UpdateExpLevel(int level)
    {
        if (_heroHUD != null && _heroHUD.expLevelText != null)
            _heroHUD.expLevelText.text = $"Lv.{level}";
    }

    private void UpdateCooldownUI()
    {
        if (_heroHUD == null || PlayerState.Instance == null) return;
        foreach (var t in AllAttackTypes)
            _heroHUD.SetCooldown(t, PlayerState.Instance.GetCooldownProgress(t));
    }

    private void UpdateCooldownFillUI()
    {
        if (_heroHUD == null || PlayerState.Instance == null) return;
        foreach (var t in AllAttackTypes)
            _heroHUD.SetChargeFill(t, 1f - PlayerState.Instance.GetCooldownProgress(t));
    }

    private void OnStageStateChanged(StageState state)
    {
        switch (state)
        {
            case StageState.Victory:
                ShowVictory();
                break;
            case StageState.Defeat:
                ShowDefeat();
                break;
        }
    }

    #endregion

    #region 结果面板

    private void ShowVictory()
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
            if (resultCoinText != null && PlayerState.Instance != null)
                resultCoinText.text = $"获得铜钱: {PlayerState.Instance.coinCount}";
        }

        PlayVictoryStamp();
    }

    /// <summary>
    /// 通关印章动画 — victory1 先砸下，victory2 随后砸下（OutBack 过冲回弹）
    /// </summary>
    private void PlayVictoryStamp()
    {
        if (victoryStampPrefab == null) return;

        var cam = Camera.main;
        if (cam == null) return;

        var stamp = Instantiate(victoryStampPrefab, cam.transform);
        stamp.transform.localPosition = new Vector3(0f, 0f, 8f);
        stamp.transform.localRotation = Quaternion.identity;

        var victory1 = stamp.transform.Find("victory1");
        var victory2 = stamp.transform.Find("victory2");

        // 记录原始缩放，初始设为 3x
        Vector3 origScale1 = Vector3.one, origScale2 = Vector3.one;
        SpriteRenderer sr1 = null, sr2 = null;

        if (victory1 != null)
        {
            sr1 = victory1.GetComponent<SpriteRenderer>();
            origScale1 = victory1.localScale;
            victory1.localScale = origScale1 * 3f;
            if (sr1 != null) { var c = sr1.color; c.a = 0f; sr1.color = c; }
        }
        if (victory2 != null)
        {
            sr2 = victory2.GetComponent<SpriteRenderer>();
            origScale2 = victory2.localScale;
            victory2.localScale = origScale2 * 3f;
            if (sr2 != null) { var c = sr2.color; c.a = 0f; sr2.color = c; }
        }

        var seq = DOTween.Sequence();
        seq.SetUpdate(true); // 无视 timeScale 暂停

        // victory1 印章砸下
        if (victory1 != null && sr1 != null)
        {
            seq.AppendCallback(() =>
            {
                var c = sr1.color; c.a = 1f; sr1.color = c;
            });
            seq.Append(victory1.DOScale(origScale1, 0.4f).SetEase(Ease.OutBack));
        }

        seq.AppendInterval(0.2f);

        // victory2 印章砸下
        if (victory2 != null && sr2 != null)
        {
            seq.AppendCallback(() =>
            {
                var c = sr2.color; c.a = 1f; sr2.color = c;
            });
            seq.Append(victory2.DOScale(origScale2, 0.4f).SetEase(Ease.OutBack));
        }
    }

    private void ShowDefeat()
    {
        if (defeatPanel != null)
        {
            defeatPanel.SetActive(true);
            if (resultCoinText != null && PlayerState.Instance != null)
                resultCoinText.text = $"获得铜钱: {PlayerState.Instance.coinCount}";
        }
    }

    #endregion

    #region 血量条颜色（委托给 HeroHUD）

    public void SetHealthBarColor(Color color)
    {
        _heroHUD?.SetHealthBarColor(color);
    }

    public void ResetHealthBarColor()
    {
        _heroHUD?.ResetHealthBarColor();
    }

    #endregion

    #region Boss 血条管理

    private void OnBossEngaged(Enemy boss)
    {
        if (bossBarsParent == null)
        {
            Debug.LogError("[BattleHUD] bossBarsParent 未赋值！");
            return;
        }

        if (_activeBossBars.Count >= maxBossBars) return;

        foreach (var bar in _activeBossBars)
        {
            if (bar != null && bar.BoundBoss == boss)
                return;
        }

        // 如果 Boss 自身挂载了 bossHealthBarPrefab，优先使用
        var bossPrefab = boss.bossHealthBarPrefab != null ? boss.bossHealthBarPrefab : bossHealthBarPrefab;
        if (bossPrefab == null)
        {
            Debug.LogError($"[BattleHUD] Boss #{boss.enemyId} 的 bossHealthBarPrefab 未赋值！（BattleHUD 和 Enemy 上均为空）");
            return;
        }
        var go = Instantiate(bossPrefab, bossBarsParent);
        var ui = go.GetComponent<BossHealthUI>();
        if (ui != null)
        {
            _activeBossBars.Add(ui);
            ui.Bind(boss);
        }
    }

    #endregion

    #region 按钮事件

    public void OnRestartButton()
    {
        StageController.Instance?.RestartStage();
    }

    public void OnMainMenuButton()
    {
        if (StageController.Instance != null)
            StageController.Instance.GoToMainMenu();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    #endregion
}
