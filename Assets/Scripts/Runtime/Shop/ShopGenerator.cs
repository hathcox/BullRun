/// <summary>
/// Generates shop offerings by selecting one item per category using rarity-weighted random selection.
/// Pure C# class — no MonoBehaviour dependency for testability.
/// </summary>
public static class ShopGenerator
{
    /// <summary>
    /// Generates a shop offering of 3 items — one per category.
    /// Uses rarity-weighted random selection from ShopItemDefinitions.
    /// </summary>
    public static ShopItemDef[] GenerateOffering()
    {
        return new ShopItemDef[]
        {
            SelectItem(ItemCategory.TradingTool),
            SelectItem(ItemCategory.MarketIntel),
            SelectItem(ItemCategory.PassivePerk),
        };
    }

    /// <summary>
    /// Selects a single item from the given category using rarity-weighted random selection.
    /// Step 1: Roll rarity based on weights (Common ~50%, Uncommon ~30%, Rare ~15%, Legendary ~5%).
    /// Step 2: Pick a random item of that rarity within the category.
    /// Falls back to Common if no items of selected rarity exist for the category.
    /// </summary>
    public static ShopItemDef SelectItem(ItemCategory category)
    {
        ItemRarity rarity = RollRarity();
        return PickItemOfRarity(category, rarity);
    }

    /// <summary>
    /// Rolls a rarity based on configured weights.
    /// </summary>
    public static ItemRarity RollRarity()
    {
        float roll = UnityEngine.Random.value;
        float cumulative = 0f;

        for (int i = 0; i < ShopItemDefinitions.RarityWeights.Length; i++)
        {
            cumulative += ShopItemDefinitions.RarityWeights[i];
            if (roll <= cumulative)
                return (ItemRarity)i;
        }

        return ItemRarity.Common;
    }

    /// <summary>
    /// Picks a random item of the given rarity and category.
    /// Falls back to Common if no items match.
    /// </summary>
    private static ShopItemDef PickItemOfRarity(ItemCategory category, ItemRarity rarity)
    {
        // Collect matching items
        var allItems = ShopItemDefinitions.AllItems;
        int matchCount = 0;

        for (int i = 0; i < allItems.Length; i++)
        {
            if (allItems[i].Category == category && allItems[i].Rarity == rarity)
                matchCount++;
        }

        // Fallback to Common if no items of selected rarity
        if (matchCount == 0 && rarity != ItemRarity.Common)
        {
            return PickItemOfRarity(category, ItemRarity.Common);
        }

        // Pick random from matches
        if (matchCount == 0)
        {
            // Shouldn't happen — every category has Common items.
            // Return first item of the requested category as last resort.
            for (int i = 0; i < allItems.Length; i++)
            {
                if (allItems[i].Category == category)
                    return allItems[i];
            }
            return allItems[0];
        }

        int pick = UnityEngine.Random.Range(0, matchCount);
        int found = 0;
        for (int i = 0; i < allItems.Length; i++)
        {
            if (allItems[i].Category == category && allItems[i].Rarity == rarity)
            {
                if (found == pick)
                    return allItems[i];
                found++;
            }
        }

        // Final safety net — return first item of requested category
        for (int i = 0; i < allItems.Length; i++)
        {
            if (allItems[i].Category == category)
                return allItems[i];
        }
        return allItems[0];
    }
}
