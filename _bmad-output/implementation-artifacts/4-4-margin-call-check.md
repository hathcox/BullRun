# Story 4.4: Margin Call Check

Status: ready-for-dev

## Story

As a player,
I want to fail the round if my profit falls below the target,
so that there are real stakes each round and the run can end.

## Acceptance Criteria

1. After market close, the round profit is compared against the margin call target
2. If round profit >= target: round succeeds, proceed to shop/next round
3. If round profit < target: MARGIN CALL triggered, run ends immediately
4. Margin call uses escalating targets per round from MarginCallTargets data
5. Margin call event is published via EventBus for UI/audio to react
6. On margin call, transition to RunSummary state (not shop)

## Tasks / Subtasks

- [ ] Task 1: Add margin call logic to MarketCloseState (AC: 1, 2, 3)
  - [ ] After liquidation, compare `roundProfit` against `MarginCallTargets.GetTarget(currentRound)`
  - [ ] If target met: transition to ShopState (or directly to next MarketOpenState if shop is not yet built)
  - [ ] If target NOT met: publish `MarginCallTriggeredEvent`, transition to RunSummaryState
  - [ ] File: `Scripts/Runtime/Core/GameStates/MarketCloseState.cs` (extend from Story 4.3)
- [ ] Task 2: Create RunSummaryState (AC: 6)
  - [ ] On Enter: calculate run statistics (rounds completed, total profit, reputation earned)
  - [ ] Display run summary UI
  - [ ] On user input (any key / button): transition to MetaHubState
  - [ ] Publish `RunEndedEvent` with run results
  - [ ] File: `Scripts/Runtime/Core/GameStates/RunSummaryState.cs`
- [ ] Task 3: Create RunSummaryUI (AC: 6)
  - [ ] Show: "MARGIN CALL" or "RUN COMPLETE" header
  - [ ] Stats: rounds completed, final cash, total profit, best round, items collected
  - [ ] Reputation earned (placeholder value until Epic 9)
  - [ ] "Press any key to continue" prompt
  - [ ] File: `Scripts/Runtime/UI/RunSummaryUI.cs`
- [ ] Task 4: Define margin call and run end events (AC: 5)
  - [ ] `MarginCallTriggeredEvent`: RoundNumber, RoundProfit, RequiredTarget, Shortfall
  - [ ] `RunEndedEvent`: RoundsCompleted, FinalCash, TotalProfit, WasMarginCalled (bool), ReputationEarned
  - [ ] Add to `Scripts/Runtime/Core/GameEvents.cs`
- [ ] Task 5: Create placeholder MetaHubState and ShopState (AC: 2, 6)
  - [ ] MetaHubState: minimal — Enter logs "MetaHub entered", awaits user input to start run
  - [ ] ShopState: minimal — Enter logs "Shop entered", auto-skips to next MarketOpenState for now
  - [ ] These are placeholders wired into the state machine so the full loop works
  - [ ] Files: `Scripts/Runtime/Core/GameStates/MetaHubState.cs`, `Scripts/Runtime/Core/GameStates/ShopState.cs`

## Dev Notes

### Architecture Compliance

- **State machine flow completed:** This story wires the full loop: MetaHub → MarketOpen → Trading → MarketClose → (Shop or RunSummary) → MetaHub
- **MarginCallTriggeredEvent** per architecture event naming: `{Subject}{Verb}Event`
- **RunContext:** Carries round number used to look up margin call target
- **Data access:** `MarginCallTargets.GetTarget(round)` — direct static class access (from Story 3.2)

### Margin Call — The Failure State (GDD Section 2.3)

> "If the player fails to meet the target in any single round, they receive a MARGIN CALL and the run ends."

This is the roguelike death equivalent. It should feel devastating but fair — the player knew the target in advance (shown during Market Open) and had 60 seconds to hit it.

### Target Values (Already in MarginCallTargets from Story 3.2)

| Round | Target | Scaling |
|-------|--------|---------|
| 1 | $200 | 1.0x |
| 2 | $350 | 1.0x |
| 3 | $600 | 1.5x |
| 4 | $900 | 1.5x |
| 5 | $1,500 | 2.0x |
| 6 | $2,200 | 2.0x |
| 7 | $3,500 | 2.5x |
| 8 | $5,000 | 3.0x |

### Round Profit Calculation

Round profit = portfolio value at market close - portfolio value at round start. This uses `Portfolio.RoundProfit` from Story 2.4. The check is simple:

```csharp
float roundProfit = ctx.Portfolio.RoundProfit;
float target = MarginCallTargets.GetTarget(ctx.CurrentRound);

if (roundProfit >= target)
{
    Debug.Log($"[GameState] Round {ctx.CurrentRound} PASSED: ${roundProfit:F2} >= ${target:F2}");
    // Proceed to shop
}
else
{
    Debug.Log($"[GameState] MARGIN CALL! Round {ctx.CurrentRound}: ${roundProfit:F2} < ${target:F2}");
    EventBus.Publish(new MarginCallTriggeredEvent(ctx.CurrentRound, roundProfit, target));
    // Transition to RunSummary
}
```

### Placeholder States

MetaHubState and ShopState are stubs here so the full game loop is wired. They'll be fleshed out in Epic 6 (Run Structure), Epic 7 (Draft Shop), and Epic 9 (Meta-Progression).

### Project Structure Notes

- Modifies: `Scripts/Runtime/Core/GameStates/MarketCloseState.cs`
- Creates: `Scripts/Runtime/Core/GameStates/RunSummaryState.cs`
- Creates: `Scripts/Runtime/Core/GameStates/MetaHubState.cs` (stub)
- Creates: `Scripts/Runtime/Core/GameStates/ShopState.cs` (stub)
- Creates: `Scripts/Runtime/UI/RunSummaryUI.cs`
- Modifies: `Scripts/Runtime/Core/GameEvents.cs`

### References

- [Source: game-architecture.md#Game State Machine] — Full state flow: MetaHub → MarketOpen → Trading → MarketClose → Shop → RunSummary
- [Source: game-architecture.md#Game State Transitions] — TransitionTo pattern
- [Source: bull-run-gdd-mvp.md#2.3] — "Margin Call: The Failure State" — target table and mechanics
- [Source: bull-run-gdd-mvp.md#2.2] — "If the player's total profit for this round falls below the Margin Call target, the run ends immediately"
- [Source: bull-run-gdd-mvp.md#6.2] — Margin call dramatic effects (detailed in Epic 10)

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
