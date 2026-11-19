using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// **装备宝箱掉落规则**（2026-11-17 用户定口径后重写）。
///
/// 用户的原话：「我说精英敌人才掉宝箱给装备饰品」+「我自己打标记配」+「精英和 boss 都是 100」。
///
/// ⇒ **谁算精英由资产上的标记决定**（<see cref="Enemy.dropsEquipment"/>，策划在敌人预制体上勾），
/// 代码里**不再维护任何敌人 ID 名单**（名单在代码里用户看不见、改不动，
/// 而且上一轮就是这么把 N1 的主力杂兵 1012/1013 误算成精英的）。
///
/// 掉落链：
/// ```
/// 敌人死亡 → 判定（Boss 恒掉 / 精英标记）→ 世界里掉一个宝箱
///   → 宝箱飞向玩家 → 拾取 → 入队一次「装备三选一」→ **波末统一弹**（与升级三选一同一队列）
/// ```
/// </summary>
public static class EliteDropRules
{
    /// <summary>精英（打了标记）掉落宝箱的概率。2026-11-17 用户定：**100%**。</summary>
    public const float EliteDropChance = 1f;

    /// <summary>Boss 掉落宝箱的概率。2026-11-17 用户定：**100%**。</summary>
    public const float BossDropChance = 1f;

    /// <summary>这个敌人是不是精英（= 预制体上勾了「掉宝箱」标记）。</summary>
    public static bool IsElite(Enemy enemy)
    {
        return enemy != null && enemy.dropsEquipment;
    }

    /// <summary>是不是 Boss。</summary>
    public static bool IsBoss(Enemy enemy)
    {
        return enemy != null && enemy.isBoss;
    }

    /// <summary>
    /// 这个敌人死亡时**是否掉宝箱**：
    /// Boss 恒掉；精英（打了标记）按 <see cref="EliteDropChance"/>（当前 = 100%）。
    /// </summary>
    public static bool ShouldDropEquipment(Enemy enemy)
    {
        if (enemy == null) return false;
        if (enemy.isBoss) return Random.value < BossDropChance;
        if (!enemy.dropsEquipment) return false;
        return Random.value < EliteDropChance;
    }
}
