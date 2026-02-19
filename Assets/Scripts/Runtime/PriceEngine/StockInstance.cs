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
    // FIX-18: TrendRate is the percentage rate per second (e.g., 0.017 = 1.7%/s).
    // TrendPerSecond is now computed dynamically from current price for compound growth.
    public float TrendRate { get; private set; }
    public float TrendPerSecond => TrendRate * CurrentPrice;
    public StockTier Tier { get; private set; }
    public StockSector Sector { get; private set; }
    public StockTierConfig TierConfig { get; private set; }

    // Noise config from tier
    public float NoiseAmplitude { get; private set; }
    public float NoiseFrequency { get; private set; }

    // Segment-based random walk state (replaces accumulator + sine wave)
    public float SegmentSlope { get; set; }
    public float SegmentTimeRemaining { get; set; }
    public float SegmentDuration { get; set; }

    // FIX-17: Tracks seconds of actual (non-frozen) trading for noise ramp-up
    public float TimeIntoTrading { get; set; }

    // Trend line tracking for mean reversion (Story 1.4)
    // FIX-17: Setter made internal so EventEffects can shift trend line on event end
    public float TrendLinePrice { get; set; }

    // Event tracking state (Story 1.3)
    public MarketEvent ActiveEvent { get; private set; }
    public float EventTargetPrice { get; private set; }
    public float EventStartPrice { get; set; }

    public void Initialize(int stockId, string tickerSymbol, StockTier tier, float startingPrice, TrendDirection trendDirection, float trendStrength, StockSector sector = StockSector.None)
    {
        StockId = stockId;
        TickerSymbol = tickerSymbol;
        CurrentPrice = startingPrice;
        TrendDirection = trendDirection;
        Tier = tier;
        Sector = sector;
        TierConfig = StockTierData.GetTierConfig(tier);

        // Noise from tier config
        NoiseAmplitude = TierConfig.NoiseAmplitude;
        NoiseFrequency = TierConfig.NoiseFrequency;

        // Start with expired segment so first update picks a new one
        SegmentSlope = 0f;
        SegmentTimeRemaining = 0f;
        SegmentDuration = 0f;

        // FIX-17: Noise ramp-up tracking
        TimeIntoTrading = 0f;

        // Trend line starts at the same price as current
        TrendLinePrice = startingPrice;

        // Event state starts cleared
        ActiveEvent = null;
        EventTargetPrice = 0f;
        EventStartPrice = 0f;

        // FIX-18: Store percentage rate directly for compound growth.
        // TrendPerSecond is now computed as TrendRate * CurrentPrice each access.
        switch (trendDirection)
        {
            case TrendDirection.Bull:
                TrendRate = trendStrength;
                break;
            case TrendDirection.Bear:
                TrendRate = -trendStrength;
                break;
            case TrendDirection.Neutral:
            default:
                TrendRate = 0f;
                break;
        }
    }

    /// <summary>
    /// Sets the stock's sector from StockDefinition data.
    /// Called during round initialization after Initialize().
    /// </summary>
    public void SetSector(StockSector sector)
    {
        Sector = sector;
    }

    /// <summary>
    /// Advances the trend line price by the trend rate. Called each frame to track
    /// where the price "should be" based on trend alone (reversion target).
    /// </summary>
    /// FIX-18: Trend line compounds at TrendRate so it tracks the same growth curve
    /// as the price itself, keeping mean reversion anchored to compound growth.
    public void UpdateTrendLine(float deltaTime)
    {
        TrendLinePrice *= (1f + TrendRate * deltaTime);
    }

    /// <summary>
    /// Applies an event to this stock, setting the active event and target price.
    /// </summary>
    public void ApplyEvent(MarketEvent evt, float targetPrice)
    {
        ActiveEvent = evt;
        EventStartPrice = CurrentPrice;
        EventTargetPrice = targetPrice;
    }

    /// <summary>
    /// Clears the active event state.
    /// </summary>
    public void ClearEvent()
    {
        ActiveEvent = null;
        EventTargetPrice = 0f;
        EventStartPrice = 0f;
    }
}
