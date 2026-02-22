/// <summary>
/// Insider tip definition — mystery intel purchasable with Reputation during the shop phase.
/// Each tip reveals hidden information about the next round (Story 13.5).
/// </summary>
public struct InsiderTipDef
{
    public InsiderTipType Type;
    public string DescriptionTemplate;
    public int Cost;

    public InsiderTipDef(InsiderTipType type, string descriptionTemplate, int cost)
    {
        Type = type;
        DescriptionTemplate = descriptionTemplate;
        Cost = cost;
    }
}

/// <summary>
/// All insider tip definitions. Nine tip types with varying costs and reveal templates.
/// Costs sourced from GameConfig for single source of truth (Story 13.5, updated Story 18.1).
/// </summary>
public static class InsiderTipDefinitions
{
    public static readonly InsiderTipDef[] All = new InsiderTipDef[]
    {
        // Story 18.6, AC 4: Price/direction tips use generic text at shop time
        // (accurate values revealed on chart at round start)
        new InsiderTipDef(InsiderTipType.PriceForecast,
            "Price forecast \u2014 revealed on chart",
            GameConfig.TipCostPriceForecast),
        new InsiderTipDef(InsiderTipType.PriceFloor,
            "Price floor \u2014 revealed on chart",
            GameConfig.TipCostPriceFloor),
        new InsiderTipDef(InsiderTipType.PriceCeiling,
            "Price ceiling \u2014 revealed on chart",
            GameConfig.TipCostPriceCeiling),
        new InsiderTipDef(InsiderTipType.EventCount,
            "Expect ~{0} disruptions \u2014 live countdown active",
            GameConfig.TipCostEventCount),
        new InsiderTipDef(InsiderTipType.DipMarker,
            "Best buy window marked on chart",
            GameConfig.TipCostDipMarker),
        new InsiderTipDef(InsiderTipType.PeakMarker,
            "Peak sell window marked on chart",
            GameConfig.TipCostPeakMarker),
        // Story 18.6, AC 4: ClosingDirection uses generic text at shop time
        new InsiderTipDef(InsiderTipType.ClosingDirection,
            "Closing direction \u2014 revealed on chart",
            GameConfig.TipCostClosingDirection),
        new InsiderTipDef(InsiderTipType.EventTiming,
            "Event timing marked on chart",
            GameConfig.TipCostEventTiming),
        new InsiderTipDef(InsiderTipType.TrendReversal,
            "Trend reversal point marked on chart",
            GameConfig.TipCostTrendReversal),
    };

    /// <summary>
    /// Returns an InsiderTipDef by type, or null if not found.
    /// </summary>
    public static InsiderTipDef? GetByType(InsiderTipType type)
    {
        for (int i = 0; i < All.Length; i++)
        {
            if (All[i].Type == type)
                return All[i];
        }
        return null;
    }
}
