using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class HeroHUDDialogue : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_Text _singleDialogueText;
    [SerializeField] private TMP_Text _playerDialogueText;
    [SerializeField] private TMP_Text _bossDialogueText;

    [Header("Portraits")]
    [SerializeField] private Image _playerPortrait;
    [SerializeField] private Image _playerPortraitFrame;
    [SerializeField] private Image _bossPortrait;
    [SerializeField] private Image _bossPortraitFrame;

    [Header("Interaction")]
    [SerializeField] private Button _advanceButton;
    [SerializeField] private Button _skipButton;

    private readonly List<DialogueEventData.Line> _lines = new List<DialogueEventData.Line>();
    private int _lineIndex;
    private System.Action _onFinished;
    private bool _isShowing;

    public bool IsShowing => _isShowing;

    private void Awake()
    {
        if (_advanceButton != null)
            _advanceButton.onClick.AddListener(Advance);
        if (_skipButton != null)
            _skipButton.onClick.AddListener(Skip);
        Clear();
    }

    private void OnDestroy()
    {
        if (_advanceButton != null)
            _advanceButton.onClick.RemoveListener(Advance);
        if (_skipButton != null)
            _skipButton.onClick.RemoveListener(Skip);
    }

    public void Show(DialogueEventData dialogueEvent, Sprite playerPortrait, System.Action onFinished)
    {
        if (dialogueEvent == null || dialogueEvent.lines == null || dialogueEvent.lines.Count == 0)
        {
            onFinished?.Invoke();
            return;
        }

        _lines.Clear();
        _lines.AddRange(dialogueEvent.lines);
        _lineIndex = 0;
        _onFinished = onFinished;
        _isShowing = true;

        SetImage(_bossPortrait, dialogueEvent.bossProfile != null ? dialogueEvent.bossProfile.portrait : null);
        SetImage(_bossPortraitFrame, dialogueEvent.bossProfile != null ? dialogueEvent.bossProfile.portraitFrame : null);
        SetVisible(_playerPortrait, false);
        SetVisible(_playerPortraitFrame, false);
        SetVisible(_bossPortrait, dialogueEvent.HasBossLines);
        SetVisible(_bossPortraitFrame, dialogueEvent.HasBossLines && _bossPortraitFrame != null && _bossPortraitFrame.sprite != null);
        SetVisible(_advanceButton, true);
        SetVisible(_skipButton, dialogueEvent.eventType == DialogueEventType.Conversation);
        RenderLine();
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

    public void Clear()
    {
        _isShowing = false;
        _onFinished = null;
        _lines.Clear();
        SetText(_singleDialogueText, string.Empty);
        SetText(_playerDialogueText, string.Empty);
        SetText(_bossDialogueText, string.Empty);
        SetVisible(_playerDialogueText, false);
        SetVisible(_bossDialogueText, false);
        SetVisible(_singleDialogueText, false);
        SetVisible(_playerPortrait, false);
        SetVisible(_playerPortraitFrame, false);
        SetVisible(_bossPortrait, false);
        SetVisible(_bossPortraitFrame, false);
        SetVisible(_advanceButton, false);
        SetVisible(_skipButton, false);
    }

    private void RenderLine()
    {
        var line = _lines[_lineIndex];
        bool hasBossLine = false;
        for (int i = 0; i < _lines.Count; i++)
        {
            if (_lines[i].speaker == DialogueSpeaker.Boss)
            {
                hasBossLine = true;
                break;
            }
        }

        if (!hasBossLine)
        {
            SetVisible(_singleDialogueText, true);
            SetVisible(_playerDialogueText, false);
            SetVisible(_bossDialogueText, false);
            SetText(_singleDialogueText, line.text);
            return;
        }

        SetVisible(_singleDialogueText, false);
        SetVisible(_playerDialogueText, line.speaker == DialogueSpeaker.Player);
        SetVisible(_bossDialogueText, line.speaker == DialogueSpeaker.Boss);
        SetText(line.speaker == DialogueSpeaker.Player ? _playerDialogueText : _bossDialogueText, line.text);

        if (_playerPortrait != null) _playerPortrait.color = line.speaker == DialogueSpeaker.Player ? Color.white : new Color(0.55f, 0.55f, 0.55f, 1f);
        if (_bossPortrait != null) _bossPortrait.color = line.speaker == DialogueSpeaker.Boss ? Color.white : new Color(0.55f, 0.55f, 0.55f, 1f);
    }

    private void Finish()
    {
        if (!_isShowing) return;
        _isShowing = false;
        var callback = _onFinished;
        _onFinished = null;
        _lines.Clear();
        Clear();
        callback?.Invoke();
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null) text.text = value;
    }

    private static void SetImage(Image image, Sprite sprite)
    {
        if (image == null) return;
        image.sprite = sprite;
        image.enabled = sprite != null;
    }

    private static void SetVisible(Component component, bool visible)
    {
        if (component != null) component.gameObject.SetActive(visible);
    }
}
