using System;

/// <summary>
/// **一局流程**（RunSession 角色）：怎么开打、怎么重开、怎么退出、现在是什么状态、战败后能不能重来。
///
/// 消费者：战败面板（<c>BattleHUD</c>）、暂停菜单（<c>PauseMenuUI</c>）、教学关导演（<c>TutorialStageDirector</c>）。
/// 它们只需要"这一局"这几件事，不该为了显示一个战败面板就依赖整个关卡门面。
///
/// 由 <c>StageController</c> 显式实现：<c>RouteConfig</c> / <c>BlockRestartOnDefeat</c> /
/// <c>SuppressDefeatUI</c> / <c>StageConfig</c> 在它身上原本是 public 字段或字段属性，
/// 用显式实现包一层，字段本身与 Inspector 序列化不受影响。
/// </summary>
public interface IRunFlow
{
    /// <summary>当前关卡状态。</summary>
    StageState CurrentState { get; }

    /// <summary>关卡是否进行中。</summary>
    bool IsStageInProgress { get; }

    /// <summary>是否胜利。</summary>
    bool IsStageVictory { get; }

    /// <summary>是否战败。</summary>
    bool IsStageDefeat { get; }

    /// <summary>战败后是否禁止「重来」（教学关为 true，只保留返回）。教学关导演会写这个开关。</summary>
    bool BlockRestartOnDefeat { get; set; }

    /// <summary>是否压掉战败面板（脚本化演出用）。</summary>
    bool SuppressDefeatUI { get; }

    /// <summary>当前卷轴路线配置（为空 = 不在卷轴路线流程里）。</summary>
    FakeRouteStageConfig RouteConfig { get; set; }

    /// <summary>当前关卡配置。</summary>
    StageConfig StageConfig { get; }

    /// <summary>开始当前关卡。</summary>
    void StartStage();

    /// <summary>重开当前关卡（战败后的「重来」）。</summary>
    void RestartStage();

    /// <summary>返回落地页（重载 Battle 场景）。</summary>
    void GoToMainMenu();

    /// <summary>收尾当前路线战斗（战败 / 退出时清理：停战斗、通知路线运行时）。</summary>
    void CleanupRouteStage(bool unloadScene);

    /// <summary>玩家阵亡（战败）时触发。</summary>
    event Action StageDefeat;
}

/// <summary>
/// **一场战斗的布场与开打**（CombatDomain 角色）：装上节点配置、开打、停战斗、切战斗场景。
///
/// 消费者：路线流程（<c>FakeRouteRuntime</c>，经 <c>IRouteStageHost</c> 合成端口）。
/// 「节点战斗怎么开、怎么停」与「这一局怎么结算」是两种关注点，分开声明才不会互相撑大。
/// </summary>
public interface ICombatStaging
{
    /// <summary>宿主是否会在场景启动时自动开战。路线流程必须强制为 false，由自己驱动。</summary>
    bool AutoStartStage { get; set; }

    /// <summary>是否处于路线战斗进行中。</summary>
    bool IsRouteCombatActive { get; }

    /// <summary>一场路线战斗结束（清场）时触发。</summary>
    event Action RouteBattleCompleted;

    /// <summary>装上当前节点的战斗配置。</summary>
    void SetCurrentNodeBattleConfig(StageConfig config);

    /// <summary>开始某节点的一场战斗。</summary>
    void StartRouteBattle(StageConfig config);

    /// <summary>开始当前路线节点（可顺带重置玩家）。</summary>
    void StartCurrentRouteNode(bool resetPlayer = false);

    /// <summary>停止战斗（进入节点间旅行前）。</summary>
    void StopCombatForRouteTravel();

    /// <summary>进入节点间旅行状态。</summary>
    void SetRouteTravelState();

    /// <summary>回到战斗场景（重新装载 Battle 场景）。</summary>
    void GoToBattleScene();
}

/// <summary>
/// **奖励门控与结算**（RunSession 的奖励段）：现在是不是在等奖励、这一局怎么结算。
///
/// 「等奖励」和「结算」是同一道门：奖励面板打开时输入被禁、战斗不算在跑，
/// 结算则是这道门最后一次通过。
/// </summary>
public interface IRewardGate
{
    /// <summary>是否在等奖励选择（三选一）。</summary>
    bool IsRouteRewardWaiting { get; }

    /// <summary>当前战斗节点清空时触发。</summary>
    event Action CombatNodeCleared;

    /// <summary>进入「等待奖励选择」状态（此时输入禁用、不算战斗）。</summary>
    void SetRouteRewardWaitState();

    /// <summary>当前战斗节点完成。</summary>
    void CompleteCurrentCombatNode();

    /// <summary>整条路线结算（胜利界面、通关铜钱、记录已通关）。</summary>
    void CompleteFakeRouteStage(int clearReward);

    /// <summary>旧版（RouteV2）路线结算。stageId 由调用方给出，避免引用 V2 的配置类型。</summary>
    void CompleteRouteStage(int stageId, int clearReward);
}
