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
    public static readonly ExpansionDef[] All = new ExpansionDef[]
    {
        new ExpansionDef("multi_stock_trading", "Multi-Stock Trading",
            "Trade 2 stocks simultaneously per round",
            GameConfig.ExpansionCostMultiStock),
        new ExpansionDef("leverage_trading", "Leverage Trading",
            "Trade with 2x leverage (double gains/losses)",
            GameConfig.ExpansionCostLeverage),
        new ExpansionDef("expanded_inventory", "Expanded Inventory",
            "+2 relic slots (5 \u2192 7 max)",
            GameConfig.ExpansionCostExpandedInventory),
        new ExpansionDef("dual_short", "Dual Short",
            "Short a second stock simultaneously",
            GameConfig.ExpansionCostDualShort),
        new ExpansionDef("intel_expansion", "Intel Expansion",
            "+1 Insider Tip slot per shop visit (2 \u2192 3)",
            GameConfig.ExpansionCostIntelExpansion),
        new ExpansionDef("extended_trading", "Extended Trading",
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
