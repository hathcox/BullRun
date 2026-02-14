using NUnit.Framework;

namespace BullRun.Tests.PriceEngine
{
    [TestFixture]
    public class GameConfigTests
    {
        [Test]
        public void StartingCapital_Is10()
        {
            Assert.AreEqual(10f, GameConfig.StartingCapital);
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

        [Test]
        public void ShortMarginRequirement_Is50Percent()
        {
            Assert.AreEqual(0.5f, GameConfig.ShortMarginRequirement);
        }

        // --- Act/Round Config Tests (Story 4.5 Task 6) ---

        [Test]
        public void TotalRounds_Is8()
        {
            Assert.AreEqual(8, GameConfig.TotalRounds);
        }

        [Test]
        public void TotalActs_Is4()
        {
            Assert.AreEqual(4, GameConfig.TotalActs);
        }

        [Test]
        public void RoundsPerAct_Is2()
        {
            Assert.AreEqual(2, GameConfig.RoundsPerAct);
        }

        [Test]
        public void Acts_Has5Entries_IncludingUnusedIndex0()
        {
            Assert.AreEqual(5, GameConfig.Acts.Length);
        }

        [Test]
        public void Acts_Act1_IsPennyStocks()
        {
            var act = GameConfig.Acts[1];
            Assert.AreEqual(1, act.ActNumber);
            Assert.AreEqual(StockTier.Penny, act.Tier);
            Assert.AreEqual(1, act.FirstRound);
            Assert.AreEqual(2, act.LastRound);
            Assert.AreEqual("Penny Stocks", act.DisplayName);
        }

        [Test]
        public void Acts_Act2_IsLowValueStocks()
        {
            var act = GameConfig.Acts[2];
            Assert.AreEqual(2, act.ActNumber);
            Assert.AreEqual(StockTier.LowValue, act.Tier);
            Assert.AreEqual(3, act.FirstRound);
            Assert.AreEqual(4, act.LastRound);
            Assert.AreEqual("Low-Value Stocks", act.DisplayName);
        }

        [Test]
        public void Acts_Act3_IsMidValueStocks()
        {
            var act = GameConfig.Acts[3];
            Assert.AreEqual(3, act.ActNumber);
            Assert.AreEqual(StockTier.MidValue, act.Tier);
            Assert.AreEqual(5, act.FirstRound);
            Assert.AreEqual(6, act.LastRound);
            Assert.AreEqual("Mid-Value Stocks", act.DisplayName);
        }

        [Test]
        public void Acts_Act4_IsBlueChips()
        {
            var act = GameConfig.Acts[4];
            Assert.AreEqual(4, act.ActNumber);
            Assert.AreEqual(StockTier.BlueChip, act.Tier);
            Assert.AreEqual(7, act.FirstRound);
            Assert.AreEqual(8, act.LastRound);
            Assert.AreEqual("Blue Chips", act.DisplayName);
        }

        // --- Tier Transition Config (Story 6.2 Task 5) ---

        [Test]
        public void TransitionDurationSeconds_Is3()
        {
            Assert.AreEqual(3f, GameConfig.TransitionDurationSeconds);
        }

        [Test]
        public void Acts_Act1_HasTagline()
        {
            var act = GameConfig.Acts[1];
            Assert.AreEqual("The Penny Pit \u2014 Where Fortunes Begin", act.Tagline);
        }

        [Test]
        public void Acts_Act2_HasTagline()
        {
            var act = GameConfig.Acts[2];
            Assert.AreEqual("Rising Stakes \u2014 Trends and Reversals", act.Tagline);
        }

        [Test]
        public void Acts_Act3_HasTagline()
        {
            var act = GameConfig.Acts[3];
            Assert.AreEqual("The Trading Floor \u2014 Sectors in Motion", act.Tagline);
        }

        [Test]
        public void Acts_Act4_HasTagline()
        {
            var act = GameConfig.Acts[4];
            Assert.AreEqual("Blue Chip Arena \u2014 The Big Leagues", act.Tagline);
        }

        [Test]
        public void Acts_AllActsHaveNonEmptyTaglines()
        {
            for (int i = 1; i <= GameConfig.TotalActs; i++)
            {
                Assert.IsFalse(string.IsNullOrEmpty(GameConfig.Acts[i].Tagline),
                    $"Act {i} has empty tagline");
            }
        }

        [Test]
        public void Acts_RoundsCoverAllRounds_NoGapsOrOverlaps()
        {
            // Verify rounds 1-8 are all accounted for
            bool[] covered = new bool[9]; // index 0 unused
            for (int actIdx = 1; actIdx <= 4; actIdx++)
            {
                var act = GameConfig.Acts[actIdx];
                for (int r = act.FirstRound; r <= act.LastRound; r++)
                {
                    Assert.IsFalse(covered[r], $"Round {r} covered by multiple acts");
                    covered[r] = true;
                }
            }
            for (int r = 1; r <= 8; r++)
            {
                Assert.IsTrue(covered[r], $"Round {r} not covered by any act");
            }
        }

        // --- Debug Starting Cash (Story 6.3 Task 5) ---

        [Test]
        public void DebugStartingCash_Has8Entries()
        {
            Assert.AreEqual(8, GameConfig.DebugStartingCash.Length);
        }

        [Test]
        public void DebugStartingCash_MatchesRebalancedValues()
        {
            Assert.AreEqual(10f, GameConfig.DebugStartingCash[0], 0.01f);   // Round 1
            Assert.AreEqual(20f, GameConfig.DebugStartingCash[1], 0.01f);   // Round 2
            Assert.AreEqual(40f, GameConfig.DebugStartingCash[2], 0.01f);   // Round 3
            Assert.AreEqual(75f, GameConfig.DebugStartingCash[3], 0.01f);   // Round 4
            Assert.AreEqual(130f, GameConfig.DebugStartingCash[4], 0.01f);  // Round 5
            Assert.AreEqual(225f, GameConfig.DebugStartingCash[5], 0.01f);  // Round 6
            Assert.AreEqual(400f, GameConfig.DebugStartingCash[6], 0.01f);  // Round 7
            Assert.AreEqual(700f, GameConfig.DebugStartingCash[7], 0.01f);  // Round 8
        }

        [Test]
        public void DebugStartingCash_AllPositive()
        {
            for (int i = 0; i < GameConfig.DebugStartingCash.Length; i++)
            {
                Assert.Greater(GameConfig.DebugStartingCash[i], 0f,
                    $"Debug starting cash for Round {i + 1} should be positive");
            }
        }

        [Test]
        public void DebugStartingCash_MonotonicallyIncreasing()
        {
            for (int i = 1; i < GameConfig.DebugStartingCash.Length; i++)
            {
                Assert.GreaterOrEqual(GameConfig.DebugStartingCash[i], GameConfig.DebugStartingCash[i - 1],
                    $"Debug cash for Round {i + 1} should be >= Round {i}");
            }
        }
    }
}
