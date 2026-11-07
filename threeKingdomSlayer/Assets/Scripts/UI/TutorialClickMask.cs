using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// **教学引导的聚光遮罩**（2026-11-06 用户定规）。
///
/// 做法（用户原话）：「你就把**字幕板**和**对应按钮**层级提到遮罩上不就行了」→
///   • 遮罩 = **一整块**，铺满宿主层；
///   • 要点的那个按钮 → **提到遮罩上面**（提到宿主层里）→ **不被压暗、射线也先命中它**；
///   • 字幕板本来就在宿主层里 → 同样在遮罩上面；
///   • 其余区域被压暗、且被遮罩吃掉点击。
///   → **不做**「把遮罩切成四块挖洞」那套。
///
/// 层级（宿主层内）：[MaskPanel(遮罩, 最底)] → [字幕板] → [提到上面的目标按钮]
/// 宿主层本身是全屏 canvas（落地页 order 60 / 天赋页教学层）→ 遮罩盖住整个界面。
///
/// ⚠️ **压暗必须用 `CanvasGroup.alpha` + 不透明底图**，不能用半透明 Image：
///    本项目实测，**运行时新建的半透明 Image 会进不了 canvas 的渲染批次** →
///    **压暗完全画不出来**（逐像素比对=没画），但不透明色却能正常显示
///    → 极易误判成"遮罩根本没做"。项目自带的 `DialogueDimOverlay`、
///    `Tutorial Blackout` 幕布也是同样问题。
///    → CanvasGroup 的 alpha 走批次级通道 → 稳定生效（2026-11-06 实测）。
///
/// ⚠️ 遮罩本体要在**玩家帧之外**创建（Editor: `EditorApplication.update`；真机: `WaitForEndOfFrame`），
///    否则同样可能进不了批次。
/// </summary>
public sealed class TutorialClickMask : MonoBehaviour
{
    [Tooltip("遮罩压暗的 alpha（0 = 全透明，只挡点击）")]
    [Range(0f, 1f)] public float dimAlpha = 0.45f;

    private CanvasGroup _group;
    private RectTransform _host;
    private RectTransform _target;
    private RectTransform _targetHome;
    private int _targetHomeIndex = -1;

    /// <summary>遮罩是否正在挡点击。</summary>
    public bool Blocking { get; private set; }

    /// <summary>当前被提到遮罩上层的目标。</summary>
    public RectTransform Target => _target;

    /// <summary>在宿主层下建这块遮罩（宿主 = 引导层 / 面板教学层）。</summary>
    public static TutorialClickMask EnsureExists(RectTransform host)
    {
        if (host == null) return null;

        var go = new GameObject("TutorialClickMask", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(host, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var mask = go.AddComponent<TutorialClickMask>();
        mask._host = host;
        mask.SetBlocking(false);
        mask.RequestPanelBuild();   // ★ 遮罩稍后在「帧外」创建（见类注释）
        return mask;
    }

    /// <summary>把遮罩的创建安排到"玩家帧之外"（否则可能进不了 canvas 批次）。</summary>
    private void RequestPanelBuild()
    {
        StartCoroutine(BuildPanelWhenCanvasSettled());
    }

    private IEnumerator BuildPanelWhenCanvasSettled()
    {
        // 等一帧再安排：避免在宿主层 canvas 刚建好、批次还没稳的那一帧创建
        yield return null;
        if (this == null) yield break;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.CallbackFunction cb = null;
        cb = () =>
        {
            UnityEditor.EditorApplication.update -= cb;
            if (this == null) return;      // 已被销毁
            BuildPanel();
            // ★ 立刻追加一帧：让遮罩在下一帧渲染前就已经进入 canvas 批次
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
        };
        UnityEditor.EditorApplication.update += cb;
#else
        yield return new WaitForEndOfFrame();
        if (this == null) yield break;
        BuildPanel();
#endif
    }

    /// <summary>建那块铺满宿主层的遮罩（CanvasGroup 压暗 + 不透明底图）。</summary>
    private void BuildPanel()
    {
        if (_host == null || _group != null) return;

        var go = new GameObject("MaskPanel", typeof(RectTransform), typeof(CanvasGroup));
        var rt = (RectTransform)go.transform;
        rt.SetParent(_host, false);
        // 铺满宿主层（宿主层本身是全屏）→ 遮罩就是全屏一块
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var group = go.GetComponent<CanvasGroup>();
        group.alpha = Blocking ? dimAlpha : 0f;
        group.interactable = false;
        group.blocksRaycasts = true;      // 由它挡住其余 UI 的点击
        _group = group;

        // 底图用**不透明**黑：压暗交给 CanvasGroup 的 alpha（半透明 Image 在本项目画不出来）
        var imgGo = new GameObject("MaskImage", typeof(RectTransform));
        var irt = (RectTransform)imgGo.transform;
        irt.SetParent(rt, false);
        irt.anchorMin = Vector2.zero;
        irt.anchorMax = Vector2.one;
        irt.offsetMin = Vector2.zero;
        irt.offsetMax = Vector2.zero;
        var img = imgGo.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 1f);
        img.raycastTarget = true;

        // 垫在宿主层最底下 → 字幕板 / 提到上面的目标都在它上面
        rt.SetAsFirstSibling();
        if (!Blocking) go.SetActive(false);
    }

    /// <summary>
    /// 设定高亮目标：把目标**提到遮罩上层**（同层内后绘制 → 不被压暗、射线也先命中它）。
    /// </summary>
    public void SetTarget(RectTransform target)
    {
        if (_target == target) return;

        RestoreTarget();
        _target = target;
        if (target == null) return;

        // 记下原父节点，隐藏时要放回去
        _targetHome = target.parent as RectTransform;
        _targetHomeIndex = target.GetSiblingIndex();

        if (_host != null)
        {
            if (target.parent != _host)
            {
                target.SetParent(_host, true);   // 保留世界位置
            }
            target.SetAsLastSibling();            // → 在遮罩上面
        }
    }

    /// <summary>开 / 关遮罩（关时把目标放回原位）。</summary>
    public void SetBlocking(bool blocking)
    {
        Blocking = blocking;
        if (_group != null)
        {
            _group.alpha = blocking ? dimAlpha : 0f;
            var panelGo = _group.gameObject;
            if (blocking) panelGo.SetActive(true);
            else panelGo.SetActive(false);
        }
        if (blocking) return;
        RestoreTarget();
    }

    /// <summary>把目标放回原来的父节点与顺序。</summary>
    private void RestoreTarget()
    {
        if (_target == null) { _targetHome = null; return; }

        if (_targetHome != null && _target.parent != _targetHome)
        {
            _target.SetParent(_targetHome, true);
            int max = _targetHome.childCount - 1;
            _target.SetSiblingIndex(Mathf.Clamp(_targetHomeIndex, 0, Mathf.Max(0, max)));
        }
        _targetHome = null;
        _targetHomeIndex = -1;
        _target = null;
    }
}
