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

        // Clear stale EventBus subscriptions from previous play sessions
        EventBus.Clear();

        _ctx = RunContext.StartNewRun();
        _priceGenerator = new PriceGenerator();
        _tradeExecutor = new TradeExecutor();
        _stateMachine = new GameStateMachine(_ctx);

        // Create event system: EventEffects processes effects, EventScheduler decides when to fire
        var eventEffects = new EventEffects();
        _priceGenerator.SetEventEffects(eventEffects);
        _eventScheduler = new EventScheduler(eventEffects);

        // Subscribe portfolio to price updates so GetTotalValue/GetRoundProfit work
        _ctx.Portfolio.SubscribeToPriceUpdates();

        // Set round start baseline so round profit starts at $0 (not starting capital)
        _ctx.Portfolio.StartRound(_ctx.Portfolio.Cash);

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[GameRunner] Awake: RunContext created, starting capital ${GameConfig.StartingCapital}");
        #endif
    }

    private void Start()
    {
        // Create chart system (subscribes to PriceUpdatedEvent, RoundStartedEvent, etc.)
        ChartSetup.Execute();

        // Create all UI systems with runtime data
        UISetup.Execute(_ctx, _ctx.CurrentRound, GameConfig.RoundDurationSeconds);
        UISetup.ExecuteMarketOpenUI();

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

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Find ChartRenderer from ChartDataHolder for debug wiring
        ChartRenderer chartRendererRef = null;
        var holder = Object.FindObjectOfType<ChartDataHolder>();
        if (holder != null) chartRendererRef = holder.Renderer;
        DebugSetup.Execute(_priceGenerator, chartRendererRef, _ctx, _stateMachine, _tradeExecutor);
        #endif

        // Kick off the game loop — skip MetaHub placeholder
        MarketOpenState.NextConfig = new MarketOpenStateConfig
        {
            StateMachine = _stateMachine,
            PriceGenerator = _priceGenerator,
            TradeExecutor = _tradeExecutor,
            EventScheduler = _eventScheduler
        };
        _stateMachine.TransitionTo<MarketOpenState>();

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[GameRunner] Start: All runtime systems created, game loop started");
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

    private void OnDestroy()
    {
        EventBus.Unsubscribe<TradeButtonPressedEvent>(OnTradeButtonPressed);
        EventBus.Unsubscribe<TradingPhaseEndedEvent>(OnTradingPhaseEnded);
        EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStartedForShort);
        EventBus.Unsubscribe<MarketOpenEvent>(OnMarketOpenForExpansions);
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
        int shares = GameConfig.ShortBaseShares;

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
        int shares = GameConfig.ShortBaseShares;

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

        if (keyboard.bKey.wasPressedThisFrame)
        {
            if (ExecuteBuy()) StartPostTradeCooldown();
        }
        else if (keyboard.sKey.wasPressedThisFrame)
        {
            if (ExecuteSell()) StartPostTradeCooldown();
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

        bool success = evt.IsBuy ? ExecuteBuy() : ExecuteSell();
        if (success) StartPostTradeCooldown();
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

    private void StartPostTradeCooldown()
    {
        _isPostTradeCooldownActive = true;
        _postTradeCooldownTimer = GameConfig.PostTradeCooldown;
        ShowCooldownOverlay();

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[GameRunner] Post-trade cooldown started: {GameConfig.PostTradeCooldown}s");
        #endif
    }

    private void ShowCooldownOverlay()
    {
        if (_quantitySelector.CooldownOverlay != null)
        {
            _quantitySelector.CooldownOverlay.SetActive(true);
            UpdateCooldownTimerDisplay();
        }
    }

    private void HideCooldownOverlay()
    {
        if (_quantitySelector.CooldownOverlay != null)
            _quantitySelector.CooldownOverlay.SetActive(false);
    }

    private void UpdateCooldownTimerDisplay()
    {
        if (_quantitySelector.CooldownTimerText != null)
            _quantitySelector.CooldownTimerText.text = $"{_postTradeCooldownTimer:F1}s";
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
        int selectedStockId = GetSelectedStockId();
        if (selectedStockId < 0) return false;

        float currentPrice = GetStockPrice(selectedStockId);
        if (currentPrice <= 0f) return false;

        string stockIdStr = selectedStockId.ToString();
        string ticker = GetSelectedTicker();

        int qty = _quantitySelector.GetCurrentQuantity(true, false, stockIdStr, currentPrice, _ctx.Portfolio);
        if (qty <= 0)
        {
            EventBus.Publish(new TradeFeedbackEvent
            {
                Message = "Insufficient cash", IsSuccess = false, IsBuy = true, IsShort = false
            });
            return false;
        }
        bool success = _tradeExecutor.ExecuteBuy(stockIdStr, qty, currentPrice, _ctx.Portfolio);
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
        int selectedStockId = GetSelectedStockId();
        if (selectedStockId < 0) return false;

        float currentPrice = GetStockPrice(selectedStockId);
        if (currentPrice <= 0f) return false;

        string stockIdStr = selectedStockId.ToString();
        string ticker = GetSelectedTicker();

        int qty = _quantitySelector.GetCurrentQuantity(false, false, stockIdStr, currentPrice, _ctx.Portfolio);
        if (qty <= 0)
        {
            EventBus.Publish(new TradeFeedbackEvent
            {
                Message = "No position to sell", IsSuccess = false, IsBuy = false, IsShort = false
            });
            return false;
        }

        // Check entry price before sell to determine profitability
        var position = _ctx.Portfolio.GetPosition(stockIdStr);
        float entryPrice = position != null ? position.AverageBuyPrice : 0f;

        bool success = _tradeExecutor.ExecuteSell(stockIdStr, qty, currentPrice, _ctx.Portfolio);
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
