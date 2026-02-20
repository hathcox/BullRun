using UnityEngine;

/// <summary>
/// Free Intel: one insider tip is free per shop visit.
/// On shop open, sets RunContext.FreeIntelThisVisit = true.
/// First tip purchase checks this flag and costs 0 if true.
/// Story 17.6 AC 5, 10.
/// </summary>
public class FreeIntelRelic : RelicBase
{
    public override string Id => "relic_free_intel";

    public override void OnShopOpen(RunContext ctx)
    {
        ctx.FreeIntelThisVisit = true;

        EventBus.Publish(new RelicActivatedEvent { RelicId = Id });

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[FreeIntelRelic] Free intel flag set for this shop visit");
        #endif
    }
}
