using System.Collections.Generic;

/// <summary>
/// Stock tier classification determining price ranges, volatility, and behavior.
/// </summary>
public enum StockTier
{
    Penny,
    LowValue,
    MidValue,
    BlueChip
}

/// <summary>
/// Configuration data for a stock tier. Immutable after construction.
/// </summary>
public readonly struct StockTierConfig
{
    public readonly float MinPrice;
    public readonly float MaxPrice;
    public readonly float BaseVolatility;
    public readonly float MinTrendStrength;
    public readonly float MaxTrendStrength;
    public readonly int MinStocksPerRound;
    public readonly int MaxStocksPerRound;
    public readonly float NoiseAmplitude;
    public readonly float NoiseFrequency;
    public readonly float MeanReversionSpeed;
    public readonly float EventFrequencyModifier;

    public StockTierConfig(
        float minPrice, float maxPrice,
        float baseVolatility,
        float minTrendStrength, float maxTrendStrength,
        int minStocksPerRound, int maxStocksPerRound,
        float noiseAmplitude, float noiseFrequency,
        float meanReversionSpeed,
        float eventFrequencyModifier)
    {
        MinPrice = minPrice;
        MaxPrice = maxPrice;
        BaseVolatility = baseVolatility;
        MinTrendStrength = minTrendStrength;
        MaxTrendStrength = maxTrendStrength;
        MinStocksPerRound = minStocksPerRound;
        MaxStocksPerRound = maxStocksPerRound;
        NoiseAmplitude = noiseAmplitude;
        NoiseFrequency = noiseFrequency;
        MeanReversionSpeed = meanReversionSpeed;
        EventFrequencyModifier = eventFrequencyModifier;
    }
}

/// <summary>
/// Static data class containing all stock tier configurations.
/// Values sourced from GDD Section 3.2.
/// </summary>
public static class StockTierData
{
    // Penny: $0.50-$5, High volatility, 3-5 stocks/round (pool: 8)
    // Target: ±30-50% move over 60s round, with visible jitter
    // Math: $2.50 start, strength 0.007 → trend $0.0175/s → $1.05 over 60s (42%)
    public static readonly StockTierConfig Penny = new StockTierConfig(
        minPrice: 0.50f, maxPrice: 5f,
        baseVolatility: 0.15f,
        minTrendStrength: 0.003f, maxTrendStrength: 0.010f,
        minStocksPerRound: 3, maxStocksPerRound: 5,
        noiseAmplitude: 0.12f, noiseFrequency: 3.0f,
        meanReversionSpeed: 0.30f,
        eventFrequencyModifier: 1.5f
    );

    // Low-Value: $5-$50, Moderate-high volatility, 3-4 stocks/round (pool: 6)
    // Math: $25 start, strength 0.005 → trend $0.125/s → $7.50 over 60s (30%)
    public static readonly StockTierConfig LowValue = new StockTierConfig(
        minPrice: 5f, maxPrice: 50f,
        baseVolatility: 0.10f,
        minTrendStrength: 0.002f, maxTrendStrength: 0.008f,
        minStocksPerRound: 3, maxStocksPerRound: 4,
        noiseAmplitude: 0.08f, noiseFrequency: 2.5f,
        meanReversionSpeed: 0.35f,
        eventFrequencyModifier: 1.2f
    );

    // Mid-Value: $50-$500, Medium volatility, 3-5 stocks/round (pool: 7)
    // Math: $250 start, strength 0.003 → trend $0.75/s → $45 over 60s (18%)
    public static readonly StockTierConfig MidValue = new StockTierConfig(
        minPrice: 50f, maxPrice: 500f,
        baseVolatility: 0.06f,
        minTrendStrength: 0.001f, maxTrendStrength: 0.005f,
        minStocksPerRound: 3, maxStocksPerRound: 5,
        noiseAmplitude: 0.05f, noiseFrequency: 2.0f,
        meanReversionSpeed: 0.40f,
        eventFrequencyModifier: 1.0f
    );

    // Blue Chip: $500-$5000, Lower volatility, 3-4 stocks/round (pool: 6)
    // Math: $2500 start, strength 0.002 → trend $5/s → $300 over 60s (12%)
    public static readonly StockTierConfig BlueChip = new StockTierConfig(
        minPrice: 500f, maxPrice: 5000f,
        baseVolatility: 0.03f,
        minTrendStrength: 0.0005f, maxTrendStrength: 0.003f,
        minStocksPerRound: 3, maxStocksPerRound: 4,
        noiseAmplitude: 0.025f, noiseFrequency: 1.5f,
        meanReversionSpeed: 0.50f,
        eventFrequencyModifier: 0.5f
    );

    private static readonly Dictionary<StockTier, StockTierConfig> _configs = new Dictionary<StockTier, StockTierConfig>
    {
        { StockTier.Penny, Penny },
        { StockTier.LowValue, LowValue },
        { StockTier.MidValue, MidValue },
        { StockTier.BlueChip, BlueChip }
    };

    public static StockTierConfig GetTierConfig(StockTier tier)
    {
        return _configs[tier];
    }
}
