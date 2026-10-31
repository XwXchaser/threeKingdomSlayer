using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// **防御按键**（2026-10-29 用户定）。
///
/// 口径：
///   • **hold 长按保持**：按住 → 防御开（长枪举起 → 落防御位）；**松开 → 防御关**（收枪出画）。
///     （不是点按切换 —— 点按切换会变成“按一下就一直防御”，玩家没法靠松手退出。）
///   • **按下立即发动**：不等手势、不等长按 → 按下就举枪。
///   • **图标亮度 = 能不能防御**：能防御（空闲 / 防御中）→ 亮；不能防御（**出招中**）→ 不亮。
///   • **不做招架判定**：没有接触帧、不反弹飞行物、敌人不会被打断
///     （手势招架已下线，见 <c>AttackSkillConfig.parryGestureEnabled</c>）。
///   • 防御期间的免伤与破防值由 <c>PlayerGuard</c> 负责。
///
/// 与手势输入的关系：
///   • 按住按钮的那根手指**不会被当成游戏手势** → 不会误触发蓄力 / 长按
///     （见 <c>InputManager.PickGameplayTouch</c>：游戏指针只取“不在 UI 上”的手指）。
///   • UI 点击不会变成游戏手势（<c>InputManager.IsPointerOverUIAt</c> 当场射线检测）。
///   • 防御与出招**互斥**：出招中不能防御（<c>PlayerState.CanStartDefend</c>），
///     防御中不能出招（<c>AttackSystem.TryExecuteAttack</c>）。
///
/// 这个组件只做「按钮 → 意图」的转发与视觉状态；真正的机制在
/// <c>PlayerState.SetDefend</c> → <c>PlayerWeaponStance.PlayDefend</c>。
/// </summary>
public class DefenseButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IActionButton
{
    public static DefenseButton Instance { get; private set; }

    [Header("引用")]
    [Tooltip("按钮本体（留空则取本组件所在对象）")]
    [SerializeField] private Button _button;
    [Tooltip("图标（盾牌面片）：切换时换色表示防御开/关")]
    [SerializeField] private Image _icon;

    [Header("配色")]
    [Tooltip("能防御时的图标色（露出原本的米白 + 铁灰对分）")]
    [SerializeField] private Color _availableColor = Color.white;
    [Tooltip("不能防御时的图标色（整图压暗到 60%）")]
    [SerializeField] private Color _unavailableColor = new Color(0.60f, 0.60f, 0.60f, 1f);

    [Header("开放时机")]
    [Tooltip("教学关：玩家点到教学拍之前，这个键是**锁住**的。\n" +
             "锁住 = 不可见 + blocksRaycasts 关闭 → 它既不会被点到，\n" +
             "也不会像普通 UI 那样**吞掉它下面的游戏手势**（InputManager.IsPointerOverUIAt 的规则）。")]
    [SerializeField] private bool _available = true;

    /// <summary>当前是否对玩家开放（锁住时不可见、也不吃点击）。</summary>
    public bool Available => _available;

    private CanvasGroup _canvasGroup;
    private Coroutine _popRoutine;
    /// <summary>当前是否**按着**（hold 语义：按住=防御中，松开=退出）。</summary>
    private bool _pressed;

    public Button Button => _button;
    public Image Icon => _icon;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (_button == null) _button = GetComponent<Button>();
        if (_icon == null) _icon = GetComponent<Image>();
        // 不用 Button.onClick：交互走 IPointerDown/Up（hold 语义），onClick 一律清掉避免双触发
        if (_button != null) _button.onClick.RemoveAllListeners();
        // CanvasGroup 与 Image 同节点 → 锁住时它自己不会被射线命中（IsPointerOverUIAt 会跳过它）
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        ApplyAvailability();
    }

    /// <summary>开放 / 锁住这个键（教学关按拍解锁）。</summary>
    public void SetAvailable(bool available)
    {
        if (_available == available) return;
        _available = available;
        ApplyAvailability();
        if (!_available) Release();
    }

    private void ApplyAvailability()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = _available ? 1f : 0f;
            _canvasGroup.blocksRaycasts = _available;
            _canvasGroup.interactable = _available;
        }
        if (_button != null) _button.interactable = _available;
    }

    private void OnEnable()
    {
        var player = PlayerState.Instance;
        if (player == null) return;
        player.OnDefendChanged += OnDefendChanged;
        RefreshAvailability();
    }

    private void OnDisable()
    {
        var player = PlayerState.Instance;
        if (player != null) player.OnDefendChanged -= OnDefendChanged;
        Release();   // HUD 被隐藏时别把防御卡在开启状态
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        // 图标亮度 = 能不能防御（出招中 → 不亮）
        RefreshAvailability();
    }

    // ── hold 语义 ───────────────────────────────────────────────

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_available) return;
        // 区域位置识别：点的地方下面有活着的敌人 → 那是攻击，不是防御 → 不接管
        if (eventData != null && ShouldYieldToCombat(eventData.position)) return;

        _pressed = true;
        PlayerState.Instance?.SetDefend(true);   // 按下立即发动；出招中会被 SetDefend 拒绝
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Release();
    }

    /// <summary>松开 → 退出防御。</summary>
    private void Release()
    {
        if (!_pressed) return;
        _pressed = false;
        PlayerState.Instance?.SetDefend(false);
    }

    /// <summary>
    /// 区域位置识别：点的地方**下面有活着的敌人** → 那是攻击，不是防御 → 不切换。
    /// （防御键就压在敌人所在的画面区域上，不能靠“UI 优先”把玩家打敌人的手势吃掉。）
    /// </summary>
    public bool ShouldYieldToCombat(Vector2 screenPos)
    {
        return EnemyManager.TryGetEnemyAtScreenPoint(screenPos, Camera.main, out _);
    }

    // ── 视觉状态 ───────────────────────────────────────────────

    private void OnDefendChanged(bool on)
    {
        RefreshAvailability();
        if (on) PlayPop();   // 防御中：弹入一下（仿刀锋“状态图标临时出现”的观感）
    }

    /// <summary>
    /// 刷新图标亮度：**能防御 → 亮**（空闲或防御中）；**不能防御 → 不亮**（出招中）。
    /// </summary>
    public void RefreshAvailability()
    {
        if (_icon == null) return;
        var player = PlayerState.Instance;
        bool canDefend = player != null && (player.IsDefendingActive || player.CanStartDefend());
        _icon.color = canDefend ? _availableColor : _unavailableColor;
    }

    /// <summary>防御开启的弹入动效：0.88 → 1.06 → 1.0（用协程，不挂 DOTween，避免残留 tween）。</summary>
    private void PlayPop()
    {
        if (_icon == null) return;
        if (_popRoutine != null) StopCoroutine(_popRoutine);
        _popRoutine = StartCoroutine(PopRoutine(_icon.rectTransform));
    }

    private System.Collections.IEnumerator PopRoutine(RectTransform target)
    {
        target.localScale = Vector3.one * 0.88f;
        float t = 0f;
        while (t < 0.07f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / 0.07f);
            target.localScale = Vector3.one * Mathf.Lerp(0.88f, 1.06f, 1f - Mathf.Pow(1f - k, 3f));
            yield return null;
        }
        t = 0f;
        while (t < 0.09f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / 0.09f);
            target.localScale = Vector3.one * Mathf.Lerp(1.06f, 1f, k * k * (3f - 2f * k));
            yield return null;
        }
        target.localScale = Vector3.one;
        _popRoutine = null;
    }
}
