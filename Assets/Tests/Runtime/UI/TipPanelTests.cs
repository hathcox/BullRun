using NUnit.Framework;
using UnityEngine;

namespace BullRun.Tests.UI
{
    [TestFixture]
    public class TipPanelTests
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

        // --- FormatCountdownText ---

        [Test]
        public void FormatCountdownText_PositiveCount_ReturnsEventsX()
        {
            Assert.AreEqual("EVENTS: 5", TipPanel.FormatCountdownText(5));
        }

        [Test]
        public void FormatCountdownText_Zero_ReturnsAllClear()
        {
            Assert.AreEqual("ALL CLEAR", TipPanel.FormatCountdownText(0));
        }

        [Test]
        public void FormatCountdownText_Negative_ReturnsEmpty()
        {
            Assert.AreEqual("", TipPanel.FormatCountdownText(-1));
        }

        [Test]
        public void FormatCountdownText_One_ReturnsEvents1()
        {
            Assert.AreEqual("EVENTS: 1", TipPanel.FormatCountdownText(1));
        }

        [Test]
        public void FormatCountdownText_LargeCount_FormatsCorrectly()
        {
            Assert.AreEqual("EVENTS: 15", TipPanel.FormatCountdownText(15));
        }

        // --- GetBadgeAbbreviation ---

        [Test]
        public void GetBadgeAbbreviation_PriceFloor_ReturnsFLR()
        {
            Assert.AreEqual("FLR", TipPanel.GetBadgeAbbreviation(InsiderTipType.PriceFloor));
        }

        [Test]
        public void GetBadgeAbbreviation_PriceCeiling_ReturnsCLG()
        {
            Assert.AreEqual("CLG", TipPanel.GetBadgeAbbreviation(InsiderTipType.PriceCeiling));
        }

        [Test]
        public void GetBadgeAbbreviation_PriceForecast_ReturnsFC()
        {
            Assert.AreEqual("FC", TipPanel.GetBadgeAbbreviation(InsiderTipType.PriceForecast));
        }

        [Test]
        public void GetBadgeAbbreviation_DipMarker_ReturnsDIP()
        {
            Assert.AreEqual("DIP", TipPanel.GetBadgeAbbreviation(InsiderTipType.DipMarker));
        }

        [Test]
        public void GetBadgeAbbreviation_PeakMarker_ReturnsPK()
        {
            Assert.AreEqual("PK", TipPanel.GetBadgeAbbreviation(InsiderTipType.PeakMarker));
        }

        [Test]
        public void GetBadgeAbbreviation_ClosingDirection_ReturnsDIR()
        {
            Assert.AreEqual("DIR", TipPanel.GetBadgeAbbreviation(InsiderTipType.ClosingDirection));
        }

        [Test]
        public void GetBadgeAbbreviation_EventTiming_ReturnsET()
        {
            Assert.AreEqual("ET", TipPanel.GetBadgeAbbreviation(InsiderTipType.EventTiming));
        }

        [Test]
        public void GetBadgeAbbreviation_TrendReversal_ReturnsTR()
        {
            Assert.AreEqual("TR", TipPanel.GetBadgeAbbreviation(InsiderTipType.TrendReversal));
        }

        [Test]
        public void GetBadgeAbbreviation_EventCount_ReturnsEVT()
        {
            Assert.AreEqual("EVT", TipPanel.GetBadgeAbbreviation(InsiderTipType.EventCount));
        }

        // --- GetBadgeColor ---

        [Test]
        public void GetBadgeColor_PriceFloor_ReturnsCyan()
        {
            Assert.AreEqual(ColorPalette.Cyan, TipPanel.GetBadgeColor(InsiderTipType.PriceFloor));
        }

        [Test]
        public void GetBadgeColor_PriceCeiling_ReturnsAmber()
        {
            Assert.AreEqual(ColorPalette.Amber, TipPanel.GetBadgeColor(InsiderTipType.PriceCeiling));
        }

        [Test]
        public void GetBadgeColor_DipMarker_ReturnsGreen()
        {
            Assert.AreEqual(ColorPalette.Green, TipPanel.GetBadgeColor(InsiderTipType.DipMarker));
        }

        [Test]
        public void GetBadgeColor_PeakMarker_ReturnsAmber()
        {
            Assert.AreEqual(ColorPalette.Amber, TipPanel.GetBadgeColor(InsiderTipType.PeakMarker));
        }

        [Test]
        public void GetBadgeColor_EventTiming_ReturnsRed()
        {
            Assert.AreEqual(ColorPalette.Red, TipPanel.GetBadgeColor(InsiderTipType.EventTiming));
        }

        [Test]
        public void GetBadgeColor_EventCount_ReturnsGreen()
        {
            Assert.AreEqual(ColorPalette.Green, TipPanel.GetBadgeColor(InsiderTipType.EventCount));
        }

        [Test]
        public void GetBadgeColor_PriceForecast_ReturnsCyan()
        {
            Assert.AreEqual(ColorPalette.Cyan, TipPanel.GetBadgeColor(InsiderTipType.PriceForecast));
        }

        [Test]
        public void GetBadgeColor_ClosingDirection_ReturnsWhite()
        {
            Assert.AreEqual(ColorPalette.White, TipPanel.GetBadgeColor(InsiderTipType.ClosingDirection));
        }

        [Test]
        public void GetBadgeColor_TrendReversal_ReturnsCyan()
        {
            Assert.AreEqual(ColorPalette.Cyan, TipPanel.GetBadgeColor(InsiderTipType.TrendReversal));
        }

        // --- All types have non-default badge abbreviations ---

        [Test]
        public void GetBadgeAbbreviation_AllNineTypes_ReturnNonQuestionMark()
        {
            var types = new[]
            {
                InsiderTipType.PriceForecast,
                InsiderTipType.PriceFloor,
                InsiderTipType.PriceCeiling,
                InsiderTipType.EventCount,
                InsiderTipType.DipMarker,
                InsiderTipType.PeakMarker,
                InsiderTipType.ClosingDirection,
                InsiderTipType.EventTiming,
                InsiderTipType.TrendReversal
            };
            foreach (var type in types)
            {
                string abbrev = TipPanel.GetBadgeAbbreviation(type);
                Assert.AreNotEqual("?", abbrev,
                    $"Type {type} should have a defined abbreviation");
                Assert.IsTrue(abbrev.Length >= 2 && abbrev.Length <= 3,
                    $"Type {type} abbreviation '{abbrev}' should be 2-3 chars");
            }
        }

        // --- Countdown boundary: zero stays ALL CLEAR ---

        [Test]
        public void FormatCountdownText_AtZero_StaysAllClear()
        {
            // Verifies the format output at zero boundary.
            // The MonoBehaviour handler guards against decrementing below 0
            // via `if (_eventCountdown <= 0) return;` — that guard requires
            // PlayMode testing since it depends on EventBus subscription state.
            Assert.AreEqual("ALL CLEAR", TipPanel.FormatCountdownText(0));
        }

        // --- PulseDuration and PulseScale are sane ---

        [Test]
        public void PulseDuration_IsPositive()
        {
            Assert.Greater(TipPanel.PulseDuration, 0f);
            Assert.LessOrEqual(TipPanel.PulseDuration, 1f);
        }

        [Test]
        public void PulseScale_IsGreaterThanOne()
        {
            Assert.Greater(TipPanel.PulseScale, 1f);
            Assert.LessOrEqual(TipPanel.PulseScale, 2f);
        }
    }
}
