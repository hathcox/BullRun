# Story FIX-4: Event Pop-Up Display with Pause & Directional Fly

Status: ready-for-dev

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

- [ ] Task 1: Fix event display initialization — CRITICAL BUG (AC: 1)
  - [ ] Add `UISetup.ExecuteNewsBanner()` call to `GameRunner.Start()`
  - [ ] Add `UISetup.ExecuteNewsTicker()` call to `GameRunner.Start()`
  - [ ] Add `UISetup.ExecuteScreenEffects()` call to `GameRunner.Start()`
  - [ ] Place these calls AFTER the existing UI setup calls but BEFORE the game state machine starts
  - [ ] Verify that NewsBanner.Initialize() subscribes to MarketEventFiredEvent
  - [ ] Verify that NewsTicker.Initialize() subscribes to MarketEventFiredEvent
  - [ ] This alone should restore banners, ticker scrolling, and screen effects (shake, flash, tint)
  - [ ] File: `Scripts/Runtime/Core/GameRunner.cs`

- [ ] Task 2: Create EventPopup UI component (AC: 2, 7, 8)
  - [ ] Create a large center-screen panel (semi-transparent background, ~60% screen width)
  - [ ] Layout: Large directional arrow (up or down) + headline text + affected ticker(s)
  - [ ] Positive: green arrow pointing UP ("▲"), green accent color, green tinted background
  - [ ] Negative: red arrow pointing DOWN ("▼"), red accent color, red tinted background
  - [ ] Font: Large, bold headline text (24-28pt), ticker symbols below (18pt)
  - [ ] Arrow indicators at 48pt+ size
  - [ ] Panel starts at screen center, hidden by default (gameObject.SetActive(false))
  - [ ] Use a dedicated Canvas or high sort order so popup renders above all other UI
  - [ ] Files: `Scripts/Setup/UISetup.cs` (create UI elements), `Scripts/Runtime/UI/EventPopup.cs` (new MonoBehaviour)

- [ ] Task 3: Implement pause-on-event (AC: 3, 6)
  - [ ] Subscribe to `MarketEventFiredEvent` on EventBus
  - [ ] On event: populate popup with headline/ticker/arrow, show popup, set `Time.timeScale = 0f`
  - [ ] Use `WaitForSecondsRealtime(1.2f)` (unscaled time) for the read duration
  - [ ] After read duration: trigger fly animation, then restore `Time.timeScale = 1f` after animation completes
  - [ ] IMPORTANT: Use unscaled time (`Time.unscaledDeltaTime`) for all popup animations since timeScale is 0
  - [ ] File: `Scripts/Runtime/UI/EventPopup.cs`

- [ ] Task 4: Implement directional fly animation (AC: 4, 5)
  - [ ] After pause duration, animate the popup:
    - **Positive (IsPositive=true):** Fly UPWARD — translate Y from center to off-screen top over ~0.4s, scale up slightly (1.0 → 1.2), fade alpha to 0
    - **Negative (IsPositive=false):** Fly DOWNWARD — translate Y from center to off-screen bottom over ~0.4s, scale up slightly, fade alpha to 0
  - [ ] Use easing: start slow, accelerate out (ease-in curve)
  - [ ] All animation uses `Time.unscaledDeltaTime` since timeScale is 0
  - [ ] After animation completes: hide popup, restore timeScale, fire completion callback
  - [ ] File: `Scripts/Runtime/UI/EventPopup.cs`

- [ ] Task 5: Implement event queuing (AC: 9)
  - [ ] If a new `MarketEventFiredEvent` fires while a popup is active (paused or animating), queue it
  - [ ] Queue is a simple `Queue<MarketEventFiredEvent>`
  - [ ] After current popup completes, check queue — if non-empty, show next event
  - [ ] If queue has multiple events, reduce pause duration for subsequent events (0.8s instead of 1.2s) to avoid too much total pause time
  - [ ] File: `Scripts/Runtime/UI/EventPopup.cs`

- [ ] Task 6: Integrate with existing event display systems (AC: 1, 2)
  - [ ] NewsBanner and NewsTicker continue to function — they provide persistent reference after the popup flies away
  - [ ] EventPopup is an ADDITIONAL display layer on top of the existing systems
  - [ ] ScreenEffects (shake, flash, tint) should activate AFTER the popup flies away and timeScale resumes
  - [ ] Ensure all three systems coexist without conflicts
  - [ ] File: `Scripts/Runtime/UI/EventPopup.cs`

- [ ] Task 7: Wire EventPopup initialization in GameRunner (AC: 2)
  - [ ] Add `UISetup.ExecuteEventPopup()` call to `GameRunner.Start()` alongside the other new init calls from Task 1
  - [ ] File: `Scripts/Runtime/Core/GameRunner.cs`, `Scripts/Setup/UISetup.cs`

- [ ] Task 8: Write tests (AC: all)
  - [ ] Test: NewsBanner receives MarketEventFiredEvent after initialization fix
  - [ ] Test: NewsTicker receives MarketEventFiredEvent after initialization fix
  - [ ] Test: EventPopup appears on MarketEventFiredEvent
  - [ ] Test: positive event shows up arrow + green styling
  - [ ] Test: negative event shows down arrow + red styling
  - [ ] Test: headline text matches event headline
  - [ ] Test: timeScale set to 0 during popup display
  - [ ] Test: timeScale restored to 1 after animation
  - [ ] Test: multiple events queue properly (second event shows after first completes)
  - [ ] Files: `Tests/Runtime/UI/EventPopupTests.cs`, `Tests/Runtime/UI/NewsBannerTests.cs` (if not existing)

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
