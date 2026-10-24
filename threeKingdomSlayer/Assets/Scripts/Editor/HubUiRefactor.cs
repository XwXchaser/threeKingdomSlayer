using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// **落地页 / 战斗 UI 分层重构**（Tools/落地页/重构 UI 分层）
///
/// 目标（用户 2026-10-24 反馈）：
///   ① 落地页（就绪态）只显示菜单向 UI，不再露出血量条 / 100-100 / 技能板；
///   ② UI 节点太多、层级平铺，难选中单个按钮调布局 → 按功能分文件夹，组根统一管理显隐。
///
/// 结构（两个根画布 + 分组文件夹）：
/// <code>
/// BattleHUD(Canvas) order 21
///   └── Combat/
///       ├── Hero/      血条·经验条·技能键·QTE板（战斗专属）
///       ├── Feedback/  连击·击杀数·里程碑·受击闪白·Buff
///       ├── Enemy/     Boss 血条·波次
///       ├── Input/     蓄力指示
///       └── Overlay/   暂停·对话·结算·战败（面板型，自己管显隐）
/// HubCanvas(Canvas) order 25
///   └── Hub/
///       ├── Top/       金币
///       ├── Side/      关卡入口
///       ├── Avatar/    落地页头像（与战斗头像同位）
///       └── Bottom/    开始关卡·任务条·四宫格·天赋树
/// </code>
///
/// 显隐改为**按组根节点**管理（`HubHudModeSwitch`）：名单从 14 个散名字降到 10 个组根/关键节点。
/// 落地页头像单独做一份（复用战斗头像的三张贴图，位置与战斗头像对齐），
/// 这样战斗 HUD 就能整组隐藏，血条与技能板不再漏到落地页。
/// </summary>
public static class HubUiRefactor
{
    public const string SceneName = "Assets/Scenes/Battle.scene";
    public const string CombatCanvasName = "BattleHUD(Canvas)";
    public const string HubCanvasName = "HubCanvas(Canvas)";

    [MenuItem("Tools/落地页/重构 UI 分层")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(SceneName, OpenSceneMode.Single);
        var combat = GameObject.Find(CombatCanvasName);
        var hub = GameObject.Find(HubCanvasName);
        if (combat == null || hub == null)
        {
            Debug.LogError("[HubRefactor] 找不到画布，先跑一次「搭建落地页 UI」");
            return;
        }

        // ── 1) 建分组文件夹（全屏 stretch 容器 → 子节点位置不变）──
        var combatRoot = MakeGroup(combat.transform, "Combat");
        var gHero = MakeGroup(combatRoot, "Hero");
        var gFeedback = MakeGroup(combatRoot, "Feedback");
        var gEnemy = MakeGroup(combatRoot, "Enemy");
        var gInput = MakeGroup(combatRoot, "Input");
        var gOverlay = MakeGroup(combatRoot, "Overlay");

        var hubRoot = MakeGroup(hub.transform, "Hub");
        var hTop = MakeGroup(hubRoot, "Top");
        var hSide = MakeGroup(hubRoot, "Side");
        // 落地页不要头像 → 不建这个组，并把旧的删掉
        var hAvatar = hubRoot.Find("Avatar") as Transform;
        if (hAvatar != null) Object.DestroyImmediate(hAvatar.gameObject);
        hAvatar = null;
        var hBottom = MakeGroup(hubRoot, "Bottom");

        // ── 2) 归位 ──
        Move(combat.transform, gHero, "HeroHUDParent");
        Move(combat.transform, gFeedback, "ComboDisplay", "KillCount", "KillMilestoneDisplay", "HittedOverlay", "BuffDisplayPanel");
        Move(combat.transform, gEnemy, "BossBars", "WaveText");
        Move(combat.transform, gInput, "ChargeIndicator");
        Move(combat.transform, gOverlay, "PauseLayer", "DialogueLayer", "Victory(panel)", "Defeat(panel)");

        // 旧的 CoinCounterUI 是「局内本局获得铜钱」→ 移回战斗画布（Combat/Feedback）
        var legacyCoin = FindIn(hub.transform, "CoinCounter");
        if (legacyCoin != null) legacyCoin.SetParent(gFeedback, false);
        BuildHubCoin(hTop);
        Move(hub.transform, hSide, "HubStageEntry");
        Move(hub.transform, hBottom, "StartStageButton", "HubTaskBar", "HubMenuPlaceholder", "HubTalentEntry", "HubTabBar");

        // ── 3) 落地页**不要头像**（用户 2026-10-24：左上角那个不需要）──
        //    战斗里的头像/血条/经验条原样不动，落地页不再放一份。

        // ── 4) 天赋入口：补墨色底板 + 静态标题 ──
        FixTalentEntry();

        // ── 4.2) 观感修正：白块 / 撑爆的文字 / 空铭牌 ──
        PolishCoinCounter();
        PolishTaskBarIcon();
        PolishAvatarLevel();

        // ── 5) 对话立绘：恢复/规整（BattleDialogueView 依赖这两个序列化引用，不能删）──
        EnsureDialoguePortraits();

        // ── 5.5) 分组容器一律保持激活（是否显示由运行时状态机决定，不要把状态写进场景）──
        Transform[] groups = { gHero, gFeedback, gEnemy, gInput, gOverlay, hTop, hSide, hAvatar, hBottom };
        for (int i = 0; i < groups.Length; i++)
        {
            if (groups[i] != null && !groups[i].gameObject.activeSelf) groups[i].gameObject.SetActive(true);
        }

        // ── 6) 显隐名单改成按组根 ──
        var sw = Object.FindObjectOfType<HubHudModeSwitch>();
        if (sw != null)
        {
            // PauseLayer 是「局内专属」（暂停键在落地页不该出现）→ 单独进 inRunOnly
            var combatGroups = new List<GameObject> { gHero.gameObject, gFeedback.gameObject, gEnemy.gameObject, gInput.gameObject };
            var pauseLayer = FindIn(gOverlay, "PauseLayer");
            if (pauseLayer != null) combatGroups.Add(pauseLayer.gameObject);
            sw.inRunOnly = combatGroups.ToArray();

            // readyOnly = **落地页专属**（关卡态隐藏）。
            // 注意：Hub/Top（金币）是**常驻件**（落地页与战斗都在，只在 overlay 时让位），
            // 不能同时进 readyOnly 和 hubOnly —— 两个名单会互相覆盖（实测金币在关卡态消失）。
            var hubGroups = new List<GameObject> { hSide.gameObject, hBottom.gameObject };
            sw.readyOnly = hubGroups.ToArray();

            var hubOnlyList = new List<GameObject> { hTop.gameObject };
            var battlePortrait = FindIn(combat.transform, "UltPortraitButton");
            if (battlePortrait != null) hubOnlyList.Add(battlePortrait.gameObject);
            sw.hubOnly = hubOnlyList.ToArray();

            // 换了名单必须重新采样：否则 _originalActive 的下标与新名单错位 → 显隐随机
            sw.Recapture();
        }

        // 战斗画布必须保持激活（它挂着 HubHudModeSwitch，关掉就等于关掉整个战斗 UI）
        combat.gameObject.SetActive(true);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[HubRefactor] UI 分层重构完成：战斗 5 组 / 落地页 4 组，显隐改为按组根管理");
    }

    // ─────────────────────────── 工具 ───────────────────────────

    private static Transform MakeGroup(Transform parent, string name)
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
        return go.transform;
    }

    private static void Move(Transform from, Transform to, params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            var node = FindIn(from, names[i]);
            if (node == null) continue;
            node.SetParent(to, false);
        }
    }

    private static Transform FindIn(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindIn(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    /// <summary>
    /// 落地页头像：**一张合成件**（金框 + 武将 + 等级飘带 + 经验条一体）。
    /// 之前是「金框 / 角色图 / 等级牌 / 经验条」四件拼装 → 等级牌像一个无关的框体、经验条像血条。
    /// 位置：与战斗头像同一处（画布 x 中心 -349），整体挂左下。
    /// </summary>
    private static void BuildHubAvatar(Transform group)
    {
        var oldLegacy = group.Find("Avatar");
        if (oldLegacy != null) Object.DestroyImmediate(oldLegacy.gameObject);
        var old = group.Find("AvatarWidget");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var root = new GameObject("AvatarWidget", typeof(RectTransform));
        root.transform.SetParent(group, false);
        var rt = (RectTransform)root.transform;
        // 参考图：头像在**左上角**（x 1-16%、y 1-9%）→ 顶左锚定
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(170f, 210f);          // 素材 571x704 → 等比
        rt.anchoredPosition = new Vector2(10f, -10f);

        var go = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(root.transform, false);
        var prt = (RectTransform)go.transform;
        prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
        prt.offsetMin = Vector2.zero; prt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.sprite = Load("hub_avatar_composite");
        img.preserveAspect = true;
        img.raycastTarget = false;
    }

    /// <summary>
    /// 天赋入口：**卡底板（深墨色 + 金边）+ 发光技能树 + 「天赋」文字**。
    /// 之前只有一条笔刷细带（没有背景框），而且旧逻辑会把同一个 Plate 节点又写回笔刷条素材。
    /// </summary>
    private static void FixTalentEntry()
    {
        var hub = GameObject.Find(HubCanvasName);
        if (hub == null) return;
        var entry = FindIn(hub.transform, "HubTalentEntry");
        if (entry == null) return;
        var rt = (RectTransform)entry;

        // 卡片整体：卡底板 260x408（素材 480x754 → 等比，正好铺满，不做 AR 缩放）
        rt.sizeDelta = new Vector2(260f, 408f);
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-20f, 0f);   // 右下角（参考图「家园」卡位）

        // 卡底板
        var plate = rt.Find("Plate") as RectTransform;
        if (plate == null)
        {
            var pgo = new GameObject("Plate", typeof(RectTransform), typeof(Image));
            pgo.transform.SetParent(rt, false);
            plate = (RectTransform)pgo.transform;
        }
        plate.anchorMin = Vector2.zero; plate.anchorMax = Vector2.one;
        plate.offsetMin = Vector2.zero; plate.offsetMax = Vector2.zero;
        plate.localScale = Vector3.one;
        plate.localRotation = Quaternion.identity;
        var pimg = plate.GetComponent<Image>();
        if (pimg == null) pimg = plate.gameObject.AddComponent<Image>();
        pimg.sprite = Load("hub_talent_card");
        pimg.type = UnityEngine.UI.Image.Type.Simple;
        pimg.preserveAspect = false;   // 框 = 素材比例，直接铺满
        pimg.raycastTarget = false;
        plate.SetAsFirstSibling();

        // 树图标：卡内上部
        var icon = rt.Find("Icon") as RectTransform;
        if (icon == null)
        {
            var igo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            igo.transform.SetParent(rt, false);
            icon = (RectTransform)igo.transform;
        }
        icon.anchorMin = new Vector2(0.5f, 1f);
        icon.anchorMax = new Vector2(0.5f, 1f);
        icon.pivot = new Vector2(0.5f, 1f);
        icon.sizeDelta = new Vector2(190f, 190f);
        icon.anchoredPosition = new Vector2(0f, -26f);
        var iimg = icon.GetComponent<Image>();
        if (iimg == null) iimg = icon.gameObject.AddComponent<Image>();
        iimg.sprite = Load("hub_talent_tree");
        iimg.type = UnityEngine.UI.Image.Type.Simple;
        iimg.preserveAspect = true;
        iimg.raycastTarget = false;

        // 文字：卡内底部亮带
        var label = rt.Find("Label") as RectTransform;
        if (label == null)
        {
            var lgo = new GameObject("Label", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            lgo.transform.SetParent(rt, false);
            label = (RectTransform)lgo.transform;
        }
        label.anchorMin = new Vector2(0f, 0f);
        label.anchorMax = new Vector2(1f, 0f);
        label.pivot = new Vector2(0.5f, 0f);
        label.offsetMin = new Vector2(10f, 14f);
        label.offsetMax = new Vector2(-10f, 62f);
        var tmp = label.GetComponent<TMPro.TextMeshProUGUI>();
        if (tmp == null) tmp = label.gameObject.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = "天赋";
        tmp.fontSize = 34f;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = new Color(0.93f, 0.90f, 0.82f);
        tmp.raycastTarget = false;
    }

    private static void EnsureDialoguePortraits()
    {
        var combat = GameObject.Find(CombatCanvasName);
        if (combat == null) return;
        var overlay = FindIn(combat.transform, "DialogueOverlay");
        if (overlay == null) return;

        var portrait = EnsurePortraitImage(overlay, "BossPortrait", new Vector2(300f, 300f));
        var frame = EnsurePortraitImage(overlay, "BossPortraitFrame", new Vector2(330f, 330f));

        // 重新接线（节点被重建后序列化引用会丢）
        var view = overlay.GetComponent<BattleDialogueView>();
        if (view != null)
        {
            var so = new SerializedObject(view);
            var p1 = so.FindProperty("_bossPortrait");
            var p2 = so.FindProperty("_bossPortraitFrame");
            if (p1 != null) p1.objectReferenceValue = portrait;
            if (p2 != null) p2.objectReferenceValue = frame;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);
        }
    }

    /// <summary>
    /// <summary>落地页右上角的金币显示：图标 + 玩家总铜钱（与局内「本局铜钱」分开）。</summary>
    private static void BuildHubCoin(Transform group)
    {
        var old = group.Find("HubCoin");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var root = new GameObject("HubCoin", typeof(RectTransform));
        root.transform.SetParent(group, false);
        var rt = (RectTransform)root.transform;
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(220f, 80f);
        rt.anchoredPosition = new Vector2(-24f, -24f);

        var iconGo = new GameObject("CoinIcon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(root.transform, false);
        var irt = (RectTransform)iconGo.transform;
        irt.anchorMin = new Vector2(0f, 0.5f);
        irt.anchorMax = new Vector2(0f, 0.5f);
        irt.pivot = new Vector2(0f, 0.5f);
        irt.sizeDelta = new Vector2(56f, 56f);
        irt.anchoredPosition = new Vector2(0f, 0f);
        var icon = iconGo.GetComponent<Image>();
        icon.sprite = Load("hub_coin");
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        var textGo = new GameObject("Amount", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        textGo.transform.SetParent(root.transform, false);
        var trt = (RectTransform)textGo.transform;
        trt.anchorMin = new Vector2(0f, 0.5f);
        trt.anchorMax = new Vector2(1f, 0.5f);
        trt.pivot = new Vector2(0f, 0.5f);
        trt.offsetMin = new Vector2(62f, -30f);
        trt.offsetMax = new Vector2(0f, 30f);
        var tmp = textGo.GetComponent<TMPro.TextMeshProUGUI>();
        tmp.text = "0";
        tmp.fontSize = 36f;
        tmp.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
        tmp.color = new Color(0.93f, 0.85f, 0.62f);
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;

        var display = root.gameObject.AddComponent<HubCoinDisplay>();
        display.iconSprite = Load("hub_coin");
        display.amountText = tmp;
    }

    /// 铜钱计数：原来的 TotalText 宽度只有 18.5px（rect 被压扁）→ 80 号字直接撑爆屏幕右侧；
    /// 图标还是旧 money 素材。→ 换成新金币素材 + 正常文字框。
    /// </summary>
    private static void PolishCoinCounter()
    {
        var hub = GameObject.Find(HubCanvasName);
        if (hub == null) return;
        var counter = FindIn(hub.transform, "CoinCounter");
        if (counter == null) return;

        var icon = counter.Find("CoinIcon");
        if (icon != null)
        {
            var irt = (RectTransform)icon;
            irt.anchorMin = new Vector2(0f, 0.5f);
            irt.anchorMax = new Vector2(0f, 0.5f);
            irt.pivot = new Vector2(0f, 0.5f);
            irt.sizeDelta = new Vector2(56f, 56f);
            irt.anchoredPosition = new Vector2(4f, 0f);
            var img = icon.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = Load("hub_coin");
                img.preserveAspect = true;
                img.raycastTarget = false;
            }
        }

        // 递归改所有 TotalText（场景里可能有重名节点，渲染出来的未必是 Find 到的那个）
        var allTexts = counter.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
        for (int i = 0; i < allTexts.Length; i++)
        {
            var at = allTexts[i];
            at.fontSize = 34f;
            at.color = new Color(0.86f, 0.70f, 0.36f);
            at.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            at.enableWordWrapping = false;
            // 直接写玩家总铜钱：落地页要的是「总铜钱」，而旧组件在运行时并不会刷新它
            at.text = SaveManager.GetCoins().ToString();
        }

        var text = counter.Find("TotalText");
        if (text != null)
        {
            var trt = (RectTransform)text;
            trt.anchorMin = new Vector2(0f, 0.5f);
            trt.anchorMax = new Vector2(1f, 0.5f);
            trt.pivot = new Vector2(0f, 0.5f);
            trt.offsetMin = new Vector2(66f, -30f);
            trt.offsetMax = new Vector2(-6f, 30f);
            var tmp = text.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.fontSize = 34f;
                tmp.color = new Color(0.86f, 0.70f, 0.36f);
                tmp.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
                tmp.enableWordWrapping = false;
                tmp.text = "0";
            }
            // 接线：原来 CoinCounterUI.totalText 没指到这个节点 → 运行时不会刷新，
            // 一直显示场景里写死的旧占位文案「铜钱: 0」
            var ui = counter.GetComponent<CoinCounterUI>();
            if (ui != null)
            {
                var so = new SerializedObject(ui);
                var prop = so.FindProperty("totalText");
                if (prop != null) { prop.objectReferenceValue = tmp; so.ApplyModifiedPropertiesWithoutUndo(); }
                var iconProp = so.FindProperty("coinIcon");
                if (iconProp != null && icon != null) { iconProp.objectReferenceValue = icon.GetComponent<Image>(); so.ApplyModifiedPropertiesWithoutUndo(); }
                EditorUtility.SetDirty(ui);
            }
        }
    }

    /// <summary>任务条的图标位还没接任务系统：Image 没有 sprite 会画成白块 → 先隐藏（LayoutElement 保留占位）。</summary>
    private static void PolishTaskBarIcon()
    {
        var hub = GameObject.Find(HubCanvasName);
        if (hub == null) return;
        var bar = FindIn(hub.transform, "HubTaskBar");
        if (bar == null) return;
        var icon = bar.Find("Content/Icon");
        if (icon == null) return;
        var img = icon.GetComponent<Image>();
        if (img != null) img.enabled = false;   // 空 Image = 白块，先关掉
    }

    /// <summary>头像的等级牌是空块 → 写上等级数字。</summary>
    private static void PolishAvatarLevel()
    {
        var hub = GameObject.Find(HubCanvasName);
        if (hub == null) return;
        var widget = FindIn(hub.transform, "AvatarWidget");
        if (widget == null) return;
        var plate = widget.Find("LevelPlate");
        if (plate == null) return;
        if (plate.Find("LevelText") != null) return;

        var go = new GameObject("LevelText", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        go.transform.SetParent(plate, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(0f, 6f); rt.offsetMax = new Vector2(0f, -26f);
        var tmp = go.GetComponent<TMPro.TextMeshProUGUI>();
        tmp.text = "12";
        tmp.fontSize = 44f;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = new Color(0.95f, 0.92f, 0.84f);
        tmp.raycastTarget = false;
    }

    private static Image EnsurePortraitImage(Transform parent, string name, Vector2 size)
    {
        var existing = parent.Find(name);
        if (existing != null)
        {
            var img = existing.GetComponent<Image>();
            if (img != null)
            {
                var rt0 = (RectTransform)existing;
                rt0.sizeDelta = size;
                rt0.anchorMin = new Vector2(1f, 0f);
                rt0.anchorMax = new Vector2(1f, 0f);
                rt0.pivot = new Vector2(0.5f, 0.5f);
                rt0.anchoredPosition = new Vector2(-180f, 260f);
                img.raycastTarget = false;
                return img;
            }
            return null;
        }

        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = new Vector2(-180f, 260f);
        var image = go.GetComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static Sprite Load(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/UI/Hub/{name}.png");
    }
}
