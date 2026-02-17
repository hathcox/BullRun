using NUnit.Framework;
using UnityEngine;

namespace BullRun.Tests.UI
{
    [TestFixture]
    public class CRTOverlayTests
    {
        private const float Tolerance = 0.01f;

        // ── CRTThemeData overlay config values ─────────────────────────────

        [Test]
        public void ScanlineOpacity_InExpectedRange()
        {
            Assert.GreaterOrEqual(CRTThemeData.ScanlineOpacity, 0.05f, "ScanlineOpacity should be >= 5%");
            Assert.LessOrEqual(CRTThemeData.ScanlineOpacity, 0.10f, "ScanlineOpacity should be <= 10%");
        }

        [Test]
        public void VignetteIntensity_IsPositive()
        {
            Assert.Greater(CRTThemeData.VignetteIntensity, 0f, "VignetteIntensity should be > 0");
            Assert.LessOrEqual(CRTThemeData.VignetteIntensity, 1f, "VignetteIntensity should be <= 1");
        }

        // ── Color migration: TradingHUD now uses CRTThemeData ─────────────

        [Test]
        public void TradingHUD_ProfitGreen_MatchesCRTTextHigh()
        {
            AssertColorEqual(CRTThemeData.TextHigh, TradingHUD.ProfitGreen, "ProfitGreen");
        }

        [Test]
        public void TradingHUD_LossRed_MatchesCRTDanger()
        {
            AssertColorEqual(CRTThemeData.Danger, TradingHUD.LossRed, "LossRed");
        }

        [Test]
        public void TradingHUD_WarningYellow_MatchesCRTWarning()
        {
            AssertColorEqual(CRTThemeData.Warning, TradingHUD.WarningYellow, "WarningYellow");
        }

        // ── Color migration: PositionOverlay uses CRTThemeData ────────────

        [Test]
        public void PositionOverlay_LongColor_MatchesCRTTextHigh()
        {
            AssertColorEqual(CRTThemeData.TextHigh, PositionOverlay.LongColor, "LongColor");
        }

        [Test]
        public void PositionOverlay_ShortColor_MatchesCRTWarning()
        {
            AssertColorEqual(CRTThemeData.Warning, PositionOverlay.ShortColor, "ShortColor");
        }

        [Test]
        public void PositionOverlay_ProfitGreen_MatchesCRTTextHigh()
        {
            AssertColorEqual(CRTThemeData.TextHigh, PositionOverlay.ProfitGreen, "ProfitGreen");
        }

        [Test]
        public void PositionOverlay_LossRed_MatchesCRTDanger()
        {
            AssertColorEqual(CRTThemeData.Danger, PositionOverlay.LossRed, "LossRed");
        }

        // ── Color migration: NewsBanner uses CRTThemeData ─────────────────

        [Test]
        public void NewsBanner_PositiveColor_UsesCRTTextHigh()
        {
            Assert.AreEqual(CRTThemeData.TextHigh.r, NewsBanner.PositiveBannerColor.r, Tolerance, "Red");
            Assert.AreEqual(CRTThemeData.TextHigh.g, NewsBanner.PositiveBannerColor.g, Tolerance, "Green");
            Assert.AreEqual(CRTThemeData.TextHigh.b, NewsBanner.PositiveBannerColor.b, Tolerance, "Blue");
            Assert.AreEqual(0.8f, NewsBanner.PositiveBannerColor.a, Tolerance, "Alpha should be 0.8");
        }

        [Test]
        public void NewsBanner_NegativeColor_UsesCRTDanger()
        {
            Assert.AreEqual(CRTThemeData.Danger.r, NewsBanner.NegativeBannerColor.r, Tolerance, "Red");
            Assert.AreEqual(CRTThemeData.Danger.g, NewsBanner.NegativeBannerColor.g, Tolerance, "Green");
            Assert.AreEqual(CRTThemeData.Danger.b, NewsBanner.NegativeBannerColor.b, Tolerance, "Blue");
            Assert.AreEqual(0.8f, NewsBanner.NegativeBannerColor.a, Tolerance, "Alpha should be 0.8");
        }

        // ── Color migration: RoundTimerUI uses CRTThemeData ───────────────

        [Test]
        public void RoundTimerUI_NormalColor_MatchesCRTTextHigh()
        {
            Color color = RoundTimerUI.GetTimerColor(30f);
            AssertColorEqual(CRTThemeData.TextHigh, color, "NormalColor (>15s)");
        }

        [Test]
        public void RoundTimerUI_UrgencyColor_MatchesCRTWarning()
        {
            Color color = RoundTimerUI.GetTimerColor(10f);
            AssertColorEqual(CRTThemeData.Warning, color, "UrgencyColor (10s)");
        }

        [Test]
        public void RoundTimerUI_CriticalColor_MatchesCRTDanger()
        {
            Color color = RoundTimerUI.GetTimerColor(3f);
            AssertColorEqual(CRTThemeData.Danger, color, "CriticalColor (3s)");
        }

        // ── Color migration: QuantitySelector uses CRTThemeData ───────────

        [Test]
        public void QuantitySelector_InactiveButtonColor_MatchesCRTPanel()
        {
            AssertColorEqual(CRTThemeData.Panel, QuantitySelector.InactiveButtonColor, "InactiveButtonColor");
        }

        // ── TierVisualData: CRT base colors unaffected by tier ────────────

        [Test]
        public void TierVisualData_PennyTheme_ChartLineIsNeonGreen()
        {
            var theme = TierVisualData.GetTheme(StockTier.Penny);
            Assert.AreEqual(0f, theme.ChartLineColor.r, Tolerance);
            Assert.AreEqual(1f, theme.ChartLineColor.g, Tolerance);
            Assert.AreEqual(0.533f, theme.ChartLineColor.b, Tolerance);
        }

        [Test]
        public void GetProfitColor_Positive_MatchesCRTTextHigh()
        {
            var color = TradingHUD.GetProfitColor(100f);
            AssertColorEqual(CRTThemeData.TextHigh, color, "Positive profit should use CRT TextHigh");
        }

        [Test]
        public void GetProfitColor_Negative_MatchesCRTDanger()
        {
            var color = TradingHUD.GetProfitColor(-50f);
            AssertColorEqual(CRTThemeData.Danger, color, "Negative profit should use CRT Danger");
        }

        [Test]
        public void GetTargetBarColor_OnPace_MatchesCRTTextHigh()
        {
            var color = TradingHUD.GetTargetBarColor(0.6f, 0.5f);
            AssertColorEqual(CRTThemeData.TextHigh, color, "On pace should use CRT TextHigh");
        }

        [Test]
        public void GetTargetBarColor_FallingBehind_MatchesCRTWarning()
        {
            var color = TradingHUD.GetTargetBarColor(0.4f, 0.6f);
            AssertColorEqual(CRTThemeData.Warning, color, "Falling behind should use CRT Warning");
        }

        [Test]
        public void GetTargetBarColor_SignificantlyBehind_MatchesCRTDanger()
        {
            var color = TradingHUD.GetTargetBarColor(0.1f, 0.8f);
            AssertColorEqual(CRTThemeData.Danger, color, "Significantly behind should use CRT Danger");
        }

        // ── Helper ────────────────────────────────────────────────────────

        private static void AssertColorEqual(Color expected, Color actual, string name)
        {
            Assert.AreEqual(expected.r, actual.r, Tolerance, $"{name}.r");
            Assert.AreEqual(expected.g, actual.g, Tolerance, $"{name}.g");
            Assert.AreEqual(expected.b, actual.b, Tolerance, $"{name}.b");
            Assert.AreEqual(expected.a, actual.a, Tolerance, $"{name}.a");
        }
    }
}
