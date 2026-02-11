using NUnit.Framework;

namespace BullRun.Tests.Trading
{
    [TestFixture]
    public class PositionTests
    {
        [Test]
        public void Constructor_SetsStockId()
        {
            var pos = new Position("ACME", 10, 25.50f);
            Assert.AreEqual("ACME", pos.StockId);
        }

        [Test]
        public void Constructor_SetsShares()
        {
            var pos = new Position("ACME", 10, 25.50f);
            Assert.AreEqual(10, pos.Shares);
        }

        [Test]
        public void Constructor_SetsAverageBuyPrice()
        {
            var pos = new Position("ACME", 10, 25.50f);
            Assert.AreEqual(25.50f, pos.AverageBuyPrice, 0.001f);
        }

        [Test]
        public void Constructor_IsLongTrue()
        {
            var pos = new Position("ACME", 10, 25.50f);
            Assert.IsTrue(pos.IsLong);
        }

        [Test]
        public void Constructor_SetsOpenTime()
        {
            var pos = new Position("ACME", 10, 25.50f);
            Assert.GreaterOrEqual(pos.OpenTime, 0f);
        }

        [Test]
        public void UnrealizedPnL_PriceUp_ReturnsProfit()
        {
            var pos = new Position("ACME", 10, 25.00f);
            float pnl = pos.UnrealizedPnL(30.00f);
            Assert.AreEqual(50.00f, pnl, 0.001f); // (30-25)*10 = 50
        }

        [Test]
        public void UnrealizedPnL_PriceDown_ReturnsLoss()
        {
            var pos = new Position("ACME", 10, 25.00f);
            float pnl = pos.UnrealizedPnL(20.00f);
            Assert.AreEqual(-50.00f, pnl, 0.001f); // (20-25)*10 = -50
        }

        [Test]
        public void UnrealizedPnL_SamePrice_ReturnsZero()
        {
            var pos = new Position("ACME", 10, 25.00f);
            float pnl = pos.UnrealizedPnL(25.00f);
            Assert.AreEqual(0f, pnl, 0.001f);
        }

        [Test]
        public void MarketValue_ReturnsSharesTimesCurrentPrice()
        {
            var pos = new Position("ACME", 10, 25.00f);
            float value = pos.MarketValue(30.00f);
            Assert.AreEqual(300.00f, value, 0.001f); // 10 * 30
        }

        [Test]
        public void MarketValue_ZeroPrice_ReturnsZero()
        {
            var pos = new Position("ACME", 10, 25.00f);
            float value = pos.MarketValue(0f);
            Assert.AreEqual(0f, value, 0.001f);
        }

        // --- Realized P&L Tests (Story 2.2) ---

        [Test]
        public void CalculateRealizedPnL_Profit()
        {
            var pos = new Position("ACME", 10, 25.00f);
            float pnl = pos.CalculateRealizedPnL(30.00f, 10);
            Assert.AreEqual(50.00f, pnl, 0.001f); // (30-25)*10
        }

        [Test]
        public void CalculateRealizedPnL_Loss()
        {
            var pos = new Position("ACME", 10, 25.00f);
            float pnl = pos.CalculateRealizedPnL(20.00f, 10);
            Assert.AreEqual(-50.00f, pnl, 0.001f); // (20-25)*10
        }

        [Test]
        public void CalculateRealizedPnL_PartialSell()
        {
            var pos = new Position("ACME", 10, 25.00f);
            float pnl = pos.CalculateRealizedPnL(30.00f, 5);
            Assert.AreEqual(25.00f, pnl, 0.001f); // (30-25)*5
        }

        [Test]
        public void CalculateRealizedPnL_SamePrice_Zero()
        {
            var pos = new Position("ACME", 10, 25.00f);
            float pnl = pos.CalculateRealizedPnL(25.00f, 10);
            Assert.AreEqual(0f, pnl, 0.001f);
        }

        // --- Short Position Tests (Story 2.3) ---

        [Test]
        public void ShortConstructor_SetsIsShort()
        {
            var pos = new Position("ACME", 10, 25.00f, 125.00f);
            Assert.IsTrue(pos.IsShort);
            Assert.IsFalse(pos.IsLong);
        }

        [Test]
        public void ShortConstructor_SetsMarginHeld()
        {
            var pos = new Position("ACME", 10, 25.00f, 125.00f);
            Assert.AreEqual(125.00f, pos.MarginHeld, 0.001f);
        }

        [Test]
        public void LongConstructor_IsShortFalse()
        {
            var pos = new Position("ACME", 10, 25.00f);
            Assert.IsFalse(pos.IsShort);
            Assert.IsTrue(pos.IsLong);
        }

        [Test]
        public void LongConstructor_MarginHeldZero()
        {
            var pos = new Position("ACME", 10, 25.00f);
            Assert.AreEqual(0f, pos.MarginHeld, 0.001f);
        }

        [Test]
        public void Short_UnrealizedPnL_PriceDown_ReturnsProfit()
        {
            var pos = new Position("ACME", 10, 25.00f, 125.00f);
            float pnl = pos.UnrealizedPnL(20.00f);
            Assert.AreEqual(50.00f, pnl, 0.001f); // (25-20)*10 = 50
        }

        [Test]
        public void Short_UnrealizedPnL_PriceUp_ReturnsLoss()
        {
            var pos = new Position("ACME", 10, 25.00f, 125.00f);
            float pnl = pos.UnrealizedPnL(30.00f);
            Assert.AreEqual(-50.00f, pnl, 0.001f); // (25-30)*10 = -50
        }

        [Test]
        public void Short_CalculateRealizedPnL_PriceDown_Profit()
        {
            var pos = new Position("ACME", 10, 25.00f, 125.00f);
            float pnl = pos.CalculateRealizedPnL(20.00f, 10);
            Assert.AreEqual(50.00f, pnl, 0.001f); // (25-20)*10
        }

        [Test]
        public void Short_CalculateRealizedPnL_PriceUp_Loss()
        {
            var pos = new Position("ACME", 10, 25.00f, 125.00f);
            float pnl = pos.CalculateRealizedPnL(30.00f, 10);
            Assert.AreEqual(-50.00f, pnl, 0.001f); // (25-30)*10
        }
    }
}
