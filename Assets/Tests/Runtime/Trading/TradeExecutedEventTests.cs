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
                TotalCost = 50.00f
            };
            Assert.AreEqual("TEST", evt.StockId);
            Assert.AreEqual(5, evt.Shares);
            Assert.AreEqual(10.00f, evt.Price, 0.001f);
            Assert.IsFalse(evt.IsBuy);
            Assert.IsTrue(evt.IsShort);
            Assert.AreEqual(50.00f, evt.TotalCost, 0.001f);
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
