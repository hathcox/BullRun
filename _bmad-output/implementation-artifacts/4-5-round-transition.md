# Story 4.5: Round Transition

Status: done

## Story

As a player,
I want a clear transition between rounds showing my results,
so that I understand how I performed and feel the pacing between rounds.

## Acceptance Criteria

1. After passing the margin call check, a round results screen is shown briefly
2. Results show: round number, round profit, target met, total accumulated cash
3. Transition flows to Draft Shop (or next MarketOpen if shop not yet built)
4. Round counter increments and act advances at the correct boundaries (rounds 3, 5, 7)
5. After Round 8 (final round), transition to a win state RunSummary instead of shop
6. The full loop is playable: MetaHub → (MarketOpen → Trading → MarketClose → results → Shop) x8 → RunSummary

## Tasks / Subtasks

- [x] Task 1: Create round results display (AC: 1, 2)
  - [x] Brief overlay after margin call passes: "ROUND 3 COMPLETE"
  - [x] Show: Round Profit (+$650), Target ($600 — PASSED), Total Cash ($2,800)
  - [x] Display for 2-3 seconds or until player presses continue
  - [x] Green checkmark or success indicator
  - [x] File: `Scripts/Runtime/UI/RoundResultsUI.cs` (new)
- [x] Task 2: Implement round/act progression in RunContext (AC: 4)
  - [x] Method: `AdvanceRound()` — increments round, checks act boundary
  - [x] Act boundaries: Rounds 1-2 = Act 1, 3-4 = Act 2, 5-6 = Act 3, 7-8 = Act 4
  - [x] Method: `GetCurrentAct()` — derives act from round number
  - [x] Method: `GetCurrentTier()` — maps act to StockTier enum
  - [x] Method: `IsRunComplete()` — returns true after Round 8
  - [x] File: `Scripts/Runtime/Core/RunContext.cs` (extend)
- [x] Task 3: Wire complete game loop transitions (AC: 3, 5, 6)
  - [x] MarketCloseState → (margin call pass) → show results → ShopState
  - [x] ShopState → (shop done) → AdvanceRound → MarketOpenState (next round)
  - [x] If `RunContext.IsRunComplete()`: → RunSummaryState with win flag
  - [x] MetaHubState → (start run) → RunContext.StartNewRun() → MarketOpenState (Round 1)
  - [x] File: multiple GameStates files (extend)
- [x] Task 4: Create act transition indicator (AC: 4)
  - [x] When act changes (e.g., Round 2 → Round 3 = Act 1 → Act 2):
  - [x] Show "ACT 2 — LOW-VALUE STOCKS" interstitial screen before MarketOpen
  - [x] Brief dramatic reveal (1-2 seconds) of the new tier
  - [x] File: `Scripts/Runtime/UI/ActTransitionUI.cs` (new)
- [x] Task 5: Win state in RunSummary (AC: 5)
  - [x] RunSummaryState already exists from Story 4.4
  - [x] Add win path: if `!wasMarginCalled && runComplete`, show victory variant
  - [x] Victory header: "RUN COMPLETE — BULL RUN!" or similar
  - [x] Show final stats: total profit across all 8 rounds, peak cash, items used
  - [x] File: `Scripts/Runtime/Core/GameStates/RunSummaryState.cs` (extend)
  - [x] File: `Scripts/Runtime/UI/RunSummaryUI.cs` (extend)
- [x] Task 6: Add act/round config mappings (AC: 4)
  - [x] Define act-to-tier mapping and round-to-act mapping in GameConfig or new data class
  - [x] `ActConfig`: act number, tier, round range, display name
  - [x] File: `Scripts/Setup/Data/GameConfig.cs` (extend)

## Dev Notes

### Architecture Compliance

- **Completes the game loop** — after this story, the full state machine is wired end-to-end
- **RunContext is the single source** for round/act progression state
- **All transitions through GameStateMachine.TransitionTo<T>()** — never bypass

### Full Game Loop After This Story

```
MetaHubState
  └→ (start run) → StartNewRun()
      └→ MarketOpenState (Round 1, Act 1, Penny)
          └→ TradingState (60s timer)
              └→ MarketCloseState (liquidate)
                  ├→ MARGIN CALL → RunSummaryState (loss)
                  └→ PASS → RoundResultsUI → ShopState
                      └→ AdvanceRound()
                          ├→ Act change? → ActTransitionUI
                          ├→ Round <= 8 → MarketOpenState (next round)
                          └→ Round > 8 → RunSummaryState (win!)
```

### Act Boundaries

| Rounds | Act | Tier | Display Name |
|--------|-----|------|-------------|
| 1-2 | 1 | Penny | "Penny Stocks" |
| 3-4 | 2 | LowValue | "Low-Value Stocks" |
| 5-6 | 3 | MidValue | "Mid-Value Stocks" |
| 7-8 | 4 | BlueChip | "Blue Chips" |

### Pacing Between Rounds

The sequence should feel brisk: MarketClose → results (2s) → Shop (15-20s) → act transition if applicable (1-2s) → MarketOpen (5-10s) → Trading. Total downtime between trading phases: ~25-30 seconds. The GDD targets a full run at 7-10 minutes.

### Project Structure Notes

- Creates: `Scripts/Runtime/UI/RoundResultsUI.cs`
- Creates: `Scripts/Runtime/UI/ActTransitionUI.cs`
- Modifies: `Scripts/Runtime/Core/RunContext.cs`
- Modifies: `Scripts/Runtime/Core/GameStates/MarketCloseState.cs`
- Modifies: `Scripts/Runtime/Core/GameStates/ShopState.cs`
- Modifies: `Scripts/Runtime/Core/GameStates/RunSummaryState.cs`
- Modifies: `Scripts/Runtime/UI/RunSummaryUI.cs`
- Modifies: `Scripts/Setup/Data/GameConfig.cs`

### References

- [Source: game-architecture.md#Game State Machine] — Full state flow and RunContext
- [Source: bull-run-gdd-mvp.md#2.1] — Run structure: 4 Acts, 2 rounds per act, 8 rounds total
- [Source: bull-run-gdd-mvp.md#2.1] — Act-to-tier mapping table
- [Source: bull-run-gdd-mvp.md#2.2] — Phase 3: "If the target is met, the player enters the Draft Shop"
- [Source: bull-run-gdd-mvp.md#9] — "Phase 1 Gate Check" — core loop must be playable

## Dev Agent Record

### Agent Model Used
Claude Opus 4.6

### Debug Log References
N/A — no debugging issues encountered

### Completion Notes List
- Task 1: Created `RoundResultsUI` MonoBehaviour with `RoundCompletedEvent` subscription. Shows "ROUND X COMPLETE" header, round profit, target status (PASSED/FAILED), total cash, and checkmark/X indicator. Auto-dismisses after 2.5s or on any key press. Added `RoundCompletedEvent` and `ActTransitionEvent` to GameEvents.
- Task 2: Added `AdvanceRound()` (returns bool for act change), `GetActForRound()`, `GetTierForAct()`, and `IsRunComplete()` to RunContext. Act boundaries derived from `(round-1)/2 + 1` formula. Run complete when round > 8.
- Task 3: MarginCallState now publishes `RoundCompletedEvent` on margin call pass before transitioning to ShopState. ShopState now uses `AdvanceRound()` instead of `PrepareForNextRound()`, checks `IsRunComplete()` and transitions to RunSummaryState (win) if true.
- Task 4: Created `ActTransitionUI` MonoBehaviour subscribing to `ActTransitionEvent`. Shows "ACT X — Tier Name" interstitial for 1.5s. Static utility methods for tier display names.
- Task 5: Added `IsVictory` static accessor to RunSummaryState (true when `!wasMarginCalled && runComplete`). RunSummaryUI header shows "BULL RUN!" for victory variant.
- Task 6: Added `ActConfig` class and `GameConfig.Acts[]` array with act-to-tier-to-round mappings. Added `TotalRounds`, `TotalActs`, `RoundsPerAct` constants.

### Change Log
- 2026-02-11: Implemented all 6 tasks for Story 4.5 (Round Transition)
- 2026-02-11: Code review fixes (8 issues resolved): ShopState publishes ActTransitionEvent on act change + position assertion; RunContext methods use GameConfig constants instead of magic numbers; PrepareForNextRound delegates to AdvanceRound; RunSummaryUI.GetHeaderText made pure; removed unused color fields; reset static IsShowing on Initialize

### File List
- `Assets/Scripts/Runtime/Core/GameEvents.cs` (modified — added RoundCompletedEvent, ActTransitionEvent)
- `Assets/Scripts/Runtime/Core/RunContext.cs` (modified — added AdvanceRound, GetActForRound, GetTierForAct, IsRunComplete)
- `Assets/Scripts/Runtime/Core/GameStates/MarginCallState.cs` (modified — publishes RoundCompletedEvent on pass)
- `Assets/Scripts/Runtime/Core/GameStates/ShopState.cs` (modified — uses AdvanceRound, handles run completion)
- `Assets/Scripts/Runtime/Core/GameStates/RunSummaryState.cs` (modified — added IsVictory accessor)
- `Assets/Scripts/Runtime/UI/RoundResultsUI.cs` (new)
- `Assets/Scripts/Runtime/UI/ActTransitionUI.cs` (new)
- `Assets/Scripts/Runtime/UI/RunSummaryUI.cs` (modified — victory header "BULL RUN!")
- `Assets/Scripts/Setup/Data/GameConfig.cs` (modified — added ActConfig, act/round constants)
- `Assets/Tests/Runtime/UI/RoundResultsUITests.cs` (new)
- `Assets/Tests/Runtime/UI/ActTransitionUITests.cs` (new)
- `Assets/Tests/Runtime/Core/GameEventsTests.cs` (modified — RoundCompletedEvent, ActTransitionEvent tests)
- `Assets/Tests/Runtime/Core/RunContextTests.cs` (modified — AdvanceRound, GetActForRound, GetTierForAct, IsRunComplete tests)
- `Assets/Tests/Runtime/Core/GameStates/MarginCallStateTests.cs` (modified — RoundCompletedEvent publish tests)
- `Assets/Tests/Runtime/Core/GameStates/ShopStateTests.cs` (modified — round advancement, run completion, act boundary tests)
- `Assets/Tests/Runtime/Core/GameStates/RunSummaryStateTests.cs` (modified — victory/IsVictory tests)
- `Assets/Tests/Runtime/UI/RunSummaryUITests.cs` (modified — BULL RUN! header test)
- `Assets/Tests/Runtime/PriceEngine/GameConfigTests.cs` (modified — ActConfig, run structure tests)
