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

    [Tooltip("路线选择优先点的选项 id（例：J1_N2 / J1_EV；空 = 点第一个可点的）")]
    public string preferredRouteChoiceId = "";

    [Header("埋点")]
    public bool recordTelemetry = true;

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
            return;
        }

        // 前排 ≥2 只 → 横划（斩击）打整排（模拟「会用范围技」）
        if (useAreaAttacks && !moveBusy && CountFrontRow() >= 2)
        {
            _nextAttackTime = Time.time + attackInterval;
            _input.SubmitGestureForDebug(MoveGesture.SwipeHorizontal, 0, -1, CenterScreenPoint(), true, 0f);
            return;
        }

        // 蓄力重击：横划（斩击）——Sweep 已被卸下，上挑只有 2 伤，不当「重击」用
        if (useHeavyAttacks && !moveBusy && Random.value < heavyChance)
        {
            _nextAttackTime = Time.time + attackInterval;
            _usedHeavyThisWave = true;
            _input.SubmitGestureForDebug(MoveGesture.SwipeHorizontal, 0, -1, CenterScreenPoint(), true, 0f);
            return;
        }

        // 默认：点一下（戳击 / 招式表的连招起手）
        _nextAttackTime = Time.time + attackInterval;
        _input.SubmitGestureForDebug(MoveGesture.Tap, 0, column, CenterScreenPoint(), true, 0f);
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

    // ───────── 挡路 UI（三选一 / 路线选择） ─────────

    /// <summary>机器人也要会点 UI：升级三选一与路线选择都不点 → 游戏会暂停在原地。</summary>
    private void HandleBlockingUi()
    {
        if (Time.time < _nextUiClickTime) return;

        var um = _ucm;
        if (um != null && um.IsChoosing && _pendingChoices.Count > 0)
        {
            var pick = _pendingChoices[Random.Range(0, _pendingChoices.Count)];
            _pendingChoices.Clear();
            _nextUiClickTime = Time.time + 0.2f;
            um.ConfirmChoice(pick);
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
            _nextUiClickTime = Time.time + 0.5f;
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
