using NUnit.Framework;
using System.Collections.Generic;

namespace BullRun.Tests.UI
{
    [TestFixture]
    public class StockSidebarTests
    {
        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
        }

        // --- StockEntry Data Tests ---

        [Test]
        public void StockEntry_CalculatePercentChange_Positive()
        {
            var entry = new StockEntry(0, "MEME", 10f);
            entry.UpdatePrice(12f);

            Assert.AreEqual(0.2f, entry.PercentChange, 0.001f);
        }

        [Test]
        public void StockEntry_CalculatePercentChange_Negative()
        {
            var entry = new StockEntry(0, "MEME", 10f);
            entry.UpdatePrice(8f);

            Assert.AreEqual(-0.2f, entry.PercentChange, 0.001f);
        }

        [Test]
        public void StockEntry_CalculatePercentChange_Zero()
        {
            var entry = new StockEntry(0, "MEME", 10f);
            entry.UpdatePrice(10f);

            Assert.AreEqual(0f, entry.PercentChange, 0.001f);
        }

        [Test]
        public void StockEntry_UpdatePrice_StoresCurrentPrice()
        {
            var entry = new StockEntry(0, "MEME", 10f);
            entry.UpdatePrice(15f);

            Assert.AreEqual(15f, entry.CurrentPrice, 0.001f);
        }

        // --- Sparkline Buffer Tests ---

        [Test]
        public void StockEntry_SparklineBuffer_AccumulatesPoints()
        {
            var entry = new StockEntry(0, "MEME", 10f);

            entry.UpdatePrice(11f);
            entry.UpdatePrice(12f);
            entry.UpdatePrice(13f);

            Assert.AreEqual(3, entry.SparklinePointCount);
        }

        [Test]
        public void StockEntry_SparklineBuffer_RollsAtCapacity()
        {
            var entry = new StockEntry(0, "MEME", 10f);

            // Fill beyond capacity (default 20)
            for (int i = 0; i < 25; i++)
            {
                entry.UpdatePrice(10f + i);
            }

            Assert.AreEqual(StockEntry.SparklineCapacity, entry.SparklinePointCount,
                "Sparkline should cap at capacity");
        }

        [Test]
        public void StockEntry_SparklineBuffer_OldestPointDropped()
        {
            var entry = new StockEntry(0, "MEME", 10f);

            for (int i = 0; i < 25; i++)
            {
                entry.UpdatePrice(10f + i);
            }

            // Oldest point should be 10+5=15 (first 5 dropped)
            float oldest = entry.GetSparklinePoint(0);
            Assert.AreEqual(15f, oldest, 0.001f);
        }

        // --- Selection State ---

        [Test]
        public void StockEntry_DefaultNotSelected()
        {
            var entry = new StockEntry(0, "MEME", 10f);
            Assert.IsFalse(entry.IsSelected);
        }

        [Test]
        public void StockEntry_SetSelected()
        {
            var entry = new StockEntry(0, "MEME", 10f);
            entry.IsSelected = true;
            Assert.IsTrue(entry.IsSelected);
        }

        // --- StockSelectedEvent Tests ---

        [Test]
        public void StockSelectedEvent_HasStockIdAndTicker()
        {
            StockSelectedEvent received = default;
            bool fired = false;
            EventBus.Subscribe<StockSelectedEvent>(e =>
            {
                received = e;
                fired = true;
            });

            EventBus.Publish(new StockSelectedEvent { StockId = 2, TickerSymbol = "YOLO" });

            Assert.IsTrue(fired);
            Assert.AreEqual(2, received.StockId);
            Assert.AreEqual("YOLO", received.TickerSymbol);
        }

        // --- StockSidebarData (pure logic) ---

        [Test]
        public void StockSidebarData_InitializeForRound_CreatesEntries()
        {
            var data = new StockSidebarData();
            var stocks = CreateTestStocks();

            data.InitializeForRound(stocks);

            Assert.AreEqual(3, data.EntryCount);
        }

        [Test]
        public void StockSidebarData_InitializeForRound_SelectsFirstByDefault()
        {
            var data = new StockSidebarData();
            var stocks = CreateTestStocks();

            data.InitializeForRound(stocks);

            Assert.AreEqual(0, data.SelectedIndex);
            Assert.IsTrue(data.GetEntry(0).IsSelected);
        }

        [Test]
        public void StockSidebarData_SelectStock_ChangesSelection()
        {
            var data = new StockSidebarData();
            var stocks = CreateTestStocks();
            data.InitializeForRound(stocks);

            data.SelectStock(1);

            Assert.AreEqual(1, data.SelectedIndex);
            Assert.IsFalse(data.GetEntry(0).IsSelected);
            Assert.IsTrue(data.GetEntry(1).IsSelected);
        }

        [Test]
        public void StockSidebarData_SelectStock_PublishesEvent()
        {
            var data = new StockSidebarData();
            var stocks = CreateTestStocks();
            data.InitializeForRound(stocks);

            StockSelectedEvent received = default;
            EventBus.Subscribe<StockSelectedEvent>(e => received = e);

            data.SelectStock(2);

            Assert.AreEqual(2, received.StockId);
            Assert.AreEqual("SAFE", received.TickerSymbol);
        }

        [Test]
        public void StockSidebarData_SelectStock_OutOfRange_Ignored()
        {
            var data = new StockSidebarData();
            var stocks = CreateTestStocks();
            data.InitializeForRound(stocks);

            data.SelectStock(10); // Out of range

            Assert.AreEqual(0, data.SelectedIndex, "Should remain on first stock");
        }

        [Test]
        public void StockSidebarData_SelectStock_NegativeIndex_Ignored()
        {
            var data = new StockSidebarData();
            var stocks = CreateTestStocks();
            data.InitializeForRound(stocks);

            data.SelectStock(-1);

            Assert.AreEqual(0, data.SelectedIndex);
        }

        [Test]
        public void StockSidebarData_ProcessPriceUpdate_UpdatesCorrectEntry()
        {
            var data = new StockSidebarData();
            var stocks = CreateTestStocks();
            data.InitializeForRound(stocks);

            data.ProcessPriceUpdate(new PriceUpdatedEvent
            {
                StockId = 1,
                NewPrice = 55f,
                PreviousPrice = 50f,
                DeltaTime = 0.016f
            });

            Assert.AreEqual(55f, data.GetEntry(1).CurrentPrice, 0.001f);
        }

        [Test]
        public void StockSidebarData_ProcessPriceUpdate_UnknownStock_Ignored()
        {
            var data = new StockSidebarData();
            var stocks = CreateTestStocks();
            data.InitializeForRound(stocks);

            // Should not throw
            data.ProcessPriceUpdate(new PriceUpdatedEvent
            {
                StockId = 99,
                NewPrice = 100f,
                PreviousPrice = 90f,
                DeltaTime = 0.016f
            });

            Assert.AreEqual(3, data.EntryCount);
        }

        [Test]
        public void StockSidebarData_InitializeForRound_PublishesDefaultSelection()
        {
            var data = new StockSidebarData();
            var stocks = CreateTestStocks();

            StockSelectedEvent received = default;
            EventBus.Subscribe<StockSelectedEvent>(e => received = e);

            data.InitializeForRound(stocks);

            Assert.AreEqual(0, received.StockId);
            Assert.AreEqual("MEME", received.TickerSymbol);
        }

        // --- Helper ---

        private List<StockInstance> CreateTestStocks()
        {
            var stocks = new List<StockInstance>();

            var s1 = new StockInstance();
            s1.Initialize(0, "MEME", StockTier.Penny, 2.50f, TrendDirection.Bull, 0.05f);
            stocks.Add(s1);

            var s2 = new StockInstance();
            s2.Initialize(1, "YOLO", StockTier.MidValue, 50f, TrendDirection.Bear, 0.03f);
            stocks.Add(s2);

            var s3 = new StockInstance();
            s3.Initialize(2, "SAFE", StockTier.BlueChip, 1500f, TrendDirection.Neutral, 0f);
            stocks.Add(s3);

            return stocks;
        }
    }
}
