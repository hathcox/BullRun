using UnityEngine;

/// <summary>
/// Core trading phase state. Manages the round timer and drives PriceGenerator updates.
/// On Enter: sets timer, publishes RoundStartedEvent.
/// On Update: decrements timer, updates prices.
/// When timer expires: transitions to MarketCloseState.
/// </summary>
public class TradingState : IGameState
{
    private float _timeRemaining;
    private float _roundDuration;
    private GameStateMachine _stateMachine;
    private PriceGenerator _priceGenerator;
    private TradeExecutor _tradeExecutor;

    public float TimeRemaining => _timeRemaining;
    public float TimeElapsed => _roundDuration - _timeRemaining;
    public float RoundDuration => _roundDuration;

    /// <summary>
    /// Static accessors for UI to read authoritative timer values (one-way dependency).
    /// </summary>
    public static float ActiveTimeRemaining { get; private set; }
    public static float ActiveRoundDuration { get; private set; }
    public static bool IsActive { get; private set; }

    /// <summary>
    /// Sets external dependencies. Must be called before transitioning to this state.
    /// </summary>
    public static TradingStateConfig NextConfig;

    public void Enter(RunContext ctx)
    {
        _roundDuration = GameConfig.RoundDurationSeconds;
        _timeRemaining = _roundDuration;

        Debug.Assert(NextConfig != null,
            "[TradingState] NextConfig is null! Set TradingState.NextConfig before calling TransitionTo<TradingState>().");

        if (NextConfig != null)
        {
            _stateMachine = NextConfig.StateMachine;
            _priceGenerator = NextConfig.PriceGenerator;
            _tradeExecutor = NextConfig.TradeExecutor;
            NextConfig = null;
        }

        // Enable trading at the start of the trading phase
        if (_tradeExecutor != null)
        {
            _tradeExecutor.IsTradeEnabled = true;
        }

        ActiveTimeRemaining = _timeRemaining;
        ActiveRoundDuration = _roundDuration;
        IsActive = true;

        EventBus.Publish(new RoundStartedEvent
        {
            RoundNumber = ctx.CurrentRound,
            Act = ctx.CurrentAct,
            TierDisplayName = ctx.CurrentActConfig.DisplayName,
            MarginCallTarget = MarginCallTargets.GetTarget(ctx.CurrentRound),
            TimeLimit = _roundDuration
        });

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[TradingState] Enter: Round {ctx.CurrentRound}, Duration {_roundDuration}s");
        #endif
    }

    public void Update(RunContext ctx)
    {
        AdvanceTime(ctx, Time.deltaTime);
    }

    /// <summary>
    /// Core timer and price update logic. Separated from Update for testability.
    /// </summary>
    public void AdvanceTime(RunContext ctx, float deltaTime)
    {
        _timeRemaining -= deltaTime;

        // Check for timer expiry BEFORE price updates to avoid processing past deadline
        if (_timeRemaining <= 0f)
        {
            _timeRemaining = 0f;
            ActiveTimeRemaining = 0f;
            IsActive = false;

            EventBus.Publish(new TradingPhaseEndedEvent
            {
                RoundNumber = ctx.CurrentRound,
                TimeExpired = true
            });

            if (_stateMachine != null)
            {
                MarketCloseState.NextConfig = new MarketCloseStateConfig
                {
                    StateMachine = _stateMachine,
                    PriceGenerator = _priceGenerator,
                    TradeExecutor = _tradeExecutor
                };
                _stateMachine.TransitionTo<MarketCloseState>();
            }
            return;
        }

        // Update static accessors for UI reads
        ActiveTimeRemaining = _timeRemaining;

        // Drive PriceGenerator updates
        if (_priceGenerator != null)
        {
            for (int i = 0; i < _priceGenerator.ActiveStocks.Count; i++)
            {
                _priceGenerator.UpdatePrice(_priceGenerator.ActiveStocks[i], deltaTime);
            }
        }
    }

    public void Exit(RunContext ctx)
    {
        IsActive = false;

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[TradingState] Exit: TimeRemaining={_timeRemaining:F2}s");
        #endif
    }
}

/// <summary>
/// Configuration passed to TradingState before transition via static NextConfig field.
/// </summary>
public class TradingStateConfig
{
    public GameStateMachine StateMachine;
    public PriceGenerator PriceGenerator;
    public TradeExecutor TradeExecutor;
}
