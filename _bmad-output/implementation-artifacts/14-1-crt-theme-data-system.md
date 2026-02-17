# Story 14.1: CRT Theme Data System

Status: ready-for-dev

## Story

As a developer,
I want a centralized CRT theme data class with all Terminal 1999 colors and a DashboardReferences struct,
so that all subsequent UI stories share consistent colors and GameRunner has clean access to UI elements without GameObject.Find.

## Acceptance Criteria

1. New `CRTThemeData.cs` exists in `Assets/Scripts/Setup/Data/` with all Terminal 1999 color constants as `public static readonly Color` fields
2. New `DashboardReferences.cs` exists in `Assets/Scripts/Runtime/UI/` containing public fields for all dashboard UI elements
3. Helper method `CRTThemeData.ApplyLabelStyle(Text text, bool highlight)` correctly applies CRT font styling (highlight = phosphor green TextHigh, dim = TextLow)
4. Helper method `CRTThemeData.ApplyPanelStyle(Image image)` applies Panel color with Border color outline pattern
5. All color values match the specified hex values within Unity Color float precision
6. No existing code is modified — this story is additive (new files only)
7. Both new files compile without errors alongside existing codebase
8. Edit Mode tests validate all color values and helper method outputs

## Tasks / Subtasks

- [ ] Task 1: Create `CRTThemeData.cs` (AC: 1, 5)
  - [ ] 1.1: Define all color constants with correct hex-to-Color conversions
  - [ ] 1.2: Add `ApplyLabelStyle(Text, bool)` helper method
  - [ ] 1.3: Add `ApplyPanelStyle(Image)` helper method
  - [ ] 1.4: Add button color constants (Buy green, Sell red, Short amber)
- [ ] Task 2: Create `DashboardReferences.cs` (AC: 2)
  - [ ] 2.1: Define public fields for Left Wing text elements (Cash, Profit, Target)
  - [ ] 2.2: Define public fields for Center Core elements (BuyButton, SellButton, ShortButton, CooldownOverlay)
  - [ ] 2.3: Define public fields for Right Wing text elements (Direction, AvgPrice, PnL, Timer, Rep)
  - [ ] 2.4: Define public fields for container transforms (LeftWing, CenterCore, RightWing, ControlDeckPanel)
  - [ ] 2.5: Define public fields for event ticker and stock labels
- [ ] Task 3: Write Edit Mode tests (AC: 5, 8)
  - [ ] 3.1: Test all color values match expected hex conversions
  - [ ] 3.2: Test ApplyLabelStyle sets correct colors
  - [ ] 3.3: Test ApplyPanelStyle sets correct color
- [ ] Task 4: Verify compilation (AC: 6, 7)
  - [ ] 4.1: Ensure no existing files are modified
  - [ ] 4.2: Run full test suite to confirm zero regressions

## Dev Notes

### Architecture Compliance

- **Data class pattern:** Follow existing `Scripts/Setup/Data/` convention — `public static class` with `public static readonly` fields. See `GameConfig.cs`, `TierVisualData.cs` for examples.
- **No ScriptableObjects:** Per project-context.md — all data as code constants.
- **No Inspector configuration:** Per Setup-Oriented framework rules.
- **Color conversion:** Unity `Color` uses 0-1 float range. Convert hex manually:
  - `#050a0a` → `new Color(5/255f, 10/255f, 10/255f, 1f)` = `new Color(0.020f, 0.039f, 0.039f, 1f)`
  - `#061818` → `new Color(6/255f, 24/255f, 24/255f, 0.9f)` (90% alpha)
  - `#28f58d` → `new Color(40/255f, 245/255f, 141/255f, 1f)` = `new Color(0.157f, 0.961f, 0.553f, 1f)`
  - `#3b6e6e` → `new Color(59/255f, 110/255f, 110/255f, 1f)` = `new Color(0.231f, 0.431f, 0.431f, 1f)`
  - `#ffb800` → `new Color(1f, 184/255f, 0f, 1f)` = `new Color(1f, 0.722f, 0f, 1f)`
  - `#ff4444` → `new Color(1f, 68/255f, 68/255f, 1f)` = `new Color(1f, 0.267f, 0.267f, 1f)`
  - `#224444` → `new Color(34/255f, 68/255f, 68/255f, 1f)` = `new Color(0.133f, 0.267f, 0.267f, 1f)`

### DashboardReferences Design

- This is a plain C# class (NOT a MonoBehaviour) — it's a data container returned by `UISetup.ExecuteControlDeck()`.
- References are `public` fields (not properties) for simplicity — follows UISetup pattern of direct field assignment.
- Uses `UnityEngine.UI.Text` and `UnityEngine.UI.Image` and `UnityEngine.UI.Button` types.
- The struct/class will be populated by UISetup in Story 14.2 — for now it just defines the shape.

### Existing Color Constants to Eventually Replace (Story 14.6)

These are the scattered color constants across the codebase that will be consolidated to CRTThemeData in later stories:
- `UISetup.BarBackgroundColor` = `new Color(0.05f, 0.07f, 0.18f, 0.9f)`
- `UISetup.LabelColor` = `new Color(0.6f, 0.6f, 0.7f, 1f)`
- `UISetup.ValueColor` = `Color.white`
- `UISetup.NeonGreen` = `new Color(0f, 1f, 0.533f, 1f)`
- `TradingHUD.ProfitGreen` = `new Color(0f, 1f, 0.533f, 1f)`
- `TradingHUD.LossRed` = `new Color(1f, 0.2f, 0.2f, 1f)`
- `TradingHUD.WarningYellow` = `new Color(1f, 0.85f, 0.2f, 1f)`
- `PositionOverlay.LongColor` = `new Color(0f, 1f, 0.533f, 1f)`
- `PositionOverlay.ShortColor` = `new Color(1f, 0.4f, 0.7f, 1f)`
- `ChartSetup.BackgroundColor` = `new Color(0.039f, 0.055f, 0.153f, 1f)`

Do NOT modify these in this story — just define the CRT equivalents. Story 14.6 handles the migration.

### Testing Approach

- Edit Mode tests only (no MonoBehaviour dependency needed).
- Test file: `Assets/Tests/Runtime/UI/CRTThemeDataTests.cs`
- Test color values by comparing `CRTThemeData.TextHigh.r` etc. to expected float values within `0.01f` tolerance.
- Test helper methods by creating temporary Text/Image GameObjects in test setup.
- Run tests via CLI: `"D:/UnityHub/Editor/6000.3.4f1/Editor/Unity.exe" -runTests -batchmode -nographics -projectPath "E:/BullRun" -testPlatform EditMode -testResults "E:/BullRun/TestResults.xml"`

### Project Structure Notes

- `Assets/Scripts/Setup/Data/CRTThemeData.cs` — follows existing pattern (`GameConfig.cs`, `TierVisualData.cs`, `EventDefinitions.cs`)
- `Assets/Scripts/Runtime/UI/DashboardReferences.cs` — new file in existing UI folder alongside `TradingHUD.cs`, `PositionOverlay.cs`
- `Assets/Tests/Runtime/UI/CRTThemeDataTests.cs` — follows test mirror structure

### References

- [Source: _bmad-output/planning-artifacts/epic-14-terminal-1999-ui.md#Story 14.1]
- [Source: _bmad-output/project-context.md#Technology Stack & Versions] — uGUI 2.0.0, Unity 6.3 LTS
- [Source: _bmad-output/project-context.md#Code Organization Rules] — data classes in `Scripts/Setup/Data/`, UI panels in `Scripts/Runtime/UI/`
- [Source: _bmad-output/planning-artifacts/game-architecture.md#Data Architecture] — pure C# static data classes pattern
- [Source: Assets/Scripts/Setup/Data/TierVisualData.cs] — existing theme struct pattern (TierVisualTheme)
- [Source: Assets/Scripts/Setup/Data/GameConfig.cs] — existing static data class pattern
- [Source: Assets/Scripts/Runtime/UI/TradingHUD.cs] — existing color constants (ProfitGreen, LossRed)
- [Source: Assets/Scripts/Runtime/UI/PositionOverlay.cs] — existing color constants (LongColor, ShortColor)
- [Source: Assets/Scripts/Setup/UISetup.cs:16-24] — existing color constants (BarBackgroundColor, LabelColor, etc.)

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
