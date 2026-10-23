using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 109 骑乘致死的第二段：无骑手战马“独立纯视觉对象”。
///
/// 为什么是独立对象而不是 109 的子物体或 Enemy_109.controller 的状态：
///   - 109 死亡后会回收到对象池（ResetEnemy → SetActive(false)），挂在它下面的子物体会一起消失；
///   - Enemy 会缓存所有子 Renderer 并建立闪白材质实例，SetParent 也无法清除旧引用；
///   - 控制器状态随对象池复用复位，赛马演出不能依赖它。
/// 因此本组件自带逐帧驱动、横向位移、渐隐与自销毁，不进任何 Enemy 控制器，不占排、不攻击、不奖励。
///
/// 时间语义：全部使用 <see cref="Time.unscaledDeltaTime"/>，与 Enemy 死亡 Tween 的 SetUpdate(true) 一致，
/// 暂停/三选一期间不会出现“骑手已演出、马却停住”的错位。
///
/// 密度补偿：素材是 960 源画布按原生像素补边到 1108，像素密度与生存动作（Enemy_109_MountedIdle1）不同，
/// 按项目约定密度差不改图片导入设置，而由本对象的 Sprite 子节点 localScale/localPosition 承担。
/// </summary>
public sealed class MountEscapeVisual : MonoBehaviour
{
    private const float DefaultFrameDuration = 0.12f;

    [Header("素材")]
    [Tooltip("战马逃跑帧（T80 透明版），按 Inspector 顺序播放。")]
    [SerializeField] private Sprite[] frames = new Sprite[0];
    [Tooltip("每帧显示时长（秒），长度应为帧数；为空时全部使用 frameDuration。")]
    [SerializeField] private float[] frameDurations = new float[0];
    [Tooltip("frameDurations 为空时使用的统一帧时长。")]
    [SerializeField, Min(0.01f)] private float frameDuration = DefaultFrameDuration;

    [Header("渲染")]
    [Tooltip("实际绘制精灵的子节点（其 localScale/localPosition 由 CavalryDeathSequence 写入密度补偿）。")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("相对来源 SpriteRenderer 的排序偏移；默认 +1，保证渐隐期间战马压在合成层之上。")]
    [SerializeField] private int sortingOrderOffset = 1;

    [Header("离场")]
    [Tooltip("是否按摄像机画面左侧移动（不假定世界 Vector3.left 就是屏幕左）。")]
    [SerializeField] private bool moveToCameraLeft = true;
    [Tooltip("离场位移距离（父物体本地单位），位移在整段帧播放时间内线性完成。")]
    [SerializeField, Min(0f)] private float moveDistance = 6.5f;
    [Tooltip("开始渐隐的时间点（0–1，按帧播放总时长归一化）。")]
    [SerializeField, Range(0f, 1f)] private float fadeStartNormalized = 0.5f;
    [Tooltip("渐隐时长（秒）。")]
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.6f;

    private static readonly List<MountEscapeVisual> ActiveVisuals = new List<MountEscapeVisual>();

    private Coroutine _routine;
    private Vector3 _startLocalPosition;
    private Vector3 _localMoveDirection = Vector3.left;
    private float _frameTotal;
    private Material _material;
    private Color _baseColor = Color.white;

    /// <summary>节点切换/清场时统一回收，避免残留无主视觉对象。</summary>
    public static void DespawnAll()
    {
        for (int i = ActiveVisuals.Count - 1; i >= 0; i--)
        {
            var visual = ActiveVisuals[i];
            if (visual != null) Destroy(visual.gameObject);
        }
        ActiveVisuals.Clear();
    }

    private void OnEnable()
    {
        if (!ActiveVisuals.Contains(this)) ActiveVisuals.Add(this);
    }

    private void OnDisable()
    {
        ActiveVisuals.Remove(this);
    }

    private void OnDestroy()
    {
        if (_material != null) Destroy(_material);
        _material = null;
    }

    /// <summary>
    /// 开始演出。由 CavalryDeathSequence 在交接帧调用。
    /// </summary>
    /// <param name="sourceRenderer">来源敌人根渲染器，用于继承排序层与排序序号。</param>
    /// <param name="densityOffset">Sprite 子节点的本地偏移（密度补偿，单位与敌人根本地一致）。</param>
    /// <param name="densityScale">Sprite 子节点的本地缩放（密度补偿）。</param>
    /// <param name="baseAlpha">起始不透明度（敌人生存时的排位透明度），避免交接瞬间变亮。</param>
    public void Play(SpriteRenderer sourceRenderer, Vector2 densityOffset, float densityScale, float baseAlpha)
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);

        if (spriteRenderer != null)
        {
            spriteRenderer.transform.localPosition = new Vector3(densityOffset.x, densityOffset.y, 0f);
            spriteRenderer.transform.localScale = new Vector3(densityScale, densityScale, 1f);
            if (sourceRenderer != null)
            {
                spriteRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
                spriteRenderer.sortingOrder = sourceRenderer.sortingOrder + sortingOrderOffset;

                // 与生存动作同一套着色器（109 用 EnemyOutline），但建独立材质实例，不与 Enemy 的闪白材质共用。
                if (sourceRenderer.sharedMaterial != null)
                {
                    if (_material != null) Destroy(_material);
                    _material = new Material(sourceRenderer.sharedMaterial);
                    spriteRenderer.sharedMaterial = _material;
                }
            }
            _baseColor = new Color(1f, 1f, 1f, Mathf.Clamp01(baseAlpha));
            if (_material != null) _material.color = _baseColor;
            spriteRenderer.color = Color.white;
        }

        _startLocalPosition = transform.localPosition;
        _localMoveDirection = ResolveCameraLeftDirection();
        _frameTotal = ComputeFrameTotal();

        if (frames != null && frames.Length > 0 && spriteRenderer != null)
            spriteRenderer.sprite = frames[0];

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(PlaybackRoutine());
    }

    private Vector3 ResolveCameraLeftDirection()
    {
        Vector3 worldDirection;
        if (moveToCameraLeft && Camera.main != null)
        {
            // 画面左侧 = 摄像机本地 -X 方向；不假定世界 Vector3.left 就是屏幕左。
            worldDirection = -Camera.main.transform.right;
        }
        else
        {
            worldDirection = Vector3.left;
        }

        // 2D 侧视角：位移只在屏幕平面内，剔除摄像机前后分量。
        Vector3 cameraForward = Camera.main != null ? Camera.main.transform.forward : Vector3.forward;
        worldDirection -= Vector3.Dot(worldDirection, cameraForward) * cameraForward;
        if (worldDirection.sqrMagnitude < 0.000001f) worldDirection = Vector3.left;
        worldDirection.Normalize();

        Transform parent = transform.parent;
        if (parent == null) return worldDirection;

        Vector3 local = parent.InverseTransformDirection(worldDirection);
        local.z = 0f;
        return local.sqrMagnitude < 0.000001f ? Vector3.left : local.normalized;
    }

    private float FrameDurationAt(int index)
    {
        if (frameDurations != null && frameDurations.Length > 0)
        {
            int i = Mathf.Clamp(index, 0, frameDurations.Length - 1);
            if (frameDurations[i] > 0f) return frameDurations[i];
        }
        return Mathf.Max(0.01f, frameDuration);
    }

    private float ComputeFrameTotal()
    {
        int count = frames != null ? frames.Length : 0;
        if (count == 0) return Mathf.Max(0.01f, frameDuration);

        float total = 0f;
        for (int i = 0; i < count; i++) total += FrameDurationAt(i);
        return Mathf.Max(0.01f, total);
    }

    private IEnumerator PlaybackRoutine()
    {
        int frameCount = frames != null ? frames.Length : 0;
        int currentFrame = -1;
        float elapsed = 0f;
        float fadeStartTime = fadeStartNormalized * _frameTotal;
        float endTime = Mathf.Max(_frameTotal, fadeStartTime + fadeDuration);

        while (elapsed < endTime)
        {
            if (spriteRenderer == null) break;

            // 帧推进
            if (frameCount > 0)
            {
                float acc = 0f;
                int wanted = frameCount - 1;
                for (int i = 0; i < frameCount; i++)
                {
                    acc += FrameDurationAt(i);
                    if (elapsed < acc) { wanted = i; break; }
                }
                if (wanted != currentFrame)
                {
                    currentFrame = wanted;
                    spriteRenderer.sprite = frames[wanted];
                }
            }

            // 横向离场
            float progress = Mathf.Clamp01(elapsed / _frameTotal);
            transform.localPosition = _startLocalPosition + _localMoveDirection * (moveDistance * progress);

            // 渐隐
            float alpha = 1f;
            if (elapsed > fadeStartTime)
                alpha = 1f - Mathf.Clamp01((elapsed - fadeStartTime) / fadeDuration);
            if (_material != null)
            {
                Color color = _baseColor;
                color.a = _baseColor.a * alpha;
                _material.color = color;
            }
            else
            {
                Color color = spriteRenderer.color;
                color.a = alpha;
                spriteRenderer.color = color;
            }

            elapsed += GameplayPause.UnscaledDelta;
            yield return null;
        }

        _routine = null;
        Destroy(gameObject);
    }
}
