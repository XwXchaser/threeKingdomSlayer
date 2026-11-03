using UnityEngine;

/// <summary>
/// **秘籍定义**：局外**永久**解锁物（技巧类）。
///
/// 与局内升级（<see cref="UpgradeDefinition"/>）的区别：
///   • 局内升级 = 三选一里拿到的、要占槽的、有等级的；
///   • **秘籍 = 掉落拿到的、一次获得永久生效、不占任何槽、不走 <c>ItemInventory</c>**（那是局内道具栏）。
///
/// 复用现有通道（不新建系统）：
///   • 掉落 → 放进现成的加权掉落池 <c>DropItemPoolConfig</c>（<c>WeightedDropItem.item</c> 指向本资产）；
///   • 拾取 → <see cref="ManualDropSystem"/> 生成可见掉落物（照抄 <c>ExpGemManager</c> 范式）；
///   • 解锁 → 写存档 <c>SaveData.unlockedManuals</c>（<see cref="ManualSaveSection"/>，与 TalentSaveSection 同构）。
/// </summary>
[CreateAssetMenu(fileName = "ManualDefinition", menuName = "一夫当关/秘籍定义")]
public class ManualDefinition : UpgradeDefinition
{
    [Header("秘籍")]
    [Tooltip("存档用的稳定 id（写进 SaveData.unlockedManuals）。**留空 = 退回 upgradeId**。\n" +
             "改名会让老档丢失这条记录（同天赋节点的规则）。")]
    public string manualId = "";

    [Tooltip("解锁的招式（可选）：获得即永久可用该招式。留空 = 不解锁招式。")]
    public AttackSkillConfig moveConfig;

    /// <summary>存档里用的 id（manualId 优先，留空退回 upgradeId）。</summary>
    public string ManualId => string.IsNullOrEmpty(manualId) ? upgradeId : manualId;
}
