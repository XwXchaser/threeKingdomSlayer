using System.Collections.Generic;
using UnityEngine;

/// <summary>Scene-authored flat world; only the assigned Game camera sees shader scroll projection.</summary>
[ExecuteAlways, DisallowMultipleComponent]
public sealed class ScrollWorldAuthoring : MonoBehaviour
{
    public Camera gameCamera;
    [Min(1f)] public float totalDistance = 175f;
    [Header("Edit Mode / Game view")]
    public bool previewInGame = true;
    [Min(0f)] public float previewDistance;
    [Header("Projection (visual only)")]
    [Min(0f)] public float bendStart = 20f;
    [Range(0f, 0.04f)] public float curvature = 0.012f;
    [Min(0.1f)] public float transitionLength = 6f;
    [Min(10f)] public float viewDistance = 80f;
    public float nearDistance = -14f;
    [System.NonSerialized] float runtimeDistance;
    readonly HashSet<ScrollWorldItem> items = new HashSet<ScrollWorldItem>();
    public float Distance => Application.isPlaying ? runtimeDistance : previewDistance;
    public int ItemCount => items.Count;
    public void Register(ScrollWorldItem item) { if (item) items.Add(item); }
    public void Unregister(ScrollWorldItem item) { items.Remove(item); }
    public void SetDistance(float distance) { runtimeDistance = Mathf.Max(0f, distance); }

    void OnEnable()
    {
        items.Clear();
        foreach (var item in GetComponentsInChildren<ScrollWorldItem>(true))
        {
            item.world = this;
            items.Add(item);
        }
        if (Application.isPlaying) runtimeDistance = 0f;
        Camera.onPreCull += BeforeCamera;
        Camera.onPostRender += AfterCamera;
    }
    void OnDisable()
    {
        Camera.onPreCull -= BeforeCamera;
        Camera.onPostRender -= AfterCamera;
        ResetProjection();
    }
    void OnValidate() { previewDistance = Mathf.Clamp(previewDistance, 0f, totalDistance); }
    void BeforeCamera(Camera camera)
    {
        ResetProjection();
        if (!isActiveAndEnabled || camera != gameCamera || (!Application.isPlaying && !previewInGame)) return;
        Shader.SetGlobalMatrix("_ScrollWorldToLocal", transform.worldToLocalMatrix);
        Shader.SetGlobalMatrix("_ScrollLocalToWorld", transform.localToWorldMatrix);
        Shader.SetGlobalFloat("_ScrollDistance", Distance);
        Shader.SetGlobalVector("_ScrollBend", new Vector4(bendStart, curvature, transitionLength, viewDistance));
        Shader.SetGlobalFloat("_ScrollNear", nearDistance);
        Shader.SetGlobalFloat("_ScrollProjectionEnabled", 1f);
        foreach (var item in items)
            if (item && item.isActiveAndEnabled && item.gameObject.activeInHierarchy) item.PrepareForGame();
    }
    void AfterCamera(Camera camera) { ResetProjection(); }
    public void ResetProjection()
    {
        Shader.SetGlobalFloat("_ScrollProjectionEnabled", 0f);
        foreach (var item in items) if (item) item.RestoreBounds();
    }
}
