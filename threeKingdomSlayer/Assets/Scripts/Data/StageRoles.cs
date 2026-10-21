using System;

/// <summary>
/// **角色契约的装配点**：流程层（<c>StageController</c>）在启动时登记自己，
/// 内核只从这里取接口 —— 不再点名流程类 ✅。
///
/// 2026-10-22（asmdef 分层第八批）：内核里 16 处 <c>StageController.Instance</c> 换成
/// <c>StageRoles.BattleState / StageRoles.Formation</c> ✅
/// —— 那些地方**本来就已经用接口**（<see cref="IStageBattleState"/> / <see cref="IStageFormation"/> ✅），
/// 只是「取实例」这一步把流程类拉进了内核 ✅。
/// </summary>
public static class StageRoles
{
    /// <summary>关卡战斗状态（战斗 / 奖励等待 / 关卡配置 / 状态事件）。</summary>
    public static IStageBattleState BattleState { get; set; }

    /// <summary>关卡阵型信息（排偏移 / 排间距 / 可见排数 / 冲锋延迟）。</summary>
    public static IStageFormation Formation { get; set; }

    /// <summary>清空登记（场景重载 / 退出战斗时用）。</summary>
    public static void Clear()
    {
        BattleState = null;
        Formation = null;
    }
}

/// <summary>
/// **道具被破坏的通知端口**：道具在战斗内核里被破坏 ✅，
/// 但「更新路线存档」是流程层的事 ✅ → 用端口通知，不直接点名流程类 ✅。
/// </summary>
public interface IPropDestroyNotifier
{
    /// <summary>通知流程层：有一个可破坏道具被破坏了（需要更新存档快照）。</summary>
    void NotifyPropDestroyed();
}

/// <summary>
/// 道具破坏通知的**装配点**（流程层登记自己）。
/// </summary>
public static class PropDestroyNotifiers
{
    /// <summary>当前的接收方（没有就跳过通知）。</summary>
    public static IPropDestroyNotifier Current { get; set; }
}
