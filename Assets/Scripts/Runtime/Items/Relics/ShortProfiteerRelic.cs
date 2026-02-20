/// <summary>
/// Short Profiteer: On short open, 10% of stock value is added to cash.
/// Story 17.3 AC 5.
/// </summary>
public class ShortProfiteerRelic : RelicBase
{
    public override string Id => "relic_short_profiteer";

    public override void OnAfterTrade(RunContext ctx, TradeExecutedEvent e)
    {
        // Only fires on short open (IsShort=true, IsBuy=false), not cover
        if (!e.IsShort || e.IsBuy) return;

        float stockValue = e.Shares * e.Price;
        float bonus = stockValue * 0.10f;
        ctx.Portfolio.AddCash(bonus);

        EventBus.Publish(new TradeFeedbackEvent
        {
            Message = $"+${bonus:F2}",
            IsSuccess = true,
            IsBuy = false,
            IsShort = true
        });

        EventBus.Publish(new RelicActivatedEvent { RelicId = Id });
    }
}
