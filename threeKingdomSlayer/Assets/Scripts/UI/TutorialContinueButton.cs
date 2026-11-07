using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 教程开场的「点击继续」按钮（2026-11-05）。
///
/// ⚠️ **为什么不复用台词区那个 `DialogueAdvanceButton`**：
///   对话气泡 / 继续按钮 / 全屏点击遮罩都挂在 `DialogueOverlay`（order=120 的嵌套 Canvas）下，
///   而那个 Canvas **没有 GraphicRaycaster** → 里面的图形收不到射线，点不动。
///   （Unity 的规则：嵌套 Canvas 的图形按「最近的祖先 Canvas」注册 GraphicRegistry，
///     只有该 Canvas **自己身上的** GraphicRaycaster 才会扫它们。）
///   → 所以这个按钮**自带 Canvas + GraphicRaycaster**（照抄场景里 `DialogueSkipButton` 的做法），
///     画在 order=121：永远压在对话层之上，而且**独立可点**。
///
/// 用法：教学导演在开场流程里 <c>Show(() =&gt; clicked = true)</c>，玩家点完再 <c>Hide()</c>。
/// </summary>
public sealed class TutorialContinueButton : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private TMP_Text _label;
    [Tooltip("按钮上的文字（运行时覆盖场景里的静态文本）")]
    [SerializeField] private string _labelText = "点击继续";

    /// <summary>
    /// **按钮要摆在谁的上方**（玩家的血条）。
    /// 场景里接的是战斗 HUD 的 <c>HealthBar</c> → 按钮就落在血条正上方。
    ///
    /// ⚠️ 两者不在同一个 Canvas（按钮在 order=121 的 TutorialIntroOverlay，血条在 order=21 的 HudCard），
    ///   所以这里用**世界坐标 → 父层局部坐标**换算，而不是写死 anchoredPosition
    ///   （写死的值在分辨率适配 / HUD 改版后会飘）。
    /// </summary>
    [SerializeField] private RectTransform _placeAbove;

    [Tooltip("与上方目标的间距（像素）")]
    [SerializeField] private float _gapAbove = 24f;

    private Action _onContinue;

    /// <summary>按钮当前是否显示着。</summary>
    public bool IsShowing => gameObject.activeSelf;

    private void Awake()
    {
        if (_button == null) _button = GetComponent<Button>();
        if (_label == null) _label = GetComponentInChildren<TMP_Text>(true);
        if (_label != null && !string.IsNullOrEmpty(_labelText)) _label.text = _labelText;
        if (_button != null)
        {
            _button.onClick.AddListener(OnClicked);
            // 场景里按钮图片的 raycastTarget 默认是 false → 不打开就点不到
            if (_button.image != null) _button.image.raycastTarget = true;
        }
    }

    private void OnDestroy()
    {
        if (_button != null) _button.onClick.RemoveListener(OnClicked);
    }

    /// <summary>显示按钮并绑定回调（再次调用会覆盖上一次的回调）。</summary>
    public void Show(Action onContinue)
    {
        _onContinue = onContinue;
        PlaceAboveAnchor();
        if (!gameObject.activeSelf) gameObject.SetActive(true);
    }

    /// <summary>
    /// 把按钮摆到 <c>_placeAbove</c>（玩家血条）的正上方。
    ///
    /// 换算：血条顶边中点（世界坐标）→ 按钮父层的局部坐标 → 按钮的 anchoredPosition。
    /// 父层的锚点原点（anchorMin==anchorMax==(0.5,0.5)）就是父层中心，
    /// 而按钮的 pivot 也是中心 → anchoredPosition.y 就是按钮中心相对父层中心的偏移。
    /// </summary>
    private void PlaceAboveAnchor()
    {
        if (_placeAbove == null) return;
        var host = transform.parent as RectTransform;
        if (host == null) return;

        var rt = transform as RectTransform;
        if (rt == null) return;

        // 布局可能还没结算（血条刚被排版）→ 先刷一次再量
        Canvas.ForceUpdateCanvases();

        var corners = new Vector3[4];
        _placeAbove.GetWorldCorners(corners);
        if (corners[1].y < corners[0].y) return;   // 量到退化数据 → 不动
        var topCenter = (corners[1] + corners[2]) * 0.5f;   // 顶边中点
        var local = host.InverseTransformPoint(topCenter);   // 父层局部坐标（父层 pivot 为原点）

        float desiredCenterY = local.y + _gapAbove + rt.rect.height * 0.5f;
        // pivot 不在中心时，anchoredPosition 指的是 pivot 而不是中心 → 换算回去
        float pivotY = desiredCenterY - (0.5f - rt.pivot.y) * rt.rect.height;
        // 父层的锚点原点（父层局部坐标）
        var anchorPoint = host.rect.min + Vector2.Scale(rt.anchorMin, host.rect.size);

        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, pivotY - anchorPoint.y);
    }

    /// <summary>收掉按钮（清掉回调）。</summary>
    public void Hide()
    {
        _onContinue = null;
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }

    /// <summary>
    /// **按真人路径点一下**（射线 + EventSystem 派发 pointerClick）。
    ///
    /// ⚠️ 为什么不用 `onClick.Invoke()` 那种“代劳”：
    ///   `Button.OnPointerClick` 会先判 `IsInteractable()`（祖先 CanvasGroup 的 interactable=false
    ///   会让它变 false）→ 那种按钮看着能点、真点下去**一点反应都没有**。
    ///   自动化如果直接调回调，就会把这一类洞全漏过去（2026-11-06 实测踩到过）。
    /// </summary>
    /// <returns>true = 点击真的送出去了。</returns>
    public bool ClickViaUi()
    {
        if (!gameObject.activeSelf) return false;
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es == null) return false;

        var rect = (RectTransform)transform;
        var canvas = rect.GetComponentInParent<Canvas>();
        if (canvas == null) return false;
        var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var pos = RectTransformUtility.WorldToScreenPoint(cam, rect.TransformPoint(rect.rect.center));

        var ped = new UnityEngine.EventSystems.PointerEventData(es) { position = pos };
        var hits = new List<UnityEngine.EventSystems.RaycastResult>();
        es.RaycastAll(ped, hits);
        if (hits.Count == 0) return false;
        var top = hits[0].gameObject;
        if (top != gameObject && !top.transform.IsChildOf(transform)) return false;   // 被别的东西盖住了

        UnityEngine.EventSystems.ExecuteEvents.Execute(gameObject, ped, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
        return true;
    }

    /// <summary>调试/自动化驱动用：直接调回调（**不经过 UI 判定**，只用于内部逻辑测试）。</summary>
    public void SimulateClick()
    {
        if (!gameObject.activeSelf) return;
        OnClicked();
    }

    private void OnClicked()
    {
        var cb = _onContinue;
        _onContinue = null;
        cb?.Invoke();
    }
}
