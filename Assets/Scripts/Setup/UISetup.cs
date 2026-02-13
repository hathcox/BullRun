using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Setup class that generates UI Canvas hierarchies during F5 rebuild.
/// Creates Trading HUD (top bar), Stock Sidebar (left panel), and Round Timer.
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
        ExecuteSidebar();
        ExecuteRoundTimer();

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[Setup] UISetup: All UI hierarchies created (MarketOpenUI, StockSidebar, RoundTimer)");
        #endif
    }

    public static void Execute(RunContext runContext, int currentRound, float roundDuration)
    {
        var hudParent = new GameObject("TradingHUD");

        // Create Canvas
        var canvasGo = new GameObject("HUDCanvas");
        canvasGo.transform.SetParent(hudParent.transform);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20; // Above chart UI

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Top bar background
        var topBar = CreatePanel("TopBar", canvasGo.transform);
        var topBarRect = topBar.GetComponent<RectTransform>();
        topBarRect.anchorMin = new Vector2(0f, 1f);
        topBarRect.anchorMax = new Vector2(1f, 1f);
        topBarRect.pivot = new Vector2(0.5f, 1f);
        topBarRect.anchoredPosition = Vector2.zero;
        topBarRect.sizeDelta = new Vector2(0f, TopBarHeight);
        topBar.GetComponent<Image>().color = BarBackgroundColor;

        // Horizontal layout group for sections
        var layout = topBar.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 40f;
        layout.padding = new RectOffset(30, 30, 5, 5);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        // Section 1: Cash
        var cashSection = CreateHUDSection("CashSection", topBar.transform);
        var cashLabel = CreateLabel("CashLabel", cashSection.transform, "CASH", LabelColor, 12);
        var cashValue = CreateLabel("CashValue", cashSection.transform, "$0.00", ValueColor, 20);

        // Section 2: Portfolio Value
        var portfolioSection = CreateHUDSection("PortfolioSection", topBar.transform);
        var portfolioLabel = CreateLabel("PortfolioLabel", portfolioSection.transform, "PORTFOLIO", LabelColor, 12);
        var portfolioValue = CreateLabel("PortfolioValue", portfolioSection.transform, "$0.00", ValueColor, 20);
        var portfolioChange = CreateLabel("PortfolioChange", portfolioSection.transform, "+0.0%", TradingHUD.ProfitGreen, 14);

        // Section 3: Round Profit
        var profitSection = CreateHUDSection("ProfitSection", topBar.transform);
        var profitLabel = CreateLabel("ProfitLabel", profitSection.transform, "ROUND PROFIT", LabelColor, 12);
        var profitValue = CreateLabel("ProfitValue", profitSection.transform, "+$0.00", ValueColor, 20);

        // Section 4: Target
        var targetSection = CreateHUDSection("TargetSection", topBar.transform);
        var targetLabel = CreateLabel("TargetLabel", targetSection.transform, "TARGET", LabelColor, 12);
        var targetValue = CreateLabel("TargetValue", targetSection.transform, "$0 / $0", ValueColor, 16);
        var targetBar = CreateProgressBar("TargetBar", targetSection.transform);

        // Initialize TradingHUD MonoBehaviour
        var tradingHUD = hudParent.AddComponent<TradingHUD>();
        tradingHUD.Initialize(
            runContext, currentRound, roundDuration,
            cashValue.GetComponent<Text>(),
            portfolioValue.GetComponent<Text>(),
            portfolioChange.GetComponent<Text>(),
            profitValue.GetComponent<Text>(),
            targetValue.GetComponent<Text>(),
            targetBar
        );
        tradingHUD.SetTopBarBackground(topBar.GetComponent<Image>());

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[Setup] TradingHUD created: round={currentRound}, target=${MarginCallTargets.GetTarget(currentRound):F0}");
        #endif
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

    /// <summary>
    /// Generates the positions panel on the right side of the screen.
    /// Displays open positions with real-time P&L.
    /// </summary>
    public static PositionPanel ExecutePositionsPanel(Portfolio portfolio)
    {
        var panelParent = new GameObject("PositionPanel");

        // Create Canvas
        var canvasGo = new GameObject("PositionsPanelCanvas");
        canvasGo.transform.SetParent(panelParent.transform);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 15;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Panel background — right side, below top bar, with left margin to avoid chart price labels
        var panelBg = CreatePanel("PositionsPanelBg", canvasGo.transform);
        var panelRect = panelBg.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(0f, -TopBarHeight);
        panelRect.sizeDelta = new Vector2(180f, -TopBarHeight);
        panelBg.GetComponent<Image>().color = SidebarBgColor;

        // Title
        CreateLabel("PositionsTitle", panelBg.transform, "POSITIONS", LabelColor, 12);

        // Entry container with vertical layout
        var containerGo = new GameObject("EntryContainer");
        containerGo.transform.SetParent(panelBg.transform, false);
        var containerRect = containerGo.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0f, 0f);
        containerRect.anchorMax = new Vector2(1f, 1f);
        containerRect.offsetMin = new Vector2(4f, 4f);
        containerRect.offsetMax = new Vector2(-4f, -24f);
        var vlg = containerGo.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        containerGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Empty state text
        var emptyGo = CreateLabel("EmptyText", panelBg.transform, "No open positions", LabelColor, 13);
        var emptyRect = emptyGo.GetComponent<RectTransform>();
        emptyRect.anchorMin = new Vector2(0f, 0.5f);
        emptyRect.anchorMax = new Vector2(1f, 0.5f);
        emptyRect.anchoredPosition = Vector2.zero;
        emptyRect.sizeDelta = new Vector2(0f, 30f);

        // Initialize PositionPanel MonoBehaviour
        var positionPanel = panelParent.AddComponent<PositionPanel>();
        positionPanel.Initialize(portfolio, containerGo.transform, emptyGo.GetComponent<Text>());

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[Setup] PositionPanel created on right sidebar");
        #endif

        return positionPanel;
    }

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

        // Initialize MarketOpenUI MonoBehaviour
        var marketOpenUI = overlayParent.AddComponent<MarketOpenUI>();
        marketOpenUI.Initialize(
            bgGo,
            headerGo.GetComponent<Text>(),
            stockListGo.GetComponent<Text>(),
            headlineGo.GetComponent<Text>(),
            targetGo.GetComponent<Text>(),
            countdownGo.GetComponent<Text>(),
            canvasGroup
        );

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[Setup] MarketOpenUI created: full-screen overlay");
        #endif

        return marketOpenUI;
    }

    /// <summary>
    /// Generates the Round Timer UI positioned near the top bar.
    /// Shows countdown text and progress bar with urgency color transitions.
    /// </summary>
    public static RoundTimerUI ExecuteRoundTimer()
    {
        var timerParent = new GameObject("RoundTimer");

        // Create Canvas
        var canvasGo = new GameObject("RoundTimerCanvas");
        canvasGo.transform.SetParent(timerParent.transform);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 25; // Above HUD

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Timer container — centered at bottom
        var containerGo = new GameObject("TimerContainer");
        containerGo.transform.SetParent(canvasGo.transform, false);
        var containerRect = containerGo.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0f);
        containerRect.anchorMax = new Vector2(0.5f, 0f);
        containerRect.pivot = new Vector2(0.5f, 0f);
        containerRect.anchoredPosition = new Vector2(0f, 16f);
        containerRect.sizeDelta = new Vector2(160f, 50f);

        // Background panel
        var bgImage = containerGo.AddComponent<Image>();
        bgImage.color = new Color(0.05f, 0.07f, 0.18f, 0.85f);

        // Vertical layout
        var vlg = containerGo.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2f;
        vlg.padding = new RectOffset(8, 8, 4, 4);
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Timer text — "0:45" format
        var timerTextGo = CreateLabel("TimerText", containerGo.transform, "1:00",
            new Color(0f, 1f, 0.533f, 1f), 24);
        timerTextGo.GetComponent<Text>().fontStyle = FontStyle.Bold;

        // Progress bar
        var progressFill = CreateProgressBar("TimerProgress", containerGo.transform);
        progressFill.fillAmount = 1f;
        progressFill.color = new Color(0f, 1f, 0.533f, 1f);

        // Initialize RoundTimerUI MonoBehaviour
        var roundTimerUI = timerParent.AddComponent<RoundTimerUI>();
        roundTimerUI.Initialize(timerTextGo.GetComponent<Text>(), progressFill, containerGo);

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[Setup] RoundTimerUI created: centered below top bar");
        #endif

        return roundTimerUI;
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
    /// Generates the Shop UI overlay panel.
    /// Three item cards horizontally arranged, cash display at top, countdown timer.
    /// Wired to ShopState via ShopState.ShopUIInstance.
    /// </summary>
    public static ShopUI ExecuteShopUI()
    {
        var overlayParent = new GameObject("ShopOverlay");

        var canvasGo = new GameObject("ShopCanvas");
        canvasGo.transform.SetParent(overlayParent.transform);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 102; // Above game UI, below RunSummary

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Full-screen darkened background
        var bgGo = CreatePanel("ShopBg", canvasGo.transform);
        var bgRect = bgGo.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        bgGo.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.08f, 0.92f);

        var canvasGroup = bgGo.AddComponent<CanvasGroup>();

        // Header: "DRAFT SHOP — ROUND X"
        var headerGo = CreateLabel("ShopHeader", bgGo.transform, "DRAFT SHOP",
            new Color(0f, 1f, 0.533f, 1f), 28);
        headerGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
        var headerRect = headerGo.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0.5f, 1f);
        headerRect.anchorMax = new Vector2(0.5f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = new Vector2(0f, -30f);
        headerRect.sizeDelta = new Vector2(400f, 40f);

        // Cash display
        var cashGo = CreateLabel("ShopCash", bgGo.transform, "$1000",
            new Color(1f, 0.85f, 0.2f, 1f), 24);
        cashGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
        var cashRect = cashGo.GetComponent<RectTransform>();
        cashRect.anchorMin = new Vector2(0.5f, 1f);
        cashRect.anchorMax = new Vector2(0.5f, 1f);
        cashRect.pivot = new Vector2(0.5f, 1f);
        cashRect.anchoredPosition = new Vector2(0f, -75f);
        cashRect.sizeDelta = new Vector2(300f, 30f);

        // Timer display
        var timerGo = CreateLabel("ShopTimer", bgGo.transform, "0:18",
            LabelColor, 20);
        var timerRect = timerGo.GetComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(0.5f, 0f);
        timerRect.anchorMax = new Vector2(0.5f, 0f);
        timerRect.pivot = new Vector2(0.5f, 0f);
        timerRect.anchoredPosition = new Vector2(0f, 30f);
        timerRect.sizeDelta = new Vector2(200f, 30f);

        // Card container — horizontal layout, centered
        var cardContainer = new GameObject("CardContainer");
        cardContainer.transform.SetParent(bgGo.transform, false);
        var containerRect = cardContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = new Vector2(0f, -20f);
        containerRect.sizeDelta = new Vector2(900f, 380f);

        var hlg = cardContainer.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20f;
        hlg.padding = new RectOffset(10, 10, 10, 10);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        // Create 3 item cards
        var cards = new ShopUI.ItemCardView[3];
        string[] defaultCategories = { "TRADING TOOL", "MARKET INTEL", "PASSIVE PERK" };
        for (int i = 0; i < 3; i++)
        {
            cards[i] = CreateItemCard(i, defaultCategories[i], cardContainer.transform);
        }

        // Initialize ShopUI MonoBehaviour
        var shopUI = overlayParent.AddComponent<ShopUI>();
        shopUI.Initialize(
            bgGo,
            cashGo.GetComponent<Text>(),
            timerGo.GetComponent<Text>(),
            headerGo.GetComponent<Text>(),
            cards,
            canvasGroup
        );

        // Wire to ShopState
        ShopState.ShopUIInstance = shopUI;

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[Setup] ShopUI created: full-screen overlay with 3 item cards");
        #endif

        return shopUI;
    }

    private static ShopUI.ItemCardView CreateItemCard(int index, string category, Transform parent)
    {
        var view = new ShopUI.ItemCardView();

        // Card background
        var cardGo = CreatePanel($"ItemCard_{index}", parent);
        var cardRect = cardGo.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(260f, 360f);
        view.CardBackground = cardGo.GetComponent<Image>();
        view.CardBackground.color = new Color(0.08f, 0.1f, 0.22f, 0.9f);
        view.Root = cardGo;

        // Vertical layout inside card
        var vlg = cardGo.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6f;
        vlg.padding = new RectOffset(12, 12, 12, 12);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Category label
        var categoryGo = CreateLabel($"Category_{index}", cardGo.transform, category,
            new Color(0.6f, 0.6f, 0.7f, 1f), 11);
        view.CategoryLabel = categoryGo.GetComponent<Text>();

        // Rarity badge (small colored bar)
        var badgeGo = CreatePanel($"RarityBadge_{index}", cardGo.transform);
        var badgeRect = badgeGo.GetComponent<RectTransform>();
        badgeRect.sizeDelta = new Vector2(80f, 4f);
        view.RarityBadge = badgeGo.GetComponent<Image>();
        view.RarityBadge.color = ShopUI.CommonColor;

        // Rarity text
        var rarityGo = CreateLabel($"Rarity_{index}", cardGo.transform, "COMMON",
            ShopUI.CommonColor, 10);
        view.RarityText = rarityGo.GetComponent<Text>();

        // Item name
        var nameGo = CreateLabel($"Name_{index}", cardGo.transform, "Item Name",
            Color.white, 16);
        nameGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
        view.NameText = nameGo.GetComponent<Text>();

        // Description text
        var descGo = CreateLabel($"Desc_{index}", cardGo.transform, "Item description goes here",
            new Color(0.75f, 0.75f, 0.8f, 1f), 12);
        var descRect = descGo.GetComponent<RectTransform>();
        descRect.sizeDelta = new Vector2(230f, 80f);
        view.DescriptionText = descGo.GetComponent<Text>();

        // Cost
        var costGo = CreateLabel($"Cost_{index}", cardGo.transform, "$0",
            Color.white, 22);
        costGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
        view.CostText = costGo.GetComponent<Text>();

        // Purchase button
        var btnGo = CreatePanel($"BuyBtn_{index}", cardGo.transform);
        var btnRect = btnGo.GetComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(200f, 40f);
        btnGo.GetComponent<Image>().color = new Color(0f, 0.6f, 0.3f, 1f);
        view.PurchaseButton = btnGo.AddComponent<Button>();

        var btnLabel = CreateLabel($"BuyBtnText_{index}", btnGo.transform, "BUY",
            Color.white, 16);
        btnLabel.GetComponent<Text>().fontStyle = FontStyle.Bold;
        view.ButtonText = btnLabel.GetComponent<Text>();

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

    private static GameObject CreateHUDSection(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.spacing = 2f;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
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

    private static Image CreateProgressBar(string name, Transform parent)
    {
        var bgGo = new GameObject(name + "Bg");
        bgGo.transform.SetParent(parent, false);
        var bgRect = bgGo.AddComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(180f, 8f);
        var bgImage = bgGo.AddComponent<Image>();
        bgImage.color = new Color(0.15f, 0.15f, 0.2f, 0.5f);

        var fillGo = new GameObject(name + "Fill");
        fillGo.transform.SetParent(bgGo.transform, false);
        var fillRect = fillGo.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        var fillImage = fillGo.AddComponent<Image>();
        fillImage.color = TradingHUD.ProfitGreen;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillAmount = 0f;

        return fillImage;
    }
}
