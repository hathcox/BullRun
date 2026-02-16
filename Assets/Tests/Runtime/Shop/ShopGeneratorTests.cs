using System.Collections.Generic;
using NUnit.Framework;

namespace BullRun.Tests.Shop
{
    [TestFixture]
    public class ShopGeneratorTests
    {
        private System.Random _random;
        private List<string> _emptyOwned;
        private HashSet<string> _allUnlocked;

        [SetUp]
        public void SetUp()
        {
            _random = new System.Random(42);
            _emptyOwned = new List<string>();
            _allUnlocked = ShopItemDefinitions.DefaultUnlockedItems;
        }

        [Test]
        public void GenerateOffering_ReturnsThreeItems()
        {
            var offering = ShopGenerator.GenerateOffering(_emptyOwned, _allUnlocked, _random);
            Assert.AreEqual(3, offering.Length);
        }

        [Test]
        public void GenerateOffering_OnePerCategory()
        {
            var offering = ShopGenerator.GenerateOffering(_emptyOwned, _allUnlocked, _random);

            Assert.IsTrue(offering[0].HasValue);
            Assert.IsTrue(offering[1].HasValue);
            Assert.IsTrue(offering[2].HasValue);
            Assert.AreEqual(ItemCategory.TradingTool, offering[0].Value.Category);
            Assert.AreEqual(ItemCategory.MarketIntel, offering[1].Value.Category);
            Assert.AreEqual(ItemCategory.PassivePerk, offering[2].Value.Category);
        }

        [Test]
        public void GenerateOffering_AllItemsHaveValidIds()
        {
            var offering = ShopGenerator.GenerateOffering(_emptyOwned, _allUnlocked, _random);

            for (int i = 0; i < offering.Length; i++)
            {
                Assert.IsTrue(offering[i].HasValue, $"Item at index {i} is null");
                Assert.IsFalse(string.IsNullOrEmpty(offering[i].Value.Id),
                    $"Item at index {i} has empty Id");
            }
        }

        [Test]
        public void GenerateOffering_AllItemsHavePositiveCost()
        {
            var offering = ShopGenerator.GenerateOffering(_emptyOwned, _allUnlocked, _random);

            for (int i = 0; i < offering.Length; i++)
            {
                Assert.IsTrue(offering[i].HasValue);
                Assert.Greater(offering[i].Value.Cost, 0,
                    $"Item {offering[i].Value.Id} has non-positive cost");
            }
        }

        [Test]
        public void GenerateOffering_MultipleCallsProduceVariety()
        {
            var seenIds = new HashSet<string>();

            for (int i = 0; i < 50; i++)
            {
                var offering = ShopGenerator.GenerateOffering(_emptyOwned, _allUnlocked, _random);
                for (int j = 0; j < offering.Length; j++)
                {
                    if (offering[j].HasValue)
                        seenIds.Add(offering[j].Value.Id);
                }
            }

            Assert.Greater(seenIds.Count, 3, "Generator always returns the same items — no variety");
        }

        [Test]
        public void SelectItem_ReturnsTradingTool_WhenCategoryIsTradingTool()
        {
            var item = ShopGenerator.SelectItem(ItemCategory.TradingTool, _emptyOwned, _allUnlocked, _random);
            Assert.IsTrue(item.HasValue);
            Assert.AreEqual(ItemCategory.TradingTool, item.Value.Category);
        }

        [Test]
        public void SelectItem_ReturnsMarketIntel_WhenCategoryIsMarketIntel()
        {
            var item = ShopGenerator.SelectItem(ItemCategory.MarketIntel, _emptyOwned, _allUnlocked, _random);
            Assert.IsTrue(item.HasValue);
            Assert.AreEqual(ItemCategory.MarketIntel, item.Value.Category);
        }

        [Test]
        public void SelectItem_ReturnsPassivePerk_WhenCategoryIsPassivePerk()
        {
            var item = ShopGenerator.SelectItem(ItemCategory.PassivePerk, _emptyOwned, _allUnlocked, _random);
            Assert.IsTrue(item.HasValue);
            Assert.AreEqual(ItemCategory.PassivePerk, item.Value.Category);
        }

        [Test]
        public void GenerateOffering_NoDuplicateCategories()
        {
            for (int i = 0; i < 20; i++)
            {
                var offering = ShopGenerator.GenerateOffering(_emptyOwned, _allUnlocked, _random);
                var categories = new HashSet<ItemCategory>();
                for (int j = 0; j < offering.Length; j++)
                {
                    if (offering[j].HasValue)
                    {
                        Assert.IsTrue(categories.Add(offering[j].Value.Category),
                            $"Duplicate category in offering: {offering[j].Value.Category}");
                    }
                }
            }
        }

        // === Weighted Distribution Tests ===

        [Test]
        public void WeightedSelection_RespectsRarityDistribution()
        {
            // Use MarketIntel — has all 4 rarities including Legendary (Wiretap)
            // TradingTool has 0 Legendary items, which would make the Legendary assertion vacuous
            var rng = new System.Random(123);
            int commonCount = 0;
            int uncommonCount = 0;
            int rareCount = 0;
            int legendaryCount = 0;
            int total = 2000;

            for (int i = 0; i < total; i++)
            {
                var item = ShopGenerator.SelectItem(ItemCategory.MarketIntel, _emptyOwned, _allUnlocked, rng);
                Assert.IsTrue(item.HasValue);
                switch (item.Value.Rarity)
                {
                    case ItemRarity.Common: commonCount++; break;
                    case ItemRarity.Uncommon: uncommonCount++; break;
                    case ItemRarity.Rare: rareCount++; break;
                    case ItemRarity.Legendary: legendaryCount++; break;
                }
            }

            float commonPct = (float)commonCount / total;
            float uncommonPct = (float)uncommonCount / total;
            float rarePct = (float)rareCount / total;
            float legendaryPct = (float)legendaryCount / total;

            // Within +/-10% tolerance of expected values (weights: 50/30/15/5)
            Assert.Greater(commonPct, 0.40f, $"Common too low: {commonPct:P1}");
            Assert.Less(commonPct, 0.60f, $"Common too high: {commonPct:P1}");
            Assert.Greater(uncommonPct, 0.20f, $"Uncommon too low: {uncommonPct:P1}");
            Assert.Less(uncommonPct, 0.40f, $"Uncommon too high: {uncommonPct:P1}");
            Assert.Greater(rarePct, 0.05f, $"Rare too low: {rarePct:P1}");
            Assert.Less(rarePct, 0.25f, $"Rare too high: {rarePct:P1}");
            Assert.Greater(legendaryPct, 0.01f, $"Legendary too low: {legendaryPct:P1}");
            Assert.Less(legendaryPct, 0.15f, $"Legendary too high: {legendaryPct:P1}");
        }

        // === Duplicate Prevention Tests ===

        [Test]
        public void DuplicatePrevention_OwnedItemsNeverAppear()
        {
            var owned = new List<string> { "tool_stop_loss", "tool_limit_order" };
            var rng = new System.Random(99);

            for (int i = 0; i < 100; i++)
            {
                var item = ShopGenerator.SelectItem(ItemCategory.TradingTool, owned, _allUnlocked, rng);
                Assert.IsTrue(item.HasValue);
                Assert.AreNotEqual("tool_stop_loss", item.Value.Id);
                Assert.AreNotEqual("tool_limit_order", item.Value.Id);
            }
        }

        [Test]
        public void DuplicatePrevention_AllToolsOwned_ReturnsNull()
        {
            var allTools = new List<string>
            {
                "tool_stop_loss", "tool_limit_order", "tool_speed_trader", "tool_flash_trade",
                "tool_margin_boost", "tool_portfolio_hedge", "tool_leverage", "tool_options_contract",
                "tool_dark_pool", "tool_algo_bot"
            };

            var item = ShopGenerator.SelectItem(ItemCategory.TradingTool, allTools, _allUnlocked, _random);
            Assert.IsFalse(item.HasValue, "Should return null when all items in category are owned");
        }

        [Test]
        public void DuplicatePrevention_OneCategoryExhausted_OthersTwoStillWork()
        {
            var allTools = new List<string>
            {
                "tool_stop_loss", "tool_limit_order", "tool_speed_trader", "tool_flash_trade",
                "tool_margin_boost", "tool_portfolio_hedge", "tool_leverage", "tool_options_contract",
                "tool_dark_pool", "tool_algo_bot"
            };

            var offering = ShopGenerator.GenerateOffering(allTools, _allUnlocked, _random);
            Assert.IsFalse(offering[0].HasValue, "TradingTool slot should be null");
            Assert.IsTrue(offering[1].HasValue, "MarketIntel slot should still have items");
            Assert.IsTrue(offering[2].HasValue, "PassivePerk slot should still have items");
        }

        // === Unlock Filtering Tests ===

        [Test]
        public void UnlockFiltering_RestrictedPoolOnlyShowsUnlockedItems()
        {
            var restrictedPool = new HashSet<string>
            {
                "tool_stop_loss",
                "intel_analyst_report",
                "perk_volume_discount",
                "tool_limit_order",
                "intel_earnings_calendar"
            };
            var rng = new System.Random(77);

            for (int i = 0; i < 50; i++)
            {
                var offering = ShopGenerator.GenerateOffering(_emptyOwned, restrictedPool, rng);
                for (int j = 0; j < offering.Length; j++)
                {
                    if (offering[j].HasValue)
                    {
                        Assert.IsTrue(restrictedPool.Contains(offering[j].Value.Id),
                            $"Item {offering[j].Value.Id} not in unlock pool");
                    }
                }
            }
        }

        [Test]
        public void UnlockFiltering_EmptyUnlockPool_ReturnsAllNulls()
        {
            var emptyPool = new HashSet<string>();
            var offering = ShopGenerator.GenerateOffering(_emptyOwned, emptyPool, _random);

            Assert.IsFalse(offering[0].HasValue, "TradingTool should be null with empty pool");
            Assert.IsFalse(offering[1].HasValue, "MarketIntel should be null with empty pool");
            Assert.IsFalse(offering[2].HasValue, "PassivePerk should be null with empty pool");
        }

        // === Deterministic Tests ===

        [Test]
        public void Deterministic_SameSeedProducesSameItems()
        {
            var rng1 = new System.Random(12345);
            var rng2 = new System.Random(12345);

            var offering1 = ShopGenerator.GenerateOffering(_emptyOwned, _allUnlocked, rng1);
            var offering2 = ShopGenerator.GenerateOffering(_emptyOwned, _allUnlocked, rng2);

            for (int i = 0; i < offering1.Length; i++)
            {
                Assert.AreEqual(offering1[i].HasValue, offering2[i].HasValue);
                if (offering1[i].HasValue)
                {
                    Assert.AreEqual(offering1[i].Value.Id, offering2[i].Value.Id,
                        $"Different items at index {i} with same seed");
                }
            }
        }

        // === SelectWeightedRarity Tests ===

        [Test]
        public void SelectWeightedRarity_OnlyOneRarity_AlwaysSelectsThatRarity()
        {
            var grouped = new Dictionary<ItemRarity, List<ShopItemDef>>
            {
                { ItemRarity.Rare, new List<ShopItemDef> { new ShopItemDef("test", "Test", "Desc", 100, ItemRarity.Rare, ItemCategory.TradingTool) } }
            };
            var rng = new System.Random(55);

            for (int i = 0; i < 50; i++)
            {
                var result = ShopGenerator.SelectWeightedRarity(grouped, rng);
                Assert.AreEqual(ItemRarity.Rare, result);
            }
        }

        // === Story 13.3: Relic Offering Tests (Uniform Random, No Rarity) ===

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
    }
}
