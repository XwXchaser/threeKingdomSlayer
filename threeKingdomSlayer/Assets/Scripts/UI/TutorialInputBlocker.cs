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
}
