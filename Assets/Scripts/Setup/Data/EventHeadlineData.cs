/// <summary>
/// Static data class containing event headline templates for market events.
/// Templates use {ticker} placeholder for stock name substitution.
/// Each MarketEventType has at least one headline template.
/// Follows the same pattern as NewsHeadlineData with System.Random for deterministic testing.
/// </summary>
public static class EventHeadlineData
{
    public static readonly string[] EarningsBeatHeadlines = new[]
    {
        "Breaking: {ticker} smashes earnings expectations!",
        "{ticker} reports record quarterly profits",
        "Analysts upgrade {ticker} after strong earnings",
        "{ticker} beats estimates — stock surges",
        "Wall Street cheers as {ticker} delivers blowout quarter",
    };

    public static readonly string[] EarningsMissHeadlines = new[]
    {
        "{ticker} misses earnings — shares tumble",
        "Disappointing quarter for {ticker}",
        "{ticker} warns of revenue shortfall",
        "Analysts downgrade {ticker} after weak report",
        "{ticker} falls short of market expectations",
    };

    public static readonly string[] PumpAndDumpHeadlines = new[]
    {
        "Suspicious volume spike on {ticker}!",
        "{ticker} trading volume explodes — buyers pile in",
        "Social media frenzy sends {ticker} soaring",
    };

    public static readonly string[] SECInvestigationHeadlines = new[]
    {
        "SEC opens investigation into {ticker}",
        "Regulatory probe announced for {ticker}",
        "Compliance concerns weigh on {ticker}",
    };

    public static readonly string[] SectorRotationHeadlines = new[]
    {
        "Sector rotation underway — {ticker} sector leads",
        "Money flows out of {ticker} sector",
        "Institutional rebalancing hits {ticker}",
    };

    public static readonly string[] MergerRumorHeadlines = new[]
    {
        "Merger rumors swirl around {ticker}",
        "Acquisition target: {ticker} surges on deal talk",
        "Breaking: {ticker} in buyout discussions",
    };

    public static readonly string[] MarketCrashHeadlines = new[]
    {
        "Market-wide sell-off hits {ticker}",
    };

    public static readonly string[] BullRunHeadlines = new[]
    {
        "Broad rally lifts {ticker} and peers",
    };

    public static readonly string[] FlashCrashHeadlines = new[]
    {
        "Flash crash rattles {ticker} trading",
    };

    public static readonly string[] ShortSqueezeHeadlines = new[]
    {
        "Short squeeze sends {ticker} soaring",
    };

    private static readonly string[][] _headlinesByType;

    static EventHeadlineData()
    {
        // Index by enum ordinal for fast lookup
        int count = System.Enum.GetValues(typeof(MarketEventType)).Length;
        _headlinesByType = new string[count][];

        _headlinesByType[(int)MarketEventType.EarningsBeat] = EarningsBeatHeadlines;
        _headlinesByType[(int)MarketEventType.EarningsMiss] = EarningsMissHeadlines;
        _headlinesByType[(int)MarketEventType.PumpAndDump] = PumpAndDumpHeadlines;
        _headlinesByType[(int)MarketEventType.SECInvestigation] = SECInvestigationHeadlines;
        _headlinesByType[(int)MarketEventType.SectorRotation] = SectorRotationHeadlines;
        _headlinesByType[(int)MarketEventType.MergerRumor] = MergerRumorHeadlines;
        _headlinesByType[(int)MarketEventType.MarketCrash] = MarketCrashHeadlines;
        _headlinesByType[(int)MarketEventType.BullRun] = BullRunHeadlines;
        _headlinesByType[(int)MarketEventType.FlashCrash] = FlashCrashHeadlines;
        _headlinesByType[(int)MarketEventType.ShortSqueeze] = ShortSqueezeHeadlines;
    }

    /// <summary>
    /// Selects a random headline template for the given event type and substitutes the ticker symbol.
    /// Uses System.Random parameter for deterministic testing.
    /// </summary>
    public static string GetHeadline(MarketEventType eventType, string tickerSymbol, System.Random random)
    {
        var templates = _headlinesByType[(int)eventType];
        string template = templates[random.Next(templates.Length)];
        return template.Replace("{ticker}", tickerSymbol);
    }

    /// <summary>
    /// Returns whether the given event type is positive (price goes up).
    /// </summary>
    public static bool IsPositiveEvent(MarketEventType eventType)
    {
        switch (eventType)
        {
            case MarketEventType.EarningsBeat:
            case MarketEventType.MergerRumor:
            case MarketEventType.BullRun:
            case MarketEventType.ShortSqueeze:
                return true;
            default:
                return false;
        }
    }
}
