using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BullRun.Tests.PriceEngine
{
    /// <summary>
    /// Diagnostic test suite that simulates full trading sessions and analyzes
    /// price movement behavior to identify flatline periods, momentum gaps, and
    /// dead zones. Outputs detailed per-round analytics.
    /// </summary>
    [TestFixture]
    public class SessionAnalysisTests
    {
        private const float DeltaTime = 0.016f; // ~60fps
        private const float RoundDuration = 60f;
        private const int FramesPerRound = (int)(RoundDuration / DeltaTime);

        // Flatline detection thresholds
        // "Visual flat" = price change < 0.3% per second (barely perceptible on chart)
        private const float FlatThresholdPerSecond = 0.003f;
        // A "flatline window" = consecutive frames where per-frame change is below this
        // Min net movement guarantee is 0.5%/s → 0.008% per frame at 60fps.
        // Threshold set below that to detect true flatlines (not just minimum-guaranteed movement).
        private const float FlatFrameThreshold = 0.00005f; // 0.005% per frame

        // Momentum analysis
        private const float StrongMoveThreshold = 0.005f; // 0.5% per frame = exciting
        private const int WindowSize = 60; // 1 second rolling window

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

        // ============================================================
        // FULL SESSION SIMULATION — Runs all 8 rounds with events
        // ============================================================

        [Test]
        public void FullSession_AnalyzePriceMovement_AllRounds()
        {
            var report = new StringBuilder();
            report.AppendLine("=== FULL SESSION PRICE MOVEMENT ANALYSIS ===");
            report.AppendLine($"Simulation: {FramesPerRound} frames/round @ {DeltaTime}s/frame = {RoundDuration}s rounds");
            report.AppendLine();

            var allRoundMetrics = new List<RoundMetrics>();

            for (int round = 1; round <= GameConfig.TotalRounds; round++)
            {
                int act = RunContext.GetActForRound(round);
                StockTier tier = RunContext.GetTierForAct(act);

                _generator.InitializeRound(act, round);
                var stocks = _generator.ActiveStocks;

                // Set up event system for this round
                var eventEffects = new EventEffects();
                eventEffects.SetActiveStocks(stocks);
                _generator.SetEventEffects(eventEffects);
                var scheduler = new EventScheduler(eventEffects);
                scheduler.InitializeRound(round, act, tier, stocks, RoundDuration);

                report.AppendLine($"--- ROUND {round} (Act {act}, {tier}) | {stocks.Count} stocks | {scheduler.ScheduledEventCount} events ---");

                for (int s = 0; s < stocks.Count; s++)
                {
                    var stock = stocks[s];
                    var metrics = SimulateAndAnalyze(stock, scheduler, stocks, tier, round, act);
                    allRoundMetrics.Add(metrics);

                    report.AppendLine($"  [{stock.TickerSymbol}] {stock.TrendDirection} trend={stock.TrendPerSecond:F4}/s");
                    report.AppendLine($"    Start: ${metrics.StartPrice:F2} → End: ${metrics.EndPrice:F2} ({metrics.TotalReturnPercent:+0.0;-0.0}%)");
                    report.AppendLine($"    Flatline frames: {metrics.TotalFlatFrames}/{FramesPerRound} ({metrics.FlatFramePercent:F1}%)");
                    report.AppendLine($"    Longest flat streak: {metrics.LongestFlatStreak} frames ({metrics.LongestFlatStreak * DeltaTime:F2}s)");
                    report.AppendLine($"    Flat windows (>0.5s): {metrics.FlatWindowCount} totaling {metrics.TotalFlatWindowSeconds:F1}s");
                    report.AppendLine($"    Strong moves (>{StrongMoveThreshold * 100:F1}%/frame): {metrics.StrongMoveFrames} ({metrics.StrongMovePercent:F1}%)");
                    report.AppendLine($"    Direction changes: {metrics.DirectionChanges} (avg momentum run: {metrics.AvgMomentumRunFrames:F0} frames)");
                    report.AppendLine($"    Rolling volatility — min: {metrics.MinRollingVolatility:F5}, max: {metrics.MaxRollingVolatility:F5}, avg: {metrics.AvgRollingVolatility:F5}");
                    report.AppendLine($"    MinSlope floor hits: {metrics.MinSlopeHits} ({metrics.MinSlopeHitPercent:F1}% of segments)");
                    report.AppendLine();
                }
            }

            // Aggregate summary
            report.AppendLine("=== AGGREGATE ANALYSIS ===");
            AppendAggregateSummary(report, allRoundMetrics);

            // Output report via Debug.Log so it appears in test runner output
            Debug.Log(report.ToString());

            // ASSERTION: Flag if any stock has >15% flat frames (indicates real problem)
            foreach (var m in allRoundMetrics)
            {
                Assert.Less(m.FlatFramePercent, 15f,
                    $"[{m.Ticker}] Round {m.Round}: {m.FlatFramePercent:F1}% flat frames exceeds 15% threshold. " +
                    $"Longest streak: {m.LongestFlatStreak} frames ({m.LongestFlatStreak * DeltaTime:F2}s)");
            }
        }

        // ============================================================
        // PER-TIER DEEP DIVES — Isolated analysis without events
        // ============================================================

        [Test]
        public void TierAnalysis_PennyStocks_NoEvents_MomentumProfile()
        {
            AnalyzeTierWithoutEvents(StockTier.Penny, "PENNY TIER (No Events)");
        }

        [Test]
        public void TierAnalysis_LowValueStocks_NoEvents_MomentumProfile()
        {
            AnalyzeTierWithoutEvents(StockTier.LowValue, "LOW-VALUE TIER (No Events)");
        }

        [Test]
        public void TierAnalysis_MidValueStocks_NoEvents_MomentumProfile()
        {
            AnalyzeTierWithoutEvents(StockTier.MidValue, "MID-VALUE TIER (No Events)");
        }

        [Test]
        public void TierAnalysis_BlueChipStocks_NoEvents_MomentumProfile()
        {
            AnalyzeTierWithoutEvents(StockTier.BlueChip, "BLUE CHIP TIER (No Events)");
        }

        // ============================================================
        // NEUTRAL TREND FOCUS — The worst case for flatlines
        // ============================================================

        [Test]
        public void NeutralTrend_AllTiers_FlatlineAnalysis()
        {
            var report = new StringBuilder();
            report.AppendLine("=== NEUTRAL TREND FLATLINE ANALYSIS (Worst Case) ===");
            report.AppendLine("Testing neutral-trend stocks across all tiers — these have zero trend drift.");
            report.AppendLine();

            var allMetrics = new List<RoundMetrics>();

            foreach (StockTier tier in Enum.GetValues(typeof(StockTier)))
            {
                var config = StockTierData.GetTierConfig(tier);
                float midPrice = (config.MinPrice + config.MaxPrice) / 2f;

                report.AppendLine($"--- {tier} (Neutral, ${midPrice:F2}, noise={config.NoiseAmplitude}, reversion={config.MeanReversionSpeed}) ---");

                // Run 5 samples per tier for statistical coverage
                for (int sample = 0; sample < 5; sample++)
                {
                    var stock = new StockInstance();
                    stock.Initialize(0, $"{tier}_{sample}", tier, midPrice, TrendDirection.Neutral, 0f);

                    var metrics = SimulateStockOnly(stock, $"{tier}_{sample}", 0, 0);
                    allMetrics.Add(metrics);

                    report.AppendLine($"  Sample {sample + 1}: flat frames={metrics.TotalFlatFrames} ({metrics.FlatFramePercent:F1}%), " +
                        $"longest streak={metrics.LongestFlatStreak} ({metrics.LongestFlatStreak * DeltaTime:F2}s), " +
                        $"flat windows={metrics.FlatWindowCount} ({metrics.TotalFlatWindowSeconds:F1}s), " +
                        $"dir changes={metrics.DirectionChanges}");
                }
                report.AppendLine();
            }

            Debug.Log(report.ToString());

            // Neutral stocks should still move. Flag if any has >20% flat frames.
            foreach (var m in allMetrics)
            {
                Assert.Less(m.FlatFramePercent, 20f,
                    $"[{m.Ticker}] Neutral trend: {m.FlatFramePercent:F1}% flat frames — price stalling without trend drive");
            }
        }

        // ============================================================
        // MEAN REVERSION CANCELLATION — When reversion fights trend
        // ============================================================

        [Test]
        public void MeanReversionCancellation_AnalyzeSlopeSuppression()
        {
            var report = new StringBuilder();
            report.AppendLine("=== MEAN REVERSION vs TREND CANCELLATION ANALYSIS ===");
            report.AppendLine("Testing scenarios where reversion bias may cancel trend+noise, producing flatness.");
            report.AppendLine();

            var allMetrics = new List<RoundMetrics>();

            foreach (StockTier tier in Enum.GetValues(typeof(StockTier)))
            {
                var config = StockTierData.GetTierConfig(tier);

                // Use weakest trend for each tier — most susceptible to reversion cancellation
                float startPrice = (config.MinPrice + config.MaxPrice) / 2f;
                float weakestTrend = config.MinTrendStrength;

                report.AppendLine($"--- {tier} (Bull, weakest trend={weakestTrend}, noise={config.NoiseAmplitude}, reversion={config.MeanReversionSpeed}) ---");

                for (int sample = 0; sample < 5; sample++)
                {
                    var stock = new StockInstance();
                    stock.Initialize(0, $"{tier}_weak_{sample}", tier, startPrice, TrendDirection.Bull, weakestTrend);

                    var metrics = SimulateStockOnly(stock, $"{tier}_weak_{sample}", 0, 0);
                    allMetrics.Add(metrics);

                    report.AppendLine($"  Sample {sample + 1}: flat={metrics.FlatFramePercent:F1}%, " +
                        $"longest streak={metrics.LongestFlatStreak} ({metrics.LongestFlatStreak * DeltaTime:F2}s), " +
                        $"minSlope hits={metrics.MinSlopeHits} ({metrics.MinSlopeHitPercent:F1}%), " +
                        $"return={metrics.TotalReturnPercent:+0.0;-0.0}%");
                }
                report.AppendLine();
            }

            Debug.Log(report.ToString());
        }

        // ============================================================
        // EVENT DEAD ZONE ANALYSIS — Gaps between events
        // ============================================================

        [Test]
        public void EventDeadZones_AnalyzeGapsBetweenEvents()
        {
            var report = new StringBuilder();
            report.AppendLine("=== EVENT DEAD ZONE ANALYSIS ===");
            report.AppendLine("Analyzing price behavior in gaps between market events.");
            report.AppendLine();

            for (int round = 1; round <= GameConfig.TotalRounds; round++)
            {
                int act = RunContext.GetActForRound(round);
                StockTier tier = RunContext.GetTierForAct(act);

                _generator.InitializeRound(act, round);
                var stocks = _generator.ActiveStocks;

                var eventEffects = new EventEffects();
                eventEffects.SetActiveStocks(stocks);
                _generator.SetEventEffects(eventEffects);
                var scheduler = new EventScheduler(eventEffects);
                scheduler.InitializeRound(round, act, tier, stocks, RoundDuration);

                // Record event fire times
                var eventTimes = new List<float>();
                for (int i = 0; i < scheduler.ScheduledEventCount; i++)
                    eventTimes.Add(scheduler.GetScheduledTime(i));

                report.AppendLine($"--- ROUND {round} (Act {act}, {tier}) ---");
                report.Append("  Event times: ");
                foreach (float t in eventTimes)
                    report.Append($"{t:F1}s ");
                report.AppendLine();

                // Calculate gaps
                eventTimes.Sort();
                report.Append("  Gaps: [0→first] ");
                float prevEnd = 0f;
                for (int i = 0; i < eventTimes.Count; i++)
                {
                    // Assume average event duration of 6s
                    float gapBefore = eventTimes[i] - prevEnd;
                    report.Append($"{gapBefore:F1}s ");
                    prevEnd = eventTimes[i] + 6f; // approximate event end
                }
                float tailGap = RoundDuration - prevEnd;
                report.AppendLine($"[last→end] {tailGap:F1}s");

                // Simulate the round and track per-second volatility
                var stock = stocks[0];
                var priceHistory = new float[FramesPerRound];
                float elapsed = 0f;
                for (int f = 0; f < FramesPerRound; f++)
                {
                    elapsed += DeltaTime;
                    scheduler.Update(elapsed, DeltaTime, stocks, tier);
                    _generator.UpdatePrice(stock, DeltaTime);
                    priceHistory[f] = stock.CurrentPrice;
                }

                // Compute per-second volatility profile
                int secondsCount = (int)(RoundDuration);
                report.Append("  Per-second volatility: ");
                for (int sec = 0; sec < secondsCount; sec++)
                {
                    int startFrame = (int)(sec / DeltaTime);
                    int endFrame = Math.Min(startFrame + (int)(1f / DeltaTime), FramesPerRound - 1);

                    float minP = float.MaxValue, maxP = float.MinValue;
                    for (int f = startFrame; f <= endFrame && f < FramesPerRound; f++)
                    {
                        if (priceHistory[f] < minP) minP = priceHistory[f];
                        if (priceHistory[f] > maxP) maxP = priceHistory[f];
                    }
                    float secVolatility = (maxP - minP) / ((minP + maxP) / 2f);
                    // Use simple bar chart: · = low, ▪ = medium, █ = high
                    char bar = secVolatility < 0.002f ? '·' : (secVolatility < 0.01f ? '▪' : '█');
                    report.Append(bar);
                }
                report.AppendLine();
                report.AppendLine();
            }

            Debug.Log(report.ToString());
        }

        // ============================================================
        // SEGMENT SLOPE DISTRIBUTION — What slopes is the engine producing?
        // ============================================================

        [Test]
        public void SegmentSlopeDistribution_AllTiers()
        {
            var report = new StringBuilder();
            report.AppendLine("=== SEGMENT SLOPE DISTRIBUTION ===");
            report.AppendLine("Analyzing the distribution of segment slopes to identify clustering near zero.");
            report.AppendLine();

            foreach (StockTier tier in Enum.GetValues(typeof(StockTier)))
            {
                var config = StockTierData.GetTierConfig(tier);
                float midPrice = (config.MinPrice + config.MaxPrice) / 2f;

                // Test both neutral and weakest bull trend
                foreach (var (label, direction, strength) in new[]
                {
                    ("Neutral", TrendDirection.Neutral, 0f),
                    ("Weakest Bull", TrendDirection.Bull, config.MinTrendStrength)
                })
                {
                    var stock = new StockInstance();
                    stock.Initialize(0, $"{tier}_{label}", tier, midPrice, direction, strength);

                    // Collect slopes when new segments are picked
                    var slopes = new List<float>();
                    float lastSegTime = 0f;
                    int minSlopeHits = 0;
                    float minSlopeThreshold = midPrice * 0.002f;

                    for (int f = 0; f < FramesPerRound; f++)
                    {
                        float prevSegTime = stock.SegmentTimeRemaining;
                        _generator.UpdatePrice(stock, DeltaTime);

                        // Detect new segment (time remaining jumped up)
                        if (stock.SegmentTimeRemaining > prevSegTime || prevSegTime <= 0f)
                        {
                            slopes.Add(stock.SegmentSlope);
                            if (Math.Abs(stock.SegmentSlope) <= minSlopeThreshold * 1.01f)
                                minSlopeHits++;
                        }
                    }

                    // Analyze distribution
                    float avgAbsSlope = 0f;
                    int nearZeroCount = 0;
                    float maxSlope = float.MinValue, minSlope = float.MaxValue;
                    foreach (float slope in slopes)
                    {
                        float abs = Math.Abs(slope);
                        avgAbsSlope += abs;
                        if (abs < midPrice * 0.003f) nearZeroCount++;
                        if (slope > maxSlope) maxSlope = slope;
                        if (slope < minSlope) minSlope = slope;
                    }
                    avgAbsSlope /= slopes.Count;

                    report.AppendLine($"  {tier} ({label}): {slopes.Count} segments over 60s");
                    report.AppendLine($"    Avg |slope|: {avgAbsSlope:F4}, range: [{minSlope:F4}, {maxSlope:F4}]");
                    report.AppendLine($"    Near-zero (<0.3% price/s): {nearZeroCount}/{slopes.Count} ({100f * nearZeroCount / slopes.Count:F0}%)");
                    report.AppendLine($"    MinSlope floor hits: {minSlopeHits}/{slopes.Count} ({100f * minSlopeHits / slopes.Count:F0}%)");
                }
                report.AppendLine();
            }

            Debug.Log(report.ToString());
        }

        // ============================================================
        // MOMENTUM CONTINUITY — How smooth is the movement?
        // ============================================================

        [Test]
        public void MomentumContinuity_AllTiers_WithEvents()
        {
            var report = new StringBuilder();
            report.AppendLine("=== MOMENTUM CONTINUITY ANALYSIS ===");
            report.AppendLine("Tracking how long momentum runs last and how often direction reverses.");
            report.AppendLine("Goal: smooth, flowing price curves without jerky reversals or dead zones.");
            report.AppendLine();

            for (int round = 1; round <= 4; round++) // Focus on early rounds (Acts 1-2)
            {
                int act = RunContext.GetActForRound(round);
                StockTier tier = RunContext.GetTierForAct(act);

                _generator.InitializeRound(act, round);
                var stocks = _generator.ActiveStocks;

                var eventEffects = new EventEffects();
                eventEffects.SetActiveStocks(stocks);
                _generator.SetEventEffects(eventEffects);
                var scheduler = new EventScheduler(eventEffects);
                scheduler.InitializeRound(round, act, tier, stocks, RoundDuration);

                report.AppendLine($"--- ROUND {round} (Act {act}, {tier}) ---");

                foreach (var stock in stocks)
                {
                    var priceHistory = new float[FramesPerRound];
                    float elapsed = 0f;

                    for (int f = 0; f < FramesPerRound; f++)
                    {
                        elapsed += DeltaTime;
                        scheduler.Update(elapsed, DeltaTime, stocks, tier);
                        _generator.UpdatePrice(stock, DeltaTime);
                        priceHistory[f] = stock.CurrentPrice;
                    }

                    // Analyze momentum runs: consecutive frames moving same direction
                    var momentumRuns = new List<int>();
                    int currentRun = 1;
                    float prevDelta = 0f;

                    for (int f = 1; f < FramesPerRound; f++)
                    {
                        float delta = priceHistory[f] - priceHistory[f - 1];
                        if (f > 1 && ((delta > 0 && prevDelta > 0) || (delta < 0 && prevDelta < 0)))
                        {
                            currentRun++;
                        }
                        else
                        {
                            if (currentRun > 0) momentumRuns.Add(currentRun);
                            currentRun = 1;
                        }
                        prevDelta = delta;
                    }
                    if (currentRun > 0) momentumRuns.Add(currentRun);

                    // Compute momentum stats
                    float avgRun = 0f;
                    int maxRun = 0, minRun = int.MaxValue;
                    int shortRuns = 0; // < 5 frames = jerky
                    int longRuns = 0;  // > 30 frames = smooth
                    foreach (int run in momentumRuns)
                    {
                        avgRun += run;
                        if (run > maxRun) maxRun = run;
                        if (run < minRun) minRun = run;
                        if (run < 5) shortRuns++;
                        if (run > 30) longRuns++;
                    }
                    avgRun /= momentumRuns.Count;

                    // Find "dead spots" — 1+ second windows with <0.5% total movement
                    int deadSpots = 0;
                    float deadSeconds = 0f;
                    int windowFrames = (int)(1f / DeltaTime); // 1 second
                    for (int f = 0; f < FramesPerRound - windowFrames; f++)
                    {
                        float windowMove = Math.Abs(priceHistory[f + windowFrames] - priceHistory[f]) / priceHistory[f];
                        if (windowMove < 0.005f)
                        {
                            if (f == 0 || Math.Abs(priceHistory[f] - priceHistory[f - 1]) / priceHistory[f] >= FlatFrameThreshold)
                            {
                                deadSpots++;
                            }
                            deadSeconds += DeltaTime;
                        }
                    }

                    report.AppendLine($"  [{stock.TickerSymbol}] {stock.TrendDirection}");
                    report.AppendLine($"    Momentum runs: avg={avgRun:F1} frames, max={maxRun}, min={minRun}");
                    report.AppendLine($"    Short runs (<5 frames): {shortRuns}/{momentumRuns.Count} ({100f * shortRuns / momentumRuns.Count:F0}%)");
                    report.AppendLine($"    Long smooth runs (>30 frames): {longRuns}/{momentumRuns.Count} ({100f * longRuns / momentumRuns.Count:F0}%)");
                    report.AppendLine($"    Dead spots (1s windows <0.5% move): {deadSpots}, total dead time: {deadSeconds:F1}s");
                }
                report.AppendLine();
            }

            Debug.Log(report.ToString());
        }

        // ============================================================
        // EARLY ROUNDS FLATLINE FREQUENCY — Statistical sampling
        // ============================================================

        [Test]
        public void EarlyRounds_FlatlineFrequency_StatisticalSampling()
        {
            var report = new StringBuilder();
            report.AppendLine("=== EARLY ROUNDS FLATLINE FREQUENCY (30 trials) ===");
            report.AppendLine("Running 30 simulated sessions focusing on rounds 1-4 (Acts 1-2).");
            report.AppendLine("Counting how often flatline periods appear and their severity.");
            report.AppendLine();

            int totalStockTrials = 0;
            int trialsWithFlatlines = 0;
            float worstFlatPercent = 0f;
            string worstFlatTicker = "";
            int worstFlatRound = 0;

            var flatPercentBuckets = new int[10]; // 0-1%, 1-2%, ..., 9-10%+

            for (int trial = 0; trial < 30; trial++)
            {
                for (int round = 1; round <= 4; round++)
                {
                    int act = RunContext.GetActForRound(round);
                    StockTier tier = RunContext.GetTierForAct(act);

                    _generator.InitializeRound(act, round);
                    var stocks = _generator.ActiveStocks;

                    var eventEffects = new EventEffects();
                    eventEffects.SetActiveStocks(stocks);
                    _generator.SetEventEffects(eventEffects);
                    var scheduler = new EventScheduler(eventEffects);
                    scheduler.InitializeRound(round, act, tier, stocks, RoundDuration);

                    foreach (var stock in stocks)
                    {
                        var metrics = SimulateAndAnalyze(stock, scheduler, stocks, tier, round, act);
                        totalStockTrials++;

                        if (metrics.FlatFramePercent > 2f)
                            trialsWithFlatlines++;

                        if (metrics.FlatFramePercent > worstFlatPercent)
                        {
                            worstFlatPercent = metrics.FlatFramePercent;
                            worstFlatTicker = metrics.Ticker;
                            worstFlatRound = round;
                        }

                        int bucket = Math.Min((int)metrics.FlatFramePercent, 9);
                        flatPercentBuckets[bucket]++;
                    }
                }
            }

            report.AppendLine($"Total stock-round trials: {totalStockTrials}");
            report.AppendLine($"Trials with >2% flat frames: {trialsWithFlatlines} ({100f * trialsWithFlatlines / totalStockTrials:F1}%)");
            report.AppendLine($"Worst case: [{worstFlatTicker}] Round {worstFlatRound} at {worstFlatPercent:F1}% flat");
            report.AppendLine();
            report.AppendLine("Flatline % distribution:");
            for (int i = 0; i < 10; i++)
            {
                string label = i < 9 ? $"{i}-{i + 1}%" : "9%+";
                int barLen = (int)(40f * flatPercentBuckets[i] / totalStockTrials);
                report.AppendLine($"  {label,5}: {new string('█', barLen)} ({flatPercentBuckets[i]})");
            }

            Debug.Log(report.ToString());

            // Soft assertion: less than 10% of trials should have significant flatlines
            float flatlineRate = (float)trialsWithFlatlines / totalStockTrials;
            Assert.Less(flatlineRate, 0.10f,
                $"{flatlineRate * 100:F1}% of stock-round trials had >2% flat frames. " +
                $"Worst: [{worstFlatTicker}] Round {worstFlatRound} at {worstFlatPercent:F1}%");
        }

        // ============================================================
        // VISUAL CHART SIMULATION — ASCII price chart for visual inspection
        // ============================================================

        [Test]
        public void VisualChart_FirstRound_AsciiPriceHistory()
        {
            var report = new StringBuilder();
            report.AppendLine("=== ASCII PRICE CHARTS — Round 1 (Penny Stocks) ===");
            report.AppendLine("Each row = 1 stock, each column = 1 second. Height = price relative to range.");
            report.AppendLine();

            _generator.InitializeRound(1, 1);
            var stocks = _generator.ActiveStocks;

            var eventEffects = new EventEffects();
            eventEffects.SetActiveStocks(stocks);
            _generator.SetEventEffects(eventEffects);
            var scheduler = new EventScheduler(eventEffects);
            scheduler.InitializeRound(1, 1, StockTier.Penny, stocks, RoundDuration);

            int chartWidth = 60; // 1 char per second
            int chartHeight = 15;

            foreach (var stock in stocks)
            {
                // Simulate and record per-second prices
                var secondPrices = new float[chartWidth];
                float elapsed = 0f;
                int secondIdx = 0;

                for (int f = 0; f < FramesPerRound; f++)
                {
                    elapsed += DeltaTime;
                    scheduler.Update(elapsed, DeltaTime, stocks, StockTier.Penny);
                    _generator.UpdatePrice(stock, DeltaTime);

                    int currentSecond = (int)(elapsed);
                    if (currentSecond >= chartWidth) currentSecond = chartWidth - 1;
                    secondPrices[currentSecond] = stock.CurrentPrice;
                }

                // Determine price range
                float minP = float.MaxValue, maxP = float.MinValue;
                for (int i = 0; i < chartWidth; i++)
                {
                    if (secondPrices[i] < minP) minP = secondPrices[i];
                    if (secondPrices[i] > maxP) maxP = secondPrices[i];
                }
                if (maxP <= minP) maxP = minP + 0.01f;

                report.AppendLine($"[{stock.TickerSymbol}] {stock.TrendDirection} | ${minP:F2} — ${maxP:F2}");

                // Render chart
                for (int row = chartHeight - 1; row >= 0; row--)
                {
                    float rowPrice = minP + (maxP - minP) * row / (chartHeight - 1);
                    report.Append($"${rowPrice,7:F2} |");
                    for (int col = 0; col < chartWidth; col++)
                    {
                        float normalizedPrice = (secondPrices[col] - minP) / (maxP - minP) * (chartHeight - 1);
                        int priceRow = (int)Math.Round(normalizedPrice);
                        report.Append(priceRow == row ? '●' : ' ');
                    }
                    report.AppendLine("|");
                }
                report.Append("         ");
                report.AppendLine(new string('-', chartWidth + 2));
                report.Append("         0");
                report.Append(new string(' ', chartWidth / 2 - 2));
                report.Append("30s");
                report.Append(new string(' ', chartWidth / 2 - 3));
                report.AppendLine("60s");
                report.AppendLine();
            }

            Debug.Log(report.ToString());
        }

        // ============================================================
        // HELPERS
        // ============================================================

        private void AnalyzeTierWithoutEvents(StockTier tier, string headerLabel)
        {
            var report = new StringBuilder();
            report.AppendLine($"=== {headerLabel} ===");

            var config = StockTierData.GetTierConfig(tier);
            report.AppendLine($"Config: noise={config.NoiseAmplitude}, reversion={config.MeanReversionSpeed}, " +
                $"trend=[{config.MinTrendStrength}, {config.MaxTrendStrength}]");
            report.AppendLine();

            var allMetrics = new List<RoundMetrics>();

            foreach (TrendDirection dir in Enum.GetValues(typeof(TrendDirection)))
            {
                float trendStrength = dir == TrendDirection.Neutral ? 0f : config.MinTrendStrength;
                float midPrice = (config.MinPrice + config.MaxPrice) / 2f;

                for (int sample = 0; sample < 3; sample++)
                {
                    var stock = new StockInstance();
                    stock.Initialize(0, $"{tier}_{dir}_{sample}", tier, midPrice, dir, trendStrength);
                    var metrics = SimulateStockOnly(stock, $"{tier}_{dir}_{sample}", 0, 0);
                    allMetrics.Add(metrics);

                    report.AppendLine($"  {dir} #{sample + 1}: flat={metrics.FlatFramePercent:F1}%, " +
                        $"longest streak={metrics.LongestFlatStreak} ({metrics.LongestFlatStreak * DeltaTime:F2}s), " +
                        $"dead spots={metrics.DeadSpotCount} ({metrics.TotalDeadSeconds:F1}s), " +
                        $"return={metrics.TotalReturnPercent:+0.0;-0.0}%");
                }
            }

            report.AppendLine();
            AppendAggregateSummary(report, allMetrics);

            Debug.Log(report.ToString());
        }

        private RoundMetrics SimulateAndAnalyze(StockInstance stock, EventScheduler scheduler,
            IReadOnlyList<StockInstance> allStocks, StockTier tier, int round, int act)
        {
            var prices = new float[FramesPerRound];
            prices[0] = stock.CurrentPrice;
            float elapsed = 0f;

            for (int f = 0; f < FramesPerRound; f++)
            {
                elapsed += DeltaTime;
                scheduler.Update(elapsed, DeltaTime, allStocks, tier);
                _generator.UpdatePrice(stock, DeltaTime);
                prices[f] = stock.CurrentPrice;
            }

            return ComputeMetrics(prices, stock, round, act);
        }

        private RoundMetrics SimulateStockOnly(StockInstance stock, string ticker, int round, int act)
        {
            var prices = new float[FramesPerRound];
            prices[0] = stock.CurrentPrice;

            for (int f = 0; f < FramesPerRound; f++)
            {
                _generator.UpdatePrice(stock, DeltaTime);
                prices[f] = stock.CurrentPrice;
            }

            return ComputeMetrics(prices, stock, round, act);
        }

        private RoundMetrics ComputeMetrics(float[] prices, StockInstance stock, int round, int act)
        {
            var m = new RoundMetrics
            {
                Ticker = stock.TickerSymbol,
                Round = round,
                Act = act,
                Tier = stock.Tier,
                TrendDirection = stock.TrendDirection,
                StartPrice = prices[0],
                EndPrice = prices[prices.Length - 1],
            };

            m.TotalReturnPercent = (m.EndPrice - m.StartPrice) / m.StartPrice * 100f;

            // Flatline analysis
            int flatStreak = 0;
            int totalFlat = 0;
            int flatWindowCount = 0;
            float flatWindowSeconds = 0f;
            bool inFlatWindow = false;
            int flatWindowStart = 0;

            // Direction change tracking
            int directionChanges = 0;
            int currentMomentumRun = 0;
            float totalMomentumRunSum = 0f;
            int momentumRunCount = 0;
            float prevDelta = 0f;

            // Strong moves
            int strongMoves = 0;

            // Rolling volatility
            float minRollingVol = float.MaxValue;
            float maxRollingVol = float.MinValue;
            float rollingVolSum = 0f;
            int rollingVolCount = 0;

            // MinSlope tracking
            int minSlopeHits = 0;
            int totalSegments = 0;
            float prevSegTime = 0f;
            float minSlopeThreshold = stock.CurrentPrice * 0.002f;

            // Dead spots (1s windows with <0.5% total move)
            int deadSpots = 0;
            float deadSeconds = 0f;
            int windowFrames = (int)(1f / DeltaTime);

            for (int f = 1; f < prices.Length; f++)
            {
                float delta = prices[f] - prices[f - 1];
                float pctChange = Math.Abs(delta) / prices[f - 1];

                // Flat frame detection
                if (pctChange < FlatFrameThreshold)
                {
                    flatStreak++;
                    totalFlat++;
                    if (flatStreak > m.LongestFlatStreak)
                        m.LongestFlatStreak = flatStreak;

                    if (!inFlatWindow && flatStreak >= (int)(0.5f / DeltaTime))
                    {
                        inFlatWindow = true;
                        flatWindowCount++;
                    }
                    if (inFlatWindow)
                        flatWindowSeconds += DeltaTime;
                }
                else
                {
                    flatStreak = 0;
                    inFlatWindow = false;
                }

                // Direction changes
                if (f > 1)
                {
                    bool sameDir = (delta > 0 && prevDelta > 0) || (delta < 0 && prevDelta < 0);
                    if (sameDir)
                    {
                        currentMomentumRun++;
                    }
                    else
                    {
                        directionChanges++;
                        totalMomentumRunSum += currentMomentumRun;
                        momentumRunCount++;
                        currentMomentumRun = 1;
                    }
                }

                // Strong moves
                if (pctChange > StrongMoveThreshold)
                    strongMoves++;

                // Rolling volatility (1-second window standard deviation of returns)
                if (f >= WindowSize)
                {
                    float sum = 0f, sumSq = 0f;
                    for (int w = f - WindowSize; w < f; w++)
                    {
                        float r = (prices[w + 1] - prices[w]) / prices[w];
                        sum += r;
                        sumSq += r * r;
                    }
                    float mean = sum / WindowSize;
                    float variance = sumSq / WindowSize - mean * mean;
                    float vol = (float)Math.Sqrt(Math.Max(0, variance));
                    rollingVolSum += vol;
                    rollingVolCount++;
                    if (vol < minRollingVol) minRollingVol = vol;
                    if (vol > maxRollingVol) maxRollingVol = vol;
                }

                prevDelta = delta;
            }

            // Dead spot analysis
            for (int f = 0; f < prices.Length - windowFrames; f++)
            {
                float windowMove = Math.Abs(prices[f + windowFrames] - prices[f]) / prices[f];
                if (windowMove < 0.005f)
                {
                    if (f == 0 || Math.Abs(prices[f] - prices[f - 1]) / prices[f] >= FlatFrameThreshold)
                        deadSpots++;
                    deadSeconds += DeltaTime;
                }
            }

            // Segment / minSlope tracking (approximate via segment time remaining observation)
            // Can't directly track here since we don't observe segment transitions during sim
            // So we estimate from the slope distribution captured elsewhere

            m.TotalFlatFrames = totalFlat;
            m.FlatFramePercent = 100f * totalFlat / (prices.Length - 1);
            m.FlatWindowCount = flatWindowCount;
            m.TotalFlatWindowSeconds = flatWindowSeconds;
            m.DirectionChanges = directionChanges;
            m.AvgMomentumRunFrames = momentumRunCount > 0 ? totalMomentumRunSum / momentumRunCount : prices.Length;
            m.StrongMoveFrames = strongMoves;
            m.StrongMovePercent = 100f * strongMoves / (prices.Length - 1);
            m.MinRollingVolatility = rollingVolCount > 0 ? minRollingVol : 0f;
            m.MaxRollingVolatility = rollingVolCount > 0 ? maxRollingVol : 0f;
            m.AvgRollingVolatility = rollingVolCount > 0 ? rollingVolSum / rollingVolCount : 0f;
            m.DeadSpotCount = deadSpots;
            m.TotalDeadSeconds = deadSeconds;

            return m;
        }

        private void AppendAggregateSummary(StringBuilder report, List<RoundMetrics> allMetrics)
        {
            if (allMetrics.Count == 0) return;

            // Per-tier aggregation
            var tierGroups = new Dictionary<StockTier, List<RoundMetrics>>();
            foreach (var m in allMetrics)
            {
                if (!tierGroups.ContainsKey(m.Tier))
                    tierGroups[m.Tier] = new List<RoundMetrics>();
                tierGroups[m.Tier].Add(m);
            }

            foreach (var kvp in tierGroups)
            {
                var tier = kvp.Key;
                var metrics = kvp.Value;

                float avgFlat = 0f, maxFlat = 0f;
                float avgDeadSeconds = 0f, maxDeadSeconds = 0f;
                int totalDeadSpots = 0;
                float avgReturn = 0f;

                foreach (var m in metrics)
                {
                    avgFlat += m.FlatFramePercent;
                    if (m.FlatFramePercent > maxFlat) maxFlat = m.FlatFramePercent;
                    avgDeadSeconds += m.TotalDeadSeconds;
                    if (m.TotalDeadSeconds > maxDeadSeconds) maxDeadSeconds = m.TotalDeadSeconds;
                    totalDeadSpots += m.DeadSpotCount;
                    avgReturn += Math.Abs(m.TotalReturnPercent);
                }

                avgFlat /= metrics.Count;
                avgDeadSeconds /= metrics.Count;
                avgReturn /= metrics.Count;

                report.AppendLine($"  {tier} ({metrics.Count} samples):");
                report.AppendLine($"    Flat frames — avg: {avgFlat:F1}%, worst: {maxFlat:F1}%");
                report.AppendLine($"    Dead time — avg: {avgDeadSeconds:F1}s, worst: {maxDeadSeconds:F1}s, total dead spots: {totalDeadSpots}");
                report.AppendLine($"    Avg absolute return: {avgReturn:F1}%");
            }

            // Per-trend direction aggregation
            var dirGroups = new Dictionary<TrendDirection, List<RoundMetrics>>();
            foreach (var m in allMetrics)
            {
                if (!dirGroups.ContainsKey(m.TrendDirection))
                    dirGroups[m.TrendDirection] = new List<RoundMetrics>();
                dirGroups[m.TrendDirection].Add(m);
            }

            report.AppendLine();
            report.AppendLine("  By Trend Direction:");
            foreach (var kvp in dirGroups)
            {
                var metrics = kvp.Value;
                float avgFlat = 0f, avgDead = 0f;
                foreach (var m in metrics)
                {
                    avgFlat += m.FlatFramePercent;
                    avgDead += m.TotalDeadSeconds;
                }
                avgFlat /= metrics.Count;
                avgDead /= metrics.Count;
                report.AppendLine($"    {kvp.Key} ({metrics.Count} samples): avg flat={avgFlat:F1}%, avg dead time={avgDead:F1}s");
            }
        }

        /// <summary>
        /// Metrics collected for a single stock's round simulation.
        /// </summary>
        private class RoundMetrics
        {
            public string Ticker;
            public int Round;
            public int Act;
            public StockTier Tier;
            public TrendDirection TrendDirection;
            public float StartPrice;
            public float EndPrice;
            public float TotalReturnPercent;

            // Flatline metrics
            public int TotalFlatFrames;
            public float FlatFramePercent;
            public int LongestFlatStreak;
            public int FlatWindowCount;
            public float TotalFlatWindowSeconds;

            // Momentum metrics
            public int DirectionChanges;
            public float AvgMomentumRunFrames;
            public int StrongMoveFrames;
            public float StrongMovePercent;

            // Volatility
            public float MinRollingVolatility;
            public float MaxRollingVolatility;
            public float AvgRollingVolatility;

            // Segment slope
            public int MinSlopeHits;
            public float MinSlopeHitPercent;

            // Dead spots
            public int DeadSpotCount;
            public float TotalDeadSeconds;
        }
    }
}
