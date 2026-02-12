using UnityEngine;

/// <summary>
/// Creates the GameRunner GameObject during F5 rebuild.
/// Runs early in SceneComposition so the game loop bootstrapper is in the scene.
/// </summary>
[SetupClass(SetupPhase.SceneComposition, 10)]
public static class GameRunnerSetup
{
    public static void Execute()
    {
        var go = new GameObject("GameRunner");
        go.AddComponent<GameRunner>();

        Debug.Log("[Setup] GameRunner created");
    }
}
