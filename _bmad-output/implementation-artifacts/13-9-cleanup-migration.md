# Story 13.9: Cleanup & Migration from Old Shop

Status: done

## Story

As a developer,
I want to cleanly remove the old 3-card draft shop implementation and migrate any still-relevant logic to the new store system,
so that there is no dead code or conflicting behavior.

## Acceptance Criteria

1. Old ShopUI single-panel layout code fully removed
2. Old category-based generation (one Tool, one Intel, one Perk) removed entirely
3. `ItemCategory` enum removed from codebase
4. `ItemRarity` enum removed from codebase — rarity is no longer a concept in the game
5. All 30 existing item definitions in `ShopItemDefinitions.cs` removed — relic items will be completely redesigned in a future epic
6. Rarity-weighted selection logic in `ShopGenerator` replaced with uniform random
7. Old "Trade Volume" upgrade card (FIX-13) migrated to Trading Deck Expansions
8. All existing shop tests updated or replaced to cover new store behavior
9. No regressions in: purchase flow, reputation deduction, item inventory display
10. Event payloads updated to reflect new store sections
11. No dead code paths referencing old shop concepts remain

## Tasks / Subtasks

- [x] Task 1: Remove ItemRarity and ItemCategory enums (AC: 3, 4)
  - [x] Delete `ItemRarity` enum (Common, Uncommon, Rare, Legendary)
  - [x] Delete `ItemCategory` enum (TradingTool, MarketIntel, PassivePerk)
  - [x] Search entire codebase for references and remove/update
  - [x] Files: `Scripts/Setup/Data/ShopItemDefinitions.cs`, any files referencing these enums
- [x] Task 2: Gut ShopItemDefinitions (AC: 5)
  - [x] Remove all 30 existing item definitions
  - [x] Replace `ShopItemDef` struct with `RelicDef` struct (Id, Name, Description, Cost — no Rarity, no Category)
  - [x] Keep only a minimal placeholder pool (5-8 test relics) for development
  - [x] File: `Scripts/Setup/Data/ShopItemDefinitions.cs` — major rewrite
- [x] Task 3: Remove rarity-weighted selection from ShopGenerator (AC: 6)
  - [x] Remove rarity tier logic, weight calculation, rarity-based filtering
  - [x] Replace with simple uniform random selection from available pool
  - [x] File: `Scripts/Runtime/Shop/ShopGenerator.cs`
- [x] Task 4: Migrate Trade Volume upgrade (AC: 7)
  - [x] Remove Trade Volume upgrade card from old shop system (FIX-13)
  - [x] Ensure equivalent functionality exists in Trading Deck Expansions (if not already covered by 13.4)
  - [x] Remove `QuantitySelector` tier-unlock shop integration if it references old shop
  - [x] Files: `Scripts/Runtime/UI/QuantitySelector.cs`, `Scripts/Runtime/UI/ShopUI.cs`
- [x] Task 5: Remove old ShopUI layout code (AC: 1, 2)
  - [x] Remove any remaining old 3-card horizontal layout code from ShopUI
  - [x] Remove old category labels, old timer display, old single-panel structure
  - [x] Verify new multi-panel layout (from 13.2) is the only active layout
  - [x] File: `Scripts/Runtime/UI/ShopUI.cs`
- [x] Task 6: Clean up old shop references (AC: 11)
  - [x] Search for dead code referencing old shop concepts: category filtering, rarity weights, old item IDs
  - [x] Remove unused imports, dead methods, commented-out old code
  - [x] Check `ItemLookup.cs` — update to work with RelicDef instead of ShopItemDef
  - [x] Check `ItemInventoryPanel.cs` — remove rarity badge display, category labels
  - [x] Files: various — grep for `ItemRarity`, `ItemCategory`, `ShopItemDef`, old item IDs
- [x] Task 7: Update event payloads (AC: 10)
  - [x] Verify `ShopOpenedEvent` reflects new store sections
  - [x] Verify `ShopClosedEvent` includes all section purchase summaries
  - [x] Remove any event fields referencing old concepts (rarity, category)
  - [x] File: `Scripts/Runtime/Core/GameEvents.cs`
- [x] Task 8: Update all shop tests (AC: 8, 9)
  - [x] `ShopGeneratorTests.cs` — update for uniform random, remove rarity tests
  - [x] `ShopTransactionTests.cs` — update for multi-panel purchase paths
  - [x] `ShopItemDefinitionsTests.cs` — update for RelicDef, remove 30-item validation
  - [x] `ShopStateTests.cs` — update for new orchestration flow
  - [x] Add any missing test coverage for new store features
  - [x] Verify no test references old enums or structs
  - [x] Files: `Tests/Runtime/Shop/` — all test files
- [x] Task 9: Regression verification (AC: 9)
  - [x] Run full test suite — no failures
  - [x] Manual smoke test: enter store, purchase relic, purchase expansion, buy tip, buy bond, reroll, proceed to next round
  - [x] Verify reputation deduction works correctly across all sections
  - [x] Verify item inventory display works with new RelicDef structure

## Dev Notes

### Architecture Compliance

- **No dead code:** This story is about removing legacy code. Be thorough — grep for all references to removed types.
- **No backwards compatibility:** We are NOT keeping old enums/structs for compatibility. Clean break.
- **Placeholder relics only:** The relic pool after this story will be minimal (5-8 test items). The real item design is a future epic.

### Search Patterns for Cleanup

Use these grep patterns to find all references:
- `ItemRarity` — enum and all usages
- `ItemCategory` — enum and all usages
- `ShopItemDef` — old struct
- `Common|Uncommon|Rare|Legendary` — in context of rarity
- `TradingTool|MarketIntel|PassivePerk` — old categories
- `tool_stop_loss|tool_limit_order|...` — old item IDs (all 30)

### Trade Volume Migration

FIX-13 added a "Trade Volume" upgrade card to the old shop. This let players unlock higher quantity tiers (x5, x10, etc.). With the new store, this concept should be handled either:
1. As a Trading Deck Expansion (if not already equivalent), OR
2. Removed entirely if the expansion system covers it differently

Check `QuantitySelector.cs` to see if Trade Volume is still relevant or if the expansion system supersedes it.

### Existing Code to Understand

Before implementing, the dev agent MUST read:
- `Scripts/Setup/Data/ShopItemDefinitions.cs` — all 30 items being removed
- `Scripts/Runtime/Shop/ShopGenerator.cs` — rarity logic being removed
- `Scripts/Runtime/Items/ItemLookup.cs` — may reference old item types
- `Scripts/Runtime/UI/ItemInventoryPanel.cs` — may display rarity badges
- `Scripts/Runtime/UI/QuantitySelector.cs` — Trade Volume upgrade integration
- All test files in `Tests/Runtime/Shop/`

### Depends On

- Stories 13.2-13.6 — new store must be fully functional before old code is removed
- Story 13.1 (Data Model) — new data structures must be in place
- Story 13.8 (Visual Polish) — animations complete before old UI code removed

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Completion Notes List

- Removed `ItemRarity` enum, `ItemCategory` enum, `ShopItemDef` struct, `RarityWeight` struct, and all 30 legacy item definitions from `ShopItemDefinitions.cs`
- Removed rarity-weighted selection methods (`GenerateOffering`, `SelectItem`, `SelectWeightedRarity`) from `ShopGenerator.cs` — only uniform random `GenerateRelicOffering` remains
- Converted `ItemLookup.cs` from `ShopItemDef` to `RelicDef` — renamed `GetItemById` → `GetRelicById`, removed `GetItemsByCategory` and `GetRarityColor`
- Rewrote `ItemInventoryPanel.cs` from 3-section category layout (Tools/Intel/Perks with rarity borders) to flat relic list with uniform amber `RelicBorderColor`
- Updated `ShopOpenedEvent.AvailableItems` (ShopItemDef[]) → `AvailableRelics` (RelicDef[]) in `GameEvents.cs`
- Removed legacy `PurchaseRelic(RunContext, ShopItemDef)` and `TryPurchase` overloads from `ShopTransaction.cs`
- Removed ShopItemDef conversion block from `ShopState.cs` — now directly uses RelicDef[]
- Removed rarity color constants (`CommonColor`, `UncommonColor`, `RareColor`, `LegendaryColor`), `Show(ShopItemDef[])` method, and `GetRarityColor` from `ShopUI.cs`
- Updated all 9 test files to remove legacy type references: rewrote tests for RelicDef, removed category/rarity tests, converted TryPurchase → PurchaseRelic
- Trade Volume (Task 4): Confirmed no Trade Volume references exist in codebase — expansion system (13.4) supersedes it
- Verified zero functional references to legacy types remain via codebase-wide grep

### Change Log

- 2026-02-16: Story 13.9 implemented — complete removal of old shop system (ItemRarity, ItemCategory, ShopItemDef, 30 items, rarity-weighted selection) and migration to RelicDef-based store
- 2026-02-16: Code review fixes applied (6 issues fixed):
  - H1: Removed vestigial `RarityText`, `RarityBadge`, `CategoryLabel` from `ShopUI.RelicSlotView` and `UISetup.CreateRelicSlot`
  - H2: Removed duplicate `GetRelicById` from `ShopItemDefinitions` (callers route to `ItemLookup.GetRelicById`)
  - M2: Added renaming note to `ShopItemDefinitions` class comment
  - M3: Renamed `ToolHotkeys`→`RelicHotkeys`, `MaxToolSlots`→`MaxDisplaySlots`, `FormatToolSlot`→`FormatRelicSlot`, `FormatEmptyToolSlot`→`FormatEmptyRelicSlot` in `ItemInventoryPanel`
  - M4: Documented struct/class inconsistency (deferred fix)
  - Removed unused `using System.Collections.Generic` from `ShopItemDefinitions.cs`

### File List

**Source files modified:**
- `Assets/Scripts/Setup/Data/ShopItemDefinitions.cs` — major rewrite (removed enums, structs, 30 items)
- `Assets/Scripts/Runtime/Shop/ShopGenerator.cs` — major rewrite (removed rarity-weighted selection)
- `Assets/Scripts/Runtime/Items/ItemLookup.cs` — major rewrite (ShopItemDef → RelicDef)
- `Assets/Scripts/Runtime/UI/ItemInventoryPanel.cs` — major rewrite (removed category partitioning)
- `Assets/Scripts/Runtime/Core/GameEvents.cs` — edited (AvailableItems → AvailableRelics)
- `Assets/Scripts/Runtime/Shop/ShopTransaction.cs` — edited (removed legacy overloads)
- `Assets/Scripts/Runtime/Core/GameStates/ShopState.cs` — edited (removed ShopItemDef conversion)
- `Assets/Scripts/Runtime/UI/ShopUI.cs` — edited (removed rarity colors, legacy Show method)

- `Assets/Scripts/Setup/UISetup.cs` — edited (removed ToolSlotView/IntelBadgeView/PerkEntryView creation, replaced with RelicSlotView)

**Other files modified:**
- `Packages/manifest.json` — package dependency update
- `Packages/packages-lock.json` — package lock update

**Test files modified:**
- `Assets/Tests/Runtime/Shop/ShopItemDefinitionsTests.cs` — complete rewrite + review fix (GetRelicById → ItemLookup)
- `Assets/Tests/Runtime/Shop/ShopGeneratorTests.cs` — complete rewrite
- `Assets/Tests/Runtime/Shop/ShopTransactionTests.cs` — complete rewrite
- `Assets/Tests/Runtime/Shop/StoreDataModelTests.cs` — complete rewrite
- `Assets/Tests/Runtime/Shop/StoreLayoutTests.cs` — edited (removed rarity test, updated event field)
- `Assets/Tests/Runtime/Items/ItemLookupTests.cs` — complete rewrite
- `Assets/Tests/Runtime/UI/ItemInventoryPanelTests.cs` — complete rewrite + review fix (Tool→Relic renaming)
- `Assets/Tests/Runtime/Core/GameStates/ShopStateTests.cs` — edited (AvailableItems → AvailableRelics)
- `Assets/Tests/Runtime/Core/ReputationManagerTests.cs` — edited (ShopItemDef → RelicDef)
- `Assets/Tests/Runtime/Shop/RelicPurchaseTests.cs` — review fix (GetRelicById → ItemLookup)
