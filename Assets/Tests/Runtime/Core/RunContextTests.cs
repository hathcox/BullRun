using System.Collections.Generic;
using NUnit.Framework;

namespace BullRun.Tests.Core
{
    [TestFixture]
    public class RunContextTests
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

        [Test]
        public void Constructor_SetsCurrentAct()
        {
            var portfolio = new Portfolio(1000f);
            var ctx = new RunContext(1, 1, portfolio);
            Assert.AreEqual(1, ctx.CurrentAct);
        }

        [Test]
        public void Constructor_SetsCurrentRound()
        {
            var portfolio = new Portfolio(1000f);
            var ctx = new RunContext(1, 3, portfolio);
            Assert.AreEqual(3, ctx.CurrentRound);
        }

        [Test]
        public void Constructor_SetsPortfolio()
        {
            var portfolio = new Portfolio(1000f);
            var ctx = new RunContext(1, 1, portfolio);
            Assert.IsNotNull(ctx.Portfolio);
            Assert.AreEqual(1000f, ctx.Portfolio.Cash, 0.001f);
        }

        [Test]
        public void ActiveItems_DefaultsToEmptyList()
        {
            var portfolio = new Portfolio(1000f);
            var ctx = new RunContext(1, 1, portfolio);
            Assert.IsNotNull(ctx.ActiveItems);
            Assert.AreEqual(0, ctx.ActiveItems.Count);
        }

        [Test]
        public void ActiveItems_CanAddItems()
        {
            var portfolio = new Portfolio(1000f);
            var ctx = new RunContext(1, 1, portfolio);
            ctx.ActiveItems.Add("TestItem");
            Assert.AreEqual(1, ctx.ActiveItems.Count);
            Assert.AreEqual("TestItem", ctx.ActiveItems[0]);
        }
        // --- Run Initialization Tests (Story 2.5 Task 1) ---

        [Test]
        public void StartNewRun_CreatesPortfolioWithStartingCapital()
        {
            var ctx = RunContext.StartNewRun();
            Assert.AreEqual(GameConfig.StartingCapital, ctx.Portfolio.Cash, 0.001f);
        }

        [Test]
        public void StartNewRun_SetsActToOne()
        {
            var ctx = RunContext.StartNewRun();
            Assert.AreEqual(1, ctx.CurrentAct);
        }

        [Test]
        public void StartNewRun_SetsRoundToOne()
        {
            var ctx = RunContext.StartNewRun();
            Assert.AreEqual(1, ctx.CurrentRound);
        }

        [Test]
        public void StartNewRun_InitializesEmptyActiveItems()
        {
            var ctx = RunContext.StartNewRun();
            Assert.IsNotNull(ctx.ActiveItems);
            Assert.AreEqual(0, ctx.ActiveItems.Count);
        }

        [Test]
        public void GetCurrentCash_ReturnsPortfolioCash()
        {
            var ctx = RunContext.StartNewRun();
            Assert.AreEqual(GameConfig.StartingCapital, ctx.GetCurrentCash(), 0.001f);
        }

        [Test]
        public void GetCurrentCash_ReflectsPortfolioChanges()
        {
            var ctx = RunContext.StartNewRun();
            ctx.Portfolio.OpenPosition("ACME", 1, 5.00f);
            Assert.AreEqual(GameConfig.StartingCapital - 5f, ctx.GetCurrentCash(), 0.001f);
        }
        [Test]
        public void StartNewRun_PublishesRunStartedEvent()
        {
            RunStartedEvent received = default;
            bool wasCalled = false;
            EventBus.Subscribe<RunStartedEvent>(e =>
            {
                received = e;
                wasCalled = true;
            });

            RunContext.StartNewRun();

            Assert.IsTrue(wasCalled);
            Assert.AreEqual(GameConfig.StartingCapital, received.StartingCapital, 0.001f);
        }

        // --- Round Transition Tests (Story 2.5 Task 4) ---

        [Test]
        public void PrepareForNextRound_IncrementsRound()
        {
            var ctx = RunContext.StartNewRun();
            Assert.AreEqual(1, ctx.CurrentRound);
            ctx.PrepareForNextRound();
            Assert.AreEqual(2, ctx.CurrentRound);
        }

        [Test]
        public void PrepareForNextRound_CashCarriesForward()
        {
            var ctx = RunContext.StartNewRun();
            ctx.Portfolio.OpenPosition("ACME", 10, 25.00f); // cash: 750
            ctx.Portfolio.ClosePosition("ACME", 10, 30.00f); // cash: 1050
            float cashBefore = ctx.GetCurrentCash();
            ctx.PrepareForNextRound();
            Assert.AreEqual(cashBefore, ctx.GetCurrentCash(), 0.001f);
        }

        [Test]
        public void PrepareForNextRound_UpdatesActCorrectly()
        {
            var ctx = RunContext.StartNewRun();
            ctx.CurrentRound = 2; // Round 2 → 3 = Act 1 → Act 2
            ctx.PrepareForNextRound();
            Assert.AreEqual(2, ctx.CurrentAct);
        }

        [Test]
        public void PrepareForNextRound_MultipleRounds_CashCompounds()
        {
            var ctx = RunContext.StartNewRun(); // cash: $10
            // Round 1: earn profit
            ctx.Portfolio.OpenPosition("ACME", 1, 5.00f); // cash: $5
            ctx.Portfolio.LiquidateAllPositions(id => 8.00f); // cash: $13
            ctx.PrepareForNextRound();
            Assert.AreEqual(2, ctx.CurrentRound);
            Assert.AreEqual(13f, ctx.GetCurrentCash(), 0.001f);

            // Round 2: earn more
            ctx.Portfolio.OpenPosition("ACME", 1, 6.00f); // cash: $7
            ctx.Portfolio.LiquidateAllPositions(id => 9.00f); // cash: $16
            ctx.PrepareForNextRound();
            Assert.AreEqual(3, ctx.CurrentRound);
            Assert.AreEqual(16f, ctx.GetCurrentCash(), 0.001f);
        }

        // --- AdvanceRound Tests (Story 4.5 Task 2) ---

        [Test]
        public void AdvanceRound_IncrementsRoundNumber()
        {
            var ctx = RunContext.StartNewRun();
            Assert.AreEqual(1, ctx.CurrentRound);
            ctx.AdvanceRound();
            Assert.AreEqual(2, ctx.CurrentRound);
        }

        [Test]
        public void AdvanceRound_UpdatesActAtBoundary_Round2ToRound3()
        {
            var ctx = RunContext.StartNewRun();
            ctx.CurrentRound = 2;
            ctx.AdvanceRound(); // Round 3 = Act 2
            Assert.AreEqual(3, ctx.CurrentRound);
            Assert.AreEqual(2, ctx.CurrentAct);
        }

        [Test]
        public void AdvanceRound_UpdatesActAtBoundary_Round4ToRound5()
        {
            var ctx = RunContext.StartNewRun();
            ctx.CurrentRound = 4;
            ctx.CurrentAct = 2;
            ctx.AdvanceRound(); // Round 5 = Act 3
            Assert.AreEqual(5, ctx.CurrentRound);
            Assert.AreEqual(3, ctx.CurrentAct);
        }

        [Test]
        public void AdvanceRound_UpdatesActAtBoundary_Round6ToRound7()
        {
            var ctx = RunContext.StartNewRun();
            ctx.CurrentRound = 6;
            ctx.CurrentAct = 3;
            ctx.AdvanceRound(); // Round 7 = Act 4
            Assert.AreEqual(7, ctx.CurrentRound);
            Assert.AreEqual(4, ctx.CurrentAct);
        }

        [Test]
        public void AdvanceRound_NoActChangeWithinSameAct()
        {
            var ctx = RunContext.StartNewRun();
            ctx.AdvanceRound(); // Round 1 → 2, still Act 1
            Assert.AreEqual(2, ctx.CurrentRound);
            Assert.AreEqual(1, ctx.CurrentAct);
        }

        [Test]
        public void AdvanceRound_ReturnsTrueWhenActChanged()
        {
            var ctx = RunContext.StartNewRun();
            ctx.CurrentRound = 2;
            bool actChanged = ctx.AdvanceRound(); // Round 2 → 3 = Act 1 → 2
            Assert.IsTrue(actChanged);
        }

        [Test]
        public void AdvanceRound_ReturnsFalseWhenActNotChanged()
        {
            var ctx = RunContext.StartNewRun();
            bool actChanged = ctx.AdvanceRound(); // Round 1 → 2, still Act 1
            Assert.IsFalse(actChanged);
        }

        // --- GetCurrentAct Tests (Story 4.5 Task 2) ---

        [Test]
        public void GetCurrentAct_Round1_ReturnsAct1()
        {
            Assert.AreEqual(1, RunContext.GetActForRound(1));
        }

        [Test]
        public void GetCurrentAct_Round2_ReturnsAct1()
        {
            Assert.AreEqual(1, RunContext.GetActForRound(2));
        }

        [Test]
        public void GetCurrentAct_Round3_ReturnsAct2()
        {
            Assert.AreEqual(2, RunContext.GetActForRound(3));
        }

        [Test]
        public void GetCurrentAct_Round4_ReturnsAct2()
        {
            Assert.AreEqual(2, RunContext.GetActForRound(4));
        }

        [Test]
        public void GetCurrentAct_Round5_ReturnsAct3()
        {
            Assert.AreEqual(3, RunContext.GetActForRound(5));
        }

        [Test]
        public void GetCurrentAct_Round6_ReturnsAct3()
        {
            Assert.AreEqual(3, RunContext.GetActForRound(6));
        }

        [Test]
        public void GetCurrentAct_Round7_ReturnsAct4()
        {
            Assert.AreEqual(4, RunContext.GetActForRound(7));
        }

        [Test]
        public void GetCurrentAct_Round8_ReturnsAct4()
        {
            Assert.AreEqual(4, RunContext.GetActForRound(8));
        }

        // --- GetCurrentTier Tests (Story 4.5 Task 2) ---

        [Test]
        public void GetCurrentTier_Act1_ReturnsPenny()
        {
            Assert.AreEqual(StockTier.Penny, RunContext.GetTierForAct(1));
        }

        [Test]
        public void GetCurrentTier_Act2_ReturnsLowValue()
        {
            Assert.AreEqual(StockTier.LowValue, RunContext.GetTierForAct(2));
        }

        [Test]
        public void GetCurrentTier_Act3_ReturnsMidValue()
        {
            Assert.AreEqual(StockTier.MidValue, RunContext.GetTierForAct(3));
        }

        [Test]
        public void GetCurrentTier_Act4_ReturnsBlueChip()
        {
            Assert.AreEqual(StockTier.BlueChip, RunContext.GetTierForAct(4));
        }

        // --- CurrentTier / CurrentActConfig Tests (Story 6.1 Task 2) ---

        [Test]
        public void CurrentTier_Act1_ReturnsPenny()
        {
            var ctx = new RunContext(1, 1, new Portfolio(1000f));
            Assert.AreEqual(StockTier.Penny, ctx.CurrentTier);
        }

        [Test]
        public void CurrentTier_Act3_ReturnsMidValue()
        {
            var ctx = new RunContext(3, 5, new Portfolio(1000f));
            Assert.AreEqual(StockTier.MidValue, ctx.CurrentTier);
        }

        [Test]
        public void CurrentTier_UpdatesAfterAdvanceRound()
        {
            var ctx = new RunContext(1, 2, new Portfolio(1000f));
            Assert.AreEqual(StockTier.Penny, ctx.CurrentTier);
            ctx.AdvanceRound(); // Round 3 = Act 2
            Assert.AreEqual(StockTier.LowValue, ctx.CurrentTier);
        }

        [Test]
        public void CurrentActConfig_ReturnsCorrectConfig()
        {
            var ctx = new RunContext(2, 3, new Portfolio(1000f));
            var config = ctx.CurrentActConfig;
            Assert.AreEqual(2, config.ActNumber);
            Assert.AreEqual(StockTier.LowValue, config.Tier);
            Assert.AreEqual("Low-Value Stocks", config.DisplayName);
        }

        [Test]
        public void CurrentActConfig_Act4_ReturnsBlueChip()
        {
            var ctx = new RunContext(4, 7, new Portfolio(1000f));
            var config = ctx.CurrentActConfig;
            Assert.AreEqual(4, config.ActNumber);
            Assert.AreEqual(StockTier.BlueChip, config.Tier);
            Assert.AreEqual(7, config.FirstRound);
            Assert.AreEqual(8, config.LastRound);
        }

        [Test]
        public void CurrentActConfig_ClampsWhenBeyondRound8()
        {
            // After round 8, AdvanceRound sets act to 5 which is out of bounds
            var ctx = new RunContext(1, 8, new Portfolio(1000f));
            ctx.AdvanceRound(); // Round 9, Act 5
            Assert.AreEqual(5, ctx.CurrentAct);
            // Should clamp to Act 4 config, not throw
            var config = ctx.CurrentActConfig;
            Assert.AreEqual(4, config.ActNumber);
            Assert.AreEqual(StockTier.BlueChip, config.Tier);
        }

        // --- GetTierForRound Tests (Story 6.1 Task 1) ---

        [Test]
        public void GetTierForRound_Round1_ReturnsPenny()
        {
            Assert.AreEqual(StockTier.Penny, RunContext.GetTierForRound(1));
        }

        [Test]
        public void GetTierForRound_Round4_ReturnsLowValue()
        {
            Assert.AreEqual(StockTier.LowValue, RunContext.GetTierForRound(4));
        }

        [Test]
        public void GetTierForRound_Round7_ReturnsBlueChip()
        {
            Assert.AreEqual(StockTier.BlueChip, RunContext.GetTierForRound(7));
        }

        // --- Full 8-Round Progression Test (Story 6.1) ---

        [Test]
        public void FullProgression_8Rounds_AllActsAndTiersCorrect()
        {
            var ctx = RunContext.StartNewRun();

            // Expected: Round → Act → Tier
            var expected = new (int round, int act, StockTier tier)[]
            {
                (1, 1, StockTier.Penny),
                (2, 1, StockTier.Penny),
                (3, 2, StockTier.LowValue),
                (4, 2, StockTier.LowValue),
                (5, 3, StockTier.MidValue),
                (6, 3, StockTier.MidValue),
                (7, 4, StockTier.BlueChip),
                (8, 4, StockTier.BlueChip),
            };

            // Round 1 — starting state
            Assert.AreEqual(expected[0].round, ctx.CurrentRound);
            Assert.AreEqual(expected[0].act, ctx.CurrentAct);
            Assert.AreEqual(expected[0].tier, ctx.CurrentTier);

            // Advance through rounds 2-8
            for (int i = 1; i < expected.Length; i++)
            {
                ctx.AdvanceRound();
                Assert.AreEqual(expected[i].round, ctx.CurrentRound,
                    $"Round mismatch at step {i}");
                Assert.AreEqual(expected[i].act, ctx.CurrentAct,
                    $"Act mismatch at round {expected[i].round}");
                Assert.AreEqual(expected[i].tier, ctx.CurrentTier,
                    $"Tier mismatch at round {expected[i].round}");
            }

            // After round 8, run should be complete
            Assert.IsFalse(ctx.IsRunComplete(), "Round 8 should NOT be complete yet");
            ctx.AdvanceRound(); // Round 9
            Assert.IsTrue(ctx.IsRunComplete(), "After round 8, run should be complete");
        }

        // --- ResetForNewRun Tests (Story 6.1 Code Review) ---

        [Test]
        public void ResetForNewRun_ResetsActAndRoundTo1()
        {
            var ctx = new RunContext(3, 5, new Portfolio(1000f));
            ctx.Portfolio.SubscribeToPriceUpdates();
            ctx.ResetForNewRun();
            Assert.AreEqual(1, ctx.CurrentAct);
            Assert.AreEqual(1, ctx.CurrentRound);
        }

        [Test]
        public void ResetForNewRun_ResetsPortfolioToStartingCapital()
        {
            var ctx = new RunContext(3, 5, new Portfolio(500f));
            ctx.Portfolio.SubscribeToPriceUpdates();
            ctx.ResetForNewRun();
            Assert.AreEqual(GameConfig.StartingCapital, ctx.Portfolio.Cash, 0.001f);
        }

        [Test]
        public void ResetForNewRun_ClearsActiveItems()
        {
            var ctx = new RunContext(1, 1, new Portfolio(1000f));
            ctx.Portfolio.SubscribeToPriceUpdates();
            ctx.ActiveItems.Add("TestItem");
            ctx.ResetForNewRun();
            Assert.AreEqual(0, ctx.ActiveItems.Count);
        }

        [Test]
        public void ResetForNewRun_PublishesRunStartedEvent()
        {
            var ctx = new RunContext(3, 5, new Portfolio(1000f));
            ctx.Portfolio.SubscribeToPriceUpdates();
            RunStartedEvent received = default;
            bool wasCalled = false;
            EventBus.Subscribe<RunStartedEvent>(e =>
            {
                received = e;
                wasCalled = true;
            });
            ctx.ResetForNewRun();
            Assert.IsTrue(wasCalled);
            Assert.AreEqual(GameConfig.StartingCapital, received.StartingCapital, 0.001f);
        }

        // --- GetActForRound Edge Cases (Story 6.1 Code Review) ---

        [Test]
        public void GetActForRound_Round0_ClampsToAct1()
        {
            Assert.AreEqual(1, RunContext.GetActForRound(0));
        }

        [Test]
        public void GetActForRound_NegativeRound_ClampsToAct1()
        {
            Assert.AreEqual(1, RunContext.GetActForRound(-5));
        }

        [Test]
        public void GetTierForAct_Act0_ClampsToAct1Tier()
        {
            Assert.AreEqual(StockTier.Penny, RunContext.GetTierForAct(0));
        }

        [Test]
        public void GetTierForAct_NegativeAct_ClampsToAct1Tier()
        {
            Assert.AreEqual(StockTier.Penny, RunContext.GetTierForAct(-1));
        }

        [Test]
        public void GetTierForAct_Act5_ClampsToAct4Tier()
        {
            Assert.AreEqual(StockTier.BlueChip, RunContext.GetTierForAct(5));
        }

        // --- IsRunComplete Tests (Story 4.5 Task 2) ---

        [Test]
        public void IsRunComplete_Round8_ReturnsTrue()
        {
            var ctx = RunContext.StartNewRun();
            ctx.CurrentRound = 8;
            ctx.AdvanceRound(); // Round 9 — run is now past round 8
            Assert.IsTrue(ctx.IsRunComplete());
        }

        [Test]
        public void IsRunComplete_Round7_ReturnsFalse()
        {
            var ctx = RunContext.StartNewRun();
            ctx.CurrentRound = 7;
            Assert.IsFalse(ctx.IsRunComplete());
        }

        [Test]
        public void IsRunComplete_Round1_ReturnsFalse()
        {
            var ctx = RunContext.StartNewRun();
            Assert.IsFalse(ctx.IsRunComplete());
        }

        [Test]
        public void IsRunComplete_ExactlyRound8_ReturnsFalse()
        {
            // Round 8 is the last playable round — not complete until AFTER round 8
            var ctx = RunContext.StartNewRun();
            ctx.CurrentRound = 8;
            Assert.IsFalse(ctx.IsRunComplete());
        }

        // --- Debug Jump Tests (Story 6.3 Code Review) ---

        [Test]
        public void DebugJump_ToRound5_SetsCorrectAct()
        {
            var ctx = RunContext.StartNewRun();
            ctx.CurrentAct = RunContext.GetActForRound(5);
            ctx.CurrentRound = 5;
            Assert.AreEqual(3, ctx.CurrentAct);
        }

        [Test]
        public void DebugJump_ToRound7_SetsCorrectAct()
        {
            var ctx = RunContext.StartNewRun();
            ctx.CurrentAct = RunContext.GetActForRound(7);
            ctx.CurrentRound = 7;
            Assert.AreEqual(4, ctx.CurrentAct);
        }

        [Test]
        public void DebugJump_StartingCapitalUpdated_MatchesDebugCash()
        {
            var ctx = RunContext.StartNewRun();
            Assert.AreEqual(GameConfig.StartingCapital, ctx.StartingCapital, 0.001f);

            // Simulate debug jump: replace portfolio and update StartingCapital
            float debugCash = GameConfig.DebugStartingCash[4]; // Round 5 = $4000
            ctx.Portfolio = new Portfolio(debugCash);
            ctx.StartingCapital = debugCash;

            Assert.AreEqual(debugCash, ctx.StartingCapital, 0.001f);
            Assert.AreEqual(debugCash, ctx.Portfolio.Cash, 0.001f);
        }

        [Test]
        public void DebugJump_RunSummaryProfit_CorrectAfterJump()
        {
            // Verifies the fix for H2: StartingCapital must update so RunSummary
            // calculates totalProfit = finalCash - StartingCapital correctly
            var ctx = RunContext.StartNewRun();

            // Jump to Round 7 with debug cash (FIX-14: now $400)
            float debugCash = GameConfig.DebugStartingCash[6]; // $400
            ctx.CurrentAct = RunContext.GetActForRound(7);
            ctx.CurrentRound = 7;
            ctx.Portfolio = new Portfolio(debugCash);
            ctx.StartingCapital = debugCash;

            // Simulate losing $200 during trading
            ctx.Portfolio.OpenPosition("TEST", 10, 20f); // -$200
            ctx.Portfolio.ClosePosition("TEST", 10, 0f); // cash now $200

            float totalProfit = ctx.Portfolio.Cash - ctx.StartingCapital;
            Assert.AreEqual(-200f, totalProfit, 1f,
                "Total profit after debug jump should reflect actual trading result, not inflated by debug cash");
        }

        // --- Run Statistics Tests (Story 6.5 Task 4) ---

        [Test]
        public void PeakCash_DefaultsToStartingCapital()
        {
            var ctx = RunContext.StartNewRun();
            Assert.AreEqual(GameConfig.StartingCapital, ctx.PeakCash, 0.01f);
        }

        [Test]
        public void PeakCash_UpdatesWhenCashIncreases()
        {
            var ctx = RunContext.StartNewRun();
            ctx.UpdateRunStats(500f); // Round profit, but cash hasn't changed
            Assert.AreEqual(GameConfig.StartingCapital, ctx.PeakCash, 0.01f,
                "PeakCash should track cash, not round profit");
        }

        [Test]
        public void PeakCash_TracksHighestCashValue()
        {
            var portfolio = new Portfolio(1000f);
            var ctx = new RunContext(1, 1, portfolio);
            // Simulate cash going up then down
            portfolio.OpenPosition("A", 10, 50f); // cash: 500
            portfolio.LiquidateAllPositions(id => 80f); // cash: 1300
            ctx.UpdateRunStats(300f);
            Assert.AreEqual(1300f, ctx.PeakCash, 0.01f);

            // Cash drops, peak should stay at 1300
            portfolio.OpenPosition("A", 10, 50f); // cash: 800
            portfolio.LiquidateAllPositions(id => 40f); // cash: 1200
            ctx.UpdateRunStats(-100f);
            Assert.AreEqual(1300f, ctx.PeakCash, 0.01f, "PeakCash should not decrease");
        }

        [Test]
        public void BestRoundProfit_DefaultsToZero()
        {
            var ctx = RunContext.StartNewRun();
            Assert.AreEqual(0f, ctx.BestRoundProfit, 0.01f);
        }

        [Test]
        public void BestRoundProfit_TracksHighestRoundProfit()
        {
            var ctx = RunContext.StartNewRun();
            ctx.UpdateRunStats(200f);
            ctx.UpdateRunStats(500f);
            ctx.UpdateRunStats(300f);
            Assert.AreEqual(500f, ctx.BestRoundProfit, 0.01f);
        }

        [Test]
        public void BestRoundProfit_IgnoresNegativeProfits()
        {
            var ctx = RunContext.StartNewRun();
            ctx.UpdateRunStats(-100f);
            Assert.AreEqual(0f, ctx.BestRoundProfit, 0.01f);
        }

        [Test]
        public void TotalRunProfit_AccumulatesRoundProfits()
        {
            var ctx = RunContext.StartNewRun();
            ctx.UpdateRunStats(200f);
            ctx.UpdateRunStats(300f);
            Assert.AreEqual(500f, ctx.TotalRunProfit, 0.01f);
        }

        [Test]
        public void TotalRunProfit_HandlesNegativeRounds()
        {
            var ctx = RunContext.StartNewRun();
            ctx.UpdateRunStats(500f);
            ctx.UpdateRunStats(-200f);
            Assert.AreEqual(300f, ctx.TotalRunProfit, 0.01f);
        }

        [Test]
        public void ItemsCollected_DefaultsToZero()
        {
            var ctx = RunContext.StartNewRun();
            Assert.AreEqual(0, ctx.ItemsCollected);
        }

        [Test]
        public void ResetForNewRun_ClearsRunStats()
        {
            var ctx = new RunContext(4, 8, new Portfolio(5000f));
            ctx.Portfolio.SubscribeToPriceUpdates();
            ctx.PeakCash = 8000f;
            ctx.BestRoundProfit = 2000f;
            ctx.TotalRunProfit = 6000f;
            ctx.ActiveItems.Add("item1");
            ctx.ActiveItems.Add("item2");
            Assert.AreEqual(2, ctx.ItemsCollected);
            ctx.ResetForNewRun();
            Assert.AreEqual(GameConfig.StartingCapital, ctx.PeakCash, 0.01f);
            Assert.AreEqual(0f, ctx.BestRoundProfit, 0.01f);
            Assert.AreEqual(0f, ctx.TotalRunProfit, 0.01f);
            Assert.AreEqual(0, ctx.ItemsCollected);
        }

        // --- RunCompleted Tests (Story 6.5 Task 1) ---

        [Test]
        public void RunCompleted_DefaultsFalse()
        {
            var ctx = RunContext.StartNewRun();
            Assert.IsFalse(ctx.RunCompleted);
        }

        [Test]
        public void RunCompleted_CanBeSetTrue()
        {
            var ctx = RunContext.StartNewRun();
            ctx.RunCompleted = true;
            Assert.IsTrue(ctx.RunCompleted);
        }

        [Test]
        public void ResetForNewRun_ClearsRunCompleted()
        {
            var ctx = new RunContext(4, 8, new Portfolio(1000f));
            ctx.Portfolio.SubscribeToPriceUpdates();
            ctx.RunCompleted = true;
            ctx.ResetForNewRun();
            Assert.IsFalse(ctx.RunCompleted);
        }

        [Test]
        public void DebugJump_AllRounds_ActAndCashCorrect()
        {
            var ctx = RunContext.StartNewRun();

            for (int round = 1; round <= GameConfig.TotalRounds; round++)
            {
                int expectedAct = RunContext.GetActForRound(round);
                float expectedCash = GameConfig.DebugStartingCash[round - 1];

                ctx.CurrentAct = expectedAct;
                ctx.CurrentRound = round;
                ctx.Portfolio = new Portfolio(expectedCash);
                ctx.StartingCapital = expectedCash;

                Assert.AreEqual(expectedAct, ctx.CurrentAct, $"Act mismatch for round {round}");
                Assert.AreEqual(round, ctx.CurrentRound, $"Round mismatch for round {round}");
                Assert.AreEqual(expectedCash, ctx.Portfolio.Cash, 0.01f, $"Cash mismatch for round {round}");
                Assert.AreEqual(expectedCash, ctx.StartingCapital, 0.01f, $"StartingCapital mismatch for round {round}");
            }
        }
    }
}
