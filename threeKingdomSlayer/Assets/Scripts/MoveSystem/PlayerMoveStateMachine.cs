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
    [Tooltip("输入管理器（留空时自动取 InputManager.Instance）。用于「按住不成时本段先不结束」的宽限与连段蓄力保持")]
    public InputManager inputManager;

    [Header("招式表（留空 = 直通模式，行为与改造前一致）")]
    public MoveTableConfig moveTable;

    [Header("开关")]
    [Tooltip("输入缓冲：接续窗口尚未打开时提前输入的招式会被记住，窗口打开时执行。仅招式表模式生效")]
    public bool bufferEnabled = true;
    [Tooltip("按住不成时本段先不结束：时钟推进到接续窗口起点就停住，等玩家松手或滑动，避免抬手晚于窗口导致输入被丢（仅对无蓄力后继的连点生效）")]
    public bool holdWaitEnabled = true;
    [Tooltip("无蓄力后继时单次按住最长驻留时间（秒），超过后本段正常收尾，避免连点把收尾段卡住。有蓄力后继的节点不受此上限约束")]
    public float holdWaitMaxSeconds = 1.2f;

    [Header("调试")]
    [Tooltip("显示运行状态面板：当前节点 / 阶段 / 窗口 / 缓冲 / 卡肉")]
    public bool showDebugPanel = false;

    // ---- 运行时状态 ----
    private bool _active;
    private AttackSkillConfig _currentMove;
    private AttackType _currentAttackType;
    private MoveGesture _entryGesture;
    private float _elapsed;
    private float _duration;
    private float _hitStopRemaining;
    private bool _hasBuffered;
    private GestureInput _bufferedInput;
    private string _lastResolution = "—";
    private int _executedCount;
    private bool _pointerWasDown;
    private bool _holdWaitUsed;
    private float _holdWaitElapsed;
    private bool _isWaiting;
    private bool _comboChargeHeld;

    private struct WindowRange
    {
        public float start;
        public float end;
        public WindowRange(float s, float e) { start = s; end = e; }
    }

    public bool UsesTable => moveTable != null && moveTable.roots != null && moveTable.roots.Count > 0;
    public bool IsMoveActive => _active;
    public AttackSkillConfig CurrentMove => _currentMove;
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
        if (inputManager == null) inputManager = InputManager.Instance;
        if (inputManager == null) inputManager = FindObjectOfType<InputManager>();

        // 招式表默认从武将配置上取，避免场景与武将两处接线；显式赋值优先
        if (moveTable == null && playerState != null && playerState.heroConfig != null)
            moveTable = playerState.heroConfig.moveTable;
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

        UpdatePointerHold();

        if (!_active) return;

        if (_duration <= 0f)
        {
            ExitToNeutral();
            return;
        }

        // 按住不成时本段先不结束：时钟推进到接续窗口起点就停住，等玩家松手或滑动。
        // 停驻时窗口已经开着，已缓冲的输入仍必须被消费（否则「刚蓄满就上划」永远出不来）。
        bool waiting = IsWaitingForHold();
        if (!waiting) _elapsed += Time.deltaTime;

        // 窗口打开后消费缓冲输入
        if (_hasBuffered && IsInsideWindow(_bufferedInput.gesture, NormalizedTime, _bufferedInput.chargeLevel))
        {
            GestureInput pending = _bufferedInput;
            _hasBuffered = false;
            AttackType ignored;
            ResolveFromCurrent(pending, out ignored);
            if (!_active) return;
        }

        if (waiting) return;

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
            if (!CanResolve(input.gesture, input.chargeLevel))
            {
                _lastResolution = $"无匹配边 {input.gesture}（不响应）";
                return false;
            }

            WindowRange range = GetWindowFor(_currentMove, _currentAttackType, input.gesture, input.chargeLevel);

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
        AttackSkillConfig root = moveTable.FindRoot(input.gesture);
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
            input.targetColumn, input.slashLeftToRight, input.slashVisualTilt, input.charged, chargeLevel: input.chargeLevel);

        if (!executed)
        {
            _lastResolution = $"直通未执行 {resolvedType}（冷却中或空挥）";
            return false;
        }

        BeginMove(null, resolvedType, input);
        _lastResolution = $"直通 {resolvedType}";
        return true;
    }

    private bool ExecuteMove(AttackSkillConfig move, GestureInput input, out AttackType resolvedType, bool cancelCurrentMove = false)
    {
        resolvedType = move != null
            ? move.attackType
            : MoveGestureDefaults.ResolveAttackType(input.gesture, input.charged);

        if (attackSystem == null) return false;

        bool executed = attackSystem.TryExecuteAttack(resolvedType,
            input.targetColumn, input.slashLeftToRight, input.slashVisualTilt, input.charged, cancelCurrentMove, input.chargeLevel, move);

        if (!executed)
        {
            _lastResolution = $"未执行 {resolvedType}（冷却中或空挥）";
            return false;
        }

        BeginMove(move, resolvedType, input);
        _lastResolution = move != null ? $"执行 {move.name}" : $"执行 {resolvedType}";
        return true;
    }

    private void BeginMove(AttackSkillConfig move, AttackType resolvedType, GestureInput input)
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
        if (duration <= 0f && move != null)
            duration = move.actionDuration;
        _duration = duration;

        // 节点切换：先结束上一段的枪体保持，再按新节点重新判断是否需要停住等蓄力
        if (_comboChargeHeld && attackSystem != null) attackSystem.EndComboChargeHold();
        _comboChargeHeld = false;
        _holdWaitUsed = false;
        _holdWaitElapsed = 0f;

        // 节点切换时手指还按着：本次按下仍算连招输入，不让站桩蓄力的表现插进来
        if (inputManager != null && inputManager.IsPointerDown) inputManager.comboChargeActive = true;

        if (inputManager != null && inputManager.IsPointerDown && attackSystem != null
            && move != null && move.HasChargeContinuation())
        {
            attackSystem.BeginComboChargeHold();
            _comboChargeHeld = true;
        }
    }

    private bool ResolveFromCurrent(GestureInput input, out AttackType resolvedType)
    {
        resolvedType = _currentMove != null ? _currentMove.attackType : _currentAttackType;

        bool edgeDeclared = false;
        AttackSkillConfig next = null;

        if (_currentMove != null && _currentMove.moveEdges != null)
        {
            for (int i = 0; i < _currentMove.moveEdges.Count; i++)
            {
                AttackMoveEdge edge = _currentMove.moveEdges[i];
                if (edge == null || edge.gesture != input.gesture) continue;
                if (input.chargeLevel < edge.minChargeLevel) continue;   // 蓄力不足 → 该边不参与匹配
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

    /// <summary>
    /// 外部原因（例如「按下即出轻攻击」的起手改写）造成当前招式被撤回时调用，回到中立。
    /// </summary>
    public void NotifyExternalReset()
    {
        if (inputManager != null) inputManager.comboChargeActive = false;
        ExitToNeutral();
        _lastResolution = "外部撤回：回到中立";
    }

    private void ExitToNeutral()
    {
        if (_comboChargeHeld && attackSystem != null) attackSystem.EndComboChargeHold();
        _comboChargeHeld = false;
        _isWaiting = false;
        _active = false;
        _currentMove = null;
        _duration = 0f;
        _elapsed = 0f;
        _hasBuffered = false;
    }

    private bool IsInsideWindow(MoveGesture gesture, float normalizedTime, int chargeLevel)
    {
        WindowRange range = GetWindowFor(_currentMove, _currentAttackType, gesture, chargeLevel);
        return normalizedTime >= range.start && normalizedTime <= range.end;
    }

    /// <summary>当前节点是否存在该手势的出路：显式边（且指定了后继）或默认重复规则</summary>
    private bool CanResolve(MoveGesture gesture, int chargeLevel)
    {
        if (_currentMove == null) return false;

        if (_currentMove.moveEdges != null)
        {
            for (int i = 0; i < _currentMove.moveEdges.Count; i++)
            {
                AttackMoveEdge edge = _currentMove.moveEdges[i];
                if (edge == null || edge.gesture != gesture) continue;
                if (chargeLevel < edge.minChargeLevel) continue;   // 蓄力不足 → 不占用缓冲，也不执行
                return edge.next != null;   // 显式声明但未指定后继 → 该输入不转移节点
            }
        }

        // 无显式边时的默认重复规则：只作用于进入本节点的手势
        return _currentMove.repeatSelf && gesture == _entryGesture;
    }

    private WindowRange GetWindowFor(AttackSkillConfig move, AttackType type, MoveGesture gesture, int chargeLevel)
    {
        // 1) 边上的显式窗口
        if (move != null && move.moveEdges != null)
        {
            for (int i = 0; i < move.moveEdges.Count; i++)
            {
                AttackMoveEdge edge = move.moveEdges[i];
                if (edge == null || edge.gesture != gesture) continue;
                if (chargeLevel < edge.minChargeLevel) continue;   // 蓄力不足的同名手势边不参与
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

    /// <summary>本节点最早的接续窗口起点（归一化）：按住不成时的停驻点</summary>
    private float GetEarliestWindowStart01(AttackSkillConfig move, AttackType type)
    {
        float best = float.MaxValue;
        if (move != null && move.moveEdges != null)
        {
            for (int i = 0; i < move.moveEdges.Count; i++)
            {
                AttackMoveEdge edge = move.moveEdges[i];
                if (edge == null || edge.next == null) continue;
                float start = GetWindowFor(move, type, edge.gesture, edge.minChargeLevel).start;
                if (start < best) best = start;
            }
        }
        if (best == float.MaxValue)
            best = GetWindowFor(move, type, _entryGesture, int.MaxValue).start;
        return best;
    }

    /// <summary>
    /// 按住不成时本段先不结束：时钟推进到接续窗口起点就停住。
    /// 返回 true 表示本帧停驻（不推进时钟，也不判定本段收尾结束）。
    ///
    /// 驻留上限只用于「无蓄力后继」的连点场景（避免把收尾段卡住）：
    /// 本段存在「需要蓄力的后继边」时不受上限约束——玩家按多久就该等多久，
    /// 否则蓄力过头会让本段先行收尾、松手时连招直接丢失。C1→C2 与 C4 两条链共用此语义。
    /// </summary>
    private bool IsWaitingForHold()
    {
        _isWaiting = false;
        if (!holdWaitEnabled || _holdWaitUsed) return false;
        if (inputManager == null || !inputManager.IsPointerDown) return false;
        if (_currentMove == null || !_currentMove.HasAnyContinuation()) return false;

        float parkTime = GetEarliestWindowStart01(_currentMove, _currentAttackType) * _duration;
        if (_elapsed < parkTime) return false;

        if (!_currentMove.HasChargeContinuation())
        {
            _holdWaitElapsed += Time.deltaTime;
            if (_holdWaitElapsed >= holdWaitMaxSeconds)
            {
                _holdWaitUsed = true;   // 本次按住不再驻留，让本段正常收尾
                return false;
            }
        }

        _isWaiting = true;
        return true;
    }

    /// <summary>按下/松开的边沿处理：重置驻留额度，并决定是否让当前戳击的枪体停住等蓄力</summary>
    private void UpdatePointerHold()
    {
        bool pointerDown = inputManager != null && inputManager.IsPointerDown;

        if (pointerDown && !_pointerWasDown)
        {
            _holdWaitUsed = false;
            _holdWaitElapsed = 0f;

            // 连招进行中按下：这段输入属于连招，不属于站桩蓄力（穿刺指示器 / 蓄力视觉都让位）
            if (_active && inputManager != null) inputManager.comboChargeActive = true;

            // 本节点存在蓄力后继时，还让枪体停在回收段中途等蓄力
            if (_active && attackSystem != null && _currentMove != null && _currentMove.HasChargeContinuation())
            {
                attackSystem.BeginComboChargeHold();
                _comboChargeHeld = true;
            }
        }

        if (!pointerDown && _pointerWasDown && _comboChargeHeld)
        {
            if (attackSystem != null) attackSystem.EndComboChargeHold();
            _comboChargeHeld = false;
        }

        if (pointerDown && _comboChargeHeld && attackSystem != null && inputManager != null)
            attackSystem.UpdateComboChargeHold(inputManager.HoldDurationSeconds, inputManager.CurrentPointerStabColumn);

        _pointerWasDown = pointerDown;
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
            ? (_currentMove != null ? _currentMove.name : _currentAttackType.ToString())
            : "中立";

        string window = "—";
        if (_active)
        {
            WindowRange range = GetWindowFor(_currentMove, _currentAttackType, _entryGesture, int.MaxValue);
            window = $"{range.start:P0}~{range.end:P0}";
        }

        string text =
            $"[招式状态机] {(UsesTable ? "招式表模式" : "直通模式")}\n" +
            $"节点: {node}\n" +
            $"阶段: {NormalizedTime:P0}  ({_elapsed:F3}s / {_duration:F3}s)   窗口: {window}   等待: {(_isWaiting ? "是" : "否")}\n" +
            $"缓冲: {(_hasBuffered ? _bufferedInput.gesture.ToString() : "无")}   卡肉: {_hitStopRemaining:F3}s   蓄力保持: {(_comboChargeHeld ? "是" : "否")}\n" +
            $"最近: {_lastResolution}   执行数: {_executedCount}";

        GUI.Box(new Rect(10f, 10f, 400f, 108f), text);
    }
}
