/// <summary>
/// Relic definition — no rarity, no category. Cost alone determines value.
/// Used by the relics panel (Story 13.3+). Replaces ShopItemDef for relic selection.
/// Story 17.2: Added EffectDescription for short mechanical tooltip text.
/// </summary>
public struct RelicDef
{
    public string Id;
    public string Name;
    public string Description;
    public string EffectDescription;
    public int Cost;
    public string IconChar;
    public string IconColorHex;

    public RelicDef(string id, string name, string description, string effectDescription, int cost,
        string iconChar, string iconColorHex)
    {
        Id = id;
        Name = name;
        Description = description;
        EffectDescription = effectDescription;
        Cost = cost;
        IconChar = iconChar;
        IconColorHex = iconColorHex;
    }
}

/// <summary>
/// All relic definitions.
/// NOTE: Class retains the "ShopItemDefinitions" name to avoid Unity .meta file disruption.
/// Conceptually this is "RelicDefinitions" — rename when next refactoring this file.
/// Story 13.9: All legacy types (ShopItemDef, ItemRarity, ItemCategory, RarityWeight)
/// and the 30-item AllItems pool have been removed.
/// Story 17.2: 23 balanced relics replacing 8 placeholders. Costs range 5-35 Rep.
/// </summary>
public static class ShopItemDefinitions
{
    /// <summary>
    /// 23 relic definitions with balanced costs (5-35 Rep). Uniform random selection.
    /// Stories 17.3-17.7 will replace StubRelic constructors with real effect classes.
    /// </summary>
    public static readonly RelicDef[] RelicPool = new RelicDef[]
    {
        new RelicDef("relic_event_trigger", "Catalyst Trader",
            "Buying a stock triggers a random market event. Buy cooldown +3s.",
            "+event on buy, +3s cooldown", 18, "!", "#FFB000"),
        new RelicDef("relic_short_multiplier", "Bear Raid",
            "Shorts execute 3 copies. You can no longer buy or sell.",
            "3x shorts, no longs", 14, "III", "#00FF41"),
        new RelicDef("relic_market_manipulator", "Market Manipulator",
            "Selling a stock causes its price to drop 15%.",
            "-15% price on sell", 12, "V", "#00FF41"),
        new RelicDef("relic_double_dealer", "Double Dealer",
            "You buy and sell 2 shares at a time.",
            "2x trade quantity", 20, "x2", "#00FF41"),
        new RelicDef("relic_quick_draw", "Quick Draw",
            "Buying is instant. Selling has 2x the normal cooldown.",
            "0s buy CD, 2x sell CD", 15, ">>", "#00FF41"),
        new RelicDef("relic_event_storm", "Event Storm",
            "Double the events per round. Events have 25% less impact.",
            "2x events, 0.75x impact", 20, "**", "#FFB000"),
        new RelicDef("relic_loss_liquidator", "Loss Liquidator",
            "Selling at a loss triggers a random event.",
            "+event on loss sell", 10, "-!", "#FFB000"),
        new RelicDef("relic_profit_refresh", "Profit Refresh",
            "Selling at profit refreshes your buy cooldown.",
            "reset buy CD on profit", 14, "+R", "#FFB000"),
        new RelicDef("relic_bull_believer", "Bull Believer",
            "Positive events 2x effectiveness. You can no longer short.",
            "2x good events, no short", 15, "^^", "#FFB000"),
        new RelicDef("relic_time_buyer", "Time Buyer",
            "Buying extends the round timer by 5 seconds.",
            "+5s timer on buy", 18, "+T", "#00FFFF"),
        new RelicDef("relic_diamond_hands", "Diamond Hands",
            "Stocks held to round end gain 30% value.",
            "+30% at liquidation", 25, "<>", "#00FFFF"),
        new RelicDef("relic_rep_doubler", "Rep Doubler",
            "Double Reputation earned from trades.",
            "2x trade rep", 28, "R2", "#FFD700"),
        new RelicDef("relic_fail_forward", "Fail Forward",
            "Reputation earned from failed trades too.",
            "rep on margin call", 8, "FF", "#FFD700"),
        new RelicDef("relic_bond_bonus", "Bond Bonus",
            "Gain 10 bonds. Lose 10 bonds on selling this relic.",
            "+10 bonds (lose on sell)", 32, "B+", "#FFD700"),
        new RelicDef("relic_free_intel", "Free Intel",
            "One Insider Tip is free every shop visit.",
            "1 free tip/visit", 10, "?F", "#00FFFF"),
        new RelicDef("relic_extra_expansion", "Extra Expansion",
            "One extra expansion offered per shop visit.",
            "+1 expansion offer", 14, "E+", "#00FFFF"),
        new RelicDef("relic_compound_rep", "Compound Rep",
            "Grants 3 rep when sold. Doubles each round held.",
            "3\u00d72^N rep on sell", 5, "$$", "#FFD700"),
        new RelicDef("relic_skimmer", "Skimmer",
            "Earn 3% of stock value when buying.",
            "+3% cash on buy", 12, "%B", "#00FF41"),
        new RelicDef("relic_short_profiteer", "Short Profiteer",
            "Earn 10% of stock value when shorting.",
            "+10% cash on short", 15, "%S", "#00FF41"),
        new RelicDef("relic_relic_expansion", "Relic Expansion",
            "Sell to permanently gain +1 relic slot. No Rep refund.",
            "+1 slot on sell (0 rep)", 35, "[+]", "#FF00FF"),
        new RelicDef("relic_event_catalyst", "Event Catalyst",
            "Rep earned = 1% chance per rep to trigger event.",
            "1%/rep \u2192 event", 14, "R!", "#FF00FF"),
        new RelicDef("relic_rep_interest", "Rep Interest",
            "Rep earns 10% interest every round start.",
            "+10% rep/round", 25, "R%", "#FFD700"),
        new RelicDef("relic_rep_dividend", "Rep Dividend",
            "Earn $1/round for every 2 rep you have.",
            "rep\u2192cash dividend", 20, "R$", "#FFD700"),
    };

}
