using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [SerializeField] private DialogueDatabase _database;
    [SerializeField] private CanvasGroup _dimOverlay;
    [SerializeField] private BattleDialogueView _dialogueView;

    private readonly Queue<DialogueEventData> _queue = new Queue<DialogueEventData>();
    private readonly HashSet<string> _playedThisRun = new HashSet<string>();
    private readonly HashSet<string> _pendingTutorialIds = new HashSet<string>();
    private readonly HashSet<string> _queuedIds = new HashSet<string>();
    private HeroHUD _heroHUD;
    private bool _isPlaying;
    private int _interactionBlockFrames;
    private string _activeEventId;
    private bool _stageStartTriggered;
    private bool _subscriptionsSet;

    public bool IsPlaying => _isPlaying;
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
        if (!_stageStartTriggered && StageController.Instance != null && StageController.Instance.IsStageInProgress)
            OnStageStateChanged(StageState.InProgress);
        TryPlayNext();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
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
        if (StageController.Instance != null)
        {
            StageController.Instance.OnStageStateChanged -= OnStageStateChanged;
            StageController.Instance.OnStageVictory -= CommitTutorialProgress;
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

    private bool QueueEvent(DialogueEventData dialogueEvent)
    {
        if (dialogueEvent == null || dialogueEvent.lines == null || dialogueEvent.lines.Count == 0) return false;
        if (string.IsNullOrEmpty(dialogueEvent.eventId))
        {
            Debug.LogWarning($"[DialogueManager] 对话资产 {dialogueEvent.name} 缺少 eventId");
            return false;
        }
        if (_queuedIds.Contains(dialogueEvent.eventId) || _playedThisRun.Contains(dialogueEvent.eventId)) return false;
        if (dialogueEvent.eventType == DialogueEventType.Tutorial && IsTutorialCompleted(dialogueEvent.eventId)) return false;

        _queuedIds.Add(dialogueEvent.eventId);
        _queue.Enqueue(dialogueEvent);
        return true;
    }

    private void TryPlayNext()
    {
        if (_isPlaying || _queue.Count == 0 || IsBlockedByOtherUI()) return;
        if (_heroHUD == null)
            _heroHUD = FindObjectOfType<HeroHUD>();
        if (_dialogueView == null)
            _dialogueView = FindObjectOfType<BattleDialogueView>();
        if (_dialogueView == null) return;

        var dialogueEvent = _queue.Dequeue();
        _queuedIds.Remove(dialogueEvent.eventId);
        _isPlaying = true;
        _activeEventId = dialogueEvent.eventId;
        SetOverlayVisible(true);
        Time.timeScale = 0f;
        _dialogueView.Show(dialogueEvent, () => Finish(dialogueEvent));
    }

    private void Finish(DialogueEventData dialogueEvent)
    {
        if (_dialogueView != null && _dialogueView.IsShowing)
            _dialogueView.Clear();

        if (dialogueEvent.eventType == DialogueEventType.Tutorial)
            _pendingTutorialIds.Add(dialogueEvent.eventId);
        else
            _playedThisRun.Add(dialogueEvent.eventId);

        _isPlaying = false;
        _interactionBlockFrames = 0;
        _activeEventId = null;
        SetOverlayVisible(false);
        Time.timeScale = 1f;
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
        if (WaveSpawner.Instance == null || EnemyManager.Instance == null || PlayerState.Instance == null || StageController.Instance == null)
            return;

        WaveSpawner.Instance.OnWaveStarted += OnWaveStarted;
        EnemyManager.Instance.OnBossEngaged += OnBossEngaged;
        EnemyManager.Instance.OnAnyEnemyDied += OnEnemyDied;
        PlayerState.Instance.OnKillCountChanged += OnKillCountChanged;
        StageController.Instance.OnStageStateChanged += OnStageStateChanged;
        StageController.Instance.OnStageVictory += CommitTutorialProgress;
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
        return dialogueEvent.stageId == 0 || (StageController.Instance != null && StageController.Instance.stageConfig != null && dialogueEvent.stageId == StageController.Instance.stageConfig.stageId);
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
        Time.timeScale = 1f;
    }

    private void SetOverlayVisible(bool visible)
    {
        if (_dimOverlay == null) return;
        _dimOverlay.alpha = visible ? 1f : 0f;
        _dimOverlay.blocksRaycasts = visible;
        _dimOverlay.interactable = visible;
    }

}
