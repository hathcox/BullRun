/// <summary>
/// Core game constants. Single source of truth for all balance and timing values.
/// </summary>
public static class GameConfig
{
    public static readonly float StartingCapital = 10f;
    public static readonly float RoundDurationSeconds = 60f;

    // 0 = per-frame updates (no fixed interval, UpdatePrice called every frame)
    public static readonly float PriceUpdateRate = 0f;

    // Short selling: margin collateral as percentage of position value
    public static readonly float ShortMarginRequirement = 0.5f;

    // Default trade quantity (always 1 share per click, unlimited holdings — FIX-15)
    public static readonly int DefaultTradeQuantity = 1;

    // Short selling lifecycle timing constants (FIX-11)
    public static readonly float ShortRoundStartLockout = 0f;
    public static readonly float ShortForcedHoldDuration = 3.0f;
    public static readonly float ShortCashOutWindow = 5.0f;
    public static readonly float ShortCashOutFlashThreshold = 2.0f;
    public static readonly float ShortPostCloseCooldown = 3.0f;
    public static readonly int ShortBaseShares = 1;

    // Post-trade cooldown: locks out all trading for this duration after a successful trade (seconds)
    public static readonly float PostTradeCooldown = 1.0f;

    // Button dim alpha during trade cooldown (0 = invisible, 1 = fully opaque)
    public static readonly float CooldownDimAlpha = 0.35f;

    // Reputation: shop currency (FIX-12). Earning logic below (FIX-14).
    public static readonly int StartingReputation = 0;

    // Store: max relic slots per run (Epic 13, AC 11)
    public static readonly int MaxRelicSlots = 4;

    // Store: reroll base cost in Reputation (Epic 13, AC 7)
    public static readonly int RerollBaseCost = 5;

    // Store: reroll cost increment per use within a shop visit (Epic 13, AC 8)
    public static readonly int RerollCostIncrement = 2;

    // Store: default insider tip slots per shop visit (Epic 13)
    public static readonly int DefaultInsiderTipSlots = 2;

    // Store: insider tip fuzz percentage (Story 13.5, AC 11)
    public static readonly float InsiderTipFuzzPercent = 0.10f;

    // Store: insider tip costs in Reputation (Story 13.5)
    public static readonly int TipCostPriceForecast = 15;
    public static readonly int TipCostPriceFloor = 20;
    public static readonly int TipCostPriceCeiling = 20;
    public static readonly int TipCostTrendDirection = 15;
    public static readonly int TipCostEventForecast = 25;
    public static readonly int TipCostEventCount = 10;
    public static readonly int TipCostVolatilityWarning = 15;
    public static readonly int TipCostOpeningPrice = 20;

    // Store: expansion costs in Reputation (Epic 13, Story 13.4)
    // FIX-15: ExpansionCostMultiStock removed — Multi-Stock expansion permanently removed
    public static readonly int ExpansionCostLeverage = 60;
    public static readonly int ExpansionCostExpandedInventory = 50;
    public static readonly int ExpansionCostDualShort = 70;
    public static readonly int ExpansionCostIntelExpansion = 40;
    public static readonly int ExpansionCostExtendedTrading = 55;

    // Store: number of expansion cards shown per shop visit (Epic 13, Story 13.4)
    public static readonly int ExpansionsPerShopVisit = 1;

    // Store: bond prices per round (Story 13.6, AC 4). Index 0 = Round 1, index 7 = Round 8 (unavailable).
    public static readonly int[] BondPricePerRound = new int[] { 3, 5, 8, 12, 17, 23, 30, 0 };

    // Store: bond sell multiplier — sell price = purchase price × this (Story 13.6, AC 9)
    public static readonly float BondSellMultiplier = 0.5f;

    // Store: Rep earned per round per bond owned (Story 13.6, AC 6)
    public static readonly int BondRepPerRoundPerBond = 1;

    // FIX-14: Reputation earning constants
    // Base Rep awarded per round completion (0-indexed: index 0 = Round 1)
    public static readonly int[] RepBaseAwardPerRound = new int[]
    {
        10, 14, 18, 22, 28, 34, 40, 48
    };

    // Bonus multiplier on target excess (e.g., 50% excess at 0.5 rate = 25% bonus on baseRep)
    public static readonly float RepPerformanceBonusRate = 0.5f;

    // Consolation Rep per round completed before margin call failure
    public static readonly int RepConsolationPerRound = 2;

    // Reputation earned per profitable trade (successful long sell or short cover with positive P&L)
    public static readonly int RepPerProfitableTrade = 1;

    // Audio (Story 11.1)
    public static readonly float MasterVolume = 1.0f;
    public static readonly float MusicVolume = 0.7f;
    public static readonly float SfxVolume = 0.8f;
    public static readonly float UiSfxVolume = 0.8f;
    public static readonly float AmbientVolume = 0.15f;
    public static readonly float TimerWarningThreshold = 15f;
    public static readonly float TimerCriticalThreshold = 5f;
    public static readonly float TradeSfxCooldown = 0.05f; // prevent sound stacking

    // Music System (Story 11.2)
    public static readonly float MusicCrossfadeDuration = 2.0f;
    public static readonly float MusicUrgencyFadeIn = 1.0f;
    public static readonly float MusicCriticalFadeIn = 0.3f;
    public static readonly float MusicEventDuckVolume = 0.3f;
    public static readonly float MusicEventDuckFade = 0.5f;
    public static readonly float MusicEventRestoreFade = 1.0f;
    public static readonly float MusicShopCrossfade = 1.5f;
    public static readonly float MusicUrgencyVolume = 0.5f;
    public static readonly float MusicCriticalVolume = 0.6f;
    public static readonly float MusicTitleAmbientVolume = 0.15f;
    public static readonly float MusicActTransitionStingerVolume = 0.8f;
    public static readonly float MusicRoundVictoryStingerVolume = 0.9f;

    // Dynamic price floor: percentage of starting price. Prevents death spiral
    // where stocks crash to near-zero and all price-scaled movement becomes imperceptible.
    // 10% means a $6 Penny stock can't drop below $0.60.
    public static readonly float PriceFloorPercent = 0.10f;

    // Seconds at round start where price holds flat (no movement)
    public static readonly float PriceFreezeSeconds = 1.0f;

    // FIX-17: Noise ramp-up duration after price freeze ends (seconds)
    // Prevents jarring teleport by easing noise amplitude from 0% to 100%
    public static readonly float NoiseRampUpSeconds = 2.0f;

    // Market Open preview duration before trading begins
    public static readonly float MarketOpenDurationSeconds = 3f;

    // Run structure: 4 acts, 2 rounds per act, 8 rounds total
    public static readonly int TotalRounds = 8;
    public static readonly int RoundsPerAct = 2;
    public static readonly int TotalActs = 4;

    // Tier transition display duration (fade in + hold + fade out)
    public static readonly float TransitionDurationSeconds = 3f;

    /// <summary>
    /// Debug starting cash per round for F3 skip-to-round.
    /// Approximate expected cash at each round based on hitting targets + compounding.
    /// 0-indexed (index 0 = Round 1). FIX-14: Rebalanced for $10 economy.
    /// </summary>
    public static readonly float[] DebugStartingCash = new float[]
    {
        10f,    // Round 1
        20f,    // Round 2
        40f,    // Round 3
        75f,    // Round 4
        130f,   // Round 5
        225f,   // Round 6
        400f,   // Round 7
        700f,   // Round 8
    };

    /// <summary>
    /// Act configuration: act number, tier, round range, display name, tagline.
    /// Indexed by act number (1-based, so index 0 is unused).
    /// </summary>
    public static readonly ActConfig[] Acts = new ActConfig[]
    {
        new ActConfig(0, StockTier.Penny, 0, 0, "", ""),
        new ActConfig(1, StockTier.Penny, 1, 2, "Penny Stocks",
            "The Penny Pit \u2014 Where Fortunes Begin"),
        new ActConfig(2, StockTier.LowValue, 3, 4, "Low-Value Stocks",
            "Rising Stakes \u2014 Trends and Reversals"),
        new ActConfig(3, StockTier.MidValue, 5, 6, "Mid-Value Stocks",
            "The Trading Floor \u2014 Sectors in Motion"),
        new ActConfig(4, StockTier.BlueChip, 7, 8, "Blue Chips",
            "Blue Chip Arena \u2014 The Big Leagues"),
    };
}

/// <summary>
/// Immutable configuration for a single act within a run.
/// </summary>
public class ActConfig
{
    public readonly int ActNumber;
    public readonly StockTier Tier;
    public readonly int FirstRound;
    public readonly int LastRound;
    public readonly string DisplayName;
    public readonly string Tagline;

    public ActConfig(int actNumber, StockTier tier, int firstRound, int lastRound,
        string displayName, string tagline)
    {
        ActNumber = actNumber;
        Tier = tier;
        FirstRound = firstRound;
        LastRound = lastRound;
        DisplayName = displayName;
        Tagline = tagline;
    }
}

