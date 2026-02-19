using NUnit.Framework;

namespace BullRun.Tests.Shop
{
    /// <summary>
    /// Story 13.10: Tests for relic sell transaction logic (AC: 6, 15).
    /// Validates sell price calculation (50% floor), reputation refund,
    /// inventory removal, and ShopItemSoldEvent firing.
    /// </summary>
    [TestFixture]
    public class RelicSellTests
    {
        private RunContext _ctx;
        private ShopTransaction _transaction;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            ItemLookup.ClearCache();
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

        // === Sell Price Calculation (AC: 5, 6) ===

        [Test]
        public void SellRelic_RefundsHalfCostRoundedDown_EvenCost()
        {
            // Relic costs 200 → refund 100
            _ctx.OwnedRelics.Add("relic_stop_loss"); // Cost: 100
            int repBefore = _ctx.Reputation.Current;

            var result = _transaction.SellRelic(_ctx, "relic_stop_loss");

            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.AreEqual(repBefore + 50, _ctx.Reputation.Current); // 100 / 2 = 50
        }

        [Test]
        public void SellRelic_RefundsHalfCostRoundedDown_OddCost()
        {
            // Speed Trader costs 150 → refund 75 (150 / 2 = 75, no rounding needed)
            _ctx.OwnedRelics.Add("relic_speed_trader"); // Cost: 150
            int repBefore = _ctx.Reputation.Current;

            var result = _transaction.SellRelic(_ctx, "relic_speed_trader");

            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.AreEqual(repBefore + 75, _ctx.Reputation.Current);
        }

        [Test]
        public void SellRelic_RefundsHalfCostRoundedDown_HighCost()
        {
            // Dark Pool Access costs 350 → refund 175
            _ctx.OwnedRelics.Add("relic_dark_pool"); // Cost: 350
            int repBefore = _ctx.Reputation.Current;

            var result = _transaction.SellRelic(_ctx, "relic_dark_pool");

            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.AreEqual(repBefore + 175, _ctx.Reputation.Current);
        }

        // === Inventory Removal (AC: 6) ===

        [Test]
        public void SellRelic_RemovesFromOwnedRelics()
        {
            _ctx.OwnedRelics.Add("relic_stop_loss");
            _ctx.OwnedRelics.Add("relic_speed_trader");

            _transaction.SellRelic(_ctx, "relic_stop_loss");

            Assert.AreEqual(1, _ctx.OwnedRelics.Count);
            Assert.IsFalse(_ctx.OwnedRelics.Contains("relic_stop_loss"));
            Assert.IsTrue(_ctx.OwnedRelics.Contains("relic_speed_trader"));
        }

        // === Error Cases ===

        [Test]
        public void SellRelic_ReturnsNotOwned_WhenRelicNotInInventory()
        {
            var result = _transaction.SellRelic(_ctx, "relic_stop_loss");

            Assert.AreEqual(ShopPurchaseResult.NotOwned, result);
            Assert.AreEqual(1000, _ctx.Reputation.Current); // No refund
        }

        [Test]
        public void SellRelic_ReturnsNotOwned_WhenRelicIdUnknown()
        {
            _ctx.OwnedRelics.Add("nonexistent_relic");

            var result = _transaction.SellRelic(_ctx, "nonexistent_relic");

            Assert.AreEqual(ShopPurchaseResult.NotOwned, result);
            // Relic should still be in inventory (sell failed)
            Assert.IsTrue(_ctx.OwnedRelics.Contains("nonexistent_relic"));
        }

        // === Event Firing (AC: 15) ===

        [Test]
        public void SellRelic_FiresShopItemSoldEvent()
        {
            ShopItemSoldEvent received = default;
            bool eventFired = false;
            EventBus.Subscribe<ShopItemSoldEvent>(e =>
            {
                eventFired = true;
                received = e;
            });

            _ctx.OwnedRelics.Add("relic_stop_loss"); // Cost: 100

            _transaction.SellRelic(_ctx, "relic_stop_loss");

            Assert.IsTrue(eventFired);
            Assert.AreEqual("relic_stop_loss", received.RelicId);
            Assert.AreEqual(50, received.RefundAmount); // 100 / 2
            Assert.AreEqual(1050, received.RemainingReputation);
        }

        [Test]
        public void SellRelic_DoesNotFireEvent_WhenNotOwned()
        {
            bool eventFired = false;
            EventBus.Subscribe<ShopItemSoldEvent>(e => { eventFired = true; });

            _transaction.SellRelic(_ctx, "relic_stop_loss");

            Assert.IsFalse(eventFired);
        }

        // === Cash Not Affected (AC: 6) ===

        [Test]
        public void SellRelic_DoesNotTouchCash()
        {
            _ctx.OwnedRelics.Add("relic_stop_loss");
            float cashBefore = _ctx.Portfolio.Cash;

            _transaction.SellRelic(_ctx, "relic_stop_loss");

            Assert.AreEqual(cashBefore, _ctx.Portfolio.Cash, 0.01f);
        }

        // === Multiple Sells ===

        [Test]
        public void SellRelic_MultipleSells_CumulativeRefund()
        {
            _ctx.OwnedRelics.Add("relic_stop_loss");      // Cost: 100 → refund 50
            _ctx.OwnedRelics.Add("relic_speed_trader");    // Cost: 150 → refund 75
            int repBefore = _ctx.Reputation.Current;

            _transaction.SellRelic(_ctx, "relic_stop_loss");
            _transaction.SellRelic(_ctx, "relic_speed_trader");

            Assert.AreEqual(0, _ctx.OwnedRelics.Count);
            Assert.AreEqual(repBefore + 50 + 75, _ctx.Reputation.Current);
        }
    }
}
