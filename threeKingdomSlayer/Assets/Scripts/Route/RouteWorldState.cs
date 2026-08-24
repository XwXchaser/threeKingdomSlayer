using System;
using UnityEngine;

public sealed class RouteWorldState : MonoBehaviour
{
    public enum State
    {
        None,
        AtCombatNode,
        AtJunction,
        Traveling
    }

    [SerializeField] private Transform routeWorldRoot;
    [SerializeField] private RouteWorldNodeBinding startingNode;

    public State CurrentState { get; private set; }
    public RouteWorldNodeBinding CurrentNode { get; private set; }
    public event Action<State, RouteWorldNodeBinding> StateChanged;

    private void Awake()
    {
        CurrentState = State.None;
    }

    public void BeginAtStartingNode()
    {
        EnterNode(startingNode);
    }

    public void EnterNode(RouteWorldNodeBinding node)
    {
        if (node == null)
            return;
        CurrentNode = node;
        SetState(node.nodeType == RouteWorldNodeType.Junction ? State.AtJunction : State.AtCombatNode);
    }

    public void BeginTravel()
    {
        SetState(State.Traveling);
    }

    public void CompleteTravel(RouteWorldNodeBinding destination)
    {
        EnterNode(destination);
    }

    private void SetState(State state)
    {
        CurrentState = state;
        StateChanged?.Invoke(state, CurrentNode);
    }
}
