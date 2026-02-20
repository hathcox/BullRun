using System.Collections.Generic;
using NUnit.Framework;

namespace BullRun.Tests.Items.Relics
{
    /// <summary>
    /// Story 17.4: Tests for event interaction relics — Catalyst Trader, Event Storm,
    /// Loss Liquidator, Profit Refresh, Bull Believer, and EventScheduler modifications.
    /// </summary>
    [TestFixture]
    public class EventRelicTests
    {
        private RunContext _ctx;
        private RelicManager _mgr;
        private EventScheduler _scheduler;
        private EventEffects _eventEffects;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            RelicFactory.ResetRegistry();
            _ctx = new RunContext(1, 1, new Portfolio(1000f));
            _mgr = _ctx.RelicManager;

            // Create EventScheduler with deterministic random for testing
            _eventEffects = new EventEffects();
            _scheduler = new EventScheduler(_eventEffects, new System.Random(42));
            _ctx.EventScheduler = _scheduler;

            // Initialize a round so ForceFireRandomEvent has tier/stocks context
            var stock = new StockInstance();
            stock.Initialize(1, "TEST", StockTier.Penny, 100f, TrendDirection.Neutral, 0f);
            var stocks = new List<StockInstance> { stock };
            _eventEffects.SetActiveStocks(stocks);
            _scheduler.InitializeRound(1, 1, StockTier.Penny, stocks, 60f);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
            RelicFactory.ResetRegistry();
        }

        // ════════════════════════════════════════════════════════════════════
        // Catalyst Trader (AC 1)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void CatalystTraderRelic_HasCorrectId()
        {
            var relic = new CatalystTraderRelic();
            Assert.AreEqual("relic_event_trigger", relic.Id);
        }

        [Test]
        public void CatalystTrader_FactoryCreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_event_trigger");
            Assert.IsNotNull(relic);
            Assert.IsInstanceOf<CatalystTraderRelic>(relic);
        }

        [Test]
        public void CatalystTrader_OnBuyTrade_FiresMarketEvent()
        {
            _mgr.AddRelic("relic_event_trigger");
            MarketEventFiredEvent? captured = null;
            EventBus.Subscribe<MarketEventFiredEvent>(e => captured = e);

            _mgr.DispatchAfterTrade(new TradeExecutedEvent
            {
                IsBuy = true, IsShort = false, TotalCost = 100f, Shares = 10, Price = 10f
            });

            Assert.IsNotNull(captured, "MarketEventFiredEvent should fire on buy trade with Catalyst Trader");
        }

        [Test]
        public void CatalystTrader_OnSellTrade_DoesNotFireEvent()
        {
            _mgr.AddRelic("relic_event_trigger");
            MarketEventFiredEvent? captured = null;
            EventBus.Subscribe<MarketEventFiredEvent>(e => captured = e);

            _mgr.DispatchAfterTrade(new TradeExecutedEvent
            {
                IsBuy = false, IsShort = false, TotalCost = 100f, Shares = 10, Price = 10f
            });

            Assert.IsNull(captured, "MarketEventFiredEvent should NOT fire on sell trade");
        }

        [Test]
        public void CatalystTrader_OnShortTrade_DoesNotFireEvent()
        {
            _mgr.AddRelic("relic_event_trigger");
            MarketEventFiredEvent? captured = null;
            EventBus.Subscribe<MarketEventFiredEvent>(e => captured = e);

            _mgr.DispatchAfterTrade(new TradeExecutedEvent
            {
                IsBuy = false, IsShort = true, TotalCost = 50f, Shares = 5, Price = 10f
            });

            Assert.IsNull(captured, "MarketEventFiredEvent should NOT fire on short trade");
        }

        [Test]
        public void CatalystTrader_PublishesRelicActivatedEvent()
        {
            _mgr.AddRelic("relic_event_trigger");
            RelicActivatedEvent? captured = null;
            EventBus.Subscribe<RelicActivatedEvent>(e => captured = e);

            _mgr.DispatchAfterTrade(new TradeExecutedEvent
            {
                IsBuy = true, IsShort = false, TotalCost = 100f, Shares = 10, Price = 10f
            });

            Assert.IsNotNull(captured);
            Assert.AreEqual("relic_event_trigger", captured.Value.RelicId);
        }

        [Test]
        public void CatalystTrader_BuyCooldownIncreased()
        {
            _mgr.AddRelic("relic_event_trigger");
            float expected = GameConfig.PostTradeCooldown + 3f;
            Assert.AreEqual(expected, _mgr.GetEffectiveCooldown(true), 0.01f);
        }

        [Test]
        public void CatalystTrader_SellCooldownUnaffected()
        {
            _mgr.AddRelic("relic_event_trigger");
            Assert.AreEqual(GameConfig.PostTradeCooldown, _mgr.GetEffectiveCooldown(false), 0.01f);
        }

        [Test]
        public void CatalystTrader_PlusQuickDraw_BuyCooldown3s()
        {
            // Quick Draw: buy = 0, Catalyst Trader: +3 = 3s total
            _mgr.AddRelic("relic_quick_draw");
            _mgr.AddRelic("relic_event_trigger");
            Assert.AreEqual(3f, _mgr.GetEffectiveCooldown(true), 0.01f);
        }

        // ════════════════════════════════════════════════════════════════════
        // Event Storm (AC 2)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void EventStormRelic_HasCorrectId()
        {
            var relic = new EventStormRelic();
            Assert.AreEqual("relic_event_storm", relic.Id);
        }

        [Test]
        public void EventStorm_FactoryCreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_event_storm");
            Assert.IsNotNull(relic);
            Assert.IsInstanceOf<EventStormRelic>(relic);
        }

        [Test]
        public void EventStorm_OnRoundStart_SetsEventCountMultiplier()
        {
            _mgr.AddRelic("relic_event_storm");
            _mgr.DispatchRoundStart(new RoundStartedEvent { RoundNumber = 1, Act = 1 });

            Assert.AreEqual(2.0f, _scheduler.EventCountMultiplier, 0.01f);
        }

        [Test]
        public void EventStorm_OnRoundStart_SetsImpactMultiplier()
        {
            _mgr.AddRelic("relic_event_storm");
            _mgr.DispatchRoundStart(new RoundStartedEvent { RoundNumber = 1, Act = 1 });

            Assert.AreEqual(0.75f, _scheduler.ImpactMultiplier, 0.01f);
        }

        [Test]
        public void EventStorm_PublishesRelicActivatedEvent()
        {
            _mgr.AddRelic("relic_event_storm");
            RelicActivatedEvent? captured = null;
            EventBus.Subscribe<RelicActivatedEvent>(e => captured = e);

            _mgr.DispatchRoundStart(new RoundStartedEvent { RoundNumber = 1, Act = 1 });

            Assert.IsNotNull(captured);
            Assert.AreEqual("relic_event_storm", captured.Value.RelicId);
        }

        // ════════════════════════════════════════════════════════════════════
        // Loss Liquidator (AC 3)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void LossLiquidatorRelic_HasCorrectId()
        {
            var relic = new LossLiquidatorRelic();
            Assert.AreEqual("relic_loss_liquidator", relic.Id);
        }

        [Test]
        public void LossLiquidator_FactoryCreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_loss_liquidator");
            Assert.IsNotNull(relic);
            Assert.IsInstanceOf<LossLiquidatorRelic>(relic);
        }

        [Test]
        public void LossLiquidator_SellAtLoss_FiresMarketEvent()
        {
            _mgr.AddRelic("relic_loss_liquidator");
            MarketEventFiredEvent? captured = null;
            EventBus.Subscribe<MarketEventFiredEvent>(e => captured = e);

            _mgr.DispatchAfterTrade(new TradeExecutedEvent
            {
                IsBuy = false, IsShort = false, ProfitLoss = -50f, TotalCost = 100f, Shares = 10, Price = 5f
            });

            Assert.IsNotNull(captured, "MarketEventFiredEvent should fire on sell-at-loss");
        }

        [Test]
        public void LossLiquidator_SellAtProfit_DoesNotFireEvent()
        {
            _mgr.AddRelic("relic_loss_liquidator");
            MarketEventFiredEvent? captured = null;
            EventBus.Subscribe<MarketEventFiredEvent>(e => captured = e);

            _mgr.DispatchAfterTrade(new TradeExecutedEvent
            {
                IsBuy = false, IsShort = false, ProfitLoss = 50f, TotalCost = 100f, Shares = 10, Price = 15f
            });

            Assert.IsNull(captured, "MarketEventFiredEvent should NOT fire on sell-at-profit");
        }

        [Test]
        public void LossLiquidator_SellAtBreakeven_DoesNotFireEvent()
        {
            _mgr.AddRelic("relic_loss_liquidator");
            MarketEventFiredEvent? captured = null;
            EventBus.Subscribe<MarketEventFiredEvent>(e => captured = e);

            _mgr.DispatchAfterTrade(new TradeExecutedEvent
            {
                IsBuy = false, IsShort = false, ProfitLoss = 0f, TotalCost = 100f, Shares = 10, Price = 10f
            });

            Assert.IsNull(captured, "MarketEventFiredEvent should NOT fire on sell-at-breakeven");
        }

        [Test]
        public void LossLiquidator_BuyTrade_DoesNotFireEvent()
        {
            _mgr.AddRelic("relic_loss_liquidator");
            MarketEventFiredEvent? captured = null;
            EventBus.Subscribe<MarketEventFiredEvent>(e => captured = e);

            _mgr.DispatchAfterTrade(new TradeExecutedEvent
            {
                IsBuy = true, IsShort = false, ProfitLoss = 0f, TotalCost = 100f, Shares = 10, Price = 10f
            });

            Assert.IsNull(captured, "MarketEventFiredEvent should NOT fire on buy trade");
        }

        [Test]
        public void LossLiquidator_PublishesRelicActivatedEvent()
        {
            _mgr.AddRelic("relic_loss_liquidator");
            RelicActivatedEvent? captured = null;
            EventBus.Subscribe<RelicActivatedEvent>(e => captured = e);

            _mgr.DispatchAfterTrade(new TradeExecutedEvent
            {
                IsBuy = false, IsShort = false, ProfitLoss = -10f, TotalCost = 100f, Shares = 10, Price = 9f
            });

            Assert.IsNotNull(captured);
            Assert.AreEqual("relic_loss_liquidator", captured.Value.RelicId);
        }

        // ════════════════════════════════════════════════════════════════════
        // Profit Refresh (AC 4)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void ProfitRefreshRelic_HasCorrectId()
        {
            var relic = new ProfitRefreshRelic();
            Assert.AreEqual("relic_profit_refresh", relic.Id);
        }

        [Test]
        public void ProfitRefresh_FactoryCreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_profit_refresh");
            Assert.IsNotNull(relic);
            Assert.IsInstanceOf<ProfitRefreshRelic>(relic);
        }

        [Test]
        public void ProfitRefresh_SellAtProfit_PublishesBuyReadyFeedback()
        {
            _mgr.AddRelic("relic_profit_refresh");
            TradeFeedbackEvent? captured = null;
            EventBus.Subscribe<TradeFeedbackEvent>(e => captured = e);

            _mgr.DispatchAfterTrade(new TradeExecutedEvent
            {
                IsBuy = false, IsShort = false, ProfitLoss = 50f, TotalCost = 150f, Shares = 10, Price = 15f
            });

            Assert.IsNotNull(captured);
            Assert.AreEqual("BUY READY", captured.Value.Message);
            Assert.IsTrue(captured.Value.IsSuccess);
        }

        [Test]
        public void ProfitRefresh_SellAtLoss_DoesNotTrigger()
        {
            _mgr.AddRelic("relic_profit_refresh");
            TradeFeedbackEvent? captured = null;
            EventBus.Subscribe<TradeFeedbackEvent>(e => captured = e);

            _mgr.DispatchAfterTrade(new TradeExecutedEvent
            {
                IsBuy = false, IsShort = false, ProfitLoss = -10f, TotalCost = 90f, Shares = 10, Price = 9f
            });

            Assert.IsNull(captured, "Profit Refresh should NOT trigger on sell-at-loss");
        }

        [Test]
        public void ProfitRefresh_SellAtBreakeven_DoesNotTrigger()
        {
            _mgr.AddRelic("relic_profit_refresh");
            TradeFeedbackEvent? captured = null;
            EventBus.Subscribe<TradeFeedbackEvent>(e => captured = e);

            _mgr.DispatchAfterTrade(new TradeExecutedEvent
            {
                IsBuy = false, IsShort = false, ProfitLoss = 0f, TotalCost = 100f, Shares = 10, Price = 10f
            });

            Assert.IsNull(captured, "Profit Refresh should NOT trigger on sell-at-breakeven");
        }

        [Test]
        public void ProfitRefresh_BuyTrade_DoesNotTrigger()
        {
            _mgr.AddRelic("relic_profit_refresh");
            TradeFeedbackEvent? captured = null;
            EventBus.Subscribe<TradeFeedbackEvent>(e => captured = e);

            _mgr.DispatchAfterTrade(new TradeExecutedEvent
            {
                IsBuy = true, IsShort = false, ProfitLoss = 0f, TotalCost = 100f, Shares = 10, Price = 10f
            });

            Assert.IsNull(captured, "Profit Refresh should NOT trigger on buy trade");
        }

        [Test]
        public void ProfitRefresh_PublishesRelicActivatedEvent()
        {
            _mgr.AddRelic("relic_profit_refresh");
            RelicActivatedEvent? captured = null;
            EventBus.Subscribe<RelicActivatedEvent>(e => captured = e);

            _mgr.DispatchAfterTrade(new TradeExecutedEvent
            {
                IsBuy = false, IsShort = false, ProfitLoss = 25f, TotalCost = 125f, Shares = 10, Price = 12.5f
            });

            Assert.IsNotNull(captured);
            Assert.AreEqual("relic_profit_refresh", captured.Value.RelicId);
        }

        // ════════════════════════════════════════════════════════════════════
        // Bull Believer (AC 5)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void BullBelieverRelic_HasCorrectId()
        {
            var relic = new BullBelieverRelic();
            Assert.AreEqual("relic_bull_believer", relic.Id);
        }

        [Test]
        public void BullBeliever_FactoryCreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_bull_believer");
            Assert.IsNotNull(relic);
            Assert.IsInstanceOf<BullBelieverRelic>(relic);
        }

        [Test]
        public void BullBeliever_OnAcquired_SetsShortingDisabled()
        {
            Assert.IsFalse(_ctx.ShortingDisabled);
            _mgr.AddRelic("relic_bull_believer");
            Assert.IsTrue(_ctx.ShortingDisabled);
        }

        [Test]
        public void BullBeliever_ShortingDisabled_DefaultFalse()
        {
            Assert.IsFalse(_ctx.ShortingDisabled);
        }

        [Test]
        public void ShortingDisabled_ResetForNewRun_ClearsFlag()
        {
            _ctx.ShortingDisabled = true;
            Assert.IsTrue(_ctx.ShortingDisabled);
            _ctx.ResetForNewRun();
            Assert.IsFalse(_ctx.ShortingDisabled);
        }

        [Test]
        public void BullBeliever_OnRoundStart_SetsPositiveImpactMultiplier()
        {
            _mgr.AddRelic("relic_bull_believer");
            _mgr.DispatchRoundStart(new RoundStartedEvent { RoundNumber = 1, Act = 1 });

            Assert.AreEqual(2.0f, _scheduler.PositiveImpactMultiplier, 0.01f);
        }

        [Test]
        public void BullBeliever_NegativeEventsUnaffected()
        {
            _mgr.AddRelic("relic_bull_believer");
            _mgr.DispatchRoundStart(new RoundStartedEvent { RoundNumber = 1, Act = 1 });

            // ImpactMultiplier should remain 1.0 (only PositiveImpactMultiplier is set)
            Assert.AreEqual(1.0f, _scheduler.ImpactMultiplier, 0.01f);
        }

        [Test]
        public void BullBeliever_PublishesRelicActivatedEvent()
        {
            _mgr.AddRelic("relic_bull_believer");
            RelicActivatedEvent? captured = null;
            EventBus.Subscribe<RelicActivatedEvent>(e => captured = e);

            _mgr.DispatchRoundStart(new RoundStartedEvent { RoundNumber = 1, Act = 1 });

            Assert.IsNotNull(captured);
            Assert.AreEqual("relic_bull_believer", captured.Value.RelicId);
        }

        // ════════════════════════════════════════════════════════════════════
        // ForceFireRandomEvent (AC 6)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void ForceFireRandomEvent_FiresMarketEvent()
        {
            MarketEventFiredEvent? captured = null;
            EventBus.Subscribe<MarketEventFiredEvent>(e => captured = e);

            _scheduler.ForceFireRandomEvent();

            Assert.IsNotNull(captured, "ForceFireRandomEvent should publish a MarketEventFiredEvent");
        }

        [Test]
        public void ForceFireRandomEvent_EventHasValidType()
        {
            MarketEventFiredEvent? captured = null;
            EventBus.Subscribe<MarketEventFiredEvent>(e => captured = e);

            _scheduler.ForceFireRandomEvent();

            Assert.IsNotNull(captured);
            // Event type should be a valid MarketEventType enum value
            Assert.IsTrue(System.Enum.IsDefined(typeof(MarketEventType), captured.Value.EventType));
        }

        [Test]
        public void ForceFireRandomEvent_EventHasNonZeroPriceEffect()
        {
            MarketEventFiredEvent? captured = null;
            EventBus.Subscribe<MarketEventFiredEvent>(e => captured = e);

            _scheduler.ForceFireRandomEvent();

            Assert.IsNotNull(captured);
            Assert.AreNotEqual(0f, captured.Value.PriceEffectPercent);
        }

        // ════════════════════════════════════════════════════════════════════
        // EventCountMultiplier and ImpactMultiplier (AC 7)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void EventScheduler_MultipliersPreservedAcrossInitializeRound()
        {
            // Story 17.4 review fix: Multipliers are now reset by TradingState, not InitializeRound.
            // InitializeRound should preserve relic-set multiplier values.
            _scheduler.EventCountMultiplier = 2.0f;
            _scheduler.ImpactMultiplier = 0.75f;
            _scheduler.PositiveImpactMultiplier = 2.0f;

            var stock = new StockInstance();
            stock.Initialize(1, "TEST", StockTier.Penny, 100f, TrendDirection.Neutral, 0f);
            var stocks = new List<StockInstance> { stock };
            _scheduler.InitializeRound(1, 1, StockTier.Penny, stocks, 60f);

            Assert.AreEqual(2.0f, _scheduler.EventCountMultiplier, 0.01f);
            Assert.AreEqual(0.75f, _scheduler.ImpactMultiplier, 0.01f);
            Assert.AreEqual(2.0f, _scheduler.PositiveImpactMultiplier, 0.01f);
        }

        [Test]
        public void EventCountMultiplier_DoubleProducesMoreEvents()
        {
            // Story 17.4 review fix: Verify EventCountMultiplier actually affects scheduled event count
            var effects = new EventEffects();
            var scheduler1 = new EventScheduler(effects, new System.Random(42));
            var scheduler2 = new EventScheduler(effects, new System.Random(42));

            var stock = new StockInstance();
            stock.Initialize(1, "TEST", StockTier.Penny, 100f, TrendDirection.Neutral, 0f);
            var stocks = new List<StockInstance> { stock };

            // Normal multiplier (1.0)
            scheduler1.InitializeRound(1, 1, StockTier.Penny, stocks, 60f);
            int normalCount = scheduler1.ScheduledEventCount;

            // Double multiplier
            scheduler2.EventCountMultiplier = 2.0f;
            scheduler2.InitializeRound(1, 1, StockTier.Penny, stocks, 60f);
            int doubleCount = scheduler2.ScheduledEventCount;

            Assert.Greater(doubleCount, normalCount,
                "EventCountMultiplier = 2.0 should produce more scheduled events than 1.0");
        }

        [Test]
        public void EventCountMultiplier_DefaultIsOne()
        {
            var effects = new EventEffects();
            var scheduler = new EventScheduler(effects);
            Assert.AreEqual(1.0f, scheduler.EventCountMultiplier, 0.01f);
        }

        [Test]
        public void ImpactMultiplier_DefaultIsOne()
        {
            var effects = new EventEffects();
            var scheduler = new EventScheduler(effects);
            Assert.AreEqual(1.0f, scheduler.ImpactMultiplier, 0.01f);
        }

        [Test]
        public void PositiveImpactMultiplier_DefaultIsOne()
        {
            var effects = new EventEffects();
            var scheduler = new EventScheduler(effects);
            Assert.AreEqual(1.0f, scheduler.PositiveImpactMultiplier, 0.01f);
        }

        // ════════════════════════════════════════════════════════════════════
        // GetEffectiveCooldown with Catalyst Trader (AC 1 cooldown stacking)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void GetEffectiveCooldown_CatalystTraderOnly_BuyAdds3s()
        {
            _mgr.AddRelic("relic_event_trigger");
            float expected = GameConfig.PostTradeCooldown + 3f;
            Assert.AreEqual(expected, _mgr.GetEffectiveCooldown(true), 0.01f);
        }

        [Test]
        public void GetEffectiveCooldown_CatalystTraderOnly_SellUnaffected()
        {
            _mgr.AddRelic("relic_event_trigger");
            Assert.AreEqual(GameConfig.PostTradeCooldown, _mgr.GetEffectiveCooldown(false), 0.01f);
        }

        [Test]
        public void GetEffectiveCooldown_QuickDrawPlusCatalystTrader_BuyCooldown3s()
        {
            _mgr.AddRelic("relic_quick_draw");
            _mgr.AddRelic("relic_event_trigger");
            // Quick Draw: buy = 0, Catalyst Trader: +3 = 3
            Assert.AreEqual(3f, _mgr.GetEffectiveCooldown(true), 0.01f);
        }

        [Test]
        public void GetEffectiveCooldown_QuickDrawPlusCatalystTrader_SellIsDouble()
        {
            _mgr.AddRelic("relic_quick_draw");
            _mgr.AddRelic("relic_event_trigger");
            // Quick Draw: sell = 2x base, Catalyst Trader: no effect on sell
            float expected = GameConfig.PostTradeCooldown * 2f;
            Assert.AreEqual(expected, _mgr.GetEffectiveCooldown(false), 0.01f);
        }

        // ════════════════════════════════════════════════════════════════════
        // Factory integration: all 5 relics create real instances (AC 10)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void Factory_CatalystTrader_CreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_event_trigger");
            Assert.IsInstanceOf<CatalystTraderRelic>(relic);
        }

        [Test]
        public void Factory_EventStorm_CreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_event_storm");
            Assert.IsInstanceOf<EventStormRelic>(relic);
        }

        [Test]
        public void Factory_LossLiquidator_CreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_loss_liquidator");
            Assert.IsInstanceOf<LossLiquidatorRelic>(relic);
        }

        [Test]
        public void Factory_ProfitRefresh_CreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_profit_refresh");
            Assert.IsInstanceOf<ProfitRefreshRelic>(relic);
        }

        [Test]
        public void Factory_BullBeliever_CreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_bull_believer");
            Assert.IsInstanceOf<BullBelieverRelic>(relic);
        }

        // ════════════════════════════════════════════════════════════════════
        // Regression: remaining pool relics still create instances
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void Factory_NonEventRelics_StillCreateInstances()
        {
            // Verify relics not in this story still work (StubRelic for 17.5-17.7)
            var relic = RelicFactory.Create("relic_double_dealer");
            Assert.IsNotNull(relic);
            Assert.IsInstanceOf<DoubleDealerRelic>(relic);
        }

        // ════════════════════════════════════════════════════════════════════
        // Event Storm + Bull Believer interaction
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void EventStormAndBullBeliever_MultipliersStack()
        {
            _mgr.AddRelic("relic_event_storm");
            _mgr.AddRelic("relic_bull_believer");
            _mgr.DispatchRoundStart(new RoundStartedEvent { RoundNumber = 1, Act = 1 });

            // Event Storm sets ImpactMultiplier = 0.75, Bull Believer sets PositiveImpactMultiplier = 2.0
            Assert.AreEqual(0.75f, _scheduler.ImpactMultiplier, 0.01f);
            Assert.AreEqual(2.0f, _scheduler.PositiveImpactMultiplier, 0.01f);
        }
    }
}
