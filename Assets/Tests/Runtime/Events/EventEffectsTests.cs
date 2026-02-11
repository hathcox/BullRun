using NUnit.Framework;

namespace BullRun.Tests.Events
{
    [TestFixture]
    public class EventEffectsTests
    {
        private EventEffects _effects;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            _effects = new EventEffects();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
        }

        // --- StartEvent tests ---

        [Test]
        public void StartEvent_AddsEventToActiveList()
        {
            var evt = new MarketEvent(MarketEventType.EarningsBeat, 0, 0.25f, 5f);
            _effects.StartEvent(evt);

            Assert.AreEqual(1, _effects.ActiveEventCount);
        }

        [Test]
        public void StartEvent_PublishesMarketEventFiredEvent()
        {
            MarketEventFiredEvent received = default;
            bool fired = false;
            EventBus.Subscribe<MarketEventFiredEvent>(e =>
            {
                received = e;
                fired = true;
            });

            var evt = new MarketEvent(MarketEventType.EarningsBeat, 3, 0.25f, 5f);
            _effects.StartEvent(evt);

            Assert.IsTrue(fired, "MarketEventFiredEvent should be published");
            Assert.AreEqual(MarketEventType.EarningsBeat, received.EventType);
            Assert.AreEqual(0.25f, received.PriceEffectPercent, 0.001f);
        }

        [Test]
        public void StartEvent_SingleStock_SetsAffectedStockIds()
        {
            MarketEventFiredEvent received = default;
            EventBus.Subscribe<MarketEventFiredEvent>(e => received = e);

            var evt = new MarketEvent(MarketEventType.EarningsBeat, 3, 0.25f, 5f);
            _effects.StartEvent(evt);

            Assert.IsNotNull(received.AffectedStockIds);
            Assert.AreEqual(1, received.AffectedStockIds.Length);
            Assert.AreEqual(3, received.AffectedStockIds[0]);
        }

        [Test]
        public void StartEvent_GlobalEvent_SetsNullAffectedStockIds()
        {
            MarketEventFiredEvent received = default;
            EventBus.Subscribe<MarketEventFiredEvent>(e => received = e);

            var evt = new MarketEvent(MarketEventType.MarketCrash, null, -0.30f, 8f);
            _effects.StartEvent(evt);

            Assert.IsNull(received.AffectedStockIds, "Global events should have null AffectedStockIds");
        }

        // --- ApplyEventEffect tests ---

        [Test]
        public void ApplyEventEffect_PositiveEvent_IncreasesPrice()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.MidValue, 100f, TrendDirection.Neutral, 0f);

            // +25% event at peak force (mid-duration)
            var evt = new MarketEvent(MarketEventType.EarningsBeat, 0, 0.25f, 4f);
            evt.ElapsedTime = 2f; // Peak force = 1.0

            float result = _effects.ApplyEventEffect(stock, evt, 0.016f);

            // Event target = 100 * (1 + 0.25) = 125
            // Lerp(100, 125, 1.0 * deltaTime) should move price toward 125
            Assert.Greater(result, 100f, "Positive event should increase price");
        }

        [Test]
        public void ApplyEventEffect_NegativeEvent_DecreasesPrice()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.MidValue, 100f, TrendDirection.Neutral, 0f);

            // -25% event at peak force
            var evt = new MarketEvent(MarketEventType.EarningsMiss, 0, -0.25f, 4f);
            evt.ElapsedTime = 2f;

            float result = _effects.ApplyEventEffect(stock, evt, 0.016f);

            Assert.Less(result, 100f, "Negative event should decrease price");
        }

        [Test]
        public void ApplyEventEffect_ZeroForce_ReturnsOriginalPrice()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.MidValue, 100f, TrendDirection.Neutral, 0f);

            // Event at start (force = 0)
            var evt = new MarketEvent(MarketEventType.EarningsBeat, 0, 0.25f, 4f);
            evt.ElapsedTime = 0f;

            float result = _effects.ApplyEventEffect(stock, evt, 0.016f);

            Assert.AreEqual(100f, result, 0.01f, "Zero force should not change price");
        }

        [Test]
        public void ApplyEventEffect_ExpiredEvent_ReturnsOriginalPrice()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.MidValue, 100f, TrendDirection.Neutral, 0f);

            var evt = new MarketEvent(MarketEventType.EarningsBeat, 0, 0.25f, 4f);
            evt.ElapsedTime = 5f; // Past duration

            float result = _effects.ApplyEventEffect(stock, evt, 0.016f);

            Assert.AreEqual(100f, result, 0.01f, "Expired event should not change price");
        }

        [Test]
        public void ApplyEventEffect_StrongerForce_ProducesLargerEffect()
        {
            var stock1 = new StockInstance();
            stock1.Initialize(0, "TEST", StockTier.MidValue, 100f, TrendDirection.Neutral, 0f);

            var stock2 = new StockInstance();
            stock2.Initialize(1, "TEST2", StockTier.MidValue, 100f, TrendDirection.Neutral, 0f);

            // Low force (quarter way through)
            var evt1 = new MarketEvent(MarketEventType.EarningsBeat, 0, 0.25f, 8f);
            evt1.ElapsedTime = 1f; // force ~0.25

            // High force (at peak)
            var evt2 = new MarketEvent(MarketEventType.EarningsBeat, 1, 0.25f, 8f);
            evt2.ElapsedTime = 4f; // force = 1.0

            float result1 = _effects.ApplyEventEffect(stock1, evt1, 0.016f);
            float result2 = _effects.ApplyEventEffect(stock2, evt2, 0.016f);

            Assert.Greater(result2 - 100f, result1 - 100f,
                "Stronger force should produce larger price movement");
        }

        // --- UpdateActiveEvents tests ---

        [Test]
        public void UpdateActiveEvents_AdvancesElapsedTime()
        {
            var evt = new MarketEvent(MarketEventType.EarningsBeat, 0, 0.25f, 5f);
            _effects.StartEvent(evt);

            _effects.UpdateActiveEvents(0.5f);

            Assert.AreEqual(0.5f, evt.ElapsedTime, 0.001f);
        }

        [Test]
        public void UpdateActiveEvents_RemovesExpiredEvents()
        {
            var evt = new MarketEvent(MarketEventType.EarningsBeat, 0, 0.25f, 1f);
            _effects.StartEvent(evt);

            // Advance past duration
            _effects.UpdateActiveEvents(1.5f);

            Assert.AreEqual(0, _effects.ActiveEventCount, "Expired event should be removed");
        }

        [Test]
        public void UpdateActiveEvents_PublishesEndedEventForExpired()
        {
            MarketEventEndedEvent received = default;
            bool fired = false;
            EventBus.Subscribe<MarketEventEndedEvent>(e =>
            {
                received = e;
                fired = true;
            });

            var evt = new MarketEvent(MarketEventType.EarningsBeat, 3, 0.25f, 1f);
            _effects.StartEvent(evt);

            _effects.UpdateActiveEvents(1.5f);

            Assert.IsTrue(fired, "MarketEventEndedEvent should be published when event expires");
            Assert.AreEqual(MarketEventType.EarningsBeat, received.EventType);
        }

        [Test]
        public void UpdateActiveEvents_KeepsActiveEvents()
        {
            var evt = new MarketEvent(MarketEventType.EarningsBeat, 0, 0.25f, 5f);
            _effects.StartEvent(evt);

            _effects.UpdateActiveEvents(0.5f);

            Assert.AreEqual(1, _effects.ActiveEventCount, "Active event should remain");
        }

        // --- Multiple events ---

        [Test]
        public void MultipleEvents_CanBeActiveSimultaneously()
        {
            var evt1 = new MarketEvent(MarketEventType.EarningsBeat, 0, 0.25f, 5f);
            var evt2 = new MarketEvent(MarketEventType.MarketCrash, null, -0.30f, 8f);
            _effects.StartEvent(evt1);
            _effects.StartEvent(evt2);

            Assert.AreEqual(2, _effects.ActiveEventCount);
        }

        [Test]
        public void GetActiveEventsForStock_ReturnsTargetedAndGlobalEvents()
        {
            var targeted = new MarketEvent(MarketEventType.EarningsBeat, 0, 0.25f, 5f);
            var global = new MarketEvent(MarketEventType.MarketCrash, null, -0.30f, 8f);
            var otherStock = new MarketEvent(MarketEventType.MergerRumor, 1, 0.20f, 6f);

            _effects.StartEvent(targeted);
            _effects.StartEvent(global);
            _effects.StartEvent(otherStock);

            var eventsForStock0 = _effects.GetActiveEventsForStock(0);

            Assert.AreEqual(2, eventsForStock0.Count,
                "Should include targeted event and global event, but not other stock's event");
        }
    }
}
