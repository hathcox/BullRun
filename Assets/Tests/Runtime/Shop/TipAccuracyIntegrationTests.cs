using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace BullRun.Tests.Shop
{
    /// <summary>
    /// Story 18.7, AC 6: Integration tests comparing SimulateRound() predictions
    /// against actual PriceGenerator + EventScheduler frame-by-frame output.
    /// EditMode tests — PriceGenerator and EventScheduler are pure C#.
    /// </summary>
    [TestFixture]
    public class TipAccuracyIntegrationTests
    {
        private struct ActualRoundResult
        {
            public float MinPrice;
            public float MaxPrice;
            public float MinPriceNormalizedTime;
            public float MaxPriceNormalizedTime;
            public float ClosingPrice;
            public float TimeWeightedAverage;
        }

        [SetUp]
        public void SetUp() { EventBus.Clear(); }

        [TearDown]
        public void TearDown() { EventBus.Clear(); }

        // === Test Infrastructure ===

        /// <summary>
        /// Runs a full round frame-by-frame, mirroring TradingState.AdvanceTime().
        /// Tracks actual min/max/close prices and their times.
        /// </summary>
        private ActualRoundResult RunFullRound(
            StockInstance stock, EventScheduler scheduler, PriceGenerator priceGen,
            float roundDuration, float fixedDeltaTime, StockTier tier)
        {
            float totalTime = 0f;
            float elapsedSinceFreeze = 0f;
            float minPrice = float.MaxValue;
            float maxPrice = float.MinValue;
            float minPriceTime = 0f;
            float maxPriceTime = 0f;
            float weightedPriceSum = 0f;
            float totalWeightedTime = 0f;
            var stocks = new List<StockInstance> { stock };

            while (totalTime < roundDuration)
            {
                bool frozen = totalTime < GameConfig.PriceFreezeSeconds;

                if (!frozen)
                {
                    scheduler.Update(elapsedSinceFreeze, fixedDeltaTime, stocks, tier);
                    priceGen.UpdatePrice(stock, fixedDeltaTime);
                    elapsedSinceFreeze += fixedDeltaTime;
                }

                float price = stock.CurrentPrice;
                if (price < minPrice) { minPrice = price; minPriceTime = totalTime; }
                if (price > maxPrice) { maxPrice = price; maxPriceTime = totalTime; }
                weightedPriceSum += price * fixedDeltaTime;
                totalWeightedTime += fixedDeltaTime;

                totalTime += fixedDeltaTime;
            }

            return new ActualRoundResult
            {
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                MinPriceNormalizedTime = minPriceTime / roundDuration,
                MaxPriceNormalizedTime = maxPriceTime / roundDuration,
                ClosingPrice = stock.CurrentPrice,
                TimeWeightedAverage = totalWeightedTime > 0f
                    ? weightedPriceSum / totalWeightedTime
                    : stock.StartingPrice
            };
        }

        /// <summary>
        /// Creates a TipActivationContext from the same stock/scheduler used for the actual run.
        /// Must be called BEFORE RunFullRound (stock state is preserved in readonly fields).
        /// </summary>
        private TipActivationContext BuildTestContext(
            StockInstance stock, EventScheduler scheduler, float roundDuration, int noiseSeed)
        {
            return new TipActivationContext
            {
                ActiveStock = stock,
                PreDecidedEvents = scheduler.PreDecidedEvents,
                RoundDuration = roundDuration,
                TierConfig = StockTierData.GetTierConfig(stock.Tier),
                Random = new System.Random(42),
                NoiseSeed = noiseSeed
            };
        }

        /// <summary>
        /// Sets up a complete round with stock, scheduler, and price generator.
        /// Uses separate random seeds for events vs noise to avoid correlation.
        /// Returns noiseSeed for deterministic tip replay matching.
        /// </summary>
        private (StockInstance stock, EventScheduler scheduler, PriceGenerator priceGen, int noiseSeed) SetupRound(
            StockTier tier, float startingPrice, TrendDirection trend, float trendRate,
            int seed, float roundDuration = 60f)
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", tier, startingPrice, trend, trendRate);
            var stocks = new List<StockInstance> { stock };

            var eventEffects = new EventEffects();
            eventEffects.SetActiveStocks(stocks);

            var scheduler = new EventScheduler(eventEffects, new System.Random(seed));
            scheduler.InitializeRound(1, 1, tier, stocks, roundDuration);

            int noiseSeed = seed + 5000;
            var priceGen = new PriceGenerator(new System.Random(seed + 1000));
            priceGen.SetNoiseSeed(noiseSeed);
            priceGen.SetEventEffects(eventEffects);

            return (stock, scheduler, priceGen, noiseSeed);
        }

        // === PriceCeiling accuracy (near-zero tolerance with deterministic replay) ===

        [Test]
        public void PriceCeiling_Accuracy_BullWithEvents([Values(42, 99, 200, 777, 1234)] int seed)
        {
            var (stock, scheduler, priceGen, noiseSeed) = SetupRound(
                StockTier.Penny, 6f, TrendDirection.Bull, 0.015f, seed);

            var ctx = BuildTestContext(stock, scheduler, 60f, noiseSeed);
            var sim = TipActivator.SimulateRound(ctx);
            var actual = RunFullRound(stock, scheduler, priceGen, 60f, 1f / 60f, StockTier.Penny);

            float tolerance = actual.MaxPrice * 0.001f;
            Assert.AreEqual(actual.MaxPrice, sim.MaxPrice, tolerance,
                $"[Seed {seed}] Ceiling sim={sim.MaxPrice:F2} vs actual={actual.MaxPrice:F2}, " +
                $"tolerance=±{tolerance:F4} ({scheduler.ScheduledEventCount} events)");
        }

        [Test]
        public void PriceCeiling_Accuracy_BearWithEvents([Values(55, 123, 456)] int seed)
        {
            var (stock, scheduler, priceGen, noiseSeed) = SetupRound(
                StockTier.MidValue, 100f, TrendDirection.Bear, 0.005f, seed);

            var ctx = BuildTestContext(stock, scheduler, 60f, noiseSeed);
            var sim = TipActivator.SimulateRound(ctx);
            var actual = RunFullRound(stock, scheduler, priceGen, 60f, 1f / 60f, StockTier.MidValue);

            float tolerance = actual.MaxPrice * 0.001f;
            Assert.AreEqual(actual.MaxPrice, sim.MaxPrice, tolerance,
                $"[Seed {seed}] Ceiling sim={sim.MaxPrice:F2} vs actual={actual.MaxPrice:F2}");
        }

        // === PriceFloor accuracy (near-zero tolerance) ===

        [Test]
        public void PriceFloor_Accuracy_BearWithEvents([Values(42, 88, 300, 555, 999)] int seed)
        {
            var (stock, scheduler, priceGen, noiseSeed) = SetupRound(
                StockTier.LowValue, 20f, TrendDirection.Bear, 0.008f, seed);

            var ctx = BuildTestContext(stock, scheduler, 60f, noiseSeed);
            var sim = TipActivator.SimulateRound(ctx);
            var actual = RunFullRound(stock, scheduler, priceGen, 60f, 1f / 60f, StockTier.LowValue);

            float tolerance = Mathf.Max(actual.MinPrice * 0.001f, 0.01f);
            Assert.AreEqual(actual.MinPrice, sim.MinPrice, tolerance,
                $"[Seed {seed}] Floor sim={sim.MinPrice:F2} vs actual={actual.MinPrice:F2}");
        }

        [Test]
        public void PriceFloor_Accuracy_BullWithEvents([Values(77, 150, 400)] int seed)
        {
            var (stock, scheduler, priceGen, noiseSeed) = SetupRound(
                StockTier.Penny, 7f, TrendDirection.Bull, 0.012f, seed);

            var ctx = BuildTestContext(stock, scheduler, 60f, noiseSeed);
            var sim = TipActivator.SimulateRound(ctx);
            var actual = RunFullRound(stock, scheduler, priceGen, 60f, 1f / 60f, StockTier.Penny);

            float tolerance = Mathf.Max(actual.MinPrice * 0.001f, 0.01f);
            Assert.AreEqual(actual.MinPrice, sim.MinPrice, tolerance,
                $"[Seed {seed}] Floor sim={sim.MinPrice:F2} vs actual={actual.MinPrice:F2}");
        }

        // === PriceForecast accuracy (near-zero tolerance) ===

        [Test]
        public void PriceForecast_Accuracy([Values(42, 100, 250, 600, 888)] int seed)
        {
            var (stock, scheduler, priceGen, noiseSeed) = SetupRound(
                StockTier.Penny, 6f, TrendDirection.Bull, 0.015f, seed);

            var ctx = BuildTestContext(stock, scheduler, 60f, noiseSeed);
            var sim = TipActivator.SimulateRound(ctx);
            var actual = RunFullRound(stock, scheduler, priceGen, 60f, 1f / 60f, StockTier.Penny);

            float tolerance = actual.TimeWeightedAverage * 0.001f;
            Assert.AreEqual(actual.TimeWeightedAverage, sim.AveragePrice, tolerance,
                $"[Seed {seed}] Forecast sim={sim.AveragePrice:F2} vs actual={actual.TimeWeightedAverage:F2}");
        }

        // === DipMarker timing (near-zero tolerance) ===

        [Test]
        public void DipMarker_Timing_Accuracy([Values(42, 99, 200, 500, 750)] int seed)
        {
            var (stock, scheduler, priceGen, noiseSeed) = SetupRound(
                StockTier.LowValue, 15f, TrendDirection.Bear, 0.006f, seed);

            var ctx = BuildTestContext(stock, scheduler, 60f, noiseSeed);
            var sim = TipActivator.SimulateRound(ctx);
            var actual = RunFullRound(stock, scheduler, priceGen, 60f, 1f / 60f, StockTier.LowValue);

            Assert.AreEqual(actual.MinPriceNormalizedTime, sim.MinPriceNormalizedTime, 0.001f,
                $"[Seed {seed}] DipMarker sim={sim.MinPriceNormalizedTime:F3} vs actual={actual.MinPriceNormalizedTime:F3}");
        }

        // === PeakMarker timing (near-zero tolerance) ===

        [Test]
        public void PeakMarker_Timing_Accuracy([Values(42, 99, 200, 500, 750)] int seed)
        {
            var (stock, scheduler, priceGen, noiseSeed) = SetupRound(
                StockTier.Penny, 6f, TrendDirection.Bull, 0.015f, seed);

            var ctx = BuildTestContext(stock, scheduler, 60f, noiseSeed);
            var sim = TipActivator.SimulateRound(ctx);
            var actual = RunFullRound(stock, scheduler, priceGen, 60f, 1f / 60f, StockTier.Penny);

            Assert.AreEqual(actual.MaxPriceNormalizedTime, sim.MaxPriceNormalizedTime, 0.001f,
                $"[Seed {seed}] PeakMarker sim={sim.MaxPriceNormalizedTime:F3} vs actual={actual.MaxPriceNormalizedTime:F3}");
        }

        // === ClosingDirection match rate (100% with deterministic replay) ===

        [Test]
        public void ClosingDirection_MatchRate_Over20Seeds()
        {
            int matches = 0;
            int total = 20;

            for (int seed = 1; seed <= total; seed++)
            {
                EventBus.Clear();

                var (stock, scheduler, priceGen, noiseSeed) = SetupRound(
                    StockTier.Penny, 6f, TrendDirection.Bull, 0.012f, seed * 37);

                float startingPrice = stock.StartingPrice;
                var ctx = BuildTestContext(stock, scheduler, 60f, noiseSeed);
                var sim = TipActivator.SimulateRound(ctx);
                var actual = RunFullRound(stock, scheduler, priceGen, 60f, 1f / 60f, StockTier.Penny);

                bool simUp = sim.ClosingPrice >= startingPrice;
                bool actualUp = actual.ClosingPrice >= startingPrice;

                if (simUp == actualUp)
                    matches++;
            }

            float matchRate = (float)matches / total;
            Assert.GreaterOrEqual(matchRate, 1.00f,
                $"ClosingDirection match rate {matchRate:P0} ({matches}/{total}) should be 100%");
        }

        [Test]
        public void ClosingDirection_MatchRate_BearTrend()
        {
            int matches = 0;
            int total = 20;

            for (int seed = 1; seed <= total; seed++)
            {
                EventBus.Clear();

                var (stock, scheduler, priceGen, noiseSeed) = SetupRound(
                    StockTier.MidValue, 100f, TrendDirection.Bear, 0.008f, seed * 41);

                float startingPrice = stock.StartingPrice;
                var ctx = BuildTestContext(stock, scheduler, 60f, noiseSeed);
                var sim = TipActivator.SimulateRound(ctx);
                var actual = RunFullRound(stock, scheduler, priceGen, 60f, 1f / 60f, StockTier.MidValue);

                bool simUp = sim.ClosingPrice >= startingPrice;
                bool actualUp = actual.ClosingPrice >= startingPrice;

                if (simUp == actualUp)
                    matches++;
            }

            float matchRate = (float)matches / total;
            Assert.GreaterOrEqual(matchRate, 1.00f,
                $"Bear ClosingDirection match rate {matchRate:P0} ({matches}/{total}) should be 100%");
        }

        [Test]
        public void ClosingDirection_MatchRate_NeutralTrend()
        {
            int matches = 0;
            int total = 20;

            for (int seed = 1; seed <= total; seed++)
            {
                EventBus.Clear();

                var (stock, scheduler, priceGen, noiseSeed) = SetupRound(
                    StockTier.LowValue, 20f, TrendDirection.Neutral, 0.005f, seed * 53);

                float startingPrice = stock.StartingPrice;
                var ctx = BuildTestContext(stock, scheduler, 60f, noiseSeed);
                var sim = TipActivator.SimulateRound(ctx);
                var actual = RunFullRound(stock, scheduler, priceGen, 60f, 1f / 60f, StockTier.LowValue);

                bool simUp = sim.ClosingPrice >= startingPrice;
                bool actualUp = actual.ClosingPrice >= startingPrice;

                if (simUp == actualUp)
                    matches++;
            }

            float matchRate = (float)matches / total;
            Assert.GreaterOrEqual(matchRate, 1.00f,
                $"Neutral ClosingDirection match rate {matchRate:P0} ({matches}/{total}) should be 100%");
        }

        // === EventTiming exact match ===

        [Test]
        public void EventTiming_ExactMatch([Values(42, 99, 300)] int seed)
        {
            var (stock, scheduler, priceGen, noiseSeed) = SetupRound(
                StockTier.Penny, 6f, TrendDirection.Bull, 0.015f, seed);

            var ctx = BuildTestContext(stock, scheduler, 60f, noiseSeed);
            var tip = new RevealedTip(InsiderTipType.EventTiming, "generic", 0f);
            var tips = new List<RevealedTip> { tip };
            var overlays = TipActivator.ActivateTips(tips, ctx);

            var markers = overlays[0].TimeMarkers;
            var preDecided = scheduler.PreDecidedEvents;

            // Sort pre-decided fire times for comparison
            float[] expectedTimes = new float[preDecided.Length];
            for (int i = 0; i < preDecided.Length; i++)
                expectedTimes[i] = Mathf.Clamp01(preDecided[i].FireTime / 60f);
            System.Array.Sort(expectedTimes);

            Assert.AreEqual(preDecided.Length, markers.Length,
                $"[Seed {seed}] Marker count should match event count");

            for (int i = 0; i < markers.Length; i++)
            {
                Assert.AreEqual(expectedTimes[i], markers[i], 0.0001f,
                    $"[Seed {seed}] Marker {i}: expected {expectedTimes[i]:F4}, got {markers[i]:F4}");
            }
        }

        // === EventCount exact match ===

        [Test]
        public void EventCount_ExactMatch([Values(42, 150, 500)] int seed)
        {
            var (stock, scheduler, priceGen, noiseSeed) = SetupRound(
                StockTier.MidValue, 100f, TrendDirection.Bull, 0.005f, seed);

            var ctx = BuildTestContext(stock, scheduler, 60f, noiseSeed);
            var tip = new RevealedTip(InsiderTipType.EventCount, "generic", 0f);
            var tips = new List<RevealedTip> { tip };
            var overlays = TipActivator.ActivateTips(tips, ctx);

            Assert.AreEqual(scheduler.PreDecidedEvents.Length, overlays[0].EventCountdown,
                $"[Seed {seed}] Event count should exactly match PreDecidedEvents length");
        }

        // === TrendReversal consistency ===

        [Test]
        public void TrendReversal_Consistency([Values(42, 88, 200, 500, 777)] int seed)
        {
            var (stock, scheduler, priceGen, noiseSeed) = SetupRound(
                StockTier.Penny, 6f, TrendDirection.Bull, 0.015f, seed);

            var ctx = BuildTestContext(stock, scheduler, 60f, noiseSeed);

            // Get simulation's reversal prediction
            var tip = new RevealedTip(InsiderTipType.TrendReversal, "generic", 0f);
            var tips = new List<RevealedTip> { tip };
            var overlays = TipActivator.ActivateTips(tips, ctx);
            bool simDetectsReversal = overlays[0].ReversalTime >= 0f;

            // Run actual round and check for direction changes at event boundaries
            var actual = RunFullRound(stock, scheduler, priceGen, 60f, 1f / 60f, StockTier.Penny);

            // With deterministic replay, if sim says no reversal,
            // actual shouldn't have an obvious one either
            if (!simDetectsReversal)
            {
                bool actualHasObviousReversal = actual.ClosingPrice < actual.MaxPrice * 0.60f;
                Assert.IsFalse(actualHasObviousReversal,
                    $"[Seed {seed}] Sim missed obvious reversal: closing={actual.ClosingPrice:F2}, max={actual.MaxPrice:F2}");
            }
        }
    }
}
