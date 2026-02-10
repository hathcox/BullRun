# Story 3.2: Trading HUD

Status: ready-for-dev

## Story

As a player,
I want to see my cash, portfolio value, round profit, and margin call target updating in real-time,
so that I know my status at a glance during trading.

## Acceptance Criteria

1. Top bar displays: Cash Available, Total Portfolio Value (with % change), Current Round Profit, Margin Call Target (with progress bar)
2. All values update in real-time as prices change and trades execute
3. Clear visual distinction between profit (green) and loss (red) states
4. Margin call target shows a progress bar indicating how close the player is to the target
5. Numbers use monospace font for stable layout as digits change
6. HUD does not obscure the chart area

## Tasks / Subtasks

- [ ] Task 1: Create TradingHUD MonoBehaviour (AC: 1, 2)
  - [ ] Subscribe to `PriceUpdatedEvent` and `TradeExecutedEvent` to trigger UI refresh
  - [ ] Read from `RunContext.Portfolio` for cash, positions, round profit
  - [ ] Read margin call target from `MarginCallTargets.GetTarget(currentRound)` (data class)
  - [ ] File: `Scripts/Runtime/UI/TradingHUD.cs`
- [ ] Task 2: Build top bar layout (AC: 1, 5, 6)
  - [ ] Four sections in a horizontal layout group:
    - Cash: "$X,XXX.XX" with dollar icon
    - Portfolio Value: "$X,XXX.XX (±X.X%)" with up/down arrow
    - Round Profit: "+$XXX.XX" or "-$XXX.XX"
    - Target: "$X,XXX / $X,XXX" with progress bar fill
  - [ ] Use TextMeshPro with monospace font for stable number widths
  - [ ] Position at top of screen, full width, above chart area
  - [ ] File: `Scripts/Runtime/UI/TradingHUD.cs`
- [ ] Task 3: Implement profit/loss color coding (AC: 3)
  - [ ] Round Profit text: green when positive, red when negative, white when zero
  - [ ] Portfolio % change: green arrow up / red arrow down
  - [ ] Margin target progress bar: green when on track, yellow when close, red when behind
  - [ ] Threshold for yellow: <50% of target met with >50% time elapsed
  - [ ] File: `Scripts/Runtime/UI/TradingHUD.cs`
- [ ] Task 4: Create MarginCallTargets data class (AC: 4)
  - [ ] Define per-round profit targets from GDD Section 2.3 table
  - [ ] Method: `GetTarget(int roundNumber)` — returns float target for that round
  - [ ] Values: $200, $350, $600, $900, $1500, $2200, $3500, $5000
  - [ ] File: `Scripts/Setup/Data/MarginCallTargets.cs`
- [ ] Task 5: Add HUD to UISetup (AC: 6)
  - [ ] Generate TradingHUD Canvas elements in UISetup
  - [ ] Position top bar above chart area with proper anchoring
  - [ ] File: `Scripts/Setup/UISetup.cs` (create or extend)

## Dev Notes

### Architecture Compliance

- **Location:** UI code in `Scripts/Runtime/UI/TradingHUD.cs`
- **One-way dependency:** TradingHUD reads from Portfolio and data classes. Never writes to them.
- **EventBus subscription:** Refresh on `PriceUpdatedEvent` and `TradeExecutedEvent`
- **Data access:** `MarginCallTargets.GetTarget(round)` — direct static class access, no DI
- **Setup Framework:** UISetup generates the Canvas hierarchy. TradingHUD is a MonoBehaviour attached to generated objects.
- **uGUI:** All layout via Canvas + LayoutGroups, created programmatically

### Number Formatting

Use consistent formatting for financial values:
- Cash/Portfolio/Profit: `$1,234.56` format with commas and 2 decimal places
- Percentage: `+12.3%` or `-5.7%` with sign
- Use `ToString("N2")` for currency, `ToString("P1")` for percentages
- Monospace font prevents layout jitter as numbers change

### Margin Call Target Progress

The progress bar for the margin call target is critical UX — it's the primary "am I winning?" indicator:

```
Target: $600
Current Round Profit: $380
Progress: ████████░░░░ 63%
```

Color the bar fill based on time-weighted progress:
- Green: on pace (profit/target >= timeElapsed/totalTime)
- Yellow: falling behind
- Red: significantly behind or negative profit

### GDD Margin Call Targets (Section 2.3)

| Round | Target | Act |
|-------|--------|-----|
| 1 | $200 | Act 1 |
| 2 | $350 | Act 1 |
| 3 | $600 | Act 2 |
| 4 | $900 | Act 2 |
| 5 | $1,500 | Act 3 |
| 6 | $2,200 | Act 3 |
| 7 | $3,500 | Act 4 |
| 8 | $5,000 | Act 4 |

### Project Structure Notes

- Creates: `Scripts/Runtime/UI/TradingHUD.cs`
- Creates: `Scripts/Setup/Data/MarginCallTargets.cs`
- Creates or extends: `Scripts/Setup/UISetup.cs`
- Depends on: Portfolio (Epic 2), PriceUpdatedEvent/TradeExecutedEvent (Epics 1-2)

### References

- [Source: game-architecture.md#Project Structure] — UI/ folder: TradingHUD.cs
- [Source: game-architecture.md#Why uGUI Over UI Toolkit] — Canvas system for dense real-time HUD
- [Source: game-architecture.md#Architectural Boundaries] — "UI classes read from runtime systems (one-way dependency)"
- [Source: game-architecture.md#Data Access] — Direct static class access pattern
- [Source: game-architecture.md#Configuration] — MarginCallTargets.cs in Scripts/Setup/Data/
- [Source: bull-run-gdd-mvp.md#6.1] — "Top Bar: Cash available | Total Portfolio Value | Current Round Profit | Margin Call Target"
- [Source: bull-run-gdd-mvp.md#2.3] — Margin call target table with per-round values
- [Source: bull-run-gdd-mvp.md#7.1] — Monospace numbers, synthwave palette

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
