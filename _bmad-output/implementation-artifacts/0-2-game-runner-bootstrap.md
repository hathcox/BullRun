# Story 0.2: Game Runner Bootstrap

Status: done

## Story

As a developer,
I want a GameRunner MonoBehaviour that boots the game loop when the scene starts,
so that pressing Play in Unity starts the game state machine and drives it every frame.

## Acceptance Criteria

1. A `GameRunner` MonoBehaviour exists and is created during F5 rebuild by a setup class
2. On `Awake()`: creates RunContext, PriceGenerator, TradeExecutor, GameStateMachine
3. On `Start()`: sets MarketOpenState.NextConfig and transitions to MarketOpenState (skipping MetaHub placeholder)
4. On `Update()`: calls GameStateMachine.Update() every frame to drive state transitions
5. The full game loop runs: MarketOpen → Trading → MarketClose → MarginCall → Shop → (loop)
6. Console logs confirm each state transition

## Tasks / Subtasks

- [x] Task 1: Create GameRunner MonoBehaviour (AC: 1, 2, 3, 4)
  - [x] File: `Scripts/Runtime/Core/GameRunner.cs` (new)
- [x] Task 2: Create GameRunnerSetup to add GameRunner to scene during F5 (AC: 1)
  - [x] File: `Scripts/Setup/GameRunnerSetup.cs` (new)
  - [x] [SetupClass(SetupPhase.SceneComposition, 10)] — runs early, after SceneSetup

## Dev Notes

- GameRunner is the only "god object" — it creates the core systems and wires the first state transition
- All subsequent transitions are handled by the states themselves via static NextConfig pattern
- MetaHub is a placeholder, so GameRunner skips directly to MarketOpenState
- PriceGenerator and TradeExecutor are plain C# classes, not MonoBehaviours
