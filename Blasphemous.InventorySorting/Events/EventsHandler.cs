namespace Blasphemous.InventorySorting.Events;

internal class EventsHandler
{
    public delegate void SimpleEvent();
    internal event SimpleEvent OnUpdate;

    internal void Update()
    {
        OnUpdate?.Invoke();
    }
}
