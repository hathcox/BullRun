using UnityEngine;

/// <summary>
/// Visual configuration for chart rendering: line colors, widths, glow settings.
/// Per GDD Section 7.1: neon green (#00FF88) synthwave aesthetic.
/// </summary>
public struct ChartVisualConfig
{
    public Color LineColor;
    public Color GlowColor;
    public float LineWidth;
    public float GlowWidth;
    public float IndicatorSize;

    /// <summary>
    /// Default synthwave aesthetic config per GDD.
    /// Line: neon green, Glow: same at 30% opacity, wider width.
    /// </summary>
    public static ChartVisualConfig Default => new ChartVisualConfig
    {
        LineColor = new Color(0f, 1f, 0.533f, 1f), // #00FF88
        GlowColor = new Color(0f, 1f, 0.533f, 0.3f), // Same at 30% alpha
        LineWidth = 0.03f,
        GlowWidth = 0.12f,
        IndicatorSize = 0.08f
    };
}
