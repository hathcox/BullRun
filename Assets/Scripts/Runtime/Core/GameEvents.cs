/// <summary>
/// All game event type definitions.
/// Events are published via EventBus for inter-system communication.
/// Naming convention: {Subject}{Verb}Event
/// </summary>

/// <summary>
/// Fired every frame when a stock's price is updated by the PriceGenerator.
/// </summary>
public struct PriceUpdatedEvent
{
    public int StockId;
    public float NewPrice;
    public float PreviousPrice;
    public float DeltaTime;
}

/// <summary>
/// Fired when a market event starts affecting stock prices.
/// AffectedStockIds is null for global events (all stocks affected).
/// </summary>
public struct MarketEventFiredEvent
{
    public MarketEventType EventType;
    public int[] AffectedStockIds;
    public float PriceEffectPercent;
}

/// <summary>
/// Fired when a market event expires and stops affecting stock prices.
/// AffectedStockIds is null for global events.
/// </summary>
public struct MarketEventEndedEvent
{
    public MarketEventType EventType;
    public int[] AffectedStockIds;
}

/// <summary>
/// Fired when a trade (buy/sell/short) is executed successfully.
/// </summary>
public struct TradeExecutedEvent
{
    public string StockId;
    public int Shares;
    public float Price;
    public bool IsBuy;
    public bool IsShort;
    public float TotalCost;
}

/// <summary>
/// Fired when a round ends (after auto-liquidation).
/// </summary>
public struct RoundEndedEvent
{
    public int RoundNumber;
    public float TotalProfit;
    public float FinalCash;
}

/// <summary>
/// Fired when a new run starts.
/// </summary>
public struct RunStartedEvent
{
    public float StartingCapital;
}

/// <summary>
/// Fired when the player selects a stock from the sidebar or via keyboard shortcut.
/// ChartRenderer subscribes to switch the displayed stock.
/// </summary>
public struct StockSelectedEvent
{
    public int StockId;
    public string TickerSymbol;
}

/// <summary>
/// Fired when the MarketOpen preview phase begins.
/// UI subscribes to display stock preview, headline, and profit target.
/// </summary>
public struct MarketOpenEvent
{
    public int RoundNumber;
    public int Act;
    public int[] StockIds;
    public string[] TickerSymbols;
    public float[] StartingPrices;
    public string[] TierNames;
    public float ProfitTarget;
    public string Headline;
}

/// <summary>
/// Fired when a trading round starts.
/// </summary>
public struct RoundStartedEvent
{
    public int RoundNumber;
    public int Act;
    public string TierDisplayName;
    public float MarginCallTarget;
    public float TimeLimit;
}

/// <summary>
/// Fired when the trading phase ends (timer expired or manual end).
/// </summary>
public struct TradingPhaseEndedEvent
{
    public int RoundNumber;
    public bool TimeExpired;
}

/// <summary>
/// Fired when the market closes and all positions are auto-liquidated.
/// Published by MarketCloseState after liquidation completes.
/// </summary>
public struct MarketClosedEvent
{
    public int RoundNumber;
    public float RoundProfit;
    public float FinalCash;
    public int PositionsLiquidated;
}

/// <summary>
/// Fired when the player fails to meet the margin call target for a round.
/// The run ends immediately after this event.
/// </summary>
public struct MarginCallTriggeredEvent
{
    public int RoundNumber;
    public float RoundProfit;
    public float RequiredTarget;
    public float Shortfall;
}

/// <summary>
/// Fired when a round is completed successfully (margin call passed).
/// UI subscribes to display round results overlay.
/// </summary>
public struct RoundCompletedEvent
{
    public int RoundNumber;
    public float RoundProfit;
    public float ProfitTarget;
    public bool TargetMet;
    public float TotalCash;
}

/// <summary>
/// Fired when an act transition occurs (new act begins).
/// UI subscribes to display act transition interstitial.
/// </summary>
public struct ActTransitionEvent
{
    public int NewAct;
    public int PreviousAct;
    public string TierDisplayName;
}

/// <summary>
/// Fired when a run is completed (win or loss). Contains full run statistics.
/// Audio system (Epic 11) subscribes for victory music.
/// Meta-progression (Epic 9) subscribes for reputation processing.
/// </summary>
public struct RunCompletedEvent
{
    public float TotalProfit;
    public float PeakCash;
    public int RoundsCompleted;
    public int ItemsCollected;
    public int ReputationEarned;
    public bool IsVictory;
}

/// <summary>
/// Fired when a run ends, either by margin call or completing all rounds.
/// </summary>
public struct RunEndedEvent
{
    public int RoundsCompleted;
    public float FinalCash;
    public float TotalProfit;
    public bool WasMarginCalled;
    public int ReputationEarned;
    public int ItemsCollected;
    public float PeakCash;
    public float BestRoundProfit;
}
