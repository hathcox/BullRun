/// <summary>
/// Trend direction for a stock during a round.
/// </summary>
public enum TrendDirection
{
    Bull,
    Bear,
    Neutral
}

/// <summary>
/// Runtime state for a single stock during a round.
/// Each stock has independent trend, price, and tier configuration.
/// </summary>
public class StockInstance
{
    public int StockId { get; private set; }
    public string TickerSymbol { get; private set; }
    public float CurrentPrice { get; set; }
    public TrendDirection TrendDirection { get; private set; }
    public float TrendPerSecond { get; private set; }
    public StockTierConfig TierConfig { get; private set; }

    // Noise state — random walk accumulator for smooth noise
    public float NoiseAmplitude { get; private set; }
    public float NoiseFrequency { get; private set; }
    public float NoiseAccumulator { get; set; }

    // Trend line tracking for mean reversion (Story 1.4)
    public float TrendLinePrice { get; private set; }

    // Event tracking state (Story 1.3)
    public MarketEvent ActiveEvent { get; private set; }
    public float EventTargetPrice { get; private set; }

    public void Initialize(int stockId, string tickerSymbol, StockTier tier, float startingPrice, TrendDirection trendDirection, float trendStrength)
    {
        StockId = stockId;
        TickerSymbol = tickerSymbol;
        CurrentPrice = startingPrice;
        TrendDirection = trendDirection;
        TierConfig = StockTierData.GetTierConfig(tier);

        // Noise from tier config
        NoiseAmplitude = TierConfig.NoiseAmplitude;
        NoiseFrequency = TierConfig.NoiseFrequency;
        NoiseAccumulator = 0f;

        // Trend line starts at the same price as current
        TrendLinePrice = startingPrice;

        // Event state starts cleared
        ActiveEvent = null;
        EventTargetPrice = 0f;

        // Convert trend strength to per-second price change based on direction
        switch (trendDirection)
        {
            case TrendDirection.Bull:
                TrendPerSecond = startingPrice * trendStrength;
                break;
            case TrendDirection.Bear:
                TrendPerSecond = -(startingPrice * trendStrength);
                break;
            case TrendDirection.Neutral:
            default:
                TrendPerSecond = 0f;
                break;
        }
    }

    /// <summary>
    /// Advances the trend line price by the trend rate. Called each frame to track
    /// where the price "should be" based on trend alone (reversion target).
    /// </summary>
    public void UpdateTrendLine(float deltaTime)
    {
        TrendLinePrice += TrendPerSecond * deltaTime;
    }

    /// <summary>
    /// Applies an event to this stock, setting the active event and target price.
    /// </summary>
    public void ApplyEvent(MarketEvent evt, float targetPrice)
    {
        ActiveEvent = evt;
        EventTargetPrice = targetPrice;
    }

    /// <summary>
    /// Clears the active event state.
    /// </summary>
    public void ClearEvent()
    {
        ActiveEvent = null;
        EventTargetPrice = 0f;
    }
}
