# Story 2.5: Capital Management

Status: ready-for-dev

## Story

As a player,
I want a limited starting cash pool that compounds across rounds,
so that early decisions have lasting consequences and capital management is a core skill.

## Acceptance Criteria

1. Run starts with $1,000 cash (from GameConfig.StartingCapital)
2. Cash carries forward between rounds — unspent cash from round N is available in round N+1
3. Shop purchases (Epic 7) deduct from the same cash pool used for trading
4. Capital state is preserved in RunContext across round transitions
5. Portfolio resets positions at round end (auto-liquidation converts everything to cash) but cash persists
6. Player cannot go into negative cash — floor at $0.00

## Tasks / Subtasks

- [ ] Task 1: Implement run initialization in RunContext (AC: 1, 4)
  - [ ] Method: `StartNewRun()` — creates fresh Portfolio with GameConfig.StartingCapital, resets act/round to 1/1
  - [ ] Method: `GetCurrentCash()` — convenience accessor for Portfolio.Cash
  - [ ] Ensure RunContext persists across round transitions (it is not recreated per round)
  - [ ] File: `Scripts/Runtime/Core/RunContext.cs` (extend from Story 2.1)
- [ ] Task 2: Implement round-end liquidation in Portfolio (AC: 2, 5)
  - [ ] Method: `LiquidateAllPositions(Func<string, float> getCurrentPrice)` — closes all positions at current prices
  - [ ] For longs: sell all at current price, add proceeds to cash
  - [ ] For shorts: cover all at current price, return margin +/- P&L
  - [ ] Clear all positions after liquidation
  - [ ] Return total realized P&L from liquidation
  - [ ] File: `Scripts/Runtime/Trading/Portfolio.cs` (extend)
- [ ] Task 3: Add cash floor enforcement (AC: 6)
  - [ ] After any operation that reduces cash, clamp: `Cash = Mathf.Max(Cash, 0f)`
  - [ ] This primarily protects against short losses exceeding margin
  - [ ] Add validation in Portfolio.DeductCash or wherever cash is modified
  - [ ] File: `Scripts/Runtime/Trading/Portfolio.cs` (extend)
- [ ] Task 4: Add round transition support (AC: 2, 4)
  - [ ] Method: `PrepareForNextRound()` on RunContext — increments round, resets round-level state
  - [ ] Portfolio.Cash carries forward unchanged
  - [ ] Portfolio positions list is already cleared by liquidation (Task 2)
  - [ ] Reset `_roundStartValue` for next round's profit tracking
  - [ ] File: `Scripts/Runtime/Core/RunContext.cs` (extend)
- [ ] Task 5: Define capital-related events (AC: 2, 5)
  - [ ] `RoundEndedEvent`: RoundNumber, TotalProfit, FinalCash
  - [ ] `RunStartedEvent`: StartingCapital
  - [ ] Add to `Scripts/Runtime/Core/GameEvents.cs`

## Dev Notes

### Architecture Compliance

- **RunContext is the single owner of run state** — cash, portfolio, act, round. Nothing else stores this.
- **Game state flow:** MetaHub → MarketOpen → Trading → MarketClose → Shop → loop. Cash persists across this entire flow.
- **Capital tension:** The GDD emphasizes that shop spending vs. trading capital is a core tension. This story ensures the infrastructure supports that by using a single cash pool.
- **Auto-liquidation timing:** Positions are liquidated in MarketClose state (Epic 4, Story 4.3). This story provides the `LiquidateAllPositions` method that Epic 4 will call.

### Capital Flow Across a Run

```
Round 1 Start: $1,000 (GameConfig.StartingCapital)
  → Trading phase: buy/sell/short → positions open
  → Market close: auto-liquidate → all positions become cash
  → Cash after Round 1: $1,150 (example: earned $150 profit)
  → Shop: buy Speed Trader for $200
  → Cash entering Round 2: $950

Round 2 Start: $950
  → Trading phase: larger positions possible if Round 1 went well
  → ... and so on, compounding across rounds
```

### Shop Integration Note

The shop (Epic 7) will call `Portfolio.DeductCash(itemCost)` when purchasing items. This story doesn't implement the shop — it ensures the cash pool supports deductions from any source. Add a general `DeductCash(float amount)` method that validates and clamps.

### The Compounding Problem

This is the core economy design challenge from GDD Section 11.1: "Starting capital should feel tight in Act 1, comfortable by Act 3 if compounded well." Players who trade well in early rounds will have more capital in later rounds, enabling larger positions. Players who trade poorly will be capital-starved, making later targets harder to hit. This snowball effect is intentional — it's the Balatro-equivalent of chip management.

### Project Structure Notes

- Modifies: `Scripts/Runtime/Core/RunContext.cs`
- Modifies: `Scripts/Runtime/Trading/Portfolio.cs`
- Modifies: `Scripts/Runtime/Core/GameEvents.cs`
- No new files needed

### References

- [Source: game-architecture.md#Game State Machine] — RunContext carries: current act, current round, cash, portfolio, active items
- [Source: game-architecture.md#Game State Machine] — States: MetaHub → MarketOpen → Trading → MarketClose → Shop → loop
- [Source: bull-run-gdd-mvp.md#2.2] — "Unspent cash carries forward as trading capital for the next round"
- [Source: bull-run-gdd-mvp.md#2.2] — "This creates a tension: spend on upgrades to be more powerful, or hoard capital for larger trades"
- [Source: bull-run-gdd-mvp.md#3.1] — "The player starts each run with a small cash pool (e.g., $1,000)"
- [Source: bull-run-gdd-mvp.md#11.1] — Starting Capital: $1,000, "Should feel tight in Act 1, comfortable by Act 3 if compounded well"

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
