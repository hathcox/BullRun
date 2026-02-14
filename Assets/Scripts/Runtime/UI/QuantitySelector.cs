using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages trade quantity selection. Preset buttons (x5, x10, x15, x25).
/// Created by UISetup, read by GameRunner at trade time.
/// Persists selection within a round; resets to x10 on RoundStartedEvent.
/// </summary>
public class QuantitySelector : MonoBehaviour
{
    public enum Preset { Five, Ten, Fifteen, TwentyFive }

    public static readonly int[] PresetValues = { 5, GameConfig.DefaultTradeQuantity, 15, 25 };
    public static readonly string[] PresetLabels = { "x5", "x10", "x15", "x25" };
    public static readonly Color ActiveButtonColor = new Color(0f, 0.5f, 0.25f, 1f);
    public static readonly Color InactiveButtonColor = new Color(0.12f, 0.14f, 0.25f, 0.8f);

    private Preset _selectedPreset = Preset.Ten;
    private Text _quantityDisplayText;
    private Image[] _buttonBackgrounds;
    private Text[] _buttonTexts;

    /// <summary>Current selected preset.</summary>
    public Preset SelectedPreset => _selectedPreset;

    /// <summary>BUY button Image reference for cooldown visual feedback.</summary>
    public Image BuyButtonImage { get; set; }

    /// <summary>SELL button Image reference for cooldown visual feedback.</summary>
    public Image SellButtonImage { get; set; }

    public void Initialize(Text quantityDisplayText, Image[] buttonBackgrounds, Text[] buttonTexts)
    {
        _quantityDisplayText = quantityDisplayText;
        _buttonBackgrounds = buttonBackgrounds;
        _buttonTexts = buttonTexts;
        SelectPreset(Preset.Ten);
        EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
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

    /// <summary>Reset to default (x10).</summary>
    public void ResetToDefault()
    {
        SelectPreset(Preset.Ten);
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
        _quantityDisplayText.text = $"Qty: {PresetValues[(int)_selectedPreset]}";
    }

    // --- Static calculation methods (testable without MonoBehaviour) ---

    /// <summary>
    /// Routes MAX calculation to the correct method based on trade type.
    /// Buy: cash/price. Short: cash/(price*margin). Sell: position shares. Cover: short shares.
    /// </summary>
    public static int CalculateMax(bool isBuy, bool isShort, float cash, float price, Portfolio portfolio, string stockId)
    {
        if (isBuy && !isShort) return CalculateMaxBuy(cash, price);
        if (!isBuy && !isShort) return CalculateMaxSell(portfolio, stockId);
        if (!isBuy && isShort) return CalculateMaxShort(cash, price);
        return CalculateMaxCover(portfolio, stockId);
    }

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
    /// Returns preset value, clamped to affordable/available amount (partial fill).
    /// Returns 0 when nothing is affordable/available.
    /// </summary>
    public int GetCurrentQuantity(bool isBuy, bool isShort, string stockId, float price, Portfolio portfolio)
    {
        int max = CalculateMax(isBuy, isShort, portfolio.Cash, price, portfolio, stockId);
        int qty = PresetValues[(int)_selectedPreset];
        // Partial fill: clamp to what's affordable/available
        if (qty > max) qty = max;
        return qty;
    }
}
