using NUnit.Framework;

namespace BullRun.Tests.UI
{
    [TestFixture]
    public class MarketCloseUITests
    {
        // --- FormatProfit static utility tests ---

        [Test]
        public void FormatProfit_PositiveValue_ShowsPlusSign()
        {
            string result = MarketCloseUI.FormatProfit(650f);
            Assert.AreEqual("+$650", result);
        }

        [Test]
        public void FormatProfit_NegativeValue_ShowsMinusSign()
        {
            string result = MarketCloseUI.FormatProfit(-120f);
            Assert.AreEqual("-$120", result);
        }

        [Test]
        public void FormatProfit_Zero_ShowsPlusSign()
        {
            string result = MarketCloseUI.FormatProfit(0f);
            Assert.AreEqual("+$0", result);
        }

        [Test]
        public void FormatProfit_LargePositive_FormatsCorrectly()
        {
            string result = MarketCloseUI.FormatProfit(1500f);
            Assert.AreEqual("+$1500", result);
        }

        [Test]
        public void FormatProfit_SmallNegative_FormatsCorrectly()
        {
            string result = MarketCloseUI.FormatProfit(-5f);
            Assert.AreEqual("-$5", result);
        }

        [Test]
        public void FormatProfit_FractionalValue_RoundsToWholeNumber()
        {
            string result = MarketCloseUI.FormatProfit(123.7f);
            Assert.AreEqual("+$124", result);
        }
    }
}
