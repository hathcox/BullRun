using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace BullRun.Tests.Shop
{
    [TestFixture]
    public class ShopGeneratorTests
    {
        [SetUp]
        public void SetUp()
        {
            // Seed RNG for deterministic tests — probabilistic tests should not flake
            Random.InitState(42);
        }

        [Test]
        public void GenerateOffering_ReturnsThreeItems()
        {
            var offering = ShopGenerator.GenerateOffering();
            Assert.AreEqual(3, offering.Length);
        }

        [Test]
        public void GenerateOffering_OnePerCategory()
        {
            var offering = ShopGenerator.GenerateOffering();

            Assert.AreEqual(ItemCategory.TradingTool, offering[0].Category);
            Assert.AreEqual(ItemCategory.MarketIntel, offering[1].Category);
            Assert.AreEqual(ItemCategory.PassivePerk, offering[2].Category);
        }

        [Test]
        public void GenerateOffering_AllItemsHaveValidIds()
        {
            var offering = ShopGenerator.GenerateOffering();

            for (int i = 0; i < offering.Length; i++)
            {
                Assert.IsFalse(string.IsNullOrEmpty(offering[i].Id),
                    $"Item at index {i} has empty Id");
            }
        }

        [Test]
        public void GenerateOffering_AllItemsHavePositiveCost()
        {
            var offering = ShopGenerator.GenerateOffering();

            for (int i = 0; i < offering.Length; i++)
            {
                Assert.Greater(offering[i].Cost, 0,
                    $"Item {offering[i].Id} has non-positive cost");
            }
        }

        [Test]
        public void GenerateOffering_MultipleCallsProduceVariety()
        {
            // Run 50 times and verify we don't always get the same items
            var seenIds = new HashSet<string>();

            for (int i = 0; i < 50; i++)
            {
                var offering = ShopGenerator.GenerateOffering();
                for (int j = 0; j < offering.Length; j++)
                {
                    seenIds.Add(offering[j].Id);
                }
            }

            // With 30 total items and 50 rolls, we should see more than 3 unique items
            Assert.Greater(seenIds.Count, 3, "Generator always returns the same items — no variety");
        }

        [Test]
        public void SelectItem_ReturnsTradingTool_WhenCategoryIsTradingTool()
        {
            var item = ShopGenerator.SelectItem(ItemCategory.TradingTool);
            Assert.AreEqual(ItemCategory.TradingTool, item.Category);
        }

        [Test]
        public void SelectItem_ReturnsMarketIntel_WhenCategoryIsMarketIntel()
        {
            var item = ShopGenerator.SelectItem(ItemCategory.MarketIntel);
            Assert.AreEqual(ItemCategory.MarketIntel, item.Category);
        }

        [Test]
        public void SelectItem_ReturnsPassivePerk_WhenCategoryIsPassivePerk()
        {
            var item = ShopGenerator.SelectItem(ItemCategory.PassivePerk);
            Assert.AreEqual(ItemCategory.PassivePerk, item.Category);
        }

        [Test]
        public void RollRarity_AlwaysReturnsValidRarity()
        {
            for (int i = 0; i < 100; i++)
            {
                var rarity = ShopGenerator.RollRarity();
                Assert.IsTrue(
                    rarity == ItemRarity.Common ||
                    rarity == ItemRarity.Uncommon ||
                    rarity == ItemRarity.Rare ||
                    rarity == ItemRarity.Legendary,
                    $"Invalid rarity: {rarity}");
            }
        }

        [Test]
        public void RollRarity_CommonIsMostFrequent()
        {
            int commonCount = 0;
            int total = 1000;

            for (int i = 0; i < total; i++)
            {
                if (ShopGenerator.RollRarity() == ItemRarity.Common)
                    commonCount++;
            }

            // Common should be ~50%, assert at least 30% to account for randomness
            float ratio = (float)commonCount / total;
            Assert.Greater(ratio, 0.3f, $"Common ratio too low: {ratio:P1}");
        }

        [Test]
        public void RollRarity_LegendaryIsRarest()
        {
            int legendaryCount = 0;
            int total = 1000;

            for (int i = 0; i < total; i++)
            {
                if (ShopGenerator.RollRarity() == ItemRarity.Legendary)
                    legendaryCount++;
            }

            // Legendary should be ~5%, assert less than 15%
            float ratio = (float)legendaryCount / total;
            Assert.Less(ratio, 0.15f, $"Legendary ratio too high: {ratio:P1}");
        }

        [Test]
        public void GenerateOffering_NoDuplicateCategories()
        {
            for (int i = 0; i < 20; i++)
            {
                var offering = ShopGenerator.GenerateOffering();
                var categories = new HashSet<ItemCategory>();
                for (int j = 0; j < offering.Length; j++)
                {
                    Assert.IsTrue(categories.Add(offering[j].Category),
                        $"Duplicate category in offering: {offering[j].Category}");
                }
            }
        }
    }
}
