# Story 16.2: Pause Menu (ESC Popup)

Status: ready-for-dev

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

- [ ] Task 1: Build pause menu overlay UI in UISetup (AC: 1, 3, 4, 5, 13, 14)
  - [ ] Add `ExecutePauseMenuUI()` method to UISetup
  - [ ] Create PauseMenuCanvas (ScreenSpaceOverlay, sortingOrder=155)
  - [ ] Full-screen background panel using ColorPalette.Background at 80% alpha (semi-transparent)
  - [ ] "PAUSED" header text in ColorPalette.White, large font (40-48px), centered
  - [ ] Four buttons in VerticalLayoutGroup: CONTINUE (Green), SETTINGS (Cyan), RETURN TO MENU (Amber), EXIT (Red)
  - [ ] Apply CRTThemeData styling, Border outlines, GameFeel button helpers
  - [ ] Confirmation popup sub-panel (initially hidden): "Abandon current run?" with YES (Red) and NO (Green) buttons
  - [ ] Store references in PauseMenuReferences struct
  - [ ] Panel starts hidden (SetActive false)
  - [ ] File: `Scripts/Setup/UISetup.cs`

- [ ] Task 2: Create PauseMenuUI controller (AC: 1, 2, 6, 7, 8, 9, 10, 11, 12, 15, 16, 17, 18, 19)
  - [ ] Create `Scripts/Runtime/UI/PauseMenuUI.cs` MonoBehaviour
  - [ ] `Initialize()` wires button click listeners and stores references
  - [ ] `bool IsPaused` property tracking pause state
  - [ ] `TogglePause()` called on ESC: if paused → resume, if not paused → pause
  - [ ] `Pause()`: set `Time.timeScale = 0`, show pause panel, duck music volume to 50%, publish `GamePausedEvent`
  - [ ] `Resume()`: set `Time.timeScale = 1`, hide pause panel, restore music volume, publish `GameResumedEvent`
  - [ ] `OnContinue()`: calls Resume()
  - [ ] `OnSettings()`: show shared settings panel (from Story 16.1), set flag so ESC closes settings first
  - [ ] `OnReturnToMenu()`: show confirmation popup
  - [ ] `OnConfirmReturnToMenu()`: Resume (restore timeScale), publish `ReturnToMenuEvent`, transition to MainMenuState
  - [ ] `OnCancelReturnToMenu()`: hide confirmation popup, return to pause buttons
  - [ ] `OnExit()`: `Application.Quit()` / `EditorApplication.isPlaying = false`
  - [ ] ESC key handling: check if settings panel is open → close settings first; check if confirmation popup is open → dismiss confirmation; otherwise toggle pause
  - [ ] File: `Scripts/Runtime/UI/PauseMenuUI.cs`

- [ ] Task 3: Wire ESC input handling in GameRunner (AC: 1, 7, 9, 15, 19)
  - [ ] In GameRunner.Update(), check for `Keyboard.current.escapeKey.wasPressedThisFrame`
  - [ ] If current state is MainMenuState → ignore ESC
  - [ ] Otherwise → call PauseMenuUI.HandleEscapePressed()
  - [ ] PauseMenuUI.HandleEscapePressed() handles the layered ESC logic (settings open → close settings, confirmation open → close confirmation, otherwise → toggle pause)
  - [ ] File: `Scripts/Runtime/Core/GameRunner.cs`

- [ ] Task 4: Handle return-to-menu game state cleanup (AC: 11)
  - [ ] Subscribe to `ReturnToMenuEvent` in GameRunner
  - [ ] On ReturnToMenuEvent: reset short state machines, clear post-trade cooldown, unsubscribe round-specific events
  - [ ] Cancel any active coroutines (shop animations, trade feedback, etc.)
  - [ ] Hide all gameplay overlays (ShopUI, MarketOpenUI, RoundResultsUI, RunSummaryUI, TierTransitionUI, EventPopup)
  - [ ] Transition GameStateMachine to MainMenuState
  - [ ] MainMenuState.Enter() will show main menu and play title music (from Story 16.1)
  - [ ] File: `Scripts/Runtime/Core/GameRunner.cs`

- [ ] Task 5: Add pause-related events to GameEvents (AC: 2, 6, 11, 17, 18)
  - [ ] Add `GamePausedEvent` struct to GameEvents.cs
  - [ ] Add `GameResumedEvent` struct to GameEvents.cs
  - [ ] Add `ReturnToMenuEvent` struct to GameEvents.cs
  - [ ] MusicManager subscribes to GamePausedEvent (duck volume) and GameResumedEvent (restore volume)
  - [ ] File: `Scripts/Runtime/Core/GameEvents.cs`, `Scripts/Runtime/Audio/MusicManager.cs`

- [ ] Task 6: Wire PauseMenuUI creation in GameRunner.Start (AC: 1)
  - [ ] Call `UISetup.ExecutePauseMenuUI()` in GameRunner.Start()
  - [ ] Create PauseMenuUI MonoBehaviour and call Initialize() with references
  - [ ] Store PauseMenuUI reference in GameRunner for ESC input forwarding
  - [ ] File: `Scripts/Runtime/Core/GameRunner.cs`, `Scripts/Setup/UISetup.cs`

- [ ] Task 7: Write tests (All AC)
  - [ ] PauseMenuUI: TogglePause sets timeScale to 0, second toggle restores to 1
  - [ ] PauseMenuUI: ESC while settings open closes settings, not pause menu
  - [ ] PauseMenuUI: ESC while confirmation open dismisses confirmation
  - [ ] PauseMenuUI: ReturnToMenu publishes ReturnToMenuEvent
  - [ ] PauseMenuUI: ESC ignored during MainMenuState
  - [ ] GamePausedEvent published on pause, GameResumedEvent on resume
  - [ ] MusicManager ducks volume on GamePausedEvent, restores on GameResumedEvent
  - [ ] Time.timeScale is 1 after ReturnToMenu (not stuck at 0)
  - [ ] Files: `Tests/Runtime/UI/PauseMenuTests.cs`

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

### Debug Log References

### Completion Notes List

### File List
