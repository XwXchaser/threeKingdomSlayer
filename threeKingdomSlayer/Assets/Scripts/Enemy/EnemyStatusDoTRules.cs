using UnityEngine;

/// <summary>
/// 敌人侧 **DoT（持续伤害）状态的公共规则**：结算时机、BOSS 暂停、战场列管理器解析。
///
/// 迁移说明（2026-10-22，批次 C）：灼烧与疾病原本由玩家侧的 <c>UpgradeEffectManager</c>
/// 用字典持有并统一结算——状态挂在**别人**身上，「敌人是不是在燃烧」只能反过来去问管理器。
/// 迁到敌人侧后，两个状态的结算口径必须一致，所以「能不能结算」只写一份，放在这里。
///
/// 门槛与迁移前逐条对齐：
///   • 关卡不在进行中 → 不结算（避免通关结算后残留 DoT 继续掉血、影响击杀计数与奖励）；
///   • BOSS 在非应战 / QTE 攻击 / 阶段切换期间 → 暂停结算。
/// </summary>
public static class EnemyStatusDoTRules
{
    /// <summary>当前是否允许该敌人的 DoT 结算。</summary>
    public static bool IsTickingAllowed(Enemy enemy)
    {
        if (enemy == null) return false;
        if (PlayerState.Instance == null || PlayerState.Instance.stageState != StageState.InProgress) return false;
        if (IsPausedForBoss(enemy)) return false;
        return true;
    }

    /// <summary>BOSS 在非应战 / QTE 攻击 / 阶段切换期间暂停 DoT。</summary>
    public static bool IsPausedForBoss(Enemy enemy)
    {
        return enemy != null && enemy.isBoss &&
            (enemy.bossState != BossState.InCombat || enemy.state == EnemyState.QTEAttacking || enemy.isPhaseTransitioning);
    }

    /// <summary>
    /// 战场列管理器（疾病扩散要按列 / 按排找目标）。
    /// 优先用敌人侧自己管理的那个（<see cref="EnemyManager"/>），拿不到再退到攻击系统那份。
    /// </summary>
    public static ColumnManager ResolveColumnManager()
    {
        if (EnemyManager.Instance != null && EnemyManager.Instance.columnManager != null)
            return EnemyManager.Instance.columnManager;
        return AttackSystem.Instance != null ? AttackSystem.Instance.columnManager : null;
    }

    /// <summary>诊断用：当前是否处于路线战斗（<c>null</c> 表示拿不到状态源）。</summary>
    public static string DescribeRouteCombat()
    {
        var controller = StageRoles.BattleState;
        if (controller == null) return "NULL";
        return controller.IsRouteCombatActive.ToString();
    }
}
