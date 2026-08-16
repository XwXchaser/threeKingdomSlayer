using System.Collections.Generic;
using UnityEngine;

public sealed class PierceAimIndicator : MonoBehaviour
{
    public static PierceAimIndicator Instance { get; private set; }
    public float PulseAlpha => pulseAlpha;
    public float PulseSpeed => pulseSpeed;
    public float PulseInterval => pulseInterval;
    public float PulseScale => pulseScale;
    public int MaxPulseCount => maxPulseCount;
    public int PulseSortingOrder => pulseSortingOrder;
    [Header("显示")]
    [SerializeField] private Color indicatorColor = new Color(1f, 0f, 0f, 0.5f);
    [SerializeField] private float yOffset = 0.05f;
    [SerializeField] private int sortingOrder = -2;
    [SerializeField] private int headSortingOrder = 6;

    [Header("Body")]
    [Min(0.05f)]
    [SerializeField] private float bodyBlockLength = 0.65f;
    [Min(0.05f)]
    [SerializeField] private float bodyBlockWidth = 0.5f;
    [Min(0f)]
    [SerializeField] private float bodyBlockGap = 0.15f;

    [Header("Head")]
    [Min(0.05f)]
    [SerializeField] private float headLength = 0.85f;
    [Min(0.05f)]
    [SerializeField] private float headWidth = 1.0f;

    [Header("Pulse")]
    [SerializeField] private bool showPiercePulse = true;
    [Range(0f, 1f)]
    [SerializeField] private float pulseAlpha = 0.22f;
    [Min(0.1f)]
    [SerializeField] private float pulseSpeed = 12f;
    [Min(0f)]
    [SerializeField] private float pulseInterval = 0.25f;
    [Min(0.01f)]
    [SerializeField] private float pulseEndHoldDuration = 0.08f;
    [Min(0.01f)]
    [SerializeField] private float pulseScale = 0.1f;
    [Min(1)]
    [SerializeField] private int maxPulseCount = 12;
    [SerializeField] private int pulseSortingOrder = 4;

    private sealed class Pulse
    {
        public GameObject root;
        public float distance;
        public float endHoldTimer;
        public bool active;
        public bool reachedEnd;
    }

    private readonly List<Vector3> _pathPoints = new List<Vector3>(8);
    private readonly List<Pulse> _pulses = new List<Pulse>();
    private GameObject _visualRoot;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private Mesh _mesh;
    private GameObject _headRoot;
    private MeshFilter _headMeshFilter;
    private MeshRenderer _headMeshRenderer;
    private Mesh _headMesh;
    private Material _material;
    private float _pulseSpawnTimer;
    private float _pathLength;
    private bool _chargeActive;
    private bool _aimReady;
    private int _currentColumn = -1;
    private int _pathSignature;

    private void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        CreateVisual();
        SetVisible(false);

        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnChargeBegan += OnChargeBegan;
            InputManager.Instance.OnChargeUpdated += OnChargeUpdated;
            InputManager.Instance.OnChargeEnded += OnChargeEnded;
        }

        if (PlayerState.Instance != null)
            PlayerState.Instance.OnPlayerDied += OnChargeEnded;
    }

    private void Update()
    {
        if (!_chargeActive || !_aimReady || !showPiercePulse || _pathPoints.Count < 2)
            return;

        float deltaTime = Time.unscaledDeltaTime;
        _pulseSpawnTimer -= deltaTime;
        while (_pulseSpawnTimer <= 0f)
        {
            SpawnPulse();
            _pulseSpawnTimer += Mathf.Max(pulseInterval, 0.01f);
        }

        for (int i = 0; i < _pulses.Count; i++)
        {
            Pulse pulse = _pulses[i];
            if (!pulse.active)
                continue;

            if (pulse.reachedEnd)
            {
                pulse.endHoldTimer -= deltaTime;
                if (pulse.endHoldTimer <= 0f)
                {
                    pulse.active = false;
                    pulse.root.SetActive(false);
                }
                continue;
            }

            pulse.distance += pulseSpeed * deltaTime;
            if (pulse.distance >= _pathLength)
            {
                pulse.distance = _pathLength;
                pulse.reachedEnd = true;
                pulse.endHoldTimer = pulseEndHoldDuration;
            }

            Vector3 position = GetPointAtDistance(_pathPoints, pulse.distance);
            Vector3 ahead = GetPointAtDistance(_pathPoints, Mathf.Min(pulse.distance + 0.1f, _pathLength));
            Vector3 direction = ahead - position;
            if (direction.sqrMagnitude > 0.0001f)
                pulse.root.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
            pulse.root.transform.position = position;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnChargeBegan -= OnChargeBegan;
            InputManager.Instance.OnChargeUpdated -= OnChargeUpdated;
            InputManager.Instance.OnChargeEnded -= OnChargeEnded;
        }

        if (PlayerState.Instance != null)
            PlayerState.Instance.OnPlayerDied -= OnChargeEnded;

        if (_mesh != null)
            Destroy(_mesh);
        if (_headMesh != null)
            Destroy(_headMesh);
        if (_material != null)
            Destroy(_material);
    }

    private void OnDisable()
    {
        SetVisible(false);
    }

    private void OnChargeBegan(Vector2 screenPosition)
    {
        _chargeActive = true;
        _aimReady = false;
        _currentColumn = -1;
        ResetPulse();
        SetVisible(false);
    }

    private void OnChargeUpdated(Vector2 screenPosition, float progress)
    {
        if (!_chargeActive || progress < 1f || InputManager.Instance == null || AttackSystem.Instance == null)
        {
            SetVisible(false);
            return;
        }

        int column = InputManager.Instance.GetPierceColumnFromScreenPosition(screenPosition);
        if (!AttackSystem.Instance.TryGetPierceIndicatorPath(column, _pathPoints, out _))
        {
            SetVisible(false);
            return;
        }

        for (int i = 0; i < _pathPoints.Count; i++)
        {
            Vector3 point = _pathPoints[i];
            point.y = yOffset;
            _pathPoints[i] = point;
        }

        int signature = CalculatePathSignature(_pathPoints);
        if (column != _currentColumn || signature != _pathSignature)
        {
            _currentColumn = column;
            _pathSignature = signature;
            RebuildMesh(_pathPoints);
            PopulatePulses();
        }

        SetVisible(true);
        _aimReady = true;
    }

    private void OnChargeEnded()
    {
        _chargeActive = false;
        _aimReady = false;
        _currentColumn = -1;
        ResetPulse();
        SetVisible(false);
    }

    private void CreateVisual()
    {
        _visualRoot = new GameObject("PierceAimIndicator_Visual");
        _visualRoot.transform.SetParent(transform, false);
        _meshFilter = _visualRoot.AddComponent<MeshFilter>();
        _meshRenderer = _visualRoot.AddComponent<MeshRenderer>();
        _meshRenderer.sortingLayerName = "Default";
        _meshRenderer.sortingOrder = sortingOrder;

        _mesh = new Mesh { name = "PierceAimIndicator_BodyMesh" };
        _mesh.MarkDynamic();
        _meshFilter.sharedMesh = _mesh;

        _headRoot = new GameObject("PierceAimIndicator_Head");
        _headRoot.transform.SetParent(_visualRoot.transform, false);
        _headMeshFilter = _headRoot.AddComponent<MeshFilter>();
        _headMeshRenderer = _headRoot.AddComponent<MeshRenderer>();
        _headMeshRenderer.sortingLayerName = "Default";
        _headMeshRenderer.sortingOrder = headSortingOrder;
        _headMesh = new Mesh { name = "PierceAimIndicator_HeadMesh" };
        _headMesh.MarkDynamic();
        _headMeshFilter.sharedMesh = _headMesh;

        Shader shader = Shader.Find("Sprites/Default");
        _material = new Material(shader) { name = "PierceAimIndicator_Material", color = indicatorColor };
        _meshRenderer.sharedMaterial = _material;
        _headMeshRenderer.sharedMaterial = _material;
    }

    private Pulse SpawnPulse()
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
            if (_pulses.Count >= maxPulseCount)
                return null;

            var root = new GameObject("PierceAimIndicator_Pulse");
            root.transform.SetParent(transform, false);
            PierceVortexVisual vortex = PierceVortexVisual.Create(root.transform,
                enableAfterimages: false, visualScaleMultiplier: pulseScale, useUnscaledTime: true);
            vortex?.SetFade(pulseAlpha);
            SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sortingLayerName = "Default";
                renderers[i].sortingOrder = pulseSortingOrder + i;
            }

            pulse = new Pulse { root = root };
            _pulses.Add(pulse);
        }

        pulse.distance = 0f;
        pulse.endHoldTimer = 0f;
        pulse.reachedEnd = false;
        pulse.active = true;
        pulse.root.SetActive(true);
        pulse.root.transform.position = _pathPoints[0];
        return pulse;
    }

    private void RebuildMesh(List<Vector3> worldPath)
    {
        if (worldPath == null || worldPath.Count < 2)
        {
            _mesh.Clear();
            return;
        }

        float totalLength = 0f;
        for (int i = 0; i < worldPath.Count - 1; i++)
            totalLength += Vector3.Distance(worldPath[i], worldPath[i + 1]);
        if (totalLength < 0.001f)
        {
            _mesh.Clear();
            return;
        }

        _pathLength = totalLength;
        float bodyEndDistance = Mathf.Max(0f, totalLength - headLength);
        float step = bodyBlockLength + bodyBlockGap;
        int blockCount = bodyEndDistance > 0f ? Mathf.CeilToInt(bodyEndDistance / step) : 0;
        var vertices = new Vector3[blockCount * 4];
        var colors = new Color[vertices.Length];
        var triangles = new int[blockCount * 6];

        int vertexIndex = 0;
        int triangleIndex = 0;
        float cursor = 0f;
        for (int i = 0; i < blockCount && cursor < bodyEndDistance; i++)
        {
            float length = Mathf.Min(bodyBlockLength, bodyEndDistance - cursor);
            Vector3 blockStart = GetPointAtDistance(worldPath, cursor);
            Vector3 blockEnd = GetPointAtDistance(worldPath, cursor + length);
            Vector3 localStart = _visualRoot.transform.InverseTransformPoint(blockStart);
            Vector3 localEnd = _visualRoot.transform.InverseTransformPoint(blockEnd);
            Vector3 direction = localEnd - localStart;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                cursor += step;
                continue;
            }

            direction.Normalize();
            Vector3 right = new Vector3(direction.z, 0f, -direction.x);
            Vector3 rightExtent = right * (bodyBlockWidth * 0.5f);
            vertices[vertexIndex] = localStart - rightExtent;
            vertices[vertexIndex + 1] = localStart + rightExtent;
            vertices[vertexIndex + 2] = localEnd + rightExtent;
            vertices[vertexIndex + 3] = localEnd - rightExtent;
            triangles[triangleIndex] = vertexIndex;
            triangles[triangleIndex + 1] = vertexIndex + 2;
            triangles[triangleIndex + 2] = vertexIndex + 1;
            triangles[triangleIndex + 3] = vertexIndex;
            triangles[triangleIndex + 4] = vertexIndex + 3;
            triangles[triangleIndex + 5] = vertexIndex + 2;
            vertexIndex += 4;
            triangleIndex += 6;
            cursor += step;
        }

        for (int i = 0; i < colors.Length; i++)
            colors[i] = Color.white;

        _mesh.Clear();
        _mesh.vertices = vertices;
        _mesh.colors = colors;
        _mesh.triangles = triangles;
        _mesh.RecalculateBounds();
        RebuildHead(worldPath, totalLength);
    }

    private void RebuildHead(List<Vector3> worldPath, float totalLength)
    {
        Vector3 worldTip = worldPath[worldPath.Count - 1];
        Vector3 worldBase = GetPointAtDistance(worldPath, Mathf.Max(0f, totalLength - headLength));
        Camera camera = Camera.main;
        if (camera == null)
        {
            _headMesh.Clear();
            return;
        }

        Vector3 tipScreen = camera.WorldToScreenPoint(worldTip);
        Vector3 baseScreen = camera.WorldToScreenPoint(worldBase);
        Vector2 screenDirection = new Vector2(tipScreen.x - baseScreen.x, tipScreen.y - baseScreen.y);
        if (screenDirection.sqrMagnitude < 0.0001f)
            screenDirection = Vector2.up;
        screenDirection.Normalize();
        Vector2 screenRight = new Vector2(screenDirection.y, -screenDirection.x);
        Vector3 worldScreenDirection = camera.transform.right * screenDirection.x
            + camera.transform.up * screenDirection.y;
        Vector3 worldScreenRight = camera.transform.right * screenRight.x
            + camera.transform.up * screenRight.y;
        Vector3 baseCenter = worldTip - worldScreenDirection * headLength;
        Vector3 rightExtent = worldScreenRight * (headWidth * 0.5f);

        _headRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        Vector3 tip = _headRoot.transform.InverseTransformPoint(worldTip);
        Vector3 leftBase = _headRoot.transform.InverseTransformPoint(baseCenter - rightExtent);
        Vector3 rightBase = _headRoot.transform.InverseTransformPoint(baseCenter + rightExtent);
        _headMesh.Clear();
        _headMesh.vertices = new[] { tip, leftBase, rightBase };
        _headMesh.triangles = new[] { 0, 1, 2 };
        _headMesh.colors = new[] { Color.white, Color.white, Color.white };
        _headMesh.RecalculateBounds();
    }

    private static Vector3 GetPointAtDistance(List<Vector3> path, float distance)
    {
        float remaining = Mathf.Max(0f, distance);
        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector3 start = path[i];
            Vector3 end = path[i + 1];
            float segmentLength = Vector3.Distance(start, end);
            if (remaining <= segmentLength || i == path.Count - 2)
            {
                float t = segmentLength > 0.0001f ? Mathf.Clamp01(remaining / segmentLength) : 0f;
                return Vector3.Lerp(start, end, t);
            }
            remaining -= segmentLength;
        }
        return path[path.Count - 1];
    }

    private static int CalculatePathSignature(List<Vector3> path)
    {
        unchecked
        {
            int hash = path.Count;
            for (int i = 0; i < path.Count; i++)
            {
                Vector3 point = path[i];
                hash = hash * 31 + Mathf.RoundToInt(point.x * 100f);
                hash = hash * 31 + Mathf.RoundToInt(point.y * 100f);
                hash = hash * 31 + Mathf.RoundToInt(point.z * 100f);
            }
            return hash;
        }
    }

    private void PopulatePulses()
    {
        ResetPulse();
        if (_pathLength <= 0.001f)
            return;

        float spacing = Mathf.Max(pulseSpeed * Mathf.Max(pulseInterval, 0.01f), 0.01f);
        int count = Mathf.Min(maxPulseCount, Mathf.CeilToInt(_pathLength / spacing));
        for (int i = 0; i < count; i++)
        {
            Pulse pulse = SpawnPulse();
            if (pulse == null)
                break;

            pulse.distance = Mathf.Min(i * spacing, _pathLength - 0.001f);
            Vector3 position = GetPointAtDistance(_pathPoints, pulse.distance);
            Vector3 ahead = GetPointAtDistance(_pathPoints, Mathf.Min(pulse.distance + 0.1f, _pathLength));
            Vector3 direction = ahead - position;
            if (direction.sqrMagnitude > 0.0001f)
                pulse.root.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
            pulse.root.transform.position = position;
        }

        _pulseSpawnTimer = pulseInterval;
    }

    private void ResetPulse()
    {
        _pulseSpawnTimer = 0f;
        for (int i = 0; i < _pulses.Count; i++)
        {
            Pulse pulse = _pulses[i];
            pulse.active = false;
            pulse.reachedEnd = false;
            pulse.endHoldTimer = 0f;
            if (pulse.root != null)
                pulse.root.SetActive(false);
        }
    }

    private void SetVisible(bool visible)
    {
        if (_visualRoot != null && _visualRoot.activeSelf != visible)
            _visualRoot.SetActive(visible);
        if (!visible)
        {
            for (int i = 0; i < _pulses.Count; i++)
                _pulses[i].root.SetActive(false);
        }
    }
}
