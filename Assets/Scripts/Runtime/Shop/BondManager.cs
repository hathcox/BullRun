using System.Collections.Generic;

/// <summary>
/// Manages bond purchases, sales, and Rep payouts (Story 13.6).
/// Plain C# class (not MonoBehaviour) for testability.
/// Bonds cost Cash (not Reputation) and generate +1 Rep per bond per round at round start.
/// </summary>
public class BondManager
{
    private readonly RunContext _ctx;

    public BondManager(RunContext ctx)
    {
        _ctx = ctx;
    }

    /// <summary>
    /// Total bonds currently held.
    /// </summary>
    public int BondsOwned => _ctx.BondsOwned;

    /// <summary>
    /// Purchase history for LIFO sell and tracking.
    /// </summary>
    public List<BondRecord> BondPurchaseHistory => _ctx.BondPurchaseHistory;

    /// <summary>
    /// Returns the bond price for the given round (1-indexed).
    /// Returns 0 for Round 8 (no bonds available).
    /// </summary>
    public static int GetCurrentPrice(int currentRound)
    {
        int index = currentRound - 1;
        if (index < 0 || index >= GameConfig.BondPricePerRound.Length)
            return 0;
        return GameConfig.BondPricePerRound[index];
    }

    /// <summary>
    /// Returns true if the player can purchase a bond this round.
    /// Cannot purchase on Round 8 (no future rounds to earn from) or if can't afford.
    /// </summary>
    public bool CanPurchase(int currentRound, float currentCash)
    {
        if (currentRound >= GameConfig.TotalRounds) return false;
        int price = GetCurrentPrice(currentRound);
        if (price <= 0) return false;
        return currentCash >= price;
    }

    /// <summary>
    /// Purchases a bond. Deducts cash, records purchase, fires BondPurchasedEvent.
    /// Returns the ShopPurchaseResult.
    /// </summary>
    public ShopPurchaseResult Purchase(int currentRound, Portfolio portfolio)
    {
        if (currentRound >= GameConfig.TotalRounds)
            return ShopPurchaseResult.Error;

        int price = GetCurrentPrice(currentRound);
        if (price <= 0)
            return ShopPurchaseResult.Error;

        if (!portfolio.CanAfford(price))
            return ShopPurchaseResult.InsufficientFunds;

        if (!portfolio.DeductCash(price))
            return ShopPurchaseResult.InsufficientFunds;

        _ctx.BondsOwned++;
        _ctx.BondPurchaseHistory.Add(new BondRecord(currentRound, price));

        EventBus.Publish(new BondPurchasedEvent
        {
            Round = currentRound,
            PricePaid = price,
            TotalBondsOwned = _ctx.BondsOwned,
            RemainingCash = portfolio.Cash
        });

        return ShopPurchaseResult.Success;
    }

    /// <summary>
    /// Returns the sell price for the most recent bond (LIFO).
    /// Sell price = most recent bond's purchase price × BondSellMultiplier.
    /// Returns 0 if no bonds owned.
    /// </summary>
    public float GetSellPrice()
    {
        if (_ctx.BondsOwned <= 0 || _ctx.BondPurchaseHistory.Count == 0)
            return 0f;

        var lastBond = _ctx.BondPurchaseHistory[_ctx.BondPurchaseHistory.Count - 1];
        return lastBond.PricePaid * GameConfig.BondSellMultiplier;
    }

    /// <summary>
    /// Sells the most recent bond (LIFO). Adds cash to portfolio, fires BondSoldEvent.
    /// </summary>
    public ShopPurchaseResult Sell(Portfolio portfolio)
    {
        if (_ctx.BondsOwned <= 0 || _ctx.BondPurchaseHistory.Count == 0)
            return ShopPurchaseResult.Error;

        float sellPrice = GetSellPrice();
        _ctx.BondPurchaseHistory.RemoveAt(_ctx.BondPurchaseHistory.Count - 1);
        _ctx.BondsOwned--;
        portfolio.AddCash(sellPrice);

        EventBus.Publish(new BondSoldEvent
        {
            SellPrice = sellPrice,
            TotalBondsOwned = _ctx.BondsOwned,
            CashAfterSale = portfolio.Cash
        });

        return ShopPurchaseResult.Success;
    }

    /// <summary>
    /// Returns the Rep earned per round from bonds (BondsOwned × BondRepPerRoundPerBond).
    /// </summary>
    public int GetRepPerRound()
    {
        return _ctx.BondsOwned * GameConfig.BondRepPerRoundPerBond;
    }

    /// <summary>
    /// Pays out bond Rep at round start. Adds Rep to ReputationManager and fires BondRepPaidEvent.
    /// </summary>
    public void PayoutRep(ReputationManager rep)
    {
        int repEarned = GetRepPerRound();
        if (repEarned <= 0) return;

        rep.Add(repEarned);

        EventBus.Publish(new BondRepPaidEvent
        {
            BondsOwned = _ctx.BondsOwned,
            RepEarned = repEarned,
            TotalReputation = rep.Current
        });
    }
}
