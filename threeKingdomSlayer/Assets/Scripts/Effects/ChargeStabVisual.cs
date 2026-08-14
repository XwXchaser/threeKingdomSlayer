using UnityEngine;

/// <summary>
/// 蓄力 Stab 前端可视化：跟随手指/鼠标在世界空间显示蓄力武器精灵
/// 监听 InputManager 蓄力事件，X 轴跟随手指左右移动，Z 旋转模拟 Slash 朝向
/// </summary>
public class ChargeStabVisual : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject stabPrefab;

    [Header("蓄力精灵")]
    public Sprite chargeSprite1;
    public Sprite chargeSprite2;
    public Sprite readySprite;
    public Sprite loopSprite1;
    public Sprite loopSprite2;

    [Header("位置偏移 (Slash 配置)")]
    public float spawnYOffset = 0f;
    public float spawnZOffset = -3.6f;

    [Header("水平移动范围")]
    public float halfWidth = 3f;

    [Header("旋转角度 (度)")]
    public float maxAngle = 60f;
    [Tooltip("手指上下移动带来的最大 X 轴俯仰角。以蓄力视觉刚出现时的指尖位置为 0，向上为负。")]
    public float maxPitchAngle = 10f;
    [Tooltip("手指向下移动带来的最大 X 轴俯仰角。以蓄力视觉刚出现时的指尖位置为 0，向下为正。")]
    public float maxDownPitchAngle = 20f;
    [Tooltip("手指在跟随平面内上下移动多少世界单位时达到最大俯仰角。")]
    public float verticalTiltHalfHeight = 2f;

    [Header("缩放")]
    public Vector3 visualScale = new Vector3(0.1f, 0.1f, 0.1f);

    [Header("动画参数")]
    [Range(0f, 1f)]
    public float appearThreshold = 0.3f;
    public float readyDuration = 0.2f;
    public float loopInterval = 0.3f;

    [Header("入场与退场")]
    [Tooltip("蓄力武器从镜头后方刺入跟手位置的时长（秒）。")]
    public float enterDuration = 0.16f;
    [Tooltip("蓄力武器向镜头后方收回的快速时长（秒）。")]
    public float exitDuration = 0.13f;
    [Tooltip("入场/退场沿相机纵深轴的移动距离（世界单位）。")]
    public float entryDistance = 3.5f;

    [Header("渐隐")]
    public float fadeOutDuration = 0.25f;

    private GameObject _visualInstance;
    private SpriteRenderer _sr;
    private float _weaponLength;
    private Camera _mainCam;

    private bool _isActive;
    private bool _hasAppeared;
    private bool _isCharged;
    private bool _readyShown;
    private float _readyTimer;
    private float _loopTimer;
    private bool _loopToggle;

    private float _fadeTimer;
    private float _exitDuration;
    private bool _isFadingOut;
    private bool _isEntering;
    private float _enterTimer;
    private Vector2 _lastScreenPos;
    private Vector3 _entryAxis;
    private Vector3 _enterStartPosition;
    private Vector3 _enterTargetPosition;
    private Quaternion _enterStartRotation;
    private Quaternion _enterTargetRotation;
    private bool _hasPitchBaseline;
    private Vector3 _pitchBaselineWorldPos;

    private void Start()
    {
        _mainCam = Camera.main;

        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnChargeBegan += OnChargeBegan;
            InputManager.Instance.OnChargeUpdated += OnChargeUpdated;
            InputManager.Instance.OnChargeEnded += OnChargeEnded;
            InputManager.Instance.OnAttackExecuted += OnAttackExecuted;
        }

        if (PlayerState.Instance != null)
            PlayerState.Instance.OnPlayerDied += OnChargeEnded;
    }

    private void Update()
    {
        if (!_hasAppeared || _visualInstance == null) return;

        if (_isEntering)
        {
            _enterTimer += Time.deltaTime;
            float t = enterDuration > 0.001f ? Mathf.Clamp01(_enterTimer / enterDuration) : 1f;
            float windupT = Mathf.Clamp01(t / 0.3f);
            float thrustT = Mathf.Clamp01((t - 0.3f) / 0.7f);
            Vector3 windupPosition = _enterTargetPosition - _entryAxis * entryDistance;
            Vector3 currentPosition = Vector3.Lerp(windupPosition, _enterTargetPosition, Mathf.SmoothStep(0f, 1f, windupT));
            if (t > 0.3f)
                currentPosition = Vector3.Lerp(windupPosition, _enterTargetPosition, Mathf.Lerp(0.3f, 1f, Mathf.SmoothStep(0f, 1f, thrustT)));
            ApplyChargePose(currentPosition);
            if (t >= 1f)
            {
                _isEntering = false;
                ApplyChargePose(_enterTargetPosition);
            }
        }

        // 渐隐
        if (_isFadingOut)
        {
            _fadeTimer -= Time.deltaTime;
            UpdateExit();
            if (_fadeTimer <= 0f)
            {
                DestroyChargeVisual();
                return;
            }
            float alpha = _exitDuration > 0.001f ? Mathf.Clamp01(_fadeTimer / _exitDuration) : 0f;
            SetAlpha(alpha);
            return;
        }

        // 满蓄力循环计时
        if (_isCharged && _readyShown)
        {
            _loopTimer -= Time.deltaTime;
            if (_loopTimer <= 0f)
            {
                _loopTimer = loopInterval;
                _loopToggle = !_loopToggle;
                if (_sr != null)
                    _sr.sprite = _loopToggle ? loopSprite2 : loopSprite1;
            }
        }
    }

    private void OnDestroy()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnChargeBegan -= OnChargeBegan;
            InputManager.Instance.OnChargeUpdated -= OnChargeUpdated;
            InputManager.Instance.OnChargeEnded -= OnChargeEnded;
            InputManager.Instance.OnAttackExecuted -= OnAttackExecuted;
        }
        if (PlayerState.Instance != null)
            PlayerState.Instance.OnPlayerDied -= OnChargeEnded;
        DestroyChargeVisual();
    }

    private void OnChargeBegan(Vector2 screenPos)
    {
        if (_isFadingOut)
            DestroyChargeVisual();

        CancelInvoke();
        _isActive = true;
        _hasAppeared = false;
        _isCharged = false;
        _readyShown = false;
        _isFadingOut = false;
        _isEntering = false;
        _hasPitchBaseline = false;
    }

    private void OnChargeUpdated(Vector2 screenPos, float progress)
    {
        _lastScreenPos = screenPos;
        if (!_isActive) return;

        if (progress >= appearThreshold)
        {
            if (!_hasAppeared)
            {
                _hasAppeared = true;
                CreateChargeVisual(screenPos);
            }

            if (_isEntering)
            {
                UpdateEntryTarget(screenPos);
            }
            else if (!_isFadingOut)
            {
                UpdatePosition(screenPos);
            }

            float visualProgress = Mathf.InverseLerp(GetChargeBeginProgress(), 1f, progress);
            UpdateSprite(visualProgress);
        }
    }

    private void OnAttackExecuted(AttackType attackType, int targetColumn)
    {
        if (attackType == AttackType.Slash)
            HandoffToSlash();
    }

    private void OnChargeEnded()
    {
        CancelInvoke();
        _isActive = false;
        _isCharged = false;
        _readyShown = false;

        if (_hasAppeared && _visualInstance != null)
        {
            BeginExit(false);
        }
        else
        {
            DestroyChargeVisual();
        }
    }

    private void CreateChargeVisual(Vector2 screenPos)
    {
        if (stabPrefab == null)
        {
            Debug.LogError("[ChargeStabVisual] stabPrefab 未赋值");
            return;
        }

        _visualInstance = Instantiate(stabPrefab);
        _visualInstance.name = "ChargeStabVisual_Instance";

        // 移除 Collider，纯视觉
        var collider = _visualInstance.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        var collider2D = _visualInstance.GetComponent<Collider2D>();
        if (collider2D != null) Destroy(collider2D);

        _sr = _visualInstance.GetComponent<SpriteRenderer>();
        if (_sr == null)
            _sr = _visualInstance.AddComponent<SpriteRenderer>();

        _visualInstance.transform.localScale = visualScale;
        _visualInstance.transform.SetParent(null); // 世界空间独立
        _weaponLength = _sr.bounds.size.y; // 世界枪长

        if (TryGetPointerWorldPosition(screenPos, out Vector3 worldPos))
        {
            _pitchBaselineWorldPos = worldPos;
            _hasPitchBaseline = true;
        }

        if (TryGetPointerWorldPosition(screenPos, out Vector3 targetPosition))
        {
            targetPosition = ClampFollowPosition(targetPosition);
            _enterTargetPosition = targetPosition;
            _enterTargetRotation = CalculateFollowRotation(targetPosition);
            _entryAxis = _enterTargetRotation * Vector3.up;
            _enterStartPosition = _enterTargetPosition - _entryAxis * entryDistance;
            ApplyChargePose(_enterStartPosition);
            _enterTimer = 0f;
            _isEntering = true;
        }
    }

    private void UpdateEntryTarget(Vector2 screenPos)
    {
        if (!_isEntering || _visualInstance == null) return;
        if (!TryGetPointerWorldPosition(_lastScreenPos, out Vector3 targetPosition)) return;

        if (!_isFadingOut)
        {
            _enterTargetPosition = ClampFollowPosition(targetPosition);
            _enterTargetRotation = CalculateFollowRotation(_enterTargetPosition);
            _entryAxis = _enterTargetRotation * Vector3.up;
        }
    }

    public bool TryGetCurrentVisualPose(out Vector3 position, out Quaternion rotation, out Vector3 scale)
    {
        if (_visualInstance == null)
        {
            position = default;
            rotation = default;
            scale = default;
            return false;
        }

        position = _visualInstance.transform.position;
        rotation = _visualInstance.transform.rotation;
        scale = _visualInstance.transform.localScale;
        return true;
    }

    public void SuppressFadeAndDestroy()
    {
        CancelInvoke();
        _isActive = false;
        _isCharged = false;
        _readyShown = false;
        _isEntering = false;
        _isFadingOut = false;
        DestroyChargeVisual();
    }

    public void HandoffToSlash()
    {
        if (_visualInstance == null)
            return;
        BeginExit(true);
    }

    private void BeginExit(bool fast)
    {
        if (_isFadingOut)
            return;

        _isEntering = false;
        _isActive = false;
        _isCharged = false;
        _readyShown = false;
        _isFadingOut = true;
        _exitDuration = fast ? Mathf.Min(exitDuration, fadeOutDuration) : fadeOutDuration;
        _fadeTimer = _exitDuration;

        if (_mainCam != null && _visualInstance != null)
        {
            _entryAxis = (_visualInstance.transform.rotation * Vector3.up).normalized;
            _enterStartPosition = _visualInstance.transform.position;
            _enterTargetPosition = _enterStartPosition - _entryAxis * entryDistance;
            _enterStartRotation = _visualInstance.transform.rotation;
            _enterTargetRotation = _enterStartRotation;
            _enterTimer = 0f;
        }
    }

    private void UpdateExit()
    {
        if (!_isFadingOut || _visualInstance == null)
            return;

        float duration = Mathf.Max(_exitDuration, 0.001f);
        float t = 1f - Mathf.Clamp01(_fadeTimer / duration);
        float eased = Mathf.SmoothStep(0f, 1f, t);
        _visualInstance.transform.position = Vector3.Lerp(_enterStartPosition, _enterTargetPosition, eased);
        _visualInstance.transform.rotation = _enterStartRotation;
    }

    /// <summary>
    /// 以枪尖为锚点定位整把枪（复刻 Stab 的射出角结构）：
    /// 枪尖对齐 tipPosition（跟手），枪尾沿枪轴反向延伸。
    /// 枪尾偏移完全由射出角（maxAngle → zRot → axis.x）决定，不做额外平移。
    /// </summary>
    private void ApplyChargePose(Vector3 tipPosition)
    {
        if (_visualInstance == null || _sr == null) return;

        Quaternion rotation = CalculateFollowRotation(tipPosition);
        Vector3 axis = rotation * Vector3.up;
        float halfLength = _weaponLength > 0.001f ? _weaponLength * 0.5f : 0f;

        _visualInstance.transform.position = tipPosition - axis * halfLength;
        _visualInstance.transform.rotation = rotation;
        _visualInstance.transform.localScale = visualScale;
    }

    private Vector3 ClampFollowPosition(Vector3 worldPosition)
    {
        Vector3 playerPos = transform.position;
        worldPosition.x = Mathf.Clamp(worldPosition.x, playerPos.x - halfWidth, playerPos.x + halfWidth);
        // 固定 Y 高度，与 Slash/Stab 攻击高度一致，避免抬手抬高蓄力武器。
        worldPosition.y = playerPos.y + spawnYOffset;
        return worldPosition;
    }

    private Quaternion CalculateFollowRotation(Vector3 worldPosition)
    {
        Vector3 playerPos = transform.position;
        float offsetX = Mathf.Clamp(worldPosition.x - playerPos.x, -halfWidth, halfWidth);
        float zRot = halfWidth > 0.001f ? (offsetX / halfWidth) * maxAngle : 0f;
        // Y 已固定，不再随上下移动产生 X 轴俯仰，避免枪尖摇摆。
        return Quaternion.Euler(90f, 0f, -zRot);
    }

    private void DestroyChargeVisual()
    {
        if (_visualInstance != null)
        {
            Destroy(_visualInstance);
            _visualInstance = null;
            _sr = null;
        }
        _hasAppeared = false;
        _isEntering = false;
        _isFadingOut = false;
        _hasPitchBaseline = false;
    }

    private Vector3 GetFollowPlaneAnchor()
    {
        Vector3 playerPos = transform.position;
        return playerPos + _mainCam.transform.up * spawnYOffset + Vector3.forward * spawnZOffset;
    }

    private bool TryGetPointerWorldPosition(Vector2 screenPos, out Vector3 worldPos)
    {
        worldPos = default;
        if (_mainCam == null) return false;

        Plane followPlane = new Plane(_mainCam.transform.forward, GetFollowPlaneAnchor());
        Ray ray = _mainCam.ScreenPointToRay(screenPos);
        if (!followPlane.Raycast(ray, out float distance)) return false;

        worldPos = ray.GetPoint(distance);
        return true;
    }

    private void UpdatePosition(Vector2 screenPos)
    {
        if (_mainCam == null || _visualInstance == null) return;
        if (!TryGetPointerWorldPosition(screenPos, out Vector3 worldPos)) return;

        ApplyChargePose(ClampFollowPosition(worldPos));
    }

    private void UpdateSprite(float progress)
    {
        if (_sr == null) return;

        if (progress < 1f)
        {
            _sr.sprite = progress < 0.65f ? chargeSprite1 : chargeSprite2;
        }
        else if (!_readyShown)
        {
            // 首次到达 100%
            _sr.sprite = readySprite;
            _isCharged = true;
            _readyShown = true;
            _readyTimer = readyDuration;

            // readyDuration 后切换到 loop
            Invoke(nameof(StartLoop), readyDuration);
        }
    }

    private void StartLoop()
    {
        if (!_isCharged || _sr == null) return;
        _loopTimer = 0f; // 立即触发第一次切换
        _loopToggle = false;
        if (_sr != null) _sr.sprite = loopSprite1;
    }

    private void SetAlpha(float alpha)
    {
        if (_sr != null)
        {
            Color c = _sr.color;
            c.a = alpha;
            _sr.color = c;
        }
    }

    private float GetChargeBeginProgress()
    {
        if (InputManager.Instance == null) return 0f;
        float minCharge = InputManager.Instance.minChargeTime;
        if (minCharge <= 0f) return 0f;
        return InputManager.Instance.longPressDuration / minCharge;
    }
}
