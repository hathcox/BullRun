# Story 13.1: Store Layout & Navigation Shell

Status: pending

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

- [ ] Task 1: Design store panel layout structure (AC: 1, 2, 3, 4, 5)
  - [ ] Create main store container with two rows: top (relics) and bottom (3-column split)
  - [ ] Top row: left control panel (Next Round + Reroll buttons) + 3 relic card slots
  - [ ] Bottom row: left panel (Expansions), center panel (Insider Tips), right panel (Bonds)
  - [ ] Each panel has header label, bordered background, and content area
  - [ ] File: `Scripts/Setup/UISetup.cs` — new `ExecuteStoreUI()` method replacing `ExecuteShopUI()`
- [ ] Task 2: Rewrite ShopUI as StoreUI controller (AC: 1, 9, 10)
  - [ ] Replace existing `ShopUI.cs` layout code with new multi-panel layout references
  - [ ] Wire "Next Round" button to close store and advance (same as existing Continue button)
  - [ ] Ensure `ShopOpenedEvent` fires on store open with updated payload
  - [ ] Ensure `ShopClosedEvent` fires on store close with updated payload
  - [ ] File: `Scripts/Runtime/UI/ShopUI.cs` — rewrite
- [ ] Task 3: Currency display bar (AC: 6, 7)
  - [ ] Reputation balance: amber/gold star icon + number (top of store or integrated into control panel)
  - [ ] Cash balance: green dollar icon + number
  - [ ] Both update reactively on purchase
  - [ ] File: `Scripts/Runtime/UI/ShopUI.cs`, `Scripts/Setup/UISetup.cs`
- [ ] Task 4: Panel placeholder content (AC: 2, 3, 4, 5)
  - [ ] Relic slots: 3 empty card frames with "empty" state
  - [ ] Expansions panel: "EXPANSIONS" header, empty content area
  - [ ] Insider Tips panel: "INSIDER TIPS" header, empty content area
  - [ ] Bonds panel: "BONDS" header, empty content area
  - [ ] Placeholders will be replaced by Stories 13.2-13.5
- [ ] Task 5: Keyboard navigation (AC: 11)
  - [ ] Tab cycles focus between panels
  - [ ] Arrow keys navigate within focused panel
  - [ ] Visual focus indicator on active panel
- [ ] Task 6: Update ShopState orchestration (AC: 9, 10)
  - [ ] ShopState.Enter() shows new store layout instead of old shop
  - [ ] ShopState.Exit() hides store, fires ShopClosedEvent
  - [ ] Remove old shop generation logic (will be re-added in 13.2-13.5)
  - [ ] File: `Scripts/Runtime/Core/GameStates/ShopState.cs`
- [ ] Task 7: Update event payloads (AC: 10)
  - [ ] `ShopOpenedEvent` — add fields for section availability (expansions available, tips available, bond available)
  - [ ] `ShopClosedEvent` — add fields for purchases per section
  - [ ] File: `Scripts/Runtime/Core/GameEvents.cs`
- [ ] Task 8: Write tests (All AC)
  - [ ] Store opens and displays all 4 panels
  - [ ] "Next Round" button transitions to next state
  - [ ] Currency displays show correct values
  - [ ] Events fire with correct payloads
  - [ ] File: `Tests/Runtime/Shop/StoreLayoutTests.cs`

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

This story creates the **layout infrastructure only**. All panels will have placeholder/empty content. The actual functionality for each panel is built in Stories 13.2-13.5. This story should be implementable without any new game logic — it is purely UI scaffolding + event updates.

## Dev Agent Record

### Agent Model Used

### Completion Notes List

### Change Log

### File List
