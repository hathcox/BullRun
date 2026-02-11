using UnityEngine;

/// <summary>
/// Stub state for MarketClose phase. Will be fully implemented in a later story.
/// </summary>
public class MarketCloseState : IGameState
{
    public void Enter(RunContext ctx)
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[MarketCloseState] Enter: Round {ctx.CurrentRound}");
        #endif
    }

    public void Update(RunContext ctx) { }

    public void Exit(RunContext ctx) { }
}
