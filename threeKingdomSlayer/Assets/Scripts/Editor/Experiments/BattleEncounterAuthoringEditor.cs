using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BattleEncounterAuthoring))]
public sealed class BattleEncounterAuthoringEditor : Editor
{
    SerializedProperty nodeId, displayName, worldDistance, battleConfigs, enabledForRoute, showAllWaves, showLabels;
    SerializedProperty battleAnchor, playerEntry, battleAreaSize, gizmoColor, enemyPreviewColor;

    void OnEnable()
    {
        nodeId = serializedObject.FindProperty("nodeId");
        displayName = serializedObject.FindProperty("displayName");
        worldDistance = serializedObject.FindProperty("worldDistance");
        battleConfigs = serializedObject.FindProperty("battleConfigs");
        enabledForRoute = serializedObject.FindProperty("enabledForRoute");
        showAllWaves = serializedObject.FindProperty("showAllWaves");
        showLabels = serializedObject.FindProperty("showLabels");
        battleAnchor = serializedObject.FindProperty("battleAnchor");
        playerEntry = serializedObject.FindProperty("playerEntry");
        battleAreaSize = serializedObject.FindProperty("battleAreaSize");
        gizmoColor = serializedObject.FindProperty("gizmoColor");
        enemyPreviewColor = serializedObject.FindProperty("enemyPreviewColor");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.LabelField("Encounter Authoring", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Scene preview only. Real enemies are still spawned by Battle.scene / StageController.", MessageType.Info);
        EditorGUILayout.PropertyField(nodeId);
        EditorGUILayout.PropertyField(displayName);
        EditorGUILayout.PropertyField(worldDistance);
        EditorGUILayout.PropertyField(enabledForRoute);
        EditorGUILayout.PropertyField(battleConfigs, true);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Battle-space authoring", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(battleAnchor);
        EditorGUILayout.PropertyField(playerEntry);
        EditorGUILayout.PropertyField(battleAreaSize);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(showAllWaves);
        EditorGUILayout.PropertyField(showLabels);
        EditorGUILayout.PropertyField(gizmoColor);
        EditorGUILayout.PropertyField(enemyPreviewColor);
        serializedObject.ApplyModifiedProperties();

        var encounter = (BattleEncounterAuthoring)target;
        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Select Battle Anchor") && encounter.battleAnchor)
                Selection.activeTransform = encounter.battleAnchor;
            if (GUILayout.Button("Select Player Entry") && encounter.playerEntry)
                Selection.activeTransform = encounter.playerEntry;
        }
        if (GUILayout.Button("Frame Encounter"))
        {
            Selection.activeGameObject = encounter.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();
        }
    }
}
