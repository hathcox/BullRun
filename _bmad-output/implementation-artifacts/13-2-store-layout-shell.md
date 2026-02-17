# Story 13.2: Store Layout & Navigation Shell

Status: done

## Story

As a player,
I want the between-rounds store to have a clear multi-panel layout with distinct sections for Relics, Expansions, Insider Tips, and Bonds,
so that I can quickly understand my options and make strategic purchases.

## Acceptance Criteria

1. Store UI replaces the current `ShopUI.cs` single-panel layout
2. Top section: Relic cards (3 empty slots) with reroll button and "Next Round" button on the left
3. Bottom-left panel: Trading Deck Expansions (vouchers) — labeled section with placeholder content
4. Bottom-center panel: Insider Tips — labeled section with placeholder content
5. Bottom-right panel: Bonds — labeled section with placeholder content
6. Current Reputation balance displayed prominently (amber/gold star icon, matching existing style)
7. Current cash balance also visible
8. Panel borders and labels match a dark, card-game aesthetic
9. Store remains untimed (player clicks "Next Round" to proceed)
10. `ShopOpenedEvent` and `ShopClosedEvent` still fire with updated payload
11. Keyboard navigation between panels (arrow keys or tab)

## Tasks / Subtasks

- [x] Task 1: Design store panel layout structure (AC: 1, 2, 3, 4, 5)
  - [x] Create main store container with two rows: top (relics) and bottom (3-column split)
  - [x] Top row: left control panel (Next Round + Reroll buttons) + 3 relic card slots
  - [x] Bottom row: left panel (Expansions), center panel (Insider Tips), right panel (Bonds)
  - [x] Each panel has header label, bordered background, and content area
  - [x] File: `Scripts/Setup/UISetup.cs` — new `ExecuteStoreUI()` method replacing `ExecuteShopUI()`
- [x] Task 2: Rewrite ShopUI as StoreUI controller (AC: 1, 9, 10)
  - [x] Replace existing `ShopUI.cs` layout code with new multi-panel layout references
  - [x] Wire "Next Round" button to close store and advance (same as existing Continue button)
  - [x] Ensure `ShopOpenedEvent` fires on store open with updated payload
  - [x] Ensure `ShopClosedEvent` fires on store close with updated payload
  - [x] File: `Scripts/Runtime/UI/ShopUI.cs` — rewrite
- [x] Task 3: Currency display bar (AC: 6, 7)
  - [x] Reputation balance: amber/gold star icon + number (top of store or integrated into control panel)
  - [x] Cash balance: green dollar icon + number
  - [x] Both update reactively on purchase
  - [x] File: `Scripts/Runtime/UI/ShopUI.cs`, `Scripts/Setup/UISetup.cs`
- [x] Task 4: Panel placeholder content (AC: 2, 3, 4, 5)
  - [x] Relic slots: 3 empty card frames with "empty" state
  - [x] Expansions panel: "EXPANSIONS" header, empty content area
  - [x] Insider Tips panel: "INSIDER TIPS" header, empty content area
  - [x] Bonds panel: "BONDS" header, empty content area
  - [x] Placeholders will be replaced by Stories 13.3-13.6
- [x] Task 5: Keyboard navigation (AC: 11)
  - [x] Tab cycles focus between panels
  - [x] Arrow keys navigate within focused panel
  - [x] Visual focus indicator on active panel
- [x] Task 6: Update ShopState orchestration (AC: 9, 10)
  - [x] ShopState.Enter() shows new store layout instead of old shop
  - [x] ShopState.Exit() hides store, fires ShopClosedEvent
  - [x] Remove old shop generation logic (will be re-added in 13.3-13.6)
  - [x] File: `Scripts/Runtime/Core/GameStates/ShopState.cs`
- [x] Task 7: Update event payloads (AC: 10)
  - [x] `ShopOpenedEvent` — add fields for section availability (expansions available, tips available, bond available)
  - [x] `ShopClosedEvent` — add fields for purchases per section
  - [x] File: `Scripts/Runtime/Core/GameEvents.cs`
- [x] Task 8: Write tests (All AC)
  - [x] Store opens and displays all 4 panels
  - [x] "Next Round" button transitions to next state
  - [x] Currency displays show correct values
  - [x] Events fire with correct payloads
  - [x] File: `Tests/Runtime/Shop/StoreLayoutTests.cs`

## Dev Notes

### Layout Reference (Balatro-inspired)

```
┌──────────────────────────────────────────────────────────────┐
│  ┌────────────┐                                              │
│  │ Next Round │   [RELIC 1]     [RELIC 2]     [RELIC 3]      │
│  │            │   $cost         $cost         $cost          │
│  │ Reroll     │                                              │
│  │ $cost      │   (Top Section — Relics / Items)             │
│  └────────────┘                                              │
│──────────────────────────────────────────────────────────────│
│  TRADING DECK EXPANSIONS    │    INSIDER TIPS       │ BONDS  │
│  (Vouchers — Bottom Left)   │    (Bottom Center)    │(Right) │
│                             │                       │        │
│  [EXPANSION 1]              │  [? ? ?]  [? ? ?]     │ [BOND] │
│  [EXPANSION 2]              │  (hidden until bought) │ $cost  │
│  [EXPANSION 3]              │                       │        │
│  (one-time permanent        │  One-time per visit    │ +Rep/  │
│   upgrades)                 │                       │ round  │
└──────────────────────────────────────────────────────────────┘
```

### Architecture Compliance

- **Setup-Oriented Generation:** Store UI created programmatically via UISetup during F5. No Inspector configuration.
- **uGUI Canvas:** All store UI built with uGUI — NOT UI Toolkit. Canvas hierarchy created in code.
- **EventBus:** Shop events updated to reflect new store sections.
- **No prefabs:** All UI elements constructed programmatically.

### Existing Code to Understand

Before implementing, the dev agent MUST read:
- `Scripts/Runtime/UI/ShopUI.cs` — current shop implementation to replace
- `Scripts/Setup/UISetup.cs` — how UI panels are generated in F5 (ExecuteShopUI method)
- `Scripts/Runtime/Core/GameStates/ShopState.cs` — shop state orchestration
- `Scripts/Runtime/Core/GameEvents.cs` — existing shop event definitions
- `Scripts/Runtime/Core/ReputationManager.cs` — how Reputation is displayed

### Important: This is a Shell

This story creates the **layout infrastructure only**. All panels will have placeholder/empty content. The actual functionality for each panel is built in Stories 13.3-13.6. This story should be implementable without any new game logic — it is purely UI scaffolding + event updates.

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Completion Notes List

- Rewrote `ShopUI.cs` from single-panel item card layout to multi-panel Balatro-style store controller with top section (control panel + 3 relic slots) and bottom section (3 labeled panels: Expansions, Insider Tips, Bonds)
- Created `ExecuteStoreUI()` in UISetup.cs replacing `ExecuteShopUI()`, building the full multi-panel layout programmatically with uGUI
- Added currency bar with both Reputation (amber/gold star) and Cash (green dollar) displays that update reactively
- Implemented keyboard navigation: Tab cycles between bottom panels, left/right arrow keys navigate, visual focus indicator (blue highlight) on active panel
- Updated `ShopOpenedEvent` with section availability flags (ExpansionsAvailable, TipsAvailable, BondAvailable)
- Updated `ShopClosedEvent` with per-section purchase counts (RelicsPurchased, ExpansionsPurchased, TipsPurchased, BondsPurchased)
- Updated ShopState to use new StoreUI API, removed old upgrade card references (HideUpgrade)
- Updated GameRunner.cs call from `ExecuteShopUI()` to `ExecuteStoreUI()`
- All bottom panels have placeholder "Coming soon..." content — to be populated by Stories 13.3-13.6
- Added `CreateRelicSlot()` and `CreateStorePanel()` helper methods in UISetup for creating relic card slots and labeled bottom panels
- Created comprehensive test suite in StoreLayoutTests.cs covering event payloads, section availability flags, per-section purchase counts, currency tracking, color constants, and null-UI safety

### Change Log

- 2026-02-16: Implemented Story 13.2 — Multi-panel store layout shell replacing single-panel shop. New Balatro-style store with relics top section, 3 bottom panels (Expansions/Tips/Bonds), currency bar, keyboard navigation, and updated event payloads.
- 2026-02-16: Code Review (AI) — Fixed 6 issues: increased test reputation to prevent flaky purchase tests (H2), added keyboard navigation tests for AC 11 (M1), added cash balance test for AC 7 (M2), added EventScheduler field to all ShopStateConfig in tests for consistency (H1/M). 3 LOW issues noted but deferred (shell-appropriate).

### File List

- Assets/Scripts/Runtime/UI/ShopUI.cs (modified — rewritten as multi-panel store controller)
- Assets/Scripts/Setup/UISetup.cs (modified — ExecuteShopUI replaced with ExecuteStoreUI, added CreateRelicSlot and CreateStorePanel helpers)
- Assets/Scripts/Runtime/Core/GameEvents.cs (modified — ShopOpenedEvent and ShopClosedEvent extended with new fields)
- Assets/Scripts/Runtime/Core/GameStates/ShopState.cs (modified — updated to new ShopUI API, added per-section purchase counts to events)
- Assets/Scripts/Runtime/Core/GameRunner.cs (modified — ExecuteShopUI call changed to ExecuteStoreUI)
- Assets/Tests/Runtime/Shop/StoreLayoutTests.cs (new — 17 tests for store layout, events, colors, keyboard navigation)
- Assets/Tests/Runtime/Core/GameStates/ShopStateTests.cs (modified — added EventScheduler to ShopStateConfig instances)
