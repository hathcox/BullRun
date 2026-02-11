# Story 1.3: Event Spike Integration

Status: review

## Story

As a player,
I want market events to cause sudden price spikes or drops that override the normal trend,
so that dramatic moments create trading opportunities.

## Acceptance Criteria

1. Events inject sharp price movements (spikes or drops) onto affected stocks
2. Spike magnitude is configurable per event type
3. Spikes create obvious visual signals — sharp V-shapes or sudden plateaus on the chart
4. Spikes override the base trend temporarily while the event is active
5. Multiple events can affect different stocks simultaneously
6. Event effects are applied through the PriceGenerator pipeline (not bypassing it)

## Tasks / Subtasks

- [x] Task 1: Create EventDefinitions static data class (AC: 2)
  - [x] Define `MarketEventType` enum: EarningsBeat, EarningsMiss, PumpAndDump, SECInvestigation, SectorRotation, MergerRumor, MarketCrash, BullRun, FlashCrash, ShortSqueeze
  - [x] Define `MarketEventConfig` struct: event type, price effect (% change), duration, tier availability, rarity
  - [x] Populate from GDD Section 3.4 table
  - [x] File: `Scripts/Setup/Data/EventDefinitions.cs`
- [x] Task 2: Create MarketEvent runtime class (AC: 1, 4)
  - [x] Fields: `EventType`, `TargetStockId` (null for global events), `PriceEffectPercent`, `Duration`, `ElapsedTime`, `IsActive`
  - [x] Method: `GetCurrentForce(float elapsed)` — returns interpolated event force (ramps up then fades)
  - [x] File: `Scripts/Runtime/Events/MarketEvent.cs`
- [x] Task 3: Create EventEffects processor (AC: 1, 3, 4)
  - [x] Method: `ApplyEventEffect(StockInstance stock, MarketEvent event, float deltaTime)` — calculates event's price impact
  - [x] Uses `Lerp(price, eventTarget, eventForce)` per architecture pattern
  - [x] Handles both single-stock and global (all-stock) events
  - [x] File: `Scripts/Runtime/Events/EventEffects.cs`
- [x] Task 4: Integrate event layer into PriceGenerator pipeline (AC: 4, 6)
  - [x] Add event spike step after noise: `if (activeEvent) price = Lerp(price, eventTarget, eventForce)`
  - [x] PriceGenerator checks for active events on each stock during UpdatePrice
  - [x] Active events stored on StockInstance or accessible via EventEffects
  - [x] File: `Scripts/Runtime/PriceEngine/PriceGenerator.cs` (extend)
- [x] Task 5: Add StockInstance event tracking fields (AC: 5)
  - [x] Add `ActiveEvent` reference (nullable) and `EventTargetPrice` field
  - [x] Method to apply/clear event state
  - [x] File: `Scripts/Runtime/PriceEngine/StockInstance.cs` (extend)
- [x] Task 6: Define event-related GameEvents (AC: 5)
  - [x] `MarketEventFiredEvent`: EventType, AffectedStockIds, PriceEffectPercent
  - [x] `MarketEventEndedEvent`: EventType, AffectedStockIds
  - [x] Add to `Scripts/Runtime/Core/GameEvents.cs`

## Dev Notes

### Architecture Compliance

- **Pipeline position:** Event spikes are step 3 of 4: trend → noise → **events** → reversion (Story 1.4)
- **Architecture formula:** `if (activeEvent) price = Lerp(price, eventTarget, eventForce)`
- **EventBus:** Publish `MarketEventFiredEvent` when an event starts — other systems (UI, audio) will subscribe later
- **Data in EventDefinitions.cs** — all event parameters as `public static readonly`
- **One file per concern:** MarketEvent.cs is the data model, EventEffects.cs is the processor, EventScheduler.cs comes in Epic 5

### Important: This Story Does NOT Include Scheduling

This story implements the **price effect** of events, not the scheduling/triggering. The EventScheduler (Epic 5, Story 5.1) will determine when events fire. For now, events should be applicable via a simple API call like:

```csharp
// Something external triggers this (EventScheduler in Epic 5, or F4 debug key)
eventEffects.StartEvent(new MarketEvent(MarketEventType.EarningsBeat, stockId, priceEffect: 0.25f, duration: 5f));
```

### Event Effect Magnitudes (GDD Section 3.4)

| Event Type | Price Effect | Tier |
|------------|-------------|------|
| Earnings Beat | +15–30% | All |
| Earnings Miss | -15–30% | All |
| Pump & Dump | Rapid rise then crash | Penny |
| SEC Investigation | -20–40% gradual | Penny, Low |
| Sector Rotation | +/- mixed | Mid, Blue |
| Merger Rumor | Surge on target | Mid, Blue |
| Market Crash | Sharp drop all stocks | All (rare) |
| Bull Run | Steady rise all stocks | All (rare) |
| Flash Crash | Drop then recover | Low, Mid |
| Short Squeeze | Violent spike | All |

### Design Principle

Events are the **primary driver of dramatic moments**. They should create obvious visual signals that skilled players learn to recognize. The price effect should be sharp and readable — not subtle. Think of events as the "boss attacks" of each round.

### Project Structure Notes

- Creates: `Scripts/Setup/Data/EventDefinitions.cs`
- Creates: `Scripts/Runtime/Events/MarketEvent.cs`
- Creates: `Scripts/Runtime/Events/EventEffects.cs`
- Modifies: `Scripts/Runtime/PriceEngine/PriceGenerator.cs`
- Modifies: `Scripts/Runtime/PriceEngine/StockInstance.cs`
- Modifies: `Scripts/Runtime/Core/GameEvents.cs`

### References

- [Source: game-architecture.md#Price Engine] — `if (activeEvent) price = Lerp(price, eventTarget, eventForce)` pipeline step
- [Source: game-architecture.md#Event System] — EventBus publish/subscribe pattern
- [Source: game-architecture.md#Project Structure] — Events/ folder under Runtime
- [Source: bull-run-gdd-mvp.md#3.3] — "Event Spikes: Market events inject sudden price movements"
- [Source: bull-run-gdd-mvp.md#3.4] — Complete event type table with effects and tier availability

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Debug Log References

None — no blocking issues encountered during implementation. All tests pass (confirmed 2026-02-10).

### Completion Notes List

- **Task 1:** Created `EventDefinitions.cs` with `MarketEventType` enum (10 event types), `MarketEventConfig` readonly struct (eventType, minPriceEffect, maxPriceEffect, duration, tierAvailability, rarity), and static data populated from GDD Section 3.4 table. All event configs use `public static readonly` per project conventions. Lookup via `GetConfig(MarketEventType)`.
- **Task 2:** Created `MarketEvent.cs` runtime class with all specified fields. `GetCurrentForce()` uses a triangle wave envelope — linearly ramps from 0→1 over first half of duration, then 1→0 over second half. Supports nullable `TargetStockId` for global events. `IsActive` checks elapsed < duration.
- **Task 3:** Created `EventEffects.cs` processor. `ApplyEventEffect` uses `Mathf.Lerp(currentPrice, eventTarget, force * deltaTime)` per architecture spec. `StartEvent` publishes `MarketEventFiredEvent` via EventBus. `UpdateActiveEvents` advances elapsed time and removes expired events (publishing `MarketEventEndedEvent`). `GetActiveEventsForStock` returns both targeted and global events.
- **Task 4:** Extended `PriceGenerator.UpdatePrice` pipeline: trend → noise → **events** → clamp. Added `SetEventEffects(EventEffects)` method for dependency injection. Event integration is optional (null-safe) — existing code works unchanged without events. Multiple active events are applied sequentially per stock.
- **Task 5:** Added `ActiveEvent` (nullable reference) and `EventTargetPrice` fields to `StockInstance`. Added `ApplyEvent(MarketEvent, float targetPrice)` and `ClearEvent()` methods. Fields initialized to null/0 in `Initialize()`.
- **Task 6:** Added `MarketEventFiredEvent` struct (EventType, AffectedStockIds, PriceEffectPercent) and `MarketEventEndedEvent` struct (EventType, AffectedStockIds) to `GameEvents.cs`. Both follow `{Subject}{Verb}Event` naming convention. `AffectedStockIds` is null for global events.

### File List

- `Assets/Scripts/Setup/Data/EventDefinitions.cs` (new)
- `Assets/Scripts/Runtime/Events/MarketEvent.cs` (new)
- `Assets/Scripts/Runtime/Events/EventEffects.cs` (new)
- `Assets/Scripts/Runtime/PriceEngine/PriceGenerator.cs` (modified)
- `Assets/Scripts/Runtime/PriceEngine/StockInstance.cs` (modified)
- `Assets/Scripts/Runtime/Core/GameEvents.cs` (modified)
- `Assets/Tests/Runtime/Events/EventDefinitionsTests.cs` (new)
- `Assets/Tests/Runtime/Events/MarketEventTests.cs` (new)
- `Assets/Tests/Runtime/Events/EventEffectsTests.cs` (new)
- `Assets/Tests/Runtime/PriceEngine/PriceGeneratorTests.cs` (modified)
- `Assets/Tests/Runtime/PriceEngine/StockInstanceTests.cs` (modified)
- `Assets/Tests/Runtime/Core/GameEventsTests.cs` (modified)

## Change Log

- 2026-02-10: Implemented event spike integration — 3 new source files, 3 modified source files, 3 new test files, 3 modified test files. Event effects pipeline step added to PriceGenerator. All 10 GDD event types configured with price effects, durations, tier availability, and rarity.
