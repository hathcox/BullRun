# Story 14.4: Action Buttons (Center Core)

Status: ready-for-dev

## Story

As a player,
I want the SELL, BUY, and SHORT buttons displayed in the center column of the Control Deck,
so that my trading actions are consolidated with the rest of my dashboard.

## Acceptance Criteria

1. **Center Core** populated by `UISetup.ExecuteControlDeck()` with:
   - Top row: HorizontalLayoutGroup with SELL (left, `CRTThemeData.Danger` red) and BUY (right, `CRTThemeData.ButtonBuy` green)
   - Bottom row: SHORT button full-width (`CRTThemeData.ButtonShort` amber)
   - Button text: white, bold, 20pt (SELL/BUY), 16pt (SHORT)
2. Cooldown overlay repositioned to cover Center Core area
3. Short P&L panel appears inline below SHORT button when active
4. Short 2 container (Dual Short expansion) positioned beside first short when active
5. Leverage badge positioned above Center Core when expansion active
6. All button click wiring preserved:
   - BUY/SELL → `TradeButtonPressedEvent` via EventBus
   - SHORT → `GameRunner.HandleShortInput()`
   - SHORT 2 → `GameRunner.HandleShort2Input()`
7. `QuantitySelector` references updated to new button Images/Texts from DashboardReferences
8. Old `ExecuteTradePanel()` method removed from UISetup
9. Keyboard shortcuts (B, S, D) still work for trading

## Tasks / Subtasks

- [ ] Task 1: Populate Center Core in `UISetup.ExecuteControlDeck()` (AC: 1)
  - [ ] 1.1: Create top row HorizontalLayoutGroup for SELL + BUY buttons
  - [ ] 1.2: Create SELL button (CRTThemeData.Danger/ButtonSell, white bold 20pt text, LayoutElement)
  - [ ] 1.3: Create BUY button (CRTThemeData.ButtonBuy, white bold 20pt text, LayoutElement)
  - [ ] 1.4: Create bottom row with SHORT button full-width (CRTThemeData.ButtonShort amber, 16pt)
  - [ ] 1.5: Wire BUY button onClick → `EventBus.Publish(new TradeButtonPressedEvent { IsBuy = true })`
  - [ ] 1.6: Wire SELL button onClick → `EventBus.Publish(new TradeButtonPressedEvent { IsBuy = false })`
  - [ ] 1.7: Wire SHORT button onClick → `GameRunner.HandleShortInput()` via FindObjectOfType
- [ ] Task 2: Create Cooldown Overlay for Center Core (AC: 2)
  - [ ] 2.1: Create cooldown overlay panel parented to ControlDeckCanvas (not Center Core — avoids layout interference)
  - [ ] 2.2: Position overlay to exactly cover Center Core area
  - [ ] 2.3: Add cooldown timer text (amber, bold, 22pt)
  - [ ] 2.4: Start hidden (SetActive false)
- [ ] Task 3: Create Short P&L panel inline (AC: 3)
  - [ ] 3.1: Create ShortPnlPanel below SHORT button with VerticalLayoutGroup
  - [ ] 3.2: Add Entry text ("Entry: $X.XX"), P&L value text (bold), countdown text (amber)
  - [ ] 3.3: Start hidden (SetActive false, shown when short is active)
  - [ ] 3.4: Use ContentSizeFitter for auto-expansion when P&L panel is shown
- [ ] Task 4: Create Short 2 container for Dual Short expansion (AC: 4)
  - [ ] 4.1: Create Short2Container positioned beside first short section
  - [ ] 4.2: Add SHORT 2 button, P&L panel with same structure as slot 1
  - [ ] 4.3: Wire SHORT 2 onClick → `GameRunner.HandleShort2Input()`
  - [ ] 4.4: Start hidden (SetActive false, shown when Dual Short expansion owned)
- [ ] Task 5: Create Leverage Badge (AC: 5)
  - [ ] 5.1: Create LeverageBadge panel positioned above Center Core
  - [ ] 5.2: Add "2x LEVERAGE" text (orange background, white bold 14pt)
  - [ ] 5.3: Start hidden (SetActive false, shown when Leverage Trading expansion owned)
- [ ] Task 6: Update DashboardReferences with button fields (AC: 7)
  - [ ] 6.1: Add button-related fields: BuyButton (Button), SellButton (Button), ShortButton (Button)
  - [ ] 6.2: Add Short UI fields: ShortButtonImage, ShortButtonText, ShortPnlPanel, ShortPnlEntryText, ShortPnlValueText, ShortPnlCountdownText
  - [ ] 6.3: Add Short 2 UI fields: Short2ButtonImage, Short2ButtonText, Short2PnlPanel, Short2PnlEntryText, Short2PnlValueText, Short2PnlCountdownText, Short2Container
  - [ ] 6.4: Add CooldownOverlay, CooldownTimerText, LeverageBadge fields
- [ ] Task 7: Update QuantitySelector references (AC: 7)
  - [ ] 7.1: Wire QuantitySelector.CooldownOverlay and CooldownTimerText from DashboardReferences
  - [ ] 7.2: Wire all ShortButton/ShortPnl/Short2 references from DashboardReferences
  - [ ] 7.3: Wire LeverageBadge from DashboardReferences
- [ ] Task 8: Update GameRunner to use DashboardReferences (AC: 7, 9)
  - [ ] 8.1: Replace `_quantitySelector = UISetup.ExecuteTradePanel()` with refs from DashboardReferences
  - [ ] 8.2: Wire `_shortButtonImage`, `_shortButtonText`, etc. from DashboardReferences instead of QuantitySelector
  - [ ] 8.3: Verify HandleShortInput/HandleShort2Input still triggered correctly
  - [ ] 8.4: Verify show/hide of trade panel tied to TradingState.IsActive still works
- [ ] Task 9: Remove old ExecuteTradePanel (AC: 8)
  - [ ] 9.1: Delete `UISetup.ExecuteTradePanel()` method (lines ~1299-1563)
  - [ ] 9.2: Remove `_quantitySelector = UISetup.ExecuteTradePanel()` from GameRunner.Start()
  - [ ] 9.3: Verify no other code references ExecuteTradePanel
- [ ] Task 10: Verify keyboard shortcuts (AC: 9)
  - [ ] 10.1: Confirm B key → Buy, S key → Sell, D key → Short still work
  - [ ] 10.2: Verify `HandleTradingInput()` in GameRunner is unaffected

## Dev Notes

### Architecture Compliance

- **Button wiring:** BUY/SELL use `EventBus.Publish(TradeButtonPressedEvent)` — same pattern as current ExecuteTradePanel. SHORT uses `FindObjectOfType<GameRunner>()` — same as current. No architectural change to event flow.
- **QuantitySelector migration:** QuantitySelector MonoBehaviour still exists as the trade quantity logic holder. Its UI reference properties are now populated from DashboardReferences instead of being set directly in ExecuteTradePanel.
- **Short state machine:** GameRunner's short state machine (UpdateShortStateMachine, OpenShortPosition, CloseShortPosition) reads from `_shortButtonImage`, `_shortPnlPanel`, etc. These must be rewired from DashboardReferences.

### Center Core Layout Detail

```
Center_Core (VerticalLayoutGroup, spacing=8, padding=8)
├── ButtonRow (HorizontalLayoutGroup, spacing=20)
│   ├── SELL button (CRTThemeData.Danger, 160px wide, 48px tall)
│   │   └── "SELL" text (white, bold, 20pt)
│   └── BUY button (CRTThemeData.ButtonBuy, 160px wide, 48px tall)
│       └── "BUY" text (white, bold, 20pt)
├── SHORT button (CRTThemeData.ButtonShort amber, full-width, 32px tall)
│   └── "SHORT" text (white, bold, 16pt)
└── ShortPnlPanel (hidden, shown when short active)
    ├── Entry text (12pt)
    ├── P&L value (14pt bold)
    └── Countdown text (12pt amber)
```

### CRT Theme Button Colors (from CRTThemeData)

- **BUY:** `CRTThemeData.ButtonBuy` = #28f58d (phosphor green) — matches TextHigh
- **SELL:** `CRTThemeData.ButtonSell` / `CRTThemeData.Danger` = #ff4444 (CRT red)
- **SHORT:** `CRTThemeData.ButtonShort` = #ffb800 (amber) — replaces current hot pink (#FF2099)

This is a deliberate color change from the current hot pink short button to amber, fitting the CRT theme.

### Current ExecuteTradePanel Code (to be replaced)

The existing `ExecuteTradePanel()` (UISetup.cs:1299-1563) creates:
- TradePanelCanvas (sortingOrder 24)
- TradePanelContainer (bottom-center, 420x60)
- BUY/SELL buttons in HorizontalLayoutGroup
- CooldownOverlay
- ShortContainer with SHORT button + P&L panel
- Short2Container with SHORT 2 button + P&L panel
- LeverageBadge
- QuantitySelector wiring

All this functionality moves into `ExecuteControlDeck()`'s Center Core section. The canvas sorting order changes from 24 (separate canvas) to part of the ControlDeckCanvas (20).

### GameRunner Short UI Wiring (GameRunner.cs:119-137)

Current code extracts refs from QuantitySelector:
```csharp
_shortButtonImage = _quantitySelector.ShortButtonImage;
_shortButtonText = _quantitySelector.ShortButtonText;
// ... etc
```

After this story, these should come from DashboardReferences (or continue through QuantitySelector which is populated from DashboardReferences — either pattern works).

### Cooldown Overlay Positioning

The cooldown overlay must cover the Center Core buttons. Since Center Core is inside a HorizontalLayoutGroup, the overlay should be parented to the ControlDeckCanvas (not the Center Core VerticalLayoutGroup) and positioned to match Center Core's screen position. Use the same pattern as current (overlayRect matches container position/size).

### Testing Approach

- Visual testing: BUY (green), SELL (red), SHORT (amber) buttons visible in center column.
- Click testing: Click BUY → trade executes. Click SELL → trade executes. Click SHORT → short state machine activates.
- Keyboard: B → Buy, S → Sell, D → Short.
- Cooldown: After trade, grey overlay covers buttons with countdown timer.
- Short P&L: When short is active, P&L panel expands below SHORT button.
- Expansions: With Dual Short owned, SHORT 2 appears. With Leverage Trading owned, "2x LEVERAGE" badge appears.

### References

- [Source: _bmad-output/planning-artifacts/epic-14-terminal-1999-ui.md#Story 14.4]
- [Source: Assets/Scripts/Setup/UISetup.cs:1299-1563] — current ExecuteTradePanel() to remove
- [Source: Assets/Scripts/Runtime/UI/QuantitySelector.cs] — all Short/Cooldown/Leverage property refs
- [Source: Assets/Scripts/Runtime/Core/GameRunner.cs:96-155] — Start() wiring to update
- [Source: Assets/Scripts/Runtime/Core/GameRunner.cs:119-137] — short UI ref extraction to rewire
- [Source: Assets/Scripts/Runtime/Core/GameRunner.cs:200-231] — Update() trade panel show/hide + short ticking
- [Source: Assets/Scripts/Setup/Data/CRTThemeData.cs] — ButtonBuy, ButtonSell, ButtonShort, Danger colors

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
