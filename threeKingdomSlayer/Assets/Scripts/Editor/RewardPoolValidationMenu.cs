using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// **奖励池配置校验**（2026-11-25）——不进 Play Mode、不改任何资产，直接跑一遍抽取器的配置检查。
///
/// 为什么要有：装备池按内部圈数分段，运行时只会校验当前圈命中的那一段；
/// 这里对**每个阶段的起始圈**各抽一次，把「某段配了概率但没货」之类的问题一次性报出来。
///
/// ⚠ 只读取配置与解锁存档；**不写档、不改资产、不发奖励**。
/// </summary>
public static class RewardPoolValidationMenu
{
    [MenuItem("一夫当关/校验奖励池配置（无尽 demo）")]
    public static void Validate()
    {
        var ucm = Object.FindObjectOfType<UpgradeChoiceManager>(true);
        if (ucm == null)
        {
            EditorUtility.DisplayDialog("奖励池校验", "当前打开的场景里找不到 UpgradeChoiceManager。\n请先打开 Battle 场景。", "好");
            return;
        }

        var sb = new StringBuilder();
        int errors = 0;
        int warnings = 0;
        int count = Mathf.Max(1, ucm.choiceCount);

        // ── 属性池（全局品质概率，与圈数无关）──
        var attrReport = new RewardDrawReport();
        var attrDrawer = new AttributeChoiceDrawer(
            ucm.attributePoolConfig,
            _ => 0,                                   // 配置校验按“全部未满级”看
            def => Mathf.Max(1, def != null ? def.maxLevel : 1),
            _ => true,                                // 前置是玩法状态，不算配置错误
            _ => true);
        attrDrawer.Draw(count, attrReport);
        errors += attrReport.errors.Count;
        warnings += attrReport.warnings.Count;
        sb.AppendLine(attrReport.Describe("属性池"));

        // ── 装备池：每个阶段的起始圈各校验一次 ──
        var equip = ucm.equipmentPoolConfig;
        if (equip == null)
        {
            errors++;
            sb.AppendLine("[装备池] 未配置（UpgradeChoiceManager.equipmentPoolConfig 为空）");
        }
        else
        {
            var circles = new List<int>();
            if (equip.stages != null)
            {
                for (int i = 0; i < equip.stages.Count; i++)
                {
                    var stage = equip.stages[i];
                    if (stage == null) continue;
                    int circle = Mathf.Max(1, stage.minCircle);
                    if (!circles.Contains(circle)) circles.Add(circle);
                }
            }
            if (circles.Count == 0) circles.Add(1);

            for (int i = 0; i < circles.Count; i++)
            {
                var report = new RewardDrawReport();
                var drawer = new EquipmentChoiceDrawer(equip, EquipmentUnlockService.IsUnlocked);
                drawer.Draw(circles[i], count, report);
                errors += report.errors.Count;
                warnings += report.warnings.Count;
                sb.AppendLine(report.Describe("装备池 / 内部圈数 " + circles[i]));
            }
        }

        string header = "奖励池校验：错误 " + errors + " 条，提醒 " + warnings + " 条。\n";
        if (errors > 0) Debug.LogError(header + sb);
        else Debug.Log(header + sb);

        EditorUtility.DisplayDialog("奖励池校验",
            errors > 0
                ? "发现 " + errors + " 条配置错误，详见 Console。"
                : (warnings > 0 ? "没有配置错误，有 " + warnings + " 条提醒，详见 Console。" : "未发现配置错误。"),
            "好");
    }
}
