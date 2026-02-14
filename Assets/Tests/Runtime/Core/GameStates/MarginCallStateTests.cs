using NUnit.Framework;

namespace BullRun.Tests.Core.GameStates
{
    [TestFixture]
    public class MarginCallStateTests
    {
        private RunContext _ctx;
        private GameStateMachine _sm;
        private MarginCallState _state;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();

            // Reset god mode so margin call checks are not bypassed
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            var godModeProp = typeof(DebugManager).GetProperty("IsGodMode",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            godModeProp?.GetSetMethod(true)?.Invoke(null, new object[] { false });
            #endif

            _ctx = new RunContext(1, 1, new Portfolio(1000f));
            _sm = new GameStateMachine(_ctx);

            MarginCallState.NextConfig = new MarginCallStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = null,
                TradeExecutor = null
            };

            _state = new MarginCallState();
        }

        [TearDown]
        public void TearDown()
        {
            MarginCallState.NextConfig = null;
            EventBus.Clear();
        }

        // --- AC1: After market close, round profit is compared against target ---

        [Test]
        public void Enter_WithZeroProfit_TriggersMarginCall()
        {
            // Round 1 target is $200. Default RoundProfit is 0 -> margin call
            MarginCallTriggeredEvent received = default;
            bool eventFired = false;
            EventBus.Subscribe<MarginCallTriggeredEvent>(e =>
            {
                received = e;
                eventFired = true;
            });

            MarketCloseState.RoundProfit = 0f;
            _state.Enter(_ctx);

            Assert.IsTrue(eventFired, "Margin call should trigger when profit is 0 (below $200 target)");
            Assert.AreEqual(1, received.RoundNumber);
            Assert.AreEqual(0f, received.RoundProfit, 0.01f);
            Assert.AreEqual(200f, received.RequiredTarget, 0.01f);
        }

        // --- AC2: If round profit >= target, proceed to shop ---

        [Test]
        public void Enter_WhenProfitMeetsTarget_TransitionsToShopState()
        {
            // Set round profit above target ($200 for round 1)
            MarketCloseState.RoundProfit = 250f;

            MarginCallState.NextConfig = new MarginCallStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = null,
                TradeExecutor = null
            };
            _sm.TransitionTo<MarginCallState>();

            // ShopState now waits for timer — no longer auto-skips
            Assert.IsInstanceOf<ShopState>(_sm.CurrentState);
        }

        [Test]
        public void Enter_WhenProfitExactlyMeetsTarget_TransitionsToShopState()
        {
            float target = MarginCallTargets.GetTarget(1); // $200
            MarketCloseState.RoundProfit = target;

            MarginCallState.NextConfig = new MarginCallStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = null,
                TradeExecutor = null
            };
            _sm.TransitionTo<MarginCallState>();

            // Exact target should pass — ShopState now waits for timer
            Assert.IsInstanceOf<ShopState>(_sm.CurrentState);
        }

        // --- AC3: If round profit < target, MARGIN CALL triggered ---

        [Test]
        public void Enter_WhenProfitBelowTarget_TransitionsToRunSummaryState()
        {
            MarketCloseState.RoundProfit = 30f; // Below $200 target

            MarginCallState.NextConfig = new MarginCallStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = null,
                TradeExecutor = null
            };
            _sm.TransitionTo<MarginCallState>();

            Assert.IsInstanceOf<RunSummaryState>(_sm.CurrentState);
        }

        [Test]
        public void Enter_WhenProfitBelowTarget_PublishesMarginCallTriggeredEvent()
        {
            MarginCallTriggeredEvent received = default;
            bool eventFired = false;
            EventBus.Subscribe<MarginCallTriggeredEvent>(e =>
            {
                received = e;
                eventFired = true;
            });

            MarketCloseState.RoundProfit = 30f; // Below $200 target

            _state.Enter(_ctx);

            Assert.IsTrue(eventFired);
            Assert.AreEqual(1, received.RoundNumber);
            Assert.AreEqual(30f, received.RoundProfit, 0.01f);
            Assert.AreEqual(200f, received.RequiredTarget, 0.01f);
            Assert.AreEqual(170f, received.Shortfall, 0.01f);
        }

        [Test]
        public void Enter_WhenProfitMeetsTarget_DoesNotPublishMarginCallEvent()
        {
            bool eventFired = false;
            EventBus.Subscribe<MarginCallTriggeredEvent>(e => eventFired = true);

            MarketCloseState.RoundProfit = 300f; // Above $200 target

            _state.Enter(_ctx);

            Assert.IsFalse(eventFired);
        }

        // --- AC4: Uses escalating targets per round ---

        [Test]
        public void Enter_UsesCorrectTargetForRound()
        {
            var ctx = new RunContext(1, 3, new Portfolio(1000f)); // Round 3
            MarginCallTriggeredEvent received = default;
            EventBus.Subscribe<MarginCallTriggeredEvent>(e => received = e);

            MarketCloseState.RoundProfit = 500f; // Below $600 target for round 3

            MarginCallState.NextConfig = new MarginCallStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = null,
                TradeExecutor = null
            };

            _state = new MarginCallState();
            _state.Enter(ctx);

            Assert.AreEqual(600f, received.RequiredTarget, 0.01f);
        }

        [Test]
        public void Enter_WhenNegativeProfit_TriggersMarginCall()
        {
            MarginCallTriggeredEvent received = default;
            bool eventFired = false;
            EventBus.Subscribe<MarginCallTriggeredEvent>(e =>
            {
                received = e;
                eventFired = true;
            });

            MarketCloseState.RoundProfit = -100f;

            _state.Enter(_ctx);

            Assert.IsTrue(eventFired);
            Assert.AreEqual(-100f, received.RoundProfit, 0.01f);
            Assert.AreEqual(300f, received.Shortfall, 0.01f); // 200 - (-100) = 300
        }

        // --- RoundCompletedEvent Tests (Story 4.5 Task 3) ---

        [Test]
        public void Enter_WhenProfitMeetsTarget_PublishesRoundCompletedEvent()
        {
            RoundCompletedEvent received = default;
            bool eventFired = false;
            EventBus.Subscribe<RoundCompletedEvent>(e =>
            {
                received = e;
                eventFired = true;
            });

            MarketCloseState.RoundProfit = 300f; // Above $200 target for round 1

            _state.Enter(_ctx);

            Assert.IsTrue(eventFired, "Should publish RoundCompletedEvent when margin call passes");
            Assert.AreEqual(1, received.RoundNumber);
            Assert.AreEqual(300f, received.RoundProfit, 0.01f);
            Assert.AreEqual(200f, received.ProfitTarget, 0.01f);
            Assert.IsTrue(received.TargetMet);
        }

        [Test]
        public void Enter_WhenProfitBelowTarget_DoesNotPublishRoundCompletedEvent()
        {
            bool eventFired = false;
            EventBus.Subscribe<RoundCompletedEvent>(e => eventFired = true);

            MarketCloseState.RoundProfit = 30f; // Below $200 target

            _state.Enter(_ctx);

            Assert.IsFalse(eventFired, "Should NOT publish RoundCompletedEvent on margin call");
        }
        // --- Victory Detection Tests (Story 6.5 Task 1) ---

        [Test]
        public void Enter_Round8Passes_SetsRunCompletedTrue()
        {
            var ctx = new RunContext(4, 8, new Portfolio(5000f)); // Round 8, Act 4
            MarketCloseState.RoundProfit = 6000f; // Well above any target

            MarginCallState.NextConfig = new MarginCallStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = null,
                TradeExecutor = null
            };

            _state = new MarginCallState();
            _state.Enter(ctx);

            Assert.IsTrue(ctx.RunCompleted, "RunCompleted should be true after round 8 margin call passes");
        }

        [Test]
        public void Enter_Round1Passes_DoesNotSetRunCompleted()
        {
            MarketCloseState.RoundProfit = 300f; // Above $200 target for round 1

            _state.Enter(_ctx);

            Assert.IsFalse(_ctx.RunCompleted, "RunCompleted should NOT be set for non-final rounds");
        }

        [Test]
        public void Enter_Round8Fails_DoesNotSetRunCompleted()
        {
            var ctx = new RunContext(4, 8, new Portfolio(5000f));
            MarketCloseState.RoundProfit = 0f; // Below target — margin call

            MarginCallState.NextConfig = new MarginCallStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = null,
                TradeExecutor = null
            };

            _state = new MarginCallState();
            _state.Enter(ctx);

            Assert.IsFalse(ctx.RunCompleted, "RunCompleted should NOT be set when round 8 margin call fails");
        }
    }
}
