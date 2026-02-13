using UnityEngine;

/// <summary>
/// Market close phase. Disables trading, liquidates all positions,
/// publishes MarketClosedEvent, pauses briefly, then transitions to next state.
/// </summary>
public class MarketCloseState : IGameState
{
    private float _pauseRemaining;
    private bool _liquidationComplete;
    private float _roundProfit;
    private GameStateMachine _stateMachine;
    private PriceGenerator _priceGenerator;
    private TradeExecutor _tradeExecutor;
    private EventScheduler _eventScheduler;

    /// <summary>
    /// Static accessors for UI to read market close state (one-way dependency).
    /// </summary>
    public static bool IsActive { get; private set; }
    public static float RoundProfit { get; internal set; }

    /// <summary>
    /// Configuration must be set before transitioning to this state.
    /// </summary>
    public static MarketCloseStateConfig NextConfig;

    /// <summary>
    /// Duration of the pause after liquidation for the player to absorb results.
    /// </summary>
    public const float PauseDuration = 2f;

    public void Enter(RunContext ctx)
    {
        Debug.Assert(NextConfig != null,
            "[MarketCloseState] NextConfig is null! Set MarketCloseState.NextConfig before calling TransitionTo<MarketCloseState>().");

        if (NextConfig != null)
        {
            _stateMachine = NextConfig.StateMachine;
            _priceGenerator = NextConfig.PriceGenerator;
            _tradeExecutor = NextConfig.TradeExecutor;
            _eventScheduler = NextConfig.EventScheduler;
            NextConfig = null;
        }

        IsActive = true;

        // Disable trading immediately before liquidation — no race conditions
        if (_tradeExecutor != null)
        {
            _tradeExecutor.IsTradeEnabled = false;
        }

        // Liquidate all positions at current prices
        int positionCount = ctx.Portfolio.PositionCount;
        _roundProfit = ctx.Portfolio.LiquidateAllPositions(stockId =>
        {
            if (_priceGenerator != null && int.TryParse(stockId, out int parsedId))
            {
                for (int i = 0; i < _priceGenerator.ActiveStocks.Count; i++)
                {
                    if (_priceGenerator.ActiveStocks[i].StockId == parsedId)
                        return _priceGenerator.ActiveStocks[i].CurrentPrice;
                }
            }
            return 0f;
        });

        // Total round profit = cash change from round start, not just liquidation P&L.
        // Manual trades realized during the round must count toward margin call targets.
        RoundProfit = ctx.Portfolio.GetRoundProfit();

        // Update run statistics with this round's results
        ctx.UpdateRunStats(RoundProfit);

        // Publish MarketClosedEvent
        EventBus.Publish(new MarketClosedEvent
        {
            RoundNumber = ctx.CurrentRound,
            RoundProfit = RoundProfit,
            FinalCash = ctx.Portfolio.Cash,
            PositionsLiquidated = positionCount
        });

        _pauseRemaining = PauseDuration;
        _liquidationComplete = true;

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[MarketCloseState] Enter: Round {ctx.CurrentRound}, Liquidated {positionCount} positions, Liquidation P&L: {(_roundProfit >= 0 ? "+" : "")}${_roundProfit:F2}, Round Profit: {(RoundProfit >= 0 ? "+" : "")}${RoundProfit:F2}, Cash: ${ctx.Portfolio.Cash:F2}");
        #endif
    }

    public void Update(RunContext ctx)
    {
        AdvanceTime(ctx, Time.deltaTime);
    }

    /// <summary>
    /// Core pause timer logic. Separated from Update for testability.
    /// </summary>
    public void AdvanceTime(RunContext ctx, float deltaTime)
    {
        if (!_liquidationComplete)
            return;

        _pauseRemaining -= deltaTime;

        if (_pauseRemaining <= 0f)
        {
            _pauseRemaining = 0f;
            IsActive = false;

            // Transition to margin call check (Story 4.4)
            if (_stateMachine != null)
            {
                MarginCallState.NextConfig = new MarginCallStateConfig
                {
                    StateMachine = _stateMachine,
                    PriceGenerator = _priceGenerator,
                    TradeExecutor = _tradeExecutor,
                    EventScheduler = _eventScheduler
                };
                _stateMachine.TransitionTo<MarginCallState>();
            }
        }
    }

    public void Exit(RunContext ctx)
    {
        IsActive = false;

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[MarketCloseState] Exit");
        #endif
    }
}

/// <summary>
/// Configuration passed to MarketCloseState before transition.
/// </summary>
public class MarketCloseStateConfig
{
    public GameStateMachine StateMachine;
    public PriceGenerator PriceGenerator;
    public TradeExecutor TradeExecutor;
    public EventScheduler EventScheduler;
}
