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
    }
}
