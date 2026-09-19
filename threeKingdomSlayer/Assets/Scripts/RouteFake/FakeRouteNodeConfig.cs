using System.Collections.Generic;
using UnityEngine;

public enum FakeRouteNodeCompletionPolicy { BattleAndReward, ChoiceConfirmation, EnemyDefeated, Immediate }
public enum FakeRouteTerminalPolicy { None, CompleteWhenContentFinished, CompleteAfterPresentation }

[CreateAssetMenu(fileName = "NewFakeRouteNodeConfig", menuName = "一夫当关/假移动路线节点")]
public sealed class FakeRouteNodeConfig : ScriptableObject
{
    public string nodeId;
    public string displayName;
    public List<DialogueEventData> dialogues = new List<DialogueEventData>();
    public List<KillRewardEntry> rewards = new List<KillRewardEntry>();
    public List<FakeRouteBattleEntry> battleEntries = new List<FakeRouteBattleEntry>();
    public FakeRouteNodeCompletionPolicy completionPolicy = FakeRouteNodeCompletionPolicy.BattleAndReward;
    public FakeRouteTerminalPolicy terminalPolicy = FakeRouteTerminalPolicy.None;
    public bool savePoint = true;
    public List<FakeRouteChoiceConfig> outgoingChoices = new List<FakeRouteChoiceConfig>();
}
