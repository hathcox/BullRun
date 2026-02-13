using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Processes market event effects on stock prices.
/// Manages active events lifecycle and applies price impacts via direct price targeting.
/// When an event fires, it captures start price and computes target price.
/// Force curve drives Lerp between start and target for fast, visible ramps.
/// </summary>
public class EventEffects
{
    private readonly List<MarketEvent> _activeEvents = new List<MarketEvent>();
    private readonly List<MarketEvent> _eventsToRemove = new List<MarketEvent>();

    // Track start/target prices per (event, stockId) pair for direct targeting
    private readonly Dictionary<(MarketEvent, int), float> _eventStartPrices = new Dictionary<(MarketEvent, int), float>();
    private readonly Dictionary<(MarketEvent, int), float> _eventTargetPrices = new Dictionary<(MarketEvent, int), float>();

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
    /// Applies event effect using direct price targeting.
    /// On first call for a stock+event pair, captures start price and computes target.
    /// Returns Lerp(startPrice, targetPrice, force) where force ramps 0→1 fast.
    /// </summary>
    public float ApplyEventEffect(StockInstance stock, MarketEvent evt, float deltaTime)
    {
        float force = evt.GetCurrentForce();
        if (force <= 0f)
            return stock.CurrentPrice;

        // Capture start/target prices on first application per stock
        var key = (evt, stock.StockId);
        if (!_eventStartPrices.ContainsKey(key))
        {
            _eventStartPrices[key] = stock.CurrentPrice;
            _eventTargetPrices[key] = stock.CurrentPrice * (1f + evt.PriceEffectPercent);
        }

        float startPrice = _eventStartPrices[key];
        float targetPrice = _eventTargetPrices[key];

        // Direct Lerp from start to target based on force curve
        return Mathf.Lerp(startPrice, targetPrice, force);
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

            // Clean up tracked prices for all stocks affected by this event
            var keysToRemove = new List<(MarketEvent, int)>();
            foreach (var key in _eventStartPrices.Keys)
            {
                if (key.Item1 == expired)
                    keysToRemove.Add(key);
            }
            for (int j = 0; j < keysToRemove.Count; j++)
            {
                _eventStartPrices.Remove(keysToRemove[j]);
                _eventTargetPrices.Remove(keysToRemove[j]);
            }

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
