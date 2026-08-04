using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public sealed class PixelHitEffectManager : MonoBehaviour
{
    private sealed class EffectInstance
    {
        public GameObject root;
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
    private Sprite _slashFrameStart;
    private Sprite _slashFrameFull;
    private Sprite _slashFrameBreak;
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
        if (_slashFrameStart != null)
            Destroy(_slashFrameStart);
        if (_slashFrameFull != null)
            Destroy(_slashFrameFull);
        if (_slashFrameBreak != null)
            Destroy(_slashFrameBreak);
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
        instance.root.transform.position = context.hasImpactPosition
            ? context.worldPosition
            : context.worldPosition + Vector3.up * bodyYOffset;
        if (Camera.main != null)
            instance.root.transform.position += Camera.main.transform.forward * cameraDepthOffset;
        instance.root.transform.rotation = Camera.main != null
            ? Quaternion.LookRotation(-Camera.main.transform.forward, Camera.main.transform.up)
            : Quaternion.identity;

        bool heavy = context.strength == HitFeedbackStrength.Heavy;
        float size = heavy ? heavySize : standardSize;
        float rayLength = heavy ? heavyRayLength : standardRayLength;
        float duration = heavy ? heavyDuration : standardDuration;
        Color color = heavy ? heavyColor : GetDamageTypeColor(context.damageType);

        instance.sequence = DOTween.Sequence().SetTarget(instance.root).SetUpdate(UpdateType.Normal, true);
        if (context.damageType == DamageType.Slash)
        {
            bool fullSlash = context.strength >= HitFeedbackStrength.Standard;
            BuildSlashEffect(instance, context.impactDirection, slashColor,
                (heavy ? slashLength * 1.2f : slashLength) * (fullSlash ? 1f : 0.72f),
                (heavy ? slashWidth * 1.2f : slashWidth) * (fullSlash ? 1f : 0.82f),
                (heavy ? slashDuration * 1.15f : slashDuration) * (fullSlash ? 1f : 0.78f),
                fullSlash ? 4 : 2);
        }
        else
        {
            BuildDirectionalBurst(instance, context.impactDirection, color, size, rayLength, duration);
        }

        var sequence = instance.sequence;
        sequence.OnComplete(() => ReturnToPool(instance, sequence));
        sequence.OnKill(() => ReturnToPool(instance, sequence));
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

        for (int i = 0; i < instance.rays.Length; i++)
        {
            float angle = angleOffset + i * 90f;
            Vector3 direction = Quaternion.Euler(0f, 0f, angle) * Vector3.up;
            var ray = instance.rays[i];
            ray.enabled = true;
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

        for (int i = 0; i < instance.rays.Length; i++)
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

    private void BuildSlashEffect(EffectInstance instance, Vector3 impactDirection, Color edgeColor,
        float length, float width, float duration, int sparkCount)
    {
        float travelSign = impactDirection.sqrMagnitude < 0.0001f
            ? 1f
            : (instance.root.transform.InverseTransformDirection(impactDirection.normalized).x >= 0f ? 1f : -1f);
        float angle = travelSign > 0f ? slashAngle : -slashAngle;
        Vector3 travel = Quaternion.Euler(0f, 0f, angle) * new Vector3(travelSign, 0f, 0f);
        Vector3 upward = Quaternion.Euler(0f, 0f, angle) * Vector3.up;
        float entryLength = length * 0.15f;
        float frame = duration / 4f;

        instance.center.enabled = true;
        instance.center.color = slashShadowColor;
        instance.center.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        SetSlashSpriteFrame(instance.center, _slashFrameStart, travelSign,
            length * 0.28f, width * 0.7f, -travel * entryLength * 0.45f);
        instance.sequence.AppendInterval(frame);
        instance.sequence.AppendCallback(() =>
        {
            instance.center.color = edgeColor;
            SetSlashSpriteFrame(instance.center, _slashFrameFull, travelSign,
                length * 0.62f, width * 1.2f, -travel * entryLength * 0.5f);
        });
        instance.sequence.AppendInterval(frame * 1.5f);
        instance.sequence.AppendCallback(() =>
        {
            instance.center.color = slashCoreColor;
            SetSlashSpriteFrame(instance.center, _slashFrameBreak, travelSign,
                length * 0.42f, width * 0.85f, -travel * entryLength * 0.15f);
        });
        instance.sequence.AppendInterval(frame * 0.9f);
        instance.sequence.AppendCallback(() => instance.center.enabled = false);

        for (int i = 0; i < instance.rays.Length; i++)
        {
            var spark = instance.rays[i];
            bool visible = i < sparkCount;
            spark.enabled = visible;
            if (!visible)
                continue;

            spark.sprite = i switch
            {
                0 => _sparkLongSprite,
                1 => _sparkForkSprite,
                _ => _sparkChipSprite
            };
            spark.color = i switch
            {
                0 => slashCoreColor,
                1 => edgeColor,
                2 => slashCoreColor,
                _ => slashShadowColor
            };
            float side = i % 2 == 0 ? -1f : 1f;
            float sparkScale = i switch
            {
                0 => length * 0.42f,
                1 => length * 0.34f,
                _ => length * 0.22f
            };
            Vector3 step0 = travel * length * (0.18f + i * 0.025f) + upward * side * width * 0.2f;
            Vector3 step1 = travel * length * (0.42f + (i % 2) * 0.08f) + upward * side * width * 0.8f;
            Vector3 step2 = travel * length * (0.68f + (i % 2) * 0.12f) + upward * side * width * 1.55f;

            spark.transform.localRotation = Quaternion.Euler(0f, 0f, angle + side * (i < 2 ? 10f : 18f));
            spark.transform.localPosition = step0;
            spark.transform.localScale = new Vector3(sparkScale, width * (i < 2 ? 0.38f : 0.26f), 1f);
            instance.sequence.InsertCallback(frame, () => spark.transform.localPosition = step1);
            instance.sequence.InsertCallback(frame * 2f, () => spark.transform.localPosition = step2);
            instance.sequence.InsertCallback(frame * 3f, () =>
                spark.transform.localScale = new Vector3(sparkScale * 0.45f, width * 0.2f, 1f));
        }
    }

    private static void SetSlashSpriteFrame(SpriteRenderer renderer, Sprite sprite,
        float travelSign, float length, float height, Vector3 position)
    {
        renderer.sprite = sprite;
        renderer.transform.localPosition = position;
        Vector3 boundsSize = sprite.bounds.size;
        float scaleX = boundsSize.x > 0f ? length / boundsSize.x : 1f;
        float scaleY = boundsSize.y > 0f ? height / boundsSize.y : 1f;
        renderer.transform.localScale = new Vector3(
            travelSign < 0f ? -scaleX : scaleX,
            scaleY,
            1f);
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
        var center = centerObject.AddComponent<SpriteRenderer>();
        ConfigureRenderer(center);

        var rays = new SpriteRenderer[4];
        for (int i = 0; i < rays.Length; i++)
        {
            var rayObject = new GameObject($"Ray_{i}");
            rayObject.transform.SetParent(root.transform, false);
            rays[i] = rayObject.AddComponent<SpriteRenderer>();
            ConfigureRenderer(rays[i]);
        }

        root.SetActive(false);
        return new EffectInstance { root = root, center = center, rays = rays };
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
        _slashFrameStart = CreatePatternSprite("SlashStart", new[]
        {
            "0000001000",
            "0000011100",
            "0000111000",
            "0000010000",
            "0000000000"
        });
        _slashFrameFull = CreatePatternSprite("SlashFull", new[]
        {
            "0000010000000000",
            "0000111000000000",
            "0001111110000000",
            "0011111111100000",
            "0111111111111000",
            "0011111111100000",
            "0001111110000000",
            "0000111000000000"
        });
        _slashFrameBreak = CreatePatternSprite("SlashBreak", new[]
        {
            "1000000000000000",
            "0110000000000000",
            "0011110000000000",
            "0000111111000000",
            "0000001111111000",
            "0000000011110000",
            "0000000000100000"
        });
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
