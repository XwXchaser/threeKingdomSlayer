using UnityEngine;

/// <summary>
/// 敌人脚下的椭圆阴影。
///
/// 设计要点：
/// - 阴影是运行时在场景根上创建的独立对象，不挂在敌人下，因此不继承敌人的弹跳、缩放与父级缩放。
/// - 贴图与材质由全类共享（静态缓存），所有敌人共用一份，不为每个敌人新建材质。
    /// - 每帧只做一次位置与缩放赋值：X/Z 跟随敌人，世界 Y 贴当前可见精灵的不透明像素脚底。
    /// - 击飞、弹跳时阴影留在地面线，离地高度决定透明度与缩放。
/// - 敌人禁用（对象池回收）时阴影同步隐藏；敌人销毁时阴影一并销毁。
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyGroundShadow : MonoBehaviour
{
    [Tooltip("阴影宽度占敌人剪影世界宽度的比例。1.15 让椭圆超出双脚，中段被脚挡住，两侧露出。")]
    [SerializeField, Range(0.05f, 2f)] private float widthToSilhouette = 1.15f;

    [Tooltip("阴影纵深（世界 Z）相对宽度的比例。平躺后，纵深即透视里的前后距离。")]
    [SerializeField, Range(0.05f, 1f)] private float depthToWidth = 0.35f;

    [Tooltip("静止时的不透明度。")]
    [SerializeField, Range(0f, 1f)] private float baseAlpha = 0.45f;

    [Tooltip("离地多高时阴影达到最小（世界单位）。")]
    [SerializeField, Min(0.01f)] private float fadeHeight = 2.5f;

    [Tooltip("离地后阴影的最小缩放比例。")]
    [SerializeField, Range(0f, 1f)] private float minScale = 0.45f;

    [Tooltip("阴影相对敌人主体 SpriteRenderer 的排序偏移，负数表示更靠后。")]
    [SerializeField] private int sortingOffset = -1;

    [Tooltip("同一动画片段内，脚底锁定为各帧不透明像素最低点的中位数。网格顶点会落在披风或脚外侧，逐帧跟随会在脚下留空并闪烁。")]
    [SerializeField] private bool lockFootPerClip = true;

    [Tooltip("不透明度阈值。低于此值的像素不算脚底。")]
    [SerializeField, Range(1, 64)] private int alphaCutoff = 16;

    [Tooltip("阴影中心相对不透明像素脚底的上移（世界单位）。0 表示贴着不透明像素脚底。")]
    [SerializeField] private float footLift = 0f;

    // 全类共享：一张贴图、一个材质
    private static Sprite _sharedSprite;
    private static Material _sharedMaterial;

    private Transform _owner;
    private Transform _shadowTransform;
    private SpriteRenderer _renderer;
    private SpriteRenderer _ownerRenderer;
    private CavalryVisualController _cavalryVisual;
    private float _groundY = -1.8f;
    private EnemyGroundAligner _aligner;
    private float _baseWidth = 1f;
    private float _lockedFootWorldY;
    private bool _footLocked;
    private int _lockedClipHash;

    public struct FootSample
    {
        public float localY;
        public float localWidth;
    }

    public static readonly System.Collections.Generic.Dictionary<Sprite, FootSample> FootCache
        = new System.Collections.Generic.Dictionary<Sprite, FootSample>();

    private void Awake()
    {
        _owner = transform;
        _ownerRenderer = GetComponent<SpriteRenderer>();
        _cavalryVisual = GetComponent<CavalryVisualController>();
        _baseWidth = ComputeBaseWidth();
        CreateShadow();
    }

    /// <summary>
    /// 按当前可见精灵的不透明像素宽度推算阴影宽度。
    /// 109 骑乘时取根节点人马合一图；落马后根渲染器重新可见，会在 LateUpdate 里重算。
    /// </summary>
    private float ComputeBaseWidth()
    {
        var sr = ResolveVisualRenderer();
        if (sr == null || sr.sprite == null) return 1f;
        var sample = GetFootSample(sr.sprite);
        float silhouetteWorldWidth = sample.localWidth * Mathf.Abs(sr.transform.lossyScale.x);
        return Mathf.Max(0.2f, silhouetteWorldWidth * widthToSilhouette);
    }

    private void OnEnable()
    {
        if (_renderer != null) _renderer.enabled = true;
    }

    private void OnDisable()
    {
        if (_renderer != null) _renderer.enabled = false;
    }

    private void LateUpdate()
    {
        if (_shadowTransform == null) return;

        // 地面线在 Awake 时不一定已被 EnemyGroundAligner 赋值，每帧读取（字段读取为常数开销）
        var aligner = _aligner;
        if (aligner == null) { _aligner = aligner = GetComponent<EnemyGroundAligner>(); }
        if (aligner != null) _groundY = aligner.groundY;

        Vector3 p = _owner.position;
        var visual = ResolveVisualRenderer();
        float targetFeetY = GetTargetFeetWorldY(visual, p.y);
        // 击飞、弹跳时脚底离开地面，阴影留在地面线上，只用高度做淡出。
        float feetY = Mathf.Max(_groundY, targetFeetY);
        if (targetFeetY < _groundY - 0.02f) feetY = targetFeetY;
        float height = Mathf.Max(0f, targetFeetY - _groundY);

        float t = Mathf.Clamp01(height / fadeHeight);
        float scale = Mathf.Lerp(1f, minScale, t);
        if (_baseWidth < 0.21f) _baseWidth = ComputeBaseWidth();

        // 平躺在 XZ 地面。Y 取不透明像素脚底再上移，避免沉进地面后从透视里消失。
        _shadowTransform.position = new Vector3(p.x, feetY + footLift, p.z);
        _shadowTransform.localScale = new Vector3(_baseWidth * scale, _baseWidth * depthToWidth * scale, 1f);

        var c = _renderer.color;
        c.a = baseAlpha * Mathf.Lerp(1f, 0.35f, t);
        _renderer.color = c;
    }

    /// <summary>
    /// 当前可见的身体渲染器。109 正式骑乘用根节点人马合一图；
    /// 根渲染器被关掉时（旧占位骑乘）改取仍在显示的 MountVisual。
    /// </summary>
    private SpriteRenderer ResolveVisualRenderer()
    {
        if (_ownerRenderer != null && _ownerRenderer.enabled && _ownerRenderer.sprite != null)
            return _ownerRenderer;
        if (_cavalryVisual == null) return _ownerRenderer;

        var mount = _cavalryVisual.mountRenderer;
        if (mount != null && mount.enabled && mount.gameObject.activeInHierarchy && mount.sprite != null)
            return mount;
        return _ownerRenderer;
    }

    private float GetTargetFeetWorldY(SpriteRenderer visual, float rootY)
    {
        if (visual == null || visual.sprite == null) return _groundY;

        if (lockFootPerClip && TryGetClipLockedFoot(visual, out float lockedLocal))
        {
            var clip = GetCurrentClip(visual);
            int clipHash = clip != null ? clip.GetInstanceID() : visual.sprite.GetInstanceID();
            if (!_footLocked || _lockedClipHash != clipHash)
            {
                _lockedClipHash = clipHash;
                _footLocked = true;
                _baseWidth = ComputeBaseWidth();
            }
            // 锁定的是精灵内部偏移，世界 Y 仍跟随当前渲染器，击飞和排位不会被钉死。
            return WorldFootY(visual, lockedLocal);
        }

        var sample = GetFootSample(visual.sprite);
        return WorldFootY(visual, sample.localY);
    }

    private static float WorldFootY(SpriteRenderer visual, float localY)
    {
        // sprite 本地单位是像素 / PPU，必须乘渲染器的世界缩放才是世界距离。
        // 1011 的 visualYOffset 把网格脚底抬到 -1.8，不透明像素脚底比它高约 0.15，阴影应贴后者。
        return visual.transform.position.y + localY * Mathf.Abs(visual.transform.lossyScale.y);
    }

    private bool TryGetClipLockedFoot(SpriteRenderer visual, out float localY)
    {
        localY = 0f;
        var clip = GetCurrentClip(visual);
        if (clip == null || visual.sprite == null) return false;
        // 不在运行时 SampleAnimation，避免改掉正在播放的精灵。
        // 同一 clip 锁定第一次采到的不透明脚底。1011 idle 六帧像素脚底只差 0.06 本地单位，
        // 换算约 0.005 世界单位，锁首帧与取中位数在画面上没有差别。
        if (!ClipFootCache.TryGetValue(clip, out localY))
        {
            localY = GetFootSample(visual.sprite).localY;
            ClipFootCache[clip] = localY;
        }
        return true;
    }

    public static readonly System.Collections.Generic.Dictionary<AnimationClip, float> ClipFootCache
        = new System.Collections.Generic.Dictionary<AnimationClip, float>();

    private AnimationClip GetCurrentClip(SpriteRenderer visual)
    {
        // 只认驱动这块渲染器的 Animator。子层（109 旧占位马）不能去读根 Animator，
        // 否则会锁到人马合一 clip 的脚底，阴影会悬在马蹄上方。
        var animator = visual != null ? visual.GetComponent<Animator>() : null;
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null || animator.runtimeAnimatorController == null) return null;
        var info = animator.GetCurrentAnimatorClipInfo(0);
        if (info == null || info.Length == 0 || info[0].clip == null) return null;
        return info[0].clip;
    }

    private FootSample GetFootSample(Sprite sprite)
    {
        if (sprite != null && FootCache.TryGetValue(sprite, out var cached)) return cached;
        var sample = SampleOpaqueFoot(sprite, (byte)alphaCutoff);
        if (sprite != null) FootCache[sprite] = sample;
        return sample;
    }

    /// <summary>
    /// 不透明像素的最低行才是视觉脚底。Tight 网格最低点会落在披风、武器或脚外侧，
    /// 1011 idle 实测比真实脚底低约 0.15 世界单位，而且六帧互相不一致。
    /// </summary>
    private static FootSample SampleOpaqueFoot(Sprite sprite, byte cutoff)
    {
        var sample = new FootSample();
        if (sprite == null || sprite.texture == null)
        {
            sample.localWidth = 1f;
            return sample;
        }

        var tex = sprite.texture;
        var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        Graphics.Blit(tex, rt);
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var readable = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false, true);
        readable.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
        readable.Apply(false, false);
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        var tr = sprite.textureRect;
        int x0 = Mathf.RoundToInt(tr.x);
        int y0 = Mathf.RoundToInt(tr.y);
        int w = Mathf.RoundToInt(tr.width);
        int h = Mathf.RoundToInt(tr.height);
        var px = readable.GetPixels32();
        Object.Destroy(readable);

        int lowest = -1;
        int left = int.MaxValue;
        int right = int.MinValue;
        int tw = tex.width;
        for (int row = 0; row < h; row++)
        {
            int ty = y0 + row;
            if (ty < 0 || ty >= tex.height) continue;
            bool any = false;
            int lx = int.MaxValue, rx = int.MinValue;
            int rowBase = ty * tw;
            for (int col = 0; col < w; col++)
            {
                int tx = x0 + col;
                if (tx < 0 || tx >= tw) continue;
                if (px[rowBase + tx].a <= cutoff) continue;
                any = true;
                if (col < lx) lx = col;
                if (col > rx) rx = col;
            }
            if (!any) continue;
            if (lowest < 0) lowest = row;
            if (lx < left) left = lx;
            if (rx > right) right = rx;
        }

        float ppu = Mathf.Max(1f, sprite.pixelsPerUnit);
        if (lowest < 0)
        {
            sample.localY = sprite.bounds.min.y;
            sample.localWidth = sprite.bounds.size.x;
            return sample;
        }

        sample.localY = (y0 + lowest - sprite.pivot.y) / ppu;
        sample.localWidth = (right - left + 1) / ppu;
        return sample;
    }

    private void CreateShadow()
    {
        if (_shadowTransform != null) return;

        var go = new GameObject(name + "_GroundShadow");
        go.hideFlags = HideFlags.DontSave;
        _shadowTransform = go.transform;

        // 绕 X 轴旋转 90°，把竖直面片放平到 XZ 地面。
        _shadowTransform.rotation = Quaternion.Euler(90f, 0f, 0f);

        _renderer = go.AddComponent<SpriteRenderer>();
        _renderer.sprite = GetSharedSprite();
        _renderer.sharedMaterial = GetSharedMaterial();
        _renderer.color = new Color(0f, 0f, 0f, baseAlpha);
        _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _renderer.receiveShadows = false;

        if (_ownerRenderer != null)
        {
            _renderer.sortingLayerID = _ownerRenderer.sortingLayerID;
            _renderer.sortingOrder = _ownerRenderer.sortingOrder + sortingOffset;
        }
    }

    private void OnDestroy()
    {
        if (_shadowTransform != null) Destroy(_shadowTransform.gameObject);
    }

    // ───────── 共享资源 ─────────

    private static Sprite GetSharedSprite()
    {
        if (_sharedSprite != null) return _sharedSprite;

        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "EnemyGroundShadow_Procedural"
        };

        var px = new Color32[size * size];
        float c = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x - c) / c;
                float ny = (y - c) / c;
                float d = Mathf.Sqrt(nx * nx + ny * ny);
                float a = 1f - Mathf.InverseLerp(0.55f, 1f, d);
                a = Mathf.Clamp01(a) * Mathf.Clamp01(a);
                px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
        }
        tex.SetPixels32(px);
        tex.Apply(false, true);

        _sharedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        _sharedSprite.name = "EnemyGroundShadow_Procedural";
        return _sharedSprite;
    }

    private static Material GetSharedMaterial()
    {
        if (_sharedMaterial != null) return _sharedMaterial;
        // 保持和角色同一精灵队列。改到透明队列会让阴影穿透身体，看起来悬在小腿上。
        var shader = Shader.Find("Sprites/Default");
        _sharedMaterial = new Material(shader)
        {
            name = "EnemyGroundShadow_Mat",
            hideFlags = HideFlags.DontSave
        };
        return _sharedMaterial;
    }
}
