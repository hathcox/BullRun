/// <summary>
/// Interface for all relic effect implementations.
/// Hook methods are called by RelicManager during game lifecycle events.
/// Relics are dispatched left-to-right in acquisition order.
/// Story 17.1: Framework only — concrete relic effects added in Stories 17.3-17.7.
/// </summary>
public interface IRelic
{
    string Id { get; }

    void OnAcquired(RunContext ctx);
    void OnRemoved(RunContext ctx);
    void OnRoundStart(RunContext ctx, RoundStartedEvent e);
    void OnRoundEnd(RunContext ctx, MarketClosedEvent e);
    void OnBeforeTrade(RunContext ctx, TradeExecutedEvent e);
    void OnAfterTrade(RunContext ctx, TradeExecutedEvent e);
    void OnMarketEventFired(RunContext ctx, MarketEventFiredEvent e);
    void OnReputationChanged(RunContext ctx, int oldRep, int newRep);
    void OnShopOpen(RunContext ctx);
    void OnSellSelf(RunContext ctx);
    /// <summary>
    /// Story 17.5: Returns a custom sell refund value, or null for default 50% cost refund.
    /// Compound Rep uses this to return its exponentially growing sell value.
    /// </summary>
    int? GetSellValue(RunContext ctx);
}
