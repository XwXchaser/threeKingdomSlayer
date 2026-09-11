#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public sealed class FakeRouteChoiceLayoutWindow : EditorWindow
{
    private FakeRouteStageConfig _route;
    private int _nodeIndex;
    private Vector2 _scroll;
    private Sprite _arrowSprite;
    private FakeRouteChoiceConfig _draggedChoice;
    private bool _draggingRotation;
    private const float ReferenceScreenWidth = 1080f;
    private const float ReferenceScreenHeight = 1920f;
    private const float PreviewWidth = 360f;
    private const float PreviewHeight = 540f;
    private const float PanelWidth = 700f;
    private const float PanelHeight = 500f;
    private const float IconWidth = 135f;
    private const float IconHeight = 165f;
    private const float IconLocalY = 22.5f;

    [MenuItem("Tools/Fake Route/Choice Layout Preview")]
    private static void Open()
    {
        GetWindow<FakeRouteChoiceLayoutWindow>("Fake Route Choice Layout");
    }

    private void OnEnable()
    {
        var guids = AssetDatabase.FindAssets("t:FakeRouteStageConfig");
        if (guids.Length > 0)
            _route = AssetDatabase.LoadAssetAtPath<FakeRouteStageConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
        _arrowSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/RouteChoiceArrow_Test.png");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Fake Route Choice Layout", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("选择节点资产后，在下方预览中直接拖动箭头调整位置；拖动右侧旋转手柄调整旋转。修改会保存到对应出口配置。", MessageType.Info);
        EditorGUI.BeginChangeCheck();
        _route = (FakeRouteStageConfig)EditorGUILayout.ObjectField("Route", _route, typeof(FakeRouteStageConfig), false);
        if (EditorGUI.EndChangeCheck())
            _nodeIndex = 0;

        if (_route == null || _route.nodes == null || _route.nodes.Count == 0)
        {
            EditorGUILayout.HelpBox("请指定 FakeRouteStageConfig。", MessageType.Warning);
            return;
        }

        var nodeNames = new string[_route.nodes.Count];
        for (int i = 0; i < _route.nodes.Count; i++)
            nodeNames[i] = _route.nodes[i] == null ? "<null>" : _route.nodes[i].nodeId + "  " + _route.nodes[i].displayName;
        _nodeIndex = Mathf.Clamp(_nodeIndex, 0, nodeNames.Length - 1);
        _nodeIndex = EditorGUILayout.Popup("Node", _nodeIndex, nodeNames);

        FakeRouteNodeConfig node = _route.nodes[_nodeIndex];
        if (node == null || node.isFinalNode)
        {
            EditorGUILayout.HelpBox("终点节点没有路线选择按钮。", MessageType.Info);
            return;
        }
        if (node.outgoingChoices == null || node.outgoingChoices.Count == 0)
        {
            EditorGUILayout.HelpBox("当前节点没有出口。", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("节点：" + node.displayName, EditorStyles.boldLabel);
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        Rect preview = GUILayoutUtility.GetRect(PreviewWidth, PreviewHeight, GUILayout.ExpandWidth(false));
        DrawPreview(preview, node);
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("恢复当前节点默认布局"))
        {
            Undo.RecordObject(node, "Reset Route Choice Layout");
            for (int i = 0; i < node.outgoingChoices.Count; i++)
            {
                var choice = node.outgoingChoices[i];
                if (choice == null) continue;
                if (choice.layout == null) choice.layout = new RouteChoiceLayout();
                choice.layout.overrideDefault = false;
            }
            EditorUtility.SetDirty(node);
            AssetDatabase.SaveAssets();
            Repaint();
        }
    }

    private void DrawPreview(Rect area, FakeRouteNodeConfig node)
    {
        EditorGUI.DrawRect(area, new Color(0.12f, 0.12f, 0.12f));
        GUI.Box(area, GUIContent.none);
        float screenScale = area.height / ReferenceScreenHeight;
        Rect panel = new Rect(area.x + (area.width - PanelWidth * screenScale) * 0.5f, area.y + (area.height - ReferenceScreenHeight * screenScale) * 0.5f + (ReferenceScreenHeight - PanelHeight) * screenScale * 0.5f, PanelWidth * screenScale, PanelHeight * screenScale);
        Vector2 origin = panel.center;
        float scale = panel.width / PanelWidth;
        for (int i = 0; i < node.outgoingChoices.Count; i++)
        {
            var choice = node.outgoingChoices[i];
            if (choice == null || choice.targetNode == null) continue;
            if (choice.layout == null) choice.layout = new RouteChoiceLayout();
            Vector2 position = choice.layout.overrideDefault ? choice.layout.anchoredPosition : GetDefaultPosition(i, node.outgoingChoices.Count);
            float rotation = choice.layout.overrideDefault ? choice.layout.rotation : 0f;
            Vector2 guiPosition = new Vector2(position.x, -position.y);
            DrawChoice(area, origin + guiPosition * scale, rotation, choice, node, i, scale);
        }
        Handles.BeginGUI();
        Handles.color = new Color(1f, 1f, 1f, 0.18f);
        Handles.DrawLine(new Vector3(origin.x - 12f, origin.y), new Vector3(origin.x + 12f, origin.y));
        Handles.DrawLine(new Vector3(origin.x, origin.y - 12f), new Vector3(origin.x, origin.y + 12f));
        Handles.EndGUI();
        GUI.Label(new Rect(area.x + 8f, area.y + 8f, 260f, 20f), "2:3 Screen · CanvasScaler 1080×1920", EditorStyles.miniLabel);
    }

    private void DrawChoice(Rect area, Vector2 center, float rotation, FakeRouteChoiceConfig choice, FakeRouteNodeConfig node, int index, float scale)
    {
        Matrix4x4 oldMatrix = GUI.matrix;
        Color oldColor = GUI.color;
        if (_arrowSprite != null)
        {
            Vector2 iconCenter = center + RotateVector(Vector2.up * -IconLocalY * scale, -rotation);
            Rect iconRect = new Rect(iconCenter.x - IconWidth * scale * 0.5f, iconCenter.y - IconHeight * scale * 0.5f, IconWidth * scale, IconHeight * scale);
            GUIUtility.RotateAroundPivot(-rotation, center);
            GUI.DrawTexture(iconRect, _arrowSprite.texture, ScaleMode.ScaleToFit, true);
        }
        GUI.color = oldColor;
        GUI.matrix = oldMatrix;

        DrawInteractiveHandles(area, center, rotation, choice, node, scale);
    }

    private void DrawInteractiveHandles(Rect area, Vector2 center, float rotation, FakeRouteChoiceConfig choice, FakeRouteNodeConfig node, float scale)
    {
        Vector2 rotationHandle = center + RotateVector(Vector2.right * 75f * scale, -rotation);
        Handles.BeginGUI();
        Handles.color = Color.cyan;
        Handles.DrawLine(center, rotationHandle);
        Handles.DrawSolidDisc(center, Vector3.forward, 5f);
        Handles.DrawWireDisc(rotationHandle, Vector3.forward, 6f);
        Handles.EndGUI();

        Event evt = Event.current;
        Rect moveRect = new Rect(center - Vector2.one * 10f, Vector2.one * 20f);
        Rect rotateRect = new Rect(rotationHandle - Vector2.one * 10f, Vector2.one * 20f);
        if (evt.type == EventType.MouseDown && evt.button == 0)
        {
            if (rotateRect.Contains(evt.mousePosition))
            {
                _draggedChoice = choice;
                _draggingRotation = true;
                evt.Use();
            }
            else if (moveRect.Contains(evt.mousePosition))
            {
                _draggedChoice = choice;
                _draggingRotation = false;
                evt.Use();
            }
        }
        else if (evt.type == EventType.MouseDrag && _draggedChoice == choice)
        {
            Undo.RecordObject(node, _draggingRotation ? "Rotate Route Choice Button" : "Move Route Choice Button");
            EnsureCustomLayout(choice);
            if (_draggingRotation)
            {
                Vector2 direction = evt.mousePosition - center;
                if (direction.sqrMagnitude > 0.001f)
                    choice.layout.rotation = -Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            }
            else
            {
                Vector2 guiOffset = (evt.mousePosition - area.center) / scale;
                choice.layout.anchoredPosition = new Vector2(guiOffset.x, -guiOffset.y);
            }
            EditorUtility.SetDirty(node);
            Repaint();
            evt.Use();
        }
        else if (evt.type == EventType.MouseUp && _draggedChoice == choice)
        {
            AssetDatabase.SaveAssets();
            _draggedChoice = null;
            evt.Use();
        }
    }

    private static void DrawArrow(Vector2 center, float size, float rotation)
    {
        Vector2 up = RotateVector(Vector2.up * size, rotation);
        Vector2 right = RotateVector(Vector2.right * size * 0.55f, rotation);
        Handles.color = new Color(1f, 0.75f, 0.1f);
        Handles.DrawLine(center - up, center + up);
        Handles.DrawLine(center + up, center - up * 0.45f - right);
        Handles.DrawLine(center + up, center - up * 0.45f + right);
    }

    private static Vector2 RotateVector(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos);
    }

    private static Vector2 GetDefaultPosition(int index, int count)
    {
        if (count <= 1) return Vector2.zero;
        return new Vector2((index - (count - 1) * 0.5f) * 160f, 0f);
    }

    private static void EnsureCustomLayout(FakeRouteChoiceConfig choice)
    {
        if (choice.layout == null) choice.layout = new RouteChoiceLayout();
        choice.layout.overrideDefault = true;
    }
}
#endif
