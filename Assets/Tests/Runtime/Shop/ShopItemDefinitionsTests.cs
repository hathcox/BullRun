using System.Collections.Generic;
using NUnit.Framework;

namespace BullRun.Tests.Shop
{
    [TestFixture]
    public class ShopItemDefinitionsTests
    {
        [Test]
        public void AllItems_Has30Items()
        {
            Assert.AreEqual(30, ShopItemDefinitions.AllItems.Length);
        }

        [Test]
        public void AllItems_Has10TradingTools()
        {
            int count = 0;
            for (int i = 0; i < ShopItemDefinitions.AllItems.Length; i++)
            {
                if (ShopItemDefinitions.AllItems[i].Category == ItemCategory.TradingTool)
                    count++;
            }
            Assert.AreEqual(10, count);
        }

        [Test]
        public void AllItems_Has10MarketIntel()
        {
            int count = 0;
            for (int i = 0; i < ShopItemDefinitions.AllItems.Length; i++)
            {
                if (ShopItemDefinitions.AllItems[i].Category == ItemCategory.MarketIntel)
                    count++;
            }
            Assert.AreEqual(10, count);
        }

        [Test]
        public void AllItems_Has10PassivePerks()
        {
            int count = 0;
            for (int i = 0; i < ShopItemDefinitions.AllItems.Length; i++)
            {
                if (ShopItemDefinitions.AllItems[i].Category == ItemCategory.PassivePerk)
                    count++;
            }
            Assert.AreEqual(10, count);
        }

        [Test]
        public void AllItems_HaveValidCosts()
        {
            for (int i = 0; i < ShopItemDefinitions.AllItems.Length; i++)
            {
                var item = ShopItemDefinitions.AllItems[i];
                Assert.Greater(item.Cost, 0, $"Item {item.Id} has invalid cost {item.Cost}");
            }
        }

        [Test]
        public void AllItems_HaveNonEmptyNames()
        {
            for (int i = 0; i < ShopItemDefinitions.AllItems.Length; i++)
            {
                var item = ShopItemDefinitions.AllItems[i];
                Assert.IsFalse(string.IsNullOrEmpty(item.Name), $"Item {item.Id} has empty name");
            }
        }

        [Test]
        public void AllItems_HaveNonEmptyDescriptions()
        {
            for (int i = 0; i < ShopItemDefinitions.AllItems.Length; i++)
            {
                var item = ShopItemDefinitions.AllItems[i];
                Assert.IsFalse(string.IsNullOrEmpty(item.Description), $"Item {item.Id} has empty description");
            }
        }

        [Test]
        public void AllItems_HaveUniqueIds()
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < ShopItemDefinitions.AllItems.Length; i++)
            {
                var item = ShopItemDefinitions.AllItems[i];
                Assert.IsTrue(seen.Add(item.Id), $"Duplicate item ID: {item.Id}");
            }
        }

        [Test]
        public void AllItems_HaveValidRarities()
        {
            for (int i = 0; i < ShopItemDefinitions.AllItems.Length; i++)
            {
                var item = ShopItemDefinitions.AllItems[i];
                Assert.IsTrue(
                    item.Rarity == ItemRarity.Common ||
                    item.Rarity == ItemRarity.Uncommon ||
                    item.Rarity == ItemRarity.Rare ||
                    item.Rarity == ItemRarity.Legendary,
                    $"Item {item.Id} has invalid rarity");
            }
        }

        [Test]
        public void GetItemsByCategory_ReturnsTradingTools()
        {
            var tools = ShopItemDefinitions.GetItemsByCategory(ItemCategory.TradingTool);
            Assert.AreEqual(10, tools.Length);
            for (int i = 0; i < tools.Length; i++)
            {
                Assert.AreEqual(ItemCategory.TradingTool, tools[i].Category);
            }
        }

        [Test]
        public void GetItemsByCategory_ReturnsMarketIntel()
        {
            var intel = ShopItemDefinitions.GetItemsByCategory(ItemCategory.MarketIntel);
            Assert.AreEqual(10, intel.Length);
            for (int i = 0; i < intel.Length; i++)
            {
                Assert.AreEqual(ItemCategory.MarketIntel, intel[i].Category);
            }
        }

        [Test]
        public void GetItemsByCategory_ReturnsPassivePerks()
        {
            var perks = ShopItemDefinitions.GetItemsByCategory(ItemCategory.PassivePerk);
            Assert.AreEqual(10, perks.Length);
            for (int i = 0; i < perks.Length; i++)
            {
                Assert.AreEqual(ItemCategory.PassivePerk, perks[i].Category);
            }
        }

        // === Rarity Weight Tests ===

        [Test]
        public void RarityWeights_HasFourEntries()
        {
            Assert.AreEqual(4, ShopItemDefinitions.RarityWeights.Length);
        }

        [Test]
        public void RarityWeights_SumTo100()
        {
            float sum = 0f;
            for (int i = 0; i < ShopItemDefinitions.RarityWeights.Length; i++)
            {
                sum += ShopItemDefinitions.RarityWeights[i].Weight;
            }
            Assert.AreEqual(100f, sum, 0.001f);
        }

        [Test]
        public void RarityWeights_AllPositive()
        {
            for (int i = 0; i < ShopItemDefinitions.RarityWeights.Length; i++)
            {
                Assert.Greater(ShopItemDefinitions.RarityWeights[i].Weight, 0f,
                    $"Rarity {ShopItemDefinitions.RarityWeights[i].Rarity} has non-positive weight");
            }
        }

        [Test]
        public void GetWeightForRarity_ReturnsCorrectWeightForCommon()
        {
            Assert.AreEqual(50f, ShopItemDefinitions.GetWeightForRarity(ItemRarity.Common));
        }

        [Test]
        public void GetWeightForRarity_ReturnsCorrectWeightForUncommon()
        {
            Assert.AreEqual(30f, ShopItemDefinitions.GetWeightForRarity(ItemRarity.Uncommon));
        }

        [Test]
        public void GetWeightForRarity_ReturnsCorrectWeightForRare()
        {
            Assert.AreEqual(15f, ShopItemDefinitions.GetWeightForRarity(ItemRarity.Rare));
        }

        [Test]
        public void GetWeightForRarity_ReturnsCorrectWeightForLegendary()
        {
            Assert.AreEqual(5f, ShopItemDefinitions.GetWeightForRarity(ItemRarity.Legendary));
        }

        [Test]
        public void EveryCategoryHasCommonItems()
        {
            var categories = new[] { ItemCategory.TradingTool, ItemCategory.MarketIntel, ItemCategory.PassivePerk };
            foreach (var cat in categories)
            {
                int commonCount = 0;
                for (int i = 0; i < ShopItemDefinitions.AllItems.Length; i++)
                {
                    if (ShopItemDefinitions.AllItems[i].Category == cat && ShopItemDefinitions.AllItems[i].Rarity == ItemRarity.Common)
                        commonCount++;
                }
                Assert.Greater(commonCount, 0, $"Category {cat} has no Common items (needed for rarity fallback)");
            }
        }

        // === Unlock Pool Tests ===

        [Test]
        public void DefaultUnlockedItems_Contains30Items()
        {
            Assert.AreEqual(30, ShopItemDefinitions.DefaultUnlockedItems.Count);
        }

        [Test]
        public void DefaultUnlockedItems_ContainsAllItemIds()
        {
            for (int i = 0; i < ShopItemDefinitions.AllItems.Length; i++)
            {
                Assert.IsTrue(ShopItemDefinitions.DefaultUnlockedItems.Contains(ShopItemDefinitions.AllItems[i].Id),
                    $"Item {ShopItemDefinitions.AllItems[i].Id} missing from DefaultUnlockedItems");
            }
        }

        [Test]
        public void IsUnlocked_ReturnsTrueForUnlockedItem()
        {
            Assert.IsTrue(ShopItemDefinitions.IsUnlocked("tool_stop_loss", ShopItemDefinitions.DefaultUnlockedItems));
        }

        [Test]
        public void IsUnlocked_ReturnsFalseForLockedItem()
        {
            var restrictedPool = new HashSet<string> { "tool_stop_loss" };
            Assert.IsFalse(ShopItemDefinitions.IsUnlocked("tool_limit_order", restrictedPool));
        }

        [Test]
        public void GetUnlockedItems_ReturnsAllItemsWithDefaultPool()
        {
            var unlocked = ShopItemDefinitions.GetUnlockedItems(ShopItemDefinitions.DefaultUnlockedItems);
            Assert.AreEqual(30, unlocked.Count);
        }

        [Test]
        public void GetUnlockedItems_ReturnsSubsetWithRestrictedPool()
        {
            var restrictedPool = new HashSet<string> { "tool_stop_loss", "intel_wiretap", "perk_master_universe" };
            var unlocked = ShopItemDefinitions.GetUnlockedItems(restrictedPool);
            Assert.AreEqual(3, unlocked.Count);
        }
    }
}
