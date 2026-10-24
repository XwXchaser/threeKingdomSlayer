using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class DialogueManager : MonoBehaviour, IDialogueModal
{

    /// <summary>
    /// 关卡战斗状态（只读 + 状态事件）。 历史上直接摸 StageController 门面，现只依赖角色接口
    /// （2026-10-20 批次 1）。延迟解析：本组件可能早于 StageController Awake。
    /// </summary>
    private static IStageBattleState _stageBattleState;

    private static IStageBattleState StageBattleState => _stageBattleState ??= StageController.Instance;
    public static DialogueManager Instance { get; private set; }

    [SerializeField] private DialogueDatabase _database;
    [SerializeField] private CanvasGroup _dimOverlay;
    [SerializeField] private BattleDialogueView _dialogueView;

    private readonly Queue<DialogueEventData> _queue = new Queue<DialogueEventData>();
    private readonly HashSet<string> _playedThisRun = new HashSet<string>();
    private readonly HashSet<string> _pendingTutorialIds = new HashSet<string>();
    private readonly HashSet<string> _queuedIds = new HashSet<string>();
    /// <summary>被强制重播（跳过「本局已播 / 存档已完成」）的事件；这类教学台词不写进完成记录，下次进教学关还要再讲一遍。</summary>
    private readonly HashSet<string> _forcedIds = new HashSet<string>();
    private HeroHUD _heroHUD;
    private bool _isPlaying;
    private bool _pausedForDialogue = true;
    private int _interactionBlockFrames;
    private string _activeEventId;
    private bool _stageStartTriggered;
    private bool _subscriptionsSet;

    public bool IsPlaying => _isPlaying;
    public bool IsBusy => _isPlaying || _queue.Count > 0;
    public bool IsInteractionBlocked => _isPlaying || _interactionBlockFrames > 0;

    public BossDialogueProfile GetBossProfile(int bossId)
    {
        if (_database == null) return null;
        for (int i = 0; i < _database.events.Count; i++)
        {
            var dialogueEvent = _database.events[i];
            if (dialogueEvent != null && dialogueEvent.bossId == bossId && dialogueEvent.bossProfile != null)
                return dialogueEvent.bossProfile;
        }
        return null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // 登记到呈现层装配点：内核只通过 IDialogueModal 接口问「对话是否挡路」
        //（2026-10-22 asmdef 分层第四批）。
        CombatPresenters.Dialogue = this;
        SetOverlayVisible(false);
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void Update()
    {
        if (_interactionBlockFrames > 0)
            _interactionBlockFrames--;

        TrySubscribe();
        if (!_stageStartTriggered && StageBattleState != null && StageBattleState.IsStageInProgress)
            OnStageStateChanged(StageState.InProgress);
        TryPlayNext();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        // 對話被打断（转场/退出战斗）时不能把 timeScale 留在 0，否则下一场景直接冻住
        if (Time.timeScale == 0f) Time.timeScale = 1f;
        if (WaveSpawner.Instance != null)
        {
            WaveSpawner.Instance.OnWaveStarted -= OnWaveStarted;
        }
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.OnBossEngaged -= OnBossEngaged;
            EnemyManager.Instance.OnAnyEnemyDied -= OnEnemyDied;
        }
        if (PlayerState.Instance != null)
            PlayerState.Instance.OnKillCountChanged -= OnKillCountChanged;
        if (StageBattleState != null)
        {
            StageBattleState.StageStateChanged -= OnStageStateChanged;
            StageBattleState.StageVictory -= CommitTutorialProgress;
        }
    }

    public static bool Trigger(string eventId)
    {
        if (Instance == null) return false;
        return Instance.QueueById(eventId);
    }

    public bool QueueById(string eventId)
    {
        return QueueEvent(_database != null ? _database.FindById(eventId) : null);
    }

    /// <summary>把一段对话排入队列并返回是否入队成功（路线运行器用）。</summary>
    /// <param name="forceReplay">true = 忽略「本局已播 / 存档已完成」。教学关的台词由导演按节拍推，必须每次都说。</param>
    public bool QueueEventAndNotify(DialogueEventData dialogueEvent, bool forceReplay = false)
    {
        return QueueEvent(dialogueEvent, forceReplay);
    }

    private bool QueueEvent(DialogueEventData dialogueEvent, bool forceReplay = false)
    {
        if (dialogueEvent == null || dialogueEvent.lines == null || dialogueEvent.lines.Count == 0) return false;
        if (string.IsNullOrEmpty(dialogueEvent.eventId))
        {
            Debug.LogWarning($"[DialogueManager] 对话资产 {dialogueEvent.name} 缺少 eventId");
            return false;
        }
        if (_queuedIds.Contains(dialogueEvent.eventId)) return false;
        if (!forceReplay && _playedThisRun.Contains(dialogueEvent.eventId)) return false;
        if (!forceReplay && dialogueEvent.eventType == DialogueEventType.Tutorial && IsTutorialCompleted(dialogueEvent.eventId)) return false;

        if (forceReplay) _forcedIds.Add(dialogueEvent.eventId);
        else _forcedIds.Remove(dialogueEvent.eventId);
        _queuedIds.Add(dialogueEvent.eventId);
        _queue.Enqueue(dialogueEvent);
        return true;
    }

    private void TryPlayNext()
    {
        if (_isPlaying || _queue.Count == 0 || IsBlockedByOtherUI()) return;
        if (_heroHUD == null)
            _heroHUD = HeroHUD.Instance;
        if (_dialogueView == null)
            _dialogueView = BattleDialogueView.Instance;
        if (_dialogueView == null) return;

        var dialogueEvent = _queue.Dequeue();
        _queuedIds.Remove(dialogueEvent.eventId);
        _isPlaying = true;
        _activeEventId = dialogueEvent.eventId;
        // nonBlocking（路线指引这类）：不暂停、不压暗、不挡点击，玩家可以边看边点
        _pausedForDialogue = !dialogueEvent.nonBlocking;
        SetOverlayVisible(_pausedForDialogue);
        if (_pausedForDialogue) Time.timeScale = 0f;
        _dialogueView.Show(dialogueEvent, () => Finish(dialogueEvent), _pausedForDialogue);
    }

    private void Finish(DialogueEventData dialogueEvent)
    {
        if (_dialogueView != null && _dialogueView.IsShowing)
            _dialogueView.Clear();

        bool forced = _forcedIds.Remove(dialogueEvent.eventId);
        if (dialogueEvent.eventType == DialogueEventType.Tutorial)
        {
            // 强制重播的教学台词不记完成，否则下次进教学关就再也不讲了
            if (!forced) _pendingTutorialIds.Add(dialogueEvent.eventId);
        }
        else
        {
            _playedThisRun.Add(dialogueEvent.eventId);
        }

        _isPlaying = false;
        _interactionBlockFrames = 0;
        _activeEventId = null;
        SetOverlayVisible(false);
        if (_pausedForDialogue) Time.timeScale = 1f;
        _pausedForDialogue = true;
        if (InputManager.Instance != null)
            InputManager.Instance.blockInputFrames = 0;

        if (_heroHUD != null && _heroHUD.flipCard != null)
        {
            bool bossInCombat = HasBossInCombat();
            if (bossInCombat)
                _heroHUD.flipCard.EnterBossCombat();
            else
                _heroHUD.flipCard.ExitBossCombat();
        }
    }

    private bool IsBlockedByOtherUI()
    {
        if (Time.timeScale == 0f) return true;
        if (UpgradeChoiceManager.Instance != null && UpgradeChoiceManager.Instance.IsChoosing) return true;
        if (ItemDiscardPopup.IsShowing) return true;

        var qte = FindObjectOfType<QTEController>();
        return qte != null && qte.State != QTEState.Idle && qte.State != QTEState.QTECompleted;
    }

    private void TrySubscribe()
    {
        if (_subscriptionsSet) return;
        if (WaveSpawner.Instance == null || EnemyManager.Instance == null || PlayerState.Instance == null || StageBattleState == null)
            return;

        WaveSpawner.Instance.OnWaveStarted += OnWaveStarted;
        EnemyManager.Instance.OnBossEngaged += OnBossEngaged;
        EnemyManager.Instance.OnAnyEnemyDied += OnEnemyDied;
        PlayerState.Instance.OnKillCountChanged += OnKillCountChanged;
        StageBattleState.StageStateChanged += OnStageStateChanged;
        StageBattleState.StageVictory += CommitTutorialProgress;
        _subscriptionsSet = true;
    }

    private void OnStageStateChanged(StageState state)
    {
        if (state == StageState.Defeat || state == StageState.Victory)
        {
            CancelActiveDialogue();
            _stageStartTriggered = false;
            return;
        }

        if (state != StageState.InProgress || _stageStartTriggered) return;
        _stageStartTriggered = true;
        ForEachMatching(DialogueTriggerType.StageStart, e => MatchesStage(e));
    }

    private void OnWaveStarted(int waveIndex)
    {
        int waveNumber = waveIndex + 1;
        ForEachMatching(DialogueTriggerType.WaveStart, e => MatchesStage(e) && e.waveNumber == waveNumber);
    }

    private void OnBossEngaged(Enemy boss)
    {
        ForEachMatching(DialogueTriggerType.BossEngaged, e => MatchesStage(e) && MatchesBoss(e, boss));
        if (boss != null)
        {
            boss.OnBossPhaseChanged -= OnBossPhaseChanged;
            boss.OnBossPhaseChanged += OnBossPhaseChanged;
        }
    }

    private void OnBossPhaseChanged(Enemy boss, int phaseIndex)
    {
        ForEachMatching(DialogueTriggerType.BossPhaseChanged, e => MatchesStage(e) && MatchesBoss(e, boss) && e.bossPhaseIndex == phaseIndex);
    }

    private void OnEnemyDied(Enemy enemy)
    {
        if (enemy == null || !enemy.isBoss) return;
        enemy.OnBossPhaseChanged -= OnBossPhaseChanged;
        ForEachMatching(DialogueTriggerType.BossDefeated, e => MatchesStage(e) && MatchesBoss(e, enemy));
    }

    private void OnKillCountChanged(int killCount)
    {
        ForEachMatching(DialogueTriggerType.KillCount, e => MatchesStage(e) && e.killCount == killCount);
    }

    private void ForEachMatching(DialogueTriggerType triggerType, System.Predicate<DialogueEventData> predicate)
    {
        if (_database == null) return;
        for (int i = 0; i < _database.events.Count; i++)
        {
            var dialogueEvent = _database.events[i];
            if (dialogueEvent != null && dialogueEvent.triggerType == triggerType && predicate(dialogueEvent))
                QueueEvent(dialogueEvent);
        }
    }

    private static bool MatchesBoss(DialogueEventData dialogueEvent, Enemy boss)
    {
        return dialogueEvent.bossId == 0 || (boss != null && dialogueEvent.bossId == boss.enemyId);
    }

    private static bool MatchesStage(DialogueEventData dialogueEvent)
    {
        return dialogueEvent.stageId == 0 || (StageBattleState != null && StageBattleState.StageConfig != null && dialogueEvent.stageId == StageBattleState.StageConfig.stageId);
    }

    private static bool HasBossInCombat()
    {
        var enemies = EnemyManager.Instance != null ? EnemyManager.Instance.GetAllAliveEnemies() : null;
        if (enemies == null) return false;
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != null && enemies[i].isBoss && enemies[i].bossState == BossState.InCombat && enemies[i].state != EnemyState.Dead)
                return true;
        }
        return false;
    }

    private static bool HasActiveBoss()
    {
        var enemies = EnemyManager.Instance != null ? EnemyManager.Instance.GetAllAliveEnemies() : null;
        if (enemies == null) return false;
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != null && enemies[i].isBoss && enemies[i].state != EnemyState.Dead)
                return true;
        }
        return false;
    }

    private static bool IsTutorialCompleted(string eventId)
    {
        return SaveManager.Load().completedTutorialDialogueIds.Contains(eventId);
    }

    private void CommitTutorialProgress()
    {
        if (_pendingTutorialIds.Count == 0) return;
        var data = SaveManager.Load();
        foreach (var eventId in _pendingTutorialIds)
        {
            if (!data.completedTutorialDialogueIds.Contains(eventId))
                data.completedTutorialDialogueIds.Add(eventId);
        }
        _pendingTutorialIds.Clear();
        SaveManager.Save(data);
    }

    private void CancelActiveDialogue()
    {
        if (!_isPlaying) return;
        _isPlaying = false;
        _activeEventId = null;
        _dialogueView?.Clear();
        SetOverlayVisible(false);
        bool bossInCombat = HasBossInCombat();
        if (bossInCombat)
            _heroHUD?.flipCard?.EnterBossCombat();
        else
            _heroHUD?.flipCard?.ExitBossCombat();
        if (_pausedForDialogue) Time.timeScale = 1f;
        _pausedForDialogue = true;
    }

    private void SetOverlayVisible(bool visible)
    {
        if (_dimOverlay == null) return;
        _dimOverlay.alpha = visible ? 1f : 0f;
        _dimOverlay.blocksRaycasts = visible;
        _dimOverlay.interactable = visible;
    }

}
