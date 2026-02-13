using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace BullRun.Tests.PriceEngine
{
    [TestFixture]
    public class PriceGeneratorTests
    {
        private PriceGenerator _generator;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            _generator = new PriceGenerator();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
        }

        // --- Trend Behavior Tests (updated for noise presence) ---

        [Test]
        public void UpdatePrice_BullTrend_OverManyFrames_NetPositive()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.MidValue, 100f, TrendDirection.Bull, 0.05f);
            float initialPrice = stock.CurrentPrice;

            // Simulate 60 frames (~1 second at 60fps) — trend should dominate noise
            for (int i = 0; i < 60; i++)
                _generator.UpdatePrice(stock, 0.016f);

            Assert.Greater(stock.CurrentPrice, initialPrice,
                "Bull trend should produce net positive movement over 1 second");
        }

        [Test]
        public void UpdatePrice_BearTrend_OverManyFrames_NetNegative()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.MidValue, 100f, TrendDirection.Bear, 0.05f);
            float initialPrice = stock.CurrentPrice;

            for (int i = 0; i < 60; i++)
                _generator.UpdatePrice(stock, 0.016f);

            Assert.Less(stock.CurrentPrice, initialPrice,
                "Bear trend should produce net negative movement over 1 second");
        }

        [Test]
        public void UpdatePrice_PublishesPriceUpdatedEvent()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.MidValue, 100f, TrendDirection.Bull, 0.05f);

            PriceUpdatedEvent receivedEvent = default;
            bool eventFired = false;
            EventBus.Subscribe<PriceUpdatedEvent>(e =>
            {
                receivedEvent = e;
                eventFired = true;
            });

            _generator.UpdatePrice(stock, 0.016f);

            Assert.IsTrue(eventFired, "PriceUpdatedEvent should be published");
            Assert.AreEqual(0, receivedEvent.StockId);
            Assert.AreEqual(100f, receivedEvent.PreviousPrice, 0.01f);
            Assert.AreEqual(0.016f, receivedEvent.DeltaTime, 0.001f);
        }

        [Test]
        public void UpdatePrice_TrendDominatesNoise_OverTime()
        {
            // Bull trend at 5%/s on $100 = $5/s. Over 1s that's $5 of trend.
            // Noise should not reverse a $5 trend movement.
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.MidValue, 100f, TrendDirection.Bull, 0.05f);

            for (int i = 0; i < 60; i++)
                _generator.UpdatePrice(stock, 0.016f);

            // Price should be roughly 100 + 5 = 105, noise may shift it but trend dominates
            Assert.Greater(stock.CurrentPrice, 102f, "Trend should be clearly visible through noise");
        }

        // --- Noise-Specific Tests ---

        [Test]
        public void UpdatePrice_NeutralTrend_NoiseStillCausesMovement()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.Penny, 2.50f, TrendDirection.Neutral, 0.0f);
            float initialPrice = stock.CurrentPrice;

            // With noise, neutral trend should still see some price movement over many frames
            bool anyChange = false;
            for (int i = 0; i < 120; i++)
            {
                _generator.UpdatePrice(stock, 0.016f);
                if (System.Math.Abs(stock.CurrentPrice - initialPrice) > 0.001f)
                {
                    anyChange = true;
                    break;
                }
            }

            Assert.IsTrue(anyChange, "Noise should cause price movement even with neutral trend");
        }

        [Test]
        public void UpdatePrice_PennyStock_HasMoreVariation_ThanBlueChip()
        {
            var penny = new StockInstance();
            penny.Initialize(0, "PNNY", StockTier.Penny, 2.50f, TrendDirection.Neutral, 0f);

            var blueChip = new StockInstance();
            blueChip.Initialize(1, "BLUE", StockTier.BlueChip, 2500f, TrendDirection.Neutral, 0f);

            float pennyVariation = 0f;
            float blueChipVariation = 0f;

            for (int i = 0; i < 600; i++)
            {
                float pennyBefore = penny.CurrentPrice;
                float blueBefore = blueChip.CurrentPrice;

                _generator.UpdatePrice(penny, 0.016f);
                _generator.UpdatePrice(blueChip, 0.016f);

                // Track percentage variation
                pennyVariation += System.Math.Abs((penny.CurrentPrice - pennyBefore) / pennyBefore);
                blueChipVariation += System.Math.Abs((blueChip.CurrentPrice - blueBefore) / blueBefore);
            }

            Assert.Greater(pennyVariation, blueChipVariation,
                "Penny stocks should have higher percentage variation than Blue Chip");
        }

        [Test]
        public void UpdatePrice_NeverGoesNegative()
        {
            // Low starting price penny stock with bear trend — should clamp at MinPrice
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.Penny, 0.15f, TrendDirection.Bear, 0.20f);

            for (int i = 0; i < 600; i++)
            {
                _generator.UpdatePrice(stock, 0.016f);
                Assert.GreaterOrEqual(stock.CurrentPrice, stock.TierConfig.MinPrice,
                    $"Price went below tier minimum at frame {i}");
            }
        }

        [Test]
        public void UpdatePrice_ClampsToTierMinPrice()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.Penny, 0.12f, TrendDirection.Bear, 0.20f);

            // Heavy bear trend on near-minimum price
            for (int i = 0; i < 300; i++)
                _generator.UpdatePrice(stock, 0.016f);

            Assert.GreaterOrEqual(stock.CurrentPrice, 0.10f,
                "Price should be clamped to Penny tier MinPrice ($0.10)");
        }

        // --- InitializeRound Tests ---

        [Test]
        public void InitializeRound_CreatesStockInstances()
        {
            _generator.InitializeRound(1, 1);
            var stocks = _generator.ActiveStocks;

            Assert.IsNotNull(stocks);
            Assert.Greater(stocks.Count, 0);
        }

        [Test]
        public void InitializeRound_StocksHaveUniqueIds()
        {
            _generator.InitializeRound(1, 1);
            var stocks = _generator.ActiveStocks;
            var ids = new HashSet<int>();

            foreach (var stock in stocks)
            {
                Assert.IsTrue(ids.Add(stock.StockId), $"Duplicate stock ID: {stock.StockId}");
            }
        }

        [Test]
        public void InitializeRound_StocksHaveTrendDirections()
        {
            _generator.InitializeRound(1, 1);
            var stocks = _generator.ActiveStocks;

            foreach (var stock in stocks)
            {
                Assert.IsTrue(
                    stock.TrendDirection == TrendDirection.Bull ||
                    stock.TrendDirection == TrendDirection.Bear ||
                    stock.TrendDirection == TrendDirection.Neutral,
                    $"Stock {stock.StockId} has invalid trend direction");
            }
        }

        [Test]
        public void InitializeRound_StocksHavePricesWithinTierRange()
        {
            for (int i = 0; i < 10; i++)
            {
                _generator.InitializeRound(1, 1);
                var stocks = _generator.ActiveStocks;

                foreach (var stock in stocks)
                {
                    Assert.GreaterOrEqual(stock.CurrentPrice, stock.TierConfig.MinPrice,
                        $"Stock {stock.TickerSymbol} price below tier min");
                    Assert.LessOrEqual(stock.CurrentPrice, stock.TierConfig.MaxPrice,
                        $"Stock {stock.TickerSymbol} price above tier max");
                }
            }
        }

        [Test]
        public void InitializeRound_StocksHaveNoiseParametersFromTier()
        {
            _generator.InitializeRound(1, 1);
            var stocks = _generator.ActiveStocks;

            foreach (var stock in stocks)
            {
                Assert.Greater(stock.NoiseAmplitude, 0f,
                    $"Stock {stock.TickerSymbol} should have positive noise amplitude");
                Assert.Greater(stock.NoiseFrequency, 0f,
                    $"Stock {stock.TickerSymbol} should have positive noise frequency");
                Assert.AreEqual(0f, stock.SegmentTimeRemaining,
                    $"Stock {stock.TickerSymbol} segment time remaining should start at 0");
            }
        }

        [Test]
        public void InitializeRound_MultipleStocksCanHaveIndependentTrends()
        {
            _generator.InitializeRound(1, 1);
            var stocks = _generator.ActiveStocks;

            Assert.Greater(stocks.Count, 1, "Should have multiple stocks for independent trend testing");
            foreach (var stock in stocks)
            {
                Assert.IsNotNull(stock.TickerSymbol);
                Assert.Greater(stock.CurrentPrice, 0f);
            }
        }

        // --- Event Integration Tests (Story 1.3) ---

        [Test]
        public void UpdatePrice_WithActiveEvent_AppliesEventEffect()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.MidValue, 100f, TrendDirection.Neutral, 0f);

            var eventEffects = new EventEffects();
            _generator.SetEventEffects(eventEffects);

            // Start a positive event at peak force
            var evt = new MarketEvent(MarketEventType.EarningsBeat, 0, 0.25f, 4f);
            evt.ElapsedTime = 2f; // Peak force = 1.0
            eventEffects.StartEvent(evt);

            float priceBefore = stock.CurrentPrice;
            _generator.UpdatePrice(stock, 0.016f);

            Assert.Greater(stock.CurrentPrice, priceBefore,
                "Active event should cause price increase for positive event");
        }

        [Test]
        public void UpdatePrice_WithoutEventEffects_StillWorks()
        {
            // No SetEventEffects called — should work as before (trend + noise only)
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.MidValue, 100f, TrendDirection.Bull, 0.05f);

            // Should not throw
            _generator.UpdatePrice(stock, 0.016f);

            Assert.Greater(stock.CurrentPrice, 99f, "Price should update normally without events");
        }

        [Test]
        public void UpdatePrice_EventOverridesTrend_WhenActive()
        {
            // Bear trend stock with strong positive event — event should dominate
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.MidValue, 100f, TrendDirection.Bear, 0.02f);

            var eventEffects = new EventEffects();
            _generator.SetEventEffects(eventEffects);

            // Strong positive event at peak
            var evt = new MarketEvent(MarketEventType.ShortSqueeze, 0, 0.50f, 4f);
            evt.ElapsedTime = 2f;
            eventEffects.StartEvent(evt);

            // Run multiple frames — event should push price up despite bear trend
            for (int i = 0; i < 30; i++)
                _generator.UpdatePrice(stock, 0.016f);

            Assert.Greater(stock.CurrentPrice, 100f,
                "Strong positive event should override bear trend");
        }

        [Test]
        public void UpdatePrice_GlobalEvent_AffectsAllStocks()
        {
            _generator.InitializeRound(1, 1);
            var stocks = _generator.ActiveStocks;
            Assert.Greater(stocks.Count, 1, "Need multiple stocks");

            var eventEffects = new EventEffects();
            _generator.SetEventEffects(eventEffects);

            // Record initial prices
            var initialPrices = new Dictionary<int, float>();
            foreach (var stock in stocks)
                initialPrices[stock.StockId] = stock.CurrentPrice;

            // Start global crash event at peak
            var evt = new MarketEvent(MarketEventType.MarketCrash, null, -0.30f, 4f);
            evt.ElapsedTime = 2f;
            eventEffects.StartEvent(evt);

            // Run several frames
            for (int i = 0; i < 30; i++)
            {
                foreach (var stock in stocks)
                    _generator.UpdatePrice(stock, 0.016f);
            }

            // All stocks should have dropped from the crash
            int droppedCount = 0;
            foreach (var stock in stocks)
            {
                if (stock.CurrentPrice < initialPrices[stock.StockId])
                    droppedCount++;
            }

            Assert.Greater(droppedCount, stocks.Count / 2,
                "Global crash should affect majority of stocks");
        }

        // --- Mean Reversion Tests (Story 1.4) ---

        [Test]
        public void UpdatePrice_NoEvent_ReversionMovesPriceTowardTrendLine()
        {
            // Neutral trend so trend line stays at starting price.
            // Manually offset current price above trend line, then verify reversion pulls it back.
            // Run many iterations so the reversion bias on segment selection dominates noise.
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.BlueChip, 1000f, TrendDirection.Neutral, 0f);

            // Artificially push price away from trend line
            stock.CurrentPrice = 1200f;

            // No event effects set — reversion bias should pull price back over time
            for (int i = 0; i < 600; i++)
                _generator.UpdatePrice(stock, 0.016f);

            Assert.Less(stock.CurrentPrice, 1200f,
                "Reversion should pull price back toward trend line");
        }

        [Test]
        public void UpdatePrice_NoEvent_ReversionMovesBelow_UpTowardTrendLine()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.BlueChip, 1000f, TrendDirection.Neutral, 0f);

            // Price below trend line
            stock.CurrentPrice = 800f;

            // Run many iterations so reversion bias dominates
            for (int i = 0; i < 600; i++)
                _generator.UpdatePrice(stock, 0.016f);

            Assert.Greater(stock.CurrentPrice, 800f,
                "Reversion should pull price up toward trend line");
        }

        [Test]
        public void UpdatePrice_BlueChip_RevertsSignificantlyFasterThanPenny()
        {
            // Both start with same % displacement from trend, neutral trend
            var blueChip = new StockInstance();
            blueChip.Initialize(0, "BLUE", StockTier.BlueChip, 1000f, TrendDirection.Neutral, 0f);
            blueChip.CurrentPrice = 1200f; // 20% above

            var penny = new StockInstance();
            penny.Initialize(1, "PNNY", StockTier.Penny, 1.00f, TrendDirection.Neutral, 0f);
            penny.CurrentPrice = 1.20f; // 20% above

            // Warm up: let both converge from initial displacement
            for (int i = 0; i < 300; i++)
            {
                _generator.UpdatePrice(blueChip, 0.016f);
                _generator.UpdatePrice(penny, 0.016f);
            }

            // Measure average deviation over many frames for statistical robustness.
            // A single final-frame snapshot is too noisy; averaging over the
            // steady-state window makes the assertion reliable.
            float blueChipDeviationSum = 0f;
            float pennyDeviationSum = 0f;
            int sampleCount = 2000;

            for (int i = 0; i < sampleCount; i++)
            {
                _generator.UpdatePrice(blueChip, 0.016f);
                _generator.UpdatePrice(penny, 0.016f);

                blueChipDeviationSum += Mathf.Abs(blueChip.CurrentPrice - 1000f) / 1000f;
                pennyDeviationSum += Mathf.Abs(penny.CurrentPrice - 1.00f) / 1.00f;
            }

            // Stronger reversion + lower noise → tighter oscillation → smaller avg deviation.
            // BlueChip ratio (MRS/Noise) = 0.50/0.025 = 20, Penny = 0.30/0.12 = 2.5.
            float blueChipAvgDeviation = blueChipDeviationSum / sampleCount;
            float pennyAvgDeviation = pennyDeviationSum / sampleCount;

            Assert.Less(blueChipAvgDeviation, pennyAvgDeviation,
                "Blue chip should have smaller average deviation from trend than penny");
        }

        [Test]
        public void UpdatePrice_WithActiveEvent_DoesNotApplyReversion()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.BlueChip, 1000f, TrendDirection.Neutral, 0f);

            var eventEffects = new EventEffects();
            _generator.SetEventEffects(eventEffects);

            // Start a positive event — this should prevent reversion
            var evt = new MarketEvent(MarketEventType.EarningsBeat, 0, 0.25f, 4f);
            evt.ElapsedTime = 2f;
            eventEffects.StartEvent(evt);

            // Price is at event target direction — event drives, not reversion
            float priceAfterOneFrame;
            _generator.UpdatePrice(stock, 0.016f);
            priceAfterOneFrame = stock.CurrentPrice;

            // With a positive event, price should go UP (event effect), not toward trend line
            Assert.Greater(priceAfterOneFrame, 1000f,
                "Active event should drive price, not reversion");
        }

        [Test]
        public void UpdatePrice_UpdatesTrendLineEachFrame()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.MidValue, 100f, TrendDirection.Bull, 0.05f);

            float initialTrendLine = stock.TrendLinePrice;
            _generator.UpdatePrice(stock, 0.016f);

            Assert.Greater(stock.TrendLinePrice, initialTrendLine,
                "PriceGenerator should update trend line each frame via UpdateTrendLine");
        }

        // --- Stock Pool Selection Tests (Story 1.5) ---

        [Test]
        public void SelectStocksForRound_ReturnsCorrectCountRange()
        {
            foreach (StockTier tier in System.Enum.GetValues(typeof(StockTier)))
            {
                var config = StockTierData.GetTierConfig(tier);
                for (int i = 0; i < 20; i++)
                {
                    var selected = _generator.SelectStocksForRound(tier);
                    Assert.GreaterOrEqual(selected.Count, config.MinStocksPerRound,
                        $"Tier {tier}: should select at least MinStocksPerRound");
                    Assert.LessOrEqual(selected.Count, config.MaxStocksPerRound,
                        $"Tier {tier}: should select at most MaxStocksPerRound");
                }
            }
        }

        [Test]
        public void SelectStocksForRound_NoDuplicatesWithinSelection()
        {
            for (int i = 0; i < 20; i++)
            {
                foreach (StockTier tier in System.Enum.GetValues(typeof(StockTier)))
                {
                    var selected = _generator.SelectStocksForRound(tier);
                    var tickers = new HashSet<string>();
                    foreach (var def in selected)
                    {
                        Assert.IsTrue(tickers.Add(def.TickerSymbol),
                            $"Duplicate ticker in selection: {def.TickerSymbol}");
                    }
                }
            }
        }

        [Test]
        public void SelectStocksForRound_AllFromCorrectPool()
        {
            foreach (StockTier tier in System.Enum.GetValues(typeof(StockTier)))
            {
                var selected = _generator.SelectStocksForRound(tier);
                foreach (var def in selected)
                {
                    Assert.AreEqual(tier, def.Tier,
                        $"{def.TickerSymbol}: should belong to tier {tier}");
                }
            }
        }

        [Test]
        public void SelectStocksForRound_ProducesVariety_AcrossMultipleRounds()
        {
            foreach (StockTier tier in System.Enum.GetValues(typeof(StockTier)))
            {
                var allSelections = new HashSet<string>();
                for (int i = 0; i < 20; i++)
                {
                    var selected = _generator.SelectStocksForRound(tier);
                    foreach (var def in selected)
                        allSelections.Add(def.TickerSymbol);
                }

                var pool = StockPoolData.GetPool(tier);
                // 20 rounds should see more than half the pool for any tier
                Assert.Greater(allSelections.Count, pool.Length / 2,
                    $"Tier {tier}: random selection should use variety of stocks across 20 rounds " +
                    $"(saw {allSelections.Count}/{pool.Length})");
            }
        }

        [Test]
        public void InitializeRound_UsesNamedStocks_FromPools()
        {
            _generator.InitializeRound(1, 1);
            var stocks = _generator.ActiveStocks;

            // Collect all valid tickers from all pools
            var allValidTickers = new HashSet<string>();
            foreach (StockTier tier in System.Enum.GetValues(typeof(StockTier)))
            {
                var pool = StockPoolData.GetPool(tier);
                foreach (var def in pool)
                    allValidTickers.Add(def.TickerSymbol);
            }

            foreach (var stock in stocks)
            {
                Assert.IsTrue(allValidTickers.Contains(stock.TickerSymbol),
                    $"Stock {stock.TickerSymbol} should come from a named pool");
            }
        }

        [Test]
        public void InitializeRound_NoDuplicateTickers()
        {
            for (int i = 0; i < 10; i++)
            {
                _generator.InitializeRound(1, 1);
                var tickers = new HashSet<string>();
                foreach (var stock in _generator.ActiveStocks)
                {
                    Assert.IsTrue(tickers.Add(stock.TickerSymbol),
                        $"Duplicate ticker in round: {stock.TickerSymbol}");
                }
            }
        }

        // --- Sector Correlation Tests (Story 6.4) ---

        [Test]
        public void InitializeRound_MidValueStocks_SameSectorShareTrendDirection()
        {
            // Run multiple times to increase confidence (sector correlation is deterministic per-sector)
            int correlatedRounds = 0;
            int testableRounds = 0;

            for (int trial = 0; trial < 50; trial++)
            {
                _generator.InitializeRound(3, 1); // Act 3 = MidValue tier
                var stocks = _generator.ActiveStocks;

                // Group by sector via ticker lookup
                var sectorTrends = new Dictionary<StockSector, List<TrendDirection>>();
                foreach (var stock in stocks)
                {
                    var def = FindDefinition(stock.TickerSymbol);
                    if (def.Sector == StockSector.None) continue;

                    if (!sectorTrends.ContainsKey(def.Sector))
                        sectorTrends[def.Sector] = new List<TrendDirection>();
                    sectorTrends[def.Sector].Add(stock.TrendDirection);
                }

                // Check sectors with 2+ stocks — they should share trend direction
                foreach (var kvp in sectorTrends)
                {
                    if (kvp.Value.Count < 2) continue;
                    testableRounds++;
                    bool allSame = true;
                    for (int i = 1; i < kvp.Value.Count; i++)
                    {
                        if (kvp.Value[i] != kvp.Value[0])
                        {
                            allSame = false;
                            break;
                        }
                    }
                    if (allSame) correlatedRounds++;
                }
            }

            // Guarantee we actually tested something — same-sector pairs must appear in 50 trials
            Assert.Greater(testableRounds, 0,
                "MidValue pool should produce same-sector pairs in 50 trials — check pool sector distribution");

            Assert.AreEqual(testableRounds, correlatedRounds,
                $"All same-sector stock pairs should share trend direction. " +
                $"Correlated: {correlatedRounds}/{testableRounds}");
        }

        [Test]
        public void InitializeRound_BlueChipStocks_SameSectorShareTrendDirection()
        {
            int correlatedRounds = 0;
            int testableRounds = 0;

            for (int trial = 0; trial < 50; trial++)
            {
                _generator.InitializeRound(4, 1); // Act 4 = BlueChip tier
                var stocks = _generator.ActiveStocks;

                var sectorTrends = new Dictionary<StockSector, List<TrendDirection>>();
                foreach (var stock in stocks)
                {
                    var def = FindDefinition(stock.TickerSymbol);
                    if (def.Sector == StockSector.None) continue;

                    if (!sectorTrends.ContainsKey(def.Sector))
                        sectorTrends[def.Sector] = new List<TrendDirection>();
                    sectorTrends[def.Sector].Add(stock.TrendDirection);
                }

                foreach (var kvp in sectorTrends)
                {
                    if (kvp.Value.Count < 2) continue;
                    testableRounds++;
                    bool allSame = true;
                    for (int i = 1; i < kvp.Value.Count; i++)
                    {
                        if (kvp.Value[i] != kvp.Value[0])
                        {
                            allSame = false;
                            break;
                        }
                    }
                    if (allSame) correlatedRounds++;
                }
            }

            Assert.Greater(testableRounds, 0,
                "BlueChip pool should produce same-sector pairs in 50 trials — check pool sector distribution");

            Assert.AreEqual(testableRounds, correlatedRounds,
                $"All same-sector Blue Chip pairs should share trend direction. " +
                $"Correlated: {correlatedRounds}/{testableRounds}");
        }

        [Test]
        public void InitializeRound_PennyStocks_NoSectorCorrelation()
        {
            // Penny stocks should NOT have sector correlation - each gets independent trend
            // Run many trials — if no correlation is applied, same-sector stocks will
            // sometimes differ in trend direction (statistically inevitable)
            bool anyDifference = false;

            for (int trial = 0; trial < 100; trial++)
            {
                _generator.InitializeRound(1, 1); // Act 1 = Penny tier
                var stocks = _generator.ActiveStocks;

                var sectorTrends = new Dictionary<StockSector, List<TrendDirection>>();
                foreach (var stock in stocks)
                {
                    var def = FindDefinition(stock.TickerSymbol);
                    if (def.Sector == StockSector.None) continue;

                    if (!sectorTrends.ContainsKey(def.Sector))
                        sectorTrends[def.Sector] = new List<TrendDirection>();
                    sectorTrends[def.Sector].Add(stock.TrendDirection);
                }

                foreach (var kvp in sectorTrends)
                {
                    if (kvp.Value.Count < 2) continue;
                    for (int i = 1; i < kvp.Value.Count; i++)
                    {
                        if (kvp.Value[i] != kvp.Value[0])
                        {
                            anyDifference = true;
                            break;
                        }
                    }
                    if (anyDifference) break;
                }
                if (anyDifference) break;
            }

            Assert.IsTrue(anyDifference,
                "Penny stocks should have independent trends - same-sector stocks should sometimes differ");
        }

        [Test]
        public void InitializeRound_LowValueStocks_NoSectorCorrelation()
        {
            bool anyDifference = false;

            for (int trial = 0; trial < 100; trial++)
            {
                _generator.InitializeRound(2, 1); // Act 2 = LowValue tier
                var stocks = _generator.ActiveStocks;

                var sectorTrends = new Dictionary<StockSector, List<TrendDirection>>();
                foreach (var stock in stocks)
                {
                    var def = FindDefinition(stock.TickerSymbol);
                    if (def.Sector == StockSector.None) continue;

                    if (!sectorTrends.ContainsKey(def.Sector))
                        sectorTrends[def.Sector] = new List<TrendDirection>();
                    sectorTrends[def.Sector].Add(stock.TrendDirection);
                }

                foreach (var kvp in sectorTrends)
                {
                    if (kvp.Value.Count < 2) continue;
                    for (int i = 1; i < kvp.Value.Count; i++)
                    {
                        if (kvp.Value[i] != kvp.Value[0])
                        {
                            anyDifference = true;
                            break;
                        }
                    }
                    if (anyDifference) break;
                }
                if (anyDifference) break;
            }

            Assert.IsTrue(anyDifference,
                "LowValue stocks should have independent trends - same-sector stocks should sometimes differ");
        }

        private StockDefinition FindDefinition(string ticker)
        {
            foreach (StockTier tier in System.Enum.GetValues(typeof(StockTier)))
            {
                var pool = StockPoolData.GetPool(tier);
                foreach (var def in pool)
                {
                    if (def.TickerSymbol == ticker) return def;
                }
            }
            return default;
        }

        // --- Debug Info Tests (Story 1.6) ---

        [Test]
        public void GetDebugInfo_ReturnsInfoForAllActiveStocks()
        {
            _generator.InitializeRound(1, 1);
            var debugInfos = _generator.GetDebugInfo();

            Assert.AreEqual(_generator.ActiveStocks.Count, debugInfos.Count,
                "Debug info count should match active stock count");
        }

        [Test]
        public void GetDebugInfo_ContainsCorrectTickers()
        {
            _generator.InitializeRound(1, 1);
            var debugInfos = _generator.GetDebugInfo();

            for (int i = 0; i < debugInfos.Count; i++)
            {
                Assert.AreEqual(_generator.ActiveStocks[i].TickerSymbol, debugInfos[i].Ticker,
                    $"Debug info ticker should match stock at index {i}");
            }
        }

        [Test]
        public void GetDebugInfo_NoActiveEvents_WhenNoEventEffectsSet()
        {
            _generator.InitializeRound(1, 1);
            var debugInfos = _generator.GetDebugInfo();

            foreach (var info in debugInfos)
            {
                Assert.IsFalse(info.HasActiveEvent,
                    $"{info.Ticker}: should have no active event when EventEffects not set");
            }
        }

        [Test]
        public void GetDebugInfo_ShowsActiveEvent_WhenEventRunning()
        {
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.MidValue, 100f, TrendDirection.Bull, 0.05f);

            // Need to add stock to generator via InitializeRound
            _generator.InitializeRound(1, 1);

            var eventEffects = new EventEffects();
            _generator.SetEventEffects(eventEffects);

            // Start event on first stock
            var firstStock = _generator.ActiveStocks[0];
            var evt = new MarketEvent(MarketEventType.EarningsBeat, firstStock.StockId, 0.25f, 4f);
            evt.ElapsedTime = 1f;
            eventEffects.StartEvent(evt);

            var debugInfos = _generator.GetDebugInfo();
            Assert.IsTrue(debugInfos[0].HasActiveEvent,
                "First stock should show active event");
            Assert.AreEqual(MarketEventType.EarningsBeat, debugInfos[0].ActiveEventType);
            Assert.AreEqual(3f, debugInfos[0].EventTimeRemaining, 0.01f,
                "Should show 3s remaining (4s duration - 1s elapsed)");
        }

        [Test]
        public void GetDebugInfo_EmptyList_WhenNoStocks()
        {
            var debugInfos = _generator.GetDebugInfo();
            Assert.AreEqual(0, debugInfos.Count);
        }
    }
}
