using NUnit.Framework;

namespace BullRun.Tests.Shop
{
    /// <summary>
    /// FIX-12: Shop purchases deduct Reputation, NOT Portfolio.Cash.
    /// All tests updated to verify Reputation flow and cash isolation.
    /// </summary>
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
            // FIX-12: Seed Reputation for tests (shop uses Rep, not cash)
            _ctx.Reputation.Add(1000);
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

        // === FIX-12 AC 4: Purchase deducts Reputation, not cash ===

        [Test]
        public void TryPurchase_DeductsExactCostFromReputation()
        {
            var item = MakeItem("test-item", "Test Item", 300);
            var result = _transaction.TryPurchase(_ctx, item);

            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.AreEqual(700, _ctx.Reputation.Current);
        }

        // === FIX-12 AC 5: Portfolio.Cash is NEVER reduced by shop purchases ===

        [Test]
        public void TryPurchase_DoesNotTouchPortfolioCash()
        {
            float cashBefore = _ctx.Portfolio.Cash;
            var item = MakeItem("test-item", "Test Item", 300);
            _transaction.TryPurchase(_ctx, item);

            Assert.AreEqual(cashBefore, _ctx.Portfolio.Cash, 0.01f, "Cash must be unchanged after shop purchase");
        }

        [Test]
        public void TryPurchase_ReturnsInsufficientFunds_WhenRepLessThanCost()
        {
            _ctx.Reputation.Reset();
            _ctx.Reputation.Add(100); // Only 100 Rep
            var item = MakeItem("expensive", "Expensive", 1500);
            var result = _transaction.TryPurchase(_ctx, item);

            Assert.AreEqual(ShopPurchaseResult.InsufficientFunds, result);
            Assert.AreEqual(100, _ctx.Reputation.Current, "Rep should be unchanged");
        }

        [Test]
        public void TryPurchase_ReturnsAlreadyOwned_WhenItemInOwnedRelics()
        {
            _ctx.OwnedRelics.Add("owned-item");
            var item = MakeItem("owned-item", "Owned Item", 100);

            var result = _transaction.TryPurchase(_ctx, item);

            Assert.AreEqual(ShopPurchaseResult.AlreadyOwned, result);
            Assert.AreEqual(1000, _ctx.Reputation.Current, "Rep should be unchanged");
            Assert.AreEqual(1, _ctx.OwnedRelics.Count, "Should still have only the original item");
        }

        // === Purchased items added to RunContext.OwnedRelics ===

        [Test]
        public void TryPurchase_AddsItemIdToOwnedRelics()
        {
            var item = MakeItem("new-item", "New Item", 200);
            _transaction.TryPurchase(_ctx, item);

            Assert.AreEqual(1, _ctx.OwnedRelics.Count);
            Assert.IsTrue(_ctx.OwnedRelics.Contains("new-item"));
        }

        // === FIX-12 AC 10: ShopItemPurchasedEvent fires with Reputation data ===

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
            Assert.AreEqual(750, received.RemainingReputation);
        }

        [Test]
        public void TryPurchase_DoesNotPublishEvent_OnInsufficientFunds()
        {
            _ctx.Reputation.Reset(); // 0 Rep
            bool eventFired = false;
            EventBus.Subscribe<ShopItemPurchasedEvent>(_ => eventFired = true);

            var item = MakeItem("too-costly", "Too Costly", 2000);
            _transaction.TryPurchase(_ctx, item);

            Assert.IsFalse(eventFired);
        }

        [Test]
        public void TryPurchase_DoesNotPublishEvent_OnAlreadyOwned()
        {
            _ctx.OwnedRelics.Add("dup");
            bool eventFired = false;
            EventBus.Subscribe<ShopItemPurchasedEvent>(_ => eventFired = true);

            var item = MakeItem("dup", "Duplicate", 100);
            _transaction.TryPurchase(_ctx, item);

            Assert.IsFalse(eventFired);
        }

        // === Can buy multiple items (deducts Rep each time) ===

        [Test]
        public void TryPurchase_CanBuyMultipleItems()
        {
            var itemA = MakeItem("item-a", "Item A", 200);
            var itemB = MakeItem("item-b", "Item B", 300);
            var itemC = MakeItem("item-c", "Item C", 100);

            Assert.AreEqual(ShopPurchaseResult.Success, _transaction.TryPurchase(_ctx, itemA));
            Assert.AreEqual(ShopPurchaseResult.Success, _transaction.TryPurchase(_ctx, itemB));
            Assert.AreEqual(ShopPurchaseResult.Success, _transaction.TryPurchase(_ctx, itemC));

            Assert.AreEqual(3, _ctx.OwnedRelics.Count);
            Assert.AreEqual(400, _ctx.Reputation.Current); // 1000 - 200 - 300 - 100
            // FIX-12 AC 5: Cash untouched
            Assert.AreEqual(1000f, _ctx.Portfolio.Cash, 0.01f);
        }

        // === Boundary tests ===

        [Test]
        public void TryPurchase_Succeeds_WhenRepExactlyEqualsCost()
        {
            var ctx = new RunContext(1, 1, new Portfolio(1000f));
            ctx.Portfolio.StartRound(ctx.Portfolio.Cash);
            ctx.Reputation.Add(300);
            var item = MakeItem("exact", "Exact Match", 300);

            var result = _transaction.TryPurchase(ctx, item);

            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.AreEqual(0, ctx.Reputation.Current);
        }

        [Test]
        public void TryPurchase_Fails_WhenRepOneShort()
        {
            var ctx = new RunContext(1, 1, new Portfolio(1000f));
            ctx.Portfolio.StartRound(ctx.Portfolio.Cash);
            ctx.Reputation.Add(299);
            var item = MakeItem("close", "Close But No Cigar", 300);

            var result = _transaction.TryPurchase(ctx, item);

            Assert.AreEqual(ShopPurchaseResult.InsufficientFunds, result);
            Assert.AreEqual(299, ctx.Reputation.Current);
        }

        // === FIX-12 AC 5: Cash carries forward unaffected by shop ===

        [Test]
        public void CashAfterPurchases_IsUnaffected()
        {
            var item = MakeItem("buy-one", "Buy One", 350);
            _transaction.TryPurchase(_ctx, item);

            // Cash should be completely untouched — full trading capital preserved
            Assert.AreEqual(1000f, _ctx.Portfolio.Cash, 0.01f);
            Assert.IsTrue(_ctx.Portfolio.CanAfford(1000f));
        }

        [Test]
        public void TryPurchase_ReturnsError_WhenContextIsNull()
        {
            var item = MakeItem("test", "Test", 100);
            var result = _transaction.TryPurchase(null, item);

            Assert.AreEqual(ShopPurchaseResult.Error, result);
        }

        [Test]
        public void MultiplePurchases_ReduceReputationAffordability()
        {
            var itemA = MakeItem("item-a", "Item A", 400);
            var itemB = MakeItem("item-b", "Item B", 400);
            var itemC = MakeItem("item-c", "Item C", 400);

            Assert.AreEqual(ShopPurchaseResult.Success, _transaction.TryPurchase(_ctx, itemA));
            Assert.AreEqual(600, _ctx.Reputation.Current);

            Assert.AreEqual(ShopPurchaseResult.Success, _transaction.TryPurchase(_ctx, itemB));
            Assert.AreEqual(200, _ctx.Reputation.Current);

            // Third item now unaffordable (200 Rep < 400 cost)
            Assert.AreEqual(ShopPurchaseResult.InsufficientFunds, _transaction.TryPurchase(_ctx, itemC));
            Assert.AreEqual(200, _ctx.Reputation.Current);
            // Cash still untouched
            Assert.AreEqual(1000f, _ctx.Portfolio.Cash, 0.01f);
        }
    }
}
