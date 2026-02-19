# Epic 16: Main Menu & Pause System

**Description:** Add a main menu screen with start game, unlocks (placeholder), settings, and exit functionality. Add an ESC-triggered pause menu with continue, settings, return to menu, and exit. Includes a settings system for audio volumes and display options with PlayerPrefs persistence. Default SFX volume reduced to 80%.

**Phase:** Phase 3 (Polish & UX)

**Dependencies:** Requires existing systems from Epics 11 (Audio), 14 (CRT Theme), and 15 (Game Feel)

---

## Story 16.1: Main Menu UI

As a player, I want a main menu to appear when the game launches with options to start a game, view unlocks, open settings, or exit, so that I have a polished entry point into the game and can configure audio/display settings before playing.

**Status:** ready-for-dev

**Acceptance Criteria:**
- Full-screen CRT-themed main menu overlay on launch
- Buttons: START GAME, UNLOCKS (placeholder "COMING SOON"), SETTINGS, EXIT
- Settings panel: Master/Music/SFX volume sliders, Fullscreen toggle, Resolution dropdown
- Settings persist to PlayerPrefs, loaded on startup
- New MainMenuState as initial game state (replaces direct MarketOpenState transition)
- Title screen music plays on main menu
- Default SFX volume changed from 100% to 80%
- AudioManager/MusicManager rewired to use SettingsManager for runtime volume control

**Tasks:** 8 tasks (SettingsManager, audio rewiring, MainMenuState, main menu UI, settings panel UI, MainMenuUI controller, GameRunner refactor, tests)

**File:** `_bmad-output/implementation-artifacts/16-1-main-menu-ui.md`

---

## Story 16.2: Pause Menu (ESC Popup)

As a player, I want to press ESC during gameplay to open a pause menu with options to continue, open settings, return to the main menu, or exit, so that I can pause the action, adjust settings mid-game, or leave a run without losing control.

**Status:** ready-for-dev (blocked by 16.1)

**Acceptance Criteria:**
- ESC opens/closes pause overlay during any gameplay state
- Time.timeScale = 0 freezes all gameplay
- Buttons: CONTINUE, SETTINGS, RETURN TO MENU (with confirmation), EXIT
- Layered ESC handling (settings > confirmation > pause toggle)
- Return to menu cleans up game state (shorts, cooldowns, overlays)
- Music ducks to 50% while paused
- Shares settings panel from Story 16.1

**Tasks:** 7 tasks (pause menu UI, PauseMenuUI controller, ESC input wiring, return-to-menu cleanup, pause events, GameRunner integration, tests)

**File:** `_bmad-output/implementation-artifacts/16-2-pause-menu.md`
