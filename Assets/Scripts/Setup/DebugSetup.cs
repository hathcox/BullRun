#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

/// <summary>
/// Setup class that creates the DebugManager GameObject in the scene.
/// SceneComposition phase — generates debug tools during F5 rebuild.
/// </summary>
// [SetupClass(SetupPhase.SceneComposition, 90)] // Uncomment when SetupPipeline infrastructure exists
public static class DebugSetup
{
    public static void Execute()
    {
        var debugGo = new GameObject("DebugManager");
        debugGo.AddComponent<DebugManager>();

        Debug.Log("[Setup] DebugManager created");
    }
}
#endif
