# Story FIX-8: Chart Price Gridlines

Status: pending

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

- [ ] Task 1: Create gridline rendering objects in ChartSetup (AC: 1, 4)
  - [ ] Create a parent GameObject "PriceGridlines" under ChartSystem
  - [ ] Create 5 LineRenderer objects (one per axis label), parented under PriceGridlines
  - [ ] LineRenderer config: thin (0.005f width), low-alpha color (e.g., 0.15 alpha white/gray), sorting order 0 (behind price line)
  - [ ] Use shared material (reuse `_lineMaterial` from ChartSetup)
  - [ ] Pass gridline LineRenderers to ChartLineView via a new `SetGridlines()` method
  - [ ] File: `Scripts/Setup/ChartSetup.cs`

- [ ] Task 2: Render gridlines in ChartLineView (AC: 2, 5, 6)
  - [ ] Add `private LineRenderer[] _gridlines` field
  - [ ] Add `SetGridlines(LineRenderer[] gridlines)` method
  - [ ] In `LateUpdate()`, after computing minPrice/maxPrice/priceRange/paddedBottom/paddedTop:
    - Calculate Y position for each gridline (evenly spaced from minPrice to maxPrice)
    - Set each LineRenderer to horizontal line from `_chartLeft` to `_chartRight` at computed Y
  - [ ] When pointCount < 2 (no data), hide gridlines
  - [ ] Gridline Y positions use same math as ChartUI axis labels (matching price label values)
  - [ ] File: `Scripts/Runtime/Chart/ChartLineView.cs`

- [ ] Task 3: Style gridlines for subtlety (AC: 3)
  - [ ] Color: `new Color(0.4f, 0.4f, 0.5f, 0.15f)` — very faint gray
  - [ ] Width: 0.005f world units (thin)
  - [ ] Sorting order: 0 (behind main line at 1, behind glow at -1... actually glow is -1, main is 1, so gridlines at -2 or 0)
  - [ ] Consider: dashed pattern (not natively supported by LineRenderer — skip for now, solid thin line is sufficient)
  - [ ] File: `Scripts/Setup/ChartSetup.cs`, `Scripts/Runtime/Chart/ChartLineView.cs`

- [ ] Task 4: Verify gridline alignment with price labels (AC: 2)
  - [ ] ChartUI.CalculateAxisLabels() computes evenly spaced values between min and max
  - [ ] ChartLineView gridlines must use the same min/max and same Y-mapping math
  - [ ] Both already use `GetLivePriceRange()` and the same padding calculation — verify they stay in sync
  - [ ] File: verification, cross-reference `ChartUI.cs:79-98` with `ChartLineView.cs:113-127`

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

## References
- `Scripts/Setup/ChartSetup.cs:19-131` — chart system creation
- `Scripts/Runtime/Chart/ChartLineView.cs:97-157` — LateUpdate rendering, coordinate math
- `Scripts/Runtime/Chart/ChartUI.cs:79-98` — axis label calculation
- `Scripts/Runtime/Chart/ChartUI.cs:159-174` — `CalculateAxisLabels()` static method
