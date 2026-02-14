# Story FIX-5: Single Stock Per Round

Status: pending

## Story

As a player,
I want to focus on a single stock each round,
so that the game feels more intense and decision-making is clearer.

## Problem Analysis

Currently the game spawns 2-4 stocks per round depending on the tier (Penny: 3-4, Low: 3-4, Mid: 2-3, Blue: 2-3). The left `StockSidebar` lets players switch between stocks with keyboard shortcuts 1-4. This creates decision paralysis and dilutes focus. The design intent is to simplify: one stock per round, no selection needed.

**Affected Code:**
- `Scripts/Setup/Data/StockTierData.cs` — min/maxStocksPerRound values
- `Scripts/Runtime/UI/StockSidebar.cs` — entire sidebar becomes unnecessary
- `Scripts/Setup/UISetup.cs` — sidebar creation (`ExecuteSidebar()`)
- `Scripts/Runtime/Core/GameRunner.cs` — sidebar data references, stock selection logic
- `Scripts/Runtime/Chart/ChartSetup.cs` — chart bounds assume sidebar on left (240px)

## Acceptance Criteria

1. Each round spawns exactly 1 stock (regardless of tier)
2. The left stock sidebar is removed from the UI
3. The chart area expands to use the freed-up left space
4. The single stock's name and price are displayed prominently (already handled by ChartUI stock name/price labels)
5. All trading actions automatically target the single active stock (no selection needed)
6. The MarketOpen preview still shows the single stock info
7. Event system still works with a single stock
8. Stock selection keyboard shortcuts (1-4) are removed

## Tasks / Subtasks

- [ ] Task 1: Set all tiers to 1 stock per round (AC: 1)
  - [ ] Change `StockTierData.Penny`: minStocksPerRound=1, maxStocksPerRound=1
  - [ ] Change `StockTierData.LowValue`: minStocksPerRound=1, maxStocksPerRound=1
  - [ ] Change `StockTierData.MidValue`: minStocksPerRound=1, maxStocksPerRound=1
  - [ ] Change `StockTierData.BlueChip`: minStocksPerRound=1, maxStocksPerRound=1
  - [ ] File: `Scripts/Setup/Data/StockTierData.cs`

- [ ] Task 2: Remove StockSidebar creation and references (AC: 2, 8)
  - [ ] Remove `UISetup.ExecuteSidebar()` call from `GameRunner.Start()`
  - [ ] Remove `_sidebarData` field and all references in `GameRunner.cs`
  - [ ] Remove stock selection keyboard shortcuts (1-4 keys) from `StockSidebar.cs` or remove file entirely
  - [ ] Remove `UISetup.ExecuteKeyLegend()` call (will be replaced by FIX-6 trade panel)
  - [ ] Keep `StockSidebar.cs` and `StockSidebarData.cs` files in place but stop creating them — removal can be done in cleanup pass
  - [ ] Files: `Scripts/Runtime/Core/GameRunner.cs`, `Scripts/Setup/UISetup.cs`

- [ ] Task 3: Update GameRunner to auto-target single stock (AC: 5)
  - [ ] Replace `GetSelectedStockId()` (was sidebar-based) with direct access to `_priceGenerator.ActiveStocks[0]`
  - [ ] Replace `GetSelectedTicker()` similarly
  - [ ] Replace `GetStockPrice(int)` with direct `_priceGenerator.ActiveStocks[0].CurrentPrice`
  - [ ] File: `Scripts/Runtime/Core/GameRunner.cs`

- [ ] Task 4: Expand chart to use freed sidebar space (AC: 3)
  - [ ] Adjust `ChartSetup.ChartWidthPercent` from 0.55f to ~0.65f (or wider — left edge can start earlier)
  - [ ] Adjust chart left bound calculation to account for no sidebar (was offset by 240px)
  - [ ] Reposition Y-axis label X offset if needed
  - [ ] File: `Scripts/Setup/ChartSetup.cs`

- [ ] Task 5: Verify MarketOpen preview with single stock (AC: 6)
  - [ ] MarketOpenState already publishes `MarketOpenEvent` with stock arrays — verify it works with 1 entry
  - [ ] MarketOpenUI stock list display should still work (just shows 1 stock)
  - [ ] File: verification only, likely no changes needed

- [ ] Task 6: Verify event system with single stock (AC: 7)
  - [ ] `EventScheduler` picks from `ActiveStocks` list — verify events fire correctly for 1 stock
  - [ ] Sector rotation events may need adjustment (they affect multiple stocks — with 1 stock, the effect is simpler)
  - [ ] Files: `Scripts/Runtime/Events/EventScheduler.cs`, verification

## Dev Notes

### Architecture Compliance
- **Setup-Oriented Generation:** Removing sidebar creation follows the pattern — just stop calling `ExecuteSidebar()`
- **EventBus:** StockSelectedEvent still fires for the single stock via MarketOpenEvent handler — chart auto-selects it
- **Static Data:** StockTierData changes are simple constant updates

### Dependencies
- **FIX-6** (Trade Panel) depends on this — the new trade panel replaces the sidebar + key legend
- **FIX-7** (Position Overlay) can be done independently
- **FIX-8** (Gridlines) can be done independently

### Risk
- Low risk — reducing stock count is a data change, removing sidebar is subtractive
- Event system edge cases with single stock need testing (sector rotation, pump & dump target selection)

## References
- `Scripts/Setup/Data/StockTierData.cs` — stock counts per tier
- `Scripts/Runtime/UI/StockSidebar.cs` — sidebar MonoBehaviour
- `Scripts/Setup/UISetup.cs:127-187` — `ExecuteSidebar()` method
- `Scripts/Runtime/Core/GameRunner.cs:59-93` — sidebar wiring
- `Scripts/Setup/ChartSetup.cs:12-13` — chart dimensions
