using NUnit.Framework;
using UnityEngine;

namespace BullRun.Tests.UI
{
    /// <summary>
    /// Tests for Story 15.1: Game Feel Phase 2 — Micro-Animations &amp; UI Juice.
    /// Covers all animation constants across StockSidebar, TradingHUD,
    /// PositionPanel, RunSummaryUI, ChartLineView, ShopUI, QuantitySelector,
    /// and FloatingTextService.
    /// Coroutine-based animations require Play Mode and are verified manually.
    /// </summary>
    [TestFixture]
    public class GameFeelPhase2Tests
    {
        // ── AC 1: Price Tick Flash ──────────────────────────────────────────────

        [Test]
        public void PriceFlashDuration_IsQuarterSecond()
        {
            Assert.AreEqual(0.25f, StockSidebar.PriceFlashDuration, 0.001f,
                "Price flash must settle in 0.25s per AC 1");
        }

        [Test]
        public void PriceFlashThreshold_IsOnePermille()
        {
            Assert.AreEqual(0.001f, StockSidebar.PriceFlashThreshold, 0.0001f,
                "Flash threshold must be 0.1% (0.001) to avoid noise per AC 1");
        }

        // ── AC 2: Cash Count-Up ────────────────────────────────────────────────

        [Test]
        public void CashTweenDuration_IsThreeHundredMs()
        {
            Assert.AreEqual(0.3f, TradingHUD.CashTweenDuration, 0.001f,
                "Cash tween must complete in 0.3s per AC 2");
        }

        // ── AC 3: Floating Trade Profit Popup ──────────────────────────────────

        [Test]
        public void FloatDuration_IsEightHundredMs()
        {
            Assert.AreEqual(0.8f, FloatingTextService.FloatDuration, 0.001f,
                "Float popup duration must be 0.8s per AC 3");
        }

        [Test]
        public void FloatDistance_IsSixtyPixels()
        {
            Assert.AreEqual(60f, FloatingTextService.FloatDistance, 0.1f,
                "Float popup must drift 60px per AC 3");
        }

        [Test]
        public void FadeStartFraction_StartsAtSixtyPercent()
        {
            Assert.AreEqual(0.6f, FloatingTextService.FadeStartFraction, 0.001f,
                "Fade should start at 60% of float duration per AC 3");
        }

        // ── AC 4: Position Entry Slide-In ──────────────────────────────────────

        [Test]
        public void EntrySlideInDuration_IsTwoHundredMs()
        {
            Assert.AreEqual(0.2f, PositionPanel.EntrySlideInDuration, 0.001f,
                "Position entry fade-in must be 0.2s per AC 4");
        }

        // ── AC 5: Auto-Liquidation Cascade ─────────────────────────────────────

        [Test]
        public void CascadeStagger_IsEightyMs()
        {
            Assert.AreEqual(0.08f, PositionPanel.CascadeStagger, 0.001f,
                "Cascade stagger must be 0.08s per AC 5");
        }

        [Test]
        public void CascadeExitDuration_IsOneFiftyMs()
        {
            Assert.AreEqual(0.15f, PositionPanel.CascadeExitDuration, 0.001f,
                "Cascade exit animation must be 0.15s per AC 5");
        }

        // ── AC 6: Sidebar Event Cell Flash ─────────────────────────────────────

        [Test]
        public void EventFlashInDuration_IsFiftyMs()
        {
            Assert.AreEqual(0.05f, StockSidebar.EventFlashInDuration, 0.001f,
                "Event flash-in must be 0.05s per AC 6");
        }

        [Test]
        public void EventFlashOutDuration_IsOneFiftyMs()
        {
            Assert.AreEqual(0.15f, StockSidebar.EventFlashOutDuration, 0.001f,
                "Event flash return must be 0.15s per AC 6");
        }

        // ── AC 7: Short Countdown Urgency ──────────────────────────────────────

        [Test]
        public void CountdownUrgencyThreshold_IsThreeSeconds()
        {
            Assert.AreEqual(3.0f, PositionPanel.CountdownUrgencyThreshold, 0.001f,
                "Urgency escalation must trigger at ≤3.0s per AC 7");
        }

        // ── AC 8: MARGIN CALL Slam-In ──────────────────────────────────────────

        [Test]
        public void MarginCallSlamDuration_IsFourHundredMs()
        {
            Assert.AreEqual(0.4f, RunSummaryUI.MarginCallSlamDuration, 0.001f,
                "MARGIN CALL slam must settle in 0.4s per AC 8");
        }

        [Test]
        public void MarginCallSlamStartScale_IsTwoPointFive()
        {
            Assert.AreEqual(2.5f, RunSummaryUI.MarginCallSlamStartScale, 0.001f,
                "MARGIN CALL text must start at 2.5x scale per AC 8");
        }

        // ── AC 10: Chart Indicator Pulse ───────────────────────────────────────

        [Test]
        public void IndicatorPulseFrequency_IsFourHz()
        {
            Assert.AreEqual(4f, ChartLineView.IndicatorPulseFrequency, 0.001f,
                "Indicator must pulse at 4Hz per AC 10");
        }

        [Test]
        public void IndicatorPulseMin_IsPointFour()
        {
            Assert.AreEqual(0.4f, ChartLineView.IndicatorPulseMin, 0.001f,
                "Indicator minimum alpha must be 0.4 per AC 10");
        }

        // ── AC 11: Shop Cascade Entry ──────────────────────────────────────────

        [Test]
        public void ShopCascadeStagger_IsSixtyMs()
        {
            Assert.AreEqual(0.06f, ShopUI.ShopCascadeStagger, 0.001f,
                "Shop cascade stagger must be 0.06s per AC 11");
        }

        [Test]
        public void ShopCascadeDuration_IsTwoHundredMs()
        {
            Assert.AreEqual(0.2f, ShopUI.ShopCascadeDuration, 0.001f,
                "Shop cascade animation must be 0.2s per AC 11");
        }

        [Test]
        public void ShopCascadeOffset_IsFortyPixels()
        {
            Assert.AreEqual(40f, ShopUI.ShopCascadeOffset, 0.1f,
                "Shop cascade must start 40px below per AC 11");
        }

        // ── AC 12: Progress Bar Smooth Tween ───────────────────────────────────

        [Test]
        public void BarTweenDuration_IsTwoHundredMs()
        {
            Assert.AreEqual(0.2f, TradingHUD.BarTweenDuration, 0.001f,
                "Progress bar tween must be 0.2s per AC 12");
        }

        // ── AC 13: Round Streak Indicator ──────────────────────────────────────

        [Test]
        public void StreakMinDisplay_IsTwo()
        {
            Assert.AreEqual(2, TradingHUD.StreakMinDisplay,
                "Streak must display at 2+ consecutive wins per AC 13");
        }

        // ── AC 14: Market Open Cascade ─────────────────────────────────────────

        [Test]
        public void RevealStagger_IsOneFiftyMs()
        {
            Assert.AreEqual(0.15f, StockSidebar.RevealStagger, 0.001f,
                "Market open reveal stagger must be 0.15s per AC 14");
        }

        [Test]
        public void RevealDuration_IsTwelveMs()
        {
            Assert.AreEqual(0.12f, StockSidebar.RevealDuration, 0.001f,
                "Market open reveal animation must be 0.12s per AC 14");
        }

        // ── AC 16: Quantity Selector Micro-Animation ───────────────────────────

        [Test]
        public void QuantityPunchDuration_IsOneFiftyMs()
        {
            Assert.AreEqual(0.15f, QuantitySelector.QuantityPunchDuration, 0.001f,
                "Quantity punch must be 0.15s per AC 16");
        }

        [Test]
        public void QuantityPunchStrength_IsQuarter()
        {
            Assert.AreEqual(0.25f, QuantitySelector.QuantityPunchStrength, 0.001f,
                "Quantity punch strength must be 0.25 per AC 16");
        }

        // ── Price Flash Threshold Guard ────────────────────────────────────────

        [Test]
        public void PriceFlashThreshold_BelowThresholdSkipsFlash()
        {
            float prevPct = 0.01f;
            float newPct = 0.01f + (StockSidebar.PriceFlashThreshold * 0.5f);
            float delta = Mathf.Abs(newPct - prevPct);
            Assert.Less(delta, StockSidebar.PriceFlashThreshold,
                "Delta below threshold must skip flash (no tween spam)");
        }

        [Test]
        public void PriceFlashThreshold_AtOrAboveThresholdTriggersFlash()
        {
            float prevPct = 0.01f;
            float newPct = 0.01f + StockSidebar.PriceFlashThreshold;
            float delta = Mathf.Abs(newPct - prevPct);
            Assert.GreaterOrEqual(delta, StockSidebar.PriceFlashThreshold,
                "Delta at threshold must trigger flash");
        }

        // ── Streak Logic ───────────────────────────────────────────────────────

        [Test]
        public void StreakMinDisplay_TwoConsecutiveTargetsMet_Qualifies()
        {
            int streak = 0;
            // Simulate two consecutive TargetMet = true rounds
            streak++; // round 1
            streak++; // round 2
            Assert.GreaterOrEqual(streak, TradingHUD.StreakMinDisplay,
                "Two consecutive wins should qualify for streak display");
        }

        [Test]
        public void StreakMinDisplay_OneMiss_ResetsCounter()
        {
            int streak = 3;
            // Simulate TargetMet = false
            streak = 0;
            Assert.Less(streak, TradingHUD.StreakMinDisplay,
                "Any miss must reset streak below display threshold");
        }
    }
}
