# Story 17.10: Relic Icons

Status: done

## Story

As a player,
I want each relic to have a distinctive text icon with color coding,
so that I can identify relics at a glance in the shop, owned bar, and trading HUD.

## Acceptance Criteria

1. `RelicDef` struct gains `string IconChar` and `string IconColorHex` fields (hex color string, e.g., `"#FFB000"`)
2. All 23 relics in `ShopItemDefinitions.RelicPool` have assigned icon characters and hex color codes
3. Icons rendered via TextMeshPro (or legacy Text) using the existing monospace terminal font — no new font imports needed
4. Icons are legible at small sizes (40x40 in trading HUD RelicBar, 60x60 in shop cards and owned relics bar)
5. Color coding follows relic category feel: trade relics = green, event relics = amber, economy relics = gold, mechanic relics = cyan, special relics = magenta
6. Icons display in all three relic display locations: shop relic cards, shop owned relics bar, and trading phase RelicBar
7. A runtime helper method parses `IconColorHex` string to `UnityEngine.Color` for rendering (keeps `RelicDef` in Setup/Data without `UnityEngine.Color` dependency)

## Tasks / Subtasks

- [x] Task 1: Extend RelicDef struct with icon fields (AC: 1)
  - [x] Add `string IconChar` field to `RelicDef` struct
  - [x] Add `string IconColorHex` field to `RelicDef` struct (hex format: `"#RRGGBB"`)
  - [x] Update `RelicDef` constructor to accept `iconChar` and `iconColorHex` parameters
  - [x] File: `Scripts/Setup/Data/ShopItemDefinitions.cs`

- [x] Task 2: Assign icons and colors to all 23 relics (AC: 2, 5)
  - [x] Update every `RelicDef` entry in `ShopItemDefinitions.RelicPool` with `IconChar` and `IconColorHex`
  - [x] Icon and color assignments:
    - Catalyst Trader: `"!"`, `"#FFB000"` (Amber)
    - Bear Raid: `"III"`, `"#00FF41"` (Green)
    - Market Manipulator: `"V"`, `"#00FF41"` (Green)
    - Double Dealer: `"x2"`, `"#00FF41"` (Green)
    - Quick Draw: `">>"`, `"#00FF41"` (Green)
    - Event Storm: `"**"`, `"#FFB000"` (Amber)
    - Loss Liquidator: `"-!"`, `"#FFB000"` (Amber)
    - Profit Refresh: `"+R"`, `"#FFB000"` (Amber)
    - Bull Believer: `"^^"`, `"#FFB000"` (Amber)
    - Time Buyer: `"+T"`, `"#00FFFF"` (Cyan)
    - Diamond Hands: `"<>"`, `"#00FFFF"` (Cyan)
    - Rep Doubler: `"R2"`, `"#FFD700"` (Gold)
    - Fail Forward: `"FF"`, `"#FFD700"` (Gold)
    - Bond Bonus: `"B+"`, `"#FFD700"` (Gold)
    - Free Intel: `"?F"`, `"#00FFFF"` (Cyan)
    - Extra Expansion: `"E+"`, `"#00FFFF"` (Cyan)
    - Compound Rep: `"$$"`, `"#FFD700"` (Gold)
    - Skimmer: `"%B"`, `"#00FF41"` (Green)
    - Short Profiteer: `"%S"`, `"#00FF41"` (Green)
    - Relic Expansion: `"[+]"`, `"#FF00FF"` (Magenta)
    - Event Catalyst: `"R!"`, `"#FF00FF"` (Magenta)
    - Rep Interest: `"R%"`, `"#FFD700"` (Gold)
    - Rep Dividend: `"R$"`, `"#FFD700"` (Gold)
  - [x] Verify hex colors vs the CRT theme palette — intentionally chose brighter neon hex values (`#00FF41`, `#FFB000`, `#FFD700`, `#00FFFF`, `#FF00FF`) over muted `ColorPalette` values for icon text legibility at small sizes
  - [x] File: `Scripts/Setup/Data/ShopItemDefinitions.cs`

- [x] Task 3: Create runtime color parsing helper (AC: 7)
  - [x] Add `RelicIconHelper` static class in `Scripts/Runtime/UI/RelicIconHelper.cs`
  - [x] Method: `static Color ParseHexColor(string hex)` — parses `"#RRGGBB"` to `UnityEngine.Color` using `ColorUtility.TryParseHtmlString`
  - [x] Method: `static Color GetIconColor(RelicDef def)` — convenience wrapper that parses `def.IconColorHex`
  - [x] Fallback: if parse fails, return `Color.white` and log a warning
  - [x] This keeps `RelicDef` in `Scripts/Setup/Data/` free of `UnityEngine.Color` references while allowing runtime code to use proper Color values
  - [x] File: `Scripts/Runtime/UI/RelicIconHelper.cs`

- [x] Task 4: Render icons on shop relic cards (AC: 3, 4, 6)
  - [x] In UISetup, add an icon Text element to each relic card slot (positioned at left side or top-left of the card)
  - [x] Icon Text element: font size suitable for ~60x60 display area, bold, monospace terminal font
  - [x] Extend `RelicSlotView` struct (in ShopUI) with `Text IconLabel` field
  - [x] In ShopUI relic card population: set `IconLabel.text = relicDef.IconChar`, set `IconLabel.color = RelicIconHelper.GetIconColor(relicDef)`
  - [x] When card is empty/sold: hide or clear the icon label
  - [x] Files: `Scripts/Setup/UISetup.cs`, `Scripts/Runtime/UI/ShopUI.cs`

- [x] Task 5: Render icons in shop owned relics bar (AC: 3, 4, 6)
  - [x] In UISetup, add an icon Text element to each owned relic slot (positioned at left side or above the name label)
  - [x] Extend `OwnedRelicSlotView` struct (in ShopUI) with `Text IconLabel` field
  - [x] In `RefreshOwnedRelicsBar()`: for populated slots, set `IconLabel.text = relicDef.IconChar`, set `IconLabel.color = RelicIconHelper.GetIconColor(relicDef)`
  - [x] For empty slots: hide the icon label (`SetActive(false)`)
  - [x] Icon should be legible at owned bar slot size (~60x60 area)
  - [x] Files: `Scripts/Setup/UISetup.cs`, `Scripts/Runtime/UI/ShopUI.cs`

- [x] Task 6: Render icons in trading phase RelicBar (AC: 3, 4, 6)
  - [x] In RelicBar (from Story 17.8), update relic badge rendering to show the icon character
  - [x] Each relic badge should display `IconChar` text colored with the parsed `IconColorHex`
  - [x] Icons must be legible at 40x40 size (smaller than shop) — use appropriate font size
  - [x] Look up `RelicDef` via `ItemLookup.GetRelicById()` to get icon data
  - [x] File: `Scripts/Runtime/UI/RelicBar.cs`

- [x] Task 7: Update existing tests and add icon data validation tests (AC: 1, 2, 5)
  - [x] Test: all 23 relics in `RelicPool` have non-null, non-empty `IconChar`
  - [x] Test: all 23 relics in `RelicPool` have valid `IconColorHex` (parseable by `ColorUtility.TryParseHtmlString`)
  - [x] Test: `RelicIconHelper.ParseHexColor` returns correct Color for known hex values
  - [x] Test: `RelicIconHelper.ParseHexColor` returns `Color.white` for invalid/empty input
  - [x] Test: no two relics share the same `IconChar` (uniqueness validation)
  - [x] Test: color categories are consistent (all green-tinted relics use the same hex, etc.)
  - [x] Update any existing RelicDef tests that construct `RelicDef` with the old constructor signature
  - [x] Files: `Tests/Runtime/Items/RelicIconTests.cs`, update `Tests/Runtime/Shop/ShopGeneratorTests.cs` if constructor changed

## Dev Notes

### Architecture Compliance

- **No ScriptableObjects:** Icon data lives as fields on the `public static readonly RelicDef` struct in `Scripts/Setup/Data/ShopItemDefinitions.cs`. Follows existing data pattern exactly.
- **Assembly boundary compliance:** `RelicDef` is in `Scripts/Setup/Data/` which may be referenced by both Setup and Runtime assemblies. Using `string` for hex color (not `UnityEngine.Color`) avoids any assembly dependency issues. The runtime helper in `Scripts/Runtime/UI/` handles the conversion.
- **Programmatic uGUI:** All icon Text elements created in `UISetup.cs`. No prefabs, no Inspector configuration.
- **Existing font reuse:** Uses the project's existing monospace terminal font (already loaded in UISetup). No new font assets imported.
- **No .meta file changes:** Only modifying existing `.cs` files and creating one new `.cs` file. Unity auto-generates the `.meta` for the new file.

### Known Limitation: Proportional Font

AC 3 specifies "monospace terminal font" but the project uses Unity's built-in `LegacyRuntime.ttf` (proportional) for all text. Multi-character icons like `III`, `x2`, `>>`, `[+]` render with variable widths. This is a pre-existing project-wide pattern — no custom monospace font has been imported. Importing a monospace font is out of scope for this story but would improve icon consistency.

### Icon Design Rationale

- **Text icons over sprite icons:** The project uses a CRT terminal aesthetic. Multi-character text glyphs (like `x2`, `>>`, `[+]`) fit the terminal theme better than traditional sprite icons. They also avoid the need for any art pipeline.
- **Color by category:** Grouping colors by relic function (trade=green, event=amber, economy=gold, mechanic=cyan, special=magenta) creates instant visual categorization. Players can scan the relic bar and know the mix of relic types in their build.
- **Hex string pattern:** Using `"#RRGGBB"` strings in the data definition means `RelicDef` stays as a plain C# struct with no Unity type dependencies. This is the safest approach for the Setup/Data assembly boundary.

### Full Icon Assignment Table

| # | Relic | IconChar | ColorHex | Category |
|---|-------|----------|----------|----------|
| 1 | Catalyst Trader | `!` | `#FFB000` | Event (Amber) |
| 2 | Bear Raid | `III` | `#00FF41` | Trade (Green) |
| 3 | Market Manipulator | `V` | `#00FF41` | Trade (Green) |
| 4 | Double Dealer | `x2` | `#00FF41` | Trade (Green) |
| 5 | Quick Draw | `>>` | `#00FF41` | Trade (Green) |
| 6 | Event Storm | `**` | `#FFB000` | Event (Amber) |
| 7 | Loss Liquidator | `-!` | `#FFB000` | Event (Amber) |
| 8 | Profit Refresh | `+R` | `#FFB000` | Event (Amber) |
| 9 | Bull Believer | `^^` | `#FFB000` | Event (Amber) |
| 10 | Time Buyer | `+T` | `#00FFFF` | Mechanic (Cyan) |
| 11 | Diamond Hands | `<>` | `#00FFFF` | Mechanic (Cyan) |
| 12 | Rep Doubler | `R2` | `#FFD700` | Economy (Gold) |
| 13 | Fail Forward | `FF` | `#FFD700` | Economy (Gold) |
| 14 | Bond Bonus | `B+` | `#FFD700` | Economy (Gold) |
| 15 | Free Intel | `?F` | `#00FFFF` | Mechanic (Cyan) |
| 16 | Extra Expansion | `E+` | `#00FFFF` | Mechanic (Cyan) |
| 17 | Compound Rep | `$$` | `#FFD700` | Economy (Gold) |
| 18 | Skimmer | `%B` | `#00FF41` | Trade (Green) |
| 19 | Short Profiteer | `%S` | `#00FF41` | Trade (Green) |
| 20 | Relic Expansion | `[+]` | `#FF00FF` | Special (Magenta) |
| 21 | Event Catalyst | `R!` | `#FF00FF` | Special (Magenta) |
| 22 | Rep Interest | `R%` | `#FFD700` | Economy (Gold) |
| 23 | Rep Dividend | `R$` | `#FFD700` | Economy (Gold) |

### Existing Code to Read Before Implementing

- `Scripts/Setup/Data/ShopItemDefinitions.cs` — `RelicDef` struct (line ~1-19), `RelicPool` array with all 23 relic definitions (from Story 17.2)
- `Scripts/Runtime/UI/ShopUI.cs` — `RelicSlotView` struct, `OwnedRelicSlotView` struct (line ~205), `RefreshOwnedRelicsBar()` (line ~1816), relic card population logic
- `Scripts/Runtime/UI/RelicBar.cs` — trading phase relic display (from Story 17.8), badge creation and population
- `Scripts/Setup/UISetup.cs` — relic card construction in `ExecuteStoreUI()`, owned relic slot construction in `CreateOwnedRelicSlot()`
- `Scripts/Runtime/Items/ItemLookup.cs` — `GetRelicById(string id)` for looking up RelicDef at runtime

### Depends On

- Story 17.2 (Shop Behavior & Data Overhaul) — all 23 relic definitions must exist in `ShopItemDefinitions.RelicPool` with the current `RelicDef` struct
- Story 17.8 (Trading Phase Relic Display) — `RelicBar.cs` must exist for trading phase icon rendering

### References

- [Source: _bmad-output/planning-artifacts/epic-17-relic-system.md#Story 17.10]
- [Source: _bmad-output/implementation-artifacts/17-2-shop-behavior-and-data-overhaul.md]
- [Source: _bmad-output/implementation-artifacts/13-10-owned-relics-bar-and-click-to-buy.md]
- [Source: _bmad-output/project-context.md#Serialization & Data]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Debug Log References

N/A — no runtime debugging required for this data/UI story.

### Completion Notes List

- Task 1: Extended `RelicDef` struct with `IconChar` and `IconColorHex` string fields. Constructor uses default params so all existing callsites (7 test files) compile without changes.
- Task 2: Assigned all 23 relics unique icon characters and hex color codes matching the category color scheme (green/amber/gold/cyan/magenta). Hex values are CRT-authentic bright neon colors distinct from the UI palette.
- Task 3: Created `RelicIconHelper` static class with `ParseHexColor` and `GetIconColor` methods. Falls back to `Color.white` with warning on parse failure. Keeps `RelicDef` free of UnityEngine.Color dependency.
- Task 4: Added `IconLabel` to `RelicSlotView`, created icon Text element in UISetup `CreateRelicSlot` (fontSize 22, bold), wired in `SetupRelicSlot`/`SetupSoldOutRelicSlot`.
- Task 5: Added `IconLabel` to `OwnedRelicSlotView`, created icon Text element in UISetup `CreateOwnedRelicSlot` (fontSize 14, bold), wired in `RefreshOwnedRelicsBar` for populated/empty states.
- Task 6: Updated `RelicBar.CreateIconSlot` to use `IconChar` and parsed `IconColorHex` instead of name-based 2-char fallback. Font size reduced to 16 for 40x40 legibility. Fallback to `GetRelicIconChar` preserved for relics without icon data.
- Task 7: Created `RelicIconTests.cs` with 16 tests covering: all relics have non-empty IconChar, valid hex parsing, unique icons, category color consistency (5 category tests), RelicIconHelper parse/fallback behavior. No changes needed to existing test files (default constructor params).

### File List

- `Scripts/Setup/Data/ShopItemDefinitions.cs` — Modified: added IconChar/IconColorHex fields to RelicDef (now required params), assigned values to all 23 relics
- `Scripts/Runtime/UI/RelicIconHelper.cs` — New: static helper for hex-to-Color parsing
- `Scripts/Runtime/UI/ShopUI.cs` — Modified: added IconLabel to RelicSlotView and OwnedRelicSlotView structs, icon rendering in SetupRelicSlot/SetupSoldOutRelicSlot/RefreshOwnedRelicsBar. Also includes Story 17.9 relic reordering code (InsertionIndicator field, selection state, reorder logic, escape key handling, reorder click wiring)
- `Scripts/Setup/UISetup.cs` — Modified: added icon Text elements to CreateRelicSlot and CreateOwnedRelicSlot. Also includes Story 17.9 insertion indicators and "Relics execute left to right" reminder label
- `Scripts/Runtime/UI/RelicBar.cs` — Modified: CreateIconSlot uses IconChar/IconColorHex, font size 16
- `Tests/Runtime/Items/RelicIconTests.cs` — New: 17 tests for icon data validation, category coverage, and RelicIconHelper
- `Tests/Runtime/Shop/StoreDataModelTests.cs` — Modified: updated RelicDef constructor call (review fix — required icon params)
- `Tests/Runtime/Shop/ShopTransactionTests.cs` — Modified: updated RelicDef constructor call (review fix)
- `Tests/Runtime/Shop/RelicPurchaseTests.cs` — Modified: updated RelicDef constructor call (review fix)
- `Tests/Runtime/Shop/ExpansionEffectsTests.cs` — Modified: updated RelicDef constructor calls (review fix)
- `Tests/Runtime/Shop/ClickToBuyTests.cs` — Modified: updated RelicDef constructor call (review fix)
- `Tests/Runtime/Core/ReputationManagerTests.cs` — Modified: updated RelicDef constructor call (review fix)

## Change Log

- 2026-02-20: Story 17.10 implemented — added text icon characters and CRT category colors to all 23 relics, displayed in shop cards, owned bar, and trading HUD RelicBar. Created RelicIconHelper for hex-to-Color parsing. 16 new tests.
- 2026-02-20: Code review (7 findings: 2H/3M/2L). Fixes applied: (H1) Documented 17.9 cross-story contamination — 17.9 code bundled in this commit without tracking, 17.9 status set to in-progress with missing tests flagged. (H2) Updated Task 2 subtask to document intentional color deviation from ColorPalette. (M1) Added AllRelicIds_CoveredByExactlyOneCategory test. (M2) Made RelicDef iconChar/iconColorHex required params, updated 6 test files. (M3) Documented proportional font limitation in dev notes. (L1, L2) Acknowledged — low priority.
