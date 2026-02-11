using UnityEngine;

/// <summary>
/// Market preview phase. Initializes round stocks, displays preview UI,
/// then transitions to TradingState after countdown.
/// </summary>
public class MarketOpenState : IGameState
{
    private float _timeRemaining;
    private float _previewDuration;
    private GameStateMachine _stateMachine;
    private PriceGenerator _priceGenerator;

    public float TimeRemaining => _timeRemaining;
    public float PreviewDuration => _previewDuration;

    /// <summary>
    /// Static accessors for UI to read preview timer (one-way dependency).
    /// </summary>
    public static float ActiveTimeRemaining { get; private set; }
    public static bool IsActive { get; private set; }

    /// <summary>
    /// Configuration must be set before transitioning to this state.
    /// </summary>
    public static MarketOpenStateConfig NextConfig;

    public void Enter(RunContext ctx)
    {
        _previewDuration = GameConfig.MarketOpenDurationSeconds;
        _timeRemaining = _previewDuration;

        Debug.Assert(NextConfig != null,
            "[MarketOpenState] NextConfig is null! Set MarketOpenState.NextConfig before calling TransitionTo<MarketOpenState>().");

        if (NextConfig != null)
        {
            _stateMachine = NextConfig.StateMachine;
            _priceGenerator = NextConfig.PriceGenerator;
            NextConfig = null;
        }

        // Initialize stocks for this round
        if (_priceGenerator != null)
        {
            _priceGenerator.InitializeRound(ctx.CurrentAct, ctx.CurrentRound);
        }

        ActiveTimeRemaining = _timeRemaining;
        IsActive = true;

        // Build stock detail arrays for the event
        int[] stockIds = null;
        string[] tickerSymbols = null;
        float[] startingPrices = null;
        string[] tierNames = null;
        string headline = "Markets await direction";

        if (_priceGenerator != null && _priceGenerator.ActiveStocks.Count > 0)
        {
            int count = _priceGenerator.ActiveStocks.Count;
            stockIds = new int[count];
            tickerSymbols = new string[count];
            startingPrices = new float[count];
            tierNames = new string[count];

            for (int i = 0; i < count; i++)
            {
                var stock = _priceGenerator.ActiveStocks[i];
                stockIds[i] = stock.StockId;
                tickerSymbols[i] = stock.TickerSymbol;
                startingPrices[i] = stock.CurrentPrice;
                tierNames[i] = stock.Tier.ToString();
            }

            headline = NewsHeadlineData.GetHeadline(_priceGenerator.ActiveStocks, new System.Random());
        }

        EventBus.Publish(new MarketOpenEvent
        {
            RoundNumber = ctx.CurrentRound,
            Act = ctx.CurrentAct,
            StockIds = stockIds,
            TickerSymbols = tickerSymbols,
            StartingPrices = startingPrices,
            TierNames = tierNames,
            ProfitTarget = MarginCallTargets.GetTarget(ctx.CurrentRound),
            Headline = headline
        });

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[MarketOpenState] Enter: Act {ctx.CurrentAct}, Round {ctx.CurrentRound}, Preview {_previewDuration}s");
        #endif
    }

    public void Update(RunContext ctx)
    {
        AdvanceTime(ctx, Time.deltaTime);
    }

    /// <summary>
    /// Core preview timer logic. Separated from Update for testability.
    /// </summary>
    public void AdvanceTime(RunContext ctx, float deltaTime)
    {
        _timeRemaining -= deltaTime;

        if (_timeRemaining <= 0f)
        {
            _timeRemaining = 0f;
            ActiveTimeRemaining = 0f;
            IsActive = false;

            // Transition to TradingState
            if (_stateMachine != null)
            {
                TradingState.NextConfig = new TradingStateConfig
                {
                    StateMachine = _stateMachine,
                    PriceGenerator = _priceGenerator
                };
                _stateMachine.TransitionTo<TradingState>();
            }
            return;
        }

        ActiveTimeRemaining = _timeRemaining;
    }

    public void Exit(RunContext ctx)
    {
        IsActive = false;

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[MarketOpenState] Exit: transitioning to Trading");
        #endif
    }
}

/// <summary>
/// Configuration passed to MarketOpenState before transition.
/// </summary>
public class MarketOpenStateConfig
{
    public GameStateMachine StateMachine;
    public PriceGenerator PriceGenerator;
}
