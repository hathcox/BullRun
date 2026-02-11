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
            // Iterate a copy to allow subscribe/unsubscribe during dispatch
            for (int i = handlers.Count - 1; i >= 0; i--)
            {
                ((Action<T>)handlers[i]).Invoke(eventData);
            }
        }
    }

    public static void Clear()
    {
        _subscribers.Clear();
    }
}
