using UnityEngine;

public sealed class DialogueBubbleLayout : MonoBehaviour
{
    [SerializeField] private RectTransform _tutorialBubble;
    [SerializeField] private RectTransform _playerBubble;
    [SerializeField] private RectTransform _bossBubble;

    public RectTransform TutorialBubble => _tutorialBubble;
    public RectTransform PlayerBubble => _playerBubble;
    public RectTransform BossBubble => _bossBubble;
}
