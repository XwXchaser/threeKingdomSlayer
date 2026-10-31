using System.Collections;
    using UnityEngine;
    using UnityEngine.UI;
    
    /// <summary>
    /// 教学手势提示：手指图标 + 滑动拖尾 + 蓄力光圈三层，全部由代码驱动，不依赖动画片段。
    ///
    /// 约定（用户确认）：
    /// - 滑动时手指沿拖尾方向从**尾端走到箭头端**，手指**不旋转**；
    /// - 拖尾只按线段角度旋转、按线段长度拉伸，用来表达划动方向；
    /// - 蓄力时手指不动，光圈在实际位置上按进度**收缩**并缓慢旋转。
    /// 所有图层 raycastTarget=false，不会挡住玩家输入。
    /// </summary>
    public class TutorialGestureHint : MonoBehaviour
    {
        [Header("图层")]
        public RectTransform root;
        [Tooltip("手指图标：滑动时沿线段平移，不旋转")]
        public RectTransform finger;
        [Tooltip("滑动拖尾：按线段方向旋转并拉伸，箭头指向手势终点")]
        public Image trail;
        [Tooltip("蓄力光圈：原地收缩 + 旋转")]
        public Image ring;
        [Tooltip("暂停时的全屏遮罩（引导期间显示）")]
        public Image dimOverlay;
        [Tooltip("屏幕坐标换算用的容器（就是预制体根下的全屏 RectTransform）")]
        public RectTransform screenSpace;
    
    [Header("尺寸（设计像素，1080 宽口径）")]
    public float fingerSize = 308f;
    public float trailThickness = 67f;
    public float ringSize = 336f;
    [Range(0f, 0.3f)] public float trailInset = 0.06f;
    
    [Header("提示层压暗")]
    [Tooltip("提示层自带的全屏压暗透明度（0 = 不压暗）。\n" +
             "场景压暗（TutorialSceneDim）不作用于所有材质（SpriteRenderer 的 _Color 改不动、\n" +
             "自定义地形的 _TutorialDim 可能被别的脚本覆盖）→ 时停拍改用这一层保证「画面真的暗下来」。")]
    [Range(0f, 0.9f)] public float dimOverlayAlpha = 0.45f;

    [Header("点击手部帧（可选）")]
    [Tooltip("点击动作帧序列（按下 / 触底 / 回弹…）。**为空则退回程序化表现**。\n" +
             "帧的画风、尺寸必须与 tutorial_gesture_finger_ui 一致，且**指尖要停在同一像素位置**\n" +
             "（否则序列帧播放时指尖会跳 → 看上去是手在漂）。")]
    [SerializeField] private Sprite[] tapFrames = new Sprite[0];
    [Tooltip("每帧停留时长（秒）")]
    [SerializeField] private float tapFrameInterval = 0.08f;
    [Tooltip("程序化按压的角度（度）：手**绕指尖 pivot** 转 → 读作“手指按下去”。\n" +
             "（Finger 节点的 pivot 正好在指尖 (0.26, 0.81) → 旋转时指尖不动，只有手在压）")]
    [SerializeField] private float tapPressAngle = 14f;

    [Header("节奏")]
    public float tapDuration = 0.9f;
    public float swipeDuration = 1.1f;
    public float holdDuration = 1.2f;
    [Range(0.2f, 1f)] public float ringEndScale = 0.55f;
    public float ringSpinSpeed = 90f;

    [Header("招架/防御")]
    [Tooltip("招架示范的划动时长（秒）。必须贴近游戏里的划动门限（≥180px/s、≤0.25s），\n" +
             "示范画得慢 → 玩家学出来的手势在游戏里不成立。")]
    public float parrySwipeSeconds = 0.22f;
    [Tooltip("防御示范：抬手前需要按住多久才算「上划后按住」（与招架挥入 0.36s 对齐）。")]
    public float parryHoldSeconds = 0.35f;
    [Tooltip("冻结拍里一次示范循环的间隔（秒）。")]
    public float parryPlaySeconds = 0.9f;
    [Tooltip("防御示范里光圈脉动的速度（弧度/秒）。")]
    public float parryPulseSpeed = 4f;
    [Tooltip("防御示范里光圈脉动的幅度（0.12 = ±12%）。")]
    public float parryPulseAmount = 0.12f;
    
    private Coroutine _routine;
    private Canvas _canvas;
    private Sprite _fingerBaseSprite;

        [Header("引导门槛（做对才继续）")]
        [Tooltip("点击判定：按下到抬起的最大位移（像素）")]
        public float tapMaxMovePixels = 60f;
        [Tooltip("点击判定：最长按下时长（秒）")]
        public float tapMaxSeconds = 0.3f;
        [Tooltip("点击判定：必须点在提示位置附近的像素半径内")]
        public float tapRadiusPixels = 260f;
        [Tooltip("引导开始后的保护时间（秒）：这段时间内不判定，避免把对话的最后一击当成手势")]
        public float gateGraceSeconds = 0.5f;
        [Tooltip("滑动判定：最小位移（像素）")]
        public float swipeMinPixels = 90f;
        [Tooltip("蓄力判定：需要按住的秒数")]
        public float chargeHoldSeconds = 0.9f;
        [Tooltip("蓄力上挑判定：按住多久算「已蓄力」。<=0 时自动取 InputManager 的一级蓄力时长（与游戏实际手感对齐）")]
        public float chargeSwipeHoldSeconds = 0f;

    private bool _gateActive;
    private GestureHintKind _gateKind;
    private Vector2 _gateFrom;
    private Vector2 _gateTo;
    private System.Action _gateOnPass;
    private Coroutine _gateLoop;
    private float _prevTimeScale = 1f;
    private bool _pressing;
    private Vector2 _pressPos;
    private float _pressStart;
    private bool _holdSatisfied;
    private Vector2 _lastReleasePos;
    private bool _lastSwipeVertical;
    private float _gateStartTime;
    private int _gateColumn = -1;
    private float _gateHoldSeconds = 0.9f;
    /// <summary>正在跟踪的触摸 id（抬手那一帧 touchCount 会归零 → 必须自己记住 id 与位置）。</summary>
    private int _trackTouchId = -1;
    private Vector2 _trackTouchPos;
    
        /// <summary>
        /// 引导门槛：暂停游戏、循环播放手势示范，玩家真的做出这个手势才恢复并回调。
        /// 暂停用 timeScale=0，期间停掉 gameplay 输入，手势用原始输入自判（不受 timeScale 影响）。
        /// </summary>
        public void Gate(GestureHintKind kind, Vector2 fromScreenPos, Vector2 toScreenPos, System.Action onPass, int targetColumn = -1)
        {
            Cancel();
            _gateKind = kind;
            _gateFrom = fromScreenPos;
            _gateTo = toScreenPos;
            _gateOnPass = onPass;
        _gateColumn = targetColumn;
            _gateActive = true;
            _pressing = false;
            _holdSatisfied = false;
    
            _prevTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            _gateHoldSeconds = ResolveGateHoldSeconds(kind);
            if (InputManager.Instance != null)
            {
                InputManager.Instance.CancelCurrentGesture();
            }
            // 引导期间拦下战斗输入（只拦自己那一份，不影响别的 owner 的拦截）
            GameplayInputGate.RequestBlock(this, InputBlockReason.TutorialGate);
            Time.timeScale = 0f;
            // 台词本身已经在压暗屏幕；这时再叠我们的遮罩会明显更黑，所以只在没有台词时补一层
            bool dialogueDimming = DialogueManager.Instance != null &&
                                   (DialogueManager.Instance.IsPlaying || DialogueManager.Instance.IsBusy);
            if (dimOverlay != null) dimOverlay.gameObject.SetActive(!dialogueDimming);
            Debug.Log("[TutorialHint] 引导开始 kind=" + _gateKind + " 暂停=" + (Time.timeScale == 0f) + " 按住门槛=" + _gateHoldSeconds.ToString("F2") + "s 遮罩引用=" + (dimOverlay != null) + " 遮罩显示=" + (dimOverlay != null && dimOverlay.gameObject.activeSelf) + " 手指=" + (finger != null && finger.gameObject.activeSelf) + " 手位=" + (finger != null ? finger.anchoredPosition.ToString() : "-"));
            _gateLoop = StartCoroutine(GateLoop());
        }
    
        /// <summary>
        /// 门槛要求的「按住多长」。蓄力上挑必须与游戏里真正能进一级蓄力的时长一致，
        /// 否则玩家按游戏手感做动作、门槛却不认（这是上一版 0.9s 卡死的根因）。
        /// </summary>
        private float ResolveGateHoldSeconds(GestureHintKind kind)
        {
            if (kind != GestureHintKind.ChargedSwipe) return chargeHoldSeconds;
            if (chargeSwipeHoldSeconds > 0f) return chargeSwipeHoldSeconds;
            var input = InputManager.Instance;
            if (input != null && input.chargeLevelTimes != null && input.chargeLevelTimes.Length > 0)
                return Mathf.Max(0.15f, input.chargeLevelTimes[0]);
            return 0.35f;
        }

        public bool IsGating => _gateActive;
    
    private void Update()
    {
        if (!_gateActive) return;
        // 对话系统的 Finish() 会无条件把 timeScale 设回 1，会把引导的暂停顶掉。
        // 门槛期间每帧压住 0，直到玩家做对。
        if (Time.timeScale != 0f) Time.timeScale = 0f;
        PollRawInput();
    }
    
        private void Pass()
        {
            if (!_gateActive) return;
            _gateActive = false;
            // 停掉所有协程（示范循环 + 正在播的单击/滑动），否则收尾协程会把手重新显示出来
            StopAllCoroutines();
            _gateLoop = null;
            _routine = null;
            // 注意：这里不能关遮罩——引导期间每播完一轮示范都会走这里，关了遮罩就没了
            HideLayers();
            Time.timeScale = _prevTimeScale;
            if (InputManager.Instance != null)
            {
                InputManager.Instance.CancelCurrentGesture();
            }
            GameplayInputGate.Release(this);
            if (dimOverlay != null) dimOverlay.gameObject.SetActive(false);
            ReplayPlayerGesture();      // 把玩家做对的那一下真的打出去
            var cb = _gateOnPass;
            _gateOnPass = null;
            cb?.Invoke();
            Debug.Log("[TutorialHint] 引导通过，恢复 timeScale=" + Time.timeScale);
        }
    
        /// <summary>把玩家做对的那一下交给游戏，让它真的出招（否则教完什么都不发生）。</summary>
    private void ReplayPlayerGesture()
    {
        var input = InputManager.Instance;
        if (input == null) return;
        switch (_gateKind)
        {
            case GestureHintKind.Tap:
                input.ReplayGesture(MoveGesture.Tap, _lastReleasePos, 0, _gateColumn);
                break;
            case GestureHintKind.Hold:
                input.ReplayGesture(MoveGesture.Hold, _lastReleasePos, 1, _gateColumn);
                break;
            case GestureHintKind.Swipe:
                input.ReplayGesture(_lastSwipeVertical ? MoveGesture.SwipeVertical : MoveGesture.SwipeHorizontal,
                    _lastReleasePos, _holdSatisfied ? 1 : 0, _gateColumn);
                break;
            case GestureHintKind.ChargedSwipe:
                input.ReplayGesture(MoveGesture.SwipeVertical, _lastReleasePos, 1, _gateColumn);
                break;
        }
    }

    private IEnumerator GateLoop()
        {
            while (true)
            {
                switch (_gateKind)
                {
                    case GestureHintKind.Tap: PlayTap(_gateFrom); break;
                    case GestureHintKind.Hold: PlayHold(_gateFrom); break;
                    case GestureHintKind.Swipe: PlaySwipe(_gateFrom, _gateTo); break;
                    case GestureHintKind.ChargedSwipe: PlayChargedSwipe(_gateFrom, _gateTo); break;
                }
                float demoSeconds = _gateKind == GestureHintKind.Tap ? tapDuration + 0.5f
                              : _gateKind == GestureHintKind.Hold ? holdDuration + 0.5f
                              : _gateKind == GestureHintKind.Swipe ? swipeDuration + 0.5f
                              : holdDuration + swipeDuration + 0.5f;   // 蓄力上挑：先蓄后划，必须播完
            yield return new WaitForSecondsRealtime(demoSeconds);
            }
        }
    
    /// <summary>原始输入判定：不依赖 InputManager（它在 timeScale=0 时不工作）。</summary>
    private void PollRawInput()
    {
        bool down = false, held = false, up = false;
        Vector2 pos = Vector2.zero;

        if (Input.touchCount > 0)
        {
            var touch = Input.GetTouch(0);
            pos = touch.position;
            _trackTouchId = touch.fingerId;
            _trackTouchPos = touch.position;
            down = touch.phase == TouchPhase.Began;
            held = touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary;
            up = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
        }
        else if (_trackTouchId >= 0)
        {
            // **抬手那一帧 touchCount 已经归零** → 看不到 Ended 相位 → 自己补一次"抬起"
            // （不补的话触摸设备的划动永远不会被识别）
            pos = _trackTouchPos;
            up = true;
            _trackTouchId = -1;
        }
        else
        {
            pos = Input.mousePosition;
            down = Input.GetMouseButtonDown(0);
            held = Input.GetMouseButton(0) && !down;
            up = Input.GetMouseButtonUp(0);
        }

        if (down)
        {
            _pressing = true;
            _pressPos = pos;
            _pressStart = Time.unscaledTime;
            _holdSatisfied = false;
        }

        float heldSeconds = _pressing ? Time.unscaledTime - _pressStart : 0f;

        // 按住类：按够时间就算过（不需要抬手）
        if (_pressing && heldSeconds >= _gateHoldSeconds && Mathf.Abs(pos.y - _pressPos.y) < swipeMinPixels && Mathf.Abs(pos.x - _pressPos.x) < swipeMinPixels)
        {
            if (_gateKind == GestureHintKind.Hold) { _pressing = false; Pass(); return; }
            if (_gateKind == GestureHintKind.ChargedSwipe && !_holdSatisfied)
            {
                _holdSatisfied = true;
                Debug.Log("[TutorialHint] 蓄力已满足（按住 " + heldSeconds.ToString("F2") + "s），接着往上划");
            }
        }

        if (!up || !_pressing) return;

        Vector2 delta = pos - _pressPos;
        float duration = Time.unscaledTime - _pressStart;
        _pressing = false;

        // 记录这一下的真实落点/方向：通过后要按它重放，之前这两个字段从来没被赋过值
        _lastReleasePos = pos;
        _lastSwipeVertical = Mathf.Abs(delta.y) >= Mathf.Abs(delta.x);
    
            switch (_gateKind)
            {
                case GestureHintKind.Tap:
                {
                float dist = Vector2.Distance(pos, _gateFrom);
                bool ok = duration <= tapMaxSeconds && delta.magnitude <= tapMaxMovePixels && dist <= tapRadiusPixels;
                if (ok) Pass();
                else Debug.Log($"[TutorialHint] 点没通过：距离={dist:F0}px(需≤{tapRadiusPixels:F0}) 时长={duration:F2}s(需≤{tapMaxSeconds:F2}) 位移={delta.magnitude:F0}px(需≤{tapMaxMovePixels:F0})");
            }
                    break;
    
                case GestureHintKind.Swipe:
                    if (delta.magnitude >= swipeMinPixels && DirectionMatches(delta)) Pass();
                    break;
    
                case GestureHintKind.ChargedSwipe:
                    if (_holdSatisfied && delta.magnitude >= swipeMinPixels && delta.y > 0f && Mathf.Abs(delta.y) >= Mathf.Abs(delta.x)) Pass();
                    else Debug.Log($"[TutorialHint] 蓄力上挑没通过：蓄力={_holdSatisfied}（需按住≥{_gateHoldSeconds:F2}s） 位移={delta.magnitude:F0}px（需≥{swipeMinPixels:F0}） dy={delta.y:F0} dx={delta.x:F0}");
                    break;
    
                case GestureHintKind.Hold:
                    if (duration >= chargeHoldSeconds) Pass();
                    break;
            }
        }
    
    private bool DirectionMatches(Vector2 delta)
    {
        Vector2 expected = _gateTo - _gateFrom;
        if (expected.sqrMagnitude < 1f) return true;
        return Vector2.Dot(delta.normalized, expected.normalized) > 0.6f;
    }
    
    private void Awake()
    {
        if (root == null) root = transform as RectTransform;
        _canvas = GetComponentInParent<Canvas>();
        if (screenSpace == null) screenSpace = root;
        var fingerImage = FingerImage;
        if (fingerImage != null) _fingerBaseSprite = fingerImage.sprite;
        EnsureDimSprite();
        HideLayers();
    }
    
        /// <summary>只隐藏图层，不动协程（协程自己收尾时用）。</summary>
        public void Hide()
        {
            HideLayers();
        }
    
        /// <summary>外部打断：停协程 + 隐藏。</summary>
        public void Cancel()
        {
            // 外部打断时如果正处在引导门槛里，必须把暂停恢复，否则整局卡在 timeScale=0
            if (_gateActive)
            {
                _gateActive = false;
                if (_gateLoop != null) { StopCoroutine(_gateLoop); _gateLoop = null; }
                Time.timeScale = _prevTimeScale > 0f ? _prevTimeScale : 1f;
                // 旧实现在这里写 gameplayInputEnabled = true（全局解锁）；现在只释放自己那一份拦截。
                GameplayInputGate.Release(this);
                _gateOnPass = null;
            }
            if (dimOverlay != null) dimOverlay.gameObject.SetActive(false);
            StopRoutine();
            HideLayers();
        }
    
    public void PlayTap(Vector2 screenPos)
    {
        StartTap(screenPos, loop: false);
    }

    /// <summary>
    /// 点击示范（**循环**）：教学拍需要“手反复点那个按钮”，不能只播一下就消失。
    /// </summary>
    public void PlayTapLoop(Vector2 screenPos)
    {
        StartTap(screenPos, loop: true);
    }

    /// <summary>
    /// **按住保持**示范（循环）：防御键的交互是 hold（按住=防御中，松开=退出），
    /// 所以示范应该是“手指按下去就不起来”，而不是反复点。
    /// </summary>
    public void PlayPressHoldLoop(Vector2 screenPos)
    {
        StopRoutine();
        ShowLayers(ring: false, trail: false, finger: true);
        Place(finger, screenPos, fingerSize);
        SetAlpha(FingerImage, 1f);
        _routine = StartCoroutine(PressHoldLoopRoutine(screenPos));
    }

    private IEnumerator PressHoldLoopRoutine(Vector2 screenPos)
    {
        // 0.25s 内按下去（缩到 0.88），然后一直按住，只做轻微呼吸
        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / 0.25f);
            finger.localScale = Vector3.one * Mathf.Lerp(1f, 0.88f, k * k);
            yield return null;
        }
        while (true)
        {
            float pulse = (Mathf.Sin(Time.unscaledTime * 4.2f) + 1f) * 0.5f;   // 0..1
            finger.localScale = Vector3.one * Mathf.Lerp(0.88f, 0.92f, pulse);
            yield return null;
        }
    }

    private void StartTap(Vector2 screenPos, bool loop)
    {
        StopRoutine();   // 只停上一个示范协程，不能动引导本身
        ShowLayers(ring: false, trail: false, finger: true);
        Place(finger, screenPos, fingerSize);
        // **光圈必须先摆位**：不摆的话它会停在 anchoredPosition(0,0) = 屏幕正中，
        // 与手指尖分开（2026-10-29 实测：表现为「tap 只有屏幕中央一个光圈」）。
        Place(ring.rectTransform, screenPos, ringSize);
        SetAlpha(FingerImage, 1f);
        _routine = StartCoroutine(TapRoutine(loop));
    }
    
        /// <summary>普通滑动：拖尾铺在线段上，手指从尾端走到箭头端，不旋转。</summary>
        public void PlaySwipe(Vector2 fromScreenPos, Vector2 toScreenPos)
        {
            StopRoutine();   // 只停上一个示范协程，不能动引导本身
            ShowLayers(ring: false, trail: true, finger: true);
            LayoutTrail(fromScreenPos, toScreenPos);
            SetAlpha(trail, 1f);
            SetAlpha(FingerImage, 1f);
            _routine = StartCoroutine(SwipeRoutine(fromScreenPos, toScreenPos, 0f));
        }
    
        /// <summary>蓄力：手指与光圈原地，光圈收缩并旋转。</summary>
        public void PlayHold(Vector2 screenPos)
        {
            StopRoutine();   // 只停上一个示范协程，不能动引导本身
            ShowLayers(ring: true, trail: false, finger: true);
            Place(finger, screenPos, fingerSize);
            Place(ring.rectTransform, screenPos, ringSize);
            ring.rectTransform.localScale = Vector3.one;
            SetAlpha(FingerImage, 1f);
            SetAlpha(ring, 1f);
            _routine = StartCoroutine(HoldRoutine());
        }
    
    /// <summary>显示提示层自带的全屏压暗（教学时停拍用；alpha &lt;= 0 = 不显示）。</summary>
    public void ShowDim(float alpha)
    {
        if (dimOverlay == null) return;
        EnsureDimSprite();
        var img = dimOverlay.GetComponent<Image>();
        if (img != null)
        {
            var c = img.color;
            c.a = Mathf.Clamp01(alpha);
            img.color = c;
        }
        dimOverlay.gameObject.SetActive(alpha > 0.001f);
    }

    /// <summary>
    /// 压暗层原本没给 sprite → **Unity 的 Image 没有 sprite 就完全不渲染**（实测：压暗层完全没显示，
    /// 连旧的「黑遮罩门槛」也一直是失效的）。这里运行时补一张 4×4 白图（不新增资产）。
    /// </summary>
    private void EnsureDimSprite()
    {
        if (dimOverlay == null) return;
        if (dimOverlay.sprite != null) return;
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        tex.hideFlags = HideFlags.HideAndDontSave;
        var colors = new Color[16];
        for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
        tex.SetPixels(colors);
        tex.Apply();
        var sp = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        sp.hideFlags = HideFlags.HideAndDontSave;
        dimOverlay.sprite = sp;
        dimOverlay.type = Image.Type.Simple;
    }

    /// <summary>隐藏提示层压暗。</summary>
    public void HideDim() => ShowDim(0f);

    /// <summary>蓄力上挑：先按住（光圈收缩 = 蓄力），再沿方向划出去。</summary>
    public void PlayChargedSwipe(Vector2 fromScreenPos, Vector2 toScreenPos)
    {
        StopRoutine();   // 只停上一个示范协程，不能动引导本身
        // 先播蓄力光圈：玩家要先看到「按住蓄力」，再看「上划」，否则学到的顺序是错的
        ShowLayers(ring: true, trail: false, finger: true);
        Place(finger, fromScreenPos, fingerSize);
        Place(ring.rectTransform, fromScreenPos, ringSize);
        ring.rectTransform.localScale = Vector3.one;
        ring.rectTransform.localRotation = Quaternion.identity;
        SetAlpha(FingerImage, 1f);
        SetAlpha(ring, 1f);
        _routine = StartCoroutine(ChargedSwipeRoutine(fromScreenPos, toScreenPos));
    }

    /// <summary>
    /// 招架示范：快速竖直上划。
    /// 时长取 <see cref="parrySwipeSeconds"/>（贴近游戏划动门限），拖尾沿段铺开。
    /// </summary>
    public void PlayParry(Vector2 fromScreenPos, Vector2 toScreenPos)
    {
        StopRoutine();
        ShowLayers(ring: false, trail: true, finger: true);
        LayoutTrail(fromScreenPos, toScreenPos);
        SetAlpha(trail, 1f);
        SetAlpha(FingerImage, 1f);
        _routine = StartCoroutine(ParrySwipeRoutine(fromScreenPos, toScreenPos, false));
    }

    /// <summary>
    /// 防御示范：快速上划 → 手指**停住** + 光圈**脉动**。
    /// 光圈刻意不收缩（收缩在项目里已经是「蓄力」的语义，这里要表达的是「按住别松手」）。
    /// </summary>
    public void PlayParryHold(Vector2 fromScreenPos, Vector2 toScreenPos)
    {
        StopRoutine();
        ShowLayers(ring: false, trail: true, finger: true);
        LayoutTrail(fromScreenPos, toScreenPos);
        SetAlpha(trail, 1f);
        SetAlpha(FingerImage, 1f);
        _routine = StartCoroutine(ParrySwipeRoutine(fromScreenPos, toScreenPos, true));
    }
    
        // ---------------------------------------------------------------- 协程
    
    private IEnumerator TapRoutine(bool loop = false)
    {
        do
        {
            // 点按表现：光圈从指尖扩散一次（表达“点在这”）
            if (ring != null)
            {
                ring.gameObject.SetActive(true);
                ring.rectTransform.localScale = Vector3.one * 0.5f;
                SetAlpha(ring, 0.9f);
            }

            if (tapFrames != null && tapFrames.Length > 0)
            {
                // ── 帧序列版：手的动作由帧承担（2026-10-29 新增）
                var image = FingerImage;
                for (int i = 0; i < tapFrames.Length; i++)
                {
                    if (image != null && tapFrames[i] != null) image.sprite = tapFrames[i];
                    yield return new WaitForSecondsRealtime(tapFrameInterval);
                }
                if (image != null && _fingerBaseSprite != null) image.sprite = _fingerBaseSprite;
            }
            else
            {
                // ── 无帧：程序化按压（**绕指尖 pivot 旋转** + 轻微压缩）
                // 手的 pivot 在指尖 → 旋转时指尖不动、只有手在压 → 读作“按下去”。
                float t = 0f;
                while (t < tapDuration)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(t / tapDuration);
                    // 前 35% 压下去，后 65% 回弹（sin 曲线：起手快、收尾稳）
                    float press = k < 0.35f
                        ? Mathf.SmoothStep(0f, 1f, k / 0.35f)
                        : 1f - Mathf.SmoothStep(0f, 1f, (k - 0.35f) / 0.65f);
                    finger.localRotation = Quaternion.Euler(0f, 0f, -tapPressAngle * press);
                    finger.localScale = Vector3.one * Mathf.Lerp(1f, 0.94f, press);
                    if (ring != null)
                    {
                        float ripple = Mathf.Clamp01(k / 0.6f);
                        ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.5f, ripple);
                        SetAlpha(ring, 0.9f * (1f - ripple));
                    }
                    yield return null;
                }
                finger.localScale = Vector3.one;
                finger.localRotation = Quaternion.identity;
            }

            if (!loop) { Finish(); yield break; }
            // 循环模式：停一拍再点下一次
            yield return new WaitForSecondsRealtime(0.35f);
        }
        while (loop);
    }
    
        private IEnumerator SwipeRoutine(Vector2 from, Vector2 to, float leadIn)
        {
            if (leadIn > 0f) yield return new WaitForSecondsRealtime(leadIn);
            finger.localScale = Vector3.one;
            LayoutTrail(from, to);
            SetAlpha(trail, 1f);
    
            float t = 0f;
            while (t < swipeDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / swipeDuration);
                Vector2 pos = Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, k));
                Place(finger, pos, fingerSize);
                finger.localRotation = Quaternion.identity;   // 手指不旋转
                float fade = k < 0.85f ? 1f : Mathf.InverseLerp(1f, 0.85f, k);
                SetAlpha(trail, fade);
                SetAlpha(FingerImage, fade);
                yield return null;
            }
            Finish();
        }
    
        private IEnumerator HoldRoutine()
        {
            float t = 0f;
            float angle = 0f;
            while (t < holdDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / holdDuration);
                ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, ringEndScale, k);
                angle += ringSpinSpeed * Time.unscaledDeltaTime;
                ring.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
                yield return null;
            }
            Finish();
        }
    
        private IEnumerator ChargedSwipeRoutine(Vector2 from, Vector2 to)
        {
            // 第一段：按住蓄力（光圈原地收缩 + 旋转），这一段要看得清，玩家才知道先 hold
            float hold = Mathf.Max(holdDuration, 0.8f);
            float t = 0f;
            float angle = 0f;
            while (t < hold)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / hold);
                if (ring != null)
                {
                    ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, ringEndScale, k);
                    angle += ringSpinSpeed * Time.unscaledDeltaTime;
                    ring.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
                }
                yield return null;
            }

            // 第二段：光圈收起，手沿方向划出去（拖尾）
            if (ring != null) ring.gameObject.SetActive(false);
            LayoutTrail(from, to);
            SetAlpha(trail, 1f);
            SetAlpha(FingerImage, 1f);
            finger.localScale = Vector3.one;
    
            float s = 0f;
            while (s < swipeDuration)
            {
                s += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(s / swipeDuration);
                Vector2 pos = Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, k));
                Place(finger, pos, fingerSize);
                finger.localRotation = Quaternion.identity;
                float fade = k < 0.85f ? 1f : Mathf.InverseLerp(1f, 0.85f, k);
                SetAlpha(trail, fade);
                SetAlpha(FingerImage, fade);
                yield return null;
            }
            Finish();
        }
    
    // ---------------------------------------------------------------- 工具

    /// <summary>
    /// 招架/防御示范：
    ///   • 招架（holdAtEnd=false）：**上↔下来回划**（循环），让玩家一眼看到「在上划」；
    ///   • 防御（holdAtEnd=true）：上划一次 → 手指停住 + 光圈脉动（表达「按住别松手」）。
    /// 拖尾每次划完淡出，**图层不隐藏**（手指常驻）→ 玩家任何时候都知道要在哪儿划。
    /// </summary>
    private IEnumerator ParrySwipeRoutine(Vector2 from, Vector2 to, bool holdAtEnd)
    {
        float dur = Mathf.Max(0.06f, parrySwipeSeconds);

        while (true)
        {
            // ── 划过去 ──
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                Vector2 pos = Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, k));
                Place(finger, pos, fingerSize);
                finger.localRotation = Quaternion.identity;
                SetAlpha(trail, 1f);
                SetAlpha(FingerImage, 1f);
                yield return null;
            }

            // ── 拖尾淡出（手指留在终点）──
            float fade = 1f;
            while (fade > 0f)
            {
                fade -= Time.unscaledDeltaTime * 5f;
                SetAlpha(trail, Mathf.Clamp01(fade));
                yield return null;
            }

            if (holdAtEnd)
            {
                // 停住 + 光圈脉动（不收缩：收缩 = 蓄力的语义，这里是「按住别松」）
                if (ring != null)
                {
                    ring.gameObject.SetActive(true);
                    Place(ring.rectTransform, to, ringSize);
                    ring.rectTransform.localScale = Vector3.one;
                    ring.rectTransform.localRotation = Quaternion.identity;
                    SetAlpha(ring, 1f);
                }
                float pulse = 0f;
                while (true)
                {
                    pulse += Time.unscaledDeltaTime * parryPulseSpeed;
                    float s = 1f + parryPulseAmount * Mathf.Sin(pulse);
                    if (ring != null) ring.rectTransform.localScale = Vector3.one * s;
                    yield return null;
                }
            }

            // ── 划回来（再循环）──
            t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                Vector2 pos = Vector2.Lerp(to, from, Mathf.SmoothStep(0f, 1f, k));
                Place(finger, pos, fingerSize);
                finger.localRotation = Quaternion.identity;
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.2f);
        }
    }

    private Image FingerImage => finger != null ? finger.GetComponent<Image>() : null;
    
        private void Finish()
        {
            _routine = null;
            HideLayers();
        }
    
        private void StopRoutine()
        {
            if (_routine != null) { StopCoroutine(_routine); _routine = null; }
        }
    
        private void HideLayers()
        {
            if (finger != null) finger.gameObject.SetActive(false);
            if (trail != null) trail.gameObject.SetActive(false);
            if (ring != null) ring.gameObject.SetActive(false);
        }
    
        private void ShowLayers(bool ring, bool trail, bool finger)
        {
            if (this.ring != null) this.ring.gameObject.SetActive(ring);
            if (this.trail != null) this.trail.gameObject.SetActive(trail);
            if (this.finger != null) this.finger.gameObject.SetActive(finger);
        }
    
        private void LayoutTrail(Vector2 from, Vector2 to)
        {
            if (trail == null) return;
            trail.gameObject.SetActive(true);
            Vector2 delta = to - from;
            float length = Mathf.Max(1f, delta.magnitude) * (1f - trailInset);
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            Place(trail.rectTransform, (from + to) * 0.5f, trailThickness);
            trail.rectTransform.sizeDelta = new Vector2(length, trailThickness);
            trail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            trail.preserveAspect = false;
        }
    
        private void Place(RectTransform target, Vector2 screenPos, float size)
        {
            if (target == null) return;
            Camera cam = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null;
            Vector2 local;
            if (screenSpace != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(screenSpace, screenPos, cam, out local))
                target.anchoredPosition = local;
            target.sizeDelta = new Vector2(size, size);
        }
    
        private static void SetAlpha(Image image, float alpha)
        {
            if (image == null) return;
            var c = image.color;
            c.a = alpha;
            image.color = c;
        }
    }
    

