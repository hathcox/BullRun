# Story FIX-11: Short Selling Mechanic Redesign — "Bet Against" System

Status: ready-for-dev

## Story

As a player,
I want shorting to be a separate, clear mechanic where I place a "bet against" the stock rather than a confusing financial instrument hidden behind the Sell button,
so that bearish plays feel exciting and distinct from selling.

## Problem Analysis

Currently shorting is buried in the "Smart Sell" system — if you have no long position and press Sell, it silently opens a short. This is confusing: players don't realize they've shorted, don't understand margin, and the P&L display is counterintuitive. Inspired by *Space Warlords Baby Trading Simulator*, shorts should be a distinct, visible mechanic — a "bet" that the price will drop, with clear risk/reward shown upfront.

**Current Short Flow (confusing):**
1. Player presses S or clicks SELL with no long position
2. `GameRunner.ExecuteSmartSell()` silently opens a short via `TradeExecutor.ExecuteShort()`
3. `Portfolio.OpenShort()` deducts margin collateral (50% of position value)
4. Short appears in PositionOverlay as "SHORT" — but player may not realize what happened
5. To close: player must press B (Smart Buy auto-detects and covers)
6. Terminology uses "margin," "collateral," "cover" — financial jargon

**Desired Flow (clear "Bet Against"):**
1. Player presses D key or clicks dedicated "BET AGAINST" button (separate from Buy/Sell)
2. Player commits a fixed cash amount as their bet (not share quantity)
3. "Bet Against" panel shows potential win/loss in real-time
4. Short bet auto-closes at market close with clear profit/loss
5. Player can manually close early via F key or "CASH OUT" button
6. Terminology uses "bet," "cash out," "win/lose" — no jargon

**Affected Code:**
- `Scripts/Runtime/Core/GameRunner.cs` — remove Smart Sell short logic, add bet routing, new keyboard shortcuts D/F
- `Scripts/Runtime/Trading/Portfolio.cs` — refactor `OpenShort()`/`CoverShort()` to bet-based model
- `Scripts/Runtime/Trading/Position.cs` — adapt for fixed-stake bet positions
- `Scripts/Runtime/Trading/TradeExecutor.cs` — new `ExecuteBet()`/`CashOutBet()` methods
- `Scripts/Setup/UISetup.cs` — new short bet UI panel
- `Scripts/Runtime/UI/PositionOverlay.cs` — display active bets with real-time P&L
- `Scripts/Runtime/UI/TradeFeedback.cs` — bet-specific feedback messages
- `Scripts/Runtime/Events/EventScheduler.cs` — ShortSqueeze targets bets
- `Scripts/Setup/Data/GameConfig.cs` — bet-related config constants

## Acceptance Criteria

1. Shorting is a SEPARATE action from Sell — Sell ONLY sells long positions, never opens a short
2. Dedicated UI for placing short bets, visually distinct from the Buy/Sell trade panel
3. Player commits a fixed cash amount (not share quantity) when shorting
4. Real-time P&L displayed directly on the short bet UI element
5. Short bets auto-close at market close (like longs) with clear profit/loss shown
6. Player can manually close a short bet early via a "Cash Out" button on the bet
7. No financial jargon: avoid "margin," "collateral," "cover" — use "bet," "cash out," "win/lose"
8. Keyboard shortcut for placing a short bet (D key) and cashing out (F key)
9. ShortSqueeze event still targets active short bets for dramatic effect
10. Remove Smart Sell logic that auto-opens shorts from the Sell button

## Tasks / Subtasks

- [ ] Task 1: Add bet-related config constants to GameConfig (AC: 3)
  - [ ] Add `public static readonly float[] BetAmountPresets = { 50f, 100f, 250f, 500f }` — fixed bet amounts
  - [ ] Add `public static readonly int DefaultBetPresetIndex = 1` — default to $100
  - [ ] Add `public static readonly float BetMaxProfitMultiplier = 2f` — max profit is 2x bet (price drops to 0)
  - [ ] File: `Assets/Scripts/Setup/Data/GameConfig.cs`

- [ ] Task 2: Refactor Position to support bet-based shorts (AC: 3, 4)
  - [ ] Add `public bool IsBet { get; private set; }` flag
  - [ ] Add `public float BetAmount { get; private set; }` for the fixed cash stake
  - [ ] Add `public float EntryPrice { get; private set; }` (same as AverageBuyPrice for bets, but clearer naming for bet context)
  - [ ] Add new constructor: `Position(string stockId, float betAmount, float entryPrice, bool isBet)` for bet positions
  - [ ] Bet P&L calculation: `profit = betAmount * ((entryPrice - currentPrice) / entryPrice)` — percentage-based on entry price
  - [ ] Max loss = betAmount (price doubles → lose entire bet). Max profit = betAmount * BetMaxProfitMultiplier (price → 0)
  - [ ] Keep existing long/short constructors working (backward compatibility during transition)
  - [ ] File: `Assets/Scripts/Runtime/Trading/Position.cs`

- [ ] Task 3: Add `PlaceBet()`/`CashOutBet()` to Portfolio (AC: 3, 5, 6)
  - [ ] Add `PlaceBet(string stockId, float betAmount, float currentPrice)` — deducts betAmount from cash, creates bet Position
  - [ ] Add `CashOutBet(string stockId, float currentPrice)` — closes bet, returns betAmount +/- P&L to cash
  - [ ] Reject bet if long position exists on same stock
  - [ ] Reject bet if bet already exists on same stock (one bet per stock)
  - [ ] Update `LiquidateAllPositions()` to handle bet positions correctly (close at current price, return cash)
  - [ ] File: `Assets/Scripts/Runtime/Trading/Portfolio.cs`

- [ ] Task 4: Add `ExecuteBet()`/`ExecuteCashOut()` to TradeExecutor (AC: 3, 6)
  - [ ] Add `ExecuteBet(string stockId, float betAmount, float currentPrice, Portfolio portfolio)` — validates and calls `Portfolio.PlaceBet()`
  - [ ] Add `ExecuteCashOut(string stockId, float currentPrice, Portfolio portfolio)` — validates and calls `Portfolio.CashOutBet()`
  - [ ] Both publish `TradeExecutedEvent` with `IsShort = true` flag for event tracking
  - [ ] Respect `IsTradeEnabled` flag (no bets during market close)
  - [ ] File: `Assets/Scripts/Runtime/Trading/TradeExecutor.cs`

- [ ] Task 5: Remove Smart Sell short logic from GameRunner (AC: 1, 10)
  - [ ] In `ExecuteSmartSell()`: remove the `else` branch that opens a short when no long position exists
  - [ ] `ExecuteSmartSell()` should now only sell longs — if no long position, show "No position to sell" feedback
  - [ ] Remove cover logic from `ExecuteSmartBuy()` — Buy only buys longs now
  - [ ] File: `Assets/Scripts/Runtime/Core/GameRunner.cs`

- [ ] Task 6: Add bet placement and cash-out methods to GameRunner (AC: 3, 6, 8)
  - [ ] Add `ExecutePlaceBet()` method — gets active stock, reads current bet preset amount, calls `TradeExecutor.ExecuteBet()`
  - [ ] Add `ExecuteCashOutBet()` method — gets active stock, calls `TradeExecutor.ExecuteCashOut()`
  - [ ] Add keyboard handling: D key → `ExecutePlaceBet()`, F key → `ExecuteCashOutBet()`
  - [ ] Subscribe to new `BetButtonPressedEvent` from UI buttons
  - [ ] File: `Assets/Scripts/Runtime/Core/GameRunner.cs`

- [ ] Task 7: Create "Bet Against" UI panel (AC: 2, 4, 7)
  - [ ] Add `ExecuteBetPanel()` to UISetup — visually distinct panel (hot pink/purple theme)
  - [ ] Panel positioned separately from Buy/Sell trade panel (e.g., right side or below)
  - [ ] Contains: bet amount presets row ($50/$100/$250/$500), "BET AGAINST" button, "CASH OUT" button
  - [ ] Show active bet status when bet is placed: "Your $100 bet: +$25" or "-$40" with real-time updates
  - [ ] "BET AGAINST" button dimmed when bet already active or no cash
  - [ ] "CASH OUT" button dimmed when no active bet
  - [ ] No jargon: labels use "BET AGAINST", "CASH OUT", "+$X" / "-$X"
  - [ ] Wire buttons to publish `BetButtonPressedEvent`
  - [ ] File: `Assets/Scripts/Setup/UISetup.cs`

- [ ] Task 8: Add `BetButtonPressedEvent` and bet-specific events (AC: 2, 8)
  - [ ] Add `BetButtonPressedEvent { bool IsPlaceBet }` — true for place, false for cash out
  - [ ] File: `Assets/Scripts/Runtime/Core/GameEvents.cs`

- [ ] Task 9: Update PositionOverlay for bet display (AC: 4, 7)
  - [ ] When active position is a bet: show "BET AGAINST" instead of "SHORT"
  - [ ] Show bet amount and real-time P&L: "Bet: $100 | P&L: +$25"
  - [ ] Use hot pink color for bet display (consistent with bet panel theme)
  - [ ] File: `Assets/Scripts/Runtime/UI/PositionOverlay.cs`

- [ ] Task 10: Update TradeFeedback for bet messages (AC: 7)
  - [ ] Add bet-specific messages: "BET $100 AGAINST {ticker}", "CASHED OUT +$25", "CASHED OUT -$40"
  - [ ] Use hot pink color for bet feedback (matches existing `ShortPink`)
  - [ ] File: `Assets/Scripts/Runtime/UI/TradeFeedback.cs`

- [ ] Task 11: Update ShortSqueeze event to target bets (AC: 9)
  - [ ] ShortSqueeze event still triggers dramatic price spike on active stock
  - [ ] Verify bet P&L calculation handles ShortSqueeze price spike correctly (bet loses value as price rises)
  - [ ] No code changes needed if ShortSqueeze just affects price — bet P&L updates automatically via price-based calculation
  - [ ] File: `Assets/Scripts/Runtime/Events/EventScheduler.cs` (verification/minor update)

- [ ] Task 12: Show/hide bet panel based on trading state (AC: 2)
  - [ ] In GameRunner.Update(): show/hide bet panel alongside trade panel based on `TradingState.IsActive`
  - [ ] Reset bet panel visual state on `TradingPhaseEndedEvent`
  - [ ] File: `Assets/Scripts/Runtime/Core/GameRunner.cs`

- [ ] Task 13: Write comprehensive tests for bet system (AC: 1-10)
  - [ ] Test: PlaceBet deducts correct cash amount
  - [ ] Test: PlaceBet creates bet Position with correct properties
  - [ ] Test: CashOutBet returns betAmount +/- P&L correctly
  - [ ] Test: Bet P&L calculation — price drop = profit, price rise = loss
  - [ ] Test: Max loss capped at betAmount (price doubles)
  - [ ] Test: Reject bet when long position exists
  - [ ] Test: Reject bet when bet already active on stock
  - [ ] Test: Sell button no longer opens shorts (AC: 1, 10)
  - [ ] Test: Buy button no longer covers shorts (AC: 10)
  - [ ] Test: LiquidateAllPositions handles bets correctly
  - [ ] Test: ShortSqueeze price spike affects bet P&L correctly
  - [ ] File: `Assets/Tests/Runtime/Trading/BetAgainstTests.cs`
  - [ ] File: `Assets/Tests/Runtime/Core/BetIntegrationTests.cs`

## Dev Notes

### Architecture Compliance
- **Setup-Oriented Generation:** New bet panel created by `UISetup.ExecuteBetPanel()` — programmatic uGUI Canvas hierarchy. No Inspector config.
- **EventBus Communication:** New `BetButtonPressedEvent` for UI→GameRunner communication. Existing `TradeExecutedEvent` and `TradeFeedbackEvent` reused for bet execution feedback.
- **No Inspector Config:** Bet constants in `GameConfig`. All UI built in code.
- **Performance:** Bet P&L is a simple arithmetic calculation — no allocation, no performance concern.

### Key Design Decisions
- **Fixed-stake model vs. share-based:** Player bets a CASH AMOUNT, not a share quantity. This is simpler to understand: "I'm betting $100 the price drops." P&L is percentage-based on price movement relative to entry price.
- **P&L formula:** `profit = betAmount * ((entryPrice - currentPrice) / entryPrice)`. If price drops 10%, player profits 10% of bet amount. If price rises 10%, player loses 10% of bet amount. Max loss = bet amount (when price doubles). Max profit configurable via multiplier.
- **Separate UI panel:** The bet panel is visually and spatially distinct from Buy/Sell. This makes the mechanic discoverable and prevents confusion with selling.
- **One bet per stock:** Simplifies the model. Player must cash out before placing a new bet. No averaging-in for bets.
- **Smart Sell removal:** `ExecuteSmartSell()` becomes a pure sell-long method. `ExecuteSmartBuy()` becomes a pure buy-long method. No more auto-detecting position type. This is cleaner and less surprising.
- **D/F keys:** Chosen to be adjacent to but distinct from B/S (buy/sell). D = "Down bet" (bet against), F = "Finish bet" (cash out).

### Dependencies
- Ideally implemented AFTER FIX-10 (trade execution delay), so the delay also applies to bet placement
- No new packages or dependencies required
- If FIX-10 is not yet implemented, bet placement can execute instantly (delay can be added later)

### Edge Cases
- If player has exactly $100 cash and places $100 bet, they can't buy stocks until they cash out — this is intentional (bet locks up capital)
- If stock price rises above 2x entry price, bet loss is capped at betAmount (player can't lose more than they bet)
- If round ends with active bet, auto-liquidation closes it at current price via `LiquidateAllPositions()`
- ShortSqueeze event: price spike means bet loses value rapidly — this is the dramatic tension point. No special handling needed beyond correct P&L calc.
- Quantity presets in bet panel ($50/$100/$250/$500) should dim if player can't afford them

### Previous Story Learnings
- From FIX-6: Trade panel architecture (UISetup.ExecuteTradePanel) provides a good template for the bet panel
- From FIX-7: PositionOverlay already distinguishes LONG/SHORT — extending for BET AGAINST is straightforward
- From FIX-9: ShortSqueeze already targets activeStocks[0] — no special bet targeting needed
- Smart Sell/Buy auto-detection in GameRunner is already complex (200+ lines) — removing it simplifies significantly

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

- 2026-02-14: Story created from FIX Sprint 2 epic definition
