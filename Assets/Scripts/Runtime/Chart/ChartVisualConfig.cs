using UnityEngine;

/// <summary>
/// Visual configuration for chart rendering: line colors, widths, glow settings.
/// Per GDD Section 7.1: neon green (#00FF88) synthwave aesthetic.
/// Widths are specified in pixels and converted to world units at runtime.
/// </summary>
public struct ChartVisualConfig
{
    public Color LineColor;
    public Color GlowColor;
    public float LineWidthPixels;
    public float GlowWidthPixels;
    public float IndicatorSize;

    /// <summary>
    /// Converts a pixel width to world units using the main camera's orthographic size.
    /// </summary>
    public float GetWorldWidth(float pixelWidth)
    {
        var cam = Camera.main;
        if (cam == null) return pixelWidth * 0.01f;
        return pixelWidth * (cam.orthographicSize * 2f / Screen.height);
    }

    /// <summary>
    /// Default synthwave aesthetic config per GDD.
    /// Line: neon green, Glow: same at 30% opacity, 2.5x wider.
    /// </summary>
    public static ChartVisualConfig Default => new ChartVisualConfig
    {
        LineColor = new Color(0f, 1f, 0.533f, 1f), // #00FF88
        GlowColor = new Color(0f, 1f, 0.533f, 0.3f), // Same at 30% alpha
        LineWidthPixels = 3f,
        GlowWidthPixels = 7.5f, // 2.5x main line
        IndicatorSize = 0.15f
    };
}
