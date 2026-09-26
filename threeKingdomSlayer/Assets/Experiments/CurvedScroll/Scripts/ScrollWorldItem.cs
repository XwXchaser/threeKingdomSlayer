using UnityEngine;

/// <summary>Persistent, editable scenery. Its Transform is always the unrolled authored pose.</summary>
[ExecuteAlways, DisallowMultipleComponent]
public sealed class ScrollWorldItem : MonoBehaviour
{
    public enum ItemKind { Scenery, Ground, Background }
    public ItemKind kind;
    public ScrollWorldAuthoring world;
    [Tooltip("Background only: route interval and incoming fade interval.")]
    public float segmentStart;
    public float segmentEnd = 175f;
    public float fadeStart;
    public bool lastBackground;
    Renderer cachedRenderer;
    Bounds savedBounds;
    bool boundsOverridden;
    MaterialPropertyBlock block;

    public Renderer ItemRenderer
    {
        get { if (!cachedRenderer) cachedRenderer = GetComponent<Renderer>(); return cachedRenderer; }
    }
    void OnEnable()
    {
        if (!world) world = GetComponentInParent<ScrollWorldAuthoring>();
        if (world) world.Register(this);
    }
    void OnDisable() { RestoreBounds(); if (world) world.Unregister(this); }
    void OnDestroy() { RestoreBounds(); if (world) world.Unregister(this); }

    public void PrepareForGame()
    {
        var r = ItemRenderer;
        if (!r || !world) return;
        if (block == null) block = new MaterialPropertyBlock();
        r.GetPropertyBlock(block);
        block.SetFloat("_ScrollAnchor", world.transform.InverseTransformPoint(transform.position).z);
        block.SetFloat("_SegmentStart", segmentStart);
        block.SetFloat("_SegmentEnd", segmentEnd);
        block.SetFloat("_FadeStart", fadeStart);
        block.SetFloat("_LastBackground", lastBackground ? 1f : 0f);
        r.SetPropertyBlock(block);
        // Shader-projected vertices are not inside the authored AABB. Override culling bounds
        // only for the Game camera; restore before Scene picking/framing or scene serialization.
        if (!boundsOverridden)
        {
            savedBounds = r.localBounds;
            r.localBounds = new Bounds(Vector3.zero, Vector3.one * 100000f);
            boundsOverridden = true;
        }
    }
    public void RestoreBounds()
    {
        if (!boundsOverridden) return;
        if (ItemRenderer) ItemRenderer.localBounds = savedBounds;
        boundsOverridden = false;
    }
}
