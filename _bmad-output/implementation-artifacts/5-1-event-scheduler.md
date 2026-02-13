# Story 5.1: Event Scheduler

Status: done

## Story

As a developer,
I want an event system that schedules 2-4 events per round at randomized intervals,
so that rounds have dynamic price catalysts that create trading opportunities and gameplay variety.

## Acceptance Criteria

1. Each round schedules 2-3 events in early rounds (Acts 1-2) and 3-4 events in late rounds (Acts 3-4)
2. Events fire at randomized intervals during the trading phase (not clustered at start or end)
3. Event timing and types are configurable per round/tier via static data
4. Events are selected from the tier-appropriate pool using rarity-weighted random selection
5. The scheduler respects `StockTierConfig.EventFrequencyModifier` to scale event frequency per tier
6. Events target specific stocks (stock-specific events) or all stocks (global events like MarketCrash/BullRun)
7. The scheduler integrates into the existing TradingState update loop
8. EventEffects (already implemented) processes the fired events — scheduler only decides WHEN and WHAT fires

## Tasks / Subtasks

- [x] Task 1: Create EventScheduler class with round initialization (AC: 1, 3)
  - [x] Create `Scripts/Runtime/Events/EventScheduler.cs`
  - [x] `InitializeRound(int round, int act, StockTier tier, List<StockInstance> activeStocks, float roundDuration)` — determines event count and pre-schedules fire times
  - [x] Event count: 2-3 for Acts 1-2, 3-4 for Acts 3-4 (use `EventSchedulerConfig` in data)
  - [x] Pre-schedule fire times: distribute events across the round duration with randomized spacing (avoid first 5s and last 5s buffer)
  - [x] Store scheduled event slots with fire times, but defer type selection until fire time (keeps events unpredictable)

- [x] Task 2: Implement tier-aware event type selection with rarity weighting (AC: 4, 5)
  - [x] `SelectEventType(StockTier tier)` — filters `EventDefinitions` configs by `TierAvailability`, then weighted random by `Rarity`
  - [x] Rarity acts as relative weight (0.5 = common, 0.1 = very rare) — normalize to probability distribution
  - [x] Add `GetEventsForTier(StockTier tier)` helper method to `EventDefinitions`
  - [x] Validate at least one event type is available for the tier (all tiers have at least EarningsBeat/EarningsMiss)
  - [x] Unit tests: verify tier filtering excludes unavailable events, verify rarity weighting produces expected distribution over many samples

- [x] Task 3: Implement event firing with stock targeting (AC: 6, 8)
  - [x] `FireEvent(MarketEventConfig config, List<StockInstance> activeStocks)` — creates `MarketEvent` instance and calls `EventEffects.StartEvent()`
  - [x] Global events (MarketCrash, BullRun): `TargetStockId = null` (affects all stocks)
  - [x] Stock-specific events: random stock selection from active stocks list
  - [x] Roll `PriceEffectPercent` between config's `MinPriceEffect` and `MaxPriceEffect`
  - [x] Use event's `Duration` from config

- [x] Task 4: Implement per-frame Update with scheduled fire times (AC: 2, 7)
  - [x] `Update(float elapsedTime, float deltaTime, List<StockInstance> activeStocks, StockTier tier)` — checks if any scheduled events should fire based on elapsed time
  - [x] Also calls `EventEffects.UpdateActiveEvents(deltaTime)` to advance active event timers
  - [x] Fires events when `elapsedTime >= scheduledFireTime` for each pending slot
  - [x] Marks slots as fired so they don't re-trigger

- [x] Task 5: Create EventSchedulerConfig static data (AC: 3)
  - [x] Add `EventSchedulerConfig` class to `Scripts/Setup/Data/EventDefinitions.cs` (co-located with event data)
  - [x] Fields: `MinEventsEarlyRounds`, `MaxEventsEarlyRounds`, `MinEventsLateRounds`, `MaxEventsLateRounds`
  - [x] Fields: `EarlyBufferSeconds` (5s), `LateBufferSeconds` (5s) — no events in first/last N seconds
  - [x] Default values: early rounds 2-3, late rounds 3-4, buffers 5s each

- [x] Task 6: Integrate EventScheduler into TradingState (AC: 7)
  - [x] Add `EventScheduler` field to `TradingStateConfig`
  - [x] In `TradingState.Enter()`: call `EventScheduler.InitializeRound()` with current round/act/tier/stocks
  - [x] In `TradingState.AdvanceTime()`: call `EventScheduler.Update()` each frame BEFORE PriceGenerator updates
  - [x] Wire up in setup code where TradingStateConfig is created (likely `MarketOpenState.cs`)
  - [x] Ensure EventScheduler receives the same `EventEffects` instance used by `PriceGenerator`

- [x] Task 7: Write comprehensive tests (AC: 1-8)
  - [x] Test: InitializeRound schedules correct event count for early vs late rounds
  - [x] Test: Events fire at scheduled times (not before, not after)
  - [x] Test: Tier filtering correctly excludes unavailable events (e.g., PumpAndDump not in BlueChip)
  - [x] Test: Rarity weighting produces valid selection (no crashes, returns valid type)
  - [x] Test: Global events (MarketCrash, BullRun) get null TargetStockId
  - [x] Test: Stock-specific events get a valid TargetStockId from active stocks
  - [x] Test: No events fire in buffer zones (first/last 5s)
  - [x] Test: EventEffects.StartEvent is called when events fire
  - [x] File: `Tests/Runtime/Events/EventSchedulerTests.cs`

## Dev Notes

### Architecture Compliance

- **EventScheduler is a pure C# class** (no MonoBehaviour) — follows the pattern of `EventEffects`, `PriceGenerator`, `TradeExecutor`
- **EventBus:** EventScheduler does NOT publish events directly — it delegates to `EventEffects.StartEvent()` which publishes `MarketEventFiredEvent`. This preserves the existing event flow.
- **Data access:** All config via static data classes (`EventDefinitions`, `StockTierData`, `GameConfig`)
- **No direct system references:** EventScheduler receives `EventEffects` as a dependency, not a direct reference to PriceGenerator
- **Existing infrastructure is solid:** `EventEffects.StartEvent()`, `MarketEvent` force curves, `EventDefinitions` configs — all fully implemented and tested. The scheduler just fills the missing "when to fire" gap.

### Integration Points

The scheduler slots into the existing pipeline:

```
TradingState.AdvanceTime(deltaTime)
  ├── EventScheduler.Update(elapsedTime, deltaTime, activeStocks, tier)
  │     ├── Check if any scheduled event should fire
  │     ├── If yes: SelectEventType() → FireEvent() → EventEffects.StartEvent()
  │     └── EventEffects.UpdateActiveEvents(deltaTime)  // advance active event timers
  └── PriceGenerator.UpdatePrice(stock, deltaTime)
        └── EventEffects.ApplyEventEffect(stock, event, deltaTime)  // already exists
```

**Key:** EventScheduler.Update() MUST be called BEFORE PriceGenerator.UpdatePrice() so that newly fired events affect prices in the same frame.

### Existing Infrastructure (DO NOT recreate)

| Component | File | Status |
|-----------|------|--------|
| `MarketEvent` | `Runtime/Events/MarketEvent.cs` | Complete — force curve, elapsed time, global/targeted |
| `EventEffects` | `Runtime/Events/EventEffects.cs` | Complete — StartEvent, ApplyEventEffect, UpdateActiveEvents |
| `EventDefinitions` | `Setup/Data/EventDefinitions.cs` | Complete — all 10 event types with configs |
| `MarketEventFiredEvent` | `Runtime/Core/GameEvents.cs` | Complete — published by EventEffects.StartEvent() |
| `MarketEventEndedEvent` | `Runtime/Core/GameEvents.cs` | Complete — published by EventEffects on expiry |
| `StockTierConfig.EventFrequencyModifier` | `Setup/Data/StockTierData.cs` | Complete — per-tier scaling |

### Event Count Logic

```csharp
// Determine event count for this round
bool isLateRound = (act >= 3);
int minEvents = isLateRound ? config.MinEventsLateRounds : config.MinEventsEarlyRounds;
int maxEvents = isLateRound ? config.MaxEventsLateRounds : config.MaxEventsEarlyRounds;
int eventCount = Random.Range(minEvents, maxEvents + 1);
```

### Event Timing Distribution

Events should be spread across the round to avoid clustering. Algorithm:
1. Define usable window: `[EarlyBuffer, RoundDuration - LateBuffer]` (e.g., [5s, 55s] for 60s round)
2. Divide window into `eventCount` equal segments
3. For each segment, pick a random time within it
4. This ensures events are roughly evenly spaced but not predictable

```csharp
float windowStart = config.EarlyBufferSeconds;
float windowEnd = roundDuration - config.LateBufferSeconds;
float segmentLength = (windowEnd - windowStart) / eventCount;

for (int i = 0; i < eventCount; i++)
{
    float segStart = windowStart + (i * segmentLength);
    float segEnd = segStart + segmentLength;
    scheduledTimes[i] = Random.Range(segStart, segEnd);
}
```

### Rarity-Weighted Selection

```csharp
// Filter events available for this tier
var available = EventDefinitions.GetEventsForTier(tier);

// Sum rarities for weight normalization
float totalWeight = 0f;
foreach (var cfg in available) totalWeight += cfg.Rarity;

// Weighted random selection
float roll = Random.Range(0f, totalWeight);
float cumulative = 0f;
foreach (var cfg in available)
{
    cumulative += cfg.Rarity;
    if (roll <= cumulative) return cfg;
}
```

### Global vs Stock-Specific Events

| Event Type | Targeting | Notes |
|-----------|-----------|-------|
| MarketCrash | Global (null) | Affects ALL stocks |
| BullRun | Global (null) | Affects ALL stocks |
| All others | Stock-specific | Random stock from active list |

### Previous Story Learnings (from Epics 1-6)

- TradingStateConfig pattern works well for dependency injection — follow same pattern for EventScheduler
- Static `NextConfig` field set before `TransitionTo<>()` call
- Pure C# classes with no MonoBehaviour dependency are highly testable
- Keep tests focused on logic, not Unity internals
- `RunContext` provides act/round info needed for event count decisions
- EventEffects already handles MarketEventFiredEvent publishing — don't duplicate

### Project Structure Notes

- New file: `Assets/Scripts/Runtime/Events/EventScheduler.cs`
- New file: `Assets/Tests/Runtime/Events/EventSchedulerTests.cs`
- Modified: `Assets/Scripts/Setup/Data/EventDefinitions.cs` (add EventSchedulerConfig + GetEventsForTier helper)
- Modified: `Assets/Scripts/Runtime/Core/GameStates/TradingState.cs` (add EventScheduler to config and update loop)
- Modified: Setup code that creates TradingStateConfig (wire EventScheduler instance)
- Existing tests: `Tests/Runtime/Events/EventEffectsTests.cs` — should continue to pass

### References

- [Source: bull-run-gdd-mvp.md#3.4] — "Each round has 2-4 events that fire at randomized intervals during the trading phase"
- [Source: epics.md#5.1] — "Schedule 2-3 events in early rounds, 3-4 in late rounds. Event timing and types configurable per round/tier."
- [Source: game-architecture.md#Market Events] — EventScheduler planned location: `Scripts/Runtime/Events/`
- [Source: game-architecture.md#Debug Tools] — "F4: Force-fire any event type on any stock"
- [Source: game-architecture.md#Event System] — "Central event bus with typed events, synchronous dispatch"
- [Source: project-context.md#Event System] — "Event scheduling, event effects on prices, tier-specific event availability"
- [Source: StockTierData.cs] — EventFrequencyModifier per tier for frequency scaling

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Debug Log References

No debug issues encountered.

### Completion Notes List

- **Task 5:** Created `EventSchedulerConfig` static data class in `EventDefinitions.cs` with configurable event counts (early 2-3, late 3-4) and buffer zones (5s each).
- **Task 2:** Added `GetEventsForTier(StockTier)` helper to `EventDefinitions` that filters all event configs by `TierAvailability`. Also implemented `SelectEventType()` with rarity-weighted random selection using cumulative weight algorithm.
- **Task 1:** Created `EventScheduler.cs` as a pure C# class. `InitializeRound()` determines event count based on act (early/late), applies `StockTierConfig.EventFrequencyModifier` scaling, and pre-schedules fire times using segment-based distribution within the buffered window. Type selection is deferred to fire time for unpredictability.
- **Task 3:** Implemented `FireEvent()` which creates `MarketEvent` instances and delegates to `EventEffects.StartEvent()`. Global events (MarketCrash, BullRun) use null `TargetStockId`; stock-specific events randomly select from active stocks. Price effect is rolled between config min/max.
- **Task 4:** Implemented `Update()` method that checks elapsed time against scheduled fire times, fires events when due, marks slots as fired to prevent re-triggering, and calls `EventEffects.UpdateActiveEvents()` every frame to advance active event timers.
- **Task 6:** Threaded `EventScheduler` through the entire state chain: `GameRunner` creates EventEffects + EventScheduler, passes through `MarketOpenStateConfig` → `TradingStateConfig` → `MarketCloseStateConfig` → `MarginCallStateConfig` → `ShopStateConfig` → `TierTransitionStateConfig` → `MetaHubStateConfig` → `RunSummaryStateConfig`. TradingState calls `InitializeRound()` in `Enter()` and `Update()` in `AdvanceTime()` before price updates.
- **Task 7:** Created 18 comprehensive tests covering: event count for early/late rounds, frequency modifier scaling, buffer zone enforcement, tier filtering, rarity weighting, global vs stock-specific targeting, event firing via EventEffects, per-frame update timing, re-fire prevention, EventSchedulerConfig defaults, and GetEventsForTier validation.

## Senior Developer Review (AI)

**Review Date:** 2026-02-12
**Reviewer:** Claude Opus 4.6 (Code Review)
**Outcome:** Changes Requested (5 issues found, all fixed)

### Action Items

- [x] [HIGH] Per-frame heap allocation in TradingState.AdvanceTime — `new List<StockInstance>` every frame violates project-context.md performance rules [TradingState.cs:134]
- [x] [HIGH] Same unnecessary allocation in TradingState.Enter [TradingState.cs:77]
- [x] [MED] GetEventsForTier allocates new List on every call — should cache since tier configs are static [EventDefinitions.cs:204]
- [x] [MED] Test assertions too loose — AC says 2-3/3-4 but tests accepted 1-4/1-5 [EventSchedulerTests.cs:59-60,79-80]
- [x] [MED] No test for BlueChip low frequency modifier (0.5x) lower-bound behavior [EventSchedulerTests.cs]

### Fixes Applied

- Changed `EventScheduler.Update()`, `InitializeRound()`, and `FireEvent()` to accept `IReadOnlyList<StockInstance>` instead of `List<StockInstance>`, eliminating per-frame list copy allocations in TradingState
- Added `_tierEventsCache` dictionary to `EventDefinitions.GetEventsForTier()` so results are computed once and cached
- Tightened test assertions: early rounds now assert 2-3 (not 1-4), late rounds assert 3-4 (not 1-5)
- Added `InitializeRound_LowFrequencyTier_ReducesEventCount` test verifying BlueChip 0.5x modifier produces fewer events and minimum of 1 is enforced

### Change Log

- 2026-02-12: Implemented EventScheduler system — all 7 tasks complete. Created EventScheduler class, EventSchedulerConfig data, GetEventsForTier helper, integrated into full game state chain, wrote comprehensive test suite.
- 2026-02-12: Code review fixes — eliminated per-frame heap allocations (IReadOnlyList), cached tier event lookups, tightened test assertions to match ACs, added BlueChip low-modifier test.

### File List

- `Assets/Scripts/Runtime/Events/EventScheduler.cs` (NEW)
- `Assets/Tests/Runtime/Events/EventSchedulerTests.cs` (NEW)
- `Assets/Scripts/Setup/Data/EventDefinitions.cs` (MODIFIED — added GetEventsForTier, EventSchedulerConfig)
- `Assets/Scripts/Runtime/Core/GameStates/TradingState.cs` (MODIFIED — EventScheduler integration)
- `Assets/Scripts/Runtime/Core/GameStates/MarketOpenState.cs` (MODIFIED — EventScheduler in config)
- `Assets/Scripts/Runtime/Core/GameStates/MarketCloseState.cs` (MODIFIED — EventScheduler in config)
- `Assets/Scripts/Runtime/Core/GameStates/MarginCallState.cs` (MODIFIED — EventScheduler in config)
- `Assets/Scripts/Runtime/Core/GameStates/ShopState.cs` (MODIFIED — EventScheduler in config)
- `Assets/Scripts/Runtime/Core/GameStates/TierTransitionState.cs` (MODIFIED — EventScheduler in config)
- `Assets/Scripts/Runtime/Core/GameStates/MetaHubState.cs` (MODIFIED — EventScheduler in config)
- `Assets/Scripts/Runtime/Core/GameStates/RunSummaryState.cs` (MODIFIED — EventScheduler in config)
- `Assets/Scripts/Runtime/Core/GameRunner.cs` (MODIFIED — creates EventEffects + EventScheduler)
