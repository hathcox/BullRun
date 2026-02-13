# Story 6.3: Escalating Profit Targets

Status: done

## Story

As a player,
I want profit targets that increase across rounds with clear scaling,
so that difficulty ramps predictably and I understand the challenge ahead.

## Acceptance Criteria

1. Profit targets follow the GDD table: $200, $350, $600, $900, $1500, $2200, $3500, $5000
2. Scaling multipliers increase per act: 1.0x (Act 1), 1.5x (Act 2), 2.0x (Act 3), 2.5-3.0x (Act 4)
3. Targets are displayed during Market Open phase and on the Trading HUD
4. Targets are configurable for balance tuning without code changes to logic
5. Debug mode (F3) allows jumping to any round with the correct target applied
6. Target difficulty curve feels fair: achievable in early rounds, demanding in later rounds

## Tasks / Subtasks

- [x] Task 1: Finalize MarginCallTargets data class (AC: 1, 2, 4)
  - [x] Verify all 8 round targets are populated per GDD Section 2.3
  - [x] Add `ScalingMultiplier` per round for reference/tuning documentation
  - [x] Ensure values are `public static readonly` for easy tuning
  - [x] Method: `GetAllTargets()` — returns array for debug display
  - [x] File: `Scripts/Setup/Data/MarginCallTargets.cs` (verify/extend from Story 3.2)
- [x] Task 2: Verify target display in Market Open (AC: 3)
  - [x] MarketOpenUI (Story 4.2) already shows target — verify it reads from MarginCallTargets correctly
  - [x] Target should be prominent: large text, clear format "$600 TARGET"
  - [x] File: `Scripts/Runtime/UI/MarketOpenUI.cs` (verify)
- [x] Task 3: Verify target display on Trading HUD (AC: 3)
  - [x] TradingHUD (Story 3.2) already shows target with progress bar — verify correctness
  - [x] Progress bar should fill based on `roundProfit / target`
  - [x] File: `Scripts/Runtime/UI/TradingHUD.cs` (verify)
- [x] Task 4: Implement F3 debug skip-to-round (AC: 5)
  - [x] F3 opens a simple round selector (1-8)
  - [x] Jumping to round N: sets RunContext to correct act/tier/round, gives appropriate starting cash
  - [x] Starting cash for debug jump: approximate expected cash at that round based on compounding
  - [x] Suggested debug cash: Round 1=$1000, Round 3=$2000, Round 5=$4000, Round 7=$8000
  - [x] File: `Scripts/Editor/DebugManager.cs` (extend)
- [x] Task 5: Add debug cash table to GameConfig (AC: 5)
  - [x] Define `DebugStartingCash` per round for F3 jumps
  - [x] File: `Scripts/Setup/Data/GameConfig.cs` (extend)

## Dev Notes

### Architecture Compliance

- **Data in MarginCallTargets** — targets are pure data, logic reads them
- **Debug tools in DebugManager** — F3 skip-to-round per architecture debug tools table
- **This story is primarily verification and F3 implementation** — the target system was built in Stories 3.2 and 4.4. This story ensures everything is correct and adds the F3 debug tool.

### Target Curve Design (GDD Section 2.3)

| Round | Target | Cumulative | Scaling | Difficulty |
|-------|--------|-----------|---------|-----------|
| 1 | $200 | $200 | 1.0x | Tutorial |
| 2 | $350 | $550 | 1.0x | Easy |
| 3 | $600 | $1,150 | 1.5x | Medium |
| 4 | $900 | $2,050 | 1.5x | Medium |
| 5 | $1,500 | $3,550 | 2.0x | Hard |
| 6 | $2,200 | $5,750 | 2.0x | Hard |
| 7 | $3,500 | $9,250 | 2.5x | Expert |
| 8 | $5,000 | $14,250 | 3.0x | Final |

> "Balancing this curve is the single most important tuning task during development."

### F3 Skip-to-Round Design

The F3 debug tool is essential for testing late-game rounds without playing through early rounds every time. The debug cash amounts are rough approximations — actual balance testing will refine them.

```
F3 → popup: "Jump to Round: [1-8]"
Select Round 5 → RunContext resets to Act 3, Round 5, MidValue tier
                → Portfolio.Cash = $4,000
                → MarketOpenState entered
```

### Project Structure Notes

- Verifies/extends: `Scripts/Setup/Data/MarginCallTargets.cs`
- Verifies: `Scripts/Runtime/UI/MarketOpenUI.cs`, `Scripts/Runtime/UI/TradingHUD.cs`
- Modifies: `Scripts/Editor/DebugManager.cs`
- Modifies: `Scripts/Setup/Data/GameConfig.cs`

### References

- [Source: bull-run-gdd-mvp.md#2.3] — Margin Call target table with scaling multipliers
- [Source: bull-run-gdd-mvp.md#2.3] — "Balancing this curve is the single most important tuning task"
- [Source: bull-run-gdd-mvp.md#11.3] — Balance testing protocol
- [Source: game-architecture.md#Debug Tools] — "F3: Jump to any act/round with configurable starting cash"

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Debug Log References

- Round 1 target corrected from $50 to $200 per GDD Section 2.3
- Existing MarketOpenUI and TradingHUD target display verified correct — no changes needed
- RunContext.Portfolio setter changed from `private` to `internal` to support F3 debug jump

### Completion Notes List

- **Task 1:** Updated MarginCallTargets with correct GDD targets ($200 for Round 1, was $50). Added `ScalingMultipliers` array (1.0x/1.5x/2.0x/2.5x-3.0x per act). Made `Targets` array `public static readonly`. Added `GetAllTargets()` method. Updated all affected tests (MarginCallTargetsTests, MarginCallStateTests) to reflect new $200 Round 1 target.
- **Task 2:** Verified MarketOpenState publishes `MarginCallTargets.GetTarget(ctx.CurrentRound)` in MarketOpenEvent. MarketOpenUI displays it as 36px bold gold text with "PROFIT TARGET" label. No changes required.
- **Task 3:** Verified TradingHUD reads `MarginCallTargets.GetTarget(_runContext.CurrentRound)`, displays as `$profit / $target` format, and fills progress bar via `CalculateTargetProgress(roundProfit, target)`. No changes required.
- **Task 4:** Implemented F3 skip-to-round in DebugManager. F3 toggles an IMGUI round selector popup showing all 8 rounds with act, debug cash, and target info. JumpToRound() resets RunContext (act/round/portfolio) and transitions to MarketOpenState. Added SetGameContext() for wiring. Updated DebugSetup and GameRunner to pass game context.
- **Task 5:** Added `DebugStartingCash` array to GameConfig with 8 entries: $1000, $1500, $2000, $3000, $4000, $6000, $8000, $12000. Added tests for array size, values, positivity, and monotonic increase.

### File List

- `Assets/Scripts/Setup/Data/MarginCallTargets.cs` — Modified: corrected Round 1 target ($50→$200), added ScalingMultipliers, GetAllTargets(), made Targets public
- `Assets/Scripts/Setup/Data/GameConfig.cs` — Modified: added DebugStartingCash array
- `Assets/Scripts/Editor/DebugManager.cs` — Modified: implemented F3 skip-to-round with IMGUI selector and JumpToRound()
- `Assets/Scripts/Setup/DebugSetup.cs` — Modified: added RunContext/GameStateMachine/TradeExecutor parameters
- `Assets/Scripts/Runtime/Core/GameRunner.cs` — Modified: passes game context to DebugSetup.Execute()
- `Assets/Scripts/Runtime/Core/RunContext.cs` — Modified: Portfolio setter changed from private to internal; StartingCapital setter changed from private to internal; added class-level comment documenting internal setter rationale
- `Assets/Tests/Runtime/Trading/MarginCallTargetsTests.cs` — Modified: updated Round 1 tests to $200, added ScalingMultipliers/GetAllTargets/curve validation tests, added ScalingMultipliers sync/non-decreasing/positivity tests
- `Assets/Tests/Runtime/Core/GameStates/MarginCallStateTests.cs` — Modified: updated all $50 references to $200, corrected shortfall calculations
- `Assets/Tests/Runtime/PriceEngine/GameConfigTests.cs` — Modified: added DebugStartingCash tests
- `Assets/Tests/Runtime/Core/RunContextTests.cs` — Modified: added debug jump tests (act/round/cash/StartingCapital verification)

## Senior Developer Review (AI)

### Review Date: 2026-02-12

### Reviewer: Claude Opus 4.6 (Adversarial Code Review)

### Findings and Fixes

**H1 (HIGH): No tests for DebugManager.JumpToRound** — JumpToRound had zero test coverage. Fixed by adding 5 debug jump tests in RunContextTests.cs that verify act, round, cash, and StartingCapital correctness for all 8 rounds, plus a regression test for the RunSummary profit calculation.

**H2 (HIGH): JumpToRound didn't update StartingCapital** — After F3 jump, RunSummaryState calculated totalProfit = finalCash - $1000 (original StartingCapital) instead of finalCash - debugCash. Fixed by adding `_runContext.StartingCapital = debugCash` in DebugManager.JumpToRound and changing StartingCapital setter to `internal`.

**M1 (MEDIUM): ScalingMultipliers dead code** — Array not referenced by any logic. Added documentation comment noting it's GDD reference data, and added 3 validation tests (same length as Targets, non-decreasing across acts, all positive) to catch drift.

**M2 (MEDIUM): Portfolio/StartingCapital internal setters** — Widened access for debug tool. Added class-level documentation explaining why internal setters exist and that production code should use AdvanceRound/PrepareForNextRound/ResetForNewRun.

**L1 (LOW): JumpToRound skips lifecycle events** — Noted, no current subscribers. Accepted for debug-only tool.

**L2 (LOW): AC4 "configurable without code changes"** — Consistent with project's static config pattern. Accepted.

### Files Modified by Review

- `Assets/Scripts/Editor/DebugManager.cs` — Added `_runContext.StartingCapital = debugCash` (H2 fix)
- `Assets/Scripts/Runtime/Core/RunContext.cs` — StartingCapital setter to internal, added class comment (H2, M2 fix)
- `Assets/Scripts/Setup/Data/MarginCallTargets.cs` — Added documentation comment on ScalingMultipliers (M1 fix)
- `Assets/Tests/Runtime/Core/RunContextTests.cs` — Added 5 debug jump tests (H1 fix)
- `Assets/Tests/Runtime/Trading/MarginCallTargetsTests.cs` — Added 3 ScalingMultipliers validation tests (M1 fix)

## Change Log

- 2026-02-12: Story 6.3 implemented. Corrected Round 1 profit target from $50 to $200 per GDD. Added scaling multipliers and GetAllTargets() to MarginCallTargets. Added DebugStartingCash to GameConfig. Implemented F3 skip-to-round debug tool in DebugManager with IMGUI round selector.
- 2026-02-12: Code review fixes applied. Fixed StartingCapital bug in JumpToRound (H2). Added debug jump tests (H1). Added ScalingMultipliers validation tests and documentation (M1). Documented internal setter rationale (M2). Story status → done.
