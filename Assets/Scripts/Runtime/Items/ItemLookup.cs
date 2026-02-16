using System.Collections.Generic;

/// <summary>
/// Static utility for resolving relic IDs to RelicDef definitions.
/// Caches a dictionary on first access for O(1) lookups.
/// Story 13.9: Converted from ShopItemDef to RelicDef.
/// Removed category filtering and rarity colors (no longer relevant concepts).
/// </summary>
public static class ItemLookup
{
    private static Dictionary<string, RelicDef> _cache;

    /// <summary>
    /// Returns the RelicDef for the given relic ID, or null if not found.
    /// First call builds the cache from ShopItemDefinitions.RelicPool.
    /// </summary>
    public static RelicDef? GetRelicById(string relicId)
    {
        EnsureCache();
        if (_cache.TryGetValue(relicId, out var def))
            return def;
        return null;
    }

    /// <summary>
    /// Builds the lookup cache on first access.
    /// </summary>
    private static void EnsureCache()
    {
        if (_cache != null) return;
        _cache = new Dictionary<string, RelicDef>(ShopItemDefinitions.RelicPool.Length);
        for (int i = 0; i < ShopItemDefinitions.RelicPool.Length; i++)
        {
            _cache[ShopItemDefinitions.RelicPool[i].Id] = ShopItemDefinitions.RelicPool[i];
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
