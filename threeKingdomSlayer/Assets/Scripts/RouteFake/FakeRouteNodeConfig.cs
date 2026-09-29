using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewFakeRouteNodeConfig", menuName = "一夫当关/假移动路线节点配置")]
public sealed class FakeRouteNodeConfig : ScriptableObject
{
    public string nodeId;
    public string displayName;
    public List<FakeRouteBattleEntry> battleEntries = new List<FakeRouteBattleEntry>();
    public DialogueEventData entryDialogue;
    public DialogueEventData postBattleDialogue;
    public FakeRoutePresentation postBattlePresentation;
    public bool isFinalNode;
    public bool savePoint;
    public FakeRoutePresentation battleBackground;
    public FakeRoutePresentation routeChoiceTransition;
    public FakeRoutePresentation routeChoiceBackground;
    public List<FakeRouteChoiceConfig> outgoingChoices = new List<FakeRouteChoiceConfig>();
}
