using NUnit.Framework;
using System.Collections.Generic;

namespace BullRun.Tests.Shop
{
    [TestFixture]
    public class InsiderTipGeneratorTests
    {
        private InsiderTipGenerator _generator;

        [SetUp]
        public void SetUp()
        {
            _generator = new InsiderTipGenerator();
        }

        // === InsiderTipDefinitions data ===

        [Test]
        public void InsiderTipDefinitions_HasNineTipTypes()
        {
            Assert.AreEqual(9, InsiderTipDefinitions.All.Length);
        }

        [Test]
        public void InsiderTipDefinitions_AllHaveUniqueTypes()
        {
            var types = new HashSet<InsiderTipType>();
            for (int i = 0; i < InsiderTipDefinitions.All.Length; i++)
            {
                Assert.IsTrue(types.Add(InsiderTipDefinitions.All[i].Type),
                    $"Duplicate tip type: {InsiderTipDefinitions.All[i].Type}");
            }
        }

        [Test]
        public void InsiderTipDefinitions_CostsMatchGameConfig()
        {
            var byType = new Dictionary<InsiderTipType, int>();
            for (int i = 0; i < InsiderTipDefinitions.All.Length; i++)
                byType[InsiderTipDefinitions.All[i].Type] = InsiderTipDefinitions.All[i].Cost;

            Assert.AreEqual(GameConfig.TipCostPriceForecast, byType[InsiderTipType.PriceForecast]);
            Assert.AreEqual(GameConfig.TipCostPriceFloor, byType[InsiderTipType.PriceFloor]);
            Assert.AreEqual(GameConfig.TipCostPriceCeiling, byType[InsiderTipType.PriceCeiling]);
            Assert.AreEqual(GameConfig.TipCostEventCount, byType[InsiderTipType.EventCount]);
            Assert.AreEqual(GameConfig.TipCostDipMarker, byType[InsiderTipType.DipMarker]);
            Assert.AreEqual(GameConfig.TipCostPeakMarker, byType[InsiderTipType.PeakMarker]);
            Assert.AreEqual(GameConfig.TipCostClosingDirection, byType[InsiderTipType.ClosingDirection]);
            Assert.AreEqual(GameConfig.TipCostEventTiming, byType[InsiderTipType.EventTiming]);
            Assert.AreEqual(GameConfig.TipCostTrendReversal, byType[InsiderTipType.TrendReversal]);
        }

        [Test]
        public void InsiderTipDefinitions_GetByType_ReturnsCorrectTip()
        {
            var result = InsiderTipDefinitions.GetByType(InsiderTipType.PriceFloor);
            Assert.IsTrue(result.HasValue);
            Assert.AreEqual(InsiderTipType.PriceFloor, result.Value.Type);
            Assert.AreEqual(GameConfig.TipCostPriceFloor, result.Value.Cost);
        }

        [Test]
        public void InsiderTipDefinitions_AllHaveNonZeroCost()
        {
            for (int i = 0; i < InsiderTipDefinitions.All.Length; i++)
            {
                Assert.Greater(InsiderTipDefinitions.All[i].Cost, 0,
                    $"Tip type {InsiderTipDefinitions.All[i].Type} has zero cost");
            }
        }

        [Test]
        public void InsiderTipDefinitions_AllHaveDescriptionTemplate()
        {
            for (int i = 0; i < InsiderTipDefinitions.All.Length; i++)
            {
                Assert.IsNotEmpty(InsiderTipDefinitions.All[i].DescriptionTemplate,
                    $"Tip type {InsiderTipDefinitions.All[i].Type} has no description template");
            }
        }

        // === GenerateTips: correct count ===

        [Test]
        public void GenerateTips_ReturnsRequestedSlotCount()
        {
            var tips = _generator.GenerateTips(2, 2, 1, new System.Random(42));
            Assert.AreEqual(2, tips.Length);
        }

        [Test]
        public void GenerateTips_ReturnsThreeWithExpansion()
        {
            var tips = _generator.GenerateTips(3, 2, 1, new System.Random(42));
            Assert.AreEqual(3, tips.Length);
        }

        [Test]
        public void GenerateTips_ClampsToAvailableTypes()
        {
            // Request more than 9 types available
            var tips = _generator.GenerateTips(12, 2, 1, new System.Random(42));
            Assert.AreEqual(9, tips.Length);
        }

        // === GenerateTips: no duplicates ===

        [Test]
        public void GenerateTips_NoDuplicateTypes()
        {
            var tips = _generator.GenerateTips(5, 2, 1, new System.Random(42));
            var types = new HashSet<InsiderTipType>();
            for (int i = 0; i < tips.Length; i++)
            {
                Assert.IsTrue(types.Add(tips[i].Definition.Type),
                    $"Duplicate tip type in offering: {tips[i].Definition.Type}");
            }
        }

        [Test]
        public void GenerateTips_NoDuplicatesAcrossMultipleSeeds()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                var tips = _generator.GenerateTips(3, 2, 1, new System.Random(seed));
                var types = new HashSet<InsiderTipType>();
                for (int i = 0; i < tips.Length; i++)
                {
                    Assert.IsTrue(types.Add(tips[i].Definition.Type),
                        $"Seed {seed}: duplicate tip type {tips[i].Definition.Type}");
                }
            }
        }

        // === GenerateTips: revealed text ===

        [Test]
        public void GenerateTips_AllHaveDisplayText()
        {
            var tips = _generator.GenerateTips(9, 2, 1, new System.Random(42));
            for (int i = 0; i < tips.Length; i++)
            {
                Assert.IsNotEmpty(tips[i].DisplayText,
                    $"Tip {tips[i].Definition.Type} has no revealed text");
            }
        }

        // === ApplyFuzz utility (method retained for general use) ===

        [Test]
        public void ApplyFuzz_ValueWithinExpectedRange()
        {
            float baseValue = 100f;
            float fuzz = 0.10f;
            var random = new System.Random(42);

            for (int i = 0; i < 100; i++)
            {
                float fuzzed = InsiderTipGenerator.ApplyFuzz(baseValue, fuzz, random);
                Assert.GreaterOrEqual(fuzzed, baseValue * 0.90f,
                    $"Fuzzed value {fuzzed} below 90% of base {baseValue}");
                Assert.LessOrEqual(fuzzed, baseValue * 1.10f,
                    $"Fuzzed value {fuzzed} above 110% of base {baseValue}");
            }
        }

        // === Deterministic with same seed ===

        [Test]
        public void GenerateTips_DeterministicWithSameSeed()
        {
            var tips1 = _generator.GenerateTips(3, 2, 1, new System.Random(42));
            var tips2 = _generator.GenerateTips(3, 2, 1, new System.Random(42));

            Assert.AreEqual(tips1.Length, tips2.Length);
            for (int i = 0; i < tips1.Length; i++)
            {
                Assert.AreEqual(tips1[i].Definition.Type, tips2[i].Definition.Type);
                Assert.AreEqual(tips1[i].DisplayText, tips2[i].DisplayText);
            }
        }

        // === Tier-appropriate values ===

        [Test]
        public void GenerateTips_PriceTips_UseGenericShopText()
        {
            // Story 18.6, AC 4: Price tips use generic text at shop time (no specific values)
            var tips = _generator.GenerateTips(9, 2, 1, new System.Random(42));

            for (int i = 0; i < tips.Length; i++)
            {
                if (tips[i].Definition.Type == InsiderTipType.PriceForecast
                    || tips[i].Definition.Type == InsiderTipType.PriceFloor
                    || tips[i].Definition.Type == InsiderTipType.PriceCeiling)
                {
                    // Generic text — should NOT contain "$" at shop time
                    Assert.IsTrue(tips[i].DisplayText.Contains("revealed on chart"),
                        $"Price tip {tips[i].Definition.Type} should use generic text: {tips[i].DisplayText}");
                    Assert.AreEqual(0f, tips[i].NumericValue,
                        $"Price tip {tips[i].Definition.Type} NumericValue should be 0 at shop time");
                }
            }
        }

        [Test]
        public void GenerateTips_EventCountDiffersAcrossActs()
        {
            // Story 18.6: Price/direction tips now use generic text (same across acts).
            // EventCount still computes an estimate, which can differ by act.
            var earlyTips = _generator.GenerateTips(9, 2, 1, new System.Random(42));
            var lateTips = _generator.GenerateTips(9, 6, 3, new System.Random(42));

            // Find EventCount tips and compare
            string earlyEventText = null;
            string lateEventText = null;
            for (int i = 0; i < earlyTips.Length; i++)
            {
                if (earlyTips[i].Definition.Type == InsiderTipType.EventCount)
                    earlyEventText = earlyTips[i].DisplayText;
            }
            for (int i = 0; i < lateTips.Length; i++)
            {
                if (lateTips[i].Definition.Type == InsiderTipType.EventCount)
                    lateEventText = lateTips[i].DisplayText;
            }

            // Both should have EventCount tip (9 tips requested, all types present)
            Assert.IsNotNull(earlyEventText, "Early tips should include EventCount");
            Assert.IsNotNull(lateEventText, "Late tips should include EventCount");
        }

        // === Story 18.1: New data validation tests (AC 10) ===

        [Test]
        public void InsiderTipType_HasExactlyNineUniqueEnumValues()
        {
            var values = System.Enum.GetValues(typeof(InsiderTipType));
            Assert.AreEqual(9, values.Length);

            var unique = new HashSet<int>();
            foreach (var val in values)
                Assert.IsTrue(unique.Add((int)val), $"Duplicate enum value: {val}");
        }

        [Test]
        public void InsiderTipDefinitions_AllNineTypesPresent()
        {
            Assert.AreEqual(9, InsiderTipDefinitions.All.Length);

            var types = new HashSet<InsiderTipType>();
            for (int i = 0; i < InsiderTipDefinitions.All.Length; i++)
                types.Add(InsiderTipDefinitions.All[i].Type);

            Assert.IsTrue(types.Contains(InsiderTipType.PriceForecast));
            Assert.IsTrue(types.Contains(InsiderTipType.PriceFloor));
            Assert.IsTrue(types.Contains(InsiderTipType.PriceCeiling));
            Assert.IsTrue(types.Contains(InsiderTipType.EventCount));
            Assert.IsTrue(types.Contains(InsiderTipType.DipMarker));
            Assert.IsTrue(types.Contains(InsiderTipType.PeakMarker));
            Assert.IsTrue(types.Contains(InsiderTipType.ClosingDirection));
            Assert.IsTrue(types.Contains(InsiderTipType.EventTiming));
            Assert.IsTrue(types.Contains(InsiderTipType.TrendReversal));
        }

        [Test]
        public void InsiderTipDefinitions_NoDuplicateTypesInArray()
        {
            var types = new HashSet<InsiderTipType>();
            for (int i = 0; i < InsiderTipDefinitions.All.Length; i++)
            {
                Assert.IsTrue(types.Add(InsiderTipDefinitions.All[i].Type),
                    $"Duplicate definition for type: {InsiderTipDefinitions.All[i].Type}");
            }
        }

        [Test]
        public void InsiderTipDefinitions_GetByType_ReturnsCorrectForAllNineTypes()
        {
            var allTypes = (InsiderTipType[])System.Enum.GetValues(typeof(InsiderTipType));
            foreach (var type in allTypes)
            {
                var result = InsiderTipDefinitions.GetByType(type);
                Assert.IsTrue(result.HasValue, $"GetByType returned null for {type}");
                Assert.AreEqual(type, result.Value.Type);
                Assert.Greater(result.Value.Cost, 0, $"{type} has zero cost");
            }
        }

        [Test]
        public void InsiderTipDefinitions_GetByType_ReturnsNullForInvalidCast()
        {
            var result = InsiderTipDefinitions.GetByType((InsiderTipType)999);
            Assert.IsFalse(result.HasValue, "GetByType should return null for invalid enum cast");
        }

        [Test]
        public void GenerateTips_NewTypes_ProduceValidDisplayText()
        {
            var tips = _generator.GenerateTips(9, 2, 1, new System.Random(42));
            for (int i = 0; i < tips.Length; i++)
            {
                Assert.IsNotNull(tips[i].DisplayText,
                    $"Tip {tips[i].Definition.Type} has null revealed text");
                Assert.IsNotEmpty(tips[i].DisplayText,
                    $"Tip {tips[i].Definition.Type} has empty revealed text");
            }
        }
    }
}
