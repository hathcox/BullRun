# BullRun Context Handoff — 2026-02-11

## What Is This Project

BullRun is a real-time stock trading roguelike in **Unity 6.3 LTS** with URP 2D. Players buy/sell stocks on a live price chart across 8 rounds (4 acts, 2 rounds each), hitting escalating profit targets to avoid margin call. Architecture uses a **Setup-Oriented Generation Framework** where F5 rebuilds the scene from code.

## What Was Done This Session

1. **Created SetupPipeline infrastructure** (Story 0-1) — `SetupPhase.cs`, `SetupClassAttribute.cs`, `SetupPipeline.cs` (F5 hotkey + pipeline executor), `SceneSetup.cs` (camera + canvas)
2. **Created GameRunner bootstrap** (Story 0-2) — `GameRunner.cs` MonoBehaviour that creates systems at runtime and drives the game state machine
3. **Fixed Input System** — Migrated `DebugManager.cs` and `StockSidebar.cs` from legacy `Input.GetKeyDown` to `UnityEngine.InputSystem.Keyboard.current`
4. **Added `Unity.InputSystem` reference** to `BullRun.asmdef`
5. **Moved UI/Chart/Debug setup from F5 edit-time to runtime** — because MonoBehaviour private fields and EventBus subscriptions don't survive the edit→play serialization boundary
6. **Added basic keyboard trading** — B=Buy, S=Sell, 1-4=Select stock
7. **Added EventBus.Clear()** in GameRunner.Awake to prevent stale subscriptions across play sessions
8. **Wired StockSidebar** to PriceGenerator's active stocks via `InitializeForRound()`

## Current State — What Works

- F5 rebuilds scene (Camera + GameRunner only in scene file)
- GameRunner.Start() creates Chart, UI, Debug systems at runtime
- Game state machine runs: MarketOpen (7s) → Trading (60s) → MarketClose → MarginCall → Shop → loop
- Timer counts down and is visible
- TradingHUD shows (CASH, ROUND PROFIT, TARGET) at top
- Stock sidebar shows stock names and prices (left side)
- Chart line renders (sometimes — see bugs)
- Price engine generates stocks and updates prices per frame

## Known Bugs To Fix (Priority Order)

### 1. PriceGenerator creates stocks from ALL tiers instead of current act's tier
**File:** `Assets/Scripts/Runtime/PriceEngine/PriceGenerator.cs` lines 87-119
**Problem:** `InitializeRound(int act, int round)` loops through ALL `StockTier` enum values. Should only create stocks for the act's tier.
**Fix:** Use `RunContext.GetTierForAct(act)` or `GameConfig.Acts[act].Tier` to get the single tier, then only call `SelectStocksForRound()` for that tier.

### 2. Round Profit shows +$1,000 instead of $0 at start
**File:** `Assets/Scripts/Runtime/Trading/Portfolio.cs` lines 294-313
**Problem:** `GetRoundProfit()` returns `GetTotalValue() - _roundStartValue`. `StartRound(float startingValue)` is called with `Portfolio.Cash` ($1,000). But `GetTotalValue()` also returns $1,000 (cash only, no positions). However `_roundStartValue` is set to 0 initially (default float), so first round shows profit = $1,000 - 0 = $1,000.
**Fix:** In `GameRunner.Start()` or after `RunContext.StartNewRun()`, call `_ctx.Portfolio.StartRound(_ctx.Portfolio.Cash)` to set the baseline. Also need to call `_ctx.Portfolio.SubscribeToPriceUpdates()` so Portfolio tracks prices.

### 3. UI Layout — Everything Overlapping
**File:** `Assets/Scripts/Setup/UISetup.cs`
**Problems:**
- Stock sidebar entries (left) are squished — 4 entries at 80px each in a narrow 200px panel, text overlapping
- Positions panel (right) "No open positions" text overlaps with chart price axis labels
- Chart price labels overlap with positions panel
**Fix:** Increase sidebar width or reduce entry height. Add left margin to positions panel to avoid overlapping chart labels. Consider making sidebar entries show only ticker + price (drop sparkline for now). Push chart price labels left of the positions panel.

### 4. Chart Line Sometimes Renders as Straight Line
**Files:** `Assets/Scripts/Setup/ChartSetup.cs`, `Assets/Scripts/Runtime/Chart/ChartRenderer.cs`
**Problem:** Event ordering issue. `MarketOpenEvent` handler calls `SetActiveStock()` which calls `ResetChart()` (sets `_roundActive=false`). If this fires after `RoundStartedEvent` (which calls `StartRound()` setting `_roundActive=true`), the chart stops accepting points.
**Fix:** In ChartSetup's `MarketOpenEvent` handler, don't call `SetActiveStock()` (which resets chart). Instead just set `_activeStockId` directly without resetting. Or better: ensure event ordering — MarketOpenEvent always fires before RoundStartedEvent (which it does, but SetActiveStock's ResetChart is the issue).
**Proposed fix in ChartSetup.cs:**
```csharp
EventBus.Subscribe<MarketOpenEvent>(evt =>
{
    if (evt.StockIds != null && evt.StockIds.Length > 0)
    {
        // Don't call SetActiveStock — it resets roundActive.
        // Just set the ID so ProcessPriceUpdate knows which stock to track.
        chartRenderer.SetActiveStockId(evt.StockIds[0]); // Need to add this method
    }
});
```
Add to ChartRenderer:
```csharp
public void SetActiveStockId(int stockId) { _activeStockId = stockId; }
```

### 5. MarketOpen Scouting Window Not Visible
**File:** `Assets/Scripts/Runtime/UI/MarketOpenUI.cs`
**Problem:** MarketOpenUI subscribes to `MarketOpenEvent` in `Initialize()` and shows a full-screen overlay with stocks, headline, target, and countdown. The overlay IS created at runtime but may not be showing because:
- The MarketOpenEvent fires during `GameRunner.Start()` BEFORE the MarketOpenUI is created (UISetup.ExecuteMarketOpenUI runs after the state transition)
- Fix: Move `_stateMachine.TransitionTo<MarketOpenState>()` to AFTER all UI is created

**Current order in GameRunner.Start():**
```
ChartSetup.Execute()
UISetup.Execute(...)          // TradingHUD
UISetup.ExecuteMarketOpenUI() // MarketOpenUI subscribes to MarketOpenEvent
UISetup.ExecuteSidebar()
...
TransitionTo<MarketOpenState>() // Publishes MarketOpenEvent — UI IS subscribed ✓
```
Wait, the order is actually correct. The issue might be that `_panel.SetActive(false)` in Initialize hides it, then MarketOpenEvent shows it, but the panel might be occluded. Need to debug.

### 6. Portfolio Not Subscribed to Price Updates
**File:** `Assets/Scripts/Runtime/Core/GameRunner.cs`
**Problem:** `Portfolio.SubscribeToPriceUpdates()` is never called. The Portfolio has this method to subscribe to PriceUpdatedEvent for caching prices (needed by GetTotalValue, GetRoundProfit, PositionPanel).
**Fix:** Add `_ctx.Portfolio.SubscribeToPriceUpdates()` in GameRunner.Awake() or Start().

## Key Architecture Notes for New Context

### File Locations
- **Setup classes:** `Assets/Scripts/Setup/` — static classes with `Execute()` methods, called at runtime by GameRunner
- **Runtime code:** `Assets/Scripts/Runtime/` — Core, UI, Chart, Trading, PriceEngine, Events
- **Editor code:** `Assets/Scripts/Editor/` — SetupPipeline.cs, DebugManager.cs
- **Data classes:** `Assets/Scripts/Setup/Data/` — GameConfig, StockTierData, MarginCallTargets, etc.
- **Implementation artifacts:** `_bmad-output/implementation-artifacts/`
- **Planning docs:** `_bmad-output/planning-artifacts/` — epics.md, game-architecture.md, bull-run-gdd-mvp.md

### Key Patterns
- **All UI created programmatically** — no Inspector configuration, no prefabs
- **EventBus** — static typed pub/sub (`EventBus.Subscribe<T>`, `EventBus.Publish<T>`)
- **State machine** — `GameStateMachine` with `IGameState` (Enter/Update/Exit). States use `static NextConfig` pattern to pass dependencies
- **One assembly** — `BullRun.asmdef` covers all of `Assets/Scripts/`. Editor code uses `#if UNITY_EDITOR` guards
- **Input System package** — NOT legacy Input. Use `Keyboard.current.xKey.wasPressedThisFrame`
- **Setup classes are runtime-only** — NOT called during F5 anymore. MonoBehaviour Initialize() and EventBus subscriptions must happen at runtime

### GameRunner.Start() Execution Order
```
EventBus.Clear()
RunContext.StartNewRun() → $1,000 capital
ChartSetup.Execute() → chart system + EventBus subscriptions
UISetup.Execute(ctx, round, duration) → TradingHUD (top bar)
UISetup.ExecuteMarketOpenUI() → market open overlay
UISetup.ExecuteSidebar() → stock sidebar (left)
UISetup.ExecutePositionsPanel(portfolio) → positions panel (right)
UISetup.ExecuteRoundTimer() → countdown timer
DebugSetup.Execute() → F1 debug overlay
TransitionTo<MarketOpenState>() → publishes MarketOpenEvent, initializes stocks
sidebarData.InitializeForRound(stocks) → populates sidebar
```

### Game State Flow
```
MetaHub (skipped) → MarketOpen (7s preview) → Trading (60s) → MarketClose (2s liquidation)
→ MarginCall (check profit vs target) → pass: Shop (auto-skip) → next round
                                        → fail: RunSummary (game over)
After round 8: RunSummary (victory)
```

## Files Changed This Session
- `Assets/Scripts/Setup/SetupPhase.cs` (NEW)
- `Assets/Scripts/Setup/SetupClassAttribute.cs` (NEW)
- `Assets/Scripts/Editor/SetupPipeline.cs` (NEW)
- `Assets/Scripts/Setup/SceneSetup.cs` (NEW)
- `Assets/Scripts/Setup/GameRunnerSetup.cs` (NEW)
- `Assets/Scripts/Runtime/Core/GameRunner.cs` (NEW)
- `Assets/Scripts/Setup/ChartSetup.cs` (MODIFIED — attribute commented out, MarketOpenEvent subscription added)
- `Assets/Scripts/Setup/UISetup.cs` (MODIFIED — attribute commented out, parameterless Execute() added)
- `Assets/Scripts/Setup/DebugSetup.cs` (MODIFIED — attribute commented out)
- `Assets/Scripts/Editor/DebugManager.cs` (MODIFIED — Input System migration)
- `Assets/Scripts/Runtime/UI/StockSidebar.cs` (MODIFIED — Input System migration)
- `Assets/Scripts/Runtime/Chart/ChartVisualConfig.cs` (MODIFIED — thicker line width)
- `Assets/Scripts/BullRun.asmdef` (MODIFIED — added Unity.InputSystem reference)
