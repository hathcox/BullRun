/// <summary>
/// Catalyst Trader: On buy trade, immediately triggers a random market event on the active stock.
/// Buy cooldown is increased by +3s (handled passively by RelicManager.GetEffectiveCooldown).
/// Story 17.4 AC 1.
/// </summary>
public class CatalystTraderRelic : RelicBase
{
    public override string Id => "relic_event_trigger";

    public override void OnAfterTrade(RunContext ctx, TradeExecutedEvent e)
    {
        // Only fires on buy (long), not on sell or short
        if (!e.IsBuy || e.IsShort) return;
        if (ctx.EventScheduler == null) return;

        ctx.EventScheduler.ForceFireRandomEvent();
        EventBus.Publish(new RelicActivatedEvent { RelicId = Id });
    }
}
