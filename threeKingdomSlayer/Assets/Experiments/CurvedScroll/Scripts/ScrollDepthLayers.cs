using System.Collections.Generic;
using UnityEngine;

/// <summary>Optional depth cues for the isolated lab. All geometry shares the same travel distance.</summary>
public sealed class ScrollDepthLayers : MonoBehaviour
{
    public CurvedScrollLab lab;
    public bool nearLayer = true, middleLayer = true, distantLayer = true, groundDetails = true;
    [Range(0, 1)] public float density = 0.7f;
    [Min(0)] public float nearMargin = 2.2f;
    [Min(0)] public float middleSpread = 7;
    public Vector2 nearScale = new Vector2(0.8f, 1.2f);
    public Vector2 middleScale = new Vector2(0.6f, 1.6f);
    public Color forestColor = new Color(.29f, .44f, .38f);
    [Range(-10, 20)] public float forestHeight = 3;
    public float forestOffset;
    public int seed = 123;

    struct Item { public Vector3 local; public float phase, size; public int kind; }
    readonly List<Mesh> ownedMeshes = new List<Mesh>();
    Item[] items;
    Transform[] objects;
    Mesh detailMesh, forestMesh;
    Vector3[] detailVertices, forestVertices;
    Color[] forestColors;
    Transform root;
    GameObject forest, details;
    bool ready;
    static readonly string[] Presets = { "Baseline", "Layers", "+ Ground" };
    int preset = 2;

    Mesh Polygon(string name, Vector3[] points, Color color)
    {
        var mesh = new Mesh { name = name };
        var colors = new Color[points.Length];
        var triangles = new int[(points.Length - 2) * 3];
        for (int i = 0; i < colors.Length; i++) colors[i] = color;
        for (int i = 0; i < points.Length - 2; i++) { triangles[i*3]=0; triangles[i*3+1]=i+1; triangles[i*3+2]=i+2; }
        mesh.vertices=points; mesh.colors=colors; mesh.triangles=triangles; mesh.RecalculateBounds();
        ownedMeshes.Add(mesh); return mesh;
    }
    GameObject Render(string name, Mesh mesh, Transform parent)
    {
        var go=new GameObject(name); go.transform.SetParent(parent,false);
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=go.AddComponent<MeshRenderer>(); renderer.sharedMaterial=lab.groundMaterial;
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows=false;
        return go;
    }
    static Vector3 V(float x, float y, float z = 0) { return new Vector3(x, y, z); }
    void Start()
    {
        if(lab==null || lab.groundMaterial==null){enabled=false; return;}
        root=new GameObject("Depth layer runtime geometry").transform; root.SetParent(lab.transform,false);
        var pole=Polygon("Flag pole",new[]{V(-.065f,0),V(.065f,0),V(.065f,6),V(-.065f,6)},new Color(.23f,.19f,.13f));
        var flag=Polygon("Flag cloth",new[]{V(0,5.9f),V(1.5f,5.5f),V(1.15f,4.5f),V(0,4.8f)},new Color(.49f,.21f,.12f));
        var rock=Polygon("Rock silhouette",new[]{V(-.9f,0),V(-.7f,.6f),V(-.1f,.85f),V(.7f,.55f),V(1,0)},new Color(.38f,.39f,.32f));
        var bush=Polygon("Bush silhouette",new[]{V(-1.2f,0),V(-1,.8f),V(-.5f,1.1f),V(0,.8f),V(.4f,1.4f),V(1,.9f),V(1.2f,0)},new Color(.18f,.30f,.19f));
        var random=new System.Random(seed);
        items=new Item[160]; objects=new Transform[80];
        for(int i=0;i<items.Length;i++)
        {
            int kind=i<16?0:(i<80?1:2);
            items[i]=new Item{kind=kind,phase=(i+(float)random.NextDouble())/items.Length,size=(float)random.NextDouble(),local=V((float)random.NextDouble(),0,(float)random.NextDouble())};
            // Each layer has its own staggered full-depth distribution.
            items[i].phase=kind==0?(i+(float)random.NextDouble())/16:kind==1?(i-16+(float)random.NextDouble())/64:(i-80+(float)random.NextDouble())/80;
            if(i>=objects.Length)continue;
            var go=new GameObject(kind==0?"Near flag":"Middle scenery"); go.transform.SetParent(root,false); objects[i]=go.transform;
            if(kind==0){Render("Pole",pole,go.transform); Render("Cloth",flag,go.transform);}
            else Render("Silhouette",i%2==0?rock:bush,go.transform);
        }
        detailVertices=new Vector3[80*4]; var dc=new Color[detailVertices.Length];var dt=new int[80*6];
        for(int i=0;i<80;i++)
        {
            for(int j=0;j<4;j++)dc[i*4+j]=i%3==0?new Color(.37f,.32f,.23f):new Color(.53f,.47f,.32f);
            int a=i*4,t=i*6;dt[t]=a;dt[t+1]=a+1;dt[t+2]=a+2;dt[t+3]=a;dt[t+4]=a+2;dt[t+5]=a+3;
        }
        detailMesh=new Mesh{name="Road detail patches"};ownedMeshes.Add(detailMesh);detailMesh.MarkDynamic();detailMesh.vertices=detailVertices;detailMesh.colors=dc;detailMesh.triangles=dt;
        details=Render("Ground details",detailMesh,root);
        forestVertices=new Vector3[162];forestColors=new Color[162];var ft=new int[480];
        for(int i=0;i<80;i++){int a=i*2,t=i*6;ft[t]=a;ft[t+1]=a+1;ft[t+2]=a+3;ft[t+3]=a;ft[t+4]=a+3;ft[t+5]=a+2;}
        forestMesh=new Mesh{name="Second distant silhouette"};ownedMeshes.Add(forestMesh);forestMesh.vertices=forestVertices;forestMesh.triangles=ft;
        forest=Render("Static distant forest",forestMesh,root);
        ready=true;Refresh();
    }
    void LateUpdate(){Refresh();}
    public void ApplyPreset(int index)
    {
        preset=Mathf.Clamp(index,0,2);
        nearLayer=middleLayer=distantLayer=preset>0;groundDetails=preset==2;
        if(lab!=null)lab.showRoadStripes=preset==0;
        Refresh();
    }
    public void Refresh()
    {
        if(!ready)return;
        float far=Mathf.Max(20,lab.viewDistance), span=far+14, cycle=(float)(lab.Distance%span), half=Mathf.Max(1.5f,lab.roadWidth*.5f);
        for(int i=0;i<items.Length;i++)
        {
            var item=items[i];float z=-14+Mathf.Repeat(item.phase*span-cycle,span);
            float visibility=Mathf.Clamp01((far-z)/10)*Mathf.Clamp01((z+14)/2);
            float x;
            if(i<objects.Length)
            {
                bool near=item.kind==0;
                bool active=(near?nearLayer:middleLayer)&&item.size<=density;
                objects[i].gameObject.SetActive(active);if(!active)continue;
                x=(half+(near?nearMargin:1)+item.local.x*(near?2:middleSpread))*(i%2==0?-1:1);
                float size=Mathf.Lerp(near?nearScale.x:middleScale.x,near?nearScale.y:middleScale.y,item.local.z);
                objects[i].localPosition=V(x,-lab.Drop(z),z);
                objects[i].localScale=V(size*(i%2==0?1:-1),size*visibility,size);
            }
            else
            {
                x=(item.local.x*2-1)*half*.9f;float width=.08f+item.size*.3f,length=.12f+item.local.z*.5f;
                if(!groundDetails||item.size>density)width=length=0;
                width*=visibility;length*=visibility;
                int k=(i-80)*4;
                detailVertices[k]=V(x-width,-lab.Drop(z-length)+.015f,z-length);
                detailVertices[k+1]=V(x+width,-lab.Drop(z-length)+.015f,z-length);
                detailVertices[k+2]=V(x+width*.7f,-lab.Drop(z+length)+.015f,z+length);
                detailVertices[k+3]=V(x-width*.6f,-lab.Drop(z+length)+.015f,z+length);
            }
        }
        details.SetActive(groundDetails);detailMesh.vertices=detailVertices;detailMesh.RecalculateBounds();
        forest.SetActive(distantLayer && lab.mode==CurvedScrollLab.ViewMode.CurvedWithBackdrop);
        for(int i=0;i<=80;i++)
        {
            float x=-160+i*4+forestOffset;
            float height=forestHeight+Mathf.Sin(i*.53f)*2+((i%2)==0?3:0);
            forestVertices[i*2]=V(x,-80,140);forestVertices[i*2+1]=V(x,height,140);
            forestColors[i*2]=forestColors[i*2+1]=forestColor;
        }
        forestMesh.vertices=forestVertices;forestMesh.colors=forestColors;forestMesh.RecalculateBounds();
    }
    void OnGUI()
    {
        if(lab==null||!lab.showControls)return;
        float scale=Mathf.Clamp(Screen.width/480f,.65f,1.5f);var old=GUI.matrix;GUI.matrix=Matrix4x4.Scale(V(scale,scale,1));
        GUILayout.BeginArea(new Rect(10,Screen.height/scale-150,300,140),GUI.skin.box);
        GUILayout.Label("DEPTH CUES / same travel speed");
        int next=GUILayout.Toolbar(preset,Presets);if(next!=preset)ApplyPreset(next);
        GUILayout.BeginHorizontal();nearLayer=GUILayout.Toggle(nearLayer,"Near");middleLayer=GUILayout.Toggle(middleLayer,"Middle");distantLayer=GUILayout.Toggle(distantLayer,"Far");GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();groundDetails=GUILayout.Toggle(groundDetails,"Ground");lab.showRoadStripes=GUILayout.Toggle(lab.showRoadStripes,"Stripes");GUILayout.EndHorizontal();
        density=GUILayout.HorizontalSlider(density,0,1);GUILayout.EndArea();GUI.matrix=old;
    }
    void OnDestroy(){foreach(var mesh in ownedMeshes)if(mesh!=null)Destroy(mesh);if(root!=null)Destroy(root.gameObject);}
}
