#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ScrollRouteConnection))]
public sealed class ScrollRouteConnectionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script");
        var c = (ScrollRouteConnection)target;
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("统一方向角", c.ExpectedAngle.ToString("F1") + "°", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("连接", c.ConnectionId);
        if (c.pathPoints == null || c.pathPoints.Length < 2)
            EditorGUILayout.HelpBox("至少需要两个路径点。", MessageType.Warning);
        if (GUILayout.Button("自动创建 3 个路径点"))
        {
            Undo.RegisterCompleteObjectUndo(c.gameObject, "Create Route Path Points");
            CreatePoints(c);
        }
        if (GUILayout.Button("校验当前连接")) ScrollRouteConnectionValidator.Validate(c);
        if (GUILayout.Button("重建道路和路肩网格"))
        {
            foreach (var road in c.GetComponentsInChildren<ScrollRouteRoadSurface>(true)) road.Rebuild();
            foreach (var shoulder in c.GetComponentsInChildren<ScrollRouteShoulderSurface>(true)) shoulder.Rebuild();
            SceneView.RepaintAll();
        }
        serializedObject.ApplyModifiedProperties();
    }

    static void CreatePoints(ScrollRouteConnection c)
    {
        var root = new GameObject("Path").transform;
        root.SetParent(c.transform, false);
        var points = new Transform[3];
        Vector3 start = c.transform.position;
        float angle = c.ExpectedAngle * Mathf.Deg2Rad;
        Vector3 forward = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
        points[0] = Point(root, "P00_Start", start);
        points[1] = Point(root, "P01_Bend", start + Vector3.forward * 12f + forward * 3f);
        points[2] = Point(root, "P02_Arrival", start + forward * 24f);
        c.pathPoints = points;
        EditorUtility.SetDirty(c);
        SceneView.RepaintAll();
    }
    static Transform Point(Transform parent, string name, Vector3 position)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, true); go.transform.position = position; return go.transform;
    }
}

public static class ScrollRouteConnectionValidator
{
    [MenuItem("Tools/Curved Scroll/Validate Route Connections")]
    public static void ValidateAll()
    {
        var connections = Object.FindObjectsOfType<ScrollRouteConnection>();
        int errors = 0;
        foreach (var c in connections) errors += Validate(c, false);
        Debug.Log("[ScrollRoute] validated=" + connections.Length + " errors=" + errors);
    }
    public static void Validate(ScrollRouteConnection c) { Validate(c, true); }
    static int Validate(ScrollRouteConnection c, bool log)
    {
        if (c == null) return 0;
        int errors = 0;
        if (c.rules == null) { Error(c, "Missing global route rules"); errors++; }
        if (c.sourceNode == null || c.targetNode == null) { Error(c, "Missing source/target node"); errors++; }
        if (c.pathPoints == null || c.pathPoints.Length < 2) { Error(c, "Path requires at least 2 points"); errors++; }
        if (c.choice != null && c.choice.targetNode != null && c.targetNode != c.choice.targetNode) { Error(c, "targetNode differs from choice.targetNode"); errors++; }
        if (c.rules != null && c.pathPoints != null && c.pathPoints.Length >= 2)
        {
            float expected = c.ExpectedAngle;
            float actual = Mathf.Atan2(c.GetTangentAtEnd().x, c.GetTangentAtEnd().z) * Mathf.Rad2Deg;
            float delta = Mathf.DeltaAngle(expected, actual);
            if (Mathf.Abs(delta) > c.rules.mergeAngleTolerance) { Error(c, "End tangent=" + actual.ToString("F1") + "°, expected=" + expected.ToString("F1") + "°"); errors++; }
        }
        if (log && errors == 0) Debug.Log("[ScrollRoute] OK " + c.ConnectionId, c);
        return errors;
    }
    static void Error(Object context, string message) { Debug.LogError("[ScrollRoute] " + message, context); }
}
#endif
