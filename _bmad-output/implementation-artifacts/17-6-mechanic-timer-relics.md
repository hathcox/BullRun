# Story 17.6: Mechanic & Timer Relics (5 Relics)

Status: done

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

- [x] Task 1: Add new RunContext fields (AC: 5, 6, 10, 11)
  - [x] Add `bool FreeIntelThisVisit` property to `RunContext` (default false)
  - [x] Add `int BonusExpansionSlots` property to `RunContext` (default 0)
  - [x] Reset both fields in `ResetForNewRun()`
  - [x] File: `Assets/Scripts/Runtime/Core/RunContext.cs`
- [x] Task 2: Create TimeBuyerRelic (AC: 1, 7, 8)
  - [x] Create `Assets/Scripts/Runtime/Items/Relics/TimeBuyerRelic.cs`
  - [x] Override `Id` => `"relic_time_buyer"`
  - [x] Override `OnAfterTrade(RunContext ctx, TradeExecutedEvent e)` — check `e.IsBuy && !e.IsShort`
  - [x] Add 5f to the active round timer in `TradingState` (requires a public `ExtendTimer(float seconds)` method on TradingState or a static setter)
  - [x] Publish `RelicActivatedEvent` with `RelicId = Id`
  - [x] Log: `[TimeBuyerRelic] Extended round timer by 5s`
- [x] Task 3: Add timer extension support to TradingState (AC: 1)
  - [x] Add `public static void ExtendTimer(float seconds)` method to `TradingState`
  - [x] Method adds seconds to `_timeRemaining` and updates `ActiveTimeRemaining`
  - [x] Only applies when `IsActive` is true (no-op otherwise)
  - [x] File: `Assets/Scripts/Runtime/Core/GameStates/TradingState.cs`
- [x] Task 4: Create DiamondHandsRelic (AC: 2, 4, 7, 8)
  - [x] Create `Assets/Scripts/Runtime/Items/Relics/DiamondHandsRelic.cs`
  - [x] Override `Id` => `"relic_diamond_hands"`
  - [x] No hook override needed — effect is passive, checked by MarketCloseState via `RelicManager.GetLiquidationMultiplier()`
  - [x] Add `GetLiquidationMultiplier()` method to `RelicManager` — returns 1.30f if diamond_hands is owned, else 1.0f
- [x] Task 5: Modify MarketCloseState for Diamond Hands liquidation boost (AC: 2, 4)
  - [x] Before calling `LiquidateAllPositions()`, check `ctx.RelicManager.GetLiquidationMultiplier()`
  - [x] If multiplier > 1.0f, multiply all long position values by the multiplier before liquidation
  - [x] Implementation: add cash bonus after liquidation based on (multiplier - 1.0f) * sum of long position values at liquidation prices
  - [x] Short positions are NOT affected (AC 4)
  - [x] Publish `RelicActivatedEvent` for diamond_hands if multiplier was applied
  - [x] File: `Assets/Scripts/Runtime/Core/GameStates/MarketCloseState.cs`
- [x] Task 6: Create MarketManipulatorRelic (AC: 3, 7, 8)
  - [x] Create `Assets/Scripts/Runtime/Items/Relics/MarketManipulatorRelic.cs`
  - [x] Override `Id` => `"relic_market_manipulator"`
  - [x] Override `OnAfterTrade(RunContext ctx, TradeExecutedEvent e)` — check `!e.IsBuy && !e.IsShort` (long sell only)
  - [x] Apply -15% to current stock price via `PriceGenerator.ApplyPriceMultiplier(string stockId, float multiplier)` — new method
  - [x] Sell already executed at pre-drop price (event fires after trade)
  - [x] Publish `RelicActivatedEvent`
  - [x] Log: `[MarketManipulatorRelic] Price dropped 15% after sell`
- [x] Task 7: Add price manipulation support to PriceGenerator (AC: 3)
  - [x] Add `public static void ApplyPriceMultiplier(string stockId, float multiplier)` method
  - [x] Method finds the active stock by ID and multiplies its `CurrentPrice` by the multiplier
  - [x] Publishes `PriceUpdatedEvent` after manipulation so chart reflects the change
  - [x] File: `Assets/Scripts/Runtime/PriceEngine/PriceGenerator.cs`
- [x] Task 8: Create FreeIntelRelic (AC: 5, 7, 8, 10)
  - [x] Create `Assets/Scripts/Runtime/Items/Relics/FreeIntelRelic.cs`
  - [x] Override `Id` => `"relic_free_intel"`
  - [x] Override `OnShopOpen(RunContext ctx)` — set `ctx.FreeIntelThisVisit = true`
  - [x] Publish `RelicActivatedEvent`
  - [x] Log: `[FreeIntelRelic] Free intel flag set for this shop visit`
- [x] Task 9: Modify ShopTransaction.PurchaseTip for free intel (AC: 5)
  - [x] In `PurchaseTip()`, before checking `CanAfford(cost)`: if `ctx.FreeIntelThisVisit` is true and no tips have been purchased this visit (`ctx.RevealedTips.Count == 0`), override cost to 0
  - [x] After the free tip is consumed, set `ctx.FreeIntelThisVisit = false` (one-time per visit)
  - [x] File: `Assets/Scripts/Runtime/Shop/ShopTransaction.cs`
- [x] Task 10: Modify ShopUI for "FREE" label on first tip slot (AC: 5)
  - [x] In tip panel rendering: if `ctx.FreeIntelThisVisit` is true and first tip slot is unpurchased, show "FREE" text instead of cost
  - [x] File: `Assets/Scripts/Runtime/UI/ShopUI.cs`
- [x] Task 11: Create ExtraExpansionRelic (AC: 6, 7, 8, 11)
  - [x] Create `Assets/Scripts/Runtime/Items/Relics/ExtraExpansionRelic.cs`
  - [x] Override `Id` => `"relic_extra_expansion"`
  - [x] Override `OnShopOpen(RunContext ctx)` — increment `ctx.BonusExpansionSlots` by 1
  - [x] Publish `RelicActivatedEvent`
  - [x] Log: `[ExtraExpansionRelic] Bonus expansion slot added for this shop visit`
- [x] Task 12: Modify ShopState for bonus expansion slots (AC: 6, 10, 11)
  - [x] In `ShopState.Enter()`: reset `ctx.FreeIntelThisVisit = false` and `ctx.BonusExpansionSlots = 0` BEFORE dispatching `DispatchShopOpen`
  - [x] After relic dispatch sets the flags, use `GameConfig.ExpansionsPerShopVisit + ctx.BonusExpansionSlots` when calling `_expansionManager.GetAvailableForShop()`
  - [x] File: `Assets/Scripts/Runtime/Core/GameStates/ShopState.cs`
- [x] Task 13: Update RelicFactory registrations (AC: 9)
  - [x] Replace StubRelic entries for `relic_time_buyer`, `relic_diamond_hands`, `relic_market_manipulator`, `relic_free_intel`, `relic_extra_expansion` with real constructors
  - [x] File: `Assets/Scripts/Runtime/Items/RelicFactory.cs`
- [x] Task 14: Write tests (AC: 1-12)
  - [x] TimeBuyerRelic: verify timer extension on buy trade, no extension on sell/short
  - [x] DiamondHandsRelic: verify GetLiquidationMultiplier returns 1.30f when owned, 1.0f otherwise
  - [x] MarketManipulatorRelic: verify price drops 15% after long sell, not on buy or short
  - [x] FreeIntelRelic: verify first tip is free, second tip costs normal, flag resets per visit
  - [x] ExtraExpansionRelic: verify BonusExpansionSlots increments on shop open, resets per visit
  - [x] Files: `Assets/Tests/Runtime/Items/Relics/MechanicRelicTests.cs`

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

## Senior Developer Review (AI)

**Reviewer:** Claude Opus 4.6 (adversarial code review)
**Date:** 2026-02-20
**Outcome:** Changes Requested → Fixed

### Findings (1 High, 3 Medium, 3 Low)

**H1 [FIXED]: ShopUI.OnTipCardClicked blocks Free Intel free tip when player has low/zero Rep**
- `OnTipCardClicked` validated affordability at full definition cost before reaching `ShopTransaction.PurchaseTip` where the free logic lives
- When player has FreeIntelThisVisit=true but insufficient Rep, UI showed "CAN'T AFFORD" instead of allowing the free purchase
- Fix: Added `isFreeFirstTip` bypass check before affordability gate

**M1 [FIXED]: PriceGenerator.ApplyPriceMultiplier didn't adjust TrendLinePrice**
- Market Manipulator's 15% price drop only modified `CurrentPrice`, leaving `TrendLinePrice` at the old level
- Mean reversion would pull price back up toward old trend, making the "buy-the-dip opportunity" too short-lived
- Fix: Also multiply `TrendLinePrice` by the multiplier

**M2 [FIXED]: No integration test for Diamond Hands bonus calculation**
- Tests only verified `GetLiquidationMultiplier()` return value, not the bonus math in MarketCloseState
- Fix: Added 3 tests covering bonus formula, zero-value edge case, and event publishing pattern

**M3 [FIXED]: ShopUI RefreshTipAffordability overwrites "FREE" label color**
- `RefreshTipAffordability()` didn't skip the free first tip slot, overwriting green "FREE" color with red on affordability refresh
- Fix: Added `FreeIntelThisVisit` check to skip free first tip in affordability refresh loop

**L1 [NOTED]: DiamondHandsRelic publishes RelicActivatedEvent from MarketCloseState, not from the relic itself**
- Inconsistent with other relics. Acceptable since effect is passive, but breaks the pattern.

**L2 [NOTED]: TradingState.ExtendTimer updates ActiveTimeRemaining but not ActiveRoundDuration**
- Timer progress calculations using RoundDuration would be inaccurate after extensions. OK if UI only reads ActiveTimeRemaining.

**L3 [FIXED]: No test for ApplyPriceMultiplier actually changing stock price**
- Only tested no-op case (null instance). Added 3 tests: price change, PriceUpdatedEvent publishing, and TrendLinePrice adjustment.

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Debug Log References

### Completion Notes List

- Implemented 5 mechanic/timer relics: TimeBuyerRelic, DiamondHandsRelic, MarketManipulatorRelic, FreeIntelRelic, ExtraExpansionRelic
- Added `FreeIntelThisVisit` (bool) and `BonusExpansionSlots` (int) fields to RunContext with proper reset in ResetForNewRun()
- Added `TradingState.ExtendTimer()` static method with `_activeInstance` pattern for timer extension
- Added `RelicManager.GetLiquidationMultiplier()` query method for Diamond Hands passive effect
- Modified MarketCloseState to compute long position values before liquidation and add 30% bonus cash when Diamond Hands is owned
- Added `PriceGenerator.ApplyPriceMultiplier()` static method with `_activeInstance` pattern for Market Manipulator price drops
- Wired `PriceGenerator.SetActiveInstance()` call in GameRunner initialization
- Modified ShopTransaction.PurchaseTip to support free first tip via FreeIntelThisVisit flag
- Modified ShopUI.ShowTips to display "FREE" label on first tip slot when FreeIntelThisVisit is active
- Restructured ShopState.Enter to reset flags before DispatchShopOpen, then generate expansion/tip offerings after relic dispatch so BonusExpansionSlots affects expansion count
- Updated RelicFactory with 5 real constructors replacing StubRelic entries
- Wrote 37 unit tests covering all 5 relics, factory integration, RunContext field defaults/resets, and edge cases

### File List

- `Assets/Scripts/Runtime/Core/RunContext.cs` (modified — added FreeIntelThisVisit, BonusExpansionSlots, reset logic)
- `Assets/Scripts/Runtime/Core/GameStates/TradingState.cs` (modified — added ExtendTimer, _activeInstance)
- `Assets/Scripts/Runtime/Core/GameStates/MarketCloseState.cs` (modified — Diamond Hands liquidation bonus)
- `Assets/Scripts/Runtime/Core/GameStates/ShopState.cs` (modified — flag resets, reordered Enter for relic dispatch before expansion generation)
- `Assets/Scripts/Runtime/Core/GameRunner.cs` (modified — PriceGenerator.SetActiveInstance wiring)
- `Assets/Scripts/Runtime/PriceEngine/PriceGenerator.cs` (modified — added ApplyPriceMultiplier, _activeInstance, review fix: TrendLinePrice adjustment)
- `Assets/Scripts/Runtime/Shop/ShopTransaction.cs` (modified — free intel tip cost override)
- `Assets/Scripts/Runtime/UI/ShopUI.cs` (modified — FREE label on first tip slot, review fix: free tip click bypass + affordability color preservation)
- `Assets/Scripts/Runtime/Items/RelicManager.cs` (modified — added GetLiquidationMultiplier)
- `Assets/Scripts/Runtime/Items/RelicFactory.cs` (modified — 5 new registrations)
- `Assets/Scripts/Runtime/Items/Relics/TimeBuyerRelic.cs` (new)
- `Assets/Scripts/Runtime/Items/Relics/DiamondHandsRelic.cs` (new)
- `Assets/Scripts/Runtime/Items/Relics/MarketManipulatorRelic.cs` (new)
- `Assets/Scripts/Runtime/Items/Relics/FreeIntelRelic.cs` (new)
- `Assets/Scripts/Runtime/Items/Relics/ExtraExpansionRelic.cs` (new)
- `Assets/Tests/Runtime/Items/Relics/MechanicRelicTests.cs` (new — 44 tests, +7 from code review)

## Change Log

- 2026-02-19: Implemented Story 17.6 — 5 mechanic/timer relics (Time Buyer, Diamond Hands, Market Manipulator, Free Intel, Extra Expansion) with system modifications and 37 tests
- 2026-02-20: Code review fixes — H1: Free Intel UI click bypass, M1: ApplyPriceMultiplier TrendLinePrice adjustment, M3: FREE label color preservation, +7 tests (M2/L3/H1 coverage)
