using System.Collections.Generic;
using UnityEngine;

public enum FakeRouteNodeCompletionPolicy { BattleAndReward, ChoiceConfirmation, EnemyDefeated, Immediate }
public enum FakeRouteTerminalPolicy { None, CompleteWhenContentFinished, CompleteAfterPresentation }

[CreateAssetMenu(fileName = "NewFakeRouteNodeConfig", menuName = "一夫当关/假移动路线节点")]
public sealed class FakeRouteNodeConfig : ScriptableObject
{
    public string nodeId;
    public string displayName;

    [Header("节点内容")]
    public DialogueEventData entryDialogue;
    public DialogueEventData postBattleDialogue;
    public FakeRoutePresentation postBattlePresentation;
    [Tooltip("终点节点：BattleEntry 完成后不显示出口，收尾进入整关结算。")]
    public bool isFinalNode;
    public List<DialogueEventData> dialogues = new List<DialogueEventData>();
    public List<KillRewardEntry> rewards = new List<KillRewardEntry>();
    public List<FakeRouteBattleEntry> battleEntries = new List<FakeRouteBattleEntry>();
    public FakeRouteNodeCompletionPolicy completionPolicy = FakeRouteNodeCompletionPolicy.BattleAndReward;
    public FakeRouteTerminalPolicy terminalPolicy = FakeRouteTerminalPolicy.None;
    public bool savePoint = true;
    public ScrollRouteVisualProfile visualProfile;
    public List<FakeRouteChoiceConfig> outgoingChoices = new List<FakeRouteChoiceConfig>();
}
