using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public sealed class PixelHitEffectManager : MonoBehaviour
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

        instance.sequence = DOTween.Sequence().SetTarget(instance.root).SetUpdate(UpdateType.Normal, true);
        if (context.isDiseaseStabHit && context.damageType == DamageType.Stab)
        {
            BuildDiseaseStabEffect(instance, heavy);
        }
        else if (context.damageType == DamageType.Slash)
        {
            bool fullSlash = context.strength >= HitFeedbackStrength.Standard;
            BuildSlashEffect(instance, context.impactDirection, heavy, fullSlash);
        }
        else if (context.damageType == DamageType.Stab)
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
        float peakScale = 2.4f * (heavy ? 1.536f : 1.344f) * Next(0.97f, 1.03f);
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

    private void CreatePixelSprite()
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = "PixelHitEffectTexture"
        };
        texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        texture.Apply(false, true);
        _pixelSprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.one * 0.5f, 2f);
        _pixelSprite.name = "PixelHitEffectSprite";
    }

    private void CreateSparkSprites()
    {
        _sparkLongSprite = CreatePatternSprite("Long", new[]
        {
            "00100",
            "00110",
            "01100",
            "01000",
            "00000"
        });
        _sparkForkSprite = CreatePatternSprite("Fork", new[]
        {
            "00100",
            "01110",
            "00100",
            "01010",
            "00000"
        });
        _sparkChipSprite = CreatePatternSprite("Chip", new[]
        {
            "0110",
            "1100",
            "0100",
            "0000"
        });
    }

    private Sprite CreatePatternSprite(string name, string[] rows)
    {
        int height = rows.Length;
        int width = rows[0].Length;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = $"PixelHitEffect_{name}_Texture"
        };
        var pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            string row = rows[height - 1 - y];
            for (int x = 0; x < width; x++)
                pixels[y * width + x] = row[x] == '1' ? Color.white : Color.clear;
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        var sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f), 2f);
        sprite.name = $"PixelHitEffect_{name}";
        return sprite;
    }
    private void CreateSlashSprites()
    {
        _slashFrames = new Sprite[SlashVariantCount * SlashFramesPerVariant];
        for (int variant = 0; variant < SlashVariantCount; variant++)
        {
            _slashFrames[variant * SlashFramesPerVariant] = CreateSlashFrameSprite(variant, 0);
            _slashFrames[variant * SlashFramesPerVariant + 1] = CreateSlashFrameSprite(variant, 1);
            _slashFrames[variant * SlashFramesPerVariant + 2] = CreateSlashFrameSprite(variant, 2);
        }
    }

    private Sprite CreateSlashFrameSprite(int variant, int frame)
    {
        const int size = 80;
        const float pixelsPerUnit = 30f;
        int center = size / 2;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = $"PixelHitEffect_Slash_{variant}_{frame}_Texture"
        };
        var pixels = new Color32[size * size];
        var random = new System.Random(49157 + variant * 6311);
        Color32 outline = new Color32(53, 16, 8, 255);
        Color32 darkRed = new Color32(145, 24, 8, 255);
        Color32 red = new Color32(213, 42, 8, 255);
        Color32 orange = new Color32(255, 101, 7, 255);
        Color32 yellow = new Color32(255, 211, 16, 255);
        Color32 white = new Color32(255, 253, 224, 255);
        float mainAngle = 34f + (float)(random.NextDouble() * 2f - 1f) * 3f;
        float[] mainDirections = { mainAngle, mainAngle + 180f };
        float[] mainLengths = { 1.02f + (float)random.NextDouble() * 0.08f, 0.92f + (float)random.NextDouble() * 0.12f };
        float[] shortDirections =
        {
            mainAngle + 66f + (float)(random.NextDouble() * 2f - 1f) * 9f,
            mainAngle + 104f + (float)(random.NextDouble() * 2f - 1f) * 10f,
            mainAngle + 238f + (float)(random.NextDouble() * 2f - 1f) * 9f,
            mainAngle + 286f + (float)(random.NextDouble() * 2f - 1f) * 10f,
            mainAngle + 150f + (float)(random.NextDouble() * 2f - 1f) * 8f
        };
        float[] shortLengths = { 0.45f, 0.32f, 0.38f, 0.25f, 0.3f };
        float expansion = frame == 0 ? 0.35f : frame == 1 ? 0.82f : 1f;
        float coreRadius = frame == 0 ? 3.1f : 5.8f * expansion;
        DrawSolidBurstCore(pixels, size, center, coreRadius + 1.6f, outline);
        DrawSolidBurstCore(pixels, size, center, coreRadius, frame == 0 ? yellow : orange);
        DrawSolidBurstCore(pixels, size, center, coreRadius * 0.72f, white);

        float[] allDirections = new float[7];
        float[] allLengths = new float[7];
        allDirections[0] = mainDirections[0];
        allDirections[1] = mainDirections[1];
        allLengths[0] = mainLengths[0];
        allLengths[1] = mainLengths[1];
        for (int i = 0; i < shortDirections.Length; i++)
        {
            allDirections[i + 2] = shortDirections[i];
            allLengths[i + 2] = shortLengths[i] * (0.94f + (float)random.NextDouble() * 0.12f);
        }

        float outer = 27f * expansion;
        DrawBurstLayer(pixels, size, center, allDirections, allLengths, 5f * expansion, outer, 3.7f, outline);
        DrawBurstLayer(pixels, size, center, allDirections, allLengths, 4.4f * expansion, outer * 0.91f, 3.1f, darkRed);
        DrawBurstLayer(pixels, size, center, allDirections, allLengths, 3.8f * expansion, outer * 0.82f, 2.7f, red);
        DrawBurstLayer(pixels, size, center, allDirections, allLengths, 3.1f * expansion, outer * 0.7f, 2.2f, orange);
        DrawBurstLayer(pixels, size, center, allDirections, allLengths, 2.5f * expansion, outer * 0.55f, 1.7f, yellow);
        DrawBurstLayer(pixels, size, center, mainDirections, new[] { mainLengths[0], mainLengths[1] },
            2.1f * expansion, outer * 0.48f, 1.25f, white);

        if (frame == 2)
            DrawSlashDebris(pixels, size, center, mainAngle, outline, darkRed, random);

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        _generatedTextures.Add(texture);
        var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * 0.5f, pixelsPerUnit);
        sprite.name = $"PixelHitEffect_Slash_{variant}_{frame}";
        return sprite;
    }

    private static void DrawSlashDebris(Color32[] pixels, int size, int center, float mainAngle,
        Color32 outline, Color32 darkRed, System.Random random)
    {
        for (int i = 0; i < 5; i++)
        {
            float directionAngle = mainAngle + (i % 2 == 0 ? 0f : 180f) + (float)(random.NextDouble() * 2f - 1f) * 22f;
            float radians = directionAngle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            Vector2 position = Vector2.one * center + direction * (29f + (float)random.NextDouble() * 5f);
            int length = 3 + random.Next(0, 4);
            int width = 1 + random.Next(0, 2);
            Color32 color = i % 2 == 0 ? outline : darkRed;
            for (int step = 0; step < length; step++)
            {
                Vector2 p = position + direction * step;
                for (int side = -width; side <= width; side++)
                    SetPixel(pixels, size, Mathf.RoundToInt(p.x + perpendicular.x * side), Mathf.RoundToInt(p.y + perpendicular.y * side), color);
            }
        }
    }

    private void CreateDiseaseStabSprites()
    {
        _diseaseStabFrames = new Sprite[DiseaseStabVariantCount * DiseaseStabFramesPerVariant];
        for (int variant = 0; variant < DiseaseStabVariantCount; variant++)
        {
            for (int frame = 0; frame < DiseaseStabFramesPerVariant; frame++)
                _diseaseStabFrames[variant * DiseaseStabFramesPerVariant + frame] = CreateDiseaseStabFrameSprite(variant, frame);
        }
    }

    private Sprite CreateDiseaseStabFrameSprite(int variant, int frame)
    {
        const int size = 80;
        const float pixelsPerUnit = 30f;
        int center = size / 2;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = $"PixelHitEffect_DiseaseStab_{variant}_{frame}_Texture"
        };
        var pixels = new Color32[size * size];
        var random = new System.Random(31871 + variant * 5779);
        Color32 outline = new Color32(28, 8, 49, 255);
        Color32 darkPurple = new Color32(67, 16, 112, 255);
        Color32 purple = new Color32(126, 37, 190, 255);
        Color32 brightPurple = new Color32(190, 75, 241, 255);
        Color32 palePurple = new Color32(231, 190, 255, 255);
        Color32 white = new Color32(255, 253, 240, 255);

        Vector2[] lobeOffsets;
        float[] lobeSizes;
        if (frame == 0)
        {
            lobeOffsets = new[]
            {
                new Vector2(-6f, 2f), new Vector2(-1f, 7f), new Vector2(7f, 4f),
                new Vector2(4f, -5f), new Vector2(-5f, -6f), new Vector2(1f, -1f)
            };
            lobeSizes = new[] { 8f, 6.5f, 9f, 5.5f, 7f, 4.5f };
        }
        else if (frame == 1)
        {
            lobeOffsets = new[]
            {
                new Vector2(-13f, 6f), new Vector2(-6f, 14f), new Vector2(6f, 12f),
                new Vector2(15f, 4f), new Vector2(11f, -7f), new Vector2(2f, -15f),
                new Vector2(-9f, -12f), new Vector2(-17f, -2f), new Vector2(1f, 2f),
                new Vector2(-3f, 7f), new Vector2(8f, -2f)
            };
            lobeSizes = new[] { 8.5f, 7f, 10.5f, 7f, 9f, 6.5f, 8f, 6.5f, 6f, 5f, 4.5f };
        }
        else
        {
            lobeOffsets = new[]
            {
                new Vector2(-12f, 4f), new Vector2(-5f, 12f), new Vector2(8f, 9f),
                new Vector2(14f, -1f), new Vector2(5f, -11f), new Vector2(-7f, -13f),
                new Vector2(-16f, -4f), new Vector2(-1f, 1f)
            };
            lobeSizes = new[] { 6.5f, 7f, 7.5f, 6f, 7f, 5.5f, 6f, 4f };
        }

        float scale = frame == 0 ? 0.76f : frame == 1 ? 1f : 0.84f;
        for (int i = 0; i < lobeOffsets.Length; i++)
        {
            float sizeFactor = lobeSizes[i] * scale * (0.86f + (float)random.NextDouble() * 0.22f);
            Vector2 offset = lobeOffsets[i] * scale;
            bool largeLobe = lobeSizes[i] >= 7f;
            if (largeLobe)
                DrawDiseaseBlob(pixels, size, center, offset, sizeFactor + 0.9f, sizeFactor * 0.82f + 0.9f, 0.28f, purple, 17 + variant * 31 + i);
            DrawDiseaseBlob(pixels, size, center, offset + new Vector2(1f, 1f), sizeFactor * 0.68f, sizeFactor * 0.54f, 0.3f, brightPurple, 117 + variant * 31 + i);
        }

        int smallBlobCount = frame == 1 ? 18 : 11;
        for (int i = 0; i < smallBlobCount; i++)
        {
            float angle = (i * 151f + variant * 41f) * Mathf.Deg2Rad;
            float distance = (frame == 1 ? 11f : 9f) + (float)random.NextDouble() * 13f;
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance * scale;
            float blobSize = (1.8f + (float)random.NextDouble() * 3.4f) * scale;
            DrawDiseaseBlob(pixels, size, center, offset, blobSize, blobSize * 0.68f, 0.34f,
                random.Next(2) == 0 ? purple : brightPurple, 217 + variant * 17 + i);
        }

        float coreScale = frame == 0 ? 0.66f : frame == 1 ? 1.1f : 0.86f;
        DrawDiseasePixelCloud(pixels, size, center, new Vector2(0f, 0f), 16f * coreScale, 13f * coreScale, brightPurple, 401 + variant);
        DrawDiseasePixelCloud(pixels, size, center, new Vector2(-1f, 1f), 14.8f * coreScale, 12f * coreScale, palePurple, 421 + variant);
        DrawDiseasePixelCloud(pixels, size, center, new Vector2(-1f, 1f), 12.8f * coreScale, 10.4f * coreScale, white, 441 + variant);
        DrawDiseaseGrain(pixels, size, center, lobeOffsets, lobeSizes, scale, frame, variant, darkPurple, purple, brightPurple, palePurple);

        if (frame == 2)
        {
            DrawPixelDisc(pixels, size, center - 19, center + 14, 2.5f, darkPurple);
            DrawPixelDisc(pixels, size, center + 19, center + 8, 2f, purple);
            DrawPixelDisc(pixels, size, center + 13, center - 18, 2.5f, darkPurple);
            DrawPixelDisc(pixels, size, center - 5, center + 22, 1.5f, brightPurple);
            DrawPixelDisc(pixels, size, center + 22, center - 5, 1.5f, purple);
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        _generatedTextures.Add(texture);
        var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * 0.5f, pixelsPerUnit);
        sprite.name = $"PixelHitEffect_DiseaseStab_{variant}_{frame}";
        return sprite;
    }

    private static void DrawDiseasePixelCloud(Color32[] pixels, int size, int center, Vector2 offset,
        float radiusX, float radiusY, Color32 color, int seed)
    {
        int minX = Mathf.FloorToInt(center + offset.x - radiusX);
        int maxX = Mathf.CeilToInt(center + offset.x + radiusX);
        int minY = Mathf.FloorToInt(center + offset.y - radiusY);
        int maxY = Mathf.CeilToInt(center + offset.y + radiusY);
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float nx = (x - center - offset.x) / Mathf.Max(radiusX, 0.01f);
                float ny = (y - center - offset.y) / Mathf.Max(radiusY, 0.01f);
                float steppedX = Mathf.Round(nx * 4f) / 4f;
                float steppedY = Mathf.Round(ny * 4f) / 4f;
                float grain = Mathf.Sin((x + seed) * 1.37f) * Mathf.Cos((y - seed) * 1.11f) * 0.1f;
                if (steppedX * steppedX + steppedY * steppedY <= 1f + grain)
                    SetPixel(pixels, size, x, y, color);
            }
        }
    }
    private static void DrawDiseaseBlob(Color32[] pixels, int size, int center, Vector2 offset,
        float radiusX, float radiusY, float irregularity, Color32 color, int seed)
    {
        int minX = Mathf.FloorToInt(center + offset.x - radiusX - 2f);
        int maxX = Mathf.CeilToInt(center + offset.x + radiusX + 2f);
        int minY = Mathf.FloorToInt(center + offset.y - radiusY - 2f);
        int maxY = Mathf.CeilToInt(center + offset.y + radiusY + 2f);
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float nx = (x - center - offset.x) / Mathf.Max(radiusX, 0.01f);
                float ny = (y - center - offset.y) / Mathf.Max(radiusY, 0.01f);
                float wave = Mathf.Sin((x + seed) * 0.72f) * Mathf.Cos((y - seed) * 0.61f) * irregularity;
                if (nx * nx + ny * ny <= 1f + wave)
                    SetPixel(pixels, size, x, y, color);
            }
        }
    }

    private static void DrawDiseaseGrain(Color32[] pixels, int size, int center, Vector2[] lobeOffsets,
        float[] lobeSizes, float scale, int frame, int variant, Color32 darkPurple,
        Color32 purple, Color32 brightPurple, Color32 palePurple)
    {
        var random = new System.Random(8123 + variant * 997 + frame * 131);
        int count = frame == 1 ? 44 : 30;
        for (int i = 0; i < count; i++)
        {
            int lobe = random.Next(lobeOffsets.Length);
            Vector2 basePosition = lobeOffsets[lobe] * scale;
            Vector2 jitter = new Vector2(
                ((float)random.NextDouble() * 2f - 1f) * lobeSizes[lobe] * scale * 0.62f,
                ((float)random.NextDouble() * 2f - 1f) * lobeSizes[lobe] * scale * 0.62f);
            int x = Mathf.RoundToInt(center + basePosition.x + jitter.x);
            int y = Mathf.RoundToInt(center + basePosition.y + jitter.y);
            float distance = Vector2.Distance(new Vector2(center, center), new Vector2(x, y));
            if (distance < 7f * scale || distance > 25f * scale)
                continue;
            int grainSize = random.Next(1, frame == 1 ? 4 : 3);
            Color32 color = random.Next(5) switch
            {
                0 => darkPurple,
                1 => purple,
                2 => purple,
                3 => brightPurple,
                _ => palePurple
            };
            DrawPixelBlock(pixels, size, x, y, grainSize, color);
        }
    }

    private static void DrawPixelBlock(Color32[] pixels, int size, int centerX, int centerY,
        int blockSize, Color32 color)
    {
        for (int y = 0; y < blockSize; y++)
        {
            for (int x = 0; x < blockSize; x++)
            {
                if ((x + y) % 3 != 2 || blockSize == 1)
                    SetPixel(pixels, size, centerX + x, centerY + y, color);
            }
        }
    }
    private static void DrawSolidBurstCoreAt(Color32[] pixels, int size, Vector2 center, float radius, Color32 color)
    {
        int limit = Mathf.CeilToInt(radius);
        for (int y = -limit; y <= limit; y++)
        {
            for (int x = -limit; x <= limit; x++)
            {
                if (x * x + y * y <= radius * radius)
                    SetPixel(pixels, size, Mathf.RoundToInt(center.x + x), Mathf.RoundToInt(center.y + y), color);
            }
        }
    }

    private void BuildDiseaseStabEffect(EffectInstance instance, bool heavy)
    {
        int variant = _diseaseStabVariantIndex++ % DiseaseStabVariantCount;
        int frameBase = variant * DiseaseStabFramesPerVariant;
        float duration = heavy ? 0.26f : 0.21f;
        float peakScale = (heavy ? 2.65f : 2.35f) * (0.98f + (variant % 3) * 0.015f);
        float contactEnd = duration * 0.18f;
        float burstEnd = duration * 0.58f;
        float holdEnd = duration * 0.7f;

        instance.center.enabled = true;
        instance.center.sprite = _diseaseStabFrames[frameBase];
        instance.center.flipX = false;
        instance.center.color = Color.white;
        instance.center.transform.localPosition = Vector3.zero;
        instance.center.transform.localRotation = Quaternion.identity;
        instance.center.transform.localScale = Vector3.one * (peakScale * 0.38f);
        instance.sequence.Insert(0f, instance.center.transform.DOScale(Vector3.one * (peakScale * 0.62f), contactEnd).SetEase(Ease.OutCubic));
        instance.sequence.InsertCallback(contactEnd, () =>
        {
            instance.center.sprite = _diseaseStabFrames[frameBase + 1];
            instance.center.transform.localScale = Vector3.one * (peakScale * 0.7f);
        });
        instance.sequence.Insert(contactEnd, instance.center.transform.DOScale(Vector3.one * peakScale, burstEnd - contactEnd).SetEase(Ease.OutBack, 1.08f));
        instance.sequence.InsertCallback(burstEnd, () =>
        {
            instance.center.sprite = _diseaseStabFrames[frameBase + 2];
            instance.center.transform.localScale = Vector3.one * peakScale;
        });
        instance.sequence.Insert(holdEnd, instance.center.transform.DOScale(Vector3.one * (peakScale * 0.82f), duration - holdEnd).SetEase(Ease.InQuad));
        instance.sequence.InsertCallback(duration, () => instance.center.enabled = false);
        for (int i = 0; i < instance.rays.Length; i++)
            instance.rays[i].enabled = false;
    }

    private void CreateStabSprites()
    {
        _stabFrames = new Sprite[StabVariantCount * StabFramesPerVariant];
        for (int variant = 0; variant < StabVariantCount; variant++)
        {
            _stabFrames[variant * StabFramesPerVariant] = CreateStabFrameSprite(variant, 0);
            _stabFrames[variant * StabFramesPerVariant + 1] = CreateStabFrameSprite(variant, 1);
            _stabFrames[variant * StabFramesPerVariant + 2] = CreateStabFrameSprite(variant, 2);
        }
    }

    private Sprite CreateStabFrameSprite(int variant, int frame)
    {
        const int size = 80;
        const float pixelsPerUnit = 30f;
        int center = size / 2;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = $"PixelHitEffect_Stab_{variant}_{frame}_Texture"
        };
        var pixels = new Color32[size * size];
        var random = new System.Random(92821 + variant * 7919);
        Color32 outline = new Color32(53, 16, 8, 255);
        Color32 darkRed = new Color32(145, 24, 8, 255);
        Color32 red = new Color32(213, 42, 8, 255);
        Color32 orange = new Color32(255, 101, 7, 255);
        Color32 yellow = new Color32(255, 211, 16, 255);
        Color32 white = new Color32(255, 253, 224, 255);

        float[] directions =
        {
            12f, 52f, 96f, 151f, 198f, 242f, 292f, 338f
        };
        float[] baseLengths =
        {
            0.82f, 0.48f, 0.3f, 0.68f, 0.78f, 0.34f, 0.5f, 0.28f
        };
        var lengths = new float[directions.Length];
        for (int i = 0; i < directions.Length; i++)
        {
            directions[i] += ((float)random.NextDouble() * 2f - 1f) * 4.5f;
            lengths[i] = baseLengths[i] * (0.94f + (float)random.NextDouble() * 0.12f);
        }

        if (frame == 0)
        {
            DrawSolidBurstCore(pixels, size, center, 3.4f, outline);
            DrawSolidBurstCore(pixels, size, center, 2.2f, white);
            DrawBurstLayer(pixels, size, center, directions, lengths, 2.4f, 12f, 3.2f, outline);
            DrawBurstLayer(pixels, size, center, directions, lengths, 1.8f, 8.2f, 2.4f, white);
        }
        else
        {
            float expansion = frame == 1 ? 0.8f : 1f;
            DrawSolidBurstCore(pixels, size, center, 7.4f * expansion, outline);
            DrawSolidBurstCore(pixels, size, center, 6.2f * expansion, darkRed);
            DrawSolidBurstCore(pixels, size, center, 5.1f * expansion, orange);
            DrawSolidBurstCore(pixels, size, center, 4f * expansion, yellow);
            DrawSolidBurstCore(pixels, size, center, 2.9f * expansion, white);
            DrawBurstLayer(pixels, size, center, directions, lengths,
                6.8f * expansion, 22f * expansion, 3.5f, outline);
            DrawBurstLayer(pixels, size, center, directions, lengths,
                5.9f * expansion, 19.5f * expansion, 2.8f, darkRed);
            DrawBurstLayer(pixels, size, center, directions, lengths,
                5.1f * expansion, 17f * expansion, 2.35f, red);
            DrawBurstLayer(pixels, size, center, directions, lengths,
                4.3f * expansion, 14f * expansion, 1.95f, orange);
            DrawBurstLayer(pixels, size, center, directions, lengths,
                3.6f * expansion, 11.5f * expansion, 1.55f, yellow);
            DrawBurstLayer(pixels, size, center, directions, lengths,
                2.9f * expansion, 8.2f * expansion, 1.15f, white);
        }

        if (frame == 2)
            DrawStabDebris(pixels, size, center, directions, outline, darkRed, random);

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        _generatedTextures.Add(texture);
        var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * 0.5f, pixelsPerUnit);
        sprite.name = $"PixelHitEffect_Stab_{variant}_{frame}";
        return sprite;
    }

    private static void DrawStabDebris(Color32[] pixels, int size, int center, float[] directions,
        Color32 outline, Color32 darkRed, System.Random random)
    {
        for (int i = 0; i < 5; i++)
        {
            int directionIndex = (i * 3 + 1) % directions.Length;
            float angle = (directions[directionIndex] + 10f) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            Vector2 position = Vector2.one * center + direction * (27f + (float)random.NextDouble() * 4f);
            int length = 3 + random.Next(0, 4);
            int width = 1 + random.Next(0, 2);
            Color32 color = i % 2 == 0 ? outline : darkRed;
            for (int step = 0; step < length; step++)
            {
                Vector2 p = position + direction * step;
                for (int side = -width; side <= width; side++)
                    SetPixel(pixels, size, Mathf.RoundToInt(p.x + perpendicular.x * side),
                        Mathf.RoundToInt(p.y + perpendicular.y * side), color);
            }
        }
    }

    private static void DrawSolidBurstCore(Color32[] pixels, int size, int center, float radius, Color32 color)
    {
        int limit = Mathf.CeilToInt(radius);
        for (int y = -limit; y <= limit; y++)
        {
            for (int x = -limit; x <= limit; x++)
            {
                float normalizedX = Mathf.Abs(x) / Mathf.Max(radius, 0.01f);
                float normalizedY = Mathf.Abs(y) / Mathf.Max(radius, 0.01f);
                float diamond = normalizedX + normalizedY;
                float square = Mathf.Max(normalizedX, normalizedY);
                if (diamond <= 1.28f && square <= 1f)
                    SetPixel(pixels, size, center + x, center + y, color);
            }
        }
    }

    private static void DrawDiseaseCloudLayer(Color32[] pixels, int size, int center,
        float[] lobeAngles, float[] lobeRadii, float radius, Color32 color)
    {
        int limit = Mathf.CeilToInt(radius + 1.5f);
        float radiusSq = (radius + 1.5f) * (radius + 1.5f);
        int lobeCount = lobeAngles.Length;
        for (int y = -limit; y <= limit; y++)
        {
            for (int x = -limit; x <= limit; x++)
            {
                float distSq = x * x + y * y;
                if (distSq > radiusSq)
                    continue;
                float dist = Mathf.Sqrt(distSq);
                float angle;
                if (dist < 0.5f)
                {
                    SetPixel(pixels, size, center + x, center + y, color);
                    continue;
                }
                angle = Mathf.Atan2(y, x) * Mathf.Rad2Deg;
                if (angle < 0f) angle += 360f;

                int next = 0;
                for (int i = 0; i < lobeCount; i++)
                {
                    if (lobeAngles[i] >= angle) { next = i; break; }
                }
                int prev = next == 0 ? lobeCount - 1 : next - 1;
                float anglePrev = lobeAngles[prev];
                float angleNext = lobeAngles[next];
                if (next == 0 && angle < lobeAngles[0])
                {
                    prev = lobeCount - 1;
                    anglePrev = lobeAngles[prev] - 360f;
                }
                if (prev == lobeCount - 1 && angle >= lobeAngles[lobeCount - 1])
                {
                    next = 0;
                    angleNext = lobeAngles[0] + 360f;
                }
                float t = (angle - anglePrev) / (angleNext - anglePrev);
                float boundary = Mathf.Lerp(lobeRadii[prev], lobeRadii[next], t) * radius;
                if (dist <= boundary)
                    SetPixel(pixels, size, center + x, center + y, color);
            }
        }
    }

    private static void DrawBurstLayer(Color32[] pixels, int size, int center, float[] directions,
        float[] lengths, float innerRadius, float outerRadius, float width, Color32 color)
    {
        for (int i = 0; i < directions.Length; i++)
        {
            float radians = directions[i] * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            float length = Mathf.Lerp(innerRadius, outerRadius, lengths[i]);
            int steps = Mathf.CeilToInt(length - innerRadius);
            for (int step = 0; step <= steps; step++)
            {
                float distance = innerRadius + step;
                float taper = 1f - Mathf.Clamp01((distance - innerRadius) / Mathf.Max(1f, length - innerRadius));
                float halfWidth = Mathf.Max(0.5f, width * taper);
                int sideLimit = Mathf.CeilToInt(halfWidth);
                for (int side = -sideLimit; side <= sideLimit; side++)
                {
                    if (Mathf.Abs(side) > halfWidth)
                        continue;
                    Vector2 position = Vector2.one * center + direction * distance + perpendicular * side;
                    SetPixel(pixels, size, Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y), color);
                }
            }
        }
    }

    private static void DrawRadialLayer(Color32[] pixels, int size, int center, float[] directions,
        float[] lengths, float maxLength, float baseHalfWidth, Color32 color)
    {
        for (int i = 0; i < directions.Length; i++)
        {
            float radians = directions[i] * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            float length = maxLength * lengths[i];
            float start = i % 2 == 0 ? 4f : 6f;
            int steps = Mathf.CeilToInt(length - start);
            for (int step = 0; step <= steps; step++)
            {
                float distance = start + step;
                float normalized = Mathf.Clamp01(distance / length);
                float halfWidth = Mathf.Max(0.55f, baseHalfWidth * Mathf.Pow(1f - normalized, 0.72f));
                int widthSteps = Mathf.CeilToInt(halfWidth);
                for (int side = -widthSteps; side <= widthSteps; side++)
                {
                    if (Mathf.Abs(side) > halfWidth)
                        continue;
                    Vector2 position = Vector2.one * center + direction * distance + perpendicular * side;
                    SetPixel(pixels, size, Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y), color);
                }
            }
        }
    }

    private static void DrawPixelDisc(Color32[] pixels, int size, int centerX, int centerY,
        float radius, Color32 color)
    {
        int limit = Mathf.CeilToInt(radius);
        float radiusSquared = radius * radius;
        for (int y = -limit; y <= limit; y++)
        {
            for (int x = -limit; x <= limit; x++)
            {
                if (x * x + y * y <= radiusSquared)
                    SetPixel(pixels, size, centerX + x, centerY + y, color);
            }
        }
    }

    private static void SetPixel(Color32[] pixels, int size, int x, int y, Color32 color)
    {
        if (x < 0 || x >= size || y < 0 || y >= size)
            return;
        pixels[y * size + x] = color;
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
