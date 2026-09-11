#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class RouteChoiceLayoutPreviewWindow : EditorWindow
{
    private const float WorldUnitsPerCanvasPixel = 0.01f;
    private bool _showLabels = true;
    private bool _showHandles = true;
    private bool _showDefaultLayout = true;

    [MenuItem("Tools/Route/Choice Layout Preview")]
    private static void Open()
    {
        GetWindow<RouteChoiceLayoutPreviewWindow>("Choice Layout Preview");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGui;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGui;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Route Choice Layout Preview", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("在 Scene View 中拖动位置手柄和旋转手柄，修改会直接保存到出口配置资产。位置单位按 UI anchoredPosition 处理。", MessageType.Info);
        _showLabels = EditorGUILayout.ToggleLeft("显示标签", _showLabels);
        _showHandles = EditorGUILayout.ToggleLeft("显示编辑手柄", _showHandles);
        _showDefaultLayout = EditorGUILayout.ToggleLeft("显示默认布局", _showDefaultLayout);
        if (GUILayout.Button("重绘 Scene View"))
            SceneView.RepaintAll();
    }

    private void OnSceneGui(SceneView sceneView)
    {
        if (SceneManager.GetActiveScene().path == string.Empty)
            return;

        var graph = Object.FindObjectOfType<RouteWorldGraph>();
        if (graph != null && graph.nodes != null)
        {
            for (int i = 0; i < graph.nodes.Length; i++)
                DrawRouteNode(graph.nodes[i]);
        }

        var v2Entry = Object.FindObjectOfType<RouteStageSceneEntryV2>();
        if (v2Entry != null && v2Entry.nodes != null)
        {
            for (int i = 0; i < v2Entry.nodes.Length; i++)
                DrawV2Node(v2Entry.nodes[i]);
        }
    }

    private void DrawRouteNode(RouteWorldNodeBinding node)
    {
        if (node == null || node.nodeDefinition == null || node.nodeDefinition.outgoingEdges == null || node.nodeDefinition.outgoingEdges.Count == 0)
            return;
        Transform anchor = node.routeChoiceAnchor != null ? node.routeChoiceAnchor : node.transform;
        DrawNodeOrigin(anchor, node.nodeDefinition.displayName, Color.cyan);
        for (int i = 0; i < node.nodeDefinition.outgoingEdges.Count; i++)
        {
            var edge = node.nodeDefinition.outgoingEdges[i];
            if (edge == null || edge.destination == null)
                continue;
            DrawChoice(anchor, node.nodeDefinition, edge.layout, edge.direction.ToString(), edge.destination.displayName, i, node.nodeDefinition.outgoingEdges.Count, Color.yellow);
        }
    }

    private void DrawV2Node(RouteCombatNodeEntryV2 node)
    {
        if (node == null || node.nodeConfig == null || node.nodeConfig.isFinalNode || node.nodeConfig.outgoingConnections == null || node.nodeConfig.outgoingConnections.Count == 0)
            return;
        Transform anchor = node.tailJunction != null ? node.tailJunction : node.transform;
        DrawNodeOrigin(anchor, node.nodeConfig.displayName, Color.magenta);
        for (int i = 0; i < node.nodeConfig.outgoingConnections.Count; i++)
        {
            var connection = node.nodeConfig.outgoingConnections[i];
            if (connection == null || connection.targetNode == null)
                continue;
            DrawChoice(anchor, node.nodeConfig, connection.layout, connection.choiceSlot, connection.targetNode.displayName, i, node.nodeConfig.outgoingConnections.Count, Color.green);
        }
    }

    private void DrawNodeOrigin(Transform anchor, string nodeName, Color color)
    {
        if (anchor == null)
            return;
        Handles.color = color;
        float size = HandleUtility.GetHandleSize(anchor.position);
        Handles.SphereHandleCap(0, anchor.position, Quaternion.identity, size * 0.08f, EventType.Repaint);
        if (_showLabels)
            Handles.Label(anchor.position, "Choice Preview: " + (string.IsNullOrEmpty(nodeName) ? "?" : nodeName));
    }

    private void DrawChoice(Transform anchor, Object owner, RouteChoiceLayout layout, string choiceName, string targetName, int index, int count, Color color)
    {
        if (anchor == null || layout == null)
            return;
        if (!_showDefaultLayout && !layout.overrideDefault)
            return;

        Vector2 canvasPosition = layout.overrideDefault ? layout.anchoredPosition : GetDefaultPosition(index, count);
        float canvasRotation = layout.overrideDefault ? layout.rotation : 0f;
        Vector3 worldPosition = anchor.TransformPoint(new Vector3(canvasPosition.x * WorldUnitsPerCanvasPixel, canvasPosition.y * WorldUnitsPerCanvasPixel, 0f));
        Quaternion worldRotation = anchor.rotation * Quaternion.Euler(0f, 0f, canvasRotation);
        float handleSize = HandleUtility.GetHandleSize(worldPosition);

        Handles.color = color;
        Handles.DrawLine(anchor.position, worldPosition);
        DrawArrow(worldPosition, worldRotation, handleSize * 0.35f, color);
        if (_showLabels)
            Handles.Label(worldPosition, choiceName + " → " + targetName + (layout.overrideDefault ? " [Custom]" : " [Default]"));

        if (!_showHandles)
            return;

        EditorGUI.BeginChangeCheck();
        Vector3 movedPosition = Handles.PositionHandle(worldPosition, worldRotation);
        Quaternion movedRotation = Handles.RotationHandle(worldRotation, movedPosition);
        if (!EditorGUI.EndChangeCheck())
            return;

        Undo.RecordObject(owner, "Adjust Route Choice Layout");
        layout.overrideDefault = true;
        Vector3 localPosition = anchor.InverseTransformPoint(movedPosition);
        layout.anchoredPosition = new Vector2(localPosition.x / WorldUnitsPerCanvasPixel, localPosition.y / WorldUnitsPerCanvasPixel);
        Quaternion localRotation = Quaternion.Inverse(anchor.rotation) * movedRotation;
        layout.rotation = NormalizeAngle(localRotation.eulerAngles.z);
        EditorUtility.SetDirty(owner);
        AssetDatabase.SaveAssets();
        SceneView.RepaintAll();
    }

    private static Vector2 GetDefaultPosition(int index, int count)
    {
        if (count <= 1)
            return Vector2.zero;
        return new Vector2((index - (count - 1) * 0.5f) * 160f, 0f);
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    private static void DrawArrow(Vector3 position, Quaternion rotation, float size, Color color)
    {
        Vector3 up = rotation * Vector3.up;
        Vector3 right = rotation * Vector3.right;
        Vector3 tip = position + up * size;
        Vector3 leftWing = position - up * size * 0.5f - right * size * 0.55f;
        Vector3 rightWing = position - up * size * 0.5f + right * size * 0.55f;
        Handles.color = color;
        Handles.DrawLine(position - up * size, tip);
        Handles.DrawLine(tip, leftWing);
        Handles.DrawLine(tip, rightWing);
    }
}
#endif
