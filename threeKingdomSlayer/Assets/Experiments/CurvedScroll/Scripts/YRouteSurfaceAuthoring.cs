using UnityEngine;

/// <summary>Persistent, scene-authored Y-branch road/shoulder surface. Mesh is built in Edit Mode and never rebuilt in Play Mode.</summary>
[ExecuteAlways, DisallowMultipleComponent]
public sealed class YRouteSurfaceAuthoring : MonoBehaviour
{
    public enum SurfaceKind { Road, Shoulder }
    public YScrollSample route;
    public Transform[] pathPoints = System.Array.Empty<Transform>();
    public SurfaceKind kind = SurfaceKind.Road;
    public bool leftSide = true;
    [Min(0.1f)] public float roadWidth = 5f;
    [Min(0.05f)] public float shoulderWidth = 2f;
    [Min(0.01f)] public float segmentLength = 4f;
    public Material surfaceMaterial;
    public bool rebuildInEditor = true;

    MeshFilter filter;
    MeshRenderer meshRenderer;

    void OnEnable()
    {
        EnsureComponents();
        if (!Application.isPlaying && rebuildInEditor && HasPath()) Rebuild();
    }

    void OnValidate()
    {
        if (!Application.isPlaying && rebuildInEditor && HasPath()) Rebuild();
    }

    bool HasPath() => pathPoints != null && pathPoints.Length >= 2 && System.Array.TrueForAll(pathPoints, p => p != null);

    void EnsureComponents()
    {
        if (!filter) filter = GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>();
        if (!meshRenderer) meshRenderer = GetComponent<MeshRenderer>() ?? gameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = surfaceMaterial;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.enabled = true;
    }

    public void Rebuild()
    {
        if (Application.isPlaying || !HasPath()) return;
        EnsureComponents();
        var points = new Vector3[pathPoints.Length];
        for (int i = 0; i < points.Length; i++) points[i] = transform.InverseTransformPoint(pathPoints[i].position);
        float halfRoad = roadWidth * 0.5f;
        int count = points.Length;
        var vertices = new Vector3[count * 2];
        var uv = new Vector2[vertices.Length];
        var triangles = new int[(count - 1) * 6];
        float distance = 0f;
        for (int i = 0; i < count; i++)
        {
            Vector3 tangent = i == 0 ? points[1] - points[0] : i == count - 1 ? points[count - 1] - points[count - 2] : points[i + 1] - points[i - 1];
            tangent.y = 0f;
            tangent.Normalize();
            Vector3 side = new Vector3(tangent.z, 0f, -tangent.x);
            float inner = kind == SurfaceKind.Road ? -halfRoad : (leftSide ? -halfRoad - shoulderWidth : halfRoad);
            float outer = kind == SurfaceKind.Road ? halfRoad : (leftSide ? -halfRoad : halfRoad + shoulderWidth);
            vertices[i * 2] = points[i] + side * inner;
            vertices[i * 2 + 1] = points[i] + side * outer;
            if (i > 0) distance += Vector3.Distance(points[i - 1], points[i]);
            uv[i * 2] = new Vector2(0f, distance / Mathf.Max(0.01f, segmentLength));
            uv[i * 2 + 1] = new Vector2(1f, distance / Mathf.Max(0.01f, segmentLength));
            if (i >= count - 1) continue;
            int t = i * 6;
            int v = i * 2;
            triangles[t] = v; triangles[t + 1] = v + 2; triangles[t + 2] = v + 1;
            triangles[t + 3] = v + 1; triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
        }
        var mesh = filter.sharedMesh;
        if (!mesh || !mesh.name.StartsWith("YRouteSurface")) mesh = new Mesh();
        mesh.name = "YRouteSurface - " + kind + " - " + (leftSide ? "Left" : "Right");
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        filter.sharedMesh = mesh;
        UnityEditor.EditorUtility.SetDirty(mesh);
        UnityEditor.EditorUtility.SetDirty(gameObject);
    }
}
