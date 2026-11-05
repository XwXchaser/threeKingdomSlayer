using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 存档数据 — JSON序列化到 Application.persistentDataPath
/// 道具系统：props 列表管理所有道具（铜钱/钥匙等），coinCount 保留向后兼容
///
/// 2026-10-22（asmdef 分层第二批）：从 <c>Assets/Scripts/Core/SaveManager.cs</c> 拆到数据层
/// —— 存档**数据模型**是纯数据（序列化用），服务（<c>SaveManager</c>）才是行为；
/// 两者分开后，服务层不依赖任何战斗 / 流程代码。
/// </summary>
[Serializable]
public class SaveData
{
    public List<int> clearedStageIds = new List<int>();
    public List<PropData> props = new List<PropData>();
    [Obsolete("使用 props.GetPropAmount(PropType.Coin) 替代")]
    public int coinCount;
    public bool tutorialCompleted;
    public List<string> completedTutorialDialogueIds = new List<string>();
    [Tooltip("已教完的**教学关手势拍** id（每教完一拍就写，**与教学关通不通无关**）。\n旧档缺失时由 TutorialSaveSection 补空列表")]
    public List<string> taughtTutorialBeats = new List<string>();
    public List<RouteStageSaveSnapshot> routeStageSnapshots = new List<RouteStageSaveSnapshot>();
    public List<FakeRouteStageSaveSnapshot> fakeRouteSnapshots = new List<FakeRouteStageSaveSnapshot>();
    public int activeFakeRouteStageId = -1;
    [Tooltip("已解锁的天赋节点 id（TalentNodeConfig.nodeId）。旧档缺失时 MigrateIfNeeded 会补空列表")]
    public List<string> unlockedTalents = new List<string>();
    [Tooltip("已领取过的局外补给实例 id（轮次+点位）。旧档缺失时由 SupplySaveSection 补空列表")]
    public List<string> claimedSupplyIds = new List<string>();
    [Tooltip("已获得的秘籍 id（ManualDefinition.ManualId）。旧档缺失时由 ManualSaveSection 补空列表")]
    public List<string> unlockedManuals = new List<string>();
    [Tooltip("已教过的招式引导 id（TalentNodeConfig.nodeId）。旧档缺失时由 MoveGuideSaveSection 补空列表")]
    public List<string> taughtMoveGuides = new List<string>();
    [Tooltip("**天赋页教学**是否已教过（2026-11-04）。第一次打开天赋页会硬性教「如何解锁天赋」；教完（真的解锁了一个节点）才写 true → 同一存档只教一次。老档缺失时默认 false")]
    public bool taughtTalentPanel = false;
    [Tooltip("**「点开始关卡」的主菜单引导**是否已教过（2026-11-04）。玩家点过开始关卡（或正式进入战斗）后写 true → 同一段教程里不再重复出现")]
    public bool taughtStartStage = false;

    /// <summary>
    /// 获取铜钱数量（优先从 props 列表读取，兼容旧存档的 coinCount 字段）
    /// </summary>
    public int GetCoinCount()
    {
        int fromProps = props.GetPropAmount(PropType.Coin);
        return fromProps > 0 || props.Count > 0 ? fromProps : coinCount;
    }

    /// <summary>
    /// 迁移旧存档：各分片各自补齐 / 迁移自己的字段（2026-10-23 存档分片：
    /// 逻辑从模型本体搬到 <c>SaveSections</c> 登记的各分片 ✅，模型只留入口 ✅）。
    /// </summary>
    public void MigrateIfNeeded()
    {
        SaveSections.MigrateAll(this);
    }
}

/// <summary>
/// 单个关卡的存档快照（线性关卡路线用）。
/// </summary>
[Serializable]
public sealed class RouteStageSaveSnapshot
{
    public int stageId;
    public string currentNodeId;
    public float currentHealth;
    public int currentRevives;
    public int currentLevel;
    public float currentExp;
    public List<RouteNodeBattleSaveState> nodeStates = new List<RouteNodeBattleSaveState>();
    public List<RouteUpgradeSaveState> upgrades = new List<RouteUpgradeSaveState>();
}

/// <summary>
/// 节点战斗进度（已完成的入场序号）。
/// </summary>
[Serializable]
public sealed class RouteNodeBattleSaveState
{
    public string nodeId;
    public List<int> completedEntryIndices = new List<int>();
}

/// <summary>
/// 已应用的升级及其等级。
/// </summary>
[Serializable]
public sealed class RouteUpgradeSaveState
{
    public string upgradeId;
    public int level;
}
