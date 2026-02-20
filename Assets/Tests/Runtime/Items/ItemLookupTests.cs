using NUnit.Framework;

namespace BullRun.Tests.Items
{
    /// <summary>
    /// Story 13.9: Tests for ItemLookup — relic ID resolution only.
    /// Removed legacy GetItemsByCategory, GetRarityColor tests (those methods no longer exist).
    /// GetItemById renamed to GetRelicById.
    /// </summary>
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

        // --- GetRelicById ---

        [Test]
        public void GetRelicById_ValidId_ReturnsCorrectDefinition()
        {
            var result = ItemLookup.GetRelicById("relic_event_trigger");
            Assert.IsTrue(result.HasValue, "Should find relic_event_trigger");
            Assert.AreEqual("Catalyst Trader", result.Value.Name);
        }

        [Test]
        public void GetRelicById_AnotherValidId_ReturnsCorrectDefinition()
        {
            var result = ItemLookup.GetRelicById("relic_short_multiplier");
            Assert.IsTrue(result.HasValue, "Should find relic_short_multiplier");
            Assert.AreEqual("Bear Raid", result.Value.Name);
        }

        [Test]
        public void GetRelicById_UnknownId_ReturnsNull()
        {
            var result = ItemLookup.GetRelicById("nonexistent_relic");
            Assert.IsFalse(result.HasValue, "Unknown ID should return null");
        }

        [Test]
        public void GetRelicById_EmptyString_ReturnsNull()
        {
            var result = ItemLookup.GetRelicById("");
            Assert.IsFalse(result.HasValue, "Empty string should return null");
        }

        // --- Cache behavior ---

        [Test]
        public void GetRelicById_MultipleCallsSameId_ReturnsSameResult()
        {
            var first = ItemLookup.GetRelicById("relic_event_trigger");
            var second = ItemLookup.GetRelicById("relic_event_trigger");
            Assert.IsTrue(first.HasValue && second.HasValue);
            Assert.AreEqual(first.Value.Id, second.Value.Id);
        }

        [Test]
        public void ClearCache_ThenLookup_StillWorks()
        {
            ItemLookup.GetRelicById("relic_event_trigger"); // Builds cache
            ItemLookup.ClearCache();
            var result = ItemLookup.GetRelicById("relic_event_trigger"); // Rebuilds cache
            Assert.IsTrue(result.HasValue);
            Assert.AreEqual("Catalyst Trader", result.Value.Name);
        }

        [Test]
        public void GetRelicById_AllPoolRelics_Resolvable()
        {
            for (int i = 0; i < ShopItemDefinitions.RelicPool.Length; i++)
            {
                var relic = ShopItemDefinitions.RelicPool[i];
                var result = ItemLookup.GetRelicById(relic.Id);
                Assert.IsTrue(result.HasValue, $"Relic {relic.Id} should be resolvable via ItemLookup");
                Assert.AreEqual(relic.Name, result.Value.Name);
            }
        }
    }
}
