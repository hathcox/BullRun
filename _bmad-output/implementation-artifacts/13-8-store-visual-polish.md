# Story 13.8: Store Visual Polish & Card Animations

Status: pending

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

- [ ] Task 1: Relic card hover effects (AC: 1)
  - [ ] Glow effect on mouse hover (scale up slightly + border brightness increase)
  - [ ] Consistent card border color across all relics (no rarity differentiation)
  - [ ] Smooth hover transition (lerp scale and color over ~0.15s)
  - [ ] File: `Scripts/Runtime/UI/ShopUI.cs` — hover event handlers
- [ ] Task 2: Purchase animation (AC: 2)
  - [ ] On purchase: card slides upward + fades out over ~0.5s
  - [ ] "SOLD" stamp appears briefly in the card's position (~1s display)
  - [ ] Use coroutine for animation sequencing
  - [ ] File: `Scripts/Runtime/UI/ShopUI.cs` — purchase animation coroutine
- [ ] Task 3: Reroll animation (AC: 3)
  - [ ] On reroll: existing cards flip/rotate briefly (scale X to 0 then back to 1)
  - [ ] During flip, swap card content to new relics
  - [ ] Stagger animation slightly across 3 cards for visual interest
  - [ ] ~0.4s total animation duration
  - [ ] File: `Scripts/Runtime/UI/ShopUI.cs` — reroll animation coroutine
- [ ] Task 4: Insider Tip flip animation (AC: 4)
  - [ ] Face-down state: dark card background with large "?" text/icon
  - [ ] On purchase: card flips (scale X: 1→0, swap content, 0→1) over ~0.6s
  - [ ] Face-up state: revealed tip text with tip type header
  - [ ] Flip accompanied by subtle "reveal" flash effect
  - [ ] File: `Scripts/Runtime/UI/ShopUI.cs` — tip flip animation coroutine
- [ ] Task 5: Bond card pulsing glow (AC: 5)
  - [ ] Subtle pulsing glow on bond card border (sinusoidal alpha oscillation)
  - [ ] Price tag displayed prominently with cash icon (green $)
  - [ ] Glow intensity increases slightly on hover
  - [ ] File: `Scripts/Runtime/UI/ShopUI.cs` — bond card Update loop
- [ ] Task 6: Expansion "OWNED" watermark (AC: 6)
  - [ ] After purchase: large semi-transparent "OWNED" text overlaid on expansion card
  - [ ] Card content still visible but clearly marked as purchased
  - [ ] Transition animation: watermark fades in over ~0.3s after purchase
  - [ ] File: `Scripts/Runtime/UI/ShopUI.cs`
- [ ] Task 7: Text legibility pass (AC: 7)
  - [ ] Ensure all card text has sufficient contrast against dark backgrounds
  - [ ] Card names: bold/larger font
  - [ ] Descriptions: smaller font, adequate line spacing
  - [ ] Costs: prominently sized with currency icon
  - [ ] File: `Scripts/Setup/UISetup.cs` — font sizes, colors, text settings
- [ ] Task 8: Visual consistency audit (AC: 8)
  - [ ] All UI built programmatically with uGUI (no prefabs, no UI Toolkit)
  - [ ] Colors consistent with existing game palette (amber/gold for Rep, green for cash)
  - [ ] Panel backgrounds use consistent dark card-game aesthetic
  - [ ] Animations use standard Unity coroutines (no DOTween or external libraries)
  - [ ] File: `Scripts/Setup/UISetup.cs`, `Scripts/Runtime/UI/ShopUI.cs`

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
- `Scripts/Runtime/UI/ShopUI.cs` — all card rendering code (after 13.1-13.5 are done)
- `Scripts/Setup/UISetup.cs` — card visual construction, color constants
- `Scripts/Runtime/UI/EventPopup.cs` — reference for existing animation patterns in the game

### Depends On

- Stories 13.1-13.5 — all panels must be functional before polish is added
- Story 13.6 (Data Model) — store state must be working

## Dev Agent Record

### Agent Model Used

### Completion Notes List

### Change Log

### File List
