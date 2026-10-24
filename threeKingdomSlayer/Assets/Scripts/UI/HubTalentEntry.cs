using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 落地页右下角的**天赋树入口大卡**（占参考图「家园」卡的位置）。
/// 位置与尺寸由 `HubLayoutStageWindow`（Tools/落地页布局台）现场拖拽调整。
/// </summary>
public class HubTalentEntry : MonoBehaviour
{
    [Header("贴图")]
    public Sprite treeSprite;

    [Header("引用")]
    public Image treeImage;
    public TMP_Text label;

    [Header("交互")]
    public Button button;
    public string talentTreeScene = "TalentTree";

    [Header("文案")]
    public string labelText = "天赋";

    private void OnEnable()
    {
        Bind();      // 运行时接线：编辑器里加的 onClick 监听不会被序列化进场景
        Refresh();
    }

    public void Refresh()
    {
        if (treeImage != null && treeSprite != null)
            treeImage.sprite = treeSprite;
        if (label != null)
            label.text = labelText;
    }

    private void OnClick()
    {
        var flow = SceneFlowManager.Instance;
        if (flow == null)
        {
            Debug.LogWarning("[HubTalentEntry] 找不到 SceneFlowManager，无法打开天赋界面");
            return;
        }
        flow.SwitchOverlay(talentTreeScene);
    }

    /// <summary>给按钮接线（运行时调用；编辑器里加的监听不会被保存）。</summary>
    public void Bind()
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);
    }
}
