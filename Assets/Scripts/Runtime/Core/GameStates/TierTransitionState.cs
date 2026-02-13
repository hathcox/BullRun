using UnityEngine;

/// <summary>
/// Intermediate state shown between ShopState and MarketOpenState when an act transition occurs.
/// Waits for TierTransitionUI animation to complete (3 seconds) before proceeding to MarketOpen.
/// Publishes ActTransitionEvent on Enter to trigger the UI overlay.
/// </summary>
public class TierTransitionState : IGameState
{
    private GameStateMachine _stateMachine;
    private PriceGenerator _priceGenerator;
    private TradeExecutor _tradeExecutor;
    private EventScheduler _eventScheduler;
    private int _previousAct;
    private float _elapsed;

    public static TierTransitionStateConfig NextConfig;

    public void Enter(RunContext ctx)
    {
        Debug.Assert(NextConfig != null,
            "[TierTransitionState] NextConfig is null! Set TierTransitionState.NextConfig before calling TransitionTo<TierTransitionState>().");

        if (NextConfig != null)
        {
            _stateMachine = NextConfig.StateMachine;
            _priceGenerator = NextConfig.PriceGenerator;
            _tradeExecutor = NextConfig.TradeExecutor;
            _eventScheduler = NextConfig.EventScheduler;
            _previousAct = NextConfig.PreviousAct;
            NextConfig = null;
        }

        _elapsed = 0f;

        // Publish ActTransitionEvent to trigger TierTransitionUI overlay
        EventBus.Publish(new ActTransitionEvent
        {
            NewAct = ctx.CurrentAct,
            PreviousAct = _previousAct,
            TierDisplayName = GameConfig.Acts[ctx.CurrentAct].DisplayName
        });

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[TierTransitionState] Enter: Act {ctx.CurrentAct} — {ctx.CurrentActConfig.DisplayName}");
        #endif
    }

    public void Update(RunContext ctx)
    {
        _elapsed += Time.deltaTime;

        // Wait for the transition animation to complete
        if (_elapsed >= TierTransitionUI.TotalDuration)
        {
            if (_stateMachine == null) return;

            MarketOpenState.NextConfig = new MarketOpenStateConfig
            {
                StateMachine = _stateMachine,
                PriceGenerator = _priceGenerator,
                TradeExecutor = _tradeExecutor,
                EventScheduler = _eventScheduler
            };
            _stateMachine.TransitionTo<MarketOpenState>();
        }
    }

    public void Exit(RunContext ctx)
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[TierTransitionState] Exit");
        #endif
    }
}

/// <summary>
/// Configuration passed to TierTransitionState before transition.
/// </summary>
public class TierTransitionStateConfig
{
    public GameStateMachine StateMachine;
    public PriceGenerator PriceGenerator;
    public TradeExecutor TradeExecutor;
    public EventScheduler EventScheduler;
    public int PreviousAct;
}
