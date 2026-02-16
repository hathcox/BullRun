using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages trade quantity selection (FIX-15: always 1 share per click, unlimited holdings).
/// Created by UISetup, read by GameRunner at trade time.
/// Resets to x1 on RoundStartedEvent.
/// </summary>
public class QuantitySelector : MonoBehaviour
{
    public static readonly Color ActiveButtonColor = new Color(0f, 0.5f, 0.25f, 1f);
    public static readonly Color InactiveButtonColor = new Color(0.12f, 0.14f, 0.25f, 0.8f);

    private int _selectedQuantity;

    /// <summary>Current selected quantity value.</summary>
    public int SelectedQuantity => _selectedQuantity;

    /// <summary>Countdown timer Text displayed inside cooldown overlay.</summary>
    public Text CooldownTimerText { get; set; }

    /// <summary>Grey overlay GameObject that covers buttons during post-trade cooldown.</summary>
    public GameObject CooldownOverlay { get; set; }

    // Short UI references (integrated into trade panel)
    public Image ShortButtonImage { get; set; }
    public Text ShortButtonText { get; set; }
    public GameObject ShortPnlPanel { get; set; }
    public Text ShortPnlEntryText { get; set; }
    public Text ShortPnlValueText { get; set; }
    public Text ShortPnlCountdownText { get; set; }

    public void Initialize()
    {
        _selectedQuantity = GameConfig.DefaultTradeQuantity; // x1
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

    /// <summary>Reset to x1 default on round start.</summary>
    public void ResetToDefault()
    {
        _selectedQuantity = GameConfig.DefaultTradeQuantity; // x1
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

    /// <summary>Maximum shares for a short: always ShortBaseShares (no capital required).</summary>
    public static int CalculateMaxShort(float cash, float price)
    {
        if (price <= 0f) return 0;
        return GameConfig.ShortBaseShares;
    }

    /// <summary>Maximum shares sellable: all held long shares.</summary>
    public static int CalculateMaxSell(Portfolio portfolio, string stockId)
    {
        var pos = portfolio.GetPosition(stockId);
        if (pos == null) return 0;
        return pos.Shares;
    }

    /// <summary>Maximum shares coverable: all held short shares.</summary>
    public static int CalculateMaxCover(Portfolio portfolio, string stockId)
    {
        var pos = portfolio.GetShortPosition(stockId);
        if (pos == null) return 0;
        return pos.Shares;
    }

    /// <summary>
    /// Returns the resolved quantity for a specific trade action.
    /// FIX-15: Always 1 share, clamped to affordable/available amount.
    /// Returns 0 when nothing is affordable/available.
    /// </summary>
    public int GetCurrentQuantity(bool isBuy, bool isShort, string stockId, float price, Portfolio portfolio)
    {
        int max = CalculateMax(isBuy, isShort, portfolio.Cash, price, portfolio, stockId);
        int qty = _selectedQuantity;
        // Partial fill: clamp to what's affordable/available
        if (qty > max) qty = max;
        return qty;
    }
}
