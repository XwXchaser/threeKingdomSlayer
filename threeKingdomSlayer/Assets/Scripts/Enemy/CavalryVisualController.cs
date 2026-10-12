using TMPro;
using UnityEngine;

/// <summary>
/// 109骑兵视觉路由。
/// 已制作的骑乘动作使用根Animator上的人马合一Sprite；成功刺击后转身接返航，返航奔跑及打断收招暂用静态姿势。
/// 动画速率适配骑兵玩法时长，真正落马后切换到正式1011控制器。
/// </summary>
[RequireComponent(typeof(CavalryEnemy), typeof(Enemy), typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class CavalryVisualController : MonoBehaviour
{
    [Header("旧占位坐骑层")]
    [SerializeField] private Transform mountAnchor;
    [SerializeField] private SpriteRenderer mountRenderer;
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
    [SerializeField] private AnimationClip turnToIdleAnimation;
    [Tooltip("奔跑帧尚未制作，暂时保持背向姿势，不能视作正式返航循环。")]
    [SerializeField] private AnimationClip retreatPoseAnimation;
    [Tooltip("打断收招尚未制作，暂用持枪姿势，不播放成功刺击。")]
    [SerializeField] private AnimationClip interruptedAnimation;
    [Tooltip("刺击Clip中命中帧的归一化时间；前后两段分别适配strikeWindup和strikeRecover。")]
    [SerializeField, Range(0.01f, 0.99f)] private float strikeHitNormalizedTime = 0.45f;
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
    private bool _strikeToRetreatPlaying;
    private float _strikeToRetreatElapsed;
    private float _strikeToRetreatDuration;
    private float _strikeToRetreatSpeed;
    private float _activeSpeed = 1f;

    private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");
    private static readonly int WindupState = Animator.StringToHash("Base Layer.Windup");
    private static readonly int ChargingState = Animator.StringToHash("Base Layer.Charging");
    private static readonly int AttackState = Animator.StringToHash("Base Layer.Attack");
    private static readonly int StrikeToRetreatState = Animator.StringToHash("Base Layer.StrikeToRetreat");
    private static readonly int InterruptedState = Animator.StringToHash("Base Layer.Interrupted");
    private static readonly int RetreatingState = Animator.StringToHash("Base Layer.Retreating");
    private static readonly int RetreatBlockedState = Animator.StringToHash("Base Layer.RetreatBlocked");
    private static readonly int TurnToIdleState = Animator.StringToHash("Base Layer.TurnToIdle");

    public bool OwnsMountedAnimator => _cavalry != null && _cavalry.IsMounted
        && mountedCombinedAnimatorController != null
        && (GetVisualMode() == VisualMode.MountedCombinedIdle || GetVisualMode() == VisualMode.MountedCombined);

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
        _strikeToRetreatPlaying = false;
        _strikeToRetreatElapsed = 0f;
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
            if (_strikeToRetreatPlaying && !_enemy.IsHitStopped
                && _cavalry.CurrentAnimationStage != CavalryEnemy.AnimationStage.RetreatBlocked)
            {
                _strikeToRetreatElapsed += Time.deltaTime;
                if (_strikeToRetreatElapsed >= _strikeToRetreatDuration)
                {
                    _strikeToRetreatPlaying = false;
                    PlayMounted(_cavalry.CurrentAnimationStage == CavalryEnemy.AnimationStage.RetreatBlocked
                        ? RetreatBlockedState : RetreatingState, retreatPoseAnimation, 0f);
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
                _strikeToRetreatPlaying = false;
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
        if (!retreatStage) _strikeToRetreatPlaying = false;
        _lastAnimationStage = stage;
        _lastMoving = moving;
        _turnPlaying = false;

        switch (stage)
        {
            case CavalryEnemy.AnimationStage.Windup:
                PlayMounted(WindupState, windupAnimation, _cavalry.WindupDuration);
                break;
            case CavalryEnemy.AnimationStage.Charging:
                PlayMounted(ChargingState, chargingAnimation, Mathf.Max(0.02f, _enemy.moveSpeed));
                break;
            case CavalryEnemy.AnimationStage.StrikeWindup:
                PlayMounted(AttackState, strikeAnimation, _cavalry.StrikeWindupDuration,
                    0f, strikeHitNormalizedTime);
                break;
            case CavalryEnemy.AnimationStage.StrikeRecover:
                // 先同步采样命中帧，再由CavalryEnemy提交伤害；收招不依赖动画结束事件。
                // Sprite键在浮点边界上可能仍采样前一帧，微小偏移确保提交当帧显示命中Sprite。
                PlayMounted(AttackState, strikeAnimation, _cavalry.StrikeRecoverDuration,
                    Mathf.Min(1f, strikeHitNormalizedTime + 0.00001f), 1f);
                break;
            case CavalryEnemy.AnimationStage.Interrupted:
                PlayMounted(InterruptedState, interruptedAnimation,
                    _cavalry.Phase == CavalryEnemy.CavalryPhase.Striking
                        ? _cavalry.StrikeRecoverDuration : _cavalry.InterruptRecoveryDuration);
                break;
            case CavalryEnemy.AnimationStage.Retreating:
            case CavalryEnemy.AnimationStage.RetreatBlocked:
                if (previousStage == CavalryEnemy.AnimationStage.StrikeRecover && strikeToRetreatAnimation != null)
                {
                    int returnRows = Mathf.Max(1, _cavalry.ChargeOriginRow - _enemy.rowIndex);
                    _strikeToRetreatDuration = Mathf.Max(0.02f, _enemy.moveSpeed) * returnRows;
                    _strikeToRetreatElapsed = 0f;
                    _strikeToRetreatPlaying = true;
                    PlayMounted(StrikeToRetreatState, strikeToRetreatAnimation, _strikeToRetreatDuration);
                    _strikeToRetreatSpeed = _activeSpeed;
                }
                if (_strikeToRetreatPlaying)
                {
                    _activeSpeed = stage == CavalryEnemy.AnimationStage.RetreatBlocked
                        ? 0f : _strikeToRetreatSpeed;
                    if (_animator != null) _animator.speed = _activeSpeed;
                }
                else
                {
                    PlayMounted(stage == CavalryEnemy.AnimationStage.RetreatBlocked
                        ? RetreatBlockedState : RetreatingState, retreatPoseAnimation, 0f);
                }
                // 受阻、移动标记及排号变化不重播转身；在原有返航时间内完成，不增加等待。
                break;
            default:
                if (moving && chargingAnimation != null)
                {
                    PlayMounted(ChargingState, chargingAnimation, Mathf.Max(0.02f, _enemy.moveSpeed));
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
}
