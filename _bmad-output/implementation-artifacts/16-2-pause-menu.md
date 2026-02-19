# Story 16.2: Pause Menu (ESC Popup)

Status: done

## Story

As a player,
I want to press ESC during gameplay to open a pause menu with options to continue, open settings, return to the main menu, or exit,
so that I can pause the action, adjust settings mid-game, or leave a run without losing control.

## Acceptance Criteria

1. Pressing ESC during any gameplay state (MarketOpen, Trading, MarketClose, Shop, TierTransition, RunSummary) opens a pause menu overlay
2. When the pause menu opens, `Time.timeScale` is set to 0, freezing all gameplay (price updates, timers, animations)
3. The pause menu displays "PAUSED" as a header in ColorPalette.White, large font, centered
4. Four vertically-stacked buttons are displayed: "CONTINUE", "SETTINGS", "RETURN TO MENU", "EXIT"
5. Buttons follow the existing CRT theme styling (same as main menu buttons: CRTThemeData colors, Border outlines, hover/click feel)
6. Clicking "CONTINUE" closes the pause menu and resumes gameplay (`Time.timeScale = 1`)
7. Pressing ESC again while the pause menu is open also closes it and resumes gameplay (toggle behavior)
8. Clicking "SETTINGS" opens the shared settings panel (created in Story 16.1) — volume changes apply in real-time even while paused
9. When the settings panel is open from the pause menu, pressing ESC closes the settings panel first (returns to pause menu), not the pause menu itself
10. Clicking "RETURN TO MENU" shows a confirmation prompt: "Abandon current run?" with "YES" and "NO" buttons
11. Confirming "RETURN TO MENU" resets the game state: sets `Time.timeScale = 1`, clears the current RunContext, hides all gameplay UI, and transitions to MainMenuState
12. Clicking "EXIT" calls `Application.Quit()` in builds; in the editor, sets `EditorApplication.isPlaying = false` (same as main menu exit)
13. The pause menu overlay uses a semi-transparent dark background (ColorPalette.Background at ~80% opacity) so the frozen game is dimly visible behind it
14. The pause menu canvas uses sortingOrder=155 (above gameplay canvases, below SettingsCanvas at 160, below CRTOverlay at 200)
15. ESC is ignored while the main menu is visible (MainMenuState) — pause is only available during gameplay
16. Audio that plays during pause (UI SFX for button hover/click) uses `Time.unscaledDeltaTime` and the MMSoundManager UI track (which ignores timeScale)
17. Music continues playing while paused (music already uses unscaled time) but at reduced volume (duck to 50%) to signal paused state
18. When the pause menu closes (continue or ESC), music volume is restored to normal
19. ESC does not open the pause menu while the Unlocks "COMING SOON" popup is visible from the main menu (edge case guard)
20. All EventBus-driven gameplay systems (PriceGenerator, EventScheduler, timers) naturally freeze because they use `Time.deltaTime` which becomes 0 when timeScale=0

## Tasks / Subtasks

- [x] Task 1: Build pause menu overlay UI in UISetup (AC: 1, 3, 4, 5, 13, 14)
  - [x] Add `ExecutePauseMenuUI()` method to UISetup
  - [x] Create PauseMenuCanvas (ScreenSpaceOverlay, sortingOrder=155)
  - [x] Full-screen background panel using ColorPalette.Background at 80% alpha (semi-transparent)
  - [x] "PAUSED" header text in ColorPalette.White, large font (40-48px), centered
  - [x] Four buttons in VerticalLayoutGroup: CONTINUE (Green), SETTINGS (Cyan), RETURN TO MENU (Amber), EXIT (Red)
  - [x] Apply CRTThemeData styling, Border outlines, GameFeel button helpers
  - [x] Confirmation popup sub-panel (initially hidden): "Abandon current run?" with YES (Red) and NO (Green) buttons
  - [x] Store references in PauseMenuReferences struct
  - [x] Panel starts hidden (SetActive false)
  - [x] File: `Scripts/Setup/UISetup.cs`

- [x] Task 2: Create PauseMenuUI controller (AC: 1, 2, 6, 7, 8, 9, 10, 11, 12, 15, 16, 17, 18, 19)
  - [x] Create `Scripts/Runtime/UI/PauseMenuUI.cs` MonoBehaviour
  - [x] `Initialize()` wires button click listeners and stores references
  - [x] `bool IsPaused` property tracking pause state
  - [x] `TogglePause()` called on ESC: if paused → resume, if not paused → pause
  - [x] `Pause()`: set `Time.timeScale = 0`, show pause panel, duck music volume to 50%, publish `GamePausedEvent`
  - [x] `Resume()`: set `Time.timeScale = 1`, hide pause panel, restore music volume, publish `GameResumedEvent`
  - [x] `OnContinue()`: calls Resume()
  - [x] `OnSettings()`: show shared settings panel (from Story 16.1), set flag so ESC closes settings first
  - [x] `OnReturnToMenu()`: show confirmation popup
  - [x] `OnConfirmReturnToMenu()`: Resume (restore timeScale), publish `ReturnToMenuEvent`, transition to MainMenuState
  - [x] `OnCancelReturnToMenu()`: hide confirmation popup, return to pause buttons
  - [x] `OnExit()`: `Application.Quit()` / `EditorApplication.isPlaying = false`
  - [x] ESC key handling: check if settings panel is open → close settings first; check if confirmation popup is open → dismiss confirmation; otherwise toggle pause
  - [x] File: `Scripts/Runtime/UI/PauseMenuUI.cs`

- [x] Task 3: Wire ESC input handling in GameRunner (AC: 1, 7, 9, 15, 19)
  - [x] In GameRunner.Update(), check for `Keyboard.current.escapeKey.wasPressedThisFrame`
  - [x] If current state is MainMenuState → ignore ESC
  - [x] Otherwise → call PauseMenuUI.HandleEscapePressed()
  - [x] PauseMenuUI.HandleEscapePressed() handles the layered ESC logic (settings open → close settings, confirmation open → close confirmation, otherwise → toggle pause)
  - [x] File: `Scripts/Runtime/Core/GameRunner.cs`

- [x] Task 4: Handle return-to-menu game state cleanup (AC: 11)
  - [x] Subscribe to `ReturnToMenuEvent` in GameRunner
  - [x] On ReturnToMenuEvent: reset short state machines, clear post-trade cooldown, unsubscribe round-specific events
  - [x] Cancel any active coroutines (shop animations, trade feedback, etc.)
  - [x] Hide all gameplay overlays (ShopUI, MarketOpenUI, RoundResultsUI, RunSummaryUI, TierTransitionUI, EventPopup)
  - [x] Transition GameStateMachine to MainMenuState
  - [x] MainMenuState.Enter() will show main menu and play title music (from Story 16.1)
  - [x] File: `Scripts/Runtime/Core/GameRunner.cs`

- [x] Task 5: Add pause-related events to GameEvents (AC: 2, 6, 11, 17, 18)
  - [x] Add `GamePausedEvent` struct to GameEvents.cs
  - [x] Add `GameResumedEvent` struct to GameEvents.cs
  - [x] Add `ReturnToMenuEvent` struct to GameEvents.cs
  - [x] MusicManager subscribes to GamePausedEvent (duck volume) and GameResumedEvent (restore volume)
  - [x] File: `Scripts/Runtime/Core/GameEvents.cs`, `Scripts/Runtime/Audio/MusicManager.cs`

- [x] Task 6: Wire PauseMenuUI creation in GameRunner.Start (AC: 1)
  - [x] Call `UISetup.ExecutePauseMenuUI()` in GameRunner.Start()
  - [x] Create PauseMenuUI MonoBehaviour and call Initialize() with references
  - [x] Store PauseMenuUI reference in GameRunner for ESC input forwarding
  - [x] File: `Scripts/Runtime/Core/GameRunner.cs`, `Scripts/Setup/UISetup.cs`

- [x] Task 7: Write tests (All AC)
  - [x] PauseMenuUI: TogglePause sets timeScale to 0, second toggle restores to 1
  - [x] PauseMenuUI: ESC while settings open closes settings, not pause menu
  - [x] PauseMenuUI: ESC while confirmation open dismisses confirmation
  - [x] PauseMenuUI: ReturnToMenu publishes ReturnToMenuEvent
  - [x] PauseMenuUI: ESC ignored during MainMenuState
  - [x] GamePausedEvent published on pause, GameResumedEvent on resume
  - [x] MusicManager ducks volume on GamePausedEvent, restores on GameResumedEvent
  - [x] Time.timeScale is 1 after ReturnToMenu (not stuck at 0)
  - [x] Files: `Tests/Runtime/UI/PauseMenuTests.cs`

## Dev Notes

### Architecture Compliance

- **Programmatic uGUI:** All UI built in UISetup.cs, no prefabs. Follows the same patterns as MainMenuUI from Story 16.1.
- **EventBus communication:** GamePausedEvent, GameResumedEvent, ReturnToMenuEvent follow existing event patterns.
- **No DOTween:** Any animations use coroutines with Time.unscaledDeltaTime (critical — must use unscaled time since timeScale=0 during pause).
- **Input handling:** Uses `Keyboard.current.escapeKey.wasPressedThisFrame` from the New Input System, matching the existing HandleTradingInput pattern in GameRunner.

### Time.timeScale = 0 Implications

Setting timeScale to 0 automatically freezes:
- `Time.deltaTime` → returns 0 (all gameplay systems using deltaTime stop)
- Coroutines using `WaitForSeconds` pause (but `WaitForSecondsRealtime` continues)
- Physics (not used in this 2D game)
- Animator (not used — all animations are code-driven)

Things that **continue** during pause:
- `Time.unscaledDeltaTime` — used for pause menu animations
- MMSoundManager UI track — plays UI SFX during pause
- Music — already plays on unscaled time; we duck volume as a pause signal

### ESC Key Layering

ESC behavior follows a stack:
1. If settings panel is open → close settings → return to pause menu
2. If confirmation popup is open → dismiss confirmation → return to pause buttons
3. If pause menu is open → resume gameplay
4. If gameplay is active → open pause menu
5. If main menu is active → ignore ESC

### Return-to-Menu Cleanup

Returning to the main menu mid-run requires careful cleanup:
- Short state machines in GameRunner must be reset (not left in Holding/CashOutWindow)
- Post-trade cooldown must be cancelled
- All overlay UIs (Shop, MarketOpen, RoundResults, RunSummary, TierTransition, EventPopup) must be hidden
- The RunContext from the abandoned run is discarded; a new one is created on next START GAME
- Time.timeScale MUST be restored to 1 before transitioning (otherwise the game stays frozen)

### Shared Settings Panel

The settings panel and SettingsManager from Story 16.1 are reused directly. PauseMenuUI just calls Show/Hide on the existing settings panel references. No duplication of settings UI code.

### Canvas Sorting Order Map (Updated)

| Canvas | sortingOrder | Purpose |
|--------|-------------|---------|
| ChartCanvas | 10 | Price chart |
| ControlDeckCanvas | 20 | Bottom dashboard |
| TradeFeedbackCanvas | 23 | Trade popups |
| MarketOpenCanvas | 100 | Round preview |
| MainMenuCanvas | 150 | Main menu (Story 16.1) |
| **PauseMenuCanvas** | **155** | **Pause overlay (new)** |
| SettingsCanvas | 160 | Settings panel (Story 16.1) |
| CRTOverlayCanvas | 200 | Vignette/scanlines |

### Music Duck During Pause

MusicManager subscribes to GamePausedEvent/GameResumedEvent:
- On pause: fade current music volume to 50% of its current level over 0.3s (using unscaled time)
- On resume: restore to original volume over 0.3s (using unscaled time)
- This provides an audible signal that the game is paused without fully muting music

### Project Structure Notes

- New files:
  - `Scripts/Runtime/UI/PauseMenuUI.cs` (alongside MainMenuUI, ShopUI)
- Modified files:
  - `Scripts/Setup/UISetup.cs` (add ExecutePauseMenuUI)
  - `Scripts/Runtime/Core/GameRunner.cs` (ESC handling, return-to-menu cleanup)
  - `Scripts/Runtime/Core/GameEvents.cs` (new event structs)
  - `Scripts/Runtime/Audio/MusicManager.cs` (pause duck/restore)

### Dependencies

- **Blocked by Story 16.1:** Requires MainMenuState, SettingsManager, and settings panel UI to exist. The "SETTINGS" button opens the same panel, and "RETURN TO MENU" transitions to MainMenuState.

### References

- [Source: Assets/Scripts/Runtime/Core/GameRunner.cs] — Update loop, HandleTradingInput ESC pattern, short state machine cleanup
- [Source: Assets/Scripts/Runtime/Core/GameStateMachine.cs] — TransitionTo<T> pattern
- [Source: Assets/Scripts/Runtime/Audio/MusicManager.cs] — Volume ducking, title screen music
- [Source: Assets/Scripts/Setup/UISetup.cs] — Canvas creation, sorting orders
- [Source: Assets/Scripts/Setup/Data/ColorPalette.cs] — Background color, alpha helpers
- [Source: Assets/Scripts/Setup/Data/CRTThemeData.cs] — Button/panel styling
- [Source: _bmad-output/implementation-artifacts/16-1-main-menu-ui.md] — MainMenuState, SettingsManager, settings panel

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Debug Log References

- Test run: 1691 passed, 0 failed, 1 skipped (pre-existing). 13 new PauseMenu tests all pass.
- Code review test run: 1692 passed, 0 failed, 1 skipped. 14 PauseMenu tests (3 new, 2 consolidated into 1).

### Completion Notes List

- Implemented full ESC pause menu system with toggle behavior, settings panel layering, confirmation popup, and return-to-menu flow
- Created PauseMenuUI controller following MainMenuUI patterns — programmatic uGUI, EventBus communication, CRT theme styling
- Added 3 new events (GamePausedEvent, GameResumedEvent, ReturnToMenuEvent) to GameEvents.cs
- MusicManager subscribes to pause events — ducks all active music tracks to 50% on pause, restores on resume using 0.3s unscaled-time fades
- ESC key handling in GameRunner.Update() with guards for MainMenuState and ComingSoonPopup
- Return-to-menu cleanup: resets short state machines, cancels post-trade cooldown, hides all gameplay overlays, transitions to MainMenuState
- PauseMenuCanvas excluded from GameplayCanvases array so it's not hidden/shown during main menu transitions
- All button animations use SetUpdate(true) to work during timeScale=0

### Code Review Fixes (2026-02-19)

- **H1 Fixed:** Wired settings Back button in PauseMenuUI.Initialize() — syncs `_isSettingsOpen` flag when Back clicked (was causing double-ESC)
- **H2 Fixed:** Resume() now calls CloseSettings() if settings panel was open — prevents orphaned settings canvas
- **H3 Fixed:** MusicManager pause duck/restore now saves and restores per-track pre-pause volumes — preserves event-ducked volume levels across pause/resume
- **H4 Fixed:** Replaced fake MusicManager tests with honest EventBus infrastructure test; added SettingsBackButton sync test and Resume-closes-settings test
- **M1 Fixed:** MainMenuState guard test now verifies full IsActive lifecycle (Enter sets true, Exit sets false)
- **M2 Fixed:** HideAllGameplayOverlays now uses cached GameplayCanvases array instead of 6x FindFirstObjectByType scene searches

### File List

- `Assets/Scripts/Runtime/UI/PauseMenuUI.cs` (new)
- `Assets/Scripts/Runtime/UI/PauseMenuReferences.cs` (new)
- `Assets/Tests/Runtime/UI/PauseMenuTests.cs` (new)
- `Assets/Scripts/Setup/UISetup.cs` (modified — added ExecutePauseMenuUI, PauseRefs)
- `Assets/Scripts/Runtime/Core/GameRunner.cs` (modified — ESC handling, ReturnToMenu cleanup, PauseMenuUI wiring)
- `Assets/Scripts/Runtime/Core/GameEvents.cs` (modified — added GamePausedEvent, GameResumedEvent, ReturnToMenuEvent)
- `Assets/Scripts/Runtime/Audio/MusicManager.cs` (modified — pause duck/restore subscriptions and handlers)

## Change Log

- 2026-02-19: Story 16.2 implemented — ESC pause menu with continue, settings, return-to-menu, exit. Music ducking on pause. 13 tests added. All 1691 tests pass.
- 2026-02-19: Code review — Fixed 4 HIGH (settings Back button sync, Resume settings close, music duck restore, fake tests) and 2 MEDIUM (MainMenuState test, FindFirstObjectByType cleanup). 14 PauseMenu tests, all 1692 pass.
