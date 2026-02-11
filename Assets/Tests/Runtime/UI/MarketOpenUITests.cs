using NUnit.Framework;
using System.Collections.Generic;

namespace BullRun.Tests.UI
{
    [TestFixture]
    public class MarketOpenUITests
    {
        [Test]
        public void BuildStockList_NullTickers_ReturnsNoStocksMessage()
        {
            Assert.AreEqual("No stocks available", MarketOpenUI.BuildStockList(null, null, null));
        }

        [Test]
        public void BuildStockList_EmptyTickers_ReturnsNoStocksMessage()
        {
            Assert.AreEqual("No stocks available", MarketOpenUI.BuildStockList(new string[0], new float[0], new string[0]));
        }

        [Test]
        public void BuildStockList_WithStocks_ContainsTickerSymbols()
        {
            string result = MarketOpenUI.BuildStockList(
                new[] { "ACME", "MOON", "STAR" },
                new[] { 150f, 2.50f, 75f },
                new[] { "MidValue", "Penny", "LowValue" });

            Assert.IsTrue(result.Contains("ACME"));
            Assert.IsTrue(result.Contains("MOON"));
            Assert.IsTrue(result.Contains("STAR"));
        }

        [Test]
        public void BuildStockList_WithStocks_ContainsStartingPrices()
        {
            string result = MarketOpenUI.BuildStockList(
                new[] { "ACME", "MOON" },
                new[] { 150f, 2.50f },
                new[] { "MidValue", "Penny" });

            Assert.IsTrue(result.Contains("$150.00"));
            Assert.IsTrue(result.Contains("$2.50"));
        }

        [Test]
        public void BuildStockList_WithStocks_ContainsTierIndicators()
        {
            string result = MarketOpenUI.BuildStockList(
                new[] { "ACME", "MOON" },
                new[] { 150f, 2.50f },
                new[] { "MidValue", "Penny" });

            Assert.IsTrue(result.Contains("[MidValue]"));
            Assert.IsTrue(result.Contains("[Penny]"));
        }

        [Test]
        public void BuildStockList_MultipleStocks_SeparatedByNewlines()
        {
            string result = MarketOpenUI.BuildStockList(
                new[] { "AAA", "BBB" },
                new[] { 100f, 200f },
                new[] { "MidValue", "MidValue" });

            Assert.IsTrue(result.Contains("\n"));
        }
    }
}
