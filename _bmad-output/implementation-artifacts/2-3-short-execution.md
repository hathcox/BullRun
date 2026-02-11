# Story 2.3: Short Execution

Status: review

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

- [x] Task 1: Add margin constants to GameConfig (AC: 2)
  - [x] Add `ShortMarginRequirement` = 0.5f (50% of position value)
  - [x] File: `Scripts/Setup/Data/GameConfig.cs` (extend)
- [x] Task 2: Extend Position for short positions (AC: 1, 8)
  - [x] Add `IsShort` flag (bool) to Position — true for shorts, false for longs
  - [x] Add `MarginHeld` field — cash locked as collateral
  - [x] Modify `UnrealizedPnL` to invert for shorts: `(entryPrice - currentPrice) * shares`
  - [x] File: `Scripts/Runtime/Trading/Position.cs` (extend)
- [x] Task 3: Add short execution to Portfolio (AC: 1, 2, 7)
  - [x] Method: `OpenShort(string stockId, int shares, float price)` — creates short position
  - [x] Calculate margin: `shares * price * GameConfig.ShortMarginRequirement`
  - [x] Validate: `CanAfford(margin)` — if false, reject
  - [x] Deduct margin from cash (not the full position value)
  - [x] File: `Scripts/Runtime/Trading/Portfolio.cs` (extend)
- [x] Task 4: Add cover (close short) to Portfolio (AC: 9)
  - [x] Method: `CoverShort(string stockId, int shares, float currentPrice)` — closes short position
  - [x] Return margin collateral to cash
  - [x] Calculate P&L: `(entryPrice - currentPrice) * shares`
  - [x] Add to cash: `margin + pnl` (can be negative if price rose)
  - [x] If total return is negative and exceeds available cash, player's cash goes to zero (margin eaten)
  - [x] File: `Scripts/Runtime/Trading/Portfolio.cs` (extend)
- [x] Task 5: Add ExecuteShort and ExecuteCover to TradeExecutor (AC: 1, 5, 7)
  - [x] Method: `ExecuteShort(string stockId, int shares, float currentPrice, Portfolio portfolio)`
  - [x] Method: `ExecuteCover(string stockId, int shares, float currentPrice, Portfolio portfolio)`
  - [x] Both publish `TradeExecutedEvent` with `isShort: true`
  - [x] Both wrapped in try-catch
  - [x] File: `Scripts/Runtime/Trading/TradeExecutor.cs` (extend)

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

Claude Opus 4.6

### Debug Log References

- `[Trading] Short rejected: insufficient cash for margin ${margin} on {shares}x {stockId}` — Portfolio.cs
- `[Trading] SHORT opened: {shares}x {stockId} at ${price} (margin held: ${margin})` — Portfolio.cs
- `[Trading] Cover rejected: no short position for {stockId}` — Portfolio.cs
- `[Trading] Cover rejected: requested {shares} but only short {position.Shares} of {stockId}` — Portfolio.cs
- `[Trading] SHORT covered: {stockId} (P&L: +${pnl}, margin returned: ${marginPortion})` — Portfolio.cs
- `[Trading] SHORT partially covered: {stockId} now {remainingShares} shares` — Portfolio.cs
- `[Trading] Short rejected: insufficient cash for margin on {shares}x {stockId}` — TradeExecutor.cs
- `[Trading] SHORT executed: {shares} shares of {stockId} at ${currentPrice} (margin held: ${margin})` — TradeExecutor.cs
- `[Trading] Cover rejected: no short position or insufficient shares` — TradeExecutor.cs
- `[Trading] COVER executed: {shares} shares of {stockId} at ${currentPrice} (P&L: +${pnl})` — TradeExecutor.cs

### Completion Notes List

- Task 1: Added `ShortMarginRequirement = 0.5f` to GameConfig. 1 new test.
- Task 2: Extended Position with short constructor (stockId, shares, entryPrice, marginHeld), IsShort flag, MarginHeld field. Modified UnrealizedPnL and CalculateRealizedPnL to invert for shorts. 8 new tests.
- Task 3: Added `OpenShort` to Portfolio. Calculates margin, validates cash, deducts margin, creates short Position. 4 new tests.
- Task 4: Added `CoverShort` to Portfolio. Returns proportional margin + P&L to cash. Handles full/partial cover. Floors cash return at 0 when loss exceeds margin. 9 new tests.
- Task 5: Added `ExecuteShort` and `ExecuteCover` to TradeExecutor. Both publish TradeExecutedEvent with IsShort=true. Try-catch wrapped. 11 new tests.

### Change Log

- 2026-02-10: Implemented all 5 tasks for Story 2.3 Short Execution. Added margin mechanics, short positions, cover functionality. 33 new unit tests added.

### File List

- `Assets/Scripts/Setup/Data/GameConfig.cs` (modified — added ShortMarginRequirement)
- `Assets/Scripts/Runtime/Trading/Position.cs` (modified — added short constructor, IsShort, MarginHeld, inverted P&L)
- `Assets/Scripts/Runtime/Trading/Portfolio.cs` (modified — added OpenShort, CoverShort)
- `Assets/Scripts/Runtime/Trading/TradeExecutor.cs` (modified — added ExecuteShort, ExecuteCover)
- `Assets/Tests/Runtime/PriceEngine/GameConfigTests.cs` (modified — added ShortMarginRequirement test)
- `Assets/Tests/Runtime/Trading/PositionTests.cs` (modified — added 8 short position tests)
- `Assets/Tests/Runtime/Trading/PortfolioTests.cs` (modified — added 13 OpenShort/CoverShort tests)
- `Assets/Tests/Runtime/Trading/TradeExecutorTests.cs` (modified — added 11 ExecuteShort/ExecuteCover tests)
