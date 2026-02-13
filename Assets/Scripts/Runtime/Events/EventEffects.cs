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
    /// Adds an event to the active list without publishing a headline.
    /// Used by SectorRotation to avoid flooding the news ticker with duplicate headlines.
    /// </summary>
    public void StartEventSilent(MarketEvent evt)
    {
        _activeEvents.Add(evt);
    }

    /// <summary>
    /// Starts a new market event. Publishes MarketEventFiredEvent via EventBus.
    /// Generates headline using EventHeadlineData and resolves ticker symbols.
    /// </summary>
    public void StartEvent(MarketEvent evt)
    {
        _activeEvents.Add(evt);

        // Resolve ticker symbols and generate headline
        string tickerForHeadline = "the market";
        string[] affectedTickers = null;

        if (!evt.IsGlobalEvent && _activeStocks != null)
        {
            for (int i = 0; i < _activeStocks.Count; i++)
            {
                if (_activeStocks[i].StockId == evt.TargetStockId.Value)
                {
                    tickerForHeadline = _activeStocks[i].TickerSymbol;
                    break;
                }
            }
            affectedTickers = new[] { tickerForHeadline };
        }

        string headline = EventHeadlineData.GetHeadline(evt.EventType, tickerForHeadline, _headlineRandom);

        EventBus.Publish(new MarketEventFiredEvent
        {
            EventType = evt.EventType,
            AffectedStockIds = evt.IsGlobalEvent ? null : new[] { evt.TargetStockId.Value },
            PriceEffectPercent = evt.PriceEffectPercent,
            Headline = headline,
            AffectedTickerSymbols = affectedTickers,
            IsPositive = EventHeadlineData.IsPositiveEvent(evt.EventType),
            Duration = evt.Duration
        });

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        string target = evt.IsGlobalEvent ? "ALL" : $"Stock {evt.TargetStockId} ({tickerForHeadline})";
        Debug.Log($"[Events] Event fired: {evt.EventType} on {target} ({evt.PriceEffectPercent:+0.0%;-0.0%} over {evt.Duration}s) — \"{headline}\"");
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

            // Resolve ticker symbols for ended event
            string[] endedTickers = null;
            if (!expired.IsGlobalEvent && _activeStocks != null)
            {
                for (int j = 0; j < _activeStocks.Count; j++)
                {
                    if (_activeStocks[j].StockId == expired.TargetStockId.Value)
                    {
                        endedTickers = new[] { _activeStocks[j].TickerSymbol };
                        break;
                    }
                }
            }

            EventBus.Publish(new MarketEventEndedEvent
            {
                EventType = expired.EventType,
                AffectedStockIds = expired.IsGlobalEvent ? null : new[] { expired.TargetStockId.Value },
                AffectedTickerSymbols = endedTickers
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
                _eventPhaseIndex.Remove(keysToRemove[j]);
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
