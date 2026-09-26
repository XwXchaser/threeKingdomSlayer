#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ScrollWorldPreviewWindow : EditorWindow
{
    ScrollWorldPreviewTrial preview;
    Vector2 scroll;
    bool autoPlay;
    double lastTime;

    [MenuItem("Tools/Curved Scroll/World Preview")]
    static void Open()
    {
        // Legacy menu redirects to the persistent editable scene, never regenerates preview trees.
        ScrollWorldAuthoringBuilder.SelectWorld();
    }

    void OnEnable()
    {
        lastTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += Tick;
        SceneView.duringSceneGui += DrawSceneOverlay;
        FindPreview();
    }

    void OnDisable()
    {
        EditorApplication.update -= Tick;
        SceneView.duringSceneGui -= DrawSceneOverlay;
        if (preview != null) preview.ClearEditorPreview();
    }

    void FindPreview()
    {
        preview = FindObjectOfType<ScrollWorldPreviewTrial>();
        if (preview == null && SceneManager.GetActiveScene().isLoaded)
            ShowNotification(new GUIContent("当前场景没有 ScrollWorldPreviewTrial"));
    }

    void Tick()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (preview == null) { FindPreview(); Repaint(); return; }
        if (autoPlay && preview.editorPreview)
        {
            double now = EditorApplication.timeSinceStartup;
            float delta = Mathf.Clamp((float)(now - lastTime), 0f, 0.1f);
            preview.SetEditorPreviewNormalized(preview.editorPreviewNormalized + delta / Mathf.Max(1f, preview.PreviewTotalLength) * 12f);
            if (preview.editorPreviewNormalized >= 0.999f) autoPlay = false;
        }
        lastTime = EditorApplication.timeSinceStartup;
        Repaint();
    }

    void OnGUI()
    {
        if (preview == null)
        {
            EditorGUILayout.HelpBox("请打开包含 ScrollWorldPreviewTrial 的卷轴测试场景。", MessageType.Info);
            if (GUILayout.Button("重新查找当前场景")) FindPreview();
            return;
        }

        var sequence = preview.sequence;
        EditorGUILayout.LabelField("连续卷轴整段预览", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("仅编辑器预览。对象使用 DontSave，不会进入运行时战斗，也不会保存到场景。关闭窗口会清理预览对象。", MessageType.Info);
        using (new EditorGUI.DisabledScope(sequence == null))
        {
            bool enabled = EditorGUILayout.ToggleLeft("显示编辑器预览", preview.editorPreview);
            if (enabled != preview.editorPreview)
            {
                Undo.RecordObject(preview, "Toggle Scroll Preview");
                preview.editorPreview = enabled;
                if (enabled) preview.RefreshEditorPreview(); else preview.ClearEditorPreview();
                EditorUtility.SetDirty(preview);
            }

            float normalized = EditorGUILayout.Slider("整段距离", preview.editorPreviewNormalized, 0f, 1f);
            if (!Mathf.Approximately(normalized, preview.editorPreviewNormalized))
            {
                Undo.RecordObject(preview, "Seek Scroll Preview");
                preview.SetEditorPreviewNormalized(normalized);
                EditorUtility.SetDirty(preview);
            }

            float distance = sequence != null ? normalized * preview.PreviewTotalLength : 0f;
            EditorGUILayout.LabelField("当前距离", distance.ToString("F1") + " / " + preview.PreviewTotalLength.ToString("F1"));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("从头")) preview.SetEditorPreviewNormalized(0f);
            if (GUILayout.Button("播放/暂停")) { autoPlay = !autoPlay; lastTime = EditorApplication.timeSinceStartup; }
            if (GUILayout.Button("末尾")) preview.SetEditorPreviewNormalized(1f);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("聚焦 Scene View")) FocusSceneView();
            if (GUILayout.Button("清理临时预览对象")) preview.ClearEditorPreview();
        }

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("环境区段", EditorStyles.boldLabel);
        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(90f));
        if (sequence != null && sequence.segments != null)
        {
            foreach (var segment in sequence.segments)
            {
                if (segment == null) continue;
                Rect rect = EditorGUILayout.GetControlRect(false, 18f);
                float start = segment.startDistance / Mathf.Max(1f, preview.PreviewTotalLength);
                float end = segment.EndDistance / Mathf.Max(1f, preview.PreviewTotalLength);
                EditorGUI.DrawRect(new Rect(rect.x + rect.width * start, rect.y, rect.width * Mathf.Max(0.01f, end - start), rect.height),
                    segment.profile != null && segment.profile.environmentMode == ScrollEnvironmentMode.ValleyRoad ? new Color(0.25f, 0.55f, 0.3f) : new Color(0.55f, 0.35f, 0.2f));
                EditorGUI.LabelField(rect, segment.segmentId + "  " + segment.startDistance.ToString("F0") + "–" + segment.EndDistance.ToString("F0"));
            }
        }
        EditorGUILayout.EndScrollView();

        if (GUI.changed) SceneView.RepaintAll();
    }

    void FocusSceneView()
    {
        if (preview == null || preview.lab == null) return;
        var view = SceneView.lastActiveSceneView;
        if (view == null) view = EditorWindow.GetWindow<SceneView>();
        if (view == null) return;
        view.LookAt(preview.lab.transform.position + Vector3.forward * (float)preview.lab.Distance, Quaternion.Euler(18f, 0f, 0f), 45f);
        view.Repaint();
    }

    void DrawSceneOverlay(SceneView sceneView)
    {
        if (preview == null || !preview.editorPreview || preview.sequence == null) return;
        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(12f, 12f, 300f, 46f), "卷轴预览", GUI.skin.window);
        GUILayout.Label("距离 " + (preview.editorPreviewNormalized * preview.PreviewTotalLength).ToString("F1") + " / " + preview.PreviewTotalLength.ToString("F1"));
        GUILayout.EndArea();
        Handles.EndGUI();
    }
}
#endif
