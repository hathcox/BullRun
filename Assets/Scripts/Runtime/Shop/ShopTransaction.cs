using UnityEngine;

/// <summary>
/// Handles atomic shop purchase logic for all panel types.
/// Pure C# class (no MonoBehaviour) for testability.
/// Relic/Expansion/Tip purchases deduct Reputation.
/// Bond purchase deducts Cash; Bond sell adds Cash.
/// </summary>
public class ShopTransaction
{
    /// <summary>
    /// Purchases a relic (shop item). Validates Reputation affordability and duplicate ownership,
    /// then atomically deducts Reputation and adds item to RunContext.OwnedRelics.
    /// Publishes ShopItemPurchasedEvent on success. Portfolio.Cash is NOT affected.
    /// </summary>
    public ShopPurchaseResult PurchaseRelic(RunContext ctx, ShopItemDef item)
    {
        try
        {
            if (ctx.OwnedRelics.Contains(item.Id))
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[ShopTransaction] Relic rejected: {item.Name} already owned");
                #endif
                return ShopPurchaseResult.AlreadyOwned;
            }

            if (ctx.OwnedRelics.Count >= GameConfig.MaxRelicSlots)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[ShopTransaction] Relic rejected: max relic slots ({GameConfig.MaxRelicSlots}) reached");
                #endif
                return ShopPurchaseResult.SlotsFull;
            }

            if (!ctx.Reputation.CanAfford(item.Cost))
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[ShopTransaction] Relic rejected: insufficient Rep {ctx.Reputation.Current} for {item.Name} ({item.Cost} Rep)");
                #endif
                return ShopPurchaseResult.InsufficientFunds;
            }

            if (!ctx.Reputation.Spend(item.Cost))
            {
                return ShopPurchaseResult.InsufficientFunds;
            }

            ctx.OwnedRelics.Add(item.Id);

            EventBus.Publish(new ShopItemPurchasedEvent
            {
                ItemId = item.Id,
                ItemName = item.Name,
                Cost = item.Cost,
                RemainingReputation = ctx.Reputation.Current
            });

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[ShopTransaction] Relic purchased: {item.Name} for {item.Cost} Rep (remaining: {ctx.Reputation.Current} Rep)");
            #endif

            return ShopPurchaseResult.Success;
        }
        catch (System.Exception ex)
        {
            ctx?.Reputation.Add(item.Cost);
            ctx?.OwnedRelics.Remove(item.Id);
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"[ShopTransaction] Relic purchase failed for {item.Name}: {ex.Message}");
            #endif
            return ShopPurchaseResult.Error;
        }
    }

    /// <summary>
    /// Backwards-compatible entry point. Delegates to PurchaseRelic.
    /// </summary>
    public ShopPurchaseResult TryPurchase(RunContext ctx, ShopItemDef item)
    {
        return PurchaseRelic(ctx, item);
    }

    /// <summary>
    /// Purchases an expansion. Validates Reputation affordability and duplicate ownership,
    /// then atomically deducts Reputation and adds expansion ID to OwnedExpansions.
    /// </summary>
    public ShopPurchaseResult PurchaseExpansion(RunContext ctx, string expansionId, string displayName, int cost)
    {
        try
        {
            if (ctx.OwnedExpansions.Contains(expansionId))
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[ShopTransaction] Expansion rejected: {displayName} already owned");
                #endif
                return ShopPurchaseResult.AlreadyOwned;
            }

            if (!ctx.Reputation.CanAfford(cost))
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[ShopTransaction] Expansion rejected: insufficient Rep {ctx.Reputation.Current} for {displayName} ({cost} Rep)");
                #endif
                return ShopPurchaseResult.InsufficientFunds;
            }

            if (!ctx.Reputation.Spend(cost))
            {
                return ShopPurchaseResult.InsufficientFunds;
            }

            ctx.OwnedExpansions.Add(expansionId);

            EventBus.Publish(new ShopExpansionPurchasedEvent
            {
                ExpansionId = expansionId,
                DisplayName = displayName,
                Cost = cost,
                RemainingReputation = ctx.Reputation.Current
            });

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[ShopTransaction] Expansion purchased: {displayName} for {cost} Rep");
            #endif

            return ShopPurchaseResult.Success;
        }
        catch (System.Exception ex)
        {
            ctx?.Reputation.Add(cost);
            ctx?.OwnedExpansions.Remove(expansionId);
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"[ShopTransaction] Expansion purchase failed for {displayName}: {ex.Message}");
            #endif
            return ShopPurchaseResult.Error;
        }
    }

    /// <summary>
    /// Purchases an insider tip reveal. Deducts Reputation and adds tip to RevealedTips.
    /// </summary>
    public ShopPurchaseResult PurchaseTip(RunContext ctx, RevealedTip tip, int cost)
    {
        try
        {
            if (ctx.RevealedTips.Count >= ctx.InsiderTipSlots)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[ShopTransaction] Tip rejected: all {ctx.InsiderTipSlots} tip slots full");
                #endif
                return ShopPurchaseResult.SlotsFull;
            }

            if (!ctx.Reputation.CanAfford(cost))
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[ShopTransaction] Tip rejected: insufficient Rep {ctx.Reputation.Current} ({cost} Rep needed)");
                #endif
                return ShopPurchaseResult.InsufficientFunds;
            }

            if (!ctx.Reputation.Spend(cost))
            {
                return ShopPurchaseResult.InsufficientFunds;
            }

            ctx.RevealedTips.Add(tip);

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[ShopTransaction] Tip purchased: {tip.Type} for {cost} Rep");
            #endif

            return ShopPurchaseResult.Success;
        }
        catch (System.Exception ex)
        {
            ctx?.Reputation.Add(cost);
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"[ShopTransaction] Tip purchase failed: {ex.Message}");
            #endif
            return ShopPurchaseResult.Error;
        }
    }

    /// <summary>
    /// Purchases a bond. Deducts Cash (not Reputation) and increments BondsOwned.
    /// Records purchase in BondPurchaseHistory.
    /// </summary>
    public ShopPurchaseResult PurchaseBond(RunContext ctx, float price)
    {
        try
        {
            if (!ctx.Portfolio.CanAfford(price))
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[ShopTransaction] Bond rejected: insufficient cash ${ctx.Portfolio.Cash:F2} for ${price:F2}");
                #endif
                return ShopPurchaseResult.InsufficientFunds;
            }

            if (!ctx.Portfolio.DeductCash(price))
            {
                return ShopPurchaseResult.InsufficientFunds;
            }
            ctx.BondsOwned++;
            ctx.BondPurchaseHistory.Add(new BondRecord(ctx.CurrentRound, price));

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[ShopTransaction] Bond purchased for ${price:F2} (bonds owned: {ctx.BondsOwned})");
            #endif

            return ShopPurchaseResult.Success;
        }
        catch (System.Exception ex)
        {
            ctx?.Portfolio.AddCash(price);
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"[ShopTransaction] Bond purchase failed: {ex.Message}");
            #endif
            return ShopPurchaseResult.Error;
        }
    }

    /// <summary>
    /// Sells a bond. Adds Cash to portfolio and decrements BondsOwned.
    /// </summary>
    public ShopPurchaseResult SellBond(RunContext ctx, float sellPrice)
    {
        try
        {
            if (ctx.BondsOwned <= 0)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log("[ShopTransaction] Bond sell rejected: no bonds owned");
                #endif
                return ShopPurchaseResult.Error;
            }

            ctx.BondsOwned--;
            ctx.Portfolio.AddCash(sellPrice);

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[ShopTransaction] Bond sold for ${sellPrice:F2} (bonds remaining: {ctx.BondsOwned})");
            #endif

            return ShopPurchaseResult.Success;
        }
        catch (System.Exception ex)
        {
            ctx.BondsOwned++;
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"[ShopTransaction] Bond sell failed: {ex.Message}");
            #endif
            return ShopPurchaseResult.Error;
        }
    }
}

/// <summary>
/// Result of a shop purchase attempt.
/// </summary>
public enum ShopPurchaseResult
{
    Success,
    InsufficientFunds,
    AlreadyOwned,
    SlotsFull,
    Error
}
