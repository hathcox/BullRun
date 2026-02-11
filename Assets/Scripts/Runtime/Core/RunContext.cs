using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central data carrier for run state.
/// Carries current act, round, portfolio, and active items.
/// </summary>
public class RunContext
{
    public int CurrentAct { get; set; }
    public int CurrentRound { get; set; }
    public Portfolio Portfolio { get; private set; }
    public List<string> ActiveItems { get; private set; }
    public float StartingCapital { get; private set; }

    public RunContext(int currentAct, int currentRound, Portfolio portfolio)
    {
        CurrentAct = currentAct;
        CurrentRound = currentRound;
        Portfolio = portfolio;
        ActiveItems = new List<string>();
        StartingCapital = portfolio.Cash;
    }

    /// <summary>
    /// Creates a fresh RunContext for a new run with starting capital.
    /// Publishes RunStartedEvent via EventBus.
    /// </summary>
    public static RunContext StartNewRun()
    {
        var ctx = new RunContext(1, 1, new Portfolio(GameConfig.StartingCapital));
        EventBus.Publish(new RunStartedEvent { StartingCapital = GameConfig.StartingCapital });
        return ctx;
    }

    /// <summary>
    /// Convenience accessor for Portfolio.Cash.
    /// </summary>
    public float GetCurrentCash()
    {
        return Portfolio.Cash;
    }

    /// <summary>
    /// Advances to next round. Cash carries forward, round-level state is reset.
    /// Positions MUST be cleared by LiquidateAllPositions before calling this.
    /// Delegates to AdvanceRound() for round/act tracking, then resets portfolio.
    /// </summary>
    public void PrepareForNextRound()
    {
        Debug.Assert(Portfolio.PositionCount == 0,
            "[RunContext] PrepareForNextRound called with open positions — liquidate first!");
        AdvanceRound();
        Portfolio.StartRound(Portfolio.Cash);
    }

    /// <summary>
    /// Advances to the next round and updates the act if crossing an act boundary.
    /// Returns true if the act changed (for triggering act transition UI).
    /// </summary>
    public bool AdvanceRound()
    {
        int previousAct = CurrentAct;
        CurrentRound++;
        CurrentAct = GetActForRound(CurrentRound);
        return CurrentAct != previousAct;
    }

    /// <summary>
    /// Returns the act number for a given round.
    /// Derives from GameConfig.Acts data: each act covers RoundsPerAct rounds.
    /// </summary>
    public static int GetActForRound(int round)
    {
        return ((round - 1) / GameConfig.RoundsPerAct) + 1;
    }

    /// <summary>
    /// Returns the StockTier for a given act number.
    /// Reads from GameConfig.Acts to avoid duplication.
    /// </summary>
    public static StockTier GetTierForAct(int act)
    {
        if (act >= 1 && act < GameConfig.Acts.Length)
            return GameConfig.Acts[act].Tier;
        return GameConfig.Acts[GameConfig.TotalActs].Tier;
    }

    /// <summary>
    /// Returns true if the run is complete (all rounds have been played).
    /// </summary>
    public bool IsRunComplete()
    {
        return CurrentRound > GameConfig.TotalRounds;
    }
}
