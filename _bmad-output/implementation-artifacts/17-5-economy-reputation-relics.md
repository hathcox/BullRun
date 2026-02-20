# Story 17.5: Economy & Reputation Relics (6 Relics)

Status: done

## Story

As a player,
I want relics that boost my Reputation economy — doubling rep gains, earning rep interest, converting rep to cash, or gaining bonds,
so that I can build a powerful economic engine across my run.

## Acceptance Criteria

1. **Rep Doubler** (`relic_rep_doubler`, `RepDoublerRelic.cs`): Double the Reputation earned from trade performance at round end (`RoundCompletedEvent`). Both base and bonus Rep are doubled. Does NOT affect bond Rep payouts, Rep Interest, or other non-trade Rep sources.
2. **Fail Forward** (`relic_fail_forward`, `FailForwardRelic.cs`): When a round ends in a margin call (`MarginCallTriggeredEvent`), the player still earns the base Reputation that would have been awarded for that round. A "Fail Forward: +X Rep" message displays on the margin call screen.
3. **Compound Rep** (`relic_compound_rep`, `CompoundRepRelic.cs`): Tracks `_roundsHeld` internally, incremented each round via OnRoundStart. When sold, grants `3 * (int)Math.Pow(2, _roundsHeld)` Reputation instead of the normal 50% cost refund. The relic's tooltip dynamically shows the current sell value.
4. **Rep Interest** (`relic_rep_interest`, `RepInterestRelic.cs`): At round start, gain 10% of current Reputation as bonus Rep (rounded down via integer division). A "+X Rep Interest" message displays during market open phase. Stacks with bond Rep payouts (both happen at round start).
5. **Rep Dividend** (`relic_rep_dividend`, `RepDividendRelic.cs`): At round start, gain $1 cash for every 2 Reputation the player currently has (integer division). A "+$X Dividend" message displays during market open phase. Cash is added to portfolio before trading begins.
6. **Bond Bonus** (`relic_bond_bonus`, `BondBonusRelic.cs`): On acquire, `RunContext.BondsOwned` is increased by 10 and 10 synthetic `BondRecord` entries are added to `BondPurchaseHistory` (round = current round). On sell (OnSellSelf), `RunContext.BondsOwned` is decreased by 10 (minimum 0) and 10 bond records are removed LIFO from `BondPurchaseHistory`.
7. `ShopTransaction.SellRelic` checks for the `OnSellSelf` override — if the relic's `OnSellSelf` returns a custom refund value, use that instead of the default 50% cost refund. Compound Rep returns its calculated value; Bond Bonus returns the normal 50%.
8. `GameRunner` round-end Rep calculation checks `RelicManager` for Rep Doubler presence and multiplies Rep by 2 if owned.
9. `GameRunner` margin call handler checks `RelicManager` for Fail Forward presence and awards base Rep if owned.
10. All 6 relic constructors registered in `RelicFactory` (replacing stub entries).
11. Each relic fires `RelicActivatedEvent` when its effect triggers (for future UI glow in Story 17.8).

## Tasks / Subtasks

- [x] Task 1: Add GameRunner modifications for Rep Doubler and Fail Forward (AC: 1, 2, 8, 9)
  - [x] In GameRunner's round-end Rep calculation (near `RoundCompletedEvent` publishing), check if `ctx.RelicManager.GetRelicById("relic_rep_doubler") != null`
  - [x] If Rep Doubler is owned, multiply the total Rep earned (base + bonus) by 2 before awarding
  - [x] Ensure the doubled Rep is reflected in `RoundCompletedEvent.RepEarned`, `.BaseRep`, `.BonusRep`
  - [x] In GameRunner's margin call handler (where `MarginCallTriggeredEvent` is published), check if `ctx.RelicManager.GetRelicById("relic_fail_forward") != null`
  - [x] If Fail Forward is owned, calculate and award base Rep for the current round despite the margin call
  - [x] Publish a `TradeFeedbackEvent` with "Fail Forward: +X Rep" message
  - [x] File: `Scripts/Runtime/Core/GameStates/MarginCallState.cs` (actual location of Rep calculation)

- [x] Task 2: Add ShopTransaction sell override for Compound Rep (AC: 3, 7)
  - [x] In `ShopTransaction.SellRelic()`, after finding the relic, call `relic.GetSellValue(ctx)` on the IRelic instance via RelicManager
  - [x] Add `int? GetSellValue(RunContext ctx)` method to IRelic returning null for default behavior
  - [x] If `GetSellValue` returns a custom value, use that instead of `relicDef.Cost / 2`
  - [x] Compound Rep's `GetSellValue` returns `3 * (int)Math.Pow(2, _roundsHeld)`
  - [x] Bond Bonus's `GetSellValue` returns null (uses default 50%)
  - [x] File: `Scripts/Runtime/Shop/ShopTransaction.cs`, `Scripts/Runtime/Items/IRelic.cs`, `Scripts/Runtime/Items/RelicBase.cs`

- [x] Task 3: Implement Rep Doubler relic (AC: 1, 10, 11)
  - [x] Create `RepDoublerRelic.cs` extending `RelicBase`
  - [x] Override `Id` to return `"relic_rep_doubler"`
  - [x] Effect is passive — MarginCallState checks for this relic's presence during round-end Rep calculation
  - [x] No hook override needed; the relic's existence is what matters (queried by MarginCallState)
  - [x] File: `Scripts/Runtime/Items/Relics/RepDoublerRelic.cs`

- [x] Task 4: Implement Fail Forward relic (AC: 2, 10, 11)
  - [x] Create `FailForwardRelic.cs` extending `RelicBase`
  - [x] Override `Id` to return `"relic_fail_forward"`
  - [x] Effect is passive — MarginCallState checks for this relic's presence during margin call handling
  - [x] No hook override needed; the relic's existence is what matters (queried by MarginCallState)
  - [x] File: `Scripts/Runtime/Items/Relics/FailForwardRelic.cs`

- [x] Task 5: Implement Compound Rep relic (AC: 3, 10, 11)
  - [x] Create `CompoundRepRelic.cs` extending `RelicBase`
  - [x] Override `Id` to return `"relic_compound_rep"`
  - [x] Add private `int _roundsHeld = 0` field
  - [x] Override `OnRoundStart` — increment `_roundsHeld` by 1
  - [x] Override `GetSellValue` — return `3 * (int)Math.Pow(2, _roundsHeld)` as the custom sell refund
  - [x] Publish `RelicActivatedEvent` when sold (for UI feedback)
  - [x] File: `Scripts/Runtime/Items/Relics/CompoundRepRelic.cs`

- [x] Task 6: Implement Rep Interest relic (AC: 4, 10, 11)
  - [x] Create `RepInterestRelic.cs` extending `RelicBase`
  - [x] Override `Id` to return `"relic_rep_interest"`
  - [x] Override `OnRoundStart` — calculate `ctx.Reputation.Current / 10` (integer division = floor)
  - [x] Add the calculated interest to Reputation via `ctx.Reputation.Add()`
  - [x] Publish `TradeFeedbackEvent` with "+X Rep Interest" message
  - [x] Publish `RelicActivatedEvent` with relic Id
  - [x] File: `Scripts/Runtime/Items/Relics/RepInterestRelic.cs`

- [x] Task 7: Implement Rep Dividend relic (AC: 5, 10, 11)
  - [x] Create `RepDividendRelic.cs` extending `RelicBase`
  - [x] Override `Id` to return `"relic_rep_dividend"`
  - [x] Override `OnRoundStart` — calculate `ctx.Reputation.Current / 2` (integer division = $1 per 2 Rep)
  - [x] Add the calculated dividend to cash via `ctx.Portfolio.AddCash()`
  - [x] Publish `TradeFeedbackEvent` with "+$X Dividend" message
  - [x] Publish `RelicActivatedEvent` with relic Id
  - [x] File: `Scripts/Runtime/Items/Relics/RepDividendRelic.cs`

- [x] Task 8: Implement Bond Bonus relic (AC: 6, 10, 11)
  - [x] Create `BondBonusRelic.cs` extending `RelicBase`
  - [x] Override `Id` to return `"relic_bond_bonus"`
  - [x] Override `OnAcquired` — increase `ctx.BondsOwned` by 10
  - [x] Add 10 synthetic `BondRecord` entries to `ctx.BondPurchaseHistory` with `Round = ctx.CurrentRound`
  - [x] These bonds generate Rep per round like normal bonds via BondManager
  - [x] Override `OnSellSelf` — decrease `ctx.BondsOwned` by 10 (clamp to minimum 0 via `Math.Max(0, ctx.BondsOwned - 10)`)
  - [x] Remove 10 bond records from `ctx.BondPurchaseHistory` LIFO (remove from the end of the list)
  - [x] If fewer than 10 records exist, remove all remaining records
  - [x] Publish `RelicActivatedEvent` with relic Id on both acquire and sell
  - [x] File: `Scripts/Runtime/Items/Relics/BondBonusRelic.cs`

- [x] Task 9: Register relics in RelicFactory (AC: 10)
  - [x] Replace stub registrations for `relic_rep_doubler`, `relic_fail_forward`, `relic_compound_rep`, `relic_rep_interest`, `relic_rep_dividend`, `relic_bond_bonus` with real constructors
  - [x] File: `Scripts/Runtime/Items/RelicFactory.cs`

- [x] Task 10: Write tests (AC: 1-11)
  - [x] Rep Doubler: round-end Rep doubled, bond Rep unaffected, Rep Interest unaffected
  - [x] Fail Forward: margin call still awards base Rep, message published, non-margin-call rounds unaffected
  - [x] Compound Rep: roundsHeld increments on OnRoundStart, sell value = `3 * 2^N` (test N=0,1,2,3), overrides default 50% refund
  - [x] Rep Interest: 10% of current Rep added at round start (floor), 0 Rep = no interest, 9 Rep = 0 interest, 10 Rep = 1 interest
  - [x] Rep Dividend: $1 per 2 Rep added as cash at round start, 0 Rep = $0, 1 Rep = $0, 5 Rep = $2
  - [x] Bond Bonus: acquire adds 10 bonds + 10 BondRecords, sell removes 10 bonds + 10 records LIFO, BondsOwned never negative
  - [x] Bond Bonus edge case: sell when fewer than 10 BondRecords exist (removes all remaining)
  - [x] ShopTransaction.SellRelic: Compound Rep returns custom sell value, other relics return default 50%
  - [x] Files: `Tests/Runtime/Items/Relics/EconomyRelicTests.cs`

## Dev Notes

### Architecture Compliance

- **One class per relic:** Each relic is a separate file in `Scripts/Runtime/Items/Relics/` following the project pattern from Story 17.1.
- **EventBus communication:** Relics publish `RelicActivatedEvent` and `TradeFeedbackEvent` via EventBus — never reference UI systems directly.
- **No ScriptableObjects:** Relic data remains as `public static readonly` in `ShopItemDefinitions.cs` (Story 17.2 handles definitions).
- **RunContext as data carrier:** `BondsOwned`, `BondPurchaseHistory`, `Reputation`, and `Portfolio` are all accessed via the RunContext passed to hook methods.
- **Transaction pattern:** The sell override in `ShopTransaction.SellRelic` follows the existing atomic transaction pattern (validate, mutate state, fire event) with try-catch rollback.

### Existing Code to Read Before Implementing

- `Scripts/Runtime/Items/RelicBase.cs` — base class with virtual no-op hooks, `OnSellSelf` signature (from Story 17.1)
- `Scripts/Runtime/Items/RelicManager.cs` — dispatch methods, `GetRelicById()` (from Story 17.1)
- `Scripts/Runtime/Items/RelicFactory.cs` — static registry of relic ID to constructor mapping (from Story 17.1)
- `Scripts/Runtime/Core/RunContext.cs` — `BondsOwned`, `BondPurchaseHistory`, `Reputation` (ReputationManager), `Portfolio`
- `Scripts/Runtime/Core/GameRunner.cs` — round-end Rep calculation (near `RoundCompletedEvent` publish), margin call handling (near `MarginCallTriggeredEvent` publish)
- `Scripts/Runtime/Core/ReputationManager.cs` — `Current` property, `Add()`, `Spend()`, `CanAfford()`, `Reset()`
- `Scripts/Runtime/Shop/ShopTransaction.cs` — `SellRelic()` method (from Story 13.10), 50% refund calculation
- `Scripts/Runtime/Core/GameEvents.cs` — `RoundCompletedEvent` (has `RepEarned`, `BaseRep`, `BonusRep`), `MarginCallTriggeredEvent`, `TradeFeedbackEvent`
- `Scripts/Setup/Data/GameConfig.cs` — Rep earning constants (base rep, bonus rep formula)
- `Scripts/Runtime/Core/BondManager.cs` — bond Rep payout logic at round start, `BondRecord` struct

### Key Design Decisions

- **Rep Doubler is passive:** Rather than hooking into OnRoundEnd and modifying Rep after the fact, GameRunner checks for the relic's existence during the Rep calculation and doubles the value before awarding. This avoids double-publishing reputation change events.
- **Fail Forward is passive:** Similar to Rep Doubler, GameRunner checks for the relic during margin call handling rather than the relic hooking into an event. This gives GameRunner full control over the margin call flow.
- **Compound Rep sell override:** Requires extending the IRelic interface (or RelicBase) with a sell-value override mechanism. Options: (a) `int? GetSellValue(RunContext ctx)` method returning null for default, (b) out parameter, (c) a flag. Option (a) is cleanest and matches the virtual no-op pattern. ShopTransaction checks this before applying the default 50% refund.
- **Bond Bonus LIFO removal:** BondPurchaseHistory is a `List<BondRecord>`. LIFO removal means removing from the end of the list (`RemoveAt(list.Count - 1)`). This ensures the most recently added (synthetic) bonds are removed first.
- **Rep Interest and Dividend timing:** Both fire in `OnRoundStart`, which executes after `EventScheduler.InitializeRound` and bond payouts. The Rep earned from interest feeds into Event Catalyst (Story 17.7) if both are owned.

### Depends On

- Story 17.1 (Relic Effect Framework) — IRelic, RelicBase, RelicManager, RelicFactory must exist
- Story 17.2 (Shop Behavior & Data Overhaul) — relic definitions with IDs registered in ShopItemDefinitions
- Story 13.10 (Owned Relics Bar & Relic Selling) — `ShopTransaction.SellRelic()` method exists and needs the sell-value override

### References

- [Source: _bmad-output/planning-artifacts/epic-17-relic-system.md#Story 17.5]
- [Source: _bmad-output/implementation-artifacts/17-1-relic-effect-framework.md]
- [Source: _bmad-output/implementation-artifacts/13-10-owned-relics-bar-and-click-to-buy.md — SellRelic transaction]
- [Source: Scripts/Runtime/Core/RunContext.cs — BondsOwned, BondPurchaseHistory, Reputation]
- [Source: Scripts/Runtime/Shop/ShopTransaction.cs — SellRelic, PurchaseRelic pattern]
- [Source: Scripts/Runtime/Core/GameEvents.cs — RoundCompletedEvent, MarginCallTriggeredEvent]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Debug Log References

### Completion Notes List

- Implemented 6 economy/reputation relics: Rep Doubler, Fail Forward, Compound Rep, Rep Interest, Rep Dividend, Bond Bonus
- Added `int? GetSellValue(RunContext ctx)` to IRelic/RelicBase for custom sell value override mechanism
- Modified MarginCallState (not GameRunner) for Rep Doubler (2x round-end rep) and Fail Forward (base rep on margin call)
- Modified ShopTransaction.SellRelic to check GetSellValue before applying default 50% refund
- Registered all 6 relics in RelicFactory replacing StubRelic entries
- All 59 new tests pass (1887/1888 total, 1 pre-existing skip)
- Note: Rep calculation code is in MarginCallState.cs, not GameRunner.cs as story tasks specified — modified the correct file

### File List

- Assets/Scripts/Runtime/Items/Relics/RepDoublerRelic.cs (new)
- Assets/Scripts/Runtime/Items/Relics/FailForwardRelic.cs (new)
- Assets/Scripts/Runtime/Items/Relics/CompoundRepRelic.cs (new)
- Assets/Scripts/Runtime/Items/Relics/RepInterestRelic.cs (new)
- Assets/Scripts/Runtime/Items/Relics/RepDividendRelic.cs (new)
- Assets/Scripts/Runtime/Items/Relics/BondBonusRelic.cs (new)
- Assets/Scripts/Runtime/Items/IRelic.cs (modified — added GetSellValue)
- Assets/Scripts/Runtime/Items/RelicBase.cs (modified — added GetSellValue default)
- Assets/Scripts/Runtime/Items/RelicFactory.cs (modified — registered 6 relics)
- Assets/Scripts/Runtime/Core/GameStates/MarginCallState.cs (modified — Rep Doubler + Fail Forward)
- Assets/Scripts/Runtime/Shop/ShopTransaction.cs (modified — sell value override)
- Assets/Tests/Runtime/Items/Relics/EconomyRelicTests.cs (new — 61 tests)

## Senior Developer Review (AI)

**Reviewer:** Iggy on 2026-02-19
**Issues Found:** 2 High, 3 Medium, 2 Low
**Outcome:** 5 issues fixed, 1 deferred (M5 tooltip — requires UI story)

### Fixes Applied
- **[H1] Fixed** RoundCompletedEvent BaseRep/BonusRep breakdown now correctly doubled when Rep Doubler active (MarginCallState.cs)
- **[H2] Fixed** FailForward_MessagePublished test now actually verifies TradeFeedbackEvent message (EconomyRelicTests.cs)
- **[M3] Fixed** Moved RelicActivatedEvent from CompoundRep.GetSellValue() (query) to OnSellSelf() (command) — no more side effects in getter; also switched Math.Pow to bit shift (L6)
- **[M4] Improved** Added RepDoubler_BaseBonusBreakdownCorrectWhenDoubled test + CompoundRep_GetSellValue_NoBoolSideEffects test (2 new tests)
- **[L7] Fixed** Added code comment documenting synthetic BondRecord cost=0 in BondBonusRelic

### Deferred
- **[M5]** AC 3 dynamic tooltip ("show current sell value") — CompoundRep.GetSellValue() is now a clean query for tooltip systems to call, but no tooltip UI exists yet. Requires Owned Relics Bar tooltip implementation (Story 17.8 or separate UI story).

## Change Log

- 2026-02-19: Story 17.5 implemented — 6 economy/reputation relics with sell override mechanism, 59 tests, all passing (1887/1888 total)
- 2026-02-19: Code review — fixed 5 issues (H1: RepDoubler base/bonus breakdown, H2: FailForward test, M3: GetSellValue side effect, M4: added 2 tests, L7: doc comment). 1 deferred (M5: tooltip). 61 tests total.
