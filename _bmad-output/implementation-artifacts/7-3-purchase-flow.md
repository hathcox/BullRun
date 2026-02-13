# Story 7.3: Purchase Flow

Status: ready-for-dev

## Story

As a player,
I want to buy items with my accumulated cash and carry unspent cash forward,
so that there is tension between upgrading and capital.

## Acceptance Criteria

1. Purchase deducts item cost from cash atomically (full cost or nothing)
2. Can buy any combination (0-3 items) that the player can afford
3. Unspent cash becomes trading capital for the next round
4. Cannot buy items that exceed available cash (purchase button disabled)
5. 15-20 second shop timer auto-closes shop when expired
6. Purchased items are added to RunContext.ActiveItems
7. ShopClosedEvent includes what was purchased and cash remaining
8. ShopItemPurchasedEvent fires for each individual purchase with item id, cost, and remaining cash

## Tasks / Subtasks

- [ ] Task 1: Create ShopTransaction class for atomic purchase logic (AC: 1, 4, 6)
  - [ ] Create `Scripts/Runtime/Shop/ShopTransaction.cs`
  - [ ] Pure C# class (no MonoBehaviour) — follows TradeExecutor, EventEffects pattern
  - [ ] `TryPurchase(RunContext ctx, ShopItemDef item)` returns `ShopPurchaseResult` (Success/InsufficientFunds/AlreadyOwned)
  - [ ] Validate cash: `ctx.Portfolio.CanAfford(item.Cost)` — reject if false
  - [ ] Validate not already owned: check `ctx.ActiveItems.Contains(item.Id)` — reject duplicate purchases
  - [ ] On success: call `ctx.Portfolio.DeductCash(item.Cost)` then `ctx.ActiveItems.Add(item.Id)`
  - [ ] Publish `ShopItemPurchasedEvent` via EventBus on successful purchase
  - [ ] Try-catch at system boundary — recoverable errors skip the purchase and log warning
  - [ ] `ShopPurchaseResult` enum: `Success`, `InsufficientFunds`, `AlreadyOwned`, `Error`

- [ ] Task 2: Add ShopTimerDurationSeconds to GameConfig (AC: 5)
  - [ ] Add `public static readonly float ShopTimerDurationSeconds = 18f;` to `Scripts/Setup/Data/GameConfig.cs`
  - [ ] 18 seconds is the midpoint of the 15-20 second GDD range
  - [ ] This value is used by ShopState for countdown and by ShopUI for timer display

- [ ] Task 3: Add shop events to GameEvents.cs (AC: 7, 8)
  - [ ] Add `ShopOpenedEvent` struct: `int RoundNumber`, `string[] AvailableItemIds`, `float CurrentCash`
  - [ ] Add `ShopItemPurchasedEvent` struct: `string ItemId`, `string ItemName`, `int Cost`, `float RemainingCash`
  - [ ] Add `ShopClosedEvent` struct: `string[] PurchasedItemIds`, `float CashRemaining`, `int RoundNumber`, `bool TimerExpired`
  - [ ] Follow existing naming convention: `{Subject}{Verb}Event`
  - [ ] File: `Scripts/Runtime/Core/GameEvents.cs` (modify)

- [ ] Task 4: Implement ShopState timer and purchase handling (AC: 2, 3, 5)
  - [ ] Replace placeholder auto-skip logic in `ShopState.Enter()` with actual shop display
  - [ ] Add `_shopTimer` field (float, counts down from `GameConfig.ShopTimerDurationSeconds`)
  - [ ] Add `_shopItems` field to hold the 3 generated items for this shop visit
  - [ ] Add `_purchasedItems` list to track purchases during this shop visit
  - [ ] Add `_shopTransaction` field (ShopTransaction instance)
  - [ ] In `Enter()`: generate shop items via ShopGenerator, publish `ShopOpenedEvent`, show ShopUI
  - [ ] In `Update()`: decrement `_shopTimer` by `Time.deltaTime`, check for expiry
  - [ ] When timer expires: call `CloseShop()` — publish `ShopClosedEvent`, transition to next state
  - [ ] Add `OnPurchaseRequested(string itemId)` method — called when player clicks a buy button
  - [ ] On purchase: delegate to `ShopTransaction.TryPurchase()`, update ShopUI card states (disable bought/unaffordable)
  - [ ] Add `CloseShop()` method to handle transition logic (currently in Enter — must be moved to close flow)
  - [ ] Move round advancement logic (AdvanceRound, act transition check, etc.) from Enter() to CloseShop()
  - [ ] Add "Done" button support: player can close shop early before timer expires
  - [ ] File: `Scripts/Runtime/Core/GameStates/ShopState.cs` (modify)

- [ ] Task 5: Wire ShopUI purchase buttons to ShopState (AC: 2, 4, 6)
  - [ ] ShopUI already has purchase buttons from Story 7-1 — wire `onClick` to ShopState.OnPurchaseRequested
  - [ ] Add `SetItems(ShopItemDef[] items, float currentCash)` method to populate the 3 card slots
  - [ ] Add `UpdateAffordability(float currentCash)` method — enables/disables buy buttons based on remaining cash
  - [ ] Add `MarkItemPurchased(string itemId)` method — visually marks a card as purchased (button disabled, "PURCHASED" overlay)
  - [ ] Add `UpdateTimer(float remainingSeconds)` method — updates countdown display
  - [ ] Add `SetOnPurchaseCallback(Action<string> callback)` — ShopState registers its purchase handler
  - [ ] Add `SetOnCloseCallback(Action callback)` — ShopState registers the "Done" button handler
  - [ ] Cash display updates after each purchase to show updated balance
  - [ ] File: `Scripts/Runtime/UI/ShopUI.cs` (modify)

- [ ] Task 6: Update ShopStateConfig with new dependencies (AC: 1, 2)
  - [ ] Add `ShopGenerator` field to `ShopStateConfig` (needed to generate items)
  - [ ] Add `ShopTransaction` field to `ShopStateConfig` (or create inline — depends on testability preference)
  - [ ] Update all call sites that create `ShopStateConfig` (MarginCallState.cs)
  - [ ] Ensure ShopGenerator and ShopTransaction instances are created in setup/wiring code
  - [ ] File: `Scripts/Runtime/Core/GameStates/ShopState.cs` (modify config class)
  - [ ] File: `Scripts/Runtime/Core/GameStates/MarginCallState.cs` (modify config creation)

- [ ] Task 7: Write comprehensive tests (AC: 1-8)
  - [ ] Test: ShopTransaction.TryPurchase deducts exact cost from cash on success
  - [ ] Test: ShopTransaction.TryPurchase returns InsufficientFunds when cash < cost
  - [ ] Test: ShopTransaction.TryPurchase returns AlreadyOwned when item already in ActiveItems
  - [ ] Test: ShopTransaction.TryPurchase adds item id to RunContext.ActiveItems on success
  - [ ] Test: ShopTransaction.TryPurchase publishes ShopItemPurchasedEvent on success
  - [ ] Test: Can purchase 0 items (just let timer expire or click Done)
  - [ ] Test: Can purchase 1, 2, or 3 items in any combination
  - [ ] Test: Cannot purchase item when exact cash equals cost minus one cent (boundary)
  - [ ] Test: Can purchase item when exact cash equals cost (boundary)
  - [ ] Test: Cash after purchases carries forward (unspent cash = trading capital)
  - [ ] Test: ShopState timer counts down and triggers close at zero
  - [ ] Test: ShopClosedEvent contains correct purchased item ids and remaining cash
  - [ ] Test: Multiple purchases update affordability (buying item A may make item C unaffordable)
  - [ ] File: `Tests/Runtime/Shop/ShopTransactionTests.cs` (new)
  - [ ] File: `Tests/Runtime/Core/GameStates/ShopStateTests.cs` (modify — add timer + purchase tests)

## Dev Notes

### Architecture Compliance

- **ShopTransaction is a pure C# class** (no MonoBehaviour) — follows the established pattern of `TradeExecutor`, `EventEffects`, `PriceGenerator`. Highly testable without Unity runtime.
- **EventBus communication:** Purchase events published via EventBus. ShopState does NOT directly reference ShopUI for updates — UI subscribes to events or receives callbacks via delegate injection.
- **Static Data:** Shop timer duration in `GameConfig` as `public static readonly` — no ScriptableObjects.
- **Error handling:** Try-catch at system boundary (ShopTransaction.TryPurchase). Recoverable errors skip the purchase operation and log a warning. The shop continues functioning even if one purchase fails.
- **Atomic transactions:** Cash deduction and item addition must happen together. Use `Portfolio.DeductCash()` first (which returns false on failure), then add to `ActiveItems` only on success. This prevents partial state.
- **RunContext is the single source of truth:** All cash managed through `RunContext.Portfolio.Cash`. All items tracked in `RunContext.ActiveItems`. No parallel state.

### Purchase Flow Sequence

```
Player clicks Buy button on ShopUI
  └── ShopUI calls OnPurchaseCallback(itemId)
        └── ShopState.OnPurchaseRequested(itemId)
              └── ShopTransaction.TryPurchase(ctx, itemDef)
                    ├── Validate: ctx.Portfolio.CanAfford(item.Cost)
                    ├── Validate: !ctx.ActiveItems.Contains(item.Id)
                    ├── ctx.Portfolio.DeductCash(item.Cost)
                    ├── ctx.ActiveItems.Add(item.Id)
                    ├── EventBus.Publish(ShopItemPurchasedEvent)
                    └── Return ShopPurchaseResult.Success
              └── ShopState updates ShopUI:
                    ├── MarkItemPurchased(itemId)
                    └── UpdateAffordability(ctx.Portfolio.Cash)
```

### Shop Timer Flow

```
ShopState.Enter()
  ├── _shopTimer = GameConfig.ShopTimerDurationSeconds (18s)
  ├── Generate items, show UI, publish ShopOpenedEvent
  └── Player can purchase items during this window

ShopState.Update() (every frame)
  ├── _shopTimer -= Time.deltaTime
  ├── ShopUI.UpdateTimer(_shopTimer)
  └── if (_shopTimer <= 0f) → CloseShop()

ShopState.CloseShop() (timer expired OR "Done" clicked)
  ├── Publish ShopClosedEvent (items purchased, cash remaining)
  ├── Advance round (ctx.AdvanceRound())
  ├── ctx.Portfolio.StartRound(ctx.Portfolio.Cash)
  └── Transition to next state (MarketOpen, TierTransition, or RunSummary)
```

### Capital Tension Design

This is the core mechanic of the shop system. From GDD Section 4: "Every dollar spent on upgrades is a dollar not available for trading in the next round."

The implementation makes this explicit:
- Cash before shop = round earnings (post-liquidation cash)
- Cash after shop = round earnings minus purchases = next round's trading capital
- Player must weigh: "$300 item vs. $300 more trading capital"
- Items do NOT produce immediate value (effects are Epic 8) — pure cost during shop

Example scenario:
- Player has $1,500 after Round 1
- Shop offers: $200 item, $350 item, $500 item
- If player buys all three ($1,050): starts Round 2 with only $450
- If player buys none: starts Round 2 with full $1,500
- If player buys one ($350): starts Round 2 with $1,150

### ShopState Refactoring Notes

The current `ShopState.Enter()` contains round advancement logic (AdvanceRound, act transition check, run completion check) that executes immediately. This story must:

1. **Move round advancement from Enter() to CloseShop()** — the shop must persist for 15-20 seconds before advancing
2. **Add Update() logic** — currently empty, needs timer countdown
3. **Keep the same transition routing** — the existing MarketOpen/TierTransition/RunSummary routing logic is correct, just needs to be called after shop closes instead of immediately

The existing transition pattern in ShopState (lines 40-80) is well-structured and should be preserved as-is, just relocated to `CloseShop()`.

### Existing Infrastructure (DO NOT recreate)

| Component | File | Status |
|-----------|------|--------|
| `Portfolio.CanAfford()` | `Runtime/Trading/Portfolio.cs` | Complete — validates cash >= cost |
| `Portfolio.DeductCash()` | `Runtime/Trading/Portfolio.cs` | Complete — deducts and returns bool |
| `RunContext.ActiveItems` | `Runtime/Core/RunContext.cs` | Complete — `List<string>` for item ids |
| `RunContext.ItemsCollected` | `Runtime/Core/RunContext.cs` | Complete — computed property from ActiveItems.Count |
| `ShopState` (placeholder) | `Runtime/Core/GameStates/ShopState.cs` | Exists — auto-skips, needs real implementation |
| `ShopUI` | `Runtime/UI/ShopUI.cs` | Exists from 7-1 — needs button wiring |
| `ShopGenerator` | `Runtime/Shop/ShopGenerator.cs` | Exists from 7-1/7-2 — generates 3 items |
| `ShopItemDefinitions` | `Setup/Data/ShopItemDefinitions.cs` | Exists from 7-1 — 30 items defined |
| `EventBus` | `Runtime/Core/EventBus.cs` | Complete — typed publish/subscribe |
| `GameConfig` | `Setup/Data/GameConfig.cs` | Complete — needs ShopTimerDurationSeconds added |

### Key Patterns to Follow

**Static NextConfig pattern** (used by all GameStates):
```csharp
public static ShopStateConfig NextConfig;

public void Enter(RunContext ctx)
{
    if (NextConfig != null)
    {
        _stateMachine = NextConfig.StateMachine;
        // ... assign fields ...
        NextConfig = null;
    }
}
```

**Debug logging pattern** (conditional compilation):
```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
Debug.Log($"[ShopState] Item purchased: {item.Name} for ${item.Cost} (remaining: ${ctx.Portfolio.Cash:F2})");
#endif
```

**Event struct pattern** (from GameEvents.cs):
```csharp
public struct ShopItemPurchasedEvent
{
    public string ItemId;
    public string ItemName;
    public int Cost;
    public float RemainingCash;
}
```

### Edge Cases

- **Timer expires mid-purchase:** Timer check is in Update(), purchase is synchronous — no race condition possible since both run on main thread
- **All items unaffordable:** Valid state — player simply waits or clicks Done. No items purchased, full cash carries forward
- **Exact cash match:** Player has exactly $300, item costs $300 — `CanAfford()` returns true (uses `>=`), purchase succeeds, player starts next round with $0
- **Zero cash:** Player can still enter shop, just can't buy anything. Timer runs, shop closes, next round starts with $0
- **Final round shop:** ShopState already handles final-round detection in its close/transition logic — if `ctx.IsRunComplete()` after AdvanceRound, routes to RunSummaryState

### Previous Story Learnings

- From 7-1: ShopState, ShopUI, ShopGenerator, ShopItemDefinitions, and shop events all established. This story builds on those foundations.
- From 7-2: ShopGenerator enhanced with rarity-weighted selection and duplicate prevention. Items are already properly generated.
- From 6-5: RunContext properties with computed accessors (ItemsCollected). Code review emphasized computed properties over manual tracking.
- From 5-1: Pure C# class pattern for game logic (EventScheduler). ShopTransaction follows the same pattern.
- From 2-5: Portfolio.DeductCash() and Portfolio.CanAfford() already exist and are tested. Use them directly.

### Project Structure Notes

- New file: `Assets/Scripts/Runtime/Shop/ShopTransaction.cs`
- New file: `Assets/Tests/Runtime/Shop/ShopTransactionTests.cs`
- Modified: `Assets/Scripts/Setup/Data/GameConfig.cs` (add ShopTimerDurationSeconds)
- Modified: `Assets/Scripts/Runtime/Core/GameEvents.cs` (add ShopOpenedEvent, ShopItemPurchasedEvent, ShopClosedEvent)
- Modified: `Assets/Scripts/Runtime/Core/GameStates/ShopState.cs` (timer, purchase handling, refactor Enter→CloseShop)
- Modified: `Assets/Scripts/Runtime/UI/ShopUI.cs` (wire buttons, affordability, timer display)
- Modified: `Assets/Scripts/Runtime/Core/GameStates/MarginCallState.cs` (update ShopStateConfig creation)
- Modified: `Assets/Tests/Runtime/Core/GameStates/ShopStateTests.cs` (add timer and purchase tests)

### References

- [Source: bull-run-gdd-mvp.md#4] — "Every dollar spent on upgrades is a dollar not available for trading in the next round"
- [Source: bull-run-gdd-mvp.md#4] — "Items costs: $100-$600 range"
- [Source: bull-run-gdd-mvp.md#2.2] — "Phase 3: Market Close & Draft Shop (15-20 seconds)"
- [Source: bull-run-gdd-mvp.md#2.2] — "Player may purchase any combination they can afford"
- [Source: bull-run-gdd-mvp.md#2.2] — "Unspent cash carries forward as trading capital"
- [Source: epics.md#7.3] — Story 7.3 acceptance criteria
- [Source: game-architecture.md#Shop System] — ShopTransaction location: `Scripts/Runtime/Shop/`
- [Source: game-architecture.md#Error Handling] — "try-catch at system boundary (shop purchase), recoverable errors skip operation"
- [Source: game-architecture.md#Event System] — "Central event bus with typed events, synchronous dispatch"
- [Source: Portfolio.cs] — `CanAfford()` and `DeductCash()` already implemented for this exact use case
- [Source: RunContext.cs] — `ActiveItems` list and `ItemsCollected` computed property ready for shop items

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### File List
