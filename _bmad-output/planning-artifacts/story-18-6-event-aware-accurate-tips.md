# Story 18.6: Event-Aware Accurate Tips

## Story

As a player,
I want my purchased insider tips to be **correct** — not fuzzy approximations that events can invalidate,
so that paying for intel gives me an actual strategic edge I can trust.

## Problem Statement

All price-based tips (Floor, Ceiling, Forecast) are computed at shop time using static tier ranges (e.g., Penny tier MaxPrice = $8.00), with ±10% fuzz applied. But market events routinely push prices 25-100% beyond tier bounds. A tip that says "Ceiling at ~$7.54" is worthless when the first event spikes the price to $10.21.

Timing-based tips (Dip Marker, Peak Marker, Trend Reversal) use crude trend-direction heuristics and ignore events entirely. Event Timing applies ±4% fuzz that makes it wrong on purpose. Closing Direction is a coin flip at shop time.

**Design shift:** Tips are insider intel you paid for. They should be **right**, not fuzzy.

## Solution: Pre-Decide Events + Price Simulation

### Architecture Change

Currently, `EventScheduler` pre-schedules fire **times** in `InitializeRound()` but selects event **types** and rolls **price effects** at fire time in `Update()`. This means event impacts are unknown when tips are computed.

**Fix:** Move event type selection and price effect rolling into `InitializeRound()`. Pre-decide the entire event plan. Then pass it to `TipActivator`, which runs a lightweight price simulation to compute accurate tip values.

### Execution Order (unchanged, just more data at each step)

```
TradingState.Enter():
  1. Reset relic multipliers
  2. Publish RoundStartedEvent → relics set multipliers synchronously
  3. EventScheduler.InitializeRound() → pre-schedules times AND pre-decides types + effects
  4. TipActivator.ActivateTips() → receives full event plan, simulates price, computes accurate tips
  5. Publish TipOverlaysActivatedEvent
```

---

## Acceptance Criteria

### AC 1: Pre-Decided Event Plan

`EventScheduler.InitializeRound()` now pre-decides event types and price effects for every scheduled slot:

- New `PreDecidedEvent` struct:
  ```
  float FireTime
  MarketEventConfig Config        // event type, duration, etc.
  float PriceEffect               // rolled effect with relic multipliers applied
  bool IsPositive                  // from EventHeadlineData.IsPositiveEvent
  List<MarketEventPhase> Phases    // for multi-phase events (PumpAndDump, FlashCrash), null for single-phase
  ```

- For each scheduled slot, `InitializeRound()` calls the existing `SelectEventType()` logic and rolls `priceEffect` between `config.MinPriceEffect` and `config.MaxPriceEffect`, applying `ImpactMultiplier` and `PositiveImpactMultiplier` (same math as current `FireEvent()`).

- Multi-phase events (PumpAndDump, FlashCrash) pre-compute their phase data (same logic as current `FireEvent()` phase construction).

- Pre-decided events stored in a `PreDecidedEvent[]` array, accessible via `public PreDecidedEvent[] PreDecidedEvents`.

- `Update()` uses the pre-decided data at fire time instead of re-rolling. It constructs `MarketEvent` from `PreDecidedEvent` data.

- Behavior is **identical** to current system — same random distributions, same relic multiplier application. The only change is WHEN the rolls happen (init time vs fire time).

### AC 2: Price Simulation in TipActivator

New `SimulateRound()` method computes the price trajectory analytically (no frame-by-frame stepping):

- **Inputs:** starting price, trend rate, trend direction, round duration, pre-decided events, dynamic floor percent
- **Between events:** apply compound trend: `price(t) = price(t_prev) * (1 + trendRate)^(t - t_prev)`
- **At each event:**
  - Single-phase: `price = price * (1 + priceEffect)`, new base for trend continuation
  - PumpAndDump: track pump peak = `price * (1 + pumpEffect)`, post-event price = `price * 0.80`
  - FlashCrash: track crash bottom = `price * (1 + crashEffect)`, post-event price = `price * 0.95`
- **Between events, track min/max:** Bull trend → min at segment start, max at segment end. Bear → inverse. Neutral → both endpoints.
- **Dynamic floor:** if simulated price drops below `startingPrice * PriceFloorPercent`, clamp to floor (mirrors PriceGenerator logic)
- **After last event:** apply trend to round end for closing price

- Outputs a `RoundSimulation` struct:
  ```
  float MinPrice                  // global minimum across entire round
  float MaxPrice                  // global maximum across entire round
  float MinPriceNormalizedTime    // when min occurs (0-1 normalized)
  float MaxPriceNormalizedTime    // when max occurs (0-1 normalized)
  float ClosingPrice              // simulated price at round end
  float AveragePrice              // (MinPrice + MaxPrice) / 2 or weighted average of sampled points
  ```

### AC 3: Accurate Tip Computation (All 9 Types)

Each tip type uses simulation results instead of tier statistics or heuristics. **No fuzz applied anywhere.**

| Tip Type | Old Source | New Source |
|----------|-----------|-----------|
| **PriceCeiling** | `tierConfig.MaxPrice ±10%` | `simulation.MaxPrice` |
| **PriceFloor** | `tierConfig.MinPrice ±10%` | `simulation.MinPrice` |
| **PriceForecast** | `tierAverage ±10%` | `simulation.AveragePrice` with band = `(MaxPrice - MinPrice) * 0.15` |
| **DipMarker** | Trend-direction heuristic ±5% | Time zone centered on `simulation.MinPriceNormalizedTime` |
| **PeakMarker** | Trend-direction heuristic ±5% | Time zone centered on `simulation.MaxPriceNormalizedTime` |
| **ClosingDirection** | Random 60/40 at shop time | `sign(simulation.ClosingPrice - startingPrice)` |
| **EventTiming** | Actual fire times ±4% | Exact fire times from PreDecidedEvents — no fuzz |
| **EventCount** | Already uses actual count | Unchanged — `PreDecidedEvents.Length` |
| **TrendReversal** | Event-cluster heuristic ±5% | Detect where simulated price changes direction (rising→falling or falling→rising) by scanning simulation trajectory at event boundaries |

### AC 4: Shop-Time Display Text Changes

Price-based tips can no longer show specific values at shop time because accurate values don't exist until round start:

| Tip Type | Old Shop Text | New Shop Text |
|----------|--------------|---------------|
| PriceForecast | "Sweet spot around ~$6.50 — marked on chart" | "Price forecast — revealed on chart" |
| PriceFloor | "Floor at ~$3.20 — marked on chart" | "Price floor — revealed on chart" |
| PriceCeiling | "Ceiling at ~$9.80 — marked on chart" | "Price ceiling — revealed on chart" |
| ClosingDirection | "Round closes HIGHER" | "Closing direction — revealed on chart" |

- `InsiderTipGenerator.CalculateDisplayText()` returns generic text for these 4 types (no price/direction computation)
- `NumericValue` field = 0 for these types at shop time (value computed at round start)
- `EventCount` keeps shop-time estimate text ("Expect ~N disruptions") — the actual count is shown live during trading
- DipMarker, PeakMarker, EventTiming, TrendReversal are already generic text ("marked on chart")

### AC 5: Round-Start Display Text Update

When `TipActivator.ActivateTips()` computes accurate values, it also updates `RevealedTip.DisplayText` with the real value:

- PriceCeiling: `"Ceiling at $10.21 — marked on chart"` (no "~", exact value)
- PriceFloor: `"Floor at $4.15 — marked on chart"`
- PriceForecast: `"Sweet spot at $7.30 — marked on chart"`
- ClosingDirection: `"Round closes HIGHER"` or `"Round closes LOWER"` (based on simulation)

This requires `RevealedTip` list in `RunContext` to be mutable (List of structs → update by index). TipActivator returns the updated tip list alongside overlays.

### AC 6: Remove All Fuzz

- Remove `GameConfig.InsiderTipFuzzPercent` constant (or set to 0 and deprecate)
- Remove `InsiderTipGenerator.ApplyFuzz()` calls for tip value computation
- Remove ±4% timing fuzz in `ComputeEventTimingOverlay()`
- Remove ±5% position fuzz in `ComputeDipMarkerOverlay()`, `ComputePeakMarkerOverlay()`, `ComputeTrendReversalOverlay()`
- `ApplyFuzz()` static method itself can remain if other systems use it, but tips must not

### AC 7: TipActivationContext Updated

`TipActivationContext` struct changes:
- **Remove:** `int ScheduledEventCount`, `float[] ScheduledFireTimes`
- **Add:** `PreDecidedEvent[] PreDecidedEvents`
- Existing fields unchanged: `ActiveStock`, `RoundDuration`, `TierConfig`, `Random`

### AC 8: Tests

- **Simulation accuracy:** Given a starting price, trend rate, and known events, `SimulateRound()` produces expected min/max/closing prices (within float precision)
- **PriceCeiling uses simulation max:** Create a scenario with a +50% event; verify ceiling = simulated max, NOT tier max
- **PriceFloor uses simulation min:** Create a scenario with a -30% event; verify floor = simulated min, NOT tier min
- **DipMarker time matches simulation min time:** Bull trend + late negative event → dip zone centers near the event time, NOT at 15%
- **PeakMarker time matches simulation max time:** Bull trend + early positive event → peak zone reflects actual peak
- **ClosingDirection matches simulation:** Bear trend with late +80% bull event → simulation shows closing higher → ClosingDirection = +1 (would have been -1 under old system)
- **EventTiming has no fuzz:** Fire times in overlay match PreDecidedEvent fire times exactly (bit-for-bit)
- **TrendReversal detects event-driven reversal:** Bull trend + late crash event → reversal detected at crash time
- **Pre-decided events match runtime behavior:** Events fire at same times with same types and effects as pre-decided
- **Multi-phase simulation:** PumpAndDump event → simulation tracks pump peak as potential max, dump trough as potential min
- **No fuzz in any tip value:** Verify no random offset is applied to any tip numeric value or time position
- **Deterministic:** Same seed → identical simulation results and tip values

---

## Files to Create

- None (all modifications to existing files)

## Files to Modify

| File | Changes |
|------|---------|
| `Assets/Scripts/Runtime/Events/EventScheduler.cs` | Add `PreDecidedEvent` struct, pre-decide types/effects in `InitializeRound()`, use pre-decided data in `Update()` |
| `Assets/Scripts/Runtime/Shop/TipActivator.cs` | Add `SimulateRound()` method, `RoundSimulation` struct, rewrite all overlay computations to use simulation results, update RevealedTip display text |
| `Assets/Scripts/Runtime/Shop/StoreDataTypes.cs` | Update `TipActivationContext` fields (remove ScheduledFireTimes/EventCount, add PreDecidedEvents) |
| `Assets/Scripts/Runtime/Shop/InsiderTipGenerator.cs` | Change price/direction tip display text to generic (no values), remove `ApplyFuzz()` usage for tips |
| `Assets/Scripts/Runtime/Core/GameStates/TradingState.cs` | Update `TipActivationContext` construction to pass `PreDecidedEvents`, handle updated RevealedTip list |
| `Assets/Scripts/Setup/Data/GameConfig.cs` | Remove or deprecate `InsiderTipFuzzPercent` |
| `Assets/Scripts/Setup/Data/InsiderTipDefinitions.cs` | Update `DescriptionTemplate` for price/direction tips |
| `Assets/Tests/Runtime/Shop/TipActivatorTests.cs` | New/updated tests for simulation accuracy and accurate tip values |
| `Assets/Tests/Runtime/Events/EventSchedulerTests.cs` | Tests for pre-decided event plan |
| `Assets/Tests/Runtime/Shop/InsiderTipGeneratorTests.cs` | Update for new generic display text |

## Dependencies

- **Depends on:** Stories 18.1-18.5 (all complete)
- **Blocks:** Nothing

## Technical Notes

### Why Analytical Simulation (Not Frame-by-Frame)

The price engine has 4 layers: trend, noise, events, mean reversion. Noise is random and zero-mean — it doesn't systematically affect min/max/closing. Mean reversion pulls toward the trend line, which the simulation already tracks via compound trend. So **trend + events** captures the signal; noise is the unpredictable jitter.

The simulation computes price analytically at event boundaries, which is exact for the deterministic components. The actual runtime price will jitter around the simulated trajectory due to noise, but the ceiling/floor/peak/dip predictions will be correct within noise amplitude (~3-8% depending on tier).

### Relic Interactions

- **Event Storm** (increases EventCountMultiplier): Already applied before InitializeRound — pre-decided event count includes this.
- **Bull Believer** (increases PositiveImpactMultiplier): Already applied before InitializeRound — pre-decided positive event effects include this.
- **Catalyst Trader / Loss Liquidator** (ForceFireRandomEvent): These fire events DURING trading in response to player actions. Pre-decided events cannot predict player behavior, so forced events are outside the simulation. This is correct — tips predict the market, not the player's own actions.
- **Time Buyer** (extends round timer): If round duration extends, pre-decided events are within the original duration. Extended time has no new events. Tips remain accurate for the pre-scheduled event window.

### Shop Card UX

The shop card flip animation still works — the card reveals the tip TYPE and a description, just without specific values for price/direction tips. The mystery of "what value will the ceiling be?" is revealed on the chart when trading starts. This actually enhances the insider-intel fantasy: you bought the intel type, now you see the real numbers when the round starts.

### Edge Cases

- **Zero events scheduled:** Simulation is pure trend. Floor/Ceiling = trend endpoints. DipMarker/PeakMarker use trend-based timing.
- **All negative events:** Ceiling may equal starting price (max is at t=0). This is correct.
- **PumpAndDump:** Peak is during pump phase, not at event end. Simulation must track intra-event peaks.
- **FlashCrash:** Trough is during crash phase, not at event end. Simulation must track intra-event troughs.
- **Neutral trend with no events:** Price is approximately flat. Floor ≈ Ceiling ≈ Forecast ≈ starting price. This is correct but boring — the tip accurately tells you "nothing much happens."

---

## Tasks / Subtasks

- [x] **Task 1: PreDecidedEvent struct + EventScheduler refactor (AC 1)**
  - [x] Create `PreDecidedEvent` struct with FireTime, Config, PriceEffect, IsPositive, Phases
  - [x] Refactor `InitializeRound()` to pre-decide event types, price effects, and phases
  - [x] Handle SectorRotation random direction in pre-decide
  - [x] Add `PreDecidedEvents` public property
  - [x] Refactor `Update()` to use pre-decided data via `FirePreDecidedEvent()`
  - [x] Keep `ForceFireRandomEvent()` working with existing `FireEvent()` path

- [x] **Task 2: TipActivationContext update (AC 7)**
  - [x] Remove `ScheduledEventCount` and `ScheduledFireTimes` fields
  - [x] Add `PreDecidedEvent[] PreDecidedEvents` field
  - [x] Update TradingState to construct context with PreDecidedEvents
  - [x] Remove `BuildFireTimesArray()` helper from TradingState

- [x] **Task 3: Price Simulation (AC 2)**
  - [x] Create `RoundSimulation` struct with MinPrice, MaxPrice, times, ClosingPrice, AveragePrice
  - [x] Implement `SimulateRound()` with analytical compound trend
  - [x] Handle single-phase events (apply priceEffect)
  - [x] Handle PumpAndDump (track pump peak as potential max, post-dump as potential min)
  - [x] Handle FlashCrash (track crash bottom as potential min)
  - [x] Apply dynamic floor clamping per `GameConfig.PriceFloorPercent`
  - [x] Track segment min/max based on trend direction

- [x] **Task 4: Accurate Tip Computation (AC 3) + Remove Fuzz (AC 6)**
  - [x] Rewrite PriceFloor overlay to use `simulation.MinPrice`
  - [x] Rewrite PriceCeiling overlay to use `simulation.MaxPrice`
  - [x] Rewrite PriceForecast overlay to use `simulation.AveragePrice` with 15% band
  - [x] Rewrite DipMarker to use `simulation.MinPriceNormalizedTime`
  - [x] Rewrite PeakMarker to use `simulation.MaxPriceNormalizedTime`
  - [x] Rewrite ClosingDirection to use `sign(simulation.ClosingPrice - startingPrice)`
  - [x] Rewrite EventTiming to use exact fire times from PreDecidedEvents (no fuzz)
  - [x] Rewrite TrendReversal to detect price direction changes at event boundaries
  - [x] EventCount uses `PreDecidedEvents.Length`
  - [x] Remove all `ApplyFuzz()` calls from tip value computation
  - [x] Remove ±4% timing fuzz from EventTiming
  - [x] Remove ±5% position fuzz from DipMarker, PeakMarker, TrendReversal

- [x] **Task 5: Shop-Time Display Text Changes (AC 4)**
  - [x] Update InsiderTipDefinitions templates for PriceForecast, PriceFloor, PriceCeiling
  - [x] Update InsiderTipDefinitions template for ClosingDirection
  - [x] Update InsiderTipGenerator.CalculateDisplayText() — return generic text, NumericValue=0

- [x] **Task 6: Round-Start Display Text Update (AC 5)**
  - [x] Implement `UpdateDisplayText()` in TipActivator
  - [x] Update PriceCeiling, PriceFloor, PriceForecast, ClosingDirection text with simulation values
  - [x] Update NumericValue for price tips
  - [x] ActivateTips modifies purchasedTips in-place

- [x] **Task 7: Remove Fuzz Constants (AC 6)**
  - [x] Remove `GameConfig.InsiderTipFuzzPercent`
  - [x] Remove test `InsiderTipFuzzPercent_IsTenPercent`
  - [x] Keep `ApplyFuzz()` method (may be used by other systems)

- [x] **Task 8: Tests (AC 8)**
  - [x] SimulateRound accuracy: pure trend (bull, bear, neutral)
  - [x] SimulateRound: single positive event exceeds tier bounds
  - [x] SimulateRound: single negative event pushes below start
  - [x] SimulateRound: PumpAndDump tracks pump peak and dump trough
  - [x] SimulateRound: FlashCrash tracks crash bottom
  - [x] SimulateRound: dynamic floor clamping
  - [x] SimulateRound: deterministic with same inputs
  - [x] PriceCeiling uses simulation max, not tier max
  - [x] PriceFloor uses simulation min, not tier min
  - [x] DipMarker time matches simulation min time
  - [x] PeakMarker time reflects actual peak
  - [x] ClosingDirection matches simulation
  - [x] EventTiming exact fire times (no fuzz)
  - [x] TrendReversal detects event-driven reversal
  - [x] PreDecidedEvents has valid data after InitializeRound
  - [x] No fuzz: all values identical regardless of Random seed
  - [x] Display text update tests (PriceCeiling, PriceFloor, ClosingDirection)
  - [x] Updated InsiderTipGeneratorTests for generic shop text
  - [x] Updated StoreVisualPolishTests for new template format

---

## Dev Agent Record

### Implementation Plan
- Pre-decide events at `InitializeRound()` time by moving `SelectEventType()` and price effect rolling from `Update()` fire time to init time
- Implement analytical price simulation (`SimulateRound`) that computes price at event boundaries using compound trend + event effects
- Rewrite all 9 tip overlay computations to use simulation results instead of heuristics/fuzz
- Update shop-time display text to be generic (price revealed at round start)
- Update round-start display text with accurate simulation values via `UpdateDisplayText()`

### Completion Notes
- All 8 ACs implemented and verified
- 30+ tests covering simulation accuracy, accurate tip computation, fuzz removal, display text updates
- EventScheduler behavior is identical to before — same random distributions, same relic multiplier application — only timing of rolls changed
- `ForceFireRandomEvent()` still works for relics (uses existing `FireEvent()` path)
- `ApplyFuzz()` method retained but no longer used by tips

### Code Review Fixes (2026-02-21)
- **H1:** Strengthened PeakMarker test assertion (>= 0.10 → >= 0.80)
- **H2:** Added missing PriceForecast display text update test
- **H3:** Added 5 pre-decided event tests to EventSchedulerTests.cs (validity, multi-phase phases, IsPositive, ImpactMultiplier)
- **M1:** Removed dead `FormatPrice()` method from InsiderTipGenerator
- **M2:** Replaced crude `(MinPrice + MaxPrice) / 2` AveragePrice with time-weighted trapezoidal average
- **M3:** Extracted `BuildPumpAndDumpPhases()` and `BuildFlashCrashPhases()` helpers in EventScheduler to deduplicate phase construction

---

## File List

| File | Status |
|------|--------|
| `Assets/Scripts/Runtime/Events/EventScheduler.cs` | MODIFIED — PreDecidedEvent struct, pre-decide in InitializeRound, FirePreDecidedEvent, PreDecidedEvents property |
| `Assets/Scripts/Runtime/Shop/TipActivator.cs` | MODIFIED — RoundSimulation struct, SimulateRound(), rewritten overlay computations, UpdateDisplayText() |
| `Assets/Scripts/Runtime/Shop/InsiderTipGenerator.cs` | MODIFIED — Generic shop text for price/direction tips, removed fuzz usage |
| `Assets/Scripts/Runtime/Core/GameStates/TradingState.cs` | MODIFIED — Pass PreDecidedEvents in context, removed BuildFireTimesArray |
| `Assets/Scripts/Setup/Data/GameConfig.cs` | MODIFIED — Removed InsiderTipFuzzPercent |
| `Assets/Scripts/Setup/Data/InsiderTipDefinitions.cs` | MODIFIED — Generic templates for price/direction tips |
| `Assets/Tests/Runtime/Shop/TipActivatorTests.cs` | MODIFIED — Complete rewrite with simulation-based tests |
| `Assets/Tests/Runtime/Shop/InsiderTipGeneratorTests.cs` | MODIFIED — Updated for generic shop text |
| `Assets/Tests/Runtime/Shop/InsiderTipPurchaseTests.cs` | MODIFIED — Removed InsiderTipFuzzPercent test |
| `Assets/Tests/Runtime/Shop/StoreVisualPolishTests.cs` | MODIFIED — Updated template assertion |
| `Assets/Tests/Runtime/Events/EventSchedulerTests.cs` | MODIFIED — Added pre-decided event tests (review fix) |
| `Assets/_Generated/Scenes/MainScene.unity` | MODIFIED — Auto-generated scene rebuild |

---

## Change Log

- 2026-02-21: Code review fixes — strengthened tests, improved AveragePrice, deduplicated phase logic, removed dead code
- 2026-02-21: Story 18.6 implementation complete — pre-decided events, price simulation, accurate tips, fuzz removal

---

## Status

done
