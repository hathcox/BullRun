# Epic 18: Insider Tips Overhaul — Visual Chart Overlays, New Tip Types & Live HUD

**Description:** Complete overhaul of the insider tips system. Remove 4 underperforming tip types (Opening Price, Volatility Warning, Trend Direction, Event Forecast) that provide information players can't act on. Add 5 new high-value tip types (Dip Marker, Peak Marker, Closing Direction, Event Timing, Trend Reversal) that directly answer "when do I buy?", "when do I sell?", and "long or short?" Transform tips from forgettable text strings into visual chart overlays — horizontal price lines, shaded time zones, vertical event markers, and directional arrows drawn directly onto the trading chart. Make Event Count a live countdown during trading. Tips become the strategic edge they were always meant to be.

**Phase:** Post-Epic 17, gameplay depth expansion
**Depends On:** Epic 13 (Store Rework — complete), Epic 17 (Relic System — complete, includes Free Intel relic)
**Status:** Ready for dev

**Design Philosophy (inspired by Space Warlord Organ Trading Simulator):**
- Information must be **visual and persistent** during trading — not a text string you read once and forget
- Information must be **directly actionable** — it should change what the player DOES, not just what they know
- Tip combos should feel **strategic** — buying 2 tips that complement each other is better than 2 random ones
- Tips should feel like having an **insider edge**, not reading a textbook

**Tip Roster After Overhaul (9 types):**

| # | Type | Question It Answers | Visual | Status |
|---|------|-------------------|--------|--------|
| 1 | Price Floor | "How low can it go?" | Chart: horizontal dashed line | Keep (add overlay) |
| 2 | Price Ceiling | "What's the top?" | Chart: horizontal dashed line | Keep (add overlay) |
| 3 | Price Forecast | "What's fair value?" | Chart: shaded horizontal band | Keep (add overlay) |
| 4 | Event Count | "How many disruptions?" | HUD: live countdown ticker | Keep (add countdown) |
| 5 | Dip Marker | "When should I buy?" | Chart: shaded green time zone | **NEW** |
| 6 | Peak Marker | "When should I sell?" | Chart: shaded amber time zone | **NEW** |
| 7 | Closing Direction | "Should I go long or short?" | Chart: arrow on right edge | **NEW** |
| 8 | Event Timing | "When do events hit?" | Chart: vertical dashed lines | **NEW** |
| 9 | Trend Reversal | "When does the trend flip?" | Chart: vertical marker + icon | **NEW** |

**Removed:** Opening Price, Volatility Warning, Trend Direction, Event Forecast

**Key Architectural Change — Round-Start Tip Activation:**

Current system generates all tip data at shop time using tier statistics. New tips (Dip Marker, Peak Marker, Closing Direction, Event Timing, Trend Reversal) require data that only exists at round start (actual trend direction, event schedule, starting price). Solution: two-phase tip lifecycle.

```
Phase 1 — Shop Time (existing flow):
  InsiderTipGenerator creates offerings with type + cost + description
  Player purchases tips → stored in RunContext.RevealedTips

Phase 2 — Round Start (NEW):
  TipActivator reads purchased tip types
  Accesses actual round data (PriceGenerator state, EventScheduler schedule)
  Computes overlay geometry (price levels, time positions, directions)
  Produces ChartTipOverlays consumed by the chart renderer
```

This cleanly separates "what intel did you buy?" from "what does that intel tell you about THIS round?"

---

## Story 18.1: Tip Data Model & Type Overhaul

As a developer,
I want to restructure the insider tip type system — removing 4 useless types, adding 5 new actionable types, and extending the data model to carry chart overlay data,
so that the tip system has the foundation for visual chart overlays and round-start activation.

**Acceptance Criteria:**

1. `InsiderTipType` enum updated:
   - **Removed:** `OpeningPrice`, `VolatilityWarning`, `TrendDirection`, `EventForecast`
   - **Added:** `DipMarker`, `PeakMarker`, `ClosingDirection`, `EventTiming`, `TrendReversal`
   - Remaining: `PriceForecast`, `PriceFloor`, `PriceCeiling`, `EventCount` (unchanged)
   - Total: 9 types

2. `InsiderTipDefinitions` updated with definitions for all 9 types:
   - Each definition has `Type`, `DescriptionTemplate`, `Cost`
   - Old definitions for removed types deleted
   - New definitions added for 5 new types

3. `GameConfig` tip cost constants updated:
   - Removed: `TipCostOpeningPrice`, `TipCostVolatilityWarning`, `TipCostTrendDirection`, `TipCostEventForecast`
   - Added: `TipCostDipMarker`, `TipCostPeakMarker`, `TipCostClosingDirection`, `TipCostEventTiming`, `TipCostTrendReversal`
   - Cost tiers reflect actionability:
     - Low (10-15 Rep): EventCount, PriceForecast, ClosingDirection
     - Medium (20-25 Rep): PriceFloor, PriceCeiling, TrendReversal
     - High (30-35 Rep): DipMarker, PeakMarker, EventTiming

4. `RevealedTip` struct in `StoreDataTypes.cs` extended:
   - Keeps existing `Type` and `RevealedText` fields (RevealedText renamed to `DisplayText`)
   - Adds `bool IsActivated` flag (false at purchase, true after round-start activation)

5. New `TipOverlayData` struct created in `Scripts/Runtime/Shop/StoreDataTypes.cs`:
   - Contains all possible overlay geometry fields:
     - `float PriceLevel` — for horizontal line overlays (Floor, Ceiling)
     - `float BandCenter` + `float BandHalfWidth` — for horizontal band overlay (Forecast)
     - `float TimeZoneCenter` + `float TimeZoneHalfWidth` — for time zone overlays (Dip, Peak), normalized 0-1
     - `float[] TimeMarkers` — for vertical marker overlays (Event Timing), normalized 0-1
     - `float ReversalTime` — for reversal marker (Trend Reversal), normalized 0-1, -1 if none
     - `int DirectionSign` — for direction arrow (+1 higher, -1 lower)
     - `int EventCountdown` — for live counter (Event Count)
   - `TipOverlayData` is populated at round start, NOT at shop time

6. `RunContext` updated:
   - New `List<TipOverlayData> ActiveTipOverlays` property (populated at round start, cleared at round end)
   - Existing `RevealedTips` list continues to serve as purchase record

7. All references to removed tip types cleaned up across codebase:
   - `InsiderTipGenerator.CalculateRevealedText()` — remove branches for deleted types
   - `ShopUI` face-down hints and formatted names — remove entries for deleted types
   - Any switch statements on `InsiderTipType` — remove dead cases, add new cases

8. Tests:
   - All 9 tip types exist in enum with unique values
   - All 9 definitions exist in `InsiderTipDefinitions.All` with non-zero costs
   - Costs match GameConfig constants
   - No duplicate types in definitions array
   - `TipOverlayData` default-constructs to safe values
   - Removed types no longer exist in enum or definitions

**Files to create:**
- None (all modifications to existing files)

**Files to modify:**
- `Assets/Scripts/Setup/Data/InsiderTipDefinitions.cs` — new enum values, new definitions
- `Assets/Scripts/Setup/Data/GameConfig.cs` — new cost constants, remove old ones
- `Assets/Scripts/Runtime/Shop/StoreDataTypes.cs` — extend RevealedTip, add TipOverlayData
- `Assets/Scripts/Runtime/Core/RunContext.cs` — add ActiveTipOverlays
- `Assets/Scripts/Runtime/Shop/InsiderTipGenerator.cs` — remove old type generation branches
- `Assets/Scripts/Runtime/UI/ShopUI.cs` — remove old face-down hints/names, add new ones
- `Assets/Tests/Runtime/Shop/InsiderTipGeneratorTests.cs` — update for new types
- `Assets/Tests/Runtime/Shop/InsiderTipPurchaseTests.cs` — update for new types

---

## Story 18.2: Tip Generation & Round-Start Activation

As a developer,
I want tip generation logic for all 9 types and a round-start activation system that computes chart overlay data using actual round parameters,
so that tips produce accurate, round-specific information that can drive visual overlays.

**Acceptance Criteria:**

1. `InsiderTipGenerator.CalculateRevealedText()` updated for new types:
   - **DipMarker:** Display text = "Dip zone marked on chart" (no price value — it's timing info)
   - **PeakMarker:** Display text = "Peak zone marked on chart"
   - **ClosingDirection:** Display text = "Round closes HIGHER" or "Round closes LOWER"
     - Computed from trend probability: bull (60%) → HIGHER, bear (25%) → LOWER, neutral → coin flip
   - **EventTiming:** Display text = "Event timing marked on chart"
   - **TrendReversal:** Display text = "Trend reversal point marked on chart"
   - Existing types (PriceForecast, PriceFloor, PriceCeiling, EventCount) unchanged

2. New `TipActivator` static class in `Scripts/Runtime/Shop/TipActivator.cs`:
   - Method: `static List<TipOverlayData> ActivateTips(List<RevealedTip> purchasedTips, TipActivationContext ctx)`
   - Called at round start after PriceGenerator and EventScheduler have initialized
   - Takes a `TipActivationContext` struct containing:
     - Trend direction and strength (from PriceGenerator/StockInstance)
     - Starting price (from StockInstance)
     - Event fire times array (from EventScheduler)
     - Event count (from EventScheduler)
     - Round duration
     - Tier config (min/max price, noise amplitude, etc.)
     - System.Random for fuzz

3. Overlay computation for each tip type:
   - **PriceFloor:** `PriceLevel` = the fuzzed floor value from RevealedTip (already computed at shop time)
   - **PriceCeiling:** `PriceLevel` = the fuzzed ceiling value from RevealedTip
   - **PriceForecast:** `BandCenter` = fuzzed average, `BandHalfWidth` = 10-15% of price range
   - **EventCount:** `EventCountdown` = actual scheduled event count from EventScheduler (may differ from shop estimate due to relics)
   - **DipMarker:** `TimeZoneCenter` + `TimeZoneHalfWidth` computed from trend direction:
     - Bull trend → dip zone in first 30% of round (price is lowest before compound growth)
     - Bear trend → dip zone in last 30% of round (price keeps falling)
     - Neutral → dip zone centered on earliest large negative event time (or mid-round if no events)
     - Zone width = ~20% of round duration
     - Apply ±5-10% timing fuzz
   - **PeakMarker:** Inverse of DipMarker:
     - Bull trend → peak zone in last 30%
     - Bear trend → peak zone in first 30%
     - Neutral → centered on latest large positive event time
     - Same zone width and fuzz
   - **ClosingDirection:** `DirectionSign` = +1 if trend is bull, -1 if bear, random if neutral
     - At round start, this uses ACTUAL trend direction (not probabilistic guess from shop time)
     - Update DisplayText to match actual direction
   - **EventTiming:** `TimeMarkers[]` = EventScheduler's pre-scheduled fire times, normalized to 0-1
     - Apply ±3-5% timing fuzz per marker (so they're close but not frame-perfect)
   - **TrendReversal:** `ReversalTime` = estimated time when price trend perception shifts
     - For bull trend: find the scheduled event time closest to a cluster of events (disruption zone) in the back half of the round; if no events in back half, set to -1 (no reversal expected)
     - For bear trend: similar but look in front half
     - If trend is very strong (MaxTrendStrength), reversal less likely — may return -1
     - Apply timing fuzz

4. Integration point — `TipActivator` is called from `TradingState.Enter()` (or wherever round initialization happens):
   - After `PriceGenerator.InitializeRound()` and `EventScheduler.InitializeRound()` complete
   - Result stored in `RunContext.ActiveTipOverlays`
   - Publishes `TipOverlaysActivatedEvent` with the overlay list (for UI/chart to consume)

5. New event: `TipOverlaysActivatedEvent` in `GameEvents.cs`:
   - Contains `List<TipOverlayData> Overlays`

6. Parsing existing RevealedTip price values:
   - PriceFloor/PriceCeiling/PriceForecast store fuzzed price in RevealedText as formatted string
   - TipActivator must parse the numeric value from the display text OR (preferred) store the raw float in a new `RevealedTip.NumericValue` field at generation time

7. Tests:
   - TipActivator produces correct overlay count matching purchased tip count
   - DipMarker zone is in first 30% for bull trends, last 30% for bear trends
   - PeakMarker zone is inverse of DipMarker
   - ClosingDirection matches actual trend direction (+1 for bull, -1 for bear)
   - EventTiming markers count matches EventScheduler's scheduled count
   - EventTiming markers are within ±5% of actual fire times
   - TrendReversal returns -1 when no reversal expected (very strong trend, no events)
   - All overlay times are normalized 0-1
   - Price overlay values are within tier min/max range
   - Deterministic: same seed → same overlays

**Files to create:**
- `Assets/Scripts/Runtime/Shop/TipActivator.cs`

**Files to modify:**
- `Assets/Scripts/Runtime/Shop/InsiderTipGenerator.cs` — new type text generation
- `Assets/Scripts/Runtime/Shop/StoreDataTypes.cs` — add NumericValue field to RevealedTip if needed
- `Assets/Scripts/Runtime/Core/GameEvents.cs` — add TipOverlaysActivatedEvent
- `Assets/Scripts/Runtime/Core/GameStates/TradingState.cs` (or MarketOpenState) — call TipActivator
- `Assets/Scripts/Runtime/Events/EventScheduler.cs` — expose scheduled fire times (getter)
- `Assets/Scripts/Runtime/PriceEngine/PriceGenerator.cs` — expose trend direction/strength (getter if not already)
- `Assets/Tests/Runtime/Shop/TipActivatorTests.cs` — new test file

---

## Story 18.3: Chart Tip Overlay Rendering

As a player,
I want purchased insider tips to appear as visual overlays directly on the trading chart — price lines, shaded zones, event markers, and directional arrows,
so that tips are impossible to miss and directly inform my trading decisions in real time.

**Acceptance Criteria:**

1. New `TipOverlayRenderer` MonoBehaviour in `Scripts/Runtime/Chart/TipOverlayRenderer.cs`:
   - Created by ChartSetup during F5 rebuild, attached to ChartSystem GameObject
   - Receives `TipOverlaysActivatedEvent` and renders overlays
   - Updates overlay positions in `LateUpdate()` (chart Y-axis rescales every frame)
   - Clears all overlays on `RoundStartedEvent` (before new overlays arrive) and `ShopOpenedEvent`

2. **Horizontal Line Overlays** (Price Floor, Price Ceiling):
   - Dashed or solid horizontal LineRenderer spanning full chart width
   - Price Floor: colored cyan/teal, labeled "FLOOR ~$X.XX" on the left edge
   - Price Ceiling: colored amber/orange, labeled "CEILING ~$X.XX" on the left edge
   - Lines reposition in LateUpdate as Y-axis rescales
   - Lines fade to low opacity (don't compete with price line)
   - Sorting order: behind price line, in front of gridlines

3. **Horizontal Band Overlay** (Price Forecast):
   - Semi-transparent filled rectangle spanning full chart width
   - Centered on forecast price, extends ±BandHalfWidth vertically
   - Colored soft blue/purple at ~15-20% opacity
   - Labeled "FORECAST ~$X.XX" at left edge
   - Resizes in LateUpdate as Y-axis rescales
   - Implemented as a simple quad mesh (MeshFilter + MeshRenderer) or UI Image

4. **Vertical Marker Overlays** (Event Timing):
   - Thin vertical dashed LineRenderers from chart bottom to chart top
   - One per scheduled event, at the fuzzed normalized time position
   - Colored red/warning at ~40% opacity
   - Small icon or label at top: lightning bolt character or "!"
   - Position updates only if round duration changes (Time Buyer relic extends timer)
   - Pool of LineRenderers created in ChartSetup (max 15, matching max possible events)

5. **Time Zone Overlays** (Dip Marker, Peak Marker):
   - Semi-transparent filled rectangles covering full chart height, limited time width
   - Dip Marker: green-tinted zone, labeled "DIP ZONE" at top
   - Peak Marker: amber/gold-tinted zone, labeled "PEAK ZONE" at top
   - ~15-20% opacity so chart is still readable underneath
   - Implemented as quad meshes positioned in world space
   - Static X positions (don't rescale unless round duration changes)

6. **Trend Reversal Marker**:
   - Vertical dashed line (like Event Timing but distinct style)
   - Colored magenta/purple at ~50% opacity
   - Small U-turn arrow character or "R" label at top
   - Only rendered if `ReversalTime >= 0` (some rounds have no predicted reversal)

7. **Direction Arrow Overlay** (Closing Direction):
   - Arrow icon on the right edge of the chart
   - Points UP (green) for "closes higher" or DOWN (red) for "closes lower"
   - Positioned at vertical center of chart
   - Labeled "CLOSING UP" / "CLOSING DOWN" next to arrow
   - Static position (doesn't rescale)
   - Implemented as a Text element or SpriteRenderer

8. **Chart label text for overlays:**
   - All overlay labels use the existing terminal font at 10-11pt
   - Labels positioned to not overlap with axis labels (left edge for price overlays, top for time overlays)
   - Labels use matching overlay color at 70-80% opacity

9. **Overlay lifecycle:**
   - Created (GameObjects) during ChartSetup — always exist but `SetActive(false)` by default
   - Activated when `TipOverlaysActivatedEvent` received — show relevant overlays
   - Cleared when round ends or new shop opens — `SetActive(false)` all
   - No per-frame allocations in steady state

10. Tests:
    - Overlay positions are within chart bounds
    - Horizontal lines reposition correctly when price range changes
    - Time zones have correct normalized X positions
    - Overlays are hidden by default and shown only after activation event
    - Overlays clear on round end
    - Event timing marker count matches overlay data marker count
    - Direction arrow shows correct direction based on DirectionSign

**Files to create:**
- `Assets/Scripts/Runtime/Chart/TipOverlayRenderer.cs`

**Files to modify:**
- `Assets/Scripts/Setup/ChartSetup.cs` — create overlay GameObjects and wire to TipOverlayRenderer
- `Assets/Scripts/Runtime/Chart/ChartLineView.cs` — expose chart bounds for TipOverlayRenderer (or share via ChartDataHolder)
- `Assets/Scripts/Runtime/Chart/ChartVisualConfig.cs` — add overlay colors/opacity constants
- `Assets/Tests/Runtime/Chart/TipOverlayRendererTests.cs` — new test file

**Depends On:** Story 18.1 (data model), Story 18.2 (overlay data generation)

---

## Story 18.4: Trading HUD Tip Panel & Live Event Countdown

As a player,
I want a dedicated tip panel in the trading HUD that replaces the old pipe-separated text, and I want the Event Count tip to be a live countdown that ticks down as events fire,
so that my purchased intel is prominent and the event countdown gives me real-time tactical information.

**Acceptance Criteria:**

1. **Remove old tip text display:**
   - Remove the pipe-separated `_tipsDisplayText` from TradingHUD
   - Remove `SetTipsDisplay()` method and related code
   - Remove the Text element creation in UISetup for tips display

2. **New Tip Indicator Panel:**
   - Small panel positioned in the trading HUD area (above or below the chart, or in the control deck)
   - Shows compact indicators for purchased tips that aren't chart overlays
   - Each indicator: icon/symbol + short label + value
   - Panel hides entirely if no tips were purchased
   - Created by UISetup during F5

3. **Event Count Live Countdown:**
   - When Event Count tip is purchased, display "EVENTS: X" in the tip panel
   - Subscribe to `MarketEventFiredEvent` — decrement counter on each event
   - When counter reaches 0, display changes to "ALL CLEAR" in green
   - Counter shows actual scheduled count (from TipOverlayData.EventCountdown), not the shop estimate
   - Animated: brief flash/pulse when counter decrements

4. **Tip type indicators in panel:**
   - Chart-overlay tips (Floor, Ceiling, Forecast, Dip, Peak, EventTiming, Reversal, ClosingDirection) show a small "active" dot/badge indicating they're drawn on the chart — minimal HUD footprint since the chart carries the information
   - Event Count shows the live countdown (primary HUD element)
   - All indicators use the CRT terminal font and color scheme

5. **Panel layout:**
   - Horizontal or vertical strip, compact
   - Auto-sizes based on number of purchased tips (0-3 indicators)
   - Uses LayoutGroup for automatic arrangement
   - Follows existing UISetup programmatic creation pattern

6. Tests:
   - Event countdown decrements correctly on MarketEventFiredEvent
   - Countdown shows "ALL CLEAR" at zero
   - Panel hides when no tips purchased
   - Panel shows correct number of indicators for purchased tip count
   - Counter starts at TipOverlayData.EventCountdown value, not shop estimate

**Files to create:**
- `Assets/Scripts/Runtime/UI/TipPanel.cs` — new MonoBehaviour for the tip HUD panel

**Files to modify:**
- `Assets/Scripts/Runtime/UI/TradingHUD.cs` — remove old tip text, integrate with TipPanel
- `Assets/Scripts/Setup/UISetup.cs` — create TipPanel GameObjects
- `Assets/Scripts/Runtime/Core/GameEvents.cs` — no changes if MarketEventFiredEvent already exists
- `Assets/Tests/Runtime/UI/TipPanelTests.cs` — new test file

**Depends On:** Story 18.1 (data model), Story 18.2 (overlay data with EventCountdown)

---

## Story 18.5: Shop Tip Card Refresh

As a player,
I want the shop tip cards to have exciting face-down teasers, clear revealed text, and costs that reflect each tip's strategic value,
so that buying tips feels like purchasing a real insider advantage.

**Acceptance Criteria:**

1. **New face-down hints** (displayed before purchase):

   | Type | Face-Down Hint |
   |------|---------------|
   | PriceForecast | "What's the sweet spot?" |
   | PriceFloor | "How low can it go?" |
   | PriceCeiling | "What's the top?" |
   | EventCount | "How many surprises?" |
   | DipMarker | "When's the best buy?" |
   | PeakMarker | "When should you sell?" |
   | ClosingDirection | "Up or down?" |
   | EventTiming | "When do shakeups hit?" |
   | TrendReversal | "When does it turn?" |

2. **New formatted display names** (shown on card after purchase):

   | Type | Display Name |
   |------|-------------|
   | PriceForecast | PRICE FORECAST |
   | PriceFloor | PRICE FLOOR |
   | PriceCeiling | PRICE CEILING |
   | EventCount | EVENT COUNT |
   | DipMarker | DIP MARKER |
   | PeakMarker | PEAK MARKER |
   | ClosingDirection | CLOSING CALL |
   | EventTiming | EVENT TIMING |
   | TrendReversal | TREND REVERSAL |

3. **Updated revealed text format** (shown after card flip):

   | Type | Revealed Text Example |
   |------|----------------------|
   | PriceForecast | "Sweet spot around $6.50 — marked on chart" |
   | PriceFloor | "Floor at ~$3.20 — marked on chart" |
   | PriceCeiling | "Ceiling at ~$9.80 — marked on chart" |
   | EventCount | "Expect ~7 disruptions — live countdown active" |
   | DipMarker | "Best buy window marked on chart" |
   | PeakMarker | "Peak sell window marked on chart" |
   | ClosingDirection | "Round closes HIGHER" / "Round closes LOWER" |
   | EventTiming | "Event timing marked on chart" |
   | TrendReversal | "Trend reversal point marked on chart" |

4. **Visual type indicator on card:**
   - Small icon/symbol on each tip card indicating the type of overlay:
     - Chart overlay tips: small chart icon or "CHART" badge
     - Event Count: counter/clock icon
   - This helps players understand WHAT they're buying before they buy it

5. **Cost rebalancing:**
   - Costs reflect the actionability and power of each tip
   - High-value tips (direct timing answers) cost more
   - Lower-value tips (general info) cost less
   - Exact values set in GameConfig (from Story 18.1 AC3)

6. **Removed type cleanup:**
   - No face-down hints, display names, or revealed text for removed types
   - All switch statements in ShopUI handle only the 9 active types
   - No dead code for OpeningPrice, VolatilityWarning, TrendDirection, EventForecast

7. Tests:
   - All 9 types have non-null, non-empty face-down hints
   - All 9 types have non-null, non-empty display names
   - No duplicate face-down hints
   - Revealed text contains expected keywords per type (e.g., "chart" for overlay types)
   - Cost values match GameConfig for all 9 types

**Files to modify:**
- `Assets/Scripts/Runtime/UI/ShopUI.cs` — face-down hints, display names, revealed text, type indicators
- `Assets/Scripts/Runtime/Shop/InsiderTipGenerator.cs` — revealed text templates
- `Assets/Tests/Runtime/Shop/InsiderTipGeneratorTests.cs` — update expected values
- `Assets/Tests/Runtime/UI/ShopTipCardTests.cs` — new test file (or extend existing)

**Depends On:** Story 18.1 (data model with new types)

---

## Story 18.6: Event-Aware Accurate Tips

As a player,
I want my purchased insider tips to be **correct** — not fuzzy approximations that events can invalidate,
so that paying for intel gives me an actual strategic edge I can trust.

**Problem:** All price-based tips use static tier ranges with ±10% fuzz, but events push prices 25-100% beyond those ranges. Timing tips use heuristics and intentional fuzz. Tips should be accurate insider intel, not guesswork.

**Solution:** Pre-decide event types and effects at round start (not at fire time), then run a lightweight price simulation so TipActivator knows the actual price trajectory.

**Acceptance Criteria:**

1. `EventScheduler.InitializeRound()` pre-decides event types AND price effects for every scheduled slot via new `PreDecidedEvent` struct (fire time, config, rolled effect, phases for multi-phase events). `Update()` uses pre-decided data instead of re-rolling. Behavior identical to current system, just decided earlier.

2. New `TipActivator.SimulateRound()` method computes price trajectory analytically: compound trend between events, event effects at boundaries, multi-phase tracking (PumpAndDump peaks, FlashCrash troughs). Outputs `RoundSimulation` struct: MinPrice, MaxPrice, MinPriceNormalizedTime, MaxPriceNormalizedTime, ClosingPrice, AveragePrice.

3. All 9 tip types use simulation results — no tier statistics, no heuristics, no fuzz:
   - PriceCeiling → `simulation.MaxPrice`
   - PriceFloor → `simulation.MinPrice`
   - PriceForecast → `simulation.AveragePrice`
   - DipMarker → time zone centered on `simulation.MinPriceNormalizedTime`
   - PeakMarker → time zone centered on `simulation.MaxPriceNormalizedTime`
   - ClosingDirection → `sign(simulation.ClosingPrice - startingPrice)`
   - EventTiming → exact fire times from PreDecidedEvents (no fuzz)
   - EventCount → `PreDecidedEvents.Length` (already accurate)
   - TrendReversal → detect direction changes in simulated trajectory

4. Shop-time display text for price/direction tips changed to generic ("Price ceiling — revealed on chart") since accurate values require round initialization. Accurate values shown on chart overlays and updated in `RevealedTip.DisplayText` at round start.

5. All fuzz removed: `InsiderTipFuzzPercent` deprecated, `ApplyFuzz()` no longer used for tips, ±4% timing fuzz removed, ±5% zone fuzz removed.

6. `TipActivationContext` updated: remove `ScheduledEventCount`/`ScheduledFireTimes`, add `PreDecidedEvents[]`.

7. Tests: simulation accuracy, per-tip-type correctness vs simulation, no-fuzz verification, multi-phase event handling, determinism.

**Full story file:** `planning-artifacts/story-18-6-event-aware-accurate-tips.md`

**Files to modify:**
- `EventScheduler.cs` — PreDecidedEvent struct, pre-decide in InitializeRound, use in Update
- `TipActivator.cs` — SimulateRound, RoundSimulation, rewrite all overlay computations
- `StoreDataTypes.cs` — TipActivationContext field changes
- `InsiderTipGenerator.cs` — generic display text for price/direction tips
- `TradingState.cs` — pass PreDecidedEvents in context, handle updated RevealedTip list
- `GameConfig.cs` — deprecate InsiderTipFuzzPercent
- `InsiderTipDefinitions.cs` — update DescriptionTemplate for price/direction tips
- Tests: TipActivatorTests, EventSchedulerTests, InsiderTipGeneratorTests

**Depends On:** Stories 18.1-18.5 (all complete)

---

## Story 18.7: Accurate Tip Simulation & Honest Display

As a player,
I want my insider tips to accurately predict real price behavior, and to be displayed honestly as estimates rather than exact values,
so that I can trust tips as a strategic edge without feeling deceived when noise causes minor deviations.

**Problem:** `SimulateRound()` (18.6) models only trend + events. The runtime PriceGenerator has 4 layers: trend, noise, events, and mean reversion. The simulation ignores the event hold-phase target drift (where mean reversion pulls event targets back toward the trend line during the hold phase), causing 15-20% overestimation on ceilings. Tips display exact values, setting false precision expectations.

**Solution:** Add hold-phase drift modeling to `SimulateRound()` using tier mean reversion speed and event duration. Track trend line price for accurate deviation calculation. Display tip values with "~" prefix to convey estimates. Build EditMode integration tests that run full rounds (PriceGenerator + EventScheduler frame-by-frame) and verify predictions are within noise tolerance (±15%).

**Acceptance Criteria:**

1. `SimulateRound()` models event hold-phase target drift using MeanReversionSpeed, capped at `2 * NoiseAmplitude * price`, applied at 30% factor over 70% of event duration. Tracks trendLinePrice alongside currentPrice.
2. For max tracking: raw event target recorded as brief peak. For continuing trend: drifted post-event price used as base.
3. Multi-phase events (PumpAndDump, FlashCrash) also model drift on their respective phases.
4. Display text uses "~$" prefix for PriceCeiling, PriceFloor, PriceForecast. Overlay labels match.
5. ClosingDirection unchanged (binary, no precision issue).
6. Integration tests: run full rounds with PriceGenerator + EventScheduler, compare actual min/max/close/timing against SimulateRound() predictions. Assert within tolerance (±15% price, ±0.15 normalized time, ≥80% closing direction match rate).
7. Regression tests: drift reduces max vs naive simulation, compounding inflation eliminated, higher MRS = more drift, zero events unchanged.

**Full story file:** `planning-artifacts/story-18-7-accurate-tip-simulation-and-honest-display.md`

**Files to create:**
- `TipAccuracyIntegrationTests.cs` — full-round integration tests

**Files to modify:**
- `TipActivator.cs` — drift modeling in SimulateRound(), trend line tracking, "~" display format
- `TipActivatorTests.cs` — drift regression tests, updated display text assertions

**Depends On:** Stories 18.1-18.6 (all complete)

---

## Story Dependency Graph

```
18.1 (Data Model)
 ├──→ 18.2 (Generation & Activation)
 │     ├──→ 18.3 (Chart Overlays)    [needs overlay data]
 │     ├──→ 18.4 (HUD & Countdown)   [needs EventCountdown]
 │     ├──→ 18.6 (Accurate Tips)     [needs activation + overlay infrastructure]
 │     │     └──→ 18.7 (Accurate Simulation & Honest Display) [fixes 18.6 accuracy]
 │     └──→ 18.5 (Shop Cards)        [needs new types]
 └──→ 18.5 (Shop Cards)              [needs new types]
```

**Suggested implementation order:** 18.1 → 18.2 → 18.5 → 18.3 → 18.4 → 18.6 → **18.7**

---

## Technical Notes

### Chart Overlay Architecture

The chart currently uses world-space rendering (LineRenderers, MeshFilter+MeshRenderer, SpriteRenderers). Tip overlays should follow the same pattern:

- **Horizontal lines:** LineRenderer (same pattern as break-even line and short position line)
- **Shaded zones/bands:** Simple quad mesh via MeshFilter+MeshRenderer with semi-transparent material
- **Vertical markers:** LineRenderer array (pooled, same pattern as gridlines)
- **Text labels:** UI Text elements in the chart's ScreenSpaceOverlay canvas
- **Direction arrow:** Text element ("^" / "v") or SpriteRenderer

Coordinate transforms use the existing normalization pattern:
```
Time → X: Mathf.Lerp(chartLeft, chartRight, normalizedTime)
Price → Y: Mathf.Lerp(paddedBottom, paddedTop, (price - min) / range)
```

Y-axis rescales every frame (LateUpdate), so horizontal overlays MUST reposition every frame.
X-axis only changes if round duration changes (Time Buyer relic), so time-based overlays are mostly static.

### Event Scheduler Exposure

EventScheduler currently keeps `_scheduledFireTimes` private. Story 18.2 requires exposing this via a read-only getter (e.g., `public ReadOnlyCollection<float> ScheduledFireTimes`). This is a minimal change.

### Dip/Peak Estimation (Updated by Story 18.6)

~~The heuristic approach with ±5% fuzz has been superseded.~~ Story 18.6 replaces trend-direction heuristics with actual price simulation. DipMarker centers on the simulated minimum price time, PeakMarker on the simulated maximum price time. No fuzz applied — tips are accurate insider intel.

### Performance Considerations

- All overlay GameObjects pre-created in ChartSetup (no runtime Instantiate)
- Overlays toggled via SetActive(true/false)
- LateUpdate repositioning uses cached world-space bounds from ChartLineView
- Event timing marker pool sized to max events (15) — most will be inactive
- No per-frame allocations
