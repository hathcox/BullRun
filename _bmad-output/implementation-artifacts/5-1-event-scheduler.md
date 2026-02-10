# Story 5.1: Event Scheduler

Status: ready-for-dev

## Story

As a developer,
I want an event system that schedules 2-4 events per round at randomized intervals,
so that rounds have dynamic price catalysts that create trading opportunities.

## Acceptance Criteria

1. Each round has 2-3 events scheduled in early rounds, 3-4 in late rounds
2. Events are scheduled at randomized intervals across the trading phase duration
3. Events do not cluster in the first or last 5 seconds — spread across the middle 80% of the round
4. Event types are selected based on current stock tier availability
5. The scheduler fires events at the scheduled time during TradingState update
6. Scheduler integrates with EventEffects (Story 1.3) to apply price impacts
7. F4 debug key can force-fire any event type on any stock

## Tasks / Subtasks

- [ ] Task 1: Create EventScheduler (AC: 1, 2, 3, 4)
  - [ ] Method: `ScheduleRound(int roundNumber, int act, StockTier tier, List<StockInstance> stocks, float roundDuration)`
  - [ ] Determine event count: 2-3 for rounds 1-4, 3-4 for rounds 5-8
  - [ ] Randomize fire times across 10%-90% of round duration (avoid edges)
  - [ ] Select event types filtered by tier availability from EventDefinitions
  - [ ] Select target stocks (random from available, or all stocks for global events)
  - [ ] Store as list of `ScheduledEvent` structs sorted by fire time
  - [ ] File: `Scripts/Runtime/Events/EventScheduler.cs`
- [ ] Task 2: Create ScheduledEvent data struct (AC: 2)
  - [ ] Fields: `FireTime` (float, seconds into round), `EventType`, `TargetStockIds` (list), `PriceEffectPercent`, `Duration`
  - [ ] Field: `HasFired` (bool) to track execution
  - [ ] File: `Scripts/Runtime/Events/EventScheduler.cs` (inner struct or separate)
- [ ] Task 3: Implement event firing during TradingState (AC: 5, 6)
  - [ ] Method: `Update(float elapsedTime)` — checks scheduled events against elapsed time
  - [ ] When `elapsedTime >= scheduledEvent.FireTime && !HasFired`: fire the event
  - [ ] Firing: create `MarketEvent` instance, pass to `EventEffects.StartEvent()` (from Story 1.3)
  - [ ] Publish `MarketEventFiredEvent` via EventBus
  - [ ] TradingState calls `EventScheduler.Update()` each frame
  - [ ] File: `Scripts/Runtime/Events/EventScheduler.cs` (extend)
- [ ] Task 4: Add event frequency config to StockTierData (AC: 1)
  - [ ] Add `EventCountMin` and `EventCountMax` per tier (or per round range)
  - [ ] Early rounds (1-4): min=2, max=3
  - [ ] Late rounds (5-8): min=3, max=4
  - [ ] File: `Scripts/Setup/Data/EventDefinitions.cs` (extend) or `Scripts/Setup/Data/GameConfig.cs`
- [ ] Task 5: Add F4 debug event trigger (AC: 7)
  - [ ] In DebugManager: F4 opens a simple selector to pick event type and target stock
  - [ ] Force-fires the selected event immediately via EventScheduler
  - [ ] Wrap in `#if UNITY_EDITOR || DEVELOPMENT_BUILD`
  - [ ] File: `Scripts/Editor/DebugManager.cs` (extend from Story 1.6)
- [ ] Task 6: Wire scheduler into MarketOpenState (AC: 1)
  - [ ] MarketOpenState.Enter() calls `EventScheduler.ScheduleRound()` after stock initialization
  - [ ] Events are ready before trading begins
  - [ ] File: `Scripts/Runtime/Core/GameStates/MarketOpenState.cs` (extend from Story 4.2)

## Dev Notes

### Architecture Compliance

- **Location:** `Scripts/Runtime/Events/EventScheduler.cs` per architecture project structure
- **Separation of concerns:** EventScheduler decides WHEN events fire. EventEffects (Story 1.3) decides HOW they affect prices. MarketEvent is the data model.
- **EventBus:** Publishes `MarketEventFiredEvent` — UI (Story 5.5) and audio (Epic 11) subscribe to this
- **TradingState drives scheduling:** TradingState.Update() calls EventScheduler.Update(). The scheduler doesn't run itself.

### Event Timing Distribution

Avoid predictable patterns. Spread events across the round:

```
Round duration: 60 seconds
Safe zone: 6s - 54s (10% - 90%)
3 events example: fire at ~15s, ~30s, ~48s (randomized ±5s)
```

Use a simple algorithm:
1. Divide the safe zone into N equal segments (N = event count)
2. Place one event randomly within each segment
3. This guarantees spacing while maintaining randomness

### Event Type Selection

Filter `EventDefinitions` by tier availability:
- Round in Act 1 (Penny): Earnings Beat/Miss, Pump & Dump, Bull Run, Market Crash, Short Squeeze
- Round in Act 2 (Low): Earnings Beat/Miss, SEC Investigation, Flash Crash, Short Squeeze
- Round in Act 3-4 (Mid/Blue): Earnings Beat/Miss, Sector Rotation, Merger Rumor, Market Crash, Bull Run

Avoid repeating the same event type twice in one round unless the pool is small.

### Integration with Story 1.3

EventScheduler creates `MarketEvent` instances and passes them to `EventEffects.StartEvent()`. The price impact pipeline (trend → noise → events → reversion) is already wired from Epic 1. This story completes the loop by providing the trigger mechanism.

### Project Structure Notes

- Creates: `Scripts/Runtime/Events/EventScheduler.cs`
- Modifies: `Scripts/Setup/Data/EventDefinitions.cs` or `Scripts/Setup/Data/GameConfig.cs`
- Modifies: `Scripts/Editor/DebugManager.cs`
- Modifies: `Scripts/Runtime/Core/GameStates/MarketOpenState.cs`
- Modifies: `Scripts/Runtime/Core/GameStates/TradingState.cs` (call scheduler update)
- Depends on: EventEffects (Story 1.3), MarketEvent (Story 1.3), EventDefinitions (Story 1.3)

### References

- [Source: game-architecture.md#Project Structure] — Events/EventScheduler.cs location
- [Source: game-architecture.md#Debug Tools] — "F4: Force-fire any event type on any stock"
- [Source: bull-run-gdd-mvp.md#3.4] — "Events are the primary source of gameplay variety within rounds. Each round has 2-4 events."
- [Source: bull-run-gdd-mvp.md#11.1] — "Events per Round: 2-3 (early), 3-4 (late)"
- [Source: bull-run-gdd-mvp.md#3.4] — Event type table with tier availability

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
