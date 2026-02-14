# Story FIX-10: Trade Execution Delay & Button Cooldown

Status: ready-for-dev

## Story

As a player,
I want a brief delay after pressing Buy or Sell before the trade executes,
so that trading feels deliberate and I can't spam-click to instantly fill massive positions.

## Problem Analysis

Currently trades execute instantly on button press with zero delay. This removes tension and allows rapid-fire clicking to bypass the quantity system. A short cooldown creates a "market fill" feel and adds weight to each trade decision.

**Current Trade Flow (instant):**
1. Player presses B/S key or clicks BUY/SELL button
2. `GameRunner.OnTradeButtonPressed()` or `HandleTradingInput()` fires immediately
3. `ExecuteSmartBuy()`/`ExecuteSmartSell()` calls `TradeExecutor` directly
4. Trade executes at current price, feedback shows instantly

**Desired Trade Flow (with delay):**
1. Player presses B/S key or clicks BUY/SELL button
2. Cooldown starts — button dims/pulses, additional presses ignored
3. After ~0.4s cooldown completes
4. Trade executes at the price AT THAT MOMENT (not the price at press time) — natural slippage
5. Feedback message appears after execution

**Affected Code:**
- `Scripts/Runtime/Core/GameRunner.cs` — trade orchestration in `ExecuteSmartBuy()`/`ExecuteSmartSell()`, `OnTradeButtonPressed()`, `HandleTradingInput()`
- `Scripts/Runtime/UI/TradeFeedback.cs` — feedback timing (currently fires on execution, which is already correct — just ensure no change needed)
- `Scripts/Setup/UISetup.cs` — button visual state during cooldown (BUY/SELL button Image references needed)
- `Scripts/Setup/Data/GameConfig.cs` — new `TradeExecutionDelay` constant

## Acceptance Criteria

1. After pressing Buy or Sell (button or keyboard), a brief cooldown of ~0.3-0.5s before the trade executes
2. During cooldown: button appears visually "processing" (dimmed color)
3. During cooldown: additional Buy/Sell presses are ignored (no queuing)
4. Trade executes at the price when the cooldown COMPLETES (not when pressed) — this is the "fill price" creating natural slippage
5. TradeFeedback message appears after execution, not on press
6. Keyboard shortcuts (B/S) respect the same cooldown
7. Cooldown duration configurable in GameConfig (e.g., `TradeExecutionDelay = 0.4f`)
8. No cooldown during auto-liquidation at market close (`Portfolio.LiquidateAllPositions` is unaffected)

## Tasks / Subtasks

- [ ] Task 1: Add `TradeExecutionDelay` config constant (AC: 7)
  - [ ] Add `public static readonly float TradeExecutionDelay = 0.4f` to `GameConfig`
  - [ ] File: `Assets/Scripts/Setup/Data/GameConfig.cs`

- [ ] Task 2: Add cooldown state tracking to `GameRunner` (AC: 1, 3, 6)
  - [ ] Add `_tradeCooldownTimer` float field (counts down to 0)
  - [ ] Add `_pendingTradeIsBuy` bool to track which trade type is pending
  - [ ] Add `_isTradeCooldownActive` bool for quick checks
  - [ ] In `Update()`, tick down `_tradeCooldownTimer` by `Time.deltaTime`
  - [ ] When timer reaches 0 and cooldown is active, execute the pending trade
  - [ ] File: `Assets/Scripts/Runtime/Core/GameRunner.cs`

- [ ] Task 3: Gate trade execution behind cooldown in `GameRunner` (AC: 1, 3, 4, 6)
  - [ ] Modify `HandleTradingInput()`: B/S keys start cooldown instead of immediate execution
  - [ ] Modify `OnTradeButtonPressed()`: button presses start cooldown instead of immediate execution
  - [ ] If cooldown already active, ignore the input (no queuing)
  - [ ] Extract trade execution into `ExecutePendingTrade()` called when timer expires
  - [ ] `ExecutePendingTrade()` reads CURRENT price at execution time (not press time)
  - [ ] File: `Assets/Scripts/Runtime/Core/GameRunner.cs`

- [ ] Task 4: Visual button feedback during cooldown (AC: 2)
  - [ ] Store references to BUY/SELL button `Image` components from `ExecuteTradePanel()`
  - [ ] When cooldown starts: dim the active button (reduce alpha or darken color)
  - [ ] When cooldown ends: restore button to normal color
  - [ ] Option: add subtle pulsing during cooldown via `Mathf.PingPong` in Update
  - [ ] File: `Assets/Scripts/Runtime/Core/GameRunner.cs`
  - [ ] File: `Assets/Scripts/Setup/UISetup.cs` (expose button Image refs if needed)

- [ ] Task 5: Ensure auto-liquidation bypasses cooldown (AC: 8)
  - [ ] Verify `Portfolio.LiquidateAllPositions()` path does NOT go through GameRunner's trade methods
  - [ ] Confirm `MarketCloseState` or `TradingState.Exit()` calls liquidation directly without cooldown
  - [ ] Add test to verify liquidation is unaffected by cooldown state
  - [ ] File: `Assets/Scripts/Runtime/Core/GameRunner.cs` (verification only)

- [ ] Task 6: Write unit tests for trade execution delay (AC: 1, 2, 3, 4, 6, 7, 8)
  - [ ] Test: Trade does not execute on the same frame as button press
  - [ ] Test: Trade executes after `TradeExecutionDelay` seconds
  - [ ] Test: Additional presses during cooldown are ignored
  - [ ] Test: Fill price uses price at execution time, not press time
  - [ ] Test: Cooldown applies to both keyboard and button inputs
  - [ ] Test: Auto-liquidation is not affected by cooldown
  - [ ] Test: Cooldown resets properly after trade completes
  - [ ] File: `Assets/Tests/Runtime/Core/TradeExecutionDelayTests.cs`

## Dev Notes

### Architecture Compliance
- **Setup-Oriented Generation:** UISetup creates the BUY/SELL buttons. GameRunner holds runtime references. No Inspector config.
- **EventBus Communication:** `TradeButtonPressedEvent` already published by buttons — GameRunner subscribes. `TradeFeedbackEvent` published after execution. No new events needed.
- **No Inspector Config:** `TradeExecutionDelay` added to `GameConfig` as static readonly.
- **Performance:** Cooldown is a simple float countdown in Update — zero allocation, negligible cost.

### Key Design Decisions
- The cooldown lives in `GameRunner` because it already orchestrates all trade execution. Adding a separate "TradeCooldown" system would be over-engineering for a float timer.
- Price is read at EXECUTION time (when timer expires), not at press time. This creates organic slippage — the price may move during the delay, making trades feel like real market fills.
- No queuing: if player mashes the button, only the first press registers. This prevents accidental double-trades.
- Auto-liquidation (`Portfolio.LiquidateAllPositions()`) already bypasses GameRunner's trade methods — it's called directly by MarketCloseState. No changes needed there, just verification.

### Dependencies
- Independent of all other FIX stories
- No new packages or dependencies required

### Edge Cases
- If `TradingState.IsActive` becomes false during cooldown (e.g., round timer expires), cancel the pending trade — don't execute it during market close
- If player switches quantity preset during cooldown, the new quantity should apply (price and quantity both read at execution time)
- Cooldown visual state should reset when trading phase ends (subscribe to `TradingPhaseEndedEvent`)

### Previous Story Learnings
- From FIX-6: BUY/SELL buttons are created by `UISetup.ExecuteTradePanel()` — the sell button Image is `TradingHUD.LossRed` and buy is `TradingHUD.ProfitGreen`
- From FIX-6: `TradeButtonPressedEvent` is the bridge between UI buttons and GameRunner
- `QuantitySelector.GetCurrentQuantity()` reads portfolio state at call time — calling it at execution time (not press time) ensures quantity reflects current state

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
