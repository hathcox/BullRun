# Story 6.3: Escalating Profit Targets

Status: ready-for-dev

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

- [ ] Task 1: Finalize MarginCallTargets data class (AC: 1, 2, 4)
  - [ ] Verify all 8 round targets are populated per GDD Section 2.3
  - [ ] Add `ScalingMultiplier` per round for reference/tuning documentation
  - [ ] Ensure values are `public static readonly` for easy tuning
  - [ ] Method: `GetAllTargets()` — returns array for debug display
  - [ ] File: `Scripts/Setup/Data/MarginCallTargets.cs` (verify/extend from Story 3.2)
- [ ] Task 2: Verify target display in Market Open (AC: 3)
  - [ ] MarketOpenUI (Story 4.2) already shows target — verify it reads from MarginCallTargets correctly
  - [ ] Target should be prominent: large text, clear format "$600 TARGET"
  - [ ] File: `Scripts/Runtime/UI/MarketOpenUI.cs` (verify)
- [ ] Task 3: Verify target display on Trading HUD (AC: 3)
  - [ ] TradingHUD (Story 3.2) already shows target with progress bar — verify correctness
  - [ ] Progress bar should fill based on `roundProfit / target`
  - [ ] File: `Scripts/Runtime/UI/TradingHUD.cs` (verify)
- [ ] Task 4: Implement F3 debug skip-to-round (AC: 5)
  - [ ] F3 opens a simple round selector (1-8)
  - [ ] Jumping to round N: sets RunContext to correct act/tier/round, gives appropriate starting cash
  - [ ] Starting cash for debug jump: approximate expected cash at that round based on compounding
  - [ ] Suggested debug cash: Round 1=$1000, Round 3=$2000, Round 5=$4000, Round 7=$8000
  - [ ] File: `Scripts/Editor/DebugManager.cs` (extend)
- [ ] Task 5: Add debug cash table to GameConfig (AC: 5)
  - [ ] Define `DebugStartingCash` per round for F3 jumps
  - [ ] File: `Scripts/Setup/Data/GameConfig.cs` (extend)

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

### Debug Log References

### Completion Notes List

### File List
