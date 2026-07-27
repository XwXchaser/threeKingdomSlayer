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
        OnActivityChanged = null;
    }

    public static void Begin(QTEController controller)
    {
        if (controller == null) return;

        RemoveDestroyedControllers(true);
        bool wasActive = ActiveControllers.Count > 0;
        if (!ActiveControllers.Add(controller)) return;

        if (!wasActive)
            OnActivityChanged?.Invoke(true);
    }

    public static void End(QTEController controller)
    {
        if (ReferenceEquals(controller, null)) return;

        bool wasActive = ActiveControllers.Count > 0;
        bool removed = ActiveControllers.Remove(controller);
        RemoveDestroyedControllers();

        if (removed && wasActive && ActiveControllers.Count == 0)
            OnActivityChanged?.Invoke(false);
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
