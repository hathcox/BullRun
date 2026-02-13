/// <summary>
/// Per-round profit targets from GDD Section 2.3.
/// Players must hit these targets or face margin call at round end.
/// Values are public static readonly for easy balance tuning.
/// </summary>
public static class MarginCallTargets
{
    /// <summary>
    /// Profit targets per round (0-indexed internally, 1-based access via GetTarget).
    /// GDD Section 2.3: $200, $350, $600, $900, $1500, $2200, $3500, $5000
    /// </summary>
    public static readonly float[] Targets = new float[]
    {
        200f,   // Round 1 — Act 1 (Tutorial)
        350f,   // Round 2 — Act 1 (Easy)
        600f,   // Round 3 — Act 2 (Medium)
        900f,   // Round 4 — Act 2 (Medium)
        1500f,  // Round 5 — Act 3 (Hard)
        2200f,  // Round 6 — Act 3 (Hard)
        3500f,  // Round 7 — Act 4 (Expert)
        5000f,  // Round 8 — Act 4 (Final)
    };

    /// <summary>
    /// Scaling multiplier per round for reference/tuning documentation.
    /// Act 1: 1.0x, Act 2: 1.5x, Act 3: 2.0x, Act 4: 2.5x-3.0x
    /// NOTE: These are GDD reference labels for difficulty context, not used in
    /// target calculation. Keep in sync with Targets when rebalancing.
    /// </summary>
    public static readonly float[] ScalingMultipliers = new float[]
    {
        1.0f,   // Round 1 — Act 1
        1.0f,   // Round 2 — Act 1
        1.5f,   // Round 3 — Act 2
        1.5f,   // Round 4 — Act 2
        2.0f,   // Round 5 — Act 3
        2.0f,   // Round 6 — Act 3
        2.5f,   // Round 7 — Act 4
        3.0f,   // Round 8 — Act 4
    };

    public static int TotalRounds => Targets.Length;

    /// <summary>
    /// Returns the profit target for the given round number (1-based).
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
