using NUnit.Framework;
using System.Collections.Generic;

namespace BullRun.Tests.Shop
{
    [TestFixture]
    public class TipActivatorTests
    {
        private TipActivationContext CreateContext(
            TrendDirection trend = TrendDirection.Bull,
            float startingPrice = 10f,
            PreDecidedEvent[] preDecidedEvents = null,
            float roundDuration = 60f,
            float trendRate = 0.015f,
            int seed = 42,
            StockTier tier = StockTier.Penny)
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", tier, startingPrice, trend, trendRate);

            return new TipActivationContext
            {
                ActiveStock = stock,
                PreDecidedEvents = preDecidedEvents,
                RoundDuration = roundDuration,
                TierConfig = StockTierData.GetTierConfig(tier),
                Random = new System.Random(seed)
            };
        }

        /// <summary>
        /// Helper to create a single-phase pre-decided event.
        /// </summary>
        private PreDecidedEvent MakeEvent(float fireTime, float priceEffect, bool isPositive = true,
            MarketEventType type = MarketEventType.EarningsBeat, float duration = 4f)
        {
            var config = new MarketEventConfig(type, priceEffect, priceEffect, duration,
                new[] { StockTier.Penny, StockTier.LowValue, StockTier.MidValue, StockTier.BlueChip }, 0.5f);
            return new PreDecidedEvent(fireTime, config, priceEffect, isPositive);
        }

        /// <summary>
        /// Helper to create a PumpAndDump pre-decided event.
        /// </summary>
        private PreDecidedEvent MakePumpAndDump(float fireTime, float pumpEffect)
        {
            var config = new MarketEventConfig(MarketEventType.PumpAndDump, pumpEffect, pumpEffect, 6f,
                new[] { StockTier.Penny }, 0.3f);
            float dumpTarget = 0.80f / (1f + pumpEffect) - 1f;
            var phases = new List<MarketEventPhase>
            {
                new MarketEventPhase(pumpEffect, 3.6f),
                new MarketEventPhase(dumpTarget, 2.4f)
            };
            return new PreDecidedEvent(fireTime, config, pumpEffect, true, phases);
        }

        /// <summary>
        /// Helper to create a FlashCrash pre-decided event.
        /// </summary>
        private PreDecidedEvent MakeFlashCrash(float fireTime, float crashEffect)
        {
            var config = new MarketEventConfig(MarketEventType.FlashCrash, crashEffect, crashEffect, 3f,
                new[] { StockTier.LowValue, StockTier.MidValue }, 0.25f);
            float recoveryTarget = 0.95f / (1f + crashEffect) - 1f;
            var phases = new List<MarketEventPhase>
            {
                new MarketEventPhase(crashEffect, 1.2f),
                new MarketEventPhase(recoveryTarget, 1.8f)
            };
            return new PreDecidedEvent(fireTime, config, crashEffect, false, phases);
        }

        // === SimulateRound accuracy tests (AC 8) ===

        [Test]
        public void SimulateRound_NoEvents_PureTrend_BullTrend()
        {
            var ctx = CreateContext(trend: TrendDirection.Bull, startingPrice: 10f, trendRate: 0.01f);
            var sim = TipActivator.SimulateRound(ctx);

            // Bull trend with no events: min = starting price, max = closing price
            Assert.AreEqual(10f, sim.MinPrice, 0.01f, "Min should be starting price for pure bull");
            Assert.Greater(sim.MaxPrice, 10f, "Max should be above starting for pure bull");
            Assert.Greater(sim.ClosingPrice, 10f, "Closing should be above starting for pure bull");
            Assert.AreEqual(sim.ClosingPrice, sim.MaxPrice, 0.01f, "For pure bull, max = closing");
        }

        [Test]
        public void SimulateRound_NoEvents_PureTrend_BearTrend()
        {
            var ctx = CreateContext(trend: TrendDirection.Bear, startingPrice: 10f, trendRate: 0.01f);
            var sim = TipActivator.SimulateRound(ctx);

            // Bear trend with no events: max = starting price, min = closing price
            Assert.AreEqual(10f, sim.MaxPrice, 0.01f, "Max should be starting price for pure bear");
            Assert.Less(sim.MinPrice, 10f, "Min should be below starting for pure bear");
            Assert.Less(sim.ClosingPrice, 10f, "Closing should be below starting for pure bear");
        }

        [Test]
        public void SimulateRound_NoEvents_NeutralTrend()
        {
            var ctx = CreateContext(trend: TrendDirection.Neutral, startingPrice: 10f, trendRate: 0.01f);
            var sim = TipActivator.SimulateRound(ctx);

            // Neutral: price stays at starting price
            Assert.AreEqual(10f, sim.MinPrice, 0.01f);
            Assert.AreEqual(10f, sim.MaxPrice, 0.01f);
            Assert.AreEqual(10f, sim.ClosingPrice, 0.01f);
        }

        [Test]
        public void SimulateRound_SinglePositiveEvent_MaxExceedsTierBounds()
        {
            // +50% event should push ceiling beyond tier max ($8)
            var events = new[] { MakeEvent(20f, 0.50f, true) };
            var ctx = CreateContext(startingPrice: 6f, preDecidedEvents: events, trendRate: 0.005f);
            var sim = TipActivator.SimulateRound(ctx);

            // Ceiling should be well above $8 tier max
            Assert.Greater(sim.MaxPrice, 8f,
                $"Ceiling ${sim.MaxPrice:F2} should exceed tier max $8.00 with +50% event");
        }

        [Test]
        public void SimulateRound_SingleNegativeEvent_FloorBelowTierMin()
        {
            // -30% event should push floor below normal range
            var events = new[] { MakeEvent(20f, -0.30f, false, MarketEventType.EarningsMiss) };
            var ctx = CreateContext(startingPrice: 6f, preDecidedEvents: events, trendRate: 0.005f);
            var sim = TipActivator.SimulateRound(ctx);

            Assert.Less(sim.MinPrice, 6f, "Floor should be below starting price with -30% event");
        }

        [Test]
        public void SimulateRound_PumpAndDump_TracksPumpPeakAsMax()
        {
            var events = new[] { MakePumpAndDump(20f, 0.80f) };
            var ctx = CreateContext(startingPrice: 5f, preDecidedEvents: events, trendRate: 0.005f,
                trend: TrendDirection.Neutral);
            var sim = TipActivator.SimulateRound(ctx);

            // Pump peak = ~$5 * (1 + 0.80) = ~$9, post-event = $5 * 0.80 = $4
            Assert.Greater(sim.MaxPrice, 8f, "Max should reflect pump peak, not post-event price");
            Assert.Less(sim.MinPrice, 5f, "Min should reflect post-dump price below starting");
        }

        [Test]
        public void SimulateRound_FlashCrash_TracksCrashBottomAsMin()
        {
            var events = new[] { MakeFlashCrash(20f, -0.25f) };
            var ctx = CreateContext(startingPrice: 10f, preDecidedEvents: events, trendRate: 0.005f,
                trend: TrendDirection.Neutral);
            var sim = TipActivator.SimulateRound(ctx);

            // Crash bottom = 10 * (1 + -0.25) = $7.50, recovery = 10 * 0.95 = $9.50
            Assert.Less(sim.MinPrice, 8f, "Min should reflect crash bottom");
        }

        [Test]
        public void SimulateRound_DynamicFloor_ClampsPrice()
        {
            // Massive negative event on low starting price should trigger floor
            var events = new[] { MakeEvent(10f, -0.95f, false, MarketEventType.MarketCrash) };
            var ctx = CreateContext(startingPrice: 5f, preDecidedEvents: events, trendRate: 0.001f,
                trend: TrendDirection.Neutral);
            var sim = TipActivator.SimulateRound(ctx);

            float expectedFloor = 5f * GameConfig.PriceFloorPercent;
            Assert.GreaterOrEqual(sim.MinPrice, expectedFloor,
                $"Min ${sim.MinPrice:F2} should not breach dynamic floor ${expectedFloor:F2}");
        }

        [Test]
        public void SimulateRound_Deterministic_SameSeedSameResults()
        {
            var events = new[] {
                MakeEvent(10f, 0.20f),
                MakeEvent(30f, -0.15f, false, MarketEventType.EarningsMiss),
                MakeEvent(45f, 0.30f)
            };

            var ctx1 = CreateContext(preDecidedEvents: events, seed: 42);
            var sim1 = TipActivator.SimulateRound(ctx1);

            var ctx2 = CreateContext(preDecidedEvents: events, seed: 42);
            var sim2 = TipActivator.SimulateRound(ctx2);

            Assert.AreEqual(sim1.MinPrice, sim2.MinPrice, 0.0001f);
            Assert.AreEqual(sim1.MaxPrice, sim2.MaxPrice, 0.0001f);
            Assert.AreEqual(sim1.ClosingPrice, sim2.ClosingPrice, 0.0001f);
            Assert.AreEqual(sim1.MinPriceNormalizedTime, sim2.MinPriceNormalizedTime, 0.0001f);
            Assert.AreEqual(sim1.MaxPriceNormalizedTime, sim2.MaxPriceNormalizedTime, 0.0001f);
        }

        // === PriceCeiling uses simulation max (AC 8) ===

        [Test]
        public void PriceCeiling_UsesSimulationMax_NotTierMax()
        {
            var events = new[] { MakeEvent(20f, 0.50f) };
            var ctx = CreateContext(startingPrice: 6f, preDecidedEvents: events, trendRate: 0.005f);
            var tip = new RevealedTip(InsiderTipType.PriceCeiling, "generic", 0f);
            var tips = new List<RevealedTip> { tip };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            var sim = TipActivator.SimulateRound(CreateContext(startingPrice: 6f, preDecidedEvents: events, trendRate: 0.005f));
            Assert.AreEqual(sim.MaxPrice, overlays[0].PriceLevel, 0.01f,
                "Ceiling overlay should use simulation max price");
            Assert.Greater(overlays[0].PriceLevel, 8f,
                "Ceiling should exceed tier max $8 with +50% event");
        }

        // === PriceFloor uses simulation min (AC 8) ===

        [Test]
        public void PriceFloor_UsesSimulationMin_NotTierMin()
        {
            var events = new[] { MakeEvent(20f, -0.30f, false, MarketEventType.EarningsMiss) };
            var ctx = CreateContext(startingPrice: 6f, preDecidedEvents: events, trendRate: 0.005f);
            var tip = new RevealedTip(InsiderTipType.PriceFloor, "generic", 0f);
            var tips = new List<RevealedTip> { tip };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            Assert.Less(overlays[0].PriceLevel, 6f,
                "Floor overlay should use simulation min, below starting price");
        }

        // === DipMarker time matches simulation min time (AC 8) ===

        [Test]
        public void DipMarker_TimeMatchesSimulationMinTime()
        {
            // Bull trend + late large negative event → dip near event time
            // -50% at t=45 on bull (price ~$15.67): target = $7.84, well below starting $10
            var events = new[] { MakeEvent(45f, -0.50f, false, MarketEventType.MarketCrash) };
            var ctx = CreateContext(trend: TrendDirection.Bull, preDecidedEvents: events, trendRate: 0.01f);
            var tip = new RevealedTip(InsiderTipType.DipMarker, "DIP", 0f);
            var tips = new List<RevealedTip> { tip };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            // Dip should be near the event, not at start
            Assert.Greater(overlays[0].TimeZoneCenter, 0.50f,
                $"Dip zone {overlays[0].TimeZoneCenter} should be near late event, not at start");
        }

        // === PeakMarker time matches simulation max time (AC 8) ===

        [Test]
        public void PeakMarker_TimeMatchesSimulationMaxTime()
        {
            // Bull trend + early positive event → peak reflects actual max
            var events = new[] { MakeEvent(10f, 0.50f) };
            var ctx = CreateContext(trend: TrendDirection.Bull, preDecidedEvents: events, trendRate: 0.01f);
            var tip = new RevealedTip(InsiderTipType.PeakMarker, "PEAK", 0f);
            var tips = new List<RevealedTip> { tip };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            // Bull trend + early positive event: max is at round end (clamped to 0.90 by ComputePeakMarkerOverlay)
            Assert.GreaterOrEqual(overlays[0].TimeZoneCenter, 0.80f,
                $"Peak zone {overlays[0].TimeZoneCenter} should be near round end for bull + early positive event");
        }

        // === ClosingDirection matches simulation (AC 8) ===

        [Test]
        public void ClosingDirection_MatchesSimulation_BearWithLatePositiveEvent()
        {
            // Bear trend with late +80% bull event → simulation shows closing higher
            var events = new[] { MakeEvent(50f, 0.80f) };
            var ctx = CreateContext(trend: TrendDirection.Bear, preDecidedEvents: events,
                startingPrice: 10f, trendRate: 0.005f);
            var tip = new RevealedTip(InsiderTipType.ClosingDirection, "DIR", 0f);
            var tips = new List<RevealedTip> { tip };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            // Verify simulation says closing higher
            var sim = TipActivator.SimulateRound(CreateContext(trend: TrendDirection.Bear,
                preDecidedEvents: events, startingPrice: 10f, trendRate: 0.005f));
            int expectedDirection = sim.ClosingPrice >= 10f ? 1 : -1;
            Assert.AreEqual(expectedDirection, overlays[0].DirectionSign,
                $"Direction should match simulation. Closing={sim.ClosingPrice:F2}, Starting=10.00");
        }

        // === EventTiming has no fuzz (AC 8) ===

        [Test]
        public void EventTiming_ExactFireTimes_NoFuzz()
        {
            var events = new[]
            {
                MakeEvent(10f, 0.10f),
                MakeEvent(25f, 0.10f),
                MakeEvent(40f, 0.10f)
            };
            var ctx = CreateContext(preDecidedEvents: events, roundDuration: 60f);
            var tip = new RevealedTip(InsiderTipType.EventTiming, "TIMING", 0f);
            var tips = new List<RevealedTip> { tip };

            var overlays = TipActivator.ActivateTips(tips, ctx);
            var markers = overlays[0].TimeMarkers;

            Assert.AreEqual(3, markers.Length);
            // Exact normalized times — bit-for-bit match
            Assert.AreEqual(10f / 60f, markers[0], 0.0001f, "Marker 0 should exactly match fire time");
            Assert.AreEqual(25f / 60f, markers[1], 0.0001f, "Marker 1 should exactly match fire time");
            Assert.AreEqual(40f / 60f, markers[2], 0.0001f, "Marker 2 should exactly match fire time");
        }

        // === TrendReversal detects event-driven reversal (AC 8) ===

        [Test]
        public void TrendReversal_BullWithLateCrash_DetectsReversal()
        {
            // Bull trend + late crash event → price was rising, then drops → reversal
            var events = new[]
            {
                MakeEvent(15f, 0.10f),          // small positive — still rising
                MakeEvent(40f, -0.40f, false, MarketEventType.MarketCrash)  // big negative — reversal
            };
            var ctx = CreateContext(trend: TrendDirection.Bull, preDecidedEvents: events, trendRate: 0.01f);
            var tip = new RevealedTip(InsiderTipType.TrendReversal, "REVERSAL", 0f);
            var tips = new List<RevealedTip> { tip };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            Assert.AreEqual("REVERSAL", overlays[0].Label);
            Assert.Greater(overlays[0].ReversalTime, 0f, "Should detect reversal");
        }

        // === Pre-decided events match runtime behavior (AC 8) ===

        [Test]
        public void PreDecidedEvents_MatchTypes_EventScheduler()
        {
            EventBus.Clear();
            var eventEffects = new EventEffects();
            var rng = new System.Random(42);
            var scheduler = new EventScheduler(eventEffects, rng);

            var stocks = new List<StockInstance>();
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.MidValue, 100f, TrendDirection.Bull, 0.01f);
            stocks.Add(stock);
            eventEffects.SetActiveStocks(stocks);

            scheduler.InitializeRound(1, 1, StockTier.MidValue, stocks, 60f);

            // Verify pre-decided events have valid data
            Assert.IsNotNull(scheduler.PreDecidedEvents);
            Assert.AreEqual(scheduler.ScheduledEventCount, scheduler.PreDecidedEvents.Length);

            for (int i = 0; i < scheduler.PreDecidedEvents.Length; i++)
            {
                var evt = scheduler.PreDecidedEvents[i];
                Assert.Greater(evt.FireTime, 0f, $"Event {i} fire time should be positive");
                Assert.Greater(evt.Config.Duration, 0f, $"Event {i} config duration should be positive");
                Assert.AreNotEqual(0f, evt.PriceEffect, $"Event {i} price effect should be non-zero");
            }

            EventBus.Clear();
        }

        // === Multi-phase simulation (AC 8) ===

        [Test]
        public void SimulateRound_MultiPhase_PumpAndDump_PumpPeakIsMax()
        {
            var events = new[] { MakePumpAndDump(20f, 0.60f) };
            var ctx = CreateContext(startingPrice: 10f, preDecidedEvents: events, trendRate: 0f,
                trend: TrendDirection.Neutral);
            var sim = TipActivator.SimulateRound(ctx);

            // Pump peak = 10 * (1 + 0.60) = $16.00
            float expectedPeak = 10f * (1f + 0.60f);
            Assert.AreEqual(expectedPeak, sim.MaxPrice, 0.1f,
                "Max should be at pump peak");

            // Post-dump = 10 * 0.80 = $8.00
            float expectedDump = 10f * 0.80f;
            Assert.AreEqual(expectedDump, sim.MinPrice, 0.1f,
                "Min should be at post-dump price");
        }

        // === No fuzz in any tip value (AC 8) ===

        [Test]
        public void NoFuzz_AllTipValues_DeterministicAcrossRuns()
        {
            var events = new[]
            {
                MakeEvent(10f, 0.20f),
                MakeEvent(30f, -0.15f, false, MarketEventType.EarningsMiss),
                MakeEvent(50f, 0.10f)
            };

            var tips = new List<RevealedTip>
            {
                new RevealedTip(InsiderTipType.PriceFloor, "generic", 0f),
                new RevealedTip(InsiderTipType.PriceCeiling, "generic", 0f),
                new RevealedTip(InsiderTipType.PriceForecast, "generic", 0f),
                new RevealedTip(InsiderTipType.DipMarker, "generic", 0f),
                new RevealedTip(InsiderTipType.PeakMarker, "generic", 0f),
                new RevealedTip(InsiderTipType.ClosingDirection, "generic", 0f),
                new RevealedTip(InsiderTipType.EventTiming, "generic", 0f),
                new RevealedTip(InsiderTipType.EventCount, "generic", 0f),
                new RevealedTip(InsiderTipType.TrendReversal, "generic", 0f)
            };

            // Run 1
            var ctx1 = CreateContext(preDecidedEvents: events, seed: 99);
            var overlays1 = TipActivator.ActivateTips(new List<RevealedTip>(tips), ctx1);

            // Run 2 — same inputs, different Random seed (shouldn't matter — no fuzz)
            var ctx2 = CreateContext(preDecidedEvents: events, seed: 123);
            var overlays2 = TipActivator.ActivateTips(new List<RevealedTip>(tips), ctx2);

            // All overlay values should be identical regardless of Random seed
            for (int i = 0; i < overlays1.Count; i++)
            {
                Assert.AreEqual(overlays1[i].PriceLevel, overlays2[i].PriceLevel, 0.0001f,
                    $"Overlay {i} ({overlays1[i].Type}) PriceLevel differs — fuzz detected");
                Assert.AreEqual(overlays1[i].BandCenter, overlays2[i].BandCenter, 0.0001f,
                    $"Overlay {i} ({overlays1[i].Type}) BandCenter differs — fuzz detected");
                Assert.AreEqual(overlays1[i].TimeZoneCenter, overlays2[i].TimeZoneCenter, 0.0001f,
                    $"Overlay {i} ({overlays1[i].Type}) TimeZoneCenter differs — fuzz detected");
                Assert.AreEqual(overlays1[i].DirectionSign, overlays2[i].DirectionSign,
                    $"Overlay {i} ({overlays1[i].Type}) DirectionSign differs — fuzz detected");
                Assert.AreEqual(overlays1[i].ReversalTime, overlays2[i].ReversalTime, 0.0001f,
                    $"Overlay {i} ({overlays1[i].Type}) ReversalTime differs — fuzz detected");
                if (overlays1[i].TimeMarkers != null && overlays2[i].TimeMarkers != null)
                {
                    Assert.AreEqual(overlays1[i].TimeMarkers.Length, overlays2[i].TimeMarkers.Length);
                    for (int j = 0; j < overlays1[i].TimeMarkers.Length; j++)
                    {
                        Assert.AreEqual(overlays1[i].TimeMarkers[j], overlays2[i].TimeMarkers[j], 0.0001f,
                            $"Overlay {i} marker {j} differs — fuzz detected");
                    }
                }
            }
        }

        // === Display text update tests (AC 5) ===

        [Test]
        public void ActivateTips_UpdatesDisplayText_PriceCeiling()
        {
            var events = new[] { MakeEvent(20f, 0.30f) };
            var ctx = CreateContext(startingPrice: 6f, preDecidedEvents: events, trendRate: 0.005f);
            var tips = new List<RevealedTip>
            {
                new RevealedTip(InsiderTipType.PriceCeiling, "Price ceiling \u2014 revealed on chart", 0f)
            };

            TipActivator.ActivateTips(tips, ctx);

            Assert.IsTrue(tips[0].DisplayText.Contains("Ceiling ~$"),
                $"Display text should be updated with actual value, got: {tips[0].DisplayText}");
            Assert.Greater(tips[0].NumericValue, 0f, "NumericValue should be set to simulation max");
        }

        [Test]
        public void ActivateTips_UpdatesDisplayText_PriceFloor()
        {
            var events = new[] { MakeEvent(20f, -0.20f, false, MarketEventType.EarningsMiss) };
            var ctx = CreateContext(startingPrice: 6f, preDecidedEvents: events, trendRate: 0.005f);
            var tips = new List<RevealedTip>
            {
                new RevealedTip(InsiderTipType.PriceFloor, "Price floor \u2014 revealed on chart", 0f)
            };

            TipActivator.ActivateTips(tips, ctx);

            Assert.IsTrue(tips[0].DisplayText.Contains("Floor ~$"),
                $"Display text should be updated, got: {tips[0].DisplayText}");
        }

        [Test]
        public void ActivateTips_UpdatesDisplayText_PriceForecast()
        {
            var events = new[] { MakeEvent(20f, 0.30f) };
            var ctx = CreateContext(startingPrice: 6f, preDecidedEvents: events, trendRate: 0.005f);
            var tips = new List<RevealedTip>
            {
                new RevealedTip(InsiderTipType.PriceForecast, "Price forecast \u2014 revealed on chart", 0f)
            };

            TipActivator.ActivateTips(tips, ctx);

            Assert.IsTrue(tips[0].DisplayText.Contains("Sweet spot ~$"),
                $"Display text should be updated with actual value, got: {tips[0].DisplayText}");
            Assert.Greater(tips[0].NumericValue, 0f, "NumericValue should be set to simulation average");
        }

        [Test]
        public void ActivateTips_UpdatesDisplayText_ClosingDirection()
        {
            var events = new[] { MakeEvent(20f, 0.30f) };
            var ctx = CreateContext(trend: TrendDirection.Bull, startingPrice: 10f,
                preDecidedEvents: events, trendRate: 0.01f);
            var tips = new List<RevealedTip>
            {
                new RevealedTip(InsiderTipType.ClosingDirection, "Closing direction \u2014 revealed on chart", 0f)
            };

            TipActivator.ActivateTips(tips, ctx);

            Assert.IsTrue(tips[0].DisplayText.Contains("HIGHER") || tips[0].DisplayText.Contains("LOWER"),
                $"Display text should be updated with direction, got: {tips[0].DisplayText}");
        }

        // === Integration tests ===

        [Test]
        public void ActivateTips_EmptyList_ReturnsEmptyList()
        {
            var ctx = CreateContext();
            var tips = new List<RevealedTip>();

            var overlays = TipActivator.ActivateTips(tips, ctx);

            Assert.IsNotNull(overlays);
            Assert.AreEqual(0, overlays.Count);
        }

        [Test]
        public void ActivateTips_MultipleTips_ReturnsCorrectCount()
        {
            var events = new[] { MakeEvent(10f, 0.10f), MakeEvent(30f, -0.10f, false, MarketEventType.EarningsMiss) };
            var ctx = CreateContext(preDecidedEvents: events);
            var tips = new List<RevealedTip>
            {
                new RevealedTip(InsiderTipType.PriceFloor, "generic", 0f),
                new RevealedTip(InsiderTipType.EventCount, "generic", 0f),
                new RevealedTip(InsiderTipType.DipMarker, "generic", 0f)
            };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            Assert.AreEqual(3, overlays.Count);
            Assert.AreEqual(InsiderTipType.PriceFloor, overlays[0].Type);
            Assert.AreEqual(InsiderTipType.EventCount, overlays[1].Type);
            Assert.AreEqual(InsiderTipType.DipMarker, overlays[2].Type);
        }

        [Test]
        public void EventCount_UsesPreDecidedEventLength()
        {
            var events = new[] { MakeEvent(10f, 0.10f), MakeEvent(20f, 0.10f), MakeEvent(30f, 0.10f) };
            var ctx = CreateContext(preDecidedEvents: events);
            var tip = new RevealedTip(InsiderTipType.EventCount, "generic", 0f);
            var tips = new List<RevealedTip> { tip };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            Assert.AreEqual(3, overlays[0].EventCountdown);
            Assert.AreEqual("EVENTS: 3", overlays[0].Label);
        }

        [Test]
        public void EventCount_NoEvents_ReturnsZero()
        {
            var ctx = CreateContext(preDecidedEvents: null);
            var tip = new RevealedTip(InsiderTipType.EventCount, "generic", 0f);
            var tips = new List<RevealedTip> { tip };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            Assert.AreEqual(0, overlays[0].EventCountdown);
        }

        [Test]
        public void EventTiming_NoEvents_ReturnsEmptyArray()
        {
            var ctx = CreateContext(preDecidedEvents: null);
            var tip = new RevealedTip(InsiderTipType.EventTiming, "TIMING", 0f);
            var tips = new List<RevealedTip> { tip };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            Assert.IsNotNull(overlays[0].TimeMarkers);
            Assert.AreEqual(0, overlays[0].TimeMarkers.Length);
            Assert.AreEqual("NO EVENTS", overlays[0].Label);
        }

        [Test]
        public void TrendReversal_NeutralTrend_NoReversal()
        {
            var events = new[] { MakeEvent(10f, 0.10f), MakeEvent(30f, -0.10f, false, MarketEventType.EarningsMiss) };
            var ctx = CreateContext(trend: TrendDirection.Neutral, preDecidedEvents: events);
            var tip = new RevealedTip(InsiderTipType.TrendReversal, "REVERSAL", 0f);
            var tips = new List<RevealedTip> { tip };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            Assert.AreEqual(-1f, overlays[0].ReversalTime, 0.001f);
            Assert.AreEqual("NO REVERSAL", overlays[0].Label);
        }

        [Test]
        public void TrendReversal_NoEvents_NoReversal()
        {
            var ctx = CreateContext(preDecidedEvents: null);
            var tip = new RevealedTip(InsiderTipType.TrendReversal, "REVERSAL", 0f);
            var tips = new List<RevealedTip> { tip };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            Assert.AreEqual(-1f, overlays[0].ReversalTime, 0.001f);
        }

        // === Sentinel value tests ===

        [Test]
        public void PriceFloor_SentinelValuesCorrect()
        {
            var events = new[] { MakeEvent(20f, -0.10f, false, MarketEventType.EarningsMiss) };
            var ctx = CreateContext(preDecidedEvents: events);
            var tip = new RevealedTip(InsiderTipType.PriceFloor, "generic", 0f);
            var overlays = TipActivator.ActivateTips(new List<RevealedTip> { tip }, ctx);

            Assert.AreEqual(-1f, overlays[0].TimeZoneCenter, 0.001f, "TimeZoneCenter sentinel");
            Assert.AreEqual(-1f, overlays[0].ReversalTime, 0.001f, "ReversalTime sentinel");
            Assert.AreEqual(-1, overlays[0].EventCountdown, "EventCountdown sentinel");
            Assert.AreEqual(0, overlays[0].DirectionSign, "DirectionSign sentinel");
        }

        [Test]
        public void DipMarker_SentinelValuesCorrect()
        {
            var events = new[] { MakeEvent(20f, -0.10f, false, MarketEventType.EarningsMiss) };
            var ctx = CreateContext(preDecidedEvents: events);
            var tip = new RevealedTip(InsiderTipType.DipMarker, "generic", 0f);
            var overlays = TipActivator.ActivateTips(new List<RevealedTip> { tip }, ctx);

            Assert.AreEqual(0f, overlays[0].PriceLevel, 0.001f, "PriceLevel sentinel");
            Assert.AreEqual(-1f, overlays[0].ReversalTime, 0.001f, "ReversalTime sentinel");
            Assert.AreEqual(-1, overlays[0].EventCountdown, "EventCountdown sentinel");
        }

        // === Story 18.7, AC 7: Drift regression tests ===

        [Test]
        public void Drift_SingleLargeEvent_ReducesMaxVsNaive()
        {
            // Bull stock, +50% event at t=30, duration=4
            // Without drift: post-event base stays at full target, trend compounds on inflated base
            // With drift: post-event base pulled down toward trend line, reducing closing price
            var events = new[] { MakeEvent(30f, 0.50f) };
            var ctx = CreateContext(trend: TrendDirection.Bull, startingPrice: 10f,
                preDecidedEvents: events, trendRate: 0.01f);
            var sim = TipActivator.SimulateRound(ctx);

            // Compute naive (no drift) closing: full target compounds for remaining time
            float priceAtEventTime = 10f * UnityEngine.Mathf.Pow(1f + 0.01f, 30f);
            float naiveTarget = priceAtEventTime * 1.50f;
            float naiveClosing = naiveTarget * UnityEngine.Mathf.Pow(1f + 0.01f, 60f - 30f - 4f);

            // Closing should be lower than naive closing (drift pulls post-event base down)
            Assert.Less(sim.ClosingPrice, naiveClosing,
                $"Closing ${sim.ClosingPrice:F2} should be below naive closing ${naiveClosing:F2} due to drift");

            // For bull trend, max is at round end (trend continues growing past event peak)
            Assert.AreEqual(sim.ClosingPrice, sim.MaxPrice, 0.01f,
                "For bull trend, MaxPrice should equal ClosingPrice (continuous growth)");
        }

        [Test]
        public void Drift_CompoundingEvents_InflationEliminated()
        {
            // Two events: +30% at t=15 (dur=4), +20% at t=40 (dur=4)
            // Without drift: second event compounds on full +30% target → inflated closing
            // With drift: second event compounds on drifted (lower) base → reduced closing
            var events = new[]
            {
                MakeEvent(15f, 0.30f),
                MakeEvent(40f, 0.20f)
            };

            // Compute naive (no drift) closing: full targets compound for remaining time
            float price15 = 10f * UnityEngine.Mathf.Pow(1f + 0.01f, 15f);
            float naiveAfterFirst = price15 * 1.30f;
            float naiveAt40 = naiveAfterFirst * UnityEngine.Mathf.Pow(1f + 0.01f, 40f - 15f - 4f);
            float naiveAfterSecond = naiveAt40 * 1.20f;
            float naiveClosing = naiveAfterSecond * UnityEngine.Mathf.Pow(1f + 0.01f, 60f - 40f - 4f);

            var ctx = CreateContext(trend: TrendDirection.Bull, startingPrice: 10f,
                preDecidedEvents: events, trendRate: 0.01f);
            var sim = TipActivator.SimulateRound(ctx);

            // Closing should be less than naive closing — drift reduces inflation from compounding events
            Assert.Less(sim.ClosingPrice, naiveClosing,
                $"Closing ${sim.ClosingPrice:F2} should be less than naive ${naiveClosing:F2} — drift eliminates inflation");
        }

        [Test]
        public void Drift_HigherMeanReversionSpeed_ProducesMoreDrift()
        {
            // Compare Penny (MRS=0.20, NA=0.08) vs LowValue (MRS=0.35, NA=0.05)
            // Use +20% positive event where both tiers are in the uncapped drift regime:
            //   Penny: force = 0.20 * rawTarget * 0.20, cap = 0.08 * rawTarget * 2 → uncapped
            //   LowValue: force = 0.20 * rawTarget * 0.35, cap = 0.05 * rawTarget * 2 → uncapped
            var events = new[] { MakeEvent(20f, 0.20f) };

            var pennyCtx = CreateContext(startingPrice: 6f, preDecidedEvents: events,
                trendRate: 0.01f, tier: StockTier.Penny);
            var pennyClosing = TipActivator.SimulateRound(pennyCtx).ClosingPrice;

            var lowValueCtx = CreateContext(startingPrice: 20f, preDecidedEvents: events,
                trendRate: 0.01f, tier: StockTier.LowValue);
            var lowValueClosing = TipActivator.SimulateRound(lowValueCtx).ClosingPrice;

            // Compare relative to starting price: LowValue (higher MRS) should drift more
            float pennyRatio = pennyClosing / 6f;
            float lowValueRatio = lowValueClosing / 20f;

            // Both get same +20% event and same trend. LowValue (MRS=0.35) should
            // have lower ratio because stronger mean reversion pulls price down more.
            Assert.Less(lowValueRatio, pennyRatio,
                $"LowValue ratio {lowValueRatio:F4} should be less than Penny ratio {pennyRatio:F4} due to higher MRS");
        }

        [Test]
        public void Drift_ZeroEvents_SimulationUnchanged()
        {
            // Pure trend, no events → no drift → results identical to pre-drift behavior
            var ctx = CreateContext(trend: TrendDirection.Bull, startingPrice: 10f,
                trendRate: 0.01f, preDecidedEvents: null);
            var sim = TipActivator.SimulateRound(ctx);

            // Expected: pure compound growth over full round duration
            float expectedClosing = 10f * UnityEngine.Mathf.Pow(1f + 0.01f, 60f);
            Assert.AreEqual(expectedClosing, sim.ClosingPrice, 0.01f,
                "Zero events should produce pure trend — no drift applied");
            Assert.AreEqual(10f, sim.MinPrice, 0.01f, "Min should be starting price");
            Assert.AreEqual(expectedClosing, sim.MaxPrice, 0.01f, "Max should be closing price");
        }

        [Test]
        public void Drift_NegativeEvent_DriftPullsTowardTrendLine()
        {
            // Large negative event: drift should pull price UP toward trend line
            // Force is self-limiting: deviation * rawTarget * MRS → as rawTarget drops, force drops
            var events = new[] { MakeEvent(20f, -0.40f, false, MarketEventType.MarketCrash) };
            var ctx = CreateContext(startingPrice: 10f, preDecidedEvents: events,
                trendRate: 0.005f, trend: TrendDirection.Neutral);
            var sim = TipActivator.SimulateRound(ctx);

            // Raw target = 10 * (1 - 0.40) = $6.00
            // Trend line ≈ $10 (neutral, no movement)
            // Drift pulls price UP toward trend line
            float rawTarget = 10f * (1f - 0.40f);
            Assert.Greater(sim.ClosingPrice, rawTarget,
                "Negative event drift should pull closing price UP toward trend line");

            // Drift should not overshoot the trend line
            Assert.Less(sim.ClosingPrice, 10f,
                "Drift should not overshoot the trend line");
        }

        [Test]
        public void Drift_DynamicFloor_RespectedAfterDrift()
        {
            // Massive negative event should trigger floor even after drift
            var events = new[] { MakeEvent(10f, -0.95f, false, MarketEventType.MarketCrash) };
            var ctx = CreateContext(startingPrice: 5f, preDecidedEvents: events, trendRate: 0.001f,
                trend: TrendDirection.Neutral);
            var sim = TipActivator.SimulateRound(ctx);

            float expectedFloor = 5f * GameConfig.PriceFloorPercent;
            Assert.GreaterOrEqual(sim.MinPrice, expectedFloor,
                $"Min ${sim.MinPrice:F2} should not breach floor ${expectedFloor:F2}");
            Assert.GreaterOrEqual(sim.ClosingPrice, expectedFloor,
                $"Closing ${sim.ClosingPrice:F2} should not breach floor ${expectedFloor:F2}");
        }

        // === Story 18.7, AC 8: Display text tests ===

        [Test]
        public void DisplayText_PriceCeiling_ContainsTilde()
        {
            var events = new[] { MakeEvent(20f, 0.30f) };
            var ctx = CreateContext(startingPrice: 6f, preDecidedEvents: events);
            var tips = new List<RevealedTip>
            {
                new RevealedTip(InsiderTipType.PriceCeiling, "generic", 0f)
            };

            TipActivator.ActivateTips(tips, ctx);

            Assert.IsTrue(tips[0].DisplayText.Contains("~$"),
                $"PriceCeiling display should contain ~$, got: {tips[0].DisplayText}");
            Assert.IsFalse(tips[0].DisplayText.Contains("at $"),
                "PriceCeiling should not contain bare 'at $' without tilde");
        }

        [Test]
        public void DisplayText_PriceFloor_ContainsTilde()
        {
            var events = new[] { MakeEvent(20f, -0.20f, false, MarketEventType.EarningsMiss) };
            var ctx = CreateContext(startingPrice: 6f, preDecidedEvents: events);
            var tips = new List<RevealedTip>
            {
                new RevealedTip(InsiderTipType.PriceFloor, "generic", 0f)
            };

            TipActivator.ActivateTips(tips, ctx);

            Assert.IsTrue(tips[0].DisplayText.Contains("~$"),
                $"PriceFloor display should contain ~$, got: {tips[0].DisplayText}");
        }

        [Test]
        public void DisplayText_PriceForecast_ContainsTilde()
        {
            var events = new[] { MakeEvent(20f, 0.30f) };
            var ctx = CreateContext(startingPrice: 6f, preDecidedEvents: events);
            var tips = new List<RevealedTip>
            {
                new RevealedTip(InsiderTipType.PriceForecast, "generic", 0f)
            };

            TipActivator.ActivateTips(tips, ctx);

            Assert.IsTrue(tips[0].DisplayText.Contains("~$"),
                $"PriceForecast display should contain ~$, got: {tips[0].DisplayText}");
        }

        [Test]
        public void DisplayText_ClosingDirection_DoesNotContainTilde()
        {
            var events = new[] { MakeEvent(20f, 0.30f) };
            var ctx = CreateContext(trend: TrendDirection.Bull, startingPrice: 10f,
                preDecidedEvents: events, trendRate: 0.01f);
            var tips = new List<RevealedTip>
            {
                new RevealedTip(InsiderTipType.ClosingDirection, "generic", 0f)
            };

            TipActivator.ActivateTips(tips, ctx);

            Assert.IsFalse(tips[0].DisplayText.Contains("~"),
                $"ClosingDirection display should NOT contain ~, got: {tips[0].DisplayText}");
        }

        [Test]
        public void OverlayLabels_PriceTypes_ContainTilde()
        {
            var events = new[] { MakeEvent(20f, 0.30f) };
            var ctx = CreateContext(startingPrice: 6f, preDecidedEvents: events);
            var tips = new List<RevealedTip>
            {
                new RevealedTip(InsiderTipType.PriceCeiling, "generic", 0f),
                new RevealedTip(InsiderTipType.PriceFloor, "generic", 0f),
                new RevealedTip(InsiderTipType.PriceForecast, "generic", 0f)
            };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            Assert.IsTrue(overlays[0].Label.Contains("~$"),
                $"CEILING label should contain ~$, got: {overlays[0].Label}");
            Assert.IsTrue(overlays[1].Label.Contains("~$"),
                $"FLOOR label should contain ~$, got: {overlays[1].Label}");
            Assert.IsTrue(overlays[2].Label.Contains("~$"),
                $"FORECAST label should contain ~$, got: {overlays[2].Label}");
        }

        [Test]
        public void OverlayLabels_NonPriceTypes_DoNotContainTilde()
        {
            var events = new[] { MakeEvent(20f, 0.30f) };
            var ctx = CreateContext(startingPrice: 6f, preDecidedEvents: events);
            var tips = new List<RevealedTip>
            {
                new RevealedTip(InsiderTipType.EventTiming, "generic", 0f),
                new RevealedTip(InsiderTipType.DipMarker, "generic", 0f),
                new RevealedTip(InsiderTipType.PeakMarker, "generic", 0f)
            };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            Assert.IsFalse(overlays[0].Label.Contains("~"),
                $"EVENT TIMING label should not contain ~, got: {overlays[0].Label}");
            Assert.IsFalse(overlays[1].Label.Contains("~"),
                $"DIP ZONE label should not contain ~, got: {overlays[1].Label}");
            Assert.IsFalse(overlays[2].Label.Contains("~"),
                $"PEAK ZONE label should not contain ~, got: {overlays[2].Label}");
        }
    }
}
