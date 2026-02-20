using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central data carrier for run state.
/// Carries current act, round, portfolio, owned relics, expansions, bonds, and tips.
/// Internal setters on state properties allow DebugManager (F3 skip-to-round)
/// to reset context during editor/dev builds. Production mutation should go
/// through AdvanceRound(), PrepareForNextRound(), or ResetForNewRun().
/// </summary>
public class RunContext
{
    public int CurrentAct { get; internal set; }
    public int CurrentRound { get; internal set; }
    public Portfolio Portfolio { get; internal set; }
    public List<string> OwnedRelics { get; private set; }
    public List<string> OwnedExpansions { get; private set; }
    public int BondsOwned { get; internal set; }
    public List<BondRecord> BondPurchaseHistory { get; private set; }
    public int CurrentShopRerollCount { get; internal set; }
    public int InsiderTipSlots { get; internal set; }
    public List<RevealedTip> RevealedTips { get; private set; }
    public ReputationManager Reputation { get; private set; }
    public BondManager Bonds { get; private set; }
    public RelicManager RelicManager { get; private set; }
    public float StartingCapital { get; internal set; }

    /// <summary>
    /// True when the player has completed all rounds (survived Round 8 margin call).
    /// Set by MarginCallState when the final round passes.
    /// </summary>
    public bool RunCompleted { get; internal set; }

    /// <summary>
    /// Highest cash value reached during the run. Updated after each round's market close.
    /// </summary>
    public float PeakCash { get; internal set; }

    /// <summary>
    /// Number of relics collected during the run. Derived from OwnedRelics list.
    /// </summary>
    public int ItemsCollected => OwnedRelics.Count;

    /// <summary>
    /// Highest single-round profit achieved during the run.
    /// </summary>
    public float BestRoundProfit { get; internal set; }

    /// <summary>
    /// Sum of all round profits during the run.
    /// </summary>
    public float TotalRunProfit { get; internal set; }

    /// <summary>
    /// Reputation earned for this run. Calculated by RunSummaryState.
    /// Stored here for MetaManager (Epic 9) to consume.
    /// </summary>
    public int ReputationEarned { get; internal set; }

    /// <summary>
    /// The stock tier for the current act. Convenience for GetTierForAct(CurrentAct).
    /// </summary>
    public StockTier CurrentTier => GetTierForAct(CurrentAct);

    /// <summary>
    /// The ActConfig for the current act. Convenience for GameConfig.Acts[CurrentAct].
    /// Clamps to valid range (returns last act config if beyond round 8).
    /// </summary>
    public ActConfig CurrentActConfig
    {
        get
        {
            int act = CurrentAct;
            if (act < 1) act = 1;
            if (act >= GameConfig.Acts.Length) act = GameConfig.TotalActs;
            return GameConfig.Acts[act];
        }
    }

    public RunContext(int currentAct, int currentRound, Portfolio portfolio)
    {
        CurrentAct = currentAct;
        CurrentRound = currentRound;
        Portfolio = portfolio;
        OwnedRelics = new List<string>();
        OwnedExpansions = new List<string>();
        BondsOwned = 0;
        BondPurchaseHistory = new List<BondRecord>();
        CurrentShopRerollCount = 0;
        InsiderTipSlots = GameConfig.DefaultInsiderTipSlots;
        RevealedTips = new List<RevealedTip>();
        Reputation = new ReputationManager();
        Bonds = new BondManager(this);
        RelicManager = new RelicManager(this);
        StartingCapital = portfolio.Cash;
        PeakCash = portfolio.Cash;
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
        if (round < 1) round = 1;
        return ((round - 1) / GameConfig.RoundsPerAct) + 1;
    }

    /// <summary>
    /// Returns the StockTier for a given round number.
    /// Convenience for GetTierForAct(GetActForRound(round)).
    /// </summary>
    public static StockTier GetTierForRound(int round)
    {
        return GetTierForAct(GetActForRound(round));
    }

    /// <summary>
    /// Returns the StockTier for a given act number.
    /// Reads from GameConfig.Acts to avoid duplication.
    /// </summary>
    public static StockTier GetTierForAct(int act)
    {
        if (act < 1) act = 1;
        if (act >= GameConfig.Acts.Length) act = GameConfig.TotalActs;
        return GameConfig.Acts[act].Tier;
    }

    /// <summary>
    /// Updates run statistics after a round's market close.
    /// Called by MarketCloseState after liquidation.
    /// PeakCash reflects post-liquidation cash (true realized value),
    /// not mid-round cash which fluctuates with open positions.
    /// </summary>
    public void UpdateRunStats(float roundProfit)
    {
        TotalRunProfit += roundProfit;
        if (roundProfit > BestRoundProfit)
            BestRoundProfit = roundProfit;
        if (Portfolio.Cash > PeakCash)
            PeakCash = Portfolio.Cash;
    }

    /// <summary>
    /// Returns true if the run is complete (all rounds have been played).
    /// </summary>
    public bool IsRunComplete()
    {
        return CurrentRound > GameConfig.TotalRounds;
    }

    /// <summary>
    /// Resets this RunContext for a fresh run. Replaces Portfolio, resets round/act to 1.
    /// Used by MetaHubState placeholder to restart the game loop.
    /// </summary>
    public void ResetForNewRun()
    {
        Portfolio.UnsubscribeFromPriceUpdates();
        Portfolio = new Portfolio(GameConfig.StartingCapital);
        Portfolio.SubscribeToPriceUpdates();
        Portfolio.StartRound(Portfolio.Cash);
        CurrentAct = 1;
        CurrentRound = 1;
        OwnedRelics.Clear();
        OwnedExpansions.Clear();
        BondsOwned = 0;
        BondPurchaseHistory.Clear();
        Bonds = new BondManager(this);
        RelicManager = new RelicManager(this);
        CurrentShopRerollCount = 0;
        InsiderTipSlots = GameConfig.DefaultInsiderTipSlots;
        RevealedTips.Clear();
        Reputation.Reset();
        StartingCapital = Portfolio.Cash;
        RunCompleted = false;
        PeakCash = Portfolio.Cash;
        BestRoundProfit = 0f;
        TotalRunProfit = 0f;
        ReputationEarned = 0;
        EventBus.Publish(new RunStartedEvent { StartingCapital = GameConfig.StartingCapital });
    }
}
