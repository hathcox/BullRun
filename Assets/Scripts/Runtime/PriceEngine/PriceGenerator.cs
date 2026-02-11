using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Price generation pipeline orchestrator.
/// Story 1.1: Trend layer. Story 1.2: Noise layer. Story 1.3: Event spikes.
/// Story 1.4: Mean reversion. Story 1.5: Named stock pools per tier.
/// </summary>
public class PriceGenerator
{
    private readonly List<StockInstance> _activeStocks = new List<StockInstance>();
    private readonly System.Random _random = new System.Random();
    private EventEffects _eventEffects;

    public IReadOnlyList<StockInstance> ActiveStocks => _activeStocks;

    /// <summary>
    /// Sets the EventEffects processor for event spike integration.
    /// When set, active events will be applied during UpdatePrice.
    /// </summary>
    public void SetEventEffects(EventEffects eventEffects)
    {
        _eventEffects = eventEffects;
    }


    /// <summary>
    /// Updates a stock's price by applying trend + noise + event/reversion layers.
    /// Called every frame for each active stock.
    /// Pipeline: trend → noise → event OR reversion → clamp
    /// </summary>
    public void UpdatePrice(StockInstance stock, float deltaTime)
    {
        float previousPrice = stock.CurrentPrice;

        // Update trend line reference point (Story 1.4)
        stock.UpdateTrendLine(deltaTime);

        // Step 1: Trend layer (Story 1.1)
        stock.CurrentPrice += stock.TrendPerSecond * deltaTime;

        // Step 2: Noise layer (Story 1.2) — smoothed random walk
        float noiseDrift = ((float)_random.NextDouble() * 2f - 1f) * stock.NoiseFrequency * deltaTime;
        stock.NoiseAccumulator += noiseDrift;
        // Mean-revert the accumulator to prevent drift from compounding
        stock.NoiseAccumulator *= 0.98f;
        float noiseEffect = stock.CurrentPrice * stock.NoiseAmplitude * stock.NoiseAccumulator * deltaTime;
        stock.CurrentPrice += noiseEffect;

        // Step 3 & 4: Event spike OR Mean reversion (Story 1.3 & 1.4)
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

        // Mean reversion — only when no event is active (Story 1.4)
        if (!hasActiveEvent)
        {
            stock.CurrentPrice = Mathf.Lerp(stock.CurrentPrice, stock.TrendLinePrice, stock.TierConfig.MeanReversionSpeed * deltaTime);
        }

        // Clamp to tier price range — never go below minimum
        if (stock.CurrentPrice < stock.TierConfig.MinPrice)
            stock.CurrentPrice = stock.TierConfig.MinPrice;

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

        foreach (StockTier tier in System.Enum.GetValues(typeof(StockTier)))
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
    /// Count is determined by the tier's MinStocksPerRound/MaxStocksPerRound config.
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
    public float ReversionSpeed;
    public bool HasActiveEvent;
    public MarketEventType ActiveEventType;
    public float EventTimeRemaining;
}
