using System.Collections.Generic;
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

        // --- Headline and display data tests (Story 5-2) ---

        [Test]
        public void StartEvent_PublishesHeadlineWithTicker()
        {
            var stocks = new List<StockInstance>();
            var stock = new StockInstance();
            stock.Initialize(3, "ACME", StockTier.MidValue, 100f, TrendDirection.Neutral, 0f);
            stocks.Add(stock);

            _effects.SetActiveStocks(stocks);
            _effects.SetHeadlineRandom(new System.Random(42));

            MarketEventFiredEvent received = default;
            EventBus.Subscribe<MarketEventFiredEvent>(e => received = e);

            var evt = new MarketEvent(MarketEventType.EarningsBeat, 3, 0.25f, 5f);
            _effects.StartEvent(evt);

            Assert.IsNotNull(received.Headline);
            Assert.IsTrue(received.Headline.Contains("ACME"),
                $"Headline should contain ticker ACME, got: {received.Headline}");
        }

        [Test]
        public void StartEvent_EarningsBeat_SetsIsPositiveTrue()
        {
            MarketEventFiredEvent received = default;
            EventBus.Subscribe<MarketEventFiredEvent>(e => received = e);

            var evt = new MarketEvent(MarketEventType.EarningsBeat, 0, 0.25f, 5f);
            _effects.StartEvent(evt);

            Assert.IsTrue(received.IsPositive, "EarningsBeat should be positive");
        }

        [Test]
        public void StartEvent_EarningsMiss_SetsIsPositiveFalse()
        {
            MarketEventFiredEvent received = default;
            EventBus.Subscribe<MarketEventFiredEvent>(e => received = e);

            var evt = new MarketEvent(MarketEventType.EarningsMiss, 0, -0.25f, 5f);
            _effects.StartEvent(evt);

            Assert.IsFalse(received.IsPositive, "EarningsMiss should not be positive");
        }

        [Test]
        public void StartEvent_SetsDuration()
        {
            MarketEventFiredEvent received = default;
            EventBus.Subscribe<MarketEventFiredEvent>(e => received = e);

            var evt = new MarketEvent(MarketEventType.EarningsBeat, 0, 0.25f, 5f);
            _effects.StartEvent(evt);

            Assert.AreEqual(5f, received.Duration, 0.001f);
        }

        [Test]
        public void StartEvent_SetsAffectedTickerSymbols()
        {
            var stocks = new List<StockInstance>();
            var stock = new StockInstance();
            stock.Initialize(0, "ZETA", StockTier.MidValue, 100f, TrendDirection.Neutral, 0f);
            stocks.Add(stock);

            _effects.SetActiveStocks(stocks);

            MarketEventFiredEvent received = default;
            EventBus.Subscribe<MarketEventFiredEvent>(e => received = e);

            var evt = new MarketEvent(MarketEventType.EarningsBeat, 0, 0.25f, 5f);
            _effects.StartEvent(evt);

            Assert.IsNotNull(received.AffectedTickerSymbols);
            Assert.AreEqual(1, received.AffectedTickerSymbols.Length);
            Assert.AreEqual("ZETA", received.AffectedTickerSymbols[0]);
        }

        [Test]
        public void StartEvent_GlobalEvent_NullTickerSymbols()
        {
            MarketEventFiredEvent received = default;
            EventBus.Subscribe<MarketEventFiredEvent>(e => received = e);

            var evt = new MarketEvent(MarketEventType.MarketCrash, null, -0.30f, 8f);
            _effects.StartEvent(evt);

            Assert.IsNull(received.AffectedTickerSymbols,
                "Global events should have null AffectedTickerSymbols");
        }

        // --- Multi-phase EventEffects tests (Story 5-3, Task 2) ---

        [Test]
        public void ApplyEventEffect_MultiPhase_UsesPhaseTarget()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.Penny, 100f, TrendDirection.Neutral, 0f);

            var phases = new List<MarketEventPhase>
            {
                new MarketEventPhase(0.80f, 5f),
                new MarketEventPhase(-1.20f, 3f)
            };
            var evt = new MarketEvent(MarketEventType.PumpAndDump, 0, 0.80f, 8f, phases);
            evt.ElapsedTime = 2.5f; // Mid-phase 0, force ~1.0

            float result = _effects.ApplyEventEffect(stock, evt, 0.016f);

            // Phase 0 target: 100 * (1 + 0.80) = 180
            Assert.Greater(result, 100f, "Phase 0 should pump price up");
        }

        [Test]
        public void ApplyEventEffect_MultiPhase_RecapturesOnPhaseTransition()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.Penny, 100f, TrendDirection.Neutral, 0f);

            var phases = new List<MarketEventPhase>
            {
                new MarketEventPhase(0.80f, 5f),
                new MarketEventPhase(-0.50f, 3f)
            };
            var evt = new MarketEvent(MarketEventType.PumpAndDump, 0, 0.80f, 8f, phases);

            // Apply during phase 0 at full force
            evt.ElapsedTime = 2.5f;
            float priceAtPump = _effects.ApplyEventEffect(stock, evt, 0.016f);
            Assert.Greater(priceAtPump, 100f, "Should have pumped");

            // Simulate price update
            stock.CurrentPrice = priceAtPump;

            // Transition to phase 1 mid-force
            evt.ElapsedTime = 6.5f; // Mid-phase 1
            float priceAtDump = _effects.ApplyEventEffect(stock, evt, 0.016f);

            // Phase 1 target: priceAtPump * (1 - 0.50) = lower price
            Assert.Less(priceAtDump, priceAtPump, "Phase 1 should dump price down from pump peak");
        }

        [Test]
        public void ApplyEventEffect_MultiPhase_FullPumpAndDumpTrajectory()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.Penny, 100f, TrendDirection.Neutral, 0f);

            // Replicate actual PumpAndDump parameters: +80% pump, crash to 80% of original
            float pumpPercent = 0.80f;
            float pumpDuration = 4.8f; // 60% of 8s
            float dumpDuration = 3.2f; // 40% of 8s
            float dumpTarget = 0.80f / (1f + pumpPercent) - 1f;

            var phases = new List<MarketEventPhase>
            {
                new MarketEventPhase(pumpPercent, pumpDuration),
                new MarketEventPhase(dumpTarget, dumpDuration)
            };
            var evt = new MarketEvent(MarketEventType.PumpAndDump, 0, pumpPercent, 8f, phases);

            float startPrice = stock.CurrentPrice;
            float dt = 0.1f;
            float peakPrice = startPrice;
            float totalDuration = evt.Duration;
            float time = 0f;

            // Simulate full timeline frame by frame
            while (time < totalDuration - dt)
            {
                time += dt;
                evt.ElapsedTime = time;

                float price = _effects.ApplyEventEffect(stock, evt, dt);
                if (price > peakPrice)
                    peakPrice = price;

                // Update stock price so next frame sees realistic state
                stock.CurrentPrice = price;
            }

            // Assert: price peaked above start during pump
            Assert.Greater(peakPrice, startPrice * 1.5f,
                $"Price should have pumped significantly above start. Peak={peakPrice}, Start={startPrice}");

            // Assert: final price is below original start (the crash landed below)
            Assert.Less(stock.CurrentPrice, startPrice,
                $"Price should end below start after dump. Final={stock.CurrentPrice}, Start={startPrice}");
        }

        [Test]
        public void ApplyEventEffect_SinglePhase_UnchangedAfterMultiPhaseRefactor()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.MidValue, 100f, TrendDirection.Neutral, 0f);

            var evt = new MarketEvent(MarketEventType.EarningsBeat, 0, 0.25f, 4f);
            evt.ElapsedTime = 2f; // Peak force

            float result = _effects.ApplyEventEffect(stock, evt, 0.016f);

            // Should still work: target = 100 * 1.25 = 125, Lerp(100, 125, 1.0) = 125
            Assert.AreEqual(125f, result, 1f, "Single-phase events should work unchanged");
        }

        [Test]
        public void StartEvent_GlobalEvent_HeadlineUsesGenericText()
        {
            _effects.SetHeadlineRandom(new System.Random(42));

            MarketEventFiredEvent received = default;
            EventBus.Subscribe<MarketEventFiredEvent>(e => received = e);

            var evt = new MarketEvent(MarketEventType.MarketCrash, null, -0.30f, 8f);
            _effects.StartEvent(evt);

            Assert.IsNotNull(received.Headline);
            Assert.IsTrue(received.Headline.Contains("the market"),
                $"Global event headline should use 'the market', got: {received.Headline}");
        }

        [Test]
        public void UpdateActiveEvents_EndedEvent_IncludesTickerSymbols()
        {
            var stocks = new List<StockInstance>();
            var stock = new StockInstance();
            stock.Initialize(3, "ACME", StockTier.MidValue, 100f, TrendDirection.Neutral, 0f);
            stocks.Add(stock);
            _effects.SetActiveStocks(stocks);

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

            Assert.IsTrue(fired);
            Assert.IsNotNull(received.AffectedTickerSymbols);
            Assert.AreEqual("ACME", received.AffectedTickerSymbols[0]);
        }
    }
}
