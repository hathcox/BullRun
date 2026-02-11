# Story 4.1: Round Timer

Status: done

## Story

As a player,
I want a 60-second countdown timer during trading,
so that I feel time pressure to make decisions.

## Acceptance Criteria

1. A visible countdown timer starts at 60 seconds when the Trading phase begins
2. Timer counts down in real-time to 0
3. Progress bar visualization fills or depletes alongside the numeric countdown
4. Visual urgency cues as time runs low: color change at 15s, pulse animation at 5s
5. When timer hits 0, the Trading phase ends and transitions to MarketClose
6. Timer is driven by the game state machine — only runs during Trading state

## Tasks / Subtasks

- [x] Task 1: Create GameStateMachine (AC: 5, 6)
  - [x] Flat state machine per architecture: holds current `IGameState`, manages transitions
  - [x] Method: `TransitionTo<T>()` — calls Exit on current, creates new state, calls Enter
  - [x] Holds reference to `RunContext` passed to all states
  - [x] Logs transitions: `[GameState] Transition: → TradingState`
  - [x] File: `Scripts/Runtime/Core/GameStateMachine.cs`
- [x] Task 2: Create IGameState interface (AC: 6)
  - [x] Methods: `Enter(RunContext ctx)`, `Update(RunContext ctx)`, `Exit(RunContext ctx)`
  - [x] File: `Scripts/Runtime/Core/IGameState.cs`
- [x] Task 3: Create TradingState with timer (AC: 1, 5, 6)
  - [x] On Enter: set `_timeRemaining = GameConfig.RoundDurationSeconds` (60f), publish `RoundStartedEvent`
  - [x] On Update: `_timeRemaining -= Time.deltaTime`, trigger PriceGenerator updates
  - [x] When `_timeRemaining <= 0`: transition to MarketCloseState
  - [x] Expose `TimeRemaining` and `TimeElapsed` for UI reading
  - [x] File: `Scripts/Runtime/Core/GameStates/TradingState.cs`
- [x] Task 4: Create timer UI display (AC: 1, 2, 3, 4)
  - [x] Numeric countdown: "0:45" format (minutes:seconds)
  - [x] Progress bar: depletes left-to-right as time decreases
  - [x] Color transitions: white/green (normal) → yellow (at 15s) → red (at 5s)
  - [x] Pulse animation at 5s: timer text scales up/down rhythmically
  - [x] Integrate with existing chart time bar from Story 3.1 or add separate timer display
  - [x] File: `Scripts/Runtime/UI/RoundTimerUI.cs` (new)
- [x] Task 5: Define state-related GameEvents (AC: 5)
  - [x] `RoundStartedEvent`: RoundNumber, Act, MarginCallTarget, TimeLimit
  - [x] `TradingPhaseEndedEvent`: RoundNumber, TimeExpired (bool)
  - [x] Add to `Scripts/Runtime/Core/GameEvents.cs`
- [x] Task 6: Add timer UI to UISetup (AC: 2)
  - [x] Generate RoundTimerUI elements — positioned near chart or top bar
  - [x] File: `Scripts/Setup/UISetup.cs` (extend)

## Dev Notes

### Architecture Compliance

- **Game State Machine:** Flat state machine per architecture — `GameStateMachine` + `IGameState` interface
- **State flow:** `MetaHub → MarketOpen → Trading → MarketClose → Shop → (loop or) RunSummary`
- **Location:** `Scripts/Runtime/Core/GameStateMachine.cs`, states in `Scripts/Runtime/Core/GameStates/`
- **RunContext:** Passed to all state Enter/Update/Exit calls. States read and modify RunContext.
- **Logging:** `[GameState] Transition: → TradingState`

### Architecture State Machine Pattern

```csharp
public class GameStateMachine
{
    private IGameState _current;
    private readonly RunContext _ctx;

    public void TransitionTo<T>() where T : IGameState, new()
    {
        _current?.Exit(_ctx);
        _current = new T();
        _current.Enter(_ctx);
        Debug.Log($"[GameState] Transition: → {typeof(T).Name}");
    }

    public void Update() => _current?.Update(_ctx);
}
```

### TradingState is the Heart of the Game

This is where the core gameplay loop runs. TradingState.Update() each frame:
1. Decrements timer
2. Drives PriceGenerator to update all stock prices
3. Checks for timer expiry → transition to MarketClose

The PriceGenerator does NOT run itself — TradingState calls it each frame. This keeps the game state machine as the single authority on when gameplay systems run.

### Timer Urgency Design (GDD Section 6.2)

The timer should create escalating tension:
- **60s–16s:** Normal pace, white/green text
- **15s–6s:** Urgency zone, yellow text, slightly faster tick sound
- **5s–0s:** Critical zone, red pulsing text, heartbeat-like pulse

### Project Structure Notes

- Creates: `Scripts/Runtime/Core/GameStateMachine.cs`
- Creates: `Scripts/Runtime/Core/IGameState.cs`
- Creates: `Scripts/Runtime/Core/GameStates/TradingState.cs`
- Creates: `Scripts/Runtime/UI/RoundTimerUI.cs`
- Modifies: `Scripts/Runtime/Core/GameEvents.cs`
- Modifies: `Scripts/Setup/UISetup.cs`

### References

- [Source: game-architecture.md#Game State Machine] — Flat state machine with RunContext, explicit transitions
- [Source: game-architecture.md#Game State Transitions] — IGameState interface and TransitionTo pattern
- [Source: game-architecture.md#Project Structure] — Core/GameStateMachine.cs, Core/GameStates/
- [Source: bull-run-gdd-mvp.md#2.2] — "Phase 2: Trading Phase (45–75 seconds)" — default 60s
- [Source: bull-run-gdd-mvp.md#6.2] — Juice and feel for urgency cues
- [Source: bull-run-gdd-mvp.md#11.1] — "Round Duration: 60 seconds. Test 45s and 75s variants"

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Debug Log References

- `[GameState] Transition: → {StateName}` — logged on every state transition
- `[TradingState] Enter: Round X, Duration Ys` — logged on trading phase start
- `[TradingState] Exit: TimeRemaining=Xs` — logged on trading phase end
- `[Setup] RoundTimerUI created: centered below top bar` — logged during F5 setup

### Completion Notes List

- Implemented flat GameStateMachine with `TransitionTo<T>()` pattern per architecture spec
- Created IGameState interface with Enter/Update/Exit(RunContext) methods
- TradingState manages 60s countdown, drives PriceGenerator each frame, transitions to MarketCloseState on expiry
- TradingState uses static NextConfig pattern to receive GameStateMachine and PriceGenerator references before transition
- Created stub MarketCloseState for transition target (to be fully implemented in later story)
- RoundTimerUI displays countdown in "M:SS" format with progress bar
- Timer urgency cues: green (normal) → yellow (15s) → red (5s) with pulse animation at 5s
- Added RoundStartedEvent and TradingPhaseEndedEvent to GameEvents.cs
- RoundStartedEvent includes MarginCallTarget field per story spec
- UISetup.ExecuteRoundTimer() generates timer UI centered below top bar

### Senior Developer Review (AI)

**Review Date:** 2026-02-11
**Review Outcome:** Changes Requested (auto-fixed)
**Reviewer Model:** Claude Opus 4.6

#### Action Items

- [x] [HIGH] RoundStartedEvent missing MarginCallTarget field — added field and populated in TradingState
- [x] [HIGH] TradingState static NextConfig null = silent failure — added Debug.Assert
- [x] [HIGH] Dual timer drift: RoundTimerUI and TradingState running independent timers — RoundTimerUI now reads from TradingState statics
- [x] [MED] Price updates after timer expiry — reordered to check expiry first with early return
- [x] [MED] No TradingState tests for Update/timer behavior — added AdvanceTime method + 8 new tests
- [x] [MED] Test pollution: AnotherTestState static LastInstance — removed unused static field

### Change Log

- 2026-02-11: Story 4.1 implemented — GameStateMachine, IGameState, TradingState, RoundTimerUI, state events, UISetup extension
- 2026-02-11: Code review fixes — 6 issues resolved (3 HIGH, 3 MED): added MarginCallTarget, fixed dual timer, added Debug.Assert, reordered expiry check, added 8 AdvanceTime tests, cleaned test statics

### File List

- `Assets/Scripts/Runtime/Core/GameStateMachine.cs` (new)
- `Assets/Scripts/Runtime/Core/IGameState.cs` (new)
- `Assets/Scripts/Runtime/Core/GameStates/TradingState.cs` (new)
- `Assets/Scripts/Runtime/Core/GameStates/MarketCloseState.cs` (new — stub)
- `Assets/Scripts/Runtime/UI/RoundTimerUI.cs` (new)
- `Assets/Scripts/Runtime/Core/GameEvents.cs` (modified — added RoundStartedEvent with MarginCallTarget, TradingPhaseEndedEvent)
- `Assets/Scripts/Setup/UISetup.cs` (modified — added ExecuteRoundTimer method)
- `Assets/Tests/Runtime/Core/GameStateMachineTests.cs` (new — 7 tests)
- `Assets/Tests/Runtime/Core/GameStates/TradingStateTests.cs` (new — 15 tests)
- `Assets/Tests/Runtime/Core/GameEventsTests.cs` (modified — added 4 event tests)
- `Assets/Tests/Runtime/UI/RoundTimerUITests.cs` (new — 13 tests)
