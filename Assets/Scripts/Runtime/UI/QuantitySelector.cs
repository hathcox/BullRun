using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Manages trade quantity selection with unlock-based tiers (FIX-13).
/// Starts at x1 (no preset buttons). Higher tiers (x5, x10, x15, x25) unlocked via Reputation shop.
/// Created by UISetup, read by GameRunner at trade time.
/// Resets to x1 on RoundStartedEvent. Unlocks persist for the run.
/// </summary>
public class QuantitySelector : MonoBehaviour
{
    public static readonly Color ActiveButtonColor = new Color(0f, 0.5f, 0.25f, 1f);
    public static readonly Color InactiveButtonColor = new Color(0.12f, 0.14f, 0.25f, 0.8f);

    // Tier 0 = x1 (default, always unlocked). Tiers 1-4 = x5, x10, x15, x25.
    private int _unlockedTierIndex = 0;
    private int _selectedQuantity;
    private int _selectedPresetTierIndex = -1; // -1 means no preset selected (using default x1)
    private Text _quantityDisplayText;

    // Dynamic preset button arrays — grow as tiers are unlocked
    private List<Image> _buttonBackgrounds = new List<Image>();
    private List<Text> _buttonTexts = new List<Text>();

    /// <summary>Current selected quantity value.</summary>
    public int SelectedQuantity => _selectedQuantity;

    /// <summary>Index of the highest unlocked tier (0 = x1 only, 4 = all unlocked).</summary>
    public int UnlockedTierIndex => _unlockedTierIndex;

    /// <summary>Highest unlocked quantity value.</summary>
    public int HighestUnlockedQuantity => GameConfig.QuantityTiers[_unlockedTierIndex].Value;

    /// <summary>BUY button Image reference for cooldown visual feedback.</summary>
    public Image BuyButtonImage { get; set; }

    /// <summary>SELL button Image reference for cooldown visual feedback.</summary>
    public Image SellButtonImage { get; set; }

    /// <summary>Countdown timer Text displayed above buttons during post-trade cooldown.</summary>
    public Text CooldownTimerText { get; set; }

    /// <summary>Callback invoked when a tier is unlocked, so UISetup can add a button.</summary>
    public System.Action<int> OnTierUnlocked { get; set; }

    public void Initialize(Text quantityDisplayText)
    {
        _quantityDisplayText = quantityDisplayText;
        _unlockedTierIndex = 0;
        _selectedQuantity = GameConfig.DefaultTradeQuantity; // x1
        _selectedPresetTierIndex = -1;
        UpdateQuantityDisplay();
        EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
        EventBus.Subscribe<QuantityTierUnlockedEvent>(OnQuantityTierUnlocked);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
        EventBus.Unsubscribe<QuantityTierUnlockedEvent>(OnQuantityTierUnlocked);
    }

    private void OnQuantityTierUnlocked(QuantityTierUnlockedEvent evt)
    {
        UnlockTier(evt.TierIndex);
    }

    private void OnRoundStarted(RoundStartedEvent evt)
    {
        ResetToDefault();
    }

    /// <summary>
    /// Unlocks the next quantity tier. Called when player buys the upgrade in shop.
    /// Returns true if a new tier was unlocked, false if already at max.
    /// </summary>
    public bool UnlockNextTier()
    {
        int nextTier = _unlockedTierIndex + 1;
        if (nextTier >= GameConfig.QuantityTiers.Length) return false;
        return UnlockTier(nextTier);
    }

    /// <summary>
    /// Unlocks a specific tier index. Tiers must be unlocked sequentially.
    /// </summary>
    public bool UnlockTier(int tierIndex)
    {
        if (tierIndex <= _unlockedTierIndex) return false;
        if (tierIndex >= GameConfig.QuantityTiers.Length) return false;
        if (tierIndex != _unlockedTierIndex + 1) return false; // Sequential only

        _unlockedTierIndex = tierIndex;
        OnTierUnlocked?.Invoke(tierIndex);

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[QuantitySelector] Tier {tierIndex} unlocked: x{GameConfig.QuantityTiers[tierIndex].Value}");
        #endif

        return true;
    }

    /// <summary>
    /// Selects a preset by tier index (1=x5, 2=x10, 3=x15, 4=x25).
    /// Rejects if tier is not unlocked.
    /// </summary>
    public bool SelectPresetByTier(int tierIndex)
    {
        if (tierIndex < 1 || tierIndex >= GameConfig.QuantityTiers.Length) return false;
        if (tierIndex > _unlockedTierIndex) return false;

        _selectedPresetTierIndex = tierIndex;
        _selectedQuantity = GameConfig.QuantityTiers[tierIndex].Value;
        UpdateButtonHighlights();
        UpdateQuantityDisplay();
        return true;
    }

    /// <summary>Reset to x1 default on round start.</summary>
    public void ResetToDefault()
    {
        _selectedPresetTierIndex = -1;
        _selectedQuantity = GameConfig.DefaultTradeQuantity; // x1
        UpdateButtonHighlights();
        UpdateQuantityDisplay();
    }

    /// <summary>Returns the list of unlocked preset values (tiers 1+ only, not the x1 default).</summary>
    public int[] GetUnlockedPresets()
    {
        if (_unlockedTierIndex < 1) return new int[0];
        var result = new int[_unlockedTierIndex];
        for (int i = 1; i <= _unlockedTierIndex; i++)
            result[i - 1] = GameConfig.QuantityTiers[i].Value;
        return result;
    }

    /// <summary>Checks if a tier index is unlocked.</summary>
    public bool IsTierUnlocked(int tierIndex)
    {
        return tierIndex >= 0 && tierIndex <= _unlockedTierIndex;
    }

    /// <summary>Registers a preset button (called by UISetup when dynamically adding buttons).</summary>
    public void RegisterPresetButton(Image background, Text text)
    {
        _buttonBackgrounds.Add(background);
        _buttonTexts.Add(text);
    }

    private void UpdateButtonHighlights()
    {
        for (int i = 0; i < _buttonBackgrounds.Count; i++)
        {
            // Button index i corresponds to tier index (i + 1)
            bool active = _selectedPresetTierIndex == (i + 1);
            _buttonBackgrounds[i].color = active ? ActiveButtonColor : InactiveButtonColor;
            if (i < _buttonTexts.Count)
                _buttonTexts[i].color = active ? Color.white : new Color(0.6f, 0.6f, 0.7f, 1f);
        }
    }

    private void UpdateQuantityDisplay()
    {
        if (_quantityDisplayText == null) return;
        _quantityDisplayText.text = $"Qty: {_selectedQuantity}";
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
    /// FIX-13: Uses selected quantity (x1 default or unlocked preset), clamped to
    /// affordable/available amount AND highest unlocked tier value.
    /// Returns 0 when nothing is affordable/available.
    /// </summary>
    public int GetCurrentQuantity(bool isBuy, bool isShort, string stockId, float price, Portfolio portfolio)
    {
        int max = CalculateMax(isBuy, isShort, portfolio.Cash, price, portfolio, stockId);
        int qty = _selectedQuantity;
        // Clamp to highest unlocked tier value
        int highestUnlocked = HighestUnlockedQuantity;
        if (qty > highestUnlocked) qty = highestUnlocked;
        // Partial fill: clamp to what's affordable/available
        if (qty > max) qty = max;
        return qty;
    }
}
