using System.Collections.Generic;

/// <summary>
/// **天赋数值载荷的读取层**（2026-11-03 B1）。
///
/// 能力天赋一律用 **statId / statAdd 通用载荷**表达（不用 flag 机制 —— <c>IsFlagUnlocked</c> 全项目零调用）：
///
/// ```
/// 节点解锁 → 按 statId 累加 statAdd → 消费点读总额
/// 例：体魄 I / II / 钢筋铁骨 三个节点都是 statId="max_health"、statAdd=30
///     → 全解锁 = +90 生命上限
/// ```
///
/// 权威来源：<c>SaveData.unlockedTalents</c>（nodeId 列表）。
/// 节点数据来源：<c>HeroConfig.abilityTree</c>（**Inspector 引用** → 可追踪，不做 Resources.Load / 字符串查表）。
///
/// 为什么需要「能力树」这一个引用：天赋页的目录（<c>TalentCatalog</c>）挂在**天赋 overlay 场景**上，
/// 战斗场景里并不加载 → 消费点不能靠目录找节点；能力页的树由武将资产直接指过来。
/// </summary>
public static class TalentStatService
{
    // ── statId 常量（节点资产与消费点共用同一套字符串）──
    public const string StatMaxHealth = "max_health";                    // 生命上限 +N
    public const string StatReviveCount = "revive_count";                // 复活次数 +N
    public const string StatItemSlot = "item_slot";                      // 道具槽 +N
    public const string StatActiveSlot = "active_slot";                  // 主动技能槽 +N
    public const string StatPassiveSlot = "passive_slot";                // 被动技能槽 +N
    public const string StatAttackPoiseDamage = "attack_poise_damage";  // 攻击架势伤害 +N
    public const string StatKillCooldownReduction = "kill_cooldown_reduction"; // 击杀后攻击 CD −N 秒
    public const string StatChoiceCount = "choice_count";                // 三选一选项数 +N
    public const string StatCoinGain = "coin_gain";                      // 铜钱获取 +N（0.3 = +30%）
    public const string StatUndying = "undying";                          // 不屈（濒死无敌）

    /// <summary>不屈：血量低于此比例（30%）时触发。</summary>
    public const float UndyingHealthThreshold = 0.30f;

    /// <summary>不屈：无敌时长（秒）。</summary>
    public const float UndyingInvincibleSeconds = 2f;

    /// <summary>已解锁节点里，某个 statId 的加成总额。</summary>
    public static float GetTotal(string statId)
    {
        if (string.IsNullOrEmpty(statId)) return 0f;
        float total = 0f;
        var unlocked = TalentSaveSection.GetUnlocked(SaveManager.Load());
        var tree = GetAbilityTree();
        if (unlocked == null || tree == null || tree.nodes == null) return 0f;

        for (int i = 0; i < tree.nodes.Count; i++)
        {
            var node = tree.nodes[i];
            if (node == null || string.IsNullOrEmpty(node.nodeId)) continue;
            if (node.statId != statId) continue;
            if (!unlocked.Contains(node.nodeId)) continue;
            total += node.statAdd;
        }
        return total;
    }

    /// <summary>某个能力是否已解锁（布尔型，如「不屈」：statAdd &gt; 0 即视为已解锁）。</summary>
    public static bool Has(string statId) => GetTotal(statId) > 0f;

    /// <summary>能力页的天赋树（由武将资产指定；没有则没有能力加成）。</summary>
    private static TalentTreeConfig GetAbilityTree()
    {
        var ps = PlayerState.Instance;
        return ps != null && ps.heroConfig != null ? ps.heroConfig.abilityTree : null;
    }
}
