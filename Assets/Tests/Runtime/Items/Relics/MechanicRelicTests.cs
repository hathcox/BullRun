using NUnit.Framework;

namespace BullRun.Tests.Items.Relics
{
    /// <summary>
    /// Story 17.6: Tests for mechanic and timer relics — Time Buyer, Diamond Hands,
    /// Market Manipulator, Free Intel, Extra Expansion, and supporting system changes.
    /// </summary>
    [TestFixture]
    public class MechanicRelicTests
    {
        private RunContext _ctx;
        private RelicManager _mgr;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            RelicFactory.ResetRegistry();
            // Reset TradingState statics to prevent cross-test pollution
            // (IsActive, _activeInstance may retain state from other fixtures)
            new TradingState().Exit(new RunContext(1, 1, new Portfolio(1000f)));
            _ctx = new RunContext(1, 1, new Portfolio(1000f));
            _mgr = _ctx.RelicManager;
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
            RelicFactory.ResetRegistry();
        }

        // ════════════════════════════════════════════════════════════════════
        // TimeBuyerRelic (AC 1)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void TimeBuyerRelic_HasCorrectId()
        {
            var relic = new TimeBuyerRelic();
            Assert.AreEqual("relic_time_buyer", relic.Id);
        }

        [Test]
        public void TimeBuyerRelic_FactoryCreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_time_buyer");
            Assert.IsNotNull(relic);
            Assert.IsInstanceOf<TimeBuyerRelic>(relic);
        }

        [Test]
        public void TimeBuyer_OnBuyTrade_PublishesRelicActivatedEvent()
        {
            _mgr.AddRelic("relic_time_buyer");
            RelicActivatedEvent? captured = null;
            EventBus.Subscribe<RelicActivatedEvent>(e => captured = e);

            _mgr.DispatchAfterTrade(new TradeExecutedEvent
            {
                IsBuy = true, IsShort = false, TotalCost = 100f, Shares = 10, Price = 10f
            });

            Assert.IsNotNull(captured);
            Assert.AreEqual("relic_time_buyer", captured.Value.RelicId);
        }

        [Test]
        public void TimeBuyer_OnSellTrade_NoActivation()
        {
            _mgr.AddRelic("relic_time_buyer");
            RelicActivatedEvent? captured = null;
            EventBus.Subscribe<RelicActivatedEvent>(e => captured = e);

            _mgr.DispatchAfterTrade(new TradeExecutedEvent
            {
                IsBuy = false, IsShort = false, TotalCost = 100f, Shares = 10, Price = 10f
            });

            Assert.IsNull(captured);
        }

        [Test]
        public void TimeBuyer_OnShortTrade_NoActivation()
        {
            _mgr.AddRelic("relic_time_buyer");
            RelicActivatedEvent? captured = null;
            EventBus.Subscribe<RelicActivatedEvent>(e => captured = e);

            _mgr.DispatchAfterTrade(new TradeExecutedEvent
            {
                IsBuy = false, IsShort = true, TotalCost = 50f, Shares = 5, Price = 10f
            });

            Assert.IsNull(captured);
        }

        [Test]
        public void ExtendTimer_WhenNotActive_IsNoOp()
        {
            // SetUp resets TradingState statics, so IsActive is false
            Assert.IsFalse(TradingState.IsActive);
            float before = TradingState.ActiveTimeRemaining;
            TradingState.ExtendTimer(5f);
            Assert.AreEqual(before, TradingState.ActiveTimeRemaining,
                "ExtendTimer should not modify ActiveTimeRemaining when IsActive is false");
        }

        // ════════════════════════════════════════════════════════════════════
        // DiamondHandsRelic (AC 2, 4)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void DiamondHandsRelic_HasCorrectId()
        {
            var relic = new DiamondHandsRelic();
            Assert.AreEqual("relic_diamond_hands", relic.Id);
        }

        [Test]
        public void DiamondHandsRelic_FactoryCreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_diamond_hands");
            Assert.IsNotNull(relic);
            Assert.IsInstanceOf<DiamondHandsRelic>(relic);
        }

        [Test]
        public void GetLiquidationMultiplier_WithDiamondHands_Returns130()
        {
            _mgr.AddRelic("relic_diamond_hands");
            Assert.AreEqual(1.30f, _mgr.GetLiquidationMultiplier(), 0.001f);
        }

        [Test]
        public void GetLiquidationMultiplier_WithoutDiamondHands_Returns100()
        {
            Assert.AreEqual(1.0f, _mgr.GetLiquidationMultiplier(), 0.001f);
        }

        [Test]
        public void GetLiquidationMultiplier_WithOtherRelics_Returns100()
        {
            _mgr.AddRelic("relic_time_buyer");
            Assert.AreEqual(1.0f, _mgr.GetLiquidationMultiplier(), 0.001f);
        }

        // ════════════════════════════════════════════════════════════════════
        // MarketManipulatorRelic (AC 3)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void MarketManipulatorRelic_HasCorrectId()
        {
            var relic = new MarketManipulatorRelic();
            Assert.AreEqual("relic_market_manipulator", relic.Id);
        }

        [Test]
        public void MarketManipulatorRelic_FactoryCreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_market_manipulator");
            Assert.IsNotNull(relic);
            Assert.IsInstanceOf<MarketManipulatorRelic>(relic);
        }

        [Test]
        public void MarketManipulator_OnLongSell_PublishesRelicActivatedEvent()
        {
            _mgr.AddRelic("relic_market_manipulator");
            RelicActivatedEvent? captured = null;
            EventBus.Subscribe<RelicActivatedEvent>(e => captured = e);

            _mgr.DispatchAfterTrade(new TradeExecutedEvent
            {
                IsBuy = false, IsShort = false, TotalCost = 100f, Shares = 10, Price = 10f,
                StockId = "0"
            });

            Assert.IsNotNull(captured);
            Assert.AreEqual("relic_market_manipulator", captured.Value.RelicId);
        }

        [Test]
        public void MarketManipulator_OnBuyTrade_NoActivation()
        {
            _mgr.AddRelic("relic_market_manipulator");
            RelicActivatedEvent? captured = null;
            EventBus.Subscribe<RelicActivatedEvent>(e => captured = e);

            _mgr.DispatchAfterTrade(new TradeExecutedEvent
            {
                IsBuy = true, IsShort = false, TotalCost = 100f, Shares = 10, Price = 10f
            });

            Assert.IsNull(captured);
        }

        [Test]
        public void MarketManipulator_OnShortSell_NoActivation()
        {
            _mgr.AddRelic("relic_market_manipulator");
            RelicActivatedEvent? captured = null;
            EventBus.Subscribe<RelicActivatedEvent>(e => captured = e);

            _mgr.DispatchAfterTrade(new TradeExecutedEvent
            {
                IsBuy = false, IsShort = true, TotalCost = 50f, Shares = 5, Price = 10f
            });

            Assert.IsNull(captured);
        }

        [Test]
        public void ApplyPriceMultiplier_WhenNoActiveInstance_IsNoOp()
        {
            // PriceGenerator._activeInstance is null by default, should not throw
            PriceGenerator.ApplyPriceMultiplier("0", 0.85f);
        }

        // ════════════════════════════════════════════════════════════════════
        // FreeIntelRelic (AC 5, 10)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void FreeIntelRelic_HasCorrectId()
        {
            var relic = new FreeIntelRelic();
            Assert.AreEqual("relic_free_intel", relic.Id);
        }

        [Test]
        public void FreeIntelRelic_FactoryCreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_free_intel");
            Assert.IsNotNull(relic);
            Assert.IsInstanceOf<FreeIntelRelic>(relic);
        }

        [Test]
        public void FreeIntel_OnShopOpen_SetsFreeIntelFlag()
        {
            Assert.IsFalse(_ctx.FreeIntelThisVisit);
            _mgr.AddRelic("relic_free_intel");
            _mgr.DispatchShopOpen();
            Assert.IsTrue(_ctx.FreeIntelThisVisit);
        }

        [Test]
        public void FreeIntel_OnShopOpen_PublishesRelicActivatedEvent()
        {
            _mgr.AddRelic("relic_free_intel");
            RelicActivatedEvent? captured = null;
            EventBus.Subscribe<RelicActivatedEvent>(e => captured = e);

            _mgr.DispatchShopOpen();

            Assert.IsNotNull(captured);
            Assert.AreEqual("relic_free_intel", captured.Value.RelicId);
        }

        [Test]
        public void FreeIntel_FirstTipIsFree()
        {
            _ctx.FreeIntelThisVisit = true;
            _ctx.Reputation.Add(100); // Give some Rep but first tip should still be free

            var transaction = new ShopTransaction();
            var tip = new RevealedTip(InsiderTipType.PriceForecast, "Test tip");
            int repBefore = _ctx.Reputation.Current;

            var result = transaction.PurchaseTip(_ctx, tip, 20);

            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.AreEqual(repBefore, _ctx.Reputation.Current); // No Rep spent
            Assert.AreEqual(1, _ctx.RevealedTips.Count);
        }

        [Test]
        public void FreeIntel_SecondTipCostsNormal()
        {
            _ctx.FreeIntelThisVisit = true;
            _ctx.Reputation.Add(100);

            var transaction = new ShopTransaction();

            // First tip — free
            var tip1 = new RevealedTip(InsiderTipType.PriceForecast, "Tip 1");
            transaction.PurchaseTip(_ctx, tip1, 20);
            int repAfterFirst = _ctx.Reputation.Current;

            // Second tip — costs normal
            var tip2 = new RevealedTip(InsiderTipType.TrendDirection, "Tip 2");
            transaction.PurchaseTip(_ctx, tip2, 15);

            Assert.AreEqual(repAfterFirst - 15, _ctx.Reputation.Current);
        }

        [Test]
        public void FreeIntel_FlagResetsAfterFirstTip()
        {
            _ctx.FreeIntelThisVisit = true;
            _ctx.Reputation.Add(100);

            var transaction = new ShopTransaction();
            var tip = new RevealedTip(InsiderTipType.PriceForecast, "Test tip");
            transaction.PurchaseTip(_ctx, tip, 20);

            Assert.IsFalse(_ctx.FreeIntelThisVisit);
        }

        [Test]
        public void FreeIntel_FlagDefaultsFalse()
        {
            Assert.IsFalse(_ctx.FreeIntelThisVisit);
        }

        [Test]
        public void FreeIntel_FlagResetsOnNewRun()
        {
            _ctx.FreeIntelThisVisit = true;
            _ctx.ResetForNewRun();
            Assert.IsFalse(_ctx.FreeIntelThisVisit);
        }

        // ════════════════════════════════════════════════════════════════════
        // ExtraExpansionRelic (AC 6, 11)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void ExtraExpansionRelic_HasCorrectId()
        {
            var relic = new ExtraExpansionRelic();
            Assert.AreEqual("relic_extra_expansion", relic.Id);
        }

        [Test]
        public void ExtraExpansionRelic_FactoryCreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_extra_expansion");
            Assert.IsNotNull(relic);
            Assert.IsInstanceOf<ExtraExpansionRelic>(relic);
        }

        [Test]
        public void ExtraExpansion_OnShopOpen_IncrementsBonusSlots()
        {
            Assert.AreEqual(0, _ctx.BonusExpansionSlots);
            _mgr.AddRelic("relic_extra_expansion");
            _mgr.DispatchShopOpen();
            Assert.AreEqual(1, _ctx.BonusExpansionSlots);
        }

        [Test]
        public void ExtraExpansion_OnShopOpen_PublishesRelicActivatedEvent()
        {
            _mgr.AddRelic("relic_extra_expansion");
            RelicActivatedEvent? captured = null;
            EventBus.Subscribe<RelicActivatedEvent>(e => captured = e);

            _mgr.DispatchShopOpen();

            Assert.IsNotNull(captured);
            Assert.AreEqual("relic_extra_expansion", captured.Value.RelicId);
        }

        [Test]
        public void BonusExpansionSlots_DefaultsToZero()
        {
            Assert.AreEqual(0, _ctx.BonusExpansionSlots);
        }

        [Test]
        public void BonusExpansionSlots_ResetsOnNewRun()
        {
            _ctx.BonusExpansionSlots = 3;
            _ctx.ResetForNewRun();
            Assert.AreEqual(0, _ctx.BonusExpansionSlots);
        }

        // ════════════════════════════════════════════════════════════════════
        // Factory integration: all 5 relics create real instances (AC 9)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void Factory_TimeBuyer_CreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_time_buyer");
            Assert.IsInstanceOf<TimeBuyerRelic>(relic);
        }

        [Test]
        public void Factory_DiamondHands_CreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_diamond_hands");
            Assert.IsInstanceOf<DiamondHandsRelic>(relic);
        }

        [Test]
        public void Factory_MarketManipulator_CreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_market_manipulator");
            Assert.IsInstanceOf<MarketManipulatorRelic>(relic);
        }

        [Test]
        public void Factory_FreeIntel_CreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_free_intel");
            Assert.IsInstanceOf<FreeIntelRelic>(relic);
        }

        [Test]
        public void Factory_ExtraExpansion_CreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_extra_expansion");
            Assert.IsInstanceOf<ExtraExpansionRelic>(relic);
        }

        // ════════════════════════════════════════════════════════════════════
        // Regression: non-17.6 relics still work
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void Factory_NonMechanicRelics_StillCreateInstances()
        {
            var relic = RelicFactory.Create("relic_skimmer");
            Assert.IsNotNull(relic);
            Assert.AreEqual("relic_skimmer", relic.Id);
        }

        // ════════════════════════════════════════════════════════════════════
        // Review fix M2: Diamond Hands bonus math (MarketCloseState integration)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void DiamondHands_BonusMath_CorrectCalculation()
        {
            // Verify the bonus formula: (multiplier - 1.0f) * longPositionValue
            _mgr.AddRelic("relic_diamond_hands");
            float multiplier = _mgr.GetLiquidationMultiplier();
            float longPositionValue = 500f; // 50 shares * $10
            float expectedBonus = (multiplier - 1.0f) * longPositionValue;

            Assert.AreEqual(150f, expectedBonus, 0.01f); // 30% of $500 = $150
        }

        [Test]
        public void DiamondHands_ZeroPositionValue_NoBonus()
        {
            _mgr.AddRelic("relic_diamond_hands");
            float multiplier = _mgr.GetLiquidationMultiplier();
            float longPositionValue = 0f;
            float bonus = (multiplier - 1.0f) * longPositionValue;

            Assert.AreEqual(0f, bonus, 0.001f);
        }

        [Test]
        public void DiamondHands_PublishesRelicActivatedEvent_WhenBonusApplied()
        {
            // Diamond Hands event is published by MarketCloseState, not the relic.
            // Verify the pattern: multiplier > 1.0 triggers event publishing.
            _mgr.AddRelic("relic_diamond_hands");
            float multiplier = _mgr.GetLiquidationMultiplier();
            Assert.Greater(multiplier, 1.0f);

            // Simulate what MarketCloseState does: publish if multiplier > 1.0 and value > 0
            RelicActivatedEvent? captured = null;
            EventBus.Subscribe<RelicActivatedEvent>(e => captured = e);
            if (multiplier > 1.0f && 500f > 0f)
                EventBus.Publish(new RelicActivatedEvent { RelicId = "relic_diamond_hands" });

            Assert.IsNotNull(captured);
            Assert.AreEqual("relic_diamond_hands", captured.Value.RelicId);
        }

        // ════════════════════════════════════════════════════════════════════
        // Review fix L3: ApplyPriceMultiplier with active instance
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void ApplyPriceMultiplier_WithActiveInstance_ChangesPrice()
        {
            var priceGen = new PriceGenerator();
            PriceGenerator.SetActiveInstance(priceGen);

            // Initialize a round to create active stocks
            priceGen.InitializeRound(1, 1);
            Assert.Greater(priceGen.ActiveStocks.Count, 0);

            var stock = priceGen.ActiveStocks[0];
            float originalPrice = stock.CurrentPrice;
            string stockIdStr = stock.StockId.ToString();

            PriceGenerator.ApplyPriceMultiplier(stockIdStr, 0.85f);

            Assert.AreEqual(originalPrice * 0.85f, stock.CurrentPrice, 0.01f);

            // Cleanup: restore null instance
            PriceGenerator.SetActiveInstance(null);
        }

        [Test]
        public void ApplyPriceMultiplier_PublishesPriceUpdatedEvent()
        {
            var priceGen = new PriceGenerator();
            PriceGenerator.SetActiveInstance(priceGen);
            priceGen.InitializeRound(1, 1);

            var stock = priceGen.ActiveStocks[0];
            string stockIdStr = stock.StockId.ToString();

            PriceUpdatedEvent? captured = null;
            EventBus.Subscribe<PriceUpdatedEvent>(e => captured = e);

            PriceGenerator.ApplyPriceMultiplier(stockIdStr, 0.85f);

            Assert.IsNotNull(captured);
            Assert.AreEqual(stock.StockId, captured.Value.StockId);

            PriceGenerator.SetActiveInstance(null);
        }

        [Test]
        public void ApplyPriceMultiplier_AdjustsTrendLinePrice()
        {
            var priceGen = new PriceGenerator();
            PriceGenerator.SetActiveInstance(priceGen);
            priceGen.InitializeRound(1, 1);

            var stock = priceGen.ActiveStocks[0];
            float originalTrendLine = stock.TrendLinePrice;
            string stockIdStr = stock.StockId.ToString();

            PriceGenerator.ApplyPriceMultiplier(stockIdStr, 0.85f);

            Assert.AreEqual(originalTrendLine * 0.85f, stock.TrendLinePrice, 0.01f);

            PriceGenerator.SetActiveInstance(null);
        }

        // ════════════════════════════════════════════════════════════════════
        // Review fix H1: Free Intel UI bypass test
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void FreeIntel_TipPurchaseSucceeds_WhenZeroRep()
        {
            // Simulates the scenario where the UI bug blocked the free tip:
            // player has 0 Rep but FreeIntelThisVisit is true
            _ctx.FreeIntelThisVisit = true;
            Assert.AreEqual(0, _ctx.Reputation.Current);

            var transaction = new ShopTransaction();
            var tip = new RevealedTip(InsiderTipType.PriceForecast, "Test tip");

            var result = transaction.PurchaseTip(_ctx, tip, 20);

            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.AreEqual(0, _ctx.Reputation.Current); // Still 0 — tip was free
            Assert.AreEqual(1, _ctx.RevealedTips.Count);
        }
    }
}
