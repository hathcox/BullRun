# Story FIX-5: Single Stock Per Round

Status: done

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

- [x] Task 1: Set all tiers to 1 stock per round (AC: 1)
  - [x] Change `StockTierData.Penny`: minStocksPerRound=1, maxStocksPerRound=1
  - [x] Change `StockTierData.LowValue`: minStocksPerRound=1, maxStocksPerRound=1
  - [x] Change `StockTierData.MidValue`: minStocksPerRound=1, maxStocksPerRound=1
  - [x] Change `StockTierData.BlueChip`: minStocksPerRound=1, maxStocksPerRound=1
  - [x] File: `Scripts/Setup/Data/StockTierData.cs`

- [x] Task 2: Remove StockSidebar creation and references (AC: 2, 8)
  - [x] Remove `UISetup.ExecuteSidebar()` call from `GameRunner.Start()`
  - [x] Remove `_sidebarData` field and all references in `GameRunner.cs`
  - [x] Remove stock selection keyboard shortcuts (1-4 keys) from `StockSidebar.cs` or remove file entirely
  - [x] Remove `UISetup.ExecuteKeyLegend()` call (will be replaced by FIX-6 trade panel)
  - [x] Keep `StockSidebar.cs` and `StockSidebarData.cs` files in place but stop creating them — removal can be done in cleanup pass
  - [x] Files: `Scripts/Runtime/Core/GameRunner.cs`, `Scripts/Setup/UISetup.cs`

- [x] Task 3: Update GameRunner to auto-target single stock (AC: 5)
  - [x] Replace `GetSelectedStockId()` (was sidebar-based) with direct access to `_priceGenerator.ActiveStocks[0]`
  - [x] Replace `GetSelectedTicker()` similarly
  - [x] Replace `GetStockPrice(int)` with direct `_priceGenerator.ActiveStocks[0].CurrentPrice`
  - [x] File: `Scripts/Runtime/Core/GameRunner.cs`

- [x] Task 4: Expand chart to use freed sidebar space (AC: 3)
  - [x] Adjust `ChartSetup.ChartWidthPercent` from 0.55f to ~0.65f (or wider — left edge can start earlier)
  - [x] Adjust chart left bound calculation to account for no sidebar (was offset by 240px)
  - [x] Reposition Y-axis label X offset if needed
  - [x] File: `Scripts/Setup/ChartSetup.cs`

- [x] Task 5: Verify MarketOpen preview with single stock (AC: 6)
  - [x] MarketOpenState already publishes `MarketOpenEvent` with stock arrays — verify it works with 1 entry
  - [x] MarketOpenUI stock list display should still work (just shows 1 stock)
  - [x] File: verification only, likely no changes needed

- [x] Task 6: Verify event system with single stock (AC: 7)
  - [x] `EventScheduler` picks from `ActiveStocks` list — verify events fire correctly for 1 stock
  - [x] Sector rotation events may need adjustment (they affect multiple stocks — with 1 stock, the effect is simpler)
  - [x] Files: `Scripts/Runtime/Events/EventScheduler.cs`, verification

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

## Dev Agent Record

### Implementation Plan
- **Task 1:** Simple data change — all four StockTierConfig instances updated from 2-4 / 3-4 stocks to exactly 1.
- **Tasks 2+3:** Removed sidebar creation and all sidebar-dependent code from GameRunner. Rewrote `GetSelectedStockId()`, `GetSelectedTicker()`, and `GetStockPrice()` to read directly from `_priceGenerator.ActiveStocks[0]`. Removed `_sidebarData` field, `UISetup.ExecuteSidebar()` call, `UISetup.ExecuteKeyLegend()` call, and sidebar MarketOpenEvent subscription. StockSidebar.cs and StockSidebarData.cs files kept in place (not instantiated) for future cleanup.
- **Task 4:** ChartWidthPercent expanded from 0.55f to 0.65f. Chart bounds are calculated centered in world space, so no offset adjustments needed — the sidebar was a UI overlay, not a world-space offset. Y-axis labels unchanged (relative to positions panel on right side).
- **Tasks 5+6:** Verification only. MarketOpenUI's BuildStockList iterates arrays and works fine with 1 entry. EventScheduler: stock-specific events target `activeStocks[0]`, global events work with any count, SectorRotation has guard `if (activeStocks.Count < 2) return` making it a no-op with 1 stock.

### Debug Log
- No errors encountered during implementation.
- All changes are subtractive or constant modifications — low regression risk.

### Completion Notes
- All 6 tasks completed. All 8 acceptance criteria satisfied.
- Test added: `AllTiers_HaveExactlyOneStockPerRound` in StockTierDataTests.cs verifies the data change.
- Existing tests unaffected — StockSidebar/StockSidebarData tests remain valid (classes still exist, just not instantiated at runtime).
- SectorRotation gracefully degrades to no-op with 1 stock (guard clause already present).
- Chart auto-selects the single stock via existing MarketOpenEvent subscriptions in ChartSetup and ChartUI.
- QuantitySelector delegates still wired to updated GetSelectedStockId/GetStockPrice methods.

### Review Follow-ups (AI)
- [ ] [AI-Review][MEDIUM] SectorRotation events silently become no-ops with 1 stock — filter from eligible event pool when activeStocks.Count < 2 [EventScheduler.cs:126-177]
- [ ] [AI-Review][LOW] Remove dead sidebar code (ExecuteSidebar, CreateStockEntryView, SidebarWidth constant) from UISetup.cs in cleanup pass [UISetup.cs:16,127-286]
- [ ] [AI-Review][LOW] Consider widening chart beyond 0.65 (e.g. 0.72-0.78) to better fill freed sidebar space [ChartSetup.cs:12]

## File List
- `Assets/Scripts/Setup/Data/StockTierData.cs` (modified) — All 4 tiers set to minStocksPerRound=1, maxStocksPerRound=1
- `Assets/Scripts/Runtime/Core/GameRunner.cs` (modified) — Removed sidebar field/references, rewrote stock accessors to use ActiveStocks[0], added debug assertion for stockId mismatch
- `Assets/Scripts/Setup/UISetup.cs` (modified) — Removed ExecuteSidebar() call from parameterless Execute(), fixed stale class doc comment
- `Assets/Scripts/Setup/ChartSetup.cs` (modified) — ChartWidthPercent 0.55f -> 0.65f
- `Assets/Tests/Runtime/PriceEngine/StockTierDataTests.cs` (modified) — Added AllTiers_HaveExactlyOneStockPerRound test

## Change Log
- 2026-02-13: Code review fixes — H1: Added debug assertion to GetStockPrice for stockId mismatch. M1: Fixed stale UISetup doc comment. M3: SectorRotation no-op added as action item. 3 LOW items deferred.
- 2026-02-13: FIX-5 implemented — Single stock per round. All tiers set to 1 stock, sidebar removed from UI creation, chart expanded to 65% width, GameRunner auto-targets single active stock.

## References
- `Scripts/Setup/Data/StockTierData.cs` — stock counts per tier
- `Scripts/Runtime/UI/StockSidebar.cs` — sidebar MonoBehaviour
- `Scripts/Setup/UISetup.cs:127-187` — `ExecuteSidebar()` method
- `Scripts/Runtime/Core/GameRunner.cs:59-93` — sidebar wiring
- `Scripts/Setup/ChartSetup.cs:12-13` — chart dimensions
