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
    }

    private void OnChargeUpdated(Vector2 screenPos, float progress)
    {
        if (!_isActive) return;

        if (progress >= appearThreshold)
        {
            if (!_hasAppeared)
            {
                _hasAppeared = true;
                CreateChargeVisual();
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

    private void CreateChargeVisual()
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
    }

    private void UpdatePosition(Vector2 screenPos)
    {
        if (_mainCam == null || _visualInstance == null) return;

        Vector3 playerPos = transform.position;
        float worldZ = playerPos.z + spawnZOffset;

        Vector3 worldPos = _mainCam.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, Mathf.Abs(worldZ - _mainCam.transform.position.z)));

        float clampedX = Mathf.Clamp(worldPos.x, playerPos.x - halfWidth, playerPos.x + halfWidth);
        _visualInstance.transform.position = new Vector3(clampedX, playerPos.y + spawnYOffset, worldZ);
    }

    private void UpdateRotation(Vector2 screenPos)
    {
        if (_mainCam == null || _visualInstance == null) return;

        Vector3 playerPos = transform.position;
        float worldZ = playerPos.z + spawnZOffset;

        Vector3 worldPos = _mainCam.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, Mathf.Abs(worldZ - _mainCam.transform.position.z)));

        float offsetX = Mathf.Clamp(worldPos.x - playerPos.x, -halfWidth, halfWidth);
        float zRot = (offsetX / halfWidth) * maxAngle;

        // Stab.prefab 基础旋转为 (90, 0, 0)，在此基础上叠加 Z 旋转
        _visualInstance.transform.rotation = Quaternion.Euler(90f, 0f, -zRot);
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
