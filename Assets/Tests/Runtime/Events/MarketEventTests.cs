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

            // Quarter way — ramping up
            evt.ElapsedTime = 2.5f;
            float forceQuarter = evt.GetCurrentForce();

            // Midpoint — peak
            evt.ElapsedTime = 5f;
            float forceMid = evt.GetCurrentForce();

            // Three quarters — fading
            evt.ElapsedTime = 7.5f;
            float forceThreeQuarter = evt.GetCurrentForce();

            Assert.Greater(forceMid, forceQuarter, "Peak force should be greater than ramp-up");
            Assert.Greater(forceMid, forceThreeQuarter, "Peak force should be greater than fade-out");
            Assert.Greater(forceQuarter, 0f, "Ramp-up should be > 0");
            Assert.Greater(forceThreeQuarter, 0f, "Fade-out should be > 0");
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
