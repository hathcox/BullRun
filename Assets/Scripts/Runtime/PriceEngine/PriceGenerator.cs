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
    /// Story 17.6: Static reference for relics to access price manipulation.
    /// Set by GameRunner during round initialization.
    /// </summary>
    private static PriceGenerator _activeInstance;
    public static void SetActiveInstance(PriceGenerator instance) { _activeInstance = instance; }

    /// <summary>
    /// Story 17.6: Applies a price multiplier to a stock by its string ID.
    /// Called by MarketManipulatorRelic after sell. Publishes PriceUpdatedEvent.
    /// </summary>
    public static void ApplyPriceMultiplier(string stockId, float multiplier)
    {
        if (_activeInstance == null) return;
        if (!int.TryParse(stockId, out int parsedId)) return;

        for (int i = 0; i < _activeInstance._activeStocks.Count; i++)
        {
            var stock = _activeInstance._activeStocks[i];
            if (stock.StockId == parsedId)
            {
                float previousPrice = stock.CurrentPrice;
                stock.CurrentPrice *= multiplier;

                // Story 17.6 review fix: Also adjust TrendLinePrice so mean reversion
                // doesn't immediately fight the price change back toward the old trend
                if (stock.TrendLinePrice > 0f)
                    stock.TrendLinePrice *= multiplier;

                EventBus.Publish(new PriceUpdatedEvent
                {
                    StockId = stock.StockId,
                    NewPrice = stock.CurrentPrice,
                    PreviousPrice = previousPrice,
                    DeltaTime = 0f
                });
                return;
            }
        }
    }

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

        // FIX-17: Track actual trading time for noise ramp-up
        stock.TimeIntoTrading += deltaTime;

        // Update trend line reference point (Story 1.4)
        stock.UpdateTrendLine(deltaTime);

        // Step 1: Trend layer (Story 1.1)
        // FIX-18: Compound growth — percentage-based trend scales with current price.
        // Bull stocks compound upward; recovery from crashes is proportional.
        stock.CurrentPrice *= (1f + stock.TrendRate * deltaTime);

        // Step 2: Segment-based choppy noise (Story 1.2)
        if (stock.SegmentTimeRemaining <= 0f)
        {
            // FIX-17: Increased min from 0.3s to 0.5s, max from 0.8s to 1.0s for smoother movement
            stock.SegmentDuration = RandomRange(0.5f, 1.0f);
            stock.SegmentTimeRemaining = stock.SegmentDuration;

            // FIX-17: Ramp noise amplitude from 0% to 100% over NoiseRampUpSeconds
            float noiseRamp = System.Math.Min(1f, stock.TimeIntoTrading / GameConfig.NoiseRampUpSeconds);

            // Random slope: scaled by noise amplitude (with ramp) and current price
            float baseSlope = ((float)_random.NextDouble() * 2f - 1f) * stock.NoiseAmplitude * noiseRamp * stock.CurrentPrice;

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

            // Minimum NET movement guarantee: check slope + trend combined,
            // not just slope alone. Prevents the common case where a large negative
            // slope nearly cancels the positive trend, producing near-zero net movement.
            float netMovement = stock.SegmentSlope + stock.TrendPerSecond;
            float minNet = stock.CurrentPrice * 0.005f;
            if (netMovement > -minNet && netMovement < minNet)
            {
                // Nudge slope so the net movement reaches the minimum threshold
                float targetNet = (netMovement >= 0f) ? minNet : -minNet;
                if (netMovement == 0f)
                    targetNet = ((float)_random.NextDouble() < 0.5f) ? minNet : -minNet;
                stock.SegmentSlope += targetNet - netMovement;
            }
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

        // Hard floor — stocks can never go to zero or negative.
        // Bounce slope must be strong enough to overcome negative trend,
        // otherwise the stock pins to the floor creating a permanent flatline.
        if (stock.CurrentPrice < 0.01f)
        {
            stock.CurrentPrice = 0.01f;
            // FIX-17: Reset trend line to floor price so mean reversion doesn't fight the bounce
            stock.TrendLinePrice = 0.01f;
            // Minimum bounce slope: overcome the trend + add strong visible upward movement.
            // Uses 2x the trend magnitude so the stock visibly rebounds off the floor
            // rather than hovering at $0.01 with imperceptible drift.
            float minBounceSlope = System.Math.Abs(stock.TrendPerSecond) * 2f + stock.CurrentPrice * 0.01f;
            if (stock.SegmentSlope < minBounceSlope)
                stock.SegmentSlope = minBounceSlope;
        }

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Diagnostic: detect truly flat price movement (delta < 0.001% of price)
        float priceDelta = System.Math.Abs(stock.CurrentPrice - previousPrice);
        float flatThreshold = stock.CurrentPrice * 0.00001f;
        if (priceDelta < flatThreshold)
        {
            Debug.LogWarning($"[PriceEngine] FLAT DETECTED {stock.TickerSymbol}: " +
                $"price=${stock.CurrentPrice:F4}, delta=${priceDelta:F6}, " +
                $"slope={stock.SegmentSlope:F6}, segTimeLeft={stock.SegmentTimeRemaining:F3}, " +
                $"trend/s={stock.TrendPerSecond:F4}, trendLine=${stock.TrendLinePrice:F4}, " +
                $"hasEvent={hasActiveEvent}, dt={deltaTime:F4}");
        }
        #endif

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
    /// FIX-15: Always spawns exactly the count specified by tier config (1 stock per round).
    /// For Mid-Value and Blue Chip tiers, stocks in the same sector share a trend direction.
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

            // For Mid/Blue tiers, pre-compute sector trend directions
            bool useSectorCorrelation = (tier == StockTier.MidValue || tier == StockTier.BlueChip);
            Dictionary<StockSector, TrendDirection> sectorTrends = null;

            if (useSectorCorrelation)
            {
                sectorTrends = new Dictionary<StockSector, TrendDirection>();
                foreach (var def in selections)
                {
                    if (def.Sector != StockSector.None && !sectorTrends.ContainsKey(def.Sector))
                    {
                        sectorTrends[def.Sector] = PickRandomTrendDirection();
                    }
                }
            }

            foreach (var def in selections)
            {
                float startingPrice = RandomRange(config.MinPrice, config.MaxPrice);
                TrendDirection direction;

                if (useSectorCorrelation && def.Sector != StockSector.None)
                {
                    direction = sectorTrends[def.Sector];
                }
                else
                {
                    direction = PickRandomTrendDirection();
                }

                float trendStrength = RandomRange(config.MinTrendStrength, config.MaxTrendStrength);

                var stock = new StockInstance();
                stock.Initialize(stockId, def.TickerSymbol, tier, startingPrice, direction, trendStrength, def.Sector);
                _activeStocks.Add(stock);

                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[PriceEngine] Stock initialized: {def.TickerSymbol} \"{def.DisplayName}\" ({tier}) @ ${startingPrice:F2}, Trend: {direction}, Strength: {trendStrength:F4}/s{(useSectorCorrelation ? $" [Sector: {def.Sector}]" : "")}");
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
    /// FIX-15: Count always driven by tier config (1 stock per round).
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
        // FIX-18: Bull-biased distribution: 60% bull, 25% bear, 15% neutral.
        // Game wants prices to generally trend up; bear stocks are the exception.
        int roll = _random.Next(100);
        if (roll < 60) return TrendDirection.Bull;
        if (roll < 85) return TrendDirection.Bear;
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
                TrendRate = stock.TrendRate,
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
    public float TrendRate;
    public float TrendPerSecond;
    public float NoiseAmplitude;
    public float SegmentSlope;
    public float SegmentTimeRemaining;
    public float ReversionSpeed;
    public bool HasActiveEvent;
    public MarketEventType ActiveEventType;
    public float EventTimeRemaining;
}
