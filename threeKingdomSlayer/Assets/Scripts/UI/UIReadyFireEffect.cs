using UnityEngine;
using UnityEngine.UI;

public class UIReadyFireEffect : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite startSprite;
    public Sprite[] loopSprites;
    public float frameRate = 10f;

    [Header("Pulse")]
    public float baseAlpha = 0.9f;
    public float pulseAlphaAmplitude = 0.1f;
    public float pulseScaleAmplitude = 0.06f;
    public float pulseSpeed = 5f;

    [Header("Layout")]
    public Vector2 localOffset;
    public float sizeScale = 1f;

    [Header("Jitter")]
    public Vector2 jitterAmplitude = new Vector2(1.5f, 2f);

    public bool playOnAwake;

    private RectTransform _root;
    private Image _image;
    private CanvasGroup _canvasGroup;
    private bool _playing;
    private float _frameTimer;
    private int _frameIndex;
    private Vector2 _baseAnchoredPosition;
    private Vector2 _baseSizeDelta;
    private Vector3 _baseScale;

    private void Awake()
    {
        _root = transform as RectTransform;
        EnsureImage();

        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;

        CacheBaseLayout();

        if (playOnAwake)
            Play();
        else
            SetVisible(false);
    }

    private void OnEnable()
    {
        if (_root == null)
            _root = transform as RectTransform;
        EnsureImage();
        CacheBaseLayout();
    }

    private void OnValidate()
    {
        if (_root == null)
            _root = transform as RectTransform;
        if (_root != null)
            ApplyStaticLayout();
    }

    private void Update()
    {
        if (!_playing)
        {
            SetVisible(false);
            return;
        }

        ApplyStaticLayout();

        float time = Time.unscaledTime;

        if (loopSprites != null && loopSprites.Length > 0)
        {
            _frameTimer += Time.unscaledDeltaTime;
            float interval = 1f / Mathf.Max(frameRate, 0.1f);
            if (_frameTimer >= interval)
            {
                _frameTimer -= interval;
                _frameIndex = (_frameIndex + 1) % loopSprites.Length;
                _image.sprite = loopSprites[_frameIndex];
            }
        }

        float pulse = Mathf.Sin(time * pulseSpeed);
        float alpha = baseAlpha + pulse * pulseAlphaAmplitude;
        float scale = 1f + pulse * pulseScaleAmplitude;

        var c = _image.color;
        c.a = Mathf.Clamp01(alpha);
        _image.color = c;

        _root.localScale = _baseScale * scale;

        float jx = (Mathf.PerlinNoise(time * 7f, 0f) - 0.5f) * 2f * jitterAmplitude.x;
        float jy = (Mathf.PerlinNoise(0f, time * 7f) - 0.5f) * 2f * jitterAmplitude.y;
        _root.anchoredPosition = _baseAnchoredPosition + localOffset + new Vector2(jx, jy);
    }

    public void ApplySprites(Sprite startS, Sprite[] loops, float fps)
    {
        startSprite = startS;
        loopSprites = loops;
        frameRate = fps > 0f ? fps : frameRate;
        if (_image != null && startSprite != null)
            _image.sprite = startSprite;
    }

    public void ApplySprites(Sprite[] sprites)
    {
        loopSprites = sprites;
    }

    public void Play()
    {
        EnsureImage();
        CacheBaseLayout();
        _playing = true;
        _frameTimer = 0f;
        _frameIndex = 0;
        if (_image != null)
        {
            if (startSprite != null)
                _image.sprite = startSprite;
            else if (loopSprites != null && loopSprites.Length > 0)
                _image.sprite = loopSprites[0];
        }
        SetVisible(true);
        ApplyStaticLayout();
    }

    public void Stop(bool hide)
    {
        _playing = false;
        _frameTimer = 0f;
        if (hide)
        {
            SetVisible(false);
            if (_image != null && startSprite != null)
                _image.sprite = startSprite;
        }
        ApplyStaticLayout();
    }

    public void SetVisible(bool visible)
    {
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }
        _canvasGroup.alpha = visible ? 1f : 0f;
    }

    public void ForceVisible(bool visible)
    {
        SetVisible(visible);
    }

    private void EnsureImage()
    {
        if (_root == null)
            _root = transform as RectTransform;
        if (_image == null)
        {
            _image = GetComponent<Image>();
            if (_image == null)
                _image = gameObject.AddComponent<Image>();
            _image.raycastTarget = false;
            _image.maskable = false;
            _image.preserveAspect = true;
        }
    }

    private void CacheBaseLayout()
    {
        if (_root == null)
            return;
        _baseAnchoredPosition = Vector2.zero;
        _baseSizeDelta = _root.sizeDelta;
        _baseScale = Vector3.one;
        ApplyStaticLayout();
    }

    private void ApplyStaticLayout()
    {
        if (_root == null)
            return;
        _root.anchoredPosition = _baseAnchoredPosition + localOffset;
        _root.sizeDelta = _baseSizeDelta * Mathf.Max(0.01f, sizeScale);
        if (!_playing)
            _root.localScale = _baseScale;
    }
}
