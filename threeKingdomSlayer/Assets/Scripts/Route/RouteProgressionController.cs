using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed class RouteProgressionController : MonoBehaviour
{
    public static RouteProgressionController Instance { get; private set; }

    [SerializeField] private RouteStageConfig routeStageConfig;
    [SerializeField] private RouteWorldState worldState;
    [SerializeField] private RouteWorldMotion worldMotion;
    [SerializeField] private RouteWorldGraph worldGraph;
    [SerializeField] private StageController stageController;

    public RouteStageConfig RouteStageConfig => routeStageConfig;
    public RouteNodeDefinition CurrentNode { get; private set; }
    public bool IsRouteMode => routeStageConfig != null;
    public bool IsChoosing { get; private set; }
    public bool IsTraveling { get; private set; }

    private GameObject _choiceRoot;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Debug.Log("[RouteDiag] Awake controller=" + name + "#" + GetInstanceID() + " active=" + isActiveAndEnabled);
    }

    private void Start()
    {
        if (stageController == null) stageController = StageController.Instance;
        if (worldGraph == null) worldGraph = GetComponentInParent<RouteWorldGraph>();
        Debug.Log("[RouteDiag] Start controller=" + name + "#" + GetInstanceID() + " stage=" + (stageController != null ? stageController.name + "#" + stageController.GetInstanceID() : "NULL") + " config=" + (routeStageConfig != null ? routeStageConfig.name : "NULL"));
        SubscribeToStageController();
    }

    private void SubscribeToStageController()
    {
        if (stageController != null)
            stageController.OnCombatNodeCleared -= OnCombatNodeCleared;
        if (stageController != null)
            stageController.OnCombatNodeCleared += OnCombatNodeCleared;
        Debug.Log("[RouteDiag] Subscribed controller=" + name + " stage=" + (stageController != null ? stageController.name + "#" + stageController.GetInstanceID() : "NULL"));
    }

    private void OnDestroy()
    {
        if (stageController != null) stageController.OnCombatNodeCleared -= OnCombatNodeCleared;
        if (_choiceRoot != null) Destroy(_choiceRoot);
        if (Instance == this) Instance = null;
    }

    public bool TryInitialize(RouteStageConfig config, out string error)
    {
        routeStageConfig = config;
        Debug.Log("[RouteDiag] TryInitialize controller=" + name + "#" + GetInstanceID() + " config=" + (config != null ? config.name : "NULL") + " stage=" + (stageController != null ? stageController.name + "#" + stageController.GetInstanceID() : "NULL"));
        if (routeStageConfig == null) { error = "路线配置为空"; return false; }
        if (!routeStageConfig.TryValidate(out error)) return false;
        CurrentNode = routeStageConfig.startNode;
        IsChoosing = false;
        IsTraveling = false;
        SubscribeToStageController();
        Debug.Log("[RouteDiag] Initialized node=" + CurrentNode.name + " combat=" + CurrentNode.IsCombat + " stageListenerReady=" + (stageController != null));
        EnterNode(CurrentNode, true);
        return true;
    }

    public void EnterNode(RouteNodeDefinition node, bool resetPlayer)
    {
        if (node == null) return;
        CurrentNode = node;
        IsChoosing = false;
        IsTraveling = false;
        worldState?.CompleteTravel(FindBinding(node));
        if (node.IsCombat)
        {
            stageController?.SetCurrentNodeBattleConfig(node.battleConfig);
            if (node.battleConfig != null)
                stageController?.StartCurrentRouteNode(resetPlayer);
            else if (node.completionJunction != null)
                BeginTravelToNode(node.completionJunction);
            else
                ShowRouteChoice();
        }
        else
        {
            stageController?.SetCurrentNodeBattleConfig(null);
            stageController?.StopCombatForRouteTravel();
            ShowRouteChoice();
        }
    }
    public StageConfig GetCurrentBattleConfig()
    {
        return CurrentNode != null && CurrentNode.IsCombat ? CurrentNode.battleConfig : null;
    }

    public bool IsCurrentCombatNode => CurrentNode != null && CurrentNode.IsCombat;

    public bool TrySelect(RouteDirection direction, out RouteNodeDefinition destination)
    {
        destination = null;
        if (CurrentNode == null || CurrentNode.outgoingEdges == null) return false;
        foreach (var edge in CurrentNode.outgoingEdges)
        {
            if (edge != null && edge.direction == direction)
            {
                destination = edge.destination;
                return destination != null;
            }
        }
        return false;
    }

    private void OnCombatNodeCleared()
    {
        Debug.Log("[RouteDiag] OnCombatNodeCleared route=" + IsRouteMode + " choosing=" + IsChoosing + " traveling=" + IsTraveling + " node=" + (CurrentNode != null ? CurrentNode.name : "NULL") + " final=" + (routeStageConfig != null && routeStageConfig.finalNode != null ? routeStageConfig.finalNode.name : "NULL"));
        if (!IsRouteMode || IsChoosing || IsTraveling || CurrentNode == null) return;
        if (!CurrentNode.IsCombat || CurrentNode == routeStageConfig.finalNode) return;
        var completion = CurrentNode.completionJunction;
        Debug.Log("[RouteDiag] completion=" + (completion != null ? completion.name + "#" + completion.GetInstanceID() : "NULL"));
        if (completion != null)
        {
            BeginTravelToNode(completion);
            return;
        }
        ShowRouteChoice();
    }

    private void BeginTravelToNode(RouteNodeDefinition destination)
    {
        var direction = FindDirection(CurrentNode, destination);
        Debug.Log("[RouteDiag] BeginTravelToNode source=" + CurrentNode.name + " destination=" + destination.name + " direction=" + direction);
        BeginTravelInternal(destination, direction);
    }

    private RouteDirection FindDirection(RouteNodeDefinition source, RouteNodeDefinition destination)
    {
        foreach (var edge in source.outgoingEdges)
            if (edge != null && edge.destination == destination)
                return edge.direction;
        return RouteDirection.Forward;
    }
    private void ShowRouteChoice()
    {
        IsChoosing = true;
        Debug.Log("[RouteDiag] ShowRouteChoice node=" + CurrentNode.name + " edges=" + (CurrentNode.outgoingEdges != null ? CurrentNode.outgoingEdges.Count.ToString() : "NULL"));
        _choiceRoot = new GameObject("RouteChoiceRuntime", typeof(RectTransform));
        var canvas = FindObjectsOfType<Canvas>(true).FirstOrDefault(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay && c.name == "BattleHUD(Canvas)");
        if (canvas == null)
            canvas = FindObjectsOfType<Canvas>(true).FirstOrDefault(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay);
        Debug.Log("[RouteDiag] Canvas=" + (canvas != null ? canvas.name + "#" + canvas.GetInstanceID() : "NULL"));
        if (canvas == null)
        {
            IsChoosing = false;
            Destroy(_choiceRoot);
            _choiceRoot = null;
            return;
        }
        _choiceRoot.transform.SetParent(canvas.transform, false);
        var root = _choiceRoot.GetComponent<RectTransform>();
        root.anchorMin = new Vector2(0.15f, 0.35f);
        root.anchorMax = new Vector2(0.85f, 0.65f);
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        int index = 0;
        foreach (var edge in CurrentNode.outgoingEdges)
        {
            if (edge == null || edge.destination == null) continue;
            var buttonGo = new GameObject("RouteChoice_" + edge.direction, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(root, false);
            var buttonRect = buttonGo.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.1f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.9f, 0.5f);
            buttonRect.sizeDelta = new Vector2(0f, 72f);
            buttonRect.anchoredPosition = new Vector2(0f, index++ * -85f);
            buttonGo.GetComponent<Image>().color = new Color(0.12f, 0.25f, 0.18f, 0.95f);
            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(buttonGo.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textGo.AddComponent<Text>();
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.text = edge.direction + " → " + edge.destination.displayName;
            var destination = edge.destination;
            var direction = edge.direction;
            buttonGo.GetComponent<Button>().onClick.AddListener(() => BeginTravel(destination, direction));
        }
    }

    private void BeginTravel(RouteNodeDefinition destination, RouteDirection direction)
    {
        if (!IsChoosing || destination == null)
        {
            Debug.LogWarning("[RouteTravelDiag] BeginTravel rejected choosing=" + IsChoosing + " destination=" + (destination != null ? destination.name : "NULL"));
            return;
        }
        BeginTravelInternal(destination, direction);
    }

    private void BeginTravelInternal(RouteNodeDefinition destination, RouteDirection direction)
    {
        IsChoosing = false;
        if (_choiceRoot != null) Destroy(_choiceRoot);
        if (worldGraph == null)
        {
            var parent = transform.parent;
            worldGraph = parent != null ? parent.Find("RouteWorldGraph")?.GetComponent<RouteWorldGraph>() : null;
        }
        Debug.Log("[RouteTravelDiag] BeginTravel source=" + (CurrentNode != null ? CurrentNode.name : "NULL") + " destination=" + destination.name + " direction=" + direction + " graph=" + (worldGraph != null ? worldGraph.name : "NULL") + " motion=" + (worldMotion != null ? worldMotion.name : "NULL"));
        worldState?.BeginTravel();
        stageController?.StopCombatForRouteTravel();
        var channel = FindChannel(CurrentNode, destination, direction);
        if (channel == null)
        {
            Debug.LogWarning("[RouteTravelDiag] no channel; completing travel immediately");
            CompleteTravel(destination);
            return;
        }
        var sourceBinding = FindBinding(CurrentNode);
        var targetBinding = FindBinding(destination);
        var sourceAnchor = GetExitAnchor(sourceBinding);
        var targetAnchor = GetEntryAnchor(targetBinding);
        if (worldMotion == null || sourceAnchor == null || targetAnchor == null)
        {
            Debug.LogError("[RouteTravelDiag] travel anchors incomplete source=" + (sourceAnchor != null ? sourceAnchor.name : "NULL") + " target=" + (targetAnchor != null ? targetAnchor.name : "NULL"));
            CompleteTravel(destination);
            return;
        }
        Vector3 turnPivotLocal = channel.turnPivot != null
            ? worldGraph.routeWorldRoot.InverseTransformPoint(channel.turnPivot.position)
            : sourceAnchor.localPosition;
        worldMotion.PlayChannel(channel, sourceAnchor.localPosition, sourceAnchor.localRotation, targetAnchor.localPosition, targetAnchor.localRotation, turnPivotLocal, () => CompleteTravel(destination));
    }

    private Transform GetExitAnchor(RouteWorldNodeBinding binding)
    {
        if (binding == null) return null;
        return binding.nodeType == RouteWorldNodeType.Combat ? binding.tailJunction : binding.junctionAnchor;
    }

    private Transform GetEntryAnchor(RouteWorldNodeBinding binding)
    {
        if (binding == null) return null;
        return binding.nodeType == RouteWorldNodeType.Combat ? binding.headJunction : binding.junctionAnchor;
    }
    private RouteWorldChannelPoint FindChannel(RouteNodeDefinition source, RouteNodeDefinition destination, RouteDirection direction)
    {
        if (worldGraph == null || worldGraph.channels == null)
        {
            Debug.LogWarning("[RouteTravelDiag] FindChannel no graph source=" + source.name + " destination=" + destination.name);
            return null;
        }
        foreach (var channel in worldGraph.channels)
            if (channel != null && channel.sourceNode != null && channel.targetNode != null
                && channel.sourceNode.nodeId == source.nodeId && channel.targetNode.nodeId == destination.nodeId
                && channel.direction == direction)
                return channel;
        Debug.LogWarning("[RouteTravelDiag] FindChannel no match source=" + source.name + " destination=" + destination.name + " direction=" + direction + " channels=" + worldGraph.channels.Length);
        return null;
    }


    private void CompleteTravel(RouteNodeDefinition destination)
    {
        CurrentNode = destination;
        IsTraveling = false;
        EnterNode(destination, false);
    }

    private RouteWorldNodeBinding FindBinding(RouteNodeDefinition node)
    {
        if (worldGraph == null || worldGraph.nodes == null) return null;
        foreach (var binding in worldGraph.nodes)
            if (binding != null && binding.nodeId == node.nodeId) return binding;
        return null;
    }
}
