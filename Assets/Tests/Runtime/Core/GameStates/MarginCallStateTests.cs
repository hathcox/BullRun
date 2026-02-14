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

            // FIX-14: Start with $10 (new economy). Round 1 target is $20 (value target).
            _ctx = new RunContext(1, 1, new Portfolio(10f));
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

        // --- FIX-14: Margin call now compares total cash against value target ---

        [Test]
        public void Enter_WithCashBelowTarget_TriggersMarginCall()
        {
            // Round 1 target is $20. Portfolio cash $10 < $20 -> margin call
            MarginCallTriggeredEvent received = default;
            bool eventFired = false;
            EventBus.Subscribe<MarginCallTriggeredEvent>(e =>
            {
                received = e;
                eventFired = true;
            });

            MarketCloseState.RoundProfit = 0f;
            _state.Enter(_ctx);

            Assert.IsTrue(eventFired, "Margin call should trigger when cash ($10) is below target ($20)");
            Assert.AreEqual(1, received.RoundNumber);
            Assert.AreEqual(20f, received.RequiredTarget, 0.01f);
        }

        // --- FIX-14: If total cash >= target, proceed to shop ---

        [Test]
        public void Enter_WhenCashMeetsTarget_TransitionsToShopState()
        {
            // Give portfolio enough cash to meet $20 target
            var ctx = new RunContext(1, 1, new Portfolio(25f));
            MarketCloseState.RoundProfit = 15f;

            MarginCallState.NextConfig = new MarginCallStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = null,
                TradeExecutor = null
            };
            var state = new MarginCallState();
            state.Enter(ctx);

            Assert.IsInstanceOf<ShopState>(_sm.CurrentState);
        }

        [Test]
        public void Enter_WhenCashExactlyMeetsTarget_TransitionsToShopState()
        {
            float target = MarginCallTargets.GetTarget(1); // $20
            var ctx = new RunContext(1, 1, new Portfolio(target));
            MarketCloseState.RoundProfit = target - 10f;

            MarginCallState.NextConfig = new MarginCallStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = null,
                TradeExecutor = null
            };
            var state = new MarginCallState();
            state.Enter(ctx);

            Assert.IsInstanceOf<ShopState>(_sm.CurrentState);
        }

        // --- FIX-14: If total cash < target, MARGIN CALL triggered ---

        [Test]
        public void Enter_WhenCashBelowTarget_TransitionsToRunSummaryState()
        {
            // Cash $10 < $20 target
            MarketCloseState.RoundProfit = 0f;

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
        public void Enter_WhenCashBelowTarget_PublishesMarginCallTriggeredEvent()
        {
            MarginCallTriggeredEvent received = default;
            bool eventFired = false;
            EventBus.Subscribe<MarginCallTriggeredEvent>(e =>
            {
                received = e;
                eventFired = true;
            });

            MarketCloseState.RoundProfit = 0f; // Cash stays at $10, below $20 target
            _state.Enter(_ctx);

            Assert.IsTrue(eventFired);
            Assert.AreEqual(1, received.RoundNumber);
            Assert.AreEqual(20f, received.RequiredTarget, 0.01f);
            Assert.AreEqual(10f, received.Shortfall, 0.01f); // $20 - $10 = $10 shortfall
        }

        [Test]
        public void Enter_WhenCashMeetsTarget_DoesNotPublishMarginCallEvent()
        {
            bool eventFired = false;
            EventBus.Subscribe<MarginCallTriggeredEvent>(e => eventFired = true);

            var ctx = new RunContext(1, 1, new Portfolio(30f)); // $30 > $20 target
            MarketCloseState.RoundProfit = 20f;

            MarginCallState.NextConfig = new MarginCallStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = null,
                TradeExecutor = null
            };
            var state = new MarginCallState();
            state.Enter(ctx);

            Assert.IsFalse(eventFired);
        }

        // --- Uses correct escalating targets per round ---

        [Test]
        public void Enter_UsesCorrectTargetForRound()
        {
            // Round 3 target is $60
            var ctx = new RunContext(1, 3, new Portfolio(50f)); // $50 < $60 target
            MarginCallTriggeredEvent received = default;
            EventBus.Subscribe<MarginCallTriggeredEvent>(e => received = e);

            MarketCloseState.RoundProfit = 10f;

            MarginCallState.NextConfig = new MarginCallStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = null,
                TradeExecutor = null
            };

            _state = new MarginCallState();
            _state.Enter(ctx);

            Assert.AreEqual(60f, received.RequiredTarget, 0.01f);
        }

        // --- RoundCompletedEvent Tests ---

        [Test]
        public void Enter_WhenCashMeetsTarget_PublishesRoundCompletedEvent()
        {
            RoundCompletedEvent received = default;
            bool eventFired = false;
            EventBus.Subscribe<RoundCompletedEvent>(e =>
            {
                received = e;
                eventFired = true;
            });

            var ctx = new RunContext(1, 1, new Portfolio(30f)); // $30 > $20 target
            MarketCloseState.RoundProfit = 20f;

            MarginCallState.NextConfig = new MarginCallStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = null,
                TradeExecutor = null
            };
            var state = new MarginCallState();
            state.Enter(ctx);

            Assert.IsTrue(eventFired, "Should publish RoundCompletedEvent when margin call passes");
            Assert.AreEqual(1, received.RoundNumber);
            Assert.AreEqual(20f, received.RoundProfit, 0.01f);
            Assert.AreEqual(20f, received.ProfitTarget, 0.01f);
            Assert.IsTrue(received.TargetMet);
            Assert.AreEqual(30f, received.TotalCash, 0.01f);
        }

        [Test]
        public void Enter_WhenCashBelowTarget_DoesNotPublishRoundCompletedEvent()
        {
            bool eventFired = false;
            EventBus.Subscribe<RoundCompletedEvent>(e => eventFired = true);

            MarketCloseState.RoundProfit = 0f; // Cash $10 < $20 target
            _state.Enter(_ctx);

            Assert.IsFalse(eventFired, "Should NOT publish RoundCompletedEvent on margin call");
        }

        // --- Victory Detection Tests ---

        [Test]
        public void Enter_Round8Passes_SetsRunCompletedTrue()
        {
            var ctx = new RunContext(4, 8, new Portfolio(1000f)); // $1000 > $800 target
            MarketCloseState.RoundProfit = 200f;

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
            var ctx = new RunContext(1, 1, new Portfolio(30f)); // $30 > $20 target
            MarketCloseState.RoundProfit = 20f;

            MarginCallState.NextConfig = new MarginCallStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = null,
                TradeExecutor = null
            };
            var state = new MarginCallState();
            state.Enter(ctx);

            Assert.IsFalse(ctx.RunCompleted, "RunCompleted should NOT be set for non-final rounds");
        }

        [Test]
        public void Enter_Round8Fails_DoesNotSetRunCompleted()
        {
            var ctx = new RunContext(4, 8, new Portfolio(500f)); // $500 < $800 target
            MarketCloseState.RoundProfit = 0f;

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

        // --- FIX-14: Reputation earning on round completion ---

        [Test]
        public void Enter_RoundPassed_AwardsBaseReputation()
        {
            var ctx = new RunContext(1, 1, new Portfolio(20f)); // Exactly meets $20 target
            MarketCloseState.RoundProfit = 10f;

            RoundCompletedEvent received = default;
            EventBus.Subscribe<RoundCompletedEvent>(e => received = e);

            MarginCallState.NextConfig = new MarginCallStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = null,
                TradeExecutor = null
            };
            var state = new MarginCallState();
            state.Enter(ctx);

            // Round 1 base = 5 Rep, exactly met target = 0 bonus
            Assert.AreEqual(5, received.RepEarned);
            Assert.AreEqual(5, received.BaseRep);
            Assert.AreEqual(0, received.BonusRep);
            Assert.AreEqual(5, ctx.Reputation.Current);
            Assert.AreEqual(5, ctx.ReputationEarned);
        }

        [Test]
        public void Enter_RoundPassed_AwardsBonusRepForExceedingTarget()
        {
            // Cash $30, target $20 → excess ratio = (30-20)/20 = 0.5
            // baseRep = 5, bonusRep = floor(5 * 0.5 * 0.5) = floor(1.25) = 1
            var ctx = new RunContext(1, 1, new Portfolio(30f));
            MarketCloseState.RoundProfit = 20f;

            RoundCompletedEvent received = default;
            EventBus.Subscribe<RoundCompletedEvent>(e => received = e);

            MarginCallState.NextConfig = new MarginCallStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = null,
                TradeExecutor = null
            };
            var state = new MarginCallState();
            state.Enter(ctx);

            Assert.AreEqual(6, received.RepEarned); // 5 base + 1 bonus
            Assert.AreEqual(5, received.BaseRep);
            Assert.AreEqual(1, received.BonusRep);
            Assert.AreEqual(6, ctx.Reputation.Current);
        }

        // --- FIX-14: Consolation Reputation on margin call failure ---

        [Test]
        public void Enter_MarginCallRound1_Awards0ConsolationRep()
        {
            // Round 1 failure: 0 rounds completed * 2 = 0 consolation
            MarketCloseState.RoundProfit = 0f;
            _state.Enter(_ctx);

            Assert.AreEqual(0, _ctx.ReputationEarned);
            Assert.AreEqual(0, _ctx.Reputation.Current);
        }

        [Test]
        public void Enter_MarginCallRound4_Awards6ConsolationRep()
        {
            // Round 4 failure: 3 rounds completed * 2 = 6 consolation Rep
            var ctx = new RunContext(2, 4, new Portfolio(50f)); // $50 < $100 target
            MarketCloseState.RoundProfit = 0f;

            MarginCallState.NextConfig = new MarginCallStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = null,
                TradeExecutor = null
            };
            var state = new MarginCallState();
            state.Enter(ctx);

            Assert.AreEqual(6, ctx.ReputationEarned); // 3 rounds * 2 Rep
            Assert.AreEqual(6, ctx.Reputation.Current);
        }

        // --- FIX-14: Rep accumulates across rounds ---

        [Test]
        public void RepAccumulates_AcrossMultipleRounds()
        {
            // Round 1: cash $25, target $20 → excess (25-20)/20=0.25
            // baseRep=5, bonus=floor(5*0.25*0.5)=floor(0.625)=0 → total 5
            var ctx = new RunContext(1, 1, new Portfolio(25f));
            MarketCloseState.RoundProfit = 15f;

            MarginCallState.NextConfig = new MarginCallStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = null,
                TradeExecutor = null
            };
            new MarginCallState().Enter(ctx);
            Assert.AreEqual(5, ctx.ReputationEarned);

            // Simulate advancing to round 2: cash $40, target $35
            ctx.CurrentRound = 2;
            ctx.Portfolio = new Portfolio(40f);
            MarketCloseState.RoundProfit = 15f;

            MarginCallState.NextConfig = new MarginCallStateConfig
            {
                StateMachine = _sm,
                PriceGenerator = null,
                TradeExecutor = null
            };
            new MarginCallState().Enter(ctx);

            // Round 2 base=8, excess=(40-35)/35≈0.143, bonus=floor(8*0.143*0.5)=0 → total 8
            Assert.AreEqual(13, ctx.ReputationEarned); // 5 + 8
            Assert.AreEqual(13, ctx.Reputation.Current);
        }
    }
}
