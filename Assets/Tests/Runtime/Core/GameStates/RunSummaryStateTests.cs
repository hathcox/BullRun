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
        public void Enter_ReputationEarnedCalculatedForLoss()
        {
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

            // Loss at round 1: 10 + (5 * 1) = 15
            Assert.AreEqual(15, received.ReputationEarned);
        }

        [Test]
        public void Enter_ItemsCollectedReflectsActiveItems()
        {
            _ctx.ActiveItems.Add("item1");
            _ctx.ActiveItems.Add("item2");

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

        // --- Reputation Calculation Tests (Story 6.5 Task 3) ---

        [Test]
        public void Enter_VictoryPath_CalculatesReputationWithProfitBonus()
        {
            // Win with $4000 total profit → 100 + floor(4000/100) = 140
            var ctx = new RunContext(4, 9, new Portfolio(5000f));
            ctx.StartingCapital = 1000f; // Profit = 5000 - 1000 = 4000
            ctx.RunCompleted = true; // Must be set for victory path

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

            Assert.AreEqual(140, received.ReputationEarned, "Win rep: 100 + floor(4000/100) = 140");
        }

        [Test]
        public void Enter_LossPath_CalculatesReputationFromRounds()
        {
            // Loss at round 3 → 10 + (5 * 3) = 25
            var ctx = new RunContext(2, 3, new Portfolio(800f));

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

            Assert.AreEqual(25, received.ReputationEarned, "Loss rep: 10 + (5 * 3) = 25");
        }

        [Test]
        public void Enter_VictoryWithSmallProfit_ReputationFloors()
        {
            // Win with $50 profit → 100 + floor(50/100) = 100
            var ctx = new RunContext(4, 9, new Portfolio(1050f));
            ctx.StartingCapital = 1000f;
            ctx.RunCompleted = true;

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

            Assert.AreEqual(100, received.ReputationEarned, "Win rep with small profit: 100 + 0 = 100");
        }

        [Test]
        public void Enter_StoresReputationInStaticAccessor()
        {
            var ctx = new RunContext(2, 3, new Portfolio(800f));

            RunSummaryState.NextConfig = new RunSummaryStateConfig
            {
                WasMarginCalled = true,
                RoundProfit = 50f,
                RequiredTarget = 600f,
                StateMachine = _sm
            };

            var state = new RunSummaryState();
            state.Enter(ctx);

            Assert.AreEqual(25, RunSummaryState.ReputationEarned, "Static accessor should match calculated reputation");
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
            ctx.ActiveItems.Add("item1");
            ctx.ActiveItems.Add("item2");

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

        // --- Negative-profit victory edge case (Story 6.5 Code Review) ---

        [Test]
        public void Enter_VictoryWithNegativeProfit_ReputationClampedToBase()
        {
            // Win but lose money (possible via debug jump with inflated StartingCapital)
            var ctx = new RunContext(4, 9, new Portfolio(800f));
            ctx.StartingCapital = 1000f; // Profit = 800 - 1000 = -200
            ctx.RunCompleted = true;

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

            // profitBonus = floor(-200/100) = -2, clamped to 0 → rep = 100
            Assert.AreEqual(100, received.ReputationEarned, "Negative profit bonus should clamp to 0, giving base 100 rep");
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
