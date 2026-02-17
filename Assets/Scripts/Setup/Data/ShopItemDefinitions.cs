/// <summary>
/// Relic definition — no rarity, no category. Cost alone determines value.
/// Used by the relics panel (Story 13.3+). Replaces ShopItemDef for relic selection.
/// </summary>
public struct RelicDef
{
    public string Id;
    public string Name;
    public string Description;
    public int Cost;

    public RelicDef(string id, string name, string description, int cost)
    {
        Id = id;
        Name = name;
        Description = description;
        Cost = cost;
    }
}

/// <summary>
/// All relic definitions.
/// NOTE: Class retains the "ShopItemDefinitions" name to avoid Unity .meta file disruption.
/// Conceptually this is "RelicDefinitions" — rename when next refactoring this file.
/// RelicPool: Placeholder relics for 13.3 infrastructure (uniform random, no rarity).
/// Story 13.9: All legacy types (ShopItemDef, ItemRarity, ItemCategory, RarityWeight)
/// and the 30-item AllItems pool have been removed. Relic items will be completely
/// redesigned in a future epic.
/// </summary>
public static class ShopItemDefinitions
{
    /// <summary>
    /// Placeholder relic pool for development/testing (Story 13.3 AC 17).
    /// 8 test relics with varying costs. Will be replaced entirely in a future item design epic.
    /// </summary>
    public static readonly RelicDef[] RelicPool = new RelicDef[]
    {
        new RelicDef("relic_stop_loss", "Stop-Loss Order",
            "Auto-sell a stock if it drops below a set threshold", 100),
        new RelicDef("relic_speed_trader", "Speed Trader",
            "Reduce trade execution delay by 50%", 150),
        new RelicDef("relic_insider_tip", "Insider Tip",
            "Preview one market event next round", 200),
        new RelicDef("relic_portfolio_hedge", "Portfolio Hedge",
            "Reduce losses on all positions by 25% for one round", 250),
        new RelicDef("relic_compound_interest", "Compound Interest",
            "All profits earn an additional 15% bonus at round end", 300),
        new RelicDef("relic_dark_pool", "Dark Pool Access",
            "Execute one trade per round at guaranteed mid-price", 350),
        new RelicDef("relic_golden_parachute", "Golden Parachute",
            "Survive one Margin Call (consumed on use)", 400),
        new RelicDef("relic_master_universe", "Master of the Universe",
            "All shop items cost 25% less for the rest of the run", 500),
    };

}
