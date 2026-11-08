using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// **导出教学字幕总表的数据源**（2026-11-07）。
///
/// 产出：<c>Library/Locus/tmp/tutorial_subtitles_dump.json</c>（台词资产 + 每行的说话人/文本
/// + 每个资产在教学链里的**引用位置**（节点/波次/拍/触发时机）+ 拍结构）。
/// 再由 <c>Tools/build_tutorial_subtitle_table.py</c> 生成
/// <c>Locus/knowledge/plan/tutorial-subtitles-20261107.xlsx</c>（用户在那里改文案）。
///
/// 用法：Tools ▸ 三国杀戮 ▸ 教学关 ▸ 导出教学字幕 → 跑 python Tools/build_tutorial_subtitle_table.py
/// </summary>
public static class TutorialSubtitleExport
{
    private const string OutPath = "Library/Locus/tmp/tutorial_subtitles_dump.json";
    private const string BeatsOutPath = "Library/Locus/tmp/tutorial_beats_dump.json";

    [MenuItem("Tools/三国杀戮/教学关/导出教学字幕")]
    public static void Export()
    {
        var sb = new StringBuilder();
        sb.Append("{\"dialogues\":[");
        bool first = true;
        foreach (var guid in AssetDatabase.FindAssets("t:DialogueEventData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var d = AssetDatabase.LoadAssetAtPath<DialogueEventData>(path);
            if (d == null) continue;
            if (!first) sb.Append(",");
            first = false;
            sb.Append("{\"path\":").Append(Q(path)).Append(",\"name\":").Append(Q(d.name))
              .Append(",\"eventId\":").Append(Q(d.eventId)).Append(",\"nonBlocking\":").Append(d.nonBlocking ? "true" : "false")
              .Append(",\"lines\":[");
            for (int i = 0; i < d.lines.Count; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append("{\"speaker\":").Append(Q(d.lines[i].speaker.ToString()))
                  .Append(",\"text\":").Append(Q(d.lines[i].text)).Append("}");
            }
            sb.Append("]}");
        }
        sb.Append("],\"usages\":[");

        bool firstU = true;
        Action<string, string, string, string, string, string, string> U = (asset, area, node, wave, beat, trigger, note) =>
        {
            if (string.IsNullOrEmpty(asset)) return;
            if (!firstU) sb.Append(",");
            firstU = false;
            sb.Append("{\"asset\":").Append(Q(asset)).Append(",\"area\":").Append(Q(area))
              .Append(",\"node\":").Append(Q(node)).Append(",\"wave\":").Append(Q(wave))
              .Append(",\"beat\":").Append(Q(beat)).Append(",\"trigger\":").Append(Q(trigger))
              .Append(",\"note\":").Append(Q(note)).Append("}");
        };

        var dir = UnityEngine.Object.FindObjectOfType<TutorialStageDirector>();
        if (dir != null)
        {
            for (int i = 0; i < dir.n1Dialogues.Length; i++) U(N(dir.n1Dialogues[i]), "教学关", "N1", "波次 " + (i + 1), "波次台词", "开波时", "");
            for (int i = 0; i < dir.e0Dialogues.Length; i++) U(N(dir.e0Dialogues[i]), "教学关", "E0", "波次 " + (i + 1), "波次台词", "开波时", "");
            for (int i = 0; i < dir.n1IntroDialogues.Length; i++) U(N(dir.n1IntroDialogues[i]), "教学关", "N1", "波次 " + (i + 1), "开场第一段字幕", "敌人就位后（点「点击继续」前）", "");
            for (int i = 0; i < dir.e0IntroDialogues.Length; i++) U(N(dir.e0IntroDialogues[i]), "教学关", "E0", "波次 " + (i + 1), "开场第一段字幕", "敌人就位后（点「点击继续」前）", "");
            U(N(dir.openingDialogue), "教学关", "N1", "开场", "开幕台词", "进入教学路线时", "");
            U(N(dir.choiceDialogue), "教学关", "路线出口", "-", "路线引导台词", "路线出现出口箭头时", "");
            U(N(dir.choiceTeachingDialogue), "教学关", "E0", "三选1", "三选一教学台词", "三选一弹出时", "");
            U(N(dir.skillDialogueCyclone), "教学关", "E0", "内容2", "技能教学台词（旋风）", "选完技能后", "");
            U(N(dir.skillDialogueWave), "教学关", "E0", "内容2", "技能教学台词（海浪）", "选完技能后", "");
            U(N(dir.skillDialogueGeneric), "教学关", "E0", "内容2", "技能教学台词（其它技能）", "选完技能后", "兼底");
            for (int k = 0; k < dir.gestureSteps.Length; k++)
            {
                var st = dir.gestureSteps[k];
                if (st == null) continue;
                string node = st.battle != null ? st.battle.name : "-";
                string wave = st.waveIndex == -2 ? "路线出口" : (st.waveIndex >= 0 ? "波次 " + (st.waveIndex + 1) : "波次 " + st.waveIndex);
                string beat = "拍 " + (k + 1) + " · " + st.kind;
                U(N(st.beforeDialogue), "教学关", node, wave, beat, "拍开始（与手势示范同时）", "");
                U(N(st.afterDialogue), "教学关", node, wave, beat, "收拍后补播", "");
                U(N(st.freezeDialogue), "教学关", node, wave, beat, "冻结瞬间", "");
                U(N(st.comboPhase2Dialogue), "教学关", node, wave, beat, "连招蓄力 阶段2", "");
            }
        }

        // 天赋树里的「招式解锁引导」（正式关）
        foreach (var g in AssetDatabase.FindAssets("t:TalentTreeConfig"))
        {
            var tree = AssetDatabase.LoadAssetAtPath<TalentTreeConfig>(AssetDatabase.GUIDToAssetPath(g));
            if (tree == null || tree.nodes == null) continue;
            foreach (var n in tree.nodes)
            {
                if (n == null) continue;
                if (n.moveGuideDialogue != null) U(n.moveGuideDialogue.name, "正式关", "招式解锁引导", "-", n.displayName + "（" + n.nodeId + "）", "解锁后首次进关", "");
                if (n.moveGuidePhase2Dialogue != null) U(n.moveGuidePhase2Dialogue.name, "正式关", "招式解锁引导", "-", n.displayName + "（" + n.nodeId + "）", "连招蓄力 阶段2", "");
                if (n.moveGuidePhase3Dialogue != null) U(n.moveGuidePhase3Dialogue.name, "正式关", "招式解锁引导", "-", n.displayName + "（" + n.nodeId + "）", "连招蓄力 阶段3（瞄准）", "");
            }
        }
        sb.Append("],");

        // 组件上的字符串字段（不是独立资产）
        sb.Append("\"inline\":[");
        bool firstI = true;
        Action<string, string, string, string, string> I2 = (id, area, node, trigger, currentText) =>
        {
            if (!firstI) sb.Append(",");
            firstI = false;
            sb.Append("{\"id\":").Append(Q(id)).Append(",\"area\":").Append(Q(area)).Append(",\"node\":").Append(Q(node))
              .Append(",\"trigger\":").Append(Q(trigger)).Append(",\"text\":").Append(Q(currentText)).Append("}");
        };
        if (dir != null)
        {
            I2("director.choiceExpLine", "教学关", "E0", "三选一弹出时（第一句）", dir.choiceExpLine);
            I2("director.choicePickLine", "教学关", "E0", "三选一弹出时（第二句，{0}=技能名）", dir.choicePickLine);
        }
        var hub = UnityEngine.Object.FindObjectOfType<HubTutorialGuide>();
        if (hub != null)
        {
            I2("hubGuide.talentText", "落地页", "天赋入口", "教学完成后 · 落地页引导", hub.talentText);
            I2("hubGuide.stageText", "落地页", "开始关卡", "天赋教学完成后 · 落地页引导", hub.stageText);
        }
        var orbGuide = UnityEngine.Object.FindObjectOfType<LevelOrbGuide>();
        if (orbGuide != null)
        {
            I2("levelOrbGuide.guideText", "教学关", "等级球", "教学关开场 · 等级球教学引导", orbGuide.guideText);
        }
        sb.Append("]}");

        Directory.CreateDirectory(Path.GetDirectoryName(OutPath));
        File.WriteAllText(OutPath, sb.ToString(), new UTF8Encoding(false));
        Debug.Log($"[教学字幕] 已导出 {OutPath}（{sb.Length} 字符）→ 接着跑 python Tools/build_tutorial_subtitle_table.py");

        // 拍结构（教学拍清单那张表）
        if (dir != null)
        {
            var bs = new StringBuilder("[");
            for (int k = 0; k < dir.gestureSteps.Length; k++)
            {
                var st = dir.gestureSteps[k];
                if (st == null) continue;
                if (bs.Length > 1) bs.Append(",");
                bs.Append("{\"i\":").Append(k)
                  .Append(",\"battle\":").Append(Q(st.battle != null ? st.battle.name : "-"))
                  .Append(",\"waveIndex\":").Append(st.waveIndex)
                  .Append(",\"kind\":").Append(Q(st.kind.ToString()))
                  .Append(",\"requiredHits\":").Append(st.requiredHits).Append("}");
            }
            bs.Append("]");
            File.WriteAllText(BeatsOutPath, bs.ToString(), new UTF8Encoding(false));
            Debug.Log($"[教学字幕] 已导出 {BeatsOutPath}");
        }
    }

    private static string N(DialogueEventData d) => d != null ? d.name : "";

    private static string Q(string s)
    {
        s = s ?? "";
        var o = new StringBuilder("\"");
        foreach (char c in s)
        {
            if (c == '"') o.Append("\\\"");
            else if (c == '\\') o.Append("\\\\");
            else if (c == '\n') o.Append("\\n");
            else if (c == '\r') o.Append("\\r");
            else if (c == '\t') o.Append("\\t");
            else if (c < 0x20) o.Append("\\u").Append(((int)c).ToString("x4"));
            else o.Append(c);
        }
        o.Append("\"");
        return o.ToString();
    }
}
