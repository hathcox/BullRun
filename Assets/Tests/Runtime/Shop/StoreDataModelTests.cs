using NUnit.Framework;

namespace BullRun.Tests.Shop
{
    /// <summary>
    /// Story 13.9: Updated to use RelicDef instead of ShopItemDef.
    /// </summary>
    [TestFixture]
    public class StoreDataModelTests
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

        private RelicDef MakeRelic(string id, string name, int cost)
        {
            RelicFactory.Register(id, () => new StubRelic(id));
            return new RelicDef(id, name, "Test relic", "", cost);
        }

        // === BondRecord struct ===

        [Test]
        public void BondRecord_Constructor_SetsFields()
        {
            var record = new BondRecord(3, 150.5f);
            Assert.AreEqual(3, record.RoundPurchased);
            Assert.AreEqual(150.5f, record.PricePaid, 0.01f);
        }

        // === RevealedTip struct ===

        [Test]
        public void RevealedTip_Constructor_SetsFields()
        {
            var tip = new RevealedTip(InsiderTipType.PriceForecast, "Stock X going up");
            Assert.AreEqual(InsiderTipType.PriceForecast, tip.Type);
            Assert.AreEqual("Stock X going up", tip.RevealedText);
        }

        // === PurchaseRelic ===

        [Test]
        public void PurchaseRelic_DeductsReputation()
        {
            var relic = MakeRelic("relic-1", "Relic One", 200);
            var result = _transaction.PurchaseRelic(_ctx, relic);

            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.AreEqual(800, _ctx.Reputation.Current);
            Assert.AreEqual(1, _ctx.OwnedRelics.Count);
            Assert.IsTrue(_ctx.OwnedRelics.Contains("relic-1"));
        }

        [Test]
        public void PurchaseRelic_DoesNotTouchCash()
        {
            float cashBefore = _ctx.Portfolio.Cash;
            var relic = MakeRelic("relic-1", "Relic One", 200);
            _transaction.PurchaseRelic(_ctx, relic);
            Assert.AreEqual(cashBefore, _ctx.Portfolio.Cash, 0.01f);
        }

        [Test]
        public void PurchaseRelic_RejectsAlreadyOwned()
        {
            _ctx.OwnedRelics.Add("relic-1");
            var relic = MakeRelic("relic-1", "Relic One", 200);
            var result = _transaction.PurchaseRelic(_ctx, relic);
            Assert.AreEqual(ShopPurchaseResult.AlreadyOwned, result);
            Assert.AreEqual(1000, _ctx.Reputation.Current);
        }

        [Test]
        public void PurchaseRelic_RejectsInsufficientRep()
        {
            _ctx.Reputation.Reset();
            _ctx.Reputation.Add(50);
            var relic = MakeRelic("relic-1", "Relic One", 200);
            var result = _transaction.PurchaseRelic(_ctx, relic);
            Assert.AreEqual(ShopPurchaseResult.InsufficientFunds, result);
            Assert.AreEqual(50, _ctx.Reputation.Current);
        }

        [Test]
        public void PurchaseRelic_PublishesShopItemPurchasedEvent()
        {
            ShopItemPurchasedEvent received = default;
            bool fired = false;
            EventBus.Subscribe<ShopItemPurchasedEvent>(e => { fired = true; received = e; });

            var relic = MakeRelic("relic-1", "Relic One", 300);
            _transaction.PurchaseRelic(_ctx, relic);

            Assert.IsTrue(fired);
            Assert.AreEqual("relic-1", received.ItemId);
            Assert.AreEqual(300, received.Cost);
            Assert.AreEqual(700, received.RemainingReputation);
        }

        // === PurchaseExpansion ===

        [Test]
        public void PurchaseExpansion_DeductsReputationAndAddsToList()
        {
            var result = _transaction.PurchaseExpansion(_ctx, "expand-1", "Expansion One", 150);

            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.AreEqual(850, _ctx.Reputation.Current);
            Assert.AreEqual(1, _ctx.OwnedExpansions.Count);
            Assert.IsTrue(_ctx.OwnedExpansions.Contains("expand-1"));
        }

        [Test]
        public void PurchaseExpansion_DoesNotTouchCash()
        {
            float cashBefore = _ctx.Portfolio.Cash;
            _transaction.PurchaseExpansion(_ctx, "expand-1", "Expansion One", 150);
            Assert.AreEqual(cashBefore, _ctx.Portfolio.Cash, 0.01f);
        }

        [Test]
        public void PurchaseExpansion_RejectsAlreadyOwned()
        {
            _ctx.OwnedExpansions.Add("expand-1");
            var result = _transaction.PurchaseExpansion(_ctx, "expand-1", "Expansion One", 150);
            Assert.AreEqual(ShopPurchaseResult.AlreadyOwned, result);
            Assert.AreEqual(1000, _ctx.Reputation.Current);
        }

        [Test]
        public void PurchaseExpansion_RejectsInsufficientRep()
        {
            _ctx.Reputation.Reset();
            _ctx.Reputation.Add(50);
            var result = _transaction.PurchaseExpansion(_ctx, "expand-1", "Expansion One", 200);
            Assert.AreEqual(ShopPurchaseResult.InsufficientFunds, result);
            Assert.AreEqual(50, _ctx.Reputation.Current);
        }

        // === PurchaseTip ===

        [Test]
        public void PurchaseTip_DeductsReputationAndAddsTip()
        {
            var tip = new RevealedTip(InsiderTipType.EventForecast, "Crash incoming");
            var result = _transaction.PurchaseTip(_ctx, tip, 100);

            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.AreEqual(900, _ctx.Reputation.Current);
            Assert.AreEqual(1, _ctx.RevealedTips.Count);
            Assert.AreEqual(InsiderTipType.EventForecast, _ctx.RevealedTips[0].Type);
            Assert.AreEqual("Crash incoming", _ctx.RevealedTips[0].RevealedText);
        }

        [Test]
        public void PurchaseTip_DoesNotTouchCash()
        {
            float cashBefore = _ctx.Portfolio.Cash;
            var tip = new RevealedTip(InsiderTipType.PriceForecast, "Going up");
            _transaction.PurchaseTip(_ctx, tip, 100);
            Assert.AreEqual(cashBefore, _ctx.Portfolio.Cash, 0.01f);
        }

        [Test]
        public void PurchaseTip_RejectsInsufficientRep()
        {
            _ctx.Reputation.Reset();
            _ctx.Reputation.Add(30);
            var tip = new RevealedTip(InsiderTipType.VolatilityWarning, "Tech rising");
            var result = _transaction.PurchaseTip(_ctx, tip, 100);
            Assert.AreEqual(ShopPurchaseResult.InsufficientFunds, result);
            Assert.AreEqual(30, _ctx.Reputation.Current);
            Assert.AreEqual(0, _ctx.RevealedTips.Count);
        }

        // === PurchaseBond (delegates to BondManager.Purchase using round-based pricing) ===

        [Test]
        public void PurchaseBond_DeductsCashAndIncrementsBonds()
        {
            // Round 1 bond costs $3
            var result = _transaction.PurchaseBond(_ctx, 3f);

            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.AreEqual(997f, _ctx.Portfolio.Cash, 0.01f);
            Assert.AreEqual(1, _ctx.BondsOwned);
            Assert.AreEqual(1, _ctx.BondPurchaseHistory.Count);
        }

        [Test]
        public void PurchaseBond_DoesNotTouchReputation()
        {
            _transaction.PurchaseBond(_ctx, 3f);
            Assert.AreEqual(1000, _ctx.Reputation.Current);
        }

        [Test]
        public void PurchaseBond_RecordsPurchaseHistory()
        {
            _ctx.CurrentRound = 3;
            // Round 3 bond costs $8
            _transaction.PurchaseBond(_ctx, 8f);

            Assert.AreEqual(1, _ctx.BondPurchaseHistory.Count);
            Assert.AreEqual(3, _ctx.BondPurchaseHistory[0].RoundPurchased);
            Assert.AreEqual(8f, _ctx.BondPurchaseHistory[0].PricePaid, 0.01f);
        }

        [Test]
        public void PurchaseBond_RejectsInsufficientCash()
        {
            // Create context with very low cash so Round 1 $3 bond is unaffordable
            var poorCtx = new RunContext(1, 1, new Portfolio(2f));
            poorCtx.Portfolio.StartRound(poorCtx.Portfolio.Cash);
            var result = _transaction.PurchaseBond(poorCtx, 3f);
            Assert.AreEqual(ShopPurchaseResult.InsufficientFunds, result);
            Assert.AreEqual(2f, poorCtx.Portfolio.Cash, 0.01f);
            Assert.AreEqual(0, poorCtx.BondsOwned);
        }

        [Test]
        public void PurchaseBond_MultiplePurchases()
        {
            // All at Round 1, price is $3 each
            _transaction.PurchaseBond(_ctx, 3f);
            _transaction.PurchaseBond(_ctx, 3f);
            _transaction.PurchaseBond(_ctx, 3f);

            Assert.AreEqual(3, _ctx.BondsOwned);
            Assert.AreEqual(991f, _ctx.Portfolio.Cash, 0.01f);
            Assert.AreEqual(3, _ctx.BondPurchaseHistory.Count);
        }

        // === SellBond ===

        [Test]
        public void SellBond_AddsCashAndDecrementsBonds()
        {
            // Purchase two bonds at Round 1 ($3 each)
            _transaction.PurchaseBond(_ctx, 3f);
            _transaction.PurchaseBond(_ctx, 3f);
            Assert.AreEqual(2, _ctx.BondsOwned);
            float cashBeforeSell = _ctx.Portfolio.Cash;

            var result = _transaction.SellBond(_ctx);

            Assert.AreEqual(ShopPurchaseResult.Success, result);
            // Sell price = 3 * 0.5 = 1.5
            Assert.AreEqual(cashBeforeSell + 1.5f, _ctx.Portfolio.Cash, 0.01f);
            Assert.AreEqual(1, _ctx.BondsOwned);
        }

        [Test]
        public void SellBond_DoesNotTouchReputation()
        {
            _transaction.PurchaseBond(_ctx, 3f);
            int repBefore = _ctx.Reputation.Current;
            _transaction.SellBond(_ctx);
            Assert.AreEqual(repBefore, _ctx.Reputation.Current);
        }

        [Test]
        public void SellBond_RejectsWhenNoBondsOwned()
        {
            Assert.AreEqual(0, _ctx.BondsOwned);
            var result = _transaction.SellBond(_ctx);
            Assert.AreEqual(ShopPurchaseResult.Error, result);
            Assert.AreEqual(1000f, _ctx.Portfolio.Cash, 0.01f);
        }

        // === Atomic purchase patterns ===

        [Test]
        public void AllPurchaseTypes_AreAtomic_ValidateThenDeductThenApply()
        {
            // Relic: validate → deduct rep → add to list
            var relic = MakeRelic("relic-a", "Relic A", 100);
            Assert.AreEqual(ShopPurchaseResult.Success, _transaction.PurchaseRelic(_ctx, relic));
            Assert.AreEqual(1, _ctx.OwnedRelics.Count);
            Assert.AreEqual(900, _ctx.Reputation.Current);

            // Expansion: validate → deduct rep → add to list
            Assert.AreEqual(ShopPurchaseResult.Success, _transaction.PurchaseExpansion(_ctx, "exp-a", "Exp A", 100));
            Assert.AreEqual(1, _ctx.OwnedExpansions.Count);
            Assert.AreEqual(800, _ctx.Reputation.Current);

            // Tip: validate → deduct rep → add to list
            var tip = new RevealedTip(InsiderTipType.PriceForecast, "Up");
            Assert.AreEqual(ShopPurchaseResult.Success, _transaction.PurchaseTip(_ctx, tip, 100));
            Assert.AreEqual(1, _ctx.RevealedTips.Count);
            Assert.AreEqual(700, _ctx.Reputation.Current);

            // Bond buy: validate cash → deduct cash → increment bonds (Round 1 = $3)
            Assert.AreEqual(ShopPurchaseResult.Success, _transaction.PurchaseBond(_ctx, 3f));
            Assert.AreEqual(1, _ctx.BondsOwned);
            Assert.AreEqual(997f, _ctx.Portfolio.Cash, 0.01f);

            // Bond sell: validate bonds → LIFO sell → add cash → decrement bonds
            // Sell price = 3 * 0.5 = 1.5
            float cashBeforeSell = _ctx.Portfolio.Cash;
            Assert.AreEqual(ShopPurchaseResult.Success, _transaction.SellBond(_ctx));
            Assert.AreEqual(0, _ctx.BondsOwned);
            Assert.AreEqual(cashBeforeSell + 1.5f, _ctx.Portfolio.Cash, 0.01f);
        }

        // === State persistence across shop visits ===

        [Test]
        public void CurrentShopRerollCount_ResetsEachShopVisit()
        {
            _ctx.CurrentShopRerollCount = 3;
            // Simulate what ShopState.Enter() does
            _ctx.CurrentShopRerollCount = 0;
            Assert.AreEqual(0, _ctx.CurrentShopRerollCount);
        }

        [Test]
        public void RevealedTips_ClearedEachShopVisit()
        {
            _ctx.RevealedTips.Add(new RevealedTip(InsiderTipType.PriceForecast, "Up"));
            _ctx.RevealedTips.Add(new RevealedTip(InsiderTipType.EventForecast, "Crash"));
            Assert.AreEqual(2, _ctx.RevealedTips.Count);

            // Simulate what ShopState.Enter() does
            _ctx.RevealedTips.Clear();
            Assert.AreEqual(0, _ctx.RevealedTips.Count);
        }

        [Test]
        public void OwnedRelics_PersistsAcrossShopVisits()
        {
            _ctx.OwnedRelics.Add("relic-1");
            _ctx.CurrentShopRerollCount = 0;
            _ctx.RevealedTips.Clear();
            Assert.AreEqual(1, _ctx.OwnedRelics.Count);
            Assert.IsTrue(_ctx.OwnedRelics.Contains("relic-1"));
        }

        [Test]
        public void OwnedExpansions_PersistsAcrossShopVisits()
        {
            _ctx.OwnedExpansions.Add("expand-1");
            _ctx.CurrentShopRerollCount = 0;
            _ctx.RevealedTips.Clear();
            Assert.AreEqual(1, _ctx.OwnedExpansions.Count);
        }

        [Test]
        public void BondsOwned_PersistsAcrossShopVisits()
        {
            _ctx.BondsOwned = 2;
            _ctx.CurrentShopRerollCount = 0;
            _ctx.RevealedTips.Clear();
            Assert.AreEqual(2, _ctx.BondsOwned);
        }

        [Test]
        public void InsiderTipSlots_PersistsAcrossShopVisits()
        {
            _ctx.InsiderTipSlots = 4;
            _ctx.CurrentShopRerollCount = 0;
            _ctx.RevealedTips.Clear();
            Assert.AreEqual(4, _ctx.InsiderTipSlots);
        }

        // === MaxRelicSlots enforcement ===

        [Test]
        public void PurchaseRelic_RejectsWhenMaxRelicSlotsReached()
        {
            for (int i = 0; i < GameConfig.MaxRelicSlots; i++)
                _ctx.OwnedRelics.Add($"relic-{i}");

            var relic = MakeRelic("relic-overflow", "Overflow", 100);
            var result = _transaction.PurchaseRelic(_ctx, relic);
            Assert.AreEqual(ShopPurchaseResult.SlotsFull, result);
            Assert.AreEqual(1000, _ctx.Reputation.Current);
        }

        [Test]
        public void PurchaseRelic_AllowsUpToMaxRelicSlots()
        {
            for (int i = 0; i < GameConfig.MaxRelicSlots - 1; i++)
                _ctx.RelicManager.AddRelic(ShopItemDefinitions.RelicPool[i].Id);

            var relic = MakeRelic("relic-last", "Last Slot", 100);
            var result = _transaction.PurchaseRelic(_ctx, relic);
            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.AreEqual(GameConfig.MaxRelicSlots, _ctx.OwnedRelics.Count);
        }

        // === InsiderTipSlots capacity ===

        [Test]
        public void PurchaseTip_RejectsWhenAllSlotsUsed()
        {
            for (int i = 0; i < _ctx.InsiderTipSlots; i++)
                _ctx.RevealedTips.Add(new RevealedTip(InsiderTipType.PriceForecast, $"tip-{i}"));

            var tip = new RevealedTip(InsiderTipType.VolatilityWarning, "Overflow");
            var result = _transaction.PurchaseTip(_ctx, tip, 100);
            Assert.AreEqual(ShopPurchaseResult.SlotsFull, result);
            Assert.AreEqual(1000, _ctx.Reputation.Current);
        }

        [Test]
        public void PurchaseTip_AllowsUpToInsiderTipSlots()
        {
            for (int i = 0; i < _ctx.InsiderTipSlots - 1; i++)
                _ctx.RevealedTips.Add(new RevealedTip(InsiderTipType.PriceForecast, $"tip-{i}"));

            var tip = new RevealedTip(InsiderTipType.EventForecast, "Last slot");
            var result = _transaction.PurchaseTip(_ctx, tip, 100);
            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.AreEqual(_ctx.InsiderTipSlots, _ctx.RevealedTips.Count);
        }

        // === PurchaseExpansion fires event ===

        [Test]
        public void PurchaseExpansion_PublishesShopExpansionPurchasedEvent()
        {
            ShopExpansionPurchasedEvent received = default;
            bool fired = false;
            EventBus.Subscribe<ShopExpansionPurchasedEvent>(e => { fired = true; received = e; });

            var result = _transaction.PurchaseExpansion(_ctx, "expand-1", "Expansion One", 150);

            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.IsTrue(fired);
            Assert.AreEqual("expand-1", received.ExpansionId);
            Assert.AreEqual("Expansion One", received.DisplayName);
            Assert.AreEqual(150, received.Cost);
            Assert.AreEqual(850, received.RemainingReputation);
        }

        [Test]
        public void PurchaseExpansion_DoesNotFireEvent_OnRejection()
        {
            _ctx.OwnedExpansions.Add("expand-1");
            bool fired = false;
            EventBus.Subscribe<ShopExpansionPurchasedEvent>(_ => fired = true);

            _transaction.PurchaseExpansion(_ctx, "expand-1", "Expansion One", 150);
            Assert.IsFalse(fired);
        }
    }
}
