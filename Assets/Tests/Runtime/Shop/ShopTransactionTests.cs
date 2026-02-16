using NUnit.Framework;

namespace BullRun.Tests.Shop
{
    /// <summary>
    /// FIX-12: Shop purchases deduct Reputation, NOT Portfolio.Cash.
    /// Story 13.9: Updated to use RelicDef instead of ShopItemDef.
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

        private RelicDef MakeRelic(string id, string name, int cost)
        {
            return new RelicDef(id, name, "Test relic", cost);
        }

        // === FIX-12 AC 4: Purchase deducts Reputation, not cash ===

        [Test]
        public void PurchaseRelic_DeductsExactCostFromReputation()
        {
            var relic = MakeRelic("test-relic", "Test Relic", 300);
            var result = _transaction.PurchaseRelic(_ctx, relic);

            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.AreEqual(700, _ctx.Reputation.Current);
        }

        // === FIX-12 AC 5: Portfolio.Cash is NEVER reduced by shop purchases ===

        [Test]
        public void PurchaseRelic_DoesNotTouchPortfolioCash()
        {
            float cashBefore = _ctx.Portfolio.Cash;
            var relic = MakeRelic("test-relic", "Test Relic", 300);
            _transaction.PurchaseRelic(_ctx, relic);

            Assert.AreEqual(cashBefore, _ctx.Portfolio.Cash, 0.01f, "Cash must be unchanged after shop purchase");
        }

        [Test]
        public void PurchaseRelic_ReturnsInsufficientFunds_WhenRepLessThanCost()
        {
            _ctx.Reputation.Reset();
            _ctx.Reputation.Add(100); // Only 100 Rep
            var relic = MakeRelic("expensive", "Expensive", 1500);
            var result = _transaction.PurchaseRelic(_ctx, relic);

            Assert.AreEqual(ShopPurchaseResult.InsufficientFunds, result);
            Assert.AreEqual(100, _ctx.Reputation.Current, "Rep should be unchanged");
        }

        [Test]
        public void PurchaseRelic_ReturnsAlreadyOwned_WhenItemInOwnedRelics()
        {
            _ctx.OwnedRelics.Add("owned-relic");
            var relic = MakeRelic("owned-relic", "Owned Relic", 100);

            var result = _transaction.PurchaseRelic(_ctx, relic);

            Assert.AreEqual(ShopPurchaseResult.AlreadyOwned, result);
            Assert.AreEqual(1000, _ctx.Reputation.Current, "Rep should be unchanged");
            Assert.AreEqual(1, _ctx.OwnedRelics.Count, "Should still have only the original item");
        }

        // === Purchased items added to RunContext.OwnedRelics ===

        [Test]
        public void PurchaseRelic_AddsItemIdToOwnedRelics()
        {
            var relic = MakeRelic("new-relic", "New Relic", 200);
            _transaction.PurchaseRelic(_ctx, relic);

            Assert.AreEqual(1, _ctx.OwnedRelics.Count);
            Assert.IsTrue(_ctx.OwnedRelics.Contains("new-relic"));
        }

        // === FIX-12 AC 10: ShopItemPurchasedEvent fires with Reputation data ===

        [Test]
        public void PurchaseRelic_PublishesShopItemPurchasedEvent_OnSuccess()
        {
            ShopItemPurchasedEvent received = default;
            bool eventFired = false;
            EventBus.Subscribe<ShopItemPurchasedEvent>(e =>
            {
                eventFired = true;
                received = e;
            });

            var relic = MakeRelic("signal-relic", "Signal Relic", 250);
            _transaction.PurchaseRelic(_ctx, relic);

            Assert.IsTrue(eventFired, "ShopItemPurchasedEvent should fire on success");
            Assert.AreEqual("signal-relic", received.ItemId);
            Assert.AreEqual("Signal Relic", received.ItemName);
            Assert.AreEqual(250, received.Cost);
            Assert.AreEqual(750, received.RemainingReputation);
        }

        [Test]
        public void PurchaseRelic_DoesNotPublishEvent_OnInsufficientFunds()
        {
            _ctx.Reputation.Reset(); // 0 Rep
            bool eventFired = false;
            EventBus.Subscribe<ShopItemPurchasedEvent>(_ => eventFired = true);

            var relic = MakeRelic("too-costly", "Too Costly", 2000);
            _transaction.PurchaseRelic(_ctx, relic);

            Assert.IsFalse(eventFired);
        }

        [Test]
        public void PurchaseRelic_DoesNotPublishEvent_OnAlreadyOwned()
        {
            _ctx.OwnedRelics.Add("dup");
            bool eventFired = false;
            EventBus.Subscribe<ShopItemPurchasedEvent>(_ => eventFired = true);

            var relic = MakeRelic("dup", "Duplicate", 100);
            _transaction.PurchaseRelic(_ctx, relic);

            Assert.IsFalse(eventFired);
        }

        // === Can buy multiple items (deducts Rep each time) ===

        [Test]
        public void PurchaseRelic_CanBuyMultipleItems()
        {
            var relicA = MakeRelic("relic-a", "Relic A", 200);
            var relicB = MakeRelic("relic-b", "Relic B", 300);
            var relicC = MakeRelic("relic-c", "Relic C", 100);

            Assert.AreEqual(ShopPurchaseResult.Success, _transaction.PurchaseRelic(_ctx, relicA));
            Assert.AreEqual(ShopPurchaseResult.Success, _transaction.PurchaseRelic(_ctx, relicB));
            Assert.AreEqual(ShopPurchaseResult.Success, _transaction.PurchaseRelic(_ctx, relicC));

            Assert.AreEqual(3, _ctx.OwnedRelics.Count);
            Assert.AreEqual(400, _ctx.Reputation.Current); // 1000 - 200 - 300 - 100
            // FIX-12 AC 5: Cash untouched
            Assert.AreEqual(1000f, _ctx.Portfolio.Cash, 0.01f);
        }

        // === Boundary tests ===

        [Test]
        public void PurchaseRelic_Succeeds_WhenRepExactlyEqualsCost()
        {
            var ctx = new RunContext(1, 1, new Portfolio(1000f));
            ctx.Portfolio.StartRound(ctx.Portfolio.Cash);
            ctx.Reputation.Add(300);
            var relic = MakeRelic("exact", "Exact Match", 300);

            var result = _transaction.PurchaseRelic(ctx, relic);

            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.AreEqual(0, ctx.Reputation.Current);
        }

        [Test]
        public void PurchaseRelic_Fails_WhenRepOneShort()
        {
            var ctx = new RunContext(1, 1, new Portfolio(1000f));
            ctx.Portfolio.StartRound(ctx.Portfolio.Cash);
            ctx.Reputation.Add(299);
            var relic = MakeRelic("close", "Close But No Cigar", 300);

            var result = _transaction.PurchaseRelic(ctx, relic);

            Assert.AreEqual(ShopPurchaseResult.InsufficientFunds, result);
            Assert.AreEqual(299, ctx.Reputation.Current);
        }

        // === FIX-12 AC 5: Cash carries forward unaffected by shop ===

        [Test]
        public void CashAfterPurchases_IsUnaffected()
        {
            var relic = MakeRelic("buy-one", "Buy One", 350);
            _transaction.PurchaseRelic(_ctx, relic);

            // Cash should be completely untouched — full trading capital preserved
            Assert.AreEqual(1000f, _ctx.Portfolio.Cash, 0.01f);
            Assert.IsTrue(_ctx.Portfolio.CanAfford(1000f));
        }

        [Test]
        public void MultiplePurchases_ReduceReputationAffordability()
        {
            var relicA = MakeRelic("relic-a", "Relic A", 400);
            var relicB = MakeRelic("relic-b", "Relic B", 400);
            var relicC = MakeRelic("relic-c", "Relic C", 400);

            Assert.AreEqual(ShopPurchaseResult.Success, _transaction.PurchaseRelic(_ctx, relicA));
            Assert.AreEqual(600, _ctx.Reputation.Current);

            Assert.AreEqual(ShopPurchaseResult.Success, _transaction.PurchaseRelic(_ctx, relicB));
            Assert.AreEqual(200, _ctx.Reputation.Current);

            // Third relic now unaffordable (200 Rep < 400 cost)
            Assert.AreEqual(ShopPurchaseResult.InsufficientFunds, _transaction.PurchaseRelic(_ctx, relicC));
            Assert.AreEqual(200, _ctx.Reputation.Current);
            // Cash still untouched
            Assert.AreEqual(1000f, _ctx.Portfolio.Cash, 0.01f);
        }
    }
}
