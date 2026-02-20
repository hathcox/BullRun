/// <summary>
/// Event Storm: At round start, doubles event count and reduces all event impacts by 25%.
/// Sets EventScheduler.EventCountMultiplier = 2.0 and ImpactMultiplier = 0.75.
/// Story 17.4 AC 2.
/// </summary>
public class EventStormRelic : RelicBase
{
    public override string Id => "relic_event_storm";

    public override void OnRoundStart(RunContext ctx, RoundStartedEvent e)
    {
        if (ctx.EventScheduler != null)
        {
            ctx.EventScheduler.EventCountMultiplier = 2.0f;
            ctx.EventScheduler.ImpactMultiplier = 0.75f;
        }

        EventBus.Publish(new RelicActivatedEvent { RelicId = Id });
    }
}
