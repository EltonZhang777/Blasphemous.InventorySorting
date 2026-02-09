using System;

namespace Blasphemous.InventorySorting.Events;

internal class EventsHandler
{
    internal event Action OnUpdate;
    internal event Action<bool> OnInventoryToggle;
    internal event Action OnFirstEnterMainMenu;

    internal void Update()
    {
        OnUpdate?.Invoke();
    }

    internal void InventoryToggle(bool active)
    {
        OnInventoryToggle?.Invoke(active);
    }

    internal void FirstEnterMainMenu()
    {
        OnFirstEnterMainMenu?.Invoke();
        OnFirstEnterMainMenu = null;
    }
}
