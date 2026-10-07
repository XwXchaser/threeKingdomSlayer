using TMPro;
using UnityEngine;

/// <summary>
/// 109骑兵视觉路由。
/// MountedIdle 使用一张人马合一的正式Sprite动画；其它尚未制作的骑乘阶段保留旧的
/// 101骑手 + 105坐骑占位表现；真正落马后切换到正式1011控制器。
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
    [Tooltip("MountedIdle使用的人马合一控制器，Idle motion为Enemy_109_MountedIdle。其它状态可沿用1011契约。")]
    [SerializeField] private RuntimeAnimatorController mountedCombinedAnimatorController;
    [Tooltip("Windup/Charging/Striking/Retreating尚未有正式合成动作时使用的旧101骑手占位控制器。")]
    [SerializeField] private RuntimeAnimatorController mountedPlaceholderAnimatorController;
    [Tooltip("永久落马后使用的正式1011普通敌人控制器。")]
    [SerializeField] private RuntimeAnimatorController dismountedAnimatorController;

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
        Dismounted
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
        Refresh(_cavalry);
    }

    private void OnDisable()
    {
        if (_cavalry != null) _cavalry.OnStateChanged -= Refresh;
    }

    private void Update()
    {
        if (_cavalry == null || _enemy == null) return;

        var phase = _cavalry.Phase;
        var mode = GetVisualMode();
        if (_lastVisualMode != mode || _lastRow != _enemy.rowIndex || _lastPhase != phase)
            Refresh(_cavalry);

        if (mode == VisualMode.MountedPlaceholder)
        {
            MirrorPlaceholderRider();
            UpdatePlaceholderMount(phase);
        }
    }

    private VisualMode GetVisualMode()
    {
        if (_cavalry == null || !_cavalry.IsMounted)
            return VisualMode.Dismounted;
        return _cavalry.Phase == CavalryEnemy.CavalryPhase.MountedIdle && mountedCombinedAnimatorController != null
            ? VisualMode.MountedCombinedIdle
            : VisualMode.MountedPlaceholder;
    }

    private void Refresh(CavalryEnemy cavalry)
    {
        if (cavalry == null || _enemy == null) return;

        var mode = GetVisualMode();
        ApplyVisualMode(mode);
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
                SetAnimatorController(mountedCombinedAnimatorController, true);
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
                SetAnimatorController(dismountedAnimatorController, _enemy != null && _enemy.state == EnemyState.Idle);
                if (_rootRenderer != null) _rootRenderer.enabled = true;
                if (mountAnchor != null) mountAnchor.gameObject.SetActive(false);
                if (riderRenderer != null) riderRenderer.enabled = false;
                if (riderAnchor != null) riderAnchor.localPosition = dismountedRiderOffset;
                break;
        }
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
