using System;
using System.Collections.Generic;
using UnityEngine;

public static class QTEActivityHub
{
    private static readonly HashSet<QTEController> ActiveControllers = new HashSet<QTEController>();

    public static event Action<bool> OnActivityChanged;

    public static bool IsActive
    {
        get
        {
            RemoveDestroyedControllers();
            return ActiveControllers.Count > 0;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        ActiveControllers.Clear();
    }

    public static void Subscribe(Action<bool> handler)
    {
        if (handler == null) return;
        OnActivityChanged -= handler;
        OnActivityChanged += handler;
        Debug.Log($"[QTE_FLIP_DIAG] Hub Subscribe target={handler.Target} subscribers={OnActivityChanged?.GetInvocationList().Length ?? 0} active={IsActive}");
    }

    public static void Unsubscribe(Action<bool> handler)
    {
        if (handler != null)
            OnActivityChanged -= handler;
    }

    public static void Begin(QTEController controller)
    {
        if (controller == null) return;

        RemoveDestroyedControllers(true);
        bool wasActive = ActiveControllers.Count > 0;
        bool added = ActiveControllers.Add(controller);
        Debug.Log($"[QTE_FLIP_DIAG] Hub Begin controller={controller.name}#{controller.GetInstanceID()} added={added} count={ActiveControllers.Count} subscribers={OnActivityChanged?.GetInvocationList().Length ?? 0} wasActive={wasActive}");
        if (!added) return;

        if (!wasActive)
        {
            Debug.Log("[QTE_FLIP_DIAG] Hub Broadcast active=true");
            OnActivityChanged?.Invoke(true);
        }
    }

    public static void End(QTEController controller)
    {
        if (ReferenceEquals(controller, null)) return;

        bool wasActive = ActiveControllers.Count > 0;
        bool removed = ActiveControllers.Remove(controller);
        RemoveDestroyedControllers();
        Debug.Log($"[QTE_FLIP_DIAG] Hub End controller={controller.name}#{controller.GetInstanceID()} removed={removed} count={ActiveControllers.Count} subscribers={OnActivityChanged?.GetInvocationList().Length ?? 0} wasActive={wasActive}");

        if (removed && wasActive && ActiveControllers.Count == 0)
        {
            Debug.Log("[QTE_FLIP_DIAG] Hub Broadcast active=false");
            OnActivityChanged?.Invoke(false);
        }
    }

    private static void RemoveDestroyedControllers(bool notify = false)
    {
        if (ActiveControllers.Count == 0) return;

        bool wasActive = ActiveControllers.Count > 0;
        ActiveControllers.RemoveWhere(controller => controller == null);
        if (notify && wasActive && ActiveControllers.Count == 0)
            OnActivityChanged?.Invoke(false);
    }
}
