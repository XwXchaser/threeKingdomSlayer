using UnityEngine;
using UnityEngine.UI;

/// <summary>Art-only adapter; never changes combat coordinates.</summary>
public sealed class ValleyArtPresentation : MonoBehaviour
{
    public CurvedScrollLab lab;
    public Material terrainMaterial;
    public Texture2D background;
    public Sprite[] nearSprites;
    public Sprite[] middleSprites;
    public Sprite[] shoulderSprites;
    [Range(0, .7f)] public float backgroundLift = .4f;
    public float tileSize=4;
    public float shoulderLength=8;
    public float shoulderWidth=1.1f;
    public float nearMargin=3;
    public float middleMargin=1.2f;
    [Header("Scenery density (counts take effect on next Play)")]
    [Range(4, 100)] public int largePropCount = 32;
    [Range(8, 200)] public int smallPropCount = 80;
    [Range(.2f, 3)] public float largePropScale = 1.3f;
    [Range(.1f, 2)] public float smallPropScale = .65f;
    [Min(0)] public float roadClearance = .35f;
    int activeLargeCount;
    public bool showShoulders=true;
    Transform root;
    SpriteRenderer[] props;
    Mesh[] strips;
    Vector3[][] stripVertices;
    GameObject[] stripObjects;
    MaterialPropertyBlock block;
    Renderer ground;
    GameObject canvas;
    static readonly int Half=Shader.PropertyToID("_HalfWidth"),Travel=Shader.PropertyToID("_Travel"),Tile=Shader.PropertyToID("_TileSize");
    System.Collections.IEnumerator Start()
    {
        if(!lab||!terrainMaterial||!background||nearSprites.Length==0||middleSprites.Length==0)yield break;
        lab.Initialize(); yield return null;
        root=new GameObject("Valley art runtime").transform;root.SetParent(lab.transform,false);
        ground=lab.GeneratedRoot.Find("Subdivided road").GetComponent<Renderer>();ground.sharedMaterial=terrainMaterial;block=new MaterialPropertyBlock();
        var depth=lab.GetComponent<ScrollDepthLayers>();if(depth){depth.enabled=false;depth.nearLayer=depth.middleLayer=depth.distantLayer=depth.groundDetails=false;depth.Refresh();}
        foreach(var r in lab.GeneratedRoot.GetComponentsInChildren<SpriteRenderer>())r.enabled=false;
        var backdrop=lab.GeneratedRoot.Find("Static distant silhouettes (placeholder)");if(backdrop)backdrop.GetComponent<Renderer>().enabled=false;
        canvas=new GameObject("Valley background canvas",typeof(Canvas));canvas.transform.SetParent(root,false);
        var c=canvas.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=lab.viewCamera;c.planeDistance=350;c.sortingOrder=-1000;
        var image=new GameObject("2x3 background",typeof(RectTransform),typeof(RawImage),typeof(AspectRatioFitter));image.transform.SetParent(canvas.transform,false);
        var raw=image.GetComponent<RawImage>();raw.texture=background;raw.raycastTarget=false;
        raw.uvRect=new Rect(0,-backgroundLift,1,1); // Lift distant valley above the curved terrain horizon without stretching.
        var fit=image.GetComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;fit.aspectRatio=2f/3f;
        activeLargeCount=Mathf.Clamp(largePropCount,4,100);
        props=new SpriteRenderer[activeLargeCount+Mathf.Clamp(smallPropCount,8,200)];
        for(int i=0;i<props.Length;i++){
            bool large=i<activeLargeCount;
            int index=large?i:i-activeLargeCount;
            var pool=large?nearSprites:middleSprites;
            // Alternate species across both sides, rather than binding even-index sprites to one side.
            var go=new GameObject(large?"Valley peak or tree":"Valley low scenery");go.transform.SetParent(root,false);var r=go.AddComponent<SpriteRenderer>();r.sharedMaterial=lab.spriteMaterial;r.sprite=pool[(index/2+index%2)%pool.Length];props[i]=r;
        }
        strips=new Mesh[20];stripVertices=new Vector3[20][];stripObjects=new GameObject[20];
        for(int i=0;i<strips.Length&&shoulderSprites.Length>0;i++){
            var s=shoulderSprites[i%shoulderSprites.Length];var mesh=new Mesh{name="Curved shoulder strip"};mesh.MarkDynamic();strips[i]=mesh;var v=new Vector3[34];stripVertices[i]=v;var uv=new Vector2[34];var tri=new int[96];var rect=s.rect;var tex=s.texture;
            for(int j=0;j<=16;j++){float u=Mathf.Lerp(rect.xMin,rect.xMax,j/16f)/tex.width;uv[j*2]=new Vector2(u,rect.yMin/tex.height);uv[j*2+1]=new Vector2(u,rect.yMax/tex.height);if(j==16)continue;int a=j*2,t=j*6;tri[t]=a;tri[t+1]=a+1;tri[t+2]=a+3;tri[t+3]=a;tri[t+4]=a+3;tri[t+5]=a+2;}
            mesh.vertices=v;mesh.uv=uv;mesh.triangles=tri;var colors=new Color[34];for(int j=0;j<34;j++)colors[j]=Color.white;mesh.colors=colors;
            var go=new GameObject("Road shoulder");go.transform.SetParent(root,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=lab.spriteMaterial;var mpb=new MaterialPropertyBlock();mpb.SetTexture("_MainTex",tex);r.SetPropertyBlock(mpb);stripObjects[i]=go;
        }
    }
    void LateUpdate()
    {
        if(!ground)return;
        float span=Mathf.Max(20,lab.viewDistance)+14,travel=(float)(lab.Distance%span),half=lab.roadWidth*.5f;
        block.SetFloat(Half,half);block.SetFloat(Travel,(float)(lab.Distance%(Mathf.Max(.1f,tileSize)*1024)));block.SetFloat(Tile,tileSize);ground.SetPropertyBlock(block);
        for(int i=0;i<props.Length;i++){
            bool near=i<activeLargeCount;int index=near?i:i-activeLargeCount,count=near?activeLargeCount:props.Length-activeLargeCount;
            float jitter=((index*17%13)/13f-.5f)*.55f;
            float z=-14+Mathf.Repeat((index+.37f+jitter)*span/count-travel,span);float side=index%2==0?-1:1;
            var r=props[i];float scale=Mathf.Max(.05f,near?largePropScale:smallPropScale);scale*=.8f+(i%5)*.1f;
            // Keep the whole sprite outside the road even when increasing its scale.
            float inwardExtent=side<0?r.sprite.bounds.max.x:-r.sprite.bounds.min.x;
            float margin=Mathf.Max(near?nearMargin:middleMargin,inwardExtent*scale+Mathf.Max(0,roadClearance));
            float x=side*(half+margin+(i*7%11)*.16f);
            r.transform.localScale=Vector3.one*scale;r.transform.localPosition=new Vector3(x,-lab.Drop(z)-r.sprite.bounds.min.y*scale,z);
            r.color=new Color(1,1,1,Mathf.Clamp01((lab.viewDistance-z)/10)*Mathf.Clamp01((z+14)/2));
        }
        for(int i=0;i<strips.Length;i++){
            if(!strips[i])continue;stripObjects[i].SetActive(showShoulders);float z=-14+Mathf.Repeat((i/2f)*span/10-travel,span);float side=i%2==0?-1:1;var v=stripVertices[i];
            for(int j=0;j<=16;j++){float depth=z+(j/16f-.5f)*shoulderLength;v[j*2]=new Vector3(side*(half-shoulderWidth*.5f),-lab.Drop(depth)+.025f,depth);v[j*2+1]=new Vector3(side*(half+shoulderWidth*.5f),-lab.Drop(depth)+.025f,depth);}
            strips[i].vertices=v;strips[i].RecalculateBounds();
        }
    }
    void OnDestroy(){if(strips!=null)foreach(var m in strips)if(m)Destroy(m);if(root)Destroy(root.gameObject);}
}
