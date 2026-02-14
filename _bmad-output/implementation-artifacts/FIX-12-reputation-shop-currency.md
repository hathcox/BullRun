# Story FIX-12: Reputation Shop Currency

Status: done

## Story

As a player,
I want the draft shop to use a separate currency called "Reputation" instead of my trading cash,
so that shop purchases don't eat into my run capital and I can manage build upgrades independently from in-round trading.

## Problem Analysis

Currently the draft shop deducts item costs from `Portfolio.Cash` via `Portfolio.DeductCash(cost)`. This creates tension between upgrading and having capital for the next round — but with the new $10 economy (FIX-14), that tension becomes punishing: buying a single item could wipe out your entire trading capital. Reputation decouples shop purchasing power from trading cash entirely.

**Current Shop Purchase Flow:**
1. `ShopState` opens → `ShopOpenedEvent` published with `Portfolio.Cash`
2. `ShopUI` displays items with costs in $ (cash)
3. Player clicks BUY → `ShopUI.OnItemPurchased()` calls `Portfolio.CanAfford(cost)` then `Portfolio.DeductCash(cost)`
4. `ShopItemPurchasedEvent` published with remaining cash
5. Cash display updates — trading capital reduced

**Desired Flow:**
1. `ShopState` opens → `ShopOpenedEvent` published with Reputation balance (not cash)
2. `ShopUI` displays items with costs in Rep (reputation icon)
3. Player clicks BUY → checks `ReputationManager.CanAfford(cost)` then `ReputationManager.Spend(cost)`
4. `ShopItemPurchasedEvent` published with remaining Rep
5. Reputation display updates — trading cash is UNTOUCHED

**Affected Code:**
- `Scripts/Setup/Data/GameConfig.cs` — Reputation-related constants
- `Scripts/Runtime/Core/GameRunner.cs` — Reputation initialization, round-end award, HUD updates
- `Scripts/Runtime/Core/GameEvents.cs` — update ShopOpenedEvent/ShopItemPurchasedEvent to carry Rep
- `Scripts/Setup/UISetup.cs` — Reputation display in HUD and shop, replace $ with Rep icon
- Shop-related state/UI — replace Portfolio.DeductCash with ReputationManager.Spend

## Acceptance Criteria

1. New `ReputationManager` class (or simple static tracker) tracks current Reputation as an integer
2. Reputation persists across rounds within a run
3. Draft Shop item prices are in Reputation, not cash
4. Shop purchase flow calls `ReputationManager.Spend()` instead of `Portfolio.DeductCash()`
5. `Portfolio.Cash` is NEVER reduced by shop purchases — full cash carries forward between rounds
6. Shop UI displays current Reputation balance (gold/amber, star icon) instead of cash
7. Trading HUD shows Reputation alongside cash in a compact display (e.g., top bar)
8. Reputation display uses distinct visual style from cash (amber/gold color, star/badge icon)
9. `ShopOpenedEvent` carries Reputation balance instead of (or in addition to) cash
10. `ShopItemPurchasedEvent` carries remaining Reputation
11. Item BUY buttons dim when insufficient Reputation (not cash)
12. Reputation starts at 0 for a new run (awarded at round end — see FIX-14)

## Tasks / Subtasks

- [x] Task 1: Create ReputationManager class (AC: 1, 2, 12)
  - [x] New file: `Assets/Scripts/Runtime/Core/ReputationManager.cs`
  - [x] Simple class with: `int Current { get; private set; }`, `void Add(int amount)`, `void Spend(int amount)`, `bool CanAfford(int cost)`, `void Reset()`
  - [x] Not a MonoBehaviour — plain C# class, instantiated by GameRunner or RunContext
  - [x] Reputation is an integer (no fractional Rep)
  - [x] Initialize to 0 on new run

- [x] Task 2: Wire ReputationManager into RunContext / GameRunner (AC: 1, 2)
  - [x] Add `ReputationManager` field to RunContext (or GameRunner if RunContext doesn't exist as a class)
  - [x] Initialize on run start: `new ReputationManager()` with 0 Rep
  - [x] Accessible to ShopState and ShopUI for purchase validation
  - [x] File: `Assets/Scripts/Runtime/Core/RunContext.cs`

- [x] Task 3: Update ShopUI purchase flow — Rep instead of Cash (AC: 3, 4, 5, 11)
  - [x] In `ShopUI.SetupCard()`: replace `Portfolio.CanAfford(cost)` → `Reputation.CanAfford(cost)`
  - [x] In `ShopTransaction.TryPurchase()`: replace `Portfolio.DeductCash(cost)` → `Reputation.Spend(cost)`
  - [x] Item card cost labels: show Rep icon/symbol instead of "$" (e.g., "★ 15")
  - [x] BUY button dimmed when `!Reputation.CanAfford(cost)`
  - [x] Verify: Portfolio.Cash is NOT touched by any shop code path
  - [x] Files: `Assets/Scripts/Runtime/UI/ShopUI.cs`, `Assets/Scripts/Runtime/Shop/ShopTransaction.cs`

- [x] Task 4: Update ShopOpenedEvent and ShopItemPurchasedEvent (AC: 9, 10)
  - [x] `ShopOpenedEvent`: replaced `CurrentCash` with `CurrentReputation` (int)
  - [x] `ShopItemPurchasedEvent`: replaced `RemainingCash` with `RemainingReputation` (int)
  - [x] `ShopClosedEvent`: replaced `CashRemaining` with `ReputationRemaining` (int)
  - [x] Updated all publishers (ShopState, ShopTransaction) and subscribers
  - [x] File: `Assets/Scripts/Runtime/Core/GameEvents.cs`

- [x] Task 5: Update Shop UI header to show Reputation balance (AC: 6, 8)
  - [x] In `UISetup.ExecuteShopUI()`: replaced cash display with Reputation display
  - [x] Format: "★ 42" in amber/gold color (ShopUI.ReputationColor)
  - [x] Updates dynamically when items purchased (Rep decreases)
  - [x] File: `Assets/Scripts/Setup/UISetup.cs`

- [x] Task 6: Add Reputation display to Trading HUD (AC: 7, 8)
  - [x] Added compact Reputation counter to the top HUD bar (Section 5: REP)
  - [x] Format: star icon + number, amber/gold color (ShopUI.ReputationColor)
  - [x] Updates when Reputation changes (via TradingHUD.RefreshDisplay)
  - [x] Position: right side of top bar, after Target section — visually distinct
  - [x] Files: `Assets/Scripts/Setup/UISetup.cs`, `Assets/Scripts/Runtime/UI/TradingHUD.cs`

- [x] Task 7: Add Reputation config constants to GameConfig (AC: 3)
  - [x] Added `StartingReputation = 0` — Rep at run start
  - [x] Item costs remain as-is (1:1 mapping to Rep), tuning deferred to FIX-14
  - [x] File: `Assets/Scripts/Setup/Data/GameConfig.cs`

- [x] Task 8: Remove Portfolio.DeductCash from shop path (AC: 5)
  - [x] Audited all code paths: ShopTransaction.TryPurchase was the only shop→Portfolio.DeductCash caller
  - [x] Replaced with ReputationManager.Spend in ShopTransaction
  - [x] Portfolio.DeductCash() method retained (used by trading/position code)
  - [x] Files: `Assets/Scripts/Runtime/Shop/ShopTransaction.cs`

- [x] Task 9: Write tests (AC: 1-12)
  - [x] Test: ReputationManager.Add increases balance correctly
  - [x] Test: ReputationManager.Spend decreases balance correctly
  - [x] Test: ReputationManager.CanAfford returns true/false correctly
  - [x] Test: ReputationManager.Spend rejects if insufficient (balance unchanged)
  - [x] Test: Shop purchase deducts Reputation, NOT Portfolio.Cash
  - [x] Test: Portfolio.Cash unchanged after shop purchase
  - [x] Test: CanAfford logic underlies BUY button disabled when insufficient Reputation
  - [x] Test: Reputation starts at 0 on new run
  - [x] Test: Reputation persists across rounds (not reset on round start)
  - [x] File: `Assets/Tests/Runtime/Core/ReputationManagerTests.cs`
  - [x] Updated existing tests: `ShopTransactionTests.cs`, `ShopStateTests.cs`

## Dev Notes

### Architecture Compliance
- **ReputationManager** is a plain C# class — no MonoBehaviour, no Inspector config. Owned by RunContext or GameRunner.
- **EventBus Communication:** Existing shop events updated to carry Rep. No new event types needed unless decoupling requires it.
- **Setup-Oriented:** Rep HUD display created by UISetup alongside existing HUD elements.

### Key Design Decisions
- **Integer currency:** Reputation is whole numbers only. Simpler display, no rounding issues. Item costs are integer Rep values.
- **Separate from Portfolio:** ReputationManager is NOT part of Portfolio. Portfolio handles trading cash and positions. ReputationManager handles meta/shop currency. Clean separation.
- **No meta-persistence yet:** This story handles within-run Reputation only. Cross-run persistence (saving Rep to disk) is Epic 9 territory. ReputationManager resets to 0 each new run.
- **Rep earning is FIX-14:** This story creates the system to TRACK and SPEND Rep. FIX-14 adds the logic to EARN it at round end. If FIX-12 ships first, Rep stays at 0 until FIX-14 is implemented (shop items unaffordable — that's fine for dev/test, can temporarily seed with debug Rep).
- **Item cost conversion:** Existing item costs are in $ (cash). Need to convert to Rep values. Rough mapping TBD during implementation — start with simple 1:1 or round numbers.

### Dependencies
- **FIX-14 (economy rebalance):** Provides the Rep EARNING mechanism. Without it, Rep stays at 0. Can stub with debug/test Rep for development.
- **FIX-13 (quantity unlocks):** Will add new shop items that cost Rep. This story just ensures the shop currency IS Rep.
- No new packages required.

### Edge Cases
- **0 Rep at run start:** All shop items unaffordable until FIX-14 awards Rep. This is correct — first shop visit after Round 1 uses Rep earned from Round 1.
- **All items too expensive:** Player skips shop (clicks Continue). No issue — cash preserved.
- **Negative Rep:** ReputationManager.Spend should reject if amount > Current. Never go negative.

## Dev Agent Record

### Implementation Plan
- **Approach:** Tasks implemented in story order (1-9). Red-green-refactor adapted per project rules: tests written but not executed by agent (Unity Test Framework requires manual run in Editor).
- **Key Integration Points:** ShopTransaction.TryPurchase uses Portfolio.CanAfford/DeductCash — will switch to ReputationManager. ShopUI.UpdateCashDisplay/SetupCard reference Portfolio.Cash — will switch to ReputationManager.Current. ShopState.Enter publishes ShopOpenedEvent with Portfolio.Cash — will carry Rep. ShopUI.RefreshAfterPurchase checks Portfolio.CanAfford — will switch to ReputationManager.
- **Item Cost Conversion:** Existing item costs (150-500) are already integers. Will keep same cost values as Rep costs (1:1 mapping) since Rep earning rates will be tuned in FIX-14.
- **ReputationManager Location:** Plain C# class in Scripts/Runtime/Core/ — owned by RunContext, not MonoBehaviour.
- **HUD Display:** Amber/gold star icon + number in top bar, visually distinct from green cash display.

### Completion Notes
- Created ReputationManager as a plain C# class owned by RunContext (not MonoBehaviour)
- Fully replaced shop currency from Portfolio.Cash to Reputation across all shop code paths
- ShopTransaction.TryPurchase now validates and deducts Reputation, never touches Portfolio.Cash
- ShopUI displays costs as "★ {cost}" with amber/gold color, checks Reputation.CanAfford for affordability
- All 3 shop events (ShopOpenedEvent, ShopItemPurchasedEvent, ShopClosedEvent) updated to carry Reputation fields
- Trading HUD now includes a REP section (Section 5) with star icon in amber/gold
- 20 new unit tests in ReputationManagerTests covering all ACs
- Updated 2 existing test files (ShopTransactionTests, ShopStateTests) for Reputation integration
- Portfolio.DeductCash() retained for trading use, but completely removed from shop path

### Debug Log
- No issues encountered during implementation. Clean separation between Reputation and Portfolio cash.

## File List

- `Assets/Scripts/Runtime/Core/ReputationManager.cs` (NEW)
- `Assets/Scripts/Runtime/Core/RunContext.cs` (MODIFIED — added Reputation property, init in constructor + ResetForNewRun)
- `Assets/Scripts/Runtime/Core/GameEvents.cs` (MODIFIED — ShopOpenedEvent, ShopItemPurchasedEvent, ShopClosedEvent fields)
- `Assets/Scripts/Setup/Data/GameConfig.cs` (MODIFIED — added StartingReputation constant)
- `Assets/Scripts/Runtime/Shop/ShopTransaction.cs` (MODIFIED — Reputation instead of Portfolio.Cash)
- `Assets/Scripts/Runtime/UI/ShopUI.cs` (MODIFIED — Reputation display, CanAfford checks, star icon costs)
- `Assets/Scripts/Runtime/UI/TradingHUD.cs` (MODIFIED — added Reputation display section)
- `Assets/Scripts/Setup/UISetup.cs` (MODIFIED — Rep display in shop + HUD Section 5)
- `Assets/Scripts/Runtime/Core/GameStates/ShopState.cs` (MODIFIED — event publishers updated for Rep)
- `Assets/Tests/Runtime/Core/ReputationManagerTests.cs` (NEW — 20 tests)
- `Assets/Tests/Runtime/Shop/ShopTransactionTests.cs` (MODIFIED — updated for Reputation)
- `Assets/Tests/Runtime/Core/GameStates/ShopStateTests.cs` (MODIFIED — updated for Reputation)

## Change Log

- 2026-02-14: Story created — Reputation as shop currency, decoupled from trading cash
- 2026-02-14: Implementation complete — All 9 tasks done, 20 new tests + updated existing tests
- 2026-02-14: Code review fixes — H1: Fixed CanAfford/Spend API inconsistency for zero-cost items; H2: Added Reputation rollback to ShopTransaction catch block; M1: ReputationManager now reads GameConfig.StartingReputation; M2: Corrected test count (20, not 22); L1: Fixed stale UISetup doc comment
