# Story 2.2: Sell Execution

Status: ready-for-dev

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

- [ ] Task 1: Add ClosePosition to Portfolio (AC: 1, 2, 3, 4)
  - [ ] Method: `ClosePosition(string stockId, int shares, float currentPrice)` — sells specified shares
  - [ ] If shares == position.Shares, remove position entirely
  - [ ] If shares < position.Shares, reduce position shares (average buy price unchanged)
  - [ ] Return realized P&L: `(currentPrice - position.AverageBuyPrice) * shares`
  - [ ] Add cash: `Cash += shares * currentPrice`
  - [ ] If no position exists for stockId, return without action
  - [ ] File: `Scripts/Runtime/Trading/Portfolio.cs` (extend from Story 2.1)
- [ ] Task 2: Add ExecuteSell to TradeExecutor (AC: 1, 4, 5, 6)
  - [ ] Method: `ExecuteSell(string stockId, int shares, float currentPrice, Portfolio portfolio)`
  - [ ] Validate: position exists and has >= requested shares
  - [ ] On success: call `portfolio.ClosePosition()`, publish `TradeExecutedEvent`
  - [ ] Log realized P&L: `[Trading] SELL: 10 shares of MEME at $3.00 (P&L: +$5.00)`
  - [ ] Wrap in try-catch per architecture pattern
  - [ ] File: `Scripts/Runtime/Trading/TradeExecutor.cs` (extend from Story 2.1)
- [ ] Task 3: Add P&L tracking to Position (AC: 5)
  - [ ] Method: `CalculateRealizedPnL(float sellPrice, int sharesSold)` — returns float
  - [ ] Ensure average buy price is maintained correctly for partial sells
  - [ ] File: `Scripts/Runtime/Trading/Position.cs` (extend from Story 2.1)

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

### Debug Log References

### Completion Notes List

### File List
