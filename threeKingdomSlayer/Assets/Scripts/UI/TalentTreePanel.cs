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

    [Header("V6 面板补件（2026-11-03 美术素材包；留空 = 不生成）")]
    [Tooltip("顶部标题牌（九宫格）：垫在标题文字下面")]
    public Sprite titlePlaque;
    [Tooltip("铜钱金额底板（九宫格）：垫在铜钱行下面；美术包里它与页签常态图字节相同，可复用同一个 Sprite")]
    public Sprite coinPlate;
    [Tooltip("顶栏装饰（旗饰）：纯装饰，不拦点击")]
    public Sprite bannerDecoration;
    [Tooltip("返回箭头：叠在关闭按钮上（保留原来的文字与点击区）")]
    public Sprite backArrow;
    [Tooltip("选中角标：替代程序化的圆形高亮圈；留空 = 仍用程序化圆环")]
    public Sprite selectionCorners;

    [Header("首次打开的天赋页教学")]
    [Tooltip("教学用的手指贴图（留空 = 只画光圈不画手）")]
    public Sprite teachingHandSprite;
    [Tooltip("教学用的光圈贴图（留空 = 程序化圆环）")]
    public Sprite teachingRingSprite;
    [Tooltip("教学手的帧序列（留空 = 只做程序化按压）")]
    public Sprite[] teachingHandFrames;
    [Tooltip("教学文案 1：指向一个「现在就能解锁」的节点")]
    public string teachingTextPickNode = "点一个天赋节点 —— 花铜钱解锁它";
    [Tooltip("教学文案 2：指向详情卡里的【解锁】按钮")]
    public string teachingTextConfirm = "再点【解锁】确认 —— 天赋就生效了";

    // ── 天赋页教学状态（2026-11-04）────────────────────────────
    private bool _teachActive;
    private int _teachStep;                 // 0 = 指向节点；1 = 指向确认按钮
    private RectTransform _teachLayer;
    private TutorialClickMask _teachMask;
    private Image _teachHand;
    private Image _teachRing;
    private TextMeshProUGUI _teachLabel;
    private RectTransform _teachLabelBg;
    private Vector3 _teachTargetLocal;
    private float _teachAnimTime;

    [Header("竖向树版式（2026-11-03）")]
    [Tooltip("各泳道的分支牌（按泳道序号 0/1/2 取；留空 = 不画）。美术包里是空底板，图标另算。")]
    public Sprite[] branchPlaques;
    [Tooltip("泳道底板色（半透明）；置 alpha=0 = 不画")]
    public Color laneBackgroundColor = new Color(0.06f, 0.07f, 0.10f, 0.55f);
    [Tooltip("泳道宽度（设计像素）")]
    public float laneWidth = 270f;
    [Tooltip("分支牌尺寸（设计像素）")]
    public float lanePlaqueWidth = 300f;
    public float lanePlaqueHeight = 188f;

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
    /// <summary>面板左上角的「返回」按钮（天赋教学的第 3 步要指它）。</summary>
    private RectTransform _closeButtonRect;
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
    /// <summary>选中节点所在页（与分类 + 页内序号一起定位选中；只记序号会取错节点）。</summary>
    private int _selectedPage = -1;

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
        // 面板一显示就可能开教学（天赋页是从 overlay 直接激活的，**不走 Open()** → 教学会漏启动）
        TryStartTeaching();
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
        UpdateTeachingAnimation();   // 教学手势的按压动画
    }

    public void Toggle() => SetVisible(!gameObject.activeSelf);

    public void Open()
    {
        Build();
        SetVisible(true);
        TryStartTeaching();
    }

    /// <summary>
    /// 关闭面板。天赋面板只存在于 overlay 场景（TalentTree.scene）里，
    /// 所以关面板必须同时把 overlay 关掉，否则会留下一个空场景、退不回战斗界面。
    /// </summary>
    public void Close()
    {
        // 关闭面板 = **放弃这次教学**（不写标记 → 下次打开还会教 ✅）
        // 但教学的第 3 步就是「点返回」→ 这时关闭面板 = **教学完成** ✅
        if (_teachActive && _teachStep == 2) CompleteTeaching();
        else StopTeaching();
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

        // 标题牌（美术）：垫在标题文字下面（固定宽度，用美术原比例；不能用 AnchorTop 的拉伸锚点）
        if (titlePlaque != null)
        {
            var plaque = CreateImage("TitlePlaque", panel.rectTransform, Color.white, titlePlaque);
            plaque.type = Image.Type.Sliced;
            plaque.raycastTarget = false;
            var pr = plaque.rectTransform;
            pr.anchorMin = new Vector2(0.5f, 1f);
            pr.anchorMax = new Vector2(0.5f, 1f);
            pr.pivot = new Vector2(0.5f, 1f);
            pr.sizeDelta = new Vector2(440f, 110f);
            pr.anchoredPosition = new Vector2(0f, -64f);
            plaque.transform.SetAsFirstSibling();
        }

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
        coinRowRect.anchoredPosition = new Vector2(-40f, -72f);
        coinRowRect.sizeDelta = new Vector2(200f, 60f);

        // 铜钱金额底板（美术）：垫在铜钱行最底层
        if (coinPlate != null)
        {
            var plate = CreateImage("CoinPlate", coinRowRect, Color.white, coinPlate);
            plate.type = Image.Type.Sliced;
            plate.raycastTarget = false;
            Stretch(plate.rectTransform);
            plate.transform.SetAsFirstSibling();
        }

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
        dbgRect.anchoredPosition = new Vector2(-72f, -20f);
        _debugText.color = new Color(0.62f, 0.58f, 0.50f, 1f);

        // 分页视口
        var viewport = new GameObject("Viewport", typeof(RectTransform));
        viewport.transform.SetParent(panel.rectTransform, false);
        _viewport = (RectTransform)viewport.transform;
        _viewport.anchorMin = new Vector2(0f, 0f);
        _viewport.anchorMax = new Vector2(1f, 1f);
        _viewport.offsetMin = new Vector2(0f, 340f);
        _viewport.offsetMax = new Vector2(0f, -340f);
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
        _closeButtonRect = closeRect;
        closeRect.anchorMin = new Vector2(0f, 1f);
        closeRect.anchorMax = new Vector2(0f, 1f);
        closeRect.pivot = new Vector2(0f, 1f);
        closeRect.sizeDelta = new Vector2(140f, 70f);
        closeRect.anchoredPosition = new Vector2(34f, -150f);
        close.raycastTarget = true;
        close.transform.SetAsLastSibling();
        var closeButton = close.gameObject.AddComponent<Button>();
        closeButton.targetGraphic = close;
        closeButton.onClick.AddListener(Close);
        var closeLabel = CreateText("Text", closeRect, "返回", 40f, TextAlignmentOptions.Center);
        Stretch(closeLabel.rectTransform);
        // 关闭按钮底板是浅色（0.85,0.75,0.55）→ 文字用深色，否则白字压浅底看不清
        closeLabel.color = new Color(0.16f, 0.13f, 0.10f, 1f);

        // 返回箭头（美术）：叠在「返回」文字左边，只装饰不改点击区
        if (backArrow != null)
        {
            var arrow = CreateImage("BackArrow", closeRect, Color.white, backArrow);
            arrow.raycastTarget = false;
            arrow.preserveAspect = true;
            var ar = arrow.rectTransform;
            ar.anchorMin = new Vector2(0f, 0.5f);
            ar.anchorMax = new Vector2(0f, 0.5f);
            ar.pivot = new Vector2(0f, 0.5f);
            ar.sizeDelta = new Vector2(44f, 44f);
            ar.anchoredPosition = new Vector2(6f, 0f);
        }

        // 顶栏旗饰（美术）：纯装饰，不拦点击
        if (bannerDecoration != null)
        {
            var banner = CreateImage("BannerDecoration", panel.rectTransform, Color.white, bannerDecoration);
            banner.raycastTarget = false;
            banner.preserveAspect = true;
            var brf = banner.rectTransform;
            brf.anchorMin = new Vector2(0f, 1f);
            brf.anchorMax = new Vector2(0f, 1f);
            brf.pivot = new Vector2(0f, 1f);
            brf.sizeDelta = new Vector2(100f, 114f);
            brf.anchoredPosition = new Vector2(14f, -26f);
        }

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
        _selectedPage = -1;
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
                // 页内容裁剪到本页范围：分支牌/底板/连线超出本页就会被裁掉，
                // 不会跑到相邻那一页（= 另一段树）上面去
                nodeRoot.sizeDelta = new Vector2(_pageWidth, _pageHeight);
                nodeRoot.gameObject.AddComponent<RectMask2D>();
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

        BuildLanes(view);

        // 连线：每个节点 → 它的每个前置（支持分叉与汇聚）
        // 竖向树的连线用「竖段 + 斜段」的肘形（参考图），跨页的边画续接短线。
        for (int i = 0; i < view.page.nodes.Count; i++)
        {
            var to = view.page.nodes[i];
            if (to == null || to.hidden) continue;
            var prereqs = treeRef.GetPrerequisites(to);
            for (int p = 0; p < prereqs.Count; p++)
            {
                var from = prereqs[p];
                if (from == null || from.hidden) continue;
                if (!view.page.nodes.Contains(from))
                {
                    // 跨页前置：子节点画一段向上的续接线，提示「上面还有一层」
                    Vector2 cb = NodePositionInPage(view, to);
                    float childTop = cb.y + nodeSize.y * 0.5f;
                    AddConnectorSegment(view, to, new Vector2(cb.x, childTop + 70f), new Vector2(cb.x, childTop));
                    continue;
                }
                BuildElbowConnector(view, from, to);
            }
        }

        // 选中高亮圈（配了美术角标就用角标，否则用程序化圆环）
        var ring = CreateImage("SelectionRing", view.nodeRoot,
            selectionCorners != null ? Color.white : new Color(1f, 0.86f, 0.4f, 0.55f),
            selectionCorners != null ? selectionCorners : ProceduralUiSprites.Ring(128, 12, 1f));
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
            // 用美术节点框时：节点上只留图标，名字与价格进详情卡（参考图就是这样 ✅）
            label.gameObject.SetActive(preferProceduralNodes);
            view.nodeLabels.Add(label);

            // 价格：框内上沿
            var cost = CreateText("Cost", rect, node.coinCost.ToString(), 18f, TextAlignmentOptions.Center);
            var costRect = cost.rectTransform;
            costRect.anchorMin = new Vector2(0f, 1f);
            costRect.anchorMax = new Vector2(1f, 1f);
            costRect.pivot = new Vector2(0.5f, 1f);
            costRect.sizeDelta = new Vector2(-10f, 22f);
            costRect.anchoredPosition = new Vector2(0f, -5f);
            cost.gameObject.SetActive(preferProceduralNodes);
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
        _tabRow.anchoredPosition = new Vector2(0f, -230f);

        float gap = 16f;
        float width = (1000f - 160f - gap * (TalentCatalog.CategoryCount - 1)) / TalentCatalog.CategoryCount;
        for (int i = 0; i < TalentCatalog.CategoryCount; i++)
        {
            if (catalog.Get(i) == null) continue;
            string label = (categoryLabels != null && i < categoryLabels.Length && !string.IsNullOrEmpty(categoryLabels[i]))
                ? categoryLabels[i]
                : $"分类 {i}";
            // 页签 = 纯文字 + 选中下划线（刀锋就是这么做的，不放牌子）
            // 配了美术页签图（A03/A04）时，底图改用美术图。
            bool tabArt = tabNormal != null;
            var hit = CreateImage($"Tab_{i}", _tabRow, tabArt ? Color.white : new Color(1f, 1f, 1f, 0f), tabArt ? tabNormal : null);
            hit.raycastTarget = true;
            if (tabArt) hit.type = Image.Type.Sliced;
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
        var card = CreateImage("DetailCard", parent,
            detailPlate != null ? Color.white : new Color(0.12f, 0.10f, 0.08f, 0.97f),
            detailPlate != null ? detailPlate : ProceduralUiSprites.RoundedPlate(22, 4, 0.95f, 1f));
        if (detailPlate != null) card.type = Image.Type.Sliced;
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

        var buy = CreateImage("BuyButton", _detailCard,
            buttonNormal != null ? Color.white : new Color(0.46f, 0.35f, 0.18f, 1f),
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
        UpdateTeaching();      // 教学的手要跟着节点/按钮走（重建后也要跟上）

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
                    // 锁定节点**也允许点开看详情**（能否购买由详情卡的 canBuy 决定）
                    view.nodeButtons[i].interactable = true;

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
                    && view.pageIndex == _selectedPage
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
            if (img != null)
            {
                if (tabNormal != null)
                {
                    // 美术页签图：常态/选中态换图；颜色保持原样（不乘暗色，否则美术色会偏）
                    img.sprite = active ? (tabActive != null ? tabActive : tabNormal) : tabNormal;
                    img.type = Image.Type.Sliced;
                    img.color = Color.white;
                }
                else img.color = new Color(1f, 1f, 1f, 0f);   // 只做点击区，不画底
            }
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
            // 必须按**页**定位：只看「分类 + 页内序号」会在多页时取错节点
            if (_selectedPage >= 0 && v.pageIndex != _selectedPage) continue;
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
            var artDot = (on ? dotOn : dotOff) != null;
            var dot = CreateImage("Dot_" + i, _dotsRoot,
                artDot ? Color.white : (on ? new Color(0.95f, 0.80f, 0.45f, 1f) : new Color(0.55f, 0.50f, 0.44f, 1f)), sprite);
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
            if (img != null)
                img.color = buttonNormal != null
                    // 美术按钮：可买 = 原色；不可买 = 压暗（美术包里没有单独的禁用图，禁用反馈由程序处理）
                    ? (canBuy ? Color.white : new Color(0.35f, 0.32f, 0.30f, 1f))
                    : (canBuy ? new Color(0.46f, 0.35f, 0.18f, 1f) : new Color(0.20f, 0.18f, 0.16f, 1f));
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
        _selectedPage = view.pageIndex;
        _feedback = null;
        Refresh();

        // 天赋页教学：玩家点了一个节点 → 下一步指向【解锁】按钮
        if (_teachActive && _teachStep == 0)
        {
            _teachStep = 1;
            UpdateTeaching();
        }
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
                // 解锁成功 → **教学进入第 3 步**：教玩家点左上角「返回」回主菜单
                // （不直接结束教学 —— 否则玩家解锁完就不知道该怎么回主菜单，2026-11-04 用户反馈）
                if (_teachActive)
                {
                    _teachStep = 2;
                    UpdateTeaching();
                }
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
        _selectedPage = -1;
        _feedback = null;
        Refresh();
        // 切分类：回到**该分类自己**记住的页（不能沿用源分类的页号）
        int remembered = Mathf.Clamp(_pageIndexByCategory[index], 0, Mathf.Max(0, PageCountOf(index) - 1));
        StartSnap(index, remembered);
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
            // 竖向：手指**上滑** delta.y 为正 → 内容跟着手指往上走（露出更深的一层）✅
            _pagesRoot.anchoredPosition = new Vector2(_pagesRoot.anchoredPosition.x, CurrentPageIndex * _pageHeight + DampY(_dragTotal.y));
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
            // 竖向：手指上滑（delta.y > 0）→ 去**更深**的一页（树往下长）✅
            if (_dragTotal.y >= threshold) targetPage = Mathf.Min(targetPage + 1, Mathf.Max(0, PageCountOf(categoryIndex) - 1));
            else if (_dragTotal.y <= -threshold) targetPage = Mathf.Max(targetPage - 1, 0);
        }
        _dragTotal = Vector2.zero;

        if (targetCategory != categoryIndex)
        {
            categoryIndex = targetCategory;
            _selectedCategory = -1;
            _selectedIndex = -1;
            _selectedPage = -1;
            _feedback = null;
            _pageIndexByCategory[Mathf.Clamp(categoryIndex, 0, TalentCatalog.CategoryCount - 1)] = targetPage;
            Refresh();
        }
        else if (targetPage != CurrentPageIndex)
        {
            // 上下翻页：清空选中（详情随之隐藏）并刷新页码高亮
            _selectedCategory = -1;
            _selectedIndex = -1;
            _selectedPage = -1;
            _feedback = null;
            _pageIndexByCategory[Mathf.Clamp(categoryIndex, 0, TalentCatalog.CategoryCount - 1)] = targetPage;
            Refresh();
        }
        else
        {
            _pageIndexByCategory[Mathf.Clamp(categoryIndex, 0, TalentCatalog.CategoryCount - 1)] = targetPage;
        }
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
        // 阻尼：第一页时「往回滑」（手指下滑，dy<0）没内容可去 → 橡皮筋；最后一页时「往下滑」（手指上滑，dy>0）同理
        bool atFirst = page <= 0 && dy < 0f;
        bool atLast = page >= count - 1 && dy > 0f;
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

    /// <summary>
    /// 泳道：每条分支一列，底板 + 顶部分支牌（参考图的三列结构）。
    /// 泳道中心取**整棵树**里该列节点的 x 平均值 → 跨页时列位置不飘 ✅。
    /// </summary>
    private void BuildLanes(PageView view)
    {
        var treeRef = view.tree;
        if (treeRef == null || treeRef.nodes == null) return;

        var xs = new Dictionary<int, List<float>>();
        var topY = new Dictionary<int, float>();
        var bottomY = new Dictionary<int, float>();
        float centerY = view.page != null ? view.page.CenterY : 0f;

        // 泳道中心 x 取**整棵树**（跨页时列不飘 ✅）；
        // 但上下范围只取**本页**的节点 → 分支牌与底板不会跑到别的页上去 ✅
        foreach (var n in treeRef.nodes)
        {
            if (n == null || n.hidden) continue;
            int lane = n.col;
            if (!xs.ContainsKey(lane)) { xs[lane] = new List<float>(); }
            xs[lane].Add(n.treePosition.x);
            if (view.page == null || !view.page.nodes.Contains(n)) continue;
            float y = n.treePosition.y - centerY;
            if (!topY.ContainsKey(lane) || y > topY[lane]) topY[lane] = y;
            if (!bottomY.ContainsKey(lane) || y < bottomY[lane]) bottomY[lane] = y;
        }

        foreach (var kv in xs)
        {
            int lane = kv.Key;
            float sum = 0f;
            foreach (float x in kv.Value) sum += x;
            float centerX = sum / kv.Value.Count;

            // 泳道底板（半透明暗块）
            if (laneBackgroundColor.a > 0.01f && topY.ContainsKey(lane))
            {
                var bg = CreateImage($"Lane_{lane}", view.nodeRoot, laneBackgroundColor, null);
                bg.raycastTarget = false;
                var br = bg.rectTransform;
                br.anchorMin = new Vector2(0.5f, 0.5f);
                br.anchorMax = new Vector2(0.5f, 0.5f);
                br.pivot = new Vector2(0.5f, 1f);
                float top = topY[lane] + nodeSize.y * 0.5f + (lanePlaqueHeight + 40f);
                float height = top - (bottomY[lane] - nodeSize.y * 0.5f - 40f);
                br.sizeDelta = new Vector2(laneWidth, height);
                br.anchoredPosition = new Vector2(centerX, top);
                bg.transform.SetAsFirstSibling();
            }

            // 分支牌（美术空底板；图标后续由美术补）
            if (branchPlaques != null && lane >= 0 && lane < branchPlaques.Length && branchPlaques[lane] != null && topY.ContainsKey(lane))
            {
                var plaque = CreateImage($"LanePlaque_{lane}", view.nodeRoot, Color.white, branchPlaques[lane]);
                plaque.raycastTarget = false;
                plaque.preserveAspect = true;
                var pr = plaque.rectTransform;
                pr.anchorMin = new Vector2(0.5f, 0.5f);
                pr.anchorMax = new Vector2(0.5f, 0.5f);
                pr.pivot = new Vector2(0.5f, 1f);
                pr.sizeDelta = new Vector2(lanePlaqueWidth, lanePlaqueHeight);
                // 牌在**根节点上方**：pivot 在上边 → 锚点要再往上抬一个牌高
                pr.anchoredPosition = new Vector2(centerX, topY[lane] + nodeSize.y * 0.5f + 30f + lanePlaqueHeight);
                plaque.transform.SetAsFirstSibling();
            }
        }
    }

    /// <summary>
    /// 肘形连线：同一条竖线上就画一段直线；不同列则「竖段 + 斜段」（参考图的分叉连线）。
    /// </summary>
    private void BuildElbowConnector(PageView view, TalentNodeConfig from, TalentNodeConfig to)
    {
        Vector2 a = NodePositionInPage(view, from);
        Vector2 b = NodePositionInPage(view, to);
        float parentBottom = a.y - nodeSize.y * 0.5f;
        float childTop = b.y + nodeSize.y * 0.5f;

        if (Mathf.Abs(b.x - a.x) < 1f)
        {
            AddConnectorSegment(view, to, new Vector2(a.x, parentBottom), new Vector2(b.x, childTop));
            return;
        }
        // 肘点：取父子之间偏上的位置，斜段不会贴到子节点框上
        float busY = Mathf.Lerp(parentBottom, childTop, 0.55f);
        AddConnectorSegment(view, to, new Vector2(a.x, parentBottom), new Vector2(a.x, busY));
        AddConnectorSegment(view, to, new Vector2(a.x, busY), new Vector2(b.x, childTop));
    }

    /// <summary>加一段连线（登记到页视图，刷新时会跟着节点状态染色）。</summary>
    private void AddConnectorSegment(PageView view, TalentNodeConfig target, Vector2 from, Vector2 to)
    {
        var line = CreateImage($"Link_{target.nodeId}", view.nodeRoot, connectorLockedColor, connector);
        line.raycastTarget = false;
        view.connectorImages.Add(line);
        view.connectorTargets.Add(target);
        PlaceConnector(line.rectTransform, from, to, connectorThickness);
    }

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

    #region 天赋页硬性教学（2026-11-04）

    /// <summary>
    /// **天赋页硬性教学**：第一次打开天赋页 → 教「怎么解锁天赋」。
    ///   步骤 0：指向一个**现在就能解锁**的节点 → 玩家点它（详情卡展开）
    ///   步骤 1：指向详情卡里的【解锁】按钮 → 玩家点它 → **真的解锁了**才算教完（写存档标记）
    ///
    /// 关闭面板 = 放弃（不写标记 → 下次打开还教 ✅）。
    /// </summary>
    private void TryStartTeaching()
    {
        if (_teachActive) return;
        var data = SaveManager.Load();
        if (TalentSaveSection.IsTalentPanelTaught(data)) return;          // 教过了 → 不再教
        if (!FindTeachingTarget(out _, out _))
        {
            // ★ 教学目标是**指向突刺（C2）**（需求 R8）。C2 已经解锁 → 这段教学的目标已经达成
            //   → **直接记为已教**（不能拿别的节点凑数教一遍没用的东西）。
            if (IsTeachingTargetAlreadyUnlocked())
            {
                TalentSaveSection.MarkTalentPanelTaught(data);
                SaveManager.Save(data);
                Debug.Log("[TalentPanel] 指向突刺（C2）已解锁 → 天赋页教学目标已达成，不再教");
            }
            return;
        }
        BuildTeachingUI();
        _teachActive = true;
        _teachStep = 0;
        UpdateTeaching();
        Debug.Log("[TalentPanel] 天赋页教学开始（第一次打开）");
    }

    /// <summary>教学目标（指向突刺 / C2）是否**已经解锁**了。</summary>
    private bool IsTeachingTargetAlreadyUnlocked()
    {
        for (int i = 0; i < _pages.Count; i++)
        {
            var pv = _pages[i];
            if (pv == null || pv.page == null || pv.page.nodes == null) continue;
            for (int n = 0; n < pv.page.nodes.Count; n++)
            {
                var node = pv.page.nodes[n];
                if (node == null || node.hidden) continue;
                if (node.moveGuideGesture != MoveGuideGesture.ComboCharge) continue;
                return TalentTreeState.IsUnlocked(node);
            }
        }
        return false;
    }

    /// <summary>放弃这次教学（关闭面板时）：不写标记，下次还教。</summary>
    private void StopTeaching()
    {
        _teachActive = false;
        _teachStep = 0;
        UnlockAllNodes();
        if (_teachMask != null) _teachMask.SetBlocking(false);
        HideTeachingVisuals();
    }

    /// <summary>
    /// 教学期间锁住**除目标以外**的节点（玩家不许点别的天赋）。
    /// 与「三选一只让选推荐卡」同一套做法。
    /// </summary>
    private void LockOtherNodes(PageView targetView, int targetIndex)
    {
        for (int i = 0; i < _pages.Count; i++)
        {
            var pv = _pages[i];
            if (pv == null) continue;
            for (int b = 0; b < pv.nodeButtons.Count; b++)
            {
                var btn = pv.nodeButtons[b];
                if (btn == null) continue;
                bool isTarget = (pv == targetView && b == targetIndex);
                btn.interactable = isTarget;
            }
        }
    }

    /// <summary>教学结束 / 放弃 → 恢复所有节点可点。</summary>
    private void UnlockAllNodes()
    {
        for (int i = 0; i < _pages.Count; i++)
        {
            var pv = _pages[i];
            if (pv == null) continue;
            for (int b = 0; b < pv.nodeButtons.Count; b++)
            {
                var btn = pv.nodeButtons[b];
                if (btn != null) btn.interactable = true;
            }
        }
    }

    /// <summary>教学完成（真的解锁了一个节点）→ 写存档标记 → 以后不再教。</summary>
    private void CompleteTeaching()
    {
        _teachActive = false;
        _teachStep = 0;
        UnlockAllNodes();          // 教学结束 → 节点恢复可点
        if (_teachMask != null) _teachMask.SetBlocking(false);
        HideTeachingVisuals();
        var data = SaveManager.Load();
        TalentSaveSection.MarkTalentPanelTaught(data);
        SaveManager.Save(data);
        Debug.Log("[TalentPanel] 天赋页教学完成（已解锁一个节点）→ 以后不再教");
    }

    private void BuildTeachingUI()
    {
        if (_teachLayer != null) return;
        var self = (RectTransform)transform;

        // 教学层：**不遮屏**（面板自己已经有 Dim，再叠一层黑会把整个面板压死 —— 2026-11-04 截图确认）。
        // 这里只作为「手 / 光圈 / 文案」的容器，不填色。
        var layer = CreateImage("TeachLayer", self, new Color(0f, 0f, 0f, 0f), null);
        layer.raycastTarget = false;
        Stretch(layer.rectTransform);
        _teachLayer = layer.rectTransform;

        // ★ **同一套聚光遮罩**（2026-11-06 用户要求：「这种教学引导是通用准则，怎么到别的界面就不遵守了？」）：
        //   天赋页的教学同样只允许点**被高亮指着的那个**（C2 节点 / 解锁按钮 / 返回按钮），
        //   其余（页签、其它节点、关闭…）一律点不到 → 遮罩在目标处挖洞。
        _teachMask = TutorialClickMask.EnsureExists(_teachLayer);

        _teachRing = CreateImage("TeachRing", layer.rectTransform, new Color(1f, 0.86f, 0.4f, 0.9f),
            teachingRingSprite != null ? teachingRingSprite : ProceduralUiSprites.Ring(128, 12, 1f));
        _teachRing.raycastTarget = false;

        _teachHand = CreateImage("TeachHand", layer.rectTransform, Color.white, teachingHandSprite);
        _teachHand.raycastTarget = false;

        // ★ **不再用“高亮材质”**（2026-11-06 用户：「本质上来说确实不需要高亮材质，
        //   而是让对应按钮显示在遮罩上层即可」）→ 手与光圈**不画**了：
        //   高亮效果 = **遮罩在目标按钮处开洞** → 按钮原样亮着，其余压暗且点不到。
        if (_teachHand != null) _teachHand.gameObject.SetActive(false);
        if (_teachRing != null) _teachRing.gameObject.SetActive(false);

        // 文案：**屏幕下方**（教学关的台词也在下方；放上方会压住标题和页签 —— 2026-11-04 截图确认）。
        // 位置放在详情卡（高 300）**之上**，避免选中节点后互相压。
        var labelBg = CreateImage("TeachLabelBg", layer.rectTransform, new Color(0.06f, 0.05f, 0.04f, 0.9f),
            ProceduralUiSprites.RoundedPlate(18, 3, 1f, 1f));
        labelBg.type = Image.Type.Sliced;
        labelBg.raycastTarget = false;
        var lbr = labelBg.rectTransform;
        lbr.anchorMin = lbr.anchorMax = new Vector2(0.5f, 0.5f);
        lbr.pivot = new Vector2(0.5f, 0.5f);
        lbr.sizeDelta = new Vector2(820f, 96f);
        lbr.anchoredPosition = Vector2.zero;

        _teachLabel = CreateText("TeachLabel", labelBg.rectTransform, "", 34f, TextAlignmentOptions.Center);
        _teachLabelBg = labelBg.rectTransform;
        var lr = _teachLabel.rectTransform;
        lr.anchorMin = lr.anchorMax = new Vector2(0.5f, 0.5f);
        lr.pivot = new Vector2(0.5f, 0.5f);
        lr.sizeDelta = new Vector2(940f, 104f);
        lr.anchoredPosition = Vector2.zero;
    }

    /// <summary>教学手势的动画（按压 + 呼吸）——和战斗里的手势提示同一套观感。</summary>
    private void UpdateTeachingAnimation()
    {
        if (!_teachActive) return;
        _teachAnimTime += Time.unscaledDeltaTime;
        float cycle = 1.1f;
        float phase = (_teachAnimTime % cycle) / cycle;
        float press = phase < 0.35f
            ? Mathf.SmoothStep(0f, 1f, phase / 0.35f)
            : 1f - Mathf.SmoothStep(0f, 1f, (phase - 0.35f) / 0.65f);

        if (_teachHand != null)
        {
            var hr = _teachHand.rectTransform;
            hr.localRotation = Quaternion.Euler(0f, 0f, 8f * press);   // 绕掌心压下去
            hr.localScale = Vector3.one * Mathf.Lerp(1f, 0.94f, press);
            // 帧序列（教学同款）：有帧就按帧切图
            if (teachingHandFrames != null && teachingHandFrames.Length > 0 && _teachHand.sprite != null)
            {
                int fi = Mathf.Min(teachingHandFrames.Length - 1, (int)(phase * teachingHandFrames.Length));
                var f = teachingHandFrames[fi];
                if (f != null) _teachHand.sprite = f;
            }
        }
        if (_teachRing != null)
        {
            float breathe = 1f + Mathf.Sin(_teachAnimTime * 3.2f) * 0.08f;
            _teachRing.rectTransform.localScale = Vector3.one * breathe;
            _teachRing.rectTransform.localRotation = Quaternion.Euler(0f, 0f, _teachAnimTime * 30f);
        }
        PlaceTeachHandOnTarget(TutorialGestureStyle.BaseHandSize * 0.55f);
    }

    /// <summary>把教学手势的**指尖**钉在目标上（按压动画的旋转不会把指尖甩离目标）。</summary>
    private void PlaceTeachHandOnTarget(float handSize)
    {
        if (_teachHand == null || _teachHand.sprite == null || _teachLayer == null) return;
        var hr = _teachHand.rectTransform;
        Vector3 tipOffset = new Vector3(
            (TutorialGestureStyle.TipUV.x - hr.pivot.x) * handSize,
            (TutorialGestureStyle.TipUV.y - hr.pivot.y) * handSize, 0f);
        tipOffset = hr.localRotation * tipOffset;
        tipOffset = new Vector3(tipOffset.x * hr.localScale.x, tipOffset.y * hr.localScale.y, 0f);
        hr.anchoredPosition = _teachTargetLocal - tipOffset;
    }

    /// <summary>把手 / 光圈指到当前该指的东西（步骤 0 = 节点；步骤 1 = 确认按钮）。</summary>
    private void UpdateTeaching()
    {
        if (!_teachActive || _teachLayer == null) return;
        if (_teachLabel != null)
            _teachLabel.text = _teachStep == 0 ? teachingTextPickNode : teachingTextConfirm;

        RectTransform target = null;
        RectTransform maskTarget = null;      // 遮罩开洞的目标 = 可点的那个（按钮）
        if (_teachStep == 0)
        {
            if (FindTeachingTarget(out var view, out int index) && index >= 0 && index < view.nodeImages.Count)
            {
                // ★ 教学期间：除了**目标节点**，其它节点**不可点**
                //   （同项目里「三选一只让选推荐卡」的做法 → 玩家不许点别的天赋）。
                LockOtherNodes(view, index);
                // 遮罩的洞开在**节点按钮**上（不是图标）→ 整张节点卡亮着 = 高亮
                if (index < view.nodeButtons.Count && view.nodeButtons[index] != null)
                    maskTarget = view.nodeButtons[index].transform as RectTransform;
                // 目标不在当前视图 → **切过去**（教学得让玩家看得见指的东西）
                if (view.category != _selectedCategory || view.pageIndex != _selectedPage)
                {
                    _selectedCategory = view.category;
                    _selectedPage = view.pageIndex;
                    if (view.category >= 0 && view.category < 3) categoryIndex = view.category;
                    Refresh();
                }
                var img = view.nodeImages[index];
                if (img != null)
                {
                    // 指向节点**图标**（那个「枪」字）而不是外框 —— 指外框的话玩家不知道指的是哪个（截图确认）
                    var icon = img.transform.Find("Icon");
                    target = (icon != null ? icon : img.transform) as RectTransform;
                }
            }
        }
        else if (_teachStep == 1)
        {
            if (_buyButton != null) { target = _buyButton.GetComponent<RectTransform>(); maskTarget = target; }
        }
        else
        {
            // 第 3 步：教玩家点「返回」回主菜单
            target = _closeButtonRect;
            maskTarget = target;
        }

        if (target == null) { HideTeachingVisuals(); if (_teachMask != null) _teachMask.SetBlocking(false); return; }

        // 遮罩的洞 = 当前该点的那个按钮 → 它不被遮罩盖住（原样亮着 = 高亮），其余点不到
        if (_teachMask != null)
        {
            _teachMask.SetTarget(maskTarget != null ? maskTarget : target);
            _teachMask.SetBlocking(true);
        }

        float handSize = TutorialGestureStyle.BaseHandSize * 0.55f;
        float ringSize = TutorialGestureStyle.BaseRingSize * 0.55f;
        // ⚠️ 锚点用**视觉中心**（`rect.center`），不是 `rectTransform.position`（那是 pivot，很多 UI 的 pivot 在角上）
        Vector3 world = target != null ? target.TransformPoint(target.rect.center) : Vector3.zero;

        if (_teachHand != null && _teachHand.sprite != null)
        {
            var hr = _teachHand.rectTransform;
            hr.sizeDelta = new Vector2(handSize, handSize);
            hr.pivot = TutorialGestureStyle.HandPivot;             // 支点放在掌心
            hr.anchorMin = hr.anchorMax = new Vector2(0.5f, 0.5f);
            // 指尖落在目标上 → 用 TipOffset 反推手的位置（**每帧重算** → 按压旋转不会把指尖甩离目标）
            Vector3 local = _teachLayer.InverseTransformPoint(world);
            _teachTargetLocal = local;
            PlaceTeachHandOnTarget(handSize);

            // 文案：**靠近手势，但既不遮手势、也不遮它指的节点**
            if (_teachLabelBg != null)
            {
                Vector2 lblPos = TutorialGestureStyle.PlaceLabelNear(_teachLayer, world, new Vector2(620f, 96f), handSize);
                _teachLabelBg.anchoredPosition = lblPos;
            }
        }
        else if (_teachHand != null) _teachHand.gameObject.SetActive(false);

        if (_teachRing != null)
        {
            var rr = _teachRing.rectTransform;
            rr.sizeDelta = new Vector2(ringSize, ringSize);
            rr.pivot = Vector2.one * 0.5f;                          // 光圈是居中的
            rr.anchorMin = rr.anchorMax = new Vector2(0.5f, 0.5f);
            rr.anchoredPosition = _teachLayer.InverseTransformPoint(world);        }
    }

    private void HideTeachingVisuals()
    {
        if (_teachLayer != null) _teachLayer.gameObject.SetActive(false);
    }

    /// <summary>
    /// 找一个「现在就能解锁」的节点当教学目标：**当前分类优先，其次「招式解锁」分类**，
    /// 其余分类最后；节点必须**已解锁前置 + 玩家现在的铜钱够**（否则玩家点了也买不了）。
    /// </summary>
    private bool FindTeachingTarget(out PageView view, out int index)
    {
        view = null;
        index = -1;
        int coins = SaveManager.GetCoins();

        // ★ ① **教学目标是「指向突刺（C2）」**（需求 R8：「主菜单教玩家进入天赋界面点出**蓄力 C2 stab**」；
        //   解锁它之后，正式关里才有用「连招蓄力」引导拍教这个招（需求 R10 / D4））。
        //   判据**不写死节点 id**：**教学手势 = 连招蓄力（ComboCharge）** 的那个节点 ——
        //   只有指向突刺是这个手势（横划那个是 Slash）→ 与 `MoveGuideService` 同一套“数据决定”的口径 ✅。
        bool hasComboChargeNode = false;
        for (int i = 0; i < _pages.Count && view == null; i++)
        {
            var pv = _pages[i];
            if (pv == null || pv.page == null || pv.page.nodes == null) continue;
            var tree = pv.tree != null ? pv.tree : (catalog != null ? catalog.Get(pv.category) : null);
            for (int n = 0; n < pv.page.nodes.Count; n++)
            {
                var node = pv.page.nodes[n];
                if (node == null || node.hidden) continue;
                if (node.moveGuideGesture != MoveGuideGesture.ComboCharge) continue;
                hasComboChargeNode = true;
                if (TalentTreeState.IsUnlocked(node)) continue;
                if (!TalentTreeState.IsAvailable(tree, node)) continue;
                if (node.coinCost > coins) continue;
                view = pv;
                index = n;
                return true;
            }
        }

        // 树里有「连招蓄力」型节点（= 指向突刺）但现在教不了（已解锁 / 前置没满足 / 铜钱不够）
        //   → **不拿别的节点凑数**（那教的是没用的一招）→ 返回 false。
        if (hasComboChargeNode) return false;

        // ② 树里根本没有「连招蓄力」型节点（不是张飞这套）→ 退回旧行为（招式解锁 → 当前分类 → 其余）
        for (int rank = 0; rank < 3 && view == null; rank++)
        {
            for (int i = 0; i < _pages.Count; i++)
            {
                var pv = _pages[i];
                if (pv == null || pv.page == null || pv.page.nodes == null) continue;
                bool isMoves = pv.category == 1;        // 招式解锁
                bool prefer = pv.category == _selectedCategory;
                if (rank == 0 && !isMoves) continue;
                if (rank == 1 && (isMoves || prefer)) continue;
                if (rank == 2 && (isMoves || prefer)) continue;
                var tree = pv.tree != null ? pv.tree : (catalog != null ? catalog.Get(pv.category) : null);
                for (int n = 0; n < pv.page.nodes.Count; n++)
                {
                    var node = pv.page.nodes[n];
                    if (node == null || node.hidden) continue;
                    if (TalentTreeState.IsUnlocked(node)) continue;             // 已解锁的当目标没意义
                    if (!TalentTreeState.IsAvailable(tree, node)) continue;      // 前置没满足 → 点不了
                    if (node.coinCost > coins) continue;                        // 铜钱不够 → 买了也白点
                    view = pv;
                    index = n;
                    return true;
                }
            }
        }
        return false;
    }

    #endregion
}
