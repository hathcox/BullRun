using NUnit.Framework;

namespace BullRun.Tests.Core.GameStates
{
    [TestFixture]
    public class MarketCloseStateTests
    {
        private RunContext _ctx;
        private GameStateMachine _sm;
        private PriceGenerator _priceGenerator;
        private TradeExecutor _tradeExecutor;
        private MarketCloseState _state;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            _ctx = new RunContext(1, 1, new Portfolio(1000f));
            _sm = new GameStateMachine(_ctx);
            _priceGenerator = new PriceGenerator();
            _tradeExecutor = new TradeExecutor();

            MarketCloseState.NextConfig = new MarketCloseStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = _priceGenerator,
                TradeExecutor = _tradeExecutor
            };

            _state = new MarketCloseState();
        }

        [TearDown]
        public void TearDown()
        {
            MarketCloseState.NextConfig = null;
            EventBus.Clear();
        }

        // --- Enter tests ---

        [Test]
        public void Enter_DisablesTrading()
        {
            Assert.IsTrue(_tradeExecutor.IsTradeEnabled);

            _state.Enter(_ctx);

            Assert.IsFalse(_tradeExecutor.IsTradeEnabled);
        }

        [Test]
        public void Enter_SetsIsActiveTrue()
        {
            _state.Enter(_ctx);

            Assert.IsTrue(MarketCloseState.IsActive);
        }

        [Test]
        public void Enter_PublishesMarketClosedEvent()
        {
            MarketClosedEvent receivedEvent = default;
            bool eventFired = false;
            EventBus.Subscribe<MarketClosedEvent>(e =>
            {
                eventFired = true;
                receivedEvent = e;
            });

            _state.Enter(_ctx);

            Assert.IsTrue(eventFired);
            Assert.AreEqual(_ctx.CurrentRound, receivedEvent.RoundNumber);
            Assert.AreEqual(_ctx.Portfolio.Cash, receivedEvent.FinalCash, 0.01f);
        }

        [Test]
        public void Enter_WithNoPositions_ReportsZeroProfit()
        {
            MarketClosedEvent receivedEvent = default;
            EventBus.Subscribe<MarketClosedEvent>(e => receivedEvent = e);

            _state.Enter(_ctx);

            Assert.AreEqual(0f, receivedEvent.RoundProfit, 0.01f);
            Assert.AreEqual(0, receivedEvent.PositionsLiquidated);
        }

        [Test]
        public void Enter_WithLongPosition_LiquidatesAndReportsProfit()
        {
            // Buy 10 shares at $50 → cost $500, cash = $500
            _ctx.Portfolio.OpenPosition("0", 10, 50f);

            // Set up a stock at price $60 in PriceGenerator
            _priceGenerator.InitializeRound(1, 1);
            // Find the stock with ID 0 and update its price
            if (_priceGenerator.ActiveStocks.Count > 0)
            {
                _priceGenerator.ActiveStocks[0].CurrentPrice = 60f;
            }

            MarketCloseState.NextConfig = new MarketCloseStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = _priceGenerator,
                TradeExecutor = _tradeExecutor
            };

            MarketClosedEvent receivedEvent = default;
            EventBus.Subscribe<MarketClosedEvent>(e => receivedEvent = e);

            _state.Enter(_ctx);

            // Position should be liquidated
            Assert.AreEqual(0, _ctx.Portfolio.PositionCount);
            Assert.AreEqual(1, receivedEvent.PositionsLiquidated);
        }

        [Test]
        public void Enter_SetsStaticRoundProfit()
        {
            _state.Enter(_ctx);

            Assert.AreEqual(0f, MarketCloseState.RoundProfit, 0.01f);
        }

        [Test]
        public void Enter_MarketClosedEvent_HasCorrectRoundNumber()
        {
            var ctx = new RunContext(1, 3, new Portfolio(1000f));
            MarketClosedEvent receivedEvent = default;
            EventBus.Subscribe<MarketClosedEvent>(e => receivedEvent = e);

            MarketCloseState.NextConfig = new MarketCloseStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = _priceGenerator,
                TradeExecutor = _tradeExecutor
            };

            _state.Enter(ctx);

            Assert.AreEqual(3, receivedEvent.RoundNumber);
        }

        [Test]
        public void Enter_WithShortPosition_LiquidatesAndReportsProfit()
        {
            // Short 10 shares at $50 → margin $250, cash = $750
            _ctx.Portfolio.OpenShort("0", 10, 50f);

            _priceGenerator.InitializeRound(1, 1);
            if (_priceGenerator.ActiveStocks.Count > 0)
            {
                // Price dropped to $40 — short profits $100
                _priceGenerator.ActiveStocks[0].CurrentPrice = 40f;
            }

            MarketCloseState.NextConfig = new MarketCloseStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = _priceGenerator,
                TradeExecutor = _tradeExecutor
            };

            MarketClosedEvent receivedEvent = default;
            EventBus.Subscribe<MarketClosedEvent>(e => receivedEvent = e);

            _state.Enter(_ctx);

            Assert.AreEqual(0, _ctx.Portfolio.PositionCount);
            Assert.AreEqual(1, receivedEvent.PositionsLiquidated);
            // Short P&L: (50 - 40) * 10 = +100
            Assert.AreEqual(100f, receivedEvent.RoundProfit, 0.01f);
        }

        [Test]
        public void AdvanceTime_AfterPauseDuration_TransitionsThroughMarginCallState()
        {
            // With RoundProfit = 0 (below $200 target), MarginCallState chains
            // through to RunSummaryState (margin call failure path)
            MarketCloseState.NextConfig = new MarketCloseStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = _priceGenerator,
                TradeExecutor = _tradeExecutor
            };
            _sm.TransitionTo<MarketCloseState>();
            var closeState = (MarketCloseState)_sm.CurrentState;

            closeState.AdvanceTime(_ctx, MarketCloseState.PauseDuration + 1f);

            // MarginCallState fires immediately and routes to RunSummaryState
            Assert.IsInstanceOf<RunSummaryState>(_sm.CurrentState);
        }

        // --- AdvanceTime tests ---

        [Test]
        public void AdvanceTime_AfterPauseDuration_SetsIsActiveFalse()
        {
            _state.Enter(_ctx);
            Assert.IsTrue(MarketCloseState.IsActive);

            _state.AdvanceTime(_ctx, MarketCloseState.PauseDuration + 1f);

            Assert.IsFalse(MarketCloseState.IsActive);
        }

        [Test]
        public void AdvanceTime_BeforePauseDuration_RemainsActive()
        {
            _state.Enter(_ctx);

            _state.AdvanceTime(_ctx, 0.5f);

            Assert.IsTrue(MarketCloseState.IsActive);
        }

        [Test]
        public void AdvanceTime_PartialPause_StaysActive()
        {
            _state.Enter(_ctx);

            _state.AdvanceTime(_ctx, MarketCloseState.PauseDuration - 0.1f);

            Assert.IsTrue(MarketCloseState.IsActive);
        }

        // --- Exit tests ---

        [Test]
        public void Exit_SetsIsActiveFalse()
        {
            _state.Enter(_ctx);
            Assert.IsTrue(MarketCloseState.IsActive);

            _state.Exit(_ctx);

            Assert.IsFalse(MarketCloseState.IsActive);
        }
    }
}
