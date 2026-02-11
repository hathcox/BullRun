using UnityEngine;

/// <summary>
/// Placeholder MetaHub state. Entry point for starting a run.
/// Will be fleshed out in Epic 9 (Meta-Progression).
/// </summary>
public class MetaHubState : IGameState
{
    public void Enter(RunContext ctx)
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[MetaHubState] Enter: MetaHub entered (placeholder)");
        #endif
    }

    public void Update(RunContext ctx) { }

    public void Exit(RunContext ctx)
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[MetaHubState] Exit");
        #endif
    }
}
