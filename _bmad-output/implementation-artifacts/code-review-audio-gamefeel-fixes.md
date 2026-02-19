# Code Review: Audio + Game Feel Integration — Fix List

**Date:** 2026-02-18
**Scope:** Uncommitted changes — audio wiring hooks + game feel sound integration
**Reviewed files:** AudioManager.cs, GameFeelManager.cs, RoundResultsUI.cs, RunSummaryUI.cs, ShopUI.cs, StockSidebar.cs, TradingHUD.cs, AudioClipLibraryTests.cs, AudioManagerTests.cs

---

## 🔴 HIGH — Must Fix

### H1 — `TradingHUD.cs:269` — Wrong field for profit popup value

`evt.TotalCost` is the gross transaction proceeds (always positive for a sell). The floating popup will always show green and display the wrong dollar amount on loss trades.

**Fix:**
```csharp
// BEFORE
float profit = evt.TotalCost;

// AFTER
float profit = evt.ProfitLoss;
```

---

### H2 — `ShopUI.cs` — `AddButtonHoverFeel` stacks duplicate EventTrigger entries on reroll

`SetupRelicSlot` is called on the same slot objects on every reroll (inside `AnimateSingleRerollFlip`). `AddButtonHoverFeel` appends new `PointerEnter`/`PointerExit` entries to the existing EventTrigger without clearing old ones. After each reroll, one more hover sound + one more competing scale tween fires per hover event.

**Fix:** Clear prior hover entries before adding new ones in `AddButtonHoverFeel`:
```csharp
private static void AddButtonHoverFeel(Button btn)
{
    if (btn == null) return;
    var trigger = btn.gameObject.GetComponent<EventTrigger>()
               ?? btn.gameObject.AddComponent<EventTrigger>();

    // ADD THIS — prevent entry accumulation on repeated calls
    trigger.triggers.RemoveAll(e => e.eventID == EventTriggerType.PointerEnter
                                  || e.eventID == EventTriggerType.PointerExit);

    // ... rest of the method unchanged
```

---

## 🟡 MEDIUM — Should Fix

### M1 — `AudioManager.cs:81` — `Instance` not cleared in `OnDestroy`

C#'s `?.` null-conditional bypasses Unity's fake-null check on destroyed MonoBehaviours. Between run teardown and the next `Initialize`, any UI script calling `AudioManager.Instance?.PlayX()` would hit a destroyed object and throw `MissingReferenceException`.

**Fix:** Add to `OnDestroy` before the unsubscribe block:
```csharp
if (Instance == this) Instance = null;
```

---

### M2 — `AudioManager.cs:25,252` — `_short2CashoutWindowSoundPlayed` is dead code

Field is declared and reset in `OnRoundStarted` but never read or written anywhere else.

**Fix:** Delete the field and its reset:
```csharp
// Remove from field declarations:
private bool _short2CashoutWindowSoundPlayed;

// Remove from OnRoundStarted:
_short2CashoutWindowSoundPlayed = false;
```

---

### M3 — No story tracks these changes

These changes complete "documented for future" items from Story 11.1 (Tasks 9 and 10) but exist outside any story with acceptance criteria. If a regression is introduced there's nothing to check against.

**Fix:** Update Story 11.1's Dev Agent Record + File List to include all modified files (TradingHUD.cs, ShopUI.cs, StockSidebar.cs, RoundResultsUI.cs, RunSummaryUI.cs, GameFeelManager.cs, and the two test files).

---

## 🟢 LOW — Nice to Fix

### L1 — `AudioManagerTests.cs:101` — `Instance_IsNullBeforeInitialize` is a no-op

The test calls `Assert.Pass(...)` unconditionally. It verifies nothing. Either delete it or replace with something meaningful.

### L2 — Hover feel pattern is duplicated across `ShopUI` and `StockSidebar`

Both implement their own EventTrigger hover wiring for `PlayButtonHover`. Low risk now, maintenance burden if the pattern needs updating.

### L3 — Modified test files not in any story's File List

`AudioClipLibraryTests.cs` and `AudioManagerTests.cs` were modified but aren't listed in Story 11.1 or 15.1 File Lists. Update whichever story claims these tests.

---

## Fix Priority Order

1. **H1** — Wrong profit value shown to player (data correctness)
2. **H2** — Audio/animation stacking on reroll (gameplay bug)
3. **M1** — Instance null safety (stability)
4. **M2** — Dead code cleanup (1 line delete)
5. **M3** — Story documentation update
6. **L1/L2/L3** — Test and doc cleanup
