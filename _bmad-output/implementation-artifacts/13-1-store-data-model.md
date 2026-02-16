# Story 13.1: Store Data Model & State Management

Status: ready for dev

## Story

As a developer,
I want a clean data model that tracks all store state (owned relics, expansions, tips, bonds, reroll count) within RunContext,
so that store state persists correctly across rounds and integrates with save/load.

## Acceptance Criteria

1. RunContext extended with `OwnedRelics: List<string>` (item IDs, max = MaxRelicSlots)
2. RunContext extended with `OwnedExpansions: List<string>` (expansion IDs, permanent per run)
3. RunContext extended with `BondsOwned: int` and `BondPurchaseHistory: List<BondRecord>`
4. RunContext extended with `CurrentShopRerollCount: int` (resets per shop visit)
5. RunContext extended with `InsiderTipSlots: int` (default 2, increased by expansion)
6. RunContext extended with `RevealedTips: List<InsiderTip>` (cleared on shop close)
7. ShopState orchestrates all four panels: relics, expansions, tips, bonds
8. All purchases are atomic (validate → deduct currency → apply effect → fire event)
9. State survives round transitions correctly
10. Old `ActiveItems` field migrated to `OwnedRelics`

## Tasks / Subtasks

- [ ] Task 1: Extend RunContext with store fields (AC: 1, 2, 3, 4, 5, 6, 10)
  - [ ] Add `OwnedRelics: List<string>` — replaces `ActiveItems`
  - [ ] Add `OwnedExpansions: List<string>`
  - [ ] Add `BondsOwned: int`
  - [ ] Add `BondPurchaseHistory: List<BondRecord>` (struct: RoundPurchased, PricePaid)
  - [ ] Add `CurrentShopRerollCount: int`
  - [ ] Add `InsiderTipSlots: int` — initialized to `GameConfig.DefaultInsiderTipSlots` (2)
  - [ ] Add `RevealedTips: List<RevealedTip>` (struct: TipType, RevealedText)
  - [ ] Migrate all existing references from `ActiveItems` to `OwnedRelics`
  - [ ] Initialize all new fields in RunContext constructor/reset
  - [ ] File: `Scripts/Runtime/Core/RunContext.cs`
- [ ] Task 2: Define store data structs (AC: 1, 3, 6)
  - [ ] `BondRecord` struct: `int RoundPurchased`, `float PricePaid`
  - [ ] `RevealedTip` struct: `InsiderTipType Type`, `string RevealedText`
  - [ ] Place in RunContext.cs or a dedicated `Scripts/Runtime/Shop/StoreDataTypes.cs`
- [ ] Task 3: Update ShopState to orchestrate all panels (AC: 7)
  - [ ] `ShopState.Enter()`:
    - Reset `CurrentShopRerollCount` to 0
    - Clear `RevealedTips`
    - Generate relic offering (delegate to ShopGenerator)
    - Select available expansions (delegate to ExpansionManager)
    - Generate insider tips (delegate to InsiderTipGenerator)
    - Calculate bond price (delegate to BondManager)
    - Show store UI with all panel data
  - [ ] `ShopState.Exit()`:
    - Fire `ShopClosedEvent` with summary of all purchases
    - Hide store UI
  - [ ] File: `Scripts/Runtime/Core/GameStates/ShopState.cs`
- [ ] Task 4: Update ShopTransaction for multi-panel purchases (AC: 8)
  - [ ] Add purchase paths: `PurchaseRelic()`, `PurchaseExpansion()`, `PurchaseTip()`, `PurchaseBond()`, `SellBond()`
  - [ ] Each path follows atomic pattern: validate → deduct → apply → fire event
  - [ ] Relic/Expansion/Tip purchases deduct Reputation
  - [ ] Bond purchase deducts Cash
  - [ ] Bond sell adds Cash
  - [ ] File: `Scripts/Runtime/Shop/ShopTransaction.cs`
- [ ] Task 5: Migrate ActiveItems references (AC: 10)
  - [ ] Find all references to `RunContext.ActiveItems` in codebase
  - [ ] Replace with `RunContext.OwnedRelics`
  - [ ] Update `ItemLookup.cs` if it references ActiveItems
  - [ ] Update `ItemInventoryPanel.cs` if it references ActiveItems
  - [ ] Update any test files referencing ActiveItems
  - [ ] Files: various — search for `ActiveItems`
- [ ] Task 6: State persistence validation (AC: 9)
  - [ ] Verify all store fields survive round transitions (ShopState → MarketOpenState → ... → ShopState)
  - [ ] OwnedRelics: persists across all rounds
  - [ ] OwnedExpansions: persists across all rounds
  - [ ] BondsOwned + BondPurchaseHistory: persists across all rounds
  - [ ] CurrentShopRerollCount: resets each shop visit
  - [ ] InsiderTipSlots: persists (modified only by expansion purchase)
  - [ ] RevealedTips: cleared each shop visit
- [ ] Task 7: Write tests (All AC)
  - [ ] RunContext initialization: all fields default correctly
  - [ ] ActiveItems → OwnedRelics migration: no broken references
  - [ ] ShopState orchestration: all panels receive data
  - [ ] ShopTransaction: each purchase path works atomically
  - [ ] State persistence: fields survive round transitions correctly
  - [ ] Files: `Tests/Runtime/Shop/StoreDataModelTests.cs`, `Tests/Runtime/Core/RunContextStoreTests.cs`

## Dev Notes

### Architecture Compliance

- **RunContext is the single source of truth** for all run state. All store state lives here.
- **Plain C# structs** for data types (BondRecord, RevealedTip) — no MonoBehaviours.
- **ShopState is the orchestrator** — it delegates to managers (ShopGenerator, ExpansionManager, InsiderTipGenerator, BondManager) but owns the flow.
- **ShopTransaction handles all purchases** — single entry point for atomic buy/sell operations.

### Migration: ActiveItems → OwnedRelics

The existing `ActiveItems: List<string>` field in RunContext tracks purchased shop items. This needs to be renamed to `OwnedRelics` to match the new naming. All references across the codebase must be updated:
- `ItemLookup.cs` — may reference ActiveItems for item effect lookups
- `ItemInventoryPanel.cs` — displays owned items during trading
- `ShopState.cs` — checks owned items to prevent duplicates
- `ShopGenerator.cs` — filters owned items from pool
- Test files — multiple references

### This Story is the Foundation

This is the **first story to implement** (recommended order). It creates the data model that Stories 13.2-13.6 and 13.7-13.9 all depend on. The ShopState orchestration here will initially call stub methods on managers that don't exist yet — that's fine. The structure is set up for each subsequent story to fill in.

### Existing Code to Understand

Before implementing, the dev agent MUST read:
- `Scripts/Runtime/Core/RunContext.cs` — current state fields, ActiveItems
- `Scripts/Runtime/Core/GameStates/ShopState.cs` — current shop orchestration
- `Scripts/Runtime/Shop/ShopTransaction.cs` — current purchase flow
- `Scripts/Runtime/Shop/ShopGenerator.cs` — current item generation
- `Scripts/Runtime/Items/ItemLookup.cs` — item reference system
- `Scripts/Runtime/UI/ItemInventoryPanel.cs` — item display during trading

### Depends On

Nothing — this is the foundation story with no dependencies.

## Dev Agent Record

### Agent Model Used

### Completion Notes List

### Change Log

### File List
