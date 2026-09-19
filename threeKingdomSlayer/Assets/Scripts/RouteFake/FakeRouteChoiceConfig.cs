using UnityEngine;

[CreateAssetMenu(fileName = "NewFakeRouteChoiceConfig", menuName = "一夫当关/假移动路线选项")]
public sealed class FakeRouteChoiceConfig : ScriptableObject
{
    public string choiceId;
    public string displayName;
    public FakeRouteNodeConfig targetNode;
    public FakeRouteTravelPresentation presentation;
}
