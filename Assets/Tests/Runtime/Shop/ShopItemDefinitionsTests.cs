using System.Collections.Generic;
using NUnit.Framework;

namespace BullRun.Tests.Shop
{
    /// <summary>
    /// Story 13.9: Tests for RelicPool definitions.
    /// All legacy AllItems/rarity/category tests removed — those concepts no longer exist.
    /// </summary>
    [TestFixture]
    public class ShopItemDefinitionsTests
    {
        // === RelicPool Tests ===

        [Test]
        public void RelicPool_HasBetween5And8Relics()
        {
            Assert.GreaterOrEqual(ShopItemDefinitions.RelicPool.Length, 5);
            Assert.LessOrEqual(ShopItemDefinitions.RelicPool.Length, 8);
        }

        [Test]
        public void RelicPool_AllRelicsHaveValidCosts()
        {
            for (int i = 0; i < ShopItemDefinitions.RelicPool.Length; i++)
            {
                var relic = ShopItemDefinitions.RelicPool[i];
                Assert.Greater(relic.Cost, 0, $"Relic {relic.Id} has invalid cost {relic.Cost}");
            }
        }

        [Test]
        public void RelicPool_AllRelicsHaveNonEmptyNames()
        {
            for (int i = 0; i < ShopItemDefinitions.RelicPool.Length; i++)
            {
                var relic = ShopItemDefinitions.RelicPool[i];
                Assert.IsFalse(string.IsNullOrEmpty(relic.Name), $"Relic {relic.Id} has empty name");
            }
        }

        [Test]
        public void RelicPool_AllRelicsHaveNonEmptyDescriptions()
        {
            for (int i = 0; i < ShopItemDefinitions.RelicPool.Length; i++)
            {
                var relic = ShopItemDefinitions.RelicPool[i];
                Assert.IsFalse(string.IsNullOrEmpty(relic.Description), $"Relic {relic.Id} has empty description");
            }
        }

        [Test]
        public void RelicPool_AllRelicsHaveUniqueIds()
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < ShopItemDefinitions.RelicPool.Length; i++)
            {
                var relic = ShopItemDefinitions.RelicPool[i];
                Assert.IsTrue(seen.Add(relic.Id), $"Duplicate relic ID: {relic.Id}");
            }
        }

        [Test]
        public void ItemLookup_GetRelicById_ValidId_ReturnsCorrectRelic()
        {
            ItemLookup.ClearCache();
            var relic = ItemLookup.GetRelicById("relic_stop_loss");
            Assert.IsTrue(relic.HasValue);
            Assert.AreEqual("relic_stop_loss", relic.Value.Id);
            Assert.AreEqual("Stop-Loss Order", relic.Value.Name);
        }

        [Test]
        public void ItemLookup_GetRelicById_UnknownId_ReturnsNull()
        {
            ItemLookup.ClearCache();
            var relic = ItemLookup.GetRelicById("nonexistent_relic");
            Assert.IsFalse(relic.HasValue);
        }

        // === RelicDef struct validation ===

        [Test]
        public void RelicDef_HasNoRarityOrCategory()
        {
            var fields = typeof(RelicDef).GetFields();
            var fieldNames = new HashSet<string>();
            for (int i = 0; i < fields.Length; i++)
                fieldNames.Add(fields[i].Name);

            Assert.IsTrue(fieldNames.Contains("Id"));
            Assert.IsTrue(fieldNames.Contains("Name"));
            Assert.IsTrue(fieldNames.Contains("Description"));
            Assert.IsTrue(fieldNames.Contains("Cost"));
            Assert.IsFalse(fieldNames.Contains("Rarity"), "RelicDef should not have Rarity field");
            Assert.IsFalse(fieldNames.Contains("Category"), "RelicDef should not have Category field");
        }

        // === Verify legacy types are removed (AC 3, 4, 5) ===

        [Test]
        public void ItemRarityEnum_DoesNotExist()
        {
            var type = System.Type.GetType("ItemRarity");
            Assert.IsNull(type, "ItemRarity enum should have been removed in Story 13.9");
        }

        [Test]
        public void ItemCategoryEnum_DoesNotExist()
        {
            var type = System.Type.GetType("ItemCategory");
            Assert.IsNull(type, "ItemCategory enum should have been removed in Story 13.9");
        }

        [Test]
        public void ShopItemDefStruct_DoesNotExist()
        {
            var type = System.Type.GetType("ShopItemDef");
            Assert.IsNull(type, "ShopItemDef struct should have been removed in Story 13.9");
        }
    }
}
