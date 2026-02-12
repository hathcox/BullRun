using NUnit.Framework;

namespace BullRun.Tests.PriceEngine
{
    [TestFixture]
    public class StockInstanceTests
    {
        [Test]
        public void Initialize_SetsStockId()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.Penny, 2.50f, TrendDirection.Bull, 0.10f);
            Assert.AreEqual(0, stock.StockId);
        }

        [Test]
        public void Initialize_SetsTickerSymbol()
        {
            var stock = new StockInstance();
            stock.Initialize(1, "ACME", StockTier.MidValue, 100f, TrendDirection.Bear, 0.05f);
            Assert.AreEqual("ACME", stock.TickerSymbol);
        }

        [Test]
        public void Initialize_SetsCurrentPrice()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.Penny, 2.50f, TrendDirection.Bull, 0.10f);
            Assert.AreEqual(2.50f, stock.CurrentPrice, 0.001f);
        }

        [Test]
        public void Initialize_SetsTrendDirection()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.Penny, 2.50f, TrendDirection.Bear, 0.10f);
            Assert.AreEqual(TrendDirection.Bear, stock.TrendDirection);
        }

        [Test]
        public void Initialize_BullTrend_PositiveTrendPerSecond()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.Penny, 2.50f, TrendDirection.Bull, 0.10f);
            Assert.Greater(stock.TrendPerSecond, 0f);
        }

        [Test]
        public void Initialize_BearTrend_NegativeTrendPerSecond()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.Penny, 2.50f, TrendDirection.Bear, 0.10f);
            Assert.Less(stock.TrendPerSecond, 0f);
        }

        [Test]
        public void Initialize_NeutralTrend_ZeroTrendPerSecond()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.Penny, 2.50f, TrendDirection.Neutral, 0.10f);
            Assert.AreEqual(0f, stock.TrendPerSecond);
        }

        [Test]
        public void Initialize_SetsTierConfig()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.BlueChip, 1000f, TrendDirection.Bull, 0.02f);
            var expectedConfig = StockTierData.GetTierConfig(StockTier.BlueChip);
            Assert.AreEqual(expectedConfig.MinPrice, stock.TierConfig.MinPrice);
            Assert.AreEqual(expectedConfig.MaxPrice, stock.TierConfig.MaxPrice);
        }

        [Test]
        public void TrendDirection_Enum_HasThreeValues()
        {
            var values = System.Enum.GetValues(typeof(TrendDirection));
            Assert.AreEqual(3, values.Length);
        }

        [Test]
        public void MultipleStocks_HaveIndependentState()
        {
            var stock1 = new StockInstance();
            stock1.Initialize(0, "AAA", StockTier.Penny, 1.00f, TrendDirection.Bull, 0.10f);

            var stock2 = new StockInstance();
            stock2.Initialize(1, "BBB", StockTier.BlueChip, 2000f, TrendDirection.Bear, 0.02f);

            Assert.AreNotEqual(stock1.StockId, stock2.StockId);
            Assert.AreNotEqual(stock1.TickerSymbol, stock2.TickerSymbol);
            Assert.AreNotEqual(stock1.CurrentPrice, stock2.CurrentPrice);
            Assert.AreNotEqual(stock1.TrendDirection, stock2.TrendDirection);
        }

        [Test]
        public void Initialize_SetsNoiseAmplitudeFromTier()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.Penny, 2.50f, TrendDirection.Bull, 0.10f);
            var tierConfig = StockTierData.GetTierConfig(StockTier.Penny);
            Assert.AreEqual(tierConfig.NoiseAmplitude, stock.NoiseAmplitude);
        }

        [Test]
        public void Initialize_SetsNoiseFrequencyFromTier()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.Penny, 2.50f, TrendDirection.Bull, 0.10f);
            var tierConfig = StockTierData.GetTierConfig(StockTier.Penny);
            Assert.AreEqual(tierConfig.NoiseFrequency, stock.NoiseFrequency);
        }

        [Test]
        public void Initialize_SegmentTimeRemainingStartsAtZero()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.Penny, 2.50f, TrendDirection.Bull, 0.10f);
            Assert.AreEqual(0f, stock.SegmentTimeRemaining);
        }

        // --- Event Tracking Tests (Story 1.3) ---

        [Test]
        public void ActiveEvent_DefaultsToNull()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.MidValue, 100f, TrendDirection.Bull, 0.05f);

            Assert.IsNull(stock.ActiveEvent, "ActiveEvent should default to null");
        }

        [Test]
        public void EventTargetPrice_DefaultsToZero()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.MidValue, 100f, TrendDirection.Bull, 0.05f);

            Assert.AreEqual(0f, stock.EventTargetPrice, 0.001f);
        }

        [Test]
        public void ApplyEvent_SetsActiveEventAndTargetPrice()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.MidValue, 100f, TrendDirection.Bull, 0.05f);

            var evt = new MarketEvent(MarketEventType.EarningsBeat, 0, 0.25f, 5f);
            stock.ApplyEvent(evt, 125f);

            Assert.AreEqual(evt, stock.ActiveEvent);
            Assert.AreEqual(125f, stock.EventTargetPrice, 0.001f);
        }

        [Test]
        public void ClearEvent_ResetsToDefaults()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.MidValue, 100f, TrendDirection.Bull, 0.05f);

            var evt = new MarketEvent(MarketEventType.EarningsBeat, 0, 0.25f, 5f);
            stock.ApplyEvent(evt, 125f);
            stock.ClearEvent();

            Assert.IsNull(stock.ActiveEvent, "ActiveEvent should be null after ClearEvent");
            Assert.AreEqual(0f, stock.EventTargetPrice, 0.001f);
        }

        [Test]
        public void ApplyEvent_OverwritesPreviousEvent()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.MidValue, 100f, TrendDirection.Bull, 0.05f);

            var evt1 = new MarketEvent(MarketEventType.EarningsBeat, 0, 0.25f, 5f);
            stock.ApplyEvent(evt1, 125f);

            var evt2 = new MarketEvent(MarketEventType.MarketCrash, 0, -0.30f, 8f);
            stock.ApplyEvent(evt2, 70f);

            Assert.AreEqual(evt2, stock.ActiveEvent);
            Assert.AreEqual(70f, stock.EventTargetPrice, 0.001f);
        }

        // --- Trend Line Tracking Tests (Story 1.4) ---

        [Test]
        public void Initialize_TrendLinePrice_EqualsStartingPrice()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.MidValue, 100f, TrendDirection.Bull, 0.05f);

            Assert.AreEqual(100f, stock.TrendLinePrice, 0.001f);
        }

        [Test]
        public void UpdateTrendLine_BullTrend_IncreasesTrendLinePrice()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.MidValue, 100f, TrendDirection.Bull, 0.05f);

            float initialTrendLine = stock.TrendLinePrice;
            stock.UpdateTrendLine(1f);

            Assert.Greater(stock.TrendLinePrice, initialTrendLine,
                "Bull trend should increase trend line price");
        }

        [Test]
        public void UpdateTrendLine_BearTrend_DecreasesTrendLinePrice()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.MidValue, 100f, TrendDirection.Bear, 0.05f);

            float initialTrendLine = stock.TrendLinePrice;
            stock.UpdateTrendLine(1f);

            Assert.Less(stock.TrendLinePrice, initialTrendLine,
                "Bear trend should decrease trend line price");
        }

        [Test]
        public void UpdateTrendLine_NeutralTrend_TrendLinePriceUnchanged()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.MidValue, 100f, TrendDirection.Neutral, 0f);

            stock.UpdateTrendLine(1f);

            Assert.AreEqual(100f, stock.TrendLinePrice, 0.001f,
                "Neutral trend should not change trend line price");
        }

        [Test]
        public void UpdateTrendLine_AccumulatesOverMultipleFrames()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.MidValue, 100f, TrendDirection.Bull, 0.05f);

            for (int i = 0; i < 60; i++)
                stock.UpdateTrendLine(0.016f);

            // TrendPerSecond = 100 * 0.05 = 5.0/s. Over ~0.96s that's ~4.8
            float expectedApprox = 100f + (stock.TrendPerSecond * 60 * 0.016f);
            Assert.AreEqual(expectedApprox, stock.TrendLinePrice, 0.01f,
                "Trend line should accumulate correctly over multiple frames");
        }
    }
}
