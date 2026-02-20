/// <summary>
/// Story 17.5: Compound Rep — tracks rounds held internally. When sold, grants
/// 3 * 2^roundsHeld Reputation instead of the normal 50% cost refund.
/// </summary>
public class CompoundRepRelic : RelicBase
{
    private int _roundsHeld;

    public override string Id => "relic_compound_rep";

    /// <summary>Exposed for testing.</summary>
    public int RoundsHeld => _roundsHeld;

    public override void OnRoundStart(RunContext ctx, RoundStartedEvent e)
    {
        _roundsHeld++;
    }

    public override int? GetSellValue(RunContext ctx)
    {
        return 3 * (1 << _roundsHeld);
    }

    public override void OnSellSelf(RunContext ctx)
    {
        EventBus.Publish(new RelicActivatedEvent { RelicId = Id });
    }
}
