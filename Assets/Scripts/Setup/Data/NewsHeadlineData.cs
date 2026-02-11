/// <summary>
/// Static data class containing news headline templates for the Market Open phase.
/// Headlines provide loose directional hints, not precise predictions.
/// </summary>
public static class NewsHeadlineData
{
    public static readonly string[] BullishHeadlines = new[]
    {
        "Markets rally on optimism",
        "Green across the board today",
        "Investor confidence surges",
        "Strong earnings reports lift sentiment",
        "Bulls charge ahead as momentum builds",
    };

    public static readonly string[] BearishHeadlines = new[]
    {
        "Storm clouds gathering on Wall Street",
        "Analysts urge caution this session",
        "Sell-off fears grip the market",
        "Bears tighten their grip today",
        "Red flags ahead — tread carefully",
    };

    public static readonly string[] VolatileHeadlines = new[]
    {
        "Buckle up — it's going to be a wild ride",
        "Volume surging across sectors",
        "Expect the unexpected this round",
        "Wild swings anticipated today",
        "Traders brace for turbulence",
    };

    public static readonly string[] NeutralHeadlines = new[]
    {
        "Markets await direction",
        "Traders holding their breath",
        "Mixed signals from the floor",
        "Steady as she goes — for now",
        "A quiet open, but for how long?",
    };

    /// <summary>
    /// Selects a headline based on the dominant trend direction of stocks in the round.
    /// Uses a System.Random for deterministic selection in tests.
    /// </summary>
    public static string GetHeadline(TrendDirection dominantTrend, System.Random random)
    {
        string[] pool;
        switch (dominantTrend)
        {
            case TrendDirection.Bull:
                pool = BullishHeadlines;
                break;
            case TrendDirection.Bear:
                pool = BearishHeadlines;
                break;
            default:
                pool = NeutralHeadlines;
                break;
        }

        return pool[random.Next(pool.Length)];
    }

    /// <summary>
    /// Selects a headline based on the active stocks, with volatile detection.
    /// Volatile = both bull and bear stocks present in roughly equal numbers.
    /// </summary>
    public static string GetHeadline(System.Collections.Generic.IReadOnlyList<StockInstance> stocks, System.Random random)
    {
        int bull = 0, bear = 0;
        for (int i = 0; i < stocks.Count; i++)
        {
            if (stocks[i].TrendDirection == TrendDirection.Bull) bull++;
            else if (stocks[i].TrendDirection == TrendDirection.Bear) bear++;
        }

        // Volatile: both bulls and bears present in equal numbers
        if (bull > 0 && bear > 0 && bull == bear)
            return VolatileHeadlines[random.Next(VolatileHeadlines.Length)];

        if (bull > bear) return BullishHeadlines[random.Next(BullishHeadlines.Length)];
        if (bear > bull) return BearishHeadlines[random.Next(BearishHeadlines.Length)];
        return NeutralHeadlines[random.Next(NeutralHeadlines.Length)];
    }

    /// <summary>
    /// Determines the dominant trend from a set of stock instances.
    /// Counts bull vs bear vs neutral and returns the majority.
    /// </summary>
    public static TrendDirection GetDominantTrend(System.Collections.Generic.IReadOnlyList<StockInstance> stocks)
    {
        int bull = 0, bear = 0;
        for (int i = 0; i < stocks.Count; i++)
        {
            if (stocks[i].TrendDirection == TrendDirection.Bull) bull++;
            else if (stocks[i].TrendDirection == TrendDirection.Bear) bear++;
        }

        if (bull > bear) return TrendDirection.Bull;
        if (bear > bull) return TrendDirection.Bear;
        return TrendDirection.Neutral;
    }
}
