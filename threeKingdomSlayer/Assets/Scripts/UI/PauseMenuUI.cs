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
    public UnityEngine.UI.Slider masterVolumeSlider;
    public UnityEngine.UI.Slider bgmVolumeSlider;
    public UnityEngine.UI.Slider sfxVolumeSlider;
    public TMP_Text masterVolumeValueText;
    public TMP_Text bgmVolumeValueText;
    public TMP_Text sfxVolumeValueText;

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

        ConfigureVolumeSlider(masterVolumeSlider, AudioManager.Instance != null ? AudioManager.Instance.GetMasterVolume() : 1f, OnMasterVolumeChanged);
        ConfigureVolumeSlider(bgmVolumeSlider, AudioManager.Instance != null ? AudioManager.Instance.GetBgmVolume() : 1f, OnBgmVolumeChanged);
        ConfigureVolumeSlider(sfxVolumeSlider, AudioManager.Instance != null ? AudioManager.Instance.GetSfxVolume() : 1f, OnSfxVolumeChanged);
        RefreshVolumeValueTexts();
    }

    private void OnDestroy()
    {
        if (pauseButton != null) pauseButton.onClick.RemoveListener(OnPauseClicked);
        if (continueButton != null) continueButton.onClick.RemoveListener(OnContinueClicked);
        if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
        if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
        if (bgmVolumeSlider != null) bgmVolumeSlider.onValueChanged.RemoveListener(OnBgmVolumeChanged);
        if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
    }

    private void OnPauseClicked()
    {
        var sc = StageController.Instance;
        if (sc == null || !sc.IsStageInProgress) return;

        _isPaused = true;
        // 先显示面板再暂停（SetActive 必须在 timeScale=0 之前，且不能放在可能抛异常的代码之后）
        if (pausePanel != null) pausePanel.SetActive(true);

        SyncVolumeSliders();
        RefreshVolumeValueTexts();
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

    private void ConfigureVolumeSlider(UnityEngine.UI.Slider slider, float value, UnityEngine.Events.UnityAction<float> callback)
    {
        if (slider == null) return;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(value);
        slider.onValueChanged.AddListener(callback);
    }

    private void SyncVolumeSliders()
    {
        var audio = AudioManager.Instance;
        if (audio == null) return;
        if (masterVolumeSlider != null) masterVolumeSlider.SetValueWithoutNotify(audio.GetMasterVolume());
        if (bgmVolumeSlider != null) bgmVolumeSlider.SetValueWithoutNotify(audio.GetBgmVolume());
        if (sfxVolumeSlider != null) sfxVolumeSlider.SetValueWithoutNotify(audio.GetSfxVolume());
    }

    private void OnMasterVolumeChanged(float value)
    {
        AudioManager.Instance?.SetMasterVolume(value);
        UpdateVolumeValueText(masterVolumeValueText, value);
    }

    private void OnBgmVolumeChanged(float value)
    {
        AudioManager.Instance?.SetBgmVolume(value);
        UpdateVolumeValueText(bgmVolumeValueText, value);
    }

    private void OnSfxVolumeChanged(float value)
    {
        AudioManager.Instance?.SetSfxVolume(value);
        UpdateVolumeValueText(sfxVolumeValueText, value);
    }

    private void RefreshVolumeValueTexts()
    {
        UpdateVolumeValueText(masterVolumeValueText, masterVolumeSlider != null ? masterVolumeSlider.value : 0f);
        UpdateVolumeValueText(bgmVolumeValueText, bgmVolumeSlider != null ? bgmVolumeSlider.value : 0f);
        UpdateVolumeValueText(sfxVolumeValueText, sfxVolumeSlider != null ? sfxVolumeSlider.value : 0f);
    }

    private static void UpdateVolumeValueText(TMP_Text text, float value)
    {
        if (text != null)
            text.text = $"{Mathf.RoundToInt(value * 100f)}%";
    }
}
