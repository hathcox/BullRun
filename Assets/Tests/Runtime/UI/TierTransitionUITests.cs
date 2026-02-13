using NUnit.Framework;
using UnityEngine;

namespace BullRun.Tests.UI
{
    [TestFixture]
    public class TierTransitionUITests
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

        // --- Taglines ---

        [Test]
        public void GetTagline_Act1_ReturnsPennyPitTagline()
        {
            string tagline = TierTransitionUI.GetTagline(1);
            Assert.AreEqual("The Penny Pit \u2014 Where Fortunes Begin", tagline);
        }

        [Test]
        public void GetTagline_Act2_ReturnsRisingStakesTagline()
        {
            string tagline = TierTransitionUI.GetTagline(2);
            Assert.AreEqual("Rising Stakes \u2014 Trends and Reversals", tagline);
        }

        [Test]
        public void GetTagline_Act3_ReturnsTradingFloorTagline()
        {
            string tagline = TierTransitionUI.GetTagline(3);
            Assert.AreEqual("The Trading Floor \u2014 Sectors in Motion", tagline);
        }

        [Test]
        public void GetTagline_Act4_ReturnsBlueChipTagline()
        {
            string tagline = TierTransitionUI.GetTagline(4);
            Assert.AreEqual("Blue Chip Arena \u2014 The Big Leagues", tagline);
        }

        [Test]
        public void GetTagline_InvalidAct0_ReturnsEmptyString()
        {
            string tagline = TierTransitionUI.GetTagline(0);
            Assert.AreEqual("", tagline);
        }

        [Test]
        public void GetTagline_InvalidAct5_ReturnsEmptyString()
        {
            string tagline = TierTransitionUI.GetTagline(5);
            Assert.AreEqual("", tagline);
        }

        // --- Display Text Parts ---

        [Test]
        public void GetActHeader_Act2_FormatsCorrectly()
        {
            string header = TierTransitionUI.GetActHeader(2);
            Assert.AreEqual("ACT 2", header);
        }

        [Test]
        public void GetTierSubtitle_Act1_ReturnsPennyStocks()
        {
            string subtitle = TierTransitionUI.GetTierSubtitle(1);
            Assert.AreEqual("PENNY STOCKS", subtitle);
        }

        [Test]
        public void GetTierSubtitle_Act2_ReturnsLowValueStocks()
        {
            string subtitle = TierTransitionUI.GetTierSubtitle(2);
            Assert.AreEqual("LOW-VALUE STOCKS", subtitle);
        }

        [Test]
        public void GetTierSubtitle_Act3_ReturnsMidValueStocks()
        {
            string subtitle = TierTransitionUI.GetTierSubtitle(3);
            Assert.AreEqual("MID-VALUE STOCKS", subtitle);
        }

        [Test]
        public void GetTierSubtitle_Act4_ReturnsBlueChips()
        {
            string subtitle = TierTransitionUI.GetTierSubtitle(4);
            Assert.AreEqual("BLUE CHIPS", subtitle);
        }

        // --- Animation Timing ---

        [Test]
        public void FadeInDuration_Is0Point5Seconds()
        {
            Assert.AreEqual(0.5f, TierTransitionUI.FadeInDuration);
        }

        [Test]
        public void HoldDuration_Is2Seconds()
        {
            Assert.AreEqual(2f, TierTransitionUI.HoldDuration);
        }

        [Test]
        public void FadeOutDuration_Is0Point5Seconds()
        {
            Assert.AreEqual(0.5f, TierTransitionUI.FadeOutDuration);
        }

        [Test]
        public void TotalDuration_Is3Seconds()
        {
            float total = TierTransitionUI.FadeInDuration + TierTransitionUI.HoldDuration + TierTransitionUI.FadeOutDuration;
            Assert.AreEqual(3f, total);
        }

        // --- Alpha calculation ---

        [Test]
        public void CalculateAlpha_AtStart_IsZero()
        {
            float alpha = TierTransitionUI.CalculateAlpha(0f);
            Assert.AreEqual(0f, alpha, 0.01f);
        }

        [Test]
        public void CalculateAlpha_HalfwayThroughFadeIn_IsHalf()
        {
            float alpha = TierTransitionUI.CalculateAlpha(0.25f);
            Assert.AreEqual(0.5f, alpha, 0.01f);
        }

        [Test]
        public void CalculateAlpha_EndOfFadeIn_IsOne()
        {
            float alpha = TierTransitionUI.CalculateAlpha(0.5f);
            Assert.AreEqual(1f, alpha, 0.01f);
        }

        [Test]
        public void CalculateAlpha_DuringHold_IsOne()
        {
            float alpha = TierTransitionUI.CalculateAlpha(1.5f);
            Assert.AreEqual(1f, alpha, 0.01f);
        }

        [Test]
        public void CalculateAlpha_HalfwayThroughFadeOut_IsHalf()
        {
            float alpha = TierTransitionUI.CalculateAlpha(2.75f);
            Assert.AreEqual(0.5f, alpha, 0.01f);
        }

        [Test]
        public void CalculateAlpha_AtEnd_IsZero()
        {
            float alpha = TierTransitionUI.CalculateAlpha(3f);
            Assert.AreEqual(0f, alpha, 0.01f);
        }

        [Test]
        public void CalculateAlpha_PastEnd_IsZero()
        {
            float alpha = TierTransitionUI.CalculateAlpha(4f);
            Assert.AreEqual(0f, alpha, 0.01f);
        }

        // --- IsShowing ---

        [Test]
        public void IsShowing_InitiallyFalse()
        {
            // TierTransitionUI.IsShowing is a static property; reset by testing it fresh
            Assert.IsFalse(TierTransitionUI.IsShowing);
        }
    }
}
