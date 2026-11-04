using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// **招式解锁引导**（2026-11-03）。
///
/// 场景：招式被放在天赋节点后面（解锁才可用）→ 玩家解锁后如果没人教，就永远学不会这个手势。
/// 规则：**解锁后第一次进关**时，在波次开始前弹一次「手势示范 + 一行台词」✅（挡住开打 → 玩家确认后才开打 ✅）。
///
/// 口径：
///   • **谁要教**：天赋节点自己配了 <c>moveGuideGesture</c> 才算要教 ✅（数据决定，不写死招式名 ✅）；
///   • **教没教过**：存档 <c>SaveData.taughtMoveGuides</c>（同一存档只教一次 ✅）；
///   • **没 UI 就不教**：界面层没登记端口时直接跳过 ✅（不会把「教过」写进存档 → 下次还教 ✅）。
/// </summary>
public static class MoveGuideService
{
    /// <summary>收集「已解锁但还没教过」的招式引导（按天赋页顺序：目录里的页 → 武将挂的那棵）。</summary>
    /// <param name="tree">武将直接挂的树（兼容旧配置 / 目录没配时的兜底）。</param>
    /// <param name="catalog">天赋目录（3 棵分类树）。**只扫 tree 会漏掉页 1 / 页 3 的节点**。</param>
    public static List<MoveGuideRequest> CollectPending(TalentTreeConfig tree, TalentCatalog catalog = null)
    {
        var result = new List<MoveGuideRequest>();
        var data = SaveManager.Load();
        var seen = new HashSet<string>();

        // 先扫目录里的全部分类（顺序 = 页签顺序）
        if (catalog != null && catalog.categories != null)
        {
            for (int c = 0; c < catalog.categories.Length; c++)
                CollectInto(result, seen, data, catalog.categories[c]);
        }

        // 兜底：武将直接挂的那棵（目录没配 / 老存档时也能教）
        CollectInto(result, seen, data, tree);
        return result;
    }

    private static void CollectInto(List<MoveGuideRequest> result, HashSet<string> seen,
        SaveData data, TalentTreeConfig tree)
    {
        if (tree == null || tree.nodes == null) return;
        for (int i = 0; i < tree.nodes.Count; i++)
        {
            var node = tree.nodes[i];
            if (node == null) continue;
            if (node.moveGuideGesture == MoveGuideGesture.None) continue;   // 没配手势 = 不教
            if (node.moveConfig == null) continue;                          // 只教「解锁招式」的节点
            if (!TalentTreeState.IsUnlocked(node)) continue;                // 没解锁 = 不教
            if (MoveGuideSaveSection.IsTaught(data, node.nodeId)) continue; // 教过了 = 不再教
            if (!seen.Add(node.nodeId)) continue;                          // 同一节点只教一次

            result.Add(new MoveGuideRequest
            {
                nodeId = node.nodeId,
                title = node.displayName,
                move = node.moveConfig,
                gesture = node.moveGuideGesture,
                dialogue = node.moveGuideDialogue
            });
        }
    }

    /// <summary>标记「这条引导已经教过」（写存档 ✅）。</summary>
    public static void MarkTaught(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)) return;
        var data = SaveManager.Load();
        MoveGuideSaveSection.MarkTaught(data, nodeId);
        SaveManager.Save(data);
    }

    /// <summary>
    /// 把待教的引导依次弹出来（**挡住开打**：调用方要等这个协程结束再开波 ✅）。
    /// 没有界面端口（端口没登记）就直接跳过 ✅。
    /// </summary>
    public static IEnumerator ShowPending()
    {
        var presenter = CombatPresenters.MoveGuide;
        if (presenter == null) yield break;

        var ps = PlayerState.Instance;
        var hero = ps != null ? ps.heroConfig : null;
        var tree = hero != null ? hero.talentTree : null;
        var catalog = hero != null ? hero.talentCatalog : null;

        List<MoveGuideRequest> pending = CollectPending(tree, catalog);
        for (int i = 0; i < pending.Count; i++)
        {
            MoveGuideRequest request = pending[i];
            bool done = false;
            presenter.ShowMoveGuide(request, () => done = true);

            // 等引导播完（最多 20 秒兜底，防止界面异常把战斗卡死 ✅）。
            float waited = 0f;
            while (!done && presenter.IsShowing && waited < 20f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            if (done)
            {
                // 只有**完整演完**才标记「已教过」——
                // 演出被掐断（超时兜底 / 界面异常）时不能标记，
                // 否则玩家会**永久失去**这个引导（第一次没看到 = 以后也不再教）。
                bool complete = presenter.CompletedNormally;
                if (complete) MarkTaught(request.nodeId);
                Debug.Log($"[MoveGuide] {(complete ? "已教" : "未完整播出（不标记教过，下次还会教）")}：{request.nodeId}（{request.title}）");
            }
        }
    }
}
