using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RouteWorldNodeFlow))]
public sealed class RouteWorldNodeFlowEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button("Play N0 → J0 → Left → N1"))
                ((RouteWorldNodeFlow)target).PlayToJunctionThenLeft();
        }
    }
}
