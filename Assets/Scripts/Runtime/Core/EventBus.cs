using System;
using System.Collections.Generic;

/// <summary>
/// Central typed event bus for inter-system communication.
/// Synchronous dispatch (same frame). Systems never reference each other directly.
/// </summary>
public static class EventBus
{
    private static readonly Dictionary<Type, List<Delegate>> _subscribers = new Dictionary<Type, List<Delegate>>();

    public static void Subscribe<T>(Action<T> handler)
    {
        var type = typeof(T);
        if (!_subscribers.TryGetValue(type, out var handlers))
        {
            handlers = new List<Delegate>();
            _subscribers[type] = handlers;
        }
        handlers.Add(handler);
    }

    public static void Unsubscribe<T>(Action<T> handler)
    {
        var type = typeof(T);
        if (_subscribers.TryGetValue(type, out var handlers))
        {
            handlers.Remove(handler);
        }
    }

    public static void Publish<T>(T eventData)
    {
        var type = typeof(T);
        if (_subscribers.TryGetValue(type, out var handlers))
        {
            // Iterate a copy to allow subscribe/unsubscribe during dispatch.
            // Catch subscriber exceptions so one bad handler doesn't block
            // the caller or other subscribers.
            for (int i = handlers.Count - 1; i >= 0; i--)
            {
                try
                {
                    ((Action<T>)handlers[i]).Invoke(eventData);
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogError($"[EventBus] Exception in {type.Name} subscriber: {ex}");
                }
            }
        }
    }

    public static void Clear()
    {
        _subscribers.Clear();
    }
}
