using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BullRun.Tests.UI
{
    [TestFixture]
    public class EventPopupTests
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
            // Restore timeScale in case a test left it paused
            Time.timeScale = 1f;
        }

        // --- Direction Arrow ---

        [Test]
        public void GetDirectionArrow_PositiveEvent_ReturnsUpArrow()
        {
            var arrow = EventPopup.GetDirectionArrow(true);
            Assert.AreEqual(EventPopup.UpArrow, arrow);
        }

        [Test]
        public void GetDirectionArrow_NegativeEvent_ReturnsDownArrow()
        {
            var arrow = EventPopup.GetDirectionArrow(false);
            Assert.AreEqual(EventPopup.DownArrow, arrow);
        }

        // --- Popup Color ---

        [Test]
        public void GetPopupColor_PositiveEvent_ReturnsGreen()
        {
            var color = EventPopup.GetPopupColor(true);
            Assert.AreEqual(EventPopup.PositiveColor, color);
        }

        [Test]
        public void GetPopupColor_NegativeEvent_ReturnsRed()
        {
            var color = EventPopup.GetPopupColor(false);
            Assert.AreEqual(EventPopup.NegativeColor, color);
        }

        // --- Color Values ---

        [Test]
        public void PositiveColor_IsGreenTinted()
        {
            Assert.Greater(EventPopup.PositiveColor.g, 0.5f, "Green channel should be prominent");
            Assert.Greater(EventPopup.PositiveColor.a, 0.5f, "Alpha should be visible");
        }

        [Test]
        public void NegativeColor_IsRedTinted()
        {
            Assert.Greater(EventPopup.NegativeColor.r, 0.5f, "Red channel should be prominent");
            Assert.Greater(EventPopup.NegativeColor.a, 0.5f, "Alpha should be visible");
        }

        // --- Configuration Constants ---

        [Test]
        public void PauseDuration_IsPositive()
        {
            Assert.Greater(EventPopup.PauseDuration, 0f);
        }

        [Test]
        public void QueuedPauseDuration_IsShorterThanNormal()
        {
            Assert.Less(EventPopup.QueuedPauseDuration, EventPopup.PauseDuration,
                "Queued events should have shorter pause to avoid excessive total pause time");
        }

        [Test]
        public void FlyDuration_IsPositive()
        {
            Assert.Greater(EventPopup.FlyDuration, 0f);
        }

        [Test]
        public void FlyDistance_IsLarge()
        {
            Assert.Greater(EventPopup.FlyDistance, 500f, "Fly distance should be large for dramatic effect");
        }

        // --- EventPopup Activation ---

        [Test]
        public void EventPopup_ActivatesOnMarketEvent()
        {
            var popup = CreateTestPopup();

            EventBus.Publish(new MarketEventFiredEvent
            {
                EventType = MarketEventType.EarningsBeat,
                Headline = "ACME beats earnings!",
                IsPositive = true,
                AffectedTickerSymbols = new[] { "ACME" },
                Duration = 5f
            });

            Assert.IsTrue(popup.Component.IsActive, "Popup should be active after event");

            Object.DestroyImmediate(popup.Root);
        }

        [Test]
        public void EventPopup_SetsTimeScaleToZero()
        {
            var popup = CreateTestPopup();
            Time.timeScale = 1f;

            EventBus.Publish(new MarketEventFiredEvent
            {
                EventType = MarketEventType.EarningsBeat,
                Headline = "ACME beats earnings!",
                IsPositive = true,
                AffectedTickerSymbols = new[] { "ACME" },
                Duration = 5f
            });

            Assert.AreEqual(0f, Time.timeScale, "TimeScale should be 0 during popup display");

            Object.DestroyImmediate(popup.Root);
            Time.timeScale = 1f;
        }

        [Test]
        public void EventPopup_ShowsCorrectHeadline()
        {
            var popup = CreateTestPopup();
            string headline = "Breaking: Market Surge!";

            EventBus.Publish(new MarketEventFiredEvent
            {
                EventType = MarketEventType.BullRun,
                Headline = headline,
                IsPositive = true,
                AffectedTickerSymbols = new[] { "ALL" },
                Duration = 5f
            });

            Assert.AreEqual(headline, popup.HeadlineText.text);

            Object.DestroyImmediate(popup.Root);
            Time.timeScale = 1f;
        }

        [Test]
        public void EventPopup_PositiveEvent_ShowsUpArrowAndGreen()
        {
            var popup = CreateTestPopup();

            EventBus.Publish(new MarketEventFiredEvent
            {
                EventType = MarketEventType.EarningsBeat,
                Headline = "ACME beats earnings!",
                IsPositive = true,
                AffectedTickerSymbols = new[] { "ACME" },
                Duration = 5f
            });

            Assert.AreEqual(EventPopup.UpArrow, popup.ArrowText.text, "Should show up arrow for positive");
            Assert.AreEqual(EventPopup.PositiveColor, popup.Background.color, "Should show green background for positive");

            Object.DestroyImmediate(popup.Root);
            Time.timeScale = 1f;
        }

        [Test]
        public void EventPopup_NegativeEvent_ShowsDownArrowAndRed()
        {
            var popup = CreateTestPopup();

            EventBus.Publish(new MarketEventFiredEvent
            {
                EventType = MarketEventType.MarketCrash,
                Headline = "Market crash incoming!",
                IsPositive = false,
                AffectedTickerSymbols = new[] { "ALL" },
                Duration = 5f
            });

            Assert.AreEqual(EventPopup.DownArrow, popup.ArrowText.text, "Should show down arrow for negative");
            Assert.AreEqual(EventPopup.NegativeColor, popup.Background.color, "Should show red background for negative");

            Object.DestroyImmediate(popup.Root);
            Time.timeScale = 1f;
        }

        [Test]
        public void EventPopup_ShowsAffectedTickers()
        {
            var popup = CreateTestPopup();

            EventBus.Publish(new MarketEventFiredEvent
            {
                EventType = MarketEventType.EarningsBeat,
                Headline = "ACME beats earnings!",
                IsPositive = true,
                AffectedTickerSymbols = new[] { "ACME" },
                Duration = 5f
            });

            Assert.IsTrue(popup.TickerText.text.Contains("ACME"), "Ticker text should show affected symbol");

            Object.DestroyImmediate(popup.Root);
            Time.timeScale = 1f;
        }

        [Test]
        public void EventPopup_GlobalEvent_ShowsAllStocks()
        {
            var popup = CreateTestPopup();

            EventBus.Publish(new MarketEventFiredEvent
            {
                EventType = MarketEventType.BullRun,
                Headline = "Bull run!",
                IsPositive = true,
                AffectedTickerSymbols = null,
                Duration = 5f
            });

            Assert.AreEqual("ALL STOCKS", popup.TickerText.text, "Global event should show ALL STOCKS");

            Object.DestroyImmediate(popup.Root);
            Time.timeScale = 1f;
        }

        // --- Event Queuing ---

        [Test]
        public void EventPopup_QueuesSecondEvent_WhenFirstIsActive()
        {
            var popup = CreateTestPopup();

            // First event
            EventBus.Publish(new MarketEventFiredEvent
            {
                EventType = MarketEventType.EarningsBeat,
                Headline = "First event",
                IsPositive = true,
                AffectedTickerSymbols = new[] { "ACME" },
                Duration = 5f
            });

            Assert.IsTrue(popup.Component.IsActive, "First event should activate popup");

            // Second event while first is active
            EventBus.Publish(new MarketEventFiredEvent
            {
                EventType = MarketEventType.MarketCrash,
                Headline = "Second event",
                IsPositive = false,
                AffectedTickerSymbols = new[] { "FAIL" },
                Duration = 5f
            });

            Assert.AreEqual(1, popup.Component.QueueCount, "Second event should be queued");

            Object.DestroyImmediate(popup.Root);
            Time.timeScale = 1f;
        }

        [Test]
        public void EventPopup_IgnoresEmptyHeadline()
        {
            var popup = CreateTestPopup();

            EventBus.Publish(new MarketEventFiredEvent
            {
                EventType = MarketEventType.EarningsBeat,
                Headline = "",
                IsPositive = true,
                Duration = 5f
            });

            Assert.IsFalse(popup.Component.IsActive, "Should not activate for empty headline");

            Object.DestroyImmediate(popup.Root);
        }

        [Test]
        public void EventPopup_IgnoresNullHeadline()
        {
            var popup = CreateTestPopup();

            EventBus.Publish(new MarketEventFiredEvent
            {
                EventType = MarketEventType.EarningsBeat,
                Headline = null,
                IsPositive = true,
                Duration = 5f
            });

            Assert.IsFalse(popup.Component.IsActive, "Should not activate for null headline");

            Object.DestroyImmediate(popup.Root);
        }

        // --- TimeScale Safety ---

        [Test]
        public void EventPopup_IsActive_DuringPause_SoOnDestroyCanRestore()
        {
            // Verifies the contract that makes OnDestroy's timeScale restoration work:
            // when popup is paused, IsActive is true so OnDestroy knows to restore timeScale.
            // (DestroyImmediate doesn't reliably trigger OnDestroy in all Unity test runners.)
            var popup = CreateTestPopup();
            Time.timeScale = 1f;

            EventBus.Publish(new MarketEventFiredEvent
            {
                EventType = MarketEventType.EarningsBeat,
                Headline = "ACME beats earnings!",
                IsPositive = true,
                AffectedTickerSymbols = new[] { "ACME" },
                Duration = 5f
            });

            Assert.AreEqual(0f, Time.timeScale, "TimeScale should be 0 during popup");
            Assert.IsTrue(popup.Component.IsActive, "IsActive must be true during pause so OnDestroy safety net works");

            Object.DestroyImmediate(popup.Root);
            Time.timeScale = 1f;
        }

        // --- Completion Event ---

        [Test]
        public void EventPopup_PublishesCompletedEvent_OnEmptyHeadlineSkip()
        {
            var popup = CreateTestPopup();
            MarketEventType receivedType = default;
            bool receivedEvent = false;

            EventBus.Subscribe<EventPopupCompletedEvent>(evt =>
            {
                receivedEvent = true;
                receivedType = evt.EventType;
            });

            EventBus.Publish(new MarketEventFiredEvent
            {
                EventType = MarketEventType.MarketCrash,
                Headline = "",
                IsPositive = false,
                Duration = 5f
            });

            Assert.IsTrue(receivedEvent, "Should publish EventPopupCompletedEvent for skipped popup");
            Assert.AreEqual(MarketEventType.MarketCrash, receivedType, "Completed event should carry original event type");
            Assert.IsFalse(popup.Component.IsActive, "Popup should not activate for empty headline");

            Object.DestroyImmediate(popup.Root);
        }

        // --- Helper ---

        private struct TestPopup
        {
            public GameObject Root;
            public EventPopup Component;
            public Image Background;
            public Text ArrowText;
            public Text HeadlineText;
            public Text TickerText;
            public CanvasGroup CanvasGroup;
        }

        private TestPopup CreateTestPopup()
        {
            var root = new GameObject("TestPopupRoot");

            var popupPanel = new GameObject("PopupPanel");
            popupPanel.transform.SetParent(root.transform);
            var rect = popupPanel.AddComponent<RectTransform>();
            var bg = popupPanel.AddComponent<Image>();
            var cg = popupPanel.AddComponent<CanvasGroup>();

            var arrowGo = new GameObject("Arrow");
            arrowGo.transform.SetParent(popupPanel.transform);
            arrowGo.AddComponent<RectTransform>();
            var arrowText = arrowGo.AddComponent<Text>();
            arrowText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var headlineGo = new GameObject("Headline");
            headlineGo.transform.SetParent(popupPanel.transform);
            headlineGo.AddComponent<RectTransform>();
            var headlineText = headlineGo.AddComponent<Text>();
            headlineText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var tickerGo = new GameObject("Ticker");
            tickerGo.transform.SetParent(popupPanel.transform);
            tickerGo.AddComponent<RectTransform>();
            var tickerText = tickerGo.AddComponent<Text>();
            tickerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var component = root.AddComponent<EventPopup>();
            component.Initialize(popupPanel, bg, arrowText, headlineText, tickerText, cg, rect);

            return new TestPopup
            {
                Root = root,
                Component = component,
                Background = bg,
                ArrowText = arrowText,
                HeadlineText = headlineText,
                TickerText = tickerText,
                CanvasGroup = cg
            };
        }
    }
}
