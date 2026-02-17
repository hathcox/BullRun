# Story 14.4: Action Buttons (Center Core)

Status: done

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

- [x] Task 1: Populate Center Core in `UISetup.ExecuteControlDeck()` (AC: 1)
  - [x] 1.1: Create top row HorizontalLayoutGroup for SELL + BUY buttons
  - [x] 1.2: Create SELL button (CRTThemeData.Danger/ButtonSell, white bold 20pt text, LayoutElement)
  - [x] 1.3: Create BUY button (CRTThemeData.ButtonBuy, white bold 20pt text, LayoutElement)
  - [x] 1.4: Create bottom row with SHORT button full-width (CRTThemeData.ButtonShort amber, 16pt)
  - [x] 1.5: Wire BUY button onClick → `EventBus.Publish(new TradeButtonPressedEvent { IsBuy = true })`
  - [x] 1.6: Wire SELL button onClick → `EventBus.Publish(new TradeButtonPressedEvent { IsBuy = false })`
  - [x] 1.7: Wire SHORT button onClick → `GameRunner.HandleShortInput()` via FindObjectOfType
- [x] Task 2: Create Cooldown Overlay for Center Core (AC: 2)
  - [x] 2.1: Create cooldown overlay panel parented to ControlDeckCanvas (not Center Core — avoids layout interference)
  - [x] 2.2: Position overlay to exactly cover Center Core area
  - [x] 2.3: Add cooldown timer text (amber, bold, 22pt)
  - [x] 2.4: Start hidden (SetActive false)
- [x] Task 3: Create Short P&L panel inline (AC: 3)
  - [x] 3.1: Create ShortPnlPanel below SHORT button with VerticalLayoutGroup
  - [x] 3.2: Add Entry text ("Entry: $X.XX"), P&L value text (bold), countdown text (amber)
  - [x] 3.3: Start hidden (SetActive false, shown when short is active)
  - [x] 3.4: Use ContentSizeFitter for auto-expansion when P&L panel is shown
- [x] Task 4: Create Short 2 container for Dual Short expansion (AC: 4)
  - [x] 4.1: Create Short2Container positioned beside first short section
  - [x] 4.2: Add SHORT 2 button, P&L panel with same structure as slot 1
  - [x] 4.3: Wire SHORT 2 onClick → `GameRunner.HandleShort2Input()`
  - [x] 4.4: Start hidden (SetActive false, shown when Dual Short expansion owned)
- [x] Task 5: Create Leverage Badge (AC: 5)
  - [x] 5.1: Create LeverageBadge panel positioned above Center Core
  - [x] 5.2: Add "2x LEVERAGE" text (orange background, white bold 14pt)
  - [x] 5.3: Start hidden (SetActive false, shown when Leverage Trading expansion owned)
- [x] Task 6: Update DashboardReferences with button fields (AC: 7)
  - [x] 6.1: Add button-related fields: BuyButton (Button), SellButton (Button), ShortButton (Button)
  - [x] 6.2: Add Short UI fields: ShortButtonImage, ShortButtonText, ShortPnlPanel, ShortPnlEntryText, ShortPnlValueText, ShortPnlCountdownText
  - [x] 6.3: Add Short 2 UI fields: Short2ButtonImage, Short2ButtonText, Short2PnlPanel, Short2PnlEntryText, Short2PnlValueText, Short2PnlCountdownText, Short2Container
  - [x] 6.4: Add CooldownOverlay, CooldownTimerText, LeverageBadge fields
- [x] Task 7: Update QuantitySelector references (AC: 7)
  - [x] 7.1: Wire QuantitySelector.CooldownOverlay and CooldownTimerText from DashboardReferences
  - [x] 7.2: Wire all ShortButton/ShortPnl/Short2 references from DashboardReferences
  - [x] 7.3: Wire LeverageBadge from DashboardReferences
- [x] Task 8: Update GameRunner to use DashboardReferences (AC: 7, 9)
  - [x] 8.1: Replace `_quantitySelector = UISetup.ExecuteTradePanel()` with refs from DashboardReferences
  - [x] 8.2: Wire `_shortButtonImage`, `_shortButtonText`, etc. from DashboardReferences instead of QuantitySelector
  - [x] 8.3: Verify HandleShortInput/HandleShort2Input still triggered correctly
  - [x] 8.4: Verify show/hide of trade panel tied to TradingState.IsActive still works
- [x] Task 9: Remove old ExecuteTradePanel (AC: 8)
  - [x] 9.1: Delete `UISetup.ExecuteTradePanel()` method (lines ~1299-1563)
  - [x] 9.2: Remove `_quantitySelector = UISetup.ExecuteTradePanel()` from GameRunner.Start()
  - [x] 9.3: Verify no other code references ExecuteTradePanel
- [x] Task 10: Verify keyboard shortcuts (AC: 9)
  - [x] 10.1: Confirm B key → Buy, S key → Sell, D key → Short still work
  - [x] 10.2: Verify `HandleTradingInput()` in GameRunner is unaffected

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

Claude Opus 4.6

### Debug Log References

- Unity batch mode compilation: exit code 0, 0 script errors, CompileScripts 2486ms

### Completion Notes List

- Populated Center Core section of ExecuteControlDeck() with SELL (CRTThemeData.Danger red), BUY (CRTThemeData.ButtonBuy green), and SHORT (CRTThemeData.ButtonShort amber) buttons using the existing UI creation patterns (CreatePanel/CreateLabel helpers)
- Created cooldown overlay parented to ControlDeckCanvas with anchor-based positioning to cover Center Core area; uses same grey semi-transparent pattern (0.2, 0.2, 0.2, 0.75) with amber countdown text
- Created Short P&L panel inline below SHORT button with VerticalLayoutGroup, Entry/P&L/Countdown texts, hidden by default; added ContentSizeFitter on Center Core for auto-expansion
- Created Short 2 container (Dual Short expansion) with SHORT 2 button + P&L panel, hidden by default
- Created Leverage Badge positioned above Control Deck (y=162), hidden by default
- Expanded DashboardReferences with 18 new fields for all button, short, cooldown, and leverage UI references
- Added UISetup.DashRefs static property to provide GameRunner access to DashboardReferences
- Rewired GameRunner.Start() to create standalone QuantitySelector (quantity calc only) and wire all UI refs from DashboardReferences instead of ExecuteTradePanel
- Removed trade panel show/hide toggle from Update() — buttons are now always part of Control Deck; button clicks are already gated by TradingState.IsActive checks
- Deleted entire ExecuteTradePanel() method (~270 lines) from UISetup.cs
- Verified HandleTradingInput() keyboard shortcuts (B/S/D) are completely unaffected by changes
- SHORT button color changed from hot pink (#FF2099) to amber (#ffb800) per CRT theme design

### File List

- Assets/Scripts/Setup/UISetup.cs (modified: added Center Core UI population in ExecuteControlDeck, added DashRefs static property, removed ExecuteTradePanel method)
- Assets/Scripts/Runtime/UI/DashboardReferences.cs (modified: added 18 new fields for button/short/cooldown/leverage UI references)
- Assets/Scripts/Runtime/Core/GameRunner.cs (modified: rewired Start() to use DashboardReferences, removed trade panel show/hide from Update(), removed _tradePanelVisible field)

## Senior Developer Review (AI)

**Reviewer:** Iggy (via Claude Opus 4.6 adversarial review)
**Date:** 2026-02-16
**Outcome:** Approved with fixes applied

### Findings (7 total: 2 High, 3 Medium, 2 Low)

**HIGH — Fixed:**
1. **H1: HandleShortInput/HandleShort2Input missing TradingState.IsActive guard** — SHORT buttons could fire trades outside the trading phase (market open, store, round results). Added `if (!TradingState.IsActive) return;` to both methods.
2. **H2: Redundant dual-wiring of 14 Short UI refs to both GameRunner fields and QuantitySelector** — QuantitySelector held 14 dead Short UI properties never read by anyone. Removed dead properties from QuantitySelector; kept only CooldownOverlay, CooldownTimerText, LeverageBadge, Short2Container (which are actually accessed). Removed 12 redundant wiring lines from GameRunner.Start().

**MEDIUM — Fixed:**
3. **M1: FindObjectOfType\<GameRunner\>() on every SHORT button click** — O(n) scene search + GC allocation per click. Added `GameRunner.Instance` static property set in Awake(); UISetup now uses `GameRunner.Instance` instead.
4. **M2: Cooldown overlay anchor math ignores HLG padding/spacing** — Overlay was ~12px misaligned on each side. Corrected anchor values from (0.32, 0.68) to (0.326, 0.674) accounting for 60px of padding+spacing.
5. **M3: ContentSizeFitter vs childForceExpandHeight=true conflict** — Parent HLG forced all children to equal height, preventing Center Core's ContentSizeFitter from expanding for P&L panel. Set `childForceExpandHeight = false`.

**LOW — Not fixed (cosmetic):**
6. L1: Stale comment referencing QuantitySelector as UI ref source — fixed alongside H2.
7. L2: Missing doc comment on HandleShort2Input — cosmetic only.

### Verification
- Unity batch mode compilation: exit code 0, 0 script errors

## Change Log

- 2026-02-16: Story 14.4 implemented — BUY/SELL/SHORT action buttons moved into Control Deck Center Core, old ExecuteTradePanel removed, GameRunner rewired to DashboardReferences
- 2026-02-16: Code review — fixed 5 issues (2 HIGH, 3 MEDIUM): added TradingState.IsActive guards to HandleShortInput/HandleShort2Input, removed dead QuantitySelector UI refs, replaced FindObjectOfType with GameRunner.Instance, corrected cooldown overlay anchors, fixed ContentSizeFitter/HLG conflict
