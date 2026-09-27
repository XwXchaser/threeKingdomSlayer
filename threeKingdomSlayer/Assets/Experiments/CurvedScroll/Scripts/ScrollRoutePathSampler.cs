using System;
using UnityEngine;

[Serializable]
public struct ScrollRouteSample
{
    public Vector3 position;
    public Vector3 tangent;
    public float distance;
    public float angle;
}

[ExecuteAlways, DisallowMultipleComponent]
public sealed class ScrollRoutePathSampler : MonoBehaviour
{
    public ScrollRouteConnection connection;
    [Min(8)] public int samples = 64;
    [SerializeField] float[] distances = Array.Empty<float>();
    [SerializeField] Vector3[] positions = Array.Empty<Vector3>();
    [SerializeField] Vector3[] tangents = Array.Empty<Vector3>();
    public float TotalLength { get; private set; }
    public ScrollRouteConnection Connection => connection;

    void OnEnable() { if (!connection) connection = GetComponentInParent<ScrollRouteConnection>(); Rebuild(); }
    void OnValidate() { if (!Application.isPlaying) Rebuild(); }
    public void Rebuild()
    {
        if (!connection || connection.pathPoints == null || connection.pathPoints.Length < 2) { TotalLength = 0f; return; }
        int count = Mathf.Max(8, samples);
        var source = connection.pathPoints;
        var sourcePositions = new Vector3[source.Length];
        for (int i = 0; i < source.Length; i++) sourcePositions[i] = source[i] ? source[i].position : connection.transform.position;
        var cumulative = new float[source.Length];
        for (int i = 1; i < source.Length; i++) cumulative[i] = cumulative[i - 1] + Vector3.Distance(sourcePositions[i - 1], sourcePositions[i]);
        TotalLength = cumulative[source.Length - 1];
        distances = new float[count]; positions = new Vector3[count]; tangents = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            float d = TotalLength * i / (count - 1f); distances[i] = d;
            int segment = 0; while (segment < cumulative.Length - 2 && cumulative[segment + 1] < d) segment++;
            float length = Mathf.Max(0.0001f, cumulative[segment + 1] - cumulative[segment]);
            float t = Mathf.InverseLerp(cumulative[segment], cumulative[segment + 1], d);
            positions[i] = Vector3.Lerp(sourcePositions[segment], sourcePositions[segment + 1], t);
            Vector3 tangent = sourcePositions[segment + 1] - sourcePositions[segment]; tangent.y = 0f;
            tangents[i] = tangent.sqrMagnitude > 0.0001f ? tangent.normalized : Vector3.forward;
        }
    }
    public ScrollRouteSample EvaluateNormalized(float normalized)
    {
        float d = Mathf.Clamp01(normalized) * TotalLength;
        return EvaluateDistance(d);
    }
    public ScrollRouteSample EvaluateDistance(float distance)
    {
        if (positions == null || positions.Length == 0) Rebuild();
        if (positions.Length == 0) return new ScrollRouteSample { position = transform.position, tangent = transform.forward };
        distance = Mathf.Clamp(distance, 0f, TotalLength);
        int index = 0; while (index < distances.Length - 2 && distances[index + 1] < distance) index++;
        float t = Mathf.InverseLerp(distances[index], distances[index + 1], distance);
        Vector3 tangent = Vector3.Slerp(tangents[index], tangents[index + 1], t).normalized;
        return new ScrollRouteSample
        {
            position = Vector3.Lerp(positions[index], positions[index + 1], t),
            tangent = tangent,
            distance = distance,
            angle = Mathf.Atan2(tangent.x, tangent.z) * Mathf.Rad2Deg
        };
    }
}
