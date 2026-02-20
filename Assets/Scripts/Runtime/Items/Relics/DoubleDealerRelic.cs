/// <summary>
/// Double Dealer: Trade quantity is doubled for buy and sell.
/// Uses passive query pattern — RelicManager.GetEffectiveTradeQuantity() checks for this relic.
/// Story 17.3 AC 1.
/// </summary>
public class DoubleDealerRelic : RelicBase
{
    public override string Id => "relic_double_dealer";
}
