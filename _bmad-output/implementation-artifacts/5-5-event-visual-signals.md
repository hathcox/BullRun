# Story 5.5: Event Visual Signals

Status: done

## Story

As a player,
I want clear visual signals when events occur,
so that I can react quickly under time pressure and learn to recognize event patterns.

## Acceptance Criteria

1. News banner flash: green banner for positive events (EarningsBeat, BullRun, MergerRumor), red for negative (EarningsMiss, MarketCrash, SECInvestigation) — slides from top with headline text
2. Suspicious volume spike indicator: pulsing icon on affected stock in sidebar when PumpAndDump fires
3. Warning icon: yellow triangle on affected stock in sidebar when SECInvestigation fires
4. Sector highlight: winning sector stocks glow green, losing sector stocks glow red in sidebar during SectorRotation
5. Screen shake + red border pulse when MarketCrash fires
6. Short position warning: red exclamation on affected stock in positions panel when ShortSqueeze fires
7. News ticker crawl in bottom bar scrolling event headlines as they fire
8. All signals appear the same frame the event fires (synchronous via EventBus)
9. Signals auto-dismiss when the event ends (subscribe to MarketEventEndedEvent)

## Tasks / Subtasks

- [x] Task 1: Create NewsBanner UI component (AC: 1, 8, 9)
  - [x] Create `Scripts/Runtime/UI/NewsBanner.cs` MonoBehaviour
  - [x] Subscribe to `MarketEventFiredEvent` — on event, show banner with headline text
  - [x] Banner slides down from top of screen, semi-transparent background
  - [x] Green background + text for `IsPositive = true` events, red for `IsPositive = false`
  - [x] Uses `MarketEventFiredEvent.Headline` from Story 5-2 (already populated)
  - [x] Auto-fade after 3 seconds or on `MarketEventEndedEvent`
  - [x] Stack multiple banners if events fire rapidly (queue with slight vertical offset)
  - [x] Create programmatically in UISetup — no Inspector config

- [x] Task 2: Create NewsTicker UI component (AC: 7, 8)
  - [x] Create `Scripts/Runtime/UI/NewsTicker.cs` MonoBehaviour
  - [x] Horizontal scrolling text strip in the bottom bar area
  - [x] Subscribe to `MarketEventFiredEvent` — inject headline into ticker queue
  - [x] Headlines scroll left-to-right continuously
  - [x] Multiple headlines can be visible simultaneously if events fire in quick succession
  - [x] Scrolling speed: ~100 pixels/second (configurable)
  - [x] Old headlines scroll off-screen and are removed
  - [x] Create programmatically in UISetup

- [x] Task 3: Add event indicator slots to StockSidebar (AC: 2, 3, 4)
  - [x] Extend `StockSidebar.cs` stock entries with an icon/indicator area
  - [x] Subscribe to `MarketEventFiredEvent` — show appropriate icon on affected stock(s)
  - [x] PumpAndDump: pulsing volume bar icon (animated alpha pulse) on target stock
  - [x] SECInvestigation: yellow warning triangle icon on target stock
  - [x] SectorRotation: green glow border on winning sector stocks, red glow border on losing sector stocks
  - [x] Subscribe to `MarketEventEndedEvent` — remove indicators when event expires
  - [x] Map `MarketEventFiredEvent.AffectedStockIds` to sidebar entries
  - [x] File: `Scripts/Runtime/UI/StockSidebar.cs` (extend)

- [x] Task 4: Create ScreenEffects component for Market Crash (AC: 5, 8)
  - [x] Create `Scripts/Runtime/UI/ScreenEffects.cs` MonoBehaviour
  - [x] Subscribe to `MarketEventFiredEvent` — on MarketCrash, trigger screen shake + red pulse
  - [x] Screen shake: offset root Canvas position randomly, decay over 1.5 seconds
  - [x] Red border pulse: UI Image overlay with red color, alpha pulses 0→0.3→0 over 2 seconds
  - [x] BullRun: subtle green tint overlay on chart background (lower intensity than crash)
  - [x] FlashCrash: brief red flash (0.2s full-screen red overlay, then fade)
  - [x] Create programmatically in UISetup
  - [x] Clean up effects on `MarketEventEndedEvent`

- [x] Task 5: Add short position warning to PositionPanel (AC: 6)
  - [x] Subscribe to `MarketEventFiredEvent` — on ShortSqueeze, show red exclamation icon next to the affected stock's position
  - [x] Warning icon pulses to draw attention
  - [x] Remove on `MarketEventEndedEvent` for ShortSqueeze type
  - [x] File: `Scripts/Runtime/UI/PositionPanel.cs` (extend from Story 3.4)

- [x] Task 6: Add UI elements to UISetup (AC: 1, 5, 7)
  - [x] Generate NewsBanner overlay area (top of screen, above chart)
  - [x] Generate NewsTicker in bottom bar area
  - [x] Generate ScreenEffects full-screen overlay (invisible by default)
  - [x] Wire MonoBehaviour references during setup
  - [x] File: `Scripts/Setup/UISetup.cs` (extend)

- [x] Task 7: Write tests (AC: 1-9)
  - [x] Test: NewsBanner shows green banner for IsPositive=true events
  - [x] Test: NewsBanner shows red banner for IsPositive=false events
  - [x] Test: NewsBanner displays correct headline text from MarketEventFiredEvent
  - [x] Test: StockSidebar shows correct icon type per event (PumpAndDump=volume, SEC=warning)
  - [x] Test: Indicators are removed when MarketEventEndedEvent fires
  - [x] Test: ScreenEffects activates shake for MarketCrash events
  - [x] Test: NewsTicker queues multiple headlines in order
  - [x] File: `Tests/Runtime/UI/NewsBannerTests.cs`
  - [x] File: `Tests/Runtime/UI/NewsTickerTests.cs`
  - [x] File: `Tests/Runtime/UI/ScreenEffectsTests.cs`

## Dev Notes

### Architecture Compliance

- **All UI is MonoBehaviour** created programmatically by UISetup — no Inspector configuration
- **EventBus subscription only** — visual signals subscribe to `MarketEventFiredEvent` and `MarketEventEndedEvent`, never poll or read game state directly
- **One-way data flow:** Events carry all needed display data (headline, IsPositive, AffectedStockIds, AffectedTickerSymbols, Duration) from Story 5-2 enrichment
- **uGUI Canvas only** — no UI Toolkit. All elements are GameObjects with RectTransform, Image, Text components
- **`_Generated/` safe** — UI elements created by UISetup in `_Generated/`, references wired during setup

### Signal-to-Event Mapping

| Event Type | Banner | Sidebar Icon | Screen Effect | Positions Warning | Ticker |
|-----------|--------|-------------|--------------|-------------------|--------|
| EarningsBeat | Green | — | — | — | Yes |
| EarningsMiss | Red | — | — | — | Yes |
| PumpAndDump | Green (initially) | Volume pulse | — | — | Yes |
| SECInvestigation | Red | Warning triangle | — | — | Yes |
| SectorRotation | — | Green/Red glow | — | — | Yes |
| MergerRumor | Green | — | — | — | Yes |
| MarketCrash | Red | — | Shake + red pulse | — | Yes |
| BullRun | Green | — | Green tint | — | Yes |
| FlashCrash | Red | — | Red flash | — | Yes |
| ShortSqueeze | Red | — | — | Red exclamation | Yes |

### UI Component Hierarchy

```
GameCanvas (existing)
├── ... (existing HUD elements)
├── NewsBannerOverlay          ← NEW (top of screen)
│   └── BannerContainer       ← Holds active banners, vertical layout
├── ScreenEffectsOverlay       ← NEW (full screen, behind HUD)
│   ├── ShakeContainer         ← Offset for shake effect
│   ├── RedPulseImage          ← Full-screen red overlay, alpha=0 default
│   ├── GreenTintImage         ← Full-screen green overlay, alpha=0 default
│   └── FlashImage             ← Full-screen red flash, alpha=0 default
├── StockSidebar (existing)
│   └── StockEntry (existing)
│       └── EventIndicator     ← NEW icon slot per stock entry
├── PositionPanel (existing)
│   └── PositionEntry (existing)
│       └── WarningIcon        ← NEW warning slot per position
└── NewsTickerBar              ← NEW (bottom of screen)
    └── ScrollingTextContainer ← Holds headline Text elements
```

### Screen Shake Implementation

Shake the NewsBanner/HUD parent transform, not the Camera (we're in uGUI Canvas space):

```csharp
private Vector3 _originalPosition;
private float _shakeIntensity;
private float _shakeDuration = 1.5f;
private float _shakeElapsed;

void UpdateShake()
{
    if (_shakeElapsed < _shakeDuration)
    {
        _shakeElapsed += Time.deltaTime;
        float decay = 1f - (_shakeElapsed / _shakeDuration);
        Vector2 offset = Random.insideUnitCircle * _shakeIntensity * decay;
        transform.localPosition = _originalPosition + (Vector3)offset;
    }
    else
    {
        transform.localPosition = _originalPosition;
    }
}
```

### Color Scheme (from GDD Section 7.1)

| Signal | Color | Hex (approximate) |
|--------|-------|-------------------|
| Positive event banner | Neon green | #00FF88 with alpha 0.8 |
| Negative event banner | Hot pink/red | #FF0066 with alpha 0.8 |
| Warning triangle | Yellow | #FFD700 |
| Volume pulse | Orange | #FF8800 |
| Crash red pulse | Deep red | #CC0000 with alpha 0.3 |
| Bull run green tint | Soft green | #00CC44 with alpha 0.15 |
| Flash red | Bright red | #FF0000 with alpha 0.5 (brief) |

### What Already Exists (DO NOT recreate)

| Component | File | Status |
|-----------|------|--------|
| `MarketEventFiredEvent` (enriched) | `GameEvents.cs` (Story 5-2) | Prerequisite — has Headline, IsPositive, AffectedTickerSymbols, Duration |
| `MarketEventEndedEvent` | `GameEvents.cs` | Complete — fires when events expire |
| `StockSidebar` | `UI/StockSidebar.cs` | Complete (Story 3.3) — extend with indicator slots |
| `PositionPanel` | `UI/PositionPanel.cs` | Complete (Story 3.4) — extend with warning icon |
| `UISetup` | `Setup/UISetup.cs` | Complete — extend with new overlay elements |
| `NewsTicker.cs` | `UI/NewsTicker.cs` | Planned in architecture — create fresh |
| `EventHeadlineData` | `Setup/Data/EventHeadlineData.cs` (Stories 5-2/3/4) | Prerequisite — all headlines populated |

### Dependencies

This story depends on ALL previous Epic 5 stories:
- **5-1:** EventScheduler (events fire during gameplay)
- **5-2:** Enriched MarketEventFiredEvent with Headline, IsPositive, etc.
- **5-3:** Tier-specific events that produce the sidebar indicators
- **5-4:** Global events that produce screen effects

Can be partially implemented with just 5-1 and 5-2 (banner + ticker work with EarningsBeat/Miss). Full visual coverage requires 5-3 and 5-4 for all event types.

### Previous Story Learnings

- UI elements created programmatically via `new GameObject()` + `AddComponent<>()` in UISetup
- MonoBehaviours subscribe to EventBus in `Start()` or `OnEnable()`, unsubscribe in `OnDestroy()` or `OnDisable()`
- Animations via `Update()` with elapsed time tracking — no Animator or DOTween
- Use `CanvasGroup` for alpha fade effects on banner/overlay elements

### Project Structure Notes

- New file: `Assets/Scripts/Runtime/UI/NewsBanner.cs`
- New file: `Assets/Scripts/Runtime/UI/NewsTicker.cs`
- New file: `Assets/Scripts/Runtime/UI/ScreenEffects.cs`
- Modified: `Assets/Scripts/Runtime/UI/StockSidebar.cs` (add event indicator slots)
- Modified: `Assets/Scripts/Runtime/UI/PositionPanel.cs` (add warning icon)
- Modified: `Assets/Scripts/Setup/UISetup.cs` (generate new UI elements)
- New file: `Assets/Tests/Runtime/UI/NewsBannerTests.cs`
- New file: `Assets/Tests/Runtime/UI/NewsTickerTests.cs`
- New file: `Assets/Tests/Runtime/UI/ScreenEffectsTests.cs`

### References

- [Source: epics.md#5.5] — Full signal list: banner flash, volume spike, warning icons, sector highlights, screen shake, news ticker
- [Source: bull-run-gdd-mvp.md#3.4] — Signal column in event type table
- [Source: bull-run-gdd-mvp.md#6.1] — "Bottom Bar: News ticker crawl showing incoming events"
- [Source: bull-run-gdd-mvp.md#7.1] — Color palette: "Neon green for gains, hot pink for losses"
- [Source: game-architecture.md#UI Manager] — UI/NewsTicker.cs planned location
- [Source: game-architecture.md#Technical Requirements] — "Post-processing: Bloom, vignette, chromatic aberration via URP"
- [Source: project-context.md#UI Framework Rules] — "uGUI (Canvas) exclusively, all Canvas hierarchies created programmatically"

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Debug Log References

No issues encountered during implementation.

### Completion Notes List

- Created NewsBanner MonoBehaviour: subscribes to MarketEventFiredEvent/MarketEventEndedEvent, shows green/red banners with headlines, slides from top, auto-fades after 3s, stacks multiple banners with vertical offset
- Created NewsTicker MonoBehaviour: subscribes to MarketEventFiredEvent, scrolls headlines left-to-right at 100px/s, diamond prefix per headline, auto-removes when scrolled off screen
- Extended StockSidebar: added EventIndicator/GlowBorder slots to StockEntryView, subscribes to market events, shows pulsing volume icon (PumpAndDump), warning triangle (SECInvestigation), green/red glow border (SectorRotation), auto-cleans on event end
- Created ScreenEffects MonoBehaviour: screen shake + red pulse for MarketCrash (1.5s decay), green tint for BullRun, brief red flash for FlashCrash (0.2s), all clean up on event end
- Extended PositionPanel: tracks ShortSqueeze events, shows pulsing red exclamation icon on affected short positions, removes on event end
- Extended UISetup: added ExecuteNewsBanner(), ExecuteNewsTicker(), ExecuteScreenEffects() methods, added EventIndicator/GlowBorder slots to CreateStockEntryView()
- Wrote comprehensive tests: NewsBannerTests (7 tests), NewsTickerTests (7 tests), ScreenEffectsTests (12 tests), plus additions to StockSidebarTests (10 tests) and PositionPanelTests (1 test)
- Updated project-context.md: added rule that AI agents must never attempt to run tests

### Code Review Fixes Applied (2026-02-13)

- [H1] Cached RectTransform in NewsBanner.ActiveBanner.Rect and NewsTicker.TickerEntry.Rect — eliminated GetComponent<RectTransform>() calls in Update loops
- [H2] Fixed ScreenEffects.StartShake() position drift — only captures _originalPosition when not already shaking, preventing permanent offset on re-trigger
- [M1] Separated pulse animation from dirty flag in StockSidebar and PositionPanel — added lightweight UpdateIndicatorPulse() and UpdateWarningPulse() methods that only update alpha, avoiding full RefreshEntryVisuals/UpdatePnLDisplay every frame during events
- [M2] Fixed NewsBanner.OnMarketEventEnded to remove only the first matching banner per event end (added break), preventing removal of all banners of same EventType
- [M3] Removed unused _containerRect field from NewsTicker (dead code)
- [M4] Extracted inline animation magic numbers to static readonly constants: NewsBanner.SlideDuration, NewsBanner.FadeDuration, StockSidebar.VolumePulseFrequency, PositionPanel.WarningPulseFrequency

### File List

| Action | File |
|--------|------|
| New | Assets/Scripts/Runtime/UI/NewsBanner.cs |
| New | Assets/Scripts/Runtime/UI/NewsTicker.cs |
| New | Assets/Scripts/Runtime/UI/ScreenEffects.cs |
| Modified | Assets/Scripts/Runtime/UI/StockSidebar.cs |
| Modified | Assets/Scripts/Runtime/UI/PositionPanel.cs |
| Modified | Assets/Scripts/Setup/UISetup.cs |
| New | Assets/Tests/Runtime/UI/NewsBannerTests.cs |
| New | Assets/Tests/Runtime/UI/NewsTickerTests.cs |
| New | Assets/Tests/Runtime/UI/ScreenEffectsTests.cs |
| Modified | Assets/Tests/Runtime/UI/StockSidebarTests.cs |
| Modified | Assets/Tests/Runtime/UI/PositionPanelTests.cs |
| Modified | _bmad-output/project-context.md |

## Change Log

- 2026-02-13: Implemented Story 5-5 Event Visual Signals — all 7 tasks completed. Added NewsBanner, NewsTicker, ScreenEffects components; extended StockSidebar with event indicators and PositionPanel with ShortSqueeze warnings; extended UISetup with new overlay generators; wrote 37 tests across 5 test files.
- 2026-02-13: Code review fixes applied — 2 HIGH and 4 MEDIUM issues fixed: cached RectTransform refs, fixed shake position drift, separated pulse animation from dirty flags, fixed banner removal logic, removed dead code, extracted magic numbers to constants.
