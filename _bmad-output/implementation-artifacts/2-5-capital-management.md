# Story 2.5: Capital Management

Status: done

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

- [x] Task 1: Implement run initialization in RunContext (AC: 1, 4)
  - [x] Method: `StartNewRun()` — creates fresh Portfolio with GameConfig.StartingCapital, resets act/round to 1/1
  - [x] Method: `GetCurrentCash()` — convenience accessor for Portfolio.Cash
  - [x] Ensure RunContext persists across round transitions (it is not recreated per round)
  - [x] File: `Scripts/Runtime/Core/RunContext.cs` (extend from Story 2.1)
- [x] Task 2: Implement round-end liquidation in Portfolio (AC: 2, 5)
  - [x] Method: `LiquidateAllPositions(Func<string, float> getCurrentPrice)` — closes all positions at current prices
  - [x] For longs: sell all at current price, add proceeds to cash
  - [x] For shorts: cover all at current price, return margin +/- P&L
  - [x] Clear all positions after liquidation
  - [x] Return total realized P&L from liquidation
  - [x] File: `Scripts/Runtime/Trading/Portfolio.cs` (extend)
- [x] Task 3: Add cash floor enforcement (AC: 6)
  - [x] After any operation that reduces cash, clamp: `Cash = Mathf.Max(Cash, 0f)`
  - [x] This primarily protects against short losses exceeding margin
  - [x] Add validation in Portfolio.DeductCash or wherever cash is modified
  - [x] File: `Scripts/Runtime/Trading/Portfolio.cs` (extend)
- [x] Task 4: Add round transition support (AC: 2, 4)
  - [x] Method: `PrepareForNextRound()` on RunContext — increments round, resets round-level state
  - [x] Portfolio.Cash carries forward unchanged
  - [x] Portfolio positions list is already cleared by liquidation (Task 2)
  - [x] Reset `_roundStartValue` for next round's profit tracking
  - [x] File: `Scripts/Runtime/Core/RunContext.cs` (extend)
- [x] Task 5: Define capital-related events (AC: 2, 5)
  - [x] `RoundEndedEvent`: RoundNumber, TotalProfit, FinalCash
  - [x] `RunStartedEvent`: StartingCapital
  - [x] Add to `Scripts/Runtime/Core/GameEvents.cs`

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
Claude Opus 4.6

### Debug Log References
No issues encountered during implementation.

### Completion Notes List
- Task 1: Added `RunContext.StartNewRun()` static factory creating fresh portfolio with GameConfig.StartingCapital, act/round at 1/1. Added `GetCurrentCash()` convenience accessor.
- Task 2: Added `LiquidateAllPositions(Func<string, float>)` — iterates all positions, sells longs at current price, covers shorts returning margin +/- P&L (floored at 0), clears positions, returns total realized P&L.
- Task 3: Added `DeductCash(float)` with CanAfford validation for shop purchases. Added `ClampCash()` private helper called after OpenPosition and DeductCash to enforce $0 floor.
- Task 4: Added `RunContext.PrepareForNextRound()` — increments round, resets round start value via `Portfolio.StartRound()`. Cash carries forward unchanged. Tested multi-round compounding.
- Task 5: Added `RoundEndedEvent` (RoundNumber, TotalProfit, FinalCash) and `RunStartedEvent` (StartingCapital) structs to GameEvents.cs.

### Change Log
- 2026-02-10: Implemented story 2-5 — run initialization, round-end liquidation, cash floor enforcement, round transitions, and capital-related events
- 2026-02-10: Code review fixes — OpenPosition now rejects insufficient cash (returns null), StartNewRun publishes RunStartedEvent, PrepareForNextRound uses Cash directly with assert, documented RoundEndedEvent as caller-published

### File List
- Assets/Scripts/Runtime/Core/RunContext.cs (new)
- Assets/Scripts/Runtime/Trading/Portfolio.cs (modified)
- Assets/Scripts/Runtime/Core/GameEvents.cs (modified)
- Assets/Tests/Runtime/Core/RunContextTests.cs (new)
- Assets/Tests/Runtime/Trading/PortfolioTests.cs (modified)
- Assets/Tests/Runtime/Core/GameEventsTests.cs (modified)
