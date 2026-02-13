# Story 7.2: Item Pool Generation

Status: done

## Story

As a developer,
I want items randomly selected from a rarity-weighted pool with unlock filtering and duplicate prevention,
so that shop offerings vary each run and respect progression systems.

## Acceptance Criteria

1. Rarity tiers (Common, Uncommon, Rare, Legendary) control selection probability with configurable weights
2. Higher rarity items appear less frequently (Common ~50%, Uncommon ~30%, Rare ~15%, Legendary ~5%)
3. One item generated per category (TradingTool, MarketIntel, PassivePerk) per shop visit
4. Items drawn from the unlocked item pool (infrastructure for Epic 9 unlock gating, all unlocked by default for now)
5. Items already owned in the current run (in RunContext.ActiveItems) are excluded from the pool
6. If a category's eligible pool is empty (all items owned), that slot returns null/empty gracefully
7. Rarity weights are defined as static configuration data (not hardcoded in selection logic)

## Tasks / Subtasks

- [x] Task 1: Add rarity weight configuration to ShopItemDefinitions (AC: 1, 2, 7)
  - [x] Define `RarityWeight` struct with `ItemRarity Rarity` and `float Weight` fields
  - [x] Add `public static readonly RarityWeight[] RarityWeights` array with default weights: Common=50, Uncommon=30, Rare=15, Legendary=5
  - [x] Add `public static float GetWeightForRarity(ItemRarity rarity)` helper method that looks up weight from the array
  - [x] Weights are relative (not percentages) so the system normalizes them at selection time — this allows the weights to remain valid even when some rarities are excluded from the pool
  - [x] File: `Scripts/Setup/Data/ShopItemDefinitions.cs` (modify)

- [x] Task 2: Add unlock pool infrastructure to ShopItemDefinitions (AC: 4)
  - [x] Add `public static readonly HashSet<string> DefaultUnlockedItems` containing all 30 item IDs (everything unlocked by default)
  - [x] Add `public static bool IsUnlocked(string itemId, HashSet<string> unlockedPool)` method that checks if an item ID exists in the provided unlock pool
  - [x] Add `public static List<ShopItemDef> GetUnlockedItems(HashSet<string> unlockedPool)` that filters `AllItems` to only those in the pool
  - [x] The unlock pool parameter pattern allows Epic 9 (Meta-Progression / Reputation) to pass a player-specific unlock set without modifying this code
  - [x] For now, all call sites pass `DefaultUnlockedItems` — no actual gating until Epic 9
  - [x] File: `Scripts/Setup/Data/ShopItemDefinitions.cs` (modify)

- [x] Task 3: Implement rarity-weighted selection algorithm in ShopGenerator (AC: 1, 2, 3, 5, 6)
  - [x] Refactor `ShopGenerator.GenerateShopItems()` (or equivalent method) to use the new weighted selection
  - [x] Accept `RunContext` (or `List<string> ownedItemIds`) parameter to know which items to exclude
  - [x] Accept `HashSet<string> unlockedPool` parameter (defaults to `ShopItemDefinitions.DefaultUnlockedItems`)
  - [x] Accept `System.Random` parameter for deterministic testing (matches project pattern from `NewsHeadlineData`, `EventHeadlineData`)
  - [x] Selection algorithm per category:
    1. Filter items by category
    2. Remove items not in the unlock pool
    3. Remove items already in `RunContext.ActiveItems` (duplicate prevention)
    4. Group remaining items by rarity
    5. Weighted random select a rarity tier (using `RarityWeights`, normalized to only include rarities that have eligible items)
    6. Random select one item from that rarity tier
  - [x] If no eligible items remain for a category, return null for that slot
  - [x] Log warning via `Debug.LogWarning` when a category pool is exhausted
  - [x] File: `Scripts/Runtime/Shop/ShopGenerator.cs` (modify)

- [x] Task 4: Add helper method for weighted random rarity selection (AC: 1, 2)
  - [x] Create `SelectWeightedRarity(Dictionary<ItemRarity, List<ShopItemDef>> groupedItems, System.Random random)` method in ShopGenerator
  - [x] Build cumulative weight array from only the rarities that have items in `groupedItems`
  - [x] Generate random float in [0, totalWeight) and walk the cumulative array to find selected rarity
  - [x] Return the selected `ItemRarity`
  - [x] This method is separate from item selection so it can be tested independently
  - [x] File: `Scripts/Runtime/Shop/ShopGenerator.cs` (modify)

- [x] Task 5: Update ShopState to pass RunContext to ShopGenerator (AC: 3, 4, 5)
  - [x] Update `ShopState.Enter()` to pass `RunContext` (or `ctx.ActiveItems`) to `ShopGenerator.GenerateShopItems()`
  - [x] Handle null item slots gracefully in the UI (hide or gray out the card if a category has no items available)
  - [x] Log the generated items for debugging: `[ShopState] Generated items: {Tool}, {Intel}, {Perk}` (or "none" for null slots)
  - [x] File: `Scripts/Runtime/Core/GameStates/ShopState.cs` (modify)

- [x] Task 6: Update ShopUI to handle empty item slots (AC: 6)
  - [x] When a shop slot receives a null item (category pool exhausted), display a "SOLD OUT" or "No items available" state
  - [x] Disable the purchase button for empty slots
  - [x] This is a graceful degradation — unlikely in early runs but possible in late-game when player owns many items
  - [x] File: `Scripts/Runtime/UI/ShopUI.cs` (modify)

- [x] Task 7: Write comprehensive tests (AC: 1-7)
  - [x] Test: Weighted selection respects rarity distribution — run 1000+ selections, verify Common appears ~50%, Uncommon ~30%, Rare ~15%, Legendary ~5% (within tolerance, e.g., +/-10%)
  - [x] Test: One item per category — GenerateShopItems returns exactly 3 items (one per category) when all pools have items
  - [x] Test: Duplicate prevention — add items to RunContext.ActiveItems, verify they never appear in generated shop items
  - [x] Test: Owned all items in a category — when all TradingTools are owned, that slot returns null, other two slots still generate normally
  - [x] Test: Unlock filtering — create a restricted unlock pool with only 5 items, verify only those items appear in generation
  - [x] Test: Deterministic with same System.Random seed — same seed produces same items
  - [x] Test: Empty unlock pool — returns null for all three slots without throwing
  - [x] Test: RarityWeights data validation — all four rarities have positive weights
  - [x] Test: GetWeightForRarity returns correct weight for each rarity
  - [x] Test: When only one rarity has eligible items, that rarity is always selected
  - [x] File: `Tests/Runtime/Shop/ShopGeneratorTests.cs` (new or extend)
  - [x] File: `Tests/Setup/Data/ShopItemDefinitionsTests.cs` (new or extend)

## Dev Notes

### Architecture Compliance

- **Setup-Oriented Generation:** No new UI or scene objects — this story is pure logic enhancement to existing ShopGenerator and ShopItemDefinitions
- **Static Data Pattern:** Rarity weights and unlock pool defined as `public static readonly` in `ShopItemDefinitions.cs` — no ScriptableObjects, matches `GameConfig`, `StockTierData`, `EventDefinitions` pattern
- **EventBus:** No new events needed — shop events (ShopOpenedEvent, ShopItemPurchasedEvent, ShopClosedEvent) already defined in Story 7.1
- **System.Random for testability:** Inject `System.Random` into generation methods for deterministic testing — same pattern used by `NewsHeadlineData.GetHeadline()` and `EventHeadlineData.GetHeadline()`
- **No direct system references:** ShopGenerator receives data via parameters (RunContext, unlock pool), not by referencing other systems directly

### What Already Exists (DO NOT recreate)

| Component | File | Status |
|-----------|------|--------|
| `ShopItemDef` struct | `ShopItemDefinitions.cs` | Complete — Id, Name, Description, Cost, Rarity, Category |
| `ItemRarity` enum | `ShopItemDefinitions.cs` | Complete — Common, Uncommon, Rare, Legendary |
| `ItemCategory` enum | `ShopItemDefinitions.cs` | Complete — TradingTool, MarketIntel, PassivePerk |
| All 30 item definitions | `ShopItemDefinitions.cs` | Complete — 10 per category |
| `ShopGenerator` class | `ShopGenerator.cs` | Exists from 7.1 — basic selection logic to be enhanced |
| `ShopState` game state | `ShopState.cs` | Exists from 7.1 — calls ShopGenerator |
| `ShopUI` panel | `ShopUI.cs` | Exists from 7.1 — displays items |
| `RunContext.ActiveItems` | `RunContext.cs` | Complete — `List<string>` tracking owned item IDs |
| Shop events | `GameEvents.cs` | Complete — ShopOpenedEvent, ShopItemPurchasedEvent, ShopClosedEvent |

### Weighted Selection Algorithm Detail

The selection algorithm must handle edge cases where some rarities have no eligible items (already owned or not unlocked). The approach:

```csharp
// Pseudocode for weighted rarity selection
public static ShopItemDef? SelectItem(ItemCategory category, List<string> ownedIds,
    HashSet<string> unlockedPool, System.Random random)
{
    // 1. Filter: category match, unlocked, not owned
    var eligible = AllItems
        .Where(i => i.Category == category)
        .Where(i => unlockedPool.Contains(i.Id))
        .Where(i => !ownedIds.Contains(i.Id))
        .ToList();

    if (eligible.Count == 0) return null;

    // 2. Group by rarity
    var grouped = eligible.GroupBy(i => i.Rarity)
        .ToDictionary(g => g.Key, g => g.ToList());

    // 3. Build weights for only present rarities
    float totalWeight = 0;
    foreach (var kvp in grouped)
        totalWeight += GetWeightForRarity(kvp.Key);

    // 4. Roll and select rarity
    float roll = (float)(random.NextDouble() * totalWeight);
    ItemRarity selectedRarity = default;
    float cumulative = 0;
    foreach (var kvp in grouped)
    {
        cumulative += GetWeightForRarity(kvp.Key);
        if (roll < cumulative) { selectedRarity = kvp.Key; break; }
    }

    // 5. Random item from selected rarity
    var pool = grouped[selectedRarity];
    return pool[random.Next(pool.Count)];
}
```

Key insight: When some rarities are depleted (e.g., all Common TradingTools owned), the weights are re-normalized among remaining rarities. This means a category with only Rare and Legendary items left will still respect the 3:1 ratio between them (15:5 = 75%:25%).

### Unlock Pool Design (Epic 9 Preparation)

The unlock pool is a `HashSet<string>` of item IDs. This story sets `DefaultUnlockedItems` to contain all 30 IDs. Epic 9 will:
1. Create a `MetaManager` that tracks player reputation and unlocks
2. Provide a player-specific `HashSet<string>` based on reputation thresholds
3. Pass that set to `ShopGenerator` instead of `DefaultUnlockedItems`

The interface is ready — the implementation just needs Epic 9 to supply the actual unlock data.

### Duplicate Prevention Scope

Duplicates are prevented **within a single run only**. Between runs, `RunContext.ActiveItems` is cleared by `ResetForNewRun()` (line 190 in RunContext.cs), so all items are available again. This aligns with the roguelike design where each run starts fresh.

### Edge Cases to Handle

1. **All items owned in a category:** Return null for that slot. ShopUI should display "Sold Out" state.
2. **All items owned across all categories:** Return three nulls. Shop should still display but with all slots showing "Sold Out."
3. **Very restricted unlock pool:** If only 1 item per category is unlocked and the player already owns it, that category returns null.
4. **No Legendary items in category pool:** Weights re-normalize among remaining rarities — selection still works correctly.

### Dev Guardrails

- **DO NOT** create new enums or structs for rarity/category — `ItemRarity` and `ItemCategory` already exist in `ShopItemDefinitions.cs`
- **DO NOT** add new events to `GameEvents.cs` — shop events are already defined from Story 7.1
- **DO NOT** modify `RunContext.ActiveItems` from ShopGenerator — that is ShopState/ShopTransaction responsibility (Story 7.3)
- **DO NOT** implement actual unlock gating logic — just the filtering infrastructure. All items remain unlocked.
- **DO NOT** use LINQ in hot paths if avoidable — but for shop generation (called once per shop visit), LINQ is acceptable for clarity
- **DO** use `System.Random` parameter (not `UnityEngine.Random`) for testability
- **DO** keep ShopGenerator stateless — all data passed via parameters, no cached state between calls
- **DO** read existing ShopGenerator code before modifying — understand the current API contract that ShopState depends on

### Previous Story Intelligence (7.1 Shop UI)

Key patterns established in 7.1 that this story must maintain:
- ShopGenerator returns items to ShopState, which passes them to ShopUI
- ShopGenerator is in `Scripts/Runtime/Shop/` folder
- ShopItemDefinitions follows the `public static readonly` pattern in `Scripts/Setup/Data/`
- Shop events are already wired — ShopOpenedEvent carries the available items list

### Existing Code to Understand

Before implementing, the dev agent MUST read:
- `Scripts/Setup/Data/ShopItemDefinitions.cs` — current item definitions and structure
- `Scripts/Runtime/Shop/ShopGenerator.cs` — current selection logic to be enhanced
- `Scripts/Runtime/Core/GameStates/ShopState.cs` — how ShopGenerator is called
- `Scripts/Runtime/UI/ShopUI.cs` — how items are displayed (for null slot handling)
- `Scripts/Runtime/Core/RunContext.cs` — ActiveItems list (duplicate prevention source)
- `Scripts/Setup/Data/NewsHeadlineData.cs` — reference for System.Random injection pattern
- `Scripts/Setup/Data/GameConfig.cs` — reference for static readonly config pattern

### Project Structure Notes

- Modifies: `Scripts/Setup/Data/ShopItemDefinitions.cs` (add rarity weights, unlock pool, helper methods)
- Modifies: `Scripts/Runtime/Shop/ShopGenerator.cs` (enhance selection with weighted rarity, unlock filtering, duplicate prevention)
- Modifies: `Scripts/Runtime/Core/GameStates/ShopState.cs` (pass RunContext to generator)
- Modifies: `Scripts/Runtime/UI/ShopUI.cs` (handle null/empty item slots)
- New/Extend: `Tests/Runtime/Shop/ShopGeneratorTests.cs` (weighted selection tests, duplicate prevention, edge cases)
- New/Extend: `Tests/Setup/Data/ShopItemDefinitionsTests.cs` (rarity weight validation, unlock pool tests)
- No new runtime files created — this story enhances existing infrastructure

### References

- [Source: bull-run-gdd-mvp.md#4] — "Draft Shop System" — shop design, 30 items across 3 categories
- [Source: bull-run-gdd-mvp.md#4.1-4.3] — Item lists with costs and rarities per category
- [Source: bull-run-gdd-mvp.md#5] — "Meta-Progression" — reputation unlocks (Epic 9, infrastructure prepared here)
- [Source: epics.md#Epic 7] — Story 7.2: "Items randomly selected from a rarity-weighted pool"
- [Source: epics.md#Epic 9] — Meta-progression unlock system (future consumer of unlock pool infrastructure)
- [Source: game-architecture.md#Shop System] — ShopGenerator location and responsibility
- [Source: game-architecture.md#Data Architecture] — Static data pattern for ShopItemDefinitions
- [Source: 7-1-shop-ui.md] — Predecessor story establishing ShopGenerator, ShopItemDefinitions, ShopState, ShopUI

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Debug Log References

- `[ShopGenerator] Category {category} pool exhausted` — logged when no eligible items remain for a category
- `[ShopState] Generated items: {Tool}, {Intel}, {Perk}` — logged on shop open showing selected items (or "none" for null slots)

### Completion Notes List

- Task 1: Added `RarityWeight` struct and replaced flat float array `RarityWeights` with struct-based `RarityWeight[]` using relative weights (50/30/15/5). Added `GetWeightForRarity()` helper. Updated existing test `RarityWeights_SumToOne` to `RarityWeights_SumTo100` to match new relative weight format.
- Task 2: Added `DefaultUnlockedItems` HashSet with all 30 item IDs, `IsUnlocked()` and `GetUnlockedItems()` methods. All items unlocked by default, ready for Epic 9 to supply restricted pools.
- Task 3: Refactored `ShopGenerator.GenerateOffering()` to accept `List<string> ownedItemIds`, `HashSet<string> unlockedPool`, and `System.Random random`. Returns `ShopItemDef?[]` (nullable) to handle exhausted pools. Selection filters by category, unlock pool, and owned items before weighted rarity selection.
- Task 4: Added `SelectWeightedRarity()` as public static method for independent testability. Builds cumulative weight from only present rarities and walks cumulative array.
- Task 5: Updated `ShopState.Enter()` to pass `ctx.ActiveItems` and `DefaultUnlockedItems` to `ShopGenerator.GenerateOffering()`. Added `_nullableOffering` field for null-aware handling. Added debug logging of generated items. Added null-slot guard in `OnPurchase`.
- Task 6: Added `SetupSoldOutCard()` method to ShopUI. Updated `Show()` to accept `ShopItemDef?[]` and route null items to sold-out display. Updated `RefreshAfterPurchase()` to skip sold-out slots.
- Task 7: Wrote 17 tests in ShopGeneratorTests (weighted distribution with 2000 samples, duplicate prevention, unlock filtering, deterministic seeding, empty pools, single-rarity selection) and 26 tests in ShopItemDefinitionsTests (rarity weights validation, GetWeightForRarity per-rarity, unlock pool coverage, IsUnlocked, GetUnlockedItems).

### File List

- `Assets/Scripts/Setup/Data/ShopItemDefinitions.cs` (modified) — Added RarityWeight struct, replaced float[] RarityWeights with RarityWeight[], added GetWeightForRarity, DefaultUnlockedItems, IsUnlocked, GetUnlockedItems
- `Assets/Scripts/Runtime/Shop/ShopGenerator.cs` (modified) — Refactored to accept owned items, unlock pool, System.Random; returns nullable items; added SelectWeightedRarity
- `Assets/Scripts/Runtime/Core/GameStates/ShopState.cs` (modified) — Updated Enter() to pass RunContext data to ShopGenerator, added null-slot handling
- `Assets/Scripts/Runtime/UI/ShopUI.cs` (modified) — Updated Show() for nullable items, added SetupSoldOutCard(), updated RefreshAfterPurchase for null slots
- `Assets/Tests/Runtime/Shop/ShopGeneratorTests.cs` (modified) — Rewrote all tests for new API, added weighted distribution, duplicate prevention, unlock filtering, deterministic, and edge case tests
- `Assets/Tests/Runtime/Shop/ShopItemDefinitionsTests.cs` (modified) — Added rarity weight validation, GetWeightForRarity tests, unlock pool tests

## Change Log

- 2026-02-13: Implemented rarity-weighted item pool generation with unlock filtering, duplicate prevention, and graceful null-slot handling. All 7 tasks completed.
- 2026-02-13: Code review fixes (3 HIGH, 4 MEDIUM). H1: Fixed distribution test to use MarketIntel (has Legendary items) + added lower bound. H2: Removed redundant _offering field, ShopOpenedEvent now publishes only non-null items. H3: Fixed inflated test counts in completion notes (17/26 actual). M1: SelectWeightedRarity now sorts keys for deterministic iteration. M2: SelectItem converts ownedItemIds to HashSet for O(1) lookups. M3: Sold-out cards now show category name instead of "---". M4: ShopState uses _nullableOffering exclusively.
