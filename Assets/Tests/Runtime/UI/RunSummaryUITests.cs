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
        public void GetHeaderText_WhenVictory_ReturnsBULLRUN()
        {
            string header = RunSummaryUI.GetHeaderText(false, true);
            Assert.AreEqual("BULL RUN!", header);
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
    }
}
