/// <summary>
/// Expansion definition — permanent one-time upgrade purchasable with Reputation.
/// Effects are NOT implemented here (Story 13.7). This is data model only.
/// </summary>
public struct ExpansionDef
{
    public string Id;
    public string Name;
    public string Description;
    public int Cost;

    public ExpansionDef(string id, string name, string description, int cost)
    {
        Id = id;
        Name = name;
        Description = description;
        Cost = cost;
    }
}

/// <summary>
/// All expansion definitions. Six permanent upgrades purchasable once per run.
/// Costs sourced from GameConfig for single source of truth.
/// </summary>
public static class ExpansionDefinitions
{
    // Expansion ID constants — use these instead of raw strings for compile-time safety
    public const string MultiStockTrading = "multi_stock_trading";
    public const string LeverageTrading = "leverage_trading";
    public const string ExpandedInventory = "expanded_inventory";
    public const string DualShort = "dual_short";
    public const string IntelExpansion = "intel_expansion";
    public const string ExtendedTrading = "extended_trading";

    public static readonly ExpansionDef[] All = new ExpansionDef[]
    {
        // FIX-15: Multi-Stock Trading expansion removed — single stock per round is permanent
        new ExpansionDef(LeverageTrading, "Leverage Trading",
            "Trade with 2x leverage (double gains/losses)",
            GameConfig.ExpansionCostLeverage),
        new ExpansionDef(ExpandedInventory, "Expanded Inventory",
            "+2 relic slots (5 \u2192 7 max)",
            GameConfig.ExpansionCostExpandedInventory),
        new ExpansionDef(DualShort, "Dual Short",
            "Open a second short position simultaneously",
            GameConfig.ExpansionCostDualShort),
        new ExpansionDef(IntelExpansion, "Intel Expansion",
            "+1 Insider Tip slot per shop visit (2 \u2192 3)",
            GameConfig.ExpansionCostIntelExpansion),
        new ExpansionDef(ExtendedTrading, "Extended Trading",
            "+15 seconds to round timer",
            GameConfig.ExpansionCostExtendedTrading),
    };

    /// <summary>
    /// Returns an ExpansionDef by ID, or null if not found.
    /// </summary>
    public static ExpansionDef? GetById(string id)
    {
        for (int i = 0; i < All.Length; i++)
        {
            if (All[i].Id == id)
                return All[i];
        }
        return null;
    }
}
