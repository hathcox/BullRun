/// <summary>
/// Per-round profit targets from GDD Section 2.3.
/// Players must hit these targets or face margin call at round end.
/// </summary>
public static class MarginCallTargets
{
    private static readonly float[] _targets = new float[]
    {
        200f,   // Round 1 — Act 1
        350f,   // Round 2 — Act 1
        600f,   // Round 3 — Act 2
        900f,   // Round 4 — Act 2
        1500f,  // Round 5 — Act 3
        2200f,  // Round 6 — Act 3
        3500f,  // Round 7 — Act 4
        5000f,  // Round 8 — Act 4
    };

    public static int TotalRounds => _targets.Length;

    /// <summary>
    /// Returns the profit target for the given round number (1-based).
    /// Clamps to valid range: rounds below 1 return first target, rounds beyond 8 return last target.
    /// </summary>
    public static float GetTarget(int roundNumber)
    {
        if (roundNumber < 1) return _targets[0];
        if (roundNumber > _targets.Length) return _targets[_targets.Length - 1];
        return _targets[roundNumber - 1];
    }
}
