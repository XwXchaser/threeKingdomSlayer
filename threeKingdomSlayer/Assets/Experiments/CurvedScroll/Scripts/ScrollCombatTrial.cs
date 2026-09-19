using System.Collections;
using UnityEngine;

/// <summary>Editor lab flow only: no route save or victory settlement.</summary>
public sealed class ScrollCombatTrial : MonoBehaviour
{
    public CurvedScrollLab lab;
    public StageController stage;
    public StageConfig battle;
    public InputManager input;
    public float settleSeconds = 0.5f;
    public string Phase { get; private set; } = "Initializing";
    bool cleared, defeated;
    bool depart;

    IEnumerator Start()
    {
        yield return null; // Allow existing battle managers to initialize and subscribe.
        if (lab == null || stage == null || battle == null || input == null)
        { Debug.LogError("ScrollCombatTrial: missing Inspector references", this); yield break; }
        stage.OnRouteBattleCompleted += OnCleared;
        stage.OnStageDefeat += OnDefeated;
        lab.Initialize(); lab.showControls = false; lab.paused = true;
        input.gameplayInputEnabled = false;
        stage.playerState.ResetPlayer();
        stage.SetRouteTravelState();
        Phase = "Approaching"; lab.StartTrip();
        while(lab.TripActive) yield return null;
        lab.paused = true; Phase = "Settling";
        yield return new WaitForSeconds(Mathf.Max(0,settleSeconds));
        Phase = "Combat";
        stage.StartRouteBattle(battle);
        input.gameplayInputEnabled = true;
        while(!cleared && !defeated && stage.CurrentState != StageState.Defeat) yield return null;
        defeated |= stage.CurrentState == StageState.Defeat;
        input.gameplayInputEnabled = false; input.CancelCurrentGesture();
        if(defeated){Phase="Defeated - stop Play Mode to retry";yield break;}
        Phase="Rewards"; stage.SetRouteRewardWaitState();
        // Let death/experience callbacks settle before inspecting blocking UI.
        yield return null;
        while((UpgradeChoiceManager.Instance != null && UpgradeChoiceManager.Instance.IsChoosing)
            || ItemDiscardPopup.IsShowing
            || (ExpGemManager.Instance != null && ExpGemManager.Instance.IsCollecting)) yield return null;
        Phase="Ready - choose Continue";
        while(!depart)yield return null;
        stage.SetRouteTravelState(); Phase="Departing"; lab.StartTrip();
        while(lab.TripActive)yield return null;
        lab.paused=true;Phase="Complete - no save written by trial";
    }
    void OnCleared(){cleared=true;}
    void OnDefeated(){defeated=true;}
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(12,12,340,85),GUI.skin.box);
        GUILayout.Label("SCROLL + REAL COMBAT TRIAL");GUILayout.Label(Phase);
        if(Phase=="Ready - choose Continue" && GUILayout.Button("Continue travel"))depart=true;
        GUILayout.EndArea();
    }
    void OnDestroy()
    {
        if(stage!=null){stage.OnRouteBattleCompleted-=OnCleared;stage.OnStageDefeat-=OnDefeated;}
    }
}
