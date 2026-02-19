using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
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
    private static readonly Color DefaultSelectedBgColor = ColorPalette.WithAlpha(ColorPalette.Dimmed(ColorPalette.Panel, 1.5f), 0.9f);
    private static readonly Color DefaultNormalBgColor = ColorPalette.WithAlpha(ColorPalette.Panel, 0.6f);
    private static Color ProfitGreen => CRTThemeData.TextHigh;
    private static Color LossRed => CRTThemeData.Danger;

    // Event indicator colors
    public static readonly Color VolumeIconColor = ColorPalette.Amber;
    public static Color WarningIconColor => CRTThemeData.Warning;
    public static readonly Color SectorWinColor = ColorPalette.WithAlpha(ColorPalette.Green, 0.4f);
    public static readonly Color SectorLoseColor = ColorPalette.WithAlpha(ColorPalette.Red, 0.4f);
    public static readonly float VolumePulseFrequency = 4f;

    // AC 1: Price tick flash constants
    public static readonly float PriceFlashDuration = 0.25f;
    public static readonly float PriceFlashThreshold = 0.001f;

    // AC 6: Sidebar event cell flash constants
    public static readonly float EventFlashInDuration = 0.05f;
    public static readonly float EventFlashOutDuration = 0.15f;

    // AC 14: Market open reveal cascade constants
    public static readonly float RevealStagger = 0.15f;
    public static readonly float RevealDuration = 0.12f;

    // Tier-themed colors (defaults to standard colors)
    private Color _selectedBgColor = DefaultSelectedBgColor;
    private Color _normalBgColor = DefaultNormalBgColor;
    private Image _sidebarBackground;

    // AC 1: Cache of previous percent change per entry for flash threshold check
    private float[] _prevPercentChange;

    // Active event indicators per stock (keyed by stock id)
    private Dictionary<int, ActiveIndicator> _activeIndicators = new Dictionary<int, ActiveIndicator>();
    private float _pulseTimer;

    public StockSidebarData Data => _data;

    public void Initialize(StockSidebarData data, StockEntryView[] entryViews)
    {
        _data = data;
        _entryViews = entryViews;
        _prevPercentChange = new float[entryViews.Length];

        // Add hover sound to each stock entry
        for (int i = 0; i < entryViews.Length; i++)
        {
            if (entryViews[i].Background == null) continue;
            var trigger = entryViews[i].Background.gameObject.GetComponent<EventTrigger>()
                       ?? entryViews[i].Background.gameObject.AddComponent<EventTrigger>();
            var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener((_) => AudioManager.Instance?.PlayButtonHover());
            trigger.triggers.Add(enterEntry);
        }

        EventBus.Subscribe<PriceUpdatedEvent>(OnPriceUpdated);
        EventBus.Subscribe<ActTransitionEvent>(OnActTransition);
        EventBus.Subscribe<MarketEventFiredEvent>(OnMarketEventFired);
        EventBus.Subscribe<MarketEventEndedEvent>(OnMarketEventEnded);
        EventBus.Subscribe<MarketOpenEvent>(OnMarketOpen);
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
        EventBus.Unsubscribe<MarketOpenEvent>(OnMarketOpen);
    }

    // AC 14: Market open — flag pending reveal cascade, executed after entries are built
    private bool _pendingRevealCascade;

    private void OnMarketOpen(MarketOpenEvent evt)
    {
        _pendingRevealCascade = true;
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
        if (keyboard.digit1Key.wasPressedThisFrame) { _data.SelectStock(0); _dirty = true; AudioManager.Instance?.PlayNavigate(); }
        else if (keyboard.digit2Key.wasPressedThisFrame) { _data.SelectStock(1); _dirty = true; AudioManager.Instance?.PlayNavigate(); }
        else if (keyboard.digit3Key.wasPressedThisFrame) { _data.SelectStock(2); _dirty = true; AudioManager.Instance?.PlayNavigate(); }
        else if (keyboard.digit4Key.wasPressedThisFrame) { _data.SelectStock(3); _dirty = true; AudioManager.Instance?.PlayNavigate(); }

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

        // Click scale punch feel
        if (_entryViews != null && index >= 0 && index < _entryViews.Length
            && _entryViews[index].Background != null)
        {
            _entryViews[index].Background.transform.DOKill();
            _entryViews[index].Background.transform
                .DOPunchScale(Vector3.one * 0.08f, 0.18f, 6, 0.5f).SetUpdate(true);
        }
    }

    private void OnMarketEventFired(MarketEventFiredEvent evt)
    {
        if (evt.AffectedStockIds == null) return;

        var indicatorType = GetIndicatorType(evt.EventType);
        if (indicatorType != IndicatorType.None)
        {
            foreach (int stockId in evt.AffectedStockIds)
            {
                _activeIndicators[stockId] = new ActiveIndicator
                {
                    Type = indicatorType,
                    EventType = evt.EventType,
                    IsPositive = evt.IsPositive
                };
            }
        }

        // AC 6: Flash the background of each affected stock entry
        Color flashColor = evt.IsPositive ? EventPopup.PositiveColor : EventPopup.NegativeColor;
        for (int i = 0; _entryViews != null && i < _entryViews.Length && i < _data.EntryCount; i++)
        {
            var entry = _data.GetEntry(i);
            bool isAffected = false;
            foreach (int sid in evt.AffectedStockIds)
            {
                if (sid == entry.StockId) { isAffected = true; break; }
            }
            if (!isAffected) continue;

            var view = _entryViews[i];
            if (view.Background == null) continue;
            Color returnColor = entry.IsSelected ? _selectedBgColor : _normalBgColor;
            view.Background.DOKill();
            view.Background.DOColor(flashColor, EventFlashInDuration)
                .SetUpdate(false)
                .OnComplete(() =>
                    view.Background.DOColor(returnColor, EventFlashOutDuration).SetUpdate(false));
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
            {
                view.PriceText.text = TradingHUD.FormatCurrency(entry.CurrentPrice);

                // AC 1: Price tick flash — fire only when delta exceeds threshold
                float delta = Mathf.Abs(entry.PercentChange - _prevPercentChange[i]);
                if (delta >= PriceFlashThreshold)
                {
                    Color flashColor = entry.PercentChange > _prevPercentChange[i] ? ProfitGreen : LossRed;
                    Color defaultColor = CRTThemeData.TextHigh;
                    view.PriceText.DOKill();
                    view.PriceText.color = flashColor;
                    view.PriceText.DOColor(defaultColor, PriceFlashDuration).SetUpdate(false);
                }
                _prevPercentChange[i] = entry.PercentChange;
            }

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

        // AC 14: Trigger market open reveal cascade on first refresh after MarketOpenEvent
        if (_pendingRevealCascade && _entryViews != null && _data.EntryCount > 0)
        {
            _pendingRevealCascade = false;
            StartCoroutine(RevealCascadeCoroutine());
        }
    }

    // AC 14: Coroutine that reveals each entry one-by-one with alpha + scaleX lerp
    private System.Collections.IEnumerator RevealCascadeCoroutine()
    {
        for (int i = 0; i < _entryViews.Length && i < _data.EntryCount; i++)
        {
            var view = _entryViews[i];
            if (view.Background == null) continue;

            var canvasGroup = view.Background.gameObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = view.Background.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            var rect = view.Background.GetComponent<RectTransform>();
            if (rect != null)
                rect.localScale = new Vector3(0.8f, 1f, 1f);

            yield return new WaitForSeconds(RevealStagger * i);

            float elapsed = 0f;
            while (elapsed < RevealDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / RevealDuration);
                canvasGroup.alpha = t;
                if (rect != null)
                    rect.localScale = new Vector3(Mathf.Lerp(0.8f, 1f, t), 1f, 1f);
                yield return null;
            }
            canvasGroup.alpha = 1f;
            if (rect != null)
                rect.localScale = Vector3.one;
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
