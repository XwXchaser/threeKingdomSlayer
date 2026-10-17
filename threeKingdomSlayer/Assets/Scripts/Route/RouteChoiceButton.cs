using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public sealed class RouteChoiceButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image iconImage;
    [SerializeField] private CanvasGroup visualGroup;
    [SerializeField] private float guideMoveDistance = 10f;
    [SerializeField] private float guideCycleDuration = 0.9f;
    [SerializeField] private float breathingAlpha = 0.12f;
    [SerializeField] private float breathingScale = 0.025f;
    [NonSerialized] public Text fallbackText;

    private string _id;
    private Action<string> _onSelected;
    private RectTransform _rectTransform;
    private RectTransform _iconRectTransform;
    private Vector2 _baseAnchoredPosition;
    private Vector3 _baseLocalScale;
    private LayoutElement _layoutElement;
    private Sequence _visualSequence;
    private Coroutine _visualStartCoroutine;

    public void Bind(RouteChoiceOption option, Action<string> onSelected)
    {
        _id = option.id;
        _onSelected = onSelected;
        if (labelText != null) labelText.text = option.label;
        if (descriptionText != null)
        {
            descriptionText.text = option.description;
            descriptionText.gameObject.SetActive(!string.IsNullOrEmpty(option.description));
        }
        if (iconImage != null)
        {
            iconImage.sprite = option.icon;
            iconImage.gameObject.SetActive(option.icon != null);
        }
        if (fallbackText != null) fallbackText.text = option.label;
        if (button == null) button = GetComponent<Button>();
        EnsureVisualSetup();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(Select);
            button.interactable = true;
        }
        StartVisualLoopNextFrame();
    }

    private void StartVisualLoopNextFrame()
    {
        _visualSequence?.Kill();
        if (_visualStartCoroutine != null)
            StopCoroutine(_visualStartCoroutine);
        _visualStartCoroutine = StartCoroutine(StartVisualLoopCoroutine());
    }

    private System.Collections.IEnumerator StartVisualLoopCoroutine()
    {
        yield return null;
        StartVisualLoop();
    }

    private void Awake()
    {
        EnsureVisualSetup();
    }

    private void EnsureVisualSetup()
    {
        if (_rectTransform == null)
            _rectTransform = transform as RectTransform;
        if (_iconRectTransform == null && iconImage != null)
            _iconRectTransform = iconImage.rectTransform;
        if (visualGroup == null)
        {
            visualGroup = GetComponent<CanvasGroup>();
            if (visualGroup == null)
                visualGroup = gameObject.AddComponent<CanvasGroup>();
        }
        if (_layoutElement == null)
        {
            _layoutElement = GetComponent<LayoutElement>();
            if (_layoutElement == null)
                _layoutElement = gameObject.AddComponent<LayoutElement>();
        }
    }

    private void OnDestroy()
    {
        _visualSequence?.Kill();
    }

    private void StartVisualLoop()
    {
        EnsureVisualSetup();
        if (_rectTransform == null || visualGroup == null) return;
        Canvas.ForceUpdateCanvases();
        _visualSequence?.Kill();
        _baseAnchoredPosition = _rectTransform.anchoredPosition;
        _baseLocalScale = _rectTransform.localScale;
        visualGroup.alpha = 1f;

        float cycleDuration = Mathf.Max(0.05f, guideCycleDuration);
        Vector2 guideDirection = GetGuideDirection();
        Vector2 guideTarget = _baseAnchoredPosition + guideDirection * guideMoveDistance;
        _visualSequence = DOTween.Sequence().SetTarget(this).SetUpdate(true);
        _visualSequence.Append(_rectTransform.DOAnchorPos(guideTarget, cycleDuration).SetEase(Ease.InOutSine));
        _visualSequence.Append(_rectTransform.DOAnchorPos(_baseAnchoredPosition, cycleDuration).SetEase(Ease.InOutSine));
        _visualSequence.Join(visualGroup.DOFade(1f - breathingAlpha, cycleDuration).SetEase(Ease.InOutSine));
        _visualSequence.Join(_rectTransform.DOScale(_baseLocalScale * (1f + breathingScale), cycleDuration).SetEase(Ease.InOutSine));
        _visualSequence.SetLoops(-1, LoopType.Restart);
    }

    private Vector2 GetGuideDirection()
    {
        if (_iconRectTransform == null || _rectTransform.parent == null)
            return Vector2.up;

        Vector3 worldDirection = _iconRectTransform.TransformDirection(Vector3.up);
        Vector3 parentDirection = _rectTransform.parent.InverseTransformDirection(worldDirection);
        Vector2 direction = new Vector2(parentDirection.x, parentDirection.y);
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up;
    }

    public void ApplyLayout(RouteChoiceLayout layout, int index, int count)
    {
        EnsureVisualSetup();
        if (_rectTransform == null) return;
        Vector2 position = GetDefaultPosition(index, count);
        float rotation = 0f;
        if (layout != null && layout.overrideDefault)
        {
            position = layout.anchoredPosition;
            rotation = layout.rotation;
        }
        _rectTransform.anchoredPosition = position;
        _rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
    }

    private static Vector2 GetDefaultPosition(int index, int count)
    {
        if (count <= 1) return Vector2.zero;
        float spacing = 160f;
        return new Vector2((index - (count - 1) * 0.5f) * spacing, 0f);
    }

    private void Select()
    {
        if (button != null) button.interactable = false;
        _visualSequence?.Kill();
        _onSelected?.Invoke(_id);
    }
}
