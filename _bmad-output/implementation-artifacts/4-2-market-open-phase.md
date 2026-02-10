# Story 4.2: Market Open Phase

Status: ready-for-dev

## Story

As a player,
I want a brief market preview showing available stocks, a news headline, and the profit target before trading starts,
so that I can plan my strategy for the round.

## Acceptance Criteria

1. A 5–10 second preview phase displays before trading begins each round
2. Shows which stocks are available for the round with ticker symbols and starting prices
3. Displays a short news headline hinting at price direction for the round
4. Shows the profit target the player must hit this round
5. Shows the current act and round number (e.g., "Act 2 — Round 3")
6. Automatically transitions to TradingState when preview time expires
7. Preview cannot be skipped (ensures player sees the information)

## Tasks / Subtasks

- [ ] Task 1: Create MarketOpenState (AC: 1, 6)
  - [ ] On Enter: initialize round stocks via PriceGenerator, set preview timer (5-10s from GameConfig)
  - [ ] On Update: countdown preview timer
  - [ ] On timer expiry: transition to TradingState
  - [ ] Publish `MarketOpenEvent` on Enter with round details
  - [ ] File: `Scripts/Runtime/Core/GameStates/MarketOpenState.cs`
- [ ] Task 2: Create MarketOpenUI screen (AC: 2, 3, 4, 5)
  - [ ] Center panel showing:
    - Act and round header: "ACT 2 — ROUND 3"
    - Stock list: ticker symbols with starting prices and tier indicators
    - News headline: randomly selected hint text relevant to round events
    - Profit target: "$600" in large prominent text
  - [ ] Animated entrance (fade in or slide up)
  - [ ] File: `Scripts/Runtime/UI/MarketOpenUI.cs`
- [ ] Task 3: Create news headline system (AC: 3)
  - [ ] Define headline templates in a data class
  - [ ] Headlines hint at round conditions: "Tech sector showing strength", "Analysts warn of volatility ahead", "Penny stocks see unusual volume"
  - [ ] Select headline based on round's stock tier and scheduled events (loose hints, not spoilers)
  - [ ] File: `Scripts/Setup/Data/NewsHeadlineData.cs` (new)
- [ ] Task 4: Add preview duration to GameConfig (AC: 1)
  - [ ] Add `MarketOpenDurationSeconds` = 7f (default, tunable)
  - [ ] File: `Scripts/Setup/Data/GameConfig.cs` (extend)
- [ ] Task 5: Define MarketOpenEvent (AC: 6)
  - [ ] `MarketOpenEvent`: RoundNumber, Act, StockIds, ProfitTarget, Headline
  - [ ] Add to `Scripts/Runtime/Core/GameEvents.cs`
- [ ] Task 6: Add MarketOpenUI to UISetup (AC: 2)
  - [ ] Generate overlay panel that appears during MarketOpen state
  - [ ] File: `Scripts/Setup/UISetup.cs` (extend)

## Dev Notes

### Architecture Compliance

- **State machine:** MarketOpenState is one of the defined states: `MetaHub → **MarketOpen** → Trading → MarketClose → Shop → RunSummary`
- **Location:** `Scripts/Runtime/Core/GameStates/MarketOpenState.cs`
- **Round initialization:** This is where PriceGenerator creates stocks for the round. MarketOpenState calls `PriceGenerator.InitializeRound(act, round)` on Enter.
- **EventBus:** Publishes `MarketOpenEvent` so UI and other systems can react
- **Data:** News headlines in `Scripts/Setup/Data/` as static data

### Round Initialization Flow

MarketOpenState.Enter() is the orchestrator for round setup:
1. Determine current act and tier from RunContext
2. Call PriceGenerator.InitializeRound() — selects stocks, sets trends
3. Set up EventScheduler with events for this round (when Epic 5 exists)
4. Capture Portfolio.StartRound() baseline for profit tracking
5. Display preview UI
6. Start countdown to trading

### News Headlines — Loose Hints

Headlines should be fun and thematic, not precise spoilers:
- Bullish round: "Markets rally on optimism" / "Green across the board"
- Bearish round: "Storm clouds gathering on Wall Street" / "Analysts urge caution"
- Volatile round: "Buckle up — it's going to be a wild ride" / "Volume surging across sectors"
- Neutral: "Markets await direction" / "Traders holding their breath"

The headline gives flavor and a loose directional hint. It's NOT a guaranteed prediction.

### GDD Phase 1 Reference (Section 2.2)

> "The round begins with a brief market preview. The player sees which stocks are available for this round, a short news headline hinting at price direction, and the profit target they must hit."

### Project Structure Notes

- Creates: `Scripts/Runtime/Core/GameStates/MarketOpenState.cs`
- Creates: `Scripts/Runtime/UI/MarketOpenUI.cs`
- Creates: `Scripts/Setup/Data/NewsHeadlineData.cs`
- Modifies: `Scripts/Setup/Data/GameConfig.cs`
- Modifies: `Scripts/Runtime/Core/GameEvents.cs`
- Modifies: `Scripts/Setup/UISetup.cs`

### References

- [Source: game-architecture.md#Game State Machine] — State flow: MetaHub → MarketOpen → Trading...
- [Source: game-architecture.md#Game State Transitions] — IGameState Enter/Update/Exit pattern
- [Source: bull-run-gdd-mvp.md#2.2] — "Phase 1: Market Open (5–10 seconds)" — preview description
- [Source: bull-run-gdd-mvp.md#2.3] — Margin call targets per round
- [Source: bull-run-gdd-mvp.md#3.4] — Event types that inform headline generation

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
