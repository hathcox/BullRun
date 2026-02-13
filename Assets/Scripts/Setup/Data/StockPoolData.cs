using System.Collections.Generic;

/// <summary>
/// Optional sector tag for stocks. Used by the event system (Epic 5) for
/// sector correlation — multiple stocks in the same sector react to sector events.
/// </summary>
public enum StockSector
{
    None,
    Tech,
    Energy,
    Health,
    Finance,
    Consumer,
    Industrial,
    Crypto
}

/// <summary>
/// Definition of a single stock available for selection into rounds.
/// </summary>
public readonly struct StockDefinition
{
    public readonly string TickerSymbol;
    public readonly string DisplayName;
    public readonly StockTier Tier;
    public readonly StockSector Sector;
    public readonly string FlavorText;

    public StockDefinition(string tickerSymbol, string displayName, StockTier tier, StockSector sector, string flavorText)
    {
        TickerSymbol = tickerSymbol;
        DisplayName = displayName;
        Tier = tier;
        Sector = sector;
        FlavorText = flavorText;
    }
}

/// <summary>
/// Static data class containing all named stock pools organized by tier.
/// Stocks are selected from these pools during round initialization.
/// </summary>
public static class StockPoolData
{
    // --- Penny Stocks: Meme culture, pump & dump, wild swings ---
    public static readonly StockDefinition[] PennyStocks = new StockDefinition[]
    {
        new StockDefinition("MEME", "MemeCoin Inc.", StockTier.Penny, StockSector.Crypto, "To the moon or to zero."),
        new StockDefinition("YOLO", "YOLO Ventures", StockTier.Penny, StockSector.None, "Life savings optional."),
        new StockDefinition("PUMP", "PumpCo Holdings", StockTier.Penny, StockSector.None, "Up 500% this week. Don't ask about last week."),
        new StockDefinition("FOMO", "FOMO Financial", StockTier.Penny, StockSector.Finance, "You're already late."),
        new StockDefinition("MOON", "Moonshot Labs", StockTier.Penny, StockSector.Tech, "Vaporware with a great logo."),
        new StockDefinition("HODL", "HODL Corp", StockTier.Penny, StockSector.Crypto, "Diamond hands only."),
        new StockDefinition("DOGE", "DogeChain Ltd", StockTier.Penny, StockSector.Crypto, "Much stock. Very volatile. Wow."),
        new StockDefinition("RICK", "Rick's Picks", StockTier.Penny, StockSector.Consumer, "Never gonna give you up."),
    };

    // --- Low-Value Stocks: Trend-based with reversals ---
    public static readonly StockDefinition[] LowValueStocks = new StockDefinition[]
    {
        new StockDefinition("BREW", "BrewTech Distillery", StockTier.LowValue, StockSector.Consumer, "Craft code and craft beer."),
        new StockDefinition("GEAR", "GearWorks Mfg", StockTier.LowValue, StockSector.Industrial, "They make things that make things."),
        new StockDefinition("BOLT", "Bolt Electric", StockTier.LowValue, StockSector.Energy, "Shocking potential."),
        new StockDefinition("NEON", "Neon Dynamics", StockTier.LowValue, StockSector.Tech, "Synthwave startup energy."),
        new StockDefinition("GRID", "GridLine Power", StockTier.LowValue, StockSector.Energy, "Keeping the lights on, barely."),
        new StockDefinition("FLUX", "Flux Capacitors", StockTier.LowValue, StockSector.Industrial, "1.21 gigawatts of revenue."),
    };

    // --- Mid-Value Stocks: Sector correlation, steadier trends ---
    public static readonly StockDefinition[] MidValueStocks = new StockDefinition[]
    {
        new StockDefinition("NOVA", "Nova Systems", StockTier.MidValue, StockSector.Tech, "Enterprise solutions nobody asked for."),
        new StockDefinition("VOLT", "Volt Power Corp", StockTier.MidValue, StockSector.Energy, "Renewable promises, coal reality."),
        new StockDefinition("MDCR", "MedCore Health", StockTier.MidValue, StockSector.Health, "Your health is our quarterly target."),
        new StockDefinition("TRDE", "TradeLane Logistics", StockTier.MidValue, StockSector.Industrial, "Moving boxes, moving markets."),
        new StockDefinition("CHIP", "ChipForge Semi", StockTier.MidValue, StockSector.Tech, "Silicon dreams and supply chain nightmares."),
        new StockDefinition("SOLR", "Solar Flare Energy", StockTier.MidValue, StockSector.Energy, "Harnessing the sun, burning through cash."),
        new StockDefinition("GENX", "GenX Biotech", StockTier.MidValue, StockSector.Health, "Editing genes and investor expectations."),
    };

    // --- Blue Chip Stocks: Stable, rare dramatic events ---
    public static readonly StockDefinition[] BlueChipStocks = new StockDefinition[]
    {
        new StockDefinition("APEX", "Apex Global", StockTier.BlueChip, StockSector.Finance, "Too big to fail. Probably."),
        new StockDefinition("TITN", "Titan Industries", StockTier.BlueChip, StockSector.Industrial, "Built different. Literally."),
        new StockDefinition("OMNI", "OmniCorp International", StockTier.BlueChip, StockSector.Tech, "We own everything you use."),
        new StockDefinition("VALT", "Vault Financial", StockTier.BlueChip, StockSector.Finance, "Where the big money sleeps."),
        new StockDefinition("CRWN", "Crown Pharma", StockTier.BlueChip, StockSector.Health, "Healing the world, one patent at a time."),
        new StockDefinition("FRGE", "Forge Dynamics", StockTier.BlueChip, StockSector.Industrial, "Forging the future, one merger at a time."),
    };

    private static readonly Dictionary<StockTier, StockDefinition[]> _pools = new Dictionary<StockTier, StockDefinition[]>
    {
        { StockTier.Penny, PennyStocks },
        { StockTier.LowValue, LowValueStocks },
        { StockTier.MidValue, MidValueStocks },
        { StockTier.BlueChip, BlueChipStocks },
    };

    /// <summary>
    /// Returns the full stock pool for a given tier.
    /// </summary>
    public static StockDefinition[] GetPool(StockTier tier)
    {
        return _pools[tier];
    }
}
