using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// **引导期可交互元素审计**（2026-11-06）。
///
/// 干什么：在 **Play Mode** 里枚举「此刻玩家真的能点到的东西」，并逐个判定
/// 「点了会发生什么 / 该不该发生」。
///
/// 为什么要它：教学的每一拍都要求玩家**只能做这一件事**。但教学 UI 是普通 uGUI 按钮，
/// 它们**不受 <see cref="GameplayInputGate"/> 约束**（那个门只管手势输入与技能释放）。
/// → 只要按钮还亮着、还吃射线，玩家就能在引导期间点到别的功能（喝药、打开别的面板…）。
/// 这类洞肉眼很难发现，靠人工点一遍也点不全 → 固化成脚本。
///
/// 用法：进 Play Mode → 停在「引导进行中」→ 菜单
///   Tools ▸ 三国杀戮 ▸ 教学关 ▸ 引导期可交互元素审计
/// 报告同时写到 <c>Library/Locus/tmp/tutorial-interaction-audit.txt</c>。
///
/// 判定口径：
///   • 「真能点到」= 在该元素的屏幕中心做一次 EventSystem 射线，**首命中就是它自己**
///     （这一条比"按钮亮着"准：还要过 raycastTarget、canvas 上有没有 GraphicRaycaster、有没有别的东西盖在上面）。
///   • 结局分三类：**允许**（设计如此）／**已拦**（点了没反应）／**洞**（点了会真的生效，但此刻不该）。
/// </summary>
public static class TutorialInteractionAudit
{
    private const string ReportPath = "Library/Locus/tmp/tutorial-interaction-audit.txt";

    /// <summary>设计允许在引导期点的东西（玩家必须能用的）。</summary>
    private static readonly HashSet<string> AllowedByDesign = new HashSet<string>
    {
        "PauseButton",          // 左上角暂停键（用户明确要求：只有它能按）
        "TutorialContinueButton" // 开场流程的「点击继续」
    };

    [MenuItem("Tools/三国杀戮/教学关/引导期可交互元素审计")]
    public static void RunInPlayMode()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("[引导审计] 需要在 Play Mode 里跑（要在引导进行中的那一拍上跑）。");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"# 引导期可交互元素审计 {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        AppendState(sb);
        AppendElements(sb);

        string dir = Path.GetDirectoryName(ReportPath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(ReportPath, sb.ToString(), new UTF8Encoding(false));
        Debug.Log($"[引导审计] 完成，报告已写入 {ReportPath}\n{sb}");
    }

    /// <summary>现场状态：这一刻"玩家被允许做什么"。</summary>
    private static void AppendState(StringBuilder sb)
    {
        sb.AppendLine("## 1. 现场状态");
        sb.AppendLine($"- Time.timeScale = {Time.timeScale}{(Time.timeScale <= 0f ? "（冻场）" : "")}");
        sb.AppendLine($"- 输入门：{GameplayInputGate.Describe()}");
        sb.AppendLine($"- 允许被拦时放技能 = {GameplayInputGate.AllowSkillActivationWhileBlocked}；允许时停中放技能 = {GameplayInputGate.AllowSkillActivationWhileFrozen}");

        var input = InputManager.Instance;
        if (input != null)
        {
            sb.AppendLine($"- 手势闸门 tutorialGestureTarget = {input.tutorialGestureTarget}；允许时停中输入 = {input.allowInputWhileTimeFrozen}");
            sb.AppendLine($"- gameplayInputEnabled = {input.gameplayInputEnabled}");
        }

        var dir = Object.FindObjectOfType<TutorialStageDirector>();
        if (dir != null)
        {
            sb.AppendLine($"- 教学导演：开场中 = {dir.IntroActive}；拍进行中 = {dir.BeatActive}；当前拍 = {(dir.CurrentStep != null ? dir.CurrentStep.kind + "@" + dir.CurrentStep.waveIndex : "无")}");
        }

        sb.AppendLine($"- 对话阻塞 = {(DialogueManager.Instance != null && DialogueManager.Instance.IsInteractionBlocked)}");
        sb.AppendLine($"- 防御键：可用 = {DefenseButton.Instance.Available}；静音 = {DefenseButton.Instance.Muted}");

        var inv = ActiveSkillInventory.Instance;
        sb.AppendLine($"- 主动技能：使用中 = {(inv != null && inv.UsesActiveSkills)}；条目 = {(inv != null ? inv.Entries.Count : 0)}");
        sb.AppendLine($"- 药水数量 = {HealthPotionManager.Instance.PotionCount}");
        sb.AppendLine();
    }

    /// <summary>屏上能点的东西 + 判定。</summary>
    private static void AppendElements(StringBuilder sb)
    {
        sb.AppendLine("## 2. 屏上可交互元素（真能点到的才列进「可点」栏）");
        sb.AppendLine();
        sb.AppendLine("| 元素 | 位置 | 可点 | 结局 | 说明 |");
        sb.AppendLine("|---|---|---|---|---|");

        var es = EventSystem.current;
        var seen = new HashSet<int>();

        // ① Button
        var buttons = Object.FindObjectsOfType<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            var b = buttons[i];
            if (b == null || !b.gameObject.activeInHierarchy || !b.interactable) continue;
            if (!seen.Add(b.GetInstanceID())) continue;

            var graphic = b.targetGraphic;
            bool clickable = false;
            string reason = "";
            if (graphic == null) reason = "没有 targetGraphic";
            else if (!graphic.raycastTarget) reason = "图片 raycastTarget=false（吃不到射线）";
            else
            {
                var canvas = b.transform.GetComponentInParent<Canvas>();
                if (canvas == null) reason = "不在 Canvas 下";
                else if (canvas.GetComponent<GraphicRaycaster>() == null) reason = $"所在 Canvas '{canvas.name}' 没有 GraphicRaycaster → 收不到射线";
                else if (es != null) clickable = TopHitIs(es, b.gameObject, out reason);
                else reason = "没有 EventSystem";
            }

            // ⚠️ 射线命中**不等于**点得动：`Button.OnPointerClick` 会先判 `IsInteractable()`，
            //   而它会把**祖先 CanvasGroup 的 interactable=false** 也算进去 →
            //   这种按钮看着能点，真点下去**一点反应都没有**（2026-11-06 实测踩到）。
            if (clickable && !b.IsInteractable())
            {
                clickable = false;
                reason = "Button.IsInteractable()=false（多半是祖先 CanvasGroup 的 interactable=false）→ 点击会被 Button 直接丢弃";
            }

            string name = b.name;
            string verdict = Judge(name, out string note);
            sb.AppendLine($"| {name} | {ShortPath(b.transform)} | {(clickable ? "**是**" : "否")} | {verdict} | {(note.Length > 0 ? note : reason)} |");
        }

        // ② BuffIcon（道具/技能槽：靠 BuffIcon.OnClicked，不是 Button）
        var icons = Object.FindObjectsOfType<BuffIcon>(true);
        for (int i = 0; i < icons.Length; i++)
        {
            var icon = icons[i];
            if (icon == null || !icon.gameObject.activeInHierarchy) continue;
            if (icon.OnClicked == null) continue;      // 没订阅点击 → 不是交互元素
            if (!seen.Add(icon.GetInstanceID())) continue;

            var canvas = icon.transform.GetComponentInParent<Canvas>();
            bool clickable = false;
            string reason = "";
            if (canvas == null) reason = "不在 Canvas 下";
            else if (canvas.GetComponent<GraphicRaycaster>() == null) reason = $"所在 Canvas '{canvas.name}' 没有 GraphicRaycaster";
            else if (es != null) clickable = TopHitIs(es, icon.gameObject, out reason);
            else reason = "没有 EventSystem";

            string verdict = Judge(icon.name, out string note);
            sb.AppendLine($"| {icon.name}(道具/技能) | {ShortPath(icon.transform)} | {(clickable ? "**是**" : "否")} | {verdict} | {(note.Length > 0 ? note : reason)} |");
        }

        sb.AppendLine();
        sb.AppendLine("### 结局口径");
        sb.AppendLine("- **允许**：设计如此（暂停键 / 开场「点击继续」）。");
        sb.AppendLine("- **已拦**：点得到但这一下不会生效（被输入门 / 时停 / 静音 / 未解锁拦下）。");
        sb.AppendLine("- **洞**：点得到**而且会真的生效**，但此刻玩家不该能做 → 要修。");
        sb.AppendLine();
        AppendPointerHandlers(sb, es);
        sb.AppendLine();
        sb.AppendLine("### 「点得到」≠「点得动」（两个独立条件）");
        sb.AppendLine("1. 射线首命中是自己（要 raycastTarget=true + 所在 Canvas 有 GraphicRaycaster + 上面没别的东西盖着）");
        sb.AppendLine("2. `Button.IsInteractable()` 为 true —— **祖先 CanvasGroup 的 interactable=false 会让它变 false**");
        sb.AppendLine("   → 这种按钮看着能点，真点下去**一点反应都没有**（2026-11-06 实测：「点击继续」被 DialogueOverlay 的 CanvasGroup 吃掉，真人点了没反应）");
        sb.AppendLine();
        sb.AppendLine("### 场景里 interactable=false 的 CanvasGroup（它们会让整棵子树的按钮失效）");
        foreach (var cg in Object.FindObjectsOfType<CanvasGroup>(true))
        {
            if (cg.interactable && cg.blocksRaycasts) continue;
            var canvas = cg.GetComponentInParent<Canvas>();
            sb.AppendLine($"- `{cg.name}`（在 Canvas '{(canvas != null ? canvas.name : "?")}' 下）：interactable={cg.interactable} blocksRaycasts={cg.blocksRaycasts} active={cg.gameObject.activeInHierarchy}");
        }
        sb.AppendLine();
        sb.AppendLine("### 药水/道具这条路径（不经过手势系统）");
        sb.AppendLine("- 血包与道具都是**点道具栏图标**触发的 → 不走手势输入路径 → `GameplayInputGate` 拦不到。");
        sb.AppendLine("- **2026-11-06 已补门控**：`HealthPotionManager.CanUsePotionNow()`（时停 / 输入门拦下时不能喝）");
        sb.AppendLine("  + `FrontItemBar.OnItemIconClicked` / `BuffDisplayPanel.OnItemIconClicked` 整体拦下（教学「用技能打断」那一拍除外）。");
        sb.AppendLine("- 现状判据：CanUsePotionNow() = " + HealthPotionManager.CanUsePotionNow());
    }

    /// <summary>
    /// 扫一遍场景里所有 **IPointerClickHandler**（不只 Button）——
    /// 很多 UI（道具图标、buff 图标）是靠事件回调接点击的，漏掉它们就会漏掉「还能点别的 UI」。
    /// </summary>
    private static void AppendPointerHandlers(StringBuilder sb, EventSystem es)
    {
        sb.AppendLine("### 所有接点击回调的组件（IPointerClickHandler，不只 Button）");
        var allBehaviours = Object.FindObjectsOfType<MonoBehaviour>(true);
        var handlers = new List<UnityEngine.EventSystems.IPointerClickHandler>();
        for (int i = 0; i < allBehaviours.Length; i++)
        {
            if (allBehaviours[i] is UnityEngine.EventSystems.IPointerClickHandler pch) handlers.Add(pch);
        }
        var reachable = new List<string>();
        var unreachable = new List<string>();
        for (int i = 0; i < handlers.Count; i++)
        {
            var h = handlers[i] as Component;
            if (h == null || !h.gameObject.activeInHierarchy) continue;
            var canvas = h.transform.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.GetComponent<GraphicRaycaster>() == null) continue;

            bool hit = es != null && TopHitIs(es, h.gameObject, out string _);
            string desc = $"{h.GetType().Name} '{h.name}'（Canvas '{canvas.name}' ord={canvas.sortingOrder}）";
            if (hit) reachable.Add(desc); else unreachable.Add(desc);
        }
        sb.AppendLine($"- **射线能命中**（= 玩家真点得到）：{reachable.Count} 个");
        for (int i = 0; i < reachable.Count; i++) sb.AppendLine($"    · {reachable[i]}");
        sb.AppendLine($"- 命中不到（被遮罩/无 raycaster/被盖住）：{unreachable.Count} 个");
        for (int i = 0; i < unreachable.Count && i < 12; i++) sb.AppendLine($"    · {unreachable[i]}");
        sb.AppendLine();
    }

    /// <summary>射线首命中是不是这个元素（把子物体也算上）。</summary>
    private static bool TopHitIs(EventSystem es, GameObject target, out string reason)
    {
        var rect = target.GetComponent<RectTransform>();
        if (rect == null) { reason = "没有 RectTransform"; return false; }
        var canvas = target.GetComponentInParent<Canvas>();
        var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        var pos = RectTransformUtility.WorldToScreenPoint(cam, rect.TransformPoint(rect.rect.center));

        var ped = new PointerEventData(es) { position = pos };
        var results = new List<RaycastResult>();
        es.RaycastAll(ped, results);
        if (results.Count == 0) { reason = "该点射线没有任何命中"; return false; }

        var top = results[0].gameObject;
        if (top == target || top.transform.IsChildOf(target.transform)) { reason = "首命中就是它自己"; return true; }
        reason = $"被 '{top.name}' 盖住（ord={results[0].sortingOrder}）";
        return false;
    }

    /// <summary>判定这个元素此刻点了会怎样。</summary>
    private static string Judge(string name, out string note)
    {
        note = "";
        if (AllowedByDesign.Contains(name)) { note = "设计允许"; return "允许"; }

        if (name == "DefenseButton")
        {
            var db = DefenseButton.Instance;
            if (db == null) { note = "防御键组件没找到"; return "已拦"; }
            if (!db.Available) { note = "还没教到防御键 → 隐藏且不吃射线"; return "已拦"; }
            if (db.Muted) { note = "静音中（压暗不可用）"; return "已拦"; }
            note = "防御可用（这一拍就是要玩家按它）";
            return "允许";
        }

        if (name.Contains("ItemSlot") || name.Contains("BuffIcon"))
        {
            var inv = ActiveSkillInventory.Instance;
            bool skillMode = inv != null && inv.UsesActiveSkills;
            bool frozen = Time.timeScale <= 0f;
            bool blocked = GameplayInputGate.IsBlocked;

            if (skillMode)
            {
                if (blocked && !GameplayInputGate.AllowSkillActivationWhileBlocked) { note = "技能释放被输入门拦下"; return "已拦"; }
                if (frozen && !GameplayInputGate.AllowSkillActivationWhileFrozen) { note = "时停中不能放技能"; return "已拦"; }
                if (DialogueManager.Instance != null && DialogueManager.Instance.IsInteractionBlocked
                    && !GameplayInputGate.AllowSkillActivationWhileBlocked) { note = "对话在播，挡下了技能点击"; return "已拦"; }
                note = "技能可释放（这一拍就是要玩家点技能槽）";
                return "允许";
            }

            // 道具/药水路径
            if (!HealthPotionManager.CanUsePotionNow())
            {
                note = "物品/药水路径已加门控（引导/暂停/时停期间点不动）";
                return "已拦";
            }
            if (HealthPotionManager.Instance != null && HealthPotionManager.Instance.PotionCount > 0)
            {
                note = "**药水路径仍无门控 → 引导期点它会真的喝掉（洞）**";
                return "洞";
            }
            note = "身上没有药水/道具（当前点不出东西）";
            return "已拦";
        }

        note = "需要人工确认这个按钮点了会发生什么";
        return "待确认";
    }

    private static string ShortPath(Transform t)
    {
        var parts = new List<string>();
        var cur = t;
        int guard = 0;
        while (cur != null && guard++ < 8) { parts.Insert(0, cur.name); cur = cur.parent; }
        return string.Join("/", parts);
    }
}
