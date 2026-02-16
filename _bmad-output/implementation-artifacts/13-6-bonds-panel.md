# Story 13.6: Bonds Panel (Reputation Investment)

Status: ready for dev

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

- [ ] Task 1: Create BondManager (AC: 3, 4, 5, 6, 7, 9, 10)
  - [ ] Plain C# class (not MonoBehaviour)
  - [ ] `BondsOwned: int` — total bonds held
  - [ ] `BondPurchaseHistory: List<BondRecord>` — tracks round purchased and price paid per bond
  - [ ] `BondRecord` struct: `RoundPurchased`, `PricePaid`
  - [ ] `GetCurrentPrice(int currentRound)` — lookup from `BondPricePerRound[]`
  - [ ] `CanPurchase(int currentRound, float currentCash)` — not Round 8, can afford
  - [ ] `Purchase(int currentRound, Portfolio portfolio)` — deduct cash, add bond record
  - [ ] `GetSellPrice()` — average of all bond purchase prices × `BondSellMultiplier` (or sell most recent for half its price)
  - [ ] `Sell(Portfolio portfolio)` — remove 1 bond (LIFO: most recent), return cash
  - [ ] `GetRepPerRound()` — returns `BondsOwned * BondRepPerRoundPerBond`
  - [ ] `PayoutRep(ReputationManager rep)` — add Rep payout at round start
  - [ ] File: `Scripts/Runtime/Shop/BondManager.cs` (NEW)
- [ ] Task 2: Bond panel UI (AC: 1, 2, 8, 11, 13)
  - [ ] Populate bottom-right panel (created in 13.2)
  - [ ] Bond card: shows current round's bond price, "BUY BOND" button
  - [ ] Info display: "Bonds Owned: X", "Earning: +Y Rep/round"
  - [ ] Sell button (visible when bonds > 0): "SELL BOND ($Z)"
  - [ ] Disable buy button if can't afford or Round 8
  - [ ] Round 8: panel shows "NO BONDS AVAILABLE" or bonds owned info only
  - [ ] File: `Scripts/Runtime/UI/ShopUI.cs` — bond panel population
- [ ] Task 3: Purchase flow (AC: 3, 15)
  - [ ] Buy button: validate round + cash → deduct cash via Portfolio → add bond → fire `BondPurchasedEvent`
  - [ ] Update UI immediately: bonds owned count, Rep projection, cash display
  - [ ] File: `Scripts/Runtime/Shop/ShopTransaction.cs` — bond purchase path
- [ ] Task 4: Sell flow (AC: 9, 10, 11, 12, 15)
  - [ ] Sell button: show confirmation prompt ("Sell 1 bond for $X?")
  - [ ] On confirm: remove 1 bond (LIFO), add cash to Portfolio, fire `BondSoldEvent`
  - [ ] Update UI: bonds owned, Rep projection, cash display, sell button visibility
  - [ ] Hide sell button when bonds reach 0
  - [ ] File: `Scripts/Runtime/Shop/ShopTransaction.cs`, `Scripts/Runtime/UI/ShopUI.cs`
- [ ] Task 5: Round-start Rep payout (AC: 6, 7, 14)
  - [ ] In `MarketOpenState.Enter()`: check BondManager, call `PayoutRep()` if bonds > 0
  - [ ] Display "+X Rep from Bonds" text during market open phase (brief overlay or integrated into market open info)
  - [ ] Rep added to ReputationManager before trading begins
  - [ ] File: `Scripts/Runtime/Core/GameStates/MarketOpenState.cs`
- [ ] Task 6: Wire to RunContext (AC: 6, 7)
  - [ ] BondManager state stored in RunContext (bonds owned, purchase history)
  - [ ] Survives round transitions
  - [ ] File: `Scripts/Runtime/Core/RunContext.cs`
- [ ] Task 7: GameConfig constants (AC: 4, 9)
  - [ ] `BondPricePerRound = new int[] { 3, 5, 8, 12, 17, 23, 30, 0 }` (index 7 = Round 8 = 0/unavailable)
  - [ ] `BondSellMultiplier = 0.5f`
  - [ ] `BondRepPerRoundPerBond = 1`
  - [ ] File: `Scripts/Setup/Data/GameConfig.cs`
- [ ] Task 8: GameEvents (AC: 15)
  - [ ] `BondPurchasedEvent`: Round, PricePaid, TotalBondsOwned, RemainingCash
  - [ ] `BondSoldEvent`: SellPrice, TotalBondsOwned, CashAfterSale
  - [ ] `BondRepPaidEvent`: BondsOwned, RepEarned, TotalReputation
  - [ ] File: `Scripts/Runtime/Core/GameEvents.cs`
- [ ] Task 9: Write tests (All AC)
  - [ ] BondManager: purchase deducts cash, price escalation correct, sell returns half, Rep payout math
  - [ ] Round 8 blocking: cannot purchase on final round
  - [ ] Cumulative Rep: buy R1 + R2, verify R3 payout = +2 Rep
  - [ ] Sell LIFO: selling removes most recent bond
  - [ ] Files: `Tests/Runtime/Shop/BondManagerTests.cs`

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

### Completion Notes List

### Change Log

### File List
