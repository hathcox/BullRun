# Story 6.1: Act-Round Progression

Status: done

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

- [x] Task 1: Formalize act/round mapping in GameConfig (AC: 1, 2, 3, 4, 5)
  - [x] Define `ActConfig` struct: ActNumber, DisplayName, Tier, StartRound, EndRound
  - [x] Populate 4 acts with tier mappings per GDD
  - [x] Method: `GetActForRound(int round)` — in RunContext (static), uses GameConfig.RoundsPerAct
  - [x] Method: `GetTierForRound(int round)` — in RunContext (static), convenience wrapper
  - [x] Constants: `TotalRounds = 8`, `RoundsPerAct = 2`, `TotalActs = 4`
  - [x] File: `Scripts/Setup/Data/GameConfig.cs` (extend)
- [x] Task 2: Integrate act awareness into RunContext (AC: 6)
  - [x] RunContext.AdvanceRound() uses GetActForRound() to update current act
  - [x] Property: `CurrentAct` (int), `CurrentTier` (StockTier), `CurrentActConfig` (ActConfig)
  - [x] `AdvanceRound()` returns bool when act changes (serves as IsNewAct signal)
  - [x] File: `Scripts/Runtime/Core/RunContext.cs` (extend)
- [x] Task 3: Wire act/tier into MarketOpenState stock initialization (AC: 2, 3, 4, 5)
  - [x] MarketOpenState passes `ctx.CurrentAct` to PriceGenerator.InitializeRound()
  - [x] PriceGenerator uses RunContext.GetTierForAct(act) to select stock pool
  - [ ] EventScheduler.ScheduleRound() — deferred to Epic 5 (Event System)
  - [x] File: `Scripts/Runtime/Core/GameStates/MarketOpenState.cs`
- [x] Task 4: Wire act/tier into margin call targets (AC: 1)
  - [x] MarginCallTargets.GetTarget() indexed by round — 8 escalating targets verified
  - [x] File: `Scripts/Setup/Data/MarginCallTargets.cs` (verified)
- [x] Task 5: Add round/act to event payloads (AC: 6)
  - [x] `MarketOpenEvent` includes Act, TierNames, ProfitTarget
  - [x] `RoundStartedEvent` includes Act, TierDisplayName, MarginCallTarget
  - [x] `ActTransitionEvent` includes NewAct, PreviousAct, TierDisplayName
  - [x] File: `Scripts/Runtime/Core/GameEvents.cs`

### Review Follow-ups (Code Review #1)
- [x] [AI-Review][HIGH] CurrentActConfig bounds check — added clamping for act > TotalActs
- [x] [AI-Review][MEDIUM] Added TierDisplayName to RoundStartedEvent and populated in TradingState
- [x] [AI-Review][MEDIUM] Added GetTierForRound() convenience method
- [x] [AI-Review][MEDIUM] Added full 8-round progression integration test

### Review Follow-ups (Code Review #2 — 2026-02-12)
- [x] [AI-Review][HIGH] CurrentAct/CurrentRound public setters → internal set (encapsulation fix)
- [x] [AI-Review][MEDIUM] ResetForNewRun() undocumented — added 4 tests for coverage
- [x] [AI-Review][MEDIUM] GetActForRound() round <= 0 — added clamp to 1 + edge case tests
- [x] [AI-Review][MEDIUM] GetTierForAct() act <= 0 — added clamp to 1 (consistent with GetActForRound)
- [ ] [AI-Review][MEDIUM] Cross-story contamination — 6.1 and 6.2 changes interleaved (process, no code fix)
- [x] [AI-Review][MEDIUM] File List corrected — removed phantom files, added missing test file

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
- Modifies: `Scripts/Runtime/Core/GameEvents.cs`
- Modifies: `Scripts/Runtime/Core/GameStates/TradingState.cs`
- Verifies: `Scripts/Setup/Data/MarginCallTargets.cs`

### References

- [Source: bull-run-gdd-mvp.md#2.1] — Run structure: 4 Acts, 2 rounds/act, 8 total, act-tier table
- [Source: bull-run-gdd-mvp.md#2.1] — "A full run takes approximately 7-10 minutes"
- [Source: game-architecture.md#Game State Machine] — RunContext carries current act, round

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Debug Log References

- `[MarketOpenState] Enter: Act {act}, Round {round}, Preview {duration}s`
- `[TradingState] Enter: Round {round}, Duration {duration}s`

### Completion Notes List

- Most implementation was completed in Stories 4.5 and 1.5 (ActConfig, GameConfig acts, AdvanceRound, GetActForRound, GetTierForAct)
- Story 6.1 added: CurrentTier, CurrentActConfig convenience properties on RunContext
- Code review #1 added: GetTierForRound(), CurrentActConfig bounds clamping, TierDisplayName on RoundStartedEvent
- EventScheduler wiring deferred to Epic 5 (not yet built)
- AC7 (7-10 min runtime) verified via math: ~69s/round × 8 rounds ≈ 9.2 minutes
- Code review #2 (2026-02-12): CurrentAct/CurrentRound → internal set, GetActForRound/GetTierForAct input clamping, ResetForNewRun tests added, File List corrected

### File List

- Assets/Scripts/Setup/Data/GameConfig.cs
- Assets/Scripts/Runtime/Core/RunContext.cs
- Assets/Scripts/Runtime/Core/GameStates/TradingState.cs
- Assets/Scripts/Runtime/Core/GameEvents.cs
- Assets/Scripts/Setup/Data/MarginCallTargets.cs
- Assets/Tests/Runtime/Core/RunContextTests.cs
- Assets/Tests/Runtime/PriceEngine/GameConfigTests.cs
- Assets/Tests/Runtime/Trading/MarginCallTargetsTests.cs
