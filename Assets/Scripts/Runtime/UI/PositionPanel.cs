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

    private PositionPanelData _data;
    private Portfolio _portfolio;
    private Dictionary<string, float> _latestPrices = new Dictionary<string, float>();
    private bool _pnlDirty;
    private bool _rebuildDirty;

    // UI elements
    private Transform _entryContainer;
    private Text _emptyText;
    private List<PositionEntryView> _entryViews = new List<PositionEntryView>();

    // Short squeeze warning tracking
    private HashSet<string> _shortSqueezeStocks = new HashSet<string>();
    private float _warningPulseTimer;

    public PositionPanelData Data => _data;

    public void Initialize(Portfolio portfolio, Transform entryContainer, Text emptyText)
    {
        _portfolio = portfolio;
        _entryContainer = entryContainer;
        _emptyText = emptyText;
        _data = new PositionPanelData();

        EventBus.Subscribe<PriceUpdatedEvent>(OnPriceUpdated);
        EventBus.Subscribe<TradeExecutedEvent>(OnTradeExecuted);
        EventBus.Subscribe<MarketEventFiredEvent>(OnMarketEventFired);
        EventBus.Subscribe<MarketEventEndedEvent>(OnMarketEventEnded);

        RefreshPanel();
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<PriceUpdatedEvent>(OnPriceUpdated);
        EventBus.Unsubscribe<TradeExecutedEvent>(OnTradeExecuted);
        EventBus.Unsubscribe<MarketEventFiredEvent>(OnMarketEventFired);
        EventBus.Unsubscribe<MarketEventEndedEvent>(OnMarketEventEnded);
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
        _rebuildDirty = true;
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
        if (_portfolio == null) return;

        _data.RefreshFromPortfolio(_portfolio);
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
                view.PnLText.text = TradingHUD.FormatProfit(entry.UnrealizedPnL);
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

    private void RebuildEntryViews()
    {
        // Clear existing views
        foreach (var view in _entryViews)
        {
            if (view.Root != null)
                Destroy(view.Root);
        }
        _entryViews.Clear();

        if (_entryContainer == null) return;

        for (int i = 0; i < _data.EntryCount; i++)
        {
            var entry = _data.GetEntry(i);
            var view = CreateEntryView(entry, _entryContainer);
            _entryViews.Add(view);
        }
    }

    private PositionEntryView CreateEntryView(PositionDisplayEntry entry, Transform parent)
    {
        var view = new PositionEntryView();

        // Entry container
        var entryGo = new GameObject($"Position_{entry.StockId}");
        entryGo.transform.SetParent(parent, false);
        var rect = entryGo.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 50f);
        var bg = entryGo.AddComponent<Image>();
        bg.color = new Color(CRTThemeData.Panel.r, CRTThemeData.Panel.g, CRTThemeData.Panel.b, 0.6f);
        view.Root = entryGo;

        // Row 1: Ticker + Shares + Type
        var row1Go = new GameObject("Row1");
        row1Go.transform.SetParent(entryGo.transform, false);
        var row1Rect = row1Go.AddComponent<RectTransform>();
        row1Rect.anchorMin = new Vector2(0f, 0.5f);
        row1Rect.anchorMax = new Vector2(1f, 1f);
        row1Rect.offsetMin = new Vector2(6f, 0f);
        row1Rect.offsetMax = new Vector2(-6f, -2f);
        var row1Layout = row1Go.AddComponent<HorizontalLayoutGroup>();
        row1Layout.childForceExpandWidth = true;
        row1Layout.childForceExpandHeight = true;

        // Ticker
        var tickerGo = CreateText($"Ticker_{entry.StockId}", row1Go.transform,
            entry.StockId, GetPositionTypeColor(entry.IsLong), 14, FontStyle.Bold, TextAnchor.MiddleLeft);
        view.TickerText = tickerGo.GetComponent<Text>();

        // Shares + Type
        var typeColor = GetPositionTypeColor(entry.IsLong);
        var sharesGo = CreateText($"Shares_{entry.StockId}", row1Go.transform,
            $"{entry.Shares}x {FormatPositionType(entry.IsLong)}", typeColor, 12, FontStyle.Normal, TextAnchor.MiddleRight);
        view.SharesText = sharesGo.GetComponent<Text>();

        // Row 2: Avg Price + P&L
        var row2Go = new GameObject("Row2");
        row2Go.transform.SetParent(entryGo.transform, false);
        var row2Rect = row2Go.AddComponent<RectTransform>();
        row2Rect.anchorMin = new Vector2(0f, 0f);
        row2Rect.anchorMax = new Vector2(1f, 0.5f);
        row2Rect.offsetMin = new Vector2(6f, 2f);
        row2Rect.offsetMax = new Vector2(-6f, 0f);
        var row2Layout = row2Go.AddComponent<HorizontalLayoutGroup>();
        row2Layout.childForceExpandWidth = true;
        row2Layout.childForceExpandHeight = true;

        // Avg price
        var avgGo = CreateText($"Avg_{entry.StockId}", row2Go.transform,
            $"Avg: {TradingHUD.FormatCurrency(entry.AveragePrice)}", new Color(0.6f, 0.6f, 0.7f, 1f),
            11, FontStyle.Normal, TextAnchor.MiddleLeft);
        view.AvgPriceText = avgGo.GetComponent<Text>();

        // P&L
        var pnlGo = CreateText($"PnL_{entry.StockId}", row2Go.transform,
            TradingHUD.FormatProfit(entry.UnrealizedPnL), Color.white, 13, FontStyle.Bold, TextAnchor.MiddleRight);
        view.PnLText = pnlGo.GetComponent<Text>();

        // Warning icon for short squeeze (hidden by default)
        var warningGo = CreateText($"Warning_{entry.StockId}", entryGo.transform,
            "\u2757", ShortSqueezeWarningColor, 18, FontStyle.Bold, TextAnchor.MiddleCenter);
        var warningRect = warningGo.GetComponent<RectTransform>();
        warningRect.anchorMin = new Vector2(1f, 0.5f);
        warningRect.anchorMax = new Vector2(1f, 0.5f);
        warningRect.pivot = new Vector2(1f, 0.5f);
        warningRect.anchoredPosition = new Vector2(-2f, 0f);
        warningRect.sizeDelta = new Vector2(20f, 20f);
        view.WarningIcon = warningGo.GetComponent<Text>();
        warningGo.SetActive(false);

        return view;
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
}
