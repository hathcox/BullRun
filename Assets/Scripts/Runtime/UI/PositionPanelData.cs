using System.Collections.Generic;

/// <summary>
/// Pure data/logic layer for the positions panel.
/// Builds display entries from Portfolio's positions.
/// </summary>
public class PositionPanelData
{
    private readonly List<PositionDisplayEntry> _entries = new List<PositionDisplayEntry>();

    public int EntryCount => _entries.Count;
    public bool IsEmpty => _entries.Count == 0;

    public PositionDisplayEntry GetEntry(int index)
    {
        return _entries[index];
    }

    /// <summary>
    /// Rebuilds the display entry list from the portfolio's current positions.
    /// </summary>
    public void RefreshFromPortfolio(Portfolio portfolio)
    {
        _entries.Clear();
        foreach (var position in portfolio.GetAllPositions())
        {
            _entries.Add(new PositionDisplayEntry(
                position.StockId,
                position.Shares,
                position.AverageBuyPrice,
                position.IsLong
            ));
        }
    }

    /// <summary>
    /// Updates P&L for all entries using a price lookup function.
    /// </summary>
    public void UpdateAllPnL(System.Func<string, float> getPrice)
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            _entries[i].UpdatePnL(getPrice(_entries[i].StockId));
        }
    }
}
