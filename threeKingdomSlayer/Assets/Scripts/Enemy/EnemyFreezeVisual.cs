using DG.Tweening;
using UnityEngine;

/// <summary>
/// 冰系（冻结）的**视觉层**：冰壳覆盖层、冰色着色、融化/碎冰特效、程序化冰块与碎片贴图。
///
/// 从 <c>Enemy</c>（原 5 千多行的上帝类）的「冻结（冰系）」区段抽出。职责边界：
///   • 本类只管「看起来冻住了」——不碰敌人的状态机、伤害、挂起令牌；
///   • 冻结/破冰的**状态与流程**仍在 Enemy（本类不决定什么时候冻结、什么时候解冻）；
///   • 需要的环境（transform / 渲染器 / 冻结表现配置）由 Enemy 每次以
///     <see cref="Context"/> 传入，类本身不持有 Enemy 引用。
///
/// 不是 MonoBehaviour：由 Enemy 自己 new 出来，避免改 prefab 序列化、也不在对象池复用路径上多一次 GetComponent。
/// 程序化贴图是静态缓存（全项目共用一份），与搬迁前行为一致。
/// </summary>
public sealed class EnemyFreezeVisual
{
    /// <summary>视觉层需要的外部环境快照。</summary>
    public struct Context
    {
        /// <summary>敌人根节点（覆盖层与特效的父级）。</summary>
        public Transform transform;
        /// <summary>敌人精灵渲染器（可见区、层级与原始颜色都取自它）。</summary>
        public SpriteRenderer spriteRenderer;
        /// <summary>美术给的冰块 Prefab（为空则用程序化冰壳）。</summary>
        public GameObject overlayPrefab;
        /// <summary>冰块缩放比例。</summary>
        public float overlayScale;
        /// <summary>冰块相对可见区的垂直偏移。</summary>
        public float overlayYOffset;
        /// <summary>冰色着色目标色。</summary>
        public Color tintColor;
    }

    /// <summary>冻结时的冰色（搬迁前是 Enemy 的常量，视觉语义，归到视觉层）。</summary>
    public static readonly Color TintColor = new Color(0.55f, 0.78f, 1f, 1f);

    private static Sprite _proceduralFrostSprite;
    private static Sprite[] _proceduralShardSprites;
    private static Sprite _proceduralShardSprite;

    private GameObject _overlayInstance;
    private SpriteRenderer _overlayRenderer;
    private bool _hasColorSnapshot;
    private Color _colorBeforeFreeze;
    private Vector3 _overlayWorldSize;

    /// <summary>冰壳覆盖层渲染器（Enemy 的呼吸动画用）。</summary>
    public SpriteRenderer OverlayRenderer => _overlayRenderer;

    /// <summary>当前冰块的世界尺寸（破冰特效按它缩放）。</summary>
    public Vector3 OverlayWorldSize => _overlayWorldSize;

    /// <summary>是否已挂着冰壳覆盖层。</summary>
    public bool HasOverlay => _overlayInstance != null;

    // ══════════════════════════════════════════════════════════════
    // 覆盖层与着色
    // ══════════════════════════════════════════════════════════════

    /// <summary>开启/关闭冰壳覆盖层与冰色着色。</summary>
    public void Apply(bool on, in Context ctx)
    {
        if (on)
        {
            if (_overlayInstance == null)
            {
                if (ctx.overlayPrefab != null)
                {
                    _overlayInstance = Object.Instantiate(ctx.overlayPrefab, ctx.transform, false);
                    _overlayRenderer = _overlayInstance.GetComponentInChildren<SpriteRenderer>();
                    FitOverlayToBlock(ctx);
                }
                else
                {
                    _overlayRenderer = CreateProceduralFrostOverlay(ctx);
                    _overlayInstance = _overlayRenderer != null ? _overlayRenderer.gameObject : null;
                    ApplyTint(ctx);
                }
            }
            return;
        }

        if (_overlayInstance != null)
        {
            Object.Destroy(_overlayInstance);
            _overlayInstance = null;
            _overlayRenderer = null;
        }
        if (_hasColorSnapshot && ctx.spriteRenderer != null)
            ctx.spriteRenderer.color = _colorBeforeFreeze;
        _hasColorSnapshot = false;
    }

    /// <summary>冰色着色：保留原色快照，lerp 到冰色。</summary>
    public void ApplyTint(in Context ctx)
    {
        if (ctx.spriteRenderer == null) return;
        if (!_hasColorSnapshot)
        {
            _colorBeforeFreeze = ctx.spriteRenderer.color;
            _hasColorSnapshot = true;
        }
        Color c = Color.Lerp(_colorBeforeFreeze, ctx.tintColor, 0.55f);
        c.a = _colorBeforeFreeze.a;
        ctx.spriteRenderer.color = c;
    }

    /// <summary>把美术给的冰块 Prefab 按“角色可见区 × 比例”自动缩放并居中（素材不用自己调尺寸）。</summary>
    public void FitOverlayToBlock(in Context ctx)
    {
        if (_overlayInstance == null || _overlayRenderer == null || _overlayRenderer.sprite == null) return;

        GetSpriteVisibleArea(out Vector3 visibleCenter, out Vector3 visibleSize, ctx);
        Vector3 blockSize = new Vector3(visibleSize.y * ctx.overlayScale, visibleSize.y * ctx.overlayScale, 1f);
        Vector3 blockCenter = visibleCenter + Vector3.up * (visibleSize.y * ctx.overlayYOffset);

        Vector3 sb = _overlayRenderer.sprite.bounds.size;
        if (sb.x <= 0.0001f || sb.y <= 0.0001f) return;

        Vector3 parentScale = ctx.transform.lossyScale;
        // 等比缩放：只按高度算，宽度跟随素材本身比例（不再分别拉伸宽高）
        float k = (visibleSize.y * ctx.overlayScale) / sb.y;
        float sx = k / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x));
        float sy = k / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y));

        _overlayInstance.transform.position = blockCenter;
        _overlayInstance.transform.localScale = new Vector3(sx, sy, 1f);
        _overlayRenderer.sortingOrder = (ctx.spriteRenderer != null ? ctx.spriteRenderer.sortingOrder : 0) + 1;
        _overlayWorldSize = blockSize;
    }

    // ══════════════════════════════════════════════════════════════
    // 特效
    // ══════════════════════════════════════════════════════════════

    /// <summary>生成一次性特效并按冰块尺寸自动缩放（不依赖素材分辨率）。</summary>
    public void SpawnFittedFx(GameObject prefab, float scaleMultiplier, in Context ctx)
    {
        if (prefab == null) return;

        GetFreezeFxPosition(out Vector3 pos, ctx);
        var fx = Object.Instantiate(prefab, pos, Quaternion.identity);
        var sr = fx.GetComponentInChildren<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;

        Vector3 sb = sr.sprite.bounds.size;
        float maxSide = Mathf.Max(sb.x, sb.y);
        if (maxSide <= 0.0001f) return;

        float target = Mathf.Max(_overlayWorldSize.x, _overlayWorldSize.y) * scaleMultiplier;
        fx.transform.localScale = Vector3.one * (target / maxSide);
    }

    /// <summary>冻结/破冰特效的生成位置：角色可见区域中心 + 冰块垂直偏移。</summary>
    public void GetFreezeFxPosition(out Vector3 position, in Context ctx)
    {
        GetSpriteVisibleArea(out Vector3 center, out Vector3 size, ctx);
        position = center + Vector3.up * (size.y * ctx.overlayYOffset);
    }

    /// <summary>
    /// 取得精灵“实际可见区域”的世界中心与尺寸。
    /// 敌人贴图普遍是大幅画布 + 角色居中（如 1011 为 1108x1108，角色仅占 644x693），
    /// 直接用 SpriteRenderer.bounds 会把冰块做得比角色大很多。这里用 textureRect（tight 区域）修正。
    /// </summary>
    public void GetSpriteVisibleArea(out Vector3 worldCenter, out Vector3 worldSize, in Context ctx)
    {
        if (ctx.spriteRenderer == null)
        {
            worldCenter = ctx.transform.position;
            worldSize = Vector3.one;
            return;
        }

        Bounds bounds = ctx.spriteRenderer.bounds;
        worldCenter = bounds.center;
        worldSize = bounds.size;

        var sprite = ctx.spriteRenderer.sprite;
        if (sprite == null) return;

        Rect rect = sprite.rect;
        Rect texRect = sprite.textureRect;
        if (rect.width <= 0.0001f || rect.height <= 0.0001f) return;
        if (texRect.width <= 0.0001f || texRect.height <= 0.0001f) return;

        // Full Rect 时两者相同，比例=1，不影响结果
        float wRatio = Mathf.Clamp01(texRect.width / rect.width);
        float hRatio = Mathf.Clamp01(texRect.height / rect.height);
        worldSize = new Vector3(bounds.size.x * wRatio, bounds.size.y * hRatio, bounds.size.z);

        Vector2 offsetPx = texRect.center - rect.center;
        if (offsetPx.sqrMagnitude < 0.0001f) return;

        Vector3 lossy = ctx.transform.lossyScale;
        float signX = lossy.x < 0f ? -1f : 1f;
        float signY = lossy.y < 0f ? -1f : 1f;
        worldCenter += new Vector3(
            offsetPx.x / rect.width * bounds.size.x * signX,
            offsetPx.y / rect.height * bounds.size.y * signY,
            0f);
    }

    /// <summary>没有冰块素材时的程序化冰壳（圆角方块 + 内圈冷色 + 斜裂纹）。</summary>
    public SpriteRenderer CreateProceduralFrostOverlay(in Context ctx)
    {
        if (ctx.spriteRenderer == null) return null;

        var go = new GameObject("FreezeOverlay");
        go.transform.SetParent(ctx.transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetProceduralFrostSprite();
        sr.color = new Color(0.72f, 0.9f, 1f, 0.8f);
        sr.sortingOrder = ctx.spriteRenderer.sortingOrder + 1;

        GetSpriteVisibleArea(out Vector3 visibleCenter, out Vector3 visibleSize, ctx);

        Vector3 blockSize = new Vector3(visibleSize.y * ctx.overlayScale, visibleSize.y * ctx.overlayScale, 1f);
        Vector3 blockCenter = visibleCenter + Vector3.up * (visibleSize.y * ctx.overlayYOffset);

        Vector3 spriteSize = sr.sprite.bounds.size;
        Vector3 parentScale = ctx.transform.lossyScale;
        float k2 = (visibleSize.y * ctx.overlayScale) / Mathf.Max(0.0001f, spriteSize.y);
        float sx = k2 / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x));
        float sy = k2 / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y));
        go.transform.localScale = new Vector3(sx, sy, 1f);
        go.transform.position = blockCenter;
        _overlayWorldSize = blockSize;
        return sr;
    }

    /// <summary>没有美术素材时的冰碎占位：闪光 + 12 片亮色冰栈向外飞散。</summary>
    public void SpawnProceduralIceBreakFx(in Context ctx)
    {
        GetSpriteVisibleArea(out Vector3 center, out Vector3 visibleSize, ctx);
        center += Vector3.up * (visibleSize.y * ctx.overlayYOffset);
        Vector3 blockSize = _overlayWorldSize.sqrMagnitude > 0.0001f
            ? _overlayWorldSize
            : new Vector3(visibleSize.y * ctx.overlayScale, visibleSize.y * ctx.overlayScale, 1f);

        var root = new GameObject("IceBreakFx");
        root.transform.position = center;

        int sortingBase = (ctx.spriteRenderer != null ? ctx.spriteRenderer.sortingOrder : 0) + 6;

        // 1) 破冰闪光：确保“冰碎了”这一下先被看到
        var flash = new GameObject("flash");
        flash.transform.SetParent(root.transform, false);
        var flashSr = flash.AddComponent<SpriteRenderer>();
        flashSr.sprite = GetProceduralShardSprite();
        flashSr.sortingOrder = sortingBase - 1;
        flashSr.color = new Color(1f, 1f, 1f, 0.9f);
        flash.transform.position = center;
        flash.transform.localScale = Vector3.one * (Mathf.Max(blockSize.x, blockSize.y) * 1.7f);
        flash.transform.DORotate(new Vector3(0f, 0f, 80f), 0.24f).SetTarget(flash).SetUpdate(UpdateType.Normal, false);
        flashSr.DOFade(0f, 0.22f).SetTarget(flash).SetUpdate(UpdateType.Normal, false);

        // 2) 碎片：12 片亮色冰栈向外飞散
        const int count = 12;
        float minSide = Mathf.Max(0.1f, Mathf.Min(blockSize.x, blockSize.y));

        for (int i = 0; i < count; i++)
        {
            var shard = new GameObject("shard");
            shard.transform.SetParent(root.transform, false);
            var sr = shard.AddComponent<SpriteRenderer>();
            sr.sprite = GetProceduralShardSprite();
            sr.sortingOrder = sortingBase;
            sr.color = new Color(0.9f, 0.97f, 1f, 1f);

            float angle = (Mathf.PI * 2f / count) * i + Random.Range(-0.25f, 0.25f);
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float startRadius = minSide * 0.22f;
            shard.transform.position = center + new Vector3(dir.x, dir.y, 0f) * startRadius;
            shard.transform.localScale = Vector3.one * Mathf.Max(0.18f, minSide * Random.Range(0.22f, 0.38f));
            shard.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            Vector3 start = shard.transform.position;
            Vector3 target = start + new Vector3(dir.x * Random.Range(0.55f, 1.15f), dir.y * Random.Range(0.45f, 0.95f), 0f);

            shard.transform.DOMove(target, 0.42f).SetEase(Ease.OutQuad).SetTarget(shard).SetUpdate(UpdateType.Normal, false);
            shard.transform.DORotate(new Vector3(0f, 0f, Random.Range(-220f, 220f)), 0.5f).SetTarget(shard).SetUpdate(UpdateType.Normal, false);
            sr.DOFade(0f, 0.45f).SetDelay(0.18f).SetTarget(shard).SetUpdate(UpdateType.Normal, false);
        }

        Object.Destroy(root, 0.85f);
    }

    /// <summary>死亡/回收：清掉覆盖层与着色快照。</summary>
    public void Clear(in Context ctx)
    {
        Apply(false, ctx);
    }

    // ══════════════════════════════════════════════════════════════
    // 程序化贴图（全项目共用一份静态缓存）
    // ══════════════════════════════════════════════════════════════

    /// <summary>程序化冰壳贴图（圆角方块 + 内圈冷色 + 斜裂纹）。</summary>
    public static Sprite GetProceduralFrostSprite()
    {
        if (_proceduralFrostSprite != null) return _proceduralFrostSprite;

        const int size = 192;
        const int bevel = 26;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point,
            name = "IceBlock_Procedural"
        };

        var px = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = Mathf.Min(x, size - 1 - x);
                int dy = Mathf.Min(y, size - 1 - y);

                // 倒角：切掉四个角，读成“一块冰”而不是“一个圆”
                if (dx + dy < bevel)
                {
                    px[y * size + x] = new Color32(255, 255, 255, 0);
                    continue;
                }

                int edgeDist = Mathf.Min(dx, dy);
                float alpha;
                int r, g, b;

                if (edgeDist <= 1)
                {
                    alpha = 0.95f; r = 236; g = 250; b = 255;   // 外轮廓
                }
                else if (edgeDist <= 4)
                {
                    alpha = 0.42f; r = 150; g = 205; b = 240;   // 内圈冷色
                }
                else
                {
                    // 内部：半透明主体 + 左上到右下的斜切高光
                    float diag = (x - y) / (float)size;
                    alpha = 0.20f + 0.10f * Mathf.Abs(diag);
                    r = 118 + (int)(72f * Mathf.Clamp01(diag + 0.5f));
                    g = 180 + (int)(56f * Mathf.Clamp01(diag + 0.5f));
                    b = 235;
                }

                // 斜向裂纹
                bool crack = ((x + 2 * y) % 81 == 0) || ((2 * x - y + size) % 123 == 0);
                if (crack && edgeDist > 3)
                {
                    alpha = 0.78f; r = 240; g = 252; b = 255;
                }

                px[y * size + x] = new Color32((byte)r, (byte)g, (byte)b, (byte)(Mathf.Clamp01(alpha) * 255f));
            }
        }

        tex.SetPixels32(px);
        tex.Apply();
        _proceduralFrostSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 64f);
        return _proceduralFrostSprite;
    }

    /// <summary>破冰碎片占位精灵：亮边 + 不透明的三角化冰块。</summary>
    public static Sprite GetProceduralShardSprite()
    {
        if (_proceduralShardSprite != null) return _proceduralShardSprite;

        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point,
            name = "IceShard_Procedural"
        };

        var px = new Color32[size * size];
        float c = (size - 1) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - c;
                float dy = y - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / c;
                float ang = Mathf.Atan2(dy, dx);
                float edge = 0.82f + 0.16f * Mathf.Sin(ang * 3f + 1.7f);

                if (d > edge)
                {
                    px[y * size + x] = new Color32(0, 0, 0, 0);
                    continue;
                }

                bool rim = d > edge - 0.2f;
                px[y * size + x] = new Color32(
                    rim ? (byte)248 : (byte)150,
                    rim ? (byte)253 : (byte)218,
                    (byte)255,
                    rim ? (byte)255 : (byte)220);
            }
        }

        tex.SetPixels32(px);
        tex.Apply();
        _proceduralShardSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 32f);
        return _proceduralShardSprite;
    }

    /// <summary>把冰块切成 3x3 碎片，破冰时用于“整块碎开”。</summary>
    public static Sprite[] GetProceduralShardSprites()
    {
        if (_proceduralShardSprites != null) return _proceduralShardSprites;

        var baseSprite = GetProceduralFrostSprite();
        var tex = baseSprite.texture;
        const int cols = 3;
        const int rows = 3;
        int cw = tex.width / cols;
        int ch = tex.height / rows;
        _proceduralShardSprites = new Sprite[cols * rows];

        for (int gy = 0; gy < rows; gy++)
        {
            for (int gx = 0; gx < cols; gx++)
            {
                var rect = new Rect(gx * cw, gy * ch, cw, ch);
                _proceduralShardSprites[gy * cols + gx] =
                    Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), baseSprite.pixelsPerUnit);
            }
        }

        return _proceduralShardSprites;
    }
}
