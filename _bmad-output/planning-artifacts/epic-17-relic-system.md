# Epic 17: Relic System — Design, Effects & Collection Management

**Description:** Complete relic system overhaul — remove all placeholder relics, build an effect execution framework, implement 23 unique relics with real gameplay effects, fix shop purchase/reroll behavior, add trading-phase relic display with tooltips, relic reordering, and relic icons. Transforms relics from inert inventory items into build-defining gameplay modifiers (Balatro joker-style).
**Phase:** Post-Epic 16, core gameplay expansion
**Depends On:** Epic 13 (Store Rework — complete), Epic 14 (CRT UI — complete)
**Status:** Ready for dev

**Relic Effect Architecture (from game-architecture.md):**
```
IRelic interface → RelicManager holds ordered list → EventBus dispatch left-to-right
Each relic = one class file in Scripts/Runtime/Items/Relics/
Relics override specific hooks: OnTradeExecuted, OnRoundStart, OnRoundEnd, etc.
Player reorders relics → execution order changes → strategic depth
```

---

## Story 17.1: Relic Effect Framework — IRelic, RelicManager & Event Dispatch

As a developer,
I want a relic effect execution framework with an IRelic interface, RelicManager, and EventBus dispatch pipeline,
So that relics can hook into game events and execute effects in player-defined order.

**Acceptance Criteria:**

**Given** the architecture specifies player-orderable item execution via IUpgrade interface
**When** this story is complete
**Then** the following components exist and are wired:

- `IRelic` interface in `Scripts/Runtime/Items/IRelic.cs` with:
  - `string Id { get; }` — matches RelicDef.Id
  - `void OnAcquired(RunContext ctx)` — called when relic is purchased
  - `void OnRemoved(RunContext ctx)` — called when relic is sold
  - `void OnRoundStart(RunContext ctx, RoundStartedEvent e)`
  - `void OnRoundEnd(RunContext ctx, MarketClosedEvent e)`
  - `void OnBeforeTrade(RunContext ctx, TradeExecutedEvent e)` — can modify trade params
  - `void OnAfterTrade(RunContext ctx, TradeExecutedEvent e)` — react to completed trade
  - `void OnMarketEventFired(RunContext ctx, MarketEventFiredEvent e)`
  - `void OnReputationChanged(RunContext ctx, int oldRep, int newRep)`
  - `void OnShopOpen(RunContext ctx)` — for shop-phase relics
  - `void OnSellSelf(RunContext ctx)` — special logic when THIS relic is sold

- `RelicBase` abstract class in `Scripts/Runtime/Items/RelicBase.cs`:
  - Implements `IRelic` with all methods as virtual no-ops
  - Each relic subclass only overrides hooks it needs

- `RelicManager` class in `Scripts/Runtime/Items/RelicManager.cs`:
  - Holds `List<IRelic> OrderedRelics` — player-orderable list
  - `AddRelic(string relicId)` — instantiates IRelic from factory, calls OnAcquired
  - `RemoveRelic(string relicId)` — calls OnRemoved, removes from list
  - `ReorderRelic(int fromIndex, int toIndex)` — moves relic in list
  - `DispatchRoundStart(RunContext ctx, RoundStartedEvent e)` — iterates left-to-right
  - `DispatchRoundEnd(RunContext ctx, MarketClosedEvent e)` — iterates left-to-right
  - `DispatchAfterTrade(RunContext ctx, TradeExecutedEvent e)` — iterates left-to-right
  - `DispatchMarketEvent(RunContext ctx, MarketEventFiredEvent e)` — iterates left-to-right
  - `DispatchReputationChanged(RunContext ctx, int oldRep, int newRep)` — iterates left-to-right
  - `DispatchShopOpen(RunContext ctx)` — iterates left-to-right
  - All dispatch methods wrapped in try-catch per relic (one relic failure doesn't break others)

- `RelicFactory` static class in `Scripts/Runtime/Items/RelicFactory.cs`:
  - `Dictionary<string, Func<IRelic>>` mapping relic IDs to constructors
  - `IRelic Create(string relicId)` — returns new instance or null if unknown

- `RunContext` updated:
  - New `RelicManager RelicManager` property, initialized in constructor
  - Existing `OwnedRelics` list stays as string IDs (source of truth for save/load)
  - RelicManager synced from OwnedRelics on run start

- `GameRunner` (or appropriate state classes) subscribes to EventBus events and routes to RelicManager dispatch methods

- `RelicEffectContext` helper struct for relics that need to modify game state:
  - Provides safe accessors for portfolio cash, cooldown timers, round timer, etc.
  - Relics modify game state through this context, not by reaching into systems directly

**And** a test relic (`TestRelic`) exists in `Scripts/Tests/` that verifies the dispatch pipeline works correctly

**Files to create:**
- `Assets/Scripts/Runtime/Items/IRelic.cs`
- `Assets/Scripts/Runtime/Items/RelicBase.cs`
- `Assets/Scripts/Runtime/Items/RelicManager.cs`
- `Assets/Scripts/Runtime/Items/RelicFactory.cs`

**Files to modify:**
- `Assets/Scripts/Runtime/Core/RunContext.cs` — add RelicManager
- `Assets/Scripts/Runtime/Core/GameRunner.cs` — wire EventBus → RelicManager dispatch
- `Assets/Scripts/Runtime/Core/GameStates/ShopState.cs` — call DispatchShopOpen

---

## Story 17.2: Shop Behavior Fixes & Relic Data Overhaul

As a player,
I want purchased relics to disappear from the shop (not just show "SOLD"), the reroll to fill all 3 slots with fresh relics, and a new pool of real relics with balanced prices,
So that the shop feels responsive and I have meaningful choices.

**Acceptance Criteria:**

**Shop Behavior — Purchase Removes Relic:**

**Given** a relic is displayed in the shop
**When** the player purchases it
**Then** the relic card is removed from the display (not "SOLD" stamped) and the slot shows as empty/blank
**And** the `_currentOffering` array sets that slot to `null` after purchase

**Shop Behavior — Reroll Fills All Slots:**

**Given** the player has purchased a relic from slot 2 (middle), leaving slots 1 and 3 still showing relics
**When** the player clicks Reroll
**Then** ALL 3 slots are regenerated with fresh relics from the pool
**And** the reroll does NOT pass an `additionalExcludeIds` list of currently displayed relics (since all slots refresh)
**And** owned relics are still excluded as before

**Relic Data — Remove All Placeholders:**

**Given** `ShopItemDefinitions.RelicPool` currently contains 8 placeholder relics with no effects
**When** this story is complete
**Then** all 8 placeholders are removed and replaced with 23 new relic definitions

**Relic Data — New Definitions:**

All 23 relics defined in `ShopItemDefinitions.RelicPool` with balanced costs:

| # | ID | Name | Description | Cost (Rep) |
|---|-----|------|-------------|------------|
| 1 | `relic_event_trigger` | Catalyst Trader | Buying a stock triggers a random market event. Buy cooldown +3s. | 25 |
| 2 | `relic_short_multiplier` | Bear Raid | Shorts execute 3 copies. You can no longer buy or sell (longs disabled). | 20 |
| 3 | `relic_market_manipulator` | Market Manipulator | Selling a stock causes its price to drop 15%. | 18 |
| 4 | `relic_double_dealer` | Double Dealer | You buy and sell 2 shares at a time instead of your current quantity. | 30 |
| 5 | `relic_quick_draw` | Quick Draw | Buying is instant (0s cooldown). Selling has 2x the normal cooldown. | 22 |
| 6 | `relic_event_storm` | Event Storm | Double the number of natural events per round. Events have 25% less price impact. | 28 |
| 7 | `relic_loss_liquidator` | Loss Liquidator | Selling a stock at a loss instantly triggers a random market event. | 15 |
| 8 | `relic_profit_refresh` | Profit Refresh | Selling a stock at a profit instantly refreshes your buy cooldown. | 20 |
| 9 | `relic_bull_believer` | Bull Believer | Positive events have double effectiveness. You can no longer short. | 22 |
| 10 | `relic_time_buyer` | Time Buyer | Buying a stock extends the round timer by 5 seconds. | 25 |
| 11 | `relic_diamond_hands` | Diamond Hands | Stocks held until the end of the round gain 30% in value before liquidation. | 35 |
| 12 | `relic_rep_doubler` | Rep Doubler | Double the Reputation earned from all trades. | 40 |
| 13 | `relic_fail_forward` | Fail Forward | Reputation is earned from failed trades (margin calls) as well as successes. | 12 |
| 14 | `relic_bond_bonus` | Bond Bonus | Gain 10 bonds immediately. Lose 10 bonds when you sell this relic. | 45 |
| 15 | `relic_free_intel` | Free Intel | One Insider Tip is free every shop visit. | 15 |
| 16 | `relic_extra_expansion` | Extra Expansion | One additional Trading Deck Expansion is offered every shop visit. | 20 |
| 17 | `relic_compound_rep` | Compound Rep | Grants 3 Reputation when sold. This value doubles every round you hold it. | 8 |
| 18 | `relic_skimmer` | Skimmer | Earn 3% of the stock value instantly when buying it (added to cash). | 18 |
| 19 | `relic_short_profiteer` | Short Profiteer | Earn 10% of the stock value instantly when you short it (added to cash). | 22 |
| 20 | `relic_relic_expansion` | Relic Expansion | Grants one additional relic slot permanently when sold. Yields 0 Rep when sold. | 50 |
| 21 | `relic_event_catalyst` | Event Catalyst | Earning Reputation gives a 1% chance per Rep earned to trigger a random event. | 20 |
| 22 | `relic_rep_interest` | Rep Interest | Reputation earns 10% interest (rounded down) at the start of every round. | 35 |
| 23 | `relic_rep_dividend` | Rep Dividend | Earn $1 at the start of every round for every 2 Reputation you have. | 28 |

**And** `RelicDef` struct gains a new `string EffectDescription` field for tooltip display (short mechanical description)
**And** `RelicFactory` is updated with constructors for all 23 relics (pointing to stub classes until Stories 17.3-17.7 implement effects)
**And** `ItemLookup` cache is invalidated/rebuilt with new pool

**Files to modify:**
- `Assets/Scripts/Setup/Data/ShopItemDefinitions.cs` — replace all 8 placeholders with 23 new relics
- `Assets/Scripts/Runtime/UI/ShopUI.cs` — purchase removes card (not SOLD stamp), reroll regenerates all 3 slots
- `Assets/Scripts/Runtime/Shop/ShopGenerator.cs` — reroll no longer passes additionalExcludeIds
- `Assets/Scripts/Runtime/Items/RelicFactory.cs` — register all 23 relic constructors
- `Assets/Scripts/Runtime/Items/ItemLookup.cs` — rebuild cache

---

## Story 17.3: Trade Modification Relics (5 Relics)

As a player,
I want relics that fundamentally change how I trade — doubling shares, modifying cooldowns, boosting shorts, or skimming profits,
So that my build strategy meaningfully alters my trading phase gameplay.

**Acceptance Criteria:**

**Relic: Double Dealer (`relic_double_dealer`)**

**Given** the player owns "Double Dealer"
**When** the player executes a buy or sell trade
**Then** the trade quantity is doubled (e.g., buying 5 shares actually buys 10)
**And** this applies to both buy and sell actions
**And** this stacks with the current quantity selector value
**And** cost/proceeds reflect the doubled quantity

**Relic: Quick Draw (`relic_quick_draw`)**

**Given** the player owns "Quick Draw"
**When** the player executes a buy trade
**Then** the post-trade cooldown for BUY is 0 seconds (instant)
**When** the player executes a sell trade
**Then** the post-trade cooldown for SELL is doubled (2x `GameConfig.PostTradeCooldown`)
**And** the cooldown timer display reflects the modified durations

**Relic: Short Multiplier (`relic_short_multiplier`)**

**Given** the player owns "Bear Raid"
**When** the player opens a short position
**Then** the short executes for 3 shares instead of the base 1
**And** the BUY and SELL buttons are permanently disabled for the rest of the run
**And** disabled buttons show a visual indicator (e.g., "LOCKED" or crossed out)
**And** keyboard shortcuts B/S are blocked
**And** OnAcquired immediately disables buy/sell if mid-round

**Relic: Skimmer (`relic_skimmer`)**

**Given** the player owns "Skimmer"
**When** the player executes a buy trade
**Then** 3% of the total trade value (shares x price) is instantly added to the player's cash
**And** a brief "+$X.XX" feedback displays near the cash counter

**Relic: Short Profiteer (`relic_short_profiteer`)**

**Given** the player owns "Short Profiteer"
**When** the player opens a short position
**Then** 10% of the stock value (shares x current price) is instantly added to the player's cash
**And** a brief "+$X.XX" feedback displays near the cash counter

**Implementation Notes:**
- Each relic is one file: `Scripts/Runtime/Items/Relics/DoubleDealerRelic.cs`, etc.
- Trade modification relics primarily hook into `OnBeforeTrade` or `OnAfterTrade`
- Short Multiplier requires a `RunContext` flag (`LongsDisabled`) that GameRunner checks before executing buy/sell
- Cooldown modifications need a `GetEffectiveCooldown(bool isBuy)` method on RelicManager that GameRunner calls

**Files to create:**
- `Assets/Scripts/Runtime/Items/Relics/DoubleDealerRelic.cs`
- `Assets/Scripts/Runtime/Items/Relics/QuickDrawRelic.cs`
- `Assets/Scripts/Runtime/Items/Relics/ShortMultiplierRelic.cs`
- `Assets/Scripts/Runtime/Items/Relics/SkimmerRelic.cs`
- `Assets/Scripts/Runtime/Items/Relics/ShortProfiteerRelic.cs`

**Files to modify:**
- `Assets/Scripts/Runtime/Core/GameRunner.cs` — check relic cooldown overrides, LongsDisabled flag, cash bonus on trade
- `Assets/Scripts/Runtime/Core/RunContext.cs` — `LongsDisabled` flag
- `Assets/Scripts/Runtime/Items/RelicFactory.cs` — register 5 constructors

---

## Story 17.4: Event Interaction Relics (5 Relics)

As a player,
I want relics that interact with the market event system — triggering events on trades, doubling event frequency, or amplifying positive events,
So that I can build around event manipulation as a core strategy.

**Acceptance Criteria:**

**Relic: Catalyst Trader (`relic_event_trigger`)**

**Given** the player owns "Catalyst Trader"
**When** the player executes a buy trade
**Then** a random market event is immediately triggered on the active stock
**And** the buy cooldown is increased by +3 seconds (added to `GameConfig.PostTradeCooldown`)
**And** the triggered event follows normal event processing (popup, price effect, etc.)

**Relic: Event Storm (`relic_event_storm`)**

**Given** the player owns "Event Storm"
**When** a trading round begins
**Then** the `EventScheduler` generates double the normal number of events for the round
**And** all event price impacts are reduced by 25% (multiplied by 0.75)
**And** the reduced impact applies to ALL events (natural + relic-triggered)

**Relic: Loss Liquidator (`relic_loss_liquidator`)**

**Given** the player owns "Loss Liquidator"
**When** the player sells a stock at a loss (sell price < average buy price)
**Then** a random market event is immediately triggered on the active stock
**And** the event is triggered AFTER the sell completes
**And** selling at a profit does NOT trigger this effect

**Relic: Profit Refresh (`relic_profit_refresh`)**

**Given** the player owns "Profit Refresh"
**When** the player sells a stock at a profit (sell price > average buy price)
**Then** the buy button cooldown is immediately reset to 0 (ready to buy again)
**And** selling at a loss does NOT trigger this effect
**And** a brief visual cue (e.g., flash on buy button) indicates the cooldown was refreshed

**Relic: Bull Believer (`relic_bull_believer`)**

**Given** the player owns "Bull Believer"
**When** a positive market event fires (IsPositive == true)
**Then** the event's price impact is doubled (PriceEffectPercent × 2.0)
**And** the SHORT button is permanently disabled for the rest of the run
**And** keyboard shortcut D is blocked
**And** OnAcquired immediately disables short if mid-round

**Implementation Notes:**
- Event-triggering relics need access to `EventScheduler.ForceFireRandomEvent()` — a new method
- Event Storm modifies EventScheduler config at round start via OnRoundStart
- Bull Believer requires a `ShortingDisabled` flag on RunContext
- Profit/loss detection uses `TradeExecutedEvent.ProfitLoss` field

**Files to create:**
- `Assets/Scripts/Runtime/Items/Relics/CatalystTraderRelic.cs`
- `Assets/Scripts/Runtime/Items/Relics/EventStormRelic.cs`
- `Assets/Scripts/Runtime/Items/Relics/LossLiquidatorRelic.cs`
- `Assets/Scripts/Runtime/Items/Relics/ProfitRefreshRelic.cs`
- `Assets/Scripts/Runtime/Items/Relics/BullBelieverRelic.cs`

**Files to modify:**
- `Assets/Scripts/Runtime/Events/EventScheduler.cs` — `ForceFireRandomEvent()`, event count multiplier, impact multiplier
- `Assets/Scripts/Runtime/Core/GameRunner.cs` — cooldown reset, short disabled check
- `Assets/Scripts/Runtime/Core/RunContext.cs` — `ShortingDisabled` flag
- `Assets/Scripts/Runtime/Items/RelicFactory.cs` — register 5 constructors

---

## Story 17.5: Economy & Reputation Relics (6 Relics)

As a player,
I want relics that boost my Reputation economy — doubling rep gains, earning rep interest, converting rep to cash, or gaining bonds,
So that I can build a powerful economic engine across my run.

**Acceptance Criteria:**

**Relic: Rep Doubler (`relic_rep_doubler`)**

**Given** the player owns "Rep Doubler"
**When** Reputation is earned from trade performance at round end (RoundCompletedEvent)
**Then** the Rep earned (both base and bonus) is doubled
**And** this does NOT affect bond Rep payouts or other non-trade Rep sources

**Relic: Fail Forward (`relic_fail_forward`)**

**Given** the player owns "Fail Forward"
**When** a round ends in a margin call (MarginCallTriggeredEvent)
**Then** the player still earns the base Reputation that would have been earned for that round
**And** a "Fail Forward: +X Rep" message displays on the margin call screen

**Relic: Compound Rep (`relic_compound_rep`)**

**Given** the player owns "Compound Rep" and has held it for N rounds
**When** the relic is sold (via the owned relics bar sell button)
**Then** the player receives `3 * 2^N` Reputation (3 Rep base, doubles each round held)
**And** the relic's tooltip dynamically shows the current sell value: "Current value: X Rep"
**And** the relic tracks `RoundsHeld` internally, incrementing on each RoundStart
**And** the normal 50% sell refund is replaced with this special value

**Relic: Rep Interest (`relic_rep_interest`)**

**Given** the player owns "Rep Interest"
**When** a new round starts (RoundStartedEvent)
**Then** the player gains 10% of their current Reputation as bonus Rep (rounded down)
**And** a "+X Rep Interest" message displays during market open phase
**And** this stacks with bond Rep payouts (both happen at round start)

**Relic: Rep Dividend (`relic_rep_dividend`)**

**Given** the player owns "Rep Dividend"
**When** a new round starts (RoundStartedEvent)
**Then** the player gains $1 cash for every 2 Reputation they currently have (integer division)
**And** a "+$X Dividend" message displays during market open phase
**And** this cash is added to portfolio before trading begins

**Relic: Bond Bonus (`relic_bond_bonus`)**

**Given** the player purchases "Bond Bonus"
**When** OnAcquired fires
**Then** `RunContext.BondsOwned` is increased by 10 immediately
**And** 10 synthetic bond purchase records are added to `BondPurchaseHistory` (round = current round)
**And** these bonds generate Rep per round like normal bonds

**Given** the player sells "Bond Bonus"
**When** OnSellSelf fires
**Then** `RunContext.BondsOwned` is decreased by 10 (minimum 0)
**And** 10 bond records are removed from `BondPurchaseHistory` (most recent first / LIFO)

**Implementation Notes:**
- Rep Doubler hooks into the round-end Rep calculation in GameRunner
- Compound Rep overrides sell behavior — needs special handling in ShopTransaction.SellRelic
- Rep Interest and Rep Dividend hook into OnRoundStart, executing before trading
- Bond Bonus modifies BondManager state directly via OnAcquired/OnRemoved

**Files to create:**
- `Assets/Scripts/Runtime/Items/Relics/RepDoublerRelic.cs`
- `Assets/Scripts/Runtime/Items/Relics/FailForwardRelic.cs`
- `Assets/Scripts/Runtime/Items/Relics/CompoundRepRelic.cs`
- `Assets/Scripts/Runtime/Items/Relics/RepInterestRelic.cs`
- `Assets/Scripts/Runtime/Items/Relics/RepDividendRelic.cs`
- `Assets/Scripts/Runtime/Items/Relics/BondBonusRelic.cs`

**Files to modify:**
- `Assets/Scripts/Runtime/Core/GameRunner.cs` — Rep doubling at round end, fail forward on margin call
- `Assets/Scripts/Runtime/Shop/ShopTransaction.cs` — special sell handling for Compound Rep
- `Assets/Scripts/Runtime/Items/RelicFactory.cs` — register 6 constructors

---

## Story 17.6: Mechanic & Timer Relics (5 Relics)

As a player,
I want relics that change core game mechanics — extending the round timer when buying, boosting held-stock value, manipulating prices, or getting free intel and extra expansions,
So that I can bend the rules of the game to match my playstyle.

**Acceptance Criteria:**

**Relic: Time Buyer (`relic_time_buyer`)**

**Given** the player owns "Time Buyer"
**When** the player executes a buy trade during the trading phase
**Then** the round timer is extended by 5 seconds
**And** the timer UI updates to reflect the new remaining time
**And** there is no cap on extensions (buying 5 times = +25 seconds)

**Relic: Diamond Hands (`relic_diamond_hands`)**

**Given** the player owns "Diamond Hands"
**When** the trading phase ends and positions are auto-liquidated (MarketCloseState)
**Then** all held long positions gain 30% in value before liquidation
**And** the bonus is applied as: `liquidation_price = current_price * 1.30`
**And** the round P&L reflects the boosted liquidation value
**And** short positions are NOT affected by this bonus

**Relic: Market Manipulator (`relic_market_manipulator`)**

**Given** the player owns "Market Manipulator"
**When** the player sells a stock (long position)
**Then** the stock's price immediately drops by 15% of its current value
**And** the price drop is applied AFTER the sell executes (player gets pre-drop price)
**And** this creates a buy-the-dip opportunity if the player has cash

**Relic: Free Intel (`relic_free_intel`)**

**Given** the player owns "Free Intel"
**When** the shop opens (ShopState.Enter)
**Then** the first Insider Tip purchased this shop visit costs 0 Reputation
**And** subsequent tips cost their normal price
**And** the first tip slot shows "FREE" instead of the normal cost
**And** `DispatchShopOpen` sets a flag that ShopTransaction.PurchaseTip checks

**Relic: Extra Expansion (`relic_extra_expansion`)**

**Given** the player owns "Extra Expansion"
**When** the shop opens and expansions are generated
**Then** one additional expansion is offered (normally 2-3, now 3-4 from unowned pool)
**And** if fewer unowned expansions remain than the increased count, all remaining are shown

**Implementation Notes:**
- Time Buyer modifies the active round timer in GameRunner directly
- Diamond Hands hooks into a pre-liquidation step in MarketCloseState
- Market Manipulator hooks into OnAfterTrade and modifies PriceGenerator state
- Free Intel sets a `FreeIntelThisVisit` flag on RunContext, reset each shop visit
- Extra Expansion modifies the expansion offering count in ShopState

**Files to create:**
- `Assets/Scripts/Runtime/Items/Relics/TimeBuyerRelic.cs`
- `Assets/Scripts/Runtime/Items/Relics/DiamondHandsRelic.cs`
- `Assets/Scripts/Runtime/Items/Relics/MarketManipulatorRelic.cs`
- `Assets/Scripts/Runtime/Items/Relics/FreeIntelRelic.cs`
- `Assets/Scripts/Runtime/Items/Relics/ExtraExpansionRelic.cs`

**Files to modify:**
- `Assets/Scripts/Runtime/Core/GameRunner.cs` — timer extension on buy
- `Assets/Scripts/Runtime/Core/GameStates/MarketCloseState.cs` — Diamond Hands pre-liquidation boost
- `Assets/Scripts/Runtime/PriceEngine/PriceGenerator.cs` — Market Manipulator price drop
- `Assets/Scripts/Runtime/Core/GameStates/ShopState.cs` — Free Intel flag, Extra Expansion count
- `Assets/Scripts/Runtime/Shop/ShopTransaction.cs` — Free Intel tip cost override
- `Assets/Scripts/Runtime/Core/RunContext.cs` — `FreeIntelThisVisit` flag
- `Assets/Scripts/Runtime/Items/RelicFactory.cs` — register 5 constructors

---

## Story 17.7: Special Relics (2 Relics)

As a player,
I want unique high-impact relics — one that turns Reputation gains into chaotic event triggers, and one expensive relic that permanently expands my collection capacity when sold,
So that I have chase-worthy build-around options.

**Acceptance Criteria:**

**Relic: Event Catalyst (`relic_event_catalyst`)**

**Given** the player owns "Event Catalyst"
**When** the player gains Reputation (from any source: round end, bonds, interest, etc.)
**Then** for each point of Reputation gained, there is a 1% chance to trigger a random market event
**And** the chance is rolled independently per Rep point (gaining 10 Rep = 10 separate 1% rolls)
**And** triggered events only fire if currently in a trading phase (no events during shop)
**And** if multiple events trigger, they queue and fire with normal popup/pause behavior
**And** events triggered this way follow normal event rules (type, targeting, impact)

**Relic: Relic Expansion (`relic_relic_expansion`)**

**Given** the player owns "Relic Expansion"
**When** the relic is sold (via owned relics bar sell button)
**Then** `GameConfig.MaxRelicSlots` is permanently increased by 1 for the rest of the run
**And** the sell refund is 0 Reputation (overrides normal 50% refund)
**And** the freed slot is immediately available
**And** the tooltip reads: "Sell to permanently gain +1 relic slot. No Rep refund."
**And** this increase persists through all future shop visits in the run

**Given** the player has NOT yet sold "Relic Expansion"
**When** it is in their inventory
**Then** it occupies a relic slot like any other relic (no passive effect while held)

**Implementation Notes:**
- Event Catalyst hooks into `OnReputationChanged` — needs to be called from ALL Rep sources
- ReputationManager needs a hook/callback when Rep changes so RelicManager can dispatch
- Relic Expansion modifies a `BonusRelicSlots` counter on RunContext (checked by GetEffectiveMaxRelicSlots)
- Relic Expansion overrides OnSellSelf to set refund to 0 and increment slot counter

**Files to create:**
- `Assets/Scripts/Runtime/Items/Relics/EventCatalystRelic.cs`
- `Assets/Scripts/Runtime/Items/Relics/RelicExpansionRelic.cs`

**Files to modify:**
- `Assets/Scripts/Runtime/Core/ReputationManager.cs` — add OnReputationChanged callback
- `Assets/Scripts/Runtime/Core/RunContext.cs` — `BonusRelicSlots` counter
- `Assets/Scripts/Runtime/Shop/ShopTransaction.cs` — check OnSellSelf override for refund, BonusRelicSlots in GetEffectiveMaxRelicSlots
- `Assets/Scripts/Runtime/Items/RelicFactory.cs` — register 2 constructors

---

## Story 17.8: Trading Phase Relic Display & Tooltips

As a player,
I want to see my owned relics displayed during the trading phase with hover tooltips showing each relic's name, description, and effect,
So that I always know what relics I have and what they do without leaving the trading screen.

**Acceptance Criteria:**

**Given** the player owns 1 or more relics
**When** the trading phase is active
**Then** a horizontal relic bar is displayed (top-right or below the event ticker)
**And** each relic is shown as a small icon/badge (sized ~40x40 px equivalent)
**And** relics are displayed in the player's current order (left-to-right)
**And** the bar dynamically updates if relics are gained/lost mid-run

**Given** the player hovers over a relic icon in the trading phase
**When** the mouse enters the relic icon area
**Then** a tooltip panel appears within ~0.1 seconds showing:
  - Relic name (bold)
  - Relic description (from RelicDef.Description)
  - Effect summary (from RelicDef.EffectDescription — mechanical details)
  - For Compound Rep: current sell value
**And** the tooltip disappears when the mouse leaves the icon area
**And** the tooltip does not obstruct the chart area

**Given** a relic effect activates during trading (e.g., Skimmer adds cash on buy)
**When** the relic's effect fires
**Then** the relic icon briefly glows/pulses (0.3s highlight animation)
**And** this provides visual confirmation that the relic did something

**And** the relic bar is NOT shown during shop phase (the owned relics bar in ShopUI handles that)
**And** the relic bar is created programmatically by UISetup (consistent with project patterns)
**And** the bar handles 5-8 relics without overflow (wraps or scrolls if needed)

**Files to create:**
- `Assets/Scripts/Runtime/UI/RelicBar.cs` — trading-phase relic display MonoBehaviour

**Files to modify:**
- `Assets/Scripts/Setup/UISetup.cs` — create relic bar in trading HUD
- `Assets/Scripts/Runtime/UI/DashboardReferences.cs` — add relic bar references
- `Assets/Scripts/Runtime/Items/RelicManager.cs` — fire event when relic activates (for glow)

---

## Story 17.9: Relic Reordering

As a player,
I want to reorder my relics in the owned relics bar by clicking to select and clicking a destination,
So that I can control the execution order for strategic effect (relics execute left-to-right).

**Acceptance Criteria:**

**Given** the player is in the shop phase viewing their owned relics bar
**When** the player clicks on an owned relic
**Then** the relic is visually highlighted as "selected" (e.g., raised, glowing border)
**And** other relic slots show insertion indicators

**When** the player clicks another relic slot or position
**Then** the selected relic moves to that position
**And** other relics shift to accommodate (insert, not swap)
**And** `RelicManager.ReorderRelic(fromIndex, toIndex)` is called
**And** the visual order updates immediately

**When** the player clicks the same relic again (or presses Escape)
**Then** the selection is cancelled and no reorder occurs

**Given** relics have been reordered
**When** a relic dispatch event fires (e.g., OnAfterTrade)
**Then** relics execute in the new visual order (left-to-right)
**And** the order persists for the rest of the run (survives round transitions)

**And** the reorder is only available in the shop phase owned relics bar (NOT the trading phase relic display — that is read-only)
**And** a brief tooltip or label reminds the player: "Relics execute left to right"

**Files to modify:**
- `Assets/Scripts/Runtime/UI/ShopUI.cs` — click-to-select-and-place reorder UI
- `Assets/Scripts/Runtime/Items/RelicManager.cs` — ReorderRelic method (already specified in 17.1)
- `Assets/Scripts/Runtime/Core/RunContext.cs` — OwnedRelics list order synced with RelicManager

---

## Story 17.10: Relic Icons

As a player,
I want each relic to have a distinctive icon displayed in the shop cards, owned relics bar, and trading phase HUD,
So that I can quickly identify relics at a glance.

**Acceptance Criteria:**

**Given** relic icons need to be created for all 23 relics
**When** this story is complete
**Then** each relic has a unique icon that:
  - Is a simple, recognizable glyph/symbol rendered programmatically (TextMeshPro icon font, Unicode symbols, or procedural drawing)
  - Uses the CRT theme color palette (amber, green, red accents on dark backgrounds)
  - Is legible at small sizes (40x40 in trading HUD, 60x60 in shop/owned bar)

**Icon Approach — Programmatic Text Icons:**
- Each RelicDef gains an `IconChar` field (string — Unicode character or emoji-like symbol)
- Icons rendered via TextMeshPro with the monospace terminal font
- Color-coded by relic category feel:
  - Trade relics: green tint
  - Event relics: amber/yellow tint
  - Economy relics: gold tint
  - Mechanic relics: cyan tint
  - Special relics: magenta tint

**Icon Assignments (suggested — adjust for readability):**

| Relic | Icon Symbol | Color |
|-------|------------|-------|
| Catalyst Trader | `!` | Amber |
| Bear Raid | `III` | Green |
| Market Manipulator | `V` | Green |
| Double Dealer | `x2` | Green |
| Quick Draw | `>>` | Green |
| Event Storm | `**` | Amber |
| Loss Liquidator | `-!` | Amber |
| Profit Refresh | `+R` | Amber |
| Bull Believer | `^^` | Amber |
| Time Buyer | `+T` | Cyan |
| Diamond Hands | `<>` | Cyan |
| Rep Doubler | `R2` | Gold |
| Fail Forward | `FF` | Gold |
| Bond Bonus | `B+` | Gold |
| Free Intel | `?F` | Cyan |
| Extra Expansion | `E+` | Cyan |
| Compound Rep | `$$` | Gold |
| Skimmer | `%B` | Green |
| Short Profiteer | `%S` | Green |
| Relic Expansion | `[+]` | Magenta |
| Event Catalyst | `R!` | Magenta |
| Rep Interest | `R%` | Gold |
| Rep Dividend | `R$` | Gold |

**And** `RelicDef` struct gains `IconChar` and `IconColor` fields
**And** icons display in: shop relic cards, shop owned relics bar, trading phase relic bar
**And** icon rendering uses existing font assets (no new font imports needed)

**Files to modify:**
- `Assets/Scripts/Setup/Data/ShopItemDefinitions.cs` — add IconChar, IconColor to RelicDef and all 23 definitions
- `Assets/Scripts/Runtime/UI/ShopUI.cs` — render icons on relic cards and owned bar
- `Assets/Scripts/Runtime/UI/RelicBar.cs` — render icons in trading phase display

---

## Dependency Graph

```
17.1 (Framework)
  └── 17.2 (Shop Fixes & Data)
        ├── 17.3 (Trade Relics) ──────────┐
        ├── 17.4 (Event Relics) ──────────┤
        ├── 17.5 (Economy Relics) ────────┤──→ 17.8 (Trading Display)
        ├── 17.6 (Mechanic Relics) ───────┤──→ 17.9 (Reordering)
        └── 17.7 (Special Relics) ────────┘──→ 17.10 (Icons)
```

**Recommended implementation order:**
1. **17.1** — Framework first (foundation for everything)
2. **17.2** — Shop fixes + all 23 relic definitions (data)
3. **17.3** — Trade modification relics (most immediately impactful)
4. **17.4** — Event interaction relics (core gameplay loop)
5. **17.5** — Economy relics (meta-progression feel)
6. **17.6** — Mechanic relics (rule-bending)
7. **17.7** — Special relics (chase items)
8. **17.8** — Trading phase display (visibility)
9. **17.9** — Reordering (strategic depth)
10. **17.10** — Icons (visual polish)

Stories 17.3-17.7 can be done in any order after 17.2. Stories 17.8-17.10 can be parallelized after 17.1.
