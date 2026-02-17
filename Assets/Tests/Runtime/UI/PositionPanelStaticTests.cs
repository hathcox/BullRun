using NUnit.Framework;
using UnityEngine;

namespace BullRun.Tests.UI
{
    [TestFixture]
    public class PositionPanelStaticTests
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
            Assert.AreEqual(PositionPanel.ProfitGreen, PositionPanel.GetPnLColor(10f));
        }

        [Test]
        public void GetPnLColor_Negative_ReturnsLossRed()
        {
            Assert.AreEqual(PositionPanel.LossRed, PositionPanel.GetPnLColor(-5f));
        }

        [Test]
        public void GetPnLColor_Zero_ReturnsWhite()
        {
            Assert.AreEqual(Color.white, PositionPanel.GetPnLColor(0f));
        }

        // --- Position Type Formatting ---

        [Test]
        public void FormatPositionType_Long_ReturnsLONG()
        {
            Assert.AreEqual("LONG", PositionPanel.FormatPositionType(true));
        }

        [Test]
        public void FormatPositionType_Short_ReturnsSHORT()
        {
            Assert.AreEqual("SHORT", PositionPanel.FormatPositionType(false));
        }

        // --- Color Constants (CRTThemeData) ---

        [Test]
        public void LongAccentColor_MatchesCRTTextHigh()
        {
            Assert.AreEqual(CRTThemeData.TextHigh.r, PositionPanel.LongAccentColor.r, 0.01f);
            Assert.AreEqual(CRTThemeData.TextHigh.g, PositionPanel.LongAccentColor.g, 0.01f);
            Assert.AreEqual(CRTThemeData.TextHigh.b, PositionPanel.LongAccentColor.b, 0.01f);
        }

        [Test]
        public void ShortAccentColor_MatchesCRTWarning()
        {
            Assert.AreEqual(CRTThemeData.Warning.r, PositionPanel.ShortAccentColor.r, 0.01f);
            Assert.AreEqual(CRTThemeData.Warning.g, PositionPanel.ShortAccentColor.g, 0.01f);
            Assert.AreEqual(CRTThemeData.Warning.b, PositionPanel.ShortAccentColor.b, 0.01f);
        }

        [Test]
        public void GetPositionTypeColor_Long_ReturnsLongAccent()
        {
            Assert.AreEqual(PositionPanel.LongAccentColor, PositionPanel.GetPositionTypeColor(true));
        }

        [Test]
        public void GetPositionTypeColor_Short_ReturnsShortAccent()
        {
            Assert.AreEqual(PositionPanel.ShortAccentColor, PositionPanel.GetPositionTypeColor(false));
        }
    }
}
