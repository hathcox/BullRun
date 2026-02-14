# Story FIX-11: Short Selling Mechanic Redesign — Separate Short Button with Timed Lifecycle

Status: done

## Story

As a player,
I want a dedicated SHORT button completely separate from Buy/Sell that lets me short 1 share with a timed lifecycle (forced hold → cash-out window → cooldown),
so that shorting feels like a deliberate side-bet with clear risk windows rather than a confusing hidden mechanic.

## Problem Analysis

Currently shorting is buried in the "Smart Sell" system — if you have no long position and press Sell, it silently opens a short. Players don't realize they've shorted. This redesign makes shorting a fully separate, visible mechanic with its own button, UI panel, and timed state machine. Shorts are a small bonus play alongside main long trades — player can hold a long AND a short on the same stock simultaneously. With the new $10 economy (FIX-14) and 1-share trading (FIX-13), a short on a $2 penny stock nets small gains/losses, keeping it as a fun side mechanic.

**Current Short Flow (confusing):**
1. Player presses S or clicks SELL with no long position
2. `GameRunner.ExecuteSmartSell()` silently opens a short via `TradeExecutor.ExecuteShort()`
3. `Portfolio.OpenShort()` deducts margin collateral (50% of position value)
4. Short appears in PositionOverlay as "SHORT" — but player may not realize what happened
5. To close: player must press B (Smart Buy auto-detects and covers)
6. Terminology uses "margin," "collateral," "cover" — financial jargon

**Desired Flow (timed state machine):**
1. Player presses D key or clicks dedicated SHORT button (separate from Buy/Sell)
2. Exactly 1 share is shorted at current market price
3. Button greys out with 8-second forced-hold countdown
4. After 8s, button re-enables as "CASH OUT" with 10-second auto-close timer
5. Player clicks CASH OUT (or timer auto-closes), P&L realized
6. 10-second cooldown before next short available
7. Player can hold long + short simultaneously — completely independent

**Short Button State Machine:**

```
ROUND START
  │
  ▼
[ROUND_LOCKOUT] ── SHORT button greyed, shows "5s" countdown
  │ (5 seconds)
  ▼
[READY] ── SHORT button enabled, normal color
  │ (player clicks SHORT)
  ▼
[HOLDING] ── 1 share shorted at market price
  │            Button greyed, shows "8s" forced-hold countdown
  │            P&L panel appears showing real-time profit/loss
  │ (8 seconds)
  ▼
[CASH_OUT_WINDOW] ── Button re-enables with "CASH OUT" text
  │                   10s auto-close countdown displayed
  │                   Countdown flashes when ≤4s remain
  │ (player clicks CASH OUT — or 10s expires → auto-close)
  ▼
[COOLDOWN] ── Short closed, P&L realized
  │            Button greyed, shows "10s" cooldown countdown
  │            P&L panel clears
  │ (10 seconds)
  ▼
[READY] ── cycle repeats
```

**Any state → ROUND END:** Short auto-closes at market price, all cooldowns cancel, UI resets.

**Affected Code:**
- `Scripts/Runtime/Core/GameRunner.cs` — remove Smart Sell/Buy short logic, add short state machine, D key routing
- `Scripts/Runtime/Trading/Portfolio.cs` — support simultaneous long + short on same stock
- `Scripts/Runtime/Trading/Position.cs` — ensure short position model works alongside long
- `Scripts/Runtime/Trading/TradeExecutor.cs` — update short open/close methods
- `Scripts/Setup/UISetup.cs` — SHORT button, Short P&L panel, countdown text elements
- `Scripts/Runtime/UI/PositionOverlay.cs` — short P&L display panel
- `Scripts/Runtime/UI/TradeFeedback.cs` — short-specific feedback messages
- `Scripts/Runtime/UI/QuantitySelector.cs` — short uses fixed share count (not quantity selector)
- `Scripts/Runtime/Events/EventScheduler.cs` — ShortSqueeze targets active short position
- `Scripts/Setup/Data/GameConfig.cs` — short lifecycle timing constants

## Acceptance Criteria

1. Dedicated SHORT button, visually distinct from Buy/Sell (e.g., hot pink/purple)
2. Shorting is COMPLETELY separate from Sell — Sell ONLY sells long positions, never opens a short
3. Remove Smart Sell logic that auto-opens shorts from the Sell button
4. Remove Smart Buy cover logic — Buy only buys longs
5. Player can hold a long position AND a short position on the same stock simultaneously
6. Only ONE short position at a time
7. Short is always 1 share (base, before item upgrades)
8. P&L calculated as `(open_price - close_price) * shares` — profit when price drops
9. **Round start lockout (5s):** SHORT button greyed with visible countdown. Buy/Sell NOT affected
10. **Forced hold (8s):** After opening short, button greys with countdown. Cannot cash out early
11. **Cash-out window (10s):** Button re-enables as "CASH OUT". Auto-close timer displayed prominently. Timer flashes/pulses when ≤4 seconds remain
12. **Auto-close:** If player doesn't click CASH OUT within 10s, short closes at market price. No penalty vs. manual close
13. **Post-close cooldown (10s):** Button greys with countdown before next short available
14. Round end during ANY state: short auto-closes at market price, all timers cancel, UI resets
15. D key opens short when READY, D key cashes out when in CASH_OUT_WINDOW
16. Separate Short P&L panel showing: entry price, current P&L (green/red), auto-close countdown
17. P&L panel only visible when short is active (HOLDING or CASH_OUT_WINDOW states)
18. Button text changes per state: "SHORT" → "8s" → "CASH OUT (10s)" → "10s" → "SHORT"
19. ShortSqueeze event still targets active short position for dramatic effect
20. Item integration points: all timing durations and share count sourced from GameConfig constants so items can modify at runtime

## Tasks / Subtasks

- [x] Task 1: Add short lifecycle config constants to GameConfig (AC: 9-13, 20)
  - [x] Add `ShortRoundStartLockout = 5.0f` — seconds before SHORT button enables at round start
  - [x] Add `ShortForcedHoldDuration = 8.0f` — seconds player must hold short before cash-out available
  - [x] Add `ShortCashOutWindow = 10.0f` — seconds player has to manually cash out before auto-close
  - [x] Add `ShortCashOutFlashThreshold = 4.0f` — seconds remaining when auto-close countdown starts flashing
  - [x] Add `ShortPostCloseCooldown = 10.0f` — seconds after close before next short available
  - [x] Add `ShortBaseShares = 1` — base share count per short (items can increase)
  - [x]File: `Assets/Scripts/Setup/Data/GameConfig.cs`

- [x] Task 2: Add ShortState enum and state tracking to GameRunner (AC: 9-14)
  - [x] Add `ShortState` enum: `RoundLockout`, `Ready`, `Holding`, `CashOutWindow`, `Cooldown`
  - [x] Add state tracking fields: `_shortState`, `_shortTimer`, `_shortEntryPrice`, `_shortShares`
  - [x] Add `UpdateShortStateMachine()` called from `Update()` during trading phase
  - [x]State transitions: timer-driven for lockout/hold/cashout/cooldown, input-driven for Ready→Holding and CashOutWindow→Cooldown
  - [x]On `RoundStartedEvent`: set state to `RoundLockout`, start 5s timer
  - [x]On round end / `TradingPhaseEndedEvent`: if short active, auto-close at market price, reset state
  - [x]File: `Assets/Scripts/Runtime/Core/GameRunner.cs`

- [x] Task 3: Implement short open/close logic in state machine (AC: 5-8)
  - [x]`Ready` + D key press → open short: call `TradeExecutor.ExecuteShort()` with `ShortBaseShares` at current price, transition to `Holding`, start 8s timer
  - [x]`CashOutWindow` + D key press → close short: call `TradeExecutor.CoverShort()` at current price, realize P&L, transition to `Cooldown`, start 10s timer
  - [x]`CashOutWindow` + timer expires → auto-close: same as manual close, no penalty
  - [x]P&L = `(entryPrice - currentPrice) * shares` — positive when price dropped
  - [x]File: `Assets/Scripts/Runtime/Core/GameRunner.cs`

- [x] Task 4: Support simultaneous long + short on same stock in Portfolio (AC: 5, 6)
  - [x]Currently `Portfolio` stores positions by stockId in a dictionary — refactor to allow both a long and short position for the same stock
  - [x]Options: separate `_shortPositions` dictionary, or compound key `stockId + "_short"`, or `Position` list per stock
  - [x]`GetPosition(stockId)` remains for long positions; add `GetShortPosition(stockId)` for shorts
  - [x]`LiquidateAllPositions()` must close both longs AND shorts
  - [x]Only ONE short per stock allowed — reject if short already exists
  - [x]File: `Assets/Scripts/Runtime/Trading/Portfolio.cs`

- [x] Task 5: Remove Smart Sell/Buy short logic from GameRunner (AC: 2, 3, 4)
  - [x]In `ExecuteSmartSell()`: remove the `else` branch that opens a short when no long position exists
  - [x]`ExecuteSmartSell()` now only sells longs — if no long position, show "No position to sell" feedback
  - [x]In `ExecuteSmartBuy()`: remove cover logic — Buy only buys longs now
  - [x]Remove any D/F key bindings if they exist from old short system
  - [x]File: `Assets/Scripts/Runtime/Core/GameRunner.cs`

- [x] Task 6: Create SHORT button and Short P&L panel in UISetup (AC: 1, 16, 17, 18)
  - [x] Add `CreateShortButton()` to UISetup — visually distinct button (hot pink/purple theme)
  - [x]Button positioned separately from Buy/Sell trade panel (e.g., below or beside)
  - [x] Add Short P&L panel: entry price, current P&L (green profit / red loss), auto-close countdown
  - [x]P&L panel hidden by default, shown only during HOLDING and CASH_OUT_WINDOW states
  - [x]Store references for GameRunner to update: button Image, button Text, P&L texts, countdown text
  - [x]Wire button click to GameRunner (same as D key press)
  - [x]File: `Assets/Scripts/Setup/UISetup.cs`

- [x] Task 7: Implement SHORT button visual state updates (AC: 9-13, 18)
  - [x]In `UpdateShortStateMachine()`, update button appearance per state:
    - `RoundLockout`: greyed/dimmed, text shows countdown "5s", "4s", etc.
    - `Ready`: normal color (hot pink), text "SHORT"
    - `Holding`: greyed/dimmed, text shows countdown "8s", "7s", etc.
    - `CashOutWindow`: enabled (bright), text "CASH OUT", separate countdown display "10s" decrementing
    - `Cooldown`: greyed/dimmed, text shows countdown "10s", "9s", etc.
  - [x]Cash-out window countdown flashes/pulses when ≤ `ShortCashOutFlashThreshold` seconds remain
  - [x]All countdown text shows one decimal place (e.g., "4.2s"), updates every frame
  - [x]File: `Assets/Scripts/Runtime/Core/GameRunner.cs`

- [x] Task 8: Implement Short P&L panel real-time updates (AC: 8, 16, 17)
  - [x]During HOLDING and CASH_OUT_WINDOW: update P&L panel every frame
  - [x]Show: "Entry: $X.XX" | "P&L: +$X.XX" or "-$X.XX" (green/red)
  - [x]Show auto-close countdown in CASH_OUT_WINDOW state
  - [x]Hide panel when transitioning to COOLDOWN or READY
  - [x]File: `Assets/Scripts/Runtime/Core/GameRunner.cs`

- [x] Task 9: Update TradeFeedback for short messages (AC: 1)
  - [x] Add short-specific messages: "SHORTED 1 @ $X.XX", "CASHED OUT +$X.XX", "CASHED OUT -$X.XX", "AUTO-CLOSED +/-$X.XX"
  - [x]Use hot pink color for short feedback (matches existing `ShortPink` if available)
  - [x]File: `Assets/Scripts/Runtime/UI/TradeFeedback.cs`

- [x] Task 10: Update PositionOverlay for simultaneous long + short (AC: 5, 16)
  - [x]If both long and short exist, overlay should show both (or short is handled by the separate P&L panel)
  - [x]Decision: PositionOverlay shows long only; Short P&L panel (Task 6/8) handles short display
  - [x]File: `Assets/Scripts/Runtime/UI/PositionOverlay.cs`

- [x] Task 11: Ensure ShortSqueeze event affects active short (AC: 19)
  - [x]ShortSqueeze fires price spike on active stock — short P&L automatically worsens (price rises = loss)
  - [x]Verify P&L panel updates correctly during squeeze
  - [x]No special code needed if ShortSqueeze just affects price — confirm this with a test
  - [x]File: `Assets/Scripts/Runtime/Events/EventScheduler.cs` (verification only)

- [x] Task 12: QuantitySelector — short bypasses quantity selector (AC: 7)
  - [x]Short always uses `GameConfig.ShortBaseShares` (1), not the quantity selector preset
  - [x]Quantity selector preset buttons still work for Buy/Sell, unaffected
  - [x]File: `Assets/Scripts/Runtime/UI/QuantitySelector.cs` (minor — ensure short path doesn't read selector)

- [x] Task 13: Write comprehensive tests (AC: 1-20)
  - [x]Test: Short opens at current price with ShortBaseShares quantity
  - [x]Test: P&L correct — price drop = profit, price rise = loss
  - [x]Test: Can hold long AND short on same stock simultaneously
  - [x]Test: Only one short at a time — second short rejected
  - [x]Test: Sell button no longer opens shorts (Smart Sell removed)
  - [x]Test: Buy button no longer covers shorts (Smart Buy cover removed)
  - [x]Test: State machine transitions: RoundLockout → Ready → Holding → CashOutWindow → Cooldown → Ready
  - [x]Test: Cannot cash out during HOLDING state (forced hold)
  - [x]Test: Auto-close fires at end of CashOutWindow — same P&L as manual close
  - [x]Test: Round end auto-closes active short and resets all state
  - [x]Test: D key ignored during RoundLockout, Holding, Cooldown states
  - [x]Test: D key opens short in Ready state
  - [x]Test: D key cashes out in CashOutWindow state
  - [x]Test: LiquidateAllPositions closes both long and short
  - [x]Test: ShortSqueeze price spike correctly worsens short P&L
  - [x]File: `Assets/Tests/Runtime/Core/ShortLifecycleTests.cs`
  - [x]File: `Assets/Tests/Runtime/Trading/SimultaneousPositionTests.cs`

## Dev Notes

### Architecture Compliance
- **Setup-Oriented Generation:** SHORT button and P&L panel created by `UISetup` methods — programmatic uGUI Canvas hierarchy. No Inspector config.
- **EventBus Communication:** Existing `TradeExecutedEvent` and `TradeFeedbackEvent` reused. New events only if needed for UI decoupling.
- **State Machine in GameRunner:** Short state machine lives in GameRunner alongside existing trading logic. Separate `UpdateShortStateMachine()` method keeps it contained.
- **No Inspector Config:** All timing constants in `GameConfig`. All UI built in code.
- **Performance:** State machine is a simple switch + timer decrement per frame. No allocation, no performance concern.

### Key Design Decisions
- **Share-based, not cash-based:** Aligned with new $10 economy (FIX-14) and single-share start (FIX-13). Player shorts 1 share, not a dollar amount. P&L is simple: `(entry - exit) * shares`.
- **Timed lifecycle:** The 5s/8s/10s/10s timing creates rhythm and tension. Player must commit — no instant in-and-out. The forced hold prevents spam-shorting, and the auto-close prevents indefinite risk exposure.
- **D key dual purpose:** D opens short when READY, D cashes out when in CASH_OUT_WINDOW. Single key, context-sensitive. Simpler than D/F split.
- **Simultaneous long + short:** Completely independent positions. Buying doesn't affect your short, selling doesn't affect your short. This enables hedging strategies once items unlock more shares.
- **Item integration via GameConfig:** All 5 timing constants and share count are in GameConfig. Items can modify these at runtime (e.g., reduce `ShortForcedHoldDuration` by 3s). No refactoring needed to add item effects later.
- **Smart Sell/Buy removal:** `ExecuteSmartSell()` becomes a pure sell-long method. `ExecuteSmartBuy()` becomes a pure buy-long method. Significantly simplifies both methods.
- **Auto-close = no penalty:** Whether player clicks CASH OUT or lets timer expire, same market price used. This is intentional — the auto-close is a safety net, not a punishment.

### Dependencies
- **FIX-10 (trade execution delay):** Already done. Post-trade cooldown applies to Buy/Sell. Short has its own separate cooldown system (the state machine timers), so no conflict.
- **FIX-13 (single share start):** Short uses `ShortBaseShares = 1` from GameConfig. When FIX-13 makes quantity a progression mechanic, short share count becomes a separate upgrade path.
- **FIX-14 (economy rebalance):** Short P&L at $10 economy is small (e.g., $0.20 on a $2 stock). This is intentional — shorting is a side mechanic. Items can amplify later.
- No new packages or dependencies required.

### Edge Cases
- **Round ends during HOLDING:** Auto-close at market price. Player gets whatever P&L exists. No forced-hold penalty.
- **Round ends during CASH_OUT_WINDOW:** Same — auto-close at market price.
- **Round ends during COOLDOWN:** No short to close. Timer cancels. Clean reset.
- **ShortSqueeze during HOLDING:** Price spikes up, short P&L goes deeply negative. Player is locked in (forced hold) and must watch. This is the dramatic tension point.
- **ShortSqueeze during CASH_OUT_WINDOW:** Player can immediately cash out to cut losses, or ride it hoping for reversal.
- **Player has $0 cash:** Can still short (shorting borrows a share, doesn't cost cash upfront). P&L settles on close.
- **Stock price goes to $0:** Short profits maximally — `(entry - 0) * shares = entry * shares`.

### Previous Story Learnings
- From FIX-6: Trade panel architecture (UISetup.ExecuteTradePanel) provides a good template for the SHORT button layout.
- From FIX-7: PositionOverlay already distinguishes LONG/SHORT — short P&L panel is a new separate element.
- From FIX-9: ShortSqueeze already targets activeStocks[0] — no special targeting needed.
- From FIX-10: Post-trade cooldown pattern (timer + button dim + countdown text) is directly reusable for short state machine visual feedback.
- Smart Sell/Buy auto-detection in GameRunner is already complex (~200+ lines) — removing it simplifies significantly.

## Dev Agent Record

### Implementation Plan
Tasks executed in dependency order: GameConfig constants (T1) → Portfolio refactor (T4) → TradeExecutor updates → GameRunner state machine (T2,3,5,7,8) → UISetup SHORT button (T6) → TradeFeedback messages (T9) → PositionOverlay (T10, no change needed) → EventScheduler verification (T11, no change needed) → QuantitySelector bypass (T12, already handled) → Comprehensive tests (T13) → Fix all broken existing tests.

### Completion Notes
All 13 tasks implemented. Key architectural decisions:
- Portfolio split into `_positions` (longs) and `_shortPositions` (shorts) dictionaries for simultaneous long+short support
- Smart Sell/Buy removed entirely — Buy/Sell are pure long-only, Short has dedicated D key + button
- Short state machine in GameRunner: RoundLockout(5s) → Ready → Holding(8s) → CashOutWindow(10s) → Cooldown(10s) → Ready
- All timing constants sourced from GameConfig for future item integration
- SHORT button (hot pink) with separate P&L panel created by UISetup.ExecuteShortButton
- Existing tests updated across PortfolioTests, TradeExecutorTests, QuantitySelectorTests, TradeFeedbackTests
- New tests: SimultaneousPositionTests (21 tests), ShortLifecycleTests (20 tests)

### Debug Log
- Updated 4 existing test files to use `GetShortPosition()` instead of `GetPosition()` for short position lookups
- Changed collision tests from "rejected" to "allows simultaneous" behavior
- Updated TradeFeedback rejection messages for FIX-11 (no more "Can't short — long position open")

### Code Review Fixes (2026-02-14)
- **CRITICAL**: Fixed SHORT button never appearing — removed unnecessary panelParent wrapper in UISetup.ExecuteShortButton so canvasGo is the root, fixing GameRunner's parent-traversal toggle
- **CRITICAL**: Deleted AI-created .meta files (SimultaneousPositionTests.cs.meta, ShortLifecycleTests.cs.meta) — Unity auto-generates these
- **HIGH**: Fixed TradeExecutionDelayTests.AutoLiquidation_WorksWithShortPositions — changed `PositionCount` to `ShortPositionCount` after Portfolio dictionary split
- **HIGH**: Added countdown to CASH OUT button text per AC 18 — button now shows "CASH OUT (Xs)"
- **MEDIUM**: Fixed countdown text format consistency — all states now use one-decimal "F1" format per Task 7
- **MEDIUM**: Clarified MultipleShorts_DifferentStocks_Allowed test name and comment re: AC 6 game rule vs Portfolio API
- **MEDIUM**: Added TradeExecutionDelayTests.cs to File List (was modified but undocumented)

## File List

### Modified
- `Assets/Scripts/Setup/Data/GameConfig.cs` — 6 short lifecycle constants
- `Assets/Scripts/Runtime/Trading/Portfolio.cs` — Separate `_shortPositions` dictionary, `GetShortPosition()`, `HasShortPosition()`, `GetAllShortPositions()`, `ShortPositionCount`
- `Assets/Scripts/Runtime/Trading/TradeExecutor.cs` — `ExecuteCover` uses `GetShortPosition()`, `ExecuteSell` simplified
- `Assets/Scripts/Runtime/Core/GameRunner.cs` — ShortState enum, state machine, `HandleShortInput()`, pure `ExecuteBuy()`/`ExecuteSell()`, removed SmartSell/SmartBuy
- `Assets/Scripts/Setup/UISetup.cs` — `ExecuteShortButton()` with SHORT button + P&L panel
- `Assets/Scripts/Runtime/UI/QuantitySelector.cs` — `CalculateMaxSell/Cover` use correct position dictionary
- `Assets/Scripts/Runtime/UI/TradeFeedback.cs` — Updated rejection reasons for FIX-11 simultaneous behavior
- `Assets/Tests/Runtime/Trading/PortfolioTests.cs` — Updated collision tests, CoverShort tests, GetPositionPnL test
- `Assets/Tests/Runtime/Trading/TradeExecutorTests.cs` — Updated collision tests, short/cover position lookups
- `Assets/Tests/Runtime/UI/QuantitySelectorTests.cs` — Updated smart routing tests for separate dictionaries
- `Assets/Tests/Runtime/UI/TradeFeedbackTests.cs` — Updated rejection message tests
- `Assets/Tests/Runtime/Core/TradeExecutionDelayTests.cs` — Fixed ShortPositionCount assertion after Portfolio refactor

### Added
- `Assets/Tests/Runtime/Trading/SimultaneousPositionTests.cs` — 21 tests for simultaneous long+short positions
- `Assets/Tests/Runtime/Core/ShortLifecycleTests.cs` — 20 tests for short lifecycle, P&L, config constants

### Unchanged (verified)
- `Assets/Scripts/Runtime/Trading/Position.cs` — No changes needed
- `Assets/Scripts/Runtime/UI/PositionOverlay.cs` — Only shows longs; short has separate P&L panel
- `Assets/Scripts/Runtime/Events/EventScheduler.cs` — ShortSqueeze already targets via price effect

## Change Log

- 2026-02-14: Story fully rewritten — replaced "bet a dollar amount" design with timed state machine, 1-share shorting, aligned with FIX-13/FIX-14 economy changes
- 2026-02-14: Implementation complete — all 13 tasks done, all existing tests updated, 2 new test files created
- 2026-02-14: Code review — 7 issues fixed (2 critical, 2 high, 3 medium), 2 low items noted
