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
