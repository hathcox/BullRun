using NUnit.Framework;

namespace BullRun.Tests.Core.GameStates
{
    [TestFixture]
    public class RunSummaryStateTests
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
            RunSummaryState.NextConfig = null;
            EventBus.Clear();
        }

        [Test]
        public void ImplementsIGameState()
        {
            var state = new RunSummaryState();
            Assert.IsInstanceOf<IGameState>(state);
        }

        [Test]
        public void Enter_PublishesRunEndedEvent_WhenMarginCalled()
        {
            RunEndedEvent received = default;
            bool eventFired = false;
            EventBus.Subscribe<RunEndedEvent>(e =>
            {
                received = e;
                eventFired = true;
            });

            RunSummaryState.NextConfig = new RunSummaryStateConfig
            {
                WasMarginCalled = true,
                RoundProfit = 50f,
                RequiredTarget = 200f,
                StateMachine = _sm
            };

            var state = new RunSummaryState();
            state.Enter(_ctx);

            Assert.IsTrue(eventFired);
            Assert.AreEqual(1, received.RoundsCompleted);
            Assert.AreEqual(_ctx.Portfolio.Cash, received.FinalCash, 0.01f);
            Assert.IsTrue(received.WasMarginCalled);
        }

        [Test]
        public void Enter_PublishesRunEndedEvent_WithCorrectRoundsCompleted()
        {
            var ctx = new RunContext(1, 5, new Portfolio(3000f));
            RunEndedEvent received = default;
            EventBus.Subscribe<RunEndedEvent>(e => received = e);

            RunSummaryState.NextConfig = new RunSummaryStateConfig
            {
                WasMarginCalled = true,
                RoundProfit = 100f,
                RequiredTarget = 1500f,
                StateMachine = _sm
            };

            var state = new RunSummaryState();
            state.Enter(ctx);

            Assert.AreEqual(5, received.RoundsCompleted);
        }

        [Test]
        public void Enter_SetsStaticAccessors()
        {
            RunSummaryState.NextConfig = new RunSummaryStateConfig
            {
                WasMarginCalled = true,
                RoundProfit = 50f,
                RequiredTarget = 200f,
                StateMachine = _sm
            };

            var state = new RunSummaryState();
            state.Enter(_ctx);

            Assert.IsTrue(RunSummaryState.IsActive);
            Assert.IsTrue(RunSummaryState.WasMarginCalled);
            Assert.AreEqual(1, RunSummaryState.RoundsCompleted);
            Assert.AreEqual(_ctx.Portfolio.Cash, RunSummaryState.FinalCash, 0.01f);
            Assert.AreEqual(50f, RunSummaryState.RoundProfit, 0.01f);
            Assert.AreEqual(200f, RunSummaryState.RequiredTarget, 0.01f);
        }

        [Test]
        public void Enter_WithNonMarginCall_SetsWasMarginCalledFalse()
        {
            RunSummaryState.NextConfig = new RunSummaryStateConfig
            {
                WasMarginCalled = false,
                RoundProfit = 5000f,
                RequiredTarget = 5000f,
                StateMachine = _sm
            };

            RunEndedEvent received = default;
            EventBus.Subscribe<RunEndedEvent>(e => received = e);

            var state = new RunSummaryState();
            state.Enter(_ctx);

            Assert.IsFalse(received.WasMarginCalled);
            Assert.IsFalse(RunSummaryState.WasMarginCalled);
        }

        [Test]
        public void Exit_SetsIsActiveFalse()
        {
            RunSummaryState.NextConfig = new RunSummaryStateConfig
            {
                WasMarginCalled = true,
                RoundProfit = 50f,
                RequiredTarget = 200f,
                StateMachine = _sm
            };

            var state = new RunSummaryState();
            state.Enter(_ctx);
            Assert.IsTrue(RunSummaryState.IsActive);

            state.Exit(_ctx);
            Assert.IsFalse(RunSummaryState.IsActive);
        }

        [Test]
        public void Enter_CalculatesTotalProfit()
        {
            // Starting capital is 1000, current cash is 1000 (no trades)
            // TotalProfit = FinalCash - StartingCapital = 0
            RunEndedEvent received = default;
            EventBus.Subscribe<RunEndedEvent>(e => received = e);

            RunSummaryState.NextConfig = new RunSummaryStateConfig
            {
                WasMarginCalled = true,
                RoundProfit = 50f,
                RequiredTarget = 200f,
                StateMachine = _sm
            };

            var state = new RunSummaryState();
            state.Enter(_ctx);

            Assert.AreEqual(0f, received.TotalProfit, 0.01f);
        }

        [Test]
        public void Enter_ReputationReflectsAccumulatedPerRoundRep()
        {
            // FIX-14: Rep is now accumulated per-round in MarginCallState, not calculated at end.
            // ctx.ReputationEarned starts at 0 if no rounds were processed.
            RunEndedEvent received = default;
            EventBus.Subscribe<RunEndedEvent>(e => received = e);

            RunSummaryState.NextConfig = new RunSummaryStateConfig
            {
                WasMarginCalled = true,
                RoundProfit = 50f,
                RequiredTarget = 200f,
                StateMachine = _sm
            };

            var state = new RunSummaryState();
            state.Enter(_ctx);

            // No rounds processed through MarginCallState → 0 accumulated Rep
            Assert.AreEqual(0, received.ReputationEarned);
        }

        [Test]
        public void Enter_ItemsCollectedReflectsOwnedRelics()
        {
            _ctx.OwnedRelics.Add("item1");
            _ctx.OwnedRelics.Add("item2");

            RunEndedEvent received = default;
            EventBus.Subscribe<RunEndedEvent>(e => received = e);

            RunSummaryState.NextConfig = new RunSummaryStateConfig
            {
                WasMarginCalled = true,
                RoundProfit = 50f,
                RequiredTarget = 200f,
                StateMachine = _sm
            };

            var state = new RunSummaryState();
            state.Enter(_ctx);

            Assert.AreEqual(2, received.ItemsCollected);
            Assert.AreEqual(2, RunSummaryState.ItemsCollected);
        }

        [Test]
        public void Enter_UsesRunContextStartingCapital_NotHardcoded()
        {
            // Create context with non-default starting capital
            var ctx = new RunContext(1, 1, new Portfolio(2000f));

            RunEndedEvent received = default;
            EventBus.Subscribe<RunEndedEvent>(e => received = e);

            RunSummaryState.NextConfig = new RunSummaryStateConfig
            {
                WasMarginCalled = true,
                RoundProfit = 50f,
                RequiredTarget = 200f,
                StateMachine = _sm
            };

            var state = new RunSummaryState();
            state.Enter(ctx);

            // TotalProfit = 2000 - 2000 = 0 (no trades, so cash == starting capital)
            Assert.AreEqual(0f, received.TotalProfit, 0.01f);
        }

        // --- FIX-14: Reputation is now accumulated per-round, not calculated at end ---

        [Test]
        public void Enter_VictoryPath_UsesAccumulatedRep()
        {
            // FIX-14: Rep earned per-round. Simulate accumulated Rep.
            var ctx = new RunContext(4, 9, new Portfolio(5000f));
            ctx.StartingCapital = 1000f;
            ctx.RunCompleted = true;
            ctx.ReputationEarned = 158; // Simulated accumulated per-round Rep

            RunEndedEvent received = default;
            EventBus.Subscribe<RunEndedEvent>(e => received = e);

            RunSummaryState.NextConfig = new RunSummaryStateConfig
            {
                WasMarginCalled = false,
                RoundProfit = 0f,
                RequiredTarget = 0f,
                StateMachine = _sm
            };

            var state = new RunSummaryState();
            state.Enter(ctx);

            Assert.AreEqual(158, received.ReputationEarned, "Should use pre-accumulated Rep, not recalculate");
        }

        [Test]
        public void Enter_LossPath_UsesAccumulatedRep()
        {
            // FIX-14: Rep earned per-round. Consolation awarded in MarginCallState.
            var ctx = new RunContext(2, 3, new Portfolio(800f));
            ctx.ReputationEarned = 17; // Simulated: 2 rounds of base Rep + consolation

            RunEndedEvent received = default;
            EventBus.Subscribe<RunEndedEvent>(e => received = e);

            RunSummaryState.NextConfig = new RunSummaryStateConfig
            {
                WasMarginCalled = true,
                RoundProfit = 50f,
                RequiredTarget = 600f,
                StateMachine = _sm
            };

            var state = new RunSummaryState();
            state.Enter(ctx);

            Assert.AreEqual(17, received.ReputationEarned, "Should use pre-accumulated Rep");
        }

        [Test]
        public void Enter_NoRoundsCompleted_ZeroRep()
        {
            // FIX-14: No rounds processed → 0 Rep
            RunEndedEvent received = default;
            EventBus.Subscribe<RunEndedEvent>(e => received = e);

            RunSummaryState.NextConfig = new RunSummaryStateConfig
            {
                WasMarginCalled = true,
                RoundProfit = 0f,
                RequiredTarget = 20f,
                StateMachine = _sm
            };

            var state = new RunSummaryState();
            state.Enter(_ctx);

            Assert.AreEqual(0, received.ReputationEarned);
        }

        [Test]
        public void Enter_StoresReputationInStaticAccessor()
        {
            var ctx = new RunContext(2, 3, new Portfolio(800f));
            ctx.ReputationEarned = 42; // Pre-accumulated

            RunSummaryState.NextConfig = new RunSummaryStateConfig
            {
                WasMarginCalled = true,
                RoundProfit = 50f,
                RequiredTarget = 600f,
                StateMachine = _sm
            };

            var state = new RunSummaryState();
            state.Enter(ctx);

            Assert.AreEqual(42, RunSummaryState.ReputationEarned, "Static accessor should match accumulated reputation");
        }

        // --- Win State Tests (Story 4.5 Task 5) ---

        [Test]
        public void Enter_WinPath_SetsIsRunCompleteTrue()
        {
            var ctx = new RunContext(4, 9, new Portfolio(5000f));
            ctx.RunCompleted = true;
            RunSummaryState.NextConfig = new RunSummaryStateConfig
            {
                WasMarginCalled = false,
                RoundProfit = 0f,
                RequiredTarget = 0f,
                StateMachine = _sm
            };

            var state = new RunSummaryState();
            state.Enter(ctx);

            Assert.IsTrue(RunSummaryState.IsVictory);
        }

        [Test]
        public void Enter_MarginCallPath_SetsIsRunCompleteFalse()
        {
            RunSummaryState.NextConfig = new RunSummaryStateConfig
            {
                WasMarginCalled = true,
                RoundProfit = 50f,
                RequiredTarget = 200f,
                StateMachine = _sm
            };

            var state = new RunSummaryState();
            state.Enter(_ctx);

            Assert.IsFalse(RunSummaryState.IsVictory);
        }

        // --- RunEndedEvent IsVictory Tests (Story 6.5 Task 6) ---

        [Test]
        public void Enter_Victory_RunEndedEventHasIsVictoryTrue()
        {
            var ctx = new RunContext(4, 9, new Portfolio(5000f));
            ctx.StartingCapital = 1000f;
            ctx.RunCompleted = true;
            ctx.PeakCash = 6000f;
            ctx.BestRoundProfit = 1200f;
            ctx.ReputationEarned = 158; // FIX-14: Pre-accumulated per-round Rep
            ctx.OwnedRelics.Add("item1");
            ctx.OwnedRelics.Add("item2");

            RunEndedEvent received = default;
            bool eventFired = false;
            EventBus.Subscribe<RunEndedEvent>(e =>
            {
                received = e;
                eventFired = true;
            });

            RunSummaryState.NextConfig = new RunSummaryStateConfig
            {
                WasMarginCalled = false,
                RoundProfit = 0f,
                RequiredTarget = 0f,
                StateMachine = _sm
            };

            var state = new RunSummaryState();
            state.Enter(ctx);

            Assert.IsTrue(eventFired, "Should publish RunEndedEvent on enter");
            Assert.IsTrue(received.IsVictory);
            Assert.AreEqual(4000f, received.TotalProfit, 0.01f);
            Assert.AreEqual(6000f, received.PeakCash, 0.01f);
            Assert.AreEqual(9, received.RoundsCompleted);
            Assert.AreEqual(2, received.ItemsCollected);
            Assert.AreEqual(158, received.ReputationEarned);
        }

        [Test]
        public void Enter_MarginCall_RunEndedEventHasIsVictoryFalse()
        {
            RunEndedEvent received = default;
            bool eventFired = false;
            EventBus.Subscribe<RunEndedEvent>(e =>
            {
                received = e;
                eventFired = true;
            });

            RunSummaryState.NextConfig = new RunSummaryStateConfig
            {
                WasMarginCalled = true,
                RoundProfit = 50f,
                RequiredTarget = 200f,
                StateMachine = _sm
            };

            var state = new RunSummaryState();
            state.Enter(_ctx);

            Assert.IsTrue(eventFired, "Should publish RunEndedEvent on loss");
            Assert.IsFalse(received.IsVictory);
        }

        // --- FIX-14: Negative-profit victory edge case ---

        [Test]
        public void Enter_VictoryWithNegativeProfit_UsesAccumulatedRep()
        {
            // FIX-14: Rep is accumulated per-round. Even with negative total profit,
            // the per-round Rep was already awarded in MarginCallState.
            var ctx = new RunContext(4, 9, new Portfolio(800f));
            ctx.StartingCapital = 1000f; // Profit = 800 - 1000 = -200
            ctx.RunCompleted = true;
            ctx.ReputationEarned = 158; // Per-round accumulated

            RunEndedEvent received = default;
            EventBus.Subscribe<RunEndedEvent>(e => received = e);

            RunSummaryState.NextConfig = new RunSummaryStateConfig
            {
                WasMarginCalled = false,
                RoundProfit = 0f,
                RequiredTarget = 0f,
                StateMachine = _sm
            };

            var state = new RunSummaryState();
            state.Enter(ctx);

            Assert.AreEqual(158, received.ReputationEarned, "Should use accumulated per-round Rep regardless of total profit");
        }

        // --- Input handling tests ---

        [Test]
        public void Enter_InputNotEnabledImmediately()
        {
            RunSummaryState.NextConfig = new RunSummaryStateConfig
            {
                WasMarginCalled = true,
                RoundProfit = 50f,
                RequiredTarget = 200f,
                StateMachine = _sm
            };

            var state = new RunSummaryState();
            state.Enter(_ctx);

            Assert.IsFalse(state.IsInputEnabled);
        }

        [Test]
        public void AdvanceTime_AfterDelay_EnablesInput()
        {
            RunSummaryState.NextConfig = new RunSummaryStateConfig
            {
                WasMarginCalled = true,
                RoundProfit = 50f,
                RequiredTarget = 200f,
                StateMachine = _sm
            };

            var state = new RunSummaryState();
            state.Enter(_ctx);

            state.AdvanceTime(_ctx, RunSummaryState.InputDelaySeconds + 0.1f);

            Assert.IsTrue(state.IsInputEnabled);
        }

        [Test]
        public void AdvanceTime_BeforeDelay_InputStaysDisabled()
        {
            RunSummaryState.NextConfig = new RunSummaryStateConfig
            {
                WasMarginCalled = true,
                RoundProfit = 50f,
                RequiredTarget = 200f,
                StateMachine = _sm
            };

            var state = new RunSummaryState();
            state.Enter(_ctx);

            state.AdvanceTime(_ctx, RunSummaryState.InputDelaySeconds - 0.1f);

            Assert.IsFalse(state.IsInputEnabled);
        }
    }
}
