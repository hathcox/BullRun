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

    public RunContext(int currentAct, int currentRound, Portfolio portfolio)
    {
        CurrentAct = currentAct;
        CurrentRound = currentRound;
        Portfolio = portfolio;
        ActiveItems = new List<string>();
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
    /// </summary>
    public void PrepareForNextRound()
    {
        Debug.Assert(Portfolio.PositionCount == 0,
            "[RunContext] PrepareForNextRound called with open positions — liquidate first!");
        CurrentRound++;
        Portfolio.StartRound(Portfolio.Cash);
    }
}
