/// <summary>
/// 关卡**阵型信息**（只读）。
///
/// 敌人等战斗侧只需要「阵型偏移 + 当前关卡配置」这几项，历史上却直接摸
/// <c>StageController.Instance</c>（50 个 public 成员的全局门面）。把这份只读契约单独声明出来，
/// 战斗侧就只依赖这 5 个成员；将来门面拆成多个类，战斗侧一行都不用改。
/// </summary>
public interface IStageFormation
{
    /// <summary>当前关卡配置（可为 null）。</summary>
    StageConfig StageConfig { get; }

    /// <summary>某列某排的 X 轴阵型偏移。</summary>
    float GetFormationOffset(int columnIndex, int rowIndex);

    /// <summary>排间距。</summary>
    float GetRowSpacing();

    /// <summary>阵型整体 Z 轴偏移。</summary>
    float GetFormationOffsetZ();

    /// <summary>可见排数上限。</summary>
    int GetMaxVisibleRows();

    /// <summary>补齐规则（按列 / 按排）。</summary>
    FillUpRule GetFillUpRule();

    /// <summary>冲锋移动延迟。</summary>
    float GetRushMoveDelay();

    /// <summary>各排的透明度系数。</summary>
    float[] GetRowAlphaFactors();
}

/// <summary>
/// 关卡**战斗状态**（只读 + 状态事件）。
///
/// 消费方只要「现在是不是在打 / 在等奖励 / 关卡是否进行中」以及关卡配置，
/// 不需要知道 StageController 这个门面长什么样。
/// 历史上这些查询散在各处直接摸 `StageController.Instance`，现改为只依赖本接口。
/// </summary>
public interface IStageBattleState
{
    /// <summary>是否处于路线战斗进行中。</summary>
    bool IsRouteCombatActive { get; }

    /// <summary>是否在等奖励选择（三选一）。</summary>
    bool IsRouteRewardWaiting { get; }

    /// <summary>关卡是否处于进行中（StageState.InProgress）。</summary>
    bool IsStageInProgress { get; }

    /// <summary>当前关卡配置（可为 null）。</summary>
    StageConfig StageConfig { get; }

    /// <summary>关卡状态变化。</summary>
    event System.Action<StageState> StageStateChanged;

    /// <summary>关卡胜利。</summary>
    event System.Action StageVictory;
}

/// <summary>
/// 关卡**流程控制**（UI 侧）：战败是否允许重来、是否压掉战败面板、当前卷轴路线、返回落地页。
///
/// 与 <see cref="IStageFormation"/> 一样是角色的切分：BattleHUD 只需要这几项，
/// 不该为了「显示战败面板」就依赖整个关卡门面。
/// </summary>
public interface IStageFlowControl
{
    /// <summary>战败后是否禁止「重来」（教学关为 true，只保留返回）。教学关导演会写这个开关。</summary>
    bool BlockRestartOnDefeat { get; set; }

    /// <summary>是否压掉战败面板（脚本化演出用）。</summary>
    bool SuppressDefeatUI { get; }

    /// <summary>当前卷轴路线配置（为空 = 不在卷轴路线流程里）。</summary>
    FakeRouteStageConfig RouteConfig { get; }

    /// <summary>当前关卡配置（进度条注入用；可为 null）。</summary>
    StageConfig StageConfig { get; }

    /// <summary>关卡是否处于进行中。</summary>
    bool IsStageInProgress { get; }

    /// <summary>重开当前关卡（战败后的「重来」）。</summary>
    void RestartStage();

    /// <summary>返回落地页（重载 Battle 场景）。</summary>
    void GoToMainMenu();
}
