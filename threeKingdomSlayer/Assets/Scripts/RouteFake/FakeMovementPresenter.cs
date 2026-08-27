using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public sealed class FakeMovementPresenter : MonoBehaviour
{
    [SerializeField] private bool showDebugPanel = true;
    [SerializeField] private string debugTitle = "假移动表现";
    [SerializeField] private Camera backgroundCamera;
    [SerializeField] private RawImage backgroundImage;
    [SerializeField] private Image legacyBackgroundImage;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RenderTexture videoRenderTexture;
    [SerializeField] private AudioSource audioSource;

    private bool _playing;
    private bool _skipRequested;
    private float _remaining;
    private string _choiceLabel;
    private bool _videoPrepared;
    private bool _skipAllowed;
    private bool _loop;

    public bool IsPlaying => _playing;

    private void Awake()
    {
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = videoRenderTexture;
            videoPlayer.loopPointReached += OnVideoFinished;
        }
    }

    private void OnDestroy()
    {
        if (videoPlayer != null) videoPlayer.loopPointReached -= OnVideoFinished;
    }

    public void SetBattleBackground(FakeRoutePresentation presentation)
    {
        if (_playing || presentation == null) return;
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

    private IEnumerator PlayPresentation(FakeRoutePresentation presentation, string label, Func<bool> canContinue, bool allowPlaceholder)
    {
        _playing = true;
        _skipRequested = false;
        _choiceLabel = label;
        bool hasVideo = presentation != null && presentation.mode == FakeRoutePresentationMode.Video && presentation.videoClip != null && videoPlayer != null && videoRenderTexture != null && backgroundImage != null;
        _remaining = presentation != null && presentation.duration > 0f ? presentation.duration : hasVideo ? (float)presentation.videoClip.length : allowPlaceholder ? 0f : 0f;
        _videoPrepared = hasVideo;
        _skipAllowed = presentation == null || presentation.skipAllowed;
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
                videoPlayer.Play();
                PlayPresentationAudio(presentation);
            }
        }
        while (!_skipRequested && (_loop || _remaining > 0f) && canContinue())
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

    private void PlayPresentationAudio(FakeRoutePresentation presentation)
    {
        if (audioSource == null || presentation == null || presentation.audioClip == null) return;
        audioSource.clip = presentation.audioClip;
        audioSource.loop = presentation.loop;
        audioSource.Play();
    }

    public IEnumerator Play(FakeRouteChoiceConfig choice, Func<bool> canContinue)
    {
        yield return PlayPresentation(choice != null ? choice.presentation : null, choice != null ? choice.displayName : "未知路线", canContinue, true);
    }

    public void Skip()
    {
        if (_playing && _skipAllowed) _skipRequested = true;
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
        if (_playing && !_loop) _remaining = 0f;
    }

    private void CompletePresentation()
    {
        bool skipped = _skipRequested;
        StopMedia();
        _playing = false;
        _skipRequested = false;
        _remaining = 0f;
        Debug.Log("[FakeRoute] presentation complete choice=" + _choiceLabel + " skipped=" + skipped);
    }

    private void StopMedia()
    {
        if (videoPlayer != null && videoPlayer.isPlaying) videoPlayer.Stop();
        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
    }

    private void OnGUI()
    {
        if (!showDebugPanel || !_playing) return;
        GUILayout.BeginArea(new Rect(20f, 320f, 420f, 130f), GUI.skin.box);
        GUILayout.Label(debugTitle + "\n" + _choiceLabel + "\n剩余 " + Mathf.Max(0f, _remaining).ToString("F1") + " 秒");
        if (GUILayout.Button("跳过表现", GUILayout.Height(38f))) Skip();
        GUILayout.EndArea();
    }
}
