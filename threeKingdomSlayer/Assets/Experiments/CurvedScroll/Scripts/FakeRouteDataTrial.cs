using System.Collections.Generic;
using UnityEngine;

/// <summary>Manual test harness for FakeRoute assets and valley travel presentation.</summary>
public sealed class FakeRouteDataTrial : MonoBehaviour
{
    public FakeRouteStageConfig route;
    public ValleyArtPresentation valleyArt;
    public CurvedScrollLab lab;
    public bool clearOnStart = true;
    public string currentNodeId { get; private set; }
    public string phase { get; private set; } = "Not started";
    readonly HashSet<string> visited = new HashSet<string>();
    readonly HashSet<string> completed = new HashSet<string>();
    readonly List<string> choices = new List<string>();
    int generation;
    FakeRouteNodeConfig current;
    float travelTimer;
    FakeRouteChoiceConfig pendingChoice;

    void Start()
    {
        if (route == null) { phase = "Missing route asset"; return; }
        if (!FakeRouteGraphValidator.TryValidate(route, out var error)) { phase = "Invalid: " + error; return; }
        if (clearOnStart) SaveManager.ClearFakeRouteSnapshot(route.routeId, route.stageId);
        ResetRuntime(); EnterNode(route.startNode);
    }
    void Update() { }

    System.Collections.IEnumerator TravelRoutine(FakeRouteChoiceConfig choice, int token)
    {
        float duration = choice.presentation != null ? Mathf.Max(.1f, choice.presentation.duration) : 1f;
        float elapsed = 0f;
        while (elapsed < duration && token == generation)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        if (token != generation) yield break;
        CommitTarget(choice.targetNode);
    }
    void ResetRuntime()
    {
        generation++; pendingChoice=null; visited.Clear(); completed.Clear(); choices.Clear(); current=null; currentNodeId=null; travelTimer=0;
        phase="Reset";
        if(lab!=null){lab.ResetTravel();lab.paused=true;}
    }
    void EnterNode(FakeRouteNodeConfig node)
    {
        if(node==null)return;
        if(visited.Contains(node.nodeId)){phase="Rejected revisit: "+node.nodeId;return;}
        current=node;currentNodeId=node.nodeId;visited.Add(node.nodeId);completed.Remove(node.nodeId);phase="Node arrived: "+node.displayName;
        if(lab!=null){lab.ResetTravel();lab.paused=true;}
        SaveCheckpoint("arrive");
    }
    public bool CompleteCurrentNode()
    {
        if(current==null)return false;
        completed.Add(current.nodeId);phase="Node completed: "+current.displayName;SaveCheckpoint("complete");return true;
    }
    public bool Choose(string choiceId)
    {
        if(current==null||!completed.Contains(current.nodeId)||current.outgoingChoices==null)return false;
        for(int i=0;i<current.outgoingChoices.Count;i++)
        {
            var choice=current.outgoingChoices[i];if(choice==null||choice.choiceId!=choiceId)continue;
            if(choice.targetNode==null||visited.Contains(choice.targetNode.nodeId)){phase="Rejected revisit: "+choice.targetNode?.nodeId;return false;}
            pendingChoice=choice;choices.Add(choice.choiceId);phase="Traveling: "+choice.choiceId+" -> "+choice.targetNode.nodeId;travelTimer=choice.presentation!=null?Mathf.Max(.1f,choice.presentation.duration):1f;
            generation++;if(lab!=null){lab.ResetTravel();lab.StartTrip();}StartCoroutine(TravelRoutine(choice,generation));return true;
        }
        return false;
    }
    void CommitTarget(FakeRouteNodeConfig target){var choice=pendingChoice;pendingChoice=null;CommitTargetInternal(target,choice);}
    void CommitTargetInternal(FakeRouteNodeConfig target,FakeRouteChoiceConfig choice){if(target==null)return;EnterNode(target);SaveCheckpoint("travel complete");}
    public void SaveCheckpoint(string reason)
    {
        if(route==null||current==null)return;
        var snapshot=new FakeRouteStageSaveSnapshot{routeId=route.routeId,stageId=route.stageId,configurationVersion=route.configurationVersion,checkpointNodeId=current.nodeId};
        snapshot.visitedNodeIds.AddRange(visited);snapshot.completedNodeIds.AddRange(completed);snapshot.routeChoiceHistory.AddRange(choices);SaveManager.SaveFakeRouteSnapshot(snapshot);Debug.Log("[FakeRouteTrial] checkpoint="+current.nodeId+" reason="+reason);
    }
    public void LoadCheckpoint()
    {
        var snapshot=SaveManager.GetFakeRouteSnapshot(route.routeId,route.stageId);if(snapshot==null){phase="No snapshot";return;}
        if(snapshot.configurationVersion!=route.configurationVersion){phase="Rejected snapshot version";return;}
        var node=route.nodes.Find(n=>n!=null&&n.nodeId==snapshot.checkpointNodeId);if(node==null){phase="Rejected snapshot node";return;}
        ResetRuntime();visited.UnionWith(snapshot.visitedNodeIds);completed.UnionWith(snapshot.completedNodeIds);choices.AddRange(snapshot.routeChoiceHistory);current=node;currentNodeId=node.nodeId;phase="Loaded checkpoint: "+node.displayName;if(lab!=null){lab.ResetTravel();lab.paused=true;}
    }
    public void ClearAndRestart(){if(route!=null)SaveManager.ClearFakeRouteSnapshot(route.routeId,route.stageId);ResetRuntime();EnterNode(route.startNode);}
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10,10,380,310),GUI.skin.box);GUILayout.Label("FAKE ROUTE DATA TRIAL");GUILayout.Label("Route: "+(route!=null?route.routeId:"NULL")+"  Version: "+(route!=null?route.configurationVersion:0));GUILayout.Label("Node: "+currentNodeId+"\nPhase: "+phase);GUILayout.Label("Visited: "+string.Join(",",visited));
        if(current!=null&&!completed.Contains(current.nodeId)&&GUILayout.Button("Complete current content"))CompleteCurrentNode();
        if(current!=null&&completed.Contains(current.nodeId)){GUILayout.Label("Choices:");for(int i=0;i<current.outgoingChoices.Count;i++){var c=current.outgoingChoices[i];if(c!=null&&GUILayout.Button(c.choiceId+" -> "+c.targetNode.nodeId))Choose(c.choiceId);}}
        GUILayout.BeginHorizontal();if(GUILayout.Button("Save checkpoint"))SaveCheckpoint("manual");if(GUILayout.Button("Load checkpoint"))LoadCheckpoint();if(GUILayout.Button("Restart"))ClearAndRestart();GUILayout.EndHorizontal();GUILayout.EndArea();
    }
}
