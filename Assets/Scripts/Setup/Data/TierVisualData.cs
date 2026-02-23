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
/// All colors derived from ColorPalette.
/// </summary>
public static class TierVisualData
{
    // Penny: Wild, chaotic — green accent on dark teal
    public static readonly TierVisualTheme Penny = new TierVisualTheme(
        accentColor: ColorPalette.Green,
        backgroundTint: new Color(0.03f, 0.04f, 0.06f, 1f),
        chartLineColor: ColorPalette.Green
    );

    // Low-Value: Warmer, more structured — amber on dark blue
    public static readonly TierVisualTheme LowValue = new TierVisualTheme(
        accentColor: ColorPalette.Amber,
        backgroundTint: new Color(0.03f, 0.05f, 0.15f, 1f),
        chartLineColor: ColorPalette.Amber
    );

    // Mid-Value: Professional, clean — cyan on navy
    public static readonly TierVisualTheme MidValue = new TierVisualTheme(
        accentColor: ColorPalette.Cyan,
        backgroundTint: new Color(0.02f, 0.04f, 0.12f, 1f),
        chartLineColor: ColorPalette.Cyan
    );

    // Blue Chip: Premium, refined — gold on deep black
    public static readonly TierVisualTheme BlueChip = new TierVisualTheme(
        accentColor: ColorPalette.Gold,
        backgroundTint: new Color(0.02f, 0.02f, 0.05f, 1f),
        chartLineColor: ColorPalette.Gold
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
            GlowColor = ColorPalette.WithAlpha(theme.ChartLineColor, 0.3f),
            LineUpColor = ColorPalette.Green,
            LineDownColor = ColorPalette.Red,
            GlowUpColor = ColorPalette.WithAlpha(ColorPalette.Green, 0.3f),
            GlowDownColor = ColorPalette.WithAlpha(ColorPalette.Red, 0.3f),
            LineWidthPixels = defaults.LineWidthPixels,
            GlowWidthPixels = defaults.GlowWidthPixels,
            IndicatorSize = defaults.IndicatorSize
        };
    }
}
