# Story 17.7: Special Relics (2 Relics)

Status: ready-for-dev

## Story

As a player,
I want unique high-impact relics with special mechanics like probability-based event triggering and self-sacrificing slot expansion,
so that I have access to powerful late-game strategic options that reward creative play.

## Acceptance Criteria

1. **Event Catalyst** (`relic_event_catalyst`): when reputation is gained, each point earned has a 1% chance to trigger a random market event
2. Event Catalyst calculates `repGained = newRep - oldRep`, then rolls `Random.value < 0.01f` for each point of rep gained
3. Event Catalyst only triggers events during the trading phase (check `TradingState.IsActive`)
4. Event Catalyst calls `EventScheduler.ForceFireRandomEvent()` on successful roll — a new method that fires a random event immediately
5. Multiple Event Catalyst triggers queue normally through the existing EventEffects system
6. **Relic Expansion** (`relic_relic_expansion`): selling this relic permanently grants +1 relic slot; refund is 0 rep (not the standard 50%)
7. Relic Expansion increments `RunContext.BonusRelicSlots` (new int field) in its `OnSellSelf` override
8. `ShopTransaction.GetEffectiveMaxRelicSlots()` is updated to include `RunContext.BonusRelicSlots` in the max slot calculation
9. While held, Relic Expansion occupies a slot but has no passive effect
10. Relic Expansion tooltip reads: "Sell to permanently gain +1 relic slot. No Rep refund."
11. Both relics extend `RelicBase` and override only the hooks they need
12. Both relics publish `RelicActivatedEvent` when their effect fires (for UI glow in Story 17.8)
13. Both relics are registered in `RelicFactory` with their proper constructors (replacing StubRelic entries)
14. `ShopTransaction.SellRelic()` checks for `OnSellSelf` override before applying standard 50% refund logic
15. Existing tests continue to pass after modifications to `RunContext`, `ShopTransaction`, `ReputationManager`, and `EventScheduler`

## Tasks / Subtasks

- [ ] Task 1: Add BonusRelicSlots to RunContext (AC: 7, 8)
  - [ ] Add `int BonusRelicSlots` property to `RunContext` (default 0)
  - [ ] Do NOT reset `BonusRelicSlots` in `ResetForNewRun()` — this persists across shop visits within a run but resets on new run
  - [ ] Actually: reset to 0 in `ResetForNewRun()` since it is per-run, not permanent
  - [ ] File: `Assets/Scripts/Runtime/Core/RunContext.cs`
- [ ] Task 2: Create EventCatalystRelic (AC: 1, 2, 3, 4, 5, 11, 12)
  - [ ] Create `Assets/Scripts/Runtime/Items/Relics/EventCatalystRelic.cs`
  - [ ] Override `Id` => `"relic_event_catalyst"`
  - [ ] Override `OnReputationChanged(RunContext ctx, int oldRep, int newRep)`
  - [ ] Calculate `repGained = newRep - oldRep`; if `repGained <= 0`, return (rep loss or no change)
  - [ ] Guard: `if (!TradingState.IsActive) return` — only fire during trading phase (AC 3)
  - [ ] For each point of repGained: roll `UnityEngine.Random.value < 0.01f`
  - [ ] On hit: call `EventScheduler.ForceFireRandomEvent()` — requires access to EventScheduler (see Task 3)
  - [ ] Publish `RelicActivatedEvent` with `RelicId = Id` on each successful trigger
  - [ ] Log: `[EventCatalystRelic] Rep +{repGained} triggered {hitCount} random event(s)`
- [ ] Task 3: Add ForceFireRandomEvent to EventScheduler (AC: 4, 5)
  - [ ] Add `public void ForceFireRandomEvent()` method to `EventScheduler`
  - [ ] Method selects a random event type using `SelectEventType(currentTier)` and fires it via `FireEvent(config, activeStocks)`
  - [ ] Requires access to current tier and active stocks — use stored references from `InitializeRound()`
  - [ ] Store `_currentTier` and `_activeStocks` references in `InitializeRound()` for use by `ForceFireRandomEvent()`
  - [ ] Events queue normally through `EventEffects.StartEvent()` (AC 5)
  - [ ] File: `Assets/Scripts/Runtime/Events/EventScheduler.cs`
- [ ] Task 4: Wire EventScheduler access for EventCatalystRelic (AC: 1, 4)
  - [ ] EventCatalystRelic needs to call `EventScheduler.ForceFireRandomEvent()` — but relics should not hold direct system references
  - [ ] Option A: Pass EventScheduler reference via RunContext (add `EventScheduler` property to RunContext, set in GameRunner)
  - [ ] Option B: Use a static accessor on EventScheduler (similar to `TradingState.IsActive` pattern)
  - [ ] Recommended: Option A — add `EventScheduler EventScheduler { get; internal set; }` to RunContext, set in `GameRunner.Awake()`
  - [ ] File: `Assets/Scripts/Runtime/Core/RunContext.cs`, `Assets/Scripts/Runtime/Core/GameRunner.cs`
- [ ] Task 5: Add OnChanged callback to ReputationManager (AC: 1, 2)
  - [ ] Verify that `RelicManager.DispatchReputationChanged(ctx, oldRep, newRep)` is already wired (from Story 17.1, Task 6)
  - [ ] If not yet wired: add `System.Action<int, int> OnChanged` callback to `ReputationManager.Add()` that captures oldRep before the add and calls the callback with (oldRep, newRep) after
  - [ ] GameRunner subscribes the callback to route to `ctx.RelicManager.DispatchReputationChanged()`
  - [ ] File: `Assets/Scripts/Runtime/Core/ReputationManager.cs`, `Assets/Scripts/Runtime/Core/GameRunner.cs`
- [ ] Task 6: Create RelicExpansionRelic (AC: 6, 7, 9, 10, 11, 12)
  - [ ] Create `Assets/Scripts/Runtime/Items/Relics/RelicExpansionRelic.cs`
  - [ ] Override `Id` => `"relic_relic_expansion"`
  - [ ] Override `OnSellSelf(RunContext ctx)` — increment `ctx.BonusRelicSlots` by 1
  - [ ] Publish `RelicActivatedEvent` with `RelicId = Id`
  - [ ] Log: `[RelicExpansionRelic] Sold — bonus relic slot granted (total bonus: {ctx.BonusRelicSlots})`
  - [ ] No other hooks overridden — while held, this relic has no passive effect (AC 9)
- [ ] Task 7: Update ShopTransaction.SellRelic for OnSellSelf and refund override (AC: 6, 14)
  - [ ] In `SellRelic()`: after removing from `OwnedRelics`, call `ctx.RelicManager.DispatchSellSelf(ctx, relicId)`
  - [ ] Add mechanism for relics to override refund amount: `RelicManager.GetSellRefundOverride(string relicId)` returns `int?` — null means use standard 50%, a value means use that exact amount
  - [ ] `RelicExpansionRelic` returns refund override of 0 (via a virtual method `GetSellRefund()` on RelicBase, default null)
  - [ ] In `SellRelic()`: check override before calculating standard `relicDef.Value.Cost / 2` refund
  - [ ] File: `Assets/Scripts/Runtime/Shop/ShopTransaction.cs`, `Assets/Scripts/Runtime/Items/RelicBase.cs`, `Assets/Scripts/Runtime/Items/RelicManager.cs`
- [ ] Task 8: Update GetEffectiveMaxRelicSlots for BonusRelicSlots (AC: 8)
  - [ ] In `ShopTransaction.GetEffectiveMaxRelicSlots()`: add `ctx.BonusRelicSlots` to the max slots calculation
  - [ ] New formula: `GameConfig.MaxRelicSlots + expandedInventoryBonus + ctx.BonusRelicSlots`
  - [ ] File: `Assets/Scripts/Runtime/Shop/ShopTransaction.cs`
- [ ] Task 9: Update RelicFactory registrations (AC: 13)
  - [ ] Replace StubRelic entries for `relic_event_catalyst` and `relic_relic_expansion` with real constructors
  - [ ] File: `Assets/Scripts/Runtime/Items/RelicFactory.cs`
- [ ] Task 10: Write tests (AC: 1-15)
  - [ ] EventCatalystRelic: verify rep gain triggers event rolls, rep loss does not, inactive trading phase blocks triggers
  - [ ] EventCatalystRelic: verify 1% probability (mock Random or test with large rep gain for statistical coverage)
  - [ ] RelicExpansionRelic: verify OnSellSelf increments BonusRelicSlots
  - [ ] RelicExpansionRelic: verify sell refund is 0 (not standard 50%)
  - [ ] GetEffectiveMaxRelicSlots: verify BonusRelicSlots is included in calculation
  - [ ] ForceFireRandomEvent: verify event fires through EventEffects
  - [ ] Files: `Assets/Tests/Runtime/Items/Relics/SpecialRelicTests.cs`, `Assets/Tests/Runtime/Events/EventSchedulerForceFireTests.cs`

## Dev Notes

### Architecture Compliance

- **EventBus communication:** EventCatalystRelic accesses EventScheduler through RunContext (not direct system reference). TradingState.IsActive is a static read-only accessor following existing patterns.
- **One class per relic:** Each relic is a separate file in `Scripts/Runtime/Items/Relics/`
- **No ScriptableObjects:** All relic data stays in `ShopItemDefinitions.RelicPool`
- **Error handling:** Per-relic try-catch in RelicManager dispatch handles failures gracefully (Story 17.1)
- **Hot path awareness:** EventCatalystRelic only fires on reputation change events (not per-frame). RelicExpansionRelic only fires on sell (one-time).

### Existing Code to Read Before Implementing

- `Scripts/Runtime/Core/RunContext.cs` — add `BonusRelicSlots` field, add `EventScheduler` property
- `Scripts/Runtime/Core/GameRunner.cs` — set `ctx.EventScheduler` reference in Awake, verify ReputationManager callback wiring
- `Scripts/Runtime/Core/ReputationManager.cs` — `Add()` method at line 21 — needs OnChanged callback for relic dispatch
- `Scripts/Runtime/Events/EventScheduler.cs` — `SelectEventType()`, `FireEvent()` methods — add `ForceFireRandomEvent()`, store tier/stocks refs from `InitializeRound()`
- `Scripts/Runtime/Shop/ShopTransaction.cs` — `SellRelic()` at line 238, `GetEffectiveMaxRelicSlots()` at line 86 — add BonusRelicSlots, OnSellSelf dispatch, refund override
- `Scripts/Runtime/Items/RelicBase.cs` — add virtual `GetSellRefund()` method returning `int?` (null = default)
- `Scripts/Runtime/Items/RelicManager.cs` — add `GetSellRefundOverride()` helper, verify `DispatchSellSelf()` exists
- `Scripts/Runtime/Items/RelicFactory.cs` — replace StubRelic entries with real constructors
- `Scripts/Runtime/Core/GameEvents.cs` — `RelicActivatedEvent` struct
- `Scripts/Runtime/Core/GameStates/TradingState.cs` — `IsActive` static accessor used by EventCatalystRelic
- `Scripts/Setup/Data/ShopItemDefinitions.cs` — RelicDef entries for `relic_event_catalyst` (cost 20) and `relic_relic_expansion` (cost 50)

### Depends On

- **Story 17.1** — IRelic, RelicBase, RelicManager, RelicFactory, RelicActivatedEvent, DispatchReputationChanged, DispatchSellSelf
- **Story 17.2** — RelicDef entries for both relics in ShopItemDefinitions.RelicPool
- **Story 17.4** (or wherever `ForceFireRandomEvent` was originally planned) — if EventScheduler.ForceFireRandomEvent was specified as a dependency from another story, coordinate. Otherwise, this story implements it from scratch.

### Key Design Decisions

- **EventScheduler access via RunContext:** Relics should not hold direct system references per project rules (EventBus or RunContext only). Adding `EventScheduler` to RunContext follows the pattern of `ReputationManager` and `BondManager` being RunContext properties. GameRunner sets it in Awake since it owns the EventScheduler instance.
- **Refund override via virtual method:** Rather than special-casing relic IDs in ShopTransaction, RelicBase gains a virtual `GetSellRefund()` method that returns `int?`. Only RelicExpansionRelic overrides it to return 0. This is extensible for future relics with custom sell behavior.
- **BonusRelicSlots is per-run:** The +1 slot persists across all remaining shop visits in the current run but resets on new run. This makes the relic a permanent investment within the run.
- **Event Catalyst probability:** 1% per rep point means earning 10 rep gives roughly a 9.6% chance of at least one event. This scales with rep-heavy builds but remains rare enough to feel special.

### References

- [Source: _bmad-output/planning-artifacts/epic-17-relic-system.md#Story 17.7]
- [Source: _bmad-output/implementation-artifacts/17-1-relic-effect-framework.md]
- [Source: _bmad-output/implementation-artifacts/17-2-shop-behavior-and-data-overhaul.md#Relic Definitions Table]

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
