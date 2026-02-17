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
/// All insider tip definitions. Eight tip types with varying costs and reveal templates.
/// Costs sourced from GameConfig for single source of truth (Story 13.5).
/// </summary>
public static class InsiderTipDefinitions
{
    public static readonly InsiderTipDef[] All = new InsiderTipDef[]
    {
        new InsiderTipDef(InsiderTipType.PriceForecast,
            "Average price this round will be ~${0}",
            GameConfig.TipCostPriceForecast),
        new InsiderTipDef(InsiderTipType.PriceFloor,
            "Price won't drop below ~${0}",
            GameConfig.TipCostPriceFloor),
        new InsiderTipDef(InsiderTipType.PriceCeiling,
            "Price won't exceed ~${0}",
            GameConfig.TipCostPriceCeiling),
        new InsiderTipDef(InsiderTipType.TrendDirection,
            "Market is trending {0}",
            GameConfig.TipCostTrendDirection),
        new InsiderTipDef(InsiderTipType.EventForecast,
            "Expect {0} events",
            GameConfig.TipCostEventForecast),
        new InsiderTipDef(InsiderTipType.EventCount,
            "There will be {0} events this round",
            GameConfig.TipCostEventCount),
        new InsiderTipDef(InsiderTipType.VolatilityWarning,
            "Expect {0} volatility",
            GameConfig.TipCostVolatilityWarning),
        new InsiderTipDef(InsiderTipType.OpeningPrice,
            "Stock opens at ~${0}",
            GameConfig.TipCostOpeningPrice),
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
