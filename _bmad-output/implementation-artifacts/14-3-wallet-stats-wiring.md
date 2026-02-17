# Story 14.3: Wallet & Stats Wiring (Left/Right Wings)

Status: ready-for-dev

## Story

As a player,
I want my Cash, Profit, and Target displayed in the left column of the Control Deck, and my Position status, Time, and Rep in the right column,
so that all my critical info is in one consolidated bottom dashboard.

## Acceptance Criteria

1. **Left Wing (Wallet)** populated by `UISetup.ExecuteControlDeck()` with:
   - Header label: "WALLET" in `CRTThemeData.TextLow` (dim cyan), 10pt
   - Cash row: "Cash:" label + value text in `CRTThemeData.TextHigh` (phosphor green)
   - Profit row: "Round Profit:" label + value text (green/red dynamic via `TradingHUD.GetProfitColor`)
   - Target row: "Target:" label + "$X.XX / $Y.YY" value text
2. **Right Wing (Stats)** populated by `UISetup.ExecuteControlDeck()` with:
   - Position header: "POSITIONS" in `CRTThemeData.TextLow`, 10pt
   - Direction row: "LONG" / "SHORT" / "FLAT" (existing PositionOverlay color logic)
   - P&L row: Trade P&L with avg price (existing format from PositionOverlay)
   - Time row: "TIME:" + countdown value in `CRTThemeData.TextHigh`
   - Rep row: "REP:" + star + value in `CRTThemeData.Warning` (amber)
3. `TradingHUD` reads from new text references (Left Wing Cash/Profit/Target) from `DashboardReferences`
4. `PositionOverlay` reads from new text references (Right Wing Position/P&L) from `DashboardReferences`
5. `RoundTimerUI` reads from new timer text reference (Right Wing Time) from `DashboardReferences`
6. Old `PositionOverlay` separate canvas is removed — logic merged into Control Deck right wing
7. Old `ExecutePositionOverlay()` method removed from UISetup
8. All real-time updates continue to work (PriceUpdatedEvent, TradeExecutedEvent, RoundStartedEvent)
9. Reputation display updates correctly from `RunContext.Reputation.Current`

## Tasks / Subtasks

- [ ] Task 1: Populate Left Wing in `UISetup.ExecuteControlDeck()` (AC: 1)
  - [ ] 1.1: Create "WALLET" header label (CRTThemeData.TextLow, 10pt)
  - [ ] 1.2: Create Cash row — "Cash:" label + value Text (CRTThemeData.TextHigh)
  - [ ] 1.3: Create Profit row — "Round Profit:" label + value Text (dynamic color)
  - [ ] 1.4: Create Target row — "Target:" label + value Text + progress bar
  - [ ] 1.5: Store all Text references in DashboardReferences
- [ ] Task 2: Populate Right Wing in `UISetup.ExecuteControlDeck()` (AC: 2)
  - [ ] 2.1: Create "POSITIONS" header label (CRTThemeData.TextLow, 10pt)
  - [ ] 2.2: Create Direction row — bold text for LONG/SHORT/FLAT
  - [ ] 2.3: Create Avg Price row — "Avg: $X.XX" text
  - [ ] 2.4: Create P&L row — "P&L: +$X.XX" bold text
  - [ ] 2.5: Create Time row — "TIME:" label + countdown value (CRTThemeData.TextHigh)
  - [ ] 2.6: Create Rep row — "REP:" label + "★ X" value (CRTThemeData.Warning amber)
  - [ ] 2.7: Store all Text references in DashboardReferences
- [ ] Task 3: Update DashboardReferences with all text fields (AC: 1, 2)
  - [ ] 3.1: Add Left Wing fields: CashText, ProfitText, TargetText, TargetProgressBar
  - [ ] 3.2: Add Right Wing fields: DirectionText, AvgPriceText, PnlText, TimerText, TimerProgressBar, RepText
  - [ ] 3.3: Add row GameObjects for show/hide: AvgPriceRow, PnlRow
- [ ] Task 4: Wire TradingHUD to new references (AC: 3, 8)
  - [ ] 4.1: Update `TradingHUD.Initialize()` to read Cash/Profit/Target/Rep texts from DashboardReferences
  - [ ] 4.2: Verify RefreshDisplay() updates all new text fields correctly
  - [ ] 4.3: Verify profit color coding (green/red) works on new Profit text
  - [ ] 4.4: Verify target progress bar updates from DashboardReferences
- [ ] Task 5: Wire PositionOverlay to new references (AC: 4, 6, 8)
  - [ ] 5.1: Modify PositionOverlay.Initialize() to accept text refs from DashboardReferences
  - [ ] 5.2: Remove separate PositionOverlay canvas creation — overlay logic now drives Right Wing texts
  - [ ] 5.3: Verify direction/avgPrice/pnl display and color-coding works in new location
  - [ ] 5.4: Verify SetActiveStock() and real-time P&L updates continue working
- [ ] Task 6: Wire RoundTimerUI to new references (AC: 5, 8)
  - [ ] 6.1: Pass Right Wing timer text and progress bar to RoundTimerUI.Initialize()
  - [ ] 6.2: Verify countdown, color transitions, and pulse animation work in new location
- [ ] Task 7: Remove old ExecutePositionOverlay (AC: 6, 7)
  - [ ] 7.1: Delete `UISetup.ExecutePositionOverlay()` method
  - [ ] 7.2: Remove `_positionOverlay = UISetup.ExecutePositionOverlay(...)` call from GameRunner.Start()
  - [ ] 7.3: Update GameRunner to get PositionOverlay from the Control Deck setup path instead
- [ ] Task 8: Verify all real-time updates (AC: 8, 9)
  - [ ] 8.1: Verify PriceUpdatedEvent triggers TradingHUD refresh (Cash, Portfolio, Profit)
  - [ ] 8.2: Verify TradeExecutedEvent triggers PositionOverlay rebuild (Direction, P&L)
  - [ ] 8.3: Verify RoundTimerUI countdown ticks in Right Wing
  - [ ] 8.4: Verify Reputation display updates after store purchases

## Dev Notes

### Architecture Compliance

- **One-way data flow:** TradingHUD reads from RunContext.Portfolio. PositionOverlay reads from Portfolio positions. RoundTimerUI reads from TradingState. No two-way bindings.
- **EventBus-driven updates:** All display refreshes are triggered by events (PriceUpdatedEvent, TradeExecutedEvent, RoundStartedEvent). The dirty-flag LateUpdate pattern remains.
- **No Find calls:** All UI element references flow through DashboardReferences — no `GameObject.Find()` or `FindObjectOfType<>()` for display elements.

### Left Wing Layout Detail

```
Left_Wing (VerticalLayoutGroup, spacing=4, padding=8)
├── "WALLET" (TextLow, 10pt, dim header)
├── CashRow (HorizontalLayout)
│   ├── "Cash:" label (TextLow, 12pt)
│   └── CashValue text (TextHigh, 16pt, bold)
├── ProfitRow (HorizontalLayout)
│   ├── "Profit:" label (TextLow, 12pt)
│   └── ProfitValue text (dynamic color, 16pt, bold)
└── TargetRow (HorizontalLayout)
    ├── "Target:" label (TextLow, 12pt)
    └── TargetValue text (TextHigh, 14pt)
```

### Right Wing Layout Detail

```
Right_Wing (VerticalLayoutGroup, spacing=4, padding=8)
├── "POSITIONS" (TextLow, 10pt, dim header)
├── DirectionText ("LONG"/"SHORT"/"FLAT", 18pt, bold, dynamic color)
├── AvgPriceRow → "Avg: $X.XX" (TextLow, 13pt) — hidden when FLAT
├── PnlRow → "P&L: +$X.XX" (dynamic color, 15pt, bold) — hidden when FLAT
├── TimerRow (HorizontalLayout)
│   ├── "TIME:" label (TextLow, 12pt)
│   └── TimerValue text (TextHigh, 16pt, bold)
└── RepRow (HorizontalLayout)
    ├── "REP:" label (TextLow, 12pt)
    └── RepValue text (Warning/amber, 16pt, bold, "★ X")
```

### PositionOverlay Refactoring Strategy

The current `PositionOverlay` class (PositionOverlay.cs) has clean separation between:
- **Initialization:** Accepts Text references and Portfolio
- **Event handling:** OnPriceUpdated, OnTradeExecuted, OnRoundStarted
- **Display logic:** ShowFlat(), ShowPosition(), UpdatePnL()

Only the initialization source changes (from separate canvas to Control Deck right wing). All internal logic stays the same. The `Initialize()` signature remains compatible — it just receives different Text objects.

### GameRunner.Start() Changes

Current flow:
```csharp
UISetup.Execute(_ctx, _ctx.CurrentRound, GameConfig.RoundDurationSeconds);
_positionOverlay = UISetup.ExecutePositionOverlay(_ctx.Portfolio);
```

New flow:
```csharp
var dashRefs = UISetup.ExecuteControlDeck(_ctx, _ctx.CurrentRound, GameConfig.RoundDurationSeconds);
// PositionOverlay is now created inside ExecuteControlDeck and attached to Control Deck
// GameRunner accesses it via dashRefs or FindObjectOfType<PositionOverlay>
```

### Existing TradingHUD.Initialize() Signature (to change)

Current (UISetup.cs:126-134):
```csharp
tradingHUD.Initialize(
    runContext, currentRound, roundDuration,
    cashValue.GetComponent<Text>(),
    portfolioValueText.GetComponent<Text>(),
    portfolioChange.GetComponent<Text>(),
    profitValue.GetComponent<Text>(),
    targetValue.GetComponent<Text>(),
    targetBar
);
```

The new signature should accept DashboardReferences (or individual text fields sourced from it). The existing private fields `_cashText`, `_portfolioValueText`, etc. remain and get assigned from the new source.

### Existing PositionOverlay.Initialize() Signature (compatible)

Current (PositionOverlay.cs:31-32):
```csharp
public void Initialize(Portfolio portfolio, Text directionText, Text avgPriceText, Text pnlText,
    GameObject avgPriceRow, GameObject pnlRow)
```

This signature can remain identical — just pass different Text objects from the Control Deck Right Wing instead of from a separate canvas.

### Testing Approach

- Visual testing: Run game, verify Left Wing shows Cash/Profit/Target, Right Wing shows Position/Time/Rep.
- Real-time: Execute trades, verify P&L updates. Watch timer countdown. Check profit color changes.
- State transitions: Verify position goes FLAT → LONG → FLAT correctly with row show/hide.
- Regression: Existing keyboard shortcuts (B, S, D) still trigger trades.

### References

- [Source: _bmad-output/planning-artifacts/epic-14-terminal-1999-ui.md#Story 14.3]
- [Source: Assets/Scripts/Setup/UISetup.cs:40-141] — current Execute(RunContext) with top bar
- [Source: Assets/Scripts/Setup/UISetup.cs:308-385] — current ExecutePositionOverlay() to remove
- [Source: Assets/Scripts/Runtime/UI/TradingHUD.cs:41-63] — Initialize() to rewire
- [Source: Assets/Scripts/Runtime/UI/TradingHUD.cs:133-203] — RefreshDisplay() logic (unchanged)
- [Source: Assets/Scripts/Runtime/UI/PositionOverlay.cs:31-46] — Initialize() to rewire
- [Source: Assets/Scripts/Runtime/UI/PositionOverlay.cs:104-153] — display logic (unchanged)
- [Source: Assets/Scripts/Runtime/UI/RoundTimerUI.cs:29-39] — Initialize() to rewire
- [Source: Assets/Scripts/Runtime/Core/GameRunner.cs:96-107] — Start() flow to update

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
