# Story 13.4: Insider Tips Panel (Hidden Intel)

Status: pending

## Story

As a player,
I want a bottom-center panel where I can purchase mystery intel cards that reveal hidden information about the next round,
so that I can gain an information edge at the cost of Reputation — but I won't know exactly what I'm getting until I buy it.

## Acceptance Criteria

1. Bottom-center panel labeled "INSIDER TIPS"
2. Shows 2 purchasable tip slots by default (expandable to 3 via Intel Expansion)
3. Tips displayed as face-down/mystery cards — showing only a cost and a "?" icon
4. After purchase, the card flips/reveals to show the actual intel text
5. Revealed information stays visible for the rest of the shop phase
6. Each tip is a one-time purchase — once bought, slot shows revealed info (not re-purchasable)
7. Tips randomly selected from tip pool each shop visit (no duplicates within same visit)
8. Purchase deducts Reputation
9. `InsiderTipPurchasedEvent` fires on purchase
10. Tip values calculated from next round's pre-generated parameters
11. Tip values include ±10% fuzz (helpful but not exact)

## Insider Tip Pool

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

## Tasks / Subtasks

- [ ] Task 1: Create InsiderTipDefinitions data (AC: 7)
  - [ ] Define `InsiderTipType` enum: PriceForecast, PriceFloor, PriceCeiling, TrendDirection, EventForecast, EventCount, VolatilityWarning, OpeningPrice
  - [ ] Define `InsiderTipDef` struct: `Type`, `DescriptionTemplate`, `Cost`
  - [ ] All 8 tip types defined as `public static readonly` data
  - [ ] File: `Scripts/Setup/Data/InsiderTipDefinitions.cs` (NEW)
- [ ] Task 2: Create InsiderTipGenerator (AC: 7, 10, 11)
  - [ ] Plain C# class (not MonoBehaviour)
  - [ ] `GenerateTips(int slotCount, RoundConfig nextRound)` — select N random tip types, calculate values
  - [ ] Value calculation per tip type:
    - PriceForecast: calculate expected average price from trend + noise parameters, apply ±10% fuzz
    - PriceFloor/Ceiling: use min/max from price range config, apply ±10% fuzz
    - TrendDirection: read trend direction directly (no fuzz — categorical)
    - EventForecast: classify scheduled events as mostly good/bad/mixed
    - EventCount: exact count of scheduled events (no fuzz — integer)
    - VolatilityWarning: classify from noise amplitude config
    - OpeningPrice: read starting price, apply ±10% fuzz
  - [ ] No duplicate tip types within same shop visit
  - [ ] File: `Scripts/Runtime/Shop/InsiderTipGenerator.cs` (NEW)
- [ ] Task 3: Round pre-generation hookup (AC: 10)
  - [ ] Ensure next round parameters are generated BEFORE shop opens so tips can read them
  - [ ] If not already pre-generated, generate round seed/config during ShopState.Enter()
  - [ ] Tip generator reads from this pre-generated data
  - [ ] File: `Scripts/Runtime/Core/GameStates/ShopState.cs` — pre-generation call
- [ ] Task 4: Mystery card UI (AC: 2, 3, 4, 5, 6)
  - [ ] 2 card slots by default in the Insider Tips panel (created in 13.1)
  - [ ] Face-down state: dark card with "?" icon and cost label
  - [ ] Face-up state (after purchase): tip type name + revealed value text
  - [ ] Purchased cards stay face-up for remainder of shop visit
  - [ ] Dynamic slot count: check `InsiderTipSlots` from RunContext (default 2, 3 with expansion)
  - [ ] File: `Scripts/Runtime/UI/ShopUI.cs` — insider tips panel population
- [ ] Task 5: Purchase flow (AC: 6, 8, 9)
  - [ ] Purchase button on face-down card: validate affordability → deduct Rep → reveal tip → fire event
  - [ ] Card transitions from face-down to face-up (flip animation in 13.8, simple swap for now)
  - [ ] `InsiderTipPurchasedEvent`: TipType, RevealedValue, Cost, RemainingReputation
  - [ ] Store revealed tips in `RunContext.RevealedTips` for potential use during trading phase
  - [ ] File: `Scripts/Runtime/Shop/ShopTransaction.cs` — tip purchase path
- [ ] Task 6: Tip display during trading (AC: 5)
  - [ ] Revealed tips from the most recent shop visit are available during the next trading round
  - [ ] Small indicator or tooltip showing purchased tips during MarketOpen/Trading phases
  - [ ] Tips cleared from RunContext when next shop visit opens
  - [ ] File: `Scripts/Runtime/UI/TradingHUD.cs` — tip display integration
- [ ] Task 7: GameConfig constants (AC: 2, 11)
  - [ ] `DefaultInsiderTipSlots = 2`
  - [ ] `InsiderTipFuzzPercent = 0.10f`
  - [ ] Individual tip costs: `TipCostPriceForecast = 15`, etc.
  - [ ] File: `Scripts/Setup/Data/GameConfig.cs`
- [ ] Task 8: GameEvents update (AC: 9)
  - [ ] Define `InsiderTipPurchasedEvent`: TipType, RevealedText, Cost, RemainingReputation
  - [ ] File: `Scripts/Runtime/Core/GameEvents.cs`
- [ ] Task 9: Write tests (All AC)
  - [ ] InsiderTipGenerator: correct number of tips, no duplicates, values within fuzz range
  - [ ] Purchase flow: Rep deducted, tip revealed, event fired, stored in RunContext
  - [ ] Slot count respects Intel Expansion
  - [ ] Files: `Tests/Runtime/Shop/InsiderTipGeneratorTests.cs`, `Tests/Runtime/Shop/InsiderTipPurchaseTests.cs`

## Dev Notes

### Architecture Compliance

- **Plain C# for logic:** `InsiderTipGenerator` is NOT a MonoBehaviour.
- **Static data:** Tip definitions in `Scripts/Setup/Data/InsiderTipDefinitions.cs`.
- **Round pre-generation:** This is the most architecturally complex part. The next round's price engine parameters must be accessible before trading starts. Check how `MarketOpenState` currently initializes round data and determine if that can be pre-seeded during ShopState.

### Critical Design: Fuzz

Tips must NOT be perfectly accurate. The ±10% fuzz means:
- A tip saying "Average price ~$3.20" might correspond to an actual average of $2.88–$3.52
- This makes tips useful for strategy but not a cheat code
- Categorical tips (TrendDirection, EventForecast, VolatilityWarning) don't have fuzz — they give qualitative info

### Existing Code to Understand

Before implementing, the dev agent MUST read:
- `Scripts/Runtime/PriceEngine/` — how price generation works, what parameters exist
- `Scripts/Runtime/Events/EventScheduler.cs` — how events are scheduled (for event count/forecast tips)
- `Scripts/Runtime/Core/GameStates/MarketOpenState.cs` — where round initialization happens
- `Scripts/Setup/Data/StockTierData.cs` — stock tier configs for price range/volatility data
- `Scripts/Runtime/Core/RunContext.cs` — where to store RevealedTips

### Depends On

- Story 13.1 (Store Layout Shell) — insider tips panel must exist
- Story 13.6 (Data Model) — `InsiderTipSlots`, `RevealedTips` fields in RunContext

## Dev Agent Record

### Agent Model Used

### Completion Notes List

### Change Log

### File List
