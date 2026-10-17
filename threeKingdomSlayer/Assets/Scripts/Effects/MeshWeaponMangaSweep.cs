using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Draws the accepted manga slash as screen-facing strokes that follow the real
/// shaft. Each time slice is one ribbon running from the tail to the tip, and the
/// surface between consecutive slices is the swept arc: widest and brightest at the
/// tip, narrowing and fading toward the tail.
/// </summary>
[DefaultExecutionOrder(1100)]
[DisallowMultipleComponent]
public sealed class MeshWeaponMangaSweep : MonoBehaviour
{
    private const int MaxHistoryRows = 24;
    // One live front row plus at most 23 committed historical rows.
    private const int MaxVisualSubsteps = 3;
    private const int HistoryCapacity = MaxHistoryRows - 1;

    private struct RootPose
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        public Vector3 TransformPoint(Vector3 local)
        {
            return position + rotation * Vector3.Scale(scale, local);
        }
    }

    [Tooltip("Master switch for opted-in Slash instances; leave this component disabled on the prefab.")]
    [SerializeField] private bool renderMangaSweep = true;
    [Tooltip("Only suppress this Slash instance's old tip trail. Disable to compare both effects together.")]
    [SerializeField] private bool replaceTipTrail = true;
    [Tooltip("Ordered real shaft points: tail, shaft bones, tip. Do not use tassel bones.")]
    [SerializeField] private Transform[] shaftPoints;
    [SerializeField] private float[] shaftSamplePositions =
        { 0f, 0.14f, 0.28f, 0.38f, 0.48f, 0.57f, 0.64f, 0.70f, 0.8f, 0.88f, 0.94f, 1f };
    [SerializeField] private Material sweepMaterial;
    [Tooltip("Keeps enough of the swing to read as one arc, rather than a short tip smear.")]
    [SerializeField, Range(0.04f, 0.4f)] private float historySeconds = 0.28f;
    [SerializeField, Range(0.002f, 0.03f)] private float sampleInterval = 1f / 60f;
    [SerializeField, Min(0.02f)] private float maxSampleGap = 0.1f;
    [SerializeField, Range(0f, 0.3f)] private float emitStart = 0.08f;
    [SerializeField, Range(0f, 0.5f)] private float peakStart = 0.22f;
    [SerializeField, Range(0.5f, 1f)] private float peakEnd = 0.74f;
    [SerializeField, Range(0.6f, 1f)] private float emitEnd = 0.92f;
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.92f;
    [Tooltip("Normalized rest-length samples of the broad main stroke: 0 is the tail, 1 is the tip. One ribbon, not two fixed centres.")]
    [SerializeField] private float[] outerShaftSamples = { 0f, 0.2f, 0.4f, 0.6f, 0.8f, 1f };
    [Tooltip("Stroke half-width at the tip, as a fraction of the resting shaft length.")]
    [SerializeField, Range(0.02f, 0.3f)] private float outerHalfWidth = 0.075f;
    [Tooltip("Stroke width at the tail relative to the tip, so the arc tapers instead of showing two bright ends.")]
    [SerializeField, Range(0.05f, 0.6f)] private float tailWidthScale = 0.18f;
    [Tooltip("Intensity where the shaft meets the tail. One keeps the tip fully bright.")]
    [SerializeField, Range(0f, 1f)] private float tailIntensity = 0.08f;
    [Tooltip("Extra width taper for older slices, applied across the stroke only, never toward the camera.")]
    [SerializeField, Range(0f, 0.5f)] private float tailPinch = 0.18f;
    [SerializeField, Min(0.1f)] private float teleportLengthRatio = 0.8f;
    [SerializeField] private string sortingLayer = "Default";
    [SerializeField] private int sortingOrder = 4;

    private Vector3[] _previousPose;
    private Vector3[] _currentPose;
    private float[] _restCumulative;
    private float[] _ages;
    private float[] _strengths;
    private Vector3[] _history;
    // Allocate only in Begin: the disabled prefab component has no mesh/history buffers.
    private List<Vector3> _vertices;
    private List<Vector2> _uvs;
    private List<Vector2> _brushUvs;
    private List<Vector2> _acrossUvs;
    private List<Color32> _colors;
    private List<int> _visibleTriangles;
    private float[] _sampleProgress;
    private float[] _sampleSpeed;
    private Vector3[] _leadingPose;
    private RootPose _previousRoot;
    private RootPose _currentRoot;
    private float _previousProgress;
    private float _leadingStrength;
    private float _leadingProgress;
    private float _leadingSpeed;
    private float _lastRealtime;
    private bool _hasLeadingRow;
    private bool _hasSampledPose;
    private Mesh _mesh;
    private GameObject _meshObject;
    private MeshRenderer _meshRenderer;
    private MeshWeaponVisual _weaponVisual;
    private Transform _effectRoot;
    private int _shaftCount;
    private int _outerSampleCount;
    private int _verticesPerRow;
    private int _capacity;
    private int _head = -1;
    private int _historyCount;
    private float _progress;
    private float _elapsedSinceSample;
    private float _previousStrength;
    private int _expectedXDirection;
    private bool _running;
    private bool _emitting;
    private bool _locallyPaused;
    private bool _previousPoseValid;
    private Matrix4x4 _lastRenderMatrix;

    public bool IsRunning => _running;
    public bool ReplaceTipTrail => replaceTipTrail;
    public int HistoryRowCount => _historyCount;
    public int VertexCount => _mesh != null ? _mesh.vertexCount : 0;

    public void Begin(Transform effectRoot, bool leftToRight, MeshWeaponVisual weaponVisual)
    {
        if (!renderMangaSweep)
        {
            Clear();
            return;
        }
        if (effectRoot == null || sweepMaterial == null || !ValidateShaft())
        {
            Debug.LogWarning("[MeshWeaponMangaSweep] Missing effect root, material, or shaft references.", this);
            Clear();
            return;
        }

        if (_running) Clear();
        enabled = true;

        _outerSampleCount = outerShaftSamples.Length;
        _verticesPerRow = _outerSampleCount * 2;
        _effectRoot = effectRoot;
        _weaponVisual = weaponVisual;
        _expectedXDirection = leftToRight ? 1 : -1;
        _shaftCount = shaftSamplePositions.Length;
        _capacity = HistoryCapacity;
        _previousPose = new Vector3[_shaftCount];
        _currentPose = new Vector3[_shaftCount];
        _restCumulative = new float[shaftPoints.Length];
        _ages = new float[_capacity];
        _strengths = new float[_capacity];
        _history = new Vector3[_capacity * _shaftCount];
        _sampleProgress = new float[_capacity];
        _sampleSpeed = new float[_capacity];
        _leadingPose = new Vector3[_shaftCount];
        int vertexCapacity = MaxHistoryRows * _verticesPerRow;
        _vertices = new List<Vector3>(vertexCapacity);
        _uvs = new List<Vector2>(vertexCapacity);
        _brushUvs = new List<Vector2>(vertexCapacity);
        _acrossUvs = new List<Vector2>(vertexCapacity);
        _colors = new List<Color32>(vertexCapacity);
        _visibleTriangles = new List<int>((MaxHistoryRows - 1)
            * Mathf.Max(_outerSampleCount - 1, 0) * 6);
        _head = -1;
        _historyCount = 0;
        _elapsedSinceSample = 0f;
        _progress = 0f;
        _previousStrength = 0f;
        _locallyPaused = false;
        _emitting = true;
        _running = true;
        _hasLeadingRow = false;
        _hasSampledPose = false;
        _lastRealtime = Time.realtimeSinceStartup;

        BuildRestDistances();
        _previousRoot = CapturePose(_previousPose);
        _previousPoseValid = true;
        CreateMeshRenderer();
    }

    public void SetProgress(float progress)
    {
        _progress = Mathf.Clamp01(progress);
    }

    public void SetPaused(bool paused)
    {
        if (!_running || _locallyPaused == paused) return;
        _locallyPaused = paused;
        if (!paused) RebaseHistory();
    }

    public void EndEmission()
    {
        _emitting = false;
    }

    public void Clear()
    {
        _running = false;
        if (enabled) enabled = false;
        _emitting = false;
        _historyCount = 0;
        _head = -1;
        _effectRoot = null;
        _weaponVisual = null;
        _previousPoseValid = false;
        DestroyMeshObjects();
        _previousPose = null;
        _currentPose = null;
        _restCumulative = null;
        _ages = null;
        _strengths = null;
        _history = null;
        _leadingPose = null;
        _sampleProgress = null;
        _sampleSpeed = null;
        _visibleTriangles = null;
        _vertices = null;
        _uvs = null;
        _brushUvs = null;
        _acrossUvs = null;
        _colors = null;
        _hasLeadingRow = false;
        _hasSampledPose = false;
    }

    private void LateUpdate()
    {
        if (!_running) return;
        float now = Time.realtimeSinceStartup;
        if (_locallyPaused || Time.deltaTime <= 0f)
        {
            // Freeze geometry/alpha. Only reproject if the parent finished moving this frame.
            if (_effectRoot != null && _effectRoot.worldToLocalMatrix != _lastRenderMatrix)
                RebuildMesh();
            return;
        }

        float gap = now - _lastRealtime;
        _lastRealtime = now;
        if (Time.deltaTime > maxSampleGap || gap > maxSampleGap)
            RebaseHistory();
        SampleFrame(Time.deltaTime);
    }

    private void SampleFrame(float deltaTime)
    {
        _currentRoot = CapturePose(_currentPose);
        float strength = _emitting ? GetEmissionStrength(_progress) : 0f;
        AdvanceAges(deltaTime);
        if (strength > 0f)
        {
            Vector3 previousTip = _previousRoot.TransformPoint(_previousPose[_shaftCount - 1]);
            Vector3 currentTip = _currentRoot.TransformPoint(_currentPose[_shaftCount - 1]);
            Vector3 tipDelta = currentTip - previousTip;
            float speed = tipDelta.magnitude / Mathf.Max(deltaTime, 0.0001f);
            float length = TotalRestLength();
            bool discontinuity = _progress < _previousProgress - 0.001f
                || (_currentRoot.position - _previousRoot.position).magnitude > length * teleportLengthRatio
                || tipDelta.magnitude > length * teleportLengthRatio
                || tipDelta.x * _expectedXDirection < -Mathf.Max(0.02f, length * 0.01f);
            if (discontinuity) ClearHistoryOnly();

            if (!_previousPoseValid || _previousStrength <= 0f || !_hasSampledPose || discontinuity)
            {
                StoreInterpolatedPose(1f, strength, _progress, speed, 0f);
                _elapsedSinceSample = 0f;
            }
            else
            {
                AddInterpolatedSamples(deltaTime, strength, speed);
            }
            for (int i = 0; i < _shaftCount; i++)
                _leadingPose[i] = _currentRoot.TransformPoint(_currentPose[i]);
            _leadingStrength = strength;
            _leadingProgress = _progress;
            _leadingSpeed = speed;
            _hasLeadingRow = _historyCount == 0 || _ages[_head] > 0.00001f;
        }
        else
        {
            if (_hasLeadingRow)
                StoreLeadingPose(deltaTime);
            _hasLeadingRow = false;
            _elapsedSinceSample = 0f;
        }
        CopyPose(_currentPose, _previousPose);
        _previousRoot = _currentRoot;
        _previousProgress = _progress;
        _previousStrength = strength;
        _previousPoseValid = true;
        PruneHistory();
        RebuildMesh();
        if (_historyCount == 0 && !_emitting) enabled = false;
    }

    private bool ValidateShaft()
    {
        if (shaftPoints == null || shaftPoints.Length < 2) return false;
        for (int i = 0; i < shaftPoints.Length; i++)
            if (shaftPoints[i] == null) return false;
        return ValidateSamples(shaftSamplePositions, 2, 12)
            && ValidateSamples(outerShaftSamples, 2, 8);
    }

    private static bool ValidateSamples(float[] samples, int minCount, int maxCount)
    {
        if (samples == null || samples.Length < minCount || samples.Length > maxCount) return false;
        for (int i = 1; i < samples.Length; i++)
            if (samples[i] <= samples[i - 1]) return false;
        return samples[0] >= 0f && samples[samples.Length - 1] <= 1f;
    }

    private void BuildRestDistances()
    {
        _restCumulative[0] = 0f;
        for (int i = 1; i < shaftPoints.Length; i++)
            _restCumulative[i] = _restCumulative[i - 1]
                + Vector3.Distance(shaftPoints[i - 1].position, shaftPoints[i].position);
    }

    private float TotalRestLength()
    {
        return _restCumulative != null && _restCumulative.Length > 0
            ? Mathf.Max(_restCumulative[_restCumulative.Length - 1], 0.001f) : 0.001f;
    }

    private RootPose CapturePose(Vector3[] pose)
    {
        float total = TotalRestLength();
        Matrix4x4 worldToWeapon = transform.worldToLocalMatrix;
        for (int sample = 0; sample < _shaftCount; sample++)
        {
            float distance = shaftSamplePositions[sample] * total;
            int segment = 0;
            while (segment < _restCumulative.Length - 2 && distance > _restCumulative[segment + 1])
                segment++;
            float segmentLength = _restCumulative[segment + 1] - _restCumulative[segment];
            float t = segmentLength > 0.0001f
                ? Mathf.Clamp01((distance - _restCumulative[segment]) / segmentLength) : 0f;
            pose[sample] = worldToWeapon.MultiplyPoint3x4(
                Vector3.Lerp(shaftPoints[segment].position, shaftPoints[segment + 1].position, t));
        }
        return new RootPose { position = transform.position, rotation = transform.rotation, scale = transform.lossyScale };
    }

    private float GetEmissionStrength(float progress)
    {
        if (progress < emitStart || progress > emitEnd) return 0f;
        float rise = Mathf.SmoothStep(0f, 1f,
            Mathf.InverseLerp(emitStart, Mathf.Max(peakStart, emitStart + 0.001f), progress));
        float fall = 1f - Mathf.SmoothStep(0f, 1f,
            Mathf.InverseLerp(Mathf.Min(peakEnd, emitEnd - 0.001f), emitEnd, progress));
        return Mathf.Clamp01(Mathf.Min(rise, fall));
    }

    private void AddInterpolatedSamples(float deltaTime, float strength, float speed)
    {
        float interval = Mathf.Max(sampleInterval, 0.001f);
        float total = _elapsedSinceSample + deltaTime;
        int due = Mathf.FloorToInt((total + 0.000001f) / interval);
        int steps = Mathf.Min(due, MaxVisualSubsteps);
        for (int step = 0; step < steps; step++)
        {
            // Even bounded slices across two observed poses, never replaying the live Tween/bones.
            float timeFromFrameStart = due > MaxVisualSubsteps
                ? deltaTime * (step + 1f) / steps
                : interval * (step + 1) - _elapsedSinceSample;
            float t = Mathf.Clamp01(timeFromFrameStart / deltaTime);
            StoreInterpolatedPose(t, Mathf.Lerp(_previousStrength, strength, t),
                Mathf.Lerp(_previousProgress, _progress, t), speed,
                Mathf.Max(0f, deltaTime - timeFromFrameStart));
        }
        _elapsedSinceSample = due > MaxVisualSubsteps ? 0f : Mathf.Max(0f, total - due * interval);
    }

    private void AdvanceAges(float deltaTime)
    {
        for (int i = 0; i < _historyCount; i++)
        {
            int index = (_head - i + _capacity) % _capacity;
            _ages[index] += deltaTime;
        }
    }

    private void StoreInterpolatedPose(float t, float strength, float progress, float speed, float age)
    {
        var root = new RootPose
        {
            position = Vector3.Lerp(_previousRoot.position, _currentRoot.position, t),
            rotation = Quaternion.Slerp(_previousRoot.rotation, _currentRoot.rotation, t),
            scale = Vector3.Lerp(_previousRoot.scale, _currentRoot.scale, t)
        };
        int offset = BeginHistoryRow(strength, progress, speed, age);
        for (int i = 0; i < _shaftCount; i++)
            _history[offset + i] = root.TransformPoint(Vector3.Lerp(_previousPose[i], _currentPose[i], t));
    }

    private void StoreLeadingPose(float age)
    {
        int offset = BeginHistoryRow(_leadingStrength, _leadingProgress, _leadingSpeed, age);
        for (int i = 0; i < _shaftCount; i++) _history[offset + i] = _leadingPose[i];
    }

    private int BeginHistoryRow(float strength, float progress, float speed, float age)
    {
        _head = (_head + 1) % _capacity;
        _ages[_head] = age;
        _strengths[_head] = Mathf.Clamp01(strength);
        _sampleProgress[_head] = progress;
        _sampleSpeed[_head] = Mathf.Clamp01(speed / Mathf.Max(TotalRestLength() * 5f, 0.01f));
        _historyCount = Mathf.Min(_historyCount + 1, _capacity);
        _hasSampledPose = true;
        return _head * _shaftCount;
    }

    private void PruneHistory()
    {
        while (_historyCount > 0)
        {
            int oldest = (_head - (_historyCount - 1) + _capacity) % _capacity;
            if (_ages[oldest] <= historySeconds) break;
            _historyCount--;
        }
    }

    private void RebaseHistory()
    {
        ClearHistoryOnly();
        if (_previousPose != null)
        {
            _previousRoot = CapturePose(_previousPose);
            _previousPoseValid = true;
        }
        _previousProgress = _progress;
        _previousStrength = 0f;
        _elapsedSinceSample = 0f;
        _lastRealtime = Time.realtimeSinceStartup;
        RebuildMesh();
    }

    private void ClearHistoryOnly()
    {
        _head = -1;
        _historyCount = 0;
        _elapsedSinceSample = 0f;
        _hasLeadingRow = false;
        _hasSampledPose = false;
        if (_mesh != null) _mesh.Clear(false);
    }

    private void CopyPose(Vector3[] source, Vector3[] destination)
    {
        for (int i = 0; i < _shaftCount; i++) destination[i] = source[i];
    }

    private void CreateMeshRenderer()
    {
        _meshObject = new GameObject("MangaSweepMesh");
        _meshObject.transform.SetParent(_effectRoot, false);
        _meshObject.transform.localPosition = Vector3.zero;
        _meshObject.transform.localRotation = Quaternion.identity;
        _meshObject.transform.localScale = Vector3.one;
        _meshRenderer = _meshObject.AddComponent<MeshRenderer>();
        MeshFilter meshFilter = _meshObject.AddComponent<MeshFilter>();
        _mesh = new Mesh { name = "MangaSweep_Runtime" };
        _mesh.MarkDynamic();
        meshFilter.sharedMesh = _mesh;
        _meshRenderer.sharedMaterial = sweepMaterial;
        _meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        _meshRenderer.receiveShadows = false;
        _meshRenderer.lightProbeUsage = LightProbeUsage.Off;
        _meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        _meshRenderer.sortingLayerName = sortingLayer;
        _meshRenderer.sortingOrder = sortingOrder;
        _meshRenderer.enabled = false;
    }

    private void RebuildMesh()
    {
        if (_mesh == null || _effectRoot == null || _meshRenderer == null) return;
        _vertices.Clear();
        _uvs.Clear();
        _colors.Clear();
        _brushUvs.Clear();
        _acrossUvs.Clear();
        _visibleTriangles.Clear();
        Matrix4x4 worldToEffect = _effectRoot.worldToLocalMatrix;
        int rowCount = _historyCount + (_hasLeadingRow ? 1 : 0);
        Camera camera = Camera.main;
        Vector3 viewForward = camera != null ? camera.transform.forward : Vector3.forward;
        float length = TotalRestLength();
        Vector3 swing = Vector3.zero;
        if (rowCount >= 2)
        {
            bool leading = _hasLeadingRow;
            int newestHistory = _head;
            int olderHistory = (_head - (_hasLeadingRow ? 0 : 1) + _capacity) % _capacity;
            swing = SampleShaft(1f, leading, newestHistory) - SampleShaft(1f, false, olderHistory);
        }
        // A fixed screen-right offset turns one ribbon edge-on in this oblique camera.
        // Build the width from the real swing and the view direction so the ribbon faces the camera.
        Vector3 widthAxis = Vector3.Cross(viewForward, swing);
        if (widthAxis.sqrMagnitude < 0.000001f)
            widthAxis = camera != null ? camera.transform.up : Vector3.up;
        widthAxis.Normalize();
        if (Vector3.Dot(widthAxis, Vector3.up) < 0f) widthAxis = -widthAxis;
        for (int row = 0; row < rowCount; row++)
        {
            bool leading = _hasLeadingRow && row == 0;
            int historyRow = row - (_hasLeadingRow ? 1 : 0);
            int historyIndex = (_head - historyRow + _capacity) % _capacity;
            float age01 = leading ? 0f : Mathf.Clamp01(_ages[historyIndex] / Mathf.Max(historySeconds, 0.001f));
            float strength = leading ? _leadingStrength : _strengths[historyIndex];
            float alpha = maxAlpha * strength * GetWeaponAlpha();
            float progress = leading ? _leadingProgress : _sampleProgress[historyIndex];
            float speed = leading ? Mathf.Clamp01(_leadingSpeed / Mathf.Max(TotalRestLength() * 5f, 0.01f)) : _sampleSpeed[historyIndex];
            Color32 color = new Color32(255, 255, 255, (byte)Mathf.Clamp(Mathf.RoundToInt(alpha * 255f), 0, 255));
            // Age only narrows a stroke across the screen. It never slides the stroke toward the tail.
            float widthScale = Mathf.Lerp(1f, tailPinch, age01 * age01);
            float mainHalfWidth = outerHalfWidth * length * widthScale;

            // Main stroke: one ribbon per slice crosses the whole shaft, so consecutive slices
            // join into a continuous swept surface that is widest and brightest at the tip.
            AddStroke(widthAxis, outerShaftSamples, mainHalfWidth, tailWidthScale, 1f, 0f,
                progress, speed, age01, color, leading, historyIndex, worldToEffect);
        }

        int stride = _verticesPerRow;
        int outerQuads = Mathf.Max(_outerSampleCount - 1, 0);
        for (int row = 0; row < rowCount - 1; row++)
        {
            int newer = row * stride;
            int older = newer + stride;
            for (int quad = 0; quad < outerQuads; quad++)
            {
                int v = quad * 2;
                AddTriangle(newer + v, newer + v + 1, older + v);
                AddTriangle(newer + v + 1, older + v + 1, older + v);
            }
        }

        _mesh.Clear(false);
        _mesh.SetVertices(_vertices);
        _mesh.SetUVs(0, _uvs);
        _mesh.SetUVs(1, _brushUvs);
        _mesh.SetUVs(2, _acrossUvs);
        _mesh.SetColors(_colors);
        _mesh.SetTriangles(_visibleTriangles, 0, false);
        _mesh.RecalculateBounds();
        _meshRenderer.enabled = _visibleTriangles.Count > 0;
        _lastRenderMatrix = worldToEffect;
    }

    private float ShaftIntensity(float normalizedShaft)
    {
        float alongShaft = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(normalizedShaft));
        return Mathf.Lerp(tailIntensity, 1f, alongShaft);
    }

    /// <summary>
    /// Adds one quad strip running from the tail to the tip of a single time slice. Width and
    /// alpha are interpolated per shaft sample, so the stroke stays bright at the tip and fades
    /// continuously toward the tail instead of showing two separate bright ends.
    /// </summary>
    private void AddStroke(Vector3 widthAxis, float[] samples, float halfWidth, float tailWidthRatio,
        float intensityRatio, float centerOffset, float progress, float speed, float age01, Color32 color,
        bool leading, int historyIndex, Matrix4x4 worldToEffect)
    {
        for (int i = 0; i < samples.Length; i++)
        {
            float shaft = Mathf.Clamp01(samples[i]);
            // Narrowing happens along the stroke's own width, never toward the camera or the tail.
            float width01 = Mathf.Lerp(tailWidthRatio, 1f, Mathf.SmoothStep(0f, 1f, shaft));
            float intensity = Mathf.Clamp01(ShaftIntensity(shaft) * intensityRatio);
            Vector3 vertex = SampleShaft(shaft, leading, historyIndex) + widthAxis * centerOffset;
            Vector3 offset = widthAxis * (halfWidth * width01);
            Color32 strokeColor = new Color32(color.r, color.g, color.b,
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.a * intensity), 0, 255));
            AddRibbonVertex(vertex - offset, -1f, age01, shaft, progress, speed, strokeColor, worldToEffect);
            AddRibbonVertex(vertex + offset, 1f, age01, shaft, progress, speed, strokeColor, worldToEffect);
        }
    }

    private void AddRibbonVertex(Vector3 world, float across, float age01, float shaft01, float progress,
        float speed, Color32 color, Matrix4x4 worldToEffect)
    {
        _vertices.Add(worldToEffect.MultiplyPoint3x4(world));
        // UV0: age on x, normalized shaft length on y.
        _uvs.Add(new Vector2(age01, shaft01));
        // UV1: action progress and swing speed, used for brush breakup.
        _brushUvs.Add(new Vector2(progress, speed));
        // UV2: signed position across this stroke's own width, -1 inner rim to +1 leading rim.
        _acrossUvs.Add(new Vector2(across, 0f));
        _colors.Add(color);
    }

    private Vector3 SampleShaft(float normalized, bool leading, int historyIndex)
    {
        normalized = Mathf.Clamp01(normalized);
        int upper = 1;
        while (upper < _shaftCount - 1 && shaftSamplePositions[upper] < normalized) upper++;
        int lower = upper - 1;
        float span = shaftSamplePositions[upper] - shaftSamplePositions[lower];
        float t = span > 0.0001f ? (normalized - shaftSamplePositions[lower]) / span : 0f;
        return Vector3.Lerp(PosePoint(lower, leading, historyIndex), PosePoint(upper, leading, historyIndex), t);
    }

    private Vector3 PosePoint(int shaft, bool leading, int historyIndex)
    {
        if (leading) return _leadingPose[shaft];
        return _history[historyIndex * _shaftCount + shaft];
    }

    private void AddTriangle(int a, int b, int c)
    {
        Vector3 edgeA = _vertices[b] - _vertices[a];
        Vector3 edgeB = _vertices[c] - _vertices[a];
        // Skip duplicate samples and zero-area triangles around the moving grip.
        if (Vector3.Cross(edgeA, edgeB).sqrMagnitude <= 0.0000000001f) return;
        _visibleTriangles.Add(a);
        _visibleTriangles.Add(b);
        _visibleTriangles.Add(c);
    }

    private float GetWeaponAlpha()
    {
        return _weaponVisual != null ? _weaponVisual.Alpha : 1f;
    }

    private void DestroyMeshObjects()
    {
        if (_meshRenderer != null) _meshRenderer.enabled = false;
        if (_meshObject != null) _meshObject.SetActive(false);
        if (_mesh != null)
        {
            if (Application.isPlaying) Destroy(_mesh);
            else DestroyImmediate(_mesh);
        }
        if (_meshObject != null)
        {
            if (Application.isPlaying) Destroy(_meshObject);
            else DestroyImmediate(_meshObject);
        }
        _mesh = null;
        _meshObject = null;
        _meshRenderer = null;
    }

    private void OnDisable()
    {
        if (_running) Clear();
    }

    private void OnDestroy()
    {
        Clear();
    }
}
