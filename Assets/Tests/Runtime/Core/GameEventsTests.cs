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
        // --- Capital Events Tests (Story 2.5 Task 5) ---

        [Test]
        public void RoundEndedEvent_StoresAllFields()
        {
            var evt = new RoundEndedEvent
            {
                RoundNumber = 3,
                TotalProfit = 150.50f,
                FinalCash = 1150.50f
            };

            Assert.AreEqual(3, evt.RoundNumber);
            Assert.AreEqual(150.50f, evt.TotalProfit, 0.01f);
            Assert.AreEqual(1150.50f, evt.FinalCash, 0.01f);
        }

        [Test]
        public void RoundEndedEvent_CanBePublishedViaEventBus()
        {
            EventBus.Clear();
            RoundEndedEvent received = default;
            bool wasCalled = false;

            EventBus.Subscribe<RoundEndedEvent>(e =>
            {
                received = e;
                wasCalled = true;
            });

            EventBus.Publish(new RoundEndedEvent
            {
                RoundNumber = 1,
                TotalProfit = 50f,
                FinalCash = 1050f
            });

            Assert.IsTrue(wasCalled);
            Assert.AreEqual(1, received.RoundNumber);
            Assert.AreEqual(50f, received.TotalProfit, 0.01f);

            EventBus.Clear();
        }

        [Test]
        public void RunStartedEvent_StoresAllFields()
        {
            var evt = new RunStartedEvent
            {
                StartingCapital = 1000f
            };

            Assert.AreEqual(1000f, evt.StartingCapital, 0.01f);
        }

        [Test]
        public void RunStartedEvent_CanBePublishedViaEventBus()
        {
            EventBus.Clear();
            RunStartedEvent received = default;
            bool wasCalled = false;

            EventBus.Subscribe<RunStartedEvent>(e =>
            {
                received = e;
                wasCalled = true;
            });

            EventBus.Publish(new RunStartedEvent
            {
                StartingCapital = 1000f
            });

            Assert.IsTrue(wasCalled);
            Assert.AreEqual(1000f, received.StartingCapital, 0.01f);

            EventBus.Clear();
        }

        // --- Round State Events Tests (Story 4.1 Task 5) ---

        [Test]
        public void RoundStartedEvent_StoresAllFields()
        {
            var evt = new RoundStartedEvent
            {
                RoundNumber = 2,
                Act = 1,
                MarginCallTarget = 150f,
                TimeLimit = 60f
            };

            Assert.AreEqual(2, evt.RoundNumber);
            Assert.AreEqual(1, evt.Act);
            Assert.AreEqual(150f, evt.MarginCallTarget, 0.01f);
            Assert.AreEqual(60f, evt.TimeLimit, 0.01f);
        }

        [Test]
        public void RoundStartedEvent_CanBePublishedViaEventBus()
        {
            EventBus.Clear();
            RoundStartedEvent received = default;
            bool wasCalled = false;

            EventBus.Subscribe<RoundStartedEvent>(e =>
            {
                received = e;
                wasCalled = true;
            });

            EventBus.Publish(new RoundStartedEvent
            {
                RoundNumber = 1,
                Act = 1,
                TimeLimit = 60f
            });

            Assert.IsTrue(wasCalled);
            Assert.AreEqual(1, received.RoundNumber);
            Assert.AreEqual(60f, received.TimeLimit, 0.01f);

            EventBus.Clear();
        }

        [Test]
        public void TradingPhaseEndedEvent_StoresAllFields()
        {
            var evt = new TradingPhaseEndedEvent
            {
                RoundNumber = 3,
                TimeExpired = true
            };

            Assert.AreEqual(3, evt.RoundNumber);
            Assert.IsTrue(evt.TimeExpired);
        }

        [Test]
        public void TradingPhaseEndedEvent_CanBePublishedViaEventBus()
        {
            EventBus.Clear();
            TradingPhaseEndedEvent received = default;
            bool wasCalled = false;

            EventBus.Subscribe<TradingPhaseEndedEvent>(e =>
            {
                received = e;
                wasCalled = true;
            });

            EventBus.Publish(new TradingPhaseEndedEvent
            {
                RoundNumber = 1,
                TimeExpired = true
            });

            Assert.IsTrue(wasCalled);
            Assert.AreEqual(1, received.RoundNumber);
            Assert.IsTrue(received.TimeExpired);

            EventBus.Clear();
        }

        // --- MarketOpenEvent Tests (Story 4.2 Task 5) ---

        [Test]
        public void MarketOpenEvent_StoresAllFields()
        {
            var evt = new MarketOpenEvent
            {
                RoundNumber = 2,
                Act = 1,
                StockIds = new[] { 0, 1, 2 },
                TickerSymbols = new[] { "ACME", "MOON", "STAR" },
                StartingPrices = new[] { 100f, 2.50f, 75f },
                TierNames = new[] { "MidValue", "Penny", "LowValue" },
                ProfitTarget = 350f,
                Headline = "Markets rally on optimism"
            };

            Assert.AreEqual(2, evt.RoundNumber);
            Assert.AreEqual(1, evt.Act);
            Assert.AreEqual(3, evt.StockIds.Length);
            Assert.AreEqual("ACME", evt.TickerSymbols[0]);
            Assert.AreEqual(100f, evt.StartingPrices[0], 0.01f);
            Assert.AreEqual("MidValue", evt.TierNames[0]);
            Assert.AreEqual(350f, evt.ProfitTarget, 0.01f);
            Assert.AreEqual("Markets rally on optimism", evt.Headline);
        }

        [Test]
        public void MarketOpenEvent_CanBePublishedViaEventBus()
        {
            EventBus.Clear();
            MarketOpenEvent received = default;
            bool wasCalled = false;

            EventBus.Subscribe<MarketOpenEvent>(e =>
            {
                received = e;
                wasCalled = true;
            });

            EventBus.Publish(new MarketOpenEvent
            {
                RoundNumber = 1,
                Act = 1,
                StockIds = new[] { 0, 1 },
                ProfitTarget = 200f,
                Headline = "Test headline"
            });

            Assert.IsTrue(wasCalled);
            Assert.AreEqual(1, received.RoundNumber);
            Assert.AreEqual(200f, received.ProfitTarget, 0.01f);
            Assert.AreEqual("Test headline", received.Headline);

            EventBus.Clear();
        }
    }
}
