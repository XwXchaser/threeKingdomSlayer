using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>Art-only adapter; never changes combat coordinates.</summary>
public sealed class ValleyArtPresentation : MonoBehaviour
{
    public CurvedScrollLab lab;
    [Header("Default presentation (used when no profile or sequence is assigned)")]
    public Material terrainMaterial;
    public Texture2D background;
    public Sprite[] nearSprites;
    public Sprite[] middleSprites;
    public Sprite[] shoulderSprites;
    [Header("Route profile (visual only)")]
    public ScrollRouteVisualProfile initialProfile;
    [Tooltip("连续环境序列。启动时按当前 lab.Distance 解析区段，使初始画面就等于区段配置，首次前进不再跳变。")]
    public ScrollWorldSequence worldSequence;
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
    int activeSmallCount;
    int largePoolCount;
    public bool showShoulders=true;
    public bool showRoad=true;
    public Material groundMaterial;
    public Sprite[] foregroundSprites;
    [Min(0)] public int foregroundCount = 12;
    [Range(.1f, 3f)] public float foregroundScale = 1f;
    Transform root;
    SpriteRenderer[] props;
    SpriteRenderer[] foregroundProps;
    SpriteRenderer[] fixedProps;
    Mesh[] strips;
    Vector3[][] stripVertices;
    GameObject[] stripObjects;
    MaterialPropertyBlock block;
    Renderer ground;
    GameObject canvas;
    RawImage backgroundImage;
    RawImage transitionImage;
    ScrollRouteVisualProfile appliedProfile;
    ScrollWorldEnvironmentSegment appliedSegment;
    ScrollWorldSequence activeSequence;
    Sprite[] appliedShoulderSprites;
    bool built, contentResolved;
    float roadVisibility = 1f;
    const int MaxPropPool = 200;
    static readonly int Half=Shader.PropertyToID("_HalfWidth"),Travel=Shader.PropertyToID("_Travel"),Tile=Shader.PropertyToID("_TileSize");
    System.Collections.IEnumerator Start()
    {
        if(!lab)yield break;
        lab.Initialize(); yield return null;
        BuildRuntimeObjects();
        ResolveContent();
    }
    /// <summary>Creates the runtime objects once. Content pools are sized later from the sequence/profile.</summary>
    void BuildRuntimeObjects()
    {
        if (built && root != null && ground != null && canvas != null) return;
        // Scene View preview roots are transient; rebuild if Unity invalidated their object references.
        if (built && root == null) built = false;
        if (built) return;
        built = true;
        if (activeSequence == null) activeSequence = worldSequence;
        if (background == null) background = Texture2D.blackTexture;
        if (nearSprites == null) nearSprites = new Sprite[0];
        if (middleSprites == null) middleSprites = new Sprite[0];
        if (shoulderSprites == null) shoulderSprites = new Sprite[0];
        if (foregroundSprites == null) foregroundSprites = new Sprite[0];
        root=new GameObject("Valley art runtime").transform;
        root.SetParent(lab.transform,false);
        #if UNITY_EDITOR
        if (!Application.isPlaying) root.gameObject.hideFlags=HideFlags.DontSave;
        #endif
        ground=lab.GeneratedRoot.Find("Subdivided road").GetComponent<Renderer>();
        block=new MaterialPropertyBlock();
        var depth=lab.GetComponent<ScrollDepthLayers>();if(depth){depth.enabled=false;depth.nearLayer=depth.middleLayer=depth.distantLayer=depth.groundDetails=false;depth.Refresh();}
        foreach(var r in lab.GeneratedRoot.GetComponentsInChildren<SpriteRenderer>())r.enabled=false;
        var backdrop=lab.GeneratedRoot.Find("Static distant silhouettes (placeholder)");if(backdrop)backdrop.GetComponent<Renderer>().enabled=false;
        canvas=new GameObject("Valley background canvas",typeof(Canvas));canvas.hideFlags=HideFlags.DontSave;canvas.transform.SetParent(root,false);
        var c=canvas.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=lab.viewCamera;c.planeDistance=350;c.sortingOrder=-1000;
        var image=new GameObject("2x3 background",typeof(RectTransform),typeof(RawImage),typeof(AspectRatioFitter));image.hideFlags=HideFlags.DontSave;image.transform.SetParent(canvas.transform,false);
        var raw=image.GetComponent<RawImage>();raw.texture=background;raw.raycastTarget=false;
        raw.uvRect=new Rect(0,-backgroundLift,1,1); // Lift distant valley above the curved terrain horizon without stretching.
        var fit=image.GetComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;fit.aspectRatio=2f/3f;
        backgroundImage=raw;
        var transitionObject=new GameObject("Background transition",typeof(RectTransform),typeof(RawImage),typeof(AspectRatioFitter));transitionObject.hideFlags=HideFlags.DontSave;transitionObject.transform.SetParent(canvas.transform,false);
        transitionImage=transitionObject.GetComponent<RawImage>();transitionImage.raycastTarget=false;transitionImage.color=new Color(1,1,1,0);transitionImage.uvRect=raw.uvRect;
        var transitionFit=transitionObject.GetComponent<AspectRatioFitter>();transitionFit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;transitionFit.aspectRatio=2f/3f;
        strips=new Mesh[20];stripVertices=new Vector3[20][];stripObjects=new GameObject[20];
        for(int i=0;i<strips.Length;i++){
            var mesh=new Mesh{name="Curved shoulder strip"};mesh.MarkDynamic();strips[i]=mesh;var v=new Vector3[34];stripVertices[i]=v;var uv=new Vector2[34];var tri=new int[96];
            for(int j=0;j<16;j++){int a=j*2,t=j*6;tri[t]=a;tri[t+1]=a+1;tri[t+2]=a+3;tri[t+3]=a;tri[t+4]=a+3;tri[t+5]=a+2;}
            mesh.vertices=v;mesh.uv=uv;mesh.triangles=tri;var colors=new Color[34];for(int j=0;j<34;j++)colors[j]=Color.white;mesh.colors=colors;
            var go=new GameObject("Road shoulder");go.hideFlags=HideFlags.DontSave;go.transform.SetParent(root,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=lab.spriteMaterial;stripObjects[i]=go;
        }
    }
    /// <summary>Resolves the segment at the current distance once, sizes pools from the whole sequence and pushes that profile.</summary>
    void ResolveContent()
    {
        if (!built || contentResolved) return;
        contentResolved = true;
        var segment = SegmentAt((float)lab.Distance);
        var profile = segment != null ? segment.profile : initialProfile;
        appliedSegment = segment;
        if (profile != null) { CopyProfileValues(profile, true); appliedProfile = profile; }
        roadVisibility = showRoad ? 1f : 0f;
        RebuildPools();
        ApplyProfileContent();
        lab.RefreshPresentation();
    }
    ScrollWorldEnvironmentSegment SegmentAt(float distance)
    {
        if (activeSequence == null) return null;
        if (!activeSequence.TryGetSegment(distance, out var current, out _, out _)) return null;
        return current != null && current.profile != null ? current : null;
    }
    /// <summary>Copies profile values onto the working fields. Geometry is skipped while travelling so width and curvature stay continuous.</summary>
    void CopyProfileValues(ScrollRouteVisualProfile profile, bool includeGeometry)
    {
        if (includeGeometry) { lab.roadWidth = profile.roadWidth; lab.curvature = profile.curvature; }
        terrainMaterial = profile.terrainMaterial;
        if (profile.background != null) background = profile.background;
        nearSprites = profile.nearSprites != null ? profile.nearSprites : new Sprite[0];
        middleSprites = profile.middleSprites != null ? profile.middleSprites : new Sprite[0];
        shoulderSprites = profile.shoulderSprites != null ? profile.shoulderSprites : new Sprite[0];
        backgroundLift = profile.backgroundLift;
        tileSize = profile.tileSize;
        shoulderLength = profile.shoulderLength;
        shoulderWidth = profile.shoulderWidth;
        nearMargin = profile.nearMargin;
        middleMargin = profile.middleMargin;
        largePropCount = profile.largePropCount;
        smallPropCount = profile.smallPropCount;
        largePropScale = profile.largePropScale;
        smallPropScale = profile.smallPropScale;
        roadClearance = profile.roadClearance;
        foregroundSprites = profile.foregroundSprites != null ? profile.foregroundSprites : new Sprite[0];
        foregroundCount = foregroundSprites.Length;
        showShoulders = profile.showShoulders;
        showRoad = profile.showRoad;
        groundMaterial = profile.groundMaterial;
    }
    /// <summary>Sizes every content pool to the union of all segments so nothing is created or destroyed while travelling.</summary>
    void RebuildPools()
    {
        int large = 0, small = 0, fixedCount = 0, foreground = 0;
        bool sequenced = activeSequence != null && activeSequence.segments != null && activeSequence.segments.Length > 0;
        if (sequenced)
        {
            foreach (var s in activeSequence.segments)
            {
                if (s == null || s.profile == null) continue;
                large = Mathf.Max(large, Mathf.Clamp(s.profile.largePropCount, 0, MaxPropPool));
                small = Mathf.Max(small, Mathf.Clamp(s.profile.smallPropCount, 0, MaxPropPool));
                fixedCount = Mathf.Max(fixedCount, s.profile.fixedPlacements != null ? s.profile.fixedPlacements.Length : 0);
                foreground = Mathf.Max(foreground, s.profile.foregroundSprites != null ? s.profile.foregroundSprites.Length : 0);
            }
        }
        else
        {
            // No sequence assigned: keep the density authored on this component.
            large = Mathf.Clamp(largePropCount, 0, MaxPropPool);
            small = Mathf.Clamp(smallPropCount, 0, MaxPropPool);
            fixedCount = appliedProfile != null && appliedProfile.fixedPlacements != null ? appliedProfile.fixedPlacements.Length : 0;
            foreground = Mathf.Clamp(foregroundCount, 0, 40);
        }
        EnsurePropPool(large, small);
        EnsureFixedPool(fixedCount);
        EnsureForegroundPool(foreground);
    }
    void EnsurePropPool(int largeCount, int smallCount)
    {
        int total = largeCount + smallCount;
        if (props != null && largePoolCount == largeCount && props.Length == total) return;
        if (props != null) foreach (var r in props) if (r) Destroy(r.gameObject);
        props = new SpriteRenderer[total];
        largePoolCount = largeCount;
        for (int i = 0; i < total; i++)
        {
            var go = new GameObject(i < largeCount ? "Valley peak or tree" : "Valley low scenery");
            go.hideFlags = HideFlags.DontSave;
            go.transform.SetParent(root, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sharedMaterial = lab.spriteMaterial;
            props[i] = r;
        }
    }
    void EnsureFixedPool(int count)
    {
        if (fixedProps != null && fixedProps.Length == count) return;
        if (fixedProps != null) foreach (var r in fixedProps) if (r) Destroy(r.gameObject);
        fixedProps = null;
        if (count <= 0) return;
        fixedProps = new SpriteRenderer[count];
        for (int i=0;i<count;i++)
        {
            var go=new GameObject("Fixed scroll prop"); go.hideFlags=HideFlags.DontSave; go.transform.SetParent(root,false);
            fixedProps[i]=go.AddComponent<SpriteRenderer>(); fixedProps[i].sharedMaterial=lab.spriteMaterial;
        }
    }
    void EnsureForegroundPool(int count)
    {
        if (foregroundProps != null && foregroundProps.Length == count) return;
        if (foregroundProps != null) foreach (var r in foregroundProps) if (r) Destroy(r.gameObject);
        foregroundProps = null;
        if (count <= 0) return;
        foregroundProps = new SpriteRenderer[count];
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Stronghold foreground prop");
            go.hideFlags = HideFlags.DontSave;
            go.transform.SetParent(root, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sharedMaterial = lab.spriteMaterial;
            foregroundProps[i] = r;
        }
    }
    /// <summary>Pushes everything that only changes when the current segment profile changes: materials, background base and pool contents.</summary>
    void ApplyProfileContent()
    {
        if (!built) return;
        PushStaticPresentation();
        activeLargeCount = Mathf.Clamp(largePropCount, 0, largePoolCount);
        activeSmallCount = props == null ? 0 : Mathf.Clamp(smallPropCount, 0, Mathf.Max(0, props.Length - largePoolCount));
        if (props != null)
        {
            for (int i = 0; i < props.Length; i++)
            {
                bool large = i < largePoolCount;
                int index = large ? i : i - largePoolCount;
                int active = large ? activeLargeCount : activeSmallCount;
                var pool = large ? nearSprites : middleSprites;
                var r = props[i];
                if (r == null) continue;
                if (index >= active || pool == null || pool.Length == 0) { r.enabled = false; continue; }
                r.sprite = pool[(index/2+index%2)%pool.Length];
                r.enabled = true;
            }
        }
        int foregroundActive = foregroundProps == null ? 0 : Mathf.Clamp(foregroundCount, 0, foregroundProps.Length);
        if (foregroundProps != null)
            for (int i = 0; i < foregroundProps.Length; i++)
            {
                var r = foregroundProps[i];
                if (r == null) continue;
                if (i >= foregroundActive || foregroundSprites == null || foregroundSprites.Length == 0) { r.enabled = false; continue; }
                r.sprite = foregroundSprites[i % foregroundSprites.Length];
                r.enabled = true;
            }
        var placements = appliedProfile != null ? appliedProfile.fixedPlacements : null;
        if (fixedProps != null)
            for (int i = 0; i < fixedProps.Length; i++)
            {
                var r = fixedProps[i];
                if (r == null) continue;
                var placement = placements != null && i < placements.Length ? placements[i] : null;
                if (placement == null || !placement.sprite) { r.enabled = false; continue; }
                r.sprite = placement.sprite;
                r.enabled = true;
            }
        ApplyShoulderSprites();
    }
    void PushStaticPresentation()
    {
        if (ground != null)
        {
            var mat = showRoad ? (terrainMaterial != null ? terrainMaterial : groundMaterial) : (groundMaterial != null ? groundMaterial : terrainMaterial);
            if (mat != null) ground.sharedMaterial = mat;
            // The subdivided mesh is the only floor; showRoad only controls the road texture band inside the shader.
            ground.enabled = mat != null;
        }
        if (background == null) background = Texture2D.blackTexture;
        if (backgroundImage != null)
        {
            backgroundImage.texture = background;
            backgroundImage.uvRect = new Rect(0, -backgroundLift, 1, 1);
        }
    }
    void ApplyShoulderSprites()
    {
        if (stripObjects == null) return;
        bool visible = showShoulders && shoulderSprites != null && shoulderSprites.Length > 0;
        bool rebuild = visible && !ReferenceEquals(appliedShoulderSprites, shoulderSprites);
        for (int i = 0; i < stripObjects.Length; i++)
        {
            if (stripObjects[i] == null) continue;
            stripObjects[i].SetActive(visible);
            if (!rebuild) continue;
            var s = shoulderSprites[i % shoulderSprites.Length];
            if (s == null || s.texture == null) continue;
            var rect = s.rect;
            var tex = s.texture;
            var uv = new Vector2[34];
            for (int j = 0; j <= 16; j++)
            {
                float u = Mathf.Lerp(rect.xMin, rect.xMax, j / 16f) / tex.width;
                uv[j * 2] = new Vector2(u, rect.yMin / tex.height);
                uv[j * 2 + 1] = new Vector2(u, rect.yMax / tex.height);
            }
            strips[i].uv = uv;
            var mpb = new MaterialPropertyBlock();
            mpb.SetTexture("_MainTex", tex);
            stripObjects[i].GetComponent<MeshRenderer>().SetPropertyBlock(mpb);
        }
        appliedShoulderSprites = shoulderSprites;
    }
    /// <summary>Applies a single profile directly (lab scenes without a world sequence).</summary>
    public void ApplyProfile(ScrollRouteVisualProfile profile)
    {
        if (!profile || !lab) return;
        if (!built) { initialProfile = profile; return; }
        activeSequence = null;
        contentResolved = true;
        CopyProfileValues(profile, true);
        appliedProfile = profile;
        appliedSegment = null;
        roadVisibility = showRoad ? 1f : 0f;
        RebuildPools();
        ApplyProfileContent();
        lab.RefreshPresentation();
    }
    /// <summary>Route flow hands its sequence over so the initial presentation already equals the current segment.</summary>
    public void SetSequence(ScrollWorldSequence sequence)
    {
        if (sequence == null || sequence == activeSequence) return;
        activeSequence = sequence;
        contentResolved = false;
        if (built) ResolveContent();
    }
    /// <summary>Builds the same visual objects used by the editor-only scene preview.</summary>
    public void EnsurePreviewObjects(ScrollWorldSequence sequence)
    {
        if (sequence != null) activeSequence = sequence;
        if (!built) BuildRuntimeObjects();
        ResolveContent();
    }
    public void ApplyWorldDistance(float distance, ScrollWorldSequence sequence)
    {
        if (!built) return;
        if (activeSequence == null && sequence != null) activeSequence = sequence;
        ResolveContent();
        if (activeSequence == null || !activeSequence.TryGetSegment(distance, out var current, out var next, out var normalized) || current == null || current.profile == null) return;
        if (!ReferenceEquals(appliedSegment, current))
        {
            // Segment content changes only here; geometry keeps blending so nothing snaps.
            appliedSegment = current;
            appliedProfile = current.profile;
            CopyProfileValues(current.profile, false);
            ApplyProfileContent();
        }
        bool hasNext = next != null && next.profile != null;
        float previousRoadWidth = lab.roadWidth, previousCurvature = lab.curvature;
        if (hasNext)
        {
            float start = Mathf.Clamp01(1f - Mathf.Max(0.05f, current.blendOut));
            float blend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, 1f, normalized));
            lab.roadWidth = Mathf.Lerp(current.profile.roadWidth, next.profile.roadWidth, blend);
            lab.curvature = Mathf.Lerp(current.profile.curvature, next.profile.curvature, blend);
            roadVisibility = Mathf.Lerp(current.profile.showRoad ? 1f : 0f, next.profile.showRoad ? 1f : 0f, blend);
            if (transitionImage)
            {
                transitionImage.texture = next.profile.background != null ? next.profile.background : background;
                transitionImage.uvRect = new Rect(0, -next.profile.backgroundLift, 1, 1);
                transitionImage.color = new Color(1f, 1f, 1f, blend);
            }
        }
        else
        {
            roadVisibility = current.profile.showRoad ? 1f : 0f;
            if (transitionImage) transitionImage.color = new Color(1f, 1f, 1f, 0f);
        }
        // Keep the road mesh in step with the width written this frame.
        if (!Mathf.Approximately(previousRoadWidth, lab.roadWidth) || !Mathf.Approximately(previousCurvature, lab.curvature)) lab.RefreshPresentation();
    }
    void LateUpdate()
    {
        if(!built||!ground)return;
        ResolveContent(); // Safety net when the sequence is handed over after Start.
        float span=Mathf.Max(20,lab.viewDistance)+14,travel=(float)(lab.Distance%span),half=lab.roadWidth*.5f;
        block.SetFloat(Half,half*Mathf.Clamp01(roadVisibility));block.SetFloat(Travel,(float)(lab.Distance%(Mathf.Max(.1f,tileSize)*1024)));block.SetFloat(Tile,tileSize);ground.SetPropertyBlock(block);
        if (props != null) for(int i=0;i<props.Length;i++){
            bool near=i<largePoolCount;int index=near?i:i-largePoolCount,count=near?activeLargeCount:activeSmallCount;
            var r=props[i];
            if (count <= 0 || index >= count || !r || !r.sprite) { if (r && r.enabled) r.enabled=false; continue; }
            if (!r.enabled) r.enabled=true;
            float jitter=((index*17%13)/13f-.5f)*.55f;
            float z=-14+Mathf.Repeat((index+.37f+jitter)*span/count-travel,span);float side=index%2==0?-1:1;
            float scale=Mathf.Max(.05f,near?largePropScale:smallPropScale);scale*=.8f+(i%5)*.1f;
            // Keep the whole sprite outside the road even when increasing its scale.
            float inwardExtent=side<0?r.sprite.bounds.max.x:-r.sprite.bounds.min.x;
            float margin=Mathf.Max(near?nearMargin:middleMargin,inwardExtent*scale+Mathf.Max(0,roadClearance));
            float x=side*(half+margin+(i*7%11)*.16f);
            r.transform.localScale=Vector3.one*scale;r.transform.localPosition=new Vector3(x,-lab.Drop(z)-r.sprite.bounds.min.y*scale,z);
            r.color=new Color(1,1,1,Mathf.Clamp01((lab.viewDistance-z)/10)*Mathf.Clamp01((z+14)/2));
        }
        for(int i=0;i<strips.Length;i++){
            if(!strips[i]||stripObjects[i]==null||!stripObjects[i].activeSelf)continue;
            float z=-14+Mathf.Repeat((i/2f)*span/10-travel,span);float side=i%2==0?-1:1;var v=stripVertices[i];
            for(int j=0;j<=16;j++){float depth=z+(j/16f-.5f)*shoulderLength;v[j*2]=new Vector3(side*(half-shoulderWidth*.5f),-lab.Drop(depth)+.025f,depth);v[j*2+1]=new Vector3(side*(half+shoulderWidth*.5f),-lab.Drop(depth)+.025f,depth);}
            strips[i].vertices=v;strips[i].RecalculateBounds();
        }
        if (foregroundProps != null)
        {
            float foregroundSpan = Mathf.Max(20, lab.viewDistance) + 14;
            float foregroundTravel = (float)(lab.Distance % foregroundSpan);
            int foregroundActive = Mathf.Clamp(foregroundCount, 0, foregroundProps.Length);
            for (int i = 0; i < foregroundProps.Length; i++)
            {
                var r = foregroundProps[i];
                if (i >= foregroundActive || !r || !r.sprite) { if (r && r.enabled) r.enabled = false; continue; }
                if (!r.enabled) r.enabled = true;
                float z = -10f + Mathf.Repeat((i + .5f) * foregroundSpan / foregroundProps.Length - foregroundTravel, foregroundSpan);
                float side = i % 3 == 0 ? 0f : (i % 2 == 0 ? -1f : 1f);
                float x = side == 0f ? 0.25f : side * Mathf.Lerp(1.2f, 3.2f, (i % 5) / 4f);
                float scale = foregroundScale * (.85f + (i % 4) * .1f);
                r.transform.localScale = Vector3.one * scale;
                r.transform.localPosition = new Vector3(x, -lab.Drop(z) - r.sprite.bounds.min.y * scale + .03f, z);
                r.sortingOrder = 2;
            }
        }
        var placements = appliedProfile != null ? appliedProfile.fixedPlacements : null;
        if (fixedProps != null && placements != null)
        {
            for (int i=0;i<fixedProps.Length;i++)
            {
                var r=fixedProps[i];
                if (r == null) continue;
                var placement = i < placements.Length ? placements[i] : null;
                if (placement == null || !placement.sprite) { if (r.enabled) r.enabled=false; continue; }
                // Placement z is authored relative to its segment when the profile flag is enabled.
                float segmentOrigin = appliedProfile.placementsUseLocalDistance && appliedSegment != null ? appliedSegment.startDistance : 0f;
                float worldAuthoredZ = placement.z + segmentOrigin;
                float distanceWithinSegment = (float)lab.Distance - segmentOrigin;
                float z = placement.loop
                    ? Mathf.Repeat(placement.z - distanceWithinSegment + 14f, Mathf.Max(20f, lab.viewDistance + 28f)) - 14f
                    : worldAuthoredZ;
                if (r.sprite != placement.sprite) r.sprite=placement.sprite;
                if (r.sortingOrder != placement.sortingOrder) r.sortingOrder=placement.sortingOrder;
                if (!r.enabled) r.enabled=true;
                r.color = new Color(1f,1f,1f,Mathf.Clamp01((lab.viewDistance-z)/10f)*Mathf.Clamp01((z+14f)/2f));
                r.transform.localScale=new Vector3(placement.flipX?-placement.scale:placement.scale,placement.scale,1f);
                r.transform.localPosition=new Vector3(placement.x,-lab.Drop(z)-r.sprite.bounds.min.y*placement.scale,z);
            }
        }
    }
    void OnDestroy()
    {
        DOTween.Kill(transform, false);
        if (foregroundProps != null) foreach (var r in foregroundProps) if (r) DOTween.Kill(r, false);
        if (fixedProps != null) foreach (var r in fixedProps) if (r) DOTween.Kill(r, false);
        if (root) DOTween.Kill(root, false);
        if (props != null) foreach (var r in props) if (r) DOTween.Kill(r, false);
        if (strips != null) foreach (var m in strips) if (m) Destroy(m);
        if (root) Destroy(root.gameObject);
        foregroundProps = null;
        fixedProps = null;
        props = null;
        appliedSegment = null;
        appliedShoulderSprites = null;
        activeSequence = null;
        built = false;
        contentResolved = false;
    }
}
