/// <summary>
/// Runtime state for an active market event affecting stock prices.
/// Tracks elapsed time and provides interpolated force that ramps up then fades.
/// </summary>
public class MarketEvent
{
    public MarketEventType EventType { get; private set; }
    public int? TargetStockId { get; private set; }
    public float PriceEffectPercent { get; private set; }
    public float Duration { get; private set; }
    public float ElapsedTime { get; set; }

    public bool IsActive => ElapsedTime < Duration;
    public bool IsGlobalEvent => TargetStockId == null;

    public MarketEvent(MarketEventType eventType, int? targetStockId, float priceEffectPercent, float duration)
    {
        EventType = eventType;
        TargetStockId = targetStockId;
        PriceEffectPercent = priceEffectPercent;
        Duration = duration;
        ElapsedTime = 0f;
    }

    /// <summary>
    /// Returns interpolated event force (0-1) that ramps up to peak at midpoint then fades.
    /// Uses a triangle wave: linearly rises to 1.0 at midpoint, linearly falls to 0.0 at end.
    /// </summary>
    public float GetCurrentForce()
    {
        if (ElapsedTime <= 0f || ElapsedTime >= Duration)
            return 0f;

        float halfDuration = Duration * 0.5f;

        if (ElapsedTime <= halfDuration)
        {
            // Ramp up: 0 → 1 over first half
            return ElapsedTime / halfDuration;
        }
        else
        {
            // Fade out: 1 → 0 over second half
            return 1f - (ElapsedTime - halfDuration) / halfDuration;
        }
    }
}
