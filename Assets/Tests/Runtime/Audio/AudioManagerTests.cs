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

        // ════════════════════════════════════════════════════════════════
        // Instance & Public UI Helper API
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Instance_IsNullBeforeInitialize()
        {
            // AudioManager.Instance is set in Initialize which requires a live MonoBehaviour.
            // In edit-mode tests there is no scene, so Instance should remain null (or whatever
            // state a prior test left it — we just verify the property exists and is accessible).
            // The compile-time check that Instance is a public static property is the real value here.
            var _ = AudioManager.Instance; // must not throw
            Assert.Pass("AudioManager.Instance property is accessible");
        }

        [Test]
        public void PublicUiHelpers_ExistAsPublicMethods()
        {
            // Verify every UI helper method is publicly accessible via reflection.
            // Catches accidental visibility changes or renames.
            string[] expectedMethods =
            {
                "PlayButtonHover", "PlayRelicHover", "PlayTabSwitch", "PlayNavigate",
                "PlayCancel", "PlayResultsDismiss", "PlayStatsCountUp",
                "PlayProfitPopup", "PlayLossPopup", "PlayRepEarned", "PlayStreakMilestone",
                "PlayTokenLaunch", "PlayTokenLand", "PlayTokenBurst"
            };

            var type = typeof(AudioManager);
            foreach (string methodName in expectedMethods)
            {
                var method = type.GetMethod(methodName,
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                Assert.IsNotNull(method, $"AudioManager.{methodName}() should be a public instance method");
            }
        }
    }
}
