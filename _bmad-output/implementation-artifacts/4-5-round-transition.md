# Story 4.5: Round Transition

Status: ready-for-dev

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

- [ ] Task 1: Create round results display (AC: 1, 2)
  - [ ] Brief overlay after margin call passes: "ROUND 3 COMPLETE"
  - [ ] Show: Round Profit (+$650), Target ($600 — PASSED), Total Cash ($2,800)
  - [ ] Display for 2-3 seconds or until player presses continue
  - [ ] Green checkmark or success indicator
  - [ ] File: `Scripts/Runtime/UI/RoundResultsUI.cs` (new)
- [ ] Task 2: Implement round/act progression in RunContext (AC: 4)
  - [ ] Method: `AdvanceRound()` — increments round, checks act boundary
  - [ ] Act boundaries: Rounds 1-2 = Act 1, 3-4 = Act 2, 5-6 = Act 3, 7-8 = Act 4
  - [ ] Method: `GetCurrentAct()` — derives act from round number
  - [ ] Method: `GetCurrentTier()` — maps act to StockTier enum
  - [ ] Method: `IsRunComplete()` — returns true after Round 8
  - [ ] File: `Scripts/Runtime/Core/RunContext.cs` (extend)
- [ ] Task 3: Wire complete game loop transitions (AC: 3, 5, 6)
  - [ ] MarketCloseState → (margin call pass) → show results → ShopState
  - [ ] ShopState → (shop done) → AdvanceRound → MarketOpenState (next round)
  - [ ] If `RunContext.IsRunComplete()`: → RunSummaryState with win flag
  - [ ] MetaHubState → (start run) → RunContext.StartNewRun() → MarketOpenState (Round 1)
  - [ ] File: multiple GameStates files (extend)
- [ ] Task 4: Create act transition indicator (AC: 4)
  - [ ] When act changes (e.g., Round 2 → Round 3 = Act 1 → Act 2):
  - [ ] Show "ACT 2 — LOW-VALUE STOCKS" interstitial screen before MarketOpen
  - [ ] Brief dramatic reveal (1-2 seconds) of the new tier
  - [ ] File: `Scripts/Runtime/UI/ActTransitionUI.cs` (new)
- [ ] Task 5: Win state in RunSummary (AC: 5)
  - [ ] RunSummaryState already exists from Story 4.4
  - [ ] Add win path: if `!wasMarginCalled && runComplete`, show victory variant
  - [ ] Victory header: "RUN COMPLETE — BULL RUN!" or similar
  - [ ] Show final stats: total profit across all 8 rounds, peak cash, items used
  - [ ] File: `Scripts/Runtime/Core/GameStates/RunSummaryState.cs` (extend)
  - [ ] File: `Scripts/Runtime/UI/RunSummaryUI.cs` (extend)
- [ ] Task 6: Add act/round config mappings (AC: 4)
  - [ ] Define act-to-tier mapping and round-to-act mapping in GameConfig or new data class
  - [ ] `ActConfig`: act number, tier, round range, display name
  - [ ] File: `Scripts/Setup/Data/GameConfig.cs` (extend)

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

### Debug Log References

### Completion Notes List

### File List
