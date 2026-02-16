# Story 13.3: Relics Panel — Item Offering, Purchase & Reroll

Status: ready for dev

## Story

As a player,
I want the top section of the store to show 3 randomly selected relics (items) that I can purchase with Reputation, and a reroll button to refresh the selection,
so that I have meaningful choices and agency in my build.

## Acceptance Criteria

1. 3 relic cards displayed in the top section, drawn randomly from the item pool
2. Items drawn using uniform random selection (no rarity system — all relics equally likely to appear)
3. Owned items excluded from the offering (no duplicates)
4. Each card shows: name, description, cost (Reputation)
5. Purchase button per card — deducts Reputation, adds item to inventory
6. Card disappears or shows "SOLD" after purchase
7. Reroll button costs Reputation (configurable: `RerollBaseCost` in GameConfig)
8. Reroll cost increases each use per shop visit (`RerollCostIncrement`)
9. Reroll cost resets each shop visit
10. Reroll regenerates all 3 unsold relic slots with new random items
11. Player can hold a maximum of 5 relics (`MaxRelicSlots` in GameConfig)
12. If at relic capacity, purchase buttons disabled with "FULL" indicator
13. Expanded Inventory expansion increases max relic slots
14. If entire relic pool exhausted (all owned), slots show "SOLD OUT"
15. `ShopItemPurchasedEvent` fires on purchase
16. No rarity system — cost alone determines value/power
17. Relic definitions (items, effects, costs) are OUT OF SCOPE — this story builds infrastructure only. Use a minimal placeholder pool for testing

## Tasks / Subtasks

- [ ] Task 1: Relic data structure (AC: 4, 16, 17)
  - [ ] Define `RelicDef` struct: `Id`, `Name`, `Description`, `Cost` (no rarity field)
  - [ ] Create minimal placeholder relic pool (5-8 test relics) for development/testing
  - [ ] Placeholder relics will be replaced entirely in a future item design epic
  - [ ] File: `Scripts/Setup/Data/ShopItemDefinitions.cs` — restructure to `RelicDef` (remove `ItemRarity`, `ItemCategory`)
- [ ] Task 2: Update ShopGenerator for uniform random (AC: 1, 2, 3, 14)
  - [ ] Remove rarity-weighted selection logic entirely
  - [ ] Generate 3 relics via uniform random from available pool
  - [ ] Exclude owned relics from pool before selection
  - [ ] Handle pool exhaustion: return null for exhausted slots
  - [ ] File: `Scripts/Runtime/Shop/ShopGenerator.cs` — rewrite
- [ ] Task 3: Relic card UI rendering (AC: 4, 6)
  - [ ] Populate 3 relic card slots in the top section (created in 13.2)
  - [ ] Each card displays: relic name, description text, cost with Rep icon
  - [ ] "SOLD" state after purchase (card greys out or shows stamp)
  - [ ] "SOLD OUT" state when pool exhausted
  - [ ] File: `Scripts/Runtime/UI/ShopUI.cs` — relic panel population
- [ ] Task 4: Purchase flow (AC: 5, 11, 12, 15)
  - [ ] Purchase button click: validate affordability + capacity → deduct Rep → add to OwnedRelics → fire event
  - [ ] Capacity check: if `OwnedRelics.Count >= MaxRelicSlots`, disable all purchase buttons, show "FULL"
  - [ ] Check `ExpansionManager` for Expanded Inventory to determine actual max slots (AC: 13)
  - [ ] Update Rep display after purchase
  - [ ] File: `Scripts/Runtime/Shop/ShopTransaction.cs` — updated purchase logic
- [ ] Task 5: Reroll mechanism (AC: 7, 8, 9, 10)
  - [ ] Reroll button shows current cost (starts at `RerollBaseCost`)
  - [ ] On click: deduct Rep, increment reroll count, regenerate unsold slots
  - [ ] Cost = `RerollBaseCost + (RerollCostIncrement * rerollCount)`
  - [ ] Disable reroll button if player can't afford
  - [ ] `CurrentShopRerollCount` resets to 0 when shop opens
  - [ ] File: `Scripts/Runtime/UI/ShopUI.cs`, `Scripts/Runtime/Shop/ShopTransaction.cs`
- [ ] Task 6: GameConfig constants (AC: 7, 8, 11)
  - [ ] `MaxRelicSlots = 5`
  - [ ] `RerollBaseCost = 5`
  - [ ] `RerollCostIncrement = 2`
  - [ ] File: `Scripts/Setup/Data/GameConfig.cs`
- [ ] Task 7: Write tests (All AC)
  - [ ] ShopGenerator: 3 relics generated, no duplicates, owned excluded, pool exhaustion
  - [ ] Purchase: Rep deducted, relic added, capacity enforced, event fired
  - [ ] Reroll: cost escalation, slot regeneration, cost reset per visit
  - [ ] Files: `Tests/Runtime/Shop/ShopGeneratorTests.cs`, `Tests/Runtime/Shop/RelicPurchaseTests.cs`

## Dev Notes

### Architecture Compliance

- **No rarity system:** `ItemRarity` enum is removed. All relics have equal selection probability. Cost is the sole indicator of power.
- **Placeholder items only:** This story creates 5-8 test relics with made-up names/costs. The real relic pool will be designed in a dedicated future epic. Do NOT spend time designing balanced items.
- **Setup-Oriented Generation:** Relic card UI populated programmatically.
- **Atomic purchases:** Validate → deduct → add → fire event. Rollback on failure.

### Existing Code to Understand

Before implementing, the dev agent MUST read:
- `Scripts/Setup/Data/ShopItemDefinitions.cs` — current 30-item definitions to replace
- `Scripts/Runtime/Shop/ShopGenerator.cs` — current rarity-weighted selection to replace
- `Scripts/Runtime/Shop/ShopTransaction.cs` — current purchase flow to update
- `Scripts/Runtime/Core/RunContext.cs` — `ActiveItems` field to migrate to `OwnedRelics`
- `Scripts/Runtime/Core/ReputationManager.cs` — Rep deduction API

### Depends On

- Story 13.2 (Store Layout Shell) — relic card slots must exist
- Story 13.1 (Data Model) — `OwnedRelics` field in RunContext

## Dev Agent Record

### Agent Model Used

### Completion Notes List

### Change Log

### File List
