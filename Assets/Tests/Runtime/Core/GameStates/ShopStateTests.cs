using NUnit.Framework;

namespace BullRun.Tests.Core.GameStates
{
    [TestFixture]
    public class ShopStateTests
    {
        private RunContext _ctx;
        private GameStateMachine _sm;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            _ctx = new RunContext(1, 1, new Portfolio(1000f));
            _sm = new GameStateMachine(_ctx);
        }

        [TearDown]
        public void TearDown()
        {
            ShopState.NextConfig = null;
            TierTransitionState.NextConfig = null;
            EventBus.Clear();
        }

        [Test]
        public void Enter_DoesNotThrow()
        {
            ShopState.NextConfig = new ShopStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = null,
                TradeExecutor = null
            };
            var state = new ShopState();
            Assert.DoesNotThrow(() => state.Enter(_ctx));
        }

        [Test]
        public void ImplementsIGameState()
        {
            var state = new ShopState();
            Assert.IsInstanceOf<IGameState>(state);
        }

        [Test]
        public void Enter_AutoSkipsToMarketOpenState()
        {
            ShopState.NextConfig = new ShopStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = null,
                TradeExecutor = null
            };
            _sm.TransitionTo<ShopState>();

            Assert.IsInstanceOf<MarketOpenState>(_sm.CurrentState);
        }

        // --- Story 4.5 Task 3: Round/Act Progression ---

        [Test]
        public void Enter_AdvancesRound()
        {
            ShopState.NextConfig = new ShopStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = null,
                TradeExecutor = null
            };
            Assert.AreEqual(1, _ctx.CurrentRound);
            _sm.TransitionTo<ShopState>();
            Assert.AreEqual(2, _ctx.CurrentRound);
        }

        [Test]
        public void Enter_UpdatesActAtBoundary()
        {
            _ctx.CurrentRound = 2; // After shop, round advances to 3 = Act 2
            _ctx.CurrentAct = 1;
            ShopState.NextConfig = new ShopStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = null,
                TradeExecutor = null
            };
            _sm.TransitionTo<ShopState>();
            Assert.AreEqual(3, _ctx.CurrentRound);
            Assert.AreEqual(2, _ctx.CurrentAct);
        }

        [Test]
        public void Enter_WhenRunComplete_TransitionsToRunSummaryState()
        {
            _ctx.CurrentRound = 8; // After round 8, run is complete
            _ctx.CurrentAct = 4;
            ShopState.NextConfig = new ShopStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = null,
                TradeExecutor = null
            };
            _sm.TransitionTo<ShopState>();
            Assert.IsInstanceOf<RunSummaryState>(_sm.CurrentState);
        }

        [Test]
        public void Enter_WhenRunComplete_RunSummaryShowsWin()
        {
            _ctx.CurrentRound = 8;
            _ctx.CurrentAct = 4;
            ShopState.NextConfig = new ShopStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = null,
                TradeExecutor = null
            };
            _sm.TransitionTo<ShopState>();
            Assert.IsFalse(RunSummaryState.WasMarginCalled, "Win path should not be margin called");
        }

        [Test]
        public void Enter_WhenNotRunComplete_TransitionsToMarketOpenState()
        {
            _ctx.CurrentRound = 5;
            _ctx.CurrentAct = 3;
            ShopState.NextConfig = new ShopStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = null,
                TradeExecutor = null
            };
            _sm.TransitionTo<ShopState>();
            Assert.IsInstanceOf<MarketOpenState>(_sm.CurrentState);
        }

        // --- ActTransitionEvent Tests (Story 4.5 Code Review Fix) ---

        [Test]
        public void Enter_WhenActChanges_PublishesActTransitionEvent()
        {
            _ctx.CurrentRound = 2; // Advancing to round 3 = Act 2
            _ctx.CurrentAct = 1;

            ActTransitionEvent received = default;
            bool eventFired = false;
            EventBus.Subscribe<ActTransitionEvent>(e =>
            {
                received = e;
                eventFired = true;
            });

            ShopState.NextConfig = new ShopStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = null,
                TradeExecutor = null
            };
            _sm.TransitionTo<ShopState>();

            Assert.IsTrue(eventFired, "Should publish ActTransitionEvent when act changes");
            Assert.AreEqual(2, received.NewAct);
            Assert.AreEqual(1, received.PreviousAct);
            Assert.AreEqual("Low-Value Stocks", received.TierDisplayName);
        }

        [Test]
        public void Enter_WhenActDoesNotChange_DoesNotPublishActTransitionEvent()
        {
            _ctx.CurrentRound = 1; // Advancing to round 2, still Act 1
            _ctx.CurrentAct = 1;

            bool eventFired = false;
            EventBus.Subscribe<ActTransitionEvent>(e => eventFired = true);

            ShopState.NextConfig = new ShopStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = null,
                TradeExecutor = null
            };
            _sm.TransitionTo<ShopState>();

            Assert.IsFalse(eventFired, "Should NOT publish ActTransitionEvent when act stays the same");
        }
    }
}
