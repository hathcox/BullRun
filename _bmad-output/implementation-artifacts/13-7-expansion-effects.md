# Story 13.7: Expansion Effects Integration

Status: done

## Story

As a player,
I want purchased Trading Deck Expansions to actually modify gameplay mechanics (multi-stock, leverage, extended timer, etc.),
so that my store investments have tangible impact during trading rounds.

## Acceptance Criteria

1. **Multi-Stock Trading:** MarketOpenState spawns 2 stocks when owned. Stock sidebar re-enabled. All trading/event systems handle 2 stocks.
2. **Leverage Trading:** TradeExecutor applies 2x multiplier to long trade P&L when owned. Visual indicator on trade panel ("2x LEVERAGE").
3. **Expanded Inventory:** MaxRelicSlots increased by 2 (5 → 7). Shop relic purchase buttons re-enabled if previously at capacity.
4. **Dual Short:** Short state machine allows 2 concurrent short positions. Second short button appears in UI.
5. **Intel Expansion:** InsiderTipSlots increased from 2 to 3. Third mystery card appears in Insider Tips panel.
6. **Extended Trading:** RoundDurationSeconds increased by 15. Timer UI reflects new duration. Stacks up to 2x.
7. Each expansion checks RunContext.OwnedExpansions at the appropriate system entry point
8. Expansions do NOT stack with themselves (one-time purchase, one effect per type)

## Tasks / Subtasks

- [x] Task 1: Multi-Stock Trading effect (AC: 1)
  - [x] `MarketOpenState.Enter()`: check for multi-stock expansion → spawn 2 stocks from tier pool instead of 1
  - [x] Re-enable stock selection sidebar (removed in FIX-5) when 2 stocks active
  - [x] Event system: ensure events work correctly with 2 active stocks
  - [x] Trading: ensure buy/sell/short target the selected stock
  - [x] Files: `Scripts/Runtime/Core/GameStates/MarketOpenState.cs`, `Scripts/Setup/UISetup.cs`, `Scripts/Runtime/Events/EventScheduler.cs`
- [x] Task 2: Leverage Trading effect (AC: 2)
  - [x] `TradeExecutor`: when calculating long trade P&L, multiply by 2 if leverage expansion owned
  - [x] Leverage applies to BOTH gains and losses (double-edged)
  - [x] Visual indicator on trade panel: "2x LEVERAGE" badge when active
  - [x] Does NOT affect short trades (shorts have their own mechanics)
  - [x] Files: `Scripts/Runtime/Trading/TradeExecutor.cs`, `Scripts/Runtime/UI/TradingHUD.cs`
- [x] Task 3: Expanded Inventory effect (AC: 3)
  - [x] When checking relic capacity, compute effective max: `GameConfig.MaxRelicSlots + (hasExpandedInventory ? 2 : 0)`
  - [x] ShopUI relic panel: re-enable purchase buttons if under new capacity
  - [x] ItemInventoryPanel: display area accommodates up to 7 items
  - [x] Files: `Scripts/Runtime/Shop/ShopTransaction.cs`, `Scripts/Runtime/UI/ShopUI.cs`, `Scripts/Runtime/UI/ItemInventoryPanel.cs`
- [x] Task 4: Dual Short effect (AC: 4)
  - [x] Short state machine: allow 2 independent short positions when expansion owned
  - [x] Second SHORT button appears in UI (or same button allows opening second short)
  - [x] Each short has independent lifecycle (hold timer, cash-out window, cooldown)
  - [x] P&L tracked separately per short position
  - [x] Files: `Scripts/Runtime/Core/GameRunner.cs`, `Scripts/Setup/UISetup.cs`, `Scripts/Runtime/UI/PositionOverlay.cs`
- [x] Task 5: Intel Expansion effect (AC: 5)
  - [x] Update `RunContext.InsiderTipSlots` from 2 to 3 when expansion purchased
  - [x] ShopUI Insider Tips panel: render 3rd mystery card slot when slots = 3
  - [x] InsiderTipGenerator: generate 3 tips instead of 2 when slots increased
  - [x] Files: `Scripts/Runtime/UI/ShopUI.cs`, `Scripts/Runtime/Shop/InsiderTipGenerator.cs`
- [x] Task 6: Extended Trading effect (AC: 6)
  - [x] When entering TradingState: check for extended trading expansion → add 15s to round duration
  - [x] Timer UI reflects extended duration
  - [x] Expansion notes say "stacks up to 2x" — but expansions can only be purchased once, so this means if combined with a future item that also extends time. For now: +15s, one-time.
  - [x] Files: `Scripts/Runtime/Core/GameStates/TradingState.cs` or `Scripts/Runtime/Core/GameRunner.cs`
- [x] Task 7: ExpansionManager effect application (AC: 7, 8)
  - [x] `ExpansionManager.HasExpansion(string expansionId)` — convenience method used by all systems
  - [x] Each system checks at its entry point (state Enter, trade execution, shop rendering)
  - [x] Guard against double-application: effects are stateless checks, not cumulative modifiers
  - [x] File: `Scripts/Runtime/Shop/ExpansionManager.cs`
- [x] Task 8: Write tests (All AC)
  - [x] Multi-Stock: 2 stocks spawned when expansion owned, 1 without
  - [x] Leverage: P&L doubled on long trades, unaffected on shorts
  - [x] Expanded Inventory: capacity correctly increased, purchase re-enabled
  - [x] Dual Short: 2 shorts possible, independent lifecycles
  - [x] Intel Expansion: 3 tip slots when owned
  - [x] Extended Trading: round duration increased by 15s
  - [x] No stacking: purchasing same expansion twice has no additional effect
  - [x] Files: `Tests/Runtime/Shop/ExpansionEffectsTests.cs`

## Dev Notes

### Architecture Compliance

- **Stateless checks:** Each system checks `ExpansionManager.HasExpansion()` at runtime. No persistent modifier state — just "is it owned? then apply effect."
- **No ScriptableObjects:** Expansion IDs are string constants defined in `ExpansionDefinitions.cs`.
- **Entry point checks:** Effects are checked at system entry points (state Enter methods, trade execution), not via a global modifier system.

### Complexity Warning

This is the **most integration-heavy story** in Epic 13. Each expansion touches a different system:
- Multi-Stock reverses FIX-5 (conditionally) — be very careful about the single-stock assumptions baked into events, trading, and UI
- Leverage modifies TradeExecutor — ensure the multiplier only applies to realized P&L, not to position cost
- Dual Short adds a second short state machine — significant complexity in the short lifecycle
- Extended Trading must not break auto-liquidation timing

### Multi-Stock is the Riskiest

FIX-5 explicitly removed multi-stock support and simplified many systems. Re-enabling it conditionally means:
- Stock sidebar must be conditionally shown/hidden
- Event targeting must work with 1 or 2 stocks
- Trade execution must know which stock is selected
- All this was working before FIX-5, so the old code patterns can be referenced

### Existing Code to Understand

Before implementing, the dev agent MUST read:
- `Scripts/Runtime/Core/GameStates/MarketOpenState.cs` — stock spawning
- `Scripts/Runtime/Trading/TradeExecutor.cs` — P&L calculation
- `Scripts/Runtime/Core/GameRunner.cs` — short state machine, trade execution, round timing
- `Scripts/Setup/UISetup.cs` — stock sidebar, short button, trade panel
- `Scripts/Runtime/Events/EventScheduler.cs` — event targeting
- `Scripts/Runtime/Shop/ExpansionManager.cs` — created in 13.4

### Depends On

- Story 13.4 (Trading Deck Expansions) — ExpansionManager and definitions must exist
- Story 13.1 (Data Model) — OwnedExpansions in RunContext

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Completion Notes List

- Task 3 (Expanded Inventory) was already implemented by `ShopTransaction.GetEffectiveMaxRelicSlots()` from Story 13.3 — no additional code changes needed.
- Leverage P&L bonus is applied in `GameRunner.ExecuteSell()` via `Portfolio.AddCash(pnl)` because `Portfolio.ClosePosition()` doesn't have access to RunContext. This keeps the leverage effect at the GameRunner orchestration layer.
- Dual Short uses duplicated state machine fields (`_short2State`, `_short2Timer`, etc.) rather than refactoring into a reusable class. This was a deliberate safety choice — lower refactoring risk for the existing short lifecycle.
- Multi-Stock conditionally re-enables the StockSidebar that was hidden by FIX-5. All trading operations (buy/sell/short) now use `GetSelectedStockId()` / `GetSelectedTicker()` which respect sidebar selection.
- Event targeting updated to randomly select target stock when multiple stocks are active, rather than hardcoding index 0.
- **Tests cannot be run from CLI** per project rules. All 24 tests written in `ExpansionEffectsTests.cs` must be validated manually in Unity Editor.

### Change Log

- `Assets/Scripts/Runtime/PriceEngine/PriceGenerator.cs` — Added `stockCountOverride` parameter to `InitializeRound()` and `SelectStocksForRound()`
- `Assets/Scripts/Runtime/Core/GameStates/MarketOpenState.cs` — Checks multi-stock expansion to pass stockCount=2
- `Assets/Scripts/Runtime/Core/GameRunner.cs` — Multi-stock sidebar, stock selection, leverage 2x P&L, dual short state machine (Short2), second short UI wiring
- `Assets/Scripts/Runtime/Events/EventScheduler.cs` — Random stock targeting in `FireEvent()` and multi-stock `FireSectorRotation()`
- `Assets/Scripts/Runtime/UI/QuantitySelector.cs` — Added LeverageBadge, Short2* UI property refs
- `Assets/Scripts/Setup/UISetup.cs` — Created leverage badge UI, second SHORT button + P&L panel
- `Assets/Scripts/Runtime/Core/GameStates/TradingState.cs` — Extended trading +15s in `Enter()`
- `Assets/Scripts/Runtime/Core/GameStates/ShopState.cs` — Intel expansion tip slots calculation in `Enter()`
- `Assets/Scripts/Runtime/Shop/ExpansionManager.cs` — Added `HasExpansion()` instance and static convenience methods
- `Assets/Tests/Runtime/Shop/ExpansionEffectsTests.cs` — Created: 24 tests covering all 8 ACs
- `Assets/Scripts/Setup/Data/ExpansionDefinitions.cs` — Added `const string` ID constants for compile-time safety
- `Assets/Scripts/Runtime/Shop/ShopTransaction.cs` — Updated to use ExpansionDefinitions constants

#### Code Review Fixes (2026-02-16)

- **[C1+C2] Fixed short position price bug**: Added `_shortOpenStockId` and `_short2OpenStockId` int fields to track the numeric stock ID that a short was opened on. All close/P&L display methods now use the correct stock's price instead of the currently selected stock (which could differ in multi-stock mode).
- **[H1] Improved test quality**: Replaced trivial math-only tests (Leverage, ExtendedTrading) with tests that exercise actual Portfolio.AddCash behavior and simulate real expansion check logic. Added `MultiStock_StocksAreDistinct`, `IntelExpansion_SlotCountUnchanged_WithoutExpansion`, `ExtendedTrading_NoDurationChange_WithoutExpansion`, and `ExpansionDefinitions_AllIdsMatchConstants` tests.
- **[H2] Centralized expansion IDs**: Added `const string` constants to `ExpansionDefinitions` (e.g., `MultiStockTrading`, `LeverageTrading`). Updated all callsites across 6 files to use constants instead of raw strings.
- **[M3] Added null position guard**: `ExecuteSell()` leverage bonus now skipped when `position` is null, preventing unearned cash bonus on edge-case sells.

### File List

- `Assets/Scripts/Runtime/PriceEngine/PriceGenerator.cs`
- `Assets/Scripts/Runtime/Core/GameStates/MarketOpenState.cs`
- `Assets/Scripts/Runtime/Core/GameRunner.cs`
- `Assets/Scripts/Runtime/Events/EventScheduler.cs`
- `Assets/Scripts/Runtime/UI/QuantitySelector.cs`
- `Assets/Scripts/Setup/UISetup.cs`
- `Assets/Scripts/Runtime/Core/GameStates/TradingState.cs`
- `Assets/Scripts/Runtime/Core/GameStates/ShopState.cs`
- `Assets/Scripts/Runtime/Shop/ExpansionManager.cs`
- `Assets/Scripts/Runtime/Shop/ShopTransaction.cs`
- `Assets/Scripts/Setup/Data/ExpansionDefinitions.cs`
- `Assets/Tests/Runtime/Shop/ExpansionEffectsTests.cs`
