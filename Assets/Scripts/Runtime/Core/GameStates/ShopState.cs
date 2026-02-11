using UnityEngine;

/// <summary>
/// Placeholder Shop state. Auto-skips to next MarketOpenState for now.
/// Will be fleshed out in Epic 7 (Draft Shop).
/// After shop, advances round and checks for run completion or act transition.
/// </summary>
public class ShopState : IGameState
{
    private GameStateMachine _stateMachine;
    private PriceGenerator _priceGenerator;
    private TradeExecutor _tradeExecutor;

    public static ShopStateConfig NextConfig;

    public void Enter(RunContext ctx)
    {
        if (NextConfig != null)
        {
            _stateMachine = NextConfig.StateMachine;
            _priceGenerator = NextConfig.PriceGenerator;
            _tradeExecutor = NextConfig.TradeExecutor;
            NextConfig = null;
        }

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[ShopState] Enter: Shop entered (placeholder — auto-skipping)");
        #endif

        // Advance to next round (replaces PrepareForNextRound with act-aware logic)
        Debug.Assert(ctx.Portfolio.PositionCount == 0,
            "[ShopState] AdvanceRound called with open positions — liquidate first!");
        bool actChanged = ctx.AdvanceRound();
        ctx.Portfolio.StartRound(ctx.Portfolio.Cash);

        if (_stateMachine == null) return;

        // Check if run is complete after advancing
        if (ctx.IsRunComplete())
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[ShopState] Run complete — transitioning to RunSummary (win)");
            #endif

            RunSummaryState.NextConfig = new RunSummaryStateConfig
            {
                WasMarginCalled = false,
                RoundProfit = 0f,
                RequiredTarget = 0f,
                StateMachine = _stateMachine
            };
            _stateMachine.TransitionTo<RunSummaryState>();
            return;
        }

        // Publish act transition event if act changed (only for ongoing runs)
        if (actChanged)
        {
            EventBus.Publish(new ActTransitionEvent
            {
                NewAct = ctx.CurrentAct,
                PreviousAct = ctx.CurrentAct - 1,
                TierDisplayName = GameConfig.Acts[ctx.CurrentAct].DisplayName
            });
        }

        // Continue to next MarketOpenState
        MarketOpenState.NextConfig = new MarketOpenStateConfig
        {
            StateMachine = _stateMachine,
            PriceGenerator = _priceGenerator,
            TradeExecutor = _tradeExecutor
        };
        _stateMachine.TransitionTo<MarketOpenState>();
    }

    public void Update(RunContext ctx) { }

    public void Exit(RunContext ctx)
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[ShopState] Exit");
        #endif
    }
}

/// <summary>
/// Configuration passed to ShopState before transition.
/// </summary>
public class ShopStateConfig
{
    public GameStateMachine StateMachine;
    public PriceGenerator PriceGenerator;
    public TradeExecutor TradeExecutor;
}
