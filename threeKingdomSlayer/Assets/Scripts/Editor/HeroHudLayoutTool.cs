using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 战斗 UI 改造工具（2026-10-27，本周战斗 UI 改造）。
///
/// 对应 `Locus/knowledge/plan/weekly-plan-combat-ui-tutorial-20261027.md` §5：
///   ① 暂停键挪左上 / ② 头像整块缩到左下 / ③ 黄色棱形技能排（主动技能+道具）
///   / ④ 通栏红血条（两侧向中间收缩）/ ⑤ 破防条退役 / ⑥ 等级球（球形面片 + 球内「水上涨」）
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
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        return true;
    }

    /// <summary>① 暂停键挪到左上角（蓝底框）。场景内元素，不在 prefab 里。</summary>
    public static void ApplyPauseButton()
    {
        var canvas = GameObject.Find("BattleHUD(Canvas)");
        if (canvas == null) return;
        var btn = canvas.transform.Find("Combat/Overlay/Pause/PauseLayer/PauseButton");
        if (btn == null) return;

        SetRect(btn.gameObject, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(26, -26), new Vector2(150, 150));
        var img = btn.GetComponent<Image>();
        if (img == null) img = btn.gameObject.AddComponent<Image>();
        img.color = new Color(0.16f, 0.42f, 0.85f, 0.92f);

        // 子节点原本锚在父级右上角 → 改成居中锚，逐个摆位
        CenterChild(btn, "PausePigeon", new Vector2(0, 26), new Vector2(112, 86));
        CenterChild(btn, "PauseWordBrush", new Vector2(0, -34), new Vector2(112, 50));
        CenterChild(btn, "PauseLabel", new Vector2(0, -34), new Vector2(104, 56));
        CenterChild(btn, "Text", Vector2.zero, new Vector2(0, 0));
    }

    private static void CenterChild(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var c = parent.Find(name);
        if (c == null) return;
        SetRect(c.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
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

        // 根节点与卡片：贴底通铺（与场景实例一致），卡片高 440
        SetRect(root.gameObject, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(0, 1050), new Vector2(0, 1920));
        SetRect(card.gameObject, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(0, 195), new Vector2(0, 440));

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

        // 卡片局部坐标：HudCard 的锚点参考点是它的**下边缘**（anchorMin/Max y=0）
        // → anchoredPosition.y 是「离卡片下边缘多高」，卡片高 440。
        // 高度 150：接近旧设计的 168（框图 zhangfei_HP_Background 是 5:1，旧 prefab 用的是 6.7:1）
        SetRect(healthGo, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(-40, 150));

        var bg = healthGo.transform.Find("Background_bottom");
        var frame = healthGo.transform.Find("Background_frame");
        var htext = healthGo.transform.Find("Health");
        if (bg != null)
        {
            Stretch(bg.gameObject);
            var bgImg = EnsureImage(bg.gameObject);
            bgImg.sprite = barFill;                       // 纯色矩形底槽 = 失去的血
            bgImg.color = new Color(0.17f, 0.14f, 0.12f, 1f);
            bgImg.type = Image.Type.Simple;
        }
        if (frame != null) Stretch(frame.gameObject);      // 金色花框（zhangfei_HP_Background），比例接近原设计
        TextMeshProUGUI healthTmp = null;
        if (htext != null)
        {
            Stretch(htext.gameObject);
            var tmp = htext.GetComponent<TMPro.TextMeshProUGUI>();
            healthTmp = tmp;
            if (tmp != null)
            {
                tmp.fontSize = 34f;
                tmp.alignment = TMPro.TextAlignmentOptions.Center;
                tmp.color = Color.white;
                if (htext.GetComponent<UnityEngine.UI.Outline>() == null)
                {
                    var ol = htext.gameObject.AddComponent<UnityEngine.UI.Outline>();
                    ol.effectColor = new Color(0f, 0f, 0f, 0.9f);
                    ol.effectDistance = new Vector2(2f, -2f);
                }
            }
        }

        // 左右两块镜像填充 → 「两侧向中间收缩」。
        // 填充用**纯色矩形**：旧的红条图（zhangfei_HP_bottom）两端带圆头 + 中间高光，
        // 镜像切两半后两个圆头会挤在中间；纯色才能得到干净的切口。
        var fillL = EnsureChild(healthGo.transform, "HealthFillLeft");
        var fli = EnsureImage(fillL);
        fli.sprite = barFill;
        fli.type = Image.Type.Filled;
        fli.fillMethod = Image.FillMethod.Horizontal;
        fli.fillOrigin = (int)Image.Origin360.Right;   // 贴中心，向左收缩
        fli.fillAmount = 1f;
        fli.color = new Color(0.85f, 0.15f, 0.12f, 1f);
        SetRect(fillL, new Vector2(0, 0), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        var fillR = EnsureChild(healthGo.transform, "HealthFillRight");
        var fri = EnsureImage(fillR);
        fri.sprite = barFill;
        fri.type = Image.Type.Filled;
        fri.fillMethod = Image.FillMethod.Horizontal;
        fri.fillOrigin = (int)Image.Origin360.Left;    // 贴中心，向右收缩
        fri.fillAmount = 1f;
        fri.color = new Color(0.85f, 0.15f, 0.12f, 1f);
        SetRect(fillR, new Vector2(0.5f, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        // ③ 头像整块（连大招能量环）搬左下并缩小 —— 三个子节点是写死尺寸，必须一起缩
        var ult = healthGo.transform.Find("UltPortraitButton");
        if (ult == null) ult = card.Find("UltPortraitButton");   // 可能已经搬到 card 下了
        if (ult != null)
        {
            ult.SetParent(card, false);
            SetRect(ult.gameObject, Vector2.zero, Vector2.zero, new Vector2(0, 0), new Vector2(44, 46), new Vector2(120, 120));
            foreach (var n in new[] { "UltBase", "UltFill", "Head" })
            {
                var c = ult.Find(n);
                if (c != null) Fill(c);
            }
            var strip = ult.Find("ComboBuffStrip");
            if (strip != null)
            {
                var srt = strip.GetComponent<RectTransform>();
                srt.localScale = new Vector3(0.42f, 0.42f, 1f);
            }
        }

        // ④ 等级球：圆形面片 + 球内「水上涨」（Filled/Vertical/Bottom）
        var orb = EnsureChild(card, "LevelOrb");
        var orbImg = EnsureImage(orb);
        orbImg.sprite = circle;
        orbImg.type = Image.Type.Simple;
        orbImg.color = new Color(0.12f, 0.13f, 0.16f, 0.92f);
        SetRect(orb, Vector2.zero, Vector2.zero, new Vector2(0, 0), new Vector2(8, 148), new Vector2(56, 56));
        var mask = orb.GetComponent<Mask>();
        if (mask == null) mask = orb.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        var liquid = EnsureChild(orb.transform, "OrbLiquid");
        var li = EnsureImage(liquid);
        li.sprite = circle;
        li.type = Image.Type.Filled;
        li.fillMethod = Image.FillMethod.Vertical;
        li.fillOrigin = (int)Image.Origin360.Bottom;
        li.fillAmount = 0f;
        li.color = new Color(0.98f, 0.78f, 0.25f, 1f);
        Stretch(liquid);

        var orbText = EnsureChild(orb.transform, "OrbLevelText");
        var ot = orbText.GetComponent<TextMeshProUGUI>();
        if (ot == null) ot = orbText.AddComponent<TextMeshProUGUI>();
        ot.text = "1";
        ot.fontSize = 26;
        ot.alignment = TextAlignmentOptions.Center;
        ot.color = Color.white;
        ot.enableWordWrapping = false;
        if (healthTmp != null && healthTmp.font != null) ot.font = healthTmp.font;   // 沿用血条数字的字体，避免缺字形
        if (orbText.GetComponent<UnityEngine.UI.Outline>() == null)
        {
            var ol = orbText.AddComponent<UnityEngine.UI.Outline>();
            ol.effectColor = new Color(0f, 0f, 0f, 0.95f);
            ol.effectDistance = new Vector2(1.5f, -1.5f);
        }
        Stretch(orbText);

        // ⑤ 黄色棱形排（主动技能 + 道具）—— 搬出 FlipPanel 与头像同行
        var fibT = card.Find("FlipPanel/FrontFace/FrontItemBar");
        if (fibT == null) fibT = card.Find("FrontItemBar");
        if (fibT != null)
        {
            fibT.SetParent(card, false);
            // pivot 必须取 (0, 0.5)：槽位自身锚点是 (0, 0.5) → anchoredPosition 相对的是
            // **父级的垂直中心**。pivot 取 (0,0)（下缘）会让槽位整体比 pivot 高一半高度 → 与头像错行。
            SetRect(fibT.gameObject, Vector2.zero, Vector2.zero, new Vector2(0, 0.5f), new Vector2(172, 106), new Vector2(560, 120));
            for (int i = 0; i < fibT.childCount; i++)
            {
                var slot = fibT.GetChild(i);
                // 槽位锚点用 (0,0.5)：anchoredPosition 相对的是**父级左缘**，才能真正“从左往右排”
                FixedLeft(slot, 104);
                Fill(FindDeep(slot, "ButtonFrame"));
                Fill(FindDeep(slot, "ButtonFace"));
                Fixed(FindDeep(slot, "Icon"), 72);
                Fill(FindDeep(slot, "CooldownDim"));
                Fill(FindDeep(slot, "CooldownFill"));
                // 棱形底框：道具样式走 Frame，技能样式走 ButtonFrame/ButtonFace —— 两套都要换
                SetDiamond(FindDeep(slot, "ButtonFrame"), diamond, new Color(0.72f, 0.56f, 0.16f, 1f));
                SetDiamond(FindDeep(slot, "ButtonFace"), diamond, new Color(0.95f, 0.78f, 0.25f, 1f));
                var fr = FindDeep(slot, "Frame");
                if (fr != null)
                {
                    Fill(fr);
                    SetDiamond(fr, diamond, new Color(0.95f, 0.78f, 0.25f, 1f));
                    fr.gameObject.SetActive(true);
                }
            }
            // 运行时 SetFrame(_skillFrame) 会盖掉 Frame 的图 —— 所以字段也换成菱形
            var fibComp = fibT.GetComponent<FrontItemBar>();
            if (fibComp != null && diamond != null)
            {
                var fibSo = new SerializedObject(fibComp);
                fibSo.FindProperty("_skillFrame").objectReferenceValue = diamond;
                fibSo.FindProperty("_slotSpacing").floatValue = 12f;
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
        so.FindProperty("healthBottomImage").objectReferenceValue = bg != null ? bg.GetComponent<Image>() : null;
        so.FindProperty("healthFrameImage").objectReferenceValue = frame != null ? frame.GetComponent<Image>() : null;
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

    /// <summary>固定尺寸、居中于父节点。</summary>
    private static void Fixed(Transform t, float size)
    {
        if (t == null) return;
        SetRect(t.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
    }

    /// <summary>固定尺寸、横向以父节点**左缘**为基准（配合 LayoutSlots 的左对齐）。</summary>
    private static void FixedLeft(Transform t, float size)
    {
        if (t == null) return;
        SetRect(t.gameObject, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
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
