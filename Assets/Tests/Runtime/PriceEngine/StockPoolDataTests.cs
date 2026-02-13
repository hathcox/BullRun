using NUnit.Framework;
using System.Collections.Generic;

namespace BullRun.Tests.PriceEngine
{
    [TestFixture]
    public class StockPoolDataTests
    {
        [Test]
        public void GetPool_AllTiers_ReturnNonEmptyPools()
        {
            foreach (StockTier tier in System.Enum.GetValues(typeof(StockTier)))
            {
                var pool = StockPoolData.GetPool(tier);
                Assert.IsNotNull(pool, $"Tier {tier}: pool should not be null");
                Assert.Greater(pool.Length, 0, $"Tier {tier}: pool should not be empty");
            }
        }

        [Test]
        public void AllStocks_HaveTickerSymbols()
        {
            foreach (StockTier tier in System.Enum.GetValues(typeof(StockTier)))
            {
                var pool = StockPoolData.GetPool(tier);
                foreach (var stock in pool)
                {
                    Assert.IsFalse(string.IsNullOrEmpty(stock.TickerSymbol),
                        $"Tier {tier}: all stocks must have a ticker symbol");
                }
            }
        }

        [Test]
        public void AllStocks_HaveDisplayNames()
        {
            foreach (StockTier tier in System.Enum.GetValues(typeof(StockTier)))
            {
                var pool = StockPoolData.GetPool(tier);
                foreach (var stock in pool)
                {
                    Assert.IsFalse(string.IsNullOrEmpty(stock.DisplayName),
                        $"{stock.TickerSymbol}: must have a display name");
                }
            }
        }

        [Test]
        public void AllStocks_TierMatchesPoolTier()
        {
            foreach (StockTier tier in System.Enum.GetValues(typeof(StockTier)))
            {
                var pool = StockPoolData.GetPool(tier);
                foreach (var stock in pool)
                {
                    Assert.AreEqual(tier, stock.Tier,
                        $"{stock.TickerSymbol}: tier should match pool tier");
                }
            }
        }

        [Test]
        public void AllTickerSymbols_AreUnique_AcrossAllPools()
        {
            var allTickers = new HashSet<string>();
            foreach (StockTier tier in System.Enum.GetValues(typeof(StockTier)))
            {
                var pool = StockPoolData.GetPool(tier);
                foreach (var stock in pool)
                {
                    Assert.IsTrue(allTickers.Add(stock.TickerSymbol),
                        $"Duplicate ticker symbol: {stock.TickerSymbol}");
                }
            }
        }

        [Test]
        public void TickerSymbols_AreBetween3And5Characters()
        {
            foreach (StockTier tier in System.Enum.GetValues(typeof(StockTier)))
            {
                var pool = StockPoolData.GetPool(tier);
                foreach (var stock in pool)
                {
                    Assert.GreaterOrEqual(stock.TickerSymbol.Length, 3,
                        $"{stock.TickerSymbol}: ticker should be at least 3 chars");
                    Assert.LessOrEqual(stock.TickerSymbol.Length, 5,
                        $"{stock.TickerSymbol}: ticker should be at most 5 chars");
                }
            }
        }

        [Test]
        public void EachPool_HasEnoughStocks_ForMaxStocksPerRound()
        {
            foreach (StockTier tier in System.Enum.GetValues(typeof(StockTier)))
            {
                var pool = StockPoolData.GetPool(tier);
                var config = StockTierData.GetTierConfig(tier);
                Assert.GreaterOrEqual(pool.Length, config.MaxStocksPerRound,
                    $"Tier {tier}: pool must have at least MaxStocksPerRound ({config.MaxStocksPerRound}) stocks");
            }
        }

        [Test]
        public void AllPools_HaveAtLeast6Stocks_ForRoundVariety()
        {
            foreach (StockTier tier in System.Enum.GetValues(typeof(StockTier)))
            {
                var pool = StockPoolData.GetPool(tier);
                Assert.GreaterOrEqual(pool.Length, 6,
                    $"Tier {tier}: pool should have at least 6 stocks for round variety");
            }
        }

        [Test]
        public void MidValueStocks_AllHaveSectorTags()
        {
            foreach (var stock in StockPoolData.MidValueStocks)
            {
                Assert.AreNotEqual(StockSector.None, stock.Sector,
                    $"{stock.TickerSymbol}: Mid-Value stocks should have sector tags");
            }
        }

        [Test]
        public void BlueChipStocks_AllHaveSectorTags()
        {
            foreach (var stock in StockPoolData.BlueChipStocks)
            {
                Assert.AreNotEqual(StockSector.None, stock.Sector,
                    $"{stock.TickerSymbol}: Blue Chip stocks should have sector tags");
            }
        }
    }
}
