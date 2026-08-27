using UnityEngine;
using UnityEngine.Video;

public enum FakeRoutePresentationMode
{
    StaticImage,
    Video
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
}
