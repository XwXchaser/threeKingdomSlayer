using System.Collections.Generic;

/// <summary>
/// 天赋树**自动切页**：把一个分类里的节点按「行」切成若干页。
///
/// 硬约束（用户定）：**页边界不能跨连线**（不做跨页前置）。
/// 所以切点不是由数量决定的，而是由「图的缝隙」决定的：
///   · 行 = 相同 y 的节点（用 treePosition.y 推导，不需要新字段）
///   · 断点 r 合法 ⟺ 没有任何一条边从 r 及其上方跨到 r+1 及其下方
///   · 每页行数 ≤ rowsPerPage（软上限）；预算超了就往回找最近的合法断点
///   · 找不到合法断点 = 这棵子树自己就比一页高 → 溢出成一页长页，并报问题（数据侧应拆成多个独立子树）
///
/// 纯数据、不依赖 UI：面板用它算页，校验器用它验数据。
/// </summary>
public static class TalentPaginator
{
    /// <summary>一页：行区间 + 落在这一页的节点。</summary>
    public sealed class TalentPage
    {
        public int index;
        public int rowStart;
        public int rowEnd;
        public float topY;      // 本页最上一行的 y
        public float bottomY;   // 本页最下一行的 y
        public readonly List<TalentNodeConfig> nodes = new List<TalentNodeConfig>();

        /// <summary>本页内容中心 y（节点坐标减去它即可在页内居中）。</summary>
        public float CenterY => (topY + bottomY) * 0.5f;
    }

    /// <summary>
    /// 把一棵树切成若干页。problems 非空时会追加「无合法断点、只能超页」之类的问题描述。
    /// </summary>
    public static List<TalentPage> Split(TalentTreeConfig tree, int rowsPerPage, List<string> problems = null)
    {
        var pages = new List<TalentPage>();
        if (tree == null || tree.nodes == null || tree.nodes.Count == 0) return pages;
        if (rowsPerPage < 1) rowsPerPage = 1;

        // 1) 行：相同 y（四舍五入到 0.1）归为一行，按 y 从大到小排
        var ys = new List<float>();
        for (int i = 0; i < tree.nodes.Count; i++)
        {
            var n = tree.nodes[i];
            if (n == null) continue;
            float y = Round1(n.treePosition.y);
            if (!ys.Contains(y)) ys.Add(y);
        }
        ys.Sort((a, b) => b.CompareTo(a));   // 上 → 下
        var rowOf = new Dictionary<TalentNodeConfig, int>();
        for (int i = 0; i < tree.nodes.Count; i++)
        {
            var n = tree.nodes[i];
            if (n == null) continue;
            rowOf[n] = ys.IndexOf(Round1(n.treePosition.y));
        }
        int rowCount = ys.Count;
        if (rowCount == 0) return pages;

        // 2) 断点合法性：cutAfter(r) 表示「第 r 行与第 r+1 行之间」能不能切
        var cutLegal = new bool[Mathf2.Max(1, rowCount)];
        for (int r = 0; r < rowCount; r++) cutLegal[r] = true;
        for (int i = 0; i < tree.nodes.Count; i++)
        {
            var child = tree.nodes[i];
            if (child == null || !rowOf.ContainsKey(child)) continue;
            var prereqs = tree.GetPrerequisites(child);
            for (int p = 0; p < prereqs.Count; p++)
            {
                var parent = prereqs[p];
                if (parent == null || !rowOf.ContainsKey(parent)) continue;
                int a = rowOf[child];
                int b = rowOf[parent];
                int lo = a < b ? a : b;
                int hi = a < b ? b : a;
                // 这条边跨越了 lo..hi-1 这些缝
                for (int r = lo; r <= hi - 1; r++)
                    if (r >= 0 && r < cutLegal.Length) cutLegal[r] = false;
            }
        }

        // 3) 贪心装箱：预算内尽量多装，但只能停在合法断点
        int start = 0;
        while (start < rowCount)
        {
            int limit = start + rowsPerPage - 1;          // 本页最多到这一行
            if (limit >= rowCount - 1)
            {
                AddPage(pages, tree, rowOf, ys, start, rowCount - 1);
                break;
            }

            int end = -1;
            for (int r = limit; r >= start; r--)           // 预算内从下往上找合法断点
            {
                if (r < rowCount - 1 && cutLegal[r]) { end = r; break; }
            }

            if (end < 0)
            {
                // 预算内没有缝隙 → 这棵子树本身超过一页，只能延长到下一个缝隙
                for (int r = limit; r < rowCount - 1; r++)
                {
                    if (cutLegal[r]) { end = r; break; }
                }
                if (end < 0) end = rowCount - 1;
                problems?.Add(
                    $"第 {pages.Count + 1} 页超过 {rowsPerPage} 行：从第 {start + 1} 行往下找不到页内合法断点（连线会跨页）。" +
                    "数据侧建议把它拆成多个没有前置关系的独立子树。");
            }

            AddPage(pages, tree, rowOf, ys, start, end);
            start = end + 1;
        }

        for (int i = 0; i < pages.Count; i++) pages[i].index = i;
        return pages;
    }

    private static void AddPage(List<TalentPage> pages, TalentTreeConfig tree,
                                Dictionary<TalentNodeConfig, int> rowOf, List<float> ys, int rowStart, int rowEnd)
    {
        var page = new TalentPage
        {
            index = pages.Count,
            rowStart = rowStart,
            rowEnd = rowEnd,
            topY = ys[rowStart],
            bottomY = ys[rowEnd]
        };
        for (int i = 0; i < tree.nodes.Count; i++)
        {
            var n = tree.nodes[i];
            if (n == null || !rowOf.ContainsKey(n)) continue;
            int r = rowOf[n];
            if (r >= rowStart && r <= rowEnd) page.nodes.Add(n);
        }
        pages.Add(page);
    }

    private static float Round1(float v) => (float)System.Math.Round(v, 1);

    /// <summary>避免在纯数据类里引 UnityEngine.Mathf（这里只需要 Max）。</summary>
    private static class Mathf2
    {
        public static int Max(int a, int b) => a > b ? a : b;
    }
}
