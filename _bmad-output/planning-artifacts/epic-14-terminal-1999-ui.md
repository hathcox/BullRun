# Epic 14: Terminal 1999 — CRT Trading Cockpit UI

**Description:** Complete visual rework of the trading-phase UI from scattered floating panels into a cohesive retro-futuristic CRT Trading Cockpit. Consolidates the top HUD bar, position overlay, and trade panel into a single bottom-docked "Control Deck." Frames the entire screen as a 1999 CRT monitor with scanlines, vignette, and phosphor-green text. Introduces a centralized theme system for consistent CRT aesthetic.

**Status:** Ready for dev
**Phase:** Post-Epic 13, visual overhaul of the trading phase
**Depends On:** Epic 13 (Store Rework — complete), FIX-16 (UI positioning — complete)

**Current State (Image #3):**
Scattered dev-art layout: Cash/Profit/Target/Rep/Time in a top bar, PositionOverlay bottom-left, Buy/Sell/Short buttons bottom-center, Chart floating in void. Player's eyes dart across the full screen.

**Target State (Image #4):**
CRT monitor frame. All status info consolidated into a rigid bottom-docked "Control Deck" with three columns (Wallet | Actions | Stats). Chart anchored above the Control Deck with visible grid lines. Event ticker is a prominent amber banner above the chart area. Phosphor-green text with CRT glow.

**Layout Reference (Target):**
```
┌─────────────────────────────────────────────────────────────┐
│  CRT Bezel Frame (vignette + scanlines)                     │
│  ┌─────────────────────────────────────────────────────────┐│
│  │     ◉ DOGE  $2.77                                      ││
│  │  ┌───────────────────────────────────────────────────┐  ││
│  │  │ ⚠ COMPLIANCE CONCERNS WEIGH ON DOGE              │  ││
│  │  └───────────────────────────────────────────────────┘  ││
│  │                                                         ││
│  │               CHART AREA                         $9.50  ││
│  │           (line chart with grid)                 $7.20  ││
│  │                                                  $5.00  ││
│  │                                                  $2.77  ││
│  │                                                         ││
│  │  ┌──────────────┬──────────────────┬───────────────┐   ││
│  │  │ WALLET       │   SELL    BUY    │ POSITIONS     │   ││
│  │  │ Cash: $2.68  │     SHORT        │ LONG  1x      │   ││
│  │  │ Profit:-$0.32│                  │ P&L: -$4.41   │   ││
│  │  │ Target:$9.68 │                  │ TIME: 0:52    │   ││
│  │  │   / $20.00   │                  │ REP: ★ 0      │   ││
│  │  └──────────────┴──────────────────┴───────────────┘   ││
│  └─────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────┘
```

---

## Story 14.1: CRT Theme Data System

As a developer, I want a centralized CRT theme data class with all Terminal 1999 colors and a DashboardReferences struct, so that all UI stories share consistent colors and GameRunner has clean access to UI elements without GameObject.Find.

**Acceptance Criteria:**
- New `CRTThemeData.cs` in `Scripts/Setup/Data/` with `public static readonly` color fields:
  - `Background`: #050a0a (Deep Teal/Black)
  - `Panel`: #061818 at 90% alpha (Dark Teal)
  - `TextHigh`: #28f58d (Phosphor Green)
  - `TextLow`: #3b6e6e (Dim Cyan)
  - `Warning`: #ffb800 (Amber)
  - `Danger`: #ff4444 (CRT Red)
  - `Border`: #224444 (Muted Teal)
  - `ButtonBuy`: #28f58d (Green)
  - `ButtonSell`: #ff4444 (Red)
  - `ButtonShort`: #ffb800 (Amber)
- New `DashboardReferences` class in `Scripts/Runtime/UI/` containing public fields for all dashboard UI elements (Text, Button, Image references) so GameRunner can wire without Find
- Helper method `CRTThemeData.ApplyLabelStyle(Text text, bool highlight)` for consistent text styling
- Helper method `CRTThemeData.ApplyPanelStyle(Image image)` for consistent panel backgrounds
- No regressions — existing code still compiles (new files only, no modifications yet)

**Files to create:**
- `Assets/Scripts/Setup/Data/CRTThemeData.cs` — theme colors and helpers
- `Assets/Scripts/Runtime/UI/DashboardReferences.cs` — UI element reference container

---

## Story 14.2: Control Deck Layout Shell

As a developer, I want the bottom-docked Control Deck panel structure with three empty columns, so that subsequent stories can populate the Wallet, Actions, and Stats wings.

**Acceptance Criteria:**
- New method `UISetup.ExecuteControlDeck()` creates:
  - `Control_Deck_Panel`: Bottom-center anchor, HorizontalLayoutGroup (Padding: 10, Spacing: 20)
  - `Left_Wing` container: VerticalLayoutGroup (for Wallet section)
  - `Center_Core` container: VerticalLayoutGroup (for Action buttons)
  - `Right_Wing` container: VerticalLayoutGroup (for Stats section)
- Panel background uses `CRTThemeData.Panel` color
- Border outline using `CRTThemeData.Border` color (Image with outline or nested panel)
- Control Deck spans ~90% of screen width, height ~160px
- Anchored to bottom-center (anchorMin 0.05,0 / anchorMax 0.95,0 / pivot 0.5,0)
- Returns `DashboardReferences` with container transforms for population
- Old top bar creation in `UISetup.Execute()` is **removed** (Cash/Profit/Target/Rep/Timer sections deleted from top bar code)
- `TradingHUD.Initialize()` signature updated to accept the new DashboardReferences instead of individual Text fields (backward-compatible: old fields still work if not null)
- Canvas sorting order fits between chart (10) and feedback (23)

**Files to modify:**
- `Assets/Scripts/Setup/UISetup.cs` — add `ExecuteControlDeck()`, remove old top bar code from `Execute()`
- `Assets/Scripts/Runtime/UI/DashboardReferences.cs` — add container references

---

## Story 14.3: Wallet & Stats Wiring (Left/Right Wings)

As a player, I want my Cash, Profit, and Target displayed in the left column of the Control Deck, and my Position status, Time, and Rep in the right column, so that all my critical info is in one consolidated bottom dashboard.

**Acceptance Criteria:**
- **Left Wing (Wallet):** Populated by `UISetup.ExecuteControlDeck()`:
  - Header label: "WALLET" in `CRTThemeData.TextLow` (dim cyan), 10pt
  - Cash row: "Cash:" label + value text, `CRTThemeData.TextHigh` (phosphor green)
  - Profit row: "Round Profit:" label + value text (green/red dynamic)
  - Target row: "Target:" label + "$X.XX / $Y.YY" value text
- **Right Wing (Stats):** Populated by `UISetup.ExecuteControlDeck()`:
  - Position header: "POSITIONS" in `CRTThemeData.TextLow`, 10pt
  - Direction row: "LONG" / "SHORT" / "FLAT" (existing PositionOverlay colors)
  - P&L row: Trade P&L with avg price (existing format)
  - Time row: "TIME:" + countdown value in `CRTThemeData.TextHigh`
  - Rep row: "RFP:" + star + value in `CRTThemeData.Warning` (amber)
- `TradingHUD` reads from new text references (Left Wing Cash/Profit/Target)
- `PositionOverlay` reads from new text references (Right Wing Position/P&L)
- `RoundTimerUI` reads from new timer text reference (Right Wing Time)
- Old `PositionOverlay` separate canvas is removed — logic merged into Control Deck
- Old `ExecutePositionOverlay()` method removed from UISetup
- All real-time updates continue to work (PriceUpdatedEvent, TradeExecutedEvent)

**Files to modify:**
- `Assets/Scripts/Setup/UISetup.cs` — populate Left/Right wings in `ExecuteControlDeck()`
- `Assets/Scripts/Runtime/UI/TradingHUD.cs` — accept new text refs from DashboardReferences
- `Assets/Scripts/Runtime/UI/PositionOverlay.cs` — accept new text refs, remove separate canvas
- `Assets/Scripts/Runtime/UI/RoundTimerUI.cs` — accept new timer text ref
- `Assets/Scripts/Runtime/UI/DashboardReferences.cs` — add all text field references

---

## Story 14.4: Action Buttons (Center Core)

As a player, I want the SELL, BUY, and SHORT buttons displayed in the center column of the Control Deck, so that my trading actions are consolidated with the rest of my dashboard.

**Acceptance Criteria:**
- **Center Core** populated by `UISetup.ExecuteControlDeck()`:
  - Top row: HorizontalLayoutGroup with SELL (left, `CRTThemeData.Danger`) and BUY (right, `CRTThemeData.ButtonBuy`)
  - Bottom row: SHORT button full-width (`CRTThemeData.ButtonShort` — amber)
  - Button text: white, bold, 20pt (SELL/BUY), 16pt (SHORT)
- Cooldown overlay repositioned to cover Center Core area
- Short P&L panel appears inline below SHORT button when active
- Short 2 container (Dual Short expansion) positioned beside first short when active
- Leverage badge positioned above Center Core when expansion active
- All button click wiring preserved:
  - BUY/SELL → `TradeButtonPressedEvent`
  - SHORT → `GameRunner.HandleShortInput()`
  - SHORT 2 → `GameRunner.HandleShort2Input()`
- `QuantitySelector` references updated to new button Images/Texts
- Old `ExecuteTradePanel()` method removed from UISetup
- Keyboard shortcuts (B, S, D) still work

**Files to modify:**
- `Assets/Scripts/Setup/UISetup.cs` — populate Center Core, remove old `ExecuteTradePanel()`
- `Assets/Scripts/Runtime/UI/QuantitySelector.cs` — new button/overlay references from DashboardReferences
- `Assets/Scripts/Runtime/UI/DashboardReferences.cs` — add button references
- `Assets/Scripts/Runtime/Core/GameRunner.cs` — use DashboardReferences instead of finding QuantitySelector

---

## Story 14.5: Chart Repositioning & Event Ticker

As a player, I want the price chart anchored above the Control Deck with an amber event ticker banner between the stock label and chart, so that the chart is framed within the CRT viewport and events are immediately visible.

**Acceptance Criteria:**
- **Chart Bounds:** Recalculated to fit ABOVE the Control Deck (~top 70% of screen, leaving bottom ~20% for Control Deck, top ~10% for stock name/ticker)
- `ChartSetup.Execute()` chart height/position adjusted: `chartBottom` raised to account for Control Deck height
- **Stock Label Area** (top of chart viewport):
  - Stock ticker + price centered at top, using `CRTThemeData.TextHigh` for ticker, white for price
  - Larger font sizes (28pt ticker, 20pt price) for CRT readability
- **Event Ticker Banner:**
  - Amber background (#ffb800 at 85% alpha) banner between stock label and chart
  - Shows event headlines from `NewsBanner` events
  - Full-width, 36px height
  - Warning icon (triangle ⚠) prefix on event text
  - Slides in from right, stays visible for event duration
  - Position: anchored below stock label, above chart area
- **Chart Grid Lines:** Updated colors to use `CRTThemeData.Border` (#224444) with subtle opacity (0.2)
- **Chart Background:** Updated to `CRTThemeData.Background` (#050a0a)
- **Price Axis Labels:** Updated to `CRTThemeData.TextLow` (dim cyan)
- **Current Price Label:** Updated to `CRTThemeData.TextHigh` (phosphor green)
- NewsBanner and NewsTicker behavior updated to use event ticker instead of separate overlays

**Files to modify:**
- `Assets/Scripts/Setup/ChartSetup.cs` — chart bounds, grid colors, label colors, background
- `Assets/Scripts/Setup/UISetup.cs` — event ticker banner creation (or integrate into ChartSetup)
- `Assets/Scripts/Runtime/UI/NewsBanner.cs` — redirect to event ticker display
- `Assets/Scripts/Runtime/Chart/ChartUI.cs` — label color updates

---

## Story 14.6: CRT Bezel Overlay & Visual Polish

As a player, I want the entire screen framed as a curved 1999 CRT monitor with scanline effects and phosphor glow, so that the trading cockpit has a cohesive retro-futuristic aesthetic.

**Acceptance Criteria:**
- **CRT Overlay Panel:** New full-screen ScreenSpaceOverlay canvas (highest sorting order, raycast disabled):
  - Vignette image: dark edges fading to transparent center (can be a procedural gradient or a pre-made sprite in `_Imported/Art/UI/`)
  - Scanline overlay: subtle horizontal lines (0.5px, ~3px spacing, 5-10% opacity) — generated via a repeating texture or shader
  - Both non-interactive (raycast disabled) so they don't block input
- **Panel Border Styling:** All Control Deck panels get a thin border effect using nested Image outlines (1px `CRTThemeData.Border` color)
- **Global CRT Theme Application:**
  - All existing scattered color constants updated to reference `CRTThemeData`:
    - `UISetup.BarBackgroundColor` → `CRTThemeData.Panel`
    - `UISetup.LabelColor` → `CRTThemeData.TextLow`
    - `UISetup.ValueColor` → `CRTThemeData.TextHigh`
    - `TradingHUD.ProfitGreen` → `CRTThemeData.TextHigh`
    - `TradingHUD.LossRed` → `CRTThemeData.Danger`
    - `TradingHUD.WarningYellow` → `CRTThemeData.Warning`
    - `PositionOverlay.LongColor` → `CRTThemeData.TextHigh`
    - `NewsBanner.PositiveBannerColor` → green variant of theme
    - `NewsBanner.NegativeBannerColor` → `CRTThemeData.Danger`
  - `ChartSetup.BackgroundColor` → `CRTThemeData.Background`
- **URP Post-Processing:** Bloom intensity increased slightly for phosphor glow on bright green text (adjust URP Volume asset — may need programmatic Volume setup)
- **TierVisualData Integration:** Tier themes now layer OVER the CRT base theme (tier accent color tints the chart line and accents, but CRT base colors remain for panels/text)
- No performance regressions — overlay is static imagery, not per-frame computation

**Files to modify:**
- `Assets/Scripts/Setup/UISetup.cs` — CRT overlay creation method, color constant updates
- `Assets/Scripts/Setup/ChartSetup.cs` — background color update
- `Assets/Scripts/Setup/Data/CRTThemeData.cs` — scanline/vignette configuration values
- `Assets/Scripts/Setup/Data/TierVisualData.cs` — integrate with CRT base theme
- `Assets/Scripts/Runtime/UI/TradingHUD.cs` — color references to CRTThemeData
- `Assets/Scripts/Runtime/UI/PositionOverlay.cs` — color references to CRTThemeData
- `Assets/Scripts/Runtime/UI/NewsBanner.cs` — color references to CRTThemeData

---

## Dependency Graph

```
14.1 (Theme Data + DashboardReferences)
  └── 14.2 (Control Deck Layout Shell)
        ├── 14.3 (Wallet & Stats — Left/Right Wings)
        ├── 14.4 (Action Buttons — Center Core)
        └── 14.5 (Chart Repositioning & Event Ticker)
              └── 14.6 (CRT Bezel Overlay & Visual Polish)
```

**Recommended implementation order:**
1. **14.1** — Theme data + references struct (foundation, no visual changes)
2. **14.2** — Control Deck shell (creates the layout, removes top bar)
3. **14.3** — Wallet + Stats wiring (Left/Right wing content)
4. **14.4** — Action buttons (Center Core content)
5. **14.5** — Chart repositioning + event ticker
6. **14.6** — CRT bezel + visual polish (final layer)

---

## Key Technical Notes for Dev Agent

### UISetup.cs Restructure Strategy
The current `UISetup.Execute(RunContext, int, float)` creates the top bar HUD. This method must be gutted — the top bar is removed. Replace with `ExecuteControlDeck()` that builds the new bottom dashboard. The parameterless `Execute()` (called by F5) should still create MarketOpenUI.

### Canvas Sorting Order Plan
| Canvas | Sorting Order | Purpose |
|--------|--------------|---------|
| ChartCanvas | 10 | Chart labels, price axis |
| ControlDeckCanvas | 20 | Bottom dashboard |
| FeedbackCanvas | 23 | Trade feedback popups |
| EventTickerCanvas | 25 | Amber event banner |
| TimerCanvas | 25 | Round timer (if separate) |
| MarketOpenCanvas | 100 | Market open overlay |
| RoundResultsCanvas | 105 | Round results |
| RunSummaryCanvas | 110 | Run summary |
| CRTOverlayCanvas | 999 | Scanlines + vignette (non-interactive) |

### Legacy Text vs TMP Decision
The codebase uses `UnityEngine.UI.Text` throughout. The user's prompt mentioned TMP_FontAsset, but migrating all text to TMP is a separate effort. For this epic, we keep legacy Text and achieve the glow effect via URP Bloom post-processing (which will make bright green text "bleed" naturally). A future epic can migrate to TMP if per-character glow control is needed.

### GameRunner Integration
`GameRunner.cs` currently calls `UISetup.Execute()`, `UISetup.ExecuteTradePanel()`, `UISetup.ExecutePositionOverlay()`, and `ChartSetup.Execute()` separately. After this epic, the flow becomes:
1. `ChartSetup.Execute()` — chart (repositioned)
2. `UISetup.ExecuteControlDeck()` — returns `DashboardReferences`
3. `UISetup.ExecuteCRTOverlay()` — bezel/scanlines on top
4. GameRunner stores `DashboardReferences` for runtime access
