using NUnit.Framework;

namespace BullRun.Tests.Trading
{
    [TestFixture]
    public class MarginCallTargetsTests
    {
        // --- GDD Section 2.3 Target Values (AC: 1) ---

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

        // --- Boundary Tests ---

        [Test]
        public void GetTarget_Round0_ReturnsFirstTarget()
        {
            Assert.AreEqual(200f, MarginCallTargets.GetTarget(0), 0.01f);
        }

        [Test]
        public void GetTarget_NegativeRound_ReturnsFirstTarget()
        {
            Assert.AreEqual(200f, MarginCallTargets.GetTarget(-1), 0.01f);
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

        // --- Scaling Multipliers (AC: 2) ---

        [Test]
        public void ScalingMultipliers_Has8Entries()
        {
            Assert.AreEqual(8, MarginCallTargets.ScalingMultipliers.Length);
        }

        [Test]
        public void ScalingMultipliers_Act1_Is1x()
        {
            Assert.AreEqual(1.0f, MarginCallTargets.ScalingMultipliers[0], 0.01f); // Round 1
            Assert.AreEqual(1.0f, MarginCallTargets.ScalingMultipliers[1], 0.01f); // Round 2
        }

        [Test]
        public void ScalingMultipliers_Act2_Is1_5x()
        {
            Assert.AreEqual(1.5f, MarginCallTargets.ScalingMultipliers[2], 0.01f); // Round 3
            Assert.AreEqual(1.5f, MarginCallTargets.ScalingMultipliers[3], 0.01f); // Round 4
        }

        [Test]
        public void ScalingMultipliers_Act3_Is2x()
        {
            Assert.AreEqual(2.0f, MarginCallTargets.ScalingMultipliers[4], 0.01f); // Round 5
            Assert.AreEqual(2.0f, MarginCallTargets.ScalingMultipliers[5], 0.01f); // Round 6
        }

        [Test]
        public void ScalingMultipliers_Act4_Is2_5xAnd3x()
        {
            Assert.AreEqual(2.5f, MarginCallTargets.ScalingMultipliers[6], 0.01f); // Round 7
            Assert.AreEqual(3.0f, MarginCallTargets.ScalingMultipliers[7], 0.01f); // Round 8
        }

        // --- GetAllTargets (AC: 4, debug display support) ---

        [Test]
        public void GetAllTargets_Returns8Targets()
        {
            float[] targets = MarginCallTargets.GetAllTargets();
            Assert.AreEqual(8, targets.Length);
        }

        [Test]
        public void GetAllTargets_MatchesGDDTable()
        {
            float[] expected = { 200f, 350f, 600f, 900f, 1500f, 2200f, 3500f, 5000f };
            float[] targets = MarginCallTargets.GetAllTargets();
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], targets[i], 0.01f, $"Target mismatch at index {i} (Round {i + 1})");
            }
        }

        [Test]
        public void GetAllTargets_ReturnsNewArray_NotInternalReference()
        {
            float[] first = MarginCallTargets.GetAllTargets();
            float[] second = MarginCallTargets.GetAllTargets();
            Assert.AreNotSame(first, second, "GetAllTargets should return a copy, not the internal array");
        }

        // --- Difficulty curve validation (AC: 6) ---

        [Test]
        public void Targets_AreMonotonicallyIncreasing()
        {
            float[] targets = MarginCallTargets.GetAllTargets();
            for (int i = 1; i < targets.Length; i++)
            {
                Assert.Greater(targets[i], targets[i - 1],
                    $"Target for Round {i + 1} (${targets[i]}) should be greater than Round {i} (${targets[i - 1]})");
            }
        }

        [Test]
        public void Targets_AllPositive()
        {
            float[] targets = MarginCallTargets.GetAllTargets();
            for (int i = 0; i < targets.Length; i++)
            {
                Assert.Greater(targets[i], 0f, $"Target for Round {i + 1} should be positive");
            }
        }

        // --- ScalingMultipliers validation (Story 6.3 Code Review) ---

        [Test]
        public void ScalingMultipliers_SameLengthAsTargets()
        {
            Assert.AreEqual(MarginCallTargets.Targets.Length, MarginCallTargets.ScalingMultipliers.Length,
                "ScalingMultipliers must have same length as Targets — keep in sync when rebalancing");
        }

        [Test]
        public void ScalingMultipliers_NonDecreasingAcrossActs()
        {
            // Each act boundary (every 2 rounds) should have >= the previous act's multiplier
            float[] m = MarginCallTargets.ScalingMultipliers;
            for (int act = 1; act < GameConfig.TotalActs; act++)
            {
                int prevActIndex = (act - 1) * GameConfig.RoundsPerAct;
                int currActIndex = act * GameConfig.RoundsPerAct;
                Assert.GreaterOrEqual(m[currActIndex], m[prevActIndex],
                    $"ScalingMultiplier for Act {act + 1} (index {currActIndex}) should be >= Act {act} (index {prevActIndex})");
            }
        }

        [Test]
        public void ScalingMultipliers_AllPositive()
        {
            for (int i = 0; i < MarginCallTargets.ScalingMultipliers.Length; i++)
            {
                Assert.Greater(MarginCallTargets.ScalingMultipliers[i], 0f,
                    $"ScalingMultiplier for Round {i + 1} should be positive");
            }
        }
    }
}
