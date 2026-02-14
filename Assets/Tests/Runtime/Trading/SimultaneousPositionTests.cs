using NUnit.Framework;

namespace BullRun.Tests.Trading
{
    /// <summary>
    /// FIX-11: Tests for simultaneous long + short positions on the same stock.
    /// Verifies the separate _positions / _shortPositions dictionaries work correctly.
    /// </summary>
    [TestFixture]
    public class SimultaneousPositionTests
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

        // --- Simultaneous Open ---

        [Test]
        public void LongThenShort_BothExist()
        {
            var portfolio = new Portfolio(2000f);
            portfolio.OpenPosition("ACME", 10, 25.00f); // cost 250, cash 1750
            portfolio.OpenShort("ACME", 5, 50.00f); // margin 125, cash 1625
            Assert.IsTrue(portfolio.HasPosition("ACME"));
            Assert.IsTrue(portfolio.HasShortPosition("ACME"));
            Assert.AreEqual(1, portfolio.PositionCount);
            Assert.AreEqual(1, portfolio.ShortPositionCount);
        }

        [Test]
        public void ShortThenLong_BothExist()
        {
            var portfolio = new Portfolio(2000f);
            portfolio.OpenShort("ACME", 5, 50.00f); // margin 125, cash 1875
            portfolio.OpenPosition("ACME", 10, 25.00f); // cost 250, cash 1625
            Assert.IsTrue(portfolio.HasPosition("ACME"));
            Assert.IsTrue(portfolio.HasShortPosition("ACME"));
        }

        [Test]
        public void GetPosition_ReturnsLongOnly()
        {
            var portfolio = new Portfolio(2000f);
            portfolio.OpenPosition("ACME", 10, 25.00f);
            portfolio.OpenShort("ACME", 5, 50.00f);
            var pos = portfolio.GetPosition("ACME");
            Assert.IsNotNull(pos);
            Assert.IsFalse(pos.IsShort);
            Assert.AreEqual(10, pos.Shares);
        }

        [Test]
        public void GetShortPosition_ReturnsShortOnly()
        {
            var portfolio = new Portfolio(2000f);
            portfolio.OpenPosition("ACME", 10, 25.00f);
            portfolio.OpenShort("ACME", 5, 50.00f);
            var pos = portfolio.GetShortPosition("ACME");
            Assert.IsNotNull(pos);
            Assert.IsTrue(pos.IsShort);
            Assert.AreEqual(5, pos.Shares);
        }

        // --- Independent Close ---

        [Test]
        public void CloseLong_ShortSurvives()
        {
            var portfolio = new Portfolio(2000f);
            portfolio.OpenPosition("ACME", 10, 25.00f);
            portfolio.OpenShort("ACME", 5, 50.00f);
            portfolio.ClosePosition("ACME", 10, 30.00f);
            Assert.IsFalse(portfolio.HasPosition("ACME"));
            Assert.IsTrue(portfolio.HasShortPosition("ACME"));
        }

        [Test]
        public void CoverShort_LongSurvives()
        {
            var portfolio = new Portfolio(2000f);
            portfolio.OpenPosition("ACME", 10, 25.00f);
            portfolio.OpenShort("ACME", 5, 50.00f);
            portfolio.CoverShort("ACME", 5, 40.00f);
            Assert.IsTrue(portfolio.HasPosition("ACME"));
            Assert.IsFalse(portfolio.HasShortPosition("ACME"));
        }

        // --- Total Value with Simultaneous ---

        [Test]
        public void GetTotalValue_SimultaneousPositions_CalculatesBothCorrectly()
        {
            var portfolio = new Portfolio(2000f);
            portfolio.OpenPosition("ACME", 10, 20.00f); // cost 200, cash 1800
            portfolio.OpenShort("ACME", 5, 40.00f); // margin 100, cash 1700
            // Long value: 10 * 30 = 300
            // Short value: margin(100) + unrealizedPnL((40-30)*5 = +50) = 150
            float total = portfolio.GetTotalValue(id => 30.00f);
            Assert.AreEqual(2150f, total, 0.001f); // 1700 + 300 + 150
        }

        [Test]
        public void GetTotalUnrealizedPnL_SimultaneousPositions_SumsBoth()
        {
            var portfolio = new Portfolio(2000f);
            portfolio.OpenPosition("ACME", 10, 20.00f); // long pnl: (30-20)*10 = +100
            portfolio.OpenShort("ACME", 5, 40.00f); // short pnl: (40-30)*5 = +50
            float pnl = portfolio.GetTotalUnrealizedPnL(id => 30.00f);
            Assert.AreEqual(150f, pnl, 0.001f);
        }

        // --- Liquidation ---

        [Test]
        public void LiquidateAll_ClearsBothLongAndShort()
        {
            var portfolio = new Portfolio(2000f);
            portfolio.OpenPosition("ACME", 10, 20.00f);
            portfolio.OpenShort("ACME", 5, 40.00f);
            portfolio.LiquidateAllPositions(id => 30.00f);
            Assert.AreEqual(0, portfolio.PositionCount);
            Assert.AreEqual(0, portfolio.ShortPositionCount);
            Assert.IsFalse(portfolio.HasPosition("ACME"));
            Assert.IsFalse(portfolio.HasShortPosition("ACME"));
        }

        [Test]
        public void LiquidateAll_SimultaneousPositions_ReturnsTotalPnL()
        {
            var portfolio = new Portfolio(2000f);
            portfolio.OpenPosition("ACME", 10, 20.00f); // cost 200, cash 1800
            portfolio.OpenShort("ACME", 5, 40.00f); // margin 100, cash 1700
            // Long pnl: (30-20)*10 = +100
            // Short pnl: (40-30)*5 = +50
            float pnl = portfolio.LiquidateAllPositions(id => 30.00f);
            Assert.AreEqual(150f, pnl, 0.001f);
        }

        // --- Duplicate Short Rejection ---

        [Test]
        public void DuplicateShort_SameStock_Rejected()
        {
            var portfolio = new Portfolio(2000f);
            portfolio.OpenShort("ACME", 5, 50.00f); // margin 125, cash 1875
            var pos = portfolio.OpenShort("ACME", 3, 40.00f); // duplicate rejected
            Assert.IsNull(pos);
            Assert.AreEqual(1875f, portfolio.Cash, 0.001f);
            Assert.AreEqual(1, portfolio.ShortPositionCount);
        }

        [Test]
        public void PortfolioAPI_AllowsMultipleShorts_DifferentStocks()
        {
            // NOTE: Portfolio API allows multiple shorts on different stocks,
            // but game rules (AC 6) limit to ONE short at a time via GameRunner's
            // state machine (single _shortStockId). This tests the data layer only.
            var portfolio = new Portfolio(2000f);
            var pos1 = portfolio.OpenShort("AAA", 5, 50.00f);
            var pos2 = portfolio.OpenShort("BBB", 3, 40.00f);
            Assert.IsNotNull(pos1);
            Assert.IsNotNull(pos2);
            Assert.AreEqual(2, portfolio.ShortPositionCount);
        }

        // --- TradeExecutor Simultaneous ---

        [Test]
        public void TradeExecutor_BuyAndShort_SameStock_BothSucceed()
        {
            var portfolio = new Portfolio(2000f);
            var executor = new TradeExecutor();
            bool buyResult = executor.ExecuteBuy("ACME", 10, 25.00f, portfolio);
            bool shortResult = executor.ExecuteShort("ACME", 5, 50.00f, portfolio);
            Assert.IsTrue(buyResult);
            Assert.IsTrue(shortResult);
            Assert.IsNotNull(portfolio.GetPosition("ACME"));
            Assert.IsNotNull(portfolio.GetShortPosition("ACME"));
        }

        [Test]
        public void TradeExecutor_SellLong_DoesNotAffectShort()
        {
            var portfolio = new Portfolio(2000f);
            var executor = new TradeExecutor();
            executor.ExecuteBuy("ACME", 10, 25.00f, portfolio);
            executor.ExecuteShort("ACME", 5, 50.00f, portfolio);
            bool sellResult = executor.ExecuteSell("ACME", 10, 30.00f, portfolio);
            Assert.IsTrue(sellResult);
            Assert.IsFalse(portfolio.HasPosition("ACME"));
            Assert.IsTrue(portfolio.HasShortPosition("ACME"));
        }

        [Test]
        public void TradeExecutor_CoverShort_DoesNotAffectLong()
        {
            var portfolio = new Portfolio(2000f);
            var executor = new TradeExecutor();
            executor.ExecuteBuy("ACME", 10, 25.00f, portfolio);
            executor.ExecuteShort("ACME", 5, 50.00f, portfolio);
            bool coverResult = executor.ExecuteCover("ACME", 5, 40.00f, portfolio);
            Assert.IsTrue(coverResult);
            Assert.IsTrue(portfolio.HasPosition("ACME"));
            Assert.IsFalse(portfolio.HasShortPosition("ACME"));
        }

        // --- Sell no longer opens short (Smart Sell removed) ---

        [Test]
        public void ExecuteSell_NoLongPosition_RejectsInsteadOfShorting()
        {
            var portfolio = new Portfolio(1000f);
            var executor = new TradeExecutor();
            bool result = executor.ExecuteSell("ACME", 10, 30.00f, portfolio);
            Assert.IsFalse(result);
            Assert.IsFalse(portfolio.HasShortPosition("ACME"));
            Assert.AreEqual(1000f, portfolio.Cash, 0.001f);
        }

        // --- Buy no longer covers short (Smart Buy removed) ---

        [Test]
        public void ExecuteBuy_WithShortPosition_BuysLongInsteadOfCovering()
        {
            var portfolio = new Portfolio(2000f);
            var executor = new TradeExecutor();
            executor.ExecuteShort("ACME", 5, 50.00f, portfolio); // margin 125, cash 1875
            bool buyResult = executor.ExecuteBuy("ACME", 10, 25.00f, portfolio); // buy long
            Assert.IsTrue(buyResult);
            // Both positions should exist — buy did NOT cover the short
            Assert.IsTrue(portfolio.HasPosition("ACME"));
            Assert.IsTrue(portfolio.HasShortPosition("ACME"));
            Assert.AreEqual(10, portfolio.GetPosition("ACME").Shares);
            Assert.AreEqual(5, portfolio.GetShortPosition("ACME").Shares);
        }
    }
}
