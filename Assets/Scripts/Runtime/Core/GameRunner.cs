using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
    private StockSidebarData _sidebarData;
    private QuantitySelector _quantitySelector;
    private bool _firstFrameSkipped;

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

        // Create all UI systems with runtime data
        UISetup.Execute(_ctx, _ctx.CurrentRound, GameConfig.RoundDurationSeconds);
        UISetup.ExecuteMarketOpenUI();
        var sidebar = UISetup.ExecuteSidebar();
        _sidebarData = sidebar.Data;
        UISetup.ExecutePositionsPanel(_ctx.Portfolio);
        UISetup.ExecuteRoundTimer();

        // Create item inventory bottom bar (subscribes to RoundStartedEvent/TradingPhaseEndedEvent)
        UISetup.ExecuteItemInventoryPanel(_ctx);

        // Create trade feedback overlay and key legend (FIX-2: short selling UI)
        UISetup.ExecuteTradeFeedback();
        UISetup.ExecuteKeyLegend();

        // Create quantity selector panel (FIX-3: trade quantity selection)
        _quantitySelector = UISetup.ExecuteQuantitySelector();
        _quantitySelector.SetDataSources(_ctx.Portfolio, GetSelectedStockId, GetStockPrice);

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

        // Re-populate sidebar whenever a new round starts (including run restarts)
        var priceGen = _priceGenerator;
        var sidebarRef = _sidebarData;
        EventBus.Subscribe<MarketOpenEvent>(_ =>
        {
            sidebarRef.InitializeForRound(
                new List<StockInstance>(priceGen.ActiveStocks));
        });

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
        // Sidebar is populated via MarketOpenEvent subscription above

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
        HandleTradingInput();
    }

    /// <summary>
    /// Keyboard trading during TradingState.
    /// Q = Cycle quantity preset, B = Buy, S = Sell, D = Short, F = Cover of selected stock.
    /// Quantity determined by QuantitySelector (1x, 5x, 10x, MAX).
    /// </summary>
    private void HandleTradingInput()
    {
        if (!TradingState.IsActive) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Q cycles quantity preset — works without stock selection
        if (keyboard.qKey.wasPressedThisFrame)
        {
            _quantitySelector.CyclePreset();
            return;
        }

        int selectedStockId = _sidebarData != null ? GetSelectedStockId() : -1;
        if (selectedStockId < 0) return;

        float currentPrice = GetStockPrice(selectedStockId);
        if (currentPrice <= 0f) return;

        // Only compute string conversions when a trade key is actually pressed (avoid per-frame allocation)
        bool anyTradeKey = keyboard.bKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame ||
            keyboard.dKey.wasPressedThisFrame || keyboard.fKey.wasPressedThisFrame;
        if (!anyTradeKey) return;

        string stockIdStr = selectedStockId.ToString();
        string ticker = GetSelectedTicker();

        if (keyboard.bKey.wasPressedThisFrame)
        {
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
            if (success)
                Debug.Log($"[Trade] BUY {qty} shares @ ${currentPrice:F2} (Stock {selectedStockId})");
            else
                Debug.Log($"[Trade] BUY rejected \u2014 insufficient cash");
            #endif
        }
        else if (keyboard.sKey.wasPressedThisFrame)
        {
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
            if (success)
                Debug.Log($"[Trade] SELL {qty} shares @ ${currentPrice:F2} (Stock {selectedStockId})");
            else
                Debug.Log($"[Trade] SELL rejected \u2014 no position to sell");
            #endif
        }
        else if (keyboard.dKey.wasPressedThisFrame)
        {
            int qty = _quantitySelector.GetCurrentQuantity(false, true, stockIdStr, currentPrice, _ctx.Portfolio);
            if (qty <= 0)
            {
                string reason = TradeFeedback.GetShortRejectionReason(_ctx.Portfolio, stockIdStr);
                EventBus.Publish(new TradeFeedbackEvent
                {
                    Message = reason, IsSuccess = false, IsBuy = false, IsShort = true
                });
                return;
            }
            bool success = _tradeExecutor.ExecuteShort(stockIdStr, qty, currentPrice, _ctx.Portfolio);
            string message = success
                ? $"SHORTED {ticker} x{qty}"
                : TradeFeedback.GetShortRejectionReason(_ctx.Portfolio, stockIdStr);
            EventBus.Publish(new TradeFeedbackEvent
            {
                Message = message, IsSuccess = success, IsBuy = false, IsShort = true
            });
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (success)
                Debug.Log($"[Trade] SHORT {qty} shares @ ${currentPrice:F2} (Stock {selectedStockId})");
            else
                Debug.Log($"[Trade] SHORT rejected (Stock {selectedStockId}): {message}");
            #endif
        }
        else if (keyboard.fKey.wasPressedThisFrame)
        {
            int qty = _quantitySelector.GetCurrentQuantity(true, true, stockIdStr, currentPrice, _ctx.Portfolio);
            if (qty <= 0)
            {
                string reason = TradeFeedback.GetCoverRejectionReason(_ctx.Portfolio, stockIdStr);
                EventBus.Publish(new TradeFeedbackEvent
                {
                    Message = reason, IsSuccess = false, IsBuy = true, IsShort = true
                });
                return;
            }
            bool success = _tradeExecutor.ExecuteCover(stockIdStr, qty, currentPrice, _ctx.Portfolio);
            string message = success
                ? $"COVERED {ticker} x{qty}"
                : TradeFeedback.GetCoverRejectionReason(_ctx.Portfolio, stockIdStr);
            EventBus.Publish(new TradeFeedbackEvent
            {
                Message = message, IsSuccess = success, IsBuy = true, IsShort = true
            });
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (success)
                Debug.Log($"[Trade] COVER {qty} shares @ ${currentPrice:F2} (Stock {selectedStockId})");
            else
                Debug.Log($"[Trade] COVER rejected (Stock {selectedStockId}): {message}");
            #endif
        }
    }

    private int GetSelectedStockId()
    {
        if (_sidebarData == null || _sidebarData.SelectedIndex < 0) return -1;
        return _sidebarData.GetEntry(_sidebarData.SelectedIndex).StockId;
    }

    private string GetSelectedTicker()
    {
        if (_sidebarData == null || _sidebarData.SelectedIndex < 0) return "???";
        return _sidebarData.GetEntry(_sidebarData.SelectedIndex).TickerSymbol;
    }

    private float GetStockPrice(int stockId)
    {
        for (int i = 0; i < _priceGenerator.ActiveStocks.Count; i++)
        {
            if (_priceGenerator.ActiveStocks[i].StockId == stockId)
                return _priceGenerator.ActiveStocks[i].CurrentPrice;
        }
        return 0f;
    }
}
