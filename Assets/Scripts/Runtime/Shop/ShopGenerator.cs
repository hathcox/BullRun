using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates shop relic offerings using uniform random selection.
/// Story 13.3: No rarity weighting — all relics equally likely.
/// Pure C# class — no MonoBehaviour dependency for testability.
/// Stateless — all data passed via parameters, no cached state between calls.
/// </summary>
public static class ShopGenerator
{
    /// <summary>
    /// Generates a relic offering of 3 items via uniform random selection.
    /// Excludes owned relics. Returns null entries for exhausted slots.
    /// </summary>
    public static RelicDef?[] GenerateRelicOffering(List<string> ownedRelicIds, System.Random random)
    {
        return GenerateRelicOffering(ownedRelicIds, null, random);
    }

    /// <summary>
    /// Generates a relic offering excluding both owned relics and additional IDs
    /// (e.g., currently displayed unsold relics during a reroll).
    /// </summary>
    public static RelicDef?[] GenerateRelicOffering(List<string> ownedRelicIds, List<string> additionalExcludeIds, System.Random random)
    {
        var pool = BuildAvailablePool(ownedRelicIds, additionalExcludeIds);

        var result = new RelicDef?[3];
        for (int i = 0; i < 3; i++)
        {
            if (pool.Count == 0)
            {
                result[i] = null;
                continue;
            }
            int idx = random.Next(pool.Count);
            result[i] = pool[idx];
            pool.RemoveAt(idx);
        }

        return result;
    }

    /// <summary>
    /// Builds the available pool by filtering out owned relics from RelicPool.
    /// </summary>
    public static List<RelicDef> BuildAvailablePool(List<string> ownedRelicIds)
    {
        return BuildAvailablePool(ownedRelicIds, null);
    }

    /// <summary>
    /// Builds the available pool by filtering out owned and additionally excluded relics.
    /// </summary>
    public static List<RelicDef> BuildAvailablePool(List<string> ownedRelicIds, List<string> additionalExcludeIds)
    {
        var excludeSet = new HashSet<string>(ownedRelicIds);
        if (additionalExcludeIds != null)
        {
            for (int i = 0; i < additionalExcludeIds.Count; i++)
                excludeSet.Add(additionalExcludeIds[i]);
        }
        var pool = new List<RelicDef>();
        var relicPool = ShopItemDefinitions.RelicPool;
        for (int i = 0; i < relicPool.Length; i++)
        {
            if (!excludeSet.Contains(relicPool[i].Id))
                pool.Add(relicPool[i]);
        }
        return pool;
    }

    /// <summary>
    /// Legacy offering generator — kept for backwards compatibility with tests.
    /// Generates one item per category using rarity-weighted selection.
    /// Will be removed in Story 13.9 cleanup.
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
    /// Legacy item selector — rarity-weighted, category-filtered.
    /// Will be removed in Story 13.9 cleanup.
    /// </summary>
    public static ShopItemDef? SelectItem(ItemCategory category, List<string> ownedItemIds, HashSet<string> unlockedPool, System.Random random)
    {
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

        var grouped = new Dictionary<ItemRarity, List<ShopItemDef>>();
        for (int i = 0; i < eligible.Count; i++)
        {
            var rarity = eligible[i].Rarity;
            if (!grouped.ContainsKey(rarity))
                grouped[rarity] = new List<ShopItemDef>();
            grouped[rarity].Add(eligible[i]);
        }

        ItemRarity selectedRarity = SelectWeightedRarity(grouped, random);
        var pool = grouped[selectedRarity];
        return pool[random.Next(pool.Count)];
    }

    /// <summary>
    /// Legacy rarity selector — weighted random.
    /// Will be removed in Story 13.9 cleanup.
    /// </summary>
    public static ItemRarity SelectWeightedRarity(Dictionary<ItemRarity, List<ShopItemDef>> groupedItems, System.Random random)
    {
        var keys = new List<ItemRarity>(groupedItems.Count);
        foreach (var kvp in groupedItems)
            keys.Add(kvp.Key);
        keys.Sort();

        float totalWeight = 0f;
        for (int i = 0; i < keys.Count; i++)
            totalWeight += ShopItemDefinitions.GetWeightForRarity(keys[i]);

        float roll = (float)(random.NextDouble() * totalWeight);
        float cumulative = 0f;
        for (int i = 0; i < keys.Count; i++)
        {
            cumulative += ShopItemDefinitions.GetWeightForRarity(keys[i]);
            if (roll < cumulative)
                return keys[i];
        }

        return keys[keys.Count - 1];
    }
}
