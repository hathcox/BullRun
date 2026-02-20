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
            RelicFactory.ResetRegistry();
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
            RelicFactory.ResetRegistry();
        }

        // === Sell Price Calculation (AC: 5, 6) ===

        [Test]
        public void SellRelic_RefundsHalfCostRoundedDown_EvenCost()
        {
            _ctx.RelicManager.AddRelic("relic_short_multiplier"); // Cost: 14
            int repBefore = _ctx.Reputation.Current;

            var result = _transaction.SellRelic(_ctx, "relic_short_multiplier");

            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.AreEqual(repBefore + 7, _ctx.Reputation.Current); // 14 / 2 = 7
        }

        [Test]
        public void SellRelic_RefundsHalfCostRoundedDown_OddCost()
        {
            // Quick Draw costs 15 → refund 7 (15 / 2 = 7 via integer division)
            _ctx.RelicManager.AddRelic("relic_quick_draw"); // Cost: 15
            int repBefore = _ctx.Reputation.Current;

            var result = _transaction.SellRelic(_ctx, "relic_quick_draw");

            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.AreEqual(repBefore + 7, _ctx.Reputation.Current);
        }

        [Test]
        public void SellRelic_RefundsHalfCostRoundedDown_HighCost()
        {
            _ctx.RelicManager.AddRelic("relic_rep_doubler"); // Cost: 28
            int repBefore = _ctx.Reputation.Current;

            var result = _transaction.SellRelic(_ctx, "relic_rep_doubler");

            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.AreEqual(repBefore + 14, _ctx.Reputation.Current); // 28 / 2 = 14
        }

        // === Inventory Removal (AC: 6) ===

        [Test]
        public void SellRelic_RemovesFromOwnedRelics()
        {
            _ctx.RelicManager.AddRelic("relic_event_trigger");
            _ctx.RelicManager.AddRelic("relic_short_multiplier");

            _transaction.SellRelic(_ctx, "relic_event_trigger");

            Assert.AreEqual(1, _ctx.OwnedRelics.Count);
            Assert.IsFalse(_ctx.OwnedRelics.Contains("relic_event_trigger"));
            Assert.IsTrue(_ctx.OwnedRelics.Contains("relic_short_multiplier"));
        }

        // === Error Cases ===

        [Test]
        public void SellRelic_ReturnsNotOwned_WhenRelicNotInInventory()
        {
            var result = _transaction.SellRelic(_ctx, "relic_event_trigger");

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

            _ctx.RelicManager.AddRelic("relic_short_multiplier"); // Cost: 14

            _transaction.SellRelic(_ctx, "relic_short_multiplier");

            Assert.IsTrue(eventFired);
            Assert.AreEqual("relic_short_multiplier", received.RelicId);
            Assert.AreEqual(7, received.RefundAmount); // 14 / 2
            Assert.AreEqual(1007, received.RemainingReputation);
        }

        [Test]
        public void SellRelic_DoesNotFireEvent_WhenNotOwned()
        {
            bool eventFired = false;
            EventBus.Subscribe<ShopItemSoldEvent>(e => { eventFired = true; });

            _transaction.SellRelic(_ctx, "relic_event_trigger");

            Assert.IsFalse(eventFired);
        }

        // === Cash Not Affected (AC: 6) ===

        [Test]
        public void SellRelic_DoesNotTouchCash()
        {
            _ctx.RelicManager.AddRelic("relic_event_trigger");
            float cashBefore = _ctx.Portfolio.Cash;

            _transaction.SellRelic(_ctx, "relic_event_trigger");

            Assert.AreEqual(cashBefore, _ctx.Portfolio.Cash, 0.01f);
        }

        // === Multiple Sells ===

        [Test]
        public void SellRelic_MultipleSells_CumulativeRefund()
        {
            _ctx.RelicManager.AddRelic("relic_event_trigger");       // Cost: 18 → refund 9
            _ctx.RelicManager.AddRelic("relic_short_multiplier");    // Cost: 14 → refund 7
            int repBefore = _ctx.Reputation.Current;

            _transaction.SellRelic(_ctx, "relic_event_trigger");
            _transaction.SellRelic(_ctx, "relic_short_multiplier");

            Assert.AreEqual(0, _ctx.OwnedRelics.Count);
            Assert.AreEqual(repBefore + 9 + 7, _ctx.Reputation.Current);
        }
    }
}
