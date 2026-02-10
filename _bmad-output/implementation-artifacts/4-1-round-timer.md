# Story 4.1: Round Timer

Status: ready-for-dev

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

- [ ] Task 1: Create GameStateMachine (AC: 5, 6)
  - [ ] Flat state machine per architecture: holds current `IGameState`, manages transitions
  - [ ] Method: `TransitionTo<T>()` — calls Exit on current, creates new state, calls Enter
  - [ ] Holds reference to `RunContext` passed to all states
  - [ ] Logs transitions: `[GameState] Transition: → TradingState`
  - [ ] File: `Scripts/Runtime/Core/GameStateMachine.cs`
- [ ] Task 2: Create IGameState interface (AC: 6)
  - [ ] Methods: `Enter(RunContext ctx)`, `Update(RunContext ctx)`, `Exit(RunContext ctx)`
  - [ ] File: `Scripts/Runtime/Core/IGameState.cs`
- [ ] Task 3: Create TradingState with timer (AC: 1, 5, 6)
  - [ ] On Enter: set `_timeRemaining = GameConfig.RoundDurationSeconds` (60f), publish `RoundStartedEvent`
  - [ ] On Update: `_timeRemaining -= Time.deltaTime`, trigger PriceGenerator updates
  - [ ] When `_timeRemaining <= 0`: transition to MarketCloseState
  - [ ] Expose `TimeRemaining` and `TimeElapsed` for UI reading
  - [ ] File: `Scripts/Runtime/Core/GameStates/TradingState.cs`
- [ ] Task 4: Create timer UI display (AC: 1, 2, 3, 4)
  - [ ] Numeric countdown: "0:45" format (minutes:seconds)
  - [ ] Progress bar: depletes left-to-right as time decreases
  - [ ] Color transitions: white/green (normal) → yellow (at 15s) → red (at 5s)
  - [ ] Pulse animation at 5s: timer text scales up/down rhythmically
  - [ ] Integrate with existing chart time bar from Story 3.1 or add separate timer display
  - [ ] File: `Scripts/Runtime/UI/RoundTimerUI.cs` (new)
- [ ] Task 5: Define state-related GameEvents (AC: 5)
  - [ ] `RoundStartedEvent`: RoundNumber, Act, MarginCallTarget, TimeLimit
  - [ ] `TradingPhaseEndedEvent`: RoundNumber, TimeExpired (bool)
  - [ ] Add to `Scripts/Runtime/Core/GameEvents.cs`
- [ ] Task 6: Add timer UI to UISetup (AC: 2)
  - [ ] Generate RoundTimerUI elements — positioned near chart or top bar
  - [ ] File: `Scripts/Setup/UISetup.cs` (extend)

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

### Debug Log References

### Completion Notes List

### File List
