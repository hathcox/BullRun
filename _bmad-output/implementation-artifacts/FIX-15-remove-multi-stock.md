# Story FIX-15: Remove Multi-Stock — Single Stock Per Round (Permanent)

Status: review

## Story

As a player,
I want exactly one stock per round with no option to change that,
so that I can focus all my attention on a single ticker and every event, trade, and decision is directly relevant to the stock I'm managing.

## Problem Analysis

Story 13.7 (Expansion Effects) re-introduced multi-stock support as a purchasable expansion and **reverted FIX-5's single-stock tier data**. The current state is:

**StockTierData.cs (current broken values):**
- Penny: 3-5 stocks/round
- Low-Value: 3-4 stocks/round
- Mid-Value: 3-5 stocks/round
- Blue Chip: 3-4 stocks/round

**MarketOpenState.Enter() logic:**
```csharp
int stockCount = ctx.OwnedExpansions.Contains(MultiStockTrading) ? 2 : -1;
_priceGenerator.InitializeRound(act, round, stockCount);
```
- Without expansion: `stockCount = -1` → uses tier config → spawns 3-5 stocks (broken!)
- With expansion: `stockCount = 2` → spawns 2 stocks (ironically fewer)

**Result:** Players see events for stocks they aren't actively trading, the sidebar flickers in/out, and the experience is unfocused.

**Design Intent:** One stock per round, always. Remove the Multi-Stock Trading expansion entirely. Remove all conditional multi-stock logic. Make this permanent and non-negotiable.

## Acceptance Criteria

1. Each round spawns exactly 1 stock regardless of tier, act, or expansions owned
2. The Multi-Stock Trading expansion is removed from `ExpansionDefinitions.All` and all references
3. `StockTierData` all four tiers set to `minStocksPerRound=1, maxStocksPerRound=1`
4. `MarketOpenState.Enter()` no longer checks for multi-stock expansion — always spawns 1 stock
5. `PriceGenerator.InitializeRound()` `stockCountOverride` parameter removed or always ignored — tier data drives the count (which is always 1)
6. `EventScheduler.FireEvent()` always targets `activeStocks[0]` — no random stock selection
7. `EventScheduler.FireSectorRotation()` simplified to single-stock only — remove multi-stock branch
8. Stock sidebar is never shown — remove conditional re-enabling from `GameRunner`
9. `GameRunner` stock selection logic (`GetSelectedStockId`, `_selectedStockIndex`, `StockSelectedEvent` subscription) simplified to always use `ActiveStocks[0]`
10. `ExpansionCostMultiStock` removed from `GameConfig`
11. Store UI shows 5 expansions instead of 6 (Multi-Stock slot removed)
12. All existing tests updated — multi-stock expansion tests removed, single-stock assertions remain
13. `Dual Short` expansion description updated — clarify it means 2 concurrent short positions on the same stock (no multi-stock implication)

## Tasks / Subtasks

- [x] Task 1: Fix StockTierData — all tiers to 1 stock per round (AC: 1, 3)
  - [x] Set `minStocksPerRound: 1, maxStocksPerRound: 1` for Penny, LowValue, MidValue, BlueChip
  - [x] Update comments to reflect "1 stock/round"
  - [x] File: `Assets/Scripts/Setup/Data/StockTierData.cs`

- [x] Task 2: Remove Multi-Stock expansion definition (AC: 2, 10, 11)
  - [x] Remove `MultiStockTrading` entry from `ExpansionDefinitions.All` array
  - [x] Keep `const string MultiStockTrading` for safe removal references
  - [x] Remove `ExpansionCostMultiStock` from `GameConfig.cs`
  - [x] Files: `Assets/Scripts/Setup/Data/ExpansionDefinitions.cs`, `Assets/Scripts/Setup/Data/GameConfig.cs`

- [x] Task 3: Simplify MarketOpenState — remove multi-stock check (AC: 4)
  - [x] Remove `ctx.OwnedExpansions.Contains(ExpansionDefinitions.MultiStockTrading)` check
  - [x] Call `PriceGenerator.InitializeRound()` without override — let tier data handle it
  - [x] File: `Assets/Scripts/Runtime/Core/GameStates/MarketOpenState.cs`

- [x] Task 4: Simplify PriceGenerator.InitializeRound() (AC: 5)
  - [x] Remove `stockCountOverride` parameter from `InitializeRound()`
  - [x] Remove `countOverride` parameter from `SelectStocksForRound()`
  - [x] File: `Assets/Scripts/Runtime/PriceEngine/PriceGenerator.cs`

- [x] Task 5: Simplify EventScheduler for single stock only (AC: 6, 7)
  - [x] `FireEvent()`: always use `activeStocks[0].StockId` — removed random stock selection
  - [x] `FireSectorRotation()`: removed multi-stock branch, kept single-stock random direction only
  - [x] File: `Assets/Scripts/Runtime/Events/EventScheduler.cs`

- [x] Task 6: Remove multi-stock sidebar logic from GameRunner (AC: 8, 9)
  - [x] Removed `_stockSidebar`, `_multiStockActive`, `_selectedStockIndex` fields
  - [x] Removed sidebar creation (`UISetup.ExecuteSidebar(4)`) and activation logic
  - [x] Removed `StockSelectedEvent` subscription and `OnStockSelected` handler
  - [x] Simplified `GetSelectedStockId()` and `GetSelectedTicker()` to always use `ActiveStocks[0]`
  - [x] Removed multi-stock sidebar initialization from `OnMarketOpenForOverlay`
  - [x] File: `Assets/Scripts/Runtime/Core/GameRunner.cs`

- [x] Task 7: Update Dual Short description (AC: 13)
  - [x] Changed from "Short a second stock simultaneously" to "Open a second short position simultaneously"
  - [x] File: `Assets/Scripts/Setup/Data/ExpansionDefinitions.cs`

- [x] Task 8: Update tests (AC: 12)
  - [x] Replaced multi-stock expansion tests with single-stock assertions in `ExpansionEffectsTests.cs`
  - [x] Updated `AllTiers_HaveExactlyOneStockPerRound` test in `StockTierDataTests.cs`
  - [x] Updated multi-stock targeting assertions to single-stock in `EventSchedulerTests.cs`
  - [x] Updated `ExpansionManagerTests.cs` — removed all multi-stock references, updated expansion counts from 6 to 5
  - [x] `SingleStockEventTests.cs` — no changes needed (already single-stock only)
  - [x] `PriceGeneratorTests.cs` — removed 5 multi-stock sector correlation tests, replaced `MultipleStocksCanHaveIndependentTrends` with `SingleStock_HasValidTrendAndPrice`
  - [x] Files: `Assets/Tests/Runtime/Shop/ExpansionEffectsTests.cs`, `Assets/Tests/Runtime/PriceEngine/StockTierDataTests.cs`, `Assets/Tests/Runtime/Events/EventSchedulerTests.cs`, `Assets/Tests/Runtime/Shop/ExpansionManagerTests.cs`, `Assets/Tests/Runtime/PriceEngine/PriceGeneratorTests.cs`

## Dev Notes

### Architecture Compliance

- **Setup-Oriented Generation:** StockTierData is a static data class — changes are constant updates
- **EventBus:** No new events needed. Removing StockSelectedEvent subscription simplifies GameRunner
- **No Inspector Config:** All changes are code-only
- **Subtractive Change:** This story is almost entirely removing code, which is the lowest-risk change type

### Key Design Decision

The Multi-Stock Trading expansion is being **permanently removed**, not just disabled. The game design has settled on single-stock-per-round as a core identity. This is not a balance decision — it is a fundamental design simplification.

### Existing Code to Understand

Before implementing, the dev agent MUST read:
- `Assets/Scripts/Setup/Data/StockTierData.cs` — stock counts per tier (BROKEN — needs fixing)
- `Assets/Scripts/Setup/Data/ExpansionDefinitions.cs` — Multi-Stock definition to remove
- `Assets/Scripts/Runtime/Core/GameStates/MarketOpenState.cs` — multi-stock check in `Enter()`
- `Assets/Scripts/Runtime/PriceEngine/PriceGenerator.cs` — `InitializeRound()` stockCountOverride
- `Assets/Scripts/Runtime/Events/EventScheduler.cs` — `FireEvent()` and `FireSectorRotation()` multi-stock paths
- `Assets/Scripts/Runtime/Core/GameRunner.cs` — sidebar logic, stock selection, `_multiStockActive`
- `Assets/Scripts/Setup/Data/GameConfig.cs` — `ExpansionCostMultiStock`

### Depends On

- Story 13.7 (Expansion Effects) must be complete — it IS (status: done)
- FIX-5 (Single Stock Per Round) original work is reference material
- FIX-9 (Scope Events to Single Stock) original work is reference material

### Risk

- **Low risk** — almost entirely subtractive (removing code and simplifying conditionals)
- **Regression concern:** Story 13.7 added multi-stock support to EventScheduler, GameRunner, PriceGenerator, MarketOpenState, and UISetup. All those additions need to be carefully unwound
- **Test concern:** Multi-stock expansion tests exist in `ExpansionEffectsTests.cs` — must be removed or updated, not left as false-passing stubs

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Completion Notes List

- AC1/AC3: All 4 tiers set to minStocksPerRound=1, maxStocksPerRound=1 in StockTierData.cs
- AC2/AC10/AC11: Multi-Stock Trading expansion removed from ExpansionDefinitions.All (5 expansions remain). ExpansionCostMultiStock removed from GameConfig. Const string `MultiStockTrading` retained for backward compatibility.
- AC4: MarketOpenState.Enter() no longer checks for multi-stock expansion — calls `InitializeRound(act, round)` with no override
- AC5: `stockCountOverride` parameter removed from PriceGenerator.InitializeRound() and SelectStocksForRound(). Tier data always drives count (1-1).
- AC6: EventScheduler.FireEvent() always targets `activeStocks[0].StockId` — removed random stock selection
- AC7: FireSectorRotation() simplified to single-stock only — removed multi-stock opposite-effect branch
- AC8/AC9: Removed `_stockSidebar`, `_multiStockActive`, `_selectedStockIndex` from GameRunner. Removed sidebar creation, StockSelectedEvent subscription, and OnStockSelected handler. GetSelectedStockId/GetSelectedTicker always use ActiveStocks[0].
- AC12: Updated 5 test files — removed multi-stock expansion tests, updated expansion counts (6→5), updated index references, updated targeting assertions to always-stock-0. Removed 5 multi-stock sector correlation tests from PriceGeneratorTests.
- AC13: Dual Short description updated from "Short a second stock simultaneously" to "Open a second short position simultaneously"

### Change Log

- 2026-02-16: FIX-15 implemented — permanently removed multi-stock support. All 8 tasks complete, all 13 ACs satisfied.

### File List

- `Assets/Scripts/Setup/Data/StockTierData.cs` (modified) — All 4 tiers set to minStocksPerRound=1, maxStocksPerRound=1
- `Assets/Scripts/Setup/Data/ExpansionDefinitions.cs` (modified) — Removed MultiStockTrading from All array, updated DualShort description
- `Assets/Scripts/Setup/Data/GameConfig.cs` (modified) — Removed ExpansionCostMultiStock constant
- `Assets/Scripts/Runtime/Core/GameStates/MarketOpenState.cs` (modified) — Removed multi-stock expansion check, simplified InitializeRound call
- `Assets/Scripts/Runtime/PriceEngine/PriceGenerator.cs` (modified) — Removed stockCountOverride parameter from InitializeRound() and SelectStocksForRound()
- `Assets/Scripts/Runtime/Events/EventScheduler.cs` (modified) — FireEvent always targets activeStocks[0], FireSectorRotation simplified to single-stock
- `Assets/Scripts/Runtime/Core/GameRunner.cs` (modified) — Removed _stockSidebar, _multiStockActive, _selectedStockIndex, sidebar creation, StockSelectedEvent sub, OnStockSelected. Simplified stock accessors.
- `Assets/Tests/Runtime/Shop/ExpansionEffectsTests.cs` (modified) — Replaced multi-stock tests with single-stock, updated expansion ID assertions
- `Assets/Tests/Runtime/PriceEngine/StockTierDataTests.cs` (modified) — Updated AllTiers_HaveMultipleStocksPerRound → AllTiers_HaveExactlyOneStockPerRound
- `Assets/Tests/Runtime/Events/EventSchedulerTests.cs` (modified) — Updated multi-stock targeting assertions to always-target-stock-0
- `Assets/Tests/Runtime/Shop/ExpansionManagerTests.cs` (modified) — Updated expansion count (6→5), removed multi-stock references, updated index references
- `Assets/Tests/Runtime/PriceEngine/PriceGeneratorTests.cs` (modified) — Removed 5 multi-stock sector correlation tests, replaced with single-stock validation test

## References

- FIX-5: `_bmad-output/implementation-artifacts/FIX-5-single-stock-per-round.md`
- FIX-9: `_bmad-output/implementation-artifacts/FIX-9-scope-events-to-single-stock.md`
- Story 13.7: `_bmad-output/implementation-artifacts/13-7-expansion-effects.md`
- `Assets/Scripts/Setup/Data/StockTierData.cs` — tier stock counts
- `Assets/Scripts/Setup/Data/ExpansionDefinitions.cs` — expansion registry
- `Assets/Scripts/Runtime/Core/GameStates/MarketOpenState.cs` — stock spawn logic
- `Assets/Scripts/Runtime/Events/EventScheduler.cs` — event targeting
- `Assets/Scripts/Runtime/Core/GameRunner.cs` — sidebar + stock selection
