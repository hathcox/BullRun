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
    private EventScheduler _eventScheduler;

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
            _eventScheduler = NextConfig.EventScheduler;
            NextConfig = null;
        }

        float roundProfit = MarketCloseState.RoundProfit;
        float totalCash = ctx.Portfolio.Cash;
        float target = MarginCallTargets.GetTarget(ctx.CurrentRound);

        // FIX-14: Targets are cumulative portfolio value targets, not profit deltas.
        // Compare total cash against target value.
        bool targetMet = totalCash >= target;

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (DebugManager.IsGodMode && !targetMet)
        {
            Debug.Log($"[MarginCallState] GOD MODE — bypassing margin call (Round {ctx.CurrentRound}, cash: ${totalCash:F2} vs target: ${target:F2})");
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

            // FIX-14: Award Reputation for completing this round
            int repEarned = CalculateRoundReputation(ctx.CurrentRound, totalCash, target);

            // Compute base/bonus breakdown for UI display BEFORE doubling (AC 6)
            int roundIndex = Mathf.Clamp(ctx.CurrentRound - 1, 0, GameConfig.RepBaseAwardPerRound.Length - 1);
            int baseRep = GameConfig.RepBaseAwardPerRound[roundIndex];
            int bonusRep = repEarned - baseRep;

            // Story 17.5: Rep Doubler — double all trade-performance Rep (base + bonus)
            if (ctx.RelicManager.GetRelicById("relic_rep_doubler") != null)
            {
                baseRep *= 2;
                bonusRep *= 2;
                repEarned *= 2;
                EventBus.Publish(new RelicActivatedEvent { RelicId = "relic_rep_doubler" });
            }

            ctx.Reputation.Add(repEarned);
            ctx.ReputationEarned += repEarned;

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[MarginCallState] Round {ctx.CurrentRound} PASSED: cash ${totalCash:F2} >= target ${target:F2}, Rep earned: {repEarned} (base: {baseRep}, bonus: {bonusRep})");
            #endif

            EventBus.Publish(new RoundCompletedEvent
            {
                RoundNumber = ctx.CurrentRound,
                RoundProfit = roundProfit,
                ProfitTarget = target,
                TargetMet = true,
                TotalCash = totalCash,
                RepEarned = repEarned,
                BaseRep = baseRep,
                BonusRep = bonusRep
            });

            // Proceed to shop (which auto-skips to next MarketOpenState for now)
            if (_stateMachine != null)
            {
                ShopState.NextConfig = new ShopStateConfig
                {
                    StateMachine = _stateMachine,
                    PriceGenerator = _priceGenerator,
                    TradeExecutor = _tradeExecutor,
                    EventScheduler = _eventScheduler
                };
                _stateMachine.TransitionTo<ShopState>();
            }
        }
        else
        {
            float shortfall = target - totalCash;

            // FIX-14: Award consolation Reputation on margin call failure
            int roundsCompleted = ctx.CurrentRound - 1; // Failed this round, completed previous
            int consolationRep = roundsCompleted * GameConfig.RepConsolationPerRound;
            if (consolationRep > 0)
            {
                ctx.Reputation.Add(consolationRep);
                ctx.ReputationEarned += consolationRep;
            }

            // Story 17.5: Fail Forward — award base Rep for failed round despite margin call
            if (ctx.RelicManager.GetRelicById("relic_fail_forward") != null)
            {
                int failRoundIndex = Mathf.Clamp(ctx.CurrentRound - 1, 0, GameConfig.RepBaseAwardPerRound.Length - 1);
                int failForwardRep = GameConfig.RepBaseAwardPerRound[failRoundIndex];
                ctx.Reputation.Add(failForwardRep);
                ctx.ReputationEarned += failForwardRep;
                EventBus.Publish(new TradeFeedbackEvent
                {
                    Message = $"Fail Forward: +{failForwardRep} Rep",
                    IsSuccess = true, IsBuy = false, IsShort = false
                });
                EventBus.Publish(new RelicActivatedEvent { RelicId = "relic_fail_forward" });
            }

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[MarginCallState] MARGIN CALL! Round {ctx.CurrentRound}: cash ${totalCash:F2} < target ${target:F2} (shortfall: ${shortfall:F2}), consolation Rep: {consolationRep}");
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
                    TradeExecutor = _tradeExecutor,
                    EventScheduler = _eventScheduler
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

    /// <summary>
    /// FIX-14: Calculates Reputation earned for completing a round.
    /// Base award scales with round number + performance bonus for exceeding target.
    /// </summary>
    public static int CalculateRoundReputation(int roundNumber, float totalCash, float target)
    {
        int roundIndex = Mathf.Clamp(roundNumber - 1, 0, GameConfig.RepBaseAwardPerRound.Length - 1);
        int baseRep = GameConfig.RepBaseAwardPerRound[roundIndex];

        // Performance bonus: how much player exceeded target as a ratio
        float excessRatio = target > 0f ? Mathf.Max(0f, (totalCash - target) / target) : 0f;
        int bonusRep = Mathf.FloorToInt(baseRep * excessRatio * GameConfig.RepPerformanceBonusRate);

        return baseRep + bonusRep;
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
    public EventScheduler EventScheduler;
}
