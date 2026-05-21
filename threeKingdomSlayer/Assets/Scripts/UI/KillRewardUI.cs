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
    [Tooltip("进度条 Slider（可选）")]
    public UnityEngine.UI.Slider progressSlider;

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
    }

    private void UpdateProgress(int killCount)
    {
        var milestones = _manager?.CurrentMilestones;
        if (killProgressText != null && milestones != null)
        {
            int next = milestones.GetNextThreshold(killCount);
            killProgressText.text = next > 0 ? $"击杀 {killCount}/{next}" : $"击杀 {killCount}";
        }

        if (progressSlider != null && milestones != null)
        {
            if (milestones.Count == 0)
            {
                progressSlider.value = 1f;
                return;
            }

            int prevThreshold = 0;
            int nextThreshold = milestones[0].killThreshold;
            foreach (var m in milestones)
            {
                if (m.killThreshold <= killCount)
                    prevThreshold = m.killThreshold;
                else
                {
                    nextThreshold = m.killThreshold;
                    break;
                }
            }
            nextThreshold = Mathf.Max(nextThreshold, prevThreshold + 1);
            progressSlider.value = Mathf.Clamp01((float)(killCount - prevThreshold) / (nextThreshold - prevThreshold));
        }
    }

    private void OnMilestoneReached(int threshold, List<KillRewardEntry> rewards)
    {
        ShowRewardPopup(threshold, rewards);
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
