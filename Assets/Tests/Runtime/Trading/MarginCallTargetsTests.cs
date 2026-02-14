using NUnit.Framework;

namespace BullRun.Tests.Trading
{
    [TestFixture]
    public class MarginCallTargetsTests
    {
        // --- FIX-14: Rebalanced cumulative value targets (AC: 2, 8) ---

        [Test]
        public void GetTarget_Round1_Returns20()
        {
            Assert.AreEqual(20f, MarginCallTargets.GetTarget(1), 0.01f);
        }

        [Test]
        public void GetTarget_Round2_Returns35()
        {
            Assert.AreEqual(35f, MarginCallTargets.GetTarget(2), 0.01f);
        }

        [Test]
        public void GetTarget_Round3_Returns60()
        {
            Assert.AreEqual(60f, MarginCallTargets.GetTarget(3), 0.01f);
        }

        [Test]
        public void GetTarget_Round4_Returns100()
        {
            Assert.AreEqual(100f, MarginCallTargets.GetTarget(4), 0.01f);
        }

        [Test]
        public void GetTarget_Round5_Returns175()
        {
            Assert.AreEqual(175f, MarginCallTargets.GetTarget(5), 0.01f);
        }

        [Test]
        public void GetTarget_Round6_Returns300()
        {
            Assert.AreEqual(300f, MarginCallTargets.GetTarget(6), 0.01f);
        }

        [Test]
        public void GetTarget_Round7_Returns500()
        {
            Assert.AreEqual(500f, MarginCallTargets.GetTarget(7), 0.01f);
        }

        [Test]
        public void GetTarget_Round8_Returns800()
        {
            Assert.AreEqual(800f, MarginCallTargets.GetTarget(8), 0.01f);
        }

        // --- Boundary Tests ---

        [Test]
        public void GetTarget_Round0_ReturnsFirstTarget()
        {
            Assert.AreEqual(20f, MarginCallTargets.GetTarget(0), 0.01f);
        }

        [Test]
        public void GetTarget_NegativeRound_ReturnsFirstTarget()
        {
            Assert.AreEqual(20f, MarginCallTargets.GetTarget(-1), 0.01f);
        }

        [Test]
        public void GetTarget_BeyondRound8_ReturnsLastTarget()
        {
            Assert.AreEqual(800f, MarginCallTargets.GetTarget(9), 0.01f);
            Assert.AreEqual(800f, MarginCallTargets.GetTarget(20), 0.01f);
        }

        [Test]
        public void TotalRounds_Returns8()
        {
            Assert.AreEqual(8, MarginCallTargets.TotalRounds);
        }

        // --- Scaling Multipliers ---

        [Test]
        public void ScalingMultipliers_Has8Entries()
        {
            Assert.AreEqual(8, MarginCallTargets.ScalingMultipliers.Length);
        }

        // --- GetAllTargets ---

        [Test]
        public void GetAllTargets_Returns8Targets()
        {
            float[] targets = MarginCallTargets.GetAllTargets();
            Assert.AreEqual(8, targets.Length);
        }

        [Test]
        public void GetAllTargets_MatchesRebalancedValues()
        {
            float[] expected = { 20f, 35f, 60f, 100f, 175f, 300f, 500f, 800f };
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

        // --- Difficulty curve validation ---

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

        // --- ScalingMultipliers validation ---

        [Test]
        public void ScalingMultipliers_SameLengthAsTargets()
        {
            Assert.AreEqual(MarginCallTargets.Targets.Length, MarginCallTargets.ScalingMultipliers.Length,
                "ScalingMultipliers must have same length as Targets — keep in sync when rebalancing");
        }

        [Test]
        public void ScalingMultipliers_NonDecreasingAcrossActs()
        {
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
