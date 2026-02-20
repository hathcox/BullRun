/// <summary>
/// Quick Draw: Buy has 0s cooldown, sell has 2x cooldown.
/// Uses passive query pattern — RelicManager.GetEffectiveCooldown() checks for this relic.
/// Story 17.3 AC 2.
/// </summary>
public class QuickDrawRelic : RelicBase
{
    public override string Id => "relic_quick_draw";
}
