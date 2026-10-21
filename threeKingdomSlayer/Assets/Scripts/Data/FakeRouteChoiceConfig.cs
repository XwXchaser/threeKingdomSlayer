using UnityEngine;

[CreateAssetMenu(fileName = "NewFakeRouteChoiceConfig", menuName = "一夫当关/假移动路线选项")]
public sealed class FakeRouteChoiceConfig : ScriptableObject
{
    public string choiceId;
    public string displayName;
    public FakeRouteNodeConfig targetNode;
    public FakeRouteTravelPresentation presentation;
    [Tooltip("路线选择按钮在面板中的位置与旋转；未覆盖时按默认排布。")]
    public RouteChoiceLayout layout = new RouteChoiceLayout();
}
