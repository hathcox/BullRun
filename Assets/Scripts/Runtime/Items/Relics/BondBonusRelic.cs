/// <summary>
/// Story 17.5: Bond Bonus — on acquire, adds 10 bonds and 10 synthetic BondRecords.
/// On sell, removes 10 bonds and 10 BondRecords LIFO. BondsOwned never goes below 0.
/// </summary>
public class BondBonusRelic : RelicBase
{
    public override string Id => "relic_bond_bonus";

    public override void OnAcquired(RunContext ctx)
    {
        ctx.BondsOwned += 10;
        for (int i = 0; i < 10; i++)
        {
            ctx.BondPurchaseHistory.Add(new BondRecord(ctx.CurrentRound, 0)); // Synthetic bond, cost=0
        }
        EventBus.Publish(new RelicActivatedEvent { RelicId = Id });
    }

    public override void OnSellSelf(RunContext ctx)
    {
        ctx.BondsOwned = System.Math.Max(0, ctx.BondsOwned - 10);

        int toRemove = System.Math.Min(10, ctx.BondPurchaseHistory.Count);
        for (int i = 0; i < toRemove; i++)
        {
            ctx.BondPurchaseHistory.RemoveAt(ctx.BondPurchaseHistory.Count - 1);
        }
        EventBus.Publish(new RelicActivatedEvent { RelicId = Id });
    }
}
