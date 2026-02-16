using NUnit.Framework;
using UnityEngine;

namespace BullRun.Tests.UI
{
    /// <summary>
    /// Story 13.9: Tests for ItemInventoryPanel formatting, hotkeys, and colors.
    /// Removed legacy category partitioning and rarity color tests
    /// (GetItemsByCategory, GetRarityColor, ItemCategory no longer exist).
    /// </summary>
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

        // --- Relic Border Color ---

        [Test]
        public void RelicBorderColor_IsAmberGold()
        {
            var color = ItemInventoryPanel.RelicBorderColor;
            Assert.AreEqual(1f, color.r, 0.01f);
            Assert.AreEqual(0.7f, color.g, 0.01f);
            Assert.AreEqual(0f, color.b, 0.01f);
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
