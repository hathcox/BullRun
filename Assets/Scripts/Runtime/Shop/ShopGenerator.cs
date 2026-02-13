using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates shop offerings by selecting one item per category using rarity-weighted random selection.
/// Supports unlock filtering (Epic 9 preparation) and duplicate prevention (owned items excluded).
/// Pure C# class — no MonoBehaviour dependency for testability.
/// Stateless — all data passed via parameters, no cached state between calls.
/// </summary>
public static class ShopGenerator
{
    /// <summary>
    /// Generates a shop offering of 3 items — one per category.
    /// Uses rarity-weighted random selection with unlock filtering and duplicate prevention.
    /// Returns nullable items — a null entry means that category's pool is exhausted.
    /// </summary>
    public static ShopItemDef?[] GenerateOffering(List<string> ownedItemIds, HashSet<string> unlockedPool, System.Random random)
    {
        return new ShopItemDef?[]
        {
            SelectItem(ItemCategory.TradingTool, ownedItemIds, unlockedPool, random),
            SelectItem(ItemCategory.MarketIntel, ownedItemIds, unlockedPool, random),
            SelectItem(ItemCategory.PassivePerk, ownedItemIds, unlockedPool, random),
        };
    }

    /// <summary>
    /// Selects a single item from the given category using rarity-weighted random selection.
    /// Filters by unlock pool, excludes owned items, then does weighted rarity selection.
    /// Returns null if no eligible items remain for the category.
    /// </summary>
    public static ShopItemDef? SelectItem(ItemCategory category, List<string> ownedItemIds, HashSet<string> unlockedPool, System.Random random)
    {
        // 1. Filter items: category match, unlocked, not owned
        var ownedSet = new HashSet<string>(ownedItemIds);
        var eligible = new List<ShopItemDef>();
        var allItems = ShopItemDefinitions.AllItems;
        for (int i = 0; i < allItems.Length; i++)
        {
            if (allItems[i].Category != category) continue;
            if (!unlockedPool.Contains(allItems[i].Id)) continue;
            if (ownedSet.Contains(allItems[i].Id)) continue;
            eligible.Add(allItems[i]);
        }

        if (eligible.Count == 0)
        {
            Debug.LogWarning($"[ShopGenerator] Category {category} pool exhausted — no eligible items remaining");
            return null;
        }

        // 2. Group by rarity
        var grouped = new Dictionary<ItemRarity, List<ShopItemDef>>();
        for (int i = 0; i < eligible.Count; i++)
        {
            var rarity = eligible[i].Rarity;
            if (!grouped.ContainsKey(rarity))
                grouped[rarity] = new List<ShopItemDef>();
            grouped[rarity].Add(eligible[i]);
        }

        // 3. Weighted random select a rarity tier
        ItemRarity selectedRarity = SelectWeightedRarity(grouped, random);

        // 4. Random select one item from that rarity tier
        var pool = grouped[selectedRarity];
        return pool[random.Next(pool.Count)];
    }

    /// <summary>
    /// Selects a rarity tier using weighted random selection.
    /// Only considers rarities that have items in groupedItems.
    /// Weights are normalized to only include present rarities.
    /// </summary>
    public static ItemRarity SelectWeightedRarity(Dictionary<ItemRarity, List<ShopItemDef>> groupedItems, System.Random random)
    {
        // Collect and sort keys to guarantee stable iteration order across .NET versions
        var keys = new List<ItemRarity>(groupedItems.Count);
        foreach (var kvp in groupedItems)
            keys.Add(kvp.Key);
        keys.Sort();

        // Build cumulative weight from sorted rarities
        float totalWeight = 0f;
        for (int i = 0; i < keys.Count; i++)
            totalWeight += ShopItemDefinitions.GetWeightForRarity(keys[i]);

        // Roll and walk cumulative weights in sorted order
        float roll = (float)(random.NextDouble() * totalWeight);
        float cumulative = 0f;
        for (int i = 0; i < keys.Count; i++)
        {
            cumulative += ShopItemDefinitions.GetWeightForRarity(keys[i]);
            if (roll < cumulative)
                return keys[i];
        }

        // Edge case: floating point — return last rarity
        return keys[keys.Count - 1];
    }
}
