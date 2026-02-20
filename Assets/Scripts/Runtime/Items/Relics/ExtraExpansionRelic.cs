using UnityEngine;

/// <summary>
/// Extra Expansion: one extra expansion offered per shop visit.
/// On shop open, increments RunContext.BonusExpansionSlots by 1.
/// ShopState uses this when generating expansion offerings.
/// Story 17.6 AC 6, 11.
/// </summary>
public class ExtraExpansionRelic : RelicBase
{
    public override string Id => "relic_extra_expansion";

    public override void OnShopOpen(RunContext ctx)
    {
        ctx.BonusExpansionSlots++;

        EventBus.Publish(new RelicActivatedEvent { RelicId = Id });

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[ExtraExpansionRelic] Bonus expansion slot added for this shop visit");
        #endif
    }
}
