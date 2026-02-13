using NUnit.Framework;

namespace BullRun.Tests.Core.GameStates
{
    [TestFixture]
    public class TierTransitionStateTests
    {
        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            TierTransitionState.NextConfig = null;
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
            TierTransitionState.NextConfig = null;
        }

        [Test]
        public void Enter_PublishesActTransitionEvent()
        {
            var ctx = new RunContext(2, 3, new Portfolio(1000f));
            TierTransitionState.NextConfig = new TierTransitionStateConfig
            {
                PreviousAct = 1
            };
            var state = new TierTransitionState();

            ActTransitionEvent? received = null;
            EventBus.Subscribe<ActTransitionEvent>(evt => received = evt);

            state.Enter(ctx);

            Assert.IsNotNull(received);
            Assert.AreEqual(2, received.Value.NewAct);
            Assert.AreEqual(1, received.Value.PreviousAct);
            Assert.AreEqual("Low-Value Stocks", received.Value.TierDisplayName);
        }

        [Test]
        public void Enter_PublishesCorrectTierDisplayName_Act3()
        {
            var ctx = new RunContext(3, 5, new Portfolio(1000f));
            TierTransitionState.NextConfig = new TierTransitionStateConfig
            {
                PreviousAct = 2
            };
            var state = new TierTransitionState();

            ActTransitionEvent? received = null;
            EventBus.Subscribe<ActTransitionEvent>(evt => received = evt);

            state.Enter(ctx);

            Assert.AreEqual("Mid-Value Stocks", received.Value.TierDisplayName);
        }

        [Test]
        public void Enter_PublishesCorrectTierDisplayName_Act4()
        {
            var ctx = new RunContext(4, 7, new Portfolio(1000f));
            TierTransitionState.NextConfig = new TierTransitionStateConfig
            {
                PreviousAct = 3
            };
            var state = new TierTransitionState();

            ActTransitionEvent? received = null;
            EventBus.Subscribe<ActTransitionEvent>(evt => received = evt);

            state.Enter(ctx);

            Assert.AreEqual("Blue Chips", received.Value.TierDisplayName);
        }
    }
}
