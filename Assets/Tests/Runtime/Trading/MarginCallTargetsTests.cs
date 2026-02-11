using NUnit.Framework;

namespace BullRun.Tests.Trading
{
    [TestFixture]
    public class MarginCallTargetsTests
    {
        [Test]
        public void GetTarget_Round1_Returns200()
        {
            Assert.AreEqual(200f, MarginCallTargets.GetTarget(1), 0.01f);
        }

        [Test]
        public void GetTarget_Round2_Returns350()
        {
            Assert.AreEqual(350f, MarginCallTargets.GetTarget(2), 0.01f);
        }

        [Test]
        public void GetTarget_Round3_Returns600()
        {
            Assert.AreEqual(600f, MarginCallTargets.GetTarget(3), 0.01f);
        }

        [Test]
        public void GetTarget_Round4_Returns900()
        {
            Assert.AreEqual(900f, MarginCallTargets.GetTarget(4), 0.01f);
        }

        [Test]
        public void GetTarget_Round5_Returns1500()
        {
            Assert.AreEqual(1500f, MarginCallTargets.GetTarget(5), 0.01f);
        }

        [Test]
        public void GetTarget_Round6_Returns2200()
        {
            Assert.AreEqual(2200f, MarginCallTargets.GetTarget(6), 0.01f);
        }

        [Test]
        public void GetTarget_Round7_Returns3500()
        {
            Assert.AreEqual(3500f, MarginCallTargets.GetTarget(7), 0.01f);
        }

        [Test]
        public void GetTarget_Round8_Returns5000()
        {
            Assert.AreEqual(5000f, MarginCallTargets.GetTarget(8), 0.01f);
        }

        [Test]
        public void GetTarget_Round0_ReturnsFirstTarget()
        {
            Assert.AreEqual(200f, MarginCallTargets.GetTarget(0), 0.01f);
        }

        [Test]
        public void GetTarget_BeyondRound8_ReturnsLastTarget()
        {
            Assert.AreEqual(5000f, MarginCallTargets.GetTarget(9), 0.01f);
            Assert.AreEqual(5000f, MarginCallTargets.GetTarget(20), 0.01f);
        }

        [Test]
        public void TotalRounds_Returns8()
        {
            Assert.AreEqual(8, MarginCallTargets.TotalRounds);
        }
    }
}
