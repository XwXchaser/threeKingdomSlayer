using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗HUD
/// 显示玩家生命值、连杀计数、当前波次、铜钱数量
/// </summary>
public class BattleHUD : MonoBehaviour
{
    [Header("生命值")]
    public Slider healthSlider;
    public Text healthText;

    [Header("复活次数")]
    public Text reviveText;

    [Header("连杀计数")]
    public Text killCountText;

    [Header("铜钱")]
    public Text coinText;

    [Header("波次")]
    public Text waveText;

    [Header("冷却指示器")]
    public Image stabCooldownImage;
    public Image slashCooldownImage;
    public Image pierceCooldownImage;
    public Image sweepCooldownImage;
    public Image launchCooldownImage;
    public Image parryCooldownImage;

    [Header("关卡状态")]
    public GameObject victoryPanel;
    public GameObject defeatPanel;
    public Text resultCoinText;

    private void Start()
    {
        // 注册事件
        if (PlayerState.Instance != null)
        {
            PlayerState.Instance.OnHealthChanged += UpdateHealth;
            PlayerState.Instance.OnReviveCountChanged += UpdateRevives;
            PlayerState.Instance.OnKillCountChanged += UpdateKillCount;
            PlayerState.Instance.OnCoinChanged += UpdateCoins;
            PlayerState.Instance.OnWaveChanged += UpdateWave;
            PlayerState.Instance.OnStageStateChanged += OnStageStateChanged;
        }

        // 初始隐藏结果面板
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);
    }

    private void Update()
    {
        // 每帧更新冷却指示器（仅在 PlayerState 就绪时执行）
        if (PlayerState.Instance != null && PlayerState.Instance.heroConfig != null)
        {
            UpdateCooldownUI();
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
            PlayerState.Instance.OnWaveChanged -= UpdateWave;
            PlayerState.Instance.OnStageStateChanged -= OnStageStateChanged;
        }
    }

    #region UI更新

    private void UpdateHealth(float current, float max)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = max;
            healthSlider.value = current;
        }
        if (healthText != null)
        {
            healthText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
        }
    }

    private void UpdateRevives(int count)
    {
        if (reviveText != null)
        {
            reviveText.text = $"复活: {count}";
            reviveText.gameObject.SetActive(count > 0);
        }
    }

    private void UpdateKillCount(int count)
    {
        if (killCountText != null)
        {
            killCountText.text = $"击杀: {count}";
        }
    }

    private void UpdateCoins(int count)
    {
        if (coinText != null)
        {
            coinText.text = $"铜钱: {count}";
        }
    }

    private void UpdateWave(int wave)
    {
        if (waveText != null)
        {
            int totalWaves = WaveSpawner.Instance != null ? WaveSpawner.Instance.TotalWaves : 0;
            waveText.text = $"波次: {wave}/{totalWaves}";
        }
    }

    private void UpdateCooldownUI()
    {
        if (PlayerState.Instance == null) return;

        UpdateCooldownImage(stabCooldownImage, AttackType.Stab);
        UpdateCooldownImage(slashCooldownImage, AttackType.Slash);
        UpdateCooldownImage(pierceCooldownImage, AttackType.Pierce);
        UpdateCooldownImage(sweepCooldownImage, AttackType.Sweep);
        UpdateCooldownImage(launchCooldownImage, AttackType.Launch);
        UpdateCooldownImage(parryCooldownImage, AttackType.Parry);
    }

    private void UpdateCooldownImage(Image image, AttackType type)
    {
        if (image == null) return;
        float progress = PlayerState.Instance.GetCooldownProgress(type);
        image.fillAmount = progress;
        // 冷却中显示红色，可用时显示绿色
        image.color = progress > 0f ? Color.red : Color.green;
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
            {
                resultCoinText.text = $"获得铜钱: {PlayerState.Instance.coinCount}";
            }
        }
    }

    private void ShowDefeat()
    {
        if (defeatPanel != null)
        {
            defeatPanel.SetActive(true);
            if (resultCoinText != null && PlayerState.Instance != null)
            {
                resultCoinText.text = $"获得铜钱: {PlayerState.Instance.coinCount}";
            }
        }
    }

    #endregion

    #region 按钮事件

    /// <summary>
    /// 重新开始按钮
    /// </summary>
    public void OnRestartButton()
    {
        StageController.Instance?.RestartStage();
    }

    /// <summary>
    /// 返回主菜单按钮
    /// </summary>
    public void OnMainMenuButton()
    {
        StageController.Instance?.GoToMainMenu();
    }

    #endregion
}
