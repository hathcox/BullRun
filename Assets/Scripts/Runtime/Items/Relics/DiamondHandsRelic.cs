/// <summary>
/// Diamond Hands: stocks held to round end gain 30% value before auto-liquidation.
/// Only long positions affected (not shorts).
/// Effect is passive — checked by MarketCloseState via RelicManager.GetLiquidationMultiplier().
/// Story 17.6 AC 2, 4.
/// </summary>
public class DiamondHandsRelic : RelicBase
{
    public override string Id => "relic_diamond_hands";
}
