# Story 5.3: Tier-Specific Events

Status: review

## Story

As a player,
I want events unique to each market tier,
so that each act feels mechanically distinct and introduces new trading dynamics as I progress.

## Acceptance Criteria

1. Pump & Dump (Penny only): rapid price rise 50-100% then crash below starting price — creates an inverted-V chart pattern
2. SEC Investigation (Penny/Low): gradual decline of 30-60% over extended duration (10s) — no recovery
3. Sector Rotation (Mid/Blue): one sector's stocks rise +15% while another sector's stocks fall -15% simultaneously
4. Merger Rumor (Mid/Blue): target stock surges 25-60% over 6s — holds at elevated level (no crash phase)
5. Each tier-specific event has headlines added to EventHeadlineData (from Story 5-2)
6. Multi-phase event support added to MarketEvent/EventEffects for events with multiple behavior phases (Pump & Dump)
7. EventScheduler (Story 5-1) correctly filters these events by tier — Penny rounds never get Sector Rotation, Blue Chip never gets Pump & Dump

## Tasks / Subtasks

- [x] Task 1: Add multi-phase event support to MarketEvent (AC: 6)
  - [x] Add `MarketEventPhase` class: `TargetPricePercent`, `PhaseDuration` (PhaseType enum not needed — phase behavior is implicit from targets)
  - [x] Add `List<MarketEventPhase> Phases` property to `MarketEvent` (null/empty for single-phase events)
  - [x] Add `int CurrentPhaseIndex` tracking (computed property based on ElapsedTime)
  - [x] Modify `GetCurrentForce()` to use phase-specific timing when phases are present
  - [x] Add `GetCurrentPhaseTarget()` — returns the active phase's target price percent
  - [x] Single-phase events (EarningsBeat/Miss etc.) continue to work unchanged (backward compatible)
  - [x] File: `Scripts/Runtime/Events/MarketEvent.cs`

- [x] Task 2: Update EventEffects to handle multi-phase events (AC: 6)
  - [x] In `ApplyEventEffect()`: when event has phases, use `GetCurrentPhaseTarget()` instead of single `PriceEffectPercent`
  - [x] Recalculate target price when phase transitions (new start price = current price at transition)
  - [x] Track phase index per (event, stockId) via `_eventPhaseIndex` dictionary; recapture on phase change
  - [x] File: `Scripts/Runtime/Events/EventEffects.cs`

- [x] Task 3: Implement Pump & Dump as multi-phase event (AC: 1)
  - [x] Phase 0 (Pump): price rises 50-100% over first 60% of duration (~4.8s of 8s total) — fast-attack force curve
  - [x] Phase 1 (Dump): price crashes to 80% of original price over remaining 40% (~3.2s) — inverted-V pattern
  - [x] Net effect: inverted-V shape on chart, skilled players sell at peak
  - [x] Create `MarketEvent` with two phases in EventScheduler's `FireEvent()` method
  - [x] Config already exists: `EventDefinitions.PumpAndDump` (Penny only, rarity 0.3, duration 8s)

- [x] Task 4: Implement SEC Investigation (AC: 2)
  - [x] Single-phase event using standard force curve with long duration (10s) — the existing slow tail-off creates a grinding feel
  - [x] Config already exists: `EventDefinitions.SECInvestigation` (Penny/Low, rarity 0.3, duration 10s, -30 to -60%)
  - [x] No recovery phase — price stays suppressed after event ends (mean reversion will slowly pull it back)
  - [x] No special implementation needed — standard single-phase path handles it

- [x] Task 5: Implement Sector Rotation as multi-stock event (AC: 3)
  - [x] Query active stocks for their `StockSector` via `StockInstance.Sector` property
  - [x] Pick one sector as "winner" and one as "loser" (randomly from sectors present in active stocks)
  - [x] Create simultaneous events: positive effect on winner sector stocks, negative on loser sector stocks
  - [x] Fallback: If stocks have no sector diversity (all same sector or all `None`), randomly split stocks via Fisher-Yates shuffle
  - [x] Config already exists: `EventDefinitions.SectorRotation` (Mid/Blue, rarity 0.4, -15% to +15%, duration 8s)
  - [x] File: `FireSectorRotation()` private method in EventScheduler

- [x] Task 6: Implement Merger Rumor (AC: 4)
  - [x] Single-phase, single-stock event — standard behavior, works with existing EventEffects Lerp
  - [x] Config already exists: `EventDefinitions.MergerRumor` (Mid/Blue, rarity 0.3, +25-60%, duration 6s)
  - [x] No special implementation needed — standard single-phase path handles it
  - [x] Verified via tests with EventScheduler tier filtering

- [x] Task 7: Add tier-specific event headlines to EventHeadlineData (AC: 5)
  - [x] PumpAndDump headlines: 3 templates added
  - [x] SECInvestigation headlines: 3 templates added
  - [x] SectorRotation headlines: 3 templates added
  - [x] MergerRumor headlines: 3 templates added
  - [x] File: `Scripts/Setup/Data/EventHeadlineData.cs`

- [x] Task 8: Add sector field to StockInstance (AC: 3)
  - [x] Add `StockSector Sector` property to `StockInstance`
  - [x] Add `SetSector()` method for setting from StockDefinition during initialization
  - [x] Set from `StockDefinition.Sector` during `PriceGenerator.InitializeRound()` stock creation
  - [x] File: `Scripts/Runtime/PriceEngine/StockInstance.cs`

- [x] Task 9: Write comprehensive tests (AC: 1-7)
  - [x] Test: Multi-phase MarketEvent transitions phases at correct times
  - [x] Test: PumpAndDump creates inverted-V price pattern (rises then crashes below start)
  - [x] Test: SECInvestigation produces gradual decline (single-phase negative event)
  - [x] Test: SectorRotation affects stocks by sector grouping (winner up, loser down)
  - [x] Test: SectorRotation falls back to random split when no sector diversity
  - [x] Test: MergerRumor produces sustained price increase (no crash phase)
  - [x] Test: Tier filtering — PumpAndDump only appears for Penny tier, SectorRotation only Mid/Blue
  - [x] Test: EventHeadlineData returns correct headlines for all 4 tier-specific event types (3+ templates each)
  - [x] Test: Single-phase events still work after multi-phase refactor (backward compatibility)
  - [x] File: `Tests/Runtime/Events/MarketEventTests.cs` (extended)
  - [x] File: `Tests/Runtime/Events/EventEffectsTests.cs` (extended)
  - [x] File: `Tests/Runtime/Events/EventSchedulerTests.cs` (extended)
  - [x] File: `Tests/Runtime/Events/EventHeadlineDataTests.cs` (extended)

## Dev Notes

### Architecture Compliance

- **MarketEvent remains a pure C# class** — multi-phase support via composition (list of phases), no inheritance hierarchy
- **EventEffects stays as the single price-effect processor** — phase tracking happens inside the existing `ApplyEventEffect` flow
- **Static data for configs** — all event params in `EventDefinitions.cs`, headlines in `EventHeadlineData.cs`
- **Sector data already exists** — `StockSector` enum and `StockDefinition.Sector` are already in `StockPoolData.cs`

### Multi-Phase Event Design

```csharp
public class MarketEventPhase
{
    public float TargetPricePercent;  // e.g., +0.80 for pump, -0.20 for dump
    public float PhaseDuration;       // seconds for this phase
}

// PumpAndDump example (8s total):
// Phase 0: +80% over 5s (pump)
// Phase 1: -120% from peak over 3s (dump — overshoots below start)

// Single-phase events (EarningsBeat etc.): Phases list is null, existing behavior unchanged
```

### Key Insight: Phase Transitions Reset Price Tracking

When a multi-phase event transitions from Phase 0 to Phase 1:
1. The **current price at transition** becomes the new start price for Phase 1
2. Phase 1's target is computed from this new start price
3. `_eventStartPrices` and `_eventTargetPrices` in EventEffects must be updated

```
PumpAndDump Timeline:
  $1.00 ──╱╲          Start: $1.00
         ╱  ╲         Phase 0 target: $1.80 (+80%)
        ╱    ╲        Phase 1 starts at: $1.80
       ╱      ╲       Phase 1 target: $0.80 (-56% from $1.80, landing below $1.00)
              ╲──
```

### Sector Rotation Implementation

```csharp
// Group active stocks by sector
var sectorGroups = activeStocks
    .Where(s => s.Sector != StockSector.None)
    .GroupBy(s => s.Sector)
    .ToList();

if (sectorGroups.Count >= 2)
{
    // Pick winner and loser sectors
    var shuffled = sectorGroups.OrderBy(_ => Random.value).ToList();
    var winnerSector = shuffled[0];
    var loserSector = shuffled[1];

    // Fire positive event on winner stocks, negative on loser stocks
    foreach (var stock in winnerSector)
        FireSectorEvent(stock, +rotationPercent);
    foreach (var stock in loserSector)
        FireSectorEvent(stock, -rotationPercent);
}
else
{
    // Fallback: random split
    var half = activeStocks.Count / 2;
    // First half up, second half down
}
```

### What Already Exists (DO NOT recreate)

| Component | File | Status |
|-----------|------|--------|
| `PumpAndDump` config | `EventDefinitions.cs` | Complete — +50-100%, 8s, Penny only, rarity 0.3 |
| `SECInvestigation` config | `EventDefinitions.cs` | Complete — -30-60%, 10s, Penny/Low, rarity 0.3 |
| `SectorRotation` config | `EventDefinitions.cs` | Complete — +/-15%, 8s, Mid/Blue, rarity 0.4 |
| `MergerRumor` config | `EventDefinitions.cs` | Complete — +25-60%, 6s, Mid/Blue, rarity 0.3 |
| `StockSector` enum | `StockPoolData.cs` | Complete — Tech, Energy, Health, Finance, Consumer, Industrial, Crypto |
| `StockDefinition.Sector` | `StockPoolData.cs` | Complete — every stock has a sector tag |
| `EventScheduler` tier filtering | `EventScheduler.cs` (Story 5-1) | Prerequisite |
| `EventHeadlineData` | `EventHeadlineData.cs` (Story 5-2) | Prerequisite — extend with new headlines |

### Previous Story Learnings

- EventEffects uses `Dictionary<(MarketEvent, int), float>` for start/target prices — must handle phase transitions carefully to avoid stale entries
- StockInstance currently lacks Sector field — needs to be added (from StockDefinition during init)
- Keep multi-phase support optional (null Phases list = single-phase) for backward compat

### Project Structure Notes

- Modified: `Assets/Scripts/Runtime/Events/MarketEvent.cs` (add multi-phase support)
- Modified: `Assets/Scripts/Runtime/Events/EventEffects.cs` (handle phase transitions)
- Modified: `Assets/Scripts/Runtime/PriceEngine/StockInstance.cs` (add Sector property)
- Modified: `Assets/Scripts/Runtime/PriceEngine/PriceGenerator.cs` (set Sector during stock creation)
- Modified: `Assets/Scripts/Setup/Data/EventHeadlineData.cs` (add tier-specific headlines, from Story 5-2)
- Modified: `Assets/Scripts/Runtime/Events/EventScheduler.cs` (sector-aware targeting for SectorRotation)
- New file: `Assets/Tests/Runtime/Events/MarketEventTests.cs`
- Modified: `Assets/Tests/Runtime/Events/EventEffectsTests.cs`

### References

- [Source: epics.md#5.3] — "Penny: Pump & Dump, Penny/Low: SEC Investigation, Mid/Blue: Sector Rotation, Mid/Blue: Merger Rumor"
- [Source: bull-run-gdd-mvp.md#3.4] — Event table with effects and tier availability
- [Source: bull-run-gdd-mvp.md#3.2] — "Penny: wild swings, pump & dump patterns. Mid-Value: sector correlation"
- [Source: StockPoolData.cs] — StockSector enum, all stocks have sector assignments
- [Source: EventDefinitions.cs] — All 4 tier-specific event configs already defined
- [Source: EventEffects.cs] — _eventStartPrices/_eventTargetPrices dictionaries for phase management

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Debug Log References

### Completion Notes List

- Task 1: Added `MarketEventPhase` class and multi-phase support to `MarketEvent`. Phases are optional (null = single-phase backward compat). `CurrentPhaseIndex` is a computed property from ElapsedTime. `GetCurrentForce()` delegates to per-phase force curves. `GetCurrentPhaseTarget()` returns active phase's target percent.
- Task 2: Updated `EventEffects.ApplyEventEffect()` to track phase transitions via `_eventPhaseIndex` dictionary. On phase change, recaptures start/target prices from current stock price. Single-phase path unchanged.
- Task 3: PumpAndDump creates 2-phase MarketEvent in `EventScheduler.FireEvent()`. Phase 0 pumps +50-100% over 60% duration, Phase 1 dumps to 80% of original over 40% duration. Creates inverted-V chart pattern.
- Task 4: SEC Investigation uses standard single-phase path — 10s duration with -30 to -60% effect creates a grinding decline via the existing fast-attack curve's long hold phase.
- Task 5: Sector Rotation implemented via `FireSectorRotation()` — groups stocks by sector, picks winner/loser sectors, fires positive events on winners and negative on losers. Falls back to random Fisher-Yates split when insufficient sector diversity.
- Task 6: Merger Rumor works out-of-the-box via standard single-phase path. Verified via tests.
- Task 7: Added 3 headline templates each for PumpAndDump, SECInvestigation, SectorRotation, MergerRumor — replacing placeholder single-template arrays.
- Task 8: Added `StockSector Sector` property and `SetSector()` method to `StockInstance`. Set from `StockDefinition.Sector` in `PriceGenerator.InitializeRound()`.
- Task 9: Comprehensive tests added across 4 test files covering multi-phase transitions, PumpAndDump behavior, sector rotation with sector diversity and fallback, tier filtering, headline templates, and backward compatibility.

### Change Log

- 2026-02-12: Implemented Story 5.3 — Tier-Specific Events (all 9 tasks complete)

### File List

- Modified: `Assets/Scripts/Runtime/Events/MarketEvent.cs`
- Modified: `Assets/Scripts/Runtime/Events/EventEffects.cs`
- Modified: `Assets/Scripts/Runtime/Events/EventScheduler.cs`
- Modified: `Assets/Scripts/Runtime/PriceEngine/StockInstance.cs`
- Modified: `Assets/Scripts/Runtime/PriceEngine/PriceGenerator.cs`
- Modified: `Assets/Scripts/Setup/Data/EventHeadlineData.cs`
- Modified: `Assets/Tests/Runtime/Events/MarketEventTests.cs`
- Modified: `Assets/Tests/Runtime/Events/EventEffectsTests.cs`
- Modified: `Assets/Tests/Runtime/Events/EventSchedulerTests.cs`
- Modified: `Assets/Tests/Runtime/Events/EventHeadlineDataTests.cs`
