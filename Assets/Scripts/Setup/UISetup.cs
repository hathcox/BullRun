using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
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
    /// <summary>Last DashboardReferences created by ExecuteControlDeck(). Used by GameRunner for button wiring.</summary>
    public static DashboardReferences DashRefs { get; private set; }

    private static readonly float TopBarHeight = 60f;
    private static readonly float SidebarWidth = 240f;
    private static readonly float EntryHeight = 50f;
    // Story 14.6: Migrated to CRTThemeData references
    private static Color BarBackgroundColor => CRTThemeData.Panel;
    private static Color SidebarBgColor => CRTThemeData.Panel;
    private static Color LabelColor => CRTThemeData.TextLow;
    private static Color ValueColor => CRTThemeData.TextHigh;
    private static Color NeonGreen => CRTThemeData.TextHigh;
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

        // Story 14.2: Create Control Deck (replaces old top bar)
        var dashRefs = ExecuteControlDeck();

        // Create TradingHUD parent and initialize with DashboardReferences
        var hudParent = new GameObject("TradingHUD");
        var tradingHUD = hudParent.AddComponent<TradingHUD>();
        tradingHUD.Initialize(dashRefs, runContext, currentRound, roundDuration);

        // AC 3 & 9: Create FloatingTextService as full-stretch child of HUD canvas
        var floatingTextGo = new GameObject("FloatingTextService");
        floatingTextGo.transform.SetParent(dashRefs.ControlDeckCanvas.transform, false);
        var floatingTextRect = floatingTextGo.AddComponent<RectTransform>();
        floatingTextRect.anchorMin = Vector2.zero;
        floatingTextRect.anchorMax = Vector2.one;
        floatingTextRect.offsetMin = Vector2.zero;
        floatingTextRect.offsetMax = Vector2.zero;
        var floatingTextService = floatingTextGo.AddComponent<FloatingTextService>();
        floatingTextService.Initialize(Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
        tradingHUD.SetFloatingTextService(floatingTextService);
        if (dashRefs.RepText != null)
            tradingHUD.SetRepTextRect(dashRefs.RepText.GetComponent<RectTransform>());

        // Wire ChartLineView so profit/loss popups spawn at the current price on the chart
        // (ChartSetup.Execute runs before UISetup.Execute so the view already exists)
        var chartLineViewRef = Object.FindFirstObjectByType<ChartLineView>();
        if (chartLineViewRef != null)
            tradingHUD.SetChartLineView(chartLineViewRef);

        // AC 13: Create streak text near the target bar in left wing
        if (dashRefs.LeftWing != null)
        {
            var streakGo = new GameObject("StreakText");
            streakGo.transform.SetParent(dashRefs.LeftWing, false);
            streakGo.AddComponent<RectTransform>();
            var streakText = streakGo.AddComponent<Text>();
            streakText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            streakText.fontSize = 16;
            streakText.fontStyle = FontStyle.Bold;
            streakText.color = ColorPalette.Gold;
            streakText.alignment = TextAnchor.MiddleLeft;
            streakText.raycastTarget = false;
            var streakLayout = streakGo.AddComponent<LayoutElement>();
            streakLayout.preferredHeight = 20f;
            streakGo.SetActive(false);
            tradingHUD.SetStreakDisplay(streakText);
        }

        // AC 15: BUY/SELL/SHORT button hover + click feel
        AddTradingButtonFeel(dashRefs.BuyButton);
        AddTradingButtonFeel(dashRefs.SellButton);
        AddTradingButtonFeel(dashRefs.ShortButton);
        if (dashRefs.Short2ButtonImage != null)
            AddTradingButtonFeel(dashRefs.Short2ButtonImage.GetComponent<Button>());

        // Story 14.3: Initialize RoundTimerUI with Right Wing timer text
        var roundTimerUI = hudParent.AddComponent<RoundTimerUI>();
        roundTimerUI.Initialize(dashRefs.TimerText, dashRefs.TimerProgressBar, null);

        // Create PositionPanel using Right Wing container from Control Deck
        var posPanelGo = new GameObject("PositionPanel");
        posPanelGo.transform.SetParent(hudParent.transform, false);
        var positionPanel = posPanelGo.AddComponent<PositionPanel>();
        positionPanel.Initialize(
            runContext,
            dashRefs.PositionEntryContainer,
            dashRefs.PositionEmptyText
        );

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[Setup] TradingHUD created: round={currentRound}, target=${MarginCallTargets.GetTarget(currentRound):F0}");
        #endif
    }

    /// <summary>
    /// Creates the bottom-docked Control Deck panel with three empty wing containers.
    /// Returns a populated DashboardReferences for downstream UI wiring.
    /// Story 14.2: Replaces the old top bar HUD layout.
    /// </summary>
    public static DashboardReferences ExecuteControlDeck()
    {
        var refs = new DashboardReferences();
        DashRefs = refs;

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
        panelRect.anchoredPosition = new Vector2(0f, 10f);
        panelRect.sizeDelta = new Vector2(0f, 160f);

        refs.ControlDeckPanel = panelRect;

        // 1.4: Apply CRTThemeData.Panel background and Border outline
        CRTThemeData.ApplyPanelStyle(panelGo.GetComponent<Image>());

        // 1.3: Add HorizontalLayoutGroup (padding=10, spacing=20)
        var hlg = panelGo.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(10, 10, 10, 10);
        hlg.spacing = 20f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false; // false allows Center Core's ContentSizeFitter to work

        // 1.5: Create Left_Wing container (~30% width via LayoutElement)
        var leftWingGo = new GameObject("Left_Wing");
        leftWingGo.transform.SetParent(panelGo.transform, false);
        leftWingGo.AddComponent<RectTransform>();
        var leftVlg = leftWingGo.AddComponent<VerticalLayoutGroup>();
        leftVlg.childAlignment = TextAnchor.MiddleCenter;
        leftVlg.childForceExpandWidth = true;
        leftVlg.childForceExpandHeight = false;
        var leftLayout = leftWingGo.AddComponent<LayoutElement>();
        leftLayout.flexibleWidth = 3f;
        leftLayout.preferredWidth = 0f;

        refs.LeftWing = leftWingGo.GetComponent<RectTransform>();

        // Story 14.6: Add border outline to Left_Wing
        var leftWingImg = leftWingGo.AddComponent<Image>();
        leftWingImg.color = Color.clear; // Transparent fill — border only
        leftWingImg.raycastTarget = false;
        var leftWingBorder = leftWingGo.AddComponent<Outline>();
        leftWingBorder.effectColor = CRTThemeData.Border;
        leftWingBorder.effectDistance = new Vector2(1f, -1f);

        // 1.6: Create Center_Core container (~40% width via LayoutElement)
        var centerCoreGo = new GameObject("Center_Core");
        centerCoreGo.transform.SetParent(panelGo.transform, false);
        centerCoreGo.AddComponent<RectTransform>();
        var centerVlg = centerCoreGo.AddComponent<VerticalLayoutGroup>();
        centerVlg.childAlignment = TextAnchor.MiddleCenter;
        centerVlg.childForceExpandWidth = true;
        centerVlg.childForceExpandHeight = false;
        var centerLayout = centerCoreGo.AddComponent<LayoutElement>();
        centerLayout.flexibleWidth = 4f;
        centerLayout.preferredWidth = 0f;

        refs.CenterCore = centerCoreGo.GetComponent<RectTransform>();

        // Story 14.6: Add border outline to Center_Core
        var centerCoreImg = centerCoreGo.AddComponent<Image>();
        centerCoreImg.color = Color.clear;
        centerCoreImg.raycastTarget = false;
        var centerCoreBorder = centerCoreGo.AddComponent<Outline>();
        centerCoreBorder.effectColor = CRTThemeData.Border;
        centerCoreBorder.effectDistance = new Vector2(1f, -1f);

        // 1.7: Create Right_Wing container (~30% width via LayoutElement)
        var rightWingGo = new GameObject("Right_Wing");
        rightWingGo.transform.SetParent(panelGo.transform, false);
        rightWingGo.AddComponent<RectTransform>();
        var rightVlg = rightWingGo.AddComponent<VerticalLayoutGroup>();
        rightVlg.childAlignment = TextAnchor.MiddleCenter;
        rightVlg.childForceExpandWidth = true;
        rightVlg.childForceExpandHeight = false;
        var rightLayout = rightWingGo.AddComponent<LayoutElement>();
        rightLayout.flexibleWidth = 3f;
        rightLayout.preferredWidth = 0f;

        refs.RightWing = rightWingGo.GetComponent<RectTransform>();

        // Story 14.6: Add border outline to Right_Wing
        var rightWingImg = rightWingGo.AddComponent<Image>();
        rightWingImg.color = Color.clear;
        rightWingImg.raycastTarget = false;
        var rightWingBorder = rightWingGo.AddComponent<Outline>();
        rightWingBorder.effectColor = CRTThemeData.Border;
        rightWingBorder.effectDistance = new Vector2(1f, -1f);

        // ── Story 14.3: Populate Left Wing (Wallet) ─────────────────────────
        leftVlg.spacing = 4f;
        leftVlg.padding = new RectOffset(8, 8, 8, 8);
        leftVlg.childAlignment = TextAnchor.UpperLeft;

        // "WALLET" header
        var walletHeaderGo = CreateLabel("WalletHeader", leftWingGo.transform, "WALLET", CRTThemeData.TextLow, 14);
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

        var cashLabelGo = CreateLabel("CashLabel", cashRowGo.transform, "Time:", CRTThemeData.TextLow, 16);
        cashLabelGo.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        var cashValueGo = CreateLabel("CashValue", cashRowGo.transform, "0:00", CRTThemeData.TextHigh, 22);
        cashValueGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
        cashValueGo.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        refs.TimerText = cashValueGo.GetComponent<Text>();

        // Profit row
        var profitRowGo = new GameObject("ProfitRow");
        profitRowGo.transform.SetParent(leftWingGo.transform, false);
        profitRowGo.AddComponent<RectTransform>();
        var profitRowHlg = profitRowGo.AddComponent<HorizontalLayoutGroup>();
        profitRowHlg.spacing = 4f;
        profitRowHlg.childAlignment = TextAnchor.MiddleLeft;
        profitRowHlg.childForceExpandWidth = false;
        profitRowHlg.childForceExpandHeight = true;

        var profitLabelGo = CreateLabel("ProfitLabel", profitRowGo.transform, "Round Profit:", CRTThemeData.TextLow, 16);
        profitLabelGo.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        var profitValueGo = CreateLabel("ProfitValue", profitRowGo.transform, "+$0.00", CRTThemeData.TextHigh, 22);
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

        var targetLabelGo = CreateLabel("TargetLabel_LW", targetRowGo.transform, "Target:", CRTThemeData.TextLow, 16);
        targetLabelGo.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        var targetValueGo = CreateLabel("TargetValue_LW", targetRowGo.transform, "$0.00 / $0.00", CRTThemeData.TextHigh, 20);
        targetValueGo.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        refs.TargetText = targetValueGo.GetComponent<Text>();

        // Target progress bar removed — target text already shows the numbers

        // ── Story 14.4: Populate Center Core (Action Buttons) ─────────────────
        centerVlg.spacing = 8f;
        centerVlg.padding = new RectOffset(8, 8, 8, 8);
        centerVlg.childAlignment = TextAnchor.UpperCenter;

        // InfoBar: Timer + Reputation — compact row above buttons
        var infoBarGo = new GameObject("InfoBar");
        infoBarGo.transform.SetParent(centerCoreGo.transform, false);
        infoBarGo.AddComponent<RectTransform>();
        var infoBarLayout = infoBarGo.AddComponent<LayoutElement>();
        infoBarLayout.preferredHeight = 24f;
        var infoBarHlg = infoBarGo.AddComponent<HorizontalLayoutGroup>();
        infoBarHlg.spacing = 8f;
        infoBarHlg.padding = new RectOffset(12, 12, 0, 0);
        infoBarHlg.childAlignment = TextAnchor.MiddleCenter;
        infoBarHlg.childForceExpandWidth = false;
        infoBarHlg.childForceExpandHeight = true;

        // Left: "Cash:" label + cash value
        var infoTimerLabelGo = CreateLabel("TimerLabel", infoBarGo.transform, "Cash:", CRTThemeData.TextLow, 16);
        infoTimerLabelGo.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        var infoTimerValueGo = CreateLabel("TimerValue", infoBarGo.transform, "$0.00", CRTThemeData.TextHigh, 20);
        infoTimerValueGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
        infoTimerValueGo.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        refs.CashText = infoTimerValueGo.GetComponent<Text>();

        // Spacer to push reputation to the right
        var infoSpacer = new GameObject("InfoSpacer");
        infoSpacer.transform.SetParent(infoBarGo.transform, false);
        infoSpacer.AddComponent<RectTransform>();
        var infoSpacerLayout = infoSpacer.AddComponent<LayoutElement>();
        infoSpacerLayout.flexibleWidth = 1f;

        // Right: "Reputation:" label + rep value
        var infoRepLabelGo = CreateLabel("RepLabel", infoBarGo.transform, "Reputation:", CRTThemeData.TextLow, 16);
        infoRepLabelGo.GetComponent<Text>().alignment = TextAnchor.MiddleRight;
        var infoRepValueGo = CreateLabel("RepValue", infoBarGo.transform, "\u2605 0", CRTThemeData.Warning, 20);
        infoRepValueGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
        infoRepValueGo.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        refs.RepText = infoRepValueGo.GetComponent<Text>();

        // Top row: SELL (left) + BUY (right) in HorizontalLayoutGroup
        var buttonRowGo = new GameObject("ButtonRow");
        buttonRowGo.transform.SetParent(centerCoreGo.transform, false);
        buttonRowGo.AddComponent<RectTransform>();
        var buttonRowLayout = buttonRowGo.AddComponent<LayoutElement>();
        buttonRowLayout.preferredHeight = 48f;
        var buttonRowHlg = buttonRowGo.AddComponent<HorizontalLayoutGroup>();
        buttonRowHlg.spacing = 20f;
        buttonRowHlg.childAlignment = TextAnchor.MiddleCenter;
        buttonRowHlg.childForceExpandWidth = true;
        buttonRowHlg.childForceExpandHeight = true;

        // SELL button — CRTThemeData.Danger red, left side
        var sellBtnGo = CreatePanel("SellButton", buttonRowGo.transform);
        sellBtnGo.GetComponent<Image>().color = CRTThemeData.Danger;
        var sellBtnLayout = sellBtnGo.AddComponent<LayoutElement>();
        sellBtnLayout.preferredWidth = 160f;
        sellBtnLayout.preferredHeight = 48f;
        var sellButton = sellBtnGo.AddComponent<Button>();
        var sellLabel = CreateLabel("SellButtonText", sellBtnGo.transform, "SELL", Color.white, 20);
        sellLabel.GetComponent<Text>().fontStyle = FontStyle.Bold;
        sellLabel.GetComponent<Text>().raycastTarget = false;

        // BUY button — CRTThemeData.ButtonBuy green, right side
        var buyBtnGo = CreatePanel("BuyButton", buttonRowGo.transform);
        buyBtnGo.GetComponent<Image>().color = CRTThemeData.ButtonBuy;
        var buyBtnLayout = buyBtnGo.AddComponent<LayoutElement>();
        buyBtnLayout.preferredWidth = 160f;
        buyBtnLayout.preferredHeight = 48f;
        var buyButton = buyBtnGo.AddComponent<Button>();
        var buyLabel = CreateLabel("BuyButtonText", buyBtnGo.transform, "BUY", Color.white, 20);
        buyLabel.GetComponent<Text>().fontStyle = FontStyle.Bold;
        buyLabel.GetComponent<Text>().raycastTarget = false;

        // Wire BUY/SELL buttons to publish TradeButtonPressedEvent
        buyButton.onClick.AddListener(() =>
            EventBus.Publish(new TradeButtonPressedEvent { IsBuy = true }));
        sellButton.onClick.AddListener(() =>
            EventBus.Publish(new TradeButtonPressedEvent { IsBuy = false }));

        refs.BuyButton = buyButton;
        refs.SellButton = sellButton;
        refs.BuyButtonImage = buyBtnGo.GetComponent<Image>();
        refs.BuyButtonText = buyLabel.GetComponent<Text>();
        refs.SellButtonImage = sellBtnGo.GetComponent<Image>();
        refs.SellButtonText = sellLabel.GetComponent<Text>();

        // Bottom row: SHORT button full-width — CRTThemeData.ButtonShort amber
        var shortBtnGo = CreatePanel("ShortButton", centerCoreGo.transform);
        shortBtnGo.GetComponent<Image>().color = CRTThemeData.ButtonShort;
        var shortBtnLayout = shortBtnGo.AddComponent<LayoutElement>();
        shortBtnLayout.preferredHeight = 32f;
        var shortButton = shortBtnGo.AddComponent<Button>();
        var shortBtnLabel = CreateLabel("ShortButtonText", shortBtnGo.transform, "SHORT", Color.white, 16);
        shortBtnLabel.GetComponent<Text>().fontStyle = FontStyle.Bold;
        shortBtnLabel.GetComponent<Text>().raycastTarget = false;

        // Wire SHORT button click → GameRunner.HandleShortInput()
        shortButton.onClick.AddListener(() =>
        {
            if (GameRunner.Instance != null) GameRunner.Instance.HandleShortInput();
        });

        refs.ShortButton = shortButton;
        refs.ShortButtonImage = shortBtnGo.GetComponent<Image>();
        refs.ShortButtonText = shortBtnLabel.GetComponent<Text>();

        // Short 2 container (Dual Short expansion) — button only, P&L shown in Right Wing PositionPanel
        var short2ContainerGo = new GameObject("Short2Container");
        short2ContainerGo.transform.SetParent(centerCoreGo.transform, false);
        short2ContainerGo.AddComponent<RectTransform>();
        var short2ContainerVlg = short2ContainerGo.AddComponent<VerticalLayoutGroup>();
        short2ContainerVlg.spacing = 4f;
        short2ContainerVlg.padding = new RectOffset(8, 8, 4, 4);
        short2ContainerVlg.childAlignment = TextAnchor.MiddleCenter;
        short2ContainerVlg.childForceExpandWidth = true;
        short2ContainerVlg.childForceExpandHeight = false;

        var short2BtnGo = CreatePanel("Short2Button", short2ContainerGo.transform);
        short2BtnGo.GetComponent<Image>().color = CRTThemeData.ButtonShort;
        var short2BtnLayout = short2BtnGo.AddComponent<LayoutElement>();
        short2BtnLayout.preferredHeight = 32f;
        var short2Button = short2BtnGo.AddComponent<Button>();
        var short2BtnLabel = CreateLabel("Short2ButtonText", short2BtnGo.transform, "SHORT 2", Color.white, 16);
        short2BtnLabel.GetComponent<Text>().fontStyle = FontStyle.Bold;
        short2BtnLabel.GetComponent<Text>().raycastTarget = false;

        short2Button.onClick.AddListener(() =>
        {
            if (GameRunner.Instance != null) GameRunner.Instance.HandleShort2Input();
        });

        short2ContainerGo.SetActive(false); // Hidden unless Dual Short expansion owned

        refs.Short2ButtonImage = short2BtnGo.GetComponent<Image>();
        refs.Short2ButtonText = short2BtnLabel.GetComponent<Text>();
        refs.Short2Container = short2ContainerGo;

        // Cooldown overlay removed — no longer needed

        // Leverage badge — positioned above Center Core, shown when Leverage Trading expansion owned
        var leverageBadgeGo = CreatePanel("LeverageBadge", canvasGo.transform);
        var leverageBadgeRect = leverageBadgeGo.GetComponent<RectTransform>();
        leverageBadgeRect.anchorMin = new Vector2(0.35f, 0f);
        leverageBadgeRect.anchorMax = new Vector2(0.65f, 0f);
        leverageBadgeRect.pivot = new Vector2(0.5f, 0f);
        leverageBadgeRect.anchoredPosition = new Vector2(0f, 162f); // Just above Control Deck (160px)
        leverageBadgeRect.sizeDelta = new Vector2(0f, 24f);
        leverageBadgeGo.GetComponent<Image>().color = ColorPalette.WithAlpha(ColorPalette.Amber, 0.9f);
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

        refs.LeverageBadge = leverageBadgeGo;

        // ── Story 14.3: Populate Right Wing (Positions/Stats) ────────────────
        rightVlg.spacing = 4f;
        rightVlg.padding = new RectOffset(8, 8, 8, 8);
        rightVlg.childAlignment = TextAnchor.UpperLeft;

        // "POSITIONS" header
        var posHeaderGo = CreateLabel("PositionsHeader", rightWingGo.transform, "POSITIONS", CRTThemeData.TextLow, 14);
        posHeaderGo.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;

        // Position entry container — PositionPanel dynamically creates entries here
        var posContainerGo = new GameObject("PositionEntryContainer");
        posContainerGo.transform.SetParent(rightWingGo.transform, false);
        posContainerGo.AddComponent<RectTransform>();
        var posContainerVlg = posContainerGo.AddComponent<VerticalLayoutGroup>();
        posContainerVlg.spacing = 2f;
        posContainerVlg.childAlignment = TextAnchor.UpperLeft;
        posContainerVlg.childForceExpandWidth = true;
        posContainerVlg.childForceExpandHeight = false;
        var posContainerLayout = posContainerGo.AddComponent<LayoutElement>();
        posContainerLayout.flexibleHeight = 1f;
        var posContainerFitter = posContainerGo.AddComponent<ContentSizeFitter>();
        posContainerFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        refs.PositionEntryContainer = posContainerGo.transform;

        // "No positions" empty text — shown when no positions are open
        var emptyTextGo = CreateLabel("PositionEmptyText", posContainerGo.transform, "No positions", ColorPalette.WhiteDim, 15);
        emptyTextGo.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
        emptyTextGo.GetComponent<Text>().fontStyle = FontStyle.Italic;
        refs.PositionEmptyText = emptyTextGo.GetComponent<Text>();

        // Timer + Rep rows moved to Center Core InfoBar (see above)

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[Setup] ControlDeck created: Left_Wing (Wallet), Center_Core (BUY/SELL/SHORT), Right_Wing (Positions/Stats)");
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
        view.Background.color = ColorPalette.WithAlpha(ColorPalette.Panel, 0.6f);

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
        bgGo.GetComponent<Image>().color = ColorPalette.WithAlpha(ColorPalette.Background, 0.92f);

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
            CRTThemeData.TextHigh, 28);
        headerGo.GetComponent<Text>().fontStyle = FontStyle.Bold;

        // Stock list
        var stocksLabelGo = CreateLabel("StocksLabel", centerPanel.transform, "AVAILABLE STOCKS", LabelColor, 12);
        var stockListGo = CreateLabel("StockList", centerPanel.transform, "Loading...", ValueColor, 16);
        stockListGo.GetComponent<RectTransform>().sizeDelta = new Vector2(400f, 120f);

        // Headline
        var headlineGo = CreateLabel("Headline", centerPanel.transform, "\"Markets await direction\"",
            ColorPalette.White, 18);
        headlineGo.GetComponent<Text>().fontStyle = FontStyle.Italic;

        // Target label
        CreateLabel("TargetLabel", centerPanel.transform, "PROFIT TARGET", LabelColor, 12);

        // Target value (large, prominent)
        var targetGo = CreateLabel("TargetValue", centerPanel.transform, "$0",
            ColorPalette.Gold, 36);
        targetGo.GetComponent<Text>().fontStyle = FontStyle.Bold;

        // Countdown text
        var countdownGo = CreateLabel("Countdown", centerPanel.transform, "Trading begins in 7...",
            LabelColor, 14);

        // Bond Rep payout text (Story 13.6, AC 14) — hidden by default
        var bondRepGo = CreateLabel("BondRepText", centerPanel.transform, "",
            ColorPalette.Amber, 16);
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
        bgGo.GetComponent<Image>().color = ColorPalette.WithAlpha(ColorPalette.Background, 0.95f);

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
            CRTThemeData.TextHigh, 32);
        headerGo.GetComponent<Text>().fontStyle = FontStyle.Bold;

        // Stats
        var statsGo = CreateLabel("Stats", centerPanel.transform, "", ColorPalette.WhiteDim, 16);
        statsGo.GetComponent<RectTransform>().sizeDelta = new Vector2(400f, 120f);

        // Prompt
        var promptGo = CreateLabel("Prompt", centerPanel.transform, "Press any key to continue",
            ColorPalette.Dimmed(ColorPalette.WhiteDim, 0.7f), 14);

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
        bgGo.GetComponent<Image>().color = ColorPalette.WithAlpha(ColorPalette.Background, 0.9f);

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
            CRTThemeData.TextHigh, 28);
        headerGo.GetComponent<Text>().fontStyle = FontStyle.Bold;

        // Stats
        var statsGo = CreateLabel("Stats", centerPanel.transform, "", ColorPalette.WhiteDim, 16);
        statsGo.GetComponent<RectTransform>().sizeDelta = new Vector2(350f, 80f);

        // Checkmark/X indicator
        var checkGo = CreateLabel("Checkmark", centerPanel.transform, "\u2713",
            CRTThemeData.TextHigh, 48);

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
        bgGo.GetComponent<Image>().color = ColorPalette.WithAlpha(ColorPalette.Background, 0.95f);

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
            ColorPalette.Gold, 48);
        actHeaderGo.GetComponent<Text>().fontStyle = FontStyle.Bold;

        // "LOW-VALUE STOCKS" — subtitle, white
        var subtitleGo = CreateLabel("TierSubtitle", centerPanel.transform, "LOW-VALUE STOCKS",
            Color.white, 28);
        subtitleGo.GetComponent<Text>().fontStyle = FontStyle.Bold;

        // "Rising Stakes — Trends and Reversals" — tagline, smaller, muted color
        var taglineGo = CreateLabel("Tagline", centerPanel.transform,
            "Rising Stakes \u2014 Trends and Reversals",
            ColorPalette.WhiteDim, 18);
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
        barGo.GetComponent<Image>().color = ColorPalette.WithAlpha(ColorPalette.Panel, 0.85f);

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
        redPulseImage.color = ColorPalette.WithAlpha(ColorPalette.Red, 0f);
        redPulseImage.raycastTarget = false;

        // Green tint overlay (BullRun)
        var greenTintGo = CreatePanel("GreenTintImage", shakeGo.transform);
        var greenTintRect = greenTintGo.GetComponent<RectTransform>();
        greenTintRect.anchorMin = Vector2.zero;
        greenTintRect.anchorMax = Vector2.one;
        greenTintRect.offsetMin = Vector2.zero;
        greenTintRect.offsetMax = Vector2.zero;
        var greenTintImage = greenTintGo.GetComponent<Image>();
        greenTintImage.color = ColorPalette.WithAlpha(ColorPalette.Green, 0f);
        greenTintImage.raycastTarget = false;

        // Flash overlay (FlashCrash)
        var flashGo = CreatePanel("FlashImage", shakeGo.transform);
        var flashRect = flashGo.GetComponent<RectTransform>();
        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.offsetMin = Vector2.zero;
        flashRect.offsetMax = Vector2.zero;
        var flashImage = flashGo.GetComponent<Image>();
        flashImage.color = ColorPalette.WithAlpha(ColorPalette.Red, 0f);
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
        bgGo.GetComponent<Image>().color = ColorPalette.Background;

        var canvasGroup = bgGo.AddComponent<CanvasGroup>();

        // ── Header: "STORE — ROUND X" ──
        var headerGo = CreateLabel("StoreHeader", bgGo.transform, "STORE",
            ColorPalette.White, 28);
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

        // ── OWNED RELICS BAR: Shows player's current relic inventory (Story 13.10) ──
        var ownedRelicsBar = new GameObject("OwnedRelicsBar");
        ownedRelicsBar.transform.SetParent(bgGo.transform, false);
        var ownedBarRect = ownedRelicsBar.AddComponent<RectTransform>();
        ownedBarRect.anchorMin = new Vector2(0.03f, 0.85f);
        ownedBarRect.anchorMax = new Vector2(0.97f, 0.93f);
        ownedBarRect.offsetMin = Vector2.zero;
        ownedBarRect.offsetMax = Vector2.zero;

        var ownedBarHlg = ownedRelicsBar.AddComponent<HorizontalLayoutGroup>();
        ownedBarHlg.spacing = 8f;
        ownedBarHlg.padding = new RectOffset(8, 8, 4, 4);
        ownedBarHlg.childAlignment = TextAnchor.MiddleCenter;
        ownedBarHlg.childForceExpandWidth = true;
        ownedBarHlg.childForceExpandHeight = true;

        // Story 17.9: Reminder label below owned relics bar (AC 10)
        var reminderGo = new GameObject("RelicOrderReminder");
        reminderGo.transform.SetParent(bgGo.transform, false);
        var reminderRect = reminderGo.AddComponent<RectTransform>();
        reminderRect.anchorMin = new Vector2(0.03f, 0.82f);
        reminderRect.anchorMax = new Vector2(0.97f, 0.855f);
        reminderRect.offsetMin = Vector2.zero;
        reminderRect.offsetMax = Vector2.zero;
        var reminderText = reminderGo.AddComponent<Text>();
        reminderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        reminderText.text = "Relics execute left to right";
        reminderText.fontSize = 9;
        reminderText.color = ColorPalette.Dimmed(ColorPalette.WhiteDim, 0.5f);
        reminderText.alignment = TextAnchor.MiddleCenter;
        reminderText.raycastTarget = false;

        // Create max possible slots (6 — base 4 + 2 from Expanded Inventory)
        var ownedSlots = new ShopUI.OwnedRelicSlotView[ShopUI.MaxPossibleOwnedSlots];
        for (int i = 0; i < ownedSlots.Length; i++)
        {
            ownedSlots[i] = CreateOwnedRelicSlot(i, ownedRelicsBar.transform);
        }

        // ── TOP SECTION: Control panel (left) + 3 Relic slots (right) ──
        var topSection = new GameObject("TopSection");
        topSection.transform.SetParent(bgGo.transform, false);
        var topRect = topSection.AddComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0.03f, 0.45f);
        topRect.anchorMax = new Vector2(0.97f, 0.84f);
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
        controlPanel.GetComponent<Image>().color = CRTThemeData.Panel;
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
        nextRoundBtnGo.GetComponent<Image>().color = ColorPalette.Dimmed(ColorPalette.Cyan, 0.4f);
        var nextRoundBtnLayout = nextRoundBtnGo.AddComponent<LayoutElement>();
        nextRoundBtnLayout.preferredHeight = 50f;
        var nextRoundButton = nextRoundBtnGo.AddComponent<Button>();
        var nextRoundLabel = CreateLabel("NextRoundBtnText", nextRoundBtnGo.transform, "NEXT ROUND", Color.white, 16);
        nextRoundLabel.GetComponent<Text>().fontStyle = FontStyle.Bold;
        nextRoundLabel.GetComponent<Text>().raycastTarget = false;

        // "Reroll" button
        var rerollBtnGo = CreatePanel("RerollBtn", controlPanel.transform);
        rerollBtnGo.GetComponent<Image>().color = ColorPalette.Dimmed(ColorPalette.CyanDim, 0.6f);
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
        shopUI.SetOwnedRelicSlots(ownedSlots); // Sell callback wired by ShopState

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

        // Story 17.10: Icon character label — bold, sized for ~60x60 display
        var iconGo = CreateLabel($"Icon_{index}", cardGo.transform, "",
            Color.white, 22);
        iconGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
        iconGo.GetComponent<Text>().raycastTarget = false;
        var iconLayout = iconGo.AddComponent<LayoutElement>();
        iconLayout.preferredHeight = 28f;
        view.IconLabel = iconGo.GetComponent<Text>();

        // Item name — bold/larger font for legibility (Story 13.8, AC 7)
        var nameGo = CreateLabel($"Name_{index}", cardGo.transform, "Empty",
            Color.white, 18);
        nameGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
        view.NameText = nameGo.GetComponent<Text>();
        view.NameText.raycastTarget = false;

        // Description text — adequate contrast and line spacing (Story 13.8, AC 7)
        var descGo = CreateLabel($"Desc_{index}", cardGo.transform, "",
            ColorPalette.WhiteDim, 13);
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

        // Story 13.10: Click-to-buy — Button on card root, no separate buy button (AC 8)
        view.PurchaseButton = cardGo.AddComponent<Button>();
        // Feedback overlay text — centered on card, hidden by default
        var feedbackGo = CreateLabel($"Feedback_{index}", cardGo.transform, "",
            Color.white, 16);
        feedbackGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
        feedbackGo.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
        feedbackGo.GetComponent<Text>().raycastTarget = false;
        feedbackGo.SetActive(false);
        view.ButtonText = feedbackGo.GetComponent<Text>();

        return view;
    }

    /// <summary>
    /// Creates an owned relic slot for the top bar (Story 13.10).
    /// Each slot shows relic name + small sell button, or "Empty" outline when vacant.
    /// </summary>
    private static ShopUI.OwnedRelicSlotView CreateOwnedRelicSlot(int index, Transform parent)
    {
        var view = new ShopUI.OwnedRelicSlotView();

        // Slot background with outline border
        var slotGo = CreatePanel($"OwnedSlot_{index}", parent);
        var slotImg = slotGo.GetComponent<Image>();
        slotImg.color = ShopUI.OwnedRelicEmptyColor;
        view.Root = slotGo;
        view.Background = slotImg;
        view.Group = slotGo.AddComponent<CanvasGroup>();

        var outline = slotGo.AddComponent<Outline>();
        outline.effectColor = ShopUI.PanelBorderColor;
        outline.effectDistance = new Vector2(1f, 1f);

        var vlg = slotGo.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2f;
        vlg.padding = new RectOffset(6, 6, 4, 4);
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Story 17.10: Icon character label (shown when populated)
        var iconGo = CreateLabel($"OwnedIcon_{index}", slotGo.transform, "",
            Color.white, 14);
        iconGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
        iconGo.GetComponent<Text>().raycastTarget = false;
        var iconLayout = iconGo.AddComponent<LayoutElement>();
        iconLayout.preferredHeight = 18f;
        view.IconLabel = iconGo.GetComponent<Text>();

        // Relic name label (shown when populated)
        var nameGo = CreateLabel($"OwnedName_{index}", slotGo.transform, "",
            Color.white, 11);
        nameGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
        nameGo.GetComponent<Text>().raycastTarget = false;
        view.NameLabel = nameGo.GetComponent<Text>();

        // Sell button — small, at bottom, intentionally placed to avoid accidents
        var sellBtnGo = CreatePanel($"SellBtn_{index}", slotGo.transform);
        sellBtnGo.GetComponent<Image>().color = ShopUI.OwnedRelicSellColor;
        var sellBtnLayout = sellBtnGo.AddComponent<LayoutElement>();
        sellBtnLayout.preferredHeight = 18f;
        view.SellButton = sellBtnGo.AddComponent<Button>();

        var sellLabel = CreateLabel($"SellBtnText_{index}", sellBtnGo.transform, "SELL",
            Color.white, 9);
        sellLabel.GetComponent<Text>().raycastTarget = false;
        view.SellButtonText = sellLabel.GetComponent<Text>();

        // Empty label (shown when vacant)
        var emptyGo = CreateLabel($"OwnedEmpty_{index}", slotGo.transform, "Empty",
            ColorPalette.WithAlpha(ColorPalette.WhiteDim, 0.5f), 10);
        emptyGo.GetComponent<Text>().raycastTarget = false;
        view.EmptyLabel = emptyGo.GetComponent<Text>();

        // Story 17.9: Insertion indicator — thin vertical line for reorder drop position (AC 2)
        var indicatorGo = new GameObject($"InsertionIndicator_{index}");
        indicatorGo.transform.SetParent(slotGo.transform, false);
        var indicatorRect = indicatorGo.AddComponent<RectTransform>();
        indicatorRect.anchorMin = new Vector2(0f, 0.1f);
        indicatorRect.anchorMax = new Vector2(0f, 0.9f);
        indicatorRect.offsetMin = new Vector2(-2f, 0f);
        indicatorRect.offsetMax = new Vector2(2f, 0f);
        indicatorRect.anchoredPosition = new Vector2(-4f, 0f);
        var indicatorImg = indicatorGo.AddComponent<Image>();
        indicatorImg.color = ColorPalette.WithAlpha(ColorPalette.Cyan, 0.6f);
        indicatorImg.raycastTarget = false;
        indicatorGo.SetActive(false);
        view.InsertionIndicator = indicatorGo;

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
            ColorPalette.White, 16);
        headerGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
        headerGo.GetComponent<Text>().raycastTarget = false;
        var headerLayout = headerGo.AddComponent<LayoutElement>();
        headerLayout.preferredHeight = 22f;

        // Underline separator
        var underlineGo = new GameObject($"{name}Underline");
        underlineGo.transform.SetParent(panelGo.transform, false);
        var underlineImg = underlineGo.AddComponent<Image>();
        underlineImg.color = ColorPalette.Border;
        underlineImg.raycastTarget = false;
        var underlineLayout = underlineGo.AddComponent<LayoutElement>();
        underlineLayout.preferredHeight = 1f;

        // Placeholder content area
        var contentGo = CreateLabel($"{name}Content", panelGo.transform, "Coming soon...",
            ColorPalette.WithAlpha(ColorPalette.GreenDim, 0.7f), 12);
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
        containerGo.GetComponent<Image>().color = ColorPalette.WithAlpha(ColorPalette.Panel, 0.85f);
        containerGo.GetComponent<Image>().raycastTarget = false;

        var canvasGroup = containerGo.AddComponent<CanvasGroup>();

        var feedbackTextGo = CreateLabel("FeedbackText", containerGo.transform, "",
            Color.white, 32);
        feedbackTextGo.GetComponent<Text>().fontStyle = FontStyle.Bold;

        var tradeFeedback = feedbackParent.AddComponent<TradeFeedback>();
        tradeFeedback.Initialize(feedbackTextGo.GetComponent<Text>(), canvasGroup);

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[Setup] TradeFeedback created: center screen (FIX-16)");
        #endif

        return tradeFeedback;
    }


    // ── Story 14.6: Cached CRT overlay textures and bloom profile ────────
    private static Texture2D _vignetteTexture;
    private static Texture2D _scanlineTexture;
    private static VolumeProfile _bloomProfile;

    /// <summary>
    /// Story 14.6: Creates a full-screen CRT bezel overlay canvas with vignette and scanline effects.
    /// ScreenSpaceOverlay at sortingOrder 999 so it renders on top of everything.
    /// Raycast disabled on all elements so it never blocks input.
    /// </summary>
    public static void ExecuteCRTOverlay()
    {
        // 1.1: Create CRTOverlayCanvas (ScreenSpaceOverlay, sortingOrder=999)
        var canvasGo = new GameObject("CRTOverlayCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        // 1.2: Disable GraphicRaycaster (no input blocking)
        // Do NOT add GraphicRaycaster at all — this prevents any raycast processing

        // 1.3: Create vignette panel — full-screen Image with gradient (dark edges, transparent center)
        var vignetteGo = new GameObject("CRTVignette");
        vignetteGo.transform.SetParent(canvasGo.transform, false);
        var vignetteRect = vignetteGo.AddComponent<RectTransform>();
        vignetteRect.anchorMin = Vector2.zero;
        vignetteRect.anchorMax = Vector2.one;
        vignetteRect.offsetMin = Vector2.zero;
        vignetteRect.offsetMax = Vector2.zero;

        var vignetteImage = vignetteGo.AddComponent<Image>();
        vignetteImage.raycastTarget = false; // 1.5
        vignetteImage.sprite = CreateVignetteSprite();
        vignetteImage.type = Image.Type.Simple;
        vignetteImage.color = Color.white; // Texture carries alpha

        // 1.4: Create scanline panel — full-screen Image with horizontal line pattern
        var scanlineGo = new GameObject("CRTScanlines");
        scanlineGo.transform.SetParent(canvasGo.transform, false);
        var scanlineRect = scanlineGo.AddComponent<RectTransform>();
        scanlineRect.anchorMin = Vector2.zero;
        scanlineRect.anchorMax = Vector2.one;
        scanlineRect.offsetMin = Vector2.zero;
        scanlineRect.offsetMax = Vector2.zero;

        var scanlineImage = scanlineGo.AddComponent<Image>();
        scanlineImage.raycastTarget = false; // 1.5
        scanlineImage.sprite = CreateScanlineSprite();
        scanlineImage.type = Image.Type.Tiled;
        float screenHeight = Screen.height > 0 ? Screen.height : 1080f;
        scanlineImage.pixelsPerUnitMultiplier = 1f;

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[Setup] CRT Overlay: vignette + scanlines created (sortingOrder=999, no raycaster)");
        #endif
    }

    /// <summary>
    /// Story 14.6: Sets up URP Bloom post-processing for phosphor glow on bright green text.
    /// Creates a global Volume with Bloom if none exists.
    /// </summary>
    public static void ExecuteBloomSetup()
    {
        // Check if a global Volume already exists
        var existingVolume = Object.FindFirstObjectByType<Volume>();
        if (existingVolume != null && existingVolume.isGlobal)
        {
            // Ensure Bloom override exists on existing volume
            if (existingVolume.profile != null && !existingVolume.profile.Has<Bloom>())
            {
                var bloom = existingVolume.profile.Add<Bloom>();
                bloom.intensity.Override(0.5f);
                bloom.threshold.Override(0.8f);
            }
            return;
        }

        // Cache VolumeProfile to prevent leaks on repeated calls
        if (_bloomProfile == null)
        {
            _bloomProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloomEffect = _bloomProfile.Add<Bloom>();
            bloomEffect.intensity.Override(0.5f);
            bloomEffect.threshold.Override(0.8f);
        }

        var volumeGo = new GameObject("CRTBloomVolume");
        var volume = volumeGo.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.profile = _bloomProfile;

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[Setup] CRT Bloom: global Volume created (intensity=0.5, threshold=0.8)");
        #endif
    }

    /// <summary>
    /// Story 14.6: Creates procedural vignette texture (radial gradient, dark edges).
    /// Cached — only generated once.
    /// </summary>
    private static Sprite CreateVignetteSprite()
    {
        if (_vignetteTexture == null)
        {
            const int size = 256;
            _vignetteTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            _vignetteTexture.name = "CRTVignette";
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - 128f) / 128f;
                    float dy = (y - 128f) / 128f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01((dist - 0.5f) * 1.5f) * CRTThemeData.VignetteIntensity;
                    _vignetteTexture.SetPixel(x, y, new Color(0, 0, 0, alpha));
                }
            }
            _vignetteTexture.Apply();
        }
        return Sprite.Create(_vignetteTexture,
            new Rect(0, 0, _vignetteTexture.width, _vignetteTexture.height),
            new Vector2(0.5f, 0.5f), 100f);
    }

    /// <summary>
    /// Story 14.6: Creates procedural scanline texture (horizontal lines, repeating pattern).
    /// Cached — only generated once. 1x6 tiled texture.
    /// </summary>
    private static Sprite CreateScanlineSprite()
    {
        if (_scanlineTexture == null)
        {
            _scanlineTexture = new Texture2D(1, 6, TextureFormat.RGBA32, false);
            _scanlineTexture.name = "CRTScanlines";
            float opacity = CRTThemeData.ScanlineOpacity;
            _scanlineTexture.SetPixel(0, 0, new Color(0, 0, 0, opacity)); // Dark line
            _scanlineTexture.SetPixel(0, 1, Color.clear);
            _scanlineTexture.SetPixel(0, 2, Color.clear);
            _scanlineTexture.SetPixel(0, 3, new Color(0, 0, 0, opacity)); // Dark line
            _scanlineTexture.SetPixel(0, 4, Color.clear);
            _scanlineTexture.SetPixel(0, 5, Color.clear);
            _scanlineTexture.wrapMode = TextureWrapMode.Repeat;
            _scanlineTexture.filterMode = FilterMode.Point;
            _scanlineTexture.Apply();
        }
        return Sprite.Create(_scanlineTexture,
            new Rect(0, 0, _scanlineTexture.width, _scanlineTexture.height),
            new Vector2(0.5f, 0.5f), 1f); // 1 pixel per unit for tiling
    }

    // ════════════════════════════════════════════════════════════════════
    // STORY 16.1: MAIN MENU UI
    // ════════════════════════════════════════════════════════════════════

    /// <summary>Last MainMenuReferences created by ExecuteMainMenuUI().</summary>
    public static MainMenuReferences MenuRefs { get; private set; }

    /// <summary>Last SettingsPanelReferences created by ExecuteSettingsUI().</summary>
    public static SettingsPanelReferences SettingsRefs { get; private set; }

    /// <summary>
    /// Creates the main menu overlay canvas with title, buttons, and "coming soon" popup.
    /// Story 16.1, Task 4: MainMenuCanvas sortingOrder=150.
    /// </summary>
    public static MainMenuReferences ExecuteMainMenuUI()
    {
        var refs = new MainMenuReferences();
        MenuRefs = refs;

        // Create MainMenuCanvas (ScreenSpaceOverlay, sortingOrder=150)
        var canvasGo = new GameObject("MainMenuCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        refs.MainMenuCanvas = canvas;

        // Full-screen background panel
        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(canvasGo.transform, false);
        var bgRect = bgGo.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.color = ColorPalette.Background;
        bgImg.raycastTarget = true;

        // Content container (centered vertical layout)
        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(bgGo.transform, false);
        var contentRect = contentGo.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.3f, 0.15f);
        contentRect.anchorMax = new Vector2(0.7f, 0.85f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        var contentVlg = contentGo.AddComponent<VerticalLayoutGroup>();
        contentVlg.spacing = 16f;
        contentVlg.childAlignment = TextAnchor.UpperCenter;
        contentVlg.childForceExpandWidth = true;
        contentVlg.childForceExpandHeight = false;
        contentVlg.padding = new RectOffset(20, 20, 20, 20);

        // Title: "BULL RUN" in Gold, large font
        var titleGo = CreateLabel("Title", contentGo.transform, "BULL RUN", ColorPalette.Gold, 56);
        titleGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
        titleGo.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
        var titleLayout = titleGo.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 80f;

        // Subtitle/tagline in GreenDim
        var subtitleGo = CreateLabel("Subtitle", contentGo.transform, "Terminal Trading", ColorPalette.GreenDim, 20);
        subtitleGo.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
        var subtitleLayout = subtitleGo.AddComponent<LayoutElement>();
        subtitleLayout.preferredHeight = 30f;

        // Spacer
        var spacerGo = new GameObject("Spacer");
        spacerGo.transform.SetParent(contentGo.transform, false);
        spacerGo.AddComponent<RectTransform>();
        var spacerLayout = spacerGo.AddComponent<LayoutElement>();
        spacerLayout.preferredHeight = 40f;

        // Button factory helper
        System.Func<string, string, Color, Button> createMenuButton = (name, label, color) =>
        {
            var btnGo = new GameObject(name);
            btnGo.transform.SetParent(contentGo.transform, false);
            btnGo.AddComponent<RectTransform>();
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = color;
            CRTThemeData.ApplyPanelStyle(btnImg);
            // Override with button color after panel style sets Panel color
            btnImg.color = color;
            var btn = btnGo.AddComponent<Button>();
            var btnLayout = btnGo.AddComponent<LayoutElement>();
            btnLayout.preferredHeight = 52f;

            var btnLabel = CreateLabel(name + "Label", btnGo.transform, label, Color.white, 22);
            btnLabel.GetComponent<Text>().fontStyle = FontStyle.Bold;
            btnLabel.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            btnLabel.GetComponent<Text>().raycastTarget = false;
            // Fill parent
            var labelRect = btnLabel.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            labelRect.sizeDelta = Vector2.zero;

            return btn;
        };

        // Four menu buttons
        refs.StartGameButton = createMenuButton("StartGameButton", "START GAME", ColorPalette.Green);
        refs.UnlocksButton = createMenuButton("UnlocksButton", "UNLOCKS", ColorPalette.Amber);
        refs.SettingsButton = createMenuButton("SettingsButton", "SETTINGS", ColorPalette.Cyan);
        refs.ExitButton = createMenuButton("ExitButton", "EXIT", ColorPalette.Red);

        // "Coming Soon" popup (hidden by default)
        var popupGo = new GameObject("ComingSoonPopup");
        popupGo.transform.SetParent(canvasGo.transform, false);
        var popupRect = popupGo.AddComponent<RectTransform>();
        popupRect.anchorMin = new Vector2(0.3f, 0.35f);
        popupRect.anchorMax = new Vector2(0.7f, 0.65f);
        popupRect.offsetMin = Vector2.zero;
        popupRect.offsetMax = Vector2.zero;
        var popupImg = popupGo.AddComponent<Image>();
        CRTThemeData.ApplyPanelStyle(popupImg);

        var popupVlg = popupGo.AddComponent<VerticalLayoutGroup>();
        popupVlg.spacing = 16f;
        popupVlg.childAlignment = TextAnchor.MiddleCenter;
        popupVlg.childForceExpandWidth = true;
        popupVlg.childForceExpandHeight = false;
        popupVlg.padding = new RectOffset(20, 20, 20, 20);

        var comingSoonText = CreateLabel("ComingSoonText", popupGo.transform, "COMING SOON", ColorPalette.Amber, 28);
        comingSoonText.GetComponent<Text>().fontStyle = FontStyle.Bold;
        var csLayout = comingSoonText.AddComponent<LayoutElement>();
        csLayout.preferredHeight = 40f;

        var backBtnGo = new GameObject("PopupBackButton");
        backBtnGo.transform.SetParent(popupGo.transform, false);
        backBtnGo.AddComponent<RectTransform>();
        var backBtnImg = backBtnGo.AddComponent<Image>();
        backBtnImg.color = ColorPalette.Cyan;
        CRTThemeData.ApplyPanelStyle(backBtnImg);
        backBtnImg.color = ColorPalette.Cyan;
        refs.PopupBackButton = backBtnGo.AddComponent<Button>();
        var backBtnLayout = backBtnGo.AddComponent<LayoutElement>();
        backBtnLayout.preferredHeight = 40f;

        var backLabel = CreateLabel("BackLabel", backBtnGo.transform, "BACK", Color.white, 18);
        backLabel.GetComponent<Text>().fontStyle = FontStyle.Bold;
        backLabel.GetComponent<Text>().raycastTarget = false;
        var backLabelRect = backLabel.GetComponent<RectTransform>();
        backLabelRect.anchorMin = Vector2.zero;
        backLabelRect.anchorMax = Vector2.one;
        backLabelRect.offsetMin = Vector2.zero;
        backLabelRect.offsetMax = Vector2.zero;
        backLabelRect.sizeDelta = Vector2.zero;

        refs.ComingSoonPopup = popupGo;
        popupGo.SetActive(false);

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[Setup] MainMenuUI created: canvas sortingOrder=150, 4 buttons + coming soon popup");
        #endif

        return refs;
    }

    /// <summary>
    /// Creates the settings panel overlay with volume sliders, display toggle, resolution dropdown.
    /// Story 16.1, Task 5: SettingsCanvas sortingOrder=160, shared between main menu and pause menu.
    /// </summary>
    public static SettingsPanelReferences ExecuteSettingsUI()
    {
        var refs = new SettingsPanelReferences();
        SettingsRefs = refs;

        // Create SettingsCanvas (above main menu and pause menu)
        var canvasGo = new GameObject("SettingsCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 160;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        refs.SettingsCanvas = canvas;

        // Dimmed overlay background (click-blocker)
        var dimGo = new GameObject("DimBackground");
        dimGo.transform.SetParent(canvasGo.transform, false);
        var dimRect = dimGo.AddComponent<RectTransform>();
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = Vector2.zero;
        dimRect.offsetMax = Vector2.zero;
        var dimImg = dimGo.AddComponent<Image>();
        dimImg.color = new Color(0f, 0f, 0f, 0.6f);
        dimImg.raycastTarget = true;

        // Settings panel (centered)
        var panelGo = new GameObject("SettingsPanel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        var panelRect = panelGo.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.25f, 0.15f);
        panelRect.anchorMax = new Vector2(0.75f, 0.85f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        var panelImg = panelGo.AddComponent<Image>();
        CRTThemeData.ApplyPanelStyle(panelImg);

        var panelVlg = panelGo.AddComponent<VerticalLayoutGroup>();
        panelVlg.spacing = 12f;
        panelVlg.childAlignment = TextAnchor.UpperCenter;
        panelVlg.childForceExpandWidth = true;
        panelVlg.childForceExpandHeight = false;
        panelVlg.padding = new RectOffset(30, 30, 20, 20);

        // "SETTINGS" header
        var headerGo = CreateLabel("Header", panelGo.transform, "SETTINGS", ColorPalette.White, 28);
        headerGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
        var headerLayout = headerGo.AddComponent<LayoutElement>();
        headerLayout.preferredHeight = 40f;

        // ── AUDIO section ──
        var audioLabel = CreateLabel("AudioLabel", panelGo.transform, "AUDIO", ColorPalette.GreenDim, 18);
        audioLabel.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        var audioLabelLayout = audioLabel.AddComponent<LayoutElement>();
        audioLabelLayout.preferredHeight = 24f;

        // Slider factory helper
        System.Func<string, string, float, SliderRow> createSlider = (name, label, defaultValue) =>
        {
            var rowGo = new GameObject(name + "Row");
            rowGo.transform.SetParent(panelGo.transform, false);
            rowGo.AddComponent<RectTransform>();
            var rowLayout = rowGo.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = 36f;
            var rowHlg = rowGo.AddComponent<HorizontalLayoutGroup>();
            rowHlg.spacing = 10f;
            rowHlg.childAlignment = TextAnchor.MiddleCenter;
            rowHlg.childForceExpandWidth = false;
            rowHlg.childForceExpandHeight = true;

            // Label
            var labelGo = CreateLabel(name + "Label", rowGo.transform, label, CRTThemeData.TextLow, 16);
            labelGo.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
            var labelLayout = labelGo.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 160f;

            // Slider
            var sliderGo = new GameObject(name + "Slider");
            sliderGo.transform.SetParent(rowGo.transform, false);
            sliderGo.AddComponent<RectTransform>();
            var sliderLayout = sliderGo.AddComponent<LayoutElement>();
            sliderLayout.flexibleWidth = 1f;
            sliderLayout.preferredHeight = 24f;

            // Background track
            var bgTrack = new GameObject("Background");
            bgTrack.transform.SetParent(sliderGo.transform, false);
            var bgTrackRect = bgTrack.AddComponent<RectTransform>();
            bgTrackRect.anchorMin = new Vector2(0f, 0.35f);
            bgTrackRect.anchorMax = new Vector2(1f, 0.65f);
            bgTrackRect.offsetMin = Vector2.zero;
            bgTrackRect.offsetMax = Vector2.zero;
            var bgTrackImg = bgTrack.AddComponent<Image>();
            bgTrackImg.color = ColorPalette.Background;

            // Fill area
            var fillAreaGo = new GameObject("Fill Area");
            fillAreaGo.transform.SetParent(sliderGo.transform, false);
            var fillAreaRect = fillAreaGo.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.35f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.65f);
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(fillAreaGo.transform, false);
            var fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = ColorPalette.Green;

            // Handle slide area
            var handleAreaGo = new GameObject("Handle Slide Area");
            handleAreaGo.transform.SetParent(sliderGo.transform, false);
            var handleAreaRect = handleAreaGo.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = Vector2.zero;
            handleAreaRect.offsetMax = Vector2.zero;

            var handleGo = new GameObject("Handle");
            handleGo.transform.SetParent(handleAreaGo.transform, false);
            var handleRect = handleGo.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(16f, 24f);
            var handleImg = handleGo.AddComponent<Image>();
            handleImg.color = ColorPalette.White;

            var slider = sliderGo.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = defaultValue;
            slider.wholeNumbers = false;
            slider.targetGraphic = handleImg;
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;

            // Percentage text
            var pctGo = CreateLabel(name + "Pct", rowGo.transform, $"{Mathf.RoundToInt(defaultValue * 100)}%", CRTThemeData.TextHigh, 16);
            var pctLayout = pctGo.AddComponent<LayoutElement>();
            pctLayout.preferredWidth = 50f;

            return new SliderRow { Slider = slider, ValueText = pctGo.GetComponent<Text>() };
        };

        var masterRow = createSlider("MasterVolume", "Master Volume", SettingsManager.MasterVolume);
        var musicRow = createSlider("MusicVolume", "Music Volume", SettingsManager.MusicVolume);
        var sfxRow = createSlider("SfxVolume", "SFX Volume", SettingsManager.SfxVolume);

        refs.MasterVolumeSlider = masterRow.Slider;
        refs.MasterVolumeText = masterRow.ValueText;
        refs.MusicVolumeSlider = musicRow.Slider;
        refs.MusicVolumeText = musicRow.ValueText;
        refs.SfxVolumeSlider = sfxRow.Slider;
        refs.SfxVolumeText = sfxRow.ValueText;

        // ── DISPLAY section ──
        var displayLabel = CreateLabel("DisplayLabel", panelGo.transform, "DISPLAY", ColorPalette.GreenDim, 18);
        displayLabel.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        var displayLabelLayout = displayLabel.AddComponent<LayoutElement>();
        displayLabelLayout.preferredHeight = 24f;

        // Fullscreen toggle row
        var fsRowGo = new GameObject("FullscreenRow");
        fsRowGo.transform.SetParent(panelGo.transform, false);
        fsRowGo.AddComponent<RectTransform>();
        var fsRowLayout = fsRowGo.AddComponent<LayoutElement>();
        fsRowLayout.preferredHeight = 36f;
        var fsRowHlg = fsRowGo.AddComponent<HorizontalLayoutGroup>();
        fsRowHlg.spacing = 10f;
        fsRowHlg.childAlignment = TextAnchor.MiddleCenter;
        fsRowHlg.childForceExpandWidth = false;
        fsRowHlg.childForceExpandHeight = true;

        var fsLabelGo = CreateLabel("FullscreenLabel", fsRowGo.transform, "Fullscreen", CRTThemeData.TextLow, 16);
        fsLabelGo.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        var fsLabelLayout = fsLabelGo.AddComponent<LayoutElement>();
        fsLabelLayout.preferredWidth = 160f;

        var toggleGo = new GameObject("FullscreenToggle");
        toggleGo.transform.SetParent(fsRowGo.transform, false);
        toggleGo.AddComponent<RectTransform>();
        var toggleLayout = toggleGo.AddComponent<LayoutElement>();
        toggleLayout.preferredWidth = 30f;
        toggleLayout.preferredHeight = 30f;

        // Toggle background
        var toggleBgGo = new GameObject("ToggleBackground");
        toggleBgGo.transform.SetParent(toggleGo.transform, false);
        var toggleBgRect = toggleBgGo.AddComponent<RectTransform>();
        toggleBgRect.anchorMin = Vector2.zero;
        toggleBgRect.anchorMax = Vector2.one;
        toggleBgRect.offsetMin = Vector2.zero;
        toggleBgRect.offsetMax = Vector2.zero;
        var toggleBgImg = toggleBgGo.AddComponent<Image>();
        toggleBgImg.color = ColorPalette.Background;

        // Toggle checkmark
        var checkGo = new GameObject("Checkmark");
        checkGo.transform.SetParent(toggleBgGo.transform, false);
        var checkRect = checkGo.AddComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.15f, 0.15f);
        checkRect.anchorMax = new Vector2(0.85f, 0.85f);
        checkRect.offsetMin = Vector2.zero;
        checkRect.offsetMax = Vector2.zero;
        var checkImg = checkGo.AddComponent<Image>();
        checkImg.color = ColorPalette.Green;

        var toggle = toggleGo.AddComponent<Toggle>();
        toggle.targetGraphic = toggleBgImg;
        toggle.graphic = checkImg;
        toggle.isOn = SettingsManager.Fullscreen;

        refs.FullscreenToggle = toggle;

        // Resolution dropdown row
        var resRowGo = new GameObject("ResolutionRow");
        resRowGo.transform.SetParent(panelGo.transform, false);
        resRowGo.AddComponent<RectTransform>();
        var resRowLayout = resRowGo.AddComponent<LayoutElement>();
        resRowLayout.preferredHeight = 36f;
        var resRowHlg = resRowGo.AddComponent<HorizontalLayoutGroup>();
        resRowHlg.spacing = 10f;
        resRowHlg.childAlignment = TextAnchor.MiddleCenter;
        resRowHlg.childForceExpandWidth = false;
        resRowHlg.childForceExpandHeight = true;

        var resLabelGo = CreateLabel("ResolutionLabel", resRowGo.transform, "Resolution", CRTThemeData.TextLow, 16);
        resLabelGo.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
        var resLabelLayout = resLabelGo.AddComponent<LayoutElement>();
        resLabelLayout.preferredWidth = 160f;

        var dropdownGo = new GameObject("ResolutionDropdown");
        dropdownGo.transform.SetParent(resRowGo.transform, false);
        var dropdownRect = dropdownGo.AddComponent<RectTransform>();
        var dropdownLayout = dropdownGo.AddComponent<LayoutElement>();
        dropdownLayout.flexibleWidth = 1f;
        dropdownLayout.preferredHeight = 30f;

        // Dropdown visual setup
        var ddImg = dropdownGo.AddComponent<Image>();
        ddImg.color = ColorPalette.Background;

        var ddLabelGo = CreateLabel("DropdownLabel", dropdownGo.transform, "Current", CRTThemeData.TextHigh, 14);
        var ddLabelRect = ddLabelGo.GetComponent<RectTransform>();
        ddLabelRect.anchorMin = new Vector2(0.05f, 0f);
        ddLabelRect.anchorMax = new Vector2(0.95f, 1f);
        ddLabelRect.offsetMin = Vector2.zero;
        ddLabelRect.offsetMax = Vector2.zero;
        ddLabelRect.sizeDelta = Vector2.zero;
        ddLabelGo.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;

        // Template for dropdown (required by Unity Dropdown)
        var templateGo = new GameObject("Template");
        templateGo.transform.SetParent(dropdownGo.transform, false);
        var templateRect = templateGo.AddComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0f, 0f);
        templateRect.anchorMax = new Vector2(1f, 0f);
        templateRect.pivot = new Vector2(0.5f, 1f);
        templateRect.sizeDelta = new Vector2(0f, 200f);
        var templateImg = templateGo.AddComponent<Image>();
        templateImg.color = ColorPalette.Panel;
        templateGo.AddComponent<UnityEngine.UI.ScrollRect>();

        var viewportGo = new GameObject("Viewport");
        viewportGo.transform.SetParent(templateGo.transform, false);
        var viewportRect = viewportGo.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewportGo.AddComponent<Image>().color = Color.clear;
        viewportGo.AddComponent<Mask>().showMaskGraphic = false;

        var contentItemsGo = new GameObject("Content");
        contentItemsGo.transform.SetParent(viewportGo.transform, false);
        var contentItemsRect = contentItemsGo.AddComponent<RectTransform>();
        contentItemsRect.anchorMin = new Vector2(0f, 1f);
        contentItemsRect.anchorMax = Vector2.one;
        contentItemsRect.pivot = new Vector2(0.5f, 1f);
        contentItemsRect.sizeDelta = new Vector2(0f, 28f);

        // Item template
        var itemGo = new GameObject("Item");
        itemGo.transform.SetParent(contentItemsGo.transform, false);
        var itemRect = itemGo.AddComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0f, 0.5f);
        itemRect.anchorMax = new Vector2(1f, 0.5f);
        itemRect.sizeDelta = new Vector2(0f, 28f);
        var itemToggle = itemGo.AddComponent<Toggle>();

        var itemBgGo = new GameObject("Item Background");
        itemBgGo.transform.SetParent(itemGo.transform, false);
        var itemBgRect = itemBgGo.AddComponent<RectTransform>();
        itemBgRect.anchorMin = Vector2.zero;
        itemBgRect.anchorMax = Vector2.one;
        itemBgRect.offsetMin = Vector2.zero;
        itemBgRect.offsetMax = Vector2.zero;
        var itemBgImg = itemBgGo.AddComponent<Image>();
        itemBgImg.color = ColorPalette.Panel;

        var itemCheckGo = new GameObject("Item Checkmark");
        itemCheckGo.transform.SetParent(itemBgGo.transform, false);
        var itemCheckRect = itemCheckGo.AddComponent<RectTransform>();
        itemCheckRect.anchorMin = Vector2.zero;
        itemCheckRect.anchorMax = Vector2.one;
        itemCheckRect.offsetMin = Vector2.zero;
        itemCheckRect.offsetMax = Vector2.zero;
        var itemCheckImg = itemCheckGo.AddComponent<Image>();
        itemCheckImg.color = ColorPalette.GreenDim;

        var itemLabelGo = CreateLabel("Item Label", itemGo.transform, "", CRTThemeData.TextHigh, 14);
        var itemLabelRect = itemLabelGo.GetComponent<RectTransform>();
        itemLabelRect.anchorMin = new Vector2(0.05f, 0f);
        itemLabelRect.anchorMax = new Vector2(0.95f, 1f);
        itemLabelRect.offsetMin = Vector2.zero;
        itemLabelRect.offsetMax = Vector2.zero;
        itemLabelRect.sizeDelta = Vector2.zero;
        itemLabelGo.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;

        itemToggle.targetGraphic = itemBgImg;
        itemToggle.graphic = itemCheckImg;

        var scrollRect = templateGo.GetComponent<UnityEngine.UI.ScrollRect>();
        scrollRect.content = contentItemsRect;
        scrollRect.viewport = viewportRect;

        var dropdown = dropdownGo.AddComponent<Dropdown>();
        dropdown.template = templateRect;
        dropdown.captionText = ddLabelGo.GetComponent<Text>();
        dropdown.itemText = itemLabelGo.GetComponent<Text>();

        templateGo.SetActive(false);

        refs.ResolutionDropdown = dropdown;

        // Spacer before BACK button
        var settingsSpacer = new GameObject("SettingsSpacer");
        settingsSpacer.transform.SetParent(panelGo.transform, false);
        settingsSpacer.AddComponent<RectTransform>();
        var settingsSpacerLayout = settingsSpacer.AddComponent<LayoutElement>();
        settingsSpacerLayout.flexibleHeight = 1f;

        // BACK button
        var backGo = new GameObject("SettingsBackButton");
        backGo.transform.SetParent(panelGo.transform, false);
        backGo.AddComponent<RectTransform>();
        var backImg = backGo.AddComponent<Image>();
        backImg.color = ColorPalette.Cyan;
        CRTThemeData.ApplyPanelStyle(backImg);
        backImg.color = ColorPalette.Cyan;
        refs.BackButton = backGo.AddComponent<Button>();
        var backLayout = backGo.AddComponent<LayoutElement>();
        backLayout.preferredHeight = 44f;

        var backLabel = CreateLabel("BackLabel", backGo.transform, "BACK", Color.white, 20);
        backLabel.GetComponent<Text>().fontStyle = FontStyle.Bold;
        backLabel.GetComponent<Text>().raycastTarget = false;
        var backLabelRect = backLabel.GetComponent<RectTransform>();
        backLabelRect.anchorMin = Vector2.zero;
        backLabelRect.anchorMax = Vector2.one;
        backLabelRect.offsetMin = Vector2.zero;
        backLabelRect.offsetMax = Vector2.zero;
        backLabelRect.sizeDelta = Vector2.zero;

        // Start hidden
        canvasGo.SetActive(false);

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[Setup] SettingsUI created: canvas sortingOrder=160, 3 sliders + toggle + dropdown");
        #endif

        return refs;
    }

    // ════════════════════════════════════════════════════════════════════
    // STORY 16.2: PAUSE MENU UI
    // ════════════════════════════════════════════════════════════════════

    /// <summary>Last PauseMenuReferences created by ExecutePauseMenuUI().</summary>
    public static PauseMenuReferences PauseRefs { get; private set; }

    /// <summary>
    /// Creates the pause menu overlay canvas with PAUSED header, 4 buttons, and confirmation popup.
    /// Story 16.2, Task 1: PauseMenuCanvas sortingOrder=155.
    /// </summary>
    public static PauseMenuReferences ExecutePauseMenuUI()
    {
        var refs = new PauseMenuReferences();
        PauseRefs = refs;

        // Create PauseMenuCanvas (ScreenSpaceOverlay, sortingOrder=155)
        var canvasGo = new GameObject("PauseMenuCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 155;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        refs.PauseMenuCanvas = canvas;

        // Full-screen semi-transparent background (ColorPalette.Background at 80% alpha)
        var bgGo = new GameObject("PauseBackground");
        bgGo.transform.SetParent(canvasGo.transform, false);
        var bgRect = bgGo.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.color = ColorPalette.WithAlpha(ColorPalette.Background, 0.8f);
        bgImg.raycastTarget = true;

        // Content container (centered vertical layout)
        var contentGo = new GameObject("PauseContent");
        contentGo.transform.SetParent(bgGo.transform, false);
        var contentRect = contentGo.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.3f, 0.2f);
        contentRect.anchorMax = new Vector2(0.7f, 0.8f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        var contentVlg = contentGo.AddComponent<VerticalLayoutGroup>();
        contentVlg.spacing = 16f;
        contentVlg.childAlignment = TextAnchor.UpperCenter;
        contentVlg.childForceExpandWidth = true;
        contentVlg.childForceExpandHeight = false;
        contentVlg.padding = new RectOffset(20, 20, 20, 20);

        refs.PausePanel = bgGo;

        // "PAUSED" header in White, large font (44px), centered
        var headerGo = CreateLabel("PausedHeader", contentGo.transform, "PAUSED", ColorPalette.White, 44);
        headerGo.GetComponent<Text>().fontStyle = FontStyle.Bold;
        var headerLayout = headerGo.AddComponent<LayoutElement>();
        headerLayout.preferredHeight = 70f;
        refs.HeaderText = headerGo.GetComponent<Text>();

        // Spacer
        var spacerGo = new GameObject("Spacer");
        spacerGo.transform.SetParent(contentGo.transform, false);
        spacerGo.AddComponent<RectTransform>();
        var spacerLayout = spacerGo.AddComponent<LayoutElement>();
        spacerLayout.preferredHeight = 20f;

        // Button factory (reuses main menu pattern)
        System.Func<string, string, Color, Button> createPauseButton = (name, label, color) =>
        {
            var btnGo = new GameObject(name);
            btnGo.transform.SetParent(contentGo.transform, false);
            btnGo.AddComponent<RectTransform>();
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = color;
            CRTThemeData.ApplyPanelStyle(btnImg);
            btnImg.color = color;
            var btn = btnGo.AddComponent<Button>();
            var btnLayout = btnGo.AddComponent<LayoutElement>();
            btnLayout.preferredHeight = 52f;

            var btnLabel = CreateLabel(name + "Label", btnGo.transform, label, Color.white, 22);
            btnLabel.GetComponent<Text>().fontStyle = FontStyle.Bold;
            btnLabel.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            btnLabel.GetComponent<Text>().raycastTarget = false;
            var labelRect = btnLabel.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            labelRect.sizeDelta = Vector2.zero;

            return btn;
        };

        // Four pause menu buttons: CONTINUE (Green), SETTINGS (Cyan), RETURN TO MENU (Amber), EXIT (Red)
        refs.ContinueButton = createPauseButton("ContinueButton", "CONTINUE", ColorPalette.Green);
        refs.SettingsButton = createPauseButton("SettingsButton", "SETTINGS", ColorPalette.Cyan);
        refs.ReturnToMenuButton = createPauseButton("ReturnToMenuButton", "RETURN TO MENU", ColorPalette.Amber);
        refs.ExitButton = createPauseButton("ExitButton", "EXIT", ColorPalette.Red);

        // Confirmation popup (initially hidden): "Abandon current run?" with YES/NO
        var confirmGo = new GameObject("ConfirmationPopup");
        confirmGo.transform.SetParent(canvasGo.transform, false);
        var confirmRect = confirmGo.AddComponent<RectTransform>();
        confirmRect.anchorMin = new Vector2(0.35f, 0.4f);
        confirmRect.anchorMax = new Vector2(0.65f, 0.6f);
        confirmRect.offsetMin = Vector2.zero;
        confirmRect.offsetMax = Vector2.zero;
        var confirmImg = confirmGo.AddComponent<Image>();
        CRTThemeData.ApplyPanelStyle(confirmImg);

        var confirmVlg = confirmGo.AddComponent<VerticalLayoutGroup>();
        confirmVlg.spacing = 16f;
        confirmVlg.childAlignment = TextAnchor.MiddleCenter;
        confirmVlg.childForceExpandWidth = true;
        confirmVlg.childForceExpandHeight = false;
        confirmVlg.padding = new RectOffset(20, 20, 20, 20);

        var confirmLabel = CreateLabel("ConfirmText", confirmGo.transform, "Abandon current run?", ColorPalette.Amber, 24);
        confirmLabel.GetComponent<Text>().fontStyle = FontStyle.Bold;
        var confirmLabelLayout = confirmLabel.AddComponent<LayoutElement>();
        confirmLabelLayout.preferredHeight = 40f;

        // YES/NO buttons row
        var btnRowGo = new GameObject("ButtonRow");
        btnRowGo.transform.SetParent(confirmGo.transform, false);
        btnRowGo.AddComponent<RectTransform>();
        var btnRowHlg = btnRowGo.AddComponent<HorizontalLayoutGroup>();
        btnRowHlg.spacing = 20f;
        btnRowHlg.childAlignment = TextAnchor.MiddleCenter;
        btnRowHlg.childForceExpandWidth = false;
        btnRowHlg.childForceExpandHeight = false;
        var btnRowLayout = btnRowGo.AddComponent<LayoutElement>();
        btnRowLayout.preferredHeight = 48f;

        // YES button (Red — destructive action)
        var yesBtnGo = new GameObject("YesButton");
        yesBtnGo.transform.SetParent(btnRowGo.transform, false);
        yesBtnGo.AddComponent<RectTransform>();
        var yesBtnImg = yesBtnGo.AddComponent<Image>();
        yesBtnImg.color = ColorPalette.Red;
        CRTThemeData.ApplyPanelStyle(yesBtnImg);
        yesBtnImg.color = ColorPalette.Red;
        refs.ConfirmYesButton = yesBtnGo.AddComponent<Button>();
        var yesLayout = yesBtnGo.AddComponent<LayoutElement>();
        yesLayout.preferredWidth = 120f;
        yesLayout.preferredHeight = 48f;

        var yesLabel = CreateLabel("YesLabel", yesBtnGo.transform, "YES", Color.white, 20);
        yesLabel.GetComponent<Text>().fontStyle = FontStyle.Bold;
        yesLabel.GetComponent<Text>().raycastTarget = false;
        var yesLabelRect = yesLabel.GetComponent<RectTransform>();
        yesLabelRect.anchorMin = Vector2.zero;
        yesLabelRect.anchorMax = Vector2.one;
        yesLabelRect.offsetMin = Vector2.zero;
        yesLabelRect.offsetMax = Vector2.zero;
        yesLabelRect.sizeDelta = Vector2.zero;

        // NO button (Green — safe action)
        var noBtnGo = new GameObject("NoButton");
        noBtnGo.transform.SetParent(btnRowGo.transform, false);
        noBtnGo.AddComponent<RectTransform>();
        var noBtnImg = noBtnGo.AddComponent<Image>();
        noBtnImg.color = ColorPalette.Green;
        CRTThemeData.ApplyPanelStyle(noBtnImg);
        noBtnImg.color = ColorPalette.Green;
        refs.ConfirmNoButton = noBtnGo.AddComponent<Button>();
        var noLayout = noBtnGo.AddComponent<LayoutElement>();
        noLayout.preferredWidth = 120f;
        noLayout.preferredHeight = 48f;

        var noLabel = CreateLabel("NoLabel", noBtnGo.transform, "NO", Color.white, 20);
        noLabel.GetComponent<Text>().fontStyle = FontStyle.Bold;
        noLabel.GetComponent<Text>().raycastTarget = false;
        var noLabelRect = noLabel.GetComponent<RectTransform>();
        noLabelRect.anchorMin = Vector2.zero;
        noLabelRect.anchorMax = Vector2.one;
        noLabelRect.offsetMin = Vector2.zero;
        noLabelRect.offsetMax = Vector2.zero;
        noLabelRect.sizeDelta = Vector2.zero;

        refs.ConfirmationPopup = confirmGo;
        confirmGo.SetActive(false);

        // Start hidden
        canvasGo.SetActive(false);

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[Setup] PauseMenuUI created: canvas sortingOrder=155, 4 buttons + confirmation popup");
        #endif

        return refs;
    }

    // ════════════════════════════════════════════════════════════════════
    // HELPER STRUCTS
    // ════════════════════════════════════════════════════════════════════

    /// <summary>Temporary struct for slider creation.</summary>
    private struct SliderRow
    {
        public Slider Slider;
        public Text ValueText;
    }

    /// <summary>
    /// Adds hover scale + hover SFX + click punch to a trading button.
    /// </summary>
    private static void AddTradingButtonFeel(Button btn)
    {
        if (btn == null) return;

        var trigger = btn.gameObject.GetComponent<EventTrigger>()
                   ?? btn.gameObject.AddComponent<EventTrigger>();

        var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener(_ =>
        {
            btn.transform.DOKill();
            btn.transform.localScale = Vector3.one;
            btn.transform.DOScale(1.05f, 0.1f);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonHover();
        });
        trigger.triggers.Add(enterEntry);

        var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener(_ =>
        {
            btn.transform.DOKill();
            btn.transform.localScale = Vector3.one;
        });
        trigger.triggers.Add(exitEntry);

        btn.onClick.AddListener(() =>
        {
            btn.transform.DOKill();
            btn.transform.localScale = Vector3.one;
            btn.transform.DOPunchScale(new Vector3(-0.08f, -0.08f, 0f), 0.15f, 1, 0f);
        });
    }

    // ════════════════════════════════════════════════════════════════════
    // Story 17.8: Relic Bar — Trading Phase Relic Display & Tooltips
    // ════════════════════════════════════════════════════════════════════

    public static RelicBar ExecuteRelicBar(RunContext ctx)
    {
        var dashRefs = DashRefs;

        // Create relic bar canvas (above Control Deck, below feedback)
        var canvasGo = new GameObject("RelicBarCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 21; // Between Control Deck (20) and feedback (23)

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Relic bar root panel — top-right area below event ticker
        var barGo = new GameObject("RelicBarRoot");
        barGo.transform.SetParent(canvasGo.transform, false);
        var barRect = barGo.AddComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.55f, 0.88f);
        barRect.anchorMax = new Vector2(0.95f, 0.96f);
        barRect.offsetMin = Vector2.zero;
        barRect.offsetMax = Vector2.zero;

        // Background panel styling
        var barBg = barGo.AddComponent<Image>();
        barBg.color = ColorPalette.WithAlpha(CRTThemeData.Panel, 0.8f);
        barBg.raycastTarget = false;
        CRTThemeData.ApplyPanelStyle(barBg);

        // HorizontalLayoutGroup for icon spacing
        var hlg = barGo.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6f;
        hlg.padding = new RectOffset(8, 8, 4, 4);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // Content size fitter to prevent overflow
        var csf = barGo.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ── Tooltip Panel ──
        var tooltipGo = new GameObject("RelicBarTooltip");
        tooltipGo.transform.SetParent(canvasGo.transform, false);
        var tooltipRect = tooltipGo.AddComponent<RectTransform>();
        tooltipRect.sizeDelta = new Vector2(260f, 120f);
        tooltipRect.pivot = new Vector2(0.5f, 0f);

        var tooltipBg = tooltipGo.AddComponent<Image>();
        tooltipBg.color = ColorPalette.WithAlpha(CRTThemeData.Background, 0.95f);
        CRTThemeData.ApplyPanelStyle(tooltipBg);

        var tooltipCg = tooltipGo.AddComponent<CanvasGroup>();
        tooltipCg.alpha = 0f;
        tooltipCg.blocksRaycasts = false;

        // Vertical layout for tooltip content
        var tooltipVlg = tooltipGo.AddComponent<VerticalLayoutGroup>();
        tooltipVlg.padding = new RectOffset(8, 8, 6, 6);
        tooltipVlg.spacing = 3f;
        tooltipVlg.childAlignment = TextAnchor.UpperLeft;
        tooltipVlg.childForceExpandWidth = true;
        tooltipVlg.childForceExpandHeight = false;

        // Content size fitter for tooltip
        var tooltipCsf = tooltipGo.AddComponent<ContentSizeFitter>();
        tooltipCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Tooltip text fields
        var nameGo = CreateLabel("TooltipName", tooltipGo.transform, "", CRTThemeData.TextHigh, 16);
        nameGo.GetComponent<Text>().alignment = TextAnchor.UpperLeft;
        nameGo.GetComponent<Text>().supportRichText = true;
        var nameText = nameGo.GetComponent<Text>();

        var descGo = CreateLabel("TooltipDesc", tooltipGo.transform, "", CRTThemeData.TextLow, 13);
        descGo.GetComponent<Text>().alignment = TextAnchor.UpperLeft;
        var descText = descGo.GetComponent<Text>();

        var effectGo = CreateLabel("TooltipEffect", tooltipGo.transform, "", ColorPalette.Amber, 13);
        effectGo.GetComponent<Text>().alignment = TextAnchor.UpperLeft;
        effectGo.GetComponent<Text>().fontStyle = FontStyle.Italic;
        var effectText = effectGo.GetComponent<Text>();

        tooltipGo.SetActive(false);

        // Wire DashboardReferences
        if (dashRefs != null)
        {
            dashRefs.RelicBarRoot = barRect;
            dashRefs.RelicBarTooltip = tooltipGo;
        }

        // Create RelicBar MonoBehaviour and initialize
        var relicBar = barGo.AddComponent<RelicBar>();
        relicBar.Initialize(ctx, barGo.transform, tooltipGo, nameText, descText, effectText);

        return relicBar;
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
