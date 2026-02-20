/// <summary>
/// Loss Liquidator: When the player sells a stock at a loss, a random market event fires.
/// Only triggers on sell (long) at a loss — sell at profit, buy, and short do not trigger.
/// Story 17.4 AC 3.
/// </summary>
public class LossLiquidatorRelic : RelicBase
{
    public override string Id => "relic_loss_liquidator";

    public override void OnAfterTrade(RunContext ctx, TradeExecutedEvent e)
    {
        // Only fires on sell-at-loss: !IsBuy, !IsShort, ProfitLoss < 0
        if (e.IsBuy || e.IsShort || e.ProfitLoss >= 0f) return;
        if (ctx.EventScheduler == null) return;

        ctx.EventScheduler.ForceFireRandomEvent();
        EventBus.Publish(new RelicActivatedEvent { RelicId = Id });
    }
}
