# Story 5.4: Global Market Events

Status: ready-for-dev

## Story

As a player,
I want rare market-wide events like crashes and short squeezes,
so that dramatic moments shake up the entire round and force rapid adaptation.

## Acceptance Criteria

1. Market Crash: all stocks drop sharply (20-40%) simultaneously — rare, high-impact
2. Short Squeeze: a shorted stock spikes violently upward (30-60%), punishing open shorts
3. Both events are available across all tiers but weighted as rare occurrences
4. Market Crash affects all active stocks; Short Squeeze targets a single stock
5. Events create high-drama moments that can make or break a round
6. Short Squeeze only triggers on stocks that the player is currently shorting (or random if no shorts open)

## Tasks / Subtasks

- [ ] Task 1: Implement Market Crash effect (AC: 1, 4)
  - [ ] Target: ALL active stocks (global event)
  - [ ] Effect: all stock prices Lerp downward 20-40% over 5-8 seconds
  - [ ] No recovery within event duration — devastating if long, rewarding if short
  - [ ] Rare: EventScheduler should give this very low weight
  - [ ] File: `Scripts/Runtime/Events/EventEffects.cs` (extend)
- [ ] Task 2: Implement Short Squeeze effect (AC: 2, 6)
  - [ ] Target selection: prefer a stock the player is currently shorting (check Portfolio)
  - [ ] If player has no shorts, select random stock
  - [ ] Effect: price spikes 30-60% rapidly over 3-5 seconds
  - [ ] Specifically punishes open short positions — the player's short P&L drops fast
  - [ ] Creates urgency to cover shorts immediately
  - [ ] File: `Scripts/Runtime/Events/EventEffects.cs` (extend)
- [ ] Task 3: Add rarity weighting to EventScheduler (AC: 3)
  - [ ] EventDefinitions already has rarity per event type
  - [ ] EventScheduler.SelectEvent() uses weighted random selection based on rarity
  - [ ] Rare events (Market Crash, Bull Run, Short Squeeze): ~5-10% chance per event slot
  - [ ] Common events (Earnings Beat/Miss): ~30-40% chance per event slot
  - [ ] Ensure at most 1 rare event per round
  - [ ] File: `Scripts/Runtime/Events/EventScheduler.cs` (extend)
- [ ] Task 4: Populate event definitions (AC: 3, 5)
  - [ ] MarketCrash: tiers=[All], rarity=Rare, magnitude=20-40%, duration=5-8s
  - [ ] ShortSqueeze: tiers=[All], rarity=Rare, magnitude=30-60%, duration=3-5s
  - [ ] Verify all 10 event types now have complete definitions
  - [ ] File: `Scripts/Setup/Data/EventDefinitions.cs` (extend)
- [ ] Task 5: Add portfolio-aware targeting for Short Squeeze (AC: 6)
  - [ ] EventScheduler or EventEffects needs read access to Portfolio (via RunContext)
  - [ ] When selecting Short Squeeze target: check `RunContext.Portfolio` for open short positions
  - [ ] If shorts exist: target the largest short position's stock
  - [ ] If no shorts: target random stock (squeeze still happens, just less impactful)
  - [ ] File: `Scripts/Runtime/Events/EventScheduler.cs` (extend)

## Dev Notes

### Architecture Compliance

- **RunContext access:** EventScheduler can read RunContext to check portfolio state for smart targeting
- **EventBus:** Same pattern — publish `MarketEventFiredEvent` on fire
- **One-way read:** EventScheduler reads from Portfolio but never modifies it. Price changes from the event will naturally affect portfolio P&L through the existing pipeline.
- **Rarity in data:** Rarity weights are config values in EventDefinitions, not hardcoded in scheduler logic

### Market Crash — The Game Changer

Market Crash is the "oh shit" moment. All stocks tank simultaneously. It's devastating for long positions but a massive windfall for shorts. This teaches players that shorting is a defensive tool, not just speculative.

```
All stocks before crash: ───────
All stocks during crash: ───╲
                               ╲───
```

### Short Squeeze — The Trap

Short Squeeze is the counter-lesson: shorting is risky. If a player has a large short position, the squeeze specifically targets that stock, punishing overexposure. This creates a "don't put all your eggs in one basket" lesson.

The portfolio-aware targeting is a deliberate design choice — it makes the Short Squeeze feel personal and creates memorable moments. It's the game "fighting back" against dominant strategies.

### Rarity Weight System

Simple weighted selection:

```csharp
// Example weights
EarningsBeat: 30
EarningsMiss: 30
FlashCrash: 15
PumpAndDump: 15  (if tier allows)
ShortSqueeze: 5
MarketCrash: 3
BullRun: 5
```

Use standard weighted random: sum weights of tier-valid events, roll random, pick event.

### Complete Event Roster After This Story

| Event | Tier | Rarity | Story |
|-------|------|--------|-------|
| Earnings Beat | All | Common | 5.2 |
| Earnings Miss | All | Common | 5.2 |
| Flash Crash | Low, Mid | Moderate | 5.2 |
| Bull Run | All | Rare | 5.2 |
| Pump & Dump | Penny | Moderate | 5.3 |
| SEC Investigation | Penny, Low | Moderate | 5.3 |
| Sector Rotation | Mid, Blue | Moderate | 5.3 |
| Merger Rumor | Mid, Blue | Moderate | 5.3 |
| Market Crash | All | Rare | 5.4 |
| Short Squeeze | All | Rare | 5.4 |

### Project Structure Notes

- Modifies: `Scripts/Runtime/Events/EventEffects.cs`
- Modifies: `Scripts/Runtime/Events/EventScheduler.cs`
- Modifies: `Scripts/Setup/Data/EventDefinitions.cs`
- No new files needed

### References

- [Source: bull-run-gdd-mvp.md#3.4] — "Market Crash: All stocks drop sharply. Screen shake + alarm. All tiers (rare)"
- [Source: bull-run-gdd-mvp.md#3.4] — "Short Squeeze: Shorted stock spikes violently. Warning on short positions. All tiers"
- [Source: bull-run-gdd-mvp.md#3.4] — Full event type table
- [Source: game-architecture.md#Event System] — EventBus pattern for event firing

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
