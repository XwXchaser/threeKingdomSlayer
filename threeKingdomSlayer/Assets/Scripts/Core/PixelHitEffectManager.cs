using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;


public sealed partial class PixelHitEffectManager : MonoBehaviour
{
    private sealed class EffectInstance
    {
        public GameObject root;
        public Transform visualRoot;
        public SpriteRenderer center;
        public SpriteRenderer[] rays;
        public Sequence sequence;
    }

    public static PixelHitEffectManager Instance { get; private set; }

    [Header("对象池")]
    [SerializeField] private int initialPoolSize = 12;

    [Header("位置")]
    [SerializeField] private float bodyYOffset = 0.8f;
    [SerializeField] private float cameraDepthOffset = 0.05f;

    [Header("Standard")]
    [SerializeField] private float standardSize = 0.32f;
    [SerializeField] private float standardRayLength = 0.42f;
    [SerializeField] private float standardDuration = 0.10f;

    [Header("Heavy")]
    [SerializeField] private float heavySize = 0.48f;
    [SerializeField] private float heavyRayLength = 0.68f;
    [SerializeField] private float heavyDuration = 0.14f;

    [Header("Slash 斩口")]
    [SerializeField] private float slashLength = 1.35f;
    [SerializeField] private float slashWidth = 0.14f;
    [SerializeField] private float slashDuration = 0.16f;
    [SerializeField] private float slashAngle = 12f;

    [Header("颜色")]
    [SerializeField] private Color stabColor = new Color(1f, 0.78f, 0.16f, 1f);
    [SerializeField] private Color slashColor = new Color(1f, 0.68f, 0.10f, 1f);
    [SerializeField] private Color slashCoreColor = new Color(1f, 0.96f, 0.72f, 1f);
    [SerializeField] private Color slashShadowColor = new Color(0.9f, 0.28f, 0.03f, 1f);
    [SerializeField] private Color pierceColor = new Color(1f, 0.86f, 0.32f, 1f);
    [SerializeField] private Color heavyColor = new Color(1f, 0.48f, 0.06f, 1f);

    private readonly Queue<EffectInstance> _pool = new Queue<EffectInstance>();
    private Sprite _pixelSprite;
    private Sprite _sparkLongSprite;
    private Sprite _sparkForkSprite;
    private Sprite _sparkChipSprite;
    private Sprite[] _slashFrames;
    private int _slashVariantIndex;
    private const int SlashVariantCount = 4;
    private const int SlashFramesPerVariant = 3;
    private const int StabVariantCount = 4;
    private const int StabFramesPerVariant = 3;

    // 用户确认：像素戳击命中特效缩小到当前的 60%，挑飞命中特效缩小到当前的 55%。
    // Slash 分支保持原尺寸不变。
    private const float StabImpactScale = 0.60f;
    private const float LaunchImpactScale = 0.55f;

    private Sprite[] _stabFrames;
    private int _stabVariantIndex;
    private Sprite[] _diseaseStabFrames;
    private int _diseaseStabVariantIndex;
    private const int DiseaseStabVariantCount = 4;
    private const int DiseaseStabFramesPerVariant = 3;
    private readonly List<Texture2D> _generatedTextures = new List<Texture2D>();
    private Transform _poolRoot;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        CreatePixelSprite();
        CreateSparkSprites();
        CreateSlashSprites();
        CreateStabSprites();
        CreateDiseaseStabSprites();
        var root = new GameObject("PixelHitEffectPool");
        root.transform.SetParent(transform, false);
        _poolRoot = root.transform;

        for (int i = 0; i < initialPoolSize; i++)
            _pool.Enqueue(CreateInstance());
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        while (_pool.Count > 0)
        {
            var instance = _pool.Dequeue();
            instance.sequence?.Kill(false);
        }

        if (_pixelSprite != null)
            Destroy(_pixelSprite);
        if (_sparkLongSprite != null)
            Destroy(_sparkLongSprite);
        if (_sparkForkSprite != null)
            Destroy(_sparkForkSprite);
        if (_sparkChipSprite != null)
            Destroy(_sparkChipSprite);
        if (_slashFrames != null)
        {
            for (int i = 0; i < _slashFrames.Length; i++)
            {
                if (_slashFrames[i] != null)
                    Destroy(_slashFrames[i]);
            }
        }
        if (_stabFrames != null)
        {
            for (int i = 0; i < _stabFrames.Length; i++)
            {
                if (_stabFrames[i] != null)
                    Destroy(_stabFrames[i]);
            }
        }
        if (_diseaseStabFrames != null)
        {
            for (int i = 0; i < _diseaseStabFrames.Length; i++)
            {
                if (_diseaseStabFrames[i] != null)
                    Destroy(_diseaseStabFrames[i]);
            }
        }
        for (int i = 0; i < _generatedTextures.Count; i++)
        {
            if (_generatedTextures[i] != null)
                Destroy(_generatedTextures[i]);
        }
        _generatedTextures.Clear();
    }

    public void RequestHit(HitFeedbackContext context)
    {
        bool isSlashLightHit = context.damageType == DamageType.Slash
            && context.strength == HitFeedbackStrength.Light;
        if ((!isSlashLightHit && context.strength < HitFeedbackStrength.Standard)
            || context.source == HitFeedbackSource.Dot)
            return;

        var instance = _pool.Count > 0 ? _pool.Dequeue() : CreateInstance();
        Play(instance, context);
    }

    private void Play(EffectInstance instance, HitFeedbackContext context)
    {
        Sequence previous = instance.sequence;
        instance.sequence = null;
        if (previous != null && previous.IsActive())
            previous.Kill(false);
        instance.root.SetActive(true);
        Vector3 impactWorldPosition = context.hasImpactPosition
            ? context.worldPosition
            : context.worldPosition + Vector3.up * bodyYOffset;
        instance.root.transform.position = impactWorldPosition;
        if (Camera.main != null)
            instance.root.transform.position += Camera.main.transform.forward * cameraDepthOffset;
        instance.root.transform.rotation = Camera.main != null
            ? Quaternion.LookRotation(-Camera.main.transform.forward, Camera.main.transform.up)
            : Quaternion.identity;
        instance.visualRoot.localPosition = Vector3.zero;

        bool heavy = context.strength == HitFeedbackStrength.Heavy;
        float size = heavy ? heavySize : standardSize;
        float rayLength = heavy ? heavyRayLength : standardRayLength;
        float duration = heavy ? heavyDuration : standardDuration;
        Color color = heavy ? heavyColor : GetDamageTypeColor(context.damageType);

        instance.sequence = GameplayPause.Bind(DOTween.Sequence().SetTarget(instance.root).SetUpdate(UpdateType.Normal, true));
        if (context.isDiseaseStabHit && context.damageType == DamageType.Stab)
        {
            BuildDiseaseStabEffect(instance, heavy);
        }
        else if (context.damageType == DamageType.Launch)
        {
            BuildLaunchEffect(instance, context.impactDirection, heavy);
        }
        else if (context.damageType == DamageType.Slash)
        {
            bool fullSlash = context.strength >= HitFeedbackStrength.Standard;
            BuildSlashEffect(instance, context.impactDirection, heavy, fullSlash);
        }
        else if (context.damageType == DamageType.Stab || context.damageType == DamageType.Pierce)
        {
            BuildStabEffect(instance, context.impactDirection, heavy);
        }
        else
        {
            BuildDirectionalBurst(instance, context.impactDirection, color, size, rayLength, duration);
        }

        var sequence = instance.sequence;
        sequence.OnComplete(() => ReturnToPool(instance, sequence));
        sequence.OnKill(() => ReturnToPool(instance, sequence));
    }

    public void AttachSlashTrail(Transform carrier, bool leftToRight, float visualTilt, float lifetime)
    {
        if (carrier == null)
            return;

        var trailRoot = new GameObject("SlashSparkTrail");
        trailRoot.transform.SetParent(carrier, false);
        Vector3 worldDirection = Quaternion.Euler(0f, 0f, visualTilt)
            * (leftToRight ? Vector3.right : Vector3.left);
        Vector3 localDirection = carrier.InverseTransformDirection(worldDirection).normalized;
        localDirection.z = 0f;
        localDirection.Normalize();
        Vector3 perpendicular = new Vector3(-localDirection.y, localDirection.x, 0f);
        float travelSign = localDirection.x >= 0f ? 1f : -1f;
        float duration = Mathf.Clamp(lifetime * 0.42f, 0.14f, 0.24f);
        var trailSequence = DOTween.Sequence().SetTarget(trailRoot.transform).SetUpdate(UpdateType.Normal, false);
        trailSequence.SetLink(trailRoot, LinkBehaviour.KillOnDestroy);

        for (int i = 0; i < 3; i++)
        {
            var sparkObject = new GameObject($"TrailSpark_{i}");
            sparkObject.transform.SetParent(trailRoot.transform, false);
            var renderer = sparkObject.AddComponent<SpriteRenderer>();
            ConfigureRenderer(renderer);
            renderer.sprite = i == 0 ? _sparkLongSprite : i == 1 ? _sparkForkSprite : _sparkChipSprite;
            renderer.color = i == 1 ? slashColor : slashCoreColor;
            renderer.flipX = travelSign < 0f;

            float side = i == 0 ? -1f : i == 1 ? 1f : -1f;
            float startOffset = 0.12f + i * 0.06f;
            float endOffset = 0.48f + i * 0.08f;
            Vector3 start = -localDirection * startOffset + perpendicular * side * 0.04f;
            Vector3 end = localDirection * endOffset + perpendicular * side * (0.12f + i * 0.04f);
            float scale = i == 0 ? 0.26f : i == 1 ? 0.21f : 0.16f;
            sparkObject.transform.localPosition = start;
            sparkObject.transform.localRotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(localDirection.y, localDirection.x) * Mathf.Rad2Deg + side * 16f);
            sparkObject.transform.localScale = new Vector3(scale * 0.45f, 0.06f, 1f);

            var sequence = DOTween.Sequence().SetTarget(sparkObject.transform).SetUpdate(UpdateType.Normal, false);
            sequence.AppendInterval(i * 0.025f);
            sequence.Append(sparkObject.transform.DOScale(new Vector3(scale, 0.12f, 1f), duration * 0.18f).SetEase(Ease.OutBack));
            sequence.Join(sparkObject.transform.DOLocalMove(end, duration * 0.82f).SetEase(Ease.OutCubic));
            sequence.Append(sparkObject.transform.DOScale(Vector3.zero, duration * 0.22f).SetEase(Ease.InQuad));
            trailSequence.Join(sequence);
        }

        trailSequence.AppendInterval(duration + 0.05f);
        trailSequence.OnComplete(() =>
        {
            if (trailRoot != null)
                Destroy(trailRoot);
        });
        trailSequence.OnKill(() =>
        {
            if (trailRoot != null)
                Destroy(trailRoot);
        });
    }

    private void BuildStabEffect(EffectInstance instance, Vector3 impactDirection, bool heavy)
    {
        int variant = _stabVariantIndex++ % StabVariantCount;
        int frameBase = variant * StabFramesPerVariant;
        int seed = unchecked(instance.root.GetInstanceID() * 397 ^ Time.frameCount * 31);
        var random = new System.Random(seed);
        float Next(float min, float max) => min + (float)random.NextDouble() * (max - min);

        float duration = heavy ? 0.34f : 0.28f;
        float peakScale = 2.4f * (heavy ? 1.536f : 1.344f)
            * StabImpactScale * Next(0.97f, 1.03f);
        float rotation = Next(-5f, 5f);
        float contactEnd = duration * 0.14f;
        float burstEnd = duration * 0.54f;
        float holdEnd = duration * 0.72f;

        instance.center.enabled = true;
        instance.center.sprite = _stabFrames[frameBase];
        instance.center.color = Color.white;
        instance.center.transform.localPosition = Vector3.zero;
        instance.center.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        instance.center.transform.localScale = Vector3.one * (peakScale * 0.34f);

        instance.sequence.Insert(0f,
            instance.center.transform.DOScale(Vector3.one * (peakScale * 0.58f), contactEnd)
                .SetEase(Ease.OutCubic));
        instance.sequence.InsertCallback(contactEnd, () =>
        {
            instance.center.sprite = _stabFrames[frameBase + 1];
            instance.center.transform.localScale = Vector3.one * (peakScale * 0.64f);
        });
        instance.sequence.Insert(contactEnd,
            instance.center.transform.DOScale(Vector3.one * peakScale, burstEnd - contactEnd)
                .SetEase(Ease.OutBack, 1.15f));
        instance.sequence.InsertCallback(burstEnd, () =>
        {
            instance.center.sprite = _stabFrames[frameBase + 2];
            instance.center.transform.localScale = Vector3.one * peakScale;
        });
        instance.sequence.Insert(holdEnd,
            instance.center.transform.DOScale(Vector3.one * (peakScale * 0.9f), duration - holdEnd)
                .SetEase(Ease.InQuad));
        instance.sequence.InsertCallback(duration, () => instance.center.enabled = false);

        for (int i = 0; i < instance.rays.Length; i++)
            instance.rays[i].enabled = false;
    }

    private void BuildDirectionalBurst(EffectInstance instance, Vector3 impactDirection, Color color,
        float size, float rayLength, float duration)
    {
        float angleOffset = GetScreenAngle(impactDirection);
        instance.center.enabled = true;
        instance.center.sprite = _pixelSprite;
        instance.center.color = color;
        instance.center.transform.localPosition = Vector3.zero;
        instance.center.transform.localRotation = Quaternion.Euler(0f, 0f, angleOffset + 45f);
        instance.center.transform.localScale = new Vector3(size * 0.45f, size * 0.7f, 1f);

        const int rayCount = 4;
        for (int i = 0; i < instance.rays.Length; i++)
        {
            var ray = instance.rays[i];
            ray.enabled = i < rayCount;
            if (!ray.enabled)
                continue;

            float angle = angleOffset + i * 90f;
            Vector3 direction = Quaternion.Euler(0f, 0f, angle) * Vector3.up;
            ray.sprite = _pixelSprite;
            ray.color = color;
            ray.transform.localRotation = Quaternion.Euler(0f, 0f, -angle);
            ray.transform.localPosition = direction * (size * 0.35f);
            ray.transform.localScale = new Vector3(size * 0.14f, rayLength * 0.3f, 1f);
        }

        instance.sequence.Insert(0f, instance.center.transform.DOScale(
            new Vector3(size * 0.65f, size * 1.25f, 1f), duration * 0.3f).SetEase(Ease.OutQuad));
        instance.sequence.Insert(duration * 0.3f,
            instance.center.transform.DOScale(Vector3.zero, duration * 0.7f).SetEase(Ease.InQuad));

        for (int i = 0; i < rayCount; i++)
        {
            float angle = angleOffset + i * 90f;
            Vector3 direction = Quaternion.Euler(0f, 0f, angle) * Vector3.up;
            var ray = instance.rays[i];
            instance.sequence.Insert(0f,
                ray.transform.DOLocalMove(direction * rayLength, duration).SetEase(Ease.OutCubic));
            instance.sequence.Insert(0f,
                ray.transform.DOScaleY(rayLength, duration * 0.3f).SetEase(Ease.OutQuad));
            instance.sequence.Insert(duration * 0.3f,
                ray.transform.DOScaleY(0f, duration * 0.7f).SetEase(Ease.InQuad));
        }
    }

    private void BuildLaunchEffect(EffectInstance instance, Vector3 impactDirection, bool heavy)
    {
        int variant = _slashVariantIndex++ % SlashVariantCount;
        int frameBase = variant * SlashFramesPerVariant;
        int seed = unchecked(instance.root.GetInstanceID() * 719 ^ Time.frameCount * 53);
        var random = new System.Random(seed);
        float Next(float min, float max) => min + (float)random.NextDouble() * (max - min);

        Vector3 localDirection = instance.root.transform.InverseTransformDirection(impactDirection);
        localDirection.z = 0f;
        if (localDirection.sqrMagnitude < 0.0001f)
            localDirection = Vector3.up;
        localDirection.Normalize();

        float angle = Mathf.Atan2(localDirection.y, localDirection.x) * Mathf.Rad2Deg + Next(-8f, 8f);
        float scale = 4.8f * (heavy ? 1.08f : 1f)
            * LaunchImpactScale * Next(0.96f, 1.04f);
        float duration = heavy ? 0.30f : 0.25f;
        float contactEnd = duration * 0.16f;
        float burstEnd = duration * 0.58f;
        float holdEnd = duration * 0.74f;

        instance.visualRoot.localRotation = Quaternion.identity;
        instance.center.enabled = true;
        instance.center.sprite = _slashFrames[frameBase];
        instance.center.flipX = false;
        instance.center.color = Color.white;
        instance.center.transform.localPosition = Vector3.zero;
        instance.center.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        instance.center.transform.localScale = Vector3.one * (scale * 0.22f);

        instance.sequence.Insert(0f, instance.center.transform.DOScale(Vector3.one * (scale * 0.58f), contactEnd)
            .SetEase(Ease.OutCubic));
        instance.sequence.InsertCallback(contactEnd, () =>
        {
            instance.center.sprite = _slashFrames[frameBase + 1];
            instance.center.transform.localScale = Vector3.one * (scale * 0.66f);
        });
        instance.sequence.Insert(contactEnd, instance.center.transform.DOScale(Vector3.one * scale, burstEnd - contactEnd)
            .SetEase(Ease.OutBack, 1.2f));
        instance.sequence.InsertCallback(burstEnd, () =>
        {
            instance.center.sprite = _slashFrames[frameBase + 2];
            instance.center.transform.localScale = Vector3.one * scale;
        });
        instance.sequence.Insert(0f, instance.visualRoot.DOLocalRotate(new Vector3(0f, 0f, Next(-32f, 32f)), duration)
            .SetEase(Ease.OutCubic));
        instance.sequence.Insert(holdEnd, instance.center.transform.DOScale(Vector3.one * (scale * 0.82f), duration - holdEnd)
            .SetEase(Ease.InQuad));
        instance.sequence.InsertCallback(duration, () => instance.center.enabled = false);

        // Launch 仅保留放大的 Slash 像素爆裂主体，避免拉伸射线形成连续“面条”感。
        for (int i = 0; i < instance.rays.Length; i++)
            instance.rays[i].enabled = false;
    }

    private void BuildSlashEffect(EffectInstance instance, Vector3 impactDirection, bool heavy, bool fullSlash)
    {
        int variant = _slashVariantIndex++ % SlashVariantCount;
        int frameBase = variant * SlashFramesPerVariant;
        int seed = unchecked(instance.root.GetInstanceID() * 613 ^ Time.frameCount * 47);
        var random = new System.Random(seed);
        float Next(float min, float max) => min + (float)random.NextDouble() * (max - min);

        Vector3 localDirection = instance.root.transform.InverseTransformDirection(impactDirection);
        localDirection.z = 0f;
        if (localDirection.sqrMagnitude < 0.0001f)
            localDirection = Vector3.right;
        localDirection.Normalize();

        bool mirror = localDirection.x < 0f;
        float tilt = Mathf.Atan2(localDirection.y, Mathf.Max(Mathf.Abs(localDirection.x), 0.0001f)) * Mathf.Rad2Deg;
        float angle = tilt + Next(-3f, 3f);
        float scale = 3f * (heavy ? 1.2f : 1f) * (fullSlash ? 1f : 0.74f) * Next(0.97f, 1.03f);
        float duration = (heavy ? 0.22f : 0.18f) * (fullSlash ? 1f : 0.82f);
        float contactEnd = duration * 0.18f;
        float burstEnd = duration * 0.58f;
        float holdEnd = duration * 0.76f;

        instance.center.enabled = true;
        instance.center.sprite = _slashFrames[frameBase];
        instance.center.flipX = mirror;
        instance.center.color = Color.white;
        instance.center.transform.localPosition = Vector3.zero;
        instance.center.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        instance.center.transform.localScale = Vector3.one * (scale * 0.28f);

        instance.sequence.Insert(0f, instance.center.transform.DOScale(Vector3.one * (scale * 0.56f), contactEnd)
            .SetEase(Ease.OutCubic));
        instance.sequence.InsertCallback(contactEnd, () =>
        {
            instance.center.sprite = _slashFrames[frameBase + 1];
            instance.center.transform.localScale = Vector3.one * (scale * 0.64f);
        });
        instance.sequence.Insert(contactEnd, instance.center.transform.DOScale(Vector3.one * scale, burstEnd - contactEnd)
            .SetEase(Ease.OutBack, 1.1f));
        instance.sequence.InsertCallback(burstEnd, () =>
        {
            instance.center.sprite = _slashFrames[frameBase + 2];
            instance.center.transform.localScale = Vector3.one * scale;
        });
        instance.sequence.Insert(holdEnd, instance.center.transform.DOScale(Vector3.one * (scale * 0.9f), duration - holdEnd)
            .SetEase(Ease.InQuad));
        instance.sequence.InsertCallback(duration, () => instance.center.enabled = false);

        int debrisCount = fullSlash ? (heavy ? 6 : 5) : 3;
        float mainAngle = angle * Mathf.Deg2Rad;
        Vector3 axis = new Vector3(Mathf.Cos(mainAngle), Mathf.Sin(mainAngle), 0f);
        Vector3 perpendicular = new Vector3(-axis.y, axis.x, 0f);
        for (int i = 0; i < instance.rays.Length; i++)
        {
            SpriteRenderer debris = instance.rays[i];
            debris.enabled = i < debrisCount;
            if (!debris.enabled)
                continue;

            float sign = i % 2 == 0 ? 1f : -1f;
            Vector3 direction = i < 2 ? axis * sign : (axis * sign * 0.7f + perpendicular * (i % 3 == 0 ? 0.72f : -0.62f)).normalized;
            float startRadius = scale * Next(0.55f, 0.78f);
            float travel = scale * Next(0.25f, 0.5f);
            float length = scale * Next(0.09f, 0.16f);
            float width = length * Next(0.35f, 0.58f);
            float delay = burstEnd * Next(0.72f, 0.9f);

            debris.sprite = i % 3 == 0 ? _sparkLongSprite : _sparkChipSprite;
            debris.color = i % 2 == 0 ? new Color(0.72f, 0.1f, 0.02f, 1f) : new Color(0.3f, 0.055f, 0.018f, 1f);
            debris.flipX = Next(0f, 1f) > 0.5f;
            debris.transform.localPosition = direction * startRadius;
            debris.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + Next(-18f, 18f));
            debris.transform.localScale = Vector3.zero;

            instance.sequence.Insert(delay, debris.transform.DOScale(new Vector3(length, width, 1f), duration * 0.14f).SetEase(Ease.OutCubic));
            instance.sequence.Insert(delay, debris.transform.DOLocalMove(direction * (startRadius + travel), duration - delay).SetEase(Ease.OutCubic));
            instance.sequence.Insert(holdEnd, debris.transform.DOScale(Vector3.zero, duration - holdEnd).SetEase(Ease.InQuad));
        }
    }
    private float GetScreenAngle(Vector3 worldDirection)
    {
        if (worldDirection.sqrMagnitude < 0.0001f || Camera.main == null)
            return 0f;

        Vector3 screenDirection = Camera.main.transform.InverseTransformDirection(worldDirection.normalized);
        return Mathf.Atan2(-screenDirection.x, screenDirection.y) * Mathf.Rad2Deg;
    }

    private void ReturnToPool(EffectInstance instance, Sequence owner)
    {
        if (instance.sequence != owner || !instance.root.activeSelf)
            return;

        instance.sequence = null;
        instance.root.SetActive(false);
        instance.root.transform.SetParent(_poolRoot, false);
        _pool.Enqueue(instance);
    }

    private EffectInstance CreateInstance()
    {
        var root = new GameObject("PixelHitEffect");
        root.transform.SetParent(_poolRoot, false);

        var centerObject = new GameObject("Center");
        centerObject.transform.SetParent(root.transform, false);
        var visualRootObject = new GameObject("VisualRoot");
        visualRootObject.transform.SetParent(root.transform, false);
        var visualRoot = visualRootObject.transform;
        centerObject.transform.SetParent(visualRoot, false);
        var center = centerObject.AddComponent<SpriteRenderer>();
        ConfigureRenderer(center);

        var rays = new SpriteRenderer[12];
        for (int i = 0; i < rays.Length; i++)
        {
            var rayObject = new GameObject($"Ray_{i}");
            rayObject.transform.SetParent(visualRoot, false);
            rays[i] = rayObject.AddComponent<SpriteRenderer>();
            ConfigureRenderer(rays[i]);
        }

        root.SetActive(false);
        return new EffectInstance { root = root, visualRoot = visualRoot, center = center, rays = rays };
    }

    private void ConfigureRenderer(SpriteRenderer renderer)
    {
        renderer.sprite = _pixelSprite;
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = 1;
    }


    private Color GetDamageTypeColor(DamageType damageType)
    {
        return damageType switch
        {
            DamageType.Stab => stabColor,
            DamageType.Slash => slashColor,
            DamageType.Pierce => pierceColor,
            _ => Color.white
        };
    }
}
