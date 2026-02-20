/// <summary>
/// Story 17.5: Rep Dividend — at round start, gain $1 cash for every 2 Reputation
/// the player currently has (integer division).
/// </summary>
public class RepDividendRelic : RelicBase
{
    public override string Id => "relic_rep_dividend";

    public override void OnRoundStart(RunContext ctx, RoundStartedEvent e)
    {
        int dividend = ctx.Reputation.Current / 2;
        if (dividend <= 0) return;

        ctx.Portfolio.AddCash(dividend);
        EventBus.Publish(new TradeFeedbackEvent
        {
            Message = $"+${dividend} Dividend",
            IsSuccess = true, IsBuy = false, IsShort = false
        });
        EventBus.Publish(new RelicActivatedEvent { RelicId = Id });
    }
}
