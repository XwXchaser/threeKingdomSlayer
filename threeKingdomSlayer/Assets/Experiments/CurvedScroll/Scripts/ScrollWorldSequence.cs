using System;
using UnityEngine;

[Serializable]
public class ScrollWorldEnvironmentSegment
{
    public string segmentId;
    [Min(0f)] public float startDistance;
    [Min(0.1f)] public float length = 40f;
    public ScrollRouteVisualProfile profile;
    [Range(0f, 1f)] public float blendIn = 0.15f;
    [Range(0f, 1f)] public float blendOut = 0.15f;

    public float EndDistance => startDistance + Mathf.Max(0.1f, length);
}

[CreateAssetMenu(fileName = "NewScrollWorldSequence", menuName = "一夫当关/连续卷轴场景序列")]
public sealed class ScrollWorldSequence : ScriptableObject
{
    [Tooltip("按 startDistance 从小到大排列。此序列只驱动卷轴视觉，不改变战斗坐标。")]
    public ScrollWorldEnvironmentSegment[] segments = Array.Empty<ScrollWorldEnvironmentSegment>();

    public bool TryGetSegment(float distance, out ScrollWorldEnvironmentSegment current, out ScrollWorldEnvironmentSegment next, out float normalized)
    {
        current = null;
        next = null;
        normalized = 0f;
        if (segments == null || segments.Length == 0) return false;
        ScrollWorldEnvironmentSegment latest = null;
        int latestIndex = -1;
        for (int i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            if (segment == null || segment.profile == null) continue;
            if (distance < segment.startDistance) break;
            latest = segment;
            latestIndex = i;
        }
        if (latest != null)
        {
            current = latest;
            next = latestIndex + 1 < segments.Length ? segments[latestIndex + 1] : null;
            normalized = Mathf.Clamp01((distance - latest.startDistance) / Mathf.Max(0.1f, latest.length));
            return true;
        }
        current = segments[0];
        next = segments.Length > 1 ? segments[1] : null;
        normalized = 0f;
        return current != null && current.profile != null;
    }
}
