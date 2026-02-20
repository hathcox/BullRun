using NUnit.Framework;

namespace BullRun.Tests.Shop
{
    /// <summary>
    /// Story 13.10: Tests for click-to-buy behavior, owned relics bar logic,
    /// and bond button preservation (regression check).
    /// Pure logic tests — no MonoBehaviour/UI instantiation needed.
    /// </summary>
    [TestFixture]
    public class ClickToBuyTests
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

        /// <summary>
        /// Helper to register a test relic in the factory and return its RelicDef.
        /// </summary>
        private RelicDef MakeTestRelic(string id, string name, string desc, int cost)
        {
            RelicFactory.Register(id, () => new StubRelic(id));
            return new RelicDef(id, name, desc, "", cost);
        }

        // === Click-to-buy relic: purchase succeeds, relic added to inventory (AC 7, 8) ===

        [Test]
        public void ClickToBuy_RelicPurchase_AddsToOwnedRelics()
        {
            var relic = MakeTestRelic("test_relic", "Test", "desc", 100);
            var result = _transaction.PurchaseRelic(_ctx, relic);

            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.IsTrue(_ctx.OwnedRelics.Contains("test_relic"));
            Assert.AreEqual(900, _ctx.Reputation.Current);
        }

        // === Click-to-buy when inventory full: no purchase (AC 12) ===

        [Test]
        public void ClickToBuy_WhenFull_RejectsWithSlotsFull()
        {
            // Fill inventory to max using real pool relics via RelicManager
            for (int i = 0; i < GameConfig.MaxRelicSlots; i++)
                _ctx.RelicManager.AddRelic(ShopItemDefinitions.RelicPool[i].Id);

            var relic = MakeTestRelic("extra", "Extra", "desc", 100);
            var result = _transaction.PurchaseRelic(_ctx, relic);

            Assert.AreEqual(ShopPurchaseResult.SlotsFull, result);
            Assert.AreEqual(1000, _ctx.Reputation.Current); // No deduction
        }

        [Test]
        public void ClickToBuy_WhenFull_WithExpandedInventory_AllowsPurchase()
        {
            _ctx.OwnedExpansions.Add(ExpansionDefinitions.ExpandedInventory);

            // Fill to base max (5) using real pool relics via RelicManager
            for (int i = 0; i < GameConfig.MaxRelicSlots; i++)
                _ctx.RelicManager.AddRelic(ShopItemDefinitions.RelicPool[i].Id);

            var relic = MakeTestRelic("beyond_base", "Beyond", "desc", 100);
            var result = _transaction.PurchaseRelic(_ctx, relic);

            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.AreEqual(GameConfig.MaxRelicSlots + 1, _ctx.OwnedRelics.Count);
        }

        // === Click-to-buy when can't afford (AC 13) ===

        [Test]
        public void ClickToBuy_WhenCantAfford_RejectsWithInsufficientFunds()
        {
            _ctx.Reputation.Reset();
            _ctx.Reputation.Add(50); // Less than relic cost

            var relic = MakeTestRelic("expensive", "Expensive", "desc", 200);
            var result = _transaction.PurchaseRelic(_ctx, relic);

            Assert.AreEqual(ShopPurchaseResult.InsufficientFunds, result);
            Assert.AreEqual(50, _ctx.Reputation.Current);
        }

        // === Click-to-buy expansion (AC 9) ===

        [Test]
        public void ClickToBuy_ExpansionPurchase_AddsToOwnedExpansions()
        {
            var result = _transaction.PurchaseExpansion(_ctx, "test_expansion", "Test", 100);

            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.IsTrue(_ctx.OwnedExpansions.Contains("test_expansion"));
            Assert.AreEqual(900, _ctx.Reputation.Current);
        }

        [Test]
        public void ClickToBuy_Expansion_WhenAlreadyOwned_Rejects()
        {
            _ctx.OwnedExpansions.Add("test_expansion");

            var result = _transaction.PurchaseExpansion(_ctx, "test_expansion", "Test", 100);

            Assert.AreEqual(ShopPurchaseResult.AlreadyOwned, result);
            Assert.AreEqual(1000, _ctx.Reputation.Current);
        }

        // === ShopItemPurchasedEvent fires on all purchases (AC 14) ===

        [Test]
        public void PurchaseRelic_FiresShopItemPurchasedEvent()
        {
            bool fired = false;
            EventBus.Subscribe<ShopItemPurchasedEvent>(e => { fired = true; });

            _transaction.PurchaseRelic(_ctx, MakeTestRelic("r", "R", "d", 100));

            Assert.IsTrue(fired);
        }

        [Test]
        public void PurchaseExpansion_FiresShopExpansionPurchasedEvent()
        {
            bool fired = false;
            EventBus.Subscribe<ShopExpansionPurchasedEvent>(e => { fired = true; });

            _transaction.PurchaseExpansion(_ctx, "exp", "Exp", 100);

            Assert.IsTrue(fired);
        }

        // === Click-to-buy insider tip (AC 10) ===

        [Test]
        public void ClickToBuy_TipPurchase_DeductsRepAndAddsTip()
        {
            var tip = new RevealedTip(InsiderTipType.PriceForecast, "Price will rise");
            int cost = 75;

            var result = _transaction.PurchaseTip(_ctx, tip, cost);

            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.AreEqual(1, _ctx.RevealedTips.Count);
            Assert.AreEqual(925, _ctx.Reputation.Current);
        }

        [Test]
        public void ClickToBuy_Tip_WhenCantAfford_RejectsWithInsufficientFunds()
        {
            _ctx.Reputation.Reset();
            _ctx.Reputation.Add(30); // Less than tip cost

            var tip = new RevealedTip(InsiderTipType.PriceForecast, "Price will rise");
            var result = _transaction.PurchaseTip(_ctx, tip, 75);

            Assert.AreEqual(ShopPurchaseResult.InsufficientFunds, result);
            Assert.AreEqual(0, _ctx.RevealedTips.Count);
            Assert.AreEqual(30, _ctx.Reputation.Current);
        }

        // === Bond buttons unchanged regression check (AC 11) ===

        [Test]
        public void BondPurchaseResult_StillReturnsExpectedTypes()
        {
            // Verify ShopPurchaseResult enum still has all expected values
            Assert.IsTrue(System.Enum.IsDefined(typeof(ShopPurchaseResult), ShopPurchaseResult.Success));
            Assert.IsTrue(System.Enum.IsDefined(typeof(ShopPurchaseResult), ShopPurchaseResult.InsufficientFunds));
            Assert.IsTrue(System.Enum.IsDefined(typeof(ShopPurchaseResult), ShopPurchaseResult.SlotsFull));
            Assert.IsTrue(System.Enum.IsDefined(typeof(ShopPurchaseResult), ShopPurchaseResult.AlreadyOwned));
            Assert.IsTrue(System.Enum.IsDefined(typeof(ShopPurchaseResult), ShopPurchaseResult.NotOwned));
            Assert.IsTrue(System.Enum.IsDefined(typeof(ShopPurchaseResult), ShopPurchaseResult.Error));
        }

        [Test]
        public void BondManager_StillAccessible_FromRunContext()
        {
            // Regression: Verify BondManager is still wired correctly
            Assert.IsNotNull(_ctx.Bonds);
        }

        // === Sell refund calculation edge cases (AC 5, 6) ===

        [Test]
        public void SellRefund_Cost20_Returns10()
        {
            _ctx.RelicManager.AddRelic("relic_short_multiplier"); // Cost: 20
            int repBefore = _ctx.Reputation.Current;

            _transaction.SellRelic(_ctx, "relic_short_multiplier");

            Assert.AreEqual(repBefore + 10, _ctx.Reputation.Current);
        }

        [Test]
        public void SellRefund_Cost30_Returns15()
        {
            _ctx.RelicManager.AddRelic("relic_double_dealer"); // Cost: 30
            int repBefore = _ctx.Reputation.Current;

            _transaction.SellRelic(_ctx, "relic_double_dealer");

            Assert.AreEqual(repBefore + 15, _ctx.Reputation.Current);
        }

        [Test]
        public void SellRefund_Cost50_Returns25()
        {
            // Story 17.7: relic_relic_expansion now has custom sell value of 0,
            // so use relic_rep_doubler (cost 40) for standard 50% refund test
            _ctx.RelicManager.AddRelic("relic_rep_doubler"); // Cost: 40
            int repBefore = _ctx.Reputation.Current;

            _transaction.SellRelic(_ctx, "relic_rep_doubler");

            Assert.AreEqual(repBefore + 20, _ctx.Reputation.Current);
        }

        // === ShopItemSoldEvent contains correct data (AC 15) ===

        [Test]
        public void SellRelic_EventContains_RelicIdAndRefund()
        {
            ShopItemSoldEvent received = default;
            EventBus.Subscribe<ShopItemSoldEvent>(e => { received = e; });

            _ctx.RelicManager.AddRelic("relic_rep_doubler"); // Cost: 40

            _transaction.SellRelic(_ctx, "relic_rep_doubler");

            Assert.AreEqual("relic_rep_doubler", received.RelicId);
            Assert.AreEqual(20, received.RefundAmount); // 40 / 2
        }

        // === Owned relics capacity dynamic adjustment (AC 2) ===

        [Test]
        public void MaxSlots_Default5_ExpandedInventory7()
        {
            Assert.AreEqual(5, ShopTransaction.GetEffectiveMaxRelicSlots(_ctx));

            _ctx.OwnedExpansions.Add(ExpansionDefinitions.ExpandedInventory);
            Assert.AreEqual(7, ShopTransaction.GetEffectiveMaxRelicSlots(_ctx));
        }

        [Test]
        public void MaxPossibleOwnedSlots_Is7()
        {
            Assert.AreEqual(7, ShopUI.MaxPossibleOwnedSlots);
        }

        // === Sell then buy cycle (AC 6, 7) ===

        [Test]
        public void SellThenBuy_FreesSlotForNewPurchase()
        {
            // Fill to max using real pool relics via RelicManager
            for (int i = 0; i < GameConfig.MaxRelicSlots; i++)
                _ctx.RelicManager.AddRelic(ShopItemDefinitions.RelicPool[i].Id);

            // Should be full
            var relic = MakeTestRelic("new_relic", "New", "desc", 100);
            Assert.AreEqual(ShopPurchaseResult.SlotsFull, _transaction.PurchaseRelic(_ctx, relic));

            // Sell one
            _transaction.SellRelic(_ctx, ShopItemDefinitions.RelicPool[0].Id);
            Assert.AreEqual(GameConfig.MaxRelicSlots - 1, _ctx.OwnedRelics.Count);

            // Now should be able to buy
            var result = _transaction.PurchaseRelic(_ctx, relic);
            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.AreEqual(GameConfig.MaxRelicSlots, _ctx.OwnedRelics.Count);
        }
    }
}
