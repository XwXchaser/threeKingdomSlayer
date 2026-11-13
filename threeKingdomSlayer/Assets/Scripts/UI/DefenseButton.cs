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

    /// <summary>
    /// **暂时静音**（教学进行中）：按钮**可见但压暗**，按下去**既不防御、也不出招**。
    ///
    /// 和 <see cref="_available"/> 是**两件不同的事**，别混：
    ///   • <c>_available = false</c> → **隐藏**（教学关还没教到格挡 → 干脆不给看，2026-10-01 用户定）
    ///   • <c>_muted = true</c>     → **压暗可见但无效**（教学进行中暂时不让用：
    ///                                3选1弹窗期间 / 招式解锁引导拍期间）
    ///
    /// 静音期间**仍然吃射线**（blocksRaycasts = true）→ 这一下被按钮吞掉，
    /// **不会漏成游戏手势**（否则玩家按“防御”反而打出一招）。
    /// </summary>
    private bool _muted;
    /// <summary>静音时是否仍然吃射线（吞掉这一下点击，不让它漏成游戏手势）。</summary>
    private bool _swallowClicks = true;

    /// <summary>当前是否处于「暂时静音」（压暗、按了没反应）。</summary>
    public bool Muted => _muted;

    private CanvasGroup _canvasGroup;
    private Coroutine _popRoutine;
    /// <summary>当前是否**按着**（hold 语义：按住=防御中，松开=退出）。</summary>
    private bool _pressed;
    /// <summary>
    /// **预输入**（2026-11-08 用户口径 4）：按了防御键，但**现在还不能防御**
    /// （出招动作锁 / 连段未结束）→ 记住这次按压，等**连段结束**自动转进防御。
    ///
    /// 取消只有两条路：松手、静音（见 <see cref="Release"/>）—— **不设超时**。
    /// </summary>
    private bool _pendingDefend;
    /// <summary>上一次看到的「防御被锁住」状态（变了才重刷表现）。</summary>
    private bool _lastDefenseLocked;
    /// <summary>招式状态机（判断「连段是不是还在进行」）。</summary>
    private PlayerMoveStateMachine _moveMachine;

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

    /// <summary>开放 / 锁住这个键（教学关按拍解锁；锁住 = 隐藏）。</summary>
    public void SetAvailable(bool available)
    {
        if (_available == available) return;
        _available = available;
        ApplyAvailability();
        if (!_available) Release();
    }

    /// <summary>
    /// **暂时静音**：可见但压暗，按下去既不防御也不出招（2026-11-05 用户定）。
    /// 与 <see cref="SetAvailable"/> 独立 → 静音的恢复**不依赖**「教到格挡没有」那套逻辑。
    /// </summary>
    /// <param name="muted">是否静音（压暗可见但不生效）。</param>
    /// <param name="swallowClicks">
    /// 静音时**是否仍然吃射线**（把这一下点击吞掉，不让它漏成游戏手势）。
    ///   • <c>true</c>：三选一面板那段 —— 玩家点防御键不该顺手打出一记攻击；
    ///   • <c>false</c>：**需要出招的教学拍** —— 否则玩家点在该区域 → 这一点被按钮吞掉 →
    ///     招式永远打不出来 → 这一拍收不了（2026-11-05 用户反馈「输入出错无法继续」）。
    /// </param>
    public void SetMuted(bool muted, bool swallowClicks = true)
    {
        if (_muted == muted && _swallowClicks == swallowClicks) return;
        _muted = muted;
        _swallowClicks = swallowClicks;
        ApplyAvailability();
        if (_muted) Release();   // 静音的瞬间把可能举着的枪收回去
    }

    private void ApplyAvailability()
    {
        if (_canvasGroup != null)
        {
            if (!_available)
            {
                // 隐藏（教学关还没教到格挡）
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }
            else if (_muted)
            {
                // 静音：**压暗但可见**；按不下去（interactable=false → 不派发事件 → 不会触发防御）。
                // **是否吃射线由调用方决定**（见 SetMuted 的参数说明）：
                //   需要出招的教学拍 → 不吃（玩家点在这一块仍然能出招，否则这一拍收不了）。
                _canvasGroup.alpha = 0.35f;
                _canvasGroup.blocksRaycasts = _swallowClicks;
                _canvasGroup.interactable = false;
            }
            else
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.interactable = true;
            }
            // 破防灰掉（2026-11-09）：可见但压暗、按不了；仍然吃射线（把这一下点击吞掉，不漏成游戏手势）。
            if (IsDefenseLockedNow())
            {
                _canvasGroup.alpha = Mathf.Min(_canvasGroup.alpha, 0.2f);
                _canvasGroup.interactable = false;
            }
        }
        if (_button != null) _button.interactable = _available && !_muted && !IsDefenseLockedNow();
    }

    private void OnEnable()
    {
        var player = PlayerState.Instance;
        if (player == null) return;
        player.OnDefendChanged += OnDefendChanged;
        if (player.Guard != null) player.Guard.OnStateChanged += OnGuardStateChanged;
        RefreshAvailability();
    }

    private void OnDisable()
    {
        var player = PlayerState.Instance;
        if (player != null)
        {
            player.OnDefendChanged -= OnDefendChanged;
            if (player.Guard != null) player.Guard.OnStateChanged -= OnGuardStateChanged;
        }
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
        // 预输入：连段结束 → 自动转进防御
        TryConsumePendingDefend();
        // 破防锁的状态变了 → 重刷表现（灰掉 / 恢复）
        bool locked = IsDefenseLockedNow();
        if (locked != _lastDefenseLocked)
        {
            _lastDefenseLocked = locked;
            ApplyAvailability();
            RefreshAvailability();
        }
    }

    /// <summary>防御是否被锁住（破防后的一段时间内）→ 防御键灰掉、按不了。</summary>
    private bool IsDefenseLockedNow()
    {
        var player = PlayerState.Instance;
        return player != null && player.Guard != null && player.Guard.IsDefenseLocked;
    }

    // ── hold 语义 ───────────────────────────────────────────────

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_available) return;
        if (_muted) return;                       // 静音：按下去**什么都不发生**（也不防御）
        if (IsDefenseLockedNow()) return;        // 破防锁：按不了（也不排队预输入）
        // 区域位置识别：点的地方下面有活着的敌人 → 那是攻击，不是防御 → 不接管
        if (eventData != null && ShouldYieldToCombat(eventData.position)) return;

        _pressed = true;
        var ps = PlayerState.Instance;
        if (ps == null) return;

        // **硬门**（游戏结束 / 三选一弹窗 / 输入门拦下）→ 拒绝（连预输入都不产生）
        if (!ps.CanAcceptDefendIntent()) return;

        if (ps.CanStartDefend())
        {
            ps.SetDefend(true);                   // 按下立即发动
            return;
        }

        // **软门**（出招动作锁 / 连段未结束）→ **预输入**：
        //   记住这次按压 → 连段结束的那一帧自动转进防御（见 TryConsumePendingDefend）。
        _pendingDefend = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Release();
    }

    /// <summary>松开 → 退出防御。</summary>
    private void Release()
    {
        // **预输入取消**（口径 5）：只在松手 / 静音 / 隐藏时取消 —— 不设超时。
        //   静音时也取消是必须的：教学拍一开始就把玩家之前挂着的防御意图清掉，
        //   否则那一拍要教的招式会被自动防御挡掉（BUG-009 同型）。
        _pendingDefend = false;
        if (!_pressed) return;
        _pressed = false;
        PlayerState.Instance?.SetDefend(false);
    }

    /// <summary>
    /// **预输入兑现**（2026-11-08 用户口径 4）：
    /// 玩家在「现在还不能防御」的时候按下了防御键 → 记住 → **连段结束**的那一帧自动进防御。
    ///
    /// 兑现条件 = 动作锁结束（<c>AttackSystem.IsActionPlaying</c>）**且** 连段走完
    /// （<c>PlayerMoveStateMachine.IsMoveActive</c>）—— 只等动作锁的话会把玩家正在打的连段砍掉。
    /// </summary>
    private void TryConsumePendingDefend()
    {
        if (!_pendingDefend || !_pressed) return;
        if (IsDefenseLockedNow()) return;        // 破防锁期间不许兑现（否则破防等于没破）
        var ps = PlayerState.Instance;
        if (ps == null || !ps.CanStartDefend()) return;
        if (IsComboStillRunning()) return;         // 连段还没结束 → 不兑现

        ps.SetDefend(true);
        // 只在**真的进防御了**才清（SetDefend 内部还会再查一次能不能防御）
        if (ps.IsDefendingActive) _pendingDefend = false;
    }

    /// <summary>
    /// 连段是不是还在进行（招式状态机还挂在当前节点上）。
    /// 取不到状态机就**不拦**（宁可早兑现，也不要让玩家的按压卡住）。
    /// </summary>
    private bool IsComboStillRunning()
    {
        if (_moveMachine == null)
        {
            var input = InputManager.Instance;
            if (input != null) _moveMachine = input.moveStateMachine;
            if (_moveMachine == null) _moveMachine = FindObjectOfType<PlayerMoveStateMachine>();
        }
        if (_moveMachine == null) return false;
        return _moveMachine.IsMoveActive;
    }

    /// <summary>
    /// 区域位置识别：点的地方**下面有活着的敌人** → 那是攻击，不是防御 → 不切换。
    /// （防御键就压在敌人所在的画面区域上，不能靠“UI 优先”把玩家打敌人的手势吃掉。）
    /// </summary>
    public bool ShouldYieldToCombat(Vector2 screenPos)
    {
        // 静音期间：这个键不接管任何点击 → **这一下也不能变成战斗手势**
        //（否则玩家按“防御”反而打出一招 —— 2026-11-05 用户反馈「按下去没有防御」）
        if (_muted) return false;
        return EnemyManager.TryGetEnemyAtScreenPoint(screenPos, Camera.main, out _);
    }

    // ── 视觉状态 ───────────────────────────────────────────────

    private void OnDefendChanged(bool on)
    {
        RefreshAvailability();
        if (on) PlayPop();   // 防御中：弹入一下（仿刀锋“状态图标临时出现”的观感）
    }

    /// <summary>
    /// 破防 → 防御键灰掉一段时间（2026-11-09 用户定）。
    ///
    /// 为什么必须做：破防硬直期间**玩法输入被门禁拦下**，但防御键是 **UI** → 玩家可以在硬直里按一下，
    /// 防御键的**预输入**会把这次按压排队 → 硬直一结束就**自动进防御** → 破防惩罚被抵消。
    /// 灰掉期间：**按不了、也不排队** → 玩家必须抬手重新按一次 → 防御不再是免费护盾。
    /// </summary>
    private void OnGuardStateChanged(PlayerGuard.GuardState state)
    {
        if (state == PlayerGuard.GuardState.Broken)
        {
            // 破防 → 立刻松开防御（否则 DefendHeld 一直为真 → 玩家不松手就出不了招）
            Release();
        }
        ApplyAvailability();
        RefreshAvailability();
    }

    /// <summary>
    /// 刷新图标亮度：**能防御 → 亮**（空闲或防御中）；**不能防御 → 不亮**（出招中）。
    /// </summary>
    public void RefreshAvailability()
    {
        if (_icon == null) return;
        var player = PlayerState.Instance;
        bool canDefend = player != null && (player.IsDefendingActive || player.CanStartDefend());
        _icon.color = (_muted || IsDefenseLockedNow() || !canDefend) ? _unavailableColor : _availableColor;
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
