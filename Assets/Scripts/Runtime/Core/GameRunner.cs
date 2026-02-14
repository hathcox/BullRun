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
    // FIX-11: Short lifecycle state machine
    public enum ShortState { RoundLockout, Ready, Holding, CashOutWindow, Cooldown }

    private GameStateMachine _stateMachine;
    private RunContext _ctx;
    private PriceGenerator _priceGenerator;
    private TradeExecutor _tradeExecutor;
    private EventScheduler _eventScheduler;
    private QuantitySelector _quantitySelector;
    private PositionOverlay _positionOverlay;
    private bool _firstFrameSkipped;
    private bool _tradePanelVisible;

    // Post-trade cooldown (FIX-10 v2): instant trade, then lock out both buttons
    private float _postTradeCooldownTimer;
    private bool _isPostTradeCooldownActive;
    private Color _buyButtonOriginalColor;
    private Color _sellButtonOriginalColor;

    // FIX-11: Short state machine fields
    private ShortState _shortState = ShortState.RoundLockout;
    private float _shortTimer;
    private float _shortEntryPrice;
    private int _shortShares;
    private string _shortStockId;

    // FIX-11: Short UI references (set by UISetup.ExecuteShortButton)
    private Image _shortButtonImage;
    private Text _shortButtonText;
    private GameObject _shortPnlPanel;
    private Text _shortPnlEntryText;
    private Text _shortPnlValueText;
    private Text _shortPnlCountdownText;
    private Color _shortButtonOriginalColor;
    private bool _shortButtonVisible;
    private float _shortFlashTimer;

    private void Awake()
    {
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

        // Create all UI systems with runtime data (FIX-5: sidebar removed — single stock per round)
        UISetup.Execute(_ctx, _ctx.CurrentRound, GameConfig.RoundDurationSeconds);
        UISetup.ExecuteMarketOpenUI();
        // FIX-7: Compact position overlay replaces old right-side PositionPanel
        _positionOverlay = UISetup.ExecutePositionOverlay(_ctx.Portfolio);
        UISetup.ExecuteRoundTimer();

        // Create item inventory bottom bar (subscribes to RoundStartedEvent/TradingPhaseEndedEvent)
        UISetup.ExecuteItemInventoryPanel(_ctx);

        // Create trade feedback overlay
        UISetup.ExecuteTradeFeedback();

        // Create trade panel with BUY/SELL buttons and quantity presets (FIX-6)
        _quantitySelector = UISetup.ExecuteTradePanel();
        _quantitySelector.gameObject.SetActive(false); // Hidden until TradingState activates

        // Store original button colors for post-trade cooldown visual reset (FIX-10 v2)
        if (_quantitySelector.BuyButtonImage != null)
            _buyButtonOriginalColor = _quantitySelector.BuyButtonImage.color;
        if (_quantitySelector.SellButtonImage != null)
            _sellButtonOriginalColor = _quantitySelector.SellButtonImage.color;

        // FIX-11: Create SHORT button and Short P&L panel
        UISetup.ExecuteShortButton(out _shortButtonImage, out _shortButtonText,
            out _shortPnlPanel, out _shortPnlEntryText, out _shortPnlValueText, out _shortPnlCountdownText);
        if (_shortButtonImage != null)
            _shortButtonOriginalColor = _shortButtonImage.color;
        if (_shortPnlPanel != null)
            _shortPnlPanel.SetActive(false);

        // Subscribe to trade button clicks from UI
        EventBus.Subscribe<TradeButtonPressedEvent>(OnTradeButtonPressed);

        // FIX-10 v2: Cancel post-trade cooldown when trading phase ends
        // FIX-11: Also resets short state machine on trading phase end
        EventBus.Subscribe<TradingPhaseEndedEvent>(OnTradingPhaseEnded);

        // FIX-11: Start short round lockout when a round starts
        EventBus.Subscribe<RoundStartedEvent>(OnRoundStartedForShort);

        // FIX-7: Wire position overlay to track the active stock
        EventBus.Subscribe<MarketOpenEvent>(OnMarketOpenForOverlay);

        // Create event display systems (subscribe to MarketEventFiredEvent)
        UISetup.ExecuteNewsBanner();
        UISetup.ExecuteNewsTicker();
        UISetup.ExecuteScreenEffects();
        UISetup.ExecuteEventPopup();

        // Create overlay UIs that subscribe to state transition events
        UISetup.ExecuteRoundResultsUI();
        UISetup.ExecuteRunSummaryUI();
        UISetup.ExecuteTierTransitionUI();
        UISetup.ExecuteShopUI();

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

        // Show/hide trade panel and short button based on trading state
        if (TradingState.IsActive != _tradePanelVisible)
        {
            _tradePanelVisible = TradingState.IsActive;
            _quantitySelector.gameObject.SetActive(_tradePanelVisible);
            if (_shortButtonImage != null)
                _shortButtonImage.transform.parent.gameObject.SetActive(_tradePanelVisible);
        }

        // FIX-10 v2: Tick post-trade cooldown timer and update countdown display
        if (_isPostTradeCooldownActive)
        {
            _postTradeCooldownTimer -= Time.deltaTime;
            if (_postTradeCooldownTimer <= 0f)
            {
                _postTradeCooldownTimer = 0f;
                _isPostTradeCooldownActive = false;
                RestoreButtonVisuals();
                HideCooldownTimer();
            }
            else
            {
                UpdateCooldownTimerDisplay();
            }
        }

        // FIX-11: Update short state machine
        if (TradingState.IsActive)
            UpdateShortStateMachine();

        HandleTradingInput();
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<TradeButtonPressedEvent>(OnTradeButtonPressed);
        EventBus.Unsubscribe<TradingPhaseEndedEvent>(OnTradingPhaseEnded);
        EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStartedForShort);
        EventBus.Unsubscribe<MarketOpenEvent>(OnMarketOpenForOverlay);
    }

    /// <summary>
    /// FIX-7: Sets the position overlay's active stock when the market opens.
    /// </summary>
    private void OnMarketOpenForOverlay(MarketOpenEvent evt)
    {
        if (_positionOverlay != null && evt.StockIds != null && evt.StockIds.Length > 0)
        {
            _positionOverlay.SetActiveStock(evt.StockIds[0]);
        }
    }

    // ========================
    // FIX-11: Short State Machine
    // ========================

    /// <summary>
    /// FIX-11: Initializes the short state machine to RoundLockout when a round starts.
    /// </summary>
    private void OnRoundStartedForShort(RoundStartedEvent evt)
    {
        _shortState = ShortState.RoundLockout;
        _shortTimer = GameConfig.ShortRoundStartLockout;
        _shortEntryPrice = 0f;
        _shortShares = 0;
        _shortStockId = null;
        if (_shortPnlPanel != null)
            _shortPnlPanel.SetActive(false);
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
                // Update P&L display
                UpdateShortPnlDisplay();
                if (_shortTimer <= 0f)
                {
                    _shortState = ShortState.CashOutWindow;
                    _shortTimer = GameConfig.ShortCashOutWindow;
                }
                break;

            case ShortState.CashOutWindow:
                _shortTimer -= dt;
                // Update P&L display with countdown
                UpdateShortPnlDisplay();
                // Auto-close if timer expires
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
            if (_shortPnlPanel != null)
                _shortPnlPanel.SetActive(true);

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

        float currentPrice = GetStockPrice(GetSelectedStockId());
        if (currentPrice <= 0f) currentPrice = _shortEntryPrice; // fallback

        bool success = _tradeExecutor.ExecuteCover(_shortStockId, _shortShares, currentPrice, _ctx.Portfolio);
        float pnl = (_shortEntryPrice - currentPrice) * _shortShares;

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
        if (_shortPnlPanel != null)
            _shortPnlPanel.SetActive(false);
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
        if (_shortPnlPanel != null)
            _shortPnlPanel.SetActive(false);
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

    /// <summary>
    /// FIX-11: Updates the Short P&L panel display during active short.
    /// </summary>
    private void UpdateShortPnlDisplay()
    {
        if (_shortPnlPanel == null || _shortStockId == null) return;

        float currentPrice = GetStockPrice(GetSelectedStockId());
        float pnl = (_shortEntryPrice - currentPrice) * _shortShares;

        if (_shortPnlEntryText != null)
            _shortPnlEntryText.text = $"Entry: ${_shortEntryPrice:F2}";

        if (_shortPnlValueText != null)
        {
            bool profit = pnl >= 0f;
            _shortPnlValueText.text = profit ? $"P&L: +${pnl:F2}" : $"P&L: -${Mathf.Abs(pnl):F2}";
            _shortPnlValueText.color = profit ? PositionOverlay.ProfitGreen : PositionOverlay.LossRed;
        }

        if (_shortPnlCountdownText != null)
        {
            if (_shortState == ShortState.CashOutWindow)
                _shortPnlCountdownText.text = $"Auto-close: {_shortTimer:F1}s";
            else
                _shortPnlCountdownText.text = "";
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
    /// 1-4 = Select quantity preset (x5/x10/x15/x25) — only if tier is unlocked (FIX-13).
    /// B = Buy (long only), S = Sell (long only) — blocked during cooldown.
    /// D = Short (Ready state) or Cash Out (CashOutWindow state).
    /// FIX-10 v2: Trades execute instantly, then post-trade cooldown locks Buy/Sell buttons.
    /// FIX-11: D key for short actions, separate from Buy/Sell cooldown.
    /// FIX-13: Keyboard shortcuts gated to unlocked tiers only.
    /// </summary>
    private void HandleTradingInput()
    {
        if (!TradingState.IsActive) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // FIX-13: Quantity preset shortcuts — only for unlocked tiers
        // Key 1 = tier 1 (x5), Key 2 = tier 2 (x10), Key 3 = tier 3 (x15), Key 4 = tier 4 (x25)
        if (keyboard.digit1Key.wasPressedThisFrame && _quantitySelector.IsTierUnlocked(1))
            _quantitySelector.SelectPresetByTier(1);
        else if (keyboard.digit2Key.wasPressedThisFrame && _quantitySelector.IsTierUnlocked(2))
            _quantitySelector.SelectPresetByTier(2);
        else if (keyboard.digit3Key.wasPressedThisFrame && _quantitySelector.IsTierUnlocked(3))
            _quantitySelector.SelectPresetByTier(3);
        else if (keyboard.digit4Key.wasPressedThisFrame && _quantitySelector.IsTierUnlocked(4))
            _quantitySelector.SelectPresetByTier(4);

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
    /// </summary>
    public void HandleShortInput()
    {
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
            RestoreButtonVisuals();
            HideCooldownTimer();

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
        DimBothButtons();
        ShowCooldownTimer();

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[GameRunner] Post-trade cooldown started: {GameConfig.PostTradeCooldown}s");
        #endif
    }

    private void DimBothButtons()
    {
        if (_quantitySelector.BuyButtonImage != null)
        {
            Color dimmed = _buyButtonOriginalColor;
            dimmed.a = GameConfig.CooldownDimAlpha;
            _quantitySelector.BuyButtonImage.color = dimmed;
        }
        if (_quantitySelector.SellButtonImage != null)
        {
            Color dimmed = _sellButtonOriginalColor;
            dimmed.a = GameConfig.CooldownDimAlpha;
            _quantitySelector.SellButtonImage.color = dimmed;
        }
    }

    private void RestoreButtonVisuals()
    {
        if (_quantitySelector.BuyButtonImage != null)
            _quantitySelector.BuyButtonImage.color = _buyButtonOriginalColor;
        if (_quantitySelector.SellButtonImage != null)
            _quantitySelector.SellButtonImage.color = _sellButtonOriginalColor;
    }

    private void ShowCooldownTimer()
    {
        if (_quantitySelector.CooldownTimerText != null)
        {
            _quantitySelector.CooldownTimerText.gameObject.SetActive(true);
            UpdateCooldownTimerDisplay();
        }
    }

    private void HideCooldownTimer()
    {
        if (_quantitySelector.CooldownTimerText != null)
            _quantitySelector.CooldownTimerText.gameObject.SetActive(false);
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
        bool success = _tradeExecutor.ExecuteSell(stockIdStr, qty, currentPrice, _ctx.Portfolio);
        EventBus.Publish(new TradeFeedbackEvent
        {
            Message = success ? $"SOLD {ticker} x{qty}" : "No position to sell",
            IsSuccess = success, IsBuy = false, IsShort = false
        });
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(success
            ? $"[Trade] SELL {qty} shares @ ${currentPrice:F2} (Stock {selectedStockId})"
            : "[Trade] SELL rejected — no position to sell");
        #endif
        return success;
    }

    // ========================
    // Utility
    // ========================

    private int GetSelectedStockId()
    {
        if (_priceGenerator.ActiveStocks.Count == 0) return -1;
        return _priceGenerator.ActiveStocks[0].StockId;
    }

    private string GetSelectedTicker()
    {
        if (_priceGenerator.ActiveStocks.Count == 0) return "???";
        return _priceGenerator.ActiveStocks[0].TickerSymbol;
    }

    private float GetStockPrice(int stockId)
    {
        if (_priceGenerator.ActiveStocks.Count == 0) return 0f;
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (stockId != _priceGenerator.ActiveStocks[0].StockId)
            Debug.LogWarning($"[GameRunner] GetStockPrice called with stockId {stockId} but ActiveStocks[0] is {_priceGenerator.ActiveStocks[0].StockId} — FIX-5 assumes single stock");
        #endif
        return _priceGenerator.ActiveStocks[0].CurrentPrice;
    }
}
