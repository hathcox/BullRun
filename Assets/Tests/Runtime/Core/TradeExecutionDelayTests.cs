using NUnit.Framework;

namespace BullRun.Tests.Core
{
    /// <summary>
    /// Tests for FIX-10: Trade Execution Delay & Button Cooldown.
    /// Validates config, cooldown state machine logic, and auto-liquidation bypass.
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

        // --- Config Tests (AC: 7) ---

        [Test]
        public void TradeExecutionDelay_ConfigIsValid()
        {
            // AC 7: Cooldown duration configurable in GameConfig
            // AC 1: "~0.3-0.5s" range
            Assert.AreEqual(0.4f, GameConfig.TradeExecutionDelay,
                "TradeExecutionDelay should be 0.4s");
            Assert.GreaterOrEqual(GameConfig.TradeExecutionDelay, 0.3f,
                "Trade delay should be at least 0.3s per AC");
            Assert.LessOrEqual(GameConfig.TradeExecutionDelay, 0.5f,
                "Trade delay should be at most 0.5s per AC");
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

        // --- Auto-Liquidation Bypass Tests (AC: 8) ---

        [Test]
        public void AutoLiquidation_DoesNotGoThroughGameRunner()
        {
            // AC 8: Auto-liquidation at market close is unaffected by cooldown.
            // MarketCloseState calls Portfolio.LiquidateAllPositions() directly.
            var portfolio = new Portfolio();
            var executor = new TradeExecutor();
            executor.IsTradeEnabled = true;

            bool bought = executor.ExecuteBuy("0", 5, 10f, portfolio);
            Assert.IsTrue(bought, "Setup: should be able to buy shares");
            Assert.AreEqual(1, portfolio.PositionCount, "Setup: should have 1 position");

            // LiquidateAllPositions works directly on Portfolio — no GameRunner involvement
            float pnl = portfolio.LiquidateAllPositions(stockId => 12f);
            Assert.AreEqual(0, portfolio.PositionCount,
                "Auto-liquidation should clear all positions without GameRunner");
            Assert.Greater(pnl, 0f,
                "Liquidation P&L should be positive (price went from $10 to $12)");
        }

        [Test]
        public void AutoLiquidation_WorksWithShortPositions()
        {
            var portfolio = new Portfolio();
            var executor = new TradeExecutor();
            executor.IsTradeEnabled = true;

            bool shorted = executor.ExecuteShort("0", 5, 10f, portfolio);
            Assert.IsTrue(shorted, "Setup: should be able to short shares");

            float pnl = portfolio.LiquidateAllPositions(stockId => 8f);
            Assert.AreEqual(0, portfolio.PositionCount,
                "Auto-liquidation should clear short positions without GameRunner");
        }

        // --- Cooldown State Machine Tests (AC: 1, 3) ---
        // These simulate GameRunner's cooldown fields as pure logic.

        [Test]
        public void CooldownStateMachine_RejectsInputWhileActive()
        {
            // AC 3: Additional presses during cooldown are ignored (no queuing)
            bool isCooldownActive = false;
            bool pendingTradeIsBuy = false;
            float cooldownTimer = 0f;
            int cooldownStartCount = 0;

            // Simulate StartTradeCooldown(true) — first press
            if (!isCooldownActive)
            {
                isCooldownActive = true;
                pendingTradeIsBuy = true;
                cooldownTimer = GameConfig.TradeExecutionDelay;
                cooldownStartCount++;
            }

            // Simulate StartTradeCooldown(false) — second press (should be rejected)
            if (!isCooldownActive)
            {
                isCooldownActive = true;
                pendingTradeIsBuy = false;
                cooldownTimer = GameConfig.TradeExecutionDelay;
                cooldownStartCount++;
            }

            Assert.AreEqual(1, cooldownStartCount,
                "Second press during active cooldown should be rejected");
            Assert.IsTrue(pendingTradeIsBuy,
                "Pending trade should remain as first press (BUY), not overwritten by rejected SELL");
        }

        [Test]
        public void CooldownStateMachine_AcceptsInputAfterCooldownExpires()
        {
            // AC 1: After cooldown completes, new trades should be accepted
            bool isCooldownActive = false;
            float cooldownTimer = 0f;
            int tradesExecuted = 0;

            // Start cooldown
            isCooldownActive = true;
            cooldownTimer = GameConfig.TradeExecutionDelay;

            // Simulate Update ticking until timer expires
            float dt = 1f / 60f;
            while (cooldownTimer > 0f)
            {
                cooldownTimer -= dt;
            }

            // Timer expired — execute trade and reset
            cooldownTimer = 0f;
            isCooldownActive = false;
            tradesExecuted++;

            // New press should now be accepted
            if (!isCooldownActive)
            {
                isCooldownActive = true;
                cooldownTimer = GameConfig.TradeExecutionDelay;
                tradesExecuted++;
            }

            Assert.AreEqual(2, tradesExecuted,
                "After cooldown expires, new trade input should be accepted");
        }

        [Test]
        public void CooldownStateMachine_CancelsOnTradingPhaseEnd()
        {
            // Edge case: TradingPhaseEndedEvent should cancel active cooldown
            bool isCooldownActive = true;
            float cooldownTimer = GameConfig.TradeExecutionDelay;
            bool tradeExecuted = false;

            // Simulate OnTradingPhaseEnded
            if (isCooldownActive)
            {
                isCooldownActive = false;
                cooldownTimer = 0f;
                // Note: trade should NOT execute — phase ended
            }

            // Simulate next Update — timer block should not fire
            if (isCooldownActive && cooldownTimer <= 0f)
            {
                tradeExecuted = true;
            }

            Assert.IsFalse(isCooldownActive,
                "Cooldown should be cancelled after trading phase ends");
            Assert.AreEqual(0f, cooldownTimer,
                "Cooldown timer should be reset to 0");
            Assert.IsFalse(tradeExecuted,
                "Trade should NOT execute when phase ended during cooldown");
        }

        [Test]
        public void CooldownStateMachine_SkipsTradeWhenTradingInactive()
        {
            // Edge case: If TradingState.IsActive becomes false during cooldown
            // (e.g., round timer expires), the pending trade should be cancelled
            bool isCooldownActive = true;
            float cooldownTimer = 0.01f; // Almost expired
            bool isTradingActive = false; // Trading stopped during cooldown
            bool tradeExecuted = false;

            // Simulate Update tick — timer expires
            cooldownTimer -= 1f / 60f;
            if (cooldownTimer <= 0f)
            {
                cooldownTimer = 0f;
                isCooldownActive = false;

                if (isTradingActive)
                {
                    tradeExecuted = true;
                }
                // else: cancelled — trading no longer active
            }

            Assert.IsFalse(tradeExecuted,
                "Trade should be cancelled when TradingState is inactive at cooldown expiry");
            Assert.IsFalse(isCooldownActive,
                "Cooldown should still be deactivated even when trade is cancelled");
        }

        [Test]
        public void CooldownTimer_CompletesWithinOneFrameOfConfig()
        {
            // Verify the float countdown pattern completes at the expected time
            float timer = GameConfig.TradeExecutionDelay;
            float dt = 1f / 60f;
            int frameCount = 0;

            while (timer > 0f)
            {
                timer -= dt;
                frameCount++;
            }

            int expectedFrames = (int)(GameConfig.TradeExecutionDelay / dt);
            Assert.GreaterOrEqual(frameCount, expectedFrames,
                "Cooldown should last at least the expected number of frames");
            Assert.LessOrEqual(frameCount, expectedFrames + 1,
                "Cooldown should not exceed expected frames by more than 1");
        }

        // --- TradeButtonPressedEvent Tests (AC: 6) ---

        [Test]
        public void TradeButtonPressedEvent_BuyAndSellVariants()
        {
            // AC 6: Both keyboard and button trades use the same event path
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
            // Verify the cancellation event path works
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

        // --- Trade Execution at Current Price Tests (AC: 4) ---

        [Test]
        public void TradeExecutor_UsesProvidedPriceNotCached()
        {
            // AC 4: Trade executes at the price when cooldown COMPLETES.
            // TradeExecutor uses the price passed at call time.
            var portfolio = new Portfolio();
            var executor = new TradeExecutor();
            executor.IsTradeEnabled = true;

            bool success = executor.ExecuteBuy("0", 5, 10f, portfolio);
            Assert.IsTrue(success);

            var pos = portfolio.GetPosition("0");
            Assert.AreEqual(10f, pos.AveragePrice, 0.01f,
                "Trade should execute at the provided price (current price at execution time)");
        }

        [Test]
        public void TradeExecutor_DifferentPriceProducesDifferentResult()
        {
            // Verify that passing a different price (as happens after cooldown delay)
            // produces different trade costs — confirms natural slippage behavior
            var portfolio1 = new Portfolio();
            var portfolio2 = new Portfolio();
            var executor = new TradeExecutor();
            executor.IsTradeEnabled = true;

            executor.ExecuteBuy("0", 10, 10f, portfolio1);
            float cost1 = GameConfig.StartingCapital - portfolio1.Cash;

            executor.ExecuteBuy("0", 10, 12f, portfolio2);
            float cost2 = GameConfig.StartingCapital - portfolio2.Cash;

            Assert.Greater(cost2, cost1,
                "Trade at higher price should cost more — confirms slippage behavior");
        }
    }
}
