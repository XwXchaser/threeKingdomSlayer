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
public class EndlessRunController : MonoBehaviour, IEndlessCircleSource
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
    [Tooltip("第 1 圈的敌人血量倍率。\n" +
             "**默认 1 = 敌人就是该节点手写关卡里的原始数值**（2026-11-19 用户定：“敌人 = 第一关原数值”）。\n" +
             "留 <1 会让无尽关比剧情关轻松；留 >1 会更狠。")]
    public float startHpScale = 1f;
    [Tooltip("第 1 圈的敌人伤害倍率（默认 1 = 原数值）。")]
    public float startDamageScale = 1f;
    [Tooltip("第 1 圈的敌人攻速倍率（默认 1 = 原数值）。")]
    public float startAttackSpeedScale = 1f;
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
    [Tooltip("每圈血量倍率（乘性）。\n" +
             "**默认 1 = 敌人不随圈数成长**（2026-11-19 用户定：“第一圈固定，不成长”，\n" +
             "且“敌人 = 第一关原数值”）⇒ 圈数只影响路线走法与分数，难度靠玩家自己的属性成长。")]
    public float hpGrowthPerCircle = 1f;
    [Tooltip("每圈 BOSS 血量倍率（乘性，比杂兵温和）。默认 1 = 不成长。")]
    public float bossHpGrowthPerCircle = 1f;
    [Tooltip("每圈攻速倍率（乘性）。默认 1 = 不成长。")]
    public float attackSpeedGrowthPerCircle = 1f;
    [Tooltip("每圈伤害倍率（乘性）。默认 1 = 不成长。")]
    public float damageGrowthPerCircle = 1f;
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
    [Tooltip("节点之间的停顿（秒，不受 timeScale 影响）。")]
    public float nodeSettleDelay = 0.8f;
    [Tooltip("回程（走到下一圈起点）的旅行时长（秒）。")]
    public float wrapTravelSeconds = 3f;

    [Header("路线（沿用剧情关的节点资产，只借用名字与视觉档）")]
    [Tooltip("起点节点 id（提交回它 = 完成一圈）。")]
    public string loopStartNodeId = "N1";
    [Tooltip("路线来源（提供节点与出口资产）。留空 = 自动找可选路线里的第一条。")]
    public FakeRouteStageConfig sourceRoute;

    [Header("测试（临时）")]
    [Tooltip("勾上后：**第一圈的 N1 / E0** 节点所有敌人都按精英处理（100% 掉装备宝箱），用于验收换装。\n" +
             "只影响这两个节点；节点结束 / 本局结束自动清除。测试完取消勾选。")]
    public bool debugForceEliteDropsFirstCircle = false;

    // ── 运行时状态 ──
    public bool IsRunning { get; private set; }
    /// <summary>本局是否刷新了历史最高分（结算面板用；由 <see cref="SaveBest"/> 在写档前判定）。</summary>
    public bool LastRunSetRecord { get; private set; }
    public int Circle { get; private set; } = 1;
    public string CurrentNodeName { get; private set; } = "";
    public string CurrentBranchName { get; private set; } = "";
    /// <summary>本圈当前节点在圈内的序号（1 起；显示用）。</summary>
    public int NodeIndex { get; private set; } = 1;
    /// <summary>本圈节点总数（显示用）。</summary>
    public int NodeCount { get; private set; } = 1;
    public EndlessScoreLedger Ledger { get; } = new EndlessScoreLedger();

    // ── 事件（UI 层订阅；本类不碰任何 UI 对象）──
    public event Action OnStateChanged;
    public event Action<bool> OnRunEnded;

    /// <summary>无限模式的一条边（出口）。</summary>
    private sealed class EndlessEdge
    {
        public string choiceId;
        public string displayName;
        public FakeRouteNodeConfig target;
        /// <summary>回程边（几何闭环前用「黑幕 + 锚点跳转」代替真旅行）。</summary>
        public bool wrap;
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
        // 无尽关没正常结束就销毁（切场景 / 退出）时也要注销，避免留下失效的圈数来源。
        UpgradeChoiceManager.Instance?.ClearCircleSource(this);
        EquipmentDropTestMode.ForceAllDrops = false;
        if (Instance == this) Instance = null;
    }

    // ─────────────────────────── 入口 ───────────────────────────

    /// <summary>开始一局无限模式。</summary>
    public void StartEndlessRun()
    {
        if (IsRunning) return;

        IsRunning = true;
        Circle = 1;
        NodeIndex = 1;
        Ledger.Reset();

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
        // ★ **回收所有教学专属状态**（2026-11-25）：
        //   路线让位后 RouteFlowHub.Current 仍指向那条假路线，教学导演只凭 RouteConfig
        //   会把它当成“还在教学关” → 招式白名单 / 防御键隐藏与静音 / 强制三选一 全都会残留。
        //   ⇒ 由教学导演的统一出口一次性清掉；导演没挂上时至少清掉白名单。
        // 教学导演在 UI 程序集，Flow 不能直接引用 → 通过 Data 层端口请求回收。
        TutorialStatePort.ReleaseForModeTakeover();
        TutorialAttackGate.Clear();   // 无导演时兜底；有导演时这次调用幂等
        // ★ 无限卷轴：无尽关必须开，否则走完一圈就出了地图（什么都不剩）
        FakeRouteRuntime.Instance?.MovementPresenter?.SetEndlessScroll(true);
        // 若剧情路线此刻正在跑（战斗 / 旅行），先把它停掉 → 否则两套流程并行
        StageController.Instance?.AbortRouteBattle();

        // ★ **接管输入门**（2026-11-17 修「无尽关整局做不了手势」）：
        //   路线让位时已把它自己那份 RouteTravel 拦截交还（见 SuspendForEndless），
        //   但没人接手的话玩家反而能乱挥 ⇒ 这里由无限模式自己当 owner：
        //   **准备阶段先拦住 → 进入节点战斗放开 → 出口面板 / 旅行期间再拦住**。
        SetGameplayInputAllowed(false);

        if (StageController.Instance != null) StageController.Instance.blockRestartOnDefeat = true;

        Subscribe();
        // 装备档位按**内部圈数**分阶段（2026-11-25）：Combat 只认 Data 层接口，不反向引用 Flow。
        UpgradeChoiceManager.Instance?.SetCircleSource(this);
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
        UpgradeChoiceManager.Instance?.ClearCircleSource(this);
        if (_flow != null) { StopCoroutine(_flow); _flow = null; }
        RouteChoicePanel.HideCurrent();
        _choicePanelOpen = false;

        // 本局铜钱落盘（幂等）：阵亡 / 主动退出都走这里。
        //   ⚠ 必须在 SaveBest 之前，否则结算面板读到的仍是旧钱包（面板只显示分数，但顺序保持一致）。
        StageController.Instance?.SettleCoins();

        SaveBest();

        if (StageController.Instance != null) StageController.Instance.blockRestartOnDefeat = false;

        // 交还输入门（别把玩家永久锁住；回落地页时路线会重新拦）
        SetGameplayInputAllowed(true);
        // 正常结束也回收一次教学专属状态（幂等），避免残留到下一局 / 落地页。
        // 教学导演在 UI 程序集，Flow 不能直接引用 → 通过 Data 层端口请求回收。
        TutorialStatePort.ReleaseForModeTakeover();
        TutorialAttackGate.Clear();   // 无导演时兜底；有导演时这次调用幂等

        IsRunning = false;
        _pendingEdge = null;
        EquipmentDropTestMode.ForceAllDrops = false;

        // 关掉无限卷轴 ⇒ 回落地页 / 剧情路线时表现与旧版一致
        FakeRouteRuntime.Instance?.MovementPresenter?.SetEndlessScroll(false);

        Debug.Log($"[Endless] 结束（阵亡={byDeath}）{Ledger}");
        OnRunEnded?.Invoke(Ledger.TotalScore > 0);
        OnStateChanged?.Invoke();
    }

    /// <summary>退出无限模式 → 回落地页。</summary>
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
        // ★ 这两条边就是「无限循环」的来源：两个终点接回起点 ⇒ 走完 = 一圈 ⇒ 结算 ⇒ 再来一圈。
        AddEdge("N3", new EndlessEdge { choiceId = "n3_to_n1", displayName = "出村：回到当阳", target = _nodes.ContainsKey("N1") ? _nodes["N1"] : null, wrap = true });
        AddEdge("N2", new EndlessEdge { choiceId = "n2_to_n1", displayName = "官道折返：回到当阳", target = _nodes.ContainsKey("N1") ? _nodes["N1"] : null, wrap = true });

        // ⚠ **不再有「循环边」**（2026-11-19）。
        //   旧实现在 N1/N3/N2 上各挂一条自环（N1→N1 / N3→N3 / N2→N2），
        //   让「一圈」变成 N1×2 → E0 → J1 → 左路 N3×3 / 右路 N2×3 —— 那是**另搭的一套拓扑**，
        //   和「拿原本第一关的内容做循环」不是一回事。
        //   现在：圈 = **第一关的一条完整路径**（N1 → E0 → J1 → 〔EV → N3 | N2〕 → 回 N1），
        //   走完回程边就结算一圈。J1 的左右支路由玩家在出口面板上选（和剧情关一致）。

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
            if (id == loopStartNodeId) break;   // 回到起点 = 走完一圈（起点本身不重复计数）
        }
        return Mathf.Max(1, count);
    }

    private IEnumerator RunLoop()
    {
        while (IsRunning)
        {
            // ── 节点战斗 ──
            //   **纯情景节点不打怪**（2026-11-19）：剧情关里 J1（城郊分叉）/ EV（村落外围）
            //   本来就没有战斗配置 —— 它们只是旅行过场。无尽模式**不得凭空给它们造一波怪**。
            _nodeCleared = false;
            if (CurrentNodeHasBattle())
            {
                StartNodeBattle();
                OnStateChanged?.Invoke();

                while (!_nodeCleared && IsRunning) yield return null;
            }
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

            // 走完**回程边** = 走完一圈（⚠ 不能用“当前节点是起点”判定：
            //   循环边（N1 → N1）也会把节点指回起点，但那一圈还没走完）
            // ★ **不再有圈结算画面**（2026-11-19 用户定：“除非玩家死亡、结束本局，
            //   你出这个结算干嘛？”）⇒ 走完回程边就**直接接着走下一圈**。
            if (edge.wrap)
            {
                // ⚠ **没有圈里程碑分**（2026-11-24 用户定：“删，从来没有这样的内容”）：
                //   圈只是内部的难度/内容复用单位，**不进分数、不进 HUD、不进结算**。
                Circle++;
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
        // 节点战斗结束 → 关闭测试掉宝（后面不会再生成该节点的敌人）。
        EquipmentDropTestMode.ForceAllDrops = false;
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
            // ★ 回程：**沿路走到下一圈的起点**（2026-11-19 无限循环）。
            //   以前是「黑幕 + 锚点跳转」（瞬移 ⇒ 玩家看不到前进，像被扔回起点）。
            //   现在是真的往前走一段 —— 目标位置由表现层的 TryGetNodeProgress 决定，
            //   它返回「当前之后最近的那一次出现」⇒ 下一圈 N1 ⇒ 天然往前走。
            yield return presenter.PlayNodeTravel(edge.target, wrapTravelSeconds, () => IsRunning);
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

    /// <summary>
    /// **开发者跳过当前节点战斗**（2026-11-19）。
    ///
    /// 与剧情路线的 <see cref="IRouteFlow.SkipCurrentNodeBattle"/> 同一套语义：
    /// **清场** ✅ + 标记本节点战斗结束 ✅ → 直接进出口面板 ✅；
    /// **不碰奖励发放** ✅（三选一该弹还是弹）。
    ///
    /// 用途：**直接测无尽循环**（不用真打一场就能一路点「前进」把圈走完）。
    ///
    /// ⚠ 为什么需要它：剧情路线的跳过按钮只认 <see cref="RouteFlowHub"/>，
    /// 而无尽模式接管后路线被 <c>SuspendForEndless</c> 置成 <c>Phase.None</c>
    /// ⇒ 那个按钮会被判成“不在战斗中”而变灰。
    /// </summary>
    public bool SkipCurrentNodeBattle()
    {
        if (!IsRunning) return false;
        if (!CurrentNodeHasBattle()) return false;        // 纯情景节点（J1 / EV）本来就没怪
        if (_nodeCleared) return false;                   // 已经打完了

        // 清场：停波次 + 清敌人 + 清投射物（不动节点状态 ✅）
        StageController.Instance?.AbortRouteBattle();
        // 通知战斗结束 → RunLoop 里的等待循环会退出 ✅
        _nodeCleared = true;
        Debug.Log("[Endless] 开发者跳过战斗 node=" + _currentNodeId);
        return true;
    }

    /// <summary>
    /// 当前是否正处在「一场节点战斗里」（给开发者按钮判可用性）。
    /// </summary>
    public bool IsInNodeBattle => IsRunning
        && !_nodeCleared
        && CurrentNodeHasBattle();

    /// <summary>
    /// 当前节点**要不要打怪**。
    ///
    /// 手写配置模式下：只有**带战斗配置**的节点才打（N1 / E0 / N3 / N2）；
    /// J1 / EV 这种**纯情景节点**只做旅行过场。
    /// 生成器模式下：每个节点都有波次（保留旧行为）。
    /// </summary>
    private bool CurrentNodeHasBattle()
    {
        if (!useAuthoredLevels) return true;
        return TryGetNodeBattleConfig(_currentNodeId, out _);
    }

    /// <summary>取节点自己的手写关卡配置（<c>FakeRouteNodeConfig.battleEntries[].battleConfig</c>）。</summary>
    private bool TryGetNodeBattleConfig(string nodeId, out StageConfig cfg)
    {
        cfg = null;
        if (string.IsNullOrEmpty(nodeId)) return false;
        if (!_nodes.TryGetValue(nodeId, out var node) || node == null || node.battleEntries == null) return false;
        for (int i = 0; i < node.battleEntries.Count; i++)
        {
            var entry = node.battleEntries[i];
            if (entry != null && entry.battleConfig != null)
            {
                cfg = entry.battleConfig;
                return true;
            }
        }
        return false;
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

        // 测试掉宝（2026-11-25）：只在第一圈的 N1 / E0 打开；后续节点 / 圈数一律关闭。
        EquipmentDropTestMode.ForceAllDrops = debugForceEliteDropsFirstCircle
            && Circle == 1
            && (_currentNodeId == "N1" || _currentNodeId == "E0");
        if (EquipmentDropTestMode.ForceAllDrops)
            Debug.Log("[Endless][Test] 本节点所有敌人强制掉装备宝箱 node=" + _currentNodeId + " circle=" + Circle);

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
        if (!TryGetNodeBattleConfig(_currentNodeId, out baseCfg) || baseCfg == null)
        {
            // 纯情景节点（J1 / EV）：剧情关里它们本来就没有战斗
            // ⇒ **不退回程序生成器**（2026-11-19）：凭空造怪会让无尽关和第一关手感不一致。
            Debug.Log($"[Endless] 节点 {_currentNodeId} 没有手写关卡 ⇒ 纯情景节点，只过场不打怪");
            return new List<WaveConfig>();
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

        // ⚠ **击杀铜钱不在这里加**（2026-11-24 核对）：
        //   <c>StageController.Start</c> 无条件把 <c>StageRunFlow.OnEnemyDied</c> 订到
        //   <c>EnemyManager.OnAnyEnemyDied</c>，而那个回调里已经做了
        //   <c>AddKill + AddCoins(coinReward × 聚财)</c>，且**不按关卡状态拦截** ⇒ 无尽关一直在加钱。
        //   这里再写一遍就是双倍。真正缺的是**阵亡时的落盘**，见 EndRun → StageController.SettleCoins。
    }

    // ─────────────────────────── 开局重置 / 存档 ───────────────────────────

    private void ResetRunState()
    {
        EquipmentDropTestMode.ForceAllDrops = false;
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
        // 先判定再写：写完之后 best 已经等于本局，就分不出「新纪录」与「平记录」了。
        LastRunSetRecord = Ledger.TotalScore > data.endlessBestScore;
        if (LastRunSetRecord) data.endlessBestScore = Ledger.TotalScore;
        if (Circle > data.endlessMaxCircle) data.endlessMaxCircle = Circle;
        SaveManager.Save(data);
        Debug.Log($"[Endless] 存档：最佳分 {data.endlessBestScore}");
    }
}
