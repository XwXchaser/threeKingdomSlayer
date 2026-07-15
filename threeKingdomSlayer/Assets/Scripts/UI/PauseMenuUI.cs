using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// 暂停菜单UI — 暂停按钮 + 暂停弹窗（继续/返回主菜单 + 当前结算奖励预览）
/// 暂停时 Time.timeScale = 0，保持 UI 可交互
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    [Header("暂停按钮")]
    public UnityEngine.UI.Button pauseButton;

    [Header("暂停菜单面板")]
    public GameObject pausePanel;
    public UnityEngine.UI.Button continueButton;
    public UnityEngine.UI.Button mainMenuButton;

    [Header("音量")]
    public UnityEngine.UI.Slider volumeSlider;

    [Header("结算信息")]
    public TMP_Text coinEarnedText;
    public TMP_Text killCountText;
    public TMP_Text milestoneText;

    private bool _isPaused;

    private void Start()
    {
        if (pauseButton != null)
            pauseButton.onClick.AddListener(OnPauseClicked);
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value = AudioManager.Instance != null
                ? AudioManager.Instance.GetMasterVolume()
                : 1f;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
    }

    private void OnDestroy()
    {
        if (pauseButton != null) pauseButton.onClick.RemoveListener(OnPauseClicked);
        if (continueButton != null) continueButton.onClick.RemoveListener(OnContinueClicked);
        if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
        if (volumeSlider != null) volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
    }

    private void OnPauseClicked()
    {
        var sc = StageController.Instance;
        if (sc == null || !sc.IsStageInProgress) return;

        _isPaused = true;
        // 先显示面板再暂停（SetActive 必须在 timeScale=0 之前，且不能放在可能抛异常的代码之后）
        if (pausePanel != null) pausePanel.SetActive(true);

        // 打开面板时同步音量滑动条到当前数据层值
        if (volumeSlider != null && AudioManager.Instance != null)
            volumeSlider.value = AudioManager.Instance.GetMasterVolume();
        
        try { RefreshSettlementInfo(); }
        catch (System.Exception e) { Debug.LogWarning($"[PauseMenuUI] RefreshSettlementInfo 异常: {e.Message}"); }
        
        Time.timeScale = 0f;
    }

    private void OnContinueClicked()
    {
        _isPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    private void OnMainMenuClicked()
    {
        Time.timeScale = 1f;
        StageController.Instance?.GoToMainMenu();
    }

    private void OnVolumeChanged(float value)
    {
        AudioManager.Instance?.SetMasterVolume(value);
    }

    private void RefreshSettlementInfo()
    {
        var ps = PlayerState.Instance;
        var km = FindObjectOfType<KillRewardManager>();

        int kills = ps != null ? ps.killCount : 0;
        int coins = ps != null ? ps.coinCount : 0;

        if (killCountText != null)
            killCountText.text = $"击杀数: {kills}";

        if (coinEarnedText != null)
            coinEarnedText.text = $"获得铜钱: {coins}";

        if (milestoneText != null && km != null)
        {
            var milestones = km.CurrentMilestones;
            var earned = km.GetEarnedThresholds();
            if (milestones != null && milestones.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine("已达成里程碑:");
                foreach (var t in earned)
                    sb.AppendLine($"  · 击杀 {t}");
                int next = milestones.GetNextThreshold(kills);
                if (next > 0)
                    sb.AppendLine($"下一目标: {next}");
                milestoneText.text = sb.ToString();
            }
        }
    }
}
