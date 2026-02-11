using NUnit.Framework;

namespace BullRun.Tests.Trading
{
    [TestFixture]
    public class TradeExecutorTests
    {
        private Portfolio _portfolio;
        private TradeExecutor _executor;
        private TradeExecutedEvent _lastEvent;
        private bool _eventReceived;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            _portfolio = new Portfolio(1000f);
            _executor = new TradeExecutor();
            _eventReceived = false;
            _lastEvent = default;

            EventBus.Subscribe<TradeExecutedEvent>(e =>
            {
                _eventReceived = true;
                _lastEvent = e;
            });
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
        }

        [Test]
        public void ExecuteBuy_Success_DeductsCash()
        {
            _executor.ExecuteBuy("ACME", 10, 25.00f, _portfolio);
            Assert.AreEqual(750f, _portfolio.Cash, 0.001f);
        }

        [Test]
        public void ExecuteBuy_Success_CreatesPosition()
        {
            _executor.ExecuteBuy("ACME", 10, 25.00f, _portfolio);
            var pos = _portfolio.GetPosition("ACME");
            Assert.IsNotNull(pos);
            Assert.AreEqual(10, pos.Shares);
            Assert.AreEqual(25.00f, pos.AverageBuyPrice, 0.001f);
        }

        [Test]
        public void ExecuteBuy_Success_PublishesTradeExecutedEvent()
        {
            _executor.ExecuteBuy("ACME", 10, 25.00f, _portfolio);
            Assert.IsTrue(_eventReceived);
            Assert.AreEqual("ACME", _lastEvent.StockId);
            Assert.AreEqual(10, _lastEvent.Shares);
            Assert.AreEqual(25.00f, _lastEvent.Price, 0.001f);
            Assert.IsTrue(_lastEvent.IsBuy);
            Assert.IsFalse(_lastEvent.IsShort);
            Assert.AreEqual(250.00f, _lastEvent.TotalCost, 0.001f);
        }

        [Test]
        public void ExecuteBuy_InsufficientCash_SkipsSilently()
        {
            _executor.ExecuteBuy("ACME", 100, 25.00f, _portfolio); // costs 2500, have 1000
            Assert.AreEqual(1000f, _portfolio.Cash, 0.001f);
            Assert.IsNull(_portfolio.GetPosition("ACME"));
            Assert.IsFalse(_eventReceived);
        }

        [Test]
        public void ExecuteBuy_ExactCash_Succeeds()
        {
            _executor.ExecuteBuy("ACME", 40, 25.00f, _portfolio); // costs exactly 1000
            Assert.AreEqual(0f, _portfolio.Cash, 0.001f);
            Assert.IsNotNull(_portfolio.GetPosition("ACME"));
            Assert.IsTrue(_eventReceived);
        }

        [Test]
        public void ExecuteBuy_MultipleBuys_WorkCorrectly()
        {
            _executor.ExecuteBuy("AAA", 5, 10.00f, _portfolio); // -50
            _executor.ExecuteBuy("BBB", 3, 20.00f, _portfolio); // -60
            Assert.AreEqual(890f, _portfolio.Cash, 0.001f);
            Assert.IsNotNull(_portfolio.GetPosition("AAA"));
            Assert.IsNotNull(_portfolio.GetPosition("BBB"));
        }

        [Test]
        public void ExecuteBuy_ReturnsTrue_OnSuccess()
        {
            bool result = _executor.ExecuteBuy("ACME", 10, 25.00f, _portfolio);
            Assert.IsTrue(result);
        }

        [Test]
        public void ExecuteBuy_ReturnsFalse_OnInsufficientCash()
        {
            bool result = _executor.ExecuteBuy("ACME", 100, 25.00f, _portfolio);
            Assert.IsFalse(result);
        }

        // --- ExecuteSell Tests (Story 2.2) ---

        [Test]
        public void ExecuteSell_Success_AddsCash()
        {
            _executor.ExecuteBuy("ACME", 10, 25.00f, _portfolio); // cash: 750
            _eventReceived = false;
            _executor.ExecuteSell("ACME", 10, 30.00f, _portfolio); // cash: 750 + 300 = 1050
            Assert.AreEqual(1050f, _portfolio.Cash, 0.001f);
        }

        [Test]
        public void ExecuteSell_Success_RemovesPosition()
        {
            _executor.ExecuteBuy("ACME", 10, 25.00f, _portfolio);
            _executor.ExecuteSell("ACME", 10, 30.00f, _portfolio);
            Assert.IsNull(_portfolio.GetPosition("ACME"));
        }

        [Test]
        public void ExecuteSell_Success_PublishesTradeExecutedEvent()
        {
            _executor.ExecuteBuy("ACME", 10, 25.00f, _portfolio);
            _eventReceived = false;
            _executor.ExecuteSell("ACME", 10, 30.00f, _portfolio);

            Assert.IsTrue(_eventReceived);
            Assert.AreEqual("ACME", _lastEvent.StockId);
            Assert.AreEqual(10, _lastEvent.Shares);
            Assert.AreEqual(30.00f, _lastEvent.Price, 0.001f);
            Assert.IsFalse(_lastEvent.IsBuy);
            Assert.IsFalse(_lastEvent.IsShort);
            Assert.AreEqual(300.00f, _lastEvent.TotalCost, 0.001f);
        }

        [Test]
        public void ExecuteSell_NoPosition_SkipsSilently()
        {
            _executor.ExecuteSell("ACME", 10, 30.00f, _portfolio);
            Assert.AreEqual(1000f, _portfolio.Cash, 0.001f);
            Assert.IsFalse(_eventReceived);
        }

        [Test]
        public void ExecuteSell_MoreThanHeld_SkipsSilently()
        {
            _executor.ExecuteBuy("ACME", 5, 25.00f, _portfolio); // cash: 875
            _eventReceived = false;
            _executor.ExecuteSell("ACME", 10, 30.00f, _portfolio); // trying to sell 10, have 5
            Assert.AreEqual(875f, _portfolio.Cash, 0.001f);
            Assert.IsFalse(_eventReceived);
        }

        [Test]
        public void ExecuteSell_PartialSell_ReducesPosition()
        {
            _executor.ExecuteBuy("ACME", 10, 25.00f, _portfolio);
            _executor.ExecuteSell("ACME", 5, 30.00f, _portfolio);
            var pos = _portfolio.GetPosition("ACME");
            Assert.IsNotNull(pos);
            Assert.AreEqual(5, pos.Shares);
        }

        [Test]
        public void ExecuteSell_ReturnsTrue_OnSuccess()
        {
            _executor.ExecuteBuy("ACME", 10, 25.00f, _portfolio);
            bool result = _executor.ExecuteSell("ACME", 10, 30.00f, _portfolio);
            Assert.IsTrue(result);
        }

        [Test]
        public void ExecuteSell_ReturnsFalse_OnNoPosition()
        {
            bool result = _executor.ExecuteSell("ACME", 10, 30.00f, _portfolio);
            Assert.IsFalse(result);
        }

        // --- ExecuteShort Tests (Story 2.3) ---

        [Test]
        public void ExecuteShort_Success_DeductsMargin()
        {
            _executor.ExecuteShort("ACME", 10, 50.00f, _portfolio); // margin = 250
            Assert.AreEqual(750f, _portfolio.Cash, 0.001f);
        }

        [Test]
        public void ExecuteShort_Success_CreatesShortPosition()
        {
            _executor.ExecuteShort("ACME", 10, 50.00f, _portfolio);
            var pos = _portfolio.GetPosition("ACME");
            Assert.IsNotNull(pos);
            Assert.IsTrue(pos.IsShort);
            Assert.AreEqual(10, pos.Shares);
        }

        [Test]
        public void ExecuteShort_Success_PublishesEvent()
        {
            _executor.ExecuteShort("ACME", 10, 50.00f, _portfolio);
            Assert.IsTrue(_eventReceived);
            Assert.AreEqual("ACME", _lastEvent.StockId);
            Assert.IsFalse(_lastEvent.IsBuy);
            Assert.IsTrue(_lastEvent.IsShort);
        }

        [Test]
        public void ExecuteShort_InsufficientCash_Rejected()
        {
            _portfolio = new Portfolio(100f);
            bool result = _executor.ExecuteShort("ACME", 10, 50.00f, _portfolio);
            Assert.IsFalse(result);
            Assert.AreEqual(100f, _portfolio.Cash, 0.001f);
        }

        [Test]
        public void ExecuteShort_ReturnsTrue_OnSuccess()
        {
            bool result = _executor.ExecuteShort("ACME", 10, 50.00f, _portfolio);
            Assert.IsTrue(result);
        }

        // --- ExecuteCover Tests (Story 2.3) ---

        [Test]
        public void ExecuteCover_Success_ReturnsMarginAndProfit()
        {
            _executor.ExecuteShort("ACME", 10, 50.00f, _portfolio); // margin: 250, cash: 750
            _eventReceived = false;
            _executor.ExecuteCover("ACME", 10, 30.00f, _portfolio); // pnl: +200
            Assert.AreEqual(1200f, _portfolio.Cash, 0.001f); // 750 + 250 + 200
        }

        [Test]
        public void ExecuteCover_Success_RemovesPosition()
        {
            _executor.ExecuteShort("ACME", 10, 50.00f, _portfolio);
            _executor.ExecuteCover("ACME", 10, 30.00f, _portfolio);
            Assert.IsNull(_portfolio.GetPosition("ACME"));
        }

        [Test]
        public void ExecuteCover_Success_PublishesEvent()
        {
            _executor.ExecuteShort("ACME", 10, 50.00f, _portfolio);
            _eventReceived = false;
            _executor.ExecuteCover("ACME", 10, 30.00f, _portfolio);
            Assert.IsTrue(_eventReceived);
            Assert.IsTrue(_lastEvent.IsBuy);
            Assert.IsTrue(_lastEvent.IsShort);
        }

        [Test]
        public void ExecuteCover_NoPosition_Rejected()
        {
            bool result = _executor.ExecuteCover("ACME", 10, 30.00f, _portfolio);
            Assert.IsFalse(result);
            Assert.IsFalse(_eventReceived);
        }

        [Test]
        public void ExecuteCover_LongPosition_Rejected()
        {
            _executor.ExecuteBuy("ACME", 10, 25.00f, _portfolio);
            _eventReceived = false;
            bool result = _executor.ExecuteCover("ACME", 10, 30.00f, _portfolio);
            Assert.IsFalse(result);
            Assert.IsFalse(_eventReceived);
        }

        [Test]
        public void ExecuteCover_ReturnsTrue_OnSuccess()
        {
            _executor.ExecuteShort("ACME", 10, 50.00f, _portfolio);
            bool result = _executor.ExecuteCover("ACME", 10, 30.00f, _portfolio);
            Assert.IsTrue(result);
        }

        // --- IsTradeEnabled Tests (Story 4.3) ---

        [Test]
        public void IsTradeEnabled_DefaultsToTrue()
        {
            Assert.IsTrue(_executor.IsTradeEnabled);
        }

        [Test]
        public void ExecuteBuy_WhenDisabled_ReturnsFalse()
        {
            _executor.IsTradeEnabled = false;
            bool result = _executor.ExecuteBuy("ACME", 10, 25.00f, _portfolio);
            Assert.IsFalse(result);
            Assert.AreEqual(1000f, _portfolio.Cash, 0.001f);
            Assert.IsFalse(_eventReceived);
        }

        [Test]
        public void ExecuteSell_WhenDisabled_ReturnsFalse()
        {
            _executor.ExecuteBuy("ACME", 10, 25.00f, _portfolio);
            _eventReceived = false;
            _executor.IsTradeEnabled = false;
            bool result = _executor.ExecuteSell("ACME", 10, 30.00f, _portfolio);
            Assert.IsFalse(result);
            Assert.IsFalse(_eventReceived);
        }

        [Test]
        public void ExecuteShort_WhenDisabled_ReturnsFalse()
        {
            _executor.IsTradeEnabled = false;
            bool result = _executor.ExecuteShort("ACME", 10, 50.00f, _portfolio);
            Assert.IsFalse(result);
            Assert.AreEqual(1000f, _portfolio.Cash, 0.001f);
            Assert.IsFalse(_eventReceived);
        }

        [Test]
        public void ExecuteCover_WhenDisabled_ReturnsFalse()
        {
            _executor.ExecuteShort("ACME", 10, 50.00f, _portfolio);
            _eventReceived = false;
            _executor.IsTradeEnabled = false;
            bool result = _executor.ExecuteCover("ACME", 10, 30.00f, _portfolio);
            Assert.IsFalse(result);
            Assert.IsFalse(_eventReceived);
        }
    }
}
