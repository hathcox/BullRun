using NUnit.Framework;

namespace BullRun.Tests.Audio
{
    [TestFixture]
    public class AudioManagerTests
    {
        // ════════════════════════════════════════════════════════════════
        // Event Sound Tier Classification (AC 11)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void GetEventSoundTier_EarningsBeat_IsPositive()
        {
            Assert.AreEqual(AudioManager.EventSoundTier.Positive,
                AudioManager.GetEventSoundTier(MarketEventType.EarningsBeat));
        }

        [Test]
        public void GetEventSoundTier_MergerRumor_IsPositive()
        {
            Assert.AreEqual(AudioManager.EventSoundTier.Positive,
                AudioManager.GetEventSoundTier(MarketEventType.MergerRumor));
        }

        [Test]
        public void GetEventSoundTier_EarningsMiss_IsNegative()
        {
            Assert.AreEqual(AudioManager.EventSoundTier.Negative,
                AudioManager.GetEventSoundTier(MarketEventType.EarningsMiss));
        }

        [Test]
        public void GetEventSoundTier_SECInvestigation_IsNegative()
        {
            Assert.AreEqual(AudioManager.EventSoundTier.Negative,
                AudioManager.GetEventSoundTier(MarketEventType.SECInvestigation));
        }

        [Test]
        public void GetEventSoundTier_SectorRotation_IsNegative()
        {
            Assert.AreEqual(AudioManager.EventSoundTier.Negative,
                AudioManager.GetEventSoundTier(MarketEventType.SectorRotation));
        }

        [Test]
        public void GetEventSoundTier_MarketCrash_IsExtreme()
        {
            Assert.AreEqual(AudioManager.EventSoundTier.Extreme,
                AudioManager.GetEventSoundTier(MarketEventType.MarketCrash));
        }

        [Test]
        public void GetEventSoundTier_BullRun_IsExtreme()
        {
            // AC 11: BullRun is explicitly listed as Extreme tier
            Assert.AreEqual(AudioManager.EventSoundTier.Extreme,
                AudioManager.GetEventSoundTier(MarketEventType.BullRun));
        }

        [Test]
        public void GetEventSoundTier_FlashCrash_IsExtreme()
        {
            Assert.AreEqual(AudioManager.EventSoundTier.Extreme,
                AudioManager.GetEventSoundTier(MarketEventType.FlashCrash));
        }

        [Test]
        public void GetEventSoundTier_ShortSqueeze_IsExtreme()
        {
            Assert.AreEqual(AudioManager.EventSoundTier.Extreme,
                AudioManager.GetEventSoundTier(MarketEventType.ShortSqueeze));
        }

        [Test]
        public void GetEventSoundTier_PumpAndDump_IsExtreme()
        {
            Assert.AreEqual(AudioManager.EventSoundTier.Extreme,
                AudioManager.GetEventSoundTier(MarketEventType.PumpAndDump));
        }

        [Test]
        public void GetEventSoundTier_AllEventTypes_HaveMapping()
        {
            // Ensure every MarketEventType has a non-None tier
            var values = System.Enum.GetValues(typeof(MarketEventType));
            foreach (MarketEventType eventType in values)
            {
                var tier = AudioManager.GetEventSoundTier(eventType);
                Assert.AreNotEqual(AudioManager.EventSoundTier.None, tier,
                    $"MarketEventType.{eventType} has no sound tier mapping");
            }
        }
    }
}
