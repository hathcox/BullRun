using NUnit.Framework;

namespace BullRun.Tests.Trading
{
    [TestFixture]
    public class TradeExecutedEventTests
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

        [Test]
        public void TradeExecutedEvent_ContainsStockId()
        {
            var evt = new TradeExecutedEvent
            {
                StockId = "ACME",
                Shares = 10,
                Price = 25.00f,
                IsBuy = true,
                IsShort = false,
                TotalCost = 250.00f
            };
            Assert.AreEqual("ACME", evt.StockId);
        }

        [Test]
        public void TradeExecutedEvent_ContainsAllFields()
        {
            var evt = new TradeExecutedEvent
            {
                StockId = "TEST",
                Shares = 5,
                Price = 10.00f,
                IsBuy = false,
                IsShort = true,
                TotalCost = 50.00f,
                ProfitLoss = 12.50f
            };
            Assert.AreEqual("TEST", evt.StockId);
            Assert.AreEqual(5, evt.Shares);
            Assert.AreEqual(10.00f, evt.Price, 0.001f);
            Assert.IsFalse(evt.IsBuy);
            Assert.IsTrue(evt.IsShort);
            Assert.AreEqual(50.00f, evt.TotalCost, 0.001f);
            Assert.AreEqual(12.50f, evt.ProfitLoss, 0.001f);
        }

        // ════════════════════════════════════════════════════════════════
        // ProfitLoss vs TotalCost Semantics (H1/F1 regression guard)
        // TotalCost = gross proceeds (always positive for sells).
        // ProfitLoss = realized P&L (negative for losses).
        // UI must use ProfitLoss for profit/loss display and branching.
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void SellAtLoss_TotalCostPositive_ProfitLossNegative()
        {
            // Sell 10 shares at $25 that were bought at $30: proceeds=$250 but loss=-$50
            var evt = new TradeExecutedEvent
            {
                StockId = "ACME",
                Shares = 10,
                Price = 25.00f,
                IsBuy = false,
                IsShort = false,
                TotalCost = 250.00f,
                ProfitLoss = -50.00f
            };
            Assert.Greater(evt.TotalCost, 0f, "TotalCost (gross proceeds) is always positive for sells");
            Assert.Less(evt.ProfitLoss, 0f, "ProfitLoss must be negative for a losing trade");
        }

        [Test]
        public void SellAtProfit_BothFieldsPositive()
        {
            // Sell 10 shares at $35 that were bought at $25: proceeds=$350, profit=+$100
            var evt = new TradeExecutedEvent
            {
                StockId = "ACME",
                Shares = 10,
                Price = 35.00f,
                IsBuy = false,
                IsShort = false,
                TotalCost = 350.00f,
                ProfitLoss = 100.00f
            };
            Assert.Greater(evt.TotalCost, 0f);
            Assert.Greater(evt.ProfitLoss, 0f, "ProfitLoss must be positive for a winning trade");
        }

        [Test]
        public void CoverAtLoss_ProfitLossNegative()
        {
            // Cover short at $30 that was opened at $25: loss=-$50
            var evt = new TradeExecutedEvent
            {
                StockId = "ACME",
                Shares = 10,
                Price = 30.00f,
                IsBuy = true,
                IsShort = true,
                TotalCost = 300.00f,
                ProfitLoss = -50.00f
            };
            Assert.IsTrue(evt.IsBuy && evt.IsShort, "Cover = IsBuy + IsShort");
            Assert.Less(evt.ProfitLoss, 0f, "ProfitLoss must be negative for a losing cover");
        }

        [Test]
        public void OpeningTrade_ProfitLossIsZero()
        {
            // Buy (opening) trade has no realized P&L
            var evt = new TradeExecutedEvent
            {
                StockId = "ACME",
                Shares = 10,
                Price = 25.00f,
                IsBuy = true,
                IsShort = false,
                TotalCost = 250.00f,
                ProfitLoss = 0f
            };
            Assert.AreEqual(0f, evt.ProfitLoss, 0.001f,
                "Opening trades have zero ProfitLoss — no realized gain/loss");
        }

        [Test]
        public void TradeExecutedEvent_PublishesViaEventBus()
        {
            TradeExecutedEvent received = default;
            bool fired = false;

            EventBus.Subscribe<TradeExecutedEvent>(e =>
            {
                fired = true;
                received = e;
            });

            EventBus.Publish(new TradeExecutedEvent
            {
                StockId = "ACME",
                Shares = 10,
                Price = 25.00f,
                IsBuy = true,
                IsShort = false,
                TotalCost = 250.00f
            });

            Assert.IsTrue(fired);
            Assert.AreEqual("ACME", received.StockId);
        }
    }
}
