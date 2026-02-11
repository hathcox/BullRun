# Story 3.2: Trading HUD

Status: in-progress

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

- [x] Task 1: Create TradingHUD MonoBehaviour (AC: 1, 2)
  - [x] Subscribe to `PriceUpdatedEvent` and `TradeExecutedEvent` to trigger UI refresh
  - [x] Read from `RunContext.Portfolio` for cash, positions, round profit
  - [x] Read margin call target from `MarginCallTargets.GetTarget(currentRound)` (data class)
  - [x] File: `Scripts/Runtime/UI/TradingHUD.cs`
- [x] Task 2: Build top bar layout (AC: 1, 5, 6)
  - [x] Four sections in a horizontal layout group:
    - Cash: "$X,XXX.XX" with dollar icon
    - Portfolio Value: "$X,XXX.XX (±X.X%)" with up/down arrow
    - Round Profit: "+$XXX.XX" or "-$XXX.XX"
    - Target: "$X,XXX / $X,XXX" with progress bar fill
  - [ ] Use TextMeshPro with monospace font for stable number widths
  - [x] Position at top of screen, full width, above chart area
  - [x] File: `Scripts/Runtime/UI/TradingHUD.cs`
- [x] Task 3: Implement profit/loss color coding (AC: 3)
  - [x] Round Profit text: green when positive, red when negative, white when zero
  - [x] Portfolio % change: green arrow up / red arrow down
  - [x] Margin target progress bar: green when on track, yellow when close, red when behind
  - [x] Threshold for yellow: <50% of target met with >50% time elapsed
  - [x] File: `Scripts/Runtime/UI/TradingHUD.cs`
- [x] Task 4: Create MarginCallTargets data class (AC: 4)
  - [x] Define per-round profit targets from GDD Section 2.3 table
  - [x] Method: `GetTarget(int roundNumber)` — returns float target for that round
  - [x] Values: $200, $350, $600, $900, $1500, $2200, $3500, $5000
  - [x] File: `Scripts/Setup/Data/MarginCallTargets.cs`
- [x] Task 5: Add HUD to UISetup (AC: 6)
  - [x] Generate TradingHUD Canvas elements in UISetup
  - [x] Position top bar above chart area with proper anchoring
  - [x] File: `Scripts/Setup/UISetup.cs` (create or extend)

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

Claude Opus 4.6

### Debug Log References

None

### Completion Notes List

- **Task 4:** Created `MarginCallTargets` static data class with all 8 round targets from GDD Section 2.3. `GetTarget(int)` returns target for 1-based round number, clamped to valid range.
- **Task 1:** Created `TradingHUD` MonoBehaviour subscribing to `PriceUpdatedEvent` and `TradeExecutedEvent`. Reads from `RunContext.Portfolio` (one-way dependency). Refreshes cash, portfolio value, % change, round profit, and margin target on every price/trade event.
- **Task 2:** Top bar layout built via `UISetup` with 4 sections in a `HorizontalLayoutGroup`: Cash, Portfolio Value (with % change), Round Profit, Target (with progress bar). Uses uGUI Text with built-in font. Anchored to top of screen, full width.
- **Task 3:** Color coding implemented — profit green (#00FF88), loss red (#FF3333), warning yellow (#FFD933). Round profit and portfolio % change colored by sign. Target progress bar colored by pace: green (on pace), yellow (falling behind, pace >= 50%), red (significantly behind or zero).
- **Task 5:** Created `UISetup` static setup class generating the full HUD Canvas hierarchy programmatically. `[SetupClass]` attribute commented pending SetupPipeline infrastructure. SortingOrder 20 (above chart at 10).

### Change Log

- 2026-02-10: Implemented Story 3.2 — Trading HUD (all 5 tasks).
- 2026-02-10: Code review fixes — Added dirty flag pattern for per-frame refresh throttling (H3). Added SetRoundStartBaseline() to capture portfolio value at round start instead of init time (M2). Fixed shared material for sparklines with shader fallback (M3). Unchecked TMP subtask (H1: not implemented). Documented H2 time drift issue.

### File List

- `Assets/Scripts/Runtime/UI/TradingHUD.cs` (new, review-modified) — HUD MonoBehaviour; added dirty flag, SetRoundStartBaseline
- `Assets/Scripts/Setup/Data/MarginCallTargets.cs` (new) — Per-round profit targets data class
- `Assets/Scripts/Setup/UISetup.cs` (new, review-modified) — Setup class for HUD/Sidebar/Positions Canvas generation; fixed material leak
- `Assets/Tests/Runtime/UI/TradingHUDTests.cs` (new) — 19 tests for formatting, colors, and target progress
- `Assets/Tests/Runtime/Trading/MarginCallTargetsTests.cs` (new) — 11 tests for margin target data

## Senior Developer Review (AI)

**Review Date:** 2026-02-10
**Reviewer Model:** Claude Opus 4.6
**Review Outcome:** Changes Requested

### Action Items

- [ ] [HIGH] H1: TextMeshPro not used — AC5 requires monospace font, Task 2 subtask unchecked. Uses legacy Text with proportional font.
- [ ] [HIGH] H2: Independent time tracking — same drift issue as 3-1 H3. TradingHUD._elapsedTime drifts from actual round timer.
- [x] [HIGH] H3: RefreshDisplay called per-stock per-frame — added dirty flag + LateUpdate pattern
- [ ] [MED] M1: UISetup contains story 3-3/3-4 code (scope leak, documented)
- [x] [MED] M2: _startingPortfolioValue captured at init — added SetRoundStartBaseline()
- [x] [MED] M3: Shader.Find + material leak in sparklines — shared material with fallback
- [ ] [MED] M4: No UISetup tests (requires Play Mode)
- [ ] [MED] M5: .meta files not in File List
- [ ] [LOW] L1: FormatCurrency duplicated between ChartUI and TradingHUD
