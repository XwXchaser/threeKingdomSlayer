using TMPro;
using UnityEngine;

/// <summary>109骑兵的临时双层验收表现。根Renderer/Animator仍负责101骑手，105仅作无逻辑坐骑。</summary>
[RequireComponent(typeof(CavalryEnemy), typeof(Enemy))]
public sealed class CavalryVisualController : MonoBehaviour
{
    [SerializeField] private Transform mountAnchor;
    [SerializeField] private SpriteRenderer mountRenderer;
    [SerializeField] private Sprite mountIdle;
    [SerializeField] private Sprite mountWalk1;
    [SerializeField] private Sprite mountWalk2;
    [SerializeField] private Transform riderAnchor;
    [SerializeField] private SpriteRenderer riderRenderer;
    [SerializeField] private SpriteRenderer sourceRenderer;
    [SerializeField] private TextMeshPro debugLabel;
    [SerializeField] private bool showDebugState = true;
    [SerializeField] private Vector3 mountedRiderOffset = new Vector3(0f, 3.5f, -0.04f);
    [SerializeField] private Vector3 dismountedRiderOffset = Vector3.zero;
    [SerializeField] private float walkFrameDuration = 0.12f;

    private CavalryEnemy _cavalry;
    private Enemy _enemy;
    private CavalryEnemy.CavalryPhase _lastPhase;
    private int _lastRow = int.MinValue;
    private float _nextFrame;
    private bool _walkFlip;

    private void Awake()
    {
        _cavalry = GetComponent<CavalryEnemy>();
        _enemy = GetComponent<Enemy>();
    }

    private void OnEnable()
    {
        _cavalry ??= GetComponent<CavalryEnemy>();
        _enemy ??= GetComponent<Enemy>();
        if (_cavalry != null) _cavalry.OnStateChanged += Refresh;
        _lastRow = int.MinValue;
        Refresh(_cavalry);
    }

    private void OnDisable()
    {
        if (_cavalry != null) _cavalry.OnStateChanged -= Refresh;
    }

    private void Update()
    {
        if (_cavalry == null || _enemy == null) return;
        if (riderRenderer != null && sourceRenderer != null)
        {
            riderRenderer.sprite = sourceRenderer.sprite;
            riderRenderer.color = sourceRenderer.color;
            if (riderRenderer.sharedMaterial != sourceRenderer.sharedMaterial)
                riderRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
        }
        var phase = _cavalry.Phase;
        if (_lastRow != _enemy.rowIndex || _lastPhase != phase)
            Refresh(_cavalry);
        if (mountRenderer != null && _cavalry.IsMounted)
        {
            bool walking = phase == CavalryEnemy.CavalryPhase.Charging || phase == CavalryEnemy.CavalryPhase.Windup || phase == CavalryEnemy.CavalryPhase.Retreating;
            if (walking && Time.time >= _nextFrame)
            {
                _walkFlip = !_walkFlip;
                mountRenderer.sprite = _walkFlip ? mountWalk1 : mountWalk2;
                _nextFrame = Time.time + Mathf.Max(0.03f, walkFrameDuration);
            }
            else if (!walking && mountRenderer.sprite != mountIdle)
                mountRenderer.sprite = mountIdle;
        }
    }

    private void Refresh(CavalryEnemy cavalry)
    {
        if (cavalry == null || _enemy == null) return;
        bool mounted = cavalry.IsMounted;
        if (mountAnchor != null) mountAnchor.gameObject.SetActive(mounted);
        if (riderAnchor != null)
            riderAnchor.localPosition = mounted ? mountedRiderOffset : dismountedRiderOffset;
        if (mountRenderer != null && mounted) mountRenderer.sprite = mountIdle;
        _lastPhase = cavalry.Phase;
        _lastRow = _enemy.rowIndex;
        if (debugLabel != null)
        {
            debugLabel.gameObject.SetActive(showDebugState);
            if (showDebugState)
            {
                debugLabel.text = $"109 #{_enemy.instanceId} {cavalry.Phase}\norigin={cavalry.ChargeOriginRow} grids={cavalry.ChargeGrids} dmg={cavalry.ChargeDamage:F0}\nrow={_enemy.rowIndex} {cavalry.LastReason}";
                debugLabel.color = !mounted ? Color.red : cavalry.Phase == CavalryEnemy.CavalryPhase.Charging || cavalry.Phase == CavalryEnemy.CavalryPhase.Striking
                    ? new Color(1f, 0.55f, 0.1f) : cavalry.Phase == CavalryEnemy.CavalryPhase.Retreating || cavalry.Phase == CavalryEnemy.CavalryPhase.RetreatBlocked
                        ? Color.cyan : Color.yellow;
            }
        }
    }
}
