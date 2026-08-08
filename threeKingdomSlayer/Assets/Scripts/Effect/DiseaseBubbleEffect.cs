using UnityEngine;

public sealed class DiseaseBubbleEffect : MonoBehaviour
{
    private sealed class Bubble
    {
        public Transform transform;
        public SpriteRenderer renderer;
        public Vector3 startPosition;
        public float age;
        public float lifetime;
        public float riseDistance;
        public float drift;
        public float baseScale;
        public bool active;
    }

    [SerializeField] private float emissionInterval = 0.32f;
    [SerializeField] private float spawnHalfWidth = 0.9f;
    [SerializeField] private float spawnMinHeight = -0.6f;
    [SerializeField] private float spawnMaxHeight = 0.8f;
    [SerializeField] private float spawnOffsetX = -1.5f;
    [SerializeField] private float spawnOffsetY = 4.0f;
    [SerializeField] private float minLifetime = 0.7f;
    [SerializeField] private float maxLifetime = 0.95f;
    [SerializeField] private float minRiseDistance = 1.5f;
    [SerializeField] private float maxRiseDistance = 2.2f;
    [SerializeField] private float minScale = 1.2f;
    [SerializeField] private float maxScale = 2.0f;

    private const int BubbleCount = 3;
    private readonly Bubble[] _bubbles = new Bubble[BubbleCount];
    private bool _emitting;
    private float _emissionTimer;
    private Sprite[] _bubbleSprites;

    private void Awake()
    {
        _bubbleSprites = CreateBubbleSprites();
        for (int i = 0; i < _bubbles.Length; i++)
        {
            var bubbleObject = new GameObject($"DiseaseBubble_{i}");
            bubbleObject.transform.SetParent(transform, false);
            var renderer = bubbleObject.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = 10;
            renderer.enabled = false;
            _bubbles[i] = new Bubble
            {
                transform = bubbleObject.transform,
                renderer = renderer
            };
        }
    }

    public void StartEmission()
    {
        _emitting = true;
        _emissionTimer = 0f;
    }

    public void StopEmission()
    {
        _emitting = false;
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        if (_emitting)
        {
            _emissionTimer -= deltaTime;
            if (_emissionTimer <= 0f)
            {
                SpawnBubble();
                _emissionTimer += Mathf.Max(0.05f, emissionInterval);
            }
        }

        bool hasActiveBubble = false;
        for (int i = 0; i < _bubbles.Length; i++)
        {
            Bubble bubble = _bubbles[i];
            if (!bubble.active)
                continue;
            hasActiveBubble = true;

            bubble.age += deltaTime;
            float progress = Mathf.Clamp01(bubble.age / bubble.lifetime);
            float easedRise = 1f - (1f - progress) * (1f - progress);
            Vector3 position = bubble.startPosition;
            position.y += bubble.riseDistance * easedRise;
            position.x += Mathf.Sin(progress * Mathf.PI) * bubble.drift;
            bubble.transform.localPosition = position;

            float scaleCurve = progress < 0.18f
                ? Mathf.Lerp(1f, 1.15f, progress / 0.18f)
                : Mathf.Lerp(1.15f, 0.8f, (progress - 0.18f) / 0.82f);
            bubble.transform.localScale = Vector3.one * (bubble.baseScale * scaleCurve);

            Color color = bubble.renderer.color;
            color.a = Mathf.Lerp(0.82f, 0f, Mathf.InverseLerp(0.3f, 1f, progress));
            bubble.renderer.color = color;

            if (Camera.main != null)
                bubble.transform.rotation = Quaternion.LookRotation(-Camera.main.transform.forward, Camera.main.transform.up);

            if (progress >= 1f)
                DisableBubble(bubble);
        }

        if (!_emitting && !hasActiveBubble)
            Destroy(gameObject);
    }

    private void SpawnBubble()
    {
        Bubble bubble = null;
        for (int i = 0; i < _bubbles.Length; i++)
        {
            if (!_bubbles[i].active)
            {
                bubble = _bubbles[i];
                break;
            }
        }
        if (bubble == null)
            return;

        bubble.active = true;
        bubble.age = 0f;
        bubble.lifetime = Random.Range(minLifetime, maxLifetime);
        bubble.riseDistance = Random.Range(minRiseDistance, maxRiseDistance);
        bubble.drift = Random.Range(-0.16f, 0.16f);
        bubble.baseScale = Random.Range(minScale, maxScale);
        bubble.startPosition = new Vector3(
            spawnOffsetX + Random.Range(-spawnHalfWidth, spawnHalfWidth),
            spawnOffsetY + Random.Range(spawnMinHeight, spawnMaxHeight),
            0.5f);
        bubble.transform.localPosition = bubble.startPosition;
        bubble.transform.localScale = Vector3.one * bubble.baseScale;
        bubble.renderer.sprite = _bubbleSprites[Random.Range(0, _bubbleSprites.Length)];
        bubble.renderer.color = new Color(1f, 1f, 1f, 1f);
        bubble.renderer.enabled = true;
    }

    private static void DisableBubble(Bubble bubble)
    {
        bubble.active = false;
        bubble.renderer.enabled = false;
    }

    private void OnDisable()
    {
        _emitting = false;
        _emissionTimer = 0f;
        for (int i = 0; i < _bubbles.Length; i++)
        {
            if (_bubbles[i] != null)
                DisableBubble(_bubbles[i]);
        }
    }

    private void OnDestroy()
    {
        if (_bubbleSprites != null)
        {
            for (int i = 0; i < _bubbleSprites.Length; i++)
            {
                if (_bubbleSprites[i] == null) continue;
                Texture2D texture = _bubbleSprites[i].texture;
                Destroy(_bubbleSprites[i]);
                if (texture != null)
                    Destroy(texture);
            }
        }
    }

    private static Sprite[] CreateBubbleSprites()
    {
        return new[]
        {
            CreateBubbleVariant(0, "DiseaseBubble_0"),
            CreateBubbleVariant(1, "DiseaseBubble_1"),
            CreateBubbleVariant(2, "DiseaseBubble_2"),
            CreateBubbleVariant(3, "DiseaseBubble_3")
        };
    }

    private static Sprite CreateBubbleVariant(int variant, string name)
    {
        const int size = 80;
        const float pixelsPerUnit = 30f;
        int center = size / 2;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = $"{name}_Texture"
        };
        var pixels = new Color32[size * size];

        Color32 outline = new Color32(28, 8, 49, 255);
        Color32 darkPurple = new Color32(67, 16, 112, 255);
        Color32 purple = new Color32(126, 37, 190, 255);
        Color32 interiorPurple = new Color32(126, 37, 190, 245);
        Color32 interiorBrightPurple = new Color32(190, 75, 241, 225);
        Color32 interiorDarkPurple = new Color32(67, 16, 112, 255);
        Color32 brightPurple = new Color32(190, 75, 241, 235);
        Color32 palePurple = new Color32(231, 190, 255, 145);
        Color32 white = new Color32(255, 253, 240, 185);

        float[] radii = { 22f, 27f, 24f, 29f };
        Vector2[] centers =
        {
            new Vector2(-2f, 1f), new Vector2(2f, -1f), new Vector2(-1f, -2f), new Vector2(1f, 2f)
        };
        float radius = radii[variant];
        Vector2 bubbleCenter = centers[variant];
        int seed = 911 + variant * 193;

        DrawBubbleContour(pixels, size, center, bubbleCenter, radius, outline, seed, 0);
        DrawBubbleInterior(pixels, size, center, bubbleCenter, radius, interiorPurple, interiorBrightPurple, interiorDarkPurple, variant);
        DrawBubbleContour(pixels, size, center, bubbleCenter, radius - 3.2f, purple, seed + 17, 1);
        DrawBubbleContour(pixels, size, center, bubbleCenter, radius - 6.2f, brightPurple, seed + 31, 2);
        DrawBubbleShadowArc(pixels, size, center, bubbleCenter, radius, darkPurple, variant);
        DrawBubbleReflection(pixels, size, center, bubbleCenter, radius, palePurple, white, variant);
        DrawBubblePixels(pixels, size, center, bubbleCenter, radius, brightPurple, purple, variant);

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * 0.5f, pixelsPerUnit);
        sprite.name = name;
        return sprite;
    }

    private static void DrawBubbleInterior(Color32[] pixels, int size, int center, Vector2 bubbleCenter,
        float radius, Color32 purple, Color32 brightPurple, Color32 darkPurple, int variant)
    {
        float innerRadius = radius - 5f;
        for (int y = Mathf.FloorToInt(center + bubbleCenter.y - innerRadius); y <= Mathf.CeilToInt(center + bubbleCenter.y + innerRadius); y++)
        {
            for (int x = Mathf.FloorToInt(center + bubbleCenter.x - innerRadius); x <= Mathf.CeilToInt(center + bubbleCenter.x + innerRadius); x++)
            {
                float dx = x - center - bubbleCenter.x;
                float dy = y - center - bubbleCenter.y;
                float normalized = Mathf.Sqrt(dx * dx + dy * dy) / innerRadius;
                if (normalized > 1f)
                    continue;

                // Keep a transparent upper-left window; purple occupies the lower/right body.
                bool lowerBody = dy < -innerRadius * 0.1f || dx > innerRadius * 0.28f;
                bool rimBand = normalized > 0.62f;
                bool stepped = ((x + y + variant * 3) & 2) == 0;
                if (lowerBody && rimBand && stepped)
                    SetPixel(pixels, size, x, y, darkPurple);
                else if ((lowerBody || normalized > 0.78f) && ((x * 3 + y * 5 + variant) % 4 != 0))
                    SetPixel(pixels, size, x, y, purple);
                else if (normalized > 0.55f && ((x + y + variant) % 5 == 0))
                    SetPixel(pixels, size, x, y, brightPurple);
            }
        }
    }

    private static void DrawBubbleContour(Color32[] pixels, int size, int center, Vector2 bubbleCenter,
        float radius, Color32 color, int seed, int band)
    {
        float startAngle = 18f + (seed % 23);
        float endAngle = 350f - (seed % 19);
        int steps = Mathf.CeilToInt(radius * 5.5f);
        for (int i = 0; i <= steps; i++)
        {
            float angle = Mathf.Lerp(startAngle, endAngle, i / (float)steps) * Mathf.Deg2Rad;
            float wobble = 1f + Mathf.Sin(angle * (3.1f + band) + seed) * 0.045f;
            float x = center + bubbleCenter.x + Mathf.Cos(angle) * radius * wobble;
            float y = center + bubbleCenter.y + Mathf.Sin(angle) * radius * wobble;
            int ix = Mathf.RoundToInt(x);
            int iy = Mathf.RoundToInt(y);
            bool gap = ((i + seed) % (band == 0 ? 19 : band == 1 ? 27 : 35)) < (band == 0 ? 3 : 2);
            if (!gap)
            {
                SetPixel(pixels, size, ix, iy, color);
                if (band == 0 && i % 4 == 0)
                    SetPixel(pixels, size, ix + 1, iy, color);
            }
        }
    }

    private static void DrawBubbleShadowArc(Color32[] pixels, int size, int center, Vector2 bubbleCenter,
        float radius, Color32 color, int variant)
    {
        int steps = Mathf.CeilToInt(radius * 2.7f);
        for (int i = 0; i < steps; i++)
        {
            float angle = Mathf.Lerp(205f, 338f, i / (float)(steps - 1)) * Mathf.Deg2Rad;
            float wobble = 1f + Mathf.Sin(angle * 4f + variant * 1.7f) * 0.05f;
            int x = Mathf.RoundToInt(center + bubbleCenter.x + Mathf.Cos(angle) * (radius - 5f) * wobble);
            int y = Mathf.RoundToInt(center + bubbleCenter.y + Mathf.Sin(angle) * (radius - 5f) * wobble);
            SetPixel(pixels, size, x, y, color);
            if (i % 3 == 0)
                SetPixel(pixels, size, x + 1, y, color);
        }
    }

    private static void DrawBubbleReflection(Color32[] pixels, int size, int center, Vector2 bubbleCenter,
        float radius, Color32 palePurple, Color32 white, int variant)
    {
        int startX = Mathf.RoundToInt(center + bubbleCenter.x - radius * 0.42f);
        int startY = Mathf.RoundToInt(center + bubbleCenter.y + radius * 0.5f);
        int[][] pattern =
        {
            new[] { 0, 0, 1, 1, 0 },
            new[] { 0, 1, 1, 0, 0 },
            new[] { 1, 1, 0, 0, 0 },
            new[] { 0, 1, 0, 0, 0 }
        };
        for (int row = 0; row < pattern.Length; row++)
        {
            for (int col = 0; col < pattern[row].Length; col++)
            {
                if (pattern[row][(col + variant) % pattern[row].Length] == 1)
                    SetPixel(pixels, size, startX + col, startY - row, row == 1 ? white : palePurple);
            }
        }
    }

    private static void DrawBubblePixels(Color32[] pixels, int size, int center, Vector2 bubbleCenter,
        float radius, Color32 brightPurple, Color32 purple, int variant)
    {
        int[][] offsets =
        {
            new[] { -7, -2 }, new[] { 6, 4 }, new[] { -3, -8 }, new[] { 9, -5 }, new[] { -10, 5 }
        };
        for (int i = 0; i < offsets.Length; i++)
        {
            int x = center + Mathf.RoundToInt(bubbleCenter.x) + offsets[(i + variant) % offsets.Length][0];
            int y = center + Mathf.RoundToInt(bubbleCenter.y) + offsets[(i + variant) % offsets.Length][1];
            if (i % 2 == 0)
                SetPixel(pixels, size, x, y, brightPurple);
            else
                SetPixel(pixels, size, x, y, purple);
        }
    }

    private static void SetPixel(Color32[] pixels, int size, int x, int y, Color32 color)
    {
        if (x < 0 || x >= size || y < 0 || y >= size)
            return;
        pixels[y * size + x] = color;
    }

    private static float NoiseAt(float x, float y)
    {
        int ix = Mathf.FloorToInt(x * 7.31f);
        int iy = Mathf.FloorToInt(y * 7.31f);
        int hash = (ix * 73856093) ^ (iy * 19349663);
        return ((hash & 0x7fffffff) % 1000) / 1000f * 2f - 1f;
    }
}
