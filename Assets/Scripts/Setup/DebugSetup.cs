#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

/// <summary>
/// Setup class that creates the DebugManager GameObject in the scene.
/// SceneComposition phase — generates debug tools during F5 rebuild.
/// </summary>
// Runtime-only: called by GameRunner.Start(), not during F5 rebuild.
public static class DebugSetup
{
    public static void Execute(PriceGenerator priceGenerator = null, ChartRenderer chartRenderer = null,
        RunContext runContext = null, GameStateMachine stateMachine = null, TradeExecutor tradeExecutor = null,
        EventScheduler eventScheduler = null)
    {
        var debugGo = new GameObject("DebugManager");
        var mgr = debugGo.AddComponent<DebugManager>();

        if (priceGenerator != null)
            mgr.SetPriceGenerator(priceGenerator);
        if (chartRenderer != null)
            mgr.SetChartRenderer(chartRenderer);
        if (runContext != null && stateMachine != null)
            mgr.SetGameContext(runContext, stateMachine, tradeExecutor, eventScheduler);

        Debug.Log("[Setup] DebugManager created");
    }
}
#endif
