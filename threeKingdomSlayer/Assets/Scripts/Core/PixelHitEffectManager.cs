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

    [Header("颜色")]
    [SerializeField] private Color stabColor = new Color(1f, 0.85f, 0.28f, 1f);
    [SerializeField] private Color slashColor = new Color(0.45f, 0.82f, 1f, 1f);
    [SerializeField] private Color pierceColor = new Color(0.35f, 1f, 0.68f, 1f);
    [SerializeField] private Color heavyColor = new Color(1f, 0.62f, 0.18f, 1f);

    private readonly Queue<EffectInstance> _pool = new Queue<EffectInstance>();
    private Sprite _pixelSprite;
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
    }

    public void RequestHit(HitFeedbackContext context)
    {
        if (context.strength < HitFeedbackStrength.Standard || context.source == HitFeedbackSource.Dot)
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
        instance.root.transform.position = context.worldPosition + Vector3.up * bodyYOffset;
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

        instance.center.color = color;
        instance.center.transform.localPosition = Vector3.zero;
        instance.center.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        instance.center.transform.localScale = new Vector3(size * 0.6f, size * 0.6f, 1f);

        for (int i = 0; i < instance.rays.Length; i++)
        {
            float angle = i * 90f;
            Vector3 direction = Quaternion.Euler(0f, 0f, angle) * Vector3.up;
            var ray = instance.rays[i];
            ray.color = color;
            ray.transform.localRotation = Quaternion.Euler(0f, 0f, -angle);
            ray.transform.localPosition = direction * (size * 0.45f);
            ray.transform.localScale = new Vector3(size * 0.18f, rayLength * 0.35f, 1f);
        }

        instance.sequence = DOTween.Sequence().SetTarget(instance.root).SetUpdate(UpdateType.Normal, true);
        instance.sequence.Insert(0f, instance.center.transform.DOScale(new Vector3(size, size, 1f), duration * 0.35f).SetEase(Ease.OutQuad));
        instance.sequence.Insert(duration * 0.35f, instance.center.transform.DOScale(Vector3.zero, duration * 0.65f).SetEase(Ease.InQuad));

        for (int i = 0; i < instance.rays.Length; i++)
        {
            float angle = i * 90f;
            Vector3 direction = Quaternion.Euler(0f, 0f, angle) * Vector3.up;
            var ray = instance.rays[i];
            instance.sequence.Insert(0f, ray.transform.DOLocalMove(direction * rayLength, duration).SetEase(Ease.OutCubic));
            instance.sequence.Insert(0f, ray.transform.DOScaleY(rayLength, duration * 0.35f).SetEase(Ease.OutQuad));
            instance.sequence.Insert(duration * 0.35f, ray.transform.DOScaleY(0f, duration * 0.65f).SetEase(Ease.InQuad));
        }

        var sequence = instance.sequence;
        sequence.OnComplete(() => ReturnToPool(instance, sequence));
        sequence.OnKill(() => ReturnToPool(instance, sequence));
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
