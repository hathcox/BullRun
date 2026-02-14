using NUnit.Framework;

namespace BullRun.Tests.Core
{
    /// <summary>
    /// FIX-14: Economy rebalance tests — $10 start, low targets, debug cash values.
    /// Covers AC 1, 2, 3, 7, 8, 9.
    /// </summary>
    [TestFixture]
    public class EconomyRebalanceTests
    {
        // === AC 1: StartingCapital is $10 ===

        [Test]
        public void StartingCapital_Is10()
        {
            Assert.AreEqual(10f, GameConfig.StartingCapital, 0.01f);
        }

        [Test]
        public void RunContext_StartNewRun_Has10StartingCapital()
        {
            EventBus.Clear();
            var ctx = RunContext.StartNewRun();
            Assert.AreEqual(10f, ctx.Portfolio.Cash, 0.01f);
            EventBus.Clear();
        }

        // === AC 2: Round targets match expected values ===

        [Test]
        public void MarginCallTarget_Round1_Is20()
        {
            Assert.AreEqual(20f, MarginCallTargets.GetTarget(1), 0.01f);
        }

        [Test]
        public void AllRoundTargets_MatchExpected()
        {
            float[] expected = { 20f, 35f, 60f, 100f, 175f, 300f, 500f, 800f };
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], MarginCallTargets.GetTarget(i + 1), 0.01f,
                    $"Round {i + 1} target mismatch");
            }
        }

        // === AC 3: DebugStartingCash values match expected progression ===

        [Test]
        public void DebugStartingCash_Has8Entries()
        {
            Assert.AreEqual(8, GameConfig.DebugStartingCash.Length);
        }

        [Test]
        public void DebugStartingCash_MatchesExpectedValues()
        {
            float[] expected = { 10f, 20f, 40f, 75f, 130f, 225f, 400f, 700f };
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], GameConfig.DebugStartingCash[i], 0.01f,
                    $"DebugStartingCash mismatch at index {i} (Round {i + 1})");
            }
        }

        [Test]
        public void DebugStartingCash_IsMonotonicallyIncreasing()
        {
            for (int i = 1; i < GameConfig.DebugStartingCash.Length; i++)
            {
                Assert.Greater(GameConfig.DebugStartingCash[i], GameConfig.DebugStartingCash[i - 1],
                    $"DebugStartingCash[{i}] should be greater than [{i - 1}]");
            }
        }

        // === AC 7: Penny stock prices affordable at $10 (at least one stock < $5) ===

        [Test]
        public void PennyStockMinPrice_IsAffordableAt10()
        {
            var pennyConfig = StockTierData.GetTier(StockTier.Penny);
            Assert.LessOrEqual(pennyConfig.MinPrice, 5f,
                "Penny stock min price must be <= $5 to be affordable with $10 starting capital");
        }

        [Test]
        public void PennyStockMinPrice_AllowsAtLeast1ShareAt10()
        {
            var pennyConfig = StockTierData.GetTier(StockTier.Penny);
            Assert.LessOrEqual(pennyConfig.MinPrice, GameConfig.StartingCapital,
                "Player must be able to afford at least 1 penny stock share with starting capital");
        }

        // === AC 8: Targets use centralized MarginCallTargets (no hardcoded old values) ===

        [Test]
        public void Targets_NoOldValuesPresent()
        {
            // Old values: 200, 350, 600, 900, 1500, 2200, 3500, 5000
            float[] targets = MarginCallTargets.GetAllTargets();
            foreach (float t in targets)
            {
                Assert.AreNotEqual(200f, t, "Old target $200 should not be in rebalanced targets");
                Assert.AreNotEqual(5000f, t, "Old target $5000 should not be in rebalanced targets");
            }
        }

        // === Reputation earning constants exist in GameConfig ===

        [Test]
        public void RepBaseAwardPerRound_Has8Entries()
        {
            Assert.AreEqual(8, GameConfig.RepBaseAwardPerRound.Length);
        }

        [Test]
        public void RepBaseAwardPerRound_MatchesExpected()
        {
            int[] expected = { 5, 8, 11, 15, 20, 26, 33, 40 };
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], GameConfig.RepBaseAwardPerRound[i],
                    $"RepBaseAwardPerRound mismatch at index {i}");
            }
        }

        [Test]
        public void RepPerformanceBonusRate_Is0_5()
        {
            Assert.AreEqual(0.5f, GameConfig.RepPerformanceBonusRate, 0.001f);
        }

        [Test]
        public void RepConsolationPerRound_Is2()
        {
            Assert.AreEqual(2, GameConfig.RepConsolationPerRound);
        }
    }
}
