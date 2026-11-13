using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// **跑关机器人（开发工具）**：自动打完关卡并把埋点写出来。
///
/// 用途：给「关卡难度调参」提供一个**机械基线**——它不躲、不走位，
/// 所以它的清场时长 ≈ 该波在「理论最低操作水平」下的耗时。
/// 拿它跟设计目标（普通波 30~55s / Boss 波 45~90s）比，就能看出 hp× 是高了还是低了；
/// 真人试玩的埋点（同一个 jsonl 格式）再补上「操作水平」这一维。
///
/// 用法：把本组件挂到场景任意对象 → Play → 勾 autoStart（或按 P 键）→ 自动开打。
/// 产物：<c>Library/Locus/telemetry/*.jsonl</c>（用 Tools/analyze_level_telemetry.py 分析）。
///
/// 2026-10-25 修正：
///   • **走手势路径**（<c>InputManager.SubmitGestureForDebug</c>）而不是直接调 TryExecuteAttack——
///     与玩家输入同一条路，命中招式表（连招能正常接段），测出来的才是真实手感。
///   • **跨节点续跑**：一场战斗结束后不退出，继续接下一节点，直到整关跑完或按 P 停。
///   • **波次边界接 WaveSpawner 的 OnWaveStarted / OnWaveCompleted**，不再用「存活数变化」推断。
///   • **胜负口径修正**：battle_end 的 victory 取真实结果（不是恒 false），hpLost 是该波内掉血。
///   • **Sweep 已从张飞配置卸下**（2026-10-25），原来的「蓄力重击」分支有一半打空 → 改用横划（斩击）。
///
/// 注意：它会**接管输入**（自己出招），只建议在调试关卡里用。
/// </summary>
public class BattleAutoPlayer : MonoBehaviour
{
    /// <summary>BOSS 的 QTE 怎么处理。</summary>
    public enum BossQTEMode
    {
        /// <summary>关掉 BOSS 的 QTE：机器人不会答 QTE（答错会掉血甚至暴毙），关掉才能量到干净的 DPS。</summary>
        Suppress = 0,
        /// <summary>机器人按 QTE 配置自动作答（点/划）：更接近会玩的玩家，但仍有被打死的风险。</summary>
        Answer = 1,
    }

    [Header("开关")]
    [Tooltip("Play 后自动开打")]
    public bool autoStart = true;

    [Tooltip("按 P 键手动开打 / 停")]
    public KeyCode toggleKey = KeyCode.P;

    [Header("打法")]
    [Tooltip("出招间隔（秒）")]
    public float attackInterval = 0.35f;

    [Tooltip("清完一波后到下一波之间的空窗（秒）")]
    public float waveGap = 0.2f;

    [Tooltip("前排有 2 只以上时改用横划（斩击，打整排）——模拟「会用范围技」的操作水平")]
    public bool useAreaAttacks = false;

    [Tooltip("混入蓄力重击（上挑/横划），用来对比不同操作水平的耗时")]
    public bool useHeavyAttacks = false;

    [Tooltip("用重击的概率（0~1）")]
    [Range(0f, 1f)] public float heavyChance = 0.25f;

    [Tooltip("BOSS 的 QTE 处理方式（测量时选 Suppress 最稳）")]
    public BossQTEMode bossQTEMode = BossQTEMode.Suppress;

    [Header("防御")]
    [Tooltip("是否用防御键格挡（走 DefenseButton 的**真实指针路径**：pointerDown/pointerUp）\n" +
             "→ 预输入、静音、区域识别、防御与出招互斥 这些规则会被一起压到")]
    public bool useDefense = true;

    [Tooltip("敌人进入攻击前摇时按住防御的概率（0~1）")]
    [Range(0f, 1f)] public float defendChance = 0.85f;

    [Tooltip("单次防御最长按住时间（秒）")]
    public float defendHoldSeconds = 1.6f;

    [Tooltip("路线选择优先点的选项 id（例：J1_N2 / J1_EV；空 = 点第一个可点的）")]
    public string preferredRouteChoiceId = "";

    [Header("埋点")]
    public bool recordTelemetry = true;

    [Header("真实流程（模拟玩家）")]
    [Tooltip("落地页就绪后**自己点「开始关卡」**（走 UI 指针路径），不直接调路线 API")]
    public bool clickHubStartButton = true;

    [Tooltip("战斗中自查「这一轮到底进没进关卡态」：路线还停在就绪态 = 落地页 UI 一定还挂在屏上\n" +
             "（之前用 StartRouteBattle 直开战斗就踩过这个坑：看着像在打，其实整个落地页 UI 都在）")]
    public bool assertInBattleState = true;

    [Tooltip("多少秒内没有任何进展（波次/存活数/出招数/UI 点击数全不变）就判定卡死")]
    public float stallSeconds = 25f;

    [Tooltip("卡死时截图留证到 Library/Locus/tmp/bot/")]
    public bool captureScreenshots = true;

    private readonly List<Enemy> _scratch = new List<Enemy>();
    private readonly List<UpgradeDefinition> _pendingChoices = new List<UpgradeDefinition>();

    private InputManager _input;
    private PlayerState _player;
    private UpgradeChoiceManager _ucm;
    private WaveSpawner _waves;
    private PlayerMoveStateMachine _moveMachine;
    private QTEController _qte;

    private bool _running;
    private bool _battleActive;
    private bool _waveActive;
    private bool _usedHeavyThisWave;
    private bool _qteSuppressed;
    private bool _hooked;
    private int _waveIndex = -1;
    private int _killsThisWave;
    private int _waveStartEnemies;
    private float _nextAttackTime;
    private float _nextUiClickTime;

    // 防御状态（机器人自己按着防御键）
    private bool _defendPressed;
    private float _defendReleaseAt;
    private int _defendPressCount;
    private int _defendActiveFrames;
    private int _defendOkCount;
    private int _defendFailNoObject;
    private readonly System.Collections.Generic.Dictionary<string, int> _defendFailReasons = new System.Collections.Generic.Dictionary<string, int>();

    // 真实流程自查
    private string _progressSignature = "";
    private float _lastProgressTime;
    private int _stallCount;
    private int _notInBattleStateCount;
    private int _attackCount;
    private int _uiClickCount;
    private int _shotIndex;
    private readonly List<string> _issues = new List<string>();

    // ───────── 生命周期 ─────────

    private void OnEnable()
    {
        EnsureHooks();
        if (autoStart) StartSession();
    }

    private void OnDisable()
    {
        Unsubscribe();
        if (_battleActive)
        {
            LevelTelemetry.EndBattle(false, CurrentCoins());
            _battleActive = false;
        }
        _waveActive = false;
        _running = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (_running) StopSession();
            else StartSession();
        }
        EnsureHooks();
        // 机器人可能在**战斗已经开始之后**才被创建（测试脚本常见）→
        // 那就收不到「出波」事件 → 永远不会进入战斗状态 → 只会站着不动。
        DetectBattleInProgress();
        // 每秒一次的真实流程自查 + 卡死检测
        if (Time.unscaledTime >= _nextSelfCheckTime)
        {
            _nextSelfCheckTime = Time.unscaledTime + 1f;
            SelfCheck();
        }
    }

    private float _nextSelfCheckTime;

    // ── 自查：这一轮到底是不是「真的在玩」 ────────────────────────────────

    /// <summary>
    /// 每秒自查两件事：
    ///   ① **进没进关卡态** —— 路线还停在「就绪态」= 落地页专属 UI（路牌入口 / 天赋入口 / 开始关卡 / 调试按钮）全挂在屏上
    ///      → 这样的对局**不像真实玩家**，必须报出来。
    ///   ② **有没有卡死** —— 波次 / 存活数 / 出招数 / UI 点击数 全部不变超过 stallSeconds → 报一条并截图。
    /// </summary>
    private void SelfCheck()
    {
        if (!_running) return;

        // ① 关卡态自查
        if (assertInBattleState && _battleActive)
        {
            var runtime = RouteFlowHub.Current as FakeRouteRuntime;
            if (runtime != null && runtime.IsPrepared)
            {
                _notInBattleStateCount++;
                string msg = "[AutoPlayer] ⚠ 战斗中路线仍停在「就绪态」→ 落地页 UI（路牌入口/天赋入口/开始关卡/调试按钮）还挂在屏上，这一轮不像真实对局";
                if (_notInBattleStateCount <= 3) Debug.LogWarning(msg);
                _issues.Add(msg);
            }
        }

        // ② 卡死检测
        string sig = BuildProgressSignature();
        if (sig != _progressSignature)
        {
            _progressSignature = sig;
            _lastProgressTime = Time.unscaledTime;
            return;
        }
        if (stallSeconds > 0f && Time.unscaledTime - _lastProgressTime > stallSeconds)
        {
            _stallCount++;
            _lastProgressTime = Time.unscaledTime;   // 每 stallSeconds 报一次，不要刷屏
            string msg = $"[AutoPlayer] ⚠ 卡死 {stallSeconds:F0}s 无进展：{sig}";
            Debug.LogWarning(msg);
            _issues.Add(msg);
            CaptureShot($"stall_{_stallCount}");
        }
    }

    /// <summary>进展签名：变了就算有进展。</summary>
    private string BuildProgressSignature()
    {
        int alive = 0;
        var cm = ColumnManager.Instance;
        if (cm != null)
        {
            foreach (var e in cm.GetAllEnemies())
                if (e != null && e.state != EnemyState.Dead) alive++;
        }
        var runtime = RouteFlowHub.Current as FakeRouteRuntime;
        string phase = runtime != null ? runtime.Phase.ToString() : "-";
        string node = runtime != null ? runtime.CurrentNodeId : "-";
        int wave = _waves != null ? _waves.CurrentWaveIndex : -1;
        return $"phase={phase} node={node} wave={wave} alive={alive} atk={_attackCount} ui={_uiClickCount}";
    }

    /// <summary>截图留证（卡死 / 关键节点）。</summary>
    private void CaptureShot(string tag)
    {
        if (!captureScreenshots) return;
        try
        {
            const string dir = "Library/Locus/tmp/bot";
            System.IO.Directory.CreateDirectory(dir);
            string path = $"{dir}/{_shotIndex:00}_{tag}.png";
            _shotIndex++;
            ScreenCapture.CaptureScreenshot(path);
            _issues.Add($"[截图] {path}");
        }
        catch (System.Exception e) { Debug.LogWarning("[AutoPlayer] 截图失败: " + e.Message); }
    }

    /// <summary>接管一段**已经在进行中**的战斗（没收到出波事件时的兜底）。</summary>
    private void DetectBattleInProgress()
    {
        if (_battleActive) return;
        if (_waves == null) return;
        if (_waves.CurrentWaveIndex < 0) return;
        if (_waves.IsAllWavesCompleted) return;
        _battleActive = true;
        _waveActive = true;
        _waveIndex = _waves.CurrentWaveIndex;
        Debug.Log($"[AutoPlayer] 接管进行中的战斗：wave={_waveIndex}");
    }

    private void StartSession()
    {
        if (_running) return;
        _running = true;
        StartCoroutine(RunSession());
    }

    private void StopSession()
    {
        _running = false;
        if (_battleActive)
        {
            LevelTelemetry.EndBattle(false, CurrentCoins());
            _battleActive = false;
        }
        _waveActive = false;
        WriteRunReport();
    }

    /// <summary>
    /// 跑完写一份报告（<c>Library/Locus/tmp/bot/run-report.txt</c>）：
    /// 关键统计 + 所有自查到的问题（不像真实对局 / 卡死 / 截图路径）。
    /// </summary>
    private void WriteRunReport()
    {
        try
        {
            const string dir = "Library/Locus/tmp/bot";
            System.IO.Directory.CreateDirectory(dir);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== 机器人跑测报告 ===");
            sb.AppendLine($"出招次数={_attackCount}  UI 点击次数={_uiClickCount}");
            sb.AppendLine($"防御：按下={_defendPressCount} 生效={_defendOkCount} 防御生效帧={_defendActiveFrames} 找不到按钮={_defendFailNoObject}");
            foreach (var kv in _defendFailReasons) sb.AppendLine($"   防御失败：{kv.Key} ×{kv.Value}");
            sb.AppendLine($"卡死次数={_stallCount}  「不在关卡态」次数={_notInBattleStateCount}");
            sb.AppendLine($"问题清单（{_issues.Count}）：");
            for (int i = 0; i < _issues.Count; i++) sb.AppendLine($"  {i + 1}. {_issues[i]}");
            string path = $"{dir}/run-report.txt";
            System.IO.File.WriteAllText(path, sb.ToString());
            Debug.Log($"[AutoPlayer] 报告 → {path}\n{sb}");
        }
        catch (System.Exception e) { Debug.LogWarning("[AutoPlayer] 写报告失败: " + e.Message); }
    }

    /// <summary>
    /// 会话主循环：处理「挡路的 UI」+ 有敌人就出招。
    /// 战斗的起止由 WaveSpawner 的事件决定，本循环不负责「战斗结束就退出」——
    /// 那样机器人只能打完一个节点就死（节点间旅行会把关卡状态切走）。
    /// </summary>
    private IEnumerator RunSession()
    {
        if (recordTelemetry) LevelTelemetry.Enabled = true;
        while (_running)
        {
            HandleBlockingUi();
            if (_battleActive) TickCombat();
            yield return null;
        }
    }

    // ───────── 战斗事件（波次边界 / 胜负） ─────────

    /// <summary>出波：同时也是「一场战斗开始」的信号（第一个波次）。</summary>
    private void OnWaveStarted(int waveIndex)
    {
        if (!_battleActive) BeginBattle();
        if (_waveActive) EndWave();
        _waveIndex = waveIndex;
        _waveActive = true;
        _usedHeavyThisWave = false;
        _killsThisWave = 0;
        _waveStartEnemies = 0;
        // 出波事件触发时敌人还没实例完 → 延后一帧再统计
        StartCoroutine(RecordWaveSnapshot(waveIndex));
    }

    /// <summary>
    /// 本波清空（WaveSpawner 的真实清波事件）。
    /// **必须按波号过滤**：WaveSpawner 在换波那一帧可能多发一次（上一波的最后一只死、
    /// 下一波还没注册时，「全部敌人已死」回调会再触发一次），那次带的是**新波号**，
    /// 直接记账就会把上一波重复记一遍、并让本波提前结算。
    /// </summary>
    private void OnWaveCompleted(int waveIndex)
    {
        if (waveIndex != _waveIndex) return;
        EndWave();
    }

    /// <summary>所有波次清空 → 这场战斗赢了。</summary>
    private void OnAllWavesCompleted()
    {
        EndBattle(true);
    }

    /// <summary>玩家阵亡 → 这场战斗输了。</summary>
    private void OnPlayerDied()
    {
        EndBattle(false);
    }

    /// <summary>玩家血量变化 → 累计整场掉血与死亡次数。</summary>
    private void OnPlayerHealthChanged(float current, float max)
    {
        LevelTelemetry.ReportHpChanged(current, max);
    }

    private void BeginBattle()
    {
        _battleActive = true;
        if (recordTelemetry) LevelTelemetry.Enabled = true;
        LevelTelemetry.BeginBattle(LabelOfCurrentBattle());
        Debug.Log($"[AutoPlayer] 战斗开始：{LabelOfCurrentBattle()}");
    }

    private void EndWave()
    {
        if (!_waveActive) return;
        _waveActive = false;
        int alive = CountAlive();
        int kills = Mathf.Max(0, _waveStartEnemies - alive);
        _killsThisWave += kills;
        LevelTelemetry.EndWave(alive, _usedHeavyThisWave, _killsThisWave);
    }

    private void EndBattle(bool victory)
    {
        if (!_battleActive) return;
        EndWave();
        _battleActive = false;
        LevelTelemetry.EndBattle(victory, CurrentCoins());
        Debug.Log($"[AutoPlayer] 战斗结束：{LabelOfCurrentBattle()} 胜利={victory}");
    }

    private System.Collections.IEnumerator RecordWaveSnapshot(int waveIndex)
    {
        yield return null;
        CollectEnemies();
        _waveStartEnemies = _scratch.Count;
        LevelTelemetry.BeginWave(waveIndex, _scratch.Count, CompositionOf(_scratch));
    }

    // ───────── 出手 ─────────

    private void TickCombat()
    {
        if (_input == null) return;

        // ── 防御优先：敌人起手时按住防御键（模拟玩家格挡）─────────────────
        //   防御与出招**互斥**（出招中不能防御 / 防御中不能出招）→ 这里先处理防御，再考虑出手。
        if (useDefense)
        {
            bool windingUp = IsEnemyWindingUp();
            if (!_defendPressed)
            {
                if (windingUp && Random.value < defendChance)
                {
                    PressDefend();
                    return;      // 这一帧不出招
                }
            }
            else
            {
                // 按住中：到时 / 敌人不再起手 → 松开（松开后下一帧才出手）
                if (Time.time >= _defendReleaseAt || !windingUp)
                {
                    ReleaseDefend();
                    _nextAttackTime = Time.time + 0.1f;
                    return;
                }
                if (PlayerState.Instance != null && PlayerState.Instance.IsDefendingActive)
                    _defendActiveFrames++;
                return;          // 继续按住，这一帧不出招
            }
        }

        if (Time.time < _nextAttackTime) return;

        // QTE 期间：不出招（输入被 QTE 系统接管），改为尝试作答
        if (TryHandleQTE()) return;

        CollectEnemies();
        if (_scratch.Count == 0)
        {
            // 兼底：本波敌人确实全死了 → 结算本波（不依赖 WaveSpawner 的事件时序）
            if (_waveActive && _waveStartEnemies > 0) EndWave();
            return;
        }

        Enemy target = FrontMostEnemy();
        if (target == null) return;

        int column = target.columnIndex;
        bool moveBusy = _moveMachine != null && _moveMachine.IsMoveActive;

        // 盾兵（1021）：**正面出手全被格挡**（只剩 1 点伤害）→ 必须蓄力上挑破盾。
        // 但破盾后它进入胆怯（受伤 ×1.5），这时继续上挑就是白砍（上挑只有 2 伤）→ 改用平A。
        if (IsShieldGuarding(target))
        {
            if (moveBusy) return;
            _nextAttackTime = Time.time + attackInterval;
            _usedHeavyThisWave = true;
            _input.SubmitGestureForDebug(MoveGesture.SwipeVertical, 1, column, CenterScreenPoint(), true, 0f);
            _attackCount++;
            return;
        }

        // 教学拍指定的招式：**按闸门给对应手势**（点一下 / 横划 / 竖划 / 连招蓄满松手）。
        // 教学关外的正式关 → 闸门是 None → 走机器人自己的打法。
        var teachTarget = _input != null ? _input.tutorialGestureTarget : TutorialGestureTarget.None;
        if (teachTarget == TutorialGestureTarget.BlockAll) return;   // 这一拍不许出招
        if (TryGetTutorialGesture(out var teachGesture, out var teachCharge))
        {
            // 连招蓄力拍：招式状态机本来就该是「进行中」（等的就是接续窗口）→ 不能被 moveBusy 拦住
            bool forceAct = teachTarget == TutorialGestureTarget.ComboHold;
            if (!moveBusy || forceAct)
            {
                _nextAttackTime = Time.time + attackInterval;
                _input.SubmitGestureForDebug(teachGesture, teachCharge, column, CenterScreenPoint(), true, 0f);
                _attackCount++;
            }
            return;
        }

        // 前排 ≥2 只 → 横划（斩击）打整排（模拟「会用范围技」）
        if (useAreaAttacks && !moveBusy && CountFrontRow() >= 2)
        {
            _nextAttackTime = Time.time + attackInterval;
            _input.SubmitGestureForDebug(MoveGesture.SwipeHorizontal, 0, -1, CenterScreenPoint(), true, 0f);
            _attackCount++;
            return;
        }

        // 蓄力重击：横划（斩击）——Sweep 已被卸下，上挑只有 2 伤，不当「重击」用
        if (useHeavyAttacks && !moveBusy && Random.value < heavyChance)
        {
            _nextAttackTime = Time.time + attackInterval;
            _usedHeavyThisWave = true;
            _input.SubmitGestureForDebug(MoveGesture.SwipeHorizontal, 0, -1, CenterScreenPoint(), true, 0f);
            _attackCount++;
            return;
        }

        // 默认：点一下（戳击 / 招式表的连招起手）
        _nextAttackTime = Time.time + attackInterval;
        _input.SubmitGestureForDebug(MoveGesture.Tap, 0, column, CenterScreenPoint(), true, 0f);
        _attackCount++;
    }

    /// <summary>
    /// 当前教学拍要求的手势（读 <c>InputManager.tutorialGestureTarget</c>）。
    /// 返回 false = 没有教学闸门（正式关）→ 机器人按自己的打法来。
    /// </summary>
    private bool TryGetTutorialGesture(out MoveGesture gesture, out int charge)
    {
        gesture = MoveGesture.Tap;
        charge = 0;
        if (_input == null) return false;
        switch (_input.tutorialGestureTarget)
        {
            case TutorialGestureTarget.Tap:
                gesture = MoveGesture.Tap; charge = 0; return true;
            case TutorialGestureTarget.SwipeHorizontal:
                gesture = MoveGesture.SwipeHorizontal; charge = 0; return true;
            case TutorialGestureTarget.ChargedVertical:
                gesture = MoveGesture.SwipeVertical; charge = 1; return true;
            case TutorialGestureTarget.ParryVertical:
                gesture = MoveGesture.SwipeVertical; charge = 0; return true;
            case TutorialGestureTarget.ComboHold:
                // 连招蓄力：蓄满后松手（在敌人那一列上）
                gesture = MoveGesture.Hold; charge = 1; return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// 当前是不是「要划一下」的教学拍。
    /// 教学拍会把手势闸门设成 <see cref="TutorialGestureTarget.SwipeHorizontal"/>（招式解锁引导拍）→
    /// 机器人这时必须**划**，否则这一拍收不了（和教点戳同一条机制）。
    /// </summary>
    private bool NeedsSwipeNow()
    {
        if (_input == null) return false;
        return _input.tutorialGestureTarget == TutorialGestureTarget.SwipeHorizontal;
    }

    /// <summary>QTE 处理：Suppress 模式下把 BOSS 的 QTE 关掉；Answer 模式下替玩家答一次。返回 true 表示「本帧已交给 QTE」。</summary>
    private bool TryHandleQTE()
    {
        if (_qte == null) return false;
        if (!_qte.IsQTEActive) return false;

        if (bossQTEMode == BossQTEMode.Answer)
        {
            _qte.TryAnswerQTEForDebug();
        }
        return true;
    }

    /// <summary>把 BOSS 的 QTE 数据清空（机器人不会答 QTE；留着只会一直吃失败伤害）。</summary>
    private void SuppressBossQTE()
    {
        if (_qteSuppressed) return;
        foreach (var e in _scratch)
        {
            if (e == null || !e.isBoss) continue;
            var controller = e.GetComponent<QTEController>();
            if (controller == null) continue;
            controller.SwitchQteData(null);
            _qteSuppressed = true;
            Debug.Log("[AutoPlayer] 已关闭 BOSS 的 QTE（测量模式）");
            return;
        }
    }

    // ── 防御（走 DefenseButton 的真实指针路径）─────────────────────────────

    /// <summary>敌人是否正在起手（AttackSpawn 前摇，还没进收招）—— 防御该按的时机。</summary>
    private bool IsEnemyWindingUp()
    {
        for (int i = 0; i < _scratch.Count; i++)
        {
            var e = _scratch[i];
            if (e == null || e.state != EnemyState.Attacking) continue;
            if (e.isAttackAnimating && !e.isAttackDrawPhase) return true;
        }
        return false;
    }

    /// <summary>
    /// 找场景里的防御键物件。
    ///
    /// ⚠️ **不直接引用 <c>DefenseButton</c> 类型**：它在 Locus.UI 程序集，
    /// 而本文件所在的 <c>Assets/Scripts/Utils</c> 属于 Locus.Flow → **Flow 不能依赖 UI**（分层铁律）。
    /// → 改成按名字找物件 + 用 <c>EventSystem</c> 的接口派发（按钮自己的静音 / 区域识别 / 预输入 逻辑照常跑）。
    /// </summary>
    private static GameObject FindDefenseButtonObject()
    {
        var go = GameObject.Find("DefenseButton");
        if (go != null) return go;
        foreach (var t in UnityEngine.Object.FindObjectsOfType<Transform>(true))
            if (t.name.Contains("DefenseButton")) return t.gameObject;
        return null;
    }

    /// <summary>按住防御键（pointerDown）。走按钮自己的逻辑：静音 / 区域识别 / 预输入 都由它判。</summary>
    private void PressDefend()
    {
        var go = FindDefenseButtonObject();
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (go == null || es == null) { _defendFailNoObject++; return; }
        var pos = DefenseButtonScreenPos(go);
        var ped = new UnityEngine.EventSystems.PointerEventData(es) { position = pos };
        UnityEngine.EventSystems.ExecuteEvents.Execute(go, ped, UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler);
        _defendPressed = true;
        _defendPressCount++;
        _defendReleaseAt = Time.time + defendHoldSeconds;

        // 诊断：按下后防御到底生效没有？卡在哪一步？
        var ps = PlayerState.Instance;
        bool ok = ps != null && ps.IsDefendingActive;
        if (ok) { _defendOkCount++; return; }
        string reason;
        if (ps != null && ps.Guard != null && ps.Guard.IsDefenseLocked) reason = "破防锁（防御键灰掉）→ 按不了";
        else if (ps != null && ps.Guard != null && ps.Guard.IsBroken) reason = "破防硬直中 → 按不了";
        else if (EnemyManager.TryGetEnemyAtScreenPoint(pos, Camera.main, out _))
            reason = "按钮位置下面有敌人 → 判定为攻击（不接管）";
        else if (ps == null) reason = "PlayerState 为空";
        else if (!ps.CanAcceptDefendIntent()) reason = "硬门拦下（非战斗态 / 输入门 / 三选一）";
        else if (!ps.CanStartDefend()) reason = "出招动作锁（软门）→ 进预输入";
        else reason = "未知（按钮静音 / 不可用 / 未派发事件）";
        _defendFailReasons[reason] = _defendFailReasons.TryGetValue(reason, out int n) ? n + 1 : 1;
    }

    /// <summary>松开防御键（pointerUp）。</summary>
    private void ReleaseDefend()
    {
        var go = FindDefenseButtonObject();
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (go != null && es != null)
        {
            var ped = new UnityEngine.EventSystems.PointerEventData(es) { position = DefenseButtonScreenPos(go) };
            UnityEngine.EventSystems.ExecuteEvents.Execute(go, ped, UnityEngine.EventSystems.ExecuteEvents.pointerUpHandler);
        }
        _defendPressed = false;
    }

    /// <summary>防御键在屏幕上的位置（当作手指按下的地方）。</summary>
    private static Vector2 DefenseButtonScreenPos(GameObject go)
    {
        var rt = go.GetComponent<UnityEngine.RectTransform>();
        if (rt == null) return Vector2.zero;
        var canvas = go.GetComponentInParent<Canvas>();
        var cam = canvas != null && canvas.renderMode != UnityEngine.RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        return RectTransformUtility.WorldToScreenPoint(cam, rt.TransformPoint(rt.rect.center));
    }

    /// <summary>机器人统计（跑完打印）。</summary>
    public string DebugStats
    {        get
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"防御按下={_defendPressCount} 生效={_defendOkCount} 防御生效帧={_defendActiveFrames} 找不到按钮={_defendFailNoObject}");
            foreach (var kv in _defendFailReasons) sb.Append($" | {kv.Key}×{kv.Value}");
            return sb.ToString();
        }
    }

    // ───────── 挡路 UI（三选一 / 路线选择） ─────────

    /// <summary>机器人也要会点 UI：升级三选一与路线选择都不点 → 游戏会暂停在原地。</summary>
    /// <summary>
    /// 落地页就绪 → **自己点「开始关卡」**（走 UI 指针路径，等价于玩家点一下）。
    ///
    /// 为什么必须走这一步：直接调 <c>StageController.StartRouteBattle</c> / <c>FakeRouteRuntime.RunPrepared</c>
    /// 都会让路线与 HUD 的状态对不上（落地页专属 UI 还挂在屏上、输入门没开）→
    /// **那一轮根本不是关卡态**，测出来的东西不作数。
    /// </summary>
    private bool TryClickHubStartButton()
    {
        var runtime = RouteFlowHub.Current as FakeRouteRuntime;
        if (runtime == null) return false;
        if (!runtime.IsPrepared) return false;
        if (runtime.Phase != FakeRoutePhase.Ready) return false;

        var go = GameObject.Find("StartStageButton");
        if (go == null || !go.activeInHierarchy) return false;
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es == null) return false;

        var ped = new UnityEngine.EventSystems.PointerEventData(es)
        {
            position = UiScreenPos(go),
            button = UnityEngine.EventSystems.PointerEventData.InputButton.Left
        };
        UnityEngine.EventSystems.ExecuteEvents.Execute(go, ped, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
        _uiClickCount++;
        _nextUiClickTime = Time.unscaledTime + 1f;
        Debug.Log("[AutoPlayer] 点「开始关卡」（落地页 → 关卡态）");
        return true;
    }

    /// <summary>某个 UI 物件在屏幕上的中心点（当作手指按下的地方）。</summary>
    private static Vector2 UiScreenPos(GameObject go)
    {
        var rt = go.GetComponent<UnityEngine.RectTransform>();
        if (rt == null) return Vector2.zero;
        var canvas = go.GetComponentInParent<Canvas>();
        var cam = canvas != null && canvas.renderMode != UnityEngine.RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        return RectTransformUtility.WorldToScreenPoint(cam, rt.TransformPoint(rt.rect.center));
    }

    private void HandleBlockingUi()
    {
        // ⚠️ 这里必须用 **unscaledTime**：三选一面板会把游戏暂停（timeScale=0），
        // 用 Time.time（缩放时间）的话时间会冻结 → 下面的「等一下再点」永远等不到 → 机器人卡死在升级三选一。
        if (Time.unscaledTime < _nextUiClickTime) return;

        // ── 落地页：自己点「开始关卡」（走 UI 指针路径）────────────────────
        //   之前用 StartRouteBattle 直开战斗 → 路线停在就绪态 → **落地页 UI 全挂在屏上**，
        //   看着像在打、其实根本不是关卡态 → 机器人必须自己走这一步才算「模拟玩家」。
        if (clickHubStartButton && TryClickHubStartButton()) return;

        var um = _ucm;
        if (um != null && um.IsChoosing && _pendingChoices.Count > 0)
        {
            var pick = _pendingChoices[Random.Range(0, _pendingChoices.Count)];
            _pendingChoices.Clear();
            _nextUiClickTime = Time.unscaledTime + 0.2f;
            _uiClickCount++;
            um.ConfirmChoice(pick);
            return;
        }

        // 对话气泡：引导/三选1 的台词是「点击继续」式 → 不点就把机器人卡死
        // （排在三选一**后面**：三选一面板会把游戏暂停（timeScale=0），更该先处理它）
        var dialogue = CombatPresenters.Dialogue;
        if (dialogue != null && dialogue.IsBusy)
        {
            _nextUiClickTime = Time.unscaledTime + 0.2f;
            _uiClickCount++;
            dialogue.Skip();
            return;
        }

        // 路线选择：只在真的进入选路阶段时扫按钮（避免每帧 FindObjectsOfType）
        var runtime = FakeRouteRuntime.Instance;
        if (runtime == null || !runtime.IsChoosing) return;

        // 指定了目标出口 → 直接走运行时的选择接口（比点按钮可靠：按钮可能还没 Bind 好）
        if (!string.IsNullOrEmpty(preferredRouteChoiceId))
        {
            if (runtime.TrySelectChoice(preferredRouteChoiceId)) return;
        }

        var buttons = UnityEngine.Object.FindObjectsOfType<RouteChoiceButton>();
        if (buttons == null || buttons.Length == 0) return;

        foreach (var b in buttons)
        {
            if (b == null || !b.gameObject.activeInHierarchy) continue;
            var btn = b.GetComponent<UnityEngine.UI.Button>();
            if (btn == null || !btn.interactable) continue;
            _nextUiClickTime = Time.unscaledTime + 0.5f;
            _uiClickCount++;
            btn.onClick.Invoke();
            break;
        }
    }

    private void OnChoicesReady(IList<UpgradeDefinition> choices)
    {
        _pendingChoices.Clear();
        if (choices != null) foreach (var c in choices) if (c != null) _pendingChoices.Add(c);
    }

    // ───────── 事件接线（幂等，容忍脚本启用顺序） ─────────

    private void EnsureHooks()
    {
        if (_input == null) _input = UnityEngine.Object.FindObjectOfType<InputManager>();
        if (_player == null) _player = PlayerState.Instance;
        if (_ucm == null)
        {
            _ucm = UpgradeChoiceManager.Instance;
            if (_ucm != null) _ucm.OnChoicesReady += OnChoicesReady;
        }
        if (_waves == null)
        {
            _waves = WaveSpawner.Instance;
            if (_waves != null)
            {
                _waves.OnWaveStarted += OnWaveStarted;
                _waves.OnWaveCompleted += OnWaveCompleted;
                _waves.OnAllWavesCompleted += OnAllWavesCompleted;
            }
        }
        if (_player == null) return;
        if (!_hooked)
        {
            _player.OnPlayerDied += OnPlayerDied;
            // 接血量变化 → 埋点里的「整场累计掉血 / 死亡次数」才有数
            _player.OnHealthChanged += OnPlayerHealthChanged;
            _hooked = true;
        }
        if (_moveMachine == null) _moveMachine = UnityEngine.Object.FindObjectOfType<PlayerMoveStateMachine>();
        if (_qte == null) _qte = UnityEngine.Object.FindObjectOfType<QTEController>();
    }

    private void Unsubscribe()
    {
        if (_ucm != null) _ucm.OnChoicesReady -= OnChoicesReady;
        if (_waves != null)
        {
            _waves.OnWaveStarted -= OnWaveStarted;
            _waves.OnWaveCompleted -= OnWaveCompleted;
            _waves.OnAllWavesCompleted -= OnAllWavesCompleted;
        }
        if (_player != null && _hooked)
        {
            _player.OnPlayerDied -= OnPlayerDied;
            _player.OnHealthChanged -= OnPlayerHealthChanged;
            _hooked = false;
        }
    }

    // ───────── 取数 ─────────

    private void CollectEnemies()
    {
        _scratch.Clear();
        var cm = ColumnManager.Instance;
        if (cm == null) return;
        foreach (var e in cm.GetAllEnemies())
        {
            if (e == null || e.state == EnemyState.Dead) continue;
            _scratch.Add(e);
        }
        if (bossQTEMode == BossQTEMode.Suppress) SuppressBossQTE();
    }

    private int CountAlive()
    {
        int n = 0;
        foreach (var e in _scratch) if (e != null && e.state != EnemyState.Dead) n++;
        return n;
    }

    /// <summary>最前排的敌人（rowIndex 最小）——机器人只打最前面的。</summary>
    private Enemy FrontMostEnemy()
    {
        Enemy best = null;
        int bestRow = int.MaxValue;
        foreach (var e in _scratch)
        {
            if (e == null || e.state == EnemyState.Dead) continue;
            if (e.rowIndex < bestRow) { bestRow = e.rowIndex; best = e; }
        }
        return best;
    }

    /// <summary>最前排上有几只（≥2 时斩击才划算）。</summary>
    private int CountFrontRow()
    {
        Enemy best = FrontMostEnemy();
        if (best == null) return 0;
        int row = best.rowIndex;
        int n = 0;
        foreach (var e in _scratch)
        {
            if (e == null || e.state == EnemyState.Dead) continue;
            if (e.rowIndex == row) n++;
        }
        return n;
    }

    /// <summary>盾兵是否还在「举盾」状态（举盾时正面出手全被格挡，只能上挑破盾）。</summary>
    private static bool IsShieldGuarding(Enemy e)
    {
        if (e == null) return false;
        if (e.enemyId != 1021 && !e.name.Contains("1021")) return false;
        var guard = e.GetComponent<ShieldGuardEnemy>();
        if (guard == null) return true;   // 没有 ShieldGuardEnemy 组件 → 当成普通盾兵处理
        return guard.IsShieldActive;
    }

    private static Vector2 CenterScreenPoint() => new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

    private static int CurrentCoins() => PlayerState.Instance != null ? PlayerState.Instance.coinCount : 0;

    private static string LabelOfCurrentBattle()
    {
        var state = StageRoles.BattleState;
        if (state != null && state.StageConfig != null) return state.StageConfig.name;
        return "unknown";
    }

    private static string CompositionOf(List<Enemy> enemies)
    {
        var counts = new Dictionary<string, int>();
        foreach (var e in enemies)
        {
            string key = EnemyKey(e);
            counts[key] = counts.TryGetValue(key, out int c) ? c + 1 : 1;
        }
        var parts = new List<string>();
        foreach (var kv in counts) parts.Add($"{kv.Key}x{kv.Value}");
        parts.Sort();
        return string.Join(" ", parts);
    }

    /// <summary>敌人编号（优先用 enemyId，回退到对象名）。</summary>
    private static string EnemyKey(Enemy e)
    {
        if (e == null) return "other";
        switch (e.enemyId)
        {
            case 1011:
            case 1021:
            case 109:
            case 105:
            case 104:
                return e.enemyId.ToString();
        }
        if (e.name.Contains("1011")) return "1011";
        if (e.name.Contains("1021")) return "1021";
        if (e.name.Contains("109")) return "109";
        if (e.name.Contains("105")) return "105";
        if (e.name.Contains("104")) return "104";
        return "other";
    }
}
