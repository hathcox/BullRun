# Story 2.2: Sell Execution

Status: review

## Story

As a player,
I want to click SELL to liquidate held shares at the current market price,
so that I can realize profits or cut losses.

## Acceptance Criteria

1. Clicking SELL sells held shares of the currently selected stock at the current market price
2. Cash equal to (share price x shares sold) is returned to the player's capital pool
3. Can sell partial or full position
4. Cannot sell if no position is held — order is silently rejected
5. Profit/loss is calculated from the difference between sell price and average buy price
6. Trade execution publishes a `TradeExecutedEvent` via EventBus
7. Sell action is mapped to Left Click on SELL / S key / LT (controller)

## Tasks / Subtasks

- [x] Task 1: Add ClosePosition to Portfolio (AC: 1, 2, 3, 4)
  - [x] Method: `ClosePosition(string stockId, int shares, float currentPrice)` — sells specified shares
  - [x] If shares == position.Shares, remove position entirely
  - [x] If shares < position.Shares, reduce position shares (average buy price unchanged)
  - [x] Return realized P&L: `(currentPrice - position.AverageBuyPrice) * shares`
  - [x] Add cash: `Cash += shares * currentPrice`
  - [x] If no position exists for stockId, return without action
  - [x] File: `Scripts/Runtime/Trading/Portfolio.cs` (extend from Story 2.1)
- [x] Task 2: Add ExecuteSell to TradeExecutor (AC: 1, 4, 5, 6)
  - [x] Method: `ExecuteSell(string stockId, int shares, float currentPrice, Portfolio portfolio)`
  - [x] Validate: position exists and has >= requested shares
  - [x] On success: call `portfolio.ClosePosition()`, publish `TradeExecutedEvent`
  - [x] Log realized P&L: `[Trading] SELL: 10 shares of MEME at $3.00 (P&L: +$5.00)`
  - [x] Wrap in try-catch per architecture pattern
  - [x] File: `Scripts/Runtime/Trading/TradeExecutor.cs` (extend from Story 2.1)
- [x] Task 3: Add P&L tracking to Position (AC: 5)
  - [x] Method: `CalculateRealizedPnL(float sellPrice, int sharesSold)` — returns float
  - [x] Ensure average buy price is maintained correctly for partial sells
  - [x] File: `Scripts/Runtime/Trading/Position.cs` (extend from Story 2.1)

## Dev Notes

### Architecture Compliance

- **Extends Story 2.1 files** — no new files, adds sell logic to existing TradeExecutor and Portfolio
- **Same error handling pattern** — try-catch at TradeExecutor boundary, silent rejection on invalid sells
- **Same EventBus pattern** — publish `TradeExecutedEvent` with `isBuy: false`
- **Logging:** `[Trading]` tag. Include realized P&L in sell log messages.

### Partial Sell Behavior

When selling part of a position:
- Average buy price stays the same (it's a weighted average of all buys, unchanged by sells)
- Only the share count decreases
- Cash received = shares sold x current price (not average price)
- P&L = (current price - average buy price) x shares sold

### Input Mapping Note

Same as Story 2.1 — input binding is a UI concern. TradeExecutor.ExecuteSell is a public method called by the Trading HUD when it's built in Epic 3.

### Project Structure Notes

- Modifies: `Scripts/Runtime/Trading/TradeExecutor.cs`
- Modifies: `Scripts/Runtime/Trading/Portfolio.cs`
- Modifies: `Scripts/Runtime/Trading/Position.cs`
- No new files needed

### References

- [Source: game-architecture.md#Error Handling] — Try-catch pattern, recover by skipping operation
- [Source: game-architecture.md#Event System] — TradeExecutedEvent publishing
- [Source: bull-run-gdd-mvp.md#3.1] — "SELL: Sell currently held shares at current market price. Returns cash to player's capital pool."
- [Source: bull-run-gdd-mvp.md#6.3] — Input mapping: Left Click SELL / S key / LT

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Debug Log References

- `[Trading] Sell rejected: no position for {stockId}` — Portfolio.cs
- `[Trading] Sell rejected: requested {shares} but only hold {position.Shares} of {stockId}` — Portfolio.cs
- `[Trading] Position closed: {stockId} (P&L: +${pnl})` — Portfolio.cs
- `[Trading] Position reduced: {stockId} now {remainingShares} shares (P&L: +${pnl})` — Portfolio.cs
- `[Trading] Sell rejected: no position or insufficient shares for {shares}x {stockId}` — TradeExecutor.cs
- `[Trading] SELL executed: {shares} shares of {stockId} at ${currentPrice} (P&L: +${pnl})` — TradeExecutor.cs

### Completion Notes List

- Task 1: Added `ClosePosition` to Portfolio. Handles full sell (removes position from dictionary), partial sell (reduces shares, preserves average buy price), invalid sell (no position or insufficient shares returns 0). Adds proceeds to cash. 9 new unit tests.
- Task 2: Added `ExecuteSell` to TradeExecutor. Try-catch at boundary, validates position exists with sufficient shares, publishes TradeExecutedEvent with IsBuy=false, logs P&L. 8 new unit tests.
- Task 3: Added `CalculateRealizedPnL` to Position. Simple calculation: (sellPrice - averageBuyPrice) * sharesSold. Used by Portfolio.ClosePosition. 4 new unit tests.

### Change Log

- 2026-02-10: Implemented all 3 tasks for Story 2.2 Sell Execution. Extended Position, Portfolio, and TradeExecutor with sell/close functionality. 21 new unit tests added.

### File List

- `Assets/Scripts/Runtime/Trading/Position.cs` (modified — added CalculateRealizedPnL)
- `Assets/Scripts/Runtime/Trading/Portfolio.cs` (modified — added ClosePosition)
- `Assets/Scripts/Runtime/Trading/TradeExecutor.cs` (modified — added ExecuteSell)
- `Assets/Tests/Runtime/Trading/PositionTests.cs` (modified — added 4 realized P&L tests)
- `Assets/Tests/Runtime/Trading/PortfolioTests.cs` (modified — added 9 ClosePosition tests)
- `Assets/Tests/Runtime/Trading/TradeExecutorTests.cs` (modified — added 8 ExecuteSell tests)
