#if DEVELOPMENT_BUILD || UNITY_EDITOR
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// **开发者「跳过本节点战斗」按钮**（2026-10-24 ✅）。
///
/// 用途：打到手机上直接跳到下一个节点 ✅（测路线 / 测场景表现 ✅，不用真打一场 ✅）。
///
/// 边界（别越线 ✅）：
///   • **只进开发构建** ✅（<c>DEVELOPMENT_BUILD</c> / 编辑器 ✅）→ 正式包里没有这个按钮 ✅；
///   • **不碰战斗逻辑** ✅：走现成通路 ✅ —— 清场 ✅ + 把本节点战斗条目标记完成 ✅
///     → 直接进战后演出 / 选路 ✅（见 <see cref="IRouteFlow.SkipCurrentNodeBattle"/> ✅）；
///   • **不碰奖励发放** ✅：三选一该弹还是弹 ✅（没开就不弹 ✅）。
///
/// 依赖方向：只认 <see cref="IRouteFlow"/> 端口 ✅（界面层 → 流程层 ✅，不点名具体类 ✅）。
///
/// 零场景接线 ✅：<see cref="RuntimeInitializeOnLoadMethod"/> 自建 UI ✅
/// （不用往 Battle 场景里挂东西 ✅，也不用改场景文件 ✅）。
/// </summary>
public sealed class DevSkipBattleButton : MonoBehaviour
{
    /// <summary>按钮文案（有中文字形时用 ✅，否则退英文 ✅）。</summary>
    private const string LabelCn = "跳过战斗";

    /// <summary>参考分辨率（与战斗 UI 一致 ✅：1080×1920 ✅）。</summary>
    private const float ReferenceWidth = 1080f;
    private const float ReferenceHeight = 1920f;

    private static DevSkipBattleButton _instance;

    private Button _button;
    private Image _background;
    private TextMeshProUGUI _label;
    private string _lastLabel = "";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (_instance != null) return;
        var go = new GameObject("DevSkipBattleButton");
        _instance = go.AddComponent<DevSkipBattleButton>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        BuildUI();
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    private void Update()
    {
        var flow = RouteFlowHub.Current;
        // 没有路线流程就藏起来 ✅（主菜单 / 其它场景不显示 ✅）
        bool show = flow != null;
        if (_button != null && _button.gameObject.activeSelf != show) _button.gameObject.SetActive(show);
        if (!show) return;

        bool inBattle = flow.Phase == FakeRoutePhase.Battle;
        if (_button != null)
        {
            _button.interactable = inBattle;
            if (_background != null)
            {
                var c = _background.color;
                c.a = inBattle ? 0.65f : 0.25f;
                _background.color = c;
            }
        }

        string nodeId = flow.CurrentNodeId;
        string text = string.IsNullOrEmpty(nodeId) ? LabelCn : LabelCn + " " + nodeId;
        if (text != _lastLabel)
        {
            _lastLabel = text;
            if (_label != null) _label.text = text;
        }
    }

    private void OnClick()
    {
        var flow = RouteFlowHub.Current;
        if (flow == null) return;
        bool ok = flow.SkipCurrentNodeBattle();
        Debug.Log("[DevSkip] 跳过战斗 " + (ok ? "成功" : "失败（当前不在战斗中）"));
    }

    // ───────────────────────── UI ─────────────────────────

    private void BuildUI()
    {
        // 自带一个 Overlay 画布 ✅（压在最上面 ✅，不依赖场景里的 Canvas ✅）
        var canvasGo = new GameObject("Canvas", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.matchWidthOrHeight = 0.5f;
        // **必须有 GraphicRaycaster** ✅：没有它画布不参与射线检测 ✅ →
        // 按钮收不到点击 ✅，点击会漏到游戏里变成攻击手势 ✅（2026-10-24 踩过 ✅）。
        if (canvasGo.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // 按钮本体（右上角 ✅）
        var btnGo = new GameObject("SkipBattleButton", typeof(RectTransform));
        btnGo.transform.SetParent(canvasGo.transform, false);
        var rect = (RectTransform)btnGo.transform;
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-24f, -24f);
        rect.sizeDelta = new Vector2(240f, 84f);

        _background = btnGo.AddComponent<Image>();
        _background.color = new Color(0f, 0f, 0f, 0.25f);

        _button = btnGo.AddComponent<Button>();
        _button.targetGraphic = _background;
        _button.onClick.AddListener(OnClick);

        // 文案
        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(btnGo.transform, false);
        var labelRect = (RectTransform)labelGo.transform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        _label = labelGo.AddComponent<TextMeshProUGUI>();
        _label.alignment = TextAlignmentOptions.Center;
        _label.fontSize = 36f;
        _label.color = Color.white;
        _label.enableWordWrapping = false;
        _label.overflowMode = TextOverflowModes.Overflow;

        var font = ResolveFontWithCjk();
        if (font != null)
        {
            _label.font = font;
            _label.text = LabelCn;
        }
        else
        {
            // 没有中文字形 ✅ → 用英文 ✅（别显示成方块 ✅）
            _label.text = "SKIP BATTLE";
        }
    }

    /// <summary>找一份**有中文字形**的 TMP 字体 ✅（借场景里现成的 ✅，不额外引字体资产 ✅）。</summary>
    private static TMP_FontAsset ResolveFontWithCjk()
    {
        var texts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
        for (int i = 0; i < texts.Length; i++)
        {
            var t = texts[i];
            if (t == null || t.gameObject.scene == null || !t.gameObject.scene.IsValid()) continue;
            var font = t.font;
            if (font != null && font.HasCharacter('跳', true, false)) return font;
        }
        return null;
    }
}
#endif
