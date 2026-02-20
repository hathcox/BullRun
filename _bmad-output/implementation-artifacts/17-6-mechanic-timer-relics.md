# Story 17.6: Mechanic & Timer Relics (5 Relics)

Status: ready-for-dev

## Story

As a player,
I want mechanic-altering and timer-manipulating relics that fundamentally change how core systems work,
so that I can build powerful run-defining strategies around game mechanic modifications.

## Acceptance Criteria

1. **Time Buyer** (`relic_time_buyer`): buying a stock extends the active round timer by 5 seconds; no cap on extensions; timer UI updates automatically since it reads `TradingState.ActiveTimeRemaining`
2. **Diamond Hands** (`relic_diamond_hands`): stocks held to round end gain 30% value before auto-liquidation; only long positions affected (not shorts); implemented via `RelicManager.GetLiquidationMultiplier()` checked by `MarketCloseState`
3. **Market Manipulator** (`relic_market_manipulator`): selling a stock (non-short) causes its price to drop 15% after the sell executes; the sell itself executes at the pre-drop price (player gets full sell proceeds); creates a buy-the-dip opportunity
4. **Diamond Hands** does NOT affect short positions — only long positions multiply by 1.30f
5. **Free Intel** (`relic_free_intel`): one insider tip is free per shop visit; on shop open, sets `RunContext.FreeIntelThisVisit = true`; first tip purchase checks this flag and costs 0 if true; the free tip slot shows a "FREE" label in the shop
6. **Extra Expansion** (`relic_extra_expansion`): one extra expansion offered per shop visit; on shop open, increments `RunContext.BonusExpansionSlots`; `ShopState` uses this when generating expansion offerings (normally 2-3, now 3-4)
7. All 5 relics extend `RelicBase` and override only the hooks they need
8. All 5 relics publish `RelicActivatedEvent` when their effect fires (for UI glow in Story 17.8)
9. All 5 relics are registered in `RelicFactory` with their proper constructors (replacing StubRelic entries)
10. `RunContext.FreeIntelThisVisit` resets to `false` each shop visit (set in `ShopState.Enter`)
11. `RunContext.BonusExpansionSlots` resets to `0` each shop visit (set in `ShopState.Enter`)
12. Existing tests continue to pass after modifications to `MarketCloseState`, `ShopState`, `ShopTransaction`, and `RunContext`

## Tasks / Subtasks

- [ ] Task 1: Add new RunContext fields (AC: 5, 6, 10, 11)
  - [ ] Add `bool FreeIntelThisVisit` property to `RunContext` (default false)
  - [ ] Add `int BonusExpansionSlots` property to `RunContext` (default 0)
  - [ ] Reset both fields in `ResetForNewRun()`
  - [ ] File: `Assets/Scripts/Runtime/Core/RunContext.cs`
- [ ] Task 2: Create TimeBuyerRelic (AC: 1, 7, 8)
  - [ ] Create `Assets/Scripts/Runtime/Items/Relics/TimeBuyerRelic.cs`
  - [ ] Override `Id` => `"relic_time_buyer"`
  - [ ] Override `OnAfterTrade(RunContext ctx, TradeExecutedEvent e)` — check `e.IsBuy && !e.IsShort`
  - [ ] Add 5f to the active round timer in `TradingState` (requires a public `ExtendTimer(float seconds)` method on TradingState or a static setter)
  - [ ] Publish `RelicActivatedEvent` with `RelicId = Id`
  - [ ] Log: `[TimeBuyerRelic] Extended round timer by 5s`
- [ ] Task 3: Add timer extension support to TradingState (AC: 1)
  - [ ] Add `public static void ExtendTimer(float seconds)` method to `TradingState`
  - [ ] Method adds seconds to `_timeRemaining` and updates `ActiveTimeRemaining`
  - [ ] Only applies when `IsActive` is true (no-op otherwise)
  - [ ] File: `Assets/Scripts/Runtime/Core/GameStates/TradingState.cs`
- [ ] Task 4: Create DiamondHandsRelic (AC: 2, 4, 7, 8)
  - [ ] Create `Assets/Scripts/Runtime/Items/Relics/DiamondHandsRelic.cs`
  - [ ] Override `Id` => `"relic_diamond_hands"`
  - [ ] No hook override needed — effect is passive, checked by MarketCloseState via `RelicManager.GetLiquidationMultiplier()`
  - [ ] Add `GetLiquidationMultiplier()` method to `RelicManager` — returns 1.30f if diamond_hands is owned, else 1.0f
- [ ] Task 5: Modify MarketCloseState for Diamond Hands liquidation boost (AC: 2, 4)
  - [ ] Before calling `LiquidateAllPositions()`, check `ctx.RelicManager.GetLiquidationMultiplier()`
  - [ ] If multiplier > 1.0f, multiply all long position values by the multiplier before liquidation
  - [ ] Implementation: add cash bonus after liquidation based on (multiplier - 1.0f) * sum of long position values at liquidation prices
  - [ ] Short positions are NOT affected (AC 4)
  - [ ] Publish `RelicActivatedEvent` for diamond_hands if multiplier was applied
  - [ ] File: `Assets/Scripts/Runtime/Core/GameStates/MarketCloseState.cs`
- [ ] Task 6: Create MarketManipulatorRelic (AC: 3, 7, 8)
  - [ ] Create `Assets/Scripts/Runtime/Items/Relics/MarketManipulatorRelic.cs`
  - [ ] Override `Id` => `"relic_market_manipulator"`
  - [ ] Override `OnAfterTrade(RunContext ctx, TradeExecutedEvent e)` — check `!e.IsBuy && !e.IsShort` (long sell only)
  - [ ] Apply -15% to current stock price via `PriceGenerator.ApplyPriceMultiplier(string stockId, float multiplier)` — new method
  - [ ] Sell already executed at pre-drop price (event fires after trade)
  - [ ] Publish `RelicActivatedEvent`
  - [ ] Log: `[MarketManipulatorRelic] Price dropped 15% after sell`
- [ ] Task 7: Add price manipulation support to PriceGenerator (AC: 3)
  - [ ] Add `public void ApplyPriceMultiplier(string stockId, float multiplier)` method
  - [ ] Method finds the active stock by ID and multiplies its `CurrentPrice` by the multiplier
  - [ ] Publishes `PriceUpdatedEvent` after manipulation so chart reflects the change
  - [ ] File: `Assets/Scripts/Runtime/PriceEngine/PriceGenerator.cs`
- [ ] Task 8: Create FreeIntelRelic (AC: 5, 7, 8, 10)
  - [ ] Create `Assets/Scripts/Runtime/Items/Relics/FreeIntelRelic.cs`
  - [ ] Override `Id` => `"relic_free_intel"`
  - [ ] Override `OnShopOpen(RunContext ctx)` — set `ctx.FreeIntelThisVisit = true`
  - [ ] Publish `RelicActivatedEvent`
  - [ ] Log: `[FreeIntelRelic] Free intel flag set for this shop visit`
- [ ] Task 9: Modify ShopTransaction.PurchaseTip for free intel (AC: 5)
  - [ ] In `PurchaseTip()`, before checking `CanAfford(cost)`: if `ctx.FreeIntelThisVisit` is true and no tips have been purchased this visit (`ctx.RevealedTips.Count == 0`), override cost to 0
  - [ ] After the free tip is consumed, set `ctx.FreeIntelThisVisit = false` (one-time per visit)
  - [ ] File: `Assets/Scripts/Runtime/Shop/ShopTransaction.cs`
- [ ] Task 10: Modify ShopUI for "FREE" label on first tip slot (AC: 5)
  - [ ] In tip panel rendering: if `ctx.FreeIntelThisVisit` is true and first tip slot is unpurchased, show "FREE" text instead of cost
  - [ ] File: `Assets/Scripts/Runtime/UI/ShopUI.cs`
- [ ] Task 11: Create ExtraExpansionRelic (AC: 6, 7, 8, 11)
  - [ ] Create `Assets/Scripts/Runtime/Items/Relics/ExtraExpansionRelic.cs`
  - [ ] Override `Id` => `"relic_extra_expansion"`
  - [ ] Override `OnShopOpen(RunContext ctx)` — increment `ctx.BonusExpansionSlots` by 1
  - [ ] Publish `RelicActivatedEvent`
  - [ ] Log: `[ExtraExpansionRelic] Bonus expansion slot added for this shop visit`
- [ ] Task 12: Modify ShopState for bonus expansion slots (AC: 6, 10, 11)
  - [ ] In `ShopState.Enter()`: reset `ctx.FreeIntelThisVisit = false` and `ctx.BonusExpansionSlots = 0` BEFORE dispatching `DispatchShopOpen`
  - [ ] After relic dispatch sets the flags, use `GameConfig.ExpansionsPerShopVisit + ctx.BonusExpansionSlots` when calling `_expansionManager.GetAvailableForShop()`
  - [ ] File: `Assets/Scripts/Runtime/Core/GameStates/ShopState.cs`
- [ ] Task 13: Update RelicFactory registrations (AC: 9)
  - [ ] Replace StubRelic entries for `relic_time_buyer`, `relic_diamond_hands`, `relic_market_manipulator`, `relic_free_intel`, `relic_extra_expansion` with real constructors
  - [ ] File: `Assets/Scripts/Runtime/Items/RelicFactory.cs`
- [ ] Task 14: Write tests (AC: 1-12)
  - [ ] TimeBuyerRelic: verify timer extension on buy trade, no extension on sell/short
  - [ ] DiamondHandsRelic: verify GetLiquidationMultiplier returns 1.30f when owned, 1.0f otherwise
  - [ ] MarketManipulatorRelic: verify price drops 15% after long sell, not on buy or short
  - [ ] FreeIntelRelic: verify first tip is free, second tip costs normal, flag resets per visit
  - [ ] ExtraExpansionRelic: verify BonusExpansionSlots increments on shop open, resets per visit
  - [ ] Files: `Assets/Tests/Runtime/Items/Relics/MechanicRelicTests.cs`

## Dev Notes

### Architecture Compliance

- **EventBus communication:** Relics never reference game systems directly. TimeBuyerRelic calls `TradingState.ExtendTimer()` (static method) rather than holding a system reference. MarketManipulatorRelic dispatches via a PriceGenerator method accessed through the RunContext or static pattern.
- **One class per relic:** Each relic is a separate file in `Scripts/Runtime/Items/Relics/` per 17.1 conventions
- **No ScriptableObjects:** All relic data stays in `ShopItemDefinitions.RelicPool` as `public static readonly`
- **Error handling:** Each relic hook is already wrapped in per-relic try-catch by `RelicManager.Dispatch*` methods (Story 17.1)
- **Hot path awareness:** TimeBuyerRelic and MarketManipulatorRelic fire only on trade events (not per-frame). DiamondHands fires once at round end. FreeIntel and ExtraExpansion fire once per shop open.

### Existing Code to Read Before Implementing

- `Scripts/Runtime/Core/RunContext.cs` — add `FreeIntelThisVisit` and `BonusExpansionSlots` fields
- `Scripts/Runtime/Core/GameStates/TradingState.cs` — `_timeRemaining`, `ActiveTimeRemaining`, `IsActive` — add `ExtendTimer()` method
- `Scripts/Runtime/Core/GameStates/MarketCloseState.cs` — `LiquidateAllPositions()` call at line 57; add Diamond Hands multiplier before liquidation
- `Scripts/Runtime/PriceEngine/PriceGenerator.cs` — `ActiveStocks` list, `CurrentPrice` property — add `ApplyPriceMultiplier()` method
- `Scripts/Runtime/Core/GameStates/ShopState.cs` — `Enter()` method where `FreeIntelThisVisit` and `BonusExpansionSlots` reset, `ExpansionsPerShopVisit` usage at line 75
- `Scripts/Runtime/Shop/ShopTransaction.cs` — `PurchaseTip()` at line 189 — add free intel cost override logic
- `Scripts/Runtime/Items/RelicBase.cs` — base class with virtual no-op hooks (from Story 17.1)
- `Scripts/Runtime/Items/RelicManager.cs` — add `GetLiquidationMultiplier()` helper method
- `Scripts/Runtime/Items/RelicFactory.cs` — replace StubRelic entries with real constructors
- `Scripts/Runtime/Core/GameEvents.cs` — `TradeExecutedEvent` struct (IsBuy, IsShort fields), `RelicActivatedEvent`
- `Scripts/Setup/Data/ShopItemDefinitions.cs` — existing RelicDef entries for these 5 relics (from Story 17.2)

### Depends On

- **Story 17.1** — IRelic, RelicBase, RelicManager, RelicFactory, RelicActivatedEvent, EventBus dispatch wiring
- **Story 17.2** — RelicDef entries for all 5 relics in ShopItemDefinitions.RelicPool, EffectDescription field

### Key Design Decisions

- **TradingState.ExtendTimer()** is a static method because TradingState already uses static accessors (`IsActive`, `ActiveTimeRemaining`) — this keeps the pattern consistent and avoids relics needing system references
- **Diamond Hands** uses a query method (`GetLiquidationMultiplier`) rather than a hook to keep the MarketCloseState in control of the timing — liquidation is too critical for a relic to directly manipulate
- **Market Manipulator** price drop happens AFTER the trade event (OnAfterTrade), guaranteeing the sell price is unaffected by the relic's own effect
- **Free Intel** uses a boolean flag rather than a counter because only one free tip per visit is allowed. The flag is consumed (set to false) on first tip purchase.
- **RunContext field resets** happen in `ShopState.Enter()` BEFORE `DispatchShopOpen()` so that relic hooks can then set the flags — order of operations matters

### References

- [Source: _bmad-output/planning-artifacts/epic-17-relic-system.md#Story 17.6]
- [Source: _bmad-output/implementation-artifacts/17-1-relic-effect-framework.md]
- [Source: _bmad-output/implementation-artifacts/17-2-shop-behavior-and-data-overhaul.md#Relic Definitions Table]

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
