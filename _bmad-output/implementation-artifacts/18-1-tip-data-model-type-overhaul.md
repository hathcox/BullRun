# Story 18.1: Tip Data Model & Type Overhaul

Status: done

## Story

As a developer,
I want to restructure the insider tip type system — removing 4 underperforming types, adding 5 new actionable types, and extending the data model to carry chart overlay data,
so that the tip system has the foundation for visual chart overlays and round-start activation in subsequent stories.

## Acceptance Criteria

1. `InsiderTipType` enum updated: remove `OpeningPrice`, `VolatilityWarning`, `TrendDirection`, `EventForecast`; add `DipMarker`, `PeakMarker`, `ClosingDirection`, `EventTiming`, `TrendReversal` — total 9 types
2. `InsiderTipDefinitions.All` updated with 9 definitions (4 kept, 5 new) — each has `Type`, `DescriptionTemplate`, `Cost`
3. `GameConfig` tip cost constants: remove 4 old, add 5 new — Low (10-15), Medium (20-25), High (30-35) Rep tiers
4. `RevealedTip` struct extended with `float NumericValue` field (stores raw fuzzed price for price-based tips, 0 for non-price tips) and `bool IsActivated` flag
5. New `TipOverlayData` struct in `StoreDataTypes.cs` with overlay geometry fields for all chart overlay types
6. `RunContext` gains `List<TipOverlayData> ActiveTipOverlays` property, initialized empty, cleared on `ResetForNewRun()`
7. `InsiderTipGenerator.CalculateRevealedText()` — remove 4 dead branches, add 5 stub branches returning placeholder text for new types
8. `ShopUI.FormatTipTypeName()` and `GetTipFaceDownHint()` — remove 4 dead cases, add 5 new cases
9. All 7 affected test files updated — fix enum counts, fix removed type references, add new type assertions
10. New data validation tests: all 9 types exist, all 9 definitions have non-zero costs matching GameConfig, no duplicates, `TipOverlayData` default-constructs to safe values

## Tasks / Subtasks

- [x] Task 1: Update `InsiderTipType` enum (AC: 1)
  - [x] Open `Assets/Scripts/Runtime/Shop/StoreDataTypes.cs` (enum at lines 40–50)
  - [x] Remove enum values: `TrendDirection` (3), `EventForecast` (4), `VolatilityWarning` (6), `OpeningPrice` (7)
  - [x] Add enum values: `DipMarker`, `PeakMarker`, `ClosingDirection`, `EventTiming`, `TrendReversal`
  - [x] Final enum should have exactly 9 values: `PriceForecast`, `PriceFloor`, `PriceCeiling`, `EventCount`, `DipMarker`, `PeakMarker`, `ClosingDirection`, `EventTiming`, `TrendReversal`
  - [x] File: `Assets/Scripts/Runtime/Shop/StoreDataTypes.cs`

- [x] Task 2: Extend `RevealedTip` struct (AC: 4)
  - [x] Open `Assets/Scripts/Runtime/Shop/StoreDataTypes.cs` (struct at lines 25–35)
  - [x] Add field: `public float NumericValue;` — raw fuzzed price for PriceFloor/PriceCeiling/PriceForecast, 0 for non-price tips
  - [x] Add field: `public bool IsActivated;` — false at purchase time, true after round-start activation (Story 18.2)
  - [x] Update constructor to accept optional `float numericValue = 0f` parameter (keep backward-compatible: existing 2-arg constructor must still work)
  - [x] Rename `RevealedText` to `DisplayText` throughout codebase (all references — use find-replace)
  - [x] File: `Assets/Scripts/Runtime/Shop/StoreDataTypes.cs`

- [x] Task 3: Create `TipOverlayData` struct (AC: 5)
  - [x] Add new struct in `Assets/Scripts/Runtime/Shop/StoreDataTypes.cs` after `RevealedTip`:
    ```csharp
    public struct TipOverlayData
    {
        public InsiderTipType Type;
        public string Label;            // Display label for overlay (e.g., "FLOOR ~$3.20")

        // Horizontal line overlays (PriceFloor, PriceCeiling)
        public float PriceLevel;        // 0 = not applicable

        // Horizontal band overlay (PriceForecast)
        public float BandCenter;        // 0 = not applicable
        public float BandHalfWidth;     // 0 = not applicable

        // Time zone overlays (DipMarker, PeakMarker) — normalized 0-1
        public float TimeZoneCenter;    // -1 = not applicable
        public float TimeZoneHalfWidth; // 0 = not applicable

        // Vertical time markers (EventTiming) — normalized 0-1
        public float[] TimeMarkers;     // null = not applicable

        // Trend reversal marker — normalized 0-1
        public float ReversalTime;      // -1 = no reversal expected

        // Direction arrow (ClosingDirection)
        public int DirectionSign;       // +1 = higher, -1 = lower, 0 = not applicable

        // Live counter (EventCount)
        public int EventCountdown;      // -1 = not applicable
    }
    ```
  - [x] Default values must be safe: `PriceLevel = 0`, `TimeZoneCenter = -1`, `ReversalTime = -1`, `DirectionSign = 0`, `EventCountdown = -1`, `TimeMarkers = null`
  - [x] File: `Assets/Scripts/Runtime/Shop/StoreDataTypes.cs`

- [x] Task 4: Update `GameConfig` cost constants (AC: 3)
  - [x] Open `Assets/Scripts/Setup/Data/GameConfig.cs` (lines 51–58)
  - [x] Remove constants: `TipCostTrendDirection`, `TipCostEventForecast`, `TipCostVolatilityWarning`, `TipCostOpeningPrice`
  - [x] Add constants with cost tiers reflecting actionability:
    ```csharp
    // Existing (adjust if needed):
    public static readonly int TipCostPriceForecast = 15;     // Low tier
    public static readonly int TipCostPriceFloor = 20;        // Medium tier
    public static readonly int TipCostPriceCeiling = 20;      // Medium tier
    public static readonly int TipCostEventCount = 10;        // Low tier

    // New:
    public static readonly int TipCostDipMarker = 30;         // High tier — answers "when to buy"
    public static readonly int TipCostPeakMarker = 30;        // High tier — answers "when to sell"
    public static readonly int TipCostClosingDirection = 15;   // Low tier — binary info
    public static readonly int TipCostEventTiming = 35;       // High tier — multiple data points
    public static readonly int TipCostTrendReversal = 25;     // Medium tier — timing estimate
    ```
  - [x] File: `Assets/Scripts/Setup/Data/GameConfig.cs`

- [x] Task 5: Update `InsiderTipDefinitions` (AC: 2)
  - [x] Open `Assets/Scripts/Setup/Data/InsiderTipDefinitions.cs` (lines 25–51)
  - [x] Remove 4 definitions for removed types (lines 36–38 TrendDirection, 39–41 EventForecast, 45–47 VolatilityWarning, 48–50 OpeningPrice)
  - [x] Add 5 new definitions:
    ```csharp
    new InsiderTipDef(InsiderTipType.DipMarker,
        "Best buy window marked on chart", GameConfig.TipCostDipMarker),
    new InsiderTipDef(InsiderTipType.PeakMarker,
        "Peak sell window marked on chart", GameConfig.TipCostPeakMarker),
    new InsiderTipDef(InsiderTipType.ClosingDirection,
        "Round closes {0}", GameConfig.TipCostClosingDirection),
    new InsiderTipDef(InsiderTipType.EventTiming,
        "Event timing marked on chart", GameConfig.TipCostEventTiming),
    new InsiderTipDef(InsiderTipType.TrendReversal,
        "Trend reversal point marked on chart", GameConfig.TipCostTrendReversal),
    ```
  - [x] Verify `All` array has exactly 9 entries after edit
  - [x] File: `Assets/Scripts/Setup/Data/InsiderTipDefinitions.cs`

- [x] Task 6: Update `RunContext` (AC: 6)
  - [x] Open `Assets/Scripts/Runtime/Core/RunContext.cs`
  - [x] Add property near line 22: `public List<TipOverlayData> ActiveTipOverlays { get; private set; }`
  - [x] Initialize in constructor (near line 137): `ActiveTipOverlays = new List<TipOverlayData>();`
  - [x] Clear in `ResetForNewRun()` (near line 262): `ActiveTipOverlays.Clear();`
  - [x] File: `Assets/Scripts/Runtime/Core/RunContext.cs`

- [x] Task 7: Update `InsiderTipGenerator` — remove dead branches, add stubs (AC: 7)
  - [x] Open `Assets/Scripts/Runtime/Shop/InsiderTipGenerator.cs`
  - [x] In `CalculateRevealedText()` switch statement (lines 63–127):
    - [x] Remove cases: `TrendDirection` (87–92), `EventForecast` (94–100), `VolatilityWarning` (110–115), `OpeningPrice` (117–123)
    - [x] Remove helper methods: `ClassifyTrendDirection()` (145–156), `ClassifyEventForecast()` (163–180), `ClassifyVolatility()` (190–195)
  - [x] Add new case stubs:
    ```csharp
    case InsiderTipType.DipMarker:
        return def.DescriptionTemplate;
    case InsiderTipType.PeakMarker:
        return def.DescriptionTemplate;
    case InsiderTipType.ClosingDirection:
        // Probabilistic at shop time: bull trend more likely → "HIGHER"
        bool closesHigher = random.NextDouble() < 0.6;
        return string.Format(def.DescriptionTemplate, closesHigher ? "HIGHER" : "LOWER");
    case InsiderTipType.EventTiming:
        return def.DescriptionTemplate;
    case InsiderTipType.TrendReversal:
        return def.DescriptionTemplate;
    ```
  - [x] Keep `CalculateEventCount()` helper (used by EventCount, still active)
  - [x] Keep `ApplyFuzz()` helper (used by price-based tips, still active)
  - [x] Keep `FormatPrice()` helper (used by price-based tips, still active)
  - [x] Update price-based cases (PriceForecast, PriceFloor, PriceCeiling) to also return the raw numeric value — this requires changing the method to output both text and numeric value, OR storing numeric value separately. Preferred approach: change `CalculateRevealedText` to return a tuple or add a `CalculateNumericValue` companion method that the caller uses to populate `RevealedTip.NumericValue`
  - [x] File: `Assets/Scripts/Runtime/Shop/InsiderTipGenerator.cs`

- [x] Task 8: Update `ShopUI` display strings (AC: 8)
  - [x] Open `Assets/Scripts/Runtime/UI/ShopUI.cs`
  - [x] Update `FormatTipTypeName()` (lines 911–925):
    - [x] Remove cases: `TrendDirection → "TREND DIRECTION"`, `EventForecast → "EVENT FORECAST"`, `VolatilityWarning → "VOLATILITY WARNING"`, `OpeningPrice → "OPENING PRICE"`
    - [x] Add cases:
      ```
      DipMarker → "DIP MARKER"
      PeakMarker → "PEAK MARKER"
      ClosingDirection → "CLOSING CALL"
      EventTiming → "EVENT TIMING"
      TrendReversal → "TREND REVERSAL"
      ```
  - [x] Update `GetTipFaceDownHint()` (lines 931–945):
    - [x] Remove cases for 4 removed types
    - [x] Add cases:
      ```
      DipMarker → "When's the best buy?"
      PeakMarker → "When should you sell?"
      ClosingDirection → "Up or down?"
      EventTiming → "When do shakeups hit?"
      TrendReversal → "When does it turn?"
      ```
  - [x] Update any `RevealedText` references to `DisplayText` (matching Task 2 rename)
  - [x] File: `Assets/Scripts/Runtime/UI/ShopUI.cs`

- [x] Task 9: Update `ShopState` tip orchestration (AC: 7)
  - [x] Open `Assets/Scripts/Runtime/Core/GameStates/ShopState.cs`
  - [x] In `OnTipPurchaseRequested()` (lines 332–361):
    - [x] Update `RevealedTip` constructor call (line 341) to pass `NumericValue` if available
    - [x] Update `InsiderTipPurchasedEvent` fields if `RevealedText` renamed to `DisplayText`
  - [x] File: `Assets/Scripts/Runtime/Core/GameStates/ShopState.cs`

- [x] Task 10: Update `TradingHUD` tip display (AC: 7)
  - [x] Open `Assets/Scripts/Runtime/UI/TradingHUD.cs`
  - [x] Update reference from `RevealedText` to `DisplayText` in the tip display loop (lines 314–332)
  - [x] File: `Assets/Scripts/Runtime/UI/TradingHUD.cs`

- [x] Task 11: Update `ShopTransaction` (AC: 7) — N/A: file never referenced RevealedText field directly
  - [x] Open `Assets/Scripts/Runtime/Shop/ShopTransaction.cs`
  - [x] Verified: PurchaseTip() passes RevealedTip as whole struct; no field access to rename
  - [x] File: `Assets/Scripts/Runtime/Shop/ShopTransaction.cs` (no changes needed)

- [x] Task 12: Update `GameEvents.cs` (AC: 7)
  - [x] Open `Assets/Scripts/Runtime/Core/GameEvents.cs`
  - [x] In `InsiderTipPurchasedEvent` (lines 253–259): rename `RevealedText` to `DisplayText`
  - [x] File: `Assets/Scripts/Runtime/Core/GameEvents.cs`

- [x] Task 13: Fix all test files (AC: 9)
  - [x] **InsiderTipGeneratorTests.cs** — 5 changes:
    - [x] Line 22: Change `8` → `9` in `HasEightTipTypes` test (rename test to `HasNineTipTypes`)
    - [x] Lines 43–50: Remove cost assertions for 4 removed types, add 5 new cost assertions
    - [x] Lines 101–104: Update clamp test — max available is now 9, not 8
    - [x] Line 197: Remove `OpeningPrice` from price tip check, or update for new types
    - [x] Add new test: verify new types produce valid revealed text
  - [x] **InsiderTipPurchaseTests.cs** — 8 changes:
    - [x] Lines 45, 108, 120, 189: Replace `TrendDirection` with a kept or new type (e.g., `EventCount` or `DipMarker`)
    - [x] Line 58: Replace `EventForecast` with a new type (e.g., `EventTiming`)
    - [x] Line 169: Replace `VolatilityWarning` with a new type (e.g., `ClosingDirection`)
    - [x] Line 180: Change `8` → `9` in `HasEightValues` test (rename test)
    - [x] Update all `RevealedText` references to `DisplayText`
    - [x] Update constructor calls if signature changes (add `NumericValue` param)
  - [x] **StoreDataModelTests.cs** — 10 changes:
    - [x] Lines 164, 340, 431: Replace `EventForecast` with new type
    - [x] Lines 188, 419: Replace `VolatilityWarning` with new type
    - [x] Update all `RevealedText` references to `DisplayText`
    - [x] Update constructor calls if signature changes
  - [x] **ClickToBuyTests.cs** — N/A: file constructs RevealedTip via constructor args, never accesses field directly
    - [x] Verified: Lines 156, 172 pass text as constructor parameter, no field rename needed
  - [x] **StoreVisualPolishTests.cs** — 4 tests to delete, 5 to add:
    - [x] Delete: `FormatTipTypeName_TrendDirection_*` (line 218), `FormatTipTypeName_EventForecast_*` (line 224), `FormatTipTypeName_VolatilityWarning_*` (line 236), `FormatTipTypeName_OpeningPrice_*` (line 242)
    - [x] Add: `FormatTipTypeName_DipMarker_ReturnsDipMarker`, `FormatTipTypeName_PeakMarker_ReturnsPeakMarker`, `FormatTipTypeName_ClosingDirection_ReturnsClosingCall`, `FormatTipTypeName_EventTiming_ReturnsEventTiming`, `FormatTipTypeName_TrendReversal_ReturnsTrendReversal`
  - [x] **MechanicRelicTests.cs** — 2 changes:
    - [x] Line 294: Replace `TrendDirection` with new type
    - [x] Update `RevealedText` → `DisplayText` in assertions
  - [x] **RunContextStoreTests.cs** — 1 change:
    - [x] Update `RevealedText` → `DisplayText` in tip assertions

- [x] Task 14: Add new data validation tests (AC: 10)
  - [x] Add to `InsiderTipGeneratorTests.cs`:
    - [x] Test: all 9 enum values are unique (expand existing uniqueness test)
    - [x] Test: all 9 definitions exist in `InsiderTipDefinitions.All` — verify count and types
    - [x] Test: every definition's cost matches its `GameConfig` constant
    - [x] Test: no duplicate types in definitions array
    - [x] Test: removed types (`OpeningPrice`, `VolatilityWarning`, `TrendDirection`, `EventForecast`) do NOT exist in enum (compile-time verified, but assert `InsiderTipDefinitions.GetByType()` returns null for invalid int-cast values)
    - [x] Test: `InsiderTipDefinitions.GetByType()` returns correct definition for each of the 9 types
  - [x] Add to `StoreDataModelTests.cs` or new file:
    - [x] Test: `TipOverlayData` default struct has safe values (`PriceLevel == 0`, `TimeZoneCenter == -1`, `ReversalTime == -1`, `DirectionSign == 0`, `EventCountdown == -1`, `TimeMarkers == null`)
    - [x] Test: `RevealedTip` with `NumericValue` stores and retrieves correctly
    - [x] Test: `RevealedTip.IsActivated` defaults to false
  - [x] Add to `RunContextStoreTests.cs`:
    - [x] Test: `ActiveTipOverlays` initialized as empty list
    - [x] Test: `ResetForNewRun()` clears `ActiveTipOverlays`

## Dev Notes

### Architecture Compliance

- **No ScriptableObjects:** All tip data remains as `public static readonly` in `Scripts/Setup/Data/`. No changes to this pattern.
- **Assembly boundary:** `InsiderTipType` enum and `RevealedTip`/`TipOverlayData` structs live in `Scripts/Runtime/Shop/StoreDataTypes.cs` (Runtime assembly). `InsiderTipDefinitions` lives in `Scripts/Setup/Data/` which references Runtime. This is the existing pattern — no boundary violations.
- **EventBus pattern:** `InsiderTipPurchasedEvent` already exists and will be updated (field rename only). No new events in this story.
- **No .meta files:** Only modifying existing `.cs` files. No new files created in this story.
- **Single source of truth:** All costs defined ONCE in `GameConfig`, referenced by `InsiderTipDefinitions`. This pattern is preserved.

### Critical Rename: RevealedText → DisplayText

This is a codebase-wide rename touching 12+ files. Use IDE find-replace or careful grep to catch every reference:
- `RevealedTip.RevealedText` → `RevealedTip.DisplayText`
- `InsiderTipPurchasedEvent.RevealedText` → `InsiderTipPurchasedEvent.DisplayText`
- All local variable assignments and reads that use `.RevealedText`
- String "RevealedText" does NOT appear in any user-facing text — this is purely internal

### RevealedTip Constructor Backward Compatibility

The existing constructor is `RevealedTip(InsiderTipType type, string revealedText)`. Adding `NumericValue` should use a default parameter: `RevealedTip(InsiderTipType type, string displayText, float numericValue = 0f)`. This preserves most existing call sites while allowing price-based tips to pass the extra value. The `IsActivated` field should default to `false` (struct default).

### New Types Are Stubs in This Story

The new tip types (DipMarker, PeakMarker, ClosingDirection, EventTiming, TrendReversal) will return placeholder text from `CalculateRevealedText()` in this story. The actual generation logic and round-start activation are Story 18.2. This story only establishes the data model and cleans up dead code.

### Test Impact Summary

| Test File | Breaking Changes | Fix Strategy |
|-----------|-----------------|--------------|
| InsiderTipGeneratorTests.cs | 5 assertions | Update counts (8→9), swap removed types for new types in cost checks |
| InsiderTipPurchaseTests.cs | 8 constructors | Swap removed InsiderTipType values for kept/new ones, rename field |
| StoreDataModelTests.cs | 10 constructors | Swap removed types, rename field |
| ClickToBuyTests.cs | 2 constructors | Rename field |
| StoreVisualPolishTests.cs | 4 tests to delete, 5 to add | Delete removed type format tests, add new type format tests |
| MechanicRelicTests.cs | 2 constructors | Swap TrendDirection for new type, rename field |
| RunContextStoreTests.cs | 1 constructor | Rename field |
| **Total** | **~31 changes** | |

### Existing Code to Read Before Implementing

Read these files COMPLETELY before making any changes:

1. `Assets/Scripts/Runtime/Shop/StoreDataTypes.cs` — enum, RevealedTip struct (lines 25–50)
2. `Assets/Scripts/Setup/Data/InsiderTipDefinitions.cs` — all definitions (lines 5–65)
3. `Assets/Scripts/Setup/Data/GameConfig.cs` — tip cost constants (lines 44–58)
4. `Assets/Scripts/Runtime/Shop/InsiderTipGenerator.cs` — all generation logic (lines 1–200)
5. `Assets/Scripts/Runtime/UI/ShopUI.cs` — FormatTipTypeName (lines 911–925), GetTipFaceDownHint (lines 931–945), AnimateTipFlip (lines 1441–1506)
6. `Assets/Scripts/Runtime/Core/RunContext.cs` — tip properties (lines 21–22, 136–137, 261–262)
7. `Assets/Scripts/Runtime/Core/GameEvents.cs` — InsiderTipPurchasedEvent (lines 253–259)
8. `Assets/Scripts/Runtime/Core/GameStates/ShopState.cs` — tip generation/purchase (lines 107–117, 332–361)
9. `Assets/Scripts/Runtime/UI/TradingHUD.cs` — tip display (lines 314–332)
10. `Assets/Scripts/Runtime/Shop/ShopTransaction.cs` — PurchaseTip method (lines 190–227)

### Project Structure Notes

- All data definitions: `Scripts/Setup/Data/` — follows existing pattern exactly
- All runtime structs: `Scripts/Runtime/Shop/StoreDataTypes.cs` — existing home for tip types
- All tip tests: `Tests/Runtime/Shop/` — existing test organization
- No new folders or files needed (except possibly a new test for TipOverlayData validation)

### Depends On

- Epic 13 (Store Rework) — complete, established the tip system
- Story 17.6 (Free Intel relic) — FreeIntelThisVisit flag, must not break

### References

- [Source: _bmad-output/planning-artifacts/epic-18-insider-tips-overhaul.md#Story 18.1]
- [Source: _bmad-output/project-context.md#Serialization & Data]
- [Source: _bmad-output/project-context.md#Testing Rules]
- [Source: _bmad-output/implementation-artifacts/13-5-insider-tips.md] (original tip system story)

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Debug Log References

N/A — data model refactor, no runtime debugging expected.

### Completion Notes List

- Removed 4 underperforming InsiderTipType enum values (TrendDirection, EventForecast, VolatilityWarning, OpeningPrice) and added 5 new actionable types (DipMarker, PeakMarker, ClosingDirection, EventTiming, TrendReversal) — total 9 types
- Extended RevealedTip struct with NumericValue (stores raw fuzzed price for price-based tips) and IsActivated flag (for Story 18.2 round-start activation)
- Renamed RevealedTip.RevealedText → DisplayText and InsiderTipPurchasedEvent.RevealedText → DisplayText across all consuming code
- Created TipOverlayData struct with overlay geometry fields for all chart overlay types (horizontal lines, bands, time zones, markers, direction arrows, event counters)
- Added ActiveTipOverlays list to RunContext (initialized empty, cleared on ResetForNewRun)
- Updated GameConfig with 5 new tip cost constants at Low/Medium/High Rep tiers, removed 4 old constants
- Updated InsiderTipDefinitions with 9 definitions (4 kept, 5 new) with description templates and GameConfig costs
- Updated InsiderTipGenerator: removed 4 dead switch branches and 3 helper methods (ClassifyTrendDirection, ClassifyEventForecast, ClassifyVolatility), added 5 stub branches for new types, changed CalculateRevealedText to return tuple (text, numericValue) for price-based tips
- Updated ShopUI FormatTipTypeName and GetTipFaceDownHint with 5 new display strings
- Updated ShopState, GameEvents, TradingHUD for DisplayText rename
- Fixed all 7 test files: updated enum counts (8→9), swapped removed type references for new types, renamed RevealedText→DisplayText
- Added 10 new data validation tests across InsiderTipGeneratorTests, StoreDataModelTests, and RunContextStoreTests

### File List

- Assets/Scripts/Runtime/Shop/StoreDataTypes.cs (modified — enum, RevealedTip struct, new TipOverlayData struct)
- Assets/Scripts/Setup/Data/GameConfig.cs (modified — tip cost constants)
- Assets/Scripts/Setup/Data/InsiderTipDefinitions.cs (modified — 9 definitions)
- Assets/Scripts/Runtime/Shop/InsiderTipGenerator.cs (modified — removed dead code, added stubs, NumericValue support)
- Assets/Scripts/Runtime/UI/ShopUI.cs (modified — FormatTipTypeName, GetTipFaceDownHint)
- Assets/Scripts/Runtime/Core/RunContext.cs (modified — ActiveTipOverlays property)
- Assets/Scripts/Runtime/Core/GameEvents.cs (modified — InsiderTipPurchasedEvent.DisplayText rename)
- Assets/Scripts/Runtime/Core/GameStates/ShopState.cs (modified — RevealedTip constructor, event field rename)
- Assets/Scripts/Runtime/UI/TradingHUD.cs (modified — DisplayText rename)
- Assets/Tests/Runtime/Shop/InsiderTipGeneratorTests.cs (modified — enum counts, cost assertions, 6 new tests)
- Assets/Tests/Runtime/Shop/InsiderTipPurchaseTests.cs (modified — swapped removed types, DisplayText rename)
- Assets/Tests/Runtime/Shop/StoreDataModelTests.cs (modified — swapped removed types, DisplayText rename, 4 new tests)
- Assets/Tests/Runtime/Shop/StoreVisualPolishTests.cs (modified — deleted 4 old format tests, added 5 new)
- Assets/Tests/Runtime/Items/Relics/MechanicRelicTests.cs (modified — swapped TrendDirection reference)
- Assets/Tests/Runtime/Core/RunContextStoreTests.cs (modified — 3 new ActiveTipOverlays tests)

### Change Log

- 2026-02-21: Story 18.1 created — comprehensive implementation guide for tip data model overhaul
- 2026-02-21: Story 18.1 implemented — tip type enum overhaul (4 removed, 5 added), RevealedTip extended with NumericValue/IsActivated, TipOverlayData struct created, RevealedText→DisplayText rename, all tests updated + 10 new validation tests
- 2026-02-21: Code review fixes — (H1) Added TipOverlayData.CreateDefault() with correct sentinel values (-1 for TimeZoneCenter, ReversalTime, EventCountdown), fixed test to validate sentinels; (M1) Completed RevealedText→DisplayText rename in TipOffering struct and all 15 remaining references; (M2) Clarified Tasks 11/13 as N/A (files never referenced renamed field)
