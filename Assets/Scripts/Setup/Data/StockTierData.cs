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
    // Penny: $0.10-$5, Very High volatility, 3-4 stocks/round, very slow reversion, frequent events
    public static readonly StockTierConfig Penny = new StockTierConfig(
        minPrice: 0.10f, maxPrice: 5f,
        baseVolatility: 0.15f,
        minTrendStrength: 0.05f, maxTrendStrength: 0.20f,
        minStocksPerRound: 3, maxStocksPerRound: 4,
        noiseAmplitude: 0.08f, noiseFrequency: 3.0f,
        meanReversionSpeed: 0.05f,
        eventFrequencyModifier: 1.5f
    );

    // Low-Value: $5-$50, High volatility, 3-4 stocks/round, slow reversion, above-average events
    public static readonly StockTierConfig LowValue = new StockTierConfig(
        minPrice: 5f, maxPrice: 50f,
        baseVolatility: 0.10f,
        minTrendStrength: 0.03f, maxTrendStrength: 0.12f,
        minStocksPerRound: 3, maxStocksPerRound: 4,
        noiseAmplitude: 0.05f, noiseFrequency: 2.5f,
        meanReversionSpeed: 0.15f,
        eventFrequencyModifier: 1.2f
    );

    // Mid-Value: $50-$500, Medium volatility, 2-3 stocks/round, moderate reversion, baseline events
    public static readonly StockTierConfig MidValue = new StockTierConfig(
        minPrice: 50f, maxPrice: 500f,
        baseVolatility: 0.06f,
        minTrendStrength: 0.02f, maxTrendStrength: 0.08f,
        minStocksPerRound: 2, maxStocksPerRound: 3,
        noiseAmplitude: 0.03f, noiseFrequency: 2.0f,
        meanReversionSpeed: 0.30f,
        eventFrequencyModifier: 1.0f
    );

    // Blue Chip: $500-$5000, Low-Med volatility, 2-3 stocks/round, fast reversion, rare events
    public static readonly StockTierConfig BlueChip = new StockTierConfig(
        minPrice: 500f, maxPrice: 5000f,
        baseVolatility: 0.03f,
        minTrendStrength: 0.01f, maxTrendStrength: 0.04f,
        minStocksPerRound: 2, maxStocksPerRound: 3,
        noiseAmplitude: 0.015f, noiseFrequency: 1.5f,
        meanReversionSpeed: 0.60f,
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
