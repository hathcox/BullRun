using UnityEngine;

/// <summary>
/// Relic Expansion: Selling this relic permanently grants +1 relic slot. Refund is 0 rep (not standard 50%).
/// While held, occupies a slot but has no passive effect.
/// Story 17.7 AC 6, 7, 9, 10, 11, 12.
/// </summary>
public class RelicExpansionRelic : RelicBase
{
    public override string Id => "relic_relic_expansion";

    public override void OnSellSelf(RunContext ctx)
    {
        ctx.BonusRelicSlots++;
        EventBus.Publish(new RelicActivatedEvent { RelicId = Id });

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[RelicExpansionRelic] Sold — bonus relic slot granted (total bonus: {ctx.BonusRelicSlots})");
        #endif
    }

    public override int? GetSellValue(RunContext ctx)
    {
        return 0;
    }
}
