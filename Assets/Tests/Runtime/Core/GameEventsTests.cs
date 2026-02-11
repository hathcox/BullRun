using NUnit.Framework;

namespace BullRun.Tests.Core
{
    [TestFixture]
    public class GameEventsTests
    {
        [Test]
        public void PriceUpdatedEvent_StoresAllFields()
        {
            var evt = new PriceUpdatedEvent
            {
                StockId = 3,
                NewPrice = 105.50f,
                PreviousPrice = 100.00f,
                DeltaTime = 0.016f
            };

            Assert.AreEqual(3, evt.StockId);
            Assert.AreEqual(105.50f, evt.NewPrice, 0.01f);
            Assert.AreEqual(100.00f, evt.PreviousPrice, 0.01f);
            Assert.AreEqual(0.016f, evt.DeltaTime, 0.001f);
        }

        [Test]
        public void PriceUpdatedEvent_CanBePublishedViaEventBus()
        {
            EventBus.Clear();
            PriceUpdatedEvent received = default;
            bool wasCalled = false;

            EventBus.Subscribe<PriceUpdatedEvent>(e =>
            {
                received = e;
                wasCalled = true;
            });

            EventBus.Publish(new PriceUpdatedEvent
            {
                StockId = 1,
                NewPrice = 50f,
                PreviousPrice = 48f,
                DeltaTime = 0.016f
            });

            Assert.IsTrue(wasCalled);
            Assert.AreEqual(1, received.StockId);
            Assert.AreEqual(50f, received.NewPrice, 0.01f);

            EventBus.Clear();
        }

        // --- MarketEventFiredEvent tests (Story 1.3) ---

        [Test]
        public void MarketEventFiredEvent_StoresAllFields()
        {
            var evt = new MarketEventFiredEvent
            {
                EventType = MarketEventType.EarningsBeat,
                AffectedStockIds = new[] { 1, 3 },
                PriceEffectPercent = 0.25f
            };

            Assert.AreEqual(MarketEventType.EarningsBeat, evt.EventType);
            Assert.AreEqual(2, evt.AffectedStockIds.Length);
            Assert.AreEqual(1, evt.AffectedStockIds[0]);
            Assert.AreEqual(3, evt.AffectedStockIds[1]);
            Assert.AreEqual(0.25f, evt.PriceEffectPercent, 0.001f);
        }

        [Test]
        public void MarketEventFiredEvent_CanBePublishedViaEventBus()
        {
            EventBus.Clear();
            MarketEventFiredEvent received = default;
            bool wasCalled = false;

            EventBus.Subscribe<MarketEventFiredEvent>(e =>
            {
                received = e;
                wasCalled = true;
            });

            EventBus.Publish(new MarketEventFiredEvent
            {
                EventType = MarketEventType.MarketCrash,
                AffectedStockIds = null,
                PriceEffectPercent = -0.30f
            });

            Assert.IsTrue(wasCalled, "MarketEventFiredEvent should be publishable via EventBus");
            Assert.AreEqual(MarketEventType.MarketCrash, received.EventType);
            Assert.IsNull(received.AffectedStockIds, "Global events should have null AffectedStockIds");

            EventBus.Clear();
        }

        // --- MarketEventEndedEvent tests (Story 1.3) ---

        [Test]
        public void MarketEventEndedEvent_StoresAllFields()
        {
            var evt = new MarketEventEndedEvent
            {
                EventType = MarketEventType.ShortSqueeze,
                AffectedStockIds = new[] { 5 }
            };

            Assert.AreEqual(MarketEventType.ShortSqueeze, evt.EventType);
            Assert.AreEqual(1, evt.AffectedStockIds.Length);
            Assert.AreEqual(5, evt.AffectedStockIds[0]);
        }

        [Test]
        public void MarketEventEndedEvent_CanBePublishedViaEventBus()
        {
            EventBus.Clear();
            MarketEventEndedEvent received = default;
            bool wasCalled = false;

            EventBus.Subscribe<MarketEventEndedEvent>(e =>
            {
                received = e;
                wasCalled = true;
            });

            EventBus.Publish(new MarketEventEndedEvent
            {
                EventType = MarketEventType.BullRun,
                AffectedStockIds = null
            });

            Assert.IsTrue(wasCalled, "MarketEventEndedEvent should be publishable via EventBus");
            Assert.AreEqual(MarketEventType.BullRun, received.EventType);

            EventBus.Clear();
        }
    }
}
