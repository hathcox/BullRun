# Story FIX-16: UI Position Display Fix & Center Notifications

Status: done

## Story

As a player,
I want to always see how many shares I hold and my current P&L clearly on screen, and have trade notifications appear in the center of the screen instead of the top,
so that I never lose track of my position during fast-paced trading and feedback is always in my field of view.

## Problem Analysis

The current trading UI has two visibility problems:

**1. Position Overlay is small and low on screen:**
- `PositionOverlay` sits at bottom-center above the trade panel (y=144px from bottom), sortingOrder 24
- Size: 420x85px with 16pt direction text, 13pt avg price, 14pt P&L
- During fast trading at 60fps, players must glance away from the chart area to check their position
- The overlay is semi-transparent (alpha 0.6) and can blend into background during volatile moments

**2. Trade feedback notifications are small and at the top of the screen:**
- `TradeFeedback` is positioned below the top bar (sortingOrder 23), anchored to top-center
- Size: only 300x30px with 18pt text
- During rapid trading, feedback disappears before the player notices (1.5s display + 0.5s fade)
- Player's eyes are focused on the chart (center) and trade buttons (bottom) — top-bar feedback is outside the field of view

**What works well (no changes needed):**
- `EventPopup` is already center-screen with dramatic pause — this is fine
- `NewsBanner` at top-left is supplementary info, not critical feedback
- `TradingHUD` top bar shows portfolio-level metrics (cash, portfolio value, round profit, target)

**Design Intent:** Move trade feedback notifications to center screen where the player is already looking. Make the position overlay more prominent so position status is always clear at a glance.

## Acceptance Criteria

1. Trade feedback notifications (`TradeFeedback`) appear at center screen instead of below the top bar
2. Trade feedback panel size increased to at least 400x50px for better readability during fast trading
3. Trade feedback text increased to at least 22pt bold for visibility
4. Trade feedback background uses higher opacity (0.85 alpha) for contrast against chart
5. Position overlay (`PositionOverlay`) relocated to bottom-left of screen (left-aligned, above inventory panel area) for persistent visibility without obscuring chart center
6. Position overlay direction text increased to at least 20pt for at-a-glance readability
7. Position overlay P&L text increased to at least 18pt bold
8. Position overlay background opacity increased to 0.75 alpha for better contrast
9. All existing color coding preserved — green/red P&L, green buy/cyan sell/pink short/red failure feedback colors unchanged
10. Trade feedback does NOT block input (no GraphicRaycaster) — matches current behavior
11. Trade feedback clears on `TradingPhaseEndedEvent` — matches current behavior
12. Position overlay still updates in real-time via dirty flag pattern on `PriceUpdatedEvent` and `TradeExecutedEvent`
13. No visual overlap between centered trade feedback and the existing center-screen `EventPopup` (different sortingOrders, EventPopup pauses game so no simultaneous display)
14. All existing `TradeFeedbackTests` continue to pass — static utility methods unchanged

## Tasks / Subtasks

- [x] Task 1: Move TradeFeedback to center screen (AC: 1, 2, 3, 4, 10, 11, 13)
  - [x] 1.1: In `UISetup.ExecuteTradeFeedback()`, change container anchor from top-center (0.5, 1.0) to center-center (0.5, 0.5)
  - [x] 1.2: Update `anchoredPosition` from `(0, -(TopBarHeight + 8))` to `(0, 0)` for true center
  - [x] 1.3: Increase `sizeDelta` from `(300, 30)` to `(420, 50)` for larger feedback area
  - [x] 1.4: Increase feedback text font size from 18pt to 24pt bold
  - [x] 1.5: Increase background opacity — change `BarBackgroundColor` usage to explicit `new Color(0.05f, 0.07f, 0.18f, 0.85f)`
  - [x] 1.6: Keep sortingOrder at 23 (below EventPopup canvas which is higher) to avoid overlap conflict
  - [x] 1.7: Verify no GraphicRaycaster on feedback canvas (already absent — confirm preserved)

- [x] Task 2: Reposition PositionOverlay to bottom-left (AC: 5, 6, 7, 8, 12)
  - [x] 2.1: In `UISetup.ExecutePositionOverlay()`, change container anchor from bottom-center (0.5, 0) to bottom-left (0, 0)
  - [x] 2.2: Update `pivot` from `(0.5, 0)` to `(0, 0)` for left-aligned anchoring
  - [x] 2.3: Update `anchoredPosition` to `(12, 144)` — left margin of 12px, same height above trade panel
  - [x] 2.4: Increase direction text font size from 16pt to 20pt bold
  - [x] 2.5: Increase P&L text font size from 14pt to 18pt bold
  - [x] 2.6: Increase avg price text font size from 13pt to 15pt
  - [x] 2.7: Increase background opacity from 0.6 to 0.75 — `new Color(0.05f, 0.07f, 0.18f, 0.75f)`
  - [x] 2.8: Adjust container `sizeDelta` width from 420 to 280 (narrower since left-aligned, not centered)
  - [x] 2.9: Verify dirty flag pattern and EventBus subscriptions unchanged in `PositionOverlay.cs`

- [x] Task 3: Write tests and validate (AC: 9, 14)
  - [x] 3.1: Run existing `TradeFeedbackTests` to confirm all static utility methods still pass
  - [x] 3.2: Add `PositionOverlayTests` — test `GetPnLColor`, `FormatDirection`, `FormatFlat` static methods (if not already tested)
  - [x] 3.3: Run full test suite to confirm no regressions

## Dev Notes

### Architecture Compliance

- **Setup-Oriented Generation:** All changes are in `UISetup.cs` (setup-time Canvas creation). No Inspector changes.
- **EventBus:** No new events needed. Existing subscriptions (`TradeFeedbackEvent`, `PriceUpdatedEvent`, `TradeExecutedEvent`, `RoundStartedEvent`, `TradingPhaseEndedEvent`) are unchanged.
- **No Inspector Config:** All positioning, sizing, and color values are code constants in `UISetup.cs`
- **Modification-Only Change:** No new files created, no files deleted. Only `UISetup.cs` positioning/sizing constants change.

### Key Design Decisions

1. **Center-screen trade feedback:** Player eyes are on chart center + trade buttons at bottom. Centering feedback puts it in the natural gaze path. The existing `EventPopup` also uses center-screen but pauses the game (Time.timeScale = 0), so there's no conflict — trade feedback only appears during active gameplay.

2. **Bottom-left position overlay:** Moving from bottom-center to bottom-left keeps the position info near the player's peripheral view of the chart without competing with the center-screen trade feedback. The trade panel buttons remain at bottom-center.

3. **Size increases:** The current 300x30px feedback and 16pt position text were designed for a more relaxed trading pace. With 60-second rounds and single-stock focus, larger text reduces cognitive load during fast decisions.

### Existing Code to Understand

Before implementing, the dev agent MUST read:
- `Assets/Scripts/Setup/UISetup.cs:312-384` — `ExecutePositionOverlay()` current layout
- `Assets/Scripts/Setup/UISetup.cs:1249-1289` — `ExecuteTradeFeedback()` current layout
- `Assets/Scripts/Runtime/UI/PositionOverlay.cs` — display logic (DO NOT modify business logic)
- `Assets/Scripts/Runtime/UI/TradeFeedback.cs` — feedback logic (DO NOT modify business logic)
- `Assets/Scripts/Runtime/UI/EventPopup.cs` — understand center-screen popup to avoid conflicts

### Canvas Sorting Order (Reference)

```
30:  NewsBanner (top)
25:  RoundTimer (top-right)
24:  PositionOverlay + TradePanel (bottom)  <-- PositionOverlay moves to bottom-left
23:  TradeFeedback (currently below top bar) <-- moves to center screen
22:  NewsTicker (bottom)
21:  ItemInventoryPanel (bottom)
20:  TradingHUD (top)
```

EventPopup sorting order is separate canvas, higher than 23. No overlap risk.

### Depends On

- FIX-15 (Remove Multi-Stock) must be complete — it IS (status: review). Single stock confirmed.
- FIX-7 (Position Overlay) original implementation — already in codebase
- No new dependencies required

### Risk

- **Low risk** — only changing positioning constants and font sizes in `UISetup.cs`
- **No business logic changes** — `PositionOverlay.cs` and `TradeFeedback.cs` runtime code untouched
- **Regression concern:** Minimal — only layout shifts, all functional behavior preserved
- **Visual concern:** Center-screen feedback could feel intrusive. Mitigated by keeping 1.5s display + 0.5s fade timing and semi-transparent background.

### Project Structure Notes

- All changes confined to `Assets/Scripts/Setup/UISetup.cs` — existing file, setup-time only
- Tests in `Assets/Tests/Runtime/UI/TradeFeedbackTests.cs` — existing file
- New test file: `Assets/Tests/Runtime/UI/PositionOverlayTests.cs` (if static methods not already tested)
- Alignment with project structure: changes are in Setup layer only, Runtime layer untouched

### References

- [Source: Assets/Scripts/Setup/UISetup.cs#ExecutePositionOverlay] — current overlay layout
- [Source: Assets/Scripts/Setup/UISetup.cs#ExecuteTradeFeedback] — current feedback layout
- [Source: Assets/Scripts/Runtime/UI/PositionOverlay.cs] — overlay display logic
- [Source: Assets/Scripts/Runtime/UI/TradeFeedback.cs] — feedback display logic
- [Source: Assets/Scripts/Runtime/UI/EventPopup.cs] — center-screen popup (reference for no-conflict)
- [Source: _bmad-output/implementation-artifacts/FIX-15-remove-multi-stock.md] — previous story context

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (claude-opus-4-6)

### Debug Log References

### Completion Notes List

- All 14 acceptance criteria satisfied via layout-only changes in `UISetup.cs`
- TradeFeedback moved from top-center (below top bar) to center-center screen; size increased to 420x50px, text to 24pt bold, background to 0.85 alpha
- PositionOverlay moved from bottom-center to bottom-left; size narrowed to 280px, direction text to 20pt, P&L to 18pt, avg price to 15pt, background to 0.75 alpha
- No runtime logic modified — `TradeFeedback.cs` and `PositionOverlay.cs` untouched
- No new events, no new files, no new dependencies
- Full test suite: 1403 passed, 0 failed, 1 skipped (pre-existing skip)
- Existing `TradeFeedbackTests` (13 tests) and `PositionOverlayTests` (10 tests) all pass — both files already existed

### Change Log

- 2026-02-16: FIX-16 story created with comprehensive context from codebase analysis
- 2026-02-16: FIX-16 implemented — TradeFeedback centered, PositionOverlay moved to bottom-left, font sizes and opacity increased
- 2026-02-16: Code review — fixed PositionOverlay container height (85→112px) for enlarged text, fixed misleading comment, corrected test counts in notes

### File List

- Assets/Scripts/Setup/UISetup.cs (modified — ExecuteTradeFeedback and ExecutePositionOverlay layout constants)

## Senior Developer Review (AI)

**Review Date:** 2026-02-16
**Reviewer Model:** Claude Opus 4.6 (claude-opus-4-6)
**Review Outcome:** Approve (with fixes applied)

### Findings Summary

| # | Severity | Description | Status |
|---|----------|-------------|--------|
| 1 | HIGH | PositionOverlay container height (85px) too small for enlarged text (~106px content) — P&L row extends past background | FIXED — height increased to 112px |
| 2 | MEDIUM | Inaccurate test counts in Completion Notes (claimed 11/9, actual 13/10) | FIXED — corrected counts |
| 3 | LOW | Misleading comment "above inventory panel area" (actually above trade panel) | FIXED — corrected comment |
| 4 | LOW | Center-screen feedback at dead center may compete with chart data — consider slight y-offset | NOT FIXED — design decision, verify visually |
| 5 | LOW | No text overflow handling on enlarged text components | NOT FIXED — low risk for typical gameplay values |

### Action Items

- [x] Fix PositionOverlay container height from 85px to 112px (UISetup.cs:336)
- [x] Correct test counts in Dev Agent Record (13 TradeFeedback, 10 PositionOverlay)
- [x] Fix misleading comment from "inventory panel area" to "trade panel area" (UISetup.cs:329)
- [ ] Visual QA: Verify center-screen feedback positioning feels right during gameplay
- [ ] Visual QA: Check text doesn't truncate with large P&L values at 18pt in 280px container
