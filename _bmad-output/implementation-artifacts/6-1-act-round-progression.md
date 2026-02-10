# Story 6.1: Act-Round Progression

Status: ready-for-dev

## Story

As a player,
I want to progress through 4 acts with 2 rounds each (8 total),
so that each run has a clear escalation arc from penny stocks to blue chips.

## Acceptance Criteria

1. A full run consists of 4 Acts with 2 Rounds per Act, totaling 8 Rounds
2. Act 1 (Rounds 1-2): Penny Stocks tier
3. Act 2 (Rounds 3-4): Low-Value Stocks tier
4. Act 3 (Rounds 5-6): Mid-Value Stocks tier
5. Act 4 (Rounds 7-8): Blue Chips tier
6. RunContext correctly tracks current act, round, and tier throughout the run
7. Full run takes approximately 7-10 minutes

## Tasks / Subtasks

- [ ] Task 1: Formalize act/round mapping in GameConfig (AC: 1, 2, 3, 4, 5)
  - [ ] Define `ActConfig` struct: ActNumber, DisplayName, Tier, StartRound, EndRound
  - [ ] Populate 4 acts with tier mappings per GDD
  - [ ] Method: `GetActForRound(int round)` — returns ActConfig
  - [ ] Method: `GetTierForRound(int round)` — returns StockTier
  - [ ] Constants: `TotalRounds = 8`, `RoundsPerAct = 2`, `TotalActs = 4`
  - [ ] File: `Scripts/Setup/Data/GameConfig.cs` (extend)
- [ ] Task 2: Integrate act awareness into RunContext (AC: 6)
  - [ ] RunContext.AdvanceRound() uses GameConfig.GetActForRound() to update current act and tier
  - [ ] Property: `CurrentAct` (int), `CurrentTier` (StockTier), `CurrentActConfig` (ActConfig)
  - [ ] Property: `IsNewAct` — true when round is the first round of a new act
  - [ ] File: `Scripts/Runtime/Core/RunContext.cs` (extend)
- [ ] Task 3: Wire act/tier into MarketOpenState stock initialization (AC: 2, 3, 4, 5)
  - [ ] MarketOpenState reads `RunContext.CurrentTier` to determine which stock pool to draw from
  - [ ] Passes tier to PriceGenerator.InitializeRound()
  - [ ] Passes tier to EventScheduler.ScheduleRound() for tier-filtered events
  - [ ] File: `Scripts/Runtime/Core/GameStates/MarketOpenState.cs` (extend)
- [ ] Task 4: Wire act/tier into margin call targets (AC: 1)
  - [ ] MarginCallTargets.GetTarget() already indexed by round (Story 3.2)
  - [ ] Verify targets escalate correctly across acts per GDD table
  - [ ] File: `Scripts/Setup/Data/MarginCallTargets.cs` (verify)
- [ ] Task 5: Add round/act to event payloads (AC: 6)
  - [ ] Ensure `RoundStartedEvent`, `MarketOpenEvent` include act and tier info
  - [ ] UI components can display "Act 2 — Round 3" from event data
  - [ ] File: `Scripts/Runtime/Core/GameEvents.cs` (extend if needed)

## Dev Notes

### Architecture Compliance

- **RunContext is the authority** for current act/round/tier — all other systems read from it
- **Data in GameConfig** — act structure is config, not logic
- **No new systems** — this story formalizes and wires the act/round mapping that Story 4.5 stubbed

### Act Structure Reference (GDD Section 2.1)

| Act | Rounds | Tier | Volatility | New Mechanics |
|-----|--------|------|------------|---------------|
| Act 1 | 1-2 | Penny Stocks | Low-Med | Core buy/sell/short |
| Act 2 | 3-4 | Low-Value | Medium | Sector events unlock |
| Act 3 | 5-6 | Mid-Value | Med-High | Complex instruments |
| Act 4 | 7-8 | Blue Chips | High | Market manipulation events |

### Relationship to Story 4.5

Story 4.5 (Round Transition) created the `AdvanceRound()` and `GetCurrentAct()` stubs. This story formalizes them with proper data-driven config and ensures the tier is correctly propagated to PriceGenerator and EventScheduler. If Story 4.5 already implemented this fully, this story validates and extends.

### Project Structure Notes

- Modifies: `Scripts/Setup/Data/GameConfig.cs`
- Modifies: `Scripts/Runtime/Core/RunContext.cs`
- Modifies: `Scripts/Runtime/Core/GameStates/MarketOpenState.cs`
- Modifies: `Scripts/Runtime/Core/GameEvents.cs` (if needed)
- Verifies: `Scripts/Setup/Data/MarginCallTargets.cs`

### References

- [Source: bull-run-gdd-mvp.md#2.1] — Run structure: 4 Acts, 2 rounds/act, 8 total, act-tier table
- [Source: bull-run-gdd-mvp.md#2.1] — "A full run takes approximately 7-10 minutes"
- [Source: game-architecture.md#Game State Machine] — RunContext carries current act, round

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
