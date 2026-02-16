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
    /// Purchases a relic using RelicDef (Story 13.3 — no rarity/category).
    /// Validates Reputation affordability, duplicate ownership, and capacity.
    /// Checks ExpansionManager for Expanded Inventory to determine actual max slots (AC 13).
    /// Publishes ShopItemPurchasedEvent on success. Portfolio.Cash is NOT affected.
    /// </summary>
    public ShopPurchaseResult PurchaseRelic(RunContext ctx, RelicDef relic)
    {
        bool repDeducted = false;
        bool relicAdded = false;
        try
        {
            if (ctx.OwnedRelics.Contains(relic.Id))
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[ShopTransaction] Relic rejected: {relic.Name} already owned");
                #endif
                return ShopPurchaseResult.AlreadyOwned;
            }

            int maxSlots = GetEffectiveMaxRelicSlots(ctx);
            if (ctx.OwnedRelics.Count >= maxSlots)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[ShopTransaction] Relic rejected: max relic slots ({maxSlots}) reached");
                #endif
                return ShopPurchaseResult.SlotsFull;
            }

            if (!ctx.Reputation.CanAfford(relic.Cost))
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[ShopTransaction] Relic rejected: insufficient Rep {ctx.Reputation.Current} for {relic.Name} ({relic.Cost} Rep)");
                #endif
                return ShopPurchaseResult.InsufficientFunds;
            }

            if (!ctx.Reputation.Spend(relic.Cost))
            {
                return ShopPurchaseResult.InsufficientFunds;
            }
            repDeducted = true;

            ctx.OwnedRelics.Add(relic.Id);
            relicAdded = true;

            EventBus.Publish(new ShopItemPurchasedEvent
            {
                ItemId = relic.Id,
                ItemName = relic.Name,
                Cost = relic.Cost,
                RemainingReputation = ctx.Reputation.Current
            });

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[ShopTransaction] Relic purchased: {relic.Name} for {relic.Cost} Rep (remaining: {ctx.Reputation.Current} Rep)");
            #endif

            return ShopPurchaseResult.Success;
        }
        catch (System.Exception ex)
        {
            if (repDeducted) ctx?.Reputation.Add(relic.Cost);
            if (relicAdded) ctx?.OwnedRelics.Remove(relic.Id);
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"[ShopTransaction] Relic purchase failed for {relic.Name}: {ex.Message}");
            #endif
            return ShopPurchaseResult.Error;
        }
    }

    /// <summary>
    /// Returns the effective max relic slots, accounting for Expanded Inventory expansion (AC 13).
    /// Checks OwnedExpansions for "expanded_inventory" to grant bonus slots.
    /// </summary>
    public static int GetEffectiveMaxRelicSlots(RunContext ctx)
    {
        int maxSlots = GameConfig.MaxRelicSlots;
        if (ctx.OwnedExpansions.Contains(ExpansionDefinitions.ExpandedInventory))
        {
            maxSlots += 2;
        }
        return maxSlots;
    }

    /// <summary>
    /// Legacy overload accepting ShopItemDef. Converts to RelicDef and delegates.
    /// </summary>
    public ShopPurchaseResult PurchaseRelic(RunContext ctx, ShopItemDef item)
    {
        return PurchaseRelic(ctx, new RelicDef(item.Id, item.Name, item.Description, item.Cost));
    }

    /// <summary>
    /// Backwards-compatible entry point. Delegates to PurchaseRelic.
    /// </summary>
    public ShopPurchaseResult TryPurchase(RunContext ctx, ShopItemDef item)
    {
        return PurchaseRelic(ctx, item);
    }

    /// <summary>
    /// Processes a reroll: deducts cost, increments reroll count (AC 7, 8, 9).
    /// Cost = RerollBaseCost + (RerollCostIncrement * currentRerollCount).
    /// Returns true if reroll succeeded, false if insufficient funds.
    /// </summary>
    public bool TryReroll(RunContext ctx)
    {
        int cost = GetRerollCost(ctx.CurrentShopRerollCount);
        if (!ctx.Reputation.CanAfford(cost))
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[ShopTransaction] Reroll rejected: insufficient Rep {ctx.Reputation.Current} for reroll cost {cost}");
            #endif
            return false;
        }

        ctx.Reputation.Spend(cost);
        ctx.CurrentShopRerollCount++;

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[ShopTransaction] Reroll #{ctx.CurrentShopRerollCount}: cost {cost} Rep (remaining: {ctx.Reputation.Current} Rep)");
        #endif

        return true;
    }

    /// <summary>
    /// Calculates reroll cost: RerollBaseCost + (RerollCostIncrement * rerollCount).
    /// </summary>
    public static int GetRerollCost(int currentRerollCount)
    {
        return GameConfig.RerollBaseCost + (GameConfig.RerollCostIncrement * currentRerollCount);
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
    /// Purchases a bond. Delegates to BondManager.Purchase for atomic buy logic.
    /// Deducts Cash (not Reputation). Fires BondPurchasedEvent (Story 13.6, AC 3, 15).
    /// </summary>
    public ShopPurchaseResult PurchaseBond(RunContext ctx, float price)
    {
        return ctx.Bonds.Purchase(ctx.CurrentRound, ctx.Portfolio);
    }

    /// <summary>
    /// Sells the most recent bond (LIFO). Delegates to BondManager.Sell for atomic sell logic.
    /// Adds sell price to portfolio cash. Fires BondSoldEvent (Story 13.6, AC 9, 10, 15).
    /// </summary>
    public ShopPurchaseResult SellBond(RunContext ctx)
    {
        return ctx.Bonds.Sell(ctx.Portfolio);
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
