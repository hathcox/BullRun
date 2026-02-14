# Story FIX-10: Post-Trade Cooldown with Countdown Timer

Status: done

## Story

As a player,
I want trades to execute instantly when I press Buy or Sell, but then be locked out of ALL trading for 3 seconds with a visible countdown timer,
so that I can't spam-click trades but still get the satisfaction of immediate execution.

## Problem Analysis

The previous implementation (v1) misunderstood the intent. It added a **pre-trade delay** — the trade was held for 0.4s before executing, creating artificial slippage. What the player actually wants is **instant execution** followed by a **post-trade cooldown** that locks out both Buy and Sell for 3 seconds with a visible timer.

**v1 Flow (WRONG — what was built):**
1. Player presses Buy/Sell
2. Button dims, 0.4s delay starts
3. Trade executes after delay at new price (slippage)
4. Only the pressed button was affected

**v2 Flow (CORRECT — what we actually want):**
1. Player presses Buy/Sell
2. Trade executes **instantly** at current price — immediate feedback
3. **Both** Buy AND Sell buttons lock out for 3 seconds
4. A countdown timer (e.g., "3.0s", "2.1s", "0.4s") displays just above the button section
5. After 3 seconds: timer disappears, both buttons unlock, player can trade again

**Key Differences from v1:**
- Trade is instant, not delayed
- Lockout is POST-trade, not pre-trade
- BOTH buttons lock (not just the one pressed)
- 3 seconds (not 0.4s)
- Visible countdown timer UI element (not just a dim)
- No slippage mechanic — trade price is what you see when you click

**Affected Code:**
- `Scripts/Runtime/Core/GameRunner.cs` — trade orchestration: remove pre-trade delay, add post-trade cooldown
- `Scripts/Setup/UISetup.cs` — create countdown timer TextMeshPro element above buttons
- `Scripts/Setup/Data/GameConfig.cs` — update `TradeExecutionDelay` to 3.0f, add timer display config
- `Scripts/Runtime/UI/QuantitySelector.cs` — may need reference to timer text element

## Acceptance Criteria

1. Trade executes **instantly** on Buy/Sell press (button click or B/S keyboard shortcut) — no delay
2. Immediately after a trade executes, **both** Buy and Sell buttons lock out for 3 seconds
3. During lockout: both buttons are visually dimmed/disabled — presses are ignored
4. During lockout: a countdown timer text (e.g., "2.4s") is visible just above the trade button section
5. Countdown timer updates every frame showing remaining time (one decimal place)
6. When countdown reaches 0: timer text disappears, both buttons re-enable, player can trade again
7. Keyboard shortcuts (B/S) are also blocked during lockout
8. Cooldown duration configurable in GameConfig (`PostTradeCooldown = 3.0f`)
9. Auto-liquidation at market close is unaffected by cooldown (bypasses GameRunner)
10. If trading phase ends during cooldown, cooldown cancels and buttons/timer reset

## Tasks / Subtasks

- [x] Task 1: Update GameConfig constants (AC: 8)
  - [x] Rename `TradeExecutionDelay` → `PostTradeCooldown = 3.0f`
  - [x] Keep `CooldownDimAlpha` for button dimming
  - [x] File: `Assets/Scripts/Setup/Data/GameConfig.cs`

- [x] Task 2: Revert trade execution to instant (AC: 1)
  - [x] Remove pre-trade cooldown gating from `HandleTradingInput()` and `OnTradeButtonPressed()`
  - [x] Trades call `ExecuteSmartBuy()`/`ExecuteSmartSell()` immediately on input (restore original instant behavior)
  - [x] Remove `_pendingTradeIsBuy` and `ExecutePendingTrade()` — no longer needed
  - [x] File: `Assets/Scripts/Runtime/Core/GameRunner.cs`

- [x] Task 3: Add post-trade cooldown after instant execution (AC: 2, 3, 7)
  - [x] After `ExecuteSmartBuy()`/`ExecuteSmartSell()` completes, start `_postTradeCooldownTimer = GameConfig.PostTradeCooldown`
  - [x] Set `_isPostTradeCooldownActive = true`
  - [x] While cooldown active: reject ALL trade inputs (both buy AND sell, keyboard AND button)
  - [x] In `Update()`: tick timer down, when it reaches 0, set `_isPostTradeCooldownActive = false`
  - [x] File: `Assets/Scripts/Runtime/Core/GameRunner.cs`

- [x] Task 4: Dim BOTH buttons during cooldown (AC: 3)
  - [x] When cooldown starts: dim **both** Buy and Sell buttons (not just the pressed one)
  - [x] When cooldown ends: restore **both** buttons to original colors
  - [x] File: `Assets/Scripts/Runtime/Core/GameRunner.cs`

- [x] Task 5: Create countdown timer UI element (AC: 4, 5, 6)
  - [x] In `UISetup.ExecuteTradePanel()`: create a Text element positioned just above the trade button area
  - [x] Style: centered, gold/yellow (warning color), bold 18pt, visible but not obtrusive
  - [x] Text hidden by default (`gameObject.SetActive(false)`)
  - [x] Expose reference via `QuantitySelector.CooldownTimerText`
  - [x] File: `Assets/Scripts/Setup/UISetup.cs`
  - [x] File: `Assets/Scripts/Runtime/UI/QuantitySelector.cs`

- [x] Task 6: Update GameRunner to drive countdown timer display (AC: 4, 5, 6)
  - [x] When cooldown starts: show timer text, set to "3.0s"
  - [x] In `Update()`: update timer text each frame with remaining time (format: `$"{remaining:F1}s"`)
  - [x] When cooldown ends: hide timer text
  - [x] File: `Assets/Scripts/Runtime/Core/GameRunner.cs`

- [x] Task 7: Handle edge cases (AC: 9, 10)
  - [x] Verified auto-liquidation still bypasses GameRunner (no change from v1)
  - [x] When `TradingPhaseEndedEvent` fires during cooldown: cancel cooldown, hide timer, restore both buttons
  - [x] Cooldown only starts on successful trades (failed trades don't trigger lockout)
  - [x] Quantity preset changes (1-4 keys) still work during cooldown
  - [x] File: `Assets/Scripts/Runtime/Core/GameRunner.cs`

- [x] Task 8: Update unit tests for new behavior (AC: all)
  - [x] Test: Trade executes instantly on button press (no delay)
  - [x] Test: Trade executes at provided price (no slippage)
  - [x] Test: After trade, both buy AND sell inputs are rejected for 3 seconds
  - [x] Test: After cooldown expires, trades are accepted again
  - [x] Test: Cooldown lasts full duration
  - [x] Test: Only successful trades start cooldown
  - [x] Test: Countdown timer value decreases over time
  - [x] Test: Countdown timer format is correct ("X.Xs")
  - [x] Test: TradingPhaseEndedEvent cancels active cooldown
  - [x] Test: Auto-liquidation is not affected by cooldown (long + short)
  - [x] Test: PostTradeCooldown config is 3.0f
  - [x] Test: Event plumbing (TradeButtonPressedEvent, TradingPhaseEndedEvent)
  - [x] File: `Assets/Tests/Runtime/Core/TradeExecutionDelayTests.cs` — 15 tests total

## Dev Notes

### Architecture Compliance
- **Setup-Oriented Generation:** UISetup creates the countdown timer TextMeshPro. GameRunner holds runtime reference. No Inspector config.
- **EventBus Communication:** `TradeButtonPressedEvent` used for button presses. `TradeFeedbackEvent` fires immediately after instant trade. No new events needed.
- **No Inspector Config:** `PostTradeCooldown` in `GameConfig` as static readonly.
- **Performance:** Simple float countdown in Update — zero allocation, negligible cost.

### Key Design Decisions
- Trade executes INSTANTLY — the player gets immediate feedback and satisfaction from clicking Buy/Sell
- The cooldown is a POST-trade lockout, not a pre-trade gate — this prevents spam without frustrating the player with delayed execution
- BOTH buttons lock — you can't buy then immediately sell (or vice versa). The 3-second cooldown applies to ALL trading
- Visible countdown timer gives the player clear feedback on when they can trade again — reduces frustration vs a silent lockout
- No slippage mechanic — the price you see is the price you get

### Dependencies
- Independent of all other FIX stories
- No new packages or dependencies required

### Edge Cases
- If trading phase ends during cooldown: cancel cooldown, hide timer, restore buttons
- Quantity preset changes during cooldown are fine — they just set up the next trade
- If player has no cash/position and presses trade: normal validation rejects it before cooldown starts (cooldown should only start on successful trades)

### Previous Story Learnings (from v1)
- From FIX-6: BUY/SELL buttons created by `UISetup.ExecuteTradePanel()` — sell is `TradingHUD.LossRed`, buy is `TradingHUD.ProfitGreen`
- From FIX-6: `TradeButtonPressedEvent` bridges UI buttons and GameRunner
- Button Image references already wired to QuantitySelector from v1 implementation
- Auto-liquidation path (`MarketCloseState` → `Portfolio.LiquidateAllPositions()`) confirmed independent of GameRunner

## File List

- `Assets/Scripts/Setup/Data/GameConfig.cs` (modify) — Rename delay → `PostTradeCooldown = 3.0f`
- `Assets/Scripts/Runtime/Core/GameRunner.cs` (modify) — Remove pre-trade delay, add post-trade cooldown + countdown timer driving
- `Assets/Scripts/Setup/UISetup.cs` (modify) — Create countdown timer TextMeshPro above trade buttons
- `Assets/Scripts/Runtime/UI/QuantitySelector.cs` (modify) — Store reference to countdown timer text
- `Assets/Tests/Runtime/Core/TradeExecutionDelayTests.cs` (rewrite) — Tests for instant trade + post-trade lockout + timer

## Dev Agent Record

### Implementation Plan (v2)
- Renamed `TradeExecutionDelay` → `PostTradeCooldown = 3.0f` in GameConfig
- Removed pre-trade delay: `_pendingTradeIsBuy`, `ExecutePendingTrade()`, `StartTradeCooldown()`, `DimButton()` all removed
- `ExecuteSmartBuy()`/`ExecuteSmartSell()` changed from `void` → `bool` return, indicating trade success
- `HandleTradingInput()`: quantity preset shortcuts (1-4) always work, trade keys (B/S) blocked during cooldown, trade executes instantly then triggers cooldown on success
- `OnTradeButtonPressed()`: checks cooldown, executes instantly, starts cooldown on success
- New `StartPostTradeCooldown()`: sets timer, dims BOTH buttons, shows countdown text
- New `DimBothButtons()`: dims both buy AND sell (not just the pressed one)
- New `ShowCooldownTimer()`/`HideCooldownTimer()`/`UpdateCooldownTimerDisplay()`: drives the countdown text UI
- Countdown timer Text element created in `UISetup.ExecuteTradePanel()`, positioned above the container, stored via `QuantitySelector.CooldownTimerText`
- Timer format: `$"{remaining:F1}s"` (e.g., "2.4s")

### Completion Notes
All 8 tasks implemented. Key change from v1: trade is now INSTANT (no slippage), with a 3-second POST-trade cooldown that locks BOTH buttons. Visible countdown timer (gold/yellow, 18pt bold) positioned above the trade panel container. Cooldown only triggers on successful trades — failed trades (no cash, no position) don't start the lockout. Quantity preset changes (1-4) remain available during cooldown. Edge cases preserved: TradingPhaseEndedEvent cancels cooldown, auto-liquidation bypasses GameRunner. 15 unit tests covering config, instant execution, cooldown state machine, timer display, phase-end cancellation, auto-liquidation bypass, and event plumbing.

## Change Log

- 2026-02-14: Story created from FIX Sprint 2 epic definition
- 2026-02-14: v1 implemented — pre-trade delay with 0.4s cooldown, button dimming, slippage
- 2026-02-14: v1 code review fixes — extracted magic numbers, rewrote tests
- 2026-02-14: **STORY REWRITTEN (v2)** — Requirements corrected per product owner. Changed from pre-trade delay to post-trade cooldown. Trade is now instant, followed by 3-second lockout on BOTH buttons with visible countdown timer. Previous implementation needs to be reverted/reworked.
- 2026-02-14: **v2 IMPLEMENTED** — All 8 tasks complete. Instant trade + 3s post-trade cooldown on both buttons + countdown timer UI. 15 unit tests.
