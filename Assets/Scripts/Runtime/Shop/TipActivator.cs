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
    public int NoiseSeed;
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
    public float ReversalNormalizedTime;
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

    // === Price Simulation via Deterministic Frame-by-Frame Replay ===

    /// <summary>
    /// Replays the round's price trajectory frame-by-frame using the actual
    /// PriceGenerator + EventEffects code path with a synchronized noise RNG seed.
    /// Produces identical prices to the real game when both use the same NoiseSeed.
    /// </summary>
    public static RoundSimulation SimulateRound(TipActivationContext ctx)
    {
        float startingPrice = ctx.ActiveStock.StartingPrice;
        float roundDuration = ctx.RoundDuration;
        int eventCount = ctx.PreDecidedEvents != null ? ctx.PreDecidedEvents.Length : 0;
        bool isBull = ctx.ActiveStock.TrendDirection == TrendDirection.Bull;

        // Defensive sort: ensure events are processed in chronological order
        if (eventCount > 1)
            System.Array.Sort(ctx.PreDecidedEvents, (a, b) => a.FireTime.CompareTo(b.FireTime));

        // Clone stock with same parameters
        var stock = new StockInstance();
        stock.Initialize(0, ctx.ActiveStock.TickerSymbol, ctx.ActiveStock.Tier,
            startingPrice, ctx.ActiveStock.TrendDirection,
            System.Math.Abs(ctx.ActiveStock.TrendRate), ctx.ActiveStock.Sector);

        // Create silent PriceGenerator with matching noise seed
        var priceGen = new PriceGenerator(new System.Random(ctx.NoiseSeed));
        priceGen.SetNoiseSeed(ctx.NoiseSeed);
        priceGen.SilentMode = true;

        // Create silent EventEffects
        var eventEffects = new EventEffects();
        eventEffects.SilentMode = true;
        var stocks = new List<StockInstance> { stock };
        eventEffects.SetActiveStocks(stocks);
        priceGen.SetEventEffects(eventEffects);

        // Replay state
        float dt = GameConfig.PriceStepSeconds;
        float totalTime = 0f;
        float globalMin = startingPrice;
        float globalMax = startingPrice;
        float globalMinTime = 0f;
        float globalMaxTime = 0f;
        float weightedPriceSum = 0f;
        float totalWeightedTime = 0f;

        // Event firing state
        bool[] eventFiredSlots = new bool[eventCount];

        // Reversal tracking
        float[] priceBeforeEvent = new float[eventCount];
        float[] eventEndTimes = new float[eventCount];
        bool[] eventEndProcessed = new bool[eventCount];
        bool priceRising = isBull;
        float reversalNormTime = -1f;

        for (int i = 0; i < eventCount; i++)
            eventEndTimes[i] = ctx.PreDecidedEvents[i].FireTime + ctx.PreDecidedEvents[i].Config.Duration;

        // Frame-by-frame replay mirroring TradingState.AdvanceTime exactly.
        // Uses accumulated elapsedSinceFreeze (not subtraction) to match TradingState's
        // _elapsedSinceFreeze field and avoid floating point discrepancies in event firing.
        float elapsedSinceFreeze = 0f;

        while (totalTime < roundDuration)
        {
            bool frozen = totalTime < GameConfig.PriceFreezeSeconds;

            if (!frozen)
            {
                // Fire pre-decided events (mirrors EventScheduler.Update)
                for (int i = 0; i < eventCount; i++)
                {
                    if (!eventFiredSlots[i] && elapsedSinceFreeze >= ctx.PreDecidedEvents[i].FireTime)
                    {
                        eventFiredSlots[i] = true;
                        priceBeforeEvent[i] = stock.CurrentPrice;

                        var pd = ctx.PreDecidedEvents[i];
                        MarketEvent mevt;
                        if (pd.Phases != null)
                            mevt = new MarketEvent(pd.Config.EventType, 0, pd.PriceEffect, pd.Config.Duration, pd.Phases);
                        else
                            mevt = new MarketEvent(pd.Config.EventType, 0, pd.PriceEffect, pd.Config.Duration);
                        eventEffects.StartEvent(mevt);
                    }
                }

                // Update active events then price (mirrors TradingState order)
                eventEffects.UpdateActiveEvents(dt);
                priceGen.UpdatePrice(stock, dt);

                // Reversal detection at event completion
                if (ctx.ActiveStock.TrendDirection != TrendDirection.Neutral)
                {
                    for (int i = 0; i < eventCount; i++)
                    {
                        if (eventFiredSlots[i] && !eventEndProcessed[i] && elapsedSinceFreeze >= eventEndTimes[i])
                        {
                            eventEndProcessed[i] = true;
                            float changePercent = System.Math.Abs(stock.CurrentPrice - priceBeforeEvent[i]) / priceBeforeEvent[i];
                            if (changePercent > 0.15f)
                            {
                                bool nowRising = stock.CurrentPrice > priceBeforeEvent[i];
                                if (nowRising != priceRising && reversalNormTime < 0f)
                                {
                                    reversalNormTime = ctx.PreDecidedEvents[i].FireTime / roundDuration;
                                    priceRising = nowRising;
                                }
                            }
                        }
                    }
                }

                elapsedSinceFreeze += dt;
            }

            // Track min/max/avg every step
            float price = stock.CurrentPrice;
            if (price < globalMin) { globalMin = price; globalMinTime = totalTime; }
            if (price > globalMax) { globalMax = price; globalMaxTime = totalTime; }
            weightedPriceSum += price * dt;
            totalWeightedTime += dt;

            totalTime += dt;
        }

        return new RoundSimulation
        {
            MinPrice = globalMin,
            MaxPrice = globalMax,
            MinPriceNormalizedTime = globalMinTime / roundDuration,
            MaxPriceNormalizedTime = globalMaxTime / roundDuration,
            ClosingPrice = stock.CurrentPrice,
            AveragePrice = totalWeightedTime > 0f ? weightedPriceSum / totalWeightedTime : startingPrice,
            ReversalNormalizedTime = reversalNormTime
        };
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

        // Compute absolute fire times (from round start) for next-event countdown
        if (count > 0)
        {
            overlay.EventFireTimes = new float[count];
            for (int i = 0; i < count; i++)
                overlay.EventFireTimes[i] = ctx.PreDecidedEvents[i].FireTime + GameConfig.PriceFreezeSeconds;
            System.Array.Sort(overlay.EventFireTimes);
        }

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

    // === TrendReversal: read from simulation replay result (AC 3) ===

    private static TipOverlayData ComputeTrendReversalOverlay(TipActivationContext ctx, RoundSimulation sim)
    {
        var overlay = TipOverlayData.CreateDefault();
        overlay.Type = InsiderTipType.TrendReversal;

        if (sim.ReversalNormalizedTime < 0f)
        {
            overlay.Label = "NO REVERSAL";
            overlay.ReversalTime = -1f;
            return overlay;
        }

        overlay.Label = "REVERSAL";
        overlay.ReversalTime = Mathf.Clamp01(sim.ReversalNormalizedTime);
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
