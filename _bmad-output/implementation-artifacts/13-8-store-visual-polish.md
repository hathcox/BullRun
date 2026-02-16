# Story 13.8: Store Visual Polish & Card Animations

Status: done

## Story

As a player,
I want store cards to have satisfying visual presentation with hover effects, purchase animations, and the mystery card flip for Insider Tips,
so that the store feels premium and engaging.

## Acceptance Criteria

1. Relic cards: hover glow effect, consistent card border style (no rarity colors — unified look)
2. Purchase animation: card slides up and fades, "SOLD" stamp appears briefly
3. Reroll animation: cards flip/shuffle before new ones appear
4. Insider Tip cards: face-down with "?" symbol, flip animation on purchase revealing the intel
5. Bond card: subtle pulsing glow, price tag prominent
6. Expansion cards: clean layout with icon + description, "OWNED" watermark when purchased
7. All text legible against dark panel backgrounds
8. Consistent with existing UI style (programmatic uGUI, no prefabs)

## Tasks / Subtasks

- [x] Task 1: Relic card hover effects (AC: 1)
  - [x] Glow effect on mouse hover (scale up slightly + border brightness increase)
  - [x] Consistent card border color across all relics (no rarity differentiation)
  - [x] Smooth hover transition (lerp scale and color over ~0.15s)
  - [x] File: `Scripts/Runtime/UI/ShopUI.cs` — hover event handlers
- [x] Task 2: Purchase animation (AC: 2)
  - [x] On purchase: card slides upward + fades out over ~0.5s
  - [x] "SOLD" stamp appears briefly in the card's position (~1s display)
  - [x] Use coroutine for animation sequencing
  - [x] File: `Scripts/Runtime/UI/ShopUI.cs` — purchase animation coroutine
- [x] Task 3: Reroll animation (AC: 3)
  - [x] On reroll: existing cards flip/rotate briefly (scale X to 0 then back to 1)
  - [x] During flip, swap card content to new relics
  - [x] Stagger animation slightly across 3 cards for visual interest
  - [x] ~0.4s total animation duration
  - [x] File: `Scripts/Runtime/UI/ShopUI.cs` — reroll animation coroutine
- [x] Task 4: Insider Tip flip animation (AC: 4)
  - [x] Face-down state: dark card background with large "?" text/icon
  - [x] On purchase: card flips (scale X: 1→0, swap content, 0→1) over ~0.6s
  - [x] Face-up state: revealed tip text with tip type header
  - [x] Flip accompanied by subtle "reveal" flash effect
  - [x] File: `Scripts/Runtime/UI/ShopUI.cs` — tip flip animation coroutine
- [x] Task 5: Bond card pulsing glow (AC: 5)
  - [x] Subtle pulsing glow on bond card border (sinusoidal alpha oscillation)
  - [x] Price tag displayed prominently with cash icon (green $)
  - [x] Glow intensity increases slightly on hover
  - [x] File: `Scripts/Runtime/UI/ShopUI.cs` — bond card Update loop
- [x] Task 6: Expansion "OWNED" watermark (AC: 6)
  - [x] After purchase: large semi-transparent "OWNED" text overlaid on expansion card
  - [x] Card content still visible but clearly marked as purchased
  - [x] Transition animation: watermark fades in over ~0.3s after purchase
  - [x] File: `Scripts/Runtime/UI/ShopUI.cs`
- [x] Task 7: Text legibility pass (AC: 7)
  - [x] Ensure all card text has sufficient contrast against dark backgrounds
  - [x] Card names: bold/larger font
  - [x] Descriptions: smaller font, adequate line spacing
  - [x] Costs: prominently sized with currency icon
  - [x] File: `Scripts/Setup/UISetup.cs` — font sizes, colors, text settings
- [x] Task 8: Visual consistency audit (AC: 8)
  - [x] All UI built programmatically with uGUI (no prefabs, no UI Toolkit)
  - [x] Colors consistent with existing game palette (amber/gold for Rep, green for cash)
  - [x] Panel backgrounds use consistent dark card-game aesthetic
  - [x] Animations use standard Unity coroutines (no DOTween or external libraries)
  - [x] File: `Scripts/Setup/UISetup.cs`, `Scripts/Runtime/UI/ShopUI.cs`

## Dev Notes

### Architecture Compliance

- **Programmatic uGUI:** All animations done via coroutines modifying RectTransform and CanvasGroup properties. No Animator controllers, no DOTween.
- **No prefabs:** Card visuals constructed in UISetup code.
- **Performance:** Animations are simple transform/alpha tweaks — no particle systems, no shader effects. Keep it lightweight.

### Animation Approach

All animations should use standard Unity coroutines with `Mathf.Lerp` and `Time.unscaledDeltaTime` (store is untimed, so unscaled time is fine). Pattern:

```csharp
IEnumerator AnimateCardPurchase(RectTransform card, CanvasGroup group)
{
    float duration = 0.5f;
    Vector2 startPos = card.anchoredPosition;
    Vector2 endPos = startPos + Vector2.up * 50f;
    for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
    {
        float p = t / duration;
        card.anchoredPosition = Vector2.Lerp(startPos, endPos, p);
        group.alpha = Mathf.Lerp(1f, 0f, p);
        yield return null;
    }
}
```

### Existing Code to Understand

Before implementing, the dev agent MUST read:
- `Scripts/Runtime/UI/ShopUI.cs` — all card rendering code (after 13.2-13.6 are done)
- `Scripts/Setup/UISetup.cs` — card visual construction, color constants
- `Scripts/Runtime/UI/EventPopup.cs` — reference for existing animation patterns in the game

### Depends On

- Stories 13.2-13.6 — all panels must be functional before polish is added
- Story 13.1 (Data Model) — store state must be working

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Completion Notes List

- **Task 1 (Hover Effects):** Added EventTrigger-based hover detection on relic slots. AnimateHover coroutine lerps scale (1.0→1.05) and card background color (RelicCardColor→RelicCardHoverColor) over 0.15s. Unified relic card border color via RelicCardColor constant — no rarity differentiation. UISetup updated to use ShopUI.RelicCardColor instead of hardcoded value, rarity badge set to ReputationColor, rarity text hidden by default.
- **Task 2 (Purchase Animation):** AnimateCardPurchase coroutine slides card upward + fades out via CanvasGroup alpha over 0.5s (AC 2). SOLD stamp uses ignoreParentGroups to remain visible under zeroed parent CanvasGroup. Creates temporary "SOLD" stamp text (bold, red, 32pt) displayed for 1s before applying final sold state. RefreshAfterPurchase now triggers animation instead of instant state change.
- **Task 3 (Reroll Animation):** AnimateRerollFlip coroutine staggers across non-sold cards (0.08s delay). Each card flips via scaleX 1→0 (content swap at midpoint) →1 over 0.4s total. RefreshRelicOffering now triggers animated reroll instead of instant swap.
- **Task 4 (Tip Flip Animation):** AnimateTipFlip coroutine performs scaleX flip (1→0→1) over 0.6s with content reveal at midpoint. Brief white flash overlay (0.15s fade) after flip for "reveal" feel. CanvasGroup added to TipCardView. Face-down "?" enlarged to 24pt for emphasis.
- **Task 5 (Bond Pulsing Glow):** UpdateBondPulse runs in Update loop using sinusoidal alpha oscillation on Outline component. Hover detection via EventTrigger boosts glow by 0.3 alpha. Bond price text enlarged to 16pt bold for prominence.
- **Task 6 (OWNED Watermark):** AnimateOwnedWatermark coroutine creates semi-transparent (40% alpha) "OWNED" text overlay that fades in over 0.3s. Card content remains visible underneath. SetExpansionCardOwned takes optional animate parameter.
- **Task 7 (Text Legibility):** Relic card names: 16→18pt. Relic descriptions: 12→13pt with improved contrast color and 1.1x line spacing. Relic costs: 20→22pt. Expansion names: 13→14pt. Expansion descriptions: 10→11pt with brighter color. Expansion costs: 12→13pt. Tip card names: 13→14pt. Tip "?" symbol: 16→24pt.
- **Task 8 (Visual Consistency):** Verified all UI built programmatically with uGUI — no prefabs, no UI Toolkit. All animations use standard Unity coroutines with Time.unscaledDeltaTime — no DOTween or external libs. Colors use existing palette: amber/gold (ReputationColor) for Rep, green (CashColor) for cash. All panel backgrounds are dark card-game aesthetic.

### Change Log

- 2026-02-16: Implemented Story 13.8 — all 8 tasks complete. Added hover effects, purchase/reroll/tip-flip animations, bond pulsing glow, OWNED watermark, text legibility improvements, visual consistency audit.
- 2026-02-16: Code review fixes — (1) Fixed SOLD stamp visibility bug (ignoreParentGroups), (2) Fixed purchase animation to slide-up per AC 2 instead of scale-up, (3) Removed Resources.GetBuiltinResource<Font> calls violating project rules, (4) Fixed tip card fontSize not resetting on reveal, (5) Added FormatTipTypeName tests and animation relationship tests, (6) Added lineSpacing to expansion/tip descriptions for AC 7 consistency.

### File List

- `Assets/Scripts/Runtime/UI/ShopUI.cs` (modified) — animation coroutines, hover handlers, bond pulse, watermark, text size increases, code review fixes
- `Assets/Scripts/Setup/UISetup.cs` (modified) — CanvasGroup on relic slots, unified card color, font size/contrast improvements
- `Assets/Tests/Runtime/Shop/StoreVisualPolishTests.cs` (new) — unit tests for animation constants, color definitions, view struct fields, FormatTipTypeName logic tests
