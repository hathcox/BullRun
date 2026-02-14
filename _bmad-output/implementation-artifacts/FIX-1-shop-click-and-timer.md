# Story FIX-1: Shop Click Fix & Timer Removal

Status: done

## Story

As a player,
I want shop item buttons to respond when I click them and have unlimited time to make my draft choices,
so that I can actually purchase upgrades and make thoughtful decisions.

## Problem Analysis

Two bugs exist in the current shop implementation:

### Bug A: Shop Items Not Clickable
**Root Cause:** In `UISetup.CreateItemCard()` (line ~1086), the button label text created via `CreateLabel()` defaults to `raycastTarget = true`. The Text component sits on top of the Button, intercepting all pointer events before they reach the Button's `onClick` handler.

**Affected Code:**
- `Scripts/Setup/UISetup.cs` — `CreateItemCard()` method, button label creation
- The `CreateLabel()` helper (line ~1363) creates Text components that default to `raycastTarget = true`

### Bug B: Shop Has a Timer (Should Not)
**Root Cause:** `ShopState.cs` implements an 18-second countdown timer that auto-closes the shop. `GameConfig.ShopDurationSeconds = 18f` drives this. The shop should be untimed — the player decides when to leave.

**Affected Code:**
- `Scripts/Runtime/Core/GameStates/ShopState.cs` — timer logic in Update(), `_timeRemaining` field
- `Scripts/Setup/Data/GameConfig.cs` — `ShopDurationSeconds` constant
- `Scripts/Runtime/UI/ShopUI.cs` — `UpdateTimer()` method, timer text display
- `Scripts/Setup/UISetup.cs` — timer text UI element creation

## Acceptance Criteria

1. All three shop item purchase buttons respond to mouse clicks
2. Shop has NO countdown timer — player browses at their own pace
3. A "Continue" or "Next Round" button is present so the player can leave the shop when ready
4. Timer text element is removed from the shop UI
5. No regressions in purchase flow (cash deduction, item tracking, button disable after purchase)

## Tasks / Subtasks

- [x] Task 1: Fix button raycast blocking (AC: 1)
  - [x] In `UISetup.CreateItemCard()`, after creating the button label via `CreateLabel()`, set `raycastTarget = false` on the Text component
  - [x] Audit ALL Text/Image components layered over interactive elements in shop UI — set `raycastTarget = false` on any non-interactive overlay elements (item name, description, cost text, rarity badge text)
  - [x] File: `Scripts/Setup/UISetup.cs`

- [x] Task 2: Remove shop timer (AC: 2, 4)
  - [x] Remove `_timeRemaining` field and all timer countdown logic from `ShopState.Update()`
  - [x] Remove the `if (_timeRemaining <= 0f) CloseShop()` auto-close behavior
  - [x] Remove `ShopUI.UpdateTimer()` method
  - [x] Remove timer text UI element from `UISetup.ExecuteShopUI()`
  - [x] Remove or deprecate `GameConfig.ShopDurationSeconds`
  - [x] Files: `Scripts/Runtime/Core/GameStates/ShopState.cs`, `Scripts/Runtime/UI/ShopUI.cs`, `Scripts/Setup/UISetup.cs`, `Scripts/Setup/Data/GameConfig.cs`

- [x] Task 3: Add "Continue" button to shop (AC: 3)
  - [x] Create a prominent "Next Round >>" button at the bottom-center of the shop panel
  - [x] Style: large, clearly visible, distinct from purchase buttons (e.g., blue/white vs green purchase buttons)
  - [x] Wire onClick to `ShopState.CloseShop()` (player-initiated close)
  - [x] Button should be always enabled — player can skip all purchases
  - [x] Files: `Scripts/Setup/UISetup.cs`, `Scripts/Runtime/UI/ShopUI.cs`, `Scripts/Runtime/Core/GameStates/ShopState.cs`

- [x] Task 4: Update tests (AC: 5)
  - [x] Remove/update timer-related tests in `ShopStateTests.cs`
  - [x] Add test: shop does NOT auto-close after any duration
  - [x] Add test: Continue button triggers shop close
  - [x] Verify existing purchase flow tests still pass
  - [x] Files: `Tests/Runtime/Core/GameStates/ShopStateTests.cs`

## Dev Notes

### Architecture Compliance
- **Setup-Oriented Generation:** Continue creating all UI programmatically in UISetup
- **EventBus:** `ShopClosedEvent` should still fire when Continue is clicked, carrying the same data
- **No Inspector config:** Button created in code, onClick wired at runtime in ShopUI.Show()

### Key Insight on raycastTarget
The `CreateLabel()` helper in UISetup is used extensively. Do NOT change the helper's default behavior — that would affect all labels project-wide. Instead, explicitly set `raycastTarget = false` on specific label instances that overlay interactive elements.

### References
- `Scripts/Setup/UISetup.cs` lines 1079-1092 (CreateItemCard button creation)
- `Scripts/Setup/UISetup.cs` lines 1363-1378 (CreateLabel helper)
- `Scripts/Runtime/Core/GameStates/ShopState.cs` lines 48, 87-99 (timer logic)
- `Scripts/Runtime/UI/ShopUI.cs` lines 127-134 (UpdateTimer)

## Dev Agent Record

### Implementation Notes
- **Task 1 (raycast fix):** Set `raycastTarget = false` on 8 elements in `CreateItemCard()`: category label, rarity badge image, rarity text, item name, description, cost, BUY button label. Also fixed DONE/Continue button label in `ExecuteShopUI()`. Did NOT modify the `CreateLabel()` helper default — per Dev Notes, only targeted specific instances.
- **Task 2 (timer removal):** Removed `_timeRemaining` field, timer countdown in `Update()`, `GameConfig.ShopDurationSeconds`, `ShopUI.UpdateTimer()`, timer text UI element. Simplified `CloseShop()` signature to remove `timerExpired` parameter (always `false` now). `ShopClosedEvent.TimerExpired` field kept for backwards compatibility, always set to `false`.
- **Task 3 (Continue button):** Restyled existing "DONE" button as "NEXT ROUND >>" — blue (0.15, 0.3, 0.6) vs green (0, 0.6, 0.3) purchase buttons, larger (240x50 vs 120x40 original), font size 18. Wired through existing `SetDoneButton`/`SetOnCloseCallback` mechanism. Always enabled.
- **Task 4 (tests):** Removed `ShopTimerDuration_IsWithinGDDRange` test. Removed `Update_TimerExpiry_TriggersCloseAndPublishesEvent`. Updated `ShopClosedEvent_ContainsPurchasedItemIds` and `ShopClosedEvent_ContainsCashRemaining` to use `InvokeCloseShop` reflection helper instead of timer. Added `Update_DoesNotAutoCloseShop` (1000 Update calls, no close). Added `CloseShop_ViaCallback_PublishesShopClosedEvent`. Renamed timer-referencing test names.
- **Additional fix (EventSystem):** No EventSystem existed in the scene — uGUI buttons require EventSystem + InputSystemUIInputModule for mouse click processing. Added creation in GameRunner.Start() before UI setup.
- **Additional fix (button height):** Buy button had 0 height inside VerticalLayoutGroup — Image-only panels have preferredHeight=0 (no sprite). Added LayoutElement with minHeight/preferredHeight=40 on buy button, preferredHeight=4 on rarity badge.
- **Pre-existing test fix (god mode):** 8 tests in MarginCallStateTests/MarketCloseStateTests were failing because `DebugManager.IsGodMode` was stuck `true` from a previous play session. Added reflection reset of `IsGodMode` to `false` in both test SetUp methods.

### Completion Notes
All 4 tasks implemented. 5 acceptance criteria satisfied:
1. Purchase buttons respond (raycastTarget=false on overlaying text/images)
2. No countdown timer (all timer code removed)
3. "NEXT ROUND >>" Continue button present (blue, prominent, bottom-center)
4. Timer text element removed from shop UI
5. Purchase flow tests preserved + 2 new tests added, 1 timer test removed, 2 timer tests updated

## File List

- `Assets/Scripts/Setup/UISetup.cs` — Modified: raycastTarget=false on shop card elements, removed timer text, restyled Continue button, added LayoutElement on buy button (40px) and rarity badge (4px)
- `Assets/Scripts/Runtime/Core/GameStates/ShopState.cs` — Modified: removed timer field/logic, simplified CloseShop, updated docstrings
- `Assets/Scripts/Runtime/UI/ShopUI.cs` — Modified: removed timerText field/param, removed UpdateTimer method, updated docstrings
- `Assets/Scripts/Setup/Data/GameConfig.cs` — Modified: removed ShopDurationSeconds constant
- `Assets/Scripts/Runtime/Core/GameRunner.cs` — Modified: added EventSystem + InputSystemUIInputModule creation in Start()
- `Assets/Tests/Runtime/Core/GameStates/ShopStateTests.cs` — Modified: removed/updated timer tests, added untimed shop tests
- `Assets/Tests/Runtime/Core/GameStates/MarginCallStateTests.cs` — Modified: reset DebugManager.IsGodMode in SetUp
- `Assets/Tests/Runtime/Core/GameStates/MarketCloseStateTests.cs` — Modified: reset DebugManager.IsGodMode in SetUp

## Change Log

- **2026-02-13:** FIX-1 implemented — Fixed shop button clicks (raycast blocking + missing EventSystem + 0-height button), removed 18s timer, added "NEXT ROUND >>" Continue button, updated tests
- **2026-02-13:** Code review fixes — Removed dead `TimerExpired` field from `ShopClosedEvent` and all references. Renamed `_doneButton`/`SetDoneButton` → `_continueButton`/`SetContinueButton` for naming consistency. Moved EventSystem creation from `GameRunner.Start()` to `UISetup.Execute()` per Setup-Oriented architecture.
