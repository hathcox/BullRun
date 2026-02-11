/// <summary>
/// Data for a single stock entry in the sidebar.
/// Tracks current price, % change from round start, and sparkline buffer.
/// Pure C# for testability — MonoBehaviour wrapper handles visuals.
/// </summary>
public class StockEntry
{
    public const int SparklineCapacity = 20;

    public int StockId { get; private set; }
    public string TickerSymbol { get; private set; }
    public float StartPrice { get; private set; }
    public float CurrentPrice { get; private set; }
    public float PercentChange { get; private set; }
    public bool IsSelected { get; set; }
    public float SparklineMin { get; private set; }
    public float SparklineMax { get; private set; }

    private readonly float[] _sparklineBuffer = new float[SparklineCapacity];
    private int _sparklineHead;
    private int _sparklineCount;

    public int SparklinePointCount => _sparklineCount;

    public StockEntry(int stockId, string ticker, float startPrice)
    {
        StockId = stockId;
        TickerSymbol = ticker;
        StartPrice = startPrice;
        CurrentPrice = startPrice;
        PercentChange = 0f;
        _sparklineHead = 0;
        _sparklineCount = 0;
    }

    public void UpdatePrice(float newPrice)
    {
        CurrentPrice = newPrice;
        PercentChange = StartPrice > 0f ? (newPrice - StartPrice) / StartPrice : 0f;

        // Add to sparkline ring buffer
        _sparklineBuffer[_sparklineHead] = newPrice;
        _sparklineHead = (_sparklineHead + 1) % SparklineCapacity;
        if (_sparklineCount < SparklineCapacity)
            _sparklineCount++;

        // Cache min/max to avoid O(n) scan per frame
        RecalculateSparklineRange();
    }

    private void RecalculateSparklineRange()
    {
        if (_sparklineCount == 0) return;
        float min = float.MaxValue;
        float max = float.MinValue;
        for (int i = 0; i < _sparklineCount; i++)
        {
            float val = GetSparklinePoint(i);
            if (val < min) min = val;
            if (val > max) max = val;
        }
        SparklineMin = min;
        SparklineMax = max;
    }

    /// <summary>
    /// Gets sparkline point at logical index (0 = oldest visible point).
    /// </summary>
    public float GetSparklinePoint(int index)
    {
        int start = _sparklineCount < SparklineCapacity
            ? 0
            : _sparklineHead;
        int actualIndex = (start + index) % SparklineCapacity;
        return _sparklineBuffer[actualIndex];
    }
}
