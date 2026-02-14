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

        // --- Color Constants ---

        [Test]
        public void LongColor_IsNeonGreen()
        {
            Assert.AreEqual(0f, PositionOverlay.LongColor.r, 0.01f);
            Assert.AreEqual(1f, PositionOverlay.LongColor.g, 0.01f);
            Assert.AreEqual(0.533f, PositionOverlay.LongColor.b, 0.01f);
        }

        [Test]
        public void ShortColor_IsHotPink()
        {
            Assert.AreEqual(1f, PositionOverlay.ShortColor.r, 0.01f);
            Assert.AreEqual(0.4f, PositionOverlay.ShortColor.g, 0.01f);
            Assert.AreEqual(0.7f, PositionOverlay.ShortColor.b, 0.01f);
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
