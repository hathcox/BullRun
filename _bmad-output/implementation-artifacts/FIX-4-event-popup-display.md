# Story FIX-4: Event Pop-Up Display with Pause & Directional Fly

Status: done

## Story

As a player,
I want market events to dramatically pop up on screen with a headline I can read, briefly pause the action, and then fly in the direction the stock will move (up or down),
so that events feel impactful and I can react to them.

## Problem Analysis

### CRITICAL BUG: Events Don't Display At All

The entire event visual chain is broken. NewsBanner, NewsTicker, and ScreenEffects are **never instantiated**. The setup methods exist in `UISetup.cs` and the component code is correct, but `GameRunner.Start()` never calls them:

**Missing from GameRunner.Start():**
```csharp
// These three calls are MISSING:
UISetup.ExecuteNewsBanner();
UISetup.ExecuteNewsTicker();
UISetup.ExecuteScreenEffects();
```

**The chain breakdown:**
- EventScheduler schedules events → WORKING
- EventScheduler fires events via EventEffects.StartEvent() → WORKING
- EventEffects publishes MarketEventFiredEvent to EventBus → WORKING
- **BREAK:** No NewsBanner/NewsTicker instances exist → no subscribers on EventBus
- Events fire silently with zero visual feedback

**Affected Code:**
- `Scripts/Runtime/Core/GameRunner.cs` — `Start()` method, missing three initialization calls
- `Scripts/Setup/UISetup.cs` — `ExecuteNewsBanner()` (line ~740), `ExecuteNewsTicker()` (line ~779), `ExecuteScreenEffects()` (line ~833) all exist and are correctly implemented

### Enhancement: Dramatic Pop-Up with Pause & Fly

Beyond fixing the initialization bug, events need to DEMAND attention with a center-screen popup, brief pause, and directional fly animation.

## Acceptance Criteria

1. **NewsBanner, NewsTicker, and ScreenEffects are instantiated and functional** (existing code, just needs init calls)
2. When a market event fires, a large pop-up appears center-screen with the event headline
3. The game briefly pauses (Time.timeScale = 0) for ~1.0-1.5 seconds so the player can read the headline
4. After the pause, the pop-up animates: flies UPWARD off-screen if positive event, flies DOWNWARD off-screen if negative event
5. The fly animation is fast, dramatic, and communicates direction unmistakably (large motion, scaling effect)
6. After the fly animation completes, normal gameplay resumes (Time.timeScale = 1)
7. The pop-up shows: event headline text, affected ticker symbol(s), and a directional arrow indicator (up/down)
8. Color coding: green background/accent for positive, red for negative (consistent with existing system)
9. Multiple rapid events don't stack pauses — if a second event fires while the first is animating, it queues and shows after

## Tasks / Subtasks

- [x] Task 1: Fix event display initialization — CRITICAL BUG (AC: 1)
  - [x] Add `UISetup.ExecuteNewsBanner()` call to `GameRunner.Start()`
  - [x] Add `UISetup.ExecuteNewsTicker()` call to `GameRunner.Start()`
  - [x] Add `UISetup.ExecuteScreenEffects()` call to `GameRunner.Start()`
  - [x] Place these calls AFTER the existing UI setup calls but BEFORE the game state machine starts
  - [x] Verify that NewsBanner.Initialize() subscribes to MarketEventFiredEvent
  - [x] Verify that NewsTicker.Initialize() subscribes to MarketEventFiredEvent
  - [x] This alone should restore banners, ticker scrolling, and screen effects (shake, flash, tint)
  - [x] File: `Scripts/Runtime/Core/GameRunner.cs`

- [x] Task 2: Create EventPopup UI component (AC: 2, 7, 8)
  - [x] Create a large center-screen panel (semi-transparent background, ~60% screen width)
  - [x] Layout: Large directional arrow (up or down) + headline text + affected ticker(s)
  - [x] Positive: green arrow pointing UP ("▲"), green accent color, green tinted background
  - [x] Negative: red arrow pointing DOWN ("▼"), red accent color, red tinted background
  - [x] Font: Large, bold headline text (24-28pt), ticker symbols below (18pt)
  - [x] Arrow indicators at 48pt+ size
  - [x] Panel starts at screen center, hidden by default (gameObject.SetActive(false))
  - [x] Use a dedicated Canvas or high sort order so popup renders above all other UI
  - [x] Files: `Scripts/Setup/UISetup.cs` (create UI elements), `Scripts/Runtime/UI/EventPopup.cs` (new MonoBehaviour)

- [x] Task 3: Implement pause-on-event (AC: 3, 6)
  - [x] Subscribe to `MarketEventFiredEvent` on EventBus
  - [x] On event: populate popup with headline/ticker/arrow, show popup, set `Time.timeScale = 0f`
  - [x] Use `WaitForSecondsRealtime(1.2f)` (unscaled time) for the read duration
  - [x] After read duration: trigger fly animation, then restore `Time.timeScale = 1f` after animation completes
  - [x] IMPORTANT: Use unscaled time (`Time.unscaledDeltaTime`) for all popup animations since timeScale is 0
  - [x] File: `Scripts/Runtime/UI/EventPopup.cs`

- [x] Task 4: Implement directional fly animation (AC: 4, 5)
  - [x] After pause duration, animate the popup:
    - **Positive (IsPositive=true):** Fly UPWARD — translate Y from center to off-screen top over ~0.4s, scale up slightly (1.0 → 1.2), fade alpha to 0
    - **Negative (IsPositive=false):** Fly DOWNWARD — translate Y from center to off-screen bottom over ~0.4s, scale up slightly, fade alpha to 0
  - [x] Use easing: start slow, accelerate out (ease-in curve)
  - [x] All animation uses `Time.unscaledDeltaTime` since timeScale is 0
  - [x] After animation completes: hide popup, restore timeScale, fire completion callback
  - [x] File: `Scripts/Runtime/UI/EventPopup.cs`

- [x] Task 5: Implement event queuing (AC: 9)
  - [x] If a new `MarketEventFiredEvent` fires while a popup is active (paused or animating), queue it
  - [x] Queue is a simple `Queue<MarketEventFiredEvent>`
  - [x] After current popup completes, check queue — if non-empty, show next event
  - [x] If queue has multiple events, reduce pause duration for subsequent events (0.8s instead of 1.2s) to avoid too much total pause time
  - [x] File: `Scripts/Runtime/UI/EventPopup.cs`

- [x] Task 6: Integrate with existing event display systems (AC: 1, 2)
  - [x] NewsBanner and NewsTicker continue to function — they provide persistent reference after the popup flies away
  - [x] EventPopup is an ADDITIONAL display layer on top of the existing systems
  - [x] ScreenEffects (shake, flash, tint) should activate AFTER the popup flies away and timeScale resumes
  - [x] Ensure all three systems coexist without conflicts
  - [x] File: `Scripts/Runtime/UI/EventPopup.cs`

- [x] Task 7: Wire EventPopup initialization in GameRunner (AC: 2)
  - [x] Add `UISetup.ExecuteEventPopup()` call to `GameRunner.Start()` alongside the other new init calls from Task 1
  - [x] File: `Scripts/Runtime/Core/GameRunner.cs`, `Scripts/Setup/UISetup.cs`

- [x] Task 8: Write tests (AC: all)
  - [x] Test: NewsBanner receives MarketEventFiredEvent after initialization fix
  - [x] Test: NewsTicker receives MarketEventFiredEvent after initialization fix
  - [x] Test: EventPopup appears on MarketEventFiredEvent
  - [x] Test: positive event shows up arrow + green styling
  - [x] Test: negative event shows down arrow + red styling
  - [x] Test: headline text matches event headline
  - [x] Test: timeScale set to 0 during popup display
  - [x] Test: timeScale restored to 1 after animation
  - [x] Test: multiple events queue properly (second event shows after first completes)
  - [x] Files: `Tests/Runtime/UI/EventPopupTests.cs`, `Tests/Runtime/UI/NewsBannerTests.cs` (if not existing)

## Dev Notes

### Architecture Compliance
- **Setup-Oriented Generation:** All UI elements created in UISetup during F5
- **uGUI Canvas:** All elements built programmatically. EventPopup uses a separate overlay Canvas with higher sort order
- **EventBus:** Subscribe to existing `MarketEventFiredEvent` — no new events needed for triggering
- **No direct system references:** EventPopup subscribes via EventBus, doesn't reference EventScheduler or EventEffects directly

### Task 1 is a Critical Bug Fix
Task 1 (adding the three missing init calls) is the highest priority item in this entire story. It should be done FIRST and verified independently. The existing NewsBanner/NewsTicker/ScreenEffects code is complete and tested — they just need to be instantiated. This single fix restores:
- News banners sliding from top with headlines
- News ticker scrolling at bottom
- Screen shake on MarketCrash
- Red pulse on MarketCrash
- Green tint on BullRun events
- Red flash on FlashCrash

### Time.timeScale Considerations
- Setting `Time.timeScale = 0` freezes ALL gameplay: price engine, chart rendering, round timer, trading input
- This is INTENTIONAL — the dramatic pause is the whole point
- All popup animations MUST use `Time.unscaledDeltaTime` and `WaitForSecondsRealtime`
- Be careful: any coroutines using `WaitForSeconds` (scaled) will freeze during the pause
- The round timer should NOT count down during the pause (Time.deltaTime will be 0, which is correct)

### Visual Design Direction
- Think "breaking news alert" — this should feel like CNBC's breaking news banner slamming onto screen
- The directional fly should be visceral — not subtle. The whole panel LAUNCHES up or down
- Consider adding a brief screen flash (green/red) at the moment of popup appearance for extra impact
- Sound effects would be ideal but are deferred to Epic 11 — the animation alone must communicate urgency

### Event Data Available
From `MarketEventFiredEvent`:
- `EventType` — type enum (EarningsBeat, MarketCrash, etc.)
- `Headline` — generated text with ticker substitution already done
- `AffectedTickerSymbols` — string array of affected tickers
- `IsPositive` — boolean for direction
- `PriceEffectPercent` — magnitude (could use to scale animation intensity)
- `Duration` — how long the event lasts

### Interaction with Existing Systems
- **NewsBanner:** Restored by Task 1. Continues providing persistent top-of-screen reminders after popup flies away
- **NewsTicker:** Restored by Task 1. Continues scrolling headline reference at bottom
- **ScreenEffects:** Restored by Task 1. MarketCrash shake, BullRun tint, FlashCrash flash all activate after popup
- **StockSidebar indicators:** PumpAndDump volume bars, SEC warning icons still appear on their own (these work independently via separate EventBus subscriptions in StockSidebar)

### References
- `Scripts/Runtime/Core/GameRunner.cs` Start() method (missing init calls)
- `Scripts/Setup/UISetup.cs` line ~740 ExecuteNewsBanner(), ~779 ExecuteNewsTicker(), ~833 ExecuteScreenEffects()
- `Scripts/Runtime/UI/NewsBanner.cs` (existing, works, just not instantiated)
- `Scripts/Runtime/UI/NewsTicker.cs` (existing, works, just not instantiated)
- `Scripts/Runtime/UI/ScreenEffects.cs` (existing, works, just not instantiated)
- `Scripts/Runtime/Events/EventEffects.cs` (where MarketEventFiredEvent is published)
- `Scripts/Runtime/Core/GameEvents.cs` lines 22-31 (MarketEventFiredEvent structure)
- `Scripts/Setup/Data/EventHeadlineData.cs` (headline templates)

## Dev Agent Record

### Implementation Notes
- **Task 1 (Critical Bug Fix):** Added three missing `UISetup.Execute*()` calls to `GameRunner.Start()` — `ExecuteNewsBanner()`, `ExecuteNewsTicker()`, `ExecuteScreenEffects()`. Placed after existing UI setup calls and before overlay/state-transition UIs. This restores all existing event visual feedback (banners, ticker scrolling, screen shake/pulse/tint/flash).
- **Tasks 2-7 (EventPopup):** Created new `EventPopup` MonoBehaviour and `UISetup.ExecuteEventPopup()`. The popup uses a dedicated overlay Canvas (sortingOrder 50) with a ~60% width panel containing a large directional arrow (48pt), bold headline (26pt), and affected tickers (18pt). Subscribes to `MarketEventFiredEvent` via EventBus. On event: pauses game (`Time.timeScale = 0`), waits 1.2s (unscaled), then flies popup up/down with ease-in curve, scale-up (1.0→1.2), and alpha fade over 0.4s. All animation uses `Time.unscaledDeltaTime` and `WaitForSecondsRealtime`. Event queuing via `Queue<MarketEventFiredEvent>` with reduced 0.8s pause for queued events. TimeScale safely restored on completion and in `OnDestroy()`.
- **Task 8 (Tests):** 22 tests in `EventPopupTests.cs` covering: direction arrows, popup colors, color values, configuration constants, activation on event, timeScale pausing, headline display, positive/negative styling, ticker display, global events, event queuing, empty/null headline handling, OnDestroy timeScale restoration, and EventPopupCompletedEvent publication. Existing `NewsBannerTests.cs` already covers NewsBanner event reception (7 tests including event-driven creation). `NewsTickerTests.cs` covers ticker event reception (8 tests including event-driven creation).
- **Architecture Compliance:** All patterns followed — programmatic uGUI, EventBus subscription, setup-oriented generation, no Inspector configuration, no direct system references, no `UnityEditor` in Runtime.

### Completion Notes
All 8 tasks and all subtasks completed. Implementation satisfies all 9 acceptance criteria:
1. NewsBanner/NewsTicker/ScreenEffects instantiated via init calls in GameRunner.Start()
2. Large center-screen popup appears on MarketEventFiredEvent
3. Game pauses (Time.timeScale = 0) for 1.2s read duration
4. Positive events fly UPWARD, negative events fly DOWNWARD
5. Dramatic fly animation with 1200px distance, scale-up, ease-in curve
6. Time.timeScale restored to saved value after animation completes
7. Popup shows headline text, affected tickers, and directional arrow (▲/▼)
8. Green background/accent for positive, red for negative
9. Queue system prevents stacked pauses; queued events use reduced 0.8s pause

## File List

- `Assets/Scripts/Runtime/Core/GameRunner.cs` — Modified: added 4 init calls (NewsBanner, NewsTicker, ScreenEffects, EventPopup)
- `Assets/Scripts/Runtime/Core/GameEvents.cs` — Modified: added EventPopupCompletedEvent struct
- `Assets/Scripts/Runtime/UI/EventPopup.cs` — New: dramatic event popup MonoBehaviour with pause, fly animation, queuing, and completion event
- `Assets/Scripts/Runtime/UI/ScreenEffects.cs` — Modified: subscribes to EventPopupCompletedEvent instead of MarketEventFiredEvent
- `Assets/Scripts/Setup/UISetup.cs` — Modified: added ExecuteEventPopup() method
- `Assets/Tests/Runtime/UI/EventPopupTests.cs` — New: 22 unit tests for EventPopup
- `Assets/Tests/Runtime/UI/ScreenEffectsTests.cs` — Modified: updated activation tests for EventPopupCompletedEvent
- `Assets/_Generated/Scenes/MainScene.unity` — Auto-generated scene update
- `ProjectSettings/TimeManager.asset` — Unity editor serialization format migration (no functional change)

## Senior Developer Review (AI)

**Reviewer:** Iggy on 2026-02-13

### Findings Fixed (Code Review)
- **H1 — ScreenEffects timing (Task 6 AC violation):** ScreenEffects subscribed to `MarketEventFiredEvent` directly, causing effects to activate during popup pause instead of after. Fixed by introducing `EventPopupCompletedEvent` — EventPopup publishes it after animation completes (and for skipped popups). ScreenEffects now subscribes to `EventPopupCompletedEvent` so effects start AFTER the popup flies away and timeScale resumes.
- **H2 — Test count mismatch:** Story claimed "15 tests" but file had 20. Added 2 more tests (OnDestroy timeScale restoration, EventPopupCompletedEvent publication). Updated doc to reflect 22 tests.
- **H3/M4 — Undocumented file changes:** `ProjectSettings/TimeManager.asset`, `MainScene.unity`, `GameEvents.cs`, `ScreenEffects.cs`, `ScreenEffectsTests.cs` added to File List.
- **M1 — Missing timeScale restoration test:** Added `EventPopup_OnDestroy_RestoresTimeScale` test verifying the safety net.
- **M3 — Dead `_isFirstEvent` field:** Removed unused member variable from `EventPopup.cs`.

### Remaining (Low, Accepted)
- L1: `static readonly` for primitive constants — acceptable, no functional impact
- L2: No coroutine cancellation on state transition — edge case, OnDestroy safety net covers it
- L3: Test suite weighted toward static/constant assertions — adequate for current scope

## Change Log

- 2026-02-13: Fixed critical bug — NewsBanner, NewsTicker, and ScreenEffects were never instantiated (missing init calls in GameRunner.Start()). Added new EventPopup system for dramatic center-screen event display with pause-and-fly animation.
- 2026-02-13: Code review fixes — Added EventPopupCompletedEvent so ScreenEffects activates after popup (not during pause). Removed dead _isFirstEvent field. Added 2 new tests. Updated File List and test documentation.
