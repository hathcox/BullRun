using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages trade quantity selection (FIX-15: always 1 share per click, unlimited holdings).
/// Created by UISetup, read by GameRunner at trade time.
/// Resets to x1 on RoundStartedEvent.
/// </summary>
public class QuantitySelector : MonoBehaviour
{
    // Story 14.6: Color constants migrated to CRTThemeData
    public static readonly Color ActiveButtonColor = new Color(CRTThemeData.TextHigh.r * 0.5f, CRTThemeData.TextHigh.g * 0.5f, CRTThemeData.TextHigh.b * 0.5f, 1f);
    public static readonly Color InactiveButtonColor = CRTThemeData.Panel;

    // AC 16: Quantity punch animation constants
    public static readonly float QuantityPunchDuration = 0.15f;
    public static readonly float QuantityPunchStrength = 0.25f;

    private int _selectedQuantity;

    // AC 16: RectTransform of the quantity display text and last known quantity
    private RectTransform _quantityDisplayRect;
    private int _lastQuantity = -1;

    /// <summary>Current selected quantity value.</summary>
    public int SelectedQuantity => _selectedQuantity;

    /// <summary>Countdown timer Text displayed inside cooldown overlay.</summary>
    public Text CooldownTimerText { get; set; }

    /// <summary>Grey overlay GameObject that covers buttons during post-trade cooldown.</summary>
    public GameObject CooldownOverlay { get; set; }

    // Story 13.7: Leverage badge (shown when expansion active)
    public GameObject LeverageBadge { get; set; }

    // Story 13.7: Short 2 container (Dual Short expansion visibility)
    public GameObject Short2Container { get; set; }

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

    /// <summary>
    /// AC 16: Sets the RectTransform of the quantity display text. Called by UISetup.
    /// </summary>
    public void SetDisplayRect(RectTransform rect)
    {
        _quantityDisplayRect = rect;
    }

    /// <summary>
    /// AC 16: Triggers quantity punch animation if value actually changed.
    /// Called from UISetup's +/- button click callbacks.
    /// </summary>
    public void OnQuantityChanged(int newQuantity)
    {
        if (newQuantity == _lastQuantity) return;
        _lastQuantity = newQuantity;

        if (_quantityDisplayRect != null)
        {
            _quantityDisplayRect.DOKill();
            _quantityDisplayRect.localScale = Vector3.one;
            _quantityDisplayRect.DOPunchScale(Vector3.one * QuantityPunchStrength, QuantityPunchDuration, 1, 0.5f)
                .SetUpdate(false);
        }
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
