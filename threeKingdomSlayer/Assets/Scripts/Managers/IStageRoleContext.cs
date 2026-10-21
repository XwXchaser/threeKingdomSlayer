using System.Collections;

/// <summary>
/// 关卡门面向三个角色实现（<c>StageRunFlow</c> / <c>StageCombatStaging</c> / <c>StageRewardGate</c>）
/// 提供的**环境**。
///
/// 沿用 <see cref="StageRouteBattleHost.IContext"/> 的做法：实现方（<c>StageController</c>）
/// 持有 MonoBehaviour 身份、Inspector 引用、状态与事件广播；三个角色类只拿到这个窄接口，
/// 不认识 <c>StageController</c> 这个门面本身。
///
/// 继承 <see cref="StageRouteBattleHost.IContext"/>：路线战斗宿主需要的成员（当前配置 /
/// 状态读写 / 波次·敌人·玩家·对象池）正好是这个接口的子集。
/// </summary>
public interface IStageRoleContext : StageRouteBattleHost.IContext
{
    /// <summary>当前卷轴路线配置（为空 = 不在卷轴路线流程里）。</summary>
    FakeRouteStageConfig RouteConfig { get; set; }

    /// <summary>宿主是否会在场景启动时自动开战。</summary>
    bool AutoStartStage { get; set; }

    /// <summary>战败后是否禁止「重来」。</summary>
    bool BlockRestartOnDefeat { get; set; }

    /// <summary>是否压掉战败面板。</summary>
    bool SuppressDefeatUI { get; }

    /// <summary>战斗场景名（回落地页 / 切战斗场景用）。</summary>
    string BattleSceneName { get; }

    /// <summary>当前路线是否已结算（结算入口幂等保护）。</summary>
    bool RouteStageSettled { get; set; }

    /// <summary>路线战斗是否已开启（节点战斗开始 → 结算前）。</summary>
    bool IsRouteBattleRunning { get; }

    /// <summary>是否正在等待奖励选择。</summary>
    bool IsRouteRewardWaiting { get; }

    /// <summary>预热敌人对象池。</summary>
    void PrewarmEnemyPools();

    /// <summary>结算本局铜钱到存档（幂等）。</summary>
    void SettleCoins();

    /// <summary>在宿主（MonoBehaviour）上启动协程——纯 C# 类不能自己 StartCoroutine。</summary>
    void RunCoroutine(IEnumerator routine);

    /// <summary>同步关卡状态但不广播事件（玩家侧状态回传时用）。</summary>
    void SyncStageStateSilently(StageState state);

    /// <summary>广播关卡状态变化。</summary>
    void RaiseStageStateChanged(StageState state);

    /// <summary>广播关卡胜利。</summary>
    void RaiseStageVictory();

    /// <summary>广播玩家阵亡（战败）。</summary>
    void RaiseStageDefeat();

    /// <summary>广播战斗节点清空。</summary>
    void RaiseCombatNodeCleared();

    /// <summary>广播路线战斗结束（清场）。</summary>
    void RaiseRouteBattleCompleted();

    /// <summary>战败事件（订阅方走角色接口）。</summary>
    event System.Action StageDefeat;

    /// <summary>战斗节点清空事件（订阅方走角色接口）。</summary>
    event System.Action CombatNodeCleared;

    /// <summary>路线战斗结束事件（订阅方走角色接口）。</summary>
    event System.Action RouteBattleCompleted;
}
