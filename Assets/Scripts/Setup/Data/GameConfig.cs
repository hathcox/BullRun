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

    // Tier transition display duration (fade in + hold + fade out)
    public static readonly float TransitionDurationSeconds = 3f;

    /// <summary>
    /// Debug starting cash per round for F3 skip-to-round.
    /// Approximate expected cash at each round based on compounding.
    /// 0-indexed (index 0 = Round 1).
    /// </summary>
    public static readonly float[] DebugStartingCash = new float[]
    {
        1000f,   // Round 1
        1500f,   // Round 2
        2000f,   // Round 3
        3000f,   // Round 4
        4000f,   // Round 5
        6000f,   // Round 6
        8000f,   // Round 7
        12000f,  // Round 8
    };

    /// <summary>
    /// Act configuration: act number, tier, round range, display name, tagline.
    /// Indexed by act number (1-based, so index 0 is unused).
    /// </summary>
    public static readonly ActConfig[] Acts = new ActConfig[]
    {
        new ActConfig(0, StockTier.Penny, 0, 0, "", ""),
        new ActConfig(1, StockTier.Penny, 1, 2, "Penny Stocks",
            "The Penny Pit \u2014 Where Fortunes Begin"),
        new ActConfig(2, StockTier.LowValue, 3, 4, "Low-Value Stocks",
            "Rising Stakes \u2014 Trends and Reversals"),
        new ActConfig(3, StockTier.MidValue, 5, 6, "Mid-Value Stocks",
            "The Trading Floor \u2014 Sectors in Motion"),
        new ActConfig(4, StockTier.BlueChip, 7, 8, "Blue Chips",
            "Blue Chip Arena \u2014 The Big Leagues"),
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
    public readonly string Tagline;

    public ActConfig(int actNumber, StockTier tier, int firstRound, int lastRound,
        string displayName, string tagline)
    {
        ActNumber = actNumber;
        Tier = tier;
        FirstRound = firstRound;
        LastRound = lastRound;
        DisplayName = displayName;
        Tagline = tagline;
    }
}
