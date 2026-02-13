using NUnit.Framework;

namespace BullRun.Tests.UI
{
    [TestFixture]
    public class RunSummaryUITests
    {
        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
        }

        [Test]
        public void GetHeaderText_WhenMarginCalled_ReturnsMARGINCALL()
        {
            string header = RunSummaryUI.GetHeaderText(true);
            Assert.AreEqual("MARGIN CALL", header);
        }

        [Test]
        public void GetHeaderText_WhenRunComplete_ReturnsRUNCOMPLETE()
        {
            string header = RunSummaryUI.GetHeaderText(false, false);
            Assert.AreEqual("RUN COMPLETE", header);
        }

        [Test]
        public void GetHeaderText_WhenVictory_ReturnsBULLRUNCOMPLETE()
        {
            string header = RunSummaryUI.GetHeaderText(false, true);
            Assert.AreEqual("BULL RUN COMPLETE!", header);
        }

        [Test]
        public void GetHeaderText_WhenMarginCalled_AlwaysReturnsMARGINCALL()
        {
            // Even if isVictory is true, margin call takes priority
            string header = RunSummaryUI.GetHeaderText(true, true);
            Assert.AreEqual("MARGIN CALL", header);
        }

        [Test]
        public void FormatCash_PositiveAmount_FormatsCorrectly()
        {
            string formatted = RunSummaryUI.FormatCash(1500.50f);
            Assert.AreEqual("$1,500.50", formatted);
        }

        [Test]
        public void FormatCash_NegativeAmount_FormatsCorrectly()
        {
            string formatted = RunSummaryUI.FormatCash(-200.00f);
            Assert.AreEqual("-$200.00", formatted);
        }

        [Test]
        public void FormatProfit_PositiveAmount_IncludesPlusSign()
        {
            string formatted = RunSummaryUI.FormatProfit(500f);
            Assert.AreEqual("+$500.00", formatted);
        }

        [Test]
        public void FormatProfit_NegativeAmount_IncludesMinusSign()
        {
            string formatted = RunSummaryUI.FormatProfit(-300f);
            Assert.AreEqual("-$300.00", formatted);
        }

        [Test]
        public void FormatProfit_Zero_ShowsPositive()
        {
            string formatted = RunSummaryUI.FormatProfit(0f);
            Assert.AreEqual("+$0.00", formatted);
        }

        // --- Victory Stats Tests (Story 6.5 Task 2) ---

        [Test]
        public void BuildVictoryStatsText_IncludesAllFields()
        {
            var evt = new RunEndedEvent
            {
                RoundsCompleted = 8,
                FinalCash = 5000f,
                TotalProfit = 4000f,
                WasMarginCalled = false,
                ReputationEarned = 140,
                ItemsCollected = 3,
                PeakCash = 6000f,
                BestRoundProfit = 1200f
            };

            string stats = RunSummaryUI.BuildVictoryStatsText(evt);
            Assert.IsTrue(stats.Contains("Total Profit"), "Should show Total Profit");
            Assert.IsTrue(stats.Contains("Peak Cash"), "Should show Peak Cash");
            Assert.IsTrue(stats.Contains("8/8"), "Should show Rounds Completed as 8/8");
            Assert.IsTrue(stats.Contains("Items Collected"), "Should show Items Collected");
            Assert.IsTrue(stats.Contains("Best Round"), "Should show Best Round");
            Assert.IsTrue(stats.Contains("Reputation"), "Should show Reputation Earned");
        }

        // --- Count-Up Animation Tests (Story 6.5 Task 5) ---

        [Test]
        public void BuildVictoryStatsTextAnimated_AtZero_AllValuesZero()
        {
            var evt = new RunEndedEvent
            {
                TotalProfit = 4000f,
                PeakCash = 6000f,
                RoundsCompleted = 8,
                ItemsCollected = 3,
                BestRoundProfit = 1200f,
                ReputationEarned = 140
            };

            string stats = RunSummaryUI.BuildVictoryStatsTextAnimated(evt, 0f);
            Assert.IsTrue(stats.Contains("+$0.00"), "At t=0, profit should be zero");
            Assert.IsTrue(stats.Contains("0/8"), "At t=0, rounds should be 0");
        }

        [Test]
        public void BuildVictoryStatsTextAnimated_AtOne_MatchesFinal()
        {
            var evt = new RunEndedEvent
            {
                TotalProfit = 4000f,
                PeakCash = 6000f,
                RoundsCompleted = 8,
                ItemsCollected = 3,
                BestRoundProfit = 1200f,
                ReputationEarned = 140
            };

            string animated = RunSummaryUI.BuildVictoryStatsTextAnimated(evt, 1f);
            string final_ = RunSummaryUI.BuildVictoryStatsText(evt);
            Assert.AreEqual(final_, animated, "At t=1, animated should match final");
        }

        [Test]
        public void BuildLossStatsText_IncludesRoundsAndProfit()
        {
            var evt = new RunEndedEvent
            {
                RoundsCompleted = 3,
                FinalCash = 800f,
                TotalProfit = -200f,
                WasMarginCalled = true,
                ReputationEarned = 25,
                ItemsCollected = 1,
                PeakCash = 1500f,
                BestRoundProfit = 300f
            };

            string stats = RunSummaryUI.BuildLossStatsText(evt);
            Assert.IsTrue(stats.Contains("Rounds Completed"), "Should show Rounds Completed");
            Assert.IsTrue(stats.Contains("Final Cash"), "Should show Final Cash");
            Assert.IsTrue(stats.Contains("Reputation"), "Should show Reputation Earned");
        }
    }
}
