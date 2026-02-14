using System.Collections.Generic;
using NUnit.Framework;

namespace BullRun.Tests.Events
{
    /// <summary>
    /// FIX-9: Verifies all events target the single active stock.
    /// No global events, no multi-stock routing, no random stock selection.
    /// </summary>
    [TestFixture]
    public class SingleStockEventTests
    {
        private EventEffects _effects;
        private EventScheduler _scheduler;
        private List<StockInstance> _singleStock;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            _effects = new EventEffects();
            _singleStock = new List<StockInstance>();
            var stock = new StockInstance();
            stock.Initialize(0, "SOLO", StockTier.MidValue, 100f, TrendDirection.Neutral, 0f);
            _singleStock.Add(stock);
            _effects.SetActiveStocks(_singleStock);
            _scheduler = new EventScheduler(_effects, new System.Random(42));
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
        }

        // --- AC1: ALL events target activeStocks[0] ---

        [Test]
        public void AllEventTypes_TargetFirstActiveStock()
        {
            var eventConfigs = new[]
            {
                EventDefinitions.EarningsBeat,
                EventDefinitions.EarningsMiss,
                EventDefinitions.MarketCrash,
                EventDefinitions.BullRunEvent,
                EventDefinitions.MergerRumor,
                EventDefinitions.ShortSqueeze,
                EventDefinitions.FlashCrash,
                EventDefinitions.SECInvestigation
            };

            foreach (var config in eventConfigs)
            {
                EventBus.Clear();
                var effects = new EventEffects();
                effects.SetActiveStocks(_singleStock);
                var scheduler = new EventScheduler(effects, new System.Random(42));

                MarketEventFiredEvent received = default;
                EventBus.Subscribe<MarketEventFiredEvent>(e => received = e);

                scheduler.FireEvent(config, _singleStock);

                Assert.IsNotNull(received.AffectedStockIds,
                    $"{config.EventType} should have non-null AffectedStockIds");
                Assert.AreEqual(1, received.AffectedStockIds.Length,
                    $"{config.EventType} should affect exactly 1 stock");
                Assert.AreEqual(0, received.AffectedStockIds[0],
                    $"{config.EventType} should target stock 0 (activeStocks[0])");
            }
        }

        [Test]
        public void NoEventHasNullTargetStockId()
        {
            // Fire all event types and verify none have null TargetStockId
            var eventConfigs = new[]
            {
                EventDefinitions.EarningsBeat,
                EventDefinitions.EarningsMiss,
                EventDefinitions.MarketCrash,
                EventDefinitions.BullRunEvent,
                EventDefinitions.MergerRumor,
                EventDefinitions.ShortSqueeze,
                EventDefinitions.FlashCrash,
                EventDefinitions.SECInvestigation,
                EventDefinitions.SectorRotation
            };

            foreach (var config in eventConfigs)
            {
                var effects = new EventEffects();
                effects.SetActiveStocks(_singleStock);
                var scheduler = new EventScheduler(effects, new System.Random(42));

                scheduler.FireEvent(config, _singleStock);

                var events = effects.GetActiveEventsForStock(0);
                for (int i = 0; i < events.Count; i++)
                {
                    Assert.IsTrue(events[i].TargetStockId.HasValue,
                        $"{config.EventType} event should have non-null TargetStockId");
                    Assert.IsFalse(events[i].IsGlobalEvent,
                        $"{config.EventType} event should not be global");
                }
            }
        }

        // --- AC2: MarketCrash/BullRun affect single stock directly ---

        [Test]
        public void MarketCrash_TargetsSingleStock_NotGlobal()
        {
            MarketEventFiredEvent received = default;
            EventBus.Subscribe<MarketEventFiredEvent>(e => received = e);

            _scheduler.FireEvent(EventDefinitions.MarketCrash, _singleStock);

            Assert.AreEqual(1, _effects.ActiveEventCount, "Should create exactly 1 event");

            var events = _effects.GetActiveEventsForStock(0);
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(0, events[0].TargetStockId);
            Assert.IsFalse(events[0].IsGlobalEvent);
        }

        [Test]
        public void BullRun_TargetsSingleStock_NotGlobal()
        {
            MarketEventFiredEvent received = default;
            EventBus.Subscribe<MarketEventFiredEvent>(e => received = e);

            _scheduler.FireEvent(EventDefinitions.BullRunEvent, _singleStock);

            Assert.AreEqual(1, _effects.ActiveEventCount, "Should create exactly 1 event");

            var events = _effects.GetActiveEventsForStock(0);
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(0, events[0].TargetStockId);
            Assert.IsFalse(events[0].IsGlobalEvent);
        }

        // --- AC3: SectorRotation applies single directional effect ---

        [Test]
        public void SectorRotation_SingleStock_CreatesOneEvent()
        {
            _scheduler.FireEvent(EventDefinitions.SectorRotation, _singleStock);

            Assert.AreEqual(1, _effects.ActiveEventCount,
                "SectorRotation should create exactly 1 event for single stock");
        }

        [Test]
        public void SectorRotation_SingleStock_NonZeroEffect()
        {
            _scheduler.FireEvent(EventDefinitions.SectorRotation, _singleStock);

            var events = _effects.GetActiveEventsForStock(0);
            Assert.AreEqual(1, events.Count);
            Assert.AreNotEqual(0f, events[0].PriceEffectPercent,
                "SectorRotation should have a non-zero price effect");
        }

        [Test]
        public void SectorRotation_EmptyStocks_DoesNotCrash()
        {
            var emptyStocks = new List<StockInstance>();
            _scheduler.FireEvent(EventDefinitions.SectorRotation, emptyStocks);

            Assert.AreEqual(0, _effects.ActiveEventCount, "Should not create events with empty stock list");
        }

        // --- AC4: ShortSqueeze targets single active stock ---

        [Test]
        public void ShortSqueeze_AlwaysTargetsActiveStock()
        {
            MarketEventFiredEvent received = default;
            EventBus.Subscribe<MarketEventFiredEvent>(e => received = e);

            _scheduler.FireEvent(EventDefinitions.ShortSqueeze, _singleStock);

            Assert.AreEqual(0, received.AffectedStockIds[0],
                "ShortSqueeze should target activeStocks[0]");
        }

        // --- AC5: Headlines reference the correct ticker ---

        [Test]
        public void AllEvents_HeadlineContainsStockTicker()
        {
            _effects.SetHeadlineRandom(new System.Random(42));

            var eventConfigs = new[]
            {
                EventDefinitions.EarningsBeat,
                EventDefinitions.EarningsMiss,
                EventDefinitions.MergerRumor
            };

            foreach (var config in eventConfigs)
            {
                EventBus.Clear();
                var effects = new EventEffects();
                effects.SetActiveStocks(_singleStock);
                effects.SetHeadlineRandom(new System.Random(42));
                var scheduler = new EventScheduler(effects, new System.Random(42));

                MarketEventFiredEvent received = default;
                EventBus.Subscribe<MarketEventFiredEvent>(e => received = e);

                scheduler.FireEvent(config, _singleStock);

                Assert.IsNotNull(received.Headline,
                    $"{config.EventType} should have a headline");
                Assert.IsNotNull(received.AffectedTickerSymbols,
                    $"{config.EventType} should have ticker symbols");
                Assert.AreEqual("SOLO", received.AffectedTickerSymbols[0],
                    $"{config.EventType} should reference the active stock ticker");
            }
        }

        // --- Edge cases ---

        [Test]
        public void FireEvent_EmptyStockList_DoesNotCrash()
        {
            var emptyStocks = new List<StockInstance>();
            _scheduler.FireEvent(EventDefinitions.EarningsBeat, emptyStocks);

            Assert.AreEqual(0, _effects.ActiveEventCount,
                "Should not create events with empty stock list");
        }

        [Test]
        public void GetActiveEventsForStock_NoGlobalMatching()
        {
            // GetActiveEventsForStock should only match by TargetStockId
            var targeted = new MarketEvent(MarketEventType.EarningsBeat, 0, 0.25f, 5f);
            var otherStock = new MarketEvent(MarketEventType.MergerRumor, 1, 0.20f, 6f);

            _effects.StartEvent(targeted);
            _effects.StartEvent(otherStock);

            var eventsForStock0 = _effects.GetActiveEventsForStock(0);
            Assert.AreEqual(1, eventsForStock0.Count,
                "Should only return events targeting stock 0");
        }
    }
}
