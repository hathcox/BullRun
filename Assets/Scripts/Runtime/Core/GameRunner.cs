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
    private StockSidebarData _sidebarData;
    private bool _firstFrameSkipped;

    private void Awake()
    {
        // Clear stale EventBus subscriptions from previous play sessions
        EventBus.Clear();

        _ctx = RunContext.StartNewRun();
        _priceGenerator = new PriceGenerator();
        _tradeExecutor = new TradeExecutor();
        _stateMachine = new GameStateMachine(_ctx);

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

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Find ChartRenderer from ChartDataHolder for debug wiring
        ChartRenderer chartRendererRef = null;
        var holder = Object.FindObjectOfType<ChartDataHolder>();
        if (holder != null) chartRendererRef = holder.Renderer;
        DebugSetup.Execute(_priceGenerator, chartRendererRef);
        #endif

        // Kick off the game loop — skip MetaHub placeholder
        MarketOpenState.NextConfig = new MarketOpenStateConfig
        {
            StateMachine = _stateMachine,
            PriceGenerator = _priceGenerator,
            TradeExecutor = _tradeExecutor
        };
        _stateMachine.TransitionTo<MarketOpenState>();

        // Populate sidebar with stocks (MarketOpenState.Enter already initialized them)
        _sidebarData.InitializeForRound(
            new List<StockInstance>(_priceGenerator.ActiveStocks));

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
    /// Basic keyboard trading during TradingState.
    /// B = Buy 10 shares of selected stock
    /// S = Sell 10 shares of selected stock
    /// </summary>
    private void HandleTradingInput()
    {
        if (!TradingState.IsActive) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        int selectedStockId = _sidebarData != null ? GetSelectedStockId() : -1;
        if (selectedStockId < 0) return;

        float currentPrice = GetStockPrice(selectedStockId);
        if (currentPrice <= 0f) return;

        string stockIdStr = selectedStockId.ToString();

        if (keyboard.bKey.wasPressedThisFrame)
        {
            bool success = _tradeExecutor.ExecuteBuy(stockIdStr, 10, currentPrice, _ctx.Portfolio);
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (success)
                Debug.Log($"[Trade] BUY 10 shares @ ${currentPrice:F2} (Stock {selectedStockId})");
            else
                Debug.Log($"[Trade] BUY rejected — insufficient cash");
            #endif
        }
        else if (keyboard.sKey.wasPressedThisFrame)
        {
            bool success = _tradeExecutor.ExecuteSell(stockIdStr, 10, currentPrice, _ctx.Portfolio);
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (success)
                Debug.Log($"[Trade] SELL 10 shares @ ${currentPrice:F2} (Stock {selectedStockId})");
            else
                Debug.Log($"[Trade] SELL rejected — no position to sell");
            #endif
        }
    }

    private int GetSelectedStockId()
    {
        if (_sidebarData == null || _sidebarData.SelectedIndex < 0) return -1;
        return _sidebarData.GetEntry(_sidebarData.SelectedIndex).StockId;
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
