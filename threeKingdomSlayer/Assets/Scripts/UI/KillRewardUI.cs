using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// 击杀奖励UI — 显示击杀进度 + 里程碑达成弹窗
/// 所有 TMP/Image/Slider 引用均通过 Inspector 配置，无硬编码美术资源
/// </summary>
public class KillRewardUI : MonoBehaviour
{
    [Header("进度显示")]
    [Tooltip("击杀进度文字（如：击杀 12/20）")]
    public TMP_Text killProgressText;
    [Tooltip("进度条 Slider — 显示关卡总进度 currentKill/totalEnemyCount")]
    public UnityEngine.UI.Slider progressSlider;
    [Tooltip("里程碑标签预制体（TMP_Text），自动按比例放置在 Slider 左侧")]
    public TMP_Text milestoneLabelPrefab;
    [Tooltip("里程碑标签距 Slider 左边缘的偏移（负值=向左偏移）")]
    public float milestoneLabelOffsetX = -30f;
    [Tooltip("里程碑标签字体颜色")]
    public Color milestoneLabelColor = Color.white;
    [Tooltip("里程碑标签字号")]
    public float milestoneLabelFontSize = 18f;
    [Tooltip("里程碑标签水平偏移量（叠加在 offsetX 之上，可正可负）")]
    public float milestoneLabelOffsetY;

    [Header("奖励弹窗")]
    [Tooltip("弹窗根节点（含 CanvasGroup 做透明动画）")]
    public GameObject rewardPopup;
    [Tooltip("弹窗标题文字（如：击杀 20 奖励!）")]
    public TMP_Text rewardTitleText;
    [Tooltip("弹窗奖励数值文字（如：+50 铜钱）")]
    public TMP_Text rewardAmountText;

    [Header("弹窗动画参数")]
    [Tooltip("弹窗显示停留时间（秒）")]
    public float popupDuration = 2f;
    [Tooltip("弹窗淡入/淡出时间（秒）")]
    public float popupFadeDuration = 0.3f;
    [Tooltip("弹窗入场缩放缓动类型")]
    public Ease popupScaleEase = Ease.OutBack;

    private KillRewardManager _manager;
    private CanvasGroup _popupCanvasGroup;
    private Sequence _popupSequence;
    private List<TMP_Text> _milestoneLabels = new List<TMP_Text>();
    private int _totalEnemyCount;

    private void Start()
    {
        _manager = FindObjectOfType<KillRewardManager>();
        if (_manager != null)
            _manager.OnMilestoneReached += OnMilestoneReached;

        if (rewardPopup != null)
        {
            _popupCanvasGroup = rewardPopup.GetComponent<CanvasGroup>();
            if (_popupCanvasGroup == null)
                _popupCanvasGroup = rewardPopup.AddComponent<CanvasGroup>();
            rewardPopup.SetActive(false);
        }

        // 读取关卡总敌人数
        StageConfig sc = StageController.Instance != null ? StageController.Instance.stageConfig : null;
        _totalEnemyCount = sc != null ? sc.GetTotalEnemyCount() : 0;

        BuildMilestoneLabels();

        if (PlayerState.Instance != null)
            PlayerState.Instance.OnKillCountChanged += UpdateProgress;

        // 初始刷新
        UpdateProgress(PlayerState.Instance != null ? PlayerState.Instance.killCount : 0);
    }

    private void OnDestroy()
    {
        _popupSequence?.Kill();

        if (_manager != null)
            _manager.OnMilestoneReached -= OnMilestoneReached;
        if (PlayerState.Instance != null)
            PlayerState.Instance.OnKillCountChanged -= UpdateProgress;

        // 清理里程碑标签
        foreach (var label in _milestoneLabels)
        {
            if (label != null) Destroy(label.gameObject);
        }
        _milestoneLabels.Clear();
    }

    private void UpdateProgress(int killCount)
    {
        var milestones = _manager?.CurrentMilestones;
        if (killProgressText != null && milestones != null)
        {
            int next = milestones.GetNextThreshold(killCount);
            killProgressText.text = next > 0 ? $"击杀 {killCount}/{next}" : $"击杀 {killCount}";
        }

        // 关卡总进度：currentKill / totalEnemyCount
        // Fill Image 使用 Simple 类型，Slider 通过 RectTransform 裁剪控制填充
        if (progressSlider != null)
        {
            progressSlider.value = _totalEnemyCount > 0
                ? Mathf.Clamp01((float)killCount / _totalEnemyCount)
                : 0f;
        }
    }

    private void OnMilestoneReached(int threshold, List<KillRewardEntry> rewards)
    {
        ShowRewardPopup(threshold, rewards);
    }

    /// <summary>
    /// 在 Slider 左侧按比例生成里程碑击杀数标签
    /// </summary>
    private void BuildMilestoneLabels()
    {
        // 清理旧标签
        foreach (var label in _milestoneLabels)
        {
            if (label != null) Destroy(label.gameObject);
        }
        _milestoneLabels.Clear();

        if (progressSlider == null || milestoneLabelPrefab == null) return;
        if (_totalEnemyCount <= 0) return;

        StageConfig sc = StageController.Instance != null ? StageController.Instance.stageConfig : null;
        if (sc == null || sc.killMilestones == null || sc.killMilestones.Count == 0) return;

        RectTransform sliderRT = progressSlider.GetComponent<RectTransform>();
        float sliderHeight = sliderRT.sizeDelta.y;

        foreach (var entry in sc.killMilestones)
        {
            float ratio = Mathf.Clamp01((float)entry.killThreshold / _totalEnemyCount);
            TMP_Text label = Instantiate(milestoneLabelPrefab, progressSlider.transform);
            label.text = entry.killThreshold.ToString();

            RectTransform labelRT = label.GetComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0.5f, 0f);
            labelRT.anchorMax = new Vector2(0.5f, 0f);
            labelRT.pivot = new Vector2(1f, 0.5f);
            labelRT.anchoredPosition = new Vector2(milestoneLabelOffsetX, sliderHeight * ratio + milestoneLabelOffsetY);
            label.color = milestoneLabelColor;
            label.fontSize = milestoneLabelFontSize;

            _milestoneLabels.Add(label);
        }
    }

    private void ShowRewardPopup(int threshold, List<KillRewardEntry> rewards)
    {
        if (rewardPopup == null) return;

        _popupSequence?.Kill();

        if (rewardTitleText != null)
            rewardTitleText.text = $"击杀 {threshold} 奖励!";

        if (rewardAmountText != null && rewards != null)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < rewards.Count; i++)
            {
                if (i > 0) sb.Append("  ");
                string label = rewards[i].rewardType == KillRewardType.Coin ? "铜钱" :
                               rewards[i].rewardType == KillRewardType.Heal ? "生命" : "升级";
                sb.Append($"+{rewards[i].rewardAmount} {label}");
            }
            rewardAmountText.text = sb.ToString();
        }

        rewardPopup.transform.localScale = Vector3.zero;
        rewardPopup.SetActive(true);
        if (_popupCanvasGroup != null) _popupCanvasGroup.alpha = 1f;

        _popupSequence = DOTween.Sequence();
        _popupSequence.Append(rewardPopup.transform.DOScale(1f, popupFadeDuration).SetEase(popupScaleEase));
        _popupSequence.AppendInterval(popupDuration);
        _popupSequence.Append(rewardPopup.transform.DOScale(0f, popupFadeDuration).SetEase(Ease.InBack));
        _popupSequence.OnComplete(() => rewardPopup.SetActive(false));
    }
}
