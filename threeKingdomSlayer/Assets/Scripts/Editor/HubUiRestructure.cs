using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// **落地页 + 战斗 HUD 的层级分类重构**（Tools/落地页/重构 UI 分类）
///
/// 用户 2026-10-26 的三条反馈对应的实现：
///   ① 落地页层级杂乱 → 落地页与战斗 HUD 都按「功能」分文件夹，组根统一管显隐；
///   ② 包子与铜钱共用一张底框 → 把合并的底框切成两段，各归各的组（可单独挪移）；
///   ③ 左下四个键没有功能 → 灰化（着色 + 压暗层），见 `HubMenuPlaceholder`。
///
/// 目标结构：
/// <code>
/// HubCanvas(Canvas) order 25
///   └── Hub/
///       ├── Top/    资源条（体力组 / 铜钱组，各带自己的底框）
///       ├── Side/   关卡入口 / 天赋入口
///       ├── Bottom/ 开始关卡 / 任务条 / MenuKeys(四宫格)
///       ├── Nav/    底部 tab 栏（已下线，保留不删）
///       └── Debug/  开发期工具（清除存档）
/// BattleHUD(Canvas) order 21
///   └── Combat/
///       ├── Hero/       战斗主体（HeroHUD prefab 实例，整块不动）
///       ├── Feedback/   Combo / Counters / Status / Buffs / Currency
///       ├── Enemy/      Boss / Wave
///       ├── Input/      Charge
///       └── Overlay/    Pause / Dialogue / Result
/// </code>
///
/// 约定：
///   • **只搬整块**，不拆 prefab 实例内部（HeroHUD_Zhangfei / BuffIcon 的子节点动了就是脏 override）；
///   • 所有分组容器都是「撑满父节点、零偏移」的矩形 → 在组之间搬节点不会改变屏幕位置；
///   • 幂等：可以重复跑。
/// </summary>
public static class HubUiRestructure
{
    public const string SceneName = "Assets/Scenes/Battle.scene";

    // ── 落地页资源条布局（设计空间 960×1920，右上角）──
    //   体力组 560..742，铜钱组 750..937（两段底框之间留 8px 空隙，可各自单独挪移）
    private const float StaminaRight = 218f;
    private const float CoinRight = 23f;
    private const float ResourceTop = -28f;
    private static readonly Vector2 StaminaSize = new Vector2(182f, 67f);
    private static readonly Vector2 CoinSize = new Vector2(187f, 67f);

    [MenuItem("Tools/落地页/重构 UI 分类")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(SceneName, OpenSceneMode.Single);
        var hub = GameObject.Find("HubCanvas(Canvas)");
        var combat = GameObject.Find("BattleHUD(Canvas)");
        if (hub == null || combat == null)
        {
            Debug.LogError("[HubRestructure] 找不到画布，先跑一次「搭建落地页 UI」");
            return;
        }

        AssetDatabase.Refresh();
        FixSpriteImports();

        // ── 落地页 ──
        var hubRoot = EnsureGroup(hub.transform, "Hub");
        var gTop = EnsureGroup(hubRoot, "Top");
        var gSide = EnsureGroup(hubRoot, "Side");
        var gBottom = EnsureGroup(hubRoot, "Bottom");
        var gMenuKeys = EnsureGroup(gBottom, "MenuKeys");
        var gNav = EnsureGroup(hubRoot, "Nav");
        var gDebug = EnsureGroup(hubRoot, "Debug");

        SplitResourceBar(gTop);
        FixTaskBar(gBottom);
        FixStartStageButton(gBottom);
        BuildMenuKeys(gBottom, gMenuKeys);

        MoveTo(gSide, "HubStageEntry");
        MoveTo(gSide, "HubTalentEntry");
        MoveTo(gNav, "HubTabBar");
        MoveTo(gDebug, "HubDebugSaveButton");

        AttachMotion(gBottom, gSide);

        // ── 战斗 HUD ──
        var gCombat = EnsureGroup(combat.transform, "Combat");
        var gHero = EnsureGroup(gCombat, "Hero");
        var gFeedback = EnsureGroup(gCombat, "Feedback");
        var gEnemy = EnsureGroup(gCombat, "Enemy");
        var gInput = EnsureGroup(gCombat, "Input");
        var gOverlay = EnsureGroup(gCombat, "Overlay");

        var fCombo = EnsureGroup(gFeedback, "Combo");
        var fCounters = EnsureGroup(gFeedback, "Counters");
        var fStatus = EnsureGroup(gFeedback, "Status");
        var fBuffs = EnsureGroup(gFeedback, "Buffs");
        var fCurrency = EnsureGroup(gFeedback, "Currency");
        MoveTo(fCombo, "ComboDisplay");
        MoveTo(fCounters, "KillCount");
        MoveTo(fCounters, "KillMilestoneDisplay");
        MoveTo(fStatus, "HittedOverlay");
        MoveTo(fBuffs, "BuffDisplayPanel");
        MoveTo(fCurrency, "CoinCounter");

        var eBoss = EnsureGroup(gEnemy, "Boss");
        var eWave = EnsureGroup(gEnemy, "Wave");
        MoveTo(eBoss, "BossBars");
        MoveTo(eWave, "WaveText");

        var iCharge = EnsureGroup(gInput, "Charge");
        MoveTo(iCharge, "ChargeIndicator");

        var oPause = EnsureGroup(gOverlay, "Pause");
        var oDialogue = EnsureGroup(gOverlay, "Dialogue");
        var oResult = EnsureGroup(gOverlay, "Result");
        MoveTo(oPause, "PauseLayer");
        MoveTo(oDialogue, "DialogueLayer");
        MoveTo(oResult, "Victory(panel)");
        MoveTo(oResult, "Defeat(panel)");

        // ── 显隐名单（按组根）──
        var sw = Object.FindObjectOfType<HubHudModeSwitch>();
        if (sw != null)
        {
            var inRun = new List<GameObject> { gHero.gameObject, gFeedback.gameObject, gEnemy.gameObject, gInput.gameObject };
            var pauseLayer = FindIn(oPause, "PauseLayer");
            if (pauseLayer != null) inRun.Add(pauseLayer.gameObject);
            sw.inRunOnly = inRun.ToArray();

            // readyOnly：落地页专属（关卡态隐藏）。Debug 组也归这里 —— 清除存档按钮不该出现在战斗中。
            sw.readyOnly = new[] { gSide.gameObject, gBottom.gameObject, gDebug.gameObject };

            // hubOnly：二级页面（选关/天赋）打开期间让位。
            var hubOnly = new List<GameObject> { gTop.gameObject };
            var portrait = FindIn(combat.transform, "UltPortraitButton");
            if (portrait != null) hubOnly.Add(portrait.gameObject);
            sw.hubOnly = hubOnly.ToArray();

            // 换了名单必须重新采样，否则显隐下标错位
            sw.Recapture();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[HubRestructure] UI 分类重构完成：落地页 5 组 / 战斗 HUD 5 组，资源条已拆成两段，四键已灰化");
    }

    // ─────────────────────── 资源条拆两段 ───────────────────────

    /// <summary>
    /// 包子与铜钱原来共用一张 369×67 的合并底框（贴图里自带中缝）。
    /// → 切成两段底框，各挂各的组，互不影响、可单独挪移。
    /// </summary>
    private static void SplitResourceBar(Transform gTop)
    {
        var coin = FindIn(gTop, "HubCoin");
        if (coin == null) { Debug.LogWarning("[HubRestructure] 找不到 HubCoin"); return; }

        var stamina = FindIn(gTop, "HubStamina") as RectTransform;
        if (stamina == null)
        {
            var go = new GameObject("HubStamina", typeof(RectTransform));
            go.transform.SetParent(gTop, false);
            stamina = (RectTransform)go.transform;
        }

        // 包子与体力数字搬进体力组
        var bun = coin.Find("BunIcon");
        var staminaText = coin.Find("StaminaText");
        if (bun != null) bun.SetParent(stamina, false);
        if (staminaText != null) staminaText.SetParent(stamina, false);

        var staminaFrame = EnsureFrame(stamina, "Frame", Load("hub_v9_stamina_frame"));
        var coinFrame = EnsureFrame(coin, "Frame", Load("hub_v9_coin_frame"));
        // 老节点里的 "Bar" 与新 "Frame" 重复 → 删掉旧的
        var oldBar = coin.Find("Bar");
        if (oldBar != null) Object.DestroyImmediate(oldBar.gameObject);

        // 两组的矩形：各自独立锚在右上角
        SetRect(stamina, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-StaminaRight, ResourceTop), StaminaSize);
        SetRect((RectTransform)coin, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-CoinRight, ResourceTop), CoinSize);

        // 组内元件位置（相对各自底框左边缘）
        if (bun != null) SetRect((RectTransform)bun, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(45f, 0f), new Vector2(60f, 61f));
        if (staminaText != null) SetRect((RectTransform)staminaText, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(98f, 0f), new Vector2(64f, 44f));

        var coinIcon = coin.Find("CoinIcon");
        var amount = coin.Find("Amount");
        if (coinIcon != null) SetRect((RectTransform)coinIcon, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(56f, 0f), new Vector2(60f, 61f));
        if (amount != null) SetRect((RectTransform)amount, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(142f, 0f), new Vector2(70f, 50f));

        // 体力数字目前没有数据源 → 占位文案 + 跟铜钱同样的字体样式
        var st = staminaText != null ? staminaText.GetComponent<TMP_Text>() : null;
        var amt = amount != null ? amount.GetComponent<TMP_Text>() : null;
        if (st != null)
        {
            if (string.IsNullOrEmpty(st.text)) st.text = "—/—";
            if (amt != null)
            {
                if (st.font == null) st.font = amt.font;
                st.fontSize = amt.fontSize > 0f ? amt.fontSize * 0.85f : 30f;
                st.color = amt.color;
                st.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            }
            st.enableWordWrapping = false;
            st.raycastTarget = false;
        }

        // 底框压在最底层
        staminaFrame.transform.SetAsFirstSibling();
        coinFrame.transform.SetAsFirstSibling();
    }

    // ─────────────────────── 任务条对齐 ───────────────────────

    /// <summary>
    /// 任务条之前是「Bar 与 Content 都被挪了 (-20,-80)」，节点矩形与实际画面差 80px，
    /// 靶标压在笔刷左缘、铜钱悬在右缘外。这里改成：底图铺满节点，内容按内缩排。
    /// </summary>
    private static void FixTaskBar(Transform gBottom)
    {
        var task = FindIn(gBottom, "HubTaskBar");
        if (task == null) return;

        var bar = task.Find("Bar");
        if (bar != null) SetStretch((RectTransform)bar, Vector2.zero, Vector2.zero);

        var content = task.Find("Content");
        if (content != null) SetStretch((RectTransform)content, new Vector2(16f, 8f), new Vector2(-16f, -8f));

        var icon = task.Find("Content/Icon");
        if (icon != null) SetRect((RectTransform)icon, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(44f, 0f), new Vector2(84f, 78f));

        var textBox = task.Find("Content/TextBox");
        if (textBox != null) SetRect((RectTransform)textBox, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(95f, 0f), new Vector2(310f, 40f));

        var title = task.Find("Content/TextBox/Title");
        if (title != null) SetRect((RectTransform)title, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(75f, 0f), new Vector2(150f, 40f));

        var progress = task.Find("Content/TextBox/Progress");
        if (progress != null) SetRect((RectTransform)progress, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(280f, 0f), new Vector2(60f, 36f));

        var fill = task.Find("Content/TextBox/ProgressFill");
        if (fill != null) SetRect((RectTransform)fill, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(75f, -28f), new Vector2(140f, 6f));

        var reward = task.Find("Content/Reward");
        if (reward != null) SetRect((RectTransform)reward, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(480f, 0f), new Vector2(48f, 49f));
    }

    // ─────────────────────── 开始关卡按钮 ───────────────────────

    /// <summary>
    /// 开始关卡按钮两处清理：
    ///   ① 旧的 TMP「开始关卡」文字，和艺术字重复 → 禁用（不删，保留引用）；
    ///   ② Button 原来是 **SpriteSwap**，按下的瞬间会把底图换成旧版的
    ///      `MM_Btn_Primary_Pressed`、不可用态换 `MM_Btn_Disabled`（遗留的 MM_ 系通用按钮图）。
    ///      → 改成 ColorTint + 归零的 spriteState，按下/不可用都不会再冒出旧版按钮。
    /// </summary>
    private static void FixStartStageButton(Transform gBottom)
    {
        var btn = FindIn(gBottom, "StartStageButton");
        if (btn == null) return;

        var label = btn.Find("Text");
        if (label != null && label.gameObject.activeSelf)
        {
            label.gameObject.SetActive(false);
            Debug.Log("[HubRestructure] 已禁用开始关卡按钮上的重复 TMP 文字（艺术字已是唯一文案）");
        }

        var button = btn.GetComponent<UnityEngine.UI.Button>();
        if (button == null) return;

        var state = new UnityEngine.UI.SpriteState
        {
            highlightedSprite = null,
            pressedSprite = null,
            selectedSprite = null,
            disabledSprite = null
        };
        button.spriteState = state;
        button.transition = UnityEngine.UI.Selectable.Transition.ColorTint;

        var colors = button.colors;
        colors.normalColor = Color.white;                    // 不染色，保持 V9 美术原色
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);   // 轻压一下（真正的反馈是 UiButtonMotion 的缩放）
        colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.75f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
    }

    // ─────────────────────── 四宫格键 ───────────────────────

    /// <summary>
    /// 四个占位键收进 MenuKeys 组、改名 MenuKey_1..4，并加压暗层 + 接上符号引用 → 运行时灰化。
    /// </summary>
    private static void BuildMenuKeys(Transform gBottom, Transform gMenuKeys)
    {
        var keys = new List<Transform>();
        foreach (var ph in gBottom.GetComponentsInChildren<HubMenuPlaceholder>(true))
        {
            // 已经归位到 MenuKeys 的也要处理（补压暗层 / 重刷灰化）→ 不能跳过
            keys.Add(ph.transform);
        }
        if (keys.Count == 0) return;
        keys.Sort((a, b) => ((RectTransform)a).anchoredPosition.x.CompareTo(((RectTransform)b).anchoredPosition.x));

        for (int i = 0; i < keys.Count; i++)
        {
            var key = keys[i];
            key.SetParent(gMenuKeys, false);
            key.name = "MenuKey_" + (i + 1);

            var symbol = key.Find("Symbol") as RectTransform;
            var dim = EnsureDimOverlay(key);

            var ph = key.GetComponent<HubMenuPlaceholder>();
            if (ph != null)
            {
                var so = new SerializedObject(ph);
                so.FindProperty("symbolImage").objectReferenceValue = symbol != null ? symbol.GetComponent<Image>() : null;
                so.FindProperty("dimOverlay").objectReferenceValue = dim;
                // 锁定外观统一写死在这里（占位键的禁用态是固定观感，不靠人工填 Inspector）
                so.FindProperty("lockedTint").colorValue = new Color(0.62f, 0.64f, 0.68f, 1f);
                so.FindProperty("lockedDim").floatValue = 0.28f;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(ph);
                ph.Apply();     // 编辑态也刷成灰化外观（OnEnable 在非 Play 模式不跑）
            }

            // 2026-10-27（用户决定）：四宫格键**从面板上撤下**。
            // 它们无名称、无功能语义，玩家会当按钮点 → 点了没反应（天赋页“无法进入”的来源）。
            // 选关 / 天赋的正式入口是右下两张卡（HubStageEntry / HubTalentEntry）。
            // 对象保留在场景里（inactive），以后要恢复只改 activeSelf。
            key.gameObject.SetActive(false);
        }
    }

    /// <summary>压暗层：无贴图的 Image 会画成实心块，直接盖住整个键。</summary>
    private static Image EnsureDimOverlay(Transform key)
    {
        var dim = key.Find("Dim") as RectTransform;
        if (dim == null)
        {
            var go = new GameObject("Dim", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(key, false);
            dim = (RectTransform)go.transform;
        }
        SetStretch(dim, Vector2.zero, Vector2.zero);
        var img = dim.GetComponent<Image>();
        if (img == null) img = dim.gameObject.AddComponent<Image>();
        img.sprite = null;
        img.type = UnityEngine.UI.Image.Type.Simple;
        img.color = new Color(0f, 0f, 0f, 0.4f);
        img.raycastTarget = false;
        dim.SetAsLastSibling();
        return img;
    }

    // ─────────────────────── 按钮动效 ───────────────────────

    private static void AttachMotion(Transform gBottom, Transform gSide)
    {
        // 开始关卡是主 CTA → 常态呼吸；选关/天赋是次级入口 → **不要呼吸**（用户 2026-10-26 拍板），
        // 只保留按下回弹。
        Attach(gBottom, "StartStageButton", true);
        Attach(gSide, "HubStageEntry", false);
        Attach(gSide, "HubTalentEntry", false);
    }

    private static void Attach(Transform parent, string name, bool idleEnabled)
    {
        var node = FindIn(parent, name);
        if (node == null) return;
        var motion = node.GetComponent<UiButtonMotion>();
        if (motion == null) motion = node.gameObject.AddComponent<UiButtonMotion>();
        motion.target = null;    // 缩放作用在按钮自身
        motion.idleEnabled = idleEnabled;
        EditorUtility.SetDirty(motion);
    }

    // ─────────────────────── 素材导入参数 ───────────────────────

    /// <summary>新切出来的两段底框：导入参数照抄原资源栏（Point / 无 mipmap / PPU 100 / 不压缩）。</summary>
    private static void FixSpriteImports()
    {
        var src = AssetImporter.GetAtPath("Assets/Sprites/UI/Hub/hub_v9_resource_bar.png") as TextureImporter;
        if (src == null) return;
        string[] paths =
        {
            "Assets/Sprites/UI/Hub/hub_v9_stamina_frame.png",
            "Assets/Sprites/UI/Hub/hub_v9_coin_frame.png"
        };
        for (int i = 0; i < paths.Length; i++)
        {
            if (AssetImporter.GetAtPath(paths[i]) == null) AssetDatabase.ImportAsset(paths[i], ImportAssetOptions.ForceUpdate);
            var dst = AssetImporter.GetAtPath(paths[i]) as TextureImporter;
            if (dst == null) continue;
            dst.textureType = src.textureType;
            dst.spriteImportMode = SpriteImportMode.Single;
            dst.filterMode = src.filterMode;
            dst.mipmapEnabled = src.mipmapEnabled;
            dst.spritePixelsPerUnit = src.spritePixelsPerUnit;
            dst.alphaIsTransparency = src.alphaIsTransparency;
            dst.textureCompression = src.textureCompression;
            dst.maxTextureSize = src.maxTextureSize;
            dst.wrapMode = TextureWrapMode.Clamp;
            // 轴心用序列化属性改（TextureImporter 没有公开的 alignment 属性）
            var so = new SerializedObject(dst);
            var align = so.FindProperty("alignment");
            if (align != null) align.intValue = 0;   // Center
            so.ApplyModifiedPropertiesWithoutUndo();
            dst.SaveAndReimport();
        }
    }

    // ─────────────────────── 工具 ───────────────────────

    private static Sprite Load(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/UI/Hub/{name}.png");
    }

    /// <summary>分组文件夹：撑满父节点、零偏移 → 组之间搬节点不改变屏幕位置。</summary>
    private static Transform EnsureGroup(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing != null) return existing;
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one;
        return go.transform;
    }

    /// <summary>把名字匹配的节点搬到目标组（从画布根开始找，跨组也能找到）。</summary>
    private static void MoveTo(Transform to, string name)
    {
        var root = to;
        while (root.parent != null) root = root.parent;      // 画布根
        var node = FindIn(root, name);
        if (node == null || node == to) return;
        node.SetParent(to, false);
    }

    private static Transform FindIn(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindIn(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    private static void SetRect(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        if (rt == null) return;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// 撑满父节点的子节点（底图 / 遮罩层用）。
    /// 注意：不能用 <see cref="SetRect"/> 写点锚点 —— 那会得到**零尺寸**的矩形（看不见）。
    /// </summary>
    private static void SetStretch(RectTransform rt, Vector2 offsetMin, Vector2 offsetMax)
    {
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    /// <summary>底框 Image：铺满父节点。</summary>
    private static Image EnsureFrame(Transform parent, string name, Sprite sprite)
    {
        var rt = parent.Find(name) as RectTransform;
        if (rt == null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            rt = (RectTransform)go.transform;
        }
        SetStretch(rt, Vector2.zero, Vector2.zero);
        var img = rt.GetComponent<Image>();
        if (img == null) img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.type = UnityEngine.UI.Image.Type.Simple;
        img.preserveAspect = false;
        img.raycastTarget = false;
        return img;
    }
}
