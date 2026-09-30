using UnityEngine;

/// <summary>
/// 招式状态机（P1）。
///
/// 职责：接收输入手势 → 解析为招式 → 交给 AttackSystem 执行；维护当前节点、接续窗口、输入缓冲与阶段时钟。
///
/// 两种模式：
///   直通模式（moveTable 为空）：不做段位解析，手势直接按等价映射放招。行为与改造前完全一致。
///   招式表模式（moveTable 已配置）：中立态查入口表；招式进行中按「当前节点的边 + 接续窗口」解析；
///                                    无匹配边 → 什么都不发生，走完收尾回中立。
///
/// P1 约束：节点时钟仍由「命中后生效的动作锁」驱动，与现状一致；空挥不推进段位。
/// </summary>
public class PlayerMoveStateMachine : MonoBehaviour
{
    [Header("引用")]
    public AttackSystem attackSystem;
    public PlayerState playerState;

    [Header("招式表（留空 = 直通模式，行为与改造前一致）")]
    public MoveTableConfig moveTable;

    [Header("开关")]
    [Tooltip("输入缓冲：接续窗口尚未打开时提前输入的招式会被记住，窗口打开时执行。仅招式表模式生效")]
    public bool bufferEnabled = true;

    [Header("调试")]
    [Tooltip("显示运行状态面板：当前节点 / 阶段 / 窗口 / 缓冲 / 卡肉")]
    public bool showDebugPanel = false;

    // ---- 运行时状态 ----
    private bool _active;
    private MoveDefinition _currentMove;
    private AttackType _currentAttackType;
    private MoveGesture _entryGesture;
    private float _elapsed;
    private float _duration;
    private float _hitStopRemaining;
    private bool _hasBuffered;
    private GestureInput _bufferedInput;
    private string _lastResolution = "—";
    private int _executedCount;

    private struct WindowRange
    {
        public float start;
        public float end;
        public WindowRange(float s, float e) { start = s; end = e; }
    }

    public bool UsesTable => moveTable != null && moveTable.roots != null && moveTable.roots.Count > 0;
    public bool IsMoveActive => _active;
    public MoveDefinition CurrentMove => _currentMove;
    public AttackType CurrentAttackType => _currentAttackType;
    public MoveGesture EntryGesture => _entryGesture;
    public float NormalizedTime => _duration > 0f ? Mathf.Clamp01(_elapsed / _duration) : 0f;
    public bool HasBufferedInput => _hasBuffered;
    public float HitStopRemaining => _hitStopRemaining;
    public string LastResolution => _lastResolution;
    public int ExecutedCount => _executedCount;

    private void Awake()
    {
        if (attackSystem == null) attackSystem = FindObjectOfType<AttackSystem>();
        if (playerState == null) playerState = FindObjectOfType<PlayerState>();
    }

    private void OnEnable()
    {
        HitFeedbackManager.OnHitStopApplied += OnHitStopApplied;
    }

    private void OnDisable()
    {
        HitFeedbackManager.OnHitStopApplied -= OnHitStopApplied;
    }

    private void OnHitStopApplied(float duration)
    {
        if (duration > _hitStopRemaining) _hitStopRemaining = duration;
    }

    private void Update()
    {
        // 卡肉期间阶段时钟一并冻结，保持与视觉序列同步（卡肉只暂停敌人与特效，玩家侧原本不受影响）
        if (_hitStopRemaining > 0f)
        {
            _hitStopRemaining -= Time.deltaTime;
            if (_hitStopRemaining < 0f) _hitStopRemaining = 0f;
            return;
        }

        if (!_active) return;

        if (_duration <= 0f)
        {
            ExitToNeutral();
            return;
        }

        _elapsed += Time.deltaTime;

        // 窗口打开后消费缓冲输入
        if (_hasBuffered && IsInsideWindow(_bufferedInput.gesture, NormalizedTime))
        {
            GestureInput pending = _bufferedInput;
            _hasBuffered = false;
            AttackType ignored;
            ResolveFromCurrent(pending, out ignored);
            if (!_active) return;
        }

        if (_elapsed >= _duration)
            ExitToNeutral();
    }

    /// <summary>
    /// 投递一次手势输入。
    /// 返回是否实际执行了招式；resolvedType 为实际执行的攻击类型（供 InputManager 派发 OnAttackExecuted）。
    /// </summary>
    public bool SubmitInput(GestureInput input, out AttackType resolvedType)
    {
        resolvedType = MoveGestureDefaults.ResolveAttackType(input.gesture, input.charged);

        // 直通模式：与改造前一致，不做任何段位解析
        if (!UsesTable)
            return ExecuteDirect(input, ref resolvedType);

        // 招式进行中：按当前节点的窗口与边解析
        if (_active)
        {
            float t = NormalizedTime;

            // 本节点没有任何出路（无显式边、也不满足默认重复规则）→ 不响应，且不占用缓冲
            if (!CanResolve(input.gesture))
            {
                _lastResolution = $"无匹配边 {input.gesture}（不响应）";
                return false;
            }

            WindowRange range = GetWindowFor(_currentMove, _currentAttackType, input.gesture);

            if (t >= range.start && t <= range.end)
                return ResolveFromCurrent(input, out resolvedType);

            if (t < range.start)
            {
                if (bufferEnabled)
                {
                    _bufferedInput = input;
                    _hasBuffered = true;
                    _lastResolution = $"缓冲 {input.gesture}（窗口 {range.start:P0} 未开）";
                }
                else
                {
                    _lastResolution = $"丢弃 {input.gesture}（窗口未开且缓冲关闭）";
                }
                return false;
            }

            _lastResolution = $"窗口已关闭 {input.gesture}（不响应）";
            return false;
        }

        // 中立态：查入口表；未配置的手势回落到默认攻击类型，保证招式表未填完时仍可游戏
        MoveDefinition root = moveTable.FindRoot(input.gesture);
        if (root == null)
            return ExecuteDirect(input, ref resolvedType);

        return ExecuteMove(root, input, out resolvedType);
    }

    // ---- 内部实现 ----

    private bool ExecuteDirect(GestureInput input, ref AttackType resolvedType)
    {
        resolvedType = MoveGestureDefaults.ResolveAttackType(input.gesture, input.charged);
        if (attackSystem == null) return false;

        bool executed = attackSystem.TryExecuteAttack(resolvedType,
            input.targetColumn, input.slashLeftToRight, input.slashVisualTilt, input.charged);

        if (!executed)
        {
            _lastResolution = $"直通未执行 {resolvedType}（冷却中或空挥）";
            return false;
        }

        BeginMove(null, resolvedType, input);
        _lastResolution = $"直通 {resolvedType}";
        return true;
    }

    private bool ExecuteMove(MoveDefinition move, GestureInput input, out AttackType resolvedType, bool cancelCurrentMove = false)
    {
        resolvedType = move != null
            ? move.MoveAttackType
            : MoveGestureDefaults.ResolveAttackType(input.gesture, input.charged);

        if (attackSystem == null) return false;

        bool executed = attackSystem.TryExecuteAttack(resolvedType,
            input.targetColumn, input.slashLeftToRight, input.slashVisualTilt, input.charged, cancelCurrentMove);

        if (!executed)
        {
            _lastResolution = $"未执行 {resolvedType}（冷却中或空挥）";
            return false;
        }

        BeginMove(move, resolvedType, input);
        _lastResolution = move != null ? $"执行 {move.ResolvedName}" : $"执行 {resolvedType}";
        return true;
    }

    private void BeginMove(MoveDefinition move, AttackType resolvedType, GestureInput input)
    {
        _active = true;
        _currentMove = move;
        _currentAttackType = resolvedType;
        _entryGesture = input.gesture;
        _elapsed = 0f;
        _hasBuffered = false;
        _executedCount++;

        // 参考时长取实际生效的动作锁；独立 CD 模式下回落到配置动作时长
        float duration = attackSystem != null ? attackSystem.LastMoveDuration : 0f;
        if (duration <= 0f && move != null && move.config != null)
            duration = move.config.actionDuration;
        _duration = duration;
    }

    private bool ResolveFromCurrent(GestureInput input, out AttackType resolvedType)
    {
        resolvedType = _currentMove != null ? _currentMove.MoveAttackType : _currentAttackType;

        bool edgeDeclared = false;
        MoveDefinition next = null;

        if (_currentMove != null && _currentMove.edges != null)
        {
            for (int i = 0; i < _currentMove.edges.Count; i++)
            {
                MoveEdge edge = _currentMove.edges[i];
                if (edge == null || edge.gesture != input.gesture) continue;
                edgeDeclared = true;   // 显式声明即接管：next 为空表示「该输入不转移节点」
                next = edge.next;
                break;
            }
        }

        // 未声明该输入时，回落节点的默认重复规则（默认只作用于进入本节点的手势）
        if (!edgeDeclared && _currentMove != null && _currentMove.repeatSelf && input.gesture == _entryGesture)
            next = _currentMove;

        if (next == null)
        {
            _lastResolution = $"无后继 {input.gesture}（不响应）";
            ExitToNeutral();
            return false;
        }

        // 接续：取消当前招式的收尾，直接执行后继
        return ExecuteMove(next, input, out resolvedType, cancelCurrentMove: true);
    }

    private void ExitToNeutral()
    {
        _active = false;
        _currentMove = null;
        _duration = 0f;
        _elapsed = 0f;
        _hasBuffered = false;
    }

    private bool IsInsideWindow(MoveGesture gesture, float normalizedTime)
    {
        WindowRange range = GetWindowFor(_currentMove, _currentAttackType, gesture);
        return normalizedTime >= range.start && normalizedTime <= range.end;
    }

    /// <summary>当前节点是否存在该手势的出路：显式边（且指定了后继）或默认重复规则</summary>
    private bool CanResolve(MoveGesture gesture)
    {
        if (_currentMove == null) return false;

        if (_currentMove.edges != null)
        {
            for (int i = 0; i < _currentMove.edges.Count; i++)
            {
                MoveEdge edge = _currentMove.edges[i];
                if (edge == null || edge.gesture != gesture) continue;
                return edge.next != null;   // 显式声明但未指定后继 → 该输入不转移节点
            }
        }

        // 无显式边时的默认重复规则：只作用于进入本节点的手势
        return _currentMove.repeatSelf && gesture == _entryGesture;
    }

    private WindowRange GetWindowFor(MoveDefinition move, AttackType type, MoveGesture gesture)
    {
        // 1) 边上的显式窗口
        if (move != null && move.edges != null)
        {
            for (int i = 0; i < move.edges.Count; i++)
            {
                MoveEdge edge = move.edges[i];
                if (edge == null || edge.gesture != gesture) continue;
                if (edge.overrideWindow)
                    return ClampWindow(new WindowRange(edge.windowStart01, edge.windowEnd01), type);
                break;
            }
        }

        // 2) 节点级窗口覆盖
        if (move != null && move.overrideWindow)
            return ClampWindow(new WindowRange(move.windowStart01, move.windowEnd01), type);

        // 3) 按招式类型派生默认窗口
        float start, end;
        MoveTableConfig.GetDefaultWindow(type, out start, out end);
        return ClampWindow(new WindowRange(start, end), type);
    }

    private WindowRange ClampWindow(WindowRange window, AttackType type)
    {
        if (moveTable != null)
        {
            // 保守模式：连显式窗口也抬到命中窗口结束之后（默认关闭，显式配置优先）
            if (moveTable.clampWindowAfterHitWindow)
            {
                float ds, de;
                MoveTableConfig.GetDefaultWindow(type, out ds, out de);
                if (window.start < ds) window.start = ds;
            }

            // 窗口最小绝对时长，防止高攻速下窗口趋近 0
            if (_duration > 0f && moveTable.minWindowSeconds > 0f)
            {
                float minWindow = Mathf.Clamp01(moveTable.minWindowSeconds / _duration);
                if (window.end < window.start) window.end = window.start;
                if (window.end - window.start < minWindow)
                    window.start = Mathf.Clamp01(window.end - minWindow);
            }
        }

        window.start = Mathf.Clamp01(window.start);
        window.end = Mathf.Clamp01(window.end);
        return window;
    }

    private void OnGUI()
    {
        if (!showDebugPanel) return;

        string node = _active
            ? (_currentMove != null ? _currentMove.ResolvedName : _currentAttackType.ToString())
            : "中立";

        string window = "—";
        if (_active)
        {
            WindowRange range = GetWindowFor(_currentMove, _currentAttackType, _entryGesture);
            window = $"{range.start:P0}~{range.end:P0}";
        }

        string text =
            $"[招式状态机] {(UsesTable ? "招式表模式" : "直通模式")}\n" +
            $"节点: {node}\n" +
            $"阶段: {NormalizedTime:P0}  ({_elapsed:F3}s / {_duration:F3}s)   窗口: {window}\n" +
            $"缓冲: {(_hasBuffered ? _bufferedInput.gesture.ToString() : "无")}   卡肉: {_hitStopRemaining:F3}s\n" +
            $"最近: {_lastResolution}   执行数: {_executedCount}";

        GUI.Box(new Rect(10f, 10f, 400f, 108f), text);
    }
}
