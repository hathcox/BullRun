# Story 13.3: Trading Deck Expansions Panel (Vouchers)

Status: pending

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

- [ ] Task 1: Create ExpansionDefinitions data (AC: 6)
  - [ ] Define `ExpansionDef` struct: `Id`, `Name`, `Description`, `Cost`
  - [ ] Define all 6 expansions as `public static readonly` data
  - [ ] File: `Scripts/Setup/Data/ExpansionDefinitions.cs` (NEW)
- [ ] Task 2: Create ExpansionManager (AC: 3, 4)
  - [ ] Plain C# class (not MonoBehaviour) for testability
  - [ ] `OwnedExpansions: List<string>` — tracks purchased expansion IDs
  - [ ] `IsOwned(string expansionId)` — check if expansion purchased
  - [ ] `Purchase(string expansionId)` — add to owned list
  - [ ] `GetAvailableForShop(int count)` — return N random unowned expansions for this shop visit
  - [ ] File: `Scripts/Runtime/Shop/ExpansionManager.cs` (NEW)
- [ ] Task 3: Expansion panel UI rendering (AC: 1, 2, 7)
  - [ ] Populate bottom-left panel (created in 13.1) with 2-3 expansion cards
  - [ ] Each card: expansion name, description, cost with Rep icon, purchase button
  - [ ] "OWNED" watermark/state for purchased expansions still visible
  - [ ] Disable purchase button if insufficient Rep
  - [ ] File: `Scripts/Runtime/UI/ShopUI.cs` — expansion panel population
- [ ] Task 4: Purchase flow (AC: 3, 5, 8)
  - [ ] Purchase button click: validate affordability + not owned → deduct Rep → mark owned → fire event
  - [ ] Card transitions to "OWNED" state after purchase
  - [ ] `ExpansionPurchasedEvent` with expansion ID and remaining Rep
  - [ ] File: `Scripts/Runtime/Shop/ShopTransaction.cs` — expansion purchase path
- [ ] Task 5: Wire to RunContext (AC: 4)
  - [ ] `RunContext.OwnedExpansions: List<string>` (set up in 13.6)
  - [ ] ExpansionManager reads/writes through RunContext
  - [ ] Expansions survive round transitions
  - [ ] File: `Scripts/Runtime/Core/RunContext.cs`
- [ ] Task 6: GameConfig constants (AC: 6)
  - [ ] `ExpansionCostMultiStock = 80`
  - [ ] `ExpansionCostLeverage = 60`
  - [ ] `ExpansionCostExpandedInventory = 50`
  - [ ] `ExpansionCostDualShort = 70`
  - [ ] `ExpansionCostIntelExpansion = 40`
  - [ ] `ExpansionCostExtendedTrading = 55`
  - [ ] File: `Scripts/Setup/Data/GameConfig.cs`
- [ ] Task 7: GameEvents update (AC: 8)
  - [ ] Define `ExpansionPurchasedEvent`: ExpansionId, ExpansionName, Cost, RemainingReputation
  - [ ] File: `Scripts/Runtime/Core/GameEvents.cs`
- [ ] Task 8: Write tests (All AC)
  - [ ] ExpansionManager: purchase marks owned, can't double-buy, available pool shrinks
  - [ ] GetAvailableForShop: returns correct count, excludes owned
  - [ ] Purchase flow: Rep deducted, event fired, persists across rounds
  - [ ] Files: `Tests/Runtime/Shop/ExpansionManagerTests.cs`

## Dev Notes

### Architecture Compliance

- **Plain C# for logic:** `ExpansionManager` is NOT a MonoBehaviour — testable plain C# owned by RunContext or ShopState.
- **Static data:** All expansion definitions in `Scripts/Setup/Data/ExpansionDefinitions.cs` as `public static readonly`.
- **No ScriptableObjects.**
- **Expansion effects are NOT implemented here** — that is Story 13.7. This story only handles the data model, UI, and purchase flow. The expansions are "owned" but have no gameplay impact until 13.7.

### Existing Code to Understand

Before implementing, the dev agent MUST read:
- `Scripts/Runtime/UI/ShopUI.cs` — where expansion panel content goes (after 13.1)
- `Scripts/Runtime/Shop/ShopTransaction.cs` — purchase flow pattern to follow
- `Scripts/Runtime/Core/RunContext.cs` — how to add OwnedExpansions
- `Scripts/Runtime/Core/GameEvents.cs` — event definition pattern

### Depends On

- Story 13.1 (Store Layout Shell) — expansion panel must exist
- Story 13.6 (Data Model) — `OwnedExpansions` field in RunContext

## Dev Agent Record

### Agent Model Used

### Completion Notes List

### Change Log

### File List
