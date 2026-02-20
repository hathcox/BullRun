/// <summary>
/// Bear Raid: Shorts execute 3 shares. Buy and sell (longs) permanently disabled.
/// OnAcquired sets LongsDisabled. Short shares via passive query pattern.
/// Story 17.3 AC 3.
/// </summary>
public class ShortMultiplierRelic : RelicBase
{
    public override string Id => "relic_short_multiplier";

    public override void OnAcquired(RunContext ctx)
    {
        ctx.LongsDisabled = true;
    }
}
