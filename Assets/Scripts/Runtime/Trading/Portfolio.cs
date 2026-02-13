using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages player's cash and open positions.
/// Central data carrier for trading state during a run.
/// </summary>
public class Portfolio
{
    private readonly Dictionary<string, Position> _positions = new Dictionary<string, Position>();
    private readonly Dictionary<string, float> _latestPrices = new Dictionary<string, float>();
    private float _roundStartValue;

    public float Cash { get; private set; }

    public Portfolio(float startingCash)
    {
        Cash = startingCash;
    }

    /// <summary>
    /// Sets cash directly. Used by debug tools (F2 god mode).
    /// </summary>
    internal void SetCash(float amount)
    {
        Cash = amount;
    }

    /// <summary>
    /// Subscribes to PriceUpdatedEvent to cache latest prices per stock.
    /// </summary>
    public void SubscribeToPriceUpdates()
    {
        EventBus.Subscribe<PriceUpdatedEvent>(OnPriceUpdated);
    }

    /// <summary>
    /// Unsubscribes from PriceUpdatedEvent to stop receiving new price updates.
    /// Retains last cached prices so GetTotalValue() still works after unsubscribing.
    /// Must be called before discarding a Portfolio to prevent memory leaks via static EventBus.
    /// </summary>
    public void UnsubscribeFromPriceUpdates()
    {
        EventBus.Unsubscribe<PriceUpdatedEvent>(OnPriceUpdated);
    }

    private void OnPriceUpdated(PriceUpdatedEvent e)
    {
        // NOTE: PriceUpdatedEvent uses int StockId, Portfolio uses string.
        // This ToString() bridge works when positions use stringified int IDs.
        // A stock registry mapping int<->string will be needed when stock names differ from IDs.
        _latestPrices[e.StockId.ToString()] = e.NewPrice;
    }

    private float GetCachedPrice(string stockId)
    {
        if (!_latestPrices.TryGetValue(stockId, out float price))
        {
            Debug.LogWarning($"[Trading] No cached price for {stockId} — using 0. Ensure PriceUpdatedEvent is being published.");
            return 0f;
        }
        return price;
    }

    /// <summary>
    /// Returns true if the portfolio has enough cash for the given cost.
    /// </summary>
    public bool CanAfford(float cost)
    {
        return Cash >= cost;
    }

    /// <summary>
    /// Deducts an amount from cash (e.g. shop purchases). Returns false if insufficient funds.
    /// </summary>
    public bool DeductCash(float amount)
    {
        if (!CanAfford(amount))
            return false;
        Cash -= amount;
        ClampCash();
        return true;
    }

    private void ClampCash()
    {
        if (Cash < 0f)
            Cash = 0f;
    }

    /// <summary>
    /// Opens or adds to a position. Deducts cost from cash.
    /// Returns null if insufficient cash. If a position already exists for the stock, averages in.
    /// </summary>
    public Position OpenPosition(string stockId, int shares, float price)
    {
        float cost = shares * price;
        if (!CanAfford(cost))
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Trading] Buy rejected: insufficient cash ${Cash:F2} for {shares}x {stockId} at ${price:F2} (cost: ${cost:F2})");
            #endif
            return null;
        }

        // Reject if a short position exists — must cover short first
        if (_positions.TryGetValue(stockId, out var existing) && existing.IsShort)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Trading] Buy rejected: short position exists for {stockId} — cover short first");
            #endif
            return null;
        }

        Cash -= cost;

        if (existing != null)
        {
            int totalShares = existing.Shares + shares;
            float avgPrice = (existing.AverageBuyPrice * existing.Shares + price * shares) / totalShares;
            var averaged = new Position(stockId, totalShares, avgPrice);
            _positions[stockId] = averaged;
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Trading] Position averaged: {stockId} now {totalShares} shares at ${avgPrice:F2}");
            #endif
            return averaged;
        }

        var position = new Position(stockId, shares, price);
        _positions[stockId] = position;
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[Trading] Position opened: {shares}x {stockId} at ${price:F2}");
        #endif
        return position;
    }

    /// <summary>
    /// Opens a short position. Deducts margin collateral from cash.
    /// Returns the position, or null if insufficient cash for margin.
    /// </summary>
    public Position OpenShort(string stockId, int shares, float price)
    {
        // Reject if a long position exists — must sell long first
        if (_positions.TryGetValue(stockId, out var existing) && existing.IsLong)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Trading] Short rejected: long position exists for {stockId} — sell long first");
            #endif
            return null;
        }

        float margin = shares * price * GameConfig.ShortMarginRequirement;
        if (!CanAfford(margin))
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Trading] Short rejected: insufficient cash for margin ${margin:F2} on {shares}x {stockId}");
            #endif
            return null;
        }

        Cash -= margin;
        var position = new Position(stockId, shares, price, margin);
        _positions[stockId] = position;
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[Trading] SHORT opened: {shares}x {stockId} at ${price:F2} (margin held: ${margin:F2})");
        #endif
        return position;
    }

    /// <summary>
    /// Covers (closes) a short position. Returns margin + P&L to cash.
    /// If loss exceeds margin, cash floors at the margin return minus loss (can't go negative from this).
    /// Returns realized P&L. Returns 0 if no short position or insufficient shares.
    /// </summary>
    public float CoverShort(string stockId, int shares, float currentPrice)
    {
        if (!_positions.TryGetValue(stockId, out var position) || !position.IsShort)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Trading] Cover rejected: no short position for {stockId}");
            #endif
            return 0f;
        }

        if (shares > position.Shares)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Trading] Cover rejected: requested {shares} but only short {position.Shares} of {stockId}");
            #endif
            return 0f;
        }

        float pnl = position.CalculateRealizedPnL(currentPrice, shares);
        float marginPortion = (shares == position.Shares)
            ? position.MarginHeld
            : position.MarginHeld * ((float)shares / position.Shares);

        float cashReturn = marginPortion + pnl;
        if (cashReturn < 0f)
            cashReturn = 0f; // margin eaten, but cash can't go negative from cover

        Cash += cashReturn;

        if (shares == position.Shares)
        {
            _positions.Remove(stockId);
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Trading] SHORT covered: {stockId} (P&L: {(pnl >= 0 ? "+" : "")}${pnl:F2}, margin returned: ${marginPortion:F2})");
            #endif
        }
        else
        {
            int remainingShares = position.Shares - shares;
            float remainingMargin = position.MarginHeld - marginPortion;
            _positions[stockId] = new Position(stockId, remainingShares, position.AverageBuyPrice, remainingMargin);
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Trading] SHORT partially covered: {stockId} now {remainingShares} shares (P&L: {(pnl >= 0 ? "+" : "")}${pnl:F2})");
            #endif
        }

        return pnl;
    }

    /// <summary>
    /// Closes or reduces a position. Adds proceeds to cash.
    /// Returns realized P&L. Returns 0 if no position or insufficient shares.
    /// </summary>
    public float ClosePosition(string stockId, int shares, float currentPrice)
    {
        if (!_positions.TryGetValue(stockId, out var position) || position.IsShort)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Trading] Sell rejected: no long position for {stockId}");
            #endif
            return 0f;
        }

        if (shares > position.Shares)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Trading] Sell rejected: requested {shares} but only hold {position.Shares} of {stockId}");
            #endif
            return 0f;
        }

        float pnl = position.CalculateRealizedPnL(currentPrice, shares);
        Cash += shares * currentPrice;

        if (shares == position.Shares)
        {
            _positions.Remove(stockId);
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Trading] Position closed: {stockId} (P&L: {(pnl >= 0 ? "+" : "")}${pnl:F2})");
            #endif
        }
        else
        {
            int remainingShares = position.Shares - shares;
            _positions[stockId] = new Position(stockId, remainingShares, position.AverageBuyPrice);
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Trading] Position reduced: {stockId} now {remainingShares} shares (P&L: {(pnl >= 0 ? "+" : "")}${pnl:F2})");
            #endif
        }

        return pnl;
    }

    /// <summary>
    /// Number of open positions.
    /// </summary>
    public int PositionCount => _positions.Count;

    /// <summary>
    /// Returns the position for a stock, or null if none exists.
    /// </summary>
    public Position GetPosition(string stockId)
    {
        _positions.TryGetValue(stockId, out var position);
        return position;
    }

    /// <summary>
    /// Returns all open positions as a read-only collection.
    /// Returns the underlying collection directly to avoid per-call heap allocation.
    /// </summary>
    public IReadOnlyCollection<Position> GetAllPositions()
    {
        return _positions.Values;
    }

    /// <summary>
    /// Closes all positions at current prices. Returns total realized P&L.
    /// Longs: sells all shares, adds proceeds to cash.
    /// Shorts: covers all, returns margin +/- P&L to cash.
    /// Clears position and price caches.
    /// Note: Callers should publish TradeExecutedEvent/RoundEndedEvent as needed.
    /// </summary>
    public float LiquidateAllPositions(Func<string, float> getCurrentPrice)
    {
        float totalPnL = 0f;
        foreach (var kvp in _positions)
        {
            var pos = kvp.Value;
            float price = getCurrentPrice(kvp.Key);
            float pnl = pos.UnrealizedPnL(price);
            totalPnL += pnl;

            if (pos.IsShort)
            {
                float cashReturn = pos.MarginHeld + pnl;
                if (cashReturn < 0f)
                    cashReturn = 0f;
                Cash += cashReturn;
            }
            else
            {
                Cash += pos.Shares * price;
            }
        }
        _positions.Clear();
        _latestPrices.Clear();
        return totalPnL;
    }

    /// <summary>
    /// Returns the unrealized P&L for a specific position, or 0 if no position exists.
    /// </summary>
    public float GetPositionPnL(string stockId, float currentPrice)
    {
        if (_positions.TryGetValue(stockId, out var position))
            return position.UnrealizedPnL(currentPrice);
        return 0f;
    }

    /// <summary>
    /// Returns true if a position exists for the given stock.
    /// </summary>
    public bool HasPosition(string stockId)
    {
        return _positions.ContainsKey(stockId);
    }

    /// <summary>
    /// Captures the current total value as the round start baseline.
    /// </summary>
    public void StartRound(float startingValue)
    {
        _roundStartValue = startingValue;
    }

    /// <summary>
    /// Current total value minus round start value.
    /// </summary>
    public float GetRoundProfit(Func<string, float> getCurrentPrice)
    {
        return GetTotalValue(getCurrentPrice) - _roundStartValue;
    }

    /// <summary>
    /// Round profit using cached prices from PriceUpdatedEvent.
    /// </summary>
    public float GetRoundProfit()
    {
        return GetRoundProfit(GetCachedPrice);
    }

    /// <summary>
    /// Total portfolio value: cash + market value of all positions.
    /// Longs: shares * currentPrice
    /// Shorts: marginHeld + unrealizedPnL (can be negative)
    /// </summary>
    public float GetTotalValue(Func<string, float> getCurrentPrice)
    {
        float total = Cash;
        foreach (var kvp in _positions)
        {
            var pos = kvp.Value;
            float price = getCurrentPrice(kvp.Key);
            if (pos.IsShort)
                total += pos.MarginHeld + pos.UnrealizedPnL(price);
            else
                total += pos.Shares * price;
        }
        return total;
    }

    /// <summary>
    /// Total portfolio value using cached prices from PriceUpdatedEvent.
    /// </summary>
    public float GetTotalValue()
    {
        return GetTotalValue(GetCachedPrice);
    }

    /// <summary>
    /// Sum of unrealized P&L across all open positions.
    /// </summary>
    public float GetTotalUnrealizedPnL(Func<string, float> getCurrentPrice)
    {
        float total = 0f;
        foreach (var kvp in _positions)
        {
            total += kvp.Value.UnrealizedPnL(getCurrentPrice(kvp.Key));
        }
        return total;
    }

    /// <summary>
    /// Total unrealized P&L using cached prices from PriceUpdatedEvent.
    /// </summary>
    public float GetTotalUnrealizedPnL()
    {
        return GetTotalUnrealizedPnL(GetCachedPrice);
    }
}
