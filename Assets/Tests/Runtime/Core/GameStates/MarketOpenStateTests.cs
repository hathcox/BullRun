using NUnit.Framework;

namespace BullRun.Tests.Core.GameStates
{
    [TestFixture]
    public class MarketOpenStateTests
    {
        private RunContext _ctx;
        private GameStateMachine _sm;
        private PriceGenerator _priceGenerator;
        private MarketOpenState _state;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            _ctx = new RunContext(1, 1, new Portfolio(1000f));
            _sm = new GameStateMachine(_ctx);
            _priceGenerator = new PriceGenerator();

            MarketOpenState.NextConfig = new MarketOpenStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = _priceGenerator
            };

            _state = new MarketOpenState();
        }

        [TearDown]
        public void TearDown()
        {
            MarketOpenState.NextConfig = null;
            TradingState.NextConfig = null;
            EventBus.Clear();
        }

        // --- Enter tests ---

        [Test]
        public void Enter_SetsPreviewDurationFromGameConfig()
        {
            _state.Enter(_ctx);

            Assert.AreEqual(GameConfig.MarketOpenDurationSeconds, _state.PreviewDuration, 0.001f);
        }

        [Test]
        public void Enter_SetsTimeRemainingToPreviewDuration()
        {
            _state.Enter(_ctx);

            Assert.AreEqual(GameConfig.MarketOpenDurationSeconds, _state.TimeRemaining, 0.001f);
        }

        [Test]
        public void Enter_InitializesRoundStocks()
        {
            _state.Enter(_ctx);

            Assert.Greater(_priceGenerator.ActiveStocks.Count, 0, "PriceGenerator should have stocks after Enter");
        }

        [Test]
        public void Enter_PublishesMarketOpenEvent()
        {
            MarketOpenEvent receivedEvent = default;
            bool eventFired = false;
            EventBus.Subscribe<MarketOpenEvent>(e =>
            {
                eventFired = true;
                receivedEvent = e;
            });

            _state.Enter(_ctx);

            Assert.IsTrue(eventFired);
            Assert.AreEqual(_ctx.CurrentRound, receivedEvent.RoundNumber);
            Assert.AreEqual(_ctx.CurrentAct, receivedEvent.Act);
            Assert.IsNotNull(receivedEvent.StockIds);
            Assert.Greater(receivedEvent.StockIds.Length, 0);
            Assert.IsNotNull(receivedEvent.Headline);
            Assert.IsNotEmpty(receivedEvent.Headline);
        }

        [Test]
        public void Enter_MarketOpenEvent_HasCorrectProfitTarget()
        {
            MarketOpenEvent receivedEvent = default;
            EventBus.Subscribe<MarketOpenEvent>(e => receivedEvent = e);

            _state.Enter(_ctx);

            float expectedTarget = MarginCallTargets.GetTarget(_ctx.CurrentRound);
            Assert.AreEqual(expectedTarget, receivedEvent.ProfitTarget, 0.01f);
        }

        [Test]
        public void Enter_SetsStaticAccessors()
        {
            _state.Enter(_ctx);

            Assert.AreEqual(GameConfig.MarketOpenDurationSeconds, MarketOpenState.ActiveTimeRemaining, 0.001f);
            Assert.IsTrue(MarketOpenState.IsActive);
        }

        // --- AdvanceTime tests ---

        [Test]
        public void AdvanceTime_DecrementsTimeRemaining()
        {
            _state.Enter(_ctx);
            float initial = _state.TimeRemaining;

            _state.AdvanceTime(_ctx, 1.0f);

            Assert.AreEqual(initial - 1.0f, _state.TimeRemaining, 0.001f);
        }

        [Test]
        public void AdvanceTime_UpdatesStaticActiveTimeRemaining()
        {
            _state.Enter(_ctx);

            _state.AdvanceTime(_ctx, 2.0f);

            Assert.AreEqual(_state.TimeRemaining, MarketOpenState.ActiveTimeRemaining, 0.001f);
        }

        [Test]
        public void AdvanceTime_WhenExpired_TransitionsToTradingState()
        {
            MarketOpenState.NextConfig = new MarketOpenStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = _priceGenerator
            };
            _sm.TransitionTo<MarketOpenState>();
            var openState = (MarketOpenState)_sm.CurrentState;

            openState.AdvanceTime(_ctx, GameConfig.MarketOpenDurationSeconds + 1f);

            Assert.IsInstanceOf<TradingState>(_sm.CurrentState);
        }

        [Test]
        public void AdvanceTime_WhenExpired_SetsIsActiveFalse()
        {
            _state.Enter(_ctx);

            _state.AdvanceTime(_ctx, GameConfig.MarketOpenDurationSeconds + 1f);

            Assert.IsFalse(MarketOpenState.IsActive);
        }

        [Test]
        public void Enter_MarketOpenEvent_HasTickerSymbols()
        {
            MarketOpenEvent receivedEvent = default;
            EventBus.Subscribe<MarketOpenEvent>(e => receivedEvent = e);

            _state.Enter(_ctx);

            Assert.IsNotNull(receivedEvent.TickerSymbols);
            Assert.Greater(receivedEvent.TickerSymbols.Length, 0);
            Assert.IsNotNull(receivedEvent.StartingPrices);
            Assert.AreEqual(receivedEvent.StockIds.Length, receivedEvent.TickerSymbols.Length);
            Assert.AreEqual(receivedEvent.StockIds.Length, receivedEvent.StartingPrices.Length);
            Assert.IsNotNull(receivedEvent.TierNames);
            Assert.AreEqual(receivedEvent.StockIds.Length, receivedEvent.TierNames.Length);
        }

        [Test]
        public void AdvanceTime_BeforeExpiry_DoesNotTransition()
        {
            MarketOpenState.NextConfig = new MarketOpenStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = _priceGenerator
            };
            _sm.TransitionTo<MarketOpenState>();

            var openState = (MarketOpenState)_sm.CurrentState;
            openState.AdvanceTime(_ctx, 1.0f);

            Assert.IsInstanceOf<MarketOpenState>(_sm.CurrentState);
        }

        // --- Exit tests ---

        [Test]
        public void Exit_SetsIsActiveFalse()
        {
            _state.Enter(_ctx);
            Assert.IsTrue(MarketOpenState.IsActive);

            _state.Exit(_ctx);

            Assert.IsFalse(MarketOpenState.IsActive);
        }
    }
}
