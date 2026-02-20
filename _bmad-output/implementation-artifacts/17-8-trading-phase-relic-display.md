# Story 17.8: Trading Phase Relic Display & Tooltips

Status: done

## Story

As a player,
I want to see my owned relics displayed during the trading phase with hover tooltips and activation glow effects,
so that I can track which relics I have, understand their effects at a glance, and see when they activate.

## Acceptance Criteria

1. A horizontal relic bar is displayed during the trading phase, positioned in the top-right area or below the event ticker
2. Each relic is shown as a small icon/badge (~40x40px equivalent) using text characters from the CRT theme font
3. Relics are displayed left-to-right matching `RelicManager.OrderedRelics` order
4. The relic bar dynamically updates when relics are gained or lost mid-run (subscribes to relic list changes)
5. Hovering over a relic icon shows a tooltip within ~0.1 seconds containing: relic name (bold), description, and effect summary (EffectDescription from RelicDef)
6. For Compound Rep relic (`relic_compound_rep`): tooltip also shows current sell value (dynamic, calculated from rounds held)
7. Tooltip does NOT obstruct the chart area — positioned above or to the side of the relic icon
8. When a relic effect activates, its icon briefly glows/pulses for 0.3 seconds (subscribes to `RelicActivatedEvent` via EventBus)
9. The relic bar is NOT shown during the shop phase — the owned relics bar in ShopUI handles relic display there
10. The relic bar is created programmatically by `UISetup` following the Setup-Oriented pattern
11. The relic bar handles 5-8 relics without overflow or visual clipping
12. `DashboardReferences` gains references for the relic bar root and tooltip objects
13. The relic bar is hidden when not in a trading-related state (hidden during MarketOpen preview, shop, run summary, main menu)

## Tasks / Subtasks

- [x] Task 1: Create RelicBar MonoBehaviour (AC: 1, 2, 3, 4, 5, 6, 7, 8, 9, 11, 13)
  - [x] Create `Assets/Scripts/Runtime/UI/RelicBar.cs` as a MonoBehaviour
  - [x] `Initialize(RunContext ctx)` method — stores RunContext reference for relic data access
  - [x] Private fields: `_iconSlots` (list of icon GameObjects), `_tooltipPanel` (GameObject), `_tooltipNameText` (Text), `_tooltipDescText` (Text), `_tooltipEffectText` (Text), `_glowTimers` (Dictionary<string, float>)
  - [x] `RefreshRelicIcons()` method — clears existing icons, iterates `ctx.RelicManager.OrderedRelics`, creates icon GameObjects for each
  - [x] Each icon: 40x40 `Image` with `Text` overlay using CRT theme character, background color from `CRTThemeData`
  - [x] Subscribe to `RelicActivatedEvent` via EventBus in `Initialize()` — triggers glow on matching relic icon (AC 8)
  - [x] Subscribe to `ShopItemPurchasedEvent` and `ShopItemSoldEvent` via EventBus — calls `RefreshRelicIcons()` (AC 4)
  - [x] Subscribe to `RoundStartedEvent` — show relic bar (AC 1, 13)
  - [x] Subscribe to `TradingPhaseEndedEvent` — hide relic bar (AC 9, 13)
  - [x] Subscribe to `MarketClosedEvent` — hide relic bar (AC 13)
  - [x] Unsubscribe all in `OnDestroy()`
- [x] Task 2: Implement hover tooltip (AC: 5, 6, 7)
  - [x] Add Unity `EventTrigger` or `IPointerEnterHandler`/`IPointerExitHandler` to each icon slot
  - [x] On pointer enter: show tooltip panel, populate with relic data from `ItemLookup.GetRelicById(relicId)`
  - [x] Tooltip content: Name (bold via rich text `<b>` tags), Description, EffectDescription
  - [x] Special case: if relicId == `relic_compound_rep`, append current sell value line (query from RelicManager or relic instance)
  - [x] Tooltip positioning: offset above or to the side of the icon; clamp to screen bounds so it never overlaps the chart area
  - [x] On pointer exit: hide tooltip panel
  - [x] Tooltip appears with no delay (Unity event trigger is effectively instant, satisfying ~0.1s AC 5)
- [x] Task 3: Implement activation glow effect (AC: 8)
  - [x] On `RelicActivatedEvent` received: find the icon slot matching `event.RelicId`
  - [x] Start a 0.3s glow timer for that slot — store in `_glowTimers` dictionary
  - [x] During `Update()`: for each active glow timer, lerp icon color/alpha to create a pulse effect (bright flash fading back to normal)
  - [x] Use `ColorPalette.White` for the flash, lerping back to the default icon color (CRTThemeData has no AccentGlow — White gives a bright phosphor flash)
  - [x] Multiple simultaneous glows supported (each relic has its own timer)
- [x] Task 4: Add relic bar references to DashboardReferences (AC: 12)
  - [x] Add `RectTransform RelicBarRoot` field to `DashboardReferences`
  - [x] Add `GameObject RelicBarTooltip` field to `DashboardReferences`
  - [x] File: `Assets/Scripts/Runtime/UI/DashboardReferences.cs`
- [x] Task 5: Create relic bar in UISetup (AC: 10)
  - [x] In `UISetup.cs`, add a new method `ExecuteRelicBar(RunContext ctx)` or add to existing `Execute()` flow
  - [x] Create relic bar Canvas (or child of existing dashboard canvas) with `HorizontalLayoutGroup`
  - [x] Position in top-right area below event ticker, or configurable position
  - [x] Set `HorizontalLayoutGroup` spacing, padding, and child alignment for 40x40 icons
  - [x] Create tooltip panel as a child with `CanvasGroup` (initially hidden, alpha 0 or inactive)
  - [x] Tooltip panel: vertical layout with Text components for name, description, effect
  - [x] Style using `CRTThemeData` colors — dark background panel, green/amber text matching CRT theme
  - [x] Wire references to `DashboardReferences` (Task 4)
  - [x] Instantiate `RelicBar` component on the bar root, call `Initialize(ctx)`
  - [x] File: `Assets/Scripts/Setup/UISetup.cs`
- [x] Task 6: Call ExecuteRelicBar from GameRunner (AC: 10)
  - [x] Add `UISetup.ExecuteRelicBar(_ctx)` call in `GameRunner.Start()` after other UI setup calls
  - [x] Ensure it runs after `UISetup.Execute()` so DashboardReferences exist
  - [x] File: `Assets/Scripts/Runtime/Core/GameRunner.cs`
- [x] Task 7: Handle visibility during state transitions (AC: 9, 13)
  - [x] Relic bar shows on `RoundStartedEvent` (trading begins)
  - [x] Relic bar hides on `TradingPhaseEndedEvent` or `MarketClosedEvent` (trading ends)
  - [x] Relic bar is not shown during shop — ShopUI has its own owned relics bar (AC 9)
  - [x] Relic bar starts hidden (inactive) — only activates when first `RoundStartedEvent` fires
  - [x] On `ReturnToMenuEvent`: hide relic bar (defensive cleanup)
- [x] Task 8: Write tests (AC: 1-13)
  - [x] RelicBar.RefreshRelicIcons: verify correct number of icons created matching OrderedRelics count
  - [x] RelicBar tooltip: verify correct data populated from ItemLookup
  - [x] RelicBar glow: verify glow timer starts on RelicActivatedEvent, completes after 0.3s
  - [x] Visibility: verify bar shows on RoundStarted, hides on TradingPhaseEnded
  - [x] Overflow: verify 8 relics render without clipping (layout test)
  - [x] Files: `Assets/Tests/Runtime/UI/RelicBarTests.cs`

## Dev Notes

### Architecture Compliance

- **Programmatic uGUI:** All UI elements created via `new GameObject()` + `AddComponent<>()` in UISetup — never configured in Inspector. Per project rules, no UI Toolkit (UXML/USS).
- **EventBus communication:** RelicBar subscribes to `RelicActivatedEvent`, `ShopItemPurchasedEvent`, `ShopItemSoldEvent`, `RoundStartedEvent`, `TradingPhaseEndedEvent`, `MarketClosedEvent` via EventBus. Never references game systems directly.
- **Setup-Oriented pattern:** RelicBar MonoBehaviour is created by UISetup during runtime initialization (GameRunner.Start), not during F5 scene generation. This follows the pattern of other UI MonoBehaviours like `ShopUI`, `PauseMenuUI`.
- **One-way UI dependency:** RelicBar reads from `RunContext.RelicManager.OrderedRelics` and `ItemLookup.GetRelicById()` — one-way data flow from runtime systems to UI.
- **CRT theme:** Use `CRTThemeData` colors for all visual styling (background, text, glow colors) to maintain consistent retro terminal aesthetic.

### Existing Code to Read Before Implementing

- `Scripts/Runtime/UI/DashboardReferences.cs` — existing reference container pattern; add `RelicBarRoot` and `RelicBarTooltip` fields
- `Scripts/Setup/UISetup.cs` — existing `Execute()`, `ExecuteControlDeck()`, `ExecuteStoreUI()` methods for pattern reference on creating programmatic uGUI
- `Scripts/Setup/Data/CRTThemeData.cs` — color definitions (BackgroundDark, TextGreen, TextAmber, AccentGlow, etc.) for consistent styling
- `Scripts/Runtime/Core/GameRunner.cs` — `Start()` method where other `UISetup.Execute*()` calls are made; add `ExecuteRelicBar()` call
- `Scripts/Runtime/Core/GameEvents.cs` — `RelicActivatedEvent` struct (from Story 17.1), `RoundStartedEvent`, `TradingPhaseEndedEvent`, `MarketClosedEvent`, `ShopItemPurchasedEvent`, `ShopItemSoldEvent`
- `Scripts/Runtime/Items/RelicManager.cs` — `IReadOnlyList<IRelic> OrderedRelics` for icon ordering (from Story 17.1)
- `Scripts/Runtime/Items/ItemLookup.cs` — `GetRelicById(string relicId)` returns `RelicDef?` with Name, Description, EffectDescription
- `Scripts/Runtime/UI/ShopUI.cs` — owned relics bar pattern (for reference on how relics are displayed in shop context, which this bar complements during trading)
- `Scripts/Setup/Data/ShopItemDefinitions.cs` — `RelicDef` struct with `EffectDescription` field (from Story 17.2)

### Depends On

- **Story 17.1** — RelicManager with `OrderedRelics`, `RelicActivatedEvent` in GameEvents.cs, EventBus dispatch wiring

### Key Design Decisions

- **Separate from ShopUI owned bar:** The trading phase relic bar is a distinct component from the shop's owned relics bar. They serve different purposes — the trading bar shows active effects and glow animations, while the shop bar supports buy/sell interactions. No code sharing needed.
- **HorizontalLayoutGroup for layout:** Using Unity's built-in `HorizontalLayoutGroup` handles spacing and alignment automatically for 5-8 icons. Content size fitter ensures no overflow. If icon count exceeds available space, icons can scale down slightly.
- **Glow via color lerp, not particles:** The 0.3s activation glow uses a simple `Image.color` lerp from bright accent to default — no particle system overhead. This is lightweight and fits the CRT aesthetic (phosphor glow effect).
- **Tooltip via CanvasGroup:** The tooltip uses a `CanvasGroup` for show/hide (alpha 0 vs 1) which is cleaner than SetActive toggling and allows fade transitions if desired later.
- **Compound Rep special case:** The tooltip dynamically calculates sell value for `relic_compound_rep` by querying the relic instance. This is the only relic with a dynamic tooltip value — all others are static text from RelicDef.
- **RelicBar subscribes to purchase/sell events:** Rather than polling, the bar subscribes to `ShopItemPurchasedEvent` and `ShopItemSoldEvent` to refresh icons. This handles mid-run relic changes reactively.

### References

- [Source: _bmad-output/planning-artifacts/epic-17-relic-system.md#Story 17.8]
- [Source: _bmad-output/implementation-artifacts/17-1-relic-effect-framework.md]
- [Source: _bmad-output/project-context.md#UI Framework Rules]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Debug Log References

N/A — no runtime debugging needed; tests validate all logic paths.

### Completion Notes List

- Created `RelicBar` MonoBehaviour with full EventBus-driven lifecycle: subscribes to 7 events (RelicActivatedEvent, ShopItemPurchasedEvent, ShopItemSoldEvent, RoundStartedEvent, TradingPhaseEndedEvent, MarketClosedEvent, ReturnToMenuEvent), unsubscribes all in OnDestroy.
- `RefreshRelicIcons()` creates 40x40 icon slots with CRTThemeData.Panel background, text overlay showing first 2 chars of relic name (placeholder for Story 17.10 proper icons).
- Hover tooltip via Unity `EventTrigger` (PointerEnter/PointerExit) — shows relic Name (bold rich text), Description, EffectDescription. Compound Rep special case appends dynamic sell value from `IRelic.GetSellValue()`.
- Tooltip positioned above icon with screen-edge clamping (right, left, top overflow handled).
- Activation glow: 0.3s timer per relic, `Color.Lerp` from `ColorPalette.White` to `CRTThemeData.Panel`. Multiple simultaneous glows supported. Used White instead of AccentGlow (not in CRTThemeData) for bright phosphor flash effect.
- UISetup.ExecuteRelicBar creates its own canvas (sortingOrder 21), HorizontalLayoutGroup bar, tooltip with CanvasGroup + VerticalLayoutGroup, ContentSizeFitter for overflow prevention.
- DashboardReferences gains `RelicBarRoot` (RectTransform) and `RelicBarTooltip` (GameObject) fields.
- GameRunner.Start() calls `UISetup.ExecuteRelicBar(_ctx)` after `UISetup.ExecuteScreenEffects()` / before overlay UIs.
- Visibility: starts hidden, shows on RoundStarted, hides on TradingPhaseEnded/MarketClosed/ReturnToMenu.
- 27 tests covering: icon creation (0, 3, 8 relics), refresh on events, glow start/multi-glow/duration, visibility state transitions, tooltip content population (AC 5), Compound Rep dynamic sell value (AC 6), tooltip edge cases, non-relic purchase filtering, icon character generation edge cases.

### Change Log

- 2026-02-20: Implemented Story 17.8 — RelicBar UI with tooltips and glow effects (all 8 tasks, 22 tests)
- 2026-02-20: Code review fixes — eliminated per-frame allocations in Update(), added relic-only purchase filter, fixed tooltip positioning for resolution independence, cleaned up tooltip show/hide (blocksRaycasts), added 5 missing tests (AC 5, 6, 8 coverage). 27 tests total.

### File List

- `Assets/Scripts/Runtime/UI/RelicBar.cs` (new) — RelicBar MonoBehaviour with icons, tooltip, glow
- `Assets/Scripts/Runtime/UI/DashboardReferences.cs` (modified) — Added RelicBarRoot, RelicBarTooltip fields
- `Assets/Scripts/Setup/UISetup.cs` (modified) — Added ExecuteRelicBar() method
- `Assets/Scripts/Runtime/Core/GameRunner.cs` (modified) — Added UISetup.ExecuteRelicBar(_ctx) call in Start()
- `Assets/Tests/Runtime/UI/RelicBarTests.cs` (new) — 27 unit tests for RelicBar
