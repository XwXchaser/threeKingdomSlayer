using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
    /// FakeRouteRuntime 的**节点流程**：进入节点 → 入口对话 → 战斗条目 → 战后演出 → 奖励阻塞判定。
    ///
    /// 按机制从 <c>FakeRouteRuntime.cs</c> 拆出（2026-10-22，partial 分文件）。
    /// partial class 与 FakeRouteRuntime.cs **同类型**，行为完全一致。
    /// 扩展点：节点「什么算完成 / 要不要收尾整关」走完成策略注册表（IRouteNodeCompletionPolicy），
    /// 不要在这里加按节点类型的分支。
    /// </summary>

public sealed partial class FakeRouteRuntime : MonoBehaviour
{
    private IEnumerator EnterNode(FakeRouteNodeConfig node)
    {
        if (node == null || !ContainsNode(node))
        {
            Debug.LogError("[FakeRoute] 目标节点无效");
            SetPhase(FakeRoutePhase.None);
            yield break;
        }

        _currentNode = node;
        _visitedNodes.Add(node);
        SetPhase(FakeRoutePhase.EnteringNode);
        movementPresenter?.ApplyNodeVisuals(node);
        yield return PlayEntryDialogue(node.entryDialogue);
        if (Phase == FakeRoutePhase.Defeated) yield break;
        if (node.savePoint && (!_restoredFromCheckpoint || node.nodeId != _restoredCheckpointNodeId))
            SaveCheckpoint();
        yield return RunBattleEntries();
        if (Phase == FakeRoutePhase.Defeated) yield break;

        if (ShouldFinishRouteAtNode(node))
        {
            if (HasUncompletedBattleEntry(node))
            {
                Debug.LogError("[FakeRoute] 终点节点仍有未完成BattleEntry: " + node.nodeId);
                SetPhase(FakeRoutePhase.None);
                yield break;
            }
            yield return FinishRoute();
            yield break;
        }

        if (node.outgoingChoices == null || node.outgoingChoices.Count == 0)
        {
            Debug.LogError("[FakeRoute] 非终点节点没有出口: " + node.nodeId);
            SetPhase(FakeRoutePhase.None);
            yield break;
        }

        SetPhase(FakeRoutePhase.ChoosingRoute);
        ShowRouteChoice();
    }

    private IEnumerator PlayPostBattleSequence(FakeRouteNodeConfig node)
    {
        if (node == null || (node.postBattleDialogue == null && node.postBattlePresentation == null)) yield break;
        int generation = _generation;
        RouteChoicePanel.HideCurrent();
        SetGameplayInput(false);
        yield return PlayEntryDialogue(node.postBattleDialogue);
        if (generation != _generation || Phase == FakeRoutePhase.Defeated) yield break;
        if (node.postBattlePresentation == null || movementPresenter == null) yield break;

        SetPhase(FakeRoutePhase.EnteringNode);
        SetGameplayInput(false);
        stageController?.SetRouteTravelState();
        yield return movementPresenter.PlayNodePresentation(node.postBattlePresentation,
            () => generation == _generation && Phase != FakeRoutePhase.Defeated);
        if (generation == _generation && Phase != FakeRoutePhase.Defeated)
        {
            // A held video frame is the node's new environment (e.g. dawn).
            // Only presentations without frame retention should restore a static background.
            movementPresenter.ApplyNodeVisuals(node);
            Debug.Log("[FakeRoute] post-battle presentation completed node=" + node.nodeId);
        }
    }

    private IEnumerator PlayEntryDialogue(DialogueEventData dialogue)
    {
        // 流程层只认对话端口（IDialogueModal），不认界面层的 DialogueManager —— 依赖方向朝下。
        var dialogueModal = CombatPresenters.Dialogue;
        if (dialogue == null || dialogueModal == null)
            yield break;

        bool queued = dialogueModal.QueueEventAndNotify(dialogue);
        if (!queued)
            yield break;

        yield return null;
        while (dialogueModal.IsBusy && Phase != FakeRoutePhase.Defeated)
            yield return null;
    }

    private IEnumerator RunBattleEntries()
    {
        if (_currentNode.battleEntries == null || _currentNode.battleEntries.Count == 0)
            yield break;

        bool anyExecuted = false;
        for (_battleIndex = 0; _battleIndex < _currentNode.battleEntries.Count; _battleIndex++)
        {
            var entry = _currentNode.battleEntries[_battleIndex];
            if (entry == null || entry.battleConfig == null) continue;
            if (entry.conditionEnabled) continue;
            if (IsBattleEntryCompleted(_currentNode, _battleIndex)) continue;

            int generation = _generation;
            _battleActive = true;
            _battleCleared = false;
            SetPhase(FakeRoutePhase.Battle);
            movementPresenter?.ApplyNodeVisuals(_currentNode);
            _battleHost.StartRouteBattle(entry.battleConfig);
            SetGameplayInput(true);

            while (_battleActive && !_battleCleared && generation == _generation && Phase != FakeRoutePhase.Defeated)
                yield return null;
            if (generation != _generation || Phase == FakeRoutePhase.Defeated) yield break;

            SetGameplayInput(false);
            SetPhase(FakeRoutePhase.WaitingReward);
            _battleHost.SetRouteRewardWaitState();
            while (IsRewardBlocking() && generation == _generation && Phase != FakeRoutePhase.Defeated)
                yield return null;
            if (generation != _generation || Phase == FakeRoutePhase.Defeated) yield break;

            MarkBattleEntryCompleted(_currentNode, _battleIndex);
            _battleActive = false;
            anyExecuted = true;
            Debug.Log("[FakeRoute] battle entry completed node=" + _currentNode.nodeId + " index=" + _battleIndex);
        }

        // 禁止回访，因此本节点本次进入的全部 Entry 必须一次跑完，然后统一播放战后演出。
        if (anyExecuted)
            yield return PlayPostBattleSequence(_currentNode);
    }

    private bool IsRewardBlocking()
    {
        var discardPrompt = CombatPresenters.ItemDiscard;
        return (UpgradeChoiceManager.Instance != null && UpgradeChoiceManager.Instance.IsChoosing)
            || (discardPrompt != null && discardPrompt.IsShowing)
            || (ExpGemManager.Instance != null && ExpGemManager.Instance.IsCollecting);
    }
}