/// <summary>
/// Abstract base class for relic implementations.
/// All hook methods are virtual no-ops — subclasses override only what they need.
/// Story 17.1: Framework only — concrete relic effects added in Stories 17.3-17.7.
/// </summary>
public abstract class RelicBase : IRelic
{
    public abstract string Id { get; }

    public virtual void OnAcquired(RunContext ctx) { }
    public virtual void OnRemoved(RunContext ctx) { }
    public virtual void OnRoundStart(RunContext ctx, RoundStartedEvent e) { }
    public virtual void OnRoundEnd(RunContext ctx, MarketClosedEvent e) { }
    public virtual void OnBeforeTrade(RunContext ctx, TradeExecutedEvent e) { }
    public virtual void OnAfterTrade(RunContext ctx, TradeExecutedEvent e) { }
    public virtual void OnMarketEventFired(RunContext ctx, MarketEventFiredEvent e) { }
    public virtual void OnReputationChanged(RunContext ctx, int oldRep, int newRep) { }
    public virtual void OnShopOpen(RunContext ctx) { }
    public virtual void OnSellSelf(RunContext ctx) { }
}
