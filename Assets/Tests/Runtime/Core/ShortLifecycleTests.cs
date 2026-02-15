using NUnit.Framework;
using UnityEngine;

namespace BullRun.Tests.Core
{
    /// <summary>
    /// FIX-11: Tests for the short selling lifecycle.
    /// Tests GameConfig constants, P&L calculations, TradeExecutor short operations,
    /// and the ShortState enum. State machine runtime behavior (timer transitions,
    /// button visuals) requires PlayMode testing in Unity Editor.
    /// </summary>
    [TestFixture]
    public class ShortLifecycleTests
    {
        private Portfolio _portfolio;
        private TradeExecutor _executor;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            _portfolio = new Portfolio(1000f);
            _executor = new TradeExecutor();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
        }

        // --- GameConfig Short Constants ---

        [Test]
        public void GameConfig_ShortRoundStartLockout_Is5Seconds()
        {
            Assert.AreEqual(5.0f, GameConfig.ShortRoundStartLockout, 0.001f);
        }

        [Test]
        public void GameConfig_ShortForcedHoldDuration_Is8Seconds()
        {
            Assert.AreEqual(8.0f, GameConfig.ShortForcedHoldDuration, 0.001f);
        }

        [Test]
        public void GameConfig_ShortCashOutWindow_Is10Seconds()
        {
            Assert.AreEqual(10.0f, GameConfig.ShortCashOutWindow, 0.001f);
        }

        [Test]
        public void GameConfig_ShortCashOutFlashThreshold_Is4Seconds()
        {
            Assert.AreEqual(4.0f, GameConfig.ShortCashOutFlashThreshold, 0.001f);
        }

        [Test]
        public void GameConfig_ShortPostCloseCooldown_Is10Seconds()
        {
            Assert.AreEqual(10.0f, GameConfig.ShortPostCloseCooldown, 0.001f);
        }

        [Test]
        public void GameConfig_ShortBaseShares_Is1()
        {
            Assert.AreEqual(1, GameConfig.ShortBaseShares);
        }

        // --- ShortState Enum Values ---

        [Test]
        public void ShortState_HasAllExpectedValues()
        {
            Assert.AreEqual(0, (int)GameRunner.ShortState.RoundLockout);
            Assert.AreEqual(1, (int)GameRunner.ShortState.Ready);
            Assert.AreEqual(2, (int)GameRunner.ShortState.Holding);
            Assert.AreEqual(3, (int)GameRunner.ShortState.CashOutWindow);
            Assert.AreEqual(4, (int)GameRunner.ShortState.Cooldown);
        }

        // --- Short P&L Calculations ---

        [Test]
        public void ShortPnL_PriceDrops_Profit()
        {
            // Short at $50, price drops to $40 -> profit = (50-40)*1 = $10
            float entryPrice = 50.00f;
            float currentPrice = 40.00f;
            int shares = GameConfig.ShortBaseShares;
            float pnl = (entryPrice - currentPrice) * shares;
            Assert.AreEqual(10.00f, pnl, 0.001f);
        }

        [Test]
        public void ShortPnL_PriceRises_Loss()
        {
            // Short at $50, price rises to $60 -> loss = (50-60)*1 = -$10
            float entryPrice = 50.00f;
            float currentPrice = 60.00f;
            int shares = GameConfig.ShortBaseShares;
            float pnl = (entryPrice - currentPrice) * shares;
            Assert.AreEqual(-10.00f, pnl, 0.001f);
        }

        [Test]
        public void ShortPnL_PriceUnchanged_Zero()
        {
            float entryPrice = 50.00f;
            float currentPrice = 50.00f;
            int shares = GameConfig.ShortBaseShares;
            float pnl = (entryPrice - currentPrice) * shares;
            Assert.AreEqual(0f, pnl, 0.001f);
        }

        [Test]
        public void ShortPnL_PriceDropsToZero_MaxProfit()
        {
            // Short at $50, price drops to $0 -> profit = (50-0)*1 = $50
            float entryPrice = 50.00f;
            float currentPrice = 0.00f;
            int shares = GameConfig.ShortBaseShares;
            float pnl = (entryPrice - currentPrice) * shares;
            Assert.AreEqual(50.00f, pnl, 0.001f);
        }

        // --- Short Open via TradeExecutor ---

        [Test]
        public void OpenShort_BaseShares_UsesGameConfigValue()
        {
            bool success = _executor.ExecuteShort("ACME", GameConfig.ShortBaseShares, 50.00f, _portfolio);
            Assert.IsTrue(success);
            var pos = _portfolio.GetShortPosition("ACME");
            Assert.IsNotNull(pos);
            Assert.AreEqual(GameConfig.ShortBaseShares, pos.Shares);
        }

        [Test]
        public void OpenShort_PublishesTradeExecutedEvent()
        {
            TradeExecutedEvent received = default;
            bool eventFired = false;
            EventBus.Subscribe<TradeExecutedEvent>(e => { received = e; eventFired = true; });

            _executor.ExecuteShort("ACME", GameConfig.ShortBaseShares, 50.00f, _portfolio);

            Assert.IsTrue(eventFired);
            Assert.IsFalse(received.IsBuy);
            Assert.IsTrue(received.IsShort);
            Assert.AreEqual(GameConfig.ShortBaseShares, received.Shares);
        }

        // --- Short Close (Cover) via TradeExecutor ---

        [Test]
        public void CoverShort_PriceDropped_RealizesProfit()
        {
            _executor.ExecuteShort("ACME", 1, 50.00f, _portfolio);
            float cashAfterShort = _portfolio.Cash;
            _executor.ExecuteCover("ACME", 1, 40.00f, _portfolio);
            // P&L = (50-40)*1 = +10, added directly to cash
            Assert.Greater(_portfolio.Cash, cashAfterShort);
            Assert.IsFalse(_portfolio.HasShortPosition("ACME"));
        }

        [Test]
        public void CoverShort_PriceRose_RealizesLoss()
        {
            _executor.ExecuteShort("ACME", 1, 50.00f, _portfolio);
            float cashAfterShort = _portfolio.Cash;
            _executor.ExecuteCover("ACME", 1, 60.00f, _portfolio);
            // P&L = (50-60)*1 = -10, deducted from cash
            Assert.Less(_portfolio.Cash, cashAfterShort);
            Assert.IsFalse(_portfolio.HasShortPosition("ACME"));
        }

        [Test]
        public void CoverShort_PublishesTradeExecutedEvent()
        {
            _executor.ExecuteShort("ACME", 1, 50.00f, _portfolio);

            TradeExecutedEvent received = default;
            bool eventFired = false;
            EventBus.Subscribe<TradeExecutedEvent>(e => { received = e; eventFired = true; });

            _executor.ExecuteCover("ACME", 1, 40.00f, _portfolio);

            Assert.IsTrue(eventFired);
            Assert.IsTrue(received.IsBuy); // Cover is a buy-back
            Assert.IsTrue(received.IsShort);
        }

        // --- Only One Short At A Time ---

        [Test]
        public void OnlyOneShortPerStock()
        {
            _executor.ExecuteShort("ACME", 1, 50.00f, _portfolio);
            bool result = _executor.ExecuteShort("ACME", 1, 45.00f, _portfolio);
            Assert.IsFalse(result);
            Assert.AreEqual(1, _portfolio.ShortPositionCount);
        }

        // --- ShortSqueeze Worsens Short P&L ---

        [Test]
        public void ShortSqueeze_PriceSpike_WorsensShortPnL()
        {
            _portfolio.OpenShort("ACME", 1, 50.00f);
            // Before squeeze: price at entry -> pnl = 0
            float pnlBefore = _portfolio.GetShortPosition("ACME").UnrealizedPnL(50.00f);
            Assert.AreEqual(0f, pnlBefore, 0.001f);

            // After squeeze: price spikes to 80 -> pnl = (50-80)*1 = -30
            float pnlAfter = _portfolio.GetShortPosition("ACME").UnrealizedPnL(80.00f);
            Assert.AreEqual(-30.00f, pnlAfter, 0.001f);
        }

        // --- TradeFeedback Short Messages ---

        [Test]
        public void TradeFeedback_ShortRejection_DuplicateShort()
        {
            _portfolio.OpenShort("ACME", 1, 50.00f);
            string reason = TradeFeedback.GetShortRejectionReason(_portfolio, "ACME");
            Assert.AreEqual("Already shorting this stock", reason);
        }

        [Test]
        public void TradeFeedback_ShortRejection_NoExistingShort()
        {
            string reason = TradeFeedback.GetShortRejectionReason(_portfolio, "ACME");
            Assert.AreEqual("Short rejected", reason);
        }

        [Test]
        public void TradeFeedback_ShortColor_IsPink()
        {
            var color = TradeFeedback.GetFeedbackColor(true, false, true);
            Assert.AreEqual(TradeFeedback.ShortPink, color);
        }

        // --- Liquidation with Active Short ---

        [Test]
        public void LiquidateAll_ClosesActiveShort()
        {
            _portfolio.OpenShort("ACME", 1, 50.00f);
            float pnl = _portfolio.LiquidateAllPositions(id => 40.00f);
            Assert.AreEqual(10.00f, pnl, 0.001f); // (50-40)*1
            Assert.AreEqual(0, _portfolio.ShortPositionCount);
        }
    }
}
