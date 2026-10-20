using System;

/// <summary>
/// 路线流程对「战斗宿主」的唯一要求（端口接口 / Ports &amp; Adapters）。
///
/// 依赖方向：由**使用方**（<see cref="FakeRouteRuntime"/>，路线层）声明自己需要什么，
/// 由**实现方**（<c>StageController</c>，应用层）去实现。这样路线层不再直接认识
/// `StageController` 这个 50 个 public 成员的全局门面，后续换成别的宿主（测试替身、
/// 另一种关卡类型）也不需要改路线流程。
///
/// 由 `StageController` 显式实现：`AutoStartStage` / `RouteConfig` / `RouteBattleCompleted`
/// 在它身上原本是 public 字段，用显式实现包一层，字段本身与 Inspector 序列化不受影响；
/// 四个方法签名已匹配，隐式实现。
///
/// ⚠️ 这只是「把契约写出来」的第一步：`FakeRouteRuntime` 仍持有 `StageController` 的序列化引用
/// （Inspector 拖拽用），但除了取实例以外不再调用它的具体成员。
/// </summary>
public interface IRouteBattleHost
{
    /// <summary>宿主是否会在场景启动时自动开战。路线流程必须强制为 false，由自己驱动。</summary>
    bool AutoStartStage { get; set; }

    /// <summary>当前正在跑的卷轴路线配置（宿主据此判断「现在是不是卷轴路线」）。</summary>
    FakeRouteStageConfig RouteConfig { get; set; }

    /// <summary>一场路线战斗结束（清场）时触发。</summary>
    event Action RouteBattleCompleted;

    /// <summary>开始某节点的一场战斗。</summary>
    void StartRouteBattle(StageConfig config);

    /// <summary>进入「等待奖励选择」状态（此时输入禁用、不算战斗）。</summary>
    void SetRouteRewardWaitState();

    /// <summary>进入节点间旅行状态。</summary>
    void SetRouteTravelState();

    /// <summary>整条路线结算（胜利界面、通关铜钱、记录已通关）。</summary>
    void CompleteFakeRouteStage(int clearReward);

    /// <summary>旧版（RouteV2）路线结算。stageId 由调用方给出，避免引用 V2 的配置类型。</summary>
    void CompleteRouteStage(int stageId, int clearReward);
}
