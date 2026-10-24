using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 落地页左下角的**四宫格菜单键** —— **功能待定**，只做置灰占位。
///
/// 规则（用户 2026-10-24 拍板）：**不命名、不编功能**；后续确定功能后再接线。
/// 位置与尺寸由 `HubLayoutStageWindow`（Tools/落地页布局台）现场拖拽调整。
/// </summary>
public class HubMenuPlaceholder : MonoBehaviour
{
    [Header("贴图")]
    public Sprite keySprite;

    [Header("引用")]
    public Image keyImage;
    public Button button;

    [Header("状态")]
    [Tooltip("功能待定 → 置灰不可点。")]
    public bool locked = true;

    private void OnEnable()
    {
        if (keyImage != null && keySprite != null) keyImage.sprite = keySprite;
        if (button != null)
        {
            button.interactable = !locked;
            var colors = button.colors;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.75f);
            button.colors = colors;
        }
    }
}
