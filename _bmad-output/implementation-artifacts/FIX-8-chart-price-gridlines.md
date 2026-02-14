# Story FIX-8: Chart Price Gridlines

Status: done

## Story

As a player,
I want horizontal lines drawn across the chart at each price label value,
so that I have a visual frame of reference for where the price is relative to key levels.

## Problem Analysis

The chart currently has Y-axis price labels on the right side (5 labels, evenly spaced from min to max price). These labels update dynamically as the price range changes. However, there are no horizontal lines connecting these labels across the chart, making it hard to judge the current price level relative to specific values like $2.75 or $2.21.

**Affected Code:**
- `Scripts/Runtime/Chart/ChartLineView.cs` — needs to render horizontal gridlines
- `Scripts/Setup/ChartSetup.cs` — needs to create gridline rendering objects
- `Scripts/Runtime/Chart/ChartUI.cs` — needs to expose price label values for gridline positioning

## Acceptance Criteria

1. Horizontal lines are drawn across the full chart width at each Y-axis price label position
2. Lines update dynamically as the price range changes (labels reposition, lines follow)
3. Lines are subtle — thin, low-opacity so they don't obscure the price line
4. Lines match the number of price labels (currently 5)
5. Lines extend from chart left edge to chart right edge
6. Lines do not render outside the chart bounds (no visual bleed)

## Tasks / Subtasks

- [x] Task 1: Create gridline rendering objects in ChartSetup (AC: 1, 4)
  - [x] Create a parent GameObject "PriceGridlines" under ChartSystem
  - [x] Create 5 LineRenderer objects (one per axis label), parented under PriceGridlines
  - [x] LineRenderer config: thin (0.005f width), low-alpha color (e.g., 0.15 alpha white/gray), sorting order 0 (behind price line)
  - [x] Use shared material (reuse `_lineMaterial` from ChartSetup)
  - [x] Pass gridline LineRenderers to ChartLineView via a new `SetGridlines()` method
  - [x] File: `Scripts/Setup/ChartSetup.cs`

- [x] Task 2: Render gridlines in ChartLineView (AC: 2, 5, 6)
  - [x] Add `private LineRenderer[] _gridlines` field
  - [x] Add `SetGridlines(LineRenderer[] gridlines)` method
  - [x] In `LateUpdate()`, after computing minPrice/maxPrice/priceRange/paddedBottom/paddedTop:
    - Calculate Y position for each gridline (evenly spaced from minPrice to maxPrice)
    - Set each LineRenderer to horizontal line from `_chartLeft` to `_chartRight` at computed Y
  - [x] When pointCount < 2 (no data), hide gridlines
  - [x] Gridline Y positions use same math as ChartUI axis labels (matching price label values)
  - [x] File: `Scripts/Runtime/Chart/ChartLineView.cs`

- [x] Task 3: Style gridlines for subtlety (AC: 3)
  - [x] Color: `new Color(0.4f, 0.4f, 0.5f, 0.15f)` — very faint gray
  - [x] Width: 0.005f world units (thin)
  - [x] Sorting order: -2 (behind glow at -1 and main line at 1)
  - [x] Consider: dashed pattern (not natively supported by LineRenderer — skip for now, solid thin line is sufficient)
  - [x] File: `Scripts/Setup/ChartSetup.cs`, `Scripts/Runtime/Chart/ChartLineView.cs`

- [x] Task 4: Verify gridline alignment with price labels (AC: 2)
  - [x] ChartUI.CalculateAxisLabels() computes evenly spaced values between min and max
  - [x] ChartLineView gridlines must use the same min/max and same Y-mapping math
  - [x] Both already use `GetLivePriceRange()` and the same padding calculation — verify they stay in sync
  - [x] File: verification, cross-reference `ChartUI.cs:79-98` with `ChartLineView.cs:113-127`

## Dev Notes

### Architecture Compliance
- **Setup-Oriented Generation:** Gridline GameObjects created in `ChartSetup.Execute()` during F5
- **No Inspector Config:** LineRenderer properties set in code
- **Performance:** LineRenderers are cheap for static horizontal lines — just 2 points each, updated once per frame

### Visual Design
- Gridlines should be barely visible — the player should notice them subconsciously
- They provide "ruler marks" so you can see "$2.50" and glance at the line to see where $2.50 is on the chart
- Color should be dimmer than the axis label text but visible against the dark chart background
- Consider matching the axis label color but at lower alpha

### Y-Position Math
Both ChartUI and ChartLineView use the same coordinate system:
```csharp
// From ChartLineView.LateUpdate():
float chartHeight = _chartTop - _chartBottom;
float padding = chartHeight * 0.1f;
float paddedBottom = _chartBottom + padding;
float paddedTop = _chartTop - padding;

// For each gridline i (0 to labelCount-1):
float priceAtLabel = minPrice + (maxPrice - minPrice) * i / (labelCount - 1);
float y = Mathf.Lerp(paddedBottom, paddedTop, (priceAtLabel - minPrice) / priceRange);
```

### Dependencies
- Independent of FIX-5, FIX-6, FIX-7
- Can be implemented in any order
- If chart bounds change (FIX-5 removes sidebar, FIX-7 removes right panel), gridlines automatically adapt since they use `_chartLeft`/`_chartRight`

### Edge Cases
- When pointCount < 2: hide gridlines (no price range to compute)
- When all prices are equal (flat line): `priceRange < 0.01f` — ChartLineView already handles this by centering +-0.5, gridlines will match
- When price range is very large: gridlines spread out naturally (evenly spaced in price, linear in Y)

## Dev Agent Record

### Implementation Plan
- Created 5 LineRenderer-based gridlines in ChartSetup using the existing `CreateLineRendererObject` helper and shared `_lineMaterial`
- Added `SetGridlines()` / `UpdateGridlines()` / `HideGridlines()` methods to ChartLineView
- Gridline Y positions calculated with `t = i / (count - 1)`, Lerped between 10%-padded chart bounds — identical math to axis label positioning
- Added static `CalculateGridlineYPositions()` method (takes pre-padded bounds) for testability
- Gridlines hidden when pointCount < 2 (no data to compute range)

### Completion Notes
- All 4 tasks implemented and verified; code review fixes applied
- AC1: Horizontal lines drawn at each axis label Y position via 5 LineRenderers
- AC2: Lines update every frame in LateUpdate using live min/max price range
- AC3: Lines styled subtle — Color(0.4, 0.4, 0.5, 0.15), width 0.005f, sortingOrder -2 (behind glow and main line)
- AC4: 5 gridlines match 5 axis labels (AxisLabelCount)
- AC5: Lines span _chartLeft to _chartRight (full chart width)
- AC6: Lines positioned within padded bounds, never outside chart area
- 8 unit tests covering Y-position math, gridline-to-axis-label alignment, and edge cases (count=0)

### Debug Log
No issues encountered during implementation.

## File List

- `Assets/Scripts/Setup/ChartSetup.cs` — Modified: added PriceGridlines parent + 5 LineRenderer creation + SetGridlines() call
- `Assets/Scripts/Runtime/Chart/ChartLineView.cs` — Modified: added _gridlines field, SetGridlines(), UpdateGridlines(), HideGridlines(), CalculateGridlineYPositions() static method
- `Assets/Tests/Runtime/Chart/ChartGridlineTests.cs` — New: 8 unit tests for gridline Y-position calculation and axis label alignment

## Senior Developer Review (AI)

**Reviewer:** Iggy | **Date:** 2026-02-13

### Findings (4 total: 1 High, 1 Medium, 2 Low)

**Fixed (1 High, 1 Medium):**
- H1: Story status was "done" and sprint-status said "done" but all code changes were uncommitted/unstaged in git. Corrected status to "review".
- M5: Gridline GameObjects were created active (inconsistent with break-even line which is created with `SetActive(false)`). Added `gridlineGo.SetActive(false)` after creation — `UpdateGridlines()` activates them on first LateUpdate.

**Not Fixed (2 Low — acceptable):**
- L3: Problem Analysis mentions ChartUI.cs modification but it wasn't needed (`CalculateAxisLabels()` was already public). Minor documentation inconsistency.
- L1 (shared): StockId type inconsistency is project-wide, not specific to FIX-8.

**Known Gap:**
- Tests only cover static `CalculateGridlineYPositions()` method. Runtime `UpdateGridlines()`/`HideGridlines()` behavior with LineRenderer objects not tested (requires PlayMode tests).

### Outcome: Changes Applied

## Change Log

- 2026-02-13: Implemented chart price gridlines (FIX-8) — 5 horizontal reference lines at axis label positions, updated dynamically per frame
- 2026-02-13: Code review fixes — sortingOrder -2 (behind glow), CalculateGridlineYPositions API aligned with runtime (pre-padded bounds), activeSelf guard, added count=0 edge test
- 2026-02-13: Code review #2 fixes — status corrected to "review" (was prematurely "done"), gridlines created inactive for consistency

## References
- `Scripts/Setup/ChartSetup.cs:19-131` — chart system creation
- `Scripts/Runtime/Chart/ChartLineView.cs:97-157` — LateUpdate rendering, coordinate math
- `Scripts/Runtime/Chart/ChartUI.cs:79-98` — axis label calculation
- `Scripts/Runtime/Chart/ChartUI.cs:159-174` — `CalculateAxisLabels()` static method
