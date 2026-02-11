using NUnit.Framework;

namespace BullRun.Tests.Core.GameStates
{
    [TestFixture]
    public class MetaHubStateTests
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
            EventBus.Clear();
        }

        [Test]
        public void Enter_DoesNotThrow()
        {
            var state = new MetaHubState();
            Assert.DoesNotThrow(() => state.Enter(_ctx));
        }

        [Test]
        public void ImplementsIGameState()
        {
            var state = new MetaHubState();
            Assert.IsInstanceOf<IGameState>(state);
        }
    }
}
