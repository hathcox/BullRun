using NUnit.Framework;
using UnityEngine;

namespace BullRun.Tests.UI
{
    [TestFixture]
    public class TradeFeedbackTests
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

        // --- GetFeedbackColor ---

        [Test]
        public void GetFeedbackColor_SuccessBuy_ReturnsGreen()
        {
            var color = TradeFeedback.GetFeedbackColor(true, true, false);
            Assert.AreEqual(TradingHUD.ProfitGreen, color);
        }

        [Test]
        public void GetFeedbackColor_SuccessSell_ReturnsCyan()
        {
            var color = TradeFeedback.GetFeedbackColor(true, false, false);
            Assert.AreEqual(TradeFeedback.SellCyan, color);
        }

        [Test]
        public void GetFeedbackColor_SuccessShort_ReturnsPink()
        {
            var color = TradeFeedback.GetFeedbackColor(true, false, true);
            Assert.AreEqual(TradeFeedback.ShortPink, color);
        }

        [Test]
        public void GetFeedbackColor_SuccessCover_ReturnsPink()
        {
            var color = TradeFeedback.GetFeedbackColor(true, true, true);
            Assert.AreEqual(TradeFeedback.ShortPink, color);
        }

        [Test]
        public void GetFeedbackColor_Failure_ReturnsRed()
        {
            var color = TradeFeedback.GetFeedbackColor(false, true, false);
            Assert.AreEqual(TradingHUD.LossRed, color);
        }

        [Test]
        public void GetFeedbackColor_FailureShort_ReturnsRed()
        {
            var color = TradeFeedback.GetFeedbackColor(false, false, true);
            Assert.AreEqual(TradingHUD.LossRed, color);
        }

        // --- GetShortRejectionReason ---

        [Test]
        public void GetShortRejectionReason_DuplicateShort_ReturnsDuplicateMessage()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenShort("ACME", 5, 50f);
            string reason = TradeFeedback.GetShortRejectionReason(portfolio, "ACME");
            Assert.AreEqual("Already shorting this stock", reason);
        }

        [Test]
        public void GetShortRejectionReason_NoPosition_ReturnsRejectedMessage()
        {
            var portfolio = new Portfolio(100f);
            string reason = TradeFeedback.GetShortRejectionReason(portfolio, "ACME");
            Assert.AreEqual("Short rejected", reason);
        }

        // --- GetCoverRejectionReason ---

        [Test]
        public void GetCoverRejectionReason_NoPosition_ReturnsNoShortMessage()
        {
            var portfolio = new Portfolio(1000f);
            string reason = TradeFeedback.GetCoverRejectionReason(portfolio, "ACME");
            Assert.AreEqual("No short position to cover", reason);
        }

        [Test]
        public void GetCoverRejectionReason_LongPosition_ReturnsNoShortMessage()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenPosition("ACME", 10, 25f);
            string reason = TradeFeedback.GetCoverRejectionReason(portfolio, "ACME");
            Assert.AreEqual("No short position to cover", reason);
        }

        [Test]
        public void GetCoverRejectionReason_ShortPositionExists_ReturnsInsufficientShares()
        {
            var portfolio = new Portfolio(1000f);
            portfolio.OpenShort("ACME", 5, 50f);
            string reason = TradeFeedback.GetCoverRejectionReason(portfolio, "ACME");
            Assert.AreEqual("Insufficient shares to cover", reason);
        }

        // --- TradeFeedbackEvent via EventBus ---

        [Test]
        public void TradeFeedbackEvent_PublishAndSubscribe()
        {
            TradeFeedbackEvent received = default;
            bool eventFired = false;
            EventBus.Subscribe<TradeFeedbackEvent>(e => { received = e; eventFired = true; });

            EventBus.Publish(new TradeFeedbackEvent
            {
                Message = "SHORTED ACME x10",
                IsSuccess = true,
                IsBuy = false,
                IsShort = true
            });

            Assert.IsTrue(eventFired);
            Assert.AreEqual("SHORTED ACME x10", received.Message);
            Assert.IsTrue(received.IsSuccess);
            Assert.IsFalse(received.IsBuy);
            Assert.IsTrue(received.IsShort);
        }

        [Test]
        public void TradeFeedbackEvent_RejectionMessage()
        {
            TradeFeedbackEvent received = default;
            bool eventFired = false;
            EventBus.Subscribe<TradeFeedbackEvent>(e => { received = e; eventFired = true; });

            EventBus.Publish(new TradeFeedbackEvent
            {
                Message = "Already shorting this stock",
                IsSuccess = false,
                IsBuy = false,
                IsShort = true
            });

            Assert.IsTrue(eventFired);
            Assert.IsFalse(received.IsSuccess);
            Assert.AreEqual("Already shorting this stock", received.Message);
        }
    }
}
