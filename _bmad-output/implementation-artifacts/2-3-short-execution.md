# Story 2.3: Short Execution

Status: ready-for-dev

## Story

As a player,
I want to SHORT a stock by borrowing and selling at the current price,
so that I can profit when prices drop.

## Acceptance Criteria

1. Shorting borrows shares and sells them at the current market price
2. Requires margin collateral — 50% of position value held in reserve from available cash
3. Player profits if the price drops below the short entry price
4. Player loses if the price rises above the short entry price
5. Shorts are auto-closed at market close (handled by Round Management, Epic 4)
6. Shorting is available from Round 1 as a core mechanic, not an unlock
7. Cannot short if insufficient cash for margin requirement — silently rejected
8. Short positions are visually distinguishable from long positions (data flag)
9. Covering a short (buying back) returns margin collateral plus/minus P&L

## Tasks / Subtasks

- [ ] Task 1: Add margin constants to GameConfig (AC: 2)
  - [ ] Add `ShortMarginRequirement` = 0.5f (50% of position value)
  - [ ] File: `Scripts/Setup/Data/GameConfig.cs` (extend)
- [ ] Task 2: Extend Position for short positions (AC: 1, 8)
  - [ ] Add `IsShort` flag (bool) to Position — true for shorts, false for longs
  - [ ] Add `MarginHeld` field — cash locked as collateral
  - [ ] Modify `UnrealizedPnL` to invert for shorts: `(entryPrice - currentPrice) * shares`
  - [ ] File: `Scripts/Runtime/Trading/Position.cs` (extend)
- [ ] Task 3: Add short execution to Portfolio (AC: 1, 2, 7)
  - [ ] Method: `OpenShort(string stockId, int shares, float price)` — creates short position
  - [ ] Calculate margin: `shares * price * GameConfig.ShortMarginRequirement`
  - [ ] Validate: `CanAfford(margin)` — if false, reject
  - [ ] Deduct margin from cash (not the full position value)
  - [ ] File: `Scripts/Runtime/Trading/Portfolio.cs` (extend)
- [ ] Task 4: Add cover (close short) to Portfolio (AC: 9)
  - [ ] Method: `CoverShort(string stockId, int shares, float currentPrice)` — closes short position
  - [ ] Return margin collateral to cash
  - [ ] Calculate P&L: `(entryPrice - currentPrice) * shares`
  - [ ] Add to cash: `margin + pnl` (can be negative if price rose)
  - [ ] If total return is negative and exceeds available cash, player's cash goes to zero (margin eaten)
  - [ ] File: `Scripts/Runtime/Trading/Portfolio.cs` (extend)
- [ ] Task 5: Add ExecuteShort and ExecuteCover to TradeExecutor (AC: 1, 5, 7)
  - [ ] Method: `ExecuteShort(string stockId, int shares, float currentPrice, Portfolio portfolio)`
  - [ ] Method: `ExecuteCover(string stockId, int shares, float currentPrice, Portfolio portfolio)`
  - [ ] Both publish `TradeExecutedEvent` with `isShort: true`
  - [ ] Both wrapped in try-catch
  - [ ] File: `Scripts/Runtime/Trading/TradeExecutor.cs` (extend)

## Dev Notes

### Architecture Compliance

- **Extends existing files** — adds short mechanics to Position, Portfolio, TradeExecutor
- **Same patterns** — try-catch, EventBus publishing, silent rejection on insufficient funds
- **Logging:** `[Trading] SHORT: 10 shares of MEME at $3.00 (margin held: $15.00)`
- **Logging:** `[Trading] COVER: 10 shares of MEME at $2.00 (P&L: +$10.00, margin returned: $15.00)`

### Margin Mechanics Explained

Shorting is more complex than buying:
1. Player shorts 10 shares at $5.00 → position value = $50
2. Margin required = $50 * 0.5 = $25 deducted from cash
3. If price drops to $3.00 → P&L = ($5.00 - $3.00) * 10 = +$20
4. Cover: player gets back $25 (margin) + $20 (profit) = $45 added to cash
5. If price rises to $7.00 → P&L = ($5.00 - $7.00) * 10 = -$20
6. Cover: player gets back $25 (margin) - $20 (loss) = $5 added to cash

### Short Risk: Unlimited Loss Potential

In real trading, shorts have unlimited loss potential. For gameplay purposes, the margin collateral caps the player's risk at the margin amount. If the loss exceeds the margin, the player just loses their margin (cash doesn't go negative). This is a simplification for fun — noted in GDD balance section.

### Input Mapping

- Short: Right Click / Shift+Space / RB
- Cover Short: Right Click held stock / Shift+S / LB
- Input wiring is a UI concern (Epic 3). TradeExecutor exposes public methods.

### Project Structure Notes

- Modifies: `Scripts/Setup/Data/GameConfig.cs`
- Modifies: `Scripts/Runtime/Trading/Position.cs`
- Modifies: `Scripts/Runtime/Trading/Portfolio.cs`
- Modifies: `Scripts/Runtime/Trading/TradeExecutor.cs`
- No new files needed

### References

- [Source: game-architecture.md#Error Handling] — Try-catch recovery pattern
- [Source: game-architecture.md#Event System] — TradeExecutedEvent
- [Source: bull-run-gdd-mvp.md#3.1] — "SHORT: Bet against a stock... requires margin collateral (50% of position value held in reserve)"
- [Source: bull-run-gdd-mvp.md#3.1] — "Shorting is available from Round 1 as a core mechanic, not an unlock"
- [Source: bull-run-gdd-mvp.md#11.1] — "Short Margin Requirement: 50% of position value"
- [Source: bull-run-gdd-mvp.md#6.3] — Input mapping: Right Click / Shift+Space / RB

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
