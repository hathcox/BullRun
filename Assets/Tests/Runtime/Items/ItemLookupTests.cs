using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace BullRun.Tests.Items
{
    [TestFixture]
    public class ItemLookupTests
    {
        [SetUp]
        public void SetUp()
        {
            ItemLookup.ClearCache();
        }

        [TearDown]
        public void TearDown()
        {
            ItemLookup.ClearCache();
        }

        // --- GetItemById ---

        [Test]
        public void GetItemById_ValidId_ReturnsCorrectDefinition()
        {
            var result = ItemLookup.GetItemById("tool_stop_loss");
            Assert.IsTrue(result.HasValue, "Should find tool_stop_loss");
            Assert.AreEqual("Stop-Loss Order", result.Value.Name);
            Assert.AreEqual(ItemCategory.TradingTool, result.Value.Category);
        }

        [Test]
        public void GetItemById_AnotherValidId_ReturnsCorrectDefinition()
        {
            var result = ItemLookup.GetItemById("intel_insider_tip");
            Assert.IsTrue(result.HasValue, "Should find intel_insider_tip");
            Assert.AreEqual("Insider Tip", result.Value.Name);
            Assert.AreEqual(ItemCategory.MarketIntel, result.Value.Category);
        }

        [Test]
        public void GetItemById_PerkId_ReturnsCorrectDefinition()
        {
            var result = ItemLookup.GetItemById("perk_golden_parachute");
            Assert.IsTrue(result.HasValue, "Should find perk_golden_parachute");
            Assert.AreEqual("Golden Parachute", result.Value.Name);
            Assert.AreEqual(ItemCategory.PassivePerk, result.Value.Category);
        }

        [Test]
        public void GetItemById_UnknownId_ReturnsNull()
        {
            var result = ItemLookup.GetItemById("nonexistent_item");
            Assert.IsFalse(result.HasValue, "Unknown ID should return null");
        }

        [Test]
        public void GetItemById_EmptyString_ReturnsNull()
        {
            var result = ItemLookup.GetItemById("");
            Assert.IsFalse(result.HasValue, "Empty string should return null");
        }

        // --- GetItemsByCategory ---

        [Test]
        public void GetItemsByCategory_FiltersToolsCorrectly()
        {
            var itemIds = new List<string>
            {
                "tool_stop_loss",
                "intel_insider_tip",
                "perk_volume_discount",
                "tool_limit_order"
            };

            var tools = ItemLookup.GetItemsByCategory(itemIds, ItemCategory.TradingTool);
            Assert.AreEqual(2, tools.Count);
            Assert.AreEqual("tool_stop_loss", tools[0].Id);
            Assert.AreEqual("tool_limit_order", tools[1].Id);
        }

        [Test]
        public void GetItemsByCategory_FiltersIntelCorrectly()
        {
            var itemIds = new List<string>
            {
                "tool_stop_loss",
                "intel_insider_tip",
                "intel_crystal_ball"
            };

            var intel = ItemLookup.GetItemsByCategory(itemIds, ItemCategory.MarketIntel);
            Assert.AreEqual(2, intel.Count);
            Assert.AreEqual("intel_insider_tip", intel[0].Id);
            Assert.AreEqual("intel_crystal_ball", intel[1].Id);
        }

        [Test]
        public void GetItemsByCategory_PreservesInputOrder()
        {
            var itemIds = new List<string>
            {
                "tool_leverage",
                "tool_stop_loss",
                "tool_dark_pool"
            };

            var tools = ItemLookup.GetItemsByCategory(itemIds, ItemCategory.TradingTool);
            Assert.AreEqual(3, tools.Count);
            Assert.AreEqual("tool_leverage", tools[0].Id, "Should preserve input order");
            Assert.AreEqual("tool_stop_loss", tools[1].Id);
            Assert.AreEqual("tool_dark_pool", tools[2].Id);
        }

        [Test]
        public void GetItemsByCategory_NoMatches_ReturnsEmptyList()
        {
            var itemIds = new List<string> { "tool_stop_loss", "tool_limit_order" };

            var perks = ItemLookup.GetItemsByCategory(itemIds, ItemCategory.PassivePerk);
            Assert.AreEqual(0, perks.Count);
        }

        [Test]
        public void GetItemsByCategory_EmptyList_ReturnsEmptyList()
        {
            var result = ItemLookup.GetItemsByCategory(new List<string>(), ItemCategory.TradingTool);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetItemsByCategory_UnknownIds_SkipsGracefully()
        {
            var itemIds = new List<string> { "fake_item", "tool_stop_loss", "another_fake" };

            var tools = ItemLookup.GetItemsByCategory(itemIds, ItemCategory.TradingTool);
            Assert.AreEqual(1, tools.Count);
            Assert.AreEqual("tool_stop_loss", tools[0].Id);
        }

        // --- GetRarityColor ---

        [Test]
        public void GetRarityColor_Common_ReturnsGray()
        {
            var color = ItemLookup.GetRarityColor(ItemRarity.Common);
            Assert.AreEqual(new Color(0.6f, 0.6f, 0.6f, 1f), color);
        }

        [Test]
        public void GetRarityColor_Uncommon_ReturnsGreen()
        {
            var color = ItemLookup.GetRarityColor(ItemRarity.Uncommon);
            Assert.AreEqual(new Color(0.2f, 0.8f, 0.2f, 1f), color);
        }

        [Test]
        public void GetRarityColor_Rare_ReturnsBlue()
        {
            var color = ItemLookup.GetRarityColor(ItemRarity.Rare);
            Assert.AreEqual(new Color(0.3f, 0.5f, 1f, 1f), color);
        }

        [Test]
        public void GetRarityColor_Legendary_ReturnsGold()
        {
            var color = ItemLookup.GetRarityColor(ItemRarity.Legendary);
            Assert.AreEqual(new Color(1f, 0.85f, 0f, 1f), color);
        }

        // --- Cache behavior ---

        [Test]
        public void GetItemById_MultipleCallsSameId_ReturnsSameResult()
        {
            var first = ItemLookup.GetItemById("tool_stop_loss");
            var second = ItemLookup.GetItemById("tool_stop_loss");
            Assert.IsTrue(first.HasValue && second.HasValue);
            Assert.AreEqual(first.Value.Id, second.Value.Id);
        }

        [Test]
        public void ClearCache_ThenLookup_StillWorks()
        {
            ItemLookup.GetItemById("tool_stop_loss"); // Builds cache
            ItemLookup.ClearCache();
            var result = ItemLookup.GetItemById("tool_stop_loss"); // Rebuilds cache
            Assert.IsTrue(result.HasValue);
            Assert.AreEqual("Stop-Loss Order", result.Value.Name);
        }
    }
}
