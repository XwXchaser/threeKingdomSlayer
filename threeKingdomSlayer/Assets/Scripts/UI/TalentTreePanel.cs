using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 天赋面板：**二维分页器**（横向 = 分类，纵向 = 同一分类内的页）。
///
/// 结构（运行时按 TalentCatalog 生成，零场景搭 UI）：
///   TalentTreePanel
///   ├ Dim
///   └ Panel
///     ├ Title / Tabs / CoinRow
///     ├ Viewport（RectMask2D 裁切）
///     │  └ Pages（位移 = (-分类 × 页宽, +页号 × 页高)）
///     │     ├ Page_c0p0 … 屏幕按网格摆放：x = 分类 × 页宽，y = -页号 × 页高
///     ├ DetailCard
///     └ CloseButton
///
/// 分页规则由 <see cref="TalentPaginator"/> 决定：按行切、**页边界不跨连线**、每页 ≤ rowsPerPage 行。
/// 因此页数由"图的缝隙"决定，改数据（新增/移动节点）后页数会自动重算。
///
/// 手势：左右拖 → 换分类；上下拖 → 类内换页；跟手位移 + 松手缓动吸附；
/// 拖拽结束 0.25s 内的点击一律忽略，避免滑动误购。
///
/// 美术：所有贴图走 SerializedField，留空用程序化占位（不做 Resources 查找）。
/// </summary>
public class TalentTreePanel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("数据（必填）")]
    public TalentTreeConfig tree;

    [Header("三分类目录（可选，填了就优先用 catalog 里对应分类的树）")]
    [Tooltip("catalog 中 categoryIndex 指定的分类优先于上面单树字段；catalog 留空或该槽留空则退回单树字段")]
    public TalentCatalog catalog;
    [Tooltip("当前分类下标：0 = 基础数值 / 1 = 招式解锁 / 2 = 三选一解锁")]
    public int categoryIndex;

    [Header("分类页签（catalog 至少填了 2 个槽位时才显示）")]
    public string[] categoryLabels = { "基础数值", "招式解锁", "三选一解锁" };

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

    [Header("程序化垫底件的覆盖槽位（留空 = 用程序化生成）")]
    [Tooltip("每页底板（九宫格）")]
    public Sprite pagePlate;
    [Tooltip("页签常态 / 选中（九宫格）")]
    public Sprite tabNormal;
    public Sprite tabActive;
    [Tooltip("页码点 常态 / 当前")]
    public Sprite dotOff;
    public Sprite dotOn;
    [Tooltip("详情卡底板（九宫格）")]
    public Sprite detailPlate;
    [Tooltip("按钮底板（九宫格）")]
    public Sprite buttonNormal;

    [Header("造型开关")]
    [Tooltip("节点用程序化金属方框（Blades 风）；关掉则用上面 nodeLocked / nodeAvailable / nodeUnlocked 槽位的图")]
    public bool preferProceduralNodes = true;
    [Tooltip("面板底/遮罩也用程序化（关掉才用场景里挂的 panelBackground / dimBackground）")]
    public bool useProceduralPanel = true;

    [Header("观测")]
    [Tooltip("右上角显示当前分类/页/节点数/已解锁数/选中项（调试用）")]
    public bool showDebugState = true;

    [Header("排版")]
    [Tooltip("节点边长（设计像素）")]
    public Vector2 nodeSize = new Vector2(160f, 160f);
    [Tooltip("连线粗细（设计像素）")]
    public float connectorThickness = 10f;
    [Tooltip("类内每页最多行数；超出则按“不跨连线”的缝隙自动分页")]
    [Min(1)] public int rowsPerPage = 8;
    [Tooltip("翻页吸附动画时长（秒）")]
    public float snapDuration = 0.22f;

    [Header("配色（美术稿下来前先用这套）")]
    public Color lockedColor = new Color(0.32f, 0.30f, 0.28f, 1f);
    public Color availableColor = new Color(0.86f, 0.70f, 0.36f, 1f);
    public Color unlockedColor = new Color(0.93f, 0.90f, 0.82f, 1f);
    public Color connectorLockedColor = new Color(0.30f, 0.28f, 0.26f, 1f);
    public Color connectorUnlockedColor = new Color(0.82f, 0.65f, 0.32f, 1f);

    [Header("调试")]
    [Tooltip("打开时在 Console 打印每次刷新的状态")]
    public bool verboseLog = false;

    /// <summary>一个屏幕 = (分类, 该分类内的第几页) 的运行时视图。</summary>
    private sealed class PageView
    {
        public int category;
        public int pageIndex;
        public TalentTreeConfig tree;
        public TalentPaginator.TalentPage page;
        public RectTransform root;
        public RectTransform nodeRoot;
        public RectTransform selectionRing;
        public readonly List<Image> nodeImages = new List<Image>();
        public readonly List<TextMeshProUGUI> nodeGlyphs = new List<TextMeshProUGUI>();
        public readonly List<Button> nodeButtons = new List<Button>();
        public readonly List<TextMeshProUGUI> nodeLabels = new List<TextMeshProUGUI>();
        public readonly List<TextMeshProUGUI> nodeCosts = new List<TextMeshProUGUI>();
        public readonly List<Image> connectorImages = new List<Image>();
        public readonly List<TalentNodeConfig> connectorTargets = new List<TalentNodeConfig>();
    }

    private RectTransform _tabRow;
    private RectTransform _viewport;
    private RectTransform _pagesRoot;
    private RectTransform _dotsRoot;
    private readonly List<PageView> _pages = new List<PageView>();
    private readonly List<Button> _tabs = new List<Button>();
    private readonly List<TextMeshProUGUI> _tabLabels = new List<TextMeshProUGUI>();
    private readonly List<Image> _tabUnderlines = new List<Image>();

    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _coinText;
    private TextMeshProUGUI _debugText;

    // 选中态 + 详情卡
    private int _selectedCategory = -1;
    private int _selectedIndex = -1;
    private string _feedback;
    private RectTransform _detailCard;
    private Image _detailIcon;
    private TextMeshProUGUI _detailName;
    private TextMeshProUGUI _detailDesc;
    private TextMeshProUGUI _detailCost;
    private TextMeshProUGUI _detailState;
    private Button _buyButton;
    private TextMeshProUGUI _buyLabel;

    // 二维分页状态
    private readonly int[] _pageIndexByCategory = new int[TalentCatalog.CategoryCount];
    private float _pageWidth = 1000f;
    private float _pageHeight = 1200f;
    private Vector2 _dragTotal;
    private bool _axisLocked;
    private bool _horizontalIntent;
    private bool _dragging;
    private float _lastDragEndTime = -1f;
    private Coroutine _snapRoutine;
    private bool _built;

    private static Sprite _circleSprite;

    private int CurrentPageIndex
    {
        get
        {
            int c = Mathf.Clamp(categoryIndex, 0, TalentCatalog.CategoryCount - 1);
            return Mathf.Clamp(_pageIndexByCategory[c], 0, Mathf.Max(0, PageCountOf(c) - 1));
        }
    }

    private void Awake()
    {
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

    /// <summary>
    /// 视口尺寸要等布局完成才准，而页宽/页高决定吸附位置。
    /// 放在 Update 同步：拖拽中 / 吸附动画中不重建，否则会当场打断动画。
    /// </summary>
    private void Update()
    {
        if (!_built || _viewport == null) return;
        if (_dragging || _snapRoutine != null) return;
        float w = _viewport.rect.width;
        float h = _viewport.rect.height;
        if (w > 10f && h > 10f && (Mathf.Abs(w - _pageWidth) > 1f || Mathf.Abs(h - _pageHeight) > 1f))
        {
            BuildPages();
            Refresh();   // 重建后必须刷一次，否则节点会停在“锁定”外观
        }
    }

    public void Toggle() => SetVisible(!gameObject.activeSelf);

    public void Open()
    {
        Build();
        SetVisible(true);
    }

    /// <summary>
    /// 关闭面板。天赋面板只存在于 overlay 场景（TalentTree.scene）里，
    /// 所以关面板必须同时把 overlay 关掉，否则会留下一个空场景、退不回战斗界面。
    /// </summary>
    public void Close()
    {
        SetVisible(false);
        var flow = SceneFlowManager.Instance;
        if (flow != null && !string.IsNullOrEmpty(flow.CurrentOverlay))
            flow.RequestCloseOverlay();
    }

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

    /// <summary>重建所有页（数据结构/分类变化都走这里）。</summary>
    public void RebuildGraph()
    {
        if (!_built) return;
        BuildPages();
        Refresh();
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

        var dim = CreateImage("Dim", self, new Color(0.02f, 0.02f, 0.02f, 0.90f), useProceduralPanel ? null : dimBackground);
        Stretch(dim.rectTransform);

        var panelSprite = useProceduralPanel
            ? ProceduralUiSprites.RoundedPlate(18, 3, 1f, 1f)
            : (panelBackground != null ? panelBackground : ProceduralUiSprites.RoundedPlate(18, 3, 1f, 1f));
        var panel = CreateImage("Panel", self, useProceduralPanel ? new Color(0.16f, 0.145f, 0.13f, 1f) : Color.white, panelSprite);
        panel.type = Image.Type.Sliced;
        Stretch(panel.rectTransform, 40f);

        _titleText = CreateText("Title", panel.rectTransform, "天  赋", 64f, TextAlignmentOptions.Center);
        AnchorTop(_titleText.rectTransform, 120f, new Vector2(0f, -80f));

        BuildTabs(panel.rectTransform);

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

        // 观测条（右上角，调试用：一眼看到当前在哪一页/多少节点/多少钱）
        _debugText = CreateText("DebugState", panel.rectTransform, "", 20f, TextAlignmentOptions.TopRight);
        var dbgRect = _debugText.rectTransform;
        dbgRect.anchorMin = new Vector2(1f, 1f);
        dbgRect.anchorMax = new Vector2(1f, 1f);
        dbgRect.pivot = new Vector2(1f, 1f);
        dbgRect.sizeDelta = new Vector2(680f, 28f);
        dbgRect.anchoredPosition = new Vector2(-72f, -118f);
        _debugText.color = new Color(0.62f, 0.58f, 0.50f, 1f);

        // 分页视口
        var viewport = new GameObject("Viewport", typeof(RectTransform));
        viewport.transform.SetParent(panel.rectTransform, false);
        _viewport = (RectTransform)viewport.transform;
        _viewport.anchorMin = new Vector2(0f, 0f);
        _viewport.anchorMax = new Vector2(1f, 1f);
        _viewport.offsetMin = new Vector2(0f, 340f);
        _viewport.offsetMax = new Vector2(0f, -300f);
        viewport.AddComponent<RectMask2D>();

        var pages = new GameObject("Pages", typeof(RectTransform));
        pages.transform.SetParent(_viewport, false);
        _pagesRoot = (RectTransform)pages.transform;
        _pagesRoot.anchorMin = Vector2.zero;
        _pagesRoot.anchorMax = Vector2.one;
        _pagesRoot.offsetMin = Vector2.zero;
        _pagesRoot.offsetMax = Vector2.zero;
        _pagesRoot.pivot = new Vector2(0.5f, 0.5f);

        // 页码点（类内多页时才显示）
        var dots = new GameObject("PageDots", typeof(RectTransform));
        dots.transform.SetParent(panel.rectTransform, false);
        _dotsRoot = (RectTransform)dots.transform;
        _dotsRoot.anchorMin = new Vector2(1f, 0.5f);
        _dotsRoot.anchorMax = new Vector2(1f, 0.5f);
        _dotsRoot.pivot = new Vector2(1f, 0.5f);
        _dotsRoot.sizeDelta = new Vector2(44f, 600f);
        _dotsRoot.anchoredPosition = new Vector2(-30f, 0f);

        BuildDetailCard(panel.rectTransform);

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

        BuildPages();

        if (catalog == null && tree == null)
            Debug.LogWarning("[Talent] TalentTreePanel 没有指定 TalentTreeConfig / TalentCatalog，面板会是空的");
    }

    /// <summary>该分类自动切页后的页数（空分类返回 0）。</summary>
    private int PageCountOf(int category)
    {
        var treeRef = catalog != null ? catalog.Get(category) : (category == 0 ? tree : null);
        if (treeRef == null) return 0;
        return TalentPaginator.Split(treeRef, rowsPerPage).Count;
    }

    /// <summary>重建全部屏幕（分类 × 类内页），并把容器移到当前 (分类, 页)。</summary>
    private void BuildPages()
    {
        if (_pagesRoot == null || _viewport == null) return;

        for (int i = _pagesRoot.childCount - 1; i >= 0; i--)
            DestroyOrImmediate(_pagesRoot.GetChild(i).gameObject);
        _pages.Clear();
        _selectedCategory = -1;
        _selectedIndex = -1;
        _feedback = null;

        _pageWidth = _viewport.rect.width;
        _pageHeight = _viewport.rect.height;
        if (_pageWidth <= 10f) _pageWidth = 1000f;
        if (_pageHeight <= 10f) _pageHeight = 1200f;

        for (int c = 0; c < TalentCatalog.CategoryCount; c++)
        {
            var treeRef = catalog != null ? catalog.Get(c) : (c == 0 ? tree : null);
            if (treeRef == null) continue;

            var problems = new List<string>();
            var pages = TalentPaginator.Split(treeRef, rowsPerPage, problems);
            for (int i = 0; i < problems.Count; i++) DebugLog.Info("[Talent] 分页提示：" + problems[i]);

            for (int pidx = 0; pidx < pages.Count; pidx++)
            {
                var page = pages[pidx];
                var pageGO = new GameObject($"Page_c{c}p{pidx}", typeof(RectTransform));
                pageGO.transform.SetParent(_pagesRoot, false);
                var pageRect = (RectTransform)pageGO.transform;
                pageRect.anchorMin = new Vector2(0.5f, 0.5f);
                pageRect.anchorMax = new Vector2(0.5f, 0.5f);
                pageRect.pivot = new Vector2(0.5f, 0.5f);
                pageRect.sizeDelta = new Vector2(_pageWidth, _pageHeight);
                pageRect.anchoredPosition = new Vector2(c * _pageWidth, -pidx * _pageHeight);

                var view = new PageView
                {
                    category = c,
                    pageIndex = pidx,
                    tree = treeRef,
                    page = page,
                    root = pageRect
                };

                // 页托板：刀锋的树直接坐在面板上，没有内层托板；只有美术提供了图才画
                if (pagePlate != null)
                {
                    var plate = CreateImage("Plate", pageRect, new Color(0.14f, 0.13f, 0.12f, 1f), pagePlate);
                    plate.type = Image.Type.Sliced;
                    plate.raycastTarget = false;
                    Stretch(plate.rectTransform, 10f);
                }

                var nr = new GameObject("Nodes", typeof(RectTransform));
                nr.transform.SetParent(pageRect, false);
                var nodeRoot = (RectTransform)nr.transform;
                nodeRoot.anchorMin = new Vector2(0.5f, 0.5f);
                nodeRoot.anchorMax = new Vector2(0.5f, 0.5f);
                nodeRoot.pivot = new Vector2(0.5f, 0.5f);
                nodeRoot.anchoredPosition = Vector2.zero;
                nodeRoot.sizeDelta = Vector2.zero;
                view.nodeRoot = nodeRoot;

                _pages.Add(view);
                BuildPageContent(view);
            }
        }

        if (catalog != null && catalog.Get(categoryIndex) == null) categoryIndex = Mathf.Max(0, FirstAvailableCategory());
        for (int c = 0; c < TalentCatalog.CategoryCount; c++)
            _pageIndexByCategory[c] = Mathf.Clamp(_pageIndexByCategory[c], 0, Mathf.Max(0, PageCountOf(c) - 1));

        JumpTo(categoryIndex, CurrentPageIndex);
        DebugLog.Info($"[Talent] 分页建成：{_pages.Count} 屏，页宽 {_pageWidth:0} 页高 {_pageHeight:0}，rowsPerPage={rowsPerPage}");
    }

    private int FirstAvailableCategory()
    {
        for (int c = 0; c < TalentCatalog.CategoryCount; c++)
            if (catalog == null ? (c == 0 && tree != null) : catalog.Get(c) != null) return c;
        return 0;
    }

    /// <summary>页内坐标 = 节点坐标 - 本页中心 y（让每页内容在自己屏里垂直居中）。</summary>
    private Vector2 NodePositionInPage(PageView view, TalentNodeConfig node)
    {
        float centerY = view.page != null ? view.page.CenterY : 0f;
        return new Vector2(node.treePosition.x, node.treePosition.y - centerY);
    }

    private void BuildPageContent(PageView view)
    {
        var treeRef = view.tree;
        if (treeRef == null || view.page == null) return;

        // 连线：每个节点 → 它的每个前置（支持分叉与汇聚）；只画两端都在本页的边
        for (int i = 0; i < view.page.nodes.Count; i++)
        {
            var to = view.page.nodes[i];
            if (to == null || to.hidden) continue;
            var prereqs = treeRef.GetPrerequisites(to);
            for (int p = 0; p < prereqs.Count; p++)
            {
                var from = prereqs[p];
                if (from == null || from.hidden) continue;
                if (!view.page.nodes.Contains(from)) continue;   // 跨页边不画（数据侧应被校验器拦住）
                var line = CreateImage($"Link_{from.nodeId}_{to.nodeId}", view.nodeRoot, connectorLockedColor, connector);
                line.raycastTarget = false;
                view.connectorImages.Add(line);
                view.connectorTargets.Add(to);
                PlaceConnector(line.rectTransform, NodePositionInPage(view, from), NodePositionInPage(view, to), connectorThickness);
            }
        }

        // 选中高亮圈
        var ring = CreateImage("SelectionRing", view.nodeRoot, new Color(1f, 0.86f, 0.4f, 0.55f), ProceduralUiSprites.Ring(128, 12, 1f));
        ring.raycastTarget = false;
        ring.rectTransform.sizeDelta = new Vector2(nodeSize.x * 1.34f, nodeSize.y * 1.34f);
        ring.gameObject.SetActive(false);
        view.selectionRing = ring.rectTransform;

        // 节点
        for (int i = 0; i < view.page.nodes.Count; i++)
        {
            var node = view.page.nodes[i];
            if (node == null) continue;

            var go = new GameObject($"Node_{node.nodeId}", typeof(RectTransform));
            go.transform.SetParent(view.nodeRoot, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = nodeSize;
            rect.anchoredPosition = NodePositionInPage(view, node);

            var img = go.AddComponent<Image>();
            img.sprite = NodeSprite(false, false);
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.raycastTarget = true;
            view.nodeImages.Add(img);

            var button = go.AddComponent<Button>();
            button.targetGraphic = img;
            var capturedView = view;
            int capturedNode = i;
            button.onClick.AddListener(() => OnNodeClicked(capturedView, capturedNode));
            view.nodeButtons.Add(button);

            // 图标：优先节点自带的图，其次它所解锁的升级条目自带的图；都没有就用首字当“刻字图形”
            var iconSprite = TalentNodeInfo.ResolveIcon(node);
            if (iconSprite != null)
            {
                var ic = CreateImage("Icon", rect, Color.white, iconSprite);
                ic.raycastTarget = false;
                ic.preserveAspect = true;
                var ir = ic.rectTransform;
                ir.anchorMin = new Vector2(0.5f, 0.5f);
                ir.anchorMax = new Vector2(0.5f, 0.5f);
                ir.pivot = new Vector2(0.5f, 0.5f);
                ir.sizeDelta = new Vector2(nodeSize.x * 0.46f, nodeSize.y * 0.46f);
                ir.anchoredPosition = new Vector2(0f, nodeSize.y * 0.16f);
                view.nodeGlyphs.Add(null);
            }
            else
            {
                var glyph = CreateText("Glyph", rect, FirstGlyph(node.displayName), 48f, TextAlignmentOptions.Center);
                var gr = glyph.rectTransform;
                gr.anchorMin = new Vector2(0.5f, 0.5f);
                gr.anchorMax = new Vector2(0.5f, 0.5f);
                gr.pivot = new Vector2(0.5f, 0.5f);
                gr.sizeDelta = new Vector2(nodeSize.x * 0.66f, nodeSize.y * 0.5f);
                gr.anchoredPosition = new Vector2(0f, nodeSize.y * 0.18f);
                view.nodeGlyphs.Add(glyph);
            }

            // 名称：框内下沿
            var label = CreateText("Label", rect, node.displayName, 22f, TextAlignmentOptions.Center);
            var lr = label.rectTransform;
            lr.anchorMin = new Vector2(0f, 0f);
            lr.anchorMax = new Vector2(1f, 0f);
            lr.pivot = new Vector2(0.5f, 0f);
            lr.sizeDelta = new Vector2(-10f, 26f);
            lr.anchoredPosition = new Vector2(0f, 6f);
            view.nodeLabels.Add(label);

            // 价格：框内上沿
            var cost = CreateText("Cost", rect, node.coinCost.ToString(), 18f, TextAlignmentOptions.Center);
            var costRect = cost.rectTransform;
            costRect.anchorMin = new Vector2(0f, 1f);
            costRect.anchorMax = new Vector2(1f, 1f);
            costRect.pivot = new Vector2(0.5f, 1f);
            costRect.sizeDelta = new Vector2(-10f, 22f);
            costRect.anchoredPosition = new Vector2(0f, -5f);
            view.nodeCosts.Add(cost);

            if (node.hidden) go.SetActive(false);
        }
    }

    private void BuildTabs(RectTransform parent)
    {
        _tabs.Clear();
        _tabLabels.Clear();
        if (catalog == null) return;

        int count = 0;
        for (int i = 0; i < TalentCatalog.CategoryCount; i++)
            if (catalog.Get(i) != null) count++;
        if (count <= 1) return;

        var row = new GameObject("Tabs", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        _tabRow = (RectTransform)row.transform;
        _tabRow.anchorMin = new Vector2(0f, 1f);
        _tabRow.anchorMax = new Vector2(1f, 1f);
        _tabRow.pivot = new Vector2(0.5f, 1f);
        _tabRow.sizeDelta = new Vector2(-160f, 96f);
        _tabRow.anchoredPosition = new Vector2(0f, -196f);

        float gap = 16f;
        float width = (1000f - 160f - gap * (TalentCatalog.CategoryCount - 1)) / TalentCatalog.CategoryCount;
        for (int i = 0; i < TalentCatalog.CategoryCount; i++)
        {
            if (catalog.Get(i) == null) continue;
            string label = (categoryLabels != null && i < categoryLabels.Length && !string.IsNullOrEmpty(categoryLabels[i]))
                ? categoryLabels[i]
                : $"分类 {i}";
            // 页签 = 纯文字 + 选中下划线（刀锋就是这么做的，不放牌子）
            var hit = CreateImage($"Tab_{i}", _tabRow, new Color(1f, 1f, 1f, 0f), null);
            hit.raycastTarget = true;
            var rect = hit.rectTransform;
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(width, 88f);
            rect.anchoredPosition = new Vector2(i * (width + gap), 0f);

            var button = hit.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            int captured = i;
            button.onClick.AddListener(() => OnTabClicked(captured));
            _tabs.Add(button);

            var text = CreateText("Label", rect, label, 34f, TextAlignmentOptions.Center);
            var tr = text.rectTransform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(0f, 10f);
            tr.offsetMax = new Vector2(0f, -10f);
            _tabLabels.Add(text);

            var underline = CreateImage("Underline", rect, availableColor, null);
            underline.raycastTarget = false;
            var ur = underline.rectTransform;
            ur.anchorMin = new Vector2(0.10f, 0f);
            ur.anchorMax = new Vector2(0.90f, 0f);
            ur.pivot = new Vector2(0.5f, 0f);
            ur.sizeDelta = new Vector2(0f, 4f);
            ur.anchoredPosition = new Vector2(0f, 2f);
            _tabUnderlines.Add(underline);
        }
    }

    private void BuildDetailCard(RectTransform parent)
    {
        var card = CreateImage("DetailCard", parent, new Color(0.12f, 0.10f, 0.08f, 0.97f),
            detailPlate != null ? detailPlate : ProceduralUiSprites.RoundedPlate(22, 4, 0.95f, 1f));
        card.type = Image.Type.Sliced;
        card.raycastTarget = true;
        _detailCard = card.rectTransform;
        _detailCard.anchorMin = new Vector2(0f, 0f);
        _detailCard.anchorMax = new Vector2(1f, 0f);
        _detailCard.pivot = new Vector2(0.5f, 0f);
        _detailCard.sizeDelta = new Vector2(-120f, 300f);
        _detailCard.anchoredPosition = new Vector2(0f, 24f);

        _detailIcon = CreateImage("Icon", _detailCard, Color.white, null);
        var ir = _detailIcon.rectTransform;
        ir.anchorMin = new Vector2(0f, 1f);
        ir.anchorMax = new Vector2(0f, 1f);
        ir.pivot = new Vector2(0f, 1f);
        ir.sizeDelta = new Vector2(96f, 96f);
        ir.anchoredPosition = new Vector2(24f, -22f);
        _detailIcon.preserveAspect = true;

        _detailName = CreateText("Name", _detailCard, "", 40f, TextAlignmentOptions.TopLeft);
        var nr = _detailName.rectTransform;
        nr.anchorMin = new Vector2(0f, 1f);
        nr.anchorMax = new Vector2(1f, 1f);
        nr.pivot = new Vector2(0f, 1f);
        nr.offsetMin = new Vector2(136f, -72f);
        nr.offsetMax = new Vector2(-24f, -18f);

        // 一条细分割线（刀锋的卡片就靠这根线分主次，不加花边）
        var divider = CreateImage("Divider", _detailCard, new Color(0.85f, 0.65f, 0.45f, 0.5f), null);
        divider.raycastTarget = false;
        var dvr = divider.rectTransform;
        dvr.anchorMin = new Vector2(0f, 1f);
        dvr.anchorMax = new Vector2(1f, 1f);
        dvr.pivot = new Vector2(0.5f, 1f);
        dvr.sizeDelta = new Vector2(-48f, 2f);
        dvr.anchoredPosition = new Vector2(0f, -74f);

        _detailDesc = CreateText("Desc", _detailCard, "", 26f, TextAlignmentOptions.TopLeft);
        var dr = _detailDesc.rectTransform;
        dr.anchorMin = new Vector2(0f, 1f);
        dr.anchorMax = new Vector2(1f, 1f);
        dr.pivot = new Vector2(0f, 1f);
        dr.offsetMin = new Vector2(136f, -178f);
        dr.offsetMax = new Vector2(-24f, -76f);

        _detailState = CreateText("State", _detailCard, "", 26f, TextAlignmentOptions.TopLeft);
        var sr = _detailState.rectTransform;
        sr.anchorMin = new Vector2(0f, 1f);
        sr.anchorMax = new Vector2(1f, 1f);
        sr.pivot = new Vector2(0f, 1f);
        sr.offsetMin = new Vector2(24f, -226f);
        sr.offsetMax = new Vector2(-320f, -182f);
        _detailState.color = new Color(0.80f, 0.72f, 0.55f, 1f);

        _detailCost = CreateText("Cost", _detailCard, "", 30f, TextAlignmentOptions.BottomLeft);
        var cr = _detailCost.rectTransform;
        cr.anchorMin = new Vector2(0f, 0f);
        cr.anchorMax = new Vector2(0f, 0f);
        cr.pivot = new Vector2(0f, 0f);
        cr.sizeDelta = new Vector2(360f, 52f);
        cr.anchoredPosition = new Vector2(24f, 24f);

        var buy = CreateImage("BuyButton", _detailCard, new Color(0.46f, 0.35f, 0.18f, 1f),
            buttonNormal != null ? buttonNormal : ProceduralUiSprites.RoundedPlate(16, 3, 0.9f, 1f));
        buy.type = Image.Type.Sliced;
        buy.raycastTarget = true;
        var br = buy.rectTransform;
        br.anchorMin = new Vector2(1f, 0f);
        br.anchorMax = new Vector2(1f, 0f);
        br.pivot = new Vector2(1f, 0f);
        br.sizeDelta = new Vector2(280f, 88f);
        br.anchoredPosition = new Vector2(-24f, 22f);
        _buyButton = buy.gameObject.AddComponent<Button>();
        _buyButton.targetGraphic = buy;
        _buyButton.onClick.AddListener(OnBuyClicked);
        _buyLabel = CreateText("Label", br, "解锁", 34f, TextAlignmentOptions.Center);
        Stretch(_buyLabel.rectTransform);

        _detailCard.gameObject.SetActive(false);
    }

    #endregion

    #region 刷新

    private void Refresh()
    {
        if (!_built) return;

        int coins = SaveManager.GetCoins();
        if (_coinText != null) _coinText.text = coins.ToString();
        RefreshTabs();
        RefreshPageDots();
        RefreshDebugState();

        for (int p = 0; p < _pages.Count; p++)
        {
            var view = _pages[p];
            if (view.tree == null || view.page == null) continue;

            for (int i = 0; i < view.nodeImages.Count && i < view.page.nodes.Count; i++)
            {
                var node = view.page.nodes[i];
                if (node == null) continue;
                bool unlocked = TalentTreeState.IsUnlocked(node);
                bool available = TalentTreeState.IsAvailable(view.tree, node);

                var img = view.nodeImages[i];
                img.sprite = NodeSprite(unlocked, available);
                img.color = preferProceduralNodes
                    ? Color.white
                    : (unlocked ? unlockedColor : available ? availableColor : lockedColor);
                img.preserveAspect = true;

                if (view.nodeButtons[i] != null)
                    view.nodeButtons[i].interactable = available || unlocked;

                if (view.nodeLabels[i] != null)
                    view.nodeLabels[i].color = unlocked ? new Color(0.15f, 0.12f, 0.10f, 1f)
                                               : available ? new Color(0.12f, 0.10f, 0.08f, 1f)
                                               : new Color(0.62f, 0.60f, 0.56f, 1f);

                if (i < view.nodeGlyphs.Count && view.nodeGlyphs[i] != null)
                {
                    view.nodeGlyphs[i].color = unlocked ? new Color(1.00f, 0.96f, 0.86f, 1f)
                                               : available ? new Color(0.99f, 0.85f, 0.50f, 1f)
                                               : new Color(0.58f, 0.55f, 0.50f, 1f);
                }

                if (i < view.nodeCosts.Count && view.nodeCosts[i] != null)
                {
                    view.nodeCosts[i].text = unlocked ? "已解锁" : node.coinCost.ToString();
                    view.nodeCosts[i].color = unlocked ? new Color(0.55f, 0.85f, 0.55f, 1f)
                                          : coins >= node.coinCost ? new Color(0.95f, 0.82f, 0.42f, 1f)
                                          : new Color(0.85f, 0.45f, 0.42f, 1f);
                }
            }

            for (int i = 0; i < view.connectorImages.Count && i < view.connectorTargets.Count; i++)
            {
                var target = view.connectorTargets[i];
                bool lit = target != null && TalentTreeState.IsUnlocked(target);
                if (view.connectorImages[i] != null)
                    view.connectorImages[i].color = lit ? connectorUnlockedColor : connectorLockedColor;
            }

            RefreshSelection(view);
        }

        RefreshDetail();
    }

    private void RefreshSelection(PageView view)
    {
        if (view.selectionRing == null) return;
        bool show = view.category == _selectedCategory
                    && _selectedCategory >= 0
                    && view.tree != null
                    && view.page != null
                    && _selectedIndex >= 0
                    && _selectedIndex < view.page.nodes.Count
                    && view.page.nodes[_selectedIndex] != null
                    && !view.page.nodes[_selectedIndex].hidden;
        view.selectionRing.gameObject.SetActive(show);
        if (show) view.selectionRing.anchoredPosition = NodePositionInPage(view, view.page.nodes[_selectedIndex]);
    }

    private void RefreshTabs()
    {
        for (int i = 0; i < _tabs.Count; i++)
        {
            var button = _tabs[i];
            if (button == null) continue;
            var img = button.targetGraphic as Image;
            bool active = i == categoryIndex;
            if (img != null) img.color = new Color(1f, 1f, 1f, 0f);   // 只做点击区，不画底
            if (i < _tabLabels.Count && _tabLabels[i] != null)
                _tabLabels[i].color = active ? new Color(0.94f, 0.89f, 0.80f, 1f) : new Color(0.55f, 0.51f, 0.46f, 1f);
            if (i < _tabUnderlines.Count && _tabUnderlines[i] != null)
                _tabUnderlines[i].gameObject.SetActive(active);
            if (i < _tabLabels.Count && _tabLabels[i] != null)
                _tabLabels[i].color = active ? new Color(0.12f, 0.10f, 0.08f, 1f) : new Color(0.80f, 0.76f, 0.70f, 1f);
        }
    }

    private TalentNodeConfig SelectedNode()
    {
        if (_selectedCategory < 0) return null;
        var treeRef = catalog != null ? catalog.Get(_selectedCategory) : null;
        if (treeRef == null || _selectedIndex < 0) return null;
        for (int i = 0; i < _pages.Count; i++)
        {
            var v = _pages[i];
            if (v.category != _selectedCategory) continue;
            if (_selectedIndex >= 0 && _selectedIndex < v.page.nodes.Count) return v.page.nodes[_selectedIndex];
        }
        return null;
    }

    /// <summary>页码点：当前分类内多页时竖排显示在右侧（一页时不显示）。</summary>
    private void RefreshPageDots()
    {
        if (_dotsRoot == null) return;
        for (int i = _dotsRoot.childCount - 1; i >= 0; i--)
            DestroyOrImmediate(_dotsRoot.GetChild(i).gameObject);

        int count = Mathf.Max(1, PageCountOf(categoryIndex));
        _dotsRoot.gameObject.SetActive(count > 1);
        if (count <= 1) return;

        int current = CurrentPageIndex;
        float spacing = 46f;
        float total = (count - 1) * spacing;
        for (int i = 0; i < count; i++)
        {
            bool on = i == current;
            var sprite = on
                ? (dotOn != null ? dotOn : ProceduralUiSprites.Dot(28))
                : (dotOff != null ? dotOff : ProceduralUiSprites.Dot(20));
            var dot = CreateImage("Dot_" + i, _dotsRoot,
                on ? new Color(0.95f, 0.80f, 0.45f, 1f) : new Color(0.55f, 0.50f, 0.44f, 1f), sprite);
            dot.raycastTarget = false;
            var r = dot.rectTransform;
            r.anchorMin = new Vector2(0.5f, 0.5f);
            r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            float size = on ? 26f : 18f;
            r.sizeDelta = new Vector2(size, size);
            r.anchoredPosition = new Vector2(0f, total * 0.5f - i * spacing);
        }
    }

    /// <summary>节点用图：程序化金属方框（Blades 风）优先；关掉开关后用槽位里的三态图。</summary>
    /// <summary>没有图标时用名字首字当“刻字图形”。</summary>
    private static string FirstGlyph(string name)
    {
        if (string.IsNullOrEmpty(name)) return "?";
        var c = name[0];
        return char.IsLetter(c) ? char.ToUpperInvariant(c).ToString() : c.ToString();
    }

    private Sprite NodeSprite(bool unlocked, bool available)
    {
        if (!preferProceduralNodes)
        {
            var slot = unlocked ? nodeUnlocked : available ? nodeAvailable : nodeLocked;
            if (slot != null) return slot;
        }
        return ProceduralUiSprites.NodeFrame(128, unlocked, available);
    }

    private int CountNodesOf(int category)
    {
        int n = 0;
        for (int i = 0; i < _pages.Count; i++)
            if (_pages[i].category == category && _pages[i].page != null) n += _pages[i].page.nodes.Count;
        return n;
    }

    /// <summary>观测条：当前在哪一页、多少节点、多少钱、选中了谁。</summary>
    private void RefreshDebugState()
    {
        if (_debugText == null) return;
        _debugText.gameObject.SetActive(showDebugState);
        if (!showDebugState) return;

        int pageCount = Mathf.Max(1, PageCountOf(categoryIndex));
        int unlockedCount = TalentSaveSection.GetUnlocked(SaveManager.Load()).Count;
        var node = SelectedNode();
        _debugText.text =
            $"分类 {categoryIndex + 1}/{TalentCatalog.CategoryCount} · 第 {CurrentPageIndex + 1}/{pageCount} 页" +
            $" · 本类节点 {CountNodesOf(categoryIndex)} · 已解锁 {unlockedCount}" +
            $" · 铜钱 {SaveManager.GetCoins()} · 选中 {(node == null ? "无" : node.displayName)}";
    }

    private void RefreshDetail()
    {
        if (_detailCard == null) return;
        var node = SelectedNode();
        var treeRef = _selectedCategory >= 0 && catalog != null ? catalog.Get(_selectedCategory) : null;
        if (node == null || treeRef == null || node.hidden)
        {
            _detailCard.gameObject.SetActive(false);
            return;
        }
        _detailCard.gameObject.SetActive(true);

        bool unlocked = TalentTreeState.IsUnlocked(node);
        var miss = new List<string>();
        foreach (var p in treeRef.GetPrerequisites(node))
            if (!TalentTreeState.IsUnlocked(p)) miss.Add(p.displayName);
        int coins = SaveManager.GetCoins();
        bool canBuy = !unlocked && miss.Count == 0 && coins >= node.coinCost && node.HasValidUnlockPayload;

        if (_detailIcon != null)
        {
            var icon = TalentNodeInfo.ResolveIcon(node);
            _detailIcon.sprite = icon != null ? icon : ProceduralUiSprites.Ring(128, 8, 1f);
            _detailIcon.color = unlocked ? unlockedColor : canBuy ? availableColor : lockedColor;
        }
        if (_detailName != null) _detailName.text = node.displayName;
        if (_detailDesc != null) _detailDesc.text = TalentNodeInfo.ResolveDescription(node);
        if (_detailCost != null)
            _detailCost.text = unlocked ? "已解锁"
                : node.coinCost > 0 ? $"花费 {node.coinCost} 铜钱"
                : "免费";

        string state;
        if (!string.IsNullOrEmpty(_feedback)) state = _feedback;
        else if (unlocked) state = "已解锁";
        else if (!node.HasValidUnlockPayload) state = "这个节点还没有配好解锁内容";
        else if (miss.Count > 0) state = "前置未解锁：" + string.Join("、", miss);
        else if (coins < node.coinCost) state = $"铜钱不足（当前 {coins}，需要 {node.coinCost}）";
        else state = "可以解锁";
        if (_detailState != null) _detailState.text = state;

        if (_buyButton != null)
        {
            _buyButton.interactable = canBuy;
            var img = _buyButton.targetGraphic as Image;
            if (img != null) img.color = canBuy ? new Color(0.46f, 0.35f, 0.18f, 1f) : new Color(0.20f, 0.18f, 0.16f, 1f);
        }
        if (_buyLabel != null)
            _buyLabel.text = unlocked ? "已解锁" : canBuy ? $"解锁 {node.coinCost}" : "不可解锁";
    }

    #endregion

    #region 交互

    private void OnNodeClicked(PageView view, int index)
    {
        // 拖拽结束后的误触：抬手那一帧会先派发 click
        if (Time.unscaledTime - _lastDragEndTime < 0.25f) return;
        if (_axisLocked && _dragTotal.sqrMagnitude > 24f * 24f) return;

        _selectedCategory = view.category;
        _selectedIndex = index;
        _feedback = null;
        Refresh();
    }

    /// <summary>详情卡的确认按钮：真正的解锁（扣钱）只在这里发生。</summary>
    private void OnBuyClicked()
    {
        var node = SelectedNode();
        var treeRef = _selectedCategory >= 0 && catalog != null ? catalog.Get(_selectedCategory) : null;
        if (node == null || treeRef == null) return;

        var result = TalentTreeState.TryUnlock(treeRef, node);
        switch (result)
        {
            case TalentUnlockResult.Success:
                _feedback = $"已解锁：{node.displayName}";
                break;
            case TalentUnlockResult.AlreadyUnlocked:
                _feedback = "已经解锁过了";
                break;
            case TalentUnlockResult.Locked:
                _feedback = "前置节点还没解锁";
                break;
            case TalentUnlockResult.NotEnoughCoins:
                _feedback = $"铜钱不足：需要 {node.coinCost}，当前 {SaveManager.GetCoins()}";
                break;
            default:
                _feedback = "这个节点还没有配好解锁内容";
                break;
        }
        Refresh();
    }

    private void OnTabClicked(int index)
    {
        if (catalog == null || catalog.Get(index) == null) return;
        if (index == categoryIndex) return;
        categoryIndex = index;
        _selectedCategory = -1;
        _selectedIndex = -1;
        _feedback = null;
        Refresh();
        StartSnap(index, CurrentPageIndex);
    }

    private void JumpTo(int category, int page)
    {
        if (_pagesRoot == null) return;
        categoryIndex = Mathf.Clamp(category, 0, TalentCatalog.CategoryCount - 1);
        _pageIndexByCategory[categoryIndex] = Mathf.Max(0, page);
        _pagesRoot.anchoredPosition = new Vector2(-categoryIndex * _pageWidth, CurrentPageIndex * _pageHeight);
    }

    private void StartSnap(int category, int page)
    {
        if (_pagesRoot == null) return;
        if (_snapRoutine != null) StopCoroutine(_snapRoutine);
        _snapRoutine = StartCoroutine(SnapTo(category, Mathf.Max(0, page)));
    }

    private IEnumerator SnapTo(int category, int page)
    {
        Vector2 from = _pagesRoot.anchoredPosition;
        Vector2 to = new Vector2(-category * _pageWidth, page * _pageHeight);
        float duration = Mathf.Max(0.01f, snapDuration);
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            float eased = 1f - Mathf.Pow(1f - k, 3f);
            _pagesRoot.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
            yield return null;
        }
        _pagesRoot.anchoredPosition = to;
        _snapRoutine = null;
    }

    #endregion

    #region 两轴手势

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_snapRoutine != null) { StopCoroutine(_snapRoutine); _snapRoutine = null; }
        _dragging = true;
        _dragTotal = Vector2.zero;
        _axisLocked = false;
        _horizontalIntent = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        _dragTotal += eventData.delta;

        if (!_axisLocked && _dragTotal.sqrMagnitude > 144f)   // 12px 以上才判主轴
        {
            _axisLocked = true;
            _horizontalIntent = Mathf.Abs(_dragTotal.x) >= Mathf.Abs(_dragTotal.y);
        }
        if (!_axisLocked || _pagesRoot == null) return;

        if (_horizontalIntent)
            _pagesRoot.anchoredPosition = new Vector2(-categoryIndex * _pageWidth + DampX(_dragTotal.x), _pagesRoot.anchoredPosition.y);
        else
            _pagesRoot.anchoredPosition = new Vector2(_pagesRoot.anchoredPosition.x, CurrentPageIndex * _pageHeight - DampY(_dragTotal.y));
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _lastDragEndTime = Time.unscaledTime;
        _dragging = false;
        if (!_axisLocked || _pagesRoot == null)
        {
            _dragTotal = Vector2.zero;
            return;
        }

        int targetCategory = categoryIndex;
        int targetPage = CurrentPageIndex;

        if (_horizontalIntent)
        {
            float threshold = _pageWidth * 0.18f;
            if (_dragTotal.x <= -threshold) targetCategory = FindCategory(categoryIndex, 1);
            else if (_dragTotal.x >= threshold) targetCategory = FindCategory(categoryIndex, -1);
        }
        else
        {
            float threshold = _pageHeight * 0.18f;
            if (_dragTotal.y <= -threshold) targetPage = Mathf.Min(targetPage + 1, Mathf.Max(0, PageCountOf(categoryIndex) - 1));
            else if (_dragTotal.y >= threshold) targetPage = Mathf.Max(targetPage - 1, 0);
        }
        _dragTotal = Vector2.zero;

        if (targetCategory != categoryIndex)
        {
            categoryIndex = targetCategory;
            _selectedCategory = -1;
            _selectedIndex = -1;
            _feedback = null;
            Refresh();
        }
        _pageIndexByCategory[Mathf.Clamp(categoryIndex, 0, TalentCatalog.CategoryCount - 1)] = targetPage;
        StartSnap(categoryIndex, targetPage);
    }

    private float DampX(float dx)
    {
        bool atFirst = FindCategory(categoryIndex, -1) == categoryIndex && dx > 0f;
        bool atLast = FindCategory(categoryIndex, 1) == categoryIndex && dx < 0f;
        return (atFirst || atLast) ? dx * 0.35f : dx;
    }

    private float DampY(float dy)
    {
        int count = Mathf.Max(1, PageCountOf(categoryIndex));
        int page = CurrentPageIndex;
        bool atFirst = page <= 0 && dy > 0f;
        bool atLast = page >= count - 1 && dy < 0f;
        return (atFirst || atLast) ? dy * 0.35f : dy;
    }

    private int FindCategory(int from, int dir)
    {
        if (dir == 0) return from;
        for (int step = 1; step <= TalentCatalog.CategoryCount; step++)
        {
            int next = from + dir * step;
            if (next < 0 || next >= TalentCatalog.CategoryCount) return from;
            bool has = catalog != null ? catalog.Get(next) != null : (next == 0 && tree != null);
            if (has) return next;
        }
        return from;
    }

    #endregion

    #region 小工具

    private static void PlaceConnector(RectTransform rect, Vector2 from, Vector2 to, float thickness)
    {
        Vector2 delta = to - from;
        float length = delta.magnitude;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        // 高度不能是 0：Image 在零高矩形上什么都不画
        rect.sizeDelta = new Vector2(length, Mathf.Max(2f, thickness));
        rect.anchoredPosition = from;
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
    }

    private static void DestroyOrImmediate(GameObject go)
    {
        if (go == null) return;
        if (Application.isPlaying) Destroy(go);
        else DestroyImmediate(go);
    }

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
