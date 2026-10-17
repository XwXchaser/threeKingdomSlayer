using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 脚底对齐：把所有敌人的真实精灵剪影最低点统一压到同一条地面基准线上。
///
/// 背景：所有敌人精灵的锚点都在画布中心（pivot 0.5,0.5），画布尺寸又各不相同，
/// 因此脚底到节点原点的距离因敌人而异。过去靠每个 Prefab 手填 visualYOffset 补偿，
/// 但数值来自"画布半高"而非真实剪影，109 这类高大单位会陷进地里。
///
/// 做法：取 Sprite.vertices（Tight 网格，不含透明边）的最低点算出所需偏移，写入
/// Enemy.runtimeVisualYOffset，与 Inspector 的 visualYOffset 相加，二者互不覆盖。
/// 按 sprite 缓存结果，不在逐帧路径上做几何计算。
///
/// 采样时机：首次拿到有效 sprite 时对齐一次，之后保持不动。刻意不做逐帧跟随，
/// 因为出招、死亡、QTE 等帧的剪影会伸到地面线以下，逐帧对齐会让单位上下浮动。
/// 多形态单位（如 109 落马换渲染层）请在其视觉控制器切换后调用 Recapture()。
/// </summary>
[DefaultExecutionOrder(-500)]
[RequireComponent(typeof(Enemy))]
public sealed class EnemyGroundAligner : MonoBehaviour
{
    [Tooltip("地面基准线世界 Y。所有敌人的脚底对齐到此值。")]
    public float groundY = -1.8f;

    [Tooltip("额外微调（世界单位），用于个别单位视觉上再压或抬一点。")]
    public float extraOffset = 0f;

    [Tooltip("负责落地的那一层 SpriteRenderer。留空则取第一个子 SpriteRenderer。")]
    public SpriteRenderer visualRenderer;

    private Enemy _enemy;
    private Transform _cachedTransform;
    private Sprite _sampledSprite;
    private bool _aligned;

    private static readonly Dictionary<Sprite, float> MeshMinYCache = new Dictionary<Sprite, float>();
    private static readonly Dictionary<Sprite, float> MeshMaxYCache = new Dictionary<Sprite, float>();

    private void Awake()
    {
        _enemy = GetComponent<Enemy>();
        _cachedTransform = transform;
        if (visualRenderer == null) visualRenderer = GetComponentInChildren<SpriteRenderer>(true);
    }

    private void LateUpdate()
    {
        // 只在尚未对齐时采样一次。刻意不跟随 sprite 换帧：出招/死亡/QTE 的剪影会伸到
        // 地面线以下（104 的 QTE 帧比 Idle 低约 1.9 世界单位），逐帧对齐会让单位上下浮动。
        if (_aligned) return;
        if (visualRenderer == null || visualRenderer.sprite == null) return;
        Align();
    }

    /// <summary>
    /// 重新采样当前精灵并重算偏移。视觉形态切换（骑乘/落马、换渲染层）后调用。
    /// </summary>
    public void Recapture()
    {
        _aligned = false;
        _sampledSprite = null;
        Align();
    }

    private void Align()
    {
        if (_enemy == null || visualRenderer == null) return;
        var sprite = visualRenderer.sprite;
        if (sprite == null) return;

        float ownScaleY = _cachedTransform.localScale.y;
        if (Mathf.Approximately(ownScaleY, 0f)) return;

        float minLocalY = GetMeshMinY(sprite);

        // 注意：localPosition 位于父节点空间，不被自身的 localScale 缩放；
        // 但会被父节点的 lossyScale 缩放。这里把父节点也纳进来，避免父级带缩放时算错。
        float parentScaleY = 1f;
        float parentWorldY = 0f;
        if (_cachedTransform.parent != null)
        {
            parentScaleY = _cachedTransform.parent.lossyScale.y;
            parentWorldY = _cachedTransform.parent.position.y;
        }
        if (Mathf.Approximately(parentScaleY, 0f)) return;

        // 静息态（bounce = 0）下要求：
        // parentWorldY + parentScaleY * (visualYOffset + runtime) + minLocalY * ownScaleY == groundY
        float requiredBaseOffset = (groundY + extraOffset - parentWorldY - minLocalY * ownScaleY) / parentScaleY;
        float nextRuntime = requiredBaseOffset - _enemy.visualYOffset;

        _sampledSprite = sprite;
        _aligned = true;

        if (Mathf.Approximately(nextRuntime, _enemy.runtimeVisualYOffset)) return;
        _enemy.runtimeVisualYOffset = nextRuntime;
        _enemy.ApplyVisualOffsetNow();
    }

    private static float GetMeshMinY(Sprite sprite)
    {
        if (MeshMinYCache.TryGetValue(sprite, out var cached)) return cached;
        var verts = sprite.vertices;
        float min = float.MaxValue;
        for (int i = 0; i < verts.Length; i++)
            if (verts[i].y < min) min = verts[i].y;
        if (min == float.MaxValue) min = sprite.bounds.min.y;
        MeshMinYCache[sprite] = min;
        return min;
    }

    /// <summary>清空剪影缓存（精灵重新导入后调用）。</summary>
    public static void ClearCache()
    {
        MeshMinYCache.Clear();
        MeshMaxYCache.Clear();
    }

    /// <summary>
    /// 精灵真实剪影在网格空间的 Y 范围（不含 Tight 打包的透明边距）。
    /// 供血条等需要避开透明边距的系统使用。
    /// </summary>
    public static bool TryGetSilhouetteBounds(Sprite sprite, out float minY, out float maxY)
    {
        minY = maxY = 0f;
        if (sprite == null) return false;

        if (!MeshMinYCache.TryGetValue(sprite, out var lo))
        {
            var verts = sprite.vertices;
            lo = float.MaxValue;
            float hi = float.MinValue;
            for (int i = 0; i < verts.Length; i++)
            {
                if (verts[i].y < lo) lo = verts[i].y;
                if (verts[i].y > hi) hi = verts[i].y;
            }
            if (lo == float.MaxValue) { lo = sprite.bounds.min.y; hi = sprite.bounds.max.y; }
            MeshMinYCache[sprite] = lo;
            MeshMaxYCache[sprite] = hi;
        }

        if (!MeshMaxYCache.TryGetValue(sprite, out var top)) return false;
        minY = lo;
        maxY = top;
        return true;
    }

    /// <summary>
    /// 精灵真实剪影高度，单位为精灵自身网格空间（等价像素/PPU），
    /// 不含 Tight 打包的透明边距。调用方再乘以根节点 lossyScale 得到世界高度。
    /// </summary>
    public static bool TryGetSilhouetteHeight(Sprite sprite, out float localHeight)
    {
        localHeight = 0f;
        if (!TryGetSilhouetteBounds(sprite, out var lo, out var hi)) return false;
        localHeight = hi - lo;
        return localHeight > 0f;
    }
}
