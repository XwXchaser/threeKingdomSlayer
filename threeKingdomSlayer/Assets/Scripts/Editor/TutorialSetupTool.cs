using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// **教学关配置工具**（Tools ▸ 三国杀戮 ▸ 教学关 ▸ 应用教学关新配置）。
///
/// 严格按 2026-11-01 用户给的教程关卡流程配：
///
///   节点一（N1）
///     wave0 → 教**点击触发攻击**
///     wave1 → 教**多次点击触发三连 stab**；完成后补一句「多次攻击可以打断敌人的攻击」（不暂停）
///     两波经验给 0 → 节点一不升级、不弹三选1
///   节点二（E0）
///     wave0 → **1012（霸体）**：敌人起霸体技时**冻结** → 教按住防御键格挡这一击
///             经验 ×3 → 击杀这一只正好升级 → **弹出唯一一次三选1（技能）**
///     wave1 → **1012（霸体）**：教**用技能打断**霸体攻击（指着玩家刚选中的那个技能）
///   节点之间 → **黑幕**（半透明压暗，不是全屏纯黑）
///
/// 幂等：可重复执行。已有资产只改内容，不重建（不丢 GUID 引用）。
/// </summary>
public static class TutorialSetupTool
{
    private const string ScenePath = "Assets/Scenes/Battle.scene";
    private const string N1BattlePath = "Assets/RouteData/Tutorial/Tutorial_N1_Battle.asset";
    private const string E0BattlePath = "Assets/RouteData/Tutorial/Tutorial_E0_Battle.asset";
    private const string RoutePath = "Assets/RouteData/Tutorial/Tutorial_Route.asset";
    private const string DialogueDir = "Assets/ScriptableObjects/Dialogue/";
    private const string SkillPoolPath = "Assets/ScriptableObjects/ActiveSkills/ActiveSkillPoolConfig.asset";

    private const string CyclonePath = "Assets/ScriptableObjects/ActiveSkills/ActiveSkill_Cyclone.asset";
    private const string WavePath = "Assets/ScriptableObjects/ActiveSkills/ActiveSkill_Wave.asset";
    private const string FireLinePath = "Assets/ScriptableObjects/ActiveSkills/ActiveSkill_FireLine.asset";

    /// <summary>节点二 1012 的血量倍率（有效血量 = 100 × 1.5 = 150）。</summary>
    private const float SuperArmorHealthMultiplier = 1.5f;
    /// <summary>节点二 1012 的伤害倍率（3 × 0.3 = 0.9 一下，教学期间不会被打死）。</summary>
    private const float SuperArmorDamageMultiplier = 0.3f;
    /// <summary>节点二 1012 的经验倍率（1012 基础 80 × 3 = 240 ≥ 升 1 级所需 200）。</summary>
    private const float SuperArmorExpMultiplier = 3f;

    /// <summary>节点一用的敌人 id（1011 = **没有 C 技**的普通兵 → 轻攻击能打断，\n" +
    /// 「多次攻击可以打断敌人的攻击」这句才成立）。形象与 1012 一致（都是旧武将）。</summary>
    private const int NormalEnemyId = 1011;

    /// <summary>节点二用的敌人 id（1012 = **带 C 技霸体前摇**的兵，用来教霸体与打断）。</summary>
    private const int SuperArmorEnemyId = 1012;

    [MenuItem("Tools/三国杀戮/教学关/应用教学关新配置")]
    public static void Apply()
    {
        var log = new List<string>();

        // ── 1) 台词资产 ────────────────────────────────────────────────
        var dlgWave1 = Load<DialogueEventData>(DialogueDir + "tutorial_t0_wave1.asset");
        var dlgCombo = EnsureDialogue("tutorial_t0_combo", "连点三下！一枪接一枪，第三下最狠。", log);
        var dlgInterrupt = EnsureDialogue("tutorial_t0_interrupt", "敌人抡招的时候，多打几下就能把它打断。", log);
        var dlgArmor = Load<DialogueEventData>(DialogueDir + "tutorial_t0_wave3.asset");
        var dlgChoice = EnsureDialogue("tutorial_t0_choice",
            "击杀敌人获得经验，经验满了就会升级，升级会弹出三选1奖励。\n听我的，选【海浪】——它能把霸体敌人推回去，打断他的招。", log);
        var dlgSkillCyclone = EnsureDialogue("tutorial_t0_skill_cyclone", "它抡起霸体招了！用【旋风】把它吹飞！", log);
        var dlgSkillWave = EnsureDialogue("tutorial_t0_skill_wave", "它抡起霸体招了！用【海浪】把它推回去！", log);
        var dlgSkillGeneric = EnsureDialogue("tutorial_t0_skill_generic", "它抡起霸体招了！用技能打断它！", log);

        // ── 2) 节点一波次：经验给 0（不升级、不弹三选1）+ 敌人统一用 1012 的新形象 ──
        var n1 = Load<StageConfig>(N1BattlePath);
        if (n1 == null) { log.Add("找不到 " + N1BattlePath); Report(log); return; }
        for (int i = 0; i < n1.waves.Count; i++)
        {
            n1.waves[i].expMultiplier = 0f;
            // 敌人形象统一成 1012（新贼兵素材）：节点一原来的 1011 是旧步兵形象
            for (int r = 0; r < n1.waves[i].rows.Count; r++)
            {
                var row = n1.waves[i].rows[r];
                if (row == null || row.enemyIds == null) continue;
                for (int e = 0; e < row.enemyIds.Length; e++)
                    if (row.enemyIds[e] != 0 && row.enemyIds[e] != RowConfig.RhythmGateMarker)
                        row.enemyIds[e] = NormalEnemyId;
            }
        }
        EditorUtility.SetDirty(n1);
        log.Add("节点一经验倍率归 0（不升级、不弹三选1）；敌人 = " + NormalEnemyId + "（普通兵，可被轻攻击打断）");

        // ── 3) 节点二：两波都是霸体敌人（1012），第一波经验给足 → 那一杀升级 → 三选1 ──
        var e0 = Load<StageConfig>(E0BattlePath);
        if (e0 == null) { log.Add("找不到 " + E0BattlePath); Report(log); return; }
        e0.waves.Clear();
        e0.waves.Add(MakeWave(2, SuperArmorEnemyId, SuperArmorHealthMultiplier, SuperArmorDamageMultiplier, SuperArmorExpMultiplier));
        e0.waves.Add(MakeWave(3, SuperArmorEnemyId, SuperArmorHealthMultiplier, SuperArmorDamageMultiplier, 1f));
        EditorUtility.SetDirty(e0);
        log.Add("节点二波次：" + string.Join(" / ", e0.waves.ConvertAll(w =>
            "wave" + w.waveId + " 敌人1012 hp×" + w.healthMultiplier + " dmg×" + w.damageMultiplier + " exp×" + w.expMultiplier)));

        // ── 4) 移除测试用的冻结技能 ────────────────────────────────────
        RemoveTestFreezeSkill(log);

        AssetDatabase.SaveAssets();

        // ── 5) 场景：教学导演 + 黑幕 ───────────────────────────────────
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) { log.Add("取消：未保存当前场景"); Report(log); return; }
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        var director = UnityEngine.Object.FindObjectOfType<TutorialStageDirector>();
        if (director == null) { log.Add("场景里找不到 TutorialStageDirector"); Report(log); return; }

        var n1Battle = director.n1Battle;
        var route = Load<FakeRouteStageConfig>(RoutePath);

        // 5.1 台词数组（节点二第二波的台词由「技能打断拍」按玩家选中的技能给）
        director.n1Dialogues = new[] { dlgWave1, dlgCombo };
        director.e0Dialogues = new[] { dlgArmor, null };
        director.choiceTeachingDialogue = dlgChoice;
        director.skillDialogueCyclone = dlgSkillCyclone;
        director.skillDialogueWave = dlgSkillWave;
        director.skillDialogueGeneric = dlgSkillGeneric;

        // 5.2 手势拍
        director.gestureSteps = new[]
        {
            // 节点一 wave0：点击触发攻击
            new TutorialGestureStep
            {
                battle = n1Battle, waveIndex = 0, kind = GestureHintKind.Tap,
                delay = 0.2f, playSeconds = 1.2f, pauseUntilPerformed = true,
            },
            // 节点一 wave1：连点三下出三连 + 完成后补一句「多次攻击可以打断」
            new TutorialGestureStep
            {
                battle = n1Battle, waveIndex = 1, kind = GestureHintKind.TapCombo,
                requiredHits = 3, delay = 0.2f, playSeconds = 0.9f, pauseUntilPerformed = true,
                afterDialogue = dlgInterrupt,
            },
            // 路线出口（点箭头）
            new TutorialGestureStep
            {
                battle = null, waveIndex = -2, kind = GestureHintKind.Tap,
                delay = 0.2f, playSeconds = 1.2f, pauseUntilPerformed = false,
            },
            // 节点二 wave0：霸体 → 冻结 → 教按住防御键
            new TutorialGestureStep
            {
                battle = director.e0Battle, waveIndex = 0, kind = GestureHintKind.DefendButton,
                freezeOnCAttack = true, delay = 0.2f, playSeconds = 1.2f, pauseUntilPerformed = false,
            },
            // 节点二 wave1：教用技能打断霸体（指向玩家在三选1 里选中的技能）
            new TutorialGestureStep
            {
                battle = director.e0Battle, waveIndex = 1, kind = GestureHintKind.SkillSlot,
                skillUpgradeId = "", delay = 0.2f, playSeconds = 1.2f, pauseUntilPerformed = false,
            },
        };

        // 5.3 唯一一次三选1 的候选（海浪 + 旋风 + 第三个先随便配着）
        director.tutorialChoicePool = new List<UpgradeDefinition>
        {
            Load<UpgradeDefinition>(WavePath),        // 海浪（推荐，能打断霸体）
            Load<UpgradeDefinition>(CyclonePath),     // 旋风（也能打断霸体）
            Load<UpgradeDefinition>(FireLinePath),    // 火线（先随便配着）
        };
        director.tutorialChoicePool.RemoveAll(x => x == null);
        director.recommendedChoice = director.tutorialChoicePool.Count > 0 ? director.tutorialChoicePool[0] : null;
        director.tutorialIceSkill = null;   // 测试用的冻结技能不再给

        EditorUtility.SetDirty(director);

        // 5.4 黑幕（半透明压暗，不是全屏纯黑）
        var blackout = EnsureBlackout(route);
        if (blackout != null)
        {
            blackout.routeConfig = route;
            blackout.tutorialRouteConfig = route;
            blackout.maxAlpha = 0.55f;
            blackout.fadeSeconds = 0.3f;
            EditorUtility.SetDirty(blackout);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        log.Add("场景已保存：" + ScenePath);
        log.Add("手势拍 " + director.gestureSteps.Length + " 条；三选1 候选 " + director.tutorialChoicePool.Count + " 个（推荐：" + (director.recommendedChoice != null ? director.recommendedChoice.displayName : "-") + "）");

        // 5.5 常驻点击特效（敌人身上的光圈）+ 结束面板按钮上移 + 提示层置顶 + 教学文字横幅
        EnsureTapIndicator(log);
        MoveResultButtonsUp(log);
        RaiseHintCanvas(log);
        RaiseDialogueCanvas(log);
        RemoveTeachingBanner(director, log);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Report(log);
    }

    /// <summary>常驻点击特效：挂在战斗 HUD 的 Canvas 下（提示只画在最前排敌人身上）。</summary>
    private static void EnsureTapIndicator(List<string> log)
    {
        var existing = UnityEngine.Object.FindObjectOfType<AttackTapIndicator>();
        AttackTapIndicator indicator = existing;
        if (indicator == null)
        {
            Canvas hudCanvas = null;
            foreach (var c in UnityEngine.Object.FindObjectsOfType<Canvas>())
            {
                if (c.GetComponent<BattleHUD>() != null) { hudCanvas = c; break; }
            }
            if (hudCanvas == null) { log.Add("找不到战斗 HUD 的 Canvas，常驻点击特效未创建"); return; }

            var go = new GameObject("Attack Tap Indicator");
            go.transform.SetParent(hudCanvas.transform, false);
            go.transform.SetAsFirstSibling();     // 画在 HUD 元素下面
            indicator = go.AddComponent<AttackTapIndicator>();
            log.Add("已创建常驻点击特效（Attack Tap Indicator）");
        }

        // 素材：**直接用教学提示那一套**（手 + 点按帧 + 光圈）
        // （用户反馈「你这常驻的光圈提示怎么跟教学的完全不一样，只有一个圈在那」）
        var hint = UnityEngine.Object.FindObjectOfType<TutorialGestureHint>();
        if (hint == null) { log.Add("找不到 TutorialGestureHint，常驻点击特效没能接上教学素材"); return; }
        var ht = hint.GetType();
        var anyField = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var fingerRect = ht.GetField("finger", anyField)?.GetValue(hint) as RectTransform;
        var ringImg = ht.GetField("ring", anyField)?.GetValue(hint) as UnityEngine.UI.Image;
        var frames = ht.GetField("tapFrames", anyField)?.GetValue(hint) as Sprite[];

        var fingerImg = fingerRect != null ? fingerRect.GetComponent<UnityEngine.UI.Image>() : null;
        indicator.handSprite = fingerImg != null ? fingerImg.sprite : null;
        indicator.ringSprite = ringImg != null ? ringImg.sprite : null;
        indicator.handTapFrames = frames;
        // 尺寸/动画参数每次都对齐（组件是序列化在场景里的，旧值不会跟代码默认值走）
        // 尺寸改成**相对敌人**（光圈直径 = 敌人屏幕高度 × 倍数）→ 正式关里不再过大
        indicator.ringHeightRatio = 0.3f;
        indicator.handToRingRatio = TutorialGestureStyle.BaseHandSize / TutorialGestureStyle.BaseRingSize;
        indicator.fallbackRingSize = TutorialGestureStyle.BaseRingSize;
        indicator.pulseSpeed = 1.6f;
        indicator.pulseAmount = 0.10f;
        indicator.ringSpinSpeed = 16f;
        indicator.tapDuration = 1.0f;
        indicator.tapPressAngle = TutorialGestureStyle.PressAngle;
        indicator.showDuringTeaching = false;
        EditorUtility.SetDirty(indicator);
        log.Add("常驻点击特效已接上教学素材（手=" + (indicator.handSprite != null ? indicator.handSprite.name : "无")
            + " 帧=" + (frames != null ? frames.Length : 0) + " 光圈=" + (indicator.ringSprite != null ? indicator.ringSprite.name : "无") + "）");
    }

    /// <summary>
    /// 把**对话层**提到三选一弹窗之上（弹窗的 Canvas sortingOrder = 100，对话层原本 = 22 → 被盖住）。
    /// 教学要在弹窗上用对话说「选哪个」，所以对话层必须能压在弹窗上面。
    /// </summary>
    private static void RaiseDialogueCanvas(List<string> log)
    {
        var view = UnityEngine.Object.FindObjectOfType<BattleDialogueView>();
        if (view == null) { log.Add("找不到 BattleDialogueView，对话层未置顶"); return; }
        var canvas = view.GetComponentInParent<Canvas>();
        if (canvas == null) { log.Add("BattleDialogueView 不在 Canvas 下，对话层未置顶"); return; }
        if (canvas.sortingOrder < 120)
        {
            canvas.sortingOrder = 120;
            EditorUtility.SetDirty(canvas);
            log.Add("对话层 Canvas 排序提到 120（盖在三选一弹窗之上）");
        }
    }

    /// <summary>
    /// 移除教学文字横幅（2026-11-01 用户反馈：「不是说了3选1的教学文本要用对话吗？」）→
    /// 三选1 的教学文字改回走对话系统。
    /// </summary>
    private static void RemoveTeachingBanner(TutorialStageDirector director, List<string> log)
    {
        if (director == null) return;
        // 组件类型已删（文件移除），用类型名反射找出来再删
        var comps = director.GetComponents<Component>();
        for (int i = 0; i < comps.Length; i++)
        {
            if (comps[i] == null) continue;
            if (comps[i].GetType().Name != "TutorialTeachingBanner") continue;
            UnityEngine.Object.DestroyImmediate(comps[i]);
            log.Add("已移除教学文字横幅（三选1 的教学文字改用对话）");
            return;
        }
    }

    /// <summary>
    /// 结束/暂停面板的按钮上移：**当前「返回」按钮压在底排的防御键上**（防御键占画布 y 490..650），
    /// 点不到返回主菜单。
    /// </summary>
    private static void MoveResultButtonsUp(List<string> log)
    {
        // 胜利面板：返回（anchor y 0.25 = 画布 480）→ 0.40（768）
        var victory = FindChild(UnityEngine.Object.FindObjectOfType<BattleHUD>() != null ? UnityEngine.Object.FindObjectOfType<BattleHUD>().transform : null, "Victory(panel)");
        if (victory != null)
        {
            var btn = victory.transform.Find("MainMenuButton") as RectTransform;
            if (btn != null)
            {
                btn.anchorMin = new Vector2(0.5f, 0.40f);
                btn.anchorMax = new Vector2(0.5f, 0.40f);
                btn.anchoredPosition = new Vector2(0f, 0f);
                EditorUtility.SetDirty(btn.gameObject);
                log.Add("胜利面板「返回」按钮上移到 anchor y 0.40（避开防御键）");
            }
        }

        // 暂停面板：继续游戏 / 返回（局部 -245 / -350 → 画布 725 / 620，压着防御键）→ -140 / -245
        var pause = FindChild(UnityEngine.Object.FindObjectOfType<BattleHUD>() != null ? UnityEngine.Object.FindObjectOfType<BattleHUD>().transform : null, "PausePanel");
        if (pause != null)
        {
            var card = pause.transform.Find("Card");
            if (card != null)
            {
                var cont = card.Find("ContinueButton") as RectTransform;
                if (cont != null) { cont.anchoredPosition = new Vector2(cont.anchoredPosition.x, -140f); EditorUtility.SetDirty(cont.gameObject); }
                var back = card.Find("MainMenuButton") as RectTransform;
                if (back != null) { back.anchoredPosition = new Vector2(back.anchoredPosition.x, -245f); EditorUtility.SetDirty(back.gameObject); }
                log.Add("暂停面板「继续游戏 / 返回」按钮上移（避开防御键）");
            }
        }
    }

    private static Transform FindChild(Transform root, string name)
    {
        if (root == null) return null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    /// <summary>
    /// 提示层（Tutorial Gesture Hint）提到**最上层**：
    /// 三选一弹窗是运行时 new 出来的 Overlay Canvas，会盖在提示层上面 →
    /// 「指着推荐卡」的提示手就看不见了（2026-11-01 实测）。
    /// </summary>
    private static void RaiseHintCanvas(List<string> log)
    {
        var hint = UnityEngine.Object.FindObjectOfType<TutorialGestureHint>();
        if (hint == null) { log.Add("找不到 TutorialGestureHint，提示层未置顶"); return; }
        var canvas = hint.GetComponentInParent<Canvas>();
        if (canvas == null) { log.Add("提示手不在 Canvas 下，无法置顶"); return; }
        if (canvas.sortingOrder < 200)
        {
            canvas.sortingOrder = 200;
            EditorUtility.SetDirty(canvas);
            log.Add("提示层 Canvas 排序提到 200（盖在三选一弹窗之上）");
        }
    }

    /// <summary>
    /// 移除测试用的冻结技能（冰雾）：
    ///   • 玩家开局不再白送（ActiveSkillInventory._grantDefaultSkills / _defaultSkills）；
    ///   • 随机技能池里也不再有它。
    /// 资产本身保留（不删文件），只是不再出现在任何给玩家的入口里。
    /// </summary>
    private static void RemoveTestFreezeSkill(List<string> log)
    {
        // ① 场景里的默认赠送
        var inventory = UnityEngine.Object.FindObjectOfType<ActiveSkillInventory>();
        if (inventory != null)
        {
            var t = typeof(ActiveSkillInventory);
            var grantField = t.GetField("_grantDefaultSkills", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var defaultsField = t.GetField("_defaultSkills", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (grantField != null) grantField.SetValue(inventory, false);
            if (defaultsField != null)
            {
                var list = defaultsField.GetValue(inventory) as List<ActiveSkillDefinition>;
                if (list != null) list.Clear();
            }
            EditorUtility.SetDirty(inventory);
            log.Add("已关闭主动技能默认赠送（冰雾不再开局白送）");
        }

        // ② 随机技能池
        var pool = Load<ActiveSkillPoolConfig>(SkillPoolPath);
        if (pool != null)
        {
            int removed = 0;
            removed += RemoveFromPool(pool.commonPool);
            removed += RemoveFromPool(pool.rarePool);
            removed += RemoveFromPool(pool.legendaryPool);
            if (removed > 0)
            {
                EditorUtility.SetDirty(pool);
                log.Add("随机技能池已移除冰雾（" + removed + " 处）");
            }
        }
    }

    private static int RemoveFromPool(List<WeightedUpgrade> pool)
    {
        if (pool == null) return 0;
        int removed = 0;
        for (int i = pool.Count - 1; i >= 0; i--)
        {
            var up = pool[i];
            if (up == null || up.upgrade == null) continue;
            if (up.upgrade.upgradeId == "active_ice_mist")
            {
                pool.RemoveAt(i);
                removed++;
            }
        }
        return removed;
    }

    /// <summary>建/取黑幕物体（自带一个置顶 Overlay Canvas）。</summary>
    private static TutorialRouteBlackout EnsureBlackout(FakeRouteStageConfig route)
    {
        var existing = UnityEngine.Object.FindObjectOfType<TutorialRouteBlackout>();
        if (existing != null) return existing;

        var go = new GameObject("Tutorial Blackout");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;      // 盖在战斗 HUD 之上
        var blackout = go.AddComponent<TutorialRouteBlackout>();
        blackout.routeConfig = route;
        blackout.tutorialRouteConfig = route;
        return blackout;
    }

    /// <summary>造一波：单个敌人在中间列。</summary>
    private static WaveConfig MakeWave(int waveId, int enemyId, float healthMultiplier, float damageMultiplier, float expMultiplier)
    {
        var row = new RowConfig { enemyIds = new[] { 0, 0, enemyId, 0, 0 } };
        return new WaveConfig
        {
            waveId = waveId,
            isBossWave = false,
            rows = new List<RowConfig> { row },
            healthMultiplier = healthMultiplier,
            bossHealthMultiplier = 0f,
            attackSpeedMultiplier = 1f,
            damageMultiplier = damageMultiplier,
            expMultiplier = expMultiplier,
            waveTintColor = Color.white,
            enableDynamicRush = false,
            rushMoveDelay = 0.2f,
            rushMoveDelayMin = 0.02f,
        };
    }

    /// <summary>建台词资产（已存在则改内容，不重建 → 不丢 GUID 引用）。</summary>
    private static DialogueEventData EnsureDialogue(string assetName, string text, List<string> log)
    {
        string path = DialogueDir + assetName + ".asset";
        var d = AssetDatabase.LoadAssetAtPath<DialogueEventData>(path);
        if (d == null)
        {
            d = ScriptableObject.CreateInstance<DialogueEventData>();
            AssetDatabase.CreateAsset(d, path);
            log.Add("新建台词资产：" + path);
        }
        d.eventId = assetName;
        d.eventType = DialogueEventType.Tutorial;
        d.triggerType = DialogueTriggerType.WaveStart;
        d.stageId = 9000;
        d.nonBlocking = true;                     // 教学台词一律不暂停
        d.lines.Clear();
        d.lines.Add(new DialogueEventData.Line { speaker = DialogueSpeaker.Player, text = text });
        EditorUtility.SetDirty(d);
        return d;
    }

    private static T Load<T>(string path) where T : Object
    {
        var a = AssetDatabase.LoadAssetAtPath<T>(path);
        if (a == null) Debug.LogWarning("[TutorialSetupTool] 找不到资源：" + path);
        return a;
    }

    private static void Report(List<string> log)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[TutorialSetupTool] 教学关配置已应用：");
        for (int i = 0; i < log.Count; i++) sb.AppendLine("  · " + log[i]);
        Debug.Log(sb.ToString());
    }
}
