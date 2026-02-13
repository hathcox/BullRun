using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static utility for resolving item IDs to ShopItemDef definitions.
/// Caches a dictionary on first access for O(1) lookups.
/// Used by ItemInventoryPanel and future Item systems (Epic 8).
/// </summary>
public static class ItemLookup
{
    private static Dictionary<string, ShopItemDef> _cache;

    /// <summary>
    /// Returns the ShopItemDef for the given item ID, or null if not found.
    /// First call builds the cache from ShopItemDefinitions.AllItems.
    /// </summary>
    public static ShopItemDef? GetItemById(string itemId)
    {
        EnsureCache();
        if (_cache.TryGetValue(itemId, out var def))
            return def;
        return null;
    }

    /// <summary>
    /// Filters a list of item IDs by category and returns the matching definitions in order.
    /// Preserves the order from the input list (important for Balatro-style left-to-right processing).
    /// </summary>
    public static List<ShopItemDef> GetItemsByCategory(List<string> itemIds, ItemCategory category)
    {
        EnsureCache();
        var result = new List<ShopItemDef>();
        for (int i = 0; i < itemIds.Count; i++)
        {
            if (_cache.TryGetValue(itemIds[i], out var def) && def.Category == category)
                result.Add(def);
        }
        return result;
    }

    /// <summary>
    /// Returns the display color for a given rarity tier.
    /// Common=gray, Uncommon=green, Rare=blue, Legendary=gold.
    /// </summary>
    public static Color GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common:    return new Color(0.6f, 0.6f, 0.6f, 1f);
            case ItemRarity.Uncommon:  return new Color(0.2f, 0.8f, 0.2f, 1f);
            case ItemRarity.Rare:      return new Color(0.3f, 0.5f, 1f, 1f);
            case ItemRarity.Legendary: return new Color(1f, 0.85f, 0f, 1f);
            default:                   return Color.white;
        }
    }

    /// <summary>
    /// Builds the lookup cache on first access.
    /// </summary>
    private static void EnsureCache()
    {
        if (_cache != null) return;
        _cache = new Dictionary<string, ShopItemDef>(ShopItemDefinitions.AllItems.Length);
        for (int i = 0; i < ShopItemDefinitions.AllItems.Length; i++)
        {
            _cache[ShopItemDefinitions.AllItems[i].Id] = ShopItemDefinitions.AllItems[i];
        }
    }

    /// <summary>
    /// Clears the cache. Useful for testing or if item definitions change.
    /// </summary>
    public static void ClearCache()
    {
        _cache = null;
    }
}
