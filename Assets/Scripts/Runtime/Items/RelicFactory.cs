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
        for (int i = 0; i < ShopItemDefinitions.RelicPool.Length; i++)
        {
            var id = ShopItemDefinitions.RelicPool[i].Id;
            _registry[id] = () => new StubRelic(id);
        }
    }
}
