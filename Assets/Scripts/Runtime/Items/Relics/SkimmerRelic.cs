/// <summary>
/// Skimmer: On buy trade, 3% of total trade value is added to cash.
/// Story 17.3 AC 4.
/// </summary>
public class SkimmerRelic : RelicBase
{
    public override string Id => "relic_skimmer";

    public override void OnAfterTrade(RunContext ctx, TradeExecutedEvent e)
    {
        // Only fires on buy (long), not on sell or short
        if (!e.IsBuy || e.IsShort) return;

        float bonus = e.TotalCost * 0.03f;
        ctx.Portfolio.AddCash(bonus);

        EventBus.Publish(new TradeFeedbackEvent
        {
            Message = $"+${bonus:F2}",
            IsSuccess = true,
            IsBuy = true,
            IsShort = false
        });

        EventBus.Publish(new RelicActivatedEvent { RelicId = Id });
    }
}
