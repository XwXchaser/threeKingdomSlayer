using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class RouteStageSceneEntryV2 : MonoBehaviour
{
    public Transform routeStageRoot;
    public RouteCombatNodeEntryV2[] nodes = Array.Empty<RouteCombatNodeEntryV2>();
    public RouteConnectionSceneBindingV2[] connections = Array.Empty<RouteConnectionSceneBindingV2>();

    public bool TryGetNode(RouteNodeConfigV2 config, out RouteCombatNodeEntryV2 entry)
    {
        entry = null;
        if (config == null || nodes == null) return false;
        for (int i = 0; i < nodes.Length; i++)
        {
            var candidate = nodes[i];
            if (candidate != null && candidate.nodeConfig == config)
            {
                entry = candidate;
                return true;
            }
        }
        return false;
    }

    public bool TryGetConnection(RouteNodeConfigV2 source, RouteNodeConfigV2 target, out RouteConnectionSceneBindingV2 binding)
    {
        binding = null;
        if (source == null || target == null || connections == null) return false;
        for (int i = 0; i < connections.Length; i++)
        {
            var candidate = connections[i];
            if (candidate != null && candidate.sourceNode == source && candidate.targetNode == target)
            {
                binding = candidate;
                return true;
            }
        }
        return false;
    }
}

public sealed class RouteCombatNodeEntryV2 : MonoBehaviour
{
    public RouteNodeConfigV2 nodeConfig;
    public Transform headJunction;
    public Transform combatArea;
    public Transform tailJunction;
    public Transform[] headToCombatPath = Array.Empty<Transform>();
    public Transform[] combatToTailPath = Array.Empty<Transform>();
}

public sealed class RouteConnectionSceneBindingV2 : MonoBehaviour
{
    public RouteNodeConfigV2 sourceNode;
    public RouteNodeConfigV2 targetNode;
    public Transform sourceTail;
    public Transform targetHead;
    public Transform rotationPivot;
    public Transform[] travelPath = Array.Empty<Transform>();
}

public sealed class RouteStageTargetsV2 : MonoBehaviour
{
    public Transform initialHeadTarget;
    public Transform combatTarget;
    public Transform tailTarget;
}
