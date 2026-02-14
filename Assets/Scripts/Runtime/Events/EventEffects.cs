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
    // Track which phase index was last captured for multi-phase events
    private readonly Dictionary<(MarketEvent, int), int> _eventPhaseIndex = new Dictionary<(MarketEvent, int), int>();
    // Reusable buffers to avoid per-frame/per-event heap allocations
    private readonly List<MarketEvent> _stockEventsBuffer = new List<MarketEvent>();
    private readonly List<(MarketEvent, int)> _keysToRemoveBuffer = new List<(MarketEvent, int)>();

    private IReadOnlyList<StockInstance> _activeStocks;
    private System.Random _headlineRandom = new System.Random();

    public int ActiveEventCount => _activeEvents.Count;

    /// <summary>
    /// Sets the active stocks list for ticker symbol resolution in headlines.
    /// Call during round initialization.
    /// </summary>
    public void SetActiveStocks(IReadOnlyList<StockInstance> stocks)
    {
        _activeStocks = stocks;
    }

    /// <summary>
    /// Sets the Random instance for deterministic headline generation in tests.
    /// </summary>
    public void SetHeadlineRandom(System.Random random)
    {
        _headlineRandom = random;
    }

    /// <summary>
    /// Starts a new market event. Publishes MarketEventFiredEvent via EventBus.
    /// All events target a specific stock (FIX-9: no global events).
    /// </summary>
    public void StartEvent(MarketEvent evt)
    {
        if (!evt.TargetStockId.HasValue)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError($"[Events] StartEvent called with null TargetStockId for {evt.EventType} — skipping");
            #endif
            return;
        }

        _activeEvents.Add(evt);

        // Resolve ticker symbol for headline
        string tickerForHeadline = "the market";
        if (_activeStocks != null && evt.TargetStockId.HasValue)
        {
            for (int i = 0; i < _activeStocks.Count; i++)
            {
                if (_activeStocks[i].StockId == evt.TargetStockId.Value)
                {
                    tickerForHeadline = _activeStocks[i].TickerSymbol;
                    break;
                }
            }
        }

        string headline = EventHeadlineData.GetHeadline(evt.EventType, tickerForHeadline, _headlineRandom);

        EventBus.Publish(new MarketEventFiredEvent
        {
            EventType = evt.EventType,
            AffectedStockIds = new[] { evt.TargetStockId.Value },
            PriceEffectPercent = evt.PriceEffectPercent,
            Headline = headline,
            AffectedTickerSymbols = new[] { tickerForHeadline },
            IsPositive = EventHeadlineData.IsPositiveEvent(evt.EventType),
            Duration = evt.Duration
        });

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[Events] Event fired: {evt.EventType} on {tickerForHeadline} ({evt.PriceEffectPercent:+0.0%;-0.0%} over {evt.Duration}s) — \"{headline}\"");
        #endif
    }

    /// <summary>
    /// Applies event effect using direct price targeting.
    /// On first call for a stock+event pair, captures start price and computes target.
    /// Returns Lerp(startPrice, targetPrice, force) where force ramps 0→1 fast.
    /// </summary>
    public float ApplyEventEffect(StockInstance stock, MarketEvent evt, float deltaTime)
    {
        var key = (evt, stock.StockId);

        // Multi-phase: check if phase has changed and recapture prices BEFORE force check
        // so transition captures happen even if force is momentarily zero at boundary.
        if (evt.Phases != null && evt.Phases.Count > 0)
        {
            int currentPhase = evt.CurrentPhaseIndex;
            bool needsCapture = !_eventPhaseIndex.ContainsKey(key) || _eventPhaseIndex[key] != currentPhase;

            if (needsCapture)
            {
                // On phase transition, use previous phase's target as new start price
                // (not stock.CurrentPrice, which may have noise/trend applied)
                float newStart = _eventTargetPrices.ContainsKey(key)
                    ? _eventTargetPrices[key]
                    : stock.CurrentPrice;
                _eventStartPrices[key] = newStart;
                _eventTargetPrices[key] = newStart * (1f + evt.GetCurrentPhaseTarget());
                _eventPhaseIndex[key] = currentPhase;
            }
        }
        else
        {
            // Single-phase: capture start/target on first application
            if (!_eventStartPrices.ContainsKey(key))
            {
                _eventStartPrices[key] = stock.CurrentPrice;
                _eventTargetPrices[key] = stock.CurrentPrice * (1f + evt.PriceEffectPercent);
            }
        }

        float force = evt.GetCurrentForce();
        if (force <= 0f)
            return stock.CurrentPrice;

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

            if (!expired.TargetStockId.HasValue)
            {
                _activeEvents.Remove(expired);
                continue;
            }

            // Resolve ticker symbol for ended event (FIX-9: all events are stock-targeted)
            string endedTicker = "unknown";
            if (_activeStocks != null)
            {
                for (int j = 0; j < _activeStocks.Count; j++)
                {
                    if (_activeStocks[j].StockId == expired.TargetStockId.Value)
                    {
                        endedTicker = _activeStocks[j].TickerSymbol;
                        break;
                    }
                }
            }

            EventBus.Publish(new MarketEventEndedEvent
            {
                EventType = expired.EventType,
                AffectedStockIds = new[] { expired.TargetStockId.Value },
                AffectedTickerSymbols = new[] { endedTicker }
            });

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Events] Event ended: {expired.EventType}");
            #endif

            // Clean up tracked prices for all stocks affected by this event
            _keysToRemoveBuffer.Clear();
            foreach (var key in _eventStartPrices.Keys)
            {
                if (key.Item1 == expired)
                    _keysToRemoveBuffer.Add(key);
            }
            for (int j = 0; j < _keysToRemoveBuffer.Count; j++)
            {
                _eventStartPrices.Remove(_keysToRemoveBuffer[j]);
                _eventTargetPrices.Remove(_keysToRemoveBuffer[j]);
                _eventPhaseIndex.Remove(_keysToRemoveBuffer[j]);
            }

            _activeEvents.Remove(expired);
        }
    }

    /// <summary>
    /// Returns all active events that affect a specific stock.
    /// WARNING: Returns a shared internal buffer — do NOT cache the returned list across calls.
    /// </summary>
    public List<MarketEvent> GetActiveEventsForStock(int stockId)
    {
        _stockEventsBuffer.Clear();
        for (int i = 0; i < _activeEvents.Count; i++)
        {
            var evt = _activeEvents[i];
            if (evt.TargetStockId == stockId)
            {
                _stockEventsBuffer.Add(evt);
            }
        }
        return _stockEventsBuffer;
    }
}
