using System.Collections.Generic;

/// <summary>
/// Manages expansion ownership and shop offering selection.
/// Plain C# class (not MonoBehaviour) for testability.
/// Reads/writes through RunContext.OwnedExpansions.
/// Effects are NOT applied here — that is Story 13.7.
/// </summary>
public class ExpansionManager
{
    private readonly RunContext _ctx;

    public ExpansionManager(RunContext ctx)
    {
        _ctx = ctx;
    }

    /// <summary>
    /// Returns the list of owned expansion IDs (delegates to RunContext).
    /// </summary>
    public List<string> OwnedExpansions => _ctx.OwnedExpansions;

    /// <summary>
    /// Checks if the given expansion has already been purchased this run.
    /// </summary>
    public bool IsOwned(string expansionId)
    {
        return _ctx.OwnedExpansions.Contains(expansionId);
    }

    /// <summary>
    /// Convenience method: checks if the given expansion is owned (alias for IsOwned).
    /// Used by all game systems at their entry points to apply expansion effects.
    /// Effects are stateless checks — not cumulative modifiers. Checking twice has no
    /// additional effect (Story 13.7, AC 7, 8).
    /// </summary>
    public bool HasExpansion(string expansionId)
    {
        return _ctx.OwnedExpansions.Contains(expansionId);
    }

    /// <summary>
    /// Static convenience: checks if a RunContext owns the given expansion.
    /// Used by systems that don't have an ExpansionManager instance.
    /// </summary>
    public static bool HasExpansion(RunContext ctx, string expansionId)
    {
        return ctx.OwnedExpansions.Contains(expansionId);
    }

    /// <summary>
    /// Returns up to 'count' random unowned expansions for this shop visit.
    /// Uses the provided System.Random for deterministic testing.
    /// </summary>
    public ExpansionDef[] GetAvailableForShop(int count, System.Random random = null)
    {
        random = random ?? new System.Random();

        var unowned = new List<ExpansionDef>();
        for (int i = 0; i < ExpansionDefinitions.All.Length; i++)
        {
            if (!IsOwned(ExpansionDefinitions.All[i].Id))
            {
                unowned.Add(ExpansionDefinitions.All[i]);
            }
        }

        // Fisher-Yates shuffle
        for (int i = unowned.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            var temp = unowned[i];
            unowned[i] = unowned[j];
            unowned[j] = temp;
        }

        int resultCount = count < unowned.Count ? count : unowned.Count;
        var result = new ExpansionDef[resultCount];
        for (int i = 0; i < resultCount; i++)
        {
            result[i] = unowned[i];
        }

        return result;
    }
}
