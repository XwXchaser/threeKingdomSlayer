using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// **道具弃置弹窗的适配器**：把界面层的静态弹窗包装成契约层的 <see cref="IItemDiscardPrompt"/>，
/// 让内核（<c>UpgradeEffectManager</c> / <c>EnemyManager</c>）不必引用界面类。
///
/// 2026-10-22（asmdef 分层第四批）：内核 → 界面硬引用接口化的一环。
/// 登记在应用启动后自动完成（不需要往场景里加对象 ✓）。
/// </summary>
public sealed class ItemDiscardPromptAdapter : IItemDiscardPrompt
{
    public static readonly IItemDiscardPrompt Instance = new ItemDiscardPromptAdapter();

    /// <summary>弹窗是否正在显示。</summary>
    public bool IsShowing => ItemDiscardPopup.IsShowing;

    /// <summary>弹出弃置弹窗（内部会创建弹窗对象）。</summary>
    public void Show(List<ItemEntry> entries, UpgradeDefinition newItem, Action<ItemDiscardResult> onComplete)
        => ItemDiscardPopup.Show(entries, newItem, onComplete);

    /// <summary>应用启动后自动登记（界面层登记、内核层使用，依赖方向朝下）。</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoRegister()
    {
        CombatPresenters.ItemDiscard = Instance;
    }
}
