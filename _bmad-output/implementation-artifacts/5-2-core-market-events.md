# Story 5.2: Core Market Events

Status: ready-for-dev

## Story

As a player,
I want earnings beat/miss events that cause stocks to rise or drop 15-30%,
so that there are clear, learnable trading opportunities each round.

## Acceptance Criteria

1. Earnings Beat event: target stock rises 15-30% over the event duration
2. Earnings Miss event: target stock drops 15-30% over the event duration
3. Flash Crash event: single stock drops sharply then recovers (V-shape)
4. Bull Run event: all stocks rise steadily for the event duration (rare)
5. Each event has configurable duration (how long the effect lasts)
6. Event magnitude varies within the defined range (randomized per occurrence)
7. Events are available across all tiers (core events, not tier-locked)

## Tasks / Subtasks

- [ ] Task 1: Implement Earnings Beat effect (AC: 1, 5, 6)
  - [ ] Target: single stock, selected by scheduler
  - [ ] Effect: price Lerps toward target price (current + 15-30% of current)
  - [ ] Duration: 5-8 seconds of active effect
  - [ ] Randomize magnitude within range on each occurrence
  - [ ] File: `Scripts/Runtime/Events/EventEffects.cs` (extend from Story 1.3)
- [ ] Task 2: Implement Earnings Miss effect (AC: 2, 5, 6)
  - [ ] Target: single stock
  - [ ] Effect: price Lerps toward target price (current - 15-30% of current)
  - [ ] Duration: 5-8 seconds
  - [ ] Mirror of Earnings Beat but downward
  - [ ] File: `Scripts/Runtime/Events/EventEffects.cs` (extend)
- [ ] Task 3: Implement Flash Crash effect (AC: 3)
  - [ ] Target: single stock
  - [ ] Two-phase effect: sharp drop (3s) then recovery (5s)
  - [ ] Drop: price falls 20-40% rapidly
  - [ ] Recovery: price Lerps back toward pre-crash level (mean reversion accelerated)
  - [ ] Creates a V-shape on the chart — buy opportunity for quick players
  - [ ] File: `Scripts/Runtime/Events/EventEffects.cs` (extend)
- [ ] Task 4: Implement Bull Run effect (AC: 4)
  - [ ] Target: ALL active stocks (global event)
  - [ ] Effect: all stock trend biases temporarily pushed bullish
  - [ ] Duration: 8-12 seconds
  - [ ] Magnitude: each stock rises 5-15% above where it would have been
  - [ ] Rare occurrence — EventScheduler should weight this low
  - [ ] File: `Scripts/Runtime/Events/EventEffects.cs` (extend)
- [ ] Task 5: Populate EventDefinitions for core events (AC: 5, 6)
  - [ ] Ensure EarningsBeat, EarningsMiss, FlashCrash, BullRun configs are complete
  - [ ] Fields per event: min/max magnitude, min/max duration, tier list, rarity weight
  - [ ] File: `Scripts/Setup/Data/EventDefinitions.cs` (extend from Story 1.3)
- [ ] Task 6: Add multi-phase event support to MarketEvent (AC: 3)
  - [ ] Flash Crash needs two phases (drop then recover)
  - [ ] Add phase support: `MarketEvent.Phases` list with per-phase target and duration
  - [ ] EventEffects processes current phase based on elapsed time
  - [ ] File: `Scripts/Runtime/Events/MarketEvent.cs` (extend from Story 1.3)

## Dev Notes

### Architecture Compliance

- **Extends Story 1.3 infrastructure** — EventEffects already handles `StartEvent()` and price Lerping
- **Data in EventDefinitions** — all event configs as `public static readonly`
- **Pipeline integration:** Events are step 3 of the price pipeline. No changes to pipeline flow needed.
- **EventBus:** `MarketEventFiredEvent` is already published by EventScheduler (Story 5.1)

### Flash Crash — Multi-Phase Design

Flash Crash is the most complex core event because it has two phases:

```
Phase 1 (Drop):   0s-3s  → price Lerps to -30% of current
Phase 2 (Recover): 3s-8s → price Lerps back to ~original level

Chart shape:
  ──────╲
         ╲
          ╱──────
```

The V-shape is the signature visual. Players who recognize it can buy during the dip and sell on recovery.

### Event Magnitude Randomization

Each occurrence rolls a random magnitude within the event's min/max range:

```csharp
float magnitude = Random.Range(eventConfig.MinMagnitude, eventConfig.MaxMagnitude);
// EarningsBeat: Random.Range(0.15f, 0.30f) → +15% to +30%
float targetPrice = stock.CurrentPrice * (1f + magnitude);
```

This prevents events from being perfectly predictable even after players learn the patterns.

### Global vs Single-Stock Events

- **Single-stock:** EarningsBeat, EarningsMiss, FlashCrash — affect one stock, selected by scheduler
- **Global:** BullRun — affect all stocks. EventEffects must iterate all active stocks.

### Project Structure Notes

- Modifies: `Scripts/Runtime/Events/EventEffects.cs`
- Modifies: `Scripts/Runtime/Events/MarketEvent.cs`
- Modifies: `Scripts/Setup/Data/EventDefinitions.cs`
- No new files — builds on Story 1.3 infrastructure

### References

- [Source: game-architecture.md#Price Engine] — `if (activeEvent) price = Lerp(price, eventTarget, eventForce)`
- [Source: bull-run-gdd-mvp.md#3.4] — Event type table: Earnings Beat (+15-30%), Earnings Miss (-15-30%), Flash Crash (drop then recover), Bull Run (all stocks rise)
- [Source: bull-run-gdd-mvp.md#3.3] — "Event Spikes: Market events inject sudden price movements — sharp spikes or drops"
- [Source: bull-run-gdd-mvp.md#3.3] — Design note: "Events should create obvious visual signals that skilled players learn to recognize"

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
