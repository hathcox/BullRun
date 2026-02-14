using UnityEngine;

/// <summary>
/// Handles atomic shop purchase logic. Pure C# class (no MonoBehaviour)
/// for testability. Follows TradeExecutor/EventEffects pattern.
/// FIX-12: Validates Reputation affordability (not cash) and duplicate ownership,
/// then deducts Rep and adds item to ActiveItems in a single atomic operation.
/// Portfolio.Cash is NEVER touched by shop purchases.
/// </summary>
public class ShopTransaction
{
    /// <summary>
    /// Attempts to purchase the given item. Validates Reputation affordability and ownership,
    /// then atomically deducts Reputation and adds item to RunContext.ActiveItems.
    /// Publishes ShopItemPurchasedEvent on success. Portfolio.Cash is NOT affected.
    /// </summary>
    public ShopPurchaseResult TryPurchase(RunContext ctx, ShopItemDef item)
    {
        try
        {
            // Validate not already owned
            if (ctx.ActiveItems.Contains(item.Id))
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[ShopTransaction] Purchase rejected: {item.Name} already owned");
                #endif
                return ShopPurchaseResult.AlreadyOwned;
            }

            // FIX-12: Validate Reputation affordability (not cash)
            if (!ctx.Reputation.CanAfford(item.Cost))
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[ShopTransaction] Purchase rejected: insufficient Rep {ctx.Reputation.Current} for {item.Name} ({item.Cost} Rep)");
                #endif
                return ShopPurchaseResult.InsufficientFunds;
            }

            // Atomic operation: add item first (reversible via Remove), then deduct Rep
            ctx.ActiveItems.Add(item.Id);
            if (!ctx.Reputation.Spend(item.Cost))
            {
                ctx.ActiveItems.Remove(item.Id);
                return ShopPurchaseResult.InsufficientFunds;
            }

            // Publish purchase event (FIX-12: Rep, not cash)
            EventBus.Publish(new ShopItemPurchasedEvent
            {
                ItemId = item.Id,
                ItemName = item.Name,
                Cost = item.Cost,
                RemainingReputation = ctx.Reputation.Current
            });

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[ShopTransaction] Purchased: {item.Name} for {item.Cost} Rep (remaining: {ctx.Reputation.Current} Rep)");
            #endif

            return ShopPurchaseResult.Success;
        }
        catch (System.Exception ex)
        {
            // Rollback: restore reputation and remove item if added (safe no-op if not in list)
            ctx?.Reputation.Add(item.Cost);
            ctx?.ActiveItems.Remove(item.Id);
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"[ShopTransaction] Purchase failed for {item.Name}: {ex.Message}");
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
    Error
}
