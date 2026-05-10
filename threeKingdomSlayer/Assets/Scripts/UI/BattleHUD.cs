using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 战斗HUD
/// 显示玩家生命值、连杀计数、当前波次、铜钱数量
/// </summary>
public class BattleHUD : MonoBehaviour
{
    [Header("生命值")]
    public Slider healthSlider;
    public TMP_Text healthText;

    [Header("复活次数")]
    public TMP_Text reviveText;

    [Header("连杀计数")]
    public TMP_Text killCountText;

    [Header("铜钱")]
    public TMP_Text coinText;

    [Header("波次")]
    public TMP_Text waveText;

    [Header("冷却指示器")]
    public Image stabCooldownImage;
    public Image slashCooldownImage;
    public Image pierceCooldownImage;
    public Image sweepCooldownImage;
    public Image launchCooldownImage;
    public Image parryCooldownImage;

    [Header("冷却充能指示器 (Radial 填充)")]
    [Tooltip("每个冷却图标下方的子 Image，ImageType=Filled/Radial360/Top。\n" +
             "fillAmount 从 0→1 对应冷却进度从 0%→100%，冷却完毕保持 fillAmount=1 表示技能就绪。")]
    public Image stabChargeFill;
    public Image slashChargeFill;
    public Image pierceChargeFill;
    public Image sweepChargeFill;
    public Image launchChargeFill;
    public Image parryChargeFill;

    [Header("关卡状态")]
    public GameObject victoryPanel;
    public GameObject defeatPanel;
    public TMP_Text resultCoinText;

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

    /// <summary>
    /// 更新冷却充能 Radial 填充指示器
    /// fillAmount 从 0→1（冷却进度0%→100%），冷却完毕保持 fillAmount=1 表示技能就绪
    /// </summary>
    private void UpdateCooldownFillUI()
    {
        if (PlayerState.Instance == null) return;
        UpdateChargeFill(stabChargeFill, AttackType.Stab);
        UpdateChargeFill(slashChargeFill, AttackType.Slash);
        UpdateChargeFill(pierceChargeFill, AttackType.Pierce);
        UpdateChargeFill(sweepChargeFill, AttackType.Sweep);
        UpdateChargeFill(launchChargeFill, AttackType.Launch);
        UpdateChargeFill(parryChargeFill, AttackType.Parry);
    }

    private void UpdateChargeFill(Image image, AttackType type)
    {
        if (image == null) return;
        float progress = PlayerState.Instance.GetCooldownProgress(type);
        // progress = timer/duration (1→0), 反向得到 0→1
        image.fillAmount = 1f - progress;
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

    #region 血量条颜色

    private Color healthBarDefaultColor;
    private bool healthBarColorSaved;

    /// <summary>
    /// 设置血量条填充颜色（狂怒大招期间变橙等）
    /// </summary>
    public void SetHealthBarColor(Color color)
    {
        if (healthSlider != null && healthSlider.fillRect != null)
        {
            var img = healthSlider.fillRect.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                if (!healthBarColorSaved)
                {
                    healthBarDefaultColor = img.color;
                    healthBarColorSaved = true;
                }
                img.color = color;
            }
        }
    }

    /// <summary>
    /// 恢复血量条默认颜色
    /// </summary>
    public void ResetHealthBarColor()
    {
        if (healthBarColorSaved)
            SetHealthBarColor(healthBarDefaultColor);
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
