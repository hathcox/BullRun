using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plain C# data class holding all AudioClip references organized by category.
/// Populated from AudioClipHolder at runtime by AudioSetup.
/// </summary>
public class AudioClipLibrary
{
    // Trading
    public AudioClip BuySuccess, SellProfit, SellLoss, TradeRejected;
    public AudioClip ShortOpen, ShortCashoutProfit, ShortCashoutLoss, ShortAutoClose;
    public AudioClip TradeCooldownStart, TradeCooldownEnd;

    // Timer
    public AudioClip TimerWarning15s, TimerCriticalTick;
    public AudioClip ShortCountdownTick, ShortCashoutWindowOpen, ShortCashoutUrgency;

    // Game State
    public AudioClip MarketOpenPreview, RoundStart, RoundCompleteSuccess, MarketClosed;
    public AudioClip MarginCall, RunVictory, RunDefeat, RunStart;

    // Act
    public AudioClip ActTransition, ActTitleReveal;

    // Market Events
    public AudioClip EventPopupAppear, EventPositive, EventNegative, EventExtreme;
    public AudioClip EventPopupDismissUp, EventPopupDismissDown;
    public AudioClip CrashRumbleLoop, BullrunShimmerLoop, FlashCrashImpact;

    // Shop
    public AudioClip ShopOpen, ShopClose, RelicPurchase, RelicHover;
    public AudioClip ExpansionPurchase, InsiderTipReveal, ShopReroll;
    public AudioClip BondPurchase, BondRepPayout, ShopCardCascadeIn;

    // Tokens/Feedback
    public AudioClip TokenLaunch, TokenLand, TokenBurst;
    public AudioClip ProfitPopup, LossPopup, RepEarned, StreakMilestone;

    // UI
    public AudioClip UiButtonHover, UiPanelOpen, UiPanelClose;
    public AudioClip UiTabSwitch, UiNavigate, UiConfirm, UiCancel, StockSelected;

    // Overlays
    public AudioClip MarketClosedStamp, MarginCallSlam;
    public AudioClip VictoryHeaderAppear, StatsCountUp, ResultsDismiss;

    // Lookup dictionary for graceful name-based access
    private Dictionary<string, AudioClip> _clipsByName;

    /// <summary>
    /// Builds the internal lookup dictionary from all fields.
    /// Call after all fields are populated.
    /// </summary>
    public void BuildLookup()
    {
        _clipsByName = new Dictionary<string, AudioClip>();
        var fields = GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        foreach (var field in fields)
        {
            if (field.FieldType != typeof(AudioClip)) continue;
            var clip = field.GetValue(this) as AudioClip;
            if (clip != null)
                _clipsByName[field.Name] = clip;
        }
    }

    /// <summary>
    /// Graceful lookup by field name. Returns null if clip not found (no exception).
    /// </summary>
    public AudioClip TryGetClip(string name)
    {
        if (_clipsByName != null && _clipsByName.TryGetValue(name, out var clip))
            return clip;
        return null;
    }

    /// <summary>
    /// Populates fields from an array of (name, clip) entries using snake_case → PascalCase conversion.
    /// Handles edge cases where file names don't match field names exactly.
    /// </summary>
    public void PopulateFromEntries(AudioClipHolder.AudioClipEntry[] entries)
    {
        if (entries == null) return;

        // Manual overrides for file names that don't PascalCase to field names
        var nameOverrides = new Dictionary<string, string>
        {
            { "short_cashout_window", "ShortCashoutWindowOpen" },
            { "event_popup_dismiss__down", "EventPopupDismissDown" }
        };

        var type = GetType();
        int loaded = 0;

        foreach (var entry in entries)
        {
            if (entry.Clip == null) continue;

            string fieldName;
            if (!nameOverrides.TryGetValue(entry.Name, out fieldName))
                fieldName = SnakeToPascal(entry.Name);

            var field = type.GetField(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (field != null && field.FieldType == typeof(AudioClip))
            {
                field.SetValue(this, entry.Clip);
                loaded++;
            }
            else
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[AudioClipLibrary] No field found for clip '{entry.Name}' (tried '{fieldName}')");
                #endif
            }
        }

        BuildLookup();

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[AudioClipLibrary] Populated {loaded} clips, {_clipsByName.Count} in lookup");
        #endif
    }

    private static string SnakeToPascal(string snake)
    {
        var parts = snake.Split('_');
        var sb = new System.Text.StringBuilder();
        foreach (var part in parts)
        {
            if (part.Length == 0) continue;
            // Handle numeric segments like "15s"
            sb.Append(char.ToUpper(part[0]));
            if (part.Length > 1)
                sb.Append(part.Substring(1));
        }
        return sb.ToString();
    }
}
