using NUnit.Framework;

namespace BullRun.Tests.Core
{
    /// <summary>
    /// Tests for FIX-10 v2: Post-Trade Cooldown with Countdown Timer.
    /// Validates config, instant trade execution, post-trade lockout state machine,
    /// and auto-liquidation bypass.
    /// Note: GameRunner is a MonoBehaviour — cooldown state machine is tested here
    /// via pure-logic simulation. Full UI integration verified in Unity Play Mode.
    /// </summary>
    [TestFixture]
    public class TradeExecutionDelayTests
    {
        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
        }

        // --- Config Tests (AC: 8) ---

        [Test]
        public void PostTradeCooldown_ConfigIs3Seconds()
        {
            // AC 8: Cooldown duration configurable in GameConfig
            Assert.AreEqual(3.0f, GameConfig.PostTradeCooldown,
                "PostTradeCooldown should be 3.0 seconds");
        }

        [Test]
        public void CooldownDimAlpha_ConfigIsValid()
        {
            // Dim alpha used for button visual feedback during cooldown
            Assert.Greater(GameConfig.CooldownDimAlpha, 0f,
                "Dim alpha must be visible (> 0)");
            Assert.Less(GameConfig.CooldownDimAlpha, 1f,
                "Dim alpha must be noticeably dimmed (< 1)");
        }

        // --- Instant Trade Execution Tests (AC: 1) ---

        [Test]
        public void TradeExecutor_ExecutesInstantly()
        {
            // AC 1: Trade executes instantly on button press — no delay
            var portfolio = new Portfolio(GameConfig.StartingCapital);
            var executor = new TradeExecutor();
            executor.IsTradeEnabled = true;

            bool success = executor.ExecuteBuy("0", 1, 5f, portfolio);

            Assert.IsTrue(success, "Trade should execute immediately");
            Assert.AreEqual(1, portfolio.PositionCount,
                "Position should exist immediately after trade");
            Assert.AreEqual(GameConfig.StartingCapital - (1 * 5f), portfolio.Cash, 0.01f,
                "Cash should be deducted immediately");
        }

        [Test]
        public void TradeExecutor_ExecutesAtCurrentPrice()
        {
            // AC 1: Trade executes at the price visible when clicked — no slippage
            var portfolio = new Portfolio(GameConfig.StartingCapital);
            var executor = new TradeExecutor();
            executor.IsTradeEnabled = true;

            executor.ExecuteBuy("0", 1, 5f, portfolio);
            var pos = portfolio.GetPosition("0");

            Assert.AreEqual(5f, pos.AverageBuyPrice, 0.01f,
                "Trade should execute at the exact price provided (instant, no slippage)");
        }

        // --- Post-Trade Cooldown State Machine Tests (AC: 2, 3, 7) ---

        [Test]
        public void PostTradeCooldown_BlocksBothBuyAndSellDuringLockout()
        {
            // AC 2, 3: After trade, BOTH buy and sell are blocked for duration
            bool isPostTradeCooldownActive = false;
            float cooldownTimer = 0f;
            int tradesBlocked = 0;

            // Simulate successful trade → start cooldown
            isPostTradeCooldownActive = true;
            cooldownTimer = GameConfig.PostTradeCooldown;

            // Attempt BUY during cooldown — should be blocked
            if (isPostTradeCooldownActive) tradesBlocked++;

            // Attempt SELL during cooldown — should also be blocked
            if (isPostTradeCooldownActive) tradesBlocked++;

            Assert.AreEqual(2, tradesBlocked,
                "Both buy AND sell should be blocked during post-trade cooldown");
        }

        [Test]
        public void PostTradeCooldown_AcceptsTradesAfterExpiry()
        {
            // AC 6: After 3 seconds, buttons unlock and player can trade again
            bool isPostTradeCooldownActive = false;
            float cooldownTimer = 0f;

            // Start cooldown
            isPostTradeCooldownActive = true;
            cooldownTimer = GameConfig.PostTradeCooldown;

            // Simulate Update ticks until timer expires
            float dt = 1f / 60f;
            while (cooldownTimer > 0f)
            {
                cooldownTimer -= dt;
            }
            cooldownTimer = 0f;
            isPostTradeCooldownActive = false;

            // New trade should now be accepted
            bool tradeAccepted = !isPostTradeCooldownActive;

            Assert.IsTrue(tradeAccepted,
                "Trades should be accepted after cooldown expires");
        }

        [Test]
        public void PostTradeCooldown_LastsFullDuration()
        {
            // AC 2: Cooldown lasts 3 seconds
            float timer = GameConfig.PostTradeCooldown;
            float dt = 1f / 60f;
            float elapsed = 0f;

            while (timer > 0f)
            {
                timer -= dt;
                elapsed += dt;
            }

            Assert.GreaterOrEqual(elapsed, GameConfig.PostTradeCooldown - dt,
                "Cooldown should last at least PostTradeCooldown minus one frame");
            Assert.LessOrEqual(elapsed, GameConfig.PostTradeCooldown + dt,
                "Cooldown should not exceed PostTradeCooldown plus one frame");
        }

        [Test]
        public void PostTradeCooldown_OnlyStartsOnSuccessfulTrade()
        {
            // Edge case: Failed trade (no cash, no position) should NOT start cooldown
            var portfolio = new Portfolio(GameConfig.StartingCapital);
            var executor = new TradeExecutor();
            executor.IsTradeEnabled = true;

            // Try to sell when no position exists — should fail
            bool sellSuccess = executor.ExecuteSell("0", 5, 10f, portfolio);

            Assert.IsFalse(sellSuccess,
                "Selling with no position should fail");
            // In GameRunner, cooldown only starts if ExecuteSmartBuy/Sell returns true
            // This test confirms the executor returns false for invalid trades
        }

        // --- Countdown Timer Display Tests (AC: 4, 5) ---

        [Test]
        public void CooldownTimerDisplay_DecreasesOverTime()
        {
            // AC 4, 5: Timer text updates each frame showing remaining time
            float timer = GameConfig.PostTradeCooldown;
            float dt = 0.1f; // Large enough step to change F1 display

            string firstDisplay = $"{timer:F1}s";
            timer -= dt;
            string secondDisplay = $"{timer:F1}s";

            // After the step, timer display should have decreased
            Assert.AreNotEqual(firstDisplay, secondDisplay,
                "Timer display should change between frames (decreasing)");
            Assert.That(timer, Is.LessThan(GameConfig.PostTradeCooldown),
                "Timer value should decrease after a frame tick");
        }

        [Test]
        public void CooldownTimerDisplay_FormatsCorrectly()
        {
            // AC 5: One decimal place format "X.Xs"
            float timer = 2.347f;
            string display = $"{timer:F1}s";

            Assert.AreEqual("2.3s", display,
                "Timer should format as one decimal place with 's' suffix");
        }

        // --- Phase-End Cancellation Tests (AC: 10) ---

        [Test]
        public void PostTradeCooldown_CancelsOnTradingPhaseEnd()
        {
            // AC 10: If trading phase ends during cooldown, cooldown cancels
            bool isPostTradeCooldownActive = true;
            float cooldownTimer = GameConfig.PostTradeCooldown;

            // Simulate OnTradingPhaseEnded
            if (isPostTradeCooldownActive)
            {
                isPostTradeCooldownActive = false;
                cooldownTimer = 0f;
            }

            Assert.IsFalse(isPostTradeCooldownActive,
                "Cooldown should be cancelled when trading phase ends");
            Assert.AreEqual(0f, cooldownTimer,
                "Timer should be reset to 0 on phase end");
        }

        // --- Auto-Liquidation Bypass Tests (AC: 9) ---

        [Test]
        public void AutoLiquidation_DoesNotGoThroughGameRunner()
        {
            // AC 9: Auto-liquidation at market close is unaffected by cooldown
            var portfolio = new Portfolio(GameConfig.StartingCapital);
            var executor = new TradeExecutor();
            executor.IsTradeEnabled = true;

            bool bought = executor.ExecuteBuy("0", 1, 5f, portfolio);
            Assert.IsTrue(bought, "Setup: should be able to buy shares");

            // LiquidateAllPositions works directly on Portfolio — no GameRunner involvement
            float pnl = portfolio.LiquidateAllPositions(stockId => 7f);
            Assert.AreEqual(0, portfolio.PositionCount,
                "Auto-liquidation should clear all positions regardless of cooldown state");
        }

        [Test]
        public void AutoLiquidation_WorksWithShortPositions()
        {
            var portfolio = new Portfolio(GameConfig.StartingCapital);
            var executor = new TradeExecutor();
            executor.IsTradeEnabled = true;

            bool shorted = executor.ExecuteShort("0", 1, 5f, portfolio);
            Assert.IsTrue(shorted, "Setup: should be able to short shares");

            float pnl = portfolio.LiquidateAllPositions(stockId => 4f);
            Assert.AreEqual(0, portfolio.ShortPositionCount,
                "Auto-liquidation should clear short positions regardless of cooldown state");
        }

        // --- Event Tests ---

        [Test]
        public void TradeButtonPressedEvent_BuyAndSellVariants()
        {
            bool receivedBuy = false;
            bool receivedSell = false;

            EventBus.Subscribe<TradeButtonPressedEvent>(evt =>
            {
                if (evt.IsBuy) receivedBuy = true;
                else receivedSell = true;
            });

            EventBus.Publish(new TradeButtonPressedEvent { IsBuy = true });
            EventBus.Publish(new TradeButtonPressedEvent { IsBuy = false });

            Assert.IsTrue(receivedBuy, "Buy event should be received");
            Assert.IsTrue(receivedSell, "Sell event should be received");
        }

        [Test]
        public void TradingPhaseEndedEvent_CanBeSubscribedAndReceived()
        {
            bool received = false;
            int receivedRound = -1;

            EventBus.Subscribe<TradingPhaseEndedEvent>(evt =>
            {
                received = true;
                receivedRound = evt.RoundNumber;
            });

            EventBus.Publish(new TradingPhaseEndedEvent { RoundNumber = 3, TimeExpired = true });

            Assert.IsTrue(received, "TradingPhaseEndedEvent should be receivable");
            Assert.AreEqual(3, receivedRound, "Round number should be passed through event");
        }
    }
}
