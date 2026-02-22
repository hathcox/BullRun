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
            float[] fireTimes = null,
            float roundDuration = 60f,
            int seed = 42)
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.Penny, startingPrice, trend, 0.015f);

            int eventCount = fireTimes != null ? fireTimes.Length : 0;

            return new TipActivationContext
            {
                ActiveStock = stock,
                ScheduledEventCount = eventCount,
                ScheduledFireTimes = fireTimes,
                RoundDuration = roundDuration,
                TierConfig = StockTierData.GetTierConfig(StockTier.Penny),
                Random = new System.Random(seed)
            };
        }

        // === Price overlay tests (AC 4) ===

        [Test]
        public void PriceFloor_ProducesPriceLevelOverlay()
        {
            var ctx = CreateContext();
            var tip = new RevealedTip(InsiderTipType.PriceFloor, "FLOOR ~$4.50", 4.50f);
            var tips = new List<RevealedTip> { tip };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            Assert.AreEqual(1, overlays.Count);
            Assert.AreEqual(InsiderTipType.PriceFloor, overlays[0].Type);
            Assert.AreEqual(4.50f, overlays[0].PriceLevel, 0.001f);
            Assert.IsTrue(overlays[0].Label.Contains("FLOOR"));
        }

        [Test]
        public void PriceCeiling_ProducesPriceLevelOverlay()
        {
            var ctx = CreateContext();
            var tip = new RevealedTip(InsiderTipType.PriceCeiling, "CEILING ~$8.00", 8.00f);
            var tips = new List<RevealedTip> { tip };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            Assert.AreEqual(1, overlays.Count);
            Assert.AreEqual(InsiderTipType.PriceCeiling, overlays[0].Type);
            Assert.AreEqual(8.00f, overlays[0].PriceLevel, 0.001f);
            Assert.IsTrue(overlays[0].Label.Contains("CEILING"));
        }

        [Test]
        public void PriceForecast_ProducesBandOverlay()
        {
            var ctx = CreateContext();
            var tip = new RevealedTip(InsiderTipType.PriceForecast, "FORECAST ~$6.50", 6.50f);
            var tips = new List<RevealedTip> { tip };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            Assert.AreEqual(1, overlays.Count);
            Assert.AreEqual(InsiderTipType.PriceForecast, overlays[0].Type);
            Assert.AreEqual(6.50f, overlays[0].BandCenter, 0.001f);
            var tierConfig = StockTierData.GetTierConfig(StockTier.Penny);
            float expectedHalfWidth = (tierConfig.MaxPrice - tierConfig.MinPrice) * 0.12f;
            Assert.AreEqual(expectedHalfWidth, overlays[0].BandHalfWidth, 0.001f);
        }

        // === EventCount tests (AC 5) ===

        [Test]
        public void EventCount_UsesActualScheduledCount()
        {
            var ctx = CreateContext(fireTimes: new float[] { 10f, 20f, 30f });
            var tip = new RevealedTip(InsiderTipType.EventCount, "EVENTS: 2", 0f);
            var tips = new List<RevealedTip> { tip };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            Assert.AreEqual(1, overlays.Count);
            Assert.AreEqual(3, overlays[0].EventCountdown, "Should use actual count (3), not shop estimate (2)");
            Assert.AreEqual("EVENTS: 3", overlays[0].Label);
        }

        [Test]
        public void EventCount_ZeroEvents_ReturnsZeroCountdown()
        {
            var ctx = CreateContext(fireTimes: null);
            var tip = new RevealedTip(InsiderTipType.EventCount, "EVENTS: 0", 0f);
            var tips = new List<RevealedTip> { tip };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            Assert.AreEqual(0, overlays[0].EventCountdown);
        }

        // === DipMarker tests (AC 6) ===

        [Test]
        public void DipMarker_BullTrend_ZoneInFirstThird()
        {
            var ctx = CreateContext(trend: TrendDirection.Bull);
            var tip = new RevealedTip(InsiderTipType.DipMarker, "DIP", 0f);
            var tips = new List<RevealedTip> { tip };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            Assert.Less(overlays[0].TimeZoneCenter, 0.35f,
                $"Bull dip zone center {overlays[0].TimeZoneCenter} should be in first third");
            Assert.AreEqual("DIP ZONE", overlays[0].Label);
        }

        [Test]
        public void DipMarker_BearTrend_ZoneInLastThird()
        {
            var ctx = CreateContext(trend: TrendDirection.Bear);
            var tip = new RevealedTip(InsiderTipType.DipMarker, "DIP", 0f);
            var tips = new List<RevealedTip> { tip };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            Assert.Greater(overlays[0].TimeZoneCenter, 0.65f,
                $"Bear dip zone center {overlays[0].TimeZoneCenter} should be in last third");
        }

        [Test]
        public void DipMarker_NeutralTrend_ZoneNearCenter()
        {
            var ctx = CreateContext(trend: TrendDirection.Neutral);
            var tip = new RevealedTip(InsiderTipType.DipMarker, "DIP", 0f);
            var tips = new List<RevealedTip> { tip };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            Assert.Greater(overlays[0].TimeZoneCenter, 0.35f);
            Assert.Less(overlays[0].TimeZoneCenter, 0.65f);
        }

        [Test]
        public void DipMarker_ZoneWidthIsTenPercent()
        {
            var ctx = CreateContext();
            var tip = new RevealedTip(InsiderTipType.DipMarker, "DIP", 0f);
            var tips = new List<RevealedTip> { tip };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            Assert.AreEqual(0.10f, overlays[0].TimeZoneHalfWidth, 0.001f);
        }

        [Test]
        public void DipMarker_ZoneClamped_NeverOffChart()
        {
            // Test with many seeds to ensure clamping works
            for (int seed = 0; seed < 50; seed++)
            {
                var ctx = CreateContext(seed: seed);
                var tip = new RevealedTip(InsiderTipType.DipMarker, "DIP", 0f);
                var tips = new List<RevealedTip> { tip };

                var overlays = TipActivator.ActivateTips(tips, ctx);

                Assert.GreaterOrEqual(overlays[0].TimeZoneCenter, 0.10f,
                    $"Seed {seed}: center {overlays[0].TimeZoneCenter} below 0.10");
                Assert.LessOrEqual(overlays[0].TimeZoneCenter, 0.90f,
                    $"Seed {seed}: center {overlays[0].TimeZoneCenter} above 0.90");
            }
        }

        // === PeakMarker tests (AC 7) ===

        [Test]
        public void PeakMarker_BullTrend_ZoneInLastThird()
        {
            var ctx = CreateContext(trend: TrendDirection.Bull);
            var tip = new RevealedTip(InsiderTipType.PeakMarker, "PEAK", 0f);
            var tips = new List<RevealedTip> { tip };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            Assert.Greater(overlays[0].TimeZoneCenter, 0.65f,
                $"Bull peak zone center {overlays[0].TimeZoneCenter} should be in last third");
            Assert.AreEqual("PEAK ZONE", overlays[0].Label);
        }

        [Test]
        public void PeakMarker_BearTrend_ZoneInFirstThird()
        {
            var ctx = CreateContext(trend: TrendDirection.Bear);
            var tip = new RevealedTip(InsiderTipType.PeakMarker, "PEAK", 0f);
            var tips = new List<RevealedTip> { tip };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            Assert.Less(overlays[0].TimeZoneCenter, 0.35f,
                $"Bear peak zone center {overlays[0].TimeZoneCenter} should be in first third");
        }

        [Test]
        public void PeakMarker_InverseOfDipMarker()
        {
            // Bull: peak should be later than dip
            var ctxBull = CreateContext(trend: TrendDirection.Bull, seed: 99);
            var dipTip = new RevealedTip(InsiderTipType.DipMarker, "DIP", 0f);
            var peakTip = new RevealedTip(InsiderTipType.PeakMarker, "PEAK", 0f);

            var dipOverlays = TipActivator.ActivateTips(new List<RevealedTip> { dipTip }, ctxBull);
            // Reset random for fair comparison
            var ctxBull2 = CreateContext(trend: TrendDirection.Bull, seed: 99);
            var peakOverlays = TipActivator.ActivateTips(new List<RevealedTip> { peakTip }, ctxBull2);

            Assert.Greater(peakOverlays[0].TimeZoneCenter, dipOverlays[0].TimeZoneCenter,
                "Bull peak should be later than bull dip");

            // Bear: peak should be earlier than dip
            var ctxBear = CreateContext(trend: TrendDirection.Bear, seed: 99);
            dipOverlays = TipActivator.ActivateTips(new List<RevealedTip> { dipTip }, ctxBear);
            var ctxBear2 = CreateContext(trend: TrendDirection.Bear, seed: 99);
            peakOverlays = TipActivator.ActivateTips(new List<RevealedTip> { peakTip }, ctxBear2);

            Assert.Less(peakOverlays[0].TimeZoneCenter, dipOverlays[0].TimeZoneCenter,
                "Bear peak should be earlier than bear dip");
        }

        // === ClosingDirection tests (AC 8) ===

        [Test]
        public void ClosingDirection_BullTrend_ReturnsPositive()
        {
            var ctx = CreateContext(trend: TrendDirection.Bull);
            var tip = new RevealedTip(InsiderTipType.ClosingDirection, "CLOSING", 0f);
            var tips = new List<RevealedTip> { tip };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            Assert.AreEqual(1, overlays[0].DirectionSign);
            Assert.AreEqual("CLOSING UP", overlays[0].Label);
        }

        [Test]
        public void ClosingDirection_BearTrend_ReturnsNegative()
        {
            var ctx = CreateContext(trend: TrendDirection.Bear);
            var tip = new RevealedTip(InsiderTipType.ClosingDirection, "CLOSING", 0f);
            var tips = new List<RevealedTip> { tip };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            Assert.AreEqual(-1, overlays[0].DirectionSign);
            Assert.AreEqual("CLOSING DOWN", overlays[0].Label);
        }

        [Test]
        public void ClosingDirection_NeutralTrend_ReturnsEitherDirection()
        {
            var ctx = CreateContext(trend: TrendDirection.Neutral);
            var tip = new RevealedTip(InsiderTipType.ClosingDirection, "CLOSING", 0f);
            var tips = new List<RevealedTip> { tip };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            Assert.IsTrue(overlays[0].DirectionSign == 1 || overlays[0].DirectionSign == -1,
                $"Neutral direction should be +1 or -1, got {overlays[0].DirectionSign}");
        }

        // === EventTiming tests (AC 9) ===

        [Test]
        public void EventTiming_MarkerCountMatchesEventCount()
        {
            var ctx = CreateContext(fireTimes: new float[] { 10f, 25f, 40f });
            var tip = new RevealedTip(InsiderTipType.EventTiming, "TIMING", 0f);
            var tips = new List<RevealedTip> { tip };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            Assert.AreEqual(3, overlays[0].TimeMarkers.Length);
        }

        [Test]
        public void EventTiming_MarkersNormalized()
        {
            var ctx = CreateContext(fireTimes: new float[] { 5f, 15f, 30f, 55f });
            var tip = new RevealedTip(InsiderTipType.EventTiming, "TIMING", 0f);
            var tips = new List<RevealedTip> { tip };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            for (int i = 0; i < overlays[0].TimeMarkers.Length; i++)
            {
                Assert.GreaterOrEqual(overlays[0].TimeMarkers[i], 0f,
                    $"Marker {i} below 0: {overlays[0].TimeMarkers[i]}");
                Assert.LessOrEqual(overlays[0].TimeMarkers[i], 1f,
                    $"Marker {i} above 1: {overlays[0].TimeMarkers[i]}");
            }
        }

        [Test]
        public void EventTiming_MarkersSorted()
        {
            var ctx = CreateContext(fireTimes: new float[] { 50f, 10f, 30f, 20f });
            var tip = new RevealedTip(InsiderTipType.EventTiming, "TIMING", 0f);
            var tips = new List<RevealedTip> { tip };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            for (int i = 1; i < overlays[0].TimeMarkers.Length; i++)
            {
                Assert.GreaterOrEqual(overlays[0].TimeMarkers[i], overlays[0].TimeMarkers[i - 1],
                    $"Markers not sorted: [{i - 1}]={overlays[0].TimeMarkers[i - 1]} > [{i}]={overlays[0].TimeMarkers[i]}");
            }
        }

        [Test]
        public void EventTiming_MarkersWithinFuzzOfActual()
        {
            float[] fireTimes = { 10f, 25f, 40f };
            float roundDuration = 60f;
            var ctx = CreateContext(fireTimes: fireTimes, roundDuration: roundDuration);
            var tip = new RevealedTip(InsiderTipType.EventTiming, "TIMING", 0f);
            var tips = new List<RevealedTip> { tip };

            var overlays = TipActivator.ActivateTips(tips, ctx);
            var markers = overlays[0].TimeMarkers;

            // Markers are sorted after fuzz so order may differ from input
            // But each actual normalized time should have a marker within ±5%
            for (int j = 0; j < fireTimes.Length; j++)
            {
                float expected = fireTimes[j] / roundDuration;
                bool found = false;
                for (int k = 0; k < markers.Length; k++)
                {
                    if (System.Math.Abs(markers[k] - expected) < 0.05f)
                    {
                        found = true;
                        break;
                    }
                }
                Assert.IsTrue(found,
                    $"No marker within ±5% of actual time {expected:F3}");
            }
        }

        [Test]
        public void EventTiming_NoEvents_ReturnsEmptyArray()
        {
            var ctx = CreateContext(fireTimes: null);
            var tip = new RevealedTip(InsiderTipType.EventTiming, "TIMING", 0f);
            var tips = new List<RevealedTip> { tip };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            Assert.IsNotNull(overlays[0].TimeMarkers);
            Assert.AreEqual(0, overlays[0].TimeMarkers.Length);
            Assert.AreEqual("NO EVENTS", overlays[0].Label);
        }

        // === TrendReversal tests (AC 10) ===

        [Test]
        public void TrendReversal_NeutralTrend_ReturnsNegativeOne()
        {
            var ctx = CreateContext(trend: TrendDirection.Neutral, fireTimes: new float[] { 10f, 20f, 30f });
            var tip = new RevealedTip(InsiderTipType.TrendReversal, "REVERSAL", 0f);
            var tips = new List<RevealedTip> { tip };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            Assert.AreEqual(-1f, overlays[0].ReversalTime, 0.001f);
            Assert.AreEqual("NO REVERSAL", overlays[0].Label);
        }

        [Test]
        public void TrendReversal_NoEvents_ReturnsNegativeOne()
        {
            var ctx = CreateContext(fireTimes: null);
            var tip = new RevealedTip(InsiderTipType.TrendReversal, "REVERSAL", 0f);
            var tips = new List<RevealedTip> { tip };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            Assert.AreEqual(-1f, overlays[0].ReversalTime, 0.001f);
        }

        [Test]
        public void TrendReversal_FewEventsInHalf_ReturnsNegativeOne()
        {
            // Bull: searches back half (0.5-1.0). Only 1 event there → no reversal
            var ctx = CreateContext(trend: TrendDirection.Bull, fireTimes: new float[] { 10f, 50f });
            var tip = new RevealedTip(InsiderTipType.TrendReversal, "REVERSAL", 0f);
            var tips = new List<RevealedTip> { tip };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            Assert.AreEqual(-1f, overlays[0].ReversalTime, 0.001f,
                "Only 1 event in back half should mean no reversal");
        }

        [Test]
        public void TrendReversal_BullWithLateEvents_ReturnsTimeInBackHalf()
        {
            // Bull: 3 events in back half (0.5-1.0)
            var ctx = CreateContext(trend: TrendDirection.Bull,
                fireTimes: new float[] { 5f, 35f, 40f, 45f });
            var tip = new RevealedTip(InsiderTipType.TrendReversal, "REVERSAL", 0f);
            var tips = new List<RevealedTip> { tip };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            Assert.Greater(overlays[0].ReversalTime, 0.50f,
                $"Bull reversal time {overlays[0].ReversalTime} should be in back half");
            Assert.AreEqual("REVERSAL", overlays[0].Label);
        }

        [Test]
        public void TrendReversal_BearWithEarlyEvents_ReturnsTimeInFrontHalf()
        {
            // Bear: 3 events in front half (0.0-0.5)
            var ctx = CreateContext(trend: TrendDirection.Bear,
                fireTimes: new float[] { 10f, 15f, 20f, 55f });
            var tip = new RevealedTip(InsiderTipType.TrendReversal, "REVERSAL", 0f);
            var tips = new List<RevealedTip> { tip };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            Assert.Less(overlays[0].ReversalTime, 0.55f,
                $"Bear reversal time {overlays[0].ReversalTime} should be in front half");
        }

        // === Integration tests (AC 14) ===

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
            var ctx = CreateContext(fireTimes: new float[] { 10f, 20f, 30f });
            var tips = new List<RevealedTip>
            {
                new RevealedTip(InsiderTipType.PriceFloor, "FLOOR", 4.50f),
                new RevealedTip(InsiderTipType.EventCount, "EVENTS", 0f),
                new RevealedTip(InsiderTipType.DipMarker, "DIP", 0f)
            };

            var overlays = TipActivator.ActivateTips(tips, ctx);

            Assert.AreEqual(3, overlays.Count);
            Assert.AreEqual(InsiderTipType.PriceFloor, overlays[0].Type);
            Assert.AreEqual(InsiderTipType.EventCount, overlays[1].Type);
            Assert.AreEqual(InsiderTipType.DipMarker, overlays[2].Type);
        }

        [Test]
        public void ActivateTips_Deterministic_SameSeedSameResults()
        {
            var tips = new List<RevealedTip>
            {
                new RevealedTip(InsiderTipType.DipMarker, "DIP", 0f),
                new RevealedTip(InsiderTipType.PeakMarker, "PEAK", 0f),
                new RevealedTip(InsiderTipType.ClosingDirection, "DIR", 0f),
                new RevealedTip(InsiderTipType.EventTiming, "TIMING", 0f)
            };

            var ctx1 = CreateContext(seed: 42, fireTimes: new float[] { 10f, 30f, 50f });
            var overlays1 = TipActivator.ActivateTips(tips, ctx1);

            var ctx2 = CreateContext(seed: 42, fireTimes: new float[] { 10f, 30f, 50f });
            var overlays2 = TipActivator.ActivateTips(tips, ctx2);

            Assert.AreEqual(overlays1.Count, overlays2.Count);
            for (int i = 0; i < overlays1.Count; i++)
            {
                Assert.AreEqual(overlays1[i].Type, overlays2[i].Type);
                Assert.AreEqual(overlays1[i].TimeZoneCenter, overlays2[i].TimeZoneCenter, 0.0001f,
                    $"Overlay {i} TimeZoneCenter differs");
                Assert.AreEqual(overlays1[i].DirectionSign, overlays2[i].DirectionSign,
                    $"Overlay {i} DirectionSign differs");
                if (overlays1[i].TimeMarkers != null && overlays2[i].TimeMarkers != null)
                {
                    Assert.AreEqual(overlays1[i].TimeMarkers.Length, overlays2[i].TimeMarkers.Length);
                    for (int j = 0; j < overlays1[i].TimeMarkers.Length; j++)
                    {
                        Assert.AreEqual(overlays1[i].TimeMarkers[j], overlays2[i].TimeMarkers[j], 0.0001f,
                            $"Overlay {i} marker {j} differs");
                    }
                }
            }
        }

        // === Sentinel value tests ===

        [Test]
        public void PriceFloor_SentinelValuesCorrect()
        {
            var ctx = CreateContext();
            var tip = new RevealedTip(InsiderTipType.PriceFloor, "FLOOR", 5f);
            var overlays = TipActivator.ActivateTips(new List<RevealedTip> { tip }, ctx);

            Assert.AreEqual(-1f, overlays[0].TimeZoneCenter, 0.001f, "TimeZoneCenter sentinel");
            Assert.AreEqual(-1f, overlays[0].ReversalTime, 0.001f, "ReversalTime sentinel");
            Assert.AreEqual(-1, overlays[0].EventCountdown, "EventCountdown sentinel");
            Assert.AreEqual(0, overlays[0].DirectionSign, "DirectionSign sentinel");
        }

        [Test]
        public void DipMarker_SentinelValuesCorrect()
        {
            var ctx = CreateContext();
            var tip = new RevealedTip(InsiderTipType.DipMarker, "DIP", 0f);
            var overlays = TipActivator.ActivateTips(new List<RevealedTip> { tip }, ctx);

            Assert.AreEqual(0f, overlays[0].PriceLevel, 0.001f, "PriceLevel sentinel");
            Assert.AreEqual(-1f, overlays[0].ReversalTime, 0.001f, "ReversalTime sentinel");
            Assert.AreEqual(-1, overlays[0].EventCountdown, "EventCountdown sentinel");
        }
    }
}
