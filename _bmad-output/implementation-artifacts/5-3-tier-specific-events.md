# Story 5.3: Tier-Specific Events

Status: ready-for-dev

## Story

As a player,
I want events unique to each market tier,
so that each act feels mechanically distinct and introduces new trading dynamics.

## Acceptance Criteria

1. Pump & Dump (Penny only): rapid price rise then crash — rewards quick sellers, punishes holders
2. SEC Investigation (Penny/Low): gradual decline of 20-40% over extended duration
3. Sector Rotation (Mid/Blue): one sector rises while another falls simultaneously
4. Merger Rumor (Mid/Blue): target stock surges 20-40% on rumor
5. Each tier-specific event has distinct behavior that teaches players new strategies per act
6. EventScheduler only selects tier-appropriate events for the current act

## Tasks / Subtasks

- [ ] Task 1: Implement Pump & Dump effect (AC: 1)
  - [ ] Target: single penny stock
  - [ ] Three-phase event: Pump (rapid rise 30-60%, 4s) → Peak (brief plateau, 1s) → Dump (crash back below starting price, 3s)
  - [ ] Creates an inverted-V shape: skilled players sell at the peak
  - [ ] Duration: ~8 seconds total
  - [ ] File: `Scripts/Runtime/Events/EventEffects.cs` (extend)
- [ ] Task 2: Implement SEC Investigation effect (AC: 2)
  - [ ] Target: single penny or low-value stock
  - [ ] Slow, grinding decline: price drops 20-40% over 10-15 seconds
  - [ ] No recovery — price stays suppressed (unlike Flash Crash)
  - [ ] Rewards players who short early when the investigation icon appears
  - [ ] File: `Scripts/Runtime/Events/EventEffects.cs` (extend)
- [ ] Task 3: Implement Sector Rotation effect (AC: 3)
  - [ ] Target: stocks grouped by sector tag (from Story 1.5 StockPoolData)
  - [ ] One sector boosted (+10-20%), another sector dragged (-10-20%)
  - [ ] Duration: 8-12 seconds
  - [ ] Requires sector awareness — affects multiple stocks simultaneously
  - [ ] If no sector tags exist on stocks, fall back to random split (half up, half down)
  - [ ] File: `Scripts/Runtime/Events/EventEffects.cs` (extend)
- [ ] Task 4: Implement Merger Rumor effect (AC: 4)
  - [ ] Target: single mid or blue chip stock
  - [ ] Surge: rapid price increase 20-40% over 5-8 seconds
  - [ ] No crash phase (unlike Pump & Dump) — price holds at elevated level
  - [ ] Creates a buy-and-hold opportunity if caught early
  - [ ] File: `Scripts/Runtime/Events/EventEffects.cs` (extend)
- [ ] Task 5: Populate tier-specific EventDefinitions (AC: 5, 6)
  - [ ] PumpAndDump: tiers=[Penny], rarity=moderate
  - [ ] SECInvestigation: tiers=[Penny, LowValue], rarity=moderate
  - [ ] SectorRotation: tiers=[MidValue, BlueChip], rarity=moderate
  - [ ] MergerRumor: tiers=[MidValue, BlueChip], rarity=moderate
  - [ ] File: `Scripts/Setup/Data/EventDefinitions.cs` (extend)
- [ ] Task 6: Validate tier filtering in EventScheduler (AC: 6)
  - [ ] Ensure EventScheduler.ScheduleRound() only selects events whose tier list includes the current tier
  - [ ] Verify: Penny rounds never get Sector Rotation, Blue Chip rounds never get Pump & Dump
  - [ ] File: `Scripts/Runtime/Events/EventScheduler.cs` (verify, extend if needed)

## Dev Notes

### Architecture Compliance

- **Extends EventEffects** — all event implementations go in EventEffects.cs (or split to per-event classes if the file gets too large)
- **Multi-phase events:** Pump & Dump uses the same phase system as Flash Crash (Story 5.2)
- **Sector tags:** Sector Rotation depends on sector tags from StockPoolData (Story 1.5). If those aren't present, degrade gracefully.
- **Data in EventDefinitions** — tier availability is a config concern, not logic

### Pump & Dump vs Flash Crash Comparison

Both have multi-phase behavior, but opposite patterns:

```
Flash Crash:    ──╲    ╱──   (V-shape: drop then recover)
Pump & Dump:    ──╱╲──       (inverted-V: rise then crash)
```

Flash Crash rewards buying the dip. Pump & Dump rewards selling the peak. Teaching players to distinguish them is a core skill progression.

### SEC Investigation — The Slow Burn

Unlike other events (5-8 seconds), SEC Investigation is a long event (10-15 seconds) with a slow grind. This teaches Act 2 players about shorting as a strategy: the price keeps declining, giving plenty of time to open and profit from a short position.

### Sector Rotation — Multi-Stock Events

This is the first event type that affects multiple stocks based on a grouping criterion (sector). Implementation approach:
1. Read sector tags from active StockInstance objects
2. Pick a "winning" sector and a "losing" sector
3. Apply positive effect to winning sector stocks, negative to losing
4. If stocks don't have sector tags, randomly split: first half up, second half down

### Project Structure Notes

- Modifies: `Scripts/Runtime/Events/EventEffects.cs`
- Modifies: `Scripts/Setup/Data/EventDefinitions.cs`
- Modifies: `Scripts/Runtime/Events/EventScheduler.cs` (verify tier filtering)
- Depends on: StockPoolData sector tags (Story 1.5)
- Depends on: Multi-phase MarketEvent support (Story 5.2)

### References

- [Source: bull-run-gdd-mvp.md#3.4] — Event table: Pump & Dump (Penny), SEC Investigation (Penny/Low), Sector Rotation (Mid/Blue), Merger Rumor (Mid/Blue)
- [Source: bull-run-gdd-mvp.md#3.2] — Tier behavior: "Penny: wild swings, pump & dump patterns", "Mid: sector correlation"
- [Source: bull-run-gdd-mvp.md#2.1] — Act-tier mapping: Act 1=Penny, Act 2=Low, Act 3=Mid, Act 4=Blue

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
