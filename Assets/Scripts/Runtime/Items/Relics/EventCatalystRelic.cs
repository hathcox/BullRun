using UnityEngine;

/// <summary>
/// Event Catalyst: When reputation is gained, each point earned has a 1% chance to trigger a random market event.
/// Only fires during the trading phase (TradingState.IsActive).
/// Story 17.7 AC 1-5, 11, 12.
/// </summary>
public class EventCatalystRelic : RelicBase
{
    public override string Id => "relic_event_catalyst";

    /// <summary>
    /// Random value provider for testability. Defaults to UnityEngine.Random.value.
    /// Returns float in [0, 1). Override in tests for deterministic behavior.
    /// </summary>
    public System.Func<float> RandomProvider = () => Random.value;

    public override void OnReputationChanged(RunContext ctx, int oldRep, int newRep)
    {
        int repGained = newRep - oldRep;
        if (repGained <= 0) return;

        if (!TradingState.IsActive) return;
        if (ctx.EventScheduler == null) return;

        int hitCount = 0;
        for (int i = 0; i < repGained; i++)
        {
            if (RandomProvider() < 0.01f)
            {
                ctx.EventScheduler.ForceFireRandomEvent();
                hitCount++;
            }
        }

        if (hitCount > 0)
        {
            EventBus.Publish(new RelicActivatedEvent { RelicId = Id });

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[EventCatalystRelic] Rep +{repGained} triggered {hitCount} random event(s)");
            #endif
        }
    }
}
