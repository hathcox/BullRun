# Story FIX-14: Economy Rebalance — $10 Start, Low Targets, Reputation Earnings

Status: ready-for-dev

## Story

As a player,
I want to start with $10, face a first-round target of $20, and earn Reputation at the end of each round based on how well I did,
so that the early game feels tight and scrappy, and every round contributes to my long-term progression.

## Problem Analysis

Current economy is built around $1,000 starting capital with targets starting at $200 (Story 6.3). With single-share trading (FIX-13) on penny stocks, $1,000 is absurdly overpowered — player can buy hundreds of shares immediately. The economy needs to be dramatically scaled down so that 1 share of a $2 penny stock is a meaningful commitment.

**Current Economy (GameConfig):**
- `StartingCapital = 1000f`
- `DebugStartingCash = { 1000, 1500, 2000, 3000, 4000, 6000, 8000, 12000 }`
- Margin call targets: $200, $350, $600, $900, $1500, $2200, $3500, $5000 (Story 6.3)
- No Reputation earning mechanism exists

**Desired Economy:**
- `StartingCapital = 10f`
- Targets: $20, $35, $60, $100, $175, $300, $500, $800
- Reputation earned each round: base + performance bonus
- Even failed runs award consolation Rep

**Key Interaction:**
- With $10 and penny stocks ($0.50–$5 range), buying 1 share at $2 uses 20% of capital
- First target of $20 means doubling your money in Round 1 — achievable with good penny stock trades
- Rep earning gives every round meaning even if the run ends in margin call

**Affected Code:**
- `Scripts/Setup/Data/GameConfig.cs` — StartingCapital, DebugStartingCash, new target array, Rep award constants
- `Scripts/Runtime/Core/GameRunner.cs` — round-end Reputation calculation and award
- `Scripts/Setup/UISetup.cs` — Rep earned display on round summary
- Margin call / round target logic — updated target values
- Stock tier price configs — verify penny stock prices work at $10 scale

## Acceptance Criteria

1. `StartingCapital` changed from `1000f` to `10f`
2. Round profit targets rebalanced:
   - Round 1: $20 | Round 2: $35
   - Round 3: $60 | Round 4: $100
   - Round 5: $175 | Round 6: $300
   - Round 7: $500 | Round 8: $800
3. `DebugStartingCash` array updated to match new economy scale
4. Reputation earned at end of each successfully completed round:
   - Base award scales with round number (Round 1 = 5 Rep, scaling up to Round 8 = 40 Rep)
   - Performance bonus: percentage of target exceeded → bonus Rep (e.g., 150% of target = +50% bonus Rep)
5. Reputation earned on margin call failure:
   - Consolation: 2 Rep per round completed before failure
6. Round summary screen shows Reputation earned breakdown (base + bonus or consolation)
7. Stock tier price ranges verified: penny stocks must be affordable at $10 scale ($0.50–$5 range)
8. All existing margin call target references updated to new values
9. Item costs (if any still use cash) reviewed — should be N/A after FIX-12 converts shop to Rep

## Tasks / Subtasks

- [ ] Task 1: Update StartingCapital and DebugStartingCash (AC: 1, 3)
  - [ ] Change `StartingCapital = 1000f` → `StartingCapital = 10f`
  - [ ] Update `DebugStartingCash` array:
    ```
    { 10f, 20f, 40f, 75f, 130f, 225f, 400f, 700f }
    ```
    (Approximate expected cash at each round based on hitting targets + compounding)
  - [ ] File: `Assets/Scripts/Setup/Data/GameConfig.cs`

- [ ] Task 2: Add margin call target array to GameConfig (AC: 2, 8)
  - [ ] Add `MarginCallTargets` array (or update existing target source):
    ```
    { 20f, 35f, 60f, 100f, 175f, 300f, 500f, 800f }
    ```
  - [ ] These are CUMULATIVE portfolio value targets (not per-round profit deltas) — verify how current `GetRoundProfit()` / margin call check works and align
  - [ ] NOTE: Need to check if targets are "round profit" (value gained this round) or "total portfolio value". Current code uses `Portfolio.GetRoundProfit()` which is `currentValue - roundStartValue`. Targets should match this semantic.
  - [ ] File: `Assets/Scripts/Setup/Data/GameConfig.cs`

- [ ] Task 3: Add Reputation earning constants to GameConfig (AC: 4, 5)
  - [ ] Add `RepBaseAwardPerRound` array: `{ 5, 8, 11, 15, 20, 26, 33, 40 }` — base Rep for completing each round
  - [ ] Add `RepPerformanceBonusRate = 0.5f` — bonus multiplier on target excess (e.g., 50% excess = 50% bonus)
  - [ ] Add `RepConsolationPerRound = 2` — Rep per round completed before margin call failure
  - [ ] File: `Assets/Scripts/Setup/Data/GameConfig.cs`

- [ ] Task 4: Implement Reputation earning on round completion (AC: 4, 6)
  - [ ] Subscribe to `RoundCompletedEvent` in GameRunner (or wherever Rep logic lives)
  - [ ] Calculate Rep earned:
    1. `baseRep = RepBaseAwardPerRound[roundIndex]`
    2. `excessRatio = max(0, (roundProfit - target) / target)` — how much player exceeded target
    3. `bonusRep = floor(baseRep * excessRatio * RepPerformanceBonusRate)`
    4. `totalRep = baseRep + bonusRep`
  - [ ] Call `ReputationManager.Add(totalRep)` (from FIX-12)
  - [ ] Publish event or store for round summary display
  - [ ] File: `Assets/Scripts/Runtime/Core/GameRunner.cs`

- [ ] Task 5: Implement consolation Rep on margin call failure (AC: 5)
  - [ ] Subscribe to `MarginCallTriggeredEvent` in GameRunner
  - [ ] Calculate: `consolationRep = roundsCompleted * RepConsolationPerRound`
  - [ ] Call `ReputationManager.Add(consolationRep)`
  - [ ] Include in run summary display
  - [ ] File: `Assets/Scripts/Runtime/Core/GameRunner.cs`

- [ ] Task 6: Update round summary screen to show Rep earned (AC: 6)
  - [ ] After round completion: display "Reputation Earned: ★ X (base) + ★ Y (bonus) = ★ Z"
  - [ ] After margin call: display "Consolation: ★ X (Y rounds completed)"
  - [ ] Use amber/gold styling consistent with FIX-12 Rep display
  - [ ] File: `Assets/Scripts/Setup/UISetup.cs` (round summary / transition UI)

- [ ] Task 7: Verify penny stock price ranges (AC: 7)
  - [ ] Check stock tier configuration for penny stocks — price range must include stocks affordable at $10
  - [ ] Current penny range: $0.10–$5 (from Story 1.5) — this works. A $2 stock costs $2 for 1 share, leaving $8 for more trades
  - [ ] Verify: no stock tier generates prices that make a single share unaffordable at $10 start
  - [ ] If penny stocks can hit $5+, player may not be able to buy — ensure at least some penny stocks are in $0.50–$3 range
  - [ ] File: Stock tier config (likely in GameConfig or a StockDefinitions file)

- [ ] Task 8: Update existing margin call target references (AC: 8)
  - [ ] Search codebase for hardcoded target values ($200, $350, etc.) from Story 6.3
  - [ ] Replace with references to new `GameConfig.MarginCallTargets` array
  - [ ] Verify `MarginCallTargets.GetTarget(roundNumber)` or equivalent reads from the new array
  - [ ] Files: Round management scripts, margin call check logic

- [ ] Task 9: Update HUD for $10-scale numbers (AC: 1)
  - [ ] Cash display formatting: at $10 scale, show 2 decimal places (e.g., "$10.00", "$12.47")
  - [ ] Verify profit display doesn't truncate small values (a $0.30 profit is significant at this scale)
  - [ ] Verify margin call target display shows correct new values
  - [ ] File: `Assets/Scripts/Setup/UISetup.cs`, HUD update scripts

- [ ] Task 10: Write tests (AC: 1-9)
  - [ ] Test: Starting capital is $10
  - [ ] Test: Margin call target for Round 1 is $20 (or equivalent profit target)
  - [ ] Test: All 8 round targets match expected values
  - [ ] Test: Rep earned on round completion — base only (exactly hit target)
  - [ ] Test: Rep earned on round completion — base + bonus (exceeded target by 50%)
  - [ ] Test: Rep earned on round completion — base + 0 bonus (exactly hit target, no excess)
  - [ ] Test: Consolation Rep on margin call — 0 rounds completed = 0 Rep
  - [ ] Test: Consolation Rep on margin call — 3 rounds completed = 6 Rep
  - [ ] Test: DebugStartingCash values match expected progression
  - [ ] Test: Penny stock prices affordable at $10 (at least one stock < $5)
  - [ ] File: `Assets/Tests/Runtime/Core/EconomyRebalanceTests.cs`
  - [ ] File: `Assets/Tests/Runtime/Core/ReputationEarningTests.cs`

## Dev Notes

### Architecture Compliance
- **GameConfig Constants:** All economy values in GameConfig — single source of truth.
- **EventBus:** Subscribe to existing `RoundCompletedEvent` and `MarginCallTriggeredEvent`. No new events needed for earning.
- **ReputationManager (FIX-12):** This story calls `ReputationManager.Add()` — depends on FIX-12 being implemented. If not, stub or implement inline.

### Key Design Decisions
- **$10 start with $20 target = 100% return required Round 1:** This sounds aggressive, but with penny stock volatility ($2 stock swinging 20-30% per round), catching a good trade on 1 share can net $0.40–$0.60, and the round has 60 seconds. With multiple buy/sell cycles (limited by 3s cooldown from FIX-10), $10 profit across the round is achievable. Tune if too hard/easy.
- **Targets are approximate:** The suggested values ($20, $35, $60...) assume compounding. If player hits targets exactly, they carry forward enough to hit the next. May need tuning.
- **Rep bonus is multiplicative on base:** `bonusRep = baseRep * excessRatio * bonusRate`. This means smashing the target rewards proportionally. A player who earns 3x the target gets significant bonus Rep.
- **Consolation Rep is flat per round:** 2 Rep per completed round. Simple, predictable. A player who dies on Round 5 gets 8 Rep (4 completed rounds * 2). Enough to feel like progress.
- **Targets as round profit vs total value:** CRITICAL — verify whether margin call checks `GetRoundProfit()` (delta this round) or total portfolio value. Current code uses `GetRoundProfit()` = `currentValue - roundStartValue`. So targets should be PROFIT targets, not absolute values. $20 target on Round 1 means earning $10 profit (starting from $10, ending at $20). Clarify during implementation.

### Dependencies
- **FIX-12 (Reputation currency):** Provides `ReputationManager.Add()`. If building FIX-14 first, create a minimal ReputationManager stub.
- **FIX-11 (short redesign):** Short P&L at $10 scale will be tiny. That's fine — shorting is a side mechanic.
- **FIX-13 (quantity unlocks):** 1-share trading at $10 economy is the intended feel. FIX-13 enforces x1 start.

### Edge Cases
- **Player earns exactly $0 profit:** Base Rep still awarded (they completed the round). Bonus = 0.
- **Player earns negative profit but above target:** Shouldn't happen — target is a minimum. But if somehow profit < 0 and above target, still award base Rep.
- **Margin call on Round 1:** 0 completed rounds × 2 Rep = 0 consolation Rep. Player gets nothing. Intentional — they didn't even finish one round.
- **Target overflow at late rounds:** $800 target on Round 8. If player has been compounding well, they might have $700+ cash. Verify the math works — player needs to profit $800 from a portfolio that might be $700-$1000+.
- **Stock prices vs capital:** At Round 5+ (Mid-Value, $50-$500 stocks), player might have $130 cash but stocks cost $50-$500. With x1 trading, a $200 stock is unaffordable. Quantity unlocks (FIX-13) and accumulated capital must bridge this gap. May need to verify stock price ranges per act align with expected capital at that round.

### Previous Story Learnings
- From Story 6.3: Escalating profit targets already exist conceptually — just need to change values.
- From Story 2.5: Capital management flow (cash carries forward) is already implemented.
- From FIX-10: Post-trade cooldown (3s) limits trade frequency — factors into whether $10→$20 is achievable in 60s with 1-share trades.

## Dev Agent Record

### Implementation Plan
_To be filled during implementation_

### Completion Notes
_To be filled after implementation_

### Debug Log
_To be filled during implementation_

## File List

_To be filled during implementation_

## Change Log

- 2026-02-14: Story created — $10 economy, rebalanced targets, Reputation earning at round end
