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
    private int _rareEventsFiredThisRound;

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
        _rareEventsFiredThisRound = 0;
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
    /// If a rare event (rarity &lt;= 0.2) has already been scheduled this round,
    /// excludes rare events to cap at maximum 1 rare event per round.
    /// </summary>
    public MarketEventConfig SelectEventType(StockTier tier)
    {
        var allAvailable = EventDefinitions.GetEventsForTier(tier);

        // Filter out rare events if max rare events already fired this round
        List<MarketEventConfig> available;
        if (_rareEventsFiredThisRound >= EventSchedulerConfig.MaxRareEventsPerRound)
        {
            available = new List<MarketEventConfig>();
            for (int i = 0; i < allAvailable.Count; i++)
            {
                if (allAvailable[i].Rarity > 0.2f)
                    available.Add(allAvailable[i]);
            }

            // If all events for this tier are rare, fall back to full list
            if (available.Count == 0)
                available = new List<MarketEventConfig>(allAvailable);
        }
        else
        {
            available = allAvailable;
        }

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
            {
                // Track rare event scheduling
                if (available[i].Rarity <= 0.2f)
                    _rareEventsFiredThisRound++;
                return available[i];
            }
        }

        // Fallback (should never reach here)
        var fallback = available[available.Count - 1];
        if (fallback.Rarity <= 0.2f)
            _rareEventsFiredThisRound++;
        return fallback;
    }

    /// <summary>
    /// Creates a MarketEvent instance and fires it via EventEffects.StartEvent().
    /// Targets a randomly selected active stock (Story 13.7: multi-stock support).
    /// </summary>
    public void FireEvent(MarketEventConfig config, IReadOnlyList<StockInstance> activeStocks)
    {
        if (activeStocks.Count == 0)
            return;

        // Target a random active stock (supports 1 or 2+ stocks)
        int targetIndex = activeStocks.Count == 1 ? 0 : _random.Next(activeStocks.Count);
        int targetStockId = activeStocks[targetIndex].StockId;

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
        else if (config.EventType == MarketEventType.FlashCrash)
        {
            // Multi-phase V-shape: crash then recover
            // Phase 0: price drops by rolled effect over first ~40% of duration
            // Phase 1: price recovers ~90% of the drop over remaining ~60% of duration
            float crashDuration = config.Duration * 0.4f;
            float recoveryDuration = config.Duration * 0.6f;

            // Recovery target: from crash bottom, recover 90% of the drop
            // Crash bottom = startPrice * (1 + priceEffect) where priceEffect is negative
            // Recovery end = crashBottom * (1 + recoveryPercent)
            // We want to end at ~95% of original: 0.95 / (1 + priceEffect) - 1
            float recoveryTarget = 0.95f / (1f + priceEffect) - 1f;

            var phases = new List<MarketEventPhase>
            {
                new MarketEventPhase(priceEffect, crashDuration),
                new MarketEventPhase(recoveryTarget, recoveryDuration)
            };

            evt = new MarketEvent(config.EventType, targetStockId, priceEffect, config.Duration, phases);
        }
        else if (config.EventType == MarketEventType.SectorRotation)
        {
            // Single-stock directional effect (FIX-9: no multi-stock sector splitting)
            FireSectorRotation(config, activeStocks);
            return;
        }
        else
        {
            evt = new MarketEvent(config.EventType, targetStockId, priceEffect, config.Duration);
        }

        _eventEffects.StartEvent(evt);
    }

    /// <summary>
    /// Fires a sector rotation effect. With 1 stock: random direction.
    /// With 2+ stocks: opposite effects on different stocks (Story 13.7).
    /// </summary>
    private void FireSectorRotation(MarketEventConfig config, IReadOnlyList<StockInstance> activeStocks)
    {
        if (activeStocks.Count == 0)
            return;

        float rotationPercent = RandomRange(Mathf.Abs(config.MinPriceEffect), config.MaxPriceEffect);

        if (activeStocks.Count == 1)
        {
            // Single stock: random direction
            bool isPositive = _random.NextDouble() >= 0.5;
            float effect = isPositive ? rotationPercent : -rotationPercent;
            var evt = new MarketEvent(config.EventType, activeStocks[0].StockId, effect, config.Duration);
            _eventEffects.StartEvent(evt);

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Events] SectorRotation fired on {activeStocks[0].TickerSymbol}: {effect:+0.0%;-0.0%} over {config.Duration}s");
            #endif
        }
        else
        {
            // Multi-stock: first stock positive, second stock negative (rotation between them)
            var evtUp = new MarketEvent(config.EventType, activeStocks[0].StockId, rotationPercent, config.Duration);
            var evtDown = new MarketEvent(config.EventType, activeStocks[1].StockId, -rotationPercent, config.Duration);
            _eventEffects.StartEvent(evtUp);
            _eventEffects.StartEvent(evtDown);

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Events] SectorRotation: {activeStocks[0].TickerSymbol} +{rotationPercent:P0}, {activeStocks[1].TickerSymbol} -{rotationPercent:P0} over {config.Duration}s");
            #endif
        }
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
