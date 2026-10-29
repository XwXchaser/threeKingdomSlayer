using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 天赋树面板。零场景搭 UI：运行时按 TalentTreeConfig 生成节点与连线，
/// 节点位置直接取 TalentNodeConfig.treePosition（设计像素，1080×1920 口径）。
///
/// 美术替换点全部是 SerializedField：把图拖进来即可换皮，不用改代码，也不走 Resources 查找
/// （遵守 design/anti-ghost-reference.md）。图留空时用程序化圆形 + 纯色占位，方便先跑通流程。
/// </summary>
public class TalentTreePanel : MonoBehaviour
{
    [Header("数据（必填）")]
    public TalentTreeConfig tree;

    [Header("字体（必填，留空则不生成文字）")]
    public TMP_FontAsset font;

    [Header("美术替换点（留空走占位）")]
    public Sprite dimBackground;
    public Sprite panelBackground;
    public Sprite nodeLocked;
    public Sprite nodeAvailable;
    public Sprite nodeUnlocked;
    public Sprite connector;
    public Sprite coinIcon;

    [Header("排版")]
    [Tooltip("节点边长（设计像素）")]
    public Vector2 nodeSize = new Vector2(160f, 160f);
    [Tooltip("连线粗细（设计像素）")]
    public float connectorThickness = 10f;

    [Header("配色（美术稿下来前先用这套）")]
    public Color lockedColor = new Color(0.32f, 0.30f, 0.28f, 1f);
    public Color availableColor = new Color(0.86f, 0.70f, 0.36f, 1f);
    public Color unlockedColor = new Color(0.93f, 0.90f, 0.82f, 1f);
    public Color connectorLockedColor = new Color(0.30f, 0.28f, 0.26f, 1f);
    public Color connectorUnlockedColor = new Color(0.82f, 0.65f, 0.32f, 1f);

    [Header("调试")]
    [Tooltip("打开时在 Console 打印每次刷新的状态")]
    public bool verboseLog = false;

    private RectTransform _nodeRoot;
    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _coinText;
    private TextMeshProUGUI _descText;
    private readonly List<Image> _nodeImages = new List<Image>();
    private readonly List<Button> _nodeButtons = new List<Button>();
    private readonly List<TextMeshProUGUI> _nodeLabels = new List<TextMeshProUGUI>();
    private readonly List<TextMeshProUGUI> _nodeCosts = new List<TextMeshProUGUI>();
    private readonly List<Image> _connectorImages = new List<Image>();
private readonly List<TalentNodeConfig> _connectorTargets = new List<TalentNodeConfig>();
    private bool _built;

    private static Sprite _circleSprite;

    private void Awake()
    {
        // 面板对象在场景里默认是关闭的（打开时才第一次跑 Awake）。
        // 不要在这里做 SetVisible(false)：Awake 刚被“激活”触发，再把自己关掉会让 Toggle 永远只能开一半。
        Build();
    }

    private void OnEnable()
    {
        TalentTreeState.OnChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        TalentTreeState.OnChanged -= Refresh;
    }

    public void Toggle() => SetVisible(!gameObject.activeSelf);

    public void Open()
    {
        Build();
        SetVisible(true);
    }

    public void Close() => SetVisible(false);

    private void SetVisible(bool visible)
    {
        if (gameObject.activeSelf == visible)
        {
            if (visible) Refresh();
            return;
        }
        gameObject.SetActive(visible);
        if (visible) Refresh();
    }

    #region UI 生成

    private void Build()
    {
        if (_built) return;
        _built = true;

        var self = (RectTransform)transform;
        self.anchorMin = Vector2.zero;
        self.anchorMax = Vector2.one;
        self.offsetMin = Vector2.zero;
        self.offsetMax = Vector2.zero;

        // 遮罩层：挡住菜单点击
        var dim = CreateImage("Dim", self, new Color(0f, 0f, 0f, 0.72f), dimBackground);
        Stretch(dim.rectTransform);

        // 面板底
        var panel = CreateImage("Panel", self, new Color(1f, 1f, 1f, 1f), panelBackground);
        Stretch(panel.rectTransform, 40f);

        // 标题
        _titleText = CreateText("Title", panel.rectTransform, "天  赋", 64f, TextAlignmentOptions.Center);
        AnchorTop(_titleText.rectTransform, 120f, new Vector2(0f, -80f));

        // 铜钱
        var coinRow = new GameObject("CoinRow", typeof(RectTransform));
        coinRow.transform.SetParent(panel.rectTransform, false);
        var coinRowRect = (RectTransform)coinRow.transform;
        coinRowRect.anchorMin = new Vector2(1f, 1f);
        coinRowRect.anchorMax = new Vector2(1f, 1f);
        coinRowRect.pivot = new Vector2(1f, 1f);
        coinRowRect.anchoredPosition = new Vector2(-72f, -56f);
        coinRowRect.sizeDelta = new Vector2(320f, 72f);

        var coin = CreateImage("CoinIcon", coinRowRect, Color.white, coinIcon);
        var coinRect = coin.rectTransform;
        coinRect.anchorMin = new Vector2(0f, 0.5f);
        coinRect.anchorMax = new Vector2(0f, 0.5f);
        coinRect.pivot = new Vector2(0f, 0.5f);
        coinRect.sizeDelta = new Vector2(56f, 56f);
        coinRect.anchoredPosition = Vector2.zero;
        coin.preserveAspect = true;

        _coinText = CreateText("CoinText", coinRowRect, "0", 48f, TextAlignmentOptions.Right);
        var coinTextRect = _coinText.rectTransform;
        coinTextRect.anchorMin = new Vector2(0f, 0f);
        coinTextRect.anchorMax = new Vector2(1f, 1f);
        coinTextRect.offsetMin = new Vector2(70f, 0f);
        coinTextRect.offsetMax = Vector2.zero;

        // 节点层（原点在面板中心）
        var nodes = new GameObject("Nodes", typeof(RectTransform));
        nodes.transform.SetParent(panel.rectTransform, false);
        _nodeRoot = (RectTransform)nodes.transform;
        _nodeRoot.anchorMin = new Vector2(0.5f, 0.5f);
        _nodeRoot.anchorMax = new Vector2(0.5f, 0.5f);
        _nodeRoot.pivot = new Vector2(0.5f, 0.5f);
        _nodeRoot.anchoredPosition = new Vector2(0f, -40f);
        _nodeRoot.sizeDelta = Vector2.zero;

        // 说明栏
        _descText = CreateText("Description", panel.rectTransform, "", 36f, TextAlignmentOptions.TopLeft);
        var descRect = _descText.rectTransform;
        descRect.anchorMin = new Vector2(0f, 0f);
        descRect.anchorMax = new Vector2(1f, 0f);
        descRect.pivot = new Vector2(0.5f, 0f);
        descRect.offsetMin = new Vector2(80f, 40f);
        descRect.offsetMax = new Vector2(-80f, 240f);

        // 关闭
        var close = CreateImage("CloseButton", panel.rectTransform, new Color(0.85f, 0.75f, 0.55f, 1f), null);
        var closeRect = close.rectTransform;
        closeRect.anchorMin = new Vector2(0f, 1f);
        closeRect.anchorMax = new Vector2(0f, 1f);
        closeRect.pivot = new Vector2(0f, 1f);
        closeRect.sizeDelta = new Vector2(140f, 88f);
        closeRect.anchoredPosition = new Vector2(56f, -40f);
        close.raycastTarget = true;
        close.transform.SetAsLastSibling();
        var closeButton = close.gameObject.AddComponent<Button>();
        closeButton.targetGraphic = close;
        closeButton.onClick.AddListener(Close);
        var closeLabel = CreateText("Text", closeRect, "返回", 40f, TextAlignmentOptions.Center);
        Stretch(closeLabel.rectTransform);

        BuildNodes();

        if (tree == null)
            Debug.LogWarning("[Talent] TalentTreePanel 没有指定 TalentTreeConfig，面板会是空的");
    }

    private void BuildNodes()
    {
        _nodeImages.Clear();
        _nodeButtons.Clear();
        _nodeLabels.Clear();
        _nodeCosts.Clear();
        _connectorImages.Clear();
        _connectorTargets.Clear();
        if (tree == null || tree.nodes == null) return;

        // 连线先画，压在节点下面
        // **按「边」画** ✅ —— 每个节点 → 它的每个前置 ✅（支持分叉 ✅ ✅）
        for (int i = 0; i < tree.nodes.Count; i++)
        {
            var to = tree.nodes[i];
            if (to == null) continue;
            var prereqs = tree.GetPrerequisites(to);
            for (int p = 0; p < prereqs.Count; p++)
            {
                var from = prereqs[p];
                if (from == null) continue;
                if (from.hidden || to.hidden) continue;   // 隐藏节点不画连线
                var line = CreateImage($"Link_{from.nodeId}_{to.nodeId}", _nodeRoot, connectorLockedColor, connector);
                _connectorImages.Add(line);
                _connectorTargets.Add(to);
                PlaceConnector(line.rectTransform, from.treePosition, to.treePosition);
            }
        }

        for (int i = 0; i < tree.nodes.Count; i++)
        {
            var node = tree.nodes[i];
            if (node == null) continue;

            var go = new GameObject($"Node_{node.nodeId}", typeof(RectTransform));
            go.transform.SetParent(_nodeRoot, false);
            // 隐藏节点：不显示（但仍建对象，保证 _nodeImages/_nodeButtons 等与 tree.nodes 下标一一对应 ✅）
            if (node.hidden) go.SetActive(false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = nodeSize;
            rect.anchoredPosition = node.treePosition;

            var img = go.AddComponent<Image>();
            img.sprite = node.icon;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            _nodeImages.Add(img);

            var button = go.AddComponent<Button>();
            button.targetGraphic = img;
            int captured = i;
            button.onClick.AddListener(() => OnNodeClicked(captured));
            _nodeButtons.Add(button);

            if (node.icon == null)
                img.sprite = GetCircleSprite();

            var label = CreateText("Label", rect, node.displayName, 30f, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
            _nodeLabels.Add(label);

            var cost = CreateText("Cost", rect, node.coinCost.ToString(), 26f, TextAlignmentOptions.Center);
            var costRect = cost.rectTransform;
            costRect.anchorMin = new Vector2(0f, 0f);
            costRect.anchorMax = new Vector2(1f, 0f);
            costRect.pivot = new Vector2(0.5f, 1f);
            costRect.sizeDelta = new Vector2(0f, 34f);
            costRect.anchoredPosition = new Vector2(0f, -6f);
            _nodeCosts.Add(cost);
        }
    }

    #endregion

    #region 刷新

    private void Refresh()
    {
        if (!_built || tree == null) return;

        int coins = SaveManager.GetCoins();
        if (_coinText != null) _coinText.text = coins.ToString();

        for (int i = 0; i < _nodeImages.Count && i < tree.nodes.Count; i++)
        {
            var node = tree.nodes[i];
            if (node == null) continue;
            bool unlocked = TalentTreeState.IsUnlocked(node);
            bool available = TalentTreeState.IsAvailable(tree, node);

            var img = _nodeImages[i];
            img.sprite = unlocked ? (nodeUnlocked != null ? nodeUnlocked : GetCircleSprite())
                        : available ? (nodeAvailable != null ? nodeAvailable : GetCircleSprite())
                        : (nodeLocked != null ? nodeLocked : GetCircleSprite());
            img.color = unlocked ? unlockedColor : available ? availableColor : lockedColor;
            img.preserveAspect = true;

            if (_nodeButtons[i] != null)
                _nodeButtons[i].interactable = available || unlocked;

            if (_nodeLabels[i] != null)
                _nodeLabels[i].color = unlocked ? new Color(0.15f, 0.12f, 0.10f, 1f)
                                       : available ? new Color(0.12f, 0.10f, 0.08f, 1f)
                                       : new Color(0.62f, 0.60f, 0.56f, 1f);

            if (i < _nodeCosts.Count && _nodeCosts[i] != null)
            {
                _nodeCosts[i].text = unlocked ? "已解锁" : node.coinCost.ToString();
                _nodeCosts[i].color = unlocked ? new Color(0.55f, 0.85f, 0.55f, 1f)
                                      : coins >= node.coinCost ? new Color(0.95f, 0.82f, 0.42f, 1f)
                                      : new Color(0.85f, 0.45f, 0.42f, 1f);
            }

            if (verboseLog)
                DebugLog.Info($"[Talent] {node.nodeId} unlocked={unlocked} available={available} cost={node.coinCost} coins={coins}");
        }

        // 连线状态：目标节点解锁即点亮（按边 ✅ ✅）
        for (int i = 0; i < _connectorImages.Count && i < _connectorTargets.Count; i++)
        {
            var target = _connectorTargets[i];
            bool lit = target != null && TalentTreeState.IsUnlocked(target);
            if (_connectorImages[i] != null)
                _connectorImages[i].color = lit ? connectorUnlockedColor : connectorLockedColor;
        }
    }

    private void OnNodeClicked(int index)
    {
        if (tree == null) return;
        var node = tree.GetNode(index);
        if (node == null) return;

        var result = TalentTreeState.TryUnlock(tree, node);
        switch (result)
        {
            case TalentUnlockResult.Success:
                SetDescription(node, $"已解锁：{node.displayName}\n{node.description}");
                break;
            case TalentUnlockResult.AlreadyUnlocked:
                SetDescription(node, $"{node.displayName}\n{node.description}");
                break;
            case TalentUnlockResult.Locked:
                SetDescription(node, "需要先解锁前置节点");
                break;
            case TalentUnlockResult.NotEnoughCoins:
                SetDescription(node, $"铜钱不足：需要 {node.coinCost}，当前 {SaveManager.GetCoins()}");
                break;
            default:
                SetDescription(node, "这个节点还没有配好解锁内容");
                break;
        }
        Refresh();
    }

    private void SetDescription(TalentNodeConfig node, string text)
    {
        if (_descText != null) _descText.text = text;
    }

    #endregion

    #region 小工具

    private Image CreateImage(string name, RectTransform parent, Color color, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.sprite = sprite;
        img.raycastTarget = sprite != null;
        return img;
    }

    private TextMeshProUGUI CreateText(string name, RectTransform parent, string content, float size, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.text = content;
        text.fontSize = size;
        text.alignment = align;
        text.color = new Color(0.90f, 0.86f, 0.78f, 1f);
        text.raycastTarget = false;
        text.enableWordWrapping = true;
        return text;
    }

    private static void Stretch(RectTransform rect, float padding = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(padding, padding);
        rect.offsetMax = new Vector2(-padding, -padding);
    }

    private static void AnchorTop(RectTransform rect, float height, Vector2 position)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, height);
        rect.anchoredPosition = position;
    }

    private static void PlaceConnector(RectTransform rect, Vector2 from, Vector2 to)
    {
        Vector2 delta = to - from;
        float length = delta.magnitude;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(length, 0f);
        rect.anchoredPosition = from;
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
    }

    /// <summary>程序化圆形占位：美术给图之前节点不至于是个白方块。</summary>
    private static Sprite GetCircleSprite()
    {
        if (_circleSprite != null) return _circleSprite;

        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            name = "TalentNodeCircle_Procedural"
        };
        var px = new Color32[size * size];
        float c = (size - 1) * 0.5f;
        float outer = size * 0.5f - 1f;
        float inner = outer - 5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                if (d > outer) px[y * size + x] = new Color32(0, 0, 0, 0);
                else if (d > inner) px[y * size + x] = new Color32(255, 255, 255, 255);
                else px[y * size + x] = new Color32(255, 255, 255, 235);
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        _circleSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _circleSprite;
    }

    #endregion
}
