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
    private bool _qteActivitySubscribed;
    private bool _lastObservedQteActivity;
    private System.Action<Enemy> _onBossEngaged;
    private System.Action<Enemy> _onEnemyDied;
    private bool _bossEventsSubscribed;

    private void Start()
    {
        if (_stageProgressBar == null)
            _stageProgressBar = GetComponentInChildren<StageProgressBar>(true);
        if (_stageProgressBar != null && _displayVersion != DisplayVersion.V3 && !UsesV2QTEOnlyFlip())
            _stageProgressBar.OnBossTransitionComplete += EnterBossCombat;
        TrySubscribeBossEvents();
        ApplyDisplayVersion();
        SubscribeQTEActivity();
    }

    public void SetDisplayVersion(DisplayVersion version)
    {
        if (_displayVersion == version) return;
        _displayVersion = version;

        if (_stageProgressBar != null)
            _stageProgressBar.OnBossTransitionComplete -= EnterBossCombat;
        if (version != DisplayVersion.V3 && _stageProgressBar != null && !UsesV2QTEOnlyFlip())
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
        if (!UsesV2QTEOnlyFlip()) return;

        bool active = QTEActivityHub.IsActive;
        if (active != _lastObservedQteActivity)
            OnQTEActivityChanged(active);
    }

    private void SubscribeQTEActivity()
    {
        Debug.Log($"[QTE_FLIP_DIAG] HUD Subscribe name={name}#{GetInstanceID()} enabled={enabled} active={gameObject.activeInHierarchy} usesV2={UsesV2QTEOnlyFlip()}");
        QTEActivityHub.Subscribe(OnQTEActivityChanged);
        _qteActivitySubscribed = true;
        OnQTEActivityChanged(QTEActivityHub.IsActive);
    }

    private void OnEnable()
    {
        SubscribeQTEActivity();
    }

    private void OnQTEActivityChanged(bool active)
    {
        Debug.Log($"[QTE_FLIP_DIAG] OnQTEActivityChanged active={active} usesV2={UsesV2QTEOnlyFlip()} _showingBack={_showingBack} _backReason={_backReason}");
        _lastObservedQteActivity = active;
        if (!UsesV2QTEOnlyFlip()) return;
        if (active)
            ShowBack(FlipReason.QTE);
        else
            ShowFront(FlipReason.QTE);
    }

    private static bool UsesV2QTEOnlyFlip()
    {
        return ActiveSkillInventory.Instance != null && ActiveSkillInventory.Instance.UsesActiveSkills;
    }

    public void EnterBossCombat()
    {
        if (UsesV2QTEOnlyFlip()) return;
        _bossCombatActive = true;
        if (_displayVersion == DisplayVersion.V3) return;
        _backReason = FlipReason.BossCombat;
        SetSide(true);
    }

    public void ExitBossCombat()
    {
        if (UsesV2QTEOnlyFlip()) return;
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
        SetVisibleSide(false);
    }

    private void TrySubscribeBossEvents()
    {
        if (UsesV2QTEOnlyFlip() || _bossEventsSubscribed || EnemyManager.Instance == null) return;

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

    /// <summary>
    /// 翻转卡片到目标面。
    /// 状态位 _showingBack 是唯一事实来源；旋转动画只是视觉效果，方向判断不再读取欧拉角
    /// （Quaternion.Euler(180,0,0) 的 eulerAngles 在 Unity 中被解算为 (0,180,180)，localEulerAngles.x 会读回 0）。
    /// </summary>
    private void SetSide(bool showBack)
    {
        if (_card == null || _frontFace == null || _backFace == null)
        {
            Debug.Log($"[QTE_FLIP_DIAG] SetSide showBack={showBack} NULL_REF card={_card == null} front={_frontFace == null} back={_backFace == null}");
            return;
        }

        // 目标面与当前状态一致：仅同步可见面并清理残留 tween
        if (_showingBack == showBack)
        {
            Debug.Log($"[QTE_FLIP_DIAG] SetSide showBack={showBack} ALREADY_SAME — sync only");
            _flipTween?.Kill();
            _flipTween = null;
            SetVisibleSide(showBack);
            return;
        }

        bool oldShowingBack = _showingBack;
        _showingBack = showBack;
        _flipTween?.Kill();
        _flipTween = null;

        float targetX = showBack ? 180f : 0f;

        // 先快照到当前应显示的面，保证旧面在翻转前可见
        SetVisibleSide(oldShowingBack);

        // 目标与快照角度一致（反向翻转打断时旧目标即新起点）则直接落位
        float snapX = oldShowingBack ? 180f : 0f;
        _card.localRotation = Quaternion.Euler(snapX, 0f, 0f);

        if (Mathf.Abs(Mathf.DeltaAngle(snapX, targetX)) <= 0.1f)
        {
            _card.localRotation = Quaternion.Euler(targetX, 0f, 0f);
            SetVisibleSide(showBack);
            return;
        }

        bool sideChanged = false;
        var sequence = DOTween.Sequence().SetTarget(this).SetUpdate(true);
        _flipTween = sequence;
        Debug.Log($"[QTE_FLIP_DIAG] SetSide showBack={showBack} STARTING_FLIP snapX={snapX} targetX={targetX} duration={_flipDuration}");
        sequence.Append(_card.DOLocalRotate(new Vector3(targetX, 0f, 0f), _flipDuration, RotateMode.Fast)
            .SetEase(Ease.InOutQuad)
            .OnUpdate(() =>
            {
                if (sideChanged || _flipTween == null || _flipTween.ElapsedPercentage() < 0.5f) return;

                sideChanged = true;
                SetVisibleSide(showBack);
            }));
        sequence.OnComplete(() =>
        {
            _card.localRotation = Quaternion.Euler(targetX, 0f, 0f);
            SetVisibleSide(showBack);
            _flipTween = null;
        });
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

    private void OnDisable()
    {
        if (!_qteActivitySubscribed) return;
        QTEActivityHub.Unsubscribe(OnQTEActivityChanged);
        _qteActivitySubscribed = false;
    }

    private void OnDestroy()
    {
        OnDisable();
        if (_stageProgressBar != null)
            _stageProgressBar.OnBossTransitionComplete -= EnterBossCombat;
        if (_bossEventsSubscribed && EnemyManager.Instance != null)
        {
            EnemyManager.Instance.OnBossEngaged -= _onBossEngaged;
            EnemyManager.Instance.OnAnyEnemyDied -= _onEnemyDied;
        }
        _flipTween?.Kill();
    }
}
