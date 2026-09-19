using System.Collections;
using UnityEngine;

/// <summary>Unified node integration test; intentionally does not write production saves.</summary>
public sealed class ScrollNodeFlowTrial : MonoBehaviour
{
    public FakeRouteStageConfig route;
    public StageController stage;
    public CurvedScrollLab lab;
    public string Phase { get; private set; }
    public FakeRouteNodeConfig Current { get; private set; }
    public bool IsWaitingForChoice => Phase == "Choose route";
    public string CurrentNodeId => Current != null ? Current.nodeId : string.Empty;
    readonly System.Collections.Generic.HashSet<FakeRouteNodeConfig> visited = new System.Collections.Generic.HashSet<FakeRouteNodeConfig>();
    bool cleared, acknowledged;
    string line;
    IEnumerator Start()
    {
        yield return null;
        if(!route || !stage || !lab){Phase="Missing references";yield break;}
        if(!FakeRouteGraphValidator.TryValidate(route,out var error)){Phase=error;yield break;}
        lab.Initialize();lab.enabled=false;lab.paused=true;
        stage.OnRouteBattleCompleted+=OnCleared;
        stage.playerState.ResetPlayer();
        stage.SetRouteTravelState();
        yield return Enter(route.startNode);
    }
    void OnCleared(){cleared=true;}
    bool Blocking()=> (UpgradeChoiceManager.Instance && UpgradeChoiceManager.Instance.IsChoosing)||ItemDiscardPopup.IsShowing||(ExpGemManager.Instance && ExpGemManager.Instance.IsCollecting);
    IEnumerator Enter(FakeRouteNodeConfig node)
    {
        Current=node;visited.Add(node);SetInput(false);
        foreach(var dialogue in node.dialogues)
        {
            if(!dialogue)continue;
            foreach(var message in dialogue.lines){line=message.text;acknowledged=false;Phase="Dialogue";while(!acknowledged)yield return null;}
        }
        foreach(var entry in node.battleEntries)
        {
            if(entry==null||!entry.battleConfig||entry.conditionEnabled)continue;
            cleared = false;
            Phase = "Battle";
            if (stage.waveSpawner != null) stage.waveSpawner.stageConfig = entry.battleConfig;
            stage.StartRouteBattle(entry.battleConfig);
            SetInput(true);
            while(!cleared && stage.CurrentState!=StageState.Defeat)yield return null;
            SetInput(false);if(stage.CurrentState==StageState.Defeat){Phase="Defeated";yield break;}
            Phase="Waiting rewards";stage.SetRouteRewardWaitState();yield return null;
            while(Blocking())yield return null;
        }
        if(node.rewards.Count>0)
        {
            acknowledged=false;Phase="Claim reward";while(!acknowledged)yield return null;
            foreach(var reward in node.rewards)
            {
                if(reward.rewardType==KillRewardType.Coin)stage.playerState.AddCoins(reward.rewardAmount);
                else if(reward.rewardType==KillRewardType.Heal){var p=stage.playerState;p.currentHealth=Mathf.Min(p.currentHealth+reward.rewardAmount,p.heroConfig.maxHealth);p.OnHealthChanged?.Invoke(p.currentHealth,p.heroConfig.maxHealth);}
                else if(reward.rewardType==KillRewardType.RandomUpgrade)for(int i=0;i<reward.rewardAmount;i++)UpgradeChoiceManager.Instance?.TriggerItemChoice();
            }
            yield return null;while(Blocking())yield return null;
        }
        stage.SetRouteTravelState();
        Phase=node.terminalPolicy==FakeRouteTerminalPolicy.None?"Choose route":"Completed (test only)";
    }
    public void CompleteRewardChoiceForTest()
    {
        acknowledged = true;
    }

    public bool Choose(FakeRouteChoiceConfig choice)
    {
        if(Phase!="Choose route"||!choice||!Current.outgoingChoices.Contains(choice)||visited.Contains(choice.targetNode))return false;
        Phase="Travel";StartCoroutine(Travel(choice));return true;
    }
    IEnumerator Travel(FakeRouteChoiceConfig choice)
    {
        var p=choice.presentation;double origin=lab.Distance;float elapsed=0,duration=Mathf.Max(.1f,p.duration);lab.curvature=p.curveStrength;
        while(elapsed<duration){elapsed+=Time.deltaTime;lab.SetPresentationDistance(origin+p.travelDistance*Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/duration)));yield return null;}
        lab.SetPresentationDistance(origin+p.travelDistance);yield return Enter(choice.targetNode);
    }
    void SetInput(bool value){if(InputManager.Instance){InputManager.Instance.gameplayInputEnabled=value;if(!value)InputManager.Instance.CancelCurrentGesture();}}
    void OnGUI()
    {
        var old = GUI.matrix;
        float scale = Mathf.Max(1f, Screen.width / 540f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale,scale,1));
        float width = Screen.width / scale;
        GUILayout.BeginArea(new Rect(16,Phase=="Choose route"?Screen.height/scale*.28f:12,width-32,260),GUI.skin.box);
        GUILayout.Label("路线测试 / " + (Current?Current.displayName:"") + " / " + Phase);
        if(Phase=="Waiting rewards")GUILayout.Label("请先完成全部升级、道具选择和经验收集");
        if(Phase=="Dialogue"){GUILayout.Label(line);if(GUILayout.Button("Continue dialogue"))acknowledged=true;}
        if(Phase=="Claim reward"&&GUILayout.Button("Claim node reward"))acknowledged=true;
        if(Phase=="Choose route")foreach(var c in Current.outgoingChoices)if(c&&c.targetNode&&!visited.Contains(c.targetNode)&&GUILayout.Button("继续前进："+c.displayName+" -> "+c.targetNode.displayName,GUILayout.Height(60)))Choose(c);
        GUILayout.EndArea();
        GUI.matrix = old;
    }
    void OnDestroy(){if(stage)stage.OnRouteBattleCompleted-=OnCleared;}
}
