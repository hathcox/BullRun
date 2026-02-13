using UnityEngine;

/// <summary>
/// Handles atomic shop purchase logic. Pure C# class (no MonoBehaviour)
/// for testability. Follows TradeExecutor/EventEffects pattern.
/// Validates affordability and duplicate ownership, then deducts cash
/// and adds item to ActiveItems in a single atomic operation.
/// </summary>
public class ShopTransaction
{
    /// <summary>
    /// Attempts to purchase the given item. Validates affordability and ownership,
    /// then atomically deducts cash and adds item to RunContext.ActiveItems.
    /// Publishes ShopItemPurchasedEvent on success.
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

            // Validate affordability
            if (!ctx.Portfolio.CanAfford(item.Cost))
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[ShopTransaction] Purchase rejected: insufficient cash ${ctx.Portfolio.Cash:F0} for {item.Name} (${item.Cost})");
                #endif
                return ShopPurchaseResult.InsufficientFunds;
            }

            // Atomic operation: add item first (reversible via Remove), then deduct cash
            ctx.ActiveItems.Add(item.Id);
            if (!ctx.Portfolio.DeductCash(item.Cost))
            {
                ctx.ActiveItems.Remove(item.Id);
                return ShopPurchaseResult.InsufficientFunds;
            }

            // Publish purchase event
            EventBus.Publish(new ShopItemPurchasedEvent
            {
                ItemId = item.Id,
                ItemName = item.Name,
                Cost = item.Cost,
                RemainingCash = ctx.Portfolio.Cash
            });

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[ShopTransaction] Purchased: {item.Name} for ${item.Cost} (remaining: ${ctx.Portfolio.Cash:F0})");
            #endif

            return ShopPurchaseResult.Success;
        }
        catch (System.Exception ex)
        {
            // Rollback: remove item if it was added (safe no-op if not in list)
            ctx.ActiveItems.Remove(item.Id);
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
