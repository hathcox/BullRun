using System.Collections.Generic;
using NUnit.Framework;

namespace BullRun.Tests.Items.Relics
{
    /// <summary>
    /// Story 17.3: Tests for trade modification relics — Double Dealer, Quick Draw,
    /// Bear Raid, Skimmer, Short Profiteer, and RelicManager helper methods.
    /// </summary>
    [TestFixture]
    public class TradeRelicTests
    {
        private RunContext _ctx;
        private RelicManager _mgr;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            RelicFactory.ResetRegistry();
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
        // Double Dealer (AC 1)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void DoubleDealerRelic_HasCorrectId()
        {
            var relic = new DoubleDealerRelic();
            Assert.AreEqual("relic_double_dealer", relic.Id);
        }

        [Test]
        public void DoubleDealerRelic_FactoryCreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_double_dealer");
            Assert.IsNotNull(relic);
            Assert.IsInstanceOf<DoubleDealerRelic>(relic);
        }

        [Test]
        public void GetEffectiveTradeQuantity_WithDoubleDealer_DoublesQuantity()
        {
            _mgr.AddRelic("relic_double_dealer");
            Assert.AreEqual(10, _mgr.GetEffectiveTradeQuantity(5));
        }

        [Test]
        public void GetEffectiveTradeQuantity_WithDoubleDealer_DoublesQuantityOf1()
        {
            _mgr.AddRelic("relic_double_dealer");
            Assert.AreEqual(2, _mgr.GetEffectiveTradeQuantity(1));
        }

        [Test]
        public void GetEffectiveTradeQuantity_WithoutDoubleDealer_ReturnsBaseQuantity()
        {
            Assert.AreEqual(5, _mgr.GetEffectiveTradeQuantity(5));
        }

        [Test]
        public void GetEffectiveTradeQuantity_WithOtherRelics_ReturnsBaseQuantity()
        {
            _mgr.AddRelic("relic_quick_draw");
            Assert.AreEqual(3, _mgr.GetEffectiveTradeQuantity(3));
        }

        [Test]
        public void GetEffectiveTradeQuantity_WithDoubleDealer_ZeroReturnsZero()
        {
            _mgr.AddRelic("relic_double_dealer");
            // 0 * 2 = 0, no phantom trades
            Assert.AreEqual(0, _mgr.GetEffectiveTradeQuantity(0));
        }

        // ════════════════════════════════════════════════════════════════════
        // Quick Draw (AC 2)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void QuickDrawRelic_HasCorrectId()
        {
            var relic = new QuickDrawRelic();
            Assert.AreEqual("relic_quick_draw", relic.Id);
        }

        [Test]
        public void QuickDrawRelic_FactoryCreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_quick_draw");
            Assert.IsNotNull(relic);
            Assert.IsInstanceOf<QuickDrawRelic>(relic);
        }

        [Test]
        public void GetEffectiveCooldown_WithQuickDraw_BuyReturnsZero()
        {
            _mgr.AddRelic("relic_quick_draw");
            Assert.AreEqual(0f, _mgr.GetEffectiveCooldown(true));
        }

        [Test]
        public void GetEffectiveCooldown_WithQuickDraw_SellReturnsDouble()
        {
            _mgr.AddRelic("relic_quick_draw");
            float expected = GameConfig.PostTradeCooldown * 2f;
            Assert.AreEqual(expected, _mgr.GetEffectiveCooldown(false));
        }

        [Test]
        public void GetEffectiveCooldown_WithoutQuickDraw_ReturnsDefault()
        {
            Assert.AreEqual(GameConfig.PostTradeCooldown, _mgr.GetEffectiveCooldown(true));
            Assert.AreEqual(GameConfig.PostTradeCooldown, _mgr.GetEffectiveCooldown(false));
        }

        [Test]
        public void GetEffectiveCooldown_WithOtherRelics_ReturnsDefault()
        {
            _mgr.AddRelic("relic_double_dealer");
            Assert.AreEqual(GameConfig.PostTradeCooldown, _mgr.GetEffectiveCooldown(true));
            Assert.AreEqual(GameConfig.PostTradeCooldown, _mgr.GetEffectiveCooldown(false));
        }

        // ════════════════════════════════════════════════════════════════════
        // Bear Raid (AC 3)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void ShortMultiplierRelic_HasCorrectId()
        {
            var relic = new ShortMultiplierRelic();
            Assert.AreEqual("relic_short_multiplier", relic.Id);
        }

        [Test]
        public void ShortMultiplierRelic_FactoryCreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_short_multiplier");
            Assert.IsNotNull(relic);
            Assert.IsInstanceOf<ShortMultiplierRelic>(relic);
        }

        [Test]
        public void BearRaid_OnAcquired_SetsLongsDisabled()
        {
            Assert.IsFalse(_ctx.LongsDisabled);
            _mgr.AddRelic("relic_short_multiplier");
            Assert.IsTrue(_ctx.LongsDisabled);
        }

        [Test]
        public void BearRaid_LongsDisabled_DefaultFalse()
        {
            Assert.IsFalse(_ctx.LongsDisabled);
        }

        [Test]
        public void GetEffectiveShortShares_WithBearRaid_Returns3()
        {
            _mgr.AddRelic("relic_short_multiplier");
            Assert.AreEqual(3, _mgr.GetEffectiveShortShares());
        }

        [Test]
        public void GetEffectiveShortShares_WithoutBearRaid_ReturnsDefault()
        {
            Assert.AreEqual(GameConfig.ShortBaseShares, _mgr.GetEffectiveShortShares());
        }

        [Test]
        public void GetEffectiveShortShares_WithOtherRelics_ReturnsDefault()
        {
            _mgr.AddRelic("relic_quick_draw");
            Assert.AreEqual(GameConfig.ShortBaseShares, _mgr.GetEffectiveShortShares());
        }

        [Test]
        public void LongsDisabled_ResetForNewRun_ClearsFlag()
        {
            _ctx.LongsDisabled = true;
            Assert.IsTrue(_ctx.LongsDisabled);
            _ctx.ResetForNewRun();
            Assert.IsFalse(_ctx.LongsDisabled);
        }

        // ════════════════════════════════════════════════════════════════════
        // Skimmer (AC 4)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void SkimmerRelic_HasCorrectId()
        {
            var relic = new SkimmerRelic();
            Assert.AreEqual("relic_skimmer", relic.Id);
        }

        [Test]
        public void SkimmerRelic_FactoryCreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_skimmer");
            Assert.IsNotNull(relic);
            Assert.IsInstanceOf<SkimmerRelic>(relic);
        }

        [Test]
        public void Skimmer_OnBuyTrade_Adds3PercentToCash()
        {
            _mgr.AddRelic("relic_skimmer");
            float initialCash = _ctx.Portfolio.Cash;
            float totalCost = 100f;

            _mgr.DispatchAfterTrade(new TradeExecutedEvent
            {
                IsBuy = true, IsShort = false, TotalCost = totalCost, Shares = 10, Price = 10f
            });

            float expectedBonus = totalCost * 0.03f; // 3.0
            Assert.AreEqual(initialCash + expectedBonus, _ctx.Portfolio.Cash, 0.01f);
        }

        [Test]
        public void Skimmer_OnSellTrade_NoEffect()
        {
            _mgr.AddRelic("relic_skimmer");
            float initialCash = _ctx.Portfolio.Cash;

            _mgr.DispatchAfterTrade(new TradeExecutedEvent
            {
                IsBuy = false, IsShort = false, TotalCost = 100f, Shares = 10, Price = 10f
            });

            Assert.AreEqual(initialCash, _ctx.Portfolio.Cash, 0.01f);
        }

        [Test]
        public void Skimmer_OnShortTrade_NoEffect()
        {
            _mgr.AddRelic("relic_skimmer");
            float initialCash = _ctx.Portfolio.Cash;

            _mgr.DispatchAfterTrade(new TradeExecutedEvent
            {
                IsBuy = false, IsShort = true, TotalCost = 50f, Shares = 5, Price = 10f
            });

            Assert.AreEqual(initialCash, _ctx.Portfolio.Cash, 0.01f);
        }

        [Test]
        public void Skimmer_PublishesTradeFeedbackEvent()
        {
            _mgr.AddRelic("relic_skimmer");
            TradeFeedbackEvent? captured = null;
            EventBus.Subscribe<TradeFeedbackEvent>(e => captured = e);

            _mgr.DispatchAfterTrade(new TradeExecutedEvent
            {
                IsBuy = true, IsShort = false, TotalCost = 100f, Shares = 10, Price = 10f
            });

            Assert.IsNotNull(captured);
            Assert.IsTrue(captured.Value.IsSuccess);
            Assert.IsTrue(captured.Value.Message.Contains("$"));
        }

        [Test]
        public void Skimmer_PublishesRelicActivatedEvent()
        {
            _mgr.AddRelic("relic_skimmer");
            RelicActivatedEvent? captured = null;
            EventBus.Subscribe<RelicActivatedEvent>(e => captured = e);

            _mgr.DispatchAfterTrade(new TradeExecutedEvent
            {
                IsBuy = true, IsShort = false, TotalCost = 100f, Shares = 10, Price = 10f
            });

            Assert.IsNotNull(captured);
            Assert.AreEqual("relic_skimmer", captured.Value.RelicId);
        }

        // ════════════════════════════════════════════════════════════════════
        // Short Profiteer (AC 5)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void ShortProfiteerRelic_HasCorrectId()
        {
            var relic = new ShortProfiteerRelic();
            Assert.AreEqual("relic_short_profiteer", relic.Id);
        }

        [Test]
        public void ShortProfiteerRelic_FactoryCreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_short_profiteer");
            Assert.IsNotNull(relic);
            Assert.IsInstanceOf<ShortProfiteerRelic>(relic);
        }

        [Test]
        public void ShortProfiteer_OnShortOpen_Adds10PercentToCash()
        {
            _mgr.AddRelic("relic_short_profiteer");
            float initialCash = _ctx.Portfolio.Cash;
            float price = 50f;
            int shares = 3;

            _mgr.DispatchAfterTrade(new TradeExecutedEvent
            {
                IsBuy = false, IsShort = true, TotalCost = 75f, Shares = shares, Price = price
            });

            float expectedBonus = shares * price * 0.10f; // 15.0
            Assert.AreEqual(initialCash + expectedBonus, _ctx.Portfolio.Cash, 0.01f);
        }

        [Test]
        public void ShortProfiteer_OnBuyTrade_NoEffect()
        {
            _mgr.AddRelic("relic_short_profiteer");
            float initialCash = _ctx.Portfolio.Cash;

            _mgr.DispatchAfterTrade(new TradeExecutedEvent
            {
                IsBuy = true, IsShort = false, TotalCost = 100f, Shares = 10, Price = 10f
            });

            Assert.AreEqual(initialCash, _ctx.Portfolio.Cash, 0.01f);
        }

        [Test]
        public void ShortProfiteer_OnSellTrade_NoEffect()
        {
            _mgr.AddRelic("relic_short_profiteer");
            float initialCash = _ctx.Portfolio.Cash;

            _mgr.DispatchAfterTrade(new TradeExecutedEvent
            {
                IsBuy = false, IsShort = false, TotalCost = 100f, Shares = 10, Price = 10f
            });

            Assert.AreEqual(initialCash, _ctx.Portfolio.Cash, 0.01f);
        }

        [Test]
        public void ShortProfiteer_OnCover_NoEffect()
        {
            _mgr.AddRelic("relic_short_profiteer");
            float initialCash = _ctx.Portfolio.Cash;

            // Cover = IsBuy=true, IsShort=true
            _mgr.DispatchAfterTrade(new TradeExecutedEvent
            {
                IsBuy = true, IsShort = true, TotalCost = 100f, Shares = 5, Price = 20f
            });

            Assert.AreEqual(initialCash, _ctx.Portfolio.Cash, 0.01f);
        }

        [Test]
        public void ShortProfiteer_PublishesRelicActivatedEvent()
        {
            _mgr.AddRelic("relic_short_profiteer");
            RelicActivatedEvent? captured = null;
            EventBus.Subscribe<RelicActivatedEvent>(e => captured = e);

            _mgr.DispatchAfterTrade(new TradeExecutedEvent
            {
                IsBuy = false, IsShort = true, TotalCost = 50f, Shares = 1, Price = 50f
            });

            Assert.IsNotNull(captured);
            Assert.AreEqual("relic_short_profiteer", captured.Value.RelicId);
        }

        // ════════════════════════════════════════════════════════════════════
        // RelicManager helper methods (AC 6, 7)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void GetEffectiveCooldown_NoRelics_ReturnsDefault()
        {
            Assert.AreEqual(GameConfig.PostTradeCooldown, _mgr.GetEffectiveCooldown(true));
            Assert.AreEqual(GameConfig.PostTradeCooldown, _mgr.GetEffectiveCooldown(false));
        }

        [Test]
        public void GetEffectiveShortShares_NoRelics_ReturnsDefault()
        {
            Assert.AreEqual(GameConfig.ShortBaseShares, _mgr.GetEffectiveShortShares());
        }

        [Test]
        public void GetEffectiveTradeQuantity_NoRelics_ReturnsBase()
        {
            Assert.AreEqual(1, _mgr.GetEffectiveTradeQuantity(1));
            Assert.AreEqual(5, _mgr.GetEffectiveTradeQuantity(5));
        }

        // ════════════════════════════════════════════════════════════════════
        // Factory integration: all 5 relics create real instances (AC 9)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void Factory_DoubleDealer_CreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_double_dealer");
            Assert.IsInstanceOf<DoubleDealerRelic>(relic);
        }

        [Test]
        public void Factory_QuickDraw_CreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_quick_draw");
            Assert.IsInstanceOf<QuickDrawRelic>(relic);
        }

        [Test]
        public void Factory_ShortMultiplier_CreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_short_multiplier");
            Assert.IsInstanceOf<ShortMultiplierRelic>(relic);
        }

        [Test]
        public void Factory_Skimmer_CreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_skimmer");
            Assert.IsInstanceOf<SkimmerRelic>(relic);
        }

        [Test]
        public void Factory_ShortProfiteer_CreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_short_profiteer");
            Assert.IsInstanceOf<ShortProfiteerRelic>(relic);
        }

        // ════════════════════════════════════════════════════════════════════
        // Remaining pool relics still create instances (regression)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void Factory_NonTradeRelics_StillCreateInstances()
        {
            // Verify relics not in this story still work (StubRelic)
            var relic = RelicFactory.Create("relic_event_trigger");
            Assert.IsNotNull(relic);
            Assert.AreEqual("relic_event_trigger", relic.Id);
        }
    }
}
