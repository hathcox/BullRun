using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Story 18.6: Pre-decided event data computed at round start.
/// Contains the full event plan (type, effect, phases) for each scheduled slot.
/// </summary>
public struct PreDecidedEvent
{
    public float FireTime;
    public MarketEventConfig Config;
    public float PriceEffect;
    public bool IsPositive;
    public List<MarketEventPhase> Phases;

    public PreDecidedEvent(float fireTime, MarketEventConfig config, float priceEffect, bool isPositive, List<MarketEventPhase> phases = null)
    {
        FireTime = fireTime;
        Config = config;
        PriceEffect = priceEffect;
        IsPositive = isPositive;
        Phases = phases;
    }
}

/// <summary>
/// Schedules and fires market events during trading rounds.
/// Determines WHEN and WHAT events fire — delegates actual price effects to EventEffects.
/// Pure C# class (no MonoBehaviour) for testability.
/// Story 18.6: Pre-decides event types and effects at InitializeRound() time.
/// </summary>
public class EventScheduler
{
    private readonly EventEffects _eventEffects;
    private readonly System.Random _random;

    private bool[] _firedSlots;
    private int _eventCount;
    private int _rareEventsFiredThisRound;

    // Story 18.6: Pre-decided events with types and effects computed at round start
    private PreDecidedEvent[] _preDecidedEvents;

    // Story 17.4: Relic multipliers — reset by TradingState before RoundStartedEvent, set by relics after dispatch,
    // then used by InitializeRound (EventCountMultiplier) and FireEvent (ImpactMultiplier, PositiveImpactMultiplier)
    public float EventCountMultiplier { get; set; } = 1.0f;
    public float ImpactMultiplier { get; set; } = 1.0f;
    public float PositiveImpactMultiplier { get; set; } = 1.0f;

    // Story 17.4: Stored for ForceFireRandomEvent
    private StockTier _currentTier;
    private IReadOnlyList<StockInstance> _currentActiveStocks;

    public int ScheduledEventCount => _eventCount;
    public EventEffects EventEffects => _eventEffects;

    /// <summary>
    /// Story 18.6: Pre-decided event plan for the current round.
    /// Available after InitializeRound() for use by TipActivator.
    /// </summary>
    public PreDecidedEvent[] PreDecidedEvents => _preDecidedEvents;

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
    /// Determines event count, pre-schedules fire times, and pre-decides event types + effects.
    /// Story 18.6: All event data is now determined at init time for tip accuracy.
    /// </summary>
    public void InitializeRound(int round, int act, StockTier tier, IReadOnlyList<StockInstance> activeStocks, float roundDuration)
    {
        // Clear stale events from previous round to prevent cached prices corrupting new stocks
        _eventEffects.ClearAllEvents();

        // Story 17.4: Store for ForceFireRandomEvent
        _currentTier = tier;
        _currentActiveStocks = activeStocks;

        _rareEventsFiredThisRound = 0;
        // Determine event count based on act (early vs late)
        bool isLateRound = (act >= 3);
        int minEvents = isLateRound ? EventSchedulerConfig.MinEventsLateRounds : EventSchedulerConfig.MinEventsEarlyRounds;
        int maxEvents = isLateRound ? EventSchedulerConfig.MaxEventsLateRounds : EventSchedulerConfig.MaxEventsEarlyRounds;

        // Apply tier frequency modifier
        float frequencyModifier = StockTierData.GetTierConfig(tier).EventFrequencyModifier;

        // Scale event count by frequency modifier and relic multiplier (round to nearest int, clamp to min 1)
        float scaledCount = _random.Next(minEvents, maxEvents + 1) * frequencyModifier * EventCountMultiplier;
        _eventCount = Mathf.Max(1, Mathf.RoundToInt(scaledCount));

        // Pre-schedule fire times and pre-decide events
        _preDecidedEvents = new PreDecidedEvent[_eventCount];
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
            float fireTime = RandomRange(segStart, segEnd);

            // Story 18.6: Pre-decide event type and price effect at init time
            var config = SelectEventType(tier);

            float priceEffect;
            bool isPositive;
            if (config.EventType == MarketEventType.SectorRotation)
            {
                // SectorRotation: random magnitude with random direction
                float rotationPercent = RandomRange(Mathf.Abs(config.MinPriceEffect), config.MaxPriceEffect) * ImpactMultiplier;
                bool dirPositive = _random.NextDouble() >= 0.5;
                priceEffect = dirPositive ? rotationPercent : -rotationPercent;
                isPositive = EventHeadlineData.IsPositiveEvent(config.EventType);
            }
            else
            {
                priceEffect = RandomRange(config.MinPriceEffect, config.MaxPriceEffect) * ImpactMultiplier;
                isPositive = EventHeadlineData.IsPositiveEvent(config.EventType);
                if (isPositive)
                    priceEffect *= PositiveImpactMultiplier;
            }

            // Pre-compute phases for multi-phase events
            List<MarketEventPhase> phases = null;
            if (config.EventType == MarketEventType.PumpAndDump)
            {
                phases = BuildPumpAndDumpPhases(priceEffect, config.Duration);
            }
            else if (config.EventType == MarketEventType.FlashCrash)
            {
                phases = BuildFlashCrashPhases(priceEffect, config.Duration);
            }

            _preDecidedEvents[i] = new PreDecidedEvent(fireTime, config, priceEffect, isPositive, phases);
            _firedSlots[i] = false;
        }

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[EventScheduler] Round {round} (Act {act}, {tier}): {_eventCount} events pre-decided, freq modifier {frequencyModifier:F1}");
        #endif
    }

    /// <summary>
    /// Per-frame update. Checks if any scheduled events should fire based on elapsed time.
    /// Also advances active event timers via EventEffects.
    /// Must be called BEFORE PriceGenerator.UpdatePrice().
    /// Story 18.6: Uses pre-decided event data instead of rolling at fire time.
    /// </summary>
    public void Update(float elapsedTime, float deltaTime, IReadOnlyList<StockInstance> activeStocks, StockTier tier)
    {
        // Check for events that should fire using pre-decided data
        if (_preDecidedEvents != null)
        {
            for (int i = 0; i < _eventCount; i++)
            {
                if (!_firedSlots[i] && elapsedTime >= _preDecidedEvents[i].FireTime)
                {
                    _firedSlots[i] = true;
                    FirePreDecidedEvent(_preDecidedEvents[i], activeStocks);
                }
            }
        }

        // Advance active event timers
        _eventEffects.UpdateActiveEvents(deltaTime);
    }

    /// <summary>
    /// Story 17.4: Immediately fires a random market event on the active stock.
    /// Uses existing SelectEventType and FireEvent logic for normal event processing.
    /// Called by relics (Catalyst Trader, Loss Liquidator) to trigger events on trade.
    /// </summary>
    public void ForceFireRandomEvent()
    {
        if (_currentActiveStocks == null || _currentActiveStocks.Count == 0) return;

        var config = SelectEventType(_currentTier);
        FireEvent(config, _currentActiveStocks);
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
    /// FIX-15: Always targets activeStocks[0] — single stock per round is permanent.
    /// </summary>
    public void FireEvent(MarketEventConfig config, IReadOnlyList<StockInstance> activeStocks)
    {
        if (activeStocks.Count == 0)
            return;

        // FIX-15: Always target the single active stock
        int targetStockId = activeStocks[0].StockId;

        // Roll price effect between min and max, apply relic multipliers (Story 17.4)
        float priceEffect = RandomRange(config.MinPriceEffect, config.MaxPriceEffect) * ImpactMultiplier;
        if (EventHeadlineData.IsPositiveEvent(config.EventType))
            priceEffect *= PositiveImpactMultiplier;

        MarketEvent evt;

        if (config.EventType == MarketEventType.PumpAndDump)
        {
            var phases = BuildPumpAndDumpPhases(priceEffect, config.Duration);
            evt = new MarketEvent(config.EventType, targetStockId, priceEffect, config.Duration, phases);
        }
        else if (config.EventType == MarketEventType.FlashCrash)
        {
            var phases = BuildFlashCrashPhases(priceEffect, config.Duration);
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
    /// Fires a sector rotation effect on the single active stock with random direction.
    /// FIX-15: Multi-stock branch removed — single stock per round is permanent.
    /// </summary>
    private void FireSectorRotation(MarketEventConfig config, IReadOnlyList<StockInstance> activeStocks)
    {
        if (activeStocks.Count == 0)
            return;

        float rotationPercent = RandomRange(Mathf.Abs(config.MinPriceEffect), config.MaxPriceEffect) * ImpactMultiplier;

        // Single stock: random direction
        bool isPositive = _random.NextDouble() >= 0.5;
        float effect = isPositive ? rotationPercent : -rotationPercent;
        var evt = new MarketEvent(config.EventType, activeStocks[0].StockId, effect, config.Duration);
        _eventEffects.StartEvent(evt);

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[Events] SectorRotation fired on {activeStocks[0].TickerSymbol}: {effect:+0.0%;-0.0%} over {config.Duration}s");
        #endif
    }

    /// <summary>
    /// Story 18.6: Fires an event from pre-decided data, constructing MarketEvent
    /// from the already-rolled type, effect, and phases.
    /// </summary>
    private void FirePreDecidedEvent(PreDecidedEvent preDecided, IReadOnlyList<StockInstance> activeStocks)
    {
        if (activeStocks.Count == 0) return;

        int targetStockId = activeStocks[0].StockId;

        if (preDecided.Config.EventType == MarketEventType.SectorRotation)
        {
            // SectorRotation uses the pre-decided effect directly (already has random direction)
            var evt = new MarketEvent(preDecided.Config.EventType, targetStockId, preDecided.PriceEffect, preDecided.Config.Duration);
            _eventEffects.StartEvent(evt);
            return;
        }

        MarketEvent marketEvent;
        if (preDecided.Phases != null)
        {
            marketEvent = new MarketEvent(preDecided.Config.EventType, targetStockId, preDecided.PriceEffect, preDecided.Config.Duration, preDecided.Phases);
        }
        else
        {
            marketEvent = new MarketEvent(preDecided.Config.EventType, targetStockId, preDecided.PriceEffect, preDecided.Config.Duration);
        }

        _eventEffects.StartEvent(marketEvent);
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
        return _preDecidedEvents[index].FireTime;
    }

    /// <summary>
    /// Builds phase data for a PumpAndDump event.
    /// Shared by InitializeRound (pre-decided) and FireEvent (forced events).
    /// </summary>
    private static List<MarketEventPhase> BuildPumpAndDumpPhases(float priceEffect, float duration)
    {
        float pumpDuration = duration * 0.6f;
        float dumpDuration = duration * 0.4f;
        float dumpTarget = 0.80f / (1f + priceEffect) - 1f;
        return new List<MarketEventPhase>
        {
            new MarketEventPhase(priceEffect, pumpDuration),
            new MarketEventPhase(dumpTarget, dumpDuration)
        };
    }

    /// <summary>
    /// Builds phase data for a FlashCrash event.
    /// Shared by InitializeRound (pre-decided) and FireEvent (forced events).
    /// </summary>
    private static List<MarketEventPhase> BuildFlashCrashPhases(float priceEffect, float duration)
    {
        float crashDuration = duration * 0.4f;
        float recoveryDuration = duration * 0.6f;
        float recoveryTarget = 0.95f / (1f + priceEffect) - 1f;
        return new List<MarketEventPhase>
        {
            new MarketEventPhase(priceEffect, crashDuration),
            new MarketEventPhase(recoveryTarget, recoveryDuration)
        };
    }

    private float RandomRange(float min, float max)
    {
        return min + (float)_random.NextDouble() * (max - min);
    }
}
