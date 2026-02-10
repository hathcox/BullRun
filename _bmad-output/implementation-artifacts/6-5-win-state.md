# Story 6.5: Win State

Status: ready-for-dev

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

- [ ] Task 1: Implement victory detection in MarketCloseState (AC: 1)
  - [ ] After Round 8 margin call passes: set `RunContext.RunCompleted = true`
  - [ ] Transition to RunSummaryState with victory flag
  - [ ] File: `Scripts/Runtime/Core/GameStates/MarketCloseState.cs` (extend)
- [ ] Task 2: Create victory variant of RunSummaryUI (AC: 2, 3, 5)
  - [ ] Victory header: "BULL RUN COMPLETE!" in gold/green, large animated text
  - [ ] Stats grid:
    - Total Profit: sum of all round profits
    - Peak Cash: highest cash value reached during the run
    - Rounds Completed: 8/8
    - Items Collected: count of shop items purchased
    - Best Round: round with highest profit
  - [ ] Distinctly different from loss variant: gold accents, upward animations, success colors
  - [ ] File: `Scripts/Runtime/UI/RunSummaryUI.cs` (extend from Story 4.4)
- [ ] Task 3: Calculate reputation earned (AC: 4)
  - [ ] Win reputation: `100 + profitBonus` where profitBonus scales with total profit
  - [ ] Loss reputation: `10 + (5 * roundsCompleted)` (already defined in GDD)
  - [ ] Display reputation earned prominently on summary screen
  - [ ] Store in RunContext for MetaManager to consume (Epic 9 will process it)
  - [ ] File: `Scripts/Runtime/Core/RunContext.cs` (extend)
- [ ] Task 4: Track peak cash and run statistics (AC: 3)
  - [ ] RunContext tracks `PeakCash` — updated after each round's market close
  - [ ] RunContext tracks `ItemsCollected` count — incremented on shop purchase
  - [ ] RunContext tracks `BestRoundProfit` — highest single round profit
  - [ ] RunContext tracks `TotalProfit` — sum of all round profits
  - [ ] File: `Scripts/Runtime/Core/RunContext.cs` (extend)
- [ ] Task 5: Add victory celebration effects (AC: 5)
  - [ ] Confetti particle effect or animated sparkles
  - [ ] Gold color theme on the summary screen
  - [ ] Numbers count up from zero to final value (animated)
  - [ ] "Press any key to continue" to MetaHub
  - [ ] File: `Scripts/Runtime/UI/RunSummaryUI.cs` (extend)
- [ ] Task 6: Define RunCompletedEvent (AC: 1)
  - [ ] `RunCompletedEvent`: TotalProfit, PeakCash, RoundsCompleted, ItemsCollected, ReputationEarned, IsVictory
  - [ ] Published by RunSummaryState on Enter
  - [ ] Audio system (Epic 11) will subscribe for victory music
  - [ ] Add to `Scripts/Runtime/Core/GameEvents.cs`

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
| Animation | Confetti, count-up numbers | Screen cracks, collapse |
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

### Debug Log References

### Completion Notes List

### File List
