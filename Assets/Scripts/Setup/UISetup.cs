using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Setup class that generates UI Canvas hierarchies during F5 rebuild.
/// Creates Control Deck (bottom-docked dashboard), trading panels, and overlays.
/// (FIX-5: Stock Sidebar removed — single stock per round, no selection needed)
/// [SetupClass(SetupPhase.SceneComposition)] attribute to be enabled when SetupPipeline exists.
/// </summary>
// Runtime-only: called by GameRunner.Start(), not during F5 rebuild.
// MonoBehaviour Initialize() calls and EventBus subscriptions must happen at runtime.
public static class UISetup
{
    private static readonly float TopBarHeight = 60f;
    private static readonly float SidebarWidth = 240f;
    private static readonly float EntryHeight = 50f;
    private static readonly Color BarBackgroundColor = new Color(0.05f, 0.07f, 0.18f, 0.9f);
    private static readonly Color SidebarBgColor = new Color(0.05f, 0.07f, 0.18f, 0.85f);
    private static readonly Color LabelColor = new Color(0.6f, 0.6f, 0.7f, 1f);
    private static readonly Color ValueColor = Color.white;
    private static readonly Color NeonGreen = new Color(0f, 1f, 0.533f, 1f);
    private static Material _sparklineMaterial;

    /// <summary>
    /// Parameterless entry point called by SetupPipeline during F5 rebuild.
    /// Creates all UI hierarchies with placeholder values so the scene is visually complete.
    /// GameStates reinitialize these with real data at runtime.
    /// </summary>
    public static void Execute()
    {
        ExecuteMarketOpenUI();

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[Setup] UISetup: All UI hierarchies created (MarketOpenUI)");
        #endif
    }

    public static PositionOverlay Execute(RunContext runContext, int currentRound, float roundDuration)
    {
        // Ensure EventSystem exists for uGUI button interactions (shop, etc.)
        if (EventSystem.current == null)
        {
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<InputSystemUIInputModule>();
        }

        // Story 14.2: Create Control Deck (replaces old top bar)
        var dashRefs = ExecuteControlDeck();

        // Create TradingHUD parent and initialize with DashboardReferences
        var hudParent = new GameObject("TradingHUD");
        var tradingHUD = hudParent.AddComponent<TradingHUD>();
        tradingHUD.Initialize(dashRefs, runContext, currentRound, roundDuration);

        // Story 14.3: Initialize RoundTimerUI with Right Wing timer text
        var roundTimerUI = hudParent.AddComponent<RoundTimerUI>();
        roundTimerUI.Initialize(dashRefs.TimerText, dashRefs.TimerProgressBar, null);

        // Story 14.3: Create PositionOverlay using Right Wing text references from Control Deck
        var posOverlayGo = new GameObject("PositionOverlay");
        var positionOverlay = posOverlayGo.AddComponent<PositionOverlay>();
        positionOverlay.Initialize(
            runContext.Portfolio,
            dashRefs.DirectionText,
            dashRefs.AvgPriceText,
            dashRefs.PnLText,
            dashRefs.AvgPriceRow,
            dashRefs.PnlRow
        );

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[Setup] TradingHUD created: round={currentRound}, target=${MarginCallTargets.GetTarget(currentRound):F0}");
        #endif

        return positionOverlay;
    }

    /// <summary>
    /// Creates the bottom-docked Control Deck panel with three empty wing containers.
    /// Returns a populated DashboardReferences for downstream UI wiring.
    /// Story 14.2: Replaces the old top bar HUD layout.
    /// </summary>
    public static DashboardReferences ExecuteControlDeck()
    {
        var refs = new DashboardReferences();

        // 1.1: Create ControlDeckCanvas with ScreenSpaceOverlay, sortingOrder=20
        var canvasGo = new GameObject("ControlDeckCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20; // Between chart (10) and feedback (23)

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        refs.ControlDeckCanvas = canvas;

        // 1.2: Create Control_Deck_Panel with bottom-center anchoring, height 160px
        var panelGo = CreatePanel("Control_Deck_Panel", canvasGo.transform);
        var panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.05f, 0f);
        panelRect.anchorMax = new Vector2(0.95f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(0f, 160f);

        refs.ControlDeckPanel = panelRect;

        // 1.4: Apply CRTThemeData.Panel background and Border outline
        CRTThemeData.ApplyPanelStyle(panelGo.GetComponent<Image>());

        // 1.3: Add HorizontalLayoutGroup (padding=10, spacing=20)
        var hlg = panelGo.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(10, 10, 10, 10);
        hlg.spacing = 20f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        // 1.5: Create Left_Wing container (~30% width via LayoutElement)
        var leftWingGo = new GameObject("Left_Wing");
        leftWingGo.transform.SetParent(panelGo.transform, false);
        leftWingGo.AddComponent<RectTransform>();
        var leftVlg = leftWingGo.AddComponent<VerticalLayoutGroup>();
        leftVlg.childAlignment = TextAnchor.MiddleCenter;
        leftVlg.childForceExpandWidth = true;
        leftVlg.childForceExpandHeight = false;
        var leftLayout = leftWingGo.AddComponent<LayoutElement>();
        leftLayout.flexibleWidth = 0.3f;

        refs.LeftWing = leftWingGo.GetComponent<RectTransform>();

        // 1.6: Create Center_Core container (~40% width via LayoutElement)
        var centerCoreGo = new GameObject("Center_Core");
        centerCoreGo.transform.SetParent(panelGo.transform, false);
        centerCoreGo.AddComponent<RectTransform>();
        var centerVlg = centerCoreGo.AddComponent<VerticalLayoutGroup>();
        centerVlg.childAlignment = TextAnchor.MiddleCenter;
        centerVlg.childForceExpandWidth = true;
        centerVlg.childForceExpandHeight = false;
        var centerLayout = centerCoreGo.AddComponent<LayoutElement>();
        centerLayout.flexibleWidth = 0.4f;

        refs.CenterCore = centerCoreGo.GetComponent<RectTransform>();

        // 1.7: Create Right_Wing container (~30% width via LayoutElement)
        var rightWingGo = new GameObject("Right_Wing");
        rightWingGo.transform.SetParent(panelGo.transform, false);
        rightWingGo.AddComponent<RectTransform>();
        var rightVlg = rightWingGo.AddComponent<VerticalLayoutGroup>();
        rightVlg.childAlignment = TextAnchor.MiddleCenter;
        rightVlg.childForceExpandWidth = true;
        rightVlg.childForceExpandHeight = false;
        var rightLayout = rightWingGo.AddComponent<LayoutElement>();
        rightLayout.flexibleWidth = 0.3f;

        refs.RightWing = rightWingGo.GetComponent<RectTransform>();

        // ── Story 14.3: Populate Left Wing (Wallet) ─────────────────────────
        leftVlg.spacing = 4f;
        leftVlg.padding = new RectOffset(8, 8, 8, 8);
        leftVlg.childAlignment = TextAnchor.UpperLeft;

        // "WALLET" header (CRTThemeData.TextLow, 10pt)
        var walletHeaderGo = CreateLabel("WalletHeader", leftWingGo.transform, "WALLET", CRTThemeData.TextLow, 10);
        walletHeaderGo.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;

        // Cash row
        var cashRowGo = new GameObject("CashRow");
        cashRowGo.transform.SetParent(leftWingGo.transform, false);
        cashRowGo.AddComponent<RectTransform>();
        var cashRowHlg = cashRowGo.AddComponent<HorizontalLayoutGroup>();
        cashRowHlg.spacing = 4f;
        cashRowHlg.childAlignment = TextAnchor.MiddleLeft;
        cashRowHlg.childForceExpandWidth = false;
        cashRowHlg.childForceExpandHeight = true;

        var cashLabelGo = CreateLabel("CashLabel", cashRowGo.transform, "Cash:", CRTThemeData.TextLow, 12);
        cashLabelGo.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        var cashValueGo = CreateLabel("CashValue", cashRowGo.transform, "$0.00", CRTThemeData.TextHigh, 16);
        cashValueGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
        cashValueGo.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        refs.CashText = cashValueGo.GetComponent<Text>();

        // Profit row
        var profitRowGo = new GameObject("ProfitRow");
        profitRowGo.transform.SetParent(leftWingGo.transform, false);
        profitRowGo.AddComponent<RectTransform>();
        var profitRowHlg = profitRowGo.AddComponent<HorizontalLayoutGroup>();
        profitRowHlg.spacing = 4f;
        profitRowHlg.childAlignment = TextAnchor.MiddleLeft;
        profitRowHlg.childForceExpandWidth = false;
        profitRowHlg.childForceExpandHeight = true;

        var profitLabelGo = CreateLabel("ProfitLabel", profitRowGo.transform, "Round Profit:", CRTThemeData.TextLow, 12);
        profitLabelGo.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        var profitValueGo = CreateLabel("ProfitValue", profitRowGo.transform, "+$0.00", CRTThemeData.TextHigh, 16);
        profitValueGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
        profitValueGo.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        refs.ProfitText = profitValueGo.GetComponent<Text>();

        // Target row
        var targetRowGo = new GameObject("TargetRow");
        targetRowGo.transform.SetParent(leftWingGo.transform, false);
        targetRowGo.AddComponent<RectTransform>();
        var targetRowHlg = targetRowGo.AddComponent<HorizontalLayoutGroup>();
        targetRowHlg.spacing = 4f;
        targetRowHlg.childAlignment = TextAnchor.MiddleLeft;
        targetRowHlg.childForceExpandWidth = false;
        targetRowHlg.childForceExpandHeight = true;

        var targetLabelGo = CreateLabel("TargetLabel_LW", targetRowGo.transform, "Target:", CRTThemeData.TextLow, 12);
        targetLabelGo.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        var targetValueGo = CreateLabel("TargetValue_LW", targetRowGo.transform, "$0.00 / $0.00", CRTThemeData.TextHigh, 14);
        targetValueGo.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        refs.TargetText = targetValueGo.GetComponent<Text>();

        // Target progress bar — filled horizontal bar below target text
        var targetBarBg = new GameObject("TargetProgressBarBg");
        targetBarBg.transform.SetParent(leftWingGo.transform, false);
        targetBarBg.AddComponent<RectTransform>();
        var targetBarBgLayout = targetBarBg.AddComponent<LayoutElement>();
        targetBarBgLayout.preferredHeight = 6f;
        var targetBarBgImage = targetBarBg.AddComponent<Image>();
        targetBarBgImage.color = new Color(0.15f, 0.15f, 0.2f, 0.5f);

        var targetBarFill = new GameObject("TargetProgressBarFill");
        targetBarFill.transform.SetParent(targetBarBg.transform, false);
        var targetBarFillRect = targetBarFill.AddComponent<RectTransform>();
        targetBarFillRect.anchorMin = Vector2.zero;
        targetBarFillRect.anchorMax = Vector2.one;
        targetBarFillRect.offsetMin = Vector2.zero;
        targetBarFillRect.offsetMax = Vector2.zero;
        var targetBarFillImage = targetBarFill.AddComponent<Image>();
        targetBarFillImage.color = CRTThemeData.TextHigh;
        targetBarFillImage.type = Image.Type.Filled;
        targetBarFillImage.fillMethod = Image.FillMethod.Horizontal;
        targetBarFillImage.fillAmount = 0f;
        refs.TargetProgressBar = targetBarFillImage;

        // ── Story 14.3: Populate Right Wing (Positions/Stats) ────────────────
        rightVlg.spacing = 4f;
        rightVlg.padding = new RectOffset(8, 8, 8, 8);
        rightVlg.childAlignment = TextAnchor.UpperLeft;

        // "POSITIONS" header (CRTThemeData.TextLow, 10pt)
        var posHeaderGo = CreateLabel("PositionsHeader", rightWingGo.transform, "POSITIONS", CRTThemeData.TextLow, 10);
        posHeaderGo.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;

        // Direction row — bold text for LONG/SHORT/FLAT
        var directionGo = CreateLabel("DirectionText", rightWingGo.transform, "FLAT", PositionOverlay.FlatColor, 18);
        directionGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
        directionGo.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        refs.DirectionText = directionGo.GetComponent<Text>();

        // Avg price row — hidden when FLAT
        var avgPriceRowGo = CreateLabel("AvgPriceText", rightWingGo.transform, "Avg: $0.00", CRTThemeData.TextLow, 13);
        avgPriceRowGo.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        refs.AvgPriceText = avgPriceRowGo.GetComponent<Text>();
        refs.AvgPriceRow = avgPriceRowGo;

        // P&L row — hidden when FLAT
        var pnlRowGo = CreateLabel("PnLText", rightWingGo.transform, "P&L: +$0.00", Color.white, 15);
        pnlRowGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
        pnlRowGo.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        refs.PnLText = pnlRowGo.GetComponent<Text>();
        refs.PnlRow = pnlRowGo;

        // Timer row
        var timerRowGo = new GameObject("TimerRow");
        timerRowGo.transform.SetParent(rightWingGo.transform, false);
        timerRowGo.AddComponent<RectTransform>();
        var timerRowHlg = timerRowGo.AddComponent<HorizontalLayoutGroup>();
        timerRowHlg.spacing = 4f;
        timerRowHlg.childAlignment = TextAnchor.MiddleLeft;
        timerRowHlg.childForceExpandWidth = false;
        timerRowHlg.childForceExpandHeight = true;

        var timerLabelGo = CreateLabel("TimerLabel", timerRowGo.transform, "TIME:", CRTThemeData.TextLow, 12);
        timerLabelGo.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        var timerValueGo = CreateLabel("TimerValue", timerRowGo.transform, "0:00", CRTThemeData.TextHigh, 16);
        timerValueGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
        timerValueGo.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        refs.TimerText = timerValueGo.GetComponent<Text>();

        // Timer progress bar — filled horizontal bar below timer row
        var timerBarBg = new GameObject("TimerProgressBarBg");
        timerBarBg.transform.SetParent(rightWingGo.transform, false);
        timerBarBg.AddComponent<RectTransform>();
        var timerBarBgLayout = timerBarBg.AddComponent<LayoutElement>();
        timerBarBgLayout.preferredHeight = 6f;
        var timerBarBgImage = timerBarBg.AddComponent<Image>();
        timerBarBgImage.color = new Color(0.15f, 0.15f, 0.2f, 0.5f);

        var timerBarFill = new GameObject("TimerProgressBarFill");
        timerBarFill.transform.SetParent(timerBarBg.transform, false);
        var timerBarFillRect = timerBarFill.AddComponent<RectTransform>();
        timerBarFillRect.anchorMin = Vector2.zero;
        timerBarFillRect.anchorMax = Vector2.one;
        timerBarFillRect.offsetMin = Vector2.zero;
        timerBarFillRect.offsetMax = Vector2.zero;
        var timerBarFillImage = timerBarFill.AddComponent<Image>();
        timerBarFillImage.color = CRTThemeData.TextHigh;
        timerBarFillImage.type = Image.Type.Filled;
        timerBarFillImage.fillMethod = Image.FillMethod.Horizontal;
        timerBarFillImage.fillAmount = 1f;
        refs.TimerProgressBar = timerBarFillImage;

        // Rep row
        var repRowGo = new GameObject("RepRow");
        repRowGo.transform.SetParent(rightWingGo.transform, false);
        repRowGo.AddComponent<RectTransform>();
        var repRowHlg = repRowGo.AddComponent<HorizontalLayoutGroup>();
        repRowHlg.spacing = 4f;
        repRowHlg.childAlignment = TextAnchor.MiddleLeft;
        repRowHlg.childForceExpandWidth = false;
        repRowHlg.childForceExpandHeight = true;

        var repLabelGo = CreateLabel("RepLabel", repRowGo.transform, "REP:", CRTThemeData.TextLow, 12);
        repLabelGo.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        var repValueGo = CreateLabel("RepValue", repRowGo.transform, "\u2605 0", CRTThemeData.Warning, 16);
        repValueGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
        repValueGo.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        refs.RepText = repValueGo.GetComponent<Text>();

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[Setup] ControlDeck created: bottom-center panel with Left_Wing (Wallet), Center_Core, Right_Wing (Positions/Stats)");
        #endif

        return refs;
    }

    /// <summary>
    /// Generates the stock selection sidebar on the left side of the screen.
    /// Creates entry slots that get populated at round start via StockSidebar.Initialize().
    /// </summary>
    public static StockSidebar ExecuteSidebar(int maxStocks = 4)
    {
        var sidebarParent = new GameObject("StockSidebar");

        // Create Canvas
        var canvasGo = new GameObject("SidebarCanvas");
        canvasGo.transform.SetParent(sidebarParent.transform);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 15;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Sidebar background panel — left side, below top bar
        var sidebarPanel = CreatePanel("SidebarPanel", canvasGo.transform);
        var panelRect = sidebarPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(0f, -TopBarHeight);
        panelRect.sizeDelta = new Vector2(SidebarWidth, -TopBarHeight);
        sidebarPanel.GetComponent<Image>().color = SidebarBgColor;

        // Vertical layout for entries
        var vlg = sidebarPanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4f;
        vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Create entry views
        var entryViews = new StockEntryView[maxStocks];
        for (int i = 0; i < maxStocks; i++)
        {
            entryViews[i] = CreateStockEntryView(i, sidebarPanel.transform);
        }

        // Initialize StockSidebar MonoBehaviour
        var sidebarData = new StockSidebarData();
        var sidebar = sidebarParent.AddComponent<StockSidebar>();
        sidebar.Initialize(sidebarData, entryViews);
        sidebar.SetSidebarBackground(sidebarPanel.GetComponent<Image>());

        // Wire click handlers
        for (int i = 0; i < maxStocks; i++)
        {
            int index = i; // Capture for closure
            var button = entryViews[i].Background.gameObject.AddComponent<Button>();
            button.onClick.AddListener(() => sidebar.OnEntryClicked(index));
        }

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[Setup] StockSidebar created: {maxStocks} entry slots");
        #endif

        return sidebar;
    }

    private static StockEntryView CreateStockEntryView(int index, Transform parent)
    {
        var view = new StockEntryView();

        // Entry container
        var entryGo = CreatePanel($"StockEntry_{index}", parent);
        var entryRect = entryGo.GetComponent<RectTransform>();
        entryRect.sizeDelta = new Vector2(0f, EntryHeight);
        view.Background = entryGo.GetComponent<Image>();
        view.Background.color = new Color(0.05f, 0.07f, 0.18f, 0.6f);

        // Single-row layout: [1] TICK  $100.00  +1.2%
        // All elements vertically centered at Y=0.5

        // Key hint — far left
        var keyHint = CreateLabel($"KeyHint_{index}", entryGo.transform,
            $"[{index + 1}]", LabelColor, 10);
        var keyRect = keyHint.GetComponent<RectTransform>();
        keyRect.anchorMin = new Vector2(0f, 0f);
        keyRect.anchorMax = new Vector2(0f, 1f);
        keyRect.pivot = new Vector2(0f, 0.5f);
        keyRect.anchoredPosition = new Vector2(4f, 0f);
        keyRect.sizeDelta = new Vector2(24f, 0f);
        keyHint.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;

        // Ticker symbol — left-center
        var tickerGo = CreateLabel($"Ticker_{index}", entryGo.transform, "---", ValueColor, 14);
        var tickerRect = tickerGo.GetComponent<RectTransform>();
        tickerRect.anchorMin = new Vector2(0f, 0f);
        tickerRect.anchorMax = new Vector2(0f, 1f);
        tickerRect.pivot = new Vector2(0f, 0.5f);
        tickerRect.anchoredPosition = new Vector2(28f, 0f);
        tickerRect.sizeDelta = new Vector2(52f, 0f);
        tickerGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
        tickerGo.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        view.TickerText = tickerGo.GetComponent<Text>();

        // Price — center
        var priceGo = CreateLabel($"Price_{index}", entryGo.transform, "$0.00", ValueColor, 13);
        var priceRect = priceGo.GetComponent<RectTransform>();
        priceRect.anchorMin = new Vector2(0f, 0f);
        priceRect.anchorMax = new Vector2(0f, 1f);
        priceRect.pivot = new Vector2(0f, 0.5f);
        priceRect.anchoredPosition = new Vector2(82f, 0f);
        priceRect.sizeDelta = new Vector2(72f, 0f);
        priceGo.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        view.PriceText = priceGo.GetComponent<Text>();

        // % Change — right
        var changeGo = CreateLabel($"Change_{index}", entryGo.transform, "+0.0%", NeonGreen, 13);
        var changeRect = changeGo.GetComponent<RectTransform>();
        changeRect.anchorMin = new Vector2(1f, 0f);
        changeRect.anchorMax = new Vector2(1f, 1f);
        changeRect.pivot = new Vector2(1f, 0.5f);
        changeRect.anchoredPosition = new Vector2(-4f, 0f);
        changeRect.sizeDelta = new Vector2(52f, 0f);
        changeGo.GetComponent<Text>().alignment = TextAnchor.MiddleRight;
        view.ChangeText = changeGo.GetComponent<Text>();

        view.SparklineRenderer = null;
        view.SparklineBounds = new Rect(-0.8f, -0.3f, 1.6f, 0.4f);

        // Event indicator icon (hidden by default) — right side of entry
        var indicatorGo = new GameObject($"EventIndicator_{index}");
        indicatorGo.transform.SetParent(entryGo.transform, false);
        var indicatorRect = indicatorGo.AddComponent<RectTransform>();
        indicatorRect.anchorMin = new Vector2(1f, 0f);
        indicatorRect.anchorMax = new Vector2(1f, 1f);
        indicatorRect.pivot = new Vector2(1f, 0.5f);
        indicatorRect.anchoredPosition = new Vector2(-56f, 0f);
        indicatorRect.sizeDelta = new Vector2(24f, 0f);
        var indicatorBg = indicatorGo.AddComponent<Image>();
        indicatorBg.color = Color.clear;
        view.EventIndicator = indicatorBg;
        indicatorGo.SetActive(false);

        var indicatorTextGo = CreateLabel($"IndicatorText_{index}", indicatorGo.transform, "", Color.white, 10);
        indicatorTextGo.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
        view.EventIndicatorText = indicatorTextGo.GetComponent<Text>();

        // Glow border for sector rotation (hidden by default) — outline effect
        var glowGo = new GameObject($"GlowBorder_{index}");
        glowGo.transform.SetParent(entryGo.transform, false);
        var glowRect = glowGo.AddComponent<RectTransform>();
        glowRect.anchorMin = Vector2.zero;
        glowRect.anchorMax = Vector2.one;
        glowRect.offsetMin = new Vector2(-2f, -2f);
        glowRect.offsetMax = new Vector2(2f, 2f);
        var glowImage = glowGo.AddComponent<Image>();
        glowImage.color = Color.clear;
        glowImage.raycastTarget = false;
        view.GlowBorder = glowImage;
        glowGo.SetActive(false);
        // Move glow to back so it doesn't cover entry content
        glowGo.transform.SetAsFirstSibling();

        return view;
    }

    // Story 14.3: ExecutePositionOverlay removed — PositionOverlay now created inside Execute()
    // using Control Deck Right Wing text references from DashboardReferences.

    /// <summary>
    /// Generates the Market Open preview overlay panel.
    /// Shows act/round, stock list, news headline, profit target, and countdown.
    /// </summary>
    public static MarketOpenUI ExecuteMarketOpenUI()
    {
        var overlayParent = new GameObject("MarketOpenOverlay");

        // Create Canvas
        var canvasGo = new GameObject("MarketOpenCanvas");
        canvasGo.transform.SetParent(overlayParent.transform);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Above everything during preview

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Full-screen darkened background
        var bgGo = CreatePanel("MarketOpenBg", canvasGo.transform);
        var bgRect = bgGo.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        bgGo.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.08f, 0.92f);

        // CanvasGroup for fade-in
        var canvasGroup = bgGo.AddComponent<CanvasGroup>();

        // Center panel
        var centerPanel = new GameObject("CenterPanel");
        centerPanel.transform.SetParent(bgGo.transform, false);
        var centerRect = centerPanel.AddComponent<RectTransform>();
        centerRect.anchorMin = new Vector2(0.5f, 0.5f);
        centerRect.anchorMax = new Vector2(0.5f, 0.5f);
        centerRect.pivot = new Vector2(0.5f, 0.5f);
        centerRect.sizeDelta = new Vector2(500f, 400f);

        var vlg = centerPanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 16f;
        vlg.padding = new RectOffset(20, 20, 20, 20);
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Act/Round header
        var headerGo = CreateLabel("Header", centerPanel.transform, "ACT 1 — ROUND 1",
            new Color(0f, 1f, 0.533f, 1f), 28);
        headerGo.GetComponent<Text>().fontStyle = FontStyle.Bold;

        // Stock list
        var stocksLabelGo = CreateLabel("StocksLabel", centerPanel.transform, "AVAILABLE STOCKS", LabelColor, 12);
        var stockListGo = CreateLabel("StockList", centerPanel.transform, "Loading...", ValueColor, 16);
        stockListGo.GetComponent<RectTransform>().sizeDelta = new Vector2(400f, 120f);

        // Headline
        var headlineGo = CreateLabel("Headline", centerPanel.transform, "\"Markets await direction\"",
            new Color(0.8f, 0.85f, 1f, 1f), 18);
        headlineGo.GetComponent<Text>().fontStyle = FontStyle.Italic;

        // Target label
        CreateLabel("TargetLabel", centerPanel.transform, "PROFIT TARGET", LabelColor, 12);

        // Target value (large, prominent)
        var targetGo = CreateLabel("TargetValue", centerPanel.transform, "$0",
            new Color(1f, 0.85f, 0.2f, 1f), 36);
        targetGo.GetComponent<Text>().fontStyle = FontStyle.Bold;

        // Countdown text
        var countdownGo = CreateLabel("Countdown", centerPanel.transform, "Trading begins in 7...",
            LabelColor, 14);

        // Bond Rep payout text (Story 13.6, AC 14) — hidden by default
        var bondRepGo = CreateLabel("BondRepText", centerPanel.transform, "",
            new Color(1f, 0.7f, 0f, 1f), 16);
        bondRepGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
        bondRepGo.SetActive(false);

        // Initialize MarketOpenUI MonoBehaviour
        var marketOpenUI = overlayParent.AddComponent<MarketOpenUI>();
        marketOpenUI.Initialize(
            bgGo,
            headerGo.GetComponent<Text>(),
            stockListGo.GetComponent<Text>(),
            headlineGo.GetComponent<Text>(),
            targetGo.GetComponent<Text>(),
            countdownGo.GetComponent<Text>(),
            canvasGroup,
            bondRepGo.GetComponent<Text>()
        );

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[Setup] MarketOpenUI created: full-screen overlay");
        #endif

        return marketOpenUI;
    }


    /// <summary>
    /// Generates the Run Summary overlay. Shows "MARGIN CALL" or "BULL RUN COMPLETE!" header,
    /// run stats, and "Press any key to continue" prompt.
    /// Subscribes to RunEndedEvent.
    /// </summary>
    public static RunSummaryUI ExecuteRunSummaryUI()
    {
        var overlayParent = new GameObject("RunSummaryOverlay");

        var canvasGo = new GameObject("RunSummaryCanvas");
        canvasGo.transform.SetParent(overlayParent.transform);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 110; // Above everything

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Full-screen darkened background
        var bgGo = CreatePanel("RunSummaryBg", canvasGo.transform);
        var bgRect = bgGo.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        bgGo.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.08f, 0.95f);

        var canvasGroup = bgGo.AddComponent<CanvasGroup>();

        // Center panel
        var centerPanel = new GameObject("CenterPanel");
        centerPanel.transform.SetParent(bgGo.transform, false);
        var centerRect = centerPanel.AddComponent<RectTransform>();
        centerRect.anchorMin = new Vector2(0.5f, 0.5f);
        centerRect.anchorMax = new Vector2(0.5f, 0.5f);
        centerRect.pivot = new Vector2(0.5f, 0.5f);
        centerRect.sizeDelta = new Vector2(500f, 350f);

        var vlg = centerPanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 16f;
        vlg.padding = new RectOffset(20, 20, 20, 20);
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Header
        var headerGo = CreateLabel("Header", centerPanel.transform, "RUN COMPLETE",
            new Color(0f, 1f, 0.4f, 1f), 32);
        headerGo.GetComponent<Text>().fontStyle = FontStyle.Bold;

        // Stats
        var statsGo = CreateLabel("Stats", centerPanel.transform, "", new Color(0.8f, 0.8f, 0.8f, 1f), 16);
        statsGo.GetComponent<RectTransform>().sizeDelta = new Vector2(400f, 120f);

        // Prompt
        var promptGo = CreateLabel("Prompt", centerPanel.transform, "Press any key to continue",
            new Color(0.6f, 0.6f, 0.6f, 1f), 14);

        // Initialize MonoBehaviour
        var runSummaryUI = overlayParent.AddComponent<RunSummaryUI>();
        runSummaryUI.Initialize(
            bgGo,
            headerGo.GetComponent<Text>(),
            statsGo.GetComponent<Text>(),
            promptGo.GetComponent<Text>(),
            canvasGroup
        );

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[Setup] RunSummaryUI created: full-screen overlay");
        #endif

        return runSummaryUI;
    }

    /// <summary>
    /// Generates the Round Results overlay. Shows "ROUND X COMPLETE" with profit,
    /// target status, and total cash. Subscribes to RoundCompletedEvent.
    /// </summary>
    public static RoundResultsUI ExecuteRoundResultsUI()
    {
        var overlayParent = new GameObject("RoundResultsOverlay");

        var canvasGo = new GameObject("RoundResultsCanvas");
        canvasGo.transform.SetParent(overlayParent.transform);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 105; // Above game UI, below RunSummary

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Full-screen darkened background
        var bgGo = CreatePanel("RoundResultsBg", canvasGo.transform);
        var bgRect = bgGo.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        bgGo.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.08f, 0.9f);

        var canvasGroup = bgGo.AddComponent<CanvasGroup>();

        // Center panel
        var centerPanel = new GameObject("CenterPanel");
        centerPanel.transform.SetParent(bgGo.transform, false);
        var centerRect = centerPanel.AddComponent<RectTransform>();
        centerRect.anchorMin = new Vector2(0.5f, 0.5f);
        centerRect.anchorMax = new Vector2(0.5f, 0.5f);
        centerRect.pivot = new Vector2(0.5f, 0.5f);
        centerRect.sizeDelta = new Vector2(400f, 250f);

        var vlg = centerPanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 12f;
        vlg.padding = new RectOffset(20, 20, 20, 20);
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Header
        var headerGo = CreateLabel("Header", centerPanel.transform, "ROUND 1 COMPLETE",
            new Color(0f, 1f, 0.4f, 1f), 28);
        headerGo.GetComponent<Text>().fontStyle = FontStyle.Bold;

        // Stats
        var statsGo = CreateLabel("Stats", centerPanel.transform, "", new Color(0.8f, 0.8f, 0.8f, 1f), 16);
        statsGo.GetComponent<RectTransform>().sizeDelta = new Vector2(350f, 80f);

        // Checkmark/X indicator
        var checkGo = CreateLabel("Checkmark", centerPanel.transform, "\u2713",
            new Color(0f, 1f, 0.4f, 1f), 48);

        // Initialize MonoBehaviour
        var roundResultsUI = overlayParent.AddComponent<RoundResultsUI>();
        roundResultsUI.Initialize(
            bgGo,
            headerGo.GetComponent<Text>(),
            statsGo.GetComponent<Text>(),
            checkGo.GetComponent<Text>(),
            canvasGroup
        );

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[Setup] RoundResultsUI created: full-screen overlay");
        #endif

        return roundResultsUI;
    }

    /// <summary>
    /// Generates the Tier Transition overlay panel with dramatic act reveal layout.
    /// Shows "ACT X" (large header), tier subtitle, and tagline with fade animation.
    /// Replaces the simpler ActTransitionUI for richer visual presentation.
    /// Subscribes to ActTransitionEvent via TierTransitionUI.
    /// </summary>
    public static TierTransitionUI ExecuteTierTransitionUI()
    {
        var overlayParent = new GameObject("TierTransitionOverlay");

        var canvasGo = new GameObject("TierTransitionCanvas");
        canvasGo.transform.SetParent(overlayParent.transform);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 108; // Above game UI, below RunSummary

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Full-screen darkened background
        var bgGo = CreatePanel("TierTransitionBg", canvasGo.transform);
        var bgRect = bgGo.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        bgGo.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.08f, 0.95f);

        var canvasGroup = bgGo.AddComponent<CanvasGroup>();

        // Center panel for vertical layout
        var centerPanel = new GameObject("CenterPanel");
        centerPanel.transform.SetParent(bgGo.transform, false);
        var centerRect = centerPanel.AddComponent<RectTransform>();
        centerRect.anchorMin = new Vector2(0.5f, 0.5f);
        centerRect.anchorMax = new Vector2(0.5f, 0.5f);
        centerRect.pivot = new Vector2(0.5f, 0.5f);
        centerRect.sizeDelta = new Vector2(700f, 250f);

        var vlg = centerPanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 12f;
        vlg.padding = new RectOffset(20, 20, 20, 20);
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // "ACT 2" — large, bold, gold header
        var actHeaderGo = CreateLabel("ActHeader", centerPanel.transform, "ACT 2",
            new Color(1f, 0.85f, 0f, 1f), 48);
        actHeaderGo.GetComponent<Text>().fontStyle = FontStyle.Bold;

        // "LOW-VALUE STOCKS" — subtitle, white
        var subtitleGo = CreateLabel("TierSubtitle", centerPanel.transform, "LOW-VALUE STOCKS",
            Color.white, 28);
        subtitleGo.GetComponent<Text>().fontStyle = FontStyle.Bold;

        // "Rising Stakes — Trends and Reversals" — tagline, smaller, muted color
        var taglineGo = CreateLabel("Tagline", centerPanel.transform,
            "Rising Stakes \u2014 Trends and Reversals",
            new Color(0.7f, 0.7f, 0.8f, 1f), 18);
        taglineGo.GetComponent<Text>().fontStyle = FontStyle.Italic;

        // Initialize TierTransitionUI MonoBehaviour
        var tierTransitionUI = overlayParent.AddComponent<TierTransitionUI>();
        tierTransitionUI.Initialize(
            bgGo,
            actHeaderGo.GetComponent<Text>(),
            subtitleGo.GetComponent<Text>(),
            taglineGo.GetComponent<Text>(),
            canvasGroup
        );

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[Setup] TierTransitionUI created: full-screen overlay with act/tier/tagline");
        #endif

        return tierTransitionUI;
    }

    /// <summary>
    /// Generates the NewsBanner overlay at the top of the screen.
    /// Shows event headline banners that slide down when market events fire.
    /// </summary>
    public static NewsBanner ExecuteNewsBanner()
    {
        var bannerParent = new GameObject("NewsBannerOverlay");

        var canvasGo = new GameObject("NewsBannerCanvas");
        canvasGo.transform.SetParent(bannerParent.transform);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30; // Above HUD, below overlays

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Banner container — top of screen
        var containerGo = new GameObject("BannerContainer");
        containerGo.transform.SetParent(canvasGo.transform, false);
        var containerRect = containerGo.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0f, 1f);
        containerRect.anchorMax = new Vector2(1f, 1f);
        containerRect.pivot = new Vector2(0.5f, 1f);
        containerRect.anchoredPosition = new Vector2(0f, -TopBarHeight - 4f);
        containerRect.sizeDelta = new Vector2(0f, 200f); // Room for stacked banners

        var newsBanner = bannerParent.AddComponent<NewsBanner>();
        newsBanner.Initialize(containerGo.transform);

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[Setup] NewsBanner created: top-of-screen overlay");
        #endif

        return newsBanner;
    }

    /// <summary>
    /// Generates the NewsTicker scrolling text bar at the bottom of the screen.
    /// Headlines scroll left-to-right as market events fire.
    /// </summary>
    public static NewsTicker ExecuteNewsTicker()
    {
        var tickerParent = new GameObject("NewsTickerBar");

        var canvasGo = new GameObject("NewsTickerCanvas");
        canvasGo.transform.SetParent(tickerParent.transform);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 22; // Above chart, below timer

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Ticker bar — bottom of screen
        float tickerHeight = 28f;
        var barGo = CreatePanel("TickerBar", canvasGo.transform);
        var barRect = barGo.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0f, 0f);
        barRect.anchorMax = new Vector2(1f, 0f);
        barRect.pivot = new Vector2(0.5f, 0f);
        barRect.anchoredPosition = Vector2.zero;
        barRect.sizeDelta = new Vector2(0f, tickerHeight);
        barGo.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.12f, 0.85f);

        // Scroll container — clipped by parent
        var scrollGo = new GameObject("ScrollContainer");
        scrollGo.transform.SetParent(barGo.transform, false);
        var scrollRect = scrollGo.AddComponent<RectTransform>();
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = new Vector2(8f, 0f);
        scrollRect.offsetMax = new Vector2(-8f, 0f);

        // Mask to clip scrolling text
        var maskImage = barGo.AddComponent<Mask>();
        maskImage.showMaskGraphic = true;

        float containerWidth = 1920f; // Reference width
        var newsTicker = tickerParent.AddComponent<NewsTicker>();
        newsTicker.Initialize(scrollGo.transform, containerWidth);

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[Setup] NewsTicker created: bottom-of-screen scrolling bar");
        #endif

        return newsTicker;
    }

    /// <summary>
    /// Generates the ScreenEffects full-screen overlay for dramatic event visuals.
    /// Screen shake, red pulse (MarketCrash), green tint (BullRun), red flash (FlashCrash).
    /// </summary>
    public static ScreenEffects ExecuteScreenEffects()
    {
        var effectsParent = new GameObject("ScreenEffectsOverlay");

        var canvasGo = new GameObject("ScreenEffectsCanvas");
        canvasGo.transform.SetParent(effectsParent.transform);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10; // Behind HUD, behind everything interactive

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        // No GraphicRaycaster — overlay should not block input

        // Shake container — all effect images parented here for shake offset
        var shakeGo = new GameObject("ShakeContainer");
        shakeGo.transform.SetParent(canvasGo.transform, false);
        var shakeRect = shakeGo.AddComponent<RectTransform>();
        shakeRect.anchorMin = Vector2.zero;
        shakeRect.anchorMax = Vector2.one;
        shakeRect.offsetMin = Vector2.zero;
        shakeRect.offsetMax = Vector2.zero;

        // Red pulse overlay (MarketCrash)
        var redPulseGo = CreatePanel("RedPulseImage", shakeGo.transform);
        var redPulseRect = redPulseGo.GetComponent<RectTransform>();
        redPulseRect.anchorMin = Vector2.zero;
        redPulseRect.anchorMax = Vector2.one;
        redPulseRect.offsetMin = Vector2.zero;
        redPulseRect.offsetMax = Vector2.zero;
        var redPulseImage = redPulseGo.GetComponent<Image>();
        redPulseImage.color = new Color(0.8f, 0f, 0f, 0f);
        redPulseImage.raycastTarget = false;

        // Green tint overlay (BullRun)
        var greenTintGo = CreatePanel("GreenTintImage", shakeGo.transform);
        var greenTintRect = greenTintGo.GetComponent<RectTransform>();
        greenTintRect.anchorMin = Vector2.zero;
        greenTintRect.anchorMax = Vector2.one;
        greenTintRect.offsetMin = Vector2.zero;
        greenTintRect.offsetMax = Vector2.zero;
        var greenTintImage = greenTintGo.GetComponent<Image>();
        greenTintImage.color = new Color(0f, 0.8f, 0.267f, 0f);
        greenTintImage.raycastTarget = false;

        // Flash overlay (FlashCrash)
        var flashGo = CreatePanel("FlashImage", shakeGo.transform);
        var flashRect = flashGo.GetComponent<RectTransform>();
        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.offsetMin = Vector2.zero;
        flashRect.offsetMax = Vector2.zero;
        var flashImage = flashGo.GetComponent<Image>();
        flashImage.color = new Color(1f, 0f, 0f, 0f);
        flashImage.raycastTarget = false;

        var screenEffects = effectsParent.AddComponent<ScreenEffects>();
        screenEffects.Initialize(shakeRect, redPulseImage, greenTintImage, flashImage);

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[Setup] ScreenEffects created: full-screen overlay with shake/pulse/tint/flash");
        #endif

        return screenEffects;
    }

    /// <summary>
    /// Generates the Store UI overlay with multi-panel Balatro-style layout (Epic 13.2).
    /// Top: control panel (Next Round + Reroll) + 3 relic card slots.
    /// Bottom: 3 panels — Expansions (left), Insider Tips (center), Bonds (right).
    /// Currency bar: Reputation + Cash.
    /// Wired to ShopState via ShopState.ShopUIInstance.
    /// </summary>
    public static ShopUI ExecuteStoreUI()
    {
        var overlayParent = new GameObject("StoreOverlay");

        var canvasGo = new GameObject("StoreCanvas");
        canvasGo.transform.SetParent(overlayParent.transform);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 102; // Above game UI, below RunSummary

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Full-screen darkened background
        var bgGo = CreatePanel("StoreBg", canvasGo.transform);
        var bgRect = bgGo.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        bgGo.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.08f, 0.95f);

        var canvasGroup = bgGo.AddComponent<CanvasGroup>();

        // ── Header: "STORE — ROUND X" ──
        var headerGo = CreateLabel("StoreHeader", bgGo.transform, "STORE",
            new Color(0f, 1f, 0.533f, 1f), 28);
        headerGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
        var headerRect = headerGo.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0.5f, 1f);
        headerRect.anchorMax = new Vector2(0.5f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = new Vector2(0f, -15f);
        headerRect.sizeDelta = new Vector2(400f, 35f);

        // ── Currency bar: Reputation (left) + Cash (right) ──
        var currencyBar = new GameObject("CurrencyBar");
        currencyBar.transform.SetParent(bgGo.transform, false);
        var currencyBarRect = currencyBar.AddComponent<RectTransform>();
        currencyBarRect.anchorMin = new Vector2(0.5f, 1f);
        currencyBarRect.anchorMax = new Vector2(0.5f, 1f);
        currencyBarRect.pivot = new Vector2(0.5f, 1f);
        currencyBarRect.anchoredPosition = new Vector2(0f, -50f);
        currencyBarRect.sizeDelta = new Vector2(400f, 30f);
        var currencyHlg = currencyBar.AddComponent<HorizontalLayoutGroup>();
        currencyHlg.spacing = 40f;
        currencyHlg.childAlignment = TextAnchor.MiddleCenter;
        currencyHlg.childForceExpandWidth = true;
        currencyHlg.childForceExpandHeight = true;

        var repGo = CreateLabel("StoreReputation", currencyBar.transform, "\u2605 0",
            ShopUI.ReputationColor, 22);
        repGo.GetComponent<Text>().fontStyle = FontStyle.Bold;

        var cashGo = CreateLabel("StoreCash", currencyBar.transform, "$ 0",
            ShopUI.CashColor, 22);
        cashGo.GetComponent<Text>().fontStyle = FontStyle.Bold;

        // ── TOP SECTION: Control panel (left) + 3 Relic slots (right) ──
        var topSection = new GameObject("TopSection");
        topSection.transform.SetParent(bgGo.transform, false);
        var topRect = topSection.AddComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0.03f, 0.45f);
        topRect.anchorMax = new Vector2(0.97f, 0.93f);
        topRect.offsetMin = Vector2.zero;
        topRect.offsetMax = Vector2.zero;

        var topHlg = topSection.AddComponent<HorizontalLayoutGroup>();
        topHlg.spacing = 15f;
        topHlg.padding = new RectOffset(10, 10, 10, 10);
        topHlg.childAlignment = TextAnchor.MiddleCenter;
        topHlg.childForceExpandWidth = false;
        topHlg.childForceExpandHeight = true;

        // Control panel (Next Round + Reroll buttons)
        var controlPanel = CreatePanel("ControlPanel", topSection.transform);
        controlPanel.GetComponent<Image>().color = new Color(0.05f, 0.07f, 0.15f, 0.9f);
        var controlLayout = controlPanel.AddComponent<LayoutElement>();
        controlLayout.preferredWidth = 180f;
        controlLayout.flexibleWidth = 0f;

        var controlVlg = controlPanel.AddComponent<VerticalLayoutGroup>();
        controlVlg.spacing = 12f;
        controlVlg.padding = new RectOffset(15, 15, 20, 20);
        controlVlg.childAlignment = TextAnchor.MiddleCenter;
        controlVlg.childForceExpandWidth = true;
        controlVlg.childForceExpandHeight = false;

        // "Next Round" button
        var nextRoundBtnGo = CreatePanel("NextRoundBtn", controlPanel.transform);
        nextRoundBtnGo.GetComponent<Image>().color = new Color(0.15f, 0.3f, 0.6f, 1f);
        var nextRoundBtnLayout = nextRoundBtnGo.AddComponent<LayoutElement>();
        nextRoundBtnLayout.preferredHeight = 50f;
        var nextRoundButton = nextRoundBtnGo.AddComponent<Button>();
        var nextRoundLabel = CreateLabel("NextRoundBtnText", nextRoundBtnGo.transform, "NEXT ROUND", Color.white, 16);
        nextRoundLabel.GetComponent<Text>().fontStyle = FontStyle.Bold;
        nextRoundLabel.GetComponent<Text>().raycastTarget = false;

        // "Reroll" button
        var rerollBtnGo = CreatePanel("RerollBtn", controlPanel.transform);
        rerollBtnGo.GetComponent<Image>().color = new Color(0.4f, 0.2f, 0.5f, 1f);
        var rerollBtnLayout = rerollBtnGo.AddComponent<LayoutElement>();
        rerollBtnLayout.preferredHeight = 40f;
        var rerollButton = rerollBtnGo.AddComponent<Button>();
        var rerollLabel = CreateLabel("RerollBtnText", rerollBtnGo.transform, "REROLL", Color.white, 14);
        rerollLabel.GetComponent<Text>().fontStyle = FontStyle.Bold;
        rerollLabel.GetComponent<Text>().raycastTarget = false;

        var rerollCostGo = CreateLabel("RerollCost", controlPanel.transform, "\u2605 1",
            ShopUI.ReputationColor, 12);
        rerollCostGo.GetComponent<Text>().raycastTarget = false;

        // 3 Relic card slots
        var relicSlots = new ShopUI.RelicSlotView[3];
        for (int i = 0; i < 3; i++)
        {
            relicSlots[i] = CreateRelicSlot(i, topSection.transform);
        }

        // ── BOTTOM SECTION: 3 panels (Expansions, Insider Tips, Bonds) ──
        var bottomSection = new GameObject("BottomSection");
        bottomSection.transform.SetParent(bgGo.transform, false);
        var bottomRect = bottomSection.AddComponent<RectTransform>();
        bottomRect.anchorMin = new Vector2(0.03f, 0.02f);
        bottomRect.anchorMax = new Vector2(0.97f, 0.44f);
        bottomRect.offsetMin = Vector2.zero;
        bottomRect.offsetMax = Vector2.zero;

        var bottomHlg = bottomSection.AddComponent<HorizontalLayoutGroup>();
        bottomHlg.spacing = 10f;
        bottomHlg.padding = new RectOffset(5, 5, 5, 5);
        bottomHlg.childAlignment = TextAnchor.MiddleCenter;
        bottomHlg.childForceExpandWidth = true;
        bottomHlg.childForceExpandHeight = true;

        // Expansions panel (left)
        var expansionsPanel = CreateStorePanel("ExpansionsPanel", "EXPANSIONS", bottomSection.transform);

        // Insider Tips panel (center)
        var tipsPanel = CreateStorePanel("TipsPanel", "INSIDER TIPS", bottomSection.transform);

        // Bonds panel (right)
        var bondsPanel = CreateStorePanel("BondsPanel", "BONDS", bottomSection.transform);

        // ── Initialize ShopUI MonoBehaviour ──
        var shopUI = overlayParent.AddComponent<ShopUI>();
        shopUI.Initialize(
            bgGo,
            repGo.GetComponent<Text>(),
            cashGo.GetComponent<Text>(),
            headerGo.GetComponent<Text>(),
            relicSlots,
            canvasGroup
        );
        shopUI.SetNextRoundButton(nextRoundButton);
        shopUI.SetRerollButton(rerollButton, rerollCostGo.GetComponent<Text>());
        shopUI.SetBottomPanels(expansionsPanel, tipsPanel, bondsPanel);

        // Wire to ShopState
        ShopState.ShopUIInstance = shopUI;

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[Setup] StoreUI created: multi-panel store layout with 3 relic slots + 3 bottom panels (13.2)");
        #endif

        return shopUI;
    }

    private static ShopUI.RelicSlotView CreateRelicSlot(int index, Transform parent)
    {
        var view = new ShopUI.RelicSlotView();

        // Card background
        var cardGo = CreatePanel($"RelicSlot_{index}", parent);
        view.CardBackground = cardGo.GetComponent<Image>();
        view.CardBackground.color = ShopUI.RelicCardColor;
        view.Root = cardGo;
        view.Group = cardGo.AddComponent<CanvasGroup>();

        var cardLayout = cardGo.AddComponent<LayoutElement>();
        cardLayout.flexibleWidth = 1f;

        // Vertical layout inside card
        var vlg = cardGo.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6f;
        vlg.padding = new RectOffset(12, 12, 12, 12);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Accent bar (amber, replaces old rarity badge — Story 13.9 cleanup)
        var accentGo = CreatePanel($"AccentBar_{index}", cardGo.transform);
        var accentRect = accentGo.GetComponent<RectTransform>();
        accentRect.sizeDelta = new Vector2(80f, 4f);
        var accentLayoutElem = accentGo.AddComponent<LayoutElement>();
        accentLayoutElem.preferredHeight = 4f;
        accentGo.GetComponent<Image>().color = ShopUI.ReputationColor;
        accentGo.GetComponent<Image>().raycastTarget = false;

        // Item name — bold/larger font for legibility (Story 13.8, AC 7)
        var nameGo = CreateLabel($"Name_{index}", cardGo.transform, "Empty",
            Color.white, 18);
        nameGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
        view.NameText = nameGo.GetComponent<Text>();
        view.NameText.raycastTarget = false;

        // Description text — adequate contrast and line spacing (Story 13.8, AC 7)
        var descGo = CreateLabel($"Desc_{index}", cardGo.transform, "",
            new Color(0.8f, 0.8f, 0.85f, 1f), 13);
        var descRect = descGo.GetComponent<RectTransform>();
        descRect.sizeDelta = new Vector2(200f, 60f);
        view.DescriptionText = descGo.GetComponent<Text>();
        view.DescriptionText.raycastTarget = false;
        view.DescriptionText.lineSpacing = 1.1f;

        // Cost — prominently sized with currency icon (Story 13.8, AC 7)
        var costGo = CreateLabel($"Cost_{index}", cardGo.transform, "",
            Color.white, 22);
        costGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
        view.CostText = costGo.GetComponent<Text>();
        view.CostText.raycastTarget = false;

        // Purchase button
        var btnGo = CreatePanel($"BuyBtn_{index}", cardGo.transform);
        var btnRect = btnGo.GetComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(180f, 36f);
        var btnLayoutElem = btnGo.AddComponent<LayoutElement>();
        btnLayoutElem.minHeight = 36f;
        btnLayoutElem.preferredHeight = 36f;
        btnGo.GetComponent<Image>().color = new Color(0f, 0.6f, 0.3f, 1f);
        view.PurchaseButton = btnGo.AddComponent<Button>();

        var btnLabel = CreateLabel($"BuyBtnText_{index}", btnGo.transform, "BUY",
            Color.white, 14);
        btnLabel.GetComponent<Text>().fontStyle = FontStyle.Bold;
        view.ButtonText = btnLabel.GetComponent<Text>();
        view.ButtonText.raycastTarget = false;

        return view;
    }

    private static GameObject CreateStorePanel(string name, string headerText, Transform parent)
    {
        var panelGo = CreatePanel(name, parent);
        panelGo.GetComponent<Image>().color = ShopUI.PanelBgColor;

        // Add border effect via outline or nested panel
        var outline = panelGo.AddComponent<Outline>();
        outline.effectColor = ShopUI.PanelBorderColor;
        outline.effectDistance = new Vector2(2f, 2f);

        var vlg = panelGo.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8f;
        vlg.padding = new RectOffset(12, 12, 10, 10);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Panel header label
        var headerGo = CreateLabel($"{name}Header", panelGo.transform, headerText,
            ShopUI.PanelHeaderColor, 16);
        headerGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
        headerGo.GetComponent<Text>().raycastTarget = false;
        var headerLayout = headerGo.AddComponent<LayoutElement>();
        headerLayout.preferredHeight = 28f;

        // Placeholder content area
        var contentGo = CreateLabel($"{name}Content", panelGo.transform, "Coming soon...",
            new Color(0.4f, 0.4f, 0.5f, 0.7f), 12);
        contentGo.GetComponent<Text>().raycastTarget = false;

        return panelGo;
    }

    /// <summary>
    /// Generates the EventPopup center-screen overlay for dramatic market event display.
    /// Large popup with directional arrow, headline, affected tickers.
    /// Pauses game, then flies up (positive) or down (negative).
    /// </summary>
    public static EventPopup ExecuteEventPopup()
    {
        var popupParent = new GameObject("EventPopupOverlay");

        var canvasGo = new GameObject("EventPopupCanvas");
        canvasGo.transform.SetParent(popupParent.transform);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50; // Above NewsBanner (30), below state overlays (100+)

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        // No GraphicRaycaster — popup should not block input during pause

        // Center-screen popup panel (~60% screen width)
        var popupPanel = CreatePanel("PopupPanel", canvasGo.transform);
        var panelRect = popupPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(1152f, 200f); // ~60% of 1920
        var bgImage = popupPanel.GetComponent<Image>();
        bgImage.color = EventPopup.PositiveColor;

        var canvasGroup = popupPanel.AddComponent<CanvasGroup>();

        // Vertical layout inside popup
        var vlg = popupPanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8f;
        vlg.padding = new RectOffset(30, 30, 20, 20);
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Directional arrow (large)
        var arrowGo = CreateLabel("ArrowText", popupPanel.transform, EventPopup.UpArrow,
            EventPopup.PositiveTextColor, 48);
        arrowGo.GetComponent<Text>().fontStyle = FontStyle.Bold;

        // Headline text (large, bold)
        var headlineGo = CreateLabel("HeadlineText", popupPanel.transform, "BREAKING NEWS",
            Color.white, 26);
        headlineGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
        var headlineRect = headlineGo.GetComponent<RectTransform>();
        headlineRect.sizeDelta = new Vector2(1000f, 40f);

        // Affected ticker symbols
        var tickerGo = CreateLabel("TickerText", popupPanel.transform, "ACME",
            EventPopup.PositiveTextColor, 18);

        // Initialize EventPopup MonoBehaviour
        var eventPopup = popupParent.AddComponent<EventPopup>();
        eventPopup.Initialize(
            popupPanel,
            bgImage,
            arrowGo.GetComponent<Text>(),
            headlineGo.GetComponent<Text>(),
            tickerGo.GetComponent<Text>(),
            canvasGroup,
            panelRect
        );

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[Setup] EventPopup created: center-screen dramatic event overlay");
        #endif

        return eventPopup;
    }

    /// <summary>
    /// Generates the trade feedback overlay at center screen.
    /// Shows brief text like "SHORTED ACME x10" that fades out after 1.5s.
    /// FIX-16: Moved from top bar to center screen for visibility during fast trading.
    /// </summary>
    public static TradeFeedback ExecuteTradeFeedback()
    {
        var feedbackParent = new GameObject("TradeFeedback");

        var canvasGo = new GameObject("TradeFeedbackCanvas");
        canvasGo.transform.SetParent(feedbackParent.transform);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 23; // Above HUD and NewsTicker (22), below EventPopup (50)
        // No GraphicRaycaster — feedback should not block input

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        // Feedback container — lower-center screen, above trade panel gaze path (FIX-16)
        var containerGo = CreatePanel("FeedbackContainer", canvasGo.transform);
        var containerRect = containerGo.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = new Vector2(0f, -120f);
        containerRect.sizeDelta = new Vector2(420f, 50f);
        containerGo.GetComponent<Image>().color = new Color(0.05f, 0.07f, 0.18f, 0.85f);
        containerGo.GetComponent<Image>().raycastTarget = false;

        var canvasGroup = containerGo.AddComponent<CanvasGroup>();

        var feedbackTextGo = CreateLabel("FeedbackText", containerGo.transform, "",
            Color.white, 24);
        feedbackTextGo.GetComponent<Text>().fontStyle = FontStyle.Bold;

        var tradeFeedback = feedbackParent.AddComponent<TradeFeedback>();
        tradeFeedback.Initialize(feedbackTextGo.GetComponent<Text>(), canvasGroup);

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[Setup] TradeFeedback created: center screen (FIX-16)");
        #endif

        return tradeFeedback;
    }

    /// <summary>
    /// Generates the trade panel at bottom-center of screen.
    /// FIX-15: Always x1 quantity, no preset buttons.
    /// Layout: quantity display row on top, SELL (red) button left, BUY (green) button right.
    /// BUY/SELL buttons publish TradeButtonPressedEvent for GameRunner to handle.
    /// </summary>
    public static QuantitySelector ExecuteTradePanel()
    {
        var panelParent = new GameObject("TradePanel");

        var canvasGo = new GameObject("TradePanelCanvas");
        canvasGo.transform.SetParent(panelParent.transform);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 24; // Between feedback (23) and timer (25)

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Main container — centered at bottom above inventory bar
        var containerGo = CreatePanel("TradePanelContainer", canvasGo.transform);
        var containerRect = containerGo.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0f);
        containerRect.anchorMax = new Vector2(0.5f, 0f);
        containerRect.pivot = new Vector2(0.5f, 0f);
        containerRect.anchoredPosition = new Vector2(0f, 82f); // Above inventory bar + news ticker
        containerRect.sizeDelta = new Vector2(420f, 60f); // Buttons only, no qty row
        containerGo.GetComponent<Image>().color = BarBackgroundColor;

        var mainLayout = containerGo.AddComponent<VerticalLayoutGroup>();
        mainLayout.spacing = 6f;
        mainLayout.padding = new RectOffset(12, 12, 6, 6);
        mainLayout.childAlignment = TextAnchor.MiddleCenter;
        mainLayout.childForceExpandWidth = true;
        mainLayout.childForceExpandHeight = false;

        // === BUY and SELL buttons (qty row removed) ===
        var buttonRow = new GameObject("ButtonRow");
        buttonRow.transform.SetParent(containerGo.transform, false);
        buttonRow.AddComponent<RectTransform>();
        var buttonRowLayout = buttonRow.AddComponent<LayoutElement>();
        buttonRowLayout.preferredHeight = 48f;
        var buttonHlg = buttonRow.AddComponent<HorizontalLayoutGroup>();
        buttonHlg.spacing = 20f;
        buttonHlg.childAlignment = TextAnchor.MiddleCenter;
        buttonHlg.childForceExpandWidth = true;
        buttonHlg.childForceExpandHeight = true;

        // SELL button — red, left side
        var sellBtnGo = CreatePanel("SellButton", buttonRow.transform);
        sellBtnGo.GetComponent<Image>().color = TradingHUD.LossRed;
        var sellBtnLayout = sellBtnGo.AddComponent<LayoutElement>();
        sellBtnLayout.preferredWidth = 160f;
        sellBtnLayout.preferredHeight = 48f;
        var sellButton = sellBtnGo.AddComponent<Button>();
        var sellLabel = CreateLabel("SellButtonText", sellBtnGo.transform, "SELL", Color.white, 22);
        sellLabel.GetComponent<Text>().fontStyle = FontStyle.Bold;
        sellLabel.GetComponent<Text>().raycastTarget = false;

        // BUY button — green, right side
        var buyBtnGo = CreatePanel("BuyButton", buttonRow.transform);
        buyBtnGo.GetComponent<Image>().color = TradingHUD.ProfitGreen;
        var buyBtnLayout = buyBtnGo.AddComponent<LayoutElement>();
        buyBtnLayout.preferredWidth = 160f;
        buyBtnLayout.preferredHeight = 48f;
        var buyButton = buyBtnGo.AddComponent<Button>();
        var buyLabel = CreateLabel("BuyButtonText", buyBtnGo.transform, "BUY", Color.white, 22);
        buyLabel.GetComponent<Text>().fontStyle = FontStyle.Bold;
        buyLabel.GetComponent<Text>().raycastTarget = false;

        // === Cooldown overlay — grey panel covering buy/sell buttons ===
        // Parented to canvas (not container) to avoid VerticalLayoutGroup interference
        var cooldownOverlayGo = CreatePanel("CooldownOverlay", canvasGo.transform);
        var overlayRect = cooldownOverlayGo.GetComponent<RectTransform>();
        // Match the container's position and size exactly
        overlayRect.anchorMin = new Vector2(0.5f, 0f);
        overlayRect.anchorMax = new Vector2(0.5f, 0f);
        overlayRect.pivot = new Vector2(0.5f, 0f);
        overlayRect.anchoredPosition = new Vector2(0f, 82f);
        overlayRect.sizeDelta = new Vector2(420f, 60f);
        cooldownOverlayGo.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.75f);

        var cooldownTimerGo = CreateLabel("CooldownTimer", cooldownOverlayGo.transform, "",
            new Color(1f, 0.85f, 0.2f, 1f), 22);
        cooldownTimerGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
        cooldownTimerGo.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
        cooldownTimerGo.GetComponent<Text>().raycastTarget = false;
        var cdTimerRect = cooldownTimerGo.GetComponent<RectTransform>();
        cdTimerRect.anchorMin = Vector2.zero;
        cdTimerRect.anchorMax = Vector2.one;
        cdTimerRect.offsetMin = Vector2.zero;
        cdTimerRect.offsetMax = Vector2.zero;

        cooldownOverlayGo.SetActive(false);

        // === SHORT section — full-width button + inline P&L panel below buy/sell ===
        var shortPink = new Color(1f, 0.2f, 0.6f, 1f);

        // Short container positioned below the buy/sell container
        var shortContainerGo = CreatePanel("ShortContainer", canvasGo.transform);
        var shortContainerRect = shortContainerGo.GetComponent<RectTransform>();
        shortContainerRect.anchorMin = new Vector2(0.5f, 0f);
        shortContainerRect.anchorMax = new Vector2(0.5f, 0f);
        shortContainerRect.pivot = new Vector2(0.5f, 1f);
        shortContainerRect.anchoredPosition = new Vector2(0f, 80f); // Just below buy/sell (82 - 2)
        shortContainerRect.sizeDelta = new Vector2(420f, 40f); // Grows when P&L visible
        shortContainerGo.GetComponent<Image>().color = new Color(0.08f, 0.05f, 0.15f, 0.85f);

        var shortVlg = shortContainerGo.AddComponent<VerticalLayoutGroup>();
        shortVlg.spacing = 4f;
        shortVlg.padding = new RectOffset(12, 12, 4, 4);
        shortVlg.childAlignment = TextAnchor.MiddleCenter;
        shortVlg.childForceExpandWidth = true;
        shortVlg.childForceExpandHeight = false;
        var shortContainerFitter = shortContainerGo.AddComponent<ContentSizeFitter>();
        shortContainerFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // SHORT button — full width
        var shortBtnGo = CreatePanel("ShortButton", shortContainerGo.transform);
        shortBtnGo.GetComponent<Image>().color = shortPink;
        var shortBtnLayout = shortBtnGo.AddComponent<LayoutElement>();
        shortBtnLayout.preferredHeight = 32f;
        var shortButton = shortBtnGo.AddComponent<Button>();
        var shortBtnLabel = CreateLabel("ShortButtonText", shortBtnGo.transform, "SHORT", Color.white, 16);
        shortBtnLabel.GetComponent<Text>().fontStyle = FontStyle.Bold;
        shortBtnLabel.GetComponent<Text>().raycastTarget = false;

        // Wire SHORT button click
        shortButton.onClick.AddListener(() =>
        {
            var runner = Object.FindObjectOfType<GameRunner>();
            if (runner != null) runner.HandleShortInput();
        });

        // Short P&L info section — hidden until short is active
        var shortPnlGo = new GameObject("ShortPnlPanel");
        shortPnlGo.transform.SetParent(shortContainerGo.transform, false);
        shortPnlGo.AddComponent<RectTransform>();

        var shortPnlVlg = shortPnlGo.AddComponent<VerticalLayoutGroup>();
        shortPnlVlg.spacing = 1f;
        shortPnlVlg.childAlignment = TextAnchor.MiddleCenter;
        shortPnlVlg.childForceExpandWidth = true;
        shortPnlVlg.childForceExpandHeight = false;

        var shortEntryGo = CreateLabel("ShortEntryText", shortPnlGo.transform, "Entry: $0.00",
            LabelColor, 12);
        shortEntryGo.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

        // P&L row — entry + value side by side
        var shortValueGo = CreateLabel("ShortPnlValue", shortPnlGo.transform, "P&L: +$0.00",
            Color.white, 14);
        shortValueGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
        shortValueGo.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

        var shortCountdownGo = CreateLabel("ShortCountdown", shortPnlGo.transform, "",
            new Color(1f, 0.85f, 0.2f, 1f), 12);
        shortCountdownGo.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

        shortPnlGo.SetActive(false); // Hidden until short is active

        // Story 13.7: Second SHORT button + P&L panel (Dual Short expansion)
        var short2ContainerGo = CreatePanel("Short2Container", canvasGo.transform);
        var short2ContainerRect = short2ContainerGo.GetComponent<RectTransform>();
        short2ContainerRect.anchorMin = new Vector2(0.5f, 0f);
        short2ContainerRect.anchorMax = new Vector2(0.5f, 0f);
        short2ContainerRect.pivot = new Vector2(0.5f, 1f);
        short2ContainerRect.anchoredPosition = new Vector2(220f, 80f); // Right of first short
        short2ContainerRect.sizeDelta = new Vector2(200f, 40f);
        short2ContainerGo.GetComponent<Image>().color = new Color(0.08f, 0.05f, 0.15f, 0.85f);

        var short2Vlg = short2ContainerGo.AddComponent<VerticalLayoutGroup>();
        short2Vlg.spacing = 4f;
        short2Vlg.padding = new RectOffset(8, 8, 4, 4);
        short2Vlg.childAlignment = TextAnchor.MiddleCenter;
        short2Vlg.childForceExpandWidth = true;
        short2Vlg.childForceExpandHeight = false;
        var short2Fitter = short2ContainerGo.AddComponent<ContentSizeFitter>();
        short2Fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var short2BtnGo = CreatePanel("Short2Button", short2ContainerGo.transform);
        short2BtnGo.GetComponent<Image>().color = shortPink;
        var short2BtnLayout = short2BtnGo.AddComponent<LayoutElement>();
        short2BtnLayout.preferredHeight = 32f;
        var short2Button = short2BtnGo.AddComponent<Button>();
        var short2BtnLabel = CreateLabel("Short2ButtonText", short2BtnGo.transform, "SHORT 2", Color.white, 16);
        short2BtnLabel.GetComponent<Text>().fontStyle = FontStyle.Bold;
        short2BtnLabel.GetComponent<Text>().raycastTarget = false;

        short2Button.onClick.AddListener(() =>
        {
            var runner = Object.FindObjectOfType<GameRunner>();
            if (runner != null) runner.HandleShort2Input();
        });

        var short2PnlGo = new GameObject("Short2PnlPanel");
        short2PnlGo.transform.SetParent(short2ContainerGo.transform, false);
        short2PnlGo.AddComponent<RectTransform>();
        var short2PnlVlg = short2PnlGo.AddComponent<VerticalLayoutGroup>();
        short2PnlVlg.spacing = 1f;
        short2PnlVlg.childAlignment = TextAnchor.MiddleCenter;
        short2PnlVlg.childForceExpandWidth = true;
        short2PnlVlg.childForceExpandHeight = false;

        var short2EntryGo = CreateLabel("Short2EntryText", short2PnlGo.transform, "Entry: $0.00", LabelColor, 12);
        short2EntryGo.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
        var short2ValueGo = CreateLabel("Short2PnlValue", short2PnlGo.transform, "P&L: +$0.00", Color.white, 14);
        short2ValueGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
        short2ValueGo.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
        var short2CountdownGo = CreateLabel("Short2Countdown", short2PnlGo.transform, "",
            new Color(1f, 0.85f, 0.2f, 1f), 12);
        short2CountdownGo.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

        short2PnlGo.SetActive(false);
        short2ContainerGo.SetActive(false); // Hidden unless Dual Short expansion owned

        // Story 13.7: Leverage badge — shown when leverage expansion is active
        var leverageBadgeGo = CreatePanel("LeverageBadge", canvasGo.transform);
        var leverageBadgeRect = leverageBadgeGo.GetComponent<RectTransform>();
        leverageBadgeRect.anchorMin = new Vector2(0.5f, 0f);
        leverageBadgeRect.anchorMax = new Vector2(0.5f, 0f);
        leverageBadgeRect.pivot = new Vector2(0.5f, 0f);
        leverageBadgeRect.anchoredPosition = new Vector2(0f, 144f); // Above buy/sell container
        leverageBadgeRect.sizeDelta = new Vector2(160f, 24f);
        leverageBadgeGo.GetComponent<Image>().color = new Color(1f, 0.6f, 0f, 0.9f); // Orange
        var leverageLabel = CreateLabel("LeverageText", leverageBadgeGo.transform, "2x LEVERAGE",
            Color.white, 14);
        leverageLabel.GetComponent<Text>().fontStyle = FontStyle.Bold;
        leverageLabel.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
        leverageLabel.GetComponent<Text>().raycastTarget = false;
        var leverageLabelRect = leverageLabel.GetComponent<RectTransform>();
        leverageLabelRect.anchorMin = Vector2.zero;
        leverageLabelRect.anchorMax = Vector2.one;
        leverageLabelRect.offsetMin = Vector2.zero;
        leverageLabelRect.offsetMax = Vector2.zero;
        leverageBadgeGo.SetActive(false); // Hidden until expansion owned

        // Initialize QuantitySelector MonoBehaviour
        var quantitySelector = panelParent.AddComponent<QuantitySelector>();
        quantitySelector.Initialize();
        quantitySelector.CooldownTimerText = cooldownTimerGo.GetComponent<Text>();
        quantitySelector.CooldownOverlay = cooldownOverlayGo;
        quantitySelector.ShortButtonImage = shortBtnGo.GetComponent<Image>();
        quantitySelector.ShortButtonText = shortBtnLabel.GetComponent<Text>();
        quantitySelector.ShortPnlPanel = shortPnlGo;
        quantitySelector.ShortPnlEntryText = shortEntryGo.GetComponent<Text>();
        quantitySelector.ShortPnlValueText = shortValueGo.GetComponent<Text>();
        quantitySelector.ShortPnlCountdownText = shortCountdownGo.GetComponent<Text>();
        quantitySelector.LeverageBadge = leverageBadgeGo;
        quantitySelector.Short2ButtonImage = short2BtnGo.GetComponent<Image>();
        quantitySelector.Short2ButtonText = short2BtnLabel.GetComponent<Text>();
        quantitySelector.Short2PnlPanel = short2PnlGo;
        quantitySelector.Short2PnlEntryText = short2EntryGo.GetComponent<Text>();
        quantitySelector.Short2PnlValueText = short2ValueGo.GetComponent<Text>();
        quantitySelector.Short2PnlCountdownText = short2CountdownGo.GetComponent<Text>();
        quantitySelector.Short2Container = short2ContainerGo;

        // Wire BUY/SELL buttons to publish TradeButtonPressedEvent
        buyButton.onClick.AddListener(() =>
            EventBus.Publish(new TradeButtonPressedEvent { IsBuy = true }));
        sellButton.onClick.AddListener(() =>
            EventBus.Publish(new TradeButtonPressedEvent { IsBuy = false }));

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[Setup] TradePanel created: BUY/SELL + SHORT integrated");
        #endif

        return quantitySelector;
    }

    /// <summary>
    /// Generates the Item Inventory bottom bar panel.
    /// Three sections: Tool slots (Q/E/R) | Intel badges | Perk list.
    /// Displays items from RunContext.OwnedRelics during trading rounds.
    /// </summary>
    public static ItemInventoryPanel ExecuteItemInventoryPanel(RunContext runContext)
    {
        const float BottomBarHeight = 50f;

        var panelParent = new GameObject("ItemInventoryPanel");

        // Create Canvas
        var canvasGo = new GameObject("InventoryCanvas");
        canvasGo.transform.SetParent(panelParent.transform);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 21; // Between HUD (20) and NewsTicker (22)

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Bottom bar background — anchored to bottom, full width
        var bottomBar = CreatePanel("BottomBar", canvasGo.transform);
        var barRect = bottomBar.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0f, 0f);
        barRect.anchorMax = new Vector2(1f, 0f);
        barRect.pivot = new Vector2(0.5f, 0f);
        barRect.anchoredPosition = new Vector2(0f, 28f); // Above NewsTicker bar (28px)
        barRect.sizeDelta = new Vector2(0f, BottomBarHeight);
        bottomBar.GetComponent<Image>().color = BarBackgroundColor;

        // Horizontal layout for relic slots (flat list, no category sections)
        var barLayout = bottomBar.AddComponent<HorizontalLayoutGroup>();
        barLayout.spacing = 12f;
        barLayout.padding = new RectOffset(16, 16, 4, 4);
        barLayout.childAlignment = TextAnchor.MiddleCenter;
        barLayout.childForceExpandWidth = true;
        barLayout.childForceExpandHeight = true;

        // Create relic slots (flat list — Story 13.9: no Tools/Intel/Perks split)
        var relicSlotViews = new ItemInventoryPanel.RelicSlotView[ItemInventoryPanel.MaxDisplaySlots];
        for (int i = 0; i < ItemInventoryPanel.MaxDisplaySlots; i++)
        {
            relicSlotViews[i] = CreateInventoryRelicSlot(i, bottomBar.transform);
        }

        // Initialize ItemInventoryPanel MonoBehaviour
        var inventoryPanel = panelParent.AddComponent<ItemInventoryPanel>();
        inventoryPanel.Initialize(runContext, bottomBar, relicSlotViews);

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[Setup] ItemInventoryPanel created: bottom bar with relic slots");
        #endif

        return inventoryPanel;
    }

    private static ItemInventoryPanel.RelicSlotView CreateInventoryRelicSlot(int index, Transform parent)
    {
        var view = new ItemInventoryPanel.RelicSlotView();

        // Slot container with horizontal layout
        var slotGo = new GameObject($"RelicSlot_{index}");
        slotGo.transform.SetParent(parent, false);
        slotGo.AddComponent<RectTransform>();
        var slotHlg = slotGo.AddComponent<HorizontalLayoutGroup>();
        slotHlg.spacing = 4f;
        slotHlg.childAlignment = TextAnchor.MiddleLeft;
        slotHlg.childForceExpandWidth = false;
        slotHlg.childForceExpandHeight = true;

        // Border — thin colored accent on the left (uniform amber for all relics)
        var borderGo = new GameObject($"RelicBorder_{index}");
        borderGo.transform.SetParent(slotGo.transform, false);
        var borderRect = borderGo.AddComponent<RectTransform>();
        borderRect.sizeDelta = new Vector2(3f, 0f);
        var borderLayout = borderGo.AddComponent<LayoutElement>();
        borderLayout.preferredWidth = 3f;
        borderLayout.flexibleHeight = 1f;
        view.Border = borderGo.AddComponent<Image>();
        view.Border.color = ItemInventoryPanel.DimmedBorderColor;

        // Hotkey label
        var hotkeyGo = CreateLabel($"Hotkey_{index}", slotGo.transform,
            $"[{ItemInventoryPanel.RelicHotkeys[index]}]",
            TradingHUD.WarningYellow, 11);
        var hotkeyLayout = hotkeyGo.AddComponent<LayoutElement>();
        hotkeyLayout.preferredWidth = 24f;
        hotkeyGo.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        view.HotkeyText = hotkeyGo.GetComponent<Text>();

        // Relic name
        var nameGo = CreateLabel($"RelicName_{index}", slotGo.transform,
            "---", ValueColor, 13);
        var nameLayout = nameGo.AddComponent<LayoutElement>();
        nameLayout.flexibleWidth = 1f;
        nameGo.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        view.NameText = nameGo.GetComponent<Text>();

        return view;
    }

    private static GameObject CreatePanel(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>();
        return go;
    }

    private static GameObject CreateLabel(string name, Transform parent, string text, Color color, int fontSize)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200f, fontSize + 8);

        var txt = go.AddComponent<Text>();
        txt.text = text;
        txt.color = color;
        txt.fontSize = fontSize;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        return go;
    }

}
