# Story 13.7: Expansion Effects Integration

Status: pending

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

- [ ] Task 1: Multi-Stock Trading effect (AC: 1)
  - [ ] `MarketOpenState.Enter()`: check for multi-stock expansion → spawn 2 stocks from tier pool instead of 1
  - [ ] Re-enable stock selection sidebar (removed in FIX-5) when 2 stocks active
  - [ ] Event system: ensure events work correctly with 2 active stocks
  - [ ] Trading: ensure buy/sell/short target the selected stock
  - [ ] Files: `Scripts/Runtime/Core/GameStates/MarketOpenState.cs`, `Scripts/Setup/UISetup.cs`, `Scripts/Runtime/Events/EventScheduler.cs`
- [ ] Task 2: Leverage Trading effect (AC: 2)
  - [ ] `TradeExecutor`: when calculating long trade P&L, multiply by 2 if leverage expansion owned
  - [ ] Leverage applies to BOTH gains and losses (double-edged)
  - [ ] Visual indicator on trade panel: "2x LEVERAGE" badge when active
  - [ ] Does NOT affect short trades (shorts have their own mechanics)
  - [ ] Files: `Scripts/Runtime/Trading/TradeExecutor.cs`, `Scripts/Runtime/UI/TradingHUD.cs`
- [ ] Task 3: Expanded Inventory effect (AC: 3)
  - [ ] When checking relic capacity, compute effective max: `GameConfig.MaxRelicSlots + (hasExpandedInventory ? 2 : 0)`
  - [ ] ShopUI relic panel: re-enable purchase buttons if under new capacity
  - [ ] ItemInventoryPanel: display area accommodates up to 7 items
  - [ ] Files: `Scripts/Runtime/Shop/ShopTransaction.cs`, `Scripts/Runtime/UI/ShopUI.cs`, `Scripts/Runtime/UI/ItemInventoryPanel.cs`
- [ ] Task 4: Dual Short effect (AC: 4)
  - [ ] Short state machine: allow 2 independent short positions when expansion owned
  - [ ] Second SHORT button appears in UI (or same button allows opening second short)
  - [ ] Each short has independent lifecycle (hold timer, cash-out window, cooldown)
  - [ ] P&L tracked separately per short position
  - [ ] Files: `Scripts/Runtime/Core/GameRunner.cs`, `Scripts/Setup/UISetup.cs`, `Scripts/Runtime/UI/PositionOverlay.cs`
- [ ] Task 5: Intel Expansion effect (AC: 5)
  - [ ] Update `RunContext.InsiderTipSlots` from 2 to 3 when expansion purchased
  - [ ] ShopUI Insider Tips panel: render 3rd mystery card slot when slots = 3
  - [ ] InsiderTipGenerator: generate 3 tips instead of 2 when slots increased
  - [ ] Files: `Scripts/Runtime/UI/ShopUI.cs`, `Scripts/Runtime/Shop/InsiderTipGenerator.cs`
- [ ] Task 6: Extended Trading effect (AC: 6)
  - [ ] When entering TradingState: check for extended trading expansion → add 15s to round duration
  - [ ] Timer UI reflects extended duration
  - [ ] Expansion notes say "stacks up to 2x" — but expansions can only be purchased once, so this means if combined with a future item that also extends time. For now: +15s, one-time.
  - [ ] Files: `Scripts/Runtime/Core/GameStates/TradingState.cs` or `Scripts/Runtime/Core/GameRunner.cs`
- [ ] Task 7: ExpansionManager effect application (AC: 7, 8)
  - [ ] `ExpansionManager.HasExpansion(string expansionId)` — convenience method used by all systems
  - [ ] Each system checks at its entry point (state Enter, trade execution, shop rendering)
  - [ ] Guard against double-application: effects are stateless checks, not cumulative modifiers
  - [ ] File: `Scripts/Runtime/Shop/ExpansionManager.cs`
- [ ] Task 8: Write tests (All AC)
  - [ ] Multi-Stock: 2 stocks spawned when expansion owned, 1 without
  - [ ] Leverage: P&L doubled on long trades, unaffected on shorts
  - [ ] Expanded Inventory: capacity correctly increased, purchase re-enabled
  - [ ] Dual Short: 2 shorts possible, independent lifecycles
  - [ ] Intel Expansion: 3 tip slots when owned
  - [ ] Extended Trading: round duration increased by 15s
  - [ ] No stacking: purchasing same expansion twice has no additional effect
  - [ ] Files: `Tests/Runtime/Shop/ExpansionEffectsTests.cs`

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
- `Scripts/Runtime/Shop/ExpansionManager.cs` — created in 13.3

### Depends On

- Story 13.3 (Trading Deck Expansions) — ExpansionManager and definitions must exist
- Story 13.6 (Data Model) — OwnedExpansions in RunContext

## Dev Agent Record

### Agent Model Used

### Completion Notes List

### Change Log

### File List
