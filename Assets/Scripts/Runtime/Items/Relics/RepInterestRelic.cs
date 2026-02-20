/// <summary>
/// Story 17.5: Rep Interest — at round start, gain 10% of current Reputation as bonus Rep
/// (rounded down via integer division).
/// </summary>
public class RepInterestRelic : RelicBase
{
    public override string Id => "relic_rep_interest";

    public override void OnRoundStart(RunContext ctx, RoundStartedEvent e)
    {
        int interest = ctx.Reputation.Current / 10;
        if (interest <= 0) return;

        ctx.Reputation.Add(interest);
        EventBus.Publish(new TradeFeedbackEvent
        {
            Message = $"+{interest} Rep Interest",
            IsSuccess = true, IsBuy = false, IsShort = false
        });
        EventBus.Publish(new RelicActivatedEvent { RelicId = Id });
    }
}
