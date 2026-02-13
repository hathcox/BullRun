using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Schedules and fires market events during trading rounds.
/// Determines WHEN and WHAT events fire — delegates actual price effects to EventEffects.
/// Pure C# class (no MonoBehaviour) for testability.
/// </summary>
public class EventScheduler
{
    private readonly EventEffects _eventEffects;
    private readonly System.Random _random;

    private float[] _scheduledFireTimes;
    private bool[] _firedSlots;
    private int _eventCount;

    public int ScheduledEventCount => _eventCount;
    public EventEffects EventEffects => _eventEffects;

    public EventScheduler(EventEffects eventEffects)
    {
        _eventEffects = eventEffects;
        _random = new System.Random();
    }

    /// <summary>
    /// Constructor with explicit Random for deterministic testing.
    /// </summary>
    public EventScheduler(EventEffects eventEffects, System.Random random)
    {
        _eventEffects = eventEffects;
        _random = random;
    }

    /// <summary>
    /// Initializes event schedule for a new round.
    /// Determines event count based on act and pre-schedules fire times.
    /// Event types are selected at fire time to keep events unpredictable.
    /// </summary>
    public void InitializeRound(int round, int act, StockTier tier, IReadOnlyList<StockInstance> activeStocks, float roundDuration)
    {
        // Determine event count based on act (early vs late)
        bool isLateRound = (act >= 3);
        int minEvents = isLateRound ? EventSchedulerConfig.MinEventsLateRounds : EventSchedulerConfig.MinEventsEarlyRounds;
        int maxEvents = isLateRound ? EventSchedulerConfig.MaxEventsLateRounds : EventSchedulerConfig.MaxEventsEarlyRounds;

        // Apply tier frequency modifier
        float frequencyModifier = StockTierData.GetTierConfig(tier).EventFrequencyModifier;

        // Scale event count by frequency modifier (round to nearest int, clamp to min 1)
        float scaledCount = _random.Next(minEvents, maxEvents + 1) * frequencyModifier;
        _eventCount = Mathf.Max(1, Mathf.RoundToInt(scaledCount));

        // Pre-schedule fire times distributed across the round
        _scheduledFireTimes = new float[_eventCount];
        _firedSlots = new bool[_eventCount];

        float windowStart = EventSchedulerConfig.EarlyBufferSeconds;
        float windowEnd = roundDuration - EventSchedulerConfig.LateBufferSeconds;

        // Safety: if round is too short for buffers, use full duration
        if (windowEnd <= windowStart)
        {
            windowStart = 0f;
            windowEnd = roundDuration;
        }

        float segmentLength = (windowEnd - windowStart) / _eventCount;

        for (int i = 0; i < _eventCount; i++)
        {
            float segStart = windowStart + (i * segmentLength);
            float segEnd = segStart + segmentLength;
            _scheduledFireTimes[i] = RandomRange(segStart, segEnd);
            _firedSlots[i] = false;
        }

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[EventScheduler] Round {round} (Act {act}, {tier}): {_eventCount} events scheduled, freq modifier {frequencyModifier:F1}");
        #endif
    }

    /// <summary>
    /// Per-frame update. Checks if any scheduled events should fire based on elapsed time.
    /// Also advances active event timers via EventEffects.
    /// Must be called BEFORE PriceGenerator.UpdatePrice().
    /// </summary>
    public void Update(float elapsedTime, float deltaTime, IReadOnlyList<StockInstance> activeStocks, StockTier tier)
    {
        // Check for events that should fire
        if (_scheduledFireTimes != null)
        {
            for (int i = 0; i < _eventCount; i++)
            {
                if (!_firedSlots[i] && elapsedTime >= _scheduledFireTimes[i])
                {
                    _firedSlots[i] = true;
                    var config = SelectEventType(tier);
                    FireEvent(config, activeStocks);
                }
            }
        }

        // Advance active event timers
        _eventEffects.UpdateActiveEvents(deltaTime);
    }

    /// <summary>
    /// Selects an event type using rarity-weighted random selection from tier-available events.
    /// </summary>
    public MarketEventConfig SelectEventType(StockTier tier)
    {
        var available = EventDefinitions.GetEventsForTier(tier);

        // Sum rarities for weight normalization
        float totalWeight = 0f;
        for (int i = 0; i < available.Count; i++)
        {
            totalWeight += available[i].Rarity;
        }

        // Weighted random selection
        float roll = (float)_random.NextDouble() * totalWeight;
        float cumulative = 0f;
        for (int i = 0; i < available.Count; i++)
        {
            cumulative += available[i].Rarity;
            if (roll <= cumulative)
                return available[i];
        }

        // Fallback (should never reach here)
        return available[available.Count - 1];
    }

    /// <summary>
    /// Creates a MarketEvent instance and fires it via EventEffects.StartEvent().
    /// Global events (MarketCrash, BullRun) target all stocks (null TargetStockId).
    /// Stock-specific events target a random stock from the active list.
    /// </summary>
    public void FireEvent(MarketEventConfig config, IReadOnlyList<StockInstance> activeStocks)
    {
        // Determine targeting
        int? targetStockId = null;
        bool isGlobal = (config.EventType == MarketEventType.MarketCrash ||
                         config.EventType == MarketEventType.BullRun);

        if (!isGlobal && activeStocks.Count > 0)
        {
            int stockIndex = _random.Next(activeStocks.Count);
            targetStockId = activeStocks[stockIndex].StockId;
        }

        // Roll price effect between min and max
        float priceEffect = RandomRange(config.MinPriceEffect, config.MaxPriceEffect);

        MarketEvent evt;

        if (config.EventType == MarketEventType.PumpAndDump)
        {
            // Multi-phase: Pump (+50-100%) then Dump (crash below starting price)
            // Phase 0: 60% of duration — pump to rolled effect
            // Phase 1: 40% of duration — crash from pump peak to 80% of original (20% below start)
            float pumpDuration = config.Duration * 0.6f;
            float dumpDuration = config.Duration * 0.4f;

            // From pump peak, calculate dump target to land at 80% of original price
            // Pump peak = startPrice * (1 + priceEffect)
            // End target = startPrice * 0.80
            // Phase 1 percent = endTarget / pumpPeak - 1 = 0.80 / (1 + priceEffect) - 1
            float dumpTarget = 0.80f / (1f + priceEffect) - 1f;

            var phases = new List<MarketEventPhase>
            {
                new MarketEventPhase(priceEffect, pumpDuration),
                new MarketEventPhase(dumpTarget, dumpDuration)
            };

            evt = new MarketEvent(config.EventType, targetStockId, priceEffect, config.Duration, phases);
        }
        else if (config.EventType == MarketEventType.SectorRotation)
        {
            // Sector-aware multi-stock targeting handled separately
            FireSectorRotation(config, activeStocks);
            return;
        }
        else
        {
            evt = new MarketEvent(config.EventType, targetStockId, priceEffect, config.Duration);
        }

        _eventEffects.StartEvent(evt);
    }

    private void FireSectorRotation(MarketEventConfig config, IReadOnlyList<StockInstance> activeStocks)
    {
        if (activeStocks.Count < 2)
            return;

        float rotationPercent = RandomRange(0.10f, config.MaxPriceEffect);

        // Group stocks by sector
        var sectorGroups = new Dictionary<StockSector, List<StockInstance>>();
        for (int i = 0; i < activeStocks.Count; i++)
        {
            var stock = activeStocks[i];
            var sector = stock.Sector;
            if (sector == StockSector.None)
                continue;

            if (!sectorGroups.ContainsKey(sector))
                sectorGroups[sector] = new List<StockInstance>();
            sectorGroups[sector].Add(stock);
        }

        List<StockInstance> winnerStocks;
        List<StockInstance> loserStocks;

        if (sectorGroups.Count >= 2)
        {
            // Pick two random sectors
            var sectors = new List<StockSector>(sectorGroups.Keys);
            int idx1 = _random.Next(sectors.Count);
            int idx2;
            do { idx2 = _random.Next(sectors.Count); } while (idx2 == idx1);

            winnerStocks = sectorGroups[sectors[idx1]];
            loserStocks = sectorGroups[sectors[idx2]];
        }
        else
        {
            // Fallback: random split
            var shuffled = new List<StockInstance>(activeStocks);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                var temp = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = temp;
            }

            int half = shuffled.Count / 2;
            winnerStocks = shuffled.GetRange(0, half);
            loserStocks = shuffled.GetRange(half, shuffled.Count - half);
        }

        // Collect all affected stock IDs and tickers for the combined headline
        var allAffectedIds = new List<int>();
        var allAffectedTickers = new List<string>();

        // Fire positive events on winner stocks (silent — no individual headlines)
        for (int i = 0; i < winnerStocks.Count; i++)
        {
            var evt = new MarketEvent(config.EventType, winnerStocks[i].StockId, rotationPercent, config.Duration);
            _eventEffects.StartEventSilent(evt);
            allAffectedIds.Add(winnerStocks[i].StockId);
            allAffectedTickers.Add(winnerStocks[i].TickerSymbol);
        }

        // Fire negative events on loser stocks (silent — no individual headlines)
        for (int i = 0; i < loserStocks.Count; i++)
        {
            var evt = new MarketEvent(config.EventType, loserStocks[i].StockId, -rotationPercent, config.Duration);
            _eventEffects.StartEventSilent(evt);
            allAffectedIds.Add(loserStocks[i].StockId);
            allAffectedTickers.Add(loserStocks[i].TickerSymbol);
        }

        // Publish one combined headline for the entire sector rotation
        string winnerTicker = winnerStocks.Count > 0 ? winnerStocks[0].TickerSymbol : "the market";
        string headline = EventHeadlineData.GetHeadline(config.EventType, winnerTicker, new System.Random(_random.Next()));

        EventBus.Publish(new MarketEventFiredEvent
        {
            EventType = config.EventType,
            AffectedStockIds = allAffectedIds.ToArray(),
            PriceEffectPercent = rotationPercent,
            Headline = headline,
            AffectedTickerSymbols = allAffectedTickers.ToArray(),
            IsPositive = EventHeadlineData.IsPositiveEvent(config.EventType),
            Duration = config.Duration
        });

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[Events] SectorRotation fired: {winnerStocks.Count} winners, {loserStocks.Count} losers ({rotationPercent:+0.0%;-0.0%} over {config.Duration}s)");
        #endif
    }

    /// <summary>
    /// Returns the number of events that have already fired this round.
    /// </summary>
    public int FiredEventCount
    {
        get
        {
            if (_firedSlots == null) return 0;
            int count = 0;
            for (int i = 0; i < _firedSlots.Length; i++)
            {
                if (_firedSlots[i]) count++;
            }
            return count;
        }
    }

    /// <summary>
    /// Returns the scheduled fire time for a specific slot (for testing).
    /// </summary>
    public float GetScheduledTime(int index)
    {
        return _scheduledFireTimes[index];
    }

    private float RandomRange(float min, float max)
    {
        return min + (float)_random.NextDouble() * (max - min);
    }
}
