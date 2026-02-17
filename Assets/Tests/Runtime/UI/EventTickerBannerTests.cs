using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BullRun.Tests.UI
{
    [TestFixture]
    public class EventTickerBannerTests
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

        // --- Banner Color ---

        [Test]
        public void GetBannerColor_ReturnsAmberAt85Alpha()
        {
            var color = EventTickerBanner.GetBannerColor();

            // CRTThemeData.Warning = #ffb800, at 85% alpha
            Assert.AreEqual(CRTThemeData.Warning.r, color.r, 0.01f, "Red channel should match Warning");
            Assert.AreEqual(CRTThemeData.Warning.g, color.g, 0.01f, "Green channel should match Warning");
            Assert.AreEqual(CRTThemeData.Warning.b, color.b, 0.01f, "Blue channel should match Warning");
            Assert.AreEqual(0.85f, color.a, 0.01f, "Alpha should be 85%");
        }

        // --- Banner Constants ---

        [Test]
        public void BannerHeight_Is36()
        {
            Assert.AreEqual(36f, EventTickerBanner.BannerHeight, 0.01f);
        }

        [Test]
        public void FadeInDuration_IsPositive()
        {
            Assert.Greater(EventTickerBanner.FadeInDuration, 0f);
        }

        [Test]
        public void DisplayDuration_IsPositive()
        {
            Assert.Greater(EventTickerBanner.DisplayDuration, 0f);
        }

        // --- Event-Driven Banner Display ---

        [Test]
        public void EventTickerBanner_ShowsBannerOnMarketEvent()
        {
            var go = CreateTestBanner(out var banner, out var headlineText);

            EventBus.Publish(new MarketEventFiredEvent
            {
                EventType = MarketEventType.EarningsBeat,
                Headline = "ACME beats earnings!",
                IsPositive = true,
                Duration = 5f
            });

            Assert.IsTrue(banner.IsShowing, "Banner should be showing after event");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void EventTickerBanner_DisplaysHeadlineWithWarningPrefix()
        {
            var go = CreateTestBanner(out var banner, out var headlineText);

            string headline = "Market Crash Incoming!";
            EventBus.Publish(new MarketEventFiredEvent
            {
                EventType = MarketEventType.MarketCrash,
                Headline = headline,
                IsPositive = false,
                Duration = 5f
            });

            Assert.IsTrue(banner.IsShowing);
            Assert.IsTrue(headlineText.text.Contains("\u26A0"), "Should have warning triangle prefix");
            Assert.IsTrue(headlineText.text.Contains(headline), "Should contain the headline text");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void EventTickerBanner_IgnoresEmptyHeadline()
        {
            var go = CreateTestBanner(out var banner, out _);

            EventBus.Publish(new MarketEventFiredEvent
            {
                EventType = MarketEventType.EarningsBeat,
                Headline = "",
                IsPositive = true,
                Duration = 5f
            });

            Assert.IsFalse(banner.IsShowing, "Should not show for empty headline");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void EventTickerBanner_QueuesMultipleEvents()
        {
            var go = CreateTestBanner(out var banner, out _);

            EventBus.Publish(new MarketEventFiredEvent
            {
                EventType = MarketEventType.EarningsBeat,
                Headline = "First headline",
                IsPositive = true,
                Duration = 5f
            });

            EventBus.Publish(new MarketEventFiredEvent
            {
                EventType = MarketEventType.MarketCrash,
                Headline = "Second headline",
                IsPositive = false,
                Duration = 5f
            });

            Assert.IsTrue(banner.IsShowing, "Banner should be showing");
            Assert.AreEqual(1, banner.QueuedCount, "Second event should be queued");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void EventTickerBanner_HidesOnEventEnd()
        {
            var go = CreateTestBanner(out var banner, out _);

            EventBus.Publish(new MarketEventFiredEvent
            {
                EventType = MarketEventType.EarningsBeat,
                Headline = "Test headline",
                IsPositive = true,
                Duration = 5f
            });

            Assert.IsTrue(banner.IsShowing);

            // End the event — should trigger fast fade-out
            EventBus.Publish(new MarketEventEndedEvent
            {
                EventType = MarketEventType.EarningsBeat
            });

            // The banner enters fade-out state, so still technically showing until fade completes
            Assert.IsTrue(banner.IsShowing, "Banner should still show during fade-out");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void EventTickerBanner_RemovesQueuedEventOnEnd()
        {
            var go = CreateTestBanner(out var banner, out _);

            EventBus.Publish(new MarketEventFiredEvent
            {
                EventType = MarketEventType.EarningsBeat,
                Headline = "First",
                IsPositive = true,
                Duration = 5f
            });

            EventBus.Publish(new MarketEventFiredEvent
            {
                EventType = MarketEventType.MarketCrash,
                Headline = "Second (queued)",
                IsPositive = false,
                Duration = 5f
            });

            Assert.AreEqual(1, banner.QueuedCount);

            // End the queued event
            EventBus.Publish(new MarketEventEndedEvent
            {
                EventType = MarketEventType.MarketCrash
            });

            Assert.AreEqual(0, banner.QueuedCount, "Queued event should be removed");

            Object.DestroyImmediate(go);
        }

        // --- Helper ---

        private static GameObject CreateTestBanner(out EventTickerBanner banner, out Text headlineText)
        {
            var go = new GameObject("TestEventTickerParent");

            var panelGo = new GameObject("BannerPanel");
            panelGo.transform.SetParent(go.transform);
            panelGo.AddComponent<RectTransform>();

            var bg = panelGo.AddComponent<Image>();

            var canvasGroup = panelGo.AddComponent<CanvasGroup>();

            var textGo = new GameObject("HeadlineText");
            textGo.transform.SetParent(panelGo.transform);
            textGo.AddComponent<RectTransform>();
            headlineText = textGo.AddComponent<Text>();
            headlineText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            banner = go.AddComponent<EventTickerBanner>();
            banner.Initialize(panelGo, bg, headlineText, canvasGroup);

            return go;
        }
    }
}
