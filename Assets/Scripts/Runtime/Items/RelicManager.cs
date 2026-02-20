using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages ordered relic collection and dispatches lifecycle hooks.
/// Plain C# class (like BondManager) — owned by RunContext.
/// Relics are dispatched left-to-right in acquisition order.
/// Per-relic try-catch ensures one failing relic doesn't break others.
/// Story 17.1: Framework only — dispatch wiring in GameRunner.
/// </summary>
public class RelicManager
{
    private readonly RunContext _ctx;
    private readonly List<IRelic> _orderedRelics = new List<IRelic>();

    public IReadOnlyList<IRelic> OrderedRelics => _orderedRelics;

    public RelicManager(RunContext ctx)
    {
        _ctx = ctx;
    }

    public void AddRelic(string relicId)
    {
        var relic = RelicFactory.Create(relicId);
        if (relic == null)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"[RelicManager] Failed to create relic: {relicId}");
            #endif
            return;
        }

        _orderedRelics.Add(relic);
        SyncOwnedRelics();

        try { relic.OnAcquired(_ctx); }
        catch (Exception ex) { LogRelicError(relicId, "OnAcquired", ex); }
    }

    public void RemoveRelic(string relicId)
    {
        for (int i = 0; i < _orderedRelics.Count; i++)
        {
            if (_orderedRelics[i].Id == relicId)
            {
                var relic = _orderedRelics[i];
                try { relic.OnRemoved(_ctx); }
                catch (Exception ex) { LogRelicError(relicId, "OnRemoved", ex); }

                _orderedRelics.RemoveAt(i);
                SyncOwnedRelics();
                return;
            }
        }
    }

    public void ReorderRelic(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= _orderedRelics.Count) return;
        if (toIndex < 0 || toIndex >= _orderedRelics.Count) return;
        if (fromIndex == toIndex) return;

        var relic = _orderedRelics[fromIndex];
        _orderedRelics.RemoveAt(fromIndex);
        _orderedRelics.Insert(toIndex, relic);
        SyncOwnedRelics();
    }

    public IRelic GetRelicById(string id)
    {
        for (int i = 0; i < _orderedRelics.Count; i++)
        {
            if (_orderedRelics[i].Id == id)
                return _orderedRelics[i];
        }
        return null;
    }

    // ════════════════════════════════════════════════════════════════════
    // Passive query helpers — GameRunner calls these instead of raw GameConfig
    // Story 17.3: Trade modification relics
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns effective post-trade cooldown.
    /// Quick Draw: 0 for buy, 2x for sell. Catalyst Trader: +3s for buy.
    /// Effects stack additively (Quick Draw 0 + Catalyst Trader +3 = 3s for buy).
    /// </summary>
    public float GetEffectiveCooldown(bool isBuy)
    {
        float cooldown;
        if (GetRelicById("relic_quick_draw") != null)
        {
            cooldown = isBuy ? 0f : GameConfig.PostTradeCooldown * 2f;
        }
        else
        {
            cooldown = GameConfig.PostTradeCooldown;
        }

        // Story 17.4: Catalyst Trader adds +3s to buy cooldown
        if (isBuy && GetRelicById("relic_event_trigger") != null)
        {
            cooldown += 3f;
        }

        return cooldown;
    }

    /// <summary>
    /// Returns effective short share count.
    /// Dual Short expansion: 2x base shares.
    /// Bear Raid relic: 3 shares (overrides Dual Short).
    /// </summary>
    public int GetEffectiveShortShares()
    {
        if (GetRelicById("relic_short_multiplier") != null)
        {
            return 3;
        }
        int shares = GameConfig.ShortBaseShares;
        if (_ctx.OwnedExpansions.Contains(ExpansionDefinitions.DualShort))
        {
            shares *= 2;
        }
        return shares;
    }

    /// <summary>
    /// Story 17.6: Returns liquidation multiplier for long positions at market close.
    /// Diamond Hands: 1.30f if owned, else 1.0f.
    /// </summary>
    public float GetLiquidationMultiplier()
    {
        if (GetRelicById("relic_diamond_hands") != null)
        {
            return 1.30f;
        }
        return 1.0f;
    }

    /// <summary>
    /// Returns effective trade quantity. Double Dealer: doubles the base quantity.
    /// </summary>
    public int GetEffectiveTradeQuantity(int baseQty)
    {
        if (GetRelicById("relic_double_dealer") != null)
        {
            return baseQty * 2;
        }
        return baseQty;
    }

    // ════════════════════════════════════════════════════════════════════
    // Dispatch methods — iterate left-to-right with per-relic try-catch
    // ════════════════════════════════════════════════════════════════════

    public void DispatchRoundStart(RoundStartedEvent e)
    {
        for (int i = 0; i < _orderedRelics.Count; i++)
        {
            try { _orderedRelics[i].OnRoundStart(_ctx, e); }
            catch (Exception ex) { LogRelicError(_orderedRelics[i].Id, "OnRoundStart", ex); }
        }
    }

    public void DispatchRoundEnd(MarketClosedEvent e)
    {
        for (int i = 0; i < _orderedRelics.Count; i++)
        {
            try { _orderedRelics[i].OnRoundEnd(_ctx, e); }
            catch (Exception ex) { LogRelicError(_orderedRelics[i].Id, "OnRoundEnd", ex); }
        }
    }

    public void DispatchBeforeTrade(TradeExecutedEvent e)
    {
        for (int i = 0; i < _orderedRelics.Count; i++)
        {
            try { _orderedRelics[i].OnBeforeTrade(_ctx, e); }
            catch (Exception ex) { LogRelicError(_orderedRelics[i].Id, "OnBeforeTrade", ex); }
        }
    }

    public void DispatchAfterTrade(TradeExecutedEvent e)
    {
        for (int i = 0; i < _orderedRelics.Count; i++)
        {
            try { _orderedRelics[i].OnAfterTrade(_ctx, e); }
            catch (Exception ex) { LogRelicError(_orderedRelics[i].Id, "OnAfterTrade", ex); }
        }
    }

    public void DispatchMarketEvent(MarketEventFiredEvent e)
    {
        for (int i = 0; i < _orderedRelics.Count; i++)
        {
            try { _orderedRelics[i].OnMarketEventFired(_ctx, e); }
            catch (Exception ex) { LogRelicError(_orderedRelics[i].Id, "OnMarketEventFired", ex); }
        }
    }

    public void DispatchReputationChanged(int oldRep, int newRep)
    {
        for (int i = 0; i < _orderedRelics.Count; i++)
        {
            try { _orderedRelics[i].OnReputationChanged(_ctx, oldRep, newRep); }
            catch (Exception ex) { LogRelicError(_orderedRelics[i].Id, "OnReputationChanged", ex); }
        }
    }

    public void DispatchShopOpen()
    {
        for (int i = 0; i < _orderedRelics.Count; i++)
        {
            try { _orderedRelics[i].OnShopOpen(_ctx); }
            catch (Exception ex) { LogRelicError(_orderedRelics[i].Id, "OnShopOpen", ex); }
        }
    }

    public void DispatchSellSelf(string relicId)
    {
        var relic = GetRelicById(relicId);
        if (relic == null) return;

        try { relic.OnSellSelf(_ctx); }
        catch (Exception ex) { LogRelicError(relicId, "OnSellSelf", ex); }
    }

    // ════════════════════════════════════════════════════════════════════
    // Internal
    // ════════════════════════════════════════════════════════════════════

    private void SyncOwnedRelics()
    {
        _ctx.OwnedRelics.Clear();
        for (int i = 0; i < _orderedRelics.Count; i++)
        {
            _ctx.OwnedRelics.Add(_orderedRelics[i].Id);
        }
    }

    private static void LogRelicError(string relicId, string hook, Exception ex)
    {
        Debug.LogError($"[RelicManager] Exception in {relicId}.{hook}: {ex}");
    }
}
