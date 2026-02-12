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

            Assert.AreEqual(0f, force, 0.01f, "Force should be 0 at end (faded out)");
        }

        [Test]
        public void GetCurrentForce_RampsUpThenFades()
        {
            var evt = new MarketEvent(MarketEventType.EarningsBeat, 0, 0.25f, 10f);

            // Early ramp phase (t=0.075, halfway through the 15% ramp)
            evt.ElapsedTime = 0.75f;
            float forceRamp = evt.GetCurrentForce();

            // Hold phase (t=0.5, well within the 15%-85% hold zone)
            evt.ElapsedTime = 5f;
            float forceHold = evt.GetCurrentForce();

            // Tail-off phase (t=0.925, halfway through the final 15%)
            evt.ElapsedTime = 9.25f;
            float forceTail = evt.GetCurrentForce();

            Assert.Greater(forceHold, forceRamp, "Hold force should be greater than ramp-up");
            Assert.Greater(forceHold, forceTail, "Hold force should be greater than tail-off");
            Assert.Greater(forceRamp, 0f, "Ramp-up should be > 0");
            Assert.Greater(forceTail, 0f, "Tail-off should be > 0");
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
    }
}
