# Story 6.5: Win State

Status: done

## Story

As a player,
I want to be celebrated when I complete Round 8,
so that winning a full run feels like a major achievement.

## Acceptance Criteria

1. After passing Round 8's margin call check, a victory sequence triggers
2. Victory screen shows "BULL RUN COMPLETE" or similar triumphant header
3. Run summary displays: total profit across all 8 rounds, peak cash reached, items collected, rounds completed
4. Reputation earned is calculated and displayed (proportional to performance)
5. Victory feels distinctly different from a margin call loss — celebratory, not just informational
6. After victory screen, transition to MetaHub

## Tasks / Subtasks

- [x] Task 1: Implement victory detection in MarketCloseState (AC: 1)
  - [x] After Round 8 margin call passes: set `RunContext.RunCompleted = true`
  - [x] Transition to RunSummaryState with victory flag
  - [x] File: `Scripts/Runtime/Core/GameStates/MarketCloseState.cs` (extend)
- [x] Task 2: Create victory variant of RunSummaryUI (AC: 2, 3, 5)
  - [x] Victory header: "BULL RUN COMPLETE!" in gold/green, large animated text
  - [x] Stats grid:
    - Total Profit: sum of all round profits
    - Peak Cash: highest cash value reached during the run
    - Rounds Completed: 8/8
    - Items Collected: count of shop items purchased
    - Best Round: round with highest profit
  - [x] Distinctly different from loss variant: gold accents, upward animations, success colors
  - [x] File: `Scripts/Runtime/UI/RunSummaryUI.cs` (extend from Story 4.4)
- [x] Task 3: Calculate reputation earned (AC: 4)
  - [x] Win reputation: `100 + profitBonus` where profitBonus scales with total profit
  - [x] Loss reputation: `10 + (5 * roundsCompleted)` (already defined in GDD)
  - [x] Display reputation earned prominently on summary screen
  - [x] Store in RunContext for MetaManager to consume (Epic 9 will process it)
  - [x] File: `Scripts/Runtime/Core/RunContext.cs` (extend)
- [x] Task 4: Track peak cash and run statistics (AC: 3)
  - [x] RunContext tracks `PeakCash` — updated after each round's market close
  - [x] RunContext tracks `ItemsCollected` count — incremented on shop purchase
  - [x] RunContext tracks `BestRoundProfit` — highest single round profit
  - [x] RunContext tracks `TotalProfit` — sum of all round profits
  - [x] File: `Scripts/Runtime/Core/RunContext.cs` (extend)
- [x] Task 5: Add victory celebration effects (AC: 5)
  - [x] Confetti particle effect or animated sparkles
  - [x] Gold color theme on the summary screen
  - [x] Numbers count up from zero to final value (animated)
  - [x] "Press any key to continue" to MetaHub
  - [x] File: `Scripts/Runtime/UI/RunSummaryUI.cs` (extend)
- [x] Task 6: Define RunCompletedEvent (AC: 1)
  - [x] `RunCompletedEvent`: TotalProfit, PeakCash, RoundsCompleted, ItemsCollected, ReputationEarned, IsVictory
  - [x] Published by RunSummaryState on Enter
  - [x] Audio system (Epic 11) will subscribe for victory music
  - [x] Add to `Scripts/Runtime/Core/GameEvents.cs`

## Dev Notes

### Architecture Compliance

- **RunContext tracks all stats** — single source of truth for run data
- **RunSummaryState handles both win and loss** — differentiated by `IsVictory` flag
- **EventBus:** Publishes `RunCompletedEvent` for meta-progression and audio
- **Reputation calculation is preliminary** — Epic 9 (Meta-Progression) will formalize the MetaManager that processes and persists reputation. This story just calculates and displays it.

### Victory vs Loss Comparison

| Element | Victory | Loss (Margin Call) |
|---------|---------|-------------------|
| Header | "BULL RUN COMPLETE!" (gold) | "MARGIN CALL" (red) |
| Colors | Gold, green, celebratory | Red, dark, dramatic |
| Animation | Sparkles, count-up numbers | Screen cracks, collapse |
| Stats | Full 8-round summary | Partial summary |
| Tone | Triumphant achievement | Devastating but cinematic |
| Reputation | 100+ bonus | 10 + 5*rounds |

### Reputation Formula (GDD Section 5)

```csharp
int CalculateReputation(RunContext ctx)
{
    if (ctx.IsVictory)
    {
        int profitBonus = Mathf.FloorToInt(ctx.TotalProfit / 100f); // $1 rep per $100 profit
        return 100 + profitBonus;
    }
    else
    {
        return 10 + (5 * ctx.CurrentRound);
    }
}
```

> "Reputation earned: 10 + (5 x rounds completed) on loss. 100 + profit bonus on win."

### The Aspirational Moment

Winning a full run should be rare and celebrated. The GDD targets 10-15% win rate for experienced players early in meta-progression. When a player finally beats Round 8, the victory screen is their reward — make it feel incredible.

### Project Structure Notes

- Modifies: `Scripts/Runtime/Core/GameStates/MarketCloseState.cs`
- Modifies: `Scripts/Runtime/UI/RunSummaryUI.cs`
- Modifies: `Scripts/Runtime/Core/RunContext.cs`
- Modifies: `Scripts/Runtime/Core/GameEvents.cs`
- No new files — extends existing run summary infrastructure

### References

- [Source: bull-run-gdd-mvp.md#5.1] — "Reputation earned from every run proportional to performance"
- [Source: bull-run-gdd-mvp.md#11.1] — "Reputation per Run (loss): 10 + (5 x rounds completed). Reputation per Run (win): 100 + profit bonus"
- [Source: bull-run-gdd-mvp.md#11.2] — "Target win rate: 10-15% for experienced players"
- [Source: bull-run-gdd-mvp.md#6.2] — "Big Wins: champagne-cork sound, confetti particles, slow-motion"
- [Source: game-architecture.md#Game State Machine] — RunSummary state in the flow

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Debug Log References

- `[MarginCallState]` logs now indicate when RunCompleted is set for round 8
- `[RunSummaryState]` logs now show "BULL RUN COMPLETE" header for victory path and reputation earned
- `[MarketCloseState]` calls UpdateRunStats for PeakCash/BestRoundProfit/TotalRunProfit tracking

### Completion Notes List

- Task 1: Added `RunCompleted` bool property to RunContext. MarginCallState sets it to true when `ctx.CurrentRound >= GameConfig.TotalRounds` and margin call passes. Victory detection flows through existing path: MarginCallState → ShopState → RunSummaryState.
- Task 2: Updated RunSummaryUI header from "BULL RUN!" to "BULL RUN COMPLETE!" with gold color (VictoryGoldColor). Added separate `BuildVictoryStatsText()` and `BuildLossStatsText()` methods. Victory shows Total Profit, Peak Cash, Rounds Completed (8/8), Items Collected, Best Round, Reputation. Loss shows simpler stats. Victory stats use gold color.
- Task 3: Implemented `CalculateReputation()` static method in RunSummaryState. Win: `100 + floor(totalProfit / 100)`. Loss: `10 + (5 * roundsCompleted)`. Result stored in `ctx.ReputationEarned` and published in both RunEndedEvent and RunCompletedEvent. Updated existing placeholder test.
- Task 4: Added PeakCash, ItemsCollected, BestRoundProfit, TotalRunProfit, ReputationEarned properties to RunContext. Added `UpdateRunStats(float roundProfit)` method that tracks all stats. Called from MarketCloseState after liquidation. All stats properly reset in `ResetForNewRun()`. Added PeakCash and BestRoundProfit fields to RunEndedEvent.
- Task 5: Added count-up number animation (2s duration, ease-out curve) via `BuildVictoryStatsTextAnimated()`. Added sparkle effects (12 animated UI dots with sine-wave alpha pulsing in gold colors). Gold color theme applied to header and stats text for victory. "Press any key to continue" prompt already existed. Sparkles cleaned up on state exit.
- Task 6: Defined `RunCompletedEvent` struct in GameEvents.cs with TotalProfit, PeakCash, RoundsCompleted, ItemsCollected, ReputationEarned, IsVictory fields. Published by RunSummaryState.Enter() after RunEndedEvent. Audio system (Epic 11) and meta-progression (Epic 9) can subscribe.

### Change Log

- 2026-02-12: Story 6.5 Win State — Implemented all 6 tasks: victory detection, victory UI variant, reputation calculation, run statistics tracking, celebration effects, and RunCompletedEvent definition.
- 2026-02-12: Code Review Fixes — H1: Made ItemsCollected a computed property (=> ActiveItems.Count), removed dead setter. H2: Consolidated RunCompletedEvent into RunEndedEvent (added IsVictory field), eliminated duplicate event publishing. H3: Reconciled TotalRunProfit with authoritative calculation in RunSummaryState. M1: Simplified victory detection to single source (ctx.RunCompleted). M3: Added negative-profit victory reputation test. M4: Fixed Dev Notes table (confetti → sparkles).

### File List

- `Assets/Scripts/Runtime/Core/RunContext.cs` (modified) — Added RunCompleted, PeakCash, ItemsCollected, BestRoundProfit, TotalRunProfit, ReputationEarned properties; UpdateRunStats method; ResetForNewRun updates
- `Assets/Scripts/Runtime/Core/GameEvents.cs` (modified) — Added RunCompletedEvent struct; Added PeakCash and BestRoundProfit fields to RunEndedEvent
- `Assets/Scripts/Runtime/Core/GameStates/MarginCallState.cs` (modified) — Sets ctx.RunCompleted when final round passes margin call
- `Assets/Scripts/Runtime/Core/GameStates/MarketCloseState.cs` (modified) — Calls ctx.UpdateRunStats(RoundProfit) after liquidation
- `Assets/Scripts/Runtime/Core/GameStates/RunSummaryState.cs` (modified) — Added CalculateReputation method; publishes RunCompletedEvent; uses RunCompleted for victory detection; stores reputation in ctx
- `Assets/Scripts/Runtime/UI/RunSummaryUI.cs` (modified) — Updated header to "BULL RUN COMPLETE!"; separate victory/loss stats methods; count-up animation; sparkle effects; gold color theme
- `Assets/Scripts/Setup/UISetup.cs` (modified) — Updated comment for RunSummary overlay description
- `Assets/Tests/Runtime/Core/RunContextTests.cs` (modified) — Added RunCompleted, PeakCash, BestRoundProfit, TotalRunProfit, ItemsCollected, UpdateRunStats tests
- `Assets/Tests/Runtime/Core/GameStates/MarginCallStateTests.cs` (modified) — Added victory detection tests (round 8 pass/fail/non-final)
- `Assets/Tests/Runtime/Core/GameStates/RunSummaryStateTests.cs` (modified) — Added reputation calculation tests, RunCompletedEvent tests, updated placeholder rep test
- `Assets/Tests/Runtime/Core/GameEventsTests.cs` (modified) — Added RunCompletedEvent struct and EventBus tests
- `Assets/Tests/Runtime/UI/RunSummaryUITests.cs` (modified) — Updated header test to "BULL RUN COMPLETE!"; added victory/loss stats tests; added count-up animation tests
