using UnityEngine;

/// <summary>Editable road surface belonging to one route connection. Phase2 authoring only.</summary>
[ExecuteAlways, DisallowMultipleComponent]
public sealed class ScrollRouteRoadSurface : MonoBehaviour
{
    public ScrollRouteConnection connection;
    public Material roadMaterial;
    [Min(0.1f)] public float width = 5f;
    [Min(0.1f)] public float segmentWidth = 8f;
    [Min(0.01f)] public float sideDepth = 0.35f;
    public bool rebuildInEditor = true;
    public Color editorColor = new Color(0.35f, 0.25f, 0.12f, 1f);
    MeshFilter filter;
    MeshRenderer meshRenderer;
    bool suppress;

    void OnEnable() { if (!connection) connection = GetComponentInParent<ScrollRouteConnection>(); ApplyVisibility(); Rebuild(); }
    void OnValidate() { if (!Application.isPlaying) { ApplyVisibility(); Rebuild(); } }
    void ApplyVisibility() { if (meshRenderer) meshRenderer.enabled = !Application.isPlaying; }
    void EnsureComponents()
    {
        if (!filter) filter = GetComponent<MeshFilter>();
        if (!filter) filter = gameObject.AddComponent<MeshFilter>();
        if (!meshRenderer) meshRenderer = GetComponent<MeshRenderer>();
        if (!meshRenderer) meshRenderer = gameObject.AddComponent<MeshRenderer>();
        if (!meshRenderer) return;
        meshRenderer.enabled = !Application.isPlaying;
        if (roadMaterial) meshRenderer.sharedMaterial = roadMaterial;
        else
        {
            var fallback = new Material(Shader.Find("Unlit/Color")) { color = editorColor };
            meshRenderer.sharedMaterial = fallback;
        }
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }
    public void Rebuild()
    {
        if (suppress || connection == null || connection.pathPoints == null || connection.pathPoints.Length < 2) return;
        EnsureComponents();
        suppress = true;
        var old = filter.sharedMesh;
        if (old && old.name.StartsWith("Generated Route Road")) DestroyImmediate(old);
        int count = connection.pathPoints.Length;
        var vertices = new Vector3[count * 2];
        var uv = new Vector2[vertices.Length];
        var triangles = new int[(count - 1) * 6];
        float accumulated = 0f;
        for (int i = 0; i < count; i++)
        {
            Vector3 center = transform.InverseTransformPoint(connection.pathPoints[i].position);
            Vector3 tangent = i == 0 ? connection.pathPoints[1].position - connection.pathPoints[0].position :
                (i == count - 1 ? connection.pathPoints[count - 1].position - connection.pathPoints[count - 2].position : connection.pathPoints[i + 1].position - connection.pathPoints[i - 1].position);
            tangent.y = 0f; tangent.Normalize();
            Vector3 side = new Vector3(tangent.z, 0f, -tangent.x);
            float half = width * 0.5f;
            vertices[i * 2] = center - side * half;
            vertices[i * 2 + 1] = center + side * half;
            if (i > 0) accumulated += Vector3.Distance(connection.pathPoints[i - 1].position, connection.pathPoints[i].position);
            uv[i * 2] = new Vector2(0f, accumulated / segmentWidth);
            uv[i * 2 + 1] = new Vector2(1f, accumulated / segmentWidth);
            if (i >= count - 1) continue;
            int t = i * 6, v = i * 2;
            triangles[t] = v; triangles[t + 1] = v + 2; triangles[t + 2] = v + 1;
            triangles[t + 3] = v + 1; triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
        }
        var mesh = new Mesh { name = "Generated Route Road - " + connection.ConnectionId };
        mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
        filter.sharedMesh = mesh;
        suppress = false;
    }
}
