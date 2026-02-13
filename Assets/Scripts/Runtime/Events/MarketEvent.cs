using System.Collections.Generic;

/// <summary>
/// Defines a single phase within a multi-phase market event.
/// Each phase has its own target price percent and duration.
/// </summary>
public class MarketEventPhase
{
    public readonly float TargetPricePercent;
    public readonly float PhaseDuration;

    public MarketEventPhase(float targetPricePercent, float phaseDuration)
    {
        TargetPricePercent = targetPricePercent;
        PhaseDuration = phaseDuration;
    }
}

/// <summary>
/// Runtime state for an active market event affecting stock prices.
/// Tracks elapsed time and provides interpolated force using a fast-attack curve:
/// rapid ramp to full force over first 15% of duration, hold, then brief tail-off.
/// Supports multi-phase events (e.g., Pump & Dump) with per-phase force curves.
/// </summary>
public class MarketEvent
{
    public MarketEventType EventType { get; private set; }
    public int? TargetStockId { get; private set; }
    public float PriceEffectPercent { get; private set; }
    public float Duration { get; private set; }
    private float _elapsedTime;
    private int _cachedPhaseIndex;
    private float _cachedPhaseElapsed = -1f;

    public float ElapsedTime
    {
        get => _elapsedTime;
        set
        {
            _elapsedTime = value;
            _cachedPhaseElapsed = -1f; // Invalidate cache
        }
    }

    public List<MarketEventPhase> Phases { get; private set; }
    public int CurrentPhaseIndex
    {
        get
        {
            if (_cachedPhaseElapsed == _elapsedTime)
                return _cachedPhaseIndex;

            _cachedPhaseElapsed = _elapsedTime;

            if (Phases == null || Phases.Count == 0)
            {
                _cachedPhaseIndex = 0;
                return 0;
            }

            float cumulative = 0f;
            for (int i = 0; i < Phases.Count; i++)
            {
                cumulative += Phases[i].PhaseDuration;
                if (_elapsedTime < cumulative)
                {
                    _cachedPhaseIndex = i;
                    return i;
                }
            }
            _cachedPhaseIndex = Phases.Count - 1;
            return _cachedPhaseIndex;
        }
    }

    public bool IsActive => ElapsedTime < Duration;
    public bool IsGlobalEvent => TargetStockId == null;

    public MarketEvent(MarketEventType eventType, int? targetStockId, float priceEffectPercent, float duration)
    {
        EventType = eventType;
        TargetStockId = targetStockId;
        PriceEffectPercent = priceEffectPercent;
        Duration = duration;
        ElapsedTime = 0f;
        Phases = null;
    }

    public MarketEvent(MarketEventType eventType, int? targetStockId, float priceEffectPercent, float duration, List<MarketEventPhase> phases)
    {
        EventType = eventType;
        TargetStockId = targetStockId;
        PriceEffectPercent = priceEffectPercent;
        Duration = duration;
        ElapsedTime = 0f;
        Phases = phases;
    }

    /// <summary>
    /// Returns the active phase's target price percent.
    /// For single-phase events, returns PriceEffectPercent.
    /// </summary>
    public float GetCurrentPhaseTarget()
    {
        if (Phases == null || Phases.Count == 0)
            return PriceEffectPercent;

        return Phases[CurrentPhaseIndex].TargetPricePercent;
    }

    /// <summary>
    /// Returns interpolated event force (0-1) using a fast-attack curve.
    /// For multi-phase events, each phase has its own independent force curve.
    /// </summary>
    public float GetCurrentForce()
    {
        if (ElapsedTime <= 0f || ElapsedTime >= Duration)
            return 0f;

        if (Phases != null && Phases.Count > 0)
            return GetMultiPhaseForce();

        return GetSinglePhaseForce(ElapsedTime, Duration);
    }

    private float GetMultiPhaseForce()
    {
        float cumulative = 0f;
        for (int i = 0; i < Phases.Count; i++)
        {
            float phaseStart = cumulative;
            cumulative += Phases[i].PhaseDuration;

            if (ElapsedTime < cumulative)
            {
                float phaseElapsed = ElapsedTime - phaseStart;
                // All phases in multi-phase events use ramp-and-hold (no tail-off).
                // The Lerp price model requires force to stay at 1.0 so the price
                // locks at the target rather than reverting as force drops.
                return GetRampAndHoldForce(phaseElapsed, Phases[i].PhaseDuration);
            }
        }
        return 0f;
    }

    /// <summary>
    /// Force curve without tail-off: ramp up over first 15% then hold at 1.0.
    /// Used for non-final phases so price peaks sharply at the phase boundary.
    /// </summary>
    private static float GetRampAndHoldForce(float elapsed, float duration)
    {
        if (elapsed <= 0f || elapsed >= duration)
            return 0f;

        float t = elapsed / duration;

        if (t <= 0.15f)
            return t / 0.15f;

        return 1f;
    }

    private static float GetSinglePhaseForce(float elapsed, float duration)
    {
        if (elapsed <= 0f || elapsed >= duration)
            return 0f;

        float t = elapsed / duration;

        if (t <= 0.15f)
        {
            return t / 0.15f;
        }
        else if (t <= 0.85f)
        {
            return 1f;
        }
        else
        {
            return 1f - (t - 0.85f) / 0.15f;
        }
    }
}
