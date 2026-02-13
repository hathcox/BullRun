using NUnit.Framework;

namespace BullRun.Tests.Shop
{
    [TestFixture]
    public class ShopTransactionTests
    {
        private RunContext _ctx;
        private ShopTransaction _transaction;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            _ctx = new RunContext(1, 1, new Portfolio(1000f));
            _ctx.Portfolio.StartRound(_ctx.Portfolio.Cash);
            _transaction = new ShopTransaction();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
        }

        private ShopItemDef MakeItem(string id, string name, int cost)
        {
            return new ShopItemDef(id, name, "Test item", cost, ItemRarity.Common, ItemCategory.TradingTool);
        }

        // === AC 1: Purchase deducts item cost atomically ===

        [Test]
        public void TryPurchase_DeductsExactCostFromCash()
        {
            var item = MakeItem("test-item", "Test Item", 300);
            var result = _transaction.TryPurchase(_ctx, item);

            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.AreEqual(700f, _ctx.Portfolio.Cash, 0.01f);
        }

        [Test]
        public void TryPurchase_ReturnsInsufficientFunds_WhenCashLessThanCost()
        {
            var item = MakeItem("expensive", "Expensive", 1500);
            var result = _transaction.TryPurchase(_ctx, item);

            Assert.AreEqual(ShopPurchaseResult.InsufficientFunds, result);
            Assert.AreEqual(1000f, _ctx.Portfolio.Cash, 0.01f, "Cash should be unchanged");
        }

        [Test]
        public void TryPurchase_ReturnsAlreadyOwned_WhenItemInActiveItems()
        {
            _ctx.ActiveItems.Add("owned-item");
            var item = MakeItem("owned-item", "Owned Item", 100);

            var result = _transaction.TryPurchase(_ctx, item);

            Assert.AreEqual(ShopPurchaseResult.AlreadyOwned, result);
            Assert.AreEqual(1000f, _ctx.Portfolio.Cash, 0.01f, "Cash should be unchanged");
            Assert.AreEqual(1, _ctx.ActiveItems.Count, "Should still have only the original item");
        }

        // === AC 6: Purchased items added to RunContext.ActiveItems ===

        [Test]
        public void TryPurchase_AddsItemIdToActiveItems()
        {
            var item = MakeItem("new-item", "New Item", 200);
            _transaction.TryPurchase(_ctx, item);

            Assert.AreEqual(1, _ctx.ActiveItems.Count);
            Assert.IsTrue(_ctx.ActiveItems.Contains("new-item"));
        }

        // === AC 8: ShopItemPurchasedEvent fires with correct data ===

        [Test]
        public void TryPurchase_PublishesShopItemPurchasedEvent_OnSuccess()
        {
            ShopItemPurchasedEvent received = default;
            bool eventFired = false;
            EventBus.Subscribe<ShopItemPurchasedEvent>(e =>
            {
                eventFired = true;
                received = e;
            });

            var item = MakeItem("signal-item", "Signal Item", 250);
            _transaction.TryPurchase(_ctx, item);

            Assert.IsTrue(eventFired, "ShopItemPurchasedEvent should fire on success");
            Assert.AreEqual("signal-item", received.ItemId);
            Assert.AreEqual("Signal Item", received.ItemName);
            Assert.AreEqual(250, received.Cost);
            Assert.AreEqual(750f, received.RemainingCash, 0.01f);
        }

        [Test]
        public void TryPurchase_DoesNotPublishEvent_OnInsufficientFunds()
        {
            bool eventFired = false;
            EventBus.Subscribe<ShopItemPurchasedEvent>(_ => eventFired = true);

            var item = MakeItem("too-costly", "Too Costly", 2000);
            _transaction.TryPurchase(_ctx, item);

            Assert.IsFalse(eventFired);
        }

        [Test]
        public void TryPurchase_DoesNotPublishEvent_OnAlreadyOwned()
        {
            _ctx.ActiveItems.Add("dup");
            bool eventFired = false;
            EventBus.Subscribe<ShopItemPurchasedEvent>(_ => eventFired = true);

            var item = MakeItem("dup", "Duplicate", 100);
            _transaction.TryPurchase(_ctx, item);

            Assert.IsFalse(eventFired);
        }

        // === AC 2: Can buy any combination (0-3 items) ===

        [Test]
        public void TryPurchase_CanBuyMultipleItems()
        {
            var itemA = MakeItem("item-a", "Item A", 200);
            var itemB = MakeItem("item-b", "Item B", 300);
            var itemC = MakeItem("item-c", "Item C", 100);

            Assert.AreEqual(ShopPurchaseResult.Success, _transaction.TryPurchase(_ctx, itemA));
            Assert.AreEqual(ShopPurchaseResult.Success, _transaction.TryPurchase(_ctx, itemB));
            Assert.AreEqual(ShopPurchaseResult.Success, _transaction.TryPurchase(_ctx, itemC));

            Assert.AreEqual(3, _ctx.ActiveItems.Count);
            Assert.AreEqual(400f, _ctx.Portfolio.Cash, 0.01f); // 1000 - 200 - 300 - 100
        }

        // === Boundary tests ===

        [Test]
        public void TryPurchase_Succeeds_WhenCashExactlyEqualsCost()
        {
            var ctx = new RunContext(1, 1, new Portfolio(300f));
            ctx.Portfolio.StartRound(ctx.Portfolio.Cash);
            var item = MakeItem("exact", "Exact Match", 300);

            var result = _transaction.TryPurchase(ctx, item);

            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.AreEqual(0f, ctx.Portfolio.Cash, 0.01f);
        }

        [Test]
        public void TryPurchase_Fails_WhenCashOneDollarShort()
        {
            var ctx = new RunContext(1, 1, new Portfolio(299f));
            ctx.Portfolio.StartRound(ctx.Portfolio.Cash);
            var item = MakeItem("close", "Close But No Cigar", 300);

            var result = _transaction.TryPurchase(ctx, item);

            Assert.AreEqual(ShopPurchaseResult.InsufficientFunds, result);
            Assert.AreEqual(299f, ctx.Portfolio.Cash, 0.01f);
        }

        // === AC 3: Cash carries forward (unspent cash = trading capital) ===

        [Test]
        public void CashAfterPurchases_CarriesForward()
        {
            var item = MakeItem("buy-one", "Buy One", 350);
            _transaction.TryPurchase(_ctx, item);

            // Remaining cash should be available as trading capital
            Assert.AreEqual(650f, _ctx.Portfolio.Cash, 0.01f);
            // Can use this cash for trading (Portfolio.CanAfford confirms)
            Assert.IsTrue(_ctx.Portfolio.CanAfford(650f));
            Assert.IsFalse(_ctx.Portfolio.CanAfford(651f));
        }

        // === Multiple purchases update affordability ===

        [Test]
        public void TryPurchase_ReturnsError_WhenContextIsNull()
        {
            var item = MakeItem("test", "Test", 100);
            var result = _transaction.TryPurchase(null, item);

            Assert.AreEqual(ShopPurchaseResult.Error, result);
        }

        [Test]
        public void MultiplePurchases_ReduceAffordability()
        {
            var itemA = MakeItem("item-a", "Item A", 400);
            var itemB = MakeItem("item-b", "Item B", 400);
            var itemC = MakeItem("item-c", "Item C", 400);

            Assert.AreEqual(ShopPurchaseResult.Success, _transaction.TryPurchase(_ctx, itemA));
            Assert.AreEqual(600f, _ctx.Portfolio.Cash, 0.01f);

            Assert.AreEqual(ShopPurchaseResult.Success, _transaction.TryPurchase(_ctx, itemB));
            Assert.AreEqual(200f, _ctx.Portfolio.Cash, 0.01f);

            // Third item now unaffordable
            Assert.AreEqual(ShopPurchaseResult.InsufficientFunds, _transaction.TryPurchase(_ctx, itemC));
            Assert.AreEqual(200f, _ctx.Portfolio.Cash, 0.01f);
        }
    }
}
