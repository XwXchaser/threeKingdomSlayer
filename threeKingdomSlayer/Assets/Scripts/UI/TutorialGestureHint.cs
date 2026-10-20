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
        public float fingerSize = 220f;
        public float trailThickness = 48f;
        public float ringSize = 240f;
        [Range(0f, 0.3f)] public float trailInset = 0.06f;
    
        [Header("节奏")]
        public float tapDuration = 0.9f;
        public float swipeDuration = 1.1f;
        public float holdDuration = 1.2f;
        [Range(0.2f, 1f)] public float ringEndScale = 0.55f;
        public float ringSpinSpeed = 90f;
    
        private Coroutine _routine;
        private Canvas _canvas;

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
                InputManager.Instance.gameplayInputEnabled = false;
            }
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
                InputManager.Instance.gameplayInputEnabled = true;
            }
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
                down = touch.phase == TouchPhase.Began;
                held = touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary;
                up = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
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
                if (InputManager.Instance != null) InputManager.Instance.gameplayInputEnabled = true;
                _gateOnPass = null;
            }
            if (dimOverlay != null) dimOverlay.gameObject.SetActive(false);
            StopRoutine();
            HideLayers();
        }
    
        public void PlayTap(Vector2 screenPos)
        {
            StopRoutine();   // 只停上一个示范协程，不能动引导本身
            ShowLayers(ring: false, trail: false, finger: true);
            Place(finger, screenPos, fingerSize);
            SetAlpha(FingerImage, 1f);
            _routine = StartCoroutine(TapRoutine());
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
    
        // ---------------------------------------------------------------- 协程
    
        private IEnumerator TapRoutine()
        {
            // 点按表现：手指往下压一下，同时光圈从指尖扩散消失，明确表达“点这里”
            if (ring != null)
            {
                ring.gameObject.SetActive(true);
                ring.rectTransform.localScale = Vector3.one * 0.5f;
                SetAlpha(ring, 0.9f);
            }
            float t = 0f;
            while (t < tapDuration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / tapDuration);
                float press = k < 0.35f ? Mathf.Lerp(1f, 0.88f, k / 0.35f) : Mathf.Lerp(0.88f, 1f, (k - 0.35f) / 0.65f);
                finger.localScale = Vector3.one * press;
                if (ring != null)
                {
                    float ripple = Mathf.Clamp01(k / 0.6f);
                    ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.5f, ripple);
                    SetAlpha(ring, 0.9f * (1f - ripple));
                }
                yield return null;
            }
            finger.localScale = Vector3.one;
            Finish();
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
    

