using System.Collections.Generic;

/// <summary>
/// **局内快照分片**：一局游戏的续玩快照（线性关卡 <c>routeStageSnapshots</c> +
/// 卷轴路线 <c>fakeRouteSnapshots</c> + 当前进行中的路线 <c>activeFakeRouteStageId</c>）。
///
/// 快照里存着「当前节点 / 血量 / 等级 / 升级 / 道具破坏状态」✅ —— 续玩与重进节点都读它 ✅。
/// </summary>
public sealed class RunSnapshotSaveSection : ISaveSection
{
    /// <summary>卷轴快照的架构版本标记（旧快照与新架构不混用 ✅）。</summary>
    private const string FakeRouteArchitectureId = "fake-route-v1";

    public string Id => "run-snapshot";

    public void Migrate(SaveData data)
    {
        if (data.routeStageSnapshots == null) data.routeStageSnapshots = new List<RouteStageSaveSnapshot>();
        if (data.fakeRouteSnapshots == null) data.fakeRouteSnapshots = new List<FakeRouteStageSaveSnapshot>();
    }

    // ── 线性关卡快照 ──

    public static RouteStageSaveSnapshot GetRouteStageSnapshot(SaveData data, int stageId)
    {
        var list = GetRouteStageSnapshots(data);
        for (int i = 0; i < list.Count; i++)
            if (list[i] != null && list[i].stageId == stageId)
                return list[i];
        return null;
    }

    public static void SaveRouteStageSnapshot(SaveData data, RouteStageSaveSnapshot snapshot)
    {
        if (snapshot == null) return;
        var list = GetRouteStageSnapshots(data);
        for (int i = list.Count - 1; i >= 0; i--)
            if (list[i] == null || list[i].stageId == snapshot.stageId)
                list.RemoveAt(i);
        list.Add(snapshot);
    }

    public static bool ClearRouteStageSnapshot(SaveData data, int stageId)
    {
        var list = GetRouteStageSnapshots(data);
        bool changed = false;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i] != null && list[i].stageId == stageId)
            {
                list.RemoveAt(i);
                changed = true;
            }
        }
        return changed;
    }

    // ── 卷轴路线快照 ──

    public static FakeRouteStageSaveSnapshot GetFakeRouteSnapshot(SaveData data, string routeId, int stageId)
    {
        var list = GetFakeRouteSnapshots(data);
        for (int i = 0; i < list.Count; i++)
        {
            var snapshot = list[i];
            if (snapshot != null && snapshot.routeId == routeId && snapshot.stageId == stageId
                && snapshot.routeArchitectureId == FakeRouteArchitectureId) return snapshot;
        }
        return null;
    }

    public static void SaveFakeRouteSnapshot(SaveData data, FakeRouteStageSaveSnapshot snapshot)
    {
        if (snapshot == null || string.IsNullOrEmpty(snapshot.routeId)) return;
        var list = GetFakeRouteSnapshots(data);
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var old = list[i];
            if (old == null || (old.routeId == snapshot.routeId && old.stageId == snapshot.stageId))
                list.RemoveAt(i);
        }
        list.Add(snapshot);
    }

    public static bool ClearFakeRouteSnapshot(SaveData data, string routeId, int stageId)
    {
        var list = GetFakeRouteSnapshots(data);
        bool changed = false;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var snapshot = list[i];
            if (snapshot != null && snapshot.routeId == routeId && snapshot.stageId == stageId)
            {
                list.RemoveAt(i);
                changed = true;
            }
        }
        return changed;
    }

    public static bool ClearAllFakeRouteSnapshots(SaveData data)
    {
        var list = GetFakeRouteSnapshots(data);
        if (list.Count == 0) return false;
        list.Clear();
        return true;
    }

    // ── 当前进行中的路线 ──

    public static void SetActiveFakeRouteStage(SaveData data, int stageId) => data.activeFakeRouteStageId = stageId;

    public static int GetActiveFakeRouteStageId(SaveData data) => data.activeFakeRouteStageId;

    public static void ClearActiveFakeRouteStage(SaveData data) => data.activeFakeRouteStageId = -1;

    // ── 列表访问（补空 ✅）──

    private static List<RouteStageSaveSnapshot> GetRouteStageSnapshots(SaveData data)
    {
        if (data.routeStageSnapshots == null) data.routeStageSnapshots = new List<RouteStageSaveSnapshot>();
        return data.routeStageSnapshots;
    }

    private static List<FakeRouteStageSaveSnapshot> GetFakeRouteSnapshots(SaveData data)
    {
        if (data.fakeRouteSnapshots == null) data.fakeRouteSnapshots = new List<FakeRouteStageSaveSnapshot>();
        return data.fakeRouteSnapshots;
    }
}
