using System.Collections.Generic;
using UnityEngine;

public sealed class PierceVortexStreamVisual : MonoBehaviour
{
    private sealed class Pulse
    {
        public GameObject root;
        public PierceVortexVisual vortex;
        public float distance;
        public bool active;
    }

    private const float PulseExpansionStartProgress = 0.70f;
    private const float PulseEndScaleMultiplier = 2.25f;

    private readonly List<Pulse> _pulses = new List<Pulse>();
    private Vector3 _start;
    private Vector3 _end;
    private float _distance;
    private float _speed;
    private float _interval;
    private float _scale;
    private float _alpha;
    private int _sortingOrder;
    private int _maxPulseCount;
    private float _spawnTimer;
    private bool _emitting = true;

    public static PierceVortexStreamVisual Create(Transform parent, Vector3 start, Vector3 end,
        float speed, float interval, float scale, float alpha, int sortingOrder, int maxPulseCount)
    {
        var root = new GameObject("Pierce_VortexStream");
        root.transform.SetParent(parent, false);
        var stream = root.AddComponent<PierceVortexStreamVisual>();
        stream.Initialize(start, end, speed, interval, scale, alpha, sortingOrder, maxPulseCount);
        return stream;
    }

    public void StopEmission()
    {
        _emitting = false;
    }

    private void Initialize(Vector3 start, Vector3 end, float speed, float interval,
        float scale, float alpha, int sortingOrder, int maxPulseCount)
    {
        _start = start;
        _end = end;
        _distance = Vector3.Distance(start, end);
        _speed = speed;
        _interval = Mathf.Max(interval, 0.01f);
        _scale = scale;
        _alpha = alpha;
        _sortingOrder = sortingOrder;
        _maxPulseCount = maxPulseCount;
    }

    private void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;
        if (_emitting)
        {
            _spawnTimer -= deltaTime;
            while (_spawnTimer <= 0f)
            {
                SpawnPulse();
                _spawnTimer += _interval;
            }
        }

        bool hasActivePulse = false;
        for (int i = 0; i < _pulses.Count; i++)
        {
            Pulse pulse = _pulses[i];
            if (!pulse.active)
                continue;

            pulse.distance += _speed * deltaTime;
            if (pulse.distance >= _distance)
            {
                pulse.active = false;
                pulse.root.SetActive(false);
                continue;
            }

            hasActivePulse = true;
            float progress = pulse.distance / _distance;
            float expansionProgress = Mathf.InverseLerp(PulseExpansionStartProgress, 1f, progress);
            float expansion = Mathf.SmoothStep(1f, PulseEndScaleMultiplier, expansionProgress);
            pulse.vortex?.SetVisualScaleMultiplier(_scale * expansion);
            pulse.vortex?.SetFade(_alpha * (1f - expansionProgress));
            Vector3 position = Vector3.Lerp(_start, _end, progress);
            Vector3 direction = (_end - position).normalized;
            pulse.root.transform.SetPositionAndRotation(position,
                Quaternion.FromToRotation(Vector3.up, direction));
        }

        if (!_emitting && !hasActivePulse)
            Destroy(gameObject);
    }

    private void SpawnPulse()
    {
        Pulse pulse = null;
        for (int i = 0; i < _pulses.Count; i++)
        {
            if (!_pulses[i].active)
            {
                pulse = _pulses[i];
                break;
            }
        }

        if (pulse == null)
        {
            if (_pulses.Count >= _maxPulseCount)
                return;

            var root = new GameObject("Pierce_VortexStreamPulse");
            root.transform.SetParent(transform, false);
            PierceVortexVisual vortex = PierceVortexVisual.Create(root.transform,
                enableAfterimages: false, visualScaleMultiplier: _scale, useUnscaledTime: true);
            vortex?.SetFade(_alpha);
            SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sortingLayerName = "Default";
                renderers[i].sortingOrder = _sortingOrder + i;
            }

            pulse = new Pulse { root = root, vortex = vortex };
            _pulses.Add(pulse);
        }

        pulse.distance = 0f;
        pulse.vortex?.SetVisualScaleMultiplier(_scale);
        pulse.vortex?.SetFade(_alpha);
        pulse.active = true;
        pulse.root.SetActive(true);
        pulse.root.transform.position = _start;
    }

    private void OnDestroy()
    {
        for (int i = 0; i < _pulses.Count; i++)
        {
            if (_pulses[i].root != null)
                Destroy(_pulses[i].root);
        }
    }
}
