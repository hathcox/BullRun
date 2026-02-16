# Story 13.9: Cleanup & Migration from Old Shop

Status: pending

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

- [ ] Task 1: Remove ItemRarity and ItemCategory enums (AC: 3, 4)
  - [ ] Delete `ItemRarity` enum (Common, Uncommon, Rare, Legendary)
  - [ ] Delete `ItemCategory` enum (TradingTool, MarketIntel, PassivePerk)
  - [ ] Search entire codebase for references and remove/update
  - [ ] Files: `Scripts/Setup/Data/ShopItemDefinitions.cs`, any files referencing these enums
- [ ] Task 2: Gut ShopItemDefinitions (AC: 5)
  - [ ] Remove all 30 existing item definitions
  - [ ] Replace `ShopItemDef` struct with `RelicDef` struct (Id, Name, Description, Cost — no Rarity, no Category)
  - [ ] Keep only a minimal placeholder pool (5-8 test relics) for development
  - [ ] File: `Scripts/Setup/Data/ShopItemDefinitions.cs` — major rewrite
- [ ] Task 3: Remove rarity-weighted selection from ShopGenerator (AC: 6)
  - [ ] Remove rarity tier logic, weight calculation, rarity-based filtering
  - [ ] Replace with simple uniform random selection from available pool
  - [ ] File: `Scripts/Runtime/Shop/ShopGenerator.cs`
- [ ] Task 4: Migrate Trade Volume upgrade (AC: 7)
  - [ ] Remove Trade Volume upgrade card from old shop system (FIX-13)
  - [ ] Ensure equivalent functionality exists in Trading Deck Expansions (if not already covered by 13.3)
  - [ ] Remove `QuantitySelector` tier-unlock shop integration if it references old shop
  - [ ] Files: `Scripts/Runtime/UI/QuantitySelector.cs`, `Scripts/Runtime/UI/ShopUI.cs`
- [ ] Task 5: Remove old ShopUI layout code (AC: 1, 2)
  - [ ] Remove any remaining old 3-card horizontal layout code from ShopUI
  - [ ] Remove old category labels, old timer display, old single-panel structure
  - [ ] Verify new multi-panel layout (from 13.1) is the only active layout
  - [ ] File: `Scripts/Runtime/UI/ShopUI.cs`
- [ ] Task 6: Clean up old shop references (AC: 11)
  - [ ] Search for dead code referencing old shop concepts: category filtering, rarity weights, old item IDs
  - [ ] Remove unused imports, dead methods, commented-out old code
  - [ ] Check `ItemLookup.cs` — update to work with RelicDef instead of ShopItemDef
  - [ ] Check `ItemInventoryPanel.cs` — remove rarity badge display, category labels
  - [ ] Files: various — grep for `ItemRarity`, `ItemCategory`, `ShopItemDef`, old item IDs
- [ ] Task 7: Update event payloads (AC: 10)
  - [ ] Verify `ShopOpenedEvent` reflects new store sections
  - [ ] Verify `ShopClosedEvent` includes all section purchase summaries
  - [ ] Remove any event fields referencing old concepts (rarity, category)
  - [ ] File: `Scripts/Runtime/Core/GameEvents.cs`
- [ ] Task 8: Update all shop tests (AC: 8, 9)
  - [ ] `ShopGeneratorTests.cs` — update for uniform random, remove rarity tests
  - [ ] `ShopTransactionTests.cs` — update for multi-panel purchase paths
  - [ ] `ShopItemDefinitionsTests.cs` — update for RelicDef, remove 30-item validation
  - [ ] `ShopStateTests.cs` — update for new orchestration flow
  - [ ] Add any missing test coverage for new store features
  - [ ] Verify no test references old enums or structs
  - [ ] Files: `Tests/Runtime/Shop/` — all test files
- [ ] Task 9: Regression verification (AC: 9)
  - [ ] Run full test suite — no failures
  - [ ] Manual smoke test: enter store, purchase relic, purchase expansion, buy tip, buy bond, reroll, proceed to next round
  - [ ] Verify reputation deduction works correctly across all sections
  - [ ] Verify item inventory display works with new RelicDef structure

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

- Stories 13.1-13.5 — new store must be fully functional before old code is removed
- Story 13.6 (Data Model) — new data structures must be in place
- Story 13.8 (Visual Polish) — animations complete before old UI code removed

## Dev Agent Record

### Agent Model Used

### Completion Notes List

### Change Log

### File List
