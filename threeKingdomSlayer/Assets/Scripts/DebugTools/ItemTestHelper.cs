using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 调试工具：关卡开始时自动向 ItemInventory 注入指定道具。
/// 挂载到任意场景 GameObject 上（建议 StageController 或独立 Debug 对象）。
///
/// 使用 Update 等待关卡开始（ResetPlayer 之后）再注入，
/// 避免被 StageController.StartStage() → ResetPlayer() 覆盖。
/// </summary>
public class ItemTestHelper : MonoBehaviour
{
    [Serializable]
    public struct TestItemEntry
    {
        [Tooltip("道具 UpgradeDefinition（gestureId != null 的道具类）")]
        public UpgradeDefinition itemDef;
        [Tooltip("注入次数（每次调用 AddItem 叠加 def.useCount）")]
        [Min(1)] public int count;
    }

    [Header("测试规则")]
    [Tooltip("模拟局外解锁同类道具堆叠能力。仅在本局注入道具前应用。")]
    public bool enableSameTypeStacking;

    [Header("测试道具列表")]
    public List<TestItemEntry> testItems = new List<TestItemEntry>();

    private bool _applied;

    private void Update()
    {
        if (_applied) return;
        if (UpgradeEffectManager.Instance == null) return;

        var ps = PlayerState.Instance;
        if (ps == null || ps.stageState != StageState.InProgress) return;

        _applied = true;
        if (ItemInventory.Instance != null)
            ItemInventory.Instance.SetSameTypeStackingForNextRun(enableSameTypeStacking);

        foreach (var entry in testItems)
        {
            if (entry.itemDef == null || entry.count <= 0) continue;

            for (int i = 0; i < entry.count; i++)
            {
                UpgradeEffectManager.Instance.ApplyUpgrade(entry.itemDef);
            }
            Debug.Log($"[ItemTestHelper] 注入道具: {entry.itemDef.displayName} x{entry.count}");
        }
    }
}
