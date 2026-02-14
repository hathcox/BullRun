# BullRun Context Handoff — 2026-02-13

## What Is This Project

BullRun is a real-time stock trading roguelike in **Unity 6.3 LTS** with URP 2D. "Balatro meets Wolf of Wall Street." Players buy/sell/short stocks on a live price chart across 8 rounds (4 acts, 2 rounds each), hitting escalating profit targets to avoid margin call. Between rounds, a draft shop offers upgrades. Architecture uses a **Setup-Oriented Generation Framework** where all game objects and UI are defined in C# code.

## Current Sprint: FIX Sprint (Bugs & Gameplay Critical Fixes)

**4 stories, all `ready-for-dev`, recommended order: FIX-1 → FIX-2 → FIX-3 → FIX-4**

### FIX-1: Shop Click Fix & Timer Removal
**File:** `_bmad-output/implementation-artifacts/FIX-1-shop-click-and-timer.md`
**Problems:**
- Shop item purchase buttons don't respond to clicks — button text labels have `raycastTarget = true`, intercepting pointer events before they reach the Button component
- Shop has an 18-second timer that auto-closes — should be untimed with a "Continue" button
**Key files:** `Scripts/Setup/UISetup.cs` (CreateItemCard ~line 1086), `Scripts/Runtime/Core/GameStates/ShopState.cs` (timer logic), `Scripts/Runtime/UI/ShopUI.cs`

### FIX-2: Short Selling UI Bindings
**File:** `_bmad-output/implementation-artifacts/FIX-2-short-selling-ui.md`
**Problem:** Short selling backend is 100% complete (Portfolio.OpenShort, CoverShort, TradeExecutor.ExecuteShort/ExecuteCover, 33 tests passing) but has ZERO keyboard bindings. GameRunner.cs only wires B=Buy and S=Sell.
**Fix:** Add D=Short, F=Cover keybindings + visual feedback
**Key files:** `Scripts/Runtime/Core/GameRunner.cs` (lines 119-159), `Scripts/Runtime/Trading/TradeExecutor.cs`, `Scripts/Runtime/Trading/Portfolio.cs`

### FIX-3: Trade Quantity Selection
**File:** `_bmad-output/implementation-artifacts/FIX-3-trade-quantity-selection.md`
**Problem:** All trades hardcoded to 10 shares in GameRunner.cs. TradeExecutor/Portfolio already accept variable quantities — just needs UI.
**Fix:** Quantity selector UI with presets (1x, 5x, 10x, MAX), Q key to cycle
**Key files:** `Scripts/Runtime/Core/GameRunner.cs` (hardcoded `10`), new `Scripts/Runtime/UI/QuantitySelector.cs`

### FIX-4: Event Pop-Up Display with Pause & Directional Fly
**File:** `_bmad-output/implementation-artifacts/FIX-4-event-popup-display.md`
**CRITICAL BUG:** NewsBanner, NewsTicker, and ScreenEffects are never instantiated. The setup methods exist in UISetup.cs and the code is correct, but GameRunner.Start() never calls:
- `UISetup.ExecuteNewsBanner()`
- `UISetup.ExecuteNewsTicker()`
- `UISetup.ExecuteScreenEffects()`
Events fire silently with zero visual feedback. Task 1 of this story is adding those 3 init calls.
**Enhancement:** After fixing init, add dramatic center-screen popup with brief pause (Time.timeScale=0) and directional fly animation (up for positive, down for negative events).
**Key files:** `Scripts/Runtime/Core/GameRunner.cs` (Start method), `Scripts/Setup/UISetup.cs` (~line 740 ExecuteNewsBanner, ~779 ExecuteNewsTicker, ~833 ExecuteScreenEffects), new `Scripts/Runtime/UI/EventPopup.cs`

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

- `Scripts/Runtime/Core/GameRunner.cs` — main bootstrap, input handling, touched by FIX-1,2,3,4
- `Scripts/Setup/UISetup.cs` — all UI creation, touched by FIX-1,2,3,4
- `Scripts/Runtime/Core/GameStates/ShopState.cs` — shop state, touched by FIX-1
- `Scripts/Runtime/UI/ShopUI.cs` — shop UI logic, touched by FIX-1
- `Scripts/Runtime/Trading/TradeExecutor.cs` — trade execution, referenced by FIX-2,3
- `Scripts/Runtime/Trading/Portfolio.cs` — position management, referenced by FIX-2,3
- `Scripts/Setup/Data/GameConfig.cs` — game constants, touched by FIX-1,3
- `Scripts/Runtime/UI/NewsBanner.cs` — existing event display, referenced by FIX-4
- `Scripts/Runtime/UI/NewsTicker.cs` — existing event display, referenced by FIX-4
- `Scripts/Runtime/UI/ScreenEffects.cs` — existing screen effects, referenced by FIX-4

## Sprint Tracking

**Status file:** `_bmad-output/implementation-artifacts/sprint-status.yaml`
**Epics file:** `_bmad-output/planning-artifacts/epics.md`
**Project rules:** `_bmad-output/project-context.md` (MUST READ before implementing)
