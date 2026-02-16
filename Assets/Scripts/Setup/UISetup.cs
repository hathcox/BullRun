using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Setup class that generates UI Canvas hierarchies during F5 rebuild.
/// Creates Trading HUD (top bar) and Round Timer.
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

    public static void Execute(RunContext runContext, int currentRound, float roundDuration)
    {
        // Ensure EventSystem exists for uGUI button interactions (shop, etc.)
        if (EventSystem.current == null)
        {
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<InputSystemUIInputModule>();
        }

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

        // FIX-12: Section 5: Reputation (amber/gold, star icon)
        var repSection = CreateHUDSection("ReputationSection", topBar.transform);
        var repLabel = CreateLabel("RepLabel", repSection.transform, "REP", LabelColor, 12);
        var repValue = CreateLabel("RepValue", repSection.transform, "\u2605 0",
            ShopUI.ReputationColor, 20);

        // Section 6: Round Timer — countdown + progress bar in top bar
        var timerSection = CreateHUDSection("TimerSection", topBar.transform);
        var timerLabel = CreateLabel("TimerLabel", timerSection.transform, "TIME", LabelColor, 12);
        var timerValue = CreateLabel("TimerValue", timerSection.transform, "1:00",
            new Color(0f, 1f, 0.533f, 1f), 20);
        timerValue.GetComponent<Text>().fontStyle = FontStyle.Bold;
        var timerProgress = CreateProgressBar("TimerProgress", timerSection.transform);
        timerProgress.fillAmount = 1f;
        timerProgress.color = new Color(0f, 1f, 0.533f, 1f);

        // Initialize RoundTimerUI on the HUD parent
        var roundTimerUI = hudParent.AddComponent<RoundTimerUI>();
        roundTimerUI.Initialize(timerValue.GetComponent<Text>(), timerProgress, timerSection);

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
        tradingHUD.SetReputationDisplay(repValue.GetComponent<Text>());

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
    /// Generates the compact position overlay centered above the trade panel.
    /// Shows direction (LONG/SHORT/FLAT), share count, avg price, and real-time P&L.
    /// </summary>
    public static PositionOverlay ExecutePositionOverlay(Portfolio portfolio)
    {
        var overlayParent = new GameObject("PositionOverlay");

        // Create Canvas — use ChartCanvas sorting order range
        var canvasGo = new GameObject("PositionOverlayCanvas");
        canvasGo.transform.SetParent(overlayParent.transform);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 24; // Same level as trade panel — visually stacked together

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        // No GraphicRaycaster — overlay should not block input

        // Overlay container — centered above trade panel buttons
        var containerGo = CreatePanel("OverlayContainer", canvasGo.transform);
        var containerRect = containerGo.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0f);
        containerRect.anchorMax = new Vector2(0.5f, 0f);
        containerRect.pivot = new Vector2(0.5f, 0f);
        containerRect.anchoredPosition = new Vector2(0f, 144f); // Above trade panel (82 + 60 + 2)
        containerRect.sizeDelta = new Vector2(420f, 85f);
        containerGo.GetComponent<Image>().color = new Color(0.05f, 0.07f, 0.18f, 0.6f); // Semi-transparent dark

        // Vertical layout for rows
        var vlg = containerGo.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2f;
        vlg.padding = new RectOffset(10, 10, 6, 6);
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Section header
        var headerGo = CreateLabel("LongHeader", containerGo.transform, "LONG POSITION",
            LabelColor, 10);
        headerGo.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

        // Row 1: Direction — "15x LONG" or "FLAT"
        var directionGo = CreateLabel("DirectionText", containerGo.transform, "FLAT",
            new Color(0.5f, 0.5f, 0.55f, 1f), 16);
        directionGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
        directionGo.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

        // Row 2: Avg price — "Avg: $2.45"
        var avgPriceGo = CreateLabel("AvgPriceText", containerGo.transform, "Avg: $0.00",
            LabelColor, 13);
        avgPriceGo.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

        // Row 3: P&L — "P&L: +$3.75"
        var pnlGo = CreateLabel("PnLText", containerGo.transform, "P&L: +$0.00",
            Color.white, 14);
        pnlGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
        pnlGo.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

        // Initialize PositionOverlay MonoBehaviour
        var positionOverlay = overlayParent.AddComponent<PositionOverlay>();
        positionOverlay.Initialize(
            portfolio,
            directionGo.GetComponent<Text>(),
            avgPriceGo.GetComponent<Text>(),
            pnlGo.GetComponent<Text>(),
            avgPriceGo,
            pnlGo
        );

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[Setup] PositionOverlay created: centered above trade panel");
        #endif

        return positionOverlay;
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

        // Category label
        var categoryGo = CreateLabel($"Category_{index}", cardGo.transform, "RELIC",
            new Color(0.6f, 0.6f, 0.7f, 1f), 11);
        view.CategoryLabel = categoryGo.GetComponent<Text>();
        view.CategoryLabel.raycastTarget = false;

        // Rarity badge (small colored bar) — unified color (Story 13.8, AC 1)
        var badgeGo = CreatePanel($"RarityBadge_{index}", cardGo.transform);
        var badgeRect = badgeGo.GetComponent<RectTransform>();
        badgeRect.sizeDelta = new Vector2(80f, 4f);
        var badgeLayoutElem = badgeGo.AddComponent<LayoutElement>();
        badgeLayoutElem.preferredHeight = 4f;
        view.RarityBadge = badgeGo.GetComponent<Image>();
        view.RarityBadge.color = ShopUI.ReputationColor;
        view.RarityBadge.raycastTarget = false;

        // Rarity text — hidden for unified look (Story 13.8, AC 1)
        var rarityGo = CreateLabel($"Rarity_{index}", cardGo.transform, "",
            ShopUI.ReputationColor, 10);
        view.RarityText = rarityGo.GetComponent<Text>();
        view.RarityText.raycastTarget = false;

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
    /// Generates the trade feedback overlay positioned below the top bar.
    /// Shows brief text like "SHORTED ACME x10" that fades out after 1.5s.
    /// </summary>
    public static TradeFeedback ExecuteTradeFeedback()
    {
        var feedbackParent = new GameObject("TradeFeedback");

        var canvasGo = new GameObject("TradeFeedbackCanvas");
        canvasGo.transform.SetParent(feedbackParent.transform);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 23; // Above HUD and NewsTicker (22)

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        // No GraphicRaycaster — feedback should not block input

        // Feedback container — centered below top bar, with dark background for readability
        var containerGo = CreatePanel("FeedbackContainer", canvasGo.transform);
        var containerRect = containerGo.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 1f);
        containerRect.anchorMax = new Vector2(0.5f, 1f);
        containerRect.pivot = new Vector2(0.5f, 1f);
        containerRect.anchoredPosition = new Vector2(0f, -(TopBarHeight + 8f));
        containerRect.sizeDelta = new Vector2(300f, 30f);
        containerGo.GetComponent<Image>().color = BarBackgroundColor;
        containerGo.GetComponent<Image>().raycastTarget = false;

        var canvasGroup = containerGo.AddComponent<CanvasGroup>();

        var feedbackTextGo = CreateLabel("FeedbackText", containerGo.transform, "",
            Color.white, 18);
        feedbackTextGo.GetComponent<Text>().fontStyle = FontStyle.Bold;

        var tradeFeedback = feedbackParent.AddComponent<TradeFeedback>();
        tradeFeedback.Initialize(feedbackTextGo.GetComponent<Text>(), canvasGroup);

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[Setup] TradeFeedback created: centered below top bar");
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
        var relicSlotViews = new ItemInventoryPanel.RelicSlotView[ItemInventoryPanel.MaxToolSlots];
        for (int i = 0; i < ItemInventoryPanel.MaxToolSlots; i++)
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
            $"[{ItemInventoryPanel.ToolHotkeys[index]}]",
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
