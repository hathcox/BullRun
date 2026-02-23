/// <summary>
/// Data structs for the store system (Epic 13).
/// Plain C# structs — no MonoBehaviours.
/// </summary>

/// <summary>
/// Records a single bond purchase for history tracking.
/// </summary>
public struct BondRecord
{
    public int RoundPurchased;
    public float PricePaid;

    public BondRecord(int roundPurchased, float pricePaid)
    {
        RoundPurchased = roundPurchased;
        PricePaid = pricePaid;
    }
}

/// <summary>
/// Tracks a revealed insider tip during a shop visit.
/// Cleared when shop closes.
/// </summary>
public struct RevealedTip
{
    public InsiderTipType Type;
    public string DisplayText;
    public float NumericValue;
    public bool IsActivated;

    public RevealedTip(InsiderTipType type, string displayText, float numericValue = 0f)
    {
        Type = type;
        DisplayText = displayText;
        NumericValue = numericValue;
        IsActivated = false;
    }
}

/// <summary>
/// Overlay geometry data for rendering tip information on the trading chart (Story 18.1).
/// Each field uses a sentinel value when not applicable for its tip type.
/// </summary>
public struct TipOverlayData
{
    public InsiderTipType Type;
    public string Label;            // Display label for overlay (e.g., "FLOOR ~$3.20")

    // Horizontal line overlays (PriceFloor, PriceCeiling)
    public float PriceLevel;        // 0 = not applicable

    // Horizontal band overlay (PriceForecast)
    public float BandCenter;        // 0 = not applicable
    public float BandHalfWidth;     // 0 = not applicable

    // Time zone overlays (DipMarker, PeakMarker) — normalized 0-1
    public float TimeZoneCenter;    // -1 = not applicable
    public float TimeZoneHalfWidth; // 0 = not applicable

    // Vertical time markers (EventTiming) — normalized 0-1
    public float[] TimeMarkers;     // null = not applicable

    // Trend reversal marker — normalized 0-1
    public float ReversalTime;      // -1 = no reversal expected

    // Direction arrow (ClosingDirection)
    public int DirectionSign;       // +1 = higher, -1 = lower, 0 = not applicable

    // Live counter (EventCount)
    public int EventCountdown;      // -1 = not applicable

    // Event fire times in absolute seconds from round start (EventCount)
    // Sorted chronologically. Used by TipPanel for next-event countdown display.
    public float[] EventFireTimes;  // null = not applicable

    /// <summary>
    /// Creates a TipOverlayData with correct sentinel values for "not applicable" fields.
    /// Use this instead of default constructor, since C# struct defaults zero all fields.
    /// </summary>
    public static TipOverlayData CreateDefault()
    {
        return new TipOverlayData
        {
            TimeZoneCenter = -1f,
            ReversalTime = -1f,
            EventCountdown = -1
        };
    }
}

/// <summary>
/// Types of insider tips that can be revealed in the shop (Story 13.5).
/// </summary>
public enum InsiderTipType
{
    PriceForecast,
    PriceFloor,
    PriceCeiling,
    EventCount,
    DipMarker,
    PeakMarker,
    ClosingDirection,
    EventTiming,
    TrendReversal
}
