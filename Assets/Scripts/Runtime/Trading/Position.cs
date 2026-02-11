/// <summary>
/// Represents an open trading position on a stock.
/// Tracks shares held, entry price, and calculates P&L.
/// Supports both long and short positions.
/// </summary>
public class Position
{
    public string StockId { get; private set; }
    public int Shares { get; private set; }
    public float AverageBuyPrice { get; private set; }
    public bool IsLong { get; private set; }
    public bool IsShort { get; private set; }
    public float MarginHeld { get; private set; }
    public float OpenTime { get; private set; }

    public Position(string stockId, int shares, float averageBuyPrice)
    {
        StockId = stockId;
        Shares = shares;
        AverageBuyPrice = averageBuyPrice;
        IsLong = true;
        IsShort = false;
        MarginHeld = 0f;
        OpenTime = UnityEngine.Time.time;
    }

    public Position(string stockId, int shares, float entryPrice, float marginHeld)
    {
        StockId = stockId;
        Shares = shares;
        AverageBuyPrice = entryPrice;
        IsLong = false;
        IsShort = true;
        MarginHeld = marginHeld;
        OpenTime = UnityEngine.Time.time;
    }

    /// <summary>
    /// Calculates unrealized profit/loss at the given current price.
    /// Long: (currentPrice - entryPrice) * shares
    /// Short: (entryPrice - currentPrice) * shares
    /// </summary>
    public float UnrealizedPnL(float currentPrice)
    {
        if (IsShort)
            return (AverageBuyPrice - currentPrice) * Shares;
        return (currentPrice - AverageBuyPrice) * Shares;
    }

    /// <summary>
    /// Calculates current market value of the position.
    /// </summary>
    public float MarketValue(float currentPrice)
    {
        return currentPrice * Shares;
    }

    /// <summary>
    /// Calculates realized P&L for selling/covering a given number of shares.
    /// Long: (sellPrice - entryPrice) * sharesSold
    /// Short: (entryPrice - coverPrice) * sharesSold
    /// </summary>
    public float CalculateRealizedPnL(float sellPrice, int sharesSold)
    {
        if (IsShort)
            return (AverageBuyPrice - sellPrice) * sharesSold;
        return (sellPrice - AverageBuyPrice) * sharesSold;
    }
}
