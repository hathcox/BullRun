using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Context struct providing all round-start data needed by TipActivator.
/// Populated in TradingState.Enter() after EventScheduler.InitializeRound().
/// Story 18.2, AC 3. Updated Story 18.6, AC 7: uses PreDecidedEvents.
/// </summary>
public struct TipActivationContext
{
    public StockInstance ActiveStock;
    public PreDecidedEvent[] PreDecidedEvents;
    public float RoundDuration;
    public StockTierConfig TierConfig;
    public System.Random Random;
}

/// <summary>
/// Story 18.6, AC 2: Simulation results for a full round price trajectory.
/// Computed analytically from trend + pre-decided events.
/// </summary>
public struct RoundSimulation
{
    public float MinPrice;
    public float MaxPrice;
    public float MinPriceNormalizedTime;
    public float MaxPriceNormalizedTime;
    public float ClosingPrice;
    public float AveragePrice;
}

/// <summary>
/// Static utility class that computes TipOverlayData from purchased tips
/// using actual round-start data. Called once per round in TradingState.Enter().
/// Pure logic — no MonoBehaviour dependency.
/// Story 18.2, AC 2. Rewritten Story 18.6: uses price simulation for accurate tips.
/// </summary>
public static class TipActivator
{
    /// <summary>
    /// Activates purchased tips using simulation-based accurate values.
    /// Story 18.6, AC 5: Also updates RevealedTip.DisplayText with real values.
    /// Returns overlays and modifies purchasedTips in-place for display text updates.
    /// </summary>
    public static List<TipOverlayData> ActivateTips(
        List<RevealedTip> purchasedTips,
        TipActivationContext ctx)
    {
        Debug.Assert(ctx.RoundDuration > 0f,
            "[TipActivator] RoundDuration must be positive");

        // Story 18.6, AC 2: Run price simulation for accurate tip values
        var simulation = SimulateRound(ctx);

        var overlays = new List<TipOverlayData>(purchasedTips.Count);
        for (int i = 0; i < purchasedTips.Count; i++)
        {
            var overlay = ComputeOverlay(purchasedTips[i], ctx, simulation);
            overlays.Add(overlay);

            // Story 18.6, AC 5: Update display text with accurate values
            var updatedTip = purchasedTips[i];
            UpdateDisplayText(ref updatedTip, simulation, ctx);
            purchasedTips[i] = updatedTip;
        }
        return overlays;
    }

    // === Price Simulation (AC 2) ===

    /// <summary>
    /// Story 18.6, AC 2: Analytically simulates the round's price trajectory.
    /// Story 18.7, AC 1-3: Models hold-phase drift via mean reversion to match runtime behavior.
    /// Computes price at each event boundary using compound trend + event effects + drift.
    /// Tracks global min/max/closing prices for accurate tip computation.
    /// </summary>
    public static RoundSimulation SimulateRound(TipActivationContext ctx)
    {
        float startingPrice = ctx.ActiveStock.StartingPrice;
        float trendRate = ctx.ActiveStock.TrendRate;
        bool isBull = ctx.ActiveStock.TrendDirection == TrendDirection.Bull;
        bool isBear = ctx.ActiveStock.TrendDirection == TrendDirection.Bear;
        float roundDuration = ctx.RoundDuration;
        float dynamicFloor = startingPrice * GameConfig.PriceFloorPercent;

        int eventCount = ctx.PreDecidedEvents != null ? ctx.PreDecidedEvents.Length : 0;

        // Defensive sort: ensure events are processed in chronological order
        if (eventCount > 1)
            System.Array.Sort(ctx.PreDecidedEvents, (a, b) => a.FireTime.CompareTo(b.FireTime));

        float globalMin = startingPrice;
        float globalMax = startingPrice;
        float globalMinTime = 0f;
        float globalMaxTime = 0f;

        float currentPrice = startingPrice;
        float trendLinePrice = startingPrice; // Story 18.7, AC 2: trend line tracking
        float currentTime = 0f;
        float weightedPriceSum = 0f;

        // TrendRate is already signed: positive for bull, negative for bear, zero for neutral
        // (set by StockInstance.Initialize based on TrendDirection)
        float signedTrendRate = trendRate;

        // Story 18.7, AC 1: Tier config for drift calculations
        float meanReversionSpeed = ctx.TierConfig.MeanReversionSpeed;
        float noiseAmplitude = ctx.TierConfig.NoiseAmplitude;

        for (int i = 0; i < eventCount; i++)
        {
            float eventTime = ctx.PreDecidedEvents[i].FireTime;

            // Apply compound trend from currentTime to eventTime
            float dt = eventTime - currentTime;
            if (dt > 0f)
            {
                float priceAtSegStart = currentPrice;
                currentPrice = currentPrice * Mathf.Pow(1f + signedTrendRate, dt);
                currentPrice = Mathf.Max(currentPrice, dynamicFloor);
                // AC 2: trend line grows with same compound rate between events
                trendLinePrice = trendLinePrice * Mathf.Pow(1f + signedTrendRate, dt);
                weightedPriceSum += (priceAtSegStart + currentPrice) / 2f * dt;

                // Track min/max within this trend segment
                TrackSegmentMinMax(priceAtSegStart, currentPrice, currentTime, eventTime, roundDuration,
                    isBull, isBear, ref globalMin, ref globalMax, ref globalMinTime, ref globalMaxTime);
            }

            // Apply event effect with hold-phase drift modeling (AC 1, AC 3)
            var preDecided = ctx.PreDecidedEvents[i];
            float priceBeforeEvent = currentPrice;
            if (preDecided.Phases != null && preDecided.Phases.Count >= 2)
            {
                // Multi-phase event
                if (preDecided.Config.EventType == MarketEventType.PumpAndDump)
                {
                    // Track pump peak as potential max (raw target = brief peak)
                    float pumpPeak = currentPrice * (1f + preDecided.PriceEffect);
                    pumpPeak = Mathf.Max(pumpPeak, dynamicFloor);
                    float pumpTime = eventTime + preDecided.Config.Duration * 0.6f;

                    if (pumpPeak > globalMax)
                    {
                        globalMax = pumpPeak;
                        globalMaxTime = pumpTime;
                    }

                    // Post-dump raw target: 80% of pre-event price
                    float rawDumpTarget = currentPrice * 0.80f;
                    rawDumpTarget = Mathf.Max(rawDumpTarget, dynamicFloor);
                    float endTime = eventTime + preDecided.Config.Duration;

                    if (rawDumpTarget < globalMin)
                    {
                        globalMin = rawDumpTarget;
                        globalMinTime = endTime;
                    }

                    // AC 3: Apply drift to dump phase
                    float driftedDumpPrice = ApplyHoldPhaseDrift(
                        rawDumpTarget, trendLinePrice,
                        preDecided.Config.Duration * 0.40f, // dump = 40% of event duration
                        meanReversionSpeed, noiseAmplitude, dynamicFloor);

                    currentPrice = driftedDumpPrice;
                    trendLinePrice = driftedDumpPrice; // AC 2: re-anchor

                    weightedPriceSum += (priceBeforeEvent + currentPrice) / 2f * preDecided.Config.Duration;
                }
                else if (preDecided.Config.EventType == MarketEventType.FlashCrash)
                {
                    // Track crash bottom as potential min (raw target = brief min)
                    float crashBottom = currentPrice * (1f + preDecided.PriceEffect);
                    crashBottom = Mathf.Max(crashBottom, dynamicFloor);
                    float crashTime = eventTime + preDecided.Config.Duration * 0.4f;

                    if (crashBottom < globalMin)
                    {
                        globalMin = crashBottom;
                        globalMinTime = crashTime;
                    }

                    // Post-recovery raw target: 95% of pre-event price
                    float rawRecoveryTarget = currentPrice * 0.95f;
                    rawRecoveryTarget = Mathf.Max(rawRecoveryTarget, dynamicFloor);
                    float endTime = eventTime + preDecided.Config.Duration;

                    if (rawRecoveryTarget > globalMax)
                    {
                        globalMax = rawRecoveryTarget;
                        globalMaxTime = endTime;
                    }

                    // AC 3: Apply drift to recovery phase
                    float driftedRecoveryPrice = ApplyHoldPhaseDrift(
                        rawRecoveryTarget, trendLinePrice,
                        preDecided.Config.Duration * 0.60f, // recovery = 60% of event duration
                        meanReversionSpeed, noiseAmplitude, dynamicFloor);

                    currentPrice = driftedRecoveryPrice;
                    trendLinePrice = driftedRecoveryPrice; // AC 2: re-anchor

                    weightedPriceSum += (priceBeforeEvent + currentPrice) / 2f * preDecided.Config.Duration;
                }
            }
            else
            {
                // Single-phase event: apply price effect with drift
                float rawTarget = currentPrice * (1f + preDecided.PriceEffect);
                rawTarget = Mathf.Max(rawTarget, dynamicFloor);

                // Track raw target as brief peak for max/min (captured during ramp-to-hold)
                if (rawTarget > globalMax)
                {
                    globalMax = rawTarget;
                    globalMaxTime = eventTime;
                }
                if (rawTarget < globalMin)
                {
                    globalMin = rawTarget;
                    globalMinTime = eventTime;
                }

                // AC 1: Apply hold-phase drift
                float driftedPrice = ApplyHoldPhaseDrift(
                    rawTarget, trendLinePrice,
                    preDecided.Config.Duration,
                    meanReversionSpeed, noiseAmplitude, dynamicFloor);

                currentPrice = driftedPrice;
                trendLinePrice = driftedPrice; // AC 2: re-anchor

                weightedPriceSum += (priceBeforeEvent + currentPrice) / 2f * preDecided.Config.Duration;
            }

            currentTime = eventTime + preDecided.Config.Duration;
        }

        // Apply trend from last event to round end
        float remainingDt = roundDuration - currentTime;
        if (remainingDt > 0f)
        {
            float priceAtSegStart = currentPrice;
            currentPrice = currentPrice * Mathf.Pow(1f + signedTrendRate, remainingDt);
            currentPrice = Mathf.Max(currentPrice, dynamicFloor);
            weightedPriceSum += (priceAtSegStart + currentPrice) / 2f * remainingDt;

            TrackSegmentMinMax(priceAtSegStart, currentPrice, currentTime, roundDuration, roundDuration,
                isBull, isBear, ref globalMin, ref globalMax, ref globalMinTime, ref globalMaxTime);
        }

        float closingPrice = currentPrice;

        return new RoundSimulation
        {
            MinPrice = globalMin,
            MaxPrice = globalMax,
            MinPriceNormalizedTime = globalMinTime / roundDuration,
            MaxPriceNormalizedTime = globalMaxTime / roundDuration,
            ClosingPrice = closingPrice,
            AveragePrice = roundDuration > 0f ? weightedPriceSum / roundDuration : startingPrice
        };
    }

    /// <summary>
    /// Story 18.7, AC 1: Computes hold-phase drift for an event target price.
    /// Models the runtime behavior where SegmentSlope * deltaTime * 0.3f drifts
    /// the event target toward the trend line during the hold phase.
    /// </summary>
    private static float ApplyHoldPhaseDrift(
        float rawTarget, float trendLinePrice, float phaseDuration,
        float meanReversionSpeed, float noiseAmplitude, float dynamicFloor)
    {
        // Hold duration: ~70% of phase duration (15% ramp + 70% hold + 15% tail)
        float holdDuration = phaseDuration * 0.70f;

        // Deviation from trend line
        float deviation = (rawTarget - trendLinePrice) / trendLinePrice;

        // Mean reversion force (matches runtime: deviation * price * MRS)
        // For negative events, deviation is negative → force is negative → drift pulls UP
        // The force is self-limiting for negative events (rawTarget is small when deviation is large)
        float reversionForce = deviation * rawTarget * meanReversionSpeed;

        // Cap at runtime limit (matches runtime: NoiseAmplitude * price * 2.0)
        float maxReversion = noiseAmplitude * rawTarget * 2.0f;

        // Drift factor 0.3 (matches runtime's SegmentSlope * deltaTime * 0.3f)
        float effectiveDrift = Mathf.Min(reversionForce, maxReversion) * 0.3f * holdDuration;

        // Drifted post-event price (subtract drift: positive deviation → lower price)
        float driftedPrice = rawTarget - effectiveDrift;
        return Mathf.Max(driftedPrice, dynamicFloor);
    }

    /// <summary>
    /// Tracks min/max within a trend segment between two time points.
    /// Bull trend → min at segment start, max at segment end.
    /// Bear trend → max at segment start, min at segment end.
    /// Neutral → check both endpoints.
    /// </summary>
    private static void TrackSegmentMinMax(
        float priceStart, float priceEnd, float timeStart, float timeEnd, float roundDuration,
        bool isBull, bool isBear,
        ref float globalMin, ref float globalMax, ref float globalMinTime, ref float globalMaxTime)
    {
        if (isBull)
        {
            // Price rises: min at start, max at end
            if (priceStart < globalMin) { globalMin = priceStart; globalMinTime = timeStart; }
            if (priceEnd > globalMax) { globalMax = priceEnd; globalMaxTime = timeEnd; }
        }
        else if (isBear)
        {
            // Price falls: max at start, min at end
            if (priceStart > globalMax) { globalMax = priceStart; globalMaxTime = timeStart; }
            if (priceEnd < globalMin) { globalMin = priceEnd; globalMinTime = timeEnd; }
        }
        else
        {
            // Neutral: check both
            if (priceStart < globalMin) { globalMin = priceStart; globalMinTime = timeStart; }
            if (priceStart > globalMax) { globalMax = priceStart; globalMaxTime = timeStart; }
            if (priceEnd < globalMin) { globalMin = priceEnd; globalMinTime = timeEnd; }
            if (priceEnd > globalMax) { globalMax = priceEnd; globalMaxTime = timeEnd; }
        }
    }

    // === Overlay Computation (AC 3) ===

    private static TipOverlayData ComputeOverlay(RevealedTip tip, TipActivationContext ctx, RoundSimulation sim)
    {
        switch (tip.Type)
        {
            case InsiderTipType.PriceFloor:
                return ComputePriceFloorOverlay(sim);
            case InsiderTipType.PriceCeiling:
                return ComputePriceCeilingOverlay(sim);
            case InsiderTipType.PriceForecast:
                return ComputePriceForecastOverlay(sim);
            case InsiderTipType.EventCount:
                return ComputeEventCountOverlay(ctx);
            case InsiderTipType.DipMarker:
                return ComputeDipMarkerOverlay(sim);
            case InsiderTipType.PeakMarker:
                return ComputePeakMarkerOverlay(sim);
            case InsiderTipType.ClosingDirection:
                return ComputeClosingDirectionOverlay(sim, ctx);
            case InsiderTipType.EventTiming:
                return ComputeEventTimingOverlay(ctx);
            case InsiderTipType.TrendReversal:
                return ComputeTrendReversalOverlay(ctx, sim);
            default:
                return TipOverlayData.CreateDefault();
        }
    }

    // === PriceFloor: uses simulation.MinPrice (AC 3) ===

    private static TipOverlayData ComputePriceFloorOverlay(RoundSimulation sim)
    {
        var overlay = TipOverlayData.CreateDefault();
        overlay.Type = InsiderTipType.PriceFloor;
        overlay.Label = $"FLOOR ~${sim.MinPrice:F2}";
        overlay.PriceLevel = sim.MinPrice;
        return overlay;
    }

    // === PriceCeiling: uses simulation.MaxPrice (AC 3) ===

    private static TipOverlayData ComputePriceCeilingOverlay(RoundSimulation sim)
    {
        var overlay = TipOverlayData.CreateDefault();
        overlay.Type = InsiderTipType.PriceCeiling;
        overlay.Label = $"CEILING ~${sim.MaxPrice:F2}";
        overlay.PriceLevel = sim.MaxPrice;
        return overlay;
    }

    // === PriceForecast: uses simulation.AveragePrice with band (AC 3) ===

    private static TipOverlayData ComputePriceForecastOverlay(RoundSimulation sim)
    {
        float bandHalfWidth = (sim.MaxPrice - sim.MinPrice) * 0.15f;
        var overlay = TipOverlayData.CreateDefault();
        overlay.Type = InsiderTipType.PriceForecast;
        overlay.Label = $"FORECAST ~${sim.AveragePrice:F2}";
        overlay.BandCenter = sim.AveragePrice;
        overlay.BandHalfWidth = bandHalfWidth;
        return overlay;
    }

    // === EventCount: uses PreDecidedEvents.Length (AC 3) ===

    private static TipOverlayData ComputeEventCountOverlay(TipActivationContext ctx)
    {
        int count = ctx.PreDecidedEvents != null ? ctx.PreDecidedEvents.Length : 0;
        var overlay = TipOverlayData.CreateDefault();
        overlay.Type = InsiderTipType.EventCount;
        overlay.Label = $"EVENTS: {count}";
        overlay.EventCountdown = count;
        return overlay;
    }

    // === DipMarker: time zone centered on simulation.MinPriceNormalizedTime (AC 3) ===

    private static TipOverlayData ComputeDipMarkerOverlay(RoundSimulation sim)
    {
        float center = Mathf.Clamp(sim.MinPriceNormalizedTime, 0.10f, 0.90f);
        var overlay = TipOverlayData.CreateDefault();
        overlay.Type = InsiderTipType.DipMarker;
        overlay.Label = "DIP ZONE";
        overlay.TimeZoneCenter = center;
        overlay.TimeZoneHalfWidth = 0.10f;
        return overlay;
    }

    // === PeakMarker: time zone centered on simulation.MaxPriceNormalizedTime (AC 3) ===

    private static TipOverlayData ComputePeakMarkerOverlay(RoundSimulation sim)
    {
        float center = Mathf.Clamp(sim.MaxPriceNormalizedTime, 0.10f, 0.90f);
        var overlay = TipOverlayData.CreateDefault();
        overlay.Type = InsiderTipType.PeakMarker;
        overlay.Label = "PEAK ZONE";
        overlay.TimeZoneCenter = center;
        overlay.TimeZoneHalfWidth = 0.10f;
        return overlay;
    }

    // === ClosingDirection: sign(simulation.ClosingPrice - startingPrice) (AC 3) ===

    private static TipOverlayData ComputeClosingDirectionOverlay(RoundSimulation sim, TipActivationContext ctx)
    {
        int direction = sim.ClosingPrice >= ctx.ActiveStock.StartingPrice ? 1 : -1;
        var overlay = TipOverlayData.CreateDefault();
        overlay.Type = InsiderTipType.ClosingDirection;
        overlay.Label = direction > 0 ? "CLOSING UP" : "CLOSING DOWN";
        overlay.DirectionSign = direction;
        return overlay;
    }

    // === EventTiming: exact fire times from PreDecidedEvents — no fuzz (AC 3, AC 6) ===

    private static TipOverlayData ComputeEventTimingOverlay(TipActivationContext ctx)
    {
        var overlay = TipOverlayData.CreateDefault();
        overlay.Type = InsiderTipType.EventTiming;

        int eventCount = ctx.PreDecidedEvents != null ? ctx.PreDecidedEvents.Length : 0;
        if (eventCount == 0)
        {
            overlay.Label = "NO EVENTS";
            overlay.TimeMarkers = System.Array.Empty<float>();
            return overlay;
        }

        float[] markers = new float[eventCount];
        for (int i = 0; i < eventCount; i++)
        {
            // Story 18.6, AC 6: No fuzz — exact fire times
            markers[i] = Mathf.Clamp01(ctx.PreDecidedEvents[i].FireTime / ctx.RoundDuration);
        }
        System.Array.Sort(markers);

        overlay.Label = "EVENT TIMING";
        overlay.TimeMarkers = markers;
        return overlay;
    }

    // === TrendReversal: detect where simulated price changes direction (AC 3) ===

    private static TipOverlayData ComputeTrendReversalOverlay(TipActivationContext ctx, RoundSimulation sim)
    {
        var overlay = TipOverlayData.CreateDefault();
        overlay.Type = InsiderTipType.TrendReversal;

        int eventCount = ctx.PreDecidedEvents != null ? ctx.PreDecidedEvents.Length : 0;
        if (eventCount == 0 || ctx.ActiveStock.TrendDirection == TrendDirection.Neutral)
        {
            overlay.Label = "NO REVERSAL";
            overlay.ReversalTime = -1f;
            return overlay;
        }

        bool isBull = ctx.ActiveStock.TrendDirection == TrendDirection.Bull;
        float startingPrice = ctx.ActiveStock.StartingPrice;
        float trendRate = ctx.ActiveStock.TrendRate;
        // TrendRate is already signed (negative for bear, positive for bull)
        float signedTrendRate = trendRate;
        float dynamicFloor = startingPrice * GameConfig.PriceFloorPercent;
        float meanReversionSpeed = ctx.TierConfig.MeanReversionSpeed;
        float noiseAmplitude = ctx.TierConfig.NoiseAmplitude;

        // Walk through the price trajectory at event boundaries to find direction changes
        // Uses drift modeling consistent with SimulateRound (H2 fix)
        float currentPrice = startingPrice;
        float trendLinePrice = startingPrice;
        float currentTime = 0f;
        bool priceRising = isBull; // Initial direction matches trend
        float reversalTime = -1f;

        for (int i = 0; i < eventCount; i++)
        {
            float eventTime = ctx.PreDecidedEvents[i].FireTime;
            var preDecided = ctx.PreDecidedEvents[i];

            // Price before event (after trend)
            float dt = eventTime - currentTime;
            if (dt > 0f)
            {
                currentPrice = currentPrice * Mathf.Pow(1f + signedTrendRate, dt);
                currentPrice = Mathf.Max(currentPrice, dynamicFloor);
                trendLinePrice = trendLinePrice * Mathf.Pow(1f + signedTrendRate, dt);
            }

            float priceBeforeEvent = currentPrice;

            // Apply event effect with drift modeling (matches SimulateRound)
            if (preDecided.Phases != null && preDecided.Phases.Count >= 2)
            {
                if (preDecided.Config.EventType == MarketEventType.PumpAndDump)
                {
                    float rawDumpTarget = priceBeforeEvent * 0.80f;
                    rawDumpTarget = Mathf.Max(rawDumpTarget, dynamicFloor);
                    currentPrice = ApplyHoldPhaseDrift(rawDumpTarget, trendLinePrice,
                        preDecided.Config.Duration * 0.40f, meanReversionSpeed, noiseAmplitude, dynamicFloor);
                }
                else if (preDecided.Config.EventType == MarketEventType.FlashCrash)
                {
                    float rawRecoveryTarget = priceBeforeEvent * 0.95f;
                    rawRecoveryTarget = Mathf.Max(rawRecoveryTarget, dynamicFloor);
                    currentPrice = ApplyHoldPhaseDrift(rawRecoveryTarget, trendLinePrice,
                        preDecided.Config.Duration * 0.60f, meanReversionSpeed, noiseAmplitude, dynamicFloor);
                }
                else
                {
                    float rawTarget = priceBeforeEvent * (1f + preDecided.PriceEffect);
                    rawTarget = Mathf.Max(rawTarget, dynamicFloor);
                    currentPrice = ApplyHoldPhaseDrift(rawTarget, trendLinePrice,
                        preDecided.Config.Duration, meanReversionSpeed, noiseAmplitude, dynamicFloor);
                }
            }
            else
            {
                float rawTarget = priceBeforeEvent * (1f + preDecided.PriceEffect);
                rawTarget = Mathf.Max(rawTarget, dynamicFloor);
                currentPrice = ApplyHoldPhaseDrift(rawTarget, trendLinePrice,
                    preDecided.Config.Duration, meanReversionSpeed, noiseAmplitude, dynamicFloor);
            }

            trendLinePrice = currentPrice; // Re-anchor after event

            // Check if direction changed significantly (>15% price move to filter noise-masked events)
            float changePercent = Mathf.Abs(currentPrice - priceBeforeEvent) / priceBeforeEvent;
            if (changePercent > 0.15f)
            {
                bool nowRising = currentPrice > priceBeforeEvent;
                if (nowRising != priceRising)
                {
                    reversalTime = eventTime / ctx.RoundDuration;
                    priceRising = nowRising;
                }
            }

            currentTime = eventTime + preDecided.Config.Duration;
        }

        if (reversalTime < 0f)
        {
            overlay.Label = "NO REVERSAL";
            overlay.ReversalTime = -1f;
            return overlay;
        }

        overlay.Label = "REVERSAL";
        overlay.ReversalTime = Mathf.Clamp01(reversalTime);
        return overlay;
    }

    // === Display Text Update (AC 5) ===

    /// <summary>
    /// Story 18.6, AC 5: Updates RevealedTip.DisplayText with accurate simulation values.
    /// Called at round start after simulation completes.
    /// </summary>
    private static void UpdateDisplayText(ref RevealedTip tip, RoundSimulation sim, TipActivationContext ctx)
    {
        switch (tip.Type)
        {
            case InsiderTipType.PriceCeiling:
                tip.DisplayText = $"Ceiling ~${sim.MaxPrice:F2} \u2014 marked on chart";
                tip.NumericValue = sim.MaxPrice;
                break;
            case InsiderTipType.PriceFloor:
                tip.DisplayText = $"Floor ~${sim.MinPrice:F2} \u2014 marked on chart";
                tip.NumericValue = sim.MinPrice;
                break;
            case InsiderTipType.PriceForecast:
                tip.DisplayText = $"Sweet spot ~${sim.AveragePrice:F2} \u2014 marked on chart";
                tip.NumericValue = sim.AveragePrice;
                break;
            case InsiderTipType.ClosingDirection:
            {
                bool closesHigher = sim.ClosingPrice >= ctx.ActiveStock.StartingPrice;
                tip.DisplayText = closesHigher ? "Round closes HIGHER" : "Round closes LOWER";
                break;
            }
        }
    }
}
