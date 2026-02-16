using NUnit.Framework;

namespace BullRun.Tests.Shop
{
    /// <summary>
    /// Story 13.3: Tests for relic purchase flow (RelicDef), capacity enforcement,
    /// reroll mechanism, and expanded inventory expansion.
    /// </summary>
    [TestFixture]
    public class RelicPurchaseTests
    {
        private RunContext _ctx;
        private ShopTransaction _transaction;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            _ctx = new RunContext(1, 1, new Portfolio(1000f));
            _ctx.Portfolio.StartRound(_ctx.Portfolio.Cash);
            _ctx.Reputation.Add(1000);
            _transaction = new ShopTransaction();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
        }

        private RelicDef MakeRelic(string id, string name, int cost)
        {
            return new RelicDef(id, name, "Test relic", cost);
        }

        // === Purchase Tests (AC 5) ===

        [Test]
        public void PurchaseRelic_DeductsReputation()
        {
            var relic = MakeRelic("test-relic", "Test Relic", 200);
            var result = _transaction.PurchaseRelic(_ctx, relic);

            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.AreEqual(800, _ctx.Reputation.Current);
        }

        [Test]
        public void PurchaseRelic_AddsToOwnedRelics()
        {
            var relic = MakeRelic("new-relic", "New Relic", 100);
            _transaction.PurchaseRelic(_ctx, relic);

            Assert.AreEqual(1, _ctx.OwnedRelics.Count);
            Assert.IsTrue(_ctx.OwnedRelics.Contains("new-relic"));
        }

        [Test]
        public void PurchaseRelic_FiresShopItemPurchasedEvent()
        {
            ShopItemPurchasedEvent received = default;
            bool eventFired = false;
            EventBus.Subscribe<ShopItemPurchasedEvent>(e =>
            {
                eventFired = true;
                received = e;
            });

            var relic = MakeRelic("event-relic", "Event Relic", 150);
            _transaction.PurchaseRelic(_ctx, relic);

            Assert.IsTrue(eventFired);
            Assert.AreEqual("event-relic", received.ItemId);
            Assert.AreEqual("Event Relic", received.ItemName);
            Assert.AreEqual(150, received.Cost);
            Assert.AreEqual(850, received.RemainingReputation);
        }

        [Test]
        public void PurchaseRelic_DoesNotTouchCash()
        {
            float cashBefore = _ctx.Portfolio.Cash;
            var relic = MakeRelic("test", "Test", 200);
            _transaction.PurchaseRelic(_ctx, relic);

            Assert.AreEqual(cashBefore, _ctx.Portfolio.Cash, 0.01f);
        }

        [Test]
        public void PurchaseRelic_RejectsAlreadyOwned()
        {
            _ctx.OwnedRelics.Add("owned-relic");
            var relic = MakeRelic("owned-relic", "Owned", 100);

            var result = _transaction.PurchaseRelic(_ctx, relic);

            Assert.AreEqual(ShopPurchaseResult.AlreadyOwned, result);
            Assert.AreEqual(1000, _ctx.Reputation.Current);
        }

        [Test]
        public void PurchaseRelic_RejectsInsufficientFunds()
        {
            _ctx.Reputation.Reset();
            _ctx.Reputation.Add(50);
            var relic = MakeRelic("expensive", "Expensive", 100);

            var result = _transaction.PurchaseRelic(_ctx, relic);

            Assert.AreEqual(ShopPurchaseResult.InsufficientFunds, result);
            Assert.AreEqual(50, _ctx.Reputation.Current);
        }

        // === Capacity Tests (AC 11, 12) ===

        [Test]
        public void PurchaseRelic_RejectsAtMaxCapacity()
        {
            for (int i = 0; i < GameConfig.MaxRelicSlots; i++)
                _ctx.OwnedRelics.Add($"relic-{i}");

            var relic = MakeRelic("one-more", "One More", 100);
            var result = _transaction.PurchaseRelic(_ctx, relic);

            Assert.AreEqual(ShopPurchaseResult.SlotsFull, result);
            Assert.AreEqual(1000, _ctx.Reputation.Current);
        }

        [Test]
        public void PurchaseRelic_SucceedsAtOneUnderCapacity()
        {
            for (int i = 0; i < GameConfig.MaxRelicSlots - 1; i++)
                _ctx.OwnedRelics.Add($"relic-{i}");

            var relic = MakeRelic("last-slot", "Last Slot", 100);
            var result = _transaction.PurchaseRelic(_ctx, relic);

            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.AreEqual(GameConfig.MaxRelicSlots, _ctx.OwnedRelics.Count);
        }

        // === Expanded Inventory Tests (AC 13) ===

        [Test]
        public void ExpandedInventory_IncreasesMaxSlots()
        {
            _ctx.OwnedExpansions.Add("expanded_inventory");
            int maxSlots = ShopTransaction.GetEffectiveMaxRelicSlots(_ctx);

            Assert.AreEqual(GameConfig.MaxRelicSlots + 2, maxSlots);
        }

        [Test]
        public void ExpandedInventory_AllowsPurchaseBeyondBaseMax()
        {
            _ctx.OwnedExpansions.Add("expanded_inventory");
            for (int i = 0; i < GameConfig.MaxRelicSlots; i++)
                _ctx.OwnedRelics.Add($"relic-{i}");

            var relic = MakeRelic("beyond-base", "Beyond Base", 100);
            var result = _transaction.PurchaseRelic(_ctx, relic);

            Assert.AreEqual(ShopPurchaseResult.Success, result);
        }

        [Test]
        public void GetEffectiveMaxRelicSlots_DefaultIsMaxRelicSlots()
        {
            Assert.AreEqual(GameConfig.MaxRelicSlots, ShopTransaction.GetEffectiveMaxRelicSlots(_ctx));
        }

        // === Reroll Tests (AC 7, 8, 9) ===

        [Test]
        public void Reroll_DeductsBaseCost()
        {
            bool result = _transaction.TryReroll(_ctx);

            Assert.IsTrue(result);
            Assert.AreEqual(1000 - GameConfig.RerollBaseCost, _ctx.Reputation.Current);
            Assert.AreEqual(1, _ctx.CurrentShopRerollCount);
        }

        [Test]
        public void Reroll_CostIncrementsEachUse()
        {
            // First reroll: base cost
            int cost0 = ShopTransaction.GetRerollCost(0);
            Assert.AreEqual(GameConfig.RerollBaseCost, cost0);

            // Second reroll: base + increment
            int cost1 = ShopTransaction.GetRerollCost(1);
            Assert.AreEqual(GameConfig.RerollBaseCost + GameConfig.RerollCostIncrement, cost1);

            // Third reroll: base + 2*increment
            int cost2 = ShopTransaction.GetRerollCost(2);
            Assert.AreEqual(GameConfig.RerollBaseCost + 2 * GameConfig.RerollCostIncrement, cost2);
        }

        [Test]
        public void Reroll_IncreasesRerollCount()
        {
            _transaction.TryReroll(_ctx);
            Assert.AreEqual(1, _ctx.CurrentShopRerollCount);

            _transaction.TryReroll(_ctx);
            Assert.AreEqual(2, _ctx.CurrentShopRerollCount);
        }

        [Test]
        public void Reroll_FailsWithInsufficientFunds()
        {
            _ctx.Reputation.Reset();
            _ctx.Reputation.Add(1); // Less than RerollBaseCost (5)

            bool result = _transaction.TryReroll(_ctx);

            Assert.IsFalse(result);
            Assert.AreEqual(1, _ctx.Reputation.Current);
            Assert.AreEqual(0, _ctx.CurrentShopRerollCount);
        }

        [Test]
        public void Reroll_CostResetPerShopVisit()
        {
            // Simulate rerolls in one visit
            _transaction.TryReroll(_ctx);
            _transaction.TryReroll(_ctx);
            Assert.AreEqual(2, _ctx.CurrentShopRerollCount);

            // Reset (happens in ShopState.Enter)
            _ctx.CurrentShopRerollCount = 0;
            Assert.AreEqual(0, _ctx.CurrentShopRerollCount);

            // First reroll in new visit should cost base again
            int cost = ShopTransaction.GetRerollCost(_ctx.CurrentShopRerollCount);
            Assert.AreEqual(GameConfig.RerollBaseCost, cost);
        }

        [Test]
        public void Reroll_MultipleRerolls_CumulativeCost()
        {
            int totalSpent = 0;
            for (int i = 0; i < 5; i++)
            {
                int cost = ShopTransaction.GetRerollCost(i);
                totalSpent += cost;
                _transaction.TryReroll(_ctx);
            }

            Assert.AreEqual(1000 - totalSpent, _ctx.Reputation.Current);
            Assert.AreEqual(5, _ctx.CurrentShopRerollCount);
        }

        // === GameConfig Constants Tests (AC 7, 8, 11) ===

        [Test]
        public void GameConfig_MaxRelicSlots_Is5()
        {
            Assert.AreEqual(5, GameConfig.MaxRelicSlots);
        }

        [Test]
        public void GameConfig_RerollBaseCost_Is5()
        {
            Assert.AreEqual(5, GameConfig.RerollBaseCost);
        }

        [Test]
        public void GameConfig_RerollCostIncrement_Is2()
        {
            Assert.AreEqual(2, GameConfig.RerollCostIncrement);
        }

        // === RelicDef Data Tests (AC 4, 16, 17) ===

        [Test]
        public void RelicPool_HasPlaceholderRelics()
        {
            Assert.GreaterOrEqual(ShopItemDefinitions.RelicPool.Length, 5);
            Assert.LessOrEqual(ShopItemDefinitions.RelicPool.Length, 8);
        }

        [Test]
        public void RelicPool_AllRelicsHaveValidData()
        {
            for (int i = 0; i < ShopItemDefinitions.RelicPool.Length; i++)
            {
                var relic = ShopItemDefinitions.RelicPool[i];
                Assert.IsFalse(string.IsNullOrEmpty(relic.Id), $"Relic {i} has empty Id");
                Assert.IsFalse(string.IsNullOrEmpty(relic.Name), $"Relic {i} has empty Name");
                Assert.IsFalse(string.IsNullOrEmpty(relic.Description), $"Relic {i} has empty Description");
                Assert.Greater(relic.Cost, 0, $"Relic {relic.Id} has non-positive cost");
            }
        }

        [Test]
        public void RelicDef_HasNoRarityOrCategory()
        {
            // Verify RelicDef struct only has Id, Name, Description, Cost (no rarity/category)
            var fields = typeof(RelicDef).GetFields();
            var fieldNames = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < fields.Length; i++)
                fieldNames.Add(fields[i].Name);

            Assert.IsTrue(fieldNames.Contains("Id"));
            Assert.IsTrue(fieldNames.Contains("Name"));
            Assert.IsTrue(fieldNames.Contains("Description"));
            Assert.IsTrue(fieldNames.Contains("Cost"));
            Assert.IsFalse(fieldNames.Contains("Rarity"), "RelicDef should not have Rarity field");
            Assert.IsFalse(fieldNames.Contains("Category"), "RelicDef should not have Category field");
        }

        [Test]
        public void GetRelicById_ReturnsCorrectRelic()
        {
            ItemLookup.ClearCache();
            var relic = ItemLookup.GetRelicById("relic_stop_loss");
            Assert.IsTrue(relic.HasValue);
            Assert.AreEqual("relic_stop_loss", relic.Value.Id);
            Assert.AreEqual("Stop-Loss Order", relic.Value.Name);
        }

        [Test]
        public void GetRelicById_ReturnsNullForUnknownId()
        {
            ItemLookup.ClearCache();
            var relic = ItemLookup.GetRelicById("nonexistent_relic");
            Assert.IsFalse(relic.HasValue);
        }
    }
}
