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
    public string RevealedText;

    public RevealedTip(InsiderTipType type, string revealedText)
    {
        Type = type;
        RevealedText = revealedText;
    }
}

/// <summary>
/// Types of insider tips that can be revealed in the shop.
/// </summary>
public enum InsiderTipType
{
    PriceDirection,
    EventWarning,
    SectorTrend
}
