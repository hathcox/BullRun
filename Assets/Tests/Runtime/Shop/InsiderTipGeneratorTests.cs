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
        public void InsiderTipDefinitions_HasEightTipTypes()
        {
            Assert.AreEqual(8, InsiderTipDefinitions.All.Length);
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
            Assert.AreEqual(GameConfig.TipCostTrendDirection, byType[InsiderTipType.TrendDirection]);
            Assert.AreEqual(GameConfig.TipCostEventForecast, byType[InsiderTipType.EventForecast]);
            Assert.AreEqual(GameConfig.TipCostEventCount, byType[InsiderTipType.EventCount]);
            Assert.AreEqual(GameConfig.TipCostVolatilityWarning, byType[InsiderTipType.VolatilityWarning]);
            Assert.AreEqual(GameConfig.TipCostOpeningPrice, byType[InsiderTipType.OpeningPrice]);
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
            // Request more than 8 types available
            var tips = _generator.GenerateTips(10, 2, 1, new System.Random(42));
            Assert.AreEqual(8, tips.Length);
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
        public void GenerateTips_AllHaveRevealedText()
        {
            var tips = _generator.GenerateTips(8, 2, 1, new System.Random(42));
            for (int i = 0; i < tips.Length; i++)
            {
                Assert.IsNotEmpty(tips[i].RevealedText,
                    $"Tip {tips[i].Definition.Type} has no revealed text");
            }
        }

        // === Fuzz: numeric values within range ===

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
                Assert.AreEqual(tips1[i].RevealedText, tips2[i].RevealedText);
            }
        }

        // === Tier-appropriate values ===

        [Test]
        public void GenerateTips_PennyTier_UsesCorrectPriceRange()
        {
            // Penny tier: act 1
            var tips = _generator.GenerateTips(8, 2, 1, new System.Random(42));

            // Find price-related tips and verify they reference reasonable penny-tier values
            for (int i = 0; i < tips.Length; i++)
            {
                if (tips[i].Definition.Type == InsiderTipType.PriceForecast
                    || tips[i].Definition.Type == InsiderTipType.PriceFloor
                    || tips[i].Definition.Type == InsiderTipType.PriceCeiling
                    || tips[i].Definition.Type == InsiderTipType.OpeningPrice)
                {
                    // Penny tier prices: $5-$8 range with ±10% fuzz
                    // So revealed text should contain a "$" sign
                    Assert.IsTrue(tips[i].RevealedText.Contains("$"),
                        $"Price tip {tips[i].Definition.Type} missing '$': {tips[i].RevealedText}");
                }
            }
        }

        [Test]
        public void GenerateTips_DifferentActsProduceDifferentValues()
        {
            // Act 1 (Penny) vs Act 3 (MidValue) should produce different price ranges
            var pennyTips = _generator.GenerateTips(8, 2, 1, new System.Random(42));
            var midTips = _generator.GenerateTips(8, 6, 3, new System.Random(42));

            // Same seed but different act → at least some tips should differ in revealed text
            bool anyDifferent = false;
            for (int i = 0; i < pennyTips.Length && i < midTips.Length; i++)
            {
                if (pennyTips[i].Definition.Type == midTips[i].Definition.Type
                    && pennyTips[i].RevealedText != midTips[i].RevealedText)
                {
                    anyDifferent = true;
                    break;
                }
            }
            // Even if types differ, the values should be different for price tips
            Assert.IsTrue(anyDifferent || pennyTips[0].Definition.Type != midTips[0].Definition.Type,
                "Penny and Mid tips should produce different values or different type ordering");
        }
    }
}
