# Story 17.2: Shop Behavior Fixes & Relic Data Overhaul

Status: ready-for-dev

## Story

As a player,
I want purchased relics to disappear from the shop, the reroll to fill all 3 slots with fresh relics, and a new pool of 23 real relics with balanced prices,
so that the shop feels responsive and I have meaningful choices.

## Acceptance Criteria

1. When a relic is purchased, its card is REMOVED from the display (empty/blank slot) — not stamped "SOLD"
2. The `_currentOffering` array sets the purchased slot to `null` after purchase
3. When rerolling, ALL 3 slots regenerate with fresh relics (including slots where relics were previously purchased)
4. Reroll does NOT pass `additionalExcludeIds` of currently displayed relics — all slots refresh
5. All 8 placeholder relics removed from `ShopItemDefinitions.RelicPool`
6. 23 new relic definitions added to `RelicPool` with balanced costs (8-50 Rep range)
7. `RelicDef` struct gains `EffectDescription` field (short mechanical description for tooltips)
8. `RelicFactory` updated with constructors for all 23 relics (pointing to StubRelic until 17.3-17.7)
9. `ItemLookup` cache rebuilt with new pool
10. Owned relics still excluded from offerings (existing behavior preserved)
11. All existing shop tests still pass with new relic pool

## Tasks / Subtasks

- [ ] Task 1: Update RelicDef struct (AC: 7)
  - [ ] Add `string EffectDescription` field to `RelicDef`
  - [ ] Update constructor to accept EffectDescription parameter
  - [ ] File: `Scripts/Setup/Data/ShopItemDefinitions.cs`
- [ ] Task 2: Replace relic pool (AC: 5, 6)
  - [ ] Remove all 8 placeholder relics from `RelicPool`
  - [ ] Add all 23 new relic definitions with Id, Name, Description, EffectDescription, Cost
  - [ ] Relic IDs follow pattern: `relic_snake_case_name`
  - [ ] Costs range 8-50 Rep (see epic-17 for full table)
  - [ ] File: `Scripts/Setup/Data/ShopItemDefinitions.cs`
- [ ] Task 3: Fix purchase removes card (AC: 1, 2)
  - [ ] In `ShopUI`, after successful purchase: set `_currentOffering[slotIndex] = null`
  - [ ] Update card visual to show empty/blank (not SOLD stamp)
  - [ ] Remove or bypass the existing "SOLD" stamp animation for relics
  - [ ] The slot should appear as an empty dark card or hidden
  - [ ] File: `Scripts/Runtime/UI/ShopUI.cs`
- [ ] Task 4: Fix reroll fills all 3 slots (AC: 3, 4)
  - [ ] In ShopUI reroll handler: call `ShopGenerator.GenerateRelicOffering(ownedRelicIds, random)` WITHOUT additionalExcludeIds
  - [ ] Remove the logic that preserves unsold relics during reroll
  - [ ] All 3 slots get fresh random relics every reroll
  - [ ] File: `Scripts/Runtime/UI/ShopUI.cs`, `Scripts/Runtime/Core/GameStates/ShopState.cs`
- [ ] Task 5: Update RelicFactory registrations (AC: 8)
  - [ ] Register all 23 relic IDs in RelicFactory with StubRelic constructors
  - [ ] Each StubRelic has the correct Id matching the RelicDef
  - [ ] File: `Scripts/Runtime/Items/RelicFactory.cs`
- [ ] Task 6: Rebuild ItemLookup cache (AC: 9)
  - [ ] Clear and rebuild the lookup dictionary
  - [ ] Verify `GetRelicById()` works for all 23 new IDs
  - [ ] File: `Scripts/Runtime/Items/ItemLookup.cs`
- [ ] Task 7: Update tests (AC: 10, 11)
  - [ ] Update ShopGeneratorTests for new pool size (23 relics)
  - [ ] Update RelicPurchaseTests if any reference placeholder relic IDs
  - [ ] Add test: purchase removes from offering array (slot becomes null)
  - [ ] Add test: reroll regenerates all 3 slots fresh (no exclusion of displayed)
  - [ ] Verify owned relic exclusion still works
  - [ ] Files: `Tests/Runtime/Shop/ShopGeneratorTests.cs`, `Tests/Runtime/Shop/RelicPurchaseTests.cs`

## Dev Notes

### Full Relic Definitions Table

| # | ID | Name | Description | EffectDescription | Cost |
|---|-----|------|-------------|-------------------|------|
| 1 | `relic_event_trigger` | Catalyst Trader | Buying a stock triggers a random market event. Buy cooldown +3s. | +event on buy, +3s cooldown | 25 |
| 2 | `relic_short_multiplier` | Bear Raid | Shorts execute 3 copies. You can no longer buy or sell. | 3x shorts, no longs | 20 |
| 3 | `relic_market_manipulator` | Market Manipulator | Selling a stock causes its price to drop 15%. | -15% price on sell | 18 |
| 4 | `relic_double_dealer` | Double Dealer | You buy and sell 2 shares at a time. | 2x trade quantity | 30 |
| 5 | `relic_quick_draw` | Quick Draw | Buying is instant. Selling has 2x the normal cooldown. | 0s buy CD, 2x sell CD | 22 |
| 6 | `relic_event_storm` | Event Storm | Double the events per round. Events have 25% less impact. | 2x events, 0.75x impact | 28 |
| 7 | `relic_loss_liquidator` | Loss Liquidator | Selling at a loss triggers a random event. | +event on loss sell | 15 |
| 8 | `relic_profit_refresh` | Profit Refresh | Selling at profit refreshes your buy cooldown. | reset buy CD on profit | 20 |
| 9 | `relic_bull_believer` | Bull Believer | Positive events 2x effectiveness. You can no longer short. | 2x good events, no short | 22 |
| 10 | `relic_time_buyer` | Time Buyer | Buying extends the round timer by 5 seconds. | +5s timer on buy | 25 |
| 11 | `relic_diamond_hands` | Diamond Hands | Stocks held to round end gain 30% value. | +30% at liquidation | 35 |
| 12 | `relic_rep_doubler` | Rep Doubler | Double Reputation earned from trades. | 2x trade rep | 40 |
| 13 | `relic_fail_forward` | Fail Forward | Reputation earned from failed trades too. | rep on margin call | 12 |
| 14 | `relic_bond_bonus` | Bond Bonus | Gain 10 bonds. Lose 10 bonds on selling this relic. | +10 bonds (lose on sell) | 45 |
| 15 | `relic_free_intel` | Free Intel | One Insider Tip is free every shop visit. | 1 free tip/visit | 15 |
| 16 | `relic_extra_expansion` | Extra Expansion | One extra expansion offered per shop visit. | +1 expansion offer | 20 |
| 17 | `relic_compound_rep` | Compound Rep | Grants 3 rep when sold. Doubles each round held. | 3×2^N rep on sell | 8 |
| 18 | `relic_skimmer` | Skimmer | Earn 3% of stock value when buying. | +3% cash on buy | 18 |
| 19 | `relic_short_profiteer` | Short Profiteer | Earn 10% of stock value when shorting. | +10% cash on short | 22 |
| 20 | `relic_relic_expansion` | Relic Expansion | +1 relic slot permanently when sold. 0 rep refund. | +1 slot on sell (0 rep) | 50 |
| 21 | `relic_event_catalyst` | Event Catalyst | Rep earned = 1% chance per rep to trigger event. | 1%/rep → event | 20 |
| 22 | `relic_rep_interest` | Rep Interest | Rep earns 10% interest every round start. | +10% rep/round | 35 |
| 23 | `relic_rep_dividend` | Rep Dividend | Earn $1/round for every 2 rep you have. | rep→cash dividend | 28 |

### Architecture Compliance

- **No ScriptableObjects** — relic data stays as `public static readonly` in ShopItemDefinitions
- **Retain class name** `ShopItemDefinitions` to avoid `.meta` file disruption (per existing comment in file)
- **Uniform random selection** preserved (no rarity system)

### Existing Code to Read Before Implementing

- `Scripts/Setup/Data/ShopItemDefinitions.cs` — current 8 placeholders to replace
- `Scripts/Runtime/UI/ShopUI.cs` — purchase handler (SOLD stamp logic to change), reroll handler
- `Scripts/Runtime/Shop/ShopGenerator.cs` — reroll `additionalExcludeIds` logic to simplify
- `Scripts/Runtime/Items/ItemLookup.cs` — cache rebuild
- `Scripts/Runtime/Items/RelicFactory.cs` — from Story 17.1

### Depends On

- Story 17.1 (Relic Effect Framework) — RelicFactory must exist for registration

### References

- [Source: _bmad-output/planning-artifacts/epic-17-relic-system.md#Story 17.2]
- [Source: Scripts/Setup/Data/ShopItemDefinitions.cs]

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
