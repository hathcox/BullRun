using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace BullRun.Tests.Items.Relics
{
    /// <summary>
    /// Story 17.7: Tests for special relics — Event Catalyst and Relic Expansion.
    /// </summary>
    [TestFixture]
    public class SpecialRelicTests
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
            SetTradingActive(false);
            EventBus.Clear();
            RelicFactory.ResetRegistry();
        }

        /// <summary>
        /// Sets TradingState.IsActive via reflection for testing relic behavior during trading phase.
        /// </summary>
        private static void SetTradingActive(bool value)
        {
            typeof(TradingState)
                .GetProperty("IsActive", BindingFlags.Public | BindingFlags.Static)
                .SetValue(null, value);
        }

        // ════════════════════════════════════════════════════════════════════
        // Event Catalyst — ID and Factory (AC 1, 13)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void EventCatalystRelic_HasCorrectId()
        {
            var relic = new EventCatalystRelic();
            Assert.AreEqual("relic_event_catalyst", relic.Id);
        }

        [Test]
        public void EventCatalyst_FactoryCreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_event_catalyst");
            Assert.IsNotNull(relic);
            Assert.IsInstanceOf<EventCatalystRelic>(relic);
        }

        // ════════════════════════════════════════════════════════════════════
        // Event Catalyst — Rep gain triggers (AC 1, 2)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void EventCatalyst_RepLoss_DoesNotTrigger()
        {
            _mgr.AddRelic("relic_event_catalyst");
            RelicActivatedEvent? captured = null;
            EventBus.Subscribe<RelicActivatedEvent>(e => captured = e);

            // Rep loss: oldRep > newRep
            _mgr.DispatchReputationChanged(10, 5);

            Assert.IsNull(captured, "Event Catalyst should NOT trigger on rep loss");
        }

        [Test]
        public void EventCatalyst_NoChange_DoesNotTrigger()
        {
            _mgr.AddRelic("relic_event_catalyst");
            RelicActivatedEvent? captured = null;
            EventBus.Subscribe<RelicActivatedEvent>(e => captured = e);

            _mgr.DispatchReputationChanged(10, 10);

            Assert.IsNull(captured, "Event Catalyst should NOT trigger when rep unchanged");
        }

        // ════════════════════════════════════════════════════════════════════
        // Event Catalyst — Trading phase guard (AC 3)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void EventCatalyst_WhenTradingNotActive_DoesNotTrigger()
        {
            // TradingState.IsActive is false by default in test context
            Assert.IsFalse(TradingState.IsActive);

            _mgr.AddRelic("relic_event_catalyst");
            MarketEventFiredEvent? captured = null;
            EventBus.Subscribe<MarketEventFiredEvent>(e => captured = e);

            // Even with large rep gain, should not trigger since trading is not active
            _mgr.DispatchReputationChanged(0, 1000);

            Assert.IsNull(captured, "Event Catalyst should NOT trigger outside trading phase");
        }

        // ════════════════════════════════════════════════════════════════════
        // Event Catalyst — Probability and event firing (AC 2, 4, 5, 12)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void EventCatalyst_DeterministicAlwaysTrigger_FiresEventPerRepPoint()
        {
            SetTradingActive(true);
            _mgr.AddRelic("relic_event_catalyst");
            var relic = (EventCatalystRelic)_mgr.GetRelicById("relic_event_catalyst");
            relic.RandomProvider = () => 0.005f; // Always < 0.01 = always triggers

            int eventCount = 0;
            EventBus.Subscribe<MarketEventFiredEvent>(e => eventCount++);

            _mgr.DispatchReputationChanged(0, 5);

            Assert.AreEqual(5, eventCount, "Each rep point should trigger one event when random always hits");
        }

        [Test]
        public void EventCatalyst_DeterministicNeverTrigger_FiresNoEvents()
        {
            SetTradingActive(true);
            _mgr.AddRelic("relic_event_catalyst");
            var relic = (EventCatalystRelic)_mgr.GetRelicById("relic_event_catalyst");
            relic.RandomProvider = () => 0.5f; // Always >= 0.01 = never triggers

            int eventCount = 0;
            EventBus.Subscribe<MarketEventFiredEvent>(e => eventCount++);

            _mgr.DispatchReputationChanged(0, 100);

            Assert.AreEqual(0, eventCount, "No events should fire when random never hits threshold");
        }

        [Test]
        public void EventCatalyst_WhenTradingActive_PublishesRelicActivatedEvent()
        {
            SetTradingActive(true);
            _mgr.AddRelic("relic_event_catalyst");
            var relic = (EventCatalystRelic)_mgr.GetRelicById("relic_event_catalyst");
            relic.RandomProvider = () => 0.005f; // Always triggers

            RelicActivatedEvent? captured = null;
            EventBus.Subscribe<RelicActivatedEvent>(e => captured = e);

            _mgr.DispatchReputationChanged(0, 1);

            Assert.IsNotNull(captured, "RelicActivatedEvent should be published when EventCatalyst triggers");
            Assert.AreEqual("relic_event_catalyst", captured.Value.RelicId);
        }

        [Test]
        public void EventCatalyst_WhenTradingActive_LargeRepGain_StatisticallyTriggers()
        {
            SetTradingActive(true);
            _mgr.AddRelic("relic_event_catalyst");

            int eventCount = 0;
            EventBus.Subscribe<MarketEventFiredEvent>(e => eventCount++);

            // 10000 rep gain at 1% per point: expected ~100 events, P(0) ≈ 2.2e-44
            _mgr.DispatchReputationChanged(0, 10000);

            Assert.Greater(eventCount, 0, "With 10000 rep gain at 1%/point, at least one event should fire");
        }

        // ════════════════════════════════════════════════════════════════════
        // Event Catalyst — No EventScheduler safety (AC 4)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void EventCatalyst_NullEventScheduler_DoesNotThrow()
        {
            var ctx = new RunContext(1, 1, new Portfolio(1000f));
            ctx.EventScheduler = null;
            ctx.RelicManager.AddRelic("relic_event_catalyst");

            // Should not throw even though EventScheduler is null
            Assert.DoesNotThrow(() =>
            {
                ctx.RelicManager.DispatchReputationChanged(0, 100);
            });
        }

        // ════════════════════════════════════════════════════════════════════
        // Relic Expansion — ID and Factory (AC 6, 13)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void RelicExpansionRelic_HasCorrectId()
        {
            var relic = new RelicExpansionRelic();
            Assert.AreEqual("relic_relic_expansion", relic.Id);
        }

        [Test]
        public void RelicExpansion_FactoryCreatesRealInstance()
        {
            var relic = RelicFactory.Create("relic_relic_expansion");
            Assert.IsNotNull(relic);
            Assert.IsInstanceOf<RelicExpansionRelic>(relic);
        }

        // ════════════════════════════════════════════════════════════════════
        // Relic Expansion — OnSellSelf increments BonusRelicSlots (AC 7)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void RelicExpansion_OnSellSelf_IncrementsBonusRelicSlots()
        {
            Assert.AreEqual(0, _ctx.BonusRelicSlots);

            var relic = new RelicExpansionRelic();
            relic.OnSellSelf(_ctx);

            Assert.AreEqual(1, _ctx.BonusRelicSlots);
        }

        [Test]
        public void RelicExpansion_MultipleSells_StackBonusSlots()
        {
            var relic1 = new RelicExpansionRelic();
            var relic2 = new RelicExpansionRelic();

            relic1.OnSellSelf(_ctx);
            relic2.OnSellSelf(_ctx);

            Assert.AreEqual(2, _ctx.BonusRelicSlots);
        }

        [Test]
        public void RelicExpansion_PublishesRelicActivatedEvent()
        {
            var relic = new RelicExpansionRelic();
            RelicActivatedEvent? captured = null;
            EventBus.Subscribe<RelicActivatedEvent>(e => captured = e);

            relic.OnSellSelf(_ctx);

            Assert.IsNotNull(captured);
            Assert.AreEqual("relic_relic_expansion", captured.Value.RelicId);
        }

        // ════════════════════════════════════════════════════════════════════
        // Relic Expansion — Sell refund is 0 (AC 6)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void RelicExpansion_GetSellValue_ReturnsZero()
        {
            var relic = new RelicExpansionRelic();
            int? value = relic.GetSellValue(_ctx);

            Assert.IsNotNull(value);
            Assert.AreEqual(0, value.Value);
        }

        [Test]
        public void RelicExpansion_SellRelic_RefundsZeroRep()
        {
            _ctx.Reputation.Add(100);
            _mgr.AddRelic("relic_relic_expansion");
            int repBefore = _ctx.Reputation.Current;

            var shop = new ShopTransaction();
            shop.SellRelic(_ctx, "relic_relic_expansion");

            Assert.AreEqual(repBefore, _ctx.Reputation.Current, "Selling Relic Expansion should refund 0 rep");
        }

        [Test]
        public void RelicExpansion_SellRelic_DispatchesOnSellSelfAndIncrementsBonusSlots()
        {
            _mgr.AddRelic("relic_relic_expansion");
            Assert.AreEqual(0, _ctx.BonusRelicSlots);

            var shop = new ShopTransaction();
            var result = shop.SellRelic(_ctx, "relic_relic_expansion");

            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.AreEqual(1, _ctx.BonusRelicSlots);
        }

        // ════════════════════════════════════════════════════════════════════
        // Relic Expansion — No passive effect while held (AC 9)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void RelicExpansion_WhileHeld_NoPassiveEffect()
        {
            _mgr.AddRelic("relic_relic_expansion");
            Assert.AreEqual(0, _ctx.BonusRelicSlots, "BonusRelicSlots should remain 0 while relic is held");

            // Dispatch various hooks — none should change BonusRelicSlots
            _mgr.DispatchRoundStart(new RoundStartedEvent { RoundNumber = 1, Act = 1 });
            Assert.AreEqual(0, _ctx.BonusRelicSlots);

            _mgr.DispatchRoundEnd(new MarketClosedEvent { RoundNumber = 1, RoundProfit = 0f, FinalCash = 1000f });
            Assert.AreEqual(0, _ctx.BonusRelicSlots);

            _mgr.DispatchShopOpen();
            Assert.AreEqual(0, _ctx.BonusRelicSlots);
        }

        // ════════════════════════════════════════════════════════════════════
        // GetEffectiveMaxRelicSlots includes BonusRelicSlots (AC 8)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void GetEffectiveMaxRelicSlots_IncludesBonusRelicSlots()
        {
            int baseLine = ShopTransaction.GetEffectiveMaxRelicSlots(_ctx);
            _ctx.BonusRelicSlots = 2;
            int withBonus = ShopTransaction.GetEffectiveMaxRelicSlots(_ctx);

            Assert.AreEqual(baseLine + 2, withBonus);
        }

        [Test]
        public void GetEffectiveMaxRelicSlots_BonusRelicSlots_DefaultZero()
        {
            Assert.AreEqual(0, _ctx.BonusRelicSlots);
            int expected = GameConfig.MaxRelicSlots;
            Assert.AreEqual(expected, ShopTransaction.GetEffectiveMaxRelicSlots(_ctx));
        }

        [Test]
        public void GetEffectiveMaxRelicSlots_ExpandedInventoryPlusBonusSlots()
        {
            _ctx.OwnedExpansions.Add(ExpansionDefinitions.ExpandedInventory);
            _ctx.BonusRelicSlots = 1;

            int expected = GameConfig.MaxRelicSlots + 2 + 1; // base + expanded_inventory(2) + bonus(1)
            Assert.AreEqual(expected, ShopTransaction.GetEffectiveMaxRelicSlots(_ctx));
        }

        // ════════════════════════════════════════════════════════════════════
        // BonusRelicSlots — resets on new run (AC 7)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void BonusRelicSlots_ResetForNewRun_ClearsToZero()
        {
            _ctx.BonusRelicSlots = 3;
            Assert.AreEqual(3, _ctx.BonusRelicSlots);

            _ctx.ResetForNewRun();

            Assert.AreEqual(0, _ctx.BonusRelicSlots);
        }

        [Test]
        public void BonusRelicSlots_DefaultIsZero()
        {
            var ctx = new RunContext(1, 1, new Portfolio(1000f));
            Assert.AreEqual(0, ctx.BonusRelicSlots);
        }

        // ════════════════════════════════════════════════════════════════════
        // Full sell flow integration test
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void RelicExpansion_FullSellFlow_SlotsIncreasedAndRelicRemoved()
        {
            _ctx.Reputation.Add(100);
            _mgr.AddRelic("relic_relic_expansion");
            Assert.IsTrue(_ctx.OwnedRelics.Contains("relic_relic_expansion"));
            Assert.AreEqual(0, _ctx.BonusRelicSlots);

            int slotsBefore = ShopTransaction.GetEffectiveMaxRelicSlots(_ctx);

            var shop = new ShopTransaction();
            var result = shop.SellRelic(_ctx, "relic_relic_expansion");

            Assert.AreEqual(ShopPurchaseResult.Success, result);
            Assert.IsFalse(_ctx.OwnedRelics.Contains("relic_relic_expansion"));
            Assert.AreEqual(1, _ctx.BonusRelicSlots);
            Assert.AreEqual(slotsBefore + 1, ShopTransaction.GetEffectiveMaxRelicSlots(_ctx));
        }

        // ════════════════════════════════════════════════════════════════════
        // Factory: both relics create real instances (AC 13)
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void Factory_EventCatalyst_NotStubRelic()
        {
            var relic = RelicFactory.Create("relic_event_catalyst");
            Assert.IsNotNull(relic);
            Assert.IsFalse(relic is StubRelic, "relic_event_catalyst should not be a StubRelic");
        }

        [Test]
        public void Factory_RelicExpansion_NotStubRelic()
        {
            var relic = RelicFactory.Create("relic_relic_expansion");
            Assert.IsNotNull(relic);
            Assert.IsFalse(relic is StubRelic, "relic_relic_expansion should not be a StubRelic");
        }

        // ════════════════════════════════════════════════════════════════════
        // Regression: other relics still create properly
        // ════════════════════════════════════════════════════════════════════

        [Test]
        public void Factory_ExistingRelics_StillCreateCorrectInstances()
        {
            Assert.IsInstanceOf<DoubleDealerRelic>(RelicFactory.Create("relic_double_dealer"));
            Assert.IsInstanceOf<CatalystTraderRelic>(RelicFactory.Create("relic_event_trigger"));
            Assert.IsInstanceOf<RepDoublerRelic>(RelicFactory.Create("relic_rep_doubler"));
            Assert.IsInstanceOf<TimeBuyerRelic>(RelicFactory.Create("relic_time_buyer"));
        }
    }
}
