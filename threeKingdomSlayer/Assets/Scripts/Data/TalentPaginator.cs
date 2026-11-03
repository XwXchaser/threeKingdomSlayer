using System.Collections.Generic;

/// <summary>
/// 天赋树**自动切页**：把一个分类里的节点按「行」切成若干页。
///
/// 硬约束（用户定，2026-11-03 更新）：**允许前置链跨页**（方案 B ✅）。
/// 所以切页只按**行数预算**（rowsPerPage）切，不再要求页边界避开连线；
/// 跨页的前置边会被记进 problems，面板据此在子节点上画一段「向上续接」短线 ✅。
///
/// 行 = 相同 y 的节点（用 treePosition.y 推导，不需要新字段）；
/// 竖向树版式下 **y 越大越靠上**（根节点 y=0，子节点 y 为负）→ 分页顺序 = 从上到下 ✅。
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

        // 3) 贪心装箱：**只按行数预算切**（允许前置链跨页 ✅ 用户已确认方案 B）
        int start = 0;
        while (start < rowCount)
        {
            int limit = start + rowsPerPage - 1;
            if (limit >= rowCount - 1)
            {
                AddPage(pages, tree, rowOf, ys, start, rowCount - 1);
                break;
            }

            // 跨页的前置边：记一笔，面板据此画续接线（不是错误，只是提示）
            int crossed = 0;
            for (int r = start; r <= limit; r++)
                if (!cutLegal[r]) crossed++;
            if (crossed > 0)
            {
                problems?.Add(
                    $"第 {pages.Count + 1} 页到第 {pages.Count + 2} 页之间有 {crossed} 条前置边跨页" +
                    "（允许跨页 ✅，面板会在子节点上画向上续接线）。");
            }

            AddPage(pages, tree, rowOf, ys, start, limit);
            start = limit + 1;
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
