using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Sparse afterimage ("ghost") copies of the 3D weapon during opted-in attacks, plus an
/// in-frame body smear so the weapon itself blurs while it moves fast.
/// Every sample interval the current skinned pose is baked once, frozen in
/// world space, then faded out over the ghost lifetime; the smear instead re-uses a per-frame
/// bake and never survives into the next frame.
/// Opt-in only: the prefab copy stays disabled and allocates nothing until <see cref="Begin"/>.
/// </summary>
[DefaultExecutionOrder(1100)]
[DisallowMultipleComponent]
public sealed class MeshWeaponAfterimage : MonoBehaviour
{
    /// <summary>One frozen copy of a single source renderer.</summary>
    private sealed class Slot
    {
        public GameObject gameObject;
        public Transform transform;
        public MeshFilter filter;
        public MeshRenderer renderer;
        public Mesh mesh;
        public MaterialPropertyBlock properties;
    }

    private const string PoolRootName = "WeaponAfterimageGhosts";
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    [Tooltip("Master switch for opted-in Slash instances; leave this component disabled on the prefab.")]
    [SerializeField] private bool renderAfterimage = true;
    [Tooltip("Only suppress this Slash instance's old tip trail. Disable to compare both effects together.")]
    [SerializeField] private bool replaceTipTrail = true;
    [Tooltip("Ghost material. The weapon's own authored-colour fade material is the intended value.")]
    [SerializeField] private Material afterimageMaterial;
    [Tooltip("Only the alpha is animated at runtime; keep the tint neutral unless the art direction changes.")]
    [SerializeField] private Color ghostTint = Color.white;
    [Tooltip("Seconds between snapshots. Deliberately sparse: never sample every frame.")]
    [SerializeField, Range(0.02f, 0.2f)] private float sampleInterval = 0.06f;
    [Tooltip("Seconds a ghost survives while it fades from startAlpha to nothing.")]
    [SerializeField, Range(0.04f, 0.6f)] private float lifetime = 0.14f;
    [Tooltip("Alpha multiplier by normalized age: 0 is fresh, 1 is fully faded.")]
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
    [Tooltip("Alpha of a fresh ghost at full swing speed. Keep it low: the ghost must never outshine the weapon.")]
    [SerializeField, Range(0.05f, 0.8f)] private float startAlpha = 0.32f;
    [Tooltip("Maximum simultaneous ghosts. The oldest one is recycled when the pool is full.")]
    [SerializeField, Range(1, 4)] private int maxCopies = 3;
    [Tooltip("Game-time step longer than this is a jump (teleport or load): that step stores no ghost.")]
    [SerializeField, Min(0.02f)] private float maxSampleGap = 0.1f;
    [Tooltip("Tip world speed (units/s) below which no ghost is stored; a nearly still weapon would read as a double weapon.")]
    [SerializeField, Min(0f)] private float minTipSpeed = 8f;
    [Tooltip("Tip world speed (units/s) that reaches full ghost alpha.")]
    [SerializeField, Min(0.01f)] private float fullTipSpeed = 45f;
    [Tooltip("Alpha budget at minTipSpeed, so a slow swing still ghosts faintly instead of popping in.")]
    [SerializeField, Range(0f, 1f)] private float minSpeedIntensity = 0.5f;
    [Tooltip("In-frame body smear: draws a few extra copies between last frame's pose and this frame's pose, so the weapon body itself blurs while it moves fast. No copy survives into the next frame.")]
    [SerializeField] private bool bodyBlur = true;
    [Tooltip("Smear copies per frame. 2-4 reads as blur; more only costs fill rate.")]
    [SerializeField, Range(0, 6)] private int bodyBlurSamples = 3;
    [Tooltip("Alpha of the whole smear set at full speed. Keep it low: it must soften the body, not double it.")]
    [SerializeField, Range(0f, 1f)] private float bodyBlurAlpha = 0.3f;
    [Tooltip("How far back in time the oldest smear copy sits, in seconds of the weapon's own motion. 0.04-0.05 is a hint of blur; higher exaggerates it. Time-based so it does not change with frame rate.")]
    [SerializeField, Range(0.01f, 0.15f)] private float bodyBlurSeconds = 0.045f;

    [Header("Thrust profile (Stab / Pierce)")]
    [Tooltip("A thrust is roughly ten times faster than a swing, so it needs its own, much shorter time constants. Same units as the swing fields above.")]
    [SerializeField, Range(0.01f, 0.2f)] private float thrustSampleInterval = 0.018f;
    [SerializeField, Range(0.02f, 0.3f)] private float thrustLifetime = 0.035f;
    [SerializeField, Range(0.05f, 0.8f)] private float thrustStartAlpha = 0.28f;
    [Tooltip("Gate values for a thrust: the wind-up and the pull-back sit well below the thrust speed.")]
    [SerializeField, Min(0f)] private float thrustMinTipSpeed = 40f;
    [SerializeField, Min(0.01f)] private float thrustFullTipSpeed = 160f;
    [SerializeField, Range(0f, 1f)] private float thrustMinSpeedIntensity = 0.4f;
    [SerializeField, Range(0.001f, 0.15f)] private float thrustBodyBlurSeconds = 0.005f;
    [SerializeField, Range(0f, 1f)] private float thrustBodyBlurAlpha = 0.25f;
    [SerializeField] private string sortingLayer = "Default";
    [Tooltip("Ghost draw order. Keep it below the weapon's own order so the weapon always paints over its ghosts.")]
    [SerializeField] private int sortingOrder = 1;

    private Renderer[] _sources;
    private MeshWeaponVisual _weaponVisual;
    private Slot[] _slots;
    private Slot[] _smearSlots;
    private Mesh[] _smearMeshes;
    private Vector3[] _smearPrevPos;
    private Vector3[] _smearCurrPos;
    private Quaternion[] _smearPrevRot;
    private Quaternion[] _smearCurrRot;
    private Vector3[] _smearPrevScale;
    private Vector3[] _smearCurrScale;
    private bool _hasPrevPose;
    private Vector3 _blurLastTip;
    private bool _blurHasTip;
    private float[] _ages;
    private float[] _weights;
    private bool[] _live;
    private GameObject _poolRoot;
    private float _elapsedSinceSample;
    private Vector3 _lastTip;
    private bool _hasTip;
    private bool _running;
    private bool _emitting;
    private bool _locallyPaused;
    // Resolved per Begin from either the swing fields or the thrust profile, so the runtime path
    // never branches on the move type again.
    private float _sampleInterval;
    private float _lifetime;
    private float _startAlpha;
    private float _minTipSpeed;
    private float _fullTipSpeed;
    private float _minSpeedIntensity;
    private float _bodyBlurSeconds;
    private float _bodyBlurAlpha;

    public bool IsRunning => _running;
    public bool ReplaceTipTrail => replaceTipTrail;

    /// <summary>Number of ghosts currently fading; used by verification scripts.</summary>
    public int LiveCopyCount
    {
        get
        {
            if (_live == null) return 0;
            int count = 0;
            for (int i = 0; i < _live.Length; i++) if (_live[i]) count++;
            return count;
        }
    }

    /// <summary>Move type that a ghost instance belongs to; picks the swing or thrust time constants.</summary>
    public enum MotionProfile
    {
        Swing = 0,
        Thrust = 1
    }

    /// <summary>
    /// Starts ghosting for one opted-in attack instance. Copies are pooled in <see cref="Begin"/> so
    /// sampling never allocates. Safe to call on a disabled component; the component enables itself.
    /// </summary>
    public void Begin(MeshWeaponVisual weaponVisual, MotionProfile profile = MotionProfile.Swing)
    {
        if (!renderAfterimage)
        {
            Clear();
            return;
        }

        List<Renderer> sources = CollectSkinnedSources(weaponVisual);
        if (sources.Count == 0 || afterimageMaterial == null)
        {
            Debug.LogWarning("[MeshWeaponAfterimage] Missing skinned weapon renderer or ghost material.", this);
            Clear();
            return;
        }
        if (_running) Clear();
        enabled = true;

        bool thrust = profile == MotionProfile.Thrust;
        _sampleInterval = thrust ? thrustSampleInterval : sampleInterval;
        _lifetime = thrust ? thrustLifetime : lifetime;
        _startAlpha = thrust ? thrustStartAlpha : startAlpha;
        _minTipSpeed = thrust ? thrustMinTipSpeed : minTipSpeed;
        _fullTipSpeed = thrust ? thrustFullTipSpeed : fullTipSpeed;
        _minSpeedIntensity = thrust ? thrustMinSpeedIntensity : minSpeedIntensity;
        _bodyBlurSeconds = thrust ? thrustBodyBlurSeconds : bodyBlurSeconds;
        _bodyBlurAlpha = thrust ? thrustBodyBlurAlpha : bodyBlurAlpha;

        _weaponVisual = weaponVisual;
        _sources = sources.ToArray();
        CreatePool();
        _elapsedSinceSample = 0f;
        _lastTip = _weaponVisual.TipPosition;
        _hasTip = false;
        _blurLastTip = _lastTip;
        _blurHasTip = false;
        _hasPrevPose = false;
        _emitting = true;
        _locallyPaused = false;
        _running = true;
    }

    /// <summary>Stops storing new ghosts and stops the body smear. Live ghosts keep fading out normally.</summary>
    public void EndEmission()
    {
        SetEmission(false);
    }

    /// <summary>
    /// Brackets a move's own active phase: emission off means no new ghost and no body smear, so a
    /// stab can ghost while it thrusts and stay clean while it winds up or is pulled back. Live
    /// ghosts keep fading out normally.
    /// </summary>
    public void SetEmission(bool emitting)
    {
        _emitting = emitting;
    }

    /// <summary>
    /// Local hit-stop and any other freeze only pause the clock: no new snapshot, no fade progress,
    /// no cross-pause speed spike when the swing resumes.
    /// </summary>
    public void SetPaused(bool paused)
    {
        if (!_running || _locallyPaused == paused) return;
        _locallyPaused = paused;
        if (!paused)
        {
            _elapsedSinceSample = 0f;
            RefreshTipBaseline();
        }
    }

    /// <summary>Destroys every ghost and mesh. Idempotent; also used by disable and destroy.</summary>
    public void Clear()
    {
        _running = false;
        _emitting = false;
        if (enabled) enabled = false;

        if (_slots != null)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                Slot slot = _slots[i];
                if (slot == null || slot.mesh == null) continue;
                if (Application.isPlaying) Destroy(slot.mesh);
                else DestroyImmediate(slot.mesh);
                slot.mesh = null;
            }
        }

        if (_poolRoot != null)
        {
            if (Application.isPlaying) Destroy(_poolRoot);
            else DestroyImmediate(_poolRoot);
        }

        if (_smearMeshes != null)
        {
            for (int i = 0; i < _smearMeshes.Length; i++)
            {
                if (_smearMeshes[i] == null) continue;
                if (Application.isPlaying) Destroy(_smearMeshes[i]);
                else DestroyImmediate(_smearMeshes[i]);
            }
        }

        _poolRoot = null;
        _slots = null;
        _smearSlots = null;
        _smearMeshes = null;
        _smearPrevPos = null;
        _smearCurrPos = null;
        _smearPrevRot = null;
        _smearCurrRot = null;
        _smearPrevScale = null;
        _smearCurrScale = null;
        _hasPrevPose = false;
        _blurHasTip = false;
        _ages = null;
        _weights = null;
        _live = null;
        _sources = null;
        _weaponVisual = null;
        _hasTip = false;
    }

    private static List<Renderer> CollectSkinnedSources(MeshWeaponVisual weaponVisual)
    {
        var sources = new List<Renderer>(2);
        if (weaponVisual == null) return sources;
        Renderer[] renderers = weaponVisual.Renderers;
        if (renderers == null) return sources;
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] is SkinnedMeshRenderer) sources.Add(renderers[i]);
        return sources;
    }

    private void CreatePool()
    {
        int sourceCount = _sources.Length;
        _poolRoot = new GameObject(PoolRootName);
        // Scene root, identity transform: the frozen poses below are world poses and must not be
        // carried along by the moving weapon or by the Slash effect root.
        _poolRoot.transform.SetParent(null, false);
        _poolRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        _poolRoot.transform.localScale = Vector3.one;
        _poolRoot.layer = gameObject.layer;

        _slots = new Slot[maxCopies * sourceCount];
        _ages = new float[maxCopies];
        _weights = new float[maxCopies];
        _live = new bool[maxCopies];

        for (int copy = 0; copy < maxCopies; copy++)
        {
            for (int source = 0; source < sourceCount; source++)
            {
                var ghost = new GameObject($"WeaponGhost_{copy}_{_sources[source].name}");
                ghost.layer = gameObject.layer;
                ghost.transform.SetParent(_poolRoot.transform, false);
                ghost.SetActive(false);

                Mesh mesh = new Mesh { name = $"WeaponAfterimage_{copy}_{source}" };
                mesh.MarkDynamic();
                MeshFilter filter = ghost.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                MeshRenderer renderer = ghost.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = afterimageMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                renderer.sortingLayerName = sortingLayer;
                renderer.sortingOrder = sortingOrder;
                renderer.enabled = false;

                _slots[copy * sourceCount + source] = new Slot
                {
                    gameObject = ghost,
                    transform = ghost.transform,
                    filter = filter,
                    renderer = renderer,
                    mesh = mesh,
                    properties = new MaterialPropertyBlock()
                };
            }
        }

        CreateSmearPool();
    }

    /// <summary>
    /// Smear copies share one mesh per source renderer: every copy renders the pose baked this
    /// frame, and the copies differ only by the pose they are drawn at.
    /// </summary>
    private void CreateSmearPool()
    {
        if (!bodyBlur || bodyBlurSamples <= 0) return;
        int sourceCount = _sources.Length;
        _smearMeshes = new Mesh[sourceCount];
        _smearSlots = new Slot[bodyBlurSamples * sourceCount];
        _smearPrevPos = new Vector3[sourceCount];
        _smearCurrPos = new Vector3[sourceCount];
        _smearPrevRot = new Quaternion[sourceCount];
        _smearCurrRot = new Quaternion[sourceCount];
        _smearPrevScale = new Vector3[sourceCount];
        _smearCurrScale = new Vector3[sourceCount];

        for (int source = 0; source < sourceCount; source++)
        {
            Mesh mesh = new Mesh { name = $"WeaponSmear_{_sources[source].name}" };
            mesh.MarkDynamic();
            _smearMeshes[source] = mesh;
            for (int copy = 0; copy < bodyBlurSamples; copy++)
            {
                var smear = new GameObject($"WeaponSmear_{copy}_{_sources[source].name}");
                smear.layer = gameObject.layer;
                smear.transform.SetParent(_poolRoot.transform, false);
                smear.SetActive(false);

                MeshFilter filter = smear.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                MeshRenderer renderer = smear.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = afterimageMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                renderer.sortingLayerName = sortingLayer;
                renderer.sortingOrder = sortingOrder;
                renderer.enabled = false;

                _smearSlots[copy * sourceCount + source] = new Slot
                {
                    gameObject = smear,
                    transform = smear.transform,
                    filter = filter,
                    renderer = renderer,
                    mesh = null,
                    properties = new MaterialPropertyBlock()
                };
            }
        }
    }

    /// <summary>
    /// Per-frame in-frame smear: one bake serves every copy, and the copies are drawn between the
    /// previous frame's pose and this frame's pose, so the weapon body blurs without leaving a
    /// ghost behind in the world. Follows <see cref="EndEmission"/>: a stab that is being pulled
    /// back must not smear.
    /// </summary>
    private void UpdateBodySmear(float deltaTime)
    {
        if (_smearSlots == null || _smearMeshes == null || _weaponVisual == null) return;

        int sourceCount = _sources.Length;
        for (int i = 0; i < sourceCount; i++)
        {
            Transform source = _sources[i].transform;
            _smearCurrPos[i] = source.position;
            _smearCurrRot[i] = source.rotation;
            _smearCurrScale[i] = source.lossyScale;
        }

        Vector3 tip = _weaponVisual.TipPosition;
        float strength = 0f;
        if (_blurHasTip)
        {
            float speed = (tip - _blurLastTip).magnitude / Mathf.Max(deltaTime, 0.0001f);
            float denominator = Mathf.Max(_fullTipSpeed, _minTipSpeed + 0.01f);
            strength = Mathf.Clamp01(Mathf.Lerp(_minSpeedIntensity, 1f,
                Mathf.InverseLerp(_minTipSpeed, denominator, speed)));
        }
        _blurLastTip = tip;
        _blurHasTip = true;

        float alpha = _emitting ? _bodyBlurAlpha * strength * Mathf.Clamp01(_weaponVisual.Alpha) : 0f;
        if (!_hasPrevPose)
        {
            StoreSmearPose();
            _hasPrevPose = true;
            SetSmearVisible(false);
            return;
        }

        if (alpha <= 0.004f)
        {
            SetSmearVisible(false);
            StoreSmearPose();
            return;
        }

        for (int i = 0; i < sourceCount; i++)
        {
            var skinned = _sources[i] as SkinnedMeshRenderer;
            if (skinned != null) skinned.BakeMesh(_smearMeshes[i], true);
        }

        Color color = ghostTint;
        color.a = Mathf.Clamp01(alpha / Mathf.Max(bodyBlurSamples, 1));
        float frameFraction = Mathf.Max(deltaTime, 0.0001f);
        for (int copy = 0; copy < bodyBlurSamples; copy++)
        {
            // Copy 0 sits farthest back; the last copy almost touches the current pose.
            // Seconds are converted to a fraction of one frame so the smear is frame-rate independent.
            float back = _bodyBlurSeconds * (1f - (copy + 1f) / (bodyBlurSamples + 1f)) / frameFraction;
            for (int i = 0; i < sourceCount; i++)
            {
                Slot slot = _smearSlots[copy * sourceCount + i];
                if (slot == null || slot.renderer == null) continue;
                if (slot.gameObject != null && !slot.gameObject.activeSelf) slot.gameObject.SetActive(true);
                Vector3 position = Vector3.LerpUnclamped(_smearCurrPos[i], _smearPrevPos[i], back);
                Quaternion rotation = Quaternion.SlerpUnclamped(_smearCurrRot[i], _smearPrevRot[i], back);
                Vector3 scale = Vector3.LerpUnclamped(_smearCurrScale[i], _smearPrevScale[i], back);
                slot.transform.SetPositionAndRotation(position, rotation);
                slot.transform.localScale = scale;
                slot.properties.SetColor(ColorId, color);
                slot.renderer.SetPropertyBlock(slot.properties);
                if (!slot.renderer.enabled) slot.renderer.enabled = true;
            }
        }

        StoreSmearPose();
    }

    private void StoreSmearPose()
    {
        if (_smearPrevPos == null) return;
        for (int i = 0; i < _sources.Length; i++)
        {
            _smearPrevPos[i] = _smearCurrPos[i];
            _smearPrevRot[i] = _smearCurrRot[i];
            _smearPrevScale[i] = _smearCurrScale[i];
        }
    }

    private void SetSmearVisible(bool visible)
    {
        if (_smearSlots == null) return;
        for (int i = 0; i < _smearSlots.Length; i++)
        {
            Slot slot = _smearSlots[i];
            if (slot == null) continue;
            if (slot.renderer != null && !visible) slot.renderer.enabled = false;
            if (slot.gameObject != null && slot.gameObject.activeSelf != visible)
                slot.gameObject.SetActive(visible);
        }
    }

    private void LateUpdate()
    {
        if (!_running) return;

        float deltaTime = Time.deltaTime;
        if (_locallyPaused || deltaTime <= 0f)
        {
            // Frozen: no snapshot and no fade progress. The tip baseline is refreshed on resume so
            // the pause is never read back as a speed spike.
            return;
        }

        if (deltaTime > maxSampleGap)
        {
            // Jump in game time: restart the clock from the pose that is actually on screen instead
            // of storing a ghost that never happened.
            _elapsedSinceSample = 0f;
            RefreshTipBaseline();
            return;
        }

        AdvanceAges(deltaTime);
        UpdateBodySmear(deltaTime);

        if (!_emitting) return;
        _elapsedSinceSample += deltaTime;
        if (_elapsedSinceSample < _sampleInterval) return;
        _elapsedSinceSample = 0f;
        StoreSnapshot(deltaTime);
    }

    private void RefreshTipBaseline()
    {
        if (_weaponVisual == null) return;
        _lastTip = _weaponVisual.TipPosition;
        _hasTip = true;
    }

    private void AdvanceAges(float deltaTime)
    {
        float life = Mathf.Max(_lifetime, 0.01f);
        for (int copy = 0; copy < _live.Length; copy++)
        {
            if (!_live[copy]) continue;
            _ages[copy] += deltaTime;
            if (_ages[copy] / life >= 1f)
            {
                SetCopyLive(copy, false);
                continue;
            }
            ApplyAlpha(copy);
        }
    }

    private void StoreSnapshot(float deltaTime)
    {
        Vector3 tip = _weaponVisual.TipPosition;
        if (!_hasTip)
        {
            // First observation only establishes the speed baseline.
            _lastTip = tip;
            _hasTip = true;
            return;
        }

        float speed = (tip - _lastTip).magnitude / Mathf.Max(deltaTime, 0.0001f);
        _lastTip = tip;
        if (speed <= _minTipSpeed) return;

        float denominator = Mathf.Max(_fullTipSpeed, _minTipSpeed + 0.01f);
        float weight = Mathf.Clamp01(Mathf.Lerp(_minSpeedIntensity, 1f,
            Mathf.InverseLerp(_minTipSpeed, denominator, speed)));

        int copy = AcquireCopy();
        if (copy < 0) return;

        int sourceCount = _sources.Length;
        for (int source = 0; source < sourceCount; source++)
        {
            Slot slot = _slots[copy * sourceCount + source];
            Renderer renderer = _sources[source];
            SkinnedMeshRenderer skinned = renderer as SkinnedMeshRenderer;
            // Measured on this project's FBX: BakeMesh(mesh, true) yields geometry in the renderer's
            // own local space (no scale baked in), so the frozen world matrix is the renderer's own.
            skinned.BakeMesh(slot.mesh, true);
            slot.transform.SetPositionAndRotation(renderer.transform.position, renderer.transform.rotation);
            slot.transform.localScale = renderer.transform.lossyScale;
        }

        _ages[copy] = 0f;
        _weights[copy] = weight;
        SetCopyLive(copy, true);
        ApplyAlpha(copy);
    }

    private int AcquireCopy()
    {
        int oldest = -1;
        float oldestAge = -1f;
        for (int copy = 0; copy < _live.Length; copy++)
        {
            if (!_live[copy]) return copy;
            if (_ages[copy] > oldestAge)
            {
                oldestAge = _ages[copy];
                oldest = copy;
            }
        }
        return oldest;
    }

    private void SetCopyLive(int copy, bool live)
    {
        _live[copy] = live;
        int sourceCount = _sources.Length;
        for (int source = 0; source < sourceCount; source++)
        {
            Slot slot = _slots[copy * sourceCount + source];
            if (slot.renderer != null) slot.renderer.enabled = false;
            if (slot.gameObject != null) slot.gameObject.SetActive(live);
        }
    }

    private void ApplyAlpha(int copy)
    {
        float life = Mathf.Max(_lifetime, 0.01f);
        float age01 = Mathf.Clamp01(_ages[copy] / life);
        float curve = fadeCurve != null && fadeCurve.length > 0
            ? Mathf.Clamp01(fadeCurve.Evaluate(age01))
            : 1f - age01;
        float weaponAlpha = _weaponVisual != null ? Mathf.Clamp01(_weaponVisual.Alpha) : 1f;
        float alpha = Mathf.Clamp01(_startAlpha * _weights[copy] * curve * weaponAlpha);
        bool visible = alpha > 0.002f;

        Color color = ghostTint;
        color.a = alpha;

        int sourceCount = _sources.Length;
        for (int source = 0; source < sourceCount; source++)
        {
            Slot slot = _slots[copy * sourceCount + source];
            if (slot.renderer == null) continue;
            slot.properties.SetColor(ColorId, color);
            slot.renderer.SetPropertyBlock(slot.properties);
            slot.renderer.enabled = visible;
        }
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
