using System.Collections.Generic;
using UnityEngine;

public sealed class PierceVortexAfterimage : MonoBehaviour
{
    private const int MaxActive = 14;
    private static readonly Queue<PierceVortexAfterimage> Active = new Queue<PierceVortexAfterimage>();

    private SpriteRenderer _backRenderer;
    private SpriteRenderer _frontRenderer;
    private float _elapsed;
    private float _rotation;
    private float _forcedFadeElapsed;
    private float _forcedFadeDuration;
    private float _forcedFadeStartAlpha = 1f;
    private bool _forcedFading;
    private PierceVortexVisual _owner;

    public static void Create(Vector3 position, Quaternion projectileRotation, Vector3 scale,
        int sourceFrame, float rotation, PierceVortexVisual owner)
    {
        while (Active.Count >= MaxActive)
        {
            PierceVortexAfterimage oldest = Active.Dequeue();
            if (oldest != null)
                Destroy(oldest.gameObject);
        }

        var root = new GameObject("Pierce_VortexAfterimage");
        root.transform.SetPositionAndRotation(position, projectileRotation);
        root.transform.localScale = scale * 1.0f;
        var afterimage = root.AddComponent<PierceVortexAfterimage>();
        afterimage._owner = owner;
        Active.Enqueue(afterimage);
        afterimage.Initialize(sourceFrame, rotation);
    }

    public bool IsOwnedBy(PierceVortexVisual owner)
    {
        return _owner == owner;
    }

    public void BeginFadeOwned(float duration)
    {
        BeginForcedFade(duration);
    }
    private void BeginForcedFade(float duration)
    {
        if (_forcedFading)
            return;

        _forcedFading = true;
        _forcedFadeElapsed = 0f;
        _forcedFadeDuration = Mathf.Max(duration, 0.001f);
        _forcedFadeStartAlpha = Mathf.Max(_backRenderer != null ? _backRenderer.color.a : 0f,
            _frontRenderer != null ? _frontRenderer.color.a : 0f);
    }

    private void Initialize(int sourceFrame, float rotation)
    {
        _rotation = rotation;
        _backRenderer = CreateLayer("VortexBack", PierceVortexVisual.GetBackFrame(sourceFrame), -2,
            new Color(0.45f, 0.12f, 0.05f, 0.60f));
        _frontRenderer = CreateLayer("VortexFront", PierceVortexVisual.GetFrontFrame(sourceFrame), -1,
            new Color(0.85f, 0.30f, 0.08f, 0.70f));
    }

    private SpriteRenderer CreateLayer(string objectName, Sprite sprite, int sortingOrder, Color color)
    {
        var layer = new GameObject(objectName).transform;
        layer.SetParent(transform, false);
        layer.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        var renderer = layer.gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = sortingOrder;
        renderer.color = color;
        return renderer;
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(_elapsed / 1.80f);
        _rotation += 65f * Time.deltaTime;
        transform.localRotation = Quaternion.Euler(0f, 0f, _rotation) * transform.localRotation;
        transform.localScale *= 1f - Time.deltaTime * 0.45f;

        if (progress > 0.5f)
        {
            _backRenderer.sprite = PierceVortexVisual.GetBackFrame(3);
            _frontRenderer.sprite = PierceVortexVisual.GetFrontFrame(3);
        }

        float alpha = 1f - progress;
        if (_forcedFading)
        {
            _forcedFadeElapsed += Time.deltaTime;
            alpha = _forcedFadeStartAlpha * (1f - Mathf.Clamp01(_forcedFadeElapsed / _forcedFadeDuration));
        }
        _backRenderer.color = new Color(1f, 1f, 1f, alpha * 0.58f);
        _frontRenderer.color = new Color(1f, 1f, 1f, alpha * 0.70f);

        if (progress >= 1f || (_forcedFading && _forcedFadeElapsed >= _forcedFadeDuration))
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (Active.Count == 0)
            return;

        var retained = new Queue<PierceVortexAfterimage>();
        while (Active.Count > 0)
        {
            PierceVortexAfterimage item = Active.Dequeue();
            if (item != null && item != this)
                retained.Enqueue(item);
        }
        while (retained.Count > 0)
            Active.Enqueue(retained.Dequeue());
    }
}
