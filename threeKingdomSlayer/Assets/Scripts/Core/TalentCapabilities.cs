/// <summary>
/// **能力类天赋的读取层**（2026-11-03 A5）。
///
/// 为什么单独一层：
///   • 能力天赋一律用 **nodeId 解锁态** 表达 —— 项目里 <c>IsFlagUnlocked</c> 零调用，
///     本周统一用 nodeId（存档里就是 <c>SaveData.unlockedTalents</c> 的 id 列表）。
///   • 节点清单在 B 批还会重排（补节点 / 改分支）→ **nodeId 与数值集中在这一个文件**，
///     对齐时只改这里，消费点不用动。
///
/// 数值与分支来自 <c>design/talent-tree-architecture.md</c> §2.1 / §6：
///   战斗向 combat   破阵 → 追击 → 运筹
///   生存向 body     铁血(生命+30) → 连击精通(连击阈值↓) → 不屈
///   经济向 fortune  聚财(铜钱+30%) → 巧思(主动槽+1) → 博学(被动槽+1)
///
/// ⚠️ **只读存档**：这里的每个读点都是「已解锁就生效」，不写存档、不改玩法流程。
/// </summary>
public static class TalentCapabilities
{
    // ── 节点 id（与天赋树资产里的 nodeId 对应；B 批补节点后在这里对齐）──
    public const string NodeBreakFormation = "talent_break_formation";    // 破阵（战斗向）
    public const string NodePursuit        = "talent_pursuit";           // 追击（战斗向）
    public const string NodeStrategy       = "talent_strategy";          // 运筹（战斗向）
    public const string NodeUndying        = "talent_undying";           // 不屈（生存向）
    public const string NodeVitality1      = "talent_stat_hp1";          // 体魄 I（生存向）
    public const string NodeVitality2      = "talent_stat_hp2";          // 体魄 II
    public const string NodeVitality3      = "talent_stat_hp3";          // 钢筋铁骨
    public const string NodeFortune        = "talent_stat_coin";         // 聚财（经济向，节点待建）
    public const string NodeCunning        = "talent_stat_slot_active";  // 巧思（主动槽+1，节点待建）
    public const string NodeErudition      = "talent_stat_slot_passive"; // 博学（被动槽+1，节点待建）

    // ── 数值（设计文档给定；改动 = 改设计文档）──
    /// <summary>铁血：每解锁一档体魄 +30 生命上限。</summary>
    public const int VitalityBonusPerTier = 30;
    /// <summary>聚财：铜钱获取 +30%。</summary>
    public const float FortuneCoinBonus = 0.30f;
    /// <summary>破阵：所有攻击 +1 架势伤害。</summary>
    public const float BreakFormationPoiseBonus = 1f;
    /// <summary>追击：击杀后攻击 CD −0.5s。</summary>
    public const float PursuitCooldownReduction = 0.5f;
    /// <summary>运筹：三选一 → 四选一。</summary>
    public const int StrategyExtraChoice = 1;
    /// <summary>不屈：血量低于 30% 触发。</summary>
    public const float UndyingHealthThreshold = 0.30f;
    /// <summary>不屈：无敌时长（秒）。</summary>
    public const float UndyingInvincibleSeconds = 2f;

    // ── 读点 ──

    public static bool BreakFormationUnlocked => IsUnlocked(NodeBreakFormation);
    public static bool PursuitUnlocked => IsUnlocked(NodePursuit);
    public static bool StrategyUnlocked => IsUnlocked(NodeStrategy);
    public static bool UndyingUnlocked => IsUnlocked(NodeUndying);
    public static bool FortuneUnlocked => IsUnlocked(NodeFortune);

    /// <summary>
    /// 铁血（体魄）加成：每解锁一档 +30 生命上限。
    /// 体魄 I / II / 钢筋铁骨 三档 → 0 / +30 / +60 / +90。
    /// </summary>
    public static int ExtraMaxHealth
    {
        get
        {
            int tiers = 0;
            if (IsUnlocked(NodeVitality1)) tiers++;
            if (IsUnlocked(NodeVitality2)) tiers++;
            if (IsUnlocked(NodeVitality3)) tiers++;
            return tiers * VitalityBonusPerTier;
        }
    }

    /// <summary>聚财：铜钱获取倍率（1 = 无加成）。只作用于敌人钱 / 里程碑 / 通关奖励。</summary>
    public static float CoinGainMultiplier => FortuneUnlocked ? 1f + FortuneCoinBonus : 1f;

    /// <summary>破阵：攻击的架势伤害加成（0 = 未解锁）。</summary>
    public static float AttackPoiseDamageBonus => BreakFormationUnlocked ? BreakFormationPoiseBonus : 0f;

    /// <summary>追击：击杀后攻击冷却缩短的秒数（0 = 未解锁）。</summary>
    public static float KillAttackCooldownReduction => PursuitUnlocked ? PursuitCooldownReduction : 0f;

    /// <summary>运筹：三选一 → 四选一 的额外选项数（0 = 未解锁）。</summary>
    public static int ChoiceCountBonus => StrategyUnlocked ? StrategyExtraChoice : 0;

    private static bool IsUnlocked(string nodeId) => TalentTreeState.IsUnlocked(nodeId);
}
