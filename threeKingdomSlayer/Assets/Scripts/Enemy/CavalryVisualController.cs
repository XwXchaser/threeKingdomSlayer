using TMPro;
using UnityEngine;

/// <summary>
/// 109骑兵视觉路由。
/// 骑乘动作使用根Animator上的人马合一Sprite；成功刺击与打断收招各有独立转身路由，返航奔跑尚未入库。
/// 动画速率适配骑兵玩法时长，真正落马后切换到正式1011控制器。
/// </summary>
[RequireComponent(typeof(CavalryEnemy), typeof(Enemy), typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class CavalryVisualController : MonoBehaviour
{
    [Header("旧占位坐骑层")]
    [SerializeField] public Transform mountAnchor;
    [SerializeField] public SpriteRenderer mountRenderer;
    [SerializeField] private Sprite mountIdle;
    [SerializeField] private Sprite mountWalk1;
    [SerializeField] private Sprite mountWalk2;
    [SerializeField] private Transform riderAnchor;
    [SerializeField] private SpriteRenderer riderRenderer;
    [SerializeField] private SpriteRenderer sourceRenderer;

    [Header("109专用Animator路由")]
    [Tooltip("人马合一骑乘控制器，由骑兵阶段驱动；落马后独立切换到1011。")]
    [SerializeField] private RuntimeAnimatorController mountedCombinedAnimatorController;
    [Tooltip("动作Clip缺失时的骑乘视觉回退；当前正式Prefab已为主要骑乘阶段提供合成动画。")]
    [SerializeField] private RuntimeAnimatorController mountedPlaceholderAnimatorController;
    [Tooltip("永久落马后使用的正式1011普通敌人控制器。")]
    [SerializeField] private RuntimeAnimatorController dismountedAnimatorController;

    [Header("正式骑乘动画（Inspector直接引用）")]
    [SerializeField] private AnimationClip windupAnimation;
    [SerializeField] private AnimationClip chargingAnimation;
    [SerializeField] private AnimationClip strikeAnimation;
    [Tooltip("成功刺击后收枪转身，在现有返航移动时长内播放一次；受阻暂停，被打断时不共用成功动作。")]
    [SerializeField] private AnimationClip strikeToRetreatAnimation;
    [Tooltip("打断急停后回正、收枪并向左转至背面，只在实际需要返航时播放；不共用成功刺击动作。")]
    [SerializeField] private AnimationClip interruptedToRetreatAnimation;
    [SerializeField] private AnimationClip turnToIdleAnimation;
    [Tooltip("返航后退时的循环奔跑动画（6 帧，一个步态周期 0.5s）。")]
    [SerializeField] private AnimationClip retreatPoseAnimation;
    [Tooltip("返航受阻（停在原地）时的背向姿势；留空则回退到奔跑动画。")]
    [SerializeField] private AnimationClip retreatBlockedPoseAnimation;
    [Tooltip("返航奔跑动画播放速度倍率（1 = 按源片 0.5s 一个步态周期）。")]
    [SerializeField, Min(0.05f)] private float retreatRunSpeed = 1f;
    [Tooltip("冲锋被打断后的受击急停动画，按现有0.20/0.22秒收招时间调速，不提交伤害。")]
    [SerializeField] private AnimationClip interruptedAnimation;
    [Tooltip("打开后在命中帧校验通过时输出一行日志（不一致时始终告警）。")]
    [SerializeField] private bool showStrikeHitDebug = false;
    [Tooltip("返航到位后转回Idle的播放时长，自动限制在现有冲锋冷却内，不延长玩法等待。")]
    [SerializeField, Min(0f)] private float turnToIdleDuration = 0.8f;

    [Header("调试与旧占位参数")]
    [SerializeField] private TextMeshPro debugLabel;
    [SerializeField] private bool showDebugState = true;
    [SerializeField] private Vector3 mountedRiderOffset = new Vector3(0f, 3.5f, -0.04f);
    [SerializeField] private Vector3 dismountedRiderOffset = Vector3.zero;
    [SerializeField] private float walkFrameDuration = 0.12f;

    public enum VisualMode
    {
        MountedCombinedIdle,
        MountedPlaceholder,
        Dismounted,
        MountedCombined
    }

    private CavalryEnemy _cavalry;
    private Enemy _enemy;
    private Animator _animator;
    private SpriteRenderer _rootRenderer;
    private CavalryEnemy.CavalryPhase _lastPhase;
    private int _lastRow = int.MinValue;
    private float _nextFrame;
    private bool _walkFlip;
    private VisualMode _lastVisualMode = (VisualMode)(-1);
    private CavalryEnemy.AnimationStage _lastAnimationStage = (CavalryEnemy.AnimationStage)(-1);
    private bool _lastMoving;
    private bool _turnPlaying;
    private float _turnElapsed;
    private float _turnDuration;
    // 两条转身路由共用计时，素材分别由Inspector直接引用。
    private bool _retreatTurnPlaying;
    private bool _retreatTurnFromInterrupt;
    private float _retreatTurnElapsed;
    private float _retreatTurnDuration;
    private float _retreatTurnSpeed;
    private float _activeSpeed = 1f;
    private bool _strikeHitVerified;

    private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");
    private static readonly int WindupState = Animator.StringToHash("Base Layer.Windup");
    private static readonly int ChargingState = Animator.StringToHash("Base Layer.Charging");
    private static readonly int AttackState = Animator.StringToHash("Base Layer.Attack");
    private static readonly int StrikeToRetreatState = Animator.StringToHash("Base Layer.StrikeToRetreat");
    private static readonly int InterruptedToRetreatState = Animator.StringToHash("Base Layer.InterruptedToRetreat");
    private static readonly int InterruptedState = Animator.StringToHash("Base Layer.Interrupted");
    private static readonly int RetreatingState = Animator.StringToHash("Base Layer.Retreating");
    private static readonly int RetreatBlockedState = Animator.StringToHash("Base Layer.RetreatBlocked");
    private static readonly int TurnToIdleState = Animator.StringToHash("Base Layer.TurnToIdle");

    public bool OwnsMountedAnimator => _cavalry != null && _cavalry.IsMounted
        && mountedCombinedAnimatorController != null
        && (GetVisualMode() == VisualMode.MountedCombinedIdle || GetVisualMode() == VisualMode.MountedCombined);

    /// <summary>
    /// 刺击命中帧在 Attack clip 上的归一化位置。
    /// 来源是 CavalryEnemy.strikeHitFrame.hitTime（与 1011 的 hitFrames 同口径），
    /// 不再手填，避免改帧序后又错位。
    /// </summary>
    public float StrikeHitNormalized
    {
        get
        {
            if (strikeAnimation == null || strikeAnimation.length <= 0f) return 0.5f;
            CavalryEnemy cavalry = _cavalry != null ? _cavalry : (_cavalry = GetComponent<CavalryEnemy>());
            if (cavalry == null) return 0.5f;
            float hitTime = cavalry.StrikeHitTime;
            if (hitTime <= 0f) return 0.5f;
            return Mathf.Clamp(hitTime / strikeAnimation.length, 0.01f, 0.99f);
        }
    }

    /// <summary>骑乘刺击的 Attack clip 是否已播到末尾（供玩法侧确认整个攻击动作演完）。</summary>
    public bool IsMountedAttackClipFinished
    {
        get
        {
            if (_animator == null) return true;
            if (_animator.IsInTransition(0)) return false;
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.fullPathHash != AttackState) return true;   // 已切走，不再等
            return stateInfo.normalizedTime >= 0.999f;
        }
    }

    /// <summary>Enemy的Idle/Attack入口不能覆盖骑兵正在播放的阶段或返航到位转身。</summary>
    public bool TryRefreshMountedAnimation()
    {
        if (!OwnsMountedAnimator) return false;
        Refresh(_cavalry);
        return true;
    }

    private void Awake()
    {
        _cavalry = GetComponent<CavalryEnemy>();
        _enemy = GetComponent<Enemy>();
        _animator = GetComponent<Animator>();
        _rootRenderer = sourceRenderer != null ? sourceRenderer : GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        _cavalry ??= GetComponent<CavalryEnemy>();
        _enemy ??= GetComponent<Enemy>();
        _animator ??= GetComponent<Animator>();
        _rootRenderer ??= sourceRenderer != null ? sourceRenderer : GetComponent<SpriteRenderer>();
        if (_cavalry != null) _cavalry.OnStateChanged += Refresh;
        _lastRow = int.MinValue;
        _lastVisualMode = (VisualMode)(-1);
        _lastAnimationStage = (CavalryEnemy.AnimationStage)(-1);
        _lastMoving = false;
        _turnPlaying = false;
        _turnElapsed = 0f;
        _retreatTurnPlaying = false;
        _retreatTurnFromInterrupt = false;
        _retreatTurnElapsed = 0f;
        _strikeHitVerified = false;
        Refresh(_cavalry);
    }

    private void OnDisable()
    {
        if (_cavalry != null) _cavalry.OnStateChanged -= Refresh;
    }

    private void LateUpdate()
    {
        if (_cavalry == null || _enemy == null || _enemy.state == EnemyState.Dead) return;

        var phase = _cavalry.Phase;
        var mode = GetVisualMode();
        bool moving = _enemy.IsRushMovementActive;
        if (_lastVisualMode != mode || _lastRow != _enemy.rowIndex || _lastPhase != phase
            || _lastAnimationStage != _cavalry.CurrentAnimationStage || _lastMoving != moving)
            Refresh(_cavalry);

        if (mode == VisualMode.MountedPlaceholder)
        {
            MirrorPlaceholderRider();
            UpdatePlaceholderMount(phase);
        }
        else if (OwnsMountedAnimator)
        {
            // Enemy的普通移动收尾可能重置speed；骑乘阶段保持自己的播放速率。
            if (_animator != null) _animator.speed = _activeSpeed;
            if (_retreatTurnPlaying && !_enemy.IsHitStopped
                && _cavalry.CurrentAnimationStage != CavalryEnemy.AnimationStage.RetreatBlocked)
            {
                _retreatTurnElapsed += Time.deltaTime;
                if (_retreatTurnElapsed >= _retreatTurnDuration)
                {
                    _retreatTurnPlaying = false;
                    PlayRetreatPose(_cavalry.CurrentAnimationStage);
                }
            }
            if (_turnPlaying && !_enemy.IsHitStopped)
            {
                _turnElapsed += Time.deltaTime;
                if (_turnElapsed >= _turnDuration)
                {
                    _turnPlaying = false;
                    PlayMounted(IdleState, null, 0f);
                }
            }
        }
    }

    private VisualMode GetVisualMode()
    {
        if (_cavalry == null || !_cavalry.IsMounted)
            return VisualMode.Dismounted;
        if (mountedCombinedAnimatorController == null) return VisualMode.MountedPlaceholder;
        if (_cavalry.Phase == CavalryEnemy.CavalryPhase.MountedIdle)
            return VisualMode.MountedCombinedIdle;
        return windupAnimation != null && chargingAnimation != null && strikeAnimation != null
            ? VisualMode.MountedCombined
            : VisualMode.MountedPlaceholder;
    }

    private void Refresh(CavalryEnemy cavalry)
    {
        if (cavalry == null || _enemy == null) return;

        var mode = GetVisualMode();
        bool modeChanged = _lastVisualMode != mode;
        if (modeChanged) ApplyVisualMode(mode);
        if (_enemy.state != EnemyState.Dead
            && (mode == VisualMode.MountedCombinedIdle || mode == VisualMode.MountedCombined))
            RefreshMountedStage(modeChanged);
        else if (mode == VisualMode.Dismounted || mode == VisualMode.MountedPlaceholder)
        {
            _lastAnimationStage = cavalry.CurrentAnimationStage;
            _lastMoving = _enemy.IsRushMovementActive;
        }
        _lastVisualMode = mode;
        _lastPhase = cavalry.Phase;
        _lastRow = _enemy.rowIndex;

        if (debugLabel != null)
        {
            debugLabel.gameObject.SetActive(showDebugState && mode != VisualMode.MountedCombinedIdle && mode != VisualMode.Dismounted);
            if (showDebugState)
            {
                debugLabel.text = $"109 #{_enemy.instanceId} {cavalry.Phase}\norigin={cavalry.ChargeOriginRow} grids={cavalry.ChargeGrids} dmg={cavalry.ChargeDamage:F0}\nrow={_enemy.rowIndex} {cavalry.LastReason}";
                debugLabel.color = !cavalry.IsMounted ? Color.red : cavalry.Phase == CavalryEnemy.CavalryPhase.Charging || cavalry.Phase == CavalryEnemy.CavalryPhase.Striking
                    ? new Color(1f, 0.55f, 0.1f) : cavalry.Phase == CavalryEnemy.CavalryPhase.Retreating || cavalry.Phase == CavalryEnemy.CavalryPhase.RetreatBlocked
                        ? Color.cyan : Color.yellow;
            }
        }
    }

    private void ApplyVisualMode(VisualMode mode)
    {
        switch (mode)
        {
            case VisualMode.MountedCombinedIdle:
            case VisualMode.MountedCombined:
                SetAnimatorController(mountedCombinedAnimatorController, false);
                if (_rootRenderer != null) _rootRenderer.enabled = true;
                if (mountAnchor != null) mountAnchor.gameObject.SetActive(false);
                if (riderRenderer != null) riderRenderer.enabled = false;
                break;

            case VisualMode.MountedPlaceholder:
                SetAnimatorController(mountedPlaceholderAnimatorController, true);
                if (_rootRenderer != null) _rootRenderer.enabled = false;
                if (mountAnchor != null) mountAnchor.gameObject.SetActive(true);
                if (riderAnchor != null) riderAnchor.localPosition = mountedRiderOffset;
                if (riderRenderer != null) riderRenderer.enabled = true;
                if (mountRenderer != null) mountRenderer.sprite = mountIdle;
                _walkFlip = false;
                _nextFrame = Time.time;
                break;

            case VisualMode.Dismounted:
                _turnPlaying = false;
                _retreatTurnPlaying = false;
                _retreatTurnFromInterrupt = false;
                _lastAnimationStage = (CavalryEnemy.AnimationStage)(-1);
                if (_animator != null) _animator.speed = 1f;
                SetAnimatorController(dismountedAnimatorController, _enemy != null && _enemy.state == EnemyState.Idle);
                if (_rootRenderer != null) _rootRenderer.enabled = true;
                if (mountAnchor != null) mountAnchor.gameObject.SetActive(false);
                if (riderRenderer != null) riderRenderer.enabled = false;
                if (riderAnchor != null) riderAnchor.localPosition = dismountedRiderOffset;
                break;
        }
    }

    private void RefreshMountedStage(bool force)
    {
        var stage = _cavalry.CurrentAnimationStage;
        bool moving = _enemy.IsRushMovementActive;
        if (!force && stage == _lastAnimationStage && moving == _lastMoving) return;

        var previousStage = _lastAnimationStage;
        bool returned = stage == CavalryEnemy.AnimationStage.Idle
            && (previousStage == CavalryEnemy.AnimationStage.Retreating
                || previousStage == CavalryEnemy.AnimationStage.RetreatBlocked);
        bool retreatStage = stage == CavalryEnemy.AnimationStage.Retreating
            || stage == CavalryEnemy.AnimationStage.RetreatBlocked;
        if (!retreatStage)
        {
            _retreatTurnPlaying = false;
            _retreatTurnFromInterrupt = false;
        }
        _lastAnimationStage = stage;
        _lastMoving = moving;
        _turnPlaying = false;

        switch (stage)
        {
            case CavalryEnemy.AnimationStage.Windup:
                PlayMounted(WindupState, windupAnimation, _cavalry.WindupDuration);
                break;
            case CavalryEnemy.AnimationStage.Charging:
                PlayMounted(ChargingState, chargingAnimation, _cavalry.ChargeStepDuration);
                break;
            case CavalryEnemy.AnimationStage.StrikeWindup:
                PlayMounted(AttackState, strikeAnimation, _cavalry.StrikeWindupDuration,
                    0f, StrikeHitNormalized);
                break;
            case CavalryEnemy.AnimationStage.StrikeRecover:
                // 先同步采样命中帧，再由CavalryEnemy提交伤害；收招不依赖动画结束事件。
                // Sprite键在浮点边界上可能仍采样前一帧，微小偏移确保提交当帧显示命中Sprite。
                PlayMounted(AttackState, strikeAnimation, _cavalry.StrikeRecoverDuration,
                    Mathf.Min(1f, StrikeHitNormalized + 0.00001f), 1f);
                VerifyStrikeHitSprite();
                break;
            case CavalryEnemy.AnimationStage.Interrupted:
                PlayMounted(InterruptedState, interruptedAnimation,
                    _cavalry.Phase == CavalryEnemy.CavalryPhase.Striking
                        ? _cavalry.StrikeRecoverDuration : _cavalry.InterruptRecoveryDuration);
                break;
            case CavalryEnemy.AnimationStage.Retreating:
            case CavalryEnemy.AnimationStage.RetreatBlocked:
                AnimationClip retreatTurnClip = previousStage == CavalryEnemy.AnimationStage.Interrupted
                    ? interruptedToRetreatAnimation
                    : previousStage == CavalryEnemy.AnimationStage.StrikeRecover ? strikeToRetreatAnimation : null;
                int returnRows = _cavalry.ChargeOriginRow - _enemy.rowIndex;
                if (retreatTurnClip != null && returnRows > 0 && _cavalry.RetreatTurnDuration > 0f)
                {
                    _retreatTurnFromInterrupt = previousStage == CavalryEnemy.AnimationStage.Interrupted;
                    // 转身时长与玩法侧保持一致：转身演完才后退（不再拉满整个返航过程）。
                    _retreatTurnDuration = Mathf.Max(0.02f, _cavalry.RetreatTurnDuration);
                    _retreatTurnElapsed = 0f;
                    _retreatTurnPlaying = true;
                    PlayMounted(_retreatTurnFromInterrupt ? InterruptedToRetreatState : StrikeToRetreatState,
                        retreatTurnClip, _retreatTurnDuration);
                    _retreatTurnSpeed = _activeSpeed;
                }
                if (_retreatTurnPlaying)
                {
                    _activeSpeed = stage == CavalryEnemy.AnimationStage.RetreatBlocked
                        ? 0f : _retreatTurnSpeed;
                    if (_animator != null) _animator.speed = _activeSpeed;
                }
                else
                {
                    PlayRetreatPose(stage);
                }
                // 受阻、移动标记及排号变化不重播转身；在原有返航时间内完成，不增加等待。
                break;
            default:
                if (moving && chargingAnimation != null)
                {
                    PlayMounted(ChargingState, chargingAnimation, _cavalry.ChargeStepDuration);
                }
                else if (returned && turnToIdleAnimation != null && turnToIdleDuration > 0f
                    && _cavalry.ChargeCooldown > 0f)
                {
                    _turnDuration = Mathf.Min(turnToIdleDuration, _cavalry.ChargeCooldown);
                    _turnElapsed = 0f;
                    _turnPlaying = true;
                    PlayMounted(TurnToIdleState, turnToIdleAnimation, _turnDuration);
                }
                else
                {
                    PlayMounted(IdleState, null, 0f);
                }
                break;
        }
    }

    /// <summary>
    /// 命中帧一致性自检：切到收尾段后，屏幕上真实显示的精灵必须是声明的命中帧。
    /// 不一致就告警（说明 strikeHitFrame.hitTime 与 clip 关键帧不一致），每次启用只报一次，不影响玩法。
    /// </summary>
    private void VerifyStrikeHitSprite()
    {
        if (_strikeHitVerified || _cavalry == null || !_cavalry.HasStrikeHitFrame) return;
        if (_enemy != null && _enemy.IsHitStopped) return;   // 卡肉时画面被冻结，等恢复后再校验
        _strikeHitVerified = true;

        string shown = _rootRenderer != null && _rootRenderer.sprite != null ? _rootRenderer.sprite.name : null;
        string declared = _cavalry.StrikeHitSpriteName;
        if (!AttackHitFrameResolver.MatchesSprite(declared, shown))
        {
            Debug.LogWarning($"[109] 刺击命中帧不一致：声明 {declared}，切分点实际显示 {shown}；" +
                             $"请核对 CavalryEnemy.strikeHitFrame.hitTime（当前 {_cavalry.StrikeHitTime:F4}s）与 Attack clip 关键帧");
        }
        else if (showStrikeHitDebug)
        {
            Debug.Log($"[109] 刺击命中帧校验通过：{shown} @ norm {StrikeHitNormalized:F4}");
        }
    }

    /// <summary>屏幕上当前是否正显示指定精灵（精确或后缀匹配，与 1011 命中帧同一口径）。</summary>
    public bool IsSpriteShowing(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName) || _rootRenderer == null || _rootRenderer.sprite == null) return false;
        return AttackHitFrameResolver.MatchesSprite(spriteName, _rootRenderer.sprite.name);
    }

    private void PlayRetreatPose(CavalryEnemy.AnimationStage stage)
    {
        if (_retreatTurnFromInterrupt && interruptedToRetreatAnimation != null)
        {
            // 循环尚未入库时保持该路由自己的末帧，不能切回颜色/姿态不同的成功转身尾态。
            PlayMounted(InterruptedToRetreatState, interruptedToRetreatAnimation, 0f, 1f, 1f);
            _activeSpeed = 0f;
            if (_animator != null) _animator.speed = 0f;
        }
        else
        {
            bool blocked = stage == CavalryEnemy.AnimationStage.RetreatBlocked;
            AnimationClip clip = blocked
                ? (retreatBlockedPoseAnimation != null ? retreatBlockedPoseAnimation : retreatPoseAnimation)
                : retreatPoseAnimation;
            PlayMounted(blocked ? RetreatBlockedState : RetreatingState, clip, 0f);
            if (!blocked && retreatRunSpeed > 0f)
            {
                _activeSpeed = retreatRunSpeed;
                if (_animator != null) _animator.speed = _activeSpeed;
            }
        }
    }

    private void PlayMounted(int state, AnimationClip clip, float duration,
        float fromNormalized = 0f, float toNormalized = 1f)
    {
        if (_animator == null) return;
        _activeSpeed = clip != null && duration > 0f
            ? clip.length * (toNormalized - fromNormalized) / duration : 1f;
        _animator.speed = _activeSpeed;
        _animator.ResetTrigger("Attack");
        _animator.ResetTrigger("Walk");
        _animator.ResetTrigger("Hit");
        _animator.Play(state, 0, fromNormalized);
        // Animator.Update(0)使阶段入口/伤害提交帧立即可见；卡肉期间保持已有冻结画面。
        if (_animator.enabled && !_enemy.IsHitStopped) _animator.Update(0f);
    }

    private void SetAnimatorController(RuntimeAnimatorController controller, bool playIdle)
    {
        if (_animator == null || controller == null) return;
        if (_animator.runtimeAnimatorController != controller)
        {
            _animator.runtimeAnimatorController = controller;
            _animator.Rebind();
            _animator.Update(0f);
        }
        if (playIdle)
            _animator.Play("Idle", 0, 0f);
    }

    private void MirrorPlaceholderRider()
    {
        if (riderRenderer == null || sourceRenderer == null) return;
        riderRenderer.sprite = sourceRenderer.sprite;
        riderRenderer.color = sourceRenderer.color;
        if (riderRenderer.sharedMaterial != sourceRenderer.sharedMaterial)
            riderRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
    }

    private void UpdatePlaceholderMount(CavalryEnemy.CavalryPhase phase)
    {
        if (mountRenderer == null || !_cavalry.IsMounted) return;
        bool walking = phase == CavalryEnemy.CavalryPhase.Charging || phase == CavalryEnemy.CavalryPhase.Windup || phase == CavalryEnemy.CavalryPhase.Retreating;
        if (walking && Time.time >= _nextFrame)
        {
            _walkFlip = !_walkFlip;
            mountRenderer.sprite = _walkFlip ? mountWalk1 : mountWalk2;
            _nextFrame = Time.time + Mathf.Max(0.03f, walkFrameDuration);
        }
        else if (!walking && mountRenderer.sprite != mountIdle)
        {
            mountRenderer.sprite = mountIdle;
        }
    }

    // ── 教学离场：淡出 ────────────────────────────────────

    private Color _mountColorOriginal;
    private Color _riderColorOriginal;
    private bool _fadeColorsCached;

    /// <summary>缓存马 / 骑手的原色（淡出前调一次）。</summary>
    public void CacheFadeColors()
    {
        if (mountRenderer != null) _mountColorOriginal = mountRenderer.color;
        if (riderRenderer != null) _riderColorOriginal = riderRenderer.color;
        _fadeColorsCached = true;
    }

    /// <summary>设置淡出透明度（0 = 完全透明，1 = 原色）。</summary>
    public void SetFadeAlpha(float alpha)
    {
        if (!_fadeColorsCached) CacheFadeColors();
        float a = Mathf.Clamp01(alpha);
        if (mountRenderer != null)
        {
            var c = _mountColorOriginal; c.a = _mountColorOriginal.a * a; mountRenderer.color = c;
        }
        if (riderRenderer != null)
        {
            var c = _riderColorOriginal; c.a = _riderColorOriginal.a * a; riderRenderer.color = c;
        }
    }

    /// <summary>还原原色（**回池前必须调**，否则下一次复用会隐形）。</summary>
    public void RestoreFadeAlpha()
    {
        if (!_fadeColorsCached) return;
        if (mountRenderer != null) mountRenderer.color = _mountColorOriginal;
        if (riderRenderer != null) riderRenderer.color = _riderColorOriginal;
        _fadeColorsCached = false;
    }
}
