using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RouteWorldMotionPrototype))]
public sealed class RouteWorldMotionPrototypeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button("Play Forward → J0 → Left 90°"))
                ((RouteWorldMotionPrototype)target).PlayForwardThenLeftTurn();
            if (GUILayout.Button("Stop and Reset World Root"))
            {
                var prototype = (RouteWorldMotionPrototype)target;
                prototype.StopMotion();
                var root = serializedObject.FindProperty("routeWorldRoot").objectReferenceValue as Transform;
                if (root != null)
                    root.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
        }
        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("进入 Play Mode 后使用测试按钮。此原型只移动 RouteWorldRoot。", MessageType.Info);
    }
}
