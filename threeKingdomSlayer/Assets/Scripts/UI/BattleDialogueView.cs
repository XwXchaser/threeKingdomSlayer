using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class BattleDialogueView : MonoBehaviour, IPointerClickHandler
{
    public static BattleDialogueView Instance { get; private set; }
    [SerializeField] private Image _clickBlocker;
    [SerializeField] private GameObject _tutorialBubble;
    [SerializeField] private GameObject _playerBubble;
    [SerializeField] private GameObject _bossBubble;
    [SerializeField] private Image _playerTail;
    [SerializeField] private Image _bossTail;
    [SerializeField] private TMP_Text _tutorialText;
    [SerializeField] private TMP_Text _playerText;
    [SerializeField] private TMP_Text _bossText;
    [SerializeField] private Image _bossPortrait;
    [SerializeField] private Image _bossPortraitFrame;
    [SerializeField] private Button _advanceButton;
    [SerializeField] private Button _skipButton;

    [Header("布局")]
    [SerializeField] private Vector2 _playerBubbleOffset = new Vector2(310f, 170f);
    [SerializeField] private float _tailInset = 33f;

    [Header("教学台词自动推进")]
    [Tooltip("教学类台词（DialogueEventType.Tutorial）每行**自动**推进的下限秒数。\n" +
             "真正的停留时长按**字数**算并取大者（中文读速约 6 字/秒）；\n" +
             "固定 2.2 秒读不完 20~30 字的教学台词（2026-10-29 复核）。")]
    [SerializeField] private float _tutorialAutoAdvanceMinSeconds = 2.2f;
    [Tooltip("每字额外停留秒数（0.16 ≈ 6 字/秒）")]
    [SerializeField] private float _tutorialSecondsPerChar = 0.16f;

    private readonly List<DialogueEventData.Line> _lines = new List<DialogueEventData.Line>();
    private Action _onFinished;
    private int _lineIndex;
    private bool _isShowing;

    private bool _pointerPressed;

    /// <summary>当前这行台词的**自动推进倒计时**（秒）；&lt;= 0 = 不自动推进。</summary>
    private float _autoAdvanceRemaining;

    public bool IsShowing => _isShowing;

    /// <summary>是否允许「点一下推进台词」。教学手势门槛期间要关掉，否则玩家做手势的那一下会把台词点没了。</summary>
    public bool PointerAdvanceEnabled { get; set; } = true;

    private void Start()
    {
        // 没有台词时，推进按钮不应该亮着（它在落地页上会压住底部的按钮）
        if (!_isShowing) Clear();
    }

    private void Update()
    {
        if (!_isShowing) return;

        // 教学台词自动推进（2026-10-29）：不靠玩家点；玩家点仍可立即跳过。
        if (_autoAdvanceRemaining > 0f)
        {
            _autoAdvanceRemaining -= Time.unscaledDeltaTime;
            if (_autoAdvanceRemaining <= 0f)
            {
                _autoAdvanceRemaining = 0f;
                Advance();
                return;
            }
        }

        if (!PointerAdvanceEnabled)
        {
            _pointerPressed = false;
            return;
        }

        if (Input.GetMouseButtonDown(0))
            _pointerPressed = true;
        if (Input.GetMouseButtonUp(0) && _pointerPressed)
        {
            _pointerPressed = false;
            Advance();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Advance();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _advanceButton.onClick.AddListener(Advance);
        _skipButton.onClick.AddListener(Skip);
        ApplyDialogueLayout();
        Clear();
    }

    private void ApplyDialogueLayout()
    {
        var heroHud = HeroHUD.Instance;
        if (heroHud == null || heroHud.portraitImage == null) return;

        var root = transform as RectTransform;
        if (root == null) return;

        Vector2 playerScreen = RectTransformUtility.WorldToScreenPoint(null, heroHud.portraitImage.rectTransform.TransformPoint(heroHud.portraitImage.rectTransform.rect.center));
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, playerScreen, null, out var playerCenter)) return;

        var playerBubble = _playerBubble.GetComponent<RectTransform>();
        var bossBubble = _bossBubble.GetComponent<RectTransform>();
        var tutorialBubble = _tutorialBubble.GetComponent<RectTransform>();
        Vector2 playerBubbleCenter = playerCenter + _playerBubbleOffset;
        Vector2 bossBubbleCenter = new Vector2(-playerBubbleCenter.x, playerBubbleCenter.y);

        SetRectCenter(playerBubble, playerBubbleCenter, root);
        SetRectCenter(tutorialBubble, playerBubbleCenter, root);
        SetRectCenter(bossBubble, bossBubbleCenter, root);
        _playerTail.rectTransform.anchoredPosition = playerBubble.anchoredPosition + new Vector2(_tailInset, 0f);
        _bossTail.rectTransform.anchoredPosition = bossBubble.anchoredPosition - new Vector2(_tailInset, 0f);

        _bossPortrait.transform.SetSiblingIndex(3);
        _bossPortraitFrame.transform.SetSiblingIndex(4);
        _playerTail.transform.SetSiblingIndex(5);
        _bossTail.transform.SetSiblingIndex(6);
        tutorialBubble.SetSiblingIndex(7);
        playerBubble.SetSiblingIndex(8);
        bossBubble.SetSiblingIndex(9);

        Vector2 bossPortraitCenter = new Vector2(-playerCenter.x, playerCenter.y);
        SetRectCenter(_bossPortrait.rectTransform, bossPortraitCenter, root);
        SetRectCenter(_bossPortraitFrame.rectTransform, bossPortraitCenter, root);
    }

    private static void SetRectCenter(RectTransform rect, Vector2 center, RectTransform parent)
    {
        Vector2 anchorPoint = parent.rect.min + Vector2.Scale(parent.rect.size, rect.anchorMin);
        rect.anchoredPosition = center - anchorPoint + Vector2.Scale(rect.pivot - Vector2.one * 0.5f, rect.rect.size);
    }

    private void OnDestroy()
    {
        _advanceButton.onClick.RemoveListener(Advance);
        _skipButton.onClick.RemoveListener(Skip);
        if (Instance == this)
            Instance = null;
    }

    public void Show(DialogueEventData dialogueEvent, Action onFinished, bool blockClicks = true)
    {
        _lines.Clear();
        _pointerPressed = false;
        PointerAdvanceEnabled = true;
        _lines.AddRange(dialogueEvent.lines);
        _lineIndex = 0;
        _onFinished = onFinished;
        _isShowing = true;
        _isTutorialEvent = dialogueEvent.eventType == DialogueEventType.Tutorial;

        bool hasBossLines = dialogueEvent.HasBossLines;
        ApplyDialogueLayout();
        SetImage(_bossPortrait, dialogueEvent.bossProfile != null ? dialogueEvent.bossProfile.portrait : null);
        SetImage(_bossPortraitFrame, dialogueEvent.bossProfile != null ? dialogueEvent.bossProfile.portraitFrame : null);
        _bossPortrait.gameObject.SetActive(hasBossLines && _bossPortrait.sprite != null);
        _bossPortraitFrame.gameObject.SetActive(hasBossLines && _bossPortraitFrame.sprite != null);
        _skipButton.gameObject.SetActive(dialogueEvent.eventType == DialogueEventType.Conversation);
        _advanceButton.gameObject.SetActive(blockClicks);
        _clickBlocker.gameObject.SetActive(blockClicks);
        RenderLine();
    }

    public void Clear()
    {
        _isShowing = false;
        _pointerPressed = false;
        _autoAdvanceRemaining = 0f;
        _onFinished = null;
        _lines.Clear();
        _tutorialBubble.SetActive(false);
        _playerBubble.SetActive(false);
        _bossBubble.SetActive(false);
        _playerTail.gameObject.SetActive(false);
        _bossTail.gameObject.SetActive(false);
        _bossPortrait.gameObject.SetActive(false);
        _bossPortraitFrame.gameObject.SetActive(false);
        _clickBlocker.gameObject.SetActive(false);
        _advanceButton.gameObject.SetActive(false);
        _skipButton.gameObject.SetActive(false);
    }

    public void Advance()
    {
        if (!_isShowing) return;
        _lineIndex++;
        if (_lineIndex >= _lines.Count)
        {
            Finish();
            return;
        }
        RenderLine();
    }

    public void Skip()
    {
        if (_isShowing) Finish();
    }

    private void RenderLine()
    {
        var line = _lines[_lineIndex];
        bool hasBossLines = false;
        for (int i = 0; i < _lines.Count; i++)
        {
            if (_lines[i].speaker == DialogueSpeaker.Boss)
            {
                hasBossLines = true;
                break;
            }
        }

        bool tutorialLayout = !hasBossLines;
        bool playerLine = !tutorialLayout && line.speaker == DialogueSpeaker.Player;
        bool bossLine = !tutorialLayout && line.speaker == DialogueSpeaker.Boss;
        _tutorialBubble.SetActive(tutorialLayout);
        _playerBubble.SetActive(playerLine);
        _bossBubble.SetActive(bossLine);
        _playerTail.gameObject.SetActive(tutorialLayout || playerLine);
        _bossTail.gameObject.SetActive(bossLine);

        if (tutorialLayout)
            _tutorialText.text = line.text;
        else if (line.speaker == DialogueSpeaker.Player)
            _playerText.text = line.text;
        else
            _bossText.text = line.text;

        // 教学类台词：排好自动推进的倒计时（正式对话不自动推进）
        _autoAdvanceRemaining = _isTutorialEvent ? ComputeAutoAdvanceSeconds(line.text) : 0f;
    }

    /// <summary>当前播的是否是教学类台词（只有教学类才自动推进）。</summary>
    private bool _isTutorialEvent;

    /// <summary>这一行台词自动推进前要停多久：下限 2.2 秒，按字数再往上加（中文读速 ≈ 6 字/秒）。</summary>
    private float ComputeAutoAdvanceSeconds(string text)
    {
        int chars = 0;
        if (!string.IsNullOrEmpty(text))
        {
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] > ' ' && text[i] < (char)0xFFFD) chars++;   // 跳过空白
            }
        }
        return Mathf.Max(_tutorialAutoAdvanceMinSeconds, chars * _tutorialSecondsPerChar);
    }

    private void Finish()
    {
        if (!_isShowing) return;
        var callback = _onFinished;
        Clear();
        callback?.Invoke();
    }

    private static void SetImage(Image image, Sprite sprite)
    {
        image.sprite = sprite;
        image.enabled = sprite != null;
    }
}
