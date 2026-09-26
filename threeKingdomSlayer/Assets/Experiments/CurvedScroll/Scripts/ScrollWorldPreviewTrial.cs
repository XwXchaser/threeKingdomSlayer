using UnityEngine;

/// <summary>Visual-only route storyboard preview. Does not start battle or modify combat objects.</summary>
public sealed class ScrollWorldPreviewTrial : MonoBehaviour
{
    public CurvedScrollLab lab;
    public ValleyArtPresentation art;
    public ScrollWorldSequence sequence;
    public bool startOpen;
    public bool hideOnScreenControls;
    [Header("Editor preview")]
    public bool editorPreview = true;
    [Range(0f, 1f)] public float editorPreviewNormalized;
    [Tooltip("编辑器预览时是否在场景视图显示运行时卷轴对象。")]
    public bool editorPreviewVisible = true;
    bool open;
    float normalized;

    void Awake()
    {
        open = startOpen;
        normalized = editorPreviewNormalized;
        if (!lab) lab = FindObjectOfType<CurvedScrollLab>();
        if (!art && lab) art = lab.GetComponent<ValleyArtPresentation>();
    }

    void OnValidate()
    {
        // Legacy preview no longer generates objects in the authored scene.
        normalized = Mathf.Clamp01(editorPreviewNormalized);
    }

    void Update()
    {
        // Kept only for serialized compatibility. The editable world owns the Game preview.
        return;
    }

    public void RefreshEditorPreview()
    {
        if (Application.isPlaying || !editorPreview) return;
        ApplyPreview();
    }

    public void ClearEditorPreview()
    {
        if (Application.isPlaying) return;
        if (lab != null)
        {
            for (int i = lab.transform.childCount - 1; i >= 0; i--)
            {
                var child = lab.transform.GetChild(i);
                if (child.name == "Runtime visual objects (not saved)" || child.name == "Valley art runtime")
                    DestroyImmediate(child.gameObject);
            }
            var initializedField = typeof(CurvedScrollLab).GetField("initialized", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            initializedField?.SetValue(lab, false);
        }
        if (art != null)
        {
            for (int i = art.transform.childCount - 1; i >= 0; i--)
            {
                var child = art.transform.GetChild(i);
                if (child.name == "Valley art runtime") DestroyImmediate(child.gameObject);
            }
            var builtField = typeof(ValleyArtPresentation).GetField("built", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var contentField = typeof(ValleyArtPresentation).GetField("contentResolved", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            builtField?.SetValue(art, false);
            contentField?.SetValue(art, false);
        }
        #if UNITY_EDITOR
        UnityEditor.SceneView.RepaintAll();
        #endif
    }

    public void SetEditorPreviewNormalized(float value)
    {
        editorPreviewNormalized = Mathf.Clamp01(value);
        normalized = editorPreviewNormalized;
        RefreshEditorPreview();
    }

    public float PreviewTotalLength => TotalLength();

    void ApplyPreview()
    {
        var world = FindObjectOfType<ScrollWorldAuthoring>();
        if (!world || Application.isPlaying) return;
        world.previewDistance = Mathf.Clamp01(editorPreviewNormalized) * world.totalDistance;
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
        UnityEditor.SceneView.RepaintAll();
        #endif
    }
    void OnDrawGizmos()
    {
        if (!editorPreview || !sequence || sequence.segments == null) return;
        for (int i = 0; i < sequence.segments.Length; i++)
        {
            var segment = sequence.segments[i];
            if (segment == null) continue;
            Gizmos.color = i == 0 ? Color.green : (i == sequence.segments.Length - 1 ? Color.red : Color.yellow);
            float x = i * 0.4f;
            Gizmos.DrawLine(new Vector3(x, 0f, segment.startDistance), new Vector3(x, 0f, segment.EndDistance));
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(new Vector3(x, 0.5f, segment.startDistance), segment.segmentId + "  " + segment.startDistance + "–" + segment.EndDistance);
            #endif
        }
    }

    float TotalLength()
    {
        if (sequence.segments != null && sequence.segments.Length > 0)
        {
            var last = sequence.segments[sequence.segments.Length - 1];
            if (last != null) return last.EndDistance;
        }
        return 1f;
    }

    void OnGUI()
    {
        return; // Superseded by the ScrollWorldAuthoring component inspector.
    }
}
