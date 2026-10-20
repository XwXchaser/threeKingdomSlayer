using UnityEngine;

public enum RouteWorldNodeType
{
    Combat,
    Junction
}

public sealed class RouteWorldNodeBinding : MonoBehaviour
{
    public string nodeId;
    public RouteNodeDefinition nodeDefinition;
    public RouteWorldNodeType nodeType;
    public Transform headJunction;
    public Transform tailJunction;
    public Transform junctionAnchor;
    public Transform observationAnchor;
    public Transform routeChoiceAnchor;
    public bool activateEnvironmentOnEnter = true;
}
