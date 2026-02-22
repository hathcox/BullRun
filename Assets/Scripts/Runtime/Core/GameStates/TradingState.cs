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
    private EventScheduler _eventScheduler;

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
    /// Story 17.6: Extends the active round timer by the given seconds.
    /// Called by TimeBuyerRelic on buy trade. No-op if trading is not active.
    /// Static method consistent with other TradingState static accessors.
    /// </summary>
    private static TradingState _activeInstance;
    public static void ExtendTimer(float seconds)
    {
        if (!IsActive || _activeInstance == null) return;
        _activeInstance._timeRemaining += seconds;
        _activeInstance._roundDuration += seconds;
        ActiveTimeRemaining = _activeInstance._timeRemaining;
        ActiveRoundDuration = _activeInstance._roundDuration;
        EventBus.Publish(new RoundTimerExtendedEvent
        {
            NewDuration = _activeInstance._roundDuration
        });
    }

    /// <summary>
    /// Sets external dependencies. Must be called before transitioning to this state.
    /// </summary>
    public static TradingStateConfig NextConfig;

    public void Enter(RunContext ctx)
    {
        // Story 13.7: Extended Trading expansion adds 15s to round duration
        _roundDuration = GameConfig.RoundDurationSeconds
            + (ctx.OwnedExpansions.Contains(ExpansionDefinitions.ExtendedTrading) ? 15f : 0f);
        _timeRemaining = _roundDuration;

        Debug.Assert(NextConfig != null,
            "[TradingState] NextConfig is null! Set TradingState.NextConfig before calling TransitionTo<TradingState>().");

        if (NextConfig != null)
        {
            _stateMachine = NextConfig.StateMachine;
            _priceGenerator = NextConfig.PriceGenerator;
            _tradeExecutor = NextConfig.TradeExecutor;
            _eventScheduler = NextConfig.EventScheduler;
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
        _activeInstance = this;

        // Story 17.4 review fix: Reset relic multipliers to defaults BEFORE RoundStartedEvent
        // so relics can set their values during dispatch (Event Storm, Bull Believer).
        // InitializeRound then uses the relic-set values for event count calculation.
        if (_eventScheduler != null)
        {
            _eventScheduler.EventCountMultiplier = 1.0f;
            _eventScheduler.ImpactMultiplier = 1.0f;
            _eventScheduler.PositiveImpactMultiplier = 1.0f;
        }

        // Publish RoundStartedEvent — relics dispatch synchronously and set multipliers
        EventBus.Publish(new RoundStartedEvent
        {
            RoundNumber = ctx.CurrentRound,
            Act = ctx.CurrentAct,
            TierDisplayName = ctx.CurrentActConfig.DisplayName,
            MarginCallTarget = MarginCallTargets.GetTarget(ctx.CurrentRound),
            TimeLimit = _roundDuration
        });

        // Initialize event scheduler AFTER relic dispatch so EventCountMultiplier affects event count
        if (_eventScheduler != null && _priceGenerator != null)
        {
            _eventScheduler.EventEffects.SetActiveStocks(_priceGenerator.ActiveStocks);
            _eventScheduler.InitializeRound(
                ctx.CurrentRound,
                ctx.CurrentAct,
                ctx.CurrentTier,
                _priceGenerator.ActiveStocks,
                _roundDuration);
        }

        // Story 18.2: Activate insider tip overlays using actual round-start data
        if (ctx.RevealedTips != null && ctx.RevealedTips.Count > 0
            && _priceGenerator != null && _priceGenerator.ActiveStocks.Count > 0)
        {
            var activationCtx = new TipActivationContext
            {
                ActiveStock = _priceGenerator.ActiveStocks[0],
                ScheduledEventCount = _eventScheduler != null ? _eventScheduler.ScheduledEventCount : 0,
                ScheduledFireTimes = BuildFireTimesArray(_eventScheduler),
                RoundDuration = _roundDuration,
                TierConfig = StockTierData.GetTierConfig(ctx.CurrentTier),
                Random = new System.Random(ctx.CurrentRound * 31 + ctx.CurrentAct)
            };
            ctx.ActiveTipOverlays.Clear();
            ctx.ActiveTipOverlays.AddRange(TipActivator.ActivateTips(ctx.RevealedTips, activationCtx));

            EventBus.Publish(new TipOverlaysActivatedEvent { Overlays = ctx.ActiveTipOverlays });
        }

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
                    TradeExecutor = _tradeExecutor,
                    EventScheduler = _eventScheduler
                };
                _stateMachine.TransitionTo<MarketCloseState>();
            }
            return;
        }

        // Update static accessors for UI reads
        ActiveTimeRemaining = _timeRemaining;

        // Skip all price/event processing during initial price freeze
        bool frozen = TimeElapsed < GameConfig.PriceFreezeSeconds;

        // Update event scheduler BEFORE price updates so newly fired events affect prices this frame
        if (!frozen && _eventScheduler != null && _priceGenerator != null)
        {
            _eventScheduler.Update(
                TimeElapsed - GameConfig.PriceFreezeSeconds,
                deltaTime,
                _priceGenerator.ActiveStocks,
                ctx.CurrentTier);
        }

        // Drive PriceGenerator updates
        if (_priceGenerator != null)
        {
            for (int i = 0; i < _priceGenerator.ActiveStocks.Count; i++)
            {
                var stock = _priceGenerator.ActiveStocks[i];
                if (frozen)
                {
                    // Publish price event at current (unchanged) price so chart draws flat line
                    EventBus.Publish(new PriceUpdatedEvent
                    {
                        StockId = stock.StockId,
                        NewPrice = stock.CurrentPrice,
                        PreviousPrice = stock.CurrentPrice,
                        DeltaTime = deltaTime
                    });
                }
                else
                {
                    _priceGenerator.UpdatePrice(stock, deltaTime);
                }
            }
        }
    }

    /// <summary>
    /// Story 18.2: Builds array of scheduled fire times for TipActivationContext.
    /// </summary>
    private static float[] BuildFireTimesArray(EventScheduler scheduler)
    {
        if (scheduler == null || scheduler.ScheduledEventCount == 0)
            return System.Array.Empty<float>();
        var times = new float[scheduler.ScheduledEventCount];
        for (int i = 0; i < times.Length; i++)
            times[i] = scheduler.GetScheduledTime(i);
        return times;
    }

    public void Exit(RunContext ctx)
    {
        IsActive = false;
        _activeInstance = null;

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
    public EventScheduler EventScheduler;
}
