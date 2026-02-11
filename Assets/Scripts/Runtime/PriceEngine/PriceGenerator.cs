using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Price generation pipeline orchestrator.
/// Story 1.1: Trend layer. Story 1.2: Noise layer. Story 1.3: Event spikes.
/// Later stories add mean reversion (1.4).
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

    // Ticker symbols pool for generating stock names
    private static readonly string[] TickerPool = new string[]
    {
        "ACME", "BULL", "BEAR", "MOON", "HODL",
        "YOLO", "PUMP", "DUMP", "APEX", "MEGA",
        "FLUX", "NEON", "VOID", "RUSH", "BOLT",
        "GRIT", "HYPE", "ZINC", "OMNI", "PEAK"
    };

    /// <summary>
    /// Updates a stock's price by applying trend + noise + event spike layers.
    /// Called every frame for each active stock.
    /// Pipeline: trend → noise → events → clamp
    /// </summary>
    public void UpdatePrice(StockInstance stock, float deltaTime)
    {
        float previousPrice = stock.CurrentPrice;

        // Step 1: Trend layer (Story 1.1)
        stock.CurrentPrice += stock.TrendPerSecond * deltaTime;

        // Step 2: Noise layer (Story 1.2) — smoothed random walk
        float noiseDrift = ((float)_random.NextDouble() * 2f - 1f) * stock.NoiseFrequency * deltaTime;
        stock.NoiseAccumulator += noiseDrift;
        // Mean-revert the accumulator to prevent drift from compounding
        stock.NoiseAccumulator *= 0.98f;
        float noiseEffect = stock.CurrentPrice * stock.NoiseAmplitude * stock.NoiseAccumulator * deltaTime;
        stock.CurrentPrice += noiseEffect;

        // Step 3: Event spike layer (Story 1.3)
        if (_eventEffects != null)
        {
            var activeEvents = _eventEffects.GetActiveEventsForStock(stock.StockId);
            for (int i = 0; i < activeEvents.Count; i++)
            {
                stock.CurrentPrice = _eventEffects.ApplyEventEffect(stock, activeEvents[i], deltaTime);
            }
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
    /// Creates stock instances for a new round with random trend directions
    /// and tier-appropriate strengths.
    /// </summary>
    public void InitializeRound(int act, int round)
    {
        _activeStocks.Clear();

        int stockId = 0;
        var usedTickers = new HashSet<string>();

        // Create stocks for each tier
        foreach (StockTier tier in System.Enum.GetValues(typeof(StockTier)))
        {
            var config = StockTierData.GetTierConfig(tier);
            int stockCount = _random.Next(config.MinStocksPerRound, config.MaxStocksPerRound + 1);

            for (int i = 0; i < stockCount; i++)
            {
                string ticker = PickUniqueTicker(usedTickers);
                float startingPrice = RandomRange(config.MinPrice, config.MaxPrice);
                TrendDirection direction = PickRandomTrendDirection();
                float trendStrength = RandomRange(config.MinTrendStrength, config.MaxTrendStrength);

                var stock = new StockInstance();
                stock.Initialize(stockId, ticker, tier, startingPrice, direction, trendStrength);
                _activeStocks.Add(stock);

                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[PriceEngine] Stock initialized: {ticker} ({tier}) @ ${startingPrice:F2}, Trend: {direction}, Strength: {trendStrength:F4}/s");
                #endif

                stockId++;
            }
        }

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[PriceEngine] Round {act}-{round} initialized with {_activeStocks.Count} stocks");
        #endif
    }

    private TrendDirection PickRandomTrendDirection()
    {
        // Roughly equal distribution: 40% bull, 40% bear, 20% neutral
        int roll = _random.Next(100);
        if (roll < 40) return TrendDirection.Bull;
        if (roll < 80) return TrendDirection.Bear;
        return TrendDirection.Neutral;
    }

    private string PickUniqueTicker(HashSet<string> used)
    {
        for (int attempt = 0; attempt < 100; attempt++)
        {
            string ticker = TickerPool[_random.Next(TickerPool.Length)];
            if (used.Add(ticker))
                return ticker;
        }
        // Fallback: generate a numeric ticker
        string fallback = $"STK{used.Count}";
        used.Add(fallback);
        return fallback;
    }

    private float RandomRange(float min, float max)
    {
        return min + (float)_random.NextDouble() * (max - min);
    }
}
