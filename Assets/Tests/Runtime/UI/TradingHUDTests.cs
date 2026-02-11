using NUnit.Framework;
using UnityEngine;

namespace BullRun.Tests.UI
{
    [TestFixture]
    public class TradingHUDTests
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

        // --- Number Formatting ---

        [Test]
        public void FormatCurrency_PositiveValue()
        {
            Assert.AreEqual("$1,234.56", TradingHUD.FormatCurrency(1234.56f));
        }

        [Test]
        public void FormatCurrency_SmallValue()
        {
            Assert.AreEqual("$0.50", TradingHUD.FormatCurrency(0.50f));
        }

        [Test]
        public void FormatCurrency_LargeValue()
        {
            Assert.AreEqual("$12,345.00", TradingHUD.FormatCurrency(12345f));
        }

        [Test]
        public void FormatProfit_Positive_HasPlusSign()
        {
            string formatted = TradingHUD.FormatProfit(500f);
            Assert.IsTrue(formatted.StartsWith("+"), "Positive profit should start with +");
            Assert.IsTrue(formatted.Contains("500"), "Should contain the value");
        }

        [Test]
        public void FormatProfit_Negative_HasMinusSign()
        {
            string formatted = TradingHUD.FormatProfit(-200f);
            Assert.IsTrue(formatted.StartsWith("-"), "Negative profit should start with -");
        }

        [Test]
        public void FormatProfit_Zero_ShowsZero()
        {
            string formatted = TradingHUD.FormatProfit(0f);
            Assert.IsTrue(formatted.Contains("0.00"), "Zero profit should show $0.00");
        }

        [Test]
        public void FormatPercentChange_Positive()
        {
            string formatted = TradingHUD.FormatPercentChange(0.123f);
            Assert.AreEqual("+12.3%", formatted);
        }

        [Test]
        public void FormatPercentChange_Negative()
        {
            string formatted = TradingHUD.FormatPercentChange(-0.057f);
            Assert.AreEqual("-5.7%", formatted);
        }

        [Test]
        public void FormatPercentChange_Zero()
        {
            string formatted = TradingHUD.FormatPercentChange(0f);
            Assert.AreEqual("+0.0%", formatted);
        }

        // --- Color Coding ---

        [Test]
        public void GetProfitColor_Positive_ReturnsGreen()
        {
            var color = TradingHUD.GetProfitColor(100f);
            Assert.AreEqual(TradingHUD.ProfitGreen, color);
        }

        [Test]
        public void GetProfitColor_Negative_ReturnsRed()
        {
            var color = TradingHUD.GetProfitColor(-50f);
            Assert.AreEqual(TradingHUD.LossRed, color);
        }

        [Test]
        public void GetProfitColor_Zero_ReturnsWhite()
        {
            var color = TradingHUD.GetProfitColor(0f);
            Assert.AreEqual(Color.white, color);
        }

        // --- Margin Target Progress ---

        [Test]
        public void CalculateTargetProgress_HalfwayToTarget()
        {
            float progress = TradingHUD.CalculateTargetProgress(300f, 600f);
            Assert.AreEqual(0.5f, progress, 0.001f);
        }

        [Test]
        public void CalculateTargetProgress_ExceedsTarget_ClampedToOne()
        {
            float progress = TradingHUD.CalculateTargetProgress(800f, 600f);
            Assert.AreEqual(1f, progress, 0.001f);
        }

        [Test]
        public void CalculateTargetProgress_NegativeProfit_ClampedToZero()
        {
            float progress = TradingHUD.CalculateTargetProgress(-100f, 600f);
            Assert.AreEqual(0f, progress, 0.001f);
        }

        [Test]
        public void CalculateTargetProgress_ZeroTarget_ReturnsOne()
        {
            float progress = TradingHUD.CalculateTargetProgress(100f, 0f);
            Assert.AreEqual(1f, progress, 0.001f);
        }

        // --- Margin Progress Bar Color ---

        [Test]
        public void GetTargetBarColor_OnPace_ReturnsGreen()
        {
            // 60% progress, 50% time elapsed — on pace
            var color = TradingHUD.GetTargetBarColor(0.6f, 0.5f);
            Assert.AreEqual(TradingHUD.ProfitGreen, color);
        }

        [Test]
        public void GetTargetBarColor_FallingBehind_ReturnsYellow()
        {
            // 40% progress, 60% time elapsed — behind pace but not critical
            var color = TradingHUD.GetTargetBarColor(0.4f, 0.6f);
            Assert.AreEqual(TradingHUD.WarningYellow, color);
        }

        [Test]
        public void GetTargetBarColor_SignificantlyBehind_ReturnsRed()
        {
            // 10% progress, 80% time elapsed — significantly behind
            var color = TradingHUD.GetTargetBarColor(0.1f, 0.8f);
            Assert.AreEqual(TradingHUD.LossRed, color);
        }

        [Test]
        public void GetTargetBarColor_NegativeProgress_ReturnsRed()
        {
            var color = TradingHUD.GetTargetBarColor(0f, 0.5f);
            Assert.AreEqual(TradingHUD.LossRed, color);
        }
    }
}
