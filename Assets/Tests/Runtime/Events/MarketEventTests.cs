using System.Collections.Generic;
using NUnit.Framework;

namespace BullRun.Tests.Events
{
    [TestFixture]
    public class MarketEventTests
    {
        [Test]
        public void Constructor_SetsAllFields()
        {
            var evt = new MarketEvent(MarketEventType.EarningsBeat, 3, 0.25f, 5f);

            Assert.AreEqual(MarketEventType.EarningsBeat, evt.EventType);
            Assert.AreEqual(3, evt.TargetStockId);
            Assert.AreEqual(0.25f, evt.PriceEffectPercent, 0.001f);
            Assert.AreEqual(5f, evt.Duration, 0.001f);
        }

        [Test]
        public void Constructor_GlobalEvent_HasNullTargetStockId()
        {
            var evt = new MarketEvent(MarketEventType.MarketCrash, null, -0.30f, 8f);

            Assert.IsNull(evt.TargetStockId);
            Assert.AreEqual(MarketEventType.MarketCrash, evt.EventType);
        }

        [Test]
        public void IsActive_TrueWhenElapsedLessThanDuration()
        {
            var evt = new MarketEvent(MarketEventType.EarningsBeat, 0, 0.25f, 5f);

            Assert.IsTrue(evt.IsActive);
        }

        [Test]
        public void IsActive_FalseWhenElapsedExceedsDuration()
        {
            var evt = new MarketEvent(MarketEventType.EarningsBeat, 0, 0.25f, 5f);
            evt.ElapsedTime = 5.1f;

            Assert.IsFalse(evt.IsActive);
        }

        [Test]
        public void IsActive_FalseWhenElapsedEqualsDuration()
        {
            var evt = new MarketEvent(MarketEventType.EarningsBeat, 0, 0.25f, 5f);
            evt.ElapsedTime = 5f;

            Assert.IsFalse(evt.IsActive);
        }

        [Test]
        public void GetCurrentForce_AtStart_ReturnsZero()
        {
            var evt = new MarketEvent(MarketEventType.EarningsBeat, 0, 0.25f, 5f);
            evt.ElapsedTime = 0f;

            float force = evt.GetCurrentForce();

            Assert.AreEqual(0f, force, 0.01f, "Force should be 0 at start (ramp up)");
        }

        [Test]
        public void GetCurrentForce_AtPeak_ReturnsMaximum()
        {
            var evt = new MarketEvent(MarketEventType.EarningsBeat, 0, 0.25f, 4f);
            // Peak at midpoint (2s into 4s duration)
            evt.ElapsedTime = 2f;

            float force = evt.GetCurrentForce();

            Assert.AreEqual(1f, force, 0.01f, "Force should be 1.0 at peak");
        }

        [Test]
        public void GetCurrentForce_AtEnd_ReturnsZero()
        {
            var evt = new MarketEvent(MarketEventType.EarningsBeat, 0, 0.25f, 4f);
            evt.ElapsedTime = 4f;

            float force = evt.GetCurrentForce();

            Assert.AreEqual(0f, force, 0.01f, "Force should be 0 at end (expired)");
        }

        [Test]
        public void GetCurrentForce_RampsUpThenSoftRelease()
        {
            // FIX-17: Tail-off is now 5% of duration, fading to 0.85 (not 0)
            var evt = new MarketEvent(MarketEventType.EarningsBeat, 0, 0.25f, 10f);

            // Early ramp phase (t=0.075, halfway through the 15% ramp)
            evt.ElapsedTime = 0.75f;
            float forceRamp = evt.GetCurrentForce();

            // Hold phase (t=0.5, well within the 15%-95% hold zone)
            evt.ElapsedTime = 5f;
            float forceHold = evt.GetCurrentForce();

            // Tail-off phase (t=0.975, halfway through the final 5%)
            evt.ElapsedTime = 9.75f;
            float forceTail = evt.GetCurrentForce();

            Assert.AreEqual(1f, forceHold, 0.01f, "Hold force should be 1.0");
            Assert.Greater(forceHold, forceRamp, "Hold force should be greater than ramp-up");
            Assert.Greater(forceHold, forceTail, "Hold force should be greater than tail-off");
            Assert.Greater(forceRamp, 0f, "Ramp-up should be > 0");
            Assert.GreaterOrEqual(forceTail, 0.85f, "Tail-off should not drop below 0.85");
            Assert.Less(forceTail, 1f, "Tail-off should be less than 1.0");
        }

        [Test]
        public void GetCurrentForce_TailOff_NeverDropsBelowPointEightFive()
        {
            // FIX-17: Force during tail-off stays between 0.85 and 1.0
            var evt = new MarketEvent(MarketEventType.EarningsBeat, 0, 0.25f, 10f);

            // Right at the end of the tail-off (t=0.999)
            evt.ElapsedTime = 9.99f;
            float forceAtEnd = evt.GetCurrentForce();

            Assert.GreaterOrEqual(forceAtEnd, 0.85f,
                "Force at end of tail-off should be >= 0.85");
        }

        [Test]
        public void GetCurrentForce_HoldPhaseExtendedTo95Percent()
        {
            // FIX-17: Hold zone now extends from 15% to 95% (was 85%)
            var evt = new MarketEvent(MarketEventType.EarningsBeat, 0, 0.25f, 10f);

            // At 90% through duration — was in tail-off before, now in hold
            evt.ElapsedTime = 9f;
            float force = evt.GetCurrentForce();

            Assert.AreEqual(1f, force, 0.01f,
                "At 90% of duration, force should be 1.0 (hold zone extended to 95%)");
        }

        [Test]
        public void GetCurrentForce_AfterDuration_ReturnsZero()
        {
            var evt = new MarketEvent(MarketEventType.EarningsBeat, 0, 0.25f, 5f);
            evt.ElapsedTime = 6f;

            float force = evt.GetCurrentForce();

            Assert.AreEqual(0f, force, 0.001f, "Force should be 0 after event has ended");
        }

        [Test]
        public void ElapsedTime_DefaultsToZero()
        {
            var evt = new MarketEvent(MarketEventType.EarningsBeat, 0, 0.25f, 5f);

            Assert.AreEqual(0f, evt.ElapsedTime, 0.001f);
        }

        [Test]
        public void IsGlobalEvent_TrueWhenTargetStockIdIsNull()
        {
            var evt = new MarketEvent(MarketEventType.MarketCrash, null, -0.30f, 8f);

            Assert.IsTrue(evt.IsGlobalEvent);
        }

        [Test]
        public void IsGlobalEvent_FalseWhenTargetStockIdIsSet()
        {
            var evt = new MarketEvent(MarketEventType.EarningsBeat, 2, 0.25f, 5f);

            Assert.IsFalse(evt.IsGlobalEvent);
        }

        // --- Multi-phase event tests (Story 5-3, Task 1) ---

        [Test]
        public void MultiPhase_NullPhases_SinglePhaseBackwardCompat()
        {
            // Single-phase events should work unchanged
            var evt = new MarketEvent(MarketEventType.EarningsBeat, 0, 0.25f, 5f);

            Assert.IsNull(evt.Phases, "Single-phase events should have null Phases");
            Assert.AreEqual(0, evt.CurrentPhaseIndex);
            Assert.AreEqual(0.25f, evt.PriceEffectPercent, 0.001f);
        }

        [Test]
        public void MultiPhase_Constructor_SetsPhases()
        {
            var phases = new List<MarketEventPhase>
            {
                new MarketEventPhase(0.80f, 5f),
                new MarketEventPhase(-1.20f, 3f)
            };

            var evt = new MarketEvent(MarketEventType.PumpAndDump, 0, 0.80f, 8f, phases);

            Assert.IsNotNull(evt.Phases);
            Assert.AreEqual(2, evt.Phases.Count);
            Assert.AreEqual(0, evt.CurrentPhaseIndex);
        }

        [Test]
        public void MultiPhase_GetCurrentPhaseTarget_ReturnsFirstPhaseInitially()
        {
            var phases = new List<MarketEventPhase>
            {
                new MarketEventPhase(0.80f, 5f),
                new MarketEventPhase(-1.20f, 3f)
            };

            var evt = new MarketEvent(MarketEventType.PumpAndDump, 0, 0.80f, 8f, phases);

            Assert.AreEqual(0.80f, evt.GetCurrentPhaseTarget(), 0.001f);
        }

        [Test]
        public void MultiPhase_TransitionsPhase_AtCorrectTime()
        {
            var phases = new List<MarketEventPhase>
            {
                new MarketEventPhase(0.80f, 5f),
                new MarketEventPhase(-1.20f, 3f)
            };

            var evt = new MarketEvent(MarketEventType.PumpAndDump, 0, 0.80f, 8f, phases);

            // During phase 0 (0-5s)
            evt.ElapsedTime = 3f;
            Assert.AreEqual(0, evt.CurrentPhaseIndex, "Should be in phase 0 at 3s");
            Assert.AreEqual(0.80f, evt.GetCurrentPhaseTarget(), 0.001f);

            // After phase 0 ends (5s+)
            evt.ElapsedTime = 5.1f;
            Assert.AreEqual(1, evt.CurrentPhaseIndex, "Should be in phase 1 at 5.1s");
            Assert.AreEqual(-1.20f, evt.GetCurrentPhaseTarget(), 0.001f);
        }

        [Test]
        public void MultiPhase_GetCurrentForce_UsesPhaseSpecificTiming()
        {
            var phases = new List<MarketEventPhase>
            {
                new MarketEventPhase(0.80f, 5f),
                new MarketEventPhase(-1.20f, 3f)
            };

            var evt = new MarketEvent(MarketEventType.PumpAndDump, 0, 0.80f, 8f, phases);

            // Mid-phase 0 (well into hold zone within phase 0)
            evt.ElapsedTime = 2.5f; // 50% through 5s phase
            float force0 = evt.GetCurrentForce();
            Assert.AreEqual(1f, force0, 0.05f, "Should be at full force mid-phase 0");

            // Start of phase 1 (should ramp from 0)
            evt.ElapsedTime = 5.0f;
            float force1Start = evt.GetCurrentForce();
            Assert.AreEqual(0f, force1Start, 0.05f, "Should be near 0 at phase 1 start");

            // Mid-phase 1
            evt.ElapsedTime = 6.5f; // 50% through 3s phase
            float force1Mid = evt.GetCurrentForce();
            Assert.AreEqual(1f, force1Mid, 0.05f, "Should be at full force mid-phase 1");
        }

        [Test]
        public void MultiPhase_SinglePhaseEvents_ForceUnchanged()
        {
            // Verify existing single-phase behavior is identical
            var evt = new MarketEvent(MarketEventType.EarningsBeat, 0, 0.25f, 10f);

            evt.ElapsedTime = 5f; // Hold zone
            float force = evt.GetCurrentForce();
            Assert.AreEqual(1f, force, 0.01f, "Single-phase force should be unchanged");
        }

        [Test]
        public void MarketEventPhase_StoresFields()
        {
            var phase = new MarketEventPhase(0.50f, 3.5f);

            Assert.AreEqual(0.50f, phase.TargetPricePercent, 0.001f);
            Assert.AreEqual(3.5f, phase.PhaseDuration, 0.001f);
        }
    }
}
