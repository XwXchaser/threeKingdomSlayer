using UnityEngine;

/// <summary>
/// **能力类天赋的消费读点**（2026-11-03 A5 建立，2026-11-03 B1 改为**通用数值载荷**驱动）。
///
/// 机制：节点带 <c>statId / statAdd</c> → 已解锁节点的 statAdd 按 statId 累加 → 本类把总额翻译成玩法数值。
/// 权威在 <see cref="TalentStatService"/>（读存档 + 能力页天赋树）。
///
/// 节点清单在 B 批还会调整 → **数值与 statId 的对应关系集中在这一个文件**，
/// 对齐时只改这里或节点资产，消费点不用动。
/// </summary>
public static class TalentCapabilities
{
    /// <summary>铁血 / 体魄：生命上限加成（每档 +30）。</summary>
    public static int ExtraMaxHealth => Mathf.RoundToInt(TalentStatService.GetTotal(TalentStatService.StatMaxHealth));

    /// <summary>不屈之志：复活次数加成。</summary>
    public static int ExtraReviveCount => Mathf.RoundToInt(TalentStatService.GetTotal(TalentStatService.StatReviveCount));

    /// <summary>行囊：道具槽加成。</summary>
    public static int ExtraItemSlot => Mathf.RoundToInt(TalentStatService.GetTotal(TalentStatService.StatItemSlot));

    /// <summary>巧思：主动技能槽加成。</summary>
    public static int ExtraActiveSlot => Mathf.RoundToInt(TalentStatService.GetTotal(TalentStatService.StatActiveSlot));

    /// <summary>博学：被动技能槽加成。</summary>
    public static int ExtraPassiveSlot => Mathf.RoundToInt(TalentStatService.GetTotal(TalentStatService.StatPassiveSlot));

    /// <summary>聚财：铜钱获取倍率（1 = 无加成）。只作用于敌人钱 / 里程碑 / 通关奖励。</summary>
    public static float CoinGainMultiplier => 1f + TalentStatService.GetTotal(TalentStatService.StatCoinGain);

    /// <summary>破阵：攻击的架势伤害加成（0 = 未解锁）。</summary>
    public static float AttackPoiseDamageBonus => TalentStatService.GetTotal(TalentStatService.StatAttackPoiseDamage);

    /// <summary>追击：击杀后攻击冷却缩短的秒数（0 = 未解锁）。</summary>
    public static float KillAttackCooldownReduction => TalentStatService.GetTotal(TalentStatService.StatKillCooldownReduction);

    /// <summary>运筹：三选一 → 四选一 的额外选项数（0 = 未解锁）。</summary>
    public static int ChoiceCountBonus => Mathf.RoundToInt(TalentStatService.GetTotal(TalentStatService.StatChoiceCount));

    /// <summary>不屈：血量低于 30% 时触发 2 秒无敌（每局 1 次）。</summary>
    public static bool UndyingUnlocked => TalentStatService.Has(TalentStatService.StatUndying);

    /// <summary>连击精通：连击阈值下调量（≤0；0 = 未解锁）。</summary>
    public static int ComboThresholdDelta => Mathf.RoundToInt(TalentStatService.GetTotal(TalentStatService.StatComboThresholdDelta));
}
