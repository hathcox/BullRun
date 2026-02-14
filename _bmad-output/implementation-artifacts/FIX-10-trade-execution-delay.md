# Story FIX-10: Trade Execution Delay & Button Cooldown

Status: done

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

- [x] Task 1: Add `TradeExecutionDelay` config constant (AC: 7)
  - [x] Add `public static readonly float TradeExecutionDelay = 0.4f` to `GameConfig`
  - [x] File: `Assets/Scripts/Setup/Data/GameConfig.cs`

- [x] Task 2: Add cooldown state tracking to `GameRunner` (AC: 1, 3, 6)
  - [x] Add `_tradeCooldownTimer` float field (counts down to 0)
  - [x] Add `_pendingTradeIsBuy` bool to track which trade type is pending
  - [x] Add `_isTradeCooldownActive` bool for quick checks
  - [x] In `Update()`, tick down `_tradeCooldownTimer` by `Time.deltaTime`
  - [x] When timer reaches 0 and cooldown is active, execute the pending trade
  - [x] File: `Assets/Scripts/Runtime/Core/GameRunner.cs`

- [x] Task 3: Gate trade execution behind cooldown in `GameRunner` (AC: 1, 3, 4, 6)
  - [x] Modify `HandleTradingInput()`: B/S keys start cooldown instead of immediate execution
  - [x] Modify `OnTradeButtonPressed()`: button presses start cooldown instead of immediate execution
  - [x] If cooldown already active, ignore the input (no queuing)
  - [x] Extract trade execution into `ExecutePendingTrade()` called when timer expires
  - [x] `ExecutePendingTrade()` reads CURRENT price at execution time (not press time)
  - [x] File: `Assets/Scripts/Runtime/Core/GameRunner.cs`

- [x] Task 4: Visual button feedback during cooldown (AC: 2)
  - [x] Store references to BUY/SELL button `Image` components from `ExecuteTradePanel()`
  - [x] When cooldown starts: dim the active button (reduce alpha or darken color)
  - [x] When cooldown ends: restore button to normal color
  - [ ] Option: add subtle pulsing during cooldown via `Mathf.PingPong` in Update (skipped — dimming provides sufficient feedback)
  - [x] File: `Assets/Scripts/Runtime/Core/GameRunner.cs`
  - [x] File: `Assets/Scripts/Setup/UISetup.cs` (expose button Image refs if needed)

- [x] Task 5: Ensure auto-liquidation bypasses cooldown (AC: 8)
  - [x] Verify `Portfolio.LiquidateAllPositions()` path does NOT go through GameRunner's trade methods
  - [x] Confirm `MarketCloseState` or `TradingState.Exit()` calls liquidation directly without cooldown
  - [x] Add test to verify liquidation is unaffected by cooldown state
  - [x] File: `Assets/Scripts/Runtime/Core/GameRunner.cs` (verification only)

- [x] Task 6: Write unit tests for trade execution delay (AC: 1, 2, 3, 4, 6, 7, 8)
  - [x] Test: Trade does not execute on the same frame as button press
  - [x] Test: Trade executes after `TradeExecutionDelay` seconds
  - [x] Test: Additional presses during cooldown are ignored
  - [x] Test: Fill price uses price at execution time, not press time
  - [x] Test: Cooldown applies to both keyboard and button inputs
  - [x] Test: Auto-liquidation is not affected by cooldown
  - [x] Test: Cooldown resets properly after trade completes
  - [x] File: `Assets/Tests/Runtime/Core/TradeExecutionDelayTests.cs`

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
- Add `TradeExecutionDelay` (0.4f) constant to GameConfig
- Add cooldown state fields to GameRunner (`_tradeCooldownTimer`, `_pendingTradeIsBuy`, `_isTradeCooldownActive`)
- Modify `HandleTradingInput()` and `OnTradeButtonPressed()` to call `StartTradeCooldown()` instead of executing trades immediately
- New `StartTradeCooldown(bool isBuy)` ignores input if cooldown already active (no queuing)
- Timer ticks down in `Update()` via `Time.deltaTime`; when expired, calls `ExecutePendingTrade()` which routes to `ExecuteSmartBuy()`/`ExecuteSmartSell()` reading CURRENT price (natural slippage)
- Expose `BuyButtonImage`/`SellButtonImage` on QuantitySelector, wired from UISetup
- `DimButton()` reduces alpha to `GameConfig.CooldownDimAlpha` during cooldown; `RestoreButtonVisuals()` resets on completion
- Subscribe to `TradingPhaseEndedEvent` to cancel pending cooldown if round timer expires mid-cooldown
- Verified `MarketCloseState.LiquidateAllPositions()` bypasses GameRunner entirely — no cooldown impact
- Note: pulsing animation (optional subtask) not implemented — dimming provides clear visual feedback

### Completion Notes
All 6 tasks implemented. Key design: cooldown lives entirely in GameRunner as a simple float timer with zero allocation. Three edge cases handled: (1) TradingState becoming inactive during cooldown cancels the trade, (2) quantity preset changes during cooldown apply at execution time, (3) TradingPhaseEndedEvent resets button visuals. Auto-liquidation path confirmed independent of GameRunner trade methods. 14 unit tests covering config validation, auto-liquidation bypass, cooldown state machine logic (input rejection, phase-end cancellation, trading-inactive cancellation), event plumbing, price slippage, and timer accuracy. Tests are Edit Mode (pure logic) since GameRunner is a MonoBehaviour — full integration testing requires Unity Play Mode.

### Debug Log
- Verified MarketCloseState.Enter() calls `ctx.Portfolio.LiquidateAllPositions()` directly — does not route through GameRunner's HandleTradingInput/OnTradeButtonPressed/ExecuteSmartBuy/ExecuteSmartSell
- TradeFeedback already fires on TradeFeedbackEvent (published after execution) — no changes needed to TradeFeedback.cs
- Original button colors stored in Start() after ExecuteTradePanel() returns, ensuring correct restore values

## File List

- `Assets/Scripts/Setup/Data/GameConfig.cs` (modified) — Added `TradeExecutionDelay` and `CooldownDimAlpha` constants
- `Assets/Scripts/Runtime/Core/GameRunner.cs` (modified) — Added cooldown state fields, timer logic in Update(), StartTradeCooldown(), DimButton() using GameConfig.CooldownDimAlpha, RestoreButtonVisuals(), ExecutePendingTrade(), OnTradingPhaseEnded(); modified HandleTradingInput() and OnTradeButtonPressed() to use cooldown
- `Assets/Scripts/Runtime/UI/QuantitySelector.cs` (modified) — Added `BuyButtonImage` and `SellButtonImage` properties; fixed line endings (LF → CRLF)
- `Assets/Scripts/Setup/UISetup.cs` (modified) — Wired BuyButtonImage/SellButtonImage on QuantitySelector in ExecuteTradePanel()
- `Assets/Tests/Runtime/Core/TradeExecutionDelayTests.cs` (new) — 14 unit tests for config, auto-liquidation bypass, cooldown state machine logic, events, slippage, timer accuracy

## Change Log

- 2026-02-14: Story created from FIX Sprint 2 epic definition
- 2026-02-14: Implemented trade execution delay with 0.4s cooldown, button dimming, input gating, TradingPhaseEnded cancellation, and auto-liquidation bypass verification. Added 13 unit tests.
- 2026-02-14: Code review fixes — Extracted dim alpha magic number to `GameConfig.CooldownDimAlpha`; rewrote tests (removed 5 redundant config assertions including meaningless `Assert.IsNotNull` on float, added 4 cooldown state machine behavior tests for input rejection, phase-end cancellation, trading-inactive cancellation, and cooldown-then-reaccept); fixed line endings in QuantitySelector.cs; corrected misleading [x] checkbox on unimplemented pulsing subtask. 14 tests total.
