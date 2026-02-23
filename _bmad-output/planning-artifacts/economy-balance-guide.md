# Economy Balance Guide

Reference document for tuning BullRun's core economy. All values live in `Assets/Scripts/Setup/Data/` as `public static readonly` constants — no ScriptableObjects, no Inspector config.

---

## Profit Targets (Margin Call Thresholds)

**File:** `MarginCallTargets.cs`

Players must end each round with total cash >= the target or they trigger a margin call and lose the run.

| Round | Act | Tier | Target | Round Growth | From $10 |
|-------|-----|------|--------|-------------|----------|
| 1 | 1 | Penny | $20 | 2.0x | 2x |
| 2 | 1 | Penny | $55 | 2.75x | 5.5x |
| 3 | 2 | Low-Value | $160 | 2.9x | 16x |
| 4 | 2 | Low-Value | $500 | 3.1x | 50x |
| 5 | 3 | Mid-Value | $1,600 | 3.2x | 160x |
| 6 | 3 | Mid-Value | $5,500 | 3.4x | 550x |
| 7 | 4 | Blue Chip | $20,000 | 3.6x | 2,000x |
| 8 | 4 | Blue Chip | $65,000 | 3.25x | 6,500x |

**Design intent:** Accelerating growth rate (2.0x early → 3.6x late). Accessible early game lets novices learn the ropes; brutal late game demands relic synergies + sharp trading. Skilled players win ~50% of runs.

### Why These Numbers

The accelerating curve mirrors how relic compounding works in practice — early rounds are pure trading skill, but by Act 3-4 the player should have powerful relic combinations that enable (and require) exponential growth. The targets track this:

- **Rounds 1-2 (2.0-2.75x):** Approachable — new players survive long enough to learn
- **Rounds 3-4 (2.9-3.1x):** Ramp-up — relics start mattering, trading alone isn't enough
- **Rounds 5-6 (3.2-3.4x):** Pressure — bad relic builds start failing here
- **Rounds 7-8 (3.6-3.25x):** Brutal — only strong builds with sharp play clear these
- The final target ($65,000) requires consistent compounding across all 8 rounds

### Tuning Levers

To make the game **easier**, reduce round-over-round growth rate toward 2.5x:
```
Example easier curve: $20, $50, $125, $300, $750, $2000, $5000, $12000
```

To make the game **harder**, increase toward 3.5x:
```
Example harder curve: $30, $100, $350, $1200, $4000, $14000, $50000, $150000
```

**Rule of thumb:** Each 0.1x change in average compound rate roughly doubles/halves the final target.

### Debug Starting Cash

**File:** `GameConfig.cs` — `DebugStartingCash[]`

These values power the F3 skip-to-round debug feature. Set ~20-25% above the previous round's target so the player has a fighting chance when skipping ahead.

| Round | Debug Cash | Based On |
|-------|-----------|----------|
| 1 | $10 | Starting capital |
| 2 | $25 | ~25% above R1 target ($20) |
| 3 | $70 | ~27% above R2 target ($55) |
| 4 | $200 | ~25% above R3 target ($160) |
| 5 | $600 | ~20% above R4 target ($500) |
| 6 | $2,000 | ~25% above R5 target ($1,600) |
| 7 | $7,000 | ~27% above R6 target ($5,500) |
| 8 | $25,000 | ~25% above R7 target ($20,000) |

**Always update these when changing targets.** A debug skip into an unwinnable round wastes testing time.

---

## Cash Earning Mechanics

### Per-Round Trading

**File:** `GameConfig.cs`

| Constant | Value | Effect |
|----------|-------|--------|
| `StartingCapital` | $10 | Cash at run start |
| `RoundDurationSeconds` | 60s | Trading window |
| `PostTradeCooldown` | 1.0s | Lockout between trades |
| `DefaultTradeQuantity` | 1 | Shares per click |
| `ShortMarginRequirement` | 50% | Collateral to open shorts |
| `PriceFloorPercent` | 10% | Min price = 10% of opening |

A skilled player executes 15-25 trades per 60s round. Trade cooldown is the primary throttle on earnings.

### Stock Tier Price Ranges

**File:** `StockTierData.cs`

| Tier | Price Range | Volatility | Trend Strength | Expected 60s Move |
|------|------------|------------|----------------|-------------------|
| Penny | $5-$8 | 25% | 0.008-0.025 | ±50-100% |
| Low-Value | $5-$50 | 10% | 0.002-0.008 | ±15-30% |
| Mid-Value | $50-$500 | 6% | 0.001-0.007 | ±30-50% |
| Blue Chip | $150-$5,000 | 3% | 0.0005-0.006 | ±25-45% |

Higher tiers have lower percentage volatility but much higher absolute prices, so dollar-value profit per trade scales with tier progression.

### Market Events (Price Catalysts)

**File:** `EventDefinitions.cs`

| Event | Effect | Duration | Tiers | Rarity |
|-------|--------|----------|-------|--------|
| Earnings Beat | +25% to +50% | 4s | All | 0.5 |
| Earnings Miss | -15% to -30% | 4s | All | 0.5 |
| Pump & Dump | +45% to +90% | 6s | Penny | 0.3 |
| SEC Investigation | -20% to -40% | 6s | Penny+Low | 0.3 |
| Sector Rotation | ±18% | 5s | Mid+Blue | 0.4 |
| Merger Rumor | +30% to +60% | 5s | Mid+Blue | 0.3 |
| Market Crash | -20% to -40% | 6s | All | 0.15 |
| Bull Run | +35% to +65% | 6s | All | 0.15 |
| Flash Crash | -15% to -30% | 3s | Low+Mid | 0.25 |
| Short Squeeze | +45% to +100% | 3s | All | 0.25 |

**Events per round** (`EventSchedulerConfig`):
- Rounds 1-4: 5-7 events
- Rounds 5-8: 7-10 events
- Max 2 rare events per round

Positive events are intentionally stronger than negatives (FIX-18) to counteract multiplicative asymmetry (a -30% drop requires +43% to recover).

---

## Relic Multipliers (Cash Impact)

**File:** `ShopItemDefinitions.cs`

These relics directly multiply cash earnings and are the primary reason skilled players compound so fast:

| Relic | Effect | Cost | Cash Multiplier |
|-------|--------|------|----------------|
| Double Dealer | 2x trade quantity | 20 Rep | 2.0x per trade |
| Bear Raid | 3x short shares, no longs | 14 Rep | 3.0x on shorts |
| Diamond Hands | +30% value at liquidation | 25 Rep | 1.3x on holds |
| Skimmer | +3% of stock value on buy | 12 Rep | Passive bonus |
| Short Profiteer | +10% of stock value on short | 15 Rep | Passive bonus |
| Bull Believer | 2x positive event strength, no shorts | 15 Rep | 2.0x on upswings |
| Market Manipulator | -15% price on sell | 12 Rep | Creates short opps |
| Rep Dividend | $1 per 2 Rep per round | 20 Rep | Rep-to-cash conversion |

### Expansion Multipliers

| Expansion | Effect | Cost |
|-----------|--------|------|
| Leverage Trading | 2x long P&L | 30 Rep |
| Dual Short | 2 shares per short | 25 Rep |
| Extended Trading | +15s round timer | 25 Rep |
| Expanded Inventory | +2 relic slots | 20 Rep |

**Worst-case stacking example:** Double Dealer (2x shares) + Leverage (2x P&L) = 4x profit per long trade. Add Diamond Hands (+30% at close) and a Bull Run event (+65%) and a single trade can yield 5-8x returns.

---

## Reputation Economy

**File:** `GameConfig.cs`

### Earning Rep

| Source | Amount | Notes |
|--------|--------|-------|
| Round completion (base) | 10, 14, 18, 22, 28, 34, 40, 48 | Per round, 0-indexed |
| Performance bonus | `base × (excess/target) × 0.5` | Bonus for exceeding target |
| Profitable trade | +1 Rep | Per trade with positive P&L |
| Bonds owned | +1 Rep/bond/round | Passive at round start |
| Margin call consolation | 2 Rep × rounds completed | On failed run |

### Spending Rep

| Category | Cost Range | Notes |
|----------|-----------|-------|
| Relics | 5-35 Rep | 23 relics in pool |
| Expansions | 15-30 Rep | 5 permanent upgrades |
| Insider Tips | 10-35 Rep | 2-3 slots per shop |
| Bonds | Cash cost, Rep payout | $3-$30, +1 Rep/round/bond |
| Rerolls | 5 + 2 per reroll | Escalating cost per visit |

Rep generation scales with performance — exceeding targets by large margins grants significant bonus Rep, which funds more powerful relic builds, which enables exceeding targets by even more. This positive feedback loop is intentional and is the core progression engine.

---

## Balance Change Checklist

When adjusting economy values:

1. **Update `MarginCallTargets.Targets[]`** — the actual threshold values
2. **Update `MarginCallTargets.ScalingMultipliers[]`** — keep in sync for reference
3. **Update `GameConfig.DebugStartingCash[]`** — set ~20% above previous round's target
4. **Playtest Rounds 1-2 first** — early rounds are the most sensitive to small changes
5. **Check relic interactions** — Double Dealer + Leverage + Diamond Hands is the ceiling combo
6. **Watch for death spirals** — if targets require near-perfect play, bad RNG on events becomes run-ending with no counterplay
7. **Verify Rep economy still works** — if targets are too high, players over-spend on tips/relics trying to keep up and starve their Rep pipeline
