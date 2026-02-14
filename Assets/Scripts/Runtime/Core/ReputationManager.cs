/// <summary>
/// Tracks Reputation currency within a run. Reputation is the shop currency —
/// separate from Portfolio.Cash (trading capital). Plain C# class, not MonoBehaviour.
/// Owned by RunContext. Resets to 0 on new run. Earning logic in FIX-14.
/// </summary>
public class ReputationManager
{
    /// <summary>
    /// Current Reputation balance. Integer currency — no fractional Rep.
    /// </summary>
    public int Current { get; private set; }

    public ReputationManager()
    {
        Current = GameConfig.StartingReputation;
    }

    /// <summary>
    /// Awards Reputation. Amount must be positive.
    /// </summary>
    public void Add(int amount)
    {
        if (amount <= 0) return;
        Current += amount;
    }

    /// <summary>
    /// Spends Reputation on a shop purchase. Returns true if successful.
    /// Rejects if amount exceeds current balance (balance unchanged).
    /// </summary>
    public bool Spend(int amount)
    {
        if (amount < 0) return false;
        if (amount == 0) return true;
        if (amount > Current) return false;
        Current -= amount;
        return true;
    }

    /// <summary>
    /// Returns true if the player can afford the given cost.
    /// </summary>
    public bool CanAfford(int cost)
    {
        return cost >= 0 && Current >= cost;
    }

    /// <summary>
    /// Resets Reputation to 0. Called on new run start.
    /// </summary>
    public void Reset()
    {
        Current = 0;
    }
}
