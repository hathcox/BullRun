/// <summary>
/// Runtime state for an active market event affecting stock prices.
/// Tracks elapsed time and provides interpolated force using a fast-attack curve:
/// rapid ramp to full force over first 15% of duration, hold, then brief tail-off.
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
    /// Returns interpolated event force (0-1) using a fast-attack curve:
    /// - First 15% of duration: ramp 0→1 (steep visible ramp)
    /// - Middle 70%: hold at 1.0
    /// - Last 15%: tail off 1→0
    /// </summary>
    public float GetCurrentForce()
    {
        if (ElapsedTime <= 0f || ElapsedTime >= Duration)
            return 0f;

        float t = ElapsedTime / Duration;

        if (t <= 0.15f)
        {
            // Fast ramp up: 0→1 over first 15%
            return t / 0.15f;
        }
        else if (t <= 0.85f)
        {
            // Hold at full force
            return 1f;
        }
        else
        {
            // Tail off: 1→0 over last 15%
            return 1f - (t - 0.85f) / 0.15f;
        }
    }
}
