using System;
using System.Collections.Generic;

/// <summary>已装备的一件装备（换装列表用：身份 + 当前等级）。</summary>
public struct EquippedItem
{
    public UpgradeDefinition definition;
    public int level;

    public EquippedItem(UpgradeDefinition definition, int level)
    {
        this.definition = definition;
        this.level = level;
    }
}

/// <summary>
/// **满槽换装端口**（2026-11-25）——可选扩展接口。
///
/// 口径（`design/gameplay-progression-loop.md` §3.4 / P0 计划 §1.3）：
///   · 已有同款 → 点新卡直接替换原槽（不经过这个端口）；
///   · 没有同款且该类满槽 → 点新卡后，在**下方列出同类别的已装备装备**，点旧装备替换；
///   · 选择完成前整个奖励流程保持等待，不推进队列 / 下一波；**不新增**取消、跳过、重抽、二次确认。
///
/// 弹窗没实现这个接口时，抽取器会报错并跳过本次奖励（不会静默吞掉、也不会伪造候选）。
/// </summary>
public interface IEquipmentReplacePrompt
{
    /// <summary>当前是否正在等玩家点「要换掉的旧装备」。</summary>
    bool IsShowing { get; }

    /// <summary>
    /// 在候选卡下方展示同类别已装备名单。
    /// </summary>
    /// <param name="newVariant">玩家刚点的新装备变体（用于标题 / 对比展示）。</param>
    /// <param name="equipped">同类别已装备的旧装备（按持有顺序）。</param>
    /// <param name="onPicked">玩家点了某件旧装备 → 用它换上新变体。</param>
    void ShowReplaceTargets(UpgradeDefinition newVariant, List<EquippedItem> equipped, Action<UpgradeDefinition> onPicked);

    /// <summary>收起名单（替换成功 / 奖励结束时调用）。</summary>
    void HideReplaceTargets();
}
