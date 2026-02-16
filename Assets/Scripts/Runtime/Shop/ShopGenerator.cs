using System.Collections.Generic;

/// <summary>
/// Generates shop relic offerings using uniform random selection.
/// Story 13.3: No rarity weighting — all relics equally likely.
/// Story 13.9: Removed all legacy rarity-weighted selection logic.
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
}
