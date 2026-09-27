using UnityEngine;

public enum ScrollRouteDirection
{
    Left,
    Forward,
    Right
}

[CreateAssetMenu(fileName = "NewScrollWorldRouteRules", menuName = "一夫当关/卷轴路线全局方向规则")]
public sealed class ScrollWorldRouteRules : ScriptableObject
{
    [Header("统一方向角（所有连接共享，不允许连接单独覆盖）")]
    public float leftAngle = -45f;
    public float forwardAngle = 0f;
    public float rightAngle = 45f;
    [Header("默认连接参数")]
    [Min(0.1f)] public float defaultTurnRadius = 12f;
    [Min(0.1f)] public float defaultRoadWidth = 5f;
    [Min(0f)] public float mergePositionTolerance = 0.25f;
    [Min(0f)] public float mergeAngleTolerance = 1f;

    public float GetAngle(ScrollRouteDirection direction)
    {
        switch (direction)
        {
            case ScrollRouteDirection.Left: return leftAngle;
            case ScrollRouteDirection.Right: return rightAngle;
            default: return forwardAngle;
        }
    }
}
