using NUnit.Framework;
using UnityEngine;

namespace BullRun.Tests.UI
{
    [TestFixture]
    public class PositionOverlayTests
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

        // --- Color Logic ---

        [Test]
        public void GetPnLColor_Positive_ReturnsProfitGreen()
        {
            Assert.AreEqual(PositionOverlay.ProfitGreen, PositionOverlay.GetPnLColor(10f));
        }

        [Test]
        public void GetPnLColor_Negative_ReturnsLossRed()
        {
            Assert.AreEqual(PositionOverlay.LossRed, PositionOverlay.GetPnLColor(-5f));
        }

        [Test]
        public void GetPnLColor_Zero_ReturnsWhite()
        {
            Assert.AreEqual(Color.white, PositionOverlay.GetPnLColor(0f));
        }

        // --- Direction Formatting ---

        [Test]
        public void FormatDirection_Long_ShowsSharesAndLong()
        {
            Assert.AreEqual("15x LONG", PositionOverlay.FormatDirection(15, true));
        }

        [Test]
        public void FormatDirection_Short_ShowsSharesAndShort()
        {
            Assert.AreEqual("5x SHORT", PositionOverlay.FormatDirection(5, false));
        }

        [Test]
        public void FormatDirection_SingleShare_Long()
        {
            Assert.AreEqual("1x LONG", PositionOverlay.FormatDirection(1, true));
        }

        // --- Flat Formatting ---

        [Test]
        public void FormatFlat_ReturnsFLAT()
        {
            Assert.AreEqual("FLAT", PositionOverlay.FormatFlat());
        }

        // --- Color Constants (Story 14.6: migrated to CRTThemeData) ---

        [Test]
        public void LongColor_MatchesCRTTextHigh()
        {
            Assert.AreEqual(CRTThemeData.TextHigh.r, PositionOverlay.LongColor.r, 0.01f);
            Assert.AreEqual(CRTThemeData.TextHigh.g, PositionOverlay.LongColor.g, 0.01f);
            Assert.AreEqual(CRTThemeData.TextHigh.b, PositionOverlay.LongColor.b, 0.01f);
        }

        [Test]
        public void ShortColor_MatchesCRTWarning()
        {
            Assert.AreEqual(CRTThemeData.Warning.r, PositionOverlay.ShortColor.r, 0.01f);
            Assert.AreEqual(CRTThemeData.Warning.g, PositionOverlay.ShortColor.g, 0.01f);
            Assert.AreEqual(CRTThemeData.Warning.b, PositionOverlay.ShortColor.b, 0.01f);
        }

        [Test]
        public void FlatColor_IsGray()
        {
            Assert.AreEqual(0.5f, PositionOverlay.FlatColor.r, 0.01f);
            Assert.AreEqual(0.5f, PositionOverlay.FlatColor.g, 0.01f);
            Assert.AreEqual(0.55f, PositionOverlay.FlatColor.b, 0.01f);
        }
    }
}
