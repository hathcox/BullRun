using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Processes market event effects on stock prices.
/// Manages active events lifecycle and applies price impacts via Lerp interpolation.
/// </summary>
public class EventEffects
{
    private readonly List<MarketEvent> _activeEvents = new List<MarketEvent>();
    private readonly List<MarketEvent> _eventsToRemove = new List<MarketEvent>();

    public int ActiveEventCount => _activeEvents.Count;

    /// <summary>
    /// Starts a new market event. Publishes MarketEventFiredEvent via EventBus.
    /// </summary>
    public void StartEvent(MarketEvent evt)
    {
        _activeEvents.Add(evt);

        EventBus.Publish(new MarketEventFiredEvent
        {
            EventType = evt.EventType,
            AffectedStockIds = evt.IsGlobalEvent ? null : new[] { evt.TargetStockId.Value },
            PriceEffectPercent = evt.PriceEffectPercent
        });

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        string target = evt.IsGlobalEvent ? "ALL" : $"Stock {evt.TargetStockId}";
        Debug.Log($"[Events] Event fired: {evt.EventType} on {target} ({evt.PriceEffectPercent:+0.0%;-0.0%} over {evt.Duration}s)");
        #endif
    }

    /// <summary>
    /// Calculates event's price impact on a stock using Lerp interpolation.
    /// Returns the new price after applying the event effect.
    /// Formula: Lerp(currentPrice, eventTarget, eventForce * deltaTime)
    /// </summary>
    public float ApplyEventEffect(StockInstance stock, MarketEvent evt, float deltaTime)
    {
        float force = evt.GetCurrentForce();
        if (force <= 0f)
            return stock.CurrentPrice;

        float eventTarget = stock.CurrentPrice * (1f + evt.PriceEffectPercent);
        return Mathf.Lerp(stock.CurrentPrice, eventTarget, force * deltaTime);
    }

    /// <summary>
    /// Updates all active events: advances elapsed time, removes expired events.
    /// Call once per frame before applying effects.
    /// </summary>
    public void UpdateActiveEvents(float deltaTime)
    {
        _eventsToRemove.Clear();

        for (int i = 0; i < _activeEvents.Count; i++)
        {
            _activeEvents[i].ElapsedTime += deltaTime;

            if (!_activeEvents[i].IsActive)
            {
                _eventsToRemove.Add(_activeEvents[i]);
            }
        }

        for (int i = 0; i < _eventsToRemove.Count; i++)
        {
            var expired = _eventsToRemove[i];

            EventBus.Publish(new MarketEventEndedEvent
            {
                EventType = expired.EventType,
                AffectedStockIds = expired.IsGlobalEvent ? null : new[] { expired.TargetStockId.Value }
            });

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Events] Event ended: {expired.EventType}");
            #endif

            _activeEvents.Remove(expired);
        }
    }

    /// <summary>
    /// Returns all active events that affect a specific stock (targeted + global events).
    /// </summary>
    public List<MarketEvent> GetActiveEventsForStock(int stockId)
    {
        var result = new List<MarketEvent>();
        for (int i = 0; i < _activeEvents.Count; i++)
        {
            var evt = _activeEvents[i];
            if (evt.IsGlobalEvent || evt.TargetStockId == stockId)
            {
                result.Add(evt);
            }
        }
        return result;
    }
}
