/// <summary>
/// Interface for game state machine states.
/// States: MetaHub → MarketOpen → Trading → MarketClose → Shop → (loop or) RunSummary
/// </summary>
public interface IGameState
{
    void Enter(RunContext ctx);
    void Update(RunContext ctx);
    void Exit(RunContext ctx);
}
