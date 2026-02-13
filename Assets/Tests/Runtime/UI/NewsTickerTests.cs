using NUnit.Framework;
using UnityEngine;

namespace BullRun.Tests.UI
{
    [TestFixture]
    public class NewsTickerTests
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

        // --- Headline Formatting ---

        [Test]
        public void FormatHeadline_AddsPrefix()
        {
            string result = NewsTicker.FormatHeadline("Test headline");
            Assert.IsTrue(result.Contains("Test headline"), "Should contain original text");
            Assert.IsTrue(result.StartsWith("\u25C6"), "Should start with diamond symbol");
        }

        [Test]
        public void FormatHeadline_PreservesFullText()
        {
            string headline = "Breaking: ACME stock surges on earnings!";
            string result = NewsTicker.FormatHeadline(headline);
            Assert.IsTrue(result.Contains(headline));
        }

        // --- Constants ---

        [Test]
        public void ScrollSpeed_IsPositive()
        {
            Assert.Greater(NewsTicker.ScrollSpeed, 0f, "Scroll speed must be positive");
        }

        [Test]
        public void FontSize_IsReasonable()
        {
            Assert.Greater(NewsTicker.FontSize, 8, "Font size should be readable");
            Assert.Less(NewsTicker.FontSize, 30, "Font size shouldn't be too large for ticker");
        }

        // --- Event-Driven Ticker ---

        [Test]
        public void NewsTicker_QueuesHeadlineOnEvent()
        {
            var go = new GameObject("TestTickerParent");
            var containerGo = new GameObject("TestContainer");
            containerGo.transform.SetParent(go.transform);
            containerGo.AddComponent<RectTransform>();

            var ticker = go.AddComponent<NewsTicker>();
            ticker.Initialize(containerGo.transform, 1920f);

            EventBus.Publish(new MarketEventFiredEvent
            {
                EventType = MarketEventType.EarningsBeat,
                Headline = "Test headline",
                IsPositive = true,
                Duration = 5f
            });

            Assert.AreEqual(1, ticker.EntryCount, "Should have one ticker entry");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void NewsTicker_QueuesMultipleHeadlines()
        {
            var go = new GameObject("TestTickerParent");
            var containerGo = new GameObject("TestContainer");
            containerGo.transform.SetParent(go.transform);
            containerGo.AddComponent<RectTransform>();

            var ticker = go.AddComponent<NewsTicker>();
            ticker.Initialize(containerGo.transform, 1920f);

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

            Assert.AreEqual(2, ticker.EntryCount, "Should queue both headlines in order");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void NewsTicker_IgnoresEmptyHeadline()
        {
            var go = new GameObject("TestTickerParent");
            var containerGo = new GameObject("TestContainer");
            containerGo.transform.SetParent(go.transform);
            containerGo.AddComponent<RectTransform>();

            var ticker = go.AddComponent<NewsTicker>();
            ticker.Initialize(containerGo.transform, 1920f);

            EventBus.Publish(new MarketEventFiredEvent
            {
                EventType = MarketEventType.EarningsBeat,
                Headline = "",
                IsPositive = true,
                Duration = 5f
            });

            Assert.AreEqual(0, ticker.EntryCount, "Should not create entry for empty headline");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void NewsTicker_IgnoresNullHeadline()
        {
            var go = new GameObject("TestTickerParent");
            var containerGo = new GameObject("TestContainer");
            containerGo.transform.SetParent(go.transform);
            containerGo.AddComponent<RectTransform>();

            var ticker = go.AddComponent<NewsTicker>();
            ticker.Initialize(containerGo.transform, 1920f);

            EventBus.Publish(new MarketEventFiredEvent
            {
                EventType = MarketEventType.EarningsBeat,
                Headline = null,
                IsPositive = true,
                Duration = 5f
            });

            Assert.AreEqual(0, ticker.EntryCount, "Should not create entry for null headline");

            Object.DestroyImmediate(go);
        }
    }
}
