# Story 17.4: Event Interaction Relics (5 Relics)

Status: done

## Story

As a player,
I want relics that interact with the market event system — triggering events on trades, doubling event frequency, or amplifying positive events,
so that I can build around event manipulation as a core strategy.

## Acceptance Criteria

1. **Catalyst Trader** (`relic_event_trigger`, `CatalystTraderRelic.cs`): When the player executes a buy trade, a random market event is immediately triggered on the active stock. Buy cooldown is increased by +3s (added to `GameConfig.PostTradeCooldown`). The triggered event follows normal event processing (popup, price effect, etc.).
2. **Event Storm** (`relic_event_storm`, `EventStormRelic.cs`): At round start, EventScheduler generates double the normal number of events for the round. All event price impacts are reduced by 25% (multiplied by 0.75). The reduced impact applies to ALL events (natural + relic-triggered).
3. **Loss Liquidator** (`relic_loss_liquidator`, `LossLiquidatorRelic.cs`): When the player sells a stock at a loss (`!IsBuy && !IsShort && ProfitLoss < 0`), a random market event is immediately triggered on the active stock. The event fires AFTER the sell completes. Selling at a profit does NOT trigger this effect.
4. **Profit Refresh** (`relic_profit_refresh`, `ProfitRefreshRelic.cs`): When the player sells a stock at a profit (`!IsBuy && !IsShort && ProfitLoss > 0`), the buy button cooldown is immediately reset to 0 (ready to buy again). Selling at a loss does NOT trigger this effect. A brief visual cue (flash on buy button) indicates the cooldown was refreshed.
5. **Bull Believer** (`relic_bull_believer`, `BullBelieverRelic.cs`): When a positive market event fires (`IsPositive == true`), the event's price impact is doubled (`PriceEffectPercent * 2.0`). The SHORT button is permanently disabled for the rest of the run. OnAcquired sets `RunContext.ShortingDisabled = true`. Keyboard shortcut D is blocked.
6. `EventScheduler` gains a `ForceFireRandomEvent()` method that immediately fires a random market event on the active stock, following normal event processing (popup, price effect).
7. `EventScheduler` gains `EventCountMultiplier` (float, default 1.0) and `ImpactMultiplier` (float, default 1.0) fields. These are applied during `InitializeRound()` for event count and during event firing for price impact. Both reset to defaults at round start before relic dispatch.
8. `RunContext` gains a `ShortingDisabled` bool flag, defaulting to false. GameRunner checks this flag before executing shorts and disables the short button accordingly.
9. `GameRunner` gains a `ResetBuyCooldown()` method (or exposes a way for relics to reset `_postTradeCooldownTimer` to 0).
10. All 5 relic constructors registered in `RelicFactory` (replacing stub entries).
11. Each relic fires `RelicActivatedEvent` when its effect triggers (for future UI glow in Story 17.8).

## Tasks / Subtasks

- [x] Task 1: Add EventScheduler modifications (AC: 6, 7)
  - [x] Add `ForceFireRandomEvent()` method to `EventScheduler` — selects a random event type, fires it on the active stock using existing event-fire logic, triggers popup/price effect
  - [x] Add `float EventCountMultiplier` field (default 1.0f) — applied in `InitializeRound()` when calculating event count
  - [x] Add `float ImpactMultiplier` field (default 1.0f) — applied when computing `PriceEffectPercent` during event firing
  - [x] Both multipliers reset to 1.0f at the start of `InitializeRound()` (before relic dispatch happens)
  - [x] File: `Scripts/Runtime/Events/EventScheduler.cs`

- [x] Task 2: Add RunContext and GameRunner modifications (AC: 4, 5, 8, 9)
  - [x] Add `bool ShortingDisabled` property to `RunContext` (default false), reset in `ResetForNewRun()`
  - [x] In `GameRunner`, check `ctx.ShortingDisabled` before executing short trades — show "LOCKED" feedback and return early if true
  - [x] In `GameRunner`, block keyboard shortcut D when `ctx.ShortingDisabled` is true
  - [x] Dim/disable short button visually when `ShortingDisabled` is true
  - [x] Add `ResetBuyCooldown()` method to `GameRunner` — sets `_postTradeCooldownTimer = 0`, `_isPostTradeCooldownActive = false`, re-enables buy button immediately
  - [x] Make `ResetBuyCooldown()` accessible to relics (via `GameRunner.Instance.ResetBuyCooldown()` or a delegate on RunContext)
  - [x] File: `Scripts/Runtime/Core/RunContext.cs`, `Scripts/Runtime/Core/GameRunner.cs`

- [x] Task 3: Update RelicManager cooldown helper for Catalyst Trader (AC: 1)
  - [x] Extend `GetEffectiveCooldown(bool isBuy)` (from Story 17.3) to also check for Catalyst Trader — adds +3s to buy cooldown when owned
  - [x] Multiple cooldown modifiers stack additively (Quick Draw 0s + Catalyst Trader +3s = 3s for buy)
  - [x] File: `Scripts/Runtime/Items/RelicManager.cs`

- [x] Task 4: Implement Catalyst Trader relic (AC: 1, 10, 11)
  - [x] Create `CatalystTraderRelic.cs` extending `RelicBase`
  - [x] Override `Id` to return `"relic_event_trigger"`
  - [x] Override `OnAfterTrade` — check `e.IsBuy && !e.IsShort`
  - [x] Call `EventScheduler.ForceFireRandomEvent()` (needs reference via RunContext or static accessor)
  - [x] Cooldown increase is handled passively by `RelicManager.GetEffectiveCooldown()` checking for this relic
  - [x] Publish `RelicActivatedEvent` with relic Id
  - [x] File: `Scripts/Runtime/Items/Relics/CatalystTraderRelic.cs`

- [x] Task 5: Implement Event Storm relic (AC: 2, 10, 11)
  - [x] Create `EventStormRelic.cs` extending `RelicBase`
  - [x] Override `Id` to return `"relic_event_storm"`
  - [x] Override `OnRoundStart` — sets `EventScheduler.EventCountMultiplier = 2.0f` and `EventScheduler.ImpactMultiplier = 0.75f`
  - [x] These values are set AFTER the multipliers reset to 1.0 at round init, so relic dispatch must happen after EventScheduler.InitializeRound
  - [x] Publish `RelicActivatedEvent` with relic Id
  - [x] File: `Scripts/Runtime/Items/Relics/EventStormRelic.cs`

- [x] Task 6: Implement Loss Liquidator relic (AC: 3, 10, 11)
  - [x] Create `LossLiquidatorRelic.cs` extending `RelicBase`
  - [x] Override `Id` to return `"relic_loss_liquidator"`
  - [x] Override `OnAfterTrade` — check `!e.IsBuy && !e.IsShort && e.ProfitLoss < 0`
  - [x] Call `EventScheduler.ForceFireRandomEvent()` to fire a random event
  - [x] Publish `RelicActivatedEvent` with relic Id
  - [x] File: `Scripts/Runtime/Items/Relics/LossLiquidatorRelic.cs`

- [x] Task 7: Implement Profit Refresh relic (AC: 4, 10, 11)
  - [x] Create `ProfitRefreshRelic.cs` extending `RelicBase`
  - [x] Override `Id` to return `"relic_profit_refresh"`
  - [x] Override `OnAfterTrade` — check `!e.IsBuy && !e.IsShort && e.ProfitLoss > 0`
  - [x] Call `GameRunner.Instance.ResetBuyCooldown()` (or equivalent delegate) to reset buy cooldown to 0
  - [x] Publish `TradeFeedbackEvent` with "BUY READY" or similar message for visual cue
  - [x] Publish `RelicActivatedEvent` with relic Id
  - [x] File: `Scripts/Runtime/Items/Relics/ProfitRefreshRelic.cs`

- [x] Task 8: Implement Bull Believer relic (AC: 5, 10, 11)
  - [x] Create `BullBelieverRelic.cs` extending `RelicBase`
  - [x] Override `Id` to return `"relic_bull_believer"`
  - [x] Override `OnAcquired` — sets `ctx.ShortingDisabled = true`
  - [x] Override `OnMarketEventFired` — check `e.IsPositive`, if true double `e.PriceEffectPercent`
  - [x] Note: Doubling the PriceEffectPercent requires the event struct to be passed by ref or the EventScheduler.ImpactMultiplier approach. If MarketEventFiredEvent is a struct (value type), the relic may need to set a multiplier on EventScheduler instead of modifying the event directly.
  - [x] Publish `RelicActivatedEvent` with relic Id
  - [x] File: `Scripts/Runtime/Items/Relics/BullBelieverRelic.cs`

- [x] Task 9: Register relics in RelicFactory (AC: 10)
  - [x] Replace stub registrations for `relic_event_trigger`, `relic_event_storm`, `relic_loss_liquidator`, `relic_profit_refresh`, `relic_bull_believer` with real constructors
  - [x] File: `Scripts/Runtime/Items/RelicFactory.cs`

- [x] Task 10: Write tests (AC: 1-11)
  - [x] Catalyst Trader: buy triggers ForceFireRandomEvent, sell does not, cooldown increased by 3s for buy
  - [x] Event Storm: OnRoundStart sets EventCountMultiplier to 2.0 and ImpactMultiplier to 0.75
  - [x] Loss Liquidator: sell-at-loss triggers event, sell-at-profit does not, buy does not
  - [x] Profit Refresh: sell-at-profit resets buy cooldown, sell-at-loss does not, buy does not
  - [x] Bull Believer: ShortingDisabled set on acquire, positive event impact doubled, negative event unchanged, short blocked
  - [x] ForceFireRandomEvent: event fires with valid type, popup/price effect occurs
  - [x] EventCountMultiplier and ImpactMultiplier: correctly applied during InitializeRound and event firing
  - [x] GetEffectiveCooldown with Catalyst Trader: buy cooldown +3s, sell cooldown unaffected
  - [x] Files: `Tests/Runtime/Items/Relics/EventRelicTests.cs`

## Dev Notes

### Architecture Compliance

- **One class per relic:** Each relic is a separate file in `Scripts/Runtime/Items/Relics/` following the project pattern from Story 17.1.
- **EventBus communication:** Relics publish `RelicActivatedEvent` and `TradeFeedbackEvent` via EventBus — never reference UI systems directly.
- **No ScriptableObjects:** Relic data remains as `public static readonly` in `ShopItemDefinitions.cs` (Story 17.2 handles definitions).
- **RunContext as data carrier:** `ShortingDisabled` flag lives on RunContext. Relics receive RunContext via method parameters.
- **EventScheduler is pure C#:** New multiplier fields and `ForceFireRandomEvent()` follow the existing non-MonoBehaviour pattern for testability.

### Existing Code to Read Before Implementing

- `Scripts/Runtime/Items/RelicBase.cs` — base class with virtual no-op hooks (from Story 17.1)
- `Scripts/Runtime/Items/RelicManager.cs` — dispatch methods, `GetEffectiveCooldown()` helper (from Stories 17.1, 17.3)
- `Scripts/Runtime/Items/RelicFactory.cs` — static registry of relic ID to constructor mapping (from Story 17.1)
- `Scripts/Runtime/Events/EventScheduler.cs` — `InitializeRound()`, event count calculation, event firing logic, `EventEffects` reference
- `Scripts/Runtime/Events/EventEffects.cs` — price effect application (where `ImpactMultiplier` would be applied)
- `Scripts/Runtime/Core/RunContext.cs` — central run state; add `ShortingDisabled` flag here
- `Scripts/Runtime/Core/GameRunner.cs` — post-trade cooldown logic (`_postTradeCooldownTimer`, `_isPostTradeCooldownActive`), short button management, keyboard input handling
- `Scripts/Runtime/Core/GameEvents.cs` — `TradeExecutedEvent` (has `IsBuy`, `IsShort`, `ProfitLoss`), `MarketEventFiredEvent` (has `IsPositive`, `PriceEffectPercent`)
- `Scripts/Setup/Data/GameConfig.cs` — `PostTradeCooldown = 1.0f`

### Key Design Decisions

- **ForceFireRandomEvent:** Reuses existing event-selection and firing logic inside EventScheduler. Selects a random `MarketEventType`, builds the event struct, publishes `MarketEventFiredEvent` via EventBus. This ensures popup/price effects work identically to scheduled events.
- **Multiplier reset timing (review fix):** Multipliers reset to 1.0 in `TradingState.Enter()` BEFORE publishing `RoundStartedEvent`. Relic dispatch via `DispatchRoundStart` sets multipliers (Event Storm, Bull Believer). Then `InitializeRound()` runs AFTER dispatch and uses relic-set values for event count calculation. `ImpactMultiplier` and `PositiveImpactMultiplier` persist for event firing during the round.
- **Bull Believer and struct events:** Since `MarketEventFiredEvent` is a C# struct (value type), the relic cannot modify it after publish. Instead, Bull Believer should set a positive-event multiplier on EventScheduler (e.g., `PositiveImpactMultiplier = 2.0f`) that EventEffects checks when applying positive events. This is architecturally cleaner than passing the event by ref.
- **Cooldown stacking:** `GetEffectiveCooldown(bool isBuy)` checks for both Quick Draw (Story 17.3) and Catalyst Trader. Effects stack: Quick Draw zeroes buy cooldown, Catalyst Trader adds +3s. If both owned: buy cooldown = 0 + 3 = 3s.

### Depends On

- Story 17.1 (Relic Effect Framework) — IRelic, RelicBase, RelicManager, RelicFactory must exist
- Story 17.2 (Shop Behavior & Data Overhaul) — relic definitions with IDs registered in ShopItemDefinitions
- Story 17.3 (Trade Modification Relics) — `GetEffectiveCooldown()` method on RelicManager, `LongsDisabled` pattern on RunContext (same pattern used for `ShortingDisabled`)

### References

- [Source: _bmad-output/planning-artifacts/epic-17-relic-system.md#Story 17.4]
- [Source: _bmad-output/implementation-artifacts/17-1-relic-effect-framework.md]
- [Source: _bmad-output/implementation-artifacts/17-3-trade-modification-relics.md]
- [Source: Scripts/Runtime/Events/EventScheduler.cs — InitializeRound, event count logic]
- [Source: Scripts/Runtime/Core/GameEvents.cs — MarketEventFiredEvent, TradeExecutedEvent]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Debug Log References

N/A — no debug issues encountered.

### Completion Notes List

- Task 1: Added `ForceFireRandomEvent()`, `EventCountMultiplier`, `ImpactMultiplier`, and `PositiveImpactMultiplier` to EventScheduler. Multipliers changed from public fields to auto-properties. Stored tier and activeStocks for use by ForceFireRandomEvent. Applied ImpactMultiplier and PositiveImpactMultiplier in FireEvent and FireSectorRotation.
- Review fix C1: Moved multiplier resets from InitializeRound to TradingState.Enter (before RoundStartedEvent). Fixed critical timing bug where relics set multipliers via RoundStartedEvent, then InitializeRound immediately reset them to 1.0. Now: reset → relic dispatch → InitializeRound uses relic values.
- Task 2: Added `ShortingDisabled` to RunContext (default false, reset in ResetForNewRun). Added `ResetBuyCooldown()` to GameRunner. Added ShortingDisabled checks to HandleShortInput, HandleShort2Input, HandleTradingInput (D key), and UpdateShortButtonVisuals. Added `ApplyShortingDisabledVisuals()` for locked visual state.
- Review fix H1: Added ShortingDisabled guard to UpdateShort2ButtonVisuals (was missing, causing Short 2 button to show normal state instead of LOCKED when Bull Believer active with Dual Short expansion).
- Task 3: Extended `GetEffectiveCooldown()` to check for Catalyst Trader (`relic_event_trigger`) and add +3s to buy cooldown. Cooldown modifiers stack additively with Quick Draw.
- Task 4: Created `CatalystTraderRelic.cs` — triggers `ForceFireRandomEvent()` on buy trades via `ctx.EventScheduler`. Added EventScheduler reference to RunContext for relic access. Wired in GameRunner.Awake.
- Task 5: Created `EventStormRelic.cs` — sets EventCountMultiplier=2.0 and ImpactMultiplier=0.75 on round start.
- Task 6: Created `LossLiquidatorRelic.cs` — triggers `ForceFireRandomEvent()` on sell-at-loss (ProfitLoss < 0).
- Task 7: Created `ProfitRefreshRelic.cs` — resets buy cooldown on sell-at-profit via RunContext delegate, publishes "BUY READY" feedback.
- Review fix M1: Replaced GameRunner.Instance.ResetBuyCooldown() with RunContext.ResetBuyCooldownAction delegate (architecture compliance — systems communicate via EventBus/RunContext, not direct references).
- Task 8: Created `BullBelieverRelic.cs` — sets ShortingDisabled=true on acquire, sets PositiveImpactMultiplier=2.0 on round start (applied by EventScheduler during event firing).
- Task 9: Registered all 5 relics in RelicFactory, replacing StubRelic entries.
- Task 10: Created 50+ tests in EventRelicTests.cs covering all 5 relics, EventScheduler multipliers, cooldown stacking, factory integration, and relic interaction.
- Review fix M2: Updated MultipliersResetOnInitializeRound test to verify multipliers are PRESERVED (reset moved to TradingState). Added EventCountMultiplier_DoubleProducesMoreEvents integration test.
- Review fix L1: Moved RelicActivatedEvent publish inside null checks in CatalystTraderRelic and LossLiquidatorRelic.

### File List

- `Assets/Scripts/Runtime/Events/EventScheduler.cs` (modified) — ForceFireRandomEvent, multiplier properties, ImpactMultiplier application
- `Assets/Scripts/Runtime/Events/EventEffects.cs` (unchanged, read only)
- `Assets/Scripts/Runtime/Core/GameStates/TradingState.cs` (modified) — Review fix: multiplier reset timing, InitializeRound after relic dispatch
- `Assets/Scripts/Runtime/Core/RunContext.cs` (modified) — ShortingDisabled property, EventScheduler reference, ResetBuyCooldownAction delegate
- `Assets/Scripts/Runtime/Core/GameRunner.cs` (modified) — ResetBuyCooldown, ShortingDisabled checks, ApplyShortingDisabledVisuals, EventScheduler wiring, ResetBuyCooldownAction wiring, Short2 visual fix
- `Assets/Scripts/Runtime/Items/RelicManager.cs` (modified) — GetEffectiveCooldown updated for Catalyst Trader +3s
- `Assets/Scripts/Runtime/Items/RelicFactory.cs` (modified) — 5 new relic registrations
- `Assets/Scripts/Runtime/Items/Relics/CatalystTraderRelic.cs` (new)
- `Assets/Scripts/Runtime/Items/Relics/EventStormRelic.cs` (new)
- `Assets/Scripts/Runtime/Items/Relics/LossLiquidatorRelic.cs` (new)
- `Assets/Scripts/Runtime/Items/Relics/ProfitRefreshRelic.cs` (new)
- `Assets/Scripts/Runtime/Items/Relics/BullBelieverRelic.cs` (new)
- `Assets/Tests/Runtime/Items/Relics/EventRelicTests.cs` (new)

## Change Log

- 2026-02-19: Implemented Story 17.4 — 5 event interaction relics (Catalyst Trader, Event Storm, Loss Liquidator, Profit Refresh, Bull Believer) with EventScheduler modifications, RunContext/GameRunner integration, and 50+ unit tests.
- 2026-02-19: Code review fixes — C1: Fixed critical multiplier timing (InitializeRound reset before relic dispatch). H1: Added ShortingDisabled guard to Short2 visuals. M1: Replaced GameRunner.Instance with RunContext delegate. M2: Updated tests for new multiplier lifecycle. M3: Changed multiplier fields to auto-properties. L1: Conditional RelicActivatedEvent publish.
