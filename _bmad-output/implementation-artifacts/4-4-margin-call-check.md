# Story 4.4: Margin Call Check

Status: done

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

- [x] Task 1: Add margin call logic to MarginCallState (AC: 1, 2, 3)
  - [x] After liquidation, compare `roundProfit` against `MarginCallTargets.GetTarget(currentRound)`
  - [x] If target met: transition to ShopState (or directly to next MarketOpenState if shop is not yet built)
  - [x] If target NOT met: publish `MarginCallTriggeredEvent`, transition to RunSummaryState
  - [x] File: `Scripts/Runtime/Core/GameStates/MarginCallState.cs` (extended from Story 4.3 stub)
  - [x] File: `Scripts/Runtime/Core/GameStates/MarketCloseState.cs` (updated to pass config to MarginCallState)
- [x] Task 2: Create RunSummaryState (AC: 6)
  - [x] On Enter: calculate run statistics (rounds completed, total profit, reputation earned)
  - [x] Display run summary UI via static accessors and RunEndedEvent
  - [x] On user input (any key / button): transition to MetaHubState
  - [x] Publish `RunEndedEvent` with run results
  - [x] File: `Scripts/Runtime/Core/GameStates/RunSummaryState.cs`
- [x] Task 3: Create RunSummaryUI (AC: 6)
  - [x] Show: "MARGIN CALL" or "RUN COMPLETE" header
  - [x] Stats: rounds completed, final cash, total profit, items collected, reputation earned
  - [x] Reputation earned (placeholder value 0 until Epic 9)
  - [x] "Press any key to continue" prompt
  - [x] File: `Scripts/Runtime/UI/RunSummaryUI.cs`
- [x] Task 4: Define margin call and run end events (AC: 5)
  - [x] `MarginCallTriggeredEvent`: RoundNumber, RoundProfit, RequiredTarget, Shortfall
  - [x] `RunEndedEvent`: RoundsCompleted, FinalCash, TotalProfit, WasMarginCalled (bool), ReputationEarned, ItemsCollected
  - [x] Add to `Scripts/Runtime/Core/GameEvents.cs`
- [x] Task 5: Create placeholder MetaHubState and ShopState (AC: 2, 6)
  - [x] MetaHubState: minimal — Enter logs "MetaHub entered", awaits user input to start run
  - [x] ShopState: minimal — Enter logs "Shop entered", auto-skips to next MarketOpenState for now
  - [x] These are placeholders wired into the state machine so the full loop works
  - [x] Files: `Scripts/Runtime/Core/GameStates/MetaHubState.cs`, `Scripts/Runtime/Core/GameStates/ShopState.cs`

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
Claude Opus 4.6

### Debug Log References
- `[MarginCallState]` — margin call pass/fail decisions logged with round number, profit, and target
- `[RunSummaryState]` — run end header (MARGIN CALL / RUN COMPLETE) with stats
- `[ShopState]` — placeholder auto-skip log
- `[MetaHubState]` — placeholder entry log

### Completion Notes List
- Task 4: Added `MarginCallTriggeredEvent` and `RunEndedEvent` structs to GameEvents.cs with full field sets per story spec
- Task 5: Created `MetaHubState` (minimal stub) and `ShopState` (auto-skips to MarketOpenState via PrepareForNextRound + TransitionTo). ShopState uses NextConfig pattern consistent with other states.
- Task 1: Implemented full margin call logic in `MarginCallState` (which was a stub from Story 4.3). Reads `MarketCloseState.RoundProfit` and compares against `MarginCallTargets.GetTarget()`. Routes to ShopState on pass, RunSummaryState on fail with MarginCallTriggeredEvent. Updated `MarketCloseState` to pass `MarginCallStateConfig` before transition.
- Task 2: Created `RunSummaryState` with static accessors for UI, publishes `RunEndedEvent` with run stats. TotalProfit calculated as FinalCash - StartingCapital. ReputationEarned is placeholder 0.
- Task 3: Created `RunSummaryUI` MonoBehaviour subscribing to RunEndedEvent. Displays header (MARGIN CALL / RUN COMPLETE), stats block, and prompt. Static utility methods for testability (GetHeaderText, FormatCash, FormatProfit).

### File List
- `Assets/Scripts/Runtime/Core/GameEvents.cs` (modified — added MarginCallTriggeredEvent, RunEndedEvent with ItemsCollected)
- `Assets/Scripts/Runtime/Core/GameStates/MarginCallState.cs` (modified — full implementation replacing stub, Debug.Assert added)
- `Assets/Scripts/Runtime/Core/GameStates/MarketCloseState.cs` (modified — passes MarginCallStateConfig before transition, RoundProfit internal set for testability)
- `Assets/Scripts/Runtime/AssemblyInfo.cs` (new — InternalsVisibleTo for test assembly)
- `Assets/Scripts/Runtime/Core/GameStates/RunSummaryState.cs` (new — with input handling and MetaHubState transition)
- `Assets/Scripts/Runtime/Core/GameStates/ShopState.cs` (new)
- `Assets/Scripts/Runtime/Core/GameStates/MetaHubState.cs` (new)
- `Assets/Scripts/Runtime/Core/RunContext.cs` (modified — added StartingCapital property)
- `Assets/Scripts/Runtime/UI/RunSummaryUI.cs` (new — with items collected stat)
- `Assets/Tests/Runtime/Core/GameEventsTests.cs` (modified — MarginCallTriggeredEvent, RunEndedEvent with ItemsCollected tests)
- `Assets/Tests/Runtime/Core/GameStates/MarketCloseStateTests.cs` (modified — updated transition chain test)
- `Assets/Tests/Runtime/Core/GameStates/MarginCallStateTests.cs` (new — fixed weak test)
- `Assets/Tests/Runtime/Core/GameStates/RunSummaryStateTests.cs` (new — added input handling, items, starting capital tests)
- `Assets/Tests/Runtime/Core/GameStates/MetaHubStateTests.cs` (new)
- `Assets/Tests/Runtime/Core/GameStates/ShopStateTests.cs` (new)
- `Assets/Tests/Runtime/UI/RunSummaryUITests.cs` (new)

## Senior Developer Review (AI)

**Review Date:** 2026-02-11
**Reviewer:** Claude Opus 4.6 (Code Review)
**Outcome:** Changes Requested → Fixed

### Issues Found: 2 High, 4 Medium, 2 Low

### Action Items
- [x] [HIGH] RunSummaryState had no input handling — player stuck forever. Fixed: added AdvanceTime with input delay + Input.anyKeyDown → TransitionTo<MetaHubState>
- [x] [HIGH] Missing "items collected" stat in UI and RunEndedEvent. Fixed: added ItemsCollected field to RunEndedEvent, RunSummaryState, and RunSummaryUI
- [x] [MED] MarginCallState missing Debug.Assert for NextConfig. Fixed: added assert consistent with all other states
- [x] [MED] RunSummaryStateConfig had unused RoundProfit/RequiredTarget fields. Fixed: now consumed and exposed as static accessors
- [x] [MED] Weak no-op test (Assert.Pass). Fixed: replaced with real assertions verifying margin call triggers
- [x] [MED] TotalProfit hardcoded to GameConfig.StartingCapital. Fixed: added StartingCapital to RunContext, used in RunSummaryState
- [ ] [LOW] Unused ProfitColor/LossColor in RunSummaryUI — cosmetic, deferred
- [ ] [LOW] ShopState infinite loop past round 8 — Epic 6 concern, not this story's scope

## Change Log
- 2026-02-11: Implemented margin call check system — MarginCallState compares round profit against escalating targets, routes to ShopState (pass) or RunSummaryState (fail). Created RunSummaryState/UI for run end display. Added MarginCallTriggeredEvent and RunEndedEvent. Created MetaHubState and ShopState placeholders to complete full game loop wiring.
- 2026-02-11: Code review fixes — Added input handling to RunSummaryState (any key → MetaHubState with 0.5s delay). Added ItemsCollected to RunEndedEvent/UI. Added Debug.Assert to MarginCallState. Added StartingCapital tracking to RunContext. Fixed weak test. Exposed RoundProfit/RequiredTarget in RunSummaryState static accessors. Added InternalsVisibleTo for test access to MarketCloseState.RoundProfit. Updated MarketCloseStateTests for full transition chain.
