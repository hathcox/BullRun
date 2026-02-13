using System.Collections.Generic;

/// <summary>
/// All shop item definitions from GDD Section 4.
/// Static data class — no ScriptableObjects per project rules.
/// </summary>
public struct ShopItemDef
{
    public string Id;
    public string Name;
    public string Description;
    public int Cost;
    public ItemRarity Rarity;
    public ItemCategory Category;

    public ShopItemDef(string id, string name, string description, int cost, ItemRarity rarity, ItemCategory category)
    {
        Id = id;
        Name = name;
        Description = description;
        Cost = cost;
        Rarity = rarity;
        Category = category;
    }
}

public enum ItemRarity { Common, Uncommon, Rare, Legendary }
public enum ItemCategory { TradingTool, MarketIntel, PassivePerk }

public struct RarityWeight
{
    public ItemRarity Rarity;
    public float Weight;

    public RarityWeight(ItemRarity rarity, float weight)
    {
        Rarity = rarity;
        Weight = weight;
    }
}

/// <summary>
/// Complete item pool for the draft shop. 30 items total: 10 per category.
/// Rarity weights for selection probability:
///   Common ~50%, Uncommon ~30%, Rare ~15%, Legendary ~5%
/// </summary>
public static class ShopItemDefinitions
{
    public static readonly RarityWeight[] RarityWeights = new RarityWeight[]
    {
        new RarityWeight(ItemRarity.Common, 50f),
        new RarityWeight(ItemRarity.Uncommon, 30f),
        new RarityWeight(ItemRarity.Rare, 15f),
        new RarityWeight(ItemRarity.Legendary, 5f),
    };

    public static float GetWeightForRarity(ItemRarity rarity)
    {
        for (int i = 0; i < RarityWeights.Length; i++)
        {
            if (RarityWeights[i].Rarity == rarity)
                return RarityWeights[i].Weight;
        }
        return 0f;
    }

    public static readonly ShopItemDef[] AllItems = new ShopItemDef[]
    {
        // === Trading Tools (10) ===
        new ShopItemDef("tool_stop_loss", "Stop-Loss Order",
            "Auto-sell a stock if it drops below a set threshold, preventing catastrophic losses",
            150, ItemRarity.Common, ItemCategory.TradingTool),
        new ShopItemDef("tool_limit_order", "Limit Order",
            "Auto-buy a stock when it hits a target price, enabling hands-free entry points",
            150, ItemRarity.Common, ItemCategory.TradingTool),
        new ShopItemDef("tool_speed_trader", "Speed Trader",
            "Reduce trade execution delay by 50%, enabling faster reactions",
            200, ItemRarity.Common, ItemCategory.TradingTool),
        new ShopItemDef("tool_flash_trade", "Flash Trade",
            "Execute 3 instant trades in rapid succession (ignores normal execution speed)",
            200, ItemRarity.Common, ItemCategory.TradingTool),
        new ShopItemDef("tool_margin_boost", "Margin Boost",
            "Increase available margin for shorting by 50%, enabling larger short positions",
            300, ItemRarity.Uncommon, ItemCategory.TradingTool),
        new ShopItemDef("tool_portfolio_hedge", "Portfolio Hedge",
            "Reduce losses on all positions by 25% for one round",
            300, ItemRarity.Uncommon, ItemCategory.TradingTool),
        new ShopItemDef("tool_leverage", "Leverage (2x)",
            "Double the size of your next trade. Double the profit or double the loss",
            250, ItemRarity.Uncommon, ItemCategory.TradingTool),
        new ShopItemDef("tool_options_contract", "Options Contract",
            "Pay a premium to lock in a buy/sell price for later in the round",
            350, ItemRarity.Uncommon, ItemCategory.TradingTool),
        new ShopItemDef("tool_dark_pool", "Dark Pool Access",
            "Execute one trade per round at guaranteed mid-price (no slippage)",
            400, ItemRarity.Rare, ItemCategory.TradingTool),
        new ShopItemDef("tool_algo_bot", "Algorithmic Bot",
            "Automatically executes a simple strategy (e.g., buy dips on strongest stock)",
            500, ItemRarity.Rare, ItemCategory.TradingTool),

        // === Market Intel (10) ===
        new ShopItemDef("intel_analyst_report", "Analyst Report",
            "Reveals the base trend direction (bull/bear/neutral) for one stock next round",
            100, ItemRarity.Common, ItemCategory.MarketIntel),
        new ShopItemDef("intel_earnings_calendar", "Earnings Calendar",
            "Know exactly when earnings events will fire during next round",
            150, ItemRarity.Common, ItemCategory.MarketIntel),
        new ShopItemDef("intel_insider_tip", "Insider Tip",
            "Preview one market event that will occur next round",
            200, ItemRarity.Common, ItemCategory.MarketIntel),
        new ShopItemDef("intel_short_interest", "Short Interest Data",
            "Reveals if a short squeeze is likely on any stock next round",
            200, ItemRarity.Uncommon, ItemCategory.MarketIntel),
        new ShopItemDef("intel_sector_forecast", "Sector Forecast",
            "Reveals which sector will outperform next round",
            250, ItemRarity.Uncommon, ItemCategory.MarketIntel),
        new ShopItemDef("intel_price_floor", "Price Floor Intel",
            "Shows the lowest price a stock will reach next round",
            300, ItemRarity.Uncommon, ItemCategory.MarketIntel),
        new ShopItemDef("intel_price_ceiling", "Price Ceiling Intel",
            "Shows the highest price a stock will reach next round",
            300, ItemRarity.Uncommon, ItemCategory.MarketIntel),
        new ShopItemDef("intel_market_maker", "Market Maker Feed",
            "See real-time buy/sell volume during trading (reveals momentum shifts)",
            400, ItemRarity.Rare, ItemCategory.MarketIntel),
        new ShopItemDef("intel_crystal_ball", "Crystal Ball",
            "Preview the full price chart shape for one stock (without exact values)",
            500, ItemRarity.Rare, ItemCategory.MarketIntel),
        new ShopItemDef("intel_wiretap", "Wiretap",
            "Know ALL events for next round in advance",
            600, ItemRarity.Legendary, ItemCategory.MarketIntel),

        // === Passive Perks (10) ===
        new ShopItemDef("perk_volume_discount", "Volume Discount",
            "All trades cost 10% less in fees (stacks)",
            150, ItemRarity.Common, ItemCategory.PassivePerk),
        new ShopItemDef("perk_interest_accrual", "Interest Accrual",
            "Earn 5% interest on unspent cash at end of each round",
            200, ItemRarity.Common, ItemCategory.PassivePerk),
        new ShopItemDef("perk_market_intuition", "Market Intuition",
            "Events are telegraphed 3 seconds earlier (visual cue appears sooner)",
            200, ItemRarity.Common, ItemCategory.PassivePerk),
        new ShopItemDef("perk_dividend_income", "Dividend Income",
            "Held stocks generate small passive income during trading",
            250, ItemRarity.Uncommon, ItemCategory.PassivePerk),
        new ShopItemDef("perk_risk_appetite", "Risk Appetite",
            "Profit targets reduced by 10% (permanent, stacks up to 3x)",
            300, ItemRarity.Uncommon, ItemCategory.PassivePerk),
        new ShopItemDef("perk_portfolio_insurance", "Portfolio Insurance",
            "First loss each round is halved",
            350, ItemRarity.Uncommon, ItemCategory.PassivePerk),
        new ShopItemDef("perk_compound_interest", "Compound Interest",
            "All profits earn an additional 15% bonus at round end",
            400, ItemRarity.Rare, ItemCategory.PassivePerk),
        new ShopItemDef("perk_wolf_instinct", "Wolf Instinct",
            "Start each round with a brief price preview (2-second future glimpse)",
            450, ItemRarity.Rare, ItemCategory.PassivePerk),
        new ShopItemDef("perk_golden_parachute", "Golden Parachute",
            "Survive one Margin Call (consumed on use)",
            500, ItemRarity.Rare, ItemCategory.PassivePerk),
        new ShopItemDef("perk_master_universe", "Master of the Universe",
            "All shop items cost 25% less for the rest of the run",
            600, ItemRarity.Legendary, ItemCategory.PassivePerk),
    };

    public static readonly HashSet<string> DefaultUnlockedItems = new HashSet<string>
    {
        "tool_stop_loss", "tool_limit_order", "tool_speed_trader", "tool_flash_trade",
        "tool_margin_boost", "tool_portfolio_hedge", "tool_leverage", "tool_options_contract",
        "tool_dark_pool", "tool_algo_bot",
        "intel_analyst_report", "intel_earnings_calendar", "intel_insider_tip", "intel_short_interest",
        "intel_sector_forecast", "intel_price_floor", "intel_price_ceiling", "intel_market_maker",
        "intel_crystal_ball", "intel_wiretap",
        "perk_volume_discount", "perk_interest_accrual", "perk_market_intuition", "perk_dividend_income",
        "perk_risk_appetite", "perk_portfolio_insurance", "perk_compound_interest", "perk_wolf_instinct",
        "perk_golden_parachute", "perk_master_universe",
    };

    public static bool IsUnlocked(string itemId, HashSet<string> unlockedPool)
    {
        return unlockedPool.Contains(itemId);
    }

    public static List<ShopItemDef> GetUnlockedItems(HashSet<string> unlockedPool)
    {
        var result = new List<ShopItemDef>();
        for (int i = 0; i < AllItems.Length; i++)
        {
            if (unlockedPool.Contains(AllItems[i].Id))
                result.Add(AllItems[i]);
        }
        return result;
    }

    /// <summary>
    /// Returns all items for the given category.
    /// </summary>
    public static ShopItemDef[] GetItemsByCategory(ItemCategory category)
    {
        int count = 0;
        for (int i = 0; i < AllItems.Length; i++)
        {
            if (AllItems[i].Category == category) count++;
        }

        var result = new ShopItemDef[count];
        int idx = 0;
        for (int i = 0; i < AllItems.Length; i++)
        {
            if (AllItems[i].Category == category)
                result[idx++] = AllItems[i];
        }
        return result;
    }
}
