using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 落地页左下角的**四宫格菜单键** —— 原本是「功能待定、只做置灰占位」。
///
/// 2026-10-27（用户决定）：**从面板上撤下**。
///   • 理由：它们无名称、无功能语义（参考图里就是四个抽象符号的置灰块），
///     玩家会当成按钮去点 → 点了没反应（用户反馈「天赋页完全无法进入」）。
///   • 选关 / 天赋的正式入口是**右下两张卡**（`HubStageEntry` / `HubTalentEntry`），
///     它们有文字标签、正常外观、不是灰的 → 不需要给它们加呼吸动效
///     （落地页只允许一个动态元素 = 「开始关卡」）。
///   • 对象保留在场景里（置为 inactive），以后要恢复只改 activeSelf。
///
/// 锁定表现（保留实现，便于将来复用）：`button.interactable = false` + 键体与符号一起压成
/// <see cref="lockedTint"/> + 盖一层 <see cref="dimOverlay"/> 压暗。
/// </summary>
public class HubMenuPlaceholder : MonoBehaviour
{
    [Header("贴图")]
    public Sprite keySprite;

    [Header("引用")]
    public Image keyImage;

    [Tooltip("键面中央的符号图标（锁定时一起压暗）")]
    public Image symbolImage;

    [Tooltip("锁定时的压暗层（无贴图的 Image 会画成实心块，直接盖住整个键）")]
    public Image dimOverlay;

    public Button button;

    [Header("状态")]
    [Tooltip("功能待定 → 置灰不可点。")]
    public bool locked = true;

    [Header("锁定外观")]
    [Tooltip("键体与符号一起压成这个颜色（禁用态读感）")]
    public Color lockedTint = new Color(0.62f, 0.64f, 0.68f, 1f);

    [Range(0f, 1f)]
    [Tooltip("压暗层的不透明度")]
    public float lockedDim = 0.28f;

    private void OnEnable()
    {
        Apply();
    }

    /// <summary>把锁定态刷到界面上（运行时与编辑态都可用）。</summary>
    public void Apply()
    {
        if (keyImage != null && keySprite != null) keyImage.sprite = keySprite;

        if (button != null)
        {
            button.interactable = !locked;
            var colors = button.colors;
            // 禁用态不再额外叠 Button 自身的染色 —— 灰化完全由 lockedTint + 压暗层控制，
            // 免得两处相乘后深浅不可控。
            colors.disabledColor = Color.white;
            colors.normalColor = Color.white;
            button.colors = colors;
        }

        if (locked)
        {
            if (keyImage != null) keyImage.color = lockedTint;
            if (symbolImage != null) symbolImage.color = lockedTint;
            if (dimOverlay != null)
            {
                dimOverlay.enabled = true;
                dimOverlay.color = new Color(0f, 0f, 0f, lockedDim);
                dimOverlay.raycastTarget = false;
            }
        }
        else
        {
            if (keyImage != null) keyImage.color = Color.white;
            if (symbolImage != null) symbolImage.color = Color.white;
            if (dimOverlay != null) dimOverlay.enabled = false;
        }
    }
}
