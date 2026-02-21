using NUnit.Framework;
using System.Collections.Generic;

namespace BullRun.Tests.Chart
{
    [TestFixture]
    public class ChartRendererTests
    {
        private ChartRenderer _renderer;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            _renderer = new ChartRenderer();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
        }

        // --- Core Point Management ---

        [Test]
        public void AddPoint_StoresTimeAndPrice()
        {
            _renderer.AddPoint(0.5f, 150f);

            Assert.AreEqual(1, _renderer.PointCount);
        }

        [Test]
        public void AddPoint_MultiplePoints_AccumulatesInOrder()
        {
            _renderer.AddPoint(0.0f, 100f);
            _renderer.AddPoint(0.25f, 110f);
            _renderer.AddPoint(0.5f, 105f);

            Assert.AreEqual(3, _renderer.PointCount);
        }

        [Test]
        public void GetPoint_ReturnsCorrectValues()
        {
            _renderer.AddPoint(0.3f, 125f);

            var point = _renderer.GetPoint(0);
            Assert.AreEqual(0.3f, point.ElapsedTime, 0.001f);
            Assert.AreEqual(125f, point.Price, 0.001f);
        }

        [Test]
        public void ResetChart_ClearsAllPoints()
        {
            _renderer.AddPoint(0.0f, 100f);
            _renderer.AddPoint(0.5f, 110f);

            _renderer.ResetChart();

            Assert.AreEqual(0, _renderer.PointCount);
        }

        // --- Active Stock Filtering ---

        [Test]
        public void SetActiveStock_StoresActiveStockId()
        {
            _renderer.SetActiveStock(3);

            Assert.AreEqual(3, _renderer.ActiveStockId);
        }

        [Test]
        public void SetActiveStock_ClearsExistingPoints()
        {
            _renderer.SetActiveStock(0);
            _renderer.AddPoint(0.5f, 100f);

            _renderer.SetActiveStock(1);

            Assert.AreEqual(0, _renderer.PointCount);
        }

        // --- Event Processing ---

        [Test]
        public void ProcessPriceUpdate_ForActiveStock_AddsPoint()
        {
            _renderer.SetActiveStock(0);
            _renderer.SetRoundDuration(60f);
            _renderer.StartRound();

            _renderer.ProcessPriceUpdate(new PriceUpdatedEvent
            {
                StockId = 0,
                NewPrice = 105f,
                PreviousPrice = 100f,
                DeltaTime = 0.016f
            });

            Assert.AreEqual(1, _renderer.PointCount);
        }

        [Test]
        public void ProcessPriceUpdate_ForDifferentStock_Ignored()
        {
            _renderer.SetActiveStock(0);
            _renderer.SetRoundDuration(60f);
            _renderer.StartRound();

            _renderer.ProcessPriceUpdate(new PriceUpdatedEvent
            {
                StockId = 1,
                NewPrice = 105f,
                PreviousPrice = 100f,
                DeltaTime = 0.016f
            });

            Assert.AreEqual(0, _renderer.PointCount);
        }

        [Test]
        public void ProcessPriceUpdate_StoresElapsedTime()
        {
            _renderer.SetActiveStock(0);
            _renderer.SetRoundDuration(60f);
            _renderer.StartRound();

            // Simulate ~30 seconds elapsed via accumulated DeltaTime
            // Set to just under 30s so DeltaTime addition reaches ~30s
            _renderer.SetElapsedTime(30f - 0.016f);
            _renderer.ProcessPriceUpdate(new PriceUpdatedEvent
            {
                StockId = 0,
                NewPrice = 110f,
                PreviousPrice = 100f,
                DeltaTime = 0.016f
            });

            var point = _renderer.GetPoint(0);
            Assert.AreEqual(30f, point.ElapsedTime, 0.01f,
                "~30s elapsed should be stored as ~30 seconds");
        }

        // --- Price Range Tracking ---

        [Test]
        public void PriceRange_TracksMinAndMax()
        {
            _renderer.AddPoint(0.0f, 100f);
            _renderer.AddPoint(0.25f, 150f);
            _renderer.AddPoint(0.5f, 80f);

            Assert.AreEqual(80f, _renderer.MinPrice, 0.001f);
            Assert.AreEqual(150f, _renderer.MaxPrice, 0.001f);
        }

        [Test]
        public void PriceRange_ResetOnClear()
        {
            _renderer.AddPoint(0.0f, 100f);
            _renderer.ResetChart();

            Assert.AreEqual(float.MaxValue, _renderer.MinPrice);
            Assert.AreEqual(float.MinValue, _renderer.MaxPrice);
        }

        // --- Current Price ---

        [Test]
        public void CurrentPrice_ReturnsLatestAddedPrice()
        {
            _renderer.AddPoint(0.0f, 100f);
            _renderer.AddPoint(0.5f, 120f);

            Assert.AreEqual(120f, _renderer.CurrentPrice, 0.001f);
        }

        [Test]
        public void CurrentPrice_ZeroWhenNoPoints()
        {
            Assert.AreEqual(0f, _renderer.CurrentPrice, 0.001f);
        }

        // --- No Decimation (procedural mesh handles large point counts) ---

        [Test]
        public void AddPoint_RetainsAllPoints_NoDecimation()
        {
            // Add many points — all should be retained
            for (int i = 0; i <= 150; i++)
            {
                _renderer.AddPoint(i / 150f, 100f + i * 0.1f);
            }

            Assert.AreEqual(151, _renderer.PointCount,
                "All points should be retained without decimation");
        }

        // --- Round Duration ---

        [Test]
        public void SetRoundDuration_StoresValue()
        {
            _renderer.SetRoundDuration(45f);

            Assert.AreEqual(45f, _renderer.RoundDuration, 0.001f);
        }

        [Test]
        public void DefaultRoundDuration_UsesGameConfig()
        {
            Assert.AreEqual(GameConfig.RoundDurationSeconds, _renderer.RoundDuration, 0.001f);
        }

        // --- Event Subscription via EventBus (Task 5) ---

        [Test]
        public void EventBus_PriceUpdatedEvent_RoutesToProcessPriceUpdate()
        {
            _renderer.SetActiveStock(0);
            _renderer.SetRoundDuration(60f);
            _renderer.StartRound();

            EventBus.Subscribe<PriceUpdatedEvent>(_renderer.ProcessPriceUpdate);

            EventBus.Publish(new PriceUpdatedEvent
            {
                StockId = 0,
                NewPrice = 105f,
                PreviousPrice = 100f,
                DeltaTime = 0.016f
            });

            Assert.AreEqual(1, _renderer.PointCount,
                "EventBus subscription should route PriceUpdatedEvent to ChartRenderer");
        }

        [Test]
        public void ProcessPriceUpdate_WhenRoundNotStarted_Ignored()
        {
            _renderer.SetActiveStock(0);
            _renderer.SetRoundDuration(60f);
            // NOT calling StartRound()

            _renderer.ProcessPriceUpdate(new PriceUpdatedEvent
            {
                StockId = 0,
                NewPrice = 105f,
                PreviousPrice = 100f,
                DeltaTime = 0.016f
            });

            Assert.AreEqual(0, _renderer.PointCount,
                "Should not add points when round hasn't started");
        }

        [Test]
        public void ResetChart_ResetsElapsedTime()
        {
            _renderer.SetActiveStock(0);
            _renderer.SetRoundDuration(60f);
            _renderer.StartRound();

            // Accumulate some elapsed time
            _renderer.ProcessPriceUpdate(new PriceUpdatedEvent
            {
                StockId = 0,
                NewPrice = 105f,
                PreviousPrice = 100f,
                DeltaTime = 30f
            });

            _renderer.ResetChart();
            _renderer.StartRound();

            _renderer.ProcessPriceUpdate(new PriceUpdatedEvent
            {
                StockId = 0,
                NewPrice = 110f,
                PreviousPrice = 105f,
                DeltaTime = 0.016f
            });

            var point = _renderer.GetPoint(0);
            Assert.AreEqual(0.016f, point.ElapsedTime, 0.001f,
                "After reset, elapsed time should be just the single DeltaTime");
        }

        [Test]
        public void ProcessPriceUpdate_AccumulatesElapsedTime()
        {
            _renderer.SetActiveStock(0);
            _renderer.SetRoundDuration(60f);
            _renderer.StartRound();

            // Two updates of 15 seconds each = 30 seconds total
            _renderer.ProcessPriceUpdate(new PriceUpdatedEvent
            {
                StockId = 0, NewPrice = 105f, PreviousPrice = 100f, DeltaTime = 15f
            });
            _renderer.ProcessPriceUpdate(new PriceUpdatedEvent
            {
                StockId = 0, NewPrice = 110f, PreviousPrice = 105f, DeltaTime = 15f
            });

            var secondPoint = _renderer.GetPoint(1);
            Assert.AreEqual(30f, secondPoint.ElapsedTime, 0.01f,
                "Two 15s updates should store 30s elapsed time");
        }

        // --- ElapsedTime Property (used by ChartUI for time bar) ---

        [Test]
        public void ElapsedTime_ZeroBeforeAnyUpdates()
        {
            Assert.AreEqual(0f, _renderer.ElapsedTime, 0.001f);
        }

        [Test]
        public void ElapsedTime_AccumulatesFromPriceUpdates()
        {
            _renderer.SetActiveStock(0);
            _renderer.SetRoundDuration(60f);
            _renderer.StartRound();

            _renderer.ProcessPriceUpdate(new PriceUpdatedEvent
            {
                StockId = 0, NewPrice = 105f, PreviousPrice = 100f, DeltaTime = 10f
            });
            _renderer.ProcessPriceUpdate(new PriceUpdatedEvent
            {
                StockId = 0, NewPrice = 110f, PreviousPrice = 105f, DeltaTime = 5f
            });

            Assert.AreEqual(15f, _renderer.ElapsedTime, 0.001f,
                "ElapsedTime should be sum of DeltaTime from price updates");
        }

        [Test]
        public void ElapsedTime_ResetToZeroOnResetChart()
        {
            _renderer.SetActiveStock(0);
            _renderer.SetRoundDuration(60f);
            _renderer.StartRound();

            _renderer.ProcessPriceUpdate(new PriceUpdatedEvent
            {
                StockId = 0, NewPrice = 105f, PreviousPrice = 100f, DeltaTime = 20f
            });

            _renderer.ResetChart();

            Assert.AreEqual(0f, _renderer.ElapsedTime, 0.001f,
                "ElapsedTime should reset to 0 after ResetChart");
        }

        // --- Timer Extension (Time Buyer relic) ---

        [Test]
        public void HandleTimerExtended_TweensRoundDuration()
        {
            _renderer.SetActiveStock(0);
            _renderer.SetRoundDuration(60f);
            _renderer.StartRound();

            // Add a point at 30s elapsed
            _renderer.ProcessPriceUpdate(new PriceUpdatedEvent
            {
                StockId = 0, NewPrice = 105f, PreviousPrice = 100f, DeltaTime = 30f
            });

            // Extend timer by 5s (60 -> 65)
            _renderer.HandleTimerExtended(new RoundTimerExtendedEvent { NewDuration = 65f });

            // Duration should NOT snap immediately — still at 60 before next update
            Assert.AreEqual(60f, _renderer.RoundDuration, 0.001f,
                "RoundDuration should not snap immediately after HandleTimerExtended");

            // After one large deltaTime, tween should move toward target
            _renderer.ProcessPriceUpdate(new PriceUpdatedEvent
            {
                StockId = 0, NewPrice = 110f, PreviousPrice = 105f, DeltaTime = 0.5f
            });

            Assert.Greater(_renderer.RoundDuration, 60f,
                "RoundDuration should begin tweening toward 65");
            Assert.LessOrEqual(_renderer.RoundDuration, 65f,
                "RoundDuration should not overshoot target");

            // After enough frames, should snap to target
            for (int i = 0; i < 60; i++)
            {
                _renderer.ProcessPriceUpdate(new PriceUpdatedEvent
                {
                    StockId = 0, NewPrice = 110f, PreviousPrice = 110f, DeltaTime = 0.016f
                });
            }

            Assert.AreEqual(65f, _renderer.RoundDuration, 0.01f,
                "RoundDuration should reach target after sufficient frames");

            var point = _renderer.GetPoint(1);
            Assert.AreEqual(30.5f, point.ElapsedTime, 0.001f,
                "Points after extension should store correct elapsed time");
        }

        [Test]
        public void ProcessPriceUpdate_ElapsedTimeBeyondOriginalDuration_StoresCorrectly()
        {
            _renderer.SetActiveStock(0);
            _renderer.SetRoundDuration(60f);
            _renderer.StartRound();

            // Simulate 55s elapsed
            _renderer.SetElapsedTime(55f);

            // Extend to 70s
            _renderer.HandleTimerExtended(new RoundTimerExtendedEvent { NewDuration = 70f });

            // Simulate enough frames for tween to complete
            for (int i = 0; i < 60; i++)
            {
                _renderer.ProcessPriceUpdate(new PriceUpdatedEvent
                {
                    StockId = 0, NewPrice = 115f, PreviousPrice = 110f, DeltaTime = 0.016f
                });
            }

            // Add point beyond original 60s duration
            _renderer.ProcessPriceUpdate(new PriceUpdatedEvent
            {
                StockId = 0, NewPrice = 120f, PreviousPrice = 115f, DeltaTime = 5f
            });

            var lastIdx = _renderer.PointCount - 1;
            var point = _renderer.GetPoint(lastIdx);
            Assert.Greater(point.ElapsedTime, 60f,
                "Elapsed time beyond original duration should not be clamped");
            Assert.AreEqual(70f, _renderer.RoundDuration, 0.01f,
                "RoundDuration should reflect extended value after tween completes");
        }

        [Test]
        public void SetRoundDuration_SnapsImmediately_NoTween()
        {
            _renderer.SetRoundDuration(75f);

            Assert.AreEqual(75f, _renderer.RoundDuration, 0.001f,
                "SetRoundDuration should snap immediately (used at round start)");
        }
    }
}
