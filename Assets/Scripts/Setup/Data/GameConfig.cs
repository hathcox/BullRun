/// <summary>
/// Core game constants. Single source of truth for all balance and timing values.
/// </summary>
public static class GameConfig
{
    public static readonly float StartingCapital = 1000f;
    public static readonly float RoundDurationSeconds = 60f;

    // 0 = per-frame updates (no fixed interval, UpdatePrice called every frame)
    public static readonly float PriceUpdateRate = 0f;

    // Short selling: margin collateral as percentage of position value
    public static readonly float ShortMarginRequirement = 0.5f;

    // Market Open preview duration before trading begins
    public static readonly float MarketOpenDurationSeconds = 7f;

    // Run structure: 4 acts, 2 rounds per act, 8 rounds total
    public static readonly int TotalRounds = 8;
    public static readonly int RoundsPerAct = 2;
    public static readonly int TotalActs = 4;

    /// <summary>
    /// Act configuration: act number, tier, round range, display name.
    /// Indexed by act number (1-based, so index 0 is unused).
    /// </summary>
    public static readonly ActConfig[] Acts = new ActConfig[]
    {
        new ActConfig(0, StockTier.Penny, 0, 0, ""),          // Unused index 0
        new ActConfig(1, StockTier.Penny, 1, 2, "Penny Stocks"),
        new ActConfig(2, StockTier.LowValue, 3, 4, "Low-Value Stocks"),
        new ActConfig(3, StockTier.MidValue, 5, 6, "Mid-Value Stocks"),
        new ActConfig(4, StockTier.BlueChip, 7, 8, "Blue Chips"),
    };
}

/// <summary>
/// Immutable configuration for a single act within a run.
/// </summary>
public class ActConfig
{
    public readonly int ActNumber;
    public readonly StockTier Tier;
    public readonly int FirstRound;
    public readonly int LastRound;
    public readonly string DisplayName;

    public ActConfig(int actNumber, StockTier tier, int firstRound, int lastRound, string displayName)
    {
        ActNumber = actNumber;
        Tier = tier;
        FirstRound = firstRound;
        LastRound = lastRound;
        DisplayName = displayName;
    }
}
