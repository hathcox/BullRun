# Story 17.1: Relic Effect Framework — IRelic, RelicManager & Event Dispatch

Status: done

## Story

As a developer,
I want a relic effect execution framework with an IRelic interface, RelicManager, and EventBus dispatch pipeline,
so that relics can hook into game events and execute effects in player-defined order.

## Acceptance Criteria

1. `IRelic` interface exists with hook methods: `OnAcquired`, `OnRemoved`, `OnRoundStart`, `OnRoundEnd`, `OnBeforeTrade`, `OnAfterTrade`, `OnMarketEventFired`, `OnReputationChanged`, `OnShopOpen`, `OnSellSelf`
2. `RelicBase` abstract class implements `IRelic` with all methods as virtual no-ops — subclasses override only what they need
3. `RelicManager` holds `List<IRelic>` in player order, dispatches events left-to-right with per-relic try-catch
4. `RelicFactory` maps relic ID strings to `Func<IRelic>` constructors — returns new instances
5. `RunContext` gains a `RelicManager` property initialized on construction
6. `GameRunner` subscribes to EventBus events and routes to RelicManager dispatch methods
7. Dispatch methods exist for: RoundStart, RoundEnd, AfterTrade, MarketEventFired, ReputationChanged, ShopOpen
8. A relic failing in dispatch does not break other relics (try-catch per relic, log error, continue)
9. `RelicManager.ReorderRelic(int fromIndex, int toIndex)` moves a relic and shifts others
10. `RelicManager.AddRelic(string relicId)` uses RelicFactory, calls OnAcquired, syncs with RunContext.OwnedRelics
11. `RelicManager.RemoveRelic(string relicId)` calls OnRemoved, removes from list, syncs with RunContext.OwnedRelics
12. New events added to GameEvents.cs: `RelicActivatedEvent` (for UI glow when a relic fires)

## Tasks / Subtasks

- [x] Task 1: Create IRelic interface (AC: 1)
  - [x] Define `Scripts/Runtime/Items/IRelic.cs` with all hook method signatures
  - [x] Each method receives `RunContext` plus the relevant event struct
  - [x] `string Id { get; }` property to match RelicDef.Id
- [x] Task 2: Create RelicBase abstract class (AC: 2)
  - [x] Define `Scripts/Runtime/Items/RelicBase.cs`
  - [x] Implement all IRelic methods as virtual no-ops (empty method bodies)
  - [x] Abstract `Id` property (subclasses must define)
- [x] Task 3: Create RelicManager (AC: 3, 7, 8, 9, 10, 11)
  - [x] Define `Scripts/Runtime/Items/RelicManager.cs`
  - [x] `List<IRelic> _orderedRelics` — internal ordered list
  - [x] `AddRelic(string relicId)` — create via factory, add to list, call OnAcquired
  - [x] `RemoveRelic(string relicId)` — find by Id, call OnRemoved, remove from list
  - [x] `ReorderRelic(int fromIndex, int toIndex)` — remove and insert
  - [x] `DispatchRoundStart(RoundStartedEvent e)` — iterate with try-catch (uses stored _ctx)
  - [x] `DispatchRoundEnd(MarketClosedEvent e)` — iterate with try-catch (uses stored _ctx)
  - [x] `DispatchBeforeTrade(TradeExecutedEvent e)` — iterate with try-catch (uses stored _ctx)
  - [x] `DispatchAfterTrade(TradeExecutedEvent e)` — iterate with try-catch (uses stored _ctx)
  - [x] `DispatchMarketEvent(MarketEventFiredEvent e)` — iterate with try-catch (uses stored _ctx)
  - [x] `DispatchReputationChanged(int oldRep, int newRep)` — iterate with try-catch (uses stored _ctx)
  - [x] `DispatchShopOpen()` — iterate with try-catch (uses stored _ctx)
  - [x] `DispatchSellSelf(string relicId)` — dispatch to specific relic only (uses stored _ctx)
  - [x] `GetRelicById(string id)` — lookup helper
  - [x] `IReadOnlyList<IRelic> OrderedRelics` — public read-only access for UI
- [x] Task 4: Create RelicFactory (AC: 4)
  - [x] Define `Scripts/Runtime/Items/RelicFactory.cs`
  - [x] `static Dictionary<string, Func<IRelic>> _registry`
  - [x] `static IRelic Create(string relicId)` — returns new instance or null
  - [x] `static void Register(string id, Func<IRelic> constructor)` — for registration
  - [x] Register placeholder/stub entries for all 8 placeholder relic IDs (actual implementations come in 17.3-17.7)
  - [x] Stub class: `StubRelic` that extends RelicBase with configurable Id, does nothing
- [x] Task 5: Wire into RunContext (AC: 5)
  - [x] Add `RelicManager RelicManager { get; private set; }` property to RunContext
  - [x] Initialize in RunContext constructor
  - [x] Fresh RelicManager created in ResetForNewRun()
- [x] Task 6: Wire EventBus to RelicManager dispatch in GameRunner (AC: 6)
  - [x] Subscribe to `RoundStartedEvent` → `ctx.RelicManager.DispatchRoundStart()`
  - [x] Subscribe to `MarketClosedEvent` → `ctx.RelicManager.DispatchRoundEnd()`
  - [x] Subscribe to `TradeExecutedEvent` → `ctx.RelicManager.DispatchAfterTrade()`
  - [x] Subscribe to `MarketEventFiredEvent` → `ctx.RelicManager.DispatchMarketEvent()`
  - [x] Wire ReputationManager.OnChanged → `ctx.RelicManager.DispatchReputationChanged()`
  - [x] In ShopState.Enter → `ctx.RelicManager.DispatchShopOpen()`
  - [x] All subscriptions unsubscribed in OnDestroy()
- [x] Task 7: Add RelicActivatedEvent (AC: 12)
  - [x] Add `RelicActivatedEvent` struct to GameEvents.cs with `string RelicId` field
  - [x] Relics can publish this event when they fire (for UI glow in Story 17.8)
- [x] Task 8: Write tests
  - [x] RelicManager: add/remove/reorder relics, dispatch order verification (21 tests)
  - [x] RelicFactory: create known ID returns instance, unknown ID returns null (8 tests)
  - [x] Dispatch: verify left-to-right order, verify try-catch (one failing relic doesn't break others)
  - [x] Files: `Tests/Runtime/Items/RelicManagerTests.cs`, `Tests/Runtime/Items/RelicFactoryTests.cs`

## Dev Notes

### Architecture Compliance

- **Player-orderable execution:** Per game-architecture.md, items process left-to-right through the player's arrangement. This is the Balatro joker ordering pattern.
- **One class per relic:** Each relic will be a separate file in `Scripts/Runtime/Items/Relics/` (created in Stories 17.3-17.7)
- **EventBus communication:** RelicManager subscribes via GameRunner — relics never reference game systems directly
- **Error handling:** Try-catch at dispatch boundary (per relic), log error, continue to next relic

### Existing Code to Read Before Implementing

- `Scripts/Runtime/Core/RunContext.cs` — current `OwnedRelics` field (List<string>), ReputationManager
- `Scripts/Runtime/Core/GameEvents.cs` — all event structs for hook method signatures
- `Scripts/Runtime/Core/GameRunner.cs` — where to add EventBus subscriptions
- `Scripts/Runtime/Core/GameStates/ShopState.cs` — where to call DispatchShopOpen
- `Scripts/Runtime/Core/ReputationManager.cs` — need to add OnChanged callback
- `Scripts/Runtime/Items/ItemLookup.cs` — existing relic lookup pattern

### Key Design Decisions

- IRelic uses specific typed methods (OnRoundStart, OnAfterTrade) rather than a single generic OnEvent — this is more explicit and avoids runtime type checking
- RelicBase provides virtual no-ops so subclasses are clean (only override what they use)
- RelicFactory uses a dictionary registry rather than reflection — simpler, faster, no assembly scanning
- Stub relics registered for all 23 IDs so the system works end-to-end before individual effects are implemented

### Project Structure Notes

- New folder: `Assets/Scripts/Runtime/Items/Relics/` — will hold all 23 relic classes (Stories 17.3-17.7)
- New files in `Assets/Scripts/Runtime/Items/`: IRelic.cs, RelicBase.cs, RelicManager.cs, RelicFactory.cs
- Per project-context.md: Items/Upgrades location is `Scripts/Runtime/Items/`

### References

- [Source: _bmad-output/planning-artifacts/game-architecture.md#Item/Modifier System]
- [Source: _bmad-output/planning-artifacts/epic-17-relic-system.md#Story 17.1]
- [Source: _bmad-output/project-context.md#EventBus Communication]

## Dev Agent Record

### Agent Model Used
Claude Opus 4.6

### Debug Log References
- Test run (initial): 1722 total, 1721 passed, 0 failed, 1 skipped (pre-existing)
- Test run (post-review): 1725 total, 1724 passed, 0 failed, 1 skipped (pre-existing)

### Completion Notes List
- ReputationManager.OnChanged event added (fires from Add, Spend, Reset) for relic dispatch
- RelicFactory auto-registers all 8 existing placeholder relics from ShopItemDefinitions.RelicPool as StubRelic instances
- RelicFactory includes ResetRegistry() for test cleanup (restores defaults after ClearRegistry)
- RelicManager.SyncOwnedRelics() keeps RunContext.OwnedRelics in sync with internal ordered list
- GameRunner wires 4 EventBus events + Reputation.OnChanged to RelicManager dispatch
- ShopState.Enter calls DispatchShopOpen after ShopOpenedEvent publish

### Code Review Fixes Applied
Review found 7 issues (2 HIGH, 3 MEDIUM, 1 LOW + 1 doc inconsistency). 6 fixed:

- **H1 (Critical)**: ShopTransaction.PurchaseRelic/SellRelic bypassed RelicManager — purchased relics never entered dispatch pipeline. Fixed by routing buy through `RelicManager.AddRelic()` and sell through `RelicManager.DispatchSellSelf()` + `RelicManager.RemoveRelic()`.
- **H2**: Missing `DispatchBeforeTrade` method — IRelic declared `OnBeforeTrade` but no dispatch existed. Added `DispatchBeforeTrade(TradeExecutedEvent e)` to RelicManager.
- **H3** (part of H1): `DispatchSellSelf` had zero call sites. Now called from `ShopTransaction.SellRelic`.
- **M1**: Redundant `RunContext` parameter on all dispatch methods — inconsistent with lifecycle hooks that use stored `_ctx`. Removed ctx param from all 8 dispatch methods.
- **M2**: Try-catch isolation only tested for `DispatchRoundStart`. Added `DispatchAfterTrade_FailingRelic_DoesNotBlockOthers` test.
- **M3**: `DispatchSellSelf` selectivity not tested. Added `DispatchSellSelf_OnlyTargetRelicIsCalled` test.
- **L1 (Not fixed)**: Story doc says "23 relics" but only 8 placeholder IDs exist — actual 23 come in Stories 17.2-17.7.

Cascade fix: 4 existing test files (ShopTransactionTests, RelicSellTests, RelicPurchaseTests, StoreDataModelTests) updated to register test relic IDs in RelicFactory and use `RelicManager.AddRelic` for pre-filling inventory, ensuring compatibility with the new RelicManager integration.

### File List
New files:
- `Assets/Scripts/Runtime/Items/IRelic.cs`
- `Assets/Scripts/Runtime/Items/RelicBase.cs`
- `Assets/Scripts/Runtime/Items/RelicManager.cs`
- `Assets/Scripts/Runtime/Items/RelicFactory.cs`
- `Assets/Scripts/Runtime/Items/Relics/StubRelic.cs`
- `Assets/Tests/Runtime/Items/RelicFactoryTests.cs` (8 tests)
- `Assets/Tests/Runtime/Items/RelicManagerTests.cs` (25 tests — 4 added in review)

Modified files:
- `Assets/Scripts/Runtime/Core/GameEvents.cs` — added RelicActivatedEvent
- `Assets/Scripts/Runtime/Core/ReputationManager.cs` — added OnChanged event
- `Assets/Scripts/Runtime/Core/RunContext.cs` — added RelicManager property
- `Assets/Scripts/Runtime/Core/GameRunner.cs` — EventBus → RelicManager dispatch wiring (review: removed ctx params)
- `Assets/Scripts/Runtime/Core/GameStates/ShopState.cs` — DispatchShopOpen call (review: removed ctx param)
- `Assets/Scripts/Runtime/Shop/ShopTransaction.cs` — review: route buy/sell through RelicManager
- `Assets/Tests/Runtime/Shop/ShopTransactionTests.cs` — review: factory registration in MakeRelic
- `Assets/Tests/Runtime/Shop/RelicSellTests.cs` — review: use RelicManager.AddRelic for test setup
- `Assets/Tests/Runtime/Shop/ClickToBuyTests.cs` — review: factory registration, real pool IDs for fill loops
- `Assets/Tests/Runtime/Shop/RelicPurchaseTests.cs` — review: factory registration, real pool IDs for fill loops
- `Assets/Tests/Runtime/Shop/StoreDataModelTests.cs` — review: factory registration, real pool IDs for fill loops
