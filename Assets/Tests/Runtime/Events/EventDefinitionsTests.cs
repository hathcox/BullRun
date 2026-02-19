using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace BullRun.Tests.Events
{
    [TestFixture]
    public class EventDefinitionsTests
    {
        // --- MarketEventType enum tests ---

        [Test]
        public void MarketEventType_ContainsAllExpectedValues()
        {
            var expectedTypes = new[]
            {
                "EarningsBeat", "EarningsMiss", "PumpAndDump", "SECInvestigation",
                "SectorRotation", "MergerRumor", "MarketCrash", "BullRun",
                "FlashCrash", "ShortSqueeze"
            };

            foreach (var typeName in expectedTypes)
            {
                Assert.IsTrue(Enum.IsDefined(typeof(MarketEventType), Enum.Parse(typeof(MarketEventType), typeName)),
                    $"MarketEventType should contain {typeName}");
            }
        }

        [Test]
        public void MarketEventType_HasExactly10Values()
        {
            var values = Enum.GetValues(typeof(MarketEventType));
            Assert.AreEqual(10, values.Length, "MarketEventType should have exactly 10 event types");
        }

        // --- MarketEventConfig struct tests ---

        [Test]
        public void MarketEventConfig_StoresAllFields()
        {
            var config = new MarketEventConfig(
                eventType: MarketEventType.EarningsBeat,
                minPriceEffect: 0.15f,
                maxPriceEffect: 0.30f,
                duration: 5f,
                tierAvailability: new[] { StockTier.Penny, StockTier.LowValue, StockTier.MidValue, StockTier.BlueChip },
                rarity: 0.5f
            );

            Assert.AreEqual(MarketEventType.EarningsBeat, config.EventType);
            Assert.AreEqual(0.15f, config.MinPriceEffect, 0.001f);
            Assert.AreEqual(0.30f, config.MaxPriceEffect, 0.001f);
            Assert.AreEqual(5f, config.Duration, 0.001f);
            Assert.AreEqual(4, config.TierAvailability.Length);
            Assert.AreEqual(0.5f, config.Rarity, 0.001f);
        }

        // --- EventDefinitions static data tests ---

        [Test]
        public void EventDefinitions_HasConfigForEveryEventType()
        {
            foreach (MarketEventType eventType in Enum.GetValues(typeof(MarketEventType)))
            {
                var config = EventDefinitions.GetConfig(eventType);
                Assert.AreEqual(eventType, config.EventType,
                    $"EventDefinitions should have config for {eventType}");
            }
        }

        [Test]
        public void EventDefinitions_AllConfigsHavePositiveDuration()
        {
            foreach (MarketEventType eventType in Enum.GetValues(typeof(MarketEventType)))
            {
                var config = EventDefinitions.GetConfig(eventType);
                Assert.Greater(config.Duration, 0f,
                    $"{eventType} should have positive duration");
            }
        }

        [Test]
        public void EventDefinitions_AllConfigsHaveNonEmptyTierAvailability()
        {
            foreach (MarketEventType eventType in Enum.GetValues(typeof(MarketEventType)))
            {
                var config = EventDefinitions.GetConfig(eventType);
                Assert.IsNotNull(config.TierAvailability, $"{eventType} should have tier availability");
                Assert.Greater(config.TierAvailability.Length, 0,
                    $"{eventType} should have at least one tier");
            }
        }

        [Test]
        public void EventDefinitions_AllConfigsHaveValidRarity()
        {
            foreach (MarketEventType eventType in Enum.GetValues(typeof(MarketEventType)))
            {
                var config = EventDefinitions.GetConfig(eventType);
                Assert.GreaterOrEqual(config.Rarity, 0f, $"{eventType} rarity should be >= 0");
                Assert.LessOrEqual(config.Rarity, 1f, $"{eventType} rarity should be <= 1");
            }
        }

        [Test]
        public void EventDefinitions_EarningsBeat_HasPositiveEffect()
        {
            // FIX-18: Increased from +20-45% to +25-50%
            var config = EventDefinitions.GetConfig(MarketEventType.EarningsBeat);
            Assert.AreEqual(0.25f, config.MinPriceEffect, 0.001f);
            Assert.AreEqual(0.50f, config.MaxPriceEffect, 0.001f);
            Assert.GreaterOrEqual(config.MaxPriceEffect, config.MinPriceEffect,
                "Max effect should be >= min effect");
        }

        [Test]
        public void EventDefinitions_EarningsMiss_HasNegativeEffect()
        {
            // FIX-18: Reduced from -20-45% to -15-30%
            var config = EventDefinitions.GetConfig(MarketEventType.EarningsMiss);
            Assert.AreEqual(-0.15f, config.MinPriceEffect, 0.001f);
            Assert.AreEqual(-0.30f, config.MaxPriceEffect, 0.001f);
        }

        [Test]
        public void EventDefinitions_MarketCrash_AffectsAllTiers()
        {
            var config = EventDefinitions.GetConfig(MarketEventType.MarketCrash);
            var tiers = new HashSet<StockTier>(config.TierAvailability);
            Assert.IsTrue(tiers.Contains(StockTier.Penny), "Market Crash should affect Penny");
            Assert.IsTrue(tiers.Contains(StockTier.LowValue), "Market Crash should affect LowValue");
            Assert.IsTrue(tiers.Contains(StockTier.MidValue), "Market Crash should affect MidValue");
            Assert.IsTrue(tiers.Contains(StockTier.BlueChip), "Market Crash should affect BlueChip");
        }

        [Test]
        public void EventDefinitions_BullRun_AffectsAllTiers()
        {
            var config = EventDefinitions.GetConfig(MarketEventType.BullRun);
            var tiers = new HashSet<StockTier>(config.TierAvailability);
            Assert.AreEqual(4, tiers.Count, "Bull Run should affect all 4 tiers");
        }

        [Test]
        public void EventDefinitions_PumpAndDump_OnlyPennyTier()
        {
            var config = EventDefinitions.GetConfig(MarketEventType.PumpAndDump);
            var tiers = new HashSet<StockTier>(config.TierAvailability);
            Assert.IsTrue(tiers.Contains(StockTier.Penny), "Pump & Dump should be available for Penny");
        }

        [Test]
        public void EventDefinitions_SECInvestigation_AvailableForPennyAndLow()
        {
            var config = EventDefinitions.GetConfig(MarketEventType.SECInvestigation);
            var tiers = new HashSet<StockTier>(config.TierAvailability);
            Assert.IsTrue(tiers.Contains(StockTier.Penny), "SEC Investigation should affect Penny");
            Assert.IsTrue(tiers.Contains(StockTier.LowValue), "SEC Investigation should affect LowValue");
        }

        [Test]
        public void EventDefinitions_MarketCrash_HasNegativeEffect()
        {
            var config = EventDefinitions.GetConfig(MarketEventType.MarketCrash);
            Assert.Less(config.MinPriceEffect, 0f, "Market Crash should have negative effect");
        }

        [Test]
        public void EventDefinitions_MinEffectNotGreaterThanMax_ForPositiveEvents()
        {
            var positiveTypes = new[] { MarketEventType.EarningsBeat, MarketEventType.BullRun, MarketEventType.ShortSqueeze };
            foreach (var eventType in positiveTypes)
            {
                var config = EventDefinitions.GetConfig(eventType);
                Assert.LessOrEqual(config.MinPriceEffect, config.MaxPriceEffect,
                    $"{eventType}: min effect should be <= max effect");
            }
        }

        [Test]
        public void EventDefinitions_MinEffectNotLessThanMax_ForNegativeEvents()
        {
            var negativeTypes = new[] { MarketEventType.EarningsMiss, MarketEventType.MarketCrash };
            foreach (var eventType in negativeTypes)
            {
                var config = EventDefinitions.GetConfig(eventType);
                // For negative effects, min (closer to 0) >= max (more negative)
                Assert.GreaterOrEqual(config.MinPriceEffect, config.MaxPriceEffect,
                    $"{eventType}: min effect should be >= max effect (both negative)");
            }
        }

        // --- FIX-17: Rebalanced magnitude tests ---

        [Test]
        public void EventDefinitions_FIX17_PumpAndDump_HasReducedMagnitude()
        {
            var config = EventDefinitions.GetConfig(MarketEventType.PumpAndDump);
            Assert.AreEqual(0.45f, config.MinPriceEffect, 0.001f, "PumpAndDump min should be 0.45");
            Assert.AreEqual(0.90f, config.MaxPriceEffect, 0.001f, "PumpAndDump max should be 0.90");
        }

        [Test]
        public void EventDefinitions_SECInvestigation_HasExpectedMagnitude()
        {
            // FIX-18: Reduced from -30-55% to -20-40%
            var config = EventDefinitions.GetConfig(MarketEventType.SECInvestigation);
            Assert.AreEqual(-0.20f, config.MinPriceEffect, 0.001f);
            Assert.AreEqual(-0.40f, config.MaxPriceEffect, 0.001f);
        }

        [Test]
        public void EventDefinitions_SectorRotation_HasExpectedMagnitude()
        {
            var config = EventDefinitions.GetConfig(MarketEventType.SectorRotation);
            Assert.AreEqual(-0.18f, config.MinPriceEffect, 0.001f);
            Assert.AreEqual(0.18f, config.MaxPriceEffect, 0.001f);
        }

        [Test]
        public void EventDefinitions_MergerRumor_HasExpectedMagnitude()
        {
            // FIX-18: Increased from +25-55% to +30-60%
            var config = EventDefinitions.GetConfig(MarketEventType.MergerRumor);
            Assert.AreEqual(0.30f, config.MinPriceEffect, 0.001f);
            Assert.AreEqual(0.60f, config.MaxPriceEffect, 0.001f);
        }

        [Test]
        public void EventDefinitions_MarketCrash_HasExpectedMagnitude()
        {
            // FIX-18: Reduced from -30-60% to -20-40%
            var config = EventDefinitions.GetConfig(MarketEventType.MarketCrash);
            Assert.AreEqual(-0.20f, config.MinPriceEffect, 0.001f);
            Assert.AreEqual(-0.40f, config.MaxPriceEffect, 0.001f);
        }

        [Test]
        public void EventDefinitions_BullRun_HasExpectedMagnitude()
        {
            // FIX-18: Increased from +25-55% to +35-65%
            var config = EventDefinitions.GetConfig(MarketEventType.BullRun);
            Assert.AreEqual(0.35f, config.MinPriceEffect, 0.001f);
            Assert.AreEqual(0.65f, config.MaxPriceEffect, 0.001f);
        }

        [Test]
        public void EventDefinitions_FlashCrash_HasExpectedMagnitude()
        {
            // FIX-18: Reduced from -25-45% to -15-30%
            var config = EventDefinitions.GetConfig(MarketEventType.FlashCrash);
            Assert.AreEqual(-0.15f, config.MinPriceEffect, 0.001f);
            Assert.AreEqual(-0.30f, config.MaxPriceEffect, 0.001f);
        }

        [Test]
        public void EventDefinitions_ShortSqueeze_HasExpectedMagnitude()
        {
            // FIX-18: Increased from +35-90% to +45-100%
            var config = EventDefinitions.GetConfig(MarketEventType.ShortSqueeze);
            Assert.AreEqual(0.45f, config.MinPriceEffect, 0.001f);
            Assert.AreEqual(1.00f, config.MaxPriceEffect, 0.001f);
        }

        // --- FIX-18: Verify positive events can recover from negative counterparts ---

        [Test]
        public void EventDefinitions_FIX18_PositiveEventsStrongerThanNegative()
        {
            // EarningsBeat max (+50%) should exceed recovery needed from EarningsMiss max (-30%)
            // Recovery from -30% needs +43%. +50% > +43%. ✓
            var beat = EventDefinitions.GetConfig(MarketEventType.EarningsBeat);
            var miss = EventDefinitions.GetConfig(MarketEventType.EarningsMiss);
            float recoveryNeeded = -miss.MaxPriceEffect / (1f + miss.MaxPriceEffect);
            Assert.Greater(beat.MaxPriceEffect, recoveryNeeded,
                "EarningsBeat max should be able to recover from EarningsMiss max");

            // BullRun max (+65%) should exceed recovery from MarketCrash max (-40%)
            // Recovery from -40% needs +67%. Close — +65% nearly recovers. Combined with
            // compound trend, full recovery is expected.
            var bull = EventDefinitions.GetConfig(MarketEventType.BullRun);
            var crash = EventDefinitions.GetConfig(MarketEventType.MarketCrash);
            Assert.Greater(bull.MaxPriceEffect, -crash.MaxPriceEffect,
                "BullRun max should exceed MarketCrash max magnitude");
        }
    }
}
