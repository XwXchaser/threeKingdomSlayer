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

    [SerializeField] private float emissionInterval = 0.18f;
    [SerializeField] private float spawnHalfWidth = 0.35f;
    [SerializeField] private float spawnMinHeight = 0.2f;
    [SerializeField] private float spawnMaxHeight = 0.85f;
    [SerializeField] private float minLifetime = 0.65f;
    [SerializeField] private float maxLifetime = 0.95f;
    [SerializeField] private float minRiseDistance = 0.55f;
    [SerializeField] private float maxRiseDistance = 0.9f;
    [SerializeField] private float minScale = 0.08f;
    [SerializeField] private float maxScale = 0.15f;

    private const int BubbleCount = 6;
    private readonly Bubble[] _bubbles = new Bubble[BubbleCount];
    private bool _emitting;
    private float _emissionTimer;
    private Sprite _bubbleSprite;

    private void Awake()
    {
        _bubbleSprite = CreateBubbleSprite();
        for (int i = 0; i < _bubbles.Length; i++)
        {
            var bubbleObject = new GameObject($"DiseaseBubble_{i}");
            bubbleObject.transform.SetParent(transform, false);
            var renderer = bubbleObject.AddComponent<SpriteRenderer>();
            renderer.sprite = _bubbleSprite;
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = 1;
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
                ? Mathf.Lerp(0.45f, 1f, progress / 0.18f)
                : Mathf.Lerp(1f, 0.72f, (progress - 0.18f) / 0.82f);
            bubble.transform.localScale = Vector3.one * (bubble.baseScale * scaleCurve);

            Color color = bubble.renderer.color;
            color.a = Mathf.Lerp(0.68f, 0f, Mathf.InverseLerp(0.3f, 1f, progress));
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
            Random.Range(-spawnHalfWidth, spawnHalfWidth),
            Random.Range(spawnMinHeight, spawnMaxHeight),
            -0.05f);
        bubble.transform.localPosition = bubble.startPosition;
        bubble.transform.localScale = Vector3.one * (bubble.baseScale * 0.45f);
        bubble.renderer.color = Random.value < 0.5f
            ? new Color(0.66f, 0.24f, 0.94f, 0.68f)
            : new Color(0.84f, 0.48f, 1f, 0.68f);
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
        if (_bubbleSprite != null)
        {
            Texture2D texture = _bubbleSprite.texture;
            Destroy(_bubbleSprite);
            if (texture != null)
                Destroy(texture);
        }
    }

    private static Sprite CreateBubbleSprite()
    {
        string[] rows =
        {
            "000111000",
            "001000100",
            "010110010",
            "100100001",
            "100000001",
            "100000001",
            "010000010",
            "001000100",
            "000111000"
        };
        const int size = 9;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = "DiseaseBubbleTexture"
        };
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            string row = rows[size - 1 - y];
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = row[x] == '1' ? Color.white : Color.clear;
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * 0.5f, 9f);
        sprite.name = "DiseaseBubbleSprite";
        return sprite;
    }
}
