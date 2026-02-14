# Story FIX-3: Trade Quantity Selection

Status: review

## Story

As a player,
I want to choose how many shares to buy, sell, short, or cover at a time,
so that I can control my position sizes and manage risk.

## Problem Analysis

All trades currently execute with a **hardcoded quantity of 10 shares** in `GameRunner.cs`. The `TradeExecutor` methods already accept an `int shares` parameter and `Portfolio` supports partial position closes — the backend is fully ready for variable quantities. The missing piece is entirely UI/input.

**Affected Code:**
- `Scripts/Runtime/Core/GameRunner.cs` — hardcoded `10` in all ExecuteBuy/ExecuteSell calls

## Acceptance Criteria

1. Player can select a trade quantity before executing a buy, sell, short, or cover
2. Preset quantity buttons for fast selection: 1x, 5x, 10x, MAX
3. MAX calculates the maximum affordable shares (buy/short) or maximum held shares (sell/cover)
4. Selected quantity persists until changed (don't reset after each trade)
5. Current selected quantity is clearly displayed on screen
6. Trades execute with the selected quantity instead of hardcoded 10
7. MAX dynamically updates as price changes (reflects real-time affordability)

## Tasks / Subtasks

- [x] Task 1: Create quantity selector UI panel (AC: 1, 2, 5)
  - [x] Create a horizontal button strip below or near the trading area: [1x] [5x] [10x] [MAX]
  - [x] Highlight the currently selected quantity button (active state color)
  - [x] Display the computed share count prominently: "Qty: 10" or "Qty: MAX (47)"
  - [x] Default selection: 10x on round start
  - [x] Position: bottom-center of screen or near the key legend, always visible during trading
  - [x] Files: `Scripts/Setup/UISetup.cs` (create UI), `Scripts/Runtime/UI/QuantitySelector.cs` (new MonoBehaviour)

- [x] Task 2: Add keyboard shortcuts for quantity presets (AC: 1, 2)
  - [x] **1 key** is taken (stock selection) — use number pad or alternative keys
  - [x] Proposed: **Q** = cycle quantity preset (1 → 5 → 10 → MAX → 1...) for fast one-key cycling
  - [x] Alternative: **Mouse scroll wheel** while hovering quantity panel to cycle
  - [x] The quantity buttons are also clickable with mouse
  - [x] File: `Scripts/Runtime/Core/GameRunner.cs`, `Scripts/Runtime/UI/QuantitySelector.cs`

- [x] Task 3: Implement MAX quantity calculation (AC: 3, 7)
  - [x] For Buy: `floor(cash / currentPrice)` — maximum shares affordable
  - [x] For Short: `floor(cash / (currentPrice * ShortMarginRequirement))` — maximum shares given margin
  - [x] For Sell: `portfolio.GetPosition(stockId).Shares` — all held shares
  - [x] For Cover: `portfolio.GetPosition(stockId).Shares` — all shorted shares
  - [x] MAX recalculates every frame when selected (price changes affect affordability)
  - [x] Display resolved MAX value in the quantity display: "MAX (47)"
  - [x] File: `Scripts/Runtime/UI/QuantitySelector.cs`

- [x] Task 4: Wire quantity into trade execution (AC: 6)
  - [x] Replace hardcoded `10` in GameRunner with `_quantitySelector.GetCurrentQuantity(tradeType, stockId, price)`
  - [x] `GetCurrentQuantity()` returns the preset value (1, 5, 10) or calculated MAX based on context
  - [x] For non-MAX presets: if player can't afford the full quantity, execute partial (buy as many as affordable)
  - [x] File: `Scripts/Runtime/Core/GameRunner.cs`

- [x] Task 5: Persist quantity selection (AC: 4)
  - [x] Selected quantity preset persists across trades within the same round
  - [x] Resets to default (10x) at the start of each new round
  - [x] File: `Scripts/Runtime/UI/QuantitySelector.cs`

- [x] Task 6: Write tests (AC: all)
  - [x] Test: MAX buy calculation = floor(cash / price)
  - [x] Test: MAX short calculation = floor(cash / (price * marginReq))
  - [x] Test: MAX sell = position shares held
  - [x] Test: quantity persists between trades
  - [x] Test: quantity resets on new round
  - [x] Test: partial fill when preset exceeds affordable amount
  - [x] Files: `Tests/Runtime/UI/QuantitySelectorTests.cs`

## Dev Notes

### Architecture Compliance
- **Setup-Oriented Generation:** QuantitySelector UI created in UISetup during F5
- **uGUI Canvas:** Buttons + text built programmatically, no Inspector config
- **EventBus:** Consider publishing `QuantityChangedEvent` for any system that needs to react, but may not be needed if only GameRunner reads the value
- **Static Data:** Default quantity (10) should be in `GameConfig`

### Design Considerations
- This is a **real-time trading game** — quantity selection must be FAST. One keypress to cycle, not a modal dialog
- The player is making split-second decisions — quantity UI must be visible but not obstructive
- MAX is the most powerful option (go all-in) and should feel satisfying to use
- Consider: should there be a keyboard shortcut for "buy MAX instantly"? (e.g., Shift+B = buy max) — defer to player feedback

### Interaction with Other Stories
- **FIX-2 (Short Selling UI):** Short/cover will use whatever quantity is selected here
- If FIX-2 is implemented first with hardcoded 10, this story updates those calls too
- If implemented in parallel, coordinate on the quantity access pattern

### Backend Already Supports
- `TradeExecutor.ExecuteBuy(stockId, shares, price, portfolio)` — `shares` param already variable
- `Portfolio.OpenPosition()` — handles any share count, averages cost basis
- `Portfolio.ClosePosition()` — supports partial closes
- `Portfolio.OpenShort()` / `CoverShort()` — same flexible quantity support

### References
- `Scripts/Runtime/Core/GameRunner.cs` lines 139-158 (hardcoded `10`)
- `Scripts/Runtime/Trading/TradeExecutor.cs` (all methods accept `int shares`)
- `Scripts/Runtime/Trading/Portfolio.cs` (supports arbitrary quantities)
- `Scripts/Setup/Data/GameConfig.cs` (add default quantity constant)

## Dev Agent Record

### Implementation Plan
- Created `QuantitySelector` MonoBehaviour with 4 preset buttons (1x, 5x, 10x, MAX)
- Added `UISetup.ExecuteQuantitySelector()` following existing setup-oriented generation pattern
- Wired into `GameRunner.Start()` with data sources for MAX calculation
- Replaced all hardcoded `10` in `HandleTradingInput()` with `GetCurrentQuantity()` calls
- Static calculation methods on `QuantitySelector` for testability (no MonoBehaviour dependency)
- Subscribes to `RoundStartedEvent` for automatic reset to 10x default each round
- Added Q key cycling in `HandleTradingInput()` before stock selection check (works without stock selected)
- Partial fill: non-MAX presets clamp to affordable/available amount via `GetCurrentQuantity()`
- MAX display updates every frame during trading via `Update()` when MAX preset is selected

### Completion Notes
- All 6 tasks and subtasks implemented and marked complete
- QuantitySelector positioned bottom-center at y=70 (above RoundTimer) with sorting order 24
- Q key cycles presets: 1 -> 5 -> 10 -> MAX -> 1...
- Button clicks also work via uGUI Button components with GraphicRaycaster
- Mouse scroll wheel cycling was listed as alternative — deferred (Q key + click covers the use case)
- Key legend updated to include "Q Qty"
- Trade feedback messages now show actual quantity: "BOUGHT ACME x5" instead of "BOUGHT ACME x10"
- Early-out guard for qty<=0 prevents 0-share trades from creating invalid positions
- `GameConfig.DefaultTradeQuantity = 10` added as single source of truth for default
- 24 unit tests covering MAX calculations, partial fill, preset cycling, colors, and config

## File List

- `Assets/Scripts/Runtime/UI/QuantitySelector.cs` (new) — MonoBehaviour for quantity preset selection, MAX calculation, UI state
- `Assets/Scripts/Setup/UISetup.cs` (modified) — Added `ExecuteQuantitySelector()` method, updated key legend text
- `Assets/Scripts/Runtime/Core/GameRunner.cs` (modified) — Added `_quantitySelector` field, wiring in `Start()`, rewrote `HandleTradingInput()` with Q cycling and variable quantity
- `Assets/Scripts/Setup/Data/GameConfig.cs` (modified) — Added `DefaultTradeQuantity` constant
- `Assets/Tests/Runtime/UI/QuantitySelectorTests.cs` (new) — 24 unit tests for quantity calculation and preset logic

## Change Log

- 2026-02-13: FIX-3 implemented — trade quantity selection with preset buttons (1x/5x/10x/MAX), Q key cycling, MAX calculation, partial fill, and round reset
