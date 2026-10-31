using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 战斗 UI 改造工具（2026-10-27，本周战斗 UI 改造；2026-10-30 按刀锋实测再调尺寸）。
///
/// 对应 `Locus/knowledge/plan/weekly-plan-combat-ui-tutorial-20261027.md` §5：
///   ① 暂停键挪左上 / ② 头像整块缩到左下 / ③ 黄色棱形技能排（主动技能+道具）
///   / ④ 通栏红血条（两侧向中间收缩）/ ⑤ 破防条退役 / ⑥ 等级球（球形面片 + 球内「水上涨」）
///
/// **2026-10-30 尺寸规则（用户拍板）**：**排布位置以我们游戏为主，UI 尺寸以《上古卷轴：刀锋》为主**。
/// 参照 `C:/Users/steam/Downloads/screenshot-20261030-154524.png`（刀锋实机 308×615）实测值：
///   • 刀锋左上罗盘 16×25（屏宽 5.2%）→ 暂停块取**折中 88×88**（可点性优先，非照抄罗盘）
///   • 刀锋底通栏生命条 235×5（屏宽 77.9%）→ 血条收窄到 **756×34**、**去掉血量数字**
///   • 刀锋左下大招菱形 ≈ 21×33 实心 → 黄色棱形 **62×125**（并补出可见外框 + 独立冷却遮罩）
///   • 头像 / 等级球 / 铜钱：刀锋无对应物 → **保持现尺寸**
/// 实测对照见 `Locus/knowledge/plan/combat-ui-reference-image-plan-20261030.md` §二之二。
///
/// **为什么做成工具**：场景里的 HUD 是 prefab 实例且带 157 处手改 override，
/// 改 prefab 不会同步到场景；两边必须用同一套函数改，否则模板与实例会漂移。
///
/// **幂等性**：所有节点都是「有就复用、只改属性；没有才新建」——
/// 在 prefab 实例里「删了重建」会产生 added override 并逐次叠加，必须避免。
///
/// 用法：菜单 `三国杀戮 ▸ 战斗UI ▸ 应用新布局`。
/// </summary>
public static class HeroHudLayoutTool
{
    private const string PrefabPath = "Assets/Prefabs/UI/HeroHUD_Zhangfei.prefab";
    private const string CircleSpritePath = "Assets/Sprites/UI/hud_orb_circle.png";
    private const string DiamondSpritePath = "Assets/Sprites/UI/hud_slot_diamond.png";
    private const string BarFillSpritePath = "Assets/Sprites/UI/hud_bar_fill.png";
    // 2026-10-30 白边版正式资产（程序化像素图：白边 + 墨底）
    private const string HealthFrameSpritePath = "Assets/Sprites/UI/Combat/hud_health_frame.png";
    private const string HealthTroughSpritePath = "Assets/Sprites/UI/Combat/hud_health_trough.png";
    private const string HealthFillSpritePath = "Assets/Sprites/UI/Combat/hud_health_fill.png";
    private const string OrbDiscSpritePath = "Assets/Sprites/UI/Combat/hud_orb_disc.png";
    private const string OrbLiquidSpritePath = "Assets/Sprites/UI/Combat/hud_orb_liquid.png";
    private const string PauseFrameSpritePath = "Assets/Sprites/UI/Combat/hud_pause_frame.png";
    private const string PauseIconSpritePath = "Assets/Sprites/UI/Combat/hud_pause_icon.png";

    // ── 尺寸常量（canvas 960×1920 口径）────────────────────────────
    private const float PauseSize = 88f;        // 刀锋罗盘 5.2% 屏宽 → 折中
    private const float PauseGlyphSize = 34f;    // 旧程序化白条（已退役）
    private const float PauseIconSize = 38f;     // 新画风暂停符号
    private const float HealthBarWidth = 756f;   // 刀锋生命条 77.9% 屏宽
    private const float HealthBarHeight = 34f;
    private const float SlotWidth = 62f;         // 刀锋大招菱形实心宽度
    private const float SlotHeight = 125f;
    private const float SlotIconSize = 48f;
    private const float SlotRim = 3f;            // 外框比内面每边大出的量（露出一圈暗金边）
    private const float SlotSpacing = 11.3f;     // 刀锋菱形间距口径
    private const float SlotRowWidth = 293f;     // 4 槽总宽（62*4 + 11.3*3）

    [MenuItem("三国杀戮/战斗UI/应用新布局", false, 2000)]
    public static void ApplyAll()
    {
        bool sceneOk = ApplyToSceneInstance();
        bool prefabOk = ApplyToPrefab();
        Debug.Log($"[HeroHudLayoutTool] 场景实例={sceneOk} prefab模板={prefabOk}");
    }

    [MenuItem("三国杀戮/战斗UI/只改场景实例", false, 2001)]
    public static void ApplyToSceneInstanceMenu() { ApplyToSceneInstance(); }

    [MenuItem("三国杀戮/战斗UI/只改prefab模板", false, 2002)]
    public static void ApplyToPrefabMenu() { ApplyToPrefab(); }

    public static bool ApplyToSceneInstance()
    {
        var hud = UnityEngine.Object.FindObjectOfType<HeroHUD>();
        if (hud == null)
        {
            Debug.LogWarning("[HeroHudLayoutTool] 场景里没有 HeroHUD");
            return false;
        }
        Apply(hud);
        ApplyPauseButton();
        ApplyHubTopHiding();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        return true;
    }

    /// <summary>
    /// 局内隐藏落地页顶栏（<c>Hub/Top</c>：铜钱 + 包子体力）。
    /// 原因（对照参考草图）：草图局内右上角**只有一个铜钱位**，
    /// 而落地页的 <c>Top</c> 与战斗 HUD 自己的 <c>CoinCounter</c> 在同一位置叠成两个铜钱；
    /// 包子体力条在局内没有意义。
    /// → 把 <c>Top</c> 从 <c>hubOnly</c>（只在一级页面打开时隐藏）移到 <c>readyOnly</c>（局内隐藏）。
    /// </summary>
    private static void ApplyHubTopHiding()
    {
        var sw = UnityEngine.Object.FindObjectOfType<HubHudModeSwitch>();
        if (sw == null) return;
        var so = new SerializedObject(sw);
        var hubOnly = so.FindProperty("hubOnly");
        var readyOnly = so.FindProperty("readyOnly");
        if (hubOnly == null || readyOnly == null) return;

        int topIndex = -1;
        for (int i = 0; i < hubOnly.arraySize; i++)
        {
            var v = hubOnly.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
            if (v != null && v.name == "Top") { topIndex = i; break; }
        }
        if (topIndex < 0) return;

        var top = hubOnly.GetArrayElementAtIndex(topIndex).objectReferenceValue;
        hubOnly.DeleteArrayElementAtIndex(topIndex);
        readyOnly.InsertArrayElementAtIndex(readyOnly.arraySize);
        readyOnly.GetArrayElementAtIndex(readyOnly.arraySize - 1).objectReferenceValue = top;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>① 暂停键挪到左上角（蓝底框）。场景内元素，不在 prefab 里。</summary>
    public static void ApplyPauseButton()
    {
        var canvas = GameObject.Find("BattleHUD(Canvas)");
        if (canvas == null) return;
        var btn = canvas.transform.Find("Combat/Overlay/Pause/PauseLayer/PauseButton");
        if (btn == null) return;

        // 2026-10-30：88×88（刀锋罗盘 5.2% 屏宽的折中值；位置不变，仍贴左上角）
        SetRect(btn.gameObject, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(19, -19), new Vector2(PauseSize, PauseSize));
        var img = btn.GetComponent<Image>();
        if (img == null) img = btn.gameObject.AddComponent<Image>();
        var frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PauseFrameSpritePath);
        img.sprite = frameSprite;
        img.color = Color.white;

        // 旧的「鸽子 + 毛笔字」美术退役（刀锋的对应物是小体积符号，不是插画）→ 换成单符号占位
        foreach (var n in new[] { "PausePigeon", "PauseWordBrush", "PauseLabel", "Text" })
        {
            var c = btn.Find(n);
            if (c != null) c.gameObject.SetActive(false);
        }
        EnsureGlyph(btn, "PauseGlyphL", new Vector2(-11f, 0f));
        EnsureGlyph(btn, "PauseGlyphR", new Vector2(11f, 0f));
        // 新画风：整张暂停符号图（替掉程序化的两根白条）
        var iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PauseIconSpritePath);
        if (iconSprite != null)
        {
            var iconGo = btn.Find("PauseIcon");
            if (iconGo == null)
            {
                var created = new GameObject("PauseIcon", typeof(RectTransform));
                created.transform.SetParent(btn, false);
                iconGo = created.transform;
            }
            var iconImg = EnsureImage(iconGo.gameObject);
            iconImg.sprite = iconSprite;
            iconImg.color = Color.white;
            iconImg.type = Image.Type.Simple;
            SetRect(iconGo.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(PauseIconSize, PauseIconSize));
            foreach (var n in new[] { "PauseGlyphL", "PauseGlyphR" })
            {
                var g = btn.Find(n);
                if (g != null) g.gameObject.SetActive(false);
            }
        }

        // 教学对话的「点击任意处继续」原本压在屏幕最底部 → 挡住整个底排 HUD，挪到画面中部
        var adv = GameObject.Find("BattleHUD(Canvas)/Combat/Overlay/Dialogue/DialogueLayer/DialogueOverlay/DialogueAdvanceButton");
        if (adv != null)
            SetRect(adv, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -120), new Vector2(281, 134));
    }

    /// <summary>暂停符号：两段白色竖条（程序化占位，最终美术待 UI 参考图产出后替换）。</summary>
    private static void EnsureGlyph(Transform parent, string name, Vector2 offset)
    {
        var go = parent.Find(name);
        if (go == null)
        {
            var created = new GameObject(name, typeof(RectTransform));
            created.transform.SetParent(parent, false);
            go = created.transform;
        }
        var img = EnsureImage(go.gameObject);
        img.sprite = null;
        img.color = Color.white;
        img.type = Image.Type.Simple;
        SetRect(go.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), offset, new Vector2(9f, PauseGlyphSize));
    }

    public static bool ApplyToPrefab()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (root == null)
        {
            Debug.LogWarning("[HeroHudLayoutTool] 打不开 prefab：" + PrefabPath);
            return false;
        }
        try
        {
            var hud = root.GetComponent<HeroHUD>();
            if (hud == null)
            {
                Debug.LogWarning("[HeroHudLayoutTool] prefab 上没有 HeroHUD");
                return false;
            }
            Apply(hud);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ────────────────────────────────────────────────────────────

    private static void Apply(HeroHUD hud)
    {
        var root = hud.transform;
        var card = root.Find("HudCard");
        if (card == null) { Debug.LogWarning("[HeroHudLayoutTool] HudCard 缺失"); return; }

        var circle = AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath);
        var diamond = AssetDatabase.LoadAssetAtPath<Sprite>(DiamondSpritePath);
        var barFill = AssetDatabase.LoadAssetAtPath<Sprite>(BarFillSpritePath);
        var healthFrameSprite = AssetDatabase.LoadAssetAtPath<Sprite>(HealthFrameSpritePath);
        var healthTroughSprite = AssetDatabase.LoadAssetAtPath<Sprite>(HealthTroughSpritePath);
        var healthFillSprite = AssetDatabase.LoadAssetAtPath<Sprite>(HealthFillSpritePath);
        var orbDiscSprite = AssetDatabase.LoadAssetAtPath<Sprite>(OrbDiscSpritePath);
        var orbLiquidSprite = AssetDatabase.LoadAssetAtPath<Sprite>(OrbLiquidSpritePath);

        // 根节点与卡片：贴底通铺（与场景实例一致）。卡片下缘推到**屏幕最底**（y=130 → 下缘 0）
        SetRect(root.gameObject, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(0, 1050), new Vector2(0, 1920));
        SetRect(card.gameObject, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(0, 130), new Vector2(0, 440));

        // ① 退役节点：经验条（等级球取代）/ 六个招式冷却图标 / 破防条
        Kill(card.Find("ExpBar"));
        foreach (var n in new[]
        {
            "PierceCooldown(Image)", "SweepCooldown(Image)", "StabCooldown(Image)",
            "SlashCooldown(Image)", "LunchCooldown(Image)", "ParryCooldown(Image)"
        }) Kill(card.Find(n));

        // ② Health(Slider) → HealthBar（通栏红条，两侧向中间收缩）
        var healthGo = card.Find("Health(Slider)") != null ? card.Find("Health(Slider)").gameObject
                     : (card.Find("HealthBar") != null ? card.Find("HealthBar").gameObject : null);
        if (healthGo == null) { Debug.LogWarning("[HeroHudLayoutTool] Health(Slider) 缺失"); return; }
        healthGo.name = "HealthBar";
        Kill(healthGo.transform.Find("Handle Slide Area"));
        Kill(healthGo.transform.Find("Fill Area"));
        Kill(healthGo.transform.Find("GuardMeter"));
        var slider = healthGo.GetComponent<Slider>();
        if (slider != null) UnityEngine.Object.DestroyImmediate(slider);

        // 血条：2026-10-30 按刀锋口径收窄到 756（屏宽 77.9%），高度维持 34
        // （刀锋含白框总高当量 ≈ 39 canvas，比我们还高一点，所以只收宽不压扁）
        SetRect(healthGo, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 125), new Vector2(HealthBarWidth - 960f, HealthBarHeight));

        var bg = healthGo.transform.Find("Background_bottom");
        var frame = healthGo.transform.Find("Background_frame");
        var htext = healthGo.transform.Find("Health");

        // ⚠️ 旧节点（Background_bottom / Background_frame）是 prefab 原有子节点，
        // 在 prefab 实例里它们的 CanvasRenderer 网格不会随 RectTransform 重建（实测：只在中缝露出细条）。
        // → 改成**新建** HealthTrough / HealthFrame（与 HealthFillLeft/Right 同套路，实测渲染正常），旧的关掉。
        if (bg != null) bg.gameObject.SetActive(false);
        if (frame != null) frame.gameObject.SetActive(false);

        var troughGo = EnsureChild(healthGo.transform, "HealthTrough");
        var trough = EnsureImage(troughGo);
        trough.sprite = healthTroughSprite != null ? healthTroughSprite : barFill;   // 失去的血（深色轨道）
        trough.color = Color.white;
        trough.type = Image.Type.Simple;
        Stretch(troughGo);

        // 外框 = 一张整条框图（美术已画好：外 1px 近黑 + 内 2px 米白 + 两端阶梯收口）
        var frameGo = EnsureChild(healthGo.transform, "HealthFrame");
        var frameImg = EnsureImage(frameGo);
        frameImg.sprite = healthFrameSprite != null ? healthFrameSprite : barFill;
        frameImg.color = Color.white;
        frameImg.type = Image.Type.Simple;
        Stretch(frameGo);
        foreach (var n in new[] { "HealthFrameTop", "HealthFrameBottom" })
        {
            var old = healthGo.transform.Find(n);
            if (old != null) old.gameObject.SetActive(false);
        }
        TextMeshProUGUI healthTmp = null;
        if (htext != null)
        {
            Stretch(htext.gameObject);
            var tmp = htext.GetComponent<TMPro.TextMeshProUGUI>();
            healthTmp = tmp;
            // 2026-10-30 用户拍板：**本版去掉血量数字**（刀锋的条上也没有数字）
            htext.gameObject.SetActive(false);
            if (tmp != null)
            {
                tmp.fontSize = 22f;
                tmp.alignment = TMPro.TextAlignmentOptions.Center;
                tmp.color = Color.white;
                if (htext.GetComponent<UnityEngine.UI.Outline>() == null)
                {
                    var ol = htext.gameObject.AddComponent<UnityEngine.UI.Outline>();
                    ol.effectColor = new Color(0f, 0f, 0f, 0.95f);
                    ol.effectDistance = new Vector2(3f, -3f);
                }
            }
        }

        // 左右两块镜像填充 → 「两侧向中间收缩」。
        // 填充用**纯色矩形**：旧的红条图（zhangfei_HP_bottom）两端带圆头 + 中间高光，
        // 镜像切两半后两个圆头会挤在中间；纯色才能得到干净的切口。
        var fillL = EnsureChild(healthGo.transform, "HealthFillLeft");
        var fli = EnsureImage(fillL);
        fli.sprite = healthFillSprite != null ? healthFillSprite : barFill;
        fli.type = Image.Type.Filled;
        fli.fillMethod = Image.FillMethod.Horizontal;
        fli.fillOrigin = (int)Image.Origin360.Right;   // 贴中心，向左收缩
        fli.fillAmount = 1f;
        fli.color = new Color(0.85f, 0.15f, 0.12f, 1f);
        // 内缩 3：让金色外框与深褐底槽露出边框（对应刀锋的「白框 + 红芯」结构）
        InsetHalf(fillL.transform, true);

        var fillR = EnsureChild(healthGo.transform, "HealthFillRight");
        var fri = EnsureImage(fillR);
        fri.sprite = healthFillSprite != null ? healthFillSprite : barFill;
        fri.type = Image.Type.Filled;
        fri.fillMethod = Image.FillMethod.Horizontal;
        fri.fillOrigin = (int)Image.Origin360.Left;    // 贴中心，向右收缩
        fri.fillAmount = 1f;
        fri.color = new Color(0.85f, 0.15f, 0.12f, 1f);
        InsetHalf(fillR.transform, false);

        // ③ 头像整块（连大招能量环）搬左下并缩小 —— 三个子节点是写死尺寸，必须一起缩
        var ult = healthGo.transform.Find("UltPortraitButton");
        if (ult == null) ult = card.Find("UltPortraitButton");   // 可能已经搬到 card 下了
        if (ult != null)
        {
            ult.SetParent(card, false);
            // 参考图实测：绿色头像（圆）canvas x 48..178、y 187..317 → 134×130
            SetRect(ult.gameObject, Vector2.zero, Vector2.zero, new Vector2(0, 0), new Vector2(48, 187), new Vector2(130, 130));
            foreach (var n in new[] { "UltBase", "UltFill", "Head" })
            {
                var c = ult.Find(n);
                if (c != null) Fill(c);
            }
            var strip = ult.Find("ComboBuffStrip");
            if (strip != null)
            {
                var srt = strip.GetComponent<RectTransform>();
                srt.localScale = new Vector3(0.43f, 0.43f, 1f);
            }
        }

        // ④ 等级球：圆形面片 + 球内「水上涨」（Filled/Vertical/Bottom）
        var orb = EnsureChild(card, "LevelOrb");
        var orbImg = EnsureImage(orb);
        orbImg.sprite = orbDiscSprite != null ? orbDiscSprite : circle;   // 新画风：墨底 + 米白圈 + 浅色内壁
        orbImg.type = Image.Type.Simple;
        orbImg.color = Color.white;
        // 参考图实测：canvas x 29..72、y 288..341 → 叠在头像左上角（球在头像之上）
        SetRect(orb, Vector2.zero, Vector2.zero, new Vector2(0, 0), new Vector2(29, 288), new Vector2(43, 53));
        var mask = orb.GetComponent<Mask>();
        if (mask == null) mask = orb.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        var liquid = EnsureChild(orb.transform, "OrbLiquid");
        var li = EnsureImage(liquid);
        li.sprite = orbLiquidSprite != null ? orbLiquidSprite : circle;
        li.type = Image.Type.Filled;
        li.fillMethod = Image.FillMethod.Vertical;
        li.fillOrigin = (int)Image.Origin360.Bottom;
        li.fillAmount = 0f;
        li.color = new Color(0.30f, 0.62f, 0.86f, 1f);
        Stretch(liquid);
        InsetRect(liquid, 4f, 4f, 4f, 4f);   // 内缩：别盖住球的白圈

        var orbText = EnsureChild(orb.transform, "OrbLevelText");
        var ot = orbText.GetComponent<TextMeshProUGUI>();
        if (ot == null) ot = orbText.AddComponent<TextMeshProUGUI>();
        ot.text = "1";
        ot.fontSize = 22;
        ot.alignment = TextAlignmentOptions.Center;
        ot.color = new Color(0.10f, 0.10f, 0.12f, 1f);   // 参考图：浅底深字
        ot.enableWordWrapping = false;
        if (healthTmp != null && healthTmp.font != null) ot.font = healthTmp.font;   // 沿用血条数字的字体，避免缺字形
        if (orbText.GetComponent<UnityEngine.UI.Outline>() == null)
        {
            var ol = orbText.AddComponent<UnityEngine.UI.Outline>();
            ol.effectColor = new Color(1f, 1f, 1f, 0.9f);
            ol.effectDistance = new Vector2(1.2f, -1.2f);
        }
        Stretch(orbText);

        // ⑤ 黄色棱形排（主动技能 + 道具）—— 搬出 FlipPanel 与头像同行
        var fibT = card.Find("FlipPanel/FrontFace/FrontItemBar");
        if (fibT == null) fibT = card.Find("FrontItemBar");
        if (fibT != null)
        {
            fibT.SetParent(card, false);
            // 2026-10-30：棱形 62×125（刀锋大招菱形实心口径），行起点与垂直中心不变
            SetRect(fibT.gameObject, Vector2.zero, Vector2.zero, new Vector2(0, 0.5f), new Vector2(184, 252), new Vector2(SlotRowWidth, SlotHeight));
            for (int i = 0; i < fibT.childCount; i++)
            {
                var slot = fibT.GetChild(i);
                // 槽位锚点用 (0,0.5)：anchoredPosition 相对的是**父级左缘**，才能真正“从左往右排”
                FixedLeft(slot, SlotWidth);
                var slotRt = slot.GetComponent<RectTransform>();
                if (slotRt != null) slotRt.sizeDelta = new Vector2(SlotWidth, SlotHeight);

                // 外框比内面每边大 SlotRim → 露出一圈暗金边（三层同图叠放导致外框不可见的问题）
                SetFixed(FindDeep(slot, "ButtonFrame"), SlotWidth + SlotRim * 2f, SlotHeight + SlotRim * 2f);
                SetDiamond(FindDeep(slot, "ButtonFrame"), diamond, new Color(0.72f, 0.56f, 0.16f, 1f));
                SetFixed(FindDeep(slot, "ButtonFace"), SlotWidth, SlotHeight);
                SetDiamond(FindDeep(slot, "ButtonFace"), diamond, new Color(0.95f, 0.78f, 0.25f, 1f));
                Fixed(FindDeep(slot, "Icon"), SlotIconSize);
                Fixed(FindDeep(slot, "ReadyGlow"), SlotIconSize);

                // 冷却遮罩：不再复用图标贴图（会把整块菱形糊住）→ 独立菱形遮罩
                var dim = FindDeep(slot, "CooldownDim");
                if (dim != null)
                {
                    SetFixed(dim, SlotWidth, SlotHeight);
                    var dimImg = EnsureImage(dim.gameObject);
                    dimImg.sprite = diamond;
                    dimImg.type = Image.Type.Simple;
                    dimImg.color = new Color(0f, 0f, 0f, 0.55f);
                }
                var cool = FindDeep(slot, "CooldownFill");
                if (cool != null)
                {
                    SetFixed(cool, SlotWidth, SlotHeight);
                    var coolImg = EnsureImage(cool.gameObject);
                    coolImg.sprite = diamond;
                    coolImg.type = Image.Type.Filled;
                    coolImg.fillMethod = Image.FillMethod.Vertical;
                    coolImg.fillOrigin = (int)Image.Origin360.Bottom;
                    coolImg.color = new Color(0f, 0f, 0f, 0.8f);
                }

                // 道具样式用的 Frame（技能样式下 BuffIcon 会把它隐藏；道具时再打开）
                var fr = FindDeep(slot, "Frame");
                if (fr != null)
                {
                    SetFixed(fr, SlotWidth, SlotHeight);
                    SetDiamond(fr, diamond, new Color(0.95f, 0.78f, 0.25f, 1f));
                    fr.gameObject.SetActive(false);
                }

                // 运行时 BuffIcon.SyncCooldownSprite 会把遮罩换回图标 → 给它一个专属遮罩图
                var iconComp = slot.GetComponent<BuffIcon>();
                if (iconComp != null && diamond != null)
                {
                    var bi = new SerializedObject(iconComp);
                    var maskProp = bi.FindProperty("_cooldownMaskSprite");
                    if (maskProp != null)
                    {
                        maskProp.objectReferenceValue = diamond;
                        bi.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
            }
            // 运行时 SetFrame(_skillFrame) 会盖掉 Frame 的图 —— 所以字段也换成菱形
            var fibComp = fibT.GetComponent<FrontItemBar>();
            if (fibComp != null && diamond != null)
            {
                var fibSo = new SerializedObject(fibComp);
                fibSo.FindProperty("_skillFrame").objectReferenceValue = diamond;
                fibSo.FindProperty("_slotSpacing").floatValue = SlotSpacing;
                fibSo.ApplyModifiedPropertiesWithoutUndo();
            }
            // 改完槽位尺寸后立刻重排一次（否则 Edit Mode 下槽位还停在旧位）
            if (fibComp != null) fibComp.LayoutSlots();
        }

        // ⑥ 旧木板布局的遗留件退役：木板 + **关卡节点进度条**（4 节点 + 连线 + 玩家旗标）
        //    节点进度条在 Edit Mode 里是激活的（运行时被翻牌卡互斥隐藏），会残留在编辑器画面上，
        //    且正好压在新底排（黄色棱形排）上。组件保留，只是关掉。
        var boardFront = card.Find("FlipPanel/FrontFace/BoardInnerFront");
        if (boardFront != null) boardFront.gameObject.SetActive(false);
        var sharedFront = card.Find("FlipPanel/FrontFace/BoardSharedFrame");
        if (sharedFront != null) sharedFront.gameObject.SetActive(false);
        var stageProgress = card.Find("FlipPanel/FrontFace/StageProgressBar");
        if (stageProgress != null) stageProgress.gameObject.SetActive(false);

        // ⑦ 接线
        var so = new SerializedObject(hud);
        so.FindProperty("healthFillLeft").objectReferenceValue = fli;
        so.FindProperty("healthFillRight").objectReferenceValue = fri;
        so.FindProperty("healthText").objectReferenceValue = htext != null ? htext.GetComponent<TextMeshProUGUI>() : null;
        so.FindProperty("healthBottomImage").objectReferenceValue = trough;
        so.FindProperty("healthFrameImage").objectReferenceValue = frameImg;
        so.FindProperty("levelOrbFill").objectReferenceValue = li;
        so.FindProperty("levelOrbText").objectReferenceValue = ot;
        so.FindProperty("levelOrbTarget").objectReferenceValue = orb.GetComponent<RectTransform>();
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ── 小工具 ────────────────────────────────────────────────

    private static GameObject EnsureChild(Transform parent, string name)
    {
        var existing = parent.Find(name);
        if (existing != null) return existing.gameObject;
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    /// <summary>按名字**深度查找**后代（层级在不同 prefab 版本间会变，不可靠路径）。</summary>
    private static Transform FindDeep(Transform parent, string name)
    {
        if (parent == null) return null;
        foreach (Transform t in parent.GetComponentsInChildren<Transform>(true))
        {
            if (t != parent && t.name == name) return t;
        }
        return null;
    }

    private static Image EnsureImage(GameObject go)
    {
        var img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        return img;
    }

    private static void Kill(Transform t)
    {
        if (t != null) UnityEngine.Object.DestroyImmediate(t.gameObject);
    }

    private static void SetRect(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    private static void Stretch(GameObject go)
    {
        SetRect(go, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
    }

    /// <summary>铺满父节点（锚点拉伸，sizeDelta 归零）。</summary>
    private static void Fill(Transform t)
    {
        if (t == null) return;
        SetRect(t.gameObject, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
    }

    /// <summary>铺满父节点并向内缩（用于画边框）。</summary>
    private static void InsetRect(GameObject go, float left, float bottom, float right, float top)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    /// <summary>在父级上沿或下沿贴一条实心细条（边框用）。</summary>
    private static void EdgeStrip(Transform parent, string name, bool atTop, float height, Sprite sprite, Color color)
    {
        var go = EnsureChild(parent, name);
        var img = EnsureImage(go);
        img.sprite = sprite;
        img.color = color;
        img.type = Image.Type.Simple;
        var rt = go.GetComponent<RectTransform>();
        if (atTop)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, -height);
        }
        else
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, height);
        }
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    /// <summary>固定尺寸、居中于父节点。</summary>
    private static void Fixed(Transform t, float size)
    {
        if (t == null) return;
        SetRect(t.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
    }

    /// <summary>固定尺寸、居中于父节点（宽高可不同）。</summary>
    private static void SetFixed(Transform t, float width, float height)
    {
        if (t == null) return;
        SetRect(t.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, height));
    }

    /// <summary>固定尺寸、横向以父节点**左缘**为基准（配合 LayoutSlots 的左对齐）。</summary>
    private static void FixedLeft(Transform t, float size)
    {
        if (t == null) return;
        SetRect(t.gameObject, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
    }

    /// <summary>
    /// 血条填充块内缩：上下各留 3（露出金框），外侧端头留 3（露出底槽），中缝不缩（切口干净）。
    /// </summary>
    private static void InsetHalf(Transform t, bool isLeft)
    {
        if (t == null) return;
        var rt = t.GetComponent<RectTransform>();
        rt.anchorMin = isLeft ? new Vector2(0f, 0f) : new Vector2(0.5f, 0f);
        rt.anchorMax = isLeft ? new Vector2(0.5f, 1f) : new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = isLeft ? new Vector2(3f, 3f) : new Vector2(0f, 3f);
        rt.offsetMax = isLeft ? new Vector2(0f, -3f) : new Vector2(-3f, -3f);
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    private static void SetDiamond(Transform t, Sprite diamond, Color? tint = null)
    {
        if (t == null || diamond == null) return;
        var img = t.GetComponent<Image>();
        if (img == null) return;
        img.sprite = diamond;
        if (tint.HasValue) img.color = tint.Value;
    }
}
