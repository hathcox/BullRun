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
