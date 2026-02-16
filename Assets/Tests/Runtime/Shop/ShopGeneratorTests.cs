using System.Collections.Generic;
using NUnit.Framework;

namespace BullRun.Tests.Shop
{
    /// <summary>
    /// Story 13.9: Tests for ShopGenerator — uniform random relic offering only.
    /// All legacy rarity-weighted selection tests removed (GenerateOffering, SelectItem,
    /// SelectWeightedRarity no longer exist).
    /// </summary>
    [TestFixture]
    public class ShopGeneratorTests
    {
        private System.Random _random;
        private List<string> _emptyOwned;

        [SetUp]
        public void SetUp()
        {
            _random = new System.Random(42);
            _emptyOwned = new List<string>();
        }

        // === Relic Offering Tests (Uniform Random, No Rarity) ===

        [Test]
        public void GenerateRelicOffering_ReturnsThreeRelics()
        {
            var rng = new System.Random(42);
            var offering = ShopGenerator.GenerateRelicOffering(new List<string>(), rng);
            Assert.AreEqual(3, offering.Length);
        }

        [Test]
        public void GenerateRelicOffering_AllRelicsHaveValidIds()
        {
            var rng = new System.Random(42);
            var offering = ShopGenerator.GenerateRelicOffering(new List<string>(), rng);
            for (int i = 0; i < offering.Length; i++)
            {
                Assert.IsTrue(offering[i].HasValue, $"Relic at index {i} is null");
                Assert.IsFalse(string.IsNullOrEmpty(offering[i].Value.Id),
                    $"Relic at index {i} has empty Id");
            }
        }

        [Test]
        public void GenerateRelicOffering_NoDuplicateRelics()
        {
            var rng = new System.Random(42);
            for (int run = 0; run < 50; run++)
            {
                var offering = ShopGenerator.GenerateRelicOffering(new List<string>(), rng);
                var ids = new HashSet<string>();
                for (int i = 0; i < offering.Length; i++)
                {
                    if (offering[i].HasValue)
                    {
                        Assert.IsTrue(ids.Add(offering[i].Value.Id),
                            $"Duplicate relic in offering: {offering[i].Value.Id}");
                    }
                }
            }
        }

        [Test]
        public void GenerateRelicOffering_OwnedRelicsExcluded()
        {
            var owned = new List<string> { "relic_stop_loss", "relic_speed_trader" };
            var rng = new System.Random(99);

            for (int i = 0; i < 50; i++)
            {
                var offering = ShopGenerator.GenerateRelicOffering(owned, rng);
                for (int j = 0; j < offering.Length; j++)
                {
                    if (offering[j].HasValue)
                    {
                        Assert.AreNotEqual("relic_stop_loss", offering[j].Value.Id);
                        Assert.AreNotEqual("relic_speed_trader", offering[j].Value.Id);
                    }
                }
            }
        }

        [Test]
        public void GenerateRelicOffering_PoolExhausted_ReturnsNulls()
        {
            // Own all 8 relics — pool should be exhausted
            var allOwned = new List<string>();
            for (int i = 0; i < ShopItemDefinitions.RelicPool.Length; i++)
                allOwned.Add(ShopItemDefinitions.RelicPool[i].Id);

            var rng = new System.Random(42);
            var offering = ShopGenerator.GenerateRelicOffering(allOwned, rng);

            for (int i = 0; i < offering.Length; i++)
            {
                Assert.IsFalse(offering[i].HasValue,
                    $"Slot {i} should be null when pool is exhausted");
            }
        }

        [Test]
        public void GenerateRelicOffering_PartialExhaustion_SomeNulls()
        {
            // Own 6 of 8 relics — only 2 available, third slot should be null
            var owned = new List<string>();
            for (int i = 0; i < 6; i++)
                owned.Add(ShopItemDefinitions.RelicPool[i].Id);

            var rng = new System.Random(42);
            var offering = ShopGenerator.GenerateRelicOffering(owned, rng);

            int nonNullCount = 0;
            for (int i = 0; i < offering.Length; i++)
            {
                if (offering[i].HasValue) nonNullCount++;
            }

            Assert.AreEqual(2, nonNullCount, "Only 2 relics should be available");
        }

        [Test]
        public void GenerateRelicOffering_UniformDistribution()
        {
            // Run many iterations and check that all relics appear with roughly equal frequency
            var rng = new System.Random(123);
            var counts = new Dictionary<string, int>();

            for (int i = 0; i < 1000; i++)
            {
                var offering = ShopGenerator.GenerateRelicOffering(new List<string>(), rng);
                for (int j = 0; j < offering.Length; j++)
                {
                    if (offering[j].HasValue)
                    {
                        string id = offering[j].Value.Id;
                        if (!counts.ContainsKey(id)) counts[id] = 0;
                        counts[id]++;
                    }
                }
            }

            // All 8 relics should appear at least once in 1000 iterations
            Assert.AreEqual(ShopItemDefinitions.RelicPool.Length, counts.Count,
                "All relics should appear at least once in 1000 iterations");
        }

        [Test]
        public void GenerateRelicOffering_Deterministic_SameSeed()
        {
            var rng1 = new System.Random(12345);
            var rng2 = new System.Random(12345);

            var offering1 = ShopGenerator.GenerateRelicOffering(new List<string>(), rng1);
            var offering2 = ShopGenerator.GenerateRelicOffering(new List<string>(), rng2);

            for (int i = 0; i < offering1.Length; i++)
            {
                Assert.AreEqual(offering1[i].HasValue, offering2[i].HasValue);
                if (offering1[i].HasValue)
                {
                    Assert.AreEqual(offering1[i].Value.Id, offering2[i].Value.Id,
                        $"Different relics at index {i} with same seed");
                }
            }
        }

        [Test]
        public void BuildAvailablePool_ExcludesOwnedRelics()
        {
            var owned = new List<string> { "relic_stop_loss" };
            var pool = ShopGenerator.BuildAvailablePool(owned);

            Assert.AreEqual(ShopItemDefinitions.RelicPool.Length - 1, pool.Count);
            for (int i = 0; i < pool.Count; i++)
            {
                Assert.AreNotEqual("relic_stop_loss", pool[i].Id);
            }
        }

        [Test]
        public void BuildAvailablePool_EmptyOwned_ReturnsFullPool()
        {
            var pool = ShopGenerator.BuildAvailablePool(new List<string>());
            Assert.AreEqual(ShopItemDefinitions.RelicPool.Length, pool.Count);
        }

        [Test]
        public void GenerateRelicOffering_WithAdditionalExcludes_DoesNotRepeatExcluded()
        {
            var rng = new System.Random(42);
            var owned = new List<string>();
            // Exclude 3 specific relics as "currently displayed"
            var excludes = new List<string> { "relic_stop_loss", "relic_speed_trader", "relic_insider_tip" };

            for (int run = 0; run < 50; run++)
            {
                var offering = ShopGenerator.GenerateRelicOffering(owned, excludes, rng);
                for (int i = 0; i < offering.Length; i++)
                {
                    if (offering[i].HasValue)
                    {
                        Assert.IsFalse(excludes.Contains(offering[i].Value.Id),
                            $"Excluded relic {offering[i].Value.Id} should not appear in offering");
                    }
                }
            }
        }

        [Test]
        public void BuildAvailablePool_WithAdditionalExcludes_FiltersAll()
        {
            var owned = new List<string> { "relic_stop_loss" };
            var excludes = new List<string> { "relic_speed_trader" };
            var pool = ShopGenerator.BuildAvailablePool(owned, excludes);

            Assert.AreEqual(ShopItemDefinitions.RelicPool.Length - 2, pool.Count);
            for (int i = 0; i < pool.Count; i++)
            {
                Assert.AreNotEqual("relic_stop_loss", pool[i].Id);
                Assert.AreNotEqual("relic_speed_trader", pool[i].Id);
            }
        }

        [Test]
        public void GenerateRelicOffering_MultipleCallsProduceVariety()
        {
            var seenIds = new HashSet<string>();

            for (int i = 0; i < 50; i++)
            {
                var offering = ShopGenerator.GenerateRelicOffering(_emptyOwned, _random);
                for (int j = 0; j < offering.Length; j++)
                {
                    if (offering[j].HasValue)
                        seenIds.Add(offering[j].Value.Id);
                }
            }

            Assert.Greater(seenIds.Count, 3, "Generator always returns the same items — no variety");
        }

        [Test]
        public void GenerateRelicOffering_AllRelicsHavePositiveCost()
        {
            var rng = new System.Random(42);
            var offering = ShopGenerator.GenerateRelicOffering(new List<string>(), rng);

            for (int i = 0; i < offering.Length; i++)
            {
                Assert.IsTrue(offering[i].HasValue);
                Assert.Greater(offering[i].Value.Cost, 0,
                    $"Relic {offering[i].Value.Id} has non-positive cost");
            }
        }
    }
}
