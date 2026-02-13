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

        // --- MarketClosedEvent Tests (Story 4.3 Task 4) ---

        [Test]
        public void MarketClosedEvent_StoresAllFields()
        {
            var evt = new MarketClosedEvent
            {
                RoundNumber = 3,
                RoundProfit = 650.50f,
                FinalCash = 1650.50f,
                PositionsLiquidated = 4
            };

            Assert.AreEqual(3, evt.RoundNumber);
            Assert.AreEqual(650.50f, evt.RoundProfit, 0.01f);
            Assert.AreEqual(1650.50f, evt.FinalCash, 0.01f);
            Assert.AreEqual(4, evt.PositionsLiquidated);
        }

        [Test]
        public void MarketClosedEvent_CanBePublishedViaEventBus()
        {
            EventBus.Clear();
            MarketClosedEvent received = default;
            bool wasCalled = false;

            EventBus.Subscribe<MarketClosedEvent>(e =>
            {
                received = e;
                wasCalled = true;
            });

            EventBus.Publish(new MarketClosedEvent
            {
                RoundNumber = 1,
                RoundProfit = -120f,
                FinalCash = 880f,
                PositionsLiquidated = 2
            });

            Assert.IsTrue(wasCalled);
            Assert.AreEqual(1, received.RoundNumber);
            Assert.AreEqual(-120f, received.RoundProfit, 0.01f);
            Assert.AreEqual(880f, received.FinalCash, 0.01f);
            Assert.AreEqual(2, received.PositionsLiquidated);

            EventBus.Clear();
        }

        // --- MarginCallTriggeredEvent Tests (Story 4.4 Task 4) ---

        [Test]
        public void MarginCallTriggeredEvent_StoresAllFields()
        {
            var evt = new MarginCallTriggeredEvent
            {
                RoundNumber = 3,
                RoundProfit = 150f,
                RequiredTarget = 600f,
                Shortfall = 450f
            };

            Assert.AreEqual(3, evt.RoundNumber);
            Assert.AreEqual(150f, evt.RoundProfit, 0.01f);
            Assert.AreEqual(600f, evt.RequiredTarget, 0.01f);
            Assert.AreEqual(450f, evt.Shortfall, 0.01f);
        }

        [Test]
        public void MarginCallTriggeredEvent_CanBePublishedViaEventBus()
        {
            EventBus.Clear();
            MarginCallTriggeredEvent received = default;
            bool wasCalled = false;

            EventBus.Subscribe<MarginCallTriggeredEvent>(e =>
            {
                received = e;
                wasCalled = true;
            });

            EventBus.Publish(new MarginCallTriggeredEvent
            {
                RoundNumber = 2,
                RoundProfit = 100f,
                RequiredTarget = 350f,
                Shortfall = 250f
            });

            Assert.IsTrue(wasCalled);
            Assert.AreEqual(2, received.RoundNumber);
            Assert.AreEqual(100f, received.RoundProfit, 0.01f);
            Assert.AreEqual(350f, received.RequiredTarget, 0.01f);
            Assert.AreEqual(250f, received.Shortfall, 0.01f);

            EventBus.Clear();
        }

        // --- RoundCompletedEvent Tests (Story 4.5 Task 1) ---

        [Test]
        public void RoundCompletedEvent_StoresAllFields()
        {
            var evt = new RoundCompletedEvent
            {
                RoundNumber = 3,
                RoundProfit = 650f,
                ProfitTarget = 600f,
                TargetMet = true,
                TotalCash = 2800f
            };

            Assert.AreEqual(3, evt.RoundNumber);
            Assert.AreEqual(650f, evt.RoundProfit, 0.01f);
            Assert.AreEqual(600f, evt.ProfitTarget, 0.01f);
            Assert.IsTrue(evt.TargetMet);
            Assert.AreEqual(2800f, evt.TotalCash, 0.01f);
        }

        [Test]
        public void RoundCompletedEvent_CanBePublishedViaEventBus()
        {
            EventBus.Clear();
            RoundCompletedEvent received = default;
            bool wasCalled = false;

            EventBus.Subscribe<RoundCompletedEvent>(e =>
            {
                received = e;
                wasCalled = true;
            });

            EventBus.Publish(new RoundCompletedEvent
            {
                RoundNumber = 2,
                RoundProfit = 400f,
                ProfitTarget = 350f,
                TargetMet = true,
                TotalCash = 1400f
            });

            Assert.IsTrue(wasCalled);
            Assert.AreEqual(2, received.RoundNumber);
            Assert.AreEqual(400f, received.RoundProfit, 0.01f);
            Assert.IsTrue(received.TargetMet);

            EventBus.Clear();
        }

        // --- ActTransitionEvent Tests (Story 4.5 Task 4) ---

        [Test]
        public void ActTransitionEvent_StoresAllFields()
        {
            var evt = new ActTransitionEvent
            {
                NewAct = 2,
                PreviousAct = 1,
                TierDisplayName = "Low-Value Stocks"
            };

            Assert.AreEqual(2, evt.NewAct);
            Assert.AreEqual(1, evt.PreviousAct);
            Assert.AreEqual("Low-Value Stocks", evt.TierDisplayName);
        }

        [Test]
        public void ActTransitionEvent_CanBePublishedViaEventBus()
        {
            EventBus.Clear();
            ActTransitionEvent received = default;
            bool wasCalled = false;

            EventBus.Subscribe<ActTransitionEvent>(e =>
            {
                received = e;
                wasCalled = true;
            });

            EventBus.Publish(new ActTransitionEvent
            {
                NewAct = 3,
                PreviousAct = 2,
                TierDisplayName = "Mid-Value Stocks"
            });

            Assert.IsTrue(wasCalled);
            Assert.AreEqual(3, received.NewAct);
            Assert.AreEqual("Mid-Value Stocks", received.TierDisplayName);

            EventBus.Clear();
        }

        // --- RunCompletedEvent Tests (Story 6.5 Task 6) ---

        [Test]
        public void RunCompletedEvent_StoresAllFields()
        {
            var evt = new RunCompletedEvent
            {
                TotalProfit = 4000f,
                PeakCash = 6000f,
                RoundsCompleted = 8,
                ItemsCollected = 3,
                ReputationEarned = 140,
                IsVictory = true
            };

            Assert.AreEqual(4000f, evt.TotalProfit, 0.01f);
            Assert.AreEqual(6000f, evt.PeakCash, 0.01f);
            Assert.AreEqual(8, evt.RoundsCompleted);
            Assert.AreEqual(3, evt.ItemsCollected);
            Assert.AreEqual(140, evt.ReputationEarned);
            Assert.IsTrue(evt.IsVictory);
        }

        [Test]
        public void RunCompletedEvent_CanBePublishedViaEventBus()
        {
            EventBus.Clear();
            RunCompletedEvent received = default;
            bool wasCalled = false;

            EventBus.Subscribe<RunCompletedEvent>(e =>
            {
                received = e;
                wasCalled = true;
            });

            EventBus.Publish(new RunCompletedEvent
            {
                TotalProfit = 4000f,
                PeakCash = 6000f,
                RoundsCompleted = 8,
                ItemsCollected = 3,
                ReputationEarned = 140,
                IsVictory = true
            });

            Assert.IsTrue(wasCalled);
            Assert.AreEqual(4000f, received.TotalProfit, 0.01f);
            Assert.IsTrue(received.IsVictory);

            EventBus.Clear();
        }

        // --- RunEndedEvent Tests (Story 4.4 Task 4) ---

        [Test]
        public void RunEndedEvent_StoresAllFields()
        {
            var evt = new RunEndedEvent
            {
                RoundsCompleted = 5,
                FinalCash = 2500f,
                TotalProfit = 1500f,
                WasMarginCalled = true,
                ReputationEarned = 0,
                ItemsCollected = 3
            };

            Assert.AreEqual(5, evt.RoundsCompleted);
            Assert.AreEqual(2500f, evt.FinalCash, 0.01f);
            Assert.AreEqual(1500f, evt.TotalProfit, 0.01f);
            Assert.IsTrue(evt.WasMarginCalled);
            Assert.AreEqual(0, evt.ReputationEarned);
            Assert.AreEqual(3, evt.ItemsCollected);
        }

        [Test]
        public void RunEndedEvent_CanBePublishedViaEventBus()
        {
            EventBus.Clear();
            RunEndedEvent received = default;
            bool wasCalled = false;

            EventBus.Subscribe<RunEndedEvent>(e =>
            {
                received = e;
                wasCalled = true;
            });

            EventBus.Publish(new RunEndedEvent
            {
                RoundsCompleted = 3,
                FinalCash = 800f,
                TotalProfit = -200f,
                WasMarginCalled = true,
                ReputationEarned = 0
            });

            Assert.IsTrue(wasCalled);
            Assert.AreEqual(3, received.RoundsCompleted);
            Assert.AreEqual(-200f, received.TotalProfit, 0.01f);
            Assert.IsTrue(received.WasMarginCalled);

            EventBus.Clear();
        }
    }
}
