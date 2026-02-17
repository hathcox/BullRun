# Story 14.5: Chart Repositioning & Event Ticker

Status: ready-for-dev

## Story

As a player,
I want the price chart anchored above the Control Deck with an amber event ticker banner between the stock label and chart,
so that the chart is framed within the CRT viewport and events are immediately visible.

## Acceptance Criteria

1. **Chart Bounds:** Recalculated to fit above the Control Deck (~top 70% of screen, leaving bottom ~20% for Control Deck, top ~10% for stock name/ticker)
2. `ChartSetup.Execute()` chart height/position adjusted: `chartBottom` raised to account for Control Deck height
3. **Stock Label Area** (top of chart viewport):
   - Stock ticker + price centered at top, using `CRTThemeData.TextHigh` for ticker, white for price
   - Larger font sizes (28pt ticker, 20pt price) for CRT readability
4. **Event Ticker Banner:**
   - Amber background (`CRTThemeData.Warning` at 85% alpha) banner between stock label and chart
   - Shows event headlines from `MarketEventFiredEvent` events
   - Full-width, 36px height
   - Warning icon (triangle ⚠) prefix on event text
   - Position: anchored below stock label, above chart area
5. **Chart Grid Lines:** Updated colors to use `CRTThemeData.Border` (#224444) with subtle opacity (0.2)
6. **Chart Background:** Updated to `CRTThemeData.Background` (#050a0a)
7. **Price Axis Labels:** Updated to `CRTThemeData.TextLow` (dim cyan)
8. **Current Price Label:** Updated to `CRTThemeData.TextHigh` (phosphor green)
9. NewsBanner behavior updated to use event ticker display instead of separate top-of-screen overlay
10. No regression in chart rendering — price line, glow trail, gridlines, trade markers all still function

## Tasks / Subtasks

- [ ] Task 1: Adjust chart bounds in ChartSetup.Execute() (AC: 1, 2)
  - [ ] 1.1: Change `ChartHeightPercent` from 0.70 to ~0.55 (top 70% minus Control Deck space)
  - [ ] 1.2: Shift chart center upward so bottom edge clears Control Deck (raise chartBottom)
  - [ ] 1.3: Adjust `ChartWidthPercent` if needed for CRT framing (keep at 0.80 or slightly reduce)
  - [ ] 1.4: Update chart bounds Rect passed to ChartLineView.Initialize()
- [ ] Task 2: Update stock label area (AC: 3)
  - [ ] 2.1: Increase StockNameLabel font size to 28pt, color to `CRTThemeData.TextHigh`
  - [ ] 2.2: Increase StockPriceLabel font size to 20pt, keep white color
  - [ ] 2.3: Reposition stock labels for new chart top position (below top 10% margin)
  - [ ] 2.4: Add stock dot/bullet prefix ("◉") to ticker display
- [ ] Task 3: Create Event Ticker Banner (AC: 4)
  - [ ] 3.1: Create EventTickerBanner panel within ChartCanvas or new EventTickerCanvas (sortingOrder 25)
  - [ ] 3.2: Anchor between stock label and chart area (full-width, 36px height)
  - [ ] 3.3: Set background color to `CRTThemeData.Warning` at 85% alpha
  - [ ] 3.4: Create headline text with "⚠" prefix, white bold text, centered
  - [ ] 3.5: Subscribe to `MarketEventFiredEvent` to show headlines
  - [ ] 3.6: Subscribe to `MarketEventEndedEvent` to hide/fade banner
  - [ ] 3.7: Implement slide-in animation (from right or fade-in)
- [ ] Task 4: Update chart visual colors (AC: 5, 6, 7, 8)
  - [ ] 4.1: Update gridline colors to `CRTThemeData.Border` with 0.2 alpha in ChartSetup
  - [ ] 4.2: Update ChartSetup.BackgroundColor to `CRTThemeData.Background`
  - [ ] 4.3: Update axis label colors to `CRTThemeData.TextLow` in CreateChartUI
  - [ ] 4.4: Update current price label color to `CRTThemeData.TextHigh`
  - [ ] 4.5: Update time progress bar color to use CRTThemeData colors
- [ ] Task 5: Refactor NewsBanner to use event ticker (AC: 9)
  - [ ] 5.1: Redirect MarketEventFiredEvent handling to event ticker banner
  - [ ] 5.2: Remove or disable old top-of-screen NewsBanner canvas overlay
  - [ ] 5.3: Update UISetup.ExecuteNewsBanner() to create event ticker instead (or remove it)
  - [ ] 5.4: Ensure stacking/multiple event handling works with new ticker location
- [ ] Task 6: Update ChartUI label color references (AC: 7, 8)
  - [ ] 6.1: Axis labels in ChartUI should reference CRTThemeData.TextLow color
  - [ ] 6.2: Current price label should reference CRTThemeData.TextHigh color
  - [ ] 6.3: Stock name label color reference updated
- [ ] Task 7: Verify chart rendering (AC: 10)
  - [ ] 7.1: Verify price line renders correctly in new bounds
  - [ ] 7.2: Verify glow trail follows line
  - [ ] 7.3: Verify gridlines appear at correct positions
  - [ ] 7.4: Verify price axis labels align with gridlines
  - [ ] 7.5: Verify break-even line and short position line render correctly
  - [ ] 7.6: Verify trade markers appear at correct chart positions

## Dev Notes

### Architecture Compliance

- **ChartSetup remains static setup:** Chart bounds calculation stays in `ChartSetup.Execute()`. No runtime chart resizing needed — Control Deck height is fixed at 160px.
- **ChartRenderer bounds:** The `Rect chartBounds` passed to `ChartLineView.Initialize()` and `ChartUI.Initialize()` determines all chart rendering. Changing this rect is the primary task.
- **Event ticker:** Can reuse `NewsBanner` event subscriptions or create a new simpler component. The key change is display location (between stock label and chart instead of top-of-screen).

### Chart Bounds Calculation Changes

Current (ChartSetup.cs:42-56):
```csharp
float chartWorldWidth = worldWidth * ChartWidthPercent;   // 0.80
float chartWorldHeight = worldHeight * ChartHeightPercent; // 0.70
float chartLeft = -chartWorldWidth / 2f;
float chartRight = chartWorldWidth / 2f;
float chartBottom = -chartWorldHeight / 2f;
float chartTop = chartWorldHeight / 2f;
```

New calculation needs to:
1. Reserve bottom ~25% for Control Deck (160px / 1080px ≈ 15%, plus margin)
2. Reserve top ~10% for stock label + event ticker
3. Chart occupies middle ~65%
4. Shift chart center upward (positive Y offset)

Approximate new values:
```csharp
float chartHeightPercent = 0.55f;  // 55% of screen height for chart
float chartYOffset = 0.10f;        // Shift center up by 10%
float chartWorldHeight = worldHeight * chartHeightPercent;
float chartBottom = -worldHeight * 0.5f + worldHeight * 0.25f; // Above control deck
float chartTop = chartBottom + chartWorldHeight;
```

### Event Ticker vs NewsBanner

**Option A (simpler):** Modify `NewsBanner` to render at the new position (between stock label and chart) instead of top-of-screen. Change banner container anchoring and visual style (amber background).

**Option B (cleaner):** Create a new dedicated event ticker component and disable old NewsBanner. NewsTicker (scrolling bottom bar) is also removed by Control Deck.

Recommendation: **Option A** — modify NewsBanner positioning and colors. Less code change, same event subscription logic.

### UI Canvas Element Positioning (ChartCanvas)

Current ChartUI elements (ChartSetup.cs:211-327):
- Axis labels: positioned at `960f * ChartWidthPercent + 10f` (right edge)
- Stock name: anchored top-center at `-70f` from top
- Stock price: anchored top-center at `-100f` from top
- Time progress bar: positioned at `-540f * ChartHeightPercent - 20f`

All these Y positions need adjustment for the new chart bounds. The axis label X position remains roughly the same.

### Grid Line Color Changes

Current (ChartSetup.cs:100-101):
```csharp
lr.startColor = new Color(0.4f, 0.4f, 0.5f, 0.15f);
lr.endColor = new Color(0.4f, 0.4f, 0.5f, 0.15f);
```

New:
```csharp
var gridColor = new Color(CRTThemeData.Border.r, CRTThemeData.Border.g, CRTThemeData.Border.b, 0.2f);
lr.startColor = gridColor;
lr.endColor = gridColor;
```

### Background Color

The chart background is currently set via `BackgroundColor = new Color(0.039f, 0.055f, 0.153f, 1f)` but it's not explicitly rendered as a background panel — the camera clear color serves as background. If a background panel is needed, create one behind the chart area using CRTThemeData.Background.

Alternatively, the camera clear color could be changed to CRTThemeData.Background in Story 14.6 as part of global theme application.

### NewsTicker Removal Consideration

The scrolling `NewsTicker` bar at the very bottom of the screen (`UISetup.ExecuteNewsTicker()`) is now covered by the Control Deck. It should be removed or repositioned. The Event Ticker Banner above the chart replaces its function.

### Testing Approach

- Visual testing: Chart should be vertically centered above Control Deck with clear separation.
- Stock label: Ticker name and price visible at top in CRT green, larger than before.
- Event ticker: Trigger a market event, verify amber banner appears between stock label and chart.
- Grid lines: Should be subtle teal (#224444) lines, not the current gray.
- Axis labels: Dim cyan color, readable against dark background.
- Price label: Bright phosphor green at chart head.
- Regression: Chart line still renders smoothly, trades still mark correctly.

### References

- [Source: _bmad-output/planning-artifacts/epic-14-terminal-1999-ui.md#Story 14.5]
- [Source: Assets/Scripts/Setup/ChartSetup.cs:11-15] — current chart constants
- [Source: Assets/Scripts/Setup/ChartSetup.cs:42-57] — current chart bounds calculation
- [Source: Assets/Scripts/Setup/ChartSetup.cs:92-108] — gridline color creation
- [Source: Assets/Scripts/Setup/ChartSetup.cs:226-304] — ChartUI label creation (axis, stock name, stock price)
- [Source: Assets/Scripts/Runtime/Chart/ChartUI.cs:24-43] — Initialize with chartBounds
- [Source: Assets/Scripts/Runtime/Chart/ChartUI.cs:45-61] — SetStockLabels
- [Source: Assets/Scripts/Runtime/UI/NewsBanner.cs:13-14] — current banner colors
- [Source: Assets/Scripts/Runtime/UI/NewsBanner.cs:40-47] — OnMarketEventFired handler
- [Source: Assets/Scripts/Setup/UISetup.cs:723-760] — ExecuteNewsBanner creation
- [Source: Assets/Scripts/Setup/UISetup.cs:766-799] — ExecuteNewsTicker creation
- [Source: Assets/Scripts/Setup/Data/CRTThemeData.cs] — Background, Border, TextLow, TextHigh, Warning colors

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
