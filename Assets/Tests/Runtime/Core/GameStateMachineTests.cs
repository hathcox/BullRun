using NUnit.Framework;

namespace BullRun.Tests.Core
{
    public class TestState : IGameState
    {
        public bool EnterCalled;
        public bool UpdateCalled;
        public bool ExitCalled;
        public RunContext LastCtx;

        public void Enter(RunContext ctx) { EnterCalled = true; LastCtx = ctx; }
        public void Update(RunContext ctx) { UpdateCalled = true; LastCtx = ctx; }
        public void Exit(RunContext ctx) { ExitCalled = true; LastCtx = ctx; }
    }

    public class AnotherTestState : IGameState
    {
        public bool EnterCalled;

        public void Enter(RunContext ctx) { EnterCalled = true; }
        public void Update(RunContext ctx) { }
        public void Exit(RunContext ctx) { }
    }

    [TestFixture]
    public class GameStateMachineTests
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

        [Test]
        public void TransitionTo_CallsEnterOnNewState()
        {
            _sm.TransitionTo<TestState>();

            var state = (TestState)_sm.CurrentState;
            Assert.IsTrue(state.EnterCalled);
            Assert.AreEqual(_ctx, state.LastCtx);
        }

        [Test]
        public void TransitionTo_CallsExitOnPreviousState()
        {
            _sm.TransitionTo<TestState>();
            var firstState = (TestState)_sm.CurrentState;

            _sm.TransitionTo<AnotherTestState>();

            Assert.IsTrue(firstState.ExitCalled);
        }

        [Test]
        public void Update_CallsUpdateOnCurrentState()
        {
            _sm.TransitionTo<TestState>();
            var state = (TestState)_sm.CurrentState;

            _sm.Update();

            Assert.IsTrue(state.UpdateCalled);
        }

        [Test]
        public void Update_WithNoState_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _sm.Update());
        }

        [Test]
        public void TransitionTo_PassesRunContext()
        {
            _sm.TransitionTo<TestState>();
            var state = (TestState)_sm.CurrentState;

            Assert.AreEqual(_ctx, state.LastCtx);
        }

        [Test]
        public void TransitionTo_ReplacesCurrentState()
        {
            _sm.TransitionTo<TestState>();
            Assert.IsInstanceOf<TestState>(_sm.CurrentState);

            _sm.TransitionTo<AnotherTestState>();
            Assert.IsInstanceOf<AnotherTestState>(_sm.CurrentState);
        }

        [Test]
        public void CurrentState_IsNullBeforeFirstTransition()
        {
            Assert.IsNull(_sm.CurrentState);
        }
    }
}
