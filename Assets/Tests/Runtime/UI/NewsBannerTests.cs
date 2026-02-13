using NUnit.Framework;
using UnityEngine;

namespace BullRun.Tests.UI
{
    [TestFixture]
    public class NewsBannerTests
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

        // --- Banner Color Selection ---

        [Test]
        public void GetBannerColor_PositiveEvent_ReturnsGreen()
        {
            var color = NewsBanner.GetBannerColor(true);
            Assert.AreEqual(NewsBanner.PositiveBannerColor, color);
        }

        [Test]
        public void GetBannerColor_NegativeEvent_ReturnsRed()
        {
            var color = NewsBanner.GetBannerColor(false);
            Assert.AreEqual(NewsBanner.NegativeBannerColor, color);
        }

        // --- Banner Color Values ---

        [Test]
        public void PositiveBannerColor_IsGreenWithAlpha()
        {
            Assert.AreEqual(0f, NewsBanner.PositiveBannerColor.r, 0.01f, "Red channel should be 0");
            Assert.Greater(NewsBanner.PositiveBannerColor.g, 0.9f, "Green channel should be high");
            Assert.Greater(NewsBanner.PositiveBannerColor.a, 0.5f, "Alpha should be visible");
        }

        [Test]
        public void NegativeBannerColor_IsRedWithAlpha()
        {
            Assert.Greater(NewsBanner.NegativeBannerColor.r, 0.9f, "Red channel should be high");
            Assert.Greater(NewsBanner.NegativeBannerColor.a, 0.5f, "Alpha should be visible");
        }

        // --- Banner Constants ---

        [Test]
        public void BannerDuration_IsPositive()
        {
            Assert.Greater(NewsBanner.BannerDuration, 0f);
        }

        [Test]
        public void BannerHeight_IsPositive()
        {
            Assert.Greater(NewsBanner.BannerHeight, 0f);
        }

        // --- Event-Driven Banner Creation ---

        [Test]
        public void NewsBanner_ShowsBannerOnPositiveEvent()
        {
            var go = new GameObject("TestBannerParent");
            var containerGo = new GameObject("TestContainer");
            containerGo.transform.SetParent(go.transform);
            containerGo.AddComponent<RectTransform>();

            var banner = go.AddComponent<NewsBanner>();
            banner.Initialize(containerGo.transform);

            EventBus.Publish(new MarketEventFiredEvent
            {
                EventType = MarketEventType.EarningsBeat,
                Headline = "ACME beats earnings!",
                IsPositive = true,
                Duration = 5f
            });

            Assert.AreEqual(1, banner.ActiveBannerCount, "Should have one active banner");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void NewsBanner_ShowsBannerOnNegativeEvent()
        {
            var go = new GameObject("TestBannerParent");
            var containerGo = new GameObject("TestContainer");
            containerGo.transform.SetParent(go.transform);
            containerGo.AddComponent<RectTransform>();

            var banner = go.AddComponent<NewsBanner>();
            banner.Initialize(containerGo.transform);

            EventBus.Publish(new MarketEventFiredEvent
            {
                EventType = MarketEventType.EarningsMiss,
                Headline = "ACME misses earnings!",
                IsPositive = false,
                Duration = 5f
            });

            Assert.AreEqual(1, banner.ActiveBannerCount, "Should have one active banner");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void NewsBanner_DisplaysCorrectHeadlineText()
        {
            var go = new GameObject("TestBannerParent");
            var containerGo = new GameObject("TestContainer");
            containerGo.transform.SetParent(go.transform);
            containerGo.AddComponent<RectTransform>();

            var banner = go.AddComponent<NewsBanner>();
            banner.Initialize(containerGo.transform);

            string expectedHeadline = "Breaking: Market Surge!";
            EventBus.Publish(new MarketEventFiredEvent
            {
                EventType = MarketEventType.BullRun,
                Headline = expectedHeadline,
                IsPositive = true,
                Duration = 5f
            });

            Assert.AreEqual(1, banner.ActiveBannerCount);

            // Verify headline text is present in the created banner
            var headlineText = containerGo.GetComponentInChildren<UnityEngine.UI.Text>();
            Assert.IsNotNull(headlineText, "Banner should contain a Text component");
            Assert.AreEqual(expectedHeadline, headlineText.text);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void NewsBanner_RemovesBannerOnEventEnd()
        {
            var go = new GameObject("TestBannerParent");
            var containerGo = new GameObject("TestContainer");
            containerGo.transform.SetParent(go.transform);
            containerGo.AddComponent<RectTransform>();

            var banner = go.AddComponent<NewsBanner>();
            banner.Initialize(containerGo.transform);

            EventBus.Publish(new MarketEventFiredEvent
            {
                EventType = MarketEventType.EarningsBeat,
                Headline = "Test headline",
                IsPositive = true,
                Duration = 5f
            });

            Assert.AreEqual(1, banner.ActiveBannerCount);

            EventBus.Publish(new MarketEventEndedEvent
            {
                EventType = MarketEventType.EarningsBeat
            });

            Assert.AreEqual(0, banner.ActiveBannerCount, "Banner should be removed when event ends");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void NewsBanner_StacksMultipleBanners()
        {
            var go = new GameObject("TestBannerParent");
            var containerGo = new GameObject("TestContainer");
            containerGo.transform.SetParent(go.transform);
            containerGo.AddComponent<RectTransform>();

            var banner = go.AddComponent<NewsBanner>();
            banner.Initialize(containerGo.transform);

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

            Assert.AreEqual(2, banner.ActiveBannerCount, "Should stack two banners");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void NewsBanner_IgnoresEmptyHeadline()
        {
            var go = new GameObject("TestBannerParent");
            var containerGo = new GameObject("TestContainer");
            containerGo.transform.SetParent(go.transform);
            containerGo.AddComponent<RectTransform>();

            var banner = go.AddComponent<NewsBanner>();
            banner.Initialize(containerGo.transform);

            EventBus.Publish(new MarketEventFiredEvent
            {
                EventType = MarketEventType.EarningsBeat,
                Headline = "",
                IsPositive = true,
                Duration = 5f
            });

            Assert.AreEqual(0, banner.ActiveBannerCount, "Should not create banner for empty headline");

            Object.DestroyImmediate(go);
        }
    }
}
