using System.Collections;
using UnityEngine;

/// <summary>
/// 教学关导演（场景：Assets/Scenes/Tutorial/Tutorial_YJ.unity —— YJ 场景的拷贝 + 战斗栈）。
///
/// 教学台词**全部走对话系统**（DialogueEventData + DialogueDatabase）：
/// - 每波开场：资产里配 triggerType = WaveStart、stageId = 9000、waveNumber = 1..4，由 DialogueManager 自动播；
/// - 收尾台词：本组件在玩家阵亡后手动 QueueEventAndNotify。
/// 对话本身会把 Time.timeScale 置 0 并挡住输入，所以“暂停”直接复用对话的暂停，不再自己画提示条。
///
/// 本组件只负责三件对话系统管不了的事：
/// 1) 最后一波把 109 改成必杀冲锋、血量拉高（首见杀是演出，不许被玩家提前打死）；
/// 2) 玩家阵亡不弹战败面板（StageController.suppressDefeatUI）；
/// 3) 记下 tutorialCompleted，然后接正式路线。
/// </summary>
public class TutorialStageDirector : MonoBehaviour
{
    [Header("关卡识别")]
    [Tooltip("只有 StageController 当前关卡等于这份配置时才工作")]
    public StageConfig tutorialStageConfig;

    [Header("收尾")]
    [Tooltip("阵亡后播放的台词；留空则直接收尾")]
    public DialogueEventData deathDialogue;
    [Tooltip("按波次（0 起）自动播的教学台词；对话会自己把时间停住")]
    public DialogueEventData[] waveDialogues = new DialogueEventData[0];
    [Tooltip("台词播完后再等多久进正式路线（真实时间，秒）")]
    public float handoffDelaySeconds = 0.8f;

    [Header("路面高度")]
    [Tooltip("卷轴着色器投影出来的路面世界 Y（正式路线也是 -1.8）。教学关要把敌人脚底对到这里，否则就是浮着或陷进地里")]
    public float groundY = -1.8f;
    [Header("初见杀")]
    [Tooltip("最后一波冲锋伤害改成这个值，保证一击必杀")]
    public float lethalChargeDamage = 20f;
    [Tooltip("最后一波同时把敌人血量抬高，避免玩家在演出前把它打死")]
    public float executionerHealth = 0f;

    [Header("教学升级")]
    [Tooltip("教学关三选一固定给的这个技能（冰雾）。留空则不弹选择")]
    public UpgradeDefinition tutorialIceSkill;

    [Header("行进（N1 → E0）")]
    [Tooltip("驱动 YJ 场景自带投影的 YScrollSample，只改 progress，不重写卷轴")]
    public YScrollSample routeVisual;
    [Tooltip("第几波开始前设置挂起（0 起）。默认第 2 波（索引 1）清完就去 E0")]
    public int travelAfterWaveIndex = 1;
    [Tooltip("行进到 E0 的 progress（E0 沿路 90 米 / 全长 325.1 米 ≈ 0.2768）")]
    public float travelToProgress = 0.2768f;
    [Tooltip("行进时长（秒）")]
    public float travelDuration = 2.5f;

    [Header("初见杀之后")]
    [Tooltip("收尾后要接的正式路线；留空则回主菜单")]
    public FakeRouteStageConfig nextRouteConfig;

    private bool _active;
    private bool _ending;
    private StageController _stage;
    private WaveSpawner _waveSpawner;

    private void Start()
    {
        _stage = StageController.Instance;
        _active = _stage != null && tutorialStageConfig != null && _stage.stageConfig == tutorialStageConfig;
        if (!_active) return;

        // 教学关不弹战败面板：这是演出，不是失败
        _stage.suppressDefeatUI = true;
        _stage.OnStageDefeat += OnStageDefeat;
        // 玩家阵亡有两条路：StageController.HandlePlayerDefeat（会发 OnStageDefeat）
        // 和 PlayerState 自己把状态置成 Defeat（不发 OnStageDefeat）。两条都要接，否则演出不会跑。
        _stage.OnStageStateChanged += OnStageStateChanged;

        _waveSpawner = FindObjectOfType<WaveSpawner>();
        if (_waveSpawner != null)
        {
            _waveSpawner.OnWaveStarted += OnWaveStarted;
            _waveSpawner.OnWaveCompleted += OnWaveCompleted;
        }

        DebugLog.Info("[Tutorial] 教学关开始");
        // 教学关不出三选一：升级弹窗会暂停并把下一波卡住
        if (UpgradeChoiceManager.Instance != null)
        {
            UpgradeChoiceManager.Instance.suppressChoices = false;
            UpgradeChoiceManager.Instance.forcedChoices = tutorialIceSkill != null
                ? new System.Collections.Generic.List<UpgradeDefinition> { tutorialIceSkill }
                : new System.Collections.Generic.List<UpgradeDefinition>();
        }
        // 教学关不能完成充能：终结技留到正式关卡再教
        if (InputManager.Instance != null) InputManager.Instance.suppressCharging = true;
        StartCoroutine(BeginStageNextFrame());
    }

    private void OnDisable()
    {
        if (_stage != null)
        {
            _stage.OnStageDefeat -= OnStageDefeat;
            _stage.OnStageStateChanged -= OnStageStateChanged;
        }
        if (_waveSpawner != null)
        {
            _waveSpawner.OnWaveStarted -= OnWaveStarted;
            _waveSpawner.OnWaveCompleted -= OnWaveCompleted;
            _waveSpawner.holdNextWave = false;
        }
        if (UpgradeChoiceManager.Instance != null)
        {
            UpgradeChoiceManager.Instance.suppressChoices = false;
            UpgradeChoiceManager.Instance.forcedChoices.Clear();
        }
        if (InputManager.Instance != null) InputManager.Instance.suppressCharging = false;
    }

    private void OnStageStateChanged(StageState state)
    {
        if (state == StageState.Defeat) OnStageDefeat();
    }

    private IEnumerator BeginStageNextFrame()
    {
        yield return null;
        if (_stage == null || !_active) yield break;

        // 非路线关卡没人点火（autoStartStage 可能被卷轴系统关掉），这里自己启动一次。
        // 延后一帧是为了让 WaveSpawner 先解析好关卡配置，否则第一波会静默不生成。
        // 教学关场景里没有卷轴路线驱动；如果场景已开 autoStartStage，就交给 StageController 自己点火，
        // 这里不要再调一次：双开会让 ResetPlayer 把玩家状态打回 Starting（攻击全部失灵的根）。
        if (_stage.autoStartStage) yield break;
        if (_stage.CurrentState != StageState.InProgress
            && _stage.CurrentState != StageState.Victory
            && _stage.CurrentState != StageState.Defeat)
        {
            _stage.StartStage();
        }
    }

    private void OnWaveStarted(int waveIndex)
    {
        if (!_active || _stage == null || _stage.stageConfig == null) return;

        // 教学台词由本组件直接推给对话系统：不走数据库的 WaveStart 匹配，
        // 避免订阅时机早晚导致第一波台词被漏掉（实测漏过）。
        if (waveDialogues != null && waveIndex < waveDialogues.Length && waveDialogues[waveIndex] != null)
            DialogueManager.Instance?.QueueEventAndNotify(waveDialogues[waveIndex]);

        bool isLastWave = waveIndex == _stage.stageConfig.waves.Count - 1;
        if (isLastWave) StartCoroutine(PrepareExecutionerWhenSpawned());
        AlignEnemiesToRoad();

        // 这一波清完要去 E0：先在波次开始时挂起“自动刷下一波”，清完后由行进修完再放行
        if (routeVisual != null && waveIndex == travelAfterWaveIndex && _waveSpawner != null)
            _waveSpawner.holdNextWave = true;
    }

    private void OnWaveCompleted(int waveIndex)
    {
        if (!_active || routeVisual == null) return;
        if (waveIndex != travelAfterWaveIndex) return;
        StartCoroutine(TravelToE0());
    }

    /// <summary>
    /// N1 → E0 的行进：直接推 YJ 场景自带的 YScrollSample.progress（卷轴投影就是它算的），
    /// 不另写一套移动。行进期间关掉输入，完毕后再放行下一波。
    /// </summary>
    private IEnumerator TravelToE0()
    {
        var input = InputManager.Instance;
        if (input != null) { input.gameplayInputEnabled = false; input.CancelCurrentGesture(); }

        routeVisual.animate = false;
        float from = routeVisual.progress;
        float to = Mathf.Clamp01(travelToProgress);
        float duration = Mathf.Max(0.2f, travelDuration);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            routeVisual.progress = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
            yield return null;
        }
        routeVisual.progress = to;
        DebugLog.Info("[Tutorial] 行进完成，到达 E0 progress=" + to.ToString("F3"));

        if (input != null) input.gameplayInputEnabled = true;
        if (_waveSpawner != null)
        {
            _waveSpawner.holdNextWave = false;
            _waveSpawner.SpawnNextWave();
        }
    }

    /// <summary>
    /// 把敌人脚底对到路面高度。项目已有 EnemyGroundAligner（按精灵剪影算偏移），
    /// 但敌人预制体上没挂；正式路线靠 BattleYRouteHost 算一次，教学关没有那一层，所以在这里补。
    /// </summary>
    private void AlignEnemiesToRoad()
    {
        var em = EnemyManager.Instance;
        if (em == null) return;
        var alive = em.GetAllAliveEnemies();
        for (int i = 0; i < alive.Count; i++)
        {
            var enemy = alive[i];
            if (enemy == null) continue;
            var aligner = enemy.GetComponent<EnemyGroundAligner>();
            if (aligner == null) aligner = enemy.gameObject.AddComponent<EnemyGroundAligner>();
            aligner.groundY = groundY;
        }
    }

    private void LateUpdate()
    {
        if (!_active) return;
        // 对象池复用的敌人可能缺对齐组件，逐帧补一次（教学关敌人数量很少）
        AlignEnemiesToRoad();

        // 兜底：阵亡事件偶尔收不到（玩家可能被 PlayerState 直接置成 Defeat），
        // 这里直接看状态，保证收尾一定会跑。
        if (!_ending && _stage != null && _stage.CurrentState == StageState.Defeat)
            OnStageDefeat();
    }

    /// <summary>
    /// 等骑将真正生成后再改数值：OnWaveStarted 触发时敌人可能还没入列，
    /// 当场取 GetAllAliveEnemies() 会拿到空列表（实测最后一波 hp 停在 20、冲锋伤害还是 1）。
    /// </summary>
    private IEnumerator PrepareExecutionerWhenSpawned()
    {
        float waited = 0f;
        while (waited < 5f)
        {
            var manager = EnemyManager.Instance;
            if (manager != null)
            {
                var alive = manager.GetAllAliveEnemies();
                for (int i = 0; i < alive.Count; i++)
                    if (alive[i] != null && alive[i].IsMountedCavalry) { PrepareExecutioner(); yield break; }
            }
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
        Debug.LogWarning("[Tutorial] 最后一波等了 5 秒没等到骑将，初见杀不会触发");
    }

    /// <summary>把最后一波的骑将变成“演出用”：冲锋必杀、打不死。</summary>
    private void PrepareExecutioner()
    {
        var manager = EnemyManager.Instance;
        if (manager == null) return;

        var alive = manager.GetAllAliveEnemies();
        int prepared = 0;
        for (int i = 0; i < alive.Count; i++)
        {
            var enemy = alive[i];
            if (enemy == null || !enemy.IsMountedCavalry) continue;
            var cavalry = enemy.GetComponent<CavalryEnemy>();
            if (cavalry == null) continue;

            cavalry.OverrideChargeDamageForScript(lethalChargeDamage);
            // 血量只在显式指定时才改；现在 109 用配置里的真实血量（500 = 5 套枪突链）
            if (executionerHealth > 0f)
            {
                enemy.maxHealth = executionerHealth;
                enemy.currentHealth = executionerHealth;
            }
            prepared++;
            DebugLog.Info($"[Tutorial] {enemy.DebugTag} 设为演出骑将：冲锋伤害 {lethalChargeDamage}、血量 {executionerHealth}");
        }
        if (prepared == 0)
            Debug.LogWarning("[Tutorial] 最后一波没有找到骑乘骑兵，初见杀不会触发（检查该波是否配置了 109）");
    }

    private void OnStageDefeat()
    {
        if (!_active || _ending) return;
        _ending = true;
        DebugLog.Info("[Tutorial] 玩家被戳死，进入收尾");
        StartCoroutine(EndingRoutine());
    }

    private IEnumerator EndingRoutine()
    {
        var dialogue = DialogueManager.Instance;
        if (deathDialogue != null && dialogue != null)
            dialogue.QueueEventAndNotify(deathDialogue);

        // 等台词播完（对话把 timeScale 置 0，这里只能用真实时间）
        yield return new WaitForSecondsRealtime(0.2f);
        while (dialogue != null && (dialogue.IsPlaying || dialogue.IsBusy))
            yield return null;

        if (handoffDelaySeconds > 0f)
            yield return new WaitForSecondsRealtime(handoffDelaySeconds);

        FinishTutorial();
    }

    private void FinishTutorial()
    {
        var data = SaveManager.Load();
        if (!data.tutorialCompleted)
        {
            data.tutorialCompleted = true;
            SaveManager.Save(data);
            DebugLog.Info("[Tutorial] 教学关已标记完成");
        }

        if (nextRouteConfig != null)
        {
            // 直接接正式开局：重建战斗场景，让 StageController.Awake 拿到卷轴路线配置。
            // 否则玩家得回主菜单再点“新游戏”，而新游戏会删档，教学又会重演一遍。
            SaveManager.ClearFakeRouteSnapshot(nextRouteConfig.routeId, nextRouteConfig.stageId);
            SaveManager.SetActiveFakeRouteStage(nextRouteConfig.stageId);
            FakeRouteLaunch.StartFromCheckpoint = false;
            FakeRouteLaunch.PendingConfig = nextRouteConfig;
            StageController.PendingStageConfig = null;
            StageController.PendingRouteStageConfig = null;
            RouteStageV2Launch.PendingConfig = null;
            DebugLog.Info("[Tutorial] 教学完成，接正式路线 " + nextRouteConfig.routeId);
            _stage?.GoToBattleScene();
            return;
        }

        _stage?.GoToMainMenu();
    }
}
