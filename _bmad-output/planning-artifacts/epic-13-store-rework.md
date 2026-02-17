# Epic 13: Store Phase Rework — Balatro-Style Shop

**Description:** Complete overhaul of the Draft Shop (Epic 7) into a multi-panel Balatro-inspired store with four distinct sections: Relics (items), Trading Deck Expansions (vouchers), Insider Tips (hidden intel), and Bonds (reputation investment). Replaces the current 3-card shop with a rich, strategic between-rounds experience.

**Status:** Ready for dev
**Phase:** Post-FIX Sprint, replaces/supersedes Epic 7 shop UI and flow
**Depends On:** FIX-12 (Reputation currency), FIX-14 (Economy rebalance)

**Layout Reference (Balatro-inspired):**
```
┌──────────────────────────────────────────────────────────────┐
│  ┌────────────┐                                              │
│  │ Next Round │   [RELIC 1]     [RELIC 2]     [RELIC 3]      │
│  │            │   $cost         $cost         $cost          │
│  │ Reroll     │                                              │
│  │ $cost      │   (Top Section — Relics / Items)             │
│  └────────────┘                                              │
│──────────────────────────────────────────────────────────────│
│  TRADING DECK EXPANSIONS    │    INSIDER TIPS       │ BONDS  │
│  (Vouchers — Bottom Left)   │    (Bottom Center)    │(Right) │
│                             │                       │        │
│  [EXPANSION 1]              │  [? ? ?]  [? ? ?]     │ [BOND] │
│  [EXPANSION 2]              │  (hidden until bought) │ $cost  │
│  [EXPANSION 3]              │                       │        │
│  (one-time permanent        │  One-time per visit    │ +Rep/  │
│   upgrades)                 │                       │ round  │
└──────────────────────────────────────────────────────────────┘
```

---

## Story 13.1: Store Data Model & State Management

As a developer, I want a clean data model that tracks all store state (owned relics, expansions, tips, bonds, reroll count) within RunContext, so that store state persists correctly across rounds and integrates with save/load.

**Acceptance Criteria:**
- `RunContext` extended with:
  - `OwnedRelics: List<string>` (item IDs, max = `MaxRelicSlots`)
  - `OwnedExpansions: List<string>` (expansion IDs, permanent per run)
  - `BondsOwned: int` (total bonds held)
  - `BondPurchaseHistory: List<int>` (which rounds bonds were bought — for sell price calc)
  - `CurrentShopRerollCount: int` (resets per shop visit)
  - `InsiderTipSlots: int` (default 2, increased by expansion)
  - `RevealedTips: List<InsiderTip>` (tips bought this shop visit, cleared on shop close)
- `ShopState` orchestrates all four panels:
  - Generates relic offering via `ShopGenerator`
  - Selects available expansions via `ExpansionManager`
  - Generates insider tips via `InsiderTipGenerator`
  - Calculates bond price via `BondManager`
- All purchases are atomic (validate → deduct currency → apply effect → fire event)
- State survives round transitions correctly
- Old `ActiveItems` field in RunContext migrated to `OwnedRelics`

**Files to modify:**
- `Assets/Scripts/Runtime/Core/RunContext.cs` — expanded state fields
- `Assets/Scripts/Runtime/Core/GameStates/ShopState.cs` — orchestration of all panels
- `Assets/Scripts/Runtime/Shop/ShopGenerator.cs` — adapted for relic-only generation
- `Assets/Scripts/Runtime/Shop/ShopTransaction.cs` — updated for multi-panel purchase types

---

## Story 13.2: Store Layout & Navigation Shell

As a player, I want the between-rounds store to have a clear multi-panel layout with distinct sections for Relics, Expansions, Insider Tips, and Bonds, so that I can quickly understand my options and make strategic purchases.

**Acceptance Criteria:**
- Store UI replaces the current `ShopUI.cs` single-panel layout
- **Top section:** Relic cards (3 slots) with reroll button and "Next Round" button on the left
- **Bottom-left panel:** Trading Deck Expansions (vouchers) — labeled section
- **Bottom-center panel:** Insider Tips — labeled section
- **Bottom-right panel:** Bonds — labeled section
- Current Reputation balance displayed prominently (amber/gold star icon, matching existing style)
- Current cash balance also visible
- Panel borders and labels match a dark, card-game aesthetic
- Store remains untimed (player clicks "Next Round" to proceed)
- `ShopOpenedEvent` and `ShopClosedEvent` still fire with updated payload
- Keyboard navigation between panels (arrow keys or tab)

**Files to modify:**
- `Assets/Scripts/Runtime/UI/ShopUI.cs` — complete rewrite of layout
- `Assets/Scripts/Setup/UISetup.cs` — shop panel construction
- `Assets/Scripts/Runtime/Core/GameStates/ShopState.cs` — updated to orchestrate new sections
- `Assets/Scripts/Runtime/Core/GameEvents.cs` — updated shop event payloads

---

## Story 13.3: Relics Panel — Item Offering, Purchase & Reroll

As a player, I want the top section of the store to show 3 randomly selected relics (items) that I can purchase with Reputation, and a reroll button to refresh the selection, so that I have meaningful choices and agency in my build.

**Acceptance Criteria:**
- 3 relic cards displayed in the top section, drawn randomly from the item pool
- Items drawn using uniform random selection (no rarity system — all relics are equally likely to appear)
- Owned items excluded from the offering (no duplicates)
- Each card shows: name, description, cost (Reputation)
- Purchase button per card — deducts Reputation, adds item to inventory
- Card disappears or shows "SOLD" after purchase
- **Reroll button** on the left side, below "Next Round"
  - Costs Reputation to reroll (configurable: `RerollBaseCost` in GameConfig)
  - Reroll cost increases each time used per shop visit (e.g., +2 Rep per reroll)
  - Reroll cost resets each shop visit
  - Regenerates all 3 unsold relic slots with new random items
- **Item limit:** Player can hold a maximum of 5 relics (configurable: `MaxRelicSlots` in GameConfig)
  - If at capacity, purchase buttons are disabled with "FULL" indicator
  - Items that expand capacity (from Trading Deck Expansions) increase this limit
- If the entire relic pool is exhausted (all items owned), slots show "SOLD OUT"
- `ShopItemPurchasedEvent` fires on purchase (existing event, updated if needed)
- **No rarity system** — relics do not have rarity tiers. Cost alone determines value/power
- Relic definitions (items, effects, costs) are OUT OF SCOPE for this epic — they will be designed separately in a future epic. This story only builds the relic panel infrastructure and purchase flow

**Files to modify:**
- `Assets/Scripts/Runtime/UI/ShopUI.cs` — relic panel rendering
- `Assets/Scripts/Runtime/Shop/ShopGenerator.cs` — updated to generate 3 items via uniform random (remove rarity-weighted selection)
- `Assets/Scripts/Runtime/Shop/ShopTransaction.cs` — capacity check before purchase
- `Assets/Scripts/Setup/Data/GameConfig.cs` — `RerollBaseCost`, `RerollCostIncrement`, `MaxRelicSlots`

---

## Story 13.4: Trading Deck Expansions Panel (Vouchers)

As a player, I want a bottom-left panel offering permanent one-time upgrades that expand my trading capabilities, so that I can invest Reputation into unlocking powerful new mechanics across the run.

**Acceptance Criteria:**
- Bottom-left panel labeled "TRADING DECK EXPANSIONS" (or similar thematic name)
- Displays available expansion upgrades as purchasable cards
- Each expansion can only be purchased **once per run** — it disappears or shows "OWNED" after purchase
- Expansions persist for the rest of the run (permanent within the run)
- Not all expansions shown every shop visit — rotate 2-3 available per visit from the unowned pool
- Purchase deducts Reputation

**Expansion Definitions:**

| Expansion | Effect | Cost (Rep) | Notes |
|-----------|--------|------------|-------|
| Multi-Stock Trading | Trade 2 stocks simultaneously per round | 80 | Adds a second stock to the round |
| Leverage Trading | Trade with 2x leverage (double gains/losses) | 60 | Multiplies P&L on long trades |
| Expanded Inventory | +2 relic slots (5 → 7 max) | 50 | Stacks if multiple tiers exist |
| Dual Short | Short a second stock simultaneously | 70 | Requires Multi-Stock or applies to same stock with 2 shorts |
| Intel Expansion | Purchase 1 additional Insider Tip per shop visit | 40 | Default is 2, this makes it 3 |
| Extended Trading | +15 seconds to round timer | 55 | Stacks up to 2x |

- Each expansion has: name, description, cost, and a visual icon/card
- Expansion effects must integrate with existing systems (e.g., Leverage modifies `TradeExecutor`, Multi-Stock modifies `MarketOpenState` stock count)
- Config constants for all expansion costs in `GameConfig`

**Files to modify:**
- `Assets/Scripts/Runtime/UI/ShopUI.cs` — expansion panel rendering
- `Assets/Scripts/Setup/Data/ExpansionDefinitions.cs` — NEW: expansion data definitions
- `Assets/Scripts/Runtime/Shop/ExpansionManager.cs` — NEW: tracks owned expansions, applies effects
- `Assets/Scripts/Runtime/Core/RunContext.cs` — `ActiveExpansions` list
- `Assets/Scripts/Setup/Data/GameConfig.cs` — expansion costs
- `Assets/Scripts/Runtime/Core/GameEvents.cs` — `ExpansionPurchasedEvent`

---

## Story 13.5: Insider Tips Panel (Hidden Intel)

As a player, I want a bottom-center panel where I can purchase mystery intel cards that reveal hidden information about the next round, so that I can gain an information edge at the cost of Reputation — but I won't know exactly what I'm getting until I buy it.

**Acceptance Criteria:**
- Bottom-center panel labeled "INSIDER TIPS"
- Shows **2 purchasable tip slots by default** (expandable to 3 via Trading Deck Expansion)
- Tips are displayed as **face-down / mystery cards** — showing only a cost and a "?" icon
- After purchase, the card flips/reveals to show the actual intel
- The revealed information stays visible for the rest of the shop phase
- Each tip is a **one-time purchase** — once bought, the slot shows the revealed info (not re-purchasable)
- Tips are randomly selected from the tip pool each shop visit
- Purchase deducts Reputation

**Insider Tip Pool:**

| Tip | Revealed Information | Cost (Rep) |
|-----|---------------------|------------|
| Price Forecast | "Average price this round will be ~$X.XX" | 15 |
| Price Floor | "Price won't drop below ~$X.XX" | 20 |
| Price Ceiling | "Price won't exceed ~$X.XX" | 20 |
| Trend Direction | "Market is trending [BULLISH / BEARISH / NEUTRAL]" | 15 |
| Event Forecast | "Expect [MOSTLY GOOD / MOSTLY BAD / MIXED] events" | 25 |
| Event Count | "There will be [N] events this round" | 10 |
| Volatility Warning | "Expect [HIGH / LOW / EXTREME] volatility" | 15 |
| Opening Price | "Stock opens at ~$X.XX" | 20 |

- Tip values are calculated based on the **already-generated** next round parameters (round must be pre-generated before shop opens, or tips are calculated from round config/seed)
- Values should include slight fuzz (±10%) so tips are helpful but not exact — e.g., "Average price ~$3.20" when actual average will be $3.45
- Tips apply to the immediately upcoming round only

**Files to modify:**
- `Assets/Scripts/Runtime/UI/ShopUI.cs` — insider tips panel, card flip animation
- `Assets/Scripts/Setup/Data/InsiderTipDefinitions.cs` — NEW: tip pool definitions
- `Assets/Scripts/Runtime/Shop/InsiderTipGenerator.cs` — NEW: selects tips, calculates values from next round data
- `Assets/Scripts/Runtime/Core/RunContext.cs` — `RevealedTips` for current round
- `Assets/Scripts/Setup/Data/GameConfig.cs` — tip costs, fuzz percentage
- `Assets/Scripts/Runtime/Core/GameEvents.cs` — `InsiderTipPurchasedEvent`

---

## Story 13.6: Bonds Panel (Reputation Investment)

As a player, I want a bonds section where I can invest cash now to earn recurring Reputation in future rounds, so that I have a long-term investment strategy alongside my immediate trading.

**Acceptance Criteria:**
- Bottom-right panel labeled "BONDS"
- Displays 1 bond available for purchase each shop visit
- **Bond purchase uses CASH (not Reputation)** — this is an investment of trading capital
- Bond price increases every round:
  - Round 1: $3, Round 2: $5, Round 3: $8, Round 4: $12, Round 5: $17, Round 6: $23, Round 7: $30, Round 8: N/A (last round, no future to earn)
  - Prices configurable in `GameConfig.BondPricePerRound[]`
- **Reputation earned from bonds is cumulative and compounds:**
  - Each bond owned generates +1 Rep at the START of each subsequent round
  - Example: Buy bond R1 → R2 start: +1 Rep. Buy another bond R2 → R3 start: +2 Rep (both bonds pay). R4 start: +2 Rep. Etc.
  - Total Rep earned from bonds = sum of (bonds_owned × rounds_remaining_after_purchase)
- Bond count displayed: "Bonds Owned: X" with projected Rep earnings shown
- **Sell bonds:** Player can sell any owned bond for **half the original purchase price** (cash back)
  - Selling removes 1 bond from the count, reducing future Rep earnings
  - Sell button visible when bonds > 0
  - Confirmation prompt before selling ("Sell 1 bond for $X?")
- Cannot purchase bonds on Round 8 (no future rounds to earn from)
- Bond panel shows: current bond price, bonds owned, Rep earned per round from bonds, sell price

**Bond Rep Payout Timing:**
- Bond Rep is awarded at round START (before trading begins), displayed as "+X Rep from Bonds" during market open phase
- This is separate from the round-end performance Rep (FIX-14)

**Files to modify:**
- `Assets/Scripts/Runtime/UI/ShopUI.cs` — bond panel rendering
- `Assets/Scripts/Runtime/Shop/BondManager.cs` — NEW: tracks bonds, calculates payouts, handles sell
- `Assets/Scripts/Runtime/Core/RunContext.cs` — `BondsOwned`, bond purchase history
- `Assets/Scripts/Setup/Data/GameConfig.cs` — `BondPricePerRound[]`, `BondSellMultiplier`
- `Assets/Scripts/Runtime/Core/GameStates/MarketOpenState.cs` — bond Rep payout at round start
- `Assets/Scripts/Runtime/Core/GameEvents.cs` — `BondPurchasedEvent`, `BondSoldEvent`, `BondRepPaidEvent`
- `Assets/Scripts/Runtime/Core/ReputationManager.cs` — bond payout integration

---

## Story 13.7: Expansion Effects Integration

As a player, I want purchased Trading Deck Expansions to actually modify gameplay mechanics (multi-stock, leverage, extended timer, etc.), so that my store investments have tangible impact during trading rounds.

**Acceptance Criteria:**
- **Multi-Stock Trading:** `MarketOpenState` spawns 2 stocks when owned. Stock sidebar re-enabled. All trading/event systems handle 2 stocks.
- **Leverage Trading:** `TradeExecutor` applies 2x multiplier to long trade P&L when owned. Visual indicator on trade panel ("2x LEVERAGE").
- **Expanded Inventory:** `MaxRelicSlots` increased by 2. Shop relic purchase buttons re-enabled if previously at capacity.
- **Dual Short:** Short state machine allows 2 concurrent short positions. Second short button appears in UI.
- **Intel Expansion:** `InsiderTipSlots` increased from 2 to 3. Third mystery card appears in Insider Tips panel.
- **Extended Trading:** `RoundDurationSeconds` increased by 15 (stacking). Timer UI reflects new duration.
- Each expansion checks `RunContext.OwnedExpansions` at the appropriate system entry point
- Expansions do NOT stack with themselves (one-time purchase, one effect)

**Files to modify:**
- `Assets/Scripts/Runtime/Core/GameStates/MarketOpenState.cs` — multi-stock
- `Assets/Scripts/Runtime/Trading/TradeExecutor.cs` — leverage multiplier
- `Assets/Scripts/Runtime/Core/GameRunner.cs` — timer extension, short dual support
- `Assets/Scripts/Runtime/UI/ShopUI.cs` — dynamic slot rendering based on expansions
- `Assets/Scripts/Runtime/Shop/ExpansionManager.cs` — effect application logic
- Various UI files for expansion visual indicators

---

## Story 13.8: Store Visual Polish & Card Animations

As a player, I want store cards to have satisfying visual presentation with hover effects, purchase animations, and the mystery card flip for Insider Tips, so that the store feels premium and engaging.

**Acceptance Criteria:**
- Relic cards: hover glow effect, consistent card border style (no rarity colors — single unified look)
- Purchase animation: card slides up and fades, "SOLD" stamp appears briefly
- Reroll animation: cards flip/shuffle before new ones appear
- Insider Tip cards: face-down with "?" symbol, flip animation on purchase revealing the intel
- Bond card: subtle pulsing glow, price tag prominent
- Expansion cards: clean layout with icon + description, "OWNED" watermark when purchased
- All text legible against dark panel backgrounds
- Consistent with existing UI style (programmatic uGUI, no prefabs)

**Files to modify:**
- `Assets/Scripts/Runtime/UI/ShopUI.cs` — animation coroutines, hover handlers
- `Assets/Scripts/Setup/UISetup.cs` — card visual construction (borders, backgrounds)

---

## Story 13.9: Cleanup & Migration from Old Shop

As a developer, I want to cleanly remove the old 3-card draft shop implementation and migrate any still-relevant logic to the new store system, so that there is no dead code or conflicting behavior.

**Acceptance Criteria:**
- Old `ShopUI` single-panel layout code removed
- Old category-based generation (one Tool, one Intel, one Perk) removed entirely
- `ItemCategory` enum removed
- `ItemRarity` enum removed — rarity is no longer a concept in the game
- All 30 existing item definitions in `ShopItemDefinitions.cs` removed — relic items will be completely redesigned in a future epic
- Rarity-weighted selection logic in `ShopGenerator` replaced with uniform random
- Old "Trade Volume" upgrade card (FIX-13) migrated to Trading Deck Expansions
- All existing shop tests updated or replaced to cover new store behavior
- No regressions in: purchase flow, reputation deduction, item inventory display
- Event payloads updated to reflect new store sections

**Files to modify:**
- `Assets/Scripts/Runtime/UI/ShopUI.cs` — remove old layout code
- `Assets/Scripts/Setup/Data/ShopItemDefinitions.cs` — gut existing 30 items, remove rarity/category enums (placeholder minimal pool for testing only)
- `Assets/Scripts/Runtime/Shop/ShopGenerator.cs` — remove rarity-weighted selection
- `Assets/Tests/Runtime/Shop/` — all test files updated
- `Assets/Scripts/Runtime/Core/GameEvents.cs` — event cleanup

---

## Dependency Graph

```
13.1 (Data Model)
  └── 13.2 (Layout Shell)
        ├── 13.3 (Relics) ──────────┐
        ├── 13.4 (Expansions) ──────┤
        ├── 13.5 (Insider Tips) ────┤──→ 13.7 (Expansion Effects)
        └── 13.6 (Bonds) ───────────┘──→ 13.8 (Visual Polish)
                                      ──→ 13.9 (Cleanup & Migration)
```

**Recommended implementation order:**
1. **13.1** — Data model first (foundation for everything)
2. **13.2** — Layout shell (empty panels with navigation)
3. **13.3** — Relics panel (most similar to existing shop, easiest win)
4. **13.4** — Trading Deck Expansions (new system, but UI-simple)
5. **13.5** — Insider Tips (requires round pre-generation hookup)
6. **13.6** — Bonds (new economy mechanic, needs careful testing)
7. **13.7** — Expansion effects (largest integration surface)
8. **13.8** — Visual polish (after all panels functional)
9. **13.9** — Cleanup (last — remove old code once new is stable)

---

## Config Constants Summary (GameConfig additions)

```csharp
// Epic 13: Store Rework
public static readonly int MaxRelicSlots = 5;
public static readonly int RerollBaseCost = 5;        // Reputation
public static readonly int RerollCostIncrement = 2;   // +2 per reroll per visit
public static readonly int DefaultInsiderTipSlots = 2;
public static readonly float InsiderTipFuzzPercent = 0.10f; // ±10% accuracy
public static readonly float BondSellMultiplier = 0.5f;     // Sell for half price
public static readonly int[] BondPricePerRound = new int[] { 3, 5, 8, 12, 17, 23, 30, 0 };
public static readonly int BondRepPerRoundPerBond = 1;

// Expansion costs (Reputation)
public static readonly int ExpansionCostMultiStock = 80;
public static readonly int ExpansionCostLeverage = 60;
public static readonly int ExpansionCostExpandedInventory = 50;
public static readonly int ExpansionCostDualShort = 70;
public static readonly int ExpansionCostIntelExpansion = 40;
public static readonly int ExpansionCostExtendedTrading = 55;

// Insider tip costs (Reputation)
public static readonly int TipCostPriceForecast = 15;
public static readonly int TipCostPriceFloor = 20;
public static readonly int TipCostPriceCeiling = 20;
public static readonly int TipCostTrendDirection = 15;
public static readonly int TipCostEventForecast = 25;
public static readonly int TipCostEventCount = 10;
public static readonly int TipCostVolatility = 15;
public static readonly int TipCostOpeningPrice = 20;
```
