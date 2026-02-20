/// <summary>
/// Profit Refresh: When the player sells a stock at a profit, the buy cooldown is reset to 0.
/// Only triggers on sell (long) at a profit — sell at loss, buy, and short do not trigger.
/// Publishes a TradeFeedbackEvent visual cue for the cooldown refresh.
/// Story 17.4 AC 4.
/// </summary>
public class ProfitRefreshRelic : RelicBase
{
    public override string Id => "relic_profit_refresh";

    public override void OnAfterTrade(RunContext ctx, TradeExecutedEvent e)
    {
        // Only fires on sell-at-profit: !IsBuy, !IsShort, ProfitLoss > 0
        if (e.IsBuy || e.IsShort || e.ProfitLoss <= 0f) return;

        // Story 17.4 review fix: Use RunContext delegate instead of GameRunner.Instance (architecture compliance)
        ctx.ResetBuyCooldownAction?.Invoke();

        EventBus.Publish(new TradeFeedbackEvent
        {
            Message = "BUY READY",
            IsSuccess = true,
            IsBuy = true,
            IsShort = false
        });

        EventBus.Publish(new RelicActivatedEvent { RelicId = Id });
    }
}
