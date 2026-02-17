using NUnit.Framework;
using UnityEngine;

namespace BullRun.Tests.UI
{
    [TestFixture]
    public class RoundTimerUITests
    {
        // --- FormatTime tests ---

        [Test]
        public void FormatTime_60Seconds_Returns1Colon00()
        {
            Assert.AreEqual("1:00", RoundTimerUI.FormatTime(60f));
        }

        [Test]
        public void FormatTime_45Seconds_Returns0Colon45()
        {
            Assert.AreEqual("0:45", RoundTimerUI.FormatTime(45f));
        }

        [Test]
        public void FormatTime_5Seconds_Returns0Colon05()
        {
            Assert.AreEqual("0:05", RoundTimerUI.FormatTime(5f));
        }

        [Test]
        public void FormatTime_0Seconds_Returns0Colon00()
        {
            Assert.AreEqual("0:00", RoundTimerUI.FormatTime(0f));
        }

        [Test]
        public void FormatTime_NegativeSeconds_ClampedTo0Colon00()
        {
            Assert.AreEqual("0:00", RoundTimerUI.FormatTime(-5f));
        }

        [Test]
        public void FormatTime_FractionalSeconds_CeilsUp()
        {
            // 44.3 seconds → ceils to 45
            Assert.AreEqual("0:45", RoundTimerUI.FormatTime(44.3f));
        }

        [Test]
        public void FormatTime_90Seconds_Returns1Colon30()
        {
            Assert.AreEqual("1:30", RoundTimerUI.FormatTime(90f));
        }

        // --- GetTimerColor tests (Story 14.6: migrated to CRTThemeData) ---

        [Test]
        public void GetTimerColor_Above15s_ReturnsCRTTextHigh()
        {
            Color color = RoundTimerUI.GetTimerColor(30f);
            Assert.AreEqual(CRTThemeData.TextHigh, color);
        }

        [Test]
        public void GetTimerColor_At15s_ReturnsCRTWarning()
        {
            Color color = RoundTimerUI.GetTimerColor(15f);
            Assert.AreEqual(CRTThemeData.Warning, color);
        }

        [Test]
        public void GetTimerColor_Between15And5_ReturnsCRTWarning()
        {
            Color color = RoundTimerUI.GetTimerColor(10f);
            Assert.AreEqual(CRTThemeData.Warning, color);
        }

        [Test]
        public void GetTimerColor_At5s_ReturnsCRTDanger()
        {
            Color color = RoundTimerUI.GetTimerColor(5f);
            Assert.AreEqual(CRTThemeData.Danger, color);
        }

        [Test]
        public void GetTimerColor_Below5s_ReturnsCRTDanger()
        {
            Color color = RoundTimerUI.GetTimerColor(2f);
            Assert.AreEqual(CRTThemeData.Danger, color);
        }

        [Test]
        public void GetTimerColor_At0s_ReturnsCRTDanger()
        {
            Color color = RoundTimerUI.GetTimerColor(0f);
            Assert.AreEqual(CRTThemeData.Danger, color);
        }

        // --- GetProgressFraction tests ---

        [Test]
        public void GetProgressFraction_FullTime_Returns1()
        {
            Assert.AreEqual(1f, RoundTimerUI.GetProgressFraction(60f, 60f), 0.001f);
        }

        [Test]
        public void GetProgressFraction_HalfTime_Returns0Point5()
        {
            Assert.AreEqual(0.5f, RoundTimerUI.GetProgressFraction(30f, 60f), 0.001f);
        }

        [Test]
        public void GetProgressFraction_NoTimeLeft_Returns0()
        {
            Assert.AreEqual(0f, RoundTimerUI.GetProgressFraction(0f, 60f), 0.001f);
        }

        [Test]
        public void GetProgressFraction_ZeroDuration_Returns0()
        {
            Assert.AreEqual(0f, RoundTimerUI.GetProgressFraction(30f, 0f), 0.001f);
        }

        [Test]
        public void GetProgressFraction_ClampedAtMax1()
        {
            Assert.AreEqual(1f, RoundTimerUI.GetProgressFraction(90f, 60f), 0.001f);
        }
    }
}
