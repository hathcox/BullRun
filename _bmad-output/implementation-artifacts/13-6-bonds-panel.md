# Story 13.6: Bonds Panel (Reputation Investment)

Status: done

## Story

As a player,
I want a bonds section where I can invest cash now to earn recurring Reputation in future rounds,
so that I have a long-term investment strategy alongside my immediate trading.

## Acceptance Criteria

1. Bottom-right panel labeled "BONDS"
2. Displays 1 bond available for purchase each shop visit
3. Bond purchase uses CASH (not Reputation) — this is an investment of trading capital
4. Bond price increases every round: $3, $5, $8, $12, $17, $23, $30 (Round 1-7)
5. Cannot purchase bonds on Round 8 (no future rounds to earn from)
6. Each bond owned generates +1 Rep at the START of each subsequent round (cumulative)
7. Example: Buy bond R1 → R2 start: +1 Rep. Buy another R2 → R3 start: +2 Rep. R4: +2 Rep. Etc.
8. Bond count displayed: "Bonds Owned: X" with projected Rep earnings
9. Sell bonds for half the original purchase price (cash back)
10. Selling removes 1 bond, reducing future Rep earnings
11. Sell button visible when bonds > 0
12. Confirmation prompt before selling ("Sell 1 bond for $X?")
13. Bond panel shows: current bond price, bonds owned, Rep earned per round, sell price
14. Bond Rep awarded at round START (before trading), displayed as "+X Rep from Bonds" during market open
15. `BondPurchasedEvent`, `BondSoldEvent`, `BondRepPaidEvent` fire on respective actions

## Tasks / Subtasks

- [x] Task 1: Create BondManager (AC: 3, 4, 5, 6, 7, 9, 10)
  - [x] Plain C# class (not MonoBehaviour)
  - [x] `BondsOwned: int` — total bonds held
  - [x] `BondPurchaseHistory: List<BondRecord>` — tracks round purchased and price paid per bond
  - [x] `BondRecord` struct: `RoundPurchased`, `PricePaid`
  - [x] `GetCurrentPrice(int currentRound)` — lookup from `BondPricePerRound[]`
  - [x] `CanPurchase(int currentRound, float currentCash)` — not Round 8, can afford
  - [x] `Purchase(int currentRound, Portfolio portfolio)` — deduct cash, add bond record
  - [x] `GetSellPrice()` — average of all bond purchase prices × `BondSellMultiplier` (or sell most recent for half its price)
  - [x] `Sell(Portfolio portfolio)` — remove 1 bond (LIFO: most recent), return cash
  - [x] `GetRepPerRound()` — returns `BondsOwned * BondRepPerRoundPerBond`
  - [x] `PayoutRep(ReputationManager rep)` — add Rep payout at round start
  - [x] File: `Scripts/Runtime/Shop/BondManager.cs` (NEW)
- [x] Task 2: Bond panel UI (AC: 1, 2, 8, 11, 13)
  - [x] Populate bottom-right panel (created in 13.2)
  - [x] Bond card: shows current round's bond price, "BUY BOND" button
  - [x] Info display: "Bonds Owned: X", "Earning: +Y Rep/round"
  - [x] Sell button (visible when bonds > 0): "SELL BOND ($Z)"
  - [x] Disable buy button if can't afford or Round 8
  - [x] Round 8: panel shows "NO BONDS AVAILABLE" or bonds owned info only
  - [x] File: `Scripts/Runtime/UI/ShopUI.cs` — bond panel population
- [x] Task 3: Purchase flow (AC: 3, 15)
  - [x] Buy button: validate round + cash → deduct cash via Portfolio → add bond → fire `BondPurchasedEvent`
  - [x] Update UI immediately: bonds owned count, Rep projection, cash display
  - [x] File: `Scripts/Runtime/Shop/ShopTransaction.cs` — bond purchase path
- [x] Task 4: Sell flow (AC: 9, 10, 11, 12, 15)
  - [x] Sell button: show confirmation prompt ("Sell 1 bond for $X?")
  - [x] On confirm: remove 1 bond (LIFO), add cash to Portfolio, fire `BondSoldEvent`
  - [x] Update UI: bonds owned, Rep projection, cash display, sell button visibility
  - [x] Hide sell button when bonds reach 0
  - [x] File: `Scripts/Runtime/Shop/ShopTransaction.cs`, `Scripts/Runtime/UI/ShopUI.cs`
- [x] Task 5: Round-start Rep payout (AC: 6, 7, 14)
  - [x] In `MarketOpenState.Enter()`: check BondManager, call `PayoutRep()` if bonds > 0
  - [x] Display "+X Rep from Bonds" text during market open phase (brief overlay or integrated into market open info)
  - [x] Rep added to ReputationManager before trading begins
  - [x] File: `Scripts/Runtime/Core/GameStates/MarketOpenState.cs`
- [x] Task 6: Wire to RunContext (AC: 6, 7)
  - [x] BondManager state stored in RunContext (bonds owned, purchase history)
  - [x] Survives round transitions
  - [x] File: `Scripts/Runtime/Core/RunContext.cs`
- [x] Task 7: GameConfig constants (AC: 4, 9)
  - [x] `BondPricePerRound = new int[] { 3, 5, 8, 12, 17, 23, 30, 0 }` (index 7 = Round 8 = 0/unavailable)
  - [x] `BondSellMultiplier = 0.5f`
  - [x] `BondRepPerRoundPerBond = 1`
  - [x] File: `Scripts/Setup/Data/GameConfig.cs`
- [x] Task 8: GameEvents (AC: 15)
  - [x] `BondPurchasedEvent`: Round, PricePaid, TotalBondsOwned, RemainingCash
  - [x] `BondSoldEvent`: SellPrice, TotalBondsOwned, CashAfterSale
  - [x] `BondRepPaidEvent`: BondsOwned, RepEarned, TotalReputation
  - [x] File: `Scripts/Runtime/Core/GameEvents.cs`
- [x] Task 9: Write tests (All AC)
  - [x] BondManager: purchase deducts cash, price escalation correct, sell returns half, Rep payout math
  - [x] Round 8 blocking: cannot purchase on final round
  - [x] Cumulative Rep: buy R1 + R2, verify R3 payout = +2 Rep
  - [x] Sell LIFO: selling removes most recent bond
  - [x] Files: `Tests/Runtime/Shop/BondManagerTests.cs`

## Dev Notes

### Architecture Compliance

- **Plain C# for logic:** `BondManager` is NOT a MonoBehaviour — pure C# for testability.
- **Cash, not Reputation:** Bonds are the ONE store section that costs trading cash. This creates a genuine tension: spend cash on a bond now (losing trading capital) to earn more Rep over time.
- **Atomic transactions:** Purchase validates cash → deducts → records → fires event. Sell validates bonds > 0 → removes → adds cash → fires event.

### Bond Economy Design

The bond prices ($3, $5, $8, $12, $17, $23, $30) are designed to create escalating tension:
- **Early bonds are cheap** ($3 in R1 with $10 starting capital) but the opportunity cost is high — that's 30% of your trading capital
- **Late bonds are expensive** ($30 in R7) but by then you have more cash, and fewer rounds remain to earn Rep
- **Cumulative payout** rewards early investment: a R1 bond earns 7 total Rep (R2-R8), while a R7 bond earns only 1 Rep (R8)
- **Sell at half price** provides an emergency cash valve — if you need capital desperately, you can liquidate bonds at a loss

### Critical: Cash vs Rep Display

The bond panel must make it VERY clear that bonds cost CASH (green dollar), not Reputation (amber star). This is the only store section using cash. Confusion here would be a significant UX issue.

### Existing Code to Understand

Before implementing, the dev agent MUST read:
- `Scripts/Runtime/Trading/Portfolio.cs` — cash deduction/addition API
- `Scripts/Runtime/Core/ReputationManager.cs` — Rep addition API
- `Scripts/Runtime/Core/GameStates/MarketOpenState.cs` — where to hook bond payout
- `Scripts/Runtime/Core/RunContext.cs` — where to store bond state
- `Scripts/Setup/Data/GameConfig.cs` — constants pattern

### Depends On

- Story 13.2 (Store Layout Shell) — bonds panel must exist
- Story 13.1 (Data Model) — `BondsOwned`, `BondPurchaseHistory` in RunContext

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Completion Notes List

- Created `BondManager` as a plain C# class wrapping RunContext bond state, with `Purchase`, `Sell` (LIFO), `GetCurrentPrice`, `CanPurchase`, `GetSellPrice`, `GetRepPerRound`, and `PayoutRep` methods.
- Added bond constants to `GameConfig`: `BondPricePerRound`, `BondSellMultiplier`, `BondRepPerRoundPerBond`.
- Added three new events to `GameEvents.cs`: `BondPurchasedEvent`, `BondSoldEvent`, `BondRepPaidEvent`.
- Wired `BondManager` as a property on `RunContext`, created in constructor and reset in `ResetForNewRun`.
- Updated `ShopTransaction.PurchaseBond` to fire `BondPurchasedEvent` and `SellBond` to use LIFO sell with auto-calculated sell price and fire `BondSoldEvent`.
- Added bond panel UI to `ShopUI.cs` with `ShowBonds`/`RefreshBondPanel` methods: buy button, info display, sell button with confirmation overlay.
- Wired bond purchase/sell into `ShopState` with `OnBondPurchaseRequested`/`OnBondSellRequested` handlers, updated `BondAvailable` flag and `BondsPurchased` tracking.
- Added bond Rep payout in `MarketOpenState.Enter()` before trading begins.
- Created comprehensive `BondManagerTests.cs` with 23 tests covering: price escalation, purchase/sell mechanics, LIFO sell order, Round 8 blocking, cumulative Rep payout, event firing, and ShopTransaction integration.
- Updated existing `StoreDataModelTests` for new `SellBond` LIFO signature and `StoreLayoutTests` for `BondAvailable` flag.
- Full test suite: 1404 passed, 0 failed, 1 skipped (pre-existing skip).

### Review Follow-ups (AI)

- [ ] [AI-Review][MEDIUM] AC 2 ambiguity: "Displays 1 bond available for purchase each shop visit" — current implementation allows unlimited bond purchases per visit at the same price. Clarify with design whether this is intentional or should be limited to 1 per visit. [ShopState.cs:OnBondPurchaseRequested]

### Change Log

- 2026-02-16: Implemented Story 13.6 — Bonds Panel (Reputation Investment). Full bond purchase/sell/payout system with UI and 23 new tests.
- 2026-02-16: Code review fixes — H1: Fixed ShopClosedEvent.BondsPurchased always 0 in CloseShop. H2: Added "+X Rep from Bonds" display to MarketOpenUI (AC 14). M1/M2: Deduplicated bond purchase/sell logic by delegating ShopTransaction to BondManager. Updated tests for round-based pricing.

### File List

- Assets/Scripts/Runtime/Shop/BondManager.cs (NEW)
- Assets/Scripts/Setup/Data/GameConfig.cs (MODIFIED)
- Assets/Scripts/Runtime/Core/GameEvents.cs (MODIFIED)
- Assets/Scripts/Runtime/Core/RunContext.cs (MODIFIED)
- Assets/Scripts/Runtime/Shop/ShopTransaction.cs (MODIFIED)
- Assets/Scripts/Runtime/UI/ShopUI.cs (MODIFIED)
- Assets/Scripts/Runtime/Core/GameStates/MarketOpenState.cs (MODIFIED)
- Assets/Scripts/Runtime/Core/GameStates/ShopState.cs (MODIFIED)
- Assets/Scripts/Runtime/UI/MarketOpenUI.cs (MODIFIED)
- Assets/Scripts/Setup/UISetup.cs (MODIFIED)
- Assets/Tests/Runtime/Shop/BondManagerTests.cs (NEW)
- Assets/Tests/Runtime/Shop/StoreDataModelTests.cs (MODIFIED)
- Assets/Tests/Runtime/Shop/StoreLayoutTests.cs (MODIFIED)
