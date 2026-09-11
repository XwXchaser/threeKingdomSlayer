using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using TMPro;
public sealed class FakeMovementPresenter : MonoBehaviour
{
    [SerializeField] private Button skipButton;
    [SerializeField] private Camera backgroundCamera;
    [SerializeField] private RawImage backgroundImage;
    [SerializeField] private Image legacyBackgroundImage;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RenderTexture videoRenderTexture;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private RectTransform chibiOverlay;
    [SerializeField] private Image blackoutImage;
    [SerializeField] private Image chibiImage;
    [SerializeField] private TMP_Text chibiTitle;
    [SerializeField] private TMP_Text chibiSubtitle;

    private bool _playing;
    private bool _skipRequested;
    private float _remaining;
    private string _choiceLabel;
    private bool _videoPrepared;
    private bool _skipAllowed;
    private bool _loop;
    private bool _coveredBackgroundPrepared;
    private bool _holdLastFrame;
    private bool _heldFrameAsBackground;
    private bool _fastForwarding;
    private UnityEngine.UI.AspectRatioFitter _videoAspectFitter;
    private Vector2 _blackoutStartPosition;
    private Vector2 _blackoutCenterPosition;
    private Vector2 _blackoutEndPosition;

    public bool IsPlaying => _playing;

    private void Awake()
    {
        if (skipButton != null)
            skipButton.gameObject.SetActive(false);
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = videoRenderTexture;
            if (audioSource != null)
            {
                videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                videoPlayer.EnableAudioTrack(0, true);
                videoPlayer.SetTargetAudioSource(0, audioSource);
            }
            videoPlayer.loopPointReached += OnVideoFinished;
        }
        if (chibiOverlay != null && blackoutImage != null)
        {
            float width = chibiOverlay.rect.width;
            _blackoutCenterPosition = blackoutImage.rectTransform.anchoredPosition;
            _blackoutStartPosition = _blackoutCenterPosition + Vector2.right * width;
            _blackoutEndPosition = _blackoutCenterPosition + Vector2.left * width;
            chibiOverlay.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (videoPlayer != null) videoPlayer.loopPointReached -= OnVideoFinished;
    }

    public void SetBattleBackground(FakeRoutePresentation presentation)
    {
        if (_playing || presentation == null) return;
        if (_heldFrameAsBackground)
        {
            _heldFrameAsBackground = false;
            _videoPrepared = false;
            return;
        }
        if (_coveredBackgroundPrepared)
        {
            _coveredBackgroundPrepared = false;
            return;
        }
        ApplyBattleBackground(presentation);
    }

    public void PrepareCoveredBackground(FakeRoutePresentation presentation)
    {
        if (!_playing || presentation == null) return;
        ApplyBattleBackground(presentation);
        _coveredBackgroundPrepared = true;
    }

    private void ApplyBattleBackground(FakeRoutePresentation presentation)
    {
        _skipAllowed = false;
        _loop = presentation.loop;
        if (presentation.mode == FakeRoutePresentationMode.Video && presentation.videoClip != null)
        {
            if (videoPlayer == null) return;
            videoPlayer.clip = presentation.videoClip;
            videoPlayer.isLooping = presentation.loop;
            videoPlayer.Prepare();
            if (backgroundImage != null)
            {
                backgroundImage.texture = videoRenderTexture;
                backgroundImage.color = Color.white;
                ConfigureVideoAspect(presentation.videoClip);
            }
            videoPlayer.Play();
            if (audioSource != null && presentation.audioClip != null)
            {
                audioSource.clip = presentation.audioClip;
                audioSource.loop = presentation.loop;
                audioSource.Play();
            }
        }
        else
        {
            StopMedia();
            ApplyTexture(presentation.staticImage != null ? presentation.staticImage.texture : null);
        }
    }

    public IEnumerator PlayOpening(FakeRoutePresentation presentation, Func<bool> canContinue)
    {
        yield return PlayPresentation(presentation, "关卡开场", canContinue, false);
    }

    public IEnumerator PlayRouteChoiceTransition(FakeRouteNodeConfig node, Func<bool> canContinue)
    {
        yield return PlayPresentation(node != null ? node.routeChoiceTransition : null, "路线选择转场", canContinue, false);
    }

    public void ShowRouteChoiceBackground(FakeRouteNodeConfig node)
    {
        if (_playing) return;
        var presentation = node != null ? node.routeChoiceBackground : null;
        StopMedia();
        _videoPrepared = false;
        ApplyTexture(presentation != null && presentation.staticImage != null ? presentation.staticImage.texture : null);
    }

    private IEnumerator PlayPresentation(FakeRoutePresentation presentation, string label, Func<bool> canContinue, bool allowPlaceholder, Action onCovered = null)
    {
        if (presentation != null && presentation.mode == FakeRoutePresentationMode.ChibiBlackout)
        {
            yield return PlayChibiBlackout(presentation, label, canContinue, onCovered);
            yield break;
        }

        _playing = true;
        _skipRequested = false;
        _choiceLabel = label;
        bool hasVideo = presentation != null && presentation.mode == FakeRoutePresentationMode.Video && presentation.videoClip != null && videoPlayer != null && videoRenderTexture != null && backgroundImage != null;
        _remaining = presentation != null && presentation.duration > 0f ? presentation.duration : hasVideo ? (float)presentation.videoClip.length : allowPlaceholder ? 0f : 0f;
        _videoPrepared = hasVideo;
        _holdLastFrame = presentation != null && presentation.holdLastFrameAsBackground;
        _fastForwarding = false;
        if (videoPlayer != null) videoPlayer.playbackSpeed = 1f;
        _skipAllowed = presentation == null || presentation.skipAllowed;
        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(Skip);
            skipButton.gameObject.SetActive(_skipAllowed);
        }
        _loop = presentation != null && presentation.loop;
        if (presentation != null && presentation.mode == FakeRoutePresentationMode.StaticImage)
        {
            StopMedia();
            ApplyTexture(presentation.staticImage != null ? presentation.staticImage.texture : null);
        }
        else if (_videoPrepared)
        {
            videoPlayer.clip = presentation.videoClip;
            videoPlayer.isLooping = _loop;
            videoPlayer.Prepare();
            while (!videoPlayer.isPrepared && canContinue() && !_skipRequested) yield return null;
            if (!_skipRequested && canContinue())
            {
                ApplyTexture(videoRenderTexture);
                ConfigureVideoAspect(presentation.videoClip);
                videoPlayer.Play();
                PlayPresentationAudio(presentation);
            }
        }
        while (!_skipRequested && (_loop || _remaining > 0f || _fastForwarding) && canContinue())
        {
            if (Time.timeScale > 0f)
            {
                if (_videoPrepared && !videoPlayer.isPlaying) videoPlayer.Play();
                _remaining -= Time.unscaledDeltaTime;
            }
            else
            {
                if (_videoPrepared && videoPlayer.isPlaying) videoPlayer.Pause();
                if (audioSource != null && audioSource.isPlaying) audioSource.Pause();
            }
            yield return null;
        }
        CompletePresentation();
    }

    private IEnumerator PlayChibiBlackout(FakeRoutePresentation presentation, string label, Func<bool> canContinue, Action onCovered)
    {
        _playing = true;
        _skipRequested = false;
        _choiceLabel = label;
        _remaining = presentation.blackoutInDuration + presentation.blackoutHoldDuration + presentation.blackoutOutDuration;
        _skipAllowed = presentation.skipAllowed;
        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(Skip);
            skipButton.gameObject.SetActive(_skipAllowed);
        }
        _loop = false;

        if (chibiOverlay == null || blackoutImage == null)
        {
            CompletePresentation();
            yield break;
        }

        blackoutImage.color = presentation.blackoutColor;
        if (chibiImage != null)
        {
            chibiImage.sprite = presentation.chibiImage;
            chibiImage.enabled = presentation.chibiImage != null;
        }
        if (chibiTitle != null) chibiTitle.text = presentation.title ?? string.Empty;
        if (chibiSubtitle != null) chibiSubtitle.text = presentation.subtitle ?? string.Empty;
        chibiOverlay.gameObject.SetActive(true);
        blackoutImage.rectTransform.anchoredPosition = _blackoutStartPosition;
        SetChibiContent(false);
        PlayPresentationAudio(presentation);

        yield return AnimateBlackout(_blackoutStartPosition, _blackoutCenterPosition, presentation.blackoutInDuration, canContinue);
        if (!_skipRequested && canContinue())
        {
            SetChibiContent(true);
            yield return WaitPresentation(presentation.blackoutHoldDuration, canContinue);
            SetChibiContent(false);
            onCovered?.Invoke();
            if (!_skipRequested) yield return AnimateBlackout(_blackoutCenterPosition, _blackoutEndPosition, presentation.blackoutOutDuration, canContinue);
        }
        CompletePresentation();
    }

    private IEnumerator AnimateBlackout(Vector2 from, Vector2 to, float duration, Func<bool> canContinue)
    {
        if (duration <= 0f)
        {
            blackoutImage.rectTransform.anchoredPosition = to;
            yield break;
        }
        float elapsed = 0f;
        while (elapsed < duration && !_skipRequested && canContinue())
        {
            if (Time.timeScale > 0f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                blackoutImage.rectTransform.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
            }
            yield return null;
        }
        blackoutImage.rectTransform.anchoredPosition = to;
    }

    private IEnumerator WaitPresentation(float duration, Func<bool> canContinue)
    {
        float elapsed = 0f;
        while (elapsed < duration && !_skipRequested && canContinue())
        {
            if (Time.timeScale > 0f) elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void SetChibiContent(bool visible)
    {
        if (chibiImage != null) chibiImage.gameObject.SetActive(visible && chibiImage.sprite != null);
        if (chibiTitle != null) chibiTitle.gameObject.SetActive(visible && !string.IsNullOrEmpty(chibiTitle.text));
        if (chibiSubtitle != null) chibiSubtitle.gameObject.SetActive(visible && !string.IsNullOrEmpty(chibiSubtitle.text));
    }

    private void PlayPresentationAudio(FakeRoutePresentation presentation)
    {
        if (audioSource == null || presentation == null || presentation.audioClip == null) return;
        audioSource.clip = presentation.audioClip;
        audioSource.loop = presentation.loop;
        audioSource.Play();
    }

    public IEnumerator Play(FakeRouteChoiceConfig choice, Func<bool> canContinue, Action onCovered)
    {
        yield return PlayPresentation(choice != null ? choice.presentation : null, choice != null ? choice.displayName : "未知路线", canContinue, true, onCovered);
    }

    public IEnumerator Play(FakeRouteChoiceConfig choice, Func<bool> canContinue)
    {
        yield return Play(choice, canContinue, null);
    }

    public void Skip()
    {
        if (!_playing || !_skipAllowed) return;
        if (_videoPrepared && !_loop)
        {
            _skipAllowed = false;
            _fastForwarding = true;
            if (videoPlayer != null) videoPlayer.playbackSpeed = 12f;
            if (skipButton != null) skipButton.gameObject.SetActive(false);
            return;
        }
        _skipRequested = true;
    }

    private void ConfigureVideoAspect(VideoClip clip)
    {
        if (backgroundImage == null || clip == null) return;
        if (_videoAspectFitter == null)
            _videoAspectFitter = backgroundImage.GetComponent<UnityEngine.UI.AspectRatioFitter>();
        if (_videoAspectFitter == null)
            _videoAspectFitter = backgroundImage.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
        _videoAspectFitter.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.EnvelopeParent;
        _videoAspectFitter.aspectRatio = (float)clip.width / clip.height;
    }

    private void ApplyTexture(Texture texture)
    {
        if (backgroundImage != null)
        {
            backgroundImage.texture = texture;
            backgroundImage.color = texture != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        }
        if (legacyBackgroundImage != null)
        {
            legacyBackgroundImage.enabled = texture != null && !_videoPrepared;
            legacyBackgroundImage.color = legacyBackgroundImage.enabled ? Color.white : new Color(1f, 1f, 1f, 0f);
        }
    }

    private void OnVideoFinished(VideoPlayer source)
    {
        if (_playing && !_loop)
        {
            _remaining = 0f;
            _fastForwarding = false;
        }
    }

    private void CompletePresentation()
    {
        bool skipped = _skipRequested;
        bool holdFrame = _videoPrepared && _holdLastFrame && !skipped;
        if (!holdFrame)
            StopMedia();
        SetChibiContent(false);
        if (chibiOverlay != null) chibiOverlay.gameObject.SetActive(false);
        _playing = false;
        _skipRequested = false;
        _remaining = 0f;
        if (skipButton != null)
            skipButton.gameObject.SetActive(false);
        _holdLastFrame = false;
        if (holdFrame)
        {
            videoPlayer.Pause();
            videoPlayer.playbackSpeed = 1f;
            _heldFrameAsBackground = true;
        }
        Debug.Log("[FakeRoute] presentation complete choice=" + _choiceLabel + " skipped=" + skipped);
    }

    private void StopMedia()
    {
        if (videoPlayer != null && videoPlayer.isPlaying) videoPlayer.Stop();
        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
    }

}
