using System.Collections.Generic;

/// <summary>
/// Pure data/logic layer for the stock sidebar.
/// Manages stock entries, selection state, and price updates.
/// Publishes StockSelectedEvent when selection changes.
/// </summary>
public class StockSidebarData
{
    private readonly List<StockEntry> _entries = new List<StockEntry>();
    private readonly Dictionary<int, int> _stockIdToIndex = new Dictionary<int, int>();
    private int _selectedIndex = -1;

    public int EntryCount => _entries.Count;
    public int SelectedIndex => _selectedIndex;

    public StockEntry GetEntry(int index)
    {
        return _entries[index];
    }

    /// <summary>
    /// Builds sidebar entries from the round's active stocks.
    /// Selects the first stock by default and publishes StockSelectedEvent.
    /// </summary>
    public void InitializeForRound(List<StockInstance> stocks)
    {
        _entries.Clear();
        _stockIdToIndex.Clear();
        _selectedIndex = -1;

        for (int i = 0; i < stocks.Count; i++)
        {
            var stock = stocks[i];
            var entry = new StockEntry(stock.StockId, stock.TickerSymbol, stock.CurrentPrice);
            _entries.Add(entry);
            _stockIdToIndex[stock.StockId] = i;
        }

        if (_entries.Count > 0)
        {
            SelectStock(0);
        }
    }

    /// <summary>
    /// Selects a stock by sidebar index (0-based). Publishes StockSelectedEvent.
    /// Out-of-range indices are ignored.
    /// </summary>
    public void SelectStock(int index)
    {
        if (index < 0 || index >= _entries.Count) return;

        // Deselect current
        if (_selectedIndex >= 0 && _selectedIndex < _entries.Count)
            _entries[_selectedIndex].IsSelected = false;

        _selectedIndex = index;
        _entries[index].IsSelected = true;

        EventBus.Publish(new StockSelectedEvent
        {
            StockId = _entries[index].StockId,
            TickerSymbol = _entries[index].TickerSymbol
        });
    }

    /// <summary>
    /// Updates the entry matching the event's stock ID with the new price.
    /// </summary>
    public void ProcessPriceUpdate(PriceUpdatedEvent evt)
    {
        if (_stockIdToIndex.TryGetValue(evt.StockId, out int index))
        {
            _entries[index].UpdatePrice(evt.NewPrice);
        }
    }
}
