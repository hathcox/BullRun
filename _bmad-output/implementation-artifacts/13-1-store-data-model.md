# Story 13.1: Store Data Model & State Management

Status: done

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

- [x] Task 1: Extend RunContext with store fields (AC: 1, 2, 3, 4, 5, 6, 10)
  - [x] Add `OwnedRelics: List<string>` — replaces `ActiveItems`
  - [x] Add `OwnedExpansions: List<string>`
  - [x] Add `BondsOwned: int`
  - [x] Add `BondPurchaseHistory: List<BondRecord>` (struct: RoundPurchased, PricePaid)
  - [x] Add `CurrentShopRerollCount: int`
  - [x] Add `InsiderTipSlots: int` — initialized to `GameConfig.DefaultInsiderTipSlots` (2)
  - [x] Add `RevealedTips: List<RevealedTip>` (struct: TipType, RevealedText)
  - [x] Migrate all existing references from `ActiveItems` to `OwnedRelics`
  - [x] Initialize all new fields in RunContext constructor/reset
  - [x] File: `Scripts/Runtime/Core/RunContext.cs`
- [x] Task 2: Define store data structs (AC: 1, 3, 6)
  - [x] `BondRecord` struct: `int RoundPurchased`, `float PricePaid`
  - [x] `RevealedTip` struct: `InsiderTipType Type`, `string RevealedText`
  - [x] Place in RunContext.cs or a dedicated `Scripts/Runtime/Shop/StoreDataTypes.cs`
- [x] Task 3: Update ShopState to orchestrate all panels (AC: 7)
  - [x] `ShopState.Enter()`:
    - Reset `CurrentShopRerollCount` to 0
    - Clear `RevealedTips`
    - Generate relic offering (delegate to ShopGenerator)
    - Select available expansions (delegate to ExpansionManager)
    - Generate insider tips (delegate to InsiderTipGenerator)
    - Calculate bond price (delegate to BondManager)
    - Show store UI with all panel data
  - [x] `ShopState.Exit()`:
    - Fire `ShopClosedEvent` with summary of all purchases
    - Hide store UI
  - [x] File: `Scripts/Runtime/Core/GameStates/ShopState.cs`
- [x] Task 4: Update ShopTransaction for multi-panel purchases (AC: 8)
  - [x] Add purchase paths: `PurchaseRelic()`, `PurchaseExpansion()`, `PurchaseTip()`, `PurchaseBond()`, `SellBond()`
  - [x] Each path follows atomic pattern: validate → deduct → apply → fire event
  - [x] Relic/Expansion/Tip purchases deduct Reputation
  - [x] Bond purchase deducts Cash
  - [x] Bond sell adds Cash
  - [x] File: `Scripts/Runtime/Shop/ShopTransaction.cs`
- [x] Task 5: Migrate ActiveItems references (AC: 10)
  - [x] Find all references to `RunContext.ActiveItems` in codebase
  - [x] Replace with `RunContext.OwnedRelics`
  - [x] Update `ItemLookup.cs` if it references ActiveItems
  - [x] Update `ItemInventoryPanel.cs` if it references ActiveItems
  - [x] Update any test files referencing ActiveItems
  - [x] Files: various — search for `ActiveItems`
- [x] Task 6: State persistence validation (AC: 9)
  - [x] Verify all store fields survive round transitions (ShopState → MarketOpenState → ... → ShopState)
  - [x] OwnedRelics: persists across all rounds
  - [x] OwnedExpansions: persists across all rounds
  - [x] BondsOwned + BondPurchaseHistory: persists across all rounds
  - [x] CurrentShopRerollCount: resets each shop visit
  - [x] InsiderTipSlots: persists (modified only by expansion purchase)
  - [x] RevealedTips: cleared each shop visit
- [x] Task 7: Write tests (All AC)
  - [x] RunContext initialization: all fields default correctly
  - [x] ActiveItems → OwnedRelics migration: no broken references
  - [x] ShopState orchestration: all panels receive data
  - [x] ShopTransaction: each purchase path works atomically
  - [x] State persistence: fields survive round transitions correctly
  - [x] Files: `Tests/Runtime/Shop/StoreDataModelTests.cs`, `Tests/Runtime/Core/RunContextStoreTests.cs`

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

Claude Opus 4.6

### Completion Notes List

- Extended RunContext with 7 new store fields: OwnedRelics, OwnedExpansions, BondsOwned, BondPurchaseHistory, CurrentShopRerollCount, InsiderTipSlots, RevealedTips
- Renamed ActiveItems → OwnedRelics across entire codebase (source + 5 test files)
- Created StoreDataTypes.cs with BondRecord, RevealedTip structs and InsiderTipType enum
- Added GameConfig.DefaultInsiderTipSlots = 2
- Updated ShopState.Enter() to reset per-visit state (reroll count, revealed tips) with TODO stubs for future manager delegates
- Refactored ShopTransaction with 5 purchase paths: PurchaseRelic, PurchaseExpansion, PurchaseTip, PurchaseBond, SellBond
- Added Portfolio.AddCash() method for bond sell proceeds
- TryPurchase preserved as backwards-compatible delegate to PurchaseRelic
- All constructor/ResetForNewRun initialization verified for new fields
- Unity batch mode compilation verified — no C# errors (exit code from pre-existing package resolution issue only)
- 50+ tests written covering initialization, persistence, atomic purchases, and reset behavior

### Change Log

- 2026-02-16: Implemented store data model and state management (Epic 13, Story 13.1)
- 2026-02-16: Code review fixes — atomicity rollbacks, MaxRelicSlots/InsiderTipSlots capacity enforcement, ShopExpansionPurchasedEvent, ShopState.Exit safety net

### Senior Developer Review (AI)

**Reviewer:** Iggy (AI-assisted) on 2026-02-16
**Result:** Approved with fixes applied

**Issues Found:** 1 High, 5 Medium, 2 Low (all HIGH/MEDIUM fixed)

- **H1 (FIXED):** PurchaseBond/SellBond catch blocks had no rollback — broke atomicity (AC 8). Added cash restoration in PurchaseBond catch, reordered SellBond to decrement before AddCash with rollback.
- **M1 (FIXED):** PurchaseRelic/PurchaseExpansion added item to list before deducting currency — reordered to deduct-then-apply per AC 8 atomic pattern.
- **M2 (FIXED):** PurchaseTip did not validate against InsiderTipSlots capacity (AC 5). Added slot check returning SlotsFull.
- **M3 (FIXED):** ShopState.Exit() did not fire ShopClosedEvent on forced exit. Added safety net check.
- **M4 (FIXED):** No MaxRelicSlots enforcement (AC 1). Added GameConfig.MaxRelicSlots = 5 and capacity check in PurchaseRelic.
- **M5 (FIXED):** PurchaseExpansion did not fire any event. Added ShopExpansionPurchasedEvent.
- **L1 (NOTED):** Packages/packages-lock.json changed but not in File List.
- **L2 (NOTED):** SellBond returns Error for "no bonds" — conflates business logic with exceptions.

**Tests Added:** 8 new tests covering MaxRelicSlots, InsiderTipSlots capacity, expansion event, and Exit safety net.

### File List

- Assets/Scripts/Runtime/Core/RunContext.cs (modified — renamed ActiveItems to OwnedRelics, added 6 new store fields, updated constructor/reset)
- Assets/Scripts/Runtime/Shop/StoreDataTypes.cs (new — BondRecord, RevealedTip structs, InsiderTipType enum)
- Assets/Scripts/Runtime/Shop/ShopTransaction.cs (modified — added PurchaseRelic, PurchaseExpansion, PurchaseTip, PurchaseBond, SellBond; review: added SlotsFull result, atomicity fixes, capacity checks, rollbacks)
- Assets/Scripts/Runtime/Core/GameStates/ShopState.cs (modified — per-visit state reset in Enter(), TODO stubs for future panels; review: Exit safety net for ShopClosedEvent)
- Assets/Scripts/Runtime/Core/GameEvents.cs (modified — review: added ShopExpansionPurchasedEvent)
- Assets/Scripts/Setup/Data/GameConfig.cs (modified — added DefaultInsiderTipSlots, MaxRelicSlots constants)
- Assets/Scripts/Runtime/Trading/Portfolio.cs (modified — added AddCash method)
- Assets/Scripts/Runtime/UI/ItemInventoryPanel.cs (modified — ActiveItems → OwnedRelics)
- Assets/Scripts/Runtime/Shop/ShopGenerator.cs (no change — already uses parameter)
- Assets/Scripts/Runtime/Items/ItemLookup.cs (no change — no ActiveItems references)
- Assets/Scripts/Setup/UISetup.cs (modified — comment update)
- Assets/Tests/Runtime/Core/RunContextStoreTests.cs (new — 20 tests for store field initialization, persistence, reset)
- Assets/Tests/Runtime/Shop/StoreDataModelTests.cs (new — 38+ tests for all purchase paths, atomicity, persistence, capacity enforcement)
- Assets/Tests/Runtime/Shop/ShopTransactionTests.cs (modified — ActiveItems → OwnedRelics)
- Assets/Tests/Runtime/Core/RunContextTests.cs (modified — ActiveItems → OwnedRelics)
- Assets/Tests/Runtime/Core/GameStates/ShopStateTests.cs (modified — ActiveItems → OwnedRelics; review: Exit safety net tests)
- Assets/Tests/Runtime/Core/GameStates/RunSummaryStateTests.cs (modified — ActiveItems → OwnedRelics)
- Assets/Tests/Runtime/UI/ItemInventoryPanelTests.cs (modified — ActiveItems → OwnedRelics)
