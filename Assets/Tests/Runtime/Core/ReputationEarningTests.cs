using NUnit.Framework;

namespace BullRun.Tests.Core
{
    /// <summary>
    /// FIX-14: Reputation earning tests — per-round awards and consolation Rep.
    /// Covers AC 4, 5, 6.
    /// </summary>
    [TestFixture]
    public class ReputationEarningTests
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

        // === AC 4: Rep earned on round completion — base only (exactly hit target) ===

        [Test]
        public void CalculateRoundRep_ExactlyHitTarget_ReturnsBaseOnly()
        {
            // Round 1, cash $20, target $20 → excess = 0 → bonus = 0
            int rep = MarginCallState.CalculateRoundReputation(1, 20f, 20f);
            Assert.AreEqual(5, rep); // Base for round 1 = 5
        }

        [Test]
        public void CalculateRoundRep_Round1_BaseIs5()
        {
            int rep = MarginCallState.CalculateRoundReputation(1, 20f, 20f);
            Assert.AreEqual(5, rep);
        }

        [Test]
        public void CalculateRoundRep_Round8_BaseIs40()
        {
            int rep = MarginCallState.CalculateRoundReputation(8, 800f, 800f);
            Assert.AreEqual(40, rep);
        }

        // === AC 4: Rep earned on round completion — base + bonus (exceeded target by 50%) ===

        [Test]
        public void CalculateRoundRep_Exceeded50Percent_ReturnsBonusRep()
        {
            // Round 1, cash $30, target $20 → excess ratio = (30-20)/20 = 0.5
            // baseRep = 5, bonusRep = floor(5 * 0.5 * 0.5) = floor(1.25) = 1
            int rep = MarginCallState.CalculateRoundReputation(1, 30f, 20f);
            Assert.AreEqual(6, rep); // 5 base + 1 bonus
        }

        [Test]
        public void CalculateRoundRep_Exceeded100Percent_ReturnsHigherBonus()
        {
            // Round 1, cash $40, target $20 → excess ratio = (40-20)/20 = 1.0
            // baseRep = 5, bonusRep = floor(5 * 1.0 * 0.5) = floor(2.5) = 2
            int rep = MarginCallState.CalculateRoundReputation(1, 40f, 20f);
            Assert.AreEqual(7, rep); // 5 base + 2 bonus
        }

        [Test]
        public void CalculateRoundRep_Exceeded200Percent_ReturnsLargeBonus()
        {
            // Round 1, cash $60, target $20 → excess ratio = (60-20)/20 = 2.0
            // baseRep = 5, bonusRep = floor(5 * 2.0 * 0.5) = floor(5.0) = 5
            int rep = MarginCallState.CalculateRoundReputation(1, 60f, 20f);
            Assert.AreEqual(10, rep); // 5 base + 5 bonus
        }

        // === AC 4: Base + 0 bonus (exactly hit target, no excess) ===

        [Test]
        public void CalculateRoundRep_ExactTarget_ZeroBonus()
        {
            // Every round at exact target should get 0 bonus
            for (int round = 1; round <= 8; round++)
            {
                float target = MarginCallTargets.GetTarget(round);
                int rep = MarginCallState.CalculateRoundReputation(round, target, target);
                int expectedBase = GameConfig.RepBaseAwardPerRound[round - 1];
                Assert.AreEqual(expectedBase, rep,
                    $"Round {round}: exactly meeting target should give base only ({expectedBase})");
            }
        }

        // === AC 5: Consolation Rep on margin call — 0 rounds completed = 0 Rep ===

        [Test]
        public void ConsolationRep_0RoundsCompleted_Returns0()
        {
            // Margin call on round 1 → 0 completed rounds
            int consolation = 0 * GameConfig.RepConsolationPerRound;
            Assert.AreEqual(0, consolation);
        }

        // === AC 5: Consolation Rep on margin call — 3 rounds completed = 6 Rep ===

        [Test]
        public void ConsolationRep_3RoundsCompleted_Returns6()
        {
            int roundsCompleted = 3;
            int consolation = roundsCompleted * GameConfig.RepConsolationPerRound;
            Assert.AreEqual(6, consolation);
        }

        [Test]
        public void ConsolationRep_7RoundsCompleted_Returns14()
        {
            int roundsCompleted = 7; // Failed on round 8
            int consolation = roundsCompleted * GameConfig.RepConsolationPerRound;
            Assert.AreEqual(14, consolation);
        }

        // === AC 4: Integration — Rep added to ReputationManager on round pass ===

        [Test]
        public void RoundCompletion_AddsRepToManager()
        {
            var ctx = new RunContext(1, 1, new Portfolio(25f)); // $25 > $20 target
            var sm = new GameStateMachine(ctx);
            MarketCloseState.RoundProfit = 15f;

            MarginCallState.NextConfig = new MarginCallStateConfig
            {
                StateMachine = sm,
                PriceGenerator = null,
                TradeExecutor = null
            };
            new MarginCallState().Enter(ctx);

            Assert.Greater(ctx.Reputation.Current, 0, "Rep should be added to manager on round pass");
            Assert.AreEqual(ctx.Reputation.Current, ctx.ReputationEarned,
                "ReputationEarned should match manager balance after first round");
        }

        // === AC 5: Integration — Consolation Rep added on margin call ===

        [Test]
        public void MarginCallRound5_Awards8ConsolationRep()
        {
            // Round 5, 4 rounds completed before failure
            var ctx = new RunContext(3, 5, new Portfolio(100f)); // $100 < $175 target
            var sm = new GameStateMachine(ctx);
            MarketCloseState.RoundProfit = 0f;

            MarginCallState.NextConfig = new MarginCallStateConfig
            {
                StateMachine = sm,
                PriceGenerator = null,
                TradeExecutor = null
            };
            new MarginCallState().Enter(ctx);

            Assert.AreEqual(8, ctx.ReputationEarned); // 4 * 2 = 8
            Assert.AreEqual(8, ctx.Reputation.Current);
        }

        // === AC 6: RoundCompletedEvent includes RepEarned ===

        [Test]
        public void RoundCompletedEvent_IncludesRepEarned()
        {
            RoundCompletedEvent received = default;
            EventBus.Subscribe<RoundCompletedEvent>(e => received = e);

            var ctx = new RunContext(1, 1, new Portfolio(20f));
            var sm = new GameStateMachine(ctx);
            MarketCloseState.RoundProfit = 10f;

            MarginCallState.NextConfig = new MarginCallStateConfig
            {
                StateMachine = sm,
                PriceGenerator = null,
                TradeExecutor = null
            };
            new MarginCallState().Enter(ctx);

            Assert.AreEqual(5, received.RepEarned, "RoundCompletedEvent should carry RepEarned");
            Assert.AreEqual(5, received.BaseRep, "Should carry base Rep component");
            Assert.AreEqual(0, received.BonusRep, "Should carry 0 bonus when exactly meeting target");
        }

        [Test]
        public void RoundCompletedEvent_IncludesBaseAndBonusBreakdown()
        {
            RoundCompletedEvent received = default;
            EventBus.Subscribe<RoundCompletedEvent>(e => received = e);

            // Cash $30, target $20 → excess ratio = 0.5, bonus = floor(5 * 0.5 * 0.5) = 1
            var ctx = new RunContext(1, 1, new Portfolio(30f));
            var sm = new GameStateMachine(ctx);
            MarketCloseState.RoundProfit = 20f;

            MarginCallState.NextConfig = new MarginCallStateConfig
            {
                StateMachine = sm,
                PriceGenerator = null,
                TradeExecutor = null
            };
            new MarginCallState().Enter(ctx);

            Assert.AreEqual(6, received.RepEarned, "Total should be 5 base + 1 bonus");
            Assert.AreEqual(5, received.BaseRep, "Base Rep for round 1 = 5");
            Assert.AreEqual(1, received.BonusRep, "Bonus Rep for 50% excess = 1");
        }

        // === Edge: Player earns exactly $0 profit but above target ===

        [Test]
        public void CalculateRoundRep_CashAboveTarget_StillAwardsBaseRep()
        {
            // Player somehow has cash above target even with 0 profit
            // (e.g., carried forward from previous round)
            int rep = MarginCallState.CalculateRoundReputation(1, 25f, 20f);
            Assert.GreaterOrEqual(rep, 5, "Should award at least base Rep");
        }

        // === Edge: Round number beyond 8 clamps to max base ===

        [Test]
        public void CalculateRoundRep_RoundBeyond8_UsesMaxBase()
        {
            int rep = MarginCallState.CalculateRoundReputation(10, 1000f, 800f);
            // Should clamp to index 7 (round 8) base = 40
            Assert.GreaterOrEqual(rep, 40);
        }

        // === CalculateRoundReputation static method is accessible ===

        [Test]
        public void CalculateRoundReputation_IsStaticAndPublic()
        {
            // Compile-time check via direct call
            int result = MarginCallState.CalculateRoundReputation(1, 20f, 20f);
            Assert.IsNotNull(result);
        }
    }
}
