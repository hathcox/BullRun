using NUnit.Framework;

namespace BullRun.Tests.PriceEngine
{
    [TestFixture]
    public class StockTierDataTests
    {
        [Test]
        public void StockTier_Enum_HasFourValues()
        {
            var values = System.Enum.GetValues(typeof(StockTier));
            Assert.AreEqual(4, values.Length);
        }

        [Test]
        public void GetTierConfig_Penny_HasCorrectPriceRange()
        {
            var config = StockTierData.GetTierConfig(StockTier.Penny);
            Assert.AreEqual(5.00f, config.MinPrice, 0.01f);
            Assert.AreEqual(8f, config.MaxPrice, 0.01f);
        }

        [Test]
        public void GetTierConfig_LowValue_HasCorrectPriceRange()
        {
            var config = StockTierData.GetTierConfig(StockTier.LowValue);
            Assert.AreEqual(5f, config.MinPrice, 0.01f);
            Assert.AreEqual(50f, config.MaxPrice, 0.01f);
        }

        [Test]
        public void GetTierConfig_MidValue_HasCorrectPriceRange()
        {
            var config = StockTierData.GetTierConfig(StockTier.MidValue);
            Assert.AreEqual(50f, config.MinPrice, 0.01f);
            Assert.AreEqual(500f, config.MaxPrice, 0.01f);
        }

        [Test]
        public void GetTierConfig_BlueChip_HasCorrectPriceRange()
        {
            var config = StockTierData.GetTierConfig(StockTier.BlueChip);
            Assert.AreEqual(150f, config.MinPrice, 0.01f);
            Assert.AreEqual(5000f, config.MaxPrice, 0.01f);
        }

        [Test]
        public void AllTiers_HaveValidPriceRanges_MaxGreaterThanMin()
        {
            foreach (StockTier tier in System.Enum.GetValues(typeof(StockTier)))
            {
                var config = StockTierData.GetTierConfig(tier);
                Assert.Greater(config.MaxPrice, config.MinPrice,
                    $"Tier {tier}: MaxPrice should be greater than MinPrice");
            }
        }

        [Test]
        public void AllTiers_HavePositiveBaseVolatility()
        {
            foreach (StockTier tier in System.Enum.GetValues(typeof(StockTier)))
            {
                var config = StockTierData.GetTierConfig(tier);
                Assert.Greater(config.BaseVolatility, 0f,
                    $"Tier {tier}: BaseVolatility should be positive");
            }
        }

        [Test]
        public void PennyTier_HasHigherVolatility_ThanBlueChip()
        {
            var penny = StockTierData.GetTierConfig(StockTier.Penny);
            var blueChip = StockTierData.GetTierConfig(StockTier.BlueChip);
            Assert.Greater(penny.BaseVolatility, blueChip.BaseVolatility);
        }

        [Test]
        public void AllTiers_HaveValidStockCountRanges()
        {
            foreach (StockTier tier in System.Enum.GetValues(typeof(StockTier)))
            {
                var config = StockTierData.GetTierConfig(tier);
                Assert.Greater(config.MinStocksPerRound, 0,
                    $"Tier {tier}: MinStocksPerRound should be positive");
                Assert.GreaterOrEqual(config.MaxStocksPerRound, config.MinStocksPerRound,
                    $"Tier {tier}: MaxStocksPerRound should be >= MinStocksPerRound");
            }
        }

        [Test]
        public void AllTiers_HaveValidTrendStrengthRanges()
        {
            foreach (StockTier tier in System.Enum.GetValues(typeof(StockTier)))
            {
                var config = StockTierData.GetTierConfig(tier);
                Assert.Greater(config.MaxTrendStrength, 0f,
                    $"Tier {tier}: MaxTrendStrength should be positive");
                Assert.GreaterOrEqual(config.MaxTrendStrength, config.MinTrendStrength,
                    $"Tier {tier}: MaxTrendStrength should be >= MinTrendStrength");
            }
        }

        [Test]
        public void VolatilityOrder_PennyHighestBlueChipLowest()
        {
            var penny = StockTierData.GetTierConfig(StockTier.Penny);
            var lowValue = StockTierData.GetTierConfig(StockTier.LowValue);
            var midValue = StockTierData.GetTierConfig(StockTier.MidValue);
            var blueChip = StockTierData.GetTierConfig(StockTier.BlueChip);

            Assert.Greater(penny.BaseVolatility, lowValue.BaseVolatility, "Penny > LowValue volatility");
            Assert.Greater(lowValue.BaseVolatility, midValue.BaseVolatility, "LowValue > MidValue volatility");
            Assert.GreaterOrEqual(midValue.BaseVolatility, blueChip.BaseVolatility, "MidValue >= BlueChip volatility");
        }

        [Test]
        public void AllTiers_HavePositiveNoiseAmplitude()
        {
            foreach (StockTier tier in System.Enum.GetValues(typeof(StockTier)))
            {
                var config = StockTierData.GetTierConfig(tier);
                Assert.Greater(config.NoiseAmplitude, 0f,
                    $"Tier {tier}: NoiseAmplitude should be positive");
            }
        }

        [Test]
        public void AllTiers_HavePositiveNoiseFrequency()
        {
            foreach (StockTier tier in System.Enum.GetValues(typeof(StockTier)))
            {
                var config = StockTierData.GetTierConfig(tier);
                Assert.Greater(config.NoiseFrequency, 0f,
                    $"Tier {tier}: NoiseFrequency should be positive");
            }
        }

        [Test]
        public void NoiseAmplitudeOrder_PennyHighestBlueChipLowest()
        {
            var penny = StockTierData.GetTierConfig(StockTier.Penny);
            var blueChip = StockTierData.GetTierConfig(StockTier.BlueChip);
            Assert.Greater(penny.NoiseAmplitude, blueChip.NoiseAmplitude,
                "Penny should have higher noise amplitude than Blue Chip");
        }

        // --- Mean Reversion Tests (Story 1.4) ---

        [Test]
        public void AllTiers_HaveNonNegativeMeanReversionSpeed()
        {
            foreach (StockTier tier in System.Enum.GetValues(typeof(StockTier)))
            {
                var config = StockTierData.GetTierConfig(tier);
                Assert.GreaterOrEqual(config.MeanReversionSpeed, 0f,
                    $"Tier {tier}: MeanReversionSpeed should be non-negative");
            }
        }

        [Test]
        public void AllTiers_MeanReversionSpeedBelowOne()
        {
            foreach (StockTier tier in System.Enum.GetValues(typeof(StockTier)))
            {
                var config = StockTierData.GetTierConfig(tier);
                Assert.LessOrEqual(config.MeanReversionSpeed, 1f,
                    $"Tier {tier}: MeanReversionSpeed should be <= 1.0");
            }
        }

        [Test]
        public void MeanReversionOrder_PennySlowestBlueChipFastest()
        {
            var penny = StockTierData.GetTierConfig(StockTier.Penny);
            var lowValue = StockTierData.GetTierConfig(StockTier.LowValue);
            var midValue = StockTierData.GetTierConfig(StockTier.MidValue);
            var blueChip = StockTierData.GetTierConfig(StockTier.BlueChip);

            Assert.Less(penny.MeanReversionSpeed, lowValue.MeanReversionSpeed, "Penny < LowValue reversion");
            Assert.Less(lowValue.MeanReversionSpeed, midValue.MeanReversionSpeed, "LowValue < MidValue reversion");
            Assert.Less(midValue.MeanReversionSpeed, blueChip.MeanReversionSpeed, "MidValue < BlueChip reversion");
        }

        [Test]
        public void GetTierConfig_Penny_HasExpectedReversionSpeed()
        {
            var config = StockTierData.GetTierConfig(StockTier.Penny);
            Assert.AreEqual(0.20f, config.MeanReversionSpeed, 0.001f);
        }

        [Test]
        public void GetTierConfig_Penny_HasExpectedVolatility()
        {
            var config = StockTierData.GetTierConfig(StockTier.Penny);
            Assert.AreEqual(0.25f, config.BaseVolatility, 0.001f);
        }

        [Test]
        public void GetTierConfig_Penny_HasExpectedTrendStrength()
        {
            var config = StockTierData.GetTierConfig(StockTier.Penny);
            Assert.AreEqual(0.008f, config.MinTrendStrength, 0.001f);
            Assert.AreEqual(0.025f, config.MaxTrendStrength, 0.001f);
        }

        [Test]
        public void GetTierConfig_Penny_HasExpectedNoiseAmplitude()
        {
            var config = StockTierData.GetTierConfig(StockTier.Penny);
            Assert.AreEqual(0.15f, config.NoiseAmplitude, 0.001f);
        }

        [Test]
        public void GetTierConfig_BlueChip_HasExpectedReversionSpeed()
        {
            var config = StockTierData.GetTierConfig(StockTier.BlueChip);
            Assert.AreEqual(0.50f, config.MeanReversionSpeed, 0.001f);
        }

        // --- Event Frequency Modifier Tests (Story 1.5) ---

        [Test]
        public void AllTiers_HavePositiveEventFrequencyModifier()
        {
            foreach (StockTier tier in System.Enum.GetValues(typeof(StockTier)))
            {
                var config = StockTierData.GetTierConfig(tier);
                Assert.Greater(config.EventFrequencyModifier, 0f,
                    $"Tier {tier}: EventFrequencyModifier should be positive");
            }
        }

        [Test]
        public void EventFrequencyOrder_PennyHighestBlueChipLowest()
        {
            var penny = StockTierData.GetTierConfig(StockTier.Penny);
            var blueChip = StockTierData.GetTierConfig(StockTier.BlueChip);
            Assert.Greater(penny.EventFrequencyModifier, blueChip.EventFrequencyModifier,
                "Penny should have higher event frequency than Blue Chip");
        }

        // --- Multiple Stocks Per Round Tests ---

        [Test]
        public void AllTiers_HaveMultipleStocksPerRound()
        {
            foreach (StockTier tier in System.Enum.GetValues(typeof(StockTier)))
            {
                var config = StockTierData.GetTierConfig(tier);
                Assert.GreaterOrEqual(config.MinStocksPerRound, 2,
                    $"Tier {tier}: MinStocksPerRound should be at least 2 for multi-stock gameplay");
                Assert.GreaterOrEqual(config.MaxStocksPerRound, config.MinStocksPerRound,
                    $"Tier {tier}: MaxStocksPerRound should be >= MinStocksPerRound");
            }
        }
    }
}
