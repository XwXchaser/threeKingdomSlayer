using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// **无限模式运行时**（2026-11-16 新增）。
///
/// ## 它是什么
/// 独立于「长坂坡剧情关」的第二种模式：沿同一条卷轴路线**循环**，每场战斗的波次由生成器现造，
/// 局内 build **跨圈保留**（用户定：不做「每圈重置」，用「升级上限软锁」代替）。
///
/// ## 推进纪律：**与剧情路线完全一致**（2026-11-16 用户纠正）
/// <code>
/// 抵达节点 → 节点战斗（生成波次）
///   → 战斗清空 → **显示出口面板（玩家点）** → 旅行演出 → 提交目标节点
///   → （提交回起点 = 完成一圈 → 圈结算面板 → 玩家点「继续」）
/// </code>
/// ⚠ 早先版本是「波清完自动进下一节点」，跳过了出口确认与旅行 —— 已按用户口径改回路线纪律。
///
/// ## 为什么不复用 <see cref="FakeRouteRuntime"/>
/// 路线运行时是**有限路线**的实现：节点列表有限、**禁止回访**、每节点写存档点。
/// 无限模式要的是**循环** + 局内进度不落盘 ⇒ 另起一条运行时，
/// 但**出口确认 UI 与旅行演出都复用**（<see cref="RouteChoicePanel"/> + <see cref="IRouteMotionPresenter"/>）。
///
/// ## 圈结构（近似用户给的拓扑）
/// <code>
/// N1 → E0 → J1 →┬ 左：EV → N3 →（出村）回 N1
///              └ 右：N2 →（折返）回 N1
/// </code>
/// J1 的左右由**玩家在出口面板上点**（左路：村庄石路 / 右路：官道）。
///
/// ## 分数
/// 击杀 / 波次 / 连击 / 无伤 / 无破防 / 圈里程碑 —— 见 <see cref="EndlessScoreLedger"/>。
///
/// ## 存档
/// 只存**最佳分**与**最高圈数**；局内进度不存（退出即清零）。
/// </summary>
public class EndlessRunController : MonoBehaviour
{
    public static EndlessRunController Instance { get; private set; }

    /// <summary>取或建一个运行时实例（场景里没有也能用）。</summary>
    public static EndlessRunController GetOrCreate()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("EndlessRunController");
        return go.AddComponent<EndlessRunController>();
    }

    [Header("起始难度（无尽模式专用）")]
    [Tooltip("第 1 圈的敌人血量倍率（<1 = 比剧情关轻松；剧情关是按“中局玩家”调的，冷启动直接用会打不过）")]
    public float startHpScale = 0.55f;
    [Tooltip("第 1 圈的敌人伤害倍率")]
    public float startDamageScale = 0.5f;
    [Tooltip("第 1 圈的敌人攻速倍率")]
    public float startAttackSpeedScale = 0.8f;
    [Tooltip("开局免费发放的三选一次数（起步 build）。\n" +
             "**默认 0** —— 2026-11-17 用户定：“去掉免费build”（没拍板过的临时加塞已撤）。\n" +
             "需要的难度曲线请改上面的起始倍率 / 每圈成长，不要用白送卡来拐弯。")]
    public int startingChoices = 0;

    [Header("阵型与可见性")]
    [Tooltip("**兜底阵型配置**（逐排半宽 / 排间距 / 逐排透明度）。\n" +
             "节点本身有手写关卡时用**它自己的**阵型；只有程序生成的节点（无手写配置）才用这份。\n" +
             "⚠ 留空 ⇒ 间距走代码兜底 (col-2)*2.0（平排、无梯形）、透明度走 [1,0.8,0.6,0.4,0.2]\n" +
             "⇒ 看起来就是“敌人间距和近远透明度完全错误”。\n" +
             "正解资产：`Assets/ScriptableObjects/StageFor/DefaultFormation.asset`。")]
    public FormationConfig fallbackFormation;

    [Header("内容来源")]
    [Tooltip("**true = 照抄节点自己的关卡配置**（用户手写的那几份：Formal_N1 / Formal_E0 / Formal_N2 / Formal_N3），\n" +
             "只按圈数改波次倍率；**false = 用程序生成器现造编队**。\n" +
             "默认走手写配置 —— 用户口径：“你之前配的关卡都很烂不如我配的”。")]
    public bool useAuthoredLevels = true;

    [Header("圈数难度缩放（只对手写配置生效）")]
    [Tooltip("每圈血量倍率（乘性）：圈 2 = ×1.6，圈 3 = ×2.56 …")]
    public float hpGrowthPerCircle = 1.6f;
    [Tooltip("每圈 BOSS 血量倍率（乘性，比杂兵温和）")]
    public float bossHpGrowthPerCircle = 1.25f;
    [Tooltip("每圈攻速倍率（乘性）")]
    public float attackSpeedGrowthPerCircle = 1.06f;
    [Tooltip("每圈伤害倍率（乘性）")]
    public float damageGrowthPerCircle = 1.04f;
    [Tooltip("攻速倍率上限")]
    public float maxAttackSpeedMultiplier = 1.5f;
    [Tooltip("伤害倍率上限")]
    public float maxDamageMultiplier = 2.0f;
    [Tooltip("BOSS 血量倍率上限")]
    public float maxBossHpMultiplier = 4f;

    [Header("生成（仅 useAuthoredLevels = false 时生效）")]
    [Tooltip("随机种子（同种子同关卡）。")]
    public int seed = 20261116;
    [Tooltip("每个节点的波数。")]
    public int wavesPerNode = 2;
    [Tooltip("每圈开头几个 N1 节点。")]
    public int n1NodesPerCircle = 2;
    [Tooltip("左路（村庄）的 N3 节点数。")]
    public int n3NodesPerBranch = 3;
    [Tooltip("右路（官道）的 N2 节点数。")]
    public int n2NodesPerBranch = 3;
    [Tooltip("节点之间的停顿（秒，不受 timeScale 影响）。")]
    public float nodeSettleDelay = 0.8f;

    [Header("路线（沿用剧情关的节点资产，只借用名字与视觉档）")]
    [Tooltip("起点节点 id（提交回它 = 完成一圈）。")]
    public string loopStartNodeId = "N1";
    [Tooltip("路线来源（提供节点与出口资产）。留空 = 自动找可选路线里的第一条。")]
    public FakeRouteStageConfig sourceRoute;

    // ── 运行时状态 ──
    public bool IsRunning { get; private set; }
    public int Circle { get; private set; } = 1;
    public string CurrentNodeName { get; private set; } = "";
    public string CurrentBranchName { get; private set; } = "";
    /// <summary>本圈当前节点在圈内的序号（1 起；显示用）。</summary>
    public int NodeIndex { get; private set; } = 1;
    /// <summary>本圈节点总数（显示用）。</summary>
    public int NodeCount { get; private set; } = 1;
    /// <summary>是否停在圈结算面板等玩家点「继续」。</summary>
    public bool IsWaitingForCircleChoice { get; private set; }
    public EndlessScoreLedger Ledger { get; } = new EndlessScoreLedger();

    // ── 事件（UI 层订阅；本类不碰任何 UI 对象）──
    public event Action OnStateChanged;
    public event Action<int, int> OnWaveSettled;
    public event Action<int, int> OnCircleSettled;
    public event Action<bool> OnRunEnded;

    /// <summary>无限模式的一条边（出口）。</summary>
    private sealed class EndlessEdge
    {
        public string choiceId;
        public string displayName;
        public FakeRouteNodeConfig target;
        /// <summary>回程边（几何闭环前用「黑幕 + 锚点跳转」代替真旅行）。</summary>
        public bool wrap;
        /// <summary>循环边（再打一轮同一个节点）；<see cref="maxLoops"/> = 本圈最多连续几次。</summary>
        public bool isLoop;
        public int maxLoops;
    }

    private readonly EndlessWaveGenerator _generator = new EndlessWaveGenerator();
    private Coroutine _flow;
    private readonly Dictionary<string, FakeRouteNodeConfig> _nodes = new Dictionary<string, FakeRouteNodeConfig>();
    private readonly Dictionary<string, List<EndlessEdge>> _edges = new Dictionary<string, List<EndlessEdge>>();
    private readonly Dictionary<string, int> _visitCounts = new Dictionary<string, int>();
    private string _currentNodeId = "N1";
    private int _circleWaveIndex;
    private int _circleWaveTotal;
    private bool _nodeCleared;
    private bool _waveActive;
    private float _waveStartTime;
    private float _waveStartHealth;
    private int _waveComboPeak;
    private EndlessEdge _pendingEdge;
    private StageConfig _formationSource;
    private bool _choicePanelOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ─────────────────────────── 入口 ───────────────────────────

    /// <summary>开始一局无限模式。</summary>
    public void StartEndlessRun()
    {
        if (IsRunning) return;

        IsRunning = true;
        IsWaitingForCircleChoice = false;
        Circle = 1;
        NodeIndex = 1;
        Ledger.Reset();
        Ledger.BeginCircle(Circle);

        if (!BuildRouteGraph())
        {
            Debug.LogError("[Endless] 找不到可用的路线节点资产（无限模式无法开始）");
            IsRunning = false;
            return;
        }

        _currentNodeId = loopStartNodeId;
        _circleWaveIndex = 0;
        CurrentBranchName = "";
        CurrentNodeName = NodeDisplayName(_currentNodeId);

        // 记住当前阵型配置（运行时生成的 StageConfig 要沿用）
        _formationSource = StageController.Instance != null ? StageController.Instance.stageConfig : null;

        ResetRunState();

        // 让落地页的「开始关卡」按钮隐藏 + **独占路线流程**（否则两套流程争同一个出口面板）
        FakeRouteRuntime.Instance?.SuspendForEndless();
        // 若剧情路线此刻正在跑（战斗 / 旅行），先把它停掉 → 否则两套流程并行
        StageController.Instance?.AbortRouteBattle();

        // ★ **接管输入门**（2026-11-17 修「无尽关整局做不了手势」）：
        //   路线让位时已把它自己那份 RouteTravel 拦截交还（见 SuspendForEndless），
        //   但没人接手的话玩家反而能乱挥 ⇒ 这里由无限模式自己当 owner：
        //   **准备阶段先拦住 → 进入节点战斗放开 → 出口面板 / 旅行期间再拦住**。
        SetGameplayInputAllowed(false);

        if (StageController.Instance != null) StageController.Instance.blockRestartOnDefeat = true;

        Subscribe();
        BeginCircle();

        // **起步 build**：开局免费发几次三选一（冷启动没有天赋/技能/装备，
        //   直接打剧情关难度会打不过 —— 用户 2026-11-16 实测反馈）。
        if (startingChoices > 0)
        {
            UpgradeChoiceManager.Instance?.GrantChoices(startingChoices);
            Debug.Log($"[Endless] 开局发放 {startingChoices} 次三选一（起步 build）");
        }
    }

    /// <summary>结束一局（玩家主动退出或战败）。</summary>
    public void EndRun(bool byDeath)
    {
        if (!IsRunning) return;
        Unsubscribe();
        if (_flow != null) { StopCoroutine(_flow); _flow = null; }
        RouteChoicePanel.HideCurrent();
        _choicePanelOpen = false;

        SaveBest();

        if (StageController.Instance != null) StageController.Instance.blockRestartOnDefeat = false;

        // 交还输入门（别把玩家永久锁住；回落地页时路线会重新拦）
        SetGameplayInputAllowed(true);

        IsRunning = false;
        IsWaitingForCircleChoice = false;
        _pendingEdge = null;

        Debug.Log($"[Endless] 结束（阵亡={byDeath}）{Ledger}");
        OnRunEnded?.Invoke(Ledger.TotalScore > 0);
        OnStateChanged?.Invoke();
    }

    /// <summary>玩家在圈结算面板点「继续」→ 下一圈。</summary>
    public void ContinueToNextCircle()
    {
        if (!IsWaitingForCircleChoice) return;
        IsWaitingForCircleChoice = false;
        Circle++;
        Ledger.BeginCircle(Circle);
        OnStateChanged?.Invoke();
    }

    /// <summary>玩家在圈结算面板点「退出」→ 回落地页。</summary>
    public void ExitToLanding()
    {
        if (!IsRunning) return;
        EndRun(false);
        // 回落地页：把路线运行时恢复成就绪态
        FakeRouteRuntime.Instance?.ResumePrepared();
    }

    // ─────────────────────────── 路线图 ───────────────────────────

    /// <summary>
    /// 从剧情关的路线资产里取出节点与出口，组装成无限模式的循环图。
    ///
    /// ⚠ **不动剧情关的资产**：共享节点资产里的 <c>outgoingChoices</c> 若加上「回 N1」，
    /// 剧情路线会多出两个出口（而且会被"禁止回访"规则锁掉）→ 所以回程边由无限模式自己持有。
    /// </summary>
    private bool BuildRouteGraph()
    {
        _nodes.Clear();
        _edges.Clear();

        var route = sourceRoute;
        if (route == null)
        {
            var catalog = RouteCatalog.Resolve();
            if (catalog != null)
            {
                var routes = catalog.GetSelectableRoutes();
                // 自动选路：取**节点最多**的那条（教学关只有 N1/E0 两个节点，且 nodeId 与正式关重名；
                // 按位置取第一条会挑中教学关 → 边全断）。
                for (int i = 0; i < routes.Count; i++)
                {
                    if (routes[i] == null || routes[i].nodes == null) continue;
                    if (route == null || routes[i].nodes.Count > route.nodes.Count) route = routes[i];
                }
            }
        }
        if (route == null || route.nodes == null) return false;

        for (int i = 0; i < route.nodes.Count; i++)
        {
            var n = route.nodes[i];
            if (n == null || string.IsNullOrEmpty(n.nodeId)) continue;
            _nodes[n.nodeId] = n;
        }

        // 现有出口（沿用剧情关的 choice 资产：编号 / 名字 / 目标节点 / 旅行表现）
        for (int i = 0; i < route.nodes.Count; i++)
        {
            var n = route.nodes[i];
            if (n == null || string.IsNullOrEmpty(n.nodeId)) continue;
            if (n.outgoingChoices == null) continue;
            for (int c = 0; c < n.outgoingChoices.Count; c++)
            {
                var choice = n.outgoingChoices[c];
                if (choice == null || choice.targetNode == null) continue;
                // 剧情关没接进来的右路（J1 → N2）在这里补上：资产早就存在，只是没挂到节点上
                AddEdge(n.nodeId, new EndlessEdge
                {
                    choiceId = choice.choiceId,
                    displayName = choice.displayName,
                    target = choice.targetNode,
                    wrap = false
                });
            }
        }

        // 剧情关里 J1 只挂了左路 → 无限模式补上右路（资产 Travel_J1_N2 已存在，挂在路线的 extraChoices 上）
        if (_nodes.ContainsKey("J1") && !HasEdge("J1", "N2"))
        {
            var right = FindChoiceAsset("j1_to_n2");
            if (right != null)
            {
                AddEdge("J1", new EndlessEdge
                {
                    choiceId = right.choiceId,
                    displayName = right.displayName,
                    target = right.targetNode,
                    wrap = false
                });
            }
            else
            {
                Debug.LogWarning("[Endless] 找不到右路出口资产（j1_to_n2）→ J1 只有左路");
            }
        }

        // 回程边（几何闭环做好之前 = 黑幕跳转；闭环之后改成真旅行）
        AddEdge("N3", new EndlessEdge { choiceId = "n3_to_n1", displayName = "出村：回到当阳", target = _nodes.ContainsKey("N1") ? _nodes["N1"] : null, wrap = true });
        AddEdge("N2", new EndlessEdge { choiceId = "n2_to_n1", displayName = "官道折返：回到当阳", target = _nodes.ContainsKey("N1") ? _nodes["N1"] : null, wrap = true });

        // 循环边（用户给的圈拓扑：N1 ×k / N3 ×3 / N2 ×3）
        if (_nodes.ContainsKey("N1"))
            AddEdge("N1", new EndlessEdge { choiceId = "n1_loop", displayName = "再战一轮：当阳乱军", target = _nodes["N1"], isLoop = true, maxLoops = Mathf.Max(1, n1NodesPerCircle) });
        if (_nodes.ContainsKey("N3"))
            AddEdge("N3", new EndlessEdge { choiceId = "n3_loop", displayName = "再战一轮：村落搜救", target = _nodes["N3"], isLoop = true, maxLoops = Mathf.Max(1, n3NodesPerBranch) });
        if (_nodes.ContainsKey("N2"))
            AddEdge("N2", new EndlessEdge { choiceId = "n2_loop", displayName = "再战一轮：官道断后", target = _nodes["N2"], isLoop = true, maxLoops = Mathf.Max(1, n2NodesPerBranch) });

        if (!_nodes.ContainsKey(loopStartNodeId)) return false;
        return true;
    }

    private void AddEdge(string fromNodeId, EndlessEdge edge)
    {
        if (string.IsNullOrEmpty(fromNodeId) || edge == null || edge.target == null) return;
        if (!_edges.TryGetValue(fromNodeId, out var list))
        {
            list = new List<EndlessEdge>();
            _edges[fromNodeId] = list;
        }
        for (int i = 0; i < list.Count; i++)
            if (list[i].choiceId == edge.choiceId) return;
        list.Add(edge);
    }

    private bool HasEdge(string fromNodeId, string targetNodeId)
    {
        if (!_edges.TryGetValue(fromNodeId, out var list)) return false;
        for (int i = 0; i < list.Count; i++)
            if (list[i].target != null && list[i].target.nodeId == targetNodeId) return true;
        return false;
    }

    /// <summary>在剧情关的资产里按 choiceId 找出口（含路线上的 extraChoices —— 右路就挂在那里）。</summary>
    private FakeRouteChoiceConfig FindChoiceAsset(string choiceId)
    {
        var catalog = RouteCatalog.Resolve();
        if (catalog == null) return null;
        var routes = catalog.GetSelectableRoutes();
        for (int r = 0; r < routes.Count; r++)
        {
            if (routes[r] == null) continue;

            if (routes[r].extraChoices != null)
            {
                for (int e = 0; e < routes[r].extraChoices.Count; e++)
                {
                    var ch = routes[r].extraChoices[e];
                    if (ch != null && ch.choiceId == choiceId) return ch;
                }
            }

            if (routes[r].nodes == null) continue;
            for (int i = 0; i < routes[r].nodes.Count; i++)
            {
                var n = routes[r].nodes[i];
                if (n == null || n.outgoingChoices == null) continue;
                for (int c = 0; c < n.outgoingChoices.Count; c++)
                {
                    var ch = n.outgoingChoices[c];
                    if (ch != null && ch.choiceId == choiceId) return ch;
                }
            }
        }
        return null;
    }

    private string NodeDisplayName(string nodeId)
    {
        if (_nodes.TryGetValue(nodeId, out var n) && n != null) return n.displayName;
        return nodeId;
    }

    /// <summary>本圈该节点已被提交过几次（用于限制循环边的重复次数）。</summary>
    private int GetVisitCount(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)) return 0;
        return _visitCounts.TryGetValue(nodeId, out int c) ? c : 0;
    }

    private void AddVisitCount(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)) return;
        _visitCounts[nodeId] = GetVisitCount(nodeId) + 1;
    }

    // ─────────────────────────── 圈 / 节点 ───────────────────────────

    private void BeginCircle()
    {
        _circleWaveIndex = 0;
        NodeIndex = 1;
        CurrentBranchName = "";
        _visitCounts.Clear();

        // 本圈节点数（左路 / 右路长度不同 → 面板显示的总数也不同）
        NodeCount = CountCircleNodes();

        Debug.Log($"[Endless] 第 {Circle} 圈开始（起点 {CurrentNodeName}，节点数 {NodeCount}）");

        if (_flow != null) StopCoroutine(_flow);
        _flow = StartCoroutine(RunLoop());
    }

    private int CountCircleNodes()
    {
        int count = 0;
        string id = _currentNodeId;
        var visited = new HashSet<string>();
        for (int guard = 0; guard < 32; guard++)
        {
            if (!_edges.TryGetValue(id, out var list) || list == null || list.Count == 0) break;
            // J1 有两条出口（左 / 右）→ 本圈按"左路"计长度（右路长度不同，面板总数会有出入，暂可接受）
            var edge = list[0];
            count++;
            if (edge == null || edge.target == null) break;
            id = edge.target.nodeId;
            if (!visited.Add(id)) break;   // 回到起点 → 一圈结束
            if (id == loopStartNodeId) { count++; break; }
        }
        return Mathf.Max(1, count);
    }

    private IEnumerator RunLoop()
    {
        while (IsRunning)
        {
            // ── 节点战斗（波次由生成器现造）──
            _nodeCleared = false;
            StartNodeBattle();
            OnStateChanged?.Invoke();

            while (!_nodeCleared && IsRunning) yield return null;
            if (!IsRunning) yield break;

            // ── 出口确认（与剧情路线同一套面板与同一套纪律）──
            yield return ShowExitAndWaitChoice();
            if (!IsRunning) yield break;
            var edge = _pendingEdge;
            _pendingEdge = null;
            if (edge == null) yield break;

            // ── 旅行 → 提交目标节点 ──
            yield return PlayTravel(edge);
            if (!IsRunning) yield break;

            _currentNodeId = edge.target != null ? edge.target.nodeId : _currentNodeId;
            CurrentNodeName = NodeDisplayName(_currentNodeId);
            CurrentBranchName = _currentNodeId == "N3" ? "左路·村庄" : (_currentNodeId == "N2" ? "右路·官道" : CurrentBranchName);
            AddVisitCount(_currentNodeId);

            // 走完**回程边** = 完成一圈（⚠ 不能用“当前节点是起点”判定：
            //   循环边（N1 → N1）也会把节点指回起点，但那一圈还没走完）
            if (edge.wrap)
            {
                // ⚠ 必须先置标记再广播：HUD 收到 OnStateChanged 时会看这个标记决定收不收起结算面板
                IsWaitingForCircleChoice = true;
                int circleGain = Ledger.SettleCircle();
                OnCircleSettled?.Invoke(circleGain, Ledger.TotalScore);
                OnStateChanged?.Invoke();

                while (IsWaitingForCircleChoice && IsRunning) yield return null;
                if (!IsRunning) yield break;

                Circle++;
                Ledger.BeginCircle(Circle);
                BeginCircle();
                yield break;   // BeginCircle 已经重新起了协程
            }

            // 节点之间的停顿（unscaled，避免三选一暂停时卡住）
            float wait = nodeSettleDelay;
            while (wait > 0f && IsRunning)
            {
                wait -= Time.unscaledDeltaTime;
                yield return null;
            }
            NodeIndex++;
            OnStateChanged?.Invoke();
        }
    }

    /// <summary>显示出口面板并等玩家选（与剧情路线共用 <see cref="RouteChoicePanel"/>）。</summary>
    private IEnumerator ShowExitAndWaitChoice()
    {
        _pendingEdge = null;

        // ★ 出口面板期间拦下手势输入（面板按钮是 UI，不受影响）
        SetGameplayInputAllowed(false);

        if (!_edges.TryGetValue(_currentNodeId, out var list) || list == null || list.Count == 0)
        {
            Debug.LogWarning("[Endless] 节点 " + _currentNodeId + " 没有出口 → 本圈结束");
            yield break;
        }

        var panelPrefab = FakeRouteRuntime.Instance != null ? FakeRouteRuntime.Instance.ChoicePanelPrefab : null;
        if (panelPrefab == null)
        {
            Debug.LogError("[Endless] 找不到出口面板预制体 → 无法显示出口确认");
            yield break;
        }

        var options = new List<RouteChoiceOption>();
        for (int i = 0; i < list.Count; i++)
        {
            var e = list[i];
            if (e == null || e.target == null) continue;
            // 循环边：本圈的重复次数用满了就不再给（用户拓扑：N1 ×k / N3 ×3 / N2 ×3）
            if (e.isLoop && GetVisitCount(e.target.nodeId) >= e.maxLoops) continue;
            options.Add(new RouteChoiceOption
            {
                id = e.choiceId,
                label = e.displayName,
                description = NodeDisplayName(e.target.nodeId)
            });
        }

        RouteChoicePanel.Show(panelPrefab, CurrentNodeName, options, OnChoicePicked);
        _choicePanelOpen = true;

        while (_pendingEdge == null && IsRunning) yield return null;
        RouteChoicePanel.HideCurrent();
        _choicePanelOpen = false;
    }

    private void OnChoicePicked(string choiceId)
    {
        if (!_edges.TryGetValue(_currentNodeId, out var list) || list == null) return;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null && list[i].choiceId == choiceId)
            {
                _pendingEdge = list[i];
                return;
            }
        }
    }

    /// <summary>
    /// 旅行演出：普通边走表现层的 <see cref="IRouteMotionPresenter.PlayChoiceTravel"/>；
    /// 回程边（几何闭环未做）走「黑幕 + 锚点跳转」，闭环后改成真旅行。
    /// </summary>
    private IEnumerator PlayTravel(EndlessEdge edge)
    {
        var presenter = FakeRouteRuntime.Instance != null ? FakeRouteRuntime.Instance.MovementPresenter : null;
        if (presenter == null)
        {
            Debug.LogWarning("[Endless] 没有旅行表现层 → 直接提交目标节点");
            yield break;
        }

        if (edge.wrap)
        {
            // 回程：黑幕一闪 + 把视觉定位回起点锚点（几何闭环后这里换成真旅行）
            yield return presenter.PlayOpening(null, edge.target, () => IsRunning);
            yield break;
        }

        // 用剧情关同款的出口配置来播旅行（choiceId → 资产），拿不到就退化成锚点跳转
        var choice = FindChoiceAsset(edge.choiceId);
        if (choice != null)
        {
            yield return presenter.PlayChoiceTravel(choice, _nodes.ContainsKey(_currentNodeId) ? _nodes[_currentNodeId] : null, edge.target, () => IsRunning);
        }
        else
        {
            yield return presenter.PlayOpening(null, edge.target, () => IsRunning);
        }
    }

    private void StartNodeBattle()
    {
        var sc = StageController.Instance;
        if (sc == null)
        {
            Debug.LogError("[Endless] 找不到 StageController，无法开始节点战斗");
            EndRun(false);
            return;
        }

        // ★ **战斗期间放开手势输入**（2026-11-17）。
        //   这里不用 `stageState` 做门槛：节点战斗由本控制器自己的流程推进，
        //   而 `StageController.StartRouteBattle` 里的状态切换可能晚一帧 ⇒ 用它会把输入卡在"没放开"。
        SetGameplayInputAllowed(true);

        List<WaveConfig> waves = useAuthoredLevels ? BuildAuthoredWaves() : BuildGeneratedWaves();

        var stageConfig = BuildRuntimeStageConfig(waves, CurrentNodeName);
        sc.StartRouteBattle(stageConfig);
    }

    /// <summary>
    /// **无限模式自己当输入门的 owner**（2026-11-17 修「无尽关整局做不了手势」）。
    ///
    /// 背景：剧情路线在「落地页 / 等待 / 旅行」阶段用 <c>GameplayInputGate.RequestBlock</c>
    /// 拦下玩家输入；无限模式让位（<c>SuspendForEndless</c>）时，路线**一直挂着那份拦截**
    /// ⇒ 无限模式整局 <c>GameplayInputGate.IsBlocked == true</c> ⇒ 玩家一个手势都做不出来。
    ///
    /// ⇒ 路线让位时交还输入门，之后由无限模式自己管：
    /// **战斗放开 / 出口面板与旅行期间拦住**（面板是 UI，按钮不受影响）。
    /// </summary>
    private void SetGameplayInputAllowed(bool allowed)
    {
        if (allowed)
        {
            GameplayInputGate.Release(this);
            return;
        }
        GameplayInputGate.RequestBlock(this, InputBlockReason.RouteTravel);
        if (InputManager.Instance != null) InputManager.Instance.CancelCurrentGesture();
    }

    /// <summary>
    /// 当前节点**手写关卡自带的阵型配置**（照搬手写关卡时必须一起带过来）。
    /// </summary>
    private FormationConfig _nodeFormation;
    /// <summary>
    /// 取当前节点的**手写关卡配置**（<c>FakeRouteNodeConfig.battleEntries[0].battleConfig</c>），
    /// 深拷贝一份并按圈数改波次倍率。
    ///
    /// ⚠ **绝不改原资产**：倍率只写在拷贝上（否则剧情关与存档会被污染）。
    /// </summary>
    private List<WaveConfig> BuildAuthoredWaves()
    {
        StageConfig baseCfg = null;
        _nodeFormation = null;   // 先清：上一个节点的阵型不能泄到下一个节点
        if (_nodes.TryGetValue(_currentNodeId, out var node) && node != null && node.battleEntries != null)
        {
            for (int i = 0; i < node.battleEntries.Count && baseCfg == null; i++)
            {
                var entry = node.battleEntries[i];
                if (entry != null && entry.battleConfig != null) baseCfg = entry.battleConfig;
            }
        }
        if (baseCfg == null)
        {
            Debug.LogWarning("[Endless] 节点 " + _currentNodeId + " 没有关卡配置 → 退回程序生成");
            return BuildGeneratedWaves();
        }

        // ★ 阵型配置**从这份手写关卡里拿**（2026-11-17 修「敌人间距和近远透明度完全错误」）：
        //   阵型（逐排半宽 / 排间距 / 逐排透明度）跟波次一样是“关卡的一部分”，
        //   照搬波次就必须照搬阵型 ⇒ 否则新建出来的关卡排布与手写关卡完全不是一回事。
        _nodeFormation = baseCfg.formationConfig;

        var cloned = CloneWaves(baseCfg.waves);
        ApplyCircleScaling(cloned, Circle);
        return cloned;
    }

    /// <summary>深拷贝波次（行与 enemyIds 都要拷，不能共用引用）。</summary>
    private static List<WaveConfig> CloneWaves(List<WaveConfig> src)
    {
        var list = new List<WaveConfig>();
        if (src == null) return list;
        for (int i = 0; i < src.Count; i++)
        {
            var w = src[i];
            if (w == null) continue;
            var nw = new WaveConfig
            {
                waveId = w.waveId,
                isBossWave = w.isBossWave,
                healthMultiplier = w.healthMultiplier,
                bossHealthMultiplier = w.bossHealthMultiplier,
                attackSpeedMultiplier = w.attackSpeedMultiplier,
                damageMultiplier = w.damageMultiplier,
                expMultiplier = w.expMultiplier,
                waveTintColor = w.waveTintColor,
                enableDynamicRush = w.enableDynamicRush,
                rushMoveDelay = w.rushMoveDelay,
                rushMoveDelayMin = w.rushMoveDelayMin,
                rows = new List<RowConfig>()
            };
            for (int r = 0; r < w.rows.Count; r++)
            {
                var row = w.rows[r];
                if (row == null) continue;
                nw.rows.Add(new RowConfig { enemyIds = row.enemyIds != null ? (int[])row.enemyIds.Clone() : new int[5] });
            }
            list.Add(nw);
        }
        return list;
    }

    /// <summary>
    /// **按圈数改波次倍率**（用户口径：“适当改波次敌人倍率”）。
    ///
    /// 血量成长要快于玩家成长（否则圈数一多就秒清），但伤害/攻速成长要慢（否则直接死亡螺旋）。
    /// 系数均为可在 Inspector 调的字段（占位值，等实机标定）。
    ///
    /// ⚠ **起始倍率 <1**：剧情关的难度是按“中局玩家”调的，
    ///   而无尽模式是冷启动（没有天赋、没技能、没装备）⇒ 第 1 圈直接用剧情关难度会打不过。
    ///   起始倍率让玩家先拿到几轮成长，再逐步超过剧情关基线。
    /// </summary>
    private void ApplyCircleScaling(List<WaveConfig> waves, int circle)
    {
        if (waves == null) return;
        int step = Mathf.Max(0, circle - 1);

        float hpScale = Mathf.Max(0.05f, startHpScale) * Mathf.Pow(Mathf.Max(1f, hpGrowthPerCircle), step);
        float bossScale = Mathf.Max(0.05f, startHpScale) * Mathf.Pow(Mathf.Max(1f, bossHpGrowthPerCircle), step);
        float speedScale = Mathf.Min(maxAttackSpeedMultiplier, Mathf.Max(0.05f, startAttackSpeedScale) * Mathf.Pow(Mathf.Max(1f, attackSpeedGrowthPerCircle), step));
        float damageScale = Mathf.Min(maxDamageMultiplier, Mathf.Max(0.05f, startDamageScale) * Mathf.Pow(Mathf.Max(1f, damageGrowthPerCircle), step));

        for (int i = 0; i < waves.Count; i++)
        {
            var w = waves[i];
            if (w == null) continue;
            w.healthMultiplier *= hpScale;
            if (w.bossHealthMultiplier > 0f)
                w.bossHealthMultiplier = Mathf.Min(maxBossHpMultiplier, w.bossHealthMultiplier * bossScale);
            w.attackSpeedMultiplier = Mathf.Min(maxAttackSpeedMultiplier, w.attackSpeedMultiplier * speedScale);
            w.damageMultiplier = Mathf.Min(maxDamageMultiplier, w.damageMultiplier * damageScale);
        }
    }

    /// <summary>程序生成波次（仅当 useAuthoredLevels = false）。</summary>
    private List<WaveConfig> BuildGeneratedWaves()
    {
        // 程序生成的节点没有手写关卡 ⇒ 阵型用控制器的兜底资产（清掉上一个节点的残留）
        _nodeFormation = null;

        var genCfg = EndlessWaveGenerator.Config.Default(seed, Circle);
        genCfg.wavesPerCircle = Mathf.Max(1, NodeCount * Mathf.Max(1, wavesPerNode));

        var waves = new List<WaveConfig>();
        int perNode = Mathf.Max(1, wavesPerNode);
        for (int w = 0; w < perNode; w++)
        {
            bool isBoss = (_currentNodeId == "N2" || _currentNodeId == "N3") && w == perNode - 1;
            var wave = _generator.GenerateWave(genCfg, _circleWaveIndex, _circleWaveIndex, genCfg.wavesPerCircle, isBoss);
            waves.Add(wave);
            _circleWaveIndex++;
        }
        return waves;
    }

    private StageConfig BuildRuntimeStageConfig(List<WaveConfig> waves, string nodeName)
    {
        var cfg = ScriptableObject.CreateInstance<StageConfig>();
        cfg.stageId = 90000 + Circle;          // 与正式关 id 区分
        cfg.stageName = $"无尽·第{Circle}圈·{nodeName}";
        cfg.waves = waves;
        cfg.clearCoinReward = 0;               // 无限模式不靠铜钱结算（分数是主体）
        cfg.collisionDamage = 50f;
        cfg.rushMoveDelay = 0.05f;
        // ★ 阵型与可见性（2026-11-17 修「敌人间距和近远透明度完全错误，每次新建关卡都这样」）。
        //   根因：从落地页进无尽时 `StageController.stageConfig` 是 null ⇒ 阵型没带过来
        //   ⇒ 间距走兜底 `(col-2)*2.0`（平排、无梯形）、透明度走兜底 `[1,0.8,0.6,0.4,0.2]`
        //   （手写关卡是 `[1,0.9,0.8,0.75,0.75]` + 逐排半宽 2/2.5/3/4/4.5）。
        //   ⇒ 优先级：节点自己的手写阵型 → 开局快照 → 控制器上的兜底资产。
        cfg.formationConfig = _nodeFormation != null ? _nodeFormation
                            : (_formationSource != null ? _formationSource.formationConfig : null)
                            ?? fallbackFormation;
        if (cfg.formationConfig == null)
            Debug.LogWarning("[Endless] 没有阵型配置（formationConfig 为空）→ 敌人间距/透明度会走代码兜底，" +
                             "请在 Inspector 上给 EndlessRunController 接上 fallbackFormation（DefaultFormation）。");
        return cfg;
    }

    // ─────────────────────────── 事件接线 ───────────────────────────

    private void Subscribe()
    {
        var sc = StageController.Instance;
        if (sc != null)
        {
            sc.OnRouteBattleCompleted += OnRouteBattleCompleted;
            sc.OnStageDefeat += OnStageDefeat;
        }
        var em = EnemyManager.Instance;
        if (em != null) em.OnAnyEnemyDied += OnEnemyDied;
        var ps = PlayerState.Instance;
        if (ps != null) ps.OnPlayerDied += OnPlayerDiedHandler;
        var ws = WaveSpawner.Instance;
        if (ws != null)
        {
            ws.OnWaveStarted += OnWaveStarted;
            ws.OnWaveCompleted += OnWaveCompleted;
        }
    }

    private void Unsubscribe()
    {
        var sc = StageController.Instance;
        if (sc != null)
        {
            sc.OnRouteBattleCompleted -= OnRouteBattleCompleted;
            sc.OnStageDefeat -= OnStageDefeat;
        }
        var em = EnemyManager.Instance;
        if (em != null) em.OnAnyEnemyDied -= OnEnemyDied;
        var ps = PlayerState.Instance;
        if (ps != null) ps.OnPlayerDied -= OnPlayerDiedHandler;
        var ws = WaveSpawner.Instance;
        if (ws != null)
        {
            ws.OnWaveStarted -= OnWaveStarted;
            ws.OnWaveCompleted -= OnWaveCompleted;
        }
    }

    private void OnRouteBattleCompleted()
    {
        _nodeCleared = true;
    }

    private void OnStageDefeat()
    {
        if (!IsRunning) return;
        EndRun(true);
    }

    /// <summary>
    /// 玩家真阵亡（复活次数用完）。
    ///
    /// ⚠ 为什么直接订 <c>PlayerState.OnPlayerDied</c> 而不是 <c>StageController.OnStageDefeat</c>：
    /// 阵亡时 <c>PlayerState</c> 会先把自身状态置为 Defeat 再抛事件，
    /// 于是 <c>StageRunFlow.OnPlayerDefeated</c> 里的状态检查会**提前 return**，
    /// <c>StageController.OnStageDefeat</c> 根本不会广播（路线模式是靠
    /// <c>FakeRouteRuntime.HandleStageDefeat</c> 兜底的，而无限模式不走路线运行时）。
    /// </summary>
    private void OnPlayerDiedHandler()
    {
        if (!IsRunning) return;
        EndRun(true);
    }

    private void OnEnemyDied(Enemy enemy)
    {
        if (!IsRunning || enemy == null) return;
        Ledger.AddKill(enemy.enemyId);
        int combo = ComboManager.Instance != null ? ComboManager.Instance.CurrentCombo : 0;
        if (combo > _waveComboPeak) _waveComboPeak = combo;
        Ledger.AddComboPeak(_waveComboPeak);
    }

    private void OnWaveStarted(int waveIndex)
    {
        _waveActive = true;
        _waveStartTime = Time.time;
        var ps = PlayerState.Instance;
        _waveStartHealth = ps != null ? ps.currentHealth : 0f;
        _waveComboPeak = 0;
    }

    private void OnWaveCompleted(int waveIndex)
    {
        if (!IsRunning || !_waveActive) return;
        _waveActive = false;

        var ps = PlayerState.Instance;
        float healthRatio = 0f;
        bool noHit = false;
        if (ps != null)
        {
            float max = Mathf.Max(1f, ps.MaxHealth);
            healthRatio = Mathf.Clamp01(ps.currentHealth / max);
            noHit = ps.currentHealth >= _waveStartHealth - 0.001f;
        }

        int gained = Ledger.SettleWave(new EndlessScoreLedger.WaveSettlementInput
        {
            elapsedSeconds = Time.time - _waveStartTime,
            targetSeconds = 30f,
            healthRatio = healthRatio,
            maxCombo = _waveComboPeak,
            noHitTaken = noHit,
            noBreakTaken = false
        });

        OnWaveSettled?.Invoke(gained, Ledger.TotalScore);
        OnStateChanged?.Invoke();
    }

    // ─────────────────────────── 开局重置 / 存档 ───────────────────────────

    private void ResetRunState()
    {
        PlayerState.Instance?.ResetPlayer();
        UltimateSystem.Instance?.ResetEnergy();
        EnemyManager.Instance?.ClearAllEnemies();
        KillRewardManager.Instance?.ResetRewards();
        ComboManager.Instance?.ResetCombo();
        HealthPotionManager.Instance?.ResetForNewStage();
        UpgradeEffectManager.Instance?.ResetAll();
    }

    private void SaveBest()
    {
        var data = SaveManager.Load();
        if (data == null) return;
        if (Ledger.TotalScore > data.endlessBestScore) data.endlessBestScore = Ledger.TotalScore;
        if (Circle > data.endlessMaxCircle) data.endlessMaxCircle = Circle;
        SaveManager.Save(data);
        Debug.Log($"[Endless] 存档：最佳分 {data.endlessBestScore}，最高圈 {data.endlessMaxCircle}");
    }
}
