using UnityEngine;

[CreateAssetMenu(fileName = "NewFakeRouteTravelPresentation", menuName = "一夫当关/卷轴旅行表现")]
public sealed class FakeRouteTravelPresentation : ScriptableObject
{
    public float duration = 2f;
    public float travelDistance = 20f;
    public AnimationCurve speedCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public float curveStrength = 0.012f;
    public float backgroundOffset;
    public bool skipAllowed = true;
}
