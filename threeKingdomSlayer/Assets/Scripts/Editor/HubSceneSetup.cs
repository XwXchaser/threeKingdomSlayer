using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// **落地页 UI 搭建工具**（Tools/落地页/搭建落地页 UI）
///
/// 在 `Assets/Scenes/Battle.scene` 的 `BattleHUD(Canvas)` 下重建落地页（就绪态）的新 UI：
///   • 右侧中部：关卡入口（景色不规则形 + TMP 关卡名）→ 点开选关界面
///   • 右下：天赋树入口大卡 → 点开天赋界面
///   • 左下：任务条（笔刷底 + 标题 + 进度 + 奖励）
///   • 左下角：四宫格菜单键（置灰占位，功能待定）
///   • 金币挪到右上（按参考图排布）
///   • 头像从 `Health(Slider)` 移到 `HeroHUDParent`（**绝对位置不变**）—— 之前它挂在血条下面，
///     血条在就绪态被隐藏，落地页根本看不到头像
///   • 底部 tab 栏下线（主界面即战斗界面）
///
/// 位置与尺寸**不写死在这里**：建好后用「Tools/落地页布局台」拖拽调整。
/// </summary>
public static class HubSceneSetup
{
    public const string SceneName = "Assets/Scenes/Battle.scene";
    public const string CanvasName = "BattleHUD(Canvas)";
    /// <summary>落地页专用画布（与战斗 HUD 分开，方便单独开关调试）。</summary>
    public const string HubCanvasName = "HubCanvas(Canvas)";

    [MenuItem("Tools/落地页/搭建落地页 UI")]
    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene(SceneName, OpenSceneMode.Single);
        var canvasGO = GameObject.Find(CanvasName);
        if (canvasGO == null)
        {
            Debug.LogError($"[HubSetup] 场景里找不到 {CanvasName}");
            return;
        }

        // 落地页 UI 与战斗 HUD 分到两个根画布：单独关掉 HubCanvas 就能只看战斗 UI（反之亦然）。
        var hubCanvas = EnsureHubCanvas(canvasGO.transform);

        // 幂等：先删掉同名的旧节点（**两个画布下都要删** —— 重构后它们在 HubCanvas 下，
        // 只删 BattleHUD 下的会留下重复节点，散落在画布根上、关卡态也显示）
        string[] generated = { "HubStageEntry", "HubTalentEntry", "HubTaskBar", "HubMenuPlaceholder" };
        for (int i = 0; i < generated.Length; i++)
        {
            // 注意要用**递归**查找：重构后节点在 Hub/Bottom/ 这类分组文件夹里，
            // Transform.Find 只找直接子节点 → 删不掉 → 每次重建都多一份
            // 要删**全部**同名节点（历史上可能已积压多份），删完再建
            var oldBattle = FindDeep(canvasGO.transform, generated[i]);
            while (oldBattle != null)
            {
                Object.DestroyImmediate(oldBattle.gameObject);
                oldBattle = FindDeep(canvasGO.transform, generated[i]);
            }
            var oldHub = FindDeep(hubCanvas, generated[i]);
            while (oldHub != null)
            {
                Object.DestroyImmediate(oldHub.gameObject);
                oldHub = FindDeep(hubCanvas, generated[i]);
            }
        }

        var font = LoadTmpFont();
        Sprite S(string n) => AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/UI/Hub/{n}.png");

        // ── 关卡入口（右侧中部，按参考图「火山卡」位置）──
        //    常规做法：锚到屏幕右边缘（anchor 1,0.5 + pivot 1,0.5）→ 屏幕宽度变化时自动贴边。
        // 参考图（露米娅）里关卡卡在**右侧 24%~41% 高度**、宽约 28%：
        // 1080 宽 → 卡片 300 宽；1920 高 → 顶边 460、底边 842。
        var stage = NewNode(hubCanvas, "HubStageEntry", new Vector2(300, 340), new Vector2(1f, 1f), new Vector2(-20f, -460f), new Vector2(1f, 1f));
        var stageImg = NewImage(stage, "Base", SpriteFill.TopStretch, S("hub_stage_entry"), false);
        stageImg.preserveAspect = true;
        var baseRt = (RectTransform)stageImg.transform;
        baseRt.offsetMin = new Vector2(0f, -210f);   // 958x672 → 300 宽 = 210 高
        baseRt.offsetMax = new Vector2(0f, 0f);
        // 名字带：用笔刷条素材（横向拉伸）在景色正下方，TMP 写关卡名
        var bandGo = new GameObject("NameBand", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        bandGo.transform.SetParent(stage, false);
        var bandRt = (RectTransform)bandGo.transform;
        bandRt.anchorMin = new Vector2(0f, 0f);
        bandRt.anchorMax = new Vector2(1f, 0f);
        bandRt.pivot = new Vector2(0.5f, 0f);
        bandRt.offsetMin = new Vector2(0f, 56f);     // 景色 210 高 → 带子在 56..130
        bandRt.offsetMax = new Vector2(0f, 130f);
        var bandImg = bandGo.GetComponent<UnityEngine.UI.Image>();
        bandImg.sprite = S("hub_taskbar_brush");
        bandImg.type = UnityEngine.UI.Image.Type.Sliced;
        bandImg.raycastTarget = false;
        var stageName = NewText(bandGo.transform, "StageName", font, 34f, TextAlignmentOptions.Center,
            new Color(0.93f, 0.90f, 0.82f));
        var stageBtn = stage.gameObject.AddComponent<Button>();
        stageBtn.targetGraphic = stageImg;
        var entry = stage.gameObject.AddComponent<HubStageEntry>();
        entry.baseImage = stageImg;
        entry.stageNameText = stageName;
        entry.button = stageBtn;
        entry.baseSprite = S("hub_stage_entry");
        entry.starEmpty = S("hub_star_empty");
        entry.starHalf = S("hub_star_half");
        entry.starFull = S("hub_star_full");
        entry.Bind();

        // ── 天赋入口（右下，占参考图「家园」卡位）──
        var talent = NewNode(hubCanvas, "HubTalentEntry", new Vector2(300, 220), new Vector2(1f, 0f), new Vector2(-30f, 120f), new Vector2(1f, 0f));
        var treeImg = NewImage(talent, "Icon", SpriteFill.TopStretch, S("hub_talent_tree"), false);
        treeImg.preserveAspect = true;
        var talentLabel = NewText(talent, "Label", font, 34f, TextAlignmentOptions.Center,
            new Color(0.93f, 0.90f, 0.82f));
        var labelRt = (RectTransform)talentLabel.transform;
        labelRt.anchorMin = new Vector2(0f, 0f);
        labelRt.anchorMax = new Vector2(1f, 0f);
        labelRt.pivot = new Vector2(0.5f, 0f);
        labelRt.offsetMin = new Vector2(0f, 4f);
        labelRt.offsetMax = new Vector2(0f, 50f);
        var talentBtn = talent.gameObject.AddComponent<Button>();
        talentBtn.targetGraphic = treeImg;
        var tentry = talent.gameObject.AddComponent<HubTalentEntry>();
        tentry.treeImage = treeImg;
        tentry.label = talentLabel;
        tentry.button = talentBtn;
        tentry.treeSprite = S("hub_talent_tree");
        tentry.Bind();

        // ── 任务条（左下）──
        //    常规做法：底图用 Image(type=Sliced) 横向拉伸，图标与文字交给 LayoutGroup 排。
        // 横向拉伸：左端贴左边缘、右端给右侧栏留 330 → 画布宽度变化时条子跟着变宽，相对关系不乱
        var task = new GameObject("HubTaskBar", typeof(RectTransform));
        task.transform.SetParent(hubCanvas, false);
        var taskRt = (RectTransform)task.transform;
        taskRt.anchorMin = new Vector2(0f, 0f);
        taskRt.anchorMax = new Vector2(1f, 0f);
        taskRt.pivot = new Vector2(0f, 0f);
        // 位置（在「开始关卡」按钮与「四宫格键」之间的一条空带里）：
        //   画布 y 越大越靠下 → 头像（含等级牌/经验条）占 offset 300~590，
        //   四宫格键占 40~136，「开始关卡」按钮占 604~776 →
        //   任务条取 150~263，落在头像下方、四宫格上方，两边都不压。
        // 参考图：任务条 x 2%-50%、y 73%-78% → 画布 offsetMin.y = 563，右端留到画布 480
        taskRt.offsetMin = new Vector2(20f, 563f);
        taskRt.offsetMax = new Vector2(-480f, 676f);   // 高 113 = 素材自然高度（裁掉留白后）
        var barImg = NewImage(task.transform, "Bar", SpriteFill.Whole, S("hub_taskbar_brush"), false);
        barImg.type = UnityEngine.UI.Image.Type.Sliced;   // 笔刷条横向拉伸不变形
        barImg.raycastTarget = false;

        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(task.transform, false);
        var contentRt = (RectTransform)content.transform;
        contentRt.anchorMin = Vector2.zero;
        contentRt.anchorMax = Vector2.one;
        contentRt.offsetMin = new Vector2(16f, 12f);
        contentRt.offsetMax = new Vector2(-16f, -12f);
        var row = content.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 14f;
        row.childAlignment = TextAnchor.MiddleLeft;
        row.childControlWidth = true;      // 用 LayoutElement 的首选尺寸决定大小
        row.childControlHeight = true;
        row.childForceExpandWidth = false;  // 弹性由 LayoutElement.flexibleWidth 决定
        row.childForceExpandHeight = false;

        var taskIcon = new GameObject("Icon", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        taskIcon.transform.SetParent(content.transform, false);
        var iconEl = taskIcon.AddComponent<LayoutElement>();
        iconEl.preferredWidth = 72f; iconEl.preferredHeight = 72f; iconEl.flexibleWidth = 0f;
        var iconImg = taskIcon.GetComponent<UnityEngine.UI.Image>();
        iconImg.preserveAspect = true; iconImg.raycastTarget = false;

        var textBox = new GameObject("TextBox", typeof(RectTransform));
        textBox.transform.SetParent(content.transform, false);
        var textBoxEl = textBox.AddComponent<LayoutElement>();
        textBoxEl.flexibleWidth = 1f; textBoxEl.preferredWidth = 380f; textBoxEl.minWidth = 120f;
        var col = textBox.AddComponent<VerticalLayoutGroup>();
        col.spacing = 0f;
        col.childAlignment = TextAnchor.MiddleLeft;
        col.childControlWidth = true;
        col.childControlHeight = true;
        col.childForceExpandWidth = true;
        col.childForceExpandHeight = false;

        var titleGo = new GameObject("Title", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        titleGo.transform.SetParent(textBox.transform, false);
        var titleEl = titleGo.AddComponent<LayoutElement>();
        titleEl.preferredHeight = 40f;
        var tmpTitle = titleGo.GetComponent<TMPro.TextMeshProUGUI>();
        tmpTitle.font = font; tmpTitle.text = "本关目标"; tmpTitle.fontSize = 30f;
        tmpTitle.alignment = TMPro.TextAlignmentOptions.Left;
        tmpTitle.color = new Color(0.95f, 0.93f, 0.86f);
        tmpTitle.enableWordWrapping = false;
        tmpTitle.raycastTarget = false;

        var progGo = new GameObject("Progress", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        progGo.transform.SetParent(textBox.transform, false);
        var progEl = progGo.AddComponent<LayoutElement>();
        progEl.preferredHeight = 30f;
        var tmpProg = progGo.GetComponent<TMPro.TextMeshProUGUI>();
        tmpProg.font = font; tmpProg.text = "0/0"; tmpProg.fontSize = 24f;
        tmpProg.alignment = TMPro.TextAlignmentOptions.Left;
        tmpProg.color = new Color(0.86f, 0.70f, 0.36f);
        tmpProg.enableWordWrapping = false;
        tmpProg.raycastTarget = false;

        var rewGo = new GameObject("Reward", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        rewGo.transform.SetParent(content.transform, false);
        var rewEl = rewGo.AddComponent<LayoutElement>();
        rewEl.preferredWidth = 56f; rewEl.preferredHeight = 56f; rewEl.flexibleWidth = 0f;
        var rewImg = rewGo.GetComponent<UnityEngine.UI.Image>();
        rewImg.sprite = S("hub_coin"); rewImg.preserveAspect = true; rewImg.raycastTarget = false;

        var tbar = task.AddComponent<HubTaskBar>();
        tbar.barImage = barImg;
        tbar.iconImage = iconImg;
        tbar.rewardImage = rewImg;
        tbar.titleText = tmpTitle;
        tbar.progressText = tmpProg;
        tbar.barSprite = S("hub_taskbar_brush");
        tbar.rewardSprite = S("hub_coin");

        // ── 四宫格菜单键（左下角，功能待定）──
        var menu = NewNode(hubCanvas, "HubMenuPlaceholder", new Vector2(96, 96), new Vector2(0f, 0f), new Vector2(40f, 40f), new Vector2(0f, 0f));
        var keyImg = NewImage(menu, "Key", SpriteFill.Whole, S("hub_fourgrid_key"), false);
        keyImg.preserveAspect = true;
        var menuBtn = menu.gameObject.AddComponent<Button>();
        menuBtn.targetGraphic = keyImg;
        var mh = menu.gameObject.AddComponent<HubMenuPlaceholder>();
        mh.keyImage = keyImg;
        mh.button = menuBtn;
        mh.keySprite = S("hub_fourgrid_key");

        // ── 金币挪到右上（按参考图排布）+ 归到落地页画布 ──
        var coin = canvasGO.transform.Find("CoinCounter");
        if (coin != null)
        {
            coin.SetParent(hubCanvas, false);
            var crt = (RectTransform)coin;
            crt.anchorMin = new Vector2(1f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = new Vector2(-114f, -84f);
        }

        // ── 「开始关卡」按钮归到落地页画布 ──
        var startBtn = canvasGO.transform.Find("StartStageButton");
        if (startBtn != null) startBtn.SetParent(hubCanvas, false);

        // ── 底部 tab 栏（已下线）也归到落地页画布 ──
        var tabBar = canvasGO.transform.Find("HubTabBar");
        if (tabBar != null) tabBar.SetParent(hubCanvas, false);

        // ── 头像：留在原层级与原位置（不动血条/经验条/头像本身）──
        //    头像挂在 Health(Slider) 下，血条在就绪态被隐藏 → 落地页看不到头像。
        //    做法：把 Health 从「局内专用」名单里拿掉（它的 Fill/Area/Frame 本来就在隐藏名单里，
        //    就绪态只剩头像可见），**不重挂父级**（它在 HeroHUD prefab 实例里，父级是祖先，不能重挂）。
        var portrait = FindDeep(canvasGO.transform, "UltPortraitButton");
        if (portrait != null)
        {
            var prt = (RectTransform)portrait;
            var health = portrait.parent as RectTransform;
            if (health != null && health.name.StartsWith("Health"))
            {
                prt.anchorMin = new Vector2(0f, 0f);
                prt.anchorMax = new Vector2(0f, 0f);
                prt.pivot = new Vector2(0.5f, 0.5f);
                prt.sizeDelta = new Vector2(300f, 300f);
                prt.anchoredPosition = new Vector2(121f, 77f);   // 改动前的实测值
            }
        }

        // ── 底部 tab 栏下线 ──
        var tabBar2 = hubCanvas.Find("HubTabBar");
        if (tabBar2 != null) tabBar2.gameObject.SetActive(false);

        // ── 新节点登记为「落地页专属」（overlay 打开时隐藏） ──
        var switcher = Object.FindObjectOfType<HubHudModeSwitch>();
        if (switcher != null)
        {
            // 落地页新元素 → 「就绪态专属」：就绪态显示，关卡态隐藏，overlay 打开时让位
            var readyList = new List<GameObject>();
            if (switcher.readyOnly != null)
            foreach (var g in switcher.readyOnly)
            {
                if (g == null) continue;
                bool stale = false;
                for (int i = 0; i < generated.Length; i++)
                    if (g.name == generated[i]) { stale = true; break; }
                if (stale) continue;
                readyList.Add(g);
            }
            for (int i = 0; i < generated.Length; i++)
            {
                var go = hubCanvas.Find(generated[i]);
                if (go != null && !readyList.Contains(go.gameObject)) readyList.Add(go.gameObject);
            }
            switcher.readyOnly = readyList.ToArray();

            // hubOnly：清掉空引用与同名旧节点（重建会换对象）；新元素归 readyOnly 管，不再进 hubOnly
            var list = new List<GameObject>();
            if (switcher.hubOnly != null)
            foreach (var g in switcher.hubOnly)
            {
                if (g == null) continue;
                bool stale = false;
                for (int i = 0; i < generated.Length; i++)
                    if (g.name == generated[i]) { stale = true; break; }
                if (stale) continue;
                list.Add(g);
            }
            switcher.hubOnly = list.ToArray();

            // Health 从「局内专用」拿掉：就绪态只隐藏血条本体，血条的 Fill/Area/Frame 已在隐藏名单里，
            // 这样头像（UltPortraitButton）在落地页能显示，位置完全不动。
            var inRun = new List<GameObject>();
            if (switcher.inRunOnly != null)
            foreach (var g in switcher.inRunOnly) inRun.Add(g);
            for (int i = inRun.Count - 1; i >= 0; i--)
            {
                if (inRun[i] != null && inRun[i].name.StartsWith("Health")) inRun.RemoveAt(i);
            }
            switcher.inRunOnly = inRun.ToArray();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[HubSetup] 落地页 UI 搭建完成并保存");
    }

    // ─────────────────────────── 工具 ───────────────────────────

    private enum SpriteFill { Whole, TopStretch }

    /// <summary>
    /// 落地页专用画布：与战斗 HUD（BattleHUD(Canvas)）分开，方便在 Hierarchy 里单独开关调试。
    /// 画布参数（CanvasScaler / GraphicRaycaster）与战斗 HUD 保持一致。
    /// </summary>
    public static Transform EnsureHubCanvas(Transform battleCanvas)
    {
        var existing = GameObject.Find(HubCanvasName);
        if (existing != null) return existing.transform;

        var go = new GameObject(HubCanvasName, typeof(RectTransform), typeof(Canvas),
                                typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        var src = battleCanvas.GetComponent<Canvas>();
        var srcScaler = battleCanvas.GetComponent<UnityEngine.UI.CanvasScaler>();
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = src != null ? src.renderMode : UnityEngine.RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 25;                 // 战斗 HUD 是 21 → 落地页画在其上
        canvas.additionalShaderChannels = src != null
            ? src.additionalShaderChannels
            : UnityEngine.AdditionalCanvasShaderChannels.TexCoord1;
        var scaler = go.GetComponent<UnityEngine.UI.CanvasScaler>();
        if (srcScaler != null)
        {
            scaler.uiScaleMode = srcScaler.uiScaleMode;
            scaler.referenceResolution = srcScaler.referenceResolution;
            scaler.screenMatchMode = srcScaler.screenMatchMode;
            scaler.matchWidthOrHeight = srcScaler.matchWidthOrHeight;
            scaler.referencePixelsPerUnit = srcScaler.referencePixelsPerUnit;
        }
        return go.transform;
    }

    [MenuItem("Tools/落地页/切换落地页 UI 显示")]
    public static void ToggleHubCanvas()
    {
        var hub = GameObject.Find(HubCanvasName);
        if (hub == null)
        {
            Debug.LogWarning("[HubSetup] 场景里还没有 " + HubCanvasName + "，先跑一次「搭建落地页 UI」");
            return;
        }
        hub.SetActive(!hub.activeSelf);
        Debug.Log("[HubSetup] 落地页 UI " + (hub.activeSelf ? "已显示" : "已隐藏"));
    }

    [MenuItem("Tools/落地页/拆分落地页 UI 画布")]
    public static void SplitHubCanvas()
    {
        var scene = EditorSceneManager.OpenScene(SceneName, OpenSceneMode.Single);
        var canvasGO = GameObject.Find(CanvasName);
        if (canvasGO == null)
        {
            Debug.LogError($"[HubSetup] 场景里找不到 {CanvasName}");
            return;
        }
        var hub = EnsureHubCanvas(canvasGO.transform);
        string[] move = { "HubStageEntry", "HubTalentEntry", "HubTaskBar", "HubMenuPlaceholder", "CoinCounter", "StartStageButton", "HubTabBar" };
        for (int i = 0; i < move.Length; i++)
        {
            var go = canvasGO.transform.Find(move[i]);
            if (go != null) go.SetParent(hub, false);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[HubSetup] 落地页 UI 已拆到 " + HubCanvasName);
    }

    private static Transform NewNode(Transform parent, string name, Vector2 size, Vector2 anchor, Vector2 pos, Vector2 pivot)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return go.transform;
    }

    /// <summary>建一个 Image 子节点；Whole = 铺满父节点，TopStretch = 横向铺满、顶部对齐。</summary>
    private static Image NewImage(Transform parent, string name, SpriteFill fill, Sprite sprite, bool raycast)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        if (fill == SpriteFill.Whole)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        else
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, -176f);
            rt.offsetMax = new Vector2(0f, 0f);
        }
        var img = go.GetComponent<Image>();
        if (sprite != null) img.sprite = sprite;
        img.raycastTarget = raycast;
        return img;
    }

    /// <summary>建一个 TMP 文本节点（默认铺满父节点，锚点由调用方再调）。</summary>
    private static TMPro.TextMeshProUGUI NewText(Transform parent, string name, TMP_FontAsset font, float size,
        TextAlignmentOptions align, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var tmp = go.GetComponent<TMPro.TextMeshProUGUI>();
        tmp.font = font;
        tmp.text = "—";
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = color;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = false;
        return tmp;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindDeep(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    private static TMP_FontAsset LoadTmpFont()
    {
        // 必须用**含中文字形**的字体：LiberationSans SDF 没有中文字 → 显示为方块/乱码。
        var chinese = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/方正粗黑宋简体 SDF.asset");
        if (chinese != null) return chinese;
        var guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        for (int i = 0; i < guids.Length; i++)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[i]));
            if (font == null) continue;
            // 有中文字形的优先
            if (font.HasCharacters("关卡天赋")) return font;
        }
        return guids.Length > 0
            ? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]))
            : null;
    }
}
