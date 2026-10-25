using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// **跑关机器人（开发工具）**：自动打完当前战斗并把埋点写出来。
///
/// 用途：给「关卡难度调参」提供一个**机械基线**——它不躲、不用蓄力重击、只平A点最近目标，
/// 所以它的清场时长 ≈ 该波在「理论最低操作水平」下的耗时。
/// 拿它跟设计目标（普通波 30~55s / Boss 波 45~90s）比，就能看出 hp× 是高了还是低了；
/// 真人试玩的埋点（同一个 jsonl 格式）再补上「操作水平」这一维。
///
/// 用法：把本组件挂到场景任意对象 → Play → 勾 autoStart（或按 P 键）→ 自动开打。
/// 产物：<c>Library/Locus/telemetry/*.jsonl</c>（用 Tools/analyze_level_telemetry.py 分析）。
///
/// 注意：它会**接管输入**（自己出招），只建议在调试关卡里用。
/// </summary>
public class BattleAutoPlayer : MonoBehaviour
{
    [Header("开关")]
    [Tooltip("Play 后自动开打")]
    public bool autoStart = true;

    [Tooltip("按 P 键手动开打")]
    public KeyCode toggleKey = KeyCode.P;

    [Header("打法")]
    [Tooltip("出招间隔（秒）")]
    public float attackInterval = 0.35f;

    [Tooltip("每波之间等待（秒）")]
    public float waveGap = 0.2f;

    [Tooltip("是否偶尔用蓄力重击（Wave/Launch），用来对比不同操作水平的耗时")]
    public bool useHeavyAttacks = false;

    [Tooltip("用重击的概率（0~1）")]
    [Range(0f, 1f)] public float heavyChance = 0.25f;

    [Tooltip("攻击命中判定：血量低于此比例才算命中（够用即可）")]
    [Range(0.05f, 0.5f)] public float damageThreshold = 0.15f;

    [Header("埋点")]
    public bool recordTelemetry = true;

    private readonly List<Enemy> _scratch = new List<Enemy>();
    private bool _running;
    private int _waveIndex = -1;
    private float _nextAttackTime;
    private bool _usedHeavyThisWave;
    private int _lastAlive = -1;
    private string _battleLabel = "";
    private bool _battleActive;
    private bool _waveActive;
    private InputManager _input;
    private readonly List<UpgradeDefinition> _pendingChoices = new List<UpgradeDefinition>();

    private void Awake()
    {
        _input = FindObjectOfType<InputManager>();
    }

    private void OnEnable()
    {
        var um = UpgradeChoiceManager.Instance;
        if (um != null) um.OnChoicesReady += OnChoicesReady;
        var ws = WaveSpawner.Instance;
        if (ws != null) ws.OnWaveStarted += OnWaveStarted;
        if (autoStart) StartCoroutine(RunBattle());
    }

    private void OnDisable()
    {
        var um = UpgradeChoiceManager.Instance;
        if (um != null) um.OnChoicesReady -= OnChoicesReady;
        var ws = WaveSpawner.Instance;
        if (ws != null) ws.OnWaveStarted -= OnWaveStarted;
    }

    /// <summary>波次边界用真实的出波事件（不用存活数推断，否则一波会被切成好几段）。</summary>
    private void OnWaveStarted(int waveIndex)
    {
        if (_waveActive) LevelTelemetry.EndWave(0, _usedHeavyThisWave);
        _waveIndex = waveIndex;
        _waveActive = true;
        _usedHeavyThisWave = false;
        // 出波事件触发时敌人还没实例完 → 延后一帧再统计
        StartCoroutine(RecordWaveSnapshot(waveIndex));
    }

    private System.Collections.IEnumerator RecordWaveSnapshot(int waveIndex)
    {
        yield return null;
        CollectEnemies();
        LevelTelemetry.BeginWave(waveIndex, _scratch.Count, CompositionOf(_scratch));
    }

    private void OnChoicesReady(System.Collections.Generic.IList<UpgradeDefinition> choices)
    {
        _pendingChoices.Clear();
        if (choices != null) foreach (var c in choices) if (c != null) _pendingChoices.Add(c);
    }

    /// <summary>机器人也要会点 UI：升级三选一与路线选择都不点 → 游戏会暂停在原地。</summary>
    private void HandleBlockingUi()
    {
        var um = UpgradeChoiceManager.Instance;
        if (um != null && um.IsChoosing && _pendingChoices.Count > 0)
        {
            var pick = _pendingChoices[Random.Range(0, _pendingChoices.Count)];
            _pendingChoices.Clear();
            um.ConfirmChoice(pick);
            return;
        }

        // 路线选择：点第一个可点的按钮
        var buttons = FindObjectsOfType<RouteChoiceButton>();
        if (buttons == null || buttons.Length == 0) return;
        foreach (var b in buttons)
        {
            if (b == null || !b.gameObject.activeInHierarchy) continue;
            var btn = b.GetComponent<UnityEngine.UI.Button>();
            if (btn == null || !btn.interactable) continue;
            btn.onClick.Invoke();
            break;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (_running) { _running = false; }
            else StartCoroutine(RunBattle());
        }
    }

    private IEnumerator RunBattle()
    {
        _running = true;
        _waveIndex = -1;
        _usedHeavyThisWave = false;
        _lastAlive = -1;
        _battleLabel = LabelOfCurrentBattle();
        if (recordTelemetry) LevelTelemetry.Enabled = true;
        LevelTelemetry.BeginBattle(_battleLabel);
        _battleActive = true;

        var attack = AttackSystem.Instance;
        var stage = StageController.Instance;

        while (_running && _battleActive)
        {
            HandleBlockingUi();
            // 战斗是否还在进行
            if (stage == null || PlayerState.Instance == null) break;
            var state = StageRoles.BattleState;
            if (state != null && !state.IsStageInProgress) break;

            CollectEnemies();
            int alive = _scratch.Count;

            // 本波敌人清空 → 结算本波
            if (_waveActive && alive == 0)
            {
                LevelTelemetry.EndWave(0, _usedHeavyThisWave);
                _waveActive = false;
            }

            if (alive > 0 && attack != null && Time.time >= _nextAttackTime)
            {
                _nextAttackTime = Time.time + attackInterval;
                Attack(attack, _scratch);
            }

            yield return null;
        }

        if (_battleActive)
        {
            if (_lastAlive > 0 && _waveIndex >= 0) LevelTelemetry.EndWave(_lastAlive, _usedHeavyThisWave);
            LevelTelemetry.EndBattle(false, 0);
        }
        _battleActive = false;
        _running = false;
    }

    private void Attack(AttackSystem attack, List<Enemy> enemies)
    {
        Enemy best = null;
        int bestRow = int.MaxValue;
        foreach (var e in enemies)
        {
            if (e == null || e.state == EnemyState.Dead) continue;
            if (e.rowIndex < bestRow) { bestRow = e.rowIndex; best = e; }
        }
        if (best == null) return;

        int column = best.columnIndex;
        bool heavy = useHeavyAttacks && Random.value < heavyChance;
        AttackType type = AttackType.Stab;
        // 盾兵（1021）正面出手全被格挡：必须用上挑破盾，否则机器人会永远打不动它
        bool isShield = best.name.Contains("1021");
        if (isShield)
        {
            type = AttackType.Launch;
            _usedHeavyThisWave = true;
        }
        else if (heavy)
        {
            type = Random.value < 0.5f ? AttackType.Sweep : AttackType.Launch;
            _usedHeavyThisWave = true;
        }
        attack.TryExecuteAttack(type, column);
    }

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
    }

    private static string CompositionOf(List<Enemy> enemies)
    {
        var counts = new Dictionary<string, int>();
        foreach (var e in enemies)
        {
            string key = e.name.Contains("1011") ? "1011"
                : e.name.Contains("1021") ? "1021"
                : e.name.Contains("109") ? "109"
                : e.name.Contains("105") ? "105"
                : e.name.Contains("104") ? "104" : "other";
            counts[key] = counts.TryGetValue(key, out int c) ? c + 1 : 1;
        }
        var parts = new List<string>();
        foreach (var kv in counts) parts.Add($"{kv.Key}x{kv.Value}");
        parts.Sort();
        return string.Join(" ", parts);
    }

    private static string LabelOfCurrentBattle()
    {
        var state = StageRoles.BattleState;
        if (state != null && state.StageConfig != null) return state.StageConfig.name;
        return "unknown";
    }
}
