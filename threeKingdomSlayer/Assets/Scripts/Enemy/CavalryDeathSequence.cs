using System.Collections;
using UnityEngine;

/// <summary>
/// 109 骑乘致死的视觉路由：第一段「骑手受击摔落」。
///
/// 与既有系统的分工：
///   - <see cref="Enemy.Die"/> 在非击飞死亡时把演出交给本组件（<see cref="Enemy.CustomDeathVisualHandler"/>），
///     因此不再叠加 DeathBounceAndFall 的根弹起／随机旋转／掉落；击飞死亡仍走原有 LaunchDeathEffect。
///   - 第二段「无骑手战马逃跑」由 <see cref="MountEscapeVisual"/> 以独立纯视觉对象接管。
///
/// 为什么不放进 Enemy_109.controller：
///   死亡演出要在 109 回池之后继续（战马逃跑），而控制器状态随对象池复用复位；
///   并且素材是 960 源画布按原生像素补边到 1108，像素密度与生存动作不同，
///   密度差按项目约定放在独立视觉对象的 scale 上，不能改根 SpriteRenderer（根 scale 属于单位、碰撞盒与血条基准）。
///
/// 素材数据全部通过 Inspector 序列化引用，不使用 Resources.Load／字符串查找。
/// </summary>
[RequireComponent(typeof(Enemy))]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class CavalryDeathSequence : MonoBehaviour
{
    private const float DefaultFrameDuration = 0.1f;

    [Header("密度补偿（960 → 1108 原生像素补边）")]
    [Tooltip("把 960 源画布按原生像素补边进 1108 后，与生存动作 Enemy_109_MountedIdle1 的尺度比。" +
             "由 RiderFall f001 与 MountedIdle1 剪影拟合得 1.2065（IoU 0.9711）。改动需重新拟合。")]
    [SerializeField, Min(0.5f)] private float densityScale = 1.2065f;
    [Tooltip("密度补偿后 Sprite 子节点的本地偏移（敌人根本地单位，未经单位 scale）。" +
             "按补边偏移 (74,120) 与 pivot 中心推导为 (0.0075, 0.5859)。")]
    [SerializeField] private Vector2 densityOffset = new Vector2(0.0075f, 0.5859f);
    [Tooltip("开关：关闭后按 1:1 直接显示（画面会整体小约 17% 且脚底抬高约 76px，仅供对照排查）。")]
    [SerializeField] private bool applyDensityCompensation = true;
    [Tooltip("按帧进度（0=第一帧,1=最后一帧）额外下移的像素（正值=画面下移）。" +
             "用于个别帧马蹄偏高：源片里马在落地前后会抬蹄，不影响其他帧。")]
    [SerializeField] private AnimationCurve fallVerticalOffsetPx = AnimationCurve.Linear(0f, 0f, 1f, 0f);
    [Tooltip("战马逃跑段的密度/接地补偿（本地单位）。源片②自身接地行比第一段高很多，" +
             "需单独偏移才能与 idle 的脚线一致：默认 (0.0075, -6.644) 使首帧脚底落在生存脚线。")]
    [SerializeField] private Vector2 mountEscapeOffset = new Vector2(0.0075f, -6.644f);

    [Header("第一段：骑手摔落（12 帧）")]
    [Tooltip("视频① 12 帧（T79 透明版），按 Inspector 顺序播放；最后一帧为交接帧（骑手倒地＋马站立）。")]
    [SerializeField] private Sprite[] riderFallFrames = new Sprite[0];
    [Tooltip("每帧显示时长（秒）。长度应为帧数−1；不足时沿用最后一个值。")]
    [SerializeField] private float[] riderFallFrameDurations = new float[0];
    [Tooltip("第一段整体时间缩放：>1 更慢，<1 更快。默认值来自视频①的原始抽帧时间戳。")]
    [SerializeField, Min(0.05f)] private float fallTimeScale = 0.8f;
    [Tooltip("入场混合时长（秒）：死亡瞬间用当前精灵淡出、f001 淡入，避免从冲锋姿势硬切。0 = 直接切。")]
    [SerializeField, Min(0f)] private float entryBlendDuration = 0.1f;
    [Tooltip("交接帧额外保持时长（秒），之后战马起步。")]
    [SerializeField, Min(0f)] private float handoffHold = 0.2f;

    [Header("第二段：交接")]
    [Tooltip("独立纯视觉战马 Prefab（自带 MountEscapeVisual）。为空时只做倒地淡出，不生成战马。")]
    [SerializeField] private MountEscapeVisual mountEscapePrefab;
    [Tooltip("交接时合成层（倒地骑手＋空马）的淡出时长；0 = 交接瞬间移除。倒地骑手是否保留由该值决定。")]
    [SerializeField, Min(0f)] private float corpseFadeDuration = 0.35f;

    private Enemy _enemy;
    private CavalryEnemy _cavalry;
    private Animator _animator;
    private SpriteRenderer _enemyRenderer;
    private Coroutine _routine;

    private GameObject _fallVisualRoot;
    private SpriteRenderer _fallVisualRenderer;
    private Transform _fallVisualSprite;
    private Material _fallVisualMaterial;
    private MountEscapeVisual _escapedMount;

    private void Awake()
    {
        _enemy = GetComponent<Enemy>();
        _cavalry = GetComponent<CavalryEnemy>();
        _animator = GetComponent<Animator>();
        _enemyRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        _enemy ??= GetComponent<Enemy>();
        if (_enemy != null) _enemy.CustomDeathVisualHandler = TryHandleMountedDeath;
    }

    private void OnDisable()
    {
        if (_enemy != null) _enemy.CustomDeathVisualHandler = null;

        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        // 序列被打断（清场／节点切换）时才回收战马；正常交接结束后 _escapedMount 已置空，由它自己退场。
        if (_escapedMount != null)
        {
            Destroy(_escapedMount.gameObject);
            _escapedMount = null;
        }

        DestroyFallVisual();
        RestoreEnemyVisual();
    }

    /// <summary>Enemy.Die() 的非击飞分支调用；返回 true 表示本组件接管演出。</summary>
    private bool TryHandleMountedDeath(Enemy enemy)
    {
        if (enemy == null || enemy != _enemy) return false;
        if (_cavalry == null || !_cavalry.IsMounted) return false;               // 已落马的 109 交回默认死亡演出
        if (riderFallFrames == null || riderFallFrames.Length == 0) return false; // 素材缺失时不吞掉默认演出

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(PlayDeathSequence());
        return true;
    }

    private IEnumerator PlayDeathSequence()
    {
        // 根渲染器由本演出接管：避免 Animator 的 Dead 状态（当前复用 MountedIdle）覆盖逐帧精灵。
        if (_animator != null) _animator.enabled = false;

        CreateFallVisual();

        // 入场混合：死亡可能发生在冲锋/刺击姿势，直接切到 f001 会跳变。
        yield return BlendInFallVisual();

        int frameCount = riderFallFrames.Length;
        for (int i = 0; i < frameCount; i++)
        {
            if (_fallVisualRenderer != null) _fallVisualRenderer.sprite = riderFallFrames[i];
            ApplyFallFootDrift(i, frameCount);
            float duration = FrameDurationAt(i);
            if (duration > 0f) yield return WaitUnscaled(duration);
        }

        if (handoffHold > 0f) yield return WaitUnscaled(handoffHold);

        // 交接：生成独立战马，合成层（倒地骑手＋空马）按配置淡出。
        SpawnEscapedMount();

        if (corpseFadeDuration > 0f && _fallVisualRenderer != null)
        {
            float elapsed = 0f;
            Color baseColor = _fallVisualMaterial != null ? _fallVisualMaterial.color : Color.white;
            while (elapsed < corpseFadeDuration)
            {
                if (_fallVisualMaterial == null) break;
                elapsed += Time.unscaledDeltaTime;
                Color color = baseColor;
                color.a = baseColor.a * (1f - Mathf.Clamp01(elapsed / corpseFadeDuration));
                _fallVisualMaterial.color = color;
                yield return null;
            }
        }

        DestroyFallVisual();
        _routine = null;
        // 交接完成后战马已是独立对象，不再由 109 的生命周期负责回收（清场由 MountEscapeVisual.DespawnAll 负责）。
        _escapedMount = null;

        // 演出结束：走 Enemy 的统一收尾（触发 OnDeathAnimComplete 并回对象池）。
        if (_enemy != null) _enemy.CompleteCustomDeathVisualSequence();
    }

    /// <summary>入场混合：根精灵淡出、f001 淡入；结束后关闭根渲染器，由逐帧视觉独占画面。</summary>
    private IEnumerator BlendInFallVisual()
    {
        Material rootMaterial = _enemyRenderer != null ? _enemyRenderer.sharedMaterial : null;
        if (entryBlendDuration > 0f && rootMaterial != null && _fallVisualMaterial != null)
        {
            Color rootBase = rootMaterial.color;
            Color visualBase = _fallVisualMaterial.color;
            float elapsed = 0f;
            while (elapsed < entryBlendDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / entryBlendDuration);
                Color rc = rootBase;
                rc.a = rootBase.a * (1f - t);
                rootMaterial.color = rc;
                Color vc = visualBase;
                vc.a = visualBase.a * t;
                _fallVisualMaterial.color = vc;
                yield return null;
            }
            // 还原材质颜色，避免对象池复用时残留半透明
            rootMaterial.color = rootBase;
            _fallVisualMaterial.color = visualBase;
        }

        if (_enemyRenderer != null) _enemyRenderer.enabled = false;
    }

    /// <summary>
    /// 落地阶段的脚线漂移补偿：沿帧序线性下移（不是逐帧钉脚，保留步态起伏）。
    /// 1 单位 offY = 16 画布像素（Δrow = −16·ΔoffY），所以下移 P 像素 = offY − P/16。
    /// </summary>
    private void ApplyFallFootDrift(int frameIndex, int frameCount)
    {
        if (_fallVisualSprite == null) return;
        if (!applyDensityCompensation) return;

        float t = frameCount > 1 ? (float)frameIndex / (frameCount - 1) : 0f;
        float downPx = fallVerticalOffsetPx != null ? fallVerticalOffsetPx.Evaluate(t) : 0f;
        float y = densityOffset.y - downPx / 16f;
        _fallVisualSprite.localPosition = new Vector3(_fallVisualSprite.localPosition.x, y, 0f);
    }

    private float FrameDurationAt(int index)
    {
        float raw;
        if (riderFallFrameDurations != null && riderFallFrameDurations.Length > 0)
        {
            int i = Mathf.Clamp(index, 0, riderFallFrameDurations.Length - 1);
            raw = riderFallFrameDurations[i];
        }
        else
        {
            raw = DefaultFrameDuration;
        }
        if (raw <= 0f) raw = DefaultFrameDuration;
        return raw * Mathf.Max(0.05f, fallTimeScale);
    }

    private static IEnumerator WaitUnscaled(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void CreateFallVisual()
    {
        DestroyFallVisual();

        _fallVisualRoot = new GameObject("Enemy109_RiderFallVisual");
        _fallVisualRoot.transform.SetParent(transform, false);
        _fallVisualRoot.transform.localPosition = Vector3.zero;
        _fallVisualRoot.transform.localRotation = Quaternion.identity;
        _fallVisualRoot.transform.localScale = Vector3.one;

        var spriteObject = new GameObject("Sprite");
        spriteObject.transform.SetParent(_fallVisualRoot.transform, false);
        float scale = applyDensityCompensation ? densityScale : 1f;
        spriteObject.transform.localPosition = applyDensityCompensation
            ? new Vector3(densityOffset.x, densityOffset.y, 0f) : Vector3.zero;
        spriteObject.transform.localScale = new Vector3(scale, scale, 1f);

        _fallVisualRenderer = spriteObject.AddComponent<SpriteRenderer>();
        _fallVisualSprite = spriteObject.transform;
        // 与生存动作同一套着色器（109 用 EnemyOutline），但建独立材质实例：
        // Enemy 的 flashMaterials 驱动闪白/排位透明度，不能与之共用。
        Material source = _enemyRenderer != null ? _enemyRenderer.sharedMaterial : null;
        if (source != null)
        {
            _fallVisualMaterial = new Material(source);
            _fallVisualRenderer.sharedMaterial = _fallVisualMaterial;
            // 闪白会把材质 alpha 拉成 1；这里统一用排位透明度作为基准，避免死亡瞬间跳亮
            _fallVisualMaterial.color = new Color(1f, 1f, 1f, _enemy != null ? _enemy.CurrentVisualAlpha : 1f);
        }
        if (_enemyRenderer != null)
        {
            _fallVisualRenderer.sortingLayerID = _enemyRenderer.sortingLayerID;
            _fallVisualRenderer.sortingOrder = _enemyRenderer.sortingOrder;
        }
        if (riderFallFrames.Length > 0) _fallVisualRenderer.sprite = riderFallFrames[0];
    }

    private void DestroyFallVisual()
    {
        if (_fallVisualRoot != null) Destroy(_fallVisualRoot);
        if (_fallVisualMaterial != null) Destroy(_fallVisualMaterial);
        _fallVisualRoot = null;
        _fallVisualRenderer = null;
        _fallVisualSprite = null;
        _fallVisualMaterial = null;
    }

    private void SpawnEscapedMount()
    {
        if (mountEscapePrefab == null) return;

        // 与敌人同一父节点、同一本地坐标系：交接瞬间的尺度/朝向/位置与合成层完全一致。
        var mount = Instantiate(mountEscapePrefab, transform.parent);
        mount.transform.localPosition = transform.localPosition;
        mount.transform.localRotation = transform.localRotation;
        mount.transform.localScale = transform.localScale;
        mount.Play(_enemyRenderer, mountEscapeOffset, applyDensityCompensation ? densityScale : 1f,
            _enemy != null ? _enemy.CurrentVisualAlpha : 1f);
        _escapedMount = mount;
    }

    /// <summary>对象池复用前恢复被本演出关掉的根渲染器/Animator。</summary>
    private void RestoreEnemyVisual()
    {
        if (_animator != null) _animator.enabled = true;
        if (_enemyRenderer != null) _enemyRenderer.enabled = true;
    }
}
