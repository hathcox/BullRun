# Story 18.2: Tip Generation & Round-Start Activation

Status: ready-for-dev

## Story

As a developer,
I want tip generation logic for all 9 types and a round-start activation system that computes chart overlay data using actual round parameters (trend direction, event schedule, starting price),
so that tips produce accurate, round-specific overlay geometry for the chart renderer in Story 18.3.

## Acceptance Criteria

1. `InsiderTipGenerator.CalculateRevealedText()` produces valid display text for all 5 new tip types — DipMarker, PeakMarker, ClosingDirection, EventTiming, TrendReversal
2. New `TipActivator` static class computes `List<TipOverlayData>` from purchased `RevealedTip` list using actual round-start data
3. `TipActivationContext` struct provides all round-start data TipActivator needs (stock instance, event scheduler, round duration, tier config, random seed)
4. Price-based tips (Floor, Ceiling, Forecast) populate overlay `PriceLevel`/`BandCenter` from `RevealedTip.NumericValue`
5. EventCount overlay sets `EventCountdown` from actual `EventScheduler.ScheduledEventCount` (not shop-time estimate)
6. DipMarker computes `TimeZoneCenter` in first 30% for bull trends, last 30% for bear trends, with ±5% fuzz
7. PeakMarker computes inverse of DipMarker (last 30% for bull, first 30% for bear)
8. ClosingDirection sets `DirectionSign` from actual `StockInstance.TrendDirection` (+1 bull, -1 bear, random neutral)
9. EventTiming populates `TimeMarkers[]` from `EventScheduler.GetScheduledTime()` normalized to 0-1, fuzzed ±3-5%
10. TrendReversal estimates disruption timing from event schedule clustering, returns -1 if no reversal expected
11. `TipActivator` called in `TradingState.Enter()` after `EventScheduler.InitializeRound()` — results stored in `RunContext.ActiveTipOverlays`
12. `TipOverlaysActivatedEvent` published after activation for chart/HUD to consume
13. All overlay times normalized 0-1; all deterministic given same seed
14. Tests: 20+ tests covering each tip type's overlay computation, edge cases (no events, neutral trend, empty tip list)

## Tasks / Subtasks

- [ ] Task 1: Update InsiderTipGenerator for new types (AC: 1)
  - [ ] Open `Assets/Scripts/Runtime/Shop/InsiderTipGenerator.cs`
  - [ ] In `CalculateRevealedText()` switch statement, add cases for 5 new types:
    ```csharp
    case InsiderTipType.DipMarker:
        return def.DescriptionTemplate;  // "Best buy window marked on chart"

    case InsiderTipType.PeakMarker:
        return def.DescriptionTemplate;  // "Peak sell window marked on chart"

    case InsiderTipType.ClosingDirection:
        // Probabilistic at shop time (actual direction resolved at round start)
        bool likelyHigher = random.NextDouble() < 0.6;  // 60% bull probability
        return string.Format(def.DescriptionTemplate, likelyHigher ? "HIGHER" : "LOWER");

    case InsiderTipType.EventTiming:
        return def.DescriptionTemplate;  // "Event timing marked on chart"

    case InsiderTipType.TrendReversal:
        return def.DescriptionTemplate;  // "Trend reversal point marked on chart"
    ```
  - [ ] For price-based types (PriceForecast, PriceFloor, PriceCeiling): ensure the caller stores raw fuzzed float in `RevealedTip.NumericValue` — update `GenerateTips()` method to pass numeric value when constructing `TipOffering`
  - [ ] Update `TipOffering` struct to carry `float NumericValue` alongside `RevealedText`
  - [ ] File: `Assets/Scripts/Runtime/Shop/InsiderTipGenerator.cs`

- [ ] Task 2: Create `TipActivationContext` struct (AC: 3)
  - [ ] Add to `Assets/Scripts/Runtime/Shop/StoreDataTypes.cs` (or new file `TipActivator.cs`):
    ```csharp
    public struct TipActivationContext
    {
        public StockInstance ActiveStock;          // From PriceGenerator.ActiveStocks[0]
        public int ScheduledEventCount;            // From EventScheduler.ScheduledEventCount
        public float[] ScheduledFireTimes;         // From EventScheduler, raw seconds
        public float RoundDuration;                // From TradingState._roundDuration
        public StockTierConfig TierConfig;         // From StockTierData.GetTierConfig(tier)
        public System.Random Random;               // For fuzz
    }
    ```
  - [ ] Context populated in TradingState.Enter() before calling TipActivator
  - [ ] `ScheduledFireTimes` array built by iterating `EventScheduler.GetScheduledTime(i)` for `i = 0..ScheduledEventCount-1`
  - [ ] File: `Assets/Scripts/Runtime/Shop/TipActivator.cs`

- [ ] Task 3: Create `TipActivator` static class (AC: 2, 11)
  - [ ] Create new file: `Assets/Scripts/Runtime/Shop/TipActivator.cs`
  - [ ] Main method signature:
    ```csharp
    public static class TipActivator
    {
        public static List<TipOverlayData> ActivateTips(
            List<RevealedTip> purchasedTips,
            TipActivationContext ctx)
        {
            var overlays = new List<TipOverlayData>(purchasedTips.Count);
            foreach (var tip in purchasedTips)
            {
                var overlay = ComputeOverlay(tip, ctx);
                overlays.Add(overlay);
            }
            return overlays;
        }

        private static TipOverlayData ComputeOverlay(RevealedTip tip, TipActivationContext ctx)
        {
            switch (tip.Type)
            {
                case InsiderTipType.PriceFloor: return ComputePriceLineOverlay(tip, "FLOOR");
                case InsiderTipType.PriceCeiling: return ComputePriceLineOverlay(tip, "CEILING");
                case InsiderTipType.PriceForecast: return ComputePriceBandOverlay(tip, ctx);
                case InsiderTipType.EventCount: return ComputeEventCountOverlay(ctx);
                case InsiderTipType.DipMarker: return ComputeDipMarkerOverlay(ctx);
                case InsiderTipType.PeakMarker: return ComputePeakMarkerOverlay(ctx);
                case InsiderTipType.ClosingDirection: return ComputeClosingDirectionOverlay(ctx);
                case InsiderTipType.EventTiming: return ComputeEventTimingOverlay(ctx);
                case InsiderTipType.TrendReversal: return ComputeTrendReversalOverlay(ctx);
                default: return default;
            }
        }
    }
    ```
  - [ ] File: `Assets/Scripts/Runtime/Shop/TipActivator.cs`

- [ ] Task 4: Implement price-based overlay computation (AC: 4)
  - [ ] `ComputePriceLineOverlay(RevealedTip tip, string labelPrefix)`:
    ```csharp
    return new TipOverlayData
    {
        Type = tip.Type,
        Label = $"{labelPrefix} ~${tip.NumericValue:F2}",
        PriceLevel = tip.NumericValue
    };
    ```
  - [ ] `ComputePriceBandOverlay(RevealedTip tip, TipActivationContext ctx)`:
    ```csharp
    float priceRange = ctx.TierConfig.MaxPrice - ctx.TierConfig.MinPrice;
    return new TipOverlayData
    {
        Type = tip.Type,
        Label = $"FORECAST ~${tip.NumericValue:F2}",
        BandCenter = tip.NumericValue,
        BandHalfWidth = priceRange * 0.12f  // ±12% of tier range
    };
    ```
  - [ ] File: `Assets/Scripts/Runtime/Shop/TipActivator.cs`

- [ ] Task 5: Implement EventCount overlay (AC: 5)
  - [ ] `ComputeEventCountOverlay(TipActivationContext ctx)`:
    ```csharp
    return new TipOverlayData
    {
        Type = InsiderTipType.EventCount,
        Label = $"EVENTS: {ctx.ScheduledEventCount}",
        EventCountdown = ctx.ScheduledEventCount
    };
    ```
  - [ ] Uses ACTUAL scheduled count (includes relic multipliers like EventCountMultiplier), not the shop-time estimate
  - [ ] File: `Assets/Scripts/Runtime/Shop/TipActivator.cs`

- [ ] Task 6: Implement DipMarker overlay (AC: 6)
  - [ ] `ComputeDipMarkerOverlay(TipActivationContext ctx)`:
    - [ ] **Bull trend** (`TrendDirection.Bull`): dip zone centered at **0.15** (early — price is lowest before compound growth)
    - [ ] **Bear trend** (`TrendDirection.Bear`): dip zone centered at **0.85** (late — price keeps falling)
    - [ ] **Neutral** (`TrendDirection.Neutral`): dip zone centered at **0.50**
    - [ ] Zone half-width: **0.10** (so total zone is 20% of round duration)
    - [ ] Apply fuzz: center ± `(random.NextDouble() * 2 - 1) * 0.05` (±5%)
    - [ ] Clamp center to [zoneHalfWidth, 1.0 - zoneHalfWidth] so zone doesn't go off chart
    ```csharp
    float center = ctx.ActiveStock.TrendDirection switch
    {
        TrendDirection.Bull => 0.15f,
        TrendDirection.Bear => 0.85f,
        _ => 0.50f
    };
    float fuzz = ((float)ctx.Random.NextDouble() * 2f - 1f) * 0.05f;
    center = Mathf.Clamp(center + fuzz, 0.10f, 0.90f);
    return new TipOverlayData
    {
        Type = InsiderTipType.DipMarker,
        Label = "DIP ZONE",
        TimeZoneCenter = center,
        TimeZoneHalfWidth = 0.10f
    };
    ```
  - [ ] File: `Assets/Scripts/Runtime/Shop/TipActivator.cs`

- [ ] Task 7: Implement PeakMarker overlay (AC: 7)
  - [ ] `ComputePeakMarkerOverlay(TipActivationContext ctx)`:
    - [ ] Inverse of DipMarker:
    - [ ] **Bull trend**: peak zone centered at **0.85** (late — compound growth maximizes)
    - [ ] **Bear trend**: peak zone centered at **0.15** (early — before compound decay)
    - [ ] **Neutral**: centered at **0.50**
    - [ ] Same half-width (0.10) and fuzz (±5%) as DipMarker
    ```csharp
    float center = ctx.ActiveStock.TrendDirection switch
    {
        TrendDirection.Bull => 0.85f,
        TrendDirection.Bear => 0.15f,
        _ => 0.50f
    };
    // Same fuzz and clamp as DipMarker
    ```
  - [ ] File: `Assets/Scripts/Runtime/Shop/TipActivator.cs`

- [ ] Task 8: Implement ClosingDirection overlay (AC: 8)
  - [ ] `ComputeClosingDirectionOverlay(TipActivationContext ctx)`:
    ```csharp
    int direction = ctx.ActiveStock.TrendDirection switch
    {
        TrendDirection.Bull => 1,
        TrendDirection.Bear => -1,
        _ => ctx.Random.NextDouble() < 0.5 ? 1 : -1  // Neutral: coin flip
    };
    return new TipOverlayData
    {
        Type = InsiderTipType.ClosingDirection,
        Label = direction > 0 ? "CLOSING UP" : "CLOSING DOWN",
        DirectionSign = direction
    };
    ```
  - [ ] At round start this uses ACTUAL trend direction from StockInstance, overriding the probabilistic shop-time guess
  - [ ] File: `Assets/Scripts/Runtime/Shop/TipActivator.cs`

- [ ] Task 9: Implement EventTiming overlay (AC: 9)
  - [ ] `ComputeEventTimingOverlay(TipActivationContext ctx)`:
    ```csharp
    if (ctx.ScheduledEventCount == 0 || ctx.ScheduledFireTimes == null)
    {
        return new TipOverlayData
        {
            Type = InsiderTipType.EventTiming,
            Label = "NO EVENTS",
            TimeMarkers = System.Array.Empty<float>()
        };
    }

    float[] markers = new float[ctx.ScheduledEventCount];
    for (int i = 0; i < ctx.ScheduledEventCount; i++)
    {
        float normalized = ctx.ScheduledFireTimes[i] / ctx.RoundDuration;
        float fuzz = ((float)ctx.Random.NextDouble() * 2f - 1f) * 0.04f; // ±4%
        markers[i] = Mathf.Clamp01(normalized + fuzz);
    }
    System.Array.Sort(markers);  // Keep chronological order after fuzz

    return new TipOverlayData
    {
        Type = InsiderTipType.EventTiming,
        Label = "EVENT TIMING",
        TimeMarkers = markers
    };
    ```
  - [ ] Fuzz is ±4% of normalized time (so for 60s round, ±2.4s offset)
  - [ ] Markers clamped to [0,1] and sorted after fuzz
  - [ ] File: `Assets/Scripts/Runtime/Shop/TipActivator.cs`

- [ ] Task 10: Implement TrendReversal overlay (AC: 10)
  - [ ] `ComputeTrendReversalOverlay(TipActivationContext ctx)`:
    - [ ] Algorithm: Find the most likely reversal point — where the dominant trend is most likely to be disrupted by events
    - [ ] **For Bull trend**: Look for event cluster in back half (0.5-1.0) — events late in a bull run can cause perceived reversal
    - [ ] **For Bear trend**: Look for event cluster in front half (0.0-0.5) — events early can arrest the decline
    - [ ] **No reversal cases (return ReversalTime = -1)**:
      - No events scheduled
      - Neutral trend (no dominant trend to reverse)
      - Very few events (≤ 1) in the relevant half
    - [ ] **Cluster finding heuristic**: Among events in the relevant half, pick the one closest to the midpoint of that half (most impactful timing)
    ```csharp
    if (ctx.ScheduledEventCount == 0 || ctx.ActiveStock.TrendDirection == TrendDirection.Neutral)
    {
        return new TipOverlayData { Type = InsiderTipType.TrendReversal, ReversalTime = -1f };
    }

    bool isBull = ctx.ActiveStock.TrendDirection == TrendDirection.Bull;
    float searchStart = isBull ? 0.5f : 0.0f;
    float searchEnd = isBull ? 1.0f : 0.5f;
    float searchMid = (searchStart + searchEnd) / 2f;  // 0.75 for bull, 0.25 for bear

    float bestTime = -1f;
    float bestDistance = float.MaxValue;
    int eventsInHalf = 0;

    for (int i = 0; i < ctx.ScheduledEventCount; i++)
    {
        float normalized = ctx.ScheduledFireTimes[i] / ctx.RoundDuration;
        if (normalized >= searchStart && normalized <= searchEnd)
        {
            eventsInHalf++;
            float distance = Mathf.Abs(normalized - searchMid);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestTime = normalized;
            }
        }
    }

    if (eventsInHalf <= 1)
    {
        return new TipOverlayData { Type = InsiderTipType.TrendReversal, ReversalTime = -1f };
    }

    float fuzz = ((float)ctx.Random.NextDouble() * 2f - 1f) * 0.05f;
    bestTime = Mathf.Clamp01(bestTime + fuzz);

    return new TipOverlayData
    {
        Type = InsiderTipType.TrendReversal,
        Label = "REVERSAL",
        ReversalTime = bestTime
    };
    ```
  - [ ] File: `Assets/Scripts/Runtime/Shop/TipActivator.cs`

- [ ] Task 11: Add TipOverlaysActivatedEvent (AC: 12)
  - [ ] Open `Assets/Scripts/Runtime/Core/GameEvents.cs`
  - [ ] Add new event struct:
    ```csharp
    public struct TipOverlaysActivatedEvent
    {
        public List<TipOverlayData> Overlays;
    }
    ```
  - [ ] File: `Assets/Scripts/Runtime/Core/GameEvents.cs`

- [ ] Task 12: Wire TipActivator into TradingState.Enter() (AC: 11, 12)
  - [ ] Open `Assets/Scripts/Runtime/Core/GameStates/TradingState.cs`
  - [ ] After EventScheduler.InitializeRound() call (after line 113), add:
    ```csharp
    // Activate insider tip overlays for this round
    if (ctx.RevealedTips != null && ctx.RevealedTips.Count > 0
        && _priceGenerator != null && _priceGenerator.ActiveStocks.Count > 0)
    {
        var activationCtx = new TipActivationContext
        {
            ActiveStock = _priceGenerator.ActiveStocks[0],
            ScheduledEventCount = _eventScheduler != null ? _eventScheduler.ScheduledEventCount : 0,
            ScheduledFireTimes = BuildFireTimesArray(_eventScheduler),
            RoundDuration = _roundDuration,
            TierConfig = StockTierData.GetTierConfig(ctx.CurrentTier),
            Random = new System.Random(ctx.CurrentRound * 31 + ctx.CurrentAct)
        };
        ctx.ActiveTipOverlays.Clear();
        ctx.ActiveTipOverlays.AddRange(TipActivator.ActivateTips(ctx.RevealedTips, activationCtx));

        EventBus.Publish(new TipOverlaysActivatedEvent { Overlays = ctx.ActiveTipOverlays });
    }
    ```
  - [ ] Add helper method to TradingState:
    ```csharp
    private static float[] BuildFireTimesArray(EventScheduler scheduler)
    {
        if (scheduler == null || scheduler.ScheduledEventCount == 0)
            return System.Array.Empty<float>();
        var times = new float[scheduler.ScheduledEventCount];
        for (int i = 0; i < times.Length; i++)
            times[i] = scheduler.GetScheduledTime(i);
        return times;
    }
    ```
  - [ ] File: `Assets/Scripts/Runtime/Core/GameStates/TradingState.cs`

- [ ] Task 13: Expose EventScheduler.GetScheduledTime() if not already public (AC: 9)
  - [ ] Open `Assets/Scripts/Runtime/Events/EventScheduler.cs`
  - [ ] Verify `GetScheduledTime(int index)` is public (it exists at line 318)
  - [ ] Verify `ScheduledEventCount` property is public (line 29)
  - [ ] If either is missing or private, add public accessor
  - [ ] File: `Assets/Scripts/Runtime/Events/EventScheduler.cs`

- [ ] Task 14: Update ShopState to pass NumericValue through purchase flow (AC: 4)
  - [ ] Open `Assets/Scripts/Runtime/Core/GameStates/ShopState.cs`
  - [ ] In `OnTipPurchaseRequested()` (line 341): when constructing `RevealedTip`, pass `NumericValue` from `TipOffering`:
    ```csharp
    var tip = new RevealedTip(offering.Definition.Type, offering.RevealedText, offering.NumericValue);
    ```
  - [ ] This requires TipOffering to carry NumericValue (Task 1)
  - [ ] File: `Assets/Scripts/Runtime/Core/GameStates/ShopState.cs`

- [ ] Task 15: Write TipActivator tests (AC: 14)
  - [ ] Create `Assets/Tests/Runtime/Shop/TipActivatorTests.cs`
  - [ ] Test helper: create `TipActivationContext` with controlled values:
    ```csharp
    private TipActivationContext CreateContext(
        TrendDirection trend = TrendDirection.Bull,
        float startingPrice = 10f,
        float[] fireTimes = null,
        float roundDuration = 60f,
        int seed = 42)
    ```
  - [ ] **Price overlay tests:**
    - [ ] PriceFloor_ProducesPriceLevelOverlay — PriceLevel equals NumericValue
    - [ ] PriceCeiling_ProducesPriceLevelOverlay — PriceLevel equals NumericValue
    - [ ] PriceForecast_ProducesBandOverlay — BandCenter equals NumericValue, BandHalfWidth > 0
  - [ ] **EventCount tests:**
    - [ ] EventCount_UsesActualScheduledCount — EventCountdown matches context's ScheduledEventCount, not shop estimate
    - [ ] EventCount_ZeroEvents_ReturnsZeroCountdown
  - [ ] **DipMarker tests:**
    - [ ] DipMarker_BullTrend_ZoneInFirstThird — TimeZoneCenter < 0.35
    - [ ] DipMarker_BearTrend_ZoneInLastThird — TimeZoneCenter > 0.65
    - [ ] DipMarker_NeutralTrend_ZoneNearCenter — TimeZoneCenter between 0.35 and 0.65
    - [ ] DipMarker_ZoneWidthIsTenPercent — TimeZoneHalfWidth == 0.10f
    - [ ] DipMarker_ZoneClamped_NeverOffChart — center always in [0.10, 0.90]
  - [ ] **PeakMarker tests:**
    - [ ] PeakMarker_BullTrend_ZoneInLastThird — TimeZoneCenter > 0.65
    - [ ] PeakMarker_BearTrend_ZoneInFirstThird — TimeZoneCenter < 0.35
    - [ ] PeakMarker_InverseOfDipMarker — bull peak > bull dip, bear peak < bear dip
  - [ ] **ClosingDirection tests:**
    - [ ] ClosingDirection_BullTrend_ReturnsPositive — DirectionSign == +1
    - [ ] ClosingDirection_BearTrend_ReturnsNegative — DirectionSign == -1
    - [ ] ClosingDirection_NeutralTrend_ReturnsEitherDirection — DirectionSign is +1 or -1 (not 0)
  - [ ] **EventTiming tests:**
    - [ ] EventTiming_MarkerCountMatchesEventCount — TimeMarkers.Length == ScheduledEventCount
    - [ ] EventTiming_MarkersNormalized — all markers in [0, 1]
    - [ ] EventTiming_MarkersSorted — markers in ascending order after fuzz
    - [ ] EventTiming_MarkersWithinFuzzOfActual — each marker within ±5% of actual normalized time
    - [ ] EventTiming_NoEvents_ReturnsEmptyArray
  - [ ] **TrendReversal tests:**
    - [ ] TrendReversal_NeutralTrend_ReturnsNegativeOne — no reversal for neutral
    - [ ] TrendReversal_NoEvents_ReturnsNegativeOne
    - [ ] TrendReversal_FewEventsInHalf_ReturnsNegativeOne — ≤1 event in relevant half
    - [ ] TrendReversal_BullWithLateEvents_ReturnsTimeInBackHalf — ReversalTime > 0.5
    - [ ] TrendReversal_BearWithEarlyEvents_ReturnsTimeInFrontHalf — ReversalTime < 0.5
  - [ ] **Integration tests:**
    - [ ] ActivateTips_EmptyList_ReturnsEmptyList
    - [ ] ActivateTips_MultipleTips_ReturnsCorrectCount
    - [ ] ActivateTips_Deterministic_SameSeedSameResults — verify with two identical calls
  - [ ] File: `Assets/Tests/Runtime/Shop/TipActivatorTests.cs`

## Dev Notes

### Architecture Compliance

- **TipActivator is a static class** — follows the pattern of static utility classes in the project (no MonoBehaviour dependency, pure C# logic). Testable without Unity runtime.
- **EventBus for communication** — TipOverlaysActivatedEvent published for chart/HUD to subscribe to. TipActivator does NOT reference ChartLineView or TradingHUD directly.
- **No ScriptableObjects** — all data flows through structs and static classes.
- **RunContext carries state** — ActiveTipOverlays stored on RunContext, accessible by any system that needs it.
- **Assembly boundary safe** — TipActivator in Scripts/Runtime/Shop/ can reference StockInstance (Runtime/PriceEngine), EventScheduler (Runtime/Events), and StoreDataTypes (Runtime/Shop). All within Runtime assembly.

### Initialization Sequence (CRITICAL)

The exact call order in the game state machine:

```
1. MarketOpenState.Enter()  → PriceGenerator.InitializeRound()     (line 50)
   [Preview phase — stock data now available]
2. TradingState.Enter()     → RoundStartedEvent published           (line 94)
                             → Relics dispatch synchronously (set EventCountMultiplier etc.)
                             → EventScheduler.InitializeRound()      (line 107)
                             → [NEW] TipActivator.ActivateTips()     (line ~114)
                             → TipOverlaysActivatedEvent published
   [Trading begins — overlays visible on chart]
```

TipActivator MUST be called after BOTH PriceGenerator and EventScheduler have initialized. PriceGenerator inits in MarketOpenState, EventScheduler inits in TradingState. The insertion point is TradingState.Enter() after line 113.

### StockInstance Access Pattern

```csharp
// From PriceGenerator:
IReadOnlyList<StockInstance> stocks = _priceGenerator.ActiveStocks;
StockInstance stock = stocks[0];  // Always 1 stock per round (MinStocksPerRound = MaxStocksPerRound = 1)

// Available at round start:
stock.TrendDirection   // Bull, Bear, or Neutral
stock.TrendRate        // ±percentage per second (positive for bull, negative for bear)
stock.StartingPrice    // Random within tier range
stock.TierConfig       // Full StockTierConfig struct
```

### EventScheduler Access Pattern

```csharp
// Public accessors (already exist):
int count = _eventScheduler.ScheduledEventCount;  // Line 29
float time = _eventScheduler.GetScheduledTime(i);  // Line 318 — raw seconds

// Build array for TipActivationContext:
float[] times = new float[count];
for (int i = 0; i < count; i++)
    times[i] = _eventScheduler.GetScheduledTime(i);
```

### TrendDirection Probabilities

From PriceGenerator.PickRandomTrendDirection() (line 299):
- **60% Bull** — price trends up
- **25% Bear** — price trends down
- **15% Neutral** — flat trend

At shop time, ClosingDirection uses these probabilities to make a guess. At round start, TipActivator reads the ACTUAL direction and may update the overlay label. The shop-time text is a preview; the chart overlay shows the truth.

### Dip/Peak Heuristic Rationale

Price follows compound growth: `price(t) ≈ startPrice × (1 + trendRate)^t`

- **Bull trend (positive TrendRate)**: Compound growth means price accelerates upward over time. The minimum price is at or near the start. The maximum is at the end.
- **Bear trend (negative TrendRate)**: Compound decay means price drops fastest later. The maximum is near the start, the minimum near the end.
- **Events can override** these patterns, but the trend is the dominant force over a full 60s round.

The ±5% fuzz and 20% zone width (±10% half-width) account for event disruptions without over-promising precision.

### TrendReversal Algorithm Explanation

A "reversal" in the price engine isn't a programmatic event — the trend rate is constant. What players perceive as a reversal is when events push the price against the dominant trend hard enough. The heuristic:

1. For bull trends, search the back half (0.5-1.0) for event clusters
2. For bear trends, search the front half (0.0-0.5)
3. Pick the event closest to the midpoint of the search region (most impactful timing)
4. If ≤1 event exists in the search half, return -1 (no reversal expected)
5. Neutral trends always return -1 (no dominant trend to reverse)

### Test StockInstance Mocking

StockInstance is a concrete class with public setters for most fields. Tests can construct one directly:

```csharp
var stock = new StockInstance();
stock.Initialize(0, "TEST", StockTier.Penny, 6.50f, TrendDirection.Bull, 0.015f);
```

No mocking framework needed — StockInstance.Initialize() sets all fields used by TipActivator.

### Existing Code to Read Before Implementing

1. `Assets/Scripts/Runtime/Shop/StoreDataTypes.cs` — TipOverlayData struct (from Story 18.1)
2. `Assets/Scripts/Runtime/Shop/InsiderTipGenerator.cs` — CalculateRevealedText(), GenerateTips()
3. `Assets/Scripts/Runtime/Core/GameStates/TradingState.cs` — Enter() method, lines 53-118
4. `Assets/Scripts/Runtime/PriceEngine/PriceGenerator.cs` — InitializeRound(), ActiveStocks
5. `Assets/Scripts/Runtime/PriceEngine/StockInstance.cs` — All properties, Initialize()
6. `Assets/Scripts/Runtime/Events/EventScheduler.cs` — InitializeRound(), GetScheduledTime()
7. `Assets/Scripts/Runtime/Core/RunContext.cs` — ActiveTipOverlays (from Story 18.1)
8. `Assets/Scripts/Runtime/Core/GameEvents.cs` — existing event patterns

### Depends On

- Story 18.1 (Tip Data Model) — InsiderTipType enum with new values, TipOverlayData struct, RevealedTip with NumericValue, RunContext.ActiveTipOverlays

### References

- [Source: _bmad-output/planning-artifacts/epic-18-insider-tips-overhaul.md#Story 18.2]
- [Source: _bmad-output/project-context.md#EventBus Communication]
- [Source: _bmad-output/project-context.md#Testing Rules]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Debug Log References

N/A

### Completion Notes List

### File List

### Change Log

- 2026-02-21: Story 18.2 created — comprehensive implementation guide for tip generation and round-start activation
