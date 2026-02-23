/// <summary>
/// Per-round portfolio value targets (FIX-19: accelerating exponential for skilled play).
/// Players must reach these TOTAL CASH values or face margin call at round end.
/// These are cumulative value targets, not per-round profit deltas.
/// Values are public static readonly for easy balance tuning.
/// Accelerating growth rate (2.0x→3.25x) — accessible early, brutal late game.
/// Skilled players win ~50% of runs.
/// </summary>
public static class MarginCallTargets
{
    /// <summary>
    /// Portfolio value targets per round (0-indexed internally, 1-based access via GetTarget).
    /// FIX-19: $20, $55, $160, $500, $1600, $5500, $20000, $65000 (accelerating exponential).
    /// Player must end round with total cash >= target to avoid margin call.
    /// </summary>
    public static readonly float[] Targets = new float[]
    {
        20f,      // Round 1 — Act 1 (2.0x your $10)
        55f,      // Round 2 — Act 1
        160f,     // Round 3 — Act 2
        500f,     // Round 4 — Act 2
        1600f,    // Round 5 — Act 3
        5500f,    // Round 6 — Act 3
        20000f,   // Round 7 — Act 4
        65000f,   // Round 8 — Act 4 (Final)
    };

    /// <summary>
    /// Scaling multiplier per round for reference/tuning documentation.
    /// Represents approximate multiplier from starting $10 to target.
    /// NOTE: These are reference labels for difficulty context, not used in
    /// target calculation. Keep in sync with Targets when rebalancing.
    /// </summary>
    public static readonly float[] ScalingMultipliers = new float[]
    {
        2.0f,     // Round 1 — Act 1 (2.0x from $10)
        5.5f,     // Round 2 — Act 1
        16.0f,    // Round 3 — Act 2
        50.0f,    // Round 4 — Act 2
        160.0f,   // Round 5 — Act 3
        550.0f,   // Round 6 — Act 3
        2000.0f,  // Round 7 — Act 4
        6500.0f,  // Round 8 — Act 4
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
