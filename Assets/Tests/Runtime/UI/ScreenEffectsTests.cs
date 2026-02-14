using NUnit.Framework;
using UnityEngine;

namespace BullRun.Tests.UI
{
    [TestFixture]
    public class ScreenEffectsTests
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

        // --- Effect Type Mapping ---

        [Test]
        public void GetEffectType_MarketCrash_ReturnsShakeAndPulse()
        {
            Assert.AreEqual(ScreenEffectType.ShakeAndPulse,
                ScreenEffects.GetEffectType(MarketEventType.MarketCrash));
        }

        [Test]
        public void GetEffectType_BullRun_ReturnsGreenTint()
        {
            Assert.AreEqual(ScreenEffectType.GreenTint,
                ScreenEffects.GetEffectType(MarketEventType.BullRun));
        }

        [Test]
        public void GetEffectType_FlashCrash_ReturnsRedFlash()
        {
            Assert.AreEqual(ScreenEffectType.RedFlash,
                ScreenEffects.GetEffectType(MarketEventType.FlashCrash));
        }

        [Test]
        public void GetEffectType_EarningsBeat_ReturnsNone()
        {
            Assert.AreEqual(ScreenEffectType.None,
                ScreenEffects.GetEffectType(MarketEventType.EarningsBeat));
        }

        [Test]
        public void GetEffectType_PumpAndDump_ReturnsNone()
        {
            Assert.AreEqual(ScreenEffectType.None,
                ScreenEffects.GetEffectType(MarketEventType.PumpAndDump));
        }

        [Test]
        public void GetEffectType_SECInvestigation_ReturnsNone()
        {
            Assert.AreEqual(ScreenEffectType.None,
                ScreenEffects.GetEffectType(MarketEventType.SECInvestigation));
        }

        // --- Color Constants ---

        [Test]
        public void CrashRedPulse_HasCorrectAlpha()
        {
            Assert.AreEqual(0.3f, ScreenEffects.CrashRedPulse.a, 0.01f,
                "Crash red pulse should have 0.3 alpha");
        }

        [Test]
        public void BullGreenTint_HasLowAlpha()
        {
            Assert.AreEqual(0.15f, ScreenEffects.BullGreenTint.a, 0.01f,
                "Bull green tint should be subtle (0.15 alpha)");
        }

        [Test]
        public void FlashRed_HasMediumAlpha()
        {
            Assert.AreEqual(0.5f, ScreenEffects.FlashRed.a, 0.01f,
                "Flash red should have 0.5 alpha");
        }

        // --- Duration Constants ---

        [Test]
        public void ShakeDuration_IsPositive()
        {
            Assert.Greater(ScreenEffects.ShakeDuration, 0f);
        }

        [Test]
        public void FlashDuration_IsBrief()
        {
            Assert.Less(ScreenEffects.FlashDuration, 1f,
                "Flash should be very brief");
            Assert.Greater(ScreenEffects.FlashDuration, 0f);
        }

        // --- Event-Driven Effects ---

        [Test]
        public void ScreenEffects_ActivatesShakeOnMarketCrash()
        {
            var go = CreateScreenEffectsGO(out var effects);

            EventBus.Publish(new EventPopupCompletedEvent
            {
                EventType = MarketEventType.MarketCrash,
                IsPositive = false
            });

            Assert.IsTrue(effects.IsShaking, "Should be shaking after MarketCrash");
            Assert.IsTrue(effects.IsRedPulsing, "Should be red pulsing after MarketCrash");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void ScreenEffects_ActivatesGreenTintOnBullRun()
        {
            var go = CreateScreenEffectsGO(out var effects);

            EventBus.Publish(new EventPopupCompletedEvent
            {
                EventType = MarketEventType.BullRun,
                IsPositive = true
            });

            Assert.IsTrue(effects.IsGreenTinting, "Should be green tinting after BullRun");
            Assert.IsFalse(effects.IsShaking, "Should not be shaking for BullRun");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void ScreenEffects_ActivatesFlashOnFlashCrash()
        {
            var go = CreateScreenEffectsGO(out var effects);

            EventBus.Publish(new EventPopupCompletedEvent
            {
                EventType = MarketEventType.FlashCrash,
                IsPositive = false
            });

            Assert.IsTrue(effects.IsFlashing, "Should be flashing after FlashCrash");
            Assert.IsFalse(effects.IsShaking, "Should not be shaking for FlashCrash");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void ScreenEffects_StopsOnEventEnd()
        {
            var go = CreateScreenEffectsGO(out var effects);

            // Start effect via popup completion
            EventBus.Publish(new EventPopupCompletedEvent
            {
                EventType = MarketEventType.MarketCrash,
                IsPositive = false
            });

            Assert.IsTrue(effects.IsShaking);

            EventBus.Publish(new MarketEventEndedEvent
            {
                EventType = MarketEventType.MarketCrash
            });

            Assert.IsFalse(effects.IsShaking, "Shake should stop when event ends");
            Assert.IsFalse(effects.IsRedPulsing, "Red pulse should stop when event ends");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void ScreenEffects_NoEffectForEarningsBeat()
        {
            var go = CreateScreenEffectsGO(out var effects);

            EventBus.Publish(new EventPopupCompletedEvent
            {
                EventType = MarketEventType.EarningsBeat,
                IsPositive = true
            });

            Assert.IsFalse(effects.IsShaking);
            Assert.IsFalse(effects.IsRedPulsing);
            Assert.IsFalse(effects.IsGreenTinting);
            Assert.IsFalse(effects.IsFlashing);

            Object.DestroyImmediate(go);
        }

        // --- Helper ---

        private GameObject CreateScreenEffectsGO(out ScreenEffects effects)
        {
            var go = new GameObject("TestScreenEffects");
            var shakeGo = new GameObject("Shake");
            shakeGo.transform.SetParent(go.transform);
            var shakeRect = shakeGo.AddComponent<RectTransform>();

            var redGo = new GameObject("Red");
            redGo.transform.SetParent(shakeGo.transform);
            redGo.AddComponent<RectTransform>();
            var redImage = redGo.AddComponent<UnityEngine.UI.Image>();

            var greenGo = new GameObject("Green");
            greenGo.transform.SetParent(shakeGo.transform);
            greenGo.AddComponent<RectTransform>();
            var greenImage = greenGo.AddComponent<UnityEngine.UI.Image>();

            var flashGo = new GameObject("Flash");
            flashGo.transform.SetParent(shakeGo.transform);
            flashGo.AddComponent<RectTransform>();
            var flashImage = flashGo.AddComponent<UnityEngine.UI.Image>();

            effects = go.AddComponent<ScreenEffects>();
            effects.Initialize(shakeRect, redImage, greenImage, flashImage);

            return go;
        }
    }
}
