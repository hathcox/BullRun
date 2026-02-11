using NUnit.Framework;

namespace BullRun.Tests.PriceEngine
{
    [TestFixture]
    public class GameConfigTests
    {
        [Test]
        public void StartingCapital_Is1000()
        {
            Assert.AreEqual(1000f, GameConfig.StartingCapital);
        }

        [Test]
        public void RoundDurationSeconds_Is60()
        {
            Assert.AreEqual(60f, GameConfig.RoundDurationSeconds);
        }

        [Test]
        public void PriceUpdateRate_IsPerFrame()
        {
            // Per-frame means update rate is 0 (every frame, no fixed interval)
            Assert.AreEqual(0f, GameConfig.PriceUpdateRate);
        }

        [Test]
        public void StartingCapital_IsPositive()
        {
            Assert.Greater(GameConfig.StartingCapital, 0f);
        }

        [Test]
        public void RoundDurationSeconds_IsPositive()
        {
            Assert.Greater(GameConfig.RoundDurationSeconds, 0f);
        }
    }
}
