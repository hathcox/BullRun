using UnityEngine;

/// <summary>
/// Margin call check phase. Compares round profit against the escalating
/// margin call target. If target met, proceeds to ShopState. If not,
/// publishes MarginCallTriggeredEvent and transitions to RunSummaryState.
/// </summary>
public class MarginCallState : IGameState
{
    private GameStateMachine _stateMachine;
    private PriceGenerator _priceGenerator;
    private TradeExecutor _tradeExecutor;

    public static MarginCallStateConfig NextConfig;

    public void Enter(RunContext ctx)
    {
        Debug.Assert(NextConfig != null,
            "[MarginCallState] NextConfig is null! Set MarginCallState.NextConfig before calling TransitionTo<MarginCallState>().");

        if (NextConfig != null)
        {
            _stateMachine = NextConfig.StateMachine;
            _priceGenerator = NextConfig.PriceGenerator;
            _tradeExecutor = NextConfig.TradeExecutor;
            NextConfig = null;
        }

        float roundProfit = MarketCloseState.RoundProfit;
        float target = MarginCallTargets.GetTarget(ctx.CurrentRound);

        bool targetMet = roundProfit >= target;

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (DebugManager.IsGodMode && !targetMet)
        {
            Debug.Log($"[MarginCallState] GOD MODE — bypassing margin call (Round {ctx.CurrentRound}, actual profit: ${roundProfit:F2} vs target: ${target:F2})");
            targetMet = true;
        }
        #endif

        if (targetMet)
        {
            // Victory detection: if this is the final round and margin call passes, the run is won
            if (ctx.CurrentRound >= GameConfig.TotalRounds)
            {
                ctx.RunCompleted = true;
            }

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[MarginCallState] Round {ctx.CurrentRound} PASSED: ${roundProfit:F2} >= ${target:F2}");
            #endif

            EventBus.Publish(new RoundCompletedEvent
            {
                RoundNumber = ctx.CurrentRound,
                RoundProfit = roundProfit,
                ProfitTarget = target,
                TargetMet = true,
                TotalCash = ctx.Portfolio.Cash
            });

            // Proceed to shop (which auto-skips to next MarketOpenState for now)
            if (_stateMachine != null)
            {
                ShopState.NextConfig = new ShopStateConfig
                {
                    StateMachine = _stateMachine,
                    PriceGenerator = _priceGenerator,
                    TradeExecutor = _tradeExecutor
                };
                _stateMachine.TransitionTo<ShopState>();
            }
        }
        else
        {
            float shortfall = target - roundProfit;

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[MarginCallState] MARGIN CALL! Round {ctx.CurrentRound}: ${roundProfit:F2} < ${target:F2} (shortfall: ${shortfall:F2})");
            #endif

            EventBus.Publish(new MarginCallTriggeredEvent
            {
                RoundNumber = ctx.CurrentRound,
                RoundProfit = roundProfit,
                RequiredTarget = target,
                Shortfall = shortfall
            });

            // Transition to RunSummary — run is over
            if (_stateMachine != null)
            {
                RunSummaryState.NextConfig = new RunSummaryStateConfig
                {
                    WasMarginCalled = true,
                    RoundProfit = roundProfit,
                    RequiredTarget = target,
                    StateMachine = _stateMachine,
                    PriceGenerator = _priceGenerator,
                    TradeExecutor = _tradeExecutor
                };
                _stateMachine.TransitionTo<RunSummaryState>();
            }
        }
    }

    public void Update(RunContext ctx) { }

    public void Exit(RunContext ctx)
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[MarginCallState] Exit");
        #endif
    }
}

/// <summary>
/// Configuration passed to MarginCallState before transition.
/// </summary>
public class MarginCallStateConfig
{
    public GameStateMachine StateMachine;
    public PriceGenerator PriceGenerator;
    public TradeExecutor TradeExecutor;
}
