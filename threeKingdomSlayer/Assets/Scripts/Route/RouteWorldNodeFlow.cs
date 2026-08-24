using System;
using System.Collections;
using UnityEngine;

public sealed class RouteWorldNodeFlow : MonoBehaviour
{
    [SerializeField] private RouteWorldState worldState;
    [SerializeField] private RouteWorldMotionPrototype worldMotion;
    [SerializeField] private RouteWorldNodeBinding junctionNode;
    [SerializeField] private RouteWorldNodeBinding leftDestinationNode;
    [SerializeField] private float forwardDistance = 8f;
    [SerializeField] private float forwardSpeed = 4f;
    [SerializeField] private float turnDuration = 1f;

    public bool IsPlaying { get; private set; }
    public event Action Completed;

    public void PlayToJunctionThenLeft()
    {
        if (IsPlaying || worldState == null || worldMotion == null || junctionNode == null || leftDestinationNode == null)
            return;
        StartCoroutine(FlowRoutine());
    }

    private IEnumerator FlowRoutine()
    {
        IsPlaying = true;
        worldState.BeginTravel();
        bool moved = false;
        worldMotion.PlayForward(forwardDistance, () => moved = true);
        while (!moved) yield return null;
        worldState.CompleteTravel(junctionNode);

        bool turned = false;
        worldMotion.PlayLeftTurn(() => turned = true);
        while (!turned) yield return null;
        if (turned)
            worldState.CompleteTravel(leftDestinationNode);
        IsPlaying = false;
        Completed?.Invoke();
    }
}
