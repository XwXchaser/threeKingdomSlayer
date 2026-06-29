using UnityEngine;

/// <summary>
/// 反伤盾（铁壁·反伤）视觉表现组件。
/// 挂载在玩家根对象上，根据蓄力阶段和盾状态驱动三段式动画：
///   Starting (蓄力中) → start1~3   /   Looping (蓄力就绪) → loop1~2   /   Losing (盾触发) → lose1 + fade
/// </summary>
public class ThornArmorEffect : MonoBehaviour
{
    [Header("精灵帧")]
    public Sprite start1;
    public Sprite start2;
    public Sprite start3;
    public Sprite loop1;
    public Sprite loop2;
    public Sprite lose1;

    [Header("尺寸与位置")]
    [Range(0.5f, 20f)]
    public float worldSize = 2.5f;
    public Vector3 localOffset = Vector3.zero;

    [Header("动画参数")]
    [Tooltip("蓄力进度阈值（0~1），与 ChargeIndicatorController.appearThreshold 保持一致")]
    [Range(0f, 1f)]
    public float appearThreshold = 0.3f;
    public float loopInterval = 0.3f;
    public float loseDuration = 0.4f;

    private enum State { Idle, Starting, Looping, Losing }

    private GameObject _visualRoot;
    private SpriteRenderer _sr;
    private State _state = State.Idle;
    private float _loopTimer;
    private float _loseTimer;
    private bool _loopToggle;
    private bool _shieldHad;

    // 蓄力进度缓存
    private float _chargeProgress;
    private bool _isCharging;

    private void Awake()
    {
        CreateVisual();
    }

    private void Start()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnChargeBegan += OnChargeBegan;
            InputManager.Instance.OnChargeUpdated += OnChargeUpdated;
            InputManager.Instance.OnChargeEnded += OnChargeEnded;
        }
        if (UpgradeEffectManager.Instance != null)
        {
            UpgradeEffectManager.Instance.OnReflectShieldConsumed += OnShieldConsumed;
        }
    }

    private void OnDestroy()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnChargeBegan -= OnChargeBegan;
            InputManager.Instance.OnChargeUpdated -= OnChargeUpdated;
            InputManager.Instance.OnChargeEnded -= OnChargeEnded;
        }
        if (UpgradeEffectManager.Instance != null)
        {
            UpgradeEffectManager.Instance.OnReflectShieldConsumed -= OnShieldConsumed;
        }
        if (_visualRoot != null)
            Destroy(_visualRoot);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
            UnityEditor.EditorApplication.delayCall += RefreshPreview;
    }

    private void RefreshPreview()
    {
        if (this == null) return;
        UnityEditor.EditorApplication.delayCall -= RefreshPreview;
        CreateVisual();
        if (_visualRoot != null)
        {
            _visualRoot.SetActive(true);
            _visualRoot.transform.localPosition = localOffset;
            _sr.sprite = loop1 != null ? loop1 : start1;
            float basePixelsPerUnit = _sr.sprite != null ? _sr.sprite.pixelsPerUnit : 100f;
            float basePixelSize = _sr.sprite != null ? Mathf.Max(_sr.sprite.rect.width, _sr.sprite.rect.height) : 128f;
            float baseWorldSize = basePixelSize / basePixelsPerUnit;
            float scale = baseWorldSize > 0.001f ? worldSize / baseWorldSize : 1f;
            _visualRoot.transform.localScale = Vector3.one * scale;
        }
    }
#endif

    private void Update()
    {
        switch (_state)
        {
            case State.Starting:
                UpdateStarting();
                break;
            case State.Looping:
                UpdateLooping();
                break;
            case State.Losing:
                UpdateLosing();
                break;
        }
    }

    // ── 公有事件回调 ──

    private void OnChargeBegan(Vector2 pos)
    {
        // 不再在此处检查 ShieldExists()，避免与 PlayerState.OnChargeBegan 的 TryGrantShield 竞态。
        // 盾的存在性推迟到 OnChargeUpdated 中判定。
        _isCharging = true;
        _chargeProgress = 0f;
    }

    private void OnChargeUpdated(Vector2 pos, float progress)
    {
        _chargeProgress = progress;
        // Idle → Starting: 进度达阈值且盾存在。不 bail out，持续等待 PlayerState 授予护盾
        if (_state == State.Idle && _isCharging && progress >= appearThreshold && ShieldExists())
        {
            TransitionTo(State.Starting);
        }
        // Starting → Looping: 蓄力满且盾仍在
        if (_state == State.Starting && progress >= 1f)
        {
            if (ShieldExists())
                TransitionTo(State.Looping);
            else
                TransitionTo(State.Idle);
        }
    }

    private void OnChargeEnded()
    {
        _isCharging = false;
        // 离开蓄力 → 护盾消失，播放破碎动画
        if (_state == State.Starting || _state == State.Looping)
            TransitionTo(State.Losing);
    }

    private void OnShieldConsumed()
    {
        // 在 Looping 状态下盾被消耗 → 播放破碎
        if (_state == State.Looping)
            TransitionTo(State.Losing);
    }

    // ── 状态更新 ──

    private void UpdateStarting()
    {
        // 将 [appearThreshold, 1] 重映射到 [0, 1]
        float t = Mathf.InverseLerp(appearThreshold, 1f, _chargeProgress);
        if (t < 0.33f)
            _sr.sprite = start1;
        else if (t < 0.66f)
            _sr.sprite = start2;
        else
            _sr.sprite = start3;
    }

    private void UpdateLooping()
    {
        _loopTimer -= Time.deltaTime;
        if (_loopTimer <= 0f)
        {
            _loopTimer = loopInterval;
            _loopToggle = !_loopToggle;
            _sr.sprite = _loopToggle ? loop1 : loop2;
        }
    }

    private void UpdateLosing()
    {
        _loseTimer -= Time.deltaTime;
        float t = Mathf.Clamp01(_loseTimer / loseDuration);
        if (_sr != null)
        {
            Color c = _sr.color;
            c.a = t;
            _sr.color = c;
        }
        if (_loseTimer <= 0f)
            TransitionTo(State.Idle);
    }

    // ── 内部 ──

    private void CreateVisual()
    {
        Transform existing = transform.Find("ThornArmorVisual");
        if (existing != null)
        {
            _visualRoot = existing.gameObject;
            _sr = _visualRoot.GetComponent<SpriteRenderer>();
            if (_sr == null)
                _sr = _visualRoot.AddComponent<SpriteRenderer>();
            if (_sr.sharedMaterial == null || _sr.sharedMaterial.shader.name != "Unlit/Transparent")
                _sr.sharedMaterial = new Material(Shader.Find("Unlit/Transparent"));
            _sr.sortingOrder = 10;
            _visualRoot.transform.localPosition = localOffset;
            _visualRoot.SetActive(false);
            return;
        }

        _visualRoot = new GameObject("ThornArmorVisual");
        _visualRoot.transform.SetParent(transform, false);
        _visualRoot.transform.localPosition = localOffset;

        _sr = _visualRoot.AddComponent<SpriteRenderer>();
        _sr.sortingOrder = 10;
        _sr.sharedMaterial = new Material(Shader.Find("Unlit/Transparent"));

        _visualRoot.SetActive(false);
    }

    private bool ShieldExists()
    {
        return UpgradeEffectManager.Instance != null && UpgradeEffectManager.Instance.GetHasReflectShield();
    }

    private void TransitionTo(State newState)
    {
        _state = newState;
        switch (newState)
        {
            case State.Idle:
                _visualRoot.SetActive(false);
                _isCharging = false;
                break;
            case State.Starting:
                _visualRoot.SetActive(true);
                _sr.color = Color.white;
                // 复位 world size（SpriteRenderer 的 size 由 sprite 决定；这里通过 localScale 控制）
                float basePixelsPerUnit = start1 != null ? start1.pixelsPerUnit : 100f;
                float basePixelSize = start1 != null ? Mathf.Max(start1.rect.width, start1.rect.height) : 128f;
                float baseWorldSize = basePixelSize / basePixelsPerUnit;
                float scale = baseWorldSize > 0.001f ? worldSize / baseWorldSize : 1f;
                _visualRoot.transform.localScale = Vector3.one * scale;
                break;
            case State.Looping:
                _loopTimer = 0f;
                _loopToggle = false;
                break;
            case State.Losing:
                _loseTimer = loseDuration;
                _sr.sprite = lose1;
                _sr.color = Color.white;
                break;
        }
    }
}
