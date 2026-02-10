# Story 1.3: Event Spike Integration

Status: ready-for-dev

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

- [ ] Task 1: Create EventDefinitions static data class (AC: 2)
  - [ ] Define `MarketEventType` enum: EarningsBeat, EarningsMiss, PumpAndDump, SECInvestigation, SectorRotation, MergerRumor, MarketCrash, BullRun, FlashCrash, ShortSqueeze
  - [ ] Define `MarketEventConfig` struct: event type, price effect (% change), duration, tier availability, rarity
  - [ ] Populate from GDD Section 3.4 table
  - [ ] File: `Scripts/Setup/Data/EventDefinitions.cs`
- [ ] Task 2: Create MarketEvent runtime class (AC: 1, 4)
  - [ ] Fields: `EventType`, `TargetStockId` (null for global events), `PriceEffectPercent`, `Duration`, `ElapsedTime`, `IsActive`
  - [ ] Method: `GetCurrentForce(float elapsed)` — returns interpolated event force (ramps up then fades)
  - [ ] File: `Scripts/Runtime/Events/MarketEvent.cs`
- [ ] Task 3: Create EventEffects processor (AC: 1, 3, 4)
  - [ ] Method: `ApplyEventEffect(StockInstance stock, MarketEvent event, float deltaTime)` — calculates event's price impact
  - [ ] Uses `Lerp(price, eventTarget, eventForce)` per architecture pattern
  - [ ] Handles both single-stock and global (all-stock) events
  - [ ] File: `Scripts/Runtime/Events/EventEffects.cs`
- [ ] Task 4: Integrate event layer into PriceGenerator pipeline (AC: 4, 6)
  - [ ] Add event spike step after noise: `if (activeEvent) price = Lerp(price, eventTarget, eventForce)`
  - [ ] PriceGenerator checks for active events on each stock during UpdatePrice
  - [ ] Active events stored on StockInstance or accessible via EventEffects
  - [ ] File: `Scripts/Runtime/PriceEngine/PriceGenerator.cs` (extend)
- [ ] Task 5: Add StockInstance event tracking fields (AC: 5)
  - [ ] Add `ActiveEvent` reference (nullable) and `EventTargetPrice` field
  - [ ] Method to apply/clear event state
  - [ ] File: `Scripts/Runtime/PriceEngine/StockInstance.cs` (extend)
- [ ] Task 6: Define event-related GameEvents (AC: 5)
  - [ ] `MarketEventFiredEvent`: EventType, AffectedStockIds, PriceEffectPercent
  - [ ] `MarketEventEndedEvent`: EventType, AffectedStockIds
  - [ ] Add to `Scripts/Runtime/Core/GameEvents.cs`

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

### Debug Log References

### Completion Notes List

### File List
