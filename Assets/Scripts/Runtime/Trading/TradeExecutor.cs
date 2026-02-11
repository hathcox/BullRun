using System;
using UnityEngine;

/// <summary>
/// Executes trade operations (buy/sell/short).
/// Try-catch at system boundary per architecture error handling pattern.
/// </summary>
public class TradeExecutor
{
    /// <summary>
    /// Executes a buy order. Returns true on success, false if rejected.
    /// Silently rejects if insufficient cash — no error dialogs.
    /// </summary>
    public bool ExecuteBuy(string stockId, int shares, float currentPrice, Portfolio portfolio)
    {
        try
        {
            float cost = shares * currentPrice;
            if (!portfolio.CanAfford(cost))
            {
                Debug.Log($"[Trading] Buy rejected: insufficient cash for {shares}x {stockId} at ${currentPrice:F2}");
                return false;
            }

            portfolio.OpenPosition(stockId, shares, currentPrice);
            EventBus.Publish(new TradeExecutedEvent
            {
                StockId = stockId,
                Shares = shares,
                Price = currentPrice,
                IsBuy = true,
                IsShort = false,
                TotalCost = cost
            });

            Debug.Log($"[Trading] BUY executed: {shares} shares of {stockId} at ${currentPrice:F2}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Trading] Trade failed: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Executes a sell order. Returns true on success, false if rejected.
    /// Silently rejects if no position held or insufficient shares.
    /// </summary>
    public bool ExecuteSell(string stockId, int shares, float currentPrice, Portfolio portfolio)
    {
        try
        {
            var position = portfolio.GetPosition(stockId);
            if (position == null || position.Shares < shares)
            {
                Debug.Log($"[Trading] Sell rejected: no position or insufficient shares for {shares}x {stockId}");
                return false;
            }

            float pnl = portfolio.ClosePosition(stockId, shares, currentPrice);
            float totalProceeds = shares * currentPrice;

            EventBus.Publish(new TradeExecutedEvent
            {
                StockId = stockId,
                Shares = shares,
                Price = currentPrice,
                IsBuy = false,
                IsShort = false,
                TotalCost = totalProceeds
            });

            Debug.Log($"[Trading] SELL executed: {shares} shares of {stockId} at ${currentPrice:F2} (P&L: {(pnl >= 0 ? "+" : "")}${pnl:F2})");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Trading] Trade failed: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Executes a short sell order. Returns true on success, false if rejected.
    /// Silently rejects if insufficient cash for margin requirement.
    /// </summary>
    public bool ExecuteShort(string stockId, int shares, float currentPrice, Portfolio portfolio)
    {
        try
        {
            float margin = shares * currentPrice * GameConfig.ShortMarginRequirement;
            if (!portfolio.CanAfford(margin))
            {
                Debug.Log($"[Trading] Short rejected: insufficient cash for margin on {shares}x {stockId} at ${currentPrice:F2}");
                return false;
            }

            var position = portfolio.OpenShort(stockId, shares, currentPrice);
            if (position == null)
                return false;

            EventBus.Publish(new TradeExecutedEvent
            {
                StockId = stockId,
                Shares = shares,
                Price = currentPrice,
                IsBuy = false,
                IsShort = true,
                TotalCost = margin
            });

            Debug.Log($"[Trading] SHORT executed: {shares} shares of {stockId} at ${currentPrice:F2} (margin held: ${margin:F2})");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Trading] Trade failed: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Covers a short position (buys back shares). Returns true on success, false if rejected.
    /// Silently rejects if no short position or insufficient shares.
    /// </summary>
    public bool ExecuteCover(string stockId, int shares, float currentPrice, Portfolio portfolio)
    {
        try
        {
            var position = portfolio.GetPosition(stockId);
            if (position == null || !position.IsShort || position.Shares < shares)
            {
                Debug.Log($"[Trading] Cover rejected: no short position or insufficient shares for {shares}x {stockId}");
                return false;
            }

            float pnl = portfolio.CoverShort(stockId, shares, currentPrice);

            EventBus.Publish(new TradeExecutedEvent
            {
                StockId = stockId,
                Shares = shares,
                Price = currentPrice,
                IsBuy = true,
                IsShort = true,
                TotalCost = shares * currentPrice
            });

            Debug.Log($"[Trading] COVER executed: {shares} shares of {stockId} at ${currentPrice:F2} (P&L: {(pnl >= 0 ? "+" : "")}${pnl:F2})");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Trading] Trade failed: {e.Message}");
            return false;
        }
    }
}
