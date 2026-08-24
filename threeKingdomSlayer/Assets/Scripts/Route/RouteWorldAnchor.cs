using UnityEngine;

public enum RouteWorldAnchorType
{
    CombatHead,
    CombatTail,
    Junction
}

public sealed class RouteWorldAnchor : MonoBehaviour
{
    public RouteWorldAnchorType anchorType;
}
