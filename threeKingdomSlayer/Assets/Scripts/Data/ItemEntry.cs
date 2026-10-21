using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 道具栏条目（局内道具的运行时表示）。
///
/// 2026-10-22（asmdef 分层第四批）：从 <c>ItemInventory.ItemEntry</c> 提成顶层类型并下沉到契约层
/// —— 道具弃置弹窗（界面层）也要用它，而界面层不该反向依赖业务层；
/// 内核与界面通过 <c>IItemDiscardPrompt</c> 契约交换它。
/// </summary>
public sealed class ItemEntry
{
    /// <summary>回血道具的手势 id（固定值，不随配置变）。</summary>
    public const string HealthPotionGestureId = "health_potion";

    public int id;
    public UpgradeDefinition definition;
    public int remainingUses;
    public bool isPotion;
    public string GestureId => isPotion ? HealthPotionGestureId : definition != null ? definition.gestureId : null;
}
