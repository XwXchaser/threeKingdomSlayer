using DG.Tweening;
using UnityEngine;

public sealed class HeroHUDFlipCard : MonoBehaviour
{
    public enum FlipReason { None, QTE, Dialogue, BossCombat }
    public enum DisplayVersion { V1_V2, V3 }

    [SerializeField] private RectTransform _card;
    [SerializeField] private CanvasGroup _frontFace;
    [SerializeField] private CanvasGroup _backFace;
    [SerializeField] private float _flipDuration = 0.35f;
    [SerializeField] private StageProgressBar _stageProgressBar;
    [SerializeField] private DisplayVersion _displayVersion = DisplayVersion.V1_V2;

    [Header("V3")]
    [SerializeField] private FrontItemBar _frontItemBar;
    [SerializeField] private BuffDisplayPanel _leftBuffPanel;

    private Tween _flipTween;
    private bool _showingBack;
    private bool _bossCombatActive;
    private FlipReason _backReason;
    private float _rotationX;
    private QTEController _qteController;
    private System.Action _onQteTriggered;
    private System.Action _onQteFinished;
    private System.Action<Enemy> _onBossEngaged;
    private System.Action<Enemy> _onEnemyDied;
    private bool _bossEventsSubscribed;

    private void Start()
    {
        _rotationX = _card != null ? _card.localEulerAngles.x : 0f;
        if (_stageProgressBar == null)
            _stageProgressBar = GetComponentInChildren<StageProgressBar>(true);
        if (_stageProgressBar != null && _displayVersion != DisplayVersion.V3)
            _stageProgressBar.OnBossTransitionComplete += EnterBossCombat;
        TrySubscribeBossEvents();
        ApplyDisplayVersion();
    }

    public void SetDisplayVersion(DisplayVersion version)
    {
        if (_displayVersion == version) return;
        _displayVersion = version;

        if (_stageProgressBar != null)
            _stageProgressBar.OnBossTransitionComplete -= EnterBossCombat;
        if (version != DisplayVersion.V3 && _stageProgressBar != null)
            _stageProgressBar.OnBossTransitionComplete += EnterBossCombat;

        // V3 切回 V1_V2 时，如果当前在 QTE 翻牌状态，先翻回正面
        if (version == DisplayVersion.V1_V2 && _showingBack && _backReason == FlipReason.QTE && !_bossCombatActive)
            SetSide(false);

        ApplyDisplayVersion();
    }

    private void ApplyDisplayVersion()
    {
        bool isV3 = _displayVersion == DisplayVersion.V3;

        if (_stageProgressBar != null)
            _stageProgressBar.gameObject.SetActive(!isV3);

        if (_frontItemBar != null)
            _frontItemBar.gameObject.SetActive(isV3);

        if (_leftBuffPanel != null)
        {
            _leftBuffPanel.SetColumnBVisible(!isV3);
            // V1 才需要血包飞行目标；V2 主动技能不包含血包。
            if (isV3 && _frontItemBar != null && HealthPotionManager.Instance != null && HealthPotionManager.Instance.IsEnabledForCurrentRules)
                _frontItemBar.TryAssignPotionTarget();
        }
    }

    private void Update()
    {
        TrySubscribeBossEvents();

        if (_qteController != null) return;

        _qteController = UnityEngine.Object.FindObjectOfType<QTEController>();
        if (_qteController == null) return;

        _onQteTriggered = () => ShowBack(FlipReason.QTE);
        _onQteFinished = () => ShowFront(FlipReason.QTE);
        _qteController.OnQTETriggered += _onQteTriggered;
        _qteController.OnQTEAttackFinished += _onQteFinished;
    }


    public void EnterBossCombat()
    {
        _bossCombatActive = true;
        if (_displayVersion == DisplayVersion.V3) return;
        _backReason = FlipReason.BossCombat;
        SetSide(true);
    }

    public void ExitBossCombat()
    {
        _bossCombatActive = false;
        if (_displayVersion == DisplayVersion.V3) return;
        if (_backReason == FlipReason.BossCombat)
        {
            _backReason = FlipReason.None;
            SetSide(false);
        }
    }

    public void ShowBack(FlipReason reason)
    {
        _backReason = reason;
        SetSide(true);
    }

    public void ShowFront(FlipReason reason)
    {
        if (_backReason != reason) return;
        if (_displayVersion == DisplayVersion.V3)
        {
            _backReason = FlipReason.None;
            SetSide(false);
            return;
        }
        _backReason = _bossCombatActive ? FlipReason.BossCombat : FlipReason.None;
        SetSide(_bossCombatActive);
    }

    public void Configure(RectTransform card, CanvasGroup frontFace, CanvasGroup backFace)
    {
        _card = card;
        _frontFace = frontFace;
        _backFace = backFace;
        _rotationX = _card.localEulerAngles.x;
        SetVisibleSide(false);
    }

    private void TrySubscribeBossEvents()
    {
        if (_bossEventsSubscribed || EnemyManager.Instance == null) return;

        _onBossEngaged = boss =>
        {
            // StageProgressBar 负责“节点移动完成后”再翻面；未初始化时保留原有直接翻面。
            if (_stageProgressBar == null || !_stageProgressBar.gameObject.activeInHierarchy)
            {
                if (_displayVersion != DisplayVersion.V3)
                    EnterBossCombat();
            }
        };
        _onEnemyDied = enemy =>
        {
            if (enemy.isBoss && _displayVersion != DisplayVersion.V3)
                ExitBossCombat();
        };
        EnemyManager.Instance.OnBossEngaged += _onBossEngaged;
        EnemyManager.Instance.OnAnyEnemyDied += _onEnemyDied;
        _bossEventsSubscribed = true;
    }

    private void SetSide(bool showBack)
    {
        if (_card == null || _frontFace == null || _backFace == null)
            return;
        if (_showingBack == showBack)
        {
            SetVisibleSide(showBack);
            return;
        }

        _showingBack = showBack;
        _flipTween?.Kill();

        float targetX = showBack ? _rotationX + 180f : _rotationX + 180f;
        _rotationX = targetX;
        var sequence = DOTween.Sequence().SetTarget(this).SetUpdate(true);
        _flipTween = sequence;
        sequence.Append(_card.DOLocalRotate(new Vector3(targetX, 0f, 0f), _flipDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.InOutQuad));
        sequence.InsertCallback(_flipDuration * 0.5f, () => SetVisibleSide(showBack));
    }

    private void SetVisibleSide(bool showBack)
    {
        _frontFace.alpha = showBack ? 0f : 1f;
        _frontFace.blocksRaycasts = !showBack;
        _frontFace.interactable = !showBack;
        _backFace.alpha = showBack ? 1f : 0f;
        _backFace.blocksRaycasts = showBack;
        _backFace.interactable = showBack;
    }

    private void OnDestroy()
    {
        if (_qteController != null)
        {
            if (_onQteTriggered != null)
                _qteController.OnQTETriggered -= _onQteTriggered;
            if (_onQteFinished != null)
                _qteController.OnQTEAttackFinished -= _onQteFinished;
        }
        if (_stageProgressBar != null)
            _stageProgressBar.OnBossTransitionComplete -= EnterBossCombat;
        _flipTween?.Kill();
    }
}
