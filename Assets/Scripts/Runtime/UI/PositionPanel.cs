using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
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

    // AC 4: Position entry slide-in (alpha-only per Dev Notes — VLG manages position)
    public static readonly float EntrySlideInDuration = 0.2f;

    // AC 5: Auto-liquidation cascade constants
    public static readonly float CascadeStagger = 0.08f;
    public static readonly float CascadeExitDuration = 0.15f;

    // AC 7: Short countdown urgency threshold
    public static readonly float CountdownUrgencyThreshold = 3.0f;

    private static readonly Color DimTextColor = ColorPalette.WhiteDim;

    private PositionPanelData _data;
    private RunContext _ctx;
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

    // AC 7: Track last integer second seen per stock for punch trigger
    private Dictionary<string, int> _lastCountdownSecond = new Dictionary<string, int>();

    public PositionPanelData Data => _data;

    public void Initialize(RunContext ctx, Transform entryContainer, Text emptyText)
    {
        _ctx = ctx;
        _entryContainer = entryContainer;
        _emptyText = emptyText;
        _data = new PositionPanelData();

        EventBus.Subscribe<PriceUpdatedEvent>(OnPriceUpdated);
        EventBus.Subscribe<TradeExecutedEvent>(OnTradeExecuted);
        EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
        EventBus.Subscribe<ShortCountdownEvent>(OnShortCountdown);
        EventBus.Subscribe<MarketEventFiredEvent>(OnMarketEventFired);
        EventBus.Subscribe<MarketEventEndedEvent>(OnMarketEventEnded);
        EventBus.Subscribe<MarketOpenEvent>(OnMarketOpen);
        EventBus.Subscribe<TradingPhaseEndedEvent>(OnTradingPhaseEnded);

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
        EventBus.Unsubscribe<TradingPhaseEndedEvent>(OnTradingPhaseEnded);
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

    private void OnRoundStarted(RoundStartedEvent evt)
    {
        _shortCountdowns.Clear();
        _rebuildDirty = true;
    }

    private void OnShortCountdown(ShortCountdownEvent evt)
    {
        _shortCountdowns[evt.StockId] = evt;
    }

    // AC 5: Auto-liquidation cascade — animate entries out before they're destroyed
    private void OnTradingPhaseEnded(TradingPhaseEndedEvent evt)
    {
        if (_entryViews.Count > 0)
        {
            var viewsToAnimate = new List<PositionEntryView>(_entryViews);
            _entryViews.Clear();
            StartCoroutine(CascadeOutEntries(viewsToAnimate));
        }
    }

    private IEnumerator CascadeOutEntries(List<PositionEntryView> views)
    {
        foreach (var view in views)
        {
            if (view.Root == null) continue;
            yield return new WaitForSeconds(CascadeStagger);

            var rect = view.Root.GetComponent<RectTransform>();
            if (rect != null)
                rect.DOAnchorPosX(rect.anchoredPosition.x + 80f, CascadeExitDuration).SetUpdate(false);

            var canvasGroup = view.Root.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = view.Root.AddComponent<CanvasGroup>();
            canvasGroup.DOFade(0f, CascadeExitDuration).SetUpdate(false).OnComplete(() =>
            {
                if (view.Root != null)
                    Destroy(view.Root);
            });
        }

        // Wait only for the last entry's exit animation to complete (stagger was already consumed above)
        yield return new WaitForSeconds(CascadeExitDuration);

        if (_emptyText != null)
            _emptyText.gameObject.SetActive(true);
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
        if (_ctx == null || _ctx.Portfolio == null) return;

        _data.RefreshFromPortfolio(_ctx.Portfolio);
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

                    // AC 7: Urgency escalation — red color and punch shake at each whole-second boundary
                    if (countdown.IsCashOutWindow && countdown.TimeRemaining <= CountdownUrgencyThreshold)
                    {
                        view.CountdownText.color = ShortSqueezeWarningColor;

                        int currentSecond = Mathf.FloorToInt(countdown.TimeRemaining);
                        if (!_lastCountdownSecond.TryGetValue(entry.StockId, out int lastSecond) || currentSecond != lastSecond)
                        {
                            _lastCountdownSecond[entry.StockId] = currentSecond;
                            var countdownRect = view.CountdownText.GetComponent<RectTransform>();
                            if (countdownRect != null)
                            {
                                countdownRect.DOKill();
                                countdownRect.DOPunchPosition(new Vector3(3f, 0f, 0f), 0.2f, 5, 0.5f).SetUpdate(false);
                            }
                        }
                    }
                    else
                    {
                        view.CountdownText.color = countdown.IsCashOutWindow ? ShortSqueezeWarningColor : CRTThemeData.Warning;
                    }
                }
                else
                {
                    view.CountdownText.gameObject.SetActive(false);
                    _lastCountdownSecond.Remove(entry.StockId);
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

        if (_entryContainer == null) return;

        for (int i = 0; i < _data.EntryCount; i++)
        {
            var entry = _data.GetEntry(i);
            var view = CreateEntryView(entry, _entryContainer);
            _entryViews.Add(view);

            // AC 4: Fade-in animation (alpha only — VLG manages position)
            // Note: must use explicit null check — ?? uses C# reference equality, not Unity's overridden ==
            var canvasGroup = view.Root.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = view.Root.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, EntrySlideInDuration).SetUpdate(false);
        }
    }

    // Share square visual constants
    public static readonly float ShareSquareSize = 8f;
    public static readonly float ShareSquareSpacing = 2f;
    public static readonly int ShareSquareMaxDisplay = 10;

    /// <summary>
    /// Creates a compact entry matching the reference layout:
    ///   LONG                    (type header, colored)
    ///   [■][■][■]              (share squares, one per share)
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

        // Line 2: Share squares — one filled square per share held
        CreateShareSquares(entryGo.transform, entry.Shares, typeColor);

        // Line 3: Info — "MOON 1x  Avg: $7.41 / P&L: -$3.43"
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
    /// Creates a horizontal row of small colored squares, one per share held.
    /// Capped at ShareSquareMaxDisplay to prevent overflow.
    /// </summary>
    private void CreateShareSquares(Transform parent, int shares, Color color)
    {
        var rowGo = new GameObject("ShareSquares");
        rowGo.transform.SetParent(parent, false);
        rowGo.AddComponent<RectTransform>();
        var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = ShareSquareSpacing;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        var rowLayout = rowGo.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = ShareSquareSize + 4f;

        int displayCount = Mathf.Min(shares, ShareSquareMaxDisplay);
        for (int i = 0; i < displayCount; i++)
        {
            var sqGo = new GameObject($"Share_{i}");
            sqGo.transform.SetParent(rowGo.transform, false);
            var sqRect = sqGo.AddComponent<RectTransform>();
            sqRect.sizeDelta = new Vector2(ShareSquareSize, ShareSquareSize);
            var sqImg = sqGo.AddComponent<Image>();
            sqImg.color = color;
            var sqLayout = sqGo.AddComponent<LayoutElement>();
            sqLayout.preferredWidth = ShareSquareSize;
            sqLayout.preferredHeight = ShareSquareSize;
        }
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
