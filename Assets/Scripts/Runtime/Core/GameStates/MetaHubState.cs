using UnityEngine;

/// <summary>
/// Placeholder MetaHub state. Auto-restarts a new run for now.
/// Will be fleshed out in Epic 9 (Meta-Progression) with office scene,
/// reputation display, and run-start button.
/// </summary>
public class MetaHubState : IGameState
{
    private GameStateMachine _stateMachine;
    private PriceGenerator _priceGenerator;
    private TradeExecutor _tradeExecutor;

    public static MetaHubStateConfig NextConfig;

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
        Debug.Log("[MetaHubState] Enter: Auto-restarting run (placeholder)");
        #endif

        // Reset for a fresh run and immediately start
        ctx.ResetForNewRun();

        if (_stateMachine != null)
        {
            MarketOpenState.NextConfig = new MarketOpenStateConfig
            {
                StateMachine = _stateMachine,
                PriceGenerator = _priceGenerator,
                TradeExecutor = _tradeExecutor
            };
            _stateMachine.TransitionTo<MarketOpenState>();
        }
    }

    public void Update(RunContext ctx) { }

    public void Exit(RunContext ctx)
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[MetaHubState] Exit");
        #endif
    }
}

/// <summary>
/// Configuration passed to MetaHubState before transition.
/// </summary>
public class MetaHubStateConfig
{
    public GameStateMachine StateMachine;
    public PriceGenerator PriceGenerator;
    public TradeExecutor TradeExecutor;
}
