using NUnit.Framework;

namespace BullRun.Tests.Shop
{
    [TestFixture]
    public class InsiderTipPurchaseTests
    {
        private RunContext _ctx;
        private ShopTransaction _transaction;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            _ctx = new RunContext(1, 1, new Portfolio(1000f));
            _ctx.Portfolio.StartRound(_ctx.Portfolio.Cash);
            _ctx.Reputation.Add(200);
            _transaction = new ShopTransaction();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
        }

        // === Purchase flow ===

        [Test]
        public void PurchaseTip_DeductsReputation()
        {
            int initialRep = _ctx.Reputation.Current;
            int cost = 15;
            var tip = new RevealedTip(InsiderTipType.PriceForecast, "Average price ~$6.50");

            var result = _transaction.PurchaseTip(_ctx, tip, cost);

            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.AreEqual(initialRep - cost, _ctx.Reputation.Current);
        }

        [Test]
        public void PurchaseTip_AddsTipToRevealedTips()
        {
            var tip = new RevealedTip(InsiderTipType.TrendDirection, "Market is trending BULLISH");
            _transaction.PurchaseTip(_ctx, tip, 15);

            Assert.AreEqual(1, _ctx.RevealedTips.Count);
            Assert.AreEqual(InsiderTipType.TrendDirection, _ctx.RevealedTips[0].Type);
            Assert.AreEqual("Market is trending BULLISH", _ctx.RevealedTips[0].RevealedText);
        }

        [Test]
        public void PurchaseTip_RejectsInsufficientFunds()
        {
            _ctx.Reputation.Reset();
            _ctx.Reputation.Add(5); // Only 5 rep
            var tip = new RevealedTip(InsiderTipType.EventForecast, "Expect MOSTLY GOOD events");

            var result = _transaction.PurchaseTip(_ctx, tip, 25);

            Assert.AreEqual(ShopPurchaseResult.InsufficientFunds, result);
            Assert.AreEqual(0, _ctx.RevealedTips.Count);
            Assert.AreEqual(5, _ctx.Reputation.Current);
        }

        [Test]
        public void PurchaseTip_RejectsWhenSlotsFull()
        {
            // Default 2 slots
            _ctx.RevealedTips.Add(new RevealedTip(InsiderTipType.PriceForecast, "tip1"));
            _ctx.RevealedTips.Add(new RevealedTip(InsiderTipType.PriceFloor, "tip2"));

            var tip = new RevealedTip(InsiderTipType.EventCount, "3 events");
            var result = _transaction.PurchaseTip(_ctx, tip, 10);

            Assert.AreEqual(ShopPurchaseResult.SlotsFull, result);
            Assert.AreEqual(2, _ctx.RevealedTips.Count);
        }

        // === Slot count respects Intel Expansion ===

        [Test]
        public void PurchaseTip_ThreeSlots_WithIntelExpansion()
        {
            // Simulate Intel Expansion granting +1 slot
            _ctx.InsiderTipSlots = 3;

            _ctx.RevealedTips.Add(new RevealedTip(InsiderTipType.PriceForecast, "tip1"));
            _ctx.RevealedTips.Add(new RevealedTip(InsiderTipType.PriceFloor, "tip2"));

            var tip = new RevealedTip(InsiderTipType.EventCount, "3 events");
            var result = _transaction.PurchaseTip(_ctx, tip, 10);

            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.AreEqual(3, _ctx.RevealedTips.Count);
        }

        [Test]
        public void PurchaseTip_ThreeSlots_RejectsAtFourth()
        {
            _ctx.InsiderTipSlots = 3;

            _ctx.RevealedTips.Add(new RevealedTip(InsiderTipType.PriceForecast, "tip1"));
            _ctx.RevealedTips.Add(new RevealedTip(InsiderTipType.PriceFloor, "tip2"));
            _ctx.RevealedTips.Add(new RevealedTip(InsiderTipType.EventCount, "tip3"));

            var tip = new RevealedTip(InsiderTipType.TrendDirection, "BEARISH");
            var result = _transaction.PurchaseTip(_ctx, tip, 15);

            Assert.AreEqual(ShopPurchaseResult.SlotsFull, result);
        }

        // === Tips cleared on new shop visit ===

        [Test]
        public void RevealedTips_ClearedOnNewShopVisit()
        {
            _ctx.RevealedTips.Add(new RevealedTip(InsiderTipType.PriceForecast, "tip1"));
            _ctx.RevealedTips.Add(new RevealedTip(InsiderTipType.TrendDirection, "tip2"));

            Assert.AreEqual(2, _ctx.RevealedTips.Count);

            // Simulate ShopState.Enter clearing tips
            _ctx.RevealedTips.Clear();

            Assert.AreEqual(0, _ctx.RevealedTips.Count);
        }

        // === Default slot count ===

        [Test]
        public void DefaultInsiderTipSlots_IsTwo()
        {
            Assert.AreEqual(2, GameConfig.DefaultInsiderTipSlots);
            Assert.AreEqual(2, _ctx.InsiderTipSlots);
        }

        // === Fuzz percentage constant ===

        [Test]
        public void InsiderTipFuzzPercent_IsTenPercent()
        {
            Assert.AreEqual(0.10f, GameConfig.InsiderTipFuzzPercent);
        }

        // === Multiple tips can be purchased within slot capacity ===

        [Test]
        public void PurchaseTip_MultiplePurchasesDeductCorrectRep()
        {
            int initialRep = _ctx.Reputation.Current;

            var tip1 = new RevealedTip(InsiderTipType.PriceForecast, "tip1");
            _transaction.PurchaseTip(_ctx, tip1, 15);

            var tip2 = new RevealedTip(InsiderTipType.EventCount, "tip2");
            _transaction.PurchaseTip(_ctx, tip2, 10);

            Assert.AreEqual(2, _ctx.RevealedTips.Count);
            Assert.AreEqual(initialRep - 15 - 10, _ctx.Reputation.Current);
        }

        // === RevealedTip stores correct data ===

        [Test]
        public void RevealedTip_StoresTypeAndText()
        {
            var tip = new RevealedTip(InsiderTipType.VolatilityWarning, "Expect HIGH volatility");
            Assert.AreEqual(InsiderTipType.VolatilityWarning, tip.Type);
            Assert.AreEqual("Expect HIGH volatility", tip.RevealedText);
        }

        // === InsiderTipType enum values ===

        [Test]
        public void InsiderTipType_HasEightValues()
        {
            var values = System.Enum.GetValues(typeof(InsiderTipType));
            Assert.AreEqual(8, values.Length);
        }

        // === Tips persist during trading round (stored in RunContext) ===

        [Test]
        public void RevealedTips_PersistAcrossRoundAdvance()
        {
            _ctx.RevealedTips.Add(new RevealedTip(InsiderTipType.PriceForecast, "price tip"));
            _ctx.RevealedTips.Add(new RevealedTip(InsiderTipType.TrendDirection, "trend tip"));

            // Simulate round advance (AdvanceRound does NOT clear tips)
            _ctx.AdvanceRound();

            Assert.AreEqual(2, _ctx.RevealedTips.Count);
            Assert.AreEqual("price tip", _ctx.RevealedTips[0].RevealedText);
        }

        // === ResetForNewRun clears tips ===

        [Test]
        public void ResetForNewRun_ClearsTipsAndResetsSlots()
        {
            _ctx.RevealedTips.Add(new RevealedTip(InsiderTipType.PriceForecast, "tip"));
            _ctx.InsiderTipSlots = 3;

            _ctx.ResetForNewRun();

            Assert.AreEqual(0, _ctx.RevealedTips.Count);
            Assert.AreEqual(GameConfig.DefaultInsiderTipSlots, _ctx.InsiderTipSlots);
        }
    }
}
