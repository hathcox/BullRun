using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Bootstrap MonoBehaviour. Created by GameRunnerSetup during F5 rebuild.
/// Creates core systems and UI at runtime, kicks off the game loop,
/// and drives the state machine every frame via Update.
///
/// Setup classes that create MonoBehaviours with Initialize()/EventBus subscriptions
/// MUST run here at runtime — not during F5 — because private fields and subscriptions
/// don't survive the edit-time → play-time serialization boundary.
/// </summary>
public class GameRunner : MonoBehaviour
{
    /// <summary>Cached instance set in Awake, used by UISetup button wiring to avoid FindObjectOfType per click.</summary>
    internal static GameRunner Instance { get; private set; }

    // FIX-11: Short lifecycle state machine
    public enum ShortState { RoundLockout, Ready, Holding, CashOutWindow, Cooldown }

    private GameStateMachine _stateMachine;
    private RunContext _ctx;
    private PriceGenerator _priceGenerator;
    private TradeExecutor _tradeExecutor;
    private EventScheduler _eventScheduler;
    private QuantitySelector _quantitySelector;
    private PauseMenuUI _pauseMenuUI;
    private bool _firstFrameSkipped;

    // Post-trade cooldown (FIX-10 v2): instant trade, then lock out both buttons
    private float _postTradeCooldownTimer;
    private bool _isPostTradeCooldownActive;

    // FIX-11: Short state machine fields (slot 1)
    private ShortState _shortState = ShortState.RoundLockout;
    private float _shortTimer;
    private float _shortEntryPrice;
    private int _shortShares;
    private string _shortStockId;
    private int _shortOpenStockId = -1; // Numeric stock ID for price lookups (multi-stock safe)

    // Story 13.7: Dual Short — second short slot fields
    private ShortState _short2State = ShortState.RoundLockout;
    private float _short2Timer;
    private float _short2EntryPrice;
    private int _short2Shares;
    private string _short2StockId;
    private int _short2OpenStockId = -1; // Numeric stock ID for price lookups (multi-stock safe)
    private bool _dualShortActive;

    // Buy/Sell UI references (wired from DashboardReferences in Start)
    private Button _buyButton;
    private Image _buyButtonImage;
    private Text _buyButtonText;
    private Color _buyButtonOriginalColor;
    private Button _sellButton;
    private Image _sellButtonImage;
    private Text _sellButtonText;
    private Color _sellButtonOriginalColor;

    // Short UI references (wired from DashboardReferences in Start)
    private Image _shortButtonImage;
    private Text _shortButtonText;
    private Color _shortButtonOriginalColor;
    private float _shortFlashTimer;

    // Story 13.7: Second short UI references
    private Image _short2ButtonImage;
    private Text _short2ButtonText;
    private Color _short2ButtonOriginalColor;
    private float _short2FlashTimer;

    private void Awake()
    {
        Instance = this;

        // Story 16.1: Load saved settings before any audio plays
        SettingsManager.Load();

        // Clear stale EventBus subscriptions from previous play sessions
        EventBus.Clear();

        // Story 16.1: Create lightweight RunContext without publishing RunStartedEvent.
        // The real run initialization happens when START GAME is clicked.
        _ctx = new RunContext(1, 1, new Portfolio(GameConfig.StartingCapital));
        _priceGenerator = new PriceGenerator();
        _tradeExecutor = new TradeExecutor();
        _stateMachine = new GameStateMachine(_ctx);

        // Create event system: EventEffects processes effects, EventScheduler decides when to fire
        var eventEffects = new EventEffects();
        _priceGenerator.SetEventEffects(eventEffects);
        _eventScheduler = new EventScheduler(eventEffects);

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[GameRunner] Awake: Systems created, starting capital ${GameConfig.StartingCapital}");
        #endif
    }

    private void Start()
    {
        // Create chart system (subscribes to PriceUpdatedEvent, RoundStartedEvent, etc.)
        ChartSetup.Execute();

        // Create all UI systems with runtime data
        UISetup.Execute(_ctx, _ctx.CurrentRound, GameConfig.RoundDurationSeconds);
        UISetup.ExecuteMarketOpenUI();

        // Story 16.1: Create main menu and settings panel UI
        var menuRefs = UISetup.ExecuteMainMenuUI();
        var settingsRefs = UISetup.ExecuteSettingsUI();

        // Story 14.6: CRT bezel overlay (vignette + scanlines on top of everything)
        UISetup.ExecuteCRTOverlay();

        // Story 14.6: URP Bloom post-processing for phosphor glow
        UISetup.ExecuteBloomSetup();

        // Create trade feedback overlay
        UISetup.ExecuteTradeFeedback();

        // Story 14.4: Create QuantitySelector (trade quantity logic only — UI now in Control Deck)
        var qsGo = new GameObject("QuantitySelector");
        _quantitySelector = qsGo.AddComponent<QuantitySelector>();
        _quantitySelector.Initialize();

        // Story 14.4: Wire short/cooldown UI refs from DashboardReferences (populated by ExecuteControlDeck)
        var dashRefs = UISetup.DashRefs;
        _quantitySelector.CooldownOverlay = dashRefs.CooldownOverlay;
        _quantitySelector.CooldownTimerText = dashRefs.CooldownTimerText;

        // Buy/Sell button refs for cooldown visuals
        _buyButton = dashRefs.BuyButton;
        _buyButtonImage = dashRefs.BuyButtonImage;
        _buyButtonText = dashRefs.BuyButtonText;
        if (_buyButtonImage != null)
            _buyButtonOriginalColor = _buyButtonImage.color;

        _sellButton = dashRefs.SellButton;
        _sellButtonImage = dashRefs.SellButtonImage;
        _sellButtonText = dashRefs.SellButtonText;
        if (_sellButtonImage != null)
            _sellButtonOriginalColor = _sellButtonImage.color;

        _shortButtonImage = dashRefs.ShortButtonImage;
        _shortButtonText = dashRefs.ShortButtonText;
        if (_shortButtonImage != null)
            _shortButtonOriginalColor = _shortButtonImage.color;

        // Story 13.7: Second short UI references from DashboardReferences
        _short2ButtonImage = dashRefs.Short2ButtonImage;
        _short2ButtonText = dashRefs.Short2ButtonText;
        if (_short2ButtonImage != null)
            _short2ButtonOriginalColor = _short2ButtonImage.color;

        // Story 14.4: Wire expansion visibility refs from DashboardReferences
        _quantitySelector.LeverageBadge = dashRefs.LeverageBadge;
        _quantitySelector.Short2Container = dashRefs.Short2Container;

        // Subscribe to trade button clicks from UI
        EventBus.Subscribe<TradeButtonPressedEvent>(OnTradeButtonPressed);

        // FIX-10 v2: Cancel post-trade cooldown when trading phase ends
        // FIX-11: Also resets short state machine on trading phase end
        EventBus.Subscribe<TradingPhaseEndedEvent>(OnTradingPhaseEnded);

        // FIX-11: Start short round lockout when a round starts
        EventBus.Subscribe<RoundStartedEvent>(OnRoundStartedForShort);

        // FIX-7: Wire position overlay to track the active stock
        EventBus.Subscribe<MarketOpenEvent>(OnMarketOpenForExpansions);

        // Story 16.1: Subscribe to StartGameRequestedEvent from MainMenuUI
        EventBus.Subscribe<StartGameRequestedEvent>(OnStartGameRequested);

        // Story 16.2: Subscribe to ReturnToMenuEvent from PauseMenuUI
        EventBus.Subscribe<ReturnToMenuEvent>(OnReturnToMenu);

        // Story 17.1: Wire EventBus events to RelicManager dispatch
        EventBus.Subscribe<RoundStartedEvent>(OnRoundStartedForRelics);
        EventBus.Subscribe<MarketClosedEvent>(OnMarketClosedForRelics);
        EventBus.Subscribe<TradeExecutedEvent>(OnTradeExecutedForRelics);
        EventBus.Subscribe<MarketEventFiredEvent>(OnMarketEventForRelics);
        _ctx.Reputation.OnChanged += OnReputationChangedForRelics;

        // Create event display systems (subscribe to MarketEventFiredEvent)
        // Story 14.5: NewsBanner replaced by EventTickerBanner (created in ChartSetup).
        // NewsTicker removed — covered by Control Deck.
        UISetup.ExecuteScreenEffects();
        UISetup.ExecuteEventPopup();

        // Create overlay UIs that subscribe to state transition events
        UISetup.ExecuteRoundResultsUI();
        UISetup.ExecuteRunSummaryUI();
        UISetup.ExecuteTierTransitionUI();
        UISetup.ExecuteStoreUI();

        // Game Feel system: screen shake, flash overlays, particles, scale punches
        GameFeelSetup.Execute();

        // Audio system: SFX playback via MMSoundManager, EventBus-driven
        AudioSetup.Execute();

        // Story 16.1: Create MainMenuUI controller and wire references
        var menuUiGo = new GameObject("MainMenuUI");
        var mainMenuUI = menuUiGo.AddComponent<MainMenuUI>();
        mainMenuUI.Initialize(menuRefs, settingsRefs);

        // Story 16.2: Create pause menu UI and controller
        var pauseRefs = UISetup.ExecutePauseMenuUI();
        var pauseUiGo = new GameObject("PauseMenuUI");
        _pauseMenuUI = pauseUiGo.AddComponent<PauseMenuUI>();
        _pauseMenuUI.Initialize(pauseRefs, settingsRefs);

        // Story 16.1: Wire MainMenuState canvas references for show/hide control
        MainMenuState.MainMenuCanvasGo = menuRefs.MainMenuCanvas.gameObject;
        MainMenuState.SettingsCanvasGo = settingsRefs.SettingsCanvas.gameObject;

        // Collect ALL gameplay canvases that should be hidden during main menu.
        // Exclude MainMenu, Settings, PauseMenu, and CRTOverlay canvases (they are overlay-managed).
        var gameplayCanvasList = new System.Collections.Generic.List<GameObject>();
        var allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in allCanvases)
        {
            if (c == menuRefs.MainMenuCanvas) continue;
            if (c == settingsRefs.SettingsCanvas) continue;
            if (c == pauseRefs.PauseMenuCanvas) continue;
            // CRTOverlay is sortingOrder 200 — always on top, never hidden
            if (c.sortingOrder >= 200) continue;
            gameplayCanvasList.Add(c.gameObject);
        }
        MainMenuState.GameplayCanvases = gameplayCanvasList.ToArray();

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Find ChartRenderer from ChartDataHolder for debug wiring
        ChartRenderer chartRendererRef = null;
        var holder = Object.FindFirstObjectByType<ChartDataHolder>();
        if (holder != null) chartRendererRef = holder.Renderer;
        DebugSetup.Execute(_priceGenerator, chartRendererRef, _ctx, _stateMachine, _tradeExecutor);
        #endif

        // Story 16.1: Start at main menu instead of jumping directly into gameplay
        _stateMachine.TransitionTo<MainMenuState>();

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[GameRunner] Start: All runtime systems created, main menu displayed");
        #endif
    }

    private void Update()
    {
        // Skip the first frame — deltaTime is massive after Start() setup work,
        // which would instantly expire the MarketOpen 7s preview timer.
        if (!_firstFrameSkipped)
        {
            _firstFrameSkipped = true;
            return;
        }

        _stateMachine.Update();

        // Story 16.2: ESC key handling for pause menu
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            HandleEscapeInput();
        }

        // Story 14.4: Action buttons are now part of Control Deck (always visible).
        // Button clicks are gated by TradingState.IsActive checks in OnTradeButtonPressed / HandleShortInput.

        // FIX-10 v2: Tick post-trade cooldown timer and update countdown display
        if (_isPostTradeCooldownActive)
        {
            _postTradeCooldownTimer -= Time.deltaTime;
            if (_postTradeCooldownTimer <= 0f)
            {
                _postTradeCooldownTimer = 0f;
                _isPostTradeCooldownActive = false;
                HideCooldownOverlay();
            }
            else
            {
                UpdateCooldownTimerDisplay();
            }
        }

        // FIX-11: Update short state machine
        if (TradingState.IsActive)
        {
            UpdateShortStateMachine();
            if (_dualShortActive)
                UpdateShort2StateMachine();
        }

        HandleTradingInput();
    }

    // ════════════════════════════════════════════════════════════════════
    // Story 16.1: START GAME from Main Menu
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Handles START GAME button click from the main menu.
    /// Initializes a fresh run and transitions to MarketOpenState.
    /// </summary>
    private void OnStartGameRequested(StartGameRequestedEvent evt)
    {
        // Initialize fresh run context (publishes RunStartedEvent for audio/music).
        // ResetForNewRun handles portfolio subscription, round start, and all state reset.
        _ctx.ResetForNewRun();

        // Configure and transition to MarketOpenState
        MarketOpenState.NextConfig = new MarketOpenStateConfig
        {
            StateMachine = _stateMachine,
            PriceGenerator = _priceGenerator,
            TradeExecutor = _tradeExecutor,
            EventScheduler = _eventScheduler
        };
        _stateMachine.TransitionTo<MarketOpenState>();

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[GameRunner] START GAME: Fresh run started, capital ${GameConfig.StartingCapital}");
        #endif
    }

    // ════════════════════════════════════════════════════════════════════
    // Story 16.2: ESC INPUT & PAUSE MENU
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Story 16.2: Handles ESC key press. Ignored during MainMenuState.
    /// Forwards to PauseMenuUI for layered ESC logic.
    /// </summary>
    private void HandleEscapeInput()
    {
        // AC 15: ESC ignored during main menu
        if (MainMenuState.IsActive) return;

        // AC 19: ESC ignored while "Coming Soon" popup is visible
        if (UISetup.MenuRefs?.ComingSoonPopup != null && UISetup.MenuRefs.ComingSoonPopup.activeSelf) return;

        if (_pauseMenuUI != null)
            _pauseMenuUI.HandleEscapePressed();
    }

    /// <summary>
    /// Story 16.2: Handles return-to-menu from pause menu.
    /// Cleans up gameplay state and transitions to MainMenuState.
    /// </summary>
    private void OnReturnToMenu(ReturnToMenuEvent evt)
    {
        // Ensure timeScale is restored
        Time.timeScale = 1f;

        // Reset short state machines
        ResetShortOnRoundEnd();

        // Cancel post-trade cooldown
        if (_isPostTradeCooldownActive)
        {
            _isPostTradeCooldownActive = false;
            _postTradeCooldownTimer = 0f;
            HideCooldownOverlay();
        }

        // Hide all gameplay overlays
        HideAllGameplayOverlays();

        // Transition to MainMenuState (shows menu, plays title music)
        _stateMachine.TransitionTo<MainMenuState>();

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[GameRunner] Return to menu — gameplay state cleaned up, transitioning to MainMenuState");
        #endif
    }

    /// <summary>
    /// Story 16.2: Hides all gameplay overlay UIs for return-to-menu cleanup.
    /// Uses the cached GameplayCanvases array (built in Start) instead of per-type scene searches.
    /// MainMenuState.Enter() also hides these canvases, but we do it early as a defensive measure
    /// before the state transition to ensure no overlay flickers during cleanup.
    /// </summary>
    private void HideAllGameplayOverlays()
    {
        if (MainMenuState.GameplayCanvases == null) return;
        for (int i = 0; i < MainMenuState.GameplayCanvases.Length; i++)
        {
            if (MainMenuState.GameplayCanvases[i] != null)
                MainMenuState.GameplayCanvases[i].SetActive(false);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // Story 17.1: Relic dispatch handlers
    // ════════════════════════════════════════════════════════════════════

    private void OnRoundStartedForRelics(RoundStartedEvent e)
    {
        _ctx.RelicManager.DispatchRoundStart(e);
    }

    private void OnMarketClosedForRelics(MarketClosedEvent e)
    {
        _ctx.RelicManager.DispatchRoundEnd(e);
    }

    private void OnTradeExecutedForRelics(TradeExecutedEvent e)
    {
        _ctx.RelicManager.DispatchAfterTrade(e);
    }

    private void OnMarketEventForRelics(MarketEventFiredEvent e)
    {
        _ctx.RelicManager.DispatchMarketEvent(e);
    }

    private void OnReputationChangedForRelics(int oldRep, int newRep)
    {
        _ctx.RelicManager.DispatchReputationChanged(oldRep, newRep);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<TradeButtonPressedEvent>(OnTradeButtonPressed);
        EventBus.Unsubscribe<TradingPhaseEndedEvent>(OnTradingPhaseEnded);
        EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStartedForShort);
        EventBus.Unsubscribe<MarketOpenEvent>(OnMarketOpenForExpansions);
        EventBus.Unsubscribe<StartGameRequestedEvent>(OnStartGameRequested);
        EventBus.Unsubscribe<ReturnToMenuEvent>(OnReturnToMenu);
        EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStartedForRelics);
        EventBus.Unsubscribe<MarketClosedEvent>(OnMarketClosedForRelics);
        EventBus.Unsubscribe<TradeExecutedEvent>(OnTradeExecutedForRelics);
        EventBus.Unsubscribe<MarketEventFiredEvent>(OnMarketEventForRelics);
        if (_ctx != null && _ctx.Reputation != null)
            _ctx.Reputation.OnChanged -= OnReputationChangedForRelics;
    }

    /// <summary>
    /// Configures expansion UI visibility when the market opens.
    /// </summary>
    private void OnMarketOpenForExpansions(MarketOpenEvent evt)
    {
        // Story 13.7: Leverage badge visibility
        if (_quantitySelector.LeverageBadge != null)
            _quantitySelector.LeverageBadge.SetActive(_ctx.OwnedExpansions.Contains(ExpansionDefinitions.LeverageTrading));

        // Story 13.7: Dual Short visibility
        _dualShortActive = _ctx.OwnedExpansions.Contains(ExpansionDefinitions.DualShort);
        if (_quantitySelector.Short2Container != null)
            _quantitySelector.Short2Container.SetActive(_dualShortActive);
    }

    // ========================
    // FIX-11: Short State Machine
    // ========================

    /// <summary>
    /// FIX-11: Initializes the short state machine to RoundLockout when a round starts.
    /// </summary>
    private void OnRoundStartedForShort(RoundStartedEvent evt)
    {
        // Story 17.3: Apply locked visuals if LongsDisabled (Bear Raid)
        if (_ctx.LongsDisabled)
            ApplyLongsDisabledVisuals();

        _shortEntryPrice = 0f;
        _shortShares = 0;
        _shortStockId = null;
        _shortOpenStockId = -1;

        // Skip lockout entirely if duration is 0
        if (GameConfig.ShortRoundStartLockout <= 0f)
        {
            _shortState = ShortState.Ready;
            _shortTimer = 0f;
        }
        else
        {
            _shortState = ShortState.RoundLockout;
            _shortTimer = GameConfig.ShortRoundStartLockout;
        }

        // Story 13.7: Reset second short
        _short2EntryPrice = 0f;
        _short2Shares = 0;
        _short2StockId = null;
        _short2OpenStockId = -1;
        if (GameConfig.ShortRoundStartLockout <= 0f)
        {
            _short2State = ShortState.Ready;
            _short2Timer = 0f;
        }
        else
        {
            _short2State = ShortState.RoundLockout;
            _short2Timer = GameConfig.ShortRoundStartLockout;
        }
    }

    /// <summary>
    /// FIX-11: Updates the short state machine every frame during trading.
    /// Timer-driven transitions for lockout/hold/cashout/cooldown.
    /// Input-driven transitions handled in HandleShortInput().
    /// </summary>
    private void UpdateShortStateMachine()
    {
        float dt = Time.deltaTime;

        switch (_shortState)
        {
            case ShortState.RoundLockout:
                _shortTimer -= dt;
                if (_shortTimer <= 0f)
                {
                    _shortState = ShortState.Ready;
                    _shortTimer = 0f;
                }
                break;

            case ShortState.Ready:
                // Waiting for player input (D key or button click)
                break;

            case ShortState.Holding:
                _shortTimer -= dt;
                if (_shortStockId != null)
                    EventBus.Publish(new ShortCountdownEvent { StockId = _shortStockId, TimeRemaining = _shortTimer, IsCashOutWindow = false });
                if (_shortTimer <= 0f)
                {
                    _shortState = ShortState.CashOutWindow;
                    _shortTimer = GameConfig.ShortCashOutWindow;
                }
                break;

            case ShortState.CashOutWindow:
                _shortTimer -= dt;
                if (_shortStockId != null)
                    EventBus.Publish(new ShortCountdownEvent { StockId = _shortStockId, TimeRemaining = _shortTimer, IsCashOutWindow = true });
                if (_shortTimer <= 0f)
                {
                    CloseShortPosition(true);
                }
                break;

            case ShortState.Cooldown:
                _shortTimer -= dt;
                if (_shortTimer <= 0f)
                {
                    _shortState = ShortState.Ready;
                    _shortTimer = 0f;
                }
                break;
        }

        UpdateShortButtonVisuals();
    }

    /// <summary>
    /// FIX-11: Opens a short position. Called when player presses D or clicks SHORT in Ready state.
    /// </summary>
    private void OpenShortPosition()
    {
        int stockId = GetSelectedStockId();
        if (stockId < 0) return;

        float currentPrice = GetStockPrice(stockId);
        if (currentPrice <= 0f) return;

        string stockIdStr = stockId.ToString();
        string ticker = GetSelectedTicker();
        int shares = _ctx.RelicManager.GetEffectiveShortShares();

        // Story 17.3 review fix: Fire RelicActivatedEvent when Bear Raid modifies short shares (AC 10)
        if (shares != GameConfig.ShortBaseShares)
            EventBus.Publish(new RelicActivatedEvent { RelicId = "relic_short_multiplier" });

        bool success = _tradeExecutor.ExecuteShort(stockIdStr, shares, currentPrice, _ctx.Portfolio);
        if (success)
        {
            _shortState = ShortState.Holding;
            _shortTimer = GameConfig.ShortForcedHoldDuration;
            _shortEntryPrice = currentPrice;
            _shortShares = shares;
            _shortStockId = stockIdStr;
            _shortOpenStockId = stockId;

            EventBus.Publish(new TradeFeedbackEvent
            {
                Message = $"SHORTED {shares} @ ${currentPrice:F2}",
                IsSuccess = true, IsBuy = false, IsShort = true
            });

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[GameRunner] Short opened: {shares} shares of {ticker} @ ${currentPrice:F2}");
            #endif
        }
        else
        {
            EventBus.Publish(new TradeFeedbackEvent
            {
                Message = TradeFeedback.GetShortRejectionReason(_ctx.Portfolio, stockIdStr),
                IsSuccess = false, IsBuy = false, IsShort = true
            });
        }
    }

    /// <summary>
    /// FIX-11: Closes the active short position. Called on manual cash-out, auto-close, or round end.
    /// </summary>
    private void CloseShortPosition(bool isAutoClose)
    {
        if (_shortStockId == null) return;

        float currentPrice = GetStockPrice(_shortOpenStockId);
        if (currentPrice <= 0f) currentPrice = _shortEntryPrice; // fallback

        bool success = _tradeExecutor.ExecuteCover(_shortStockId, _shortShares, currentPrice, _ctx.Portfolio);
        float pnl = (_shortEntryPrice - currentPrice) * _shortShares;

        // Award reputation for profitable short trades
        if (success && pnl > 0f)
        {
            _ctx.Reputation.Add(GameConfig.RepPerProfitableTrade);
            _ctx.ReputationEarned += GameConfig.RepPerProfitableTrade;
        }

        string pnlStr = pnl >= 0 ? $"+${pnl:F2}" : $"-${Mathf.Abs(pnl):F2}";
        string prefix = isAutoClose ? "AUTO-CLOSED" : "CASHED OUT";

        EventBus.Publish(new TradeFeedbackEvent
        {
            Message = $"{prefix} {pnlStr}",
            IsSuccess = true, IsBuy = false, IsShort = true
        });

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[GameRunner] Short closed ({prefix}): P&L {pnlStr}");
        #endif

        _shortState = ShortState.Cooldown;
        _shortTimer = GameConfig.ShortPostCloseCooldown;
        _shortEntryPrice = 0f;
        _shortShares = 0;
        _shortStockId = null;
        _shortOpenStockId = -1;
    }

    /// <summary>
    /// FIX-11: Auto-closes short and resets all state at round end.
    /// </summary>
    private void ResetShortOnRoundEnd()
    {
        // If short is active (Holding or CashOutWindow), auto-close at market price
        if (_shortState == ShortState.Holding || _shortState == ShortState.CashOutWindow)
        {
            CloseShortPosition(true);
        }

        // Full reset regardless of state
        _shortState = ShortState.RoundLockout;
        _shortTimer = 0f;
        _shortEntryPrice = 0f;
        _shortShares = 0;
        _shortStockId = null;
        _shortOpenStockId = -1;

        // Story 13.7: Reset second short on round end
        if (_short2State == ShortState.Holding || _short2State == ShortState.CashOutWindow)
        {
            CloseShort2Position(true);
        }
        _short2State = ShortState.RoundLockout;
        _short2Timer = 0f;
        _short2EntryPrice = 0f;
        _short2Shares = 0;
        _short2StockId = null;
        _short2OpenStockId = -1;
    }

    /// <summary>
    /// FIX-11: Updates the SHORT button visual state per current ShortState.
    /// </summary>
    private void UpdateShortButtonVisuals()
    {
        if (_shortButtonImage == null || _shortButtonText == null) return;

        switch (_shortState)
        {
            case ShortState.RoundLockout:
                _shortButtonImage.color = DimColor(_shortButtonOriginalColor);
                _shortButtonText.text = $"{_shortTimer:F1}s";
                break;

            case ShortState.Ready:
                _shortButtonImage.color = _shortButtonOriginalColor;
                _shortButtonText.text = "SHORT";
                break;

            case ShortState.Holding:
                _shortButtonImage.color = DimColor(_shortButtonOriginalColor);
                _shortButtonText.text = $"{_shortTimer:F1}s";
                break;

            case ShortState.CashOutWindow:
                _shortButtonImage.color = _shortButtonOriginalColor;
                _shortButtonText.text = $"CASH OUT ({Mathf.CeilToInt(_shortTimer)}s)";
                // Flash when <= threshold seconds remain
                if (_shortTimer <= GameConfig.ShortCashOutFlashThreshold)
                {
                    _shortFlashTimer += Time.deltaTime * 6f; // ~3 flashes per second
                    float alpha = 0.6f + Mathf.Sin(_shortFlashTimer) * 0.4f;
                    Color flashColor = _shortButtonOriginalColor;
                    flashColor.a = alpha;
                    _shortButtonImage.color = flashColor;
                }
                break;

            case ShortState.Cooldown:
                _shortButtonImage.color = DimColor(_shortButtonOriginalColor);
                _shortButtonText.text = $"{_shortTimer:F1}s";
                _shortFlashTimer = 0f;
                break;
        }
    }

    // ========================
    // Story 13.7: Dual Short — Second Short State Machine
    // ========================

    private void UpdateShort2StateMachine()
    {
        float dt = Time.deltaTime;
        switch (_short2State)
        {
            case ShortState.RoundLockout:
                _short2Timer -= dt;
                if (_short2Timer <= 0f) { _short2State = ShortState.Ready; _short2Timer = 0f; }
                break;
            case ShortState.Ready:
                break;
            case ShortState.Holding:
                _short2Timer -= dt;
                if (_short2StockId != null)
                    EventBus.Publish(new ShortCountdownEvent { StockId = _short2StockId, TimeRemaining = _short2Timer, IsCashOutWindow = false });
                if (_short2Timer <= 0f) { _short2State = ShortState.CashOutWindow; _short2Timer = GameConfig.ShortCashOutWindow; }
                break;
            case ShortState.CashOutWindow:
                _short2Timer -= dt;
                if (_short2StockId != null)
                    EventBus.Publish(new ShortCountdownEvent { StockId = _short2StockId, TimeRemaining = _short2Timer, IsCashOutWindow = true });
                if (_short2Timer <= 0f) CloseShort2Position(true);
                break;
            case ShortState.Cooldown:
                _short2Timer -= dt;
                if (_short2Timer <= 0f) { _short2State = ShortState.Ready; _short2Timer = 0f; }
                break;
        }
        UpdateShort2ButtonVisuals();
    }

    public void HandleShort2Input()
    {
        if (!TradingState.IsActive) return;

        switch (_short2State)
        {
            case ShortState.Ready:
                OpenShort2Position();
                break;
            case ShortState.CashOutWindow:
                CloseShort2Position(false);
                break;
        }
    }

    private void OpenShort2Position()
    {
        int stockId = GetSelectedStockId();
        if (stockId < 0) return;
        float currentPrice = GetStockPrice(stockId);
        if (currentPrice <= 0f) return;

        string stockIdStr = stockId.ToString();
        int shares = _ctx.RelicManager.GetEffectiveShortShares();

        // Story 17.3 review fix: Fire RelicActivatedEvent when Bear Raid modifies short shares (AC 10)
        if (shares != GameConfig.ShortBaseShares)
            EventBus.Publish(new RelicActivatedEvent { RelicId = "relic_short_multiplier" });

        bool success = _tradeExecutor.ExecuteShort(stockIdStr + "_s2", shares, currentPrice, _ctx.Portfolio);
        if (success)
        {
            _short2State = ShortState.Holding;
            _short2Timer = GameConfig.ShortForcedHoldDuration;
            _short2EntryPrice = currentPrice;
            _short2Shares = shares;
            _short2StockId = stockIdStr + "_s2";
            _short2OpenStockId = stockId;

            EventBus.Publish(new TradeFeedbackEvent
            {
                Message = $"SHORTED #2 {shares} @ ${currentPrice:F2}",
                IsSuccess = true, IsBuy = false, IsShort = true
            });
        }
    }

    private void CloseShort2Position(bool isAutoClose)
    {
        if (_short2StockId == null) return;
        float currentPrice = GetStockPrice(_short2OpenStockId);
        if (currentPrice <= 0f) currentPrice = _short2EntryPrice;

        bool success = _tradeExecutor.ExecuteCover(_short2StockId, _short2Shares, currentPrice, _ctx.Portfolio);
        float pnl = (_short2EntryPrice - currentPrice) * _short2Shares;

        if (success && pnl > 0f)
        {
            _ctx.Reputation.Add(GameConfig.RepPerProfitableTrade);
            _ctx.ReputationEarned += GameConfig.RepPerProfitableTrade;
        }

        string pnlStr = pnl >= 0 ? $"+${pnl:F2}" : $"-${Mathf.Abs(pnl):F2}";
        string prefix = isAutoClose ? "AUTO-CLOSED #2" : "CASHED OUT #2";

        EventBus.Publish(new TradeFeedbackEvent { Message = $"{prefix} {pnlStr}", IsSuccess = true, IsBuy = false, IsShort = true });

        _short2State = ShortState.Cooldown;
        _short2Timer = GameConfig.ShortPostCloseCooldown;
        _short2EntryPrice = 0f;
        _short2Shares = 0;
        _short2StockId = null;
        _short2OpenStockId = -1;
    }

    private void UpdateShort2ButtonVisuals()
    {
        if (_short2ButtonImage == null || _short2ButtonText == null) return;
        switch (_short2State)
        {
            case ShortState.RoundLockout:
                _short2ButtonImage.color = DimColor(_short2ButtonOriginalColor);
                _short2ButtonText.text = $"{_short2Timer:F1}s";
                break;
            case ShortState.Ready:
                _short2ButtonImage.color = _short2ButtonOriginalColor;
                _short2ButtonText.text = "SHORT 2";
                break;
            case ShortState.Holding:
                _short2ButtonImage.color = DimColor(_short2ButtonOriginalColor);
                _short2ButtonText.text = $"{_short2Timer:F1}s";
                break;
            case ShortState.CashOutWindow:
                _short2ButtonImage.color = _short2ButtonOriginalColor;
                _short2ButtonText.text = $"CASH OUT ({Mathf.CeilToInt(_short2Timer)}s)";
                if (_short2Timer <= GameConfig.ShortCashOutFlashThreshold)
                {
                    _short2FlashTimer += Time.deltaTime * 6f;
                    float alpha = 0.6f + Mathf.Sin(_short2FlashTimer) * 0.4f;
                    Color flashColor = _short2ButtonOriginalColor;
                    flashColor.a = alpha;
                    _short2ButtonImage.color = flashColor;
                }
                break;
            case ShortState.Cooldown:
                _short2ButtonImage.color = DimColor(_short2ButtonOriginalColor);
                _short2ButtonText.text = $"{_short2Timer:F1}s";
                _short2FlashTimer = 0f;
                break;
        }
    }

    private static Color DimColor(Color original)
    {
        Color dimmed = original;
        dimmed.a = GameConfig.CooldownDimAlpha;
        return dimmed;
    }

    // ========================
    // Trading Input
    // ========================

    /// <summary>
    /// Keyboard trading during TradingState.
    /// B = Buy (long only), S = Sell (long only) — blocked during cooldown.
    /// D = Short (Ready state) or Cash Out (CashOutWindow state).
    /// FIX-10 v2: Trades execute instantly, then post-trade cooldown locks Buy/Sell buttons.
    /// FIX-11: D key for short actions, separate from Buy/Sell cooldown.
    /// </summary>
    private void HandleTradingInput()
    {
        if (!TradingState.IsActive) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // FIX-11: D key for short actions (independent of Buy/Sell cooldown)
        if (keyboard.dKey.wasPressedThisFrame)
        {
            HandleShortInput();
        }

        // Buy/Sell trade actions: blocked during post-trade cooldown
        if (_isPostTradeCooldownActive) return;

        // Story 17.3: B/S blocked when LongsDisabled (Bear Raid)
        if (_ctx.LongsDisabled) return;

        if (keyboard.bKey.wasPressedThisFrame)
        {
            if (ExecuteBuy()) StartPostTradeCooldown(true);
        }
        else if (keyboard.sKey.wasPressedThisFrame)
        {
            if (ExecuteSell()) StartPostTradeCooldown(false);
        }
    }

    /// <summary>
    /// FIX-11: Handles D key press (or SHORT button click) based on current short state.
    /// Gated by TradingState.IsActive to prevent trades outside the trading phase.
    /// </summary>
    public void HandleShortInput()
    {
        if (!TradingState.IsActive) return;

        switch (_shortState)
        {
            case ShortState.Ready:
                OpenShortPosition();
                break;
            case ShortState.CashOutWindow:
                CloseShortPosition(false);
                break;
            // All other states: D key ignored
        }
    }

    /// <summary>
    /// Handles TradeButtonPressedEvent from BUY/SELL UI buttons.
    /// FIX-10 v2: Executes trade instantly, then starts post-trade cooldown.
    /// FIX-11: Buy/Sell are pure long-only now.
    /// </summary>
    private void OnTradeButtonPressed(TradeButtonPressedEvent evt)
    {
        if (!TradingState.IsActive) return;
        if (_isPostTradeCooldownActive) return;
        // Story 17.3: Block buy/sell buttons when LongsDisabled (Bear Raid)
        if (_ctx.LongsDisabled) return;

        bool success = evt.IsBuy ? ExecuteBuy() : ExecuteSell();
        if (success) StartPostTradeCooldown(evt.IsBuy);
    }

    /// <summary>
    /// FIX-10 v2 + FIX-11: Cancels cooldowns and resets short state when trading phase ends.
    /// </summary>
    private void OnTradingPhaseEnded(TradingPhaseEndedEvent evt)
    {
        // Cancel post-trade cooldown
        if (_isPostTradeCooldownActive)
        {
            _isPostTradeCooldownActive = false;
            _postTradeCooldownTimer = 0f;
            HideCooldownOverlay();

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[GameRunner] Trading phase ended — cancelled post-trade cooldown");
            #endif
        }

        // FIX-11: Auto-close short and reset state
        ResetShortOnRoundEnd();
    }

    // ========================
    // Post-Trade Cooldown (FIX-10 v2)
    // ========================

    private void StartPostTradeCooldown(bool isBuy)
    {
        float cooldown = _ctx.RelicManager.GetEffectiveCooldown(isBuy);

        // Story 17.3 review fix: Fire RelicActivatedEvent when Quick Draw modifies cooldown (AC 10)
        if (cooldown != GameConfig.PostTradeCooldown)
            EventBus.Publish(new RelicActivatedEvent { RelicId = "relic_quick_draw" });

        if (cooldown <= 0f) return; // Quick Draw: instant buy, no cooldown

        _isPostTradeCooldownActive = true;
        _postTradeCooldownTimer = cooldown;
        ShowCooldownOverlay();

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[GameRunner] Post-trade cooldown started: {cooldown}s (isBuy={isBuy})");
        #endif
    }

    private void ShowCooldownOverlay()
    {
        // Dim both buttons and set non-interactable
        if (_buyButton != null) _buyButton.interactable = false;
        if (_sellButton != null) _sellButton.interactable = false;
        if (_buyButtonImage != null) _buyButtonImage.color = DimColor(_buyButtonOriginalColor);
        if (_sellButtonImage != null) _sellButtonImage.color = DimColor(_sellButtonOriginalColor);
        UpdateCooldownTimerDisplay();
    }

    private void HideCooldownOverlay()
    {
        // Story 17.3: If LongsDisabled, keep buttons locked
        if (_ctx.LongsDisabled)
        {
            ApplyLongsDisabledVisuals();
            return;
        }

        // Restore both buttons to original state
        if (_buyButton != null) _buyButton.interactable = true;
        if (_sellButton != null) _sellButton.interactable = true;
        if (_buyButtonImage != null) _buyButtonImage.color = _buyButtonOriginalColor;
        if (_sellButtonImage != null) _sellButtonImage.color = _sellButtonOriginalColor;
        if (_buyButtonText != null) _buyButtonText.text = "BUY";
        if (_sellButtonText != null) _sellButtonText.text = "SELL";
    }

    /// <summary>
    /// Story 17.3: Dims and disables buy/sell buttons when LongsDisabled (Bear Raid).
    /// </summary>
    private void ApplyLongsDisabledVisuals()
    {
        if (_buyButton != null) _buyButton.interactable = false;
        if (_sellButton != null) _sellButton.interactable = false;
        if (_buyButtonImage != null) _buyButtonImage.color = DimColor(_buyButtonOriginalColor);
        if (_sellButtonImage != null) _sellButtonImage.color = DimColor(_sellButtonOriginalColor);
        if (_buyButtonText != null) _buyButtonText.text = "LOCKED";
        if (_sellButtonText != null) _sellButtonText.text = "LOCKED";
    }

    private void UpdateCooldownTimerDisplay()
    {
        string timer = $"{_postTradeCooldownTimer:F1}s";
        if (_buyButtonText != null) _buyButtonText.text = timer;
        if (_sellButtonText != null) _sellButtonText.text = timer;
    }

    // ========================
    // Trade Execution (FIX-11: pure long-only, no more Smart Sell/Buy)
    // ========================

    /// <summary>
    /// FIX-11: Buy only buys long positions. No more cover logic.
    /// Returns true if a trade was successfully executed (triggers post-trade cooldown).
    /// </summary>
    private bool ExecuteBuy()
    {
        // Story 17.3: Block buys when LongsDisabled (Bear Raid)
        if (_ctx.LongsDisabled)
        {
            EventBus.Publish(new TradeFeedbackEvent
            {
                Message = "LOCKED", IsSuccess = false, IsBuy = true, IsShort = false
            });
            return false;
        }

        int selectedStockId = GetSelectedStockId();
        if (selectedStockId < 0) return false;

        float currentPrice = GetStockPrice(selectedStockId);
        if (currentPrice <= 0f) return false;

        string stockIdStr = selectedStockId.ToString();
        string ticker = GetSelectedTicker();

        int baseQty = _quantitySelector.GetCurrentQuantity(true, false, stockIdStr, currentPrice, _ctx.Portfolio);
        int qty = _ctx.RelicManager.GetEffectiveTradeQuantity(baseQty);
        // Story 17.3 review fix: Clamp doubled qty to what's affordable
        if (qty > baseQty && currentPrice > 0f)
        {
            int maxAffordable = (int)(_ctx.Portfolio.Cash / currentPrice);
            if (qty > maxAffordable) qty = maxAffordable;
        }
        if (qty <= 0)
        {
            EventBus.Publish(new TradeFeedbackEvent
            {
                Message = "Insufficient cash", IsSuccess = false, IsBuy = true, IsShort = false
            });
            return false;
        }
        bool success = _tradeExecutor.ExecuteBuy(stockIdStr, qty, currentPrice, _ctx.Portfolio);
        // Story 17.3 review fix: Fire RelicActivatedEvent after successful trade only (M2)
        if (success && qty != baseQty)
            EventBus.Publish(new RelicActivatedEvent { RelicId = "relic_double_dealer" });
        EventBus.Publish(new TradeFeedbackEvent
        {
            Message = success ? $"BOUGHT {ticker} x{qty}" : "Insufficient cash",
            IsSuccess = success, IsBuy = true, IsShort = false
        });
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(success
            ? $"[Trade] BUY {qty} shares @ ${currentPrice:F2} (Stock {selectedStockId})"
            : "[Trade] BUY rejected — insufficient cash");
        #endif
        return success;
    }

    /// <summary>
    /// FIX-11: Sell only sells long positions. No more short logic.
    /// If no long position exists, shows "No position to sell" feedback.
    /// Returns true if a trade was successfully executed (triggers post-trade cooldown).
    /// </summary>
    private bool ExecuteSell()
    {
        // Story 17.3: Block sells when LongsDisabled (Bear Raid)
        if (_ctx.LongsDisabled)
        {
            EventBus.Publish(new TradeFeedbackEvent
            {
                Message = "LOCKED", IsSuccess = false, IsBuy = false, IsShort = false
            });
            return false;
        }

        int selectedStockId = GetSelectedStockId();
        if (selectedStockId < 0) return false;

        float currentPrice = GetStockPrice(selectedStockId);
        if (currentPrice <= 0f) return false;

        string stockIdStr = selectedStockId.ToString();
        string ticker = GetSelectedTicker();

        // Story 17.3 review fix: Fetch position early for qty clamping and profitability check
        var position = _ctx.Portfolio.GetPosition(stockIdStr);
        float entryPrice = position != null ? position.AverageBuyPrice : 0f;

        int baseQty = _quantitySelector.GetCurrentQuantity(false, false, stockIdStr, currentPrice, _ctx.Portfolio);
        int qty = _ctx.RelicManager.GetEffectiveTradeQuantity(baseQty);
        // Story 17.3 review fix: Clamp doubled qty to available position
        if (qty > baseQty && position != null && qty > position.Shares)
            qty = position.Shares;
        if (qty <= 0)
        {
            EventBus.Publish(new TradeFeedbackEvent
            {
                Message = "No position to sell", IsSuccess = false, IsBuy = false, IsShort = false
            });
            return false;
        }

        bool success = _tradeExecutor.ExecuteSell(stockIdStr, qty, currentPrice, _ctx.Portfolio);
        // Story 17.3 review fix: Fire RelicActivatedEvent after successful trade only (M2)
        if (success && qty != baseQty)
            EventBus.Publish(new RelicActivatedEvent { RelicId = "relic_double_dealer" });
        if (success)
        {
            // Story 13.7 AC 2: Leverage expansion doubles long trade P&L
            if (position != null && _ctx.OwnedExpansions.Contains(ExpansionDefinitions.LeverageTrading))
            {
                float pnl = (currentPrice - entryPrice) * qty;
                _ctx.Portfolio.AddCash(pnl);
            }

            if (currentPrice > entryPrice)
            {
                _ctx.Reputation.Add(GameConfig.RepPerProfitableTrade);
                _ctx.ReputationEarned += GameConfig.RepPerProfitableTrade;
            }
        }
        EventBus.Publish(new TradeFeedbackEvent
        {
            Message = success ? $"SOLD {ticker} x{qty}" : "No position to sell",
            IsSuccess = success, IsBuy = false, IsShort = false
        });
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(success
            ? $"[Trade] SELL {qty} shares @ ${currentPrice:F2} (Stock {selectedStockId}){(_ctx.OwnedExpansions.Contains(ExpansionDefinitions.LeverageTrading) ? " [2x LEVERAGE]" : "")}"
            : "[Trade] SELL rejected — no position to sell");
        #endif
        return success;
    }

    // ========================
    // Utility
    // ========================

    /// <summary>
    /// FIX-15: Always returns the single active stock's ID.
    /// </summary>
    private int GetSelectedStockId()
    {
        if (_priceGenerator.ActiveStocks.Count == 0) return -1;
        return _priceGenerator.ActiveStocks[0].StockId;
    }

    /// <summary>
    /// FIX-15: Always returns the single active stock's ticker.
    /// </summary>
    private string GetSelectedTicker()
    {
        if (_priceGenerator.ActiveStocks.Count == 0) return "???";
        return _priceGenerator.ActiveStocks[0].TickerSymbol;
    }

    /// <summary>
    /// Returns the current price for a given stock ID.
    /// FIX-15: With single stock, this is always ActiveStocks[0] but we keep the
    /// ID-based lookup for Dual Short which tracks stockId per short position.
    /// </summary>
    private float GetStockPrice(int stockId)
    {
        if (_priceGenerator.ActiveStocks.Count == 0) return 0f;
        for (int i = 0; i < _priceGenerator.ActiveStocks.Count; i++)
        {
            if (_priceGenerator.ActiveStocks[i].StockId == stockId)
                return _priceGenerator.ActiveStocks[i].CurrentPrice;
        }
        return _priceGenerator.ActiveStocks[0].CurrentPrice;
    }
}
