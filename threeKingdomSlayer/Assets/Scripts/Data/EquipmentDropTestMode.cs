/// <summary>
/// **装备掉落测试开关**（2026-11-25，临时验收用）。
///
/// 由 <c>EndlessRunController</c> 在「第一圈 N1 / E0」节点战斗期间置 true；
/// <c>WaveSpawner</c> 生成敌人时把 <c>Enemy.dropsEquipment</c> 强制为 true
/// ⇒ 这些敌人 100% 掉装备宝箱，方便验收空槽 / 同款替换 / 满槽换装。
///
/// ⚠ 只影响**本次测试节点**的掉落，不改敌人预制体；节点结束 / 本局结束 / 控制器销毁都会清掉。
/// 默认 false，正式流程不读取。
/// </summary>
public static class EquipmentDropTestMode
{
    /// <summary>是否把所有新生成的敌人都按精英处理（掉宝箱）。</summary>
    public static bool ForceAllDrops;
}
