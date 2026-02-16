using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace BullRun.Tests.UI
{
    [TestFixture]
    public class ItemInventoryPanelTests
    {
        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            ItemLookup.ClearCache();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
            ItemLookup.ClearCache();
        }

        // --- Formatting Utilities ---

        [Test]
        public void FormatToolSlot_WithItemName()
        {
            Assert.AreEqual("[Q] Insider Tip", ItemInventoryPanel.FormatToolSlot("Q", "Insider Tip"));
        }

        [Test]
        public void FormatEmptyToolSlot_ShowsDashes()
        {
            Assert.AreEqual("[Q] ---", ItemInventoryPanel.FormatEmptyToolSlot("Q"));
        }

        [Test]
        public void FormatHotkey_WrapsInBrackets()
        {
            Assert.AreEqual("[Q]", ItemInventoryPanel.FormatHotkey("Q"));
            Assert.AreEqual("[E]", ItemInventoryPanel.FormatHotkey("E"));
            Assert.AreEqual("[R]", ItemInventoryPanel.FormatHotkey("R"));
        }

        // --- Hotkey Constants ---

        [Test]
        public void ToolHotkeys_AreQER()
        {
            Assert.AreEqual(3, ItemInventoryPanel.ToolHotkeys.Length);
            Assert.AreEqual("Q", ItemInventoryPanel.ToolHotkeys[0]);
            Assert.AreEqual("E", ItemInventoryPanel.ToolHotkeys[1]);
            Assert.AreEqual("R", ItemInventoryPanel.ToolHotkeys[2]);
        }

        [Test]
        public void MaxToolSlots_IsThree()
        {
            Assert.AreEqual(3, ItemInventoryPanel.MaxToolSlots);
        }

        // --- Item Category Partitioning (via ItemLookup) ---

        [Test]
        public void GetItemsByCategory_SingleTool_ReturnsMatchingItem()
        {
            var items = new List<string> { "tool_stop_loss" };
            var tools = ItemLookup.GetItemsByCategory(items, ItemCategory.TradingTool);
            Assert.AreEqual(1, tools.Count);
            Assert.AreEqual("Stop-Loss Order", tools[0].Name);
        }

        [Test]
        public void GetItemsByCategory_MultipleTools_OrderPreserved()
        {
            var items = new List<string>
            {
                "tool_leverage",
                "intel_insider_tip",
                "tool_stop_loss",
                "tool_dark_pool"
            };
            var tools = ItemLookup.GetItemsByCategory(items, ItemCategory.TradingTool);
            Assert.AreEqual(3, tools.Count);
            Assert.AreEqual("tool_leverage", tools[0].Id, "First tool = Q slot");
            Assert.AreEqual("tool_stop_loss", tools[1].Id, "Second tool = E slot");
            Assert.AreEqual("tool_dark_pool", tools[2].Id, "Third tool = R slot");
        }

        [Test]
        public void GetItemsByCategory_FourTools_ReturnsAll()
        {
            var items = new List<string>
            {
                "tool_stop_loss",
                "tool_limit_order",
                "tool_speed_trader",
                "tool_flash_trade" // 4th tool — exceeds max display
            };
            var tools = ItemLookup.GetItemsByCategory(items, ItemCategory.TradingTool);
            // ItemLookup returns all matching items; panel caps display at MaxToolSlots (3)
            Assert.AreEqual(4, tools.Count, "ItemLookup returns all tools");
            // Panel would only display first 3 in Q/E/R slots
        }

        [Test]
        public void GetItemsByCategory_PassivePerks_ListedCorrectly()
        {
            var items = new List<string>
            {
                "perk_volume_discount",
                "perk_golden_parachute"
            };
            var perks = ItemLookup.GetItemsByCategory(items, ItemCategory.PassivePerk);
            Assert.AreEqual(2, perks.Count);
            Assert.AreEqual("Volume Discount", perks[0].Name);
            Assert.AreEqual("Golden Parachute", perks[1].Name);
        }

        [Test]
        public void GetItemsByCategory_IntelItems_ReturnsCorrectItems()
        {
            var items = new List<string>
            {
                "intel_analyst_report",
                "intel_crystal_ball"
            };
            var intel = ItemLookup.GetItemsByCategory(items, ItemCategory.MarketIntel);
            Assert.AreEqual(2, intel.Count);
            Assert.AreEqual("Analyst Report", intel[0].Name);
            Assert.AreEqual("Crystal Ball", intel[1].Name);
        }

        [Test]
        public void GetItemsByCategory_MixedItems_SortIntoCorrectSections()
        {
            var items = new List<string>
            {
                "tool_stop_loss",
                "intel_insider_tip",
                "perk_volume_discount",
                "tool_leverage",
                "intel_crystal_ball",
                "perk_golden_parachute"
            };

            var tools = ItemLookup.GetItemsByCategory(items, ItemCategory.TradingTool);
            var intel = ItemLookup.GetItemsByCategory(items, ItemCategory.MarketIntel);
            var perks = ItemLookup.GetItemsByCategory(items, ItemCategory.PassivePerk);

            Assert.AreEqual(2, tools.Count, "Should have 2 tools");
            Assert.AreEqual(2, intel.Count, "Should have 2 intel items");
            Assert.AreEqual(2, perks.Count, "Should have 2 perks");
        }

        [Test]
        public void GetItemsByCategory_EmptyOwnedRelics_ReturnsEmptyLists()
        {
            var items = new List<string>();
            var tools = ItemLookup.GetItemsByCategory(items, ItemCategory.TradingTool);
            var intel = ItemLookup.GetItemsByCategory(items, ItemCategory.MarketIntel);
            var perks = ItemLookup.GetItemsByCategory(items, ItemCategory.PassivePerk);

            Assert.AreEqual(0, tools.Count);
            Assert.AreEqual(0, intel.Count);
            Assert.AreEqual(0, perks.Count);
        }

        // --- Rarity Display ---

        [Test]
        public void GetRarityColor_AllTiers_ReturnExpectedColors()
        {
            Assert.AreEqual(new Color(0.6f, 0.6f, 0.6f, 1f), ItemLookup.GetRarityColor(ItemRarity.Common));
            Assert.AreEqual(new Color(0.2f, 0.8f, 0.2f, 1f), ItemLookup.GetRarityColor(ItemRarity.Uncommon));
            Assert.AreEqual(new Color(0.3f, 0.5f, 1f, 1f), ItemLookup.GetRarityColor(ItemRarity.Rare));
            Assert.AreEqual(new Color(1f, 0.85f, 0f, 1f), ItemLookup.GetRarityColor(ItemRarity.Legendary));
        }

        // --- Dimmed/Empty State Colors ---

        [Test]
        public void DimmedColor_IsSubdued()
        {
            Assert.AreEqual(new Color(0.4f, 0.4f, 0.4f, 1f), ItemInventoryPanel.DimmedColor);
        }

        [Test]
        public void DimmedBorderColor_IsFaded()
        {
            Assert.AreEqual(new Color(0.2f, 0.2f, 0.2f, 0.5f), ItemInventoryPanel.DimmedBorderColor);
        }
    }
}
