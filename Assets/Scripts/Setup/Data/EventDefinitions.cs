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
/// Magnitudes increased to 10-100% range for dramatic, visible price ramps.
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

    // FIX-17: Magnitudes reduced for event persistence.
    // FIX-18: Positive events made stronger, negative events made weaker to counteract
    // multiplicative percentage asymmetry (a -30% drop needs +43% to recover).

    // Earnings Beat: +25-50%, All tiers (FIX-18: up from +20-45%)
    public static readonly MarketEventConfig EarningsBeat = new MarketEventConfig(
        eventType: MarketEventType.EarningsBeat,
        minPriceEffect: 0.25f,
        maxPriceEffect: 0.50f,
        duration: 4f,
        tierAvailability: AllTiers,
        rarity: 0.5f
    );

    // Earnings Miss: -15-30%, All tiers (FIX-18: down from -20-45%)
    public static readonly MarketEventConfig EarningsMiss = new MarketEventConfig(
        eventType: MarketEventType.EarningsMiss,
        minPriceEffect: -0.15f,
        maxPriceEffect: -0.30f,
        duration: 4f,
        tierAvailability: AllTiers,
        rarity: 0.5f
    );

    // Pump & Dump: +45-90% pump phase, Penny only (was +75-150%)
    public static readonly MarketEventConfig PumpAndDump = new MarketEventConfig(
        eventType: MarketEventType.PumpAndDump,
        minPriceEffect: 0.45f,
        maxPriceEffect: 0.90f,
        duration: 6f,
        tierAvailability: PennyOnly,
        rarity: 0.3f
    );

    // SEC Investigation: -20-40%, Penny and Low (FIX-18: down from -30-55%)
    public static readonly MarketEventConfig SECInvestigation = new MarketEventConfig(
        eventType: MarketEventType.SECInvestigation,
        minPriceEffect: -0.20f,
        maxPriceEffect: -0.40f,
        duration: 6f,
        tierAvailability: PennyAndLow,
        rarity: 0.3f
    );

    // Sector Rotation: ±18%, Mid and Blue (was ±30%)
    public static readonly MarketEventConfig SectorRotation = new MarketEventConfig(
        eventType: MarketEventType.SectorRotation,
        minPriceEffect: -0.18f,
        maxPriceEffect: 0.18f,
        duration: 5f,
        tierAvailability: MidAndBlue,
        rarity: 0.4f
    );

    // Merger Rumor: +30-60%, Mid and Blue (FIX-18: up from +25-55%)
    public static readonly MarketEventConfig MergerRumor = new MarketEventConfig(
        eventType: MarketEventType.MergerRumor,
        minPriceEffect: 0.30f,
        maxPriceEffect: 0.60f,
        duration: 5f,
        tierAvailability: MidAndBlue,
        rarity: 0.3f
    );

    // Market Crash: -20-40%, All tiers (FIX-18: down from -30-60%)
    public static readonly MarketEventConfig MarketCrash = new MarketEventConfig(
        eventType: MarketEventType.MarketCrash,
        minPriceEffect: -0.20f,
        maxPriceEffect: -0.40f,
        duration: 6f,
        tierAvailability: AllTiers,
        rarity: 0.15f
    );

    // Bull Run: +35-65%, All tiers (FIX-18: up from +25-55%)
    public static readonly MarketEventConfig BullRunEvent = new MarketEventConfig(
        eventType: MarketEventType.BullRun,
        minPriceEffect: 0.35f,
        maxPriceEffect: 0.65f,
        duration: 6f,
        tierAvailability: AllTiers,
        rarity: 0.15f
    );

    // Flash Crash: -15-30%, Low and Mid (FIX-18: down from -25-45%)
    public static readonly MarketEventConfig FlashCrash = new MarketEventConfig(
        eventType: MarketEventType.FlashCrash,
        minPriceEffect: -0.15f,
        maxPriceEffect: -0.30f,
        duration: 3f,
        tierAvailability: LowAndMid,
        rarity: 0.25f
    );

    // Short Squeeze: +45-100%, All tiers (FIX-18: up from +35-90%)
    public static readonly MarketEventConfig ShortSqueeze = new MarketEventConfig(
        eventType: MarketEventType.ShortSqueeze,
        minPriceEffect: 0.45f,
        maxPriceEffect: 1.00f,
        duration: 3f,
        tierAvailability: AllTiers,
        rarity: 0.25f
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

    private static readonly Dictionary<StockTier, List<MarketEventConfig>> _tierEventsCache =
        new Dictionary<StockTier, List<MarketEventConfig>>();

    /// <summary>
    /// Returns all event configs available for the given tier.
    /// Filters by TierAvailability field on each config. Results are cached.
    /// </summary>
    public static List<MarketEventConfig> GetEventsForTier(StockTier tier)
    {
        if (_tierEventsCache.TryGetValue(tier, out var cached))
            return cached;

        var result = new List<MarketEventConfig>();
        foreach (var kvp in _configs)
        {
            var config = kvp.Value;
            for (int i = 0; i < config.TierAvailability.Length; i++)
            {
                if (config.TierAvailability[i] == tier)
                {
                    result.Add(config);
                    break;
                }
            }
        }
        _tierEventsCache[tier] = result;
        return result;
    }
}

/// <summary>
/// Configuration for the event scheduler timing and counts.
/// Controls how many events fire per round based on act progression.
/// </summary>
public static class EventSchedulerConfig
{
    public static readonly int MinEventsEarlyRounds = 5;
    public static readonly int MaxEventsEarlyRounds = 7;
    public static readonly int MinEventsLateRounds = 7;
    public static readonly int MaxEventsLateRounds = 10;
    public static readonly float EarlyBufferSeconds = 2f;
    public static readonly float LateBufferSeconds = 2f;
    public static readonly int MaxRareEventsPerRound = 2;
}
