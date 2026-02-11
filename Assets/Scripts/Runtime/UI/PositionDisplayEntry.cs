/// <summary>
/// Display data for a single position entry in the positions panel.
/// Pure C# for testability. Calculated from Portfolio's Position data.
/// </summary>
public class PositionDisplayEntry
{
    public string StockId { get; private set; }
    public int Shares { get; private set; }
    public float AveragePrice { get; private set; }
    public bool IsLong { get; private set; }
    public bool IsShort => !IsLong;
    public float UnrealizedPnL { get; private set; }

    public PositionDisplayEntry(string stockId, int shares, float averagePrice, bool isLong)
    {
        StockId = stockId;
        Shares = shares;
        AveragePrice = averagePrice;
        IsLong = isLong;
        UnrealizedPnL = 0f;
    }

    /// <summary>
    /// Recalculates unrealized P&L at the given current price.
    /// </summary>
    public void UpdatePnL(float currentPrice)
    {
        if (IsLong)
            UnrealizedPnL = (currentPrice - AveragePrice) * Shares;
        else
            UnrealizedPnL = (AveragePrice - currentPrice) * Shares;
    }
}
