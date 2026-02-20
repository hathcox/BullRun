/// <summary>
/// Story 17.5: Rep Doubler — doubles Reputation earned from trade performance at round end.
/// Passive relic: MarginCallState checks for this relic's presence during Rep calculation.
/// Does NOT affect bond Rep payouts, Rep Interest, or other non-trade Rep sources.
/// </summary>
public class RepDoublerRelic : RelicBase
{
    public override string Id => "relic_rep_doubler";
}
