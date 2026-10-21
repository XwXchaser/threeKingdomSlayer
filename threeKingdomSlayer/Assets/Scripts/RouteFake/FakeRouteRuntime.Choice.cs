using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// FakeRouteRuntime 的**分支选择**：玩家选出口 → 记录选择历史 → 播放旅行 → 进入目标节点。
///
/// 按机制从 <c>FakeRouteRuntime.cs</c> 拆出（2026-10-22，partial 分文件）。
/// partial class 与 FakeRouteRuntime.cs **同类型**，行为完全一致。
///
/// 【2026-10-22 扩展点】出口的「呈现」与「可选性」都做成了可注册的能力：
///   • 呈现：<see cref="IRouteChoiceFormatter"/>（内置：标题 = 出口名 → 目标节点名）；
///   • 可选性：<see cref="IRouteChoiceValidator"/>（内置：禁止回访已访问节点）。
/// 新增分支形态（带预览 / 带消耗 / 需要天赋解锁）不必改本类。
/// </summary>

public sealed partial class FakeRouteRuntime : MonoBehaviour
{
    /// <summary>出口呈现格式化器（扩展点）。自定义优先于内置兼底。</summary>
    private readonly List<IRouteChoiceFormatter> _choiceFormatters = new List<IRouteChoiceFormatter>();

    /// <summary>出口可选性判定器（扩展点）。全部通过才算可选。</summary>
    private readonly List<IRouteChoiceValidator> _choiceValidators = new List<IRouteChoiceValidator>();

    /// <summary>注册**出口呈现**（扩展点）。</summary>
    public void RegisterChoiceFormatter(IRouteChoiceFormatter formatter)
    {
        if (formatter == null) return;
        _choiceFormatters.Add(formatter);
    }

    /// <summary>注册**出口呈现**（扩展点，委托版）。</summary>
    public void RegisterChoiceFormatter(
        System.Func<FakeRouteChoiceConfig, FakeRouteNodeConfig, bool> canFormat,
        System.Action<FakeRouteChoiceConfig, FakeRouteNodeConfig, RouteChoiceOption> format)
    {
        RegisterChoiceFormatter(new DelegateRouteChoiceFormatter(canFormat, format));
    }

    /// <summary>注册**出口可选性判定**（扩展点）。</summary>
    public void RegisterChoiceValidator(IRouteChoiceValidator validator)
    {
        if (validator == null) return;
        _choiceValidators.Add(validator);
    }

    /// <summary>注册**出口可选性判定**（扩展点，委托版）。返回非空字符串 = 不可选（该字符串是原因）。</summary>
    public void RegisterChoiceValidator(
        System.Func<FakeRouteChoiceConfig, FakeRouteNodeConfig, string> check)
    {
        RegisterChoiceValidator(new DelegateRouteChoiceValidator(check));
    }

    /// <summary>按注册顺序找第一个负责该出口的格式化器；都没有则用内置兼底。</summary>
    private IRouteChoiceFormatter ResolveChoiceFormatter(FakeRouteChoiceConfig choice, FakeRouteNodeConfig fromNode)
    {
        for (int i = 0; i < _choiceFormatters.Count; i++)
        {
            var formatter = _choiceFormatters[i];
            if (formatter != null && formatter.CanFormat(choice, fromNode)) return formatter;
        }
        return DefaultRouteChoiceFormatterInstance;
    }

    /// <summary>内置兼底格式化器（标题 = 出口名 → 目标节点名）。</summary>
    private static readonly IRouteChoiceFormatter DefaultRouteChoiceFormatterInstance = new DefaultRouteChoiceFormatter();

    /// <summary>该出口当前是否可选（跑一遍所有判定器）。</summary>
    private bool IsChoiceSelectable(FakeRouteChoiceConfig choice, FakeRouteNodeConfig fromNode, out string reason)
    {
        reason = null;
        // 内置规则：禁止回访已访问过的节点。
        if (choice != null && choice.targetNode != null && _visitedNodes.Contains(choice.targetNode))
        {
            reason = "已走过这条路";
            return false;
        }
        for (int i = 0; i < _choiceValidators.Count; i++)
        {
            var validator = _choiceValidators[i];
            if (validator == null) continue;
            if (!validator.IsSelectable(choice, fromNode, out string validatorReason))
            {
                reason = string.IsNullOrEmpty(validatorReason) ? "当前不可选" : validatorReason;
                return false;
            }
        }
        return true;
    }

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
                if (!IsChoiceSelectable(choice, _currentNode, out string lockedReason))
                {
                    Debug.LogWarning("[FakeRoute] 出口不可选: " + choice.choiceId + "（" + lockedReason + "）");
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
        var options = BuildChoiceOptions(_currentNode);
        RouteChoicePanel.Show(choicePanelPrefab, _currentNode != null ? _currentNode.displayName : string.Empty, options, id => TrySelectChoice(id));
    }

    /// <summary>
    /// 构造某个节点的出口选项（呈现 + 可选性）。
    /// 拆成独立方法：面板只负责显示，选项怎么算全在这里，便于单独验证。
    /// </summary>
    private List<RouteChoiceOption> BuildChoiceOptions(FakeRouteNodeConfig node)
    {
        var options = new List<RouteChoiceOption>();
        if (node == null || node.outgoingChoices == null) return options;
        for (int i = 0; i < node.outgoingChoices.Count; i++)
        {
            var choice = node.outgoingChoices[i];
            if (choice == null || choice.targetNode == null) continue;
            var option = new RouteChoiceOption { id = choice.choiceId, layout = choice.layout };

            // 呈现：自定义格式化器优先，内置兼底保持「出口名 → 目标节点名」。
            ResolveChoiceFormatter(choice, node).Format(choice, node, option);

            // 可选性：不可选时锁定按钮并把原因显示在副标题上。
            if (!IsChoiceSelectable(choice, node, out string lockedReason))
            {
                option.locked = true;
                option.lockedReason = lockedReason;
                if (string.IsNullOrEmpty(option.description)) option.description = lockedReason;
            }
            options.Add(option);
        }
        return options;
    }
}