using UnityEngine;

/// <summary>
/// **拆下一件占槽饰品**（2026-11-24，影之武装 / 饰品可替换）。
///
/// 与 <see cref="UpgradeEffectManager.ResetAll"/> 的区别：
///   · `ResetAll` 是**开局 / 重开**用的全量清零：连主动技能、道具一起清，并重置各项倍率基线；
///   · 本方法是**换装**用的单件卸载：只拆这一件，**不动**其它已获得内容、不动玩家血量。
///
/// ⚠ 只支持**占被动槽**的饰品（<see cref="UpgradeCategory.AttackPassive"/> / <see cref="UpgradeCategory.TimedPassive"/>）。
/// 机制型条目（地刺 / 击退波 / 聚拢波 / 反伤…）是 `Numeric`，效果压进 `_statLayers` / `_executors`，
/// **没有可逆的减法**（层叠混合 / 乘算），硬拆会把数值算错 ⇒ 明确拒绝，返回 false。
///
/// ⚠ 撤回等级不会回滚：拆下再装上是从 Lv.1 重新开始（换装是「换一件」，不是「换个位置」）。
/// </summary>
public partial class UpgradeEffectManager
{
    /// <summary>
    /// 拆下一件饰品：注销行为模块 + 移出已应用表 + 从玩家持有列表移除 + 释放一个被动槽。
    /// </summary>
    /// <returns>拆下成功返回 true；没持有 / 不支持拆卸返回 false。</returns>
    public bool RemoveUpgrade(UpgradeDefinition def)
    {
        if (def == null) return false;
        if (!_appliedUpgrades.TryGetValue(def.upgradeId, out int level) || level <= 0) return false;

        bool occupiesSlot = def.category == UpgradeCategory.AttackPassive
                         || def.category == UpgradeCategory.TimedPassive;
        if (!occupiesSlot)
        {
            Debug.LogWarning($"[UpgradeEffectManager] {def.upgradeId} 不是占槽饰品（category={def.category}），不支持拆卸");
            return false;
        }

        // ① 行为型效果清理（与 ResetAll 的逐条清理同口径：先让执行器自己 Remove）
        foreach (var exec in _executors.Values)
            exec.Remove(def.upgradeId);

        // ② 注销触发模块
        if (def.category == UpgradeCategory.TimedPassive)
            TimedPassiveModule.Instance?.Unregister(def.upgradeId);
        else
            PassiveTriggerModule.Instance?.Unregister(def.upgradeId);

        // ③ 蓄力冲击波这类「记在管理器字段上」的特例要一起清
        if (_chargeHitShockwaveDefinition == def)
        {
            _chargeHitShockwaveDefinition = null;
            _chargeHitShockwaveLevel = 0;
            _chargeHitShockwaveHitCount = 0;
        }

        // ④ 已应用表 + 槽位计数
        _appliedUpgrades.Remove(def.upgradeId);
        _passiveSlotUsers = Mathf.Max(0, _passiveSlotUsers - 1);

        // ⑤ 玩家持有列表（UI 的被动排 / 饰品排就是读这份，删掉即刷新）
        var ps = PlayerState.Instance;
        if (ps != null)
        {
            for (int i = ps.acquiredUpgrades.Count - 1; i >= 0; i--)
            {
                var u = ps.acquiredUpgrades[i];
                if (u != null && u.definition == def) ps.acquiredUpgrades.RemoveAt(i);
            }
        }

        OnUpgradesReset?.Invoke();
        Debug.Log($"[UpgradeEffectManager] 拆下饰品 {def.displayName}（Lv.{level}），剩余被动槽占用 {_passiveSlotUsers}");
        return true;
    }
}
