using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Context struct providing all round-start data needed by TipActivator.
/// Populated in TradingState.Enter() after EventScheduler.InitializeRound().
/// Story 18.2, AC 3.
/// </summary>
public struct TipActivationContext
{
    public StockInstance ActiveStock;
    public int ScheduledEventCount;
    public float[] ScheduledFireTimes;
    public float RoundDuration;
    public StockTierConfig TierConfig;
    public System.Random Random;
}

/// <summary>
/// Static utility class that computes TipOverlayData from purchased tips
/// using actual round-start data. Called once per round in TradingState.Enter().
/// Pure logic — no MonoBehaviour dependency.
/// Story 18.2, AC 2.
/// </summary>
public static class TipActivator
{
    public static List<TipOverlayData> ActivateTips(
        List<RevealedTip> purchasedTips,
        TipActivationContext ctx)
    {
        Debug.Assert(ctx.RoundDuration > 0f,
            "[TipActivator] RoundDuration must be positive");
        var overlays = new List<TipOverlayData>(purchasedTips.Count);
        for (int i = 0; i < purchasedTips.Count; i++)
        {
            var overlay = ComputeOverlay(purchasedTips[i], ctx);
            overlays.Add(overlay);
        }
        return overlays;
    }

    private static TipOverlayData ComputeOverlay(RevealedTip tip, TipActivationContext ctx)
    {
        switch (tip.Type)
        {
            case InsiderTipType.PriceFloor:
                return ComputePriceLineOverlay(tip, "FLOOR");
            case InsiderTipType.PriceCeiling:
                return ComputePriceLineOverlay(tip, "CEILING");
            case InsiderTipType.PriceForecast:
                return ComputePriceBandOverlay(tip, ctx);
            case InsiderTipType.EventCount:
                return ComputeEventCountOverlay(ctx);
            case InsiderTipType.DipMarker:
                return ComputeDipMarkerOverlay(ctx);
            case InsiderTipType.PeakMarker:
                return ComputePeakMarkerOverlay(ctx);
            case InsiderTipType.ClosingDirection:
                return ComputeClosingDirectionOverlay(ctx);
            case InsiderTipType.EventTiming:
                return ComputeEventTimingOverlay(ctx);
            case InsiderTipType.TrendReversal:
                return ComputeTrendReversalOverlay(ctx);
            default:
                return TipOverlayData.CreateDefault();
        }
    }

    // === Price-based overlays (AC 4) ===

    private static TipOverlayData ComputePriceLineOverlay(RevealedTip tip, string labelPrefix)
    {
        var overlay = TipOverlayData.CreateDefault();
        overlay.Type = tip.Type;
        overlay.Label = $"{labelPrefix} ~${tip.NumericValue:F2}";
        overlay.PriceLevel = tip.NumericValue;
        return overlay;
    }

    private static TipOverlayData ComputePriceBandOverlay(RevealedTip tip, TipActivationContext ctx)
    {
        float priceRange = ctx.TierConfig.MaxPrice - ctx.TierConfig.MinPrice;
        var overlay = TipOverlayData.CreateDefault();
        overlay.Type = tip.Type;
        overlay.Label = $"FORECAST ~${tip.NumericValue:F2}";
        overlay.BandCenter = tip.NumericValue;
        overlay.BandHalfWidth = priceRange * 0.12f;
        return overlay;
    }

    // === EventCount overlay (AC 5) ===

    private static TipOverlayData ComputeEventCountOverlay(TipActivationContext ctx)
    {
        var overlay = TipOverlayData.CreateDefault();
        overlay.Type = InsiderTipType.EventCount;
        overlay.Label = $"EVENTS: {ctx.ScheduledEventCount}";
        overlay.EventCountdown = ctx.ScheduledEventCount;
        return overlay;
    }

    // === DipMarker overlay (AC 6) ===

    private static TipOverlayData ComputeDipMarkerOverlay(TipActivationContext ctx)
    {
        float center = ctx.ActiveStock.TrendDirection switch
        {
            TrendDirection.Bull => 0.15f,
            TrendDirection.Bear => 0.85f,
            _ => 0.50f
        };
        float fuzz = ((float)ctx.Random.NextDouble() * 2f - 1f) * 0.05f;
        center = Mathf.Clamp(center + fuzz, 0.10f, 0.90f);

        var overlay = TipOverlayData.CreateDefault();
        overlay.Type = InsiderTipType.DipMarker;
        overlay.Label = "DIP ZONE";
        overlay.TimeZoneCenter = center;
        overlay.TimeZoneHalfWidth = 0.10f;
        return overlay;
    }

    // === PeakMarker overlay (AC 7) ===

    private static TipOverlayData ComputePeakMarkerOverlay(TipActivationContext ctx)
    {
        float center = ctx.ActiveStock.TrendDirection switch
        {
            TrendDirection.Bull => 0.85f,
            TrendDirection.Bear => 0.15f,
            _ => 0.50f
        };
        float fuzz = ((float)ctx.Random.NextDouble() * 2f - 1f) * 0.05f;
        center = Mathf.Clamp(center + fuzz, 0.10f, 0.90f);

        var overlay = TipOverlayData.CreateDefault();
        overlay.Type = InsiderTipType.PeakMarker;
        overlay.Label = "PEAK ZONE";
        overlay.TimeZoneCenter = center;
        overlay.TimeZoneHalfWidth = 0.10f;
        return overlay;
    }

    // === ClosingDirection overlay (AC 8) ===

    private static TipOverlayData ComputeClosingDirectionOverlay(TipActivationContext ctx)
    {
        int direction = ctx.ActiveStock.TrendDirection switch
        {
            TrendDirection.Bull => 1,
            TrendDirection.Bear => -1,
            _ => ctx.Random.NextDouble() < 0.5 ? 1 : -1
        };

        var overlay = TipOverlayData.CreateDefault();
        overlay.Type = InsiderTipType.ClosingDirection;
        overlay.Label = direction > 0 ? "CLOSING UP" : "CLOSING DOWN";
        overlay.DirectionSign = direction;
        return overlay;
    }

    // === EventTiming overlay (AC 9) ===

    private static TipOverlayData ComputeEventTimingOverlay(TipActivationContext ctx)
    {
        var overlay = TipOverlayData.CreateDefault();
        overlay.Type = InsiderTipType.EventTiming;

        if (ctx.ScheduledEventCount == 0 || ctx.ScheduledFireTimes == null)
        {
            overlay.Label = "NO EVENTS";
            overlay.TimeMarkers = System.Array.Empty<float>();
            return overlay;
        }

        float[] markers = new float[ctx.ScheduledEventCount];
        for (int i = 0; i < ctx.ScheduledEventCount; i++)
        {
            float normalized = ctx.ScheduledFireTimes[i] / ctx.RoundDuration;
            float fuzz = ((float)ctx.Random.NextDouble() * 2f - 1f) * 0.04f;
            markers[i] = Mathf.Clamp01(normalized + fuzz);
        }
        System.Array.Sort(markers);

        overlay.Label = "EVENT TIMING";
        overlay.TimeMarkers = markers;
        return overlay;
    }

    // === TrendReversal overlay (AC 10) ===

    private static TipOverlayData ComputeTrendReversalOverlay(TipActivationContext ctx)
    {
        var overlay = TipOverlayData.CreateDefault();
        overlay.Type = InsiderTipType.TrendReversal;

        if (ctx.ScheduledEventCount == 0 || ctx.ScheduledFireTimes == null
            || ctx.ActiveStock.TrendDirection == TrendDirection.Neutral)
        {
            overlay.Label = "NO REVERSAL";
            overlay.ReversalTime = -1f;
            return overlay;
        }

        bool isBull = ctx.ActiveStock.TrendDirection == TrendDirection.Bull;
        float searchStart = isBull ? 0.5f : 0.0f;
        float searchEnd = isBull ? 1.0f : 0.5f;
        float searchMid = (searchStart + searchEnd) / 2f;

        float bestTime = -1f;
        float bestDistance = float.MaxValue;
        int eventsInHalf = 0;

        for (int i = 0; i < ctx.ScheduledEventCount; i++)
        {
            float normalized = ctx.ScheduledFireTimes[i] / ctx.RoundDuration;
            if (normalized >= searchStart && normalized <= searchEnd)
            {
                eventsInHalf++;
                float distance = Mathf.Abs(normalized - searchMid);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTime = normalized;
                }
            }
        }

        if (eventsInHalf <= 1)
        {
            overlay.Label = "NO REVERSAL";
            overlay.ReversalTime = -1f;
            return overlay;
        }

        float fuzz = ((float)ctx.Random.NextDouble() * 2f - 1f) * 0.05f;
        bestTime = Mathf.Clamp01(bestTime + fuzz);

        overlay.Label = "REVERSAL";
        overlay.ReversalTime = bestTime;
        return overlay;
    }
}
