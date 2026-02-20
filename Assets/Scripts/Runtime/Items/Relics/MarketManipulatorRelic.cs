using UnityEngine;

/// <summary>
/// Market Manipulator: selling a stock (non-short) causes its price to drop 15% after the sell executes.
/// The sell itself executes at the pre-drop price (player gets full sell proceeds).
/// Creates a buy-the-dip opportunity.
/// Story 17.6 AC 3.
/// </summary>
public class MarketManipulatorRelic : RelicBase
{
    public override string Id => "relic_market_manipulator";

    public override void OnAfterTrade(RunContext ctx, TradeExecutedEvent e)
    {
        // Only fires on long sell (not buy, not short)
        if (e.IsBuy || e.IsShort) return;

        PriceGenerator.ApplyPriceMultiplier(e.StockId, 0.85f);

        EventBus.Publish(new RelicActivatedEvent { RelicId = Id });

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[MarketManipulatorRelic] Price dropped 15% after sell");
        #endif
    }
}
