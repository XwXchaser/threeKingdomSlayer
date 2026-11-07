using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// **引导期点击遮罩**（2026-11-06）。
///
/// 干什么：教学开场的「暂停 + 两段字幕」期间，玩家**只能**点左上角的暂停键和「点击继续」。
/// 手势被 <c>timeScale=0</c> + <c>GameplayInputGate</c> 挡住了，但 **UI 按钮挡不住**——
/// 道具栏 / 技能槽 / 英雄头像这些还是能点（点了会亮一下，玩家就以为能交互）。
///
/// ⚠️ **为什么这个遮罩必须自带 Canvas + GraphicRaycaster**：
///   对话层那个 order=120 的嵌套 Canvas 没有 GraphicRaycaster → 里面的图形收不到射线；
///   而且它上面还挂着一个 `CanvasGroup(interactable=0, blocksRaycasts=0)` → 整棵子树都不可交互。
///   → 所以本遮罩**自己带一套**（Canvas overrideSorting + GraphicRaycaster + 全屏 Image）。
///
/// ⚠️ **层级**：遮罩在 `order=121`（对话层 120 之上、HUD 21 之上）→ 挡住所有别的 UI；
///   「点击继续」按钮是**同一 Canvas 里更靠后的兄弟节点** → 射线优先命中它 → 按钮仍点得到；
///   暂停层被提到 `order=130` → 暂停键与暂停面板都在遮罩之上 → 暂停功能不受影响。
/// </summary>
public sealed class TutorialInputBlocker : MonoBehaviour
{
    [SerializeField] private Image _blocker;

    /// <summary>
    /// **压暗（黑幕）层**：教学文本出现时把画面压暗，突出那行字。
    ///
    /// ⚠️ **压暗必须用「不透明黑底图 + CanvasGroup.alpha」**：
    ///   本项目实测，半透明 Image（<c>Image.color.a &lt; 1</c>）在某些情况下画不出来
    ///   （逐像素比对等于没画），而 CanvasGroup 的 alpha 走批次级通道 → 稳定生效。
    ///   → 场景里的 TutorialTeachingDim 就是一个 **不透明黑 Image + CanvasGroup**，
    ///     这里只驱动它的 alpha。
    ///
    /// ⚠️ 这个层**不能**挂在「点击继续」按钮的祖先上（CanvasGroup 会连按钮一起压暗、
    ///   interactable=false 还会让按钮点不动）→ 它是独立的层，按钮在它上面。
    ///
    /// ⚠️ 它的层级必须**低于台词气泡**（DialogueOverlay order=120）→
    ///   否则那行字自己也被压暗了（这层 order=110）。
    /// </summary>
    [SerializeField] private CanvasGroup _dim;

    [Tooltip("压暗的透明度（0 = 不压暗）")]
    [Range(0f, 1f)] public float dimAlpha = 0.55f;

    /// <summary>遮罩当前是否在挡点击。</summary>
    public bool IsBlocking { get; private set; }

    private void Awake()
    {
        if (_blocker == null) _blocker = GetComponent<Image>();
        Hide();
    }

    /// <summary>开始挡点击。</summary>
    public void Show()
    {
        IsBlocking = true;
        // ⚠️ 只关**图形本身**（enabled + raycastTarget），不能关 GameObject：
        //   这个 Image 就挂在本组件所在的 GameObject 上，关掉 GameObject 会把「点击继续」按钮一起关掉。
        if (_blocker != null) { _blocker.enabled = true; _blocker.raycastTarget = true; }
    }

    /// <summary>不再挡点击。</summary>
    public void Hide()
    {
        IsBlocking = false;
        if (_blocker != null) { _blocker.raycastTarget = false; _blocker.enabled = false; }
    }

    /// <summary>
    /// 开 / 关压暗（黑幕）。
    ///
    /// ⚠️ **与 Show()/Hide() 分开**：点击遮罩只管「挡不挡点击」，
    ///   压暗的开关由教学导演按**拍的节奏**管（文本出现 → 压暗；手势做完 → 撤）。
    ///   所以「点完『点击继续』」这一步**不能**把压暗撤掉 —— 第二段文本 + 手势还在屏幕上。
    /// </summary>
    public void SetDim(bool on)
    {
        if (_dim == null) return;
        _dim.alpha = on ? dimAlpha : 0f;
    }
}
