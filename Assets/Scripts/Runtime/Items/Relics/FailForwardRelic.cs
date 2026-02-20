/// <summary>
/// Story 17.5: Fail Forward — when a round ends in a margin call, the player still earns
/// the base Reputation that would have been awarded for that round.
/// Passive relic: MarginCallState checks for this relic's presence during margin call handling.
/// </summary>
public class FailForwardRelic : RelicBase
{
    public override string Id => "relic_fail_forward";
}
