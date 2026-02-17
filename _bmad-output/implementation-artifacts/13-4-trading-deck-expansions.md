# Story 13.4: Trading Deck Expansions Panel (Vouchers)

Status: done

## Story

As a player,
I want a bottom-left panel offering permanent one-time upgrades that expand my trading capabilities,
so that I can invest Reputation into unlocking powerful new mechanics across the run.

## Acceptance Criteria

1. Bottom-left panel labeled "TRADING DECK EXPANSIONS" (or similar thematic name)
2. Displays 2-3 available expansion upgrades per shop visit from the unowned pool
3. Each expansion purchasable only once per run — shows "OWNED" after purchase
4. Expansions persist for the rest of the run (permanent within run)
5. Purchase deducts Reputation
6. Six expansion types defined with distinct effects and costs
7. Each expansion card shows: name, description, cost (Rep icon)
8. `ExpansionPurchasedEvent` fires on purchase

## Expansion Definitions

| Expansion | Effect | Cost (Rep) |
|-----------|--------|------------|
| Multi-Stock Trading | Trade 2 stocks simultaneously per round | 80 |
| Leverage Trading | Trade with 2x leverage (double gains/losses) | 60 |
| Expanded Inventory | +2 relic slots (5 → 7 max) | 50 |
| Dual Short | Short a second stock simultaneously | 70 |
| Intel Expansion | +1 Insider Tip slot per shop visit (2 → 3) | 40 |
| Extended Trading | +15 seconds to round timer | 55 |

## Tasks / Subtasks

- [x] Task 1: Create ExpansionDefinitions data (AC: 6)
  - [x] Define `ExpansionDef` struct: `Id`, `Name`, `Description`, `Cost`
  - [x] Define all 6 expansions as `public static readonly` data
  - [x] File: `Scripts/Setup/Data/ExpansionDefinitions.cs` (NEW)
- [x] Task 2: Create ExpansionManager (AC: 3, 4)
  - [x] Plain C# class (not MonoBehaviour) for testability
  - [x] `OwnedExpansions: List<string>` — tracks purchased expansion IDs
  - [x] `IsOwned(string expansionId)` — check if expansion purchased
  - [x] `Purchase(string expansionId)` — add to owned list
  - [x] `GetAvailableForShop(int count)` — return N random unowned expansions for this shop visit
  - [x] File: `Scripts/Runtime/Shop/ExpansionManager.cs` (NEW)
- [x] Task 3: Expansion panel UI rendering (AC: 1, 2, 7)
  - [x] Populate bottom-left panel (created in 13.2) with 2-3 expansion cards
  - [x] Each card: expansion name, description, cost with Rep icon, purchase button
  - [x] "OWNED" watermark/state for purchased expansions still visible
  - [x] Disable purchase button if insufficient Rep
  - [x] File: `Scripts/Runtime/UI/ShopUI.cs` — expansion panel population
- [x] Task 4: Purchase flow (AC: 3, 5, 8)
  - [x] Purchase button click: validate affordability + not owned → deduct Rep → mark owned → fire event
  - [x] Card transitions to "OWNED" state after purchase
  - [x] `ExpansionPurchasedEvent` with expansion ID and remaining Rep
  - [x] File: `Scripts/Runtime/Shop/ShopTransaction.cs` — expansion purchase path (pre-existed from 13.1, no modification needed)
- [x] Task 5: Wire to RunContext (AC: 4)
  - [x] `RunContext.OwnedExpansions: List<string>` (set up in 13.1)
  - [x] ExpansionManager reads/writes through RunContext
  - [x] Expansions survive round transitions
  - [x] File: `Scripts/Runtime/Core/RunContext.cs`
- [x] Task 6: GameConfig constants (AC: 6)
  - [x] `ExpansionCostMultiStock = 80`
  - [x] `ExpansionCostLeverage = 60`
  - [x] `ExpansionCostExpandedInventory = 50`
  - [x] `ExpansionCostDualShort = 70`
  - [x] `ExpansionCostIntelExpansion = 40`
  - [x] `ExpansionCostExtendedTrading = 55`
  - [x] File: `Scripts/Setup/Data/GameConfig.cs`
- [x] Task 7: GameEvents update (AC: 8)
  - [x] Define `ExpansionPurchasedEvent`: ExpansionId, ExpansionName, Cost, RemainingReputation
  - [x] File: `Scripts/Runtime/Core/GameEvents.cs`
- [x] Task 8: Write tests (All AC)
  - [x] ExpansionManager: purchase marks owned, can't double-buy, available pool shrinks
  - [x] GetAvailableForShop: returns correct count, excludes owned
  - [x] Purchase flow: Rep deducted, event fired, persists across rounds
  - [x] Files: `Tests/Runtime/Shop/ExpansionManagerTests.cs`

## Dev Notes

### Architecture Compliance

- **Plain C# for logic:** `ExpansionManager` is NOT a MonoBehaviour — testable plain C# owned by RunContext or ShopState.
- **Static data:** All expansion definitions in `Scripts/Setup/Data/ExpansionDefinitions.cs` as `public static readonly`.
- **No ScriptableObjects.**
- **Expansion effects are NOT implemented here** — that is Story 13.7. This story only handles the data model, UI, and purchase flow. The expansions are "owned" but have no gameplay impact until 13.7.

### Existing Code to Understand

Before implementing, the dev agent MUST read:
- `Scripts/Runtime/UI/ShopUI.cs` — where expansion panel content goes (after 13.2)
- `Scripts/Runtime/Shop/ShopTransaction.cs` — purchase flow pattern to follow
- `Scripts/Runtime/Core/RunContext.cs` — how to add OwnedExpansions
- `Scripts/Runtime/Core/GameEvents.cs` — event definition pattern

### Depends On

- Story 13.2 (Store Layout Shell) — expansion panel must exist
- Story 13.1 (Data Model) — `OwnedExpansions` field in RunContext

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Completion Notes List

- Created `ExpansionDef` struct and `ExpansionDefinitions` static class with all 6 expansion types, costs sourced from GameConfig
- Created `ExpansionManager` as plain C# class (not MonoBehaviour) wrapping RunContext.OwnedExpansions with IsOwned, Purchase, and GetAvailableForShop (Fisher-Yates shuffle for random selection)
- Added expansion card UI rendering to ShopUI: dynamically creates cards inside the existing expansions panel with name, description, cost (Rep icon), and purchase button; shows "OWNED" state after purchase; disables button when insufficient Rep
- Wired expansion purchase flow in ShopState: generates expansion offering via ExpansionManager, handles purchase via ShopTransaction.PurchaseExpansion (already existed from 13.1), fires ShopExpansionPurchasedEvent
- Added 7 GameConfig constants for expansion costs + ExpansionsPerShopVisit
- Task 7 (GameEvents): ShopExpansionPurchasedEvent already existed from Story 13.1 with matching fields (ExpansionId, DisplayName, Cost, RemainingReputation) — no changes needed
- Task 5 (RunContext): OwnedExpansions already existed from Story 13.1 — ExpansionManager delegates to it
- Updated ShopState to track expansion purchase count in ShopClosedEvent and set ExpansionsAvailable flag in ShopOpenedEvent
- Wrote 17 tests covering: ExpansionDefinitions data integrity, IsOwned, Purchase idempotency, GetAvailableForShop count/exclusion/exhaustion, purchase flow integration via ShopTransaction, and round persistence
- Unity batch mode compilation verified: 0 errors, 0 new warnings

### Change Log

- 2026-02-16: Implemented Story 13.4 — Trading Deck Expansions panel with data model, manager, UI, purchase flow, and tests
- 2026-02-16: Code review fixes — (H1) Fixed CloseShop ShopClosedEvent.ExpansionsPurchased using _expansionsPurchasedCount instead of hardcoded 0; (M1/M4) Removed dead ExpansionManager.Purchase() — ShopTransaction.PurchaseExpansion is the single purchase path; (M2) Fixed ShowExpansions to store _ctx for RefreshExpansionAffordability; Updated tests to match

### File List

- Assets/Scripts/Setup/Data/ExpansionDefinitions.cs (NEW)
- Assets/Scripts/Setup/Data/GameConfig.cs (MODIFIED)
- Assets/Scripts/Runtime/Shop/ExpansionManager.cs (NEW)
- Assets/Scripts/Runtime/UI/ShopUI.cs (MODIFIED)
- Assets/Scripts/Runtime/Core/GameStates/ShopState.cs (MODIFIED)
- Assets/Tests/Runtime/Shop/ExpansionManagerTests.cs (NEW)
