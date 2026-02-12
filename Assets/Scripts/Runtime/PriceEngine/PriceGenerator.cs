using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Price generation pipeline orchestrator.
/// Story 1.1: Trend layer. Story 1.2: Noise layer (choppy segment walk).
/// Story 1.3: Event spikes. Story 1.4: Mean reversion.
/// Story 1.5: Named stock pools per tier.
/// </summary>
public class PriceGenerator
{
    private readonly List<StockInstance> _activeStocks = new List<StockInstance>();
    private readonly System.Random _random = new System.Random();
    private EventEffects _eventEffects;

    public IReadOnlyList<StockInstance> ActiveStocks => _activeStocks;

    /// <summary>
    /// Sets the EventEffects processor for event spike integration.
    /// </summary>
    public void SetEventEffects(EventEffects eventEffects)
    {
        _eventEffects = eventEffects;
    }

    /// <summary>
    /// Updates a stock's price using segment-based choppy random walk + events/reversion.
    /// Pipeline: trend → segment noise → event OR reversion → clamp
    /// </summary>
    public void UpdatePrice(StockInstance stock, float deltaTime)
    {
        float previousPrice = stock.CurrentPrice;

        // Update trend line reference point (Story 1.4)
        stock.UpdateTrendLine(deltaTime);

        // Step 1: Trend layer (Story 1.1)
        stock.CurrentPrice += stock.TrendPerSecond * deltaTime;

        // Step 2: Segment-based choppy noise (Story 1.2)
        if (stock.SegmentTimeRemaining <= 0f)
        {
            // Pick new segment
            stock.SegmentDuration = RandomRange(0.3f, 1.5f);
            stock.SegmentTimeRemaining = stock.SegmentDuration;

            // Random slope: scaled by noise amplitude and current price
            float baseSlope = ((float)_random.NextDouble() * 2f - 1f) * stock.NoiseAmplitude * stock.CurrentPrice;

            // Trend bias: slight pull toward trend direction
            float trendBias = stock.TrendPerSecond * 0.5f;

            // Mean reversion bias: if price is above trend line, favor negative slopes.
            // Uses price deviation directly (not scaled by NoiseAmplitude) so the bias
            // is strong enough to reliably pull price back toward trend.
            float reversionBias = 0f;
            if (stock.TrendLinePrice > 0f)
            {
                float deviation = (stock.CurrentPrice - stock.TrendLinePrice) / stock.TrendLinePrice;
                reversionBias = -deviation * stock.CurrentPrice * stock.TierConfig.MeanReversionSpeed;
            }

            stock.SegmentSlope = baseSlope + trendBias + reversionBias;
        }

        stock.SegmentTimeRemaining -= deltaTime;
        stock.CurrentPrice += stock.SegmentSlope * deltaTime;

        // Step 3: Event spikes (Story 1.3)
        bool hasActiveEvent = false;
        if (_eventEffects != null)
        {
            var activeEvents = _eventEffects.GetActiveEventsForStock(stock.StockId);
            if (activeEvents.Count > 0)
            {
                hasActiveEvent = true;
                for (int i = 0; i < activeEvents.Count; i++)
                {
                    stock.CurrentPrice = _eventEffects.ApplyEventEffect(stock, activeEvents[i], deltaTime);
                }
            }
        }

        // Clamp to tier price range
        if (stock.CurrentPrice < stock.TierConfig.MinPrice)
            stock.CurrentPrice = stock.TierConfig.MinPrice;
        if (stock.CurrentPrice > stock.TierConfig.MaxPrice * 2f)
            stock.CurrentPrice = stock.TierConfig.MaxPrice * 2f;

        EventBus.Publish(new PriceUpdatedEvent
        {
            StockId = stock.StockId,
            NewPrice = stock.CurrentPrice,
            PreviousPrice = previousPrice,
            DeltaTime = deltaTime
        });
    }

    /// <summary>
    /// Creates stock instances for a new round by selecting from named stock pools.
    /// </summary>
    public void InitializeRound(int act, int round)
    {
        _activeStocks.Clear();

        int stockId = 0;

        // Only create stocks for this act's tier (not all tiers)
        StockTier tier = RunContext.GetTierForAct(act);
        {
            var selections = SelectStocksForRound(tier);
            var config = StockTierData.GetTierConfig(tier);

            foreach (var def in selections)
            {
                float startingPrice = RandomRange(config.MinPrice, config.MaxPrice);
                TrendDirection direction = PickRandomTrendDirection();
                float trendStrength = RandomRange(config.MinTrendStrength, config.MaxTrendStrength);

                var stock = new StockInstance();
                stock.Initialize(stockId, def.TickerSymbol, tier, startingPrice, direction, trendStrength);
                _activeStocks.Add(stock);

                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[PriceEngine] Stock initialized: {def.TickerSymbol} \"{def.DisplayName}\" ({tier}) @ ${startingPrice:F2}, Trend: {direction}, Strength: {trendStrength:F4}/s");
                #endif

                stockId++;
            }
        }

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[PriceEngine] Round {act}-{round} initialized with {_activeStocks.Count} stocks");
        #endif
    }

    /// <summary>
    /// Selects a random subset of stocks from a tier's pool for a round.
    /// </summary>
    public List<StockDefinition> SelectStocksForRound(StockTier tier)
    {
        var pool = StockPoolData.GetPool(tier);
        var config = StockTierData.GetTierConfig(tier);
        int count = _random.Next(config.MinStocksPerRound, config.MaxStocksPerRound + 1);

        // Clamp to pool size
        if (count > pool.Length)
            count = pool.Length;

        // Fisher-Yates partial shuffle to pick 'count' unique stocks
        var indices = new List<int>(pool.Length);
        for (int i = 0; i < pool.Length; i++)
            indices.Add(i);

        var selected = new List<StockDefinition>(count);
        for (int i = 0; i < count; i++)
        {
            int pick = _random.Next(i, indices.Count);
            // Swap
            int temp = indices[i];
            indices[i] = indices[pick];
            indices[pick] = temp;

            selected.Add(pool[indices[i]]);
        }

        return selected;
    }

    private TrendDirection PickRandomTrendDirection()
    {
        // Roughly equal distribution: 40% bull, 40% bear, 20% neutral
        int roll = _random.Next(100);
        if (roll < 40) return TrendDirection.Bull;
        if (roll < 80) return TrendDirection.Bear;
        return TrendDirection.Neutral;
    }

    /// <summary>
    /// Returns debug info for all active stocks. Used by DebugOverlayUI.
    /// </summary>
    public List<StockDebugInfo> GetDebugInfo()
    {
        var infos = new List<StockDebugInfo>(_activeStocks.Count);
        for (int i = 0; i < _activeStocks.Count; i++)
        {
            var stock = _activeStocks[i];
            var info = new StockDebugInfo
            {
                Ticker = stock.TickerSymbol,
                CurrentPrice = stock.CurrentPrice,
                TrendLinePrice = stock.TrendLinePrice,
                TrendDirection = stock.TrendDirection,
                TrendPerSecond = stock.TrendPerSecond,
                NoiseAmplitude = stock.NoiseAmplitude,
                SegmentSlope = stock.SegmentSlope,
                SegmentTimeRemaining = stock.SegmentTimeRemaining,
                ReversionSpeed = stock.TierConfig.MeanReversionSpeed,
            };

            if (_eventEffects != null)
            {
                var events = _eventEffects.GetActiveEventsForStock(stock.StockId);
                if (events.Count > 0)
                {
                    var evt = events[0];
                    info.HasActiveEvent = true;
                    info.ActiveEventType = evt.EventType;
                    info.EventTimeRemaining = evt.Duration - evt.ElapsedTime;
                }
            }

            infos.Add(info);
        }
        return infos;
    }

    private float RandomRange(float min, float max)
    {
        return min + (float)_random.NextDouble() * (max - min);
    }
}

/// <summary>
/// Debug data snapshot for a single stock. Read-only, used by debug overlay.
/// </summary>
public struct StockDebugInfo
{
    public string Ticker;
    public float CurrentPrice;
    public float TrendLinePrice;
    public TrendDirection TrendDirection;
    public float TrendPerSecond;
    public float NoiseAmplitude;
    public float SegmentSlope;
    public float SegmentTimeRemaining;
    public float ReversionSpeed;
    public bool HasActiveEvent;
    public MarketEventType ActiveEventType;
    public float EventTimeRemaining;
}
