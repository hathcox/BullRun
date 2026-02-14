using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages trade quantity selection. Preset buttons (1x, 5x, 10x, MAX)
/// with real-time MAX calculation. Created by UISetup, read by GameRunner at trade time.
/// Persists selection within a round; resets to 10x on RoundStartedEvent.
/// </summary>
public class QuantitySelector : MonoBehaviour
{
    public enum Preset { One, Five, Ten, Max }

    public static readonly int[] PresetValues = { 1, 5, 10, 0 };
    public static readonly string[] PresetLabels = { "1x", "5x", "10x", "MAX" };
    public static readonly Color ActiveButtonColor = new Color(0f, 0.5f, 0.25f, 1f);
    public static readonly Color InactiveButtonColor = new Color(0.12f, 0.14f, 0.25f, 0.8f);

    private Preset _selectedPreset = Preset.Ten;
    private Text _quantityDisplayText;
    private Image[] _buttonBackgrounds;
    private Text[] _buttonTexts;
    private Portfolio _portfolio;
    private Func<int> _getSelectedStockId;
    private Func<int, float> _getStockPrice;

    /// <summary>Current selected preset.</summary>
    public Preset SelectedPreset => _selectedPreset;

    public void Initialize(Text quantityDisplayText, Image[] buttonBackgrounds, Text[] buttonTexts)
    {
        _quantityDisplayText = quantityDisplayText;
        _buttonBackgrounds = buttonBackgrounds;
        _buttonTexts = buttonTexts;
        SelectPreset(Preset.Ten);
        EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
    }

    /// <summary>
    /// Sets data sources for MAX calculation and real-time display.
    /// </summary>
    public void SetDataSources(Portfolio portfolio, Func<int> getSelectedStockId, Func<int, float> getStockPrice)
    {
        _portfolio = portfolio;
        _getSelectedStockId = getSelectedStockId;
        _getStockPrice = getStockPrice;
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
    }

    private void OnRoundStarted(RoundStartedEvent evt)
    {
        ResetToDefault();
    }

    /// <summary>Select a specific quantity preset and update UI.</summary>
    public void SelectPreset(Preset preset)
    {
        _selectedPreset = preset;
        UpdateButtonHighlights();
        UpdateQuantityDisplay();
    }

    /// <summary>Cycle to next preset: 1 -> 5 -> 10 -> MAX -> 1...</summary>
    public void CyclePreset()
    {
        int next = ((int)_selectedPreset + 1) % 4;
        SelectPreset((Preset)next);
    }

    /// <summary>Reset to default (10x).</summary>
    public void ResetToDefault()
    {
        SelectPreset(Preset.Ten);
    }

    private void Update()
    {
        // Recalculate MAX display every frame when MAX is selected (AC 7)
        if (_selectedPreset == Preset.Max && TradingState.IsActive)
            UpdateQuantityDisplay();
    }

    private void UpdateButtonHighlights()
    {
        if (_buttonBackgrounds == null) return;
        for (int i = 0; i < _buttonBackgrounds.Length; i++)
        {
            bool active = i == (int)_selectedPreset;
            _buttonBackgrounds[i].color = active ? ActiveButtonColor : InactiveButtonColor;
            if (_buttonTexts != null && i < _buttonTexts.Length)
                _buttonTexts[i].color = active ? Color.white : new Color(0.6f, 0.6f, 0.7f, 1f);
        }
    }

    private void UpdateQuantityDisplay()
    {
        if (_quantityDisplayText == null) return;
        if (_selectedPreset == Preset.Max)
        {
            int maxQty = GetMaxBuyQuantity();
            _quantityDisplayText.text = $"Qty: MAX ({maxQty})";
        }
        else
        {
            _quantityDisplayText.text = $"Qty: {PresetValues[(int)_selectedPreset]}";
        }
    }

    private int GetMaxBuyQuantity()
    {
        if (_portfolio == null || _getSelectedStockId == null || _getStockPrice == null)
            return 0;
        int stockId = _getSelectedStockId();
        if (stockId < 0) return 0;
        float price = _getStockPrice(stockId);
        return CalculateMaxBuy(_portfolio.Cash, price);
    }

    // --- Static calculation methods (testable without MonoBehaviour) ---

    /// <summary>Maximum shares affordable for a buy: floor(cash / price).</summary>
    public static int CalculateMaxBuy(float cash, float price)
    {
        if (price <= 0f) return 0;
        return Mathf.FloorToInt(cash / price);
    }

    /// <summary>Maximum shares affordable for a short: floor(cash / (price * marginReq)).</summary>
    public static int CalculateMaxShort(float cash, float price)
    {
        if (price <= 0f) return 0;
        return Mathf.FloorToInt(cash / (price * GameConfig.ShortMarginRequirement));
    }

    /// <summary>Maximum shares sellable: all held long shares.</summary>
    public static int CalculateMaxSell(Portfolio portfolio, string stockId)
    {
        var pos = portfolio.GetPosition(stockId);
        if (pos == null || pos.IsShort) return 0;
        return pos.Shares;
    }

    /// <summary>Maximum shares coverable: all held short shares.</summary>
    public static int CalculateMaxCover(Portfolio portfolio, string stockId)
    {
        var pos = portfolio.GetPosition(stockId);
        if (pos == null || !pos.IsShort) return 0;
        return pos.Shares;
    }

    /// <summary>
    /// Returns the resolved quantity for a specific trade action.
    /// For MAX: calculates max for the specific trade type.
    /// For non-MAX: returns preset value, clamped to affordable/available amount (partial fill).
    /// Returns 0 when nothing is affordable/available.
    /// </summary>
    public int GetCurrentQuantity(bool isBuy, bool isShort, string stockId, float price, Portfolio portfolio)
    {
        if (_selectedPreset == Preset.Max)
        {
            if (isBuy && !isShort) return CalculateMaxBuy(portfolio.Cash, price);
            if (!isBuy && !isShort) return CalculateMaxSell(portfolio, stockId);
            if (!isBuy && isShort) return CalculateMaxShort(portfolio.Cash, price);
            return CalculateMaxCover(portfolio, stockId);
        }

        int qty = PresetValues[(int)_selectedPreset];

        // Partial fill: clamp to what's affordable/available
        if (isBuy && !isShort)
        {
            int max = CalculateMaxBuy(portfolio.Cash, price);
            if (qty > max) qty = max;
        }
        else if (!isBuy && !isShort)
        {
            int max = CalculateMaxSell(portfolio, stockId);
            if (qty > max) qty = max;
        }
        else if (!isBuy && isShort)
        {
            int max = CalculateMaxShort(portfolio.Cash, price);
            if (qty > max) qty = max;
        }
        else
        {
            int max = CalculateMaxCover(portfolio, stockId);
            if (qty > max) qty = max;
        }

        return qty;
    }
}
