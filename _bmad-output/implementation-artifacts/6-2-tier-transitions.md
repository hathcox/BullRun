# Story 6.2: Tier Transitions

Status: ready-for-dev

## Story

As a player,
I want visual and audio changes between acts,
so that progressing to a new market tier feels like entering a new stage.

## Acceptance Criteria

1. When advancing from one act to the next, a tier transition screen is shown
2. Transition screen displays the new act number, tier name, and a brief tagline
3. Each tier has a distinct visual theme applied to the trading UI (color accent, background tint)
4. Transition has a dramatic entrance animation (fade, slide, or zoom)
5. Transition screen displays for 2-3 seconds before proceeding to MarketOpen
6. Tier visual theme persists throughout the act's rounds

## Tasks / Subtasks

- [ ] Task 1: Create TierTransitionUI screen (AC: 1, 2, 4, 5)
  - [ ] Full-screen overlay with act/tier reveal
  - [ ] Layout: "ACT 2" (large), "LOW-VALUE STOCKS" (subtitle), tagline (small)
  - [ ] Taglines per act:
    - Act 1: "The Penny Pit — Where Fortunes Begin"
    - Act 2: "Rising Stakes — Trends and Reversals"
    - Act 3: "The Trading Floor — Sectors in Motion"
    - Act 4: "Blue Chip Arena — The Big Leagues"
  - [ ] Animate: fade in (0.5s), hold (2s), fade out (0.5s)
  - [ ] File: `Scripts/Runtime/UI/TierTransitionUI.cs`
- [ ] Task 2: Define per-tier visual themes (AC: 3, 6)
  - [ ] Create `TierVisualTheme` data: accent color, background tint, chart line color variation
  - [ ] Penny: wild/chaotic feel — hot neon green, dark purple tint
  - [ ] Low-Value: warmer — amber/gold accents, dark blue tint
  - [ ] Mid-Value: professional — cyan/teal accents, navy tint
  - [ ] Blue Chip: premium — gold accents, deep black tint
  - [ ] File: `Scripts/Setup/Data/TierVisualData.cs` (new)
- [ ] Task 3: Apply tier theme to trading UI (AC: 3, 6)
  - [ ] On act change: update chart line color, sidebar accent, HUD accent from TierVisualTheme
  - [ ] Background tint applied to chart area
  - [ ] Theme persists for both rounds in the act
  - [ ] Method: `ApplyTierTheme(TierVisualTheme theme)` on relevant UI components
  - [ ] File: `Scripts/Runtime/UI/TradingHUD.cs` (extend), `Scripts/Runtime/UI/StockSidebar.cs` (extend), `Scripts/Runtime/Chart/ChartRenderer.cs` (extend)
- [ ] Task 4: Wire transition into game loop (AC: 1, 5)
  - [ ] When `RunContext.IsNewAct` is true after AdvanceRound(), show TierTransitionUI before MarketOpen
  - [ ] Transition happens between ShopState exit and next MarketOpenState enter
  - [ ] Skip transition for Act 1 Round 1 (run just started, no transition needed)
  - [ ] File: `Scripts/Runtime/Core/GameStates/ShopState.cs` (extend) or create `TierTransitionState.cs`
- [ ] Task 5: Add tier transition data to GameConfig (AC: 2)
  - [ ] Add taglines to ActConfig struct
  - [ ] Add `TransitionDurationSeconds` = 3f
  - [ ] File: `Scripts/Setup/Data/GameConfig.cs` (extend)
- [ ] Task 6: Add TierTransitionUI to UISetup (AC: 1)
  - [ ] Generate overlay panel for tier transitions
  - [ ] File: `Scripts/Setup/UISetup.cs` (extend)

## Dev Notes

### Architecture Compliance

- **Visual themes as data** — `Scripts/Setup/Data/TierVisualData.cs` follows the data pattern
- **State machine:** Could use a dedicated `TierTransitionState` or handle within ShopState/MarketOpenState. A dedicated state is cleaner and follows the flat state machine pattern.
- **UI reads theme data** — one-way dependency, UI applies themes from data classes

### Tier Visual Progression

The visual escalation should mirror the gameplay escalation. Each tier feels more intense, more premium, and more high-stakes:

```
Act 1 (Penny):    Gritty, neon, chaotic energy      → "Back alley trading"
Act 2 (Low):      Warmer, more structured            → "Trading floor"
Act 3 (Mid):      Professional, clean, sector-aware  → "Corner office"
Act 4 (Blue):     Premium, gold, high stakes         → "Penthouse"
```

This progression echoes the office upgrade meta-progression from the GDD.

### Audio Transition Note

Audio changes per tier (different music tracks, ambient) are covered in Epic 11. This story handles visual theming only. The tier transition screen is a natural point for both to switch simultaneously.

### Project Structure Notes

- Creates: `Scripts/Runtime/UI/TierTransitionUI.cs`
- Creates: `Scripts/Setup/Data/TierVisualData.cs`
- Creates (optional): `Scripts/Runtime/Core/GameStates/TierTransitionState.cs`
- Modifies: `Scripts/Runtime/UI/TradingHUD.cs`, `Scripts/Runtime/UI/StockSidebar.cs`, `Scripts/Runtime/Chart/ChartRenderer.cs` (theme application)
- Modifies: `Scripts/Setup/Data/GameConfig.cs`
- Modifies: `Scripts/Setup/UISetup.cs`

### References

- [Source: bull-run-gdd-mvp.md#2.1] — Act/tier table with volatility and mechanic progression
- [Source: bull-run-gdd-mvp.md#7.1] — "Dark navy backgrounds with neon green for gains, hot pink for losses, gold for premium/rare"
- [Source: bull-run-gdd-mvp.md#9] — "Add visual/audio differentiation per tier" (Week 5 goal)
- [Source: game-architecture.md#Game State Machine] — State flow supports intermediate states

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
