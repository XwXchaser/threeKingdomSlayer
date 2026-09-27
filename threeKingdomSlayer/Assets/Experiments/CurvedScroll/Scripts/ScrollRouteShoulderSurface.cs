using UnityEngine;

/// <summary>Editable roadside strip following a route connection.</summary>
[ExecuteAlways, DisallowMultipleComponent]
public sealed class ScrollRouteShoulderSurface : MonoBehaviour
{
    public ScrollRouteConnection connection;
    public Material material;
    public Color editorColor = new Color(0.55f, 0.42f, 0.2f, 1f);
    [Min(0.05f)] public float width = 1f;
    public bool leftSide = true;
    MeshFilter filter;
    MeshRenderer meshRenderer;

    void OnEnable() { if (!connection) connection = GetComponentInParent<ScrollRouteConnection>(); ApplyVisibility(); Rebuild(); }
    void OnValidate() { if (!Application.isPlaying) { ApplyVisibility(); Rebuild(); } }
    void ApplyVisibility() { if (meshRenderer) meshRenderer.enabled = !Application.isPlaying; }
    public void Rebuild()
    {
        if (connection == null || connection.pathPoints == null || connection.pathPoints.Length < 2) return;
        filter = GetComponent<MeshFilter>();
        if (!filter) filter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        if (!meshRenderer) meshRenderer = gameObject.AddComponent<MeshRenderer>();
        if (!meshRenderer) return;
        meshRenderer.enabled = !Application.isPlaying;
        if (material) meshRenderer.sharedMaterial = material;
        else
        {
            var fallback = new Material(Shader.Find("Unlit/Color")) { color = editorColor };
            meshRenderer.sharedMaterial = fallback;
        }
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        int count = connection.pathPoints.Length; var v = new Vector3[count * 2]; var uv = new Vector2[v.Length]; var tri = new int[(count - 1) * 6];
        for (int i=0;i<count;i++)
        {
            Vector3 center=transform.InverseTransformPoint(connection.pathPoints[i].position);
            Vector3 tangent=i==0?connection.pathPoints[1].position-connection.pathPoints[0].position:(i==count-1?connection.pathPoints[count-1].position-connection.pathPoints[count-2].position:connection.pathPoints[i+1].position-connection.pathPoints[i-1].position);
            tangent.y=0; tangent.Normalize(); Vector3 side=new Vector3(tangent.z,0,-tangent.x)*(leftSide?-1:1); float inner=connection.roadWidth*.5f;
            v[i*2]=center+side*inner;v[i*2+1]=center+side*(inner+width);uv[i*2]=new Vector2(0,i);uv[i*2+1]=new Vector2(1,i);
            if(i>=count-1)continue;int t=i*6,a=i*2;tri[t]=a;tri[t+1]=a+2;tri[t+2]=a+1;tri[t+3]=a+1;tri[t+4]=a+2;tri[t+5]=a+3;
        }
        var mesh=new Mesh{name="Generated Route Shoulder - "+connection.ConnectionId};mesh.vertices=v;mesh.uv=uv;mesh.triangles=tri;mesh.RecalculateNormals();mesh.RecalculateBounds();filter.sharedMesh=mesh;
    }
}
