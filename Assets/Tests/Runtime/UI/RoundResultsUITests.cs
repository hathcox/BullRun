using NUnit.Framework;

namespace BullRun.Tests.UI
{
    [TestFixture]
    public class RoundResultsUITests
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

        // --- GetHeaderText ---

        [Test]
        public void GetHeaderText_ReturnsRoundCompleteWithNumber()
        {
            string header = RoundResultsUI.GetHeaderText(3);
            Assert.AreEqual("ROUND 3 COMPLETE", header);
        }

        [Test]
        public void GetHeaderText_Round1()
        {
            string header = RoundResultsUI.GetHeaderText(1);
            Assert.AreEqual("ROUND 1 COMPLETE", header);
        }

        [Test]
        public void GetHeaderText_Round8()
        {
            string header = RoundResultsUI.GetHeaderText(8);
            Assert.AreEqual("ROUND 8 COMPLETE", header);
        }

        // --- FormatProfit (FIX-14: now uses F2 for $10-scale precision) ---

        [Test]
        public void FormatProfit_PositiveValue_ShowsPlusSign()
        {
            string result = RoundResultsUI.FormatProfit(650f);
            Assert.AreEqual("+$650.00", result);
        }

        [Test]
        public void FormatProfit_NegativeValue_ShowsMinusSign()
        {
            string result = RoundResultsUI.FormatProfit(-120f);
            Assert.AreEqual("-$120.00", result);
        }

        [Test]
        public void FormatProfit_Zero_ShowsPlusSign()
        {
            string result = RoundResultsUI.FormatProfit(0f);
            Assert.AreEqual("+$0.00", result);
        }

        [Test]
        public void FormatProfit_SmallValue_ShowsDecimals()
        {
            string result = RoundResultsUI.FormatProfit(0.30f);
            Assert.AreEqual("+$0.30", result);
        }

        // --- FormatTarget (FIX-14: now uses F2) ---

        [Test]
        public void FormatTarget_WhenMet_ShowsPassed()
        {
            string result = RoundResultsUI.FormatTarget(20f, true);
            Assert.AreEqual("$20.00 \u2014 PASSED", result);
        }

        [Test]
        public void FormatTarget_WhenNotMet_ShowsFailed()
        {
            string result = RoundResultsUI.FormatTarget(20f, false);
            Assert.AreEqual("$20.00 \u2014 FAILED", result);
        }

        // --- FormatCash (FIX-14: now uses N2) ---

        [Test]
        public void FormatCash_PositiveAmount_FormatsCorrectly()
        {
            string result = RoundResultsUI.FormatCash(2800f);
            Assert.AreEqual("$2,800.00", result);
        }

        [Test]
        public void FormatCash_SmallAmount_FormatsCorrectly()
        {
            string result = RoundResultsUI.FormatCash(10.50f);
            Assert.AreEqual("$10.50", result);
        }

        // --- BuildStatsText (FIX-14: includes Rep earned line) ---

        [Test]
        public void BuildStatsText_ContainsAllFields()
        {
            var evt = new RoundCompletedEvent
            {
                RoundNumber = 3,
                RoundProfit = 15.50f,
                ProfitTarget = 60f,
                TargetMet = true,
                TotalCash = 75.50f,
                RepEarned = 11,
                BaseRep = 11,
                BonusRep = 0
            };

            string stats = RoundResultsUI.BuildStatsText(evt);

            Assert.IsTrue(stats.Contains("+$15.50"), "Should contain round profit");
            Assert.IsTrue(stats.Contains("$60.00"), "Should contain target amount");
            Assert.IsTrue(stats.Contains("PASSED"), "Should contain PASSED indicator");
            Assert.IsTrue(stats.Contains("$75.50"), "Should contain total cash");
            Assert.IsTrue(stats.Contains("\u2605 11"), "Should contain Rep earned with star");
            Assert.IsTrue(stats.Contains("Base: 11"), "Should contain base Rep breakdown");
        }

        [Test]
        public void BuildStatsText_FailedTarget_ContainsFailed()
        {
            var evt = new RoundCompletedEvent
            {
                RoundNumber = 2,
                RoundProfit = 5f,
                ProfitTarget = 35f,
                TargetMet = false,
                TotalCash = 15f
            };

            string stats = RoundResultsUI.BuildStatsText(evt);

            Assert.IsTrue(stats.Contains("FAILED"), "Should contain FAILED indicator");
        }

        [Test]
        public void BuildStatsText_IncludesRepEarned_BaseOnly()
        {
            var evt = new RoundCompletedEvent
            {
                RoundNumber = 1,
                RoundProfit = 10f,
                ProfitTarget = 20f,
                TargetMet = true,
                TotalCash = 20f,
                RepEarned = 5,
                BaseRep = 5,
                BonusRep = 0
            };

            string stats = RoundResultsUI.BuildStatsText(evt);
            Assert.IsTrue(stats.Contains("Reputation Earned"), "Should contain Rep label");
            Assert.IsTrue(stats.Contains("\u2605 5"), "Should contain Rep amount with star");
            Assert.IsTrue(stats.Contains("Base: 5"), "Should show base breakdown");
            Assert.IsFalse(stats.Contains("Bonus:"), "Should NOT show bonus when 0");
        }

        [Test]
        public void BuildStatsText_IncludesRepEarned_WithBonus()
        {
            var evt = new RoundCompletedEvent
            {
                RoundNumber = 1,
                RoundProfit = 20f,
                ProfitTarget = 20f,
                TargetMet = true,
                TotalCash = 30f,
                RepEarned = 6,
                BaseRep = 5,
                BonusRep = 1
            };

            string stats = RoundResultsUI.BuildStatsText(evt);
            Assert.IsTrue(stats.Contains("\u2605 6"), "Should contain total Rep with star");
            Assert.IsTrue(stats.Contains("Base: 5"), "Should show base component");
            Assert.IsTrue(stats.Contains("Bonus: 1"), "Should show bonus component");
        }

        // --- Display duration constant ---

        [Test]
        public void DisplayDuration_IsBetween2And3Seconds()
        {
            Assert.GreaterOrEqual(RoundResultsUI.DisplayDuration, 2f);
            Assert.LessOrEqual(RoundResultsUI.DisplayDuration, 3f);
        }
    }
}
