using System.Collections;
using UnityEngine;

/// <summary>
/// 骑兵专属行为（109）：骑乘期间只有一次次的冲锋循环
///   判定 → 前摇 → 前移 → 命中（打前方敌人或玩家）/ 被打断 → 原路返航 → 结束
/// 规则要点：
/// - 骑乘期间不做普通攻击；落马后交回普通敌人逻辑（含普通攻击与普通补齐）。
/// - 冲锋可以从任意排发起，不需要先回到某固定排；冲锋伤害 = chargeDamage × (发起排 + 1) × 波次攻击倍率。
/// - 冲锋路径上出现敌人就停在它后面一排前探打它，不越过、不并排。
/// - 返航目标是本次冲锋的发起排；中途受阻就停在阻塞前等待，状态机继续循环重试。
/// - 只有被标记为“可打断骑兵冲锋”的攻击（当前 = 蓄力攻击）能打断冲锋；Parry 暂不打断。
/// - 真正进入 Enemy.Launch() 才永久落马。
/// 该组件不创建普通 WaveMarch 订单；普通补齐仍由 ColumnManager 独立负责。
/// </summary>
public sealed partial class CavalryEnemy : MonoBehaviour
{
    [Header("骑兵配置")]
    [Tooltip("骑兵敌人ID，用于校验Prefab配置。")]
    [SerializeField] private int cavalryEnemyId = 109;
    [Tooltip("冲锋基础伤害；最终伤害 = 本值 × 冲锋格数（发起排+1）× 波次攻击倍率。")]
    [SerializeField, Min(0f)] private float chargeDamage = 8f;

    /// <summary>
    /// 教学关演出用：把本次冲锋伤害改成固定值（不污染 Enemy_109.prefab 的正式数值）。
    /// </summary>
    public void OverrideChargeDamageForScript(float value) => chargeDamage = Mathf.Max(0f, value);
    [Tooltip("判定成功后到开始前移的动作前摇时长；前摇期间可被打断。")]
    [SerializeField, Min(0f)] private float windupDuration = 0.3f;
    [Tooltip("命中前探时长；也为玩家留出‘看到冲锋起来后拉远’的反应窗口。")]
    [SerializeField, Min(0.02f)] private float strikeWindup = 0.36f;
    [Tooltip("命中收招时长。")]
    [SerializeField, Min(0.02f)] private float strikeRecover = 0.3f;
    [Tooltip("被打断后进入返航前的收招时长。")]
    [SerializeField, Min(0f)] private float interruptRecover = 0.2f;
    [Tooltip("冲锋时向前推进一排的时长（秒/排）。独立于普通行军的 moveSpeed，改这里不会影响普通行军。")]
    [SerializeField, Min(0.02f)] private float chargeStepDuration = 0.35f;
    [Tooltip("返航时向后退回一排的时长（秒/排）。独立于普通行军的 moveSpeed，改这里不会影响普通行军。")]
    [SerializeField, Min(0.02f)] private float retreatStepDuration = 0.35f;
    [Tooltip("返航转身时长（秒）：先把转身动作在原地演完，再开始逐排后退。0 = 不转身，直接切背向姿势。")]
    [SerializeField, Min(0f)] private float retreatTurnDuration = 0.4f;
    [Tooltip("两次冲锋之间的冷却。")]
    [SerializeField, Min(0f)] private float chargeCooldown = 1.6f;
    [Tooltip("SpawnEntry结束后推迟战斗判定的时间，避免入场同帧发起冲锋。")]
    [SerializeField, Min(0f)] private float spawnEntrySettleDuration = 0.35f;

    [Header("命中帧（与 1011 同一口径：按屏幕精灵名结算）")]
    [Tooltip("刺击的伤害判帧。spriteName = 该帧精灵名（如 Enemy_109_Attack4），运行时比对屏幕上真实显示的精灵；" +
             "hitTime/keyIndex 用于把 Attack clip 切成前摇/收尾两段，默认值见 Tools/三国杀戮/校验攻击命中帧。")]
    [SerializeField] private AttackHitFrame strikeHitFrame = new AttackHitFrame
    {
        spriteName = "",
        keyIndex = 0,
        resolve = true,
        hitTime = 0f
    };

    public enum CavalryPhase { MountedIdle, Windup, Charging, Striking, Retreating, RetreatBlocked, Dismounted }
    // 视觉子阶段不新增移动/伤害状态；命中前探与收招共享Striking玩法阶段。
    public enum AnimationStage { Idle, Windup, Charging, StrikeWindup, StrikeRecover, Interrupted, Retreating, RetreatBlocked, Dismounted }

    public CavalryPhase Phase { get; private set; } = CavalryPhase.MountedIdle;
    public AnimationStage CurrentAnimationStage { get; private set; } = AnimationStage.Idle;
    public float WindupDuration => windupDuration;
    public float StrikeWindupDuration => strikeWindup;
    public float StrikeRecoverDuration => strikeRecover;
    public float InterruptRecoveryDuration => interruptRecover;
    public float ChargeCooldown => chargeCooldown;
    /// <summary>冲锋每前进一排的时长（秒/排）。</summary>
    public float ChargeStepDuration => chargeStepDuration;
    /// <summary>返航每退回一排的时长（秒/排）。</summary>
    public float RetreatStepDuration => retreatStepDuration;
    /// <summary>返航前的原地转身时长（秒）；0 = 不转身。</summary>
    public float RetreatTurnDuration => retreatTurnDuration;
    /// <summary>是否声明了刺击命中帧精灵名（空 = 只能靠前摇计时结算）。</summary>
    public bool HasStrikeHitFrame => !string.IsNullOrEmpty(strikeHitFrame.spriteName);
    /// <summary>刺击命中帧精灵名，与 1011 的 hitFrames.spriteName 同一口径。</summary>
    public string StrikeHitSpriteName => strikeHitFrame.spriteName;
    /// <summary>命中帧在 Attack clip 中的时间（秒）；由关键帧推导，用作前摇/收尾的切分点。</summary>
    public float StrikeHitTime => strikeHitFrame.hitTime;
    public event System.Action<CavalryEnemy> OnStateChanged;
    public string LastReason { get; private set; } = "";

    private Enemy _enemy;
    private CavalryVisualController _visual;
    private Coroutine _cycleRoutine;

    /// <summary>收尾阶段等待 Attack clip 播完的安全超时（秒），防止配置错误导致卡死。</summary>
    private const float MaxAttackTailWait = 2f;

    /// <summary>前摇结束后等待命中帧精灵上屏的最长时间（秒）；超时则按计时兜底结算。</summary>
    private const float HitFrameGrace = 0.15f;
    private bool _mounted;
    private int _generation;
    private bool _damageCommitted;
    private int _chargeOriginRow = -1;
    private int _chargeGrids = 1;
    private float _nextChargeTime;
    private bool _spawnEntryCompleted = true;
    private float _spawnEntrySettleUntil;
    /// <summary>本次冲锋是否在冻结中被暂停过：解冻后不续播冲锋，转为返航（设计 §4.1 / 用例 11f）。</summary>
    private bool _frozenDuringCharge;

    public bool IsMounted => _mounted;
    /// <summary>冲锋判定已开始且本次伤害尚未提交：只有这段时间会被“可打断冲锋”的攻击打断。</summary>
    public bool IsCharging => _mounted && (Phase == CavalryPhase.Windup || Phase == CavalryPhase.Charging || Phase == CavalryPhase.Striking);
    public bool IsSpecialMoveActive => _mounted && Phase != CavalryPhase.MountedIdle && Phase != CavalryPhase.Dismounted;
    public int ChargeOriginRow => _chargeOriginRow;
    public int ChargeGrids => _chargeGrids;

    /// <summary>本次冲锋造成的伤害：chargeDamage × 冲锋格数 × 波次攻击倍率。</summary>
    public float ChargeDamage
    {
        get
        {
            float waveMult = _enemy != null && _enemy.BaseAttackDamage > 0f
                ? _enemy.attackDamage / _enemy.BaseAttackDamage
                : 1f;
            return chargeDamage * Mathf.Max(1, _chargeGrids) * waveMult;
        }
    }

    public bool IsSpawnEntrySettling => !_spawnEntryCompleted || Time.time < _spawnEntrySettleUntil;

    private bool OwnerGone => _enemy == null || _enemy.state == EnemyState.Dead;
    private bool IsStale(int generation) => generation != _generation || !_mounted || _enemy == null;
    private bool HasMovementOrder() => _enemy != null && (_enemy.HasRushMoveOrder || _enemy.IsRushMovementActive);

    /// <summary>
    /// 本帧骑兵动作是否应暂停：卡肉，或冻结/破冰（设计 §4.1：冻结期间冲锋停在原地，不落位、不落马）。
    /// 冲锋阶段一旦因冻结暂停，标记本代际冲锋作废，解冻后由冲锋循环转入返航。
    /// </summary>
    private bool StepPaused()
    {
        if (_enemy == null) return false;
        if (_enemy.IsActionPaused)
        {
            if (IsCharging) _frozenDuringCharge = true;
            return true;
        }
        return _enemy.IsHitStopped;
    }

    private void Awake()
    {
        _enemy = GetComponent<Enemy>();
        _visual = GetComponent<CavalryVisualController>();
    }

    /// <summary>每次从对象池取出时调用。Enemy.Initialize 会调用本方法。</summary>
    public void ResetCavalry()
    {
        if (_cycleRoutine != null)
        {
            StopCoroutine(_cycleRoutine);
            _cycleRoutine = null;
        }
        _enemy ??= GetComponent<Enemy>();
        _visual ??= GetComponent<CavalryVisualController>();
        EnemyManager.Instance?.columnManager?.ReleaseCavalrySlot(_enemy);

        _mounted = true;
        _generation = 0;
        _damageCommitted = false;
        _chargeOriginRow = -1;
        _chargeGrids = 1;
        _nextChargeTime = 0f;
        _spawnEntryCompleted = true;
        _spawnEntrySettleUntil = 0f;
        _frozenDuringCharge = false;
        Phase = CavalryPhase.MountedIdle;
        CurrentAnimationStage = AnimationStage.Idle;
        LastReason = "";
        OnStateChanged?.Invoke(this);
    }

    /// <summary>
    /// Enemy 在 Idle 状态每帧调用，驱动骑乘态状态机（判定 / 返航重试）。
    /// 返回 true 表示本帧由骑兵接管。
    /// </summary>
    public bool Tick()
    {
        if (_enemy == null || !_mounted || _enemy.state == EnemyState.Dead)
            return false;
        if (_cycleRoutine != null)
            return true;
        if (_enemy.state != EnemyState.Idle)
            return false;
        if (IsSpawnEntrySettling || HasMovementOrder())
            return false;
        if (PlayerState.Instance == null || PlayerState.Instance.stageState != StageState.InProgress)
            return false;

        // 返航受阻：每帧重试继续返航，不重新发起冲锋
        if (Phase == CavalryPhase.RetreatBlocked)
        {
            TryResumeRetreat();
            return true;
        }

        if (Time.time < _nextChargeTime)
            return false;

        var manager = EnemyManager.Instance?.columnManager;
        if (manager == null || !manager.IsCavalryReadyForCombat(_enemy))
            return false;

        // row0 没有冲锋距离：如果还欠一次返航（任何中断路径都可能把它留在 row0），先把返航补完。
        if (_enemy.rowIndex <= 0)
        {
            if (_chargeOriginRow > _enemy.rowIndex)
            {
                _cycleRoutine = StartCoroutine(RetreatCycle());
                return true;
            }
            return false;
        }

        _chargeOriginRow = _enemy.rowIndex;
        _chargeGrids = _chargeOriginRow + 1;
        _cycleRoutine = StartCoroutine(CavalryCycle());
        return true;
    }

    #region 冲锋循环


    #endregion

    #region 位移


    #endregion

    #region 外部事件

    /// <summary>被“可打断骑兵冲锋”的攻击命中时调用（当前 = 蓄力攻击）。</summary>

    #endregion

    private void SetPhase(CavalryPhase phase, string reason = "")
    {
        Phase = phase;
        LastReason = reason;
        CurrentAnimationStage = phase switch
        {
            CavalryPhase.Windup => AnimationStage.Windup,
            CavalryPhase.Charging => AnimationStage.Charging,
            CavalryPhase.Striking => AnimationStage.StrikeWindup,
            CavalryPhase.Retreating => AnimationStage.Retreating,
            CavalryPhase.RetreatBlocked => AnimationStage.RetreatBlocked,
            CavalryPhase.Dismounted => AnimationStage.Dismounted,
            _ => AnimationStage.Idle
        };
        OnStateChanged?.Invoke(this);
    }

    private void SetAnimationStage(AnimationStage stage)
    {
        if (CurrentAnimationStage == stage) return;
        CurrentAnimationStage = stage;
        OnStateChanged?.Invoke(this);
    }

    private void Diag(string eventName, string detail = "")
    {
        if (_enemy == null) return;
        Debug.Log($"[CavalryDiag] {eventName} {_enemy.DebugTag} col={_enemy.columnIndex} phase={Phase} state={_enemy.state} row={_enemy.rowIndex} mounted={_mounted} origin={_chargeOriginRow} grids={_chargeGrids} committed={_damageCommitted} {detail}");
    }
}
