using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
    /// FakeRouteRuntime 的**分支选择**：玩家选出口 → 记录选择历史 → 播放旅行 → 进入目标节点。
    ///
    /// 按机制从 <c>FakeRouteRuntime.cs</c> 拆出（2026-10-22，partial 分文件）。
    /// partial class 与 FakeRouteRuntime.cs **同类型**，行为完全一致。
    /// </summary>

public sealed partial class FakeRouteRuntime : MonoBehaviour
{
    public bool TrySelectChoice(string choiceId)
    {
        if (Phase != FakeRoutePhase.ChoosingRoute || _currentNode == null || string.IsNullOrEmpty(choiceId)) return false;
        if (_currentNode.outgoingChoices == null) return false;
        for (int i = 0; i < _currentNode.outgoingChoices.Count; i++)
        {
            var choice = _currentNode.outgoingChoices[i];
            if (choice != null && choice.choiceId == choiceId)
            {
                if (choice.targetNode == null || !ContainsNode(choice.targetNode)) return false;
                if (_visitedNodes.Contains(choice.targetNode))
                {
                    Debug.LogWarning("[FakeRoute] 禁止回访：目标节点已访问 " + choice.targetNode.nodeId);
                    return false;
                }
                SetPhase(FakeRoutePhase.FakeMoving);
                RouteChoicePanel.HideCurrent();
                _choiceHistory.Add(new FakeRouteChoiceSaveState
                {
                    sourceNodeId = _currentNode.nodeId,
                    choiceId = choice.choiceId,
                    targetNodeId = choice.targetNode.nodeId
                });
                SetGameplayInput(false);
                _battleHost.SetRouteTravelState();
                _flow = StartCoroutine(PlayPlaceholderAndEnter(choice, _generation));
                return true;
            }
        }
        return false;
    }

    private IEnumerator PlayPlaceholderAndEnter(FakeRouteChoiceConfig choice, int generation)
    {
        if (movementPresenter != null)
            yield return movementPresenter.PlayChoiceTravel(choice, _currentNode, choice.targetNode, () => generation == _generation && Phase == FakeRoutePhase.FakeMoving);
        else
        {
            // 没有表现层时无法播放旅行：报告并直接提交目标节点，不静默改变节点语义。
            Debug.LogError("[FakeRoute] 表现层缺失，无法播放出口旅行: " + (choice != null ? choice.choiceId : "null"));
        }
        if (generation != _generation || Phase == FakeRoutePhase.Defeated) yield break;
        yield return EnterNode(choice.targetNode);
    }
    private void ShowRouteChoice()
    {
        if (_currentNode == null || _currentNode.outgoingChoices == null) return;
        var options = new List<RouteChoiceOption>();
        for (int i = 0; i < _currentNode.outgoingChoices.Count; i++)
        {
            var choice = _currentNode.outgoingChoices[i];
            if (choice == null || choice.targetNode == null) continue;
            options.Add(new RouteChoiceOption
            {
                id = choice.choiceId,
                label = choice.displayName + " → " + choice.targetNode.displayName,
                layout = choice.layout
            });
        }
        RouteChoicePanel.Show(choicePanelPrefab, _currentNode.displayName, options, id => TrySelectChoice(id));
    }
}