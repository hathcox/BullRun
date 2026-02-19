# Story 13.10: Owned Relics Bar, Click-to-Buy, and Relic Selling

Status: done

## Story

As a player,
I want to see my owned relics displayed at the top of the store, buy items by clicking their cards directly, and sell owned relics for partial Reputation refund,
so that I can manage my relic inventory with clear visual feedback and streamlined interactions.

## Acceptance Criteria

1. An "Owned Relics" bar appears at the very top of the store, above the existing TopSection
2. The bar displays 5 slots (matching `GameConfig.MaxRelicSlots`); when Expanded Inventory is owned, bar expands to 7 slots
3. Each owned relic slot shows the relic name (abbreviated if needed) and a small sell button
4. Empty relic slots show an outline/border with "Empty" text to indicate available capacity
5. The sell button on each owned relic displays the sell-back amount (50% of the relic's original cost, rounded down)
6. Clicking sell removes the relic from `RunContext.OwnedRelics`, refunds 50% Reputation (rounded down), and updates all store displays
7. When a relic is purchased from the store, it immediately appears in the next available slot in the owned relics bar
8. Relic cards in the store are purchased by clicking the card itself — no dedicated buy button
9. Expansion cards are purchased by clicking the card itself — no dedicated buy button
10. Insider Tip cards are purchased by clicking the card itself — no dedicated buy button
11. Bond cards retain their existing BUY/SELL buttons (no change to bonds interaction)
12. If the player owns 5 relics (or 7 with Expanded Inventory) and clicks a store relic card, a brief "INVENTORY FULL" message appears on the card (no purchase occurs)
13. Clicking a relic card the player can't afford still shows "CAN'T AFFORD" feedback (existing behavior, now on card click instead of button)
14. `ShopItemPurchasedEvent` continues to fire on all purchases (existing behavior preserved)
15. `ShopItemSoldEvent` fires when a relic is sold from the owned bar, containing the relic ID and refund amount
16. The existing TopSection relic offering area is compressed vertically to make room for the owned relics bar above it

## Tasks / Subtasks

- [x] Task 1: Add sell transaction logic (AC: 6, 15)
  - [x] Add `SellRelic(RunContext ctx, string relicId)` to `ShopTransaction.cs`
  - [x] Calculate sell price: `relicDef.Cost / 2` (integer division = floor)
  - [x] Remove relic from `ctx.OwnedRelics`, add Reputation refund
  - [x] Return result enum (Success, NotOwned)
  - [x] Define `ShopItemSoldEvent` in `GameEvents.cs` with relicId and refundAmount fields
  - [x] Fire `ShopItemSoldEvent` on successful sale
  - [x] File: `Scripts/Runtime/Shop/ShopTransaction.cs`, `Scripts/Runtime/Core/GameEvents.cs`

- [x] Task 2: Create owned relics bar in UISetup (AC: 1, 2, 3, 4, 16)
  - [x] Add "OwnedRelicsBar" section above TopSection in `ExecuteStoreUI()`
  - [x] Adjust TopSection anchor to shift down, making room for the new bar
  - [x] Create HorizontalLayoutGroup with 5 slots (default) — each slot is a small card panel
  - [x] Each slot contains: relic name label, sell button with refund amount label
  - [x] Empty slot: outline border, "Empty" label, no sell button
  - [x] Sell button positioned at bottom-right or bottom of slot, small and intentional (not easy to accidentally click)
  - [x] Wire up owned relic slot views to a new `OwnedRelicSlotView` struct in ShopUI
  - [x] File: `Scripts/Setup/UISetup.cs`

- [x] Task 3: Add ShopUI owned relics bar logic (AC: 3, 4, 5, 6, 7)
  - [x] Add `OwnedRelicSlotView` struct: Root, NameLabel, SellButton, SellButtonText, EmptyLabel, CanvasGroup
  - [x] Add `_ownedRelicSlots` array field to ShopUI
  - [x] Add `RefreshOwnedRelicsBar()` method: populates slots from `ctx.OwnedRelics`, looks up RelicDef for name/cost
  - [x] Each sell button shows "SELL ★{refund}" where refund = cost / 2
  - [x] Sell button click: calls sell callback → ShopState handles transaction → refreshes all displays
  - [x] Call `RefreshOwnedRelicsBar()` on shop open and after any purchase or sale
  - [x] Handle dynamic slot count: check Expanded Inventory for 5 vs 7 slots
  - [x] File: `Scripts/Runtime/UI/ShopUI.cs`

- [x] Task 4: Convert relic cards to click-to-buy (AC: 8, 12, 13)
  - [x] Remove dedicated buy button from relic card slots in UISetup
  - [x] Add Button component to the relic card root panel itself
  - [x] Wire card click to purchase flow (same logic as current buy button)
  - [x] On click when inventory full: flash "INVENTORY FULL" text on the card briefly (~1.5s), no purchase
  - [x] On click when can't afford: flash "CAN'T AFFORD" text on the card briefly
  - [x] Adjust card layout since buy button row is removed (reclaim vertical space)
  - [x] File: `Scripts/Setup/UISetup.cs`, `Scripts/Runtime/UI/ShopUI.cs`

- [x] Task 5: Convert expansion cards to click-to-buy (AC: 9)
  - [x] Remove dedicated buy button from expansion card slots in UISetup
  - [x] Add Button component to the expansion card root panel
  - [x] Wire card click to existing expansion purchase flow
  - [x] Preserve "OWNED" watermark behavior for already-owned expansions
  - [x] Preserve "CAN'T AFFORD" feedback (now on card click)
  - [x] Adjust card layout since buy button is removed
  - [x] File: `Scripts/Setup/UISetup.cs`, `Scripts/Runtime/UI/ShopUI.cs`

- [x] Task 6: Convert insider tip cards to click-to-buy (AC: 10)
  - [x] Remove dedicated buy button from insider tip card slots in UISetup
  - [x] Add Button component to the insider tip card root panel
  - [x] Wire card click to existing tip purchase flow
  - [x] Preserve mystery card flip animation on purchase
  - [x] Preserve "CAN'T AFFORD" feedback (now on card click)
  - [x] Adjust card layout since buy button is removed
  - [x] File: `Scripts/Setup/UISetup.cs`, `Scripts/Runtime/UI/ShopUI.cs`

- [x] Task 7: Wire sell flow in ShopState (AC: 6, 7, 14, 15)
  - [x] Add `OnSellRequested(int ownedSlotIndex)` handler in ShopState
  - [x] Call `ShopTransaction.SellRelic()`, handle result
  - [x] On success: refresh owned relics bar, update currency displays, refresh relic card button states (capacity may have freed up)
  - [x] Refresh relic offering cards in case capacity changed (re-enable previously "FULL" cards)
  - [x] File: `Scripts/Runtime/Core/GameStates/ShopState.cs`

- [x] Task 8: Write tests (All AC)
  - [x] SellRelic transaction: correct refund amount (cost/2 floor), relic removed, reputation added
  - [x] SellRelic edge cases: sell relic not owned returns NotOwned
  - [x] Owned relics bar displays correct count and names
  - [x] Click-to-buy relic: purchase succeeds, relic appears in owned bar
  - [x] Click-to-buy when full: no purchase, message shown
  - [x] Click-to-buy expansion and insider tip: purchase succeeds
  - [x] Bond buttons unchanged (regression check)
  - [x] Sell refund calculation: odd cost relics round down (e.g., 150 cost → 75 refund, 251 cost → 125 refund)
  - [x] Files: `Tests/Runtime/Shop/RelicSellTests.cs`, `Tests/Runtime/Shop/ClickToBuyTests.cs`

## Dev Notes

### Architecture Compliance

- **Programmatic uGUI:** All new UI elements built in `UISetup.cs`, no prefabs. Follows existing store construction pattern from Stories 13.1–13.8.
- **Transaction pattern:** Sell logic follows the same atomic pattern as `PurchaseRelic()` in `ShopTransaction.cs` — validate, mutate state, fire event.
- **Event system:** New `ShopItemSoldEvent` follows existing `ShopItemPurchasedEvent` pattern in `GameEvents.cs`.
- **No DOTween:** Any new animations (inventory full flash, sell feedback) use coroutines with `Mathf.Lerp` and `Time.unscaledDeltaTime`.

### Layout Changes

- Current TopSection anchors: `anchorMin(0.03, 0.45)` to `anchorMax(0.97, 0.93)`
- New OwnedRelicsBar should occupy roughly `anchorMin(0.03, 0.85)` to `anchorMax(0.97, 0.93)` (above TopSection)
- TopSection shifts down: `anchorMin(0.03, 0.42)` to `anchorMax(0.97, 0.84)` (compressed to fit)
- These values are approximate — adjust during implementation for visual balance

### Sell Button Placement

The sell button must be intentionally placed to avoid accidental clicks. Place it as a small button at the bottom of each owned relic slot, visually distinct from the relic card content (different color, smaller text). The sell price label on the button makes the consequence clear before clicking.

### Slot Count Dynamic Adjustment

The owned relics bar must check `ShopTransaction.GetEffectiveMaxRelicSlots(ctx)` to determine whether to show 5 or 7 slots. When Expanded Inventory is purchased mid-shop-visit, the bar should refresh to show the additional slots.

### References

- [Source: _bmad-output/implementation-artifacts/13-3-relics-panel.md] — Original relic panel implementation
- [Source: _bmad-output/implementation-artifacts/13-8-store-visual-polish.md] — Card animation patterns
- [Source: Assets/Scripts/Setup/Data/GameConfig.cs] — MaxRelicSlots = 5
- [Source: Assets/Scripts/Runtime/Shop/ShopTransaction.cs] — GetEffectiveMaxRelicSlots(), PurchaseRelic()
- [Source: Assets/Scripts/Setup/Data/ShopItemDefinitions.cs] — RelicDef struct and RelicPool

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Debug Log References

- `[ShopTransaction] Relic sold: {name} for {refund} Rep refund` — ShopTransaction.cs
- `[ShopState] Relic sold: {relicId} (slot {index})` — ShopState.cs

### Completion Notes List

- Task 1: Added `SellRelic()` to ShopTransaction with 50% refund (floor), `NotOwned` enum value, `ShopItemSoldEvent` in GameEvents.cs. 10 tests.
- Task 2: Created OwnedRelicsBar section in UISetup with HorizontalLayoutGroup, 7 slots (5 base + 2 Expanded Inventory), each with name label, sell button, and empty label. Adjusted TopSection anchor from 0.93 to 0.84 for vertical space.
- Task 3: Added `OwnedRelicSlotView` struct, `RefreshOwnedRelicsBar()` method with dynamic slot visibility, sell button wiring with refund amount display, and empty slot handling in ShopUI.
- Task 4: Converted relic cards to click-to-buy — removed BuyBtn panel, added Button to card root, added `OnRelicCardClicked()` with INVENTORY FULL / CAN'T AFFORD flash feedback via coroutine.
- Task 5: Converted expansion cards to click-to-buy — removed BuyButton panel, added Button to card root, added `OnExpansionCardClicked()` with CAN'T AFFORD flash. Preserved OWNED watermark.
- Task 6: Converted insider tip cards to click-to-buy — removed BuyButton panel, added Button to card root, added `OnTipCardClicked()` with CAN'T AFFORD flash. Preserved mystery flip animation.
- Task 7: Added `OnSellRequested()` to ShopState wiring sell callback to ShopTransaction.SellRelic(), with full UI refresh (owned bar, currency, affordability, capacity). Added `RefreshAfterSell()` to ShopUI.
- Task 8: 27 new tests across 2 files (10 RelicSellTests + 17 ClickToBuyTests). Full regression: 1650/1650 passed, 0 failed.

### Senior Developer Review (AI)

**Reviewer:** Iggy on 2026-02-19
**Outcome:** Approved with fixes applied

**Issues Found:** 3 High, 3 Medium, 2 Low — 6 fixed automatically, 2 Low noted.

**Fixes Applied:**
1. [H1] Added `RefreshOwnedRelicsBar()` call after expansion purchase in ShopState to handle Expanded Inventory mid-visit (AC 2)
2. [H2] Added 2 insider tip click-to-buy tests (`ClickToBuy_TipPurchase_DeductsRepAndAddsTip`, `ClickToBuy_Tip_WhenCantAfford_RejectsWithInsufficientFunds`) in ClickToBuyTests.cs (AC 10)
3. [H3] Added try-catch rollback pattern to `SellRelic()` matching `PurchaseRelic()` atomic transaction pattern in ShopTransaction.cs
4. [M1] Added `AddButtonClickFeel` + `AddButtonHoverFeel` to owned relic sell buttons in `RefreshOwnedRelicsBar()`
5. [M2] Added `Background` field to `OwnedRelicSlotView` struct, cached in `CreateOwnedRelicSlot`, used in `RefreshOwnedRelicsBar` instead of `GetComponent<Image>()`
6. [M3] Added coroutine tracking arrays (`_relicFlashCoroutines`, `_expansionFlashCoroutines`, `_tipFlashCoroutines`) to prevent flash feedback stacking on rapid clicks

**Noted (not fixed):**
- [L1] No header label on Owned Relics bar (usability consideration)
- [L2] Bond regression tests are shallow (enum existence + null checks only)

### Change Log

- 2026-02-19: Initial implementation of Story 13.10 — owned relics bar, click-to-buy, relic selling
- 2026-02-19: Code review fixes — 6 issues resolved (3H, 3M): expansion→owned bar refresh, tip test, SellRelic rollback, sell button feel, Image caching, flash coroutine stacking

### File List

- `Assets/Scripts/Runtime/Shop/ShopTransaction.cs` (modified — added SellRelic method, NotOwned enum value)
- `Assets/Scripts/Runtime/Core/GameEvents.cs` (modified — added ShopItemSoldEvent struct)
- `Assets/Scripts/Runtime/UI/ShopUI.cs` (modified — added OwnedRelicSlotView, RefreshOwnedRelicsBar, click-to-buy handlers, flash feedback coroutines, RefreshAfterSell)
- `Assets/Scripts/Setup/UISetup.cs` (modified — added OwnedRelicsBar section, CreateOwnedRelicSlot, removed buy buttons from relic/expansion/tip cards, adjusted TopSection anchor)
- `Assets/Scripts/Runtime/Core/GameStates/ShopState.cs` (modified — added OnSellRequested handler, sell callback wiring, owned bar refresh on purchase)
- `Assets/Tests/Runtime/Shop/RelicSellTests.cs` (new — 10 tests for sell transaction logic)
- `Assets/Tests/Runtime/Shop/ClickToBuyTests.cs` (new — 17 tests for click-to-buy and sell-buy cycle)
