/// <summary>
/// Placeholder relic that does nothing. Configurable Id via constructor.
/// Used by RelicFactory for all 8 placeholder relic IDs until real relic
/// classes are built in Stories 17.3-17.7.
/// </summary>
public class StubRelic : RelicBase
{
    private readonly string _id;

    public override string Id => _id;

    public StubRelic(string id)
    {
        _id = id;
    }
}
