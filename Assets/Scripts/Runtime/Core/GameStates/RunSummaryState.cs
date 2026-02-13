using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Run summary state. Calculates run statistics and publishes RunEndedEvent.
/// Displayed after margin call or after completing all rounds.
/// Transitions to MetaHubState on user input (any key).
/// </summary>
public class RunSummaryState : IGameState
{
    private GameStateMachine _stateMachine;
    private PriceGenerator _priceGenerator;
    private TradeExecutor _tradeExecutor;
    private bool _inputEnabled;

    /// <summary>
    /// Static accessors for UI to read run summary data (one-way dependency).
    /// </summary>
    public static bool IsActive { get; private set; }
    public static bool WasMarginCalled { get; private set; }
    public static int RoundsCompleted { get; private set; }
    public static float FinalCash { get; private set; }
    public static float TotalProfit { get; private set; }
    public static int ReputationEarned { get; private set; }
    public static float RoundProfit { get; private set; }
    public static float RequiredTarget { get; private set; }
    public static int ItemsCollected { get; private set; }
    public static bool IsVictory { get; private set; }

    public static RunSummaryStateConfig NextConfig;

    /// <summary>
    /// Minimum time before input is accepted, to prevent accidental skip.
    /// </summary>
    public const float InputDelaySeconds = 0.5f;

    private float _inputDelayRemaining;

    public void Enter(RunContext ctx)
    {
        bool wasMarginCalled = false;
        float roundProfit = 0f;
        float requiredTarget = 0f;

        if (NextConfig != null)
        {
            wasMarginCalled = NextConfig.WasMarginCalled;
            roundProfit = NextConfig.RoundProfit;
            requiredTarget = NextConfig.RequiredTarget;
            _stateMachine = NextConfig.StateMachine;
            _priceGenerator = NextConfig.PriceGenerator;
            _tradeExecutor = NextConfig.TradeExecutor;
            NextConfig = null;
        }

        float finalCash = ctx.Portfolio.Cash;
        float totalProfit = finalCash - ctx.StartingCapital;
        int roundsCompleted = ctx.CurrentRound;
        int reputationEarned = 0; // Placeholder until Epic 9
        int itemsCollected = ctx.ActiveItems.Count;

        // Set static accessors for UI
        IsActive = true;
        WasMarginCalled = wasMarginCalled;
        IsVictory = !wasMarginCalled && ctx.IsRunComplete();
        RoundsCompleted = roundsCompleted;
        FinalCash = finalCash;
        TotalProfit = totalProfit;
        ReputationEarned = reputationEarned;
        RoundProfit = roundProfit;
        RequiredTarget = requiredTarget;
        ItemsCollected = itemsCollected;

        _inputEnabled = false;
        _inputDelayRemaining = InputDelaySeconds;

        EventBus.Publish(new RunEndedEvent
        {
            RoundsCompleted = roundsCompleted,
            FinalCash = finalCash,
            TotalProfit = totalProfit,
            WasMarginCalled = wasMarginCalled,
            ReputationEarned = reputationEarned,
            ItemsCollected = itemsCollected
        });

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        string header = wasMarginCalled ? "MARGIN CALL" : "RUN COMPLETE";
        Debug.Log($"[RunSummaryState] Enter: {header} — Rounds: {roundsCompleted}, Cash: ${finalCash:F2}, Profit: ${totalProfit:F2}");
        #endif
    }

    public void Update(RunContext ctx)
    {
        AdvanceTime(ctx, Time.deltaTime);
    }

    /// <summary>
    /// Core input delay and transition logic. Separated from Update for testability.
    /// </summary>
    public void AdvanceTime(RunContext ctx, float deltaTime)
    {
        if (!_inputEnabled)
        {
            _inputDelayRemaining -= deltaTime;
            if (_inputDelayRemaining <= 0f)
            {
                _inputEnabled = true;
            }
            return;
        }

        // Check for any key press to transition to MetaHubState
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
        {
            if (_stateMachine != null)
            {
                MetaHubState.NextConfig = new MetaHubStateConfig
                {
                    StateMachine = _stateMachine,
                    PriceGenerator = _priceGenerator,
                    TradeExecutor = _tradeExecutor
                };
                _stateMachine.TransitionTo<MetaHubState>();
            }
        }
    }

    /// <summary>
    /// Whether input is currently accepted (after delay).
    /// </summary>
    public bool IsInputEnabled => _inputEnabled;

    public void Exit(RunContext ctx)
    {
        IsActive = false;

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[RunSummaryState] Exit");
        #endif
    }
}

/// <summary>
/// Configuration passed to RunSummaryState before transition.
/// </summary>
public class RunSummaryStateConfig
{
    public bool WasMarginCalled;
    public float RoundProfit;
    public float RequiredTarget;
    public GameStateMachine StateMachine;
    public PriceGenerator PriceGenerator;
    public TradeExecutor TradeExecutor;
}
