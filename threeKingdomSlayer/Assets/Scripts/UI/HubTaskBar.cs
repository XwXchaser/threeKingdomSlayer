using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 落地页左下角的**任务条**（墨色笔刷底 + 图标 + 任务标题 + 进度 + 奖励图标）。
///
/// 内容与数据架构见 `design/task-system-design.md`（任务系统）。
/// 本组件目前只负责**显示**：由任务系统调 `SetTask` 写入内容，没任务时显示占位文案。
/// 位置与尺寸由 `HubLayoutStageWindow`（Tools/落地页布局台）现场拖拽调整。
/// </summary>
public class HubTaskBar : MonoBehaviour
{
    [Header("贴图")]
    public Sprite barSprite;
    public Sprite iconSprite;
    public Sprite rewardSprite;

    [Header("引用")]
    public Image barImage;
    public Image iconImage;
    public Image rewardImage;
    public TMP_Text titleText;
    public TMP_Text progressText;

    [Header("占位文案")]
    [Tooltip("还没接任务系统时显示的文案。")]
    public string placeholderTitle = "本关目标";
    public string placeholderProgress = "0/0";

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (barImage != null && barSprite != null) barImage.sprite = barSprite;
        if (iconImage != null && iconSprite != null) iconImage.sprite = iconSprite;
        if (rewardImage != null && rewardSprite != null) rewardImage.sprite = rewardSprite;
        if (titleText != null) titleText.text = placeholderTitle;
        if (progressText != null) progressText.text = placeholderProgress;
    }

    /// <summary>写入一条任务（由任务系统调用）。</summary>
    public void SetTask(string title, int current, int max, Sprite icon = null, Sprite reward = null)
    {
        if (titleText != null) titleText.text = title;
        if (progressText != null) progressText.text = $"{current}/{Mathf.Max(1, max)}";
        if (icon != null && iconImage != null) iconImage.sprite = icon;
        if (reward != null && rewardImage != null) rewardImage.sprite = reward;
    }
}
