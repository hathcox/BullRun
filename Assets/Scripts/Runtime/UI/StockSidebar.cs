using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// MonoBehaviour for the stock selection sidebar.
/// Manages visual updates, click handling, and keyboard shortcuts.
/// Pure logic delegated to StockSidebarData.
/// </summary>
public class StockSidebar : MonoBehaviour
{
    private StockSidebarData _data;
    private StockEntryView[] _entryViews;
    private bool _dirty;

    // Story 14.6: Color constants migrated to CRTThemeData
    private static readonly Color DefaultSelectedBgColor = new Color(CRTThemeData.Panel.r * 1.5f, CRTThemeData.Panel.g * 1.5f, CRTThemeData.Panel.b * 1.5f, 0.9f);
    private static readonly Color DefaultNormalBgColor = new Color(CRTThemeData.Panel.r, CRTThemeData.Panel.g, CRTThemeData.Panel.b, 0.6f);
    private static Color ProfitGreen => CRTThemeData.TextHigh;
    private static Color LossRed => CRTThemeData.Danger;

    // Event indicator colors
    public static readonly Color VolumeIconColor = new Color(1f, 0.533f, 0f, 1f);    // #FF8800 orange
    public static Color WarningIconColor => CRTThemeData.Warning;
    public static readonly Color SectorWinColor = new Color(CRTThemeData.TextHigh.r, CRTThemeData.TextHigh.g, CRTThemeData.TextHigh.b, 0.4f);
    public static readonly Color SectorLoseColor = new Color(CRTThemeData.Danger.r, CRTThemeData.Danger.g, CRTThemeData.Danger.b, 0.4f);
    public static readonly float VolumePulseFrequency = 4f;

    // Tier-themed colors (defaults to standard colors)
    private Color _selectedBgColor = DefaultSelectedBgColor;
    private Color _normalBgColor = DefaultNormalBgColor;
    private Image _sidebarBackground;

    // Active event indicators per stock (keyed by stock id)
    private Dictionary<int, ActiveIndicator> _activeIndicators = new Dictionary<int, ActiveIndicator>();
    private float _pulseTimer;

    public StockSidebarData Data => _data;

    public void Initialize(StockSidebarData data, StockEntryView[] entryViews)
    {
        _data = data;
        _entryViews = entryViews;

        EventBus.Subscribe<PriceUpdatedEvent>(OnPriceUpdated);
        EventBus.Subscribe<ActTransitionEvent>(OnActTransition);
        EventBus.Subscribe<MarketEventFiredEvent>(OnMarketEventFired);
        EventBus.Subscribe<MarketEventEndedEvent>(OnMarketEventEnded);
    }

    /// <summary>
    /// Sets the sidebar panel background image reference for tier theme tinting.
    /// Called by UISetup after creating the sidebar.
    /// </summary>
    public void SetSidebarBackground(Image sidebarBackground)
    {
        _sidebarBackground = sidebarBackground;
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<PriceUpdatedEvent>(OnPriceUpdated);
        EventBus.Unsubscribe<ActTransitionEvent>(OnActTransition);
        EventBus.Unsubscribe<MarketEventFiredEvent>(OnMarketEventFired);
        EventBus.Unsubscribe<MarketEventEndedEvent>(OnMarketEventEnded);
    }

    private void OnActTransition(ActTransitionEvent evt)
    {
        var theme = TierVisualData.GetThemeForAct(evt.NewAct);
        ApplyTierTheme(theme);
    }

    /// <summary>
    /// Applies tier visual theme colors to sidebar elements.
    /// Updates background tint and entry selection accent colors.
    /// </summary>
    public void ApplyTierTheme(TierVisualTheme theme)
    {
        _normalBgColor = new Color(theme.BackgroundTint.r, theme.BackgroundTint.g,
            theme.BackgroundTint.b, 0.6f);
        _selectedBgColor = new Color(
            theme.AccentColor.r * 0.3f, theme.AccentColor.g * 0.3f,
            theme.AccentColor.b * 0.3f, 0.9f);

        if (_sidebarBackground != null)
        {
            _sidebarBackground.color = new Color(
                theme.BackgroundTint.r, theme.BackgroundTint.g,
                theme.BackgroundTint.b, 0.85f);
        }

        _dirty = true;
    }

    private void OnPriceUpdated(PriceUpdatedEvent evt)
    {
        _data.ProcessPriceUpdate(evt);
        _dirty = true;
    }

    private void Update()
    {
        if (_data == null) return;

        // Keyboard shortcuts: 1-4 select stocks
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.digit1Key.wasPressedThisFrame) { _data.SelectStock(0); _dirty = true; }
        else if (keyboard.digit2Key.wasPressedThisFrame) { _data.SelectStock(1); _dirty = true; }
        else if (keyboard.digit3Key.wasPressedThisFrame) { _data.SelectStock(2); _dirty = true; }
        else if (keyboard.digit4Key.wasPressedThisFrame) { _data.SelectStock(3); _dirty = true; }

        // Pulse animation for event indicators
        if (_activeIndicators.Count > 0)
        {
            _pulseTimer += Time.deltaTime;
        }
    }

    private void LateUpdate()
    {
        if (_dirty)
        {
            _dirty = false;
            RefreshEntryVisuals();
        }
        else if (_activeIndicators.Count > 0)
        {
            UpdateIndicatorPulse();
        }
    }

    /// <summary>
    /// Called by entry button click handlers.
    /// </summary>
    public void OnEntryClicked(int index)
    {
        _data.SelectStock(index);
        RefreshEntryVisuals();
    }

    private void OnMarketEventFired(MarketEventFiredEvent evt)
    {
        if (evt.AffectedStockIds == null) return;

        var indicatorType = GetIndicatorType(evt.EventType);
        if (indicatorType == IndicatorType.None) return;

        foreach (int stockId in evt.AffectedStockIds)
        {
            _activeIndicators[stockId] = new ActiveIndicator
            {
                Type = indicatorType,
                EventType = evt.EventType,
                IsPositive = evt.IsPositive
            };
        }
        _dirty = true;
    }

    private void OnMarketEventEnded(MarketEventEndedEvent evt)
    {
        if (evt.AffectedStockIds == null) return;

        foreach (int stockId in evt.AffectedStockIds)
        {
            if (_activeIndicators.TryGetValue(stockId, out var indicator) &&
                indicator.EventType == evt.EventType)
            {
                _activeIndicators.Remove(stockId);
            }
        }
        _dirty = true;
    }

    /// <summary>
    /// Maps event type to indicator type for sidebar display.
    /// Static for testability.
    /// </summary>
    public static IndicatorType GetIndicatorType(MarketEventType eventType)
    {
        switch (eventType)
        {
            case MarketEventType.PumpAndDump: return IndicatorType.VolumePulse;
            case MarketEventType.SECInvestigation: return IndicatorType.Warning;
            case MarketEventType.SectorRotation: return IndicatorType.SectorGlow;
            default: return IndicatorType.None;
        }
    }

    /// <summary>
    /// Gets the indicator color for a given indicator type and sector direction.
    /// Static for testability.
    /// </summary>
    public static Color GetIndicatorColor(IndicatorType type, bool isPositive = true)
    {
        switch (type)
        {
            case IndicatorType.VolumePulse: return VolumeIconColor;
            case IndicatorType.Warning: return WarningIconColor;
            case IndicatorType.SectorGlow: return isPositive ? SectorWinColor : SectorLoseColor;
            default: return Color.clear;
        }
    }

    private void RefreshEntryVisuals()
    {
        if (_entryViews == null) return;

        for (int i = 0; i < _entryViews.Length && i < _data.EntryCount; i++)
        {
            var entry = _data.GetEntry(i);
            var view = _entryViews[i];

            if (view.TickerText != null)
                view.TickerText.text = entry.TickerSymbol;

            if (view.PriceText != null)
                view.PriceText.text = TradingHUD.FormatCurrency(entry.CurrentPrice);

            if (view.ChangeText != null)
            {
                view.ChangeText.text = TradingHUD.FormatPercentChange(entry.PercentChange);
                view.ChangeText.color = entry.PercentChange > 0f ? ProfitGreen
                    : entry.PercentChange < 0f ? LossRed
                    : Color.white;
            }

            if (view.Background != null)
                view.Background.color = entry.IsSelected ? _selectedBgColor : _normalBgColor;

            // Update sparkline using cached min/max from StockEntry
            if (view.SparklineRenderer != null)
            {
                int count = entry.SparklinePointCount;
                view.SparklineRenderer.positionCount = count;
                float range = entry.SparklineMax - entry.SparklineMin;
                for (int p = 0; p < count; p++)
                {
                    float x = view.SparklineBounds.xMin + (view.SparklineBounds.width * p / Mathf.Max(1, count - 1));
                    float price = entry.GetSparklinePoint(p);
                    float yNorm = range > 0f ? (price - entry.SparklineMin) / range : 0.5f;
                    float y = view.SparklineBounds.yMin + view.SparklineBounds.height * yNorm;
                    view.SparklineRenderer.SetPosition(p, new Vector3(x, y, 0f));
                }
            }

            // Update event indicator
            if (view.EventIndicator != null)
            {
                int stockId = entry.StockId;
                if (_activeIndicators.TryGetValue(stockId, out var indicator))
                {
                    view.EventIndicator.gameObject.SetActive(true);
                    var color = GetIndicatorColor(indicator.Type);
                    // Pulse alpha for VolumePulse type
                    if (indicator.Type == IndicatorType.VolumePulse)
                    {
                        float pulse = 0.5f + 0.5f * Mathf.Sin(_pulseTimer * VolumePulseFrequency);
                        color.a = pulse;
                    }
                    view.EventIndicator.color = color;

                    if (view.EventIndicatorText != null)
                    {
                        view.EventIndicatorText.text = GetIndicatorSymbol(indicator.Type);
                    }
                }
                else
                {
                    view.EventIndicator.gameObject.SetActive(false);
                }
            }

            // Update sector glow border
            if (view.GlowBorder != null)
            {
                int stockId = entry.StockId;
                if (_activeIndicators.TryGetValue(stockId, out var glowIndicator) &&
                    glowIndicator.Type == IndicatorType.SectorGlow)
                {
                    view.GlowBorder.gameObject.SetActive(true);
                    // Determine direction from event type context (stored as isPositive)
                    view.GlowBorder.color = glowIndicator.IsPositive ? SectorWinColor : SectorLoseColor;
                }
                else
                {
                    view.GlowBorder.gameObject.SetActive(false);
                }
            }
        }
    }

    private void UpdateIndicatorPulse()
    {
        if (_entryViews == null) return;

        for (int i = 0; i < _entryViews.Length && i < _data.EntryCount; i++)
        {
            var entry = _data.GetEntry(i);
            var view = _entryViews[i];

            if (view.EventIndicator != null)
            {
                int stockId = entry.StockId;
                if (_activeIndicators.TryGetValue(stockId, out var indicator) &&
                    indicator.Type == IndicatorType.VolumePulse)
                {
                    var color = GetIndicatorColor(indicator.Type);
                    float pulse = 0.5f + 0.5f * Mathf.Sin(_pulseTimer * VolumePulseFrequency);
                    color.a = pulse;
                    view.EventIndicator.color = color;
                }
            }
        }
    }

    /// <summary>
    /// Gets the Unicode symbol for an indicator type.
    /// Static for testability.
    /// </summary>
    public static string GetIndicatorSymbol(IndicatorType type)
    {
        switch (type)
        {
            case IndicatorType.VolumePulse: return "\u2581\u2583\u2585\u2587"; // Volume bars
            case IndicatorType.Warning: return "\u26A0"; // Warning triangle
            default: return "";
        }
    }
}

public enum IndicatorType
{
    None,
    VolumePulse,
    Warning,
    SectorGlow
}

public class ActiveIndicator
{
    public IndicatorType Type;
    public MarketEventType EventType;
    public bool IsPositive;
}

/// <summary>
/// References to UI elements for a single stock entry in the sidebar.
/// Created by UISetup, consumed by StockSidebar.
/// </summary>
public class StockEntryView
{
    public Text TickerText;
    public Text PriceText;
    public Text ChangeText;
    public Image Background;
    public LineRenderer SparklineRenderer;
    public Rect SparklineBounds;
    public Image EventIndicator;
    public Text EventIndicatorText;
    public Image GlowBorder;
}
