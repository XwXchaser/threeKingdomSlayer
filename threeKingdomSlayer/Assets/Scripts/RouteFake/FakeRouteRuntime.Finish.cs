using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
    /// FakeRouteRuntime 的**收尾**：战败处理、整关结算、通关后推进存档里的「当前关卡」。
    ///
    /// 按机制从 <c>FakeRouteRuntime.cs</c> 拆出（2026-10-22，partial 分文件）。
    /// partial class 与 FakeRouteRuntime.cs **同类型**，行为完全一致。
    /// </summary>

public sealed partial class FakeRouteRuntime : MonoBehaviour
{
    public void HandleStageDefeat()
    {
        _generation++;
        _battleActive = false;
        _battleCleared = false;
        SetGameplayInput(false);
        SetPhase(FakeRoutePhase.Defeated);
        if (_flow != null)
        {
            StopCoroutine(_flow);
            _flow = null;
        }
    }

    private IEnumerator FinishRoute()
    {
        if (_routeSettled) yield break;
        _routeSettled = true;
        SetGameplayInput(false);
        SetPhase(FakeRoutePhase.Completed);
        yield return new WaitForSecondsRealtime(0.5f);
        SaveManager.ClearFakeRouteSnapshot(routeConfig.routeId, routeConfig.stageId);
        AdvanceActiveStageAfterClear();
        _battleHost.CompleteFakeRouteStage(routeConfig.clearCoinReward);
    }

    /// <summary>
    /// 通关后把存档「当前关卡」推进到可选列表里的下一条路线（教学关 → 正式关）。
    ///
    /// 落地页开机就是按这个值自举的，不推进的话打完教学关回到落地页还是教学关，
    /// 玩家必须自己去选关页手动切。已是最后一条时保持不变（重复挑战不倒退）。
    /// </summary>
    private void AdvanceActiveStageAfterClear()
    {
        if (routeConfig == null) return;
        var scm = RouteCatalog.Resolve();
        if (scm == null) return;
        var routes = scm.GetSelectableRoutes();
        int index = -1;
        for (int i = 0; i < routes.Count; i++)
            if (routes[i] != null && routes[i].stageId == routeConfig.stageId) { index = i; break; }
        if (index < 0 || index + 1 >= routes.Count) return;
        var next = routes[index + 1];
        if (next == null) return;
        SaveManager.SetActiveFakeRouteStage(next.stageId);
        Debug.Log("[FakeRoute] 通关推进当前关卡: " + routeConfig.stageName + " → " + next.stageName);
    }
}