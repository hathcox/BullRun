# Story FIX-12: Reputation Shop Currency

Status: ready-for-dev

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

- [ ] Task 1: Create ReputationManager class (AC: 1, 2, 12)
  - [ ] New file: `Assets/Scripts/Runtime/Core/ReputationManager.cs`
  - [ ] Simple class with: `int Current { get; private set; }`, `void Add(int amount)`, `void Spend(int amount)`, `bool CanAfford(int cost)`, `void Reset()`
  - [ ] Not a MonoBehaviour — plain C# class, instantiated by GameRunner or RunContext
  - [ ] Reputation is an integer (no fractional Rep)
  - [ ] Initialize to 0 on new run

- [ ] Task 2: Wire ReputationManager into RunContext / GameRunner (AC: 1, 2)
  - [ ] Add `ReputationManager` field to RunContext (or GameRunner if RunContext doesn't exist as a class)
  - [ ] Initialize on run start: `new ReputationManager()` with 0 Rep
  - [ ] Accessible to ShopState and ShopUI for purchase validation
  - [ ] File: `Assets/Scripts/Runtime/Core/GameRunner.cs`

- [ ] Task 3: Update ShopUI purchase flow — Rep instead of Cash (AC: 3, 4, 5, 11)
  - [ ] In `ShopUI.OnItemPurchased()`: replace `Portfolio.CanAfford(cost)` → `ReputationManager.CanAfford(cost)`
  - [ ] Replace `Portfolio.DeductCash(cost)` → `ReputationManager.Spend(cost)`
  - [ ] Item card cost labels: show Rep icon/symbol instead of "$" (e.g., "★ 15" or "15 Rep")
  - [ ] BUY button dimmed when `!ReputationManager.CanAfford(cost)`
  - [ ] Verify: Portfolio.Cash is NOT touched by any shop code path
  - [ ] Files: Shop UI scripts (find exact file — likely in UISetup or a ShopUI component)

- [ ] Task 4: Update ShopOpenedEvent and ShopItemPurchasedEvent (AC: 9, 10)
  - [ ] `ShopOpenedEvent`: add `int Reputation` field (or replace cash field)
  - [ ] `ShopItemPurchasedEvent`: change `RemainingCash` to `RemainingReputation` (or add Rep field)
  - [ ] Update all publishers and subscribers of these events
  - [ ] File: `Assets/Scripts/Runtime/Core/GameEvents.cs`

- [ ] Task 5: Update Shop UI header to show Reputation balance (AC: 6, 8)
  - [ ] In `UISetup.ExecuteShopUI()`: replace the cash display text with Reputation display
  - [ ] Format: "★ 42" or "42 REP" in gold/amber color
  - [ ] Update dynamically when items purchased (Rep decreases)
  - [ ] File: `Assets/Scripts/Setup/UISetup.cs`

- [ ] Task 6: Add Reputation display to Trading HUD (AC: 7, 8)
  - [ ] Add compact Reputation counter to the top HUD bar (alongside Cash, Portfolio Value, etc.)
  - [ ] Format: small star icon + number, amber/gold color (#FFB300 or similar)
  - [ ] Updates when Reputation changes (awarded at round end via FIX-14)
  - [ ] Position: right side of top bar or below cash display — visually distinct
  - [ ] File: `Assets/Scripts/Setup/UISetup.cs`

- [ ] Task 7: Add Reputation config constants to GameConfig (AC: 3)
  - [ ] Add `StartingReputation = 0` — Rep at run start
  - [ ] Item Rep costs can be defined here or in item definitions — for now, placeholder constants or a simple mapping
  - [ ] Note: actual Rep EARNING logic is in FIX-14 — this story only handles tracking and spending
  - [ ] File: `Assets/Scripts/Setup/Data/GameConfig.cs`

- [ ] Task 8: Remove Portfolio.DeductCash from shop path (AC: 5)
  - [ ] Audit all code paths where shop purchases call Portfolio methods
  - [ ] Remove or guard any `Portfolio.DeductCash()` calls from shop flow
  - [ ] `Portfolio.DeductCash()` method can remain (may be used elsewhere) but shop must NOT call it
  - [ ] Files: Shop-related scripts, GameRunner shop integration

- [ ] Task 9: Write tests (AC: 1-12)
  - [ ] Test: ReputationManager.Add increases balance correctly
  - [ ] Test: ReputationManager.Spend decreases balance correctly
  - [ ] Test: ReputationManager.CanAfford returns true/false correctly
  - [ ] Test: ReputationManager.Spend rejects if insufficient (balance unchanged)
  - [ ] Test: Shop purchase deducts Reputation, NOT Portfolio.Cash
  - [ ] Test: Portfolio.Cash unchanged after shop purchase
  - [ ] Test: BUY button disabled when insufficient Reputation
  - [ ] Test: Reputation starts at 0 on new run
  - [ ] Test: Reputation persists across rounds (not reset on round start)
  - [ ] File: `Assets/Tests/Runtime/Core/ReputationManagerTests.cs`

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
_To be filled during implementation_

### Completion Notes
_To be filled after implementation_

### Debug Log
_To be filled during implementation_

## File List

_To be filled during implementation_

## Change Log

- 2026-02-14: Story created — Reputation as shop currency, decoupled from trading cash
