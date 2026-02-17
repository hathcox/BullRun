# Story 14.2: Control Deck Layout Shell

Status: ready-for-dev

## Story

As a developer,
I want the bottom-docked Control Deck panel structure with three empty columns,
so that subsequent stories can populate the Wallet, Actions, and Stats wings.

## Acceptance Criteria

1. New method `UISetup.ExecuteControlDeck()` creates the full Control Deck panel hierarchy and returns a populated `DashboardReferences`
2. `Control_Deck_Panel` is bottom-center anchored with `HorizontalLayoutGroup` (Padding: 10, Spacing: 20)
3. Three child containers exist: `Left_Wing` (VerticalLayoutGroup), `Center_Core` (VerticalLayoutGroup), `Right_Wing` (VerticalLayoutGroup)
4. Panel background uses `CRTThemeData.Panel` color; border outline uses `CRTThemeData.Border` color
5. Control Deck spans ~90% of screen width, height ~160px, anchored to bottom-center (anchorMin 0.05,0 / anchorMax 0.95,0 / pivot 0.5,0)
6. `DashboardReferences` is returned with container transforms (LeftWing, CenterCore, RightWing, ControlDeckPanel) populated
7. Old top bar creation in `UISetup.Execute(RunContext, int, float)` is removed (Cash/Profit/Target/Rep/Timer sections deleted)
8. `TradingHUD.Initialize()` signature updated to accept `DashboardReferences` instead of individual Text fields (backward-compatible: old fields still work if not null)
9. Canvas sorting order is 20 (between chart at 10 and feedback at 23)
10. No regressions — existing TradingHUD update logic continues to compile and function

## Tasks / Subtasks

- [ ] Task 1: Create `UISetup.ExecuteControlDeck()` method (AC: 1, 2, 3, 5, 9)
  - [ ] 1.1: Create ControlDeckCanvas with ScreenSpaceOverlay, sortingOrder=20, CanvasScaler 1920x1080
  - [ ] 1.2: Create `Control_Deck_Panel` with bottom-center anchoring (anchorMin 0.05,0 / anchorMax 0.95,0 / pivot 0.5,0), height 160px
  - [ ] 1.3: Add HorizontalLayoutGroup to Control_Deck_Panel (padding 10, spacing 20)
  - [ ] 1.4: Apply `CRTThemeData.Panel` background color and `CRTThemeData.Border` border styling
  - [ ] 1.5: Create `Left_Wing` container with VerticalLayoutGroup
  - [ ] 1.6: Create `Center_Core` container with VerticalLayoutGroup
  - [ ] 1.7: Create `Right_Wing` container with VerticalLayoutGroup
  - [ ] 1.8: Populate and return `DashboardReferences` with container transforms
- [ ] Task 2: Remove old top bar code from `UISetup.Execute(RunContext, int, float)` (AC: 7)
  - [ ] 2.1: Remove top bar panel creation (TopBar, HorizontalLayoutGroup, sections 1-6)
  - [ ] 2.2: Remove Cash/Portfolio/Profit/Target/Rep/Timer section creation
  - [ ] 2.3: Keep EventSystem creation and TradingHUD/RoundTimerUI initialization (rewire to new refs)
  - [ ] 2.4: Call `ExecuteControlDeck()` instead, pass DashboardReferences to TradingHUD and RoundTimerUI
- [ ] Task 3: Update `TradingHUD.Initialize()` (AC: 8, 10)
  - [ ] 3.1: Add overload or modify `Initialize()` to accept `DashboardReferences`
  - [ ] 3.2: Extract text references from DashboardReferences into existing private fields
  - [ ] 3.3: Ensure all existing RefreshDisplay logic works unchanged
- [ ] Task 4: Update `DashboardReferences.cs` with container fields (AC: 6)
  - [ ] 4.1: Add `Transform LeftWing`, `Transform CenterCore`, `Transform RightWing`, `Transform ControlDeckPanel` fields
  - [ ] 4.2: Add `Canvas ControlDeckCanvas` field for sorting order management
- [ ] Task 5: Verify compilation and visual layout (AC: 10)
  - [ ] 5.1: Confirm no compile errors across all modified files
  - [ ] 5.2: Verify Control Deck panel is visible at bottom of screen during trading phase
  - [ ] 5.3: Verify old top bar is fully removed

## Dev Notes

### Architecture Compliance

- **UISetup pattern:** Follow existing `ExecuteTradePanel()` / `ExecutePositionOverlay()` pattern — static method that creates Canvas + hierarchy, returns a component or data object.
- **No ScriptableObjects/Inspector configuration** per project rules.
- **DashboardReferences** is a plain C# class (created in 14.1), not a MonoBehaviour.

### Old Top Bar Code to Remove (UISetup.cs:50-141)

The entire body of `Execute(RunContext, int, float)` after the EventSystem check creates:
- `TradingHUD` parent GameObject
- `HUDCanvas` (Canvas, sortingOrder 20)
- `TopBar` panel with HorizontalLayoutGroup
- CashSection, PortfolioSection, ProfitSection, TargetSection, ReputationSection, TimerSection
- RoundTimerUI initialization
- TradingHUD initialization with individual text references

All of this is replaced by `ExecuteControlDeck()`. The EventSystem creation (lines 43-48) must be preserved.

### New Execute(RunContext, int, float) Flow

After this story, the method should:
1. Ensure EventSystem exists (keep existing code)
2. Call `ExecuteControlDeck()` → get `DashboardReferences`
3. Create `TradingHUD` parent, add `TradingHUD` component, call `Initialize(dashRefs, runContext, currentRound, roundDuration)`
4. Create `RoundTimerUI` component, call `Initialize()` with timer text/progress from `DashboardReferences`
5. Return or store `DashboardReferences` for GameRunner

### Control Deck Layout Structure

```
ControlDeckCanvas (sortingOrder=20)
└── Control_Deck_Panel (bottom-center, 90% width, 160px height)
    ├── Border_Image (CRTThemeData.Border, 1-2px outline effect)
    ├── Background_Image (CRTThemeData.Panel)
    └── HorizontalLayoutGroup (padding=10, spacing=20)
        ├── Left_Wing (VerticalLayoutGroup) — ~30% width via LayoutElement
        ├── Center_Core (VerticalLayoutGroup) — ~40% width via LayoutElement
        └── Right_Wing (VerticalLayoutGroup) — ~30% width via LayoutElement
```

### Border Implementation Strategy

Use nested panel approach: outer Image with `CRTThemeData.Border` color, inner Image with `CRTThemeData.Panel` color, offset by 1-2px. This avoids needing Outline components or custom shaders.

### Canvas Sorting Order Reference

| Canvas | Sorting Order | Purpose |
|--------|--------------|---------|
| ChartCanvas | 10 | Chart labels, price axis |
| ControlDeckCanvas | 20 | Bottom dashboard (replaces HUDCanvas) |
| FeedbackCanvas | 23 | Trade feedback popups |
| TradePanelCanvas | 24 | Buy/Sell/Short buttons |
| TimerCanvas | 25 | Round timer |

### GameRunner Integration Impact

`GameRunner.Start()` currently calls:
- `UISetup.Execute(_ctx, _ctx.CurrentRound, GameConfig.RoundDurationSeconds)` — this call changes to also receive/return DashboardReferences
- The TradingHUD and RoundTimerUI references that GameRunner uses indirectly should still work via the same event-driven pattern.

### Existing Color Constants (Still Active)

The old color constants (`BarBackgroundColor`, `LabelColor`, `ValueColor`, `NeonGreen`) in UISetup.cs are NOT removed in this story — they're still used by other methods (ExecuteSidebar, ExecuteMarketOpenUI, etc.). Story 14.6 migrates all constants to CRTThemeData.

### Testing Approach

- Visual testing: Run game, verify Control Deck panel appears at bottom with three empty columns and CRT theme colors.
- Compile verification: All existing code must still compile. TradingHUD, RoundTimerUI, PositionOverlay must still function.
- Layout verification: Columns should have roughly 30%/40%/30% width split.

### References

- [Source: _bmad-output/planning-artifacts/epic-14-terminal-1999-ui.md#Story 14.2]
- [Source: Assets/Scripts/Setup/UISetup.cs:14-24] — existing color constants
- [Source: Assets/Scripts/Setup/UISetup.cs:40-141] — current Execute(RunContext, int, float) to be replaced
- [Source: Assets/Scripts/Runtime/UI/TradingHUD.cs:41-63] — current Initialize() signature
- [Source: Assets/Scripts/Runtime/UI/RoundTimerUI.cs:29-39] — current Initialize() signature
- [Source: Assets/Scripts/Runtime/UI/DashboardReferences.cs] — container fields to add
- [Source: Assets/Scripts/Setup/Data/CRTThemeData.cs] — Panel and Border color constants

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
