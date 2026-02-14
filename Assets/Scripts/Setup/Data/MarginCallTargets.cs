/// <summary>
/// Per-round portfolio value targets (FIX-14: rebalanced for $10 economy).
/// Players must reach these TOTAL CASH values or face margin call at round end.
/// These are cumulative value targets, not per-round profit deltas.
/// Values are public static readonly for easy balance tuning.
/// </summary>
public static class MarginCallTargets
{
    /// <summary>
    /// Portfolio value targets per round (0-indexed internally, 1-based access via GetTarget).
    /// FIX-14: $20, $35, $60, $100, $175, $300, $500, $800 (cumulative value targets).
    /// Player must end round with total cash >= target to avoid margin call.
    /// </summary>
    public static readonly float[] Targets = new float[]
    {
        20f,    // Round 1 — Act 1 (double your $10)
        35f,    // Round 2 — Act 1
        60f,    // Round 3 — Act 2
        100f,   // Round 4 — Act 2
        175f,   // Round 5 — Act 3
        300f,   // Round 6 — Act 3
        500f,   // Round 7 — Act 4
        800f,   // Round 8 — Act 4 (Final)
    };

    /// <summary>
    /// Scaling multiplier per round for reference/tuning documentation.
    /// Represents approximate multiplier from starting $10 to target.
    /// NOTE: These are reference labels for difficulty context, not used in
    /// target calculation. Keep in sync with Targets when rebalancing.
    /// </summary>
    public static readonly float[] ScalingMultipliers = new float[]
    {
        2.0f,   // Round 1 — Act 1 (2x from $10)
        3.5f,   // Round 2 — Act 1
        6.0f,   // Round 3 — Act 2
        10.0f,  // Round 4 — Act 2
        17.5f,  // Round 5 — Act 3
        30.0f,  // Round 6 — Act 3
        50.0f,  // Round 7 — Act 4
        80.0f,  // Round 8 — Act 4
    };

    public static int TotalRounds => Targets.Length;

    /// <summary>
    /// Returns the cumulative value target for the given round number (1-based).
    /// Clamps to valid range: rounds below 1 return first target, rounds beyond 8 return last target.
    /// </summary>
    public static float GetTarget(int roundNumber)
    {
        if (roundNumber < 1) return Targets[0];
        if (roundNumber > Targets.Length) return Targets[Targets.Length - 1];
        return Targets[roundNumber - 1];
    }

    /// <summary>
    /// Returns a copy of all targets for debug display or iteration.
    /// </summary>
    public static float[] GetAllTargets()
    {
        return (float[])Targets.Clone();
    }
}
