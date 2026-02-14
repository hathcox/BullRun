using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Compact position overlay anchored to the bottom-left of the chart area.
/// Shows current position info: shares, direction (LONG/SHORT/FLAT), avg price, P&L.
/// Read-only — never modifies positions, only reads for display.
/// Replaces the old PositionPanel right sidebar (FIX-7).
/// </summary>
public class PositionOverlay : MonoBehaviour
{
    public static readonly Color LongColor = new Color(0f, 1f, 0.533f, 1f);   // #00FF88 neon green
    public static readonly Color ShortColor = new Color(1f, 0.4f, 0.7f, 1f);  // #FF66B3 hot pink
    public static readonly Color FlatColor = new Color(0.5f, 0.5f, 0.55f, 1f); // Gray
    public static readonly Color ProfitGreen = new Color(0f, 1f, 0.533f, 1f);
    public static readonly Color LossRed = new Color(1f, 0.2f, 0.2f, 1f);

    private Portfolio _portfolio;
    private Text _directionText;  // "15x LONG" or "FLAT"
    private Text _avgPriceText;   // "Avg: $2.45"
    private Text _pnlText;        // "P&L: +$3.75"
    private GameObject _avgPriceRow;
    private GameObject _pnlRow;

    private string _activeStockId;
    private int _activeStockIdInt = -1;
    private float _lastKnownPrice;
    private bool _pnlDirty;
    private bool _rebuildDirty;

    public void Initialize(Portfolio portfolio, Text directionText, Text avgPriceText, Text pnlText,
        GameObject avgPriceRow, GameObject pnlRow)
    {
        _portfolio = portfolio;
        _directionText = directionText;
        _avgPriceText = avgPriceText;
        _pnlText = pnlText;
        _avgPriceRow = avgPriceRow;
        _pnlRow = pnlRow;

        EventBus.Subscribe<PriceUpdatedEvent>(OnPriceUpdated);
        EventBus.Subscribe<TradeExecutedEvent>(OnTradeExecuted);
        EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);

        RefreshDisplay();
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<PriceUpdatedEvent>(OnPriceUpdated);
        EventBus.Unsubscribe<TradeExecutedEvent>(OnTradeExecuted);
        EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
    }

    private void OnPriceUpdated(PriceUpdatedEvent evt)
    {
        if (_activeStockIdInt < 0) return;
        if (evt.StockId == _activeStockIdInt)
        {
            _lastKnownPrice = evt.NewPrice;
            _pnlDirty = true;
        }
    }

    private void OnTradeExecuted(TradeExecutedEvent evt)
    {
        _rebuildDirty = true;
    }

    private void OnRoundStarted(RoundStartedEvent evt)
    {
        _rebuildDirty = true;
    }

    private void LateUpdate()
    {
        if (_rebuildDirty)
        {
            _rebuildDirty = false;
            _pnlDirty = false;
            RefreshDisplay();
        }
        else if (_pnlDirty)
        {
            _pnlDirty = false;
            UpdatePnL();
        }
    }

    /// <summary>
    /// Sets which stock the overlay tracks. Called when the active stock changes.
    /// Accepts int StockId to avoid per-tick string allocations in OnPriceUpdated.
    /// </summary>
    public void SetActiveStock(int stockId)
    {
        _activeStockIdInt = stockId;
        _activeStockId = stockId.ToString();
        _rebuildDirty = true;
    }

    /// <summary>
    /// Full rebuild of the display from portfolio state.
    /// </summary>
    public void RefreshDisplay()
    {
        if (_portfolio == null) return;

        var position = _activeStockId != null ? _portfolio.GetPosition(_activeStockId) : null;

        if (position == null || position.Shares <= 0)
        {
            ShowFlat();
            return;
        }

        float pnl = position.UnrealizedPnL(_lastKnownPrice);
        ShowPosition(position.Shares, position.IsLong, position.AverageBuyPrice, pnl);
    }

    private void ShowFlat()
    {
        if (_directionText != null)
        {
            _directionText.text = "FLAT";
            _directionText.color = FlatColor;
        }
        if (_avgPriceRow != null)
            _avgPriceRow.SetActive(false);
        if (_pnlRow != null)
            _pnlRow.SetActive(false);
    }

    private void ShowPosition(int shares, bool isLong, float avgPrice, float pnl)
    {
        if (_directionText != null)
        {
            _directionText.text = FormatDirection(shares, isLong);
            _directionText.color = isLong ? LongColor : ShortColor;
        }
        if (_avgPriceText != null)
        {
            _avgPriceText.text = $"Avg: {TradingHUD.FormatCurrency(avgPrice)}";
        }
        if (_pnlText != null)
        {
            _pnlText.text = $"P&L: {TradingHUD.FormatProfit(pnl)}";
            _pnlText.color = GetPnLColor(pnl);
        }
        if (_avgPriceRow != null)
            _avgPriceRow.SetActive(true);
        if (_pnlRow != null)
            _pnlRow.SetActive(true);
    }

    private void UpdatePnL()
    {
        if (_portfolio == null || _activeStockId == null) return;

        var position = _portfolio.GetPosition(_activeStockId);
        if (position == null || position.Shares <= 0)
        {
            ShowFlat();
            return;
        }

        float pnl = position.UnrealizedPnL(_lastKnownPrice);
        if (_pnlText != null)
        {
            _pnlText.text = $"P&L: {TradingHUD.FormatProfit(pnl)}";
            _pnlText.color = GetPnLColor(pnl);
        }
    }

    // --- Static utility methods for testability ---

    public static Color GetPnLColor(float pnl)
    {
        if (pnl > 0f) return ProfitGreen;
        if (pnl < 0f) return LossRed;
        return Color.white;
    }

    public static string FormatDirection(int shares, bool isLong)
    {
        return $"{shares}x {(isLong ? "LONG" : "SHORT")}";
    }

    public static string FormatFlat()
    {
        return "FLAT";
    }
}
