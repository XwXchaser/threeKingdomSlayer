using UnityEngine;
using UnityEngine.Video;

public enum FakeRoutePresentationMode
{
    StaticImage,
    Video,
    ChibiBlackout
}

[CreateAssetMenu(fileName = "NewFakeRoutePresentation", menuName = "一夫当关/假移动表现")]
public sealed class FakeRoutePresentation : ScriptableObject
{
    public FakeRoutePresentationMode mode;
    public Sprite staticImage;
    public VideoClip videoClip;
    [Min(0f)] public float duration;
    public bool loop;
    public bool skipAllowed = true;
    public AudioClip audioClip;
    public Sprite chibiImage;
    public string title;
    public string subtitle;
    [Min(0f)] public float blackoutInDuration = 0.35f;
    [Min(0f)] public float blackoutHoldDuration = 0.8f;
    [Min(0f)] public float blackoutOutDuration = 0.35f;
    public Color blackoutColor = Color.black;
}
