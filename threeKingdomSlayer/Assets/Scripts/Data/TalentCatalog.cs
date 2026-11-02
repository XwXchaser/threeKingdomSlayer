using UnityEngine;

/// <summary>
/// 天赋三分类目录（唯一入口）。
///
/// 为什么要有它：
/// · 面板要按分类取树（3 页签 = 3 棵树，各是一个 TalentTreeConfig 资产）；
/// · 读档过滤要「当前所有合法 nodeId」的集合 —— 数据层是纯静态、没有资产引用，
///   必须由外部把它递进去；
/// · 编辑器校验要做跨资产 id 唯一性检查。
///
/// 三分类之间**不做前置**（各自的前置只在各自的 TalentTreeConfig 内解析），
/// 所以这里只是把 3 份资产并列摆出来，不参与前置关系。
/// </summary>
[CreateAssetMenu(fileName = "TalentCatalog", menuName = "一夫当关/天赋目录")]
public class TalentCatalog : ScriptableObject
{
    public const int CategoryCount = 3;

    [Tooltip("固定 3 个槽位：0 = 基础数值 / 1 = 招式解锁 / 2 = 三选一解锁。\n"
           + "允许留空（留空 = 该分类暂时没有内容）；槽位顺序与页签顺序一致。")]
    public TalentTreeConfig[] categories = new TalentTreeConfig[CategoryCount];

    /// <summary>按分类下标取树；越界或留空返回 null。</summary>
    public TalentTreeConfig Get(int index)
    {
        if (categories == null || index < 0 || index >= categories.Length) return null;
        return categories[index];
    }

    /// <summary>该分类名下所有节点的 id（读档过滤 / 全局唯一性校验用）。把 ids 传进来由调用方复用容器。</summary>
    public void CollectNodeIds(System.Collections.Generic.ICollection<string> ids)
    {
        if (ids == null || categories == null) return;
        for (int c = 0; c < categories.Length; c++)
        {
            var treeRef = categories[c];
            if (treeRef == null || treeRef.nodes == null) continue;
            for (int i = 0; i < treeRef.nodes.Count; i++)
            {
                var node = treeRef.nodes[i];
                if (node == null || string.IsNullOrEmpty(node.nodeId)) continue;
                ids.Add(node.nodeId);
            }
        }
    }

    private void OnValidate()
    {
        // 槽位数量写死为 3，避免面板按 categoryIndex 取到 null 时还要处理数组长度不一致
        if (categories == null || categories.Length != CategoryCount)
        {
            var resized = new TalentTreeConfig[CategoryCount];
            if (categories != null)
                for (int i = 0; i < Mathf.Min(categories.Length, CategoryCount); i++)
                    resized[i] = categories[i];
            categories = resized;
        }
    }
}
