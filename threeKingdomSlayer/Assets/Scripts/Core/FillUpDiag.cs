using UnityEngine;

/// <summary>
/// **补齐（波次行军）诊断日志** —— 2026-11-09 BUG A「前方没有敌人也不前移」定位用。
///
/// 只输出、**不改任何行为**。定位完成后删掉本文件 + 各调用点即可（调用点都是单行）。
///
/// 要看清楚的四件事：
///   ① 逻辑排队列的形状：每个排的种类（活人排 / 空排占位 / 999 节奏门）、排号、成员明细；
///   ② 前导空排：按设计**只在前方还有活人排时保留**；前方本来就没活人排时它必须被移除，
///      否则后面的活人排永远停在 row1（玩家射程只有 1 排 → 打不到 → 这一波推不动）；
///   ③ 幽灵排：敌人被移出列时，它的**逻辑排归属**必须一起注销；否则那一排在队列里
///      仍然算「活人排」→ 后面的排永远不前移；
///   ④ 「整排清空」判定必须**排除正在被移出的那一个**，否则非死亡离场
///      （109 骑兵回退淡出、教学 despawn）会让那一排看起来还活着 → 不触发重排。
///
/// 所有方法都标了 <see cref="ConditionalAttribute"/>（UNITY_EDITOR）→ **打包产物里整段被剥离，零开销**。
/// </summary>
public static class FillUpDiag
{
    /// <summary>总开关（Editor 内也能临时关掉）。</summary>
    public static bool Enabled = true;

    // ── 快照构造 ────────────────────────────────────────────────────────────

    /// <summary>逻辑排队列的完整快照（每个排一行 + 成员明细）。</summary>
    public static string BuildQueueDump(LogicalRowQueue queue)
    {
        if (queue == null) return "    (队列为空)";
        var sb = new System.Text.StringBuilder();
        var rows = queue.Rows;
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            sb.Append($"    [Q{i}] kind={row.kind} init={row.initialRow} cur={row.currentRow} removed={row.removed} "
                    + $"members=[{DescribeMembers(queue, row)}]\n");
        }
        return sb.ToString();
    }

    /// <summary>某个逻辑排的成员明细（状态 / 排号 / 列号）。</summary>
    public static string DescribeMembers(LogicalRowQueue queue, LogicalWaveRow row)
    {
        if (queue == null || row == null) return "(无)";
        var sb = new System.Text.StringBuilder();
        bool first = true;
        foreach (var pair in queue.EnemyRows)
        {
            if (!ReferenceEquals(pair.Value, row)) continue;
            var e = pair.Key;
            if (!first) sb.Append(", ");
            first = false;
            if (e == null) { sb.Append("null"); continue; }
            sb.Append($"{e.DebugTag}[{e.state} row={e.rowIndex} col={e.columnIndex}]");
        }
        return first ? "(空)" : sb.ToString();
    }

    /// <summary>敌人一句话描述。</summary>
    public static string Describe(Enemy e)
    {
        if (e == null) return "(null)";
        return $"{e.DebugTag}[{e.state} row={e.rowIndex} col={e.columnIndex}]";
    }

    // ── 各个诊断点 ──────────────────────────────────────────────────────────

    /// <summary>开波：逻辑排队列被重建（能看到前导空排是不是从一开始就存在）。</summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogQueueConfigured(LogicalRowQueue queue, string reason)
    {
        if (!Enabled || queue == null) return;
        Debug.Log($"[FillDiag] 逻辑排队列重建（{reason}）\n{BuildQueueDump(queue)}");
    }

    /// <summary>运行时排号重排：逐排说明「去了哪 / 为什么被移除」。</summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogReflowRow(int index, LogicalWaveRow row, string what)
    {
        if (!Enabled || row == null) return;
        Debug.Log($"[FillDiag] 重排 [Q{index}] kind={row.kind} init={row.initialRow} cur={row.currentRow} → {what}");
    }

    /// <summary>运行时排号重排后的队列快照。</summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogReflowSummary(LogicalRowQueue queue)
    {
        if (!Enabled || queue == null) return;
        Debug.Log($"[FillDiag] 重排后队列快照：\n{BuildQueueDump(queue)}");
    }

    /// <summary>
    /// 敌人被移出列时：这一排到底还剩几个人。
    /// <paramref name="registeredAfterRemove"/> = 移除之后它**仍在队列里登记**（= 幽灵排）。
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogRemoveEnemy(LogicalRowQueue queue, Enemy enemy, int columnIndex,
        bool registeredAfterRemove, LogicalWaveRow logicalRow,
        bool livingAsIs, bool livingExcludingSelf,
        bool mayCreateWaveMarch, bool reflowRequested)
    {
        if (!Enabled) return;
        string rowInfo = logicalRow != null
            ? $"kind={logicalRow.kind} init={logicalRow.initialRow} cur={logicalRow.currentRow} removed={logicalRow.removed}"
            : "(不在任何逻辑排)";
        Debug.Log(
            $"[FillDiag] 移出列：{Describe(enemy)} col={columnIndex} isBoss={(enemy != null && enemy.isBoss)}\n"
            + $"    队列登记（移除后仍在队列里）={registeredAfterRemove} 所属逻辑排={rowInfo}\n"
            + $"    该排活人数：现有判定(HasLivingMembers)={livingAsIs} / 排除本敌={livingExcludingSelf}\n"
            + $"    mayCreateWaveMarch={mayCreateWaveMarch}（要求 state==Dead 且非 Boss）→ 已请求重排={reflowRequested}\n"
            + $"    队列快照：\n{BuildQueueDump(queue)}");
    }

    /// <summary>波次行军规划：逐个敌人说明「为什么派 / 为什么跳」。</summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogPlanEnemy(Enemy enemy, LogicalWaveRow logicalRow, int targetRow, string reason)
    {
        if (!Enabled) return;
        string rowInfo = logicalRow != null
            ? $"init={logicalRow.initialRow} cur={logicalRow.currentRow} kind={logicalRow.kind}"
            : "无";
        Debug.Log($"[FillDiag] 行军规划 {Describe(enemy)} 逻辑排({rowInfo}) 目标排={targetRow} → {reason}");
    }

    /// <summary>波次行军规划汇总 + 队列快照。</summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogPlanSummary(LogicalRowQueue queue, int candidates, int planned)
    {
        if (!Enabled) return;
        Debug.Log($"[FillDiag] 行军规划汇总：候选={candidates} 派出={planned}\n{BuildQueueDump(queue)}");
    }
}
