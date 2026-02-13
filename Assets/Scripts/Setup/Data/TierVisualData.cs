using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Visual theme configuration for a single stock tier.
/// Defines accent color, background tint, and chart line color variation.
/// </summary>
public readonly struct TierVisualTheme
{
    public readonly Color AccentColor;
    public readonly Color BackgroundTint;
    public readonly Color ChartLineColor;

    public TierVisualTheme(Color accentColor, Color backgroundTint, Color chartLineColor)
    {
        AccentColor = accentColor;
        BackgroundTint = backgroundTint;
        ChartLineColor = chartLineColor;
    }
}

/// <summary>
/// Static data class containing visual theme definitions per stock tier.
/// Per GDD Section 7.1: synthwave aesthetic with escalating visual intensity.
///
/// Penny:     Wild/chaotic feel — hot neon green accent, dark purple tint
/// Low-Value: Warmer — amber/gold accents, dark blue tint
/// Mid-Value: Professional — cyan/teal accents, navy tint
/// Blue Chip: Premium — gold accents, deep black tint
/// </summary>
public static class TierVisualData
{
    // Penny: Wild, chaotic, "back alley trading" — neon green on dark purple
    public static readonly TierVisualTheme Penny = new TierVisualTheme(
        accentColor: new Color(0f, 1f, 0.533f, 1f),       // #00FF88 hot neon green
        backgroundTint: new Color(0.06f, 0.03f, 0.15f, 1f), // Dark purple
        chartLineColor: new Color(0f, 1f, 0.533f, 1f)      // #00FF88 neon green
    );

    // Low-Value: Warmer, more structured, "trading floor" — amber/gold on dark blue
    public static readonly TierVisualTheme LowValue = new TierVisualTheme(
        accentColor: new Color(1f, 0.75f, 0.1f, 1f),       // Amber/gold
        backgroundTint: new Color(0.03f, 0.05f, 0.15f, 1f), // Dark blue
        chartLineColor: new Color(1f, 0.75f, 0.1f, 1f)      // Amber/gold line
    );

    // Mid-Value: Professional, clean, "corner office" — cyan/teal on navy
    public static readonly TierVisualTheme MidValue = new TierVisualTheme(
        accentColor: new Color(0f, 0.9f, 0.9f, 1f),        // Cyan/teal
        backgroundTint: new Color(0.02f, 0.04f, 0.12f, 1f), // Navy
        chartLineColor: new Color(0f, 0.9f, 0.9f, 1f)       // Cyan/teal line
    );

    // Blue Chip: Premium, refined, "penthouse" — gold on deep black
    public static readonly TierVisualTheme BlueChip = new TierVisualTheme(
        accentColor: new Color(1f, 0.85f, 0f, 1f),          // Gold
        backgroundTint: new Color(0.02f, 0.02f, 0.05f, 1f), // Deep black
        chartLineColor: new Color(1f, 0.85f, 0f, 1f)         // Gold line
    );

    private static readonly Dictionary<StockTier, TierVisualTheme> _themes =
        new Dictionary<StockTier, TierVisualTheme>
    {
        { StockTier.Penny, Penny },
        { StockTier.LowValue, LowValue },
        { StockTier.MidValue, MidValue },
        { StockTier.BlueChip, BlueChip }
    };

    /// <summary>
    /// Returns the visual theme for the specified stock tier.
    /// </summary>
    public static TierVisualTheme GetTheme(StockTier tier)
    {
        if (_themes.TryGetValue(tier, out var theme))
            return theme;
        return Penny; // Fallback to lowest tier
    }

    /// <summary>
    /// Convenience: returns the visual theme for the specified act number.
    /// </summary>
    public static TierVisualTheme GetThemeForAct(int actNumber)
    {
        var tier = RunContext.GetTierForAct(actNumber);
        return GetTheme(tier);
    }

    /// <summary>
    /// Converts a TierVisualTheme to a ChartVisualConfig for use by ChartLineView.
    /// Preserves line/glow width settings from the default config.
    /// </summary>
    public static ChartVisualConfig ToChartVisualConfig(TierVisualTheme theme)
    {
        var defaults = ChartVisualConfig.Default;
        return new ChartVisualConfig
        {
            LineColor = theme.ChartLineColor,
            GlowColor = new Color(theme.ChartLineColor.r, theme.ChartLineColor.g,
                theme.ChartLineColor.b, 0.3f),
            LineWidthPixels = defaults.LineWidthPixels,
            GlowWidthPixels = defaults.GlowWidthPixels,
            IndicatorSize = defaults.IndicatorSize
        };
    }
}
