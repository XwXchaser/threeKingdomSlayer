using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// **战斗 HUD 端口**：内核要改血条颜色时只认这个接口，不认 <c>BattleHUD</c>。
///
/// 2026-10-22（asmdef 分层第四批）：内核 → 界面的硬引用接口化。
/// 之前 <c>UltimateEffect_Berserk</c> 用 <c>FindObjectOfType&lt;BattleHUD&gt;()</c> 找 HUD
/// （「按类型找对象」正是反模式），现在由界面层把自己登记到 <see cref="CombatPresenters"/>。
/// </summary>
public interface ICombatHud
{
    /// <summary>把血条染成指定颜色（狂化等状态）。</summary>
    void SetHealthBarColor(Color color);

    /// <summary>恢复血条原本颜色。</summary>
    void ResetHealthBarColor();
}

/// <summary>
/// **对话模态端口**：内核问「对话是否正在挡路」（挡住技能激活 / 交互）。
/// </summary>
public interface IDialogueModal
{
    /// <summary>对话正在播放或仍在拦输入帧 → 视为挡路。</summary>
    bool IsInteractionBlocked { get; }
}

/// <summary>
/// **道具弃置弹窗端口**：道具栏满时内核要弹窗让玩家弃掉一个旧道具。
/// </summary>
public interface IItemDiscardPrompt
{
    /// <summary>弹窗是否正在显示。</summary>
    bool IsShowing { get; }

    /// <summary>弹窗并等待玩家选择（回调在玩家做出选择后触发）。</summary>
    void Show(List<ItemEntry> entries, UpgradeDefinition newItem, Action<ItemDiscardResult> onComplete);
}

/// <summary>
/// **三选一弹窗端口**：内核（<c>UpgradeChoiceManager</c>）只通过它展示 / 关闭弹窗，
/// 不再直接引用界面组件（<c>UpgradeChoicePopup</c>）。
/// </summary>
public interface IUpgradeChoiceView
{
    /// <summary>展示一组候选升级。</summary>
    void ShowChoices(List<UpgradeDefinition> choices);

    /// <summary>关闭弹窗（动画结束后回调 <paramref name="onDone"/>）。</summary>
    void Dismiss(Action onDone);
}

/// <summary>
/// **伤害跳字端口**：内核只负责「敌人受击了，掉多少血、什么颜色」，
/// 跳字对象的生成 / 回收 / 复用全在界面层。
/// </summary>
public interface IDamageNumberPresenter
{
    /// <summary>在敌人头顶弹一个伤害数字。</summary>
    void Spawn(Vector3 worldPos, float damage, Color? colorOverride, HitFeedbackStrength feedbackStrength);

    /// <summary>清理当前所有活跃的跳字（切波次时用），返回清理数量。</summary>
    int ClearAll();
}

/// <summary>
/// **呈现层装配点**：界面层把自己登记进来，内核只通过接口使用。
///
/// 规则：
///   • 登记发生在**界面层**（<c>Awake</c> 或 <c>RuntimeInitializeOnLoadMethod</c>）✓
///     —— 上层登记、下层使用，依赖方向始终朝下 ✓；
///   • 内核取不到实现时按「没有界面」处理（安全降级 ✓），不直接 new 界面对象 ✓；
///   • 新机制要弹窗 / 改 HUD 时，加自己的端口 + 在界面层登记 ✓，不用改内核 ✗。
/// </summary>
public static class CombatPresenters
{
    /// <summary>战斗 HUD（血条颜色等）。</summary>
    public static ICombatHud Hud { get; set; }

    /// <summary>对话模态（挡路查询）。</summary>
    public static IDialogueModal Dialogue { get; set; }

    /// <summary>道具弃置弹窗。</summary>
    public static IItemDiscardPrompt ItemDiscard { get; set; }

    /// <summary>伤害跳字。</summary>
    public static IDamageNumberPresenter DamageNumbers { get; set; }

    /// <summary>清空登记（场景重载 / 退出战斗时用）。</summary>
    public static void Clear()
    {
        Hud = null;
        Dialogue = null;
        ItemDiscard = null;
        DamageNumbers = null;
    }
}
