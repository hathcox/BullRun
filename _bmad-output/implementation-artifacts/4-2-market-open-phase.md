# Story 4.2: Market Open Phase

Status: done

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

- [x] Task 1: Create MarketOpenState (AC: 1, 6)
  - [x] On Enter: initialize round stocks via PriceGenerator, set preview timer (5-10s from GameConfig)
  - [x] On Update: countdown preview timer
  - [x] On timer expiry: transition to TradingState
  - [x] Publish `MarketOpenEvent` on Enter with round details
  - [x] File: `Scripts/Runtime/Core/GameStates/MarketOpenState.cs`
- [x] Task 2: Create MarketOpenUI screen (AC: 2, 3, 4, 5)
  - [x] Center panel showing:
    - Act and round header: "ACT 2 — ROUND 3"
    - Stock list: ticker symbols with starting prices and tier indicators
    - News headline: randomly selected hint text relevant to round events
    - Profit target: "$600" in large prominent text
  - [x] Animated entrance (fade in or slide up)
  - [x] File: `Scripts/Runtime/UI/MarketOpenUI.cs`
- [x] Task 3: Create news headline system (AC: 3)
  - [x] Define headline templates in a data class
  - [x] Headlines hint at round conditions: "Tech sector showing strength", "Analysts warn of volatility ahead", "Penny stocks see unusual volume"
  - [x] Select headline based on round's stock tier and scheduled events (loose hints, not spoilers)
  - [x] File: `Scripts/Setup/Data/NewsHeadlineData.cs` (new)
- [x] Task 4: Add preview duration to GameConfig (AC: 1)
  - [x] Add `MarketOpenDurationSeconds` = 7f (default, tunable)
  - [x] File: `Scripts/Setup/Data/GameConfig.cs` (extend)
- [x] Task 5: Define MarketOpenEvent (AC: 6)
  - [x] `MarketOpenEvent`: RoundNumber, Act, StockIds, ProfitTarget, Headline
  - [x] Add to `Scripts/Runtime/Core/GameEvents.cs`
- [x] Task 6: Add MarketOpenUI to UISetup (AC: 2)
  - [x] Generate overlay panel that appears during MarketOpen state
  - [x] File: `Scripts/Setup/UISetup.cs` (extend)

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

Claude Opus 4.6

### Debug Log References

- `[MarketOpenState] Enter: Act X, Round Y, Preview Zs` — logged on market open phase start
- `[MarketOpenState] Exit: transitioning to Trading` — logged on transition to trading
- `[Setup] MarketOpenUI created: full-screen overlay` — logged during F5 setup

### Completion Notes List

- MarketOpenState implements IGameState with configurable preview timer (default 7s)
- On Enter: initializes round stocks via PriceGenerator.InitializeRound(), publishes MarketOpenEvent
- On timer expiry: sets TradingState.NextConfig and transitions to TradingState automatically
- Uses same testable AdvanceTime pattern as TradingState (from Story 4.1 review)
- Debug.Assert guards NextConfig to prevent silent failures
- Static accessors (ActiveTimeRemaining, IsActive) for UI one-way reads
- MarketOpenUI is a full-screen overlay with fade-in animation (CanvasGroup alpha)
- Displays: Act/Round header, stock tickers with prices and tier indicators, news headline in quotes, profit target in large yellow text, countdown
- Subscribes to MarketOpenEvent to populate data, hides on RoundStartedEvent
- NewsHeadlineData provides 4 headline pools (Bullish, Bearish, Volatile, Neutral) with 5 headlines each
- GetHeadline(stocks, random) overload detects volatile conditions (mixed bull/bear) to select VolatileHeadlines
- MarketOpenEvent includes RoundNumber, Act, StockIds, TickerSymbols, StartingPrices, TierNames, ProfitTarget, Headline
- GameConfig.MarketOpenDurationSeconds = 7f added
- UISetup.ExecuteMarketOpenUI() generates overlay at sortingOrder 100 (above all game UI)

### Change Log

- 2026-02-11: Story 4.2 implemented — MarketOpenState, MarketOpenUI, NewsHeadlineData, MarketOpenEvent, GameConfig extension, UISetup extension
- 2026-02-11: Code review fixes — AC2 stock display (tickers/prices/tiers), volatile headline selection, no-op test replaced, dead code removed

## Senior Developer Review (AI)

**Review Date:** 2026-02-11
**Review Outcome:** Changes Requested (5 HIGH/MEDIUM issues, 2 LOW)
**Reviewer Model:** Claude Opus 4.6

### Action Items

- [x] [HIGH] AC2 not implemented: BuildStockList showed only count, not ticker symbols/prices/tiers [MarketOpenUI.cs:108-117]
- [x] [HIGH] VolatileHeadlines pool never used — dead code, GetHeadline had no volatile path [NewsHeadlineData.cs:47-63]
- [x] [MED] No-op test AdvanceTime_WhenExpired_SetsTradingStateNextConfig asserted Assert.IsTrue(true) [MarketOpenStateTests.cs:157-167]
- [x] [MED] BuildStockListDetailed was dead code — never called from any production path [MarketOpenUI.cs:122-135]
- [x] [MED] Tier indicators not shown — Task 2 required tier display but neither BuildStockList method showed it [MarketOpenUI.cs]
- [ ] [LOW] No test for MarketOpenState.Enter with null PriceGenerator [MarketOpenState.cs:43-66]
- [ ] [LOW] TODO comment left in production code suggesting incomplete work [MarketOpenUI.cs:113-116]

### Resolution Summary

All 5 HIGH and MEDIUM issues fixed automatically:
- MarketOpenEvent now carries TickerSymbols[], StartingPrices[], TierNames[] arrays
- MarketOpenState populates stock detail arrays from PriceGenerator.ActiveStocks
- MarketOpenUI.BuildStockList rewritten to display "ACME  $150.00  [MidValue]" format
- Dead BuildStockListDetailed removed; TODO comment removed
- NewsHeadlineData.GetHeadline(stocks, random) overload added with volatile detection
- StockInstance.Tier property added for tier enum access
- No-op test replaced with real assertion verifying event detail arrays
- All affected tests updated (MarketOpenUITests, MarketOpenStateTests, GameEventsTests, NewsHeadlineDataTests)

### File List

- `Assets/Scripts/Runtime/Core/GameStates/MarketOpenState.cs` (new)
- `Assets/Scripts/Runtime/UI/MarketOpenUI.cs` (new)
- `Assets/Scripts/Setup/Data/NewsHeadlineData.cs` (new)
- `Assets/Scripts/Setup/Data/GameConfig.cs` (modified — added MarketOpenDurationSeconds)
- `Assets/Scripts/Runtime/Core/GameEvents.cs` (modified — added MarketOpenEvent with stock detail fields)
- `Assets/Scripts/Setup/UISetup.cs` (modified — added ExecuteMarketOpenUI method)
- `Assets/Scripts/Runtime/PriceEngine/StockInstance.cs` (modified — added Tier property)
- `Assets/Tests/Runtime/Core/GameStates/MarketOpenStateTests.cs` (new — 13 tests)
- `Assets/Tests/Runtime/PriceEngine/NewsHeadlineDataTests.cs` (new — 14 tests)
- `Assets/Tests/Runtime/UI/MarketOpenUITests.cs` (new — 6 tests)
- `Assets/Tests/Runtime/Core/GameEventsTests.cs` (modified — added 2 MarketOpenEvent tests)
