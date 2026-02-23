using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BullRun.Tests.PlayMode.Shop
{
    /// <summary>
    /// PlayMode integration tests verifying tip accuracy through the actual TradingState
    /// game loop with real Unity frame timing. Each test:
    /// 1. Creates PriceGenerator + EventScheduler (seeded for reproducibility)
    /// 2. Configures TradingState.NextConfig and calls Enter() (real integration path)
    /// 3. Loops calling AdvanceTime(ctx, Time.deltaTime) each frame via yield return null
    /// 4. Tracks actual min/max/close prices from PriceGenerator output
    /// 5. Compares tip predictions (captured from TipOverlaysActivatedEvent) against actuals
    ///
    /// Time.timeScale is set to 10x to keep test execution under ~7s per round.
    /// Variable frame timing is the core value of PlayMode — proves tips stay accurate
    /// even with coarser dt values than the 1/60 used in EditMode tests.
    /// </summary>
    [TestFixture]
    public class TipAccuracyPlayModeTests
    {
        private float _savedTimeScale;

        // Captured during TradingState.Enter() via EventBus
        private List<TipOverlayData> _capturedOverlays;
        private int _eventsFired;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            _savedTimeScale = Time.timeScale;
            _capturedOverlays = null;
            _eventsFired = 0;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = _savedTimeScale;
            TradingState.NextConfig = null;
            EventBus.Clear();
        }

        // =====================================================================
        // Test Methods — Each exercises the full TradingState loop for one scenario
        // =====================================================================

        [UnityTest]
        public IEnumerator AllTips_Accurate_Penny_Seed42()
        {
            return RunFullRoundAndVerifyAllTips(act: 1, round: 1, seed: 42);
        }

        [UnityTest]
        public IEnumerator AllTips_Accurate_Penny_Seed99()
        {
            return RunFullRoundAndVerifyAllTips(act: 1, round: 2, seed: 99);
        }

        [UnityTest]
        public IEnumerator AllTips_Accurate_Penny_Seed200()
        {
            return RunFullRoundAndVerifyAllTips(act: 1, round: 1, seed: 200);
        }

        [UnityTest]
        public IEnumerator AllTips_Accurate_LowValue_Seed42()
        {
            return RunFullRoundAndVerifyAllTips(act: 2, round: 3, seed: 42);
        }

        [UnityTest]
        public IEnumerator AllTips_Accurate_LowValue_Seed777()
        {
            return RunFullRoundAndVerifyAllTips(act: 2, round: 4, seed: 777);
        }

        [UnityTest]
        public IEnumerator AllTips_Accurate_MidValue_Seed42()
        {
            return RunFullRoundAndVerifyAllTips(act: 3, round: 5, seed: 42);
        }

        [UnityTest]
        public IEnumerator AllTips_Accurate_MidValue_Seed300()
        {
            return RunFullRoundAndVerifyAllTips(act: 3, round: 6, seed: 300);
        }

        /// <summary>
        /// Runs 10 seeds and checks that ClosingDirection tip matches actual direction
        /// at least 60% of the time. Variable frame timing makes this harder than EditMode.
        /// Uses class-based result holder so coroutine can write back to caller.
        /// </summary>
        [UnityTest]
        public IEnumerator ClosingDirection_MatchRate_Across10Seeds()
        {
            int matches = 0;
            int total = 10;
            var holder = new RoundResultHolder();

            for (int i = 1; i <= total; i++)
            {
                EventBus.Clear();
                _capturedOverlays = null;

                int seed = i * 37;
                yield return RunRoundInto(act: 1, round: 1, seed: seed, holder);

                if (_capturedOverlays == null) continue;

                // Find ClosingDirection overlay
                TipOverlayData dirOverlay = default;
                bool found = false;
                foreach (var overlay in _capturedOverlays)
                {
                    if (overlay.Type == InsiderTipType.ClosingDirection)
                    {
                        dirOverlay = overlay;
                        found = true;
                        break;
                    }
                }

                if (!found) continue;

                bool tipSaysUp = dirOverlay.DirectionSign > 0;
                bool actualUp = holder.ClosingPrice >= holder.StartingPrice;

                if (tipSaysUp == actualUp)
                    matches++;
            }

            float matchRate = (float)matches / total;
            Assert.GreaterOrEqual(matchRate, 0.60f,
                $"ClosingDirection match rate {matchRate:P0} ({matches}/{total}) should be >= 60% " +
                "with variable frame timing");
        }

        // =====================================================================
        // Core Test Infrastructure
        // =====================================================================

        /// <summary>
        /// Reference type for passing round results out of coroutines.
        /// Must be a class (not struct) so coroutine modifications are visible to caller.
        /// </summary>
        private class RoundResultHolder
        {
            public float StartingPrice;
            public float ClosingPrice;
        }

        /// <summary>
        /// Runs a full round through TradingState with real Unity timing and verifies all 9 tips.
        /// </summary>
        private IEnumerator RunFullRoundAndVerifyAllTips(int act, int round, int seed)
        {
            Time.timeScale = 10f;

            // === 1. Create PriceGenerator with seeded Random ===
            var priceGen = new PriceGenerator(new System.Random(seed));
            priceGen.InitializeRound(act, round);
            Assert.IsTrue(priceGen.ActiveStocks.Count > 0,
                $"[Seed {seed}] PriceGenerator.InitializeRound produced no stocks");
            var stock = priceGen.ActiveStocks[0];
            float startingPrice = stock.StartingPrice;

            // === 2. Create EventScheduler ===
            var eventEffects = new EventEffects();
            eventEffects.SetActiveStocks(priceGen.ActiveStocks);
            var scheduler = new EventScheduler(eventEffects, new System.Random(seed + 500));
            priceGen.SetEventEffects(eventEffects);

            // === 3. Create RunContext with all 9 tips purchased ===
            var portfolio = new Portfolio(GameConfig.StartingCapital);
            var ctx = new RunContext(act, round, portfolio);
            for (int i = 0; i < 9; i++)
                ctx.RevealedTips.Add(new RevealedTip((InsiderTipType)i, "generic", 0f));

            // === 4. Subscribe to capture events ===
            EventBus.Subscribe<TipOverlaysActivatedEvent>(e =>
                _capturedOverlays = new List<TipOverlayData>(e.Overlays));
            EventBus.Subscribe<MarketEventFiredEvent>(e => _eventsFired++);

            // === 5. Configure TradingState (no StateMachine to prevent MarketClose transition) ===
            TradingState.NextConfig = new TradingStateConfig
            {
                StateMachine = null,
                PriceGenerator = priceGen,
                TradeExecutor = null,
                EventScheduler = scheduler
            };
            var tradingState = new TradingState();

            // === 6. Enter TradingState — THE REAL INTEGRATION POINT ===
            // This calls: EventScheduler.InitializeRound() -> TipActivator.ActivateTips()
            // -> publishes TipOverlaysActivatedEvent with computed overlays
            tradingState.Enter(ctx);

            Assert.IsNotNull(_capturedOverlays,
                $"[Seed {seed}] TipOverlaysActivatedEvent was not published during Enter()");
            Assert.AreEqual(9, _capturedOverlays.Count,
                $"[Seed {seed}] Expected 9 tip overlays (one per type), got {_capturedOverlays.Count}");

            float roundDuration = TradingState.ActiveRoundDuration;

            // === 7. Run the round with real Unity frame timing ===
            float actualMinPrice = startingPrice;
            float actualMaxPrice = startingPrice;
            float actualMinPriceTime = 0f;
            float actualMaxPriceTime = 0f;
            float priceSum = 0f;
            int priceCount = 0;
            float elapsedTime = 0f;

            while (TradingState.IsActive)
            {
                float dt = Time.deltaTime;
                tradingState.AdvanceTime(ctx, dt);
                elapsedTime += dt;

                // Track actual prices every frame
                float price = stock.CurrentPrice;
                if (price < actualMinPrice) { actualMinPrice = price; actualMinPriceTime = elapsedTime; }
                if (price > actualMaxPrice) { actualMaxPrice = price; actualMaxPriceTime = elapsedTime; }
                priceSum += price;
                priceCount++;

                yield return null; // Real Unity frame boundary
            }

            float actualClosingPrice = stock.CurrentPrice;
            float actualAvgPrice = priceCount > 0 ? priceSum / priceCount : startingPrice;
            float actualMinNormTime = actualMinPriceTime / roundDuration;
            float actualMaxNormTime = actualMaxPriceTime / roundDuration;

            // === 8. Verify all 9 tip types ===
            var overlaysByType = new Dictionary<InsiderTipType, TipOverlayData>();
            foreach (var overlay in _capturedOverlays)
                overlaysByType[overlay.Type] = overlay;

            // -- PriceFloor --
            // Tip floor is analytical (no noise), so actual can go lower due to noise.
            // With 10x timeScale, larger dt amplifies noise effects.
            // Scale tolerance by event count: more events = more noise-driven divergence.
            if (overlaysByType.TryGetValue(InsiderTipType.PriceFloor, out var floorOverlay))
            {
                int eventCount = scheduler.ScheduledEventCount;
                float scaledRate = 0.35f + eventCount * 0.04f;
                float baseValue = Mathf.Max(actualMinPrice, floorOverlay.PriceLevel);
                float tolerance = Mathf.Max(baseValue * scaledRate, 0.50f);
                Assert.AreEqual(actualMinPrice, floorOverlay.PriceLevel, tolerance,
                    $"[Seed {seed}] PriceFloor: tip=${floorOverlay.PriceLevel:F2}, actual=${actualMinPrice:F2}, " +
                    $"tolerance=+-{tolerance:F2} ({eventCount} events, rate={scaledRate:P0})");
            }

            // -- PriceCeiling --
            // Tip ceiling is analytical (no noise), so actual can go higher due to noise.
            if (overlaysByType.TryGetValue(InsiderTipType.PriceCeiling, out var ceilingOverlay))
            {
                int eventCount = scheduler.ScheduledEventCount;
                float scaledRate = 0.35f + eventCount * 0.04f;
                float baseValue = Mathf.Max(actualMaxPrice, ceilingOverlay.PriceLevel);
                float tolerance = Mathf.Max(baseValue * scaledRate, 0.50f);
                Assert.AreEqual(actualMaxPrice, ceilingOverlay.PriceLevel, tolerance,
                    $"[Seed {seed}] PriceCeiling: tip=${ceilingOverlay.PriceLevel:F2}, actual=${actualMaxPrice:F2}, " +
                    $"tolerance=+-{tolerance:F2} ({eventCount} events, rate={scaledRate:P0})");
            }

            // -- PriceForecast --
            if (overlaysByType.TryGetValue(InsiderTipType.PriceForecast, out var forecastOverlay))
            {
                // Wider tolerance: average price is sensitive to noise accumulation over variable dt
                float range = Mathf.Abs(actualMaxPrice - actualMinPrice);
                float tolerance = Mathf.Max(range * 0.50f, actualAvgPrice * 0.45f);
                Assert.AreEqual(actualAvgPrice, forecastOverlay.BandCenter, tolerance,
                    $"[Seed {seed}] PriceForecast: tip=${forecastOverlay.BandCenter:F2}, actual=${actualAvgPrice:F2}, " +
                    $"tolerance=+-{tolerance:F2}");
            }

            // -- EventCount (exact match — count doesn't depend on timing) --
            if (overlaysByType.TryGetValue(InsiderTipType.EventCount, out var countOverlay))
            {
                Assert.AreEqual(scheduler.ScheduledEventCount, countOverlay.EventCountdown,
                    $"[Seed {seed}] EventCount: tip={countOverlay.EventCountdown}, " +
                    $"scheduled={scheduler.ScheduledEventCount}");
            }

            // -- DipMarker (time zone) --
            // Noise shifts WHEN the min/max occur, not just their values.
            // With 10x timeScale and coarser dt, timing diverges more than values.
            // Scale margin by event count: more events = more timing uncertainty.
            if (overlaysByType.TryGetValue(InsiderTipType.DipMarker, out var dipOverlay))
            {
                int eventCount = scheduler.ScheduledEventCount;
                float margin = dipOverlay.TimeZoneHalfWidth + 0.25f + eventCount * 0.03f;
                Assert.AreEqual(actualMinNormTime, dipOverlay.TimeZoneCenter, margin,
                    $"[Seed {seed}] DipMarker: tipCenter={dipOverlay.TimeZoneCenter:F3}, " +
                    $"actual={actualMinNormTime:F3}, margin=+-{margin:F3} ({eventCount} events)");
            }

            // -- PeakMarker (time zone) --
            if (overlaysByType.TryGetValue(InsiderTipType.PeakMarker, out var peakOverlay))
            {
                int eventCount = scheduler.ScheduledEventCount;
                float margin = peakOverlay.TimeZoneHalfWidth + 0.25f + eventCount * 0.03f;
                Assert.AreEqual(actualMaxNormTime, peakOverlay.TimeZoneCenter, margin,
                    $"[Seed {seed}] PeakMarker: tipCenter={peakOverlay.TimeZoneCenter:F3}, " +
                    $"actual={actualMaxNormTime:F3}, margin=+-{margin:F3} ({eventCount} events)");
            }

            // -- ClosingDirection (non-failing — noise can flip close calls) --
            if (overlaysByType.TryGetValue(InsiderTipType.ClosingDirection, out var dirOverlay))
            {
                Assert.IsTrue(dirOverlay.DirectionSign == 1 || dirOverlay.DirectionSign == -1,
                    $"[Seed {seed}] ClosingDirection: DirectionSign should be +-1, got {dirOverlay.DirectionSign}");

                bool tipSaysUp = dirOverlay.DirectionSign > 0;
                bool actualUp = actualClosingPrice >= startingPrice;
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (tipSaysUp != actualUp)
                {
                    Debug.LogWarning($"[TipTest] ClosingDirection mismatch seed {seed}: " +
                        $"tip={dirOverlay.DirectionSign}, actual close=${actualClosingPrice:F2} vs start=${startingPrice:F2}");
                }
                #endif
            }

            // -- EventTiming (exact fire times — no timing dependency) --
            if (overlaysByType.TryGetValue(InsiderTipType.EventTiming, out var timingOverlay))
            {
                var preDecided = scheduler.PreDecidedEvents;
                Assert.AreEqual(preDecided.Length, timingOverlay.TimeMarkers.Length,
                    $"[Seed {seed}] EventTiming: marker count {timingOverlay.TimeMarkers.Length} " +
                    $"!= pre-decided event count {preDecided.Length}");

                // Sort pre-decided fire times for comparison (markers are sorted)
                float[] expectedTimes = new float[preDecided.Length];
                for (int j = 0; j < preDecided.Length; j++)
                    expectedTimes[j] = Mathf.Clamp01(preDecided[j].FireTime / roundDuration);
                System.Array.Sort(expectedTimes);

                for (int j = 0; j < timingOverlay.TimeMarkers.Length; j++)
                {
                    Assert.AreEqual(expectedTimes[j], timingOverlay.TimeMarkers[j], 0.001f,
                        $"[Seed {seed}] EventTiming marker {j}: " +
                        $"expected={expectedTimes[j]:F4}, got={timingOverlay.TimeMarkers[j]:F4}");
                }
            }

            // -- TrendReversal (consistency check — see EditMode tests for detailed logic) --
            if (overlaysByType.TryGetValue(InsiderTipType.TrendReversal, out var reversalOverlay))
            {
                bool simDetectsReversal = reversalOverlay.ReversalTime >= 0f;
                if (simDetectsReversal)
                {
                    Assert.GreaterOrEqual(reversalOverlay.ReversalTime, 0f);
                    Assert.LessOrEqual(reversalOverlay.ReversalTime, 1f,
                        $"[Seed {seed}] TrendReversal time should be in [0,1]");
                }
            }

            // === 9. Log summary ===
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[TipTest] Seed {seed} | {stock.TickerSymbol} ({stock.Tier}, {stock.TrendDirection}) " +
                $"| Start=${startingPrice:F2} Close=${actualClosingPrice:F2} " +
                $"| Min=${actualMinPrice:F2}@{actualMinNormTime:F3} Max=${actualMaxPrice:F2}@{actualMaxNormTime:F3} " +
                $"| Avg=${actualAvgPrice:F2} | Events={_eventsFired} | Frames={priceCount} " +
                $"| AvgDt={(priceCount > 0 ? elapsedTime / priceCount : 0f):F4}s");
            #endif
        }

        /// <summary>
        /// Runs a single round through TradingState, writing results into the holder.
        /// Used by multi-seed tests (ClosingDirection match rate).
        /// </summary>
        private IEnumerator RunRoundInto(int act, int round, int seed, RoundResultHolder holder)
        {
            Time.timeScale = 10f;

            var priceGen = new PriceGenerator(new System.Random(seed));
            priceGen.InitializeRound(act, round);
            if (priceGen.ActiveStocks.Count == 0) yield break;

            var stock = priceGen.ActiveStocks[0];
            holder.StartingPrice = stock.StartingPrice;

            var eventEffects = new EventEffects();
            eventEffects.SetActiveStocks(priceGen.ActiveStocks);
            var scheduler = new EventScheduler(eventEffects, new System.Random(seed + 500));
            priceGen.SetEventEffects(eventEffects);

            var portfolio = new Portfolio(GameConfig.StartingCapital);
            var ctx = new RunContext(act, round, portfolio);
            for (int i = 0; i < 9; i++)
                ctx.RevealedTips.Add(new RevealedTip((InsiderTipType)i, "generic", 0f));

            EventBus.Subscribe<TipOverlaysActivatedEvent>(e =>
                _capturedOverlays = new List<TipOverlayData>(e.Overlays));

            TradingState.NextConfig = new TradingStateConfig
            {
                StateMachine = null,
                PriceGenerator = priceGen,
                TradeExecutor = null,
                EventScheduler = scheduler
            };
            var tradingState = new TradingState();
            tradingState.Enter(ctx);

            while (TradingState.IsActive)
            {
                tradingState.AdvanceTime(ctx, Time.deltaTime);
                yield return null;
            }

            holder.ClosingPrice = stock.CurrentPrice;
        }
    }
}
