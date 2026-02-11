using System.Collections.Generic;

/// <summary>
/// Types of market events that can affect stock prices.
/// </summary>
public enum MarketEventType
{
    EarningsBeat,
    EarningsMiss,
    PumpAndDump,
    SECInvestigation,
    SectorRotation,
    MergerRumor,
    MarketCrash,
    BullRun,
    FlashCrash,
    ShortSqueeze
}

/// <summary>
/// Configuration for a market event type. Immutable after construction.
/// Defines price effect range, duration, tier availability, and rarity.
/// </summary>
public readonly struct MarketEventConfig
{
    public readonly MarketEventType EventType;
    public readonly float MinPriceEffect;
    public readonly float MaxPriceEffect;
    public readonly float Duration;
    public readonly StockTier[] TierAvailability;
    public readonly float Rarity;

    public MarketEventConfig(
        MarketEventType eventType,
        float minPriceEffect,
        float maxPriceEffect,
        float duration,
        StockTier[] tierAvailability,
        float rarity)
    {
        EventType = eventType;
        MinPriceEffect = minPriceEffect;
        MaxPriceEffect = maxPriceEffect;
        Duration = duration;
        TierAvailability = tierAvailability;
        Rarity = rarity;
    }
}

/// <summary>
/// Static data class containing all market event configurations.
/// Values sourced from GDD Section 3.4.
/// </summary>
public static class EventDefinitions
{
    private static readonly StockTier[] AllTiers = new[]
    {
        StockTier.Penny, StockTier.LowValue, StockTier.MidValue, StockTier.BlueChip
    };

    private static readonly StockTier[] PennyOnly = new[] { StockTier.Penny };

    private static readonly StockTier[] PennyAndLow = new[]
    {
        StockTier.Penny, StockTier.LowValue
    };

    private static readonly StockTier[] MidAndBlue = new[]
    {
        StockTier.MidValue, StockTier.BlueChip
    };

    private static readonly StockTier[] LowAndMid = new[]
    {
        StockTier.LowValue, StockTier.MidValue
    };

    // Earnings Beat: +15-30%, All tiers
    public static readonly MarketEventConfig EarningsBeat = new MarketEventConfig(
        eventType: MarketEventType.EarningsBeat,
        minPriceEffect: 0.15f,
        maxPriceEffect: 0.30f,
        duration: 5f,
        tierAvailability: AllTiers,
        rarity: 0.5f
    );

    // Earnings Miss: -15-30%, All tiers
    public static readonly MarketEventConfig EarningsMiss = new MarketEventConfig(
        eventType: MarketEventType.EarningsMiss,
        minPriceEffect: -0.15f,
        maxPriceEffect: -0.30f,
        duration: 5f,
        tierAvailability: AllTiers,
        rarity: 0.5f
    );

    // Pump & Dump: Rapid rise then crash, Penny only
    public static readonly MarketEventConfig PumpAndDump = new MarketEventConfig(
        eventType: MarketEventType.PumpAndDump,
        minPriceEffect: 0.30f,
        maxPriceEffect: 0.60f,
        duration: 8f,
        tierAvailability: PennyOnly,
        rarity: 0.3f
    );

    // SEC Investigation: -20-40% gradual, Penny and Low
    public static readonly MarketEventConfig SECInvestigation = new MarketEventConfig(
        eventType: MarketEventType.SECInvestigation,
        minPriceEffect: -0.20f,
        maxPriceEffect: -0.40f,
        duration: 10f,
        tierAvailability: PennyAndLow,
        rarity: 0.3f
    );

    // Sector Rotation: +/- mixed, Mid and Blue
    public static readonly MarketEventConfig SectorRotation = new MarketEventConfig(
        eventType: MarketEventType.SectorRotation,
        minPriceEffect: -0.10f,
        maxPriceEffect: 0.10f,
        duration: 8f,
        tierAvailability: MidAndBlue,
        rarity: 0.4f
    );

    // Merger Rumor: Surge on target, Mid and Blue
    public static readonly MarketEventConfig MergerRumor = new MarketEventConfig(
        eventType: MarketEventType.MergerRumor,
        minPriceEffect: 0.15f,
        maxPriceEffect: 0.35f,
        duration: 6f,
        tierAvailability: MidAndBlue,
        rarity: 0.3f
    );

    // Market Crash: Sharp drop all stocks, All tiers (rare)
    public static readonly MarketEventConfig MarketCrash = new MarketEventConfig(
        eventType: MarketEventType.MarketCrash,
        minPriceEffect: -0.20f,
        maxPriceEffect: -0.40f,
        duration: 8f,
        tierAvailability: AllTiers,
        rarity: 0.1f
    );

    // Bull Run: Steady rise all stocks, All tiers (rare)
    public static readonly MarketEventConfig BullRunEvent = new MarketEventConfig(
        eventType: MarketEventType.BullRun,
        minPriceEffect: 0.15f,
        maxPriceEffect: 0.30f,
        duration: 8f,
        tierAvailability: AllTiers,
        rarity: 0.1f
    );

    // Flash Crash: Drop then recover, Low and Mid
    public static readonly MarketEventConfig FlashCrash = new MarketEventConfig(
        eventType: MarketEventType.FlashCrash,
        minPriceEffect: -0.15f,
        maxPriceEffect: -0.30f,
        duration: 4f,
        tierAvailability: LowAndMid,
        rarity: 0.2f
    );

    // Short Squeeze: Violent spike, All tiers
    public static readonly MarketEventConfig ShortSqueeze = new MarketEventConfig(
        eventType: MarketEventType.ShortSqueeze,
        minPriceEffect: 0.20f,
        maxPriceEffect: 0.50f,
        duration: 4f,
        tierAvailability: AllTiers,
        rarity: 0.2f
    );

    private static readonly Dictionary<MarketEventType, MarketEventConfig> _configs =
        new Dictionary<MarketEventType, MarketEventConfig>
        {
            { MarketEventType.EarningsBeat, EarningsBeat },
            { MarketEventType.EarningsMiss, EarningsMiss },
            { MarketEventType.PumpAndDump, PumpAndDump },
            { MarketEventType.SECInvestigation, SECInvestigation },
            { MarketEventType.SectorRotation, SectorRotation },
            { MarketEventType.MergerRumor, MergerRumor },
            { MarketEventType.MarketCrash, MarketCrash },
            { MarketEventType.BullRun, BullRunEvent },
            { MarketEventType.FlashCrash, FlashCrash },
            { MarketEventType.ShortSqueeze, ShortSqueeze }
        };

    public static MarketEventConfig GetConfig(MarketEventType eventType)
    {
        return _configs[eventType];
    }
}
