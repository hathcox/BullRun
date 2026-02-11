using NUnit.Framework;

namespace BullRun.Tests.Core.GameStates
{
    [TestFixture]
    public class TradingStateTests
    {
        private RunContext _ctx;
        private TradingState _state;
        private GameStateMachine _sm;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            _ctx = new RunContext(1, 1, new Portfolio(1000f));
            _sm = new GameStateMachine(_ctx);

            // Configure TradingState with dependencies
            TradingState.NextConfig = new TradingStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = null // No price generator for unit tests
            };

            _state = new TradingState();
        }

        [TearDown]
        public void TearDown()
        {
            TradingState.NextConfig = null;
            EventBus.Clear();
        }

        // --- Enter tests ---

        [Test]
        public void Enter_SetsTimeRemainingToRoundDuration()
        {
            _state.Enter(_ctx);

            Assert.AreEqual(GameConfig.RoundDurationSeconds, _state.TimeRemaining);
        }

        [Test]
        public void Enter_SetsTimeElapsedToZero()
        {
            _state.Enter(_ctx);

            Assert.AreEqual(0f, _state.TimeElapsed);
        }

        [Test]
        public void Enter_PublishesRoundStartedEvent()
        {
            RoundStartedEvent receivedEvent = default;
            bool eventFired = false;
            EventBus.Subscribe<RoundStartedEvent>(e =>
            {
                eventFired = true;
                receivedEvent = e;
            });

            _state.Enter(_ctx);

            Assert.IsTrue(eventFired);
            Assert.AreEqual(_ctx.CurrentRound, receivedEvent.RoundNumber);
            Assert.AreEqual(_ctx.CurrentAct, receivedEvent.Act);
            Assert.AreEqual(GameConfig.RoundDurationSeconds, receivedEvent.TimeLimit);
        }

        [Test]
        public void Enter_PublishesRoundStartedEvent_WithMarginCallTarget()
        {
            RoundStartedEvent receivedEvent = default;
            EventBus.Subscribe<RoundStartedEvent>(e => receivedEvent = e);

            _state.Enter(_ctx);

            float expectedTarget = MarginCallTargets.GetTarget(_ctx.CurrentRound);
            Assert.AreEqual(expectedTarget, receivedEvent.MarginCallTarget, 0.01f);
        }

        [Test]
        public void RoundDuration_EqualsGameConfigValue()
        {
            _state.Enter(_ctx);

            Assert.AreEqual(GameConfig.RoundDurationSeconds, _state.RoundDuration);
        }

        [Test]
        public void TimeElapsed_IsComplementOfTimeRemaining()
        {
            _state.Enter(_ctx);

            float totalDuration = _state.RoundDuration;
            Assert.AreEqual(totalDuration, _state.TimeRemaining + _state.TimeElapsed, 0.001f);
        }

        [Test]
        public void Enter_SetsStaticAccessors()
        {
            _state.Enter(_ctx);

            Assert.AreEqual(GameConfig.RoundDurationSeconds, TradingState.ActiveTimeRemaining, 0.001f);
            Assert.AreEqual(GameConfig.RoundDurationSeconds, TradingState.ActiveRoundDuration, 0.001f);
            Assert.IsTrue(TradingState.IsActive);
        }

        // --- AdvanceTime / Update tests ---

        [Test]
        public void AdvanceTime_DecrementsTimeRemaining()
        {
            _state.Enter(_ctx);
            float initialTime = _state.TimeRemaining;

            _state.AdvanceTime(_ctx, 1.0f);

            Assert.AreEqual(initialTime - 1.0f, _state.TimeRemaining, 0.001f);
        }

        [Test]
        public void AdvanceTime_UpdatesStaticActiveTimeRemaining()
        {
            _state.Enter(_ctx);

            _state.AdvanceTime(_ctx, 10.0f);

            Assert.AreEqual(_state.TimeRemaining, TradingState.ActiveTimeRemaining, 0.001f);
        }

        [Test]
        public void AdvanceTime_WhenTimerExpires_PublishesTradingPhaseEndedEvent()
        {
            _state.Enter(_ctx);
            TradingPhaseEndedEvent receivedEvent = default;
            bool eventFired = false;
            EventBus.Subscribe<TradingPhaseEndedEvent>(e =>
            {
                eventFired = true;
                receivedEvent = e;
            });

            // Advance past the full duration
            _state.AdvanceTime(_ctx, GameConfig.RoundDurationSeconds + 1f);

            Assert.IsTrue(eventFired);
            Assert.AreEqual(_ctx.CurrentRound, receivedEvent.RoundNumber);
            Assert.IsTrue(receivedEvent.TimeExpired);
        }

        [Test]
        public void AdvanceTime_WhenTimerExpires_ClampsTimeRemainingToZero()
        {
            _state.Enter(_ctx);

            _state.AdvanceTime(_ctx, GameConfig.RoundDurationSeconds + 5f);

            Assert.AreEqual(0f, _state.TimeRemaining);
        }

        [Test]
        public void AdvanceTime_WhenTimerExpires_SetsIsActiveFalse()
        {
            _state.Enter(_ctx);

            _state.AdvanceTime(_ctx, GameConfig.RoundDurationSeconds + 1f);

            Assert.IsFalse(TradingState.IsActive);
        }

        [Test]
        public void AdvanceTime_WhenTimerExpires_TransitionsToMarketCloseState()
        {
            // Transition to TradingState via the state machine
            TradingState.NextConfig = new TradingStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = null
            };
            _sm.TransitionTo<TradingState>();
            var tradingState = (TradingState)_sm.CurrentState;

            tradingState.AdvanceTime(_ctx, GameConfig.RoundDurationSeconds + 1f);

            Assert.IsInstanceOf<MarketCloseState>(_sm.CurrentState);
        }

        [Test]
        public void AdvanceTime_BeforeExpiry_DoesNotPublishEndedEvent()
        {
            _state.Enter(_ctx);
            bool eventFired = false;
            EventBus.Subscribe<TradingPhaseEndedEvent>(e => eventFired = true);

            _state.AdvanceTime(_ctx, 10f);

            Assert.IsFalse(eventFired);
        }

        // --- Exit tests ---

        [Test]
        public void Exit_SetsIsActiveFalse()
        {
            _state.Enter(_ctx);
            Assert.IsTrue(TradingState.IsActive);

            _state.Exit(_ctx);

            Assert.IsFalse(TradingState.IsActive);
        }
    }
}
