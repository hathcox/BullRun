using UnityEngine;

/// <summary>
/// Time Buyer: buying a stock extends the active round timer by 5 seconds.
/// No cap on extensions. Timer UI updates automatically since it reads TradingState.ActiveTimeRemaining.
/// Story 17.6 AC 1.
/// </summary>
public class TimeBuyerRelic : RelicBase
{
    public override string Id => "relic_time_buyer";

    public override void OnAfterTrade(RunContext ctx, TradeExecutedEvent e)
    {
        // Only fires on buy (long), not on sell or short
        if (!e.IsBuy || e.IsShort) return;

        TradingState.ExtendTimer(5f);

        EventBus.Publish(new RelicActivatedEvent { RelicId = Id });

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[TimeBuyerRelic] Extended round timer by 5s");
        #endif
    }
}
