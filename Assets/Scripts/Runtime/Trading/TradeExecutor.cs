using System;
using UnityEngine;

/// <summary>
/// Executes trade operations (buy/sell/short).
/// Try-catch at system boundary per architecture error handling pattern.
/// </summary>
public class TradeExecutor
{
    /// <summary>
    /// Controls whether trades can be executed. Set to false during market close
    /// to prevent trades during liquidation. TradingState sets this to true on Enter.
    /// </summary>
    public bool IsTradeEnabled { get; set; } = true;

    /// <summary>
    /// Executes a buy order. Returns true on success, false if rejected.
    /// Silently rejects if insufficient cash — no error dialogs.
    /// </summary>
    public bool ExecuteBuy(string stockId, int shares, float currentPrice, Portfolio portfolio)
    {
        if (!IsTradeEnabled)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[Trading] Buy rejected: trading is disabled (market closed)");
            #endif
            return false;
        }

        try
        {
            var position = portfolio.OpenPosition(stockId, shares, currentPrice);
            if (position == null)
                return false;

            float cost = shares * currentPrice;
            EventBus.Publish(new TradeExecutedEvent
            {
                StockId = stockId,
                Shares = shares,
                Price = currentPrice,
                IsBuy = true,
                IsShort = false,
                TotalCost = cost,
                ProfitLoss = 0f
            });

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Trading] BUY executed: {shares} shares of {stockId} at ${currentPrice:F2}");
            #endif
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
        if (!IsTradeEnabled)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[Trading] Sell rejected: trading is disabled (market closed)");
            #endif
            return false;
        }

        try
        {
            var position = portfolio.GetPosition(stockId);
            if (position == null || position.Shares < shares)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[Trading] Sell rejected: no long position or insufficient shares for {shares}x {stockId}");
                #endif
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
                TotalCost = totalProceeds,
                ProfitLoss = pnl
            });

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Trading] SELL executed: {shares} shares of {stockId} at ${currentPrice:F2} (P&L: {(pnl >= 0 ? "+" : "")}${pnl:F2})");
            #endif
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
    /// No capital required — rejects only for duplicate shorts or trading disabled.
    /// </summary>
    public bool ExecuteShort(string stockId, int shares, float currentPrice, Portfolio portfolio)
    {
        if (!IsTradeEnabled)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[Trading] Short rejected: trading is disabled (market closed)");
            #endif
            return false;
        }

        try
        {
            var position = portfolio.OpenShort(stockId, shares, currentPrice);
            if (position == null)
                return false;

            float margin = position.MarginHeld;
            EventBus.Publish(new TradeExecutedEvent
            {
                StockId = stockId,
                Shares = shares,
                Price = currentPrice,
                IsBuy = false,
                IsShort = true,
                TotalCost = margin,
                ProfitLoss = 0f
            });

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Trading] SHORT executed: {shares} shares of {stockId} at ${currentPrice:F2} (margin held: ${margin:F2})");
            #endif
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
        if (!IsTradeEnabled)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[Trading] Cover rejected: trading is disabled (market closed)");
            #endif
            return false;
        }

        try
        {
            var position = portfolio.GetShortPosition(stockId);
            if (position == null || position.Shares < shares)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"[Trading] Cover rejected: no short position or insufficient shares for {shares}x {stockId}");
                #endif
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
                TotalCost = shares * currentPrice,
                ProfitLoss = pnl
            });

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Trading] COVER executed: {shares} shares of {stockId} at ${currentPrice:F2} (P&L: {(pnl >= 0 ? "+" : "")}${pnl:F2})");
            #endif
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Trading] Trade failed: {e.Message}");
            return false;
        }
    }
}
