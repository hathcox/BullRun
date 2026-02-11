/// <summary>
/// Core game constants. Single source of truth for all balance and timing values.
/// </summary>
public static class GameConfig
{
    public static readonly float StartingCapital = 1000f;
    public static readonly float RoundDurationSeconds = 60f;

    // 0 = per-frame updates (no fixed interval, UpdatePrice called every frame)
    public static readonly float PriceUpdateRate = 0f;

    // Short selling: margin collateral as percentage of position value
    public static readonly float ShortMarginRequirement = 0.5f;
}
