# Story FIX-7: Current Position Overlay — Bottom-Left of Chart

Status: pending

## Story

As a player,
I want to see my current position (shares, direction, P&L) overlaid on the bottom-left of the stock chart,
so that I always know my exposure at a glance while watching the price action.

## Problem Analysis

Currently the `PositionPanel` is a right sidebar (180px wide) that lists all open positions. With the single-stock-per-round change (FIX-5), there's only ever one position to display, making the full sidebar wasteful. The new design places a compact position display directly on the chart area for immediate visual context.

**Affected Code:**
- `Scripts/Runtime/UI/PositionPanel.cs` — rewrite as compact overlay
- `Scripts/Setup/UISetup.cs` — replace `ExecutePositionsPanel()` with new overlay
- `Scripts/Runtime/Core/GameRunner.cs` — remove old positions panel creation
- `Scripts/Setup/ChartSetup.cs` — chart bounds may need adjustment (no right panel taking space)

## Acceptance Criteria

1. A compact position panel is overlaid on the bottom-left corner of the main chart area
2. When no position is held: shows "FLAT" in neutral color
3. When long: shows share count, "LONG" label in green, and unrealized P&L
4. When short: shows share count, "SHORT" label in pink/red, and unrealized P&L
5. P&L updates in real-time as price changes
6. P&L color: green for profit, red for loss
7. The overlay is semi-transparent so the chart line is still visible behind it
8. The old right-side PositionPanel is removed
9. Average entry price is displayed for reference

## Tasks / Subtasks

- [ ] Task 1: Create compact position overlay UI (AC: 1, 2, 3, 4, 7, 9)
  - [ ] Create `ExecutePositionOverlay()` in UISetup (replaces `ExecutePositionsPanel()`)
  - [ ] Position: anchored to bottom-left of chart canvas area
  - [ ] Layout (compact, ~200x80px):
    ```
    ┌─────────────────────┐
    │ 15x LONG            │
    │ Avg: $2.45          │
    │ P&L: +$3.75         │
    └─────────────────────┘
    ```
  - [ ] When flat:
    ```
    ┌─────────────────────┐
    │ FLAT                │
    └─────────────────────┘
    ```
  - [ ] Background: semi-transparent dark (alpha ~0.6)
  - [ ] Canvas: use ChartCanvas (sorting order 10) or a dedicated overlay canvas at order 11
  - [ ] File: `Scripts/Setup/UISetup.cs`

- [ ] Task 2: Create PositionOverlay MonoBehaviour (AC: 2, 3, 4, 5, 6)
  - [ ] New file: `Scripts/Runtime/UI/PositionOverlay.cs`
  - [ ] Subscribe to `PriceUpdatedEvent` for real-time P&L updates
  - [ ] Subscribe to `TradeExecutedEvent` to refresh position state
  - [ ] Subscribe to `RoundStartedEvent` to reset to FLAT
  - [ ] Fields: shares count text, direction label, avg price text, P&L text
  - [ ] Colors: LONG = neon green (#00FF88), SHORT = hot pink (#FF66B3), FLAT = gray, P&L green/red
  - [ ] `RefreshDisplay()`: reads from Portfolio for current position state
  - [ ] File: `Scripts/Runtime/UI/PositionOverlay.cs`

- [ ] Task 3: Remove old PositionPanel (AC: 8)
  - [ ] Remove `UISetup.ExecutePositionsPanel()` call from `GameRunner.Start()`
  - [ ] Keep `PositionPanel.cs` and `PositionPanelData.cs` files (don't delete — cleanup pass later)
  - [ ] Files: `Scripts/Runtime/Core/GameRunner.cs`, `Scripts/Setup/UISetup.cs`

- [ ] Task 4: Expand chart to use freed right-side space (AC: 8)
  - [ ] Adjust `ChartSetup.ChartWidthPercent` to account for no right sidebar (180px freed)
  - [ ] Reposition Y-axis labels further right if needed
  - [ ] Note: coordinate with FIX-5 which also expands chart (left sidebar removal)
  - [ ] File: `Scripts/Setup/ChartSetup.cs`

- [ ] Task 5: Wire PositionOverlay to Portfolio (AC: 5, 6)
  - [ ] Pass Portfolio reference to PositionOverlay.Initialize()
  - [ ] Wire in GameRunner.Start() after portfolio is created
  - [ ] Since FIX-5 means single stock, the overlay always queries the single active stock's position
  - [ ] File: `Scripts/Runtime/Core/GameRunner.cs`

## Dev Notes

### Architecture Compliance
- **Setup-Oriented Generation:** `ExecutePositionOverlay()` follows same pattern as other Execute methods
- **uGUI Canvas:** Text + Image components, no Inspector config
- **EventBus:** Subscribes to PriceUpdatedEvent, TradeExecutedEvent, RoundStartedEvent
- **Read-only UI:** Never modifies positions, only reads for display

### Display Format
- Shares: "15x" — simple count with x suffix
- Direction: "LONG" or "SHORT" — uppercase, color-coded
- Avg price: "Avg: $2.45" — shows cost basis
- P&L: "+$3.75" or "-$1.20" — with sign, color-coded green/red
- When flat: just "FLAT" centered, dimmed color

### Performance
- P&L update: only when PriceUpdatedEvent fires for the active stock (not every frame)
- Position rebuild: only when TradeExecutedEvent fires (on actual trade)
- Light UI — just 4 text fields, no layout groups needed

### Dependencies
- **FIX-5** (Single Stock): simplifies this — only 1 stock to track, no position list needed
- Can be implemented independently but positioning coordinates with FIX-5 chart expansion

## References
- `Scripts/Runtime/UI/PositionPanel.cs` — current right sidebar (to be replaced)
- `Scripts/Setup/UISetup.cs:292-353` — `ExecutePositionsPanel()` (to be replaced)
- `Scripts/Runtime/Core/GameRunner.cs:61` — current positions panel creation
- `Scripts/Runtime/Trading/Portfolio.cs` — position data source
