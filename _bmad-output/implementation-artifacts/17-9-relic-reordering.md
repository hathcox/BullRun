# Story 17.9: Relic Reordering

Status: done

## Story

As a player,
I want to reorder my relics in the shop's owned relics bar by clicking to select and clicking a destination,
so that I can control the execution order for strategic effect (relics execute left-to-right).

## Acceptance Criteria

1. In shop phase, clicking an owned relic in the owned relics bar highlights it as "selected" (raised, glowing border)
2. Other relic slots show insertion indicators when a relic is selected (subtle markers between slots showing where the relic will land)
3. Clicking another position moves the selected relic there (insert, not swap)
4. Other relics shift to accommodate the inserted relic
5. `RelicManager.ReorderRelic(fromIndex, toIndex)` is called on reorder
6. Clicking the same relic again or pressing Escape cancels selection without reordering
7. Relics execute in the new visual order on next dispatch (left-to-right)
8. Order persists for the rest of the run (survives round transitions between shop and trading phases)
9. Reorder is only available in the shop phase owned relics bar (NOT the trading phase RelicBar display from Story 17.8)
10. A brief label or tooltip reminds the player: "Relics execute left to right"
11. `RunContext.OwnedRelics` list order is synced with `RelicManager` after every reorder

## Tasks / Subtasks

- [x] Task 1: Add selection state tracking to ShopUI (AC: 1, 6)
  - [x] Add `_selectedRelicIndex` field (`int`, default `-1` = no selection) to ShopUI
  - [x] Add `_isRelicReorderMode` bool field to ShopUI
  - [x] Add `SelectRelicForReorder(int slotIndex)` method that sets selection state and updates visuals
  - [x] Add `CancelRelicSelection()` method that clears selection state and restores visuals
  - [x] If selected slot is clicked again, call `CancelRelicSelection()` (AC 6)
  - [x] File: `Scripts/Runtime/UI/ShopUI.cs`

- [x] Task 2: Add visual highlight for selected relic (AC: 1)
  - [x] Define `SelectedRelicBorderColor` static readonly Color (use a bright variant, e.g., `ColorPalette.Cyan` or brighter amber)
  - [x] Define `SelectedRelicScale` constant (e.g., `1.08f`) for slight scale-up effect
  - [x] When a relic is selected: change slot `Background.color` to `SelectedRelicBorderColor`, scale slot root via `localScale`
  - [x] When selection is cancelled: restore slot to `OwnedRelicSlotColor`, reset `localScale` to `Vector3.one`
  - [x] File: `Scripts/Runtime/UI/ShopUI.cs`

- [x] Task 3: Add insertion indicators between slots (AC: 2)
  - [x] Extend `OwnedRelicSlotView` struct with `GameObject InsertionIndicator` field (a thin vertical bar between slots)
  - [x] Create insertion indicator GameObjects in UISetup during owned relics bar construction — one between each pair of adjacent slots, plus one before slot 0 and one after last slot
  - [x] Indicators are hidden by default (`SetActive(false)`)
  - [x] When a relic is selected: show insertion indicators at all valid drop positions (all positions except the selected relic's current position)
  - [x] Indicator visual: thin vertical line (2-4px wide) with a subtle glow color (e.g., `ColorPalette.Cyan` at 0.6 alpha)
  - [x] When selection is cancelled or reorder completes: hide all insertion indicators
  - [x] Files: `Scripts/Runtime/UI/ShopUI.cs`, `Scripts/Setup/UISetup.cs`

- [x] Task 4: Wire click handlers for reorder (AC: 3, 4, 5, 11)
  - [x] In `RefreshOwnedRelicsBar()`, add a click handler to each populated owned relic slot root (not the sell button — the slot background/name area)
  - [x] Click logic: if no relic selected → select this relic (Task 1). If a relic is already selected → perform reorder to this position.
  - [x] On reorder: call `RelicManager.ReorderRelic(_selectedRelicIndex, targetIndex)`
  - [x] After `ReorderRelic`, sync `RunContext.OwnedRelics` list to match `RelicManager.OrderedRelics` order
  - [x] Call `RefreshOwnedRelicsBar()` to update visuals after reorder
  - [x] Clear selection state after successful reorder
  - [x] Ensure sell button click does NOT trigger reorder (stop event propagation or check click target)
  - [x] File: `Scripts/Runtime/UI/ShopUI.cs`

- [x] Task 5: Sync RunContext.OwnedRelics after reorder (AC: 11)
  - [x] Add `SyncOwnedRelicsFromRelicManager()` helper method to ShopUI (or ShopState)
  - [x] Method rebuilds `RunContext.OwnedRelics` list from `RelicManager.OrderedRelics` IDs
  - [x] Called immediately after every `ReorderRelic()` call
  - [x] This ensures save/load and other systems see the new order
  - [x] File: `Scripts/Runtime/UI/ShopUI.cs` or `Scripts/Runtime/Core/GameStates/ShopState.cs`

- [x] Task 6: Add Escape key cancellation (AC: 6)
  - [x] In ShopUI `Update()`, check for Escape key press when `_isRelicReorderMode` is true
  - [x] On Escape: call `CancelRelicSelection()`
  - [x] Ensure this does not conflict with existing Escape handling (e.g., pause menu) — reorder cancel takes priority when in reorder mode
  - [x] File: `Scripts/Runtime/UI/ShopUI.cs`

- [x] Task 7: Add "Relics execute left to right" reminder label (AC: 10)
  - [x] Add a small label above or below the owned relics bar: "Relics execute left to right"
  - [x] Use a dim/subtle color (e.g., `ColorPalette.Dimmed(ColorPalette.TextPrimary, 0.5f)`) so it is informative but not distracting
  - [x] Label is always visible when the owned relics bar is showing (not only during reorder mode)
  - [x] Create the label in UISetup during owned relics bar construction
  - [x] Files: `Scripts/Setup/UISetup.cs`, `Scripts/Runtime/UI/DashboardReferences.cs` (if reference wiring needed)

- [x] Task 8: Verify execution order after reorder (AC: 7, 8)
  - [x] After reorder, verify that `RelicManager.OrderedRelics` reflects the new order
  - [x] Verify that the next dispatch (e.g., `DispatchRoundStart`) iterates in the new visual order
  - [x] Verify that `RunContext.OwnedRelics` persists the new order across round transitions (shop → trading → shop)
  - [x] This is primarily a test task — see Task 9

- [x] Task 9: Write tests (All AC)
  - [x] Test: `ReorderRelic` moves relic and shifts others correctly (insert, not swap) — verify resulting list for multiple from/to combinations
  - [x] Test: `ReorderRelic` with fromIndex == toIndex is a no-op
  - [x] Test: `RunContext.OwnedRelics` order matches `RelicManager.OrderedRelics` after reorder
  - [x] Test: dispatch order matches visual order after reorder (create 3 test relics, reorder, dispatch, verify call order)
  - [x] Test: order persists through simulated round transition (reorder, enter trading state, return to shop, verify order unchanged)
  - [x] Test: selection state reset after successful reorder (no lingering selection)
  - [x] Test: cancel selection restores original visual state
  - [x] Files: `Tests/Runtime/Items/RelicReorderTests.cs`, `Tests/Runtime/UI/RelicReorderUITests.cs`

## Dev Notes

### Architecture Compliance

- **Programmatic uGUI:** All new UI elements (insertion indicators, reminder label) created in `UISetup.cs` during F5. No prefabs, no Inspector configuration.
- **EventBus communication:** Reorder logic communicates through `RelicManager` method calls (UI → RelicManager is the allowed one-way dependency for UI classes per project-context.md).
- **No ScriptableObjects:** Selection state lives as instance fields on ShopUI MonoBehaviour. No new data definitions needed.
- **Single-scene architecture:** All changes are runtime code; no scene modifications needed.
- **No DOTween for selection visuals:** Use direct property assignment for `Background.color` and `localScale` changes (instant, not animated). If animation is desired, use coroutines with `Mathf.Lerp`.

### Existing Code to Read Before Implementing

- `Scripts/Runtime/UI/ShopUI.cs` — `OwnedRelicSlotView` struct (line ~205), `RefreshOwnedRelicsBar()` method (line ~1816), `_ownedRelicSlots` array, `SetOwnedRelicSlots()`, sell button click wiring pattern
- `Scripts/Runtime/Items/RelicManager.cs` — `ReorderRelic(int fromIndex, int toIndex)` already defined in Story 17.1 (AC 9)
- `Scripts/Runtime/Core/RunContext.cs` — `OwnedRelics` property (`List<string>`, line ~16), used for save/load and UI display
- `Scripts/Runtime/Core/GameStates/ShopState.cs` — shop phase lifecycle, sell callback wiring, `RefreshOwnedRelicsBar` calls after purchases
- `Scripts/Setup/UISetup.cs` — owned relics bar construction (`CreateOwnedRelicSlot` method from Story 13.10)
- `Scripts/Runtime/Shop/ShopTransaction.cs` — `GetEffectiveMaxRelicSlots(ctx)` for dynamic slot count

### Key Design Decisions

- **Click-to-select-and-place (not drag-and-drop):** Simpler to implement with uGUI Button components already on slots. Drag-and-drop would require PointerDrag handlers and visual feedback that adds complexity. Click-select-click is also more accessible for gamepad (future).
- **Insert, not swap:** Moving a relic inserts it at the target position and shifts others. This matches `RelicManager.ReorderRelic` semantics (remove from old index, insert at new index). More intuitive for ordering than swapping.
- **Insertion indicators as separate GameObjects:** Created once in UISetup, toggled via `SetActive`. Avoids per-frame allocation. Positioned between slot roots using layout offsets.
- **Sell button isolation:** The sell button click must NOT trigger reorder. Use `EventTrigger` or check `EventSystem.current.currentSelectedGameObject` to distinguish sell button clicks from slot background clicks, or add a separate clickable area (Button on the name label area) that excludes the sell button region.

### Depends On

- Story 17.1 (Relic Effect Framework) — `RelicManager.ReorderRelic(fromIndex, toIndex)` must exist
- Story 13.10 (Owned Relics Bar) — owned relics bar UI must exist with `OwnedRelicSlotView` struct and `RefreshOwnedRelicsBar()` method

### References

- [Source: _bmad-output/planning-artifacts/epic-17-relic-system.md#Story 17.9]
- [Source: _bmad-output/implementation-artifacts/13-10-owned-relics-bar-and-click-to-buy.md]
- [Source: _bmad-output/implementation-artifacts/17-1-relic-effect-framework.md]
- [Source: _bmad-output/project-context.md#EventBus Communication]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Debug Log References

N/A

### Completion Notes List

- **Note:** Story 17.9 was implemented during the Story 17.10 session and committed in the same git commit (`b57dee6`). It was not tracked separately through the development workflow. Identified and documented during 17.10 code review.
- Task 1-6: Selection state, visual highlight, insertion indicators, click handlers, OwnedRelics sync, and Escape cancellation all implemented in `ShopUI.cs`.
- Task 7: "Relics execute left to right" reminder label added in `UISetup.cs`.
- Task 8: Execution order verified via `RelicManager.OrderedRelics` which is the canonical source of truth used by dispatch.
- Task 9: Tests written — 13 core reorder tests in `RelicReorderTests.cs` and 18 UI state tests in `RelicReorderUITests.cs`. All 31 tests pass (2038 total, 0 failed).

### File List

- `Scripts/Runtime/UI/ShopUI.cs` — Modified: added reorder state fields, SelectRelicForReorder, CancelRelicSelection, PerformRelicReorder, selection/insertion visual helpers, Escape key handling in Update, reorder click wiring in RefreshOwnedRelicsBar. Code review fix: cancel reorder state in RefreshOwnedRelicsBar, restore visuals on same-index early return.
- `Scripts/Setup/UISetup.cs` — Modified: added insertion indicator GameObjects in CreateOwnedRelicSlot, "Relics execute left to right" reminder label in owned relics bar construction
- `Tests/Runtime/Items/RelicReorderTests.cs` — New: 13 tests for RelicManager.ReorderRelic core logic (insert semantics, OwnedRelics sync, dispatch order, round persistence, boundary safety). 3 duplicates with RelicManagerTests removed during review.
- `Tests/Runtime/UI/RelicReorderUITests.cs` — New: 18 tests for ShopUI reorder state (selection, cancellation, reorder execution, visual highlight/scale reset, insertion indicator visibility, refresh cancels reorder)

## Change Log

- 2026-02-20: Story 17.9 implemented (bundled in commit b57dee6 with Story 17.10). Relic reordering via click-to-select-and-place in shop owned relics bar. No dedicated tests written — open gap identified during 17.10 code review.
- 2026-02-20: Task 9 tests written — 29 tests across RelicReorderTests.cs (16 core) and RelicReorderUITests.cs (13 UI state). All pass (2037 total, 0 failed).
- 2026-02-20: Code review (6 findings: 2H/2M/2L), 4 fixes applied — cancel stale reorder state on refresh (H1), insertion indicator test coverage (H2), restore visuals on same-index early return (M1), remove 3 duplicate tests (M2). 2038 tests pass, 0 failed.
