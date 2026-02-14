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
    private GameStateMachine _stateMachine;
    private RunContext _ctx;
    private PriceGenerator _priceGenerator;
    private TradeExecutor _tradeExecutor;
    private EventScheduler _eventScheduler;
    private QuantitySelector _quantitySelector;
    private PositionOverlay _positionOverlay;
    private bool _firstFrameSkipped;
    private bool _tradePanelVisible;

    // Trade execution delay (FIX-10)
    private float _tradeCooldownTimer;
    private bool _pendingTradeIsBuy;
    private bool _isTradeCooldownActive;
    private Color _buyButtonOriginalColor;
    private Color _sellButtonOriginalColor;

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

        // Set round start baseline so round profit starts at $0 (not $1,000)
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

        // Store original button colors for cooldown visual reset (FIX-10)
        if (_quantitySelector.BuyButtonImage != null)
            _buyButtonOriginalColor = _quantitySelector.BuyButtonImage.color;
        if (_quantitySelector.SellButtonImage != null)
            _sellButtonOriginalColor = _quantitySelector.SellButtonImage.color;

        // Subscribe to trade button clicks from UI
        EventBus.Subscribe<TradeButtonPressedEvent>(OnTradeButtonPressed);

        // FIX-10: Cancel cooldown when trading phase ends
        EventBus.Subscribe<TradingPhaseEndedEvent>(OnTradingPhaseEnded);

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

        // Show/hide trade panel based on trading state
        if (TradingState.IsActive != _tradePanelVisible)
        {
            _tradePanelVisible = TradingState.IsActive;
            _quantitySelector.gameObject.SetActive(_tradePanelVisible);
        }

        // FIX-10: Tick trade cooldown timer
        if (_isTradeCooldownActive)
        {
            _tradeCooldownTimer -= Time.deltaTime;
            if (_tradeCooldownTimer <= 0f)
            {
                _tradeCooldownTimer = 0f;
                _isTradeCooldownActive = false;

                // Cancel if trading phase ended during cooldown
                if (TradingState.IsActive)
                {
                    ExecutePendingTrade();
                }
                else
                {
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log("[GameRunner] Trade cooldown expired but TradingState no longer active — cancelling pending trade");
                    #endif
                }

                // Restore button visuals
                RestoreButtonVisuals();
            }
        }

        HandleTradingInput();
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<TradeButtonPressedEvent>(OnTradeButtonPressed);
        EventBus.Unsubscribe<TradingPhaseEndedEvent>(OnTradingPhaseEnded);
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

    /// <summary>
    /// Keyboard trading during TradingState.
    /// 1-4 = Select quantity preset (x5/x10/x15/x25).
    /// B = Smart Buy (buy or cover), S = Smart Sell (sell or short).
    /// FIX-10: B/S now start a cooldown instead of executing immediately.
    /// </summary>
    private void HandleTradingInput()
    {
        if (!TradingState.IsActive) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Quantity preset shortcuts: 1=x5, 2=x10, 3=x15, 4=x25
        if (keyboard.digit1Key.wasPressedThisFrame)
            _quantitySelector.SelectPreset(QuantitySelector.Preset.Five);
        else if (keyboard.digit2Key.wasPressedThisFrame)
            _quantitySelector.SelectPreset(QuantitySelector.Preset.Ten);
        else if (keyboard.digit3Key.wasPressedThisFrame)
            _quantitySelector.SelectPreset(QuantitySelector.Preset.Fifteen);
        else if (keyboard.digit4Key.wasPressedThisFrame)
            _quantitySelector.SelectPreset(QuantitySelector.Preset.TwentyFive);
        // Trade actions: B=Smart Buy, S=Smart Sell (FIX-10: starts cooldown)
        else if (keyboard.bKey.wasPressedThisFrame)
            StartTradeCooldown(true);
        else if (keyboard.sKey.wasPressedThisFrame)
            StartTradeCooldown(false);
    }

    /// <summary>
    /// Handles TradeButtonPressedEvent from BUY/SELL UI buttons.
    /// FIX-10: Starts cooldown instead of executing immediately.
    /// </summary>
    private void OnTradeButtonPressed(TradeButtonPressedEvent evt)
    {
        if (!TradingState.IsActive) return;
        StartTradeCooldown(evt.IsBuy);
    }

    /// <summary>
    /// FIX-10: Cancels pending trade and restores button visuals when trading phase ends.
    /// </summary>
    private void OnTradingPhaseEnded(TradingPhaseEndedEvent evt)
    {
        if (_isTradeCooldownActive)
        {
            _isTradeCooldownActive = false;
            _tradeCooldownTimer = 0f;
            RestoreButtonVisuals();

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[GameRunner] Trading phase ended — cancelled pending trade cooldown");
            #endif
        }
    }

    /// <summary>
    /// FIX-10: Starts the trade execution cooldown. Ignores input if cooldown already active.
    /// </summary>
    private void StartTradeCooldown(bool isBuy)
    {
        if (_isTradeCooldownActive) return; // No queuing — ignore additional presses

        _isTradeCooldownActive = true;
        _pendingTradeIsBuy = isBuy;
        _tradeCooldownTimer = GameConfig.TradeExecutionDelay;

        // Dim the active button during cooldown
        DimButton(isBuy);

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[GameRunner] Trade cooldown started: {(isBuy ? "BUY" : "SELL")}, delay={GameConfig.TradeExecutionDelay}s");
        #endif
    }

    /// <summary>
    /// FIX-10: Dims the BUY or SELL button Image to indicate processing.
    /// </summary>
    private void DimButton(bool isBuy)
    {
        Image buttonImage = isBuy ? _quantitySelector.BuyButtonImage : _quantitySelector.SellButtonImage;
        if (buttonImage != null)
        {
            Color dimmed = buttonImage.color;
            dimmed.a = GameConfig.CooldownDimAlpha;
            buttonImage.color = dimmed;
        }
    }

    /// <summary>
    /// FIX-10: Restores both BUY and SELL buttons to their original colors.
    /// </summary>
    private void RestoreButtonVisuals()
    {
        if (_quantitySelector.BuyButtonImage != null)
            _quantitySelector.BuyButtonImage.color = _buyButtonOriginalColor;
        if (_quantitySelector.SellButtonImage != null)
            _quantitySelector.SellButtonImage.color = _sellButtonOriginalColor;
    }

    /// <summary>
    /// FIX-10: Executes the pending trade at the CURRENT price (not the price at press time).
    /// This creates natural slippage — price may have moved during the cooldown.
    /// </summary>
    private void ExecutePendingTrade()
    {
        if (_pendingTradeIsBuy)
            ExecuteSmartBuy();
        else
            ExecuteSmartSell();
    }

    /// <summary>
    /// Smart buy: if player has a SHORT position → cover it. Otherwise → buy (open/add long).
    /// </summary>
    private void ExecuteSmartBuy()
    {
        int selectedStockId = GetSelectedStockId();
        if (selectedStockId < 0) return;

        float currentPrice = GetStockPrice(selectedStockId);
        if (currentPrice <= 0f) return;

        string stockIdStr = selectedStockId.ToString();
        string ticker = GetSelectedTicker();
        var position = _ctx.Portfolio.GetPosition(stockIdStr);

        if (position != null && position.IsShort)
        {
            // COVER the short position
            int qty = _quantitySelector.GetCurrentQuantity(true, true, stockIdStr, currentPrice, _ctx.Portfolio);
            if (qty <= 0)
            {
                EventBus.Publish(new TradeFeedbackEvent
                {
                    Message = TradeFeedback.GetCoverRejectionReason(_ctx.Portfolio, stockIdStr),
                    IsSuccess = false, IsBuy = true, IsShort = true
                });
                return;
            }
            bool success = _tradeExecutor.ExecuteCover(stockIdStr, qty, currentPrice, _ctx.Portfolio);
            EventBus.Publish(new TradeFeedbackEvent
            {
                Message = success ? $"COVERED {ticker} x{qty}"
                    : TradeFeedback.GetCoverRejectionReason(_ctx.Portfolio, stockIdStr),
                IsSuccess = success, IsBuy = true, IsShort = true
            });
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log(success
                ? $"[Trade] COVER {qty} shares @ ${currentPrice:F2} (Stock {selectedStockId})"
                : $"[Trade] COVER rejected (Stock {selectedStockId})");
            #endif
        }
        else
        {
            // BUY (open/add long position)
            int qty = _quantitySelector.GetCurrentQuantity(true, false, stockIdStr, currentPrice, _ctx.Portfolio);
            if (qty <= 0)
            {
                EventBus.Publish(new TradeFeedbackEvent
                {
                    Message = "Insufficient cash", IsSuccess = false, IsBuy = true, IsShort = false
                });
                return;
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
        }
    }

    /// <summary>
    /// Smart sell: if player has a LONG position → sell it. Otherwise → short (open short).
    /// </summary>
    private void ExecuteSmartSell()
    {
        int selectedStockId = GetSelectedStockId();
        if (selectedStockId < 0) return;

        float currentPrice = GetStockPrice(selectedStockId);
        if (currentPrice <= 0f) return;

        string stockIdStr = selectedStockId.ToString();
        string ticker = GetSelectedTicker();
        var position = _ctx.Portfolio.GetPosition(stockIdStr);

        if (position != null && !position.IsShort && position.Shares > 0)
        {
            // SELL the long position
            int qty = _quantitySelector.GetCurrentQuantity(false, false, stockIdStr, currentPrice, _ctx.Portfolio);
            if (qty <= 0)
            {
                EventBus.Publish(new TradeFeedbackEvent
                {
                    Message = "No position to sell", IsSuccess = false, IsBuy = false, IsShort = false
                });
                return;
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
        }
        else
        {
            // SHORT (open short position)
            int qty = _quantitySelector.GetCurrentQuantity(false, true, stockIdStr, currentPrice, _ctx.Portfolio);
            if (qty <= 0)
            {
                EventBus.Publish(new TradeFeedbackEvent
                {
                    Message = TradeFeedback.GetShortRejectionReason(_ctx.Portfolio, stockIdStr),
                    IsSuccess = false, IsBuy = false, IsShort = true
                });
                return;
            }
            bool success = _tradeExecutor.ExecuteShort(stockIdStr, qty, currentPrice, _ctx.Portfolio);
            EventBus.Publish(new TradeFeedbackEvent
            {
                Message = success ? $"SHORTED {ticker} x{qty}"
                    : TradeFeedback.GetShortRejectionReason(_ctx.Portfolio, stockIdStr),
                IsSuccess = success, IsBuy = false, IsShort = true
            });
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log(success
                ? $"[Trade] SHORT {qty} shares @ ${currentPrice:F2} (Stock {selectedStockId})"
                : $"[Trade] SHORT rejected (Stock {selectedStockId})");
            #endif
        }
    }

    /// <summary>
    /// FIX-5: Returns the single active stock's ID directly from PriceGenerator.
    /// </summary>
    private int GetSelectedStockId()
    {
        if (_priceGenerator.ActiveStocks.Count == 0) return -1;
        return _priceGenerator.ActiveStocks[0].StockId;
    }

    /// <summary>
    /// FIX-5: Returns the single active stock's ticker directly from PriceGenerator.
    /// </summary>
    private string GetSelectedTicker()
    {
        if (_priceGenerator.ActiveStocks.Count == 0) return "???";
        return _priceGenerator.ActiveStocks[0].TickerSymbol;
    }

    /// <summary>
    /// FIX-5: Returns the single active stock's current price directly from PriceGenerator.
    /// Parameter stockId kept for Func&lt;int, float&gt; delegate compatibility (QuantitySelector).
    /// </summary>
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
