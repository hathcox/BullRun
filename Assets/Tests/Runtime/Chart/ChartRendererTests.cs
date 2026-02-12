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
            Assert.AreEqual(0.3f, point.NormalizedTime, 0.001f);
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
        public void ProcessPriceUpdate_CalculatesNormalizedTime()
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
            Assert.AreEqual(0.5f, point.NormalizedTime, 0.01f,
                "~30s of 60s round should be ~0.5 normalized time");
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
            Assert.Less(point.NormalizedTime, 0.01f,
                "After reset, normalized time should start near 0 again");
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
            Assert.AreEqual(0.5f, secondPoint.NormalizedTime, 0.01f,
                "Two 15s updates should put second point at 0.5 normalized time");
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
    }
}
