# BullRun Context Handoff — 2026-02-13

## What Is This Project

BullRun is a real-time stock trading roguelike in **Unity 6.3 LTS** with URP 2D. "Balatro meets Wolf of Wall Street." Players buy/sell/short stocks on a live price chart across 8 rounds (4 acts, 2 rounds each), hitting escalating profit targets to avoid margin call. Between rounds, a draft shop offers upgrades. Architecture uses a **Setup-Oriented Generation Framework** where all game objects and UI are defined in C# code.

## Current Sprint: FIX Sprint (Bugs & Gameplay Critical Fixes)

**8 stories total. FIX-1 through FIX-3 done. FIX-4 in review. FIX-5 through FIX-8 ready for dev.**
**Recommended order for remaining: FIX-4 (finish review) → FIX-8 → FIX-5 → FIX-7 → FIX-6**

### FIX-1: Shop Click Fix & Timer Removal — DONE
**File:** `_bmad-output/implementation-artifacts/FIX-1-shop-click-and-timer.md`
**Status:** done

### FIX-2: Short Selling UI Bindings — DONE
**File:** `_bmad-output/implementation-artifacts/FIX-2-short-selling-ui.md`
**Status:** done

### FIX-3: Trade Quantity Selection — DONE
**File:** `_bmad-output/implementation-artifacts/FIX-3-trade-quantity-selection.md`
**Status:** done

### FIX-4: Event Pop-Up Display with Pause & Directional Fly — IN REVIEW
**File:** `_bmad-output/implementation-artifacts/FIX-4-event-popup-display.md`
**CRITICAL BUG:** NewsBanner, NewsTicker, and ScreenEffects are never instantiated. The setup methods exist in UISetup.cs and the code is correct, but GameRunner.Start() never calls:
- `UISetup.ExecuteNewsBanner()`
- `UISetup.ExecuteNewsTicker()`
- `UISetup.ExecuteScreenEffects()`
Events fire silently with zero visual feedback. Task 1 of this story is adding those 3 init calls.
**Enhancement:** After fixing init, add dramatic center-screen popup with brief pause (Time.timeScale=0) and directional fly animation (up for positive, down for negative events).
**Key files:** `Scripts/Runtime/Core/GameRunner.cs` (Start method), `Scripts/Setup/UISetup.cs` (~line 740 ExecuteNewsBanner, ~779 ExecuteNewsTicker, ~833 ExecuteScreenEffects), new `Scripts/Runtime/UI/EventPopup.cs`

### FIX-5: Single Stock Per Round — READY FOR DEV
**File:** `_bmad-output/implementation-artifacts/FIX-5-single-stock-per-round.md`
**Problem:** Currently 2-4 stocks per round with a left sidebar for switching. Design calls for 1 stock per round — simpler, more focused gameplay.
**Fix:** Set all StockTierData min/maxStocksPerRound to 1, remove StockSidebar, expand chart, auto-target single stock
**Key files:** `Scripts/Setup/Data/StockTierData.cs`, `Scripts/Runtime/UI/StockSidebar.cs`, `Scripts/Setup/UISetup.cs`, `Scripts/Runtime/Core/GameRunner.cs`, `Scripts/Setup/ChartSetup.cs`

### FIX-6: Trading Panel Overhaul — Buy/Sell Buttons — READY FOR DEV
**File:** `_bmad-output/implementation-artifacts/FIX-6-trading-panel-overhaul.md`
**Problem:** Trading uses 4 keyboard keys (B/S/D/F) requiring players to understand the long/short distinction. UX should be simplified to 2 smart buttons.
**Fix:** Large BUY/SELL buttons with smart routing (BUY auto-covers shorts, SELL auto-shorts when flat). Quantity presets changed to x5/x10/x15/x25. Replaces old QuantitySelector, KeyLegend, and D/F keybindings.
**Depends on:** FIX-5 (single stock simplifies targeting)
**Key files:** `Scripts/Runtime/UI/QuantitySelector.cs`, `Scripts/Setup/UISetup.cs`, `Scripts/Runtime/Core/GameRunner.cs`

### FIX-7: Current Position Overlay — Bottom-Left of Chart — READY FOR DEV
**File:** `_bmad-output/implementation-artifacts/FIX-7-position-overlay.md`
**Problem:** PositionPanel is a 180px right sidebar showing all positions — overkill for single-stock design.
**Fix:** Compact overlay on bottom-left of chart showing shares, direction (LONG/SHORT/FLAT), avg price, and real-time P&L. Remove old right sidebar.
**Key files:** `Scripts/Runtime/UI/PositionPanel.cs` (replaced), `Scripts/Setup/UISetup.cs`, `Scripts/Setup/ChartSetup.cs`

### FIX-8: Chart Price Gridlines — READY FOR DEV
**File:** `_bmad-output/implementation-artifacts/FIX-8-chart-price-gridlines.md`
**Problem:** Y-axis price labels exist but no horizontal lines extend across the chart, making it hard to judge price levels.
**Fix:** Add subtle horizontal LineRenderers at each of the 5 price label positions. Lines update dynamically with price range.
**Independent:** No dependencies on other FIX stories.
**Key files:** `Scripts/Setup/ChartSetup.cs`, `Scripts/Runtime/Chart/ChartLineView.cs`, `Scripts/Runtime/Chart/ChartUI.cs`

## Pre-Existing Known Bugs (from 2026-02-11 handoff, NOT in FIX sprint)

These bugs were identified earlier and are NOT addressed by the current FIX sprint:

1. **PriceGenerator creates stocks from ALL tiers** instead of current act's tier — `PriceGenerator.cs` lines 87-119
2. **Round Profit shows +$1,000** instead of $0 at start — `Portfolio.cs`, missing `StartRound()` call in GameRunner
3. **UI layout overlaps** — sidebar, positions panel, chart labels overlapping — `UISetup.cs`
4. **Chart line sometimes renders as straight line** — event ordering issue in ChartSetup.cs MarketOpenEvent handler
5. **MarketOpen scouting window not visible** — timing/occlusion issue in MarketOpenUI.cs
6. **Portfolio not subscribed to price updates** — missing `SubscribeToPriceUpdates()` call in GameRunner

See `_bmad-output/context-handoff-2026-02-11.md` for full details on these bugs.

## Completed Work (Epics 1-7)

All core gameplay loop is implemented and working:
- **Epic 1:** Price Engine (trend, noise, events, reversion, tiers, debug overlay)
- **Epic 2:** Trading System (buy, sell, short backend, portfolio, capital management)
- **Epic 3:** Chart Rendering (price chart, HUD, sidebar, positions panel)
- **Epic 4:** Round Management (timer, market open, auto-liquidation, margin call, transitions)
- **Epic 5:** Event System (scheduler, core/tier/global events, visual signals)
- **Epic 6:** Run Structure (act progression, tier transitions, profit targets, stock pools, win state)
- **Epic 7:** Draft Shop (shop UI, item pool generation, purchase flow, inventory display)

## Key Architecture Quick Reference

- **All UI programmatic** — uGUI Canvas, created in UISetup.cs, no Inspector config
- **EventBus** — static typed pub/sub, systems never reference each other
- **GameStateMachine** — IGameState (Enter/Update/Exit), flat state machine
- **RunContext** — carries all run data (act, round, cash, portfolio, items)
- **Static data** — `Scripts/Setup/Data/` as `public static readonly`, no ScriptableObjects
- **Input** — Unity Input System, `Keyboard.current.xKey.wasPressedThisFrame`
- **Tests** — Unity Test Framework, NEVER run via CLI, user runs manually in Editor

## File Structure

| System | Location |
|---|---|
| Core (state, events, runner) | `Scripts/Runtime/Core/` |
| Trading (executor, portfolio, position) | `Scripts/Runtime/Trading/` |
| Price Engine | `Scripts/Runtime/PriceEngine/` |
| Events (scheduler, effects) | `Scripts/Runtime/Events/` |
| Shop | `Scripts/Runtime/Shop/` |
| Items | `Scripts/Runtime/Items/` |
| Chart | `Scripts/Runtime/Chart/` |
| UI | `Scripts/Runtime/UI/` |
| Setup classes | `Scripts/Setup/` |
| Static data | `Scripts/Setup/Data/` |
| Tests | `Tests/Runtime/`, `Tests/Editor/` |
| Story files | `_bmad-output/implementation-artifacts/` |
| Planning docs | `_bmad-output/planning-artifacts/` |

## Critical Files for FIX Sprint

- `Scripts/Runtime/Core/GameRunner.cs` — main bootstrap, input handling, touched by FIX-1,2,3,4,5,6
- `Scripts/Setup/UISetup.cs` — all UI creation, touched by FIX-1,2,3,4,5,6,7
- `Scripts/Runtime/Core/GameStates/ShopState.cs` — shop state, touched by FIX-1
- `Scripts/Runtime/UI/ShopUI.cs` — shop UI logic, touched by FIX-1
- `Scripts/Runtime/Trading/TradeExecutor.cs` — trade execution, referenced by FIX-2,3,6
- `Scripts/Runtime/Trading/Portfolio.cs` — position management, referenced by FIX-2,3,6,7
- `Scripts/Setup/Data/GameConfig.cs` — game constants, touched by FIX-1,3
- `Scripts/Runtime/UI/NewsBanner.cs` — existing event display, referenced by FIX-4
- `Scripts/Runtime/UI/NewsTicker.cs` — existing event display, referenced by FIX-4
- `Scripts/Runtime/UI/ScreenEffects.cs` — existing screen effects, referenced by FIX-4
- `Scripts/Setup/Data/StockTierData.cs` — stock counts per tier, touched by FIX-5
- `Scripts/Runtime/UI/StockSidebar.cs` — stock sidebar (removed by FIX-5)
- `Scripts/Runtime/UI/QuantitySelector.cs` — quantity presets, touched by FIX-6
- `Scripts/Runtime/UI/PositionPanel.cs` — position display (replaced by FIX-7)
- `Scripts/Setup/ChartSetup.cs` — chart creation, touched by FIX-5,7,8
- `Scripts/Runtime/Chart/ChartLineView.cs` — chart rendering, touched by FIX-8
- `Scripts/Runtime/Chart/ChartUI.cs` — chart UI/labels, referenced by FIX-8

## Sprint Tracking

**Status file:** `_bmad-output/implementation-artifacts/sprint-status.yaml`
**Epics file:** `_bmad-output/planning-artifacts/epics.md`
**Project rules:** `_bmad-output/project-context.md` (MUST READ before implementing)
