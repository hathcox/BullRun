using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MonoBehaviour for the positions panel on the right sidebar.
/// Displays open positions with real-time P&L updates.
/// Read-only — never modifies positions.
/// </summary>
public class PositionPanel : MonoBehaviour
{
    // Story 14.6: Color constants migrated to CRTThemeData
    public static Color ProfitGreen => CRTThemeData.TextHigh;
    public static Color LossRed => CRTThemeData.Danger;
    public static Color LongAccentColor => CRTThemeData.TextHigh;
    public static Color ShortAccentColor => CRTThemeData.Warning;

    public static Color ShortSqueezeWarningColor => CRTThemeData.Danger;
    public static readonly float WarningPulseFrequency = 5f;

    private static readonly Color DimTextColor = new Color(0.5f, 0.5f, 0.55f, 1f);

    private PositionPanelData _data;
    private Portfolio _portfolio;
    private Dictionary<string, float> _latestPrices = new Dictionary<string, float>();
    private bool _pnlDirty;
    private bool _rebuildDirty;

    // Stock ID → ticker symbol mapping (built from MarketOpenEvent)
    private Dictionary<string, string> _tickerLookup = new Dictionary<string, string>();

    // UI elements
    private Transform _entryContainer;
    private Text _emptyText;
    private List<PositionEntryView> _entryViews = new List<PositionEntryView>();

    // Short squeeze warning tracking
    private HashSet<string> _shortSqueezeStocks = new HashSet<string>();
    private float _warningPulseTimer;

    // Short countdown tracking (per-stock timer from GameRunner)
    private Dictionary<string, ShortCountdownEvent> _shortCountdowns = new Dictionary<string, ShortCountdownEvent>();

    public PositionPanelData Data => _data;

    public void Initialize(Portfolio portfolio, Transform entryContainer, Text emptyText)
    {
        _portfolio = portfolio;
        _entryContainer = entryContainer;
        _emptyText = emptyText;
        _data = new PositionPanelData();

        Debug.Log($"[panel-ui-bug] Initialize: portfolio={portfolio != null}, container={entryContainer != null}, emptyText={emptyText != null}");

        EventBus.Subscribe<PriceUpdatedEvent>(OnPriceUpdated);
        EventBus.Subscribe<TradeExecutedEvent>(OnTradeExecuted);
        EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
        EventBus.Subscribe<ShortCountdownEvent>(OnShortCountdown);
        EventBus.Subscribe<MarketEventFiredEvent>(OnMarketEventFired);
        EventBus.Subscribe<MarketEventEndedEvent>(OnMarketEventEnded);
        EventBus.Subscribe<MarketOpenEvent>(OnMarketOpen);

        RefreshPanel();
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<PriceUpdatedEvent>(OnPriceUpdated);
        EventBus.Unsubscribe<TradeExecutedEvent>(OnTradeExecuted);
        EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
        EventBus.Unsubscribe<ShortCountdownEvent>(OnShortCountdown);
        EventBus.Unsubscribe<MarketEventFiredEvent>(OnMarketEventFired);
        EventBus.Unsubscribe<MarketEventEndedEvent>(OnMarketEventEnded);
        EventBus.Unsubscribe<MarketOpenEvent>(OnMarketOpen);
    }

    private void OnMarketOpen(MarketOpenEvent evt)
    {
        _tickerLookup.Clear();
        if (evt.StockIds != null && evt.TickerSymbols != null)
        {
            for (int i = 0; i < evt.StockIds.Length && i < evt.TickerSymbols.Length; i++)
            {
                _tickerLookup[evt.StockIds[i].ToString()] = evt.TickerSymbols[i];
            }
        }
        Debug.Log($"[panel-ui-bug] OnMarketOpen: built ticker lookup with {_tickerLookup.Count} entries");
    }

    private void OnMarketEventFired(MarketEventFiredEvent evt)
    {
        if (evt.EventType != MarketEventType.ShortSqueeze) return;
        if (evt.AffectedTickerSymbols == null) return;

        foreach (var ticker in evt.AffectedTickerSymbols)
        {
            _shortSqueezeStocks.Add(ticker);
        }
        _pnlDirty = true;
    }

    private void OnMarketEventEnded(MarketEventEndedEvent evt)
    {
        if (evt.EventType != MarketEventType.ShortSqueeze) return;
        if (evt.AffectedTickerSymbols == null) return;

        foreach (var ticker in evt.AffectedTickerSymbols)
        {
            _shortSqueezeStocks.Remove(ticker);
        }
        _pnlDirty = true;
    }

    private void OnPriceUpdated(PriceUpdatedEvent evt)
    {
        _latestPrices[evt.StockId.ToString()] = evt.NewPrice;
        _pnlDirty = true;
    }

    private void OnTradeExecuted(TradeExecutedEvent evt)
    {
        Debug.Log($"[panel-ui-bug] OnTradeExecuted: stock={evt.StockId}, isBuy={evt.IsBuy}, isShort={evt.IsShort}, shares={evt.Shares}");
        _rebuildDirty = true;
    }

    private void OnRoundStarted(RoundStartedEvent evt)
    {
        _shortCountdowns.Clear();
        _rebuildDirty = true;
    }

    private void OnShortCountdown(ShortCountdownEvent evt)
    {
        _shortCountdowns[evt.StockId] = evt;
    }

    private void Update()
    {
        if (_shortSqueezeStocks.Count > 0)
        {
            _warningPulseTimer += Time.deltaTime;
        }
    }

    private void LateUpdate()
    {
        if (_rebuildDirty)
        {
            _rebuildDirty = false;
            _pnlDirty = false;
            RefreshPanel();
        }
        else if (_pnlDirty)
        {
            _pnlDirty = false;
            UpdatePnLDisplay();
        }
        else if (_shortSqueezeStocks.Count > 0)
        {
            UpdateWarningPulse();
        }
    }

    private void RefreshPanel()
    {
        if (_portfolio == null)
        {
            Debug.LogWarning("[panel-ui-bug] RefreshPanel: _portfolio is null, skipping");
            return;
        }

        _data.RefreshFromPortfolio(_portfolio);
        Debug.Log($"[panel-ui-bug] RefreshPanel: entryCount={_data.EntryCount}, container={_entryContainer != null}");
        for (int i = 0; i < _data.EntryCount; i++)
        {
            var e = _data.GetEntry(i);
            Debug.Log($"[panel-ui-bug]   Entry[{i}]: stock={e.StockId}, ticker={ResolveTickerSymbol(e.StockId)}, shares={e.Shares}, isLong={e.IsLong}, avgPrice={e.AveragePrice}");
        }
        RebuildEntryViews();
        UpdatePnLDisplay();

        if (_emptyText != null)
            _emptyText.gameObject.SetActive(_data.IsEmpty);
    }

    private void UpdatePnLDisplay()
    {
        _data.UpdateAllPnL(GetCachedPrice);

        for (int i = 0; i < _entryViews.Count && i < _data.EntryCount; i++)
        {
            var entry = _data.GetEntry(i);
            var view = _entryViews[i];

            if (view.PnLText != null)
            {
                view.PnLText.text = $"P&L: {TradingHUD.FormatProfit(entry.UnrealizedPnL)}";
                view.PnLText.color = GetPnLColor(entry.UnrealizedPnL);
            }

            // Short squeeze warning icon
            if (view.WarningIcon != null)
            {
                bool showWarning = _shortSqueezeStocks.Contains(entry.StockId) && !entry.IsLong;
                view.WarningIcon.gameObject.SetActive(showWarning);
                if (showWarning)
                {
                    float pulse = 0.5f + 0.5f * Mathf.Sin(_warningPulseTimer * WarningPulseFrequency);
                    var c = ShortSqueezeWarningColor;
                    c.a = pulse;
                    view.WarningIcon.color = c;
                }
            }

            // Short countdown timer — only shown on short positions
            if (view.CountdownText != null)
            {
                if (!entry.IsLong && _shortCountdowns.TryGetValue(entry.StockId, out var countdown))
                {
                    view.CountdownText.gameObject.SetActive(true);
                    if (countdown.IsCashOutWindow)
                        view.CountdownText.text = $"Auto-close: {countdown.TimeRemaining:F1}s";
                    else
                        view.CountdownText.text = $"Hold: {countdown.TimeRemaining:F1}s";
                    view.CountdownText.color = countdown.IsCashOutWindow ? ShortSqueezeWarningColor : CRTThemeData.Warning;
                }
                else
                {
                    view.CountdownText.gameObject.SetActive(false);
                }
            }
        }
    }

    private void UpdateWarningPulse()
    {
        for (int i = 0; i < _entryViews.Count && i < _data.EntryCount; i++)
        {
            var entry = _data.GetEntry(i);
            var view = _entryViews[i];

            if (view.WarningIcon != null)
            {
                bool showWarning = _shortSqueezeStocks.Contains(entry.StockId) && !entry.IsLong;
                view.WarningIcon.gameObject.SetActive(showWarning);
                if (showWarning)
                {
                    float pulse = 0.5f + 0.5f * Mathf.Sin(_warningPulseTimer * WarningPulseFrequency);
                    var c = ShortSqueezeWarningColor;
                    c.a = pulse;
                    view.WarningIcon.color = c;
                }
            }
        }
    }

    private float GetCachedPrice(string stockId)
    {
        return _latestPrices.TryGetValue(stockId, out float price) ? price : 0f;
    }

    /// <summary>
    /// Resolves a numeric stock ID (e.g. "0") to its ticker symbol (e.g. "MOON").
    /// Falls back to the raw ID if no mapping exists.
    /// </summary>
    private string ResolveTickerSymbol(string stockId)
    {
        return _tickerLookup.TryGetValue(stockId, out string ticker) ? ticker : stockId;
    }

    private void RebuildEntryViews()
    {
        // Clear existing views
        foreach (var view in _entryViews)
        {
            if (view.Root != null)
                Destroy(view.Root);
        }
        _entryViews.Clear();

        if (_entryContainer == null)
        {
            Debug.LogWarning("[panel-ui-bug] RebuildEntryViews: _entryContainer is null, skipping");
            return;
        }

        for (int i = 0; i < _data.EntryCount; i++)
        {
            var entry = _data.GetEntry(i);
            var view = CreateEntryView(entry, _entryContainer);
            _entryViews.Add(view);
            Debug.Log($"[panel-ui-bug] Created view[{i}]: root={view.Root.name}, ticker={ResolveTickerSymbol(entry.StockId)}");
        }
    }

    /// <summary>
    /// Creates a compact entry matching the reference layout:
    ///   LONG                    (type header, colored)
    ///   MOON 1x Avg: $7.41 / P&L: -$3.43   (info line)
    ///   Hold: 4.2s             (countdown, shorts only)
    /// </summary>
    private PositionEntryView CreateEntryView(PositionDisplayEntry entry, Transform parent)
    {
        var view = new PositionEntryView();
        var typeColor = GetPositionTypeColor(entry.IsLong);
        string ticker = ResolveTickerSymbol(entry.StockId);

        // Entry container with VLG for compact stacking
        var entryGo = new GameObject($"Position_{entry.StockId}");
        entryGo.transform.SetParent(parent, false);
        entryGo.AddComponent<RectTransform>();
        var entryVlg = entryGo.AddComponent<VerticalLayoutGroup>();
        entryVlg.spacing = 0f;
        entryVlg.padding = new RectOffset(4, 4, 1, 1);
        entryVlg.childAlignment = TextAnchor.UpperLeft;
        entryVlg.childForceExpandWidth = true;
        entryVlg.childForceExpandHeight = false;
        var entryFitter = entryGo.AddComponent<ContentSizeFitter>();
        entryFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        view.Root = entryGo;

        // Line 1: Type header — "LONG" or "SHORT" (bold, type-colored)
        var typeGo = CreateLayoutText($"Type_{entry.StockId}", entryGo.transform,
            FormatPositionType(entry.IsLong), typeColor, 15, FontStyle.Bold, TextAnchor.MiddleLeft, 18f);
        view.TickerText = typeGo.GetComponent<Text>();

        // Line 2: Info — "MOON 1x  Avg: $7.41 / P&L: -$3.43"
        var infoRowGo = new GameObject("InfoRow");
        infoRowGo.transform.SetParent(entryGo.transform, false);
        infoRowGo.AddComponent<RectTransform>();
        var infoLayout = infoRowGo.AddComponent<HorizontalLayoutGroup>();
        infoLayout.spacing = 0f;
        infoLayout.childForceExpandWidth = true;
        infoLayout.childForceExpandHeight = true;
        var infoLayoutElem = infoRowGo.AddComponent<LayoutElement>();
        infoLayoutElem.preferredHeight = 18f;

        // Left: "MOON 1x Avg: $7.41"
        var detailGo = CreateText($"Detail_{entry.StockId}", infoRowGo.transform,
            $"{ticker} {entry.Shares}x Avg: {TradingHUD.FormatCurrency(entry.AveragePrice)}",
            DimTextColor, 14, FontStyle.Normal, TextAnchor.MiddleLeft);
        view.AvgPriceText = detailGo.GetComponent<Text>();
        view.SharesText = detailGo.GetComponent<Text>(); // shares included in same text

        // Right: "P&L: -$3.43"
        var pnlGo = CreateText($"PnL_{entry.StockId}", infoRowGo.transform,
            $"P&L: {TradingHUD.FormatProfit(entry.UnrealizedPnL)}", Color.white,
            14, FontStyle.Bold, TextAnchor.MiddleRight);
        view.PnLText = pnlGo.GetComponent<Text>();

        // Warning icon for short squeeze (hidden by default, overlaid)
        var warningGo = CreateText($"Warning_{entry.StockId}", entryGo.transform,
            "\u26A0", ShortSqueezeWarningColor, 12, FontStyle.Bold, TextAnchor.MiddleCenter);
        var warningRect = warningGo.GetComponent<RectTransform>();
        warningRect.anchorMin = new Vector2(1f, 1f);
        warningRect.anchorMax = new Vector2(1f, 1f);
        warningRect.pivot = new Vector2(1f, 1f);
        warningRect.anchoredPosition = new Vector2(-2f, -1f);
        warningRect.sizeDelta = new Vector2(16f, 14f);
        // Exclude from layout so it overlays
        var warningLayoutIgnore = warningGo.AddComponent<LayoutElement>();
        warningLayoutIgnore.ignoreLayout = true;
        view.WarningIcon = warningGo.GetComponent<Text>();
        warningGo.SetActive(false);

        // Countdown text for short timer (hidden by default)
        var countdownGo = CreateLayoutText($"Countdown_{entry.StockId}", entryGo.transform,
            "", CRTThemeData.Warning, 14, FontStyle.Normal, TextAnchor.MiddleRight, 17f);
        view.CountdownText = countdownGo.GetComponent<Text>();
        countdownGo.SetActive(false);

        return view;
    }

    /// <summary>
    /// Creates a Text GameObject that participates in layout (has LayoutElement with preferredHeight).
    /// </summary>
    private GameObject CreateLayoutText(string name, Transform parent, string text, Color color,
        int fontSize, FontStyle style, TextAnchor alignment, float preferredHeight)
    {
        var go = CreateText(name, parent, text, color, fontSize, style, alignment);
        var layoutElem = go.AddComponent<LayoutElement>();
        layoutElem.preferredHeight = preferredHeight;
        return go;
    }

    private GameObject CreateText(string name, Transform parent, string text, Color color,
        int fontSize, FontStyle style, TextAnchor alignment)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var txt = go.AddComponent<Text>();
        txt.text = text;
        txt.color = color;
        txt.fontSize = fontSize;
        txt.fontStyle = style;
        txt.alignment = alignment;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return go;
    }

    // --- Static utility methods for testability ---

    public static Color GetPositionTypeColor(bool isLong)
    {
        return isLong ? LongAccentColor : ShortAccentColor;
    }

    public static Color GetPnLColor(float pnl)
    {
        if (pnl > 0f) return ProfitGreen;
        if (pnl < 0f) return LossRed;
        return Color.white;
    }

    public static string FormatPositionType(bool isLong)
    {
        return isLong ? "LONG" : "SHORT";
    }
}

/// <summary>
/// References to UI elements for a single position entry.
/// </summary>
public class PositionEntryView
{
    public GameObject Root;
    public Text TickerText;
    public Text SharesText;
    public Text AvgPriceText;
    public Text PnLText;
    public Text WarningIcon;
    public Text CountdownText;
}
