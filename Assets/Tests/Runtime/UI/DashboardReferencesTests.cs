using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BullRun.Tests.UI
{
    [TestFixture]
    public class DashboardReferencesTests
    {
        // --- Story 14.3: Verify DashboardReferences has all required fields ---

        [Test]
        public void DashboardReferences_HasLeftWingFields()
        {
            var refs = new DashboardReferences();
            // Left Wing fields should exist and be null by default
            Assert.IsNull(refs.CashText);
            Assert.IsNull(refs.ProfitText);
            Assert.IsNull(refs.TargetText);
            Assert.IsNull(refs.TargetProgressBar);
        }

        [Test]
        public void DashboardReferences_HasRightWingFields()
        {
            var refs = new DashboardReferences();
            // Right Wing fields should exist and be null by default
            Assert.IsNull(refs.DirectionText);
            Assert.IsNull(refs.AvgPriceText);
            Assert.IsNull(refs.PnLText);
            Assert.IsNull(refs.TimerText);
            Assert.IsNull(refs.TimerProgressBar);
            Assert.IsNull(refs.RepText);
        }

        [Test]
        public void DashboardReferences_HasRowGameObjects()
        {
            var refs = new DashboardReferences();
            // Row GameObjects for show/hide should exist and be null by default
            Assert.IsNull(refs.AvgPriceRow);
            Assert.IsNull(refs.PnlRow);
        }

        [Test]
        public void DashboardReferences_HasContainerTransforms()
        {
            var refs = new DashboardReferences();
            Assert.IsNull(refs.LeftWing);
            Assert.IsNull(refs.CenterCore);
            Assert.IsNull(refs.RightWing);
            Assert.IsNull(refs.ControlDeckPanel);
            Assert.IsNull(refs.ControlDeckCanvas);
        }

        // --- Story 14.3: Verify TradingHUD profit color wiring (static utility used by Left Wing) ---

        [Test]
        public void TradingHUD_GetProfitColor_Positive_ReturnsGreen()
        {
            Assert.AreEqual(TradingHUD.ProfitGreen, TradingHUD.GetProfitColor(100f));
        }

        [Test]
        public void TradingHUD_GetProfitColor_Negative_ReturnsRed()
        {
            Assert.AreEqual(TradingHUD.LossRed, TradingHUD.GetProfitColor(-50f));
        }

        [Test]
        public void TradingHUD_GetProfitColor_Zero_ReturnsWhite()
        {
            Assert.AreEqual(Color.white, TradingHUD.GetProfitColor(0f));
        }

        // --- Story 14.3: Verify target progress calculation (Left Wing target bar) ---

        [Test]
        public void TradingHUD_CalculateTargetProgress_HalfwayToTarget()
        {
            float progress = TradingHUD.CalculateTargetProgress(500f, 1000f);
            Assert.AreEqual(0.5f, progress, 0.001f);
        }

        [Test]
        public void TradingHUD_CalculateTargetProgress_AtTarget_ReturnsOne()
        {
            float progress = TradingHUD.CalculateTargetProgress(1000f, 1000f);
            Assert.AreEqual(1f, progress, 0.001f);
        }

        [Test]
        public void TradingHUD_CalculateTargetProgress_OverTarget_ClampedToOne()
        {
            float progress = TradingHUD.CalculateTargetProgress(2000f, 1000f);
            Assert.AreEqual(1f, progress, 0.001f);
        }

        [Test]
        public void TradingHUD_CalculateTargetProgress_ZeroTarget_ReturnsOne()
        {
            float progress = TradingHUD.CalculateTargetProgress(500f, 0f);
            Assert.AreEqual(1f, progress, 0.001f);
        }

        // --- Story 14.3: Verify target bar color logic (Left Wing progress bar colors) ---

        [Test]
        public void TradingHUD_GetTargetBarColor_OnPace_ReturnsGreen()
        {
            Color color = TradingHUD.GetTargetBarColor(0.6f, 0.5f);
            Assert.AreEqual(TradingHUD.ProfitGreen, color);
        }

        [Test]
        public void TradingHUD_GetTargetBarColor_SlightlyBehind_ReturnsYellow()
        {
            Color color = TradingHUD.GetTargetBarColor(0.4f, 0.6f);
            Assert.AreEqual(TradingHUD.WarningYellow, color);
        }

        [Test]
        public void TradingHUD_GetTargetBarColor_FarBehind_ReturnsRed()
        {
            Color color = TradingHUD.GetTargetBarColor(0.1f, 0.6f);
            Assert.AreEqual(TradingHUD.LossRed, color);
        }

        [Test]
        public void TradingHUD_GetTargetBarColor_ZeroProgress_ReturnsRed()
        {
            Color color = TradingHUD.GetTargetBarColor(0f, 0.5f);
            Assert.AreEqual(TradingHUD.LossRed, color);
        }

        // --- Story 14.3: Verify RoundTimerUI formatting (Right Wing timer) ---

        [Test]
        public void RoundTimerUI_FormatTime_FullMinutes()
        {
            Assert.AreEqual("2:00", RoundTimerUI.FormatTime(120f));
        }

        [Test]
        public void RoundTimerUI_FormatTime_PartialSeconds()
        {
            // 90.5s -> CeilToInt = 91s -> 1:31
            Assert.AreEqual("1:31", RoundTimerUI.FormatTime(90.5f));
        }

        [Test]
        public void RoundTimerUI_FormatTime_Zero()
        {
            Assert.AreEqual("0:00", RoundTimerUI.FormatTime(0f));
        }

        [Test]
        public void RoundTimerUI_FormatTime_Negative_ClampsToZero()
        {
            Assert.AreEqual("0:00", RoundTimerUI.FormatTime(-5f));
        }

        // --- Story 14.3: Verify timer color transitions (Right Wing urgency) ---

        [Test]
        public void RoundTimerUI_GetTimerColor_Normal_ReturnsGreen()
        {
            Color color = RoundTimerUI.GetTimerColor(30f);
            Assert.AreEqual(0f, color.r, 0.01f);
            Assert.AreEqual(1f, color.g, 0.01f);
        }

        [Test]
        public void RoundTimerUI_GetTimerColor_Urgency_ReturnsYellow()
        {
            Color color = RoundTimerUI.GetTimerColor(10f);
            Assert.AreEqual(1f, color.r, 0.01f);
            Assert.AreEqual(0.85f, color.g, 0.01f);
        }

        [Test]
        public void RoundTimerUI_GetTimerColor_Critical_ReturnsRed()
        {
            Color color = RoundTimerUI.GetTimerColor(3f);
            Assert.AreEqual(1f, color.r, 0.01f);
            Assert.AreEqual(0.2f, color.g, 0.01f);
        }

        // --- Story 14.3: Verify timer progress fraction (Right Wing progress bar) ---

        [Test]
        public void RoundTimerUI_GetProgressFraction_HalfTime()
        {
            float fraction = RoundTimerUI.GetProgressFraction(30f, 60f);
            Assert.AreEqual(0.5f, fraction, 0.001f);
        }

        [Test]
        public void RoundTimerUI_GetProgressFraction_Full()
        {
            float fraction = RoundTimerUI.GetProgressFraction(60f, 60f);
            Assert.AreEqual(1f, fraction, 0.001f);
        }

        [Test]
        public void RoundTimerUI_GetProgressFraction_ZeroDuration()
        {
            float fraction = RoundTimerUI.GetProgressFraction(10f, 0f);
            Assert.AreEqual(0f, fraction, 0.001f);
        }
    }
}
