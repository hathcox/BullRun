/// <summary>
/// Bull Believer: Positive market events have doubled price impact.
/// Short button is permanently disabled for the rest of the run.
/// OnAcquired sets ShortingDisabled = true. OnRoundStart sets PositiveImpactMultiplier = 2.0.
/// Story 17.4 AC 5.
/// </summary>
public class BullBelieverRelic : RelicBase
{
    public override string Id => "relic_bull_believer";

    public override void OnAcquired(RunContext ctx)
    {
        ctx.ShortingDisabled = true;
    }

    public override void OnRoundStart(RunContext ctx, RoundStartedEvent e)
    {
        // Set positive impact multiplier after EventScheduler resets multipliers
        if (ctx.EventScheduler != null)
            ctx.EventScheduler.PositiveImpactMultiplier = 2.0f;

        EventBus.Publish(new RelicActivatedEvent { RelicId = Id });
    }

    public override void OnMarketEventFired(RunContext ctx, MarketEventFiredEvent e)
    {
        // No action needed — PositiveImpactMultiplier is already applied by EventScheduler.FireEvent
        // This hook exists for future UI glow integration
    }
}
