using System;
using System.Collections.Generic;

/// <summary>
/// Static factory for creating IRelic instances by relic ID.
/// Maintains a registry of constructor functions.
/// Story 17.1: Relic framework with dynamic registration from RelicPool.
/// Story 17.2: Pool expanded to 23 relics (auto-registered via RegisterDefaults).
/// Stories 17.3-17.7 will replace StubRelic constructors with real relic classes.
/// </summary>
public static class RelicFactory
{
    private static readonly Dictionary<string, Func<IRelic>> _registry = new Dictionary<string, Func<IRelic>>();

    static RelicFactory()
    {
        RegisterDefaults();
    }

    public static void Register(string id, Func<IRelic> constructor)
    {
        _registry[id] = constructor;
    }

    public static IRelic Create(string relicId)
    {
        if (string.IsNullOrEmpty(relicId)) return null;
        if (_registry.TryGetValue(relicId, out var constructor))
            return constructor();
        return null;
    }

    public static void ClearRegistry()
    {
        _registry.Clear();
    }

    /// <summary>
    /// Re-registers all default relic constructors. Call after ClearRegistry() in tests.
    /// </summary>
    public static void ResetRegistry()
    {
        _registry.Clear();
        RegisterDefaults();
    }

    private static void RegisterDefaults()
    {
        // Story 17.3: Register real relic constructors for trade modification relics
        _registry["relic_double_dealer"] = () => new DoubleDealerRelic();
        _registry["relic_quick_draw"] = () => new QuickDrawRelic();
        _registry["relic_short_multiplier"] = () => new ShortMultiplierRelic();
        _registry["relic_skimmer"] = () => new SkimmerRelic();
        _registry["relic_short_profiteer"] = () => new ShortProfiteerRelic();

        // Story 17.4: Register real relic constructors for event interaction relics
        _registry["relic_event_trigger"] = () => new CatalystTraderRelic();
        _registry["relic_event_storm"] = () => new EventStormRelic();
        _registry["relic_loss_liquidator"] = () => new LossLiquidatorRelic();
        _registry["relic_profit_refresh"] = () => new ProfitRefreshRelic();
        _registry["relic_bull_believer"] = () => new BullBelieverRelic();

        // Story 17.5: Register real relic constructors for economy/reputation relics
        _registry["relic_rep_doubler"] = () => new RepDoublerRelic();
        _registry["relic_fail_forward"] = () => new FailForwardRelic();
        _registry["relic_compound_rep"] = () => new CompoundRepRelic();
        _registry["relic_rep_interest"] = () => new RepInterestRelic();
        _registry["relic_rep_dividend"] = () => new RepDividendRelic();
        _registry["relic_bond_bonus"] = () => new BondBonusRelic();

        // Story 17.6: Register real relic constructors for mechanic/timer relics
        _registry["relic_time_buyer"] = () => new TimeBuyerRelic();
        _registry["relic_diamond_hands"] = () => new DiamondHandsRelic();
        _registry["relic_market_manipulator"] = () => new MarketManipulatorRelic();
        _registry["relic_free_intel"] = () => new FreeIntelRelic();
        _registry["relic_extra_expansion"] = () => new ExtraExpansionRelic();

        // Story 17.7: Register real relic constructors for special relics
        _registry["relic_event_catalyst"] = () => new EventCatalystRelic();
        _registry["relic_relic_expansion"] = () => new RelicExpansionRelic();

        // All 23 relics now have real implementations — StubRelic fallback for safety only
        for (int i = 0; i < ShopItemDefinitions.RelicPool.Length; i++)
        {
            var id = ShopItemDefinitions.RelicPool[i].Id;
            if (!_registry.ContainsKey(id))
                _registry[id] = () => new StubRelic(id);
        }
    }
}
