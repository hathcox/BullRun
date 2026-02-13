using System.Collections.Generic;
using NUnit.Framework;

namespace BullRun.Tests.Events
{
    [TestFixture]
    public class EventSchedulerTests
    {
        private EventEffects _eventEffects;
        private EventScheduler _scheduler;
        private List<StockInstance> _activeStocks;
        private System.Random _deterministicRandom;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            _eventEffects = new EventEffects();
            _deterministicRandom = new System.Random(42);
            _scheduler = new EventScheduler(_eventEffects, _deterministicRandom);

            // Create test stocks
            _activeStocks = new List<StockInstance>();
            var stock1 = new StockInstance();
            stock1.Initialize(0, "TEST1", StockTier.MidValue, 100f, TrendDirection.Neutral, 0f);
            _activeStocks.Add(stock1);

            var stock2 = new StockInstance();
            stock2.Initialize(1, "TEST2", StockTier.MidValue, 200f, TrendDirection.Neutral, 0f);
            _activeStocks.Add(stock2);
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
        }

        // --- Task 1: InitializeRound tests ---

        [Test]
        public void InitializeRound_EarlyActs_Schedules2To3Events()
        {
            // Run multiple times to verify range
            int minSeen = int.MaxValue;
            int maxSeen = int.MinValue;

            for (int i = 0; i < 100; i++)
            {
                var rng = new System.Random(i);
                var scheduler = new EventScheduler(_eventEffects, rng);
                scheduler.InitializeRound(1, 1, StockTier.MidValue, _activeStocks, 60f);
                int count = scheduler.ScheduledEventCount;
                if (count < minSeen) minSeen = count;
                if (count > maxSeen) maxSeen = count;
            }

            // MidValue EventFrequencyModifier is 1.0, so event count should be exactly 2-3
            Assert.GreaterOrEqual(minSeen, 2, "Early rounds should schedule at least 2 events (AC1)");
            Assert.LessOrEqual(maxSeen, 3, "Early rounds should not exceed 3 events with modifier 1.0 (AC1)");
        }

        [Test]
        public void InitializeRound_LateActs_Schedules3To4Events()
        {
            int minSeen = int.MaxValue;
            int maxSeen = int.MinValue;

            for (int i = 0; i < 100; i++)
            {
                var rng = new System.Random(i);
                var scheduler = new EventScheduler(_eventEffects, rng);
                scheduler.InitializeRound(5, 3, StockTier.MidValue, _activeStocks, 60f);
                int count = scheduler.ScheduledEventCount;
                if (count < minSeen) minSeen = count;
                if (count > maxSeen) maxSeen = count;
            }

            // MidValue EventFrequencyModifier is 1.0, so event count should be exactly 3-4
            Assert.GreaterOrEqual(minSeen, 3, "Late rounds should schedule at least 3 events (AC1)");
            Assert.LessOrEqual(maxSeen, 4, "Late rounds should not exceed 4 events with modifier 1.0 (AC1)");
        }

        [Test]
        public void InitializeRound_HighFrequencyTier_ScalesEventCount()
        {
            // Penny tier has EventFrequencyModifier = 1.5
            int totalPenny = 0;
            int totalMid = 0;
            int runs = 200;

            for (int i = 0; i < runs; i++)
            {
                var rng1 = new System.Random(i);
                var scheduler1 = new EventScheduler(_eventEffects, rng1);
                scheduler1.InitializeRound(1, 1, StockTier.Penny, _activeStocks, 60f);
                totalPenny += scheduler1.ScheduledEventCount;

                var rng2 = new System.Random(i);
                var scheduler2 = new EventScheduler(_eventEffects, rng2);
                scheduler2.InitializeRound(1, 1, StockTier.MidValue, _activeStocks, 60f);
                totalMid += scheduler2.ScheduledEventCount;
            }

            float avgPenny = (float)totalPenny / runs;
            float avgMid = (float)totalMid / runs;

            Assert.Greater(avgPenny, avgMid, "Penny tier (1.5x modifier) should average more events than MidValue (1.0x)");
        }

        [Test]
        public void InitializeRound_LowFrequencyTier_ReducesEventCount()
        {
            // BlueChip tier has EventFrequencyModifier = 0.5
            int totalBlue = 0;
            int totalMid = 0;
            int runs = 200;

            for (int i = 0; i < runs; i++)
            {
                var rng1 = new System.Random(i);
                var scheduler1 = new EventScheduler(_eventEffects, rng1);
                scheduler1.InitializeRound(1, 1, StockTier.BlueChip, _activeStocks, 60f);
                totalBlue += scheduler1.ScheduledEventCount;

                var rng2 = new System.Random(i);
                var scheduler2 = new EventScheduler(_eventEffects, rng2);
                scheduler2.InitializeRound(1, 1, StockTier.MidValue, _activeStocks, 60f);
                totalMid += scheduler2.ScheduledEventCount;
            }

            float avgBlue = (float)totalBlue / runs;
            float avgMid = (float)totalMid / runs;

            Assert.Less(avgBlue, avgMid, "BlueChip tier (0.5x modifier) should average fewer events than MidValue (1.0x)");

            // BlueChip 0.5x: early rounds 2-3 * 0.5 = 1-2 (clamped to min 1)
            // Verify minimum is enforced
            for (int i = 0; i < 50; i++)
            {
                var rng = new System.Random(i);
                var scheduler = new EventScheduler(_eventEffects, rng);
                scheduler.InitializeRound(1, 1, StockTier.BlueChip, _activeStocks, 60f);
                Assert.GreaterOrEqual(scheduler.ScheduledEventCount, 1, "BlueChip should have at least 1 event (Mathf.Max(1,...))");
            }
        }

        // --- Task 1: Event timing distribution tests ---

        [Test]
        public void InitializeRound_EventTimesWithinBufferedWindow()
        {
            float roundDuration = 60f;
            _scheduler.InitializeRound(1, 1, StockTier.MidValue, _activeStocks, roundDuration);

            for (int i = 0; i < _scheduler.ScheduledEventCount; i++)
            {
                float time = _scheduler.GetScheduledTime(i);
                Assert.GreaterOrEqual(time, EventSchedulerConfig.EarlyBufferSeconds,
                    $"Event {i} fires too early (in buffer zone)");
                Assert.LessOrEqual(time, roundDuration - EventSchedulerConfig.LateBufferSeconds,
                    $"Event {i} fires too late (in buffer zone)");
            }
        }

        [Test]
        public void InitializeRound_NoEventsInBufferZones()
        {
            float roundDuration = 60f;
            float earlyBuffer = EventSchedulerConfig.EarlyBufferSeconds;
            float lateBuffer = EventSchedulerConfig.LateBufferSeconds;

            for (int seed = 0; seed < 50; seed++)
            {
                var rng = new System.Random(seed);
                var scheduler = new EventScheduler(_eventEffects, rng);
                scheduler.InitializeRound(1, 1, StockTier.MidValue, _activeStocks, roundDuration);

                for (int i = 0; i < scheduler.ScheduledEventCount; i++)
                {
                    float time = scheduler.GetScheduledTime(i);
                    Assert.GreaterOrEqual(time, earlyBuffer, $"Seed {seed}, Event {i}: fires in early buffer zone");
                    Assert.LessOrEqual(time, roundDuration - lateBuffer, $"Seed {seed}, Event {i}: fires in late buffer zone");
                }
            }
        }

        // --- Task 2: Tier-aware event selection ---

        [Test]
        public void SelectEventType_ReturnsValidEventForTier()
        {
            var config = _scheduler.SelectEventType(StockTier.MidValue);

            // Verify the returned event is available for MidValue
            bool availableForTier = false;
            for (int i = 0; i < config.TierAvailability.Length; i++)
            {
                if (config.TierAvailability[i] == StockTier.MidValue)
                {
                    availableForTier = true;
                    break;
                }
            }

            Assert.IsTrue(availableForTier, $"Selected event {config.EventType} should be available for MidValue tier");
        }

        [Test]
        public void SelectEventType_ExcludesUnavailableEvents()
        {
            // PumpAndDump is Penny-only — should never appear for BlueChip
            bool pumpAndDumpSeen = false;

            for (int i = 0; i < 500; i++)
            {
                var rng = new System.Random(i);
                var scheduler = new EventScheduler(_eventEffects, rng);
                var config = scheduler.SelectEventType(StockTier.BlueChip);

                if (config.EventType == MarketEventType.PumpAndDump)
                {
                    pumpAndDumpSeen = true;
                    break;
                }
            }

            Assert.IsFalse(pumpAndDumpSeen, "PumpAndDump should never be selected for BlueChip tier");
        }

        [Test]
        public void SelectEventType_RarityWeightingProducesValidSelection()
        {
            // Just verify it doesn't crash and returns a valid config
            for (int i = 0; i < 100; i++)
            {
                var rng = new System.Random(i);
                var scheduler = new EventScheduler(_eventEffects, rng);

                foreach (StockTier tier in new[] { StockTier.Penny, StockTier.LowValue, StockTier.MidValue, StockTier.BlueChip })
                {
                    var config = scheduler.SelectEventType(tier);
                    Assert.IsTrue(config.Duration > 0f,
                        $"Should return valid event config for {tier}, got {config.EventType} with zero duration");
                }
            }
        }

        // --- Task 3: Event firing with stock targeting ---

        [Test]
        public void FireEvent_GlobalEvent_SetsNullTargetStockId()
        {
            MarketEventFiredEvent received = default;
            EventBus.Subscribe<MarketEventFiredEvent>(e => received = e);

            _scheduler.FireEvent(EventDefinitions.MarketCrash, _activeStocks);

            Assert.IsNull(received.AffectedStockIds, "MarketCrash should be global (null AffectedStockIds)");
        }

        [Test]
        public void FireEvent_BullRunEvent_SetsNullTargetStockId()
        {
            MarketEventFiredEvent received = default;
            EventBus.Subscribe<MarketEventFiredEvent>(e => received = e);

            _scheduler.FireEvent(EventDefinitions.BullRunEvent, _activeStocks);

            Assert.IsNull(received.AffectedStockIds, "BullRun should be global (null AffectedStockIds)");
        }

        [Test]
        public void FireEvent_StockSpecificEvent_SetsValidTargetStockId()
        {
            MarketEventFiredEvent received = default;
            EventBus.Subscribe<MarketEventFiredEvent>(e => received = e);

            _scheduler.FireEvent(EventDefinitions.EarningsBeat, _activeStocks);

            Assert.IsNotNull(received.AffectedStockIds, "EarningsBeat should target a specific stock");
            Assert.AreEqual(1, received.AffectedStockIds.Length);

            // Target should be one of the active stocks
            int targetId = received.AffectedStockIds[0];
            bool validTarget = false;
            for (int i = 0; i < _activeStocks.Count; i++)
            {
                if (_activeStocks[i].StockId == targetId)
                {
                    validTarget = true;
                    break;
                }
            }
            Assert.IsTrue(validTarget, $"Target stock ID {targetId} should be in active stocks list");
        }

        [Test]
        public void FireEvent_CallsEventEffectsStartEvent()
        {
            _scheduler.FireEvent(EventDefinitions.EarningsBeat, _activeStocks);

            Assert.AreEqual(1, _eventEffects.ActiveEventCount, "EventEffects should have one active event after FireEvent");
        }

        // --- Task 4: Per-frame Update ---

        [Test]
        public void Update_FiresEventAtScheduledTime()
        {
            _scheduler.InitializeRound(1, 1, StockTier.MidValue, _activeStocks, 60f);

            // Find the earliest scheduled time
            float earliestTime = float.MaxValue;
            for (int i = 0; i < _scheduler.ScheduledEventCount; i++)
            {
                float t = _scheduler.GetScheduledTime(i);
                if (t < earliestTime) earliestTime = t;
            }

            // Update just before — no events should fire
            _scheduler.Update(earliestTime - 0.1f, 0.016f, _activeStocks, StockTier.MidValue);
            Assert.AreEqual(0, _scheduler.FiredEventCount, "No events should fire before scheduled time");

            // Update at the scheduled time — event should fire
            _scheduler.Update(earliestTime, 0.016f, _activeStocks, StockTier.MidValue);
            Assert.AreEqual(1, _scheduler.FiredEventCount, "One event should fire at scheduled time");
        }

        [Test]
        public void Update_DoesNotReFireAlreadyFiredEvents()
        {
            _scheduler.InitializeRound(1, 1, StockTier.MidValue, _activeStocks, 60f);

            float earliestTime = float.MaxValue;
            for (int i = 0; i < _scheduler.ScheduledEventCount; i++)
            {
                float t = _scheduler.GetScheduledTime(i);
                if (t < earliestTime) earliestTime = t;
            }

            _scheduler.Update(earliestTime, 0.016f, _activeStocks, StockTier.MidValue);
            int firedAfterFirst = _scheduler.FiredEventCount;

            // Update again with same time
            _scheduler.Update(earliestTime + 0.016f, 0.016f, _activeStocks, StockTier.MidValue);
            Assert.AreEqual(firedAfterFirst, _scheduler.FiredEventCount,
                "Should not re-fire already fired events");
        }

        [Test]
        public void Update_CallsEventEffectsUpdateActiveEvents()
        {
            _scheduler.InitializeRound(1, 1, StockTier.MidValue, _activeStocks, 60f);

            // Manually fire an event to have an active event
            _scheduler.FireEvent(EventDefinitions.EarningsBeat, _activeStocks);
            Assert.AreEqual(1, _eventEffects.ActiveEventCount);

            // Update with deltaTime — should advance event timers via UpdateActiveEvents
            // Since EarningsBeat has 5s duration, it should remain active after 0.1s
            _scheduler.Update(0f, 0.1f, _activeStocks, StockTier.MidValue);
            Assert.AreEqual(1, _eventEffects.ActiveEventCount, "Event should still be active after 0.1s");

            // Advance past duration (5s) — event should expire
            _scheduler.Update(0f, 6f, _activeStocks, StockTier.MidValue);
            Assert.AreEqual(0, _eventEffects.ActiveEventCount, "Event should expire after 6s");
        }

        // --- Task 5: EventSchedulerConfig ---

        [Test]
        public void EventSchedulerConfig_HasCorrectDefaults()
        {
            Assert.AreEqual(2, EventSchedulerConfig.MinEventsEarlyRounds);
            Assert.AreEqual(3, EventSchedulerConfig.MaxEventsEarlyRounds);
            Assert.AreEqual(3, EventSchedulerConfig.MinEventsLateRounds);
            Assert.AreEqual(4, EventSchedulerConfig.MaxEventsLateRounds);
            Assert.AreEqual(5f, EventSchedulerConfig.EarlyBufferSeconds, 0.01f);
            Assert.AreEqual(5f, EventSchedulerConfig.LateBufferSeconds, 0.01f);
        }

        // --- GetEventsForTier helper ---

        [Test]
        public void GetEventsForTier_ReturnsOnlyAvailableEvents()
        {
            var pennyEvents = EventDefinitions.GetEventsForTier(StockTier.Penny);
            var blueChipEvents = EventDefinitions.GetEventsForTier(StockTier.BlueChip);

            // PumpAndDump is Penny-only
            bool pennyHasPumpAndDump = false;
            for (int i = 0; i < pennyEvents.Count; i++)
            {
                if (pennyEvents[i].EventType == MarketEventType.PumpAndDump)
                    pennyHasPumpAndDump = true;
            }
            Assert.IsTrue(pennyHasPumpAndDump, "Penny tier should include PumpAndDump");

            bool blueHasPumpAndDump = false;
            for (int i = 0; i < blueChipEvents.Count; i++)
            {
                if (blueChipEvents[i].EventType == MarketEventType.PumpAndDump)
                    blueHasPumpAndDump = true;
            }
            Assert.IsFalse(blueHasPumpAndDump, "BlueChip tier should NOT include PumpAndDump");
        }

        [Test]
        public void GetEventsForTier_AllTiersHaveAtLeastEarningsBeatAndMiss()
        {
            foreach (StockTier tier in new[] { StockTier.Penny, StockTier.LowValue, StockTier.MidValue, StockTier.BlueChip })
            {
                var events = EventDefinitions.GetEventsForTier(tier);
                bool hasEarningsBeat = false;
                bool hasEarningsMiss = false;

                for (int i = 0; i < events.Count; i++)
                {
                    if (events[i].EventType == MarketEventType.EarningsBeat) hasEarningsBeat = true;
                    if (events[i].EventType == MarketEventType.EarningsMiss) hasEarningsMiss = true;
                }

                Assert.IsTrue(hasEarningsBeat, $"{tier} should have EarningsBeat available");
                Assert.IsTrue(hasEarningsMiss, $"{tier} should have EarningsMiss available");
            }
        }

        // --- Story 5-2: End-to-end headline integration tests ---

        [Test]
        public void FireEvent_EarningsBeat_PublishesHeadlineWithTicker()
        {
            _eventEffects.SetActiveStocks(_activeStocks);
            _eventEffects.SetHeadlineRandom(new System.Random(99));

            MarketEventFiredEvent received = default;
            EventBus.Subscribe<MarketEventFiredEvent>(e => received = e);

            _scheduler.FireEvent(EventDefinitions.EarningsBeat, _activeStocks);

            Assert.IsNotNull(received.Headline, "Headline should not be null");
            Assert.IsTrue(received.Headline.Length > 0, "Headline should not be empty");
            Assert.IsTrue(received.IsPositive, "EarningsBeat should be positive");
            Assert.AreEqual(5f, received.Duration, 0.01f, "Duration should match EventDefinitions config");
        }

        [Test]
        public void FireEvent_EarningsMiss_PublishesNegativeHeadline()
        {
            _eventEffects.SetActiveStocks(_activeStocks);
            _eventEffects.SetHeadlineRandom(new System.Random(99));

            MarketEventFiredEvent received = default;
            EventBus.Subscribe<MarketEventFiredEvent>(e => received = e);

            _scheduler.FireEvent(EventDefinitions.EarningsMiss, _activeStocks);

            Assert.IsNotNull(received.Headline);
            Assert.IsFalse(received.IsPositive, "EarningsMiss should not be positive");
            Assert.AreEqual(5f, received.Duration, 0.01f);
        }

        [Test]
        public void FireEvent_EarningsBeat_PriceEffectInExpectedRange()
        {
            MarketEventFiredEvent received = default;
            EventBus.Subscribe<MarketEventFiredEvent>(e => received = e);

            // Fire many times and verify price effect range
            for (int i = 0; i < 100; i++)
            {
                var rng = new System.Random(i);
                var scheduler = new EventScheduler(_eventEffects, rng);
                scheduler.FireEvent(EventDefinitions.EarningsBeat, _activeStocks);

                Assert.GreaterOrEqual(received.PriceEffectPercent, 0.20f,
                    $"EarningsBeat price effect should be >= 20%, got {received.PriceEffectPercent}");
                Assert.LessOrEqual(received.PriceEffectPercent, 0.50f,
                    $"EarningsBeat price effect should be <= 50%, got {received.PriceEffectPercent}");
            }
        }

        [Test]
        public void FireEvent_EarningsMiss_PriceEffectInExpectedRange()
        {
            MarketEventFiredEvent received = default;
            EventBus.Subscribe<MarketEventFiredEvent>(e => received = e);

            for (int i = 0; i < 100; i++)
            {
                var rng = new System.Random(i);
                var scheduler = new EventScheduler(_eventEffects, rng);
                scheduler.FireEvent(EventDefinitions.EarningsMiss, _activeStocks);

                Assert.LessOrEqual(received.PriceEffectPercent, -0.20f,
                    $"EarningsMiss price effect should be <= -20%, got {received.PriceEffectPercent}");
                Assert.GreaterOrEqual(received.PriceEffectPercent, -0.50f,
                    $"EarningsMiss price effect should be >= -50%, got {received.PriceEffectPercent}");
            }
        }

        [Test]
        public void FireEvent_EarningsBeat_HeadlineContainsTargetStockTicker()
        {
            _eventEffects.SetActiveStocks(_activeStocks);
            _eventEffects.SetHeadlineRandom(new System.Random(42));

            MarketEventFiredEvent received = default;
            EventBus.Subscribe<MarketEventFiredEvent>(e => received = e);

            // Use a fixed seed so we know which stock gets targeted
            var fixedScheduler = new EventScheduler(_eventEffects, new System.Random(42));
            fixedScheduler.FireEvent(EventDefinitions.EarningsBeat, _activeStocks);

            Assert.IsNotNull(received.AffectedTickerSymbols);
            Assert.AreEqual(1, received.AffectedTickerSymbols.Length);

            string ticker = received.AffectedTickerSymbols[0];
            Assert.IsTrue(received.Headline.Contains(ticker),
                $"Headline '{received.Headline}' should contain ticker '{ticker}'");
        }

        // --- Story 5-3: Tier-Specific Event Tests ---

        [Test]
        public void FireEvent_PumpAndDump_CreatesMultiPhaseEvent()
        {
            var pennyStocks = new List<StockInstance>();
            var stock = new StockInstance();
            stock.Initialize(0, "MEME", StockTier.Penny, 1f, TrendDirection.Neutral, 0f);
            pennyStocks.Add(stock);

            MarketEventFiredEvent received = default;
            EventBus.Subscribe<MarketEventFiredEvent>(e => received = e);

            _scheduler.FireEvent(EventDefinitions.PumpAndDump, pennyStocks);

            Assert.AreEqual(MarketEventType.PumpAndDump, received.EventType);
            Assert.AreEqual(8f, received.Duration, 0.01f, "PumpAndDump duration should be 8s");
            Assert.IsTrue(received.IsPositive, "PumpAndDump should appear positive to lure players during pump phase");
        }

        [Test]
        public void FireEvent_PumpAndDump_PriceRisesThenCrashes()
        {
            var pennyStocks = new List<StockInstance>();
            var stock = new StockInstance();
            stock.Initialize(0, "MEME", StockTier.Penny, 1f, TrendDirection.Neutral, 0f);
            pennyStocks.Add(stock);

            var effects = new EventEffects();
            effects.SetActiveStocks(pennyStocks);
            var scheduler = new EventScheduler(effects, new System.Random(42));

            scheduler.FireEvent(EventDefinitions.PumpAndDump, pennyStocks);

            // Phase 0 mid-point: price should be above start
            var activeEvents = effects.GetActiveEventsForStock(0);
            Assert.AreEqual(1, activeEvents.Count);
            var evt = activeEvents[0];

            Assert.IsNotNull(evt.Phases, "PumpAndDump should have phases");
            Assert.AreEqual(2, evt.Phases.Count, "PumpAndDump should have 2 phases");
            Assert.Greater(evt.Phases[0].TargetPricePercent, 0f, "Phase 0 (pump) should be positive");
            Assert.Less(evt.Phases[1].TargetPricePercent, 0f, "Phase 1 (dump) should be negative");
        }

        [Test]
        public void FireEvent_SECInvestigation_SinglePhaseNegative()
        {
            var pennyStocks = new List<StockInstance>();
            var stock = new StockInstance();
            stock.Initialize(0, "MEME", StockTier.Penny, 5f, TrendDirection.Neutral, 0f);
            pennyStocks.Add(stock);

            MarketEventFiredEvent received = default;
            EventBus.Subscribe<MarketEventFiredEvent>(e => received = e);

            _scheduler.FireEvent(EventDefinitions.SECInvestigation, pennyStocks);

            Assert.AreEqual(MarketEventType.SECInvestigation, received.EventType);
            Assert.AreEqual(10f, received.Duration, 0.01f);
            Assert.Less(received.PriceEffectPercent, 0f, "SEC Investigation should have negative price effect");
            Assert.IsFalse(received.IsPositive);
        }

        [Test]
        public void FireEvent_MergerRumor_SinglePhasePositive()
        {
            MarketEventFiredEvent received = default;
            EventBus.Subscribe<MarketEventFiredEvent>(e => received = e);

            _scheduler.FireEvent(EventDefinitions.MergerRumor, _activeStocks);

            Assert.AreEqual(MarketEventType.MergerRumor, received.EventType);
            Assert.AreEqual(6f, received.Duration, 0.01f);
            Assert.Greater(received.PriceEffectPercent, 0f, "Merger Rumor should have positive price effect");
            Assert.IsTrue(received.IsPositive);
        }

        [Test]
        public void FireEvent_SectorRotation_CreatesEventsForMultipleStocks()
        {
            // Create stocks with different sectors
            var sectorStocks = new List<StockInstance>();
            var tech1 = new StockInstance();
            tech1.Initialize(0, "NOVA", StockTier.MidValue, 100f, TrendDirection.Neutral, 0f);
            tech1.SetSector(StockSector.Tech);
            sectorStocks.Add(tech1);

            var tech2 = new StockInstance();
            tech2.Initialize(1, "CHIP", StockTier.MidValue, 100f, TrendDirection.Neutral, 0f);
            tech2.SetSector(StockSector.Tech);
            sectorStocks.Add(tech2);

            var energy1 = new StockInstance();
            energy1.Initialize(2, "SOLR", StockTier.MidValue, 100f, TrendDirection.Neutral, 0f);
            energy1.SetSector(StockSector.Energy);
            sectorStocks.Add(energy1);

            var effects = new EventEffects();
            effects.SetActiveStocks(sectorStocks);
            var scheduler = new EventScheduler(effects, new System.Random(42));

            scheduler.FireEvent(EventDefinitions.SectorRotation, sectorStocks);

            // Should fire events on multiple stocks (winners + losers)
            Assert.GreaterOrEqual(effects.ActiveEventCount, 2,
                "SectorRotation should create events for at least 2 stocks (winner + loser sectors)");
        }

        [Test]
        public void FireEvent_SectorRotation_WinnerPositiveLoserNegative()
        {
            var sectorStocks = new List<StockInstance>();
            var tech = new StockInstance();
            tech.Initialize(0, "NOVA", StockTier.MidValue, 100f, TrendDirection.Neutral, 0f);
            tech.SetSector(StockSector.Tech);
            sectorStocks.Add(tech);

            var energy = new StockInstance();
            energy.Initialize(1, "SOLR", StockTier.MidValue, 100f, TrendDirection.Neutral, 0f);
            energy.SetSector(StockSector.Energy);
            sectorStocks.Add(energy);

            var effects = new EventEffects();
            effects.SetActiveStocks(sectorStocks);
            var scheduler = new EventScheduler(effects, new System.Random(42));

            scheduler.FireEvent(EventDefinitions.SectorRotation, sectorStocks);

            // One stock should go up, one should go down
            // NOTE: GetActiveEventsForStock returns a shared buffer — extract values before the next call
            var events0 = effects.GetActiveEventsForStock(0);
            Assert.AreEqual(1, events0.Count, "Stock 0 should have one event");
            float effect0 = events0[0].PriceEffectPercent;

            var events1 = effects.GetActiveEventsForStock(1);
            Assert.AreEqual(1, events1.Count, "Stock 1 should have one event");
            float effect1 = events1[0].PriceEffectPercent;

            // One should be positive, one negative
            bool onePositiveOneNegative =
                (effect0 > 0 && effect1 < 0) ||
                (effect0 < 0 && effect1 > 0);

            Assert.IsTrue(onePositiveOneNegative,
                $"One sector should go up and the other down. Got: Stock0={effect0}, Stock1={effect1}");
        }

        [Test]
        public void FireEvent_SectorRotation_FallsBackToRandomSplit_WhenNoSectorDiversity()
        {
            // All stocks have the same sector — should fall back to random split
            var sameSecStocks = new List<StockInstance>();
            for (int i = 0; i < 4; i++)
            {
                var stock = new StockInstance();
                stock.Initialize(i, $"STK{i}", StockTier.MidValue, 100f, TrendDirection.Neutral, 0f);
                stock.SetSector(StockSector.Tech); // All same sector
                sameSecStocks.Add(stock);
            }

            var effects = new EventEffects();
            effects.SetActiveStocks(sameSecStocks);
            var scheduler = new EventScheduler(effects, new System.Random(42));

            scheduler.FireEvent(EventDefinitions.SectorRotation, sameSecStocks);

            // Should still create events via random split
            Assert.GreaterOrEqual(effects.ActiveEventCount, 2,
                "SectorRotation should still work via random split when no sector diversity");

            // Verify some go up and some go down
            bool hasPositive = false;
            bool hasNegative = false;
            for (int i = 0; i < sameSecStocks.Count; i++)
            {
                var events = effects.GetActiveEventsForStock(i);
                for (int j = 0; j < events.Count; j++)
                {
                    if (events[j].PriceEffectPercent > 0) hasPositive = true;
                    if (events[j].PriceEffectPercent < 0) hasNegative = true;
                }
            }
            Assert.IsTrue(hasPositive && hasNegative,
                "Random split should produce both positive and negative effects");
        }

        [Test]
        public void FireEvent_SectorRotation_PublishesSingleCombinedHeadline()
        {
            var sectorStocks = new List<StockInstance>();
            var tech = new StockInstance();
            tech.Initialize(0, "NOVA", StockTier.MidValue, 100f, TrendDirection.Neutral, 0f);
            tech.SetSector(StockSector.Tech);
            sectorStocks.Add(tech);

            var energy = new StockInstance();
            energy.Initialize(1, "SOLR", StockTier.MidValue, 100f, TrendDirection.Neutral, 0f);
            energy.SetSector(StockSector.Energy);
            sectorStocks.Add(energy);

            var effects = new EventEffects();
            effects.SetActiveStocks(sectorStocks);
            var scheduler = new EventScheduler(effects, new System.Random(42));

            int headlineCount = 0;
            EventBus.Subscribe<MarketEventFiredEvent>(e => headlineCount++);

            scheduler.FireEvent(EventDefinitions.SectorRotation, sectorStocks);

            Assert.AreEqual(1, headlineCount,
                "SectorRotation should publish exactly 1 combined headline, not one per stock");
            Assert.GreaterOrEqual(effects.ActiveEventCount, 2,
                "Individual stock events should still be active");
        }

        [Test]
        public void TierFiltering_PumpAndDump_OnlyPennyTier()
        {
            bool seenForPenny = false;
            bool seenForNonPenny = false;

            for (int i = 0; i < 500; i++)
            {
                var rng = new System.Random(i);
                var scheduler = new EventScheduler(_eventEffects, rng);

                if (scheduler.SelectEventType(StockTier.Penny).EventType == MarketEventType.PumpAndDump)
                    seenForPenny = true;

                foreach (var tier in new[] { StockTier.LowValue, StockTier.MidValue, StockTier.BlueChip })
                {
                    if (scheduler.SelectEventType(tier).EventType == MarketEventType.PumpAndDump)
                        seenForNonPenny = true;
                }
            }

            Assert.IsTrue(seenForPenny, "PumpAndDump should appear for Penny tier");
            Assert.IsFalse(seenForNonPenny, "PumpAndDump should NEVER appear for non-Penny tiers");
        }

        // --- Story 5-4: FlashCrash multi-phase tests ---

        [Test]
        public void FireEvent_FlashCrash_CreatesMultiPhaseEvent()
        {
            var lowStocks = new List<StockInstance>();
            var stock = new StockInstance();
            stock.Initialize(0, "ZAPP", StockTier.LowValue, 100f, TrendDirection.Neutral, 0f);
            lowStocks.Add(stock);

            var effects = new EventEffects();
            effects.SetActiveStocks(lowStocks);
            var scheduler = new EventScheduler(effects, new System.Random(42));

            scheduler.FireEvent(EventDefinitions.FlashCrash, lowStocks);

            var activeEvents = effects.GetActiveEventsForStock(0);
            Assert.AreEqual(1, activeEvents.Count);
            var evt = activeEvents[0];

            Assert.IsNotNull(evt.Phases, "FlashCrash should have phases");
            Assert.AreEqual(2, evt.Phases.Count, "FlashCrash should have 2 phases (crash + recovery)");
            Assert.Less(evt.Phases[0].TargetPricePercent, 0f, "Phase 0 (crash) should be negative");
            Assert.Greater(evt.Phases[1].TargetPricePercent, 0f, "Phase 1 (recovery) should be positive");
        }

        [Test]
        public void FireEvent_FlashCrash_VShapeTrajectory()
        {
            var lowStocks = new List<StockInstance>();
            var stock = new StockInstance();
            stock.Initialize(0, "ZAPP", StockTier.LowValue, 100f, TrendDirection.Neutral, 0f);
            lowStocks.Add(stock);

            var effects = new EventEffects();
            effects.SetActiveStocks(lowStocks);
            var scheduler = new EventScheduler(effects, new System.Random(42));

            scheduler.FireEvent(EventDefinitions.FlashCrash, lowStocks);

            var activeEvents = effects.GetActiveEventsForStock(0);
            var evt = activeEvents[0];

            float startPrice = stock.CurrentPrice;
            float dt = 0.05f;
            float lowestPrice = startPrice;
            float time = 0f;

            // Simulate full timeline
            while (time < evt.Duration - dt)
            {
                time += dt;
                evt.ElapsedTime = time;

                float price = effects.ApplyEventEffect(stock, evt, dt);
                if (price < lowestPrice)
                    lowestPrice = price;

                stock.CurrentPrice = price;
            }

            // V-shape: price should have dropped significantly during crash phase
            Assert.Less(lowestPrice, startPrice * 0.80f,
                $"Price should have crashed at least 20% during flash crash. Lowest={lowestPrice}, Start={startPrice}");

            // Final price should recover near original (within 15%)
            Assert.Greater(stock.CurrentPrice, startPrice * 0.80f,
                $"Price should recover near original after flash crash. Final={stock.CurrentPrice}, Start={startPrice}");
        }

        // --- Story 5-4: Short Squeeze portfolio-aware targeting ---

        [Test]
        public void FireEvent_ShortSqueeze_TargetsShortedStock()
        {
            var stocks = new List<StockInstance>();
            var stock0 = new StockInstance();
            stock0.Initialize(0, "SAFE", StockTier.MidValue, 100f, TrendDirection.Neutral, 0f);
            stocks.Add(stock0);

            var stock1 = new StockInstance();
            stock1.Initialize(1, "RISK", StockTier.MidValue, 50f, TrendDirection.Neutral, 0f);
            stocks.Add(stock1);

            // Create portfolio with a short using ticker symbol (production matching path)
            var portfolio = new Portfolio(10000f);
            portfolio.OpenShort("RISK", 100, 50f);

            var runContext = new RunContext(1, 1, portfolio);

            var effects = new EventEffects();
            effects.SetActiveStocks(stocks);
            var scheduler = new EventScheduler(effects, new System.Random(42));
            scheduler.SetRunContext(runContext);

            MarketEventFiredEvent received = default;
            EventBus.Subscribe<MarketEventFiredEvent>(e => received = e);

            scheduler.FireEvent(EventDefinitions.ShortSqueeze, stocks);

            Assert.IsNotNull(received.AffectedStockIds);
            Assert.AreEqual(1, received.AffectedStockIds[0],
                "Short Squeeze should target stock 1 (player's shorted stock matched by ticker)");
        }

        [Test]
        public void FireEvent_ShortSqueeze_TargetsLargestShort()
        {
            var stocks = new List<StockInstance>();
            var stock0 = new StockInstance();
            stock0.Initialize(0, "SMALL", StockTier.MidValue, 100f, TrendDirection.Neutral, 0f);
            stocks.Add(stock0);

            var stock1 = new StockInstance();
            stock1.Initialize(1, "BIG", StockTier.MidValue, 50f, TrendDirection.Neutral, 0f);
            stocks.Add(stock1);

            // Create portfolio with shorts using ticker symbols — "BIG" has more shares
            var portfolio = new Portfolio(50000f);
            portfolio.OpenShort("SMALL", 10, 100f);
            portfolio.OpenShort("BIG", 200, 50f);

            var runContext = new RunContext(1, 1, portfolio);

            var effects = new EventEffects();
            effects.SetActiveStocks(stocks);
            var scheduler = new EventScheduler(effects, new System.Random(42));
            scheduler.SetRunContext(runContext);

            MarketEventFiredEvent received = default;
            EventBus.Subscribe<MarketEventFiredEvent>(e => received = e);

            scheduler.FireEvent(EventDefinitions.ShortSqueeze, stocks);

            Assert.AreEqual(1, received.AffectedStockIds[0],
                "Short Squeeze should target stock with largest short position (BIG, 200 shares)");
        }

        [Test]
        public void FireEvent_ShortSqueeze_RandomTargetWhenNoShorts()
        {
            var stocks = new List<StockInstance>();
            var stock0 = new StockInstance();
            stock0.Initialize(0, "ABC", StockTier.MidValue, 100f, TrendDirection.Neutral, 0f);
            stocks.Add(stock0);

            var stock1 = new StockInstance();
            stock1.Initialize(1, "DEF", StockTier.MidValue, 50f, TrendDirection.Neutral, 0f);
            stocks.Add(stock1);

            // Portfolio with no shorts (only longs)
            var portfolio = new Portfolio(10000f);
            portfolio.OpenPosition("ABC", 10, 100f);

            var runContext = new RunContext(1, 1, portfolio);

            var effects = new EventEffects();
            effects.SetActiveStocks(stocks);
            var scheduler = new EventScheduler(effects, new System.Random(42));
            scheduler.SetRunContext(runContext);

            MarketEventFiredEvent received = default;
            EventBus.Subscribe<MarketEventFiredEvent>(e => received = e);

            scheduler.FireEvent(EventDefinitions.ShortSqueeze, stocks);

            Assert.IsNotNull(received.AffectedStockIds, "Should still target a stock");
            Assert.AreEqual(1, received.AffectedStockIds.Length);
            // Target should be one of the active stocks
            int target = received.AffectedStockIds[0];
            Assert.IsTrue(target == 0 || target == 1, "Target should be a valid active stock");
        }

        [Test]
        public void FireEvent_ShortSqueeze_DoesNotModifyPortfolio()
        {
            var stocks = new List<StockInstance>();
            var stock = new StockInstance();
            stock.Initialize(0, "TEST", StockTier.MidValue, 100f, TrendDirection.Neutral, 0f);
            stocks.Add(stock);

            var portfolio = new Portfolio(10000f);
            portfolio.OpenShort("TEST", 50, 100f);
            float cashBefore = portfolio.Cash;
            int posCountBefore = portfolio.PositionCount;

            var runContext = new RunContext(1, 1, portfolio);

            var effects = new EventEffects();
            effects.SetActiveStocks(stocks);
            var scheduler = new EventScheduler(effects, new System.Random(42));
            scheduler.SetRunContext(runContext);

            scheduler.FireEvent(EventDefinitions.ShortSqueeze, stocks);

            Assert.AreEqual(cashBefore, portfolio.Cash, 0.01f,
                "Short Squeeze targeting should not modify portfolio cash");
            Assert.AreEqual(posCountBefore, portfolio.PositionCount,
                "Short Squeeze targeting should not modify position count");
        }

        // --- Story 5-4: Rare event cap ---

        [Test]
        public void SelectEventType_RareEventCap_MaxOnePerRound()
        {
            // After InitializeRound, fire events and track rare event count
            int rareEventsInRound = 0;
            int totalEvents = 0;

            // Test across many seeds
            for (int seed = 0; seed < 200; seed++)
            {
                rareEventsInRound = 0;
                totalEvents = 0;

                var effects = new EventEffects();
                var scheduler = new EventScheduler(effects, new System.Random(seed));
                scheduler.InitializeRound(1, 1, StockTier.MidValue, _activeStocks, 60f);

                // Simulate selecting events for all slots
                for (int i = 0; i < scheduler.ScheduledEventCount; i++)
                {
                    var config = scheduler.SelectEventType(StockTier.MidValue);
                    if (config.Rarity <= 0.2f)
                        rareEventsInRound++;
                    totalEvents++;
                }

                Assert.LessOrEqual(rareEventsInRound, 1,
                    $"Seed {seed}: Should have at most 1 rare event per round, got {rareEventsInRound}");
            }
        }

        // --- Story 5-4: MarketCrash/BullRun end-to-end verification ---

        [Test]
        public void FireEvent_MarketCrash_AffectsAllActiveStocks()
        {
            var effects = new EventEffects();
            effects.SetActiveStocks(_activeStocks);
            var scheduler = new EventScheduler(effects, new System.Random(42));

            scheduler.FireEvent(EventDefinitions.MarketCrash, _activeStocks);

            // Both stocks should see the global event
            var stock0Events = effects.GetActiveEventsForStock(0);
            var stock1Events = effects.GetActiveEventsForStock(1);

            Assert.AreEqual(1, stock0Events.Count, "Stock 0 should be affected by MarketCrash");
            Assert.AreEqual(1, stock1Events.Count, "Stock 1 should be affected by MarketCrash");
            Assert.AreEqual(stock0Events[0], stock1Events[0],
                "Both stocks should reference the same global event");
        }

        [Test]
        public void FireEvent_BullRun_AffectsAllActiveStocksPositive()
        {
            var effects = new EventEffects();
            effects.SetActiveStocks(_activeStocks);
            var scheduler = new EventScheduler(effects, new System.Random(42));

            MarketEventFiredEvent received = default;
            EventBus.Subscribe<MarketEventFiredEvent>(e => received = e);

            scheduler.FireEvent(EventDefinitions.BullRunEvent, _activeStocks);

            Assert.IsTrue(received.IsPositive, "BullRun should be positive");
            Assert.Greater(received.PriceEffectPercent, 0f, "BullRun price effect should be positive");

            var stock0Events = effects.GetActiveEventsForStock(0);
            var stock1Events = effects.GetActiveEventsForStock(1);

            Assert.AreEqual(1, stock0Events.Count, "Stock 0 should be affected by BullRun");
            Assert.AreEqual(1, stock1Events.Count, "Stock 1 should be affected by BullRun");
        }

        // --- Story 5-4: Headline tests ---

        [Test]
        public void EventHeadlineData_AllFourGlobalEventTypesHaveMultipleHeadlines()
        {
            Assert.GreaterOrEqual(EventHeadlineData.MarketCrashHeadlines.Length, 3,
                "MarketCrash should have at least 3 headlines");
            Assert.GreaterOrEqual(EventHeadlineData.BullRunHeadlines.Length, 3,
                "BullRun should have at least 3 headlines");
            Assert.GreaterOrEqual(EventHeadlineData.FlashCrashHeadlines.Length, 3,
                "FlashCrash should have at least 3 headlines");
            Assert.GreaterOrEqual(EventHeadlineData.ShortSqueezeHeadlines.Length, 3,
                "ShortSqueeze should have at least 3 headlines");
        }

        [Test]
        public void TierFiltering_SectorRotation_OnlyMidAndBlue()
        {
            bool seenForMid = false;
            bool seenForBlue = false;
            bool seenForPennyOrLow = false;

            for (int i = 0; i < 500; i++)
            {
                var rng = new System.Random(i);
                var scheduler = new EventScheduler(_eventEffects, rng);

                if (scheduler.SelectEventType(StockTier.MidValue).EventType == MarketEventType.SectorRotation)
                    seenForMid = true;
                if (scheduler.SelectEventType(StockTier.BlueChip).EventType == MarketEventType.SectorRotation)
                    seenForBlue = true;
                if (scheduler.SelectEventType(StockTier.Penny).EventType == MarketEventType.SectorRotation)
                    seenForPennyOrLow = true;
                if (scheduler.SelectEventType(StockTier.LowValue).EventType == MarketEventType.SectorRotation)
                    seenForPennyOrLow = true;
            }

            Assert.IsTrue(seenForMid || seenForBlue, "SectorRotation should appear for Mid/Blue tiers");
            Assert.IsFalse(seenForPennyOrLow, "SectorRotation should NOT appear for Penny/Low tiers");
        }
    }
}

