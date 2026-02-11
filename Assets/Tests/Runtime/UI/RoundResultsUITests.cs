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

        // --- FormatProfit ---

        [Test]
        public void FormatProfit_PositiveValue_ShowsPlusSign()
        {
            string result = RoundResultsUI.FormatProfit(650f);
            Assert.AreEqual("+$650", result);
        }

        [Test]
        public void FormatProfit_NegativeValue_ShowsMinusSign()
        {
            string result = RoundResultsUI.FormatProfit(-120f);
            Assert.AreEqual("-$120", result);
        }

        [Test]
        public void FormatProfit_Zero_ShowsPlusSign()
        {
            string result = RoundResultsUI.FormatProfit(0f);
            Assert.AreEqual("+$0", result);
        }

        // --- FormatTarget ---

        [Test]
        public void FormatTarget_WhenMet_ShowsPassed()
        {
            string result = RoundResultsUI.FormatTarget(600f, true);
            Assert.AreEqual("$600 \u2014 PASSED", result);
        }

        [Test]
        public void FormatTarget_WhenNotMet_ShowsFailed()
        {
            string result = RoundResultsUI.FormatTarget(600f, false);
            Assert.AreEqual("$600 \u2014 FAILED", result);
        }

        // --- FormatCash ---

        [Test]
        public void FormatCash_PositiveAmount_FormatsCorrectly()
        {
            string result = RoundResultsUI.FormatCash(2800f);
            Assert.AreEqual("$2,800", result);
        }

        [Test]
        public void FormatCash_SmallAmount_FormatsCorrectly()
        {
            string result = RoundResultsUI.FormatCash(500f);
            Assert.AreEqual("$500", result);
        }

        // --- BuildStatsText ---

        [Test]
        public void BuildStatsText_ContainsAllFields()
        {
            var evt = new RoundCompletedEvent
            {
                RoundNumber = 3,
                RoundProfit = 650f,
                ProfitTarget = 600f,
                TargetMet = true,
                TotalCash = 2800f
            };

            string stats = RoundResultsUI.BuildStatsText(evt);

            Assert.IsTrue(stats.Contains("+$650"), "Should contain round profit");
            Assert.IsTrue(stats.Contains("$600"), "Should contain target amount");
            Assert.IsTrue(stats.Contains("PASSED"), "Should contain PASSED indicator");
            Assert.IsTrue(stats.Contains("$2,800"), "Should contain total cash");
        }

        [Test]
        public void BuildStatsText_FailedTarget_ContainsFailed()
        {
            var evt = new RoundCompletedEvent
            {
                RoundNumber = 2,
                RoundProfit = 100f,
                ProfitTarget = 300f,
                TargetMet = false,
                TotalCash = 1100f
            };

            string stats = RoundResultsUI.BuildStatsText(evt);

            Assert.IsTrue(stats.Contains("FAILED"), "Should contain FAILED indicator");
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
