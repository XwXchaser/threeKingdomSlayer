#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(ScrollWorldAuthoring))]
public sealed class ScrollWorldAuthoringEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.HelpBox("Hierarchy 下的 Layout 是真实保存的场景。直接移动/缩放/复制/删除子物件，Scene 始终平铺。下面的距离条只影响 Game 相机，不改动任何物件 Transform。", MessageType.Info);
        var world = (ScrollWorldAuthoring)target;
        DrawPropertiesExcluding(serializedObject, "m_Script", "previewDistance");
        if (world.previewConnection != null)
        {
            var sampler = world.previewConnection.GetComponent<ScrollRoutePathSampler>();
            if (!sampler) sampler = world.previewConnection.gameObject.AddComponent<ScrollRoutePathSampler>();
            EditorGUILayout.LabelField("Connection length", sampler.TotalLength.ToString("F1"));
        }
        var distance = serializedObject.FindProperty("previewDistance");
        using (new EditorGUI.DisabledScope(Application.isPlaying))
            distance.floatValue = EditorGUILayout.Slider("Game 预览距离", distance.floatValue, 0f, world.totalDistance);
        if (world.previewConnection != null)
        {
            var progress = serializedObject.FindProperty("previewProgress");
            progress.floatValue = EditorGUILayout.Slider("Connection Progress", progress.floatValue, 0f, 1f);
        }
        if (serializedObject.ApplyModifiedProperties()) { EditorApplication.QueuePlayerLoopUpdate(); SceneView.RepaintAll(); }
        EditorGUILayout.LabelField("当前显示距离", world.Distance.ToString("F2"));
        if (GUILayout.Button("Scene 聚焦整条平铺路线")) Frame(world);
        EditorGUILayout.HelpBox("地面：修改 MeshRenderer 的独立材质。侧景：修改 Transform / SpriteRenderer（图片、颜色、翻转、排序）。新增物件：复制已有侧景即可。Play 从距离 0 开始，不继承编辑预览距离。", MessageType.None);
    }
    public static void Frame(ScrollWorldAuthoring world)
    {
        var view = SceneView.lastActiveSceneView ? SceneView.lastActiveSceneView : EditorWindow.GetWindow<SceneView>();
        view.in2DMode = false;
        view.LookAt(world.transform.TransformPoint(new Vector3(0,0,world.totalDistance*.5f)), Quaternion.Euler(55, -25, 0), world.totalDistance*.65f, false, true);
    }
}

public static class ScrollWorldAuthoringBuilder
{
    const string Folder = "Assets/Experiments/CurvedScroll/Authoring";
    [MenuItem("Tools/Curved Scroll/Select Editable World")]
    public static void SelectWorld()
    {
        var world = Object.FindObjectOfType<ScrollWorldAuthoring>();
        if (!world) { Debug.LogWarning("当前场景尚未迁移。请使用 Create Editable World From Trial（只迁移一次）。"); return; }
        Selection.activeGameObject = world.gameObject;
        EditorGUIUtility.PingObject(world.gameObject);
        ScrollWorldAuthoringEditor.Frame(world);
    }
    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Experiments/CurvedScroll", "Authoring");
    }
    static GameObject Child(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }
    static Material MaterialAsset(string name, string shader)
    {
        var s = Shader.Find(shader);
        if (!s) throw new System.InvalidOperationException("Missing shader: " + shader);
        var mat = new Material(s) { name = name };
        AssetDatabase.CreateAsset(mat, AssetDatabase.GenerateUniqueAssetPath(Folder + "/" + name + ".mat"));
        return mat;
    }
    static Mesh GroundMesh(float begin, float end)
    {
        int rows = Mathf.CeilToInt((end-begin)*2);
        var v = new Vector3[(rows+1)*2]; var uv = new Vector2[v.Length]; var tri = new int[rows*6];
        for(int i=0;i<=rows;i++)
        {
            float z=Mathf.Lerp(begin,end,(float)i/rows);
            v[i*2]=new Vector3(-35,0,z);v[i*2+1]=new Vector3(35,0,z);
            uv[i*2]=new Vector2(-35,z);uv[i*2+1]=new Vector2(35,z);
            if(i==rows)continue;int k=i*6,a=i*2;
            tri[k]=a;tri[k+1]=a+2;tri[k+2]=a+1;tri[k+3]=a+1;tri[k+4]=a+2;tri[k+5]=a+3;
        }
        var mesh=new Mesh {name="Unrolled ground"};mesh.vertices=v;mesh.uv=uv;mesh.triangles=tri;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
    static void Prop(Transform parent, ScrollWorldAuthoring world, Material mat, string name, Sprite sprite, float x, float z, float scale, bool flip, int order, float roadHalf)
    {
        if(!sprite)return;
        // Author an unobstructed corridor. This is a one-off conversion, NOT a runtime clamp.
        float extent = Mathf.Max(Mathf.Abs(sprite.bounds.min.x),Mathf.Abs(sprite.bounds.max.x))*scale;
        float sign=x<0?-1:1;
        x=sign*Mathf.Max(Mathf.Abs(x),roadHalf+.5f+extent);
        var go=Child(name,parent);
        go.transform.localPosition=new Vector3(x,-sprite.bounds.min.y*scale,z);
        go.transform.localScale=Vector3.one*scale;
        var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sharedMaterial=mat;r.flipX=flip;r.sortingOrder=order;
        var item=go.AddComponent<ScrollWorldItem>();item.world=world;
    }
    [MenuItem("Tools/Curved Scroll/Create Editable World From Trial")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Edit Mode only");
        if(Object.FindObjectOfType<ScrollWorldAuthoring>()) { SelectWorld();return; }
        var flow=Object.FindObjectOfType<ScrollNodeFlowTrial>();
        if(!flow||!flow.worldSequence||!flow.lab)throw new System.InvalidOperationException("Missing trial scene bindings");
        var lab=flow.lab;var art=lab.GetComponent<ValleyArtPresentation>();
        if(lab.transform.childCount!=0)throw new System.InvalidOperationException("Clear obsolete transient preview roots before migration.");
        EnsureFolder();
        var root=new GameObject("Scroll World - Editable Layout");
        root.transform.position=lab.transform.position;
        var world=root.AddComponent<ScrollWorldAuthoring>();world.gameCamera=lab.viewCamera;
        world.bendStart=lab.bendStart;world.curvature=lab.curvature;world.transitionLength=lab.transitionLength;world.viewDistance=lab.viewDistance;
        var segments=flow.worldSequence.segments;world.totalDistance=segments[segments.Length-1].EndDistance;
        var layout=Child("Layout - edit objects here",root.transform);
        var spriteMat=MaterialAsset("Scroll_Scenery","CurvedScroll/Authoring Sprite");
        var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);var quadMesh=quad.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(quad);
        for(int s=0;s<segments.Length;s++)
        {
            var segment=segments[s];var p=segment.profile;if(!p)continue;
            var group=Child(s.ToString("00")+"_"+segment.segmentId,layout.transform);group.transform.localPosition=Vector3.forward*segment.startDistance;
            var ground=Child("Ground - editable material",group.transform);
            float from=s==0?-14f:0f,to=segment.length+(s==segments.Length-1?80f:0f);
            var mesh=GroundMesh(from,to);AssetDatabase.CreateAsset(mesh,AssetDatabase.GenerateUniqueAssetPath(Folder+"/"+segment.segmentId+"_Ground.asset"));
            ground.AddComponent<MeshFilter>().sharedMesh=mesh;
            var gm=MaterialAsset(segment.segmentId+"_Ground","CurvedScroll/Authoring Ground");
            var source=p.showRoad?p.terrainMaterial:(p.groundMaterial?p.groundMaterial:p.terrainMaterial);
            if(source){if(source.HasProperty("_Road"))gm.SetTexture("_Road",source.GetTexture("_Road"));if(source.HasProperty("_Side"))gm.SetTexture("_Side",source.GetTexture("_Side"));}
            gm.SetFloat("_HalfWidth",p.roadWidth*.5f);gm.SetFloat("_RoadVisible",p.showRoad?1:0);gm.SetFloat("_TileSize",p.tileSize);
            var gr=ground.AddComponent<MeshRenderer>();gr.sharedMaterial=gm;gr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;gr.receiveShadows=false;
            ground.AddComponent<ScrollWorldItem>().kind=ScrollWorldItem.ItemKind.Ground;
            var fixedRoot=Child("Landmarks",group.transform);
            if(p.fixedPlacements!=null)for(int i=0;i<p.fixedPlacements.Length;i++){
                var f=p.fixedPlacements[i];if(f==null)continue;
                Prop(fixedRoot.transform,world,spriteMat,i.ToString("00")+"_"+(f.sprite?f.sprite.name:"Missing"),f.sprite,f.x,f.z,f.scale,f.flipX,f.sortingOrder,p.roadWidth*.5f);
            }
            var sides=Child("Roadside - baked editable objects",group.transform);
            for(int layer=0;layer<2;layer++){
                var sprites=layer==0?p.nearSprites:p.middleSprites;if(sprites==null||sprites.Length==0)continue;
                int count=layer==0?p.largePropCount:p.smallPropCount;
                for(int i=0;i<count;i++){
                    var sprite=sprites[(i/2+i%2)%sprites.Length];float scale=(layer==0?p.largePropScale:p.smallPropScale)*(.8f+(i%5)*.1f);
                    float z=(i+.4f)*segment.length/Mathf.Max(1,count);float x=(i%2==0?-1:1)*(p.roadWidth*.5f+3f+(i*7%11)*.16f);
                    Prop(sides.transform,world,spriteMat,(layer==0?"Large_":"Small_")+i.ToString("00")+"_"+sprite.name,sprite,x,z,scale,false,0,p.roadWidth*.5f);
                }
            }
            var bg=Child("Background - far image",group.transform);bg.transform.localPosition=new Vector3(-24,9,segment.length*.5f);bg.transform.localScale=new Vector3(12,18,1);
            bg.AddComponent<MeshFilter>().sharedMesh=quadMesh;
            var bm=MaterialAsset(segment.segmentId+"_Background","CurvedScroll/Authoring Background");bm.SetTexture("_MainTex",p.background);bm.SetFloat("_Lift",p.backgroundLift);bm.renderQueue=2501+s;
            bg.AddComponent<MeshRenderer>().sharedMaterial=bm;
            var bi=bg.AddComponent<ScrollWorldItem>();bi.kind=ScrollWorldItem.ItemKind.Background;bi.segmentStart=segment.startDistance;bi.segmentEnd=segment.EndDistance;bi.lastBackground=s==segments.Length-1;
            bi.fadeStart=s==0?0:segments[s-1].EndDistance-segments[s-1].length*Mathf.Max(.05f,segments[s-1].blendOut);
        }
        var markers=Child("Node distance markers",root.transform);
        var start=Child("A_Start (20)",markers.transform);start.transform.localPosition=Vector3.forward*20;
        Child("B_Left or C_Right (75)",markers.transform).transform.localPosition=Vector3.forward*75;
        Child("D_End (145)",markers.transform).transform.localPosition=Vector3.forward*145;
        flow.authoredWorld=world;
        lab.enabled=false;if(art)art.enabled=false;
        var depth=lab.GetComponent<ScrollDepthLayers>();if(depth)depth.enabled=false;
        foreach(var old in Object.FindObjectsOfType<ScrollWorldPreviewTrial>()){old.enabled=false;old.editorPreview=false;old.startOpen=false;old.hideOnScreenControls=true;EditorUtility.SetDirty(old);}
        EditorUtility.SetDirty(flow);EditorUtility.SetDirty(lab);if(art)EditorUtility.SetDirty(art);if(depth)EditorUtility.SetDirty(depth);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject=root;ScrollWorldAuthoringEditor.Frame(world);
    }
}
#endif
