# Story 17.8: Trading Phase Relic Display & Tooltips

Status: ready-for-dev

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

- [ ] Task 1: Create RelicBar MonoBehaviour (AC: 1, 2, 3, 4, 5, 6, 7, 8, 9, 11, 13)
  - [ ] Create `Assets/Scripts/Runtime/UI/RelicBar.cs` as a MonoBehaviour
  - [ ] `Initialize(RunContext ctx)` method — stores RunContext reference for relic data access
  - [ ] Private fields: `_iconSlots` (list of icon GameObjects), `_tooltipPanel` (GameObject), `_tooltipNameText` (Text), `_tooltipDescText` (Text), `_tooltipEffectText` (Text), `_glowTimers` (Dictionary<string, float>)
  - [ ] `RefreshRelicIcons()` method — clears existing icons, iterates `ctx.RelicManager.OrderedRelics`, creates icon GameObjects for each
  - [ ] Each icon: 40x40 `Image` with `Text` overlay using CRT theme character, background color from `CRTThemeData`
  - [ ] Subscribe to `RelicActivatedEvent` via EventBus in `Initialize()` — triggers glow on matching relic icon (AC 8)
  - [ ] Subscribe to `ShopItemPurchasedEvent` and `ShopItemSoldEvent` via EventBus — calls `RefreshRelicIcons()` (AC 4)
  - [ ] Subscribe to `RoundStartedEvent` — show relic bar (AC 1, 13)
  - [ ] Subscribe to `TradingPhaseEndedEvent` — hide relic bar (AC 9, 13)
  - [ ] Subscribe to `MarketClosedEvent` — hide relic bar (AC 13)
  - [ ] Unsubscribe all in `OnDestroy()`
- [ ] Task 2: Implement hover tooltip (AC: 5, 6, 7)
  - [ ] Add Unity `EventTrigger` or `IPointerEnterHandler`/`IPointerExitHandler` to each icon slot
  - [ ] On pointer enter: show tooltip panel, populate with relic data from `ItemLookup.GetRelicById(relicId)`
  - [ ] Tooltip content: Name (bold via rich text `<b>` tags), Description, EffectDescription
  - [ ] Special case: if relicId == `relic_compound_rep`, append current sell value line (query from RelicManager or relic instance)
  - [ ] Tooltip positioning: offset above or to the side of the icon; clamp to screen bounds so it never overlaps the chart area
  - [ ] On pointer exit: hide tooltip panel
  - [ ] Tooltip appears with no delay (Unity event trigger is effectively instant, satisfying ~0.1s AC 5)
- [ ] Task 3: Implement activation glow effect (AC: 8)
  - [ ] On `RelicActivatedEvent` received: find the icon slot matching `event.RelicId`
  - [ ] Start a 0.3s glow timer for that slot — store in `_glowTimers` dictionary
  - [ ] During `Update()`: for each active glow timer, lerp icon color/alpha to create a pulse effect (bright flash fading back to normal)
  - [ ] Use `CRTThemeData.AccentGlow` or similar bright color for the flash, lerping back to the default icon color
  - [ ] Multiple simultaneous glows supported (each relic has its own timer)
- [ ] Task 4: Add relic bar references to DashboardReferences (AC: 12)
  - [ ] Add `RectTransform RelicBarRoot` field to `DashboardReferences`
  - [ ] Add `GameObject RelicBarTooltip` field to `DashboardReferences`
  - [ ] File: `Assets/Scripts/Runtime/UI/DashboardReferences.cs`
- [ ] Task 5: Create relic bar in UISetup (AC: 10)
  - [ ] In `UISetup.cs`, add a new method `ExecuteRelicBar(RunContext ctx)` or add to existing `Execute()` flow
  - [ ] Create relic bar Canvas (or child of existing dashboard canvas) with `HorizontalLayoutGroup`
  - [ ] Position in top-right area below event ticker, or configurable position
  - [ ] Set `HorizontalLayoutGroup` spacing, padding, and child alignment for 40x40 icons
  - [ ] Create tooltip panel as a child with `CanvasGroup` (initially hidden, alpha 0 or inactive)
  - [ ] Tooltip panel: vertical layout with Text components for name, description, effect
  - [ ] Style using `CRTThemeData` colors — dark background panel, green/amber text matching CRT theme
  - [ ] Wire references to `DashboardReferences` (Task 4)
  - [ ] Instantiate `RelicBar` component on the bar root, call `Initialize(ctx)`
  - [ ] File: `Assets/Scripts/Setup/UISetup.cs`
- [ ] Task 6: Call ExecuteRelicBar from GameRunner (AC: 10)
  - [ ] Add `UISetup.ExecuteRelicBar(_ctx)` call in `GameRunner.Start()` after other UI setup calls
  - [ ] Ensure it runs after `UISetup.Execute()` so DashboardReferences exist
  - [ ] File: `Assets/Scripts/Runtime/Core/GameRunner.cs`
- [ ] Task 7: Handle visibility during state transitions (AC: 9, 13)
  - [ ] Relic bar shows on `RoundStartedEvent` (trading begins)
  - [ ] Relic bar hides on `TradingPhaseEndedEvent` or `MarketClosedEvent` (trading ends)
  - [ ] Relic bar is not shown during shop — ShopUI has its own owned relics bar (AC 9)
  - [ ] Relic bar starts hidden (inactive) — only activates when first `RoundStartedEvent` fires
  - [ ] On `ReturnToMenuEvent`: hide relic bar (defensive cleanup)
- [ ] Task 8: Write tests (AC: 1-13)
  - [ ] RelicBar.RefreshRelicIcons: verify correct number of icons created matching OrderedRelics count
  - [ ] RelicBar tooltip: verify correct data populated from ItemLookup
  - [ ] RelicBar glow: verify glow timer starts on RelicActivatedEvent, completes after 0.3s
  - [ ] Visibility: verify bar shows on RoundStarted, hides on TradingPhaseEnded
  - [ ] Overflow: verify 8 relics render without clipping (layout test)
  - [ ] Files: `Assets/Tests/Runtime/UI/RelicBarTests.cs`

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

### Debug Log References

### Completion Notes List

### File List
