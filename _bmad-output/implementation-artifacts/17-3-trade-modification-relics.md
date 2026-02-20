# Story 17.3: Trade Modification Relics (5 Relics)

Status: ready-for-dev

## Story

As a player,
I want relics that fundamentally change how I trade — doubling shares, modifying cooldowns, boosting shorts, or skimming profits,
so that my build strategy meaningfully alters my trading phase gameplay.

## Acceptance Criteria

1. **Double Dealer** (`relic_double_dealer`, `DoubleDealerRelic.cs`): When the player executes a buy or sell trade, the trade quantity is doubled (e.g., buying 5 shares actually buys 10). Stacks with the current quantity selector value. Cost/proceeds reflect the doubled quantity.
2. **Quick Draw** (`relic_quick_draw`, `QuickDrawRelic.cs`): Buying has 0s post-trade cooldown (instant). Selling has 2x the normal cooldown (`GameConfig.PostTradeCooldown * 2`). The cooldown timer display reflects modified durations.
3. **Bear Raid** (`relic_short_multiplier`, `ShortMultiplierRelic.cs`): Shorts execute 3 shares instead of the base 1. Buy and sell (longs) are permanently disabled for the rest of the run. OnAcquired immediately disables buy/sell if mid-round. Disabled buttons show a visual indicator (e.g., "LOCKED"). Keyboard shortcuts B/S are blocked.
4. **Skimmer** (`relic_skimmer`, `SkimmerRelic.cs`): On buy trade, 3% of total trade value (shares x price) is instantly added to the player's cash. A brief "+$X.XX" feedback displays via TradeFeedbackEvent.
5. **Short Profiteer** (`relic_short_profiteer`, `ShortProfiteerRelic.cs`): On short open, 10% of stock value (shares x current price) is instantly added to the player's cash. A brief "+$X.XX" feedback displays via TradeFeedbackEvent.
6. `RelicManager` exposes a `GetEffectiveCooldown(bool isBuy)` helper method that GameRunner calls instead of using `GameConfig.PostTradeCooldown` directly. Returns 0 for buy if Quick Draw owned, returns `PostTradeCooldown * 2` for sell if Quick Draw owned, otherwise returns the default.
7. `RelicManager` exposes a `GetEffectiveShortShares()` helper method that returns 3 if Bear Raid owned, otherwise returns `GameConfig.ShortBaseShares`.
8. `RunContext` gains a `LongsDisabled` bool flag, defaulting to false. GameRunner checks this flag before executing buy/sell trades and disables the buttons accordingly.
9. All 5 relic constructors registered in `RelicFactory` (replacing stub entries).
10. Each relic fires `RelicActivatedEvent` when its effect triggers (for future UI glow in Story 17.8).

## Tasks / Subtasks

- [ ] Task 1: Add RunContext and GameRunner modifications (AC: 6, 7, 8)
  - [ ] Add `bool LongsDisabled` property to `RunContext` (default false), reset in `ResetForNewRun()`
  - [ ] In `GameRunner`, replace direct `GameConfig.PostTradeCooldown` usage with `ctx.RelicManager.GetEffectiveCooldown(isBuy)` call
  - [ ] In `GameRunner`, check `ctx.LongsDisabled` before executing buy/sell trades — show "LOCKED" feedback and return early if true
  - [ ] In `GameRunner`, block keyboard shortcuts B/S when `ctx.LongsDisabled` is true
  - [ ] In `GameRunner`, replace `GameConfig.ShortBaseShares` with `ctx.RelicManager.GetEffectiveShortShares()` for short share count
  - [ ] Dim/disable buy and sell buttons visually when `LongsDisabled` is true
  - [ ] File: `Scripts/Runtime/Core/RunContext.cs`, `Scripts/Runtime/Core/GameRunner.cs`

- [ ] Task 2: Add RelicManager helper methods (AC: 6, 7)
  - [ ] Add `GetEffectiveCooldown(bool isBuy)` to `RelicManager` — iterates relics checking for Quick Draw, returns modified cooldown
  - [ ] Add `GetEffectiveShortShares()` to `RelicManager` — iterates relics checking for Bear Raid, returns modified share count
  - [ ] Both methods use `GetRelicById()` internally to check if specific relics are owned
  - [ ] File: `Scripts/Runtime/Items/RelicManager.cs`

- [ ] Task 3: Implement Double Dealer relic (AC: 1, 9, 10)
  - [ ] Create `DoubleDealerRelic.cs` extending `RelicBase`
  - [ ] Override `Id` to return `"relic_double_dealer"`
  - [ ] Override `OnBeforeTrade` — doubles the trade quantity before execution
  - [ ] Quantity doubling must stack with the current quantity selector value (multiply by 2)
  - [ ] Publish `RelicActivatedEvent` when effect fires
  - [ ] File: `Scripts/Runtime/Items/Relics/DoubleDealerRelic.cs`

- [ ] Task 4: Implement Quick Draw relic (AC: 2, 9, 10)
  - [ ] Create `QuickDrawRelic.cs` extending `RelicBase`
  - [ ] Override `Id` to return `"relic_quick_draw"`
  - [ ] Effect is passive — `RelicManager.GetEffectiveCooldown()` checks for this relic's presence
  - [ ] No hook override needed; the relic's existence is what matters (queried by RelicManager helper)
  - [ ] File: `Scripts/Runtime/Items/Relics/QuickDrawRelic.cs`

- [ ] Task 5: Implement Bear Raid relic (AC: 3, 9, 10)
  - [ ] Create `ShortMultiplierRelic.cs` extending `RelicBase`
  - [ ] Override `Id` to return `"relic_short_multiplier"`
  - [ ] Override `OnAcquired` — sets `ctx.LongsDisabled = true`
  - [ ] Effect on short shares is passive — `RelicManager.GetEffectiveShortShares()` checks for this relic's presence
  - [ ] File: `Scripts/Runtime/Items/Relics/ShortMultiplierRelic.cs`

- [ ] Task 6: Implement Skimmer relic (AC: 4, 9, 10)
  - [ ] Create `SkimmerRelic.cs` extending `RelicBase`
  - [ ] Override `Id` to return `"relic_skimmer"`
  - [ ] Override `OnAfterTrade` — check `e.IsBuy && !e.IsShort`
  - [ ] Calculate bonus: `e.TotalCost * 0.03f`
  - [ ] Add bonus to `ctx.Portfolio.AddCash()` (or direct `Portfolio.Cash +=` depending on API)
  - [ ] Publish `TradeFeedbackEvent` with "+$X.XX" message and `IsSuccess = true`
  - [ ] Publish `RelicActivatedEvent` with relic Id
  - [ ] File: `Scripts/Runtime/Items/Relics/SkimmerRelic.cs`

- [ ] Task 7: Implement Short Profiteer relic (AC: 5, 9, 10)
  - [ ] Create `ShortProfiteerRelic.cs` extending `RelicBase`
  - [ ] Override `Id` to return `"relic_short_profiteer"`
  - [ ] Override `OnAfterTrade` — check `e.IsShort && !e.IsBuy` (short open, not cover)
  - [ ] Calculate bonus: `e.TotalCost * 0.10f`
  - [ ] Add bonus to `ctx.Portfolio.AddCash()` (or direct `Portfolio.Cash +=` depending on API)
  - [ ] Publish `TradeFeedbackEvent` with "+$X.XX" message and `IsSuccess = true`
  - [ ] Publish `RelicActivatedEvent` with relic Id
  - [ ] File: `Scripts/Runtime/Items/Relics/ShortProfiteerRelic.cs`

- [ ] Task 8: Register relics in RelicFactory (AC: 9)
  - [ ] Replace stub registrations for `relic_double_dealer`, `relic_quick_draw`, `relic_short_multiplier`, `relic_skimmer`, `relic_short_profiteer` with real constructors
  - [ ] File: `Scripts/Runtime/Items/RelicFactory.cs`

- [ ] Task 9: Write tests (AC: 1-10)
  - [ ] Double Dealer: trade quantity doubled, stacks with quantity selector, buy and sell both affected
  - [ ] Quick Draw: buy cooldown returns 0, sell cooldown returns 2x, other relics unaffected
  - [ ] Bear Raid: LongsDisabled set on acquire, short shares return 3, buy/sell blocked
  - [ ] Skimmer: 3% of buy trade value added to cash, no effect on sell, TradeFeedbackEvent published
  - [ ] Short Profiteer: 10% of short open value added to cash, no effect on buy/sell/cover
  - [ ] GetEffectiveCooldown: returns default when no relics, returns modified when Quick Draw owned
  - [ ] GetEffectiveShortShares: returns 1 when no relics, returns 3 when Bear Raid owned
  - [ ] Files: `Tests/Runtime/Items/Relics/TradeRelicTests.cs`

## Dev Notes

### Architecture Compliance

- **One class per relic:** Each relic is a separate file in `Scripts/Runtime/Items/Relics/` following the project pattern from Story 17.1.
- **EventBus communication:** Relics publish `TradeFeedbackEvent` and `RelicActivatedEvent` via EventBus — never reference UI systems directly.
- **No ScriptableObjects:** Relic data remains as `public static readonly` in `ShopItemDefinitions.cs` (Story 17.2 handles definitions).
- **RunContext as data carrier:** `LongsDisabled` flag lives on RunContext (the central run state carrier). Relics receive RunContext via method parameters.
- **Passive query pattern:** Quick Draw and Bear Raid use a passive presence-check pattern — RelicManager helper methods query whether the relic exists in the ordered list rather than relics storing state on themselves.

### Existing Code to Read Before Implementing

- `Scripts/Runtime/Items/RelicBase.cs` — base class with virtual no-op hooks (from Story 17.1)
- `Scripts/Runtime/Items/RelicManager.cs` — dispatch methods, AddRelic/RemoveRelic, ordered list (from Story 17.1)
- `Scripts/Runtime/Items/RelicFactory.cs` — static registry of relic ID to constructor mapping (from Story 17.1)
- `Scripts/Runtime/Core/RunContext.cs` — central run state; add `LongsDisabled` flag here
- `Scripts/Runtime/Core/GameRunner.cs` — post-trade cooldown logic (`_postTradeCooldownTimer`, `GameConfig.PostTradeCooldown`), short execution (`GameConfig.ShortBaseShares`), buy/sell button management
- `Scripts/Runtime/Core/GameEvents.cs` — `TradeExecutedEvent` struct (has `IsBuy`, `IsShort`, `TotalCost`, `ProfitLoss`), `TradeFeedbackEvent`, `RelicActivatedEvent`
- `Scripts/Setup/Data/GameConfig.cs` — `PostTradeCooldown = 1.0f`, `ShortBaseShares = 1`
- `Scripts/Runtime/UI/QuantitySelector.cs` — current quantity value that Double Dealer multiplies

### Key Design Decisions

- **GetEffectiveCooldown returns float:** GameRunner replaces `GameConfig.PostTradeCooldown` with this method call. The method checks for Quick Draw and potentially other future cooldown-modifying relics.
- **GetEffectiveShortShares returns int:** GameRunner replaces `GameConfig.ShortBaseShares` with this method call. Bear Raid sets it to 3.
- **Double Dealer modifies trade event:** Uses `OnBeforeTrade` hook so the trade executor receives the modified quantity. The `TradeExecutedEvent.Shares` field should reflect the doubled amount.
- **Skimmer and Short Profiteer use OnAfterTrade:** Cash bonus is added after the trade succeeds, ensuring no bonus on failed trades.

### Depends On

- Story 17.1 (Relic Effect Framework) — IRelic, RelicBase, RelicManager, RelicFactory must exist
- Story 17.2 (Shop Behavior & Data Overhaul) — relic definitions with IDs registered in ShopItemDefinitions

### References

- [Source: _bmad-output/planning-artifacts/epic-17-relic-system.md#Story 17.3]
- [Source: _bmad-output/implementation-artifacts/17-1-relic-effect-framework.md]
- [Source: Scripts/Setup/Data/GameConfig.cs — PostTradeCooldown, ShortBaseShares]
- [Source: Scripts/Runtime/Core/GameEvents.cs — TradeExecutedEvent, TradeFeedbackEvent]

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
