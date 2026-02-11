using UnityEngine;

/// <summary>
/// Flat state machine per architecture. Holds current IGameState, manages transitions.
/// States read and modify RunContext passed to Enter/Update/Exit calls.
/// </summary>
public class GameStateMachine
{
    private IGameState _current;
    private readonly RunContext _ctx;

    public IGameState CurrentState => _current;

    public GameStateMachine(RunContext ctx)
    {
        _ctx = ctx;
    }

    /// <summary>
    /// Transitions to a new state. Calls Exit on current, creates new state, calls Enter.
    /// </summary>
    public void TransitionTo<T>() where T : IGameState, new()
    {
        _current?.Exit(_ctx);
        _current = new T();
        _current.Enter(_ctx);

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[GameState] Transition: → {typeof(T).Name}");
        #endif
    }

    /// <summary>
    /// Calls Update on the current state. Should be called every frame.
    /// </summary>
    public void Update()
    {
        _current?.Update(_ctx);
    }
}
