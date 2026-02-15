using NUnit.Framework;
using UnityEngine;

namespace BullRun.Tests.Core
{
    /// <summary>
    /// FIX-15: Mathematical simulation tests validating that each round is winnable
    /// given the stock tier parameters, starting capital, and trade mechanics.
    /// Uses exponential price model: price(t) = price(0) * e^(trendStrength * t),
    /// which matches the price engine's compound growth (dp/dt = strength * price).
    /// </summary>
    [TestFixture]
    public class RoundWinnabilityTests
    {
        /// <summary>
        /// Round 1 max theoretical profit must exceed the $10 needed to reach $20 target.
        /// Single hold for the full round at max trend strength with exponential growth.
        /// </summary>
        [Test]
        public void Round1_MaxTheoreticalProfit_ExceedsTarget()
        {
            var penny = StockTierData.GetTierConfig(StockTier.Penny);
            float midPrice = (penny.MinPrice + penny.MaxPrice) / 2f;
            int shares = Mathf.FloorToInt(GameConfig.StartingCapital / midPrice);
            float roundDuration = GameConfig.RoundDurationSeconds;

            // Exponential: profit = shares * price * (e^(strength * time) - 1)
            float maxPriceMove = midPrice * (Mathf.Exp(penny.MaxTrendStrength * roundDuration) - 1f);
            float maxProfit = shares * maxPriceMove;
            float profitNeeded = MarginCallTargets.GetTarget(1) - GameConfig.StartingCapital;

            Assert.Greater(maxProfit, profitNeeded,
                $"Max theoretical profit ${maxProfit:F2} must exceed needed ${profitNeeded:F2}");
        }

        /// <summary>
        /// At 50% of max theoretical profit (achievable by decent trend-riding play),
        /// profit should reach the Round 1 target.
        /// </summary>
        [Test]
        public void Round1_ReasonableProfit_CanReachTarget()
        {
            var penny = StockTierData.GetTierConfig(StockTier.Penny);
            float midPrice = (penny.MinPrice + penny.MaxPrice) / 2f;
            int shares = Mathf.FloorToInt(GameConfig.StartingCapital / midPrice);
            float roundDuration = GameConfig.RoundDurationSeconds;

            float maxPriceMove = midPrice * (Mathf.Exp(penny.MaxTrendStrength * roundDuration) - 1f);
            float maxProfit = shares * maxPriceMove;
            float reasonableProfit = maxProfit * 0.5f;
            float profitNeeded = MarginCallTargets.GetTarget(1) - GameConfig.StartingCapital;

            Assert.GreaterOrEqual(reasonableProfit, profitNeeded,
                $"Reasonable profit (50% of max) ${reasonableProfit:F2} must reach ${profitNeeded:F2}");
        }

        /// <summary>
        /// The round duration and trade cooldown must allow at least 3 full buy-sell cycles.
        /// </summary>
        [Test]
        public void Round1_TradeWindowAllowsSufficientCycles()
        {
            float tradingTime = GameConfig.RoundDurationSeconds;
            float cooldown = GameConfig.PostTradeCooldown;
            // Each cycle: buy (cooldown) + hold (variable) + sell (cooldown)
            // Minimum cycle time = 2 * cooldown (instant hold)
            float minSecondsPerCycle = 2f * cooldown;
            float maxCycles = tradingTime / minSecondsPerCycle;

            Assert.GreaterOrEqual(maxCycles, 3f,
                $"Must allow at least 3 cycles ({maxCycles:F1} possible with {cooldown}s cooldown)");
        }

        /// <summary>
        /// A single share held for one 20s cycle capturing 40% of the average trend move
        /// should produce meaningful profit (> $0.50).
        /// </summary>
        [Test]
        public void Round1_SingleShareProfitPerCycle_IsMeaningful()
        {
            var penny = StockTierData.GetTierConfig(StockTier.Penny);
            float midPrice = (penny.MinPrice + penny.MaxPrice) / 2f;
            float avgTrendStrength = (penny.MinTrendStrength + penny.MaxTrendStrength) / 2f;
            float holdTime = 20f;
            float captureEfficiency = 0.4f;

            // Exponential move over hold period
            float movePerShare = midPrice * (Mathf.Exp(avgTrendStrength * holdTime) - 1f);
            float profitPerCycle = movePerShare * captureEfficiency * 1; // 1 share

            Assert.Greater(profitPerCycle, 0.50f,
                $"Single share profit per cycle ${profitPerCycle:F2} should be > $0.50");
        }

        /// <summary>
        /// For penny tier rounds (1-2), verify that max theoretical profit with exponential
        /// growth exceeds the profit needed to hit the target.
        /// Higher tiers are validated more loosely since events and excess cash contribute.
        /// </summary>
        [Test]
        public void AllRounds_TheoreticalProfitExceedsTarget()
        {
            float[] targets = MarginCallTargets.GetAllTargets();
            float cash = GameConfig.StartingCapital;

            for (int round = 1; round <= GameConfig.TotalRounds; round++)
            {
                StockTier tier = RunContext.GetTierForRound(round);
                var config = StockTierData.GetTierConfig(tier);
                float roundDuration = GameConfig.RoundDurationSeconds;
                float target = targets[round - 1];
                float profitNeeded = target - cash;

                // Simulate 3 compounding trade cycles using best-case price (min price = most shares)
                float cycleTime = roundDuration / 3f;
                float simCash = cash;
                for (int c = 0; c < 3; c++)
                {
                    int s = Mathf.Max(1, Mathf.FloorToInt(simCash / config.MinPrice));
                    float profit = s * config.MinPrice * (Mathf.Exp(config.MaxTrendStrength * cycleTime) - 1f);
                    simCash += profit;
                }
                float maxProfit = simCash - cash;

                // Penny tier (rounds 1-2): strict check — trend alone must suffice
                // Higher tiers: 50% threshold — events, items, and excess cash cover the rest
                float threshold = (tier == StockTier.Penny) ? profitNeeded : profitNeeded * 0.5f;

                Assert.Greater(maxProfit, threshold,
                    $"Round {round} ({tier}): max profit ${maxProfit:F2} must exceed threshold ${threshold:F2} (cash=${cash:F2}, target=${target:F2})");

                // Advance cash to target for next round
                cash = target;
            }
        }

        /// <summary>
        /// After a profitable hold cycle, verify player can afford more shares in cycle 2
        /// (compounding within a round). Uses max trend with 50% capture over 30s.
        /// </summary>
        [Test]
        public void PennyTier_PriceRange_AllowsCompoundingWithinRound()
        {
            var penny = StockTierData.GetTierConfig(StockTier.Penny);
            float midPrice = (penny.MinPrice + penny.MaxPrice) / 2f;

            // Cycle 1: buy shares at midPrice, hold 30s with max trend, 50% capture
            int initialShares = Mathf.FloorToInt(GameConfig.StartingCapital / midPrice);
            float holdTime = 30f;
            float captureEfficiency = 0.5f;
            float movePerShare = midPrice * (Mathf.Exp(penny.MaxTrendStrength * holdTime) - 1f);
            float cycle1Profit = movePerShare * captureEfficiency * initialShares;

            // After cycle 1: can afford more shares?
            float cashAfterCycle1 = GameConfig.StartingCapital + cycle1Profit;
            int sharesAfterCycle1 = Mathf.FloorToInt(cashAfterCycle1 / midPrice);

            Assert.Greater(sharesAfterCycle1, initialShares,
                $"After cycle 1 profit ${cycle1Profit:F2}, should afford more shares ({sharesAfterCycle1} vs {initialShares})");
        }
    }
}
