using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 配置总表的**域定义**：每个域 = 一张 Excel sheet。
///
/// 分类原则（为什么这么切）：
///   1. 按「对比形态」而不是按资产类型切：
///      · 宽表（行=资产，列=字段）→ 需要横向比数值的（敌人 / 技能 / 大招 / 升级标识与文案）
///      · 长表（行=条目，列=字段）→ 变长文本或逐级数值（台词 / 升级逐级）
///   2. 身份列（键 / 资产路径 / 名称）由管线固定加在最前面，回写按「键」匹配，不按行号。
///   3. 只读列给参考（原文、只读参数），可编辑列在 Excel 里标黄底。
///   4. 一个字段一格，一格一个属性路径 → 新增字段只要在 Columns 里加一列。
///
/// 新增一个域：在 BuildAll() 里加一段即可，不需要改管线。
/// </summary>
public static class ConfigTableDomains
{
    public static List<ConfigTableDomain> BuildAll()
    {
        return new List<ConfigTableDomain>
        {
            BuildEnemyNumbers(),
            BuildSkillNumbers(),
            BuildUltimateNumbers(),
            BuildUpgradeDefinitions(),
            BuildUpgradeLevelNumbers(),
            BuildInBattleTutorialTexts(),
            BuildOutOfBattleTutorialTexts(),
            BuildCombatDialogues(),
        };
    }

    // ── 10 敌人数值 ─────────────────────────────────────────────────────────

    private static ConfigTableDomain BuildEnemyNumbers()
    {
        var d = new ConfigTableDomain
        {
            Sheet = "10_敌人数值",
            Title = "敌人基础数值（行=敌人 prefab）。改数值列即可；黄底=可改，灰底=只读参考。"
                    + "注意：移动速度的单位是**秒/排**（走一排要几秒），数值越小走得越快；攻速是次/秒（间隔=1/攻速）。",
            Shape = "wide"
        };
        d.Columns.AddRange(new[]
        {
            "敌人ID", "名称", "生命上限", "攻速(次/秒)", "攻击间隔(秒,只读)", "攻击伤害", "射程(排)",
            "移动速度(秒/排)", "最大架势", "眩晕时长(秒)", "破防硬直(秒)", "金币", "经验", "远程", "Boss",
            "冲锋基础伤害"
        });

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources/EnemyPrefabs" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) continue;
            var enemy = go.GetComponent<Enemy>();
            if (enemy == null) continue;

            var row = new ConfigTableRow
            {
                Key = go.name,
                Label = enemy.enemyName,
                AssetPath = path,
                Component = "Enemy"
            };

            row.Cells.Add(ConfigTablePipeline.Cell(enemy, "Enemy", "enemyId", "敌人ID", false));
            row.Cells.Add(ConfigTablePipeline.Cell(enemy, "Enemy", "enemyName", "名称", true));
            row.Cells.Add(ConfigTablePipeline.Cell(enemy, "Enemy", "maxHealth", "生命上限", true));
            row.Cells.Add(ConfigTablePipeline.Cell(enemy, "Enemy", "attackSpeed", "攻速(次/秒)", true));
            row.Cells.Add(new ConfigTableCell
            {
                Column = "攻击间隔(秒,只读)",
                Component = "Enemy",
                Property = "attackSpeed",
                Editable = false,
                Baseline = enemy.attackSpeed > 0f ? (1f / enemy.attackSpeed).ToString("0.###") : ""
            });
            row.Cells.Add(ConfigTablePipeline.Cell(enemy, "Enemy", "attackDamage", "攻击伤害", true));
            row.Cells.Add(ConfigTablePipeline.Cell(enemy, "Enemy", "attackRange", "射程(排)", true));
            row.Cells.Add(ConfigTablePipeline.Cell(enemy, "Enemy", "moveSpeed", "移动速度(秒/排)", true));
            row.Cells.Add(ConfigTablePipeline.Cell(enemy, "Enemy", "maxPoise", "最大架势", true));
            row.Cells.Add(ConfigTablePipeline.Cell(enemy, "Enemy", "stunDuration", "眩晕时长(秒)", true));
            row.Cells.Add(ConfigTablePipeline.Cell(enemy, "Enemy", "poiseStunDuration", "破防硬直(秒)", true));
            row.Cells.Add(ConfigTablePipeline.Cell(enemy, "Enemy", "coinReward", "金币", true));
            row.Cells.Add(ConfigTablePipeline.Cell(enemy, "Enemy", "expReward", "经验", true));
            row.Cells.Add(ConfigTablePipeline.Cell(enemy, "Enemy", "isRanged", "远程", true));
            row.Cells.Add(ConfigTablePipeline.Cell(enemy, "Enemy", "isBoss", "Boss", true));

            var cavalry = go.GetComponent<CavalryEnemy>();
            if (cavalry != null)
                row.Cells.Add(ConfigTablePipeline.Cell(cavalry, "CavalryEnemy", "chargeDamage", "冲锋基础伤害", true));

            d.Rows.Add(row);
        }
        return d;
    }

    // ── 20 技能数值 ─────────────────────────────────────────────────────────

    private static ConfigTableDomain BuildSkillNumbers()
    {
        var d = new ConfigTableDomain
        {
            Sheet = "20_技能数值",
            Title = "招式数值（行=AttackSkillConfig 资产）。攻击类型/列模式等枚举填英文枚举名。",
            Shape = "wide"
        };
        d.Columns.AddRange(new[]
        {
            "技能ID", "攻击类型", "伤害类型", "伤害", "架势伤害", "范围(排)", "冷却(秒)", "动作时长(秒)",
            "列模式", "重攻击", "打断骑兵冲锋", "挑飞时长(秒)", "破冰推排"
        });

        foreach (string guid in AssetDatabase.FindAssets("t:AttackSkillConfig"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var cfg = AssetDatabase.LoadAssetAtPath<AttackSkillConfig>(path);
            if (cfg == null) continue;

            var row = new ConfigTableRow
            {
                Key = cfg.name,
                Label = cfg.name,
                AssetPath = path,
                Component = ""
            };
            row.Cells.Add(ConfigTablePipeline.Cell(cfg, "", "id", "技能ID", true));
            row.Cells.Add(ConfigTablePipeline.Cell(cfg, "", "attackType", "攻击类型", true));
            row.Cells.Add(ConfigTablePipeline.Cell(cfg, "", "damageType", "伤害类型", true));
            row.Cells.Add(ConfigTablePipeline.Cell(cfg, "", "damage", "伤害", true));
            row.Cells.Add(ConfigTablePipeline.Cell(cfg, "", "poiseDamage", "架势伤害", true));
            row.Cells.Add(ConfigTablePipeline.Cell(cfg, "", "rangeRows", "范围(排)", true));
            row.Cells.Add(ConfigTablePipeline.Cell(cfg, "", "cooldown", "冷却(秒)", true));
            row.Cells.Add(ConfigTablePipeline.Cell(cfg, "", "actionDuration", "动作时长(秒)", true));
            row.Cells.Add(ConfigTablePipeline.Cell(cfg, "", "columnMode", "列模式", true));
            row.Cells.Add(ConfigTablePipeline.Cell(cfg, "", "isHeavy", "重攻击", true));
            row.Cells.Add(ConfigTablePipeline.Cell(cfg, "", "interruptsCavalryCharge", "打断骑兵冲锋", true));
            row.Cells.Add(ConfigTablePipeline.Cell(cfg, "", "launchDuration", "挑飞时长(秒)", true));
            row.Cells.Add(ConfigTablePipeline.Cell(cfg, "", "breakIcePushRows", "破冰推排", true));

            d.Rows.Add(row);
        }
        return d;
    }

    // ── 21 大招数值 ─────────────────────────────────────────────────────────

    private static ConfigTableDomain BuildUltimateNumbers()
    {
        var d = new ConfigTableDomain
        {
            Sheet = "21_大招数值",
            Title = "大招数值（行=UltimateSkillConfig 资产）。",
            Shape = "wide"
        };
        d.Columns.AddRange(new[]
        {
            "技能ID", "冷却(秒)", "能量消耗", "伤害", "伤害类型",
            "狂怒持续(秒)", "狂怒突刺CD(秒)", "狂怒伤害倍率"
        });

        foreach (string guid in AssetDatabase.FindAssets("t:UltimateSkillConfig"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var cfg = AssetDatabase.LoadAssetAtPath<UltimateSkillConfig>(path);
            if (cfg == null) continue;

            var row = new ConfigTableRow
            {
                Key = cfg.name,
                Label = cfg.name,
                AssetPath = path,
                Component = ""
            };
            row.Cells.Add(ConfigTablePipeline.Cell(cfg, "", "id", "技能ID", true));
            row.Cells.Add(ConfigTablePipeline.Cell(cfg, "", "cooldown", "冷却(秒)", true));
            row.Cells.Add(ConfigTablePipeline.Cell(cfg, "", "energyCost", "能量消耗", true));
            row.Cells.Add(ConfigTablePipeline.Cell(cfg, "", "damage", "伤害", true));
            row.Cells.Add(ConfigTablePipeline.Cell(cfg, "", "damageType", "伤害类型", true));
            row.Cells.Add(ConfigTablePipeline.Cell(cfg, "", "berserkDuration", "狂怒持续(秒)", true));
            row.Cells.Add(ConfigTablePipeline.Cell(cfg, "", "berserkStabCooldown", "狂怒突刺CD(秒)", true));
            row.Cells.Add(ConfigTablePipeline.Cell(cfg, "", "berserkDamageMultiplier", "狂怒伤害倍率", true));

            d.Rows.Add(row);
        }
        return d;
    }

    // ── 30 三选一升级（标识 + 文案） ─────────────────────────────────────────

    private static ConfigTableDomain BuildUpgradeDefinitions()
    {
        var d = new ConfigTableDomain
        {
            Sheet = "30_三选一升级",
            Title = "三选一升级的标识与文案（行=UpgradeDefinition，含主动技能）。占位符原样保留，别删。",
            Shape = "wide"
        };
        d.Columns.AddRange(new[]
        {
            "分类", "升级ID", "显示名", "稀有度", "满级", "效果类型",
            "主说明模板", "补充说明模板", "逐级补充说明"
        });

        foreach (var cfg in CollectUpgrades())
        {
            var row = new ConfigTableRow
            {
                Key = string.IsNullOrEmpty(cfg.upgradeId) ? cfg.name : cfg.upgradeId,
                Label = cfg.displayName,
                AssetPath = AssetDatabase.GetAssetPath(cfg),
                Component = ""
            };
            row.Cells.Add(ConfigTablePipeline.Cell(cfg, "", "category", "分类", true));
            row.Cells.Add(ConfigTablePipeline.Cell(cfg, "", "upgradeId", "升级ID", true));
            row.Cells.Add(ConfigTablePipeline.Cell(cfg, "", "displayName", "显示名", true));
            row.Cells.Add(ConfigTablePipeline.Cell(cfg, "", "rarity", "稀有度", true));
            row.Cells.Add(ConfigTablePipeline.Cell(cfg, "", "maxLevel", "满级", true));
            row.Cells.Add(ConfigTablePipeline.Cell(cfg, "", "effectType", "效果类型", true));
            row.Cells.Add(ConfigTablePipeline.Cell(cfg, "", "descriptionTemplate", "主说明模板", true));
            row.Cells.Add(ConfigTablePipeline.Cell(cfg, "", "extraDescriptionTemplate", "补充说明模板", true));
            row.Cells.Add(new ConfigTableCell
            {
                Column = "逐级补充说明",
                Component = "",
                Property = "levelFeatureDescriptions",
                Editable = false,
                Baseline = string.Join(" | ", cfg.levelFeatureDescriptions ?? new List<string>())
            });

            d.Rows.Add(row);
        }
        return d;
    }

    // ── 31 升级逐级数值（长表） ─────────────────────────────────────────────

    private static ConfigTableDomain BuildUpgradeLevelNumbers()
    {
        var d = new ConfigTableDomain
        {
            Sheet = "31_升级逐级数值",
            Title = "数值型升级的逐级参数（行=某升级的某一级）。只列有 numericLevels 的升级。",
            Shape = "long"
        };
        d.Columns.AddRange(new[] { "等级", "浮点值", "整数值", "第二整数值" });

        foreach (var cfg in CollectUpgrades())
        {
            if (cfg.numericLevels == null || cfg.numericLevels.Count == 0) continue;
            string path = AssetDatabase.GetAssetPath(cfg);
            for (int i = 0; i < cfg.numericLevels.Count; i++)
            {
                var lv = cfg.numericLevels[i];
                var row = new ConfigTableRow
                {
                    Key = (string.IsNullOrEmpty(cfg.upgradeId) ? cfg.name : cfg.upgradeId) + "@Lv" + (i + 1),
                    Label = (string.IsNullOrEmpty(cfg.displayName) ? cfg.name : cfg.displayName) + " Lv" + (i + 1),
                    AssetPath = path,
                    Component = ""
                };
                string p = "numericLevels.Array.data[" + i + "].";
                row.Cells.Add(new ConfigTableCell
                {
                    Column = "等级",
                    Component = "",
                    Property = p + "floatValue",
                    Editable = false,
                    Baseline = (i + 1).ToString()
                });
                row.Cells.Add(ConfigTablePipeline.Cell(cfg, "", p + "floatValue", "浮点值", true));
                row.Cells.Add(ConfigTablePipeline.Cell(cfg, "", p + "intValue", "整数值", true));
                row.Cells.Add(ConfigTablePipeline.Cell(cfg, "", p + "secondaryIntValue", "第二整数值", true));
                d.Rows.Add(row);
            }
        }
        return d;
    }

    // ── 90 台词文案（长表） ─────────────────────────────────────────────────
    // ── 台词域（按**局内 / 局外**分，局外 = 主菜单·落地页·天赋页） ───────────

    /// <summary>文案归属：局内（关卡内）/ 局外（主菜单·落地页·天赋页）/ 战斗剧情台词。</summary>
    private enum TextScope { InBattle, OutBattle, Combat }

    private static ConfigTableDomain BuildInBattleTutorialTexts()
    {
        var d = NewDialogueDomain("90_局内教学文案",
            "关卡内的教学文案：教学关每一拍的台词、事件表里的教学台词、以及战斗场景里的引导按钮/等级球/三选一提示。"
            + "只改「新文案」列；留空=不改。「部署状态」= 在用 / 仅被引用 / 未部署。");
        CollectDialogueRows(d, TextScope.InBattle);
        CollectSceneTextRows(d, TextScope.InBattle);
        return d;
    }

    private static ConfigTableDomain BuildOutOfBattleTutorialTexts()
    {
        var d = NewDialogueDomain("91_局外教学文案",
            "局外（主菜单/落地页/天赋页）的教学文案：教你解锁新招式的引导、落地页引导、天赋页提示。"
            + "只改「新文案」列；留空=不改。");
        CollectDialogueRows(d, TextScope.OutBattle);
        CollectSceneTextRows(d, TextScope.OutBattle);
        return d;
    }

    private static ConfigTableDomain BuildCombatDialogues()
    {
        var d = NewDialogueDomain("92_战斗与剧情台词",
            "非教学的局内台词（Boss 登场/剧情对话等）。只改「新文案」列；留空=不改。");
        CollectDialogueRows(d, TextScope.Combat);
        return d;
    }

    private static ConfigTableDomain NewDialogueDomain(string sheet, string title)
    {
        var d = new ConfigTableDomain { Sheet = sheet, Title = title, Shape = "long" };
        d.Columns.AddRange(new[] { "事件ID", "说话人", "部署状态", "引用位置", "触发方式", "关卡/波次", "所在对象", "组件", "字段", "原文", "新文案" });
        return d;
    }

    /// <summary>
    /// 把台词资产按归属域填进表：
    ///   局外 = 被天赋树的「招式解锁引导」引用（教你解锁新招式）
    ///   局内 = 教学导演的拍/槽，或事件类型是 Tutorial
    ///   战斗/剧情 = 其余（Boss 登场、剧情对话）
    /// </summary>
    private static void CollectDialogueRows(ConfigTableDomain d, TextScope scope)
    {
        var usage = CollectDirectorUsages();      // 教学导演（局内）
        var talentUse = CollectTalentUsages();    // 天赋树招式引导（局外）
        var refs = CollectDialogueRefSlots();      // 谁引用了它（精确到字段）
        var tutorialWaves = CollectTutorialStageWaves(); // 教学关 stageId → 波次总数

        foreach (string guid in AssetDatabase.FindAssets("t:DialogueEventData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var cfg = AssetDatabase.LoadAssetAtPath<DialogueEventData>(path);
            if (cfg == null || cfg.lines == null) continue;

            TextScope belong;
            if (talentUse.ContainsKey(path)) belong = TextScope.OutBattle;
            else if (usage.ContainsKey(path)) belong = TextScope.InBattle;
            else if (cfg.eventType == DialogueEventType.Tutorial) belong = TextScope.InBattle;
            else belong = TextScope.Combat;
            if (belong != scope) continue;

            // 教学关只保留**真的配到当前教学流程**的内容：
            //   ① 被教学导演的拍/槽引用；或
            //   ② 事件类型是 Tutorial，且关卡属于教学关、触发方式会自动触发、波次号在该关卡范围内。
            // 其余（别的关卡的、Manual 手动的、波次对不上的、没人引用的）一律不进表。
            bool configured = usage.ContainsKey(path) || talentUse.ContainsKey(path);
            if (!configured && scope == TextScope.InBattle)
            {
                configured = cfg.eventType == DialogueEventType.Tutorial
                             && cfg.triggerType != DialogueTriggerType.Manual
                             && tutorialWaves.TryGetValue(cfg.stageId, out int waveCount)
                             && cfg.waveNumber >= 1 && cfg.waveNumber <= waveCount;
            }
            if (!configured && scope != TextScope.Combat) continue;

            string status, where;
            if (usage.TryGetValue(path, out var places)) { status = "在用"; where = string.Join(" ; ", places); }
            else if (talentUse.TryGetValue(path, out var tplaces)) { status = "在用"; where = string.Join(" ; ", tplaces); }
            else if (refs.TryGetValue(path, out var slots))
            {
                status = "仅被引用";
                where = string.Join(" ; ", slots.Count > 3 ? slots.GetRange(0, 3).ToArray() : slots.ToArray())
                       + (slots.Count > 3 ? " …共 " + slots.Count + " 处" : "");
            }
            else { status = "未部署"; where = "（没有任何对象/资产引用它）"; }

            for (int i = 0; i < cfg.lines.Count; i++)
            {
                var line = cfg.lines[i];
                var row = new ConfigTableRow
                {
                    Key = (string.IsNullOrEmpty(cfg.eventId) ? cfg.name : cfg.eventId) + "#L" + i,
                    Label = cfg.name + " #" + i,
                    AssetPath = path,
                    Component = ""
                };
                string p = "lines.Array.data[" + i + "].";
                row.Cells.Add(new ConfigTableCell { Column = "事件ID", Component = "", Property = "eventId", Editable = false, Baseline = cfg.eventId });
                row.Cells.Add(new ConfigTableCell { Column = "说话人", Component = "", Property = p + "speaker", Editable = false, Baseline = line.speaker.ToString() });
                row.Cells.Add(new ConfigTableCell { Column = "部署状态", Component = "", Property = "eventId", Editable = false, Baseline = status });
                row.Cells.Add(new ConfigTableCell { Column = "引用位置", Component = "", Property = "eventId", Editable = false, Baseline = where });
                row.Cells.Add(new ConfigTableCell { Column = "触发方式", Component = "", Property = "triggerType", Editable = false, Baseline = cfg.triggerType + " / " + cfg.eventType });
                string sc = "关卡" + cfg.stageId + " 波次" + cfg.waveNumber;
                if (cfg.killCount > 0) sc += " 击杀数" + cfg.killCount;
                if (cfg.bossId > 0) sc += " Boss" + cfg.bossId + (cfg.bossPhaseIndex > 0 ? "/阶段" + cfg.bossPhaseIndex : "");
                if (cfg.bossProfile != null) sc += " BOSS档案:" + cfg.bossProfile.name;
                row.Cells.Add(new ConfigTableCell { Column = "关卡/波次", Component = "", Property = "stageId", Editable = false, Baseline = sc });
                row.Cells.Add(new ConfigTableCell { Column = "原文", Component = "", Property = p + "text", Editable = false, Baseline = line.text });
                row.Cells.Add(new ConfigTableCell { Column = "新文案", Component = "", Property = p + "text", Editable = true, Baseline = line.text });
                d.Rows.Add(row);
            }
        }
    }

    /// <summary>教学关的 stageId → 该关的波次总数（从 RouteData/Tutorial 下的 StageConfig 资产读）。</summary>
    private static Dictionary<int, int> CollectTutorialStageWaves()
    {
        var map = new Dictionary<int, int>();
        foreach (string g in AssetDatabase.FindAssets("", new[] { "Assets/RouteData/Tutorial" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            var cfg = AssetDatabase.LoadAssetAtPath<StageConfig>(path);
            if (cfg == null || cfg.waves == null) continue;
            int waveCount = 0;
            foreach (var w in cfg.waves) waveCount = Mathf.Max(waveCount, w.waveId + 1);
            if (map.TryGetValue(cfg.stageId, out int old) && old >= waveCount) continue;
            map[cfg.stageId] = waveCount;
        }
        return map;
    }

    /// <summary>教学导演（教学关）的台词槽位 → 局内。</summary>
    private static Dictionary<string, List<string>> CollectDirectorUsages()
    {
        var usage = new Dictionary<string, List<string>>();
        Action<UnityEngine.Object, string> mark = (obj, where) =>
        {
            if (obj == null) return;
            string path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path)) return;
            if (!usage.TryGetValue(path, out var list)) { list = new List<string>(); usage[path] = list; }
            if (!list.Contains(where)) list.Add(where);
        };
        var dir = UnityEngine.Object.FindObjectOfType<TutorialStageDirector>();
        if (dir != null)
        {
            for (int i = 0; i < dir.n1Dialogues.Length; i++) mark(dir.n1Dialogues[i], "教学关/N1/波次" + (i + 1) + "/波次台词");
            for (int i = 0; i < dir.e0Dialogues.Length; i++) mark(dir.e0Dialogues[i], "教学关/E0/波次" + (i + 1) + "/波次台词");
            for (int i = 0; i < dir.n1IntroDialogues.Length; i++) mark(dir.n1IntroDialogues[i], "教学关/N1/波次" + (i + 1) + "/开场第一段");
            for (int i = 0; i < dir.e0IntroDialogues.Length; i++) mark(dir.e0IntroDialogues[i], "教学关/E0/波次" + (i + 1) + "/开场第一段");
            mark(dir.openingDialogue, "教学关/开幕台词");
            mark(dir.choiceDialogue, "教学关/路线出口引导");
            mark(dir.choiceTeachingDialogue, "教学关/三选一教学");
            mark(dir.skillDialogueCyclone, "教学关/技能教学(旋风)");
            mark(dir.skillDialogueWave, "教学关/技能教学(海浪)");
            mark(dir.skillDialogueGeneric, "教学关/技能教学(其它)");
            for (int k = 0; k < dir.gestureSteps.Length; k++)
            {
                var st = dir.gestureSteps[k];
                if (st == null) continue;
                string node = st.battle != null ? st.battle.name : "-";
                string beat = "拍" + (k + 1) + "·" + st.kind;
                mark(st.beforeDialogue, "教学关/" + node + "/" + beat + "/拍前");
                mark(st.afterDialogue, "教学关/" + node + "/" + beat + "/收拍");
                mark(st.freezeDialogue, "教学关/" + node + "/" + beat + "/冻结");
                mark(st.comboPhase2Dialogue, "教学关/" + node + "/" + beat + "/连招阶段2");
            }
        }
        return usage;
    }

    /// <summary>天赋树的「招式解锁引导」→ 局外（教你解锁新招式）。</summary>
    private static Dictionary<string, List<string>> CollectTalentUsages()
    {
        var usage = new Dictionary<string, List<string>>();
        Action<UnityEngine.Object, string> mark = (obj, where) =>
        {
            if (obj == null) return;
            string path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path)) return;
            if (!usage.TryGetValue(path, out var list)) { list = new List<string>(); usage[path] = list; }
            if (!list.Contains(where)) list.Add(where);
        };
        foreach (string g in AssetDatabase.FindAssets("t:TalentTreeConfig"))
        {
            var tree = AssetDatabase.LoadAssetAtPath<TalentTreeConfig>(AssetDatabase.GUIDToAssetPath(g));
            if (tree == null || tree.nodes == null) continue;
            foreach (var n in tree.nodes)
            {
                if (n == null) continue;
                mark(n.moveGuideDialogue, "天赋页/" + n.displayName + "/" + n.nodeId + "/解锁引导");
                mark(n.moveGuidePhase2Dialogue, "天赋页/" + n.displayName + "/" + n.nodeId + "/解锁引导·阶段2");
                mark(n.moveGuidePhase3Dialogue, "天赋页/" + n.displayName + "/" + n.nodeId + "/解锁引导·阶段3");
            }
        }
        return usage;
    }

    /// <summary>反向引用扫描：谁引用了这些台词资产（精确到对象+字段）。</summary>
    private static Dictionary<string, List<string>> CollectDialogueRefSlots()
    {
        var dialoguePaths = new HashSet<string>();
        foreach (string g in AssetDatabase.FindAssets("t:DialogueEventData"))
            dialoguePaths.Add(AssetDatabase.GUIDToAssetPath(g));
        var refs = new Dictionary<string, List<string>>();
        System.Action<UnityEngine.Object, string> scan = (host, label) =>
        {
            if (host == null) return;
            var so = new UnityEditor.SerializedObject(host);
            var it = so.GetIterator();
            bool enter = true;
            while (it.NextVisible(enter))
            {
                enter = true;
                if (it.propertyType != UnityEditor.SerializedPropertyType.ObjectReference) continue;
                var v = it.objectReferenceValue;
                if (v == null) continue;
                string p = AssetDatabase.GetAssetPath(v);
                if (string.IsNullOrEmpty(p) || !dialoguePaths.Contains(p)) continue;
                if (!refs.TryGetValue(p, out var list)) { list = new List<string>(); refs[p] = list; }
                string entry = label + " / " + it.propertyPath;
                if (!list.Contains(entry)) list.Add(entry);
            }
        };
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
            foreach (var comp in root.GetComponentsInChildren<Component>(true))
                if (comp != null) scan(comp, System.IO.Path.GetFileName(scene.path) + " / " + HierarchyPath(comp.transform) + " / " + comp.GetType().Name);
        foreach (string root in new[] { "Assets/RouteData", "Assets/ScriptableObjects", "Assets/Prefabs" })
            foreach (string g in AssetDatabase.FindAssets("", new[] { root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                if (path.EndsWith(".cs") || path.EndsWith(".meta")) continue;
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (obj != null) scan(obj, path);
            }
        return refs;
    }

    // ── 场景组件里的内联文案（局内/局外） ──────────────────────────────

    /// <summary>
    /// 扫场景里"引导/教学"类组件的字符串字段。
    /// 局外 = 落地页/天赋页组件（HubTutorialGuide / TalentTreePanel…）；其余归局内。
    /// </summary>
    private static void CollectSceneTextRows(ConfigTableDomain d, TextScope scope)
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        CollectSceneTextRowsOf(scene, scene.path, d, scope);

        // 天赋页在另一个场景（可能没加载）
        const string talentScene = "Assets/Scenes/TalentTree.scene";
        if (!System.IO.File.Exists(talentScene)) return;

        // 已经开着就直接用（**不能关**：上面可能带着用户未保存的改动）
        var loaded = FindLoadedScene(talentScene);
        if (loaded.IsValid())
        {
            CollectSceneTextRowsOf(loaded, talentScene, d, scope);
            return;
        }

        var opened = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
            talentScene, UnityEditor.SceneManagement.OpenSceneMode.Additive);
        var prevActive = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (prevActive.IsValid() && prevActive.isLoaded)
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(prevActive);
        try { CollectSceneTextRowsOf(opened, talentScene, d, scope); }
        finally
        {
            // 只关自己开的、且没被改脏的
            if (!opened.isDirty) UnityEditor.SceneManagement.EditorSceneManager.CloseScene(opened, true);
        }
    }

    /// <summary>找已加载的指定路径场景。</summary>
    private static UnityEngine.SceneManagement.Scene FindLoadedScene(string path)
    {
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            var s = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            if (s.IsValid() && s.isLoaded && s.path == path) return s;
        }
        return default;
    }

    private static void CollectSceneTextRowsOf(UnityEngine.SceneManagement.Scene scene, string scenePath,
                                               ConfigTableDomain d, TextScope scope)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var comp in root.GetComponentsInChildren<Component>(true))
            {
                if (comp == null) continue;
                string typeName = comp.GetType().Name;
                if (!typeName.Contains("Guide") && !typeName.Contains("Tutorial") && !typeName.Contains("Hint")
                    && !typeName.Contains("Talent")) continue;

                bool outBattle = typeName.Contains("Hub") || typeName.Contains("Talent") || scenePath.Contains("TalentTree");
                if ((outBattle ? TextScope.OutBattle : TextScope.InBattle) != scope) continue;

                string objPath = HierarchyPath(comp.transform);
                var so = new UnityEditor.SerializedObject(comp);
                var it = so.GetIterator();
                bool enter = true;
                while (it.NextVisible(enter))
                {
                    enter = true;
                    if (it.propertyType != UnityEditor.SerializedPropertyType.String) continue;
                    string value = it.stringValue;
                    if (string.IsNullOrEmpty(value)) continue;
                    // 跳过"按名字引用场景/资源"的字段（不是文案）
                    if (it.propertyPath.EndsWith("Scene") || it.propertyPath.EndsWith("Asset")
                        || it.propertyPath.EndsWith("Prefab") || it.propertyPath.EndsWith("Name")) continue;

                    var row = new ConfigTableRow
                    {
                        Key = scenePath + "|" + objPath + "|" + typeName + "|" + it.propertyPath,
                        Label = objPath + " · " + it.propertyPath,
                        AssetPath = scenePath,
                        Component = typeName
                    };
                    row.Cells.Add(new ConfigTableCell { Column = "所在对象", Component = typeName, Property = it.propertyPath, Editable = false, Baseline = objPath, ObjectPath = objPath });
                    row.Cells.Add(new ConfigTableCell { Column = "组件", Component = typeName, Property = it.propertyPath, Editable = false, Baseline = typeName, ObjectPath = objPath });
                    row.Cells.Add(new ConfigTableCell { Column = "字段", Component = typeName, Property = it.propertyPath, Editable = false, Baseline = it.propertyPath, ObjectPath = objPath });
                    row.Cells.Add(new ConfigTableCell { Column = "部署状态", Component = typeName, Property = it.propertyPath, Editable = false, Baseline = "在用", ObjectPath = objPath });
                    row.Cells.Add(new ConfigTableCell { Column = "触发方式", Component = typeName, Property = it.propertyPath, Editable = false, Baseline = "场景内联", ObjectPath = objPath });
                    row.Cells.Add(new ConfigTableCell { Column = "引用位置", Component = typeName, Property = it.propertyPath, Editable = false, Baseline = System.IO.Path.GetFileName(scenePath) + " / " + objPath, ObjectPath = objPath });
                    row.Cells.Add(new ConfigTableCell { Column = "原文", Component = typeName, Property = it.propertyPath, Editable = false, Baseline = value, ObjectPath = objPath });
                    row.Cells.Add(new ConfigTableCell { Column = "新文案", Component = typeName, Property = it.propertyPath, Editable = true, Baseline = value, ObjectPath = objPath });
                    d.Rows.Add(row);
                }
            }
        }
    }

    /// <summary>场景里对象的层级路径（根/子/孙），用作稳定定位。</summary>
    private static string HierarchyPath(Transform t)
    {
        var parts = new List<string>();
        var cur = t;
        while (cur != null) { parts.Insert(0, cur.name); cur = cur.parent; }
        return string.Join("/", parts);
    }

    // ── 工具 ────────────────────────────────────────────────────────────────

    private static List<UpgradeDefinition> CollectUpgrades()
    {
        var result = new List<UpgradeDefinition>();
        var seen = new HashSet<string>();

        // t: 过滤器对派生类型不可靠 → 基础类型与派生类型都扫一遍，再用 is 过滤
        foreach (string filter in new[] { "t:UpgradeDefinition", "t:ActiveSkillDefinition" })
        {
            foreach (string guid in AssetDatabase.FindAssets(filter))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!seen.Add(path)) continue;
                var cfg = AssetDatabase.LoadAssetAtPath<UpgradeDefinition>(path);
                if (cfg != null) result.Add(cfg);
            }
        }
        return result.OrderBy(c => c.category.ToString()).ThenBy(c => c.upgradeId).ToList();
    }
}
