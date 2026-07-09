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

    [Header("渐隐")]
    public float fadeOutDuration = 0.25f;

    private GameObject _visualInstance;
    private SpriteRenderer _sr;
    private Camera _mainCam;

    private bool _isActive;
    private bool _hasAppeared;
    private bool _isCharged;
    private bool _readyShown;
    private float _readyTimer;
    private float _loopTimer;
    private bool _loopToggle;

    private float _fadeTimer;
    private bool _isFadingOut;
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
        }

        if (PlayerState.Instance != null)
            PlayerState.Instance.OnPlayerDied += OnChargeEnded;
    }

    private void Update()
    {
        if (!_hasAppeared || _visualInstance == null) return;

        // 渐隐
        if (_isFadingOut)
        {
            _fadeTimer -= Time.deltaTime;
            if (_fadeTimer <= 0f)
            {
                DestroyChargeVisual();
                return;
            }
            float alpha = Mathf.Clamp01(_fadeTimer / fadeOutDuration);
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
        }
        if (PlayerState.Instance != null)
            PlayerState.Instance.OnPlayerDied -= OnChargeEnded;
        DestroyChargeVisual();
    }

    private void OnChargeBegan(Vector2 screenPos)
    {
        // 取消上一次未完成的渐隐
        if (_isFadingOut)
            DestroyChargeVisual();

        CancelInvoke();
        _isActive = true;
        _hasAppeared = false;
        _isCharged = false;
        _readyShown = false;
        _isFadingOut = false;
        _hasPitchBaseline = false;
    }

    private void OnChargeUpdated(Vector2 screenPos, float progress)
    {
        if (!_isActive) return;

        if (progress >= appearThreshold)
        {
            if (!_hasAppeared)
            {
                _hasAppeared = true;
                CreateChargeVisual(screenPos);
            }

            UpdatePosition(screenPos);
            UpdateRotation(screenPos);
            UpdateSprite(progress);
        }
    }

    private void OnChargeEnded()
    {
        CancelInvoke();
        _isActive = false;
        _isCharged = false;
        _readyShown = false;

        if (_hasAppeared && _visualInstance != null)
        {
            _isFadingOut = true;
            _fadeTimer = fadeOutDuration;
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

        if (TryGetPointerWorldPosition(screenPos, out Vector3 worldPos))
        {
            _pitchBaselineWorldPos = worldPos;
            _hasPitchBaseline = true;
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
        DestroyChargeVisual();
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

        Vector3 playerPos = transform.position;
        float clampedX = Mathf.Clamp(worldPos.x, playerPos.x - halfWidth, playerPos.x + halfWidth);
        _visualInstance.transform.position = new Vector3(clampedX, worldPos.y, worldPos.z);
    }

    private void UpdateRotation(Vector2 screenPos)
    {
        if (_mainCam == null || _visualInstance == null) return;
        if (!TryGetPointerWorldPosition(screenPos, out Vector3 worldPos)) return;

        Vector3 playerPos = transform.position;
        float offsetX = Mathf.Clamp(worldPos.x - playerPos.x, -halfWidth, halfWidth);
        float zRot = halfWidth > 0.001f ? (offsetX / halfWidth) * maxAngle : 0f;
        float xPitch = 0f;
        if (_hasPitchBaseline && verticalTiltHalfHeight > 0.001f)
        {
            float verticalOffset = Vector3.Dot(worldPos - _pitchBaselineWorldPos, _mainCam.transform.up);
            if (verticalOffset < 0f)
                xPitch = Mathf.Clamp01(-verticalOffset / verticalTiltHalfHeight) * maxDownPitchAngle;
            else
                xPitch = -Mathf.Clamp01(verticalOffset / verticalTiltHalfHeight) * maxPitchAngle;
        }

        // Stab.prefab 基础旋转为 (90, 0, 0)。X 俯仰只根据出现后的上下移动叠加。
        _visualInstance.transform.rotation = Quaternion.Euler(90f + xPitch, 0f, -zRot);
    }

    private void UpdateSprite(float progress)
    {
        if (_sr == null) return;

        if (progress < 1f)
        {
            // 蓄力中：根据进度切换 charge1/2
            float mappedProgress = (progress - appearThreshold) / (1f - appearThreshold);
            _sr.sprite = mappedProgress < 0.65f ? chargeSprite1 : chargeSprite2;
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
}
