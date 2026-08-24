using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class RouteStageV2PreviewWindow : EditorWindow
{
    private bool _showNodes = true;
    private bool _showInternalPaths = true;
    private bool _showConnections = true;
    private bool _showLabels = true;
    private bool _showDirections = true;

    [MenuItem("Tools/Route V2/Scene Preview")]
    private static void Open()
    {
        GetWindow<RouteStageV2PreviewWindow>("Route V2 Preview");
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
        EditorGUILayout.LabelField("RouteStage V2 Preview", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("仅绘制当前 RouteStage Scene 的节点、内部路径和连接路径，不修改场景对象。", MessageType.Info);
        _showNodes = EditorGUILayout.ToggleLeft("显示节点锚点", _showNodes);
        _showInternalPaths = EditorGUILayout.ToggleLeft("显示节点内部路径", _showInternalPaths);
        _showConnections = EditorGUILayout.ToggleLeft("显示 Tail→Head 连接", _showConnections);
        _showLabels = EditorGUILayout.ToggleLeft("显示标签", _showLabels);
        _showDirections = EditorGUILayout.ToggleLeft("显示方向箭头", _showDirections);
        if (GUILayout.Button("校验当前 RouteStage"))
            RouteStageV2Validator.ValidateActive();
        if (GUI.changed)
            SceneView.RepaintAll();
    }

    private void OnSceneGui(SceneView sceneView)
    {
        if (SceneManager.GetActiveScene().path == string.Empty)
            return;
        var entry = Object.FindObjectOfType<RouteStageSceneEntryV2>();
        if (entry == null)
            return;

        if (_showNodes && entry.nodes != null)
        {
            for (int i = 0; i < entry.nodes.Length; i++)
            {
                var node = entry.nodes[i];
                if (node == null)
                    continue;
                DrawNode(node);
            }
        }

        if (_showConnections && entry.connections != null)
        {
            for (int i = 0; i < entry.connections.Length; i++)
            {
                var connection = entry.connections[i];
                if (connection == null)
                    continue;
                DrawConnection(connection);
            }
        }
    }

    private void DrawNode(RouteCombatNodeEntryV2 node)
    {
        DrawAnchor(node.headJunction, Color.cyan, "Head", node.nodeConfig != null ? node.nodeConfig.nodeId : "?");
        DrawAnchor(node.combatArea, Color.green, "Combat", node.nodeConfig != null ? node.nodeConfig.nodeId : "?");
        DrawAnchor(node.tailJunction, Color.yellow, "Tail", node.nodeConfig != null ? node.nodeConfig.nodeId : "?");
        if (!_showInternalPaths)
            return;
        DrawPath(node.headToCombatPath, new Color(0.2f, 0.8f, 1f), true);
        DrawPath(node.combatToTailPath, new Color(1f, 0.7f, 0.1f), true);
    }

    private void DrawConnection(RouteConnectionSceneBindingV2 connection)
    {
        DrawAnchor(connection.rotationPivot, Color.magenta, "Pivot", connection.name);
        DrawPath(connection.travelPath, Color.magenta, true);
        if (connection.sourceTail != null && connection.targetHead != null)
        {
            Handles.color = new Color(1f, 0.2f, 0.8f, 0.35f);
            Handles.DrawDottedLine(connection.sourceTail.position, connection.targetHead.position, 4f);
        }
    }

    private void DrawAnchor(Transform anchor, Color color, string label, string owner)
    {
        if (anchor == null)
            return;
        Handles.color = color;
        Handles.SphereHandleCap(0, anchor.position, Quaternion.identity, HandleUtility.GetHandleSize(anchor.position) * 0.08f, EventType.Repaint);
        if (_showLabels)
            Handles.Label(anchor.position, owner + " " + label);
    }

    private void DrawPath(Transform[] path, Color color, bool drawArrow)
    {
        if (path == null || path.Length < 2)
            return;
        Handles.color = color;
        for (int i = 0; i < path.Length - 1; i++)
        {
            if (path[i] == null || path[i + 1] == null)
                continue;
            Handles.DrawLine(path[i].position, path[i + 1].position);
            if (_showDirections && drawArrow)
                DrawArrow(path[i].position, path[i + 1].position, color);
        }
    }

    private void DrawArrow(Vector3 from, Vector3 to, Color color)
    {
        Vector3 direction = to - from;
        if (direction.sqrMagnitude < 0.0001f)
            return;
        Vector3 midpoint = Vector3.Lerp(from, to, 0.55f);
        Handles.color = color;
        Handles.ArrowHandleCap(0, midpoint, Quaternion.LookRotation(direction), HandleUtility.GetHandleSize(midpoint) * 0.2f, EventType.Repaint);
    }
}
