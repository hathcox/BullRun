# Story FIX-6: Trading Panel Overhaul — Buy/Sell Buttons with Quantity Presets

Status: pending

## Story

As a player,
I want large BUY and SELL buttons with quantity presets (x5, x10, x15, x25),
so that trading feels fast and intuitive with clear visual actions.

## Problem Analysis

Currently trading is keyboard-driven: B=Buy, S=Sell, D=Short, F=Cover with a separate quantity selector (1x/5x/10x/MAX). This requires players to remember 5+ keybindings and understand the distinction between sell/short and buy/cover. The new design simplifies to 2 smart buttons:

- **BUY** button: Opens a long position, or covers a short position if currently short
- **SELL** button: Closes a long position, or opens a short position if no position held

This is a UX simplification — the backend already supports all 4 operations, the UI just needs to route intelligently.

**Affected Code:**
- `Scripts/Runtime/UI/QuantitySelector.cs` — presets change from [1,5,10,MAX] to [5,10,15,25]
- `Scripts/Setup/UISetup.cs` — remove old quantity selector, create new trade panel
- `Scripts/Runtime/Core/GameRunner.cs` — replace 4-key input with 2-button/2-key trade routing
- Remove: key legend (replaced by visible buttons)

## Acceptance Criteria

1. A trade panel is visible during trading with: quantity presets on top, SELL button on left, BUY button on right
2. Quantity presets are: x5, x10, x15, x25 (buttons, clickable + keyboard shortcut)
3. BUY button: if no position or long position → executes buy. If short position → executes cover
4. SELL button: if long position → executes sell. If no position or short position → executes short
5. SELL with no position opens a short (implicit shorting)
6. Buttons are large and clearly labeled (BUY green, SELL red)
7. Keyboard shortcuts still work: B=Buy action, S=Sell action (same routing as buttons)
8. Trade feedback still shows what happened: "BOUGHT ACME x10", "SHORTED ACME x5", etc.
9. Quantity panel defaults to x10 on round start
10. Panel is positioned at the bottom-center of the screen (replacing old quantity selector + key legend area)

## Tasks / Subtasks

- [ ] Task 1: Update QuantitySelector presets (AC: 2, 9)
  - [ ] Change `PresetValues` from [1, 5, 10, 0] to [5, 10, 15, 25]
  - [ ] Change `PresetLabels` from ["1x", "5x", "10x", "MAX"] to ["x5", "x10", "x15", "x25"]
  - [ ] Change `Preset` enum from { One, Five, Ten, Max } to { Five, Ten, Fifteen, TwentyFive }
  - [ ] Remove MAX calculation logic (no longer a preset)
  - [ ] Default preset: x10 (index 1)
  - [ ] Remove Q key cycling (replaced by direct preset button clicks / number keys)
  - [ ] Update `GameConfig.DefaultTradeQuantity` from 10 to 10 (no change, but verify)
  - [ ] File: `Scripts/Runtime/UI/QuantitySelector.cs`

- [ ] Task 2: Create new trade panel UI (AC: 1, 6, 10)
  - [ ] Replace `ExecuteQuantitySelector()` and `ExecuteKeyLegend()` with a new `ExecuteTradePanel()` method
  - [ ] Layout: horizontal panel at bottom-center of screen
    ```
    ┌───────────────────────────────────────────────┐
    │  [x5] [x10] [x15] [x25]                      │
    │  ┌──────────┐            ┌──────────┐         │
    │  │   SELL   │            │   BUY    │         │
    │  │  (red)   │            │  (green) │         │
    │  └──────────┘            └──────────┘         │
    └───────────────────────────────────────────────┘
    ```
  - [ ] SELL button: large, red background (LossRed), left side
  - [ ] BUY button: large, green background (ProfitGreen), right side
  - [ ] Quantity presets: row of 4 buttons above the buy/sell buttons
  - [ ] Canvas sorting order: 24 (same as old quantity selector)
  - [ ] File: `Scripts/Setup/UISetup.cs`

- [ ] Task 3: Implement smart trade routing in GameRunner (AC: 3, 4, 5, 7, 8)
  - [ ] Create a unified `ExecuteSmartBuy()` method:
    - Check if player has a SHORT position for the stock → execute COVER
    - Otherwise → execute BUY (open/add to long position)
    - Feedback: "COVERED ACME x10" or "BOUGHT ACME x10"
  - [ ] Create a unified `ExecuteSmartSell()` method:
    - Check if player has a LONG position for the stock → execute SELL
    - Otherwise → execute SHORT (open short position)
    - Feedback: "SOLD ACME x10" or "SHORTED ACME x5"
  - [ ] Wire B key to `ExecuteSmartBuy()`, S key to `ExecuteSmartSell()`
  - [ ] Wire BUY/SELL button onClick to same methods
  - [ ] Remove D key (short) and F key (cover) — no longer needed
  - [ ] Remove Q key cycling — presets selected by click or keyboard shortcut
  - [ ] File: `Scripts/Runtime/Core/GameRunner.cs`

- [ ] Task 4: Wire button clicks to GameRunner (AC: 1, 3, 4)
  - [ ] The BUY and SELL buttons need onClick handlers that call into GameRunner
  - [ ] Options: (a) static event via EventBus, (b) direct reference, (c) public method on a new TradePanelController MonoBehaviour
  - [ ] Recommended: Publish `TradeButtonPressedEvent { IsBuy: bool }` on click, GameRunner subscribes
  - [ ] Files: `Scripts/Setup/UISetup.cs`, `Scripts/Runtime/Core/GameRunner.cs`, new event type

- [ ] Task 5: Update trade feedback messages (AC: 8)
  - [ ] Smart buy that covers short: "COVERED {ticker} x{qty}"
  - [ ] Smart buy that opens long: "BOUGHT {ticker} x{qty}"
  - [ ] Smart sell that closes long: "SOLD {ticker} x{qty}"
  - [ ] Smart sell that opens short: "SHORTED {ticker} x{qty}"
  - [ ] Rejection messages: "Insufficient cash", "No position to sell" etc.
  - [ ] File: `Scripts/Runtime/Core/GameRunner.cs`

- [ ] Task 6: Clean up removed UI (AC: 10)
  - [ ] Remove `ExecuteKeyLegend()` call from `GameRunner.Start()`
  - [ ] Replace `ExecuteQuantitySelector()` with new `ExecuteTradePanel()`
  - [ ] Remove old quantity selector positioning/layout code
  - [ ] Files: `Scripts/Runtime/Core/GameRunner.cs`, `Scripts/Setup/UISetup.cs`

- [ ] Task 7: Update tests (AC: all)
  - [ ] Update QuantitySelector tests: new presets [5,10,15,25], no MAX
  - [ ] Add smart routing tests: buy when flat→long, buy when short→cover, sell when long→sell, sell when flat→short
  - [ ] Files: `Tests/Runtime/UI/QuantitySelectorTests.cs`

## Dev Notes

### Architecture Compliance
- **Setup-Oriented Generation:** New `ExecuteTradePanel()` follows same pattern as other `Execute*()` methods in UISetup
- **uGUI Canvas:** All buttons/panels built programmatically with Image/Button/Text components
- **EventBus:** Button presses published as events for GameRunner to consume (decoupled)
- **No Inspector Config:** All sizing, colors, positioning in code

### Smart Trade Routing Logic
```
ExecuteSmartBuy(stockId, qty, price, portfolio):
  position = portfolio.GetPosition(stockId)
  if position != null AND position.IsShort:
    → ExecuteCover(stockId, min(qty, position.Shares), price, portfolio)
  else:
    → ExecuteBuy(stockId, qty, price, portfolio)

ExecuteSmartSell(stockId, qty, price, portfolio):
  position = portfolio.GetPosition(stockId)
  if position != null AND position.IsLong:
    → ExecuteSell(stockId, min(qty, position.Shares), price, portfolio)
  else:
    → ExecuteShort(stockId, qty, price, portfolio)
```

### Quantity Handling Without MAX
- With presets [5, 10, 15, 25], there's no MAX calculation needed
- If the player can't afford the full quantity, trades should still partial-fill (buy as many as affordable)
- For sells: if player holds fewer shares than selected quantity, sell all held shares
- For shorts: if player can't afford full margin, short as many as affordable

### Dependencies
- **FIX-5** (Single Stock) should be done first — simplifies the stock targeting logic
- Without FIX-5, the panel still needs to know which stock is selected (from sidebar)

### UI Positioning
- Old layout: KeyLegend at bottom-left, QuantitySelector at bottom-center, RoundTimer at bottom-center
- New layout: TradePanel at bottom-center (wider, with buy/sell buttons + quantity presets), RoundTimer stays above

## References
- `Scripts/Runtime/UI/QuantitySelector.cs` — current quantity selector (to be modified)
- `Scripts/Setup/UISetup.cs:1232-1264` — ExecuteKeyLegend (to be removed)
- `Scripts/Setup/UISetup.cs:1270-1348` — ExecuteQuantitySelector (to be replaced)
- `Scripts/Runtime/Core/GameRunner.cs:139-268` — HandleTradingInput (to be rewritten)
- `Scripts/Runtime/Trading/TradeExecutor.cs` — all 4 trade methods (unchanged, just called differently)
