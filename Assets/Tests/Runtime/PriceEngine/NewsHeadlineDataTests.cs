using NUnit.Framework;
using System.Collections.Generic;

namespace BullRun.Tests.PriceEngine
{
    [TestFixture]
    public class NewsHeadlineDataTests
    {
        [Test]
        public void BullishHeadlines_IsNotEmpty()
        {
            Assert.Greater(NewsHeadlineData.BullishHeadlines.Length, 0);
        }

        [Test]
        public void BearishHeadlines_IsNotEmpty()
        {
            Assert.Greater(NewsHeadlineData.BearishHeadlines.Length, 0);
        }

        [Test]
        public void VolatileHeadlines_IsNotEmpty()
        {
            Assert.Greater(NewsHeadlineData.VolatileHeadlines.Length, 0);
        }

        [Test]
        public void NeutralHeadlines_IsNotEmpty()
        {
            Assert.Greater(NewsHeadlineData.NeutralHeadlines.Length, 0);
        }

        [Test]
        public void GetHeadline_Bull_ReturnsFromBullishPool()
        {
            var random = new System.Random(42);
            string headline = NewsHeadlineData.GetHeadline(TrendDirection.Bull, random);

            Assert.IsNotNull(headline);
            Assert.IsNotEmpty(headline);
            CollectionAssert.Contains(NewsHeadlineData.BullishHeadlines, headline);
        }

        [Test]
        public void GetHeadline_Bear_ReturnsFromBearishPool()
        {
            var random = new System.Random(42);
            string headline = NewsHeadlineData.GetHeadline(TrendDirection.Bear, random);

            Assert.IsNotNull(headline);
            CollectionAssert.Contains(NewsHeadlineData.BearishHeadlines, headline);
        }

        [Test]
        public void GetHeadline_Neutral_ReturnsFromNeutralPool()
        {
            var random = new System.Random(42);
            string headline = NewsHeadlineData.GetHeadline(TrendDirection.Neutral, random);

            Assert.IsNotNull(headline);
            CollectionAssert.Contains(NewsHeadlineData.NeutralHeadlines, headline);
        }

        [Test]
        public void GetDominantTrend_MostlyBull_ReturnsBull()
        {
            var stocks = CreateStocksWithTrends(TrendDirection.Bull, TrendDirection.Bull, TrendDirection.Bear);
            Assert.AreEqual(TrendDirection.Bull, NewsHeadlineData.GetDominantTrend(stocks));
        }

        [Test]
        public void GetDominantTrend_MostlyBear_ReturnsBear()
        {
            var stocks = CreateStocksWithTrends(TrendDirection.Bear, TrendDirection.Bear, TrendDirection.Bull);
            Assert.AreEqual(TrendDirection.Bear, NewsHeadlineData.GetDominantTrend(stocks));
        }

        [Test]
        public void GetDominantTrend_Equal_ReturnsNeutral()
        {
            var stocks = CreateStocksWithTrends(TrendDirection.Bull, TrendDirection.Bear);
            Assert.AreEqual(TrendDirection.Neutral, NewsHeadlineData.GetDominantTrend(stocks));
        }

        [Test]
        public void GetDominantTrend_AllNeutral_ReturnsNeutral()
        {
            var stocks = CreateStocksWithTrends(TrendDirection.Neutral, TrendDirection.Neutral);
            Assert.AreEqual(TrendDirection.Neutral, NewsHeadlineData.GetDominantTrend(stocks));
        }

        [Test]
        public void GetHeadline_Stocks_VolatileMix_ReturnsFromVolatilePool()
        {
            var stocks = CreateStocksWithTrends(TrendDirection.Bull, TrendDirection.Bear);
            var random = new System.Random(42);
            string headline = NewsHeadlineData.GetHeadline(stocks, random);

            Assert.IsNotNull(headline);
            CollectionAssert.Contains(NewsHeadlineData.VolatileHeadlines, headline);
        }

        [Test]
        public void GetHeadline_Stocks_MostlyBull_ReturnsFromBullishPool()
        {
            var stocks = CreateStocksWithTrends(TrendDirection.Bull, TrendDirection.Bull, TrendDirection.Bear);
            var random = new System.Random(42);
            string headline = NewsHeadlineData.GetHeadline(stocks, random);

            Assert.IsNotNull(headline);
            CollectionAssert.Contains(NewsHeadlineData.BullishHeadlines, headline);
        }

        [Test]
        public void GetHeadline_Stocks_AllNeutral_ReturnsFromNeutralPool()
        {
            var stocks = CreateStocksWithTrends(TrendDirection.Neutral, TrendDirection.Neutral);
            var random = new System.Random(42);
            string headline = NewsHeadlineData.GetHeadline(stocks, random);

            Assert.IsNotNull(headline);
            CollectionAssert.Contains(NewsHeadlineData.NeutralHeadlines, headline);
        }

        private List<StockInstance> CreateStocksWithTrends(params TrendDirection[] trends)
        {
            var stocks = new List<StockInstance>();
            for (int i = 0; i < trends.Length; i++)
            {
                var stock = new StockInstance();
                stock.Initialize(i, $"TST{i}", StockTier.MidValue, 100f, trends[i], 0.05f);
                stocks.Add(stock);
            }
            return stocks;
        }
    }
}
